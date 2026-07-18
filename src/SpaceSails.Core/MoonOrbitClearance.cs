namespace SpaceSails.Core;

/// <summary>
/// #286 — a kept/auto orbit around a MOON must clear the parent planet it circles beside. The owner,
/// the morning after the Uranus stranding session: "we should make sure that in all Moon-docked
/// situations we do not orbit through the planet, like the issue was with Uranus." A circular park of
/// radius r about a moon at distance d from its parent comes within (d − r) of the parent's centre —
/// an orbit that looks innocent around the moon can sweep a path that skims or threads the world.
///
/// <para><b>Reuses the #278 gate, does not reinvent it.</b> The verdict is measured by flying the
/// moon's swept kept orbit (a full circle about the moon, on the rails, over one local period) through
/// <see cref="SurfaceClearance.Check"/> — the same surface-clearance law the planner already refuses a
/// threading line with. The moon itself is passed as the arrival body (a kept orbit sits AT its own
/// park radius, not a threaded body — the #229 don't-cry-wolf lesson), so only the PARENT (and any
/// other body the swept circle passes) is judged over the whole path.</para>
///
/// <para><b>The clamp bounds the radius; it does NOT redesign the autopilot.</b> Owner rulings stand:
/// the auto-orbit still ends in a KEPT orbit (station-keeping law) and the autopilot's Hill site still
/// sits on the tide-stable mean radius (<see cref="OrbitRule.ParkingRadius"/>). This only caps that
/// radius at <see cref="OrbitRule.MaxKeptRadiusUnderParent"/> when the parent binds. For every shipped
/// moon the tide-stable park is far tighter than the cap, so <see cref="Verdict.Clamped"/> is false and
/// the behaviour is unchanged — this is the guard that keeps it true for any future inner moon.</para>
/// </summary>
public static class MoonOrbitClearance
{
    /// <summary>Samples per swept kept orbit — a full circle about the moon over one local period.
    /// 720 (half-degree) is fine enough that <see cref="ClosestApproach.Passes"/> finds the true
    /// near-parent point, cheap enough to sweep every moon in a test.</summary>
    public const int DefaultSweepSamples = 720;

    /// <summary>The verdict on one moon's kept orbit against its parent.</summary>
    /// <param name="StandardRadius">The tide-stable radius the autopilot wants (<see cref="OrbitRule.ParkingRadius"/>).</param>
    /// <param name="SafeRadius">The radius it will actually keep — the standard park clamped under the
    /// parent (<see cref="OrbitRule.MaxKeptRadiusUnderParent"/>), never below the surface floor.</param>
    /// <param name="ParentDistance">The moon's distance from its parent's centre (d).</param>
    /// <param name="ParentClearance">The parent's #278 clearance radius (surface/cloud tops + band).</param>
    /// <param name="StandardNearSide">How close the STANDARD swept orbit comes to the parent's centre (d − standard).</param>
    /// <param name="SafeNearSide">How close the CLAMPED swept orbit comes to the parent's centre (d − safe).</param>
    /// <param name="Clamped">The standard park would breach the parent's band, so the radius was tightened.</param>
    /// <param name="NoSafeOrbit">Even the surface floor threads the parent — the moon has no flyable kept orbit at all.</param>
    /// <param name="StandardViolation">What the STANDARD swept orbit hits, from the reused #278 gate — the
    /// evidence for the sweep test. Null when the standard park already clears everything (every shipped moon).</param>
    public readonly record struct Verdict(
        string MoonId, string MoonName, double MoonRadius,
        string ParentId, string ParentName,
        double StandardRadius, double SafeRadius,
        double ParentDistance, double ParentClearance,
        double StandardNearSide, double SafeNearSide,
        bool Clamped, bool NoSafeOrbit,
        SurfaceClearance.Violation? StandardViolation)
    {
        /// <summary>The standard swept orbit's tightest pass belongs to the PARENT and breaches its band.</summary>
        public bool StandardThreadsParent => StandardViolation is { } v && v.BodyId == ParentId;

        /// <summary>The clamped kept orbit's altitude over the moon's own surface.</summary>
        public double SafeAltitude => SafeRadius - MoonRadius;
    }

