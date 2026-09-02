namespace SpaceSails.Core;

/// <summary>
/// #528 §7 · THE PLASMA BALL THAT IS NOT A BALL. Owner, on the charge board (#523): <i>"the charge is being
/// equalized could have plasma ball like beautifull effect if physics supports it."</i>
///
/// <para>It supports something better than a ball, and the issue said so itself: <b>a real discharge is a PLUME
/// from the sharpest extremity, never a sphere around the ship.</b> Field strength is potential over radius of
/// curvature, so her antenna whip runs some 20,000× the field of her hull skin. A glow drawn as a halo about the
/// hull is drawn in the one place a discharge can never start. Every filament here therefore leaves the
/// MASTHEAD — see <see cref="Masthead"/> — and the guard that says so is the point of this type existing.</para>
///
/// <h3>Why the geometry is in Core and not in the renderer</h3>
/// <para>This repo's third named bug class is the drawn shape reporting one thing while the sim says another,
/// and its first is a client geometry literal nobody could put a test on. So the picture is a pure function
/// here — filament count, lengths, angles and intensity for a given dumped charge, band and phase — and
/// <c>Map.Plot.Bodies.cs</c>'s <c>DrawDischarge</c> is nothing but a pen that walks what this returns.</para>
///
/// <h3>ONE scale, and it is the sensors' own</h3>
/// <para>Brightness is not a second opinion about how loud she was. <see cref="DumpBrightness"/> is
/// <see cref="HullCharge.SeenFartherFactor"/>'s own excess, normalised — the same arithmetic the sensors run
/// and the charge board prints. Retune <c>ChargeGlowFactor</c> and the plume follows without anybody editing
/// it, which is exactly what a second constant here would have prevented.</para>
///
/// <h3>Two states, two clocks</h3>
/// <list type="bullet">
/// <item><b>A DUMP</b> is one bright snap and an afterglow over <see cref="FlashMs"/> of REAL time. The honest
/// event is 2.2 ms through the contactor (Lab 43 §C); 600 ms is a stylisation of about 300× and the smallest
/// lie a player can still see happen. What it must not be is slow-motion lightning — 0.22 J is a static shock
/// off a door handle, so the picture is a filament and a snap, never a fireball.</item>
/// <item><b>ARCING</b> is a slow crawl of short filaments off the whip, on SIM time, so the band is readable
/// from the map without opening the panel — the same principle as the vacuum clocks being readable from the
/// corridor. Sim time and not the wall clock, because a paused map must draw the same frame twice: a plume
/// that danced while the world was stopped would be the one thing on this chart that is not state.</item>
/// </list>
///
/// <h3>No <c>Random</c>, ever</h3>
/// <para>The flicker is a hash of a quantised phase. A renderer that rolled dice would draw a different picture
/// on every re-render of the same instant, which breaks the paused frame, the fingerprint ledgers and any hope
/// of a test reading the pen.</para>
/// </summary>
public static class DischargePlume
{
    // ── The whip ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How far her whip stands off the hull, in SCREEN PIXELS. The map spans thirteen orders of
    /// magnitude of zoom (<c>Camera.MetersPerPixel</c> 1e-2 → 1e11); a mast sized in world metres would be a
    /// hair at one end of that and would swallow the solar system at the other. It is a glyph, like the ship
    /// dot and like the velocity dart.</summary>
    public const double MastPx = 11.0;

    /// <summary>
    /// The masthead, in ship-local SCREEN pixels — the point every filament leaves from, and the whole
    /// physical claim of this type in one function.
    ///
    /// <para>Ship-local means (0,0) is the ship marker itself, so a caller adds her screen pixel and nothing
    /// else. That the answer is <see cref="MastPx"/> away from the origin rather than at it is the plume law:
    /// a discharge centred on the hull centroid is the shape Lab 43 ruled out.</para>
    ///
    /// <para><paramref name="mastAngleRad"/> is a SCREEN angle — 0 = +X (right), increasing CLOCKWISE on the
    /// canvas, because canvas Y points down while world Y points up. A caller holding a world heading crosses
    /// over once, at the one call that crosses, exactly as <see cref="VelocityArrow"/> requires.</para>
    /// </summary>
    public static (double X, double Y) Masthead(double mastAngleRad) =>
        (Math.Cos(mastAngleRad) * MastPx, Math.Sin(mastAngleRad) * MastPx);

    // ── How bright, and off what ──────────────────────────────────────────────────────────────────────

    /// <summary>How long a dump stays on screen, in real milliseconds. See the class note: the physical event
    /// is 2.2 ms, and this is the smallest stylisation a player can see happen.</summary>
    public const double FlashMs = 600.0;

