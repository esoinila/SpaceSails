namespace SpaceSails.Core.Tests;

// #145 — the DRAWN trajectory length is scaled to the frame's local timescale in a co-moving
// gas-giant frame, so the owner's 7-day Titan approach no longer renders as a spirograph coil of
// ~8-10 laps around Saturn. These pin the pure window-length helper (OrbitRule.FrameScaledWindowSeconds
// and its LocalOrbitPeriod). The projection/ETA math it feeds is unchanged.
public class FrameScaledRibbonTests
{
    // Saturn's gravitational parameter (m³/s²) and a representative mid parking radius from the
    // playtest geometry — a ~3-day Saturn orbit, which over a 30-day auto horizon draws ~10 laps.
    private const double SaturnMu = 3.7931e16;

    [Fact]
    public void LocalOrbitPeriod_MatchesKepler_Titan()
    {
        // Titan orbits Saturn at ~1.2219e9 m; its period is ~15.9 days.
        double t = OrbitRule.LocalOrbitPeriod(1.2219e9, SaturnMu);
        Assert.True(Math.Abs(t - 15.9 * 86400) < 0.5 * 86400, $"expected ~15.9 d, got {t / 86400:F2} d");
    }

    // At a mid Saturn orbit the local period is ~3 days, so 1.25 periods draws ~3.75 days of ribbon —
    // roughly one clean arc instead of the ~10 laps the full 30-day horizon would coil.
    [Fact]
    public void Window_MidSaturnOrbit_IsAboutOnePlusLocalPeriod()
    {
        // Radius giving a ~3-day period: r = (T/2π)^(2/3) · μ^(1/3).
        double threeDays = 3 * 86400;
        double r = Math.Cbrt(Math.Pow(threeDays / (2 * Math.PI), 2) * SaturnMu);

        double fullHorizon = 30 * 86400;
        double window = OrbitRule.FrameScaledWindowSeconds(r, SaturnMu, fullHorizon, floorSeconds: 6 * 3600);

        Assert.True(Math.Abs(window - 1.25 * threeDays) < 0.25 * 86400, $"expected ~3.75 d, got {window / 86400:F2} d");
        Assert.True(window < fullHorizon, "the coil-hiding truncation must be well under the full horizon");
    }

    // The imminent step is never hidden: an armed insertion 7 days out floors the window to 7 d + the
    // node margin even though 1.25 local periods (~3.75 d) is shorter. The plan's NEXT line and the
    // ribbon must not contradict (#145.5).
    [Fact]
    public void Window_FlooredToImminentNode_NeverHidesTheStep()
    {
        double threeDays = 3 * 86400;
        double r = Math.Cbrt(Math.Pow(threeDays / (2 * Math.PI), 2) * SaturnMu);

        double nodePlusMargin = 7 * 86400 + 12 * 3600;
        double window = OrbitRule.FrameScaledWindowSeconds(r, SaturnMu, 30 * 86400, floorSeconds: nodePlusMargin);

        Assert.Equal(nodePlusMargin, window, 3);
    }

    // Ceiling: when the full projection is already shorter than a local period (a wide, slow orbit),
    // we just draw all of it — the truncation never lengthens the ribbon.
    [Fact]
    public void Window_NeverExceedsFullHorizon()
    {
        // A very large radius → a local period far longer than the horizon.
        double window = OrbitRule.FrameScaledWindowSeconds(5e10, SaturnMu, fullHorizonSeconds: 10 * 86400, floorSeconds: 6 * 3600);
        Assert.Equal(10 * 86400, window, 3);
    }

    // Floor still bounded by the ceiling: even a floor is clamped to the full projection, so the
    // drawn ribbon never runs past the data it draws.
    [Fact]
    public void Window_FloorClampedToFullHorizon()
    {
        double window = OrbitRule.FrameScaledWindowSeconds(3e8, SaturnMu, fullHorizonSeconds: 2 * 86400, floorSeconds: 30 * 86400);
        Assert.Equal(2 * 86400, window, 3);
    }

    // Degenerate inputs fall back to the full horizon (no truncation): a mass-less dock (μ=0), a
    // zero radius, or a non-finite horizon must be a safe no-op for the caller.
    [Theory]
    [InlineData(0, SaturnMu)]
    [InlineData(-1, SaturnMu)]
    [InlineData(3e8, 0)]
    [InlineData(3e8, -5)]
    public void Window_DegenerateInputs_ReturnFullHorizon(double radius, double mu)
    {
        double full = 12 * 86400;
        Assert.Equal(full, OrbitRule.FrameScaledWindowSeconds(radius, mu, full, floorSeconds: 6 * 3600));
    }

    [Fact]
    public void Window_NonFiniteHorizon_ReturnsHorizonUnchanged()
    {
        Assert.True(double.IsInfinity(
            OrbitRule.FrameScaledWindowSeconds(3e8, SaturnMu, double.PositiveInfinity, floorSeconds: 6 * 3600)));
    }
}
