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
            TheOneThingHeIsDoingThisFrame(ex, g, i, dt, walls, sight, hide);

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

    // ── #870 · THE ONE THING HE IS DOING, AND THE ORDER THAT DECIDES IT ───────────────────────────────

    /// <summary>
    /// #870 · WHAT THIS MAN IS DOING THIS FRAME — exactly one of seven things, and the ORDER IS THE FEATURE.
    ///
    /// <para>Every arm below was written into one <c>if / else if</c> chain by the six issues that built this
    /// file, and the chain's order carried five separate rulings that were named nowhere. It is a list now,
    /// read top to bottom, and the first arm that will have him takes him. Each arm's own docblock says what
    /// it is; this is the priority, and WHY it is this priority:</para>
    ///
    /// <list type="number">
    /// <item><b>He is walking you out</b> (#833) — an escort in progress is not a thing a bench can stop and
    /// not a thing a sighting can interrupt. He is not on a round at all, so nothing below applies to him.</item>
    /// <item><b>He is waiting outside the door he saw you shut</b> (#821) — ABOVE the run, and that is the
    /// whole of what a locked door is worth: a man coming at a run cannot run through a partition.</item>
    /// <item><b>He is coming after you</b> (#835) — above everything a round does, because a man who has said
    /// your floor into a radio has stopped doing his rounds.</item>
    /// <item><b>He has lost you to a door</b> (#821/#835) — only ever reached by a man who did NOT watch the
    /// catch turn, because the man who did is two arms up, knocking.</item>
    /// <item><b>He is made and covering for it</b> (#793/#831) — the hold, which is asked once a frame of
    /// every mover in <see cref="AdvancePatrol"/> and spent here.</item>
    /// <item><b>He is crossing the floor to you</b> (#833) — a hail is a detour from the round, so it sits
    /// under everything that is not the round and over the round itself.</item>
    /// <item>…and if nobody wants anything of him, <b>he walks his round</b>. The default, the ordinary case,
    /// and the one the whole floor is mostly made of.</item>
    /// </list>
    ///
    /// <para><b>Nothing here decides anything.</b> Every arm keeps the guard clause it was written with, in
    /// the order it was written in; the arms return whether they took him, and this reads as the list. The
    /// #870 lane that made it a list pinned every frame of twelve rounds first
    /// (<c>EveryRoundFingerprintsTheSameTests</c>) and reproduced every hash after — including a RED run with
    /// two of these lines swapped, which is the proof that the order below is load-bearing rather than
    /// decorative.</para>
    /// </summary>
    private void TheOneThingHeIsDoingThisFrame(
        SurfaceExcursion ex, Guard g, int index, double dt,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        IReadOnlyList<SurfaceCollision.Segment> sight,
        (RingOffice.Stall Cell, string Key)? hide)
    {
        if (HeIsWalkingYouOut(g, dt, walls)) { return; }
        if (HeIsWaitingOutsideTheDoorHeSawYouShut(g, dt, walls, hide)) { return; }
        if (HeIsComingAfterYou(ex, g, dt, walls)) { return; }
        if (HeHasLostYouToADoor(g, index, hide)) { return; }
        if (HeIsMadeAndCoveringForIt(g, dt, walls, sight)) { return; }
        if (HeIsCrossingTheFloorToYou(ex, g, index, dt, walls)) { return; }

        WalkTheRound(g, dt, walls);
    }

    /// <summary>#833 · THE ESCORT. The one guard on the floor who is not walking a round, walking the captain
    /// off it at his shoulder.</summary>
    private bool HeIsWalkingYouOut(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (!ReferenceEquals(g, _escort))
        {
            return false;
        }

        // #833 · The one guard who is not walking a round at all. He is ahead of everything else in
        // this loop because an escort in progress is not a thing a bench can stop and not a thing a
        // sighting can interrupt: the captain is at his shoulder, and there is nothing left to see.
        g.Held = false;
        WalkTheEscort(g, dt, walls);
        return true;
    }

    /// <summary>#821 · THE KNOCK. He watched the catch go over, so he is standing outside it — and standing
    /// outside it is the whole of what he does.</summary>
    private bool HeIsWaitingOutsideTheDoorHeSawYouShut(
        Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls,
        (RingOffice.Stall Cell, string Key)? hide)
    {
        if (hide is not { } shut || !CubicleLock.WaitsAtTheDoor(g.SawYouShutIt))
        {
            return false;
        }

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
        return true;
    }

    /// <summary>#835 · THE RUN. He has called it in and he is coming — the earned branch, and never the
    /// ambient one.</summary>
    private bool HeIsComingAfterYou(
        SurfaceExcursion ex, Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (!g.AfterYou)
        {
            return false;
        }

        // #835 · The other man who is not walking a round. He is held off the bench law for the
        // escort's own reason and one of his own: #793's hold is a law about a TAIL — something
        // following you covertly, which has to stop when you stop or the trick is up. A man who has
        // said your floor and your direction into a radio is not being covert about anything.
        //
        // #821 · A run that did NOT see the catch turn carries on to where you were, which is what
        // losing somebody looks like, and #835's own cap ends it. A locked door is not a smoke bomb.
        g.Held = false;
        RunAfterTheCaptain(ex, g, dt, walls);
        return true;
    }

    /// <summary>#821 · THE EMPTY WASHROOM. He was crossing the floor to somebody who is behind a door now,
    /// and that is the ground ending his hail rather than the captain doing it.</summary>
    private bool HeHasLostYouToADoor(Guard g, int index, (RingOffice.Stall Cell, string Key)? hide)
    {
        if (hide is null || !g.WalkingUp)
        {
            return false;
        }

        // …and a man who was crossing the floor to somebody who is now behind a door has lost them.
        //
        // #835 · NOT BOOKED AS WALKING OFF, and the distinction is exact rather than generous. This
        // branch is only ever reached by a guard who did NOT see the catch turn — the man who did is
        // two branches up, knocking — so what happened is that somebody came round a corner into an
        // empty washroom. That is the GROUND ending it, and #835's own rule is that a refusal by the
        // ground may never be booked against the man standing in front of it. The captain who ducks
        // in where he can see them does not get this branch at all; they get the knock, which is the
        // whole of what the door was ever worth.
        GiveUpTheHail(g, index, walkedAway: false);
        g.Held = false;
        return true;
    }

    /// <summary>#793/#831 · THE COVER ACT. A made tail that has been stopped by a captain sitting down, doing
    /// something plausible about it.</summary>
    private bool HeIsMadeAndCoveringForIt(
        Guard g, double dt,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        if (!g.Held)
        {
            return false;
        }

        // A tail that has been made cannot walk on past you. It stops where it stopped, and it drops
        // off the motion fan honestly while it does — the same clause the stand at a stop keeps.
        //
        // #831 · …and it stops AT SOMETHING. The rule is untouched; the picture is not a statue.
        TheCoverAct(g, dt, walls, sight);
        return true;
    }

    /// <summary>#833 · THE APPROACH. He has said hold on, and he is walking over to say the rest of it to
    /// your face.</summary>
    private bool HeIsCrossingTheFloorToYou(
        SurfaceExcursion ex, Guard g, int index, double dt,
        IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (!g.WalkingUp)
        {
            return false;
        }

        WalkUpToTheCaptain(ex, g, index, dt, walls);
        return true;
    }
}
