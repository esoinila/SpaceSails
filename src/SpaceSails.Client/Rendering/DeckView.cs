using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// Top-down deck plan view (M12, rebuilt on <see cref="DeckPlan"/> in M13): the tactical map
/// of your own ship. Windows draw teal, droids patrol, cargo racks mirror the hold, and the
/// shuttle sits in its cradle unless it's away boarding. Since #958 it is the ONLY walked view —
/// the raycast twin that shared this plan went with the owner's ruling.
/// </summary>
public sealed partial class DeckView
{
    public readonly record struct State(
        double AvatarX, double AvatarY, double HeadingRad,
        int CargoUnits, double Charge, bool ShuttleAway, bool ElectricUniverse,
        bool Docked = false,
        // #330 (owner: "we could have the sanity meter visible when we walk around"): the nerve gauge
        // rides EVERY walk mode — surface, haven/bar ashore, and aboard the ship (compact, a whisper) —
        // but never flight (the map view has its own instruments and never draws a DeckView). ShowNerve
        // gates it; NerveCompact draws the subtler aboard size that must clear the deck chrome.
        double Nerve = 0, string NerveReadout = "", bool ShowNerve = false, bool NerveCompact = false,
        // #453: blows taken this excursion — drives the condition pips under the nerve bar. -1 = hide them
        // (off-excursion, where skin is not being counted).
        int HitsTaken = -1,
        // #480 · WHY the gauge moved. NerveFlash is the in-the-moment line ("it laid hands on you  −1") that
        // hangs beside the pips for a beat; NerveLedger is the last few, newest first, so the captain can
        // read back what broke them. Owner: "what caused the sanity loss and what we did to regain it."
        string? NerveFlash = null,
        System.Collections.Generic.IReadOnlyList<string>? NerveLedger = null,
        // #708 · IS THIS FLOOR DARK. Handed down as the sim's own answer to the one Core ask
        // (UndergroundComplex.IsDark), never re-derived here — the #591 one-reach lesson: a renderer working
        // out for itself what the sim already knows is how two instruments come to disagree. When true the
        // world is drawn ONLY where the suit's headlights fall (SuitLamp), and the rest is black rather than
        // dim. Instruments are untouched: the motion fan, the on-grid smudges and the corner gauges are drawn
        // after the dark is laid down, because a thing you HEAR through a wall in an unlit hall is the entire
        // point of the feature.
        bool Dark = false,
        // #784 · IS THE CAPTAIN SITTING DOWN. Owner, live 2026-08-08: "Let's make the graphics say I am
        // sitting down at the avatar level — like different graphics etc." So it is a fact about the FIGURE
        // and not a caption: the mark below is drawn differently, and a glance at the deck says sitting with
        // no panel text involved. Handed down as the sim's own answer (the table panel IS the chair, #757),
        // never re-derived here — a renderer working out for itself what the sim already knows is how two
        // instruments come to disagree (#591).
        bool Seated = false,
        // #825 · THE REAL STALL, said out loud. Empty while the machine is keeping up. This is the MACHINE's
        // banner and not the mothership's: CommsLink's "SIGNAL BREAKING UP" (SurfaceHud.OrbitComms, painted
        // on the line above) is a scripted fiction about a downlink and has never had anything to do with
        // whether the game is handing out frames. Two different facts, two different sentences — and on the
        // one afternoon they were both on the glass, the only one the player could read was the wrong one.
        // Handed down whole from the sim's own clock (FrameGap over Map.Deck's SimStalenessSeconds), which
        // is the same clock the INPUT path is gated on: a HUD that says the world is live while a click is
        // being held would be this repo's sentence-vs-sim bug class aimed at the HUD itself.
        string? StallBanner = null);

