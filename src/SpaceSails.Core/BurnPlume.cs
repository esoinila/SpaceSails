namespace SpaceSails.Core;

/// <summary>
/// #167 · A BURN MUST BE FELT. Owner (2026-07-16, before the Enceladus parcel run): <i>"Now we should have
/// some burn-happening sound and visual effect also."</i>
///
/// <para>Burns in this game are increasingly things the ship does BY ITSELF — the #152/#165 scheduled
/// transfer burns fire at their own epochs, usually at four figures of warp, and the autopilot's approach
/// and insertion burns fire inside a tick the captain never touched. Until this type existed the most
/// consequential instant in the game was a line of toast text. This is the picture that instant makes.</para>
///
/// <h3>The exhaust leaves the STERN, opposite the push</h3>
/// <para>The one physical claim here, and the whole reason the geometry is a type rather than a literal in
/// the renderer: a drive that pushes the ship one way throws its reaction mass the other. So every feather
/// in this plume leaves <see cref="Nozzle"/> — <see cref="NozzlePx"/> off the marker along
/// <see cref="ExhaustAngle"/>, which is the burn's own direction turned through π — and fans about that
/// same line. A flame drawn on the side she is accelerating TOWARD is the one picture Newton rules out, and
/// it is exactly the picture a renderer draws by accident.</para>
///
/// <h3>Why the geometry is in Core and not in the renderer</h3>
/// <para>The same reason <see cref="DischargePlume"/> is: this repo's first named bug class is a client
/// geometry literal nobody could put a test on, and its third is the drawn shape reporting one thing while
/// the sim says another. So the picture is a pure function here — feather count, lengths, angles, reach and
/// intensity for a given burn — and <c>Map.Burn.cs</c>'s <c>DrawBurnPlume</c> is a pen that walks what this
/// returns and nothing else.</para>
///
/// <h3>ONE clock, and it is the player's</h3>
/// <para><see cref="DischargePlume"/> wrote the two-clock rule down (#1086): a FLASH is on the real clock,
/// because it is a statement about the player's eye; a CRAWL is on sim time, because it is a statement
/// about the world. A burn is entirely the first kind, and the issue says why in one line: <i>"at high warp
/// a burn instant can pass in one frame — the effect should be wall-clock-timed (~1 s), not
/// sim-time-timed, so it reads at any warp."</i> At 10,000× the impulse and the next four minutes of sim
/// time land inside a single frame; a plume that decayed on sim time would be over before the frame that
/// drew it was flushed. So <see cref="FlashMs"/> is real milliseconds, and there is no sim-time term in
/// this file at all.</para>
///
/// <h3>No <c>Random</c>, and no per-process seed either</h3>
/// <para>The flutter is a hash of a quantised phase, so the same instant always draws the same feathers —
/// a renderer that rolled dice would draw a different picture on every re-render of one instant, which
/// breaks any hope of a test reading the pen. The mix in <see cref="Flutter"/> is spelled out by hand
/// rather than taken from <c>System.HashCode</c>, which seeds itself once per PROCESS: identical
/// arguments would then give one picture today and another tomorrow, and a frame that cannot be reproduced
/// across two runs cannot be pinned in a fingerprint ledger.</para>
/// </summary>
public static class BurnPlume
{
    // ── Where the nozzle is ───────────────────────────────────────────────────────────────────────────

    /// <summary>How far her stern stands off the marker, in SCREEN PIXELS. Sized like
    /// <see cref="DischargePlume.MastPx"/> and for the same reason: the map spans thirteen orders of
    /// magnitude of zoom, so a nozzle sized in world metres would be a hair at one end of that range and
    /// would swallow the solar system at the other. It is a glyph, like the ship dot.</summary>
    public const double NozzlePx = 7.0;

