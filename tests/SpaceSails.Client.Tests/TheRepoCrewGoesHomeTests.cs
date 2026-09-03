using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #731 · <b>THE REPO CREW WALK BACK TO THEIR OWN BOAT</b> — driven on real regolith, through the shipping
/// surface frame, with the shipping busted panel doing the settling.
///
/// <h3>What was actually wrong, and it was not only a missing beat</h3>
///
/// <para>#731's third first customer is <i>"sweep teams / the insurance-job crew"</i>, and the hull half of
/// that shipped in #946: the team that boards an <c>InsuranceJob</c> wreck files out through its own lock.
/// The crew that lands ON you is the other one, and it never left at all. Nothing anywhere set
/// <c>CollectorsComing</c> back to false and nothing anywhere took a body off the ground — the only two
/// things that ever removed a repo crew from a moon were the captain lifting off and the captain dying.</para>
///
/// <para>So the bribe's own sentence — <i>"{callsign} logs a clean sweep and sheers off"</i> — was a lie of
/// this repository's third named class (the sim doing one thing while a sentence reports another). It calls
/// <c>RemoveHunter</c>, which searches the list of hunters in SPACE; a crew standing on regolith was never in
/// it. The card closed, the next frame found them still inside <c>CollectorLanding.ReachDu</c>, and the writ
/// was served again with a fresh seed and a fresh demand, for ever.</para>
///
/// <h3>The beat that replaces it, and the guards below</h3>
///
/// <para>They turn round from where they are standing, walk back on the SAME <see cref="NpcWalk"/> the sweep
/// team files out on, queue at their own hatch by the SAME <see cref="Egress.PlaceInTheFile"/> arithmetic one
/// <see cref="InspectionTeam.FileSpacingDu"/> apart, and go in one at a time. The captain is held off the
/// hatch while somebody is working it, exactly as a wreck's crew-only lock holds him. <b>And not one word is
/// said about any of it</b>, which the last guard proves by transcribing every frame of it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRepoCrewGoesHomeTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>One frame, at the clamp the surface step spends however long the browser was away
    /// (<c>MaxSurfaceStepSeconds</c>) — the game's own worst frame, driven at its own ceiling.</summary>
    private const double Frame = 0.1;

    /// <summary>How many frames the whole walk home is given before the guard calls it stuck. At the frame
    /// above and <see cref="NpcWalk.PaceDu"/> that is 600 du of walking, and the field is 300 across.</summary>
    private const int FrameCeiling = 3000;

    /// <summary>A moon with a shipped ground under it.</summary>
    private const string Body = "luna";

    // ── (a) THEY GO, AND THE WRIT IS NOT SERVED TWICE ───────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>THE BRIBE IS PAID AND THE CREW ACTUALLY LEAVE.</b> The whole scene, driven end to end on
    /// the shipping frame: the boat comes down, a hand goes on the carry loop, the captain pays, and the ✕
    /// closes the card. Then:
    ///
    /// <list type="number">
    /// <item><b>They are gone.</b> Every body is off the ground inside the ceiling, and
    /// <c>CollectorsComing</c> is false afterwards, so nothing lands again this excursion.</item>
    /// <item><b>And the writ is never served again.</b> <c>_busted</c> stays null for every frame of it,
    /// which is the bug: before this lane the very next frame re-opened the demand with a fresh seed,
    /// because the crew had not moved and <c>HasYou</c> was still true.</item>
    /// <item><b>One at a time.</b> Never two bodies off the deck on one frame — a hatch that swallowed a
    /// crew in one frame would be a despawn wearing a door.</item>
    /// </list>
    ///
    /// <para><b>RED</b> by reverting the go-home branch in <c>StepCollectors</c> (the crew resume the
    /// pursuit): row 2 fails on the first frame after the card closes.</para>
    /// </summary>
    [Fact]
    public void PAID_OffTheyWalkBackToTheirBoatAndTheWritIsNeverServedAgain()
    {
        Pages.Map map = OnTheGroundWithACrewOnYou();
        IList crew = TheCrew(map);
        Assert.True(crew.Count >= 2,
            $"the bench put {crew.Count} collector(s) on the ground; a FILE wants at least two, or the "
            + "single-file half of this lane is asserted against a world that cannot show it.");

        TheWritIsSettledAndThePanelClosed(map);
        Assert.True(GoingHome(map), "nothing told the crew their business here was done.");

        int spent = 0;
        int mostGoneOnOneFrame = 0;
        while (crew.Count > 0 && spent < FrameCeiling)
        {
            int before = crew.Count;
            Invoke(map, "StepCollectors", Frame);
            spent++;
            mostGoneOnOneFrame = Math.Max(mostGoneOnOneFrame, before - crew.Count);

            Assert.True(Get(map, "_busted") is null,
                string.Create(CultureInfo.InvariantCulture, $"the writ was served AGAIN on frame {spent}")
                + ", by a crew who had just been paid off and told the captain they were sheering off.");
        }

        Assert.True(crew.Count == 0,
            string.Create(CultureInfo.InvariantCulture,
                $"{crew.Count} of them are still standing on the regolith after {spent} frames")
            + " — a paid-off repo crew that never leaves is the scene this lane exists to end.");
        Assert.Equal(1, mostGoneOnOneFrame);
        Assert.False(Coming(map), "the boat is still 'coming' with nobody left to come.");
        Assert.False(Landed(map), "the boat is still down with nobody in it.");
    }

    // ── (b) THEIR LEGS BEGIN AT THEIR FEET ──────────────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>THE WALK HOME STARTS WHERE THEY ARE STANDING.</b> #1064 killed exactly this lie in the bar
    /// — a stranger who had refused your last offer was taken off the floor and re-planned from a cellar
    /// doorstep seven deck units away — and it may not come back on a moon.
    ///
    /// <para>Two clauses, both driven: the FIRST point of the very first route each body plans is the place
    /// that body was standing on when the card closed; and on no frame of the whole walk does anybody cover
    /// more ground than <see cref="NpcWalk.PaceDu"/> allows. The second is what makes the first mean
    /// something — a route that starts at your feet and then jumps is still a teleport.</para>
    ///
    /// <para><b>RED</b> by planning the route from the boat (<c>new DeckReachability.Point(ex.CollectorBoatX,
    /// ex.CollectorBoatY)</c>) instead of from the body.</para>
    /// </summary>
    [Fact]
    public void THEIR_LegsBeginAtTheirFeetAndNoFrameJumps()
    {
        Pages.Map map = OnTheGroundWithACrewOnYou();
        IList crew = TheCrew(map);
        TheWritIsSettledAndThePanelClosed(map);

        var stoodAt = new List<(double X, double Y)>();
        foreach (object? c in crew)
        {
            stoodAt.Add((XOf(c!), YOf(c!)));
        }

        // One frame: every body plans its way home on the frame the scene ends, never a frame later. A body
        // that is leaving and has no route for a frame is a body the deck could draw taking a step it never
        // planned (the sweep team paid for this line already).
        Invoke(map, "StepCollectors", Frame);

        int routesChecked = 0;
        for (int i = 0; i < crew.Count; i++)
        {
            NpcWalk? walk = WalkOf(crew[i]!);
            Assert.True(walk is not null,
                $"collector {i} is going home with no route at all — that is a despawn wearing a hatch.");
            IReadOnlyList<DeckReachability.Point> route = walk!.Route;
            Assert.True(route.Count > 0, $"collector {i}'s route home is empty.");

            double away = Distance(route[0].X, route[0].Y, stoodAt[i].X, stoodAt[i].Y);
            Assert.True(away <= DeckPlan.AvatarRadius,
                string.Create(CultureInfo.InvariantCulture,
                    $"collector {i}'s walk home begins {away:F2} du from where they were standing (({stoodAt[i].X:F2},{stoodAt[i].Y:F2}) → ({route[0].X:F2},{route[0].Y:F2}))")
                + ". Legs begin at feet; anything else is the vanish-and-reappear #1064 killed in the bar.");
            routesChecked++;
        }

        Assert.True(routesChecked >= 2, "fewer than two routes were examined — this guard asserts nothing.");

        // …and nothing jumps for the rest of it. The budget is one frame of the walker's own pace plus a
        // body radius of slack for the sub-stepped slide along a wall.
        double ceiling = (NpcWalk.PaceDu * Frame) + DeckPlan.AvatarRadius;
        var were = new List<(double X, double Y)>();
        foreach (object? c in crew)
        {
            were.Add((XOf(c!), YOf(c!)));
        }

        int spent = 0;
        while (crew.Count > 0 && spent < FrameCeiling)
        {
            int before = crew.Count;
            Invoke(map, "StepCollectors", Frame);
            spent++;

            if (crew.Count == before)
            {
                for (int i = 0; i < crew.Count; i++)
                {
                    double moved = Distance(XOf(crew[i]!), YOf(crew[i]!), were[i].X, were[i].Y);
                    Assert.True(moved <= ceiling, string.Create(CultureInfo.InvariantCulture,
                        $"collector {i} covered {moved:F2} du on one frame, and a walk at "
                        + $"{NpcWalk.PaceDu:F1} du/s may cover {ceiling:F2}. That is a teleport."));
                }
            }

            were.Clear();
            foreach (object? c in crew)
            {
                were.Add((XOf(c!), YOf(c!)));
            }
        }

        Assert.Empty(crew);
    }

    // ── (c) SINGLE FILE, AT THEIR OWN HATCH ─────────────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>A QUEUE, NOT A CROWD AT ONE DOOR.</b> Owner's word for this exit is <i>single file</i>, and
    /// the arithmetic that makes it one is Core's, shared with the hull sweep team's own lock file
    /// (<see cref="Egress.PlaceInTheFile"/>) rather than copied.
    ///
    /// <para>On every frame where more than one of them is still on the ground, the places they are walking
    /// to are distinct, they lie on the boat's own queue line, and consecutive ranks are exactly
    /// <see cref="InspectionTeam.FileSpacingDu"/> apart. And when the head goes through, the file steps
    /// FORWARD — the one behind re-plans to the vacated place rather than standing where it was.</para>
    ///
    /// <para><b>RED</b> by handing every rank the same goal (rank 0's place for everybody): the spacing
    /// clause fails on the first frame two of them are afoot.</para>
    /// </summary>
    [Fact]
    public void SINGLE_FileAtTheirOwnHatchAndTheFileStepsForward()
    {
        Pages.Map map = OnTheGroundWithACrewOnYou();
        IList crew = TheCrew(map);
        TheWritIsSettledAndThePanelClosed(map);
        (double hatchX, double hatchY) = TheHatch(map);

        int filesSeen = 0;
        int steppedForward = 0;
        (double X, double Y)? theStaleGoalOfTheSurvivor = null;
        int spent = 0;

        while (crew.Count > 0 && spent < FrameCeiling)
        {
            int before = crew.Count;
            Invoke(map, "StepCollectors", Frame);
            spent++;
            bool headWentThrough = crew.Count < before;

            var goals = new List<(double X, double Y)>();
            for (int i = 0; i < crew.Count; i++)
            {
                if (WalkOf(crew[i]!) is { } walk)
                {
                    goals.Add((walk.For.X, walk.For.Y));
                }
            }

            // The frame the head goes through is the one frame a survivor's route is honestly stale: the file
            // re-plans on the NEXT frame, on the floor, rather than being nudged sideways here. So the
            // invariant is asked of settled frames, and the step forward is asked of the frame after.
            if (!headWentThrough && goals.Count == crew.Count && goals.Count > 0)
            {
                for (int rank = 0; rank < goals.Count; rank++)
                {
                    (double wantX, double wantY) = Egress.PlaceInTheFile(
                        hatchX, hatchY, 0, -1, rank, InspectionTeam.FileSpacingDu);
                    Assert.True(Distance(goals[rank].X, goals[rank].Y, wantX, wantY) < 1e-9,
                        string.Create(CultureInfo.InvariantCulture,
                            $"rank {rank} is walking to ({goals[rank].X:F2},{goals[rank].Y:F2}) and the file's "
                            + $"own place for that rank is ({wantX:F2},{wantY:F2})."));
                    if (rank > 0)
                    {
                        double gap = Distance(
                            goals[rank].X, goals[rank].Y, goals[rank - 1].X, goals[rank - 1].Y);
                        Assert.True(Math.Abs(gap - InspectionTeam.FileSpacingDu) < 1e-9,
                            string.Create(CultureInfo.InvariantCulture,
                                $"two bodies in the file stand {gap:F2} du apart and the law says "
                                + $"{InspectionTeam.FileSpacingDu:F2} — that is a crowd, not a queue."));
                    }
                }

                if (goals.Count >= 2)
                {
                    filesSeen++;
                }

                if (theStaleGoalOfTheSurvivor is { } was)
                {
                    Assert.True(Distance(goals[0].X, goals[0].Y, was.X, was.Y) > 1e-9,
                        "the head went through the hatch and the next one did not step forward — a queue "
                        + "whose survivors stand still is three people who happen to be facing a door.");
                    steppedForward++;
                    theStaleGoalOfTheSurvivor = null;
                }
            }

            if (headWentThrough && goals.Count > 0)
            {
                theStaleGoalOfTheSurvivor = goals[0];
            }
        }

        Assert.True(filesSeen > 0, "two of them were never in the queue together — this guard asserts nothing.");
        Assert.True(steppedForward > 0, "the file never stepped forward — this guard asserts nothing.");
        Assert.Empty(crew);
    }

    // ── (d) THE HATCH DOES NOT OPEN FOR THE CAPTAIN ─────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>IT IS THEIR BOAT.</b> The issue's own law: <i>the door opens for the NPC by their own
    /// authority exactly where the plate says the captain's TRY would fail, and no line of dialog may explain
    /// it.</i> A wreck says it with <c>WreckLayout.HeldAtLock</c>; a boat says it with
    /// <see cref="CollectorLanding.HeldOffTheirHatch"/>, and neither says a word.
    ///
    /// <para>Driven by walking the captain ONTO the hatch every frame and watching the game put him back off
    /// it: while somebody is working it he is never nearer than a body plus a standoff. And the hold is
    /// bounded — it is gone the moment the last of them is through, because a permanent no-go bubble beside
    /// the way home is furniture, and furniture in the walk home kills captains on air.</para>
    ///
    /// <para><b>RED</b> by removing the <c>HeldOffTheirHatch</c> call: the captain stands on the hatch at
    /// 0.00 du while the crew work it.</para>
    /// </summary>
    [Fact]
    public void THE_HatchIsTheirsAndTheCaptainIsHeldOffItOnlyWhileItIsWorked()
    {
        Pages.Map map = OnTheGroundWithACrewOnYou();
        IList crew = TheCrew(map);
        TheWritIsSettledAndThePanelClosed(map);
        (double hatchX, double hatchY) = TheHatch(map);

        double keepOut = DeckPlan.AvatarRadius + Egress.DoorStandoffDu;
        int heldFrames = 0;
        int spent = 0;

        while (crew.Count > 0 && spent < FrameCeiling)
        {
            // Standing in their way, every frame, on purpose.
            Set(map, "_avatarX", hatchX);
            Set(map, "_avatarY", hatchY);

            Invoke(map, "StepCollectors", Frame);
            spent++;

            bool working = crew.Count > 0 && AtTheHatchOf(crew[0]!) > 0;
            double off = Distance(
                (double)Get(map, "_avatarX")!, (double)Get(map, "_avatarY")!, hatchX, hatchY);
            if (working)
            {
                heldFrames++;
                Assert.True(off >= keepOut - 1e-9, string.Create(CultureInfo.InvariantCulture,
                    $"the captain is {off:F2} du off a hatch somebody else is working, and the law is "
                    + $"{keepOut:F2}. That leaf opens on their authority and not on his."));
            }
        }

        Assert.True(heldFrames > 0, "nobody ever worked the hatch — this guard asserts nothing.");

        // …and once they are gone it is regolith again. The captain walks onto the spot and stays there.
        Set(map, "_avatarX", hatchX);
        Set(map, "_avatarY", hatchY);
        Invoke(map, "StepCollectors", Frame);
        Assert.Equal(hatchX, (double)Get(map, "_avatarX")!, 9);
        Assert.Equal(hatchY, (double)Get(map, "_avatarY")!, 9);
    }

    // ── (e) NOT ONE WORD ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>NOTHING EXPLAINS THE HATCH.</b> <i>"An NPC exiting through a door and that door refusing the
    /// captain ten seconds later is the whole beat, and no line of dialog may explain it."</i>
    ///
    /// <para>Everything the game puts in front of the player — the pulse's own words, the centred card and
    /// the story beat — transcribed on every frame of the walk home and compared. One string, or the beat is
    /// being narrated.</para>
    ///
    /// <para><b>RED</b> by pulsing anything at all when the last body goes through the hatch.</para>
    /// </summary>
    [Fact]
    public void NOT_OneWordIsSaidWhileTheyWalkHome()
    {
        Pages.Map map = OnTheGroundWithACrewOnYou();
        IList crew = TheCrew(map);
        TheWritIsSettledAndThePanelClosed(map);

        var said = new List<string>();
        int spent = 0;
        while (crew.Count > 0 && spent < FrameCeiling)
        {
            Invoke(map, "StepCollectors", Frame);
            spent++;
            said.Add(WhatIsOnTheScreen(map));
        }

        // …including the frame after the last of them is gone, which is where a "they lifted off" line would
        // be if anybody were tempted to write one.
        Invoke(map, "StepCollectors", Frame);
        said.Add(WhatIsOnTheScreen(map));

        Assert.True(said.Count > 10, "the walk home was too short to say anything during — asserts nothing.");
        Assert.True(said.Distinct().Count() == 1,
            string.Create(CultureInfo.InvariantCulture,
                $"the game said {said.Distinct().Count()} different things while a repo crew walked back to their own boat")
            + ", and it is supposed to say nothing at all:\n  "
            + string.Join("\n  ", said.Distinct().Take(6)));
    }

    /// <summary>
    /// #731 · <b>THE ID THE GAME MINTS IS THE ID THE CODE LOOKS FOR.</b> The positive half of
    /// <c>CollectorLanding.IsAGroundCrew</c>, asked of a REAL encounter opened by the shipping writ rather
    /// than of a string this test built out of the same constant — which would be true by construction, and a
    /// guard that cannot tell pass from fail is a named bug class here.
    ///
    /// <para>This is the seam the whole lane hangs off: <c>RemoveHunter</c> recognises a ground crew by this
    /// id, and for months the encounter was filed under one spelling while the code that ends an encounter
    /// looked for hunters in another list entirely.</para>
    ///
    /// <para><b>RED</b> by changing the prefix on one side only (the constant is quoted at both ends now, so
    /// the RED has to be typed into <c>TheWritIsServed</c>): the crew are never told their business is done.</para>
    /// </summary>
    [Fact]
    public void THE_IdTheGameMintsIsTheIdTheCodeLooksFor()
    {
        Pages.Map map = OnTheGroundWithACrewOnYou();
        object busted = Get(map, "_busted")!;
        var id = (string)busted.GetType().GetProperty("HunterId", Hidden)!.GetValue(busted)!;

        Assert.True(CollectorLanding.IsAGroundCrew(id),
            $"the writ was filed under `{id}` and the code that ends an encounter does not recognise it as a "
            + "crew on the ground — so nothing will ever tell them to go home.");

        TheWritIsSettledAndThePanelClosed(map);
        Assert.True(GoingHome(map), "the id was recognised and the crew were still not told to go home.");
    }

    // ── (f) THE SAME EVERY TIME ─────────────────────────────────────────────────────────────────────

    /// <summary>#731 · Deterministic, like everything else on this lane: two identical grounds, the same
    /// frames, the same trace to the last decimal. A walk home that rolled a die would be a scene the frozen
    /// watch could not draw twice.</summary>
    [Fact]
    public void THE_WalkHomeIsTheSameEveryTime()
    {
        Assert.Equal(TraceOfOneWalkHome(), TraceOfOneWalkHome());
    }

    private static string TraceOfOneWalkHome()
    {
        Pages.Map map = OnTheGroundWithACrewOnYou();
        IList crew = TheCrew(map);
        TheWritIsSettledAndThePanelClosed(map);

        var trace = new List<string>();
        int spent = 0;
        while (crew.Count > 0 && spent < FrameCeiling)
        {
            Invoke(map, "StepCollectors", Frame);
            spent++;
            foreach (object? c in crew)
            {
                trace.Add(string.Create(CultureInfo.InvariantCulture,
                    $"{spent}:{XOf(c!):F6},{YOf(c!):F6}"));
            }
        }

        Assert.True(trace.Count > 20, "the trace is too short to tell two runs apart — asserts nothing.");
        return string.Join("|", trace);
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component standing on real regolith with a repo boat down and its crew on the
    /// captain's carry loop — everything through the shipping calls, nothing planted.</summary>
    private static Pages.Map OnTheGroundWithACrewOnYou()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, "Luna", "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);          // regolith, never a Hive floor
        exType.GetProperty("CollectorCallsign")!.SetValue(ex, "BAILIFF");
        exType.GetProperty("CollectorsComing")!.SetValue(ex, true);
        exType.GetProperty("CollectorsEtaSeconds")!.SetValue(ex, 0.0);
        exType.GetProperty("CollectorShelterNoted")!.SetValue(ex, true);  // the siege plate is not this lane's
        exType.GetProperty("CollectorsHailed")!.SetValue(ex, true);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_credits", 10_000_000);
        Set(map, "_heat", new HeatState(3, 0.0));  // a party of two: a file wants somebody to queue behind
        Invoke(map, "RebuildSurfaceDeck");
        Assert.False((bool)Get(map, "OnWreck")!, "the bench is aboard a wreck; the collectors only land on ground.");

        // The boat comes down through its own shipping call, at its own seeded place.
        Invoke(map, "StepCollectors", Frame);
        IList crew = TheCrew(map);
        Assert.True(crew.Count > 0, "no repo crew got out of the boat — the bench has nothing to drive.");

        // …and the captain walks into them, which is the only way a writ is ever served on foot. Parked one
        // reach below the file so the pursuit closes in a frame or two rather than crossing the field.
        Set(map, "_avatarX", XOf(crew[0]!));
        Set(map, "_avatarY", YOf(crew[0]!) - (CollectorLanding.ReachDu / 2));

        int spent = 0;
        while (Get(map, "_busted") is null && spent < FrameCeiling)
        {
            Invoke(map, "StepCollectors", Frame);
            spent++;
        }

        Assert.True(Get(map, "_busted") is not null,
            "the writ was never served, so there is no encounter for the crew to be finished with.");
        return map;
    }

    /// <summary>Pay them and shut the card — the shipping bribe and the shipping dismiss, so what this bench
    /// proves is what a captain's two presses actually do.</summary>
    private static void TheWritIsSettledAndThePanelClosed(Pages.Map map)
    {
        Invoke(map, "BustedBribe");
        Invoke(map, "CloseBusted");
        Assert.True(Get(map, "_busted") is null, "the card did not close.");
    }

    /// <summary>Everything the game is putting in front of the player, as one line — the same three surfaces
    /// the sweep team's own canon guard transcribes.</summary>
    private static string WhatIsOnTheScreen(Pages.Map map)
    {
        var pulse = (PulseSlot)Get(map, "_pulse")!;
        object? view = Get(map, "_viewObject");
        object? story = Get(map, "_storyCard");
        string card = view is DeckPlan.ConsoleSpot spot
            ? $"{spot.Label}/{spot.Caption}/{spot.Outcome}"
            : "-";
        return $"{pulse.Message ?? "-"}|{card}|{story?.ToString() ?? "-"}";
    }

    private static (double X, double Y) TheHatch(Pages.Map map)
    {
        object ex = Get(map, "_surface")!;
        return ((double)ex.GetType().GetProperty("CollectorBoatX")!.GetValue(ex)!,
                (double)ex.GetType().GetProperty("CollectorBoatY")!.GetValue(ex)!);
    }

    private static bool GoingHome(Pages.Map map) => ExFlag(map, "CollectorsGoingHome");

    private static bool Coming(Pages.Map map) => ExFlag(map, "CollectorsComing");

    private static bool Landed(Pages.Map map) => ExFlag(map, "CollectorsLanded");

    private static bool ExFlag(Pages.Map map, string name)
    {
        object ex = Get(map, "_surface")!;
        return (bool)ex.GetType().GetProperty(name)!.GetValue(ex)!;
    }

    private static IList TheCrew(Pages.Map map) =>
        (IList)typeof(Pages.Map).GetField("_collectors", Hidden)!.GetValue(map)!;

    private static double XOf(object c) => (double)c.GetType().GetField("X", Hidden)!.GetValue(c)!;

    private static double YOf(object c) => (double)c.GetType().GetField("Y", Hidden)!.GetValue(c)!;

    private static double AtTheHatchOf(object c) =>
        (double)c.GetType().GetField("AtTheHatch", Hidden)!.GetValue(c)!;

    private static NpcWalk? WalkOf(object c) =>
        (NpcWalk?)c.GetType().GetField("Walk", Hidden)!.GetValue(c);

    private static double Distance(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static object? Get(object o, string name)
    {
        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        Assert.True(prop is not null, $"the component has no `{name}` — this guard is reading a dead name.");
        return prop!.GetValue(o);
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static void Invoke(object o, string method, params object?[] args) =>
        o.GetType().GetMethod(method, Hidden)!.Invoke(o, args);
}
