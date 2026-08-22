using System;
using System.Collections.Generic;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #731 · THE EXIT IS THE FULL STOP — one walker, for the people who are not the captain and not the
/// Old Ones.
///
/// <para><b>Owner, 2026-08-06, streamed during the smoke run:</b> <i>"The NPCs but not reevers could also
/// use the A* if we want to show them leaving a scene etc. If they go behind a door that is locked to us,
/// we use that as 'I guess that concludes the conversation' point in the plot / situation."</i> And the
/// limitation it fixes, in his own words: <i>"Like on the bar now they have to wait for us to leave before
/// they can sit up… or leave the bar."</i></para>
///
/// <para><b>And the same day, the other direction:</b> <i>"In the space bars there are lot of cases where we
/// can have npcs arrive at bar from locked place or go to a locked place. Now it is possible to have NPC ask
/// to sit down at our table and offer a quest! This is the classic TTRPG event."</i> So this class carries
/// BOTH marks of punctuation. The route out is a full stop. The route in is a cold open.</para>
///
/// <h3>What it is not</h3>
///
/// <para>It is not a second mover. Everything below spends <see cref="AutoWalk"/> — the route the captain's
/// own click plans — through <see cref="SurfaceCollision.Slide"/>, the one primitive the captain's boots,
/// the guard's round and the Old Ones' shamble all already obey. There is no wall list in here, no copied
/// radius, and no step this class takes that the collision has not already agreed to. A person crossing a
/// room the captain is standing in must be walking on the same floor the captain is standing on, or the
/// whole beat is a cartoon played over the top of the simulation.</para>
///
/// <h3>The four laws</h3>
///
/// <list type="number">
/// <item><b>NEVER REEVERS.</b> Owner's ruling, twice: <i>"let's not make them walk too sensibly… they have
/// their own reever-mind-issues, the kind of way they walk is nice and spooky now"</i>, and on #724
/// <i>"Lets not help reevers move in any easier if possible."</i> An Old One that could find a doorway would
/// stop being the thing you shed on an obstacle when you are out of rounds. So the refusal is written into
/// the CONSTRUCTOR rather than into a review checklist: hand <see cref="Plan"/> any gait but
/// <see cref="SurfaceCollision.Gait.Person"/> and it throws. There is no path through this file that walks a
/// stagger.</item>
///
/// <item><b>Unhurried.</b> <see cref="PaceDu"/> is slower than the guard's parade and much slower than the
/// captain's stride, because a regular finishing a drink is not going anywhere in particular and a body that
/// crosses a hall at a captain's pace reads as a chase.</item>
///
/// <item><b>Blocked by the captain → it stops, waits, and looks at you.</b> The owner's own proposal on the
/// issue, and it is content rather than a failure mode: standing in the doorway of a room somebody is trying
/// to leave is a thing a player can do on purpose, and the answer must never be that they walk through you.
/// The captain is not a wall — the collision has never known about the body of the avatar — so the yield is
/// this class's own, and it is a REFUSAL TO STEP rather than a push: the route stays live, the walk resumes
/// the moment the doorway clears, and no frame ever ends with the two of them inside one another.
///
/// <para><b>Unless the captain is the errand.</b> Somebody who has crossed a room to sit down at your table
/// is not obstructed by you being at it, and the first build of this deadlocked every arrival on its last
/// stride for exactly that reason. So the berth is the caller's to set — see
/// <see cref="NoPersonalSpace"/>.</para></item>
///
/// <item><b>The ground has the last word.</b> If the stepper refuses a move the plan expected to make, the
/// walk <see cref="AutoWalk.Snag"/>s and stops, exactly as the captain's own does. A body grinding against a
/// wall forever is a bug wearing a feature's clothes whoever is wearing the boots.</item>
/// </list>
///
/// <para>Pure and world-blind: it holds a position, a route and a name, and it is handed the walls and the
/// captain on every step. Nothing in here reads a clock, and nothing in here knows what a bar is.</para>
/// </summary>
public sealed class NpcWalk
{
    /// <summary>How fast a person crosses a room when nothing is wrong, in deck units per second.
    ///
    /// <para>Argued rather than tuned, and argued DOWNWARD from the two paces this game already walks. The
    /// captain's stride is 9.0 du/s and a guard's parade is <c>PatrolBeat.WalkSpeed</c> = 3.2; both are
    /// somebody going somewhere for a reason. A regular who has finished a drink is not, and the whole read
    /// of the beat depends on it: the walk is the ceremony, and a body that crosses the hall at a guard's
    /// clip reads as an errand rather than as a room with a metabolism. Two du a second is a little under a
    /// slow walking pace on ground where a du is about a metre — long enough that the captain notices
    /// somebody has stood up, short enough that the hall's longest leg is spent in well under a minute.</para>
    /// </summary>
    public const double PaceDu = 2.0;

