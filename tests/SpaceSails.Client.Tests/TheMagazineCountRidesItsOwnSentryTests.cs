using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1039 · THE MAGAZINE COUNT FOLLOWS THE WALKER.
///
/// <para>Owner, mid-playtest on the Tilt's ground (screenshot on #1015): <i>"The magazine count follows the
/// walker here…"</i> — a seven-segment <c>99</c> welded just under the mothership's orbit line, with no sentry
/// mark anywhere near it, riding along the top of the frame for the rest of the excursion.</para>
///
/// <para><b>Whose count it actually was.</b> GATE-1, the shuttle's own door sentry — set down inside the tube
/// at <c>MoonSurface.SurfaceTopY + 2</c> when the boat mates, never bought and never dry, so its drum reads a
/// permanent 99. The captain then walks DOWN the field and the frame is <c>FollowCam</c>-centred on him, so
/// GATE-1's mark slides off the top of the glass. #986 F2's band-avoidance in <c>DeckView.DrawTheSentries</c>
/// then re-seated the plate with <c>Math.Max(CommsBand.ReservedBottom, …)</c> — a FIXED screen row — and left
/// it there. A plate pinned to a fixed row of a camera that is pinned to the captain is a plate pinned to the
/// captain. The sim had a sentry twenty deck units behind him; the pen drew a number over his head. Third
/// named bug class: a DRAWN SHAPE reporting something the sim never said.</para>
///
/// <para><b>Why these guards are measured off a pen and a projection, never off typed pixels.</b> The plate
/// coordinates depend on the plan, the viewport and where the captain is standing. So a real
/// <see cref="DeckView"/> draws the frame onto a measuring pen, and where the sentry's own mark landed is
/// asked of <see cref="DeckView.PlacementFor"/> — the one projection the frame itself draws with (#729) —
/// rather than re-derived here. A guard that restated the plate arithmetic would be the fifth named class: a
/// green test that asserts nothing.</para>
///
/// <para>RED PROOF — the gate was dropped from <c>DeckView.DrawTheSentries</c> and these were watched go red
/// before the fix was trusted:</para>
/// <code>
/// ACounterIsNeverDrawnWhereItsSentrysMarkIsNot [FAIL]
///   #1039 · the sentry's mark is at (640,-240) — 240px above the top of the glass — and its scoreboard is
///   drawn anyway at y=47, on a frame that is centred on the captain. That is the count following the walker.
///
/// TheCountStaysLeashedToItsSentryAsTheCaptainWalksAway [FAIL]
///   #1039 · 20 deck units down the field the sentry's mark is at (640,-40) and its magazine plate is 107px
///   away at y=47. …
/// </code>
/// <para><see cref="ASentryOnTheGlassStillWearsItsDrumRightAboveItsMark"/> stayed GREEN through that run, which
/// is the half of the proof that matters second: the law being kept is an anchor, not a mute.</para>
/// </summary>
public sealed class TheMagazineCountRidesItsOwnSentryTests
{
    private const int WidthPx = 1280, HeightPx = 720;

    /// <summary>The Tilt's ground — the world the owner was walking when he saw it.</summary>
    private const string Ground = "surface:miranda:0";

    /// <summary>GATE-1's readout: a door sentry's drum is never spent, so it always reads full.</summary>
    private const string Magazine = "99";

    /// <summary>The dark scoreboard panel #314 seats its two digits on, picked out by its ink rather than by
    /// a coordinate — the same handle the #986 F2 guard next door uses.</summary>
    private static readonly RgbaColor Scoreboard = new(16, 10, 10, 225);

    // ── THE PEN THAT MEASURES ─────────────────────────────────────────────────────────────────────────

    private readonly record struct Word(float X, float Y, string Text);

    private readonly record struct Mark(float X, float Y, float W, float H, RgbaColor Ink);

    private sealed class MeasuringPen : IRenderer
    {
        public List<Mark> Fills { get; } = [];

        public List<Word> Words { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background)
        {
        }

