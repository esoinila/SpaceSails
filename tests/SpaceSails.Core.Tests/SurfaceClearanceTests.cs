namespace SpaceSails.Core.Tests;

/// <summary>
/// #267 — the surface-clearance constraint on a PLANNED trajectory. The planner solves in point-mass
/// space, so a conic can "reach the window" while its path runs straight through a body's interior (the
/// owner's live match-and-clamp ribbon threaded Uranus). SurfaceClearance verifies a sampled path clears
/// every body it passes — surface (or cloud tops) plus a safety band — while NOT crying wolf on a
/// legitimate arrival that deliberately ends at its target (the #196/#229 lesson).
/// </summary>
public class SurfaceClearanceTests
{
    private static ICelestialEphemeris Sol() => CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    // A path holding a fixed world offset off a body's rail across a few sample times (co-moving with it,
    // the way the #219 subsurface fixture builds its samples — the body moves between samples).
    private static List<TrajectorySample> Along(ICelestialEphemeris eph, string bodyId, Vector2d offset, params double[] times)
    {
        var path = new List<TrajectorySample>();
        foreach (double t in times)
        {
            path.Add(new TrajectorySample(t, eph.Position(bodyId, t) + offset));
        }

        return path;
    }

    private static CelestialBody Body(ICelestialEphemeris eph, string id) => eph.Bodies.First(b => b.Id == id);

    [Fact]
    public void ClearPass_OverEveryBody_IsNoViolation()
    {
        // A path coasting 500,000 km off Enceladus — far above its (and Saturn's, and the Sun's) clearance
        // band. Nothing threaded: the planner is clear to fly it.
        ICelestialEphemeris eph = Sol();
        List<TrajectorySample> path = Along(eph, "enceladus", new Vector2d(5e8, 0), 0, 50, 100);

        Assert.Null(SurfaceClearance.Check(path, eph));
    }

    [Fact]
    public void ThreadingPass_ThroughUranusInterior_IsAViolationThatThreads()
    {
        // The reported bug in Core: a line whose samples sit deep inside Uranus's disk (0.3 R from centre).
        // No arrival target — a pure fly-through — so it is judged over the whole path and flagged a strike.
        ICelestialEphemeris eph = Sol();
        CelestialBody uranus = Body(eph, "uranus");
        List<TrajectorySample> path = Along(eph, "uranus", new Vector2d(0.3 * uranus.BodyRadius, 0), 0, 50, 100);

        SurfaceClearance.Violation? v = SurfaceClearance.Check(path, eph);
        Assert.NotNull(v);
        Assert.Equal("uranus", v!.Value.BodyId);
        Assert.True(v.Value.Threads, "A line through the interior is a strike, not a band shave.");
        Assert.True(v.Value.Altitude < 0, "Subsurface altitude is negative.");
    }

    [Fact]
    public void LegitimateArrivalAtBody_JudgedFromTheAchievedPark_IsNoViolation()
    {
        // An armed approach that ends AT Enceladus: the coarse terminal coast grazes below the surface a
        // step before the insert lifts it to a safe park (the #219/#229 class). Judged from the ACHIEVED
        // final sample (a safe park), naming Enceladus as the arrival target, it is NOT a threaded planet.
        ICelestialEphemeris eph = Sol();
        CelestialBody enc = Body(eph, "enceladus");
        var path = new List<TrajectorySample>
        {
            new(0, eph.Position("enceladus", 0) + new Vector2d(5e8, 0)),          // inbound, safe
            new(50, eph.Position("enceladus", 50) + new Vector2d(0.5 * enc.BodyRadius, 0)), // transient graze
            new(100, eph.Position("enceladus", 100) + new Vector2d(5e8, 0)),      // achieved park, safe
        };

        // Named as the arrival target → the achieved park is judged: no false refusal.
        Assert.Null(SurfaceClearance.Check(path, eph, arrivalBodyId: "enceladus"));

        // Proof the exemption is what saves it: the SAME path with no arrival target reports the graze.
        SurfaceClearance.Violation? raw = SurfaceClearance.Check(path, eph);
        Assert.NotNull(raw);
        Assert.Equal("enceladus", raw!.Value.BodyId);
        Assert.True(raw.Value.Threads, "Without the arrival exemption the transient subsurface graze is a strike.");
    }

