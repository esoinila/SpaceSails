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
/// #770 · BOOK A ROOM AT THE COUNTER — driven end to end on a real floor.
///
/// <para>The block's park-view suites have said <c>NEGOTIATION ROOM · BOOK AT THE COUNTER</c> since #816 with
/// nowhere to do it. Everything below is asked of a REAL <see cref="Pages.Map"/> on a REAL generated Hive
/// floor — the harness <c>CoSeatingIsAStripTests</c> established one file over — with the counter opened by
/// the shipping verb, the room booked by the shipping press and the chair taken with the shipping [E]. A
/// guard that read <c>Map.razor</c> for the word <c>seated-dock</c> would pass on a build where the booked
/// table opened a modal over the whole hall, which is the one thing #865 ruled it may never do.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBookedRoomIsAStripTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    /// <summary>The sites this file will walk looking for a floor that can reach a particular state. Luna is
    /// first and is where every single-site guard runs; the rest exist because ONE of the two rooms this
    /// feature opens — the one with nobody across the table — is a fact about a HALL being empty, and luna
    /// B1's hall has somebody in it on every watch the rota has.</summary>
    private static readonly string[] Sites =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    /// <summary>A purse that can afford either room twice over, so nothing below is measuring an empty
    /// pocket by accident.</summary>
    private const int APurse = 500;

    private static int TheFloor => FloorOf(Body);

    private static int FloorOf(string body) => UndergroundComplex.TopPressurisedFloor(body)
        ?? throw new InvalidOperationException($"{body} has no pressurised floor to book a room on.");

    // ── THE FLOOR, THE COUNTER AND THE PRESS ──────────────────────────────────────────────────────────

    /// <summary>A live component on a real Hive floor. <c>CoSeatingIsAStripTests.OnTheFloor</c> verbatim,
    /// with one addition: the watch can be pinned, because half of what this feature is about is which shift
    /// the room was taken on.</summary>
    private static Pages.Map OnTheFloor(long? watch = null, string body = Body)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on " +
                "has moved, and the verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, FloorOf(body));

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_credits", APurse);
        if (watch is { } pinned)
        {
            Set(map, "_watchCheat", (long?)pinned);
        }

        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>…and standing at the counter with the card open, through the shipping opener, so the card
    /// the press lands on is the one a captain would be looking at.</summary>
    private static Pages.Map AtTheCounter(long? watch = null, string body = Body)
    {
        Pages.Map map = OnTheFloor(watch, body);
        Barkeep counter = CounterService.For(body, UndergroundComplex.Comfort.UpperCanteen)
            ?? throw new InvalidOperationException(
                $"{body}'s upper canteen has no counter — this whole file is about a verb on its card.");
        Invoke(map, "OpenCounterService", counter);
        Assert.True((bool)Get(map, "TheCounterRentsRooms")!,
            "the Hive's own counter says it does not rent rooms, so the two buttons this issue adds are " +
            "not drawn on any card in the game.");
        return map;
    }

    /// <summary>The booking the captain is holding right now, off the counter's own book.</summary>
    private static RoomBooking.Booking? Held(Pages.Map map) =>
        (RoomBooking.Booking?)Invoke(map, "TheRoomYouHold", Get(map, "_surface"));

    private static long WatchOn(Pages.Map map) =>
        (long)Get(map, "_surface")!.GetType().GetProperty("CanteenWatch")!.GetValue(Get(map, "_surface"))!;

    private static UndergroundComplex.RingRoom RoomOn(int number, string body = Body)
    {
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(body, FloorOf(body), MoonSurface.ExpeditionField());
        Assert.True(floor.Park is { } green && green.Frontage.Any(r => r.Number == number),
            $"room {number} is not on {body} B{-FloorOf(body)}'s ring at all.");
        return floor.Park!.Value.Frontage.Single(r => r.Number == number);
    }

    /// <summary>
    /// Book a room and sit down at its long table, with the shipping presses, on the first (site, watch) the
    /// game actually reaches the asked-for state on.
    ///
    /// <para>Both states are real and both are the feature: a room with a delegation across the table, and a
    /// room with nobody in it. The second is a fact about a HALL being empty — luna's is never empty, so the
    /// walk goes further afield rather than the guard quietly asserting the first case twice.</para>
    /// </summary>
    private static (Pages.Map Map, RoomBooking.Booking Booking, string Site) SatAtABookedTable(
        bool wantCompany)
    {
        foreach (string site in Sites)
        {
            if (UndergroundComplex.TopPressurisedFloor(site) is not { } level
                || !UndergroundComplex.HasParkBlock(site, level))
            {
                continue;
            }

            for (long watch = 0; watch < CanteenRegulars.WatchFill.Count; watch++)
            {
                Pages.Map map = AtTheCounter(watch, site);
                Invoke(map, "BookARoom", false);
                if (Held(map) is not { } booking)
                {
                    Invoke(map, "BookARoom", true);
                    if (Held(map) is not { } bigOne)
                    {
                        continue;
                    }
                    booking = bigOne;
                }

                if (!TakeAChairIn(map, booking.Room, site))
                {
                    continue;
                }

                object sitting = Get(map, "_table")!;
                bool company = !(bool)sitting.GetType().GetProperty("Solo")!.GetValue(sitting)!;
                if (company == wantCompany)
                {
                    return (map, booking, site);
                }
            }
        }

        Assert.Fail(
            $"no site in the sweep books a negotiation room and seats the captain " +
            $"{(wantCompany ? "with" : "without")} a delegation opposite — the guard below would be " +
            "asserting about a state the game cannot reach.");
        throw new InvalidOperationException("unreachable");
    }

    /// <summary>Stand on one of the room's own published chairs and press [E].</summary>
    private static bool TakeAChairIn(Pages.Map map, int number, string body = Body)
    {
        UndergroundComplex.RingRoom room = RoomOn(number, body);
        foreach (RingOffice.Chair chair in room.Seats)
        {
            Set(map, "_avatarX", chair.X);
            Set(map, "_avatarY", chair.Y);
            Invoke(map, "TryTakeOfficeChair");
            if (Get(map, "_table") is not null)
            {
                return true;
            }
        }
        return false;
    }

    // ── (a) THE PRESS TAKES COIN, ONE ROOM, AND WRITES BOTH BOOKS ─────────────────────────────────────

    /// <summary>
    /// #770 · BOOKING ONE COSTS WHAT THE LABEL SAID, HAPPENS ONCE, AND IS REMEMBERED IN TWO PLACES.
    ///
    /// <para>The captain's field book keeps a sentence the captain wrote; the COUNTER's book keeps five
    /// integers and nothing a panel could print (#715/#781's own two-book grammar). A second press is
    /// refused out loud and takes no more coin, because the counter will not write you two rooms.</para>
    ///
    /// <para><b>The RED case.</b> Move the debit after the write and press twice: the purse assertion fires
    /// with two rooms' worth gone. Drop the <c>AlreadyHeldLine</c> guard and the same assertion fires.</para>
    /// </summary>
    [Fact]
    public void BOOKING_TakesTheCoinOnce_AndWritesBothBooks()
    {
        Pages.Map map = AtTheCounter(watch: 1);
        Assert.Null(Held(map));

        Invoke(map, "BookARoom", false);
        RoomBooking.Booking booking = Held(map)
            ?? throw new InvalidOperationException(
                $"{Body} B{-TheFloor} has no small negotiation room to book — this guard needs one.");

        Assert.Equal(APurse - booking.Paid, (int)Get(map, "_credits")!);
        Assert.Equal(WatchOn(map), booking.Watch);
        Assert.Equal(TheFloor, booking.Level);
        Assert.False(booking.Big, "the small button handed over the long room.");

        // THE COUNTER'S BOOK — one entry, machine-readable, under the counter's own entity.
        var contacts = (ContactLedger)Get(map, "_contacts")!;
        string[] entries = [.. contacts.For(RoomBooking.LedgerId).KnownTells];
        Assert.Single(entries);
        Assert.Equal(RoomBooking.BookedEntry(in booking), entries[0]);

        // THE CAPTAIN'S BOOK — a sentence, and the one the watch calls for.
        Assert.Contains(RoomBooking.WhoBookedNote(booking.Room, booking.Watch), TheFieldBook(map));

        // …and a second press changes nothing at all.
        int purse = (int)Get(map, "_credits")!;
        Invoke(map, "BookARoom", false);
        Invoke(map, "BookARoom", true);
        Assert.Equal(purse, (int)Get(map, "_credits")!);
        Assert.Single(contacts.For(RoomBooking.LedgerId).KnownTells);
        Assert.Equal(
            RoomBooking.AlreadyHeldLine(booking.Room), (string?)Get(map, "_barNotice"));
    }

    // ── (b) THE PLATE AND THE DOGGED LEAVES, UNTIL THE WATCH TURNS ────────────────────────────────────

    /// <summary>
    /// #770 · THE PLAN SAYS THE ROOM IS TAKEN — and stops saying it when the shift does.
    ///
    /// <para>Three facts, each failing on its own: the booked plate is stencilled beside the room's door and
    /// nowhere else; every one of that room's street leaves is drawn dogged; and neither survives the watch
    /// turning, because the building rents by the watch and takes it back without saying anything.</para>
    ///
    /// <para><b>The RED case, both ways.</b> Take the watch comparison out of
    /// <see cref="RoomBooking.HeldOn"/> and the release assertion fires with the plate still on the plan a
    /// shift later. Take the leaf loop out of <c>HiveInterior</c> and the dogged-door count goes to zero.</para>
    /// </summary>
    [Fact]
    public void THE_DECK_DrawsTheBookedPlateAndTheDoggedLeaves_UntilTheWatchTurns()
    {
        Pages.Map map = AtTheCounter(watch: 1);

        int lockedBefore = LockedDoors(map).Count;
        Assert.DoesNotContain(ThePlan(map).RoomLabels, l => l.Text.Contains("BOOKED", StringComparison.Ordinal));

        Invoke(map, "BookARoom", false);
        RoomBooking.Booking booking = Held(map)
            ?? throw new InvalidOperationException("nothing was booked, so there is no plate to look for.");
        UndergroundComplex.RingRoom room = RoomOn(booking.Room);

        (float X, float Y, string Text)[] booked =
            [.. ThePlan(map).RoomLabels.Where(l => l.Text.Contains("BOOKED", StringComparison.Ordinal))];
        Assert.Single(booked);
        Assert.Equal(booking.Plate, booked[0].Text);
        Assert.True(
            Math.Abs(booked[0].X - room.Door.X1) < 0.5 && Math.Abs(booked[0].Y - room.Door.Y1) < 0.5,
            "the BOOKED plate is not stencilled beside the room's own door — a captain reading the plan " +
            $"would look for room {booking.Room} somewhere else on the frontage.");
        Assert.True(RoomBooking.TryReadPlate(booked[0].Text, out long onThePlate, out _)
            && onThePlate == booking.Watch,
            "the plate on the plan does not name the shift the room was taken on, and the SEAT reads this " +
            "very label to know whose room it is sitting in.");

        List<DeckPlan.Door> locked = LockedDoors(map);
        Assert.Equal(lockedBefore + room.Doors.Count, locked.Count);
        foreach (SurfaceLayout.Doorway leaf in room.Doors)
        {
            Assert.Contains(locked, d =>
                Math.Abs(d.X1 - (float)leaf.X1) < 0.01f && Math.Abs(d.Y1 - (float)leaf.Y1) < 0.01f);
        }

        // …AND THE WATCH TURNS. Same component, same book, one shift later: the room is the building's
        // again and the plan says so.
        Set(map, "_watchCheat", (long?)(booking.Watch + 1));
        Invoke(map, "RebuildSurfaceDeck");

        Assert.Null(Held(map));
        Assert.DoesNotContain(ThePlan(map).RoomLabels, l => l.Text.Contains("BOOKED", StringComparison.Ordinal));
        Assert.Equal(lockedBefore, LockedDoors(map).Count);

        // …and the book has NOT un-written itself. The counter remembers every booking ever made; what the
        // turn of the watch changed is which one is true now.
        Assert.Single(((ContactLedger)Get(map, "_contacts")!).For(RoomBooking.LedgerId).KnownTells);
    }

    // ── (d) THE BOOKED TABLE IS THE SAME STRIP, WITH THE DELEGATION'S MOVES ADDED ─────────────────────

    /// <summary>
    /// #770/#865 · SITTING DOWN WITH A DELEGATION DOCKS — it does not take the screen.
    ///
    /// <para>Owner's ruling that this whole feature is sequenced behind: <i>"It should somehow UI wise be
    /// same style as the sitting alone case"</i> and <i>"Just the social functions as additional
    /// options."</i> A deal across a booked table is exactly that case with more company and a fourth
    /// button, so it is the same component — <c>CoSeatingIsAStripTests</c>'s own proof shape, applied to the
    /// room this issue built.</para>
    ///
    /// <para>Five facts, each failing on its own: the frame is the docked one; the occupancy fact is
    /// unchanged (somebody IS opposite); the three social moves are on the row; the deal move is on it too;
    /// and the room is still drawing behind it.</para>
    ///
    /// <para><b>The RED case.</b> Hand the booked sitting <c>TheyCameToYou = true</c> — the one flag the
    /// razor forks the backdrop on — and the first assertion fires with the whole hall behind a scrim.</para>
    /// </summary>
    [Fact]
    public void THE_BOOKED_TABLE_IsTheSameStripWithTheDelegationsMovesAdded()
    {
        (Pages.Map map, RoomBooking.Booking booking, string site) = SatAtABookedTable(wantCompany: true);
        int fixtures = ThePlan(map).Consoles.Count();

        Assert.True((bool)Get(map, "SeatedIsDocked")!,
            "sitting down across a booked table raised a card over the room — the owner's ruling is that " +
            "this presents like the sitting-alone case, and #865 spent a whole issue getting there.");
        Assert.False((bool)Get(map, "SeatedIsAConversation")!,
            "the booked table still counts as a conversation, so the card branch and its backdrop render.");

        object sitting = Get(map, "_table")!;
        Assert.False((bool)sitting.GetType().GetProperty("Solo")!.GetValue(sitting)!,
            "the strip reports the captain as sitting alone in a room with a delegation across the table — " +
            "the frame was fixed by lying about the room.");

        var moves = (IReadOnlyList<Encounter.Move>)Invoke(map, "TableMovesOnTheTable")!;
        string[] ids = [.. moves.Select(m => m.Id)];
        foreach (string social in new[] { CanteenTable.SmallTalk, CanteenTable.Round, CanteenTable.Leave })
        {
            Assert.True(ids.Contains(social),
                $"the strip offers no `{social}` at the booked table. Offered: {string.Join(", ", ids)}");
        }
        Assert.True(ids.Contains(RoomBooking.PutItToThem),
            $"the one thing this room is FOR is not on the row. Offered: {string.Join(", ", ids)}");

        Assert.Equal(fixtures, ThePlan(map).Consoles.Count());
        Assert.True(Invoke(map, "SeatedCustomerLine") is string { Length: > 0 },
            "the booked table draws no customer line, so this is a second component after all.");
        Assert.True(Invoke(map, "SeatedCompanyLine") is string { Length: > 0 },
            "nothing on the strip says who the captain is across the table from.");

        // …and the deal move SPEAKS, through the framework's own fixed-outcome path and not a case in a
        // switch. #749's law: a move that carries a line is a move that lands.
        Invoke(map, "TableMove", RoomBooking.PutItToThem);
        Assert.Equal(
            RoomBooking.PutItToThemLine,
            (string?)sitting.GetType().GetProperty("Outcome")!.GetValue(sitting));
        Assert.True((bool)Get(map, "SeatedIsDocked")!, "putting it to them raised a card.");

        // …and the empty room is the other reachable half of the same verb.
        (Pages.Map alone, RoomBooking.Booking _, string _) = SatAtABookedTable(wantCompany: false);
        object solo = Get(alone, "_table")!;
        Assert.Equal(
            RoomBooking.RoomIsYoursAndEmptyLine,
            (string?)solo.GetType().GetProperty("Outcome")!.GetValue(solo));
        Assert.DoesNotContain(
            RoomBooking.PutItToThem,
            ((IReadOnlyList<Encounter.Move>)Invoke(alone, "TableMovesOnTheTable")!).Select(m => m.Id));
        Assert.True(booking.Room > 0);
    }

    // ── (e) A DIG AT THE BOOKED TABLE FILES UNDER THE DELEGATION ─────────────────────────────────────

    /// <summary>
    /// #770/#715 · WORKING A CASE IN FRONT OF SOMEBODY IS A THING THAT HAPPENED TO THAT SOMEBODY.
    ///
    /// <para>The captain's field book gets the write-up as always; the DELEGATION's own ledger entry gets a
    /// machine id and nothing a panel could print — #781's grammar at this counter, one room along. The dig
    /// is driven through the shipping spread verb on the shipping hold, so what is asserted is the state a
    /// captain's press actually leaves.</para>
    ///
    /// <para><b>The RED case.</b> File under <c>RoomBooking.LedgerId</c> instead of the person opposite and
    /// the entity assertion fires: the counter would be remembering a conversation it was not in.</para>
    /// </summary>
    [Fact]
    public void A_DIG_AtTheBookedTable_FilesUnderTheDelegation()
    {
        (Pages.Map map, RoomBooking.Booking booking, string site) = SatAtABookedTable(wantCompany: true);
        object sitting = Get(map, "_table")!;
        string them = (string)sitting.GetType().GetProperty("Plate")!.GetValue(sitting)!;

        var contacts = (ContactLedger)Get(map, "_contacts")!;
        Assert.Empty(contacts.For(them).KnownTells);

        Assert.True((bool)Get(map, "CanSpreadTheCaseHere")!,
            "the case may not be spread out in a room the captain paid for and shut the door of — a booked " +
            "room is cabinet class (#758) and the ladder has stopped reading it as one.");

        Invoke(map, "SeedTheSpreadFinds");
        var sleeve = (List<Satchel.Item>)Get(map, "_satchel")!;
        Assert.NotEmpty(sleeve);

        // The dig, on the shipping hold, run to the end the way a captain's twenty seconds run.
        Invoke(map, "WriteItUp", sleeve[0]);
        for (int frame = 0; frame < 4000 && Get(map, "_table") is not null; frame++)
        {
            Invoke(map, "StepSurface", 0.05);
            if (contacts.For(them).KnownTells.Length > 0)
            {
                break;
            }
        }

        string[] filed = [.. contacts.For(them).KnownTells];
        Assert.True(filed.Length == 1,
            $"the dig at the booked table filed {filed.Length} entries under “{them}”. It should file " +
            "exactly one, under the person it was put to.");
        Assert.Equal(RoomBooking.DugAgainstThem(booking.Room, booking.Watch), filed[0]);

        // …and it is NOT filed under the counter, which was not in the room.
        Assert.Single(contacts.For(RoomBooking.LedgerId).KnownTells);
    }

    // ── (f) #758's LAW APPLIES IN A ROOM YOU PAID FOR ────────────────────────────────────────────────

    /// <summary>
    /// #770/#758 · A BOOKED ROOM IS A CABINET-CLASS QUIET SPACE — curtain by default, and the door is a
    /// visible choice.
    ///
    /// <para>Owner's own ruling on the cabinets: <i>"something that prevents the sound or sight catching
    /// what happens there too easily but is not a real door."</i> A room rented by the watch gets the same
    /// ladder, on a leaf ordinal of its own so dogging it can never dog a cabinet off the hall — and the
    /// counter's note names the room rather than a cabinet number, because a book that got the building
    /// wrong would be the oldest fault this repository has.</para>
    ///
    /// <para><b>The RED case.</b> Take the <c>Cabinet</c> ordinal off the booked sitting and the leaf button
    /// vanishes; point the note at <c>CabinetPrivacy.WhoWasInsideNote</c> and the sentence names a cabinet
    /// four hundred and something, which the last assertion catches.</para>
    /// </summary>
    [Fact]
    public void THE_BOOKED_ROOM_IsCabinetClass_CurtainByDefault_AndTheDoorIsSaidPlainly()
    {
        (Pages.Map map, RoomBooking.Booking booking, string site) = SatAtABookedTable(wantCompany: true);

        Assert.True((bool)Get(map, "ACabinetLeafToWork")!,
            "a room the captain paid for has no leaf to work — the whole of what the coin bought was a door.");
        Assert.Equal(CabinetPrivacy.Stage.Curtain, (CabinetPrivacy.Stage)Get(map, "CabinetStage")!);
        Assert.Equal(CabinetPrivacy.DogTheDoorLabel, (string)Get(map, "CabinetLeafLabel")!);

        Invoke(map, "WorkTheCabinetLeaf");

        Assert.Equal(CabinetPrivacy.Stage.Door, (CabinetPrivacy.Stage)Get(map, "CabinetStage")!);
        Assert.Equal(CabinetPrivacy.DrawTheCurtainLabel, (string)Get(map, "CabinetLeafLabel")!);

        // A DOGGED DOOR NEVER LEAKS, and the cloth sometimes does. Core owns both halves; what is checked
        // here is that the booked room is actually running them.
        object sitting = Get(map, "_table")!;
        int leaf = (int)sitting.GetType().GetProperty("Cabinet")!.GetValue(sitting)!;
        Assert.True(RoomBooking.IsABookedLeaf(leaf));
        Assert.Equal(booking.Room, RoomBooking.RoomOfLeaf(leaf));

        for (int beat = 0; beat < 200; beat++)
        {
            Assert.False(
                CabinetPrivacy.Leaks(Body, leaf, booking.Watch, beat, CabinetPrivacy.Stage.Door),
                $"beat {beat} leaked out of a dogged door in a room the captain paid for.");
        }
        Assert.Contains(
            Enumerable.Range(0, 400),
            beat => CabinetPrivacy.Leaks(Body, leaf, booking.Watch, beat, CabinetPrivacy.Stage.Curtain));

        // …and the counter wrote the door down as a ROOM, never as a cabinet number nobody could find.
        string note = RoomBooking.DoggedTheDoorNote(booking.Room, booking.Watch);
        Assert.Contains(note, TheFieldBook(map));
        Assert.DoesNotContain("cabinet", note, StringComparison.OrdinalIgnoreCase);
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

    private static List<DeckPlan.Door> LockedDoors(Pages.Map map) =>
        [.. ThePlan(map).Doors.Where(d => d.Locked)];

    /// <summary>Every sentence in the captain's own field book right now, however the page happens to keep
    /// it — the guard is about what was WRITTEN, not about the shape of the list.</summary>
    private static List<string> TheFieldBook(Pages.Map map)
    {
        var said = new List<string>();
        foreach (object? entry in (IEnumerable)Get(map, "_fieldNotes")!)
        {
            if (entry is null)
            {
                continue;
            }
            foreach (PropertyInfo p in entry.GetType().GetProperties(Hidden))
            {
                if (p.PropertyType == typeof(string) && p.GetValue(entry) is string { Length: > 0 } text)
                {
                    said.Add(text);
                }
            }
        }
        return said;
    }

    /// <summary>A field OR a property, on the page OR on the seat object hanging off it — the same law
    /// <see cref="Invoke"/> keeps for verbs, because #870 lane 6c moved the state and the readings together
    /// and a guard about a booking should not care which partial the author put a reading on. Deliberately
    /// NOT a fallback for any name: a reading that has been DELETED still fails loudly right here.</summary>
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
        if (o.GetType().GetProperty(name, Hidden) is { } prop)
        {
            return prop.GetValue(o);
        }
        if (o is Pages.Map page && SeatState.Seat(page) is { } seat)
        {
            if (seat.GetType().GetProperty(name, Hidden) is { } onTheSeat)
            {
                return onTheSeat.GetValue(seat);
            }
            if (seat.GetType().GetField(name, Hidden) is { } seatField)
            {
                return seatField.GetValue(seat);
            }
        }
        Assert.Fail($"the component has no `{name}` — this guard is reading a dead name.");
        return null;
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    /// <summary>#870 lane 6c's own lookup: a verb that is not on the page is asked of the seat object
    /// hanging off it. Deliberately not a fallback for any name — a deleted verb still fails loudly.</summary>
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
