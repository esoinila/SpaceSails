using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′d) — ONE GUARD, WALKING. The mutable class the round is made of:
// the seven things he can be doing, named; the state that says which; and (6′d) the transitions that move him
// between them. The header note lives in Map.Patrol.cs.
public sealed partial class Map
{
    /// <summary>One guard, walking. Mutable and client-side for the reason the Reevers and the sweep team
    /// are: the rules are pure in Core and the list is the client's business.</summary>
    private sealed class Guard
    {
        /// <summary>
        /// #870 lane 6′d · THE SEVEN THINGS ONE MAN CAN BE DOING, named.
        ///
        /// <para>7d turned <c>AdvancePatrol</c>'s <c>if / else if</c> chain into a list of six named arms and
        /// pinned every frame of thirteen rounds to prove the ORDER was carried across unchanged. What it could
        /// not do is give the STATES a name: the arms are methods, so "which one is he in" was a thing you
        /// worked out by reading six predicates in the right order. This is that answer as a value.</para>
        ///
        /// <para>The members are in the CHAIN's order, top to bottom, and <see cref="DoingThisFrame"/> is the
        /// chain — same tests, same order, one expression. The conductor switches on it, so the man's state and
        /// the arm he is walked through cannot be two answers.</para>
        /// </summary>
        public enum Doing
        {
            /// <summary>#833 · He is walking you out. He is not on a round at all.</summary>
            Escorting,

            /// <summary>#821 · He watched the catch go over, so he is standing outside it.</summary>
            AtTheDoor,

            /// <summary>#835 · He called it in and he is coming.</summary>
            AfterYou,

            /// <summary>#821/#835 · He was crossing the floor to somebody who is behind a door now.</summary>
            LostToADoor,

            /// <summary>#793/#831 · Made, stopped, and doing something plausible about it.</summary>
            Covering,

            /// <summary>#833 · He has said hold on, and he is crossing the floor to you.</summary>
            WalkingUp,

            /// <summary>…and if nobody wants anything of him, he walks his round.</summary>
            OnTheRound,
        }

        /// <summary>
        /// #870 lane 6′d · WHAT THIS MAN IS DOING THIS FRAME — the chain, as one expression, in the order 7d
        /// pinned and with the predicates 7d wrote.
        ///
        /// <para><b>TWO OF THE SEVEN ARE NOT HIS TO KNOW.</b> Whether he is THE escort is a fact about the
        /// round (<c>Patrol.Escort</c> holds one guard out of the list, by reference); whether the captain is
        /// shut into a cubicle is a fact about the floor, asked once a frame of the page. So this is a method
        /// taking those two bits rather than a property reading eight booleans: a property could only ever have
        /// answered four of the seven arms, which is a state machine that disagrees with its own conductor on
        /// the three cases the feature is actually about.</para>
        /// </summary>
        /// <param name="heIsTheEscort">Whether the round is holding THIS man as the one walking the captain
        /// out — <c>ReferenceEquals(g, Escort)</c>, asked by the caller because the round owns that answer.</param>
        /// <param name="youAreBehindADoor">Whether the captain is shut into a cubicle this frame. Asked ONCE a
        /// frame and handed down, so every arm below reads the same door.</param>
        public Doing DoingThisFrame(bool heIsTheEscort, bool youAreBehindADoor) =>
            heIsTheEscort ? Doing.Escorting
            : youAreBehindADoor && CubicleLock.WaitsAtTheDoor(SawYouShutIt) ? Doing.AtTheDoor
            : AfterYou ? Doing.AfterYou
            : youAreBehindADoor && WalkingUp ? Doing.LostToADoor
            : Held ? Doing.Covering
            : WalkingUp ? Doing.WalkingUp
            : Doing.OnTheRound;

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
        public int Leg { get; private set; }

        /// <summary>Seconds left standing at the stop they have reached. THE GAP a captain times.</summary>
        public double Standing { get; private set; }

