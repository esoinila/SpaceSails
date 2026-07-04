namespace SpaceSails.Core;

/// <summary>
/// A scan job: aim the telescope at a bearing (radians, heliocentric ecliptic frame — same
/// atan2(Y, X) convention as every other bearing in Core, sun at the origin) and sweep an arc
/// centered on it. Sweeping is not instantaneous: it consumes sim time proportional to the arc
/// swept (vision ¶12/¶14 — a scanning-and-tracking *station*, not a free instant-reveal).
/// </summary>
public readonly record struct ScanJob(double CenterBearingRad, double ArcWidthRad)
{
    /// <summary>A full 360° survey takes this long at sim speed.</summary>
    public const double FullSurveySeconds = 6 * 3600;

    /// <summary>Sweep rate: sim seconds consumed per radian of arc swept.</summary>
    public const double SecondsPerRadian = FullSurveySeconds / (2 * Math.PI);

    /// <summary>Sim time this job takes to sweep its whole arc.</summary>
    public double DurationSeconds => ArcWidthRad * SecondsPerRadian;
}

/// <summary>
/// The ship-side telescope (worldbuilding notes §5): a passive, wide-envelope instrument whose
/// detection range depends on which way it's pointed relative to the sun. Looking straight at
/// the sun blinds it (glare swamps everything but the brightest); looking straight away from the
/// sun is the pirate's best hunting angle — dark sky, targets lit from behind you.
/// </summary>
public sealed class TelescopeModel(double baseRangeMeters = TelescopeModel.BaseRangeMetersDefault)
{
    /// <summary>~6× SensorModel.Default's 1e11 m — a telescope reaches far further than the
    /// ship's passive proximity sensor, at the cost of needing to be aimed and swept.</summary>
    public const double BaseRangeMetersDefault = 6.0e11;

    /// <summary>Range fraction when pointed straight at the sun: near-blind, not literally zero
    /// — a big enough or charged-up hull can still smear through the glare.</summary>
    public const double SunwardRangeFactor = 0.08;

    public double BaseRange { get; } = baseRangeMeters;

    /// <summary>
    /// Detection range along <paramref name="lookDirection"/> from <paramref name="shipPosition"/>.
    /// Let φ be the angle between <paramref name="lookDirection"/> and the direction from the ship
    /// to the sun (the sun sits at the origin, as elsewhere in Core):
    /// <code>
    ///   Range(φ) = BaseRange × (SunwardRangeFactor + (1 − SunwardRangeFactor) × (1 − cos φ) / 2)
    /// </code>
    /// φ = 0 (looking straight at the sun) ⇒ SunwardRangeFactor × BaseRange (~8%, near-blind).
    /// φ = π (looking straight away from the sun, anti-sunward) ⇒ BaseRange (100%). The cosine
    /// ramp is smooth and monotonic in φ across the whole range.
    /// </summary>
    public double Range(Vector2d shipPosition, Vector2d lookDirection)
    {
        Vector2d look = lookDirection.Normalized();
        if (look.LengthSquared == 0)
        {
            return BaseRange * SunwardRangeFactor;
        }

        Vector2d sunward = (Vector2d.Zero - shipPosition).Normalized();
        double cosPhi = sunward.LengthSquared == 0 ? 0 : sunward.Dot(look);
        double envelope = SunwardRangeFactor + (1 - SunwardRangeFactor) * (1 - cosPhi) / 2;
        return BaseRange * envelope;
    }
}

