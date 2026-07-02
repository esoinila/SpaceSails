namespace SpaceSails.Core;

/// <summary>Instantaneous kinematic state of a ship. Immutable; stepping produces a new state.</summary>
public readonly record struct ShipState(Vector2d Position, Vector2d Velocity, double SimTime);