        /// <summary>Seconds since this one last stopped the round at the captain. Starts far in the past so
        /// the first challenge of a floor is never held back.</summary>
        public double SinceStop { get; private set; } = PatrolBeat.AfterTheStopSeconds * 4;

        /// <summary>The A* leg they are spending, or null when the next one is due.</summary>
        public AutoWalk? Route { get; private set; }

        /// <summary>#858 · The NEXT leg, being planned a slice at a time while he stands at this one. Null
        /// whenever he is not standing at a stop.
        ///
        /// <para>Lab 45 priced the plan he used to make on the frame he left a stop at up to 6.4 ms — 38.6%
        /// of a 60 fps frame, natively, in a game that ships to WASM — and it lands on a frame the player is
        /// often watching him on. He stands for five seconds either way; this is the same work, done then.
        /// It carries the two points it was planned between (<c>AutoWalk.Planner.PlannedFor</c>), so a man
        /// whose errand changed while he stood can never be handed a route he did not ask for.</para></summary>
        public AutoWalk.Planner? Planning { get; private set; }

        /// <summary>#832 · How many times in a row this leg has been re-planned because the ground refused a
        /// step. Bounded, so a stop that genuinely cannot be reached is dropped rather than ground at
        /// forever — and reset the moment they arrive anywhere.</summary>
        public int Retries { get; private set; }

        /// <summary>#832 · What the captain can make of them on the frame just drawn — nothing, a distant
        /// figure, or the marker. Read by the droid filler, written by the step: one answer per frame, so
        /// the marker, the smear and the challenge cannot disagree about whether there is anybody
        /// there.</summary>
        public PatrolBeat.Sighting Seen { get; private set; }

        /// <summary>#793 · Whether this one is HELD — stopped because the captain sat down on a bench in the
        /// open (<see cref="FootTail.MustHold"/>). One answer per frame, written by the step and read by the
        /// filler, so the figure that has stopped and the figure DRAWN as stopped are one figure.
        ///
        /// <para>It is false for every guard in the game and will stay false: a round is a route the
        /// building published before the captain arrived, and a published route cannot be a tail. The field
        /// is here because the hold is a law about MOVERS rather than about watchers — the day something
        /// does follow the captain, it must not need a second stepper to be stopped by a bench.</para></summary>
        public bool Held { get; private set; }

        /// <summary>#833 · Whether this one has said <i>hold on</i> and is crossing the floor to you. The
        /// round is suspended while it is true and resumes from wherever he ends up, whether he arrives or
        /// gives up — a walk-up is a detour, never a new state machine.</summary>
        public bool WalkingUp { get; private set; }

        /// <summary>#833 · How long he has been walking up. Bounded by
        /// <see cref="PatrolBeat.WalkUpSeconds"/>, because a captain who keeps a pillar between you and him
        /// for twenty seconds has walked away by any honest reading.</summary>
        public double WalkUpFor { get; private set; }

        /// <summary>#833 · Seconds until the next re-plan. A walk-up and an escort chase a MOVING target (the
        /// captain, and the captain's shoulder), and an A* every frame is not free in WASM — nor is it what a
        /// man crossing a corridor does.</summary>
        public double RePlanIn { get; private set; }

        /// <summary>#835 · Whether this one has called it in and is coming at a run. False for every guard on
        /// every floor until the captain earns it, which is the whole of the ambient law.</summary>
        public bool AfterYou { get; private set; }

        /// <summary>#835 · Why he is. Carried on the man rather than on the page because it is what he SAYS
        /// when he reaches you (<see cref="PatrolBeat.WhyHeCame"/>), and a reason kept anywhere else could
        /// be a different reason by the time the card goes up.</summary>
        public PatrolBeat.Provocation Why { get; private set; }

        /// <summary>#835 · How long he has been at it. Bounded by
        /// <see cref="PatrolBeat.AfterYouSecondsCap"/> — he is a retired cop, not a wolf.</summary>
        public double AfterYouFor { get; private set; }

