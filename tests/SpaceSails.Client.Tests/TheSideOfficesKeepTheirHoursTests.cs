using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1332 D and E · <b>THE FORWARDING DESK AND THE ADJUSTER'S ROOM KEEP THEIR HOURS — ON A LIVE PAGE.</b> Docked at
/// Cinder Roost or The Deep and ridden down to the hotel level, the page is walked through the office's watches: the
/// door shut every watch but its own and ajar on it (one in five, one in four — both edges of the band, over several
/// runs' seeds); nobody walks out of an empty room; the shut door's line on every press and the book's line once; the
/// ajar line told once per run on a free slot; the paper taken once (D's filed under Plant and Cinder Roost, E's
/// filed under nothing) and the desk bare after; the door never shut on a man inside; its title read only from inside;
/// and the dev start a tester boots.
/// </summary>
public sealed class TheSideOfficesKeepTheirHoursTests
{
    /// <summary>The two offices this slice added, by haven.</summary>
    public static TheoryData<string> TheTwoHavens => new() { ForwardingDesk.HavenId, AdjustersRoom.HavenId };

    private static double Hypot(double dx, double dy) => Math.Sqrt((dx * dx) + (dy * dy));

    private static SideOffice OfficeAt(string berth) => HavenInterior.TheOfficeAt(berth)!;

    private static string NameOf(string berth) => berth == ForwardingDesk.HavenId ? "Cinder Roost" : "The Deep";

    /// <summary>What the book must file this office's lines under, typed here from the design (Plant + Cinder Roost,
    /// Nebula Mutual + The Deep) and NOT read back off the office, so a guard on it cannot agree with a wrong office.</summary>
    private static string TheSubjects(string berth) => berth == ForwardingDesk.HavenId
        ? CaseSubjects.Line(CaseSubjects.Office("Plant"), CaseSubjects.Place("Cinder Roost"))
        : CaseSubjects.Line(CaseSubjects.Office("Nebula Mutual"), CaseSubjects.Place("The Deep"));

    /// <summary>A page docked at the haven, ashore, ridden down the first car to the hotel level, the first ride's own
    /// line told and the slot cleared.</summary>
    private static Pages.Map OnTheHotelLevel(string name, string berth)
    {
        Pages.Map map = TheRideDownIsAWayBackTests.Ashore(name, berth);
        Assert.True((bool)Invoke(map, "RideTheHavenLiftTo", HavenLevels.ServiceLevel, 0)!);
        Set(map, "_pulse", PulseSlot.Empty);
        Frames(map, 1);
        Assert.Equal(HavenLevels.FirstRideLine, InTheSlot(map));
        Set(map, "_pulse", PulseSlot.Empty);
        return map;
    }

    private static ulong Seed(Pages.Map map) => (ulong)Read(map, "WorldSeed")!;

    /// <summary>The first of this run's watches from 10 on that IS the office's.</summary>
    private static long TheOfficesWatch(Pages.Map map, SideOffice office)
    {
        for (long w = 10; ; w++)
        {
            if (office.IsAjarOn(Seed(map), w))
            {
                return w;
            }
        }
    }

    /// <summary>A sim time well inside <paramref name="watch"/>.</summary>
    private static double Within(long watch) => (watch * PatronRota.WatchSeconds) + 600;

    private static void At(Pages.Map map, double simTime) => Set(map, "SimTime", simTime);

    private static void Frames(Pages.Map map, int n, double dt = 1.0 / 30.0)
    {
        for (int i = 0; i < n; i++)
        {
            Invoke(map, "AdvanceBarWalkers", dt);
        }
    }

    private static DeckPlan Deck(Pages.Map map) => (DeckPlan)Read(map, "_deckPlan")!;

    private static IList<Pages.Map.Walker> Afoot(Pages.Map map) => (IList<Pages.Map.Walker>)Read(map, "_barAfoot")!;

    private static void StandAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    private static string? InTheSlot(Pages.Map map) => ((PulseSlot)Read(map, "_pulse")!).Message;

    private static IEnumerable<FieldNote> Notes(Pages.Map map) => (IEnumerable<FieldNote>)Read(map, "_fieldNotes")!;

    private static HashSet<string> Register(Pages.Map map) => (HashSet<string>)Read(map, "_roomsTurnedOver")!;

