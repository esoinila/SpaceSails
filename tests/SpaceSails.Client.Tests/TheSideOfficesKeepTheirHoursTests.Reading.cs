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
/// #1332 D and E · The second half of <see cref="TheSideOfficesKeepTheirHoursTests"/> (split to hold the file under the
/// house's 500 lines): the two offices' dev starts and the paper's title read only from inside its room. The helpers
/// (<c>OnTheHotelLevel</c>, the clock, the stand-at-a-point idioms) are the other half's.
/// </summary>
public sealed partial class TheSideOfficesKeepTheirHoursTests
{
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
