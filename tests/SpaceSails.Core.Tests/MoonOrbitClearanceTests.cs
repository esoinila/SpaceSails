using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #286 — a kept/auto orbit around a MOON must clear the parent planet it circles beside. The owner,
/// after the Uranus stranding session: "we should make sure that in all Moon-docked situations we do
/// not orbit through the planet." The exhaustive sweep below is the evidence: EVERY moon in every
/// shipped/archived scenario, its solved kept orbit flown through the reused #278 surface-clearance
/// gate, asserting the swept path clears the parent (and the moon's own surface). The synthetic cases
/// prove the clamp binds — and refuses — where a pathological inner moon would thread its world.
/// </summary>
public class MoonOrbitClearanceTests
{
    private static ScenarioDefinition Load(string file) =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", file));

    // ---- The exhaustive sweep: every moon in every scenario clears its parent today ----

    [Theory]
    [InlineData("sol.json")]
    [InlineData("sol-eu.json")]
    [InlineData("oops.json")]
    public void EveryMoon_StandardKeptOrbit_ClearsItsParent(string scenarioFile)
    {
        var eph = CircularOrbitEphemeris.FromScenario(Load(scenarioFile));
        int moonsChecked = 0;

        foreach (CelestialBody moon in eph.Bodies)
        {
            if (moon.Kind != BodyKind.Moon)
            {
                continue;
            }

            MoonOrbitClearance.Verdict v = MoonOrbitClearance.Solve(eph, moon)
                ?? throw new Xunit.Sdk.XunitException($"{moon.Name}: Solve returned null for a moon.");
            moonsChecked++;

            // The core #286 property: the near side of the standard swept orbit stays outside the
            // parent's #278 clearance band. Exact geometric lower bound on distance to the parent centre.
            Assert.True(v.StandardNearSide >= v.ParentClearance,
                $"{moon.Name} (parent {v.ParentName}): standard kept orbit near-side {v.StandardNearSide:e3} m " +
                $"is inside the parent clearance {v.ParentClearance:e3} m — it would thread {v.ParentName}. " +
                $"park={v.StandardRadius:e3} d={v.ParentDistance:e3}");

            // No moon should be so tight it has no flyable orbit, and none should need clamping today.
            Assert.False(v.NoSafeOrbit, $"{moon.Name}: unexpectedly has no safe kept orbit.");
            Assert.False(v.Clamped, $"{moon.Name}: standard park unexpectedly clamped (parent binds).");

            // Reuse the #278 gate on the swept orbit itself: nothing threads the PARENT (or any planet).
            // A moon parked at its own airless floor may report a benign band 'shave' of ITSELF (the park
            // sits at 1.1 R by design) — that is the autopilot working, not a threaded world.
            IReadOnlyList<TrajectorySample> path = MoonOrbitClearance.SweptPath(eph, moon, v.StandardRadius, 0.0);
            SurfaceClearance.Violation? gate = SurfaceClearance.Check(path, eph, moon.Id);
            if (gate is { } hit)
            {
                Assert.Equal(moon.Id, hit.BodyId);
                Assert.False(hit.Threads, $"{moon.Name}: swept orbit threads {hit.BodyName}.");
            }
        }

        Assert.True(moonsChecked > 0, $"{scenarioFile}: no moons found to sweep.");
    }

    [Fact]
    public void Sol_RealMoons_ParkFarInsideTheParentClearance()
    {
        // A concrete evidence sample the owner asked for: Miranda (the Uranus stranding moon) and Phobos
        // (Mars' close-in deep well) both keep a wide margin from their parent. Numbers land in the PR body.
        var eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

        MoonOrbitClearance.Verdict miranda = MoonOrbitClearance.Solve(eph, eph.Bodies.First(b => b.Id == "miranda"))!.Value;
        Assert.False(miranda.Clamped);
        Assert.True(miranda.StandardNearSide > 4 * miranda.ParentClearance,
            $"Miranda near-side {miranda.StandardNearSide:e3} should dwarf Uranus clearance {miranda.ParentClearance:e3}.");

        MoonOrbitClearance.Verdict phobos = MoonOrbitClearance.Solve(eph, eph.Bodies.First(b => b.Id == "phobos"))!.Value;
        Assert.False(phobos.Clamped);
        Assert.True(phobos.StandardNearSide > phobos.ParentClearance,
            $"Phobos near-side {phobos.StandardNearSide:e3} should clear Mars clearance {phobos.ParentClearance:e3}.");
    }