    /// <summary>#313 · Everything the surface excursion overlays on the grid: the timed dig channel
    /// (shovel + bar), a panic-dropped chest, own caches' ✗ marks, and the crude motion-tracker fan
    /// (moving blips by bearing/range, cadence-pulsed). Null off-surface — the ship draws none of it.</summary>
    public readonly record struct SurfaceHud(
        double DigProgress,          // <0 = not channeling
        bool HasDroppedChest, double DropX, double DropY,
        // #830 · A return, and WHICH KIND it is. Blob = a living contact that has stopped travelling: the
        // unsure, smeared, nest-register return the owner asked for ("if the guard is still then it would be
        // a blurry blob"). The kind is decided in Core (MotionTracker.BlipKind) and only DRAWN here — the
        // alternative was faking a velocity for a standing man, which would have had the sim saying one
        // thing while the instrument drew another.
        System.Collections.Generic.IReadOnlyList<(double Bearing, double Range, bool Blob)> Blips,
        int Cadence,                 // MotionTracker.Cadence as int
        string Readout,
        System.Collections.Generic.IReadOnlyList<(double X, double Y, bool Haunted)> CacheMarks,
        double Nerve,                // #317: 0..100 (NerveModel.Max = steady). Drawn in the OPPOSITE corner
        string NerveReadout,         // to the motion tracker — the channel-law corner gauge, never on the grid
        // #314: deployed sentries (with their 99-counter readout + a firing zap line) and the husks of
        // downed Old Ones — ON-grid marks, not corner widgets. Optional so #313 callers still compile.
        // #488: whether this excursion wears the REGOLITH'S instruments — the motion-tracker fan and its
        // caption. False aboard a derelict, which is a ship rather than a moon: the away team reads her,
        // they do not sweep her. It is a flag rather than a null hud because the ON-GRID marks (deployed
        // sentries, husks, blood) belong on any deck, and suppressing the whole hud to lose the tracker
        // silently took the sentries with it.
        bool Instruments = true,
        // #488: contacts the motion fan HEARS but the captain cannot see — drawn on the grid as a soft
        // smudge, never a body. Owner: "the motion detector data could be shown on the map … but like fuzzy
        // area, not precise location." A blip is a bearing and a range off a crude fan; painting it as a dot
        // would be claiming a precision the instrument does not have, and would hand back the surprise the
        // line-of-sight rule just bought.
        System.Collections.Generic.IReadOnlyList<(double X, double Y, double Radius)>? Smudges = null,
        // #488: fading "movement was here" marks — where the fan last had a contact it no longer hears.
        // A memory of a PLACE: it stays put and dims, and it never follows anything.
        System.Collections.Generic.IReadOnlyList<(double X, double Y, double Fade)>? Ghosts = null,
        // #488: an on-grid countdown, drawn in the same seven-segment idiom as a sentry's magazine — the
        // owner's own comparison ("similar counter as the round count counting down seconds on the map").
        // A pulse message could not carry it: by the time the overload is running the message channel is
        // swamped with PA calls, and the one number that decides whether the captain lives was scrolling
        // past in the noise. Anchored to the thing that is about to fail, so it recedes behind you as you run.
        (double X, double Y, string Text)? Countdown = null,
        System.Collections.Generic.IReadOnlyList<(double X, double Y, string Counter, bool Dry, bool Firing, double AimX, double AimY)>? Bots = null,
        System.Collections.Generic.IReadOnlyList<(double X, double Y)>? Husks = null,
        // #324: the contextual surface keybar — the deploy/drop keys spelled out along the bottom while
        // they're live (a bot in the sling shows [T], a chest in hand shows [G]). #212 affordances-never-hide.
        string? KeyHints = null,
        // #327 the ship calls home: the mothership's in-voice orbit line, painted plainly across the top
        // (the #324 HUD-visibility law). Null when the excursion carries no orbit risk. Severity colours
        // it: 0 calm teal (steady), 1 amber (slipping), 2 red (failing / lost) — the maroon, never silent.
        string? OrbitComms = null,
        int OrbitSeverity = 0,
        // COMMS-LOSS (owner, cruise 2026-07-19): the mothership downlink phase colouring the orbit line —
        // 0 nominal (paint live), 1 degraded (greyed, faint static), 2 blackout (dim, flickering static,
        // the frozen last-known value). The OrbitComms string already carries the honest stale banner; this
        // just drives the visual static/grey so the readout LOOKS lost, not merely worded so.
        int CommsState = 0,
        // Lane-1 (owner, 2026-07-18: "advertise the dig and bot options in text under the motion
        // detector"): short contextual lines seated BENEATH the tracker readout in the left instrument
        // column — the dig-site and sentry affordances spelled out. Column chrome only, never over the
        // grid (the OverlayBands / dig-channel-watch law). Optional so earlier callers still compile.
        System.Collections.Generic.IReadOnlyList<string>? TrackerCaptions = null,
        // Beach-comber kit (owner, 2026-07-18: "some kind of grid system onto planet Miranda for marking
        // the checked squares on that visit"): the per-visit swept grid — each probed square at its centre,
        // Hard = the shovel rang off bedrock. Drawn as a subtle dug/checked glyph ON the regolith, under
        // the movers. Optional so earlier callers still compile.
        System.Collections.Generic.IReadOnlyList<(double X, double Y, bool Hard)>? SweptSquares = null,
        // #371 Phase 3 · EXPEDITION FOG OF WAR. DarkRegions = each forced chamber's axis-aligned bounds and
        // its visibility state (0 unseen — a hatched void, walls/consoles hidden; 1 explored — drawn dim;
        // 2 visible — drawn lit). Echoes = fading "movement was here" ripples a contact left when it slipped
        // behind cover. Both empty/absent off an expedition site (open terrain draws exactly as before).
        System.Collections.Generic.IReadOnlyList<(double X0, double Y0, double X1, double Y1, int State)>? DarkRegions = null,
        System.Collections.Generic.IReadOnlyList<(double X, double Y, double Alpha)>? Echoes = null,
        // #440 · THE STANDING PROMPT. One bright line above the keybar for the thing the whole excursion
        // hangs on right now — today, the chest in your hands and the key that puts it in the ground. The
        // keybar is deliberately dim chrome you stop reading; this is not chrome. It stays up until the
        // thing is done (owner, 2026-07-26: "It is the key to survival there"). Null when nothing is owed.
        string? StandingPrompt = null,
        // #453 · 1..0 fade on the blood spatter thrown when a blow got past the block. 0 = none.
        double BloodSplash = 0,
        // #573 · BEACONS on the motion fan: fixed PLACES worth walking to — the way home, and the shelter.
        // Owner: "could we show those as nearby beacons in the motion meter?... maybe some different colour
        // there something soothing :-D". Soothing is exactly right and it is not only taste: the fan has
        // meant ONE thing since it was built — something is moving and it wants you — so anything else
        // painted on it has to be unmistakably not that. Red things move; blue rings are places, and places
        // do not come to you.
        System.Collections.Generic.IReadOnlyList<(double Bearing, double Range, bool IsHome, bool IsLab)>? Beacons = null,
        // #573 · Your OWN caches, once they are inside the fan's reach. Owner: "we would like our own caches
        // onto the detector also.... since now finding them is a real task :-D (only if in range though)".
        // The range gate is the whole point — a map that always knows where your treasure is has taken the
        // task back off you.
        System.Collections.Generic.IReadOnlyList<(double Bearing, double Range)>? CacheBeacons = null,
        // #573 · A TIP, not a fix. Owner: "some kind of we were tipped about sites could be marked there
        // vaguely also to narrow down search... like the intel of the site gives a vague large blob."
        // Deliberately the same idiom as the fan's contact smudges — his own earlier ruling that uncertain
        // knowledge must be painted as an AREA, because drawing a dot would claim a precision the
        // information does not have and hand back the search it was only meant to narrow.
        System.Collections.Generic.IReadOnlyList<(double Bearing, double Range, double Spread)>? Rumours = null,
        // #564 · THE TANK. Seconds left and how far home is, so the gauge can be DRAWN rather than written
        // as prose. It shipped first as the top line of the caption list and the owner went looking for a
        // meter under the tracker and found nothing — because a footnote in 10px dim monospace, sitting in a
        // list of key hints, is not a meter. Negative = no tank (aboard, or off a surface entirely).
        double AirSeconds = -1,
        double AirDistanceHome = 0,
        // #562 · The glyph over the channel bar, so the one bar can say WHICH slow thing you are doing. It
        // was always a shovel, which was fine while digging was the only channel; the tube rearm is not a
        // shovel and reading one there would be the sim saying one thing while a picture says another.
        string ChannelGlyph = "⛏",
        // #562 · The tint of the channel bar's fill. The rearm is the ship helping you, not you exposing
        // yourself, so it reads cold-green rather than the dig's warning amber.
        bool ChannelIsAid = false,
        // #591 · HOW FAR THE FAN HEARS, handed down from the sim rather than re-derived here. The renderer
        // used to work its own reach out of the viewport while the sim used a flat half-width, so on any
        // window that was not exactly 64:28 the blip you SAW at the rim was not the blip the chirp had
        // HEARD. Underground the reach shortens with depth and that drift would have become load-bearing.
        // Non-positive = fall back to the viewport derivation (callers that predate this).
        double FanReach = -1,
        // #591 · Where the captain is, painted on the instrument itself — "B14 · ARCHIVE". Null on the
        // regolith. How deep you are is the single most important fact about your situation down there: it
        // is the number that decides whether you get back up on the air you have, and it was only ever
        // available as a label lying on the floor plan behind you.
        string? TrackerPlace = null,
        // #612 · WHERE THE AIR IS COMING FROM. Owner: "where here does it say if I consume tanks or have
        // air?" / "now we don't see if we need to worry about O2 from anywhere. That is really important info
        // for the suit hud to tell us." — it did not say, anywhere. The gauge showed a duration and a
        // distance and left the single most important bit, whether that duration is going DOWN, to be
        // inferred from where the captain thought they were standing. Harmless on a surface, where the answer
        // is always yes; not harmless once a lift can put you on a pressurised floor in sixty seconds.
        //
        // A WHOLE ANSWER rather than a bool, because there are three roofs and the tank does a different
        // thing under each. Handed down as the sim's OWN answer rather than re-derived here (the #591
        // one-reach lesson: the renderer working out for itself what the sim already knows is how two
        // instruments come to disagree — and there is nothing worse to disagree about than whether the
        // captain can breathe).
        SuitAir.Supply AirSupply = SuitAir.Supply.Tanks);

