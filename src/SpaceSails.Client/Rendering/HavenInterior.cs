namespace SpaceSails.Client.Rendering;

/// <summary>
/// Walkable haven interiors — the "go ashore" side of docking (2026-07-07). A client-side
/// <c>bodyId → </c><see cref="DeckPlan"/> lookup, so no scenario-schema change is needed yet:
/// dock at a mass-less ⚓ station haven, step off the ship, and walk around inside.
///
/// Milestone 1 is deliberately small — one walkable room for <c>the-space-bar</c>, empty but
/// for the gangway hatch home. The bar's intel-broker / job-board consoles (fronting the Comms
/// desk's dark-web market and the departures board) land in the follow-up; the plan already
/// routes console interaction, so wiring them is a data change, not a structural one.
/// </summary>
public static class HavenInterior
{
    /// <summary>The interior for a docked body, or null if that haven has no deck to walk (yet).</summary>
    public static DeckPlan? ForBody(string bodyId) => bodyId switch
    {
        "the-space-bar" => TheSpaceBar.Value,
        _ => null,
    };

    // The Space Bar off Mars: one rectangular room, 24×14 du, a panoramic window along the
    // spinward wall onto real space (the raycaster fills it from the live ephemeris), and a
    // gangway hatch back to the ship. Built once, lazily, and shared.
    private static class TheSpaceBar
    {
        internal static readonly DeckPlan Value = Build();

        private static DeckPlan Build()
        {
            DeckPlan.Wall[] walls =
            [
                new(-12, 7, 12, 7, IsWindow: true, IsHull: true), // spinward: the long window onto space
                new(12, 7, 12, -7, false, true),                  // starboard bulkhead
                new(12, -7, -12, -7, false, true),                // back-of-house bulkhead
                new(-12, -7, -12, 7, false, true),                // port bulkhead
            ];

            DeckPlan.ConsoleSpot[] consoles =
            [
                // The bar's regulars, each at their own booth — walk up, press E, and they slide a
                // contract across the table (go-ashore quests). The label after "◈ " is the giver's
                // handle; Map maps the handle to their trade (Silas → bounties, Coil → cargo runs).
                new(DeckPlan.ConsoleKind.BarPatron, -7, 2, "◈ ONE-EYE SILAS"),
                new(DeckPlan.ConsoleKind.BarPatron, 7, 2, "◈ MADAM COIL"),
                new(DeckPlan.ConsoleKind.BarPatron, 0, 4, "◈ GILT-EYE"),

                // The gangway home — walk to it and press E to step back aboard (Q does it too).
                new(DeckPlan.ConsoleKind.Airlock, 0, -6, "⚓ BACK ABOARD"),
            ];

            (float X, float Y, string Text)[] roomLabels =
            [
                (0, 5.5f, "THE SPACE BAR"),
            ];

            DeckPlan.Backdrop[] backdrops =
            [
                new("art/the-space-bar.jpg", -12, 7, 24, 14, 0.95f), // fills the room, under the overlay
            ];

            return new DeckPlan(walls, consoles, roomLabels, backdrops,
                spawnX: 0, spawnY: -3,     // stand mid-room, a few steps in from the gangway
                droidCount: 3, fillDroids: SeatPatrons, location: static (_, _) => "THE SPACE BAR");
        }

        // The bar's regulars — seated fixtures, a faint idle sway, deterministic in sim time (free,
        // stateless), mirroring the ship's droid infantry. One patron per booth (matches the consoles).
        private static void SeatPatrons(double simTime, DeckPlan.Droid[] patrons)
        {
            double sway = 0.05 * Math.Sin(simTime * 0.0009);
            patrons[0] = new DeckPlan.Droid(-7, 2.6 + sway, -Math.PI / 2, "Silas");
            patrons[1] = new DeckPlan.Droid(7, 2.6 - sway, -Math.PI / 2, "Coil");
            patrons[2] = new DeckPlan.Droid(0, 4.6 + sway, -Math.PI / 2, "Gilt-Eye");
        }
    }
}