    [Fact]
    public void Solve_ReturnsNull_ForNonOrbitableBodies()
    {
        var eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        Assert.Null(MoonOrbitClearance.Solve(eph, eph.Bodies.First(b => b.Id == "sun")));      // parentless
        Assert.Null(MoonOrbitClearance.Solve(eph, eph.Bodies.First(b => b.Id == "earth")));    // a planet, not a moon
        Assert.Null(MoonOrbitClearance.Solve(eph, eph.Bodies.First(b => b.Id == "the-tilt"))); // a μ=0 station
    }

    // ---- Synthetic pathology: the clamp binds where a real inner moon would thread its parent ----

    // A giant with a massive, very-close moon: its tide-stable park is geometrically bigger than the
    // moon's clearance from the giant, so the STANDARD swept orbit dives into the planet — but a tighter
    // park (>= the surface floor) still clears. The #286 danger zone, made concrete.
    private static CircularOrbitEphemeris ClampScenario()
    {
        var giant = new CelestialBody("giant", "Giant", null, 1e15, 2e7, 0, 0, 0, BodyKind.Planet);
        var moon = new CelestialBody("inner", "Inner", "giant", 5e14, 1e6, 2.55e7, 5e5, 0, BodyKind.Moon);
        return new CircularOrbitEphemeris([giant, moon]);
    }

    [Fact]
    public void InnerMoon_StandardParkThreadsParent_IsClampedToASafeTighterOrbit()
    {
        var eph = ClampScenario();
        MoonOrbitClearance.Verdict v = MoonOrbitClearance.Solve(eph, eph.Bodies.First(b => b.Id == "inner"))!.Value;

        // The standard park WOULD thread the giant...
        Assert.True(v.StandardNearSide < v.ParentClearance, "the standard park should breach the parent here.");
        Assert.NotNull(v.StandardViolation);
        Assert.True(v.StandardThreadsParent, "the reused #278 gate should flag the parent on the standard orbit.");

        // ...so the radius is clamped, not the autopilot redesigned: still a KEPT circular orbit, tighter.
        Assert.True(v.Clamped);
        Assert.False(v.NoSafeOrbit);
        Assert.True(v.SafeRadius < v.StandardRadius, "the clamp must tighten the radius.");
        Assert.True(v.SafeRadius >= OrbitRule.SurfaceParkRadii * v.MoonRadius, "never below the surface floor.");

        // And the CLAMPED swept orbit clears the parent through the very same gate.
        Assert.True(v.SafeNearSide >= v.ParentClearance, "clamped near-side must clear the parent band.");
        IReadOnlyList<TrajectorySample> safePath = MoonOrbitClearance.SweptPath(eph, eph.Bodies.First(b => b.Id == "inner"), v.SafeRadius, 0.0);
        SurfaceClearance.Violation? gate = SurfaceClearance.Check(safePath, eph, "inner");
        Assert.True(gate is null || gate.Value.BodyId != "giant",
            "the clamped orbit must not thread the giant.");
    }

    [Fact]
    public void InnerMoon_TooClose_HasNoSafeOrbit_AndRefusesInTheCaptainsVoice()
    {
        // Even tighter/closer: the surface floor itself threads the giant. No kept orbit clears the planet.
        var giant = new CelestialBody("giant", "Giant", null, 1e15, 2e7, 0, 0, 0, BodyKind.Planet);
        var moon = new CelestialBody("hugger", "Hugger", "giant", 3e14, 1.2e6, 2.35e7, 5e5, 0, BodyKind.Moon);
        var eph = new CircularOrbitEphemeris([giant, moon]);

        MoonOrbitClearance.Verdict v = MoonOrbitClearance.Solve(eph, moon)!.Value;
        Assert.True(v.NoSafeOrbit, "the floor itself threads the giant — no safe orbit.");
        Assert.False(v.Clamped);

        string voice = MoonOrbitClearance.RefusalText(v);
        Assert.Contains("Hugger", voice);
        Assert.Contains("Giant", voice);
        Assert.Contains("captain", voice);
    }

    [Fact]
    public void ClampVoice_ForAClampableMoon_SaysItHoldsATighterOrbit()
    {
        var eph = ClampScenario();
        MoonOrbitClearance.Verdict v = MoonOrbitClearance.Solve(eph, eph.Bodies.First(b => b.Id == "inner"))!.Value;
        string voice = MoonOrbitClearance.RefusalText(v);
        Assert.Contains("tighter orbit", voice);
        Assert.Contains("Inner", voice);
        Assert.Contains("Giant", voice);
    }
}
