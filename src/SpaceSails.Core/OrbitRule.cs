namespace SpaceSails.Core;

/// <summary>
/// Orbital insertion around a planet (M20). Prograde-only pulses can scale the velocity vector
/// but never rotate it, so entering a planetary orbit by hand is physically impossible — this
/// is the ship system that does the turn. The window: inside the planet's Hill sphere and under
/// the relative-speed limit. The cost: honest — pulses proportional to the Δv the insertion
/// burn actually performs.
/// </summary>
public static partial class OrbitRule
{
    /// <summary>Above this relative speed the insertion burn would shred the sail. Same limit as boarding.</summary>
    public const double MaxRelativeSpeed = 5000;

    /// <summary>The indicator becomes visible within this many Hill radii — approach guidance.</summary>
    public const double IndicatorRangeHillRadii = 5;

    /// <summary>One insertion pulse buys this fraction of current heliocentric speed as Δv.</summary>
    public const double DeltaVPerPulseFraction = 0.01;

    /// <summary>
    /// M25: the armed autopilot works from this many Hill radii out. Threading the bare Hill
    /// sphere by hand is a needle at map scale (owner: prograde-only steering can't do close
    /// maneuvers — "point at it and throttle would really be used instead", so the ship's
    /// systems do exactly that, priced in pulses like every assisted burn).
    /// </summary>
    public const double CaptureRangeHillRadii = 5;

    /// <summary>Capture-range floor: Mercury's Hill sphere is ~2e8 m — invisible at plot zoom.</summary>
    public const double CaptureRangeFloorMeters = 3e9;

    /// <summary>Auto-approach closing speed as a fraction of the window's speed limit —
    /// arrives under the limit with margin for tidal drift along the fall.</summary>
    public const double ApproachSpeedFraction = 0.8;

    /// <summary>The approach aims to clear the TARGET's own surface by this factor of its body
    /// radius. A naive fall aimed at the center gravity-focuses straight into the planet — the
    /// owner's Saturn playtest flew right through it — so the aim is offset off-center by the
    /// impact parameter that puts the ballistic periapsis at this safe radius.</summary>
    public const double ApproachSafeBodyRadii = 2.0;

    /// <summary>When auto-orbiting a MOON, the approach chord is bent around its PARENT planet,
    /// kept this many parent-radii clear — so aiming at Enceladus never threads Saturn.</summary>
    public const double ParentSafeBodyRadii = 2.0;

    /// <summary>The autopilot inserts only this deep inside the Hill sphere. A manual O-press
    /// at the Hill edge is the player's choice; the autopilot parks where the sun's tide
    /// cannot strip the orbit (prograde orbits are long-term stable to roughly half Hill).</summary>
    public const double AutopilotInsertHillFraction = 0.5;

    /// <summary>Surface-margin floor for any parked/inserted orbit: never circularize below this
    /// many body radii. Enceladus is airless and its Hill sphere is only ≈ 3.8 R, so its
    /// robustly-stable park depth (≈ 0.33 Hill ≈ 1.24 R) sits below the old 1.5 R guideline — the
    /// tidal-stability constraint wins for a deep well, and 1.1 R still clears the surface with
    /// margin. Roomy moons and planets park far above this and never feel it.</summary>
    public const double SurfaceParkRadii = 1.1;

    /// <summary>Where the autopilot circularizes — the insertion gate — as a fraction of the Hill
    /// sphere. Empirically (the Enceladus 10-day / ≈36-orbit drift sweep) prograde orbits hold
    /// robustly to ≈ 0.33 Hill; nearer half-Hill they are chaotic and strip over many orbits. The
    /// old 0.5-Hill "stable to roughly half Hill" was only ever tested over Earth's ⅛-orbit — this
    /// is the depth that actually holds. Big bodies complete few orbits in 10 days, so this deeper,
    /// safer park is inert for them beyond a slightly deeper insertion.</summary>
    public const double ParkStableHillFraction = 0.33;

    /// <summary>The safe-approach periapsis for a deep well is aimed at the MIDDLE of the insert
    /// band (surface floor … park radius), so the ballistic fall dwells inside the window near
    /// periapsis and the tick loop reliably catches the insertion instead of blasting through a
    /// thin shell. Inert for big bodies, whose 2 R aim already sits far below the band.</summary>
    public const double InsertBandMidpoint = 0.5;

    /// <summary>A deep well is captured no faster than this many times its parking circular speed:
    /// screaming in at the global 4 km/s would need a monster insertion burn and skip the thin
    /// stable shell between ticks. For a roomy moon or planet the global approach speed is already
    /// far slower than this cap, so it is inert (issue #136).</summary>
    public const double ApproachCircularSpeedFactor = 8.0;

    /// <summary>Parked orbits never sit outside this fraction of the Hill sphere — the sun's tide
    /// strips the outer Hill. The park target is capped here.</summary>
    public const double ParkCeilingHillFraction = 0.9;

