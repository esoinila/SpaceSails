namespace SpaceSails.Core.Tests;

/// <summary>
/// #240 · THE SWEEP'S COVERAGE IS A FUNCTION OF TIME, AND NOW IT SAYS SO.
///
/// <para>A <see cref="ScanJob"/> has always taken sim time in proportion to the arc it sweeps
/// (<see cref="ScanJob.DurationSeconds"/>). What was missing was the one statement that makes a coverage a
/// function of time at all: WHERE THE BEAM STARTS. Without it the only honest thing a caller could say
/// about a pass was "it is finished", which is exactly why the wreck reveal fired at 100 % every time.</para>
///
/// <para>The convention, stated once and pinned here: the beam starts at the leading edge
/// (<c>Center − Arc/2</c>) and runs to the trailing one. Everything else falls out of it — deterministically,
/// which is law in Core.</para>
/// </summary>
public class ScanCoverageOverTimeTests
{
    private const double Tol = 1e-9;

    /// <summary>THE EDGES AND THE MIDDLE. A bearing at the leading edge is covered at the very start of the
    /// pass, the aim point half way through, the trailing edge at the end.</summary>
    [Fact]
    public void TheBeamStartsAtTheLeadingEdgeAndFinishesAtTheTrailing()
    {
        var job = new ScanJob(1.0, 0.4);

        Assert.Equal(0.0, job.CoverageFraction(1.0 - 0.2)!.Value, 9);
        Assert.Equal(0.5, job.CoverageFraction(1.0)!.Value, 9);
        Assert.Equal(1.0, job.CoverageFraction(1.0 + 0.2)!.Value, 9);
    }

    /// <summary>
    /// A THIRD OF THE WAY IN IS A THIRD OF THE WAY THROUGH — the whole of #240 in one line. Owner: "Early in
    /// the arc → she glints at 12%; late → 96%."
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.12)]
    [InlineData(0.3)]
    [InlineData(0.5)]
    [InlineData(0.96)]
    [InlineData(1.0)]
    public void EveryFractionOfTheArcIsThatFractionOfThePass(double fraction)
    {
        var job = new ScanJob(-2.3, 0.75);
        double bearing = job.CenterBearingRad - (job.ArcWidthRad / 2) + (fraction * job.ArcWidthRad);

        Assert.Equal(fraction, job.CoverageFraction(bearing)!.Value, 9);
    }

    /// <summary>
    /// A BEARING THE BEAM NEVER REACHES HAS NO MOMENT. Null, not 0 and not 1 — a caller that got a number
    /// for every direction could never tell "she was swept at the start" from "she was never swept", and a
    /// pass that misses still has to complete empty and honest.
    /// </summary>
    [Fact]
    public void ABearingOutsideTheWedgeIsNeverCovered()
    {
        var job = new ScanJob(0.0, 0.4);

        Assert.Null(job.CoverageFraction(0.21));
        Assert.Null(job.CoverageFraction(-0.21));
        Assert.Null(job.CoverageFraction(Math.PI));

        // A job that sweeps nothing covers nothing, including its own aim point.
        Assert.Null(new ScanJob(0.5, 0).CoverageFraction(0.5));
    }

    /// <summary>
    /// COVERED AND DETECTABLE ARE THE SAME QUESTION. <see cref="TrackingStation.InArc"/> is what decides
    /// whether a sweep sees a contact at all; a coverage that disagreed with it would find things the sweep
    /// does not, or lose things it does. Walked right round the circle rather than sampled near the edges.
    /// </summary>
    [Fact]
    public void CoverageAgreesWithTheArcTestTheSweepItselfUses()
    {
        foreach (double centre in new[] { -3.0, -0.4, 0.0, 1.7, 3.1 })
        {
            foreach (double arc in new[] { 0.05, 0.4, 2.0, Math.PI })
            {
                var job = new ScanJob(centre, arc);
                for (int i = 0; i < 720; i++)
                {
                    double bearing = (Math.Tau * i / 720) - Math.PI;
                    bool inArc = TrackingStation.InArc(bearing, centre, arc);
                    double? covered = job.CoverageFraction(bearing);

                    // The two boundary bearings are exactly on the edge; float rounding may put one of them
                    // on either side of a `<=`, and a guard that failed there would be measuring arithmetic.
                    bool onTheEdge =
                        Math.Abs(Math.Abs(NormalizeAngle(bearing - centre)) - (arc / 2)) < 1e-12;
                    if (onTheEdge)
                    {
                        continue;
                    }

                    Assert.Equal(inArc, covered is not null);
                    if (covered is { } f)
                    {
                        Assert.InRange(f, -Tol, 1 + Tol);
                    }
                }
            }
        }
    }

    /// <summary>
    /// A FULL-CIRCLE SURVEY HAS NO OUTSIDE. Every direction gets a moment, each one exactly once, in order —
    /// so the passive watch and the 360° job need no special case anywhere.
    /// </summary>
    [Fact]
    public void AFullSurveyCoversEveryBearingExactlyOnceAndInOrder()
    {
        var job = new ScanJob(0.0, Math.Tau);
        double previous = -1;

        for (int i = 0; i < 360; i++)
        {
            double bearing = -Math.PI + (Math.Tau * i / 360);
            double f = job.CoverageFraction(bearing)!.Value;
            Assert.InRange(f, 0.0, 1.0);
            Assert.True(f > previous, $"the survey went backwards at {bearing:0.000} rad");
            previous = f;
        }
    }

    /// <summary>DETERMINISM IS LAW. Same job, same bearing, same answer — this is what lets "we got lucky"
    /// be a replayable event rather than a roll.</summary>
    [Fact]
    public void TheSameBearingAlwaysGlintsAtTheSameMoment()
    {
        var job = new ScanJob(0.77, 0.31);
        double first = job.CoverageFraction(0.7)!.Value;

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(first, new ScanJob(0.77, 0.31).CoverageFraction(0.7)!.Value);
        }
    }

    private static double NormalizeAngle(double angle)
    {
        angle %= Math.Tau;
        if (angle > Math.PI)
        {
            angle -= Math.Tau;
        }
        if (angle < -Math.PI)
        {
            angle += Math.Tau;
        }
        return angle;
    }
}
