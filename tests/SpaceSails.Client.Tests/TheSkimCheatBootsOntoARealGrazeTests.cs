using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1389 · <c>?skim=&lt;body&gt;</c> BOOTS ONTO AN ARC WHOSE NATURAL PASS REALLY GRAZES THE CLOUD TOPS.
///
/// <para>The cheat bisected an impact parameter inside a bracket (3 R to 30 R) that did not contain the hitting
/// one: the ship is backed off along a straight line while the body swings on its orbit, so three days out
/// the body has left that line by r·θ²/2 (about 400,000 km at Venus). Every b in the bracket flew hundreds of
/// thousands of kilometres wide and the boot said "cloud tops ~2 days out" over an arc that passed 370,000 km
/// from Venus. The oracle here is independent of the cheat's own search: the booted ship is flown through the
/// gravity-only sim at a much finer step, and its periapsis must sit inside the shell.</para>
/// </summary>
public sealed class TheSkimCheatBootsOntoARealGrazeTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [Theory]
    [InlineData("venus")]
    [InlineData("jupiter")]
    [InlineData("saturn")]
    [InlineData("earth")]
    [InlineData("mars")]
    [InlineData("titan")]
    public async Task TheBootedArcsPeriapsisIsInsideTheShell(string bodyId)
    {
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync($"/map?skim={bodyId}");
        Assert.Null(booted.Threw);

        var eph = (ICelestialEphemeris)booted.Field("_ephemeris")!;
        var ship = (ShipState)booted.Field("_ship")!;
        CelestialBody body = eph.Bodies.First(b => b.Id == bodyId);

        // Gravity only (no drag), sun + the body: the periapsis of the arc as booted, flown fine.
        var vac = new Simulator(new CircularOrbitEphemeris(
            eph.Bodies.Where(b => b.Id == bodyId || b.Id == body.ParentId || b.ParentId == bodyId || b.ParentId is null)
                .Select(b => b with { Atmosphere = null }).ToList()), 1.0);
        // The live ship steps at 60 s (AdaptiveWarpQuantum), so that is the step the oracle flies; the plot's own
        // 3-hour projection reads this same arc thousands of kilometres off and is NOT the oracle.
        IReadOnlyList<TrajectorySample> path = vac.ProjectAdaptive(
            ship, null, 6 * 86400.0, minTimeStep: 5, maxTimeStep: 60, dynamicalTimeFraction: 1.0 / 200, maxSamples: 60_000);
        double peri = ClosestApproach.Passes(path, eph).First(p => p.BodyId == bodyId).Distance;

        double altitudeKm = (peri - body.BodyRadius) / 1000.0;
        Assert.True(altitudeKm >= 0.0 && altitudeKm <= body.Atmosphere!.TopAltitude / 1000.0,
            $"periapsis altitude {altitudeKm:F1} km is outside the 0..{body.Atmosphere!.TopAltitude / 1000.0:F0} km shell");
    }
}
