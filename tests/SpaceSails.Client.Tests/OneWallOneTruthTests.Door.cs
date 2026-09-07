using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// <b>THE DOOR, AND THE GUN</b> — the two consumers of the wall list that are not the pen, and the reporting
/// <see cref="OneWallOneTruthTests"/> fails with.
///
/// <para>What this part owns is the doorway half of the same law: a locked door is backed by the same list
/// the pen draws from, a sealed doorway is drawn shut even with the captain standing at it, and every sentry
/// sight call is handed the stone that INCLUDES shut doors — so nothing can shoot through a door it cannot
/// walk through.</para>
/// </summary>
public sealed partial class OneWallOneTruthTests
{
    // ══ THE DOOR ═════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #442 · <b>A LOCKED DOOR IS A WALL WITH A DOOR'S LOOK.</b>
    ///
    /// <para>The issue names the idiom by its own comment: a locked door is <i>"decoration only, and is
    /// backed by a real wall so you can't pass"</i> — two records for one barrier, kept in step by hand,
    /// which is precisely the parallel construct that drifts. This pins the pairing so a drift goes red:
    /// every locked leaf on every deck that has one must lie ON a collision segment out of the SAME list
    /// the pen draws from. A locked door whose backing wall moved, or was never laid, fails here.</para>
    ///
    /// <para>The plan's own answer about the doorway (<see cref="DeckPlan.DoorwayIsWalledUp"/>) is asserted
    /// against the walls themselves rather than trusted, so the derivation cannot quietly start answering a
    /// different question than the one the pen is now asking it.</para>
    /// </summary>
    [Fact]
    public void ALockedDoorIsBackedByTheSameListThePenDrawsFrom()
    {
        var bad = new List<string>();
        int locked = 0, open = 0;

        foreach (string scene in Scenes.Names())
        {
            DeckPlan plan;
            try
            {
                plan = Scenes.Build(scene);
            }
            catch (Exception)
            {
                continue; // a scene this harness cannot stand up is not this guard's business
            }

            foreach (DeckPlan.Door d in plan.Doors)
            {
                double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
                bool stone = plan.Walls.Any(w => !w.Unseen && OnTheSameLine(w, d));
                _ = d.Locked ? locked++ : open++;

                if (d.Locked && !stone)
                {
                    bad.Add($"  {scene}: a LOCKED door at ({mx:0.##}, {my:0.##}) with no wall behind it — "
                            + "it reads as a barrier and is not one.");
                }

                // …and the plan's own derived answer is the walls' answer, on every door in the game.
                if (plan.DoorwayIsWalledUp(d) != stone)
                {
                    bad.Add($"  {scene}: the door at ({mx:0.##}, {my:0.##}) is "
                            + (stone ? "walled up" : "an open doorway")
                            + $" and the plan says {(plan.DoorwayIsWalledUp(d) ? "walled up" : "open")} — "
                            + "the derivation the pen reads has parted company with the list.");
                }
            }
        }

        Assert.True(locked > 0, "no scene carries a locked door — this guard is asserting nothing");
        Assert.True(open > 0, "no scene carries an ordinary door — this guard is asserting nothing");
        Fail(bad, "door(s) whose look and whose stone disagree");
    }

