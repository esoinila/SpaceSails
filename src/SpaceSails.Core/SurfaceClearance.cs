namespace SpaceSails.Core;

/// <summary>
/// #267 — the surface-clearance constraint on a PLANNED trajectory. The planner solves in point-mass
/// space: a conic is "valid" the instant it reaches the window, even when its path crosses a body's
/// interior — the owner's live match-and-clamp ribbon threaded straight through Uranus's disk. This is
/// the one gate that asks the other question the point-mass solve never does: does the proposed line
/// CLEAR every body it passes?
///
/// <para><b>One kernel, not a second path-flyer.</b> It reuses the single path-scanning routine
/// (<see cref="ClosestApproach.Passes"/>, which coarse-strides then parabola-refines the tightest pass
/// per body) — the one-truth law. For each body along the sampled path it compares that tightest pass to
/// a clearance radius: the body's surface (its cloud tops for an atmosphere-bearing body) plus a safety
/// band. A pass inside that band is a <see cref="Violation"/>; a pass below the bare surface is a
/// <see cref="Violation.Threads"/> — the line runs through the planet, the reported bug exactly.</para>
///
/// <para><b>Do not cry wolf on a legitimate arrival (#196/#229 lesson).</b> An armed approach deliberately
/// ENDS at its target — a dock, an orbit insertion. The coarse terminal coast grazes the target's surface
/// a step before the insert lifts it back to a safe park, and that graze is the machinery working, not a
/// threaded planet (the same false-positive class <see cref="AutopilotRehearsal.PlanCollisionPass"/> fixed
/// for the alarm). So the caller names the <paramref name="arrivalBodyId"/> and this judges THAT body from
/// the ACHIEVED final sample (the park/berth), while still judging every OTHER body over the whole path.
/// The target's PARENT planet is never the arrival body, so a match that threads the parent still refuses.</para>
///
/// <para><b>The #263 aerocapture corridor is the future SANCTIONED exception:</b> a plan that DELIBERATELY
/// aims a periapsis inside an atmosphere shell to brake. Until that lane lands, cloud tops are just more
/// surface to clear — no plan is allowed to dip below them here.</para>
///
/// <para>Pure and tiny by design: a function of the path, the rails, and the named arrival — no clock, no
/// randomness. Client WASM and any server replay agree on the verdict.</para>
/// </summary>
public static class SurfaceClearance
{
    /// <summary>The safety band above the surface (or cloud tops), as a fraction of the body's radius.
    /// 0.1 R puts the clearance floor at 1.1·R — the same <see cref="OrbitRule.SurfaceParkRadii"/> floor
    /// the autopilot already refuses to circularize below, so "clear to plan over it" and "safe to park
    /// above it" speak the same number.</summary>
    public const double SafetyBandBodyRadii = 0.1;

    /// <summary>The minimum clean separation the planner must keep from a body's centre: its surface — or
    /// its cloud tops (<see cref="Atmosphere.TopAltitude"/> for an atmosphere-bearing body) — plus the
    /// <see cref="SafetyBandBodyRadii"/> band. A planned pass tighter than this is a clearance violation.</summary>
    public static double ClearanceRadius(CelestialBody body)
    {
        double cloudTops = body.BodyRadius + (body.Atmosphere?.TopAltitude ?? 0.0);
        return cloudTops + SafetyBandBodyRadii * body.BodyRadius;
    }

    /// <summary>A body the planned line fails to clear, at its tightest pass.</summary>
    /// <param name="ClearanceRadius">The floor this pass fell under (<see cref="ClearanceRadius"/>).</param>
    /// <param name="MinDistance">The ship↔body separation at the tightest (judged) pass.</param>
    public readonly record struct Violation(
        string BodyId, string BodyName, double BodyRadius, double ClearanceRadius,
        double MinDistance, double SimTime, Vector2d ShipPosition)
    {
        /// <summary>The line runs BELOW the bare surface — it threads the body's interior (a strike),
        /// not merely a shave through the safety band.</summary>
        public bool Threads => MinDistance < BodyRadius;

        /// <summary>How tight the pass is against the clearance floor — &lt;1 inside the band, and the
        /// smaller the worse (0 = dead-centre of the body). The check keeps the smallest.</summary>
        public double Severity => ClearanceRadius > 0 ? MinDistance / ClearanceRadius : 0;

        /// <summary>Clean altitude over the bare surface at the tightest pass (negative when subsurface).</summary>
        public double Altitude => MinDistance - BodyRadius;
    }

    /// <summary>
    /// The single worst clearance violation along <paramref name="path"/>, or null when the whole line
    /// clears every body's band. Reuses <see cref="ClosestApproach.Passes"/> for the tightest pass per
    /// body, then measures each against <see cref="ClearanceRadius"/>. When <paramref name="arrivalBodyId"/>
    /// is given, that body is judged from the ACHIEVED final sample (a legitimate arrival AT it is not a
    /// threaded planet — the #229 lesson); every other body, including the target's parent, is judged over
    /// the whole path.
    /// </summary>
    /// <param name="path">The planned/rehearsed trajectory to verify (needs ≥ 2 samples).</param>
    /// <param name="ephemeris">The same rails the sim flies.</param>
    /// <param name="arrivalBodyId">The body the plan deliberately ends at (dock/insertion target), or null
    /// for a pure fly-through (a long-haul coast, a plotted burn) that arrives at no body.</param>
    public static Violation? Check(
        IReadOnlyList<TrajectorySample> path,
        ICelestialEphemeris ephemeris,
        string? arrivalBodyId = null)
    {
        if (path.Count < 2)
        {
            return null;
        }

        TrajectorySample arrival = path[^1];
        Violation? worst = null;
        foreach (ClosestApproach.Pass pass in ClosestApproach.Passes(path, ephemeris))
        {
            if (FindBody(ephemeris, pass.BodyId) is not { } body)
            {
                continue;
            }

            double minDistance = pass.Distance;
            double simTime = pass.SimTime;
            Vector2d shipPos = pass.ShipPosition;

            // #267/#229: the arrival target is judged from the ACHIEVED final sample — not the transient
            // approach graze the insert/clamp resolves. Its parent planet is a different body, judged raw
            // over the whole path, so a line that threads the parent still refuses.
            if (arrivalBodyId is not null && pass.BodyId == arrivalBodyId)
            {
                minDistance = (arrival.Position - ephemeris.Position(pass.BodyId, arrival.SimTime)).Length;
                simTime = arrival.SimTime;
                shipPos = arrival.Position;
            }

            double clearance = ClearanceRadius(body);
            if (minDistance >= clearance)
            {
                continue; // clean over this body
            }

            var violation = new Violation(
                body.Id, body.Name, body.BodyRadius, clearance, minDistance, simTime, shipPos);
            if (worst is null || violation.Severity < worst.Value.Severity)
            {
                worst = violation;
            }
        }

        return worst;
    }

    /// <summary>The refusal, in the captain's voice (owner: "refusal with the reason") — a threaded planet
    /// gets the plain-spoken line, a band shave the softer one. The caller adds its own channel emoji.</summary>
    public static string RefusalText(Violation violation) =>
        violation.Threads
            ? $"that line threads {violation.BodyName}, captain — not with me at the helm"
            : $"that line shaves {violation.BodyName} too close, captain — not with me at the helm";

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
