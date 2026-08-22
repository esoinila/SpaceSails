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
/// #731 v2 · THE SWEEP TEAM WALKS OUT THROUGH THE AIRLOCK — driven on a real hull, over the shipping frame.
///
/// <para>#731's third first customer, and the free dread upgrade. Until this lane the team did not leave
/// abstractly: <b>it did not leave at all.</b> It was spawned, it walked its route forever, and it stopped
/// existing only because the next boarding cleared the list — so the thing that decides whether the captain
/// is in trouble simply never resolved. Now they finish the hull, queue at their own lock, and file through
/// it one at a time, unhurried, and are gone. <b>Nothing is said about any of it.</b></para>
///
/// <para>The door in this beat is not a plate. A wreck has no <c>UndergroundComplex.LockedDoor</c> and
/// nobody has ever painted a sign on its shuttle bulkhead; what it has is
/// <see cref="WreckLayout.ShuttleLockX"/> and the crew-only rule stated as two functions in Core. So this
/// guard asks the LAW rather than a string: the file stands on the lock's own standoff line, and the captain
/// is <see cref="WreckLayout.HeldAtLock"/> while they hold it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheSweepTeamWalksOutTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>One frame, at the clamp the sweep loop spends however long the browser was away.</summary>
    private const double Frame = 0.1;

    /// <summary>How many frames the whole sweep-and-leave is given. At this frame that is fifty minutes of
    /// ship time, which is many laps of a hull two hundred du long.</summary>
    private const int FrameCeiling = 30000;

    // ── (a) THEY FINISH, THEY QUEUE, THEY GO ────────────────────────────────────────────────────────

    /// <summary>
    /// #731 v2 · <b>THEY WALK OUT ON REAL LEGS, SINGLE FILE, AND THEN THEY ARE GONE.</b>
    ///
    /// <para>The whole beat, driven on a real hull with the shipping sweep frame and a captain hidden well
    /// off their axis so nothing else in the scene ever fires:</para>
    ///
    /// <list type="number">
    /// <item><b>They leave at all.</b> Every one of the three reaches <c>Awareness.Leaving</c> — the state
    /// this scene has never had — and every one of them is eventually off the deck.</item>
    /// <item><b>On real legs.</b> While they are leaving, each carries an <see cref="NpcWalk"/> whose route
    /// is more than one point (a one-point route is a position handed over, not a walk), and no sweeper is
    /// ever inside the hull's own stone on any frame.</item>
    /// <item><b>Single file.</b> On the frames when more than one of them is leaving and none has arrived
    /// yet, their goals are distinct places on the lock's own line, one
    /// <see cref="InspectionTeam.FileSpacingDu"/> apart — a queue, not three people at one door.</item>
    /// <item><b>Unhurried.</b> The pace of the walk home is <see cref="NpcWalk.PaceDu"/>, which is slower
    /// than <see cref="InspectionTeam.SweepSpeed"/>. They are not in a hurry; they are finished.</item>
    /// <item><b>Through the lock, one at a time.</b> They come off the deck in the order they queued, and
    /// never two on one frame.</item>
    /// </list>
    ///
    /// <para><b>The RED case.</b> The behaviour this replaces — no <c>Leaving</c> at all, the team walking
    /// its route until the next boarding clears it. Row 1 goes red with three bodies still aboard after
    /// fifty minutes. Verbatim in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_SWEEP_TeamFilesOutThroughTheirOwnLockAndIsGone()
    {
        Pages.Map map = OnAHull();
        IList team = Sweepers(map);
        Assert.Equal(InspectionTeam.TeamSize, team.Count);

        // The captain, off the map entirely — this guard is about a team that never finds you, and the
        // first draft parked them inside the hull, where SWEEP-3 duly walked round a bulkhead and challenged
        // them. A guard whose scene is interrupted by the scene it is not about is a guard measuring the
        // wrong thing.
        Hidden_TheCaptainIsNowhereNear(map);

        DeckPlan plan = ThePlan(map);
        var everLeaving = new HashSet<string>(StringComparer.Ordinal);
        var wentThrough = new List<string>();
        int mostGoneOnOneFrame = 0;
        int filesSeen = 0;
        int realWalksHome = 0;
        int spent = 0;

        while (team.Count > 0 && spent < FrameCeiling)
        {
            var before = new List<string>();
            foreach (object? s in team)
            {
                before.Add(CallsignOf(s!));
            }

            Invoke(map, "AdvanceSweepTeam", Frame);
            spent++;

            var leaving = new List<object>();
            foreach (object? s in team)
            {
                Assert.False(plan.Collides(XOf(s!), YOf(s!)), string.Create(CultureInfo.InvariantCulture,
                    $"{CallsignOf(s!)} is inside the hull's own stone at ({XOf(s!):F2},{YOf(s!):F2}) on frame {spent}."));
                if (StateOf(s!) != InspectionTeam.Awareness.Leaving)
                {
                    continue;
                }
                everLeaving.Add(CallsignOf(s!));
                leaving.Add(s!);

                NpcWalk? walk = WalkOf(s!);
                Assert.True(walk is not null,
                    $"{CallsignOf(s!)} is leaving with no walk at all — a body that reaches an airlock "
                    + "without a route is a despawn wearing a door.");
                // A one-point route is honest for a body already standing on its place in the file — one of
                // them finishes its lap AT the lock — and is a teleport for anybody else. So the clause is
                // asked against how far there was to go, and the anti-vacuous half is counted below.
                double toGo = Math.Sqrt(
                    ((XOf(s!) - walk.For.X) * (XOf(s!) - walk.For.X))
                    + ((YOf(s!) - walk.For.Y) * (YOf(s!) - walk.For.Y)));
                Assert.True(walk.Route.Count > 1 || toGo < DeckReachability.DefaultStep * 4,
                    string.Create(CultureInfo.InvariantCulture,
                        $"{CallsignOf(s!)}'s route home is {walk.Route.Count} point(s) long with {toGo:F1} du still to go — that is a position handed over, not a walk."));
                if (walk.Route.Count > 1)
                {
                    realWalksHome++;
                }
                Assert.Equal(NpcWalk.PaceDu, walk.Pace, 9);
            }

            // SINGLE FILE: while more than one of them is still walking home, they are queued on the lock's
            // own line, one spacing apart, and no two of them want the same place.
            var goals = leaving.Where(s => WalkOf(s)!.Afoot).Select(s => WalkOf(s)!.For.X).ToList();
            if (goals.Count > 1)
            {
                filesSeen++;
                Assert.Equal(goals.Count, goals.Distinct().Count());
                foreach (double gx in goals)
                {
                    double back = WreckLayout.ShuttleLockX - Egress.DoorStandoffDu - gx;
                    Assert.True(
                        back >= -1e-9
                        && Math.Abs((back / InspectionTeam.FileSpacingDu)
                            - Math.Round(back / InspectionTeam.FileSpacingDu)) < 1e-6,
                        string.Create(CultureInfo.InvariantCulture,
                            $"a leaver is bound for x={gx:F2}, which is {back:F2} du back from the lock's standoff line and not a whole number of file spacings. That is a huddle at a door, not a queue."));
                }
            }

            int wentThisFrame = 0;
            foreach (string who in before)
            {
                bool still = false;
                foreach (object? s in team)
                {
                    still |= CallsignOf(s!) == who;
                }
                if (!still)
                {
                    wentThrough.Add(who);
                    wentThisFrame++;
                }
            }
            mostGoneOnOneFrame = Math.Max(mostGoneOnOneFrame, wentThisFrame);
        }

        var stuck = new List<string>();
        foreach (object? s in team)
        {
            NpcWalk? w = WalkOf(s!);
            stuck.Add(string.Create(CultureInfo.InvariantCulture,
                $"{CallsignOf(s!)} {StateOf(s!)} at ({XOf(s!):F1},{YOf(s!):F1}) leg {LegOf(s!)}/{StartLegOf(s!)} laps {LapsOf(s!)} walk {(w is null ? "none" : $"{w.State} for x={w.For.X:F1} route {w.Route.Count}")}"));
        }

        Assert.True(spent < FrameCeiling,
            $"{team.Count} of {InspectionTeam.TeamSize} are still aboard after {FrameCeiling} frames: {string.Join("; ", stuck)}. The "
            + "sweep team is supposed to finish the hull and go home through their own lock — a team that "
            + "never leaves is the old behaviour, in which the only thing that ever removed them was the "
            + "NEXT boarding.");
        Assert.Equal(InspectionTeam.TeamSize, everLeaving.Count);
        Assert.Equal(InspectionTeam.TeamSize, wentThrough.Count);
        Assert.True(mostGoneOnOneFrame == 1,
            $"{mostGoneOnOneFrame} of them went through the hatch on one frame. Single file means one body "
            + "at a time, and a lock that swallows a team in a frame is a despawn wearing a door.");
        Assert.True(realWalksHome > 0,
            "not one of them planned a route home longer than a single point, so the walk clause is green "
            + "because nobody ever had anywhere to walk — a guard that asserts nothing.");
        Assert.True(filesSeen > 0,
            "there was never a frame with two of them walking home at once, so the single-file clause is "
            + "green because it never ran — a guard that asserts nothing.");
        Assert.True(NpcWalk.PaceDu < InspectionTeam.SweepSpeed,
            "the walk home is not slower than the sweep. Unhurried is the owner's own word for this beat.");
    }

    // ── (b) AND THE DOOR IS THEIRS WHILE THEY HOLD IT ───────────────────────────────────────────────

    /// <summary>
    /// #731 v2 · <b>THE LOCK REFUSES THE CAPTAIN WHILE THE TEAM IS FILING THROUGH IT.</b>
    ///
    /// <para>The crew-only rule already exists in Core and is already how the pack is kept off the shuttle
    /// (<see cref="WreckLayout.HeldAtLock"/> / <see cref="WreckLayout.PastTheLock"/>) — it lives there
    /// precisely so <i>"nothing uninvited reaches the shuttle"</i> is pinned by a test rather than by a
    /// comment. The away team on the far side of it ARE crew, and while they are working it the captain is
    /// not. Nothing says so: three professionals queueing at your way home is the sentence.</para>
    ///
    /// <para>Driven by parking the captain past the lock on every frame and asking where the frame leaves
    /// them. While anybody is <c>Leaving</c>, they are held; once the last of them is through, the way home
    /// opens again on the very next frame — a door that stayed shut afterwards would be a lane that took the
    /// way home away.</para>
    ///
    /// <para><b>The RED case.</b> Drop the hold. The captain walks out through the middle of the file.
    /// Verbatim in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_LOCK_IsTheirsWhileTheyAreFilingThroughIt()
    {
        Pages.Map map = OnAHull();
        IList team = Sweepers(map);

        double past = WreckLayout.ShuttleLockX + 2;
        bool everHeld = false;
        bool everLet = false;
        int spent = 0;

        while (spent < FrameCeiling)
        {
            // Past the lock on X — so the only question this frame asks is "may I go home" — and hard up
            // against the outboard wall on Y, where the lock bulkhead itself is between them and the file.
            // Nobody ever sees them, which is the scene this guard is about.
            Set(map, "_avatarX", past);
            Set(map, "_avatarY", (double)WreckLayout.BottomY);

            Invoke(map, "AdvanceSweepTeam", Frame);
            spent++;

            // Asked of the state the frame LEFT, not the state it started in: on the frame the last of them
            // steps through, the way home opens, and a guard that read the old answer would be asserting that
            // a door nobody is holding is still shut.
            bool holding = false;
            foreach (object? s in team)
            {
                holding |= StateOf(s!) == InspectionTeam.Awareness.Leaving;
            }

            double where = (double)Get(map, "_avatarX")!;
            if (holding)
            {
                everHeld = true;
                Assert.False(WreckLayout.PastTheLock(where, DeckPlan.AvatarRadius), string.Create(
                    CultureInfo.InvariantCulture,
                    $"the captain is at x={where:F2}, past the lock, on a frame when the away team is filing through it. The hatch is crew-keyed and they are the crew; the captain waits, and nothing tells them so."));
            }
            else if (team.Count == 0)
            {
                everLet = true;
                Assert.True(WreckLayout.PastTheLock(where, DeckPlan.AvatarRadius), string.Create(
                    CultureInfo.InvariantCulture,
                    $"the captain is held at x={where:F2} with nobody left aboard. The team took the way home with them, which is a lane that broke the excursion rather than one that added a beat."));
                break;
            }
        }

        Assert.True(everHeld,
            "nobody ever held the lock, so the row above is green because the question was never asked.");
        Assert.True(everLet, "the last of them never went through.");
    }

    // ── (c) AND NOT ONE WORD IS SAID ABOUT ANY OF IT ────────────────────────────────────────────────

    /// <summary>
    /// #731 v2 · <b>THE TEAM THAT NEVER FOUND YOU GOES HOME AND THE GAME DOES NOT MENTION IT.</b>
    ///
    /// <para>§13.8 in its purest form, and the whole of why this beat is a dread upgrade rather than a
    /// notification: the captain finds out they are alone again by looking, or does not find out at all. So
    /// everything the game puts in front of the player is transcribed on every frame from the first
    /// <c>Leaving</c> to the last body through the hatch, and it must not change once.</para>
    ///
    /// <para><b>The RED case.</b> Plant one line on the state change — the obvious one somebody reaches for,
    /// <c>"SWEEP-1: that's the hull. We're clear."</c> Verbatim in the pull request.</para>
    /// </summary>
    [Fact]
    public void NOTHING_IsSaidAboutTheTeamGoingHome()
    {
        Pages.Map map = OnAHull();
        IList team = Sweepers(map);
        Hidden_TheCaptainIsNowhereNear(map);

        var said = new List<string>();
        bool started = false;
        int spent = 0;

        while (team.Count > 0 && spent < FrameCeiling)
        {
            Invoke(map, "AdvanceSweepTeam", Frame);
            spent++;
            foreach (object? s in team)
            {
                started |= StateOf(s!) == InspectionTeam.Awareness.Leaving;
            }
            if (started)
            {
                said.Add(WhatIsOnTheScreen(map));
            }
        }

        Assert.True(said.Count > 0, "the team never started leaving — this guard asserts nothing.");
        Assert.True(said.Distinct().Count() == 1,
            $"the game said {said.Distinct().Count()} different things while the sweep team walked out "
            + "through their own airlock, and it is supposed to say nothing at all:\n  "
            + string.Join("\n  ", said.Distinct().Take(6)));
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component aboard a real derelict, with the shipping wreck deck under it and the
    /// shipping sweep team on it. Nothing else in the sim is running.</summary>
    private static Pages.Map OnAHull()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        var wreck = new Derelict.Wreck(
            "sweep-bench", "Bench Hull", Derelict.WreckCause.InsuranceJob, 250_000, 40.0);
        string bodyId = Derelict.BodyIdFor(wreck.Id);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(bodyId, wreck.ShipName, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        Set(map, "_wreck", wreck);
        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_weaponsTight", true);   // so the spawn's one warning line is not owed

        Invoke(map, "RebuildSurfaceDeck");
        Assert.True((bool)Get(map, "OnWreck")!, "the bench is not aboard a wreck.");

        Invoke(map, "SpawnSweepTeam", InspectionTeam.TeamSize);
        return map;
    }

    /// <summary>Everything the game is putting in front of the player, as one line — the pulse's own words,
    /// the centred card and the story beat, which are the three surfaces this beat could explain itself
    /// on.</summary>
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

    /// <summary>Park the captain where nothing aboard can see them. A lamp is 20 du long, so nowhere on the
    /// hull is far enough — the honest answer is off the map, which is the same idiom the deck's own empty
    /// figure slots use.</summary>
    private static void Hidden_TheCaptainIsNowhereNear(Pages.Map map)
    {
        Set(map, "_avatarX", -9999.0);
        Set(map, "_avatarY", -9999.0);
    }

    private static IList Sweepers(Pages.Map map) =>
        (IList)typeof(Pages.Map).GetField("_sweepers", Hidden)!.GetValue(map)!;

    private static string CallsignOf(object s) =>
        (string)s.GetType().GetProperty("Callsign", Hidden)!.GetValue(s)!;

    private static double XOf(object s) => (double)s.GetType().GetField("X", Hidden)!.GetValue(s)!;

    private static int LegOf(object s) => (int)s.GetType().GetField("RouteLeg", Hidden)!.GetValue(s)!;

    private static int StartLegOf(object s) => (int)s.GetType().GetField("StartLeg", Hidden)!.GetValue(s)!;

    private static int LapsOf(object s) => (int)s.GetType().GetField("Laps", Hidden)!.GetValue(s)!;

    private static double YOf(object s) => (double)s.GetType().GetField("Y", Hidden)!.GetValue(s)!;

    private static InspectionTeam.Awareness StateOf(object s) =>
        (InspectionTeam.Awareness)s.GetType().GetField("State", Hidden)!.GetValue(s)!;

    private static NpcWalk? WalkOf(object s) =>
        (NpcWalk?)s.GetType().GetField("Walk", Hidden)!.GetValue(s);

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

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

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(map, args);
    }
}