    // #708 · The pen, and the mask that can be slipped over it. `_renderer` is what every draw in this file
    // writes to; for the world phase of a DARK floor it is the LampMask, and for everything else — the
    // instruments, the gauges, the captain's own mark — it is the canvas itself. Swapped rather than checked
    // at eight hundred call sites, for the reason written on LampMask.
    private readonly IRenderer _canvas;
    private readonly LampMask _mask;
    private IRenderer _renderer;
    private readonly DeckPlan.Droid[] _droids = new DeckPlan.Droid[DeckPlan.MaxDroids];
    private readonly float[] _scratch = new float[32];

    // #841 / Lab 46 · THE STOPWATCH, AND IT IS NULL. Everything the draw-cost probe knows lives on this one
    // object, and it does not exist unless ?perf=1 asked for it: every mark site in the conductor is
    // `_perf?.Mark(…)`, so an unarmed build pays one null check per pass and never reads a clock. It hangs
    // HERE rather than on the Map component on purpose — see FramePerf's own note on #905's frame ledger.
    private FramePerf? _perf;

    /// <summary>#841 · The draw-cost probe, or null — which is what it is in every build nobody armed.
    /// The page reads it to paint the HUD line; nothing else in the client holds it.</summary>
    public FramePerf? Perf => _perf;

