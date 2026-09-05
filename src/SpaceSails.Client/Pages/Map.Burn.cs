using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: #167 — the one place a burn becomes a thing the captain can see and hear. The hook every burn
// kind fires, the pen that draws its flame off the stern, and the beat that washes along the path ahead.
public partial class Map
{
    /// <summary>
    /// #167 · THE ONE PLACE A BURN IS FELT. Owner, 2026-07-16: <i>"Now we should have some burn-happening
    /// sound and visual effect also."</i>
    ///
    /// <para><b>Why one hook and not eight.</b> The issue's own words are <i>"one hook where the impulse is
    /// applied"</i>, and the reason is in the sentence before them: burns are increasingly things the ship
    /// does BY ITSELF. There are nine places in this client where the ship's velocity changes because the
    /// drive fired — a hand on the arrow keys, a plotted node retiring, the autopilot's approach, its
    /// insertion, its station-keeping trim, a scheduled transfer burn at its epoch, the panel's own orbital
    /// insertion, the terminal dock match, and the arrival brake. Feedback bolted onto each of them
    /// separately would be feedback that is present at eight of them and missing at the ninth, and the
    /// ninth would be whichever one was added last. So every one of them ends here, and
    /// <c>EveryBurnIsFeltTests</c> enumerates them and drives them to prove it.</para>
    ///
    /// <para><b>Two things happen and they are the same event.</b> The picture is armed (three fields, read
    /// by <see cref="DrawBurnPlume"/> on every frame until the window closes) and the <c>burn</c> cue is
    /// fired ONCE, scaled by <see cref="BurnPlume.CueScale"/> — the same number the flame is sized by, so
    /// the ear and the eye cannot come to disagree about how big the burn was. There is no other
    /// <c>PlayCue("burn")</c> in the client, and a guard says so: that is what makes "one cue per burn"
    /// a property of the code rather than a habit.</para>
    ///
    /// <para><b>The clock is the player's.</b> <c>_lastBurnMs</c> is the RENDERER's timestamp, not
    /// <c>SimTime</c> — the #1086 two-clock rule, and the issue's third bullet says why: at 10,000× warp
    /// the impulse and the next four minutes of sim time land inside one frame, so an effect timed in sim
    /// seconds would be over before the frame that drew it was flushed.</para>
    ///
    /// <para><b>A burn that moved nothing is not a burn.</b> Zero (or negative) pulses, or a Δv that is
    /// zero or not finite, arms nothing and sounds nothing: there would be no direction to throw exhaust
    /// along, and the honest picture of a drive that did not fire is no picture.</para>
    /// </summary>
    /// <param name="pulses">Reaction-mass pulses this burn actually spent — how big it was.</param>
    /// <param name="deltaV">Where it PUSHED her, in world space. The exhaust goes the other way
    /// (<see cref="BurnPlume.ExhaustAngle"/>); only the direction is read, never the magnitude.</param>
    private void BurnFired(int pulses, Vector2d deltaV)
    {
        if (pulses <= 0 || !double.IsFinite(deltaV.X) || !double.IsFinite(deltaV.Y) || deltaV.LengthSquared <= 0)
        {
            return;
        }

        _lastBurnPulses = pulses;
        _lastBurnDeltaV = deltaV;
        _lastBurnMs = _lastTimestampMs ?? 0;
        _burnsFired++;

        RendererInterop.PlayCue("burn", BurnPlume.CueScale(pulses));
    }

    /// <summary>When she last fired, in RENDERER-clock milliseconds. See <see cref="BurnFired"/> on why
    /// this is not sim time. Negative infinity is a ship that has never burned, and
    /// <see cref="BurnPlume.Shape"/> answers <see cref="BurnPlume.None"/> for it without being asked
    /// twice.</summary>
    private double _lastBurnMs = double.NegativeInfinity;

    /// <summary>…how many pulses that burn spent — the flame's size and the cue's.</summary>
    private int _lastBurnPulses;

    /// <summary>…and which way it pushed her, in world space. The flame leaves the other side.</summary>
    private Vector2d _lastBurnDeltaV;

    /// <summary>How many burns have gone through the hook this session. Nothing in the game reads it; it is
    /// what lets a guard say "this kind fired exactly once" about a seam whose only other outputs are a
    /// sound the bench cannot hear and a picture that fades.</summary>
    private int _burnsFired;

    /// <summary>The plume as it stands this frame — <see cref="BurnPlume.None"/> whenever nothing is
    /// burning, which is nearly always.</summary>
    private BurnPlume.Plume ThePlumeRightNow =>
        BurnPlume.Shape(_lastBurnPulses, (_lastTimestampMs ?? 0) - _lastBurnMs);