    /// <summary>The most sub-steps one frame may spend, matching the captain's own click-walk and the
    /// guard's round. Named here so a walker's worst frame is the same shape as theirs rather than a number
    /// of its own.</summary>
    public const int SubStepsPerFrame = 8;

    /// <summary>How near the captain a walker will come before it stops, as a multiple of a body's radius.
    /// TWO — one body-width — which is the distance at which two people of that radius are touching, and
    /// therefore the smallest gap at which nobody has clipped anybody. Larger would be a walker that flinches
    /// away from a captain standing harmlessly beside its route; smaller is an overlap.</summary>
    public const double PersonalSpaceInRadii = 2.0;

    /// <summary>…and NONE, for the walk whose whole errand is the captain.
    ///
    /// <para>The yield above is a rule about being IN THE WAY, and it is right for the beat it was written
    /// for: a regular crossing a hall to a staff door has no business with the captain, so a captain standing
    /// in that door is an obstruction and being looked at is the content. <b>Somebody who has crossed the room
    /// to sit down at your table is the opposite case.</b> They are not blocked by you; they have come for
    /// you, and a body that stops a metre short of the chair it is walking to and stares forever is not
    /// politeness, it is a deadlock — which is exactly what the first build of this did, at 1.45 du, on the
    /// last stride of every arrival, because the captain was sitting in the next chair.</para>
    ///
    /// <para>So a walk that begins or ends at the captain's own table is planned with this instead. Nothing
    /// else in the game yields to the avatar either — the collision has never known about the captain's body,
    /// and a captain may already stand on a seated regular — so this is the ordinary law, and
    /// <see cref="PersonalSpaceInRadii"/> is the exception the doorway beat earns.</para></summary>
    public const double NoPersonalSpace = 0.0;

    /// <summary>Below this, a step is nothing and a frame's budget is spent. The guard's round pays for this
    /// number in its own comment (#832 — a motion tracker went silent for an evening on <c>&gt; 0</c>), and
    /// it is spelled the same way here on purpose.</summary>
    private const double NoBudgetLeft = 1e-9;

    /// <summary>Squared deck units below which a frame's whole movement counts as none at all — the same
    /// threshold the guard's stride uses to decide his facing did not change.</summary>
    private const double StoodStillSq = 1e-18;

    /// <summary>What a walker is doing this frame. Read by the caller to know when to take the figure off
    /// the deck (the door clicked) or to raise whatever the arrival was for.</summary>
    public enum Doing
    {
        /// <summary>On its way, and it moved this frame.</summary>
        Walking,

        /// <summary>Stopped, because the captain is in the way. The route is still live; it is looking at
        /// you.</summary>
        Waiting,

        /// <summary>It reached the end of its route. For a departure that is the door, and the door clicking
        /// is the scene's full stop; for an arrival that is the chair.</summary>
        Arrived,

        /// <summary>The ground refused a move the plan expected. It stopped rather than grinding.</summary>
        Snagged,
    }

    /// <summary>Where a walker is going, and the door this walk is about — carried as the PLATE the building
    /// painted on it rather than as a flag somebody set.
    ///
    /// <para>That distinction is the whole of law two on #731. "This door refuses the captain" written as a
    /// <c>bool</c> beside the walker is a field a test can type in and a world that cannot tell pass from
    /// fail (this repo's fifth named bug class). Written as the door's own sign, the claim is checkable
    /// against the same authority the captain's TRY asks — and <see cref="Egress"/>, which is the only thing
    /// that builds one of these for a departure, will not accept anything but a
    /// <see cref="UndergroundComplex.LockedDoor"/>, so a public door cannot be handed to it at all.</para>
    /// </summary>
    /// <para><b>A walk's door is not always the place it ends.</b> A departure ends AT one; an arrival ends at
    /// a chair and carries the plate of the one it CAME OUT OF, because that plate is the whole cold open —
    /// the captain is meant to look at the person opposite them and, if they were watching, know which door
    /// in this building opened for her. So the sign is the door this walk is ABOUT, and the coordinates are
    /// where it ends; a walk with no door in it at all (none ships today) simply has neither.</para>
    /// </summary>
    /// <param name="Sign">The plate, verbatim — the door this walk is about, whichever end of it that is.
    /// Empty only for a walk with no door in its story.</param>
    /// <param name="X">Where the walk ENDS: a standing place in front of a leaf, or a chair. Never the leaf
    /// itself, which is stone.</param>
    public readonly record struct Bound(string Sign, double X, double Y)
    {
        /// <summary>Is there a door in this walk's story — one it is bound for, or one it came out of?
        /// </summary>
        public bool IsADoor => Sign.Length > 0;
    }