/// <summary>
/// A tracked contact: what the ledger last confirmed about it, and how confident that fix still
/// is. Quality is stored as of <see cref="LastConfirmedTime"/>; use <see cref="EffectiveQuality"/>
/// to fold in decay since then — kept as a pure function of sim time so nothing needs mutating
/// just to read the current state (determinism is law: same inputs, same answer, always).
/// </summary>
public readonly record struct TrackedTarget(
    string ShipId, Observation LastObservation, double LastConfirmedTime, double Quality)
{
    /// <summary>
    /// Quality at <paramref name="simTime"/>: unchanged inside the staleness horizon, then decays
    /// linearly at <see cref="TrackedTargetLedger.DecayPerStaleDay"/> per day beyond it. Clamped
    /// to zero (never negative — a stale track just stays "lost", it doesn't go debt-negative).
    /// </summary>
    public double EffectiveQuality(double simTime)
    {
        double staleSeconds = simTime - LastConfirmedTime - TrackedTargetLedger.StalenessHorizonSeconds;
        if (staleSeconds <= 0)
        {
            return Quality;
        }

        double staleDays = staleSeconds / 86400;
        return Math.Max(0, Quality - TrackedTargetLedger.DecayPerStaleDay * staleDays);
    }

    /// <summary>
    /// Prediction-cone tightening for a tracked ship (vision ¶14 / worldbuilding §5): a
    /// confidently-held track visibly sharpens the intercept. 1 at quality 0 (no help — the
    /// ordinary cone), down to 0.3 at quality 1 (a fresh, perfect reconfirm).
    /// </summary>
    public double UncertaintyScale(double simTime) => 1 - 0.7 * EffectiveQuality(simTime);
}

/// <summary>
/// The tracking-post ledger: once a passive sweep finds a ship, keeping the lock is cheap — a
/// short re-look near its predicted position confirms it's still there and bumps quality; skip
/// checks too long (or the target burns hard enough to slip the re-acquire cone) and quality
/// decays until the contact is lost. <see cref="MaxTracks"/> (telescope count) caps how many
/// ships can be held simultaneously — the natural upgrade axis (vision ¶14/¶16).
/// </summary>
public sealed class TrackedTargetLedger(int maxTracks = 1)
{
    /// <summary>Sim seconds a target can go unconfirmed before quality starts decaying.</summary>
    public const double StalenessHorizonSeconds = 5 * 86400;

    /// <summary>Quality lost per day stale beyond the horizon.</summary>
    public const double DecayPerStaleDay = 0.2;

    /// <summary>Below this quality the lock is lost and the entry is dropped.</summary>
    public const double LostThreshold = 0.05;

    /// <summary>Quality granted to a brand-new track (a first find is shaky).</summary>
    public const double InitialQuality = 0.4;

    /// <summary>Quality gained on a successful sweep re-detect or short re-confirm look.</summary>
    public const double ReconfirmGain = 0.35;

    public int MaxTracks { get; set; } = maxTracks;

    private readonly Dictionary<string, TrackedTarget> _entries = new();

    public IReadOnlyCollection<TrackedTarget> Entries => _entries.Values;

    public bool IsTracked(string shipId) => _entries.ContainsKey(shipId);

    public bool TryGet(string shipId, out TrackedTarget target) => _entries.TryGetValue(shipId, out target);

    /// <summary>
    /// Register a fresh sweep detection. A target already tracked treats this as a free
    /// reconfirm (sweeping over something you already hold costs nothing extra). A brand-new
    /// target is refused once <see cref="MaxTracks"/> is full — upgrade telescopes or drop
    /// something first.
    /// </summary>
    public bool Add(Observation observation)
    {
        if (_entries.TryGetValue(observation.TargetId, out TrackedTarget existing))
        {
            _entries[observation.TargetId] = existing with
            {
                LastObservation = observation,
                LastConfirmedTime = observation.SimTime,
                Quality = Math.Min(1.0, existing.EffectiveQuality(observation.SimTime) + ReconfirmGain),
            };
            return true;
        }

        if (_entries.Count >= MaxTracks)
        {
            return false;
        }

        _entries[observation.TargetId] = new TrackedTarget(
            observation.TargetId, observation, observation.SimTime, InitialQuality);
        return true;
    }