        /// <summary>#835 · Seconds of radio left before he moves. He stands still for this, and that beat IS
        /// the warning the run is starting.</summary>
        public double CallingIn { get; private set; }

        /// <summary>#835 · Which hand he takes a wall on when the direct run is spent — <c>ReeverChase</c>'s
        /// own stable handedness, so he rounds a corner instead of dithering at the face of it. Fixed when
        /// the run starts, per #324's reason: a side that changed frame to frame is a body that never gets
        /// anywhere.</summary>
        public int WallSide { get; private set; } = 1;

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
        public bool SawYouShutIt { get; private set; }

        /// <summary>#821 · Whether he is standing outside a shut cubicle waiting for it to open. The round is
        /// suspended while it is true and resumes from wherever he ends up — a wait is a detour, exactly as a
        /// walk-up is, and never a new state machine.</summary>
        public bool Knocking { get; private set; }

        /// <summary>#821 · Whether the two knuckles have already landed. Once per wait: a man who knocked
        /// twice would be a loop, and the whole of the line is that he does not knock again.</summary>
        public bool Knocked { get; private set; }

        /// <summary>#831 · Which watchclock station he last signed on his round, or 0 before the first one.
        /// It is what makes the DOUBLE SIGN-IN readable: a made tail's cover act at the station he signed a
        /// minute ago is the gumshoe's confirmation, and nothing anywhere says so out loud
        /// (<see cref="PatrolBeat.DoubleSignIn"/>).</summary>
        public int SignedPoint { get; private set; }

        /// <summary>#831 · Which station he is signing RIGHT NOW as a cover act, or 0 when he is not
        /// performing one. Written by the hold, read by the audit — one answer per frame, so the man who has
        /// stopped and the man DRAWN as having stopped for something are one man.</summary>
        public int CoverPoint { get; private set; }

        /// <summary>#831 · WHAT HE DECIDED TO READ, held for as long as the hold lasts. A man who re-chose
        /// the nearest fixture every frame walks toward one, gets nearer a second, turns round, and shuffles
        /// between the two forever — which is a statue with extra steps. He picks once.</summary>
        public PatrolBeat.WallThing? CoverAt { get; private set; }

        /// <summary>#831 · How long he has been getting to it. Bounded by
        /// <see cref="PatrolBeat.CoverDriftSeconds"/>: past that he reads it from where he stands.</summary>
        public double CoverFor { get; private set; }

        // ── #821 · THE DOOR ─────────────────────────────────────────────────────────────────────────────

        /// <summary>#821 · HE WAS LOOKING WHEN THE CATCH WENT OVER. The round asks
        /// <see cref="PatrolBeat.Notices"/> of every man on the frame the press turns the catch, and the ones
        /// who answer yes are told here. THE one bit the whole hide turns on, and it is set in exactly one
        /// place because it can never be re-derived: a partition goes across the opening on the very next
        /// rebuild and every answer after that would be no.</summary>
        public void HeSeesYouShutTheDoor() => SawYouShutIt = true;

        /// <summary>#821 · He has reached the cell's published step square, and standing at it is what he is
        /// doing now. The stand he was owed at a stop is spent — he is not at a stop, he is at a door.</summary>
        public void HeStandsAtTheDoor()
        {
            Knocking = true;
            Standing = 0;
        }

        /// <summary>#821 · The two knuckles land. Once per wait: a man who knocked twice would be a loop, and
        /// the whole of the line is that he does not knock again.</summary>
        public void HeKnocksOnce() => Knocked = true;

        /// <summary>#821 · …and while he is still crossing the floor to it he is not standing at it. The wait
        /// is a place, not an intention.</summary>
        public void HeIsStillWalkingToTheDoor() => Knocking = false;

