namespace SpaceSails.Core;

/// <summary>
/// One trade lane as world-space geometry (SundaySecondPlan F4): the segment between its two
/// anchor bodies' positions *now*, with a lane radius. This is what the sensors map draws and
/// hit-tests — the corridor the owner sketched by hand, made a real selectable region.
/// </summary>
public readonly record struct CorridorRegion(
    string AId, string BId, string PairName, Vector2d A, Vector2d B, double Radius)
{
    /// <summary>Display name of the lane itself, e.g. "Earth–Mars lane".</summary>
    public string Name => $"{PairName} lane";

    public double Length => (B - A).Length;

    public Vector2d Midpoint => (A + B) / 2;

    /// <summary>Closest point on the lane's center segment to <paramref name="point"/>.</summary>
    public Vector2d ClosestPoint(Vector2d point)
    {
        Vector2d axis = B - A;
        double lengthSquared = axis.LengthSquared;
        if (lengthSquared <= 0)
        {
            return A;
        }

        double t = Math.Clamp((point - A).Dot(axis) / lengthSquared, 0, 1);
        return A + axis * t;
    }

    public double DistanceTo(Vector2d point) => (point - ClosestPoint(point)).Length;

    public bool Contains(Vector2d point) => DistanceTo(point) <= Radius;
}

/// <summary>
/// The known trade lanes as live geometry — a pure function of (scenario bodies, sim time),
/// same anchor set the corridor-watch scan programs use. Also answers the empty-space question
/// "is this spot near a lane, here and now?" for the map's scan popup.
/// </summary>
public static class TradeCorridors
{
    /// <summary>The bodies whose pairs form the published lanes (shared with <see cref="ScanPrograms"/>).</summary>
    public static readonly string[] TradeAnchors = ["venus", "earth", "mars", "jupiter", "saturn"];

    /// <summary>Lane radius as a fraction of the lane's current length…</summary>
    public const double LaneRadiusFraction = 0.05;

    /// <summary>…but never thinner than this (short lanes still have real traffic dispersion).</summary>
    public const double MinLaneRadiusMeters = 1.2e10;

    /// <summary>"Near a lane" for the empty-space popup: within this many lane radii.</summary>
    public const double NearLaneFactor = 2.0;

    /// <summary>Every present lane at <paramref name="simTime"/>, one per anchor pair.</summary>
    public static IReadOnlyList<CorridorRegion> Regions(ICelestialEphemeris ephemeris, double simTime)
    {
        var present = new List<CelestialBody>();
        foreach (string id in TradeAnchors)
        {
            CelestialBody? body = ephemeris.Bodies.FirstOrDefault(b => b.Id == id);
            if (body is not null)
            {
                present.Add(body);
            }
        }

        var regions = new List<CorridorRegion>();
        for (int i = 0; i < present.Count; i++)
        {
            for (int j = i + 1; j < present.Count; j++)
            {
                CelestialBody a = present[i], b = present[j];
                Vector2d pa = ephemeris.Position(a.Id, simTime);
                Vector2d pb = ephemeris.Position(b.Id, simTime);
                double radius = Math.Max(MinLaneRadiusMeters, LaneRadiusFraction * (pb - pa).Length);
                regions.Add(new CorridorRegion(a.Id, b.Id, $"{a.Name}–{b.Name}", pa, pb, radius));
            }
        }

        return regions;
    }

    /// <summary>
    /// The lane nearest to <paramref name="point"/> (by distance to its center segment). False
    /// only when there are no lanes at all. Combine with <see cref="NearLaneFactor"/>:
    /// a point within <c>Radius × NearLaneFactor</c> counts as "near the lane".
    /// </summary>
    public static bool TryNearest(
        IReadOnlyList<CorridorRegion> regions, Vector2d point,
        out CorridorRegion nearest, out double distance)
    {
        nearest = default;
        distance = double.MaxValue;
        foreach (CorridorRegion region in regions)
        {
            double d = region.DistanceTo(point);
            if (d < distance)
            {
                (nearest, distance) = (region, d);
            }
        }

        return regions.Count > 0;
    }

    /// <summary>
    /// The telescope wedge that covers <paramref name="corridor"/> from the ship's vantage —
    /// the same aim the corridor-watch scan programs compute, now derivable from a lane the
    /// player clicked on the map.
    /// </summary>
    public static ScanJob SweepJobFor(CorridorRegion corridor, Vector2d shipPosition) =>
        WedgeOverSegment(shipPosition, corridor.A, corridor.B);

    /// <summary>Wedge from <paramref name="vantage"/> covering the segment
    /// <paramref name="a"/>–<paramref name="b"/> plus <see cref="ScanPrograms.ArcMarginRad"/>
    /// padding each side. A vantage on the segment means a full-circle sweep.</summary>
    public static ScanJob WedgeOverSegment(Vector2d vantage, Vector2d a, Vector2d b)
    {
        Vector2d toMid = (a + b) / 2 - vantage;
        if (toMid.LengthSquared == 0)
        {
            return new ScanJob(0, Math.Tau);
        }

        double centerBearing = TrackingStation.Bearing(toMid);
        double bearingA = TrackingStation.Bearing(a - vantage);
        double bearingB = TrackingStation.Bearing(b - vantage);
        double halfSpread = Math.Max(AngleDelta(centerBearing, bearingA), AngleDelta(centerBearing, bearingB));
        double arcWidth = Math.Clamp(
            2 * halfSpread + 2 * ScanPrograms.ArcMarginRad, ScanPrograms.ArcMarginRad, Math.Tau);
        return new ScanJob(centerBearing, arcWidth);
    }

    private static double AngleDelta(double a, double b)
    {
        double d = (b - a) % Math.Tau;
        if (d > Math.PI) d -= Math.Tau;
        if (d < -Math.PI) d += Math.Tau;
        return Math.Abs(d);
    }
}