    /// <summary>
    /// The direction the exhaust goes, given the direction the burn PUSHES — the physical claim of this
    /// type in one line, and the only place the turn through π happens.
    ///
    /// <para>Both angles are SCREEN angles: 0 = +X (right), increasing CLOCKWISE on the canvas, because
    /// canvas Y points down while world Y points up. A caller holding a world Δv crosses over once, at the
    /// one call that crosses, exactly as <c>DrawVelocityArrowhead</c> requires.</para>
    /// </summary>
    public static double ExhaustAngle(double thrustScreenAngleRad) => thrustScreenAngleRad + Math.PI;

    /// <summary>
    /// The nozzle, in ship-local SCREEN pixels — the point every feather leaves from. Ship-local means
    /// (0,0) is the ship marker itself, so a caller adds her screen pixel and nothing else.
    ///
    /// <para>That the answer is <see cref="NozzlePx"/> away from the origin, on the side AWAY from
    /// <paramref name="thrustScreenAngleRad"/>, is the whole law: a flame at the marker's centre says
    /// nothing, and a flame on the side she is accelerating toward says something false.</para>
    /// </summary>
    public static (double X, double Y) Nozzle(double thrustScreenAngleRad)
    {
        double exhaust = ExhaustAngle(thrustScreenAngleRad);
        return (Math.Cos(exhaust) * NozzlePx, Math.Sin(exhaust) * NozzlePx);
    }

    // ── How big, and for how long ─────────────────────────────────────────────────────────────────────

    /// <summary>How long a burn stays on screen, in REAL milliseconds. The issue's own number: <i>"the
    /// effect should be wall-clock-timed (~1 s) … so it reads at any warp."</i></summary>
    public const double FlashMs = 1000.0;

    /// <summary>How long one step of the flutter lasts, in real milliseconds — eleven steps across the
    /// whole window, which reads as a flame rather than as an animation.</summary>
    public const double FlashStepMs = 90.0;

    /// <summary>The pulse count at which the plume is as big as it ever gets. Chosen off the game's own
    /// scale rather than picked out of the air: a hand-flown trim is one pulse, a plotted node is a
    /// handful, and an orbital insertion at a moon is tens — so a captain who has seen both can tell
    /// them apart at a glance, and anything larger than an insertion is simply "the biggest burn there
    /// is".</summary>
    public const int FullPulses = 40;

    /// <summary>
    /// How big a burn of this many pulses is, 0…1 — monotone, and saturating at
    /// <see cref="FullPulses"/>.
    ///
    /// <para>The square root, not the ratio: one pulse against forty would otherwise be 2.5% of a plume,
    /// which is nothing at all on the glass, and the point of scaling by pulses is that a small burn still
    /// READS as a burn while a large one dwarfs it. Zero pulses is not a small burn — it is not a
    /// burn.</para>
    /// </summary>
    public static double Brightness(int pulses) =>
        pulses <= 0 ? 0.0 : Math.Clamp(Math.Sqrt(pulses / (double)FullPulses), 0.0, 1.0);

    /// <summary>How loud and how long the <c>burn</c> cue is for a burn of this many pulses, 0…1 — the
    /// SAME number the picture is scaled by, so the ear and the eye can never come to disagree about how
    /// big the burn was. <c>renderer.js</c>'s <c>playCue</c> takes this as its second argument.</summary>
    public static double CueScale(int pulses) => Brightness(pulses);

    // ── The shape ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The fewest feathers any drawn plume has — a one-pulse trim.</summary>
    public const int MinFeathers = 3;

    /// <summary>…and the most, at <see cref="FullPulses"/>. Also the size a caller's buffer must be.</summary>
    public const int MaxFeathers = 6;

    /// <summary>Floats per feather in <see cref="Feathers"/>' output: three points, x,y each. One kink, so
    /// the flame reads as exhaust rather than as spokes — the <see cref="DischargePlume"/> encoding.</summary>
    public const int FloatsPerFeather = 6;

    /// <summary>Half the angle the feathers fan across, in radians. Kept well inside a right angle so that
    /// every feather — kink included — still travels AFT: a nozzle whose flame can reach sideways past the
    /// beam is drawing a burn in a direction the ship is not burning.</summary>
    public const double SpreadRad = 0.55;

