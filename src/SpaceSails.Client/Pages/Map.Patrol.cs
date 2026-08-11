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
/// <para><b>Nothing in this file can start a chase.</b> A sighting raises a card, the card reads the wallet,
/// and the worst outcome is a walk back to the lift. That is the owner's law, and it is enforced by there
/// being no other branch.</para>
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
    /// disagree about who is steering.</summary>
    private bool CaptainIsUnderEscort => _escort is not null;

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

        string bodyId = ex.Stop.Body.Id;
        int level = ex.Floor;
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
            else if (g.Held)
            {
                // A tail that has been made cannot walk on past you. It stops where it stopped, and it drops
                // off the motion fan honestly while it does — the same clause the stand at a stop keeps.
                g.Vx = 0;
                g.Vy = 0;
            }
            else if (g.WalkingUp)
            {
                WalkUpToTheCaptain(ex, g, dt, walls);
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
        SurfaceExcursion ex, Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
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

        // WALKING AWAY IS ALLOWED. Owner's own note on the approach: it is its own tell. Nothing follows and
        // nothing escalates — that is #835's question, and this file still has no branch that could.
        if (!PatrolBeat.StillComing(g.WalkUpFor, g.X, g.Y, _avatarX, _avatarY))
        {
            GiveUpTheHail(g);
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
                // is not a challenge, it is a man deciding it is not worth the detour.
                GiveUpTheHail(g);
                return;
            }
            g.Route = planned.Route;
        }

        SpendTheStride(g, dt, walls);
    }

    /// <summary>#833 · He thinks better of it and goes back to work — from wherever the walk-up left him,
    /// with the cooldown running so the floor does not simply hail you again on the next frame.</summary>
    private void GiveUpTheHail(Guard g)
    {
        g.WalkingUp = false;
        g.WalkUpFor = 0;
        g.Route = null;
        g.Retries = 0;
        g.Vx = 0;
        g.Vy = 0;
        g.SinceStop = 0;
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
            if (g.WalkingUp)
            {
                return;
            }
        }

        foreach (Guard g in _guards)
        {
            if (g.SinceStop < PatrolBeat.AfterTheStopSeconds
                || !PatrolBeat.Notices(g.X, g.Y, _avatarX, _avatarY, sight))
            {
                continue;
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
            EndTheEscort(g);
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
