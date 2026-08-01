using SpaceSails.Client.Rendering;
using SpaceSails.Core;

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

    // First sight of the monolith: within this many deck-units of it, the captain lays eyes on the thing
    // (owner's #313 maze). Reaches the maze approach (outer wall ~12 du out) with margin. FLAGGED for tuning.
    private const double MonolithSightRange = 26.0;

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
    private readonly List<(double Bearing, double Range)> _hudBlips = [];
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

    // on the surface). Set in BeginSurfaceExcursion, read by SurfaceOrbitComms.
    private double _orbitHoldAtBoarding;

    // #327: the in-voice orbit line the surface HUD shows — the ship calling down as its hold erodes. The
    // owner's Miranda maroon was LOVED as story; the SILENCE was the bug. While the shuttle is down and
    // the mothership floats FREE (a moon is no dockable berth), the ship reports its hold every tick:
    // steady → slipping → failing → lost, never buried. Null only OFF-surface; on an excursion it always
    // speaks. A docked ship gets its own calm line (#331 follow-up) — the station holds it, no fuel spent
    // — instead of a hold countdown, and the ladder can never fire (this returns before any StageFor).
    private (string Line, int Severity)? SurfaceOrbitComms()
    {
        if (_surface is null)
        {
            return null; // not on a surface — nothing to report
        }

        // #370: on the away-team gig the HUD's ship-line becomes the AWAY CLOCK — time left in shuttle range
        // (owner: "a mission clock at the away site that ticks down the window"). It supersedes the ordinary
        // hold/docked line while the team is on the gig's site.
        if (_surface is { Expedition: true } && ExpeditionComms() is { } away)
        {
            return away;
        }

        // #394: on the deflection rock the ship-line becomes the DOOM CLOCK — T-minus to impact, naming the
        // stakes ("⏱ IMPACT — RINGSIDE EXCHANGE — T-4:32"). It supersedes the ordinary hold/docked line.
        if (_surface is { Deflection: true } && DeflectionComms() is { } doom)
        {
            return doom;
        }

        if (_dockedHavenId is not null)
        {
            // Owner ruling (#331 follow-up): docked at a station, its mass holds the orbit for us — no
            // fuel spent, no hold to count down. Say so plainly rather than a countdown or a false "∞".
            return (OrbitHold.DockedComms, 0);
        }

        if (_orbitKept)
        {
            double remaining = OrbitHold.HoldSeconds(_reactionMassPulses, _keepTrimPulsesPerDay);
            double boarding = _orbitHoldAtBoarding > 0 ? _orbitHoldAtBoarding : remaining;
            OrbitHold.Stage stage = OrbitHold.StageFor(remaining, boarding);
            return (OrbitHold.Comms(stage, remaining), OrbitHold.Severity(stage));
        }

        // Not keeping. If we boarded WITH a hold, the keeper has since given up (the tank ran dry, a loud
        // handback) — the orbit is degrading: the maroon, announced. If we never had a hold, no one was
        // ever trimming it — a standing red the whole excursion. Either way, loud, never silent.
        return _orbitHoldAtBoarding > 0
            ? (OrbitHold.Comms(OrbitHold.Stage.Lost, 0), OrbitHold.Severity(OrbitHold.Stage.Lost))
            : (OrbitHold.NotHoldingComms, 2);
    }

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
        public bool AnyChannel => Channel is not null || DoorChannel is not null || DrillChannel is not null
            || SecretLabDoorChannel is not null || OutpostDoorChannel is not null;
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
            excursion.Bots.Add(new SurfaceBot { Unit = b.Unit, Rounds = b.Rounds, Deployed = false });
        }

        _surface = excursion;
        ResolveSecretLab(excursion); // #409: does this body hide one of Vantar's labs? (seed, or a known/cheat pre-reveal)
        ResolveOutpost(excursion);   // #563: does this SITE carry an outpost hut? (three in four do)
        if (_airCheatSeconds is { } startingAir)
        {
            excursion.AirSeconds = startingAir;   // #564 ?air=N — a short tank, for testing the line
        }
        _reevers.Clear();
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

    // ── #564 · THE TANK. ────────────────────────────────────────────────────────────────────────────────
    //
    // GroundLesson has told every new captain "The walk back is half the tank" since #440, about a resource
    // that did not exist. This is the resource.
    //
    // The rule it is built under: AIR MUST NEVER BE A SILENT TIMER THAT KILLS YOU. So there are three
    // things and not one — a readout that says how much FURTHER you may go (not merely how much is left), a
    // one-time line on the step where you cross the point of no return, and a death that says plainly what
    // happened. A countdown that quietly runs out is the same design failure as an invisible wall.
    private void StepSuitAir(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // Inside the ship or in her tube you are breathing hers, and the tank tops up. This is the ONLY
        // place it refills (bar a cache found out in the world), which is what makes the tube the anchor
        // the whole supply line hangs from (#562).
        if (MoonSurface.IsSafeAboard(_avatarY))
        {
            ex.AirSeconds = SuitAir.Refill(ex.AirSeconds, dtRealSeconds * TubeRefillRate);
            ex.AirWarned = false;   // re-arm the warning: the next walk out gets told again
            return;
        }

        ex.AirSeconds = SuitAir.Drain(ex.AirSeconds, dtRealSeconds);

        double home = DistanceToTheTube();

        // THE LINE. Once, on the step it is crossed, while there is still a decision in it.
        if (!ex.AirWarned && SuitAir.PastPointOfNoReturn(ex.AirSeconds, home))
        {
            ex.AirWarned = true;
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage(SuitAir.CrossingWarning);
        }

        if (ex.AirSeconds <= 0)
        {
            ShowPulseMessage(SuitAir.SuffocationLine);
            // The cause is PASSED, not rolled — see TriggerSurfaceOverdrawDeath. A suffocation narrated as
            // an Old One's hand would be the sim doing one thing and a sentence reporting another.
            TriggerSurfaceOverdrawDeath(ex, nerveRanOut: false, known: DeathCause.Suffocated);
        }
    }

    /// <summary>How far the captain is from the tube mouth — the way home, and the only distance the suit
    /// has any opinion about. A DISTANCE and never a coordinate, so a captain 400 du sideways and one 400 du
    /// deep are priced identically (#453: depth is not a danger gradient).</summary>
    private double DistanceToTheTube()
    {
        double dx = _avatarX - MoonSurface.SpawnX;
        double dy = _avatarY - MoonSurface.SpawnY;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>How fast her tube refills a suit — several times real time, because standing in an airlock
    /// watching a gauge is not the game. Getting home is the achievement; the top-up is a formality.</summary>
    private const double TubeRefillRate = 12.0;

    // ── #562 · THE TUBE REARMS YOU. ────────────────────────────────────────────────────────────────────
    //
    // Owner, playtesting Miranda with both sentries shouldered and dry: "The gun reload at airlock is not
    // working here now… I carry both guns but they are not being reloaded." He was right twice over.
    //
    // The bug: boarding the shuttle REMOVES the bots from _shipBots and puts them in ex.Bots, and they only
    // come back on liftoff. So for the whole excursion the roster is empty, and every rearm affordance — all
    // of which read _shipBots — reported "No bots aboard… they're deployed on a surface, or written off."
    // That is false in the one state it matters: the captain is carrying both of them, shouldered, in his own
    // airlock. Worse, it was a trap. A dry bot could not be fed until liftoff, and the reason you walked back
    // was that it went dry.
    //
    // The fix he asked for: "I expect them to be reloaded at that tube I was at." So the down-tube feeds
    // them — automatically, cheaply, one magazine at a time, with a bar you can watch and a receipt that
    // says what it cost.
    //
    // WHY A PLACE AND NOT A BUTTON — this is the design, in his words: "the reload forces the player to plan
    // their routes … and keep their supply line safe for retreat to reload", and the tube is therefore "the
    // invisible tether to players distance". Every excursion becomes a loop with a known anchor, and the
    // interesting question is how far out you dare go before the walk back costs more than the rounds would.
    // The retreat is the price; the credits deliberately are not (SentryBot.RestockPricePerRound, halved).
    private void StepTubeRearm(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // Standing anywhere but inside the tube ends it. No penalty and nothing lost: rounds already racked
        // are already in the magazine, and the bar simply starts over next time you come back.
        if (!MoonSurface.IsInDownTube(_avatarX, _avatarY))
        {
            ex.RearmBotIndex = null;
            ex.RearmProgress = 0;
            return;
        }

        // Nothing to feed, or nothing to feed it with. Both are quiet — a captain walks through this tube on
        // every single trip, and a tube that nags on the way out would be worse than one that never spoke.
        if (ex.RearmBotIndex is not { } idx)
        {
            idx = NextBotWantingRounds(ex);
            if (idx < 0 || _credits < SentryBot.RestockPricePerRound)
            {
                return;
            }
            ex.RearmBotIndex = idx;
            ex.RearmProgress = 0;
        }

        // The bot may have been planted (or the list rebuilt) since the clock started.
        if (idx >= ex.Bots.Count || ex.Bots[idx].Deployed)
        {
            ex.RearmBotIndex = null;
            ex.RearmProgress = 0;
            return;
        }

        ex.RearmProgress += dtRealSeconds / SentryBot.RearmSecondsPerMagazine;
        if (ex.RearmProgress < 1.0)
        {
            return;
        }

        RackOneMagazine(ex, idx);
        ex.RearmBotIndex = null;
        ex.RearmProgress = 0;
    }

    /// <summary>The first SHOULDERED bot that is short of a full magazine, or -1. Deployed bots are skipped
    /// on purpose: one standing out on the regolith is not in the tube being handed rounds, and pretending
    /// otherwise would be exactly the sim-says-one-thing-sentence-says-another bug this whole lane fixes.
    /// Fills in roster order, one at a time — a magazine is a timer, and one whole timer beats two short.</summary>
    private static int NextBotWantingRounds(SurfaceExcursion ex)
    {
        for (int i = 0; i < ex.Bots.Count; i++)
        {
            if (!ex.Bots[i].Deployed && ex.Bots[i].Rounds < SentryBot.MaxMagazine)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>Rack one magazine as full as the purse allows, spend the credits, and say so. The quote is
    /// the same pure Core law the haven armory uses (<see cref="SentryBot.QuoteRestock"/>) over a one-bot
    /// list — the same price seen from another door, never a second economy.</summary>
    private void RackOneMagazine(SurfaceExcursion ex, int idx)
    {
        SurfaceBot bot = ex.Bots[idx];
        SentryBot.RestockQuote quote = SentryBot.QuoteRestock([bot.Rounds], _credits);
        if (quote.RoundsBought <= 0)
        {
            return; // the purse ran dry between starting the clock and finishing it
        }

        bot.Rounds = quote.Magazines[0];
        _credits -= quote.Cost;
        RendererInterop.PlayCue("board");
        RequestVaultSave();   // rounds and purse both moved — durable before the next thing happens

        // The first time this ever happens to a captain, the card explains the tether. After that the
        // receipt is the right register: you know where the ammo comes from now.
        if (!ShowTubeRearmCardOnce())
        {
            ShowPulseMessage(
                $"🔫 {bot.Unit} racked to {SentryBot.Readout(bot.Rounds)} — {quote.Cost:N0} cr. " +
                (NextBotWantingRounds(ex) >= 0 ? "Feeding the next one." : "Both full. Back out you go."));
        }
    }

    // #563 · The world grew and the captain has read why. Same seam as CloseGroundLesson — Dismiss() hands
    // the keyboard back to the map div, which matters doubly here: this card can open mid-excursion with a
    // pack already walking toward you, and a swallowed keypress would be a death.
    private void CloseGroundGrew()
    {
        _groundGrewOpen = false;
    }

    /// <summary>#563 · Raise the map-just-grew card, but only ever once per captain. Called from every path
    /// that appends real ground to the live plan (a forced expedition door, Vantar's concealed lab door).
    ///
    /// <para>Returns true when the card went up, so the caller can keep its toast for every later time —
    /// the card explains the rule to someone who has never seen it, and the toast is exactly right for
    /// someone who has. Saving immediately is deliberate: the one-time bit must be durable the instant it
    /// is spent, the same habit the convergence reveal uses.</para></summary>
    private bool ShowGroundGrewCardOnce()
    {
        if (_groundGrewSeen)
        {
            return false;
        }
        _groundGrewSeen = true;
        _groundGrewOpen = true;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    // #562 · The captain has read what the tube does. Same Dismiss() seam — the keyboard goes back to the
    // map div, which matters here because the card fires INSIDE the tube, i.e. the moment before a captain
    // means to walk back out into whatever they retreated from.
    private void CloseTubeRearm()
    {
        _tubeRearmOpen = false;
    }

    /// <summary>#562 · Raise the tube-feeds-you card, once per captain ever. Returns true when it went up,
    /// so the caller keeps its receipt line for every later racking. The card teaches the shape of an
    /// excursion — one anchor, plan the route home — and the receipt is right for a captain who knows.</summary>
    private bool ShowTubeRearmCardOnce()
    {
        if (_tubeRearmSeen)
        {
            return false;
        }
        _tubeRearmSeen = true;
        _tubeRearmOpen = true;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    // #329 follow-up: narrate a coarse descent phase and hand the frame back to the browser so the queued
    // render paints (the flying-🛸 door repaints with the new sub-line) before the next synchronous block.
    // Task.Delay(1) parks on a browser timer — the yield that resets Chrome's page-unresponsive timer, so
    // each phase's block is measured on its own and never chains into a multi-second freeze.
    private async Task DescentPhaseAsync(string phase)
    {
        _descentPhase = phase;
        StateHasChanged();
        await Task.Delay(1);
    }

    // #348: pay the first surface frame HERE, under the descent door, so the live rAF loop never has to
    // cold-run it as one long block (the surviving page-unresponsive dialog). Two isolated halves, each
    // fronted by a yield: first StepSurface(0) warms the tide/chase/tracker code without advancing time,
    // then one DrawWalkFrame() paints the enlarged deck once (invisible under the door) to tier up the
    // batched DeckView.Draw + its text JSON. Guarded and try/caught — a warm-up is a nicety, never a
    // thing that may break the walk down; if anything is not ready yet, the live loop simply pays it as
    // before (still just the one dialog we had), so this can only help.
    private async Task WarmFirstSurfaceFrameAsync()
    {
        if (_deckView is null || _renderer is null || _surface is null)
        {
            return;
        }
        try
        {
            _descentPhase = "reading the ground — the sweep…";
            StateHasChanged();
            await Task.Delay(1);
            StepSurface(0); // zero dt: advances nothing, only tiers up the first cold surface step

            _descentPhase = "reading the ground — the ground…";
            StateHasChanged();
            await Task.Delay(1);
            DrawWalkFrame(); // one throwaway paint under the door — warms the cold DeckView.Draw
            await Task.Delay(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"surface warm-up skipped: {ex}");
        }
    }

    // (Re)build the ship + tube + surface plan for the live excursion, honoring what we carry and which
    // of our caches are in this ground. Keeps the avatar where they stand — the world grows, nobody
    // teleports (the #133 "opened wing appears without teleporting anyone" law, pointed downward).
    private void RebuildSurfaceDeck()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        // #488: a DERELICT is not a world. She gets a dead ship to walk — a spine, compartments, and the
        // evidence bolted to her decks — instead of the regolith field and its tube. Routed by body id, the
        // same trick the expedition sites use, so nothing else in the excursion has to know the difference.
        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _) && _wreck is { } aboard)
        {
            _deckPlan = WreckInterior.WreckDeck(
                aboard, _wreckExamined, _wreckSalvaged, 3 + ReeverEngineCeiling, FillSurfaceDroids,
                HeldDoors(), BlockedDoors());
            return;
        }

        _deckPlan = MoonSurface.SurfaceDeck(
            ex.Stop.Body.Id, ex.Stop.Body.Name, OwnCachePositionsAt(ex.Stop.Body.Id),
            3 + ReeverEngineCeiling, FillSurfaceDroids,
            siteSalt: ex.Site.LayoutSalt, siteName: ex.Site.Name); // #320: the picked site seeds the ground + names the header

        // #371 Phase 3: on an expedition site, compose the sealed doors and replay every region already
        // forced open this visit onto the freshly-built base — so a bury/lift/drop rebuild grows back exactly
        // what the incremental door-force appends had. The base build is memoized (Phase 1), so this is one
        // cheap append on top, never a regeneration.
        if (ex.Expedition)
        {
            ComposeExpeditionSite(ex);
        }
        // #394: on the inbound rock, compose the marked DRILL POINT (the channeled charge-bore console).
        if (ex.Deflection)
        {
            ComposeDeflectionSite(ex);
        }
        // #409: on ANY body that hides a lab (expedition deep field or a rare ordinary moon), compose the
        // revealed hidden door and — once forced — replay the appended lab region onto the freshly-built base.
        ComposeSecretLabSite(ex);
        ComposeOutpost(ex);          // #563: the hut — its dogged hatch, or the room once it is forced
    }

    // ✗ marks the REAL spot (playtest bug #5): a free-form bury recorded the actual dug coords, so the
    // mark and the 'dig at the X' console land where the shovel did. A legacy/rumour cache with no stored
    // spot falls back to the deterministic hash-scatter, so every old save still plants a stable ✗.
    private List<(string Id, double X, double Y, int ReeverLevel)> OwnCachePositionsAt(string bodyId)
    {
        var list = new List<(string, double, double, int)>();
        foreach (TreasureCache c in _caches.CachesAt(bodyId))
        {
            if (!c.PlayerOwned)
            {
                continue;
            }
            (double x, double y) = c is { DigX: { } dx, DigY: { } dy }
                ? (dx, dy)
                : MoonSurface.CachePosition(c.Id);
            list.Add((c.Id, x, y, c.ReeverLevel));
        }
        return list;
    }

    // ── Digging [E]: a timed, abortable channel. The 2D6 roll fires at channel START so the pack can turn
    //    out and close on you WHILE the bar fills — the watch is the gameplay. Two entry points now: an own
    //    cache's ✗ console (DigSiteInteract, 'dig at the X'), and the BARE GROUND (SurfaceGroundInteract,
    //    the beach-comber kit — bury a carried chest or probe an empty hole where you stand). ──

    // The ✗ console: 'dig at the X' lifts the own cache nearest this mark. The only surviving dig CONSOLE —
    // free-form burying/probing retired the fixed ⛏ site (they ride SurfaceGroundInteract instead).
    private void DigSiteInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (ex.AnyChannel)
        {
            return; // already channeling (dig or door-force) — stepping away aborts, [E] doesn't re-trigger
        }
        if (DigSettling)
        {
            // #452: you are standing on the ✗ you just made. Lifting it is a real choice, not the next tap.
            ShowPulseMessage("The earth's still settling. Give it a breath before you put a shovel back in.");
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.DigSite } spot)
        {
            return;
        }
        string? nearest = NearestOwnCacheId(ex.Stop.Body.Id, spot.X, spot.Y);
        if (nearest is null)
        {
            ShowPulseMessage("The X is scuffed to nothing — no chest here.");
            return;
        }
        BeginDig(ex, DigKind.Lift, cacheId: nearest, anchorX: spot.X, anchorY: spot.Y);
    }

    // The beach-comber kit's bare-ground [E] (owner, Evening wind 2026-07-18): dig where you STAND. With a
    // chest in the sling this buries it here — bury anywhere; empty-handed it probes a hole to try your luck
    // — a fishing expedition, a first-class trip, never a dead end. Either way the ground must be reasonable
    // regolith (outside the landing band and the walls), and the D100 first decides whether it's diggable at
    // all — some ground is too hard, and the die handles that. Called from the deck E handler when no
    // console is in reach (Map.Deck); a no-op off the surface.
    private void SurfaceGroundInteract()
    {
        if (_surface is not { } ex || ex.AnyChannel)
        {
            return;
        }
        if (DigSettling)
        {
            // #452: the shovel just came out of this ground. A held [E] must not immediately start the next
            // hole — one deliberate press per hole, or the chest goes in and out without you meaning it.
            ShowPulseMessage("The earth's still settling. Give it a breath before you put a shovel back in.");
            return;
        }
        // Safe up in the tube / aboard, or up on the landing band — no digging the fused pad.
        if (!MoonSurface.IsDiggableGround(_avatarX, _avatarY))
        {
            ShowPulseMessage(ex.Carrying
                ? "The landing pad's fused rockcrete — no burying here. Carry it out onto the regolith."
                : "Nothing to probe on the landing pad — it's fused rockcrete. Walk out onto the regolith.");
            return;
        }

        (int sqX, int sqY) = BeachComber.SquareOf(_avatarX, _avatarY);

        // #409: the beach-comber's metal detector screams the instant it sweeps the square that hides a lab
        // door — an INSTANT reveal (no dig), the "ping on the right seeded square". Empty-handed only (the
        // detector is the fishing kit); consumes the E. A near-miss shrieks a proximity hint but still probes.
        if (!ex.Carrying && TrySecretLabDetectorReveal(ex, sqX, sqY))
        {
            return;
        }

        // The die's first job (owner: "some surfaces may be too hard to dig … the die could handle those").
        // Bedrock refuses the dig outright — no hole, no watch — but the square is now KNOWN and joins the
        // swept grid so the sweep reads it as checked.
        Probe probe = BeachComber.Roll(ex.Stop.Body.Id, sqX, sqY);
        if (probe.IsTooHard)
        {
            ex.Swept[(sqX, sqY)] = probe.Outcome;
            string bedrockLabTail = ex.Carrying ? "" : SecretLabProximityTail(ex, sqX, sqY);
            RendererInterop.PlayCue(bedrockLabTail.Length > 0 ? "reveal" : "board");
            ShowPulseMessage((ex.Carrying
                ? "⛏ The shovel rings off bedrock — this square won't take a chest. Try a step over."
                : "⛏ The shovel rings off bedrock a foot down — too hard to dig here. Try another square.") + bedrockLabTail);
            return;
        }

        if (ex.Carrying)
        {
            BeginDig(ex, DigKind.Bury, cacheId: null, anchorX: _avatarX, anchorY: _avatarY);
        }
        else
        {
            BeginDig(ex, DigKind.Probe, cacheId: null, anchorX: _avatarX, anchorY: _avatarY, squareX: sqX, squareY: sqY);
        }
    }

    private string? NearestOwnCacheId(string bodyId, double x, double y)
    {
        string? best = null;
        double bestSq = double.MaxValue;
        foreach ((string id, double cx, double cy, int _) in OwnCachePositionsAt(bodyId))
        {
            double d = (cx - x) * (cx - x) + (cy - y) * (cy - y);
            if (d < bestSq)
            {
                (bestSq, best) = (d, id);
            }
        }
        return best;
    }

    // Start the channel and ROLL THE WATCHDOGS NOW — the pack (if any) turns out at the edges and begins
    // to shamble in while the shovel-bar fills. No modal: the dice reveal rides the pulse line, the grid
    // stays visible so the captain watches the tide. The anchor is where the shovel bit in — stepping away
    // from HERE aborts (no more fixed console to test), and a bury records it as the ✗ (playtest bug #5).
    private void BeginDig(SurfaceExcursion ex, DigKind kind, string? cacheId, double anchorX, double anchorY, int squareX = 0, int squareY = 0)
    {
        int standing = WatchdogLevelAt(ex.Stop.Body.Id);
        ReeverRoll roll = ReeverRaid.Roll(ReeverSeed(ex.Stop.Body.Id), standing);
        ex.Channel = new DigChannel
        {
            Kind = kind, CacheId = cacheId, Roll = roll,
            AnchorX = anchorX, AnchorY = anchorY, SquareX = squareX, SquareY = squareY,
        };
        RendererInterop.PlayCue("board");
        RaiseReevers(roll); // spawn the pack (if roused) so it's already closing during the bar
        ex.Channel.Rolled = true;
        ShowPulseMessage(kind switch
        {
            DigKind.Bury => "⛏ Digging a hole to bury the chest… hold position. Watch the tracker — step away to abort.",
            DigKind.Lift => "⛏ Working the X open… hold position. Step away to abort.",
            _ => "⛏ Sinking a probe hole… hold position. Watch the tracker — step away to abort.",
        });
    }

    // Advance the channel each frame. Stepping off the anchor aborts (chest back in hand, hole abandoned,
    // sprint begins); filling the bar completes the act.
    private void StepDigChannel(double dtRealSeconds)
    {
        if (_surface is not { Channel: { } ch } ex)
        {
            return;
        }
        // Away from where the shovel bit in → abort. (Free-form digs have no console to test, so we hold
        // the captain to the anchor point the dig started at.)
        double dx = _avatarX - ch.AnchorX, dy = _avatarY - ch.AnchorY;
        if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius)
        {
            AbortDig(ex);
            return;
        }

        // #456: the shovel is the loudest thing you choose to do. Every tick of the channel calls anything
        // within earshot to the HOLE — the signature trade of the surface, that the thing worth doing is the
        // thing that announces you. Walls do not muffle it.
        MakeNoise(ch.AnchorX, ch.AnchorY, ReeverHearing.Noise.Digging);

        ch.Progress += dtRealSeconds / DigChannelSeconds;
        if (ch.Progress >= 1.0)
        {
            CompleteDig(ex, ch);
        }
    }

    private void AbortDig(SurfaceExcursion ex)
    {
        DigKind? kind = ex.Channel?.Kind;
        ex.Channel = null;
        if (_reevers.Count == 0)
        {
            ShowPulseMessage("You stop digging. The hole's left half-dug.");
            return;
        }
        ShowPulseMessage(kind switch
        {
            DigKind.Bury => "🩸 You drop the shovel — the hole's abandoned. RUN (or drop the chest: press G).",
            DigKind.Lift => "🩸 You leave the X half-open. RUN.",
            _ => "🩸 You drop the shovel — the probe's abandoned. RUN.",
        });
    }

    // #452 (owner, live 2026-07-27: "it is too easy to bury and dig up by accident now by just pressing down
    // E in sequence"). Burying mints the ✗ AT YOUR FEET, so the instant the shovel goes down you are standing
    // on a dig site — and the very next [E] lifts straight back out what you just spent 3.6 seconds putting
    // in. Hold the key, or tap it twice out of habit, and the ground quietly undoes itself. So a finished
    // dig leaves the earth SETTLING: [E] will not start another one here for a beat, and says why.
    private const double DigSettleSeconds = 2.0;
    private double _digSettleUntilMs = double.NegativeInfinity;

    // True while the last dig is still settling — the guard that makes bury-then-undo a deliberate act.
    private bool DigSettling => (_lastTimestampMs ?? 0) < _digSettleUntilMs;

    private void CompleteDig(SurfaceExcursion ex, DigChannel ch)
    {
        ex.Channel = null;
        _digSettleUntilMs = (_lastTimestampMs ?? 0) + (DigSettleSeconds * 1000.0);
        switch (ch.Kind)
        {
            case DigKind.Bury:
                BuryChestHere(ex, ch.Roll, ch.AnchorX, ch.AnchorY);
                break;
            case DigKind.Lift when ch.CacheId is { } id:
                LiftChestHere(ex, id, ch.Roll);
                break;
            case DigKind.Probe:
                ProbeHere(ex, ch.SquareX, ch.SquareY);
                break;
        }
    }

    // The carried chest goes into the ground AT THE ANCHOR — where the shovel dug, recorded on the cache so
    // the ✗ and 'dig at the X' land exactly there (playtest bug #5, no more hash-scatter). Invisible to
    // confiscation by construction; the presence LEFT on the chest is the pack that turned out (the standing
    // watchdog level, hardened by this roll).
    private void BuryChestHere(SurfaceExcursion ex, ReeverRoll roll, double digX, double digY)
    {
        int coin = Math.Clamp(ex.PendingCoin, 0, _credits);
        _credits -= coin;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();

        int standing = WatchdogLevelAt(ex.Stop.Body.Id);
        int presence = Math.Max(standing, roll.Reevers);
        TreasureCache cache = _caches.Bury(ex.Stop.Body.Id, coin, ex.PendingCargo, SimTime, "you", playerOwned: true, presence, digX, digY);
        SeedDiscoveryWatch();

        ex.Buried = true;
        ex.Cache = cache;
        RebuildSurfaceDeck(); // the chest is down; the new ✗ joins the ground where you dug
        RequestVaultSave();
        // #380 item 6 (owner ruling 2026-07-19: "new players are left mystified") — the discovery risk was
        // taught only at the moment of loss. One line at bury time: rivals may dig it up over the coming
        // days, and Reever-haunted ground keeps it safer.
        ShowPulseMessage($"⛏ Chest buried — {cache.ContentsLine()} off the books. The ✗ marks this spot. Rivals may dig it up over the coming days; the more Reevers haunt this ground, the safer it stays. Now get back to the shuttle.");
    }

    // The beach-comber probe resolves (the fishing expedition's payoff, or its honest shrug). The D100
    // already ruled out bedrock at BeginDig, so this hole turned up either nothing (the common case,
    // "unlucky … but still possible") or a rare shallow find — a little coin and maybe a scrap. Modest by
    // design: luck, never an economy. Either way the square joins the per-visit swept grid.
    private void ProbeHere(SurfaceExcursion ex, int squareX, int squareY)
    {
        Probe probe = BeachComber.Roll(ex.Stop.Body.Id, squareX, squareY);
        ex.Swept[(squareX, squareY)] = probe.Outcome;

        // #411: a rare seeded square on an outer icy moon hides a cold KAAMOS supply pod — a cargo run that
        // never arrived, distinct from ordinary treasure. Sweeping it the first time assembles cold-pod (and
        // may open the reach). Once held, the square is ordinary regolith and the normal probe result stands.
        if (!_kaamos.Has("cold-pod") && KaamosFind.IsColdPodSquare(ex.Stop.Body.Id, squareX, squareY))
        {
            TryAssembleKaamos("cold-pod",
                "❄ Your probe rings off metal a foot down — not a coin, a HULL. You clear the frost and it's a " +
                "SEALED SUPPLY POD, decades cold. " + KaamosLore.ById("cold-pod")!.Lore);
            return;
        }

        // #409: a near-miss on a hidden lab door — the detector shrieks that something big and metal is very
        // close, keep sweeping the squares around here (tacked onto the honest probe result).
        string labTail = SecretLabProximityTail(ex, squareX, squareY);

        if (!probe.IsFind)
        {
            RendererInterop.PlayCue(labTail.Length > 0 ? "reveal" : "board");
            ShowPulseMessage("🕳 Nothing but regolith down there. The detector stays quiet — you mark the square and move on." + labTail);
            return;
        }

        // A shallow find: pocket the coin, and take the scrap if the hold has room (else leave it — a
        // scrap's not worth a sprint). Small numbers on purpose.
        _credits += probe.FindCoin;
        int scrapTaken = 0;
        if (probe.FindScrapUnits > 0 && _cargoUnits < CargoCapacity)
        {
            int take = Math.Min(probe.FindScrapUnits, CargoCapacity - _cargoUnits);
            _cargoUnits += take;
            _cargoValue += take * CargoMarket.UnitValue(BeachComber.FindCargoClass);
            _cargoByClass[BeachComber.FindCargoClass] = _cargoByClass.GetValueOrDefault(BeachComber.FindCargoClass) + take;
            scrapTaken = take;
        }
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        string scrapTail = scrapTaken > 0 ? $" + {scrapTaken} scrap of salvage" : "";
        ShowPulseMessage($"✨ The detector chirps — you turn up {probe.FindCoin:N0} cr{scrapTail} a few inches down. Luck, not a fortune. Mark it and keep moving." + labTail);
    }

    private void LiftChestHere(SurfaceExcursion ex, string cacheId, ReeverRoll roll)
    {
        if (_caches.Dig(cacheId) is not { } c)
        {
            return;
        }
        _credits += c.Coin;
        int unitsBack = 0, unitsLost = 0;
        foreach (CacheCargo line in c.Cargo)
        {
            int room = CargoCapacity - _cargoUnits;
            int take = Math.Min(room, line.Units);
            if (take > 0)
            {
                _cargoUnits += take;
                _cargoValue += take * CargoMarket.UnitValue(line.CargoClass);
                _cargoByClass[line.CargoClass] = _cargoByClass.GetValueOrDefault(line.CargoClass) + take;
                unitsBack += take;
            }
            unitsLost += line.Units - take;
        }
        CompleteFetchCacheFor(c);
        _ = roll; // the pack already turned out at channel start
        RebuildSurfaceDeck(); // the ✗ is gone
        RequestVaultSave();
        string lost = unitsLost > 0 ? $" ({unitsLost}u left — hold full)" : "";
        ShowPulseMessage($"🗺 Dug up {c.Coin:N0} cr + {unitsBack} units{lost}. Back to the shuttle.");
        PayCompletedQuests();
    }

    // The panic choice (owner's unruled carry-speed, settled): DROP the chest to run full speed. The
    // dropped chest stays on the grid to recover (walk back onto it and [E]).
    private void DropChest()
    {
        if (_surface is not { Carrying: true } ex)
        {
            return;
        }
        ex.ChestDropped = true;
        ex.DropX = _avatarX;
        ex.DropY = _avatarY;
        // #456: a chest hitting regolith is one sharp report. You dropped it to run — and the sound tells
        // anything close where you just were, which is exactly the cost of that trade.
        MakeNoise(_avatarX, _avatarY, ReeverHearing.Noise.Clatter);
        if (ex.Channel is not null)
        {
            ex.Channel = null;
        }
        RebuildSurfaceDeck();
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage("🪤 Chest dropped! Full sprint now — come back for it when the ground's clear.");
    }

    private void TryRecoverDroppedChest()
    {
        if (_surface is not { ChestDropped: true } ex)
        {
            return;
        }
        double d = Math.Sqrt((_avatarX - ex.DropX) * (_avatarX - ex.DropX) + (_avatarY - ex.DropY) * (_avatarY - ex.DropY));
        if (d <= DeckPlan.InteractRadius)
        {
            ex.ChestDropped = false;
            RebuildSurfaceDeck();
            RendererInterop.PlayCue("board");
            ShowPulseMessage("🧰 Chest back in the sling.");
        }
    }

    // ── The 2D6 Old Ones: turn out, spawn converging from the edges, and NEVER stop. ──

    private void RaiseReevers(ReeverRoll roll)
    {
        if (!roll.Roused)
        {
            ShowPulseMessage($"🎲 {roll.Describe()} — the ground stays quiet. For now.");
            return;
        }
        SpawnReevers(roll.Reevers);
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage($"🎲 {roll.Describe()} — the OLD ONES stir! {roll.Reevers} shamble up from the regolith, converging. Patient, ancient, and many. Don't get cornered.");
    }

    // Spawn a pack spread across the deep field so they converge from several bearings (not single file)
    // onto the captain and the tube line — the motion-tracker "wall of signal" moment.
    private void SpawnReevers(int count)
    {
        double baseY = Math.Min(_avatarY - 4, MoonSurface.MonolithY + 10);
        for (int i = 0; i < count; i++)
        {
            if (_reevers.Count >= ReeverEngineCeiling)
            {
                break;
            }
            double frac = count > 1 ? i / (double)(count - 1) : 0.5;
            double x = -40 + frac * 70 + (i % 2 == 0 ? -3 : 3);
            double y = baseY - (i % 3) * 4;
            _reevers.Add(new Reever
            {
                X = x, Y = Math.Min(y, MoonSurface.ReeverBarrierY - 1), Facing = Math.PI / 2,
                // Seed the thermal shuffle off the excursion threat seed + the spawn ordinal so each pack
                // member shivers on its own phase (client-only, like the position itself — never saved).
                JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)i + 1UL,

                // #459 (owner, live 2026-07-27: "I did not see any reevers last time… were there any?" —
                // "Not having any is major bug"). THIS pack is roused BY the shovel: the line the player is
                // reading as they spawn literally says they "shamble up from the regolith, CONVERGING".
                // After #446 they were born unaware, so they converged on nothing — they stood where they
                // rose, and standing still they are invisible to a motion-only tracker too. The whole
                // dig-under-threat loop silently became an empty field.
                //
                // They know the DIG, not the captain: LastSeen is the hole, exactly as #456's ear hands out
                // a PLACE rather than a target. Walk away from the noise you made and they still arrive at
                // it. #446's unaware feature is untouched — it governs the Old Ones already standing on the
                // ground when you get there (the tide's), which is the case the owner described.
                EverSeen = true,
                LastSeenX = _avatarX,
                LastSeenY = _avatarY,
            });
        }
    }

    // #464 · LAND ME, ALREADY. Owner, 2026-07-27: "It is not ready until it is playtested in the browser."
    // Every surface playtest began with a two-minute walk from the boot position to the shuttle hatch, and
    // scripted walking wedges on the bay wall often enough that the interesting states (a charge, a blow
    // landing, five and down) were being verified by unit test instead of by eye. So: /map?land=1 rides the
    // shuttle down the moment the world is ready — the REAL BeginSurfaceExcursion, the real descent phases,
    // the real ground. It skips only the walk to the hatch and the boarding panel, nothing that matters.
    private bool _landCheat;

    private async Task AutoLandForCheatAsync()
    {
        if (!_landCheat || _surface is not null)
        {
            return;
        }
        // The same board the hatch would show, so the cheat can never reach somewhere the player could not.
        // #488: when a DERELICT is in reach she wins the toss — ?wreck=1&land=1 is the one-URL way onto her,
        // the same promise ?land=1 makes for a surface. Without this the cheat lands on whatever moon happens
        // to be nearer and the wreck is unreachable except by walking the deck to the shuttle bay.
        List<ShuttleStop> board = [.. ShuttleDestinationsInRange()];
        ShuttleStop? target =
            board.FirstOrDefault(s => s.IsLandable && Derelict.TryParseWreckId(s.Body.Id, out _))
            ?? board.FirstOrDefault(s => s.IsLandable);
        if (target is null)
        {
            ShowPulseMessage("🧪 DEV ?land=1: nothing landable in shuttle reach from this berth.");
            return;
        }
        LandingSite site = LandingSites.For(target.Body.Id)[
            Math.Clamp(_forcedSiteIndex ?? 0, 0, LandingSites.For(target.Body.Id).Count - 1)];
        // Bring the sling down loaded — a cheat that lands you empty-handed made [T] look broken
        // (owner: "why are there no sentries to plant?" / "Button T stopped working?").
        await BeginSurfaceExcursion(target, ShuttleExcursion.Pack(0, _credits, []), botsToBring: 2, site: site);

        // #470: and put the boots OUT ON THE GROUND, not at the tube mouth. The cheat exists so the surface
        // can be playtested at all; landing at the threshold still left a long walk down-field before
        // anything could reach the captain, which is the walk the cheat was invented to remove. Drop them in
        // the open regolith short of the deep field — far enough out that the pack can actually arrive, close
        // enough that the way home is still a real run.
        // #488: a DERELICT has no regolith and no landing band, so the open-ground drop above is meaningless
        // inside her — MoonSurface's coordinates would put the away team OUTSIDE the hull, standing in
        // vacuum next to the ship they came to search. She keeps her own spawn, just inside her airlock.
        if (_surface is { } landed && Derelict.TryParseWreckId(landed.Stop.Body.Id, out _))
        {
            (_avatarX, _avatarY) = (WreckInterior.SpawnX, WreckInterior.SpawnY);
            RebuildSurfaceDeck();
            return;
        }

        if (_surface is not null)
        {
            _avatarX = MoonSurface.SpawnX;
            _avatarY = MoonSurface.LandingBandY - 12;
            RebuildSurfaceDeck();
        }
    }

    // #458: how many Old Ones /map?reevers=N asks for on the first landing. 0 = the cheat is off. They are
    // roused in the DEEP and come to you (#461) — never set down on the landing pad, which read as the Old
    // Ones somehow knowing where the shuttle would touch down.
    private int _reeverAmbushCheat;

    // The surface tick: dig channel, sentries, the chase, and the ambient tide — all cheap, no pathfinding.
    private void StepSurface(double dtRealSeconds)
    {
        if (_surface is null)
        {
            return;
        }

        // #472 · THE WORLD HOLDS STILL WHILE THE CARD IS UP. The first-ground lesson is a full-screen modal:
        // the captain cannot walk, cannot dig, cannot plant. The Old Ones did not care. Landing with a pack
        // out meant they closed, laid hands on and killed the captain BEHIND the card — the tutorial was a
        // death sentence, and the very first thing a new captain ever reads is the last thing they read.
        // Watched it happen live: dismissed the lesson straight onto the WHAT HAPPENED card.
        //
        // So the surface clock stops with the card. Nothing steps, and the arrival grace (#461) is rolled
        // forward by the paused span so the twenty seconds the captain is owed start when they can actually
        // use them — reading the rules must never spend the head start the rules are describing.
        // #563 · The map-just-grew card holds the world for exactly the same reason, and needs it MORE: the
        // lesson at least fires on arrival, inside the #461 grace, while this one fires the instant a door
        // gives — deep in a site, after a five-second channel that anything nearby has had time to walk
        // toward. Reading why the map grew must not be what gets you killed.
        // #562 · The tube-rearm card holds it too. The tube is the safest square on the moon, so this is
        // belt-and-braces rather than a rescue — but a modal that leaves the world running is a bug waiting
        // for the one player who opens it with something already in the tube mouth.
        if (_groundLessonOpen || _groundGrewOpen || _tubeRearmOpen)
        {
            _surface.LandedAtMs += dtRealSeconds * 1000.0;
            return;
        }

        StepSuitAir(dtRealSeconds);     // #564: the tank, the line, and the walk home
        StepTubeRearm(dtRealSeconds);   // #562: the ship feeds your sentries while you stand in her tube
        StepDigChannel(dtRealSeconds);
        AdvanceVacuumClocks(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds)); // #488: the vacuum soak
        AdvancePump(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds));         // #488: the thrifty road
        ServeStandingPumpOrder();                                                   // #488: …and the corridor last
        AdvanceScuttleClock(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds)); // #488: the overload
        AdvanceNests(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds));        // #488: the nest is a source
        AdvanceVacuumExposure(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds)); // #488: vacuum is ground
        CheckVentPayoffUnderfoot();   // #488: the room shows what the vacuum left — when you walk into it
        StepDoorChannel(dtRealSeconds); // #371 Phase 3: the forced-door progress bar
        StepSecretLabDoorChannel(dtRealSeconds); // #409: the hidden lab door's force channel
        StepOutpostDoorChannel(dtRealSeconds);   // #563: the outpost hatch's force channel
        StepDrillChannel(dtRealSeconds); // #394: the drilling — sinking the charge into the rock
        StepSentries(dtRealSeconds);
        StepReevers(dtRealSeconds);
        StepExpeditionFog(dtRealSeconds); // #371 Phase 3: born-dark regions + behind-cover contacts + echoes
        // #370/#394: an away site runs NO endless tide (owner: "not a continuous endless stream like on
        // Miranda"). The expedition's beats may rouse a LIMITED pack; the deflection rock runs the pack OFF
        // entirely (the horror is the clock). The tracker stays live either way.
        if (_surface is { Deflection: true })
        {
            StepDeflection(dtRealSeconds);
        }
        else if (_surface is { Expedition: true })
        {
            StepExpedition(dtRealSeconds);
        }
        else if (!OnWreck)
        {
            StepTide(dtRealSeconds);
        }
        // #488 · A DERELICT RUNS NO TIDE. She is not ground: nothing crawls up out of a steel deck, and
        // SpawnReevers places its pack in REGOLITH coordinates — off the monolith line, against the moon's
        // barrier — so every contact the tide raised aboard her materialised OUTSIDE THE HULL, in space
        // (owner, once they were finally drawable: "now the reevers are outside the ship … they are space
        // reevers :-D").
        //
        // It also matters mechanically, not just visually: her pack is AUTHORED and FINITE (SpawnWreckPack),
        // which is the whole reason venting can clear her. An endless stream would make the vacuum soak,
        // the pump and the airlock gun all pointless — you cannot out-wait a tide.
        StepFirstContactChirp(dtRealSeconds);
        StepComms(dtRealSeconds); // COMMS-LOSS: advance the mothership downlink phase + snapshot the last-known feed
        TryRecoverDroppedChest();
    }

    // ── COMMS-LOSS · the mothership's telemetry downlink (owner, cruise 2026-07-19). ──────────────────
    //
    // THE HONESTY LAW (CommsLink): this loop advances a pure, seeded DISPLAY phase and snapshots the
    // last-known feed. It touches NO game state a consequence rides on — the ship's real orbit hold, the
    // reaction-mass tank, the away/doom clock and everything else keep advancing in their own fields,
    // untouched. All that changes is what the HUD is ALLOWED to show (SurfaceComms). So a blackout can
    // NEVER strand the captain: the truth continues underneath, liftoff stays player-initiated, and on
    // recovery the live true state snaps back with a catch-up pulse. Withheld confirmation, never denied
    // information — the difference between fair dread and a feels-bad bug.
    private void StepComms(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }
        // The link's clock, clamped like the tide's so a background-tab resume can't leap an episode.
        ex.CommsSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);

        ulong seed = ex.ThreatSeed;
        // Schedule the next episode lazily off the current clock. The onset ODDS rise deep in the site /
        // during interference (CommsOnsetBias) — owner: "more likely deep in a site".
        if (!ex.CommsActive && ex.CommsNextOnset < 0)
        {
            ex.CommsNextOnset = ex.CommsSeconds + CommsLink.NextGap(seed, ex.CommsOnsetIndex, CommsOnsetBias());
        }
        // Cross the onset threshold → the episode begins; capture its shape once (deterministic per index).
        if (!ex.CommsActive && ex.CommsSeconds >= ex.CommsNextOnset)
        {
            ex.CommsActive = true;
            ex.CommsEpisodeStart = ex.CommsNextOnset;
            ex.CommsEpisodeDuration = CommsLink.EpisodeDuration(seed, ex.CommsOnsetIndex);
            ex.CommsEpisodeDeepens = CommsLink.EpisodeDeepens(seed, ex.CommsOnsetIndex);
        }

        CommsLink.Phase phase = ex.CommsActive
            ? CommsLink.PhaseAt(ex.CommsEpisodeStart, ex.CommsEpisodeDuration, ex.CommsEpisodeDeepens, ex.CommsSeconds)
            : CommsLink.Phase.Nominal;

        // Snapshot the last-known feed while the link is clean — this is EXACTLY the truth right now, so a
        // later freeze paints an honestly-recent value. The true line is SurfaceOrbitComms (the honest
        // underlying feed); comms-loss never changes it, only whether we're allowed to show it live.
        if (phase == CommsLink.Phase.Nominal)
        {
            if (SurfaceOrbitComms() is { } liveNow)
            {
                ex.CommsLastLine = liveNow.Line;
                ex.CommsLastSeverity = liveNow.Severity;
            }
            ex.CommsLastContactSeconds = ex.CommsSeconds;
        }

        // First-loss teaching notice (once per excursion): the feed just dropped — the frozen readout is
        // stale, the suit instruments still run true.
        if (phase != CommsLink.Phase.Nominal && !ex.CommsFirstLossAnnounced)
        {
            ex.CommsFirstLossAnnounced = true;
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage(CommsLink.FirstLossPulse);
        }

        // Recovery edge: the episode has ended (phase back to Nominal after being active). Fire the
        // catch-up rush against the TRUE current severity — honest, so a hold that went bad while dark is
        // owned out loud, not hidden.
        if (ex.CommsActive && phase == CommsLink.Phase.Nominal)
        {
            ex.CommsActive = false;
            ex.CommsOnsetIndex++;
            ex.CommsNextOnset = -1; // reseed the next quiet gap from here
            int trueSeverity = SurfaceOrbitComms()?.Severity ?? 0;
            // Only speak recovery on the non-away feed (the orbit-hold ladder) — the away/doom clock never
            // went dark (its number stayed live on the suit), so there's nothing to "catch up" there.
            if (ex is not { Expedition: true } and not { Deflection: true })
            {
                RendererInterop.PlayCue("board");
                ShowPulseMessage(CommsLink.RecoveryPulse(trueSeverity));
            }
        }

        ex.CommsPhase = phase;
    }

    // The onset odds multiplier: the link is strong at the ship (a drop there would be silly), and drops
    // grow likelier the deeper the captain wanders into the site (owner: "more likely deep in a site,
    // during solar interference"). 1× at the tube mouth, up to ~2× deep by the monolith.
    private double CommsOnsetBias()
    {
        if (MoonSurface.IsSafeAboard(_avatarY))
        {
            return 0.5; // basically at the ship — the downlink is solid
        }
        double top = MoonSurface.SurfaceTopY;
        double deep = MoonSurface.MonolithY;
        double span = top - deep;
        double depth = span > 0 ? Math.Clamp((top - _avatarY) / span, 0.0, 1.0) : 0.0;
        return 1.0 + depth;
    }

    // A scripted onset (a bad expedition beat, solar interference): if no episode is underway, pull the
    // next one forward to NOW. Pure schedule nudge — it changes WHEN the display gate closes, never the
    // ship's real state, so the honesty law holds untouched.
    private void TriggerCommsEpisode()
    {
        if (_surface is { CommsActive: false } ex)
        {
            ex.CommsNextOnset = ex.CommsSeconds;
        }
    }

    // COMMS-LOSS · the DISPLAY gate over the honest feed. Wraps SurfaceOrbitComms (the true, always-honest
    // mothership line) with the live link phase, returning what the HUD is allowed to show plus the comms
    // state for the renderer's static/greyed treatment. NEVER alters a hard deadline the captain reckons
    // locally: on an away/deflection gig the numeric clock is the SUIT's own count (not a downlink), so it
    // stays live and honest and is only TAGGED unconfirmed; only the orbit-hold ladder (the ship's own
    // telemetry) freezes at last-known. Returns null off-surface, exactly like SurfaceOrbitComms.
    private (string Line, int Severity, int CommsState)? SurfaceComms()
    {
        if (_surface is not { } ex)
        {
            return null;
        }
        if (SurfaceOrbitComms() is not { } live)
        {
            return null;
        }
        CommsLink.Phase phase = ex.CommsPhase;
        if (phase == CommsLink.Phase.Nominal)
        {
            return (live.Line, live.Severity, 0);
        }

        // The away/doom clock is the suit's own reckoning, a hard deadline whose closing costs crew — it
        // must NEVER be withheld (that would strand unfairly). Keep the live number, its severity AND its
        // normal colour (CommsState 0 — an honest instrument must never LOOK lost); only append the text
        // tag flagging that the ship can no longer confirm it. Honest by construction.
        if (ex is { Expedition: true } or { Deflection: true })
        {
            return (live.Line + CommsLink.UnconfirmedTag(phase), live.Severity, 0);
        }

        // The orbit-hold ladder IS the mothership's downlink — freeze it at the last-known value, banner it
        // as stale (how long since contact), and carry the LAST-KNOWN severity (not the true one — we can't
        // hear the true one). The true orbit keeps eroding underneath; recovery reveals it.
        string frozen = ex.CommsLastLine ?? live.Line;
        int frozenSeverity = ex.CommsLastLine is null ? live.Severity : ex.CommsLastSeverity;
        double since = Math.Max(0.0, ex.CommsSeconds - ex.CommsLastContactSeconds);
        return (CommsLink.StaleBanner(phase, since) + frozen, frozenSeverity, (int)phase);
    }

    // #338 addendum · THE GAME'S FIRST SOUND: chirp on the tracker's first-contact edge. Counts the movers
    // the long ear actually HEARS this frame (within detection range), advances the pure edge/hysteresis in
    // MotionTracker.StepChirp, and plays the two-tone radar ping on the 0→N transition. Sound only — the
    // fan and the existing tide/raise notices carry the words; this is the "device chirps in the holster"
    // that makes you look even when the device is slung. Muting is a JS-side master switch (respected there).
    private void StepFirstContactChirp(double dtRealSeconds)
    {
        if (_surface is null)
        {
            return;
        }
        double detection = MotionTracker.DetectionRange(SurfaceVisualHalfWidthDu);
        var entities = _reevers.Select(r => new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy));
        int heard = MotionTracker.DetectedMovingCount(_avatarX, _avatarY, entities, detection);
        (_chirp, bool chirp) = MotionTracker.StepChirp(_chirp, heard, dtRealSeconds);
        if (chirp)
        {
            RendererInterop.PlayChirp();
        }
    }

    // ── #317 The nerve gauge: the regolith frays it, the ship's safety eases it, the monolith gores it. ──

    // The one per-frame nerve advance, called from the sim loop every tick (not just on the surface): the
    // pure NerveModel.Advance owns the whole on-planet law — drain only out on the regolith (moving contacts,
    // a live chase, digging under threat, being cornered), the once-in-a-life monolith first-sight hit (the
    // #226 hook #318 named), and the airlock/off-planet ease-off (the ship is safety). The client's only job
    // is to read the live situation and, when the big hit fires, sound the cue and speak.
    private void StepNerve(double dtRealSeconds)
    {
        bool onExcursion = _surface is { } ex;
        bool onRegolith = onExcursion && !MoonSurface.IsSafeAboard(_avatarY);

        // #380 item 2: the band this frame opened on — so once, per excursion, we can speak the FIRST slide
        // down a rung (naming the cause and the remedy the bare gauge never did). Recovery only ever raises
        // the nerve, so a fall can arise solely from the regolith's toll below.
        NerveModel.NerveBand bandBefore = NerveModel.BandFor(_nerve);

        // #379 · the per-spell sighting tally still lives here (the tracker's own hearing decides what counts
        // as a fresh contact); #480 prices the result in whole pips instead of a shaped float.
        int heardMovers = 0;
        if (onRegolith && _surface is not null)
        {
            // #446: the tracker's fan still HEARS to its full detection range — that far, faint blip is the
            // whole point of the instrument. But a contact only FRIGHTENS you inside the dread range.
            double detection = Math.Min(
                MotionTracker.DetectionRange(SurfaceVisualHalfWidthDu), NerveModel.DreadRangeDeckUnits);
            var ents = _reevers.Select(r => new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy));
            heardMovers = MotionTracker.DetectedMovingCount(_avatarX, _avatarY, ents, detection);
        }
        // #480: charge ONLY the first fright of a spell. AdvanceSightings reports a fresh contact on every
        // RISE in the heard count, and with a pack weaving in and out of the dread range that rises over and
        // over — playtested as "something crests the tracker −1" four times in eight seconds, which is a
        // repeat-tax and exactly what the owner ruled against. `Seen == 0` is the spell's first fright; the
        // rest of the watch is free until the tracker has been quiet long enough to re-arm it.
        bool firstFrightOfSpell = _sightings.Seen == 0;
        (NerveModel.SightingSpell nextSpell, int freshSightings) =
            NerveModel.AdvanceSightings(_sightings, heardMovers, dtRealSeconds);
        _sightings = nextSpell;

        var frame = new NervePips.Frame(
            OnExcursion: onExcursion,
            OnRegolith: onRegolith,
            SeesMonolith: onRegolith && SeesMonolith(),
            // #446 (owner, live 2026-07-26: "The reevers should not lower sanity unless they get REALLY
            // close"). ChaseActive used to be the bare `_reevers.Count > 0` — a pack EXISTING anywhere on
            // the field, so one Old One drifting on the far rim taxed the captain at the same flat rate as
            // one at their shoulder, and the gauge bottomed out before anything ever reached them. Now we
            // hand Core the distance to the nearest hunter and it prices the dread off that; the moving
            // count is likewise only the ones near enough to matter, so a far-off tide is atmosphere.
            Stressors: onRegolith
                ? new NerveModel.Stressors(
                    CountMovingReeversWithin(NerveModel.DreadRangeDeckUnits),
                    _reevers.Count > 0,
                    _surface!.Channeling,
                    IsCornered(),
                    NearestReeverRange())
                : default,
            FreshSightings: onRegolith && firstFrightOfSpell ? freshSightings : 0,
            Touched: _touchedThisFrame,
            DtSeconds: dtRealSeconds,
            // #480 · fear tracks MORTAL DANGER: below a couple of blows left, every further hand costs its
            // pip again instead of being absorbed by the once-per-encounter latch.
            HealthPipsLeft: _surface is { } hurt ? CaptainCondition.MaxHits - hurt.HitsTaken : int.MaxValue);

        NervePips.Step step = NervePips.Advance(_nerve, _monolithSeen, _nerveBeats, in frame);
        bool monolithFired = !_monolithSeen && step.MonolithSeen;
        _nerve = step.Nerve;
        _monolithSeen = step.MonolithSeen;
        _nerveBeats = step.Beats;
        _touchedThisFrame = false;

        // THE DELIVERABLE OF #480: every pip that moved says why — a line by the gauge in the moment, and a
        // bounded ledger on the Captain desk that can be read back afterwards (and by the death card).
        if (step.Events.Count > 0)
        {
            _nerveLedger = NervePips.Record(_nerveLedger, step.Events);
            FlashNerve(step.Events[^1]);
        }

        if (monolithFired)
        {
            RendererInterop.PlayCue("alarm");
            // #380 item 8: name the bill the shock just dealt — the poetic beat and the NERVE gauge shake hands.
            ShowPulseMessage("👁 The monolith resolves out of the dark — too regular, too old, too patient. Something behind your eyes lurches — your nerve takes the hit.");
            RequestVaultSave();
            // #400 §3: first human eyes on the monolith — the once-in-a-life beat offers a shot for the record.
            // The backdrop the captain's portrait composites onto. This beat shipped with NO vista at all,
            // so the marquee once-in-a-life shot was a portrait disc floating on an empty stage — owner,
            // 2026-07-28: "kind of lame". It now poses against the canon ground doing what the canon ground
            // does: the monolith behind, the pack closing, and GATE-1 firing over your shoulder.
            OfferSelfie(SelfieBeats.FirstMonolith, "art/selfie-monolith.jpg");
        }

        // #380 item 2: the one-per-excursion band-drop pulse. The first time this frame's toll drops the nerve
        // a whole rung (Steady→Rattled, or lower), say WHY it falls and HOW to mend it — the cause+remedy the
        // bare gauge never showed. Latched on the excursion (a fresh landing re-arms it), the house one-time idiom.
        if (onExcursion && _surface is { NerveBandDropAnnounced: false } dropEx
            && NerveModel.BandFor(_nerve) > bandBefore)
        {
            dropEx.NerveBandDropAnnounced = true;
            ShowPulseMessage("Nerves fraying — Reevers, digging under threat, and worse all take their toll. Get back aboard to steady them.");
        }
    }

    // #480 · Say it, then keep it. The flash is the in-the-moment cause ("it laid hands on you  −1") that
    // hangs by the gauge for a beat; the ledger is the same line kept so the Captain desk — and the death
    // card — can answer "what broke me?" after the fact.
    private void FlashNerve(NervePips.Event e)
    {
        _nerveFlash = e.Line;
        _nerveFlashUntilMs = (_lastTimestampMs ?? 0) + NerveFlashMs;
    }

    /// <summary>The ONE way anything outside the regolith law may move the nerve (#480). Takes the old
    /// storage-scale amount and a plain-words label, banks anything under a whole pip, and — when a pip
    /// actually moves — flashes it and files it in the ledger. Nothing may change the gauge anonymously:
    /// if a caller cannot name its shock in the house voice, it has no business spending the captain's nerve.
    /// </summary>
    private void ApplyNerveShock(double rawAmount, string label)
    {
        (double nerve, double carry, NervePips.Event? e) =
            NervePips.ApplyShock(_nerve, _nerveShockCarry, rawAmount, label);
        _nerve = nerve;
        _nerveShockCarry = carry;
        if (e is { } fired)
        {
            _nerveLedger = NervePips.Record(_nerveLedger, [fired]);
            FlashNerve(fired);
        }
    }

    /// <summary>The relief seam's counterpart (#308/#321 → #480): a drink, a pill, a bunk or a shared glass
    /// gives WHOLE pips back and says so, so a recovery is exactly as legible as a loss.</summary>
    private void ApplyNerveRelief(double rawRestore)
    {
        (double nerve, NervePips.Event? e) = NervePips.ApplyRelief(_nerve, rawRestore);
        _nerve = nerve;
        if (e is { } fired)
        {
            _nerveLedger = NervePips.Record(_nerveLedger, [fired]);
            FlashNerve(fired);
        }
    }

    /// <summary>The flash line, while it is still fresh — what the gauge writes beside the pips.</summary>
    private string? LiveNerveFlash =>
        _nerveFlash is not null && (_lastTimestampMs ?? 0) < _nerveFlashUntilMs ? _nerveFlash : null;

    /// <summary>The ledger as plain lines for the corner — newest first.</summary>
    private IReadOnlyList<string>? NerveLedgerLines =>
        _nerveLedger.Count == 0 ? null : _nerveLedger.Select(e => e.Line).ToList();

    /// <summary>The DEAD captain's ledger, snapshotted at the rebirth seam so the death card can answer
    /// "what broke you?" after the live one has been handed clean to the new captain.</summary>
    private IReadOnlyList<NervePips.Event> _deathNerveLedger = [];

    /// <summary>Those same events as lines, for the death card.</summary>
    private IReadOnlyList<string>? DeathNerveLedgerLines =>
        _deathNerveLedger.Count == 0 ? null : _deathNerveLedger.Select(e => e.Line).ToList();

    private int CountMovingReevers()
    {
        int n = 0;
        foreach (Reever r in _reevers)
        {
            if (MotionTracker.IsMoving(r.Vx, r.Vy))
            {
                n++;
            }
        }
        return n;
    }

    // #456 · A NOISE ON THE GROUND. Owner, 2026-07-27: "they can hear digging etc loud noises, but generally
    // they have to spot you by hearing or by seeing before they give chase… when they are initially behind
    // obstructions except maybe one or two they do not participate in chasing you if they don't know where
    // you are." This is that ear, and it is what keeps the un-leashed pack (#453) fair.
    //
    // What a Reever gets from a sound is a PLACE, not a target: it learns where the noise came from and goes
    // to look. Hearing ignores walls on purpose — stone hides you from eyes, never from ears — so digging
    // behind a monolith buys sight-cover and nothing else. Move after making noise and they converge on an
    // empty hole, which is a real tactic.
    private void MakeNoise(double x, double y, ReeverHearing.Noise noise)
    {
        if (_surface is not { } ex)
        {
            return;
        }
        // #461: the arrival grace covers the EAR too, or the first shovel-stroke would undo it.
        if (!SurfaceArrival.CanBeSpotted(((_lastTimestampMs ?? 0) - ex.LandedAtMs) / 1000.0))
        {
            return;
        }
        double reachSq = ReeverHearing.RangeOf(noise) * ReeverHearing.RangeOf(noise);
        foreach (Reever r in _reevers)
        {
            double dx = r.X - x, dy = r.Y - y;
            if ((dx * dx) + (dy * dy) > reachSq)
            {
                continue; // too far to have heard it — it keeps its ground (#446's feature)
            }
            // It heard SOMETHING, and now it knows a spot worth walking to. If the captain is still there
            // when it arrives it sees them the honest way; if not, the trail leads to a hole in the ground.
            r.LastSeenX = x;
            r.LastSeenY = y;
            r.EverSeen = true;
        }
    }

    // #465 · A SHUT DOOR IS OPAQUE. Owner, 2026-07-27: "the gun would be behind one door and not shooting
    // through it." Doors are not collision segments — the passage is always walkable, by law — so they never
    // entered the sight test, and the tube's built-in gun happily shot straight through a closed airlock.
    //
    // Opacity and solidity are NOT the same property (this is exactly the distinction #442 is about): a shut
    // door stops the eye and the round while never stopping the captain's boots. So sight queries get the
    // walls PLUS whatever doors are shut this instant, and collision keeps getting the walls alone.
    private readonly List<SurfaceCollision.Segment> _sightBlockers = [];

    private IReadOnlyList<SurfaceCollision.Segment> SightBlockers()
    {
        _sightBlockers.Clear();
        foreach (SurfaceCollision.Segment seg in _deckPlan.CollisionSegments)
        {
            _sightBlockers.Add(seg);
        }
        foreach (DeckPlan.Door d in _deckPlan.Doors)
        {
            if (!IsDoorShut(d))
            {
                continue; // standing open — you can see (and shoot) straight down the tube
            }
            _sightBlockers.Add(new SurfaceCollision.Segment(d.X1, d.Y1, d.X2, d.Y2));
        }
        return _sightBlockers;
    }

    // The same rule DeckView draws with (Core Airlock), so what blocks a shot is exactly what the player
    // sees closed — one door open at a time, the far end of an interlocked tube always shut.
    private bool IsDoorShut(DeckPlan.Door d)
    {
        if (d.Locked)
        {
            return true;
        }
        double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
        double toDoor = Math.Sqrt(((_avatarX - mx) * (_avatarX - mx)) + ((_avatarY - my) * (_avatarY - my)));

        // ONE RULE, AND IT IS THE ONE THE PLAYER CAN SEE. I briefly opened doors here for Reevers too, on
        // the owner's "unlocked doors should open for reevers" — and it broke the invariant this method
        // exists to hold, stated in the comment above it: the RENDERER decides a door is open from the
        // CAPTAIN's distance and nothing else. Adding a second opener here made the sim treat a door as
        // open while the deck drew it shut, so a gun fired through a door the player could see was closed
        // (owner, twice: "a reever was shot through a closed door").
        //
        // What blocks a shot must be exactly what the player sees closed. If Reevers are ever to work
        // doors, the RENDERER has to learn it at the same moment — one source of truth or none.
        double nearestPartner = double.PositiveInfinity;
        if (d.Interlock != 0)
        {
            foreach (DeckPlan.Door other in _deckPlan.Doors)
            {
                if (other.Interlock != d.Interlock || other.Locked || other.Equals(d))
                {
                    continue;
                }
                double ox = (other.X1 + other.X2) / 2.0, oy = (other.Y1 + other.Y2) / 2.0;
                nearestPartner = Math.Min(nearestPartner,
                    Math.Sqrt(((_avatarX - ox) * (_avatarX - ox)) + ((_avatarY - oy) * (_avatarY - oy))));
            }
        }
        return !Airlock.MayOpen(toDoor, nearestPartner, DeckPlan.DoorOpenRadius);
    }

    // #446: the movers CLOSE ENOUGH TO FRIGHTEN — the same count, fenced to the dread range. The tracker
    // still hears every mover on the field (its fan is untouched, and a far blip is exactly the dread the
    // fan is for); this is only what the nerve is priced from, so a hunter you have time to walk away from
    // costs nothing. It also feeds the sighting spell, so a dot on the far rim no longer lands a jolt.
    private int CountMovingReeversWithin(double range)
    {
        double r2 = range * range;
        int n = 0;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - _avatarX, dy = r.Y - _avatarY;
            if (MotionTracker.IsMoving(r.Vx, r.Vy) && (dx * dx) + (dy * dy) <= r2)
            {
                n++;
            }
        }
        return n;
    }

    // #446: how far off the nearest Old One is, in deck units — infinity on an empty ground. Core prices the
    // whole sustained dread through this one number (NerveModel.Dread).
    private double NearestReeverRange()
    {
        double best = double.PositiveInfinity;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - _avatarX, dy = r.Y - _avatarY;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < best)
            {
                best = d2;
            }
        }
        return double.IsPositiveInfinity(best) ? best : Math.Sqrt(best);
    }

    // A net between the captain and the tube: an Old One wedged up-field (nearer the tube mouth than the
    // captain) and laterally close enough to block the sprint. Cheap geometry, matching the encirclement
    // the pack already leans into — the "cornered" the owner named, priced as a stressor.
    // #475 · CORNERED HAS TO MEAN CORNERED. Core prices this as "a net wedged between the captain and the
    // tube mouth" and charges the sharpest routine drain in the game for it — 5.0/s, more than a full-contact
    // chase — deliberately NOT discounted by range, because being cut off is not a distance term
    // (NerveModelTests.BeingCornered_IsCloseByDefinition_AndIsNeverDiscountedByRange pins that on purpose).
    //
    // The law was right; this predicate was not keeping its side of the bargain. It asked only for a contact
    // somewhere ABOVE the captain in a lateral lane, with no bound on how far above — so a single Old One
    // drifting forty deck units up, nowhere near anything, read as a net and billed the full 5.0/s. Three
    // captains in a row died on that: full gauge, never touched, killed by a dot on the far rim.
    //
    // A hunter you can comfortably walk around is not wedged between you and anywhere. So it only counts once
    // it is near enough to contest the escape — the same range at which Core says an Old One stops being
    // scenery, which keeps the two halves of the owner's ruling ("not unless they get REALLY close") agreeing.
    private bool IsCornered()
    {
        foreach (Reever r in _reevers)
        {
            if (r.Y > _avatarY + 1.0 && r.Y <= MoonSurface.SurfaceTopY + 0.5 &&
                Math.Abs(r.X - _avatarX) < CornerLateralRange)
            {
                double dx = r.X - _avatarX, dy = r.Y - _avatarY;
                if ((dx * dx) + (dy * dy) <= NerveModel.DreadRangeDeckUnits * NerveModel.DreadRangeDeckUnits)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool SeesMonolith()
    {
        double dx = _avatarX - MoonSurface.MonolithX;
        double dy = _avatarY - MoonSurface.MonolithY;
        return (dx * dx) + (dy * dy) <= MonolithSightRange * MonolithSightRange;
    }

    // #314: the sentry line. Every SentryBot.FireIntervalSeconds, deployed non-dry bots each put one
    // round into the nearest Old One in their arc — the counter ticks down, the Reever soaks a hit, and
    // at RoundsPerReever hits it drops to a husk left where it fell. Pure resolution in Core; this owns
    // the cadence, the zap-line flash, and the husk ledger. Dry bots freeze silent.
    private void StepSentries(double dtRealSeconds)
    {
        if (_surface is not { } ex || ex.Bots.Count == 0)
        {
            return;
        }
        ex.FireTimer += dtRealSeconds;
        if (ex.FireTimer < SentryBot.FireIntervalSeconds)
        {
            return;
        }
        ex.FireTimer = 0;

        var live = ex.Bots.Where(b => b.Deployed && b.Rounds > 0).ToList();
        if (live.Count == 0 || _reevers.Count == 0)
        {
            return;
        }

        var deployed = live.Select(b => new SentryBot.Deployed(b.Unit, b.X, b.Y, b.Rounds)).ToList();
        var targets = _reevers.Select(r => new SentryBot.Target(r.X, r.Y, r.HitsTaken)).ToList();
        // #437: the guns obey the maze too — a slab between a bot and an Old One breaks the shot, on the
        // SAME segments the captain collides with and the Reevers sight along (owner, live 2026-07-26:
        // "Now the cannons shot though the walls").
        SentryBot.Volley volley = SentryBot.Step(deployed, targets, SightBlockers());

        // Fold the drained magazines back and flash a zap line from each bot that fired.
        double nowMs = _lastTimestampMs ?? 0;
        for (int i = 0; i < live.Count; i++)
        {
            SurfaceBot bot = live[i];
            bool fired = volley.Bots[i].Rounds < bot.Rounds;
            // #461: the tube's built-in gun never runs dry — it is the shuttle's fixture, not your magazine.
            // Everything else about it is an ordinary sentry (it obeys the walls, it can only shoot what it
            // can see), it simply never stops being able to hold the threshold.
            bot.Rounds = SurfaceArrival.IsDoorSentry(bot.Unit)
                ? SurfaceArrival.DoorSentryRounds
                : volley.Bots[i].Rounds;
            if (fired)
            {
                // #456: your own guns are the loudest thing on the moon. A volley calls the deep to the BOT
                // — so bringing sentries still buys time (#314), but now it is paid for by being found.
                MakeNoise(bot.X, bot.Y, ReeverHearing.Noise.Gunfire);

                // #488 · AND ABOARD, IT WAKES THEM. Owner: "when the guns start singing the reevers nearby
                // start to wake up." A hull that has been silent for forty years, and the first thing that
                // happens is automatic fire in a steel corridor — nothing sleeps through that.
                //
                // It goes through the wreck's own noise rule, so it obeys the same hard cap as everything
                // else the captain does: the NEAREST two, and no more. A firefight will steadily wake the
                // ship because it keeps happening, which is the right consequence and still never a summons.
                MakeNoiseAboard(bot.X, bot.Y, LoudEarshot);
            }
            if (fired && NearestReeverInArc(bot) is { } aim)
            {
                bot.AimX = aim.X;
                bot.AimY = aim.Y;
                bot.FiringUntilMs = nowMs + 120;
            }
        }

        // Re-map surviving Reevers' hit counts (position-match; the list order is preserved by Step's
        // survivor pass, which drops downed ones in index order). Rebuild from the survivor list.
        ApplyReeverSurvivors(volley.Reevers);

        if (volley.Husks.Count > 0)
        {
            foreach (SentryBot.Husk h in volley.Husks)
            {
                ex.Husks.Add((h.X, h.Y));
            }
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage($"🔫 Zap — {volley.Husks.Count} Old One{(volley.Husks.Count == 1 ? "" : "s")} down, {(volley.Husks.Count == 1 ? "a husk" : "husks")} left in the regolith. The sentries hold — watch the counters.");
        }
        // No per-shot cue: the guns fire five times a second — the zap-line flash and the ticking
        // counter carry the feedback; only a downed Old One earns a sound.
    }

    // Rebuild _reevers from the SentryBot survivor snapshot: downed ones are gone, survivors carry their
    // new hit counts. Matches by index over the live list Step was fed (same order, downed dropped).
    private void ApplyReeverSurvivors(IReadOnlyList<SentryBot.Target> survivors)
    {
        // Survivors preserve the fed order with downed entries removed, so walk both lists in step.
        int s = 0;
        var kept = new List<Reever>(survivors.Count);
        foreach (Reever r in _reevers)
        {
            if (s < survivors.Count && Math.Abs(survivors[s].X - r.X) < 1e-6 && Math.Abs(survivors[s].Y - r.Y) < 1e-6)
            {
                r.HitsTaken = survivors[s].HitsTaken;
                kept.Add(r);
                s++;
            }
            // else: this Reever was downed this volley — drop it.
        }
        if (kept.Count != _reevers.Count)
        {
            _reevers.Clear();
            _reevers.AddRange(kept);
        }
    }

    // Where a bot that just fired should be DRAWN aiming. Owner, live 2026-07-27: "See it fire through wall
    // now." #437/#438 taught the SHOT and the PIN to respect stone — but this, the third caller, still picked
    // by bare distance, so the gun legitimately shot the nearest thing it could SEE while the zap line was
    // drawn at the nearest thing FULL STOP. A beam painted across a monolith at a target the bot never
    // engaged: the fire was honest, the picture was not. Same CanEngage gate as the volley, so the beam can
    // only ever be drawn at the target the volley could actually have spent its round on.
    private (double X, double Y)? NearestReeverInArc(SurfaceBot bot)
    {
        double bestSq = SentryBot.RangeDeckUnits * SentryBot.RangeDeckUnits;
        (double, double)? best = null;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - bot.X, dy = r.Y - bot.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 <= bestSq && SentryBot.CanEngage(bot.X, bot.Y, r.X, r.Y, _deckPlan.CollisionField))
            {
                bestSq = d2;
                best = (r.X, r.Y);
            }
        }
        return best;
    }

    // #314: deploy a carried sentry at the captain's feet, or retrieve a deployed one they're standing on.
    // The [E]-style act on the bare ground — no console, so it's the T key (Map.Deck). Retrieval wins when
    // you're on top of a bot (dry or not); else you set one down.
    private void DeployOrRetrieveSentry()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        // Retrieve: a deployed bot within reach → back into the sling (keeps its remaining rounds).
        SurfaceBot? onFoot = null;
        double bestSq = DeckPlan.InteractRadius * DeckPlan.InteractRadius;
        foreach (SurfaceBot b in ex.Bots)
        {
            if (!b.Deployed)
            {
                continue;
            }
            double dx = b.X - _avatarX, dy = b.Y - _avatarY;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 <= bestSq)
            {
                bestSq = d2;
                onFoot = b;
            }
        }
        if (onFoot is not null)
        {
            onFoot.Deployed = false;
            RendererInterop.PlayCue("board");
            ShowPulseMessage($"🤖 {onFoot.Unit} shouldered — counter at {SentryBot.Readout(onFoot.Rounds)}. Back in the sling.");
            return;
        }

        // Deploy: the first carried bot goes down where you stand, facing the field.
        SurfaceBot? carried = ex.Bots.FirstOrDefault(b => !b.Deployed);
        if (carried is null)
        {
            ShowPulseMessage(ex.Bots.Count == 0
                ? "No sentry bots loaded — bring them down at boarding next time."
                : "Every bot's already deployed. Walk onto one and press T to pick it up.");
            return;
        }
        carried.Deployed = true;
        carried.X = _avatarX;
        carried.Y = _avatarY;
        RendererInterop.PlayCue("board");
        // #380 item 7 (owner ruling 2026-07-19: "new players are left mystified") — the FIRST deploy of an
        // excursion spells the whole doctrine out once, before the bots bite: they run dry, and a bot left
        // behind at liftoff is a write-off. Later deploys keep the short line.
        if (!ex.SentryHintShown)
        {
            ex.SentryHintShown = true;
            ShowPulseMessage($"🤖 {carried.Unit} deployed — magazine {SentryBot.Readout(carried.Rounds)}. The bot holds the line while its magazine lasts — a siege always outlasts the ammo. Bots buy time, not safety; don't forget them at liftoff.");
            return;
        }
        ShowPulseMessage($"🤖 {carried.Unit} deployed — magazine {SentryBot.Readout(carried.Rounds)}. It'll hold this arc until the counter reads 00. Bots buy time, not safety.");
    }

    private void StepReevers(double dtRealSeconds)
    {
        if (_surface is null || _reevers.Count == 0)
        {
            return;
        }
        double dt = Math.Min(dtRealSeconds, 0.1);
        double step = ReeverSpeed * dt;
        // The other half of the same bug: being CAUGHT was gated on the moon's safe line too, so aboard a
        // wreck nothing was ever found by anything — no blow, and no nerve either.
        bool onSurface = !CaptainBeyondReach;
        bool caught = false;
        double now = SimTime; // the thermal shuffle's time base (sim-seconds; the surface runs at 1×)
        // A Reever that advances less than this in a frame made effectively NO progress — it's at its leash,
        // wedged on a wall, or already on target. Tied to the tracker's own motion floor: sub-floor motion
        // this frame is "still" by the same law the fan reads, so we hold it and let it shiver in place.
        double idleProgress = MotionTracker.StillSpeed * dt;
        // #324: the maze is law for the many too — the Reevers bump-and-slide on the SAME wall segments
        // the captain does, and can only see the captain when no wall stands between.
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        const double reeverRadius = DeckPlan.AvatarRadius;
        // Sight for DRAWING is not the same list as sight for WALKING: a shut door stops the eye and not
        // the shamble, so the visibility test below uses the blockers (walls + shut doors) rather than the
        // collision field.
        IReadOnlyList<SurfaceCollision.Segment>? sight = OnWreck ? SightBlockers() : null;
        foreach (Reever r in _reevers)
        {
            // #488 · THE ONES THAT HAVE NOT WOKEN YET. They do not move, so they cost nothing here and the
            // motion tracker cannot see them (it is a MOTION fan — a still contact is not a contact). What
            // CAN see them is the captain: within lamp range and with no bulkhead in the way, a dormant one
            // is drawn exactly as it is — folded down, not moving, and about to stop being either.
            if (r.Dormant)
            {
                double lampDx = r.X - _avatarX, lampDy = r.Y - _avatarY;
                bool inLamp = (lampDx * lampDx) + (lampDy * lampDy) <= DormantSightRange * DormantSightRange
                              && SurfaceCollision.HasLineOfSight(_avatarX, _avatarY, r.X, r.Y, walls);
                r.VisibleOnMap = inLamp;
                r.Vx = 0;
                r.Vy = 0;

                // Its own clock, or the away team walking into it — whichever comes first.
                if (now >= r.WakeAtMs || inLamp)
                {
                    WakeTheSleeper(r);
                }
                continue;
            }

            // #488 · WHAT THE CAPTAIN CAN SEE, decided BEFORE anything below can `continue` past it. It
            // used to sit at the bottom of the loop, so an awake-but-unaware contact never reached it and
            // kept the VisibleOnMap it woke up with — drawn through steel (owner: "but also I see the
            // reever on map through the walls now"). Every path needs the same answer, so it is taken here.
            if (OnWreck)
            {
                bool wasSeen = r.VisibleOnMap;
                r.VisibleOnMap = SurfaceCollision.HasLineOfSight(_avatarX, _avatarY, r.X, r.Y, sight);

                // THE AMBUSH JOLT. Owner: "the surprise was there … but it had zero effect on my sanity?"
                // The #379 sighting spell charges only the first fright of a spell, which is right for a
                // horizon and wrong for a ship made of corners: a thing arriving INSIDE arm's reach with no
                // warning is a different event from one you watched cross a field.
                if (!wasSeen && r.VisibleOnMap)
                {
                    double jx = r.X - _avatarX, jy = r.Y - _avatarY;
                    if ((jx * jx) + (jy * jy) <= AmbushRange * AmbushRange)
                    {
                        ApplyNerveShock(
                            NervePips.SightingPips * (int)NervePips.PipUnit,
                            "it was already in the room with you");
                        RendererInterop.PlayCue("alarm");
                    }
                }
            }

            // #314: a live sentry pins the Old Ones on its arc — a Reever under a deployed, non-dry bot's
            // guns is held where it stands (stopped, not slowed) while it's ground down. Once the counter
            // reads 00 the gun goes quiet and the shamble resumes. This is "bots buy time, never safety".
            if (PinnedBySentry(r))
            {
                // The pin is law: the Old One is held where it stands while it's ground down. It is NOT a
                // statue, though (owner, cruise 2026-07-19) — it shivers in place. Capture the anchor once
                // so the mean-zero shuffle never creeps the pinned spot, and keep the tracker-facing
                // velocity a hard 0 so a pinned contact still reads honestly STILL on the fan (option a).
                if (!r.Idle)
                {
                    r.Idle = true;
                    r.AnchorX = r.X;
                    r.AnchorY = r.Y;
                }
                r.Vx = 0;
                r.Vy = 0;
                ApplyIdleShiver(r, walls, reeverRadius, now,
                    Math.Atan2(_avatarY - r.AnchorY, _avatarX - r.AnchorX));
                if (onSurface && ReeverChase.Caught(r.X, r.Y, _avatarX, _avatarY))
                {
                    caught = true;
                }
                continue;
            }
            // #324 line-of-sight: a Reever tracks the captain's LIVE position only while it can see them.
            // A wall between the two breaks the look — then it shambles to the last spot it saw them, and
            // (having never seen them, or arrived there) leans on the tube choke it always knows. Duck
            // behind stone and the hunter loses your live position; a stopped Reever also drops off the
            // motion tracker (motion-only law) — breaking sight in the maze is now real play.
            // #461: the arrival grace. A hull setting down is not news — they take it for one of their own
            // (owner: "ship in itself does not attract them. They expect it is their ship"). It is the warm
            // body walking out that is news, and even that gets a beat: nothing may notice the captain, by
            // eye OR by ear, until the grace has run. It is what makes stepping out of the door possible.
            // #488: aboard, a SHUT DOOR breaks their look as well as a wall — otherwise a hull full of
            // dogged hatches is no cover at all, and closing one behind you buys nothing. `sight` is walls
            // plus shut doors; off a wreck it is null and this is the old walls-only test exactly.
            if (SurfaceArrival.CanBeSpotted(((_lastTimestampMs ?? 0) - (_surface?.LandedAtMs ?? 0)) / 1000.0)
                && SurfaceCollision.HasLineOfSight(r.X, r.Y, _avatarX, _avatarY, sight ?? walls))
            {
                r.LastSeenX = _avatarX;
                r.LastSeenY = _avatarY;
                r.EverSeen = true;
            }

            // Owner, 2026-07-26: "make sure reevers behind walls can be unaware of the player being there
            // if they have not seen the player." An Old One that has NEVER laid eyes on the captain does
            // not know there is anyone out here to hunt — so it keeps its own ground and shivers there. It
            // no longer leans on the tube choke on spec, which read as knowing where you'd be before it had
            // any right to. It joins the hunt the frame stone stops standing between you (and once it has
            // seen you, losing sight only demotes it to the last-seen shamble — it does not forget).
            if (!r.EverSeen)
            {
                // #446 — and the owner's ruling on it, 2026-07-27: "The unaware reevers is a feature, not a
                // bug. As the player ventures deeper they can see the player then." An Old One that has
                // never laid eyes on the captain KEEPS ITS GROUND, holding whatever deep it claimed. The
                // stillness is the point: the field is quiet until you walk far enough in to be seen, and
                // then it is not. (A wander was tried here and reverted on that ruling — do not re-add it.)
                // #488 · AND ABOARD TOO. Owner: "I like them to be unaware… is there a problem with that in
                // the space scenario?" — there is not, and a prowl briefly added here was the wrong answer.
                // The only thing stillness costs a wreck is a motion fan with nothing to hear, and the fix
                // for that is not to make THEM noisy. It is to notice that the noisy thing on a dead ship is
                // the CAPTAIN: the pump, the handle, the valve, the hatch, the PA. See MakeNoiseAboard —
                // the racket you make is what puts contacts on the tracker, walking to the place it came
                // from. So the ship stays silent until you touch something, and then it does not.
                if (!r.Idle)
                {
                    r.Idle = true;
                    r.AnchorX = r.X;
                    r.AnchorY = r.Y;
                }
                r.Vx = 0;
                r.Vy = 0;
                ApplyIdleShiver(r, walls, reeverRadius, now, r.Facing);
                if (onSurface && ReeverChase.Caught(r.X, r.Y, _avatarX, _avatarY))
                {
                    caught = true; // walked right into it in the dark — that counts as being found
                }
                continue;
            }
            // Past the unaware gate above, this one HAS seen the captain: it hunts the last place it laid
            // eyes on them (their live position while the look holds).
            double tgtX = r.LastSeenX;
            double tgtY = r.LastSeenY;

            // Where this one actually stands right now (the anchor while idle) — needed to know how far the
            // run still is, so the encirclement can fade as it closes.
            double baseXPre = r.Idle ? r.AnchorX : r.X;
            double baseYPre = r.Idle ? r.AnchorY : r.Y;

            // Crude encirclement: aim a little toward the tube choke so the pack cuts the escape angle
            // instead of trailing single-file — the cornering loss-condition becomes real geometry.
            // #472 · THE BIAS SHAPES THE APPROACH, NOT THE DESTINATION. Owner: "still the reevers seem to
            // stop before the airlock" / "there is nothing between player and reevers still they do not
            // close the distance?" — and playtested: the pack parks a few units off the captain and hovers.
            //
            // The encirclement pulled the AIM POINT a fixed 28% toward the tube choke, at every range. So a
            // Reever standing on the captain was still steering at a spot offset up-field, arrived THERE,
            // and stopped — for good. It could never actually reach anybody; the cornering geometry was
            // quietly a no-contact rule.
            //
            // Fade the bias with distance: cut the escape angle while the run is long (which is the whole
            // point of it), and go straight for the captain once close. Contact is never sacrificed to
            // cleverness.
            double toTarget = Math.Sqrt(((tgtX - baseXPre) * (tgtX - baseXPre)) + ((tgtY - baseYPre) * (tgtY - baseYPre)));
            double bias = EncircleBias * Math.Clamp((toTarget - EncircleCloseRange) / EncircleFadeRange, 0, 1);
            // The encircle bias aims a little toward the WAY OUT, so they cut the escape rather than merely
            // following. Aboard a wreck the way out is her airlock, not the regolith's tube mouth — the moon
            // constants here would have them drifting toward a doorway on another map while they chased.
            double outX = OnWreck ? WreckLayout.SpawnX : MoonSurface.SpawnX;
            double outY = OnWreck ? WreckLayout.SpawnY : MoonSurface.SurfaceTopY;
            double aimX = tgtX + (outX - tgtX) * bias;
            double aimY = tgtY + (outY - tgtY) * bias;
            // #453 · ONE LEASH, AND IT IS A DOOR — not a distance. Owner, live 2026-07-27: "Let's not have
            // any don't venture too far set-up by y-coordinate. If you can get away with it with the help of
            // the sentries then do it but you might get killed by the reevers (or end up joining them)."
            //
            // This retires the 2026-07-18 tide home-range. That invisible horizontal line was the thing he
            // watched a charge halt on — "they were charging towards and just stopped… as if their path was
            // blocked by static distance from the airlock… why did they stop charging just to be shot while
            // standing still." Because ReeverChase clamped their y there, and a clamped Reever makes no
            // progress, so the client latched it Idle at zero velocity: a free target frozen on a line the
            // player could neither see nor shoot through.
            //
            // Now EVERY Old One — tide or dig-roll pack — chases to the one barrier that is real fiction: the
            // crew-only door at the tube mouth. How deep you dare go is priced by the sentries you brought
            // and your nerve, not by a number in the geometry.
            // #468 (owner, live 2026-07-27: "see how the dead reever is in the middle of the door… the reever
            // collision to door is just the centerpoint?"). It was. The crew-only clamp stopped their CENTRE
            // at the threshold, so a 0.7-radius body sat half inside the doorway — husks lay across the door
            // line, and worse, the gun's line to that centre never crossed the door segment, which is why a
            // round appeared to go THROUGH a shut door. Stop the BODY instead: they halt a full radius short
            // and the threshold stays clear, so what the player sees and what the geometry believes agree.
            // #488 · NOT ABOARD A WRECK. ReeverBarrierY is the REGOLITH's crew-only tube line (−20), and
            // ReeverChase caps every contact at it: `if (ny > barrierY) ny = barrierY`. A derelict's hull is
            // y ∈ [−9, +9], so on the first frame the cap threw the whole pack down to −20.7 — eleven units
            // below her keel, outside the ship, sitting in space at the bottom of the screen (owner, with
            // six of them out there: "that works on Miranda but not here").
            //
            // She has no such line. Her barrier is the CREW-ONLY LOCK at x = 21, which is a separate clamp
            // and already holds. Vertically the hull's own walls are the only thing that should stop them.
            double barrier = OnWreck ? double.PositiveInfinity : MoonSurface.ReeverBarrierY - reeverRadius;

            // Chase from the CANONICAL spot: while idle, r.X/r.Y carry the cosmetic shiver, so we step from
            // the fixed anchor instead (else the shuffle would feed itself and the anchor would drift). A
            // moving Reever's anchor is unset, so this is just its live position.
            double baseX = r.Idle ? r.AnchorX : r.X;
            double baseY = r.Idle ? r.AnchorY : r.Y;
            // #324 follow-up: which way this one skirts a wall it can't push through. Read off the shiver
            // seed, so the hand is FIXED per contact (no dithering at the face) and a pack splits — half
            // work a slab left, half right, and the two streams meet you around its ends.
            int wallSide = (r.JitterSeed & 1) == 0 ? 1 : -1;
            (double nx, double ny) = ReeverChase.Step(
                baseX, baseY, aimX, aimY, step * VacuumDrag(r), barrier, walls, reeverRadius, wallSide);

            double progressed = Math.Sqrt(((nx - baseX) * (nx - baseX)) + ((ny - baseY) * (ny - baseY)));

            if (progressed < idleProgress)
            {
                // No real progress — it's at its home-range leash, wedged on a wall, or already on the
                // captain: hold it and let it shiver (owner, cruise 2026-07-19). Anchor the resting spot
                // once; keep the tracker-facing velocity 0 so a held contact reads honestly still (option a).
                if (!r.Idle)
                {
                    r.Idle = true;
                    r.AnchorX = nx;
                    r.AnchorY = ny;
                }
                r.Vx = 0;
                r.Vy = 0;
                ApplyIdleShiver(r, walls, reeverRadius, now,
                    Math.Atan2(_avatarY - r.AnchorY, _avatarX - r.AnchorX));
            }
            else
            {
                // A live shamble — measured from the canonical base so a Reever breaking out of its idle
                // hold reports honest velocity from its true resting spot, not from the shivered position.
                r.Idle = false;
                r.Vx = dt > 0 ? (nx - baseX) / dt : 0;
                r.Vy = dt > 0 ? (ny - baseY) / dt : 0;
                r.X = nx;
                r.Y = ny;
                r.Facing = Math.Atan2(_avatarY - ny, _avatarX - nx);
            }
            // #488 · THE LOCK IS CREW-ONLY. Owner: "we don't want any uninvited infestations going there."
            // The lock bulkhead has a passage in it — walls alone would let the pack walk it, the same way
            // the captain does — so the rule that stops them is the one the ship's own tube already runs on:
            // a hatch keyed to the crew. It can reach the door. It cannot open the door.
            if (OnWreck && WreckLayout.PastTheLock(r.X, DeckPlan.AvatarRadius))
            {
                r.X = WreckLayout.HeldAtLock(r.X, DeckPlan.AvatarRadius);
                r.Vx = Math.Min(r.Vx, 0);
            }

            // #488 · THE MAP IS YOUR EYES, NOT AN X-RAY. Owner: "if there is a reever behind a closed door
            // should I see it so clearly on the map … the reevers can never surprise when opening a door
            // now :-D" — dead right, and it was making the tracker pointless as well: why read a fan when
            // the deck plan already draws every body through every bulkhead?
            //
            // So aboard a wreck a contact is DRAWN only with a clear line to it — walls and SHUT DOORS both
            // count, which is what puts the surprise back into opening one. It stays on the motion tracker
            // the whole time, because a motion fan hears through steel; that is the entire point of owning
            // one. Two instruments, two jobs: the fan says something is moving over there, and your own
            // eyes say what it is and exactly where.
            if (onSurface && ReeverChase.Caught(r.X, r.Y, _avatarX, _avatarY))
            {
                caught = true;
            }
        }

        // #441: the whole pack has stepped — now make them keep their elbows out (owner: "reevers merging
        // into a one blob… they should not"). AFTER the chase, never instead of it, so the shove can never
        // cancel forward progress or deadlock a queue at a corner back into #435's stall. Only the MOVING
        // ones are shoved: an idling contact is anchored on purpose (its shiver is mean-zero around that
        // anchor), and nudging it would creep the resting spot the anchor exists to hold still.
        if (_reevers.Count > 1)
        {
            Span<(double X, double Y)> spread = stackalloc (double X, double Y)[_reevers.Count];
            for (int i = 0; i < _reevers.Count; i++)
            {
                spread[i] = (_reevers[i].X, _reevers[i].Y);
            }
            ReeverPack.KeepApart(spread, walls, reeverRadius);
            // #453: and off the captain's own dot, on the same law. Safe here because every Reever's catch
            // test has already run this frame — reaching you still catches you; this only stops the drawn
            // dots from merging into one once that verdict is in.
            ReeverPack.KeepClearOfCaptain(spread, _avatarX, _avatarY, walls, reeverRadius);
            for (int i = 0; i < _reevers.Count; i++)
            {
                Reever moved = _reevers[i];
                if (moved.Idle)
                {
                    // #466 (owner: "Why did the reevers freeze into a blob there?… it's almost like blood
                    // clotting :-D"). Idling contacts used to be SKIPPED by the shove, to protect the anchor
                    // their mean-zero shiver orbits — but stopped is exactly when a pack piles up, so the
                    // spacing switched itself off at the one moment it was needed and they clotted at the
                    // door. Space them too, and carry the ANCHOR with them so the shiver stays centred on
                    // where the body actually is instead of dragging it back into the clot.
                    double ax = spread[i].X - moved.X, ay = spread[i].Y - moved.Y;
                    moved.AnchorX += ax;
                    moved.AnchorY += ay;
                }
                moved.X = spread[i].X;
                moved.Y = spread[i].Y;
                // The tracker reads velocity, and being shoved aside IS movement — but it is not the
                // hunter's own approach, so it never re-reports as closing. Leave Vx/Vy as the chase set
                // them; the shove is a correction to where it ended, not a claim about where it was going.
            }
        }

        if (caught)
        {
            ReeverCatch();
        }

        // #453: and then the swings themselves. AFTER the pack has stepped and been spaced, so "touching"
        // is measured on where the bodies actually ended up this frame.
        ResolveReeverSwings(_lastTimestampMs ?? 0);
    }

    // Thermal motion (owner, cruise 2026-07-19: "the reevers could be more active, like little thermal
    // motion so they don't just stay still"). Shiver a STILL Old One around its fixed anchor: a tiny,
    // seeded, mean-zero positional shuffle (ReeverIdle.JitterAt) plus a slow facing twitch. The shuffle is
    // wall-slid from the anchor with the SAME bump-and-slide the shamble uses, so it can never wedge the
    // body through stone even a hair. Velocity is the caller's to zero (option a keeps the fan honest);
    // this only moves the cosmetic position and facing, never the anchor.
    private void ApplyIdleShiver(Reever r, IReadOnlyList<SurfaceCollision.Segment> walls, double radius,
        double t, double baseFacing)
    {
        (double jx, double jy) = ReeverIdle.JitterAt(r.JitterSeed, t);
        (r.X, r.Y) = SurfaceCollision.Slide(r.AnchorX, r.AnchorY, jx, jy, radius, walls);
        r.Facing = baseFacing + ReeverIdle.FacingTwitchAt(r.JitterSeed, t);
    }

    // True if any deployed, non-dry sentry has this Old One inside its firing arc — the pin that holds it.
    private bool PinnedBySentry(Reever r)
    {
        if (_surface is not { } ex)
        {
            return false;
        }
        foreach (SurfaceBot b in ex.Bots)
        {
            // #437: a bot only holds what it can SEE — stone between the two breaks the pin exactly as it
            // breaks the shot, so a Reever that rounds a corner genuinely breaks contact with the gun
            // grinding it down.
            if (b.Deployed && b.Rounds > 0
                && SentryBot.CanEngage(b.X, b.Y, r.X, r.Y, SightBlockers()))
            {
                return true;
            }
        }
        return false;
    }

    // Lane-1 · THE TIDE (owner, Saturday-evening playtest 2026-07-18): "even with bots there is only so
    // long time to stay there." The deep hands up a Reever at seeded, jittered intervals for the WHOLE
    // excursion — no fixed total ("reevers coming from bottom of screen without any limited number … at
    // random intervals"). This supersedes the old dig-gated linger trickle: the tide runs from the moment
    // the boots hit regolith, not only after a dig, so time in the deep field is bounded on any visit. The
    // acute ReeverRaid pack (BeginDig) still turns out ON TOP of it — the tide is the ambient pressure.
    private void StepTide(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // #488: the tide is Old Ones clawing UP OUT OF THE REGOLITH. A derelict is a steel hull in vacuum —
        // there is no ground for them to come out of, and a wreck that quietly filled with Reevers would be
        // a different (and unearned) story than the one her evidence tells. Whatever is aboard a wreck gets
        // put there on purpose, not by the ground's own cadence.
        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _))
        {
            return;
        }
        // #318-style guard: clamp the frame delta before it feeds the accumulator so a background-tab
        // resume (rAF suspended, a multi-second delta) can't spawn a wall of Reevers in one frame — and
        // resolve at most MaxTideSpawnsPerFrame claw-outs this frame, letting any backlog trail over the
        // next few. TideSeconds only ever grows by a clamped ≤0.1 s, so in practice this loops 0–1 times.
        ex.TideSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);
        if (ex.TideNextGap <= 0.0)
        {
            ex.TideNextGap = ReeverTide.NextGap(ex.ThreatSeed, ex.TideSpawnIndex);
        }

        int resolved = 0;
        while (ex.TideSeconds >= ex.TideNextGap && resolved < MaxTideSpawnsPerFrame)
        {
            resolved++;
            ex.TideSeconds -= ex.TideNextGap;
            // The engine ceiling is a perf guard, not a gameplay cap: at the ceiling the claw-out is
            // skipped this beat but the tide clock rolls right on, so the deep resumes handing them up the
            // instant a sentry drops one and frees a slot.
            if (_reevers.Count < ReeverEngineCeiling)
            {
                SpawnTideReever(ex);
            }
            ex.TideSpawnIndex++;
            ex.TideNextGap = ReeverTide.NextGap(ex.ThreatSeed, ex.TideSpawnIndex);
        }

        // Don't bank unbounded seconds while pinned at the ceiling — hold at a single gap's worth so the
        // tide resumes promptly (not in a sudden flood) once a slot frees.
        if (_reevers.Count >= ReeverEngineCeiling && ex.TideSeconds > ex.TideNextGap)
        {
            ex.TideSeconds = ex.TideNextGap;
        }
    }

    // One tide Reever claws out of the deep edge at its seeded spawn point and begins to shamble up the
    // field. Silent by design — the motion tracker is the warning, not a klaxon (owner: "they should show
    // in the motion detector long before on the map"); only the first of an excursion earns a line so the
    // player learns the deep is alive. Marked Tide so StepReevers leashes it to the home range.
    private void SpawnTideReever(SurfaceExcursion ex)
    {
        (double x, double y) = MoonSurface.TideSpawnPoint(ex.ThreatSeed, ex.TideSpawnIndex);
        _reevers.Add(new Reever
        {
            X = x, Y = y, Facing = Math.PI / 2, Tide = true,
            // A distinct phase per tide contact (the spawn index, salted apart from the pack stream) so a
            // deep field of leash-held Old Ones all shiver independently at their home range.
            JitterSeed = (ex.ThreatSeed * 0xD1B54A32D192ED03UL) + (ulong)ex.TideSpawnIndex + 1UL,
        });

        // #459: a tide Reever claws out UNAWARE — it holds the deep it rose into until it sees or hears you
        // (#446's feature; the deep fills up and you meet it by venturing down). But if you are digging right
        // now, it rose into the sound of a shovel: replay the noise so anything in earshot — including the
        // one that just arrived — learns where the hole is. Otherwise MakeNoise only ever reached the Old
        // Ones that already existed when you started digging, and every latecomer was born deaf to it.
        if (ex.Channel is { } digging)
        {
            MakeNoise(digging.AnchorX, digging.AnchorY, ReeverHearing.Noise.Digging);
        }

        if (!ex.TideAnnounced)
        {
            ex.TideAnnounced = true;
            // #380 item 3: the one-time tide notice is the natural slot to say what a Reever IS — the first
            // time the deep stirs, name the Old Ones and the escape (fleeing works; they want YOU, not loot).
            ShowPulseMessage("〜 The tracker stirs — something's moving in the deep, far below. The regolith never stays empty for long. Don't linger. Reevers — the Old Ones. They don't want your loot; they want YOU. Grab what you came for and run.");
        }
    }

    // A caught digger: no loot taken (the whole point) — it prices the danger in heat, the same lever the
    // law's collectors use. Debounced so one brush isn't a stunlock.
    //
    // #380 item 1 — NOT a death today (owner constraint: don't build the surface-death / insurance-captain
    // mechanic here, just route what exists). A Reever's hand raises heat + shocks the nerve; the captain is
    // told to RUN, not resurrected. When the surface-death lane lands, this is the site that would classify
    // the death via DeathNarration.SurfaceEnd(_nerve, seed) → DeathCause.Reevers / .Joined and hand it to the
    // shared BUSTED resurrection (Cause + DeathBodyName on the encounter); the art + lines are already wired.
    private void ReeverCatch()
    {
        double now = _lastTimestampMs ?? 0;
        if (now - _lastReeverCatchMs < 1500)
        {
            return;
        }
        _lastReeverCatchMs = now;

        // #380 item 1 / Evening wind #20 — THE OVERDRAW. Nerves already bottomed out and an Old One lays
        // hands ANYWAY: this qualifying hit breaks the captain. Read on the nerve BEFORE the touch shock
        // (already empty + more damage), routed place-dependently (the Old Ones took you — or, rarely, you
        // joined them) into the shared BUSTED resurrection, where the piracy insurance issues a new captain.
        // Fail Forward — the run continues (ledger, ship and hoards persist). Below empty is where it breaks;
        // above it, the touch only floors the gauge and the captain is told to RUN, as before.
        if (_surface is { } dying && _busted is null && CaptainSuccession.OverdrawQualifies(_nerve))
        {
            TriggerSurfaceOverdrawDeath(dying, nerveRanOut: true); // the gauge broke first
            return;
        }

        if (_surface is { } ex)
        {
            ex.Catches++;
        }
        _heat = EncounterRule.RaiseHeat(_heat, 1, SimTime);
        // #480 · The nerve price of a hand on you is decided by NervePips, not here: ONE pip, ONCE per
        // encounter (owner: "repeated strikes should not cost more of sanity … we already take medical hit
        // from reever"), and again on every hand once the captain is nearly gone. We only report the event.
        _touchedThisFrame = true;
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage("🩸 An Old One lays hands on you — it wants no loot, only you. Tear free and RUN!");
        RequestVaultSave();
    }

    // ── #453 · THE EXCHANGE: five blows, and a die between each one and your skin ──────────────────────
    //
    // Owner, 2026-07-27: "player health could be like 5 reever hits but the reever sphere must touch the
    // player sphere when a hit is received. Player should have some melee blocking ability. Dice throw. We
    // should narrate what happens to the player. Maybe a splash of blood when reever hit goes through
    // players attempt to block it. :-D"
    //
    // A swing resolves ONLY on real contact — the two bodies touching, not merely near — and every Old One
    // winds up on its own cadence, so being held at arm's length by the pack shove (#441) is not a blender.
    private double _bloodUntilMs = double.NegativeInfinity;

    // Blood on the regolith for a moment after a blow gets through — the surface has never had visual
    // punctuation for being hurt, and "you are bleeding" should not be something you read in a corner.
    private bool BloodShowing => (_lastTimestampMs ?? 0) < _bloodUntilMs;

    // #466: a blow lands only when the two bodies TOUCH and nothing stands between them. Stone (and a shut
    // door) stops an arm exactly as it stops a round — otherwise a Reever pressed against the far face of a
    // slab is close enough to kill you through it.
    private bool CanSwingAt(Reever r, IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        // #471: contact is "at arm's length or nearer", and it must include EXACTLY arm's length. The
        // keep-off-the-captain shove (#453) parks a crowding Old One at precisely PersonalSpace — the very
        // same 1.4 that is the touch distance — so a strict comparison left every one of them a floating
        // hair too far away to ever swing. Playtested: three pressed against the captain, nerve shot, and
        // the condition still read "unmarked" because not one blow could register. A hair of tolerance.
        const double reach = CaptainCondition.TouchDistance + 0.05;
        double dx = r.X - _avatarX, dy = r.Y - _avatarY;
        if ((dx * dx) + (dy * dy) > reach * reach)
        {
            return false;
        }
        return SurfaceCollision.HasLineOfSight(r.X, r.Y, _avatarX, _avatarY, sight);
    }

    /// <summary>
    /// WHERE NOTHING CAN REACH THE CAPTAIN, on whichever thing they are standing.
    ///
    /// <para>Owner, standing shoulder to shoulder with an Old One aboard a wreck with a full nerve bar and
    /// five unmarked condition pips: <i>"look I take no damage or sanity loss from reever now."</i> He was
    /// exactly right, and it was never once possible. Both the blow and the being-caught were gated on
    /// <c>MoonSurface.IsSafeAboard</c>, which asks whether the captain is above the regolith's top rim at
    /// y = −20 — and a wreck's ENTIRE deck runs from −9 to +9. Every square metre of every derelict has
    /// always been "safely up the tube at the ship".</para>
    ///
    /// <para>The FOURTH bug of exactly this shape this weekend (the regolith tide aboard, the moon barrier
    /// clamping the pack outside the hull, the moon spawn point, and now this). The pattern is a MOON
    /// CONSTANT GOVERNING A SHIP, and it hides so well because the moon's number is not absurd for a wreck
    /// — it is merely satisfied everywhere, so the feature silently never fires and nothing ever errors.</para>
    ///
    /// <para>Aboard, safety is not a latitude. It is the shuttle's own lock: past that bulkhead is the away
    /// team's side and nothing follows you there, which is the same crew-only-door law the tube obeys.</para>
    /// </summary>
    private bool CaptainBeyondReach =>
        OnWreck
            ? WreckLayout.PastTheLock(_avatarX, DeckPlan.AvatarRadius)
            : MoonSurface.IsSafeAboard(_avatarY);

    private void ResolveReeverSwings(double nowMs)
    {
        if (_surface is not { } ex || _busted is not null || CaptainBeyondReach)
        {
            return; // up the tube, or past the shuttle lock — nothing reaches you there
        }

        // Who has a hand on you RIGHT NOW: bodies touching, the owner's rule. Counted first, because being
        // swarmed is itself a penalty on the block — every one past the first is another thing to watch.
        // #466 (owner, live 2026-07-27: "The reevers killed me through a wall there"). Touching is not
        // enough — a body a hair from yours on the FAR SIDE of a thin slab is still 1.4 units away, and the
        // swing landed through the stone. A blow needs a clear line as well as contact: the same sight law
        // the eyes and the guns obey (#324/#438), shut doors included (#465).
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();
        int touching = 0;
        foreach (Reever r in _reevers)
        {
            if (CanSwingAt(r, sight))
            {
                touching++;
            }
        }
        if (touching == 0)
        {
            return;
        }

        foreach (Reever r in _reevers)
        {
            if (!CanSwingAt(r, sight))
            {
                continue;
            }
            if (nowMs - r.LastSwingMs < CaptainCondition.SwingCooldownSeconds * 1000.0)
            {
                continue; // still winding up
            }
            r.LastSwingMs = nowMs;

            // The die, seeded off this contact and its swing count so a long fight never repeats itself.
            r.Swings++;
            ulong seed = DiceRule.Seed(r.JitterSeed, $"swing:{r.Swings}");
            DiceRoll roll = CaptainCondition.BlockRoll(seed, _nerve, ex.Carrying, touching);

            if (CaptainCondition.Resolve(roll) == CaptainCondition.Exchange.Blocked)
            {
                // #467: its own voice. A block RINGS — bright, hard, over in a blink — so it can never be
                // confused with the blow that gets through (owner: "I should know when I'm hurt").
                ShowPulseMessage($"🛡 {CaptainCondition.BlockLine(seed)}");
                RendererInterop.PlayCue("block");
                if (_showVentPanel)
                {
                    _ventMessage = $"🛡 {CaptainCondition.BlockLine(seed)}";
                }
                continue;
            }

            // It got through. One of the five, blood on the ground, and the old touch cost on top.
            ex.HitsTaken++;
            _bloodUntilMs = nowMs + 900;
            ShowPulseMessage($"🩸 {CaptainCondition.HitLine(seed)}");
            if (_showVentPanel)
            {
                // The pulse message lives on the canvas, and the board is standing on top of the canvas.
                // A blow landed while reading the panel has to arrive ON the panel or it never happened.
                _ventMessage = $"🩸 {CaptainCondition.HitLine(seed)}";
            }
            // #467: low, wet and wrong — nothing else in the game sounds like this. And at one pip left the
            // game stops being subtle about it: a floor-level dread tone on top, every single time.
            RendererInterop.PlayCue("wound");
            if (CaptainCondition.MaxHits - ex.HitsTaken == 1)
            {
                RendererInterop.PlayCue("last");
            }
            // #480: the blow already charged the body. The nerve is charged once for being CAUGHT (and
            // again every time once you are nearly gone) — NervePips decides, we only report it.
            _touchedThisFrame = true;
            RequestVaultSave();

            if (CaptainCondition.IsDown(ex.HitsTaken))
            {
                // The fifth blow. Routed into the SAME staged death the overdraw uses, so the piracy
                // insurance issues a new captain and the run continues (Fail Forward) — the ship, the
                // ledger and every buried cache outlive you (#455's rebirth thread).
                // The FIFTH BLOW — the condition marker decided, not the nerve. Since #480 this is the
                // common surface death, and it must not narrate as an overdraw.
                TriggerSurfaceOverdrawDeath(ex, nerveRanOut: false);
                return;
            }
        }
    }

    // ── Liftoff: board the shuttle (player-initiated ONLY — nothing self-resolves). ──

    private void LiftOffFromSurface()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        ex.Channel = null;
        bool escapedWithWatchdogs = _reevers.Count > 0;
        TreasureCache? buried = ex.Cache;

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

        _surface = null;
        _reevers.Clear();
        _lastNearestReeverRange = null;

        SetDeckForDock(ex.RestoreHavenId); // rebuild the ship/complex; folds the surface away
        (_avatarX, _avatarY, _avatarHeading) = (-6, -6.5, Math.PI / 2); // step off into the bay
        RendererInterop.PlayCue("board");

        string botTail = abandoned > 0
            ? $" {abandoned} sentry bot{(abandoned == 1 ? "" : "s")} left behind — written off."
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
            ShowPulseMessage($"🛸 Back aboard from {ex.Stop.Body.Name}.{tail}{botTail}");
        }
    }

    // ── The lonely automated kiosk (#313 amenity): a PLACE has shops. Pulse receipts (#119 style),
    //    house voice — last restocked before the war. ──

    // Slot 0 is the souvenir tee — its item + gag are filled from the moon underfoot at buy time
    // (SurfaceSouvenir), so Ganymede sells a Ganymede shirt, not Miranda's (#379). The placeholder
    // strings below are never shown; they only hold slot 0's price and mark the seam.
    // Owner, 2026-07-28: "The T-shirts etc everywhere where they are missing." Every HAVEN gift shop has
    // painted a tee AND a magnet since #367; the GROUND kiosk sold both and showed neither — a pulse line
    // and nothing to look at. The art column closes that: what you bought now gets held up.
    private static readonly (string Item, int Price, string Line, string Art)[] KioskStock =
    [
        ("the local souvenir tee", 15, "(keyed to the walked body — see VisitKiosk)",
            "art/souvenir-surface-tshirt.jpg"),
        ("a fridge magnet", 8, "It clamps to your suit's chestplate and refuses to let go. Value: eternal.",
            "art/souvenir-surface-magnet.jpg"),
        ("a vacuum-sealed hot meal", 12, "The label promises 'MEAT-ADJACENT'. The heater still works. Mostly.",
            ""), // no art — it is a ration pouch, and the joke is funnier unseen
    ];

    private int _kioskPicks;

    /// <summary>What the kiosk just sold you, held up for a look — the ground's answer to the haven gift
    /// shops' view-object cards. Null when nothing is being inspected.</summary>
    private readonly record struct KioskBuy(string Item, string Line, string Art);

    private KioskBuy? _kioskCard;

    private void CloseKioskCard() => _kioskCard = null;

    private void VisitKiosk()
    {
        if (_surface is not { } ex)
        {
            return; // the kiosk only sells on the ground it stands on
        }
        int slot = _kioskPicks % KioskStock.Length;
        (string item, int price, string line, string art) = KioskStock[slot];
        _kioskPicks++;
        if (slot == 0)
        {
            // The souvenir tee, keyed to the moon actually underfoot (#379): Ganymede's kiosk prints a
            // Ganymede shirt; Miranda keeps its canon line. Copy is generated, so any landable body works.
            CelestialBody body = ex.Stop.Body;
            item = SurfaceSouvenir.TeeItem(body.Name);
            line = SurfaceSouvenir.TeeGag(body.Id, body.Name);
        }
        if (_credits < price)
        {
            ShowPulseMessage($"🛒 {item} — {price} cr. The slot blinks INSUFFICIENT FUNDS in a dead language. Empty pockets, captain.");
            return;
        }
        _credits -= price;
        RendererInterop.PlayCue("board");
        ShowPulseMessage($"🧾 Bought {item} for {price} cr. {line} (The kiosk was last restocked before the war.)");
        if (art.Length > 0)
        {
            // Hold it up. The img onerror-hides, so an unpainted slot still degrades to the caption alone.
            _kioskCard = new KioskBuy(item, line, art);
        }
    }

    // ── The droid buffer: the ship's crew, plus the live Old Ones on the surface. ──

    private void FillSurfaceDroids(double simTime, DeckPlan.Droid[] buffer)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // [0..3): the crew
        for (int i = 0; i < ReeverEngineCeiling; i++)
        {
            int slot = 3 + i;
            // #371 Phase 3 (expedition fog): a behind-cover Old One is NOT drawn on the walked map — parked
            // off-screen exactly like an empty slot. VisibleOnMap is always true off an expedition site, so
            // Miranda and the moons draw every contact as before. The motion tracker (which reads _reevers
            // directly, not this buffer) still hears it through the wall — untouched.
            if (i < _reevers.Count && _reevers[i].VisibleOnMap)
            {
                Reever r = _reevers[i];
                buffer[slot] = new DeckPlan.Droid(r.X, r.Y, r.Facing, "Reever");
            }
            else
            {
                buffer[slot] = new DeckPlan.Droid(-9999, -9999, 0, "Reever");
            }
        }
    }

    // ── The motion tracker HUD (#313): a crude corner sweep of MOVING contacts, built for the renderer.
    //    Motion only — a wall-blocked, momentarily-still Old One drops off the fan. ──

    /// <summary>The fuzzy returns painted on the deck for contacts the fan hears through steel. Held here
    /// and refilled per frame, like every other HUD buffer.</summary>
    private readonly List<(double X, double Y, double Radius)> _hudSmudges = [];

    /// <summary>How wide a return is at point-blank, in deck units — already a REGION rather than a spot,
    /// because a crude fan never knew better than that.</summary>
    private const double SmudgeBaseRadius = 2.6;

    /// <summary>And how much wider per unit of range: the further off the contact, the vaguer the ear.</summary>
    private const double SmudgeRangeSpread = 0.12;

    /// <summary>Where the fan last heard each contact, and when — the raw material for the ghosts.</summary>
    private readonly Dictionary<Reever, (double X, double Y, double HeardAtMs)> _ghosts = [];

    /// <summary>The fading "movement was here" marks handed to the renderer.</summary>
    private readonly List<(double X, double Y, double Fade)> _hudGhosts = [];

    /// <summary>The nest's own motion, for the fan's benefit. It goes nowhere; it is never still. Anything
    /// above <see cref="MotionTracker.StillSpeed"/> reads as a live return, which is the truth about it.</summary>
    private const double NestChurn = 0.6;

    /// <summary>How wide the nest reads. Deliberately larger than any body smudge — the captain should be
    /// able to tell "something is in there" from "THAT is what is in there" at a glance.</summary>
    private const double NestSmudgeRadius = 4.2;

    /// <summary>Where the nest is, while it is still producing. Null once her room has been blown — a vented
    /// nest is off the tracker and off the map, and that silence is the reward for the soak.</summary>
    private (double X, double Y)? LiveNestPosition()
    {
        if (_wreck is not { Cause: Derelict.WreckCause.Infested })
        {
            return null;
        }
        if (!_ventSpaces.TryGetValue(WreckLayout.NestCompartment, out HullVenting.Space nest)
            || nest.Vented || !nest.Infested)
        {
            return null;
        }

        DeckReachability.Point at = WreckLayout.CauseStation(Derelict.WreckCause.Infested);
        return (at.X, at.Y);
    }

    /// <summary>How long a fresh return takes to settle from bright to its resting glow — the phosphor
    /// cooling, not the memory expiring. FLAGGED for tuning.</summary>
    private const double GhostSettleSeconds = 5.0;

    /// <summary>And the glow it never drops below. THE TRACKER REMEMBERS: a mark stays until the same
    /// contact is heard somewhere else. It is only wiped by better information, never by time.</summary>
    private const double GhostFloor = 0.45;

    /// <summary>How close a contact has to appear, with no warning, to land the ambush fright. Tight on
    /// purpose: this is "it was already in the room", not "I can see it down the corridor". FLAGGED.</summary>
    private const double AmbushRange = 7.0;

    /// <summary>
    /// #488 · THE PROWL — how a woken Old One that has not found you yet moves about a dead ship.
    ///
    /// <para>Deliberately NOT the regolith behaviour: out on the ground an unaware contact keeps its own
    /// deep and holds still, by the owner's own ruling, and that is untouched. Aboard, stillness would mean
    /// a motion tracker that never hears anything until the moment something is on top of you — which
    /// defeats the instrument the corridors were built around.</para>
    ///
    /// <para>Slow, aimless, and honest: it picks somewhere to be, walks there obeying the walls, and picks
    /// again. It is not searching for the captain — it does not know there is one. It is just awake.</para>
    /// </summary>
    private void Prowl(Reever r, IReadOnlyList<SurfaceCollision.Segment> walls, double radius,
                       double step, double now)
    {
        r.Idle = false;

        if (now >= r.ProwlUntilMs)
        {
            // Somewhere else on this deck, chosen off its own seed so each one wanders its own way and the
            // pack does not migrate as a blob.
            ulong pick = r.JitterSeed + (ulong)(now / ProwlLegMs);
            r.ProwlX = WreckLayout.AftX + 2 + ((pick % 53UL) / 53.0 * (WreckLayout.BowX - 8 - WreckLayout.AftX));
            r.ProwlY = ((pick / 53UL) % 3UL) switch { 0 => -6.0, 1 => 0.0, _ => 6.0 };
            r.ProwlUntilMs = now + ProwlLegMs;
        }

        double prowlStep = step * ProwlSpeedFraction;
        (double nx, double ny) = ReeverChase.Step(
            r.X, r.Y, r.ProwlX, r.ProwlY, prowlStep, double.PositiveInfinity, walls, radius,
            (r.JitterSeed & 1) == 0 ? 1 : -1);

        // Real velocity, because that is the entire point: the fan hears MOTION.
        r.Vx = prowlStep > 0 ? (nx - r.X) / (step / ReeverSpeed) : 0;
        r.Vy = prowlStep > 0 ? (ny - r.Y) / (step / ReeverSpeed) : 0;
        r.X = nx;
        r.Y = ny;
        r.Facing = System.Math.Atan2(r.ProwlY - ny, r.ProwlX - nx);

        // Wedged against something, or arrived: take a new bearing next frame rather than grinding.
        if (System.Math.Abs(r.Vx) + System.Math.Abs(r.Vy) < 0.01)
        {
            r.ProwlUntilMs = 0;
        }
    }

    /// <summary>How long a prowler holds one bearing before picking another.</summary>
    private const double ProwlLegMs = 7_000;

    /// <summary>A prowl is a wander, not a hunt — well under the chase so a contact that has actually SEEN
    /// you is unmistakably faster. FLAGGED for tuning.</summary>
    private const double ProwlSpeedFraction = 0.42;

    /// <summary>Gather the deployed sentries for the renderer. Pulled out of the full HUD build so the
    /// WRECK path can have them too: a bot on a steel deck is drawn exactly like a bot on regolith, and it
    /// was only ever invisible aboard because the whole hud was suppressed to get rid of the tracker.</summary>
    private void RefreshHudBots(SurfaceExcursion ex)
    {
        double nowMs = _lastTimestampMs ?? 0;
        _hudBots.Clear();
        foreach (SurfaceBot b in ex.Bots)
        {
            if (!b.Deployed)
            {
                continue;
            }
            _hudBots.Add((b.X, b.Y, SentryBot.Readout(b.Rounds), b.Rounds <= 0, b.FiringUntilMs > nowMs, b.AimX, b.AimY));
        }
    }

    private DeckView.SurfaceHud? BuildSurfaceHud()
    {
        if (_surface is not { } ex)
        {
            return null;
        }

        // #488: a DERELICT wears none of the regolith's INSTRUMENTS. The motion tracker sweeps for Old Ones
        // clawing out of ground that is not there; the key hints offer to DIG on a steel deck; the tracker
        // caption talks about movement in the deep. Boarded live, all three printed over the wreck's own
        // compartment labels and made her read like a moon with walls. She is a ship: the away team reads
        // her, they do not sweep her.
        //
        // THAT WAS DONE BY RETURNING NULL, AND IT TOOK THE SENTRIES WITH IT. Deployed bots are drawn from
        // this HUD, so aboard a wreck a bot went down, held its arc, pinned Old Ones — and was invisible.
        // (Owner, mid-playtest: "I tried to deploy K99 but the map does not show anything there.") A bot
        // holding a corridor while the pump runs is the loop this lane is FOR, so the wreck now gets a
        // REDUCED hud rather than none: the marks that belong on a deck, and none of the regolith's
        // instruments.
        bool onWreck = Derelict.TryParseWreckId(ex.Stop.Body.Id, out _);
        if (onWreck)
        {
            RefreshHudBots(ex);

            // THE TRACKER COMES UP WHEN THERE IS SOMETHING TO TRACK, AND THAT IS THE POINT. Owner: "we
            // could really use the motion detector here … I think we need it activating to bring it up on
            // hud — that could be the first sign we found something."
            //
            // Better than always-on, and better than my #488 call to remove it outright (which was only
            // defensible while the pack aboard was invisible, mislocated and topped up by a regolith tide).
            // On a hull you have been told is dead, the INSTRUMENT APPEARING is the beat: no caption, no
            // announcement, just a fan that was not on the screen a second ago. Once it has seen anything
            // it stays live for the rest of the boarding — an ear does not un-hear.
            _hudEntities.Clear();
            foreach (Reever r in _reevers)
            {
                _hudEntities.Add(new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy));
            }

            // THE NEST IS THE LOUDEST THING ABOARD. Owner: "the nest should show in the map and as movement
            // both." It never walks anywhere, so a fan that only reports travel would call it silence — but
            // a nest is not a still contact, it is a mass of small motion that never stops. So it goes on
            // the tracker with a motion of its own: a return that is always there, always in the same place,
            // and (below) far broader than a body. Once the captain has heard it they know where the ship's
            // supply is without being told, and cutting it becomes a place they can walk to.
            (double X, double Y)? nestAt = LiveNestPosition();
            if (nestAt is { } nx)
            {
                _hudEntities.Add(new MotionTracker.Entity(nx.X, nx.Y, NestChurn, 0));
            }
            IReadOnlyList<MotionTracker.Blip> aboardBlips = MotionTracker.Sweep(_avatarX, _avatarY, _hudEntities);
            double? aboardNearest = aboardBlips.Count > 0 ? aboardBlips[0].Range : null;
            bool aboardClosing = aboardNearest is { } an && _lastNearestReeverRange is { } prevAboard
                                 && an < prevAboard - 0.01;
            _lastNearestReeverRange = aboardNearest;

            _hudBlips.Clear();
            foreach (MotionTracker.Blip b in aboardBlips)
            {
                _hudBlips.Add((b.Bearing, b.Range));
            }
            _wreckTrackerLive |= aboardBlips.Count > 0;

            // A SMUDGE FOR EVERY CONTACT THE FAN HEARS AND THE CAPTAIN CANNOT SEE. Placed off the blip's
            // OWN bearing and range — the fan's actual output — rather than off the contact's true
            // position, and blurred by a radius that grows with range, because a crude fan is less sure
            // about a far return. What the captain gets is a region, which is exactly what they were told.
            _hudSmudges.Clear();
            foreach (Reever r in _reevers)
            {
                if (r.VisibleOnMap)
                {
                    // Your own eyes are better than the fan, so what you SEE also updates what the tracker
                    // remembers. Look away and the mark it leaves behind is where you last actually saw it.
                    _ghosts[r] = (r.X, r.Y, _lastTimestampMs ?? 0);
                    continue;
                }
                if (r.Dormant)
                {
                    continue;   // hibernating: nothing to hear, and nothing was ever heard
                }
                if (Math.Sqrt(((r.Vx * r.Vx) + (r.Vy * r.Vy))) < MotionTracker.StillSpeed)
                {
                    continue;   // a motion fan hears MOTION; a contact holding still is not a return
                }
                double dx = r.X - _avatarX, dy = r.Y - _avatarY;
                double range = Math.Sqrt((dx * dx) + (dy * dy));
                _hudSmudges.Add((r.X, r.Y, SmudgeBaseRadius + (range * SmudgeRangeSpread)));
                _ghosts[r] = (r.X, r.Y, _lastTimestampMs ?? 0);
            }

            // And on the map as a smear the size of the thing itself — not a contact the captain is meant to
            // shoot, a REGION they are meant to recognise. It is the one return that never moves and never
            // stops, which is how you tell it from the pack the moment you see it.
            if (nestAt is { } nm)
            {
                _hudSmudges.Add((nm.X, nm.Y, NestSmudgeRadius));
            }

            // THE GHOST OF WHERE IT WAS. Owner: "let's have the map show like a ghost of where movement was
            // last seen." A return that stops — because the contact went still, or slipped behind a hatch —
            // does not simply vanish, because the captain's knowledge does not. The mark stays where the
            // fan last had it and fades out over a few seconds, which is exactly as long as that knowledge
            // is worth anything. What it never does is follow: a ghost is a memory of a PLACE.
            // PHOSPHOR PERSISTENCE — the Aliens tracker, and the owner's own rule for it: "it was there it
            // last moved … it is probably still there until it moves away, when we will detect it again.
            // Better to have a couple of ghost detections than miss a reever."
            //
            // So a ghost NEVER expires. It burns bright where the return came in, decays to a floor, and
            // then sits there being the best information anyone has. If the contact moves again the mark
            // moves with it; if it went still, the mark is telling the truth — a thing that stopped is
            // still there. And if it slipped away without ever being heard again, the mark is a LIE the
            // captain can walk into, which is the price of an instrument that would rather be wrong than
            // quiet.
            _hudGhosts.Clear();
            double nowGhost = _lastTimestampMs ?? 0;
            foreach ((Reever ghosted, (double gx, double gy, double heardAt)) in _ghosts)
            {
                if (ghosted.VisibleOnMap)
                {
                    continue;   // your own eyes are on it — the memory is not needed
                }
                double age = (nowGhost - heardAt) / 1000.0;
                double fade = Math.Max(GhostFloor, 1.0 - (age / GhostSettleSeconds));
                _hudGhosts.Add((gx, gy, fade));
            }

            return new DeckView.SurfaceHud(
                TrackerCaptions: null,
                DigProgress: ex.DoorChannel?.Progress ?? -1,   // a forced door is a ship thing; digging is not
                HasDroppedChest: false, DropX: 0, DropY: 0,
                Blips: _hudBlips,
                Cadence: (int)MotionTracker.CadenceFor(aboardNearest),
                Readout: MotionTracker.Readout(aboardNearest, aboardClosing),
                CacheMarks: [],                                // nothing is buried on a steel deck
                Nerve: _nerve,
                NerveReadout: NerveModel.Readout(_nerve),
                Bots: _hudBots,                                // ← the fix
                Husks: _hudHusks,
                KeyHints: BuildSurfaceKeyHints(ex),            // names [T] aboard, never DIG
                Countdown: _scuttleSecondsLeft is { } burning
                    ? (WreckLayout.ScuttleStation.X, WreckLayout.ScuttleStation.Y,
                       HullVenting.SoakLabel(burning))
                    : null,
                Instruments: _wreckTrackerLive,                // it appears when something moves. That IS the warning.
                Smudges: _hudSmudges,                          // heard through steel: a region, never a body
                Ghosts: _hudGhosts,                            // and where it was, fading
                BloodSplash: BloodShowing
                    ? Math.Clamp((_bloodUntilMs - (_lastTimestampMs ?? 0)) / 900.0, 0, 1)
                    : 0);
        }
        // #371 Phase 1 (perf): fill the reused entity buffer instead of a lazy Select — one iterator fewer
        // per frame, and MotionTracker.Sweep reads it as an IEnumerable exactly as before.
        _hudEntities.Clear();
        foreach (Reever r in _reevers)
        {
            _hudEntities.Add(new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy));
        }
        IReadOnlyList<MotionTracker.Blip> blips = MotionTracker.Sweep(_avatarX, _avatarY, _hudEntities);
        double? nearest = blips.Count > 0 ? blips[0].Range : null;
        bool closing = nearest is { } n && _lastNearestReeverRange is { } prev && n < prev - 0.01;
        _lastNearestReeverRange = nearest;

        _hudBlips.Clear();
        foreach (MotionTracker.Blip b in blips)
        {
            _hudBlips.Add((b.Bearing, b.Range));
        }

        // The own caches' ✗ marks (with the DigX/DigY-or-hash-scatter fallback, same as OwnCachePositionsAt)
        // straight into the reused buffer — no intermediate list + Select allocation.
        string bodyId = ex.Stop.Body.Id;
        _hudMarks.Clear();
        // 🗺 Layers (#405) Ground finds → Treasure ✗: the buried-cache marks the excursion HUD carries.
        foreach (TreasureCache c in LayerVisible("finds.treasure") ? _caches.CachesAt(bodyId) : [])
        {
            if (!c.PlayerOwned)
            {
                continue;
            }
            (double mx, double my) = c is { DigX: { } dx, DigY: { } dy }
                ? (dx, dy)
                : MoonSurface.CachePosition(c.Id);
            _hudMarks.Add((mx, my, c.ReeverLevel > 0));
        }

        RefreshHudBots(ex);

        _hudHusks.Clear();
        // 🗺 Layers (#405) Ground finds → Husks: the downed-Old-One marks left in the regolith (#316).
        if (LayerVisible("finds.husks"))
        {
            foreach ((double hx, double hy) in ex.Husks)
            {
                _hudHusks.Add((hx, hy));
            }
        }

        // The per-visit swept grid: every beach-comber square probed this excursion, at its centre, with a
        // hard-ground flag so the deck-plan paints a bedrock mark distinct from a plain checked square. The
        // draw is BOUNDED (MaxSweptDrawn) so a fully-probed field can't paint an unbounded mark cloud.
        _hudSwept.Clear();
        foreach (KeyValuePair<(int X, int Y), BeachComber.Outcome> kv in ex.Swept)
        {
            if (_hudSwept.Count >= MaxSweptDrawn)
            {
                break;
            }
            (double cx, double cy) = BeachComber.SquareCenter(kv.Key.X, kv.Key.Y);
            _hudSwept.Add((cx, cy, kv.Value == BeachComber.Outcome.TooHard));
        }

        // #327 the ship calling home, now behind the COMMS-LOSS display gate: SurfaceComms wraps the honest
        // feed with the live downlink phase, so a degraded/blacked-out link freezes the orbit line at
        // last-known (banner + CommsState for the renderer's static). The true state is never touched.
        (string Line, int Severity, int CommsState)? orbit = SurfaceComms();

        return new DeckView.SurfaceHud(
            TrackerCaptions: BuildTrackerCaptions(ex, _hudMarks.Count),
            // #371 Phase 3 / #562: the one progress bar serves every slow thing — a dig, a forced door, or
            // the tube racking a magazine. The rearm is last because it is the only one that can be running
            // while the captain is somewhere the others cannot happen (inside the tube), so it can never
            // actually contend; ordering it here just keeps the two hands-on channels reading first.
            DigProgress: ex.Channel?.Progress ?? ex.DoorChannel?.Progress
                ?? (ex.RearmBotIndex is not null ? ex.RearmProgress : -1),
            // #562: and it says which. A shovel over a magazine being racked would be exactly the class of
            // lie this lane exists to fix; the rearm is the ship HELPING you, so it reads cold-green.
            ChannelGlyph: ex.RearmBotIndex is not null && ex.Channel is null && ex.DoorChannel is null ? "🔫" : "⛏",
            ChannelIsAid: ex.RearmBotIndex is not null && ex.Channel is null && ex.DoorChannel is null,
            HasDroppedChest: ex.ChestDropped, DropX: ex.DropX, DropY: ex.DropY,
            Blips: _hudBlips,
            Cadence: (int)MotionTracker.CadenceFor(nearest),
            Readout: MotionTracker.Readout(nearest, closing),
            CacheMarks: _hudMarks,
            Nerve: _nerve,
            NerveReadout: NerveModel.Readout(_nerve),
            Bots: _hudBots,
            Husks: _hudHusks,
            KeyHints: BuildSurfaceKeyHints(ex),
            OrbitComms: orbit?.Line,          // #327: the ship's calling-home line, never buried
            OrbitSeverity: orbit?.Severity ?? 0,
            CommsState: orbit?.CommsState ?? 0, // COMMS-LOSS: 0 nominal · 1 degraded · 2 blackout — the renderer's static/grey cue
            SweptSquares: _hudSwept,
            DarkRegions: BuildDarkRegions(ex),   // #371 Phase 3: born-dark / explored appended chambers
            Echoes: BuildEchoes(ex),             // #371 Phase 3: fading "movement was here" ripples
            StandingPrompt: BuildStandingPrompt(ex),
            // #453: the blood fades over its window, so the spatter is a beat rather than a decal.
            BloodSplash: BloodShowing ? Math.Clamp((_bloodUntilMs - (_lastTimestampMs ?? 0)) / 900.0, 0, 1) : 0);
    }

    // #440 · The standing prompt: ONE bright line above the keybar for the thing this excursion hangs on.
    // Owner, 2026-07-26: "the press T to bury treasure is not advertised clearly enough on surface… It is
    // the key to survival there" — said while misremembering the key, which is the proof. A chest in hand is
    // the whole reason you came and the whole thing you lose, so it gets a line that does not blend into
    // chrome and does not go away until the chest is in the ground. It also answers WHERE, because "where
    // you stand" is the rule and nothing on screen ever said so: out on the open regolith, past the pad.
    private string? BuildStandingPrompt(SurfaceExcursion ex)
    {
        if (!ex.Carrying)
        {
            return null; // nothing owed — the ground goes quiet again
        }
        return MoonSurface.IsDiggableGround(_avatarX, _avatarY)
            ? "⛏ CARRYING THE CHEST — press E to BURY IT HERE"
            : "⛏ CARRYING THE CHEST — walk out onto the regolith, then E to bury it";
    }

    // #324: the contextual surface keybar. The owner couldn't find the deploy key — so while a bot rides
    // the sling it spells out [T] deploy, and a chest in hand spells [G] drop. Affordances never hide.
    private string BuildSurfaceKeyHints(SurfaceExcursion ex)
    {
        // #488: aboard a derelict there is nothing to DIG. There is, however, very much somewhere to plant
        // a sentry — a bot holding a corridor while a compartment pumps down is the loop this whole lane is
        // for — and this bar used to say otherwise and then hide the key, which is how the owner ended up
        // pressing T at a map that showed him nothing. Affordances never hide (#212).
        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _))
        {
            var aboard = new List<string> { "WASD — move", "E — examine / take" };
            if (ex.Bots.Any(b => !b.Deployed))
            {
                aboard.Add("🤖 T — deploy a sentry");
            }
            else if (ex.Bots.Any(b => b.Deployed &&
                     ((b.X - _avatarX) * (b.X - _avatarX)) + ((b.Y - _avatarY) * (b.Y - _avatarY))
                         <= DeckPlan.InteractRadius * DeckPlan.InteractRadius))
            {
                aboard.Add("🤖 T — pick up the sentry");
            }
            aboard.Add("F — first person");
            aboard.Add(_audioEnabled ? "🔊 M — mute" : "🔇 M — unmute");
            return string.Join(" ∙ ", aboard);
        }

        // #440: the bar must NAME the thing that matters. "E — dig / use" is honest but generic, and it was
        // generic at the one moment it should shout — with the chest in your hands (owner, 2026-07-26: "the
        // press T to bury treasure is not advertised clearly enough on surface… It is the key to survival
        // there", having misremembered the key himself). Carrying → the bar says BURY, in the imperative.
        var parts = new List<string> { "WASD — move", ex.Carrying ? "⛏ E — BURY THE CHEST HERE" : "E — dig / use" };
        bool carryingBot = ex.Bots.Any(b => !b.Deployed);
        bool deployedUnderfoot = ex.Bots.Any(b => b.Deployed &&
            ((b.X - _avatarX) * (b.X - _avatarX)) + ((b.Y - _avatarY) * (b.Y - _avatarY))
                <= DeckPlan.InteractRadius * DeckPlan.InteractRadius);
        if (carryingBot)
        {
            parts.Add("🤖 T — deploy a sentry");
        }
        else if (deployedUnderfoot)
        {
            parts.Add("🤖 T — pick up the sentry");
        }
        if (ex.Carrying)
        {
            parts.Add("G — drop the chest & sprint");
        }
        parts.Add("F — first person");
        parts.Add(_audioEnabled ? "🔊 M — mute" : "🔇 M — unmute"); // #338: the first-sound switch, always spelled out
        return string.Join(" ∙ ", parts);
    }

    // Lane-1 (owner, 2026-07-18: "advertise the dig and bot options in text under the motion detector"):
    // the short contextual lines seated below the tracker readout in the left instrument column. They
    // teach the two levers the surface offers — the DIG (the reason to come, the reason to hurry) and the
    // SENTRY (the thing that buys time against the tide, never safety). Kept to a couple of lines so the
    // column stays legible; empty entries are skipped by the renderer.
    private List<string> BuildTrackerCaptions(SurfaceExcursion ex, int ownMarkCount)
    {
        var lines = new List<string>();

        // #564 · THE TANK, first in the column and always shown. It leads because it is the only thing on
        // the surface that kills you without touching you, and because the readout's job is to be glanced
        // at rather than calculated: it says how much FURTHER you may go, not merely how much is left. A
        // bare countdown would be exactly the silent timer this mechanic must not be.
        lines.Add(SuitAir.Readout(ex.AirSeconds, DistanceToTheTube()));

        // The dig affordance, honest to the sling (playtest bug #1 / owner ruling #9: the ground must SAY
        // what's possible). Carrying → bury anywhere you stand; empty → the beach-comber probe, a real
        // fishing expedition, never a dead end. An own ✗ in this ground always earns its own lift line.
        if (ex.Carrying)
        {
            lines.Add("⛏ E on the regolith — bury the chest where you stand");
        }
        else
        {
            lines.Add("🪛 E on the regolith — probe for shallow treasure");
        }
        if (ownMarkCount > 0)
        {
            lines.Add("🗺 E at your ✗ — dig the cache back up");
        }
        // #409: once the hidden lab door is revealed, advertise it until it's forced.
        if (ex.SecretLabDoorRevealed && !ex.SecretLabForced)
        {
            lines.Add("⚙ E at the ⚙ HIDDEN DOOR — force the secret lab open");
        }

        // The sentry affordance — spell out T while it matters (a bot in the sling to set, or ones holding
        // the line). The tide never stops, so the caption tells the truth: they buy time, not safety.
        //
        // #440 (owner, live 2026-07-26: "The T key for sentry planting is not mentioned there now on the
        // sentry line?"). It wasn't: once the LAST bot left the sling, this fell to the "N holding" line,
        // which names no key at all — and the keybar only says [T] while you happen to be standing on a
        // bot. So the moment you had committed both, the key that takes them back up vanished from the
        // screen entirely. Now T is named in EVERY state that has a bot in it, planted or slung.
        int carried = ex.Bots.Count(b => !b.Deployed);
        int deployed = ex.Bots.Count(b => b.Deployed);
        if (carried > 0)
        {
            lines.Add($"🤖 T — set a sentry ({carried} in the sling)");
        }
        if (deployed > 0)
        {
            lines.Add($"🤖 {deployed} sentry holding — T at one to lift it · buys time, not safety");
        }

        return lines;
    }

    // Seed the 2D6 from place + integer-second instant — deterministic, replayable in a test.
    private ulong ReeverSeed(string bodyId) => DiceRule.Seed($"reever:{bodyId}", (long)SimTime);

    // The highest watchdog presence standing over any chest already at this body (the ground's memory).
    private int WatchdogLevelAt(string bodyId)
    {
        int level = 0;
        foreach (TreasureCache c in _caches.CachesAt(bodyId))
        {
            level = Math.Max(level, c.ReeverLevel);
        }
        return level;
    }
}