    /// <summary>Half the angle a feather's second half may bend by. <see cref="SpreadRad"/> + this stays
    /// under π/2 — see that field.</summary>
    public const double KinkRad = 0.35;

    /// <summary>What the plume looks like this instant, with nothing about WHERE she is or which way she
    /// pushed.</summary>
    /// <param name="Intensity">0…1. Zero means draw nothing at all — not a dim plume, nothing.</param>
    /// <param name="Feathers">How many streaks leave the nozzle.</param>
    /// <param name="ReachPx">How far the longest of them may reach, in screen pixels off the nozzle.</param>
    public readonly record struct Plume(double Intensity, int Feathers, double ReachPx)
    {
        /// <summary>Whether there is anything on screen at all. The one question the renderer asks first.</summary>
        public bool Draws => Intensity > 0 && Feathers > 0;
    }

    /// <summary>Nothing is burning: what <see cref="Shape"/> returns for a quiet ship, and what the pen
    /// checks for before it reads anything else.</summary>
    public static readonly Plume None = new(0.0, 0, 0.0);

    /// <summary>
    /// The plume for a burn of <paramref name="pulses"/> fired <paramref name="sinceBurnMs"/> REAL
    /// milliseconds ago.
    ///
    /// <para>Nothing burned, nothing draws — <see cref="None"/> for zero (or negative) pulses, for a burn
    /// that has not happened yet, and for one whose <see cref="FlashMs"/> window has closed. A ship that
    /// has never fired is the <see cref="double.NegativeInfinity"/> case a caller starts from, and it lands
    /// in the closed-window branch on its own.</para>
    /// </summary>
    public static Plume Shape(int pulses, double sinceBurnMs)
    {
        if (pulses <= 0 || double.IsNaN(sinceBurnMs) || sinceBurnMs < 0 || sinceBurnMs >= FlashMs)
        {
            return None;
        }

        double size = Brightness(pulses);
        double intensity = size * (1.0 - (sinceBurnMs / FlashMs));
        if (intensity <= 0)
        {
            return None;
        }

        return new Plume(
            intensity,
            MinFeathers + (int)Math.Round(size * (MaxFeathers - MinFeathers)),
            NozzlePx * (1.3 + (size * 2.4)));
    }

    /// <summary>The flutter's phase, in steps, from the real milliseconds since the burn. The one clock
    /// this file has — see the class note.</summary>
    public static double FlashPhase(double sinceBurnMs) => sinceBurnMs / FlashStepMs;

