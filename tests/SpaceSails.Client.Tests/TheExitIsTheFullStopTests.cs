using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #731 · THE EXIT IS THE FULL STOP, AND THE ARRIVAL IS THE COLD OPEN — driven on the room that ships.
///
/// <para><b>Owner, 2026-08-06:</b> <i>"The NPCs but not reevers could also use the A* if we want to show them
/// leaving a scene etc. If they go behind a door that is locked to us, we use that as 'I guess that concludes
/// the conversation' point in the plot / situation."</i> And: <i>"Like on the bar now they have to wait for us
/// to leave before they can sit up… or leave the bar."</i></para>
///
/// <para><b>And the other direction, the same day:</b> <i>"In the space bars there are lot of cases where we
/// can have npcs arrive at bar from locked place or go to a locked place. Now it is possible to have NPC ask
/// to sit down at our table and offer a quest! This is the classic TTRPG event."</i></para>
///
/// <h3>What is asked here rather than in Core</h3>
///
/// <para>Core's own <c>TheExitIsTheFullStopTests</c> walks the walker over the floor's stone and proves its
/// four laws. Two things can only be proved on this side, and both are the ones the owner would notice
/// first:</para>
///
/// <list type="number">
/// <item><b>SHE WALKS.</b> #865's rule is unchanged — somebody came to you, therefore the card — but the
/// sentence before it is new: she used to become true where she stood. So the guard drives the shipping wait
/// beat with #757's own approach cheat and demands a POSITION TRACE, over the shipping surface tick, before
/// the card comes up.</item>
/// <item><b>AND NOTHING IS SAID ABOUT IT.</b> <i>"no line of dialog may explain it"</i> is the issue's own
/// hardest sentence, and it is asked as a DIFFERENTIAL: one watch of this room is played twice, once with the
/// shift's departure dealt and once with it suppressed, and the two transcripts of everything the game put in
/// front of the player must be the same text.</item>
/// </list>
///
/// <para>The bench is <c>CoSeatingIsAStripTests</c>' — a real <see cref="Pages.Map"/> on a real generated
/// floor, seats taken with the shipping verbs, frames spent in the shipping <c>StepSurface</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheExitIsTheFullStopTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    /// <summary>One frame. A tenth of a second, which is exactly the longest step the walker band will take
    /// in one frame however long the browser was away — so this is the game's own worst frame, driven at its
    /// own ceiling, and never a bench-only clock.</summary>
    private const double Frame = 0.1;

    /// <summary>How many frames a walk across a hall is given before the guard calls it stuck. At the frame
    /// above and <see cref="NpcWalk.PaceDu"/> this is 800 du of walking — more than twice the width of the
    /// whole field, so a walk that hits it is going round in circles.</summary>
    private const int FrameCeiling = 4000;

    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor to sit down on.");

    // ── THE FLOOR ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component on a real Hive floor, with nothing running but the deck — the bench
    /// <c>MustStandUpBeforeWalkingTests</c> established and <c>CoSeatingIsAStripTests</c> uses.</summary>
    private static Pages.Map OnTheFloor()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on " +
                "has moved, and the seat verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, TheFloor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);

        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Spend the floor's own first-entry latches, so a card the ROOM owes cannot be mistaken for a
    /// card the walker raised. Every one of these is one-shot per excursion and none of them is #731's.
    /// </summary>
    private static void NothingPending(Pages.Map map)
    {
        object ex = Get(map, "_surface")!;
        ex.GetType().GetProperty("HiveCantinaHallShown")!.SetValue(ex, true);
        ex.GetType().GetProperty("HiveCabinetShown")!.SetValue(ex, true);
    }

    // ── (e) THE ONE WHO COMES TO YOUR TABLE WALKS IN ─────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>THE WALK IS THE CEREMONY.</b>
    ///
    /// <para>Owner: <i>"Now it is possible to have NPC ask to sit down at our table and offer a quest! This is
    /// the classic TTRPG event."</i> A door that has never opened for the captain opens for somebody, they
    /// cross the real floor on real legs, and they sit down opposite you — and the card is raised when they
    /// get there, not on the frame the beat was spent.</para>
    ///
    /// <para>#865's law is UNCHANGED and is re-asserted here so it cannot be lost in the rewiring: they came
    /// to you, therefore the card. What is new is everything in front of it:</para>
    ///
    /// <list type="number">
    /// <item>the beat produces a WALKER and not a seated stranger — the card is still down on that frame;</item>
    /// <item>the walk is bound for a door on this floor's LOCKED list, carrying that door's own plate;</item>
    /// <item>the position trace over the shipping surface tick is a real path — many distinct positions,
    /// several deck units of ground covered, and no frame inside stone;</item>
    /// <item>and only then: <c>TheyCameToYou</c>, the conversation frame, and #865's card.</item>
    /// </list>
    ///
    /// <para><b>The RED case.</b> Make <c>WalkSomebodyToYourTable</c> return false — the old behaviour, where
    /// she becomes true where she stands — and (1) and (3) go red with the card up on the press frame and no
    /// trace at all. The verbatim run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_ARRIVAL_WalksInAndTheCardWaitsForHer()
    {
        Pages.Map map = OnTheFloor();
        NothingPending(map);
        TakeAFreeTop(map);

        Assert.True((bool)Get(map, "SeatedIsDocked")!, "the sit did not dock in the first place.");

        // #757's own force-the-approach cheat: WHETHER somebody comes, never who or what.
        Set(map, "_approachCheat", (bool?)true);
        Invoke(map, "TableMove", SittingAlone.Wait);

        object ex = Get(map, "_surface")!;
        IList afoot = Walkers(ex);

        // (1) SOMEBODY IS ON THEIR FEET, and the card is not up yet.
        Assert.True(afoot.Count == 1,
            $"the wait beat put {afoot.Count} people on their feet. The arrival is supposed to be a WALK "
            + "across the hall from a door that does not open for the captain — a beat that produces nobody "
            + "walking is the old teleport wearing #731's name.");
        Assert.False((bool)Get(map, "SeatedIsAConversation")!,
            "the card came up on the frame the beat was spent — she is still at the door, and the card is "
            + "for a face that is not at the table yet.");

        NpcWalk walk = WalkOf(afoot[0]!);

        // (2) SHE CAME OUT OF A DOOR THAT DOES NOT OPEN FOR YOU — the provenance, carried on the walk as the
        // building's own plate, and matched back to this floor's LOCKED list rather than to a flag.
        Assert.True(walk.For.IsADoor,
            "her walk carries no door at all. The cold open IS the provenance — a door that has never opened "
            + "for the captain opened for somebody — and a walk that cannot name it has none.");
        IReadOnlyList<UndergroundComplex.LockedDoor> locked =
            UndergroundComplex.Build(Body, TheFloor, MoonSurface.ExpeditionField()).Locked;
        Assert.Contains(locked, l => string.Equals(l.Sign, walk.For.Sign, StringComparison.Ordinal));
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            Assert.False(
                SatchelTry.Offer(new Satchel.Item(kind, "test-item"), SatchelTry.Target.RoomDoor, Body).Worked,
                $"the captain's own {kind} opens `{walk.For.Sign}` — she came out of a door the captain "
                + "could have walked through, and the cold open is a corridor.");
        }
        Assert.True(walk.Plate == SittingAlone.VisitorPlate,
            $"the figure crossing the hall is `{walk.Plate}` and the person who sits down is "
            + $"`{SittingAlone.VisitorPlate}` — two people for one arrival.");
        Assert.True(walk.Route.Count > 1,
            $"her route is {walk.Route.Count} point(s) long — that is a position handed over, not a walk.");

        // (3) THE TRACE. The shipping surface tick, frame by frame, over the shipping collision.
        DeckPlan plan = ThePlan(map);
        var trace = new List<(double X, double Y)> { (walk.X, walk.Y) };
        int spent = 0;
        while (afoot.Count > 0 && spent < FrameCeiling)
        {
            Invoke(map, "StepSurface", Frame);
            spent++;
            if (afoot.Count > 0)
            {
                trace.Add((walk.X, walk.Y));
            }
            Assert.False(plan.Collides(walk.X, walk.Y), string.Create(CultureInfo.InvariantCulture,
                $"she is inside the room's own stone at ({walk.X:F2},{walk.Y:F2}) on frame {spent}."));
        }

        Assert.True(spent < FrameCeiling,
            $"she was still crossing the hall after {FrameCeiling} frames — a body grinding forever is a bug "
            + "wearing a feature's clothes. She is "
            + string.Create(CultureInfo.InvariantCulture,
                $"{walk.State} at ({walk.X:F2},{walk.Y:F2}), bound for ({walk.For.X:F2},{walk.For.Y:F2}), "
                + $"with the captain at ({(double)Get(map, "_avatarX")!:F2},{(double)Get(map, "_avatarY")!:F2})"
                + $" and {walk.Route.Count} point(s) of route."));
        Assert.True(trace.Distinct().Count() >= 10,
            $"her whole crossing is {trace.Distinct().Count()} distinct position(s). A walk the player can "
            + "watch is not two frames long.");

        double ground = 0;
        for (int i = 1; i < trace.Count; i++)
        {
            ground += Math.Sqrt(
                ((trace[i].X - trace[i - 1].X) * (trace[i].X - trace[i - 1].X))
                + ((trace[i].Y - trace[i - 1].Y) * (trace[i].Y - trace[i - 1].Y)));
        }
        Assert.True(ground > 3.0,
            "she covered "
            + string.Create(CultureInfo.InvariantCulture, $"{ground:F2}")
            + " du of ground getting to your table — the walk IS the ceremony, and a body that appears a "
            + "stride away has not crossed a room.");

        // (4) …AND ONLY NOW THE CARD. #865's rule, untouched: they came to you, therefore the card.
        Assert.True((bool)Get(map, "SeatedIsAConversation")!,
            "she crossed the whole hall, reached the chair, and got a HUD strip. #865's law is that somebody "
            + "who comes to YOU raises the card — the walk was supposed to change WHEN, never whether.");
        Assert.False((bool)Get(map, "SeatedIsDocked")!,
            "the strip and the card are both up — the frame forks on one question and it has two answers.");
    }

    // ── (g) AND NOT ONE LINE EXPLAINS IT ─────────────────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>THE ROOM TOLD YOU SOMETHING AND THE GAME DID NOT.</b>
    ///
    /// <para>The issue's hardest sentence: <i>"An NPC exiting through a door and that door refusing the
    /// captain ten seconds later is the whole beat, and no line of dialog may explain it."</i> A grep for
    /// "staff only" would be a guard that passes on any wording somebody had not thought of, so this is a
    /// <b>DIFFERENTIAL</b>: one watch of one room, played twice over the shipping surface tick with the same
    /// seeds and the same clock — once with the shift's departure dealt, once with it suppressed — and
    /// everything the game put in front of the player is transcribed on every frame and compared.</para>
    ///
    /// <para>Byte for byte the same text. A pulse, a card, a story beat or an outcome line that only exists
    /// when somebody stood up and walked out of the room is exactly the line the owner forbade.</para>
    ///
    /// <para>The suppressed run is the room with the SAME shift and the same everything, with only the deal
    /// held back (<c>HallDeparted</c> pre-spent), so the two runs differ in one fact and nothing else.</para>
    ///
    /// <para><b>The RED case.</b> Plant one line on the deal — <c>ShowPulseMessage("🚪 Staff only. That's
    /// why.")</c> in <c>TheyStandUpAndGo</c> — and the transcripts part on the frame she stands up. The
    /// verbatim run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void NOTHING_IsSaidAboutTheDoorThatOpenedForSomebodyElse()
    {
        (string Said, int Left, IReadOnlyList<int> StoodUp) walked = OneWatchOfTheRoom(suppressed: false);
        (string Said, int Left, IReadOnlyList<int> StoodUp) quiet = OneWatchOfTheRoom(suppressed: true);

        Assert.True(walked.Left > 0,
            "nobody left the room in the watch this guard plays, so the two transcripts are the same text "
            + "for the wrong reason — this would be a green test that asserts nothing.");
        Assert.Equal(0, quiet.Left);

        Assert.Equal(quiet.Said, walked.Said);

        // …and the other half of the same beat: the chair comes back EMPTY. One body, one place.
        object ex = Get(OnTheFloor(), "_surface")!;
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(Body, TheFloor, MoonSurface.ExpeditionField());
        UndergroundComplex.Amenity hall = floor.Amenities.First(a =>
            CanteenRegulars.PeopleSitHere(Body, TheFloor, a));
        var stoodUp = new HashSet<int>(walked.StoodUp);
        Assert.NotEmpty(stoodUp);
        foreach (CanteenRegulars.TableSeat top in
            CanteenRegulars.Tables(Body, TheFloor, hall, 0, stoodUp))
        {
            Assert.False(stoodUp.Contains(top.Index) && top.Taken,
                $"top {top.Index} still has somebody sitting at it after they stood up and walked out — one "
                + "body is drawn in two places, which is this repo's third named bug class arriving inside "
                + "the feature that caused it.");
        }
        _ = ex;
    }

    /// <summary>Play one watch of the canteen and transcribe everything the game said. The clock is set to
    /// the LAST CALL of watch zero, so every departure the shift scheduled is due — a schedule that runs to
    /// three quarters of a four-hour shift cannot otherwise be reached by stepping frames.</summary>
    private static (string Said, int Left, IReadOnlyList<int> StoodUp) OneWatchOfTheRoom(bool suppressed)
    {
        Pages.Map map = OnTheFloor();
        NothingPending(map);

        object ex = Get(map, "_surface")!;
        Type exType = ex.GetType();

        // Watch zero, at last call. CanteenWatch is already zero on a fresh excursion, and WatchIndex of this
        // clock is zero too — the room that is DRAWN and the shift that is WALKED are one shift.
        Set(map, "SimTime", Egress.LastCallFraction * PatronRota.WatchSeconds);
        Assert.Equal(0L, PatronRota.WatchIndex((double)Get(map, "SimTime")!));
        Assert.Equal(0L, (long)exType.GetProperty("CanteenWatch")!.GetValue(ex)!);

        if (suppressed)
        {
            // The SAME room and the SAME shift, with only the deal held back: every top is marked already
            // dealt, and the watch/floor stamps are set so the frame does not simply clear them again.
            var already = (HashSet<int>)exType.GetProperty("HallDeparted")!.GetValue(ex)!;
            for (int i = 0; i < 64; i++)
            {
                already.Add(i);
            }
            exType.GetProperty("WalkersWatch")!.SetValue(ex, 0L);
            exType.GetProperty("WalkersFloor")!.SetValue(ex, TheFloor);
        }

        var stood = (HashSet<int>)exType.GetProperty("HallStoodUp")!.GetValue(ex)!;
        var said = new StringBuilder();
        int left = 0;

        // The SAME number of frames both times, always — a run that stopped early because the room went
        // quiet would make the two transcripts different lengths for a reason that is not the law.
        for (int frame = 0; frame < 400; frame++)
        {
            Invoke(map, "StepSurface", Frame);
            left = Math.Max(left, stood.Count);
            said.Append(frame).Append('|').Append(WhatIsOnTheScreen(map)).Append('\n');
        }

        return (said.ToString(), left, [.. stood.OrderBy(i => i)]);
    }

    /// <summary>Everything the game is putting in front of the player, as one line. The pulse's own words,
    /// the centred card's label and body, and the story card's beat — which are the three surfaces this
    /// project says things on.</summary>
    private static string WhatIsOnTheScreen(Pages.Map map)
    {
        var pulse = (PulseSlot)Get(map, "_pulse")!;
        object? view = Get(map, "_viewObject");
        object? story = Get(map, "_storyCard");

        string card = "-";
        if (view is DeckPlan.ConsoleSpot spot)
        {
            card = $"{spot.Label}/{spot.Caption}/{spot.Outcome}";
        }

        return $"{pulse.Message ?? "-"}|{card}|{story?.ToString() ?? "-"}";
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sit the captain down at a free canteen top with the press a captain takes it with.</summary>
    private static void TakeAFreeTop(Pages.Map map)
    {
        DeckPlan.ConsoleSpot[] spots =
            [.. ThePlan(map).Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveTable)];
        Assert.True(spots.Length > 0,
            "this floor carves no free canteen top — the arrival guard would be asserting about a chair that "
            + "is not there.");
        Set(map, "_avatarX", (double)spots[0].X);
        Set(map, "_avatarY", (double)spots[0].Y);
        Assert.True((bool)Invoke(map, "TryTakeTable")!, "the press at the free top was not taken.");
        Assert.True(Get(map, "_table") is not null, "nobody sat down at the free top.");
    }

    /// <summary>The excursion's own list of people on their feet, as an <see cref="IList"/> — the walker
    /// record is private to the component and this guard only needs to count it and read the walk off it.
    /// </summary>
    private static IList Walkers(object ex) =>
        (IList)ex.GetType().GetProperty("Walkers", Hidden)!.GetValue(ex)!;

    private static NpcWalk WalkOf(object walker) =>
        (NpcWalk)walker.GetType().GetProperty("Walk", Hidden)!.GetValue(walker)!;

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

    private static object? Get(object o, string name)
    {
        if (SeatState.TryFollow(o, name, out object? seated))
        {
            return seated;
        }

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
        object target = map;
        if (call is null && SeatState.Seat(map) is { } seat)
        {
            call = seat.GetType().GetMethod(method, Hidden);
            target = seat;
        }
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(target, args);
    }
}
