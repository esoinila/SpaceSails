using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;
// Map.Surface — the walked surface excursion (#295 walked bury; #313 destination-first). The shuttle
// asks WHERE, not WHY: boarding offers a destination, the tube grows in place, and the captain walks
// down and commits to nothing. Digging is a timed, abortable channel; the Old Ones (Reevers) shamble
// but come in numbers, converging from the edges — the motion tracker is the early warning, the
// crew-only door is the sanctuary, and nothing on the moon ever self-resolves.

/// <summary>
/// #251 · THE EXCURSION'S TUNING AND ITS LIVE STATE — the numbers the moon is played at, and everything
/// one walk down the tube remembers while it lasts: the nerve, the Old Ones, the tracker's draw buffers,
/// the cheats, the four teaching cards, the descent, and the ship's sentry roster.
///
/// <para>This file is the head of the <c>Map.Surface</c> tree, whose leaves were cut off one at a time
/// under #870 and #251. The three cut from THIS file are named for the moment they own: <c>.Cast</c> (the
/// figures on the ground and the channels they work through, as types), <c>.Boarding</c> (the one method
/// that builds an excursion and grows the tube in place), and <c>.Liftoff</c> (going home, which nothing
/// but the captain ever starts).</para>
///
/// <para>Every constant here is a <c>const</c> and the family declares no <c>static readonly</c> at all,
/// so no initializer chain can be broken by the cut — see
/// <c>NoPartialClassSpreadsItsStaticFieldsTests</c>. No member is renamed, re-scoped or re-ordered.</para>
/// </summary>
public partial class Map
{

    // Old-Ones tuning (#313, owner: "little more goblin like in their speed... their fear is that they
    // are so many"): a shamble well under the captain's 9.0 — even a chest-carrying captain (0.8×9=7.2)
    // outpaces them, so the menace is numbers + persistence, not pace. FLAGGED for the owner's verdict.
    private const double ReeverSpeed = 5.6;

    // Carrying a chest slows the captain to 7.2 du/s — still faster than the shamble, but DROPPING it
    // (panic key) restores full 9.0. The honest carry-speed default the owner left unruled (flagged).
    private const double CarryChestSpeedFactor = 0.8;

    // A dig (bury OR lift) is a channeled action: several real seconds of shovel-work you can be caught
    // mid. The vulnerability window the owner asked for.
    private const double DigChannelSeconds = 3.6;

    // Encirclement: each Old One leans this fraction of its aim toward the tube mouth (the choke), so
    // the pack cuts angles to corner the captain instead of trailing single-file. Cheap, no pathfinding.
    private const double EncircleBias = 0.28;

    // #472: inside this range the pack stops cutting angles and simply comes for you — the bias is a way of
    // ARRIVING, never a reason not to. Between here and EncircleFadeRange further out it eases back in.
    private const double EncircleCloseRange = 6.0;
    private const double EncircleFadeRange = 14.0;

    // Lane-1 · the ENGINE ceiling on simultaneously ACTIVE Reevers (owner, 2026-07-18). This is a perf
    // guard, NOT a gameplay cap: the tide as a rule never stops ("without any limited number"), but we
    // won't hold more than this many live contacts at once for the render/step budget. Generous by
    // design — the tide rarely reaches it unless the captain lingers deep for a very long time. Sizes the
    // surface droid buffer (3 crew + this ≤ DeckPlan.MaxDroids = 27).
    private const int ReeverEngineCeiling = 24;

    // #318 false-hang follow-up: per-frame ceilings for the surface spawners. The step delta is clamped to
    // MaxSurfaceStepSeconds (the same 0.1 s cap StepReevers uses) so a background-tab resume can't hand a
    // multi-second delta into an accumulator, and at most MaxTideSpawnsPerFrame claw-outs resolve in any
    // one frame — a hard guard so the loop can never spin the frame. The backlog simply catches up over
    // the next few frames; the tide is relentless, never instantaneous.
    private const double MaxSurfaceStepSeconds = 0.1;
    private const int MaxTideSpawnsPerFrame = 4;