    /// <summary>What a hull merely ARCING simmers at, against a full dump's 1.0. Low enough that the dump is
    /// unmistakably the event and the crawl is unmistakably a state, high enough to be read at a glance from
    /// across the map — which is the entire reason the crawl exists.</summary>
    public const double ArcingSimmer = 0.35;

    /// <summary>
    /// How bright a dump of this much charge is, 0…1 — and it is the SENSORS' number, not a new one.
    ///
    /// <para><see cref="HullCharge.SeenFartherFactor"/> is how much farther a hull at a given charge is heard;
    /// its excess over a cold hull, taken as a fraction of the most she can ever carry, is how much she just
    /// let go of. Dump nothing and there is nothing to draw. Dump everything and it is full brightness. The
    /// board, the sensors and this picture therefore cannot come to disagree, because there is only one
    /// number.</para>
    /// </summary>
    public static double DumpBrightness(double dumpedCharge)
    {
        double most = HullCharge.SeenFartherFactor(1.0) - 1.0;
        return most <= 0
            ? 0.0   // a world where charge costs nothing to carry is a world with nothing to show
            : Math.Clamp((HullCharge.SeenFartherFactor(dumpedCharge) - 1.0) / most, 0, 1);
    }

    // ── The shape ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many filaments rake off the whip at the snap of a dump.</summary>
    public const int FlashFilaments = 5;

    /// <summary>…and how many crawl there while she is merely arcing. Fewer, shorter, slower: a state, not an
    /// event.</summary>
    public const int ArcingFilaments = 3;

    /// <summary>The most filaments any state draws — the size a caller's buffer must be, in filaments.</summary>
    public const int MaxFilaments = FlashFilaments;

    /// <summary>Floats per filament in <see cref="Filaments"/>' output: three points, x,y each. One kink, so
    /// they read as a discharge rather than as spokes.</summary>
    public const int FloatsPerFilament = 6;

    /// <summary>What the plume looks like this instant, with nothing about WHERE she is in it.</summary>
    /// <param name="Intensity">0…1. Zero means draw nothing at all — not a dim plume, nothing.</param>
    /// <param name="Filaments">How many bolts leave the whip.</param>
    /// <param name="ReachPx">How far the longest of them may reach, in screen pixels off the masthead.</param>
    /// <param name="Flashing">True while a dump is the loudest thing happening; false for the arcing crawl.</param>
    public readonly record struct Plume(double Intensity, int Filaments, double ReachPx, bool Flashing)
    {
        /// <summary>Whether there is anything on screen at all. The one question the renderer asks first.</summary>
        public bool Draws => Intensity > 0 && Filaments > 0;
    }

    /// <summary>Nothing is happening: the quiet hull's plume, and what <see cref="Shape"/> returns for it.</summary>
    public static readonly Plume None = new(0.0, 0, 0.0, false);

    /// <summary>
    /// The plume for a hull that dumped <paramref name="dumpedCharge"/> this many milliseconds ago and is
    /// sitting in <paramref name="band"/> now.
    ///
    /// <para>Two contributions and the louder wins: the fading snap of the dump, and the steady simmer of an
    /// arcing hull. A captain who dumps while ARCING therefore sees the flash and then the crawl she is still
    /// in, which is the truth of the two states rather than an ordering somebody chose.</para>
    ///
    /// <para>Bands below <see cref="HullCharge.Band.Arcing"/> contribute NOTHING. She is charged, and a charged
    /// hull that is not arcing has nothing leaving it — the board is where you read that, and drawing a glow
    /// for it would make the map say a hull is discharging when it is not.</para>
    /// </summary>
    public static Plume Shape(double dumpedCharge, HullCharge.Band band, double sinceDumpMs)
    {
        double flash = 0.0;
        if (sinceDumpMs >= 0 && sinceDumpMs < FlashMs)
        {
            flash = DumpBrightness(dumpedCharge) * (1.0 - (sinceDumpMs / FlashMs));
        }

        double simmer = band == HullCharge.Band.Arcing ? ArcingSimmer : 0.0;
        bool flashing = flash > simmer;
        double intensity = Math.Max(flash, simmer);
        if (intensity <= 0)
        {
            return None;
        }

        return new Plume(
            intensity,
            flashing ? FlashFilaments : ArcingFilaments,
            MastPx * (flashing ? 1.6 : 0.7) * (0.55 + (intensity * 0.45)),
            flashing);
    }

    // ── The clocks ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How long one step of the arcing crawl lasts, in SIM seconds. Slow enough to read as a crawl
    /// rather than as static.</summary>
    public const double CrawlStepSeconds = 0.35;