        /// <summary>#821 · HE FORGETS HE SAW ANYTHING. Both roads out of the wait — the captain opens the door,
        /// or the ground will not give him a route to it — leave the same man behind, which is why the three
        /// bits come off together and in one place.</summary>
        public void HeForgetsTheCatch()
        {
            SawYouShutIt = false;
            Knocking = false;
            Knocked = false;
        }

        // ── #835 · THE CALL-IN, AND THE RUN ──────────────────────────────────────────────────────

        /// <summary>
        /// #835 · HE SAYS IT INTO THE RADIO, AND THEN HE COMES — everything the man himself becomes on the one
        /// frame a provocation lands, in one place.
        ///
        /// <para>The round is SUSPENDED rather than ended: the leg he was on, the stop he was owed and the
        /// approach he may have been walking all come off him here, and when the run is over he goes back on
        /// the round from wherever he has ended up. That is why a walk-up is cancelled here and not left to
        /// contradict the run one field along.</para>
        /// </summary>
        /// <param name="index">His place in the list, which fixes the hand he takes a wall on. Two men rounding
        /// a slab from opposite ends is <c>ReeverChase</c>'s own idiom and its own reason.</param>
        public void HeCallsItIn(PatrolBeat.Provocation why, int index)
        {
            AfterYou = true;
            Why = why;
            AfterYouFor = 0;
            CallingIn = PatrolBeat.CallItInSeconds;
            WallSide = index % 2 == 0 ? 1 : -1;
            WalkingUp = false;
            WalkUpFor = 0;
            Standing = 0;
            Route = null;
            Retries = 0;
            SinceStop = 0;
        }

        /// <summary>#835 · One more frame of being come after. Bounded by
        /// <see cref="PatrolBeat.AfterYouSecondsCap"/> — he is a retired cop, not a wolf.</summary>
        public void HeIsOneFrameFurtherIntoTheRun(double dt) => AfterYouFor += dt;

        /// <summary>#835 · …and the radio first, which he stands still for. That beat IS the warning that the
        /// run is starting.</summary>
        public void HeSpendsAFrameOnTheRadio(double dt) => CallingIn -= dt;

        /// <summary>#835 · HE STOPS RUNNING — the reason goes with it, because a reason kept past the run is a
        /// sentence waiting to be said about something that is over. One place, so the two ends of a run cannot
        /// leave different amounts of it behind on the man.</summary>
        public void HeStopsRunning()
        {
            AfterYou = false;
            AfterYouFor = 0;
            CallingIn = 0;
            Why = PatrolBeat.Provocation.None;
            Route = null;
            Retries = 0;
        }

        /// <summary>He has stopped, at you — a hand on your arm, a chase given up on, or a wallet read. The
        /// stand is the same five seconds a stop is worth, and the cooldown starts from zero so the floor does
        /// not simply ask again on the next frame.</summary>
        public void HeStandsAtYou()
        {
            Standing = PatrolBeat.StandSeconds;
            SinceStop = 0;
        }

        // ── #833 · THE WALK-UP, AND THE WALK BACK ──────────────────────────────────────────────

        /// <summary>#833 · HE SAYS HOLD ON, AND STARTS WALKING. A hail is a DETOUR from the round, never a
        /// second state machine, which is why the leg and the stand come off him here and the round picks up
        /// again from wherever the walk leaves him.</summary>
        public void HeStartsWalkingUp()
        {
            WalkingUp = true;
            WalkUpFor = 0;
            RePlanIn = 0;
            Standing = 0;
            Route = null;
            Retries = 0;
            SinceStop = 0;
        }

        /// <summary>#833 · One more frame of crossing the floor. Bounded by
        /// <see cref="PatrolBeat.WalkUpSeconds"/>, because a captain who keeps a pillar between you and him for
        /// twenty seconds has walked away by any honest reading.</summary>
        public void HeIsOneFrameFurtherIntoTheWalkUp(double dt) => WalkUpFor += dt;

