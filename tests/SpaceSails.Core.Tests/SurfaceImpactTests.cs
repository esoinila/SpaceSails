namespace SpaceSails.Core.Tests;

/// <summary>
/// #264 — the impact enforcer's crossing detector. Lab 16 warned "periapsis under the surface — impact
/// coming"; SurfaceImpact is what finally makes it arrive by catching the moment a live-flown step
/// reaches a body's surface radius. These pin the step-robust geometry: a normal crossing, a clean miss,
/// a tunnelling pass (both endpoints outside, the chord through the body — the case an endpoint-only
/// check sails through), a step starting inside, rail motion (the body sweeps over the ship), the
/// zero-radius haven exemption, and earliest-of-many selection.
/// </summary>
public class SurfaceImpactTests
{
    // A fixed-position ephemeris: each body sits where the map places it, time-independent — enough to
    // exercise the segment/disc geometry. The rail-motion test supplies a moving body of its own.
    private sealed class FixedBodies(params CelestialBody[] bodies) : ICelestialEphemeris
    {
        public IReadOnlyList<CelestialBody> Bodies { get; } = bodies;

        public Vector2d Position(string bodyId, double simTime) =>
            // The body's "orbit radius" is repurposed as a fixed X offset from origin for these tests.
            new(Bodies.First(b => b.Id == bodyId).OrbitRadius, 0);
    }

    private sealed class MovingBody(string id, double radius, Vector2d at0, Vector2d at1) : ICelestialEphemeris
    {
        public IReadOnlyList<CelestialBody> Bodies { get; } =
            [new CelestialBody(id, id, null, 0, radius, 0, 0, 0)];

        public Vector2d Position(string bodyId, double simTime) =>
            // Linear between t=0 and t=1 (the step used by the rail-motion test spans exactly that).
            at0 + (at1 - at0) * simTime;
    }

    private static CelestialBody Planet(double radius, double xOffset = 0) =>
        new("p", "Planet", null, 0, radius, xOffset, 0, 0);

    [Fact]
    public void StraddlingStep_FindsTheSurfaceCrossing()
    {
        // Body radius 1000 at origin; the step runs from x=-2000 (clear) to the centre (inside). It first
        // touches the surface halfway, at (-1000, 0).
        var eph = new FixedBodies(Planet(1000));
        SurfaceImpact.Crossing? hit = SurfaceImpact.FirstCrossing(
            new Vector2d(-2000, 0), 0, Vector2d.Zero, 1, eph);

        Assert.NotNull(hit);
        Assert.Equal("p", hit!.Value.BodyId);
        Assert.Equal(0.5, hit.Value.Fraction, precision: 9);
        Assert.Equal(-1000, hit.Value.Position.X, precision: 6);
        Assert.Equal(0.5, hit.Value.SimTime, precision: 9);
    }

    [Fact]
    public void CleanPass_AboveTheSurface_IsNull()
    {
        // The chord holds y=2000, a whole radius clear of the 1000 m surface — no crossing.
        var eph = new FixedBodies(Planet(1000));
        Assert.Null(SurfaceImpact.FirstCrossing(
            new Vector2d(-2000, 2000), 0, new Vector2d(2000, 2000), 1, eph));
    }

    [Fact]
    public void TunnellingStep_BothEndpointsClear_StillImpacts()
    {
        // The step-robustness that matters (#264): a coarse step from x=-5000 to x=+5000 has BOTH
        // endpoints 5000 m clear of the 1000 m body, yet the chord passes clean through it. An
        // "is it inside at the endpoints?" check misses this; the min-distance root does not.
        var eph = new FixedBodies(Planet(1000));
        SurfaceImpact.Crossing? hit = SurfaceImpact.FirstCrossing(
            new Vector2d(-5000, 0), 0, new Vector2d(5000, 0), 1, eph);

        Assert.NotNull(hit);
        Assert.Equal(0.4, hit!.Value.Fraction, precision: 9); // enters at x=-1000 over a 10000 m sweep
    }

    [Fact]
    public void StepStartingInsideTheSurface_IsImmediateContact()
    {
        var eph = new FixedBodies(Planet(1000));
        SurfaceImpact.Crossing? hit = SurfaceImpact.FirstCrossing(
            new Vector2d(500, 0), 0, new Vector2d(5000, 0), 1, eph);

        Assert.NotNull(hit);
        Assert.Equal(0.0, hit!.Value.Fraction, precision: 9);
    }

    [Fact]
    public void RailMotion_BodySweepsOverAStationaryShip_Impacts()
    {
        // The ship holds at the origin; the body slides from x=+3000 to x=-3000 across the step, passing
        // over it. Both of the body's endpoint positions are 3000 m clear — only interpolating the body's
        // rail motion (not just the ship's) catches the strike.
        var eph = new MovingBody("m", radius: 1000, at0: new Vector2d(3000, 0), at1: new Vector2d(-3000, 0));
        SurfaceImpact.Crossing? hit = SurfaceImpact.FirstCrossing(
            Vector2d.Zero, 0, Vector2d.Zero, 1, eph);

        Assert.NotNull(hit);
        Assert.Equal(1.0 / 3.0, hit!.Value.Fraction, precision: 6); // body's near edge reaches the ship at s=1/3
    }

    [Fact]
    public void ZeroRadiusHaven_IsNeverStruck()
    {
        // A mass-less station haven carries no BodyRadius; docked ships and havens on rails are exempt
        // (#264). The chord runs straight through its centre and reports nothing.
        var eph = new FixedBodies(Planet(radius: 0));
        Assert.Null(SurfaceImpact.FirstCrossing(
            new Vector2d(-2000, 0), 0, new Vector2d(2000, 0), 1, eph));
    }

    [Fact]
    public void TwoBodies_ReturnsTheEarliestCrossing()
    {
        // A near body at x=-1000 (struck early) and a far body at x=+4000 (struck late); the earliest
        // crossing wins so the ship dies where it first touches rock.
        var eph = new FixedBodies(Planet(1000, xOffset: -1000) with { Id = "near", Name = "Near" },
                                  Planet(1000, xOffset: 4000) with { Id = "far", Name = "Far" });
        SurfaceImpact.Crossing? hit = SurfaceImpact.FirstCrossing(
            new Vector2d(-3000, 0), 0, new Vector2d(6000, 0), 1, eph);

        Assert.NotNull(hit);
        Assert.Equal("near", hit!.Value.BodyId);
    }

    [Fact]
    public void FirstInsideFraction_MatchesTheClosedForm()
    {
        // Direct check of the quadratic root: a=(-2,0), b=(4,0), r=1 → enters at s=0.25.
        double? s = SurfaceImpact.FirstInsideFraction(new Vector2d(-2, 0), new Vector2d(4, 0), 1);
        Assert.NotNull(s);
        Assert.Equal(0.25, s!.Value, precision: 9);
    }
}
