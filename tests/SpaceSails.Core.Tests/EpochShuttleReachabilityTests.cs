namespace SpaceSails.Core.Tests;

/// <summary>
/// The epoch shuttle-board law (owner, live playtest 2026-07-19: "Is there a place to land with the
/// shuttle on the Jupiter system... some moon etc?"). A fresh docked start at any gas-giant haven must
/// show at least one landable moon on the shuttle-bay board — the board must never boot empty.
///
/// <para>The shuttle reaches <see cref="ShuttleRange.RangeMeters"/> (5e8 m). A moon and a station orbit
/// the same parent at different radii; their separation at epoch t=0 follows purely from the two orbit
/// radii and the phase difference: d² = r₁² + r₂² − 2·r₁·r₂·cos(Δφ), minimised (to |r₁ − r₂|) when the
/// phases coincide. #-fix re-phased two moons so a giant-haven docked start has a landable in reach at
/// t=0 with wide distance margin, and stays reachable for the opening sim days as the phases drift:</para>
/// <list type="bullet">
/// <item><b>Ganymede → phase 2.2</b> (= The Red Eye, Δφ=0): epoch separation |1.0704e9 − 8.5e8| ≈ 2.20e8 m
/// — the minimum for this pair. The forward in-range window is ≈ 1.3 days (≈ 31 h to drift-out; ≈ 2.6 d
/// total; synodic period ≈ 17.3 d). Ganymede gives the more robust window than Europa (2.6 d vs 2.4 d), so
/// it is the re-phased Jupiter moon.</item>
/// <item><b>Titan → phase 5.15</b> (Ringside is at 5.0, so Δφ=+0.15): epoch separation ≈ 2.31e8 m. Titan is
/// placed slightly AHEAD of Ringside, not on top of it: because Titan and Ringside orbit at nearly the same
/// rate (synodic period ≈ 114 d), a Δφ=0 co-location would leave Titan looming beside the station for ~13 d
/// and its gravity would wreck Ringside's #155 last-mile auto-rendezvous (the clean 2-pulse phasing bus
/// becomes a 34-pulse correction fight — see AutopilotRehearsalTests.StationRun_WithSchedule...). Starting
/// Titan ahead makes it recede, so the rendezvous corridor stays clean while the shuttle still has a ≈ 4.2-day
/// forward landing window.</item>
/// </list>
///
/// <para>Only phases moved — no orbit radius, eccentricity, period or station orbit changed — so the
/// Kepler-rails e=0 byte-identical gate (<see cref="EphemerisTests.Eccentricity0_ByteIdenticalToLegacyCircularFormula"/>)
/// still holds. Enceladus (min approach ≈ 1.11e9 m from Ringside, a haven-flagged moon) stays out of
/// shuttle reach on purpose — the standing tease.</para>
///
/// <para>The berth-matrix audit closed the last two groundless berths by ADDING a landable to each —
/// no re-phase, since a moon whose orbit radius plus the station's stays under the 5e8 reach is in range
/// at EVERY phase (max separation r₁+r₂ &lt; range), so the window is permanent, not a drift-out slot:</para>
/// <list type="bullet">
/// <item><b>Triton off The Deep</b>: The Deep at 6.0e7 m, Triton at 3.5476e8 m about Neptune (both phase
/// 2.9 → epoch separation |3.5476e8 − 6.0e7| = 2.9476e8 m). Max separation r₁+r₂ = 4.1476e8 m &lt; 5e8 m,
/// so Triton is reachable at every phase — permanent window. Triton is REAL-retrograde (negative
/// orbitPeriodS): the e=0 rails carry the sign cleanly (Position uses the same angle formula, so the
/// byte-identical gate is unmoved; TransferPlanner guards period &gt; 0; the alert board already Math.Abs-es it).</item>
/// <item><b>The Clinker off Cinder Roost</b>: Cinder Roost at 1.5e7 m, The Clinker (a small captured cinder,
/// μ≈4.5e3) at 2.2e7 m about Venus (both phase 1.3 → epoch separation 7.0e6 m). Max separation 3.7e7 m ≪ 5e8 m
/// — permanent window. A tiny-well body (Hill barely clears its 2 km radius), it is a land-on-it skerry, not an
/// orbit-insertion target, so it is Phobos-exempt from the sane-parked-orbit invariant.</item>
/// </list>
/// </summary>
public class EpochShuttleReachabilityTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    /// <summary>The shuttle-bay board for a docked start at <paramref name="stationId"/> at epoch t=0,
    /// built through the real production filter (<see cref="ShuttleExcursion.Destinations"/>) from the
    /// shipped ephemeris — so this pins exactly what the player sees on booting docked there.</summary>
    private static IReadOnlyList<ShuttleExcursion.Destination> BoardAt(
        CircularOrbitEphemeris eph, string stationId)
    {
        Vector2d here = eph.Position(stationId, 0);
        var candidates = eph.Bodies.Select(b => new ShuttleExcursion.Candidate(
            BodyId: b.Id,
            Kind: b.Kind,
            ParentId: b.ParentId,
            DistanceMeters: (eph.Position(b.Id, 0) - here).Length,
            BodyRadiusMeters: b.BodyRadius,
            HasInterior: b.Kind == BodyKind.Station,
            HasCache: false));
        return ShuttleExcursion.Destinations(candidates, dockedBodyId: stationId);
    }

    private static double SeparationAtEpoch(CircularOrbitEphemeris eph, string a, string b) =>
        (eph.Position(a, 0) - eph.Position(b, 0)).Length;

    // ── The fix: a giant-haven docked start shows a landable moon at epoch ──

    [Fact]
    public void RedEye_HasALandableMoonInShuttleRange_AtEpoch()
    {
        CircularOrbitEphemeris eph = Sol();

        IReadOnlyList<ShuttleExcursion.Destination> board = BoardAt(eph, "red-eye");
        Assert.Contains(board, d => d.IsLandableSurface);
        // Concretely the re-phased Ganymede, within one hop with wide margin (≈2.20e8 m ≪ 5e8 m).
        Assert.Contains(board, d => d.BodyId == "ganymede" && d.IsLandableSurface);

        double sep = SeparationAtEpoch(eph, "red-eye", "ganymede");
        Assert.True(ShuttleRange.InRange(sep), $"Ganymede is {sep:0.###e0} m from The Red Eye — out of shuttle range.");
        Assert.Equal(2.204e8, sep, 2e6); // |1.0704e9 − 8.5e8|, phases coincide → minimum separation
    }

    [Fact]
    public void Ringside_HasALandableMoonInShuttleRange_AtEpoch()
    {
        CircularOrbitEphemeris eph = Sol();

        IReadOnlyList<ShuttleExcursion.Destination> board = BoardAt(eph, "ringside-exchange");
        Assert.Contains(board, d => d.IsLandableSurface);
        Assert.Contains(board, d => d.BodyId == "titan" && d.IsLandableSurface);

        double sep = SeparationAtEpoch(eph, "ringside-exchange", "titan");
        Assert.True(ShuttleRange.InRange(sep), $"Titan is {sep:0.###e0} m from Ringside Exchange — out of shuttle range.");
        Assert.Equal(2.31e8, sep, 3e6); // Δφ=+0.15 (Titan ahead of Ringside, receding); deep inside the 5e8 reach
    }

    // ── The berth-matrix audit: the last two groundless berths each get a landable in range at epoch ──

    [Fact]
    public void TheDeep_HasALandableMoonInShuttleRange_AtEpoch()
    {
        CircularOrbitEphemeris eph = Sol();

        IReadOnlyList<ShuttleExcursion.Destination> board = BoardAt(eph, "the-deep");
        Assert.Contains(board, d => d.IsLandableSurface);
        Assert.Contains(board, d => d.BodyId == "triton" && d.IsLandableSurface);

        double sep = SeparationAtEpoch(eph, "the-deep", "triton");
        Assert.True(ShuttleRange.InRange(sep), $"Triton is {sep:0.###e0} m from The Deep — out of shuttle range.");
        Assert.Equal(2.9476e8, sep, 2e6); // |3.5476e8 − 6.0e7|, both phase 2.9; margin ≈ 2.05e8 m under the 5e8 reach
    }

    [Fact]
    public void CinderRoost_HasALandableMoonInShuttleRange_AtEpoch()
    {
        CircularOrbitEphemeris eph = Sol();

        IReadOnlyList<ShuttleExcursion.Destination> board = BoardAt(eph, "cinder-roost");
        Assert.Contains(board, d => d.IsLandableSurface);
        Assert.Contains(board, d => d.BodyId == "the-clinker" && d.IsLandableSurface);

        double sep = SeparationAtEpoch(eph, "cinder-roost", "the-clinker");
        Assert.True(ShuttleRange.InRange(sep), $"The Clinker is {sep:0.###e0} m from Cinder Roost — out of shuttle range.");
        Assert.Equal(7.0e6, sep, 5e4); // |2.2e7 − 1.5e7|, both phase 1.3; a short hop, deep inside the 5e8 reach
    }

    // ── The new pairs' windows are PERMANENT: r₁+r₂ < range, so they never drift out (any phase in reach) ──

    [Theory]
    [InlineData("the-deep", "triton", 4.1476e8)]         // 3.5476e8 + 6.0e7
    [InlineData("cinder-roost", "the-clinker", 3.7e7)]   // 2.2e7 + 1.5e7
    public void AuditLandable_StaysInRange_AtEveryPhase(string stationId, string moonId, double maxSeparation)
    {
        // The reachability upper bound is the sum of the two orbit radii (phases opposed). When that is
        // under the shuttle reach the pair is in range at t=0 and forever after — no drift-out slot.
        Assert.True(ShuttleRange.InRange(maxSeparation),
            $"{moonId}/{stationId} max separation {maxSeparation:0.###e0} m must be under the 5e8 reach.");

        CircularOrbitEphemeris eph = Sol();
        for (int d = 0; d <= 40; d++) // sweep well past any synodic beat — never leaves range
        {
            double t = d * 86400.0;
            double sep = (eph.Position(stationId, t) - eph.Position(moonId, t)).Length;
            Assert.True(ShuttleRange.InRange(sep) && sep <= maxSeparation + 1.0,
                $"{moonId} left shuttle range of {stationId} on day {d}: {sep:0.###e0} m.");
        }
    }

    // ── The window stays open through the opening sim days (phases drift slowly apart) ──

    [Theory]
    [InlineData("red-eye", "ganymede", 1)]        // Ganymede/Red Eye forward window ≈ 1.3 d (closest at epoch)
    [InlineData("ringside-exchange", "titan", 3)] // Titan/Ringside forward window ≈ 4.2 d (Titan ahead, receding)
    public void ReachableMoon_StaysInRange_OverTheOpeningDays(string stationId, string moonId, int days)
    {
        CircularOrbitEphemeris eph = Sol();
        for (int d = 0; d <= days; d++)
        {
            double t = d * 86400.0;
            double sep = (eph.Position(stationId, t) - eph.Position(moonId, t)).Length;
            Assert.True(ShuttleRange.InRange(sep),
                $"{moonId} left shuttle range of {stationId} after {d} day(s): {sep:0.###e0} m.");
        }
    }

    // ── The always-in-range co-orbital pairs still hold (untouched by the re-phase) ──

    [Theory]
    [InlineData("selene-gate", "luna")]      // Luna shares Selene Gate's orbit radius (co-orbital)
    [InlineData("the-space-bar", "phobos")]  // The Rusty Roadstead off Phobos
    [InlineData("the-tilt", "miranda")]      // The Tilt off Miranda (the #246 shuttle-bury moon)
    public void CoOrbitalHaven_KeepsItsLandableMoon_AtEpoch(string stationId, string moonId)
    {
        CircularOrbitEphemeris eph = Sol();

        IReadOnlyList<ShuttleExcursion.Destination> board = BoardAt(eph, stationId);
        Assert.Contains(board, d => d.BodyId == moonId && d.IsLandableSurface);

        double sep = SeparationAtEpoch(eph, stationId, moonId);
        Assert.True(ShuttleRange.InRange(sep), $"{moonId} is {sep:0.###e0} m from {stationId} — out of shuttle range.");
    }

    // ── The standing tease: Enceladus is NEVER a shuttle hop from Ringside (design gap, not this fix) ──

    [Fact]
    public void Enceladus_StaysOutOfShuttleReach_TheStandingTease()
    {
        CircularOrbitEphemeris eph = Sol();

        // Enceladus orbits deep inside Ringside's radius (2.38e8 m vs 1.35e9 m): even at closest approach
        // the gap ≈ 1.11e9 m dwarfs the 5e8 m shuttle reach. The Triton/Venus-rock lane landed its two
        // berths' surfaces (above), but Enceladus stays the standing tease on purpose — a haven-flagged
        // moon you can see and never quite shuttle to.
        double sep = SeparationAtEpoch(eph, "ringside-exchange", "enceladus");
        Assert.False(ShuttleRange.InRange(sep));
        Assert.DoesNotContain(BoardAt(eph, "ringside-exchange"), d => d.BodyId == "enceladus");
    }
}
