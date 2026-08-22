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
/// #731 v2 · FOLLOW ME INTO THE CABINET — driven on the room that ships, with the presses a captain makes.
///
/// <para><b>Owner, 2026-08-06, on #751's cabinets:</b> <i>"Also it is dramatic telling when our contact wants
/// us to follow them into kabinetti :-D"</i></para>
///
/// <h3>What can only be proved on this side</h3>
///
/// <para>Core's <c>FollowMeIntoTheCabinetTests</c> proves the geometry and the law: the booth is a door held
/// open, the walk is a real route, and nobody with nothing to say leads anybody anywhere. Three things are
/// only true of the game:</para>
///
/// <list type="number">
/// <item><b>She actually stands up, mid-conversation</b>, on the frame the thing she came to say becomes
/// sayable — and the card comes down to the strip, because she is not at your table any more.</item>
/// <item><b>She waits</b>, in the doorway, looking at you, for as long as you take.</item>
/// <item><b>The scene resumes in the booth with the SAME deal move</b>, and the only sentence anywhere in
/// the whole beat is #758's own description of a curtain or a leaf. Nothing explains anything.</item>
/// </list>
///
/// <para>The bench is <c>TheExitIsTheFullStopTests</c>' — a real <see cref="Pages.Map"/> on a real generated
/// floor, seats taken with the shipping verbs, frames spent in the shipping <c>StepSurface</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class FollowMeIntoTheCabinetTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    /// <summary>One frame — a tenth of a second, the longest step the walker band takes however long the
    /// browser was away. The game's own worst frame, driven at its own ceiling.</summary>
    private const double Frame = 0.1;

    private const int FrameCeiling = 4000;

    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor to sit down on.");

    // ── (a) SHE STANDS UP, CROSSES THE HALL, AND WAITS AT THE DOOR ───────────────────────────────────

    /// <summary>
    /// #731 v2 · <b>THE WALK IS THE TELLING, AND THE DOORWAY IS THE INVITATION.</b>
    ///
    /// <para>Owner: <i>"it is dramatic telling when our contact wants us to follow them into kabinetti."</i>
    /// The whole beat, end to end, on the shipping surface tick:</para>
    ///
    /// <list type="number">
    /// <item>She crosses the hall to your table and the card comes up — #731 v1, unchanged.</item>
    /// <item>You wave her in and let her buy. On the frame her ask becomes sayable she <b>stands up</b>: a
    /// walker on the floor bound for a CABINET's own plate (which is on nobody's locked list), and the card
    /// is down — you are sitting at your own table again, watching somebody walk away.</item>
    /// <item>Her crossing is a real path — many distinct positions, several deck units of ground, and no
    /// frame inside stone — and it ends at the door rather than through it.</item>
    /// <item><b>And then she waits</b>, for hundreds of frames, facing the captain the whole time. She does
    /// not go in, she does not vanish, and she does not walk back.</item>
    /// <item>You follow. Sitting down at that booth resumes the conversation: her plate, her scene, the
    /// modal frame — and the <b>same deal move</b>, on offer, saying the same sentence it would have said in
    /// the hall.</item>
    /// <item>The only line in the whole thing is <see cref="CabinetPrivacy.SaidOn"/>, #758's own, asserted by
    /// identity against the shipped constant.</item>
    /// </list>
    ///
    /// <para><b>The RED cases.</b> (i) Teleport her — make <c>WalkTheVisitorIntoACabinet</c> place her at the
    /// door instead of planning (or simply return false) and rows 2 and 3 go red with nobody on their feet.
    /// (ii) Take away the wait — treat <c>Errand.LeadingYouIn</c> like the other two in <c>AdvanceWalkers</c>
    /// so an arrived walker is removed — and row 4 goes red with an empty doorway. Both verbatim in the pull
    /// request.</para>
    /// </summary>
    [Fact]
    public void THE_CONTACT_WalksYouIntoACabinetAndWaitsAtTheDoor()
    {
        Pages.Map map = OnTheFloor();
        NothingPending(map);
        int top = ATopSheWouldLeadYouFrom(map);

        object ex = Get(map, "_surface")!;
        IList afoot = Walkers(ex);

        // ── (1) #731 v1: somebody crosses the hall to you and the card comes up.
        Set(map, "_approachCheat", (bool?)true);
        Invoke(map, "TableMove", SittingAlone.Wait);
        Assert.True(afoot.Count == 1, "nobody crossed the hall — this guard cannot even begin.");
        int spent = 0;
        while (afoot.Count > 0 && spent++ < FrameCeiling)
        {
            Invoke(map, "StepSurface", Frame);
        }
        Assert.True((bool)Get(map, "SeatedIsAConversation")!, "she never reached the table.");

        // ── (2) …and now the two rungs, and on the second of them she stands up.
        Invoke(map, "TableMove", SittingAlone.WaveIn);
        Assert.True(afoot.Count == 0,
            "she got up while she was still being waved into the chair — the beat is supposed to fire when "
            + "the thing she came to say becomes sayable, and nothing has been said yet.");

        Invoke(map, "TableMove", SittingAlone.LetThemBuy);

        Assert.True(afoot.Count == 1,
            $"the drink was answered and {afoot.Count} people are on their feet. She is supposed to stand up "
            + "in the middle of it and walk you out of the hall — a beat that puts nobody on the floor is "
            + "the old scene with #731 v2's name on it.");
        object her = afoot[0]!;
        NpcWalk walk = WalkOf(her);
        Assert.Equal("LeadingYouIn", ErrandOf(her));
        Assert.Equal(SittingAlone.VisitorPlate, walk.Plate);

        // …bound for a CABINET, and a cabinet is not a door that refuses you. The mirror of v1's law.
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(Body, TheFloor, MoonSurface.ExpeditionField());
        UndergroundComplex.Amenity hall = floor.Amenities.First(a =>
            CanteenRegulars.PeopleSitHere(Body, TheFloor, a));
        Assert.Contains(hall.Hall!.Value.Cabinets,
            c => string.Equals(c.Plate, walk.For.Sign, StringComparison.Ordinal));
        Assert.DoesNotContain(floor.Locked,
            l => string.Equals(l.Sign, walk.For.Sign, StringComparison.Ordinal));

        // …and the card is DOWN. She is not at your table any more; the panel is the one you were sitting in
        // before she arrived, with the last thing she said still on it.
        Assert.False((bool)Get(map, "SeatedIsAConversation")!,
            "the modal card is still up over a chair she has got out of.");
        Assert.True((bool)Get(map, "SeatedIsDocked")!, "the sitting lost its frame entirely.");
        Assert.Equal(SittingAlone.DrinkTakenLine, OutcomeOn(map));

        // ── (3) THE TRACE. The shipping surface tick, frame by frame, over the shipping collision.
        DeckPlan plan = ThePlan(map);
        var trace = new List<(double X, double Y)> { (walk.X, walk.Y) };
        var transcript = new List<string> { WhatIsOnTheScreen(map) };
        spent = 0;
        while (walk.State != NpcWalk.Doing.Arrived && spent < FrameCeiling)
        {
            Invoke(map, "StepSurface", Frame);
            spent++;
            trace.Add((walk.X, walk.Y));
            transcript.Add(WhatIsOnTheScreen(map));
            Assert.False(plan.Collides(walk.X, walk.Y), string.Create(CultureInfo.InvariantCulture,
                $"she is inside the room's own stone at ({walk.X:F2},{walk.Y:F2}) on frame {spent}."));
        }
        Assert.True(spent < FrameCeiling,
            $"she was still crossing the hall after {FrameCeiling} frames — she is {walk.State}.");
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
            "she covered " + string.Create(CultureInfo.InvariantCulture, $"{ground:F2}")
            + " du getting to the booth — the walk across the hall IS the beat, and a body that appears at "
            + "the door has not crossed anything.");

        // ── (4) AND SHE WAITS. Hundreds of frames of a woman standing in a doorway looking at you.
        for (int i = 0; i < 300; i++)
        {
            Invoke(map, "StepSurface", Frame);
            transcript.Add(WhatIsOnTheScreen(map));
            Assert.True(afoot.Count >= 1 && ReferenceEquals(afoot[0], her),
                $"the doorway is empty after {i} frame(s) of waiting. The whole beat is that she stands "
                + "there until you come — a figure that walks in on its own has taken the invitation away.");
            double looks = Math.Atan2(
                (double)Get(map, "_avatarY")! - walk.Y, (double)Get(map, "_avatarX")! - walk.X);
            Assert.True(Math.Abs(Math.Atan2(Math.Sin(looks - walk.Facing), Math.Cos(looks - walk.Facing)))
                    < 0.001,
                "she is not looking at you. Being looked at from a doorway is the whole of what this beat "
                + "says instead of a line of dialogue.");
        }

        // ── (5) AND NOT ONE WORD WAS SAID ABOUT ANY OF IT.
        Assert.True(transcript.Distinct().Count() == 1,
            $"the game said {transcript.Distinct().Count()} different things while she was crossing the hall "
            + "and standing in the doorway, and it is supposed to say nothing at all:\n  "
            + string.Join("\n  ", transcript.Distinct().Take(6)));

        // ── (6) YOU FOLLOW HER IN.
        int booth = (int)Get(ex, "EscortCabinet")!;
        int boothTop = (int)Get(ex, "EscortCabinetTop")!;
        Assert.True(booth > 0 && boothTop >= 0, "nobody is holding a door open, according to the excursion.");
        CanteenRegulars.TableSeat seat = CanteenRegulars
            .Tables(Body, TheFloor, hall, 0L, (HashSet<int>)Get(ex, "HallStoodUp")!)
            .First(s => s.Index == boothTop);
        Assert.True(seat.Cabinet == booth, "the top she is holding the door of is not that cabinet's top.");

        Invoke(map, "CloseTable");
        Set(map, "_avatarX", seat.X);
        Set(map, "_avatarY", seat.Y);
        Invoke(map, "RebuildSurfaceDeck");
        Assert.True((bool)Invoke(map, "TryTakeTable")!, "sitting down in the booth was refused.");

        object t = Get(map, "_table")!;
        Assert.True((bool)Get(map, "SeatedIsAConversation")!,
            "you followed her into a booth and got the docked strip. She came to YOU — #865's law does not "
            + "change because the table did.");
        Assert.Equal(SittingAlone.VisitorPlate, (string)Get(t, "Plate")!);
        Assert.Equal(booth, (int)Get(t, "Cabinet")!);
        Assert.True((bool)Get(t, "Quiet")!, "the resumed scene does not think it is in a cabinet.");
        Assert.Empty(afoot);

        // …and the one sentence in the whole beat is #758's, by identity.
        CabinetPrivacy.Stage stage = CabinetPrivacy.EscortsStage(SittingAlone.VisitorPlate);
        Assert.Equal(CabinetPrivacy.SaidOn(stage), OutcomeOn(map));
        Assert.Equal(
            stage == CabinetPrivacy.Stage.Door,
            ((HashSet<string>)Get(ex, "CabinetsDogged")!).Contains(CabinetPrivacy.Key(TheFloor, booth)));

        // ── (7) THE SAME DEAL MOVE, IN A ROOM WITH NO EARS IN IT.
        Encounter.Move deal = Escort.TheDealMoveIn(SittingAlone.TheVisitor())!.Value;
        Assert.Contains(SceneOn(t).Moves, m => string.Equals(m.Id, deal.Id, StringComparison.Ordinal));
        Assert.True((bool)Invoke(map, "TableMoveOnOffer", deal)!,
            $"`{deal.Label}` is on the panel and disabled. The whole reason she walked you in here is that "
            + "she would not say it in the hall — a booth that cannot hear it is a walk for nothing.");
        Invoke(map, "TableMove", deal.Id);
        Assert.Equal(SittingAlone.TheAskLine, OutcomeOn(map));
    }

    // ── (b) AND IF YOU NEVER FOLLOW ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// #731 v2 · <b>SHE WAITS A WATCH, AND THEN SHE GOES THROUGH A DOOR THAT DOES NOT OPEN FOR YOU.</b>
    ///
    /// <para>The refusal is #731 v1's full stop arriving from the other direction, and it needs no new
    /// machinery: <see cref="Egress.ArrivalDoor"/> off the SAME frozen watch and the SAME top she crossed the
    /// room from, so she leaves through the leaf she came out of. Every satchel kind is offered at it through
    /// the shipping <c>SatchelTry</c> and refused.</para>
    ///
    /// <para>Nothing is said. The doorway is simply empty the next time you look at it.</para>
    ///
    /// <para><b>The RED case.</b> Drop the patience — <c>SheHasWaitedLongEnough</c> returning false — and she
    /// stands in that doorway for the rest of the shift. Verbatim in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_DOORWAY_IsEmptyIfYouNeverCome()
    {
        Pages.Map map = OnTheFloor();
        NothingPending(map);
        _ = ATopSheWouldLeadYouFrom(map);

        object ex = Get(map, "_surface")!;
        IList afoot = Walkers(ex);

        Set(map, "_approachCheat", (bool?)true);
        Invoke(map, "TableMove", SittingAlone.Wait);
        int spent = 0;
        while (afoot.Count > 0 && spent++ < FrameCeiling)
        {
            Invoke(map, "StepSurface", Frame);
        }
        Invoke(map, "TableMove", SittingAlone.WaveIn);
        Invoke(map, "TableMove", SittingAlone.LetThemBuy);
        Assert.Single(afoot);

        NpcWalk walk = WalkOf(afoot[0]!);
        spent = 0;
        while (walk.State != NpcWalk.Doing.Arrived && spent++ < FrameCeiling)
        {
            Invoke(map, "StepSurface", Frame);
        }
        Assert.Equal(NpcWalk.Doing.Arrived, walk.State);

        // …and now the captain does not come. The clock crosses her patience.
        var said = new List<string> { WhatIsOnTheScreen(map) };
        Set(map, "SimTime", (double)Get(map, "SimTime")! + Escort.PatienceSeconds + 1);
        NpcWalk? away = null;
        spent = 0;
        while (spent++ < FrameCeiling)
        {
            Invoke(map, "StepSurface", Frame);
            said.Add(WhatIsOnTheScreen(map));
            foreach (object? w in afoot)
            {
                if (w is not null && ErrandOf(w) == "Leaving"
                    && WalkOf(w).Plate == SittingAlone.VisitorPlate)
                {
                    away = WalkOf(w);
                }
            }
            if (away is not null || afoot.Count == 0)
            {
                break;
            }
        }

        Assert.True((int)Get(ex, "EscortCabinetTop")! < 0,
            "the excursion still thinks somebody is holding a door open for a captain who never came.");
        Assert.True(away is not null,
            "she never left. A body that waits in a doorway for the rest of the shift is a statue, and the "
            + "owner's own answer to never following is that she goes through a door that does not open "
            + "for you.");

        Assert.True(away!.For.IsADoor, "she walked out carrying no door at all.");
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(Body, TheFloor, MoonSurface.ExpeditionField());
        Assert.Contains(floor.Locked,
            l => string.Equals(l.Sign, away.For.Sign, StringComparison.Ordinal));
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            Assert.False(
                SatchelTry.Offer(new Satchel.Item(kind, "test-item"), SatchelTry.Target.RoomDoor, Body).Worked,
                $"the captain's own {kind} opens `{away.For.Sign}` — she is leaving through a door they "
                + "could have followed her through, and the full stop is a comma.");
        }

        Assert.True(said.Distinct().Count() == 1,
            "the game said something about her giving up and going:\n  "
            + string.Join("\n  ", said.Distinct().Take(6)));
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component on a real Hive floor, with nothing running but the deck.</summary>
    private static Pages.Map OnTheFloor()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved, and the seat verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
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
    /// card this beat raised. Every one of these is one-shot per excursion and none of them is #731's.</summary>
    private static void NothingPending(Pages.Map map)
    {
        object ex = Get(map, "_surface")!;
        ex.GetType().GetProperty("HiveCantinaHallShown")!.SetValue(ex, true);
        ex.GetType().GetProperty("HiveCabinetShown")!.SetValue(ex, true);
    }

    /// <summary>Sit the captain at a free HALL top that Core says this shift's contact would lead them away
    /// from — asked of <see cref="Escort.LeadsYouIn"/> rather than picked, so the bench and the game are
    /// choosing the same seat for the same reason.</summary>
    private static int ATopSheWouldLeadYouFrom(Pages.Map map)
    {
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(Body, TheFloor, MoonSurface.ExpeditionField());
        UndergroundComplex.Amenity hall = floor.Amenities.First(a =>
            CanteenRegulars.PeopleSitHere(Body, TheFloor, a));

        DeckPlan.ConsoleSpot[] spots =
            [.. ThePlan(map).Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveTable)];
        foreach (CanteenRegulars.TableSeat top in CanteenRegulars.Tables(Body, TheFloor, hall, 0L))
        {
            if (top.Cabinet > 0 || top.Taken
                || !Escort.LeadsYouIn(
                    Body, TheFloor, 0L, top.Index, SittingAlone.VisitorPlate, SittingAlone.TheVisitor()))
            {
                continue;
            }
            if (!spots.Any(s => Math.Abs(s.X - top.X) < 0.5 && Math.Abs(s.Y - top.Y) < 0.5))
            {
                continue;
            }
            Set(map, "_avatarX", top.X);
            Set(map, "_avatarY", top.Y);
            Assert.True((bool)Invoke(map, "TryTakeTable")!, $"the press at top {top.Index} was not taken.");
            return top.Index;
        }

        throw new InvalidOperationException(
            "no free hall top on luna B1 watch 0 is one the contact would lead the captain away from — the "
            + "bench cannot drive the beat it exists to drive.");
    }

    /// <summary>Everything the game is putting in front of the player, as one line: the pulse's own words,
    /// the centred card, the story beat, and the panel's own outcome — which are the four surfaces this beat
    /// could possibly explain itself on.</summary>
    private static string WhatIsOnTheScreen(Pages.Map map)
    {
        var pulse = (PulseSlot)Get(map, "_pulse")!;
        object? view = Get(map, "_viewObject");
        object? story = Get(map, "_storyCard");

        string card = view is DeckPlan.ConsoleSpot spot
            ? $"{spot.Label}/{spot.Caption}/{spot.Outcome}"
            : "-";
        return $"{pulse.Message ?? "-"}|{card}|{story?.ToString() ?? "-"}|{OutcomeOn(map) ?? "-"}";
    }

    private static string? OutcomeOn(Pages.Map map) =>
        Get(map, "_table") is { } t ? (string?)Get(t, "Outcome") : null;

    private static Encounter.Scene SceneOn(object table) =>
        (Encounter.Scene)table.GetType().GetProperty("Scene", Hidden)!.GetValue(table)!;

    private static IList Walkers(object ex) =>
        (IList)ex.GetType().GetProperty("Walkers", Hidden)!.GetValue(ex)!;

    private static NpcWalk WalkOf(object walker) =>
        (NpcWalk)walker.GetType().GetProperty("Walk", Hidden)!.GetValue(walker)!;

    private static string ErrandOf(object walker) =>
        walker.GetType().GetProperty("For", Hidden)!.GetValue(walker)!.ToString()!;

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
