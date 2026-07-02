namespace SpaceSails.Core;

/// <summary>
/// Double-precision 2D vector. Simulation coordinates are meters in the ecliptic plane;
/// doubles are mandatory — solar-system positions overflow float precision.
/// </summary>
public readonly record struct Vector2d(double X, double Y)
{
    public static readonly Vector2d Zero = new(0, 0);

    public double Length => Math.Sqrt(X * X + Y * Y);

    public double LengthSquared => X * X + Y * Y;

    public static Vector2d operator +(Vector2d a, Vector2d b) => new(a.X + b.X, a.Y + b.Y);

    public static Vector2d operator -(Vector2d a, Vector2d b) => new(a.X - b.X, a.Y - b.Y);

    public static Vector2d operator *(Vector2d v, double s) => new(v.X * s, v.Y * s);

    public static Vector2d operator *(double s, Vector2d v) => new(v.X * s, v.Y * s);

    public static Vector2d operator /(Vector2d v, double s) => new(v.X / s, v.Y / s);

    public static Vector2d operator -(Vector2d v) => new(-v.X, -v.Y);

    public double Dot(Vector2d other) => X * other.X + Y * other.Y;

    public Vector2d Normalized()
    {
        double length = Length;
        return length > 0 ? this / length : Zero;
    }
}
