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
    }

    private readonly List<Guard> _guards = [];

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

        bool anythingHeard = false;
        for (int i = 0; i < _guards.Count; i++)
        {
            Guard g = _guards[i];
            g.SinceStop += dt;

            FootTail.Mover afoot = PatrolBeat.OnTheRound(i, g.X, g.Y);
            g.Held = FootTail.MustHold(sitting, _avatarX, _avatarY, in afoot, sight);
            if (ReferenceEquals(g, _escort))
            {
                // #833 · The one guard who is not walking a round at all. He is ahead of everything else in
                // this loop because an escort in progress is not a thing a bench can stop and not a thing a
                // sighting can interrupt: the captain is at his shoulder, and there is nothing left to see.
                g.Held = false;
                WalkTheEscort(g, dt, walls);
            }
            else if (g.AfterYou)
            {
                // #835 · The other man who is not walking a round. He is held off the bench law for the
                // escort's own reason and one of his own: #793's hold is a law about a TAIL — something
                // following you covertly, which has to stop when you stop or the trick is up. A man who has
                // said your floor and your direction into a radio is not being covert about anything.
                g.Held = false;
                RunAfterTheCaptain(ex, g, dt, walls);
            }
            else if (g.Held)
            {
                // A tail that has been made cannot walk on past you. It stops where it stopped, and it drops
                // off the motion fan honestly while it does — the same clause the stand at a stop keeps.
                g.Vx = 0;
                g.Vy = 0;
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

        StopTheRoundIfAnybodySeesYou(sight);
    }

    /// <summary>
    /// One leg of the round. The route is planned with A* between two published stops and spent through the
    /// captain's own collision at a person's gait, so a guard finds doorways and slides off walls exactly as
    /// a person does. Arriving starts the STAND — the gap the whole feature is about.
    /// </summary>
    private void WalkTheRound(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (g.Standing > 0)
        {
            g.Standing -= dt;
            g.Vx = 0;   // a stopped mover drops off a motion fan, honestly
            g.Vy = 0;
            return;
        }

        PatrolBeat.Stop target = _patrolBeat[g.Leg % _patrolBeat.Count];

        if (g.Route is not { Active: true })
        {
            var from = new DeckReachability.Point(g.X, g.Y);
            var to = new DeckReachability.Point(target.X, target.Y);

            // The lattice is the LEG's box and not the floor's — Core's own answer, so the audit that
            // proves every leg walkable proves the route this actually plans. See PatrolBeat.LatticeFor for
            // why the difference is not a micro-optimisation: a floor-sized lattice on every arrival would
            // hitch the frame in WASM.
            AutoWalk.Attempt planned = AutoWalk.Plan(
                true, from, to, walls, DeckPlan.AvatarRadius,
                PatrolBeat.LatticeFor(
                    new PatrolBeat.Stop(g.X, g.Y, "here"), target, MoonSurface.ExpeditionField()));

            if (planned.Route is null)
            {
                // Nothing connects. The audit says this cannot happen on a floor this generator builds
                // (§13.1) and a guard is not the place to find out otherwise — so the round simply drops
                // the stop and carries on rather than standing in a corridor forever.
                g.Retries = 0;
                g.Leg = (g.Leg + 1) % _patrolBeat.Count;
                return;
            }
            g.Route = planned.Route;
        }

        SpendTheStride(g, dt, walls);

        double gx = target.X - g.X, gy = target.Y - g.Y;
        bool there = (gx * gx) + (gy * gy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu;
        if (there)
        {
            g.Route = null;
            g.Retries = 0;
            g.Standing = PatrolBeat.StandSeconds;   // THE GAP the whole feature is about
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

        PatrolBeat.Read read = PatrolBeat.TheGuardReads(ex.Stop.Body.Id, g.Plate, _satchel);

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

        // A PASS THAT WORKS COSTS NOTHING. Encounter.NervePipsFor's own arithmetic — the band that lands is
        // free and the two that hurt cost a pip — and it has to be, or the badge is worth nothing: a captain
        // who paid the same either way would have earned a longer sentence and no mechanic. It files nothing
        // either, because a notebook full of manners is a notebook nobody reads.
        if (read.Satisfied)
        {
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
