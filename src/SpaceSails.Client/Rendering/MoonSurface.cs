namespace SpaceSails.Client.Rendering;

/// <summary>
/// PR-295 · The walked bury scene. The owner buried his first chest on Miranda and got "just the array
/// of pop-ups"; he wanted to WALK — "as if the shuttle was the door." So the shuttle bay (now on the
/// ship's bottom edge — the wild side) grows a TUBE DOWN to a barren moon surface, welded on exactly
/// like the station boarding tube grows UP to a lobby (owner's geography law: top = civilization,
/// bottom = the wild). You step off, carry the chest out to the dig spot, press [E] to bury, and — if
/// the 2D6 Reevers turned out — sprint back up the tube to the crew-only door they cannot cross.
///
/// <para>No new renderer: this is the same crude <c>DeckPlan</c> grid every interior uses (owner: "the
/// NetHack/TTRPG grid IS the table map"), just reskinned barren — a fenced landing area of hull-line
/// rim walls, a regolith floor, and one dig site. The wing SLOT is destination-selected: different
/// moons plug their own surface in here even though only the bury site ships today. The <c>fillDroids</c>
/// delegate is supplied by the caller so the live, chasing Reevers (which are NOT a pure function of
/// sim time) can be drawn through the same droid buffer as the ship's stationary crew.</para>
/// </summary>
public static class MoonSurface
{
    // The down-tube mouth is the ship's bottom-hull SHUTTLE-BAY HATCH (DeckPlan.ShuttleHatchX1..X2).
    private const float TubeLeft = DeckPlan.ShuttleHatchX1;   // -9
    private const float TubeRight = DeckPlan.ShuttleHatchX2;  // -5

    /// <summary>The surface's top rim / tube mouth. The regolith hangs below the ship's bottom hull.</summary>
    public const float SurfaceTopY = -20f;
    private const float SurfaceBottomY = -44f;
    private const float SurfaceLeftX = -22f;
    private const float SurfaceRightX = 10f;

    /// <summary>Where the chest goes into the ground — the [E] DigSite console, out in the open so the
    /// walk there (and the sprint back) is a real crossing.</summary>
    public const float DigSiteX = -6f;
    public const float DigSiteY = -33f;

    /// <summary>The crew-only threshold (owner addendum): Reevers are penned on the surface at the tube
    /// mouth and can never climb it — the door won't open to them. Fed to <c>ReeverChase.Step</c>.</summary>
    public const double ReeverBarrierY = SurfaceTopY;

    /// <summary>The avatar's spawn stepping off the shuttle — just inside the tube mouth, facing out
    /// onto the surface.</summary>
    public const double SpawnX = -7.0;
    public const double SpawnY = SurfaceTopY - 1.5;

    /// <summary>True once the digger is back in the tube / aboard — clear of every Reever by the
    /// crew-only-door law. The sprint is won here.</summary>
    public static bool IsSafeAboard(double avatarY) => avatarY > SurfaceTopY;