        public void EndFrame()
        {
        }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f)
        {
        }

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f)
        {
        }

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f)
        {
        }

        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f)
        {
            if (fill is { } ink && Rect(pointsXY) is { } m)
            {
                Fills.Add(m with { Ink = ink });
            }
        }

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f)
        {
        }

        public void DrawText(float x, float y, string text, RgbaColor color,
                             string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Words.Add(new Word(x, y, text));

        private static Mark? Rect(ReadOnlySpan<float> xy)
        {
            if (xy.Length != 8)
            {
                return null;
            }
            float x0 = xy[0], y0 = xy[1], x1 = xy[4], y1 = xy[5];
            bool square =
                Math.Abs(xy[2] - x1) < 1e-3f && Math.Abs(xy[3] - y0) < 1e-3f &&
                Math.Abs(xy[6] - x0) < 1e-3f && Math.Abs(xy[7] - y1) < 1e-3f;
            return square
                ? new Mark(Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x1 - x0), Math.Abs(y1 - y0), default)
                : null;
        }
    }

    // ── ONE FRAME OF THE FIELD, WITH GATE-1 STANDING WHERE THE SHUTTLE LEFT IT ─────────────────────────

    private static DeckPlan TheField() => Scenes.Build(Ground);

    /// <summary>Draw one frame with the captain standing at <paramref name="captainY"/> and a single deployed
    /// sentry holding station at <paramref name="botY"/>, both on the shuttle's own column.</summary>
    private static MeasuringPen AFrameWithTheCaptainAt(double captainY, double botX, double botY)
    {
        DeckPlan field = TheField();
        var state = new DeckView.State(field.SpawnX, captainY, 0.9, 0, 0,
            ShuttleAway: false, ElectricUniverse: false,
            Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, NerveCompact: false, HitsTaken: 0);
        var hud = new DeckView.SurfaceHud(
            DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            Blips: [], Cadence: 2, Readout: "no movement — for now",
            CacheMarks: [], Nerve: 58, NerveReadout: "FRAYED", Instruments: true,
            Bots: [(botX, botY, Magazine, false, false, 0.0, 0.0)]);

        var pen = new MeasuringPen();
        new DeckView(pen).Draw(field, WidthPx, HeightPx, simTime: 0.0, in state, surface: hud);
        return pen;
    }

    /// <summary>Where that sentry's own mark landed on the glass, asked of the projection the frame draws
    /// with rather than worked out again here.</summary>
    private static (float X, float Y, float Scale) TheSentrysMarkOnTheGlass(double captainY, double botX, double botY)
    {
        DeckPlan field = TheField();
        Assert.True(field.FollowCam,
            "#1039 is a FollowCam bug — if this ground no longer scrolls with the captain the guard is "
            + "watching the wrong frame");
        DeckView.Placement place =
            DeckView.PlacementFor(field, WidthPx, HeightPx, field.SpawnX, captainY, 0, 0);
        return (place.Ox + ((float)botX * place.Scale),
                place.Oy - ((float)botY * place.Scale),
                place.Scale);
    }

    private static Mark? TheScoreboard(MeasuringPen pen) =>
        pen.Fills.Where(m => m.Ink == Scoreboard).Select(m => (Mark?)m).SingleOrDefault();

    private static int TheDigits(MeasuringPen pen) => pen.Words.Count(w => w.Text == Magazine);

    /// <summary>The furthest a plate seated by its own sentry can ever be from that sentry's mark: the gap
    /// over the bot box plus the plate's own height, and — for a bot standing in the very top of the frame —
    /// the ship's reserved band it may be pushed below. Anything past this is not a seat, it is a station.</summary>
    private static double LeashPx(float scale) =>
        CommsBand.ReservedBottom + (0.8 * scale) + (2.0 * scale);

    // ── GUARD (a) · NO MARK, NO COUNTER ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The captain walks down the field until the door sentry is off the top of the glass. Its magazine plate
    /// goes with it: a counter is its sentry's instrument and cannot be drawn where the sentry is not.
    ///
    /// <para>RED PROOF: drop the <c>markIsOnTheGlass</c> gate in <c>DeckView.DrawTheSentries</c> and the plate
    /// reappears parked under the ship's line with its sentry hundreds of pixels above the frame.</para>
    /// </summary>
    [Fact]
    public void ACounterIsNeverDrawnWhereItsSentrysMarkIsNot()
    {
        // GATE-1 at the tube mouth; the captain thirty deck units down the field, which is where the owner
        // was standing. Both positions are the fiction's own, not a hunt for a pixel.
        const double botY = MoonSurface.SurfaceTopY + 2, captainY = botY - 30;
        (float mx, float my, _) = TheSentrysMarkOnTheGlass(captainY, MoonSurface.SpawnX, botY);
        Assert.True(my < 0,
            $"the guard's own setup is wrong: the sentry's mark is at y={my:0.#}, still on a {HeightPx}px "
            + "frame, so there is nothing off-glass to be lied about");

        MeasuringPen pen = AFrameWithTheCaptainAt(captainY, MoonSurface.SpawnX, botY);

        Mark? board = TheScoreboard(pen);
        Assert.True(board is null,
            $"#1039 · the sentry's mark is at ({mx:0.#},{my:0.#}) — {-my:0.#}px above the top of the glass — "
            + $"and its scoreboard is drawn anyway at y={board?.Y ?? 0:0.#}, on a frame that is centred on the "
            + "captain. That is the count following the walker.");
        Assert.Equal(0, TheDigits(pen));
    }

    // ── GUARD (b) · …AND EVERY SENTRY YOU CAN SEE STILL WEARS ITS DRUM ────────────────────────────────

    /// <summary>
    /// The fix is an anchor, not a mute. With the captain standing beside the same sentry the plate is drawn,
    /// carries the digits, and sits directly over the mark it belongs to.
    /// </summary>
    [Fact]
    public void ASentryOnTheGlassStillWearsItsDrumRightAboveItsMark()
    {
        const double botY = MoonSurface.SurfaceTopY + 2, captainY = botY - 4;
        (float mx, float my, float scale) = TheSentrysMarkOnTheGlass(captainY, MoonSurface.SpawnX, botY);

        MeasuringPen pen = AFrameWithTheCaptainAt(captainY, MoonSurface.SpawnX, botY);
        Mark? seated = TheScoreboard(pen);
        Assert.True(seated.HasValue,
            $"#1039 · the sentry's mark is on the glass at ({mx:0.#},{my:0.#}) and it is wearing no drum at "
            + "all — the fix is meant to ANCHOR the counter to its bot, never to mute it");
        Mark board = seated!.Value;

        Assert.Equal(1, TheDigits(pen));
        Assert.True(Math.Abs((board.X + (board.W / 2)) - mx) < 1e-2,
            $"#1039 · the plate is centred at x={board.X + (board.W / 2):0.#} and its sentry's mark at "
            + $"x={mx:0.#} — a counter that is not over its own bot's column is over somebody else's");
        Assert.True(Math.Abs((board.Y + (board.H / 2)) - my) <= LeashPx(scale),
            $"#1039 · the plate's centre is at y={board.Y + (board.H / 2):0.#} and its sentry's mark at "
            + $"y={my:0.#} — further than a seat, which makes it a station on the glass rather than an "
            + "instrument on the bot");
    }

    // ── GUARD (c) · THE OWNER'S OWN SENTENCE, WALKED ──────────────────────────────────────────────────

    /// <summary>
    /// Walk the captain away from a sentry that never moves, one deck unit at a time, and in every single
    /// frame the counter — if it is drawn at all — is still leashed to that sentry's mark. It may never take
    /// up a fixed station on a camera that is itself pinned to the captain.
    ///
    /// <para>RED PROOF: drop the <c>markIsOnTheGlass</c> gate and this fails at the first step past the top
    /// edge, printing the plate holding station while its sentry walks off the frame.</para>
    /// </summary>
    [Fact]
    public void TheCountStaysLeashedToItsSentryAsTheCaptainWalksAway()
    {
        const double botY = MoonSurface.SurfaceTopY + 2;
        var seenOnTheGlass = 0;

        for (var step = 0; step <= 40; step++)
        {
            double captainY = botY - step;
            (float mx, float my, float scale) = TheSentrysMarkOnTheGlass(captainY, MoonSurface.SpawnX, botY);
            MeasuringPen pen = AFrameWithTheCaptainAt(captainY, MoonSurface.SpawnX, botY);

            if (TheScoreboard(pen) is not { } board)
            {
                continue;   // no mark on the glass, no counter — the law's other half, guarded above
            }

            seenOnTheGlass++;
            double dy = Math.Abs((board.Y + (board.H / 2)) - my);
            Assert.True(dy <= LeashPx(scale),
                $"#1039 · {step} deck units down the field the sentry's mark is at ({mx:0.#},{my:0.#}) and its "
                + $"magazine plate is {dy:0.#}px away at y={board.Y:0.#}. The frame is centred on the captain, "
                + "so a plate that stops tracking its bot is a plate tracking HIM — which is exactly what the "
                + "owner saw: \"the magazine count follows the walker\".");
        }

        Assert.True(seenOnTheGlass > 4,
            $"the walk only ever drew {seenOnTheGlass} counters — a guard that never saw the plate cannot "
            + "prove where it was seated");
    }
}