    // #317 · The nerve gauge (first slice of #226's Fail Forward sanity). The captain's nerve, 0..100:
    // full = steady hands, empty = nerves shot. Drains from the regolith's stressors, eases off aboard,
    // and — unlike Reever positions — PERSISTS in the vault (a captain who fled shaking is still shaking
    // after a reload). Display-first: the bar bottoming out only SPEAKS; consequences stay with #226.
    private double _nerve = NerveModel.Steady;
    private bool _monolithSeen; // the Lovecraftian first-sight hit fires once in a life (persisted)

    // #480 · The nerve is QUANTIZED — ten whole pips, and nothing moves it anonymously. Owner: "what caused
    // the sanity loss and what we did to regain it. Now it is vague and wishy-washy." NervePips owns the
    // law; the client's job is to read the situation, carry the clocks, and SAY what happened.
    private NervePips.Beats _nerveBeats = NervePips.Beats.Fresh;   // beat clocks + the touch latch
    private double _nerveShockCarry;                               // sub-pip prickles bank here
    private IReadOnlyList<NervePips.Event> _nerveLedger = [];      // newest first — the Captain desk reads it
    private bool _touchedThisFrame;                                // set by the catch/exchange, priced by StepNerve
    private string? _nerveFlash;                                   // the in-the-moment line by the gauge
    private double _nerveFlashUntilMs;

    /// <summary>How long a nerve event's line hangs by the gauge before fading.</summary>
    private const double NerveFlashMs = 2600;

    // #649 · First sight of the monolith: the range is the OBJECT'S, not this file's.
    //
    // It was a flat 26 du typed here — eyeballed against a slab six deck units across, back when the slab
    // was on the wrong moon. The stone is fifty-four across now and its swept apron is eighty-six, so 26
    // would have put the biggest single fright in a captain's life at the moment they walked into the rock,
    // with nothing left to resolve out of anything. Monolith.SightRangeDu is three fifths of its height, so
    // the beat lands while the thing is still a shape on the tracker and the RESOLVING is what does it —
    // and if it ever grows again, the sight grows with it instead of quietly becoming a lie.

    // Cornered: a Reever wedged up-field of the captain and this close laterally reads as a net across the
    // escape (owner: "being cornered"). A cheap geometry check — no pathfinding. FLAGGED for tuning.
    private const double CornerLateralRange = 7.0;

    // The live excursion, or null when we're not on a surface. Reever state is client-only real-time
    // (never saved — same law as any NPC position).
    private SurfaceExcursion? _surface;
    private readonly List<Reever> _reevers = [];
    private double _lastReeverCatchMs;
    private double? _lastNearestReeverRange; // for the tracker's closing/drifting read

    // #371 Phase 1 (perf) · reusable HUD buffers. BuildSurfaceHud runs EVERY surface frame and used to
    // allocate ~7 fresh LINQ Lists each time; these instance buffers are cleared-and-refilled instead. Safe
    // because the SurfaceHud that borrows them is consumed synchronously inside the same DrawWalkFrame call
    // (the previous frame's HUD is dead before the next refill), so nothing outlives a buffer's contents.
    private readonly List<MotionTracker.Entity> _hudEntities = [];
    // #830 · …and each return carries WHICH KIND it is, because a fan that reports a standing man and a
    // walking one with the same dot has thrown away the only fact the captain wanted.
    private readonly List<(double Bearing, double Range, bool Blob)> _hudBlips = [];
    private readonly List<(double X, double Y, bool Haunted)> _hudMarks = [];
    private readonly List<(double X, double Y, string Counter, bool Dry, bool Firing, double AimX, double AimY)> _hudBots = [];
    private readonly List<(double X, double Y)> _hudHusks = [];
    // #316 law 1, second half · the robbed holes — disturbed ground where a ✗ used to be. The abandoned dry
    // bots are NOT a list of their own: they go into _hudBots as the dry bots they are, so the mark a captain
    // reads is the mark #314 already draws for a sentry at 00.
    private readonly List<(double X, double Y)> _hudPits = [];
    private readonly List<(double X, double Y, bool Hard)> _hudSwept = [];

