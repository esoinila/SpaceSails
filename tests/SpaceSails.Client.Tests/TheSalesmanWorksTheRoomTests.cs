using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1061 · <b>YOU SEE THE PITCH COMING.</b>
///
/// <para><b>Owner, 2026-09-01:</b> <i>"let's at some point work on those A* walking insurance salesmen at
/// stations :-D"</i> And the design's own sentence for what that buys: <i>the captain watching him work two
/// tables before reaching yours is the point.</i></para>
///
/// <para>Until this lane Harlan Fess drifted round a ring of FURNITURE — a standing place at the counter, the
/// ends of two or three tops — with nothing at the far end of any of it, and then teleported at the captain
/// the moment they sat down. The room contained a man walking. What it contains now is a man SELLING: he
/// crosses to somebody else's table, stands there for a beat, goes on to the next mark, and when the room is
/// worked he leaves through a leaf that does not open for the captain.</para>
///
/// <h3>Why these guards are behavioural and not arithmetic</h3>
///
/// <para>The round itself is Core's and is guarded there (<c>TheSalesmanWorksTheRoomTests</c>, Core side:
/// frozen, real people, real pauses). What Core cannot see is whether the PAGE ever walks it — a perfect
/// round nobody spends is exactly the shape of the bug that put this whole feature in a browser twice already
/// (a salesman who never got on the floor at all, and a lift that only ever went down). So everything below
/// stands a real room up, runs the frame the way the game runs it, and reads the answer off the floor.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 23 s over 7 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheSalesmanWorksTheRoomTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";
    private const string ThreadId = "b71f4a0c39d24e5ba8027c6f1d3e5490";

    /// <summary>The one floor of this moon that people sit on — asked of the building, never typed in.</summary>
    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
                                  ?? throw new InvalidOperationException($"{Body} has no pressurised floor.");

    // ── The claims ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>HE WORKS THE ROOM'S OWN TABLES BEFORE HE EVER REACHES YOURS.</b> The lane's whole point, driven:
    /// the captain takes a top, the frames run, and by the time a body sets off for THEIR table it has already
    /// stood at <see cref="Egress.MarksBeforeTheTable"/> other people's.
    ///
    /// <para>Everything asserted here is read off the FLOOR and matched back against the room. A table only
    /// counts once he has actually <c>Arrived</c> at it — a man still crossing the hall has not worked
    /// anybody — and every one of them is checked against the tops <c>CanteenRegulars</c> really seated on
    /// this watch, so a round that stopped at empty chairs could not pass. The bench's own precondition is
    /// asserted first: a room with nobody in it proves nothing about a man working it.</para>
    ///
    /// <para><b>Proven RED</b> by dropping <c>TheCaptainHasWatchedHimWork</c> from <c>SendTheRepIn</c>'s
    /// crossing gate — he sets off for the captain's table having worked 0 of the room's own.</para>
    /// </summary>
    [Fact]
    public void HE_WorksTheRoomsOwnTablesBeforeHeEverReachesYours()
    {
        Pages.Map map = InTheCanteen();
        IReadOnlyList<CanteenRegulars.TableSeat> tops = TheRoomsTops(map);
        int seated = tops.Count(t => t.Taken && !t.Quiet);
        Assert.True(seated >= Egress.MarksBeforeTheTable,
                    $"{Body} B{-TheFloor} seats {seated} people on this watch, and the floor under this guard "
                    + $"is {Egress.MarksBeforeTheTable} — the room has nobody in it to work, so this guard "
                    + "would be a green number never asked of the world.");

        TakeATopAlone(map);

        var worked = new List<int>();
        bool crossingToYou = false;
        for (int i = 0; i < 4000 && !crossingToYou; i++)
        {
            RunFrames(map, 1);
            foreach (object who in Walkers(map))
            {
                string errand = Errand(who);
                if (errand == "RepPitching")
                {
                    crossingToYou = true;
                    continue;
                }

                int table = (int)Get(who, "Table")!;
                if (errand == "RepRounds" && table >= 0 && Arrived(who) && !worked.Contains(table))
                {
                    worked.Add(table);
                }
            }
        }

        Assert.True(crossingToYou,
                    "four hundred sim-seconds and he never set off for the captain's table at all — the room "
                    + "is being worked and the pitch never comes.");
        Assert.True(worked.Count >= Egress.MarksBeforeTheTable,
                    $"he set off for the captain's table having stood at {worked.Count} of the room's own "
                    + $"({string.Join(", ", worked)}), and the floor is {Egress.MarksBeforeTheTable}. The "
                    + "captain never saw the pitch coming.");

        foreach (int table in worked)
        {
            Assert.True(tops.Any(t => t.Index == table && t.Taken && !t.Quiet),
                        $"he stood a beat at top {table}, which this room did not seat anybody at — he is "
                        + "selling a policy to an empty chair.");
        }
    }

    /// <summary>
    /// <b>…AND ON A VISIT THE ROTA DOES NOT SEND HIM, NOBODY WORKS ANYBODY.</b> The vacuity half of the guard
    /// above, and the reason it is worth having: a room-working that happened on every watch would satisfy
    /// every claim in this file and would be a salesman nailed to every bar in the game.
    ///
    /// <para>Same room, same tops, same frames — one field different. No round is ever dealt, no mark is ever
    /// worked, and no body of his is ever on the floor.</para>
    /// </summary>
    [Fact]
    public void AND_AVisitTheRotaDoesNotSendHimToWorksNobody()
    {
        Pages.Map map = InTheCanteen(working: false);
        TakeATopAlone(map);
        RunFrames(map, 4000);

        Assert.Null(Field(map, "_repRound"));
        Assert.Equal(0, (int)Field(map, "_repMarksWorked")!);
        Assert.DoesNotContain(Walkers(map).Cast<object>(), w => Errand(w).StartsWith("Rep", StringComparison.Ordinal));
        Assert.Null((NebulaRep.RepPitch?)Field(map, "_repCard"));
    }

    /// <summary>
    /// <b>WHEN THE ROOM IS WORKED, HE GOES — OUT THROUGH A LEAF THAT DOES NOT OPEN FOR YOU.</b> #731's own
    /// full stop, spent by the man it was never built for: the round runs out, and he leaves like anybody
    /// whose shift has ended.
    ///
    /// <para>The captain is on their feet throughout, so there is nobody to pitch to and the round is the
    /// whole of his working day. The leaf he goes out of is matched back against the building's own
    /// <c>Locked</c> list — the type <see cref="Egress"/> refuses to take anything else as — and afterwards he
    /// does not come back, because a shift that ends and then does not is not a shift.</para>
    ///
    /// <para><b>Proven RED</b> by returning <c>false</c> from <c>HeGoesOffShift</c> before it plans anything:
    /// the round runs out and the salesman is simply still standing there, for ever.</para>
    /// </summary>
    [Fact]
    public void WHEN_TheRoomIsWorkedHeGoesOutThroughALeafThatDoesNotOpenForYou()
    {
        Pages.Map map = InTheCanteen();

        string? sign = null;
        for (int i = 0; i < 6000 && sign is null; i++)
        {
            RunFrames(map, 1);
            foreach (object who in Walkers(map))
            {
                if (Errand(who) == "RepLeaving")
                {
                    sign = (string)Get(Get(Get(who, "Walk")!, "For")!, "Sign")!;
                }
            }
        }

        Assert.True(sign is not null,
                    "six hundred sim-seconds with nobody to sell to and the salesman never went off shift — "
                    + "he is still working a room he has finished.");

        IReadOnlyList<UndergroundComplex.LockedDoor> leaves =
            UndergroundComplex.Build(Body, TheFloor, MoonSurface.ExpeditionField()).Locked;
        Assert.True(leaves.Any(d => string.Equals(d.Sign, sign, StringComparison.Ordinal)),
                    $"he left through `{sign}`, which is not a leaf this floor hangs a lock on at all.");

        // …and he does not come back. A shift that ends and then does not is not a shift.
        RunFrames(map, 3000);
        Assert.DoesNotContain(Walkers(map).Cast<object>(),
                              w => Errand(w) is "RepRounds" or "RepPitching" or "RepLeaving");
    }

    /// <summary>
    /// <b>NOTHING IS SAID AT ANYBODY ELSE'S TABLE.</b> The design's own words: <i>no card raised for
    /// NPC-to-NPC sales; the room just visibly contains a man selling.</i> §13.8, and the reason the patter is
    /// a PAUSE and not a line — a pop-up over a sale the captain is not part of would be the game narrating
    /// its own ambience.
    ///
    /// <para>The whole round is run with the captain on their feet, and at no frame of it is a card up or a
    /// pulse written. The pulse is checked as an ABSENCE against a strip that starts empty, which is the same
    /// assertion #1059 makes of the docked paper.</para>
    /// </summary>
    [Fact]
    public void NOTHING_IsSaidAtAnybodyElsesTable()
    {
        Pages.Map map = InTheCanteen();
        Assert.Null(ThePulse(map));

        for (int i = 0; i < 3000; i++)
        {
            RunFrames(map, 1);
            Assert.Null((NebulaRep.RepPitch?)Field(map, "_repCard"));
            Assert.True(ThePulse(map) is null,
                        $"the room said `{ThePulse(map)}` about a man selling to somebody who is not the "
                        + "captain. The pause is the patter.");
        }

        // …and the round really was walked, so this is not a green number asked of an empty room.
        Assert.True((int)Field(map, "_repMarksWorked")! > 0,
                    "nobody was worked in the whole run, so nothing had a chance to be said about it.");
    }

    /// <summary>
    /// <b>HE GIVES THE ROOM A BODY'S BERTH, AND GIVES THE CAPTAIN NONE.</b> The #731 doorway ruling, kept
    /// through the one mechanism that keeps it: a walk carrying a berth stops, waits and turns to look at
    /// whoever is in its way; a walk carrying none does not, because at the captain's own table the captain is
    /// not in the way — they are the reason for the walk.
    ///
    /// <para>Read off the WALK and not off the planner, because that is the only place the distinction
    /// actually exists. Before this lane his rounds carried no berth at all underground, so a captain standing
    /// between him and a top was walked through rather than waited for.</para>
    ///
    /// <para><b>Proven RED</b> by handing <c>NpcWalk.NoPersonalSpace</c> to his round walks again.</para>
    /// </summary>
    [Fact]
    public void HE_GivesTheRoomABodysBerthAndTheCaptainNone()
    {
        Pages.Map map = InTheCanteen();
        TakeATopAlone(map);

        int rounds = 0, pitches = 0;
        for (int i = 0; i < 4000; i++)
        {
            RunFrames(map, 1);
            foreach (object who in Walkers(map))
            {
                string errand = Errand(who);
                if (errand is not ("RepRounds" or "RepPitching" or "RepLeaving"))
                {
                    continue;
                }

                double berth = KeepOut(Get(who, "Walk")!);
                if (errand == "RepPitching")
                {
                    pitches++;
                    Assert.True(berth == 0,
                                $"his walk to the captain's own table keeps {berth:0.##} du clear of them — a "
                                + "body that stops one width short of the chair it is walking to and stares "
                                + "for ever is a deadlock, not politeness.");
                }
                else
                {
                    rounds++;
                    Assert.True(berth > 0,
                                $"his {errand} walk keeps nobody any room at all. A captain standing in the "
                                + "way of a man crossing a room he has no business with them in is supposed "
                                + "to stop him.");
                }
            }
        }

        Assert.True(rounds > 0 && pitches > 0,
                    $"{rounds} round frame(s) and {pitches} pitch frame(s) — one of the two halves of this "
                    + "claim was never exercised at all.");
    }

    // ── …AND IN THE ROOM THE OWNER ACTUALLY DRINKS IN ──────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SAME WORKING DAY IN A DOCKED STATION'S BAR.</b> The design says <i>at stations</i>, and a
    /// station's bar is not a Hive canteen: its people are names in numbered chairs off <c>PatronRota</c> with
    /// #1064's churn over the top of them, not tops off <c>CanteenRegulars</c>. One round, two rooms — so the
    /// claim is made twice, because a beat that only worked underground would be exactly half of what the
    /// owner asked for.
    ///
    /// <para>He stands at somebody else's chair before he crosses to the captain's top, and when the bar is
    /// worked he goes out through one of its own back-room leaves. Both halves are read off the floor.</para>
    ///
    /// <para><b>Proven RED</b> by dropping <c>TheCaptainHasWatchedHimWork</c> from
    /// <c>SendTheRepIntoTheBar</c>'s crossing gate.</para>
    /// </summary>
    [Fact]
    public void ASHORE_HeWorksTheBarsOwnRegularsBeforeHeReachesYourTop()
    {
        Pages.Map map = InTheBar();
        IReadOnlyList<HavenInterior.SeatedRegular> rota = HavenInterior.ResolveRegulars(TheRedEye, 0);
        int present = rota.Count(r => r.Present);
        Assert.True(present >= Egress.MarksBeforeTheTable,
                    $"{TheRedEye}'s bar seats {present} regular(s) on this watch and the floor under this "
                    + $"guard is {Egress.MarksBeforeTheTable} — the room has nobody in it to work.");

        SitAtABarTop(map);

        var worked = new List<int>();
        bool crossingToYou = false;
        for (int i = 0; i < 4000 && !crossingToYou; i++)
        {
            RunBarFrames(map, 1);
            foreach (object who in BarAfoot(map))
            {
                if (Errand(who) == "Approaching"
                    && string.Equals((string)Get(Get(who, "Walk")!, "Plate")!, NebulaRep.Plate,
                                     StringComparison.Ordinal))
                {
                    crossingToYou = true;
                    continue;
                }

                int mark = (int)Get(who, "Table")!;
                if (Errand(who) == "RepRounds" && mark >= 0 && Arrived(who) && !worked.Contains(mark))
                {
                    worked.Add(mark);
                }
            }
        }

        Assert.True(crossingToYou, "he never crossed the bar to the captain's top at all.");
        Assert.True(worked.Count >= Egress.MarksBeforeTheTable,
                    $"he crossed to the captain's top having stood at {worked.Count} of the bar's own chairs "
                    + $"({string.Join(", ", worked)}), and the floor is {Egress.MarksBeforeTheTable}.");

        foreach (int mark in worked)
        {
            Assert.True(mark < rota.Count && rota[mark].Present,
                        $"he stood a beat at rota slot {mark}, which this bar is not seating anybody in.");
        }
    }

    /// <summary>…and when the bar is worked he leaves through one of its own back-room leaves — the same
    /// full stop the room's regulars get, spent by a man who does not live here.
    ///
    /// <para><b>Proven RED</b> by returning <c>false</c> from <c>HeGoesOffShiftFromTheBar</c> before it plans
    /// anything.</para></summary>
    [Fact]
    public void ASHORE_AndWhenTheBarIsWorkedHeGoesOutThroughOneOfItsOwnLeaves()
    {
        Pages.Map map = InTheBar();

        string? sign = null;
        for (int i = 0; i < 6000 && sign is null; i++)
        {
            RunBarFrames(map, 1);
            foreach (object who in BarAfoot(map))
            {
                if (Errand(who) == "RepLeaving")
                {
                    sign = (string)Get(Get(Get(who, "Walk")!, "For")!, "Sign")!;
                }
            }
        }

        Assert.True(sign is not null,
                    "six hundred sim-seconds with nobody to sell to and the salesman is still in this bar.");
        Assert.Contains(HavenInterior.BarBand(TheRedEye)!.Value.Doors,
                        leaf => string.Equals(leaf.Sign, sign, StringComparison.Ordinal));
    }

    // ── The room ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A captain standing on a real canteen floor of a real building, with Fess on the rota (or
    /// deliberately off it). The bench <c>TheRepCrossesTheFloorTests</c> stands its worlds on, because a guard
    /// handed a world it invented itself cannot tell pass from fail.</summary>
    private static Pages.Map InTheCanteen(bool working = true)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden)!;
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
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", working);
        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Sit the captain down alone at one of the room's own tops, through the shipped press.</summary>
    private static void TakeATopAlone(Pages.Map map)
    {
        var plan = (DeckPlan)Field(map, "_deckPlan")!;
        foreach (DeckPlan.ConsoleSpot top in
                 plan.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveTable))
        {
            Set(map, "_avatarX", (double)top.X);
            Set(map, "_avatarY", (double)top.Y);
            if (Invoke(map, "TryTakeTable") is true)
            {
                Assert.True((bool)Read(map, "SeatedAlone")!, "the captain sat down with company.");
                return;
            }
        }

        Assert.Fail("no free top on this watch — this world has nobody sitting alone in it.");
    }

    /// <summary>THE ROOM'S OWN LIST OF TOPS, asked through the page's own answer to "which amenity is the
    /// canteen" — never a second reading of the building.</summary>
    private static IReadOnlyList<CanteenRegulars.TableSeat> TheRoomsTops(Pages.Map map)
    {
        object ex = Field(map, "_surface")!;
        object?[] args = [ex, null];
        bool found = (bool)typeof(Pages.Map).GetMethod("TheCanteenOn", Hidden)!.Invoke(null, args)!;
        Assert.True(found, $"{Body} B{-TheFloor} is not a floor people sit on.");

        return CanteenRegulars.Tables(
            Body, TheFloor, (UndergroundComplex.Amenity)args[1]!,
            (long)ex.GetType().GetProperty("CanteenWatch", Hidden)!.GetValue(ex)!,
            (IReadOnlySet<int>)ex.GetType().GetProperty("HallStoodUp", Hidden)!.GetValue(ex)!);
    }

    /// <summary>Run the room's own frame, the way the surface tick runs it.</summary>
    private static void RunFrames(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Field(map, "SimTime")! + dt);
            Invoke(map, "AdvanceWalkers", dt);
            Invoke(map, "AdvanceTheRep", dt);
        }
    }

    /// <summary>The classy great-port tier, and the room the owner drinks in.</summary>
    private const string TheRedEye = "red-eye";

    /// <summary>A page clamped onto The Red Eye, standing in its bar, with the deck the game builds — the
    /// bench <c>TheDockedBarIsAWalkableRoomTests</c> stands its worlds on.</summary>
    private static Pages.Map InTheBar()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_dockedHavenId", TheRedEye);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", true);
        Invoke(map, "SetDeckForDock", TheRedEye);
        Invoke(map, "StandAtTheBarThreshold");
        return map;
    }

    /// <summary>Sit the captain at a free top in the bar, through the shipped press (#973 L5b's eighth
    /// sitting site) — never a sitting this guard assembled for itself.</summary>
    private static void SitAtABarTop(Pages.Map map)
    {
        foreach (DeckReachability.Point top in HavenInterior.BarBand(TheRedEye)!.Value.Tops)
        {
            Set(map, "_avatarX", top.X);
            Set(map, "_avatarY", top.Y);
            if (Invoke(map, "TryTakeBarTop") is true)
            {
                Assert.True((bool)Read(map, "SeatedAlone")!, "the captain sat down with company.");
                return;
            }
        }

        Assert.Fail("no top in this bar had a place at it to sit — nobody is sitting alone in this world.");
    }

    /// <summary>Run the bar's own frame, the way the walked view runs it.</summary>
    private static void RunBarFrames(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Field(map, "SimTime")! + dt);
            Invoke(map, "AdvanceBarWalkers", dt);
        }
    }

    private static IList BarAfoot(Pages.Map map) => (IList)Field(map, "_barAfoot")!;

    private static IList Walkers(Pages.Map map)
    {
        object ex = Field(map, "_surface")!;
        return (IList)ex.GetType().GetProperty("Walkers", Hidden)!.GetValue(ex)!;
    }

    private static string Errand(object walker) => Get(walker, "For")!.ToString()!;

    private static bool Arrived(object walker) =>
        Get(Get(walker, "Walk")!, "State")!.ToString() == nameof(NpcWalk.Doing.Arrived);

    /// <summary>How much room this walk keeps clear of the captain, in deck units. The walk's own number and
    /// not the planner's argument — the field is what the step actually spends.</summary>
    private static double KeepOut(object walk) =>
        (double)typeof(NpcWalk).GetField("_keepOut", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(walk)!;

    private static string? ThePulse(Pages.Map map) => ((PulseSlot)Field(map, "_pulse")!).Message;

    // ── Reflection plumbing ────────────────────────────────────────────────────────────────────────────

    private static FieldInfo FieldOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name.");

    private static object? Field(Pages.Map map, string name) => FieldOf(name).GetValue(map);

    private static void Set(Pages.Map map, string name, object? value) => FieldOf(name).SetValue(map, value);

    private static object? Get(object o, string member) => o.GetType().GetProperty(member, Hidden)!.GetValue(o);

    private static object? Read(Pages.Map map, string member) =>
        typeof(Pages.Map).GetProperty(member, Hidden)!.GetValue(map);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo call = typeof(Pages.Map).GetMethod(method, Hidden)
            ?? throw new InvalidOperationException($"Map has no `{method}` — this guard is reading a dead name.");
        try
        {
            return call.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