    /// <summary>Upper edge of the tide-STABLE park band, as a fraction of the Hill sphere. The
    /// autopilot circularizes at <see cref="ParkStableHillFraction"/> (≈ 0.33 Hill); the Enceladus
    /// 10-day / ≈36-orbit drift sweep (Lab 16) mapped robust stability out to ≈ 0.33 Hill and chaos
    /// nearer half-Hill (an orbit at ≈ 0.53 Hill strips over hours — the owner's stranded ship, #180).
    /// This 0.4-Hill edge sits a small grace above the 0.33 park so the autopilot's own insertion
    /// never trips the tide-risk verdict, while a manual park anywhere near half-Hill does.</summary>
    public const double ParkStableCeilingHillFraction = 0.4;

    /// <summary>Multiplicative grace applied to the <see cref="ParkingRadius"/> when it defines the
    /// stable-band ceiling for a very deep well (Hill so tight that the park is clamped up off the
    /// surface, above 0.4 Hill). Keeps the autopilot's own circular park just inside the band under
    /// floating-point round-off.</summary>
    public const double StableBandGrace = 1.02;

    /// <summary>Hill-sphere radius: where the body's gravity owns a satellite against its parent's tide.
    /// Uses the body's <see cref="CelestialBody.OrbitRadius"/> (semi-major axis) — the stable, mean
    /// value. For an eccentric body whose Hill sphere breathes with distance, prefer the
    /// instantaneous overload fed by <see cref="ICelestialEphemeris.InstantaneousOrbitRadius"/>.</summary>
    public static double HillRadius(CelestialBody body, double parentMu) =>
        HillRadius(body.OrbitRadius, body.Mu, parentMu);

    /// <summary>Hill-sphere radius from an explicit parent distance — pass the instantaneous
    /// distance (PR-B, Kepler rails) so an elliptical body's capture window tracks its real, changing
    /// separation instead of a fixed circle. Identical to the mean overload for a circular body.</summary>
    public static double HillRadius(double orbitRadius, double bodyMu, double parentMu) =>
        orbitRadius * Math.Pow(bodyMu / (3 * parentMu), 1.0 / 3.0);

    /// <summary>Circular-orbit speed around the body at the given distance.</summary>
    public static double CircularSpeed(CelestialBody body, double distance) =>
        Math.Sqrt(body.Mu / distance);

    /// <summary>Local circular-orbit period (s) at radius <paramref name="radius"/> around a body of
    /// gravitational parameter <paramref name="mu"/>: T = 2π·√(r³/μ). One sqrt — cheap enough to
    /// recompute every frame as the ship's radius changes.</summary>
    public static double LocalOrbitPeriod(double radius, double mu) =>
        2 * Math.PI * Math.Sqrt(radius * radius * radius / mu);

    /// <summary>
    /// #265 — the period (s) of a ship's BOUND two-body orbit about a body, or null when the orbit is
    /// not captured (non-negative energy, i.e. parabolic/hyperbolic) or the ship is outside the body's
    /// Hill sphere. The period is one full revolution — the length a captured ship's plot ribbon should
    /// draw instead of a precessing bouquet. Same energy/semi-major-axis kernel as <see cref="ParkStability"/>:
    /// a = −μ/(2·energy), T = 2π·√(a³/μ). Unbound legs (a transfer or a hyperbolic pass) return null so
    /// the caller keeps the full-length ribbon where the future genuinely extends.
    /// </summary>
    public static double? BoundOrbitPeriod(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, double hillRadius)
    {
        Vector2d r = ship.Position - bodyPosition;
        double radius = r.Length;
        if (!(radius > 0) || !(hillRadius > 0) || !(body.Mu > 0) || radius >= hillRadius)
        {
            return null;
        }

        Vector2d v = ship.Velocity - bodyVelocity;
        double mu = body.Mu;
        double energy = v.LengthSquared / 2 - mu / radius;
        if (energy >= 0)
        {
            return null; // unbound: no revolution to close
        }

        double a = -mu / (2 * energy);                          // semi-major axis (>0 when bound)
        return 2 * Math.PI * Math.Sqrt(a * a * a / mu);
    }