    /// <summary>
    /// Build the ship + down-tube + barren surface as one continuous walkable plan for a bury on
    /// <paramref name="bodyDisplayName"/>. <paramref name="droidCount"/> and <paramref name="fillDroids"/>
    /// come from the caller so the ship's crew AND the live Reevers ride the one droid buffer.
    /// </summary>
    public static DeckPlan SurfaceDeck(
        string bodyDisplayName, int droidCount, System.Action<double, DeckPlan.Droid[]> fillDroids)
    {
        System.ArgumentNullException.ThrowIfNull(fillDroids);
        DeckPlan ship = DeckPlan.Ship;

        // Start from the ship, minus the sealed bottom-hull hatch (the surface opens it) — the same
        // move BuildComplex makes with the top airlock hatch.
        var sealedHatch = new DeckPlan.Wall(TubeLeft, DeckPlan.ShuttleHatchY, TubeRight, DeckPlan.ShuttleHatchY, false, true);
        var walls = new List<DeckPlan.Wall>(ship.Walls.Where(w => !w.Equals(sealedHatch)));
        var doors = new List<DeckPlan.Door>(ship.Doors.Where(d => !IsHatchDoor(d)));
        var labels = new List<(float X, float Y, string Text)>(ship.RoomLabels);

        // The down-tube: two hull walls from the ship's bottom hatch to the surface rim, with an
        // auto-door at each end — the same tube grammar as the station boarding tube, pointed down.
        walls.Add(new(TubeLeft, DeckPlan.ShuttleHatchY, TubeLeft, SurfaceTopY, false, true));
        walls.Add(new(TubeRight, DeckPlan.ShuttleHatchY, TubeRight, SurfaceTopY, false, true));
        doors.Add(new(TubeLeft, DeckPlan.ShuttleHatchY, TubeRight, DeckPlan.ShuttleHatchY)); // ship-end: the crew-only door
        doors.Add(new(TubeLeft, SurfaceTopY, TubeRight, SurfaceTopY));                       // surface-end auto-door

        // The barren landing area: a fenced rim of hull lines with the tube mouth open at the top.
        walls.Add(new(SurfaceLeftX, SurfaceTopY, TubeLeft, SurfaceTopY, false, true));   // top rim, port of the tube
        walls.Add(new(TubeRight, SurfaceTopY, SurfaceRightX, SurfaceTopY, false, true)); // top rim, starboard of the tube
        walls.Add(new(SurfaceLeftX, SurfaceTopY, SurfaceLeftX, SurfaceBottomY, false, true));
        walls.Add(new(SurfaceRightX, SurfaceTopY, SurfaceRightX, SurfaceBottomY, false, true));
        walls.Add(new(SurfaceLeftX, SurfaceBottomY, SurfaceRightX, SurfaceBottomY, false, true));

        var consoles = new List<DeckPlan.ConsoleSpot>(
            ship.Consoles.Where(c => c.Kind != DeckPlan.ConsoleKind.Airlock))
        {
            // The dig site out on the regolith — walk up, press E to bury (or, on a return, to dig).
            new(DeckPlan.ConsoleKind.DigSite, DigSiteX, DigSiteY, "⛏ DIG HERE"),
            // The way home: board the shuttle at the tube mouth. Kept clear of the tube walls.
            new(DeckPlan.ConsoleKind.SurfaceAirlock, -7f, SurfaceTopY - 3f, "🚀 BOARD THE SHUTTLE"),
        };

        labels.Add((DigSiteX, SurfaceTopY - 6, $"— {bodyDisplayName.ToUpperInvariant()} SURFACE —"));
        labels.Add(((SurfaceLeftX + SurfaceRightX) / 2, SurfaceBottomY + 3, "REGOLITH · NO ATMOSPHERE"));

        var backdrops = new List<DeckPlan.Backdrop>(ship.Backdrops);

        return new DeckPlan(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(),
            spawnX: SpawnX, spawnY: SpawnY,
            droidCount: droidCount, fillDroids: fillDroids,
            location: (x, y) => y > DeckPlan.ShuttleHatchY ? ship.Location(x, y)
                              : y > SurfaceTopY ? "DOWN-TUBE"
                              : y > SurfaceTopY - 5 ? "LANDING AREA"
                              : $"{bodyDisplayName.ToUpperInvariant()} SURFACE",
            doors: null, shipFixtures: true, followCam: true, tables: ship.Tables);
    }

    // The ship carries one amber shuttle-airlock door across the (now bottom) hatch; drop it so the
    // tube's own doors take over the threshold.
    private static bool IsHatchDoor(DeckPlan.Door d) =>
        System.Math.Abs(d.Y1 - DeckPlan.ShuttleHatchY) < 0.01f && System.Math.Abs(d.Y2 - DeckPlan.ShuttleHatchY) < 0.01f;
}