    [Fact]
    public void ArrivalTargetsParent_IsStillJudged_AMatchThatThreadsTheParentRefuses()
    {
        // The owner's exact case: match-and-clamp at The Tilt (a μ=0 station orbiting Uranus). Arriving AT
        // the station is legitimate, but the matched line dives through URANUS — the station's PARENT, a
        // different body, never exempt. So even naming the-tilt as the arrival target, Uranus refuses.
        ICelestialEphemeris eph = Sol();
        CelestialBody uranus = Body(eph, "uranus");
        List<TrajectorySample> path = Along(eph, "uranus", new Vector2d(0.4 * uranus.BodyRadius, 0), 0, 50, 100);

        SurfaceClearance.Violation? v = SurfaceClearance.Check(path, eph, arrivalBodyId: "the-tilt");
        Assert.NotNull(v);
        Assert.Equal("uranus", v!.Value.BodyId);
        Assert.True(v.Value.Threads);
    }

    [Fact]
    public void GrazeWithinTheBand_AboveTheSurface_IsAViolationButNotAThread()
    {
        // A line that shaves Uranus at 1.05 R — above the bare surface (1.0 R) but inside the 1.1 R
        // clearance band. The planner shouldn't cut it that fine: a violation, but not a strike.
        ICelestialEphemeris eph = Sol();
        CelestialBody uranus = Body(eph, "uranus");
        List<TrajectorySample> path = Along(eph, "uranus", new Vector2d(1.05 * uranus.BodyRadius, 0), 0, 50, 100);

        SurfaceClearance.Violation? v = SurfaceClearance.Check(path, eph);
        Assert.NotNull(v);
        Assert.Equal("uranus", v!.Value.BodyId);
        Assert.False(v.Value.Threads, "Above the surface but within the band is a shave, not a strike.");
        Assert.True(v.Value.Altitude > 0, "A band graze still clears the bare surface.");
    }

    [Fact]
    public void ClearanceRadius_IsSurfacePlusBand_AndCloudTopsForAnAtmosphere()
    {
        // Airless: 1.1 R, the same SurfaceParkRadii floor the autopilot parks above.
        var airless = new CelestialBody("rock", "Rock", "sun", Mu: 1e14, BodyRadius: 1e6,
            OrbitRadius: 1e11, OrbitPeriod: 1e7, InitialPhase: 0);
        Assert.Equal(1.1e6, SurfaceClearance.ClearanceRadius(airless), 3);

        // With an atmosphere the floor rises to the cloud tops plus the band (the #263 aerocapture corridor
        // will later be the ONE sanctioned exception allowed below this).
        var shelled = airless with { Atmosphere = new Atmosphere(RefDensity: 1, ScaleHeight: 1e4, TopAltitude: 2e5) };
        Assert.Equal(1e6 + 2e5 + 0.1e6, SurfaceClearance.ClearanceRadius(shelled), 3);
    }

    [Fact]
    public void RefusalText_SpeaksInTheCaptainsVoice_ThreadVersusShave()
    {
        var thread = new SurfaceClearance.Violation("uranus", "Uranus", 2.5e7, 2.75e7, 1e7, 0, default);
        Assert.Contains("threads Uranus", SurfaceClearance.RefusalText(thread));
        Assert.Contains("not with me at the helm", SurfaceClearance.RefusalText(thread));

        var shave = thread with { MinDistance = 2.6e7 }; // above the surface, inside the band
        Assert.False(shave.Threads);
        Assert.Contains("shaves Uranus too close", SurfaceClearance.RefusalText(shave));
    }

    [Fact]
    public void ShortPath_IsNeverAViolation()
    {
        ICelestialEphemeris eph = Sol();
        Assert.Null(SurfaceClearance.Check(new List<TrajectorySample>(), eph));
        Assert.Null(SurfaceClearance.Check(new List<TrajectorySample> { new(0, Vector2d.Zero) }, eph));
    }
}
