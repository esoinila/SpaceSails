namespace SpaceSails.Core;

/// <summary>
/// #488 · The derelict's GEOMETRY, in Core so it can be audited.
///
/// <para>This is the same split the surfaces already use — <see cref="SurfaceLayout"/> holds the shape in
/// Core and the client wraps it into a DeckPlan. It exists here for one reason: the owner boarded the
/// LONG SHRIFT and found the ship sealed in half by its own damage, with every build green, because the
/// layout lived in the client where no test could walk it.</para>
///
/// <para>Now <see cref="DeckReachability"/> can. A wreck whose compartments cannot be reached from the
/// airlock fails CI instead of failing a captain.</para>
/// </summary>
public static class WreckLayout
{
    // The hull, in deck units. A long thin ship: bow to the right, engineering aft to the left, one spine
    // corridor down the middle, compartments hanging off it.
    public const float AftX = -34f;
    public const float BowX = 26f;
    public const float TopY = -9f;
    public const float BottomY = 9f;

    /// <summary>Half the spine corridor's height — the corridor runs from −<see cref="SpineHalfHeight"/>
    /// to +<see cref="SpineHalfHeight"/>.</summary>
    public const float SpineHalfHeight = 3f;

    /// <summary>Where the shuttle puts the away team down — just inside the wreck's airlock, on the spine.
    /// Deliberately AT a doorway, so the first compartment is one step away.</summary>
    public const double SpawnX = 18.0;

    /// <summary>Spawn Y — on the spine.</summary>
    public const double SpawnY = 0.0;

    /// <summary>Half-width of a doorway through the spine wall. The first cut used 1.0 and the wreck was
    /// unwalkable: a 2 du gap minus the avatar's 1.4 du diameter leaves a 0.6 du slot nobody can find.</summary>
    public const float DoorHalfWidth = 3.0f;

    /// <summary>The compartments, bow to aft. Bounds are CONTIGUOUS on purpose — one ends exactly where the
    /// next begins. Leaving gaps between them created 1 du dead slots, walled both sides and narrower than
    /// the captain: traps with no way in that existed only to go wrong.</summary>
    /// <para>The aft-most rooms run all the way to the transom and the bow-most stop where the hull starts
    /// tapering — otherwise each end leaves a strip of ship walled off from everything, which is the same
    /// dead-slot mistake as the gaps, just at the ends where it is easier to miss.</para>
    public static readonly (string Name, float X0, float X1, bool Top)[] Compartments =
    [
        ("BRIDGE", 13f, BowX - 6, true),
        ("CREW SPACES", 0f, 13f, true),
        // The bottom row had nothing at the bow — a strip of ship with no name, reachable but belonging to
        // nothing. The audit spotted the asymmetry; she gets a room instead of a remainder.
        ("FORWARD LOCKER", 13f, BowX - 6, false),
        ("LIFEBOAT CRADLES", 0f, 13f, false),
        ("DEEP HOLD", -15f, 0f, true),
        ("NEAR HOLD", -15f, 0f, false),
        ("ENGINEERING", AftX, -15f, true),
        ("REACTOR SPACES", AftX, -15f, false),
    ];

    /// <summary>Where the spine opens into each compartment. ONE list, read by the wall builder (which
    /// leaves the gaps) AND the door builder (which draws them), so a doorway can never be cut somewhere
    /// the player is not shown — a gap nobody can see is the same as no gap at all.
    ///
    /// <para>Ascending, because the wall walk runs aft-to-bow and consumes them in order.</para></summary>
    public static float[] DoorCentres()
    {
        float[] centres = [-24f, -7f, 7f, (float)SpawnX];
        System.Array.Sort(centres); // order-proof: never trust the literal's order
        return centres;
    }

    /// <summary>The playable bounds an audit sweeps — the hull with a margin.</summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) Bounds =>
        (AftX - 2, TopY - 2, BowX + 2, BottomY + 2);

