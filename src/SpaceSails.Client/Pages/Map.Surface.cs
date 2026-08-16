using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Surface — the walked surface excursion (#295 walked bury; #313 destination-first). The shuttle
// asks WHERE, not WHY: boarding offers a destination, the tube grows in place, and the captain walks
// down and commits to nothing. Digging is a timed, abortable channel; the Old Ones (Reevers) shamble
// but come in numbers, converging from the edges — the motion tracker is the early warning, the
// crew-only door is the sanctuary, and nothing on the moon ever self-resolves.
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

    private sealed class ShipBot(string unit, int rounds)
    {
        public string Unit { get; } = unit;
        public int Rounds { get; set; } = rounds;
    }

    /// <summary>
    /// #633 · HOW MANY FIGURES A SURFACE PLAN HAS TO BE ABLE TO DRAW, in one place, as the sum of its bands
    /// rather than a number somebody typed. The two branches each maintained this expression separately and
    /// each dropped the other's band: one read <c>3 + ReeverEngineCeiling + MaxCollectors</c>, the other
    /// <c>3 + ReeverEngineCeiling + InspectionTeam.TeamSize</c>. Both were correct on their own branch and
    /// both are wrong now, which is precisely why the sum lives here and every caller reads it.
    ///
    /// <para>The bands, in buffer order: the crew (3), the pack (<see cref="ReeverEngineCeiling"/>), the repo
    /// crew (<see cref="MaxCollectors"/>), the sweep team (<c>InspectionTeam.TeamSize</c>), and #804's
    /// rounds (<see cref="PatrolBand"/>). <see cref="FillSurfaceDroids"/> writes them at exactly these
    /// offsets.</para></summary>
    private const int SurfaceDroidCount =
        3 + ReeverEngineCeiling + MaxCollectors + InspectionTeam.TeamSize + PatrolBand;

    /// <summary>#804 · Where the rounds' slots start. Stated as the sum of every band before it, so a fifth
    /// filler cannot quietly overwrite a fourth — which is precisely the bug #633 paid for.</summary>
    private const int PatrolFirstSlot =
        3 + ReeverEngineCeiling + MaxCollectors + InspectionTeam.TeamSize;

    private sealed class Reever
    {
        public double X, Y, Facing, Vx, Vy;
        public int HitsTaken;   // #314: rounds a sentry has ground into it (downs at RoundsPerReever)

        // Lane-1: a TIDE Reever (clawed up from the deep edge, owner 2026-07-18) versus a dig-roll pack
        // member. The tide holds to its home range (never ventures near the landing); the pack chases to
        // the very crew-only door. Same creature, two leashes.
        public bool Tide;

        // #324: crude line-of-sight memory. A Reever only tracks the captain's LIVE position while it can
        // SEE them (no wall between); blind, it shambles to where it last laid eyes, then leans on the tube
        // choke. Duck behind a wall and it loses your live position — the maze becomes a real instrument.
        public double LastSeenX, LastSeenY;
        public bool EverSeen;

        // Thermal motion (owner, cruise 2026-07-19: "the reevers could be more active, like little thermal
        // motion so they don't just stay still"). A STILL Old One — pinned by a sentry, held at its tide
        // leash, or idling on a stalled chase — shivers around a FIXED anchor instead of standing statue.
        // Idle latches the still state and captures the anchor exactly once, so the mean-zero shuffle
        // (ReeverIdle.JitterAt) never creeps the resting spot; JitterSeed fixes this contact's phase so no
        // two shiver in lockstep. Cleared the frame it makes real progress again (back to a live chase).
        public bool Idle;
        public double AnchorX, AnchorY;
        public ulong JitterSeed;

        // #371 Phase 3 (expedition fog of war): is this Old One drawn on the walked MAP right now? True on
        // open ground the ship overwatches; false behind cover (a wall between it and the captain) on an
        // expedition site — the motion tracker still HEARS it (untouched), so a wall-hidden mover reads only
        // as a blip and, when it slips from sight while moving, leaves a fading echo. Always true off an
        // expedition site (no fog there). Client-only, like the position itself.
        public bool VisibleOnMap = true;

        /// <summary>
        /// #488 · HIBERNATING. Owner: <i>"could we have like slumbering reevers that are not immediately
        /// active but begin to wake up once we board the ship … they would not show on map before they
        /// become active … unless they are within our observed vision space … maybe they can hybernate
        /// somehow the 40 years."</i>
        ///
        /// <para>It is the only honest answer to how anything is still aboard after forty years on a hull
        /// with no air plant and nothing to eat — and it is already the reason the vacuum soak has to be
        /// long: the ENCYSTED kind "has done this before and is in no hurry". Same animal, same trick.</para>
        ///
        /// <para>A dormant one does not move, so it is invisible to a MOTION tracker for free. It is not
        /// drawn either — unless the captain can actually SEE it, which is the moment the lamp finds
        /// something folded in a corner that has not moved in four decades and is about to.</para>
        /// </summary>
        public bool Dormant;

        /// <summary>When this one comes round on its own. Noise aboard pulls it earlier.</summary>
        public double WakeAtMs;

        /// <summary>Where a woken-but-unaware one is currently wandering to aboard a wreck, and until when.
        /// Not a search — it does not know there is anyone to search for.</summary>
        public double ProwlX, ProwlY, ProwlUntilMs;

        /// <summary>How long THIS one has been standing in vacuum. Owner: "I pumped the near hold to vacuum
        /// but there are still reevers in it?" — because the kill only ever fired at the instant a room's
        /// soak completed, so anything already inside, or that walked in afterwards, was untouched and a
        /// room at hard vacuum was scenery. Exposure is per-contact now, and it accrues wherever it stands.</summary>
        public double VacuumSeconds;

        // #453: this contact's own swing clock and swing count. Each Old One winds up separately (so a
        // crowd is not a blender) and each swing seeds its own die, so a long fight never repeats a line.
        public double LastSwingMs = double.NegativeInfinity;
        public int Swings;
    }

    // #314: a sentry on the surface — carried in the sling or deployed and holding the line, with its
    // dwindling magazine. Deployed bots fire the SentryBot volley; a firing bot flags a brief zap line.
    private sealed class SurfaceBot
    {
        public required string Unit { get; init; }
        public int Rounds { get; set; }
        public bool Deployed { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double AimX { get; set; }
        public double AimY { get; set; }
        public double FiringUntilMs { get; set; }

        // #603 · WHAT IS IN IT. Owner: "those rounds might be special types even." A magazine is no longer
        // just a count — it is a count OF SOMETHING, because the lab round clears a line with one shot and
        // will kill you at arm's length, and issue ball does neither.
        public string AmmoId { get; set; } = Core.Ammunition.Issue.Id;
    }

    // The three things a channeled dig can be (beach-comber kit): bury a carried chest where you stand,
    // lift an own cache back up at its ✗, or probe an empty hole to try your luck (the fishing expedition).
    private enum DigKind { Bury, Lift, Probe }

    private sealed class DigChannel
    {
        public double Progress;       // 0..1
        public DigKind Kind;          // bury / lift / probe
        public string? CacheId;       // the cache being lifted (null for a bury or a probe)
        public double AnchorX, AnchorY; // where the shovel bit in — stepping away from HERE aborts, and a
                                        // bury records this spot as the ✗ (free-form, playtest bug #5)
        public int SquareX, SquareY;  // the probe's beach-comber square (unused for bury/lift)
        public ReeverRoll Roll;       // rolled at channel START so the threat can interrupt the bar
        public bool Rolled;           // reevers spawned for this channel
    }

    // #371 Phase 3 · the forced-door channel (owner's "progress bar of forcing a door to open"). Parallel to
    // DigChannel but its own act: several real seconds of shoulder-to-the-door, abortable by stepping away
    // from the door, watched while the away clock ticks (no fresh Reever roll — the site's own diced beats
    // are the threat). On completion the door's REGION APPENDS to the live map.
    private sealed class DoorChannel
    {
        public double Progress;         // 0..1
        public required string DoorId;  // the sealed door being forced (outer or nested)
        public double AnchorX, AnchorY; // the door console — stepping away from HERE aborts
    }

    // #394 · THE DRILLING. The channel that sinks the charge into the rock — parallel to the door-force
    // channel but MUCH longer (DeflectionGig.RockProfile.DrillSeconds, per rock type) and, unlike a door,
    // its Progress PERSISTS across re-channels: a drill-snap complication backs the progress up, and the
    // captain sets the shoulder again from there. Abortable by stepping away from the drill point.
    private sealed class DrillChannel
    {
        public double AnchorX, AnchorY; // the drill point — stepping away from HERE pauses the bore
    }

    // ── #696 · THE DARKROOM CHANNEL ────────────────────────────────────────────────────────────────────
    //
    // Owner: "That is something one would do without using tanked air... we take time to process the loot."
    //
    // The third channel, and deliberately the same shape as the other two: an anchor you have to stand on, a
    // clock, and an effect that fires ONLY at the far end. What makes it different from a dig is that it is
    // silent — a captain photographing a pay sheet is not swinging a shovel — so the teeth are not noise,
    // they are the twenty seconds themselves, watched on the fan.
    //
    // NOTE WHAT IS NOT ON THIS CLASS: anything about air. The hold passes sim time and StepSuitAir prices
    // sim time, and the two never speak. A tank field here would be a second answer to a question that is
    // already answered correctly in one place, on four different kinds of ground (#573/#585/#608/#612).
    private sealed class ProcessingHold
    {
        /// <summary>Which slow thing — the sentences differ, the clock does not.</summary>
        public required Core.Processing.Work Work { get; init; }

        /// <summary>The document under the captain's hands. It is STILL IN THE SATCHEL: nothing is removed
        /// and nothing is filed until the far end, which is what makes "an interruption loses nothing"
        /// structural rather than a promise about tidying up afterwards.</summary>
        public required Core.Satchel.Item Item { get; init; }

        /// <summary>What to call it on screen, composed once at the start, so the abandon line and the start
        /// line cannot disagree about what is in the captain's hands.</summary>
        public required string Label { get; init; }

        /// <summary>Where the boots were when the hold started, and which floor they were on. Drifting off
        /// this spot — or riding the lift — abandons it.</summary>
        public double AnchorX { get; init; }
        public double AnchorY { get; init; }
        public int Floor { get; init; }

        /// <summary>Sim seconds stood so far.</summary>
        public double Elapsed { get; set; }

        /// <summary>#691 · Where the thing is being set down, in the captain's own words, captured at the
        /// START. The captain cannot move during a hold, so this can never go stale — and re-deriving it off
        /// the avatar at the far end would be a second answer to a question already asked.</summary>
        public string Standing { get; init; } = "";

        /// <summary>#603 · What the paper is being read AT, for the clue path. Null on a leave.</summary>
        public (SatchelTry.Target Target, string? Context, string Label)? At { get; init; }
    }

    private sealed class SurfaceExcursion
    {
        public required ShuttleStop Stop { get; init; }
        public required string? RestoreHavenId { get; init; }

        // #320 · WHERE on the body we set down — the chosen landing site (seeded set per body, picked in the
        // boarding panel). Its LayoutSalt parameterizes the surface deck-plan (a different site grows a
        // different ground); its Name rides the surface header. Persists for the whole visit; re-landing
        // re-offers the same seeded set. Defaults to site 0 (the Wild Plain, the canon ground).
        public LandingSite Site { get; init; }
        public int PendingCoin { get; set; }
        public List<CacheCargo> PendingCargo { get; init; } = [];
        public bool ChestDropped { get; set; }
        public double DropX, DropY;
        public bool Buried { get; set; }                 // the carried chest went into the ground
        public DigChannel? Channel { get; set; }

        // #696 · The document under the captain's hands right now, if any. It rides on the EXCURSION and not
        // on the component, so that the one interruption nothing could sensibly listen for — the shuttle
        // lifting — takes the hold with it rather than leaving a clock ticking over an excursion that no
        // longer exists. Never saved: a half-photographed sheet is not a possession.
        public ProcessingHold? Processing { get; set; }

        // Lane-1 · the tide clock (owner, 2026-07-18): the deep hands up a Reever every seeded gap, for
        // the whole excursion, with no fixed total. TideSeconds accrues real time; when it crosses the
        // seeded TideNextGap a Reever claws out and the index advances (which re-seeds the next gap and
        // its spawn x). Pure cadence in ReeverTide; this is just the client's accumulator.
        public double TideSeconds { get; set; }
        public double TideNextGap { get; set; }
        public int TideSpawnIndex { get; set; }
        public bool TideAnnounced { get; set; }          // the one-time "the deep stirs" notice has fired

        // #461 · when the shuttle mated, in surface seconds. The arrival grace is measured off this: a hull
        // setting down is not news to the Old Ones (they take it for one of their own), so nothing may notice
        // the captain until SurfaceArrival.SpotGraceSeconds have passed.
        // #469: REAL-TIME milliseconds (_lastTimestampMs), NOT SimTime. SimTime is the ship's orbital sim
        // clock; standing on a regolith it barely advances, so a grace measured on it never expired — and a
        // never-expiring grace means no Old One may EVER notice the captain. They walked to whatever spot
        // they were born knowing and froze there. The surface's own clock is the rAF one, the same one the
        // swing cooldown, the blood fade and the dig settle already use.
        public double LandedAtMs { get; set; }
        public bool GraceEndedAnnounced { get; set; }
        public bool SentryHintShown { get; set; }         // #380 item 7: the one-time first-deploy sentry hint has fired
        public bool NerveBandDropAnnounced { get; set; }  // #380 item 2: the one-time "nerves fraying" band-drop pulse has fired

        // #649 · The one-per-excursion beat for the moment the monolith stops being a shape and becomes the
        // sky. Separate from _monolithSeen, which is the once-in-a-LIFE nerve hit: the first sight is a
        // milestone and happens once ever, but ARRIVING at the foot of it is worth a line every time you make
        // the walk, and it is the walk the owner wants to be long enough to feel the thing grow.
        public bool MonolithApproachAnnounced { get; set; }

        // #649 · THE WATCH. How long the captain has stood inside the monolith's sight this excursion, and
        // whether the ground has already done its one strange thing. Real-time seconds, like every other
        // surface clock (#469: SimTime is the ship's orbital clock and barely advances on a regolith, so a
        // dwell measured on it would never come due — the bug that froze the Old Ones where they were born).
        //
        // Per EXCURSION, never persisted: this is not a milestone and there is no ledger of it anywhere in
        // the game. Nothing is counting; that is rather the point.
        public double MonolithDwellSeconds { get; set; }
        public bool MonolithWatchSpent { get; set; }

        public ulong ThreatSeed { get; set; }
        public TreasureCache? Cache { get; set; }        // set on a completed bury (for the map card)

        // The per-visit swept grid (owner, 2026-07-18: "some kind of grid system onto planet Miranda for
        // marking the checked squares on that visit"). Every beach-comber square probed THIS excursion,
        // keyed by its integer BeachComber square → what the throw turned up, so the deck-plan can paint a
        // subtle checked/bedrock mark. Client-only and per-visit — a fresh SurfaceExcursion on the next
        // landing starts empty, exactly like the Reever positions (never saved).
        public Dictionary<(int X, int Y), BeachComber.Outcome> Swept { get; } = [];
        public int Catches { get; set; }

        // #453: blows that got PAST the block this excursion. Five and the captain is down (the piracy
        // insurance issues the next one). Per-excursion: you come back down whole, having healed aboard.
        public int HitsTaken { get; set; }
        // #370 · the away-expedition state, live only when this landing is on the gig's site. Expedition
        // gates OFF the endless tide and arms the diced on-site beats (AwayExpeditionEvents). The accruals
        // are settled into the payout on liftoff (ExpeditionReward): the ground-time clock, the last beat
        // ordinal fired, banked discovery bonus, and scientists lost to the dark.
        public bool Expedition { get; init; }
        public double ExpeditionOnSiteSeconds { get; set; }
        public int ExpeditionLastOrdinal { get; set; } = -1;
        public int ExpeditionBonus { get; set; }
        public int ExpeditionScientistsLost { get; set; }
        public bool ExpeditionStrandingFired { get; set; } // the one-time "the window closed" toll has rolled
        public bool ExpeditionRevealFired { get; set; }    // #370: the bigger picture has surfaced (darkens the table, earns the truth bonus)

        // #394 · the away-DEFLECTION state, live only when this landing is on the inbound rock. Like the
        // expedition it gates OFF the endless tide (the horror is the CLOCK, not the pack) and arms the
        // diced complications (DeflectionGig). DrillProgress fills 0→1 as the charge is bored; ChargeArmed
        // when it completes; BurnFired once the ablation charge fires (the rail bends). CrewLost docks the
        // pay. Settled on liftoff (or resolved as an impact if the clock runs out).
        public bool Deflection { get; init; }
        public double DeflectionOnSiteSeconds { get; set; }
        public int DeflectionLastOrdinal { get; set; } = -1;
        public double DrillProgress { get; set; }          // 0..1 — the charge bore (persists across snaps)
        public bool ChargeArmed { get; set; }              // the drill reached depth; the charge is set
        public bool BurnFired { get; set; }                // the ablation charge fired (once)
        public int DeflectionCrewLost { get; set; }
        public bool DeflectionResolved { get; set; }       // the one-time impact/abort resolution has run
        public DrillChannel? DrillChannel { get; set; }

        // #371 Phase 3 · THE DOOR-OPEN DREAM. The forced-door channel and the appended-region state — live
        // ONLY on an expedition excursion. OpenedDoors are every sealed door (outer + nested) forced this
        // visit; LootedCaches every discovery cache claimed. Both key the region compose on a RebuildSurfaceDeck
        // (bury/lift/drop) so a full rebuild replays exactly what the incremental appends grew. Session-only,
        // never saved — a fresh landing starts sealed (same law as the Reever positions).
        public DoorChannel? DoorChannel { get; set; }
        public HashSet<string> OpenedDoors { get; } = [];
        public HashSet<string> LootedCaches { get; } = [];

        // #371 Phase 3 · fog-of-war state (expedition sites only). SeenRegions = every appended region the
        // captain's line of sight has ever reached (stays "explored", drawn dim); VisibleRegions = those in
        // sight right now (drawn lit). Echoes = the fading "movement was here" ripples a contact leaves when it
        // slips behind cover while moving. LastFogCell throttles the region recompute to captain-cell moves.
        public HashSet<string> SeenRegions { get; } = [];
        public HashSet<string> VisibleRegions { get; } = [];
        public List<(double X, double Y, double Born)> Echoes { get; } = [];
        public (int Cx, int Cy)? LastFogCell { get; set; }

        // COMMS-LOSS (owner, cruise 2026-07-19: "loss of comms.. that also is great horror element"). The
        // mothership's telemetry downlink can DEGRADE or DROP for a while, freezing the away HUD's ship-line
        // at "last known" while the suit instruments run on. Pure cadence in CommsLink; these are the client's
        // live accumulator + the active-episode shape + the last-known snapshot the freeze paints from.
        // Client-only, per-visit, never saved (same law as the Reever positions).
        public double CommsSeconds { get; set; }              // on-site seconds — the link's clock
        public double CommsNextOnset { get; set; } = -1;      // CommsSeconds at which the next episode starts (-1 = unscheduled)
        public int CommsOnsetIndex { get; set; }              // the monotonic episode ordinal (seeds the cadence)
        public bool CommsActive { get; set; }                 // an episode is underway right now
        public double CommsEpisodeStart { get; set; }         // its start (CommsSeconds), and…
        public double CommsEpisodeDuration { get; set; }      // …its length, and…
        public bool CommsEpisodeDeepens { get; set; }         // …whether it drops all the way to blackout
        public CommsLink.Phase CommsPhase { get; set; } = CommsLink.Phase.Nominal; // the live phase this frame
        // The last-known mothership readout, snapshotted every frame the link is NOMINAL — what the freeze
        // paints (stale, honestly labelled) while the downlink is down. The TRUE state keeps advancing
        // underneath in the real fields; only this DISPLAY is withheld (the honesty law, see CommsLink).
        public string? CommsLastLine { get; set; }
        public int CommsLastSeverity { get; set; }
        public double CommsLastContactSeconds { get; set; }   // the CommsSeconds of the last nominal contact
        public bool CommsFirstLossAnnounced { get; set; }     // the one-time "static — the feed drops" notice has fired

        // #409 · THE SECRET LAB. Live only when this body hides one of Dr. Vantar's sealed labs. Placement is
        // the seeded hidden-door spot; DoorRevealed once a beach-comber probe pings the right square (or a
        // revisit to an already-found body, or the cheat); Forced once the door is wrenched open (appends the
        // lab region); CacheLooted / LogsRead / RevealFired track the interior. The DoorChannel is the forced-
        // door progress bar (reuses the door-force idiom). Session-only EXCEPT the "found" fact, which persists
        // per game-thread in _secretLabsFound (the vault/thread idiom).
        public SecretLab.Placement? Lab { get; set; }
        public bool SecretLabDoorRevealed { get; set; }
        public bool SecretLabForced { get; set; }

        // #822 · …and whether the crawl at the back of THE HEART has been forced. The fire code's second
        // exit, and it is hidden the way the front door is: nothing marks it, nothing is on the tracker, and
        // it stays a wall until a captain sets their shoulder to it. Session-only, like Forced.
        public bool SecretLabCrawlForced { get; set; }
        public bool SecretLabCacheLooted { get; set; }
        public bool SecretLabRevealFired { get; set; }
        public HashSet<string> SecretLabLogsRead { get; } = [];
        public DoorChannel? SecretLabDoorChannel { get; set; }

        // #563 · The outpost hut on this site, if it has one: where it stands, whether the hatch has been
        // forced this visit, whether its locker and its effects have been taken/read, and the force channel
        // while it is running. Session state — a hut re-seals between excursions, which is honest enough:
        // nobody out here is maintaining a door you levered off its dogs.
        // #564 · THE TANK. Seconds of suit air left, and whether the captain has already been told they
        // crossed the point of no return (the warning is a LINE you cross, said once — not a nag).
        public double AirSeconds { get; set; } = SuitAir.TankSeconds;
        public bool AirWarned { get; set; }

        // #573 · The low-air mark is a SEPARATE warning from the point-of-no-return, because in a bounded
        // field the point-of-no-return can never fire at all and the captain would die having been told
        // nothing. Both are one-shot per walk.
        public bool AirLowWarned { get; set; }

        // #573 · Whether the secondary pack's cut-in has been announced. One-shot, re-armed by a refill.
        public bool ReserveNoted { get; set; }

        // #573 · Whether "you can hear yourself in the helmet" has been said at the current level of
        // distress. Re-arms once the captain calms down, so it marks a CHANGE rather than nagging.
        public bool HardBreathingNoted { get; set; }

        // #573 · The deep shelter's charging rack: one charge per excursion, then it is dry.
        // #573 · Per-shelter state, keyed by index into SurfaceShelter.SpecsFor - a site carries several
        // now. The rack records WHEN it was drawn so it can climb back on its own; the locker is simply
        // spent, because nobody is out here restocking ammunition.
        // #573 · Each rack's reservoir, in suit-seconds. Absent = never visited, so it is full (or partly
        // drawn by somebody else — see SurfaceShelter.SomebodyWasHere). Always producing, never "spent".
        public Dictionary<int, double> ShelterReservoir { get; } = [];
        public HashSet<int> ShelterPumpNoted { get; } = [];

        // #608 · The same three pieces of state for the underground refuges, kept SEPARATE rather than
        // sharing the shelter dictionaries. The two are indexed differently — a shelter is an index into a
        // site's shelter list, a refuge is an index into a FLOOR's — so one dictionary would have B3's
        // refuge and the site's fourth shelter arguing over the same key, and the captain would find a rack
        // mysteriously drawn down by a building they have never been in.
        public Dictionary<int, double> RefugeReservoir { get; } = [];
        public HashSet<int> RefugePumpNoted { get; } = [];
        public bool RefugeBreathNoted { get; set; }

        // ── #585 · THE HIVE. Which floor the captain is on (0 = the surface), and which rooms down there
        //    have already been turned over. Persisted with the excursion, so stepping back into the lift
        //    finds the facility exactly as you left it.
        public int Floor { get; set; }
        public HashSet<int> HiveRoomsEmptied { get; } = [];
        public HashSet<int> HiveFloorsSeen { get; } = [];

        // #803 · …and which of the doors that never open somebody took the hasp off with a sentry
        // (HiveInterior.LockKey). Replayed on every rebuild, exactly the way an emptied room is, so a floor
        // does not grow its wall back while the captain is two rooms away.
        public HashSet<string> LocksShotOpen { get; } = [];

        // #821 · …and which WC cubicles have the catch over (HiveInterior.CubicleKey). Replayed on every
        // rebuild for the very same reason, and kept on the EXCURSION rather than in the vault: a catch is a
        // thing a hand is holding shut, and a save that restored a locked cubicle on a floor the captain is
        // no longer standing in would be the building keeping a secret nobody is behind any more.
        public HashSet<string> CubiclesShut { get; } = [];

        // #821 · How many times a basin has been used this excursion, so two washes are two lines and not
        // one line said twice — the beat the pool is seeded on.
        public int WashBeats { get; set; }

        // #821 · …and whether this watch has already paid its one pip for a wash (CubicleLock.
        // WashPipsPerWatch). A row of four basins is a room, not an income.
        public long WashPaidWatch { get; set; } = long.MinValue;

        // #803 · …and what the shot itself was: the fired-shot facts this ground has heard, in the order
        // they happened. Nothing in this build reads them beyond the field book — the pack's ear is rung by
        // MakeNoise, as it always has been — and #804 prices them.
        public List<GunfireHeard.Shot> ShotsHeard { get; set; } = [];

        // #803 · Whether the captain has been told, once, what a shot indoors actually spends.
        public bool GunfireWarned { get; set; }

        // #688 · WHAT THE CAPTAIN PUT DOWN, AND WHERE. Owner: "no way to drop stuff." Excursion-scoped by
        // deliberate v1 choice — the world does not keep a ledger of every sheet of paper anybody ever set on
        // a floor, and the line the captain reads says as much out loud rather than implying a permanence the
        // sim does not have. Within the walk it is exactly where they left it, which is #615's whole law.
        public LeftBehind Ground { get; } = new();

        // #590 · Which shaft bands this excursion has already talked its way into. Only gates the once-per-
        // shaft beat when a card is accepted; the CARD itself is durable and lives in the vault, because a
        // possession that evaporated when the shuttle lifted would not be a possession.
        public HashSet<int> HiveShaftsOpened { get; } = [];

        // And which have already refused you once. The refusal is said EVERY time — a gate that goes quiet
        // on the second press reads as a broken button — but it is only FILED once, because pressing one
        // gate eleven times is not eleven findings.
        public HashSet<int> HiveShaftsRefused { get; } = [];

        // #609 · Whether this excursion has had the DEAD AIR card. Once: after that the pulse line is
        // enough, because by then it is knowledge rather than news.
        public bool HiveVacuumWarned { get; set; }

        // #592 · Whether this excursion has already had the floor-with-no-plate beat. Once is the whole
        // point: the second time you step out down there it is just a corridor, and it should be.
        public bool HiveUnlistedSeen { get; set; }

        // #725 · Whether this excursion has already had THE PLATE card, and whether it has already had THE
        // STAFF MESS. Two flags in the DEAD AIR family and for its reason: the first time is the find and
        // every time after is a lobby and a canteen, which is exactly what they should become. Excursion-
        // scoped like every one of their siblings above — a captain who lands again is walking in for the
        // first time again, and that is the same ruling the vacuum warning already makes.
        public bool HiveUnlistedPlateShown { get; set; }
        public bool HiveStaffMessShown { get; set; }

        // #751 · …and the two rooms the hall rule adds, in the same family and with the same latch
        // discipline. THE HALL is the B1 cantina walked into for the first time; THE CABINET is ANY of the
        // three doors along its back wall, once TOTAL and never once per door — three identical cards in a
        // row would spend the beat on the second one. The field book's own line about a cabinet files
        // alongside the card, off the same latch, so they can never double up or race.
        public bool HiveCantinaHallShown { get; set; }
        public bool HiveCabinetShown { get; set; }

        // #759 · …and the park behind the hall's glass. ATTENDANCE IS RECORDED is what the plate at the gate
        // says, so the book records that you were there, once, and then the poll stands down. The
        // surveillance is a LINE and not a system — nothing counts anything, and nothing ever refers to it
        // again.
        public bool HiveParkNoteFiled { get; set; }

        // #677 · Whether this excursion has already crossed the seam, and already stepped out into the
        // halls. Two flags and not one, because they are two different events on the same ride and either
        // can happen without the other on a later trip — a captain who rode straight down on a card they
        // were already carrying crosses the seam without the shaft ever having been a mystery.
        public bool HiveSeamCrossed { get; set; }
        public bool HiveFoundSeen { get; set; }

        // #677 · …and whether the wall has already been raised as a card. There are several records in a
        // band and they are all the same wall, so the card and the casebook gist are once per excursion the
        // way the authority card's are — while the POCKET line is said every time, because something goes in
        // every time and #678's law is that a pickup line is printed for something that actually went in.
        public bool HiveHallRecordShown { get; set; }

        // #528 · Whether this excursion has already had the two reveal cards the Hive earns — the sealed way
        // on, and the first authority card. Once each: a card that pops at every sealed door in a corridor
        // of sealed doors is a slideshow, and the second one is never the beat the first one was.
        public bool HiveSealedWayShown { get; set; }
        public bool HiveAuthorityShown { get; set; }

        // #707 · Which amenity rooms this excursion has already written up, by the same room key the haul
        // rooms use. The plate is pulsed every time you stand at the counter — a console that goes silent
        // reads as broken (#212) — but the write-up is filed ONCE, because leaning on a bar eleven times is
        // not eleven findings. Exactly the shape the refused-shaft line above already uses.
        public HashSet<int> HiveAmenitiesRead { get; } = [];

        // #709 · Which of the canteen's regulars this excursion has already heard. Keyed off the same room
        // key the amenities use, offset clear of any real room index, so one person's breath is filed once
        // and the plate pulses on every visit after.
        public HashSet<int> HiveRegularsHeard { get; } = [];

        // #709 · Which notice on the cork board comes next. A counter and not a set, because the board is the
        // one thing down here worth re-reading in ORDER — four notices, one per press, round and round.
        public int HiveBoardNext { get; set; }

        // #709 · WHICH SHIFT the canteen's people are on. Owner: "let's have some random element of who is in
        // the bar and where they got to sit down."
        //
        // Frozen ONCE, when this excursion's underground floor is drawn, and read by everything afterwards.
        // The roster turns over with the watch (PatronRota's own, upstairs) — but the deck is built at one
        // instant and the [E] press happens at another, so reading the clock a second time would let the
        // figure on screen and the person the game answers about be two different people. That is bug class
        // three with a face on it, and a watch chosen once cannot drift into it.
        public long CanteenWatch { get; set; }

        // ── #746 · WHAT HAS HAPPENED AT WHICH TABLE, THIS WATCH ───────────────────────────────────────
        //
        // Owner, 2026-08-06: "asking to sit is missing... offer-a-drink needs to matter."
        //
        // Every one of these is keyed "watch:floor:tableIndex" (see Map.Table.cs's TableKey) rather than by
        // position, because a table has an ordinal and a pair of doubles is a guess. WATCH-SCOPED by design:
        // the shift turning over is the room forgetting, which is what makes a fumbled ask survivable and a
        // bought round worth buying NOW rather than banking.
        //
        // Excursion-scoped like every other Hive flag above. The things that must OUTLIVE the walk — the
        // chit, and the name it was written under — are deliberately not here at all: they are in the
        // satchel, which is durable, and CanteenTable.Cover reads them. A "you have cover" boolean kept
        // beside the possession that IS the cover would be this repo's most expensive bug class with a flag
        // on it.
        public HashSet<string> TableRounds { get; } = [];     // a round was bought at that table
        public HashSet<string> TableMoves { get; } = [];      // "key:who:moveId" — moves already made there
        public HashSet<string> TableAskShut { get; } = [];    // a LOUD file closed ask-about-work there
        public HashSet<string> TableHardened { get; } = [];   // an ask was fumbled there (−1 on the next)

        // …and the three facts the NO-AND scatters across the ROOM rather than across one table. Not keyed
        // by table on purpose: the fitter being worth asking and the temp having overheard are the SCENE
        // moving, and the scene is the room.
        public bool TableFitterOpen { get; set; }
        public bool TableTempOverheard { get; set; }
        public bool TableHouseWays { get; set; }

        // ── #757 · AND WHAT HAS HAPPENED AT A TABLE YOU TOOK ALONE ────────────────────────────────────
        //
        // Owner, live in the hall: "I have empty table but I cannot sit down", and then the sharpening the
        // same evening: "Suppose I just want to sit down and wait to be disturbed?"
        //
        // Same key, same watch scope, same reason as the four sets above. HOW MANY BEATS you have sat
        // through at a top is the ROOM's memory of you rather than the conversation's — standing up and
        // sitting down again must not buy a fresh set of dice, because the approach is seeded on the beat
        // and re-rolling by standing up is exactly the "press it again for a better answer" this game
        // refuses everywhere else.
        public Dictionary<string, int> TableWaits { get; } = [];

        // …and whether somebody has already crossed the room to a given top this watch. ONE approach per
        // table per shift: she came over, and whichever way that went, it went. Waving her off is an
        // answer, not a re-roll.
        public HashSet<string> TableApproached { get; } = [];

        // ── #784 · WHAT THE SHORT REST HAS ALREADY GIVEN BACK, THIS WATCH ─────────────────────────────
        //
        // Owner: "Sitting down relaxes and heals" — and, naming the shape, "it is like short rest in TTRPG",
        // which is bounded recovery and not a tap. ShortRest owns the ceiling; these two are the ledger it
        // is measured against.
        //
        // Keyed on the WATCH and deliberately not on the table, because the cap is a fact about the SHIFT.
        // Keyed by table it would be a cap you could reset by standing up and taking the next top along —
        // the same "press it again for a better answer" #757 closed on the approach roll, wearing a chair.
        public Dictionary<long, int> RestPipsEased { get; } = [];   // nerve pips handed back this watch
        public Dictionary<long, int> RestHitsKnit { get; } = [];    // blows knitted this watch

        // #784 · …and which carried things have been written up PROPERLY (seated, in the captain's own hand)
        // rather than photographed and left. Excursion-scoped like the rest of this block: the BOOK is what
        // is durable, and it keeps the note; this only stops the pen offering to write the same page twice.
        public HashSet<string> WrittenUpProperly { get; } = [];

        // #743/#746 · Whether the staff mess has already had its chit beat. Once per excursion, in the DEAD
        // AIR family: the first time you show a pass to an empty room and eat is the beat, and every time
        // after it is lunch.
        public bool MessChitBeatShown { get; set; }

        // #752 · …and whether the cage's gate has already read it. Same family, same reason: the first time
        // a piece of paper you talked somebody out of gets you through a door is the beat, and every trip
        // after it is the commute. Not keyed by band — the chit opens exactly one gate (Core's rule), so a
        // set of bands would be a set that never holds two things.
        public bool ChitGateBeatShown { get; set; }

        // #588 · Which rooms' kit this excursion has turned up, and whether the person has assembled.
        public HashSet<int> KitPieces { get; } = [];
        public bool DossierShown { get; set; }

        // #585 · This site's shelters, worked out once. See SheltersOn for why this is a field and not a
        // call: the threshold rule asks the question once per hunter per frame, and the answer is fixed for
        // the whole excursion.
        public IReadOnlyList<SurfaceStructure.Spec>? ShelterSpecs { get; set; }

        // ── #583 · THE REPO BOAT. Whether one is coming, when, and what is painted on it — all decided
        //    ONCE, from the heat this captain earned, at the moment the shuttle sets down. ──
        public bool CollectorsComing { get; set; }
        public double CollectorsEtaSeconds { get; set; } = double.PositiveInfinity;
        public string CollectorCallsign { get; set; } = "";
        public bool CollectorsLanded { get; set; }
        public bool CollectorsHailed { get; set; }
        public bool CollectorShelterNoted { get; set; }
        public double CollectorBoatX { get; set; }
        public double CollectorBoatY { get; set; }

        // How long this excursion has been running, in surface seconds. The boat's ETA is measured against
        // it, so the arrival lands MID-MISSION rather than at the hatch.
        public double SecondsOnTheGround { get; set; }

        // #580 · There is deliberately NO locker state here any more. The old HashSet of spent lockers is
        // what stranded the owner beside an empty one; a shelter now reloads whoever reaches it, every time,
        // so there is nothing left to remember. See SurfaceShelter.LockerRounds for the ruling.

        // #573 · Which ruins have been turned over this visit. A room stays entered once emptied — the walls
        // and the door remain, so it still reads as a place you have been.
        public HashSet<string> RuinsSearched { get; } = [];

        // #573 · Whether the "you are breathing shelter air" line has been said for this visit inside. Reset
        // on stepping out, so coming back in says it again — arriving in a refuge is worth noticing twice.
        public bool ShelterBreathNoted { get; set; }

        public SurfaceOutpost.Placement? Outpost { get; set; }
        public bool OutpostForced { get; set; }
        public bool OutpostLooted { get; set; }
        public bool OutpostEffectsRead { get; set; }
        public DoorChannel? OutpostDoorChannel { get; set; }

        public List<SurfaceBot> Bots { get; init; } = [];  // #314: sentries carried + deployed this excursion

        // #562 · The tube rearm in progress: which shouldered bot is being racked, and how far along (0..1).
        // Null whenever nobody is being fed — which is most of the time, including the instant the captain
        // steps out of the tube. Session state only: walking out abandons it, and the rounds already bought
        // are already in the magazine, so there is nothing half-finished to persist.
        public int? RearmBotIndex { get; set; }
        public double RearmProgress { get; set; }
        public List<(double X, double Y)> Husks { get; init; } = [];  // #314: downed Old Ones, left where they fell (#316)
        public double FireTimer { get; set; }              // #314: accrues to the SentryBot fire cadence

        // A chest is in hand right now: something was loaded, not yet buried, not dropped.
        public bool Carrying => (PendingCoin > 0 || PendingCargo.Count > 0) && !Buried && !ChestDropped;
        public bool Channeling => Channel is not null;
        // #371 Phase 3 / #394: any channel underway (a dig, a door-force, OR the drill) — mutually exclusive.
        //
        // #696 · AND THE DARKROOM IS ONE OF THEM. A captain photographing a pay sheet has both hands full;
        // more to the point, all of these draw the SAME progress bar (#562), so two at once would be one bar
        // reporting one of them and the captain watching the wrong clock. The exclusion runs both ways —
        // BeginProcessing refuses while a channel is up, and every [E] that starts a channel already asks
        // this property.
        public bool AnyChannel => Channel is not null || DoorChannel is not null || DrillChannel is not null
            || SecretLabDoorChannel is not null || OutpostDoorChannel is not null || Processing is not null;
    }

    // ── Boarding: pick a surface, optionally load a chest, and grow the tube IN PLACE. ──

    // Destination-first entry (#313). Called from the shuttle bay when the captain chooses a landable
    // surface. The chest is optional cargo already packed by the boarding panel; boarding empty-handed
    // is a complete, valid sightseeing hop. NO teleport: the captain keeps standing at the bay and the
    // down-tube + surface weld on below, so they walk down continuously.
    private async Task BeginSurfaceExcursion(ShuttleStop stop, ShuttleExcursion.ChestLoad chest, int botsToBring = 0, LandingSite? site = null)
    {
        if (_ephemeris is null)
        {
            return;
        }
        // #320: which of the body's seeded landing sites did the captain pick? Default to site 0 (the Wild
        // Plain, the canon ground) when none was chosen — an empty-salt site keeps today's ground exactly.
        LandingSite chosenSite = site ?? LandingSites.At(stop.Body.Id, 0);
        _boardTarget = null;
        _shuttleBayStops = null;

        // #318/#329 follow-up: the descent runs several FIRST-TIME synchronous blocks back to back — the
        // clock jump + buried-cache discovery scan, then the tube/surface/monolith-maze + collision weld,
        // then the first (cold-interpreted) render of the enlarged deck. On the ~100×-slower Debug bundle
        // each can pass Chrome's page-unresponsive threshold, so the owner saw the dialog fire TWICE.
        // Same cure as the boot: raise the flying-🛸 descent door and yield to the browser BETWEEN the
        // coarse phases (each narrated), so no single phase blocks the main thread long enough to trip it.
        // We do NOT restructure any generation logic — only phase-yield around the existing calls.
        _shuttleDescending = true;

        // Phase 1 — clear the bay: advance the clock across the crossing (and the discovery scan the
        // time-jump can trigger for buried caches).
        await DescentPhaseAsync("clearing the bay…");
        AdvanceShuttleClock(stop.TravelSeconds); // the flight down (abstracted by the tube) costs the clock

        // #733 · …and the mothership FLIES that crossing now instead of standing still through it, so a
        // free-flying ship whose track really was diving ends the flight there rather than tunnelling
        // through the rock. Nothing further belongs under a captain who has just been collected by a
        // surface: welding a ground and a tube behind the freeze-frame would be building the wrong scene.
        if (_busted is not null)
        {
            _shuttleDescending = false;
            return;
        }

        // #370: is this landing the away-team's gig site? If so the excursion arms the expedition (no tide,
        // diced beats, the away clock) instead of a normal surface visit.
        bool isExpeditionSite = _expedition is { } plan && plan.SiteBodyId == stop.Body.Id;
        // #394: is this landing the deflection gig's inbound rock? Then the excursion arms the drilling.
        bool isDeflectionRock = _deflection is { } dgig && dgig.RockBodyId == stop.Body.Id;

        var excursion = new SurfaceExcursion
        {
            Stop = stop,
            RestoreHavenId = _dockedHavenId,
            PendingCoin = chest.Coin,
            PendingCargo = [.. chest.Cargo],
            ThreatSeed = ReeverSeed(stop.Body.Id),
            Expedition = isExpeditionSite,
            Deflection = isDeflectionRock,
            Site = chosenSite,
        };

        // #314: pull up to botsToBring sentries off the ship's roster into the sling (carried, not yet
        // deployed). They leave _shipBots for the excursion and return on liftoff unless abandoned.
        int take = Math.Clamp(botsToBring, 0, _shipBots.Count);
        for (int i = 0; i < take; i++)
        {
            ShipBot b = _shipBots[0];
            _shipBots.RemoveAt(0);
            excursion.Bots.Add(new SurfaceBot
            {
                Unit = b.Unit,
                // #728 QA · ?mags=N — the sling comes down holding what the URL asked for. Here, at the one
                // place a magazine crosses into an excursion: the readout, the shelter's receipt and both of
                // the locker's refusals all read this number, so a cheat applied any later would have shown a
                // tester one captain in the instrument and a different one at the press.
                Rounds = _magazineCheat ?? b.Rounds,
                Deployed = false,
            });
        }

        // #784 QA · ?hurt=N — the captain steps out already marked, so the short rest's HEALING half is
        // watchable. Here, at the one place an excursion's blow count begins, and never later: the condition
        // marker, the block roll's modifier stack and the breathing rate all read this number, and a cheat
        // that wrote it after they had started reading would show a tester three different captains.
        if (_hurtCheat is { } blowsAlready)
        {
            excursion.HitsTaken = Math.Clamp(blowsAlready, 0, CaptainCondition.MaxHits - 1);
        }

        _surface = excursion;

        // ── #583 · DOES THE HEAT FOLLOW YOU DOWN? Rolled ONCE, here, off the heat this captain earned and
        //    this excursion's threat seed. Decided at the hatch and never re-rolled, so the answer is a fact
        //    about this trip rather than a die thrown at the player every minute. ──
        _collectors.Clear();
        // Regolith only for now. The owner wants this "on land OR at a ship looting it", and he is right —
        // but a boat cannot set down inside a derelict, so that arrival is a docking and a walk in through
        // somebody else's airlock, which is its own build (#584). Landing a boat on a hull's deck plan would
        // be the geometry lying about the fiction, which is the one bug this project keeps paying for.
        excursion.CollectorsComing = !OnWreck
            && (_collectorCheatSeconds is not null
                || CollectorLanding.WillFollowYouDown(_heat.Level, excursion.ThreatSeed));
        if (excursion.CollectorsComing)
        {
            excursion.CollectorsEtaSeconds = _collectorCheatSeconds
                ?? CollectorLanding.ArrivesAfterSeconds(_heat.Level, excursion.ThreatSeed);
            excursion.CollectorCallsign = CollectorLanding.CallsignFor(excursion.ThreatSeed);
        }

        // #580 · The bird stops mid-sentence as the hatch closes. Anything it was saying was about the ship,
        // and the captain has just stopped being aboard her — leaving the bubble hanging over a moon is the
        // stale half of the same bug. (Everything that would ADD one is gated in SquawkNow.)
        _parrotSquawk = null;

        ResolveSecretLab(excursion); // #409: does this body hide one of Vantar's labs? (seed, or a known/cheat pre-reveal)
        ResolveOutpost(excursion);   // #563: does this SITE carry an outpost hut? (three in four do)
        if (_airCheatSeconds is { } startingAir)
        {
            excursion.AirSeconds = startingAir;   // #564 ?air=N — a short tank, for testing the line
        }
        _reevers.Clear();
        _sweepers.Clear();
        _lastNearestReeverRange = null;
        _chirp = MotionTracker.ChirpState.Fresh; // #338: the long ear starts armed — the first mover chirps
        _sightings = NerveModel.SightingSpell.Fresh; // #379: a fresh watch — the first fright of it lands full

        // #327: snapshot the mothership's hold at the moment of boarding DOWN — the reference the surface
        // ladder erodes against. A kept orbit quotes pulses ÷ Lab-25 trim rate; an unkept one is 0 (the
        // surface then flies a standing "not holding" red). A berthed ship carries no orbit risk (0 too;
        // SurfaceOrbitComms gates it out by _dockedHavenId anyway).
        _orbitHoldAtBoarding = _orbitKept && _dockedHavenId is null
            ? OrbitHold.HoldSeconds(_reactionMassPulses, _keepTrimPulsesPerDay)
            : 0;

        // Phase 2 — weld the tube + wide surface + monolith maze + collision segments onto the deck.
        await DescentPhaseAsync("welding the tube…");
        RebuildSurfaceDeck();

        // Phase 3 — read the ground: flip to the deck view, then paint the FIRST surface frame HERE,
        // under the still-up door, before ever handing control to the live loop.
        await DescentPhaseAsync("reading the ground…");
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;
        _deckPanX = _deckPanY = 0;

        // #348 (owner, 2026-07-18 playtest: "let's also try to fix this timeout … we basically just add
        // dynamically some web-page content … it was just one dialog"). #333 split the descent so no
        // dialog fired TWICE, but ONE remained: the first LIVE deck frame. The renderer batches a whole
        // frame into two interop calls, so DeckView.Draw is almost pure managed work — and its FIRST run
        // for the enlarged regolith (all the wall/maze/HUD paths + the text JSON) is cold-interpreted on
        // the ~100×-slower Debug bundle. The rAF loop fires it as a single un-yielded block the instant
        // _deckMode flips, which is the surviving page-unresponsive dialog. The boot's cure, pointed here:
        // pay that first frame NOW, off the rAF loop, split into its two heavy halves each on its own
        // yield (the surface step, then the paint), so the cold tiering lands in isolated slices the
        // browser breathes between. When the live loop takes over, the paths are warm and the frame cheap.
        await WarmFirstSurfaceFrameAsync();

        StateHasChanged();
        await Task.Delay(1);
        _shuttleDescending = false; // surface welded, walkable, and painted once — drop the descent door
        RendererInterop.PlayCue("board");
        string load = chest.IsEmpty
            ? "Empty sling — a fishing expedition: probe the regolith for shallow treasure (E where you stand)."
            : "A chest rides in the cargo sling — bury it anywhere on the regolith (E where you stand).";
        string bots = take > 0
            ? $" {take} sentry bot{(take == 1 ? "" : "s")} in the sling — press T on the surface to set one down."
            : "";
        if (isExpeditionSite && _expedition is { } gig)
        {
            string who = gig.Flavor == ExpeditionFlavor.Science ? "science team" : "survey crew";
            ShowPulseMessage($"🛸 Shuttle mated to {stop.Body.Name}. The {who} scrambles down the tube and fans out across the site. The ship holds the course-match above — watch the away clock. Walk them through it.");
        }
        else
        {
            ShowPulseMessage($"🛸 Shuttle mated to {stop.Body.Name}. {load}{bots} Walk down the tube. [E] the kiosk, wander, or dig — your call.");
        }
        _descentPhase = null;

        // #461 · the clock the arrival grace is measured off, and the house sentry that makes walking out of
        // the door possible at all (owner: "there should always be one un-paid-for sentry at the door" — he
        // had to spend one of his own just to get clear). It is the shuttle's own fixture: never bought,
        // never counted against the sling, and left behind without a ledger complaint.
        _surface!.LandedAtMs = _lastTimestampMs ?? 0;

        // #488 · A DERELICT GETS THE SAME GUN, and needs it more. Owner: "there might be an infested ship
        // where the cannons are needed also :-D … we should have a cannon in the airlock there to cover the
        // retreat." Same fixture, same law — the shuttle's own, never bought, never dry — but placed on the
        // wreck's spine just inboard of her airlock, so it covers the corridor you will be running back
        // down. On an INFESTED hull that is the difference between a salvage run and a burial.
        if (Derelict.TryParseWreckId(stop.Body.Id, out _))
        {
            _surface!.Bots.Add(new SurfaceBot
            {
                Unit = SurfaceArrival.DoorSentryUnit,
                Rounds = SurfaceArrival.DoorSentryRounds,
                Deployed = true,
                X = WreckLayout.SpawnX + 2,
                Y = WreckLayout.SpawnY,
            });

            // #488: build the valve board for this hull — which rooms the thing got into, and who sealed
            // themselves in where. Seeded off the wreck, so a reload finds the same ship.
            if (_wreck is { } aboardWreck)
            {
                PrepareVenting(aboardWreck);

                // …and whether she is carrying the one warm thing. Seeded off her id, decided ONCE here, so
                // a rebuild of the deck can never roll a node onto a hull that did not have one.
                PrepareArchiveNode(aboardWreck);
            }

            // A fresh boarding is a fresh hull: nothing has woken, the fan has not come up, and the tracker
            // remembers nothing about her yet.
            _anythingHasWokenAboard = false;
            _wreckTrackerLive = false;
            _ghosts.Clear();

            // …and if she is infested, what got in is still aboard. Deep aft, around the nest, already
            // aware — this is the one wreck you read on the way OUT.
            // #488: ?reevers=N works ABOARD now too. The ambush cheat lives further down this method, past
            // the wreck's early return, so it has never once reached a derelict — and it could not have
            // helped if it had: SpawnReevers places its pack in regolith coordinates. Routed to the wreck's
            // own spawner instead, so the owner can dial the hull hot for the fight the airlock gun exists
            // for ("I want to test triggering the reevers :-D").
            // #538 · AND SOMEBODY ELSE MAY ALREADY BE ABOARD. The INSURANCE JOB hosts the sweep team, because
            // her own fiction already says "she was LOST ON PURPOSE… the most valuable thing aboard is the
            // evidence" — so what they came to remove is exactly what the captain came to take. Nothing had to be
            // invented for the owner's "they want to keep their secrets, but the rewards could be big also".
            // #537 · WHAT SHE IS HIDING, decided once, off her id — so a captain who comes back finds the
            // same ship rather than a fresh roll.
            ResolveHullVoid();

            if (_wreck is { Cause: Derelict.WreckCause.InsuranceJob } || _sweepTeamCheat > 0)
            {
                SpawnSweepTeam(_sweepTeamCheat > 0 ? _sweepTeamCheat : InspectionTeam.TeamSize);
            }

            if (_wreck is { Cause: Derelict.WreckCause.Infested } || _reeverAmbushCheat > 0)
            {
                SpawnWreckPack(_reeverAmbushCheat > 0 ? _reeverAmbushCheat : 4);
                ShowPulseMessage(
                    "🕷 Your lamp finds movement deep aft — she is not empty. GATE-1 is live in the airlock behind you. Read what you can and GET OUT.");
                RendererInterop.PlayCue("alarm");
            }
            return;
        }

        _surface!.Bots.Add(new SurfaceBot
        {
            Unit = SurfaceArrival.DoorSentryUnit,
            Rounds = SurfaceArrival.DoorSentryRounds,
            Deployed = true,
            // INSIDE the tube, above the mouth — owner: "inside the tube there is always an unlimited ammo
            // sentry built in… so if a reever tailgates through the door that fixed sentry prevents reever
            // from getting into the shuttle." It covers the threshold from the safe side, so the one that
            // slips in behind you dies in the corridor rather than aboard.
            X = MoonSurface.SpawnX,
            Y = MoonSurface.SurfaceTopY + 2,
        });

        // #458: the ambush cheat. #461 (owner: "That makes no sense… how did they know the shuttle would
        // land just there") — it no longer sets them down ON the pad, which was absurd fiction AND unplayable.
        // They start out in the deep, aware, and COME. That still exercises the chase, the spacing and the
        // exchange in seconds; it just does not pretend the Old Ones knew where the shuttle was going.
        if (_reeverAmbushCheat > 0)
        {
            SpawnReevers(_reeverAmbushCheat);
            ShowPulseMessage($"🧪 DEV: {_reeverAmbushCheat} Old One(s) roused in the deep and inbound — walk down and meet them.");
        }

        // #440 · THE FIRST GROUND (owner, 2026-07-26: "Definitely we need a landing site tutorial also for
        // new captains"). The surface is the only place that can take everything from you in ninety seconds,
        // and it used to explain itself in 10px of dimmed corner text. So the FIRST time a captain's boots
        // touch regolith — after the descent door has dropped and the ground is painted behind it, never
        // over the flying-🛸 door — the lesson goes up: three keys, four laws, nothing else. Once per
        // captain, persisted, then never again (#292: only greet the truly new).
        if (!_groundLessonSeen)
        {
            _groundLessonSeen = true;
            // #448: give the card its own frame. The pulse message above has just queued a render, and
            // raising a full-screen modal in the SAME synchronous stretch chains a second one onto it —
            // exactly the back-to-back blocks #333 broke apart everywhere else in this descent. One yield
            // costs a frame nobody sees and keeps the browser's clock reset between the two.
            await Task.Delay(1);
            _groundLessonOpen = true;
            StateHasChanged();
        }
    }

    // #440: the captain has read the first-ground card. The bit is already set (and saved with the vault),
    // so this only takes the card back down — a reload never re-teaches them.
    //
    // #470: the razor dismisses this through the Dismiss() seam, which hands the keyboard back to the map
    // div afterwards. It matters most here of all: this card is the FIRST thing a new captain ever sees on
    // the ground, and without the way home the tutorial that teaches three keys switched all three off.
    private void CloseGroundLesson()
    {
        _groundLessonOpen = false;
    }

    // ── Liftoff: board the shuttle (player-initiated ONLY — nothing self-resolves). ──

    private void LiftOffFromSurface()
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // #540 · AND THIS IS THE ONE THAT MATTERS. Owner: "its doors open once the warm up is complete", and then
        // the scene it is for — "Under a swarm of reevers with slinged autoguns that wait can feel really long
        // time 😎". Departing the SYSTEM was already gated; the ride HOME from a hull was not, which is exactly
        // where a captain meets the clock: standing at the lock, in the open, waiting to be let in.
        if (!BoatReadyToFly())
        {
            return;
        }

        ex.Channel = null;
        bool escapedWithWatchdogs = _reevers.Count > 0;
        TreasureCache? buried = ex.Cache;
        bool droppedAndLeft = ex.ChestDropped; // read before the excursion (and its dropped pile) is folded away

        // #314: carried sentries come home (with their drained magazines); any left DEPLOYED on the
        // ground is abandoned — a write-off with a ledger line (#119 voice). Retrieve them before liftoff
        // to keep them.
        int abandoned = 0;
        foreach (SurfaceBot b in ex.Bots)
        {
            if (SurfaceArrival.IsDoorSentry(b.Unit))
            {
                continue; // #461: the tube's own gun. Never yours to carry home, never a write-off.
            }
            if (b.Deployed)
            {
                abandoned++;
                LogAutopilotEvent(SentryBot.AbandonLedgerLine(b.Unit, b.Rounds));
            }
            else
            {
                _shipBots.Add(new ShipBot(b.Unit, b.Rounds));
            }
        }

        // #370: an away-team gig settles its payout on the ride home — the fat base plus banked discoveries,
        // docked for any scientist lost to the dark (ExpeditionReward). Narrated, then the gig is closed.
        bool settledExpedition = ex.Expedition && SettleExpedition(ex);
        // #394: lifting off the deflection rock. If the charge fired it settles its heroic pay; if it never
        // fired (an abort), the rock is left on its line — the impact resolves and the port takes it.
        bool settledDeflection = ex.Deflection && SettleDeflection(ex);

        // #583 · IF THEY WERE STILL COMING, YOU GOT AWAY — and the game says so, because an escape that is
        // narrated as nothing is indistinguishable from an escape that never happened. The heat is untouched:
        // outwalking a writ is not settling one, and they know the ship and they will know the next port.
        bool outwalkedTheWrit = ex.CollectorsLanded && _busted is null;

        // #696 · A HOLD THAT THE SHUTTLE ENDS IS STILL AN INTERRUPTION, AND IT IS SAID. Nothing to undo —
        // the sleeve was never emptied and the book was never written in — but a captain who lifted off in
        // the middle of photographing a file has to be told the file is still in their pocket and still
        // unread, or the first thing they do at the desk is look for a gist that was never filed.
        //
        // Only here, and deliberately not on the death paths that also clear _surface: a captain being
        // narrated through the four-stage freeze does not need a line about their paperwork.
        ProcessingIsInterrupted(Core.Processing.Interruption.LiftedOff);

        _surface = null;
        // #612: the next landing works its own air out from scratch, so no crossing line is ever inherited
        // from the last moon.
        _airSupplyNoted = null;
        _reevers.Clear();
        _collectors.Clear();
        _lastNearestReeverRange = null;

        if (outwalkedTheWrit)
        {
            ShowPulseMessage(CollectorLanding.EscapedLine);
        }

        SetDeckForDock(ex.RestoreHavenId); // rebuild the ship/complex; folds the surface away
        (_avatarX, _avatarY, _avatarHeading) = (-6, -6.5, Math.PI / 2); // step off into the bay
        RendererInterop.PlayCue("board");

        string botTail = abandoned > 0
            ? $" {abandoned} sentry bot{(abandoned == 1 ? "" : "s")} left behind — written off."
            : "";

        // #313 · THE CHEST YOU DROPPED AND NEVER WENT BACK FOR. Dropping it (G) says "come back for it when
        // the ground's clear", and inside the excursion that is exactly true — walk over the spot and it is
        // back in the sling. Lift off without it and the ✗-less pile on the regolith is simply gone with the
        // excursion. What the SIM does is the honest news, and it was the one thing never said: nothing went
        // into the ground, so nothing ever left the ship's books — the coin never left the purse, the hold
        // never emptied. Say it, or the captain flies home believing they abandoned a fortune out there.
        string dropTail = droppedAndLeft
            ? " 🧰 You lifted off without the chest you dropped — but nothing went into the ground, so nothing left the books: the coin is still in the purse and the hold is untouched."
            : "";
        if (buried is { } cache)
        {
            _treasureMapCard = cache;
            RendererInterop.PlayCue("reveal");
            string tail = escapedWithWatchdogs
                ? $" {cache.ReeverLevel} Old One(s) haunt this ground now — the best kind of lock."
                : "";
            ShowPulseMessage($"🛸 Lifted off {ex.Stop.Body.Name}. Map filed (🗺).{tail}{botTail}");
        }
        else if (!settledExpedition && !settledDeflection) // an away-gig settle already spoke its payout line
        {
            string tail = escapedWithWatchdogs ? " You outran the Old Ones." : "";
            ShowPulseMessage($"🛸 Back aboard from {ex.Stop.Body.Name}.{tail}{botTail}{dropTail}");
        }
    }

}
