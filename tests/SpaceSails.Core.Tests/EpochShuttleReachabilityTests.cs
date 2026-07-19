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
/// shuttle reach on purpose — the standing tease is left for the Triton/Venus-rock design lane.</para>
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
        // the gap ≈ 1.11e9 m dwarfs the 5e8 m shuttle reach. Left for the Triton/Venus-rock design lane.
        double sep = SeparationAtEpoch(eph, "ringside-exchange", "enceladus");
        Assert.False(ShuttleRange.InRange(sep));
        Assert.DoesNotContain(BoardAt(eph, "ringside-exchange"), d => d.BodyId == "enceladus");
    }
}
