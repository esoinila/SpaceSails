using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// U10-F05 · <b>THE HOTEL LOBBY'S DOOR STENCILS HAVE A BACKING.</b> CABIN 1 to CABIN 5 and the office's plate are a
/// hatch's plate drawn straight onto the lobby's photograph, and on the ceiling lamps they washed out (QA 2026-10-29).
/// The room labels have worn a dark plate over art since #348; a hatch's plate over a backdrop takes it too.
///
/// <para>The page is docked at Ringside Exchange and ridden down to MEMBERS' ROOMS, its own walked frame is drawn
/// through a pen that notes every filled polygon and every word, and each CABIN plate's word must sit inside a dark
/// fill the width of its run.</para>
///
/// <para><b>Proven RED</b> by <c>BackThePlate</c> returning at its first line: no fill is drawn under any plate.</para>
/// </summary>
public sealed class TheDoorPlatesOverArtWearABackingTests
{
    private const string SpaceBar = "the-space-bar";   // the haven the finding was filed at

    private static Pages.Map OnTheHotelLevel(string name)
    {
        Pages.Map map = TheRideDownIsAWayBackTests.Ashore(name, SpaceBar);
        Assert.True((bool)Invoke(map, "RideTheHavenLiftTo", HavenLevels.ServiceLevel, 0)!);
        Set(map, "_pulse", PulseSlot.Empty);
        return map;
    }

    [Fact]
    public void EveryCabinPlateInTheLobbyStandsOnADarkFill()
    {
        Pages.Map map = OnTheHotelLevel("plates-backing");
        var pen = new ThePenThatKeepsFills();
        Set(map, "_deckView", new DeckView(pen));
        Set(map, "_viewportWidth", 1200);
        Set(map, "_viewportHeight", 700);
        Invoke(map, "DrawWalkFrame");

        var plan = (DeckPlan)Read(map, "_deckPlan")!;
        Assert.NotEmpty(plan.Backdrops);   // the lobby is a photograph, which is the whole premise
        int checkedPlates = 0;
        for (int n = 1; n <= HavenLevels.Cabins; n++)
        {
            string plate = HavenLevels.CabinDoorPlate(n);
            foreach ((float x, float y, string text) in pen.Words.Where(w => w.Text == plate))
            {
                checkedPlates++;
                Assert.True(
                    pen.Fills.Any(f => f.Alpha is > 100 and < 255 && x >= f.X0 && x <= f.X1 && y >= f.Y0 && y <= f.Y1),
                    $"{plate} is written at ({x:0}, {y:0}) over the lobby's photograph with no dark backing under it.");
            }
        }

        Assert.True(checkedPlates > 0, "no CABIN plate was drawn, so this proves nothing.");
    }

    private sealed class ThePenThatKeepsFills : IRenderer
    {
        public List<(float X, float Y, string Text)> Words { get; } = [];
        public List<(float X0, float Y0, float X1, float Y1, int Alpha)> Fills { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background)
        {
            Words.Clear();
            Fills.Clear();
        }

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f) { }

        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f)
        {
            if (fill is not { } c || pointsXY.Length < 8)
            {
                return;
            }

            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i + 1 < pointsXY.Length; i += 2)
            {
                x0 = Math.Min(x0, pointsXY[i]);
                x1 = Math.Max(x1, pointsXY[i]);
                y0 = Math.Min(y0, pointsXY[i + 1]);
                y1 = Math.Max(y1, pointsXY[i + 1]);
            }

            Fills.Add((x0, y0, x1, y1, c.A));
        }

        public void DrawText(float x, float y, string text, RgbaColor color,
            string font = "12px sans-serif", TextAlign align = TextAlign.Left) => Words.Add((x, y, text));

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
            float x, float y, float w, float h, float a = 1f) { }
    }
}
