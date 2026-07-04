namespace SpaceSails.Client.Pages;

/// <summary>
/// Duty-station desks (Saturday Plan PR-11, docs/SaturdayPlan/StationDesks.md): the UI is
/// organized as full-screen desks the crew switches between by number key, not small pop-up
/// panels over the map. Each value's numeric order matches the keyboard shortcut (1-7) and the
/// station tab bar. Deck has no summary chip of its own (see DeskChips) — everything else does.
/// </summary>
public enum ShipDesk
{
    Nav = 1,
    Sensors,
    WarRoom,
    Trade,
    Comms,
    Galley,
    Deck,
}
