namespace SpaceSails.Core;

/// <summary>
/// One point on a projected trajectory: where the ship is and when it is there.
/// Plotting mode scrubs these by time; renderers draw them as a polyline.
/// </summary>
public readonly record struct TrajectorySample(double SimTime, Vector2d Position);
