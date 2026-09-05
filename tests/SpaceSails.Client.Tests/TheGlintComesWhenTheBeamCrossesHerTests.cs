using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Pages.Stations;
using SpaceSails.Core;
using Xunit;

// BL0005: this bench sets the tracking post's parameters from outside rather than standing the component
// up in a render tree — the licence TheScopeGoesWhereTheCaptainPointsItTests already takes in this suite.
#pragma warning disable BL0005

namespace SpaceSails.Client.Tests;

/// <summary>
/// #240 · THE FIND ALWAYS LANDED AT 100 %.
///
/// <para>Owner, watching the roadster scan climb: <i>"Is it randomized now, the point when we find the car,
/// or is it always at 100%? We might get lucky earlier also?"</i></para>
///
/// <para><b>Ground truth as filed.</b> PR-A revealed a hidden body only on a COMPLETED pass whose disc
/// covered its true position — so wherever in the swept sky the thing actually sat, it was found at the end,
/// and the climbing percentage was suspense about nothing.</para>
///
/// <para><b>The fix is not new arithmetic.</b> The sweep's coverage over time was already fully defined:
/// <c>WedgeToward</c> aims it and <c>ActiveProgress</c> times it. All that was added is
/// <see cref="ScanJob.CoverageFraction"/> — the statement that the beam runs from the leading edge to the
/// trailing one — and a live report of where the beam is. Luck by geometry, not by dice: a third of the way
/// into the arc is a third of the way through the pass, every time, on any machine.</para>
///
/// <para>These guards drive the REAL <see cref="TrackingPost"/> clock and the REAL page handler; only the
/// wire between them is the bench's, and the events on it are the component's own.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheGlintComesWhenTheBeamCrossesHerTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private const string LostThingId = "the-lost-thing";

    /// <summary>The ship, well off the sun so a bearing is a real direction and not a rounding artefact.</summary>
    private static readonly Vector2d ShipAt = new(1.0e11, 0);

    private const double Range = 5.0e11;      // ship → the patch of sky
    private const double PatchRadius = 1.0e10;

    /// <summary>The pass's own length, from Core: a narrow wedge is priced at the floor, so this is
    /// <c>MinPassSeconds</c> at a telescope speed of 1. Derived, never typed.</summary>
    private static double PassSeconds => SensorTaskGeometry.Duration(
        SensorTask.AreaScan(PatchCentredOn(0.5), PatchRadius, "x"),
        SensorTaskGeometry.WedgeToward(ShipAt, PatchCentredOn(0.5), PatchRadius),
        1);

    // ── THE LAW ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHE GLINTS WHERE SHE STANDS. A contact three tenths of the way into the swept arc is found three
    /// tenths of the way through the pass — not at the end, and not at the start either. Both failure modes
    /// are caught by the same assertion, which is why it is a band and not a bound: the shipped bug lands at
    /// 1.0, and a reveal that fired on any coverage at all lands at ~0.
    /// </summary>
    [Theory]
    [InlineData(0.12)]
    [InlineData(0.30)]
    [InlineData(0.80)]
    public void AContactPartWayIntoTheArcIsFoundPartWayThroughThePass(double whereInTheArc)
    {
        double found = TheProgressSheWasFoundAt(whereInTheArc);

        Assert.InRange(found, whereInTheArc - 0.06, whereInTheArc + 0.06);
    }

    /// <summary>
    /// AND IT IS NOT ALWAYS THE SAME MOMENT, which is the owner's actual question. Three contacts, three
    /// places in the arc, three different glints — a reveal rule that had gone back to a fixed instant would
    /// give the same number three times and this is what would say so.
    /// </summary>
    [Fact]
    public void WhereSheStandsInTheArcIsWhatDecidesWhenSheGlints()
    {
        double early = TheProgressSheWasFoundAt(0.12);
        double middle = TheProgressSheWasFoundAt(0.50);
        double late = TheProgressSheWasFoundAt(0.88);

        Assert.True(early < middle && middle < late,
            $"the glint does not move with her: {early:0.000}, {middle:0.000}, {late:0.000}");
        Assert.True(late - early > 0.5,
            $"the whole spread is only {late - early:0.000} of a pass — the find is still effectively fixed");
    }

    /// <summary>
    /// A PASS THAT NEVER COVERS HER COMPLETES EMPTY, HONESTLY. The find is earlier, never freer: something
    /// outside the swept disc is not charted by a scan that went nowhere near it, at any coverage, including
    /// the completing one.
    /// </summary>
    [Fact]
    public void AScanThatMissesHerCompletesWithoutFindingHer()
    {
        // The patch is aimed a full arc-width off her, so she is outside the disc AND outside the wedge.
        Vector2d elsewhere = PointAtBearing(BearingToHer + (4 * ArcWidth));
        Assert.Null(RunTheScan(elsewhere).FoundAtProgress);
    }

    /// <summary>
    /// DETERMINISM SURVIVES. "We got lucky" has to be a replayable event: same world, same scan, same
    /// sim-instant, or a save-and-reload would hand the captain a different hunt.
    /// </summary>
    [Fact]
    public void TheSameHuntGlintsAtTheSameInstantEveryTime()
    {
        double first = TheProgressSheWasFoundAt(0.37);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(first, TheProgressSheWasFoundAt(0.37), 12);
        }
    }

    /// <summary>
    /// THE PREMISE: the bench really does put her part way into the wedge, not at its aim point. Every case
    /// above is about WHERE in the arc she is, so a bench that quietly placed her at the centre every time
    /// would agree with a broken rule and a fixed one alike.
    /// </summary>
    [Theory]
    [InlineData(0.12)]
    [InlineData(0.50)]
    [InlineData(0.88)]
    public void ThePremise_SheReallyStandsWhereTheBenchSaysSheDoes(double whereInTheArc)
    {
        Vector2d patch = PatchCentredOn(whereInTheArc);

        // Inside the disc the scan sweeps…
        Assert.True((HerPosition - patch).Length <= PatchRadius,
            "she is outside the patch — this scan could never find her at all");

        // …and at the intended fraction of the wedge the telescope actually aims.
        ScanJob job = SensorTaskGeometry.WedgeToward(ShipAt, patch, PatchRadius);
        Assert.Equal(whereInTheArc, job.CoverageFraction(BearingToHer)!.Value, 2);
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    private static double BearingToHer => 0.0;

    private static Vector2d HerPosition => PointAtBearing(BearingToHer);

    private static double ArcWidth => 2 * Math.Asin(PatchRadius / Range);

    private static Vector2d PointAtBearing(double bearing) =>
        ShipAt + new Vector2d(Range * Math.Cos(bearing), Range * Math.Sin(bearing));

    /// <summary>The patch of sky to scan so that SHE sits at <paramref name="whereInTheArc"/> of the swept
    /// wedge: aim the disc off her by the offset that puts her bearing at that fraction of the arc.</summary>
    private static Vector2d PatchCentredOn(double whereInTheArc) =>
        PointAtBearing(BearingToHer + ((0.5 - whereInTheArc) * ArcWidth));

    private static double TheProgressSheWasFoundAt(double whereInTheArc)
    {
        double? found = RunTheScan(PatchCentredOn(whereInTheArc)).FoundAtProgress;
        Assert.True(found is not null,
            $"the scan never found her at all with her {whereInTheArc:0.00} of the way into the arc — "
            + "this bench would then be measuring nothing.");
        return found!.Value;
    }

    /// <summary>
    /// One area scan, run on the REAL telescope schedule, its coverage reports fed to the REAL page handler
    /// tick by tick. Returns the fraction of the pass at which the page charted her, or null if it never did.
    /// </summary>
    private static (double? FoundAtProgress, int Ticks) RunTheScan(Vector2d patch)
    {
        (Pages.Map map, TrackingPost post, List<AreaScanCoverage> reports) = AHuntInProgress();

        Assert.True(post.EnqueueTask(SensorTask.AreaScan(patch, PatchRadius, "the patch")));

        var revealed = (HashSet<string>)Get(map, "_revealedBodyIds")!;
        double step = PassSeconds / 60;   // sixty looks at the sweep, which is what a rAF tick is

        for (int i = 1; i <= 70; i++)
        {
            reports.Clear();
            Tick(post, step * i);

            foreach (AreaScanCoverage report in reports)
            {
                Invoke(map, "OnAreaScanCovered", report);
                if (revealed.Contains(LostThingId))
                {
                    return (report.Covered, i);
                }
            }
        }

        return (null, 70);
    }

    /// <summary>A world with one hidden thing in it, a ship, and a telescope desk aimed from the same
    /// point — the standing the owner's roadster hunt was actually in.</summary>
    private static (Pages.Map Map, TrackingPost Post, List<AreaScanCoverage> Reports) AHuntInProgress()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        // She is parked: an enormous period, so her true position over one pass is her position, and the
        // guard is about the beam moving rather than about her moving.
        var ephemeris = new CircularOrbitEphemeris(
        [
            new CelestialBody("sol", "Sol", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody(LostThingId, "The Lost Thing", "sol", 0, 3, HerPosition.Length, 1e18,
                Math.Atan2(HerPosition.Y, HerPosition.X), BodyKind.Station),
        ]);

        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_ship", new ShipState(ShipAt, Vector2d.Zero, 0));
        ((HashSet<string>)Get(map, "_hiddenBodyIds")!).Add(LostThingId);

        var reports = new List<AreaScanCoverage>();
        var post = new TrackingPost
        {
            ShipPosition = ShipAt,
            ShipVelocity = Vector2d.Zero,
            MaxTracks = 1,
            TelescopeSpeedFactor = 1,
            Ephemeris = ephemeris,
            Candidates = [],
        };
        // The desk's own render early-out, the same piece of theatre the other benches in this suite ride
        // on: told a render is already queued, ComponentBase returns instead of reaching for a render
        // handle nothing here has. Raising a coverage report is an event callback, and an event callback
        // on a component asks for one.
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(post, true);
        post.OnAreaScanCoverage =
            EventCallback.Factory.Create<AreaScanCoverage>(post, reports.Add);

        Tick(post, 0);
        reports.Clear();
        return (map, post, reports);
    }

    private static void Tick(TrackingPost post, double toSimTime)
    {
        MethodInfo tick = typeof(TrackingPost).GetMethod("OnParametersSet", Hidden)
            ?? throw new MissingMethodException("TrackingPost has no OnParametersSet — this bench's tick has moved.");
        tick.Invoke(post, null);
        post.SimTime = toSimTime;
        tick.Invoke(post, null);
    }

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static object? Get(object o, string field) =>
        (o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o);

    private static void Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);
}