    private readonly AutoWalk _route;
    private readonly double _radius;
    private readonly double _keepOut;

    private NpcWalk(string plate, AutoWalk route, Bound bound, double x, double y, double facing,
        double radius, double pace, double keepOutInRadii)
    {
        Plate = plate;
        _route = route;
        For = bound;
        X = x;
        Y = y;
        Facing = facing;
        _radius = radius;
        Pace = pace;
        _keepOut = radius * keepOutInRadii;
    }

    /// <summary>Whose figure this is — the plate the deck draws over their head and the room already knows
    /// them by (<c>CanteenRegulars</c>' own, never a second name).</summary>
    public string Plate { get; }

    /// <summary>Where they are going, and what is painted on it.</summary>
    public Bound For { get; }

    /// <summary>Deck units per second. Held rather than read from <see cref="PaceDu"/> so a caller with a
    /// reason (a lab, a test that wants a leg walked in one frame) can say so out loud.</summary>
    public double Pace { get; }

    /// <summary>Where they are, in the surface's own coordinates.</summary>
    public double X { get; private set; }

    /// <summary>…and where they are, the other half.</summary>
    public double Y { get; private set; }

    /// <summary>Which way they are facing, in radians. Their heading while walking; the captain while
    /// waiting, because being looked at is the content.</summary>
    public double Facing { get; private set; }

    /// <summary>What happened on the last <see cref="Step"/>.</summary>
    public Doing State { get; private set; } = Doing.Walking;

    /// <summary>Is this walk still going to happen? False once they are through the door, or once the ground
    /// refused them.</summary>
    public bool Afoot => State is Doing.Walking or Doing.Waiting;

    /// <summary>The route as planned, for a test that has to prove a real path was walked rather than a
    /// position assigned. Never mutated.</summary>
    public IReadOnlyList<DeckReachability.Point> Route => _route.Route;

    /// <summary>
    /// Plan somebody's walk, or find out there is no way through.
    ///
    /// <para>The plan is <see cref="AutoWalk.Plan"/>'s, with the BODY's goal reach (zero — one lattice step)
    /// and never a finger's, because this is somebody walking to a place and not somebody pointing at one.
    /// Null when the floor does not connect the two, which is the reachability audit's own verdict and is
    /// never a reason to place the figure at the far end anyway.</para>
    /// </summary>
    /// <param name="plate">Their plate. The deck draws it; nothing else reads it.</param>
    /// <param name="bound">Where they are going and what is on the door.</param>
    /// <param name="from">Where they are standing now.</param>
    /// <param name="walls">The floor's own stone — the caller's collision list, never a copy.</param>
    /// <param name="radius">Their body, which is the captain's body: one law, one width.</param>
    /// <param name="gait">Who is walking. <see cref="SurfaceCollision.Gait.Person"/> or nothing at all —
    /// see law one in the class docs.</param>
    /// <param name="pace">Deck units per second; <see cref="PaceDu"/> unless a caller has a reason.</param>
    /// <param name="keepOutInRadii">How wide a berth this walk gives the captain, in body radii.
    /// <see cref="PersonalSpaceInRadii"/> for anybody going about their own business, and
    /// <see cref="NoPersonalSpace"/> for a walk whose errand IS the captain — see that constant for why
    /// the polite answer is the wrong one at somebody's own table.</param>
    /// <exception cref="ArgumentException">The gait was not a person's. Never Reevers.</exception>
    public static NpcWalk? Plan(
        string plate,
        Bound bound,
        DeckReachability.Point from,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        double radius,
        SurfaceCollision.Gait gait,
        double pace = PaceDu,
        double keepOutInRadii = PersonalSpaceInRadii)
    {
        ArgumentNullException.ThrowIfNull(plate);
        ArgumentNullException.ThrowIfNull(walls);

        // ── LAW ONE, AND IT IS THE FIRST THING THIS FILE DOES ──
        // Owner: the Old Ones keep their stagger. A walker that could be handed one is a walker somebody
        // will hand one to, on a Friday, for a reason that made sense at the time.
        if (gait != SurfaceCollision.Gait.Person)
        {
            throw new ArgumentException(
                $"{nameof(NpcWalk)} walks people. {gait} is not a person's gait, and the Old Ones keep " +
                "theirs (#731, #724).", nameof(gait));
        }
        if (!(pace > 0) || double.IsNaN(pace) || double.IsInfinity(pace))
        {
            throw new ArgumentOutOfRangeException(nameof(pace), pace, "A walking pace is positive and finite.");
        }
        if (!(keepOutInRadii >= 0) || double.IsInfinity(keepOutInRadii))
        {
            throw new ArgumentOutOfRangeException(nameof(keepOutInRadii), keepOutInRadii,
                "A berth is nought or wider, and finite.");
        }

        var to = new DeckReachability.Point(bound.X, bound.Y);
        AutoWalk.Attempt attempt = AutoWalk.Plan(
            true, from, to, walls, radius, AutoWalk.BoundsFor(walls, from, to));
        if (attempt.Route is not { } route)
        {
            return null;
        }

        double facing = Math.Atan2(to.Y - from.Y, to.X - from.X);
        return new NpcWalk(plate, route, bound, from.X, from.Y, facing, radius, pace, keepOutInRadii);
    }

