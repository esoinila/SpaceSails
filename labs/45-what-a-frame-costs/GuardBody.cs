using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Labs.Lab45;

/// <summary>
/// #852 · ONE GUARD, ONE FRAME — the round as <c>Map.Patrol.cs</c> walks it, re-stated here because the
/// original cannot be called.
///
/// <para><b>Why it is re-stated and not called.</b> <c>WalkTheRound</c> and <c>SpendTheStride</c> are private
/// methods of the <c>Map</c> Blazor component. Instantiating that headlessly would mean a renderer, a
/// <c>NavigationManager</c> and a JS runtime, none of which this lab has any business standing up — and a
/// browser stopwatch would be worthless anyway (standing law: an MCP-driven tab is <c>document.hidden</c>,
/// rAF throttled and timers clamped). So the loop is transcribed, and every line of it that COSTS anything
/// is a call into the same Core/Client code the component calls:</para>
///
/// <list type="number">
/// <item><c>AutoWalk.Plan</c> over <c>PatrolBeat.LatticeFor</c> — the A* on arrival at a stop.</item>
/// <item>up to <c>AutoWalkSubStepsPerFrame</c> (8) × <c>SurfaceCollision.Slide</c> at
/// <c>Gait.Person</c> — the stride.</item>
/// <item><c>PatrolBeat.SightingFor</c> and <c>PatrolBeat.Heard</c> — the two asks the loop makes of every
/// guard, every frame, at the bottom of <c>AdvancePatrol</c>.</item>
/// <item><c>PatrolBeat.Notices</c> — the third ask, made once per guard per frame from
/// <c>StopTheRoundIfAnybodySeesYou</c>.</item>
/// </list>
///
/// <para>What is NOT transcribed is every branch that does no arithmetic: the escort, the cubicle knock, the
/// bench hold, the walk-up. Those are the same guard doing one of these same things, so leaving them out
/// makes the round CHEAPER here than in the game, never dearer. Anything this lab finds expensive is
/// therefore a floor under the real bill, which is the direction a benchmark is allowed to be wrong in.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
internal sealed class GuardBody
{
    /// <summary>Map.Deck.cs's own cap, quoted: a frame spends at most this many sub-steps of the route.</summary>
    internal const int SubStepsPerFrame = 8;

    internal double X, Y;
    private int _leg;
    private double _standing;
    private int _retries;
    private AutoWalk? _route;

    /// <summary>How many A* plans this body has asked for — the spike counter Section C reports.</summary>
    internal int Plans { get; private set; }

    /// <summary>How many <c>Slide</c> calls the stride has actually spent.</summary>
    internal long Slides { get; private set; }

    internal GuardBody(World world, int index, int count)
    {
        int leg = PatrolBeat.StartLeg(world.Beat.Count, index, Math.Max(1, count));
        PatrolBeat.Stop at = world.Beat[leg];
        X = at.X;
        Y = at.Y;
        _leg = (leg + 1) % world.Beat.Count;
        _standing = PatrolBeat.StandSeconds;
    }

