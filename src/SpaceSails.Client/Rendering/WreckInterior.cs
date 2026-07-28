using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #488 · THE WRECK YOU WALK THROUGH. A derelict is neither a world nor a berth, so it gets neither the
/// regolith field (<see cref="MoonSurface"/>) nor a station concourse (<see cref="HavenInterior"/>) — it
/// gets a dead ship: a spine corridor, compartments off it, and the airlock you came in through.
///
/// <para>The layout is the same for every wreck, because a hull is a hull. What CHANGES with the cause is
/// where the damage is and what the evidence consoles say — <see cref="Derelict.Evidence"/> is a sentence,
/// and this file is where that sentence becomes somewhere you stand. A reactor cascade eats the aft
/// compartment; a hull breach punches a line through the beam; a staged loss leaves the lifeboat cradles
/// conspicuously, deliberately empty.</para>
///
/// <para>Deterministic: the same wreck always builds the same hull, so a revisit finds the bodies where it
/// left them.</para>
/// </summary>
public static class WreckInterior
{
    // The hull, in deck units. A long thin ship: bow to the right, engineering aft to the left, one spine
    // corridor down the middle, four compartments hanging off it.
    private const float AftX = -34f;
    private const float BowX = 26f;
    private const float TopY = -9f;
    private const float BottomY = 9f;

    /// <summary>Where the shuttle puts the away team down — just inside the wreck's airlock, amidships.</summary>
    public const double SpawnX = 18.0;

    /// <summary>Spawn Y — on the spine.</summary>
    public const double SpawnY = 0.0;

    /// <summary>The compartments, bow to aft. Each is a place with a name, because "you are in a room" is
    /// most of what makes a wreck a place rather than a map.</summary>
    private static readonly (string Name, float X0, float X1, bool Top)[] Compartments =
    [
        ("BRIDGE", 14f, 25f, true),
        ("CREW SPACES", 1f, 13f, true),
        ("LIFEBOAT CRADLES", 1f, 13f, false),
        ("DEEP HOLD", -15f, 0f, true),
        ("NEAR HOLD", -15f, 0f, false),
        ("ENGINEERING", -33f, -16f, true),
        ("REACTOR SPACES", -33f, -16f, false),
    ];

    /// <summary>Build the derelict's walkable interior for a given wreck.</summary>
    public static DeckPlan WreckDeck(
        in Derelict.Wreck wreck,
        System.Collections.Generic.IReadOnlySet<string> examined,
        bool salvaged,
        int droidCount,
        System.Action<double, DeckPlan.Droid[]> fillDroids)
    {
        System.ArgumentNullException.ThrowIfNull(fillDroids);
        examined ??= new System.Collections.Generic.HashSet<string>();

        var walls = new System.Collections.Generic.List<DeckPlan.Wall>();
        var consoles = new System.Collections.Generic.List<DeckPlan.ConsoleSpot>();
        var labels = new System.Collections.Generic.List<(float X, float Y, string Text)>();

        // ── The hull ──────────────────────────────────────────────────────────────────────────────────
        // Outer shell. The bow tapers; the aft is a flat transom where the drive used to be.
        walls.Add(new DeckPlan.Wall(AftX, TopY, BowX - 6, TopY, false, true));
        walls.Add(new DeckPlan.Wall(AftX, BottomY, BowX - 6, BottomY, false, true));
        walls.Add(new DeckPlan.Wall(BowX - 6, TopY, BowX, -2f, false, true));   // bow taper, top
        walls.Add(new DeckPlan.Wall(BowX - 6, BottomY, BowX, 2f, false, true)); // bow taper, bottom
        walls.Add(new DeckPlan.Wall(BowX, -2f, BowX, 2f, true, true));          // the bridge window
        walls.Add(new DeckPlan.Wall(AftX, TopY, AftX, BottomY, false, true));   // the transom

        // The spine corridor: two long walls with gaps left as doorways into each compartment.
        foreach ((float x0, float x1) in SpineSegments())
        {
            walls.Add(new DeckPlan.Wall(x0, -3f, x1, -3f, false, false));
            walls.Add(new DeckPlan.Wall(x0, 3f, x1, 3f, false, false));
        }

        // Compartment dividers.
        foreach ((string name, float x0, float x1, bool top) in Compartments)
        {
            float yIn = top ? -3f : 3f;
            float yOut = top ? TopY : BottomY;
            walls.Add(new DeckPlan.Wall(x0, yIn, x0, yOut, false, false));
            walls.Add(new DeckPlan.Wall(x1, yIn, x1, yOut, false, false));
            labels.Add(((x0 + x1) / 2f, top ? TopY + 2f : BottomY - 1.5f, name));
        }

        // ── The damage ────────────────────────────────────────────────────────────────────────────────
        // What killed her is drawn INTO the hull, so the cause is legible before anyone reads a console.
        foreach (DeckPlan.Wall w in DamageWalls(wreck.Cause))
        {
            walls.Add(w);
        }

        // ── The way home ──────────────────────────────────────────────────────────────────────────────
        consoles.Add(new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ShuttleAirlock, (float)SpawnX + 4f, 0f, "🛸 BACK TO THE SHUTTLE"));
        labels.Add(((float)SpawnX + 4f, 5.5f, "— " + wreck.ShipName.ToUpperInvariant() + " —"));

