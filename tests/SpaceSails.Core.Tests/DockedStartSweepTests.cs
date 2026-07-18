using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #288 / #289 — the docked-start smoke sweep and the outer-oasis law.
///
/// #288: every dockable berth in the shipped Sol scenario must boot clean from a docked start — a valid
/// clamped berth (inside the DockRule envelope) with its fuel pump live — so /map?dock=&lt;id&gt; smoke-tests
/// any position without paying the multi-hour navigate tax. The sweep reads the same DockableHavens
/// registry the cheat does, so every haven a scenario adds later is swept for free.
///
/// #289: the outer wells are oases in a large desert (owner, 2026-07-18). Every gas giant from Jupiter
/// out must carry a self-sustaining haven with a pump — the #262 Uranus stranding taught what a missing
/// pump costs — and every haven beyond Saturn must offer fuel.
/// </summary>
public class DockedStartSweepTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    // The outer giants (#289 audits Jupiter and everything beyond); Saturn is the "beyond Saturn" fence.
    private const double JupiterOrbitRadius = 7.7857e11;
    private const double SaturnOrbitRadius = 1.43353e12;

    private static double DistanceFromSun(ICelestialEphemeris eph, CelestialBody body) =>
        (eph.Position(body.Id, 0) - eph.Position("sun", 0)).Length;

    // ---- #288: the docked-start smoke sweep ----

    [Fact]
    public void EveryDockableBerth_BootsCleanFromADockedStart()
    {
        CircularOrbitEphemeris eph = Sol();
        IReadOnlyList<CelestialBody> berths = DockableHavens.All(eph);

        // A non-empty registry of distinct ids (no /map?dock= collisions).
        Assert.NotEmpty(berths);
        Assert.Equal(berths.Count, berths.Select(b => b.Id).Distinct().Count());

        foreach (CelestialBody berth in berths)
        {
            // Boot from the docked start exactly as the client does — the one shared berth construction.
            ShipState docked = BerthState.CoMoving(eph, berth.Id, 0, BerthState.BerthOffsetMeters);

            // Docked state true: the berth satisfies the very DockRule envelope the clamp gate demands.
            Vector2d pos = eph.Position(berth.Id, 0);
            Vector2d vel = TransferMath.BodyVelocity(eph, berth.Id, 0);
            Assert.True(DockRule.InEnvelope(docked, pos, vel, berth.BodyRadius),
                $"{berth.Id}: a docked start must satisfy the dock envelope it claims to clamp in");

            // Services enumerable and live: the ship boots alongside this berth's own fuel pump — the same
            // truth the "⛽ FILL HER UP" button reads — so a docked start is refuellable from minute one.
            CelestialBody? pump = FuelReachability.AlongsidePump(eph, docked);
            Assert.NotNull(pump);
            Assert.Equal(berth.Id, pump!.Id);
        }
    }

    [Fact]
    public void Sweep_CoversTheKnownDockableBerths()
    {
        IReadOnlyList<string> ids = DockableHavens.AllIds(Sol());

        // Inner boltholes + one self-sustaining haven per outer giant (Red Eye at Jupiter, The Deep at
        // Neptune are the #289 additions). A regression that drops any berth from the map fails here.
        foreach (string id in new[]
                 {
                     "cinder-roost", "selene-gate", "the-space-bar",
                     "red-eye", "ringside-exchange", "the-tilt", "the-deep",
                 })
        {
            Assert.Contains(id, ids);
        }
    }

    // ---- #289: the outer-oasis law ----

    [Theory]
    [InlineData("jupiter")]
    [InlineData("saturn")]
    [InlineData("uranus")]
    [InlineData("neptune")]
    public void EveryOuterGiant_HasADockableFuelHaven(string giantId)
    {
        CircularOrbitEphemeris eph = Sol();
        CelestialBody giant = eph.Bodies.First(b => b.Id == giantId);
        Assert.True(giant.OrbitRadius >= JupiterOrbitRadius - 1.0, "sanity: these are the outer giants");

        // At least one dockable haven orbits this giant — no outer well may be a pumpless desert (#262).
        List<CelestialBody> oasis = [.. DockableHavens.All(eph).Where(h => h.ParentId == giantId)];
        Assert.NotEmpty(oasis);

        // And docking at it boots alongside a working pump: a self-sustaining oasis, fuel included.
        foreach (CelestialBody haven in oasis)
        {
            ShipState docked = BerthState.CoMoving(eph, haven.Id, 0, BerthState.BerthOffsetMeters);
            Assert.Equal(haven.Id, FuelReachability.AlongsidePump(eph, docked)?.Id);
        }
    }

    [Fact]
    public void EveryHavenBeyondSaturn_OffersFuel()
    {
        CircularOrbitEphemeris eph = Sol();

        List<CelestialBody> outerHavens =
            [.. eph.Bodies.Where(b => b.IsHaven && DistanceFromSun(eph, b) > SaturnOrbitRadius)];

        Assert.NotEmpty(outerHavens); // The Tilt (Uranus), The Deep (Neptune)
        foreach (CelestialBody haven in outerHavens)
        {
            // The oasis law locked: a fuel pump rides every haven beyond Saturn (FuelReachability counts a
            // pump wherever Kind==Station or IsHaven). A future outer haven that somehow lost its pump fails.
            Assert.True(haven.Kind == BodyKind.Station || haven.IsHaven,
                $"{haven.Id}: a haven beyond Saturn must offer fuel");
        }
    }
}