        /// <summary>#833 · He is not crossing the floor any more — he is at arm's length, or he is standing at
        /// a door. The clock is deliberately NOT reset here; see <see cref="Check"/>.</summary>
        public void HeStopsWalkingUp() => WalkingUp = false;

        /// <summary>#833 · HE THINKS BETTER OF IT and goes back to work, from wherever the walk-up left him,
        /// with the cooldown running so the floor does not simply hail you again on the next frame.</summary>
        public void HeGivesUp()
        {
            WalkingUp = false;
            WalkUpFor = 0;
            Route = null;
            Retries = 0;
            SinceStop = 0;
        }

        /// <summary>#833 · A walk-up, a wait at a door and an escort all chase a MOVING target, and an A* every
        /// frame is not free in WASM — nor is it what a man crossing a corridor does. One frame nearer the next
        /// plan.</summary>
        public void HeCountsDownToARePlan(double dt) => RePlanIn -= dt;

        /// <summary>#833 · …and the clock is wound again on the frame he takes a fresh route.</summary>
        public void HeWillRePlanInAWhile() => RePlanIn = PatrolBeat.RePlanEverySeconds;

        /// <summary>#833 · HE TAKES YOU BACK TO THE CAR. The stand he was owed is spent and the leg is
        /// forgotten: he is not on a round while this lasts, and the round starts him at the car afterwards,
        /// which is where a round starts anyway.</summary>
        public void HeStartsWalkingYouOut(AutoWalk route)
        {
            Route = route;
            Standing = 0;
            Retries = 0;
            RePlanIn = PatrolBeat.RePlanEverySeconds;
        }

        // ── #793/#831 · THE HOLD, THE COVER ACT AND THE SIGN-IN ───────────────────────────────────

        /// <summary>#831 · HE IS PUT ON THE FLOOR mid-round: standing out his five seconds at the stop he was
        /// placed on, already signing its station and looking at it. The one transition a guard has before he
        /// has done anything, and it is why <c>Standing</c> and <c>Leg</c> are not written by a caller.</summary>
        public void HeStartsHisRoundAt(int leg, int signedPoint)
        {
            Leg = leg;
            Standing = PatrolBeat.StandSeconds;
            SignedPoint = signedPoint;
        }

        /// <summary>#831 · He signs the watchclock station he has arrived at. What makes the DOUBLE SIGN-IN
        /// readable later: a made tail's cover act at the station he signed a minute ago is the gumshoe's
        /// confirmation, and nothing anywhere says so out loud.</summary>
        public void HeSignsIn(int point) => SignedPoint = point;

        /// <summary>#793 · THE LAW'S OWN ANSWER ABOUT THIS MAN, ONE PER FRAME. Written by the step and read by
        /// the filler, so the figure that has stopped and the figure DRAWN as stopped are one figure. The four
        /// arms above the hold each say it of themselves too: an escort, a man at a door and a man at a run are
        /// not tails, and a law about tails may not stop them.</summary>
        public void HeIsHeld(bool held) => Held = held;

        /// <summary>#831 · One answer per frame about whether he is performing a cover act, wound back to
        /// nothing at the top of every frame and written by the hold and by nothing else — a man on his round is
        /// not covering for anything.</summary>
        public void HeIsCoveringNothingThisFrame() => CoverPoint = 0;

        /// <summary>#831 · The hold is over, so whatever he had decided to read is over with it.</summary>
        public void HeStopsCovering()
        {
            CoverAt = null;
            CoverFor = 0;
        }

        /// <summary>#831 · One more frame of getting to it. Bounded by
        /// <see cref="PatrolBeat.CoverDriftSeconds"/>: past that he reads it from where he stands.</summary>
        public void HeIsOneFrameFurtherIntoTheCover(double dt) => CoverFor += dt;

