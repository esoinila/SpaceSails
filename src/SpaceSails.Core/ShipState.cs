namespace SpaceSails.Core;

/// <summary>
/// Instantaneous state of a ship. Immutable; stepping produces a new state. <see cref="Charge"/>
/// is the hull charge in [0, 1] for the Electric Universe layer (M7); it stays 0 — and costs
/// nothing — in Newtonian scenarios, so all pre-M7 states and replays are unchanged.
/// </summary>
public readonly record struct ShipState(Vector2d Position, Vector2d Velocity, double SimTime, double Charge = 0);
