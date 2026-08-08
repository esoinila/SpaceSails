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

    // ── #583 · A REPO CREW ON FOOT. Owner: "FBI does not arrest cars ... they look for the driver". ────
    //
    // They are not Old Ones and they do not behave like them: they walk, they spread out, they do not tire,
    // and what happens if one reaches you is a WRIT, not a mauling. Client-owned position, exactly like a
    // Reever's — never saved, rebuilt from the seeded roll on any reload.
    private sealed class Collector
    {
        public double X, Y, Facing;
        public double Vx, Vy;

        // The stable handedness ReeverChase.Step wants so a wall is rounded rather than dithered at. Spread
        // across the party so they flow around a slab from both ends instead of queueing at one corner.
        public int WallSide = 1;
    }

    private readonly List<Collector> _collectors = [];

    // The engine ceiling for the buffer arithmetic below (CollectorLanding.PartySize is clamped to 4).
    private const int MaxCollectors = 4;

    /// <summary>
    /// #633 · HOW MANY FIGURES A SURFACE PLAN HAS TO BE ABLE TO DRAW, in one place, as the sum of its bands
    /// rather than a number somebody typed. The two branches each maintained this expression separately and
    /// each dropped the other's band: one read <c>3 + ReeverEngineCeiling + MaxCollectors</c>, the other
    /// <c>3 + ReeverEngineCeiling + InspectionTeam.TeamSize</c>. Both were correct on their own branch and
    /// both are wrong now, which is precisely why the sum lives here and every caller reads it.
    ///
    /// <para>The bands, in buffer order: the crew (3), the pack (<see cref="ReeverEngineCeiling"/>), the repo
    /// crew (<see cref="MaxCollectors"/>), the sweep team (<c>InspectionTeam.TeamSize</c>).
    /// <see cref="FillSurfaceDroids"/> writes them at exactly these offsets.</para></summary>
    private const int SurfaceDroidCount =
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
            excursion.Bots.Add(new SurfaceBot { Unit = b.Unit, Rounds = b.Rounds, Deployed = false });
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

        // #573 · INSIDE THE SHELTER, NOTHING IS SPENT. Owner, twice and unambiguously: "it should not be
        // possible to run out of air inside the emergency shelter" / "air should not be expended while in it
        // at all". Its sign has read PRESSURISED since the day it was built, and a suit standing in an
        // atmosphere is not drawing on its tank.
        //
        // Checked BEFORE the drain and returning outright, so there is no ordering by which a captain
        // sitting in a refuge can suffocate in it. The tank does not tick up either — the rack does that,
        // deliberately and with a ceiling; simply standing here is safety, not resupply.
        // #585 · UNDERGROUND, THE FLOOR DECIDES. Owner's biggest open question, answered with a beat in it:
        // B1 still holds pressure, so it is a refuge exactly like a shelter - the tank stops and the nerve
        // steadies. Everything below is dead, so depth is paid for in air and every stair down is a decision
        // about getting back up. Checked before the drain, like the shelter branch, so no ordering can
        // suffocate a captain standing in a pressurised corridor.
        //
        // #612 · AND THE WHOLE QUESTION IS NOW ASKED IN CORE. These were three conditions in a row here, and
        // the readout that reported them was a fourth condition somewhere else — which is the exact shape of
        // every expensive bug this project has filed: two places working the same answer out separately, and
        // one of them edited. SuitAir.SourceOf is the one predicate. The drain branches on ITS answer, the
        // gauge is handed the same answer, and the plate by the lift asks it the same way — so nothing on
        // screen can report a rule the sim is not running.
        //
        // The order inside SourceOf is this method's own order and must stay so: floor, then refuge, then
        // shelter, then ship.
        int inside = ShelterUnderfoot(ex);
        SuitAir.Supply supply = AirSupplyOf(ex);
        AnnounceAirSupply(supply, roomSpeaksForItself: inside >= 0 || RefugeUnderfoot(ex) >= 0);

        if (ex.Floor < 0)
        {
            // What the FLOOR provides on its own — the identical question HiveInterior's plate asks of the
            // same level, which is why the sign on the wall and the tank on your back cannot come apart.
            if (SuitAir.SourceOf(ex.Stop.Body.Id, ex.Floor, insideShelter: false, aboard: false)
                == SuitAir.Supply.Room)
            {
                ex.RefugeBreathNoted = false;
                return;
            }

            // ── #608 · THE REFUGE ON A DEAD FLOOR ────────────────────────────────────────────────────────
            //
            // Owner: "there should be like at least one air replenish station in each of the airless labs
            // underground... for pure safety" — and, the reason, "otherwise the elevator being busy could
            // kill employees".
            //
            // The SAME two things a shelter does, in the same order and by the same functions: the drain
            // stops because you are standing in an atmosphere, and the rack pumps on its own for as long as
            // you care to stand there. Checked BEFORE the drain and returning outright, exactly like the
            // shelter branch below, so there is no ordering by which a captain sitting in a refuge can
            // suffocate in it.
            //
            // What it does NOT do is make the floor free. It is one room, never beside the lift, and its
            // regulator stops at the same two thirds somebody set on the surface for the next person
            // through the door — so depth still costs air (#585), and the refuge buys RANGE.
            int refuge = RefugeUnderfoot(ex);
            if (refuge >= 0)
            {
                if (!ex.RefugeBreathNoted)
                {
                    ex.RefugeBreathNoted = true;
                    ShowPulseMessage(UndergroundComplex.RefugeBreathingLine);
                    string found = SurfaceShelter.PartialLine(
                        RefugeReservoirNow(ex, refuge) / SurfaceShelter.ReservoirSeconds);
                    if (found.Length > 0)
                    {
                        // The same fact told by state rather than by a card, and down here it is a colder
                        // one: a rack in a sealed room a hundred and fifty metres under a moon has been
                        // drawn on, and the building has been shut for decades.
                        ShowAndFile(found, "🫁");
                    }
                }

                ex.RefugeReservoir[RefugeKey(ex.Floor, refuge)] = DrawFromRack(
                    ex, RefugeReservoirNow(ex, refuge), dtRealSeconds, out double intoTheTank);
                if (intoTheTank > 0)
                {
                    if (ex.RefugePumpNoted.Add(refuge))
                    {
                        ShowPulseMessage(SurfaceShelter.PumpingLine);
                    }
                }
                else if (ex.RefugePumpNoted.Contains(refuge) && ex.RefugePumpNoted.Add(-refuge - 1))
                {
                    ShowPulseMessage(SurfaceShelter.PumpDoneLine);
                }
                return;
            }
            ex.RefugeBreathNoted = false;

            // Anywhere else on a dead floor drains exactly like open regolith: this is the price of going
            // deeper, and it is the only thing stopping the facility from being somewhere to live.
        }

        if (inside >= 0)
        {
            if (!ex.ShelterBreathNoted)
            {
                ex.ShelterBreathNoted = true;
                ShowPulseMessage(SurfaceShelter.BreathingLine);
                string story = SurfaceShelter.PartialLine(
                    ShelterReservoirNow(ex, inside) / SurfaceShelter.ReservoirSeconds);
                if (story.Length > 0)
                {
                    // "Somebody was here" is a fact about the world told by state rather than by a card —
                    // exactly the kind of thing that was being lost eight seconds after it was earned.
                    ShowAndFile(story, "⛺");
                }
            }

            // #573 · THE RACK ALWAYS GIVES, and the PUMPING TIME is the cost. Owner: "it should always give
            // some more air... like a steady production rate... The time it takes to pump air is good
            // incentive to not take too much." It replaced a one-shot draw that could be SPENT, which had a
            // nasty failure he walked into: stranded beside an empty rack with nothing to do but die. A
            // cracker that always produces cannot strand anybody, and standing in a shed while the Old Ones
            // keep walking prices the top-up far better than an empty state ever did.
            ex.ShelterReservoir[inside] = DrawFromRack(ex, ShelterReservoirNow(ex, inside),
                dtRealSeconds, out double pumped);
            if (pumped > 0)
            {
                if (ex.ShelterPumpNoted.Add(inside))
                {
                    ShowPulseMessage(SurfaceShelter.PumpingLine);
                }
            }
            else if (ex.ShelterPumpNoted.Contains(inside) && ex.ShelterPumpNoted.Add(-inside - 1))
            {
                ShowPulseMessage(SurfaceShelter.PumpDoneLine);
            }
            return;
        }
        ex.ShelterBreathNoted = false;

        // Inside the ship or in her tube you are breathing hers, and the tank tops up. This is the ONLY
        // place it refills (bar a cache found out in the world), which is what makes the tube the anchor
        // the whole supply line hangs from (#562).
        if (supply == SuitAir.Supply.Ship)
        {
            ex.AirSeconds = SuitAir.Refill(ex.AirSeconds, dtRealSeconds * TubeRefillRate);
            ex.AirWarned = false;   // re-arm the warnings: the next walk out gets told again
            ex.AirLowWarned = false;
            ex.ReserveNoted = false;
            return;
        }

        // #612 + #608 · THE DRAIN IS GATED ON THE SAME PREDICATE THE GAUGE READS.
        //
        // Every branch above has already returned for its own reason — it had a rack to run or a tank to top
        // up, which this cannot express. What it CAN do is make the two answers impossible to disagree: the
        // suit does not spend anything the hud has just told the captain it is not spending. Nothing reaches
        // here that the predicate calls not-drawing, so this line does nothing today; the day somebody adds a
        // fifth way to breathe and forgets one of the branches above, it is the difference between a wrong
        // colour and a death. It reads the VALUE the gauge was handed, not a fresh call — a second call is a
        // second chance to answer differently.
        if (!SuitAir.Drawing(supply))
        {
            return;
        }

        // #573 · BREATHING RATE. What you are doing, how frightened you are, and how hurt — the owner's
        // diving rule ("keep calm so the O2 does not run out"), which makes holding your nerve an actual
        // move rather than a mood.
        double moved = Math.Sqrt(((_avatarX - _airLastX) * (_avatarX - _airLastX))
            + ((_avatarY - _airLastY) * (_avatarY - _airLastY)));
        (_airLastX, _airLastY) = (_avatarX, _avatarY);

        double speed = dtRealSeconds > 0 ? moved / dtRealSeconds : 0;
        double exertion = speed < 0.5 ? SuitAir.Breathing.Still
            : speed > 7.0 ? SuitAir.Breathing.Running
            : SuitAir.Breathing.Walking;

        double rate = SuitAir.Breathing.Rate(exertion, _nerve, ex.HitsTaken, CaptainCondition.MaxHits);
        ex.AirSeconds = SuitAir.Drain(ex.AirSeconds, dtRealSeconds * rate);

        // Say it once when the breathing itself becomes the problem. Not a nag — a diagnosis, and a hint
        // that standing still is a move.
        if (!ex.HardBreathingNoted && rate >= SuitAir.Breathing.WorthMentioning)
        {
            ex.HardBreathingNoted = true;
            ShowPulseMessage(SuitAir.Breathing.HardBreathingLine);
        }
        else if (rate < SuitAir.Breathing.WorthMentioning * 0.8)
        {
            ex.HardBreathingNoted = false;   // re-arm once they have calmed down
        }

        double home = DistanceToTheTube();

        // #696 · Did an alarm go off on this tick? A captain must never suffocate inside a silent hold, and
        // a warning that plays while they are watching a progress bar fill is #564's forbidden silent timer
        // wearing a costume. Collected across the three thresholds and acted on once, below.
        bool alarmed = false;

        // THE LINE. Once, on the step it is crossed, while there is still a decision in it.
        if (!ex.AirWarned && SuitAir.PastPointOfNoReturn(ex.AirSeconds, home))
        {
            ex.AirWarned = true;
            alarmed = true;
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage(SuitAir.CrossingWarning);
        }

        // #573 · THE SECONDARY PACK CUTS IN. The EMU's real half-hour reserve, and unlike everything else
        // here it is NOT distance-gated: the primary being gone is worth saying wherever you are standing.
        if (!ex.ReserveNoted && SuitAir.OnTheReserve(ex.AirSeconds))
        {
            ex.ReserveNoted = true;
            alarmed = true;
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage(SuitAir.ReserveEngagedLine);
        }

        // #573 · AND the absolute low mark, which is the one that can actually fire in a field this size.
        // Without it a captain dies flat, having been warned about nothing — the silent timer the whole
        // mechanic forbids. It also raises the CARD, once per captain, because running out of air ends the
        // run and the owner is right that it deserves more than a toast that scrolls past.
        if (!ex.AirLowWarned && SuitAir.RunningLow(ex.AirSeconds, home))
        {
            ex.AirLowWarned = true;
            alarmed = true;
            RendererInterop.PlayCue("alarm");
            if (!ShowAirCardOnce())
            {
                ShowPulseMessage(SuitAir.LowAirWarning(ex.AirSeconds, home));
            }
        }

        // #696 · THE ALARM TAKES YOUR HANDS OFF THE PAPER. Note which way this dependency points: the suit
        // interrupts the darkroom, the darkroom knows nothing about the suit. Each threshold is one-shot per
        // walk, so this is a BEAT and never a lockout — the next press starts the same hold again, and a
        // captain who wants to finish reading a manifest on the reserve is allowed to make that decision.
        if (alarmed)
        {
            ProcessingIsInterrupted(Core.Processing.Interruption.Alarm);
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

    // #573 · Last frame's position, for working out whether the captain is standing, walking or running.
    // Speed is not otherwise tracked on the surface, and the difference between a stroll and a sprint is the
    // whole of the owner's "keep calm" rule.
    private double _airLastX;
    private double _airLastY;

    // ── #612 · WHERE THE AIR IS COMING FROM. ────────────────────────────────────────────────────────────
    //
    // The last answer the predicate gave, kept ONLY so the crossing can be said once. It is deliberately not
    // what anything DISPLAYS — a cached copy of a fact is a second source of that fact, and this is the one
    // fact in the game that must not have two. Null before an excursion has ticked, which is also what stops
    // the crossing line firing on the frame a captain lands.
    private SuitAir.Supply? _airSupplyNoted;

    /// <summary>#612 · THE CROSSING, SAID ONCE. Owner: <i>"maybe pop-up about you have air or you are in
    /// vacuum type ... it is vital info :-D"</i>.
    ///
    /// <para>It fires only where the tank STARTS or STOPS, never on Room→Ship (both are free, and a line
    /// about a change that costs nothing is the nag that turns a vital fact into wallpaper), and never on
    /// the first tick of an excursion. A room with a DOOR is left to say it in its own voice —
    /// <c>SurfaceShelter.BreathingLine</c> and <c>UndergroundComplex.RefugeBreathingLine</c> are already the
    /// better sentences for those thresholds, and two lines for one door is exactly the noise the tank
    /// mechanic was told not to become. What is left is the crossing nothing else narrates: stepping out of
    /// the car onto a floor that holds or does not, and leaving her tube for the regolith.</para></summary>
    private void AnnounceAirSupply(SuitAir.Supply supply, bool roomSpeaksForItself)
    {
        SuitAir.Supply? was = _airSupplyNoted;
        _airSupplyNoted = supply;

        if (was is null || SuitAir.Drawing(was.Value) == SuitAir.Drawing(supply) || roomSpeaksForItself)
        {
            return;
        }

        RendererInterop.PlayCue("blip");
        ShowPulseMessage(SuitAir.SupplyChangedLine(supply));
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

    // #573 · The captain has read what the tank is doing. Dismiss() hands the keyboard back — and here that
    // matters more than anywhere: this card opens while the air is already going, so a swallowed keypress
    // is spent air.
    private void CloseAirCard()
    {
        _airCardOpen = false;
    }

    // ── #573 · THE SHELTER'S CHARGING RACK [E]. The only place outside her tube that refills a suit, and
    //    therefore the only reason the deep field is worth crossing rather than merely looking at. ──
    /// <summary>#573 · The fixed places the fan should point at: the way home, and every shelter. Bearings
    /// and ranges from the captain, so the tracker answers "which way" for somewhere that does not move.</summary>
    private List<(double Bearing, double Range, bool IsHome, bool IsLab)> BuildBeacons(SurfaceExcursion ex)
    {
        var list = new List<(double, double, bool, bool)>();
        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _))
        {
            return list;   // a hull has neither a tube mouth nor a shelter
        }

        void Add(double x, double y, bool home, bool lab = false)
        {
            double dx = x - _avatarX, dy = y - _avatarY;
            list.Add((Math.Atan2(dy, dx), Math.Sqrt((dx * dx) + (dy * dy)), home, lab));
        }

        // ── #591 · UNDERGROUND, THE BEACONS ARE DIFFERENT PLACES ──
        //
        // Owner, on B1: "now that we are underground the elevator would be nice to be on the motion detector,
        // the surface hut's are really not that relevant down here".
        //
        // He is right, and it is the same fault as the reach: these are SURFACE beacons. The tube mouth and
        // the shelters are up a lift shaft and several hundred metres of rock away, so painting them on the
        // fan down here is not merely useless — it is actively wrong, because the way home ring is the one
        // the captain reads when the air gets short and it would be pointing at a hut they cannot reach.
        //
        // Down here there is exactly ONE place worth a ring, and it is the same one every floor: the lift.
        // It is the way home in the only sense that matters underground, so it takes the HOME flag and the
        // calm colour that goes with it — a place, not a thing that moves.
        if (ex.Floor < 0)
        {
            (double liftX, double liftY) = UndergroundComplex.ShaftAt(MoonSurface.ExpeditionField());
            Add(liftX, liftY + UndergroundComplex.CorridorHalf + 2.5, home: true);

            // ── #608 · AND THE REFUGES, WHICH ARE THIS FLOOR'S SHELTERS ──
            //
            // Owner, exactly: "and those need to show in the motion detector, not the surface ones, when you
            // are 150 meters below surface."
            //
            // That sentence is the whole rule and both halves of it are load-bearing. The surface shelters
            // are up a shaft and several hundred metres of rock away, so painting them here would be the map
            // lying (#573) in its most expensive form — a ring on the instrument a captain would spend the
            // last of a tank walking toward. They are already gone, and they stay gone: this branch never
            // touches SheltersOn.
            //
            // What replaces them is the thing the ring MEANS. On the regolith a not-home ring is "air you
            // can reach that is not the ship"; on a dead floor that is the refuge, and it deserves the same
            // colour because it is the same promise. It also answers #608's hardest requirement — "a refuge
            // you discover AFTER you needed it is a cruelty" — without a map, a paper or a tutorial: the
            // instrument the captain already watches simply has it on it.
            //
            // Nothing is painted on a floor that holds pressure, because there is nothing to point at: the
            // whole floor is the refuge, and a ring saying "air, 40 du that way" while you are standing in
            // air is an instrument disagreeing with the room.
            foreach ((double rx, double ry) in RefugesOn())
            {
                Add(rx, ry, home: false);
            }
            return list;
        }

        Add(MoonSurface.SpawnX, MoonSurface.SpawnY, home: true);
        foreach (SurfaceStructure.Spec shelter in SheltersOn(ex))
        {
            Add(shelter.CentreX, shelter.CentreY, home: false);
        }

        // #585/#584 · AND THE LIFT HEAD, once the door is known. Owner, standing in a ruin that happened to
        // have a violet door: "it should be this space? but how do I get in this has purple door and is not
        // emergency shelter?"
        //
        // Two failures behind that one sentence. First, the HUD has been saying "E at the ⊙ HIDDEN DOOR —
        // force the secret lab open" while nothing anywhere says WHERE it is (#584, filed before he hit it and
        // then hit anyway). A prompt you cannot act on is worse than silence.
        //
        // Second, mine and worse: I gave imported violet to shelters (always), to about one ruin door in
        // seven, AND to the lift head — so a colour that was supposed to mean "somebody shipped this here"
        // now means "some doors", and the one door it most needed to distinguish was lost among them. A
        // signal that fires on three unrelated things is not a signal.
        //
        // The beacon is the honest fix: the ground can carry as many violet doors as the fiction wants, and
        // the INSTRUMENT says which one is the way down.
        if (ex.SecretLabDoorRevealed && ex.Lab is { HasLab: true } lab)
        {
            Add(lab.DoorX, lab.DoorY, home: false, lab: true);
        }

        return list;
    }

    /// <summary>#573 · Your own buried caches, as marks on the fan — but ONLY once they are inside its
    /// reach. The range gate is the entire design: a field this size made finding your own ✗ a real task,
    /// and an instrument that always knew where it was would take that task straight back off you.</summary>
    private List<(double Bearing, double Range)> BuildCacheBeacons()
    {
        var list = new List<(double, double)>();
        foreach ((double mx, double my, bool _) in _hudMarks)
        {
            double dx = mx - _avatarX, dy = my - _avatarY;
            double range = Math.Sqrt((dx * dx) + (dy * dy));
            if (range <= CacheDetectRangeDu)
            {
                list.Add((Math.Atan2(dy, dx), range));
            }
        }
        return list;
    }

    /// <summary>How close a buried cache has to be before the fan admits to it.</summary>
    private const double CacheDetectRangeDu = 55.0;

    /// <summary>#573 · What the captain has been TOLD, as opposed to what they can see — a wide, soft wash
    /// rather than a mark. A tip narrows a search; it does not end one, and a dot would claim a precision
    /// the information does not have.</summary>
    private List<(double Bearing, double Range, double Spread)> BuildRumours(SurfaceExcursion ex)
    {
        var list = new List<(double, double, double)>();
        if (ex.Lab is not { HasLab: true } lab || ex.SecretLabDoorRevealed)
        {
            return list;
        }
        // #585: a tip you were GIVEN counts, not only a place you have already been. This used to read only
        // _secretLabsFound, so the wash helped on a return visit and did nothing on the first — the one visit
        // where a captain actually needs help. The clue chain had no way into the instrument at all.
        if (!_labLeads.Contains(ex.Stop.Body.Id) && !_secretLabsFound.Contains(ex.Stop.Body.Id))
        {
            return list;   // nobody has tipped you about this one; there is nothing to be vague about
        }

        double dx = lab.DoorX - _avatarX, dy = lab.DoorY - _avatarY;
        list.Add((Math.Atan2(dy, dx), Math.Sqrt((dx * dx) + (dy * dy)), RumourSpreadDu));
        return list;
    }

    /// <summary>How wide a rumour reads on the fan, in deck units. Big — it is a tip, not a fix.</summary>
    private const double RumourSpreadDu = 45.0;

    // ── #573 · TURNING OVER A RUIN [E]. About half of them hold something; the rest are somebody's empty
    //    house, and finding those is what makes the others worth the air it cost to walk in. ──
    private void RuinSalvageInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.RuinSalvage } spot)
        {
            return;
        }

        // Identify WHICH ruin by its position — the console sits at the building's centre, which is the
        // stable key SurfaceLayout hands out.
        string body = ex.Stop.Body.Id, salt = ex.Site.LayoutSalt;
        SurfaceLayout.Plan plan = SurfaceLayout.For(body, MoonSurface.ExpeditionField(), salt);
        IReadOnlyList<(double X, double Y)> centres = plan.BuildingCentres ?? [];

        int which = -1;
        for (int i = 0; i < centres.Count; i++)
        {
            if (Math.Abs(centres[i].X - spot.X) < 0.5 && Math.Abs(centres[i].Y - spot.Y) < 0.5)
            {
                which = i;
                break;
            }
        }
        if (which < 0)
        {
            return;
        }

        string key = $"{which}";
        if (!ex.RuinsSearched.Add(key))
        {
            ShowPulseMessage("You have already been through this one.");
            return;
        }

        switch (SurfaceSalvage.WhatIsInside(body, salt, which))
        {
            case SurfaceSalvage.Find.Rounds:
            {
                int rounds = SurfaceSalvage.RoundsIn(body, salt, which);
                var takers = ex.Bots.Where(b => b.Rounds < SentryBot.MaxMagazine).ToList();
                int left = rounds;
                foreach (SurfaceBot bot in takers)
                {
                    int take = Math.Min(SentryBot.MaxMagazine - bot.Rounds, left);
                    bot.Rounds += take;
                    left -= take;
                    if (left <= 0)
                    {
                        break;
                    }
                }
                RendererInterop.PlayCue("board");
                ShowPulseMessage(SurfaceSalvage.RoundsLine(rounds - left));
                break;
            }

            case SurfaceSalvage.Find.Goods:
            {
                int credits = SurfaceSalvage.GoodsIn(body, salt, which);
                _credits += credits;
                RendererInterop.PlayCue("board");
                ShowAndFile(SurfaceSalvage.GoodsLine(credits), "💰");
                break;
            }

            case SurfaceSalvage.Find.Papers:
                // Texture, never testimony (#563): a roster, a docket, a note in a locker. Nothing here
                // explains what is outside, and nothing ever will.
                ShowAndFile(SurfaceSalvage.PapersLine(body, salt, which), "📄");
                ApplyNerveShock(2.0, "somebody else's paperwork, still where they left it");
                AssembleSomebody(ex, body, salt, which);   // #588: a person, out of the pieces

                // #585 · AND SOMETIMES A PLACE NAME. This is the thread that makes the labs findable at all:
                // a docket in a ruin, read carefully, names a moon somebody was running something on.
                if (DiceRule.Roll(DiceRule.Seed($"lead:papers:{body}:{salt}:{which}"), 3).Face == 1)
                {
                    GrantLabLead(DiceRule.Seed($"lead:pick:{body}:{salt}:{which}"));
                }
                break;

            default:
                ShowAndFile(SurfaceSalvage.EmptyRoomLine(body, salt, which), "🚪");
                break;
        }

        RebuildSurfaceDeck();
        RequestVaultSave();
    }

    /// <summary>Every shelter on this site, in the stable order everything else indexes by.</summary>
    /// <summary>#585 · Every shelter on this site — computed ONCE per excursion and remembered.
    ///
    /// <para>Owner, after the rebuild: <i>"I think it felt a little sluggish at some points."</i> This was
    /// the cost I had just added. <c>SurfaceShelter.SpecsFor</c> is pure but not free — it re-runs the
    /// seeded placement, up to nine shelters over thirty hashed candidate spots each, with a separation
    /// check against everything placed so far. That was fine when it was called twice a frame to draw
    /// beacons. It stopped being fine the moment the threshold rule (#585) called it once PER OLD ONE PER
    /// FRAME: twenty-four hunters × ~270 hash-and-lerp attempts, sixty times a second, to answer a question
    /// whose answer cannot change for the whole excursion.</para>
    ///
    /// <para>Determinism is what makes the cache safe: same body, same salt, same field ⇒ same list, every
    /// time. Cleared with the excursion, so a new site recomputes.</para></summary>
    private IReadOnlyList<SurfaceStructure.Spec> SheltersOn(SurfaceExcursion ex)
    {
        if (ex.ShelterSpecs is { } cached)
        {
            return cached;
        }
        IReadOnlyList<SurfaceStructure.Spec> specs = Derelict.TryParseWreckId(ex.Stop.Body.Id, out _)
            ? []
            : SurfaceShelter.SpecsFor(ex.Stop.Body.Id, ex.Site.LayoutSalt, MoonSurface.ExpeditionField());
        ex.ShelterSpecs = specs;
        return specs;
    }

    /// <summary>#585 · Walk a body out of solid mass it has ended up inside — a wall that was built around
    /// it rather than one it walked into. Tries short steps outward on a ring of bearings and takes the first
    /// that is open ground; gives up rather than loop, because a contact stuck in stone is a curiosity and a
    /// frame that never ends is a crash.</summary>
    private static (double X, double Y) ExtricateFromStone(
        double x, double y, IReadOnlyList<SurfaceCollision.Segment> walls, double radius)
    {
        if (!SurfaceCollision.Blocked(x, y, radius, walls))
        {
            return (x, y);
        }

        for (double reach = 1.5; reach <= 18.0; reach += 1.5)
        {
            for (int i = 0; i < 12; i++)
            {
                double a = i / 12.0 * Math.Tau;
                double tx = x + (Math.Cos(a) * reach), ty = y + (Math.Sin(a) * reach);
                if (!SurfaceCollision.Blocked(tx, ty, radius, walls))
                {
                    return (tx, ty);
                }
            }
        }
        return (x, y);
    }

    /// <summary>#585 · Push a body back out of any shelter it has ended up inside. The door reads a suit;
    /// nothing else on this ground gets to be in there. Cheap: a site carries a handful of shelters and the
    /// common case is a single Contains() that says no.</summary>
    private (double X, double Y) HoldOutsideShelters(double x, double y)
    {
        if (_surface is not { } ex)
        {
            return (x, y);
        }
        foreach (SurfaceStructure.Spec spec in SheltersOn(ex))
        {
            (x, y) = SurfaceShelter.HoldAtTheThreshold(spec, x, y);
        }
        return (x, y);
    }

    /// <summary>Which shelter the captain is standing inside, or -1.</summary>
    private int ShelterUnderfoot(SurfaceExcursion ex)
    {
        IReadOnlyList<SurfaceStructure.Spec> all = SheltersOn(ex);
        for (int i = 0; i < all.Count; i++)
        {
            if (SurfaceShelter.Contains(all[i], _avatarX, _avatarY))
            {
                return i;
            }
        }
        return -1;
    }

    private bool StandingInTheShelter(SurfaceExcursion ex) => ShelterUnderfoot(ex) >= 0;

    /// <summary>A rack's reservoir right now, in suit-seconds. A rack nobody in this run has touched is full
    /// — unless somebody ELSE drew on it, which is the whole "finding one not full means somebody was here"
    /// story, told by state rather than by a card.</summary>
    private double ShelterReservoirNow(SurfaceExcursion ex, int index)
    {
        if (index < 0)
        {
            return 0;
        }
        if (ex.ShelterReservoir.TryGetValue(index, out double held))
        {
            return held;
        }
        double start = SurfaceShelter.SomebodyWasHere(ex.Stop.Body.Id, ex.Site.LayoutSalt, index)
            ? SurfaceShelter.ReservoirSeconds * 0.42
            : SurfaceShelter.ReservoirSeconds;
        ex.ShelterReservoir[index] = start;
        return start;
    }

    // ── #608 · ONE RACK LAW, TWO BUILDINGS ──────────────────────────────────────────────────────────────
    //
    // The underground refuges are the surface shelter's mechanic, underground — so they are the surface
    // shelter's CODE, not a second copy of it. Everything that decides how much air moves lives in
    // SurfaceShelter (Produce, Transfer, the two-thirds ceiling somebody set for the next person through the
    // door) and both callers step through this one function.
    //
    // The reason it is a function rather than a comment saying "keep these in sync": this project's most
    // expensive habit is two places that have to agree and only one being changed, and the two racks are a
    // perfect candidate — they will be tuned by somebody reading one of them. #573's shelter and #608's
    // refuge now cannot drift, because there is nothing to drift.

    /// <summary>#612 + #608 · IS THE TANK RUNNING? ONE ANSWER, READ BY THE SIM AND BY THE GAUGE.
    ///
    /// <para>#612 shipped the <c>AIR: TANKS / ROOM</c> source on the hud because the owner asked <i>"where
    /// here does it say if I consume tanks or have air?"</i> — and its own issue states the hard part: the
    /// gauge <b>"has to agree with the plate by the lift on every floor. Two instruments disagreeing about
    /// whether you can breathe is worse than one instrument saying nothing."</b></para>
    ///
    /// <para>It was computed as its own expression beside <c>StepSuitAir</c>'s branches, which is the exact
    /// arrangement this project keeps paying for: two places that must agree, and only one gets changed. It
    /// did not survive its first contact with a new way to breathe. A refuge (#608) stops the drain, and the
    /// hud went on reading TANKS while the sim was not spending anything — a captain sitting in air being
    /// told, in colour, that their tank was running out.</para>
    ///
    /// <para>So the drain is gated on this and the gauge is fed from this. Anything that ever becomes a new
    /// place to breathe is added HERE, once, and both follow.</para>
    ///
    /// <para><b>And "here" is now Core</b>, because this client-side version could still only be read by a
    /// client: the plate <c>HiveInterior</c> paints by the lift was calling
    /// <c>UndergroundComplex.HoldsPressure</c> for itself and spelling its own words, which made a THIRD
    /// answer to the same question. <see cref="SuitAir.SourceOf"/> is the predicate; this method is the one
    /// place that gathers the four facts to hand it, and every surface reads what it says.</para></summary>
    /// <para><b>#621 · and the third fact was answered with the wrong world's rule.</b> "Aboard" was
    /// <c>MoonSurface.IsSafeAboard(_avatarY)</c> — the regolith's top rim at y = −20 — while a derelict's
    /// whole deck runs −9 to +9, so every point aboard every wreck said YES. The gauge told a captain
    /// standing in a hull that has held vacuum for years that they were on HER AIR and their tank was
    /// FILLING, and the drain agreed with it. <see cref="AwayTeamSide.BackAtTheShuttle"/> is the one place
    /// that knows which door you are on the far side of, and both the reach rule and this one now read
    /// it.</para>
    private SuitAir.Supply AirSupplyOf(SurfaceExcursion ex) =>
        SuitAir.SourceOf(
            ex.Stop.Body.Id,                                 // #677 which building — the halls breathe
            ex.Floor,
            StandingInTheShelter(ex),                        // #573 the deep shelter
            CaptainBeyondReach,                              // her tube — or past a wreck's lock: breathing hers
            ex.Floor < 0 && RefugeUnderfoot(ex) >= 0);       // #608 a pressure refuge on a dead floor

    /// <summary>Is the tank running? The one bit of <see cref="AirSupplyOf"/>, for callers that want no
    /// more than that.</summary>
    private bool TankIsDrawing(SurfaceExcursion ex) => SuitAir.Drawing(AirSupplyOf(ex));

    /// <summary>Run one rack for <paramref name="dt"/> seconds: it makes air, it moves what it can into the
    /// suit, and the warnings re-arm if anything went in. Returns the reservoir it is left holding, and
    /// reports how much reached the tank.</summary>
    private double DrawFromRack(SurfaceExcursion ex, double held, double dt, out double pumped)
    {
        double made = SurfaceShelter.Produce(held, dt);
        pumped = SurfaceShelter.Transfer(ex.AirSeconds, made, SuitAir.TankSeconds, dt);
        if (pumped > 0)
        {
            ex.AirSeconds = SuitAir.Refill(ex.AirSeconds, pumped);
            ex.AirLowWarned = false;
            ex.AirWarned = false;
            ex.ReserveNoted = false;
        }
        return made - pumped;
    }

    /// <summary>#608 · The refuges on the floor the captain is standing on, read off the deck the renderer
    /// actually drew.
    ///
    /// <para><b>Not rebuilt from Core.</b> <c>UndergroundComplex.Build</c> is pure but not free, and this is
    /// asked every frame by the suit; more importantly, a second call would be a second answer. The consoles
    /// on <c>_deckPlan</c> ARE the refuges — <see cref="HiveInterior.FloorDeck"/> put them there off the
    /// floor plan — so the room the captain can see and the room that holds their air are the same object by
    /// construction rather than by two functions agreeing.</para></summary>
    private List<(double X, double Y)> RefugesOn()
    {
        var found = new List<(double, double)>();
        foreach (DeckPlan.ConsoleSpot spot in _deckPlan.Consoles)
        {
            if (spot.Kind == DeckPlan.ConsoleKind.HiveRefuge)
            {
                found.Add((spot.X, spot.Y));
            }
        }
        return found;
    }

    /// <summary>Which refuge the captain is standing inside, or -1. Never anything but -1 above ground.</summary>
    private int RefugeUnderfoot(SurfaceExcursion ex)
    {
        if (ex.Floor >= 0)
        {
            return -1;
        }
        List<(double X, double Y)> all = RefugesOn();
        for (int i = 0; i < all.Count; i++)
        {
            if (UndergroundComplex.RefugeHolds(all[i].X, all[i].Y, _avatarX, _avatarY))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>A refuge rack's reservoir, in suit-seconds — the shelter's own story, told about a room
    /// under a moon. Keyed per FLOOR, so walking back into B7's refuge finds it as you left it and B3's is
    /// somebody else's problem.</summary>
    private double RefugeReservoirNow(SurfaceExcursion ex, int index)
    {
        if (index < 0)
        {
            return 0;
        }
        int key = RefugeKey(ex.Floor, index);
        if (ex.RefugeReservoir.TryGetValue(key, out double held))
        {
            return held;
        }

        // #573's idiom, underground: a rack that is not full means SOMEBODY WAS HERE. Down here that is a
        // colder sentence than it is on the regolith — the building has been shut for decades and the seals
        // on this room have not — and it costs nothing but a seeded roll.
        double start = SurfaceShelter.SomebodyWasHere(
                ex.Stop.Body.Id, $"{ex.Site.LayoutSalt}:hive{ex.Floor}", index)
            ? SurfaceShelter.ReservoirSeconds * 0.42
            : SurfaceShelter.ReservoirSeconds;
        ex.RefugeReservoir[key] = start;
        return start;
    }

    /// <summary>One key per refuge per floor, so B2's rack is not B3's. Same shape as
    /// <see cref="HiveInterior.RoomKey"/>, and deliberately a different dictionary.</summary>
    private static int RefugeKey(int level, int index) => (level * 1000) - index;

    // ── #585 · THE HIVE: down the shaft, and back up ───────────────────────────────
    //
    // Owner: "I just don't want the secret lab to be puny 2 door apartment, but look like it could facilitate
    // a large operation with serious funding."
    //
    // Three calls were made on his behalf when he said "go forward" (all pinned by TheHiveTests, so each is a
    // one-line overrule): you find it by LOOKING (the lift head's door is the one imported thing on the moon),
    // it goes three floors and the bottom is not a bottom, and B1 still holds pressure while everything below
    // it does not. That last one is the whole feel of the place - the top floor lulls you and the rest costs
    // you air.
    private void HiveLiftInteract()
    {
        if (_surface is null)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveLift or DeckPlan.ConsoleKind.HiveHead })
        {
            return;
        }

        // ── #600 · THE PANEL, BECAUSE THE CAR ONLY WENT DOWN ──
        //
        // Owner, on B1: "looks like the elevator only takes me down... how do I get back to the surface with
        // it :-D Am I marooned in a secret lab underground now :-D ?" — then, deciding it: "we should have
        // elevator panel with UI then".
        //
        // This function USED to be the ride: one keypress, always one floor deeper, and the only way out was
        // to reach the bottom of the band first. On a twenty-floor site that meant riding eighteen floors
        // further from the surface, through dead air, to go up. The file's own comment two screens down says
        // a captain trapped on a dead floor is a death; the lift was the thing doing the trapping.
        //
        // It survived #590, #591 and #592 all editing this function, because none of them asked what the UP
        // case did — and the A* audit cannot see a state machine. It proves the captain can REACH the lift,
        // never that the lift is a way HOME.
        //
        // Core owns which buttons exist (UndergroundComplex.LiftPanel) so that the #590 card gate and the
        // #592 silence are ONE pure, tested rule instead of something re-derived in a razor file.
        _liftOutcome = null;
        _showLiftPanel = true;
        RendererInterop.PlayCue("board");
    }

    /// <summary>#600 · The buttons on this car's panel, from where the captain is standing.
    ///
    /// <para>#752 · The whole satchel goes in, and not a second list of ids: the cage's gate reads the
    /// day-labour chit, and whether the captain has cover is a fact about what they are CARRYING
    /// (<c>CanteenTable.Cover</c>) — asked of the same pocket the player can open and look in.</para></summary>
    private IReadOnlyList<UndergroundComplex.LiftStop> LiftStops() =>
        _surface is { } ex
            ? UndergroundComplex.LiftPanel(ex.Stop.Body.Id, ex.Floor, AuthorityCardIds(), _satchel)
            : [];

    /// <summary>#600 · A button was pressed. A refusing button says why and the car does not move — a button
    /// that is present and explains itself is the entire reason it is not simply absent.</summary>
    private void PressLiftButton(UndergroundComplex.LiftStop stop)
    {
        if (_surface is not { } ex || stop.IsCurrent)
        {
            return;
        }
        if (stop.Refusal is { } refused)
        {
            // Said every time — a refusal that goes quiet on the second press reads as a broken button.
            // Filed once, because pressing one gate eleven times is not eleven findings.
            //
            // #686 · And said INSIDE the panel. Owner, at this exact gate: "result text is not displayed
            // so it can be read. It is the same kind of bug as we had with the inventory item use." The
            // panel stays open on a refusal, and a line pulsed from here plays under the backdrop's blur —
            // in the DOM and not on the screen (#680's disease, second organ). The book still gets the
            // record; the saying happens where the player is looking.
            //
            // #684 · And it is the MATRIX that answers now. The panel used to keep a second set of sentences
            // of its own (UndergroundComplex.WrongCardLine, deleted), so the sharpest refusals in the game —
            // another shaft of THIS site, named, versus somebody else's building, named (#679/#683) — had no
            // client caller and nobody ever read them. One source, and this is a caller of it.
            UndergroundComplex.GateRead read =
                UndergroundComplex.TheGateReads(ex.Stop.Body.Id, ex.Floor, _satchel);
            int refusedBand = UndergroundComplex.BandOf(stop.Level);
            if (ex.HiveShaftsRefused.Add(refusedBand))
            {
                _liftOutcome = read.Line;
                FileNote(read.Line, "🔒");

                // #684 · …and it is TOLD. Owner: the read's outcome must be raised as a story card in the
                // house idiom, carrying the matrix's own line. It goes up OVER the panel — the view-object
                // block is the last modal in Map.razor and shares the same backdrop band, so the card is the
                // pop-up that is up, and #736's law is met by the line being ON it rather than only in the
                // panel row underneath. Once per shaft per excursion, off the same latch as the field note:
                // one event, one memory, and two latches would eventually disagree (#751's rule).
                _viewObject = new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                    read.Label, read.ArtUrl, read.Line);
            }
            else
            {
                _liftOutcome = $"🔒 {refused}";
            }
            return;
        }

        _showLiftPanel = false;

        // #689 · Going below this car's own band is the OTHER shaft, and #590's card is what opened it — but
        // the saying of that belongs to the ARRIVAL and not to this line. It used to be said right here, on
        // the exact frame the panel closes and the floor is rebuilt under the captain's feet, and the owner
        // played the whole loop without ever seeing it. The blur disease's third organ: not under a modal
        // this time, under a scene change. The ride carries the stop with it, and the doors say it.
        RideTheLiftTo(ex, stop.Level, stop);
    }

    private void CloseLiftPanel()
    {
        _showLiftPanel = false;
        _liftOutcome = null;
    }

    /// <summary>#600 · Whether the lift panel is up. The sim keeps running behind it, exactly as it does
    /// behind the valve board.</summary>
    private bool _showLiftPanel;

    /// <summary>#686 · What the last refused button answered, said inside the panel itself. The pulse HUD
    /// renders under the modal backdrop's blur, so a refusal routed there is in the DOM and not on the
    /// screen. Cleared whenever the panel opens or closes — the line belongs to one stand at one gate.</summary>
    private string? _liftOutcome;

    /// <param name="via">#689 · The button that was pressed, when a button was pressed. A ride that goes
    /// through the card gate has a story to tell on arrival, and only the panel knows which trip that was —
    /// the dev floor cheat rides the same car and has no gate to cross.</param>
    private void RideTheLiftTo(
        SurfaceExcursion ex, int level, UndergroundComplex.LiftStop? via = null)
    {
        int fromLevel = ex.Floor;
        bool wasUnderground = ex.Floor < 0;
        ex.Floor = level;

        if (level == 0)
        {
            // #602 · Back out INSIDE THE SHED — the box the car came up into. Owner: "I would expect to spawn
            // into the elevator box where we went down with", which is also what the line below has always
            // said out loud ("lets you out into somebody's idea of a maintenance shed").
            //
            // Taken from the shed itself rather than from a magic offset off its centre, so the spot the
            // captain lands on cannot drift from the walls that are drawn around it. That drift is exactly
            // what put him in a wall: this used to compute its own position from the RAW seeded head spot
            // while the shed was built at the nudged one.
            // #681 · …through the one door the captain is ever placed through, so the car has the same net
            // under it that the landing does. The audit still pins the un-nudged square (#602's own guard).
            (double carX, double carY) = MoonSurface.LiftHead(
                ex.Stop.Body.Id, ex.Site.LayoutSalt, MoonSurface.ExpeditionField()).CarFloor;
            // #603 \u00b7 And what you came out WITH. Owner: "Also in the brief pop-up of going to the surface...
            // inventory key needs to be advertised." Surfacing is the moment a captain takes stock \u2014 they
            // have just stopped spending air and started counting what it bought \u2014 so it is the one place
            // the pocket should announce itself without being asked.
            string carried = _satchel.Count > 0
                ? $" You are carrying {_satchel.Count} thing{(_satchel.Count == 1 ? "" : "s")} out of it \u2014 \ud83c\udf92 I to look."
                : "";
            ShowPulseMessage("\ud83d\udec3 The car climbs for a long time and lets you out into somebody's idea of a " +
                "maintenance shed. The moon is exactly as indifferent as you left it." + carried);
            // #681: and the placement LAST, so if the net has to catch anybody its line is the one left on
            // screen. A rescue nobody sees is a bug nobody reports.
            StandCaptainAt(carX, carY, "the car lets you out into the shed on the surface");
            RequestVaultSave();
            return;
        }

        (double sx, double sy) = HiveInterior.SpawnOn(MoonSurface.ExpeditionField());
        StandCaptainAt(sx, sy, "the car opens onto the floor");   // #681: the same net, underground
        RendererInterop.PlayCue("board");

        if (!wasUnderground)
        {
            // #585 \u00b7 THE CARD, on the first descent only. Owner: "I think we need to gen AI pop-up about
            // finding the elevator." It is the beat the whole feature turns on \u2014 the moment a moon stops
            // being a field with things scattered on it and becomes a LID.
            if (ex.HiveFloorsSeen.Count == 0)
            {
                // #411 · …and which card, because the head office's first descent is not a branch office's.
                // Core decides, so the two can never be shown for the wrong building.
                (string dLabel, string dArt, string dCard) =
                    UndergroundComplex.FirstDescentCard(ex.Stop.Body.Id);
                _viewObject = new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY, dLabel, dArt, dCard);
            }
        }

        // #411 · THE TWO FLOORS THE WHOLE ARC WAS WRITTEN FOR. Raised after the first-descent block on
        // purpose: those floors are reached from underground, so `wasUnderground` is true by the time a
        // captain gets to either of them and the establishing card above has long since been spent.
        MaybeRaiseHeadOfficeBeat(ex);

        // ── #725 · …AND THE SIGN THAT SAYS IT, WHICH HAD NO FRAME ──────────────────────────────────────
        //
        // Owner's audit: "are we giving enough attention to plot-significant finds? They should have a
        // Gen-AI image and their own dialog by our standards." The corrected plate is the whole arc's
        // arithmetic in one object and it was a wall stencil — missable at deck-plan zoom by a player who
        // has just walked past the reveal, with the game none the wiser.
        //
        // WHICH FLOOR IS CORE'S ANSWER (IsUnlistedLobby, off #694's own plate law) rather than a band sum
        // done again in a client. The card is per-excursion once, exactly like DEAD AIR above it, and it
        // takes the _viewObject slot uncontested: the top of every band holds pressure, so the air card can
        // never want the same frame, and the establishing card was spent floors ago.
        if (UndergroundComplex.IsUnlistedLobby(ex.Stop.Body.Id, level) && !ex.HiveUnlistedPlateShown)
        {
            ex.HiveUnlistedPlateShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.UnlistedLobbyLabel,
                UndergroundComplex.UnlistedLobbyArtUrl,
                UndergroundComplex.UnlistedLobbyCard);
            RendererInterop.PlayCue("reveal");
        }

        // ── #693 · EVERYTHING THE DOORS OPENING HAS TO SAY, AND THE LAW THAT DECIDES WHAT YOU HEAR ──
        //
        // This was five blocks in a row, three of them carrying a comment explaining that they were
        // deliberately LAST — because the pulse has one slot, the last write won, and the order these lines
        // happened to be written in was the entire contract. #592's climax was not one of the three. The
        // first words on a floor that does not exist, the biggest sentence in that feature, had been losing
        // the slot to the routine pressurisation line since the day it shipped: eaten by the weather.
        //
        // The arrival is COMPOSED now (Core, ArrivalSayings) with a RANK on each saying, and PulseSlot's law
        // picks the winner — a lower rank may not displace a higher one that is still held. So this loop
        // says all of them, the book keeps every one in the order they were said, and the screen keeps the
        // biggest. Shuffle the list and the same line is on screen; that is the law, and it is swept over
        // every arrival the generator admits (ThePulseKeepsTheBiggestSentenceTests).
        //
        // What stays here is what Core does not have: the cards, the nerve, the flags and the save.
        bool firstSight = ex.HiveFloorsSeen.Add(level);

        foreach (UndergroundComplex.Saying saying in UndergroundComplex.ArrivalSayings(
                     ex.Stop.Body.Id, fromLevel, level,
                     new UndergroundComplex.ArrivalMemory(
                         WasUnderground: wasUnderground,
                         FirstSightOfThisFloor: firstSight,
                         VacuumWarned: ex.HiveVacuumWarned,
                         UnlistedSeen: ex.HiveUnlistedSeen,
                         ChitBeatSpent: ex.ChitGateBeatShown,
                         SeamCrossed: ex.HiveSeamCrossed,
                         FoundSeen: ex.HiveFoundSeen,
                         ShaftsNarrated: ex.HiveShaftsOpened),
                     via, AuthorityCardIds(), _satchel))
        {
            // #768 · HELD, NOT SAID — because this same loop raises cards (the dead-air warning below, the
            // gate's own face) and the block above it may already have raised the first-descent card. Every
            // one of them lands a backdrop on top of whatever is on the pulse. The book gets the line now,
            // in the order it was said; the screen gets the winner when the captain closes the card.
            HoldAndFile(saying.Text, saying.Glyph, saying.Rank);

            switch (saying.Beat)
            {
                // #592 · THE FLOOR THAT IS NOT ON THE PLAN. The whole beat of the feature, said once, on the
                // first step out onto the band nobody listed.
                case UndergroundComplex.ArrivalBeat.Unlisted:
                    ex.HiveUnlistedSeen = true;
                    ApplyNerveShock(9.0, "a building with floors it does not count");
                    break;

                // ── #609 · WHETHER YOU CAN BREATHE HERE IS A CARD, NOT A TOAST ──
                //
                // Owner, having suffocated on B2: "I thought there is air in the base?" / "there should be a
                // warning or something :-D" / "maybe pop-up about you have air or you are in vacuum type ...
                // it is vital info" / "like the basement is more dangerous than the surface now :-D".
                //
                // The last one is exactly right and it is the DESIGN — depth is paid for in air (#585) — but
                // the game was announcing the single most important fact about a floor in a pulse that fades
                // in eight seconds, alongside pulses about hardware and dust. So the FIRST time each
                // excursion meets dead air it stops the world with a card; every later dead floor is the
                // pulse line again, because by then the captain has been told and a card per floor would be
                // a card nobody reads.
                case UndergroundComplex.ArrivalBeat.DeadAirFirst:
                    ex.HiveVacuumWarned = true;
                    _viewObject = new DeckPlan.ConsoleSpot(
                        DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                        UndergroundComplex.VacuumCardLabel,
                        UndergroundComplex.VacuumArtUrl,
                        UndergroundComplex.VacuumCard(ex.Stop.Body.Id, level, ex.AirSeconds));
                    break;

                // #689 · THE CARD'S FINEST HOUR. Owner, having found the card, fed the gate and ridden past
                // the listed bottom: "It was locked until I got it ... there was no story point about it
                // being needed or used." The line existed; it was said on the frame the panel closed and the
                // floor was torn down and rebuilt. Here the doors are open, the captain is standing still,
                // and the car is not going anywhere. Once per shaft per excursion — and the band comes off
                // the saying, because the ride that opened it already worked out which one that was.
                //
                // #684 · …and it is TOLD as a CARD, which is the other end of the read the panel makes. A
                // gate that refuses raises one at the panel; a gate that AGREES raises one here — same
                // title, same idiom, the face of the card the gate actually read. It belongs in this arm
                // and nowhere else: the beat already carries WHICH card opened the gate, so nothing here
                // re-derives a fact about a building it does not own (§13.15), and the picture can never
                // drift from the sentence #693 just said beside it. The pulse is that law's business and is
                // untouched — this is the saying a later tick cannot overwrite.
                case UndergroundComplex.ArrivalBeat.CardAccepted when saying.Gate is { } opened:
                    ex.HiveShaftsOpened.Add(opened.Band);
                    UndergroundComplex.GateRead told = UndergroundComplex.TheGateAccepted(opened);
                    _viewObject = new DeckPlan.ConsoleSpot(
                        DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                        told.Label, told.ArtUrl, told.Line);
                    ApplyNerveShock(3.0, "a gate that still obeys an office nobody can find");
                    break;

                // #752 · …AND THE OTHER PAPER'S ARRIVAL, WHICH IS THE JOB FINISHING. The Hand's line was
                // "take this to the lift and don't be clever near the counter"; this is the lift having heard
                // of it. No nerve shock: the card's gate is a dead office still saluting, which is
                // frightening, and a gate that reads a timesheet and waves you through is the least
                // frightening thing this building has done. The GIST is filed with the beat — the beat is
                // what happened, the gist is what the paper turned out to be worth.
                case UndergroundComplex.ArrivalBeat.ChitGate:
                    ex.ChitGateBeatShown = true;
                    FileNote(CanteenTable.ChitGateGist, CanteenTable.ChitGlyph);
                    RequestVaultSave();
                    break;

                // #677 · The pour stopping, said in the shaft on the way.
                case UndergroundComplex.ArrivalBeat.Seam:
                    ex.HiveSeamCrossed = true;
                    break;

                // #677 · The first gallery. The same price as the floor nobody listed, and not bigger,
                // deliberately: nothing down here threatens, and a site that bills a captain for standing in
                // a comfortable room is a predator whatever the prose says (§10.4c's ruling).
                case UndergroundComplex.ArrivalBeat.Found:
                    ex.HiveFoundSeen = true;
                    ApplyNerveShock(9.0, "a room that was ready before anybody thought to build one");
                    break;

                default:
                    break;   // the descent's own line and every later air line are prose and nothing else
            }
        }

        ApplyNerveShock(UndergroundComplex.HoldsPressure(ex.Stop.Body.Id, level) ? 2.0 : 5.0,
            "a building this expensive, this far down, and this empty");

        // #768 · …AND NOW THE ARRIVAL KNOWS WHETHER IT PUT A CARD IN FRONT OF ITSELF. Every card this
        // arrival can raise has been raised by here, so this is the only place that can honestly ask. Doors
        // that opened on nothing but prose pulse the winner immediately — the shipped behaviour, unchanged.
        // Doors that also raised a card keep it, and the ✕ on that card is what finally says it.
        ReleaseHeldSayingsUnlessACardStopsTheWorld();
        RequestVaultSave();
    }

    /// <summary>#585 - Where the camouflaged lift head stands. Reuses the seeded secret-lab door spot, so the
    /// ground already kept clear for the old chamber is exactly the ground the shed stands on - one seeded
    /// fact, two uses, nothing to keep in sync.</summary>
    private (double X, double Y) SecretLabHeadSpot(SurfaceExcursion ex)
    {
        // #602 · THE SAME FUNCTION THE SHED IS DRAWN BY, and for once the comment above this one was not
        // describing the code. Owner, stepping out of the car: "Oh I emerged into the wall on the surface...
        // I cannot move :-D"
        //
        // This used to return SecretLab.For(...).DoorX/DoorY — the RAW seeded spot. But MoonSurface builds
        // the shed at SecretLab.HeadSpot(...), which starts from that raw spot and then MOVES it clear of
        // the shelters and huts already standing there. So on any site where the nudge did something, the
        // lift returned the captain to the un-nudged spot: inside the very structure the nudge exists to
        // avoid, in a wall, unable to move.
        //
        // The old comment claimed "one seeded fact, two uses, nothing to keep in sync" — which was the
        // intention and not the code. It is one function now, so it is true.
        return SecretLab.HeadSpot(
            ex.Stop.Body.Id, ex.Site.LayoutSalt, MoonSurface.ExpeditionField());
    }

    /// <summary>#585 - Turning over one room of the facility. About a third are stripped; the rarest thing in
    /// the building is a FILE ON SOMEBODY, because it is the only haul you spend on a person.</summary>
    private void HiveHaulInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveHaul } spot)
        {
            return;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        int which = -1;
        for (int i = 0; i < floor.RoomCentres.Count; i++)
        {
            if (Math.Abs(floor.RoomCentres[i].X - spot.X) < 0.5
                && Math.Abs(floor.RoomCentres[i].Y - spot.Y) < 0.5)
            {
                which = i;
                break;
            }
        }
        if (which < 0)
        {
            return;
        }

        // #678 · THE ROOM IS NOT CONSUMED UNTIL THE FIND IS. This used to be one line — test the key and mark
        // it emptied in the same breath — which is exactly why a find the pocket could not take was destroyed
        // by the act of looking at it. Now the room is only struck off once the pickup has actually happened.
        int roomKey = HiveInterior.RoomKey(ex.Floor, which);
        if (ex.HiveRoomsEmptied.Contains(roomKey))
        {
            return;
        }

        UndergroundComplex.Haul haul = UndergroundComplex.InRoom(ex.Stop.Body.Id, ex.Floor, which);

        // ── #701 · THE ODD BOOK ─────────────────────────────────────────────────────────────────────────
        //
        // Owner: "a better alternative to finding an empty room. You look around but only one book catches
        // your attention." Searching a room and finding nothing is the most common outcome in this building
        // and the least written; one would-be-empty room in six now says something instead.
        //
        // It happens BEFORE the pocket, and it returns without ever reaching it, because a book is not a
        // haul: nothing goes in the satchel, no credits change hands, and — the load-bearing part — THE ROOM
        // IS NOT STRUCK OFF. The book is still on the shelf, so the console is still standing there and [E]
        // opens the card again. Re-reading is free; the casebook learns the gist once per book per thread
        // (#603's law: looking is free, knowledge is one-shot), which Core decides, not this method.
        //
        // The shelf line is the ROOM's line; the GIST is what the book is worth and is the thing the casebook
        // keeps. Filing both would put the same shelf in the book twice in two registers, and the second
        // search would file it a third time.
        if (OddBooks.Search(ex.Stop.Body.Id, ex.Floor, which, _oddBooksRead, _bookCheat) is { } shelf)
        {
            // #528's idiom, caption-only: there is no art file for these yet and one that is wired but
            // unpainted would render an img the browser hides — the lifeboat-muster precedent is a card that
            // never claims a picture at all.
            //
            // #736 · THE SHELF LINE IS ON THE CARD, not under it. The comment three lines down has said since
            // #701 that "the pulse would play under the card's own blur" — and the shelf line was pulsed on
            // the frame this card goes up, so the sentence that tells you WHY one book caught your eye was
            // the one sentence of the beat behind the frosted glass. Composed into the card's own story, the
            // way #603's document card already carries its try's answer: one object card, one text.
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                shelf.Title, null, shelf.Line + "\n\n" + shelf.Card);

            if (shelf.Gist is { } gist)
            {
                // FileNote and not ShowAndFile: the saying is on the card above, and a pulse would play under
                // the card's own blur (#686/#736).
                FileNote(gist, OddBooks.Glyph);
                _oddBooksRead = [.. shelf.Filed];
            }

            RequestVaultSave();
            return;
        }

        // Everything found down here is FILED (#587) - this is the place in the game most worth being able to
        // re-read, and a file on a harbourmaster that faded after eight seconds would be a joke.
        // #613 \u00b7 AND SAY WHETHER IT WENT INTO YOUR POCKET. Owner, after clearing a corridor: "now I used e
        // to search and then checked inventory on many rooms" / "we should tell the users if stuff is picked
        // up into inventory or not."
        //
        // The haul line describes what is in the room and stops, which was right while everything either
        // paid out in credits or was flavour. Some of it is now a THING YOU CARRY and some of it is not, and
        // the captain was opening the satchel after every single room to find out which \u2014 the game asking
        // the player to audit it.
        // \u2500\u2500 #613 \u00b7 THE CARD YOU FOUND IS ALWAYS A CARD YOU CARRY \u2500\u2500
        //
        // Owner, in a four-floor site: "now I picked authority card but it did not go to inventory."
        //
        // He was right and the cause was mine. CardInRoom returns the card for the band BELOW, and correctly
        // returns null on the bottom band \u2014 a card for a hole nobody dug would be a lie. But the client then
        // handed out a lead and put NOTHING in the pocket, while the prose went on describing a countersigned
        // card in the captain's hand. The sim did one thing and the sentence said another: this repo's third
        // named bug class, and I shipped it again.
        //
        // A card is an OBJECT. You picked it up, so you have it. When the shaft it runs is not in this
        // building it is a card for another one \u2014 which is exactly the wallet the refusal line has been
        // describing all along ("every one of them countersigned, current, and for another shaft"). Until now
        // that line was describing a thing the game could not give you. Now the deepest floor of one facility
        // hands you the way into the next, which is the best thing a bottom floor could possibly hold.
        //
        // ── #684 · AND THE LEAD IS NOT SPENT UNTIL THE CARD IS IN THE POCKET ──
        //
        // This used to call GrantLabLead here, which BANKS the lead and says it out loud — one step ahead of
        // the capacity check below. On a bottom-band Key with a full pocket the find is refused, the room is
        // not emptied and the same card is offered again on the next search (#678's law) — but the lead had
        // already been heard, and news can only be heard once. The captain kept the knowledge without ever
        // carrying the card that was supposed to be how they got it.
        //
        // The moon is only NAMED here now. It is announced further down, after the pocket has agreed.
        UndergroundComplex.AuthorityCard? found = null;
        string? farLead = null;
        if (haul == UndergroundComplex.Haul.Key)
        {
            found = UndergroundComplex.CardInRoom(ex.Stop.Body.Id, ex.Floor);
            if (found is null
                && NameAMoonWorthLookingAt(
                    DiceRule.Seed($"lead:hive-key:{ex.Stop.Body.Id}:{ex.Floor}:{which}")) is { } far)
            {
                farLead = far;
                if (UndergroundComplex.SiteHasBand(far, 0))
                {
                    found = new UndergroundComplex.AuthorityCard(far, 0);
                }
            }
        }

        // ── #678 · THE POCKET NEVER LIES ──
        //
        // Owner, after a card he had read a pickup line for turned out not to be in the satchel: "we should
        // have CI test that makes sure all picked items that sound useful are put into the inventory ... If
        // refused the item should stay where it was investigated last — not disappear like they do now, or
        // seem to."
        //
        // The composition used to live here, in the wrong order: the "Into your pocket" line was built and
        // shown, the room was already struck off, and only THEN did Satchel.Add get a chance to refuse. At
        // capacity the find was destroyed and the sentence had already claimed it. It is one pure call now
        // (UndergroundComplex.WhatGoesInThePocket), which is the only way a test can walk every haul against
        // every pocket — and it answers all three parts at once: what goes in, what is said, and whether the
        // room has been emptied at all.
        // #677 · Core mints the id, because the id says which KIND of place the thing came out of and three
        // separate seams read that later — the pocket line, the satchel row and the look-card. Composed here
        // as a string literal it was one place; the moment a second class of relic existed it would have
        // been the client teaching itself a fact about a band it does not own.
        string findId = UndergroundComplex.FindId(ex.Stop.Body.Id, ex.Floor, which);
        UndergroundComplex.Pickup pick = UndergroundComplex.WhatGoesInThePocket(
            haul, ex.Stop.Body.Id, found, findId, _satchel);

        string room = UndergroundComplex.HaulLine(haul, ex.Stop.Body.Id, ex.Floor, which, found);

        if (!pick.RoomEmptied)
        {
            // Nothing changes but the sentence. The room is not struck off, so searching it again offers the
            // same find — which is the enforcement side of #615 (leaving a thing must never destroy it).
            // The deck is deliberately NOT rebuilt: the console has to still be standing there.
            ShowAndFile(room + pick.Line, "\ud83d\udd26");
            RequestVaultSave();   // the field note is a possession too (#587)
            return;
        }

        ex.HiveRoomsEmptied.Add(roomKey);

        // #684 · NOW the lead is spent — the room has been turned over for good and whatever it held is in
        // the pocket. Said BEFORE the room's own line for the reason everything in this method is ordered:
        // the pulse keeps one slot and the last write wins, and the haul is the sentence worth surviving.
        if (farLead is { } lead)
        {
            AnnounceLabLead(lead);
        }

        if (haul == UndergroundComplex.Haul.Equipment)
        {
            _credits += 900;
            RendererInterop.PlayCue("board");
        }

        // #677/#603 \u00b7 WHAT THE BOOK KEEPS. Everywhere in the game the pulse line and the casebook line are
        // the same sentence, because what happened IS what is worth remembering. A record out of the halls
        // is the exception #603's law was written for: looking is free, knowledge is one-shot, so the screen
        // gets the event (a rubbing went into a pocket) and the book gets what the captain now KNOWS about a
        // wall. Filing both would put one wall in the book twice in two registers \u2014 #701's rule, learned on
        // the shelves \u2014 and there are several of these records in a band.
        if (UndergroundComplex.CasebookGistOf(haul, ex.Stop.Body.Id, ex.Floor) is { } hallGist)
        {
            ShowPulseMessage(room + pick.Line);
            if (!ex.HiveHallRecordShown)
            {
                FileNote(hallGist, "\u2b55");
            }
        }
        else
        {
            ShowAndFile(room + pick.Line, haul == UndergroundComplex.Haul.Dirt ? "\ud83d\uddc3" : "\ud83d\udd26");
        }

        if (haul == UndergroundComplex.Haul.Dirt)
        {
            ApplyNerveShock(4.0, "reading somebody's file in a building that should not exist");
        }

        // #585 · A facility keeps records of the OTHER facilities. Operational paper and files are the
        // strongest leads in the game, which is the right shape: the deeper into one of these you go, the
        // more of the map opens up.
        // ── #603 · PAPER IS NOW A THING YOU CARRY, NOT A LEAD YOU ARE GIVEN ──
        //
        // Operational paper used to grant a lead the moment you picked it up — the game did the thinking and
        // handed you the answer. The owner's version is better: the decision to read a document AS A CLUE is
        // the player's, and making it is what lights the tracker. A paper you have not connected to anything
        // is just paper.
        //
        // A file on somebody is carried too, but it is never offered to a door: it is leverage on a PERSON,
        // which is the only currency down here you spend on somebody you can go and meet.
        //
        // #678 · ONE ADD, and it is the one the sentence was written about. There used to be four of these
        // scattered down the method, each deciding for itself what this room hands over — which is how the
        // authority card came to be added AFTER the full-pocket check and to slip through it entirely.
        if (pick.Take is { } took)
        {
            _satchel = [.. Core.Satchel.Add(_satchel, took)];
        }

        if (haul == UndergroundComplex.Haul.Relic)
        {
            // The card is raised on the spot. For the thing on the pallet that is unconditional and every
            // time: it is the one object in the game a captain will want to look at again the moment they
            // find it, and #528's once-per-excursion gate is for things that RECUR — a rib mouth, a card, a
            // dead floor. There is one of those in a facility and most facilities do not have one.
            //
            // #677 · A HALL RECORD DOES RECUR, so it takes the gate. Several galleries in a band hold one
            // and they are all the same wall; the fifth full-screen card about it would be a slideshow, and
            // it is the same call the authority card makes twenty lines below. WHICH card is Core's — the
            // find's own id knows what it came out of, so this seam can never show a photograph of a pallet
            // to a captain standing in an empty gallery.
            CarriedObject.Reveal shown = CarriedObject.RelicReveal(findId);
            bool recurs = UndergroundComplex.IsHallRecord(findId);
            if (!recurs || !ex.HiveHallRecordShown)
            {
                _viewObject = new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                    shown.Label, shown.ArtUrl, shown.Story);

                // The fright is on the FIRST one for the same reason the card is. It is also the same size
                // as the pallet's and not larger: nothing down here threatens, everything accommodates, and
                // a place that bills a captain by the room for standing in it is a predator whatever the
                // prose says. What is frightening about a hall is arithmetic, not attention.
                ApplyNerveShock(9.0, recurs
                    ? "putting a tape measure against something that does not answer to one"
                    : "standing next to something that was measured for a neck");
            }

            ex.HiveHallRecordShown |= recurs;
        }
        else if (haul == UndergroundComplex.Haul.Dirt)
        {
            // A file still names a moon on its own — it is about a PERSON and the person is somewhere. Only
            // the operational paper became a thing you have to decide about.
            GrantLabLead(DiceRule.Seed($"lead:hive:{ex.Stop.Body.Id}:{ex.Floor}:{which}"));
        }

        // #678 · Said only when something actually went in — "that was the last space" is a fact about a
        // pickup, and on a room that handed over nothing it would be a warning attached to nothing.
        //
        // #688 · And it is a fact about ONE COMPARTMENT, because after the restructure "the satchel is full"
        // has no meaning: a sleeve stuffed with manifests says nothing about the pockets, and neither of them
        // says anything about the wallet, which never fills at all. The warning names what ran out.
        if (pick.Take is { } went && Core.Satchel.IsFull(_satchel, went.Kind))
        {
            ShowPulseMessage(Core.Satchel.CompartmentOf(went.Kind) == Core.Satchel.Compartment.Sleeve
                ? "🎒 That was the last sheet the document sleeve will hold. Something has to be read or " +
                  "left behind before you can carry any more paper out of here."
                : "🎒 Your hands and pockets are full. Something has to be spent or left behind before you " +
                  "can carry anything else out of here.");
        }

        // #590 · THE CARD IS NOW A THING YOU HOLD. It runs the shaft below the band it was found in, so the
        // way deeper into a facility is earned by working the floors you are on — and it is durable, so the
        // gate still reads it a month and a moon later.
        //
        // On the bottom band there is no shaft below to authorise, and rather than hand out an authority for
        // a hole nobody dug, that Key names another moon: the same payoff Records and Dirt give, which keeps
        // the deepest floor of a site pointing outward instead of at itself.
        //
        // #528 · THE COUNTERSIGNATURE. Owner: "the authority card could also have a gen ai image to really
        // tell the story here :-D" — the right pair with the sealed way, because the Hive has exactly two
        // objects about the idea of passage and this is the one that works. It is raised for a card that is
        // IN THE POCKET and never for one the world declined to mint (#678).
        if (found is { } card && pick.Take is not null && !ex.HiveAuthorityShown)
        {
            ex.HiveAuthorityShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.AuthorityCardLabel,
                UndergroundComplex.AuthorityCardArtUrl(card),
                UndergroundComplex.AuthorityCardStory(card));
        }

        RebuildSurfaceDeck();
        RequestVaultSave();
    }

    /// <summary>#585 - A door that never opens, read out loud. It is a WALL with a world behind it, and the
    /// game never once pretends otherwise - a door that teases would turn scale into a puzzle.</summary>
    private void HiveSignInteract()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveSign } spot)
        {
            return;
        }
        string sign = spot.Label.Replace("\ud83d\udd12 ", "");

        // #528 \u00b7 THE WAY ON, CLOSED. Owner, standing at a rib's far end: "I see there is a nice lock here at
        // the end of the corridor.... maybe we could have a gen-AI image for it and a pop-up to tell the
        // story?"
        //
        // It fires at the moment it explains the most (#528's fourth rule): the captain has just walked the
        // length of a rib and met a plate with a distance painted on it. ONLY the sealed way earns a card \u2014 a
        // room door that will not open is a sign to read, not a scene, and forty of them would be a
        // slideshow. The pulse line still says its piece underneath, here and every time after.
        if (UndergroundComplex.IsSealedWay(sign) && _surface is { } sealedEx && !sealedEx.HiveSealedWayShown)
        {
            sealedEx.HiveSealedWayShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.SealedWayCardLabel,
                UndergroundComplex.SealedWayArtUrl,
                UndergroundComplex.SealedWayCard(sign));
            ApplyNerveShock(3.0, "a corridor somebody dug for a year and then closed");
        }

        // ── #603 · AND THE DOOR TELLS YOU YOU HAVE POCKETS ──
        //
        // Owner: "we should advertise the items list on closed locked door pop-up... something like check
        // your items button there that opens inventory."
        //
        // This is the #212 law applied to the satchel: the refusal already said WHY, and a captain standing
        // in front of it with an authority in their pocket had no way to find out whether it was the one.
        // The door now offers the verb. It is also how the satchel gets discovered at all — nobody reads a
        // keybind, and everybody presses the button on the thing that just refused them.
        _lockedDoor = new LockedDoorLook(
            sign,
            UndergroundComplex.LockedLine(sign),
            UndergroundComplex.IsSealedWay(sign)
                ? SatchelTry.Target.SealedWay
                : SatchelTry.Target.RoomDoor);
    }

    /// <summary>#603 · The door the captain is standing at, while its pop-up is up.</summary>
    private sealed record LockedDoorLook(string Sign, string Line, SatchelTry.Target Target);

    private LockedDoorLook? _lockedDoor;

    private void CloseLockedDoor() => _lockedDoor = null;

    /// <summary>#603 · "Check your items" — the satchel, opened AT something, so every item is a thing you
    /// can offer rather than a thing you can look at.</summary>
    private void OpenSatchelAtTheDoor()
    {
        if (_lockedDoor is { } door)
        {
            // #680 · Say what the thing IS, not just what is painted on it. "Offering it to MEDICAL"
            // read as a person until the owner asked what it meant — and a line the owner has to ask
            // about is a line that is not doing its job. The sign stays, the noun arrives.
            _satchelTarget = (door.Target, null,
                door.Target == SatchelTry.Target.SealedWay
                    ? $"the sealed way ({door.Sign})"
                    : $"the locked {door.Sign} door");
        }
        _lockedDoor = null;
        _satchelOutcome = null;
        TheSatchelOpensOnThePocket();
        _showSatchel = true;
    }

    /// <summary>#603 · Open it from nowhere in particular — just to see what you are carrying.</summary>
    private void OpenSatchel()
    {
        _satchelTarget = null;
        _satchelOutcome = null;
        TheSatchelOpensOnThePocket();
        _showSatchel = true;
    }

    /// <summary>#688 · The I key, both ways. Owner: <i>"If I press I when inventory is open, let's close it
    /// then."</i>
    ///
    /// <para>A pocket you open by reflex has to shut by the same reflex. Note which way this closes: a satchel
    /// opened AT a door still closes on I, because the captain's hand is on the key and not on the fiction —
    /// and <see cref="CloseSatchel"/> clears the target, so the next I is an honest look at what you have.</para></summary>
    private void ToggleSatchel()
    {
        if (_showSatchel)
        {
            CloseSatchel();
            return;
        }

        OpenSatchel();
    }

    private void CloseSatchel()
    {
        _showSatchel = false;
        _satchelTarget = null;
        _satchelOutcome = null;
    }

    private bool _showSatchel;

    // ── #690 · THE OTHER THING A SATCHEL HOLDS ──────────────────────────────────────────────────────────
    //
    // Owner, designing the paper-shedding loop: "should we have notes / clues section in our inventory ui?"
    // — and, on the register it should be written in: "it's like our detective notepad :-D".
    //
    // The field book (#587) rendered only in the Captain's ledger, a ship-brain surface. #688 made that a
    // real cost: leaving a paper files its gist to the book, so knowledge was being deliberately moved into
    // a place unreachable from the ground it came off. Record the essential data, throw out the paper, and
    // be able to read the record standing in the dark.
    //
    // A pocket and a notebook are both things a satchel holds. There is NO second store here: the tab reads
    // _fieldNotes, the one book (#587's law — one place that can never be forgotten about), through the same
    // Core projection the ledger renders.
    private enum SatchelPage
    {
        /// <summary>What you are carrying. Always where an open lands — the pocket is the primary tool.</summary>
        Carried,

        /// <summary>What the ground has told you.</summary>
        Notes,
    }

    private SatchelPage _satchelPage = SatchelPage.Carried;

    /// <summary>#690 · Whether the NOTES tab is showing the whole book rather than this ground alone. Defaults
    /// to this ground: a captain at a door wants what THIS building has told them, not the memoirs.</summary>
    private bool _notesEverywhere;

    /// <summary>#690 · Every open lands on the pocket, on this ground, whatever the last one was left on. The
    /// tab choice is not a setting — it is where you were looking a moment ago, and a moment ago is over.
    /// One method rather than two copies, because a third opener would forget one of these lines.</summary>
    private void TheSatchelOpensOnThePocket()
    {
        _satchelPage = SatchelPage.Carried;
        _notesEverywhere = false;

        // #697 · And the wallet is folded shut. A folder that reopened expanded would be the card rows it
        // replaced with an extra line above them, which is the whole row spent for nothing.
        _walletOpen = false;
    }

    /// <summary>#690 · The ground underfoot, named the way the BOOK names it — through
    /// <see cref="Core.FieldNotes.PlaceLabel"/> and never re-derived here, so the filter can never drift off
    /// the labels <see cref="FileNote"/> wrote. Null off a surface, where there is no ground to be on.</summary>
    private string? PlaceUnderfoot() =>
        _surface is { } ex ? Core.FieldNotes.PlaceLabel(ex.Stop.Body.Name, ex.Site.Name) : null;

    /// <summary>#690 · What the NOTES tab shows on this ground: the one book, filtered by the ledger's own
    /// grouping. Read-only — the book is capped and durable by its own laws and the satchel just holds it
    /// open.</summary>
    private IReadOnlyList<Core.FieldNote> NotesFromThisGround() =>
        Core.FieldNotes.Here(_fieldNotes, PlaceUnderfoot());

    /// <summary>#680 · What the last failed offer answered, said inside the dialog itself. The pulse HUD
    /// renders under the modal backdrop's blur, so a refusal routed there is in the DOM and not on the
    /// screen. Cleared whenever the satchel opens or closes — the line belongs to one conversation at one
    /// door, not to the pocket.</summary>
    private string? _satchelOutcome;

    /// <summary>What the satchel is currently open AT, if anything: the target, whatever that target needs
    /// to judge an offer, and what to call it on screen.</summary>
    private (SatchelTry.Target Target, string? Context, string Label)? _satchelTarget;

    /// <summary>#614 · The card for a carried thing, or null if it is ordinary. Asked once per row while the
    /// satchel draws, which is why <see cref="CarriedObject.Card"/> is cheap and pure.</summary>
    private CarriedObject.Reveal? LookAtItem(Core.Satchel.Item item) =>
        _surface is { } ex ? CarriedObject.Card(item, ex.Stop.Body.Id) : null;

    /// <summary>#614 · LOOK AT IT PROPERLY. Owner: <i>"we could have gen-AI images of plotwise important
    /// items... maybe they say something about what door they open."</i>
    ///
    /// <para>Free and repeatable, per #603's ruling that reading a thing and DECIDING something with it are
    /// two different acts — the owner asked for the paper to be viewable many times and the same law covers
    /// every object worth a card. The satchel stays open underneath, because a captain comparing three
    /// authority cards should not have to reopen their pockets between each one.</para></summary>
    private void OpenItemCard(Core.Satchel.Item item)
    {
        if (LookAtItem(item) is not { } card)
        {
            return;
        }

        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            card.Label, card.ArtUrl, card.Story);
    }

    /// <summary>#688 · LEAVE IT. Owner, live: <i>"The keycard story is already big, but no way to drop
    /// stuff."</i>
    ///
    /// <para>The satchel had a verb for offering a thing and a verb for looking at one, and none at all for
    /// putting one down — while the game's own prose kept telling the captain that something had to be
    /// <i>read, spent or left behind</i>. The only way to make room was to spend something.</para>
    ///
    /// <para>It is its own small control per row rather than a mode, for #614's reason one size down: a
    /// captain making room must never be one mis-click away from offering a relic to a bulkhead. The satchel
    /// STAYS OPEN — you are putting a thing down in order to pick a thing up — which is exactly why the
    /// confirmation is stored for the dialog rather than pulsed (#680: the pulse HUD renders under the
    /// backdrop's blur, so a line sent there is in the DOM and not on the screen).</para>
    ///
    /// <para>#615's law, unbroken: leaving never destroys. It is lying on the square the captain is standing
    /// on, and <see cref="TryPickUpWhatYouLeft"/> hands it straight back.</para></summary>
    private void LeaveItem(Core.Satchel.Item item)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        string standing = WhereYouAreStanding();

        // ── #696 · A DOCUMENT IS NOT DROPPED. IT IS PROCESSED, AND THAT TAKES TIME ──
        //
        // Owner: "we take time to process the loot." Leaving a paper files what it SAID (below), which is
        // the captain photographing it — and that is seconds of standing still, not a click. Only documents
        // hold: there is no gist to a handful of rounds, so there is nothing to stand still for, and the
        // question "is there a gist" is asked ONCE, by Core, here and at the far end alike.
        if (LeftBehind.GistOf(item, standing) is { Length: > 0 })
        {
            BeginProcessing(ex, Core.Processing.Work.File, item, standing, at: null);
            return;
        }

        SetItDown(ex, item, standing);
    }

    /// <summary>#688 · The drop itself, once whatever had to happen first has happened. Everything that was
    /// in <see cref="LeaveItem"/> before #696 put a clock in front of it — unchanged, because the effect at
    /// the far end of a hold must be the effect the game already had, not a second copy of it.</summary>
    private void SetItDown(SurfaceExcursion ex, Core.Satchel.Item item, string standing)
    {
        // Rounds go down as the whole stack: six rounds is ONE thing you are carrying (#603), so it is one
        // thing you set down. Leaving them a round at a time would be inventory management.
        (int sqX, int sqY) = BeachComber.SquareOf(_avatarX, _avatarY);
        ex.Ground.Leave(LeftBehind.SpotKey(ex.Floor, sqX, sqY), item);
        _satchel = [.. Core.Satchel.Remove(_satchel, item.Kind, item.Id, item.Count)];

        // ── #688 · A DOCUMENT LEAVES ITS GIST BEHIND IN THE BOOK ──
        //
        // Owner refinement on the drop verb: leaving a paper files what it said before the paper leaves the
        // pocket. A captain does not abandon a pay sheet without having looked at it — what they are putting
        // down is the SHEET, and the sheet was only ever costing them bulk. The sleeve empties; the knowledge
        // does not, which is #587's law ("a find that is shown once is a find that is lost") arriving at the
        // moment the captain lets go.
        //
        // FileNote and not ShowAndFile: the SAYING is one line and it goes wherever the captain is actually
        // looking (#680/#686), which is what SayItWhereTheyAreLooking decides. A second pulse for the gist
        // would be the book reading itself out loud.
        string? gist = LeftBehind.GistOf(item, standing);
        if (gist is { Length: > 0 })
        {
            FileNote(gist, item.Kind == Core.Satchel.Kind.Dirt ? "🗃" : "📋");
        }

        SayItWhereTheyAreLooking(LeftBehind.LeaveLine(SatchelLabel(item), standing, gist is not null));

        // #698 · AND THE DECK SAYS SO IMMEDIATELY. Owner: "there was nothing marked onto the map?" The
        // marks are composed by the rebuild, so a drop that did not rebuild would leave the captain looking
        // at the ground they just used and seeing the same nothing that produced the complaint. It is the
        // same one-append-on-a-memoized-base the bury and the door-force already pay.
        RebuildSurfaceDeck();
        RequestVaultSave();
    }

    /// <summary>#680 + #696 · Say it where the captain is looking. The law #680 wrote is about the BLUR and
    /// not about the pulse: with the satchel open over the world a line sent to the HUD is in the DOM and not
    /// on the screen, and with the satchel shut a line stored for the dialog is nowhere at all.
    ///
    /// <para>It became a fork rather than a fact the moment #696 let a leave finish with the dialog closed —
    /// the hold shuts the satchel so the captain can watch the fan, and they may or may not have opened it
    /// again by the time the shutter closes. One method, asked by every caller, so no site has to guess.</para>
    ///
    /// <para>#736 · AND THE FORK GREW THE REST OF THE POP-UPS. Owner, restating the law in general: <i>"When
    /// an action is made on a pop-up, the result text must be readable on that pop-up — not on the blurred
    /// background. All actions, like using the elevator, items, etc. should report to the pop-up so the text
    /// is not blurred by the modal backdrop."</i> The seam existed and knew about exactly one dialog, so
    /// every other pop-up in the game went on losing its answers to the blur one organ at a time (#680 the
    /// satchel, #686 the car panel, #736 the freight agent's receipt behind his own card).</para>
    ///
    /// <para>The table below is that law, in one place, read TOP-DOWN in z-order: the pop-up nearest the
    /// captain's eye owns the answer, because that is the one their eye is on and the one whose subtree the
    /// backdrop cannot blur. Nothing in front of them at all, and the HUD's pulse is exactly right — a line
    /// about the world, said on the world.</para></summary>
    private void SayItWhereTheyAreLooking(string line)
    {
        // #774 · The object card, and it is FIRST because it is on top: both full-screen cards are drawn
        // with the same backdrop class, and this one is written later in Map.razor, so when an event raises
        // both (the outpost's effects plate under the dossier it assembles) this is the one the captain's
        // eye is on.
        //
        // It APPENDS where every other row below replaces, and that difference is the whole of #774. The
        // events that raise this card have two to five things to say IN ONE BREATH, all at the same rank —
        // a slot has one winner and picking it by write order is the contract #693 killed, while a region
        // has room for all of them and so has no winner to pick. Answers arriving one press at a time (the
        // panels below) are a slot's proper business and stay one.
        if (_viewObject is { } shown)
        {
            _viewObject = shown with
            {
                Outcome = shown.Outcome is { Length: > 0 } already ? $"{already}\n\n{line}" : line,
            };
            return;
        }

        // A raised card is the most modal thing in the game — it stops the world and waits to be dismissed,
        // and everything else on this list is behind it. Its answer rides the card record itself, so it
        // cannot outlive the card it belongs to.
        if (_revealCard is { } card)
        {
            _revealCard = card with { Outcome = line };
            return;
        }
        if (_showSatchel)
        {
            _satchelOutcome = line;     // #680 — the dialog this law was first written for
            return;
        }
        if (_showLiftPanel)
        {
            _liftOutcome = line;        // #686 — the car panel, the same disease's second organ
            return;
        }
        if (_pinJob is not null)
        {
            _pinOutcome = line;         // the hatch keypad: a buzz that is not read is a keypad that is broken
            return;
        }
        if (_showAlarmPanel)
        {
            _alarmOutcome = line;
            return;
        }
        if (_showDoorBoard)
        {
            _doorBoardOutcome = line;
            return;
        }
        if (_showCaptainsRemote)
        {
            _remoteOutcome = line;
            return;
        }
        if (_showChargeBoard)
        {
            _chargeBoardMessage = line; // the board already had a slot of its own — this only aims at it
            return;
        }
        if (_showVentPanel)
        {
            _ventMessage = line;        // ditto: the valve board says everything else it does right here
            return;
        }
        if (_barMenu is not null)
        {
            _barNotice = line;          // the counter-example #736 was filed against — the keep answers on his card
            return;
        }
        ShowPulseMessage(line);
    }

    /// <summary>#688 · What is at your feet, answered before what is in the walls. Returns true when the press
    /// was spent on the ground — the console under the captain gets the NEXT one, which is the honest order:
    /// a thing you put down yourself is not a thing you should have to walk away from to reach.</summary>
    private bool TryPickUpWhatYouLeft()
    {
        if (_surface is not { } ex)
        {
            return false;
        }

        // The square the captain is standing on, and the ring around it. A three-metre grid cell is smaller
        // than a captain's idea of "where I put it down", and #615's law is only real if the way back is
        // real — a relic you cannot find again was destroyed, whatever the store says it is holding.
        //
        // #698 · THE RING IS ONE RING. This scan used to be written out here, which made the keybar's new
        // offer ("E — take back what you left") a SECOND transcription of the same geometry — and a prompt
        // measured off a copy of the law is the bug class this repo has already paid for four times. Core
        // owns it now; the key and the sentence ask the identical function.
        if (ex.Ground.SpotInReach(ex.Floor, _avatarX, _avatarY) is not { } spot)
        {
            return false;
        }

        LeftBehind.Recovery back = ex.Ground.PickUp(spot, _satchel);
        _satchel = [.. back.Pocket];

        // Both halves are named. A recovery that quietly left something on the floor without saying so would
        // be #678's silent drop one verb later — and this time the captain put it there on purpose.
        ShowPulseMessage(LeftBehind.FoundAgainLine(
            [.. back.Taken.Select(SatchelLabel)],
            [.. back.StillThere.Select(SatchelLabel)]));

        // #698 · The mark goes when the spot does — and STAYS when it does not. A recovery that could not
        // take everything leaves the rest lying there (the one operation whose failure mode must leave the
        // world as it found it), so the redraw is the honest one either way: the composer reads the store.
        RebuildSurfaceDeck();
        RequestVaultSave();
        return true;
    }

    /// <summary>#698 · Is the captain inside the recovery ring of a spot with something lying on it? The
    /// keybar's question, and it is <see cref="LeftBehind.SpotInReach"/> — the SAME call
    /// <see cref="TryPickUpWhatYouLeft"/> makes — so the offer on the bar and the press it advertises can
    /// never come apart. Off an excursion there is no ground to have left anything on.</summary>
    private bool StandingOnWhatYouLeft() =>
        _surface is { } ex && ex.Ground.AnythingInReach(ex.Floor, _avatarX, _avatarY);

    /// <summary>#688 · Where the captain is, in their own words, for the line that says where a thing was
    /// left. Underground it is a floor with a number painted on it; up top it is the ground.</summary>
    private string WhereYouAreStanding() => _surface is { Floor: < 0 } ex
        ? $"on the floor of B{-ex.Floor}"
        : "on the regolith at your feet";

    // ── #696 · THE DARKROOM: PROCESSING THE LOOT TAKES TIME, AND THE AIR PRICES THE WHERE ───────────────
    //
    // Owner, mid-run: "How is our detective notebook / picture taking progressing for our ability to process
    // the files etc so we don't need carry them. That is something one would do without using tanked air. It
    // is good game mechanic... we take time to process the loot."
    //
    // Read the whole of Core.Processing for the design. What this half does is three things and no more:
    // start a hold, advance it while the boots stay put, and — at the far end — call the effect the game
    // ALREADY HAD. Nothing here touches the tank. The hold passes sim time; StepSuitAir prices sim time on
    // whatever ground the captain chose to stand on; the decision "read it here or haul it to the shelter"
    // falls out of two systems that have never heard of each other. That is the whole ruling.
    //
    // THE SATCHEL SHUTS ON THE WAY IN, and it is the one judgement call in this lane worth arguing about.
    // #691's leave verb kept the dialog open on purpose (you put a thing down in order to pick a thing up)
    // and that was right while the drop was instantaneous. It is wrong the moment the drop has a body: the
    // teeth of this mechanic are twenty seconds of being STATIONARY AND VISIBLE, and a captain cannot watch
    // a motion tracker through a backdrop blur. So the pocket closes, the fan comes back, and the bar fills
    // over the captain's own mark. #680's law is not "say it in the dialog" — it is "say it where the player
    // is looking", which is what SayItWhereTheyAreLooking asks every time.

    /// <summary>#696 · How long one document takes, right now. The Core constant, unless the QA cheat has
    /// switched the clock off — and it is a PROPERTY rather than four call sites, so the sim, the bar, the
    /// keybar hint and the satchel's own leave hint can never be running different numbers.</summary>
    private double ProcessingSeconds => _processCheatSeconds ?? Core.Processing.SecondsPerDocument;

    /// <summary>#696 · Take the document out and start the clock. The item is NOT removed and nothing is
    /// filed: everything happens at the far end, so an interruption has nothing to undo.</summary>
    private void BeginProcessing(
        SurfaceExcursion ex,
        Core.Processing.Work work,
        Core.Satchel.Item item,
        string standing,
        (SatchelTry.Target Target, string? Context, string Label)? at)
    {
        string label = SatchelLabel(item);

        // One pair of hands. The satchel shuts on the way in, but the captain can open it again with I and
        // press the row a second time — and a control that does nothing and says nothing is indistinguishable
        // from a bug (#603), so the refusal is a sentence.
        if (ex.Processing is { } already)
        {
            SayItWhereTheyAreLooking(Core.Processing.AlreadyBusyLine(already.Work, already.Label));
            return;
        }

        // And a shovel already in the ground is the same objection wearing a different glyph: every channel
        // on this surface draws the ONE progress bar (#562), so two at once is a captain watching a clock
        // that belongs to something else. The dig, the door-force and the drill all ask this same property
        // before they start.
        if (ex.AnyChannel)
        {
            return;
        }

        var hold = new ProcessingHold
        {
            Work = work,
            Item = item,
            Label = label,
            AnchorX = _avatarX,
            AnchorY = _avatarY,
            Floor = ex.Floor,
            Standing = standing,
            At = at,
        };
        ex.Processing = hold;

        // The pocket goes away so the fan comes back. See the note above — this is the one place the #691
        // "satchel stays open" call is deliberately reversed, and the reason is that the vulnerability IS
        // the mechanic.
        CloseSatchel();

        // ?process=0 · the QA switch. A story test must not have to wait out a clock designed to be felt,
        // and a zero-length hold is finished the instant it starts rather than one frame later — a frame is
        // a tick of air on the regolith, and a cheat that quietly charged for it would make the very guard
        // this lane ships flap. It also skips the "hold position" line, because a build that tells the
        // captain to stand still and then does not is a build whose prose has stopped being true.
        if (ProcessingSeconds <= 0)
        {
            CompleteProcessing(ex, hold);
            return;
        }

        ShowPulseMessage(Core.Processing.StartLine(work, label, ProcessingSeconds));
        RendererInterop.PlayCue("blip");
    }

    /// <summary>#696 · Advance the hold. Stepping off the spot — or riding the lift to another floor —
    /// abandons it; filling the clock fires the effect.
    ///
    /// <para>There is no air arithmetic in this method and there must never be any. StepSurface calls
    /// StepSuitAir on the same tick with the same dt, whatever this returns, so the hold is priced by where
    /// the captain is standing without one line here knowing that a tank exists.</para></summary>
    private void StepProcessing(double dtRealSeconds)
    {
        if (_surface is not { Processing: { } hold } ex)
        {
            return;
        }

        if (ex.Floor != hold.Floor
            || Core.Processing.Wandered(hold.AnchorX, hold.AnchorY, _avatarX, _avatarY))
        {
            AbandonProcessing(ex, Core.Processing.Interruption.Walked);
            return;
        }

        hold.Elapsed += dtRealSeconds;
        if (Core.Processing.Done(hold.Elapsed, ProcessingSeconds))
        {
            CompleteProcessing(ex, hold);
        }
    }

    /// <summary>#696 · The far end. The hold clears FIRST, so the effect it fires runs in a world with no
    /// hold in it — a leave that re-entered <see cref="LeaveItem"/> would otherwise meet its own clock and
    /// refuse itself.</summary>
    private void CompleteProcessing(SurfaceExcursion ex, ProcessingHold hold)
    {
        ex.Processing = null;
        RendererInterop.PlayCue("board");

        if (hold.Work == Core.Processing.Work.Read && hold.At is { } at)
        {
            // #603/#697 · Exactly the ending a press has always had, with exactly the arguments the press
            // would have passed. Not a copy of it — the same method — because a hand-written second ending
            // is this repo's first named bug class aimed at a state transition.
            TheOfferIsAnswered(SatchelTry.Offer(hold.Item, at.Target, at.Context), hold.Item, at);
            return;
        }

        SetItDown(ex, hold.Item, hold.Standing);
    }

    /// <summary>#696 · Cancel honestly. Nothing filed, nothing consumed, the paper still in the sleeve — and
    /// ONE line saying so, because a twenty-second investment that evaporates in silence reads as a lost
    /// press rather than as a decision the world took away from you.</summary>
    private void AbandonProcessing(SurfaceExcursion ex, Core.Processing.Interruption why)
    {
        if (ex.Processing is not { } hold)
        {
            return;
        }

        ex.Processing = null;
        SayItWhereTheyAreLooking(Core.Processing.AbandonedLine(hold.Work, hold.Label, why));
    }

    /// <summary>#696 · What the satchel says while a hold runs, or null when nothing is under the captain's
    /// hands. Composed in Core so the dialog and the standing prompt cannot grow two vocabularies for one
    /// clock.</summary>
    private string? ProcessingUnderway() => _surface is { Processing: { } hold }
        ? Core.Processing.HoldLine(hold.Work, hold.Label,
            Core.Processing.SecondsLeft(hold.Elapsed, ProcessingSeconds))
        : null;

    /// <summary>#696 · What the 🫳 control promises before it is pressed. A DOCUMENT costs seconds, because
    /// leaving one means photographing it first (#691); anything else is set down and that is all. The
    /// question "is this a document" is <see cref="LeftBehind.GistOf"/>'s — the SAME call
    /// <see cref="LeaveItem"/> branches on — so the hint and the press can never disagree about which rows
    /// have a clock behind them.</summary>
    private string LeaveHintFor(Core.Satchel.Item item) =>
        _surface is not null && LeftBehind.GistOf(item, WhereYouAreStanding()) is { Length: > 0 }
            ? Core.Processing.LeaveHint(ProcessingSeconds)
            : "Leave it here — it stays where you put it";

    /// <summary>#696 · The interruptions the hold cannot see coming, routed from the systems that CAN.
    ///
    /// <para>An air alarm breaks it on purpose. #564's founding rule is that air must never be a silent timer
    /// that kills you, and a warning that fires while the captain is watching a progress bar fill is exactly
    /// that timer wearing a costume — so the bar goes away and the alarm has the screen to itself. It fires
    /// once per walk per threshold (the warnings are one-shot), so it is a beat and never a lockout: the
    /// captain may start the same hold again on the next press and finish it on the reserve if that is the
    /// decision they want to take.</para></summary>
    private void ProcessingIsInterrupted(Core.Processing.Interruption why)
    {
        if (_surface is { Processing: not null } ex)
        {
            AbandonProcessing(ex, why);
        }
    }

    // ── #697 · THE WALLET IS ONE THING, AND IT COMES OUT ALL AT ONCE ────────────────────────────────────
    //
    // Owner: "Let's also add option to try all ID cards ... by grouping them into a folder in the inventory."
    // And, on the register the answer is written in: "It is a little throw at the movie ... where he had this
    // wallet with zillion different contradictory IDs :-D"
    //
    // A captain who has worked three sites is carrying several authorities that disagree about who they work
    // for, and holding them up used to be four presses producing four sentences of which one was worth
    // reading. The fold is Core's (SatchelTry.OfferWallet, #683's ladder); everything here is the gesture.

    /// <summary>#697 · Whether the wallet is open on the CARRIED page. Folded shut on every open
    /// (<see cref="TheSatchelOpensOnThePocket"/>) — the cards are one row until the captain asks for them.</summary>
    private bool _walletOpen;

    /// <summary>#697 · What is in the wallet, which is Core's own grouping of the pocket and never a filter
    /// written in the dialog: <see cref="Core.Satchel.CompartmentOf"/> is the law about which things are flat,
    /// and a second answer here would drift the first time a kind changes compartment (#688).</summary>
    private IReadOnlyList<Core.Satchel.Item> Wallet() =>
        Core.Satchel.OfKind(_satchel, Core.Satchel.Kind.Authority);

    /// <summary>#697 · Where the whole wallet can be offered at once: whatever the satchel is open AT, when
    /// that thing reads authorities. The question is Core's — the same <see cref="SatchelTry.CanOffer"/> the
    /// rows ask (#688) — so the folder can never carry a live offer at something the rows have gone inert
    /// for.</summary>
    private (SatchelTry.Target Target, string? Context, string Label)? WalletTarget() =>
        _satchelTarget is { } at && SatchelTry.CanOffer(Core.Satchel.Kind.Authority, at.Target) ? at : null;

    /// <summary>#697 · FAN THE WALLET AT THE READER. One press, every card, one line.
    ///
    /// <para>It performs no state transition of its own. A success ends through exactly the resolution a
    /// single successful try ends through (<see cref="TheOfferIsAnswered"/>), because a hand-written copy of
    /// that ending is this repo's first named bug class aimed at a state transition — and the day one of them
    /// learns to spend something, the other will not.</para></summary>
    private void TryTheWholeWallet()
    {
        if (WalletTarget() is not { } at)
        {
            return;
        }

        IReadOnlyList<Core.Satchel.Item> wallet = Wallet();
        if (wallet.Count == 0)
        {
            return;
        }

        // No item is charged to the fan: a wallet's success has no single row to spend, and an authority is
        // never consumed by being read anyway. Core has already decided which card answered.
        TheOfferIsAnswered(SatchelTry.OfferWallet(wallet, at.Target, at.Context), null, at);
    }

    /// <summary>#603 · Offer one carried thing to whatever the satchel is open at. The outcome is always
    /// SAID — a control that does nothing and says nothing is indistinguishable from a bug.</summary>
    private void TryItem(Core.Satchel.Item item)
    {
        if (TargetFor(item) is not { } at)
        {
            return;
        }

        // ── #696 · DECIDING A PAPER IS A MAP TAKES THE SAME SECONDS AS PHOTOGRAPHING ONE ──
        //
        // Owner's ruling covers both halves of the detective loop in one sentence, and it has to: a game
        // that charged for filing a document and handed the clue reading away free would be teaching the
        // captain to read everything on the spot and file nothing, which is the exact behaviour the cost
        // model exists to make a decision.
        //
        // The hold is started here and not inside TheOfferIsAnswered, because that method is the ENDING both
        // presses share (#697) — putting a clock inside it would put a clock in front of a wallet fan too.
        // Nothing else about the ending moves: the far end calls it with the same three arguments this line
        // would have.
        if (_surface is { } ex && item.Kind == Core.Satchel.Kind.Paper && at.Target == SatchelTry.Target.Tracker)
        {
            BeginProcessing(ex, Core.Processing.Work.Read, item, WhereYouAreStanding(), at);
            return;
        }

        TheOfferIsAnswered(SatchelTry.Offer(item, at.Target, at.Context), item, at);
    }

    /// <summary>#603 · What an offer DOES once it has an answer — the one ending both presses share.
    ///
    /// <para><paramref name="item"/> is the thing that was held up, or null when the whole wallet was fanned
    /// (#697): a fan has no single row to charge, and the two consuming branches below can only ever fire for
    /// a paper or a handful of rounds, neither of which is ever in a wallet. That is what makes "no double
    /// effects" structural rather than a promise.</para></summary>
    private void TheOfferIsAnswered(
        SatchelTry.Outcome outcome,
        Core.Satchel.Item? item,
        (SatchelTry.Target Target, string? Context, string Label) at)
    {
        // ── #680 · THE ANSWER IS SAID WHERE THE PLAYER IS LOOKING ──
        //
        // Owner, live, in caps: "pressing Try IT on item produces a text that is IMPOSSIBLE to read" /
        // "it is behind the blurring effect... so we don't tell the story."
        //
        // A refusal keeps the satchel open (#614 — a captain comparing three cards should not have to
        // reopen their pockets), and this method used to pulse the line FIRST and branch after — so every
        // refusal, the exact sentences #603's law exists for, played to the HUD under the backdrop's blur.
        // The sim told the story; the z-order ate it. In the DOM is not on the screen (the owner's own
        // formulation): the one layer the backdrop cannot blur is the dialog's own subtree, so a failed
        // offer is stored for the dialog to say, and only a success — which closes the modal — pulses.
        if (!outcome.Worked)
        {
            _satchelOutcome = outcome.Line;
            return;
        }

        ShowPulseMessage(outcome.Line);

        // ── #603 · READING A PAPER NEVER SPENDS IT ──
        //
        // Owner: "press I ... inventory opens... select paper and see what the clue is." / "it should be
        // viewable many times."
        //
        // The first cut consumed it, which was wrong twice over. It conflated LOOKING with DECIDING — one
        // click both read the document and burned it — and it broke the field book's own law (#587: "a find
        // that is shown once is a find that is lost"). A paper is a thing you own; you can take it out and
        // read it again in a year.
        //
        // So the document is always shown, in full, every time. The tracker gets plotted on the first read
        // and GrantLabLead no-ops on every one after, which is the honest shape: the knowledge is what is
        // one-shot, not the paper.
        if (item is { Kind: Core.Satchel.Kind.Paper } read && at.Target == SatchelTry.Target.Tracker)
        {
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                $"📋 {Core.FieldClue.Label(Core.FieldClue.CertaintyOf(read.Id)).ToUpperInvariant()}",
                "",
                Core.FieldClue.Document(read.Id) + "\n\n" + outcome.Line);

            GrantLabLead(DiceRule.Seed($"clue:{read.Id}"));
        }

        // ── #603 case 2 · THE ROUNDS GO IN, AND THE GUN REMEMBERS WHAT THEY WERE ──
        //
        // Six rounds is not a resupply, it is a decision — which gun, and with what. A sentry loaded with
        // the lab round clears a line with one shot and refuses anything already on top of it; one loaded
        // with issue ball does neither. Both facts have to live on the magazine, so the bot carries the kind.
        if (item is { Kind: Core.Satchel.Kind.Rounds } fed && at.Target == SatchelTry.Target.DrySentry
            && _surface is { } loadEx)
        {
            SurfaceBot? gun = loadEx.Bots.FirstOrDefault(b => b.Deployed && b.Unit == at.Context);
            if (gun is not null)
            {
                gun.Rounds += fed.Count;
                gun.AmmoId = fed.Id;
                _satchel = [.. Core.Satchel.Remove(_satchel, fed.Kind, fed.Id, fed.Count)];

                Core.Ammunition.Kind kind = Core.Ammunition.ById(fed.Id);
                if (kind.MinimumRangeDu > 0)
                {
                    ShowPulseMessage(
                        $"🔫 {gun.Unit} takes them. It will not fire these at anything closer than " +
                        $"{kind.MinimumRangeDu:F0} du — they arm after travel, and that is the whole point " +
                        "of them.");
                }
            }
        }

        CloseSatchel();
    }

    /// <summary>#603 · What this item can be offered to right now.
    ///
    /// <para>Opened AT something — a door, a gate — everything goes to that. Opened from nowhere with the I
    /// key, most things are just a look, but a DOCUMENT can always be read as a clue, because the tracker is
    /// on the captain's arm and deciding a paper is a map is something they can do standing anywhere. That
    /// is the owner's own framing: the lead is not granted on pickup, it is granted when the player decides
    /// the paper means something.</para>
    ///
    /// <para>#688 · AT A DOOR, ONLY A KEY. Owner: <i>"Let's make a bigger story point about finding any kind
    /// of key or keycard and only suggest those at doors. Or tools, but not just like some papers."</i> The
    /// law is Core's (<see cref="SatchelTry.CanOffer"/>) and this is the one place that can route around it,
    /// so it asks. Nothing about the REFUSALS changed — a captain holding three authorities still gets every
    /// wrong-shaft and wrong-site reading #679 wrote. What stopped is the game dangling forty live offers at
    /// a bulkhead to hide the one that mattered.</para></summary>
    private (SatchelTry.Target Target, string? Context, string Label)? TargetFor(Core.Satchel.Item item)
    {
        if (_satchelTarget is { } at)
        {
            return SatchelTry.CanOffer(item.Kind, at.Target) ? at : null;
        }

        // A document can always be read — the tracker is on the captain's arm.
        if (item.Kind == Core.Satchel.Kind.Paper)
        {
            return (SatchelTry.Target.Tracker, null, "the motion tracker");
        }

        // #603 case 2 · And rounds go into a gun you are STANDING AT. Owner: "if we run empty on our
        // autoguns and have those on our inventory then from there we should be able to load them into the
        // guns." The interesting case is precisely a sentry that has run dry out in the field, away from the
        // tube — a handful is never a resupply, but it might be enough to get you home.
        if (item.Kind == Core.Satchel.Kind.Rounds && DrySentryUnderfoot() is { } unit)
        {
            return (SatchelTry.Target.DrySentry, unit, unit);
        }

        return null;
    }

    /// <summary>#603 · The dry sentry within reach, if there is one. Deployed and out of ammunition: a live
    /// one does not want your six rounds and a carried one is not deployed.</summary>
    private string? DrySentryUnderfoot()
    {
        if (_surface is not { } ex)
        {
            return null;
        }

        double radiusSq = DeckPlan.InteractRadius * DeckPlan.InteractRadius;
        foreach (SurfaceBot b in ex.Bots)
        {
            if (!b.Deployed || b.Rounds > 0)
            {
                continue;
            }
            double dx = b.X - _avatarX, dy = b.Y - _avatarY;
            if ((dx * dx) + (dy * dy) <= radiusSq)
            {
                return b.Unit;
            }
        }
        return null;
    }

    /// <summary>What to write on one row of the pocket. The prose is rebuilt from the world here rather than
    /// stored, so a save can never go stale against the words.</summary>
    private static string SatchelLabel(Core.Satchel.Item item) => item.Kind switch
    {
        Core.Satchel.Kind.Authority =>
            UndergroundComplex.AuthorityCard.TryParse(item.Id, out UndergroundComplex.AuthorityCard c)
                ? UndergroundComplex.CardTitle(c)
                : "🎫 an authority card",
        // #613 · Each paper by its own name. Owner: "the operational papers could have individual short
        // titles… now they look identical in inventory." The certainty stays on the end, because that is the
        // one thing about a paper worth comparing across a pocketful of them.
        Core.Satchel.Kind.Paper =>
            $"📋 {Core.FieldClue.Title(item.Id)} — {Core.FieldClue.Label(Core.FieldClue.CertaintyOf(item.Id))}",
        Core.Satchel.Kind.Rounds => item.Id == Ammunition.LabTwoStage.Id
            ? $"🔫 {item.Count} × {Ammunition.LabTwoStage.Name}"
            : $"🔫 {item.Count} loose round{(item.Count == 1 ? "" : "s")}",

        // #614 · Named for what you actually have, which is paperwork about a thing you left in a room.
        // #677 · …and there are two of those now, told apart by the find's own id and named by the same
        // authored fragment the look-card is titled with — the odd book's idiom, and it means no row prose
        // was invented for a hall.
        Core.Satchel.Kind.Relic => UndergroundComplex.IsHallRecord(item.Id)
            ? UndergroundComplex.FoundRecordCardLabel
            : "⭕ measurements of the thing on the pallet",

        // #746 · The day-labour chit, printed as it is printed. Both ways of getting it are the same piece
        // of paper — the name in the book downstairs is not on the card, which is the whole horror of it.
        Core.Satchel.Kind.Chit => $"{CanteenTable.ChitGlyph} {CanteenTable.ChitTitle}",

        _ => "🗃 a file on somebody",
    };

    // ── #588 · A PERSON, OUT OF THE PIECES ─────────────────────────────────────────────────────────────
    //
    // Owner: "when we find somebody's kit maybe we get gen ai compilation of what we discover about them...
    // nice place for world building and dropping bread crumbs about our big plot", and then the part that
    // makes it a MECHANIC rather than a lore drop — "if we know what happened to someone we get contacts
    // easily by contacting their loved ones, in some cases that might lead our gum-shoe-efforts forward."
    //
    // Three pieces of kit make a person (one is litter, two is a coincidence). The payoff is not loot: it is
    // an errand, and a name to drop, and — sometimes — somebody who has been waiting nine years for news and
    // knows something nobody would ever tell a pirate.
    //
    // ── #774 · AND THE SENTENCES ARE READ ON THE CARD, NOT UNDER IT ────────────────────────────────────
    //
    // This method used to raise the card and then fire two to four ShowAndFile lines beneath it: the person,
    // the next of kin, what the family knows, the in that fell out of the kit — every one of them pulsed
    // into the HUD while a full-screen backdrop stood in front of the HUD. All of them were filed, none of
    // them was readable, and the captain closed the card onto silence.
    //
    // #768's hold cannot settle it and was refused for exactly this case: it releases ONE winner, and these
    // are a same-rank SEQUENCE whose survivor would have been decided by which line was appended last —
    // the ordering-as-contract bug #693 killed. So the remedy is #736's law instead. The card carries them,
    // in Core's own reading order (FieldDossier.Beat), and the book keeps every one of them exactly as it
    // always did: what changed is where a sentence is READ, never what is recorded.
    private void AssembleSomebody(SurfaceExcursion ex, string body, string salt, int roomIndex)
    {
        ex.KitPieces.Add(roomIndex);

        // ?kit=1 — the picture comes together on the FIRST piece. Three papers rooms at one room in eight,
        // inside a single excursion, is the rarest thing on the regolith; the cheat moves the gate and
        // nothing else, so what a tester reads is a dossier a captain can genuinely be handed.
        int enough = _kitCheat ? 1 : FieldDossier.FragmentsToAssemble;
        if (ex.KitPieces.Count < enough || ex.DossierShown)
        {
            return;
        }
        ex.DossierShown = true;

        // The stranger is keyed on the room whose papers COMPLETED the picture, so which body you are
        // holding is a fact about where you searched — not a global roll that would have happened anyway.
        FieldDossier.Person who = FieldDossier.Who(body, salt, roomIndex);
        string place = Core.FieldNotes.PlaceLabel(ex.Stop.Body.Name, ex.Site.Name);

        // Everything this kit has to say, already in the order it is read — composed in Core beside the
        // rolls that decide whether each sentence exists at all, so the client never picks an order.
        IReadOnlyList<FieldDossier.Saying> debrief =
            FieldDossier.Debrief(body, salt, roomIndex, everySaying: _kitCheat);

        // The card, with the compiled effects AND the debrief on it. Reuses the ViewObject pop-up the
        // builder's plate and the souvenirs already use — one image surface, not a second one to keep in
        // step. The caption is the fiction; the outcome region is what you are now holding.
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            FieldDossier.ConsoleLabel, FieldDossier.ArtUrl, FieldDossier.Compiled(who, place),
            FieldDossier.DebriefBlock(debrief));

        RendererInterop.PlayCue("board");

        // The book, unchanged: the same sentences under the same glyphs in the same order they were said.
        foreach (FieldDossier.Saying said in debrief)
        {
            FileNote(said.Text, said.Glyph);

            // #585: and what the family knows is a PLACE. This is the owner's own chain closing — "if we
            // know what happened to someone we get contacts easily by contacting their loved ones, in some
            // cases that might lead our gum-shoe-efforts forward" — arriving, eventually, at a moon on the
            // tracker. Banked at the same point in the sequence it has always stood, between the hint and
            // the in, because the field book's order is a record of when things were said.
            if (said.Beat is FieldDossier.Beat.WhatTheFamilyKnows)
            {
                GrantLabLead(DiceRule.Seed($"lead:kin:{body}:{salt}:{roomIndex}"));
            }
        }

        ApplyNerveShock(4.0, "a stranger's whole life, laid out on a rock");
    }

    // ── #585 · THE DETECTOR, SWEEPING ─────────────────────────────────────────────────────────────────
    //
    // Owner: "the detector should also give detecting readings near it."
    //
    // The probe was all-or-nothing — the exact square pings, its eight neighbours shriek, and the other four
    // thousand squares say nothing — so finding a lab was a lottery rather than a search. This is the
    // hot-and-cold every treasure hunt has run on forever: a reading that climbs as you close and falls away
    // as you drift, so a captain can pick a bearing, walk it, and TURN when it cools.
    //
    // It only wakes on a moon somebody has named (#585's leads). A detector that hummed everywhere would hand
    // over every lab in the system for free and make the whole clue chain pointless.
    private SecretLab.Reading _lastDetectorReading = SecretLab.Reading.Silent;

    private void StepSecretLabDetector()
    {
        if (_surface is not { } ex
            || ex.Lab is not { HasLab: true } lab
            || ex.SecretLabDoorRevealed
            || !_labLeads.Contains(ex.Stop.Body.Id))
        {
            _lastDetectorReading = SecretLab.Reading.Silent;
            return;
        }

        double dx = lab.DoorX - _avatarX, dy = lab.DoorY - _avatarY;
        SecretLab.Reading now = SecretLab.ReadingAt(Math.Sqrt((dx * dx) + (dy * dy)));
        if (now == _lastDetectorReading)
        {
            return;   // speak on the CHANGE only; a needle that narrates every frame is noise
        }

        bool warmer = now > _lastDetectorReading;
        _lastDetectorReading = now;

        string line = SecretLab.ReadingLine(now, warmer);
        if (line.Length > 0)
        {
            ShowPulseMessage(line);
            if (warmer && now >= SecretLab.Reading.Strong)
            {
                RendererInterop.PlayCue("pulse");
            }
        }
    }

    /// <summary>#585 · A clue names a moon. Called from every find in the gumshoe chain — a file in a
    /// facility, papers in a ruin, what a dead specialist's family turns out to know.</summary>
    /// <summary>Names a moon worth searching, and returns WHICH — #613, so a Key found on a bottom floor can
    /// mint the card for the shaft it points at. Returns the body even when the lead was already known: the
    /// lead is news you can only hear once, the card is an object that exists regardless.</summary>
    private string? GrantLabLead(ulong seed)
    {
        if (NameAMoonWorthLookingAt(seed) is not { } named)
        {
            return null;
        }
        AnnounceLabLead(named);
        return named;
    }

    /// <summary>#684 · WHICH moon, and NOTHING else — no lead written down, no line said, no save asked for.
    ///
    /// <para>Split out because one caller has to know the answer BEFORE it is allowed to keep it. A Key found
    /// on a bottom band mints its card for the site a lead names (#613), and that mint can be refused by a
    /// full pocket (#678) — at which point the room is not emptied and searching it again offers the same
    /// find. The lead was being banked and SAID during the naming, one step ahead of the capacity check, so a
    /// captain with no room left walked away holding the knowledge and none of the card. The sentence was
    /// composed before the act it describes, which is the exact fault #678 was filed about, surviving in the
    /// one branch of it that reached outside the pocket.</para></summary>
    private string? NameAMoonWorthLookingAt(ulong seed)
    {
        if (_surface is not { } ex)
        {
            return null;
        }

        var candidates = new List<string>();
        foreach (ShuttleStop stop in ShuttleDestinationsInRange())
        {
            if (stop.IsLandable && !Derelict.TryParseWreckId(stop.Body.Id, out _))
            {
                candidates.Add(stop.Body.Id);
            }
        }
        if (!candidates.Contains(ex.Stop.Body.Id))
        {
            candidates.Add(ex.Stop.Body.Id);
        }

        return SecretLab.MoonWorthLookingAt(candidates, seed);
    }

    /// <summary>#684 · Bank the lead and say it — once. News you can only hear the first time, which is why
    /// it must not be spent on a find the pocket then refuses.</summary>
    private void AnnounceLabLead(string named)
    {
        if (!_labLeads.Add(named))
        {
            return;
        }

        string display = ShuttleDestinationsInRange()
            .FirstOrDefault(s => s.Body.Id == named)?.Body.Name ?? named;

        // #774 · Where the captain is looking, because one of this method's callers is the dossier assembly
        // and the dossier's own card is up when it calls — a moon named into a pulse behind that backdrop is
        // the fifth sentence of the same bug. Every other caller reaches this with nothing in front of them,
        // where the seam is an ordinary pulse and indistinguishable from one.
        SayWhereTheyAreLookingAndFile(SecretLab.LeadLine(display), "🔎");
        RequestVaultSave();
    }

    // ── #587 · THE FIELD BOOK ──────────────────────────────────────────────────────────────────────────
    //
    // Owner: "we should maybe collect the tips to ledger if we don't show them again?" — and he is right,
    // because until now they were NOT shown again. Every find out there arrived through ShowPulseMessage,
    // which fades after at most eight seconds and is then gone: you walk twenty minutes across a vacuum for
    // a sentence you cannot read twice.
    //
    // Exactly the bug he already ruled on for the bar (#347: the words a player paid for "may not hide"),
    // so it gets exactly the same answer — a durable, capped, vault-persisted book, projected into the
    // captain's ledger grouped by PLACE. On the ground the thing you want back is not who told you, it is
    // where you were standing.
    private List<Core.FieldNote> _fieldNotes = [];

    // #585 · MOONS SOMEBODY HAS NAMED. Owner: "We will be needing some kind of clue in the plot arc to the
    // radar to really find it in reasonable time in the game :-D ... now we kind of found it by just knowing
    // it is here somewhere."
    //
    // This is the missing link in the whole gumshoe chain. A clue found in a facility, a ruin or a dead
    // specialist's family names a MOON; landing on a named moon wakes the detector and the tracker's vague
    // wash. Without it the search was a lottery with four thousand tickets.
    private readonly HashSet<string> _labLeads = [];

    // ── #603 · THE SATCHEL: everything the captain is carrying on foot ──────────────────────────────────
    //
    // Owner: "we should have some option to try use those at the locked doors... maybe we need like
    // on-site-carried-items inventory ... The captains ledger has the ship stuff but we should have
    // something similar on foot."
    //
    // This REPLACES the #590 card set. Cards were the first thing the captain carried that the player could
    // not see, and by the time papers, files and loose rounds joined them there would have been four private
    // stores and still no pockets. One list now, and a panel draws it.
    //
    // Durable, not per-excursion: you find a thing eleven floors under a moon, fly home, come back a month
    // later and it is still in your pocket. So it rides in the vault beside the leads rather than on the
    // SurfaceExcursion, which is thrown away the moment the shuttle lifts.
    private List<Core.Satchel.Item> _satchel = [];

    /// <summary>#590 · The card ids the lift panel and its gate ask about, DERIVED from the satchel so there
    /// is one store. A second copy kept in step by hand is the failure this ground's spec opens with a table
    /// of.</summary>
    private HashSet<string> AuthorityCardIds() =>
        [.. Core.Satchel.OfKind(_satchel, Core.Satchel.Kind.Authority).Select(i => i.Id)];

    // #684 · HeldAuthorities() lived here — the wallet parsed back into cards, sorted, so the panel's own
    // refusal line could name every one of them. It went with WrongCardLine: the gate's answer is the
    // matrix's now, and the matrix is handed the SATCHEL rather than a second list derived from it.

    /// <summary>Say it AND keep it. Every durable find on a surface goes through here rather than through
    /// ShowPulseMessage directly, so there is one place that can never be forgotten about — the pulse is the
    /// doorbell, the book is the record.
    ///
    /// <para>#693 · The book keeps everything whatever the screen does, so <paramref name="rank"/> only ever
    /// decides the doorbell. A line that loses the slot is still filed, in the order it was said.</para>
    /// </summary>
    private void ShowAndFile(string text, string glyph, PulseRank rank = PulseRank.Status)
    {
        ShowPulseMessage(text, rank);
        FileNote(text, glyph);
    }

    /// <summary>#774 · <see cref="ShowAndFile"/>'s sibling for a durable find announced in the same breath as
    /// a card that will stand in front of it: the book keeps it, and the SAYING goes wherever the captain's
    /// eye actually is (<see cref="SayItWhereTheyAreLooking"/>) rather than to a HUD behind a backdrop. With
    /// nothing raised the two are the same method — there is no rank because there is no contest: this line
    /// is not competing for a slot, it is being written where it can be read.</summary>
    private void SayWhereTheyAreLookingAndFile(string text, string glyph)
    {
        SayItWhereTheyAreLooking(text);
        FileNote(text, glyph);
    }

    // ── #768 · WHEN THE EVENT THAT SPEAKS ALSO RAISES A CARD ───────────────────────────────────────────
    //
    // #693 gave the one pulse slot a law, and left one loss it could not settle: an ARRIVAL that raises a
    // card says its lines and then puts a full-screen backdrop in front of them. The first-descent card
    // (#585) over the gate-accepted beat (#689) is the case the owner filed; the repo boat's plate (#583)
    // over its own arrival line is the same shape. No rank helps — the line is not losing to a bigger line,
    // it is losing to the whole HUD, and the dwell runs out behind the blur while the captain reads the card.
    //
    // So an event that may raise a card HOLDS its sayings (PulseHold, Core — the same rank law, minus the
    // clock, because the lines were composed in one breath) and the card's dismissal lets the winner go. The
    // book still keeps every one of them at the moment they were said: what is deferred is the DOORBELL, not
    // the record, and not the event.
    //
    // Deliberately NOT a general queue: #693 declined that and it stays declined. An event that raises no
    // card releases on the spot, which is an ordinary pulse and indistinguishable from one.

    /// <summary>#768 · Say it AND keep it — but not yet, if a card is about to stand in front of it. The book
    /// gets it now; the screen gets the winner when the card closes. Pair every caller with
    /// <see cref="ReleaseHeldSayingsUnlessACardStopsTheWorld"/> once the event's cards have been raised.</summary>
    private void HoldAndFile(string text, string glyph, PulseRank rank = PulseRank.Status)
    {
        _held = _held.Hold(text, rank);
        FileNote(text, glyph);
    }

    /// <summary>#768 · The same, for a line the book does not keep — a warning about the here and now rather
    /// than a durable find.</summary>
    private void HoldSaying(string text, PulseRank rank = PulseRank.Status) =>
        _held = _held.Hold(text, rank);

    /// <summary>#768 · Is something in front of the captain that a pulse would play UNDER? The two full-screen
    /// cards, asked of the world as it now stands rather than predicted from the conditions that raise
    /// them — a copy of those conditions is a second rule to keep in step, and this one cannot be wrong.</summary>
    private bool ACardStopsTheWorld => _viewObject is not null || _revealCard is not null;

    /// <summary>#768 · The end of an event that had things to say: if nothing is in front of the captain the
    /// held winner is simply pulsed, here and now, exactly as it always was. If a card IS up, it stays held
    /// and <see cref="ReleaseHeldSayings"/> says it when that card is dismissed.</summary>
    private void ReleaseHeldSayingsUnlessACardStopsTheWorld()
    {
        if (ACardStopsTheWorld)
        {
            return;
        }
        ReleaseHeldSayings();
    }

    /// <summary>#768 · The card is gone — say what it was standing on. Called from every road out of a card,
    /// which is why all of them go through CloseViewObject / CloseRevealCard and none of them clear the field
    /// by hand. A released line takes its ordinary dwell and may be outranked afterwards like any other: a
    /// held line is a line that has not been said yet, never a line with special powers.</summary>
    private void ReleaseHeldSayings()
    {
        if (!_held.Any)
        {
            return;
        }
        (_pulse, _held) = _held.ReleaseInto(_pulse, _lastTimestampMs ?? 0);
    }

    /// <summary>#686 · The record half alone, for a line whose SAYING happens inside an open dialog — the
    /// pulse would play under that dialog's blur, but the book must still remember.</summary>
    private void FileNote(string text, string glyph)
    {
        if (_surface is not { } ex || string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        _fieldNotes = [.. Core.FieldNotes.Append(_fieldNotes, new Core.FieldNote(
            text, SimTime, Core.FieldNotes.PlaceLabel(ex.Stop.Body.Name, ex.Site.Name), glyph))];
    }

    // ── #586 · WHAT SOMEBODY LEFT AT THE FOOT OF THE MONOLITH [E] ──────────────────────────────────────
    //
    // Owner: "let's have gen AI image at the monolith and some items appearing there now and then ... it is
    // supposed to be impressive... now it looks like a box in closet."
    //
    // The picture is the [E] on the slab itself. THIS is the other half, and it is the half that makes the
    // place alive: a landmark that never changes is scenery you visit once. Every line here is somebody
    // ELSE's visit — a cutting rig laid down neatly, a scoured plate, bootprints that all face the slab and
    // none lead away. The monolith itself never speaks, never reacts, and is never confirmed to have noticed
    // anything, which is the whole register (see reever-origin canon: the game never explains this).
    private void MonolithFootInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.MonolithFoot })
        {
            return;
        }

        long epoch = Monolith.EpochAt(SimTime);
        Monolith.Offering left = Monolith.AtTheFoot(ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch);
        if (left == Monolith.Offering.Nothing)
        {
            return;
        }

        string key = $"monolith:{epoch}";
        if (!ex.RuinsSearched.Add(key))
        {
            ShowPulseMessage("You have already looked at it. It has not changed.");
            return;
        }

        ShowAndFile(Monolith.FootLine(left, ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch), "▮");

        // It costs nerve to stand here reading somebody else's last afternoon. Remains cost more.
        ApplyNerveShock(left == Monolith.Offering.Remains ? 5.0 : 2.0,
            "somebody else got this far, and this is what is left of their visit");
        RequestVaultSave();
    }

    // ── #573 · THE SHELTER'S EMERGENCY LOCKER [E]. Owner, on Andy Weir's bubble shelters: they "should also
    //    contain reload to guns". A shelter stocked with air and nothing else is a tap, not a refuge. ──
    private void ShelterLockerInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.ShelterLocker })
        {
            return;
        }
        int whichLocker = ShelterUnderfoot(ex);
        if (whichLocker < 0)
        {
            return;
        }

        var takers = ex.Bots.Where(b => b.Rounds < SentryBot.MaxMagazine).ToList();
        if (takers.Count == 0)
        {
            ShowPulseMessage(SurfaceShelter.LockerFullLine);
            return;
        }

        // #580 · EVERY MAGAZINE, EVERY TIME, FOR AS LONG AS YOU CARE TO STAND HERE. Owner: "we want in
        // practise unlimited reloads of rounds at the shelters not like couple mags". No drawer, no
        // reservoir, no cooldown — the press is the point of the building. What this costs is the walk here
        // and the air it took, which is where the pressure in an excursion is supposed to live.
        int loaded = 0;
        foreach (SurfaceBot bot in takers)
        {
            loaded += SentryBot.MaxMagazine - bot.Rounds;
            bot.Rounds = SentryBot.MaxMagazine;
        }

        RendererInterop.PlayCue("board");
        ShowPulseMessage(SurfaceShelter.LockerLine(loaded));
        RequestVaultSave();
    }

    private void ShelterTankInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.ShelterTank })
        {
            return;
        }

        // #573 · The rack is not a button any more — it pumps on its own for as long as a captain stands in
        // the shelter (StepSuitAir), because the owner is right that the PUMPING TIME is the honest cost:
        // "the time it takes to pump air is good incentive to not take too much". So [E] reads the gauge
        // rather than working a lever. An affordance that did nothing would be worse than none (#212), so it
        // tells you what the machine is doing and lets you decide how long to stand there.
        int which = ShelterUnderfoot(ex);
        if (which < 0)
        {
            ShowPulseMessage("🫁 The rack's fitting is inside. Step in out of the vacuum.");
            return;
        }

        double held = ShelterReservoirNow(ex, which);
        ShowPulseMessage(RackGaugeLine(ex, held));
    }

    // ── #608 · THE REFUGE'S RACK [E]. The same gauge, in a poured room under a moon. ─────────────────────
    //
    // Owner: "Still for safety there would need to be a couple of places with air lock and air refilling,
    // because otherwise the elevator being busy could kill employees, and those honest criminal scientists
    // are hard to recruit :-D"
    //
    // It reads the machine rather than working a lever, for the identical reason the shelter's does: the
    // rack pumps on its own for as long as you stand in the air (StepSuitAir), and the PUMPING TIME is the
    // honest cost. An affordance that did nothing would be worse than none (#212), so [E] says what the
    // machine is doing and leaves the captain to decide how long they dare stand there — which, on a dead
    // floor with the lift several rooms away, is a much sharper decision than it is on the regolith.
    private void HiveRefugeInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.HiveRefuge })
        {
            return;
        }

        int which = RefugeUnderfoot(ex);
        if (which < 0)
        {
            ShowPulseMessage("🫁 The rack's fitting is through the inner door. Step into the air.");
            return;
        }
        ShowPulseMessage(RackGaugeLine(ex, RefugeReservoirNow(ex, which)));
    }

    // ── #707 · THE COUNTER, THE BASINS AND THE MACHINES ─────────────────────────────────────────────────
    //
    // Owner: "all the secret labs dont have any cantina / bar nor any toilets."
    //
    // ONE verb for all three rooms, because it is one verb: stand in a room somebody ate or washed in and
    // read what is left of it. There is deliberately nothing to spend and nothing to pick up — this issue
    // builds the ROOMS, and the social layer that belongs in them (overhearing the next table, the exposure
    // a stranger's face costs in a room where every face is known, the meal-line ID check) is filed
    // separately by the owner's own scope split.
    //
    // WHICH ROOM IT IS COMES FROM CORE, matched by position exactly the way HiveHaulInteract matches a room
    // index. Reading it off the console's own label would have been closer to hand and would have been a
    // second copy of the fixture's name, going quietly wrong the day somebody edits one of the two.
    private void HiveAmenityInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveAmenity } spot)
        {
            return;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        for (int i = 0; i < floor.Amenities.Count; i++)
        {
            UndergroundComplex.Amenity a = floor.Amenities[i];
            if (Math.Abs(a.X - spot.X) >= 0.5 || Math.Abs(a.Y - spot.Y) >= 0.5)
            {
                continue;
            }

            // ── #756 · A COUNTER THAT TAKES ORDERS ANSWERS THE PRESS ITSELF ───────────────────────────
            //
            // Owner, live at this exact fixture: "HOW DO I ORDER A DRINK FROM THE BAR?????? I walk to the
            // bar to buy a drink... not possible... WHY?" What is below was the WHOLE of what pressing E
            // here ever did — a paragraph ABOUT a counter, read standing at the counter, with a purse in
            // your pocket and nothing in the building to spend it on.
            //
            // CORE SAYS WHETHER THIS FIXTURE SERVES, and the card that opens is the very one the Tilt bar
            // has opened since #247. The room's first-entry paragraph still lands — through FileNote rather
            // than ShowAndFile, because the pulse half would now play UNDER the card's own blur (#686/#736)
            // and a line said where nobody is looking is a line not said.
            if (Core.Interior.CounterService.For(ex.Stop.Body.Id, a.Use) is { } counter)
            {
                if (ex.HiveAmenitiesRead.Add(HiveInterior.RoomKey(ex.Floor, i)))
                {
                    FileNote(UndergroundComplex.AmenityLine(ex.Stop.Body.Id, a.Use), "🍽");
                    RequestVaultSave();   // the field note is a possession too (#587)
                }
                OpenCounterService(counter);
                return;
            }

            // FILED, not flashed. The mess's manifest is a lead about somewhere else entirely and the bar's
            // paint is the only warm thing in the building; both are worth being able to re-read, and a line
            // that faded in eight seconds would be #587's lesson unlearned. Once per room per excursion.
            if (ex.HiveAmenitiesRead.Add(HiveInterior.RoomKey(ex.Floor, i)))
            {
                ShowAndFile(UndergroundComplex.AmenityLine(ex.Stop.Body.Id, a.Use), "🍽");
                RequestVaultSave();   // the field note is a possession too (#587)
            }
            else
            {
                ShowPulseMessage(a.Plate);
            }
            return;
        }
    }

    // ── #725 · THE ROOM NOBODY EATS IN, NOTICED ON THE WAY IN ────────────────────────────────────────────
    //
    // Owner's audit: "are we giving enough attention to plot-significant finds? They should have a Gen-AI
    // image and their own dialog by our standards." The staff mess was map furniture — a plate, four machines
    // and three tables — and a captain who walked through it without pressing E got nothing at all.
    //
    // A ROOM BEAT AND NOT A FLOOR BEAT, which is the one thing that makes it different from every other card
    // down here. The floor it sits on is an ordinary floor; the find is a door and what is behind it. So this
    // is the refuge idiom (poll the position, ask Core whether the room holds you) rather than the lift
    // idiom, and the containment law is UndergroundComplex's own — Amenity.Contains delegates to RefugeHolds,
    // so the box the card fires in is the box the walls are drawn on.
    //
    // WHICH ROOM COMES FROM CORE: the floor plan's own StaffCanteen amenity, which CarveAmenities only ever
    // makes on StaffCanteenFloor. Nothing here re-asks which floor that is — a second answer to a question
    // Core already owns is the table at the top of UndergroundComplex.cs.
    //
    // Once per excursion, like DEAD AIR: the first time is the find, and after that it is a canteen.
    //
    // AND IT IS ASKED ONCE PER FLOOR, not once per frame. UndergroundComplex.Build lays a whole floor out;
    // running it inside the step loop would be a generator call every tick for as long as the captain is
    // underground, which is the cost the renderer already pays exactly once. Cached against (body, floor)
    // because Build is pure and deterministic per that pair — the same reason a floor is the same floor
    // every visit.
    private (string Body, int Floor)? _messRoomFor;
    private UndergroundComplex.Amenity? _messRoom;

    private void CheckStaffMessUnderfoot()
    {
        // #746 · TWO beats live in this room now, and they are not the same beat. The room's own card is the
        // FIND (once, #743); showing the chit to it is what somebody with a reason to be here does, and a
        // captain who walked through this room on an earlier floor-crawl and only later talked their way onto
        // the cage crew must still get it. So the poll runs until BOTH have had their turn.
        if (_surface is not { } ex || ex.Floor >= 0 || _viewObject is not null
            || (ex.HiveStaffMessShown && ex.MessChitBeatShown))
        {
            return;
        }

        if (_messRoomFor != (ex.Stop.Body.Id, ex.Floor))
        {
            _messRoomFor = (ex.Stop.Body.Id, ex.Floor);
            _messRoom = null;
            foreach (UndergroundComplex.Amenity a in
                UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
            {
                if (a.Use == UndergroundComplex.Comfort.StaffCanteen)
                {
                    _messRoom = a;
                    break;
                }
            }
        }

        if (_messRoom is { } mess && mess.Contains(_avatarX, _avatarY))
        {
            if (!ex.HiveStaffMessShown)
            {
                ex.HiveStaffMessShown = true;
                _viewObject = new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                    UndergroundComplex.StaffMessLabel,
                    UndergroundComplex.StaffMessArtUrl,
                    UndergroundComplex.StaffMessCard);
                RendererInterop.PlayCue("reveal");
                return;   // one beat at a time — see below
            }

            // #746 · …and if you are carrying the cage crew's chit, you eat. ADDITIVE, and deliberately
            // second: #743's card is the find, and this is what somebody with a reason to be here does next.
            //
            // NEVER IN THE SAME FRAME AS THE CARD. This line is PULSED, and a pulse played while a card is up
            // renders under that card's backdrop and its blur — #680, exactly. The poll survives the card
            // (see the guard at the top), so the beat lands on the tick after the captain closes it, still
            // standing in the room, with nothing over the top of it.
            ShowMessChitBeat(ex);
        }
    }

    // ── #751 · THE HALL, AND THE DOORS ALONG THE BACK OF IT ──────────────────────────────────────────────
    //
    // Owner: these rooms are story-grade — they get first-entry CARDS with gen-AI art, the same one-shot
    // pattern as THE PLATE and THE STAFF MESS. So this is #725's idiom, verbatim: poll the position, ask
    // CORE whether a room holds you, raise the card once and never again this excursion.
    //
    // TWO ROOMS ON ONE POLL, and the order is the order a captain meets them: you cannot be in a cabinet
    // without having been in the hall, so the hall's card is raised first and the cabinet's on a later tick.
    // One card at a time — #680: a second card raised in the same frame renders under the first one's
    // backdrop and its blur.
    //
    // AND THE CABINET FILES ITS NOTE OFF THE SAME LATCH. The card is the moment and the note is the book's
    // compressed record of it; two latches would be two ways for one event to be remembered, and a save in
    // between would eventually show one without the other.
    private (string Body, int Floor)? _hallFor;
    private UndergroundComplex.Amenity? _hallRoom;

    private void CheckCantinaHallUnderfoot()
    {
        if (_surface is not { } ex || ex.Floor >= 0 || _viewObject is not null)
        {
            return;
        }

        // Both beats spent, and the poll stands down for the rest of the excursion. The hall's own card is
        // asked of CORE rather than of the flag alone, because there is one building where it never fires
        // at all (the head office's dining room, #411) — and a poll that waited for a card that is never
        // coming would run for the whole walk.
        bool hallDue = !ex.HiveCantinaHallShown
            && UndergroundComplex.ShowsCantinaHallCard(ex.Stop.Body.Id, ex.Floor);
        if (!hallDue && ex.HiveCabinetShown)
        {
            return;
        }

        // Cached against (body, floor) for the reason #725's own comment gives: Build lays a whole floor
        // out, and running it inside the step loop would be a generator call every tick for as long as the
        // captain is underground.
        if (_hallFor != (ex.Stop.Body.Id, ex.Floor))
        {
            _hallFor = (ex.Stop.Body.Id, ex.Floor);
            _hallRoom = null;
            foreach (UndergroundComplex.Amenity a in
                UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
            {
                if (a.Use == UndergroundComplex.Comfort.UpperCanteen && a.Hall is not null)
                {
                    _hallRoom = a;
                    break;
                }
            }
        }

        if (_hallRoom is not { } hallRoom || hallRoom.Hall is not { } hall
            || !hallRoom.Contains(_avatarX, _avatarY))
        {
            return;
        }

        // WHICH BUILDING'S HALL EARNS THE CARD IS CORE'S CALL (see hallDue above). The card names the sign
        // on the door — CANTEEN 1 · CARRIERS & CONTRACTORS — and the head office's dining room is a
        // different room with a different register and an arrival card of its own (#411).
        if (hallDue)
        {
            ex.HiveCantinaHallShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.CantinaHallLabel,
                UndergroundComplex.CantinaHallArtUrl,
                UndergroundComplex.CantinaHallCard);
            RendererInterop.PlayCue("reveal");
            // No save: nothing durable changed. The latch is excursion-scoped like every one of its
            // siblings (#725) — a captain who lands again is walking in for the first time again.
            return;   // one beat at a time
        }

        if (!ex.HiveCabinetShown && hall.CabinetAt(_avatarX, _avatarY) is not null)
        {
            ex.HiveCabinetShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.CabinetLabel,
                UndergroundComplex.CabinetArtUrl,
                UndergroundComplex.CabinetCard);
            // FileNote and NOT ShowAndFile: the card is up, and a pulse under an open card's backdrop is
            // the exact bug #680 was filed on. The book still remembers.
            FileNote(UndergroundComplex.CabinetNote, UndergroundComplex.CabinetGlyph);
            RendererInterop.PlayCue("reveal");
            // …and THIS one saves, because the field book is durable and the note is now in it.
            RequestVaultSave();
        }
    }

    // ── #709 · STOPPING AT SOMEBODY'S TABLE ──────────────────────────────────────────────────────────────
    //
    // Owner: "we should have people in the bar... we have cover story."
    //
    // The Hive's first conversation, and deliberately the smallest one the game has. A regular gives you ONE
    // breath of their working day and nothing else: no menu, no trade, no round to buy, no contact seam. That
    // restraint is the feature — the upper canteen is where a captain is UNREMARKABLE, and a room that starts
    // offering things is a room that is paying attention to you.
    //
    // NO RISK HERE, ON PURPOSE. #618 rules that talking is what blows a cover — but that is the GUARDS on the
    // bottom floors, who are not built yet, and whose whole point is that probing them probes back. Band 0 is
    // the floor whose own sign says NO PASS REQUIRED. Wiring an exposure cost into this conversation now would
    // pre-empt a ruling that has not been made and would make the one safe room in the building unsafe.
    //
    // Filed once per person per excursion, pulsed every time after — the same law the amenity write-up above
    // uses, and for the same reason: leaning on a table eleven times is not eleven findings, and a console
    // that goes silent reads as broken (#212).
    private void HiveRegularInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveRegular } spot)
        {
            return;
        }

        // WHICH PERSON COMES FROM CORE, matched by position — the same discipline HiveAmenityInteract uses
        // one method up. Reading the line off the console's own label would have been closer to hand and
        // would have been a second copy of the cast, going quietly wrong the day somebody edits one of them.
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        int seat = 0;
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            foreach (CanteenRegulars.Seated who in
                CanteenRegulars.Sitting(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
            {
                if (Math.Abs(who.X - spot.X) < 0.5 && Math.Abs(who.Y - spot.Y) < 0.5)
                {
                    if (ex.HiveRegularsHeard.Add(HiveInterior.RoomKey(ex.Floor, 900 + seat)))
                    {
                        ShowAndFile(who.Line, CanteenRegulars.Glyph);
                        RequestVaultSave();
                    }
                    else
                    {
                        ShowPulseMessage(who.Plate);
                    }
                    return;
                }
                seat++;
            }
        }
    }

    // ── #709 · READING THE BOARD ─────────────────────────────────────────────────────────────────────────
    //
    // Owner: "let's add a bulletin board to the bar" · "maybe spot the person notifying in the bar."
    //
    // ONE NOTICE PER PRESS, in the order they are pinned, then round again. Deliberate: a board that dumped
    // four notices into one card would be read once and never looked at twice, and the whole point of this
    // object is that a captain comes BACK to it after meeting the people. The pump notice means nothing until
    // you have heard the fitter, and everything afterwards.
    //
    // NOTHING HERE READS Notice.Pairs. Which regular pinned which notice is the player's to make or to miss,
    // and a client that hinted would spend the only thing the board has.
    private void HiveBoardInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveBoard })
        {
            return;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            var pinned = CanteenBoard.Pinned(ex.Stop.Body.Id, ex.Floor, a);
            if (pinned.Count == 0)
            {
                continue;
            }

            CanteenBoard.Notice n = pinned[ex.HiveBoardNext % pinned.Count];
            ex.HiveBoardNext++;
            ShowAndFile($"{n.Head} — {n.Body}", CanteenBoard.Glyph);
            RequestVaultSave();   // the field note is a possession too (#587)
            return;
        }
    }

    /// <summary>#608 · What a rack — either rack — says when it is asked how it is doing. One reading of one
    /// machine, so the shed on the regolith and the refuge eleven floors down can never describe the same
    /// state in two different ways.</summary>
    private static string RackGaugeLine(SurfaceExcursion ex, double held) =>
        ex.AirSeconds >= SuitAir.TankSeconds * SurfaceShelter.FillToFraction
            ? SurfaceShelter.PumpDoneLine
            : held > SurfaceShelter.ReservoirSeconds * 0.1
                ? SurfaceShelter.PumpingLine
                : SurfaceShelter.TrickleLine;

    /// <summary>#573 · Raise the tank-is-low card, once per captain ever. Returns true when it went up, so
    /// the caller keeps its pulse line for every later trip.</summary>
    private bool ShowAirCardOnce()
    {
        if (_airCardSeen)
        {
            return false;
        }
        _airCardSeen = true;
        _airCardOpen = true;
        RequestVaultSave();
        StateHasChanged();
        return true;
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
        // #585 · UNDERGROUND. A floor of the Hive is laid inside the SURFACE'S OWN envelope, so the whole
        // facility costs no new coordinate space - the owner's insight, and the reason "down" beat "wider".
        // Routed here, the same way a derelict is, so nothing else in the excursion has to know.
        if (ex.Floor < 0)
        {
            // #709 · Freeze which shift the canteen is on before the room is drawn, and hand the deck that
            // number rather than a clock. Everything afterwards — the [E] press, a rebuild after searching a
            // room — reads the same frozen watch, so the people drawn at the tables stay the people the game
            // answers about.
            // #751 · …or the one a tester pinned with ?watch=N. Applied HERE, at the one place the watch is
            // ever frozen, so the cheat cannot become a second answer to "which shift is this".
            ex.CanteenWatch = _watchCheat ?? PatronRota.WatchIndex(SimTime);
            _deckPlan = HiveInterior.FloorDeck(
                ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField(),
                3 + ReeverEngineCeiling + MaxCollectors, FillSurfaceDroids, ex.HiveRoomsEmptied,
                ex.CanteenWatch);
            // #411 · the head office's two floors with a beat on them get one console apiece, APPENDED the
            // way the hidden door and the outpost hut are — so the Hive's generator, and the A* audit that
            // walks every floor of it, are untouched.
            ComposeHeadOfficeFloor(ex);
            ComposeWhatYouLeft(ex);
            return;
        }

        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _) && _wreck is { } aboard)
        {
            _deckPlan = WreckInterior.WreckDeck(
                aboard, _wreckExamined, _wreckSalvaged, SurfaceDroidCount, FillSurfaceDroids,
                HeldDoors(), BlockedDoors(), _archiveAboard, _archivePurged);
            ComposeWhatYouLeft(ex);
            return;
        }

        _deckPlan = MoonSurface.SurfaceDeck(
            ex.Stop.Body.Id, ex.Stop.Body.Name, OwnCachePositionsAt(ex.Stop.Body.Id),
            SurfaceDroidCount, FillSurfaceDroids,
            siteSalt: ex.Site.LayoutSalt, siteName: ex.Site.Name, // #320: the picked site seeds the ground + names the header
            // #586: which visit-window the monolith's foot is showing. Bucketed off sim time so it holds
            // still for a whole excursion (a captain who comes back the same afternoon finds what they left —
            // his object-persistence law) and has moved on by the time it is worth walking out there again.
            monolithEpoch: Monolith.EpochAt(SimTime),
            // #585: whether this ground carries a clandestine site is ALREADY decided (ResolveSecretLab, which
            // honours ?secretlab=1). The renderer is told; it never rolls again.
            hasSecretSite: ex.Lab is { HasLab: true });

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
        ComposeWhatYouLeft(ex);      // #698: and whatever the captain themselves put down on this ground
    }

    /// <summary>
    /// #698 · THE MARK FOR WHAT YOU PUT DOWN. Owner, on B12 of the clinic: <i>"I dropped 3 files on somebody
    /// here but there was nothing marked onto the map?"</i>
    ///
    /// <para>Called on EVERY branch of the rebuild — the Hive's floors, a derelict's steel and the open
    /// regolith — because a captain can set a thing down on any of them, and a marker that only worked on
    /// the deck somebody happened to test is #691's flagged call shipped a second time.</para>
    ///
    /// <para>One mark per SPOT, from <see cref="LeftBehind.SpotsOn"/>, appended the way the hidden door and
    /// the outpost hut are — so nothing about the three generators changes, and the A* audits that walk them
    /// are untouched. It carries no walls, so it cannot block: the owner asked for scenery, and a mark you
    /// can be pinned against is worse than no mark at all.</para>
    /// </summary>
    private void ComposeWhatYouLeft(SurfaceExcursion ex) =>
        _deckPlan.AppendRegion(LeftMarks.Region(ex.Ground, ex.Floor));

    /// <summary>
    /// #681 · THE ONE DOOR THE CAPTAIN IS EVER PUT THROUGH. Owner, on a boot that pinned him inside a wall of
    /// the deep site's compound: <i>"The second url put me into the wall... I cannot move."</i> — and, while
    /// stuck there: <i>"Maybe some code to move the character either side instead of spawning it so it cannot
    /// move?"</i>
    ///
    /// <para>Every place the sim <b>places</b> the captain rather than letting them walk — the landing, the
    /// car coming back up, the tube mouth, the hull's airlock — goes through here. The deck is built first
    /// (the walls have to exist before anything can ask about them), then the chosen square is handed to
    /// <see cref="SpawnNudge"/>, and if it is inside collision the captain is walked out to the nearest square
    /// that is not.</para>
    ///
    /// <para><b>And it says so.</b> A rescue that happened quietly is a bug that shipped quietly. The line
    /// goes to the pulse AND to the excursion log, so a nudged spawn shows up in play and in a report — which
    /// is the only thing that keeps this from becoming the place generator bugs go to be forgotten. The CI
    /// ladder (<c>TheLandingPutsYouSomewhereYouCanWALKTests</c>) asserts the square BEFORE this runs, for
    /// exactly the same reason.</para>
    ///
    /// <para>Past <see cref="SpawnNudge.MaxDu"/> it refuses to help and shouts instead. A site with no
    /// standing room within six paces of its own spawn is built wrong, and papering that over would hand the
    /// player a working-looking excursion on ground that is not.</para>
    /// </summary>
    /// <param name="where">What the sim was trying to do, in the captain's own words — it is what the loud
    /// failure names, so "the shuttle sets you down at the lift head" beats "spawn".</param>
    private void StandCaptainAt(double x, double y, string where)
    {
        (_avatarX, _avatarY) = (x, y);
        RebuildSurfaceDeck();

        SpawnNudge.Result spot = SpawnNudge.Clear(x, y, DeckPlan.AvatarRadius, _deckPlan.CollisionField);
        if (spot.Failed)
        {
            ShowAndFile(SpawnNudge.BrokenGroundLine(where), "⚠");
            return;
        }
        if (!spot.Moved)
        {
            return;
        }

        (_avatarX, _avatarY) = (spot.X, spot.Y);
        ShowAndFile(SpawnNudge.ClearedLine(spot.MovedDu), "🧤");
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

        // ── #723 · THE SHOVEL IS SOMETHING THE GROUND HAS, NOT SOMETHING THE KEY DOES ──
        //
        // Found by playing, on B1 of a Hive: [E] with empty hands in a pressurised spine corridor 150 m
        // down ran the beach-comber probe, left the orange dug square on the rockcrete, and at the canteen's
        // west face said "the shovel rings off bedrock a foot down — too hard to dig here. Try another
        // square." Both halves lie. There is no bedrock under a floor somebody invoiced — and "try another
        // square" is an INVITATION: it tells the captain that some square down here does dig, when nothing
        // can ever be buried on any square of any corridor of the building.
        //
        // The owner's answer was to gate the verb on the GROUND rather than on the keypress, so this is the
        // FIRST question the bare-ground [E] asks — ahead of the settling window too, because a captain who
        // dug on the regolith and then took the lift down would otherwise be told the earth beneath a
        // rockcrete corridor needed a breath to settle. Indoors the shovel is not in the candidate list at
        // all and the press falls through to the same honest nothing [E] gives on any other deck; the
        // too-hard line below still belongs to genuine surface squares, which is where bedrock genuinely is.
        if (!MoonSurface.ShovelWorksOnThisFloor(ex.Floor))
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
        if (!MoonSurface.IsDiggableGround(_avatarX, _avatarY, ex.Floor))
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

        // Only what is IN THE CHEST leaves the books. The chest is a snapshot taken at the shuttle door
        // (ShuttleExcursion.Pack); the hold keeps living all the way down — a cache dug back up on this
        // same ground, a beach-comber scrap recovered after a panic drop. Clearing the whole hold here
        // therefore ATE those units: the map card and the "off the books" line name only the snapshot, so
        // they were neither buried nor aboard. Coin was always deducted honestly (the pending amount, no
        // more); this is cargo's half of the same law. ShuttleExcursion.HoldAfterBurying owns the rule.
        var left = ShuttleExcursion.HoldAfterBurying(_cargoByClass, ex.PendingCargo);
        _cargoByClass.Clear();
        foreach (KeyValuePair<string, int> line in left)
        {
            _cargoByClass[line.Key] = line.Value;
        }
        RecomputeCargoTotals();

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
        if (!_kaamos.Has("cold-pod") && KaamosPodHere(ex.Stop.Body.Id, squareX, squareY))
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
        double baseY = Math.Min(_avatarY - 4, MoonSurface.AnchorY + 10);
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

    /// <summary>Which body <c>?land=&lt;bodyId&gt;</c> asked for, or null for "the first one in reach". Matched on
    /// id OR name, case-insensitively, because a playtester types what they see on the map.</summary>
    private string? _landBodyCheat;

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

        // #585 / #633 · A NAMED BODY WINS THE TOSS OUTRIGHT, so any ground can be opened and LOOKED at from
        // one URL. Two spellings reach this line — `?body=<id>` (#585) and `?land=<id|name>` (#464) — because
        // the branches grew one each; BOTH still parse, and they resolve through this ONE lookup rather than
        // two, which is the only way they can be guaranteed to land in the same place.
        //
        // The named body must still be on the board — the cheat may never reach somewhere the player could
        // not — and when it is not, it REFUSES and says so. A cheat that silently lands you somewhere else is
        // worse than one that refuses, because you playtest the wrong scene and trust the result.
        if ((_landBodyCheat ?? _forcedLandingBodyId) is { } wanted)
        {
            ShuttleStop? named = board.FirstOrDefault(
                s => s.IsLandable
                     && (string.Equals(s.Body.Id, wanted, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(s.Body.Name, wanted, StringComparison.OrdinalIgnoreCase)));

            if (named is null)
            {
                string inReach = string.Join(", ", board.Where(s => s.IsLandable).Select(s => s.Body.Id));
                ShowPulseMessage($"🧪 DEV ?land={wanted}: not landable from this berth. In reach: " +
                                 (inReach.Length > 0 ? inReach : "nothing"));
                _landCheat = false;
                return;
            }

            await RideTheShuttleDownForCheatAsync(named);
            return;
        }

        ShuttleStop? target =
            board.FirstOrDefault(s => s.IsLandable && Derelict.TryParseWreckId(s.Body.Id, out _))
            ?? board.FirstOrDefault(s => s.IsLandable);
        if (target is null)
        {
            ShowPulseMessage("🧪 DEV ?land=1: nothing landable in shuttle reach from this berth.");
            return;
        }

        await RideTheShuttleDownForCheatAsync(target);
    }

    /// <summary>
    /// The descent both <c>?land=1</c> and <c>?land=&lt;bodyId&gt;</c> ride. One method on purpose: two copies of
    /// "land, then put the boots somewhere sensible" would drift, and the drift would be invisible until a
    /// playtester landed by name and found themselves standing in vacuum.
    /// </summary>
    private async Task RideTheShuttleDownForCheatAsync(ShuttleStop target)
    {
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
            StandCaptainAt(WreckInterior.SpawnX, WreckInterior.SpawnY,
                "the boarding tube lets you out into her airlock");
            return;
        }

        if (_surface is not { } landedOn)
        {
            return;
        }

        // #585 · ?secretlab=1&land=1 PUTS YOU AT THE SHAFT. Owner, after an evening of walking a 310 x 260
        // field to reach the one thing being tested: "instruct to put the debug cheat start next to the lab
        // so that it can be really tested without playing to find it" — "I mean next to the elevator shaft".
        //
        // The hunt is the GAME (the clue, the wash, the detector gradient), and it is exactly what must not
        // stand between a developer and the feature under test. Every one of the open Hive issues — the
        // sealed rooms, the visualiser, the card, the tracker, the unlisted band — needs the captain standing
        // at the lift head within seconds, repeatedly.
        //
        // Only under the secret-lab cheat: an ordinary landing still drops you on the open regolith, so this
        // can never quietly become how the game plays.
        if (_secretLabForceBodyId == landedOn.Stop.Body.Id && landedOn.Lab is { HasLab: true })
        {
            // A pace outside the shed's door, facing it — not inside, so the walk in is still walked and the
            // door, the label and the console are all exercised the way a player meets them.
            //
            // #681 · ASKED OF THE SHED. This used to take the head spot and subtract 7.5 from its Y, which was
            // a pace clear of the door back when the head was a hand-typed 10 x 8 box — and was the far wall
            // the moment #606 made it an ordinary hut with a seeded angle. The owner landed inside it:
            // "The second url put me into the wall... I cannot move." One building, one answer, and the
            // landing reads it the same way the returning car reads CarFloor.
            (double hx, double hy) = MoonSurface.LiftHead(
                landedOn.Stop.Body.Id, landedOn.Site.LayoutSalt, MoonSurface.ExpeditionField()).DoorStep;

            ShowPulseMessage(_foundCheat
                ? "🧪 DEV ?found=1: set down at the lift head, with the wallet already full. Ride down."
                : "🧪 DEV ?secretlab=1: set down at the lift head. The shed is in front of you.");
            StandCaptainAt(hx, hy, "the shuttle sets you down on the lift head's doorstep");

            // ── #677 · …AND THE PAPERWORK, because the halls are behind two gates and one of them is the
            // rarest object in the game. Every band this site has gets its card, minted through the SAME
            // AuthorityCard the rooms mint, put in the SAME satchel the pockets hold — so the lift panel,
            // the gate, the refusal ladder and the wallet fan all behave exactly as they do for a captain
            // who earned them. A cheat that seeded a private "you may descend" flag would be testing a
            // second mechanism that does not ship.
            if (_foundCheat)
            {
                // Every band index the arithmetic admits, and NOT "until one is missing": there is a whole
                // band with nothing in it between the unlisted floors and the halls, so a loop that stopped
                // at the first absence would hand out every card except the only one this cheat exists for.
                int last = UndergroundComplex.BandOf(UndergroundComplex.DeepestPossibleFloor);
                for (int band = 0; band <= last; band++)
                {
                    if (!UndergroundComplex.SiteHasBand(landedOn.Stop.Body.Id, band))
                    {
                        continue;
                    }
                    var card = new UndergroundComplex.AuthorityCard(landedOn.Stop.Body.Id, band);
                    _satchel = [.. Core.Satchel.Add(
                        _satchel, new Core.Satchel.Item(Core.Satchel.Kind.Authority, card.Id))];
                }
            }

            // ── #693 · …OR ONE CARD, WHICH IS THE ROW AND THE BEAT AND THE REFUSAL ────────────────────────
            //
            // #692's own honest note: "reaching the row needs an authority card in the wallet and no dev
            // cheat mints one." ?found=1 above hands over every authority a site issued, but it also parks a
            // particular rock, so the carded lift row and the gate beat could not be seen on an ordinary
            // site at all. Same mint, same satchel, same gate — one card instead of the set.
            //
            // The band the site does not have is deliberately NOT invented: the pocket stays empty and the
            // line says which bands there were, because a cheat that silently gives you nothing is a tester
            // playtesting the wrong scene without knowing it.
            if (_cardCheat is { } asked)
            {
                string cardBody = landedOn.Stop.Body.Id;
                int standingOn = _startingFloorCheat ?? 0;
                var wanted = new List<int>();
                int deepestBand = UndergroundComplex.BandOf(UndergroundComplex.DeepestPossibleFloor);

                if (asked == "all")
                {
                    for (int band = 0; band <= deepestBand; band++)
                    {
                        wanted.Add(band);
                    }
                }
                else if (asked == "next")
                {
                    // ASKED OF THE BUILDING, exactly as the panel asks it: the shaft that EXISTS below where
                    // you are standing, stepping over the band of nothing under the unlisted floors (#677).
                    // Working it out here as "band + 1" is §13.15's second cause, and it has already been
                    // the cause of something three times on this ground.
                    if (UndergroundComplex.NextShaftBelow(cardBody, standingOn) is { } next)
                    {
                        wanted.Add(next);
                    }
                }
                else if (int.TryParse(asked, System.Globalization.NumberStyles.Integer,
                             System.Globalization.CultureInfo.InvariantCulture, out int askedBand))
                {
                    wanted.Add(askedBand);
                }

                var minted = new List<int>();
                foreach (int band in wanted)
                {
                    if (!UndergroundComplex.SiteHasBand(cardBody, band))
                    {
                        continue;
                    }
                    var card = new UndergroundComplex.AuthorityCard(cardBody, band);
                    _satchel = [.. Core.Satchel.Add(
                        _satchel, new Core.Satchel.Item(Core.Satchel.Kind.Authority, card.Id))];
                    minted.Add(band);
                }

                var has = new List<string>();
                for (int band = 0; band <= deepestBand; band++)
                {
                    if (UndergroundComplex.SiteHasBand(cardBody, band))
                    {
                        has.Add(band.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }

                ShowPulseMessage(minted.Count > 0
                    ? $"🧪 DEV ?card={asked}: band {string.Join(", ", minted)} authority in the wallet — " +
                      "🎒 I to read it, then ride and watch the row."
                    : $"🧪 DEV ?card={asked}: this site has no such band, so nothing was minted. It has " +
                      $"band {string.Join(", ", has)}.");
            }

            // ...and ?floor=N goes the rest of the way down, because half the open work on this feature is
            // about what a FLOOR looks like rather than about finding the way in.
            if (_startingFloorCheat is { } askedFor)
            {
                // #592/#677 · ASKED OF THE BUILDING, not worked out here. This used to clamp to the true
                // bottom and then snap into the unlisted band's shaft head — correct arithmetic for a
                // building with ONE gap in it, and a captain set down in solid rock the day there were two
                // (the band of nothing under the unlisted band, #677). A caller doing its own geometry about
                // a building it does not own is §13.15's second cause, and this is the third time it has
                // been the cause of something.
                string cheatBody = landedOn.Stop.Body.Id;
                RideTheLiftTo(landedOn, UndergroundComplex.NearestFloorTo(cheatBody, askedFor));

                // #746 · …and ?tablescene=1 goes the last leg too, because the scene under test is a
                // conversation at a table and the lift head is the other end of the floor from it.
                StandInTheCanteenIfAsked(landedOn);

                // #756 · …and ?counter=1 goes the same last leg to the other fixture in that room.
                StandAtTheCounterIfAsked(landedOn);
            }
            return;
        }

        // #681 · Asked of the field, not retyped. "SpawnX, LandingBandY − 12" was a spot nothing had ever
        // reserved, so the generator built a hut through it on two sites and the drop came down inside a wall
        // — the ordinary landing's version of the same bug the lift head had. SurfaceLayout.LandingApproach
        // is now the ONE answer: this reads it to know where to drop, and the claim ledger reads it to know
        // what to keep clear.
        (double dropX, double dropY, _) = SurfaceLayout.LandingApproach(MoonSurface.ExpeditionField());
        StandCaptainAt(dropX, dropY, "the shuttle sets you down on the open regolith below the pad");
    }

    // #458: how many Old Ones /map?reevers=N asks for on the first landing. 0 = the cheat is off. They are
    // roused in the DEEP and come to you (#461) — never set down on the landing pad, which read as the Old
    // Ones somehow knowing where the shuttle would touch down.
    private int _reeverAmbushCheat;

    /// <summary>#756 QA · <c>?counter=1</c> — the whole route to a counter that takes orders, booted. Set in
    /// Map.Sim's cheat parse; read here, where the last leg is walked.</summary>
    private bool _counterCheat;

    /// <summary>#774 QA · <c>?kit=1</c> — the field dossier assembles on the FIRST piece of somebody's kit,
    /// and with every saying it can carry. Set in Map.Sim's cheat parse; read in
    /// <see cref="AssembleSomebody"/>, which is the one place both gates are asked about.</summary>
    private bool _kitCheat;

    /// <summary>#756 QA · Stand the captain AT THE COUNTER when <c>?counter=1</c> asked for it.
    ///
    /// <para>The amenity's own published spot, which is the service side of the counter and the very square
    /// the console dot is drawn on — so this walks the captain to the fixture rather than to a coordinate
    /// somebody measured off a picture of it. Through <c>StandCaptainAt</c>, so the pad-crew net (#681) has
    /// its say if the hall ever carves a table onto that square.</para></summary>
    private void StandAtTheCounterIfAsked(SurfaceExcursion ex)
    {
        if (!_counterCheat || ex.Floor >= 0)
        {
            return;
        }

        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
        {
            if (Core.Interior.CounterService.For(ex.Stop.Body.Id, a.Use) is null)
            {
                continue;
            }

            ShowPulseMessage(
                "🧪 DEV ?counter=1: THE COUNTER, in reach. Press E to open the card and order something.");
            StandCaptainAt(a.X, a.Y, "you step up to the counter");
            return;
        }
    }

    // #649: /map?watchers=1 opens the monolith's attentive window and shortens the dwell to a couple of
    // seconds. It does NOT change what happens — the beat, the variant roll and the (zero) cost are the
    // ones a captain gets — because a cheat that shows you a different scene is worse than no cheat at all.
    private bool _watchersCheat;

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
        if (_groundLessonOpen || _groundGrewOpen || _tubeRearmOpen || _airCardOpen)
        {
            _surface.LandedAtMs += dtRealSeconds * 1000.0;
            return;
        }

        StepSuitAir(dtRealSeconds);     // #564: the tank, the line, and the walk home
        StepTubeRearm(dtRealSeconds);   // #562: the ship feeds your sentries while you stand in her tube
        StepDigChannel(dtRealSeconds);
        // #696 · The darkroom hold, AFTER the tank. Order matters and it is this way round on purpose: the
        // air for this tick is charged on whatever ground the captain is standing on before the hold is
        // allowed to notice the tick at all, so a hold can never finish a frame "for free" and the alarm
        // that breaks a hold has already fired by the time the bar is asked to fill.
        StepProcessing(dtRealSeconds);
        AdvanceVacuumClocks(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds)); // #488: the vacuum soak
        AdvancePump(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds));         // #488: the thrifty road
        ServeStandingPumpOrder();                                                   // #488: …and the corridor last
        AdvanceScuttleClock(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds)); // #488: the overload
        AdvanceNests(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds));        // #488: the nest is a source
        AdvanceFire(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds));         // #524: and the fire eats
        AdvanceVacuumExposure(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds)); // #488: vacuum is ground
        CheckVentPayoffUnderfoot();   // #488: the room shows what the vacuum left — when you walk into it
        CheckStaffMessUnderfoot();    // #725: …and the one room down here that is a find rather than a route
        CheckCantinaHallUnderfoot();  // #751: the hall, and the doors along the back of it
        StepDoorChannel(dtRealSeconds); // #371 Phase 3: the forced-door progress bar
        StepSecretLabDoorChannel(dtRealSeconds); // #409: the hidden lab door's force channel
        StepSecretLabDetector();                 // #585: the needle climbs as you close on a named moon
        StepOutpostDoorChannel(dtRealSeconds);   // #563: the outpost hatch's force channel
        StepDrillChannel(dtRealSeconds); // #394: the drilling — sinking the charge into the rock
        StepSentries(dtRealSeconds);
        // #585 · NOTHING SHAMBLES DOWN HERE. Owner, stepping out of the car: "I don't think there should be
        // reevers down here", then "now the reevers are on surface right, so they should not be visible here
        // on screen now?" — both correct, and the second is the sharper point: they are still up there, and
        // a captain underground should neither see them nor hear them on the fan.
        //
        // Same law a derelict already runs under: the tide claws up out of REGOLITH, and a poured, sealed,
        // still-powered facility is not regolith. Clearing the live pack means a chase that followed you
        // across the field is simply over — and the pack you meet coming back up is a fresh one, which is a
        // better scene than one that rode down in the lift with you.
        if (_surface is { Floor: < 0 })
        {
            _reevers.Clear();
        }
        StepReevers(dtRealSeconds);
        StepCollectors(dtRealSeconds); // #583: the repo boat, and the people who got out of it
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
    //
    // #637 · It used to read the MOON's axis unconditionally — "are you above the regolith's top rim at
    // y = −20" — and a derelict's whole deck runs −9..+9, so aboard a wreck the early return fired at every
    // point of every hull and the link never degraded anywhere. AwayTeamSide is the one place that knows
    // which door you are on the far side of, and now which axis measures the walk away from it.
    private double CommsOnsetBias() =>
        AwayTeamSide.CommsOnsetBias(OnWreck, _avatarX, _avatarY, DeckPlan.AvatarRadius);

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
        double detection = FanReach();   // #591: shorter with every floor down
        // #583 / #538 · The chirp counts everything on its feet down here — the pack, the sweep team, and
        // the repo crew. The holster device does not know or care whose boots they are, and a boat crew
        // walking up on you is exactly the thing it exists to make you look at. ONE accessor, so the ear and
        // the hull can never disagree about who is walking about in it.
        var entities = EverythingThatMoves();
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
        // #591 · A SHELTER STEADIES YOU. Owner: "we should restore sanity when we get to a shelter also."
        //
        // No new machinery was needed and that is the tell that it is the right rule: nerve already comes
        // back one beat at a time whenever the captain is SAFE (NervePips.Cause.Airlock), and safe has always
        // meant "aboard, or in her tube". A pressurised drum with a door nothing outside can work is safe by
        // exactly the same definition — it is the reason the air stops there too (#573). Saying so here is
        // the whole change.
        //
        // It also completes the building's argument. It gives you air, it reloads you, it keeps the Old Ones
        // at the threshold — and until now you sat in it watching the one gauge that measures whether you can
        // keep doing this go on falling.
        // #585: a pressurised floor of the Hive steadies you for the same reason a shelter does - it is warm,
        // it is lit, and nothing outside can work its doors. A DEAD floor does not, which is most of them.
        bool inShelter = onExcursion && _surface is { } shelterEx
            && (ShelterUnderfoot(shelterEx) >= 0
                || (shelterEx.Floor < 0 && UndergroundComplex.HoldsPressure(shelterEx.Stop.Body.Id, shelterEx.Floor)));
        // #637 · AND A DERELICT IS EXPOSED GROUND TOO. This asked the moon's question — "are you above the
        // regolith's top rim at y = −20" — and a wreck's whole deck runs −9..+9, so aboard a hull it was
        // always FALSE: the ambient pressure the entire dread economy runs on never applied inside a ship,
        // `?wreck=infested` included. A captain could walk the spine of a haunted hull, in the dark, in
        // vacuum, and the gauge scored it as standing in the shuttle bay. The damage half of that same
        // constant was fixed in #574 and the air half in #621; this is the sanity half, one call site over.
        //
        // The name changed with the meaning: it is not the regolith, it is being on the far side of your own
        // door — whichever door this world has (AwayTeamSide).
        bool awayFromSafety = onExcursion
            && !AwayTeamSide.BackAtTheShuttle(OnWreck, _avatarX, _avatarY, DeckPlan.AvatarRadius)
            && !inShelter;

        // #380 item 2: the band this frame opened on — so once, per excursion, we can speak the FIRST slide
        // down a rung (naming the cause and the remedy the bare gauge never did). Recovery only ever raises
        // the nerve, so a fall can arise solely from the regolith's toll below.
        NerveModel.NerveBand bandBefore = NerveModel.BandFor(_nerve);

        // #379 · the per-spell sighting tally still lives here (the tracker's own hearing decides what counts
        // as a fresh contact); #480 prices the result in whole pips instead of a shaped float.
        int heardMovers = 0;
        if (awayFromSafety && _surface is not null)
        {
            // #446: the tracker's fan still HEARS to its full detection range — that far, faint blip is the
            // whole point of the instrument. But a contact only FRIGHTENS you inside the dread range.
            double detection = Math.Min(FanReach(), NerveModel.DreadRangeDeckUnits);
            var ents = _reevers.Select(r => new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy))
                .Concat(_collectors.Select(c => new MotionTracker.Entity(c.X, c.Y, c.Vx, c.Vy)));
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
            OnRegolith: awayFromSafety,
            // There is no monolith aboard a dead ship, and now that a wreck can BE exposed ground the guard
            // has to be said rather than left to arithmetic (#637's whole list is things satisfied by
            // accident). SeesMonolith() now answers the question properly on its own — it asks
            // Monolith.StandsOn, the same predicate the renderer builds the slab from, and a wreck id is
            // not the canon moon — so !OnWreck is belt to that braces rather than the only thing holding it.
            SeesMonolith: !OnWreck && awayFromSafety && SeesMonolith(),
            // #446 (owner, live 2026-07-26: "The reevers should not lower sanity unless they get REALLY
            // close"). ChaseActive used to be the bare `_reevers.Count > 0` — a pack EXISTING anywhere on
            // the field, so one Old One drifting on the far rim taxed the captain at the same flat rate as
            // one at their shoulder, and the gauge bottomed out before anything ever reached them. Now we
            // hand Core the distance to the nearest hunter and it prices the dread off that; the moving
            // count is likewise only the ones near enough to matter, so a far-off tide is atmosphere.
            Stressors: awayFromSafety
                ? new NerveModel.Stressors(
                    CountMovingReeversWithin(NerveModel.DreadRangeDeckUnits),
                    _reevers.Count > 0,
                    _surface!.Channeling,
                    IsCornered(),
                    NearestReeverRange())
                : default,
            FreshSightings: awayFromSafety && firstFrightOfSpell ? freshSightings : 0,
            Touched: _touchedThisFrame,
            DtSeconds: dtRealSeconds,
            // #480 · fear tracks MORTAL DANGER: below a couple of blows left, every further hand costs its
            // pip again instead of being absorbed by the once-per-encounter latch.
            HealthPipsLeft: _surface is { } hurt ? CaptainCondition.MaxHits - hurt.HitsTaken : int.MaxValue,
            // THE DWELL. The one sustained pressure that is not a regolith pressure: a derelict's interior
            // reads as "aboard" to every other rule in NervePips, so standing beside the archive node would
            // otherwise be scored SAFE and hand pips BACK while the ledger said it was taking them.
            InArchiveField: InArchiveField);

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

        // #649 · AND THE ARRIVAL, which is a different beat from the first sight and lands much closer in.
        //
        // Monolith.ApproachLine has existed since #586 with NO CALLER — designed and never consumed, which
        // QAHandoff-StoryTelling.md §1 names as its own failure class. It is the one beat of pure SCALE the
        // crude grid cannot draw by itself, and the scale pass is exactly where it belongs: the nerve hit
        // fires at three fifths of the thing's height away, while it is still a shape; this fires when you
        // cross onto the swept ground and it fills the view. Two beats, two distances, both derived from how
        // big the thing actually is.
        if (onExcursion && _surface is { MonolithApproachAnnounced: false } walk
            && Monolith.StandsOn(walk.Stop.Body.Id, walk.Site.LayoutSalt)
            && DistanceToAnchorSquared() <= Monolith.ApproachRangeDu * Monolith.ApproachRangeDu)
        {
            walk.MonolithApproachAnnounced = true;
            ShowAndFile(Monolith.ApproachLine, "▮");
        }

        // #649 · AND THEN, RARELY, THE GROUND DOES SOMETHING. See MonolithWatch for the register and the
        // three gates; this is only the clock and the telling. Owner's reference: Babylon 5, Sheridan and
        // the giants on the playground — "background puppeteers watching if their kids perform in the school
        // play." Parental, not predatory: it costs nothing, it never repeats inside an excursion, and the
        // world never remarks on it afterwards.
        StepMonolithWatch(dtRealSeconds);

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

    // #586 · IN SIGHT OF THE MONOLITH — and only where the monolith actually IS.
    //
    // This used to be pure distance to MoonSurface.AnchorX/Y, which is the DEEP ANCHOR of every ground
    // there is: every seeded site puts its own fixture there (Luna's mass-driver muzzle, a plinth
    // elsewhere), so walking up to any of them fired the once-in-a-life Lovecraftian hit — 24 nerve, the
    // line "👁 The monolith resolves out of the dark", and the FirstMonolith selfie against the monolith
    // plate — over a broken launch machine. And _monolithSeen is kept FOR LIFE, so the captain who did
    // that could never be shown the real slab's beat again. Constant governing the wrong thing, and the
    // sentence disagreeing with the sim, in one line.
    //
    // Monolith.StandsOn is the same predicate the renderer builds the slab's card from, so the beat cannot
    // drift from the object again.
    /// <summary>#649 · THE DWELL, AND THE ONE STRANGE THING.
    ///
    /// <para>Three gates, all of them Core's (<see cref="MonolithWatch"/>): the PLACE (the monolith's own
    /// ground, and inside its sight), the WINDOW (about one visit-window in three is attentive, on the same
    /// slow clock the foot-offerings use, so it holds still for a whole excursion), and the DWELL — you have
    /// to STAY. Nothing is watching to see you arrive. It is watching to see whether you stand there.</para>
    ///
    /// <para>Walking out of sight resets the clock, which is the difference between standing at a thing and
    /// passing it. Once per excursion at most, and the beat costs the captain nothing —
    /// <see cref="MonolithWatch.NerveCost"/> carries the reasoning and is the one number to change.</para>
    ///
    /// <para>Deliberately NOT a story card or a plate. The picture idiom (#528) is the right instrument for
    /// almost everything and the wrong one here: a frame around a thing says THIS IS A THING, and the whole
    /// ruling is that anything happening near this stone stays deniable.</para></summary>
    private void StepMonolithWatch(double dtRealSeconds)
    {
        if (_surface is not { } ex || !MonolithWatch.CanHappenOn(ex.Stop.Body.Id, ex.Site.LayoutSalt))
        {
            return;
        }

        if (!SeesMonolith())
        {
            ex.MonolithDwellSeconds = 0;   // you walked away; standing at a thing is not passing it
            return;
        }

        ex.MonolithDwellSeconds += dtRealSeconds;

        // ?watchers=1 — the beat is rare BY DESIGN (one window in three, then forty seconds of standing
        // still), which makes it the exact shape of scene Map.Sim's own rule is about: "a scene nobody can
        // reach on demand is a scene that ships broken." The cheat opens the window and shortens the dwell;
        // it does not change what happens, so what a tester sees is what a captain sees.
        double dwell = _watchersCheat ? MonolithWatch.DwellSeconds * 0.05 : MonolithWatch.DwellSeconds;
        if (ex.MonolithWatchSpent || ex.MonolithDwellSeconds < dwell)
        {
            return;
        }

        long epoch = Monolith.EpochAt(SimTime);
        if (!_watchersCheat && !MonolithWatch.AttentiveIn(ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch))
        {
            ex.MonolithWatchSpent = true;   // this window is not one of them; do not keep asking
            return;
        }

        ex.MonolithWatchSpent = true;
        MonolithWatch.What what = MonolithWatch.Which(
            ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch, packOnTheField: _reevers.Count > 0);
        ShowAndFile(MonolithWatch.Line(what), MonolithWatch.Glyph);

        // NerveCost is 0.0 and the call is left in on purpose: the number is a feel call the owner may want
        // to make, and a call site that has to be re-found is a decision that quietly never gets made.
        if (MonolithWatch.NerveCost > 0)
        {
            ApplyNerveShock(MonolithWatch.NerveCost, "something out here was paying attention");
        }
    }

    /// <summary>How far the captain is from the deep anchor, squared. One expression, because the sight beat
    /// and the arrival beat must measure from the same point or they can disagree about where the thing
    /// is.</summary>
    private double DistanceToAnchorSquared()
    {
        double dx = _avatarX - MoonSurface.AnchorX;
        double dy = _avatarY - MoonSurface.AnchorY;
        return (dx * dx) + (dy * dy);
    }

    private bool SeesMonolith()
    {
        if (_surface is not { } ex || !Monolith.StandsOn(ex.Stop.Body.Id, ex.Site.LayoutSalt))
        {
            return false;
        }
        return DistanceToAnchorSquared() <= Monolith.SightRangeDu * Monolith.SightRangeDu;
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
        // #538 · WEAPONS TIGHT. While the order stands, nothing of the captain's fires — not a deployed
        // bot, not the tube gun that never runs dry. Skipping the volley entirely is the honest
        // implementation: no rounds leave, no magazines drain, and no noise is made, which is the point.
        if (!SentryBot.MayOpenFire(_weaponsTight))
        {
            return;
        }

        // #603 · And what does leave is what is IN them: a bot loaded with the lab round drops a queue in
        // one shot and one loaded with issue ball grinds them down.
        var loaded = live.Select(b => Core.Ammunition.ById(b.AmmoId)).ToList();
        SentryBot.Volley volley = SentryBot.Step(deployed, targets, SightBlockers(), loaded);

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
        // #603 · WHAT IS LOADED DECIDES WHAT IT WILL SHOOT AT. Owner: "some lab found exploding rounds
        // might be too dangerous to use to close by targets."
        //
        // A two-stage round arms after travel, so at arm's length the second charge goes off level with the
        // gun and whoever is standing beside it. The sentry simply will not take that shot — the interlock
        // idiom this ground already speaks (#462's airlock, #523's automatic, the vent readiness refusal).
        //
        // The consequence is the frightening part and it is entirely the captain's own doing: a gun loaded
        // with the wrong thing is SILENT with the pack on top of it, because of a choice made three rooms
        // ago. The override the owner asked for ("the gun complains but also gives override option to just
        // fire") belongs at the HUD, on a captain's word — not here, where it would fire itself.
        double minimum = Core.Ammunition.ById(bot.AmmoId).MinimumRangeDu;
        double minimumSq = minimum * minimum;

        double bestSq = SentryBot.RangeDeckUnits * SentryBot.RangeDeckUnits;
        (double, double)? best = null;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - bot.X, dy = r.Y - bot.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < minimumSq)
            {
                continue;   // inside the arming distance: it would take the gun with it
            }
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

    /// <summary>
    /// FILL A CARRIED SENTRY AT THE LOCK. Owner: <i>"Carrying the autogun to our shuttle air-lock should reload
    /// it ( might ve needed for big ship) 😎"</i>
    ///
    /// <para>The boat carries the belts; the bot carries only what you last gave it. So a drained sentry is a
    /// WALK rather than a write-off — a stroll on a small hull, and a real decision on the 4× hauler of #531
    /// with a pack somewhere behind you. Free of any other currency on purpose: the cost is time and exposure,
    /// the same shape as the pump's.</para>
    ///
    /// <para>Returns true when it actually did something, so the lock can say that instead of opening the
    /// destination list — pressing E again gets you the list, and nothing is taken away.</para>
    /// </summary>
    private bool TryFillCarriedSentryAtTheLock()
    {
        if (_surface is not { } ex)
        {
            return false;
        }

        SurfaceBot? carried = ex.Bots.FirstOrDefault(b => !b.Deployed);
        if (carried is null)
        {
            return false;   // nothing in the sling; the lock has its usual job to do
        }

        if (!SentryBot.NeedsFilling(carried.Rounds))
        {
            ShowPulseMessage(SentryBot.AlreadyFullLine(carried.Unit));
            return false;   // it said its piece, but the lock should still open
        }

        // #540 · A COLD BOAT ARMS NOBODY. Owner, on what makes the wait bite: "As the ammo count runs down and
        // reload place is warming up to allow use and its gun." The belts live aboard her, behind a hatch that is
        // dogged while she sleeps — so going dark takes away the resupply and the covering gun as well as the ride.
        if (!SilentRunning.HatchOpen(BoatState))
        {
            ShowPulseMessage(SilentRunning.ReloadNeedsHerAwakeLine(BoatSecondsLeft));
            RendererInterop.PlayCue("block");
            return true;    // handled: an empty sling and a shut boat is not a reason to offer a ride out
        }

        int was = carried.Rounds;
        carried.Rounds = SentryBot.MaxMagazine;
        ShowPulseMessage(SentryBot.FilledLine(carried.Unit, was));
        LogAutopilotEvent($"🤖 {carried.Unit} refilled at the lock ({SentryBot.Readout(was)} → " +
                          $"{SentryBot.Readout(SentryBot.MaxMagazine)}).");
        RendererInterop.PlayCue("board");
        RequestVaultSave();
        return true;
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

            // #585 · AND OUT OF THE SHELTERS. Owner, playing: "lol I saw one reever get into a shelter :-D",
            // then "3 reevers waiting in the shelter :-D". A doorway has to be a real gap or the captain
            // could not use it either, so geometry alone was always going to let a body through — but the
            // building's own arrival line promises "Nothing outside can work that door", and the whole
            // reason it exists is his ask for "rooms with doors we can hide behind while we reload our guns
            // safe from reevers". A refuge you can be followed into is just a smaller room to die in.
            //
            // Same fiction that already pens them off the shuttle: the door reads a SUIT. They may crowd the
            // threshold and wait there — which is its own good scene — and they may not come in.
            (nx, ny) = HoldOutsideShelters(nx, ny);

            // #585 · AND OUT OF ANYTHING ELSE THEY ENDED UP INSIDE. Owner, playing: "I think we landed a
            // building on top of two reevers here :-D".
            //
            // He did. The pack is spawned in regolith coordinates and the ground is BUILT around them — the
            // lift head, the outpost hut and the seeded structures all appear on a deck the Old Ones are
            // already standing on. Bump-and-slide keeps a body out of a wall it walks into; it has nothing to
            // say about a wall that arrives around a body already there, so they were sealed in, shuffling
            // inside somebody's masonry.
            //
            // So: if a contact is standing IN stone, walk it out along the shortest way. Cheap — the test is
            // one collision query that says "no" for every Old One on an ordinary frame.
            (nx, ny) = ExtricateFromStone(nx, ny, walls, reeverRadius);

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
        // #724 · A SHIVER LOOKS FOR NOTHING. The gait is Stagger both because this is an Old One (the
        // owner's ruling) and because a cosmetic mean-zero twitch that could funnel itself sideways into a
        // doorway would be a still body slowly walking off through the door it happened to be idling beside.
        (r.X, r.Y) = SurfaceCollision.Slide(
            r.AnchorX, r.AnchorY, jx, jy, radius, walls, SurfaceCollision.Gait.Stagger);
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
    // ── #583 · THE REPO BOAT COMES DOWN ────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-08-01: "but the heat should not target the ship when the player is not on it but only
    // target the captain... we could have some other shuttle land near ours on some sites ... that would be
    // the heat when we are on land or at a ship looting it" — and, settling it, "FBI does not arrest cars ...
    // they look for the driver".
    //
    // #580 stopped the wolves from catching an empty hull, which was right and left heat meaning nothing
    // during the part of the game the captain is actually in. This is the other half: the collectors come to
    // the person. A boat sets down between you and your ride, a crew gets out, and they walk. They cannot be
    // out-burned out here, only outwalked — and the only door that closes on them is the tube's.
    private void StepCollectors(double dtRealSeconds)
    {
        if (_surface is not { } ex || _busted is not null)
        {
            return;
        }

        double dt = Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);
        ex.SecondsOnTheGround += dt;

        if (!ex.CollectorsComing)
        {
            return;
        }

        if (!ex.CollectorsLanded)
        {
            if (ex.SecondsOnTheGround < ex.CollectorsEtaSeconds)
            {
                return;
            }
            LandTheCollectors(ex);
            return; // one beat to read the sky before they start walking
        }

        // The maze is law for them too: they bump-and-slide on the SAME segments the captain's boots do, so
        // a building costs them the long way round exactly as it costs an Old One. Unlike an Old One there
        // is no crew-only leash — they came in their own boat and they have their own airlock.
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        bool reachable = !CaptainBeyondReach;

        foreach (Collector c in _collectors)
        {
            double wasX = c.X, wasY = c.Y;
            (c.X, c.Y) = CollectorLanding.Step(
                c.X, c.Y, _avatarX, _avatarY, dt, walls, DeckPlan.AvatarRadius, c.WallSide);

            // #585: the repo crew waits outside too — which is exactly what their own line already says they
            // do ("they take up positions and settle in to wait"). A writ that walks through the door would
            // make that sentence a lie, and would take the one decision out of the scene: whether to sit on
            // your air or run for the tube.
            (c.X, c.Y) = HoldOutsideShelters(c.X, c.Y);

            c.Vx = dt > 0 ? (c.X - wasX) / dt : 0;
            c.Vy = dt > 0 ? (c.Y - wasY) / dt : 0;
            if (Math.Abs(c.Vx) > 1e-6 || Math.Abs(c.Vy) > 1e-6)
            {
                c.Facing = Math.Atan2(c.Vy, c.Vx);
            }

            // A shelter is a pressure vessel, not a sanctuary — and the game says so out loud rather than
            // letting the player discover it by being taken inside one they thought was safe.
            if (!ex.CollectorShelterNoted && ShelterUnderfoot(ex) >= 0
                && CollectorLanding.HasYou(c.X, c.Y, _avatarX, _avatarY) is false
                && Distance(c.X, c.Y, _avatarX, _avatarY) < 24)
            {
                ex.CollectorShelterNoted = true;

                // #768 · HELD: the siege plate goes up two lines below, and this is the one sentence in the
                // scene that tells a captain the shelter they are standing in will not save them. It is a
                // rule of the world learned once — a Beat — and it was being said under a backdrop.
                HoldSaying(CollectorLanding.ShelterIsNotSanctuaryLine, PulseRank.Beat);

                // #528 · AND THE PICTURE DOES THE SAME JOB THE LINE DOES: it shows them SETTLED, not
                // attacking. Nothing in this frame is a fight. The clock is your tank, and what makes it
                // horrible is how comfortable everyone else looks.
                ShowRevealCard(
                    CollectorLanding.SiegePlate.Title,
                    CollectorLanding.SiegePlate.ArtFile,
                    CollectorLanding.SiegePlate.Caption);
                ReleaseHeldSayingsUnlessACardStopsTheWorld();   // #768 — it does; the shelter line waits
            }

            if (reachable && CollectorLanding.HasYou(c.X, c.Y, _avatarX, _avatarY))
            {
                TheWritIsServed(ex);
                return;
            }
        }
    }

    private static double Distance(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>#583 · The boat touches down, off to one side of the tube — near enough to be between you
    /// and the way home, never on top of the hatch (a boat parked on the door would end the excursion by
    /// geometry instead of by decision).</summary>
    private void LandTheCollectors(SurfaceExcursion ex)
    {
        ex.CollectorsLanded = true;
        ex.CollectorBoatX = CollectorLanding.SetsDownX(MoonSurface.SpawnX, ex.ThreatSeed);
        ex.CollectorBoatY = MoonSurface.ReeverBarrierY - 6;

        int party = CollectorLanding.PartySize(_heat.Level);
        _collectors.Clear();
        for (int i = 0; i < party && i < MaxCollectors; i++)
        {
            _collectors.Add(new Collector
            {
                X = ex.CollectorBoatX + ((i - ((party - 1) / 2.0)) * 3.5),
                Y = ex.CollectorBoatY - 2,
                Facing = -Math.PI / 2,
                WallSide = i % 2 == 0 ? 1 : -1,
            });
        }

        RendererInterop.PlayCue("alarm");

        // #768 · HELD, because the plate below goes up in the same breath and both of these would play under
        // its backdrop — the same family as the Hive's arrival, arising the same way: the world acting, not a
        // press on a pop-up. The RANK is what decides which one the captain is left with, and it is a
        // judgement about what the lines ARE: a boat you did not call setting down beside yours is a thing
        // that happened once and the book will keep (Beat); the hail that follows it is radio (Status).
        HoldSaying(CollectorLanding.ArrivalLine(ex.CollectorCallsign), PulseRank.Beat);
        if (!ex.CollectorsHailed)
        {
            ex.CollectorsHailed = true;
            HoldSaying(CollectorLanding.HailLine);
        }

        // #528 · THE ONLY WARNING THE PLAYER GETS. Four loaded lines narrate this pursuit and every one of
        // them was a toast; the arrival is the worst place for that, because after it the only information
        // in the world is a tracker fan. A sentence that fades in a second and a half is not a warning.
        // Core owns the words — and the caption is ClosingLine, which was written, reviewed and shipped and
        // then referenced by nothing at all until now.
        ShowRevealCard(
            CollectorLanding.ArrivalPlate.Title,
            CollectorLanding.ArrivalPlate.ArtFile,
            CollectorLanding.ArrivalPlate.Caption);
        ReleaseHeldSayingsUnlessACardStopsTheWorld();   // #768 — it does, so the two lines wait for the ✕

        // It is a fright, and a specific one: the ground just stopped being only about the Old Ones.
        ApplyNerveShock(4.0, "a boat you did not call, setting down beside yours");
        RequestVaultSave();
    }

    /// <summary>#583 · A hand on your carry loop, on foot, on somebody else's moon. It opens the SAME demand
    /// the same people open on your own deck — submit, bribe, or resist — because it is the same writ and
    /// they want the same thing. What is different is that you walked into it and cannot burn away.</summary>
    private void TheWritIsServed(SurfaceExcursion ex)
    {
        RendererInterop.PlayCue("board");
        ShowPulseMessage(CollectorLanding.ContactLine(ex.CollectorCallsign));

        ulong seed = DiceRule.Seed(ex.ThreatSeed, $"busted-on-foot:{(long)SimTime}");
        _busted = new BustedEncounter
        {
            HunterId = $"collector-ground:{ex.Stop.Body.Id}",
            HunterCallsign = ex.CollectorCallsign,
            Heat = Math.Max(1, _heat.Level),
            Seed = seed,
            Bribe = BustedRule.BribeDemand(Math.Max(1, _heat.Level), seed),
            Cause = DeathCause.Collector,
            DeathBodyName = ex.Stop.Body.Name,
        };
        RequestVaultSave();
    }

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
        // #580 · NO SHIP HEAT FROM A HAND ON YOUR SUIT. This used to raise _heat by one per catch, and that
        // was wrong twice over. Owner: "moving on the planet should NOT cause HEAT" / "any heat should happen
        // on the surface or site, not in space" / "we don't want to be guarding our parking lot ... that is
        // not good game play :-D".
        //
        // He is right on the fiction and on the play. HEAT is what the collectors and the law hold against
        // your SHIP, earned by robbery and piracy and hot cargo — an Old One grabbing a suit on Miranda tells
        // nobody anything, and there is no ledger out here to be entered in. On the play side the coupling
        // was worse than untidy: it turned every excursion into a slow tax on the parked ship, so a good long
        // walk came home to wolves. The site's own pressure is ex.Catches, above, and that stays local.
        //
        // Same class as the Debt Collector deaths he caught earlier: a space-side consequence reaching down
        // onto a moon where it has no business being.
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
    /// <para>#621 · And the answer now lives in <see cref="AwayTeamSide.BackAtTheShuttle"/>, because the AIR
    /// needed the same fact and worked it out for itself with the moon's rule alone — the same bug, in the
    /// one instrument a captain cannot survive being lied to by. Two places computing one fact is the bug
    /// even while they agree.</para>
    private bool CaptainBeyondReach =>
        AwayTeamSide.BackAtTheShuttle(OnWreck, _avatarX, _avatarY, DeckPlan.AvatarRadius);

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

            // #696 · SOMETHING GOT A HAND ON YOU, AND THE EXPOSURE IS GONE. Placed before the block roll on
            // purpose: being REACHED is what ends the hold, not being hurt by it. A captain who turns a blow
            // aside has still had somebody's arm come through the space they were photographing into, and a
            // darkroom that survived that would be telling them the ground is safer than it is — which is
            // the whole thing the twenty seconds were bought to say.
            ProcessingIsInterrupted(Core.Processing.Interruption.Reached);

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
        // #633 · FOUR BANDS, NOT THREE, AND THEY MAY NOT OVERLAP. `main` put the sweep team at
        // `3 + ReeverEngineCeiling` because on that branch nothing else lived there; this branch had already
        // given those slots to the repo crew (#583). Reunited without this line the collectors' loop below
        // would overwrite all three sweepers every frame — one buffer written by two fillers, which is the
        // named bug class and exactly the thing a merge produces. The bands are stated ONCE, here and in
        // SurfaceDroidCount, and every filler is offset from the one before it.
        FillSweeperDroids(buffer, 3 + ReeverEngineCeiling + MaxCollectors);
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

        // #583 · And the repo crew, in their own slots after the Old Ones. Drawn as people, named so the
        // renderer can give them their own ink — they are not hostiles of the same kind and should not read
        // as more Reevers on the walked map.
        for (int i = 0; i < MaxCollectors; i++)
        {
            int slot = 3 + ReeverEngineCeiling + i;
            buffer[slot] = i < _collectors.Count
                ? new DeckPlan.Droid(_collectors[i].X, _collectors[i].Y, _collectors[i].Facing, "Collector")
                : new DeckPlan.Droid(-9999, -9999, 0, "Collector");
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
            _hudEntities.AddRange(EverythingThatMoves());   // #538: the pack AND the sweep team

            // #583: a repo crew that boarded a wreck behind you is a contact like any other.
            foreach (Collector c in _collectors)
            {
                _hudEntities.Add(new MotionTracker.Entity(c.X, c.Y, c.Vx, c.Vy));
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
        // #538 / #583 · The pack, the sweep team AND the repo crew — everything on this ground that is on
        // its feet, from the one accessor that lists them.
        _hudEntities.AddRange(EverythingThatMoves());

        // #591 · The sweep is cut to what the fan can hear from this floor. On the regolith that is
        // unbounded and nothing changes; eleven floors down it is the reason the corridor is quiet.
        double fanReach = FanReach();
        IReadOnlyList<MotionTracker.Blip> blips =
            MotionTracker.Sweep(_avatarX, _avatarY, _hudEntities, fanReach);
        double? nearest = blips.Count > 0 ? blips[0].Range : null;
        bool closing = nearest is { } n && _lastNearestReeverRange is { } prev && n < prev - 0.01;
        _lastNearestReeverRange = nearest;

        _hudBlips.Clear();
        foreach (MotionTracker.Blip b in blips)
        {
            _hudBlips.Add((b.Bearing, b.Range));
        }

        // ── #591 · A CONTACT BEHIND A WALL IS A SMUDGE, NOT A CLEAN BLIP ──
        //
        // Open regolith is open: a return out there is a return, and the fan's report is as good as it gets.
        // Inside a poured facility it is not — a fan that reads a body through two bulkheads with the same
        // confidence it reads one down an open corridor is claiming a precision it does not have.
        //
        // No new mechanism: this is the same fog #371 built for wrecks (SightBlockers → a blurred region
        // whose radius grows with range, because a crude fan is less sure about a far return), pointed
        // underground. The buffer is cleared unconditionally so a floor with nothing on it cannot inherit
        // the smears of the last derelict the captain walked.
        //
        // Nothing walks these corridors yet — the Old Ones are a regolith tide and are cleared on descent
        // (owner: "I don't think there should be reevers down here"). So today this smudges an empty floor.
        // That is the correct order of work and the issue says so: make the instrument honest first, and
        // whatever eventually comes down here inherits a tracker that already behaves like it is
        // underground rather than one that has to be taught afterwards.
        _hudSmudges.Clear();
        if (ex.Floor < 0)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = SightBlockers();
            foreach (MotionTracker.Blip b in blips)
            {
                double bx = _avatarX + (Math.Cos(b.Bearing) * b.Range);
                double by = _avatarY + (Math.Sin(b.Bearing) * b.Range);
                if (SurfaceCollision.HasLineOfSight(_avatarX, _avatarY, bx, by, walls))
                {
                    continue;   // you can see it. Your own eyes beat the fan, exactly as they do aboard.
                }
                _hudSmudges.Add((bx, by, SmudgeBaseRadius + (b.Range * SmudgeRangeSpread)));
            }
        }

        // The own caches' ✗ marks (with the DigX/DigY-or-hash-scatter fallback, same as OwnCachePositionsAt)
        // straight into the reused buffer — no intermediate list + Select allocation.
        string bodyId = ex.Stop.Body.Id;
        _hudMarks.Clear();

        // #591 · EVERYTHING BURIED IS BURIED ON THE SURFACE. A floor of the Hive reuses the surface's own
        // coordinate envelope (#585), which is what makes depth free — and it also means a cache buried at
        // (x, y) on the regolith has an (x, y) on B3 that is several hundred metres of rock away and belongs
        // to somebody else's corridor. Drawn unguarded, the captain's own treasure ✗ appears ON the facility
        // deck, and its beacon on the fan points at it.
        //
        // Same reasoning as the beacons above: these are surface instruments reporting surface facts, and
        // underground they are not merely useless but WRONG. Gated once, here, because _hudMarks feeds both
        // the on-grid marks and BuildCacheBeacons — one source, one gate.
        // 🗺 Layers (#405) Ground finds → Treasure ✗: the buried-cache marks the excursion HUD carries.
        foreach (TreasureCache c in LayerVisible("finds.treasure") && ex.Floor >= 0 ? _caches.CachesAt(bodyId) : [])
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
        // #591 · Also a surface fact: a probed regolith square has nothing to say about a poured floor
        // hundreds of metres under it.
        foreach (KeyValuePair<(int X, int Y), BeachComber.Outcome> kv in ex.Floor < 0 ? [] : ex.Swept)
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
            // #371 Phase 3 / #562 / #696: the one progress bar serves every slow thing — a dig, a forced
            // door, a document being photographed, or the tube racking a magazine. The rearm is last because
            // it is the only one that can be running while the captain is somewhere the others cannot happen
            // (inside the tube), so it can never actually contend; ordering it here keeps the hands-on
            // channels reading first.
            DigProgress: ex.Channel?.Progress ?? ex.DoorChannel?.Progress
                ?? (ex.Processing is { } paper
                    ? Core.Processing.Fraction(paper.Elapsed, ProcessingSeconds)
                    : ex.RearmBotIndex is not null ? ex.RearmProgress : -1),
            // #562: and it says which. A shovel over a magazine being racked would be exactly the class of
            // lie this lane exists to fix; the rearm is the ship HELPING you, so it reads cold-green.
            ChannelGlyph: SurfaceChannelGlyph(ex),
            ChannelIsAid: SurfaceChannelIsAid(ex),
            HasDroppedChest: ex.ChestDropped, DropX: ex.DropX, DropY: ex.DropY,
            Blips: _hudBlips,
            Cadence: (int)MotionTracker.CadenceFor(nearest),
            Readout: MotionTracker.Readout(nearest, closing),
            CacheMarks: _hudMarks,
            Nerve: _nerve,
            NerveReadout: NerveModel.Readout(_nerve),
            // #573: the places worth walking to, as calm rings on the fan — plus your own caches once they
            // are in reach, and any rumour you are working from as a wide soft wash.
            Beacons: BuildBeacons(ex),
            CacheBeacons: BuildCacheBeacons(),
            Rumours: BuildRumours(ex),
            // #564: the tank, drawn as a bar under the tracker.
            AirSeconds: ex.AirSeconds,
            // #612 · Is the tank actually running? The gauge said nothing either way until the owner asked
            // "where here does it say if I consume tanks or have air?" — and it now asks ONE function
            // (#608), the same one the drain itself is gated on.

            AirDistanceHome: DistanceToTheTube(),
            // #612 · AND WHERE IT IS COMING FROM — the sim's own answer, handed down rather than worked out
            // again in the renderer. Owner, on a pressurised floor: "where here does it say if I consume
            // tanks or have air?" The bar showed a clock and never said whether the clock was running.
            AirSupply: AirSupplyOf(ex),
            // #573 · AND, once it is low, a BIG on-grid counter anchored to the captain — the same
            // seven-segment idiom the reactor overload uses, which is the owner's own comparison
            // ("similar counter as the round count counting down seconds on the map"). A bar in the corner
            // is for glancing at; this is for when glancing is no longer enough.
            Countdown: SuitAir.RunningLow(ex.AirSeconds, DistanceToTheTube()) || SuitAir.OnTheReserve(ex.AirSeconds)
                ? (_avatarX, _avatarY + 2.6, $"O2 {(int)(ex.AirSeconds / 60)}:{(int)(ex.AirSeconds % 60):00}")
                : null,
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
            BloodSplash: BloodShowing ? Math.Clamp((_bloodUntilMs - (_lastTimestampMs ?? 0)) / 900.0, 0, 1) : 0,
            // #591 · The fan's real reach, so the ring the captain reads is the ring the chirp heard, and
            // where they are, so depth is on the instrument rather than on the plan behind them.
            FanReach: fanReach,
            TrackerPlace: ex.Floor < 0 ? UndergroundComplex.NameOf(ex.Stop.Body.Id, ex.Floor) : null,
            // #591 · Contacts heard through a wall are a REGION, never a body. Nothing walks these corridors
            // yet (the Old Ones are a regolith tide and are cleared on descent, by the owner's ruling), so
            // today this smudges an empty floor — which is the correct order of work: make the instrument
            // honest first, and whatever eventually comes down here inherits a tracker that already behaves
            // like it is underground instead of one that has to be taught after the fact.
            Smudges: _hudSmudges);
    }

    // #440 · The standing prompt: ONE bright line above the keybar for the thing this excursion hangs on.
    // Owner, 2026-07-26: "the press T to bury treasure is not advertised clearly enough on surface… It is
    // the key to survival there" — said while misremembering the key, which is the proof. A chest in hand is
    // the whole reason you came and the whole thing you lose, so it gets a line that does not blend into
    // chrome and does not go away until the chest is in the ground. It also answers WHERE, because "where
    // you stand" is the rule and nothing on screen ever said so: out on the open regolith, past the pad.
    private string? BuildStandingPrompt(SurfaceExcursion ex)
    {
        // #696 · A HOLD OUTRANKS THE CHEST. For the seconds it runs, the one thing on the screen that can
        // still be decided is whether the captain keeps their boots where they are — and the prompt says the
        // clock, because "hold position" without a number is an instruction to wait for an unknown length of
        // time while something walks towards you.
        if (ex.Processing is { } paper)
        {
            return $"{Core.Processing.Glyph} PROCESSING — hold position " +
                $"({Core.Processing.SecondsLeft(paper.Elapsed, ProcessingSeconds):F0} s). Step away and it is lost.";
        }

        if (!ex.Carrying)
        {
            return null; // nothing owed — the ground goes quiet again
        }
        // #723 · The floor rides along, so this line stops promising a burial on a Hive corridor. Underground
        // it now reads "walk out onto the regolith" — which is the honest instruction down there, because the
        // way to bury a chest 150 m under a facility is the lift.
        return MoonSurface.IsDiggableGround(_avatarX, _avatarY, ex.Floor)
            ? "⛏ CARRYING THE CHEST — press E to BURY IT HERE"
            : "⛏ CARRYING THE CHEST — walk out onto the regolith, then E to bury it";
    }

    /// <summary>#562 + #696 · WHICH slow thing the one bar is showing. A ladder rather than four inline
    /// conditions at the call site, because the glyph, the tint and the PROGRESS all have to pick the same
    /// winner — and three copies of one precedence order is the shape that drifts.</summary>
    private static string SurfaceChannelGlyph(SurfaceExcursion ex) =>
        ex.Channel is not null || ex.DoorChannel is not null ? "⛏"
        : ex.Processing is not null ? Core.Processing.Glyph
        : ex.RearmBotIndex is not null ? "🔫"
        : "⛏";

    /// <summary>#562 · Is the bar the ship HELPING you (cold green) or you exposing yourself (warning
    /// amber)? Only the rearm is help. The darkroom is emphatically not: standing still in the open for
    /// twenty seconds is the cost the whole mechanic is made of, and a soothing colour over it would be the
    /// picture arguing with the sim.</summary>
    private static bool SurfaceChannelIsAid(SurfaceExcursion ex) =>
        ex.Channel is null && ex.DoorChannel is null && ex.Processing is null && ex.RearmBotIndex is not null;

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
            var aboard = new List<string>
            {
                "WASD — move",
                // #698 · What [E] will actually do, and the ground wins. The recovery runs ahead of console
                // dispatch (#691), so standing in the ring with "E — examine / take" on the bar is the bar
                // describing a press it is not going to get.
                StandingOnWhatYouLeft() ? LeftBehind.ReachPrompt : "E — examine / take",
            };

            // #538 · the sentry remote lives on the HUD, and it never hides: an affordance you cannot see is an
            // affordance you do not have (#212), and this is the one whose absence gets a captain shot.
            if (ex.Bots.Count > 0)
            {
                aboard.Add(_weaponsTight ? "🤖 H — WEAPONS TIGHT (press to free)" : "🤖 H — weapons tight");
            }

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
            if (_satchel.Count > 0)
            {
                aboard.Add($"🎒 I — items ({_satchel.Count})");
            }

            // #537 · A VERB NOBODY IS TOLD ABOUT IS A VERB NOBODY HAS. Caught by booting the scene and
            // reading the hint bar, which is the owner's own method: the knock was bound, the clock ran, the
            // sweep team heard it — and the strip along the bottom never mentioned K existed.
            aboard.Add(IsSounding
                ? (_soundQuietly ? "✊ K — stop knocking" : "📡 K — stop sounding")
                : (_soundQuietly ? "✊ K — knock (quiet)" : "📡 K — sound the plating (loud)"));

            aboard.Add("F — first person");
            aboard.Add(_audioEnabled ? "🔊 M — mute" : "🔇 M — unmute");
            return string.Join(" ∙ ", aboard);
        }

        // #440: the bar must NAME the thing that matters. "E — dig / use" is honest but generic, and it was
        // generic at the one moment it should shout — with the chest in your hands (owner, 2026-07-26: "the
        // press T to bury treasure is not advertised clearly enough on surface… It is the key to survival
        // there", having misremembered the key himself). Carrying → the bar says BURY, in the imperative.
        // #698 · AND WHAT YOU PUT DOWN OUTRANKS BOTH OF THEM. Owner, on B12 of the clinic: "I dropped 3
        // files on somebody here but there was nothing marked onto the map?" — the deck now carries the
        // mark, and this is the other half: [E] answers your feet before it answers the walls (#691), so
        // inside the recovery ring the press is the pickup, whatever else the captain is holding. A bar
        // that promised BURY THE CHEST while the key handed back a folder would be the sim doing one thing
        // and a sentence reporting another, which is a bug class this repo has named.
        // #723 · …and that is precisely what this bar was doing underground. It offered "E — dig" on poured
        // rockcrete, and with a chest in the sling it shouted BURY THE CHEST HERE over a corridor where the
        // key now — correctly — does nothing at all. So the floor is asked first, of the same one fact the
        // key is gated on. Above ground nothing moves: the pad is not diggable either, but it is one step
        // from ground that is, so the chest keeps the imperative #440 asked for.
        var parts = new List<string>
        {
            "WASD — move",
            StandingOnWhatYouLeft() ? LeftBehind.ReachPrompt
                : !MoonSurface.ShovelWorksOnThisFloor(ex.Floor) ? "E — use"
                : ex.Carrying ? "⛏ E — BURY THE CHEST HERE"
                : "E — dig / use",
        };
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

        // #603 · The satchel, once there is anything in it. Owner: "the I key should be advertised in the
        // hud also like we do now for the other keys." Shown WITH the count, because the useful question at
        // a glance is not "do I have pockets" but "is there anything in them".
        if (_satchel.Count > 0)
        {
            parts.Add($"🎒 I — items ({_satchel.Count})");
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

        // #564 · The tank used to be the top line HERE, and the owner went looking for a meter under the
        // tracker and found nothing — because a line of dim 10px text among the key hints is a footnote, not
        // a gauge. It is a drawn BAR now (DeckView, fed by SurfaceHud.AirSeconds); this list is back to
        // being what it always was, the affordances.

        // The dig affordance, honest to the sling (playtest bug #1 / owner ruling #9: the ground must SAY
        // what's possible). Carrying → bury anywhere you stand; empty → the beach-comber probe, a real
        // fishing expedition, never a dead end. An own ✗ in this ground always earns its own lift line.
        // #723 · …and it is only an affordance where the verb exists. This is the line that sent a captain
        // pressing [E] on a spine corridor: teaching the shovel on a floor whose ground is poured rockcrete
        // is teaching a key that will not answer. The same one fact the key and the bar are gated on.
        if (MoonSurface.ShovelWorksOnThisFloor(ex.Floor))
        {
            lines.Add(ex.Carrying
                ? "⛏ E on the regolith — bury the chest where you stand"
                : "🪛 E on the regolith — probe for shallow treasure");
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
