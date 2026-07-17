namespace SpaceSails.Core.Tests;

/// <summary>
/// #179/#180: the moon-grade stability verdict. The manual Enter-orbit press circularizes at the
/// ship's CURRENT radius; at Enceladus the owner pressed at ≈ 0.53 Hill — the tide-chaotic band the
/// Lab 16 drift sweep mapped (robust to ≈ 0.33 Hill, strips near half-Hill) — and the orbit silently
/// died. <see cref="OrbitRule.ParkStability"/> is the predicate that catches that at insert and while
/// a bound orbit decays.
/// </summary>
public class OrbitStabilityTests
{
    // An Enceladus-class deep well: tiny airless moon, Hill sphere only ≈ 3.8 body radii, so its
    // 0.33-Hill stable park sits just above the surface floor.
    private static readonly CelestialBody Moon =
        new("enceladus", "Enceladus", "saturn", Mu: 7.2e9, BodyRadius: 250e3,
            OrbitRadius: 238e6, OrbitPeriod: 118_000, InitialPhase: 0, Kind: BodyKind.Moon);

    private const double Hill = 948e3; // ≈ 3.8 body radii — a real deep well

    /// <summary>A circular park at <paramref name="radiusFraction"/> × Hill: ship at that radius on
    /// +X, velocity the local circular speed, body at rest at the origin.</summary>
    private static ShipState CircularParkAt(double radiusFraction)
    {
        double radius = radiusFraction * Hill;
        double vCirc = Math.Sqrt(Moon.Mu / radius);
        return new ShipState(new Vector2d(radius, 0), new Vector2d(0, vCirc), 0);
    }

    [Fact]
    public void ParkStability_AtTheAutopilotParkDepth_IsStable()
    {
        // 0.33 Hill — exactly where the armed autopilot circularizes. Must never flag its own park.
        var ship = CircularParkAt(OrbitRule.ParkStableHillFraction);

        Assert.Equal(OrbitRule.ParkStabilityVerdict.Stable,
            OrbitRule.ParkStability(ship, Vector2d.Zero, Vector2d.Zero, Moon, Hill));
    }

    [Fact]
    public void ParkStability_CircularAtHalfHill_IsTideRisk()
    {
        // The owner's Enceladus press: ≈ 0.53 Hill, deep in the chaotic band (Lab 16). Bound now,
        // but the tide strips it over hours — the alert predicate must fire immediately, long before
        // escape or impact (#180 gate).
        var ship = CircularParkAt(0.53);

        Assert.Equal(OrbitRule.ParkStabilityVerdict.TideRisk,
            OrbitRule.ParkStability(ship, Vector2d.Zero, Vector2d.Zero, Moon, Hill));
    }

    [Fact]
    public void ParkStability_EccentricWithPeriapsisUnderTheFloor_IsSubsurface()
    {
        // Ship at apoapsis 400 km moving too slow to hold it — periapsis falls to ≈ 200 km, well
        // under the 1.1·R = 275 km surface floor: the orbit intersects the moon, impact is coming.
        double ra = 400e3;
        double va = Math.Sqrt(2 * (Moon.Mu / ra - Moon.Mu / (2 * 300e3))); // a = 300 km → rp ≈ 200 km
        var ship = new ShipState(new Vector2d(ra, 0), new Vector2d(0, va), 0);

        Assert.Equal(OrbitRule.ParkStabilityVerdict.Subsurface,
            OrbitRule.ParkStability(ship, Vector2d.Zero, Vector2d.Zero, Moon, Hill));
    }

    [Fact]
    public void ParkStability_HyperbolicFlyby_IsNotBound()
    {
        // Screaming through the Hill sphere: positive two-body energy — never captured.
        var ship = new ShipState(new Vector2d(300e3, 0), new Vector2d(0, 2000), 0);

        Assert.Equal(OrbitRule.ParkStabilityVerdict.NotBound,
            OrbitRule.ParkStability(ship, Vector2d.Zero, Vector2d.Zero, Moon, Hill));
    }

