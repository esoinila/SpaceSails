namespace SpaceSails.Core;

/// <summary>
/// #251 · THE HUNTER'S FLIGHT — spawning one, the nearest policed body, holding station, advancing on the
/// captain, predicting the path, and breaking off.
///
/// <para>Split out of <c>EncounterRule.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class EncounterRule
{
    /// <summary>One hunter, fitting out at the nearest policed body — parked there (riding the
    /// body's own orbital velocity) until <see cref="HunterFittingOutDays"/> pass.</summary>
    public static HunterState SpawnHunter(string id, string callsign, string originBodyId,
        Vector2d originPosition, Vector2d originVelocity, double simTime, string? warrant = null) =>
        new(id, callsign, originBodyId, simTime, simTime + HunterFittingOutDays * DaySeconds,
            new ShipState(originPosition, originVelocity, simTime), CaughtPlayer: false, BrokenOff: false,
            Warrant: warrant);

    /// <summary>Nearest planet inside <see cref="PolicedThresholdMeters"/> that isn't a haven —
    /// where hired muscle comes from (Earth/Mars in Sol; central, policed space generally). Null
    /// if nothing policed is reachable — a pure outer-reaches scenario has no cavalry to call.</summary>
    public static CelestialBody? NearestPolicedBody(ICelestialEphemeris ephemeris, Vector2d playerPosition, double simTime)
    {
        CelestialBody? best = null;
        double bestDistance = double.MaxValue;
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.IsHaven || body.ParentId is null)
            {
                continue; // havens shelter pirates, not hunters; the sun itself isn't a "body"
            }

            Vector2d position = ephemeris.Position(body.Id, simTime);
            if (position.Length >= PolicedThresholdMeters)
            {
                continue; // outer reaches — no policed muscle stationed out here
            }

            double distance = (position - playerPosition).Length;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = body;
            }
        }

        return best;
    }

    /// <summary>#580 · THE CHASE HOLDS ITS BREATH WHILE THE CAPTAIN IS OFF THE SHIP.
    ///
    /// <para>Owner, walking Miranda with his ship docked and empty behind him: <i>"zero heat against empty
    /// ship (we don't have any playing there, clearly there is some lights on keeper on ship but we do not
    /// play that)"</i>, <i>"the heat must follow the captain, not the ship"</i>, and the play argument that
    /// settles it — <i>"we don't want to be guarding our parking lot ... that is not good game play :-D"</i>.</para>
    ///
    /// <para>He is right, and the bug was live: the pursuit loop kept running through an excursion, so a
    /// hunter could reach and CATCH a hull with nobody aboard it, opening a boarding demand at a captain
    /// standing in a suit on a moon. Whatever the collectors want, they want it from the person, and the
    /// person is not there.</para>
    ///
    /// <para>Holding is not the same as freezing. The hunter's clock is carried forward to
    /// <paramref name="simTime"/> so that coming back aboard resumes the chase from where it stood, rather
    /// than letting the pursuit integrate the whole excursion in one burst and land on the captain the
    /// instant they climb the ladder. Position and velocity are untouched: the wolf waited.</para></summary>
    public static HunterState HoldStation(HunterState hunter, double simTime) =>
        hunter with { State = hunter.State with { SimTime = simTime } };

    /// <summary>Dumb, relentless pursuit: thrust-limited acceleration toward the player's CURRENT
    /// position, integrated over whatever <paramref name="simTime"/> delta the caller advances by
    /// (Map.razor calls this in <see cref="HunterStepSeconds"/> quanta to match the NPC cadence).
    /// Before <see cref="HunterState.ActivationSimTime"/> the hunter just coasts on the velocity
    /// it was parked with (still fitting out); once caught or broken off it holds still, a spent
    /// contact the caller is free to retire.</summary>
    public static HunterState AdvanceHunter(HunterState hunter, ShipState player, double simTime)
    {
        if (hunter.CaughtPlayer || hunter.BrokenOff)
        {
            return hunter;
        }

        double dt = simTime - hunter.State.SimTime;
        if (dt <= 0)
        {
            return hunter;
        }

        // Coasting cases — the hunter drifts on its current velocity, unable to refine the chase:
        // still fitting out, peeled off after a warning shot, or blinded by the sun behind the
        // player. In every case it stops closing until the condition clears.
        if (simTime < hunter.ActivationSimTime
            || simTime < hunter.PeeledUntilSimTime
            || SunBlinded(hunter.State.Position, player.Position))
        {
            Vector2d coasted = hunter.State.Position + hunter.State.Velocity * dt;
            return hunter with { State = new ShipState(coasted, hunter.State.Velocity, simTime) };
        }

        Vector2d toPlayer = player.Position - hunter.State.Position;
        Vector2d accelDirection = toPlayer.Normalized();
        Vector2d newVelocity = hunter.State.Velocity + accelDirection * HunterAccelMps2 * dt;
        Vector2d newPosition = hunter.State.Position + hunter.State.Velocity * dt;
        var newState = new ShipState(newPosition, newVelocity, simTime);

        double distance = (newPosition - player.Position).Length;
        double relativeSpeed = (newVelocity - player.Velocity).Length;
        bool caught = distance < CatchRadiusMeters && relativeSpeed < CatchRelativeSpeedMetersPerSecond;

        return hunter with { State = newState, CaughtPlayer = caught };
    }

    /// <summary>
    /// Fire control's hunter special case (the aim-solution fork): a hunter flies the PURSUIT
    /// LAW, not gravity — dead-reckoning it through the Simulator (the standard freighter
    /// estimate) is wrong twice over: it adds a solar pull the hunter never feels AND drops the
    /// <see cref="HunterAccelMps2"/> it relentlessly adds toward the player. That error is
    /// ½·a·τ² ≈ 13,000 km on a 2 h slug flight, against OrdnanceRule's 5e5 m hit radius — a
    /// guaranteed structural miss on anything past a knife-fight. So REPLAY
    /// <see cref="AdvanceHunter"/> itself, in its own <see cref="HunterStepSeconds"/> quanta,
    /// against the player's PLOTTED course: to our own gun deck the pursuit law is public
    /// knowledge the way gravity is to PathPredictor — the only honest unknown is whether the
    /// player keeps to the plot (burn off it and you bend your own firing solution: the
    /// collector chases the real you, not the plan). Cost: horizon/60 s of plain additions and
    /// one recorded knot per <paramref name="maxKnots"/> stride — the hunter stays exactly as
    /// light as it flies. The hunter's true track is itself piecewise-linear at these quanta
    /// (Euler positions), so undecimated knots are EXACT, not sampled. A predicted catch or
    /// break-off freezes the track (a spent contact holds still). The final knot always lands
    /// exactly on the horizon, so <c>Samples[^1].Position</c> is the aim point at t_hit.
    /// </summary>
    public static IReadOnlyList<TrajectorySample> PredictHunterPath(
        HunterState hunter,
        IReadOnlyList<TrajectorySample> playerPath,
        double horizonSeconds,
        int maxKnots = 4000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxKnots);
        double end = hunter.State.SimTime + Math.Max(0, horizonSeconds);
        int totalSteps = (int)Math.Ceiling(Math.Max(0, horizonSeconds) / HunterStepSeconds);
        // Decimation for months-long horizons: integrate every quantum, record every stride-th.
        // Between recorded knots a linear read sags by at most ⅛·a·(stride·60)² — under the
        // 5e5 m hit radius up to ~40 min strides, i.e. horizons beyond a month.
        int stride = totalSteps / maxKnots + 1;

        var samples = new List<TrajectorySample>(Math.Min(totalSteps, maxKnots) + 2)
        {
            new(hunter.State.SimTime, hunter.State.Position),
        };

        HunterState h = hunter;
        int step = 0;
        int cursor = 0; // step times only grow — resume each path search where the last ended
        while (h.State.SimTime < end && !h.CaughtPlayer && !h.BrokenOff)
        {
            double stepTime = Math.Min(end, h.State.SimTime + HunterStepSeconds);
            h = AdvanceHunter(h, PlayerStateAt(playerPath, stepTime, ref cursor), stepTime);
            step++;
            if (step % stride == 0 || h.State.SimTime >= end)
            {
                samples.Add(new TrajectorySample(h.State.SimTime, h.State.Position));
            }
        }

        if (samples[^1].SimTime < end)
        {
            samples.Add(new TrajectorySample(end, samples[^1].Position));
        }

        return samples;
    }

    /// <summary>The player's plotted state at a sim time: position interpolated linearly along
    /// the path, velocity the local segment's slope (all <see cref="AdvanceHunter"/>'s steering
    /// and catch check need). Beyond either end the nearest leg extrapolates — a shot aimed past
    /// the plot's horizon is reaching past what the plot honestly knows anyway.
    /// <paramref name="cursor"/> is a monotonic resume hint: query times only ever grow, so the
    /// whole replay walks the path once instead of rescanning it every quantum.</summary>
    private static ShipState PlayerStateAt(IReadOnlyList<TrajectorySample> path, double simTime, ref int cursor)
    {
        if (path.Count == 0)
        {
            throw new ArgumentException("player path must hold at least one sample", nameof(path));
        }

        if (path.Count == 1)
        {
            return new ShipState(path[0].Position, Vector2d.Zero, simTime);
        }

        while (cursor < path.Count - 2 && path[cursor + 1].SimTime < simTime)
        {
            cursor++;
        }

        TrajectorySample a = path[cursor], b = path[cursor + 1];
        double span = b.SimTime - a.SimTime;
        if (span <= 0)
        {
            return new ShipState(b.Position, Vector2d.Zero, simTime);
        }

        Vector2d velocity = (b.Position - a.Position) / span;
        double f = (simTime - a.SimTime) / span;
        return new ShipState(a.Position + (b.Position - a.Position) * f, velocity, simTime);
    }

    /// <summary>The player has stayed hidden at a haven this long — the hunter loses the scent.
    /// <paramref name="hiddenDurationSeconds"/> is however long the caller has tracked continuous
    /// haven orbit; Map.razor owns that clock since it depends on the player's live flight path,
    /// not anything this pure function can see on its own.</summary>
    public static HunterState ApplyBreakOff(HunterState hunter, double hiddenDurationSeconds) =>
        !hunter.CaughtPlayer && !hunter.BrokenOff && hiddenDurationSeconds >= BreakOffHiddenDays * DaySeconds
            ? hunter with { BrokenOff = true }
            : hunter;
}
