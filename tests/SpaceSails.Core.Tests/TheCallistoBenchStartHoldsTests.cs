using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1318 · <c>/map?start=callisto</c> — THE FREE PARK THE SURFACE TOUR'S CALLISTO ROWS STAND ON.
///
/// <para>No berth can put Callisto on the shuttle board: The Red Eye rides 8.5e8 m from Jupiter and
/// Callisto 1.8827e9 m, so the gap never closes below 1.03e9 m against a one-hop reach of
/// <see cref="ShuttleRange.RangeMeters"/>. The bench start lets the ship go alongside instead, through the
/// same <see cref="BerthState.CoOrbital"/> construction the Enceladus bench start and the cycler arrival use.
/// Its standoff is the literal in <c>Map.Sim.Starts.WhatAStartHangsOff</c>, quoted here the way
/// <see cref="TheParkedShipIsNotRunDownByTheMoonTests"/> quotes the Enceladus one.</para>
///
/// <para>The law: across a whole excursion (the crossing down, two hours on the ground, the crossing back)
/// the parked hull meets no surface and never leaves shuttle reach of the moon, at every phase of
/// Callisto's orbit — or the start would strand a tester on a ground his ship has drifted away from.</para>
/// </summary>
public class TheCallistoBenchStartHoldsTests
{
    private const string Moon = "callisto";
    private const double Standoff = 1.5e8;
    private const double GroundSeconds = 2.0 * 3600.0;

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    public void TheParkedHull_ClearsEverySurface_AndStaysInOneHop(double phaseOfCallistosYear)
    {
        CircularOrbitEphemeris eph = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        var simulator = new Simulator(eph, timeStepSeconds: 1.0);
        CelestialBody callisto = eph.Bodies.First(b => b.Id == Moon);
        double epoch = phaseOfCallistosYear * callisto.OrbitPeriod;

        ShipState ship = BerthState.CoOrbital(eph, Moon, epoch, Standoff);
        double crossing = ShuttleRange.TravelSeconds(Standoff);
        double end = epoch + crossing + GroundSeconds + crossing;
        double farthest = 0.0;

        while (ship.SimTime < end)
        {
            LoiterClock.Coast leg = LoiterClock.Advance(
                simulator, eph, ship, Math.Min(LoiterClock.QuantumSeconds, end - ship.SimTime));
            Assert.True(leg.Struck is null,
                $"the parked hull met {leg.Struck?.BodyName} {(leg.Ship.SimTime - epoch) / 3600:F2} h after the park.");
            ship = leg.Ship;
            farthest = Math.Max(farthest, (ship.Position - eph.Position(Moon, ship.SimTime)).Length);
        }

        Assert.True(ShuttleRange.InRange(farthest),
            $"the parked hull drifted {farthest:e3} m from Callisto — out of the {ShuttleRange.RangeMeters:e3} m hop.");
    }
}
