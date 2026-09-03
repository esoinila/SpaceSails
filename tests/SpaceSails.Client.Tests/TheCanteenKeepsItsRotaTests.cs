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
/// #731 (B1 canteen) · THE ROTA TURNS OVER WHERE THE CAPTAIN CAN SEE IT — driven on the floor that ships.
///
/// <para><b>The issue's second customer, in its own words:</b> <i>"The B1 canteen: rota turnover made visible —
/// the agency temp leaving at watch change through the staff door, showing the pass nobody inside asks
/// for."</i></para>
///
/// <para>#731 v1 gave this floor a schedule that only ever DRAINED it. The bar upstairs learned the other
/// direction in #1064 — <c>Egress.Arrivals</c>, the same deal run in reverse — and this lane spends that seam
/// here rather than forking a canteen-flavoured copy of it. What is new on this side is the ENDING one of the
/// ten has: they get to the leaf, turn back to the room, and hold the pass up to it, and the room does not
/// look.</para>
///
/// <h3>What is asked here rather than in Core</h3>
///
/// <para>Core's own <c>TheRotaTurnsOverInTheOpenTests</c> proves the rota reading, the deal and the seating are
/// what they claim. Three things can only be proved on this side, driven frame by frame through the shipping
/// <c>StepSurface</c> over the shipping deck's own collision:</para>
///
/// <list type="number">
/// <item><b>They WALK.</b> A body comes out of a leaf the captain's own TRY is refused at, crosses real ground,
/// and is in the chair on the frame it reaches it — never on the frame it set off.</item>
/// <item><b>The pass is held up, and nothing answers it.</b> The gesture happens; the screen does not change
/// by one byte while it does.</item>
/// <item><b>And not one line explains any of it</b> — a differential over a whole watch of the room, dealt and
/// suppressed, transcribed on every frame and compared.</item>
/// </list>
///
/// <para>The bench is <c>TheExitIsTheFullStopTests</c>' — a real <see cref="Pages.Map"/> on a real generated
/// floor, frames spent in the shipping surface tick.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCanteenKeepsItsRotaTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    /// <summary>One frame. A tenth of a second, which is the longest step the walker band will take in one
    /// frame however long the browser was away — the game's own worst frame, driven at its own ceiling.
    /// </summary>
    private const double Frame = 0.1;

    /// <summary>How many frames a beat is given before the guard calls it stuck. At the frame above this is
    /// 200 du of walking, which is most of the way across the field.</summary>
    private const int FrameCeiling = 1000;

    /// <summary>How far into a shift "the scene is not over yet" is. Forty seconds, the same reading #1064's
    /// own pair takes in the bar.</summary>
    private const double EarlyInTheWatch = 40.0;

    /// <summary>Which shifts the sweep plays. Forty-eight is eight days of this station's time — enough that
    /// the seeded third who finish and the seeded few who come on are different people many times over, and
    /// enough that the room's rarest beat (the agency temp rostered AND finishing, about one shift in eleven
    /// on this floor) is asked of properly rather than hoped for.</summary>
    private static IEnumerable<long> Watches() => Enumerable.Range(0, 48).Select(i => (long)i);

    /// <summary>The room people sit in on this floor, as Core carved it.</summary>
    private static UndergroundComplex.Amenity TheRoom() =>
        ThisFloor.Amenities.First(a => CanteenRegulars.PeopleSitHere(Body, TheFloor, a));

    /// <summary>#731 · The shifts on which the schedule names a given kind of departure — asked of Core's own
    /// deal, off the same frozen watch the room is drawn on, so the guard drives the shifts the game itself
    /// would and never a shift it wishes existed.</summary>
    private static List<long> ShiftsWhere(Func<Egress.Move, bool> what)
    {
        UndergroundComplex.Amenity room = TheRoom();
        IReadOnlyList<UndergroundComplex.LockedDoor> locked = ThisFloor.Locked;
        var found = new List<long>();
        foreach (long watch in Watches())
        {
            IReadOnlyList<Egress.Move> going = Egress.Departures(
                Body, TheFloor, watch,
                CanteenRegulars.Tables(Body, TheFloor, room, watch), locked);
            if (going.Any(what))
            {
                found.Add(watch);
            }
        }

        return found;
    }

    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor to sit down on.");

    private static UndergroundComplex.FloorPlan ThisFloor =>
        UndergroundComplex.Build(Body, TheFloor, MoonSurface.ExpeditionField());

    // ── THE FLOOR ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component on a real Hive floor, on a named shift, with the clock standing at
    /// <paramref name="intoTheWatch"/> seconds into it. Nothing runs but the deck.</summary>
    private static Pages.Map OnTheFloor(long watch, double intoTheWatch)
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
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, TheFloor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        // #751's own tester cheat, applied at the one place the watch is ever frozen — so the room the deck
        // draws and the shift the walkers are dealt off are one shift, exactly as they are in play.
        Set(map, "_watchCheat", (long?)watch);
        // …and the clock hand, which is the only thing the frame contributes to the deal. The SCHEDULE is a
        // function of the frozen watch above and does not move while it is read.
        Set(map, "SimTime", intoTheWatch);

        Invoke(map, "RebuildSurfaceDeck");

        // Spend the floor's own first-entry latches, so a card the ROOM owes cannot be mistaken for one the
        // rota raised. Every one of these is one-shot per excursion and none of them is #731's.
        exType.GetProperty("HiveCantinaHallShown")!.SetValue(ex, true);
        exType.GetProperty("HiveCabinetShown")!.SetValue(ex, true);
        return map;
    }

    /// <summary>Spend frames of the shipping surface tick without moving the clock — the schedule is already
    /// frozen and the deal only needs the tick to reach it.</summary>
    private static void Play(Pages.Map map, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            Invoke(map, "StepSurface", Frame);
        }
    }

    // ── (a) NOBODY COMES IN BEFORE THE SHIFT SAYS SO, AND SOMEBODY DOES AFTERWARDS ───────────────────

    /// <summary>
    /// #731 · <b>THE ROOM KEEPS ITS OWN HOURS IN BOTH DIRECTIONS.</b> The non-vacuous pair, on the floor that
    /// ships, over sixteen of its own shifts.
    ///
    /// <para><b>The quiet half:</b> forty seconds into a watch, on every shift whose first arrival is not due
    /// for hours, not one body has come out of the back and not one chair has changed hands. A room that fills
    /// itself on frame one has no hours, it has a leak.</para>
    ///
    /// <para><b>The other half:</b> at last call, on every shift the schedule actually named somebody for,
    /// somebody has crossed the floor and is IN a chair — drawn there by the one function that answers who is
    /// in which chair, and answering the [E] press as themselves.</para>
    ///
    /// <para>Both counts are asserted, so neither half can be true because the other never happened.</para>
    ///
    /// <para><b>The RED cases.</b> Drop the <c>AtSecondsIntoWatch</c> gate in <c>DealTheArrivals</c> and the
    /// quiet half goes red on every shift; make <c>TheyComeOutOfTheBack</c> return false and the other half
    /// does. Both runs are in the pull request.</para>
    /// </summary>
    [Fact]
    public void NOBODY_ComesInBeforeTheShiftSaysSoAndSomebodyDoesAfterwards()
    {
        int quiet = 0, filled = 0, refused = 0;
        var complaints = new List<string>();

        foreach (long watch in Watches())
        {
            if (quiet >= 12 && filled >= 6)
            {
                break;   // both halves are covered; a longer sweep only costs frames.
            }

            // ── the quiet half ──
            Pages.Map early = OnTheFloor(watch, EarlyInTheWatch);
            object earlyEx = Get(early, "_surface")!;
            Play(early, 400);

            double firstDue = Due(earlyEx).Count == 0
                ? double.PositiveInfinity
                : Due(earlyEx).Min(m => m.AtSecondsIntoWatch);
            if (firstDue > EarlyInTheWatch)
            {
                quiet++;
                int inEarly = CameIn(earlyEx).Count + Afoot(earlyEx, "Arriving");
                if (inEarly != 0)
                {
                    complaints.Add(
                        $"watch {watch}: the watch is {EarlyInTheWatch:F0}s old, nobody is due for another "
                        + string.Create(CultureInfo.InvariantCulture, $"{firstDue - EarlyInTheWatch:F0}")
                        + $"s, and {inEarly} regular(s) have already walked in. A room that fills itself on "
                        + "frame one has no hours, it has a leak.");
                }
            }

            // ── the other half ──
            Pages.Map late = OnTheFloor(watch, Egress.LastCallFraction * PatronRota.WatchSeconds);
            object lateEx = Get(late, "_surface")!;
            Play(late, FrameCeiling);

            if (Due(lateEx).Count == 0)
            {
                continue;
            }

            IReadOnlyDictionary<int, string> cameIn = CameIn(lateEx);
            if (cameIn.Count == 0)
            {
                // The floor refused the walk — no standing place at that leaf, or no lattice between it and
                // the chair. That is the honest answer and never a body placed at the far end of a walk that
                // could not be walked, so it is COUNTED rather than complained about; the coverage floor below
                // is what proves it is not the answer every time.
                refused++;
                continue;
            }

            filled++;
            UndergroundComplex.Amenity room = TheRoom();
            HashSet<int> stood = StoodUp(lateEx);
            IReadOnlyList<CanteenRegulars.TableSeat> drawn =
                CanteenRegulars.Tables(Body, TheFloor, room, watch, stood, cameIn);
            IReadOnlyList<CanteenRegulars.Seated> pressed =
                CanteenRegulars.Sitting(Body, TheFloor, room, watch, stood, cameIn);
            DeckPlan plan = ThePlan(late);

            foreach ((int top, string plate) in cameIn)
            {
                CanteenRegulars.TableSeat seat = drawn.First(t => t.Index == top);
                if (!seat.Taken || !string.Equals(seat.Plate, plate, StringComparison.Ordinal))
                {
                    complaints.Add(
                        $"watch {watch}: `{plate}` walked to top {top} and the room draws it holding "
                        + $"`{seat.Plate ?? "nobody"}`.");
                }
                if (!pressed.Any(p => string.Equals(p.Plate, plate, StringComparison.Ordinal)))
                {
                    complaints.Add(
                        $"watch {watch}: [E] finds nobody called `{plate}` in the room the deck is drawing "
                        + "them in — the drawn room and the pressed room are two rooms.");
                }
                if (!plan.Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.HiveRegular
                        && string.Equals(c.Label, plate, StringComparison.Ordinal)))
                {
                    complaints.Add(
                        $"watch {watch}: `{plate}` is sitting at top {top} with no console over them. A "
                        + "person you cannot press [E] at is furniture.");
                }
            }
        }

        Assert.True(complaints.Count == 0, Report(complaints));
        Assert.True(quiet >= 12,
            $"only {quiet} of the swept shifts could exercise the quiet half — this pair is half a pair.");
        Assert.True(filled >= 6,
            $"only {filled} of the swept shifts ever put anybody in a chair ({refused} more were refused by "
            + "the floor). The room still only empties, which is the state this lane was opened to end.");
    }

    // ── (b) …AND OUT OF A LEAF THAT DOES NOT OPEN FOR YOU ───────────────────────────────────────────

    /// <summary>
    /// #731 · <b>THE DOOR THEY COME OUT OF IS ONE THE CAPTAIN'S TRY REFUSES.</b> The issue's load-bearing law:
    /// <i>"An NPC exiting through a door and that door refusing the captain ten seconds later is the whole
    /// beat, and no line of dialog may explain it."</i>
    ///
    /// <para>Every walk this floor's rota puts afoot — in either direction — carries a sign, that sign is on
    /// this floor's own <c>Locked</c> list, and the captain's own offer at such a leaf is refused for every
    /// kind of thing a satchel can hold. Asked of the walkers the shipping tick actually produced, not of a
    /// schedule read on the side.</para>
    ///
    /// <para><b>The RED case.</b> Deal the leaf out of the floor's public doors and the sweep names the sign
    /// and the shift. The run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_LEAF_TheyUseIsOneTheCaptainsTryRefuses()
    {
        var seen = new List<string>();
        var complaints = new List<string>();
        IReadOnlyList<UndergroundComplex.LockedDoor> locked = ThisFloor.Locked;

        foreach (long watch in Watches())
        {
            if (seen.Count >= 200 && seen.Contains("Arriving")
                && seen.Any(e => e is "Leaving" or "ShowingThePass"))
            {
                break;   // both directions have been walked; a longer sweep only costs frames.
            }

            Pages.Map map = OnTheFloor(watch, Egress.LastCallFraction * PatronRota.WatchSeconds);
            object ex = Get(map, "_surface")!;

            for (int frame = 0; frame < FrameCeiling; frame++)
            {
                Invoke(map, "StepSurface", Frame);
                foreach (object walker in Walkers(ex))
                {
                    NpcWalk walk = WalkOf(walker);
                    string errand = ErrandOf(walker);
                    seen.Add(errand);

                    if (!walk.For.IsADoor)
                    {
                        complaints.Add(
                            $"watch {watch}: `{walk.Plate}` is {errand} with no door in their walk at all.");
                        continue;
                    }
                    if (!locked.Any(l => string.Equals(l.Sign, walk.For.Sign, StringComparison.Ordinal)))
                    {
                        complaints.Add(
                            $"watch {watch}: `{walk.Plate}` is {errand} through `{walk.For.Sign}`, which is "
                            + "not a leaf this floor publishes as locked at all.");
                    }
                }
            }
        }

        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            Assert.False(
                SatchelTry.Offer(new Satchel.Item(kind, "test-item"), SatchelTry.Target.RoomDoor, Body).Worked,
                $"the captain's own {kind} opens a room door on this floor — the beat is a corridor.");
        }

        Assert.True(complaints.Count == 0, Report(complaints));
        Assert.True(seen.Count >= 200,
            $"only {seen.Count} walker-frame(s) in the whole sweep — this law was barely asked.");
        Assert.Contains("Arriving", seen);
        Assert.True(seen.Any(e => e is "Leaving" or "ShowingThePass"),
            "nobody left the room in the whole sweep, so half of this law was never asked.");
    }

    // ── (c) THE PASS, HELD UP TO NOBODY ────────────────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>THE GESTURE HAPPENS, AND NOTHING ANSWERS IT.</b>
    ///
    /// <para><i>"the agency temp leaving at watch change through the staff door, showing the pass nobody inside
    /// asks for."</i> This is the beat, and it is four facts driven over the shipping tick:</para>
    ///
    /// <list type="number">
    /// <item>the temp's exit is a DIFFERENT ERRAND from everybody else's — the other nine reach the leaf and
    /// are gone on that frame, and the temp stops;</item>
    /// <item>they hold for <see cref="CanteenRegulars.PassHeldSeconds"/> without moving a deck unit, and the
    /// deck goes on drawing them at the leaf the whole time;</item>
    /// <item>they are turned back INTO the room they are leaving — at the chair they got up from and not at
    /// the captain, because a pass is not shown to whoever happens to walk past;</item>
    /// <item>and <b>nothing answers</b>. Every byte the game puts in front of the player is identical on every
    /// frame of the hold to what it was on the frame before it began, and the leaf goes on refusing the
    /// captain afterwards.</item>
    /// </list>
    ///
    /// <para><b>The RED case.</b> Give the temp <c>Errand.Leaving</c> like everybody else and the hold is zero
    /// frames long. The run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_TEMP_ShowsThePassAndNobodyAsksForIt()
    {
        int shown = 0, plain = 0;
        var complaints = new List<string>();

        // WHICH SHIFTS TO PLAY IS THE SCHEDULE'S ANSWER, asked of Core's own deal off the same frozen watch
        // the room is drawn on — so this drives the shifts the game itself would and never one it wishes
        // existed. The temp is rostered AND rolled to finish about one shift in eleven on this floor, which is
        // the room's own scarcity; playing all forty-eight to find them would be four hundred thousand frames.
        List<long> theirs = ShiftsWhere(m => CanteenRegulars.ShowsThePassOnTheWayOut(m.Plate));
        List<long> everybody = ShiftsWhere(m => !CanteenRegulars.ShowsThePassOnTheWayOut(m.Plate));
        Assert.True(theirs.Count >= 3,
            $"the schedule names the agency temp on only {theirs.Count} of the swept shifts. The beat the "
            + "issue names would be unreachable in play.");
        Assert.True(everybody.Count >= 3,
            $"the schedule names anybody ELSE on only {everybody.Count} shifts, so 'the temp is different' is "
            + "a claim about a set of one.");

        foreach (long watch in theirs.Take(3).Concat(everybody.Take(2)))
        {
            Pages.Map map = OnTheFloor(watch, Egress.LastCallFraction * PatronRota.WatchSeconds);
            object ex = Get(map, "_surface")!;

            object? gesturing = null;
            for (int frame = 0; frame < FrameCeiling && gesturing is null; frame++)
            {
                Invoke(map, "StepSurface", Frame);
                foreach (object walker in Walkers(ex))
                {
                    if (ErrandOf(walker) == "ShowingThePass")
                    {
                        gesturing = walker;
                        break;
                    }
                    if (ErrandOf(walker) == "Leaving")
                    {
                        plain++;
                    }
                }
            }

            if (gesturing is null)
            {
                continue;
            }

            NpcWalk walk = WalkOf(gesturing);
            if (!CanteenRegulars.ShowsThePassOnTheWayOut(walk.Plate))
            {
                complaints.Add(
                    $"watch {watch}: `{walk.Plate}` is showing a pass and Core says they are not the one who "
                    + "does. Two opinions about one habit.");
                continue;
            }

            // Walk them to the leaf.
            int spent = 0;
            while (walk.State != NpcWalk.Doing.Arrived && spent < FrameCeiling)
            {
                Invoke(map, "StepSurface", Frame);
                spent++;
                if (!Walkers(ex).Cast<object>().Contains(gesturing))
                {
                    break;
                }
            }

            if (walk.State != NpcWalk.Doing.Arrived)
            {
                // The ground refused them somewhere between the table and the leaf. Honest, and not this
                // guard's beat — but it must never have gestured anyway.
                continue;
            }

            shown++;

            // ── THE HOLD ──
            (double x, double y) stood = (walk.X, walk.Y);
            string screenBefore = WhatIsOnTheScreen(map);
            var frames = 0;
            var moved = 0.0;
            var facings = new List<double>();
            while (Walkers(ex).Cast<object>().Contains(gesturing) && frames < FrameCeiling)
            {
                Invoke(map, "StepSurface", Frame);
                frames++;
                moved = Math.Max(moved, Math.Sqrt(
                    ((walk.X - stood.x) * (walk.X - stood.x)) + ((walk.Y - stood.y) * (walk.Y - stood.y))));
                facings.Add(walk.Facing);
                if (WhatIsOnTheScreen(map) != screenBefore)
                {
                    complaints.Add(
                        $"watch {watch}: the screen changed while `{walk.Plate}` was holding the pass up. It "
                        + $"said `{screenBefore}` and now says `{WhatIsOnTheScreen(map)}` — the room answered, "
                        + "and the whole beat is that it does not.");
                    break;
                }
            }

            double held = frames * Frame;
            if (held < CanteenRegulars.PassHeldSeconds)
            {
                complaints.Add(
                    $"watch {watch}: `{walk.Plate}` reached the leaf and was off the floor after "
                    + string.Create(CultureInfo.InvariantCulture, $"{held:F1}")
                    + "s. The gesture is the beat and it lasted "
                    + string.Create(CultureInfo.InvariantCulture, $"{frames}")
                    + " frame(s).");
            }
            if (moved > 0.01)
            {
                complaints.Add(
                    $"watch {watch}: `{walk.Plate}` wandered "
                    + string.Create(CultureInfo.InvariantCulture, $"{moved:F2}")
                    + " du while holding the pass up. They are standing at a door, not pacing.");
            }
            if (Walkers(ex).Cast<object>().Contains(gesturing))
            {
                complaints.Add(
                    $"watch {watch}: `{walk.Plate}` is still at the leaf after {frames} frame(s). Nobody asked "
                    + "for the pass, and the answer to that is that they go through — not that they wait "
                    + "forever.");
            }

            // …and they were turned back into the room the whole time, at the chair they got up from.
            double aimed = Math.Atan2(PassTo(gesturing).Y - walk.Y, PassTo(gesturing).X - walk.X);
            foreach (double facing in facings)
            {
                if (Math.Abs(Math.Atan2(Math.Sin(facing - aimed), Math.Cos(facing - aimed))) > 1e-6)
                {
                    complaints.Add(
                        $"watch {watch}: `{walk.Plate}` is facing "
                        + string.Create(CultureInfo.InvariantCulture, $"{facing:F3}")
                        + " and the room they are leaving is at "
                        + string.Create(CultureInfo.InvariantCulture, $"{aimed:F3}")
                        + ". A pass held up to a wall is not the beat.");
                    break;
                }
            }
        }

        Assert.True(complaints.Count == 0, Report(complaints));
        Assert.True(shown >= 2,
            $"the agency temp held the pass up on only {shown} of the shifts the schedule named them on. The "
            + "beat the issue names is not reaching the floor.");
        Assert.True(plain >= 1,
            "nobody in the whole sweep left the room the ordinary way, so 'the temp is DIFFERENT' is a claim "
            + "about a set of one.");
    }

    // ── (d) THE CAPTAIN IN THE DOORWAY ─────────────────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>STAND IN THE WAY AND THEY STOP, WAIT, AND LOOK AT YOU.</b> The ruling on this issue said out
    /// loud, asked of the canteen: <i>"a departing NPC blocked by the captain in the doorway stops, waits, and
    /// looks at you — that IS content; they never clip through."</i>
    ///
    /// <para>The captain is put where the walk ends. The body comes up the room, stops, says
    /// <see cref="NpcWalk.Doing.Waiting"/>, never comes inside one body-width, and never finishes — and then
    /// the captain steps aside and the same walk finishes itself.</para>
    ///
    /// <para><b>The RED case.</b> Plan the scheduled walk with <c>NpcWalk.NoPersonalSpace</c> and the walk
    /// finishes with the captain standing on the spot. The run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_BLOCKED_DoorwayIsABodyThatWaitsAndNeverOneThatClips()
    {
        int blocked = 0;
        var complaints = new List<string>();

        foreach (long watch in ShiftsWhere(_ => true).Take(5))
        {
            Pages.Map map = OnTheFloor(watch, Egress.LastCallFraction * PatronRota.WatchSeconds);
            object ex = Get(map, "_surface")!;

            object? going = null;
            for (int frame = 0; frame < FrameCeiling && going is null; frame++)
            {
                Invoke(map, "StepSurface", Frame);
                going = Walkers(ex).Cast<object>()
                    .FirstOrDefault(w => ErrandOf(w) is "Leaving" or "ShowingThePass");
            }

            if (going is null)
            {
                continue;
            }

            NpcWalk walk = WalkOf(going);
            Set(map, "_avatarX", walk.For.X);
            Set(map, "_avatarY", walk.For.Y);
            blocked++;

            double closest = double.MaxValue;
            for (int frame = 0; frame < 600; frame++)
            {
                Invoke(map, "StepSurface", Frame);
                double dx = walk.X - walk.For.X, dy = walk.Y - walk.For.Y;
                closest = Math.Min(closest, Math.Sqrt((dx * dx) + (dy * dy)));
                if (!Walkers(ex).Cast<object>().Contains(going))
                {
                    complaints.Add(
                        $"watch {watch}: the captain is standing on the doorstep and `{walk.Plate}` went "
                        + "through him anyway — the walk finished.");
                    break;
                }
            }

            if (!Walkers(ex).Cast<object>().Contains(going))
            {
                continue;
            }

            if (walk.State != NpcWalk.Doing.Waiting)
            {
                complaints.Add(
                    $"watch {watch}: `{walk.Plate}` is {walk.State} with the captain in their way, "
                    + string.Create(CultureInfo.InvariantCulture,
                        $"({walk.X:F2},{walk.Y:F2}) bound for ({walk.For.X:F2},{walk.For.Y:F2}), "
                        + $"closest {closest:F2} du, {walk.Route.Count} point(s) of route")
                    + ". The answer is to stop and look at you.");
            }
            if (closest < DeckPlan.AvatarRadius * NpcWalk.PersonalSpaceInRadii * 0.99)
            {
                complaints.Add(
                    $"watch {watch}: `{walk.Plate}` came within "
                    + string.Create(CultureInfo.InvariantCulture, $"{closest:F2}")
                    + " du of the captain. Two bodies of radius "
                    + string.Create(CultureInfo.InvariantCulture, $"{DeckPlan.AvatarRadius:F2}")
                    + " are inside one another there.");
            }

            double looking = Math.Atan2(
                (double)Get(map, "_avatarY")! - walk.Y, (double)Get(map, "_avatarX")! - walk.X);
            if (Math.Abs(Math.Atan2(Math.Sin(walk.Facing - looking), Math.Cos(walk.Facing - looking))) > 1e-6)
            {
                complaints.Add(
                    $"watch {watch}: `{walk.Plate}` is blocked and is not looking at you. Being looked at is "
                    + "the content.");
            }

            // …and step aside: the same walk, unbroken, finishes itself.
            Set(map, "_avatarX", walk.For.X + 40.0);
            Set(map, "_avatarY", walk.For.Y + 40.0);
            for (int frame = 0; frame < FrameCeiling && Walkers(ex).Cast<object>().Contains(going); frame++)
            {
                Invoke(map, "StepSurface", Frame);
            }

            if (Walkers(ex).Cast<object>().Contains(going))
            {
                complaints.Add(
                    $"watch {watch}: the captain stepped aside and `{walk.Plate}` never finished. A yield is a "
                    + "refusal to step, not a broken route.");
            }
        }

        Assert.True(complaints.Count == 0, Report(complaints));
        Assert.True(blocked >= 3,
            $"only {blocked} shift(s) put anybody in a doorway to block — this law was barely asked.");
    }

    // ── (e) AND NOT ONE LINE EXPLAINS ANY OF IT ────────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>THE ROOM TOLD YOU SOMETHING AND THE GAME DID NOT.</b>
    ///
    /// <para>The issue's hardest sentence — <i>"no line of dialog may explain it"</i> — asked of everything
    /// this lane adds, and asked as a DIFFERENTIAL rather than as a grep, because a grep passes on any wording
    /// nobody thought of. One watch of one room is played twice over the shipping tick with the same seeds and
    /// the same clock: once with the shift's whole churn dealt, once with it suppressed. Everything the game
    /// put in front of the player is transcribed on every frame and compared byte for byte.</para>
    ///
    /// <para>The dealt run is required to have actually churned — somebody in, somebody out, and the pass
    /// shown at least once across the sweep — so the two transcripts cannot be equal for the wrong
    /// reason.</para>
    ///
    /// <para><b>The RED case.</b> Plant one line on the arrival —
    /// <c>ShowPulseMessage("🚪 Staff only. That's why.")</c> in <c>TheyComeOutOfTheBack</c> — and the
    /// transcripts part on the frame the leaf opens. The run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void NOT_ONE_LineExplainsTheRotasTurnover()
    {
        int churned = 0, gestured = 0;

        foreach (long watch in Watches())
        {
            (string Said, int In, int Out, int Pass) dealt = OneWatchOfTheRoom(watch, suppressed: false);
            (string Said, int In, int Out, int Pass) quiet = OneWatchOfTheRoom(watch, suppressed: true);

            Assert.Equal(0, quiet.In);
            Assert.Equal(0, quiet.Out);
            Assert.Equal(quiet.Said, dealt.Said);

            if (dealt.In > 0 && dealt.Out > 0)
            {
                churned++;
            }
            gestured += dealt.Pass;
        }

        Assert.True(churned >= 2,
            $"only {churned} of the swept shifts had somebody both come in and go out. The two transcripts "
            + "would be the same text for the wrong reason, which is a green test that asserts nothing.");
        Assert.True(gestured >= 1,
            "the pass was never shown in any of the swept shifts, so the quietest thing this lane adds was "
            + "never in the transcript at all.");
    }

    /// <summary>Play one watch of the canteen at last call and transcribe everything the game said. The
    /// suppressed run is the SAME room on the SAME shift with only the deal held back — both directions
    /// pre-spent — so the two runs differ in one fact and nothing else.</summary>
    private static (string Said, int In, int Out, int Pass) OneWatchOfTheRoom(long watch, bool suppressed)
    {
        Pages.Map map = OnTheFloor(watch, Egress.LastCallFraction * PatronRota.WatchSeconds);
        object ex = Get(map, "_surface")!;
        Type exType = ex.GetType();

        if (suppressed)
        {
            var already = (HashSet<int>)exType.GetProperty("HallDeparted")!.GetValue(ex)!;
            for (int i = 0; i < 64; i++)
            {
                already.Add(i);
            }
            var arrived = (HashSet<string>)exType.GetProperty("HallArrived")!.GetValue(ex)!;
            foreach (string plate in CanteenRegulars.AllProse())
            {
                arrived.Add(plate);
            }
            exType.GetProperty("WalkersWatch")!.SetValue(ex, watch);
            exType.GetProperty("WalkersFloor")!.SetValue(ex, TheFloor);
        }

        var said = new StringBuilder();
        int pass = 0;

        // The SAME number of frames both times, always — a run that stopped early because the room went quiet
        // would make the two transcripts different lengths for a reason that is not the law.
        for (int frame = 0; frame < 700; frame++)
        {
            Invoke(map, "StepSurface", Frame);
            pass += Afoot(ex, "ShowingThePass");
            said.Append(frame).Append('|').Append(WhatIsOnTheScreen(map)).Append('\n');
        }

        return (said.ToString(), CameIn(ex).Count, StoodUp(ex).Count, pass);
    }

    /// <summary>Everything the game is putting in front of the player, as one line — the pulse's own words,
    /// the centred card's label and body, and the story card's beat.</summary>
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

    private static IList Walkers(object ex) =>
        (IList)ex.GetType().GetProperty("Walkers", Hidden)!.GetValue(ex)!;

    private static IReadOnlyDictionary<int, string> CameIn(object ex) =>
        (Dictionary<int, string>)ex.GetType().GetProperty("HallCameIn", Hidden)!.GetValue(ex)!;

    private static HashSet<int> StoodUp(object ex) =>
        (HashSet<int>)ex.GetType().GetProperty("HallStoodUp", Hidden)!.GetValue(ex)!;

    private static IReadOnlyList<Egress.Move> Due(object ex) =>
        (IReadOnlyList<Egress.Move>?)ex.GetType().GetProperty("HallArrivals", Hidden)!.GetValue(ex) ?? [];

    private static int Afoot(object ex, string errand) =>
        Walkers(ex).Cast<object>().Count(w => ErrandOf(w) == errand);

    private static NpcWalk WalkOf(object walker) =>
        (NpcWalk)walker.GetType().GetProperty("Walk", Hidden)!.GetValue(walker)!;

    private static string ErrandOf(object walker) =>
        walker.GetType().GetProperty("For", Hidden)!.GetValue(walker)!.ToString()!;

    private static (double X, double Y) PassTo(object walker) =>
        ((double)walker.GetType().GetProperty("PassToX", Hidden)!.GetValue(walker)!,
         (double)walker.GetType().GetProperty("PassToY", Hidden)!.GetValue(walker)!);

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

    private static string Report(IReadOnlyList<string> complaints) =>
        complaints.Count == 0
            ? ""
            : $"{complaints.Count} complaint(s):\n" + string.Join("\n", complaints.Take(12));

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