        // ── The evidence ──────────────────────────────────────────────────────────────────────────────
        // Three stations to read her by. The FIRST is always the cause's own evidence, wherever that
        // damage is; the others are the ship's own record — the log and the manifest — which is what lets
        // a careful captain catch a wreck that lies (Derelict.MisreadsAs).
        foreach ((string id, float x, float y, string label) in EvidenceSpots(wreck.Cause))
        {
            bool done = examined.Contains(id);
            consoles.Add(new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.WreckEvidence, x, y, done ? "✔ " + label : label));
        }

        // ── The decision ──────────────────────────────────────────────────────────────────────────────
        // Amidships in the near hold, where the cargo actually is. You cannot decide what to do with her
        // from the bridge — you have to go and look at what she was carrying.
        if (!salvaged)
        {
            consoles.Add(new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.WreckSalvage, -7f, 6f, "📋 THE CARGO — AND WHAT TO DO ABOUT HER"));
        }

        return new DeckPlan(
            [.. walls], [.. consoles], [.. labels], [],
            spawnX: SpawnX, spawnY: SpawnY,
            droidCount: droidCount, fillDroids: fillDroids,
            location: (x, y) => LocationName(x, y),
            doors: [], shipFixtures: false, followCam: true, tables: []);
    }

    // The spine's walls, broken by a doorway into each compartment.
    private static System.Collections.Generic.IEnumerable<(float X0, float X1)> SpineSegments()
    {
        // Doorway centres, one per compartment pair, with 2 du openings.
        float[] doors = [20f, 7f, -7f, -24f];
        float x = AftX;
        foreach (float d in doors)
        {
            if (d - 1f > x)
            {
                yield return (x, d - 1f);
            }
            x = d + 1f;
        }
        yield return (x, BowX - 6);
    }

    // What killed her, as geometry. Drawn as hull-kind walls so the renderer treats them as structure.
    private static System.Collections.Generic.IEnumerable<DeckPlan.Wall> DamageWalls(Derelict.WreckCause cause)
    {
        switch (cause)
        {
            case Derelict.WreckCause.ReactorCascade:
                // The aft third is gone — the transom peeled outward.
                yield return new DeckPlan.Wall(AftX, -6f, AftX - 5f, -9f, false, true);
                yield return new DeckPlan.Wall(AftX, 6f, AftX - 5f, 9f, false, true);
                yield return new DeckPlan.Wall(AftX - 5f, -9f, AftX - 5f, 9f, false, true);
                break;

            case Derelict.WreckCause.HullBreach:
                // A line straight through the beam — you can see the stars through her.
                yield return new DeckPlan.Wall(-2f, TopY, 2f, BottomY, true, true);
                break;

            case Derelict.WreckCause.Piracy:
                // The near hold opened from outside, its plating cut away.
                yield return new DeckPlan.Wall(-14f, BottomY, -2f, BottomY, true, true);
                break;

            case Derelict.WreckCause.Mutiny:
                // Two barricades facing each other down the spine.
                yield return new DeckPlan.Wall(-1f, -3f, -1f, 3f, false, false);
                yield return new DeckPlan.Wall(4f, -3f, 4f, 3f, false, false);
                break;

            default:
                // DriveFailure, LifeSupportFailure, NavigationalError, InsuranceJob — she is INTACT, which
                // is its own kind of wrong. Nothing to draw; that IS the finding.
                break;
        }
    }

    // The three places you read her by. The first is the cause's own damage; the log and the manifest are
    // always aboard, and always where a ship keeps them.
    private static (string Id, float X, float Y, string Label)[] EvidenceSpots(Derelict.WreckCause cause) =>
    [
        CauseSpot(cause),
        ("log", 20f, -6f, "🖥 THE BRIDGE LOG"),
        ("manifest", -7f, -6f, "📦 THE CARGO MANIFEST"),
    ];

    private static (string Id, float X, float Y, string Label) CauseSpot(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.ReactorCascade => ("cause", -26f, -6f, "☢ THE REACTOR SPACES"),
        Derelict.WreckCause.DriveFailure => ("cause", -26f, 6f, "🔧 THE DRIVE BELLS"),
        Derelict.WreckCause.HullBreach => ("cause", 0f, 6f, "🕳 THE HOLE THROUGH HER"),
        Derelict.WreckCause.LifeSupportFailure => ("cause", 7f, -6f, "🌬 THE SCRUBBER STACKS"),
        Derelict.WreckCause.NavigationalError => ("cause", 20f, -6.5f, "🧭 THE NAV POST"),
        Derelict.WreckCause.Mutiny => ("cause", 7f, -6f, "🔒 THE ARMS LOCKER"),
        Derelict.WreckCause.Piracy => ("cause", -7f, 6f, "📦 THE STRIPPED HOLD"),
        Derelict.WreckCause.InsuranceJob => ("cause", 7f, 6f, "🚀 THE LIFEBOAT CRADLES"),
        _ => ("cause", 0f, 0f, "THE WRECK"),
    };

    /// <summary>Which compartment a point stands in — the header line the HUD reads.</summary>
    private static string LocationName(double x, double y)
    {
        foreach ((string name, float x0, float x1, bool top) in Compartments)
        {
            bool inX = x >= x0 && x <= x1;
            bool inY = top ? y < -3 : y > 3;
            if (inX && inY)
            {
                return name;
            }
        }
        return System.Math.Abs(y) <= 3 ? "THE SPINE" : "THE HULL";
    }
}