    /// <summary>
    /// Write the plume's feathers into <paramref name="into"/> as x,y,x,y,x,y per feather — three points
    /// each, in ship-local SCREEN pixels, and <b>every one of them starting at <see cref="Nozzle"/> and
    /// travelling aft</b>. Returns how many floats were written.
    ///
    /// <para>Pure. The same arguments give the same floats, always, in this process and in the next one:
    /// the flutter is <see cref="Flutter"/> of <paramref name="phase"/>'s whole part. No <c>Random</c>, no
    /// clock read inside, nothing cached.</para>
    /// </summary>
    /// <param name="into">At least <c>plume.Feathers * <see cref="FloatsPerFeather"/></c> floats. Shorter
    /// and this throws rather than drawing half a flame somebody would have to explain.</param>
    public static int Feathers(in Plume plume, double thrustScreenAngleRad, double phase, Span<double> into)
    {
        int need = plume.Feathers * FloatsPerFeather;
        if (into.Length < need)
        {
            throw new ArgumentException(
                $"a plume of {plume.Feathers} feathers needs {need} floats.", nameof(into));
        }

        if (!plume.Draws)
        {
            return 0;
        }

        double exhaust = ExhaustAngle(thrustScreenAngleRad);
        (double nozzleX, double nozzleY) = Nozzle(thrustScreenAngleRad);

        // The flame is held still WITHIN a step and re-drawn at the next one: a burn snaps and flutters, it
        // does not sweep. (DischargePlume's arcing crawl sweeps because it is a state; this is an event.)
        long step = (long)Math.Floor(phase);

        int w = 0;
        for (int i = 0; i < plume.Feathers; i++)
        {
            uint h = Flutter(step, i);
            double spread = (((h & 0xFFFF) / 65535.0) - 0.5) * 2.0 * SpreadRad;
            double length = plume.ReachPx * (0.5 + ((((h >> 16) & 0xFF) / 255.0) * 0.5));
            double angle = exhaust + spread;

            double midX = nozzleX + (Math.Cos(angle) * length * 0.5);
            double midY = nozzleY + (Math.Sin(angle) * length * 0.5);
            double kink = angle + (((((h >> 24) & 0xFF) / 255.0) - 0.5) * 2.0 * KinkRad);
            double tipX = midX + (Math.Cos(kink) * length * 0.5);
            double tipY = midY + (Math.Sin(kink) * length * 0.5);

            into[w] = nozzleX; into[w + 1] = nozzleY;
            into[w + 2] = midX; into[w + 3] = midY;
            into[w + 4] = tipX; into[w + 5] = tipY;
            w += FloatsPerFeather;
        }

        return w;
    }

    /// <summary>The flutter: a deterministic scramble of a step and a feather index. See the class note
    /// for why this is written out instead of calling <c>HashCode.Combine</c>.</summary>
    private static uint Flutter(long step, int feather)
    {
        ulong x = ((ulong)step * 0x9E3779B97F4A7C15UL)
                  + ((ulong)(uint)feather * 0xBF58476D1CE4E5B9UL)
                  + 0xD6E8FEB86659FD93UL;
        x ^= x >> 30; x *= 0xBF58476D1CE4E5B9UL;
        x ^= x >> 27; x *= 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return (uint)x;
    }

    // ── What the pen does with it ─────────────────────────────────────────────────────────────────────

    /// <summary>The flame's alpha for a plume of this intensity, 0…255. Never fully transparent while it
    /// draws at all, so a feather in the buffer is a feather on the glass.</summary>
    public static byte Alpha(in Plume plume) =>
        (byte)Math.Clamp(70 + (plume.Intensity * 185), 0, 255);

    /// <summary>How wide the feathers are drawn, in pixels.</summary>
    public static float StrokePx(in Plume plume) => plume.Intensity > 0.5 ? 2f : 1.4f;

    /// <summary>The bright throat, and it sits ON the nozzle because that is where the drive is. Small,
    /// and biggest at the instant of the burn.</summary>
    public static float CoreRadiusPx(in Plume plume) => (float)(1.6 + (plume.Intensity * 1.4));

    // ── …and what the RIBBON does with it ─────────────────────────────────────────────────────────────

    /// <summary>How much of the drawn path ahead re-tints for the beat, as a fraction of the plotted
    /// ribbon. The issue asks for <i>"the plotted path segment ahead"</i>, not the whole line: a wash that
    /// ran the length of the ribbon would say "everything changed", where what actually changed is the
    /// next stretch she is about to fly.</summary>
    public const double BeatPathFraction = 0.2;

    /// <summary>How many vertices of the ribbon the beat covers, given how many it has. Always at least a
    /// segment (two vertices) so a short ribbon still shows the beat, never more than there are.</summary>
    public static int BeatVertices(int vertexCount) =>
        vertexCount < 2 ? 0 : Math.Clamp((int)(vertexCount * BeatPathFraction), 2, vertexCount);

    /// <summary>The beat's alpha along that stretch, 0…255 — the same intensity as the flame, so the wash
    /// fades out with it rather than on a second timetable.</summary>
    public static byte BeatAlpha(in Plume plume) =>
        (byte)Math.Clamp(plume.Intensity * 210, 0, 255);
}
