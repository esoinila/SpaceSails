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
/// #1332 C · <b>THE PRESERVATION OFFICE KEEPS ITS HOURS — ON A LIVE PAGE.</b> Docked at Ringside Exchange and ridden
/// down to MEMBERS' ROOMS, the page is walked through the office's watches: the door shut three watches in four and
/// ajar on the clerk's; the clerk on his feet only on his watch and only on that floor; the shut door's line on
/// every press and the book's line once; the warm chair told once per run; the sheet taken once, filed under the
/// Authority and Ringside Exchange, and the desk bare after; the door never shut on a man inside; and the dev start
/// a tester boots.
/// </summary>
public sealed class ThePreservationOfficeKeepsItsHoursTests
{
    private const string Ringside = PreservationOffice.HavenId;

    private static double Hypot(double dx, double dy) => Math.Sqrt((dx * dx) + (dy * dy));

    /// <summary>A page docked at Ringside Exchange, ashore, ridden down the first car to the hotel level.</summary>
    private static Pages.Map OnTheHotelLevel(string name)
    {
        Pages.Map map = TheRideDownIsAWayBackTests.Ashore(name, Ringside);
        Assert.True((bool)Invoke(map, "RideTheHavenLiftTo", HavenLevels.ServiceLevel, 0)!);

        // The first ride down's own line is owed here and takes the first free slot; let it be told, so the
        // office's lines below are asked of a slot nothing else is waiting for.
        Set(map, "_pulse", PulseSlot.Empty);
        Frames(map, 1);
        Assert.Equal(HavenLevels.FirstRideLine, InTheSlot(map));
        Set(map, "_pulse", PulseSlot.Empty);
        return map;
    }

    private static ulong Seed(Pages.Map map) => (ulong)Read(map, "WorldSeed")!;

    /// <summary>The first of this run's watches from 10 on that IS the clerk's.</summary>
    private static long TheClerksWatch(Pages.Map map)
    {
        for (long w = 10; ; w++)
        {
            if (PreservationOffice.IsTheClerksWatch(Seed(map), w))
            {
                return w;
            }
        }
    }

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

    private static Pages.Map.Walker? TheClerk(Pages.Map map) =>
        Afoot(map).FirstOrDefault(w => w.Walk.Plate == PreservationOffice.ClerkPlate);

    private static void StandAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    private static string? InTheSlot(Pages.Map map) => ((PulseSlot)Read(map, "_pulse")!).Message;

    private static IEnumerable<FieldNote> Notes(Pages.Map map) => (IEnumerable<FieldNote>)Read(map, "_fieldNotes")!;

    private static HashSet<string> Register(Pages.Map map) => (HashSet<string>)Read(map, "_roomsTurnedOver")!;

    /// <summary>A point inside the office, a pace and a half in front of the desk.</summary>
    private static DeckReachability.Point InsideTheOffice()
    {
        DeckReachability.Point desk = HavenInterior.TheOfficeDeskAt(Ringside)!.Value;
        return new DeckReachability.Point(desk.X, desk.Y - 1.5);
    }