    private static ToldOnce Told(Pages.Map map) => (ToldOnce)Read(map, "_toldOnce")!;

    private static DeckReachability.Point Doorstep(string berth) => HavenInterior.TheOfficeDoorstepAt(berth)!.Value;

    /// <summary>A point inside the office, a pace and a half in front of the desk.</summary>
    private static DeckReachability.Point Inside(string berth)
    {
        DeckReachability.Point desk = HavenInterior.TheOfficeDeskAt(berth)!.Value;
        return new DeckReachability.Point(desk.X, desk.Y - 1.5);
    }

    /// <summary>
    /// <b>SHUT EVERY WATCH BUT ITS OWN, AJAR ON IT — AND THE PLAN SAYS SO, AT BOTH EDGES OF THE WATCH.</b> The same
    /// page is moved to the watch before the office's, the first and the last second of its own, and the one after:
    /// the drawn door follows the clock, the paper lies on the desk only while it stands open, and every other leaf
    /// stays locked throughout. Swept over six runs' seeds, because the watch is the run's and one run proves only
    /// that run.
    ///
    /// <para><b>Proven RED</b> by <c>ItIsTheOfficesWatch</c> answering false (never ajar), by the frame's
    /// <c>KeepTheOfficeHours</c> call removed (the door does not follow the watch turning under the captain), and by
    /// the page asking the Preservation office's clock whatever the haven.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void TheDoorIsShutOffItsWatchAndAjarOnItAtBothEdgesAndForEverySeed(string berth)
    {
        SideOffice office = OfficeAt(berth);
        Pages.Map map = OnTheHotelLevel($"side-hours-{berth}", berth);
        var offsets = new HashSet<int>();

        for (int run = 0; run < 6; run++)
        {
            Set(map, "_activeThreadId", $"side-thread-{run}");
            offsets.Add(office.OffsetFor(Seed(map)));
            long his = TheOfficesWatch(map, office);

            void Expect(double simTime, bool open, string why)
            {
                At(map, simTime);
                Frames(map, 2);
                Assert.True(
                    open == HavenInterior.TheOfficeStandsOpenIn(Deck(map)),
                    $"run {run}, {why}: the door is {(open ? "shut" : "open")}.");
                Assert.Equal(open, HavenInterior.TheSheetLiesIn(Deck(map)));
                Assert.Equal(open ? HavenLevels.Cabins - 1 : HavenLevels.Cabins, Deck(map).Doors.Count(d => d.Locked));
            }

            double start = his * PatronRota.WatchSeconds;
            Expect(start - 1, false, "the last second of the watch before");
            Expect(start, true, "the first second of its watch");
            Expect(start + PatronRota.WatchSeconds - 1, true, "the last second of its watch");
            Expect(start + PatronRota.WatchSeconds, false, "the first second of the watch after");
            Expect(start + ((office.WatchesPerTurn - 1) * PatronRota.WatchSeconds), false, "the last watch of its turn");
            Expect(start + (office.WatchesPerTurn * PatronRota.WatchSeconds), true, "its next turn");
        }

        Assert.True(offsets.Count > 1, "six runs, one offset: the clock ignores its seed.");
    }