        /// <summary>#831 · HE PICKS ONCE, on the frame the hold starts, and then it is what he is doing. A man
        /// who re-chose the nearest fixture every frame walks toward one, gets nearer a second, turns round, and
        /// shuffles between the two forever — which is a statue with extra steps.</summary>
        public void HePicksSomethingToRead(PatrolBeat.WallThing? thing) => CoverAt = thing;

        /// <summary>#831 · …and this is the station he is reading RIGHT NOW as a cover act. Written by the hold,
        /// read by the audit — one answer per frame, so the man who has stopped and the man DRAWN as having
        /// stopped for something are one man.</summary>
        public void HeCovers(int at) => CoverPoint = at;

        /// <summary>#832 · WHAT THE CAPTAIN MAY MAKE OF HIM on the frame just drawn. One call, one answer, used
        /// by the marker and by nothing else — so a guard behind a wall is off the deck by construction rather
        /// than by a renderer's opinion.</summary>
        public void HeIsSeen(PatrolBeat.Sighting seen) => Seen = seen;

        // ── THE ROUND, THE LEG AND THE ROUTE ────────────────────────────────────────────────────

        /// <summary>One more frame since he last stopped the round at the captain — the cooldown, spent whatever
        /// else he is doing, because a floor that could ask twice running is a floor that asks forever.</summary>
        public void HeIsOneFrameFurtherFromTheStop(double dt) => SinceStop += dt;

        /// <summary>One more frame of THE GAP a captain times.</summary>
        public void HeSpendsAFrameStanding(double dt) => Standing -= dt;

        /// <summary>The A* he is about to spend a stride of — a leg of the round, a corridor crossed to a
        /// captain, a door walked to, or the walk back to the car. One field, four errands, one stepper.</summary>
        public void HeTakesTheRoute(AutoWalk route) => Route = route;

        /// <summary>…and the route he was on is not the route any more: he has arrived, he has been stopped, or
        /// the ground refused him a step.</summary>
        public void HeDropsHisRoute() => Route = null;

        /// <summary>#858 · The plan he made while he stood is spent, or was for a walk he is no longer
        /// making.</summary>
        public void HeForgetsThePlanAhead() => Planning = null;

        /// <summary>#858 · …and the next leg, planned a slice at a time while he stands at this one. It carries
        /// the two points it was planned between, so a man whose errand changed while he stood can never be
        /// handed a route he did not ask for.</summary>
        public void HePlansAhead(AutoWalk.Planner ahead) => Planning = ahead;

        /// <summary>#832 · A refused step costs the plan and nothing else, so the leg is taken again from
        /// wherever the body actually ended up.</summary>
        /// <returns>How many times running this leg has now been re-planned — bounded by the caller, because a
        /// stop that genuinely cannot be reached must not be ground at forever.</returns>
        public int HeTriesTheLegAgain() => ++Retries;

        /// <summary>Nothing connects, or it has been ground at long enough: the round simply drops the stop and
        /// carries on rather than standing in a corridor forever.</summary>
        public void HeDropsTheStop(int stops)
        {
            Retries = 0;
            Leg = (Leg + 1) % stops;
        }

        /// <summary>HE IS THERE. The route is spent, the re-plans are forgiven, and THE GAP the whole feature is
        /// about starts running.</summary>
        public void HeArrivesAtTheStop()
        {
            Route = null;
            Retries = 0;
            Standing = PatrolBeat.StandSeconds;
        }

        /// <summary>…and the next stop is the one he heads for when the stand is over.</summary>
        public void HeTakesTheNextLeg(int stops) => Leg = (Leg + 1) % stops;

        /// <summary>#833 · The car is reached, or the floor would not give him a route to it: the controls come
        /// back and he goes back to the round from where he stands, with the cooldown running so the doors are
        /// not a place you get asked twice.</summary>
        public void HeIsDoneWalkingYouOut()
        {
            Route = null;
            Retries = 0;
            SinceStop = 0;
            Standing = PatrolBeat.StandSeconds;
        }
    }
}