    /// <summary>Solve and judge the kept orbit at <paramref name="moon"/>. Returns null for a body that is
    /// not an orbit-able moon (no parent, μ = 0, or not a <see cref="BodyKind.Moon"/>).</summary>
    public static Verdict? Solve(
        ICelestialEphemeris ephemeris, CelestialBody moon, double simTime = 0.0, int samples = DefaultSweepSamples)
    {
        if (moon.ParentId is null || !(moon.Mu > 0) || moon.Kind != BodyKind.Moon)
        {
            return null;
        }

        CelestialBody? parent = FindBody(ephemeris, moon.ParentId);
        if (parent is null)
        {
            return null;
        }

        double distance = ephemeris.InstantaneousOrbitRadius(moon.Id, simTime);
        double hill = OrbitRule.HillRadius(moon.OrbitRadius, moon.Mu, parent.Mu);
        double standard = OrbitRule.ParkingRadius(moon, hill);
        double parentClearance = SurfaceClearance.ClearanceRadius(parent);
        double cap = OrbitRule.MaxKeptRadiusUnderParent(distance, parent);
        double floor = OrbitRule.SurfaceParkRadii * moon.BodyRadius;

        bool noSafe = cap < floor;                       // even the tightest legal park threads the parent
        double safe = noSafe ? floor : Math.Min(standard, cap);
        bool clamped = !noSafe && safe < standard * (1 - 1e-9);

        // Reuse the #278 gate on the STANDARD swept orbit — the evidence of a real violation (if any).
        IReadOnlyList<TrajectorySample> stdPath = SweptPath(ephemeris, moon, standard, simTime, samples);
        SurfaceClearance.Violation? stdViol = SurfaceClearance.Check(stdPath, ephemeris, moon.Id);

        return new Verdict(
            moon.Id, moon.Name, moon.BodyRadius, parent.Id, parent.Name,
            standard, safe, distance, parentClearance,
            distance - standard, distance - safe, clamped, noSafe, stdViol);
    }

    /// <summary>The kept circular orbit about <paramref name="moon"/> at <paramref name="radius"/>, sampled
    /// as a time-stamped path over one local orbital period, with the moon (and its parent) advancing on the
    /// rails between samples. This is the faithful swept trajectory the #278 gate (and the sweep test) fly:
    /// a co-rotating epicycle whose near side reaches d − radius of the parent. Ready for
    /// <see cref="SurfaceClearance.Check"/> / <see cref="ClosestApproach.Passes"/>.</summary>
    public static IReadOnlyList<TrajectorySample> SweptPath(
        ICelestialEphemeris ephemeris, CelestialBody moon, double radius, double simTime, int samples = DefaultSweepSamples)
    {
        int n = Math.Max(2, samples);
        double period = OrbitRule.LocalOrbitPeriod(radius, moon.Mu);
        var path = new List<TrajectorySample>(n + 1);
        for (int i = 0; i <= n; i++)
        {
            double f = (double)i / n;
            double t = simTime + period * f;
            double theta = 2 * Math.PI * f;
            Vector2d moonPos = ephemeris.Position(moon.Id, t);
            path.Add(new TrajectorySample(
                t, moonPos + new Vector2d(radius * Math.Cos(theta), radius * Math.Sin(theta))));
        }

        return path;
    }

    /// <summary>The refuse/warn line, in the captain's voice (the caller adds its channel emoji). A moon
    /// with NO safe orbit is refused outright; a clampable one is a note that the autopilot will hold the
    /// tighter orbit — never thread the planet silently (#286).</summary>
    public static string RefusalText(Verdict verdict) =>
        verdict.NoSafeOrbit
            ? $"{verdict.MoonName} hugs {verdict.ParentName} too tightly for any kept orbit that clears the planet, captain — no park there, not with me at the helm"
            : $"{verdict.MoonName}'s standard park would skim {verdict.ParentName} — I'll hold a tighter orbit that clears the planet, captain";

    private static CelestialBody? FindBody(ICelestialEphemeris ephemeris, string id)
    {
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.Id == id)
            {
                return body;
            }
        }

        return null;
    }
}
