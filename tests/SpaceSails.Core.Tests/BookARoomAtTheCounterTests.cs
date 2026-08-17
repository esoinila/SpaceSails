using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #770 · BOOK A ROOM AT THE COUNTER — the laws, measured against the building the generator actually makes.
///
/// <para>The block has stencilled <c>NEGOTIATION ROOM · BOOK AT THE COUNTER</c> on its park-view suites since
/// #816 with nowhere to do it. What is guarded here is the grammar that arrived: a price, a pick, a plate,
/// a door law, the counter's forked note, and who ends up sitting opposite.</para>
///
/// <para><b>Nothing below is stated against a coordinate or a room number typed into this file.</b> Every
/// sweep walks the real generator over the real field and asks Core's own published questions, because a law
/// measured off a number typed in a test is this house's fifth named bug class.</para>
/// </summary>
public sealed class BookARoomAtTheCounterTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every floor in the sweep that has a block with a ring round it.</summary>
    private static IEnumerable<(string Body, int Level, IReadOnlyList<UndergroundComplex.RingRoom> Ring)>
        EveryRing()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || !UndergroundComplex.HasParkBlock(body, level))
            {
                continue;
            }
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            if (floor.Park is { } green && green.Frontage.Count > 0)
            {
                yield return (body, level, green.Frontage);
            }
        }
    }

    /// <summary>Every watch the rota has, so a law about the shift is asked about every shift there is.</summary>
    private static IEnumerable<long> EveryWatch() =>
        Enumerable.Range(0, CanteenRegulars.WatchFill.Count).Select(i => (long)i);

    // ── THE FORK IS REAL, WHICH IS WHY THE FEATURE HAS TWO PRICES ─────────────────────────────────────

    /// <summary>
    /// #770 · BOTH KINDS OF NEGOTIATION ROOM EXIST IN THE BUILDING THE GENERATOR MAKES.
    ///
    /// <para><b>This guard is why <see cref="RoomBooking.IsTheBigRoom"/> is not about the glass.</b> The
    /// issue's own draft asked for the PARK WINDOW to be the status marker — but <c>RingBox</c> only ever
    /// reaches for <see cref="UndergroundComplex.ParkViewPlates"/> when the room HAS the view, so every
    /// negotiation room in the game has glass and "with glass / without" would have been a threshold that
    /// selects everything: a guard that cannot tell pass from fail, which this repository has a name for.
    /// The second assertion below is that fault, kept as a live measurement rather than as a claim in a
    /// comment.</para>
    ///
    /// <para><b>The RED case.</b> Point <c>IsTheBigRoom</c> at <c>room.HasView</c> and the "small ones" half
    /// of the first assertion goes red on every site in the sweep, because there are none.</para>
    /// </summary>
    [Fact]
    public void THE_TWO_PRICES_AreTwoRoomsTheBuildingActuallyHas()
    {
        int big = 0, small = 0, glazed = 0, unglazed = 0;

        foreach ((string _, int _, IReadOnlyList<UndergroundComplex.RingRoom> ring) in EveryRing())
        {
            foreach (UndergroundComplex.RingRoom room in ring)
            {
                if (!RoomBooking.IsANegotiationRoom(in room))
                {
                    continue;
                }
                if (RoomBooking.IsTheBigRoom(in room)) { big++; } else { small++; }
                if (room.HasView) { glazed++; } else { unglazed++; }
            }
        }

        Assert.True(big > 0 && small > 0,
            $"the two prices are not two rooms: the sweep found {big} long negotiation room(s) and {small} " +
            "small one(s). One of the counter's two buttons can never be pressed to any effect, and the " +
            "guards below that measure the fork are measuring nothing.");

        Assert.True(glazed > 0 && unglazed == 0,
            $"the sweep found {unglazed} negotiation room(s) with no view — the issue's PARK WINDOW fork " +
            "would have had two sides after all, and RoomBooking.IsTheBigRoom is now measuring the wrong " +
            "thing. Read the docblock on it before changing this number.");
    }

    // ── (a) THE BOOKING TAKES COIN AND EXACTLY ONE ROOM ───────────────────────────────────────────────

    /// <summary>
    /// #770 · ONE PRESS, ONE ROOM, OF THE KIND THAT WAS ASKED FOR — and the price follows the ROOM.
    ///
    /// <para>Three facts and each fails on its own: the pick returns a room that is really on this ring and
    /// really a negotiation room; the kind it returns is the kind that was asked for (strictly — a button
    /// that says 24 cr must never hand over the 40 cr room); and a room already taken is not handed out
    /// twice.</para>
    ///
    /// <para><b>The RED case.</b> Drop the <c>IsTheBigRoom(in room) == wantBig</c> clause from
    /// <see cref="RoomBooking.RoomYouGet"/> and the kind assertion fires on the first site whose ring puts a
    /// small negotiation room ahead of a long one; drop the <c>taken</c> clause and the last one fires
    /// everywhere.</para>
    /// </summary>
    [Fact]
    public void THE_PICK_IsOneRoomOfTheKindAskedFor_AndNeverTheSameOneTwice()
    {
        int asked = 0;

        foreach ((string body, int level, IReadOnlyList<UndergroundComplex.RingRoom> ring) in EveryRing())
        {
            foreach (bool wantBig in new[] { true, false })
            {
                if (RoomBooking.RoomYouGet(ring, wantBig) is not { } room)
                {
                    continue;   // this floor has none of that kind; the counter refuses out loud.
                }
                asked++;

                UndergroundComplex.RingRoom got = ring.Single(r => r.Number == room);
                Assert.True(RoomBooking.IsANegotiationRoom(in got),
                    $"{body} B{-level}: the counter handed over room {room}, whose plate reads " +
                    $"“{got.Plate}”. That is not a room this building rents.");
                Assert.True(RoomBooking.IsTheBigRoom(in got) == wantBig,
                    $"{body} B{-level}: asked for the {(wantBig ? "long" : "small")} room and got room " +
                    $"{room}, which is the other one — at the other price. The label on the button and the " +
                    "coin that leaves the purse are now two different rooms.");
                Assert.Equal(wantBig, RoomBooking.BigOn(ring, room));
                Assert.Equal(RoomBooking.PriceOf(wantBig), RoomBooking.PriceOf(RoomBooking.BigOn(ring, room)));

                // …and it is not handed out again while somebody has it.
                int? second = RoomBooking.RoomYouGet(ring, wantBig, new[] { room });
                Assert.True(second != room,
                    $"{body} B{-level}: room {room} was booked and the counter offered it again.");
            }
        }

        Assert.True(asked > 8,
            $"only {asked} floor/kind pairs in the whole sweep could be booked at all — this guard is " +
            "asserting about a building the generator has stopped making.");
    }

    /// <summary>#770 · The two prices are two numbers, the big one is the dearer one, and both sit above what
    /// this counter charges for a round (<see cref="CounterService.RoundRate"/>) — a room for the watch is
    /// the third thing this desk sells and it should not undercut the second.</summary>
    [Fact]
    public void THE_PRICES_AreTwoNumbersAndTheLongOneCostsMore()
    {
        Assert.True(RoomBooking.BigRoomPrice > RoomBooking.Price);
        Assert.True(RoomBooking.Price > CounterService.RoundRate);
        Assert.Equal(RoomBooking.Price, RoomBooking.PriceOf(false));
        Assert.Equal(RoomBooking.BigRoomPrice, RoomBooking.PriceOf(true));
        Assert.Contains(
            RoomBooking.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
            RoomBooking.BookLabel(false), StringComparison.Ordinal);
        Assert.Contains(
            RoomBooking.BigRoomPrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
            RoomBooking.BookLabel(true), StringComparison.Ordinal);
    }

    /// <summary>
    /// #770 · THE PLATE SAYS IT IS BOOKED, WHICH WATCH FOR, AND WHICH OF THE TWO ROOMS IT IS.
    ///
    /// <para>And it reads back — the seat asks the PLAN whether this is the room the captain paid for
    /// (<see cref="RoomBooking.TryReadPlate"/>), so the stencil beside the door and the scene at the table
    /// are one fact. A plate that could not be read back would put the drawn room and the pressed room a
    /// parser apart, which is this project's third named bug class.</para>
    ///
    /// <para><b>The RED case.</b> Drop the watch out of <see cref="RoomBooking.BookedPlate"/> and both
    /// halves fire: the stencil stops naming the shift, and the round trip stops recovering it.</para>
    /// </summary>
    [Fact]
    public void THE_PLATE_SaysBookedAndTheWatchAndReadsBack()
    {
        foreach (long watch in EveryWatch())
        {
            foreach (bool big in new[] { true, false })
            {
                string plate = RoomBooking.BookedPlate(watch, big);

                Assert.Contains("BOOKED", plate, StringComparison.Ordinal);
                Assert.Contains("NEGOTIATION ROOM", plate, StringComparison.Ordinal);
                Assert.Contains(
                    watch.ToString(System.Globalization.CultureInfo.InvariantCulture), plate,
                    StringComparison.Ordinal);
                Assert.Equal(big, plate.Contains("LONG ROOM", StringComparison.Ordinal));

                Assert.True(RoomBooking.TryReadPlate(plate, out long read, out bool readBig),
                    $"the booked plate “{plate}” cannot be read back by the parser the SEAT uses.");
                Assert.Equal(watch, read);
                Assert.Equal(big, readBig);
            }
        }

        // …and it says no to everything that is not one of ours, so a plan full of other stencils cannot
        // seat a captain in a room nobody paid for.
        foreach (string other in UndergroundComplex.ParkViewPlates.Append(RoomBooking.EmptyRoomPlate)
            .Append("").Append("NEGOTIATION ROOM · BOOKED · WATCH tuesday"))
        {
            Assert.False(RoomBooking.TryReadPlate(other, out _, out _),
                $"“{other}” was read as a booking.");
        }
    }

    /// <summary>#770/#715 · The counter's book is FIVE INTEGERS and a tag, and it round-trips — the keep's
    /// own machine-readable idiom (<see cref="TheKeep.AskedEntry"/>), so nothing a person typed can end up
    /// inside a field the parser splits on, and the same ledger can hold both without either learning about
    /// the other.</summary>
    [Fact]
    public void THE_BOOK_KeepsAMachineEntryThatReadsBack_AndIgnoresEverybodyElses()
    {
        foreach (long watch in EveryWatch())
        {
            var made = new RoomBooking.Booking(-3, 7, watch % 2 == 0, watch, RoomBooking.TheCaptain);
            string entry = RoomBooking.BookedEntry(in made);

            Assert.True(RoomBooking.TryReadBooking(entry, RoomBooking.TheCaptain, out RoomBooking.Booking back));
            Assert.Equal(made, back);
            Assert.Equal(made.Paid, back.Paid);
            Assert.Equal(made.Plate, back.Plate);
        }

        Assert.False(RoomBooking.TryReadBooking(
            TheKeep.AskedEntry(TheKeep.TopicOf(0), 3), RoomBooking.TheCaptain, out _),
            "one of the keep's own asks was read as a room booking — two features are sharing one book and " +
            "one of them has started answering for the other.");
        Assert.False(RoomBooking.TryReadBooking(null, RoomBooking.TheCaptain, out _));
        Assert.False(RoomBooking.TryReadBooking("room-booked:1:2:3", RoomBooking.TheCaptain, out _));
    }

    // ── (b) THE DOOR HONOURS THE BOOKER, AND ONLY UNTIL THE WATCH TURNS ───────────────────────────────

    /// <summary>
    /// #770 · TWO CLAUSES, FAILING IN OPPOSITE DIRECTIONS.
    ///
    /// <para>A door that honoured anybody is a door that was never booked; a door that honoured the booker
    /// forever is a room the building has given away. Both are asked on every watch the rota has, and the
    /// release is asked against every OTHER watch rather than against one hand-picked later number.</para>
    ///
    /// <para><b>The RED case, both ways.</b> Take the name comparison out of
    /// <see cref="RoomBooking.HonoursTheDoor"/> and the stranger assertion fires on watch 0. Take the watch
    /// comparison out and the release assertion fires on the first turn of the shift.</para>
    /// </summary>
    [Fact]
    public void THE_DOOR_HonoursTheBookerAndNobodyElse_AndReleasesWhenTheWatchTurns()
    {
        foreach (long watch in EveryWatch())
        {
            var held = new RoomBooking.Booking(-3, 4, true, watch, RoomBooking.TheCaptain);

            Assert.True(RoomBooking.HonoursTheDoor(in held, RoomBooking.TheCaptain, watch),
                $"watch {watch}: the room the captain paid for will not let the captain in.");

            foreach (string stranger in new[] { "the keep", "a guard", "", "The Captain", "the captain " })
            {
                Assert.False(RoomBooking.HonoursTheDoor(in held, stranger, watch),
                    $"watch {watch}: the door honoured “{stranger}”. A door that honours anybody is a door " +
                    "that was never booked, and the whole of what the coin bought was that it does not.");
            }

            foreach (long later in EveryWatch().Concat([watch + 6, watch - 1]))
            {
                if (later == watch)
                {
                    continue;
                }
                Assert.False(RoomBooking.HonoursTheDoor(in held, RoomBooking.TheCaptain, later),
                    $"a room booked on watch {watch} was still the captain's on watch {later}. The building " +
                    "rents by the watch and takes it back without saying anything.");
            }
        }
    }

    /// <summary>#770 · …and the DECK's own question, which is the same law with a room number on it: the plate
    /// goes on exactly one room, on exactly one floor, on exactly one shift.</summary>
    [Fact]
    public void THE_PLATE_GoesOnOneRoomOnOneFloorOnOneWatch()
    {
        var held = new RoomBooking.Booking(-3, 4, false, 2, RoomBooking.TheCaptain);

        Assert.True(RoomBooking.HoldsThisRoom(in held, -3, 4, 2));
        Assert.False(RoomBooking.HoldsThisRoom(in held, -3, 5, 2));
        Assert.False(RoomBooking.HoldsThisRoom(in held, -4, 4, 2));
        Assert.False(RoomBooking.HoldsThisRoom(in held, -3, 4, 3));
        Assert.True(RoomBooking.HeldOn(in held, -3, 2));
        Assert.False(RoomBooking.HeldOn(in held, -3, 3));
    }

    /// <summary>#770/#758 · A booked room's leaf never collides with a hall cabinet's, which is the whole
    /// reason it has a base of its own — one key for two doors is one source consumed as if it were two, and
    /// dogging a suite on the garden would have shut a cabinet off the hall.</summary>
    [Fact]
    public void A_BOOKED_LEAF_IsNeverAHallCabinetsLeaf()
    {
        for (int room = 1; room <= 64; room++)
        {
            int leaf = RoomBooking.LeafOrdinal(room);
            Assert.True(RoomBooking.IsABookedLeaf(leaf));
            Assert.Equal(room, RoomBooking.RoomOfLeaf(leaf));
            Assert.NotEqual(CabinetPrivacy.Key(-3, leaf), CabinetPrivacy.Key(-3, room));
        }

        for (int cabinet = 0; cabinet <= 64; cabinet++)
        {
            Assert.False(RoomBooking.IsABookedLeaf(cabinet),
                $"hall cabinet {cabinet} reads as a booked room's leaf, so the counter would write the " +
                "wrong sentence into the book about the wrong door.");
        }
    }

    // ── (c) THE COUNTER'S NOTE IS WATCH-AWARE, AND BOTH SENTENCES ARE REACHABLE ───────────────────────

    /// <summary>
    /// #770/#781 · THE FACT IS FILED ON EVERY WATCH; ONLY THE SENTENCE FORKS.
    ///
    /// <para><see cref="CabinetPrivacy.WhoWasInsideNote"/>'s own law one fixture along. The keep is behind
    /// this counter on the living watches and nowhere near it on the dead ones, and the note has to be able
    /// to say so — while still being a note, on every watch, so a captain who booked at four in the morning
    /// has a record of it.</para>
    ///
    /// <para><b>Anti-vacuous by measurement.</b> Both halves are counted across the rota rather than
    /// asserted: a rota that drifted so that every watch is a living one would fail here rather than pass
    /// silently with one branch never taken.</para>
    ///
    /// <para><b>The RED case.</b> Drop the <see cref="TheKeep.KeptWatch"/> fork and name the keep on every
    /// watch: the dead-watch assertion fires, naming the shift.</para>
    /// </summary>
    [Fact]
    public void THE_COUNTERS_NOTE_NamesTheKeepOnlyOnTheWatchesHeIsBehindIt()
    {
        int living = 0, dead = 0;

        foreach (long watch in EveryWatch())
        {
            foreach (string note in new[]
            {
                RoomBooking.WhoBookedNote(4, watch), RoomBooking.DoggedTheDoorNote(4, watch),
            })
            {
                Assert.False(string.IsNullOrWhiteSpace(note),
                    $"watch {watch}: the counter wrote nothing down at all.");
                Assert.Contains("4", note, StringComparison.Ordinal);

                if (TheKeep.KeptWatch(watch))
                {
                    Assert.Contains(TheKeep.Name, note, StringComparison.Ordinal);
                }
                else
                {
                    Assert.DoesNotContain(TheKeep.Name, note, StringComparison.Ordinal);
                    Assert.Contains("nobody behind the counter", note, StringComparison.Ordinal);
                }
            }

            if (TheKeep.KeptWatch(watch)) { living++; } else { dead++; }
        }

        Assert.True(living > 0 && dead > 0,
            $"the rota has {living} living watch(es) and {dead} dead one(s) — one of the two sentences the " +
            "counter can write is unreachable, and this guard has stopped being able to tell them apart.");
    }

    // ── WHO IS SITTING OPPOSITE ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #770 · THE DELEGATION IS SOMEBODY WHO WAS ALREADY IN THE BUILDING — the same answer every time, and
    /// NOBODY when the hall is empty.
    ///
    /// <para>Both outcomes are the point. A room booked on a shift with nobody in the canteen is a room with
    /// nobody in it, and being told that is #757's own law: nothing happening is an OUTCOME here and never a
    /// control that went quiet.</para>
    ///
    /// <para><b>The RED case.</b> Seed the pick on anything that is not (site, floor, watch) — a counter, a
    /// clock — and the determinism assertion fires on the second read.</para>
    /// </summary>
    [Fact]
    public void THE_DELEGATION_IsOneOfTheRoomAndTheSameOneEveryTime_OrNobodyAtAll()
    {
        IReadOnlyList<CanteenRegulars.Seated> emptyHall = [];
        Assert.Null(RoomBooking.TheDelegation("luna", -3, 0, emptyHall));
        Assert.Null(RoomBooking.TheDelegation("luna", -3, 0, null));

        int somebody = 0, nobody = 0;
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);

            foreach (long watch in EveryWatch())
            {
                foreach (UndergroundComplex.Amenity a in floor.Amenities)
                {
                    IReadOnlyList<CanteenRegulars.Seated> rota =
                        CanteenRegulars.Sitting(body, level, a, watch);
                    if (rota.Count == 0)
                    {
                        continue;
                    }

                    CanteenRegulars.Seated? pick = RoomBooking.TheDelegation(body, level, watch, rota);
                    if (pick is { } who)
                    {
                        Assert.True(rota.Contains(who),
                            $"{body} B{-level} watch {watch}: the party across the table is not one of the " +
                            "people in this hall.");
                        somebody++;
                    }
                    else
                    {
                        nobody++;
                    }

                    // …and the same answer every time it is asked, because a room whose company changed
                    // between two reads would be the drawn room and the pressed room disagreeing.
                    Assert.Equal(pick, RoomBooking.TheDelegation(body, level, watch, rota));
                }
            }
        }

        Assert.True(somebody > 8 && nobody > 8,
            $"the sweep seated a delegation {somebody} time(s) and left the room empty {nobody} time(s) " +
            "with people in the hall. Both are the feature — a room you paid for with nobody in it is the " +
            "expensive half — and a threshold that reached only one of them is a guard that cannot tell " +
            "pass from fail.");
    }

    /// <summary>
    /// #770 · THE STRIP AT A BOOKED TABLE IS THE THREE SOCIAL MOVES PLUS ONE, AND THE EMPTY ROOM IS THE TWO
    /// EVERY LONE SEAT HAS.
    ///
    /// <para>The ids are <see cref="CanteenTable"/>'s and <see cref="SittingAlone"/>'s and deliberately not
    /// new ones: a saved game and a guard both key on an id, and a delegation is people at a table. The one
    /// move this room owns is the deal move, and it carries its own line so the client's move switch never
    /// grows a case for it (#749's <c>Says</c> path).</para>
    ///
    /// <para><b>The RED case.</b> Drop <see cref="RoomBooking.PutItToThem"/> out of the scene and the fourth
    /// assertion fires; give it no <c>Says</c> and the last one does.</para>
    /// </summary>
    [Fact]
    public void THE_BOOKED_SCENES_AreTheOrdinaryMovesPlusTheOneDealMove()
    {
        Encounter.Scene withThem = RoomBooking.TheDelegationTable("◈ A DRIVER, NOT SAYING WHO FOR");
        string[] ids = [.. withThem.Moves.Select(m => m.Id)];

        Assert.Equal(RoomBooking.DelegationSceneId, withThem.Id);
        Assert.Contains(CanteenTable.SmallTalk, ids);
        Assert.Contains(CanteenTable.Round, ids);
        Assert.Contains(CanteenTable.Leave, ids);
        Assert.Contains(RoomBooking.PutItToThem, ids);
        Assert.Contains("◈ A DRIVER, NOT SAYING WHO FOR", withThem.Counterpart, StringComparison.Ordinal);

        Encounter.Move round = withThem.Moves.Single(m => m.Id == CanteenTable.Round);
        Assert.Equal(Encounter.Requirement.Credits, round.Needs);
        Assert.Equal(CanteenTable.RoundPrice, round.Credits);

        Encounter.Move deal = withThem.Moves.Single(m => m.Id == RoomBooking.PutItToThem);
        Assert.True(deal.Says is { Length: > 0 },
            "the deal move carries no line, so the client's move switch has to grow a case for it — which " +
            "is the exact dodge #749's Says path was written to make impossible.");
        Assert.Equal(0, deal.Credits);

        Encounter.Scene alone = RoomBooking.TheEmptyRoom();
        Assert.Equal(RoomBooking.EmptySceneId, alone.Id);
        Assert.Equal([SittingAlone.Wait, SittingAlone.Stand], alone.Moves.Select(m => m.Id));

        // Every scene in this game can be left, and these two are scenes.
        Assert.True(Encounter.CanAlwaysLeave(withThem));
        Assert.True(Encounter.CanAlwaysLeave(alone));

        Assert.True(RoomBooking.IsABookedSitting(withThem.Id));
        Assert.True(RoomBooking.IsABookedSitting(alone.Id));
        Assert.False(RoomBooking.IsABookedSitting(RingOffice.TheChair("", true, true).Id));
        Assert.False(RoomBooking.IsABookedSitting(null));
    }

    // ── §13.8 ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #770 · NOTHING THIS ROOM SAYS EXPLAINS ANYTHING.
    ///
    /// <para>The canon sweep, walked over <see cref="RoomBooking.AllProse"/> so a line added tomorrow is
    /// checked tomorrow. A room rented by the watch is a fact about a BUILDING; the moment one of these
    /// sentences says what the building is for, the whole gradient stops being free storytelling and becomes
    /// exposition with a price on it.</para>
    /// </summary>
    [Fact]
    public void NOTHING_THE_BOOKING_SAYS_ExplainsAnything()
    {
        string[] forbidden =
        [
            "old one", "old ones", "reever", "restore", "backup", "kaamos",
            "experiment", "alien", "ancient", "specimen", "harvest",
        ];

        var prose = RoomBooking.AllProse().ToList();
        Assert.True(prose.Count > 20,
            $"the canon sweep walks {prose.Count} line(s) — AllProse has stopped keeping up with the file it " +
            "is meant to be the whole of.");

        foreach (string line in prose)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
