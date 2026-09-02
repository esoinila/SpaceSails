using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #681 · THE LANDING'S OWN LAW, which the ground has never had one of. Owner, one URL after the world
/// finished loading: <i>"The second url put me into the wall... I cannot move."</i>
///
/// <para><b>Why nothing caught it.</b> <c>YouCanWalkTheHiveTests</c> floods every floor <i>from the lift</i>;
/// <c>TheLiftPutsYouSomewhereYouCanSTANDTests</c> pins the lift exit. Every reachability audit this project
/// owns starts from a point it assumes is good — and the landing spawn IS that assumption. Same blind spot
/// that let #600 live for weeks: an audit proves you can reach things from X and never once that X is
/// somewhere a person can stand.</para>
///
/// <para><b>The ladder, weakest to strongest</b> — the owner's own, from the issue thread. All three run,
/// because they catch different breaks:</para>
/// <list type="number">
/// <item><b>Standable</b> — the square is in the walkable field. Catches spawn-in-wall.</item>
/// <item><b>Can move</b> — at least one orthogonal neighbour is walkable. His own suggestion (<i>"or a test
/// on spawn that the player can move"</i>), and strictly stronger: a square can be clear and still be a cell
/// with wall on all four sides, which rung 1 would happily pass.</item>
/// <item><b>Can get home</b> — the spawn's walkable component holds the way home, the shelter and (where the
/// ground has one) the lift head. #600's lesson applied to minute zero: reachable is not the same as
/// returnable.</item>
/// </list>
///
/// <para><b>Asserted on the UN-NUDGED square.</b> <see cref="SpawnNudge"/> now walks a badly-placed captain
/// out to clear ground at runtime, and that net must never be allowed to absorb a generator bug — the same
/// way the swept apron quietly hid #574. So every rung here asks the placer what it CHOSE, not where the
/// captain ended up. Net catches the player; audit catches the bug.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 90 s over 4 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheLandingPutsYouSomewhereYouCanWALKTests
{
    // Every landable body the scenario ships, plus the two cheat rocks the secret-lab URLs park alongside the
    // berth — `?secretlab=1` and `?secretlab=deep` are boot cheats that change the surface layout (they force
    // a clandestine site onto the ground), so they are sites the generator admits and belong in the sweep.
    // The list is hand-kept for the same reason EverySiteMeetsTheSpecTests keeps one: the scenario is a
    // wwwroot JSON the test host cannot reliably locate.
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The search box for a surface walk. It has to reach ABOVE the landing band, because the way
    /// home is the tube mouth and that sits at the top rim — capping at the band would fail every site for a
    /// goal that was merely outside the box rather than walled off (#602's own lesson, paid for once).</summary>
    private static (double, double, double, double) Bounds =>
        (Field.LeftX, Field.BottomY, Field.RightX, Field.TopY);

    /// <summary>One thing the sim PLACES the captain on, for one site: what it is called, where it puts them,
    /// and the deck they land on.</summary>
    private readonly record struct Placing(string What, double X, double Y, DeckPlan Deck, string Site);

    /// <summary>
    /// Every (body × site × boot-cheat) combination the generator admits, and every square any of them stands
    /// the captain on before they have taken a step.
    ///
    /// <para><c>?site=N</c> chooses the site; <c>?body=</c> / <c>?land=&lt;id&gt;</c> choose the body;
    /// <c>?secretlab=1|deep</c> and <c>?kaamos=hq</c> force a clandestine site onto the ground (which is what
    /// grows the lift head and its two imported doors). Those are the boot cheats in
    /// <c>docs/testing-guide.md</c> that change what the surface is made of — the rest (<c>?reevers=</c>,
    /// <c>?watchers=</c>, <c>?nerve=</c>, <c>?kaamos=pod</c>) change what happens ON it, not its walls.</para>
    /// </summary>
    private static IEnumerable<Placing> EveryPlacing()
    {
        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                foreach (bool hasLab in new[] { false, true })
                {
                    DeckPlan deck = MoonSurface.SurfaceDeck(
                        body, body, [], 0, (_, _) => { },
                        siteSalt: site.LayoutSalt, siteName: site.Name, hasSecretSite: hasLab);

                    string where = $"{body} · {site.Name}{(hasLab ? " · ?secretlab" : "")}";

                    // ?land=1 on an ordinary ground: the open regolith a shuttle-length below the pad. Asked
                    // of the field, which is also what the claim ledger asks — a test that retyped the offset
                    // would be auditing its own copy rather than the ground that ships.
                    (double dropX, double dropY, _) = SurfaceLayout.LandingApproach(Field);
                    yield return new Placing(
                        "?land=1 sets you down on the open regolith", dropX, dropY, deck, where);

                    if (!hasLab)
                    {
                        continue;
                    }

                    // ?secretlab=…&land=1: the doorstep of the lift head's shed. THE ONE THE OWNER LANDED IN.
                    // Asked of the shed itself — a test that retyped the offset would be the same bug it is
                    // guarding against, which is the whole moral of #602.
                    MoonSurface.LiftHeadBox head = MoonSurface.LiftHead(body, site.LayoutSalt, Field);
                    (double stepX, double stepY) = head.DoorStep;
                    yield return new Placing(
                        "?secretlab=…&land=1 sets you down on the lift head's doorstep",
                        stepX, stepY, deck, where);

                    // …and the surface end of the lift, because if the car reuses the square the landing
                    // does, that is the second head of one bug and both must be pinned.
                    (double carX, double carY) = head.CarFloor;
                    yield return new Placing(
                        "the car lets you out into the shed", carX, carY, deck, where);
                }
            }
        }
    }

    /// <summary>Report the whole table, then fail once. A guard that dies on the first bad site turns "sweep
    /// every ground" into a queue of one-at-a-time surprises — the house idiom, from
    /// <c>EverySiteMeetsTheSpecTests</c>.</summary>
    private static void AuditEveryPlacing(Func<Placing, string?> check, string law)
    {
        var bad = new List<string>();
        foreach (Placing p in EveryPlacing())
        {
            string? complaint = check(p);
            if (complaint is not null)
            {
                bad.Add($"  {p.Site}: {p.What} at ({p.X:F1}, {p.Y:F1}) — {complaint}");
            }
        }

        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} placement(s) break the law: {law}");
        foreach (string line in bad.Take(24))
        {
            sb.AppendLine(line);
        }
        if (bad.Count > 24)
        {
            sb.AppendLine($"  …and {bad.Count - 24} more.");
        }
        Assert.Fail(sb.ToString());
    }

    [Fact]
    public void RungOne_TheCaptainCanSTANDWhereTheLandingPutsThem()
    {
        // The bug, stated as the law. Nothing subtle: the square the sim chose is inside a wall, and the
        // captain spends the excursion looking at a HUD full of offers they cannot walk to.
        AuditEveryPlacing(
            p => DeckReachability.Standable(p.X, p.Y, DeckPlan.AvatarRadius, p.Deck.CollisionField)
                ? null
                : "that is solid.",
            "spec — you can stand where you are set down");
    }

    [Fact]
    public void RungTwo_AndTheCaptainCanMOVE()
    {
        // Owner: "or a test on spawn that the player can move." Stronger than standability, and it costs one
        // more line: a square can be perfectly clear and still be a CELL, with wall on all four sides. Rung
        // one would sign that off, and the captain would have exactly the experience they had — standing in a
        // live world, unable to take one step.
        //
        // A step is DeckReachability's own step, so "can move" here means precisely what it means to the A*
        // audits below. Two notions of a pace would be two pathfinders agreeing by luck.
        const double step = DeckReachability.DefaultStep;
        (double Dx, double Dy)[] orthogonal = [(step, 0), (-step, 0), (0, step), (0, -step)];

        AuditEveryPlacing(
            p => orthogonal.Any(d => DeckReachability.Standable(
                     p.X + d.Dx, p.Y + d.Dy, DeckPlan.AvatarRadius, p.Deck.CollisionField))
                ? null
                : "every square around it is wall — a sealed cell.",
            "spec — from where you are set down you can take a step");
    }

    [Fact]
    public void RungThree_AndTheWayHomeIsOnTheSameSideOfTheWalls()
    {
        // #600's lesson applied to minute zero. Standing and stepping are not enough: a captain set down on
        // an island can do both and is still finished, because the tank is running the whole time and the
        // shelter and the tube are on the other side of a wall they will never find a way round.
        //
        // Both ends of every walk are checked, never assumed — that is the rule this project learned the hard
        // way. A goal buried in wall makes CanReach return false and reads as "the ground is broken" when the
        // truth is "the test aimed at a wall".
        AuditEveryPlacing(p =>
        {
            if (!DeckReachability.Standable(p.X, p.Y, DeckPlan.AvatarRadius, p.Deck.CollisionField))
            {
                return null;   // rung one owns that failure; saying it twice is not two findings.
            }

            var from = new DeckReachability.Point(p.X, p.Y);
            var wanted = new List<(string Name, DeckReachability.Point At)>
            {
                // The way home. The tube mouth is standable on every site by construction — it is where an
                // ordinary landing actually leaves the captain — which is exactly why it is the honest goal.
                ("the way home", new DeckReachability.Point(MoonSurface.SpawnX, MoonSurface.SpawnY)),
            };

            // The shelter's door — the answer to the air mechanic, and the one destination on the ground that
            // a captain's life depends on reaching. Its doorway, not its middle: the middle is a tank inside a
            // drum, and a walk that has to end ON the console would be measuring the console's clearance.
            foreach (DeckPlan.ConsoleSpot c in p.Deck.Consoles)
            {
                if (c.Kind == DeckPlan.ConsoleKind.ShelterDoor)
                {
                    wanted.Add(("the shelter", new DeckReachability.Point(c.X, c.Y)));
                }
                // …and the way down, on the grounds that have one. A lift head you cannot walk to from the
                // square the game set you on is the whole feature, sealed off at the first step.
                if (c.Kind == DeckPlan.ConsoleKind.HiveHead)
                {
                    wanted.Add(("the lift head", new DeckReachability.Point(c.X, c.Y)));
                }
            }

            IReadOnlyList<string> missed = DeckReachability.Unreachable(
                from, wanted, p.Deck.CollisionField, DeckPlan.AvatarRadius, Bounds);

            return missed.Count == 0 ? null
                : "cannot be walked from there to " + string.Join(", ", missed) + ".";
        }, "spec — from where you are set down you can reach the way home, the shelter and the way down");
    }

    [Fact]
    public void TheNudgeIsANetAndNotAFix()
    {
        // The pairing that keeps the runtime rescue honest, pinned so nobody can loosen it by accident.
        //
        // Owner: "it says so" — a nudge that happened quietly is a bug that shipped quietly. And bounded:
        // "if nothing walkable is that close, the site is broken beyond nudging and should fail loudly".
        var walls = new List<SurfaceCollision.Segment>
        {
            // A closed box a captain could not stand in the middle of, and nothing else for a long way.
            new(-2, -2, 2, -2), new(-2, 2, 2, 2), new(-2, -2, -2, 2), new(2, -2, 2, 2),
            new(-2, -1, 2, -1), new(-2, 0, 2, 0), new(-2, 1, 2, 1),
        };

        SpawnNudge.Result rescued = SpawnNudge.Clear(0, 0, DeckPlan.AvatarRadius, walls);
        Assert.True(rescued.Moved, "the net did not catch a captain standing inside hatched mass.");
        Assert.False(rescued.Failed);
        Assert.False(SurfaceCollision.Blocked(rescued.X, rescued.Y, DeckPlan.AvatarRadius, walls));

        // Deterministic: the same blocked square gives the same rescue, every boot, forever. An unreproducible
        // rescue would make the next report of this bug impossible to chase.
        SpawnNudge.Result again = SpawnNudge.Clear(0, 0, DeckPlan.AvatarRadius, walls);
        Assert.Equal(rescued, again);

        // …and bounded. Walled in past MaxDu, it refuses to help and hands the caller its loud failure.
        var sealedIn = new List<SurfaceCollision.Segment>();
        for (double t = -12; t <= 12.001; t += 0.5)
        {
            sealedIn.Add(new(-12, t, 12, t));   // solid hatch across the whole neighbourhood
        }
        SpawnNudge.Result hopeless = SpawnNudge.Clear(0, 0, DeckPlan.AvatarRadius, sealedIn);
        Assert.True(hopeless.Failed, "a captain sealed in solid rock was quietly reported as fine.");
        Assert.Equal(0, hopeless.MovedDu);
    }
}
