using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// Top-down deck plan view (M12, rebuilt on <see cref="DeckPlan"/> in M13): the tactical map
/// of your own ship. Windows draw teal, droids patrol, cargo racks mirror the hold, and the
/// shuttle sits in its cradle unless it's away boarding. First-person is the immersive twin
/// (<see cref="FirstPersonView"/>); both render the same plan.
/// </summary>
public sealed class DeckView
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
        bool Dark = false);

    /// <summary>#313 · Everything the surface excursion overlays on the grid: the timed dig channel
    /// (shovel + bar), a panic-dropped chest, own caches' ✗ marks, and the crude motion-tracker fan
    /// (moving blips by bearing/range, cadence-pulsed). Null off-surface — the ship draws none of it.</summary>
    public readonly record struct SurfaceHud(
        double DigProgress,          // <0 = not channeling
        bool HasDroppedChest, double DropX, double DropY,
        System.Collections.Generic.IReadOnlyList<(double Bearing, double Range)> Blips,
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

    private static readonly RgbaColor Floor = new(10, 14, 22);
    private static readonly RgbaColor HullLine = new(170, 185, 205);
    private static readonly RgbaColor InnerLine = new(110, 125, 145, 200);

    // #563 · Solid ROCK, as opposed to a made pressure boundary. Same weight as hull because it is just as
    // solid, but warm and dusty rather than cold blue-white — the difference between a monolith and a
    // bulkhead, which is the difference between standing on a moon and standing in a ship.
    // #600 · Paint on poured concrete: worn, low-contrast, and deliberately dimmer than any label that
    // means something is interactable. Signage you read at a glance and then stop seeing.
    // #612 · Owner: "they are kind of hidden now". They were — a dim blue-grey on a dark floor, which is
    // fine for a wall marking and wrong for the one plate that answers WHERE AM I. Facility signage yellow
    // now, and bright enough to be read from across a corridor without hunting for it.
    //
    // ...and he then hit it AGAIN, which is the tell: the fault was never the hue. Ink over a corridor full
    // of hull lines, doors and console glow has little contrast left to raise, because the thing it is
    // competing with is BUSY rather than bright. Text on a busy deck needs a BACKGROUND — which is exactly
    // what #348 concluded for the room labels, and this plate is signage twice their size. So the yellow
    // stays and every painted sign gets its own dark panel (see the BigLabels draw). The way a stairwell
    // actually marks a level is a painted panel, not a brighter stencil.
    private static readonly RgbaColor StencilPaint = new(240, 208, 96, 245);

    // #612 · The dark panel every painted sign sits on, so the deck behind it stops competing.
    private static readonly RgbaColor StencilPlate = new(10, 14, 20, 225);

    /// <summary>You can breathe here — the relief colour, cool and calm. A floor that still holds pressure,
    /// or a #608 refuge cut into one that does not. The SAME green the gauge's own source chip wears,
    /// because a captain who has learned a colour on one instrument must not have to learn it again on the
    /// other.</summary>
    private static readonly RgbaColor StencilAir = new(130, 214, 176, 245);

    /// <summary>And you cannot. The same amber every other "this is costing you" reads in — including the
    /// chip on the suit gauge.</summary>
    private static readonly RgbaColor StencilDead = new(232, 150, 84, 245);

    private static readonly RgbaColor StoneLine = new(166, 150, 130);

    /// <summary>#677 · THE THIRD MATERIAL — the found halls' walls, and the only ink in the game that is not
    /// a fact about anybody.
    ///
    /// <para>Hull is cold blue-white because a bulkhead is metal somebody paid for; stone is warm and dusty
    /// because it is the moon you are standing on. This is neither: a flat, chroma-free grey that belongs to
    /// no palette, no department and no body, drawn heavier than either of them and with no texture,
    /// hatching or interior line-work of any kind. <b>The absence is the style</b> (§13.20), and it is the
    /// same thing #649's slab says by having nothing drawn inside its face.</para>
    ///
    /// <para>Deliberately NOT bright. Owner: <i>"a material the light does not grip"</i> — so it sits below
    /// the hull's value rather than above it, and on a floor where the suit's cone is the whole of the seeing
    /// (#708) that is most of what a captain ever learns about it.</para></summary>
    private static readonly RgbaColor SeamlessLine = new(150, 150, 150);
    private static readonly RgbaColor WindowLine = new(80, 220, 210, 220);
    private static readonly RgbaColor ConsoleGlow = new(120, 220, 200);
    private static readonly RgbaColor ConsoleNear = new(190, 255, 220);
    private static readonly RgbaColor AvatarColor = new(255, 210, 80);
    private static readonly RgbaColor CrateColor = new(200, 160, 90, 220);
    private static readonly RgbaColor ShuttleColor = new(150, 210, 255, 220);
    private static readonly RgbaColor DroidColor = new(150, 160, 180);
    private static readonly RgbaColor ReeverColor = new(230, 80, 70);   // #295: watchdog red

    // #583 · The repo crew. A cold institutional amber, deliberately NOT the Old Ones' red: what is walking
    // toward you matters, and two hostiles that read identically on the map is one hostile with two names.
    // Red is the thing that wants to eat you; amber is the thing that wants your money and has paperwork.
    private static readonly RgbaColor CollectorColor = new(226, 170, 60);

    /// <summary>#538 · A professional reads COLD — instrument white-blue, not the pack's red. Two hostile things
    /// on one deck have to be told apart at a glance, and the colour is the only thing doing that job while a
    /// captain is deciding which way to run.</summary>
    private static readonly RgbaColor SweeperColor = new(150, 205, 235);

    /// <summary>
    /// #537 · The ship's own structure — closed-cell metal foam and everything packed into it.
    ///
    /// <para><b>BLACK, and that is the point.</b> Owner: <i>"the hatched line should have the line and black bg
    /// under it … I don't want to draw attention to it :-D … so we can hide things more in it."</i> The first
    /// version filled the runs a shade lighter than the deck, and that inverted the original bug rather than
    /// fixing it: instead of a black gap you could see INTO, there was a bright bar announcing exactly where
    /// every hiding place on the ship was. A structure that draws the eye is as bad as one you can see through.
    /// So it is the deck's own black with only the hatch over it — present, structural, and utterly unremarkable
    /// until somebody knocks on it.</para>
    /// </summary>
    private static readonly RgbaColor FoamFill = new(8, 11, 15, 255);

    /// <summary>…and the section hatch over it, barely there. Any brighter and the wall becomes a texture a
    /// player studies, which is the opposite of what it is for — the owner's whole note about this was that it
    /// must not draw attention, because things are meant to hide in it.</summary>
    private static readonly RgbaColor FoamHatch = new(58, 66, 76, 105);
    private static readonly RgbaColor HuskColor = new(120, 70, 60, 150); // #314: a downed Old One's husk
    private static readonly RgbaColor BotColor = new(120, 210, 160);     // #314: a live sentry, gun-green
    private static readonly RgbaColor BotDim = new(90, 100, 110);        // #314: a dry sentry, gone quiet
    private static readonly RgbaColor SegLit = new(255, 90, 70);         // #314: the 99-counter, seven-segment red
    private static readonly RgbaColor SegDim = new(90, 50, 45, 200);     // #314: a frozen 00, dim glyph
    private static readonly RgbaColor SegWarn = new(255, 185, 70);       // #314: magazine under 25 — warming amber
    private static readonly RgbaColor SegAlarm = new(255, 45, 35);       // #314: magazine under 10 — hot alarm red
    private static readonly RgbaColor ZapColor = new(180, 255, 210, 235);// #314: the sentry's zap line
    private static readonly RgbaColor TextDim = new(140, 160, 180, 170);

    // #348 (owner, 2026-07-18: "make these room texts have better contrast … the Med Bay should stand out
    // from the cabins more … make it the shiny clean room that stands out from the bunk rooms. Like the
    // exception that makes the role.. it can look old and used but clean."). The room labels used to draw
    // in the dim grey TextDim, which the cabin art JPGs swallowed. Now every room label rides a subtle
    // dark backing plate (the house sentry-counter / SANITY-plate idiom) under a brighter fill, so the
    // schematic reads over the panels. MED BAY is the deliberate exception — the one clean room among the
    // grubby bunks: a whiter, cooler label on a cleaner plate with a thin cyan-white keyline.
    private static readonly RgbaColor RoomLabelText = new(214, 228, 242, 245);    // brighter than the old TextDim
    private static readonly RgbaColor RoomLabelPlate = new(8, 12, 18, 170);       // subtle dark backing, reads over art
    private static readonly RgbaColor MedBayText = new(240, 250, 255, 252);       // clean-room white, faint cool cast
    private static readonly RgbaColor MedBayPlate = new(16, 26, 32, 165);         // a cleaner, cooler plate than the bunks
    private static readonly RgbaColor MedBayKeyline = new(150, 222, 236, 155);    // the tidy edge — a thin cyan-white keyline
    // #371 Phase 3 · expedition fog-of-war palette. An UNSEEN forced chamber is a dark hatched void (unknown
    // ground behind a freshly-forced door); an EXPLORED one (seen, now out of sight) draws in a cold dim
    // slate; a VISIBLE one draws normally. Echoes ripple in the tracker's own green — "movement was here".
    // #708 · PITCH. Not the deck's near-black Floor (10,14,22) and not an alpha over it: a floor with no
    // fixtures on an airless world has nothing to scatter light, so what the lamp misses is not dark grey,
    // it is nothing. Opaque, so no console glow, no plate and no hull line can bleed through it.
    private static readonly RgbaColor Pitch = new(0, 0, 0, 255);

    private static readonly RgbaColor VoidFill = new(4, 7, 12, 214);
    private static readonly RgbaColor VoidHatch = new(34, 46, 62, 90);
    private static readonly RgbaColor VoidText = new(90, 110, 135, 150);
    private static readonly RgbaColor ExploredWall = new(74, 90, 112, 140);
    private static readonly RgbaColor ExploredText = new(120, 140, 162, 120);
    private static readonly RgbaColor EchoColor = new(120, 200, 150, 255);

    private static readonly RgbaColor DoorShut = new(255, 180, 90, 220);   // amber airlock door, closed
    private static readonly RgbaColor DoorOpen = new(255, 180, 90, 90);    // retracted leaves, faded
    private static readonly RgbaColor DoorLocked = new(120, 140, 170, 210);// another berth's sealed hatch
    private const double DoorOpenRadius = DeckPlan.DoorOpenRadius; // #465: one number, shared with sight

    // #708 · The pen, and the mask that can be slipped over it. `_renderer` is what every draw in this file
    // writes to; for the world phase of a DARK floor it is the LampMask, and for everything else — the
    // instruments, the gauges, the captain's own mark — it is the canvas itself. Swapped rather than checked
    // at eight hundred call sites, for the reason written on LampMask.
    private readonly IRenderer _canvas;
    private readonly LampMask _mask;
    private IRenderer _renderer;
    private readonly DeckPlan.Droid[] _droids = new DeckPlan.Droid[DeckPlan.MaxDroids];
    private readonly float[] _scratch = new float[32];

    // #314 magazine-counter change-emphasis (owner, live playtest 2026-07-19: "make the round-count
    // numbers even bigger … I love to see those numbers move"). The DeckView draw is immediate-mode
    // and stateless, so a brief pop on decrement needs somewhere to remember each bot's last counter
    // and when it last changed. Keyed by the sentry's index in the per-frame Bots list (stable order —
    // a spent bot stays in place, dimmed). Pure rendering; never touches gameplay.
    private const float MagBasePx = 28f;    // the scoreboard digits — ~2× the old 15px label
    private const double MagFlash = 0.16;   // seconds a change stays lit + swollen
    private string[] _botCounters = System.Array.Empty<string>();
    private double[] _botCounterChanged = System.Array.Empty<double>();

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

    // #424 HULL-SHUDDER · the unison pause. When a shudder fires on a populated interior deck (the ship,
    // a haven bar/hall) the client hands a FROZEN npc-hold time here for the held-breath beat: every present
    // NPC/patron is filled at that ONE shared timestamp — so their idle thermal jitter and patrol/pace all
    // stop together (the synchronized freeze IS the feature) — and their heads turn up as one. Null the rest
    // of the time, when the deck fills live at simTime. The deck-shake itself rides the render pan (panX/panY),
    // a pure transient offset that never moves an entity anchor.
    // #424 THE UNEXPLAINED SIGNAL · the crew glance. A companion ambient event: when a faint distant buzzer
    // sounds off-deck the STAFF (not the drinking patrons) briefly catch each other's eye — <paramref
    // name="crewGlance"/> turns every working crew member (barkeep, customs, the ship's own droids) to face
    // the nearest other crew member for the beat, a synchronized look. The patrons keep animating, oblivious.
    public void Draw(DeckPlan plan, int widthPx, int heightPx, double simTime, in State state,
        double panX = 0, double panY = 0, SurfaceHud? surface = null, double? npcHoldTime = null,
        bool crewGlance = false)
    {
        _renderer = _canvas;    // never inherit a mask from a frame that threw
        _renderer.BeginFrame(widthPx, heightPx, state.Dark ? Pitch : Floor);

        float scale = Math.Min(widthPx / 64f, heightPx / 28f);

        // A whole-plan tactical frame (bare ship / lone room) centres on the plan origin; a docked
        // complex is far too long for the fixed frame, so it scrolls to keep the avatar centred
        // (FollowCam). Manual pan still nudges either.
        float ox = plan.FollowCam ? widthPx / 2f - (float)state.AvatarX * scale + (float)panX : widthPx / 2f + (float)panX;
        float oy = plan.FollowCam ? heightPx / 2f + (float)state.AvatarY * scale + (float)panY : heightPx / 2f + (float)panY;
        (float X, float Y) P(double dx, double dy) => (ox + (float)dx * scale, oy - (float)dy * scale);

        // #708 · ON A DARK FLOOR THE PEN GOES BEHIND THE LAMP. Everything from here to the sentries is the
        // WORLD — ground, walls, doors, plates, fixtures, husks, bodies — and on a dark floor none of it
        // exists outside the headlights. The mask is disarmed again before the instruments, which are not
        // part of the world and never were.
        if (state.Dark)
        {
            _mask.Arm(state.AvatarX, state.AvatarY, state.HeadingRad, LampRingDu, scale, ox, oy);
            _renderer = _mask;
        }

        // #371 Phase 3 fog: the visibility state of a point against the forced-chamber overlay — -1 = not in
        // any chamber (draw as normal), 0 = unseen (hidden under the void), 1 = explored (dim), 2 = visible.
        var darkRegions = surface?.DarkRegions;
        int DarkState(double x, double y)
        {
            if (darkRegions is null)
            {
                return -1;
            }
            int best = -1;
            foreach ((double x0, double y0, double x1, double y1, int st) in darkRegions)
            {
                if (x >= x0 && x <= x1 && y >= y0 && y <= y1 && st > best)
                {
                    best = st; // a point in overlapping rects takes the most-revealed state
                }
            }
            return best;
        }

        // Ship-only dressing (cargo crates, shuttle cradle, reactor, cantina tables) is hardcoded to
        // the ship's geometry — a bare haven room has none of it, but a docked complex still contains
        // the ship. Everything else (backdrops, walls, doors, labels, consoles, droids, the avatar) is
        // plan-driven and general.
        bool isShip = plan.ShipFixtures;

        // Room backdrops sit UNDER every vector overlay (walls, consoles, avatar, labels stay on top
        // for legibility — the hybrid look). Each is top-left at (X, Y) deck-units, W×H deck-units.
        // Registration is idempotent, so calling it per frame is cheap.
        foreach (DeckPlan.Backdrop bd in plan.Backdrops)
        {
            (float bx, float by) = P(bd.X, bd.Y);
            _renderer.DrawImage(_renderer.RegisterImage(bd.Url), bx, by, bd.W * scale, bd.H * scale, bd.Alpha);
        }

        for (int gx = -22; gx <= 28; gx += 4)
        {
            DrawSeg(P(gx, -9.6), P(gx, 9.6), new RgbaColor(255, 255, 255, 10), 1f);
        }

        // #563 · THE FIELD FALLS INTO THE DARK. An UNSEEN wall stops the captain and draws nothing, which
        // fixed the owner's "square border … it seems artificial on a Moon" and immediately created the
        // other half of the problem: an invisible wall you walk into with no warning is worse than a fence,
        // not better. So the ground darkens over the last several deck units before any unseen bound.
        //
        // It is honest rather than decorative. An airless moon has no atmosphere to scatter light, so
        // regolith the lamp never reaches is simply black — the field does not END, it stops being visible,
        // and you read "there is nothing out that way" BEFORE you touch anything.
        //
        // THE FALLOFF DEPTH WOBBLES, and that is the whole point of doing it this way. Fading on the same
        // axis-aligned bounds would have drawn the identical rectangle in a softer pencil and left the
        // complaint untouched ("at least not obviously so with square area"). The wobble is keyed to world
        // position, not to time or camera, so the dark edge is a fact about the place and holds still while
        // you walk along it.
        //
        // Hung off the unseen walls themselves, so it appears exactly where a hidden bound is and nowhere
        // else — a ship's plan has none and is untouched.
        // #563 · TERRAIN, under the falloff so ground near the bound fades into the dark with everything
        // else. Owner: "put something more interesting in the landscape." These are drawn and never
        // collided — they live in their own array precisely so no oversight can give them substance.
        foreach (SpaceSails.Core.SurfaceScenery.Mark m in plan.Scenery)
        {
            (RgbaColor ink, float wide) = m.Of switch
            {
                SpaceSails.Core.SurfaceScenery.Kind.CraterRim => (new RgbaColor(74, 70, 64, 190), 1.4f),
                SpaceSails.Core.SurfaceScenery.Kind.Scree => (new RgbaColor(62, 58, 54, 150), 1f),
                SpaceSails.Core.SurfaceScenery.Kind.Ridge => (new RgbaColor(84, 78, 70, 200), 1.7f),
                _ => (new RgbaColor(58, 60, 66, 175), 1.3f),
            };
            DrawSeg(P(m.X1, m.Y1), P(m.X2, m.Y2), ink, wide);
        }

        DrawUnseenFalloff(plan, scale, ox, oy);

        // #371 Phase 3 fog: paint the still-UNSEEN forced chambers as dark hatched voids — unknown ground
        // behind a freshly-forced door — over the floor/grid, under everything that follows (the walls and
        // consoles inside are skipped, so nothing pokes through). Explored/visible chambers get no void.
        if (darkRegions is { Count: > 0 })
        {
            foreach ((double x0, double y0, double x1, double y1, int st) in darkRegions)
            {
                if (st != 0)
                {
                    continue;
                }
                (float vx0, float vy0) = P(x0, y1); // deck +y is up on screen → y1 is the top edge
                float vw = (float)(x1 - x0) * scale, vh = (float)(y1 - y0) * scale;
                FillRect(vx0, vy0, vw, vh, VoidFill);
                for (float vhy = vy0 + 6f; vhy < vy0 + vh; vhy += 7f) // crude hatch
                {
                    DrawSeg((vx0, vhy), (vx0 + vw, vhy), VoidHatch, 1f);
                }
                _renderer.DrawText(vx0 + vw / 2f, vy0 + vh / 2f, "· ? ·", VoidText, "10px monospace", TextAlign.Center);
            }
        }

        // ── #537 · STRUCTURE, FILLED. Owner, reading the deck after the wall padding shipped: "we should cover
        //    those narrow spaces … all of them … if we can see into them from the hall then they don't hide
        //    anything", then how it should look — "some kind of fill there would make it look like the space is
        //    filled with stuff" — and then what it IS: "I like to think it is structurally optimal metal foam
        //    and technology of the ship :-D  metal foam :-D"
        //
        //    He is right about the bug and right about the material. A run drawn as two lines round a black gap
        //    reads as a SPACE, and a hiding place drawn as a space is not hidden — a captain could read every
        //    void off the map without knocking on anything, which made the clue redundant and the sounder a
        //    formality. And metal foam is the honest answer to why the walls are thick at all: closed-cell
        //    metallic foam is stiff for its mass, which is exactly what you fill a whipple layer with. The
        //    thickness is engineering, not an excuse for a hiding place.
        //
        //    So it is drawn as CELLS rather than as hatching: a stochastic scatter that reads as foam packed
        //    with kit, and — the part that matters — reads identically along its whole length, so the one
        //    stretch of it that is hollow looks like all the rest until somebody sounds it.
        foreach (DeckPlan.Structure s in plan.Structures)
        {
            (float fx0, float fy0) = P(Math.Min(s.X0, s.X1), Math.Max(s.Y0, s.Y1));
            (float fx1, float fy1) = P(Math.Max(s.X0, s.X1), Math.Min(s.Y0, s.Y1));
            float fw = fx1 - fx0, fh = fy1 - fy0;
            if (fw <= 0 || fh <= 0)
            {
                continue;
            }

            FillRect(fx0, fy0, fw, fh, FoamFill);

            // SECTION HATCH — the drawing convention for CUT MATERIAL, which is exactly what this is. Owner:
            // "could we get like a cross-section dashed line instead of the current fill?" He is right and it is
            // the better answer for two reasons. A deck plan IS a section drawing, so 45° hatching is the mark an
            // engineer would already read as "you are looking at the inside of a wall" — no legend needed. And it
            // is uniform: a stochastic scatter has clumps and sparse patches, and a player hunting for hiding
            // places will read a sparse patch as a lead. Hatching has nothing to find in it, which is the whole
            // job — the one stretch that is hollow must look like every other stretch until somebody knocks.
            float step = 0.85f * scale;
            if (step < 3f)
            {
                continue;   // finer than this is a smear at this zoom, not a hatch
            }

            float dash = step * 0.55f, gap = step * 0.35f;

            // 45° in SCREEN space: y = x − c. Sweep c so the family covers the whole rectangle.
            for (float c = fx0 - fh; c <= fx1; c += step)
            {
                // Where that diagonal enters and leaves this rectangle.
                float tFrom = Math.Max(fx0, c + fy0);
                float tTo = Math.Min(fx1, c + fy1);

                for (float td = tFrom; td < tTo; td += dash + gap)
                {
                    float tEnd = Math.Min(td + dash, tTo);
                    DrawSeg((td, td - c), (tEnd, tEnd - c), FoamHatch, 1f);
                }
            }
        }

        foreach (DeckPlan.Wall w in plan.Walls)
        {
            // #563 · An UNSEEN wall is never drawn — the open field's envelope, which collides but has no
            // object in the world to be. It is checked before the fog so it stays invisible in every
            // lighting state, including the lit deck where the fog test passes everything.
            if (w.Unseen)
            {
                continue;
            }

            // #371 Phase 3 fog: a wall inside a still-unseen forced chamber is hidden (the room is unknown
            // until the captain looks in); one in an explored-but-out-of-sight chamber draws dim.
            int ws = DarkState((w.X1 + w.X2) / 2.0, (w.Y1 + w.Y2) / 2.0);
            if (ws == 0)
            {
                continue;
            }
            // #589 · A body's stone is drawn in a body's colour. Falls back to the old warm grey-brown
            // when a plan carries no ink (the ship, the stations, anything made of steel), so nothing that
            // is not a world changes at all.
            RgbaColor stone = plan.StoneInk is { } ink ? new RgbaColor(ink.R, ink.G, ink.B) : StoneLine;

            // #605 · A MADE structure can carry its own ink too. Owner, riding floors cut from the same
            // bones: "Let's like change the wall colors on different floors... now they look too same" —
            // answered with department livery rather than a per-floor gradient, so the colour is a language
            // and not decoration. Null everywhere it has always been null (the ship, the stations, the
            // wrecks are steel), so nothing outside the Hive changes by a pixel.
            RgbaColor hull = plan.HullInk is { } made ? new RgbaColor(made.R, made.G, made.B) : HullLine;

            // #677 · …AND A THIRD MATERIAL, WHICH TAKES NO INK FROM EITHER OF THEM. Both branches above read
            // a palette — the department that painted this corridor, the moon this rock came out of — and a
            // palette is an ANSWER. The found halls are drawn in one flat constant, ahead of both, because
            // the day a livery or a body colour reached them the walls would start saying whose they were.
            RgbaColor color = ws == 1 ? ExploredWall
                : w.IsWindow ? WindowLine
                : w.IsSeamless ? SeamlessLine
                : w.IsStone ? stone
                : w.IsHull ? hull
                : InnerLine;
            // Stone is drawn as heavy as hull: it is just as solid, and a monolith you could mistake for
            // rubble is a monolith that stops being the centrepiece of the moon it stands on. Seamless is
            // heavier than either, because it is the one surface in the game with no line-work inside it and
            // weight is all the drawing has left to say SOLID with.
            DrawSeg(P(w.X1, w.Y1), P(w.X2, w.Y2), color,
                w.IsSeamless ? 3.5f : w.IsHull || w.IsStone ? 2.5f : 1.5f);
        }

        // Automatic airlock doors (the docking tube): shut across the passage until you near them,
        // then they retract to a stub at each jamb. Purely visual — the passage is always walkable.
        foreach (DeckPlan.Door d in plan.Doors)
        {
            if (d.Locked)
            {
                // Another berth's sealed hatch — always shut, drawn cold (steel-blue), a real wall behind.
                //
                // #585 · Owner, in the Hive: "the doors should be different color than the walls and say
                // locked on approach." The cold steel-blue already differs from every wall ink in the game;
                // what it lacked was WEIGHT — at 3.5px against hull-bright walls it read as just another
                // line. A door that will never open is the most informative object in a facility, so it is
                // drawn heaviest of all, with a second inner stroke so it looks barred rather than merely
                // shut. (The "say locked on approach" half is the console at its midpoint, which names what
                // is behind it as you come near.)
                DrawSeg(P(d.X1, d.Y1), P(d.X2, d.Y2), DoorLocked, 5.5f);
                DrawSeg(P(d.X1, d.Y1), P(d.X2, d.Y2), new RgbaColor(20, 26, 38, 220), 2.0f);
                continue;
            }
            double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
            double toDoor = Math.Sqrt((state.AvatarX - mx) * (state.AvatarX - mx)
                                    + (state.AvatarY - my) * (state.AvatarY - my));
            // #462 · THE AIRLOCK INTERLOCK. Owner, 2026-07-27: "only one door in a tube is open at a time…
            // think of airlock" — "both doors being open at the same time defeats the purpose". Doors in the
            // same group take turns: only the one NEAREST the captain may stand open, so the far end is
            // always drawn shut. That is the visible barrier the Old Ones stop at (they used to halt at a gap
            // painted open, because the captain standing at the threshold held BOTH ends retracted), and it
            // is what seals a tailgater in the tube with the built-in gun (#461) instead of letting it
            // follow you aboard. The rule itself lives in Core Airlock so CI pins it.
            double nearestPartner = double.PositiveInfinity;
            if (d.Interlock != 0)
            {
                foreach (DeckPlan.Door other in plan.Doors)
                {
                    if (other.Interlock != d.Interlock || other.Locked || other.Equals(d))
                    {
                        continue;
                    }
                    double pmx = (other.X1 + other.X2) / 2.0, pmy = (other.Y1 + other.Y2) / 2.0;
                    double toOther = Math.Sqrt((state.AvatarX - pmx) * (state.AvatarX - pmx)
                                             + (state.AvatarY - pmy) * (state.AvatarY - pmy));
                    nearestPartner = Math.Min(nearestPartner, toOther);
                }
            }
            // #592 · A door is made of the hill it is set in — unless somebody paid to ship it here. The
            // ship and the stations keep the old amber (StoneInk is null there): they ARE steel, and nothing
            // about a bulkhead should start depending on which moon is outside.
            RgbaColor shut = DoorShut, leaf = DoorOpen;
            if (plan.DoorInk is { } local)
            {
                SpaceSails.Core.BodyPalette.Ink di = d.Imported
                    ? SpaceSails.Core.BodyPalette.Imported
                    : local;
                shut = new RgbaColor(di.R, di.G, di.B, 230);
                leaf = new RgbaColor(di.R, di.G, di.B, 95);
            }

            // #606 · A MACHINED DOOR IS A DIFFERENT OBJECT, not a different colour. Owner, hiding the lift
            // head in an ordinary hut: "The expensive doors would be the clue."
            //
            // Colour had already been asked to carry this and could not (#585) — violet means shelter, means
            // one ruin hatch in seven, and means the way down, so it identified nothing. Weight is a second
            // channel: a fat leaf with an inner rail and its frame picked out at the jambs, against the single
            // thin stroke every hatch on the moon is drawn with. That reads at a glance, from close, without a
            // word of copy — which is the whole technique (docs/art-manifest-hive.md).
            //
            // It still retracts. SEALED is what it looks like, not what it does: a door here that refused to
            // open would strand a captain in a lift head, and the reachability audits would be right to say so.
            float weight = d.Machined ? 6f : 3.5f;
            bool open = Airlock.MayOpen(toDoor, nearestPartner, DoorOpenRadius);
            if (open)
            {
                // Retracted: a short leaf at each jamb (25% in from each end).
                DrawSeg(P(d.X1, d.Y1), P(d.X1 + (d.X2 - d.X1) * 0.25f, d.Y1 + (d.Y2 - d.Y1) * 0.25f), leaf, weight - 1f);
                DrawSeg(P(d.X2, d.Y2), P(d.X2 - (d.X2 - d.X1) * 0.25f, d.Y2 - (d.Y2 - d.Y1) * 0.25f), leaf, weight - 1f);
            }
            else
            {
                DrawSeg(P(d.X1, d.Y1), P(d.X2, d.Y2), shut, weight);
                if (d.Machined)
                {
                    DrawSeg(P(d.X1, d.Y1), P(d.X2, d.Y2), new RgbaColor(18, 20, 30, 210), 2f);
                }
            }
            if (d.Machined)
            {
                // The frame: a short stub across the opening at each jamb, the way a plan draws a door that
                // was set into a hole somebody cut rather than built around.
                float jx = d.X2 - d.X1, jy = d.Y2 - d.Y1;
                float jl = MathF.Sqrt((jx * jx) + (jy * jy));
                if (jl > 0.01f)
                {
                    float nx = -jy / jl * 0.9f, ny = jx / jl * 0.9f;
                    DrawSeg(P(d.X1 - nx, d.Y1 - ny), P(d.X1 + nx, d.Y1 + ny), shut, 2.5f);
                    DrawSeg(P(d.X2 - nx, d.Y2 - ny), P(d.X2 + nx, d.Y2 + ny), shut, 2.5f);
                }
            }
        }

        // #348: each room label on its own dark backing plate for contrast over the art panels, with
        // MED BAY drawn as the clean-room exception (see the RoomLabel* colours above).
        // #600 · SIGNAGE, painted on the structure at the size a facility actually paints it. Owner, riding
        // between floors cut from the same bones: "something different in every floor so we visually spot
        // some difference when we go to different floors."
        //
        // Drawn before the room labels and in a dimmer ink than them ON PURPOSE: this is paint on a wall the
        // captain glances at, not a caption competing with the consoles. It is big enough to read without
        // looking for it and quiet enough to ignore while doing something else.
        //
        // #612 · ON A PLATE, not merely in a louder colour. The dim-paint idea above was right about the
        // FICTION and wrong about the screen: paint over a lit corridor is hard to read, and the owner hit
        // that twice ("they are kind of hidden now", then again after the ink was brightened). A dark panel
        // behind the letters is what makes signage legible in the real world too, and it is the same trick
        // #348 already uses one size down for the room labels — so the Hive's plate and the ship's cabin
        // labels are now the same instrument at two scales, which is one thing to learn instead of two.
        foreach ((float bx, float by, string text, float px, int tone) in plan.BigLabels)
        {
            if (DarkState(bx, by) == 0)
            {
                continue;
            }
            (float bxp, float byp) = P(bx, by);
            // #612 · Owner: "The meters and the floor name could be yellow here... they are kind of hidden
            // now.... it should say if the floor is pressurized also." Tone chooses the ink and nothing
            // else: tone 1 is the relief of somewhere you can breathe, tone 2 is the one that costs you, and
            // everything else is paint on a wall. A state gets the colour that state wears everywhere else
            // in the game — the same green and the same amber as the chip on the suit gauge.
            RgbaColor ink = tone switch
            {
                1 => StencilAir,
                2 => StencilDead,
                _ => StencilPaint,
            };

            // Monospace, so the width is arithmetic rather than a measurement the renderer cannot do — the
            // same 0.6-em-per-glyph estimate DrawRoomLabel has used since #348, with the baseline sitting
            // roughly three quarters down the panel (canvas draws text from its alphabetic baseline).
            float w = (text.Length * px * 0.62f) + (px * 0.9f);
            float h = px * 1.32f;
            float x0 = bxp - (w / 2f), y0 = byp - (h * 0.77f);
            FillRect(x0, y0, w, h, StencilPlate);
            DrawRectOutline(x0, y0, w, h, new RgbaColor(ink.R, ink.G, ink.B, 90));
            _renderer.DrawText(bxp, byp, text, ink, $"bold {px:0}px monospace", TextAlign.Center);
        }

        foreach ((float lx, float ly, string text) in plan.RoomLabels)
        {
            int ls = DarkState(lx, ly); // #371 Phase 3 fog: hide an unseen chamber's label, dim an explored one
            if (ls == 0)
            {
                continue;
            }
            (float lxp, float lyp) = P(lx, ly);
            if (ls == 1)
            {
                _renderer.DrawText(lxp, lyp, text, ExploredText, "10px monospace", TextAlign.Center);
            }
            else
            {
                DrawRoomLabel(lxp, lyp, text, medBay: text == "MED BAY");
            }
        }

        // #313 surface ground overlays: own caches' ✗ marks and a panic-dropped chest (drawn under the
        // avatar/droids so a mover can stand on them).
        if (surface is { } hud)
        {
            // Beach-comber kit: the per-visit swept grid, drawn FIRST so every other ground mark sits on
            // top. A checked square is a faint dug divot (a small ring + tick); a bedrock square rings off
            // with a dim ✕ — the sweep at a glance, in the deck-plan NetHack idiom (subtle, never loud).
            if (hud.SweptSquares is { } swept)
            {
                foreach ((double swx, double swy, bool hard) in swept)
                {
                    (float sx, float sy) = P(swx, swy);
                    if (hard)
                    {
                        _renderer.DrawText(sx, sy + 3, "✕", new RgbaColor(120, 110, 95, 150), "10px monospace", TextAlign.Center);
                    }
                    else
                    {
                        _renderer.DrawCircle(sx, sy, 0.35f * scale, null, new RgbaColor(110, 130, 120, 130), 1f);
                        _renderer.DrawText(sx, sy + 3, "·", new RgbaColor(120, 150, 135, 160), "10px monospace", TextAlign.Center);
                    }
                }
            }
            foreach ((double mx, double my, bool haunted) in hud.CacheMarks)
            {
                (float sx, float sy) = P(mx, my);
                var xcol = haunted ? new RgbaColor(230, 120, 90, 230) : new RgbaColor(230, 210, 120, 230);
                _renderer.DrawText(sx, sy + 4, "✗", xcol, "bold 16px monospace", TextAlign.Center);
                if (haunted)
                {
                    _renderer.DrawText(sx, sy - 12, "yours · something walks near it", new RgbaColor(230, 120, 90, 170), "8px monospace", TextAlign.Center);
                }
            }
            if (hud.HasDroppedChest)
            {
                (float dx2, float dy2) = P(hud.DropX, hud.DropY);
                _renderer.DrawText(dx2, dy2 + 5, "🧰", new RgbaColor(200, 160, 90, 240), "15px monospace", TextAlign.Center);
                _renderer.DrawText(dx2, dy2 - 11, "dropped chest", new RgbaColor(200, 160, 90, 180), "8px monospace", TextAlign.Center);
            }
            // #314: husks of downed Old Ones — dim marks left where they fell (the forensic seed, #316).
            if (hud.Husks is { } husks)
            {
                foreach ((double hkx, double hky) in husks)
                {
                    (float sx, float sy) = P(hkx, hky);
                    _renderer.DrawCircle(sx, sy, 0.55f * scale, HuskColor, HuskColor);
                    _renderer.DrawText(sx, sy + 3, "×", new RgbaColor(90, 60, 60, 220), "bold 11px monospace", TextAlign.Center);
                }
            }
            // #371 Phase 3: movement echoes — where a contact was last seen before it slipped behind cover.
            // A dim tracker-green ripple that fades over its life; "here was movement before" (owner's ask),
            // making the motion tracker's through-wall blips all the more exciting to chase.
            if (hud.Echoes is { } echoes)
            {
                foreach ((double ex2, double ey2, double alpha) in echoes)
                {
                    (float sx, float sy) = P(ex2, ey2);
                    byte a = (byte)Math.Clamp(alpha * 180.0, 0, 180);
                    var ring = new RgbaColor(EchoColor.R, EchoColor.G, EchoColor.B, a);
                    _renderer.DrawCircle(sx, sy, (0.35f + 0.5f * (float)alpha) * scale, null, ring, 1.2f);
                    _renderer.DrawText(sx, sy + 3, "·", ring, "10px monospace", TextAlign.Center);
                }
            }
        }

        if (isShip)
        {
            // Cargo crates: one per unit aboard (in the top-port hold now — #295).
            for (int i = 0; i < Math.Min(state.CargoUnits, 12); i++)
            {
                (float cx, float cy) = P(-10 + (i % 4) * 1.9, 5 + (i / 4) * 1.6);
                DrawBox(cx, cy, 0.65f * scale, CrateColor);
            }

            // Shuttle in its cradle (bottom-port bay now — #295) — or away doing piracy.
            if (!state.ShuttleAway)
            {
                DrawShuttle(P(-6.5, -6.5), scale, simTime);
            }
            else
            {
                (float bx, float by) = P(-6.5, -6.5);
                _renderer.DrawText(bx, by, "— AWAY —", new RgbaColor(255, 170, 80, 200), "bold 11px monospace", TextAlign.Center);
                if (Math.Sin(simTime * 0.005) > 0)
                {
                    DrawSeg(P(-9, -9.9), P(-5, -9.9), new RgbaColor(255, 120, 80, 220), 3f);
                }
            }

            // Reactor + charge conduit (engine room).
            (float rx, float ry) = P(-19, 2.5);
            _renderer.DrawCircle(rx, ry, 1.6f * scale, null, InnerLine, 2f);
            double throb = 0.5 + 0.5 * Math.Sin(simTime * 0.002);
            var reactor = new RgbaColor(120, 200, 255, (byte)(90 + 70 * throb));
            _renderer.DrawCircle(rx, ry, 0.9f * scale, reactor, reactor);
            if (state.ElectricUniverse)
            {
                var conduit = new RgbaColor(255, 240, 120, (byte)(40 + 180 * state.Charge));
                DrawSeg(P(-19, 1), P(-20, -4), conduit, 3f);
            }

        }

        // Round tables (plan-driven: the ship's cantina, a haven bar) — a ring on the floor.
        foreach ((float tx, float ty) in plan.Tables)
        {
            (float cx2, float cy2) = P(tx, ty);
            _renderer.DrawCircle(cx2, cy2, 0.9f * scale, null, InnerLine, 1.5f);
        }

        // Droid pirate infantry (the ship's; a haven has none — DroidCount 0).
        // #424 HULL-SHUDDER: during the unison pause the NPCs are filled at the FROZEN onset time (all their
        // simTime-driven idle jitter / patrol / pace stop together — the synchronized held breath), and their
        // heads turn up as one (facing snapped screen-up). A Reever is never a patron, so it keeps its facing.
        bool headsUp = npcHoldTime.HasValue;
        plan.FillDroids(npcHoldTime ?? simTime, _droids);
        // #424 THE UNEXPLAINED SIGNAL: pre-compute each working crew member's glance — the facing toward the
        // NEAREST other crew member — so the barkeep and the dock-hand catch each other's eye as one. Only
        // built when a signal is glancing; a Reever or a drinking patron is never crew (StaffFacing skips them).
        double?[]? glance = crewGlance ? BuildCrewGlance(plan.DroidCount) : null;
        for (int di = 0; di < plan.DroidCount; di++)
        {
            DeckPlan.Droid droid = _droids[di];
            (float dx, float dy) = P(droid.X, droid.Y);
            // #295: the Reevers read hostile — a red mark, not the crew's grey.
            bool reever = droid.Name == "Reever";
            bool collector = droid.Name == "Collector";   // #583: a repo crew on foot, amber not red
            // #538: the sweep team, by callsign. They collide and are seen on the captain's own radius, so
            // they are drawn on it too — the #473 lesson about daylight showing between a body and its
            // picture.
            //
            // #633 · THREE KINDS OF FIGURE ON ONE DECK, and each branch only knew two. The pack is red, the
            // repo crew amber, a professional cold blue: what is walking toward you matters, and two hostiles
            // that read identically on the map are one hostile with two names.
            bool sweeper = IsSweeper(droid.Name);
            RgbaColor mark = reever ? ReeverColor
                : collector ? CollectorColor
                : sweeper ? SweeperColor
                : DroidColor;
            // #473 · AN OLD ONE'S PICTURE IS ITS BODY. The captain is drawn at exactly DeckPlan.AvatarRadius
            // (below), but the Old Ones — who collide, catch, block and get shoved apart on that SAME radius —
            // were drawn a tenth of a deck unit smaller. Every law that reads their body therefore fired with
            // daylight still showing between the dots: a catch at CatchRadius = 1.4 left a 0.2du gap on
            // screen, a pack held at PersonalSpace looked loose rather than shoulder to shoulder, and each one
            // parked against a wall floated just off it. Owner: "check all reever collisions… the radius must
            // be used in every single one" — the drawing is one of them. Crew stay at 0.5: nothing collides
            // with a barkeep, so their mark is free to be a mark.
            // #583: a collector has a body that catches on the same radius as everyone else's, so it is
            // drawn at that radius for the same reason an Old One is — the picture IS the law. Same for a
            // sweeper (#538), and for the same reason.
            float bodyRadius = reever || collector || sweeper ? (float)DeckPlan.AvatarRadius : 0.5f;
            _renderer.DrawCircle(dx, dy, bodyRadius * scale, mark, mark);
            // Heads up as one (hull-shudder pause), or the crew catch each other's eye (unexplained signal),
            // else the droid's own facing. The shudder pause wins if both somehow overlap.
            double facing = headsUp && !reever && !collector && !sweeper ? Math.PI / 2
                : glance?[di] ?? droid.FacingRad;
            float fx = dx + (float)Math.Cos(facing) * scale * 0.8f;
            float fy = dy - (float)Math.Sin(facing) * scale * 0.8f;
            DrawSeg((dx, dy), (fx, fy), mark, 1.5f);

            // #538 · THE LAMP, DRAWN AT EXACTLY THE ANGLE THE RULE CHECKS. InspectionTeam.LampConeHalfAngleDegrees
            // and LampRange are read straight from Core here rather than eyeballed, because a cone drawn wider than
            // it is tested is a lie the player learns the expensive way — and this cone IS the counter-play, so it
            // has to be trustworthy enough to stand three metres to the side of.
            if (sweeper)
            {
                double half = SpaceSails.Core.InspectionTeam.LampConeHalfAngleDegrees * Math.PI / 180.0;
                double range = SpaceSails.Core.InspectionTeam.LampRange;
                RgbaColor lamp = SweeperColor with { A = 44 };
                for (int e = -1; e <= 1; e += 2)
                {
                    double edge = facing + (e * half);
                    // AND STOPPED AT THE FIRST BULKHEAD, because the RULE stops there. First pass drew both
                    // edges to full reach through steel — cone tested right, cone drawn wrong, which is the
                    // same lie as drawing it too wide and just as expensive to learn from: a captain would
                    // have read light spilling into a compartment nobody could actually see into.
                    double lit = plan.CastRay(droid.X, droid.Y, Math.Cos(edge), Math.Sin(edge),
                                              out double hit, out _, out _, out _)
                        ? Math.Min(range, hit)
                        : range;
                    float reach = (float)lit * scale;
                    DrawSeg((dx, dy),
                            (dx + (float)Math.Cos(edge) * reach, dy - (float)Math.Sin(edge) * reach),
                            lamp, 1f);
                }
            }

            _renderer.DrawText(dx, dy - 0.9f * scale, droid.Name,
                reever ? ReeverColor
                    : collector ? CollectorColor
                    : sweeper ? SweeperColor
                    : TextDim,
                "8px monospace", TextAlign.Center);
        }

        // ── #708 · AND HERE THE DARK IS LAID DOWN. The world is drawn; the mask comes off; the black goes on
        //    over everything the headlights do not reach, with a hard edge where the cone stops.
        //
        //    Everything BELOW this line is drawn over the dark on purpose, and each for its own reason:
        //    a deployed sentry (it carries a lamp — you can see a light in a dark hall even if you cannot
        //    see what it lights), the motion fan's smudges and ghosts (an instrument, #591, whose whole
        //    worth is hearing what you cannot see), the overload countdown (a lit display), the blood and
        //    the screen-flash (they happen to YOU), the captain's own mark, and the corner gauges.
        if (state.Dark)
        {
            _renderer = _canvas;
            PaintTheDark(widthPx, heightPx, in state, scale, ox, oy);
        }

        // #314: deployed sentries — a gun-green mark (dim once dry), a zap line to the Old One it's
        // dropping, and its crude two-digit magazine readout riding above (seven-segment red, dim at 00).
        // Drawn ON the grid, not a corner widget — the counter is meant to be read from across the map.
        if (surface is { Bots: { } sentries })
        {
            // Keep the per-bot change-tracking arrays as long as the deployed list (grows only).
            if (_botCounters.Length < sentries.Count)
            {
                System.Array.Resize(ref _botCounters, sentries.Count);
                System.Array.Resize(ref _botCounterChanged, sentries.Count);
            }
            for (int i = 0; i < sentries.Count; i++)
            {
                (double bxr, double byr, string counter, bool dry, bool firing, double aimX, double aimY) = sentries[i];
                (float sx, float sy) = P(bxr, byr);
                if (firing && !dry)
                {
                    (float zx, float zy) = P(aimX, aimY);
                    DrawSeg((sx, sy), (zx, zy), ZapColor, 1.6f);
                    _renderer.DrawCircle(zx, zy, 3f, ZapColor, ZapColor);
                }
                RgbaColor body = dry ? BotDim : BotColor;
                DrawBox(sx, sy, 0.55f * scale, body);
                _renderer.DrawCircle(sx, sy, 0.3f * scale, body, body);

                // The number changed this frame? Stamp the moment so the pop below can key off it. (First
                // sight of a bot counts as a change — a one-off blip as it deploys, which reads as intent.)
                if (_botCounters[i] != counter)
                {
                    _botCounters[i] = counter;
                    _botCounterChanged[i] = simTime;
                }
                double since = simTime - _botCounterChanged[i];
                float pop = since >= 0 && since < MagFlash ? (float)(1.0 - since / MagFlash) : 0f;

                // #314 low-ammo warning (owner, 2026-07-19): the magazine's house red is the identity down
                // the top of the belt; it warms to amber under 25 and snaps to a hot alarm red under 10 —
                // the small honest touch the counter never had. Non-numeric readouts keep the house red.
                RgbaColor digit = dry ? SegDim : SegLit;
                if (!dry && int.TryParse(counter, out int rounds))
                {
                    if (rounds < 10) digit = SegAlarm;
                    else if (rounds < 25) digit = SegWarn;
                }
                // On a decrement the digits flash brighter and swell for a frame or two — the owner loves
                // to watch them move, so the change gets a subtle brighten-toward-white + size pop.
                if (!dry && pop > 0f) digit = LerpToWhite(digit, 0.7f * pop);
                float fontPx = MagBasePx * (1f + 0.16f * pop);

                // The readout: a dark scoreboard panel with the two big digits, anchored above the bot so
                // it never covers the mark or its neighbours. Plate stays a steady size; only the number pops.
                float pw = 3.0f * scale, ph = 2.0f * scale;
                float plateBottom = sy - 0.8f * scale;      // clears the bot box (half 0.55·scale) with a gap
                float plateTop = plateBottom - ph;
                FillRect(sx - pw / 2, plateTop, pw, ph, new RgbaColor(16, 10, 10, 225));
                float baseY = (plateTop + plateBottom) / 2f + fontPx * 0.35f; // optical centre for the fixed-px glyphs
                _renderer.DrawText(sx, baseY, counter, digit,
                    $"bold {fontPx:0.#}px monospace", TextAlign.Center);
            }
        }

        // #488 · WHAT THE FAN HEARS THROUGH STEEL. A soft, edgeless bloom over roughly where the return
        // came from — big enough that it names a REGION and not a spot. Drawn under everything else so a
        // contact you can actually see is always the sharper mark on the deck.
        if (surface is { Smudges: { } heard })
        {
            foreach ((double smx, double smy, double smr) in heard)
            {
                (float ssx, float ssy) = P(smx, smy);
                float rPx = (float)(smr * scale);
                // Three widening rings, each fainter: no hard edge anywhere, so the eye reads "somewhere
                // about here" rather than a position.
                // Owner: "let's show them much better on motion detector still." The first pass was so
                // faint it read as a rendering artefact; a return you have to hunt for is not a warning.
                // Loud enough to catch the eye, still edgeless enough that it can never be mistaken for a
                // position — and it BREATHES, so a live return is obviously live.
                float pulse = 0.82f + 0.18f * (float)Math.Sin(simTime * 0.004);
                for (int ring = 4; ring >= 1; ring--)
                {
                    float f = ring / 4f;
                    byte alpha = (byte)Math.Clamp(96 * (1.05f - f) * pulse, 0f, 255f);
                    _renderer.DrawCircle(ssx, ssy, rPx * f * pulse, new RgbaColor(226, 96, 84, alpha), default);
                }
            }
        }

        // #488 · GHOSTS: where the fan last had something. Dimmer and colder than a live return, and drawn
        // with a broken ring so it never reads as a contact — this is a memory, not a target.
        if (surface is { Ghosts: { } ghosts })
        {
            foreach ((double gx, double gy, double fade) in ghosts)
            {
                (float gsx, float gsy) = P(gx, gy);
                byte a = (byte)Math.Clamp(70 * fade, 0f, 255f);
                float gr = (float)(2.4 * scale);
                _renderer.DrawCircle(gsx, gsy, gr, new RgbaColor(150, 120, 160, (byte)(a / 3)), default);
                // Four short arcs of a ring, so the eye reads "was here" rather than "is here".
                for (int seg = 0; seg < 4; seg++)
                {
                    double a0 = (seg * Math.PI / 2) + 0.35;
                    DrawSeg(
                        (gsx + (float)(Math.Cos(a0) * gr), gsy + (float)(Math.Sin(a0) * gr)),
                        (gsx + (float)(Math.Cos(a0 + 0.75) * gr), gsy + (float)(Math.Sin(a0 + 0.75) * gr)),
                        new RgbaColor(170, 140, 180, a), 1.1f);
                }
            }
        }

        // #488 · THE OVERLOAD, ON THE GRID. Same scoreboard as a magazine, bigger and always alarm-red,
        // anchored to the thing that is about to fail — so it recedes behind the captain as they run, and
        // the one number that decides whether they live is never in the message channel with the PA calls.
        if (surface is { Countdown: { } burn })
        {
            (float bx, float by) = P(burn.X, burn.Y);
            float pw = 5.4f * scale, ph = 3.2f * scale;
            float top = by - 2.4f * scale;

            FillRect(bx - pw / 2, top, pw, ph, new RgbaColor(20, 6, 6, 235));
            // A hard border so it reads as a fitted instrument rather than a floating label.
            DrawSeg((bx - pw / 2, top), (bx + pw / 2, top), SegAlarm, 1.2f);
            DrawSeg((bx - pw / 2, top + ph), (bx + pw / 2, top + ph), SegAlarm, 1.2f);

            float px = MagBasePx * 1.5f;
            _renderer.DrawText(bx, top + ph / 2 + px * 0.35f, burn.Text, SegAlarm,
                $"bold {px:0.#}px monospace", TextAlign.Center);
        }

        // Consoles.
        //
        // ONE PROMPT, AND IT IS THE TRUE ONE. Owner, twice, on two different decks: "there two e's are too
        // close to each others now" and then "see the two crowded consoles at the back of our ship". Both
        // times I moved a console — and both times the real fault was here: this drew an [E] over EVERY
        // console inside the interact radius, while the key itself only ever answers the NEAREST one
        // (InteractAtConsole → NearestConsoleSpot). So a captain standing between two fittings saw two
        // offers, and one of them was a lie.
        //
        // Geometry could never fix that. A bridge is dense on purpose — helm, nav post, scope and three
        // desks inside a few du — so "keep every pair 6 du apart" is not a ship anyone would want to walk.
        // Asking the same function the key asks is the fix, it is one line, and it is right on every deck in
        // the game at once: her own, a derelict's, a station's, the regolith.
        //
        // #708 · AND THE LAMP GOES BACK ON THE PEN FOR THEM. Consoles are drawn late — after the fan's
        // smudges, so a contact heard through a wall is not painted over by a plate — which puts them on the
        // wrong side of the blackout. They are WORLD, though: a fitting bolted to a wall in an unlit hall is
        // not visible because it is important. So the world is drawn in two passes and both of them are
        // behind the headlights, rather than moving the blackout and quietly hiding the instrument.
        if (state.Dark)
        {
            _renderer = _mask;
        }

        DeckPlan.ConsoleSpot? answering = plan.NearestConsoleSpot(state.AvatarX, state.AvatarY);

        foreach (DeckPlan.ConsoleSpot console in plan.Consoles)
        {
            // #371 Phase 3 fog: a console inside an unseen chamber is unknown (hidden); an explored one is
            // dimmed. A still-sealed door's console sits OUTSIDE any chamber rect, so it always shows.
            if (DarkState(console.X, console.Y) == 0)
            {
                continue;
            }
            (float sx, float sy) = P(console.X, console.Y);

            // Lit only when [E] would actually reach THIS console. The radius check is still the gate —
            // NearestConsoleSpot applies it — so nothing lights up across the ship; what changed is that a
            // second console in range no longer claims a key it will not get.
            bool near = answering == console;
            RgbaColor c = near ? ConsoleNear : ConsoleGlow;
            _renderer.DrawCircle(sx, sy, near ? 5f : 3.5f, c, c);
            _renderer.DrawText(sx, sy - 10, console.Label, near ? ConsoleNear : TextDim,
                near ? "bold 10px monospace" : "9px monospace", TextAlign.Center);
            if (near)
            {
                _renderer.DrawText(sx, sy + 20, "[E]", ConsoleNear, "bold 11px monospace", TextAlign.Center);
            }
        }

        _renderer = _canvas;    // #708 · and off again — everything below is the captain, or an instrument

        // The captain.
        (float ax, float ay) = P(state.AvatarX, state.AvatarY);

        // #453 · BLOOD, when a blow gets past the block (owner: "Maybe a splash of blood when reever hit
        // goes through players attempt to block it. :-D"). Seeded spatter around the captain, thrown on the
        // regolith UNDER them so it reads as coming off the body. Brief — it is punctuation, not a decal.
        // #467 · THE SCREEN REACTS. Owner: "I had no sound to alert that I was taking damage… I should know
        // when I'm hurt." A small spatter under the boots was too easy to miss mid-fight, so a blow also
        // washes the EDGES of the screen red on the same fade. Peripheral, never over the grid — the deck
        // stays readable while you decide whether to run.
        if (surface is { BloodSplash: > 0 } hurt)
        {
            double f = Math.Clamp(hurt.BloodSplash, 0, 1);
            byte a = (byte)Math.Clamp(150 * f, 0, 255);
            var edge = new RgbaColor(150, 12, 12, a);
            float band = Math.Max(10f, heightPx * 0.055f);
            FillRect(0, 0, widthPx, band, edge);
            FillRect(0, heightPx - band, widthPx, band, edge);
            FillRect(0, 0, band, heightPx, edge);
            FillRect(widthPx - band, 0, band, heightPx, edge);
        }

        if (surface is { BloodSplash: > 0 })
        {
            double fade = Math.Clamp(surface.Value.BloodSplash, 0, 1);
            for (int i = 0; i < 9; i++)
            {
                // A fixed fan, so the spatter is stable for the moment it is up rather than crawling.
                double a = i * 2.399963229728653;             // the golden angle again
                double reach = scale * (0.5 + (0.16 * (i % 4)));
                float bx = ax + (float)(Math.Cos(a) * reach);
                float by = ay + (float)(Math.Sin(a) * reach);
                var blood = new RgbaColor(190, 30, 30, (byte)Math.Clamp(235 * fade, 0, 255));
                _renderer.DrawCircle(bx, by, Math.Max(1.5f, 0.16f * scale), blood, blood);
            }
        }

        // #473: the captain's mark already happened to equal AvatarRadius — say so, so the two can never
        // drift apart again the way the Old Ones' mark had.
        _renderer.DrawCircle(ax, ay, (float)DeckPlan.AvatarRadius * scale, AvatarColor, AvatarColor);
        float hx = ax + (float)Math.Cos(state.HeadingRad) * scale * 1.1f;
        float hy = ay - (float)Math.Sin(state.HeadingRad) * scale * 1.1f;
        DrawSeg((ax, ay), (hx, hy), AvatarColor, 2f);

        // #313 the dig channel: a shovel glyph over the captain and a crude progress bar — the
        // vulnerability window, drawn ON the grid so the player watches the tracker while it fills.
        if (surface is { DigProgress: >= 0 } dig)
        {
            // #562: the glyph and the tint say WHICH slow thing this is. A shovel over a magazine being
            // racked would be the same class of lie this project keeps paying for.
            RgbaColor glyphInk = dig.ChannelIsAid
                ? new RgbaColor(150, 235, 200, 245)
                : new RgbaColor(255, 230, 140, 240);
            RgbaColor fillInk = dig.ChannelIsAid
                ? new RgbaColor(120, 215, 175, 240)
                : new RgbaColor(255, 200, 90, 240);
            _renderer.DrawText(ax, ay - 1.6f * scale, dig.ChannelGlyph, glyphInk, "bold 15px monospace", TextAlign.Center);
            float bw = 3.2f * scale, bh = 0.45f * scale;
            float bx0 = ax - bw / 2, by0 = ay + 1.1f * scale;
            FillRect(bx0, by0, bw, bh, new RgbaColor(20, 24, 30, 220));
            FillRect(bx0, by0, bw * (float)Math.Clamp(dig.DigProgress, 0, 1), bh, fillInk);
        }

        // #313 the motion tracker: a crude corner fan of MOVING blips (bearing/range), including
        // contacts beyond the grid edge — the early warning. Cadence pulses the blips as they close.
        if (surface is { Instruments: true } tHud)
        {
            DrawMotionTracker(widthPx, heightPx, simTime, tHud);
        }

        // #317/#330 the nerve gauge: a crude deck-plan bar in the TOP-LEFT column. On the surface it is the
        // full-size head of the instrument column (the tracker seats beneath it); aboard the ship and in a
        // haven it whispers (compact, tucked below the deck chrome). Shown in every walk mode, never flight.
        if (state.ShowNerve)
        {
            DrawNerveLedger(state, heightPx);
            DrawNerveGauge(simTime, state.Nerve, state.NerveReadout, state.NerveCompact, state.HitsTaken, surface?.BloodSplash ?? 0);
        }

        // #327 the ship calls home: the mothership's orbit line, painted plainly across the TOP-CENTRE —
        // the one channel the owner's Miranda maroon never had. Never buried (the #324 visibility law):
        // calm teal while it holds, amber as it slips, a pulsing red for the last call and the maroon.
        if (surface is { OrbitComms: { Length: > 0 } orbitLine } oHud)
        {
            RgbaColor color = oHud.OrbitSeverity switch
            {
                >= 2 => new RgbaColor(255, 90, 70, (byte)(170 + 85 * (0.5 + 0.5 * Math.Sin(simTime * 4.0)))),
                1 => new RgbaColor(255, 190, 100, 235),
                _ => new RgbaColor(130, 225, 205, 220),
            };
            // COMMS-LOSS: when the downlink is degraded/blacked out the orbit line is a STALE readout — drop
            // it to a cold signal-grey and flicker its alpha like breaking static (faster + deeper on a full
            // blackout), so the frozen last-known value LOOKS lost, not just worded so. The honesty is in the
            // banner text (SurfaceComms); this is the matching visual.
            if (oHud.CommsState > 0)
            {
                double flickerHz = oHud.CommsState >= 2 ? 11.0 : 6.0;
                double floor = oHud.CommsState >= 2 ? 0.28 : 0.55; // blackout drops darker between flickers
                double f = floor + (1.0 - floor) * (0.5 + 0.5 * Math.Sin(simTime * flickerHz));
                color = new RgbaColor(170, 180, 190, (byte)(255 * Math.Clamp(f, 0.0, 1.0)));
            }
            _renderer.DrawText(widthPx / 2f, 20, orbitLine, color, "13px monospace", TextAlign.Center);
        }

        // Blind-UI audit finding: with the tube off-camera, nothing said the ship was docked or
        // how to go ashore — the tester could only guess "airlock" by genre convention. On the surface
        // the keybar turns contextual (#324): the deploy/drop keys spell themselves out while they matter.
        string bottomHint = surface is { KeyHints: { Length: > 0 } hints }
            ? hints
            : state.Docked
                ? "docked ⚓ walk up through the airlock to go ashore ∙ WASD — move ∙ E — interact ∙ F — first person ∙ Q — helm"
                : "WASD / arrows — move ∙ E — interact ∙ F — first person ∙ Q — back to the helm";
        _renderer.DrawText(ox, heightPx - 10, bottomHint, TextDim, "11px monospace", TextAlign.Center);

        // #440: the standing prompt rides just ABOVE the keybar, bright and a size up — the same eyeline the
        // player already checks for keys, but unmistakably not chrome. Gently breathing so it reads as a
        // thing still owed rather than furniture.
        if (surface is { StandingPrompt: { Length: > 0 } standing })
        {
            double breathe = 0.78 + (0.22 * Math.Sin(simTime * 0.001 * 2.2));
            var promptColor = new RgbaColor(255, 205, 90, (byte)Math.Clamp(255 * breathe, 60, 255));
            _renderer.DrawText(ox, heightPx - 30, standing, promptColor, "bold 14px monospace", TextAlign.Center);
        }

        _mask.Disarm();     // #708 · the lamp is a per-frame fact; nothing survives the frame it was aimed in
        _renderer.EndFrame();
    }

    // #424 THE UNEXPLAINED SIGNAL · the crew glance. From the freshly-filled _droids, work out each WORKING
    // crew member's facing toward the nearest OTHER crew member — so the barkeep and the dock-hand (and, on
    // the bare ship, the ship's own droids) catch each other's eye as one. A drinking patron (a seated
    // regular, the Magpie) and a Reever are never crew, so their entry stays null (they keep their own
    // facing, oblivious to the buzzer). Returns a per-droid facing override, or null where there's no glance.
    private double?[] BuildCrewGlance(int count)
    {
        var facing = new double?[count];
        // The crew indices + their world positions this frame.
        Span<int> crew = stackalloc int[count];
        int n = 0;
        for (int i = 0; i < count; i++)
        {
            if (IsCrew(_droids[i].Name))
            {
                crew[n++] = i;
            }
        }
        if (n < 2)
        {
            return facing; // a lone crew member has no one to catch eyes with — no glance
        }
        for (int a = 0; a < n; a++)
        {
            DeckPlan.Droid da = _droids[crew[a]];
            double bestSq = double.MaxValue;
            int nearest = -1;
            for (int b = 0; b < n; b++)
            {
                if (b == a)
                {
                    continue;
                }
                DeckPlan.Droid db = _droids[crew[b]];
                double d = (db.X - da.X) * (db.X - da.X) + (db.Y - da.Y) * (db.Y - da.Y);
                if (d < bestSq)
                {
                    (bestSq, nearest) = (d, crew[b]);
                }
            }
            DeckPlan.Droid dn = _droids[nearest];
            facing[crew[a]] = Math.Atan2(dn.Y - da.Y, dn.X - da.X); // world radians toward the caught eye
        }
        return facing;
    }

    // A WORKING crew member (the people who work the deck): the barkeep, the customs officer, the ship's own
    // droids — anyone who is neither a Reever nor a drinking PATRON (a seated bar regular, or the Magpie).
    private static bool IsCrew(string name) =>
        name is not ("Reever" or "Collector") && !IsSweeper(name) && !IsPatron(name);

    /// <summary>#538 · A sweeper, by callsign. Never crew: nobody on that team is going to catch a barkeep's eye
    /// during a hull shudder, and giving them the crew's grey would hide the second hostile thing on the deck.</summary>
    private static bool IsSweeper(string name) => name.StartsWith("SWEEP-", StringComparison.Ordinal);

    // The drinking patrons — the regulars' short names (HavenInterior.ShortNameFor) + the roaming Magpie +
    // the station Oracle (a ranting-drunk bar fixture, #425, not working staff) + the empty-chair fallback.
    // They never react to the off-deck buzzer; only the staff do.
    private static bool IsPatron(string name) => name switch
    {
        "Silas" or "Coil" or "Gilt-Eye" or "The Fixer" or "Regular" or "Magpie" or "Oracle" => true,
        _ => false,
    };

    // #314: brighten a colour toward white by t (0..1) — the one-frame decrement flash on the magazine
    // digits. Alpha is preserved; only the RGB warms up.
    private static RgbaColor LerpToWhite(RgbaColor c, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        static byte L(byte v, float t) => (byte)(v + (255 - v) * t);
        return new RgbaColor(L(c.R, t), L(c.G, t), L(c.B, t), c.A);
    }

    // ── #708 · THE DARK, PAINTED ─────────────────────────────────────────────────────────────────────────
    //
    // The LampMask decides what gets DRAWN; this decides where it STOPS. A wall that starts under the
    // captain's boots and runs the width of the field passes the mask honestly — part of it is lit — and
    // without this it would be drawn all the way into the black. So once the world is down, everything
    // outside the light is painted over in Pitch.
    //
    // It is a fan of opaque wedges rather than one clever polygon-with-a-hole, because a canvas fill rule is
    // not a thing this renderer's command buffer carries and a hole punched with an even-odd winding is the
    // sort of drawing that works until somebody's browser disagrees. A wedge is four points and a fill.
    //
    // Each wedge runs from an INNER radius out past the farthest corner of the viewport. The inner radius is
    // the whole of the model, and it is two values: the lamp's reach where the cone points, the arm's-reach
    // ring everywhere else.

    /// <summary>How finely the fan is stepped. The inner arc's vertices are pushed out by 1/cos(step/2) so
    /// the chords lie ON the true radius at their midpoints and OUTSIDE it everywhere else — the black
    /// therefore never creeps inside the light, which would nibble the cone away from its own edge.</summary>
    private const double MaskStepRad = 4.0 * Math.PI / 180.0;

    private void PaintTheDark(int widthPx, int heightPx, in State state, float scale, float ox, float oy)
    {
        float ax = ox + ((float)state.AvatarX * scale);
        float ay = oy - ((float)state.AvatarY * scale);

        // Far enough to clear the corner of the canvas the captain is standing furthest from, whatever the
        // pan and whatever the follow-cam is doing.
        float far = 8f + Math.Max(
            Math.Max(Corner(0, 0), Corner(widthPx, 0)),
            Math.Max(Corner(0, heightPx), Corner(widthPx, heightPx)));

        float Corner(float cx, float cy) => MathF.Sqrt(((cx - ax) * (cx - ax)) + ((cy - ay) * (cy - ay)));

        // The captain's facing in SCREEN angle: deck +y is up, screen +y is down, so the sign flips.
        double axis = -state.HeadingRad;
        double half = SpaceSails.Core.SuitLamp.ConeHalfAngleDegrees * Math.PI / 180.0;
        float coneInner = (float)(SpaceSails.Core.SuitLamp.RangeDu * scale);
        float ringInner = (float)(LampRingDu * scale);

        // Two arcs, and they meet exactly on the cone's edges — so the edge itself is one straight radial
        // line, drawn to the pixel, which is the line the captain reads a wall appearing at.
        Fan(axis - half, axis + half, coneInner);                       // down the beam: black past the reach
        Fan(axis + half, axis - half + (2 * Math.PI), ringInner);       // everywhere else: black past the ring

        void Fan(double from, double to, float inner)
        {
            int steps = Math.Max(1, (int)Math.Ceiling((to - from) / MaskStepRad));
            double step = (to - from) / steps;
            float push = (float)(1.0 / Math.Cos(step / 2));
            float outer = far * push;

            for (int i = 0; i < steps; i++)
            {
                double a0 = from + (step * i);
                double a1 = a0 + step;
                float c0 = (float)Math.Cos(a0), s0 = (float)Math.Sin(a0);
                float c1 = (float)Math.Cos(a1), s1 = (float)Math.Sin(a1);
                float ri = inner * push;

                _scratch[0] = ax + (c0 * ri); _scratch[1] = ay + (s0 * ri);
                _scratch[2] = ax + (c1 * ri); _scratch[3] = ay + (s1 * ri);
                _scratch[4] = ax + (c1 * outer); _scratch[5] = ay + (s1 * outer);
                _scratch[6] = ax + (c0 * outer); _scratch[7] = ay + (s0 * outer);
                _renderer.DrawPolygon(_scratch.AsSpan(0, 8), Pitch, Pitch, 1f);
            }
        }
    }

    // #563 · How deep the dark reaches in from an unseen bound, in deck units, and how far that depth
    // wanders along it. A straight falloff is just the rectangle again; these two numbers are what stop the
    // eye finding a corner.
    private const double FalloffBaseDu = 7.0;
    private const double FalloffWanderDu = 5.0;

    /// <summary>Darken the ground approaching every <see cref="DeckPlan.Wall.Unseen"/> bound, with an
    /// irregular inner edge. No-op on any plan without unseen walls — i.e. every ship and station.</summary>
    private void DrawUnseenFalloff(DeckPlan plan, float scale, float ox, float oy)
    {
        // The bounds of the unseen set tell us which side of the field each one is, and therefore which way
        // "inward" points — a vertical wall at the smallest x faces right, and so on.
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        int unseen = 0;
        foreach (DeckPlan.Wall w in plan.Walls)
        {
            if (!w.Unseen) { continue; }
            unseen++;
            minX = Math.Min(minX, Math.Min(w.X1, w.X2)); maxX = Math.Max(maxX, Math.Max(w.X1, w.X2));
            minY = Math.Min(minY, Math.Min(w.Y1, w.Y2)); maxY = Math.Max(maxY, Math.Max(w.Y1, w.Y2));
        }
        if (unseen == 0)
        {
            return;
        }

        const int Bands = 4;
        foreach (DeckPlan.Wall w in plan.Walls)
        {
            if (!w.Unseen) { continue; }

            bool vertical = Math.Abs(w.X1 - w.X2) < 0.001f;
            double inward = vertical
                ? (Math.Abs(w.X1 - minX) < Math.Abs(w.X1 - maxX) ? 1.0 : -1.0)
                : (Math.Abs(w.Y1 - minY) < Math.Abs(w.Y1 - maxY) ? 1.0 : -1.0);

            double a0 = vertical ? Math.Min(w.Y1, w.Y2) : Math.Min(w.X1, w.X2);
            double a1 = vertical ? Math.Max(w.Y1, w.Y2) : Math.Max(w.X1, w.X2);
            const double step = 2.0;

            for (double a = a0; a < a1; a += step)
            {
                double span = Math.Min(step, a1 - a);
                double depth = FalloffBaseDu + (FalloffWanderDu * Wander(a, vertical ? w.X1 : w.Y1));

                for (int k = 0; k < Bands; k++)
                {
                    // Darkest against the bound, thinning inward. Near-black keyed to the floor's own blue
                    // so the dark reads as unlit ground rather than as a painted shape.
                    var ink = new RgbaColor(4, 6, 10, (byte)(205 - (k * 48)));
                    double d0 = depth * k / Bands, d1 = depth * (k + 1) / Bands;
                    double c0 = vertical ? w.X1 + (inward * d0) : w.Y1 + (inward * d0);
                    double c1 = vertical ? w.X1 + (inward * d1) : w.Y1 + (inward * d1);

                    (double bx0, double by0, double bx1, double by1) = vertical
                        ? (Math.Min(c0, c1), a, Math.Max(c0, c1), a + span)
                        : (a, Math.Min(c0, c1), a + span, Math.Max(c0, c1));

                    float sx0 = ox + ((float)bx0 * scale), sy0 = oy - ((float)by1 * scale);
                    float sx1 = ox + ((float)bx1 * scale), sy1 = oy - ((float)by0 * scale);
                    FillRect(sx0, sy0, sx1 - sx0, sy1 - sy0, ink);
                }
            }
        }
    }

    /// <summary>A stable 0..1 wander keyed to a world position — deterministic, so the dark edge is a fact
    /// about the place and does not shimmer as the camera moves or the frame ticks.</summary>
    private static double Wander(double along, double which)
    {
        // Low frequency ON PURPOSE. A high-frequency hash makes adjacent steps uncorrelated and the
        // dark edge reads as a jagged comb — obviously generated. This undulates over tens of deck
        // units, so the boundary wanders the way a shadow line does and no straight run is legible.
        double s = Math.Sin((along * 0.11) + (which * 0.037)) * 0.5;
        return s + 0.5;   // 0..1
    }

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

    // The crude motion-tracker fan (top-right corner, screen-space): a graph-paper radar showing MOVING
    // contacts by bearing + range, clamped to the ring when beyond it. Blips pulse faster as they close.
    private static readonly RgbaColor TrackerRing = new(120, 200, 150, 150);

    // #330 · Where the left-edge instrument column begins under the SANITY plate (its base bottom ≈ 70px
    // + a small consistent gap). The tracker's centre sits directly below this — one honest column.
    private const double SanityColumnTop = 82.0;
    private const double TrackerDesiredRadius = 116.0;   // owner: "make the motion meter bigger" — the ~115 class

    private void DrawMotionTracker(int widthPx, int heightPx, double simTime, in SurfaceHud hud)
    {
        // #324/#330 (owner: "make the motion meter bigger and visible… put the motion under the sanity
        // meter"): the excursion's star instrument, big enough to read at a glance, seated in the top-left
        // column directly beneath the SANITY plate on an opaque disc. It SHRINKS proportionally on a small
        // viewport rather than clipping, and the ship-desk chrome is hidden on-surface so nothing buries it.
        float r = (float)MotionTracker.TrackerRadius(widthPx, heightPx, SanityColumnTop, TrackerDesiredRadius);
        (double acx, double acy) = MotionTracker.TrackerAnchor(widthPx, heightPx, r, SanityColumnTop);
        float cx = (float)acx, cy = (float)acy;

        // Sizes scale with the disc so a shrunk tracker stays legible and a big one reads across the map.
        float labelPx = (float)Math.Clamp(r * 0.13, 10, 15);
        float readoutPx = (float)Math.Clamp(r * 0.11, 9, 13);
        // Lane-1 (owner, 2026-07-18): the Reever blips read SMALLER than the old contacts — a tight,
        // insistent dot, not a fat one, so a crowding tide is a rash of pinpricks rather than a smear.
        float blipNear = (float)Math.Max(2.6, r * 0.042);
        float blipFar = (float)Math.Max(2.0, r * 0.032);

        // The graph-paper fan: an opaque backing disc, three rings + crosshair.
        _renderer.DrawCircle(cx, cy, r + 6f, new RgbaColor(6, 11, 10, 238), TrackerRing, 1f);
        _renderer.DrawCircle(cx, cy, r, null, TrackerRing, 1.75f);
        _renderer.DrawCircle(cx, cy, r * 0.66f, null, new RgbaColor(120, 200, 150, 85), 1f);
        _renderer.DrawCircle(cx, cy, r * 0.33f, null, new RgbaColor(120, 200, 150, 70), 1f);
        DrawSeg((cx - r, cy), (cx + r, cy), new RgbaColor(120, 200, 150, 70), 1f);
        DrawSeg((cx, cy - r), (cx, cy + r), new RgbaColor(120, 200, 150, 70), 1f);
        _renderer.DrawText(cx, cy - r - 8, "MOTION TRACKER", TrackerRing, $"bold {labelPx:0}px monospace", TextAlign.Center);

        // #591 · WHERE YOU ARE, ON THE INSTRUMENT. Owner: "the motion tracker should be in underground
        // visibility mode when we are deeeeeeeep under surface" — and depth is the single most important
        // fact about a captain's situation down there, because it is the number that decides whether they
        // get back up on the air they have. It was only ever readable as a label lying on the floor plan
        // behind them, which is the wrong place: you read the plan when you are thinking and the instrument
        // when you are worried.
        //
        // Drawn in the fan's own ink, above the ring beside the title, so a glance says "you are inside
        // something" before a single word is read.
        if (hud.TrackerPlace is { Length: > 0 } place)
        {
            _renderer.DrawText(cx, cy - r + 4, place, TrackerRing,
                $"{Math.Clamp(labelPx * 0.82, 8, 11):0}px monospace", TextAlign.Center);
        }

        // Lane-1 (owner, 2026-07-18): the Reever blips are red and "pulsing like a heartbeat" — the
        // creatures' pulse on the sweep. A lub-dub envelope drives the blips' size and glow, quickening
        // with the tracker cadence as the nearest closes; even a far-off tide keeps a slow, live beat.
        double beatHz = hud.Cadence switch { 3 => 2.4, 2 => 1.6, 1 => 1.0, _ => 0.75 };
        double beat = Heartbeat((simTime * 0.001 * beatHz) % 1.0); // 0..1 lub-dub envelope
        byte beatAlpha = (byte)(120 + (135 * beat));
        float beatScale = 0.72f + (0.5f * (float)beat);

        // #338 "The long ear": the range that reaches the ring edge is no longer a magic 60 — it is a
        // MULTIPLE of what the eye actually sees here (the surface camera shows 64×28 du, so this viewport's
        // visible half-width in du is widthPx/scale/2), so the fan hears several times farther than the grid
        // shows. A blip's DISTANCE is read straight off the fan: faint + small on the rim, firming to an
        // insistent near dot as it closes (MotionTracker.BlipIntensity) — the dread-gap made visible.
        // #591: the reach is HANDED DOWN by the sim now, because the sim and this draw were deriving it
        // separately and could disagree — and underground the sim shortens it with depth, which would have
        // made that disagreement the difference between a blip you can hear and a blip you can only see.
        // The viewport derivation stays as the fallback for callers from before the field existed.
        float surfScale = Math.Min(widthPx / 64f, heightPx / 28f);
        double visualHalfWidthDu = (widthPx / Math.Max(surfScale, 0.001f)) / 2.0;
        double detectionRange = hud.FanReach > 0 ? hud.FanReach : MotionTracker.DetectionRange(visualHalfWidthDu);
        foreach ((double bearing, double range) in hud.Blips)
        {
            double rr = Math.Min(range / detectionRange, 1.0) * (r - 6);
            // World bearing: +x = right, +y = port (up on screen) → screen y flips.
            float bx = cx + (float)(Math.Cos(bearing) * rr);
            float by = cy - (float)(Math.Sin(bearing) * rr);
            double firm = MotionTracker.BlipIntensity(range, detectionRange); // 1 near … FaintFloor on the rim
            float sz = (float)(blipFar + ((blipNear - blipFar) * firm)) * beatScale;
            byte alpha = (byte)Math.Clamp(beatAlpha * (0.35 + (0.65 * firm)), 30, 255);
            var col = new RgbaColor(235, 70, 60, alpha); // watchdog red, pulsing — dimmer the farther out
            _renderer.DrawCircle(bx, by, sz, col, col);
        }

        // #573 · A RUMOUR: a soft, wide, low-contrast wash. It is under everything else on purpose — it is
        // the least certain thing on the instrument and must never compete with a contact.
        if (hud.Rumours is { Count: > 0 } rumours)
        {
            foreach ((double bearing, double range, double spread) in rumours)
            {
                double rr = Math.Min(range / detectionRange, 1.0) * (r - 5);
                float bx = cx + (float)(Math.Cos(bearing) * rr);
                float by = cy - (float)(Math.Sin(bearing) * rr);
                float wide = (float)Math.Max(9.0, spread / detectionRange * r);
                for (int ring = 3; ring >= 1; ring--)
                {
                    _renderer.DrawCircle(bx, by, wide * ring / 3f, null,
                        new RgbaColor(120, 170, 210, (byte)(30 + (10 * (3 - ring)))), 1f);
                }
            }
        }

        // #573 · Own caches, once they are close enough for the fan to have any business knowing.
        if (hud.CacheBeacons is { Count: > 0 } caches)
        {
            foreach ((double bearing, double range) in caches)
            {
                double rr = Math.Min(range / detectionRange, 1.0) * (r - 5);
                float bx = cx + (float)(Math.Cos(bearing) * rr);
                float by = cy - (float)(Math.Sin(bearing) * rr);
                var gold = new RgbaColor(235, 205, 120, 220);
                DrawSeg((bx - 3.5f, by - 3.5f), (bx + 3.5f, by + 3.5f), gold, 1.6f);
                DrawSeg((bx + 3.5f, by - 3.5f), (bx - 3.5f, by + 3.5f), gold, 1.6f);
            }
        }

        // #573 · The beacons, drawn UNDER nothing and OVER the rings: hollow, calm, and slowly breathing,
        // so they never read as contacts. A place clamps to the rim when it is beyond the fan's reach, the
        // same way a distant mover does — you always know which way it is, never how far once it is far.
        if (hud.Beacons is { Count: > 0 } beacons)
        {
            double breathe = 0.85 + (0.15 * Math.Sin(simTime * 0.0016));
            foreach ((double bearing, double range, bool isHome, bool isLab) in beacons)
            {
                double rr = Math.Min(range / detectionRange, 1.0) * (r - 5);
                float bx = cx + (float)(Math.Cos(bearing) * rr);
                float by = cy - (float)(Math.Sin(bearing) * rr);

                // The way home is warmer than a shelter, because they are not the same promise: one is your
                // ship, the other is somebody else's roof.
                //
                // #585 · And a LIFT HEAD is neither, so it gets the imported violet the door itself wears
                // (#592). With nine shelter rings on the fan, one more ring in the same ink is not a signal —
                // the owner had a tracker full of identical circles and no way to tell which one was the way
                // down. A beacon that cannot be told apart from its neighbours is decoration.
                var ink = isLab
                    ? new RgbaColor(
                        SpaceSails.Core.BodyPalette.Imported.R,
                        SpaceSails.Core.BodyPalette.Imported.G,
                        SpaceSails.Core.BodyPalette.Imported.B, (byte)(235 * breathe))
                    : isHome
                        ? new RgbaColor(150, 215, 255, (byte)(210 * breathe))
                        : new RgbaColor(130, 235, 215, (byte)(195 * breathe));

                // The lab ring is drawn a size larger and doubled, so it reads at a glance on a busy fan.
                _renderer.DrawCircle(bx, by, (float)((isLab ? 8.0 : 5.5) * breathe), null, ink, isLab ? 2.4f : 1.8f);
                _renderer.DrawCircle(bx, by, isLab ? 2.4f : 1.6f, ink, ink);
            }
        }

        _renderer.DrawText(cx, cy + r + 14, hud.Readout, TrackerRing, $"{readoutPx:0}px monospace", TextAlign.Center);

        // #564 · THE AIR METER, directly under the tracker where the owner looked for it. It gets a BAR
        // rather than a line of text for the same reason NERVE does: it is one of the two things on a
        // surface that can kill you without anything touching you, and a number buried among key hints is
        // not something a captain glances at while a pack closes.
        float airBottom = cy + r + 14 + readoutPx + 6f;
        if (hud.AirSeconds >= 0)
        {
            float aw = r * 1.75f, ah = Math.Max(7f, r * 0.085f);
            float ax0 = Math.Max(8f, cx - (aw / 2)), ay0 = airBottom;
            double frac = Math.Clamp(hud.AirSeconds / SuitAir.TankSeconds, 0, 1);

            // Colour is the BAND, not the fraction — because the question is never "how full is it" but
            // "can I still get home from here", and those two part company the moment you walk anywhere.
            SuitAir.Band band = SuitAir.BandFor(hud.AirSeconds, hud.AirDistanceHome);
            RgbaColor fill = band switch
            {
                SuitAir.Band.Easy => new RgbaColor(120, 200, 235, 235),
                SuitAir.Band.Thinking => new RgbaColor(225, 200, 95, 240),
                SuitAir.Band.PastTheLine => new RgbaColor(240, 120, 60, 245),
                SuitAir.Band.Critical => new RgbaColor(255, 60, 45, 250),
                _ => new RgbaColor(90, 40, 38, 230),
            };

            // #612 · AND WHERE IT IS COMING FROM. Owner, on a pressurised floor with no way to tell:
            // "Maybe we should have on our hud a AIR: Tanks / External symbol... it is vital info."
            //
            // Drawn as a SOLID CHIP — dark letters on a block of colour — rather than as coloured text,
            // because a filled block is read pre-attentively and a word is not. At a glance the captain gets
            // green-or-amber; a beat later a triangle pointing down or a stopped square; only then the word.
            // That is three chances to learn the most consequential fact on the surface without reading.
            //
            // The chip is a SEPARATE colour from the bar on purpose. The bar answers "can I still get home
            // on this tank", which stays a real question in a shelter — you have to leave eventually. The
            // chip answers "is it going down right now". Both are true at once and they are not the same,
            // and the old gauge could only show one of them.
            bool drawing = SuitAir.Drawing(hud.AirSupply);
            RgbaColor chipInk = hud.AirSupply switch
            {
                SuitAir.Supply.Room => StencilAir,
                SuitAir.Supply.Ship => new RgbaColor(150, 215, 255, 245),
                _ => StencilDead,
            };

            FillRect(ax0 - 6f, ay0 - 15f, aw + 12f, ah + 32f, new RgbaColor(6, 11, 10, 205));
            _renderer.DrawText(ax0, ay0 - 4f, "AIR", fill, "bold 10px monospace", TextAlign.Left);

            string chip = $"{SuitAir.SourceGlyph(hud.AirSupply)} {SuitAir.SourceLabel(hud.AirSupply)}";
            float chipW = (chip.Length * 6.2f) + 10f;
            float chipX = Math.Max(ax0 + 26f, ax0 + aw - chipW);
            FillRect(chipX, ay0 - 14f, chipW, 13f, chipInk);
            _renderer.DrawText(chipX + 5f, ay0 - 4f, chip, new RgbaColor(8, 12, 16, 255),
                "bold 10px monospace", TextAlign.Left);

            FillRect(ax0, ay0, aw, ah, new RgbaColor(14, 18, 24, 220));
            FillRect(ax0, ay0, aw * (float)frac, ah, fill);
            // A held tank is ringed in its source's colour, so the "is it running" answer is on the bar
            // itself and not only on the chip above it — the one place a captain's eye is already resting.
            DrawRectOutline(ax0, ay0, aw, ah, drawing ? TrackerRing : chipInk);
            _renderer.DrawText(ax0, ay0 + ah + 11f,
                SuitAir.Readout(hud.AirSeconds, hud.AirDistanceHome, hud.AirSupply),
                drawing ? fill : chipInk, "10px monospace", TextAlign.Left);
            airBottom = ay0 + ah + 20f;
        }

        // Lane-1: the dig/sentry captions seated beneath the readout (owner: "advertise the dig and bot
        // options in text under the motion detector"). Column chrome only — and drawn only while each line
        // clears the viewport bottom, so a short screen never buries the keybar under them.
        if (hud.TrackerCaptions is { Count: > 0 } captions)
        {
            // #440: these were CENTRED on the tracker's centre-x. The tracker sits in the left instrument
            // gutter, so every caption longer than about twice that inset ran off the left edge of the
            // canvas and was sliced — the leading glyph gone, the line starting mid-word at x=0. The one
            // place the ground explains itself was unreadable (owner: "not advertised clearly enough").
            // Left-align them in the gutter instead, so a caption grows RIGHTWARDS into open screen and
            // every word survives however long the line gets.
            float capPx = (float)Math.Clamp(r * 0.095, 9, 12);
            float capY = airBottom + 6f;
            float capX = Math.Max(8f, cx - r);
            foreach (string caption in captions)
            {
                if (string.IsNullOrEmpty(caption) || capY > heightPx - 16)
                {
                    break;
                }
                _renderer.DrawText(capX, capY, caption, TextDim, $"{capPx:0}px monospace", TextAlign.Left);
                capY += capPx + 5f;
            }
        }
    }

    // A lub-dub heartbeat envelope over a [0,1) beat phase: two quick gaussian thumps near the start of
    // the cycle, then a rest — the shape the Reever blips pulse to (owner: "pulsing like a heartbeat").
    private static double Heartbeat(double phase)
    {
        double lub = Math.Exp(-Math.Pow((phase - 0.06) / 0.05, 2));
        double dub = 0.7 * Math.Exp(-Math.Pow((phase - 0.20) / 0.055, 2));
        return Math.Min(1.0, lub + dub);
    }

    // #317 the nerve gauge (top-left, screen-space): a crude deck-plan bar — full teal = steady hands,
    // draining through amber to blood as the regolith's stressors fray the captain. The whole gauge trembles
    // harder the lower the nerve falls (the "tremor in the glyph" the flavor ladder names), and a house-voice
    // line reads out beneath it. Display-only — this slice never rolls, exits, or ends a run (#226 owns that).
    private static readonly RgbaColor NerveFrame = new(150, 170, 190, 175);
    private void DrawNerveGauge(double simTime, double nerve, string readout, bool compact, int hitsTaken, double bloodFlash)
    {
        double frac = NerveModel.Fraction(nerve);
        NerveModel.NerveBand band = NerveModel.BandFor(nerve);
        RgbaColor fill = band switch
        {
            NerveModel.NerveBand.Steady => new RgbaColor(120, 220, 170, 235),
            NerveModel.NerveBand.Rattled => new RgbaColor(185, 220, 130, 235),
            NerveModel.NerveBand.Shaken => new RgbaColor(230, 200, 90, 240),
            NerveModel.NerveBand.Fraying => new RgbaColor(235, 150, 80, 245),
            _ => new RgbaColor(230, 80, 70, 250),
        };

        // The trembling scales with how much nerve is GONE — steady hands are still, shot ones shake hard.
        double tremor = 1.0 - frac;
        float jx = (float)(Math.Sin(simTime * 0.02) * tremor * tremor * 3.0);
        float jy = (float)(Math.Cos(simTime * 0.017) * tremor * tremor * 2.0);

        // #324/#330 (owner: "let's make sanity visible :-D … even on the ship bar also"): a plainly-labelled
        // top-left gauge on its own dark plate. Full-size on the regolith where the FP toggle steps aside;
        // COMPACT aboard/ashore, tucked below the deck chrome (the top-left first-person toggle) so it
        // whispers without colliding.
        // #380 item 2: the plate NAMES the meter — "NERVE", the diegetic name every flavor rung, band-drop,
        // and shock pulse already speaks (the #226 sanity system's on-screen face). No name, no cause, no
        // remedy was the mystery; the name lands here, the cause+remedy in the band-drop pulse (Map.Surface).
        float w = compact ? 150f : 210f;
        float h = compact ? 13f : 18f;
        float labelPx = compact ? 9f : 11f;
        float baseY = compact ? 112f : 30f;   // aboard: clear below the top-left FP toggle; surface: column head
        float x0 = 18f + jx, y0 = baseY + jy;

        FillRect(x0 - 8f, y0 - 20f, w + 16f, h + 42f, new RgbaColor(6, 11, 10, 205));  // the backing plate
        _renderer.DrawText(x0, y0 - 6, "NERVE", NerveFrame, $"bold {labelPx:0}px monospace", TextAlign.Left);

        // #480 · TEN WHOLE PIPS, not a bar. Owner: "the sanity events should be quantized … not this float
        // stuff we have now." A sliding fill is exactly what made a loss unreadable — you cannot tell a
        // slide from a stop, or a big cause from a small one. Discrete pips can only ever change by a whole
        // unit, so the eye sees COUNT, and the flash line beside them says which cause spent it. Deliberately
        // the same pip idiom as the condition marker below, because the two meters are now comparable (#469).
        FillRect(x0, y0, w, h, new RgbaColor(14, 18, 24, 220));           // the empty channel
        int pipsLeft = NervePips.PipsOf(nerve);
        float npGap = w * 0.012f;
        float npW = (w - (npGap * (NervePips.MaxPips - 1))) / NervePips.MaxPips;
        for (int i = 0; i < NervePips.MaxPips; i++)
        {
            float px = x0 + (i * (npW + npGap));
            FillRect(px, y0, npW, h, i < pipsLeft ? fill : new RgbaColor(22, 28, 34, 200));
        }
        DrawRectOutline(x0, y0, w, h, NerveFrame);                        // the frame
        _renderer.DrawText(x0, y0 + h + 13, readout, fill, $"{labelPx:0}px monospace", TextAlign.Left);

        // #453 · THE CONDITION MARKER, under the nerve bar exactly where the owner asked for it ("Some kind
        // of hit condition marker below the nerves bar"). Five pips: how many blows are left in you. It is
        // NOT a second bar — nerve is a slope you slide down, skin is a countdown you can read at a glance,
        // and the two must never be mistaken for each other while you decide whether to run.
        if (hitsTaken >= 0)
        {
            float py = y0 + h + 22f;
            float pip = compact ? 7f : 10f;
            float gap = pip * 0.55f;
            int left = Math.Max(0, CaptainCondition.MaxHits - hitsTaken);
            var spent = new RgbaColor(70, 26, 24, 220);
            var intact = left switch
            {
                >= 4 => new RgbaColor(200, 90, 85, 240),
                3 => new RgbaColor(225, 120, 70, 245),
                2 => new RgbaColor(235, 90, 60, 250),
                _ => new RgbaColor(255, 45, 35, 255),   // one left: the loudest thing in the corner
            };
            for (int i = 0; i < CaptainCondition.MaxHits; i++)
            {
                float px = x0 + (i * (pip + gap));
                RgbaColor fillPip = i < left ? intact : spent;
                // #467: the pip that just went out FLASHES white-hot for a beat, so the eye is pulled to the
                // corner exactly when it changed rather than discovering the loss later.
                if (i == left && bloodFlash > 0)
                {
                    byte hot = (byte)Math.Clamp(255 * bloodFlash, 0, 255);
                    fillPip = new RgbaColor(255, (byte)(230 * bloodFlash), (byte)(210 * bloodFlash), hot);
                }
                FillRect(px, py, pip, pip, fillPip);
                DrawRectOutline(px, py, pip, pip, NerveFrame);
            }
            _renderer.DrawText(x0 + (CaptainCondition.MaxHits * (pip + gap)) + 6f, py + pip - 1f,
                CaptainCondition.Readout(hitsTaken), intact, $"{labelPx:0}px monospace", TextAlign.Left);
        }
    }

    // #480 · THE CAUSE, said twice. The FLASH is the line for the pip that just moved, sat right under the
    // gauge where the eye already is; the LEDGER keeps the last few so "what broke me?" has an answer after
    // the fact (the death card reads the same list). Owner: "what caused the sanity loss and what we did to
    // regain it. Now it is vague and wishy-washy." Losses read red, gains green — a recovery must be as
    // legible as a loss, or only half the ruling is honoured.
    private void DrawNerveLedger(in State state, int heightPx)
    {
        var ledger = state.NerveLedger;
        bool hasFlash = !string.IsNullOrEmpty(state.NerveFlash);
        if (!hasFlash && (ledger is null || ledger.Count == 0))
        {
            return;
        }

        float px = state.NerveCompact ? 9f : 11f;
        float x = 18f;

        // Anchored to the BOTTOM of the left column, growing upward. The first cut sat it directly under
        // the gauge and it landed straight on top of the motion tracker's fan — unreadable, and it buried
        // the one instrument you actually steer by. Down here it shares the column with nothing, and the
        // reading order still runs newest-nearest-the-eye.
        // The bottom margin clears the keybar AND the first-person toggle that sits in this same corner —
        // at 46 the last ledger line printed straight through the button.
        const float BottomClearance = 78f;
        float lineH = px + 2f;
        int rows = (ledger?.Count ?? 0) + (ledger is { Count: > 0 } ? 1 : 0) + (hasFlash ? 1 : 0);
        float y = heightPx - BottomClearance - (rows * lineH);

        if (hasFlash)
        {
            _renderer.DrawText(x, y, state.NerveFlash!, new RgbaColor(255, 225, 210, 250),
                $"bold {px:0}px monospace", TextAlign.Left);
            y += lineH + 4f;
        }

        if (ledger is null || ledger.Count == 0)
        {
            return;
        }

        _renderer.DrawText(x, y, "NERVE LEDGER", NerveFrame, $"bold {px - 1:0}px monospace", TextAlign.Left);
        y += lineH;
        for (int i = 0; i < ledger.Count; i++)
        {
            // Older lines fade — the newest cause is the one that matters while you are deciding to run.
            byte a = (byte)Math.Clamp(215 - (i * 22), 70, 255);
            bool gain = ledger[i].Contains('+');
            var c = gain ? new RgbaColor(120, 220, 170, a) : new RgbaColor(235, 140, 130, a);
            _renderer.DrawText(x, y, ledger[i], c, $"{px - 1:0}px monospace", TextAlign.Left);
            y += lineH;
        }
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