    /// <summary>The legs only — everything <c>WalkTheRound</c> does, and none of what the eye does. Timed
    /// separately so the table can say which half of a guard is the expensive half.</summary>
    internal void WalkTheRound(World world, double dt)
    {
        if (_standing > 0)
        {
            _standing -= dt;
            return;
        }

        PatrolBeat.Stop target = world.Beat[_leg % world.Beat.Count];

        if (_route is not { Active: true })
        {
            AutoWalk.Attempt planned = AutoWalk.Plan(
                true,
                new DeckReachability.Point(X, Y),
                new DeckReachability.Point(target.X, target.Y),
                world.Legs,
                DeckPlan.AvatarRadius,
                PatrolBeat.LatticeFor(new PatrolBeat.Stop(X, Y, "here"), target, world.Envelope));
            Plans++;

            if (planned.Route is null)
            {
                _retries = 0;
                _leg = (_leg + 1) % world.Beat.Count;
                return;
            }
            _route = planned.Route;
        }

        SpendTheStride(world, dt);

        double gx = target.X - X, gy = target.Y - Y;
        if ((gx * gx) + (gy * gy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu)
        {
            _route = null;
            _retries = 0;
            _standing = PatrolBeat.StandSeconds;
            _leg = (_leg + 1) % world.Beat.Count;
            return;
        }

        if (_route is not { Active: true })
        {
            _route = null;
            if (++_retries > PatrolBeat.RePlansPerLeg)
            {
                _retries = 0;
                _leg = (_leg + 1) % world.Beat.Count;
            }
        }
    }

    private void SpendTheStride(World world, double dt)
    {
        double budget = PatrolBeat.WalkSpeed * dt;

        for (int step = 0; step < SubStepsPerFrame && budget > 1e-9; step++)
        {
            if (_route is not { Active: true } route
                || !route.TryStep(X, Y, budget, out double dx, out double dy))
            {
                break;
            }

            (double nx, double ny) = SurfaceCollision.Slide(
                X, Y, dx, dy, DeckPlan.AvatarRadius, world.Legs, SurfaceCollision.Gait.Person);
            Slides++;

            double mx = nx - X, my = ny - Y;
            if ((mx * mx) + (my * my) < 1e-18)
            {
                route.Snag();
                break;
            }

            budget -= Math.Sqrt((mx * mx) + (my * my));
            X = nx;
            Y = ny;
        }
    }

    /// <summary>
    /// #852 · HOW MANY FULL WALL SWEEPS THIS GUARD'S THREE ASKS WILL ACTUALLY RUN THIS FRAME — nought, two
    /// or three, and it is the finding that makes the whole eye table readable.
    ///
    /// <para><c>EyesOn</c> range-checks BEFORE it calls <c>HasLineOfSight</c>, so a guard the captain is
    /// nowhere near costs three squared-distance comparisons and not one segment of stone. The sweeps only
    /// start when somebody is inside the eye's reach:</para>
    ///
    /// <list type="bullet">
    /// <item>beyond <c>MarkerSightDu</c> (30 du): <b>0 sweeps</b>.</item>
    /// <item>within 30 du: <c>SightingFor</c> sweeps, and <c>Heard</c> — whose own earshot IS
    /// <c>MarkerSightDu</c> — sweeps a second time through <c>DrawnFor</c>. <b>2 sweeps</b>, and the second
    /// one is the same question as the first, asked again.</item>
    /// <item>within <c>NoticeDu</c> (9 du): <c>Notices</c> too. <b>3 sweeps</b>.</item>
    /// </list>
    ///
    /// <para>Computed here rather than measured, because it is arithmetic on the same constants the
    /// predicates use — and it is reported alongside the timings so a row of the table can never be read as
    /// "eight guards did twenty-four sweeps" when eight guards did none.</para>
    /// </summary>
    internal int SweepsFor(double captainX, double captainY)
    {
        double dx = captainX - X, dy = captainY - Y;
        double d2 = (dx * dx) + (dy * dy);
        int n = 0;
        if (d2 <= PatrolBeat.MarkerSightDu * PatrolBeat.MarkerSightDu)
        {
            n += 2;
        }
        if (d2 <= PatrolBeat.NoticeDu * PatrolBeat.NoticeDu)
        {
            n++;
        }
        return n;
    }

    /// <summary>
    /// The eye, exactly the three asks the component makes of every guard on every frame, against the PLAIN
    /// sight list the component hands them. Returns something so nothing can be optimised away.
    /// </summary>
    internal int TheEye(World world, double captainX, double captainY)
    {
        int acc = (int)PatrolBeat.SightingFor(captainX, captainY, X, Y, world.Sight);
        if (PatrolBeat.Heard(captainX, captainY, X, Y, world.Sight))
        {
            acc++;
        }
        if (PatrolBeat.Notices(X, Y, captainX, captainY, world.Sight))
        {
            acc += 2;
        }
        return acc;
    }
}
