using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #251 · Split from Guard.cs, moved verbatim (the header note lives in Map.Patrol.cs): (6′d) the named
// transitions that move one guard between his postures — the door, the call-in and the run, the walk-up
// and the walk back, the hold and the cover, the round and the leg — and the check that no two coexist.
// Every field and property of the guard stays in Guard.cs.
public sealed partial class Map
{
    private sealed partial class Guard
    {
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

        /// <summary>
        /// #833 · He is not crossing the floor any more — he is at arm's length with a card coming up, or he is
        /// standing at a door.
        ///
        /// <para>#920 · AND THE CLOCK STOPS WITH HIM. This was the one road out of the walk-up that left
        /// <see cref="WalkUpFor"/> running: the other two zero it (<see cref="HeGivesUp"/> when he thinks better
        /// of it, <see cref="HeCallsItIn"/> when the walk-up becomes a run), and this one — the road THROUGH the
        /// card, which is the road the feature is for — did not. #906's sweep counted <b>1,972</b> guard-frames
        /// of thirteen rounds carrying a walk-up clock on a man who had stopped walking up. Nothing read it
        /// while it was stale — only the walk-up reads it, and <see cref="HeStartsWalkingUp"/> zeroes it — so
        /// not one frame of the round is walked differently for this line; but it is not free, because #906's
        /// transcript writes down every field of a guard and two of the thirteen digests moved on those 1,972
        /// lines and on nothing else. That trade is worth making: a spent clock that reads as a running one is
        /// a field waiting for the first caller who asks it an honest question, and <see cref="Check"/> now
        /// says it can never happen.</para>
        /// </summary>
        public void HeStopsWalkingUp()
        {
            WalkingUp = false;
            WalkUpFor = 0;
        }

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

        // ── #870 lane 6′d · THE POSTURES THAT CANNOT COEXIST ─────────────────────────────────────

        /// <summary>
        /// #870 lane 6′d · IS THIS ONE MAN? Eight pairs that may never be true together (#920 added the
        /// eighth), asked of every guard after every frame of every case in
        /// <c>EveryRoundFingerprintsTheSameTests</c> — 13 cases, 7,100 frames — and never once false.
        ///
        /// <para>Each is a pair of postures, not a tidy-up: <c>CallingIn</c> above zero on a man who is not
        /// coming is a radio call for a run that is over; a <c>CoverAt</c> on a man who is not held is a
        /// fixture he decided to read on a frame he was walking away; a man <c>Knocking</c> who never watched
        /// the catch turn is the hide working for somebody it was never meant to work for. Every one of them
        /// would have been reachable while these were 28 public fields and eleven verbs wrote them.</para>
        ///
        /// <h3>AND THE TWO THINGS THAT DO COEXIST, WHICH ARE NOT ON THIS LIST AND WHY</h3>
        ///
        /// <list type="bullet">
        /// <item><b><c>Held</c> with <c>WalkingUp</c></b> — a walk-up that a bench has stopped. The hold arm sits
        /// ABOVE the walk-up arm precisely so a made tail stops crossing the floor without forgetting that it
        /// was; the approach resumes when the captain stands up. Suspended, not contradicted.</item>
        /// <item><b><c>Standing</c> above zero with a live <c>Route</c></b> — a man who was standing out his five
        /// seconds at a stop when something took him off the round. The stand is REMEMBERED across the detour,
        /// which is the same law: a detour is never a new state machine.
        /// <para>#920 looked at whether this one is a law too, because #906's sweep observes it ZERO times, and
        /// it is not: it is reachable by construction. A man arrives at a stop
        /// (<see cref="HeArrivesAtTheStop"/> gives him five seconds and no route), the captain shuts a cubicle
        /// on him while he stands, and the wait at the door hands him a route to that door
        /// (<c>Patrol.WaitOutsideTheCubicle</c>) on a frame where nothing has touched the stand — <c>Standing</c>
        /// is spent only by <see cref="HeSpendsAFrameStanding"/>, which is the ROUND's arm and is not the arm he
        /// is in. He is a man with five seconds owed him and somewhere to be. Asserting it would be an invariant
        /// nothing has ever walked, which is a clause waiting to go red on somebody else's afternoon; the tally
        /// in <c>TheTwoThatCoexistAndTheOneThatIsNowALaw</c> is where a case that reaches it would show
        /// up.</para></item>
        /// </list>
        ///
        /// <para><b>#920 · AND THE THIRD IS NOW ONE OF THE EIGHT.</b> <c>WalkUpFor</c> above zero with
        /// <c>WalkingUp</c> false — a spent clock — used to be the third thing on this list, tallied at 1,972
        /// frames and filed rather than fixed. <see cref="HeStopsWalkingUp"/> zeroes it now, so the clock stops
        /// when the walk-up does on all three roads out, and the pair is a law instead of a number.</para>
        /// </summary>
        /// <returns>The first contradiction, in a sentence, or <c>null</c> when he is one man.</returns>
        public string? Check() =>
            AfterYou && WalkingUp
                ? "he is coming at a run AND crossing the floor to say hold on"
            : Knocking && !SawYouShutIt
                ? "he is knocking at a door he never watched anybody shut"
            : Knocking && WalkingUp
                ? "he is standing at a door AND crossing the floor to somebody"
            : CoverPoint != 0 && !Held
                ? $"he is signing station {CoverPoint} as a cover act and nothing is holding him"
            : CoverAt is not null && !Held
                ? "he has picked something to read as a cover act and nothing is holding him"
            : CallingIn > 0 && !AfterYou
                ? "he has radio left to say and there is no run to say it for"
            : Why != PatrolBeat.Provocation.None && !AfterYou
                ? $"he is carrying a reason ({Why}) and he is not coming after anybody"
            // #920 · The eighth. A walk-up clock still running on a man who is not walking up: unread while
            // stale, and a field that reads as a running clock when the walk is over is the fifth bug class
            // sitting quietly, waiting for the first caller who asks it an honest question.
            : WalkUpFor > 0 && !WalkingUp
                ? $"he has been walking up for {WalkUpFor:0.###}s and he is not walking up"
            : null;
    }
}