    /// <summary>
    /// #145 — how many seconds of a time-parameterized trajectory to DISPLAY in a co-moving frame
    /// around a Hill-sphere body. The full projection is sized for solar legs (days–weeks); drawn
    /// inside a moon system it renders as a spirograph coil (the owner's 7-day Titan approach = ~8-10
    /// overlapping laps of Saturn). So the DRAWN length scales to the frame's local timescale:
    /// ~<paramref name="periods"/> local orbital periods at the ship's current <paramref name="radius"/>,
    /// while the underlying projection/ETA math stays full length.
    ///
    /// Clamped: never shorter than <paramref name="floorSeconds"/> (the caller folds in "a few hours"
    /// and the time-to-next-plan-node + margin, so the imminent step is never hidden); never longer
    /// than <paramref name="fullHorizonSeconds"/> (when the full projection is already shorter than a
    /// local period — e.g. a wide moon far from its planet — we just draw all of it).
    ///
    /// Degenerate inputs (non-positive radius/μ, non-finite horizon) fall back to the full horizon —
    /// no truncation — so a mass-less dock or the Sun frame is a no-op for the caller.
    /// </summary>
    public static double FrameScaledWindowSeconds(
        double radius, double mu, double fullHorizonSeconds, double floorSeconds, double periods = 1.25)
    {
        if (!(radius > 0) || !(mu > 0) || !double.IsFinite(fullHorizonSeconds))
        {
            return fullHorizonSeconds;
        }
        double window = periods * LocalOrbitPeriod(radius, mu);
        window = Math.Max(window, Math.Max(0, floorSeconds));
        return Math.Min(window, fullHorizonSeconds);
    }

    /// <summary>The radius the autopilot parks/circularizes at — the insertion gate (issue #136).
    /// Robustly tide-stable (≈ 0.33 Hill, <see cref="ParkStableHillFraction"/>), a clear margin above
    /// the surface (≥ <see cref="SurfaceParkRadii"/>·R), never in the tide-stripped outer Hill
    /// (≤ 0.9 Hill). For a roomy moon or planet this is the flat 0.33·Hill; for a deep-well moon
    /// whose 0.33·Hill would collide with the surface it is clamped up off the body.</summary>
    public static double ParkingRadius(CelestialBody body, double hillRadius)
    {
        double floor = SurfaceParkRadii * body.BodyRadius;
        double ceiling = ParkCeilingHillFraction * hillRadius;
        double target = Math.Max(ParkStableHillFraction * hillRadius, floor);
        return ceiling <= floor ? floor : Math.Min(target, ceiling);
    }

    /// <summary>#286 — a hair of extra grace beyond the parent's #278 clearance band that a
    /// moon-kept orbit keeps from the parent, as a fraction of the parent's radius. So a clamped
    /// park doesn't sit exactly on the gate boundary. 0.1 R matches the surface safety band.</summary>
    public const double ParentKeepGraceRadii = 0.1;

    /// <summary>#286 — the largest kept-orbit radius about a MOON whose whole swept circle still clears
    /// the PARENT planet. A circular park of radius r about a moon at distance <paramref name="distanceToParent"/>
    /// from its parent comes within (d − r) of the parent's centre, so keeping the swept circle out of the
    /// parent's #278 <see cref="SurfaceClearance.ClearanceRadius"/> band (plus a <see cref="ParentKeepGraceRadii"/>
    /// grace) requires r ≤ d − clearance − grace. This is the ONLY new geometry the #286 clamp adds; the
    /// clearance itself is the reused surface-clearance gate, so "safe to plan over the parent" and "safe to
    /// keep an orbit beside it" speak the same number. For every shipped moon this cap is far wider than the
    /// tide-stable <see cref="ParkingRadius"/>, so the clamp is inert; it is the guard that an inner moon with
    /// a small Hill sphere and a sky-filling parent can never thread the world it circles beside.</summary>
    public static double MaxKeptRadiusUnderParent(double distanceToParent, CelestialBody parent) =>
        distanceToParent - SurfaceClearance.ClearanceRadius(parent) - ParentKeepGraceRadii * parent.BodyRadius;

    /// <summary>The upper radius of the tide-STABLE park band about a body — the widest a bound
    /// orbit's apoapsis may reach before the sun's tide starts to strip it (Lab 16 drift sweep,
    /// #180). Normally <see cref="ParkStableCeilingHillFraction"/>·Hill (0.4 Hill), a small grace
    /// above the 0.33-Hill autopilot park; for a very deep well whose park is clamped up off the
    /// surface it is the park radius itself with a hair of grace, so the autopilot's own insertion
    /// is never judged unstable.</summary>
    public static double StableParkCeiling(CelestialBody body, double hillRadius) =>
        Math.Max(ParkStableCeilingHillFraction * hillRadius, ParkingRadius(body, hillRadius) * StableBandGrace);

    /// <summary>True when a CIRCULAR park at <paramref name="radius"/> would be tide-stable: at or
    /// above the surface floor (<see cref="SurfaceParkRadii"/>·R) and at or below the stable-band
    /// ceiling (<see cref="StableParkCeiling"/>). This is exactly the test the manual Enter-orbit
    /// press needs — it circularizes at the ship's current radius — so a press in the chaotic band
    /// (the owner's ≈ 0.53-Hill Enceladus park, #180) is caught before it strands the ship.</summary>
    public static bool RadiusInStableBand(double radius, CelestialBody body, double hillRadius) =>
        radius >= SurfaceParkRadii * body.BodyRadius && radius <= StableParkCeiling(body, hillRadius);
}
