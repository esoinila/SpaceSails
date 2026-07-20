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
        public bool SentryHintShown { get; set; }         // #380 item 7: the one-time first-deploy sentry hint has fired

        public ulong ThreatSeed { get; set; }
        public TreasureCache? Cache { get; set; }        // set on a completed bury (for the map card)

        // The per-visit swept grid (owner, 2026-07-18: "some kind of grid system onto planet Miranda for
        // marking the checked squares on that visit"). Every beach-comber square probed THIS excursion,
        // keyed by its integer BeachComber square → what the throw turned up, so the deck-plan can paint a
        // subtle checked/bedrock mark. Client-only and per-visit — a fresh SurfaceExcursion on the next
        // landing starts empty, exactly like the Reever positions (never saved).
        public Dictionary<(int X, int Y), BeachComber.Outcome> Swept { get; } = [];
        public int Catches { get; set; }
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

        public List<SurfaceBot> Bots { get; init; } = [];  // #314: sentries carried + deployed this excursion
        public List<(double X, double Y)> Husks { get; init; } = [];  // #314: downed Old Ones, left where they fell (#316)
        public double FireTimer { get; set; }              // #314: accrues to the SentryBot fire cadence

        // A chest is in hand right now: something was loaded, not yet buried, not dropped.
        public bool Carrying => (PendingCoin > 0 || PendingCargo.Count > 0) && !Buried && !ChestDropped;
        public bool Channeling => Channel is not null;
        // #371 Phase 3 / #394: any channel underway (a dig, a door-force, OR the drill) — mutually exclusive.
        public bool AnyChannel => Channel is not null || DoorChannel is not null || DrillChannel is not null;
    }

    // ── Boarding: pick a surface, optionally load a chest, and grow the tube IN PLACE. ──

    // Destination-first entry (#313). Called from the shuttle bay when the captain chooses a landable
    // surface. The chest is optional cargo already packed by the boarding panel; boarding empty-handed
    // is a complete, valid sightseeing hop. NO teleport: the captain keeps standing at the bay and the
    // down-tube + surface weld on below, so they walk down continuously.
    private async Task BeginSurfaceExcursion(ShuttleStop stop, ShuttleExcursion.ChestLoad chest, int botsToBring = 0)
    {
        if (_ephemeris is null)
        {
            return;
        }
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
        _deckPlan = MoonSurface.SurfaceDeck(
            ex.Stop.Body.Id, ex.Stop.Body.Name, OwnCachePositionsAt(ex.Stop.Body.Id),
            3 + ReeverEngineCeiling, FillSurfaceDroids);

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
        // Safe up in the tube / aboard, or up on the landing band — no digging the fused pad.
        if (!MoonSurface.IsDiggableGround(_avatarX, _avatarY))
        {
            ShowPulseMessage(ex.Carrying
                ? "The landing pad's fused rockcrete — no burying here. Carry it out onto the regolith."
                : "Nothing to probe on the landing pad — it's fused rockcrete. Walk out onto the regolith.");
            return;
        }

        (int sqX, int sqY) = BeachComber.SquareOf(_avatarX, _avatarY);

        // The die's first job (owner: "some surfaces may be too hard to dig … the die could handle those").
        // Bedrock refuses the dig outright — no hole, no watch — but the square is now KNOWN and joins the
        // swept grid so the sweep reads it as checked.
        Probe probe = BeachComber.Roll(ex.Stop.Body.Id, sqX, sqY);
        if (probe.IsTooHard)
        {
            ex.Swept[(sqX, sqY)] = probe.Outcome;
            RendererInterop.PlayCue("board");
            ShowPulseMessage(ex.Carrying
                ? "⛏ The shovel rings off bedrock — this square won't take a chest. Try a step over."
                : "⛏ The shovel rings off bedrock a foot down — too hard to dig here. Try another square.");
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

    private void CompleteDig(SurfaceExcursion ex, DigChannel ch)
    {
        ex.Channel = null;
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

        if (!probe.IsFind)
        {
            RendererInterop.PlayCue("board");
            ShowPulseMessage("🕳 Nothing but regolith down there. The detector stays quiet — you mark the square and move on.");
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
        ShowPulseMessage($"✨ The detector chirps — you turn up {probe.FindCoin:N0} cr{scrapTail} a few inches down. Luck, not a fortune. Mark it and keep moving.");
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
            });
        }
    }

    // The surface tick: dig channel, sentries, the chase, and the ambient tide — all cheap, no pathfinding.
    private void StepSurface(double dtRealSeconds)
    {
        if (_surface is null)
        {
            return;
        }
        StepDigChannel(dtRealSeconds);
        StepDoorChannel(dtRealSeconds); // #371 Phase 3: the forced-door progress bar
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
        else
        {
            StepTide(dtRealSeconds);
        }
        StepFirstContactChirp(dtRealSeconds);
        TryRecoverDroppedChest();
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

        var frame = new NerveModel.Frame(
            OnExcursion: onExcursion,
            OnRegolith: onRegolith,
            SeesMonolith: onRegolith && SeesMonolith(),
            Stressors: onRegolith
                ? new NerveModel.Stressors(CountMovingReevers(), _reevers.Count > 0, _surface!.Channeling, IsCornered())
                : default,
            DtSeconds: dtRealSeconds);

        NerveModel.Step step = NerveModel.Advance(_nerve, _monolithSeen, in frame);
        _nerve = step.Nerve;
        _monolithSeen = step.MonolithSeen;

        // #379 · the per-spell diminishing sightings (Evening wind #18). Only the regolith frays you — a
        // mover seen from the airlock's safety costs nothing (same law as the drain), so off the regolith we
        // feed the tally zero movers, which also winds the spell down toward its quiet reset. The fresh
        // contacts that crest THIS frame land a diminishing, S-curve-shaped jolt.
        int heardMovers = 0;
        if (onRegolith && _surface is not null)
        {
            double detection = MotionTracker.DetectionRange(SurfaceVisualHalfWidthDu);
            var ents = _reevers.Select(r => new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy));
            heardMovers = MotionTracker.DetectedMovingCount(_avatarX, _avatarY, ents, detection);
        }
        (NerveModel.SightingSpell nextSpell, int freshSightings) =
            NerveModel.AdvanceSightings(_sightings, heardMovers, dtRealSeconds);
        if (onRegolith && freshSightings > 0)
        {
            _nerve = NerveModel.SightingDrain(_nerve, _sightings.Seen, freshSightings);
        }
        _sightings = nextSpell;

        if (step.MonolithHitFired)
        {
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage("👁 The monolith resolves out of the dark — too regular, too old, too patient. Something behind your eyes lurches, and your hands remember it.");
            RequestVaultSave();
        }
    }

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

    // A net between the captain and the tube: an Old One wedged up-field (nearer the tube mouth than the
    // captain) and laterally close enough to block the sprint. Cheap geometry, matching the encirclement
    // the pack already leans into — the "cornered" the owner named, priced as a stressor.
    private bool IsCornered()
    {
        foreach (Reever r in _reevers)
        {
            if (r.Y > _avatarY + 1.0 && r.Y <= MoonSurface.SurfaceTopY + 0.5 &&
                Math.Abs(r.X - _avatarX) < CornerLateralRange)
            {
                return true;
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
        SentryBot.Volley volley = SentryBot.Step(deployed, targets);

        // Fold the drained magazines back and flash a zap line from each bot that fired.
        double nowMs = _lastTimestampMs ?? 0;
        for (int i = 0; i < live.Count; i++)
        {
            SurfaceBot bot = live[i];
            bool fired = volley.Bots[i].Rounds < bot.Rounds;
            bot.Rounds = volley.Bots[i].Rounds;
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

    private (double X, double Y)? NearestReeverInArc(SurfaceBot bot)
    {
        double bestSq = SentryBot.RangeDeckUnits * SentryBot.RangeDeckUnits;
        (double, double)? best = null;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - bot.X, dy = r.Y - bot.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 <= bestSq)
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
        bool onSurface = !MoonSurface.IsSafeAboard(_avatarY);
        bool caught = false;
        double now = SimTime; // the thermal shuffle's time base (sim-seconds; the surface runs at 1×)
        // A Reever that advances less than this in a frame made effectively NO progress — it's at its leash,
        // wedged on a wall, or already on target. Tied to the tracker's own motion floor: sub-floor motion
        // this frame is "still" by the same law the fan reads, so we hold it and let it shiver in place.
        double idleProgress = MotionTracker.StillSpeed * dt;
        // #324: the maze is law for the many too — the Reevers bump-and-slide on the SAME wall segments
        // the captain does, and can only see the captain when no wall stands between.
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionSegments;
        const double reeverRadius = DeckPlan.AvatarRadius;
        foreach (Reever r in _reevers)
        {
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
            if (SurfaceCollision.HasLineOfSight(r.X, r.Y, _avatarX, _avatarY, walls))
            {
                r.LastSeenX = _avatarX;
                r.LastSeenY = _avatarY;
                r.EverSeen = true;
            }
            double tgtX = r.EverSeen ? r.LastSeenX : MoonSurface.SpawnX;
            double tgtY = r.EverSeen ? r.LastSeenY : MoonSurface.SurfaceTopY;

            // Crude encirclement: aim a little toward the tube choke so the pack cuts the escape angle
            // instead of trailing single-file — the cornering loss-condition becomes real geometry.
            double aimX = tgtX + (MoonSurface.SpawnX - tgtX) * EncircleBias;
            double aimY = tgtY + (MoonSurface.SurfaceTopY - tgtY) * EncircleBias;
            // Lane-1: two leashes. A dig-roll PACK member chases to the very crew-only door
            // (ReeverBarrierY); a TIDE Reever holds the deep and turns back at its home range — owner
            // 2026-07-18: they "will stop venturing too far" toward the landing. The barrier IS the leash
            // (ReeverChase caps y at it), so bots can pin a deep spot but never protect the whole field.
            double barrier = r.Tide ? MoonSurface.ReeverTideHomeRangeY : MoonSurface.ReeverBarrierY;

            // Chase from the CANONICAL spot: while idle, r.X/r.Y carry the cosmetic shiver, so we step from
            // the fixed anchor instead (else the shuffle would feed itself and the anchor would drift). A
            // moving Reever's anchor is unset, so this is just its live position.
            double baseX = r.Idle ? r.AnchorX : r.X;
            double baseY = r.Idle ? r.AnchorY : r.Y;
            (double nx, double ny) = ReeverChase.Step(baseX, baseY, aimX, aimY, step, barrier, walls, reeverRadius);
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
            if (onSurface && ReeverChase.Caught(r.X, r.Y, _avatarX, _avatarY))
            {
                caught = true;
            }
        }
        if (caught)
        {
            ReeverCatch();
        }
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
            if (b.Deployed && b.Rounds > 0 && SentryBot.InRange(b.X, b.Y, r.X, r.Y))
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
        if (!ex.TideAnnounced)
        {
            ex.TideAnnounced = true;
            ShowPulseMessage("〜 The tracker stirs — something's moving in the deep, far below. The regolith never stays empty for long. Don't linger.");
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
        if (_surface is { } ex)
        {
            ex.Catches++;
        }
        _heat = EncounterRule.RaiseHeat(_heat, 1, SimTime);
        // #379 · Evening wind #19: "if they get to skin, that is a different thing." A hand on you is not a
        // sighting — a big, FLAT nerve lump that bypasses the diminishing-sighting rule and the S-curve, so
        // touch always hurts noticeably. Debounced by the same catch cadence above, so a brush is not a
        // stunlock. (The gauge only shows on-excursion, but the nerve carries — a mauling follows you aboard.)
        _nerve = NerveModel.Shock(_nerve, NerveModel.TouchShock);
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage("🩸 An Old One lays hands on you — it wants no loot, only you. Tear free and RUN!");
        RequestVaultSave();
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
    private static readonly (string Item, int Price, string Line)[] KioskStock =
    [
        ("the local souvenir tee", 15, "(keyed to the walked body — see VisitKiosk)"),
        ("a fridge magnet", 8, "It clamps to your suit's chestplate and refuses to let go. Value: eternal."),
        ("a vacuum-sealed hot meal", 12, "The label promises 'MEAT-ADJACENT'. The heater still works. Mostly."),
    ];

    private int _kioskPicks;

    private void VisitKiosk()
    {
        if (_surface is not { } ex)
        {
            return; // the kiosk only sells on the ground it stands on
        }
        int slot = _kioskPicks % KioskStock.Length;
        (string item, int price, string line) = KioskStock[slot];
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

    private DeckView.SurfaceHud? BuildSurfaceHud()
    {
        if (_surface is not { } ex)
        {
            return null;
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
        foreach (TreasureCache c in _caches.CachesAt(bodyId))
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

        _hudHusks.Clear();
        foreach ((double hx, double hy) in ex.Husks)
        {
            _hudHusks.Add((hx, hy));
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

        (string Line, int Severity)? orbit = SurfaceOrbitComms(); // #327: the ship calling home

        return new DeckView.SurfaceHud(
            TrackerCaptions: BuildTrackerCaptions(ex, _hudMarks.Count),
            // #371 Phase 3: the one progress bar serves both channels — a dig OR a forced door.
            DigProgress: ex.Channel?.Progress ?? ex.DoorChannel?.Progress ?? -1,
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
            SweptSquares: _hudSwept,
            DarkRegions: BuildDarkRegions(ex),   // #371 Phase 3: born-dark / explored appended chambers
            Echoes: BuildEchoes(ex));            // #371 Phase 3: fading "movement was here" ripples
    }

    // #324: the contextual surface keybar. The owner couldn't find the deploy key — so while a bot rides
    // the sling it spells out [T] deploy, and a chest in hand spells [G] drop. Affordances never hide.
    private string BuildSurfaceKeyHints(SurfaceExcursion ex)
    {
        var parts = new List<string> { "WASD — move", "E — dig / use" };
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

        // The sentry affordance — spell out T while it matters (a bot in the sling to set, or ones holding
        // the line). The tide never stops, so the caption tells the truth: they buy time, not safety.
        int carried = ex.Bots.Count(b => !b.Deployed);
        int deployed = ex.Bots.Count(b => b.Deployed);
        if (carried > 0)
        {
            lines.Add($"🤖 T — set a sentry ({carried} in the sling)");
        }
        else if (deployed > 0)
        {
            lines.Add($"🤖 {deployed} sentry holding — buys time, not safety");
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