    /// <summary>
    /// Her drive, and what is coming off it — and <b>the geometry is not here</b>. It is
    /// <see cref="BurnPlume"/> in Core, where a test can ask it questions; this is a pen that walks what
    /// Core hands back and nothing else, the discipline <c>DrawDischarge</c> was built to.
    ///
    /// <para>Drawn every map frame while the window is open, paused included: the flash is on the wall
    /// clock, so a captain who hits pause the instant an automated burn fires still watches it fade. It is
    /// laid BEFORE the hull dot and the velocity dart, so the marker sits on top of its own exhaust rather
    /// than under it.</para>
    ///
    /// <para>The ink is <c>ShipColor</c> — her own. A drive plume is the one thing on this chart that is
    /// unambiguously the ship doing something, and giving it a colour of its own would have been a new
    /// token saying what an existing one already says.</para>
    /// </summary>
    private void DrawBurnPlume(float sx, float sy)
    {
        BurnPlume.Plume plume = ThePlumeRightNow;
        if (!plume.Draws)
        {
            return;
        }

        // World Y is up, canvas Y is down (Camera.WorldToScreen flips it) — so the screen angle is the world
        // angle mirrored, and the flip lives HERE, once, at the one call that crosses from one to the other.
        // The same rule DrawVelocityArrowhead and DrawDischarge obey.
        double thrustScreenRad = Math.Atan2(-_lastBurnDeltaV.Y, _lastBurnDeltaV.X);

        (double nozzleX, double nozzleY) = BurnPlume.Nozzle(thrustScreenRad);
        float throatX = sx + (float)nozzleX;
        float throatY = sy + (float)nozzleY;

        // One buffer for the whole flame: a stackalloc inside the loop is a frame-rate-shaped foot-gun
        // (CA2014), and this runs on every rendered frame of the window.
        Span<double> feathers = stackalloc double[BurnPlume.MaxFeathers * BurnPlume.FloatsPerFeather];
        int written = BurnPlume.Feathers(
            plume, thrustScreenRad, BurnPlume.FlashPhase((_lastTimestampMs ?? 0) - _lastBurnMs), feathers);

        RgbaColor ink = ShipColor with { A = BurnPlume.Alpha(plume) };
        float widthPx = BurnPlume.StrokePx(plume);
        Span<float> feather = stackalloc float[BurnPlume.FloatsPerFeather];
        for (int i = 0; i < written; i += BurnPlume.FloatsPerFeather)
        {
            feather[0] = sx + (float)feathers[i];
            feather[1] = sy + (float)feathers[i + 1];
            feather[2] = sx + (float)feathers[i + 2];
            feather[3] = sy + (float)feathers[i + 3];
            feather[4] = sx + (float)feathers[i + 4];
            feather[5] = sy + (float)feathers[i + 5];
            _renderer!.DrawPolyline(feather, ink, widthPx);
        }

        // The throat sits ON the nozzle, because that is where the drive is.
        _renderer!.DrawCircle(throatX, throatY, BurnPlume.CoreRadiusPx(plume), ink, ink);
    }

    /// <summary>
    /// #167, the issue's other half of the picture: <i>"the plotted path segment ahead re-tints for a
    /// beat."</i> A burn is the moment the line she is flying stops being the line she was flying, and the
    /// wash says WHERE that took effect — the next stretch, not the whole ribbon.
    ///
    /// <para>It re-strokes the ribbon's own leading vertices, in <c>ShipColor</c> over the ribbon's amber,
    /// at the flame's own alpha — so it fades out with the flame instead of running to a second timetable.
    /// No new colour token: this is the ship's ink washing forward along her own track.</para>
    ///
    /// <para><see cref="_ribbonVertexCount"/> is how many vertices <c>DrawShipTrajectory</c> actually laid
    /// into <c>_scratch</c> this frame, and it is zero on every frame that drew no ballistic ribbon at all
    /// (docked, autopilot flying its own rehearsed path, too few samples). Reading it rather than
    /// re-deriving those conditions is what stops this from washing a line that is not on the glass.</para>
    /// </summary>
    private void DrawTheBurnBeatAlongThePathAhead()
    {
        int vertices = BurnPlume.BeatVertices(_ribbonVertexCount);
        if (vertices < 2)
        {
            return;
        }

        BurnPlume.Plume plume = ThePlumeRightNow;
        if (!plume.Draws)
        {
            return;
        }

        _renderer!.DrawPolyline(
            _scratch.AsSpan(0, vertices * 2), ShipColor with { A = BurnPlume.BeatAlpha(plume) }, 2.5f);
    }

    /// <summary>How many vertices of the ballistic ribbon are live in <c>_scratch</c> this frame. Zero
    /// means no ribbon was drawn — see <see cref="DrawTheBurnBeatAlongThePathAhead"/>.</summary>
    private int _ribbonVertexCount;
}