    [Fact]
    public void ParkStability_OutsideTheHillSphere_IsNotBound()
    {
        // Slow but far beyond the Hill sphere — the moon does not own it.
        var ship = new ShipState(new Vector2d(2 * Hill, 0), new Vector2d(0, 50), 0);

        Assert.Equal(OrbitRule.ParkStabilityVerdict.NotBound,
            OrbitRule.ParkStability(ship, Vector2d.Zero, Vector2d.Zero, Moon, Hill));
    }

    [Fact]
    public void RadiusInStableBand_TracksTheVerdictBoundaries()
    {
        // The manual-press band test agrees with the circular-orbit verdict at every radius.
        Assert.True(OrbitRule.RadiusInStableBand(OrbitRule.ParkStableHillFraction * Hill, Moon, Hill));
        Assert.False(OrbitRule.RadiusInStableBand(0.53 * Hill, Moon, Hill));            // chaotic band
        Assert.False(OrbitRule.RadiusInStableBand(0.5 * OrbitRule.SurfaceParkRadii * Moon.BodyRadius, Moon, Hill)); // under floor
    }

    [Fact]
    public void StableParkCeiling_KeepsTheAutopilotParkInsideTheBand()
    {
        // Whatever the well's depth, the autopilot's own park radius is never above the ceiling.
        double park = OrbitRule.ParkingRadius(Moon, Hill);
        Assert.True(park <= OrbitRule.StableParkCeiling(Moon, Hill));
    }

    // ---- #265: the bound-orbit period the plot ribbon caps to (one closing revolution) ------------

    [Fact]
    public void BoundOrbitPeriod_CircularPark_EqualsTheLocalCircularPeriod()
    {
        // A circular park (a = r) revolves in exactly the local circular period — the one revolution the
        // captured ribbon should draw instead of a bouquet.
        double radius = OrbitRule.ParkStableHillFraction * Hill;
        var ship = CircularParkAt(OrbitRule.ParkStableHillFraction);

        double? period = OrbitRule.BoundOrbitPeriod(ship, Vector2d.Zero, Vector2d.Zero, Moon, Hill);

        Assert.NotNull(period);
        Assert.Equal(OrbitRule.LocalOrbitPeriod(radius, Moon.Mu), period!.Value, 3);
    }

    [Fact]
    public void BoundOrbitPeriod_EccentricBound_UsesTheSemiMajorAxis()
    {
        // Ship at apoapsis 400 km with a semi-major axis of 300 km (bound): T = 2π√(a³/μ), NOT the local
        // circular period at the current 400 km radius — the ellipse's own revolution.
        double ra = 400e3, a = 300e3;
        double va = Math.Sqrt(2 * (Moon.Mu / ra - Moon.Mu / (2 * a)));
        var ship = new ShipState(new Vector2d(ra, 0), new Vector2d(0, va), 0);

        double? period = OrbitRule.BoundOrbitPeriod(ship, Vector2d.Zero, Vector2d.Zero, Moon, Hill);

        Assert.NotNull(period);
        Assert.Equal(2 * Math.PI * Math.Sqrt(a * a * a / Moon.Mu), period!.Value, 3);
    }

    [Fact]
    public void BoundOrbitPeriod_HyperbolicFlyby_IsNull()
    {
        // Positive two-body energy — never captured, so there is no revolution to cap to (full ribbon).
        var ship = new ShipState(new Vector2d(300e3, 0), new Vector2d(0, 2000), 0);

        Assert.Null(OrbitRule.BoundOrbitPeriod(ship, Vector2d.Zero, Vector2d.Zero, Moon, Hill));
    }

    [Fact]
    public void BoundOrbitPeriod_OutsideTheHillSphere_IsNull()
    {
        // Beyond the Hill sphere the body does not own the ship — an unbound leg, full ribbon.
        var ship = new ShipState(new Vector2d(2 * Hill, 0), new Vector2d(0, 50), 0);

        Assert.Null(OrbitRule.BoundOrbitPeriod(ship, Vector2d.Zero, Vector2d.Zero, Moon, Hill));
    }
}
