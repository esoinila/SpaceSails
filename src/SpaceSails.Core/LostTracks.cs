namespace SpaceSails.Core;

/// <summary>
/// A track the ledger lost custody of (SundaySecondPlan F2): the last confirmed observation and
/// the search region where the hull must still be. The region's center dead-reckons along the
/// ballistic prediction; its radius grows exactly like the prediction cone (the same velocity
/// noise + plausible burst + continuous-maneuver terms — a lost lock is just a cone we can no
/// longer see the tip of). A completed search pass that finds nothing still narrows the region
/// (recorded via <see cref="LostTrackLedger.RecordSearchPass"/>), rebasing the growth.
/// </summary>
public readonly record struct LostTrack(
    string ShipId, Observation LastObservation, double LostTime, double RebasedRadius, double RebaseTime)
{
    /// <summary>Search radius at <paramref name="simTime"/>: the rebased radius plus cone-style
    /// growth since the last rebase (velocity sigma + plausible impulsive burst, linear; plus
    /// the continuous-maneuver quadratic term).</summary>
    public double SearchRadius(double simTime)
    {
        double dt = Math.Max(0, simTime - RebaseTime);
        double impulse = PredictedPath.PlausibleBurstPulses
            * PredictedPath.ImpulsePulseFraction * LastObservation.Velocity.Length;
        return RebasedRadius + (PredictedPath.VelocitySigma + impulse) * dt
            + 0.5 * NpcShip.DefaultManeuverBudget * dt * dt;
    }

    /// <summary>True once the region has grown past any practical single-instrument search —
    /// the trail is cold and the ledger should let the case go.</summary>
    public bool IsColdCase(double simTime) => SearchRadius(simTime) > LostTrackLedger.ColdCaseRadiusMeters;
}

/// <summary>
/// The scanning desk's cold-case board: every lost lock and its live search region. Mutable
/// sibling of <see cref="TrackedTargetLedger"/> — entries arrive when the ledger drops a track,
/// shrink when searched, and leave on re-acquire or when the trail goes cold.
/// </summary>
public sealed class LostTrackLedger
{
    /// <summary>Region radius beyond which a search is hopeless (the whole neighborhood of a
    /// planet); the entry becomes a cold case and is dropped.</summary>
    public const double ColdCaseRadiusMeters = 5e10;

    /// <summary>A completed search pass that finds nothing rules out sky: the region rebases to
    /// this fraction of its current radius.</summary>
    public const double ShrinkOnPass = 0.6;

    private readonly Dictionary<string, LostTrack> _entries = new();

    public IReadOnlyCollection<LostTrack> Entries => _entries.Values;

    public bool IsLost(string shipId) => _entries.ContainsKey(shipId);

    public bool TryGet(string shipId, out LostTrack lost) => _entries.TryGetValue(shipId, out lost);

    /// <summary>A fresh case never starts wider than this: a track that decayed to loss over
    /// many days has a cone far bigger than any search, but the desk still opens the case on
    /// the most plausible half of it — rule out the center first, then let it go cold.</summary>
    public const double InitialRadiusCapMeters = ColdCaseRadiusMeters / 2;

    /// <summary>File a track the ledger just dropped. The initial region radius is the
    /// prediction cone's half-width at the moment of loss (the search starts exactly as wide
    /// as the uncertainty that broke the lock), capped at <see cref="InitialRadiusCapMeters"/>
    /// so every new case starts searchable.</summary>
    public void AddFrom(TrackedTarget dropped, double simTime)
    {
        double impulse = PredictedPath.PlausibleBurstPulses
            * PredictedPath.ImpulsePulseFraction * dropped.LastObservation.Velocity.Length;
        var cone = new PredictedPath(
            dropped.LastObservation, Array.Empty<TrajectorySample>(), ImpulseBudget: impulse);
        double radius = Math.Min(cone.HalfWidthAt(simTime), InitialRadiusCapMeters);
        _entries[dropped.ShipId] = new LostTrack(
            dropped.ShipId, dropped.LastObservation, simTime, radius, simTime);
    }

    /// <summary>A search pass over the region completed without a find: rebase the radius down
    /// by <see cref="ShrinkOnPass"/> as of <paramref name="simTime"/>.</summary>
    public void RecordSearchPass(string shipId, double simTime)
    {
        if (_entries.TryGetValue(shipId, out LostTrack lost))
        {
            _entries[shipId] = lost with
            {
                RebasedRadius = lost.SearchRadius(simTime) * ShrinkOnPass,
                RebaseTime = simTime,
            };
        }
    }

    /// <summary>Remove and return every entry whose region has gone cold by <paramref name="simTime"/>.</summary>
    public IReadOnlyList<LostTrack> DropColdCases(double simTime)
    {
        List<LostTrack>? cold = null;
        foreach (KeyValuePair<string, LostTrack> kv in _entries)
        {
            if (kv.Value.IsColdCase(simTime))
            {
                (cold ??= []).Add(kv.Value);
            }
        }

        if (cold is not null)
        {
            foreach (LostTrack lost in cold)
            {
                _entries.Remove(lost.ShipId);
            }
        }

        return cold ?? (IReadOnlyList<LostTrack>)[];
    }

    public bool Drop(string shipId) => _entries.Remove(shipId);
}

/// <summary>Pure re-acquire check for a lost-search telescope pass.</summary>
public static class LostSearchRule
{
    /// <summary>Where the search region is centered now: the ballistic dead-reckoning of the
    /// last observation (gravity is public knowledge; only the burns since are not).</summary>
    public static Vector2d PredictedCenter(ICelestialEphemeris ephemeris, LostTrack lost, double simTime)
    {
        double horizon = Math.Max(1, simTime - lost.LastObservation.SimTime);
        PredictedPath path = PathPredictor.Predict(ephemeris, lost.LastObservation, null, horizon);
        return path.Samples.Count > 0 ? path.Samples[^1].Position : lost.LastObservation.Position;
    }

    /// <summary>
    /// One completed search pass: found again only if the hull really is inside the current
    /// search region AND within the telescope's sun-relative range. On success the caller feeds
    /// the observation back to the tracked ledger and drops the lost entry; on failure, call
    /// <see cref="LostTrackLedger.RecordSearchPass"/> so the region narrows.
    /// </summary>
    public static bool TryReacquire(
        ICelestialEphemeris ephemeris, TelescopeModel telescope, LostTrack lost,
        Vector2d observerPosition, ShipState actualTarget, double simTime, out Observation observation)
    {
        Vector2d center = PredictedCenter(ephemeris, lost, simTime);
        if ((actualTarget.Position - center).Length > lost.SearchRadius(simTime))
        {
            observation = default;
            return false;
        }

        Vector2d toTarget = actualTarget.Position - observerPosition;
        if (toTarget.Length > telescope.Range(observerPosition, toTarget))
        {
            observation = default;
            return false;
        }

        observation = new Observation(lost.ShipId, simTime, actualTarget.Position, actualTarget.Velocity);
        return true;
    }
}
