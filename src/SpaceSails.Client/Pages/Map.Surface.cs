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
    /// crew (<see cref="MaxCollectors"/>), the sweep team (<c>InspectionTeam.TeamSize</c>), #804's
    /// rounds (<see cref="PatrolBand"/>), and #731's walkers (<see cref="WalkerBand"/>).
    /// <see cref="FillSurfaceDroids"/> writes them at exactly these offsets.</para></summary>
    private const int SurfaceDroidCount =
        3 + ReeverEngineCeiling + MaxCollectors + InspectionTeam.TeamSize + PatrolBand + WalkerBand;

    /// <summary>#804 · Where the rounds' slots start. Stated as the sum of every band before it, so a fifth
    /// filler cannot quietly overwrite a fourth — which is precisely the bug #633 paid for.</summary>
    private const int PatrolFirstSlot =
        3 + ReeverEngineCeiling + MaxCollectors + InspectionTeam.TeamSize;

    /// <summary>#731 · …and where the walkers' slots start: after the rounds, by the same arithmetic and for
    /// the same reason. A guard and a regular finishing a drink are two different kinds of person and never
    /// share a slot.</summary>
    private const int WalkerFirstSlot =
        3 + ReeverEngineCeiling + MaxCollectors + InspectionTeam.TeamSize + PatrolBand;

    internal sealed class Reever
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
    public sealed class SurfaceBot
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
    public enum DigKind { Bury, Lift, Probe }

    public sealed class DigChannel
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
    public sealed class DoorChannel
    {
        public double Progress;         // 0..1
        public required string DoorId;  // the sealed door being forced (outer or nested)
        public double AnchorX, AnchorY; // the door console — stepping away from HERE aborts
    }

    // #394 · THE DRILLING. The channel that sinks the charge into the rock — parallel to the door-force
    // channel but MUCH longer (DeflectionGig.RockProfile.DrillSeconds, per rock type) and, unlike a door,
    // its Progress PERSISTS across re-channels: a drill-snap complication backs the progress up, and the
    // captain sets the shoulder again from there. Abortable by stepping away from the drill point.
    public sealed class DrillChannel
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

        // #1063 · …and the clock having moved, the neighbours have had their shift. THE BURIAL IS EVALUATED
        // HERE and only here: after the crossing's time is spent and before one wall of this ground has been
        // laid, which is the only moment a filled ground can be filled without a captain standing in it. See
        // Map.Burial.cs — on every voyage where nobody has been past a seam this does nothing at all.
        BuryWhatWasOpened();

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

        // #615/#573 · AND THE BUILDING REMEMBERS BEING WALKED. The rooms this captain has already gone
        // through under THIS moon, out of the register that rides the vault and into the live set the deck
        // builder reads. Here, before the first frame of the walk, because a floor drawn ahead of the seeding
        // would put a console back on a room that was emptied a month ago — and because LEAVE's whole promise
        // is that a declined find is still there, which is a promise you can only keep if a KEPT one is not.
        SeedTurnedOverRooms(excursion);

        // #316 law 1 · …AND THE HUSKS THE LAST VISIT LEFT LYING HERE. Same moment, same reason: what the
        // ground kept is on the ship's ledger, and a field is meant to still be the field you shot it up.
        SeedTheHusksLeftHere(excursion);

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

        // #973 L4 · SETTING DOWN IS AN ARRIVAL TOO. A page you don't remember writing that NAMES this ground
        // is finished by standing on it — no roll. Said after the boat's own receipt above and before the
        // wreck branch below, so a derelict boarding gets it exactly as a regolith landing does.
        TheArrivalIsRemembered(stop.Body.Id);

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
        // #455 rule 2 · …and a chest left on the regolith now stays there as a REAL cache, in the open, on
        // the harder roll. See LeaveTheDroppedChestInTheOpen (Map.Surface.Dig) for why that is the build the
        // rule was asking for rather than a nicety.
        TreasureCache? dropped = LeaveTheDroppedChestInTheOpen(ex);

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
        //
        // #731 · …and a crew who are WALKING HOME were not outwalked. Once the writ is settled the escape
        // line is a lie of the same family as the one that sentence exists to prevent: the captain paid, or
        // fought clear, and watched them go. Nothing is said in that case, which is the correct amount.
        bool outwalkedTheWrit = ex.CollectorsLanded && !ex.CollectorsGoingHome && _busted is null;

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
        // back in the sling.
        //
        // #455 rule 2 · Lift off without it and it is no longer simply gone. It stays where it fell, in the
        // open, on the harder roll — a real chest in the ledger with a real map card, which is what makes
        // "buried beats dropped, by a lot" a thing the game can be wrong about instead of a thing it merely
        // says. The coin and cargo therefore DO leave the books now (#648's honest "nothing left the books"
        // line was honest about a world that no longer exists), so the news is what the chest reads as.
        string dropTail = dropped is { } open
            ? $" 🧰 You lifted off without the chest you dropped — it stays where it fell. {open.Safety.Sentence} Map filed (🗺)."
            : droppedAndLeft
                ? " 🧰 You lifted off without the chest you dropped — but it was empty, so nothing was left out there."
                : "";
        if ((buried ?? dropped) is { } cache)
        {
            _treasureMapCard = cache;
            RendererInterop.PlayCue("reveal");
            string tail = escapedWithWatchdogs
                ? $" {cache.ReeverLevel} Old One(s) haunt this ground now — the best kind of lock."
                : "";
            ShowPulseMessage(buried is not null
                ? $"🛸 Lifted off {ex.Stop.Body.Name}. Map filed (🗺).{tail}{botTail}"
                : $"🛸 Lifted off {ex.Stop.Body.Name}.{tail}{botTail}{dropTail}");
        }
        else if (!settledExpedition && !settledDeflection) // an away-gig settle already spoke its payout line
        {
            string tail = escapedWithWatchdogs ? " You outran the Old Ones." : "";
            ShowPulseMessage($"🛸 Back aboard from {ex.Stop.Body.Name}.{tail}{botTail}{dropTail}");
        }
    }

}