    /// <summary>
    /// #731 v2 · SHE HAS GOT THERE, AND SHE IS LOOKING BACK AT YOU.
    ///
    /// <para>The wait-and-look is already law three's own posture — a walker blocked in a doorway turns and
    /// faces the captain, because being looked at is the content — and the escort beat needs the same posture
    /// for the opposite reason: she is not waiting for you to get out of the way, she is waiting for you to
    /// come. <i>"It is dramatic telling when our contact wants us to follow them into kabinetti."</i> A body
    /// standing in a doorway looking back across a loud hall at you says the whole thing, and no line has
    /// to.</para>
    ///
    /// <para>The ONLY writer of <see cref="Facing"/> outside a step, and it is deliberately not a step: it
    /// moves nothing, spends no budget, asks the stone nothing and cannot change <see cref="State"/>. A walk
    /// that has <see cref="Doing.Arrived"/> is over; what its body does with its head afterwards is the
    /// caller's beat and not the route's.</para>
    /// </summary>
    public void LookTowards(double x, double y) => Facing = Math.Atan2(y - Y, x - X);

    /// <summary>
    /// One frame of somebody walking away, or somebody walking in.
    ///
    /// <para>The shape is the guard's stride and the captain's click-walk, because it is the same shape:
    /// a budget of <see cref="Pace"/> × <paramref name="dt"/>, spent in sub-steps, each one handed to
    /// <see cref="SurfaceCollision.Slide"/> with the person's gait. Nothing here re-implements collision;
    /// if it did, this whole class would be a cartoon.</para>
    ///
    /// <para><b>And the captain is checked BEFORE the step is taken, never after.</b> A yield tested after
    /// the move is a walker that stands inside the captain for one frame and then apologises — which is a
    /// clip, drawn, at sixty frames a second. So the destination is measured first: if it would come inside
    /// one body-width of them, the walker does not move at all this frame, turns to look at them, and says
    /// <see cref="Doing.Waiting"/>. It keeps its route, so the walk finishes itself the moment the doorway
    /// clears.</para>
    /// </summary>
    /// <returns>True when the walker moved this frame.</returns>
    public bool Step(
        double dt, IReadOnlyList<SurfaceCollision.Segment> walls, double captainX, double captainY)
    {
        ArgumentNullException.ThrowIfNull(walls);
        if (!Afoot || dt <= 0)
        {
            return false;
        }

        double keepOut = _keepOut;
        double budget = Pace * dt;
        double startX = X, startY = Y;
        bool yielded = false;

        for (int step = 0; step < SubStepsPerFrame && budget > NoBudgetLeft; step++)
        {
            if (!_route.TryStep(X, Y, budget, out double dx, out double dy))
            {
                break;
            }

            (double nx, double ny) = SurfaceCollision.Slide(
                X, Y, dx, dy, _radius, walls, SurfaceCollision.Gait.Person);

            // ── LAW THREE ── the captain is not stone, so the stone did not stop us. We stop ourselves,
            // and we stop BEFORE the move rather than after it.
            double cx = nx - captainX, cy = ny - captainY;
            if ((cx * cx) + (cy * cy) < keepOut * keepOut)
            {
                yielded = true;
                break;
            }

            double mx = nx - X, my = ny - Y;
            if ((mx * mx) + (my * my) < StoodStillSq)
            {
                // ── LAW FOUR ── the plan and the ground disagreed. Stop honestly.
                _route.Snag();
                break;
            }

            budget -= Math.Sqrt((mx * mx) + (my * my));
            X = nx;
            Y = ny;
        }

        double tx = X - startX, ty = Y - startY;
        bool moved = (tx * tx) + (ty * ty) > StoodStillSq;
        if (moved)
        {
            Facing = Math.Atan2(ty, tx);
        }

        if (yielded)
        {
            // Looking at you. This is the beat, not a stall.
            Facing = Math.Atan2(captainY - Y, captainX - X);
            State = Doing.Waiting;
            return moved;
        }

        State = _route switch
        {
            { Arrived: true } => Doing.Arrived,
            { Active: false } => Doing.Snagged,
            _ => Doing.Walking,
        };
        return moved;
    }
}