    /// <summary>#841 · Arm the draw-cost probe (<c>?perf=1</c>). <paramref name="say"/> is where the console
    /// block goes — <c>Console.WriteLine</c> in the browser, a collector in the guard. Idempotent: a second
    /// call keeps the window that is already filling.</summary>
    public FramePerf ArmThePerfProbe(Action<string> say) => _perf ??= new FramePerf(say);

    // #314 magazine-counter change-emphasis (owner, live playtest 2026-07-19: "make the round-count
    // numbers even bigger … I love to see those numbers move"). The DeckView draw is immediate-mode
    // and stateless, so a brief pop on decrement needs somewhere to remember each bot's last counter
    // and when it last changed. Keyed by the sentry's index in the per-frame Bots list (stable order —
    // a spent bot stays in place, dimmed). Pure rendering; never touches gameplay.
    private const float MagBasePx = 28f;    // the scoreboard digits — ~2× the old 15px label
    private const double MagFlash = 0.16;   // seconds a change stays lit + swollen
    private string[] _botCounters = System.Array.Empty<string>();
    private double[] _botCounterChanged = System.Array.Empty<double>();

    /// <summary>
    /// #729 · WHERE THE FLOOR IS ON THE GLASS — the deck view's own projection, named and handed out.
    ///
    /// <para>Click-to-walk has to answer the inverse question (this pixel is WHICH square metre of deck?),
    /// and the one way to get that wrong is to write the arithmetic down twice. This project has already
    /// paid for that class three times in one afternoon — unaudited client geometry literals, drawn one way
    /// and reasoned about another — so <see cref="Draw"/> reads its scale and origin from here, and so does
    /// the click. A pen and a pointer that disagree about where the wall is would send the captain walking
    /// at something they never pointed at.</para>
    /// </summary>
    public readonly record struct Placement(float Scale, float Ox, float Oy)
    {
        /// <summary>Canvas pixel → deck units. The exact inverse of the <c>P()</c> the renderer draws with:
        /// deck +Y is UP on screen, which is the sign that makes this worth having in one place.</summary>
        public (double X, double Y) ToDeck(double px, double py) =>
            ((px - Ox) / Scale, (Oy - py) / Scale);
    }