    /// <summary>
    /// A short, cheap directed look at a tracked target's predicted position: PathPredictor
    /// dead-reckons forward from the last observation, and the re-acquire succeeds only if the
    /// real position is still inside that predicted uncertainty cone AND in telescope range —
    /// a target that burned hard enough to leave the cone slips the re-acquire, which is the
    /// whole point of caring about track quality. Success refreshes the observation (so future
    /// predictions tighten too) and bumps quality; failure leaves the entry to decay normally.
    /// </summary>
    public bool TryConfirm(
        string shipId, ICelestialEphemeris ephemeris, TelescopeModel telescope,
        Vector2d observerPosition, ShipState actualTarget, double simTime)
    {
        if (!_entries.TryGetValue(shipId, out TrackedTarget entry))
        {
            return false;
        }

        double dt = simTime - entry.LastObservation.SimTime;
        if (dt < 0)
        {
            return false;
        }

        PredictedPath predicted = PathPredictor.Predict(ephemeris, entry.LastObservation, null, Math.Max(1, dt));
        double halfWidth = predicted.HalfWidthAt(simTime);
        Vector2d predictedPosition = predicted.Samples.Count > 0
            ? predicted.Samples[^1].Position
            : entry.LastObservation.Position;

        if ((actualTarget.Position - predictedPosition).Length > halfWidth)
        {
            return false; // not where the cone said — the short look finds nothing
        }

        Vector2d toTarget = actualTarget.Position - observerPosition;
        if (toTarget.Length > telescope.Range(observerPosition, toTarget))
        {
            return false; // right neighborhood, but past the sun-relative envelope
        }

        var refreshed = new Observation(shipId, simTime, actualTarget.Position, actualTarget.Velocity);
        _entries[shipId] = entry with
        {
            LastObservation = refreshed,
            LastConfirmedTime = simTime,
            Quality = Math.Min(1.0, entry.EffectiveQuality(simTime) + ReconfirmGain),
        };
        return true;
    }

    /// <summary>Drop any entry whose effective quality has decayed below <see cref="LostThreshold"/>
    /// by <paramref name="simTime"/> — burned hard or ignored too long, the ledger forgets it.</summary>
    public void AdvanceTime(double simTime)
    {
        List<string>? lost = null;
        foreach (KeyValuePair<string, TrackedTarget> kv in _entries)
        {
            if (kv.Value.EffectiveQuality(simTime) <= LostThreshold)
            {
                (lost ??= []).Add(kv.Key);
            }
        }

        if (lost is not null)
        {
            foreach (string id in lost)
            {
                _entries.Remove(id);
            }
        }
    }

    public bool Drop(string shipId) => _entries.Remove(shipId);
}

/// <summary>
/// Pure geometry: sweep a scan job over a batch of candidates and a corridor-watch program
/// builder for known trade routes. No mutable state — everything here is a stateless function of
/// its arguments, so the caller (the tracking-post component) owns the actual ledger/job state.
/// </summary>
public static class TrackingStation
{
    public static double Bearing(Vector2d v) => Math.Atan2(v.Y, v.X);

    /// <summary>True if <paramref name="bearing"/> lies within ±half of <paramref name="arcWidth"/>
    /// of <paramref name="centerBearing"/>, all wrapped mod 2π.</summary>
    public static bool InArc(double bearing, double centerBearing, double arcWidth) =>
        Math.Abs(NormalizeAngle(bearing - centerBearing)) <= arcWidth / 2;

    private static double NormalizeAngle(double angle)
    {
        angle %= Math.Tau;
        if (angle > Math.PI) angle -= Math.Tau;
        if (angle < -Math.PI) angle += Math.Tau;
        return angle;
    }