    /// <summary>Every wall on the wreck: hull, spine (with its doorways), compartment bulkheads, and the
    /// damage that killed her. This is the exact geometry the client turns into a DeckPlan, so what the
    /// audit walks is what the captain walks.</summary>
    public static IReadOnlyList<SurfaceCollision.Segment> Walls(Derelict.WreckCause cause)
    {
        var walls = new List<SurfaceCollision.Segment>();

        // Outer shell. The bow tapers; the aft is a flat transom where the drive used to be.
        walls.Add(new(AftX, TopY, BowX - 6, TopY));
        walls.Add(new(AftX, BottomY, BowX - 6, BottomY));
        walls.Add(new(BowX - 6, TopY, BowX, -2f));
        walls.Add(new(BowX - 6, BottomY, BowX, 2f));
        walls.Add(new(BowX, -2f, BowX, 2f));
        walls.Add(new(AftX, TopY, AftX, BottomY));

        // The spine corridor: two long walls, broken by a doorway into each compartment.
        foreach ((float x0, float x1) in SpineSegments())
        {
            walls.Add(new(x0, -SpineHalfHeight, x1, -SpineHalfHeight));
            walls.Add(new(x0, SpineHalfHeight, x1, SpineHalfHeight));
        }

        // Compartment bulkheads.
        foreach ((string _, float x0, float x1, bool top) in Compartments)
        {
            float yIn = top ? -SpineHalfHeight : SpineHalfHeight;
            float yOut = top ? TopY : BottomY;
            walls.Add(new(x0, yIn, x0, yOut));
            walls.Add(new(x1, yIn, x1, yOut));
        }

        walls.AddRange(DamageWalls(cause));
        return walls;
    }

    /// <summary>The spine's wall runs, with the doorways left out.</summary>
    public static IEnumerable<(float X0, float X1)> SpineSegments()
    {
        float[] doors = DoorCentres();
        float x = AftX;
        foreach (float d in doors)
        {
            if (d - DoorHalfWidth > x)
            {
                yield return (x, d - DoorHalfWidth);
            }
            x = System.Math.Max(x, d + DoorHalfWidth);
        }

        // Never emit a reversed tail: a segment whose start has passed its end is not a wall, it is a bug
        // that reads as one — and it once drew straight back over the doorway the loop had just cut.
        if (x < BowX - 6)
        {
            yield return (x, BowX - 6);
        }
    }

    /// <summary>
    /// What killed her, as geometry — drawn INTO the hull so the cause is legible before anyone reads a
    /// console.
    ///
    /// <para><b>THE RULE: damage may never seal the spine.</b> The corridor is the only way fore-and-aft,
    /// so anything spanning it cuts the ship in half. Two mutiny barricades did exactly that and made the
    /// whole aft end — including the cargo manifest — unreachable. Damage that belongs IN the corridor
    /// (barricades) must leave a gap wider than the captain; damage that crosses the ship (a breach) is
    /// drawn as the holes it made, not as a line through the middle. <c>WreckLayoutTests</c> walks every
    /// cause with A* and fails if this is ever broken again.</para>
    /// </summary>
    public static IEnumerable<SurfaceCollision.Segment> DamageWalls(Derelict.WreckCause cause)
    {
        switch (cause)
        {
            case Derelict.WreckCause.ReactorCascade:
                // The aft third is gone — the transom peeled outward. Entirely outside the hull.
                yield return new(AftX, -6f, AftX - 5f, -9f);
                yield return new(AftX, 6f, AftX - 5f, 9f);
                yield return new(AftX - 5f, -9f, AftX - 5f, 9f);
                break;

            case Derelict.WreckCause.HullBreach:
                // Where it went in, and where it came out. It really did pass straight through her, but the
                // damage is the two HOLES, not a line across the ship — a wall spanning hull to hull would
                // cross the spine and cut the wreck in half.
                yield return new(-2f, TopY, 2f, TopY + 2.5f);
                yield return new(-2f, BottomY - 2.5f, 2f, BottomY);
                break;

            case Derelict.WreckCause.Piracy:
                // The near hold opened from outside, its plating cut away.
                yield return new(-14f, BottomY, -2f, BottomY);
                break;

            case Derelict.WreckCause.Infested:
                // The crew barricaded the spine from the INSIDE and it did not help. Same weave rule as a
                // mutiny — half the corridor each, never a seal — because the retreat has to stay open.
                // It is the fighting withdrawal that makes this wreck worth boarding.
                yield return new(-12f, -SpineHalfHeight, -12f, 0f);
                yield return new(-3f, 0f, -3f, SpineHalfHeight);
                break;

            case Derelict.WreckCause.Mutiny:
                // Two barricades facing each other down the spine — each covering exactly HALF the corridor,
                // on opposite sides, so the captain weaves through. They spanned the full 6 du at first and
                // sealed the ship in half; then they left 2 du, which passed the reachability audit but the
                // owner still had to thread it ("a couple walkways are a bit narrow"). Half the corridor
                // each leaves a 3 du gap — room to walk it badly, which is the actual bar.
                //
                // Better fiction, too: a barricade nobody can get round is a wall. These are what two
                // frightened watches actually built, and what the other side eventually got past.
                yield return new(-1f, -SpineHalfHeight, -1f, 0f);
                yield return new(4f, 0f, 4f, SpineHalfHeight);
                break;

            default:
                // DriveFailure, LifeSupportFailure, NavigationalError, InsuranceJob — she is INTACT, which
                // is its own kind of wrong. Nothing to draw; that IS the finding.
                break;
        }
    }