    // #371 Phase 1 (perf) · the swept-grid draw is bounded. The per-visit probed squares grow toward the
    // whole field's worth of marks if a captain digs the ground out; this caps how many are handed to the
    // renderer each frame. Set far above any realistic visit (tens of probes), so it never trims a mark in
    // normal play — it only stops a pathologically over-probed field from painting an unbounded mark cloud
    // every frame. At that density the omitted squares are visually redundant, so no visible behaviour change.
    private const int MaxSweptDrawn = 256;

    // #338 addendum · the first-contact chirp's edge state (owner: "some kind of sound on the first
    // detected Reever … even if the device is slung the sound would tell that something is up"). The 0→N
    // transition + re-arm hysteresis live in MotionTracker.StepChirp; this is just the carried state,
    // re-armed fresh at every touchdown so the first mover of a new excursion always chirps.
    private MotionTracker.ChirpState _chirp = MotionTracker.ChirpState.Fresh;

    // #379 (owner, Ganymede playtest + Evening wind #18): the per-spell SIGHTING tally. A fresh contact
    // cresting the long ear is a discrete, diminishing jolt (first full, each subsequent within the spell a
    // fraction), resetting after the fan has been quiet a while. Re-armed fresh at every touchdown so the
    // first fright of a new excursion always lands full. The math is NerveModel.AdvanceSightings; this is the
    // carried state, threaded through StepNerve alongside the continuous drain.
    private NerveModel.SightingSpell _sightings = NerveModel.SightingSpell.Fresh;

    // #338 law 1: the tracker HEARS several times farther than the eye sees. The surface camera shows a
    // 64-du-wide field, so the visible half-width is ~32 du; the long ear reaches that × the tunable
    // multiple. Used to gate the first-contact chirp on a contact the tracker can actually hear.
    private const double SurfaceVisualHalfWidthDu = 32.0;

    /// <summary>#591 · HOW FAR THE FAN HEARS FROM WHERE THE CAPTAIN IS STANDING — the ONE number, read by
    /// the chirp, by the nerve, by the sweep and by the draw.
    ///
    /// <para>Owner: <i>"the motion tracker should be in underground visibility mode when we are deeeeeeeep
    /// under surface"</i>. Underground the reach degrades with depth, which gives depth a third cost after
    /// air and time — and the one the player can name.</para>
    ///
    /// <para>It is a method rather than four call sites because those four call sites were already drifting.
    /// <c>DeckView.DrawMotionTracker</c> computed its own reach from the viewport while the sim used a flat
    /// 32 du half-width, so on any window not exactly 64:28 the blip a captain SAW at the rim was not the
    /// blip the chirp had HEARD. That is the sim-says-one-thing-the-drawing-says-another failure this
    /// project keeps paying for, and shortening one of them without the other would have made it load-
    /// bearing. The hud now carries this number and the renderer draws to it.</para></summary>
    private double FanReach() =>
        MotionTracker.UndergroundRange(
            MotionTracker.DetectionRange(SurfaceVisualHalfWidthDu), _surface?.Floor ?? 0);

    /// <summary>#708 · The <c>?dark=1</c> boot cheat: the fixtures are out on every floor this excursion
    /// walks. Never consulted on its own — it is handed to <see cref="UndergroundComplex.IsDark"/>, which is
    /// the only thing in this game allowed to answer the question.</summary>
    private bool _lampsOutCheat;

    /// <summary>#701 · The odd books whose GIST this game-thread has already filed
    /// (<see cref="OddBooks.Entry.Id"/>s). Rides the vault with the rest of the thread's progress, because
    /// the one-shot is about knowledge and knowledge does not un-happen on a reload. Never consulted on its
    /// own — it is handed to <see cref="OddBooks.Search"/>, which is the only thing allowed to answer
    /// whether this reading files anything.</summary>
    private List<string> _oddBooksRead = [];

    /// <summary>#677 · The <c>?found=1</c> boot cheat: park the rock whose site has halls under it, and hand
    /// the captain the paperwork that opens every gate on the way down. It changes nothing Core decides —
    /// the site is seeded off its own body id like every other — so what a tester walks is what a captain
    /// would walk.</summary>
    private bool _foundCheat;

    /// <summary>#693 · The <c>?card=</c> boot cheat: which authority to put in the wallet before the first
    /// ride, or null when unset. <c>next</c> / <c>all</c> / a band index; see the parser in Map.Sim for why
    /// no body id is typed into it.</summary>
    private string? _cardCheat;

