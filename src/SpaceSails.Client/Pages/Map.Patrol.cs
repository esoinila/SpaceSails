using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #804 · THE ROUNDS, ON THE FLOOR. The brain is <see cref="PatrolBeat"/> in Core and every number, every
/// stop and every sentence below comes from it; this file is the walking, the knowing and the telling.
///
/// <para><b>They walk the A* the audits walk.</b> Owner, #729: <i>"maybe the guards can use A* also so they
/// do not come off as reevers or kind of crazy scary."</i> A leg of a round is planned with
/// <see cref="AutoWalk"/> over <c>DeckReachability</c> — the same route machinery the click-to-walk cheat
/// hands the captain, over the same collision field the captain's own stepper asks — and spent through
/// <see cref="SurfaceCollision.Slide"/> at a person's gait. A thing that finds doorways is somebody on a
/// payroll, and the player is meant to read that off the motion before any card says so.</para>
///
/// <para><b>What the captain knows and what the guard knows are different, and the difference is the
/// feature.</b> A marker is drawn only inside the captain's own sightline; outside it the motion tracker
/// hears a mover through the rock (the instrument is untouched — a guard walks, so a guard is a contact),
/// and closer-but-unseen there are boots. The guard registers the captain at a THIRD of the eye's reach, so
/// there is a real window in which you can see them and they cannot see you. That window is the whole
/// stealth verb: watch the round, wait, step out behind it.</para>
///
/// <para><b>A sighting still cannot start a chase, and that is still the default.</b> A round that registers
/// somebody standing there hails, walks over, reads the wallet, and at worst walks you back to the lift.
/// That is the owner's original law and it is untouched.</para>
///
/// <para><b>#835 · THE OTHER BRANCH, WHICH IS EARNED AND NEVER AMBIENT.</b> Owner, reversing his own
/// standing law with the implementation named: <i>"they need to catch us .... like reevers :-D we could use
/// that code :-D"</i> — and, in the same breath, <i>"just no damage by default :-D"</i>. So there are now
/// exactly three doors into a run (<see cref="PatrolBeat.Provocation"/>): walking off on a hail for the
/// SECOND time in a watch, being watched taking a hasp off with a gun, and having been booked
/// <see cref="PatrolBeat.EscortsAWatchAllows"/> times already. Every one of them is a thing the captain did,
/// and the paragraph above is what happens on every floor where none of them has. He calls it in before he
/// moves (<see cref="TheRadioCall"/>), comes on the Old Ones' own homing step at a person's gait
/// (<see cref="RunAfterTheCaptain"/> → <c>ReeverChase.Step</c>), and ends either with a hand on your arm
/// (<see cref="HeHasYou"/>) or standing in a corridor watching you go (<see cref="HeLosesYou"/>). He is
/// never removed from the floor by either. <b>Nothing on this road touches the captain's health</b> — there
/// is no <c>HitsTaken</c> in this file and there never will be — and the ladder it feeds is the escort you
/// already know, which past the threshold simply keeps going up (<see cref="TheKickOut"/>).</para>
///
/// <para><b>#833 · AND EVERY BEAT OF IT IS WALKED.</b> Two things in this file used to be sentences over
/// placements, and the owner caught both in one evening on B2. The card went up the frame he NOTICED you, at
/// up to nine deck units — <i>"I think the guard should approach us when it does the inspection"</i> — so
/// there is now a HAIL and an APPROACH between the notice and the read (<see cref="TheHail"/>,
/// <see cref="WalkUpToTheCaptain"/>), the captain's controls stay free through all of it, and walking away is
/// allowed. And the escort was <c>StandCaptainAt</c>: <i>"how did I jump to elevator there?"</i> — so the
/// walk back is now a walk (<see cref="WalkTheEscort"/>), he plans the route himself, the captain is walked
/// at his shoulder through his own collision, and both of them are moving contacts on the fan the whole way.
/// The one placement left is behind a caption that ADMITS it is a cut.</para>
///
/// <para><b>One stepper.</b> The round, the walk-up and the escort all spend their frame through
/// <see cref="SpendTheStride"/> — the captain's own sub-stepper, once. #832 was paid for by a copy of that
/// loop drifting from its original by one epsilon; three copies of it would have been three chances to do
/// that again.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>One guard, walking. Mutable and client-side for the reason the Reevers and the sweep team
    /// are: the rules are pure in Core and the list is the client's business.</summary>
    private sealed class Guard
    {
        /// <summary>What is drawn over them — the ROUND's number, from Core.</summary>
        public required string DeckName { get; init; }

        /// <summary>Who they read as when the round stops at you.</summary>
        public required string Plate { get; init; }

        public double X;
        public double Y;
        public double Facing;

        /// <summary>How fast they are travelling this frame. A MOTION tracker hears travel and nothing else,
        /// so without this the fan would report an empty floor while two people walked it — the panel
        /// disagreeing with the sim, which is the one thing this codebase does not allow.</summary>
        public double Vx;
        public double Vy;

        /// <summary>Which stop of the shared beat they are heading for.</summary>
        public int Leg;

        /// <summary>Seconds left standing at the stop they have reached. THE GAP a captain times.</summary>
        public double Standing;

        /// <summary>Seconds since this one last stopped the round at the captain. Starts far in the past so
        /// the first challenge of a floor is never held back.</summary>
        public double SinceStop = PatrolBeat.AfterTheStopSeconds * 4;

        /// <summary>The A* leg they are spending, or null when the next one is due.</summary>
        public AutoWalk? Route;

        /// <summary>#858 · The NEXT leg, being planned a slice at a time while he stands at this one. Null
        /// whenever he is not standing at a stop.
        ///
        /// <para>Lab 45 priced the plan he used to make on the frame he left a stop at up to 6.4 ms — 38.6%
        /// of a 60 fps frame, natively, in a game that ships to WASM — and it lands on a frame the player is
        /// often watching him on. He stands for five seconds either way; this is the same work, done then.
        /// It carries the two points it was planned between (<c>AutoWalk.Planner.PlannedFor</c>), so a man
        /// whose errand changed while he stood can never be handed a route he did not ask for.</para></summary>
        public AutoWalk.Planner? Planning;

        /// <summary>#832 · How many times in a row this leg has been re-planned because the ground refused a
        /// step. Bounded, so a stop that genuinely cannot be reached is dropped rather than ground at
        /// forever — and reset the moment they arrive anywhere.</summary>
        public int Retries;

        /// <summary>#832 · What the captain can make of them on the frame just drawn — nothing, a distant
        /// figure, or the marker. Read by the droid filler, written by the step: one answer per frame, so
        /// the marker, the smear and the challenge cannot disagree about whether there is anybody
        /// there.</summary>
        public PatrolBeat.Sighting Seen;

        /// <summary>#793 · Whether this one is HELD — stopped because the captain sat down on a bench in the
        /// open (<see cref="FootTail.MustHold"/>). One answer per frame, written by the step and read by the
        /// filler, so the figure that has stopped and the figure DRAWN as stopped are one figure.
        ///
        /// <para>It is false for every guard in the game and will stay false: a round is a route the
        /// building published before the captain arrived, and a published route cannot be a tail. The field
        /// is here because the hold is a law about MOVERS rather than about watchers — the day something
        /// does follow the captain, it must not need a second stepper to be stopped by a bench.</para></summary>
        public bool Held;

        /// <summary>#833 · Whether this one has said <i>hold on</i> and is crossing the floor to you. The
        /// round is suspended while it is true and resumes from wherever he ends up, whether he arrives or
        /// gives up — a walk-up is a detour, never a new state machine.</summary>
        public bool WalkingUp;

        /// <summary>#833 · How long he has been walking up. Bounded by
        /// <see cref="PatrolBeat.WalkUpSeconds"/>, because a captain who keeps a pillar between you and him
        /// for twenty seconds has walked away by any honest reading.</summary>
        public double WalkUpFor;

        /// <summary>#833 · Seconds until the next re-plan. A walk-up and an escort chase a MOVING target (the
        /// captain, and the captain's shoulder), and an A* every frame is not free in WASM — nor is it what a
        /// man crossing a corridor does.</summary>
        public double RePlanIn;

        /// <summary>#835 · Whether this one has called it in and is coming at a run. False for every guard on
        /// every floor until the captain earns it, which is the whole of the ambient law.</summary>
        public bool AfterYou;

        /// <summary>#835 · Why he is. Carried on the man rather than on the page because it is what he SAYS
        /// when he reaches you (<see cref="PatrolBeat.WhyHeCame"/>), and a reason kept anywhere else could
        /// be a different reason by the time the card goes up.</summary>
        public PatrolBeat.Provocation Why;

        /// <summary>#835 · How long he has been at it. Bounded by
        /// <see cref="PatrolBeat.AfterYouSecondsCap"/> — he is a retired cop, not a wolf.</summary>
        public double AfterYouFor;

        /// <summary>#835 · Seconds of radio left before he moves. He stands still for this, and that beat IS
        /// the warning the run is starting.</summary>
        public double CallingIn;

        /// <summary>#835 · Which hand he takes a wall on when the direct run is spent — <c>ReeverChase</c>'s
        /// own stable handedness, so he rounds a corner instead of dithering at the face of it. Fixed when
        /// the run starts, per #324's reason: a side that changed frame to frame is a body that never gets
        /// anywhere.</summary>
        public int WallSide = 1;

        /// <summary>
        /// #821 · WAS HE LOOKING AT YOU WHEN THE CATCH WENT OVER?
        ///
        /// <para>THE one bit the whole hide turns on. It is written by the press
        /// (<c>Map.Cubicle.ShutTheCubicle</c>) off <see cref="PatrolBeat.Notices"/> — the same predicate the
        /// challenge is gated on, over the same sight blockers, on the frame the catch turned — and never
        /// re-derived here, because a partition goes across the opening on the very next rebuild and every
        /// answer after that would be "no".</para>
        ///
        /// <para>False is the ordinary case and the one the feature is FOR: a man who came round the corner
        /// afterwards is looking at a door with OCCUPIED on it, which is what a washroom door says all
        /// day.</para>
        /// </summary>
        public bool SawYouShutIt;

        /// <summary>#821 · Whether he is standing outside a shut cubicle waiting for it to open. The round is
        /// suspended while it is true and resumes from wherever he ends up — a wait is a detour, exactly as a
        /// walk-up is, and never a new state machine.</summary>
        public bool Knocking;

        /// <summary>#821 · Whether the two knuckles have already landed. Once per wait: a man who knocked
        /// twice would be a loop, and the whole of the line is that he does not knock again.</summary>
        public bool Knocked;

        /// <summary>#831 · Which watchclock station he last signed on his round, or 0 before the first one.
        /// It is what makes the DOUBLE SIGN-IN readable: a made tail's cover act at the station he signed a
        /// minute ago is the gumshoe's confirmation, and nothing anywhere says so out loud
        /// (<see cref="PatrolBeat.DoubleSignIn"/>).</summary>
        public int SignedPoint;

        /// <summary>#831 · Which station he is signing RIGHT NOW as a cover act, or 0 when he is not
        /// performing one. Written by the hold, read by the audit — one answer per frame, so the man who has
        /// stopped and the man DRAWN as having stopped for something are one man.</summary>
        public int CoverPoint;

        /// <summary>#831 · WHAT HE DECIDED TO READ, held for as long as the hold lasts. A man who re-chose
        /// the nearest fixture every frame walks toward one, gets nearer a second, turns round, and shuffles
        /// between the two forever — which is a statue with extra steps. He picks once.</summary>
        public PatrolBeat.WallThing? CoverAt;

        /// <summary>#831 · How long he has been getting to it. Bounded by
        /// <see cref="PatrolBeat.CoverDriftSeconds"/>: past that he reads it from where he stands.</summary>
        public double CoverFor;
    }

    private readonly List<Guard> _guards = [];

    /// <summary>#831 · Everything on this floor's walls a held man could plausibly be reading — the
    /// watchclock stations first, then the shut doors' signs and the posters (<see cref="PatrolBeat.ReadablesOn"/>).
    /// Built once with the round, for the round's own reason: it is a fact about the floor, and a stepper
    /// that searched the floor plan every frame would be Lab 45's bill paid sixty times a second.</summary>
    private readonly List<PatrolBeat.WallThing> _patrolReadables = [];

    /// <summary>The beat: the stops, in the order this watch walks them. Built once when the floor is
    /// entered so every guard on it shares ONE route and a captain can learn it — the sweep team's rule,
    /// for its reason: being hidden from has to be legible.</summary>
    private readonly List<PatrolBeat.Stop> _patrolBeat = [];

    /// <summary>How long the captain has been on this floor. Feeds <see cref="PatrolBeat.CanBeNoticed"/>, so
    /// stepping out of the car into somebody's face is a beat rather than an instant.</summary>
    private double _patrolFloorSeconds;

    /// <summary>How long since the boots were last mentioned. One line, cooled — a warning, not a
    /// narrator.</summary>
    private double _patrolHeardAgo;

    // ── #833 · THE WALK BACK, AS STATE ────────────────────────────────────────────────────────────────
    //
    // Deliberately four small fields on the page rather than a class: an escort is one guard, one destination
    // and one clock, it exists only while a floor does, and the moment it needs a type of its own it will be
    // because something other than the lift is a destination — which is a ruling nobody has made.

    /// <summary>#833 · The guard walking the captain back to the car, or null. While it is set the captain's
    /// controls are HELD (<see cref="CaptainIsUnderEscort"/>) and this guard walks the escort rather than his
    /// round.</summary>
    private Guard? _escort;

    /// <summary>#833 · The guard whose read failed and whose walk back has not started yet. The card is up in
    /// front of the captain at that moment; the walk begins when it comes down, so the player watches the
    /// walk rather than reading about it over the top of one.</summary>
    private Guard? _escortDue;

    /// <summary>#833 · Where the escort is going: the car's own mouth, from the one placement the sim ever
    /// puts the captain through (#681). It is the END STATE of the walk rather than the start of it.</summary>
    private (double X, double Y) _escortCar;

    /// <summary>#833 · How long the walk back has been going. Bounded by
    /// <see cref="PatrolBeat.EscortSecondsCap"/> — past which the cut is ADMITTED rather than narrated.</summary>
    private double _escortSeconds;

    /// <summary>#833 · Whether the small talk has landed yet. Once per escort: a man who said the same thing
    /// about the pumps twice on one corridor would be a loop, not a character.</summary>
    private bool _escortSaidPumps;

    /// <summary>#833 · Are the captain's controls being held by somebody walking them off the floor? Read by
    /// the deck's own stepper and by its key handler — the same one answer, so the keys and the legs cannot
    /// disagree about who is steering.
    ///
    /// <para>#835 · A run is deliberately NOT in here. The controls are held for the walk out and for nothing
    /// else: a captain being come after must be able to run, or rung five ("escape is possible") would be a
    /// sentence with no keys behind it.</para></summary>
    private bool CaptainIsUnderEscort => _escort is not null;

    // ── #835 · WHAT THE WATCH REMEMBERS ───────────────────────────────────────────────────────────────
    //
    // Two counters and the shift they belong to. They are on the PAGE rather than on a guard because they
    // are facts about the captain's evening, not about a man: the owner's complaint was that the same FACE
    // had been booked four times, and the fourth guard to write it down is as entitled to know as the first.
    // They survive a floor change (the floors are one site's evening) and they turn over with the watch, the
    // same clock everything else down here turns over on.

    /// <summary>#835 · Which watch <see cref="_escortsThisWatch"/> and <see cref="_walkedAwayThisWatch"/>
    /// belong to. Anything else is last night's paperwork.</summary>
    private long _patrolWatch = long.MinValue;

    /// <summary>#835 · How many times a round has walked the captain back to a car this watch. The number the
    /// owner's own evening produced, and the one both halves of the escalation ask
    /// (<see cref="PatrolBeat.BookedTooOften"/>).</summary>
    private int _escortsThisWatch;

    /// <summary>#835 · How many hails the captain has simply walked away from this watch. The first is free
    /// and stays free.</summary>
    private int _walkedAwayThisWatch;

    /// <summary>#835 · Whether the walk in progress ends at the sky rather than at the car. Decided ONCE,
    /// where the walk begins, off the same predicate the card's own sentence was composed from — so the man
    /// who said he was not pressing the button for your floor is the man who does not press it.</summary>
    private bool _kickOutDue;

    /// <summary>#835 · The escort has reached the car and the ride up is owed. Armed rather than taken, the
    /// same way <see cref="_escortDue"/> is: the ride happens on a frame of its own, outside the loop that is
    /// walking the list of guards it is about to empty.</summary>
    private bool _kickOutRideDue;

    /// <summary>#835 · How long the KICKED OUT plate stays painted on the shed wall. Counted down on the
    /// surface, where nothing else in this file runs, and one rebuild takes it away again.</summary>
    private double _kickedOutPlateFor;

    // ── #836 · THE FLETCH WALLET, AS STATE ────────────────────────────────────────────────────────────
    //
    // Owner, evening playtest 2026-08-11: "I think I should be able to pick the badge I show the guard...
    // like Fletch ... suppose we have 4 different ID's ... one of them real".
    //
    // Three fields, and the division between them is the whole of the feature: what is IN YOUR HAND (decided
    // during the approach, spent at the read), whether the fan is OPEN in front of you, and what the book
    // remembers about every paper you have ever handed anybody. The first two die with the floor; the third
    // is durable, because it is the only thing a chooser row's hint is ever derived from.

    /// <summary>#836 · The paper that goes into his hand when he arrives. Seeded at the hail from
    /// <see cref="WalletChoice.DefaultFor"/> — last shown at this site, else the real one — so a captain who
    /// ignores the fan entirely still hands over the paper a reasonable person would already be holding.</summary>
    private Satchel.Item? _paperInHand;

    /// <summary>#836 · Is the fan up? Only ever while somebody is walking over
    /// (<see cref="WalletChoice.Fans"/> said there was a choice to make), and it comes down the moment he is
    /// at arm's length: no swapping papers in front of a guard.</summary>
    private bool _walletFanOpen;

    /// <summary>#836 · THE CAPTAIN'S OWN PAPER TRAIL — one row per read, naming which identity was shown and
    /// how it went. Durable (it rides the vault), because <i>worked here, twice</i> is worth nothing if it
    /// forgets between excursions, and because the owner's own reading of it is the horror: <i>two names on
    /// one face across one watch is the facility's own case against you</i>, pointed back at the captain.</summary>
    private readonly List<WalletChoice.Shown> _shownBook = [];

    /// <summary>Dev cheat: <c>?patrol=N</c> forces N rounds onto whatever restricted floor you boot onto,
    /// so the scene is reachable without waiting for a watch that rolled two.</summary>
    private int? _patrolCheat;

    /// <summary>Dev cheat: <c>?badge=1</c> mints this site's own pass at the landing, so the satisfied arm
    /// of the challenge is reachable without working the whole cage-crew lane first.</summary>
    private bool _badgeCheat;

    /// <summary>How long between mentions of the boots. Long enough that it is an event; short enough that
    /// a captain who has walked away and come back is told again.</summary>
    private const double HeardAgainSeconds = 20.0;

    /// <summary>How many figures a patrol may need drawing. The band in the droid buffer, stated once.</summary>
    private const int PatrolBand = PatrolBeat.MostOnAFloor;

    // ── PUTTING THEM ON THE FLOOR ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Build the round for the floor the captain has just arrived on, or clear it if this floor has none.
    /// Called from the lift ride — the one place a floor changes — and never from the deck rebuild, which
    /// happens every time a room is searched and would restart the round under a captain who was timing it.
    /// </summary>
    private void SpawnPatrolFor(SurfaceExcursion ex)
    {
        _guards.Clear();
        _patrolBeat.Clear();
        _patrolReadables.Clear();
        _patrolFloorSeconds = 0;
        _patrolHeardAgo = HeardAgainSeconds;

        // #833 · …and the walk back dies with the floor it was being walked on. The car IS the destination,
        // so a captain who has ridden it is a captain the escort is over for — and an escort holding a guard
        // off a list that has just been cleared would hold the controls forever.
        _escort = null;
        _escortDue = null;
        _escortSeconds = 0;
        _escortSaidPumps = false;

        // #835 · …and so does the run and the ride it was owed. A captain who got into the car mid-run has
        // ESCAPED — rung five, and the honest one — so the man who was coming is left standing on a floor
        // this page is no longer simulating. The ride owed is cleared for the same reason it is cleared at
        // the top of TheKickOut: it has either just happened or it never will.
        _kickOutDue = false;
        _kickOutRideDue = false;

        // #836 · …and so does the paper in your hand. A hand goes to a pocket for a man who is walking over,
        // and the man is a floor above now. The BOOK is not cleared here — that is the durable half, and a
        // captain who has ridden one floor has not forgotten which name worked downstairs.
        _walletFanOpen = false;
        _paperInHand = null;

        // #821 · …and so does the hide. A floor change is a new set of doors and a new set of men, and a
        // "he walked past" line kept from the floor above would be a beat about a room nobody is in.
        //
        // The CATCHES go with it, and the reason is in the field's own doc: a catch is a thing a hand is
        // holding shut, and the hand has just ridden the lift. Today it cannot happen — the only way out of
        // a shut cubicle is to turn the catch back — but a door left OCCUPIED on a floor nobody is standing
        // on would be a room the building had sealed against itself, forever, with nothing to say why. The
        // set is the excursion's rather than the vault's for the same reason (see SurfaceExcursion).
        _walkedPastSaid = false;
        ex.CubiclesShut.Clear();

        string bodyId = ex.Stop.Body.Id;
        int level = ex.Floor;

        // #835 · THE WATCH'S OWN MEMORY, turned over with the shift and with nothing else. Asked before the
        // patrolled-floor gate below, because a captain who has been thrown out and has come back down to a
        // floor with nobody on it is still the same evening.
        if (_patrolWatch != ex.CanteenWatch)
        {
            _patrolWatch = ex.CanteenWatch;
            _escortsThisWatch = 0;
            _walkedAwayThisWatch = 0;
        }

        if (!PatrolBeat.IsPatrolled(bodyId, level))
        {
            return;
        }

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(bodyId, level, field);

        _patrolBeat.AddRange(PatrolBeat.BeatFor(bodyId, level, ex.CanteenWatch, floor, field));
        if (_patrolBeat.Count < 2)
        {
            // A floor whose plan yields nothing to walk gets nobody. It is not a failure worth a sentence —
            // an empty corridor is what this building is mostly made of — but it must not put a guard on a
            // round with one stop in it, standing still forever at the car.
            _patrolBeat.Clear();
            return;
        }

        // #831 · …and everything on this floor's walls a held man could be reading. Off Core, once, with the
        // round it belongs to.
        _patrolReadables.AddRange(PatrolBeat.ReadablesOn(floor, field));

        int heads = _patrolCheat is { } forced
            ? System.Math.Clamp(forced, 0, PatrolBeat.MostOnAFloor)
            : PatrolBeat.GuardsOn(bodyId, level, ex.CanteenWatch);

        for (int i = 0; i < heads; i++)
        {
            int leg = PatrolBeat.StartLeg(_patrolBeat.Count, i, System.Math.Max(1, heads));
            PatrolBeat.Stop at = _patrolBeat[leg];
            _guards.Add(new Guard
            {
                DeckName = PatrolBeat.DeckName(i),
                Plate = PatrolBeat.PlateOf(bodyId, level, ex.CanteenWatch, i),
                X = at.X,
                Y = at.Y,
                Leg = (leg + 1) % _patrolBeat.Count,
                Standing = PatrolBeat.StandSeconds,

                // #831 · A man is put on the floor already signing the station he is standing at, looking at
                // it. The first thing a captain stepping off the car sees is somebody doing something.
                Facing = at.Point is { } start ? start.Facing : 0,
                SignedPoint = at.Point is { } signed ? signed.Number : 0,
            });
        }
    }

    // ── THE LOOP ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Walk them, decide what each side can know about the other, and let a sighting raise the
    /// card. Called once a frame from <c>StepSurface</c>.</summary>
    private void AdvancePatrol(double dtRealSeconds)
    {
        // #835 · …and the one clause that runs where nothing else here does. The KICKED OUT plate is painted
        // on the SURFACE, which is the one place this file has no guards, no beat and no floor — so its clock
        // is above the gate rather than behind it.
        FadeTheKickedOutPlate(dtRealSeconds);

        if (_guards.Count == 0 || _surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }

        double dt = System.Math.Min(dtRealSeconds, MaxSurfaceStepSeconds);
        _patrolFloorSeconds += dt;
        _patrolHeardAgo += dt;

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();

        // #833 · THE CARD HAS COME DOWN, SO THE WALK BEGINS. Asked here rather than wired into
        // CloseViewObject because this is the one place that runs every frame of every floor: whichever road
        // out of the card the captain took — Esc, Enter, E, the backdrop, Close — the walk starts on the
        // first frame after it, and there is no fifth road that could miss it.
        if (_escortDue is { } due && _viewObject is null)
        {
            _escortDue = null;
            BeginTheWalkBack(due, walls);
        }

        // #835 · …and the same clause for the rung above it. The ride up is armed at the car and taken here,
        // on a frame of its own: the ride empties the list of guards this method is about to walk, and a loop
        // that cleared its own collection mid-iteration is a bug looking for a rare afternoon.
        if (_kickOutRideDue && _viewObject is null)
        {
            _kickOutRideDue = false;
            TheKickOut(ex);
            return;
        }

        // #793 · DOES ANYBODY HAVE TO STOP BECAUSE THE CAPTAIN DID? Owner, on the bench: "it is a good
        // gumshoe move to see if anyone is following us by foot, as they would need to stop moving also."
        // The question is asked of Core once a frame, of every mover, and the answer today is always no —
        // a round is a published route (PatrolBeat.OnTheRound) and a published route is not a tail. This is
        // the SEAM: the hold lives on the stepper every mover already goes through, so nothing has to be
        // rebuilt the day something on this floor is actually following somebody.
        bool sitting = SeatedOnABenchInTheOpen;

        // ── #821 · IS THE CAPTAIN SHUT INTO A CUBICLE? ─────────────────────────────────────────────────
        //
        // Asked ONCE a frame and handed to everything below, so the door that is drawn shut, the door the
        // round cannot see through and the door the exposure ladder calls private are one door.
        //
        // THE SENTRY'S OWN LAW LIVES IN THE TWO BRANCHES IT OPENS, and in nothing else: a guard who watched
        // the catch go over stands outside it (WaitAtTheDoor); a guard who did not walks his beat past an
        // OCCUPIED plate without breaking stride. IT BUYS TIME, NOT SAFETY — there is no branch here in
        // which a locked door ends a challenge, and CubicleLock.OpensALockedCubicle is a constant false
        // rather than a rule, so no future edit can quietly make one.
        (RingOffice.Stall Cell, string Key)? hide = TheCubicleTheCaptainIsShutIn(ex);

        bool anythingHeard = false;
        for (int i = 0; i < _guards.Count; i++)
        {
            Guard g = _guards[i];
            g.SinceStop += dt;

            // #831 · One answer per frame about whether he is performing a cover act, written below by the
            // hold and by nothing else — a man on his round is not covering for anything.
            g.CoverPoint = 0;

            FootTail.Mover afoot = PatrolBeat.OnTheRound(i, g.X, g.Y);
            g.Held = FootTail.MustHold(sitting, _avatarX, _avatarY, in afoot, sight);
            if (!g.Held)
            {
                // #831 · The hold is over, so whatever he had decided to read is over with it. Cleared HERE,
                // off the law's own answer, rather than in each of the branches below that walk him away.
                g.CoverAt = null;
                g.CoverFor = 0;
            }
            if (ReferenceEquals(g, _escort))
            {
                // #833 · The one guard who is not walking a round at all. He is ahead of everything else in
                // this loop because an escort in progress is not a thing a bench can stop and not a thing a
                // sighting can interrupt: the captain is at his shoulder, and there is nothing left to see.
                g.Held = false;
                WalkTheEscort(g, dt, walls);
            }
            else if (hide is { } shut && CubicleLock.WaitsAtTheDoor(g.SawYouShutIt))
            {
                // #821 · He watched the catch go over. He does not open it — nothing in this game does
                // (CubicleLock.OpensALockedCubicle) — he walks over, knocks once, and waits.
                //
                // ABOVE #835's RUN, and that is the whole of what the door is worth. A man coming at a run
                // cannot run through a partition: he arrives, and then he is a man standing outside a door.
                // What he is NOT is finished — he keeps AfterYou and he keeps his reason, so opening the
                // door gives the captain back the exact rung of the ladder they ducked out of rather than a
                // softer one. IT BUYS TIME, NOT SAFETY, and this branch is where that sentence is spent.
                g.Held = false;
                WaitOutsideTheCubicle(g, dt, walls, in shut.Cell);
            }
            else if (g.AfterYou)
            {
                // #835 · The other man who is not walking a round. He is held off the bench law for the
                // escort's own reason and one of his own: #793's hold is a law about a TAIL — something
                // following you covertly, which has to stop when you stop or the trick is up. A man who has
                // said your floor and your direction into a radio is not being covert about anything.
                //
                // #821 · A run that did NOT see the catch turn carries on to where you were, which is what
                // losing somebody looks like, and #835's own cap ends it. A locked door is not a smoke bomb.
                g.Held = false;
                RunAfterTheCaptain(ex, g, dt, walls);
            }
            else if (hide is not null && g.WalkingUp)
            {
                // …and a man who was crossing the floor to somebody who is now behind a door has lost them.
                //
                // #835 · NOT BOOKED AS WALKING OFF, and the distinction is exact rather than generous. This
                // branch is only ever reached by a guard who did NOT see the catch turn — the man who did is
                // two branches up, knocking — so what happened is that somebody came round a corner into an
                // empty washroom. That is the GROUND ending it, and #835's own rule is that a refusal by the
                // ground may never be booked against the man standing in front of it. The captain who ducks
                // in where he can see them does not get this branch at all; they get the knock, which is the
                // whole of what the door was ever worth.
                GiveUpTheHail(g, i, walkedAway: false);
                g.Held = false;
            }
            else if (g.Held)
            {
                // A tail that has been made cannot walk on past you. It stops where it stopped, and it drops
                // off the motion fan honestly while it does — the same clause the stand at a stop keeps.
                //
                // #831 · …and it stops AT SOMETHING. The rule is untouched; the picture is not a statue.
                TheCoverAct(g, dt, walls, sight);
            }
            else if (g.WalkingUp)
            {
                WalkUpToTheCaptain(ex, g, i, dt, walls);
            }
            else
            {
                WalkTheRound(g, dt, walls);
            }

            // WHAT THE CAPTAIN MAY KNOW. One call, one answer, used by the marker and by nothing else — so
            // a guard behind a wall is off the deck by construction rather than by a renderer's opinion.
            // #832 · …and it is now a THREE-rung answer, because the eye's edge is not a cliff: the far
            // fifth of the reach is a distant figure with no round number on it, and only past the whole
            // reach — or behind a wall — is there nothing at all.
            g.Seen = PatrolBeat.SightingFor(_avatarX, _avatarY, g.X, g.Y, sight);
            anythingHeard |= PatrolBeat.Heard(_avatarX, _avatarY, g.X, g.Y, sight);
        }

        // …and the ear, once, cooled. It is deliberately said only for somebody the captain CANNOT see: a
        // line about boots over a marker you are looking at is the picture and the sentence disagreeing.
        if (anythingHeard && _patrolHeardAgo >= HeardAgainSeconds)
        {
            _patrolHeardAgo = 0;
            ShowPulseMessage(PatrolBeat.HeardLine);
            LogAutopilotEvent(PatrolBeat.HeardLine);
        }

        // #821 · A SHUT DOOR IS NOT A DISGUISE, IT IS A WALL. Nobody new registers the captain while it is
        // over — not because the door hides them, but because there is a partition between the two of them
        // and PatrolBeat.Notices is a sightline question. Asked here, once, rather than inside the loop: a
        // hail raised on the frame the catch turned would be a man challenging a door.
        if (hide is null)
        {
            StopTheRoundIfAnybodySeesYou(sight);
        }
        else
        {
            TheRoundWalkedPast();
        }
    }

    // ── #821 · THE HIDE ───────────────────────────────────────────────────────────────────────────────

    /// <summary>#821 · Which cubicle the captain is shut into, or null. One question, off the one set of
    /// shut cells the deck itself is rebuilt from — a second opinion about which doors are over is a second
    /// answer to whether the captain is hidden at all.</summary>
    private (RingOffice.Stall Cell, string Key)? TheCubicleTheCaptainIsShutIn(SurfaceExcursion ex) =>
        CubicleAround(ex) is { Cell: { } cell, Key: { } key } && ex.CubiclesShut.Contains(key)
            ? (cell, key)
            : null;

    /// <summary>#821 · Whether the round has already been heard going past this hide. One line per shut
    /// door: it is the reward for having got in unseen, and a sentence repeated every time a man crosses the
    /// room would be a narrator rather than a beat.</summary>
    private bool _walkedPastSaid;

    /// <summary>
    /// #821 · A ROUND THAT NEVER SAW YOU, HEARD THROUGH A PARTITION.
    ///
    /// <para>Said once, and only for somebody who has actually come into the room — inside
    /// <see cref="PatrolBeat.NoticeDu"/>, which is the reach at which he WOULD have registered you had the
    /// door been open. That is the whole point of the sentence: it is the moment the lock paid, and a captain
    /// who never hears it has not learned that the plate did nothing for them.</para>
    /// </summary>
    private void TheRoundWalkedPast()
    {
        if (_walkedPastSaid)
        {
            return;
        }

        foreach (Guard g in _guards)
        {
            if (g.SawYouShutIt)
            {
                continue;
            }

            double dx = g.X - _avatarX, dy = g.Y - _avatarY;
            if ((dx * dx) + (dy * dy) > PatrolBeat.NoticeDu * PatrolBeat.NoticeDu)
            {
                continue;
            }

            _walkedPastSaid = true;
            ShowPulseMessage(CubicleLock.WalkedPastLine);
            LogAutopilotEvent(CubicleLock.WalkedPastLine);
            return;
        }
    }

    /// <summary>
    /// #821 · HE WALKS OVER, KNOCKS ONCE, AND WAITS.
    ///
    /// <para>Owner's law, word for word: <i>"A guard who SAW you duck in knocks, then waits, then the escort
    /// line is waiting when you open the door."</i> So this method has no branch that opens anything, no
    /// timer that gives up, and no road to a card — the challenge is raised by the door being OPENED
    /// (<c>Map.Cubicle.OpenTheCubicle</c>), through #833's own walk-up, face to face at arm's length.</para>
    ///
    /// <para>He walks to Core's published STEP square (<see cref="RingOffice.Stall.StepX"/>), which is a
    /// door's clearance outside the leaf — a coordinate the placer chose, never one measured here — on the
    /// same A* and the same gait his round uses, because it is the same man doing the same walk.</para>
    /// </summary>
    private void WaitOutsideTheCubicle(
        Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls, in RingOffice.Stall cell)
    {
        g.WalkingUp = false;
        g.RePlanIn -= dt;

        double dx = cell.StepX - g.X, dy = cell.StepY - g.Y;
        if ((dx * dx) + (dy * dy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu)
        {
            g.Vx = 0;
            g.Vy = 0;
            g.Route = null;
            g.Facing = System.Math.Atan2(cell.DoorY - g.Y, cell.DoorX - g.X);

            if (!g.Knocking)
            {
                g.Knocking = true;
                g.Standing = 0;
            }
            if (!g.Knocked)
            {
                g.Knocked = true;
                ShowPulseMessage(CubicleLock.KnockLine, PulseRank.Beat);
                LogAutopilotEvent(CubicleLock.KnockLine);
                ShowPulseMessage(CubicleLock.BoughtTimeLine);
                RendererInterop.PlayCue("blip");
            }
            return;
        }

        if (g.Route is not { Active: true } || g.RePlanIn <= 0)
        {
            g.RePlanIn = PatrolBeat.RePlanEverySeconds;
            AutoWalk.Attempt planned = AutoWalk.Plan(
                true, new DeckReachability.Point(g.X, g.Y),
                new DeckReachability.Point(cell.StepX, cell.StepY),
                walls, DeckPlan.AvatarRadius,
                PatrolBeat.LatticeFor(
                    new PatrolBeat.Stop(g.X, g.Y, "here"),
                    new PatrolBeat.Stop(cell.StepX, cell.StepY, "the cubicle door"),
                    MoonSurface.ExpeditionField()));

            if (planned.Route is null)
            {
                // The ground will not give him a route to a door he watched shut. He is not left crossing a
                // washroom forever: he forgets he saw anything and goes back on the round, which is the
                // mildest honest outcome and the only one this file has ever had.
                g.SawYouShutIt = false;
                g.Knocking = false;
                g.Knocked = false;
                g.Route = null;
                return;
            }
            g.Route = planned.Route;
        }

        g.Knocking = false;
        SpendTheStride(g, dt, walls);
    }

    /// <summary>
    /// One leg of the round. The route is planned with A* between two published stops and spent through the
    /// captain's own collision at a person's gait, so a guard finds doorways and slides off walls exactly as
    /// a person does. Arriving starts the STAND — the gap the whole feature is about.
    ///
    /// <para>#831 · <b>AND THE STAND IS NOW A SIGN-IN.</b> Owner: <i>"they actually in real life like have
    /// these check points they electronically sign on rounds to prove they did their round."</i> The stop he
    /// arrives at IS the square a watchclock station is signed from (Core snapped it there), so arriving
    /// turns him to face the plate and the five seconds are the act. Nothing about the timing moved.</para>
    ///
    /// <para>#831 · <b>AND THE LEG KEEPS RIGHT.</b> Owner: <i>"they should respect right side traffic, and
    /// not walk in the middle of the corridor."</i> The A* is untouched — the same search over the same
    /// field — and its WAYPOINT LINE is put on the walker's own side of the corridor
    /// (<see cref="PatrolBeat.KeepRight"/>) before he spends a stride of it. Once per leg, at plan time: Lab
    /// 45's frame budget never sees it.</para>
    /// </summary>
    private void WalkTheRound(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (g.Standing > 0)
        {
            g.Standing -= dt;
            g.Vx = 0;   // a stopped mover drops off a motion fan, honestly
            g.Vy = 0;
            PlanTheNextLegWhileHeStands(g, walls);
            return;
        }

        PatrolBeat.Stop target = _patrolBeat[g.Leg % _patrolBeat.Count];

        if (g.Route is not { Active: true })
        {
            var from = new DeckReachability.Point(g.X, g.Y);
            var to = new DeckReachability.Point(target.X, target.Y);

            // #858 · …and most of the time this costs nothing, because the stand he has just finished spent
            // its three hundred frames doing it (PlanTheNextLegWhileHeStands). Finish() completes whatever
            // is left — everything, when there was no stand, or when what he stood planning was a walk he is
            // no longer making — so the man always leaves the stop with a route in his hand.
            AutoWalk.Planner plan = g.Planning is { } ahead && ahead.PlannedFor(from, to)
                ? ahead
                : PlanTheLeg(g, target, walls);
            g.Planning = null;
            AutoWalk.Attempt planned = plan.Finish();

            if (planned.Route is null)
            {
                // Nothing connects. The audit says this cannot happen on a floor this generator builds
                // (§13.1) and a guard is not the place to find out otherwise — so the round simply drops
                // the stop and carries on rather than standing in a corridor forever.
                g.Retries = 0;
                g.Leg = (g.Leg + 1) % _patrolBeat.Count;
                return;
            }

            // #831 · THE LANE, laid on the line he is about to walk. The route the A* proved is the route he
            // takes; what this decides is where along the width of the corridor it runs — and it hands the
            // offset back at every corner, doorway and rib mouth, because a lane is a preference and a
            // preference that could wedge a man against a jamb would be a wall.
            //
            // …AND ON A RE-PLAN HE TAKES THE MIDDLE. `Retries` is above zero only because the last line he
            // was handed did not walk, and the first thing a preference does when it stops working is stop
            // being applied. KeepRight now proves the ground under every hop before it offsets one, so this
            // is the belt under the braces rather than the fix — but it is the clause that means no future
            // lane can ever be the reason a man stops getting anywhere, which is the difference between a
            // preference and a wall.
            g.Route = AutoWalk.Along(g.Retries == 0
                ? PatrolBeat.KeepRight(planned.Route.Route, walls, DeckPlan.AvatarRadius)
                : planned.Route.Route);
        }

        SpendTheStride(g, dt, walls);

        double gx = target.X - g.X, gy = target.Y - g.Y;
        bool there = (gx * gx) + (gy * gy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu;
        if (there)
        {
            g.Route = null;
            g.Retries = 0;
            g.Standing = PatrolBeat.StandSeconds;   // THE GAP the whole feature is about

            // #831 · …and the gap has a body now. He turns to the plate on the wall and signs it. The
            // facing comes off Core's own fixture rather than being worked out here, so the man and the
            // thing he is looking at can never be two different answers.
            if (target.Point is { } station)
            {
                // From where he ACTUALLY ended up, not from the square Core measured the fixture off:
                // AtTheStopDu is a tolerance and a man a stride short of the plate is still looking at it.
                g.Facing = System.Math.Atan2(station.Y - g.Y, station.X - g.X);
                g.SignedPoint = station.Number;
            }

            g.Leg = (g.Leg + 1) % _patrolBeat.Count;
            return;
        }

        // #832 · A SNAG IS NOT AN ARRIVAL. The old clause charged a full stand and skipped the stop the
        // moment the route went inactive for any reason, which meant a body grazing a jamb halfway down a
        // corridor stood for five seconds and then walked somewhere else entirely — a round that was mostly
        // standing, in places that are not stops, and an unlearnable one for a captain trying to time it.
        // Standing is earned by ARRIVING; a refused step only costs the plan, so the leg is taken again from
        // wherever the body actually ended up. Bounded, because a stop that genuinely cannot be reached must
        // not be ground at forever (§13.1's audit says a floor this generator builds has no such stop, and a
        // guard is not the place to find out otherwise).
        if (g.Route is not { Active: true })
        {
            g.Route = null;
            if (++g.Retries > PatrolBeat.RePlansPerLeg)
            {
                g.Retries = 0;
                g.Leg = (g.Leg + 1) % _patrolBeat.Count;
            }
        }
    }

    /// <summary>
    /// #858 · THE PLAN, MADE WHILE HE IS STANDING THERE ANYWAY.
    ///
    /// <para>Lab 45's only frame-eating measurement: the A* this round asks for costs a median 1.6–2.2 ms
    /// and a worst <b>6.4 ms — 38.6% of a 60 fps frame</b>, spent whole on the frame a guard leaves a stop,
    /// about twice a minute per guard, and that is native on a desk machine while this game ships to WASM.
    /// It lands on the frame the player is most likely to be looking straight at him.</para>
    ///
    /// <para>He stands at a stop for <see cref="PatrolBeat.StandSeconds"/> — five seconds, ~300 frames in
    /// which he does not move and already knows which stop is next. So the same search runs then, a slice at
    /// a time (<see cref="PatrolBeat.PlanCellsAFrame"/>), and the departure frame usually finds it done.</para>
    ///
    /// <para><b>NOTHING HE DOES CHANGES.</b> The route is the same route — <c>AutoWalk.Planner</c> is
    /// <c>AutoWalk.Plan</c>'s own search, paused, from the same spot to the same stop over the same walls,
    /// and he cannot move while he is standing — and the stand is the same five seconds, because the slicing
    /// is not allowed to end it or extend it. If the errand changes under him, the plan is simply not the
    /// plan for the walk he makes, and the departure frame pays the old bill exactly as it always did.</para>
    /// </summary>
    private void PlanTheNextLegWhileHeStands(Guard g, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (_patrolBeat.Count == 0)
        {
            return;
        }

        PatrolBeat.Stop next = _patrolBeat[g.Leg % _patrolBeat.Count];
        if (g.Planning is not { } ahead
            || !ahead.PlannedFor(
                new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(next.X, next.Y)))
        {
            ahead = PlanTheLeg(g, next, walls);
            g.Planning = ahead;
        }
        ahead.Advance(PatrolBeat.PlanCellsAFrame);
    }

    /// <summary>The A* for one leg of the round, opened but not walked — the ONE place the round says what
    /// it is asking for, so the plan made during a stand and the plan made on the frame a man leaves one are
    /// the same question by construction.
    ///
    /// <para>The lattice is the LEG's box and not the floor's — Core's own answer
    /// (<see cref="PatrolBeat.LatticeFor"/>), so the audit that proves every leg walkable proves the route
    /// this actually plans. See that method for why the difference is not a micro-optimisation: a
    /// floor-sized lattice on every arrival would hitch the frame in WASM.</para></summary>
    private static AutoWalk.Planner PlanTheLeg(
        Guard g, in PatrolBeat.Stop target, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        AutoWalk.Planner.Begin(
            new DeckReachability.Point(g.X, g.Y),
            new DeckReachability.Point(target.X, target.Y),
            walls, DeckPlan.AvatarRadius,
            PatrolBeat.LatticeFor(
                new PatrolBeat.Stop(g.X, g.Y, "here"), target, MoonSurface.ExpeditionField()));

    /// <summary>
    /// #833 · ONE FRAME OF WALKING, FOR EVERY REASON ANYBODY ON THIS FLOOR WALKS. The round spends its leg
    /// through here, the walk-up crosses the corridor through here and the escort walks the captain out
    /// through here — one loop, so a man is the same man whichever errand he is on and there is exactly one
    /// place his gait can be got wrong.
    ///
    /// <para>#832 · THE EPSILON, AND WHY THE FAN WAS SILENT ALL EVENING. This loop is the captain's own
    /// sub-stepper (<c>Map.Deck.cs</c>), copied — and the copy dropped one character of it: the captain
    /// spends his budget while <c>budget &gt; 1e-9</c>, this spent it while <c>budget &gt; 0</c>. A frame's
    /// budget is never consumed to exactly zero in binary, so on nearly every frame the loop took ONE MORE
    /// sub-step of about 1e-17 du, the slide moved the body by less than the snag threshold, and the route
    /// was declared refused by the ground. The round's arrival clause then read that refusal as an ARRIVAL:
    /// five seconds of standing, and the stop skipped. Measured over a simulated minute of luna B2, a guard
    /// was moving 4% of the time and covered 7.7 du — which is why the owner's whole session read <i>"no
    /// movement — for now"</i> with a man plainly walking the corridor. The wiring was never the fault; the
    /// guard genuinely was not moving. (§13 lesson, again: a copied stepper is a copy of its bugs and of
    /// nothing else — which is why #833 has THREE callers here and no second copy.)</para>
    /// </summary>
    /// <returns>Whether the body actually moved this frame — what the fan is about to be told.</returns>
    private bool SpendTheStride(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        double budget = PatrolBeat.WalkSpeed * dt;
        double startX = g.X, startY = g.Y;

        for (int step = 0; step < AutoWalkSubStepsPerFrame && budget > 1e-9; step++)
        {
            if (g.Route is not { Active: true } route
                || !route.TryStep(g.X, g.Y, budget, out double dx, out double dy))
            {
                break;
            }

            (double nx, double ny) = SurfaceCollision.Slide(
                g.X, g.Y, dx, dy, DeckPlan.AvatarRadius, walls, SurfaceCollision.Gait.Person);

            double mx = nx - g.X, my = ny - g.Y;
            if ((mx * mx) + (my * my) < 1e-18)
            {
                route.Snag();
                break;
            }

            budget -= System.Math.Sqrt((mx * mx) + (my * my));
            g.X = nx;
            g.Y = ny;
        }

        double tx = g.X - startX, ty = g.Y - startY;
        g.Vx = dt > 0 ? tx / dt : 0;
        g.Vy = dt > 0 ? ty / dt : 0;
        if ((tx * tx) + (ty * ty) > 1e-8)
        {
            // Face the way they are actually travelling, not the way they wanted to — truer, and it is what
            // makes a round readable from the far end of a corridor.
            g.Facing = System.Math.Atan2(ty, tx);
            return true;
        }
        return false;
    }

    // ── #831 · THE COVER ACT ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #831 · A MADE TAIL DOES NOT FREEZE BARE — he finds something on a wall and reads it.
    ///
    /// <para>Owner, on the man he watched stand in the middle of a corridor: <i>"why would it just stand
    /// there if there is no inspection point etc"</i> — and the rule that generalises it: <i>"A MADE tail
    /// performs a COVER ACT instead of freezing bare: turns to the nearest wall fixture, checks a plate,
    /// reads a docket — same hold, same honest Vx=0, but the picture says 'man with business' not
    /// 'statue'."</i></para>
    ///
    /// <para><b><see cref="FootTail.MustHold"/> is untouched and stays untouched.</b> He still stops exactly
    /// where the law says he stops, and he still drops off the motion fan honestly when he is standing —
    /// #830's blob is what a standing man returns and this changes none of it. What changed is what a player
    /// SEES: a station, a shut door's sign, a poster, and a man doing the most ordinary thing in a facility
    /// in front of it.</para>
    ///
    /// <para><b>The drift.</b> Owner: <i>"Mid-corridor with nothing near: he does not hold there — he drifts
    /// the few du to the nearest fixture first, then holds."</i> A few du and no more
    /// (<see cref="PatrolBeat.CoverDriftDu"/>) and on a straight slide rather than an A*: a man crossing a
    /// corridor to a plate does not plan a route, and a plan per held frame is Lab 45's bill for nothing.
    /// With nothing at all within reach the hold is bare and HONEST — the audit counts those, because a
    /// floor that could not offer a man anything to look at is a finding rather than a fudge.</para>
    ///
    /// <para><b>The tell rides for free.</b> The nearest thing to a man who has just left a stop is the
    /// station he just signed, so a made tail signs it again — the same act at the same plate twice in one
    /// round, which is the gumshoe's confirmation. Nothing says so: no card, no line, no marker. It is
    /// watched (<see cref="PatrolBeat.DoubleSignIn"/> is the one place the sim names it, for the audit).</para>
    /// </summary>
    private void TheCoverAct(
        Guard g, double dt,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        g.Route = null;
        g.CoverFor += dt;

        // HE PICKS ONCE, on the frame the hold starts, and then it is what he is doing.
        g.CoverAt ??= PatrolBeat.CoverFor(g.X, g.Y, _patrolReadables, sight, walls);

        if (g.CoverAt is not { } thing)
        {
            // Not so much as a WALL within a few du of where the law stopped him — which on these floors is
            // nowhere, and is left standing rather than papered over precisely so a floor plan that grew such
            // a place would show up as a finding instead of as a statue.
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        // HE IS LOOKING AT IT the whole time, walking to it or standing at it. A man crossing two du to a
        // plate has already decided which plate.
        g.Facing = System.Math.Atan2(thing.Y - g.Y, thing.X - g.X);
        g.CoverPoint = thing.Point;

        if (PatrolBeat.AtTheCover(g.X, g.Y, in thing) || g.CoverFor > PatrolBeat.CoverDriftSeconds)
        {
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        // THE DRIFT, on the captain's own collision at the round's own gait — and honestly on the fan while
        // it lasts, because he is moving and a motion tracker hears movers.
        double dx = thing.X - g.X, dy = thing.Y - g.Y;
        double gap = System.Math.Sqrt((dx * dx) + (dy * dy));
        double take = System.Math.Min(PatrolBeat.WalkSpeed * dt, System.Math.Max(0, gap - PatrolBeat.CoverStandDu));

        // #832's epsilon, one stepper along: a budget is never spent to exactly zero in binary, and a slide
        // of 1e-17 du is a man who has arrived being reported to the fan as a man who is moving.
        if (take < 1e-9)
        {
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        (double nx, double ny) = SurfaceCollision.Slide(
            g.X, g.Y, dx / gap * take, dy / gap * take,
            DeckPlan.AvatarRadius, walls, SurfaceCollision.Gait.Person);

        double mx = nx - g.X, my = ny - g.Y;
        if ((mx * mx) + (my * my) < 1e-8)
        {
            // The ground will not let him any nearer — a rail, a bench, the corner of a pier. He reads it
            // from where he is, which is what a person does, and he is STILL: a man scraping along a wall by
            // a millionth of a deck unit is a body the motion fan would honestly call a mover.
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        g.X = nx;
        g.Y = ny;
        g.Vx = dt > 0 ? mx / dt : 0;
        g.Vy = dt > 0 ? my / dt : 0;
    }

    // ── #833 · THE APPROACH ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// He has said <i>hold on</i>, and now he crosses the floor to say the rest of it.
    ///
    /// <para>The same A* and the same gait as a leg of his round — it is the same man doing the same walk,
    /// with a moving destination — and the captain's controls are never touched. Three ways out, and only one
    /// of them raises a card: he ARRIVES at <see cref="PatrolBeat.CardReachDu"/>, or the captain walks out
    /// past <see cref="PatrolBeat.GivesUpBeyondDu"/>, or the floor refuses him a route to where you are
    /// standing. The last two put him back on his round with the cooldown running.</para>
    /// </summary>
    private void WalkUpToTheCaptain(
        SurfaceExcursion ex, Guard g, int index, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        g.WalkUpFor += dt;
        g.RePlanIn -= dt;

        // THE READ HAPPENS HERE AND NOWHERE ELSE. Face to face, at card distance — which is the whole of
        // #833's first half, and the reason this clause is above everything else in the method.
        if (PatrolBeat.AtCardReach(g.X, g.Y, _avatarX, _avatarY))
        {
            g.Vx = 0;
            g.Vy = 0;
            g.Route = null;
            g.Facing = System.Math.Atan2(_avatarY - g.Y, _avatarX - g.X);

            // …unless something else is already in front of the captain. He simply stands there at arm's
            // length until it comes down: a challenge behind a backdrop is a challenge nobody read (#777).
            if (_viewObject is null)
            {
                g.WalkingUp = false;
                TheRoundStopsAtYou(ex, g);
            }
            return;
        }

        // WALKING AWAY IS ALLOWED — and #835 did not take that away. Owner's own note on the approach: it is
        // its own tell. The FIRST one in a watch still ends exactly as it always has, with a man stopping
        // where he is and writing something short. It is doing it twice that <see cref="GiveUpTheHail"/> now
        // has an answer for, and that answer is a whole rung further up the ladder.
        if (!PatrolBeat.StillComing(g.WalkUpFor, g.X, g.Y, _avatarX, _avatarY))
        {
            GiveUpTheHail(g, index, walkedAway: true);
            return;
        }

        if (g.Route is not { Active: true } || g.RePlanIn <= 0)
        {
            g.RePlanIn = PatrolBeat.RePlanEverySeconds;
            AutoWalk.Attempt planned = AutoWalk.Plan(
                true, new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(_avatarX, _avatarY),
                walls, DeckPlan.AvatarRadius,
                PatrolBeat.LatticeFor(
                    new PatrolBeat.Stop(g.X, g.Y, "here"),
                    new PatrolBeat.Stop(_avatarX, _avatarY, "you"),
                    MoonSurface.ExpeditionField()));

            if (planned.Route is null)
            {
                // He can see you and cannot walk to you — a window, a gallery, the far side of a rail. That
                // is not a challenge, it is a man deciding it is not worth the detour. #835 · And it is NOT
                // walking away: the captain did nothing, so it may never be counted as the second time he
                // did it. The ground refused him, and the ground is not the captain's fault.
                GiveUpTheHail(g, index, walkedAway: false);
                return;
            }
            g.Route = planned.Route;
        }

        SpendTheStride(g, dt, walls);
    }

    /// <summary>
    /// #833 · He thinks better of it and goes back to work — from wherever the walk-up left him, with the
    /// cooldown running so the floor does not simply hail you again on the next frame.
    ///
    /// <para>#835 · …unless this is the second time tonight you have done it to him. The first is free and
    /// stays free (<see cref="PatrolBeat.HailsYouMayWalkAwayFrom"/>) — a man who followed you the first time
    /// would make #833's whole approach a trap rather than a decision. The second is one of the three things
    /// that earn a run, and the count is the WATCH's, not this man's: walking off on two different guards is
    /// walking off twice.</para>
    /// </summary>
    /// <param name="walkedAway">Whether the CAPTAIN ended it. False when the floor did — no route, a rail, a
    /// gallery — and a refusal by the ground may never be booked against the man standing in front of it.</param>
    private void GiveUpTheHail(Guard g, int index, bool walkedAway)
    {
        // #836 · Nobody is coming, so the fan comes down. A wallet open in front of a captain with no man
        // crossing the floor is a dialog with no clock on it — and the paper stays in the hand, because
        // deciding who you are is not undone by somebody thinking better of asking.
        _walletFanOpen = false;

        g.WalkingUp = false;
        g.WalkUpFor = 0;
        g.Route = null;
        g.Retries = 0;
        g.Vx = 0;
        g.Vy = 0;
        g.SinceStop = 0;

        if (walkedAway && PatrolBeat.WalkingOffEarnsIt(++_walkedAwayThisWatch))
        {
            TheRadioCall(g, PatrolBeat.Provocation.WalkedAwayTwice, index);
            return;
        }

        ShowPulseMessage(PatrolBeat.WalkedAwayLine);
        LogAutopilotEvent(PatrolBeat.WalkedAwayLine);
    }

    // ── THE CHALLENGE ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The first guard who registers the captain HAILS him. One at a time, and never while a card is already
    /// up: two of these on one screen would be the stacked-card mistake #777 named, and a challenge behind a
    /// backdrop is a challenge nobody read.
    ///
    /// <para>#833 · This used to raise the card itself, which is what made the inspection telepathy: a notice
    /// is registered at up to <see cref="PatrolBeat.NoticeDu"/>, and a wallet is read at arm's length. So a
    /// notice now buys the HAIL and nothing else, and the card is the walk-up's business
    /// (<see cref="WalkUpToTheCaptain"/>).</para>
    /// </summary>
    private void StopTheRoundIfAnybodySeesYou(IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        // …and never while somebody is already on their way over, or walking you out. An approach is the
        // stop, in progress; a second one behind it would be two men doing one job.
        if (_viewObject is not null || !PatrolBeat.CanBeNoticed(_patrolFloorSeconds)
            || _escort is not null || _escortDue is not null)
        {
            return;
        }

        foreach (Guard g in _guards)
        {
            if (g.WalkingUp || g.AfterYou)
            {
                return;
            }
        }

        for (int i = 0; i < _guards.Count; i++)
        {
            Guard g = _guards[i];
            if (g.SinceStop < PatrolBeat.AfterTheStopSeconds
                || !PatrolBeat.Notices(g.X, g.Y, _avatarX, _avatarY, sight))
            {
                continue;
            }

            // #835 · THE ONE PLACE A SIGHTING CAN BUY ANYTHING BUT A HAIL, and it is not the sighting that
            // buys it — it is the four lines already on the clipboard. Owner: the fiction strains when the
            // same guard books the same face four times and just keeps walking. Everything else about this
            // loop is #833's, unchanged: notice, hail, walk over, read.
            if (PatrolBeat.BookedTooOften(_escortsThisWatch))
            {
                TheRadioCall(g, PatrolBeat.Provocation.BookedTooManyTimes, i);
                return;
            }

            TheHail(g);
            return;
        }
    }

    /// <summary>
    /// #833 · THE HAIL. He turns, says the one short line, and starts walking — and that is the whole of what
    /// a notice buys. It is the second of warning that makes the approach a beat rather than an ambush: a
    /// captain with a badge gets it out, and a captain mid-mischief has a corridor's length to decide.
    ///
    /// <para>The cooldown clock starts HERE rather than at the card, so a hail that is walked away from costs
    /// the same silence as one that ends in a read — the floor does not get to keep asking.</para>
    /// </summary>
    private void TheHail(Guard g)
    {
        FanTheWallet();
        g.WalkingUp = true;
        g.WalkUpFor = 0;
        g.RePlanIn = 0;
        g.Standing = 0;
        g.Route = null;
        g.Retries = 0;
        g.SinceStop = 0;
        g.Vx = 0;
        g.Vy = 0;
        g.Facing = System.Math.Atan2(_avatarY - g.Y, _avatarX - g.X);

        ShowPulseMessage(PatrolBeat.HailLine, PulseRank.Beat);
        LogAutopilotEvent(PatrolBeat.HailLine);
        RendererInterop.PlayCue("blip");
    }

    // ── #836 · THE WALLET, FANNED DURING THE APPROACH ─────────────────────────────────────────────────
    //
    // Owner: "I think I should be able to pick the badge I show the guard... like Fletch ... suppose we have
    // 4 different ID's ... one of them real ... but not authorized for access to all places we roam."
    //
    // WHERE IT LIVES IS THE WHOLE RULING. The 2026-08-08 call — no TRY verb, the read is automatic and told
    // on a card — is untouched, because the choice happens one beat EARLIER: he has said hold on and is
    // crossing the floor, and the fan is up over the walk-up while he does it. By the time he is at arm's
    // length your hand is already in your pocket, and what is in it is what he reads.
    //
    // The controls are never taken for it. A captain may ignore the fan, or walk away from the whole thing
    // (#833/#835) — the default is seeded at the hail precisely so ignoring it is a real option rather than
    // an accident.

    /// <summary>#836 · The name on your own papers. Everything the site issued you carries it, spelled the
    /// way the rota spells it — read off the thread the captain is being played on, so a renamed captain's
    /// wallet renames itself with them.</summary>
    private string NameOnYourOwnPapers =>
        ActiveThreadInfo is { } row ? Captains.For(row).Name : Captains.Name(_activeThreadId);

    /// <summary>#836 · The papers a guard's palm is for, in the fan's own stable order. One list, read by the
    /// dialog and by the default alike.</summary>
    private IReadOnlyList<Satchel.Item> TheWalletFan =>
        _surface is { } ex ? WalletChoice.Fan(ex.Stop.Body.Id, _satchel) : [];

    /// <summary>#836 · Is the fan actually in front of the captain? Asked by the dialog that draws it AND by
    /// the cancel key that shuts it, so Esc can never swallow a keystroke for a dialog nobody can see — a
    /// wallet that lost a paper between the hail and the frame is a wallet with no choice left in it.</summary>
    private bool WalletFanIsUp => _walletFanOpen && TheWalletFan.Count > 1;

    /// <summary>
    /// #836 · THE HAIL PUTS A PAPER IN YOUR HAND, and — only when there is a choice to make — opens the fan.
    ///
    /// <para>With one paper in the wallet nothing happens that did not happen before this feature existed: no
    /// dialog, no friction, and the same paper goes into the same hand. That is the promise the
    /// <see cref="WalletChoice.Fans"/> gate keeps, and it is asked of Core so the dialog and the read cannot
    /// come to different conclusions about whether the captain had a decision.</para>
    /// </summary>
    private void FanTheWallet()
    {
        if (_surface is not { } ex)
        {
            return;
        }

        string bodyId = ex.Stop.Body.Id;
        _paperInHand = WalletChoice.DefaultFor(bodyId, _satchel, _shownBook);
        _walletFanOpen = WalletChoice.Fans(bodyId, _satchel);
    }

    /// <summary>#836 · ONE CHOICE, and picking is the whole of it. There is no confirm step — the row IS the
    /// decision, the same way the bin's row is (#798) — and the fan comes down with it, because a modal left
    /// standing over the approach would hide the man who is walking at you. Owner's own framing: <i>one
    /// choice, under time pressure, made BEFORE the read.</i></summary>
    private void ChooseThePaper(Satchel.Item paper)
    {
        _paperInHand = paper;
        _walletFanOpen = false;
        RendererInterop.PlayCue("blip");
    }

    /// <summary>#836 · Shutting the fan without touching it. Whatever was already in the hand stays in it —
    /// no arm of this dialog can leave a captain holding nothing they did not choose to hold.</summary>
    private void CloseTheWalletFan() => _walletFanOpen = false;

    /// <summary>
    /// #836 · WHAT ACTUALLY GOES INTO HIS HAND, asked at the read and not before.
    ///
    /// <para>The chosen paper, if it is still in the wallet — a pass can be taken off you between the hail and
    /// the arrival (<see cref="PatrolBeat.PassRevokedLine"/>), and a hand holding a paper the satchel no longer
    /// has would be the sim and the pocket disagreeing. Otherwise the default, which is what a captain who
    /// never opened the fan is holding anyway.</para>
    /// </summary>
    private Satchel.Item? ThePaperHandedOver(string bodyId) =>
        _paperInHand is { } chosen && WalletChoice.StillHeld(_satchel, chosen)
            ? chosen
            : WalletChoice.DefaultFor(bodyId, _satchel, _shownBook);

    /// <summary>
    /// He stops, reads what is in your wallet, and tells you the answer. The judgement is Core's and only
    /// Core's; this raises the card, spends the nerve and — when it comes to that — walks you back.
    /// </summary>
    private void TheRoundStopsAtYou(SurfaceExcursion ex, Guard g)
    {
        g.SinceStop = 0;
        g.Standing = PatrolBeat.StandSeconds;
        g.Vx = 0;
        g.Vy = 0;
        g.Facing = System.Math.Atan2(_avatarY - g.Y, _avatarX - g.X);

        // #836 · THE PAPER, AND THEN THE READ OF IT. The fan comes down here whether or not the captain ever
        // touched it — his hand is out, and the whole ruling is that there is no swapping in front of him.
        string bodyId = ex.Stop.Body.Id;
        _walletFanOpen = false;
        Satchel.Item? handed = ThePaperHandedOver(bodyId);

        PatrolBeat.Read read = PatrolBeat.TheGuardReads(bodyId, g.Plate, handed);

        // #684's idiom, one building along: the read is TOLD on a card, with the outcome in the card's own
        // amber row (#736) rather than pulsed under a backdrop nobody can see through. #804 shipped it
        // caption-only under the house's degradation law and the painting has now dropped in behind it —
        // Core's own constant, the same plate whichever way the wallet reads, because the man in it has not
        // read it yet either.
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            read.Label, PatrolBeat.ChallengeArtUrl, read.Card, read.Told);
        RendererInterop.PlayCue("reveal");

        LogAutopilotEvent($"{read.Label} — {read.Told}");

        // #836 · THE ROUND LOG REMEMBERS THE NAME. Owner: "every challenge writes down which identity you
        // showed." This is the captain's own half of that ledger, filed on BOTH arms — and the clean arm is
        // the one that had to change, because a paper that worked here is exactly the thing the next chooser
        // row has to be able to say. It is the escort note's idiom (a fact, never a mechanic), and it is the
        // ONLY thing the hint on a row is ever derived from.
        FileTheNameYouGave(bodyId, ex.Floor, handed);

        // A PASS THAT WORKS COSTS NOTHING. Encounter.NervePipsFor's own arithmetic — the band that lands is
        // free and the two that hurt cost a pip — and it has to be, or the badge is worth nothing: a captain
        // who paid the same either way would have earned a longer sentence and no mechanic.
        if (read.Satisfied)
        {
            RequestVaultSave();
            return;
        }

        ApplyNerveShock(NervePips.SightingPips * NervePips.PipUnit, "you were asked and could not answer");
        FileNote(PatrolBeat.EscortNote, "👮");

        // The mildest honest consequence, and the whole of it: back to the car — WALKED (#833). It is only
        // ARMED here, because the card telling the captain about it is standing in front of him at this exact
        // moment; the walk starts on the first frame after the card comes down, which is the frame he can
        // actually watch it happen on.
        _escortDue = g;
        RequestVaultSave();
    }

    /// <summary>
    /// #836 · THE BOOK KEEPS WHICH NAME YOU GAVE HIM — one line in the field book, and one row in the
    /// captain's own paper trail, composed from the SAME outcome the card was composed from
    /// (<see cref="WalletChoice.WhatHappens"/>).
    ///
    /// <para>That single source is the point. The sentence the captain reads on the amber row and the line
    /// their book keeps about the same thirty seconds are two readings of one fact, so no future edit can make
    /// the book remember a challenge differently from the way it was told — which on this ground is the third
    /// named bug class, in the one system whose entire register is procedure.</para>
    ///
    /// <para>An empty hand is filed too. <i>Nothing came out of the wallet</i> is a thing that happened to
    /// you, and a book that only kept the interesting nights would be a book that flattered its owner.</para>
    /// </summary>
    private void FileTheNameYouGave(string bodyId, int level, Satchel.Item? handed)
    {
        WalletChoice.Outcome how = WalletChoice.WhatHappens(bodyId, handed);
        string name = NameOnYourOwnPapers;

        if (handed is { } paper)
        {
            IReadOnlyList<WalletChoice.Shown> filed = WalletChoice.Remember(
                _shownBook, new WalletChoice.Shown(paper.Id, bodyId, level, how));
            _shownBook.Clear();
            _shownBook.AddRange(filed);

            FileNote(
                WalletChoice.ShownNote(paper, bodyId, level, how, name), WalletChoice.GlyphOf(paper));
            return;
        }

        // Nothing was handed over, so there is no paper to remember — but there is still an evening, and it
        // still gets a line. Filed against no id, which is why it never colours a chooser row.
        FileNote(WalletChoice.ShownNote(null, bodyId, level, how, name), PatrolBeat.BadgeGlyph);
    }

    // ── #833 · THE WALKED ESCORT ──────────────────────────────────────────────────────────────────────
    //
    // Owner, evening playtest 2026-08-11, escorted four times: "how did I jump to elevator there?" … "So the
    // guard walk me back to the car" … "ohhh ... they should definitely show on the motion tracker".
    //
    // What shipped was StandCaptainAt with EscortLine's prose over it: an instant placement narrated as a
    // walk, with the guard left standing wherever he was. Everything below exists to make that sentence
    // literally true, and the guards on it are about the sentence rather than about the geometry.

    /// <summary>
    /// #833 · He plans the route to the car himself and the walk begins. If the ground will not give him one
    /// — which §13.1's audit says cannot happen on a floor this generator builds — the old placement is kept,
    /// with a caption that ADMITS it is a cut. The sentence may never claim a walk the sim did not take.
    /// </summary>
    private void BeginTheWalkBack(Guard g, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        (double sx, double sy) = HiveInterior.SpawnOn(MoonSurface.ExpeditionField());

        // #835 · WHICH FLOOR THIS WALK ENDS ON, decided ONCE and here — above the route, so that even the
        // pathological cut below still ends where the card said it would. It is asked of the escorts BEFORE
        // this one, which is the same number and the same predicate the card in front of the captain was
        // composed from a moment ago (PatrolBeat.TheGuardHasYou): the man who said he was not pressing the
        // button for your floor is the man who does not press it. Two answers to one question, worked out in
        // two places, is the sentence-vs-sim bug class this feature has already paid for twice.
        _kickOutDue = PatrolBeat.BookedTooOften(_escortsThisWatch);
        _escortsThisWatch++;

        AutoWalk.Attempt planned = AutoWalk.Plan(
            true, new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(sx, sy),
            walls, DeckPlan.AvatarRadius,
            PatrolBeat.LatticeFor(
                new PatrolBeat.Stop(g.X, g.Y, "here"), new PatrolBeat.Stop(sx, sy, "the car"),
                MoonSurface.ExpeditionField()));

        if (planned.Route is null)
        {
            TheCutToTheLift(sx, sy);
            return;
        }

        // The captain's own hands come off the controls for this stretch and only this stretch — including
        // any route he had clicked, which would otherwise walk him out from under the escort.
        CancelAutoWalk(false);

        g.Route = planned.Route;
        g.Standing = 0;
        g.Retries = 0;
        g.RePlanIn = PatrolBeat.RePlanEverySeconds;
        _escort = g;
        _escortCar = (sx, sy);
        _escortSeconds = 0;
        _escortSaidPumps = false;
    }

    /// <summary>
    /// #833 · One frame of the walk back. He spends his route through the same stepper his round does, and
    /// the captain is WALKED at his shoulder — through <c>DeckPlan.Move</c>, the one primitive the captain's
    /// body is ever stepped by, so the escort obeys the same walls his own legs do and never once places him.
    ///
    /// <para><b>The tether is what makes them arrive together.</b> A guard who out-walked the man he was
    /// escorting would not be escorting anybody, so he waits when the captain falls behind
    /// (<see cref="PatrolBeat.TetherDu"/>) and the captain's legs are worked a little brisker than his
    /// (<see cref="PatrolBeat.CatchUpFactor"/>) until the gap closes.</para>
    ///
    /// <para><b>The last pace is the captain's.</b> Once the guard is standing at the car the captain keeps
    /// walking, to the car's own mouth — which is the exact square the old placement used, arrived at rather
    /// than assigned. There is no <c>StandCaptainAt</c> on this road at all.</para>
    /// </summary>
    private void WalkTheEscort(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        _escortSeconds += dt;
        g.RePlanIn -= dt;
        (double sx, double sy) = _escortCar;

        double hx = sx - g.X, hy = sy - g.Y;
        bool heIsThere = (hx * hx) + (hy * hy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu;

        double lx = _avatarX - g.X, ly = _avatarY - g.Y;
        bool waitingForYou = (lx * lx) + (ly * ly) > PatrolBeat.TetherDu * PatrolBeat.TetherDu;

        if (heIsThere || waitingForYou)
        {
            g.Vx = 0;
            g.Vy = 0;
            if (heIsThere)
            {
                g.Facing = System.Math.Atan2(ly, lx);   // at the doors, half turned back to you
            }
        }
        else
        {
            if (g.Route is not { Active: true } || g.RePlanIn <= 0)
            {
                g.RePlanIn = PatrolBeat.RePlanEverySeconds;
                AutoWalk.Attempt again = AutoWalk.Plan(
                    true, new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(sx, sy),
                    walls, DeckPlan.AvatarRadius,
                    PatrolBeat.LatticeFor(
                        new PatrolBeat.Stop(g.X, g.Y, "here"), new PatrolBeat.Stop(sx, sy, "the car"),
                        MoonSurface.ExpeditionField()));
                if (again.Route is null)
                {
                    EndTheEscort(g);
                    TheCutToTheLift(sx, sy);
                    return;
                }
                g.Route = again.Route;
            }

            SpendTheStride(g, dt, walls);
        }

        // …and the captain, walked. His target is the guard's shoulder while the guard is moving, and the
        // car's own mouth once the guard is standing at it.
        (double tx, double ty) = heIsThere ? (sx, sy) : ShoulderOf(g);
        double cdx = tx - _avatarX, cdy = ty - _avatarY;
        double want = System.Math.Sqrt((cdx * cdx) + (cdy * cdy));
        if (want > 1e-6)
        {
            double pace = System.Math.Min(want, PatrolBeat.WalkSpeed * PatrolBeat.CatchUpFactor * dt);
            (_avatarX, _avatarY) = _deckPlan.Move(_avatarX, _avatarY, cdx / want * pace, cdy / want * pace);
            _avatarHeading = System.Math.Atan2(cdy, cdx);
            RefreshAshore();
        }

        // The small talk, once, on the walk — the punishment's whole texture is a man this unbothered.
        if (!_escortSaidPumps && _escortSeconds >= PatrolBeat.PumpsAfterSeconds)
        {
            _escortSaidPumps = true;
            ShowPulseMessage(PatrolBeat.PumpsLine);
            LogAutopilotEvent(PatrolBeat.PumpsLine);
        }

        double adx = sx - _avatarX, ady = sy - _avatarY;
        if (heIsThere && (adx * adx) + (ady * ady) <= PatrolBeat.AtTheCarDu * PatrolBeat.AtTheCarDu)
        {
            bool up = _kickOutDue;
            EndTheEscort(g);

            // #835 · THE WALK THAT KEEPS GOING. Owner: "If we get kicked out then maybe we end up back to the
            // surface :-D" — and the picture is all existing geography, so this is one longer walk and no new
            // machinery. He does not go back to his round from here; he gets in with you.
            if (up)
            {
                _kickOutRideDue = true;
                return;
            }

            ShowPulseMessage(PatrolBeat.EscortDoneLine, PulseRank.Beat);
            LogAutopilotEvent(PatrolBeat.EscortDoneLine);
            return;
        }

        // The bound. A walk that has taken a minute and a half is a walk something is wrong with, and the one
        // honest way out of it is to say so rather than to keep narrating it.
        if (_escortSeconds > PatrolBeat.EscortSecondsCap)
        {
            EndTheEscort(g);
            TheCutToTheLift(sx, sy);
        }
    }

    /// <summary>
    /// #833 · A pace back and a hand's width to his left — where you walk beside somebody who is showing you
    /// out. Taken off his FACING, so the captain swings round the corners with him instead of being dragged
    /// through them.
    ///
    /// <para><b>Mostly IN HIS WAKE, and that is measured rather than styled.</b> A first cut put the captain
    /// half a pace to the side, and a doorway is not half a pace wider than a man: the target kept landing in
    /// stone, the captain slid along the jamb, the tether stretched and the guard stood waiting — an escort
    /// that stuttered its way down the corridor and was moving a THIRD of the time (titan B6: 34%, and it ran
    /// out the whole ninety-second bound without ever reaching the car). Walking where he walked is walkable
    /// by construction, because he has just walked it: the same sweep now measures 99% moving on all 22
    /// floors. Both numbers are <c>TheEscortIsAWalkTests</c>'s own.</para>
    /// </summary>
    private static (double X, double Y) ShoulderOf(Guard g)
    {
        double back = PatrolBeat.ShoulderDu, side = PatrolBeat.ShoulderDu * 0.25;
        return (g.X - (System.Math.Cos(g.Facing) * back) - (System.Math.Sin(g.Facing) * side),
                g.Y - (System.Math.Sin(g.Facing) * back) + (System.Math.Cos(g.Facing) * side));
    }

    /// <summary>#833 · The controls come back and he goes back to the round — from the car, which is where
    /// the round starts anyway, with the cooldown running so the doors are not a place you get asked twice.</summary>
    private void EndTheEscort(Guard g)
    {
        _escort = null;
        _escortSeconds = 0;
        _escortSaidPumps = false;
        g.Vx = 0;
        g.Vy = 0;
        g.Route = null;
        g.Retries = 0;
        g.SinceStop = 0;
        g.Standing = PatrolBeat.StandSeconds;
    }

    /// <summary>
    /// #833 · THE ONE HONEST JUMP-CUT. Kept for the pathological case only — a floor that will not give a
    /// guard a route to its own car — and it SAYS it is a cut. The old code did this every single time and
    /// narrated it as a walk, which is the sentence-vs-sim bug class the owner caught twice in one evening.
    /// </summary>
    private void TheCutToTheLift(double sx, double sy)
    {
        // Through the one door the sim ever puts the captain through (#681), so a cut can never end inside a
        // wall either.
        StandCaptainAt(sx, sy, "the guard walks you back to the lift");
        ShowPulseMessage(PatrolBeat.EscortCutLine, PulseRank.Beat);
        LogAutopilotEvent(PatrolBeat.EscortCutLine);

        // #835 · …and a cut may shorten the walk but it may never change where it ends. If the card said he
        // was riding up with you, he rides up with you: a jump-cut that quietly downgraded a kick-out to an
        // escort would be the sentence and the sim disagreeing about the one consequence that costs anything.
        if (_kickOutDue)
        {
            _kickOutRideDue = true;
        }
    }

    // ── WHERE THE PASS COMES FROM ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #804 · The site puts the captain on its books. Called from the ARRIVAL the day-labour chit opened
    /// (#752) — the moment the gig is not a promise any more — and once per site, because a wallet with two
    /// identical passes in it would be the pocket saying something the sim did not.
    /// </summary>
    private void IssueTheSitePass(SurfaceExcursion ex)
    {
        string bodyId = ex.Stop.Body.Id;
        if (PatrolBeat.BadgeHeld(bodyId, _satchel))
        {
            return;
        }

        Satchel.Item pass = PatrolBeat.Badge(bodyId);
        if (!Satchel.CanTake(_satchel, pass))
        {
            return;   // the wallet never fills, so this cannot happen — and a silent grant that did not land
        }             // is exactly the third named bug class, so it is asked rather than assumed (§13.9).

        _satchel = [.. Satchel.Add(_satchel, pass)];
        ShowPulseMessage(PatrolBeat.BadgeIssuedLine);
        LogAutopilotEvent(PatrolBeat.BadgeIssuedLine);
        FileNote(PatrolBeat.BadgeGist, PatrolBeat.BadgeGlyph);
    }

    // ── #835 · THE OTHER BRANCH ───────────────────────────────────────────────────────────────────────
    //
    // Owner, evening playtest 2026-08-11: "they need to catch us .... like reevers :-D we could use that code
    // :-D" … "just no damage by default :-D" … "If we get kicked out then maybe we end up back to the surface
    // :-D".
    //
    // THREE THINGS ABOUT EVERYTHING BELOW, and all three are what keep it from being a stealth level:
    //
    //   1. IT IS EARNED. The only callers of TheRadioCall are the three provocations, and every one of them
    //      is a thing the captain chose to do. A round that merely sees somebody still hails (#833).
    //   2. HE IS PROCEDURAL, NOT FERAL. The radio comes first and he stands still to say it, which is the
    //      warning; then he runs on the Old Ones' own homing step (the owner named that code) at a PERSON's
    //      gait, so he finds a doorway rather than grinding his shoulder on the jamb.
    //   3. NOTHING HERE TOUCHES THE BODY. There is no HitsTaken in this file, no swing, no roll. Being caught
    //      costs one pip of nerve and the rest of your evening; the horror stays with the Old Ones.
    //
    // AND HE CANNOT OPEN ANYTHING YOU CANNOT. The run is spent through SurfaceCollision.Slide against
    // _deckPlan.CollisionField — the captain's own walls, the same list his own legs are stepped against — so
    // a door that is shut for the captain is a wall for the man behind him, by construction rather than by a
    // clause. That is the whole of rung five's promise about a locked room: whatever the door's state is, it
    // is one state, and both of them are asking it.

    /// <summary>
    /// #835 · HE SAYS IT INTO THE RADIO, AND THEN HE COMES. The one road into a run, so a fourth trigger
    /// invented tomorrow has to come through here and be a <see cref="PatrolBeat.Provocation"/> to do it.
    ///
    /// <para>The round is suspended while it lasts and resumes from wherever he ends up, exactly as a walk-up
    /// does — a run is a longer detour, never a second state machine.</para>
    /// </summary>
    /// <param name="index">His place in the list, which fixes the hand he takes a wall on. Two men rounding a
    /// slab from opposite ends is <c>ReeverChase</c>'s own idiom and its own reason.</param>
    private void TheRadioCall(Guard g, PatrolBeat.Provocation why, int index)
    {
        if (!PatrolBeat.EarnsIt(why))
        {
            return;   // the gate, asked rather than assumed: nothing may run on Provocation.None
        }

        // #836 · …and the fan comes down here too. A man who has said your floor into a radio is not going to
        // look at a piece of paper, and a chooser left up over a run would be a decision with nothing on the
        // other end of it.
        _walletFanOpen = false;

        g.AfterYou = true;
        g.Why = why;
        g.AfterYouFor = 0;
        g.CallingIn = PatrolBeat.CallItInSeconds;
        g.WallSide = index % 2 == 0 ? 1 : -1;
        g.WalkingUp = false;
        g.WalkUpFor = 0;
        g.Standing = 0;
        g.Route = null;
        g.Retries = 0;
        g.SinceStop = 0;
        g.Vx = 0;
        g.Vy = 0;
        g.Facing = System.Math.Atan2(_avatarY - g.Y, _avatarX - g.X);

        ShowPulseMessage(PatrolBeat.CallsItInLine, PulseRank.Beat);
        LogAutopilotEvent(PatrolBeat.CallsItInLine);
        RendererInterop.PlayCue("blip");
    }

    /// <summary>
    /// #835 · ONE FRAME OF BEING COME AFTER. <c>ReeverChase.Step</c>, which is the code the owner pointed at,
    /// with the one thing a uniform changes about it: the legs are <c>Gait.Person</c>, so he goes through the
    /// doorway a shambler would grind against.
    ///
    /// <para><b>He is not planned and that is deliberate.</b> The round and the walk-up are A*, because a man
    /// doing his job takes a route; a man running after somebody does not plan, he comes at you and grazes
    /// the walls. The handrail is #324's own crude try-perpendicular and it is the whole of his cleverness —
    /// which is why a corner is worth taking and a shut door is worth being behind.</para>
    ///
    /// <para>Three ways out, and none of them removes him from the floor: he has you
    /// (<see cref="HeHasYou"/>), he loses you (<see cref="HeLosesYou"/>), or the captain rides the car and
    /// the whole floor stops being simulated — which is the escape rung five is about.</para>
    /// </summary>
    private void RunAfterTheCaptain(
        SurfaceExcursion ex, Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        g.AfterYouFor += dt;

        // THE RADIO FIRST, AND HE STANDS STILL FOR IT. This beat is the warning, and a man who talked while
        // he ran would have spent it. He is off the fan for it too, honestly — he is not travelling.
        if (g.CallingIn > 0)
        {
            g.CallingIn -= dt;
            g.Vx = 0;
            g.Vy = 0;
            g.Facing = System.Math.Atan2(_avatarY - g.Y, _avatarX - g.X);
            return;
        }

        if (PatrolBeat.HasYou(g.X, g.Y, _avatarX, _avatarY))
        {
            HeHasYou(ex, g);
            return;
        }

        if (!PatrolBeat.StillAfterYou(g.AfterYouFor, g.X, g.Y, _avatarX, _avatarY))
        {
            HeLosesYou(g);
            return;
        }

        double startX = g.X, startY = g.Y;
        (g.X, g.Y) = ReeverChase.Step(
            g.X, g.Y, _avatarX, _avatarY, PatrolBeat.AfterYouSpeed * dt,
            // No barrier. The Old Ones are penned on their side of a crew-only door; a man on the payroll is
            // already inside the building and there is nothing down here he is not allowed past. What stops
            // him is the walls, and the walls are the captain's own.
            barrierY: double.PositiveInfinity,
            walls, DeckPlan.AvatarRadius, g.WallSide, SurfaceCollision.Gait.Person);

        // …and the fan is told what actually happened, not what was asked for — #832's whole lesson. A man
        // flat against stone is a man standing still, and the instrument may say so.
        double mx = g.X - startX, my = g.Y - startY;
        g.Vx = dt > 0 ? mx / dt : 0;
        g.Vy = dt > 0 ? my / dt : 0;
        if ((mx * mx) + (my * my) > 1e-8)
        {
            g.Facing = System.Math.Atan2(my, mx);
        }
    }

    /// <summary>
    /// #835 · A HAND ON YOUR ARM — and that is the entire physical event.
    ///
    /// <para><b>Zero damage, by the owner's own ruling.</b> Nothing in this method reads or writes
    /// <c>HitsTaken</c>, swings anything or rolls anything, and the whole file is grep-able for that. One pip
    /// of nerve moves, because being run down in a corridor is frightening, and it is the TOUCH pip — the
    /// lump the model already keeps for a hand laid on you — rather than a new cause nobody ruled on.</para>
    ///
    /// <para>Then it is the ladder, and the ladder is the escort you already know: the card goes up, and the
    /// walk it names starts on the first frame after it comes down (<see cref="_escortDue"/>), which is the
    /// frame the captain can watch it happen on.</para>
    /// </summary>
    private void HeHasYou(SurfaceExcursion ex, Guard g)
    {
        // …unless something else is in front of the captain. He stands there with your arm until it comes
        // down: a catch behind a backdrop is a catch nobody read (#777).
        if (_viewObject is not null)
        {
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        PatrolBeat.Provocation why = g.Why;
        EndTheRun(g);
        g.Standing = PatrolBeat.StandSeconds;
        g.SinceStop = 0;
        g.Facing = System.Math.Atan2(_avatarY - g.Y, _avatarX - g.X);

        // The same two-armed card the wallet read is told on, with the guard's own plate behind it — one
        // picture for the whole feature, because the man in it has not decided anything yet either (#804).
        PatrolBeat.Read held = PatrolBeat.TheGuardHasYou(g.Plate, why, _escortsThisWatch);
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            held.Label, PatrolBeat.ChallengeArtUrl, held.Card, held.Told);
        RendererInterop.PlayCue("reveal");
        LogAutopilotEvent($"{held.Label} — {held.Told}");

        ApplyNerveShock(NervePips.TouchPips * NervePips.PipUnit, "a hand closed on your arm in a corridor");
        FileNote(PatrolBeat.EscortNote, "👮");

        _escortDue = g;
        RequestVaultSave();
    }

    /// <summary>#835 · HE HAS LOST YOU, AND HE IS STILL THERE. The other end of the run, and the one the
    /// captain's own legs earn: he stops, he says one more thing into the radio, and he goes back to the
    /// round from wherever he has ended up — with the cooldown running, so the floor does not simply start
    /// again on the next frame. Nothing is removed from the list; a despawn would be the building rubbing out
    /// a man who is standing in a corridor you can walk back down.</summary>
    private void HeLosesYou(Guard g)
    {
        EndTheRun(g);
        g.Standing = PatrolBeat.StandSeconds;
        g.SinceStop = 0;
        ShowPulseMessage(PatrolBeat.LostYouLine, PulseRank.Beat);
        LogAutopilotEvent(PatrolBeat.LostYouLine);
    }

    /// <summary>#835 · He stops running. One place, so the two ends of a run cannot leave different amounts
    /// of it behind on the man.</summary>
    private static void EndTheRun(Guard g)
    {
        g.AfterYou = false;
        g.AfterYouFor = 0;
        g.CallingIn = 0;
        g.Why = PatrolBeat.Provocation.None;
        g.Route = null;
        g.Retries = 0;
        g.Vx = 0;
        g.Vy = 0;
    }

    /// <summary>
    /// #835 · SOMEBODY SAW YOU DO THAT — the seam a crime comes in through, and the only one.
    ///
    /// <para>ONE crime is wired today and it is wired honestly: the gun (#803's DESIGNATE, the hasp coming
    /// off a door). It is the only thing a captain can do on these floors that this game already calls a
    /// crime — the door-forcing channels are all surface ground, and turning over a room is the floor's
    /// ordinary verb rather than something anybody has ruled on. The day a door-force lands down here it
    /// hangs off this same call and needs nothing else.</para>
    ///
    /// <para><b>SEEN, not heard.</b> <c>GunfireHeard</c> already keeps the noise ledger and deliberately does
    /// not react to it; a man who came running at a bang three corridors away would be the floor-wide hunt
    /// this feature does not have. This asks the one question <see cref="PatrolBeat.Notices"/> answers — his
    /// eye, his own short reach, over the same walls everything else on this floor sees through.</para>
    ///
    /// <para>The car's four-second grace is deliberately not asked. That grace exists so that stepping out of
    /// a lift into somebody's face is a beat rather than an instant; a gun going off is not somebody standing
    /// there being looked at, and a man who ignored it because his shift had just started would be the
    /// building refusing to notice its own doors coming apart.</para>
    /// </summary>
    private void SomebodySawThat(PatrolBeat.Provocation why)
    {
        if (!PatrolBeat.EarnsIt(why) || _guards.Count == 0 || _surface is not { Floor: < 0 })
        {
            return;
        }

        // Not while somebody is already walking you out, and not while somebody is already coming: an
        // escalation on top of an escalation is two men doing one job.
        if (_escort is not null || _escortDue is not null || _kickOutRideDue)
        {
            return;
        }

        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();
        for (int i = 0; i < _guards.Count; i++)
        {
            if (_guards[i].AfterYou)
            {
                return;
            }
        }

        for (int i = 0; i < _guards.Count; i++)
        {
            Guard g = _guards[i];
            if (!PatrolBeat.Notices(g.X, g.Y, _avatarX, _avatarY, sight))
            {
                continue;
            }
            TheRadioCall(g, why, i);
            return;
        }
    }

    // ── #835 · THE TOP RUNG: BACK TO THE SKY ──────────────────────────────────────────────────────────

    /// <summary>
    /// #835 · THE KICK-OUT. The escort has reached the car and he gets in with you.
    ///
    /// <para><b>No new machinery, one longer walk.</b> The ride is <c>RideTheLiftTo(ex, 0)</c> — the ONE
    /// transition this game has ever had between a floor and the regolith, the same one the panel's SURFACE
    /// row presses — so the captain comes out of the cage inside the shed, a pace in from its door, through
    /// the one net every placement in the excursion goes through (#681). There is no
    /// <c>StandCaptainAt</c> on this road: the walk to the car was walked (#833) and the ride is a ride.</para>
    ///
    /// <para><b>The pass goes first, and it is SAID.</b> A possession that leaves the satchel in silence is
    /// the sim doing something the prose never mentioned — the bug class this feature has paid for twice. It
    /// is only said when there was one to take: a captain who never had a pass is thrown out of a site he was
    /// never on the books of, and nothing about that needs a sentence.</para>
    ///
    /// <para><b>And the way back in is left exactly where the building already keeps it.</b> The shaft's own
    /// gate reads the wallet (#752), so a captain with nothing in it is refused in words by machinery that
    /// was already there. Nothing here has to invent a re-entry rule, and #836's wallet of names can grow one
    /// later without this method changing.</para>
    /// </summary>
    private void TheKickOut(SurfaceExcursion ex)
    {
        string bodyId = ex.Stop.Body.Id;
        bool hadOne = PatrolBeat.BadgeHeld(bodyId, _satchel);
        if (hadOne)
        {
            _satchel = [.. Satchel.Remove(_satchel, Satchel.Kind.Badge, PatrolBeat.BadgeId(bodyId))];
        }

        // The plate is armed BEFORE the ride, because the ride rebuilds the deck the plate is painted on.
        _kickedOutPlateFor = PatrolBeat.KickedOutPlateSeconds;
        RideTheLiftTo(ex, 0);

        // ONE REGION, NOT TWO PULSES — #774's law, and this moment is exactly what it is for. The ejection
        // has two things to say in one breath (the pass, and the doors) and the slot holds one line: said as
        // two calls they are two writes to it and the captain reads only the second, which would have made
        // "the removal is spoken" a sentence that was technically emitted and never seen. The quiet line goes
        // LAST because it is the closer, and it is the owner's copy verbatim.
        var said = new List<string>();
        if (hadOne)
        {
            said.Add(PatrolBeat.PassRevokedLine);
            FileNote(PatrolBeat.PassRevokedNote, PatrolBeat.BadgeGlyph);
        }
        said.Add(PatrolBeat.DoorsCloseLine);

        string closing = string.Join("\n\n", said);
        ShowPulseMessage(closing, PulseRank.Beat);
        LogAutopilotEvent(closing);
        FileNote(PatrolBeat.KickOutNote, "👮");
        RequestVaultSave();
    }

    /// <summary>
    /// #835 · THE BIG TEXT, as the tube doors part — and it is the DESCENT PLATE, not a new instrument.
    ///
    /// <para>The stack is the one <c>HiveInterior</c> paints over every car mouth in the building, in the
    /// same three sizes and the same stencil ink: the big line, the floor's name, and whether you can breathe
    /// on it. The bottom two are read off the same two functions every other plate in the game reads them off
    /// (<c>UndergroundComplex.DepthPaint</c> and <c>SuitAir.PlateLine</c>), so the sign over the shed and the
    /// gauge on the suit are physically incapable of disagreeing — and what they say is exactly what the
    /// owner's copy says they say: SURFACE, and a tank running.</para>
    ///
    /// <para>It hangs over the shed's roof, off the hut's own envelope rather than off a number typed here,
    /// and it comes down after <see cref="PatrolBeat.KickedOutPlateSeconds"/> because a sign that stayed
    /// would be #694's facility name on all thirteen floors: a thing you stop reading.</para>
    /// </summary>
    private (float X, float Y, string Text, float Px, int Tone)[]? TheKickedOutPlate(SurfaceExcursion ex)
    {
        if (_kickedOutPlateFor <= 0)
        {
            return null;
        }

        MoonSurface.LiftHeadBox shed = MoonSurface.LiftHead(
            ex.Stop.Body.Id, ex.Site.LayoutSalt, MoonSurface.ExpeditionField());
        double x = shed.CentreX, top = shed.CentreY + shed.HalfH;

        SuitAir.Supply air = SuitAir.SourceOf(ex.Stop.Body.Id, 0, insideShelter: false, aboard: false);
        return
        [
            ((float)x, (float)(top + 8.6), PatrolBeat.KickedOutBigText, 44f, 0),
            ((float)x, (float)(top + 5.8), UndergroundComplex.DepthPaint(0), 19f, 0),
            ((float)x, (float)(top + 3.4), SuitAir.PlateLine(air), 17f, SuitAir.Drawing(air) ? 2 : 1),
        ];
    }

    /// <summary>#835 · The plate's own clock, ticked where nothing else in this file runs — the surface. When
    /// it runs out ONE rebuild takes the sign down; the guard clause above it is what keeps that rebuild from
    /// being a per-frame cost on every excursion this game has.</summary>
    private void FadeTheKickedOutPlate(double dtRealSeconds)
    {
        if (_kickedOutPlateFor <= 0)
        {
            return;
        }

        _kickedOutPlateFor -= System.Math.Min(dtRealSeconds, MaxSurfaceStepSeconds);
        if (_kickedOutPlateFor <= 0)
        {
            _kickedOutPlateFor = 0;
            RebuildSurfaceDeck();
        }
    }

    // ── DRAWING THEM, AND HEARING THEM ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fill the guards into the droid buffer — and ONLY the ones the captain can actually see. An unseen
    /// round is parked off-map at the same coordinates an empty slot uses, the #371 idiom the Old Ones
    /// already take, so a guard behind a wall is not on the deck at all rather than drawn dim.
    /// </summary>
    private void FillPatrolDroids(DeckPlan.Droid[] buffer, int firstSlot)
    {
        // …and never anywhere but a floor of the Hive. The list is cleared on the way up, but the buffer is
        // shared with the ship, the havens and the derelicts, and a filler that trusted a list to have been
        // emptied is one lifted shuttle away from drawing a contract guard on the bridge.
        bool underground = _surface is { Floor: < 0 };

        for (int i = 0; i < PatrolBand; i++)
        {
            int slot = firstSlot + i;
            if (slot >= buffer.Length)
            {
                return;
            }

            // #793 · …and whether they are HELD rides along, off the same one answer the step wrote. A
            // figure that has stopped because you sat down is drawn stopped (#795's warm seated ink), and
            // the fact goes down from the sim rather than being worked out by the pen.
            // #832 · …as does whether this is a figure or a MARKER. Out at the far end of the eye's reach
            // the pen gets a silhouette to draw and no round number to write over it — the sim decides which
            // rung (PatrolBeat.SightingFor), the renderer only draws what it is handed.
            buffer[slot] = underground && i < _guards.Count
                           && _guards[i].Seen != PatrolBeat.Sighting.None
                ? new DeckPlan.Droid(
                    _guards[i].X, _guards[i].Y, _guards[i].Facing, _guards[i].DeckName, _guards[i].Held,
                    _guards[i].Seen == PatrolBeat.Sighting.Smear)
                : new DeckPlan.Droid(-9999, -9999, 0, PatrolBeat.DeckName(i));
        }
    }
}