    /// <summary>
    /// One candidate check: true (with an <see cref="Observation"/>) if the target's bearing from
    /// the observer falls inside the swept wedge AND its distance is within the telescope's
    /// sun-relative range along that bearing at the moment the sweep completes. Deterministic —
    /// pure geometry, no randomness needed.
    /// </summary>
    public static bool TryDetect(
        TelescopeModel telescope, ScanJob job, Vector2d observerPosition,
        string targetId, ShipState target, double sweepCompleteTime, out Observation observation)
    {
        Vector2d toTarget = target.Position - observerPosition;
        double distance = toTarget.Length;
        if (distance <= 0 || !InArc(Bearing(toTarget), job.CenterBearingRad, job.ArcWidthRad))
        {
            observation = default;
            return false;
        }

        double range = telescope.Range(observerPosition, toTarget);
        if (distance > range)
        {
            observation = default;
            return false;
        }

        observation = new Observation(targetId, sweepCompleteTime, target.Position, target.Velocity);
        return true;
    }

    /// <summary>Run one completed sweep against a batch of candidates; returns every detection.</summary>
    public static IReadOnlyList<Observation> Sweep(
        TelescopeModel telescope, ScanJob job, Vector2d observerPosition,
        IEnumerable<(string Id, ShipState State)> candidates, double sweepCompleteTime)
    {
        var found = new List<Observation>();
        foreach ((string id, ShipState state) in candidates)
        {
            if (TryDetect(telescope, job, observerPosition, id, state, sweepCompleteTime, out Observation obs))
            {
                found.Add(obs);
            }
        }

        return found;
    }
}

/// <summary>One ready-made scan program: a named preset sweep for a known trade corridor.</summary>
public readonly record struct ScanProgram(string Name, ScanJob Job);

/// <summary>
/// Ready-made scanning programs for known trade routes (vision ¶14/¶16): for every pair of
/// present trade-anchor bodies, a program that sweeps the corridor between them from the ship's
/// current vantage. A pure function of (scenario bodies, ship position, time) — call it fresh
/// whenever any of those change; nothing here is cached or mutable.
/// </summary>
public static class ScanPrograms
{
    private static readonly string[] TradeAnchors = ["venus", "earth", "mars", "jupiter", "saturn"];

    /// <summary>Extra arc padding on each side of the corridor's angular spread, so a normal
    /// maneuver doesn't slip the target outside the wedge mid-sweep.</summary>
    public const double ArcMarginRad = 8.0 * Math.PI / 180.0;

    public static IReadOnlyList<ScanProgram> BuildPrograms(
        ICelestialEphemeris ephemeris, Vector2d shipPosition, double simTime)
    {
        var present = new List<CelestialBody>();
        foreach (string id in TradeAnchors)
        {
            CelestialBody? body = ephemeris.Bodies.FirstOrDefault(b => b.Id == id);
            if (body is not null)
            {
                present.Add(body);
            }
        }

        var programs = new List<ScanProgram>();
        for (int i = 0; i < present.Count; i++)
        {
            for (int j = i + 1; j < present.Count; j++)
            {
                CelestialBody a = present[i], b = present[j];
                Vector2d pa = ephemeris.Position(a.Id, simTime);
                Vector2d pb = ephemeris.Position(b.Id, simTime);
                Vector2d toMid = (pa + pb) / 2 - shipPosition;
                if (toMid.LengthSquared == 0)
                {
                    continue;
                }

                double centerBearing = TrackingStation.Bearing(toMid);
                double bearingA = TrackingStation.Bearing(pa - shipPosition);
                double bearingB = TrackingStation.Bearing(pb - shipPosition);
                double halfSpread = Math.Max(AngleDelta(centerBearing, bearingA), AngleDelta(centerBearing, bearingB));
                double arcWidth = Math.Clamp(2 * halfSpread + 2 * ArcMarginRad, ArcMarginRad, Math.Tau);

                programs.Add(new ScanProgram($"{a.Name}–{b.Name} corridor watch", new ScanJob(centerBearing, arcWidth)));
            }
        }

        return programs;
    }

    private static double AngleDelta(double a, double b)
    {
        double d = (b - a) % Math.Tau;
        if (d > Math.PI) d -= Math.Tau;
        if (d < -Math.PI) d += Math.Tau;
        return Math.Abs(d);
    }
}
