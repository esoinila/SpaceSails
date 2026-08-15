using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #866 · POINT AT THE GARDEN AND WALK THERE — and, whatever happens, BE TOLD.
///
/// <para>Owner, live on the B1 park gravel, 2026-08-13: <i>"Now the click to walk does not work here in
/// park?"</i> He clicked open, walkable-looking gravel about six deck units off, with no fixture under the
/// cursor and no modal in the way, and the captain did not move one pixel — and nothing was said.</para>
///
/// <para><b>What the click landed on was a raised bed.</b> The park's floor WEARS A PHOTOGRAPH, and the
/// gravel in that photograph curves where the deck's own walk is a published centreline; what reads as
/// open ground in the picture is, on the plan, one of nine beds fourteen deck units by seven. That much is
/// the room being a room. The bug is what the planner did about it: A* counted a goal reached only within
/// ONE LATTICE STEP — half a deck unit — so a finger that lands anywhere on a thing wider than half a metre
/// asks for a square that is solid, the search can only answer <i>no route at all</i>, and it flooded every
/// walkable square on the floor to find that out (456 ms, natively, on a desk machine, for one refused
/// click).</para>
///
/// <para>So this file asks the room the owner was standing in the question he asked it: <b>from where a
/// captain stands on the gravel, does a click on the park answer?</b> Everywhere in it — the published
/// walk, every bed, every bench, and a grid over the whole box, which is what a finger actually does.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheParkTakesAClickTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan FloorOf(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park)> EveryPark()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                if (floor.Park is { } park)
                {
                    yield return (body, level, park);
                }
            }
        }
    }

    /// <summary>
    /// THE CLICK, AS THE CLIENT MAKES IT — <c>Map.Deck.ClickToWalkAt</c>'s own two steps, in its own order:
    /// snap to whatever fixture the finger was on, then plan with a POINTED-AT goal's reach.
    ///
    /// <para>The snap is duplicated here and that is deliberate: it is four lines of the client's private
    /// method, and a guard that skipped it would be planning a walk nobody in the game ever plans. What must
    /// NOT be duplicated is the planning, and it is not — <see cref="AutoWalk.Plan"/> is called with the
    /// same arguments off the same real <see cref="DeckPlan"/>, including
    /// <see cref="AutoWalk.PointingReachDu"/>, which is the whole of what this fix changed.</para>
    /// </summary>
    private static AutoWalk.Attempt Click(DeckPlan deck, DeckReachability.Point from, double gx, double gy)
    {
        (double tx, double ty) = (gx, gy);
        if (deck.NearestConsoleSpot(gx, gy) is { } spot)
        {
            (tx, ty) = spot.NearestPointTo(gx, gy);
        }
        else
        {
            foreach (DeckPlan.Door door in deck.Doors)
            {
                double mx = (door.X1 + door.X2) / 2.0, my = (door.Y1 + door.Y2) / 2.0;
                if (((gx - mx) * (gx - mx)) + ((gy - my) * (gy - my))
                    <= DeckPlan.InteractRadius * DeckPlan.InteractRadius)
                {
                    (tx, ty) = (mx, my);
                    break;
                }
            }
        }

        var to = new DeckReachability.Point(tx, ty);
        return AutoWalk.Plan(
            true, from, to, deck.CollisionField, DeckPlan.AvatarRadius,
            AutoWalk.BoundsFor(deck.CollisionSegments, from, to),
            DeckReachability.DefaultStep, AutoWalk.PointingReachDu);
    }

    // ── (a) THE PARK ANSWERS A CLICK ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// FROM A STAND SQUARE ON THE GRAVEL, EVERY PLACE IN THE PARK ANSWERS A CLICK — the published walk, the
    /// beds, the benches, and a grid over the whole box.
    ///
    /// <para>The grid is the half that matters and the reason this is not written as "every point on the
    /// published Walk". Those samples are the centreline of the gravel and they were ALREADY fine (145 of
    /// 145 on luna B1 before the fix); a guard pointed only at them would have reported that the park was
    /// healthy on the very afternoon the owner could not walk across it. A finger does not land on a
    /// centreline. It lands on the room.</para>
    ///
    /// <para><b>Proven RED</b> on today's park, by putting <c>AutoWalk.PointingReachDu</c> back to the
    /// one-lattice-step arrival the planner shipped with (pass 0 for the reach):</para>
    /// <code>
    /// 4407 of 8382 clicks in a park planned no route at all — the captain clicks and nothing happens:
    ///   luna B1: from (-5.0, -208.5), a click at (-99.5, -218.5) planned nothing.  [in bed 1]
    ///   luna B1: from (-5.0, -208.5), a click at (-99.5, -216.5) planned nothing.  [in bed 1]
    ///   luna B1: from (-5.0, -208.5), a click at (-97.5, -218.5) planned nothing.  [in bed 1]
    /// </code>
    /// </summary>
    [Fact]
    public void EveryPlaceInTheParkAnswersAClickFromTheGravel()
    {
        var bad = new List<string>();
        int parks = 0, clicks = 0;

        foreach ((string body, int level, UndergroundComplex.Park park) in EveryPark())
        {
            DeckPlan deck = FloorOf(body, level);
            parks++;

            // Where the captain is standing: the park's own published spot (?park=1 stands him exactly
            // there) and two samples off the gravel itself, so the verdict is not about one square.
            var stands = new List<DeckReachability.Point> { new(park.X, park.Y) };
            stands.Add(new DeckReachability.Point(park.Walk[park.Walk.Count / 3].X,
                park.Walk[park.Walk.Count / 3].Y));
            stands.Add(new DeckReachability.Point(park.Walk[2 * park.Walk.Count / 3].X,
                park.Walk[2 * park.Walk.Count / 3].Y));

            foreach (DeckReachability.Point from in stands)
            {
                Assert.True(
                    DeckReachability.Standable(from.X, from.Y, DeckPlan.AvatarRadius, deck.CollisionField),
                    $"{body} B{-level}: the captain cannot stand at ({from.X:F1}, {from.Y:F1}) — every "
                    + "verdict below would be about a body inside a wall.");
            }

            // WHAT A FINGER POINTS AT. The room as a grid (a click lands where it lands), plus the three
            // lists the park publishes, so a bed or a bench that stopped answering is named as itself.
            var targets = new List<(double X, double Y, string What)>();
            for (double gx = park.X0 + 1; gx <= park.X1 - 1; gx += 5.0)
            {
                for (double gy = park.Y0 + 1; gy <= park.Y1 - 1; gy += 5.0)
                {
                    targets.Add((gx, gy, "the gravel"));
                }
            }
            foreach (UndergroundComplex.GrowingBed bed in park.Beds)
            {
                targets.Add((bed.X, bed.Y, $"the middle of bed {bed.Number}"));
            }
            foreach ((double bx, double by) in park.Benches)
            {
                targets.Add((bx, by, "a bench"));
            }
            for (int i = 0; i < park.Walk.Count; i += 4)
            {
                targets.Add((park.Walk[i].X, park.Walk[i].Y, "the published walk"));
            }

            // One stand square per park for the whole list, and the park the owner was actually in gets all
            // three — the cross-product over eleven parks is a four-minute test and proves nothing the one
            // room does not.
            foreach (DeckReachability.Point from in body == "luna" ? stands : stands.Take(1))
            {
                foreach ((double gx, double gy, string what) in targets)
                {
                    clicks++;
                    if (Click(deck, from, gx, gy).Route is null && bad.Count < 400)
                    {
                        bad.Add($"  {body} B{-level}: from ({from.X:F1}, {from.Y:F1}), a click on {what} at "
                            + $"({gx:F1}, {gy:F1}) planned nothing.");
                    }
                }
            }
        }

        Assert.True(parks >= 10, $"only {parks} parks were clicked — this proved little.");
        Assert.True(clicks > 5000, $"only {clicks} clicks were planned — this proved little.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} of {clicks} clicks in a park planned no route at all — the captain clicks and "
            + $"nothing happens:\n{string.Join("\n", bad.Take(12))}");
    }

    /// <summary>
    /// THE SECOND LOOK IS WIDE ENOUGH FOR THIS BUILDING'S OWN BEDS — measured off the geometry, not off the
    /// paragraph that argues for it.
    ///
    /// <para><see cref="AutoWalk.PointingFallbackDu"/> is derived in its own summary from a bed's half-depth
    /// plus a body's clearance plus a lattice step. That derivation is arithmetic in a comment, which is
    /// exactly the thing this repo has been bitten by: the day somebody makes the beds deeper, the number
    /// stays 5.0 and the park stops taking a click again, silently, with every test green. So the worst case
    /// is MEASURED here and the constant has to cover it.</para>
    ///
    /// <para><b>OUTSIDE the bed is the whole of the measurement</b>, and it was the whole of it before the
    /// bed was solid too. When this was written a bed was four rails round a pocket a body fits in perfectly
    /// well and nobody on the floor can get into, so "the nearest standable square" to a bed's middle was
    /// the bed's own middle and a guard that asked THAT question would have measured 0.2 du and proved
    /// nothing. #874 filled the pocket in, and the answer this file wants did not move by a millimetre —
    /// because what a click needs has always been the nearest square OUT OF the thing, which is the only
    /// ground the walk can actually settle on. See <see cref="AutoWalk.PointingFallbackDu"/> for the run
    /// that measured that.</para>
    /// </summary>
    [Fact]
    public void TheSecondLookCoversTheDeepestThingThisBuildingPlants()
    {
        double worst = 0;
        string worstAt = "";
        int beds = 0;

        foreach ((string body, int level, UndergroundComplex.Park park) in EveryPark())
        {
            DeckPlan deck = FloorOf(body, level);
            foreach (UndergroundComplex.GrowingBed bed in park.Beds)
            {
                beds++;

                // The middle of the bed is the furthest into it a finger can land, so it is the worst case
                // by construction — no need to walk the whole box.
                double near = double.MaxValue;
                for (double r = 0.25; r <= 12.0 && near == double.MaxValue; r += 0.25)
                {
                    for (int a = 0; a < 64; a++)
                    {
                        double th = a * Math.PI / 32.0;
                        double cx = bed.X + (r * Math.Cos(th)), cy = bed.Y + (r * Math.Sin(th));
                        if (!bed.Contains(cx, cy)
                            && DeckReachability.Standable(cx, cy, DeckPlan.AvatarRadius, deck.CollisionField))
                        {
                            near = r;
                            break;
                        }
                    }
                }

                if (near > worst)
                {
                    worst = near;
                    worstAt = $"{body} B{-level}'s bed {bed.Number} at ({bed.X:F1}, {bed.Y:F1})";
                }
            }
        }

        Assert.True(beds >= 90, $"only {beds} growing beds were measured — this proved little.");
        Assert.True(worst > 3.0,
            $"the deepest bed in the building is only {worst:F1} du from ground outside it — the beds have "
            + "shrunk to nothing and this guard is no longer measuring the case it exists for.");

        // …plus a lattice step, because what the walk settles for is a SQUARE of the grid: the nearest one
        // out of the bed can sit a step further out than the nearest POINT does, and four of this park's
        // nine beds are exactly that case.
        Assert.True(AutoWalk.PointingFallbackDu >= worst + DeckReachability.DefaultStep,
            $"a finger that lands in the middle of {worstAt} is {worst:F1} du from anywhere out of it a body "
            + $"could stand, the grid it settles on is {DeckReachability.DefaultStep:F1} du coarse, and the "
            + $"second look only goes {AutoWalk.PointingFallbackDu:F1} du. That click plans nothing.");
    }

    /// <summary>A pointed-at goal still ends inside the [E] ring. The reach is what a click SETTLES for, and
    /// the whole automation dividend of #729 is that the walk stops near enough to press the thing — so the
    /// day the reach grows past <see cref="DeckPlan.InteractRadius"/>, arrival stops meaning arrival.</summary>
    [Fact]
    public void WhatAClickSettlesForIsStillInsideTheInteractRing()
    {
        Assert.True(AutoWalk.PointingReachDu < DeckPlan.InteractRadius,
            $"a click now settles for {AutoWalk.PointingReachDu:F1} du off what was pointed at, and [E] only "
            + $"answers within {DeckPlan.InteractRadius:F1} du. Walking to a console would stop out of reach "
            + "of it.");
    }

    // ── (b) AND WHATEVER HAPPENS, THE CAPTAIN IS TOLD ─────────────────────────────────────────────────

    /// <summary>
    /// A PLAN THAT FINDS NOTHING ALWAYS CARRIES THE REASON, AND A PLAN THAT FINDS A ROUTE NEVER DOES.
    ///
    /// <para>Both halves, on the same real floor, in the same breath. The refusing half is the #603/#825
    /// class ("a control that does nothing and says nothing is indistinguishable from a broken control"),
    /// and the succeeding half is what stops the fix for it being a client that chatters at every click.</para>
    ///
    /// <para>The reach and its second look are passed here, because a fix that made the refusal unreachable
    /// on a real floor would have made this assertion vacuous — the sealed point below is outside the
    /// building's own hull, which no reach in this class can walk to.</para>
    /// </summary>
    [Fact]
    public void APlanWithNoRouteAlwaysSaysWhyAndAPlanWithOneSaysNothing()
    {
        DeckPlan deck = FloorOf("luna", -1);
        UndergroundComplex.Park park = UndergroundComplex.Build("luna", -1, Field).Park!.Value;
        var from = new DeckReachability.Point(park.X, park.Y);

        AutoWalk.Attempt walked = Click(deck, from, park.Walk[park.Walk.Count / 2].X,
            park.Walk[park.Walk.Count / 2].Y);
        Assert.True(walked.Route is not null,
            "the park's own gravel planned no route — the silent half below would prove nothing.");
        Assert.Null(walked.Refusal);

        // Well outside the field's own walls: a place no corridor reaches and no reach in this class can
        // settle for. The refusal is load-bearing and must survive the fix.
        AutoWalk.Attempt nowhere = Click(deck, from, Field.RightX + 60, Field.BottomY - 60);
        Assert.Null(nowhere.Route);
        Assert.Equal(AutoWalk.RefusalLine, nowhere.Refusal);
        Assert.False(string.IsNullOrWhiteSpace(nowhere.Refusal));
    }

    /// <summary>
    /// AND THE CLIENT SAYS IT — once, unconditionally, on the refusing branch and nowhere else.
    ///
    /// <para>The behaviour above is Core's contract; this is the wiring, which is the half that is actually
    /// forgotten (the same reason <c>TheParkIsWalkableTests</c> scans for the attendance note's latch). Read
    /// off the shipping source of <c>Map.Deck.ClickToWalkAt</c>: exactly one line is said when the plan comes
    /// back empty, it is said whatever Core put in the <c>Refusal</c> field, Core's own canonical sentence is
    /// the floor under it, and the branch that DOES set off walking says nothing at all.</para>
    ///
    /// <para><b>Proven RED</b> by restoring the conditional the handler shipped with
    /// (<c>if (attempt.Refusal is { } line) { ShowPulseMessage(line); }</c>):</para>
    /// <code>
    /// the refusal is spoken only when Core happened to fill one in. The day any path through the planner
    /// comes back empty-handed and wordless, this branch goes silent — which is the whole of #866.
    /// </code>
    ///
    /// <para>…and RED again by deleting the call outright, which is what a click that plans nothing looked
    /// like to the owner:</para>
    /// <code>
    /// ClickToWalkAt says NOTHING when the plan comes back with no route. A control that does nothing and
    /// says nothing is indistinguishable from a broken one (#603, #825, #866).
    /// </code>
    /// </summary>
    [Fact]
    public void TheClickHandlerSaysWhyOnceWhenItPlansNothingAndNothingWhenItWalks()
    {
        string source = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Deck.cs"));

        int at = source.IndexOf("private void ClickToWalkAt(", StringComparison.Ordinal);
        Assert.True(at > 0, "Map.Deck.cs no longer has a ClickToWalkAt — this guard is reading nothing.");
        int end = source.IndexOf("\n    private ", at + 10, StringComparison.Ordinal);
        string method = source[at..(end < 0 ? source.Length : end)];

        // The refusing branch, from the test for an empty route to the return that ends it.
        int branch = method.IndexOf("attempt.Route is null", StringComparison.Ordinal);
        Assert.True(branch > 0, "the click handler no longer tests the plan for an empty route.");
        int walks = method.IndexOf("_autoWalk = attempt.Route", StringComparison.Ordinal);
        Assert.True(walks > branch, "the click handler no longer sets off walking after the refusal branch.");

        string refusing = method[branch..walks];
        string walking = method[walks..];

        Assert.True(Regex.Matches(refusing, @"ShowPulseMessage\(").Count == 1,
            $"ClickToWalkAt says {Regex.Matches(refusing, @"ShowPulseMessage\(").Count} things when the plan "
            + "comes back with no route. A control that does nothing and says nothing is indistinguishable "
            + "from a broken one (#603, #825, #866) — and one that says two things is a stutter.");

        Assert.True(refusing.Contains("ShowPulseMessage(attempt.Refusal ?? AutoWalk.RefusalLine)",
                StringComparison.Ordinal),
            "the refusal is spoken only when Core happened to fill one in. The day any path through the "
            + "planner comes back empty-handed and wordless, this branch goes silent — which is the whole "
            + "of #866. Core's canonical line is the floor under it: "
            + "ShowPulseMessage(attempt.Refusal ?? AutoWalk.RefusalLine).");

        Assert.True(!walking.Contains("ShowPulseMessage(", StringComparison.Ordinal),
            "the click handler says something on the branch that DOES set off walking. The say-why is for "
            + "the click that plans nothing; a line on every successful click is chatter.");

        // …and the sentence itself is Core's, not a copy typed into the client, so the guard above and the
        // pixels the owner reads are the same string.
        Assert.DoesNotContain("No way through from here", source, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }
}
