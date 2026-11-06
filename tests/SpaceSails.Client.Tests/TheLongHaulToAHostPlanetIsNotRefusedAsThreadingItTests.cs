using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1387 · A LONG HAUL TO A PLANET IS NEVER REFUSED AS "THAT LINE THREADS" THAT PLANET.
///
/// <para>The surface-clearance gate sampled the solved conic to <c>ArrivalCenterTime</c> — the instant the arc
/// reaches the planet's CENTRE — and judged the arrival body from that final sample, so every haul to every
/// planet read as threading it (Saturn from Selene Gate, the recommended first voyage; Mars; Jupiter). The
/// haul stops at the capture range, far above the surface, and now so does the sampled path. The sweep solves a
/// long haul from every haven to every haven's host planet and asserts that no refusal ever NAMES the
/// destination: another body in the way may still refuse the line, and that is not what is guarded.</para>
/// </summary>
public sealed class TheLongHaulToAHostPlanetIsNotRefusedAsThreadingItTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [Fact]
    public async Task FromSeleneGateTheHaulToSaturnClearsItsOwnDestination()
    {
        PastTheGateBench.Boot b = await PastTheGateBench.BootAsync("/map?dock=selene-gate");
        Assert.Null(b.Threw);
        typeof(Map).GetMethod("Undock", Hidden)!.Invoke(b.Page, null);

        string? block = ClearanceBlock(b, b.Read<ShipState>("_ship"), "saturn", out bool solved);
        Assert.True(solved, "the Selene Gate to Saturn departure did not solve, so there is no path to judge");

        Assert.True(block is null || !block.Contains("Saturn"), $"refused: {block}");
    }

    [Fact]
    public async Task NoHaulFromAnyHavenIsRefusedForThreadingItsOwnDestination()
    {
        PastTheGateBench.Boot b = await PastTheGateBench.BootAsync("/map?dock=selene-gate");
        Assert.Null(b.Threw);
        typeof(Map).GetMethod("Undock", Hidden)!.Invoke(b.Page, null);
        var eph = (ICelestialEphemeris)b.Field("_ephemeris")!;
        var ship = b.Read<ShipState>("_ship");
        // Where the undock leaves her, relative to her berth: a ship parked exactly ON a station would "thread"
        // that station at the first sample, which is a bench artefact and not a long haul the game plots.
        Vector2d standoff = ship.Position - eph.Position("selene-gate", ship.SimTime);
        Vector2d standoffVel = ship.Velocity - TransferMath.BodyVelocity(eph, "selene-gate", ship.SimTime);

        var hosts = eph.Bodies.Where(h => h.IsHaven).Select(h => LongHaul.JumpTargetPlanet(eph, h.Id))
            .Where(p => p is not null).Select(p => p!).DistinctBy(p => p.Id).ToList();
        var refused = new List<string>();
        int hauls = 0;
        foreach (CelestialBody from in eph.Bodies.Where(h => h.IsHaven))
        {
            ShipState atHaven = ship with
            {
                Position = eph.Position(from.Id, ship.SimTime) + standoff,
                Velocity = TransferMath.BodyVelocity(eph, from.Id, ship.SimTime) + standoffVel,
            };
            foreach (CelestialBody host in hosts)
            {
                if (LongHaul.JumpTargetPlanet(eph, from.Id)?.Id == host.Id)
                {
                    continue; // already home: nothing to haul
                }

                string? block = ClearanceBlock(b, atHaven, host.Id, out bool solved);
                if (!solved)
                {
                    continue;
                }

                hauls++;
                if (block is not null && block.Contains(host.Name))
                {
                    refused.Add($"{from.Id} -> {host.Id}: {block}");
                }
            }
        }

        Assert.True(hauls > 10, $"the sweep judged only {hauls} solved hauls");
        Assert.True(refused.Count == 0, $"{refused.Count} of {hauls} hauls refused for threading their own destination:\n"
                                        + string.Join("\n", refused.Take(8)));
    }

    private static string? ClearanceBlock(PastTheGateBench.Boot b, ShipState from, string destId, out bool solved)
    {
        var eph = (ICelestialEphemeris)b.Field("_ephemeris")!;
        CelestialBody planet = LongHaul.JumpTargetPlanet(eph, destId)!;
        LongHaul.Departure dep = LongHaul.SolveDeparture(from, eph, planet);
        solved = dep.Ok;
        if (!dep.Ok)
        {
            return null; // an unsolvable haul has no path to judge
        }

        typeof(Map).GetField("_ship", Hidden)!.SetValue(b.Page, from);
        return (string?)typeof(Map).GetMethod("LongHaulClearanceBlock", Hidden)!.Invoke(b.Page, [dep, planet]);
    }
}