    /// <summary>The projection this plan is drawn under at this size, with this pan. A whole-plan tactical
    /// frame (bare ship / lone room) centres on the plan origin; a docked complex — or a moon field, or a
    /// hive floor — is far too long for the fixed frame, so it scrolls to keep the avatar centred
    /// (FollowCam). Manual pan still nudges either.</summary>
    public static Placement PlacementFor(
        DeckPlan plan, int widthPx, int heightPx, double avatarX, double avatarY, double panX, double panY)
    {
        ArgumentNullException.ThrowIfNull(plan);
        float scale = Math.Min(widthPx / 64f, heightPx / 28f);
        return new Placement(
            scale,
            plan.FollowCam ? widthPx / 2f - ((float)avatarX * scale) + (float)panX : (widthPx / 2f) + (float)panX,
            plan.FollowCam ? heightPx / 2f + ((float)avatarY * scale) + (float)panY : (heightPx / 2f) + (float)panY);
    }

    public DeckView(IRenderer renderer)
    {
        _canvas = renderer;
        _mask = new LampMask(renderer);
        _renderer = renderer;
    }

    /// <summary>
    /// #708 · THE ARM'S-REACH RING — the faint spill of light off your own suit, and the one thing a dark
    /// floor always shows you.
    ///
    /// <para>It is <see cref="DeckPlan.InteractRadius"/> and not a number of its own, because the law it
    /// exists to keep is #212's: <b>an affordance the game will let you use must never be invisible.</b>
    /// This is exactly the radius in which pressing [E] does something, so the ring and the reach are the
    /// same fact said twice, and a captain in a black hall who can work a console can always see it. Type a
    /// separate number here and the first time somebody tunes one of them the game starts offering
    /// interactions with nothing drawn under them.</para>
    /// </summary>
    private const double LampRingDu = DeckPlan.InteractRadius;

