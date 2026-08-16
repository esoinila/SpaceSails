using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′b) — ONE GUARD, WALKING. The mutable class the round is made of,
// moved off the page with the state it is: the header note lives in Map.Patrol.cs, and every field below is
// exactly the field it was, in the order it was, with the docblock it was written with.
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
}