    /// <summary>
    /// <b>SHUT THREE WATCHES IN FOUR, AJAR ON THE CLERK'S — AND THE PLAN SAYS SO.</b> The same page on the hotel level
    /// is moved from a watch that is not his, to his, to the one after: the drawn door follows the clock, the sheet
    /// lies on the desk only while it stands open, and every other leaf stays locked throughout.
    ///
    /// <para><b>Proven RED</b> by <c>ItIsTheClerksWatch</c> answering false (never ajar) and by the frame's
    /// <c>KeepTheOfficeHours</c> call removed (the door does not follow the watch turning under the captain).</para>
    /// </summary>
    [Fact]
    public void TheDoorIsShutOffTheClerksWatchAndAjarOnIt()
    {
        Pages.Map map = OnTheHotelLevel("office-hours");
        long his = TheClerksWatch(map);

        At(map, PreservationOffice.SetsOffAt(his - 1) + 600);
        Frames(map, 2);
        Assert.False(HavenInterior.TheOfficeStandsOpenIn(Deck(map)), "the door stands open off the clerk's watch.");
        Assert.Equal(HavenLevels.Cabins, Deck(map).Doors.Count(d => d.Locked));
        Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)));

        At(map, PreservationOffice.SetsOffAt(his) + 600);
        Frames(map, 2);
        Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)), "the door is shut on the clerk's watch.");
        Assert.Equal(HavenLevels.Cabins - 1, Deck(map).Doors.Count(d => d.Locked));
        Assert.True(HavenInterior.TheSheetLiesIn(Deck(map)), "the desk is bare before anybody took the sheet.");

        At(map, PreservationOffice.SetsOffAt(his + 1) + 600);
        Frames(map, 2);
        Assert.False(HavenInterior.TheOfficeStandsOpenIn(Deck(map)), "the door stands open the watch after his.");
    }

    /// <summary>
    /// <b>THE CLERK IS ON HIS FEET ONLY ON HIS WATCH, ONLY ON THAT FLOOR, AND ONLY UNTIL HE REACHES HIS CAR.</b> A
    /// second into his watch he is on the floor at his door, plated <c>Clerk</c>; walked on, he reaches the car the
    /// room names for him and is gone, and he is not dealt again; off his watch, on the concourse above, and on
    /// another haven's hotel level nobody is.
    ///
    /// <para><b>Proven RED</b> by removing the watch clause from <c>TheClerksLeg</c> (a clerk every watch) and by
    /// dropping the <c>Along</c> window (dealt again at his door once he had gone).</para>
    /// </summary>
    [Fact]
    public void TheClerkWalksFromHisDoorToHisCarOnHisWatchAndOnlyThere()
    {
        Pages.Map map = OnTheHotelLevel("office-clerk");
        long his = TheClerksWatch(map);
        DeckReachability.Point door = HavenInterior.TheOfficeDoorstepAt(Ringside)!.Value;
        DeckReachability.Point car =
            HavenInterior.TheCageLandingAt(Ringside, HavenInterior.TheClerksCarAt(Ringside)!.Value)!.Value;

        // Off his watch: nobody.
        At(map, PreservationOffice.SetsOffAt(his - 1) + 1);
        Frames(map, 5);
        Assert.Null(TheClerk(map));

        // A second into his watch: at his door, plated as the canon plates him, and a leaving walker.
        At(map, PreservationOffice.SetsOffAt(his) + 0.5);
        Frames(map, 1);
        Pages.Map.Walker clerk = TheClerk(map) ?? throw new Xunit.Sdk.XunitException("no clerk on his watch.");
        Assert.Equal(Pages.Map.Errand.Leaving, clerk.For);
        Assert.Equal("", clerk.Who);
        Assert.True(Hypot(clerk.Walk.X - door.X, clerk.Walk.Y - door.Y) < 3.0, "he did not come out of his door.");

        // Walked on (the sim clock with him): he reaches his car and is gone, and is not dealt again.
        double last = double.MaxValue;
        for (int i = 0; i < 30 * 60 && TheClerk(map) is { } w; i++)
        {
            last = Hypot(w.Walk.X - car.X, w.Walk.Y - car.Y);
            At(map, (double)Read(map, "SimTime")! + (1.0 / 30.0));
            Frames(map, 1);
        }

        Assert.Null(TheClerk(map));
        Assert.True(last < 3.5, $"he vanished {last:F1} du from his car.");
        for (int i = 0; i < 90; i++)
        {
            At(map, (double)Read(map, "SimTime")! + (1.0 / 30.0));
            Frames(map, 1);
            Assert.Null(TheClerk(map));
        }

        // On the concourse on his watch: nobody down here is drawn up there.
        At(map, PreservationOffice.SetsOffAt(his) + 0.5);
        Assert.True((bool)Invoke(map, "RideTheHavenLiftTo", HavenLevels.Concourse, 0)!);
        Frames(map, 3);
        Assert.Null(TheClerk(map));

        // Another haven's hotel level at the same instant: no office, no clerk.
        Pages.Map elsewhere = TheRideDownIsAWayBackTests.Ashore("office-clerk-elsewhere", "the-space-bar");
        Assert.True((bool)Invoke(elsewhere, "RideTheHavenLiftTo", HavenLevels.ServiceLevel, 0)!);
        At(elsewhere, PreservationOffice.SetsOffAt(his) + 0.5);
        Frames(elsewhere, 3);
        Assert.Null(TheClerk(elsewhere));
    }

    /// <summary>
    /// <b>[E] AT THE SHUT DOOR: THE OFFICE'S LINE EVERY PRESS, AND THE BOOK'S LINE ONCE</b>, filed 📍 under the
    /// Authority and Ringside Exchange.
    ///
    /// <para><b>Proven RED</b> by the plate-read tag's clause dropped (filed twice) and by the office's branch removed
    /// from <c>KnockOnHatch</c> (the ring's knock answers instead).</para>
    /// </summary>
    [Fact]
    public void TheShutDoorSaysTheOfficesLineEveryPressAndTheBookFilesThePlateOnce()
    {
        Pages.Map map = OnTheHotelLevel("office-knock");
        long his = TheClerksWatch(map);
        At(map, PreservationOffice.SetsOffAt(his + 1) + 60);
        Frames(map, 1);
        DeckReachability.Point door = HavenInterior.TheOfficeDoorstepAt(Ringside)!.Value;
        StandAt(map, door.X, door.Y);

        for (int press = 0; press < 2; press++)
        {
            Set(map, "_pulse", PulseSlot.Empty);
            Invoke(map, "InteractAtConsole");
            Assert.Equal(PreservationOffice.ShutLine, InTheSlot(map));

            // Something else in the book between the presses, so the book's own no-repeat-of-the-last-line rule
            // cannot be what keeps the plate's line to once: the register has to.
            Invoke(map, "FileNote", "an unrelated line between the two presses", "·");
        }

        FieldNote[] filed = [.. Notes(map).Where(n => n.Text == PreservationOffice.PlateReadLine)];
        FieldNote once = Assert.Single(filed);
        Assert.Equal(PreservationOffice.PlateReadGlyph, once.Glyph);
        Assert.Equal(PreservationOffice.SubjectsFor("Ringside Exchange"), once.Subjects);
        Assert.Contains(PreservationOffice.PlateReadTag, Register(map));
    }

    /// <summary>
    /// <b>INSIDE WHILE IT STANDS OPEN: TOLD ONCE PER RUN, ON A FREE SLOT, AND FILED NOWHERE.</b>
    ///
    /// <para><b>Proven RED</b> by the register clause dropped (told again on the next visit) and by the free-slot
    /// clause dropped (wrote over a line being read).</para>
    /// </summary>
    [Fact]
    public void InsideWhileItStandsOpenTheWarmChairIsToldOncePerRun()
    {
        Pages.Map map = OnTheHotelLevel("office-warm");
        long his = TheClerksWatch(map);
        At(map, PreservationOffice.SetsOffAt(his) + 600);
        Frames(map, 1);

        DeckReachability.Point inside = InsideTheOffice();
        StandAt(map, inside.X, inside.Y);
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "ShowPulseMessage", "a line still being read", PulseRank.Status);
        Frames(map, 3);
        Assert.Equal("a line still being read", InTheSlot(map));

        Set(map, "_pulse", PulseSlot.Empty);
        Frames(map, 1);
        Assert.Equal(PreservationOffice.WarmStillLine, InTheSlot(map));
        Assert.DoesNotContain(Notes(map), n => n.Text == PreservationOffice.WarmStillLine);

        // Out, a later clerk's watch, and in again: never a second time this run.
        DeckReachability.Point door = HavenInterior.TheOfficeDoorstepAt(Ringside)!.Value;
        StandAt(map, door.X, door.Y);
        At(map, PreservationOffice.SetsOffAt(his + PreservationOffice.WatchesPerTurn) + 600);
        Frames(map, 2);
        StandAt(map, inside.X, inside.Y);
        for (int i = 0; i < 10; i++)
        {
            Set(map, "_pulse", PulseSlot.Empty);
            Frames(map, 1);
            Assert.NotEqual(PreservationOffice.WarmStillLine, InTheSlot(map));
        }
    }

    /// <summary>
    /// <b>THE SHEET IS TAKEN ONCE PER RUN</b>: into the sleeve, the card reading it as papers read away from their
    /// room, the book filing the document 📋 under the Authority and Ringside Exchange, and the desk bare — on this
    /// watch and on every clerk's watch after it. The office keeps its hours.
    ///
    /// <para><b>Proven RED</b> by <c>TheOfficeAsItStands</c> ignoring the taken tag (the sheet back on the desk next
    /// watch) and by the book call removed from the pick-up.</para>
    /// </summary>
    [Fact]
    public void TheSheetIsTakenOncePerRunAndFiledUnderTheAuthorityAndRingside()
    {
        Pages.Map map = OnTheHotelLevel("office-sheet");
        long his = TheClerksWatch(map);
        At(map, PreservationOffice.SetsOffAt(his) + 600);
        Frames(map, 1);

        DeckReachability.Point inside = InsideTheOffice();
        StandAt(map, inside.X, inside.Y);
        Invoke(map, "InteractAtConsole");

        var sleeve = (List<Satchel.Item>)Read(map, "_satchel")!;
        Assert.Single(sleeve, i => i == PreservationOffice.TheSheet);
        var card = (DeckPlan.ConsoleSpot)Read(map, "_viewObject")!;
        Assert.Equal(PreservationOffice.SheetTitle, card.Label);
        Assert.Equal(PreservationOffice.SheetDocument, card.Caption);

        FieldNote filed = Assert.Single(Notes(map), n => n.Text == PreservationOffice.SheetDocument);
        Assert.Equal(PreservationOffice.SheetGlyph, filed.Glyph);
        Assert.Equal(PreservationOffice.SubjectsFor("Ringside Exchange"), filed.Subjects);

        Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
        Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)), "the sheet is still on the desk after it was taken.");

        // The next clerk's watch: the door keeps its hours and the desk stays bare.
        StandAt(map, HavenInterior.TheOfficeDoorstepAt(Ringside)!.Value.X, 30);
        At(map, PreservationOffice.SetsOffAt(his + PreservationOffice.WatchesPerTurn) + 600);
        Frames(map, 2);
        Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
        Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)));
        Assert.Single((List<Satchel.Item>)Read(map, "_satchel")!, i => i == PreservationOffice.TheSheet);
    }

    /// <summary>
    /// <b>THE DOOR IS NEVER SHUT ON A MAN INSIDE.</b> The watch turns with the captain in the office: the doorway
    /// stays until he steps out, and the frame he is in the corridor it is shut.
    ///
    /// <para><b>Proven RED</b> by dropping the inside clause from <c>TheOfficeAsItStands</c> (the leaf locked with
    /// him behind it).</para>
    /// </summary>
    [Fact]
    public void TheDoorIsNeverShutOnAManInside()
    {
        Pages.Map map = OnTheHotelLevel("office-inside");
        long his = TheClerksWatch(map);
        At(map, PreservationOffice.SetsOffAt(his) + 600);
        Frames(map, 1);

        DeckReachability.Point inside = InsideTheOffice();
        StandAt(map, inside.X, inside.Y);
        At(map, PreservationOffice.SetsOffAt(his + 1) + 60);
        Frames(map, 3);
        Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)), "the door was shut on the captain inside.");

        DeckReachability.Point door = HavenInterior.TheOfficeDoorstepAt(Ringside)!.Value;
        StandAt(map, door.X, door.Y);
        Frames(map, 1);
        Assert.False(HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
    }

    /// <summary>
    /// <b>A TESTER STANDS AT THE DOOR IN ONE URL, EITHER WAY.</b> Both rows are in the front door's list;
    /// <c>office=shut</c> stands the captain under the plate with the door shut whatever the watch, and
    /// <c>office=open</c> stands him in the corridor with the door ajar and the sheet on the desk.
    ///
    /// <para><b>Proven RED</b> by the rows left out of <c>DevStarts.All</c> and by <c>ItIsTheClerksWatch</c>
    /// ignoring the latch.</para>
    /// </summary>
    [Fact]
    public void TheOfficeIsOneDevStartAwayShutOrOpen()
    {
        string[] rows = [.. DevStarts.All.Select(e => e.Url)
            .Where(u => u.Contains("&office=", StringComparison.Ordinal) && u.Contains("dock=" + Ringside, StringComparison.Ordinal))];
        Assert.Equal(
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut",
             "/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=open"],
            rows);

        foreach ((string url, bool open) in new[] { (rows[0], false), (rows[1], true) })
        {
            Pages.Map map = OnTheHotelLevel($"office-dev-{open}");
            Set(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench(url));
            long his = TheClerksWatch(map);
            At(map, PreservationOffice.SetsOffAt(open ? his + 1 : his) + 600);   // the watch the latch overrules
            Invoke(map, "StandAtTheOfficeIfAsked");
            Frames(map, 1);

            Assert.Equal(open, HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
            Assert.Equal(open, HavenInterior.TheSheetLiesIn(Deck(map)));
            double x = (double)Read(map, "_avatarX")!, y = (double)Read(map, "_avatarY")!;
            Assert.False(SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, Deck(map).CollisionField));
            DeckReachability.Point door = HavenInterior.TheOfficeDoorstepAt(Ringside)!.Value;
            Assert.Equal(open, Hypot(x - door.X, y - door.Y) > DeckPlan.InteractRadius);
        }
    }
    /// <summary>
    /// #1353 · <b>THE SHEET HAS A NAME ONLY IN ITS ROOM.</b> On the clerk's watch, with the door ajar and the sheet on
    /// the desk, the page's own walked frame is drawn twice: from the corridor at the office's doorstep, where the
    /// desk is in plain view through the doorway and its paper is a dot with no title; and from inside, a pace and a
    /// half from the desk, where the title is on the glass. Kosh: the paper is a paper.
    ///
    /// <para><b>Proven RED</b> by the paper check taken out of the plate (<c>APaperKeptToItsRoom</c> answering false):
    /// the title is legible from the corridor again.</para>
    /// </summary>
    [Fact]
    public void TheSheetsTitleIsReadFromInsideTheOfficeAndNotFromTheCorridor()
    {
        Pages.Map map = OnTheHotelLevel("office-paper");
        long his = TheClerksWatch(map);
        At(map, PreservationOffice.SetsOffAt(his) + 600);
        Frames(map, 1);
        Assert.True(HavenInterior.TheSheetLiesIn(Deck(map)), "the desk is bare, so this proves nothing.");

        var pen = new TheWordsOnTheGlass();
        Set(map, "_deckView", new DeckView(pen));
        Set(map, "_viewportWidth", 1200);
        Set(map, "_viewportHeight", 700);

        DeckReachability.Point door = HavenInterior.TheOfficeDoorstepAt(Ringside)!.Value;
        StandAt(map, door.X, door.Y);
        Invoke(map, "DrawWalkFrame");
        Assert.Contains(HavenLevels.CabinDoorPlate(1), pen.Said);   // the frame was drawn, and it is the hotel level
        Assert.DoesNotContain(PreservationOffice.SheetTitle, pen.Said);

        DeckReachability.Point inside = InsideTheOffice();
        StandAt(map, inside.X, inside.Y);
        Assert.True(HavenInterior.InTheOffice(Ringside, inside.X, inside.Y, HavenLevels.ServiceLevel));
        Invoke(map, "DrawWalkFrame");
        Assert.Contains(PreservationOffice.SheetTitle, pen.Said);
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
