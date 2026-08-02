using System.Collections.Generic;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #621 · THE TANK RUNS INSIDE A DEAD SHIP.
///
/// <para>The suit asked whether the captain was breathing hers with
/// <c>MoonSurface.IsSafeAboard(_avatarY)</c> — "are you above the regolith's top rim at y = −20". A
/// derelict's entire deck runs from <see cref="WreckLayout.TopY"/> = −9 to
/// <see cref="WreckLayout.BottomY"/> = +9, so the answer aboard a wreck was YES at every point of every
/// hull. Standing in a ship that has held vacuum for years the gauge read <i>"AIR · FILLING — you are on
/// her air, not the tank"</i>, the one-shot line said <i>"🫁 HER AIR. The tank stops and starts filling —
/// the only place in the world it does"</i>, and the tank genuinely refilled.</para>
///
/// <para>The fifth <b>MOON CONSTANT GOVERNING A SHIP</b>, and the one with the largest blast radius, because
/// it is the instrument the whole excursion is timed against. It also made
/// <see cref="DeathCause.Suffocated"/> unreachable on a derelict — a death with prose, a tail and (now) a
/// picture that no captain could ever have been shown.</para>
///
/// <para>Walked as a GRID over the whole hull rather than sampled at the spawn, because the bug's signature
/// is that it is satisfied everywhere: one probe in the right place proves nothing about a predicate whose
/// failure mode is answering the same thing at every point.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class HerAirIsNotADeadHullsAirTests
{
    private const double AvatarRadius = 0.7;   // DeckPlan.AvatarRadius, the value the page passes

    /// <summary>What the suit concludes standing at (x, y) aboard a derelict — the whole chain the page
    /// runs, with the excursion on a wreck's floor 0, no shelter and no Hive refuge.</summary>
    private static SuitAir.Supply SupplyAboardAWreckAt(double x, double y) =>
        SuitAir.SourceOf(
            floor: 0,
            insideShelter: false,
            aboard: AwayTeamSide.BackAtTheShuttle(onWreck: true, x, y, AvatarRadius),
            inRefuge: false);

    [Fact]
    public void EveryMetreOfADerelictShortOfTheLock_RunsTheTank()
    {
        var bad = new List<string>();

        for (double x = WreckLayout.AftX; x <= WreckLayout.ShuttleLockX - AvatarRadius - 0.5; x += 0.5)
        {
            for (double y = WreckLayout.TopY; y <= WreckLayout.BottomY; y += 0.5)
            {
                SuitAir.Supply supply = SupplyAboardAWreckAt(x, y);
                if (supply != SuitAir.Supply.Tanks)
                {
                    bad.Add($"  ({x:0.0}, {y:0.0}): the suit says {supply} — \"{SuitAir.PlateLine(supply)}\" "
                        + "— inside a hull that has held vacuum for years.");
                }
            }
        }

        Report(bad, "points aboard a derelict where the suit thinks the captain is breathing ship's air");
    }

    [Fact]
    public void TheAwayTeamsOwnSideOfTheLock_IsStillHerAir()
    {
        // And the other end of the journey, because "down is not up": past the shuttle's lock IS her air,
        // and a fix that made the whole wreck vacuum would have broken the one place the tank refills.
        for (double y = WreckLayout.TopY; y <= WreckLayout.BottomY; y += 0.5)
        {
            Assert.Equal(
                SuitAir.Supply.Ship,
                SupplyAboardAWreckAt(WreckLayout.BowX - 1, y));
        }

        // The spawn is INSIDE her, one step short of the lock — the first thing a boarding captain reads
        // must be that the clock is running.
        Assert.Equal(SuitAir.Supply.Tanks, SupplyAboardAWreckAt(WreckLayout.SpawnX, WreckLayout.SpawnY));
    }

    [Fact]
    public void TheMoonsRimIsWhyItHid()
    {
        // Not a fix — the REASON, pinned so nobody re-derives the moon's rule aboard a hull. The moon's
        // number is not absurd for a wreck; it is merely satisfied everywhere, so the feature silently never
        // fires and nothing ever errors.
        Assert.True(MoonSurface.IsSafeAboard(WreckLayout.TopY));
        Assert.True(MoonSurface.IsSafeAboard(WreckLayout.BottomY));
    }

    [Fact]
    public void OnAMoonTheTubeIsStillTheOnlyPlaceSheFillsIt()
    {
        // The surface half of the same predicate, unchanged: out on the regolith the clock is real, and up
        // the tube it is not. A one-function merge must not have moved the moon's own line.
        Assert.Equal(
            SuitAir.Supply.Tanks,
            SuitAir.SourceOf(0, false,
                AwayTeamSide.BackAtTheShuttle(onWreck: false, 0, MoonSurface.SurfaceTopY - 1, AvatarRadius)));
        Assert.Equal(
            SuitAir.Supply.Ship,
            SuitAir.SourceOf(0, false,
                AwayTeamSide.BackAtTheShuttle(onWreck: false, 0, MoonSurface.SurfaceTopY + 1, AvatarRadius)));
    }

    private static void Report(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad.GetRange(0, System.Math.Min(20, bad.Count)))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}