    private void FillRect(float x, float y, float w, float h, RgbaColor color)
    {
        Span<float> s = _scratch.AsSpan(0, 8);
        s[0] = x; s[1] = y; s[2] = x + w; s[3] = y; s[4] = x + w; s[5] = y + h; s[6] = x; s[7] = y + h;
        _renderer.DrawPolygon(s, color, color, 1f);
    }

    // #348: a room label with a subtle dark backing plate (raised contrast over the cabin art), and the
    // MED BAY exception — a whiter, cooler label on a cleaner plate ringed by a thin cyan-white keyline,
    // "the shiny clean room that stands out from the bunk rooms" (owner, 2026-07-18). The plate sits in
    // the float command buffer (flushed under all text), so it always backs the glyphs and never covers
    // them. Text draws on the alphabetic baseline at (cx, cy); the plate is sized to the monospace run
    // (~6px/char at 10px) and seated around that baseline.
    private void DrawRoomLabel(float cx, float cy, string text, bool medBay)
    {
        float w = text.Length * 6.0f + 9f;
        const float h = 13f;
        float x0 = cx - w / 2f, y0 = cy - 10f;
        FillRect(x0, y0, w, h, medBay ? MedBayPlate : RoomLabelPlate);
        if (medBay)
        {
            DrawRectOutline(x0, y0, w, h, MedBayKeyline); // the clean room's tidy edge — the exception's keyline
        }
        _renderer.DrawText(cx, cy, text, medBay ? MedBayText : RoomLabelText,
            medBay ? "bold 10px monospace" : "10px monospace", TextAlign.Center);
    }

    private void DrawRectOutline(float x, float y, float w, float h, RgbaColor color)
    {
        Span<float> s = _scratch.AsSpan(0, 10);
        s[0] = x; s[1] = y; s[2] = x + w; s[3] = y; s[4] = x + w; s[5] = y + h;
        s[6] = x; s[7] = y + h; s[8] = x; s[9] = y;
        _renderer.DrawPolyline(s, color, 1.5f);
    }

    private void DrawSeg((float X, float Y) a, (float X, float Y) b, RgbaColor color, float width)
    {
        Span<float> s = _scratch.AsSpan(0, 4);
        s[0] = a.X; s[1] = a.Y; s[2] = b.X; s[3] = b.Y;
        _renderer.DrawPolyline(s, color, width);
    }

    private void DrawBox(float cx, float cy, float half, RgbaColor color)
    {
        Span<float> s = _scratch.AsSpan(0, 10);
        s[0] = cx - half; s[1] = cy - half;
        s[2] = cx + half; s[3] = cy - half;
        s[4] = cx + half; s[5] = cy + half;
        s[6] = cx - half; s[7] = cy + half;
        s[8] = cx - half; s[9] = cy - half;
        _renderer.DrawPolyline(s, color, 1.5f);
    }

    private void DrawShuttle((float X, float Y) at, float scale, double simTime)
    {
        Span<float> s = _scratch.AsSpan(0, 10);
        float u = scale * 0.9f;
        (float x, float y) = at;
        s[0] = x + 2.2f * u; s[1] = y;
        s[2] = x - 1.4f * u; s[3] = y + 1.1f * u;
        s[4] = x - 0.8f * u; s[5] = y;
        s[6] = x - 1.4f * u; s[7] = y - 1.1f * u;
        s[8] = x + 2.2f * u; s[9] = y;
        _renderer.DrawPolyline(s, ShuttleColor, 2f);
        _renderer.DrawCircle(x + 0.8f * u, y, 0.35f * u, ShuttleColor, ShuttleColor);
        if (Math.Sin(simTime * 0.003) > 0.6)
        {
            var beacon = new RgbaColor(120, 255, 160, 200);
            _renderer.DrawCircle(x - 1.2f * u, y, 2.5f, beacon, beacon);
        }
    }
}