    /// <summary>
    /// #442 · <b>A LEAF WITH A WALL ACROSS IT IS NEVER DRAWN SLIDING OPEN.</b>
    ///
    /// <para>The second direction of the owner's report, and the one the net found rather than the one it
    /// was written for: three constructs in the game are a wall PLUS an unlocked door laid over it — the
    /// ship's own shuttle hatch while she is docked ("the hatch itself — sealed here"), every dogged
    /// compartment hatch (<c>ShipWith</c>: <i>"a dogged hatch is a WALL, and the walls are what everything
    /// else asks"</i>), and a haven's sealed berth hatch. All three drew as ORDINARY automatic doors, which
    /// means they retracted as the captain walked up — the player watched the opening open and then walked
    /// into stone.</para>
    ///
    /// <para>The captain is stood <b>right at</b> each doorway, inside <c>DoorOpenRadius</c>, which is the
    /// only place the bug exists: further off, every leaf is drawn shut anyway and the guard would pass on a
    /// world that cannot tell pass from fail.</para>
    ///
    /// <para><b>What the pen is asked is the RETRACTED STUB, and the first cut of this guard asked the
    /// wrong thing.</b> A retracted leaf is two short strokes reaching a quarter of the way in from each
    /// jamb; a shut one is a single full-span stroke. Asking for the full span passed on the broken
    /// renderer, because the WALL behind a sealed hatch is drawn at exactly the same two endpoints — the
    /// guard was reading the stone and calling it the door. The stub belongs to nothing else on the deck,
    /// so that is what is looked for, and taking the fix back out now puts 18 doorways on the report.</para>
    /// </summary>
    [Fact]
    public void ASealedDoorwayIsDrawnShut_EvenWithTheCaptainStandingAtIt()
    {
        var bad = new List<string>();
        int sealedLeaves = 0, ordinary = 0;

        foreach (string scene in Scenes.Names())
        {
            DeckPlan plan;
            try
            {
                plan = Scenes.Build(scene);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (DeckPlan.Door d in plan.Doors)
            {
                if (d.Locked)
                {
                    continue; // always drawn shut and cold; the guard above owns that one
                }
                double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
                bool walled = plan.DoorwayIsWalledUp(d);

                // Standing ON the leaf: as open as the interlock will ever let this one be.
                (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, mx, my);
                bool retracted = Stub(strokes, place, d);

                if (walled)
                {
                    sealedLeaves++;
                    if (retracted)
                    {
                        bad.Add($"  {scene}: the doorway at ({mx:0.##}, {my:0.##}) has a wall across it and "
                                + "the pen slid the leaf ASIDE — an opening you cannot walk through.");
                    }
                }
                else if (d.Interlock == 0)
                {
                    // No partner to take turns with (#462), so standing in it is the whole of the rule and
                    // this one MUST be open. The interlocked pairs are left out rather than guessed at: the
                    // far end of a tube is drawn shut on purpose and is not this guard's business.
                    ordinary++;
                    if (!retracted)
                    {
                        bad.Add($"  {scene}: the ordinary doorway at ({mx:0.##}, {my:0.##}) is walkable and "
                                + "the pen drew the leaf SHUT with the captain standing in it.");
                    }
                }
            }
        }

        Assert.True(sealedLeaves > 0,
            "not one sealed doorway in the whole scene list — this guard is being handed a world that "
            + "cannot tell pass from fail");
        Assert.True(ordinary > 0, "not one ordinary doorway — the retract path is untested");
        Fail(bad, "doorway(s) the pen drew as the opposite of what the walls say");
    }

    /// <summary>Did the pen draw this door RETRACTED — the short stub reaching a quarter of the way in from
    /// a jamb that <c>DeckView.DrawTheDoors</c> lays for an open leaf, and that nothing else on a deck
    /// draws? Asked at both jambs, either of which is proof the leaf slid aside.</summary>
    private static bool Stub(IEnumerable<Stroke> strokes, DeckView.Placement p, in DeckPlan.Door d)
    {
        (float ax, float ay) = On(p, d.X1, d.Y1);
        (float bx, float by) = On(p, d.X2, d.Y2);
        (float qax, float qay) = On(p, d.X1 + ((d.X2 - d.X1) * 0.25f), d.Y1 + ((d.Y2 - d.Y1) * 0.25f));
        (float qbx, float qby) = On(p, d.X2 - ((d.X2 - d.X1) * 0.25f), d.Y2 - ((d.Y2 - d.Y1) * 0.25f));
        return strokes.Any(s =>
            (Near(s.X1, ax) && Near(s.Y1, ay) && Near(s.X2, qax) && Near(s.Y2, qay))
            || (Near(s.X1, bx) && Near(s.Y1, by) && Near(s.X2, qbx) && Near(s.Y2, qby)));
    }

    /// <summary>Does this wall lie along this door — same line, covering its span? A locked door is drawn
    /// as the middle stretch of the wall behind it, so "backed" means the wall covers the door's two ends
    /// AND its MIDDLE.
    ///
    /// <para>The middle is the load-bearing third of the test. A CARVED doorway leaves two stubs whose inner
    /// ends touch the door's ends exactly (<c>DeckExpansions.CarveDoorway</c>), so an end-only test calls
    /// every ordinary auto-door on the ship "walled up" — the gap between the stubs is the whole point of a
    /// doorway and it is precisely what a stub does not cover.</para></summary>
    private static bool OnTheSameLine(in DeckPlan.Wall w, in DeckPlan.Door d) =>
        SurfaceCollision.DistanceToSegment(d.X1, d.Y1, w.X1, w.Y1, w.X2, w.Y2) < 0.15
        && SurfaceCollision.DistanceToSegment(d.X2, d.Y2, w.X1, w.Y1, w.X2, w.Y2) < 0.15
        && SurfaceCollision.DistanceToSegment(
            (d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0, w.X1, w.Y1, w.X2, w.Y2) < 0.15;

    // ══ THE GUN ══════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #442 / #437 · <b>THE STONE A SHOT IS MEASURED AGAINST AND THE STONE ITS BEAM IS DRAWN AGAINST ARE
    /// ONE LIST.</b>
    ///
    /// <para>The surface page keeps two wall lists on purpose and they are not interchangeable:
    /// <c>_deckPlan.CollisionField</c> is the stone (what stops a boot) and <c>SightBlockers()</c> is the
    /// stone PLUS whatever doors are shut this instant (what stops an eye and a round — #465, because
    /// <i>"the gun would be behind one door and not shooting through it"</i>). Every reader that decides
    /// what a sentry can HIT must be handed the second one; the page's own comment beside the beam says so
    /// (<i>"Same CanEngage gate as the volley, so the beam can only ever be drawn at the target the volley
    /// could actually have spent its round on"</i>).</para>
    ///
    /// <para>This reads the page and insists every <c>SentryBot</c> sight call is handed
    /// <c>SightBlockers()</c>. Source-level on purpose: the alternative is standing a whole Blazor page up
    /// to catch a one-argument slip, and the slip is exactly the kind a reader can see and a runtime test
    /// only reaches on the one frame a Reever happens to be behind a shut door.</para>
    ///
    /// <para><b>Proven RED on today's code:</b> <c>NearestReeverInArc</c> — the method that decides where
    /// the firing beam is PAINTED — passed <c>_deckPlan.CollisionField</c> while the volley beside it
    /// passed <c>SightBlockers()</c>, so the picture and the round disagreed about a shut door.</para>
    /// </summary>
    [Fact]
    public void EverySentrySightCall_IsHandedTheStoneThatIncludesShutDoors()
    {
        string page = ClientSource("Pages", "Map.Surface.Reevers.Sentries.cs");
        var bad = new List<string>();
        int calls = 0;

        foreach (string method in new[] { "SentryBot.CanEngage(", "SentryBot.Step(" })
        {
            int at = 0;
            while ((at = page.IndexOf(method, at, StringComparison.Ordinal)) >= 0)
            {
                int close = MatchingParen(page, at + method.Length - 1);
                string args = close < 0 ? page[at..] : page[(at + method.Length)..close];
                calls++;
                if (!args.Contains("SightBlockers()", StringComparison.Ordinal))
                {
                    int line = page.Take(at).Count(c => c == '\n') + 1;
                    bad.Add($"  Map.Surface.Reevers.Sentries.cs:{line} · {method}…) is handed "
                            + $"`{args.Split(',').Last().Trim()}` — not SightBlockers(). A shut door stops "
                            + "the round and this reader has never heard of it.");
                }
                at = close < 0 ? at + method.Length : close;
            }
        }

        Assert.True(calls >= 3,
            $"only {calls} sentry sight call(s) found — the page was renamed or the guard is reading the "
            + "wrong text");
        Fail(bad, "sentry sight call(s) handed the wrong wall list");
    }

    /// <summary>One of the client's own source files, read off disk — the same trick
    /// <c>SeatsAreDrawnTests</c> uses to hold a claim about the pen that no runtime call can reach.</summary>
    private static string ClientSource(params string[] parts)
    {
        System.IO.DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string src = System.IO.Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (System.IO.Directory.Exists(src))
            {
                return System.IO.File.ReadAllText(System.IO.Path.Combine([src, .. parts]));
            }
            at = at.Parent;
        }
        throw new System.IO.DirectoryNotFoundException(
            $"could not find the repo root above {AppContext.BaseDirectory}");
    }

    /// <summary>Index of the ')' that closes the '(' at <paramref name="open"/>, or -1.</summary>
    private static int MatchingParen(string s, int open)
    {
        int depth = 0;
        for (int i = open; i < s.Length; i++)
        {
            if (s[i] == '(')
            {
                depth++;
            }
            else if (s[i] == ')' && --depth == 0)
            {
                return i;
            }
        }
        return -1;
    }

    // ── REPORTING ─────────────────────────────────────────────────────────────────────────────────────

    private static string Dissent(bool boots, bool shamble, bool round, bool eye, bool drawn)
    {
        var said = new List<string>();
        said.Add(boots ? "the boot is stopped" : "THE BOOT WALKS THROUGH");
        said.Add(shamble ? "the shamble is stopped" : "THE OLD ONE WALKS THROUGH");
        said.Add(round ? "the round is stopped" : "THE ROUND PASSES");
        said.Add(eye ? "the eye is broken" : "THE EYE SEES THROUGH");
        said.Add(drawn ? "the pen drew it" : "THE PEN DREW NOTHING");
        return string.Join(", ", said) + ".";
    }

    private static void Fail(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad.Take(40))
        {
            sb.AppendLine(line);
        }
        if (bad.Count > 40)
        {
            sb.AppendLine($"  …and {bad.Count - 40} more.");
        }
        Assert.Fail(sb.ToString());
    }
}