    /// <summary>
    /// <b>NOBODY KEEPS THESE HOURS.</b> The door's watch is ajar and the room is empty: no walker plated
    /// <c>Clerk</c> (or anything) is dealt from the office's door on its watch, a second in or deep into it — the
    /// walker is slice C's alone.
    ///
    /// <para><b>Proven RED</b> by <c>HasAClerk</c> true on the office (the Preservation clerk steps out of an empty
    /// room).</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void TheRoomIsEmptyAndNobodyWalksOutOfIt(string berth)
    {
        SideOffice office = OfficeAt(berth);
        Assert.False(office.HasAClerk);
        Pages.Map map = OnTheHotelLevel($"side-empty-{berth}", berth);
        long his = TheOfficesWatch(map, office);
        DeckReachability.Point door = Doorstep(berth);

        foreach (double offset in new[] { 0.5, 5.0, 600.0, PatronRota.WatchSeconds - 5 })
        {
            At(map, (his * PatronRota.WatchSeconds) + offset);
            Frames(map, 5);
            Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)), "the door is shut on its own watch.");
            Assert.DoesNotContain(
                Afoot(map), w => string.Equals(w.Walk.Plate, PreservationOffice.ClerkPlate, StringComparison.Ordinal));
            Assert.DoesNotContain(
                Afoot(map), w => w.For == Pages.Map.Errand.Leaving && Hypot(w.Walk.X - door.X, w.Walk.Y - door.Y) < 1.0);
        }
    }

    /// <summary>
    /// <b>[E] AT THE SHUT DOOR: THE OFFICE'S LINE EVERY PRESS, AND THE BOOK'S LINE ONCE</b>, filed 📍 under the
    /// office's own two subjects — and the other two offices' plates, tags and lines answer nothing here.
    ///
    /// <para><b>Proven RED</b> by the plate-read tag's clause dropped (filed twice), by the office's branch removed
    /// from <c>KnockOnHatch</c> (the ring's knock answers instead), and by <c>TryTheOfficeDoor</c> reading the
    /// Preservation office's plate whatever the haven.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void TheShutDoorSaysTheOfficesLineEveryPressAndTheBookFilesThePlateOnce(string berth)
    {
        SideOffice office = OfficeAt(berth);
        Pages.Map map = OnTheHotelLevel($"side-knock-{berth}", berth);
        long his = TheOfficesWatch(map, office);
        At(map, Within(his + 1));
        Frames(map, 1);
        DeckReachability.Point door = Doorstep(berth);
        StandAt(map, door.X, door.Y);

        for (int press = 0; press < 3; press++)
        {
            Set(map, "_pulse", PulseSlot.Empty);
            Invoke(map, "InteractAtConsole");
            Assert.Equal(office.ShutLine, InTheSlot(map));

            // Something else in the book between the presses, so the book's own no-repeat-of-the-last-line rule
            // cannot be what keeps the plate's line to once: the register has to.
            Invoke(map, "FileNote", "an unrelated line between the presses", "·");
        }

        FieldNote once = Assert.Single(Notes(map), n => n.Text == office.PlateReadLine);
        Assert.Equal(PreservationOffice.PlateReadGlyph, once.Glyph);
        Assert.Equal(TheSubjects(berth), once.Subjects);
        Assert.Contains(office.PlateReadTag, Register(map));
        Assert.All(
            SideOffices.All.Where(o => o != office),
            o => Assert.DoesNotContain(o.PlateReadTag, Register(map)));
    }

    /// <summary>
    /// <b>INSIDE WHILE IT STANDS OPEN: TOLD ONCE PER RUN, ON A FREE SLOT, AND FILED NOWHERE.</b> The told-once
    /// memory's key is added on the frame the line reaches the slot and not before: a captain who walked in while a
    /// line was still being read is owed it.
    ///
    /// <para><b>Proven RED</b> by the told-once clause dropped (told again on the next visit), by the free-slot
    /// clause dropped (wrote over a line being read), and by the key added before the slot check (never told).</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void InsideWhileItStandsOpenTheAjarLineIsToldOncePerRun(string berth)
    {
        SideOffice office = OfficeAt(berth);
        Pages.Map map = OnTheHotelLevel($"side-ajar-{berth}", berth);
        long his = TheOfficesWatch(map, office);
        At(map, Within(his));
        Frames(map, 1);

        string key = ToldOnce.Key(ToldOnce.OfficeAjar, berth);
        Assert.False(Told(map).Has(key));
        DeckReachability.Point inside = Inside(berth);
        StandAt(map, inside.X, inside.Y);
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "ShowPulseMessage", "a line still being read", PulseRank.Status);
        Frames(map, 3);
        Assert.Equal("a line still being read", InTheSlot(map));
        Assert.False(Told(map).Has(key), "the line was marked told on a frame it never reached the slot.");

        Set(map, "_pulse", PulseSlot.Empty);
        Frames(map, 1);
        Assert.Equal(office.AjarLine, InTheSlot(map));
        Assert.True(Told(map).Has(key));
        Assert.DoesNotContain(Notes(map), n => n.Text == office.AjarLine);

        // Out, its next turn, and in again: never a second time this run.
        DeckReachability.Point door = Doorstep(berth);
        StandAt(map, door.X, door.Y);
        At(map, Within(his + office.WatchesPerTurn));
        Frames(map, 2);
        StandAt(map, inside.X, inside.Y);
        for (int i = 0; i < 10; i++)
        {
            Set(map, "_pulse", PulseSlot.Empty);
            Frames(map, 1);
            Assert.NotEqual(office.AjarLine, InTheSlot(map));
        }
    }

    /// <summary>
    /// <b>THE PAPER IS TAKEN ONCE PER RUN</b>: into the sleeve, the card reading it as papers read away from their
    /// room, the desk bare on this watch and on every watch after it. The note on D's desk is filed 📋 under Plant and
    /// Cinder Roost; the blank claim form on E's is a form and not evidence, and files nothing at all.
    ///
    /// <para><b>Proven RED</b> by <c>FilesTheSheet</c> true on the adjuster (the form is filed), by the book call
    /// removed from the pick-up (the note is never filed), and by <c>TheOfficeAsItStands</c> ignoring the taken tag
    /// (the paper back on the desk next turn).</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void ThePaperIsTakenOncePerRunAndTheBookFilesOnlyTheNote(string berth)
    {
        SideOffice office = OfficeAt(berth);
        Pages.Map map = OnTheHotelLevel($"side-paper-{berth}", berth);
        long his = TheOfficesWatch(map, office);
        At(map, Within(his));
        Frames(map, 1);

        DeckReachability.Point inside = Inside(berth);
        StandAt(map, inside.X, inside.Y);
        int notesBefore = Notes(map).Count();
        Invoke(map, "InteractAtConsole");

        var sleeve = (List<Satchel.Item>)Read(map, "_satchel")!;
        Assert.Single(sleeve, i => i == office.TheSheet);
        var card = (DeckPlan.ConsoleSpot)Read(map, "_viewObject")!;
        Assert.Equal(office.SheetTitle, card.Label);
        Assert.Equal(office.SheetDocument, card.Caption);
        Assert.Contains(office.SheetTakenTag, Register(map));

        // Typed from the design, not read off the office's own flag: the note is filed, and the form is not.
        if (berth == ForwardingDesk.HavenId)
        {
            FieldNote filed = Assert.Single(Notes(map), n => n.Text == office.SheetDocument);
            Assert.Equal(PreservationOffice.SheetGlyph, filed.Glyph);
            Assert.Equal(TheSubjects(berth), filed.Subjects);
        }
        else
        {
            Assert.DoesNotContain(Notes(map), n => n.Text == office.SheetDocument);
            Assert.Equal(notesBefore, Notes(map).Count());
        }

        Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
        Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)), "the paper is still on the desk after it was taken.");

        // Its next turn: the door keeps its hours and the desk stays bare; a second press takes nothing more.
        StandAt(map, Doorstep(berth).X, 30);
        At(map, Within(his + office.WatchesPerTurn));
        Frames(map, 2);
        Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
        Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)));
        StandAt(map, inside.X, inside.Y);
        Invoke(map, "InteractAtConsole");
        Assert.Single((List<Satchel.Item>)Read(map, "_satchel")!, i => i == office.TheSheet);
    }

    /// <summary>
    /// <b>A FULL SLEEVE LEAVES THE PAPER WHERE IT LIES.</b> The pocket-full line, nothing taken, the desk still laid,
    /// the register unmarked, and nothing filed.
    ///
    /// <para><b>Proven RED</b> by the <c>Satchel.CanTake</c> check taken out of the pick-up.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void AFullSleeveLeavesThePaperOnTheDesk(string berth)
    {
        SideOffice office = OfficeAt(berth);
        Pages.Map map = OnTheHotelLevel($"side-full-{berth}", berth);
        long his = TheOfficesWatch(map, office);
        At(map, Within(his));
        Frames(map, 1);

        var sleeve = (List<Satchel.Item>)Read(map, "_satchel")!;
        int guard = 0;
        while (Satchel.CanTake(sleeve, office.TheSheet) && guard++ < 200)
        {
            sleeve.Add(new Satchel.Item(Satchel.Kind.Paper, $"filler-{guard}"));
        }

        Assert.False(Satchel.CanTake(sleeve, office.TheSheet), "the sleeve would not fill.");
        DeckReachability.Point inside = Inside(berth);
        StandAt(map, inside.X, inside.Y);
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "InteractAtConsole");

        Assert.Equal(UndergroundComplex.PocketFullLine.Trim(), InTheSlot(map));
        Assert.DoesNotContain(office.TheSheet, (List<Satchel.Item>)Read(map, "_satchel")!);
        Assert.DoesNotContain(office.SheetTakenTag, Register(map));
        Assert.True(HavenInterior.TheSheetLiesIn(Deck(map)));
        Assert.DoesNotContain(Notes(map), n => n.Text == office.SheetDocument);
    }

    /// <summary>
    /// <b>THE DOOR IS NEVER SHUT ON A MAN INSIDE.</b> The watch turns with the captain in the office: the doorway
    /// stays until he steps out, and the frame he is in the corridor it is shut.
    ///
    /// <para><b>Proven RED</b> by dropping the inside clause from <c>TheOfficeAsItStands</c>.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void TheDoorIsNeverShutOnAManInside(string berth)
    {
        SideOffice office = OfficeAt(berth);
        Pages.Map map = OnTheHotelLevel($"side-inside-{berth}", berth);
        long his = TheOfficesWatch(map, office);
        At(map, Within(his));
        Frames(map, 1);

        DeckReachability.Point inside = Inside(berth);
        StandAt(map, inside.X, inside.Y);
        At(map, Within(his + 1));
        Frames(map, 3);
        Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)), "the door was shut on the captain inside.");

        DeckReachability.Point door = Doorstep(berth);
        StandAt(map, door.X, door.Y);
        Frames(map, 1);
        Assert.False(HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
    }

    /// <summary>
    /// <b>NOTHING IS DIFFERENT ANYWHERE ELSE.</b> On the office's own watch, another haven's hotel level (one with no
    /// office, and the other new office's) keeps five locked leaves, no paper and no ajar line: one office to a
    /// haven, and a door's clock is that door's.
    ///
    /// <para><b>Proven RED</b> by <c>HasTheOffice</c> answering true for The Space Bar.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void AnotherHavensHotelLevelIsUntouchedOnTheOfficesWatch(string berth)
    {
        SideOffice office = OfficeAt(berth);
        Pages.Map here = OnTheHotelLevel($"side-here-{berth}", berth);
        long his = TheOfficesWatch(here, office);

        string sibling = berth == ForwardingDesk.HavenId ? AdjustersRoom.HavenId : ForwardingDesk.HavenId;
        foreach (string other in new[] { "the-space-bar", "the-tilt", sibling })
        {
            Pages.Map map = TheRideDownIsAWayBackTests.Ashore($"side-elsewhere-{berth}-{other}", other);
            Assert.True((bool)Invoke(map, "RideTheHavenLiftTo", HavenLevels.ServiceLevel, 0)!);
            Set(map, "_activeThreadId", Read(here, "_activeThreadId"));
            SideOffice? theirs = HavenInterior.TheOfficeAt(other);

            // A watch that is this office's and, where the haven has an office of its own, NOT theirs.
            long w = his;
            for (int guard = 0; theirs is not null && theirs.IsAjarOn(Seed(map), w) && guard < 64; guard++)
            {
                w += office.WatchesPerTurn;
            }

            Assert.True(
                theirs is null || !theirs.IsAjarOn(Seed(map), w),
                $"{other} has {berth}'s office (or one on its clock): no watch of {berth}'s is not also its own.");

            At(map, Within(w));
            Frames(map, 3);
            Assert.True(office.IsAjarOn(Seed(map), w));
            Assert.False(HavenInterior.TheOfficeStandsOpenIn(Deck(map)), $"{other}'s door stands open on {berth}'s watch.");
            Assert.Equal(HavenLevels.Cabins, Deck(map).Doors.Count(d => d.Locked));
            Assert.DoesNotContain(
                Deck(map).Consoles, c => string.Equals(c.Label, office.DoorPlate, StringComparison.Ordinal));
            Assert.False(Told(map).Has(ToldOnce.Key(ToldOnce.OfficeAjar, other)));
        }
    }

    /// <summary>
    /// <b>A TESTER STANDS AT THE DOOR IN ONE URL, EITHER WAY.</b> Both of each office's rows are in the front door's
    /// list; <c>office=shut</c> stands the captain under the plate with the door shut whatever the watch, and
    /// <c>office=open</c> stands him at the doorstep with the door ajar and the paper on the desk.
    ///
    /// <para><b>Proven RED</b> by the rows left out of <c>DevStarts.All</c> and by <c>ItIsTheOfficesWatch</c>
    /// ignoring the latch.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void EachDoorIsOneDevStartAwayShutOrOpen(string berth)
    {
        SideOffice office = OfficeAt(berth);
        string[] rows = [.. DevStarts.All.Select(e => e.Url)
            .Where(u => u.Contains("&office=", StringComparison.Ordinal) && u.Contains("dock=" + berth + "&", StringComparison.Ordinal))];
        Assert.Equal(
            [$"/map?dock={berth}&ashore=1&havenfloor=-1&office=shut", $"/map?dock={berth}&ashore=1&havenfloor=-1&office=open"],
            rows);

        foreach ((string url, bool open) in new[] { (rows[0], false), (rows[1], true) })
        {
            Pages.Map map = OnTheHotelLevel($"side-dev-{berth}-{open}", berth);
            Set(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench(url));
            long his = TheOfficesWatch(map, office);
            At(map, Within(open ? his + 1 : his));   // the watch the latch overrules
            Invoke(map, "StandAtTheOfficeIfAsked");
            Frames(map, 1);

            Assert.Equal(open, HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
            Assert.Equal(open, HavenInterior.TheSheetLiesIn(Deck(map)));
            double x = (double)Read(map, "_avatarX")!, y = (double)Read(map, "_avatarY")!;
            Assert.False(SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, Deck(map).CollisionField));
            DeckReachability.Point door = Doorstep(berth);
            Assert.True(Hypot(x - door.X, y - door.Y) <= DeckPlan.InteractRadius, "the tester is not at the door.");
        }
    }

    /// <summary>
    /// <b>THE PAPER HAS A NAME ONLY IN ITS ROOM (#1353's rule, at both new doors).</b> On the office's watch, with the
    /// door ajar and the paper on the desk, the page's own walked frame is drawn twice: from the corridor at the
    /// office's doorstep, where the paper is a dot on a desk with no title; and from inside, a pace and a half from
    /// the desk, where the title is on the glass.
    ///
    /// <para><b>Proven RED</b> by the paper check taken out of the plate (<c>APaperKeptToItsRoom</c> answering
    /// false): the title is legible from the corridor again.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoHavens))]
    public void ThePapersTitleIsReadFromInsideAndNotFromTheCorridor(string berth)
    {
        SideOffice office = OfficeAt(berth);
        Pages.Map map = OnTheHotelLevel($"side-title-{berth}", berth);
        long his = TheOfficesWatch(map, office);
        At(map, Within(his));
        Frames(map, 1);
        Assert.True(HavenInterior.TheSheetLiesIn(Deck(map)), "the desk is bare, so this proves nothing.");

        var pen = new TheWordsOnTheGlass();
        Set(map, "_deckView", new DeckView(pen));
        Set(map, "_viewportWidth", 1200);
        Set(map, "_viewportHeight", 700);

        DeckReachability.Point door = Doorstep(berth);
        StandAt(map, door.X, door.Y);
        Invoke(map, "DrawWalkFrame");
        Assert.Contains(HavenLevels.CabinDoorPlate(1), pen.Said);   // the frame was drawn, and it is the hotel level
        Assert.DoesNotContain(office.SheetTitle, pen.Said);

        DeckReachability.Point inside = Inside(berth);
        StandAt(map, inside.X, inside.Y);
        Assert.True(HavenInterior.InTheOffice(berth, inside.X, inside.Y, HavenLevels.ServiceLevel));
        Invoke(map, "DrawWalkFrame");
        Assert.Contains(office.SheetTitle, pen.Said);
    }

    /// <summary>A pen that keeps the words of the last frame and nothing else.</summary>
    private sealed class TheWordsOnTheGlass : IRenderer
    {
        public List<string> Said { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Said.Clear();

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f) { }

        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawText(float x, float y, string text, RgbaColor color,
            string font = "12px sans-serif", TextAlign align = TextAlign.Left) => Said.Add(text);

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
            float x, float y, float w, float h, float a = 1f) { }
    }
}