    /// <summary>#701 · The <c>?book=</c> dev cheat, null when unset. Never consulted on its own either: it is
    /// an ARGUMENT to <see cref="OddBooks.Search"/> and never a second answer OR-ed in beside it, which is
    /// §13.18's rule and the reason <c>?dark=1</c> did not black out the regolith at noon.</summary>
    private int? _bookCheat;

    /// <summary>#708 · IS THE GROUND UNDER THE CAPTAIN'S BOOTS DARK — the one ask, put once, by everything
    /// here that cares. Today that is the renderer and nothing else: the tracker, the sentries and the pack
    /// keep their own rules and are never told, which is the point — a contact crossing behind you in a hall
    /// your lights cannot reach is the whole feature.</summary>
    private bool DarkHere() =>
        _surface is { } ex && UndergroundComplex.IsDark(ex.Stop.Body.Id, ex.Floor, _lampsOutCheat);

    // #327 the ship calls home: the mothership's station-keeping hold (sim-seconds) at the moment the
    // captain boarded DOWN — the reference the escalating ladder measures against (OrbitHold). Positive
    // = boarded with a real kept-orbit hold; 0 = boarded onto an orbit no one is keeping (a standing red
    // #440 · the first-ground lesson. The bit is per captain and rides in the vault (ProgressSection), so a
    // reload never re-teaches someone who has already walked a moon; the flag below is just whether the card
    // is on screen right now.
    private bool _groundLessonSeen;
    private bool _groundLessonOpen;

    // #563 · the map-just-grew card, same shape: the SEEN bit is per captain and rides in the vault, the
    // OPEN bit is only whether the card is on screen this instant. Fires the first time forcing something
    // open appends real ground to the live plan — the one mechanic in this game nobody would guess exists,
    // and which until now was announced by a toast that faded.
    private bool _groundGrewSeen;
    private bool _groundGrewOpen;

    // #584 · …and WHERE it grew, which the card never said. Composed by GroundGrows.Where out of the floor's
    // own plate, the SDR compass and the fan's own "N du" — no sentence of its own — and set by the one
    // writer every reveal goes through (TheGroundJustGrew). Transient like the OPEN bit: it describes a spot
    // relative to where the captain was standing at the moment the door gave, so it has no business
    // outliving the card that quotes it.
    private string _groundGrewWhere = "";

    // #562 · the tube-feeds-you card, same shape again. Fires the first time the ship racks a magazine while
    // the captain stands in her down-tube — the card that teaches the supply line, not the feature.
    private bool _tubeRearmSeen;
    private bool _tubeRearmOpen;

    // #573 · the tank-is-low card. Same shape again: a persisted seen-bit, a transient open-bit.
    private bool _airCardSeen;
    private bool _airCardOpen;

    // #318 false-hang follow-up: true while the tube + wide-surface plan welds on after 'Board' — the
    // brief synchronous build the loading-style descent door covers (a flying 🛸), so a slow build reads
    // as the shuttle ride, not a frozen click. See BeginSurfaceExcursion.
    private bool _shuttleDescending;

    // #329 follow-up: the coarse descent phase the door narrates RIGHT NOW. The descent runs several
    // first-time synchronous blocks (clock jump, tube/surface/maze weld, first cold render) that each
    // tripped Chrome's page-unresponsive dialog on the Debug bundle; DescentPhaseAsync sets this and
    // yields between them so the door repaints and no single block blocks the main thread too long.
    private string? _descentPhase;

    // #314: the ship's sentry roster — the two real boarding troopers (K-77, R-3B), each with a 99-round
    // magazine that survives a berth-to-berth save (Map.Vault). Full on a fresh ship; drained by use,
    // refilled at a haven's rearm line (Map.Trade). Bots carried down to a surface leave this list for the
    // excursion and return (unless abandoned).
    private readonly List<ShipBot> _shipBots =
        [.. SentryBot.RosterUnits.Select(u => new ShipBot(u, SentryBot.MaxMagazine))];

    public sealed class ShipBot(string unit, int rounds)
    {
        public string Unit { get; } = unit;
        public int Rounds { get; set; } = rounds;
    }
}