    // ── Where things stand ────────────────────────────────────────────────────────────────────────────
    //
    // ONE definition per station, read by BOTH the client (which places the console) and the audit (which
    // walks to it). They were separate literals for one commit and immediately drifted — the log moved in
    // Core and stayed put in the client — which is the same duplication that let a doorway be cut where the
    // player was never shown one. A station the audit walks to must be the station the captain presses E on.

    /// <summary>The way home — just along the spine from the spawn.</summary>
    public static DeckReachability.Point ShuttleStation => new(SpawnX + 4f, 0f);

    /// <summary>The cargo, and the decision about her: amidships in the near hold, where the cargo is. You
    /// cannot decide what to do with her from the bridge — you have to go and look at what she carried.</summary>
    public static DeckReachability.Point CargoStation => new(-7f, 6f);

    /// <summary>The bridge log. Clear of the BRIDGE's aft bulkhead — it sat exactly on it for a commit.</summary>
    public static DeckReachability.Point LogStation => new(16f, -6f);

    /// <summary>The cargo manifest, in the deep hold.</summary>
    public static DeckReachability.Point ManifestStation => new(-7f, -6f);

    /// <summary>Every place the captain must be able to REACH: the three evidence stations, the cargo
    /// decision, and the way home. This is the list CI walks.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> Stations(Derelict.WreckCause cause) =>
    [
        ("the way back to the shuttle", ShuttleStation),
        ("the cargo (the decision)", CargoStation),
        ("the bridge log", LogStation),
        ("the cargo manifest", ManifestStation),
        (CauseStationName(cause), CauseStation(cause)),
    ];

    /// <summary>Where the cause's own evidence stands.</summary>
    public static DeckReachability.Point CauseStation(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.ReactorCascade => new(-26f, -6f),
        Derelict.WreckCause.DriveFailure => new(-26f, 6f),
        // Not x=0: that is the bulkhead NEAR HOLD and LIFEBOAT CRADLES share, and a station standing ON a
        // wall cannot be walked to. The audit caught this on its very first run.
        Derelict.WreckCause.HullBreach => new(-4f, 6f),
        Derelict.WreckCause.LifeSupportFailure => new(7f, -6f),
        Derelict.WreckCause.NavigationalError => new(17f, -6.5f),
        Derelict.WreckCause.Mutiny => new(7f, -6f),
        Derelict.WreckCause.Piracy => new(-7f, 6f),
        // The nest, deep aft where it has had years to spread.
        Derelict.WreckCause.Infested => new(-24f, 6f),
        Derelict.WreckCause.InsuranceJob => new(7f, 6f),
        _ => new(0f, 0f),
    };

    /// <summary>The cause station's name, for a readable failure.</summary>
    public static string CauseStationName(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.ReactorCascade => "the reactor spaces",
        Derelict.WreckCause.DriveFailure => "the drive bells",
        Derelict.WreckCause.HullBreach => "the hole through her",
        Derelict.WreckCause.LifeSupportFailure => "the scrubber stacks",
        Derelict.WreckCause.NavigationalError => "the nav post",
        Derelict.WreckCause.Mutiny => "the arms locker",
        Derelict.WreckCause.Piracy => "the stripped hold",
        Derelict.WreckCause.Infested => "the nest in the deep hold",
        Derelict.WreckCause.InsuranceJob => "the lifeboat cradles",
        _ => "the wreck",
    };
}