    /// <summary>How long one step of a dump's flicker lasts, in real milliseconds — eight or nine steps across
    /// the whole <see cref="FlashMs"/> window, which is a snap and not an animation.</summary>
    public const double FlashStepMs = 70.0;

    /// <summary>How far the fan swings across one whole step, in radians. This is what makes the arcing state a
    /// CRAWL: the hash gives each step its own bolts, and the fraction of a step swings them while it
    /// lasts.</summary>
    public const double CrawlSweepRad = 0.30;

    /// <summary>The arcing crawl's phase, in steps, from SIM time. Paused, sim time is frozen and so is this —
    /// the same frame drawn twice is the same frame.</summary>
    public static double CrawlPhase(double simTimeSeconds) => simTimeSeconds / CrawlStepSeconds;

    /// <summary>A dump's phase, in steps, from the real milliseconds since it happened. The flash is the one
    /// thing here on the wall clock, because a 600 ms afterglow is a statement about the player's eye rather
    /// than about the world.</summary>
    public static double FlashPhase(double sinceDumpMs) => sinceDumpMs / FlashStepMs;

    // ── The filaments themselves ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Write the plume's filaments into <paramref name="into"/> as x,y,x,y,x,y per filament — three points
    /// each, in ship-local SCREEN pixels, and <b>every one of them starting at <see cref="Masthead"/></b>.
    /// Returns how many floats were written.
    ///
    /// <para>Pure. The same arguments give the same floats, always: the flicker is a hash of
    /// <paramref name="phase"/>'s whole part, and its fractional part swings the fan (the crawl). No
    /// <c>Random</c>, no clock read inside, nothing cached.</para>
    ///
    /// <para><paramref name="mastAngleRad"/> is the SCREEN angle of the whip (see <see cref="Masthead"/>); the
    /// bolts fan about it, because that is the direction the field points.</para>
    /// </summary>
    /// <param name="into">At least <c>plume.Filaments * <see cref="FloatsPerFilament"/></c> floats. Shorter and
    /// this throws rather than drawing half a bolt somebody would have to explain.</param>
    public static int Filaments(in Plume plume, double mastAngleRad, double phase, Span<double> into)
    {
        int need = plume.Filaments * FloatsPerFilament;
        if (into.Length < need)
        {
            throw new ArgumentException(
                $"a plume of {plume.Filaments} filaments needs {need} floats.", nameof(into));
        }

        if (!plume.Draws)
        {
            return 0;
        }

        (double mastX, double mastY) = Masthead(mastAngleRad);

        long step = (long)Math.Floor(phase);
        // The crawl: within one step the whole fan swings, so the bolts travel rather than blink. A dump does
        // not crawl — it snaps — so its fan is held still for the step it belongs to.
        double drift = plume.Flashing ? 0.0 : (phase - step) * CrawlSweepRad;

        int w = 0;
        for (int i = 0; i < plume.Filaments; i++)
        {
            uint h = (uint)HashCode.Combine(step, i);
            double spread = (((h & 0xFFFF) / 65535.0) - 0.5) * 1.9;              // radians off the whip's line
            double length = plume.ReachPx * (0.45 + ((((h >> 16) & 0xFF) / 255.0) * 0.55));
            double angle = mastAngleRad + spread + drift;

            double midX = mastX + (Math.Cos(angle) * length * 0.5);
            double midY = mastY + (Math.Sin(angle) * length * 0.5);
            double kink = angle + (((((h >> 24) & 0xFF) / 255.0) - 0.5) * 1.2);
            double tipX = midX + (Math.Cos(kink) * length * 0.5);
            double tipY = midY + (Math.Sin(kink) * length * 0.5);

            into[w] = mastX; into[w + 1] = mastY;
            into[w + 2] = midX; into[w + 3] = midY;
            into[w + 4] = tipX; into[w + 5] = tipY;
            w += FloatsPerFilament;
        }

        return w;
    }

    // ── What the pen does with it ─────────────────────────────────────────────────────────────────────

    /// <summary>The ink's alpha for a plume of this intensity, 0…255. Never fully transparent while it draws
    /// at all, so a filament in the buffer is a filament on the glass.</summary>
    public static byte Alpha(in Plume plume) =>
        (byte)Math.Clamp(60 + (plume.Intensity * 195), 0, 255);

    /// <summary>How wide the bolts are drawn, in pixels. A dump's are heavier than a crawl's.</summary>
    public static float StrokePx(in Plume plume) => plume.Flashing ? 1.6f : 1f;

    /// <summary>The bright core, and it sits ON the masthead because that is where the field is — small, and
    /// biggest at the snap of a dump.</summary>
    public static float CoreRadiusPx(in Plume plume) => plume.Flashing ? 3.4f : 1.8f;
}
