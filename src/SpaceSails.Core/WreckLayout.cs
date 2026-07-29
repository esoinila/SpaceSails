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

    /// <summary>
    /// THE AWAY TEAM'S OWN LOCK, across the spine between the wreck and the shuttle. Owner: <i>"Let's keep
    /// the shuttle door locked in such a way that we don't vent our own shuttle by accident. Also we don't
    /// want any uninvited infestations going there."</i>
    ///
    /// <para>Two jobs, one bulkhead. It is the boundary the ship's atmosphere stops at — crack every valve
    /// on this hull and the shuttle never notices — and it is a CREW-ONLY door, the same rule the ship's own
    /// tube runs on: the away team work it, and nothing else aboard can. The pack has never operated a
    /// hatch and is not going to start.</para>
    ///
    /// <para>It is deliberately AFT of <see cref="ShuttleStation"/> and FORWARD of <see cref="SpawnX"/>, so
    /// the team lands inside the ship having already come through it.</para>
    /// </summary>
    public const float ShuttleLockX = 21f;

    /// <summary>
    /// THE CREW-ONLY RULE, as a function rather than a line buried in the walk loop. Given where something
    /// that is not the away team wants to be, return where it is actually allowed to be.
    ///
    /// <para>The lock bulkhead has a passage cut in it — it has to, or the captain could not get home — so
    /// walls alone would let the pack walk it exactly the way the captain does. What stops them is the same
    /// rule the ship's own tube runs on: a hatch keyed to the crew. It can reach the door. It cannot open
    /// the door.</para>
    ///
    /// <para>This lives in Core so the invariant is PINNED BY A TEST instead of by a comment. "Nothing
    /// uninvited reaches the shuttle" is the kind of promise that is quietly broken by a refactor three
    /// months from now, and the owner would find out by watching something follow him home.</para>
    /// </summary>
    public static double HeldAtLock(double x, double radius) =>
        System.Math.Min(x, ShuttleLockX - radius);

    /// <summary>Whether this position is on the shuttle's side of the lock — where only the away team
    /// ever gets to stand.</summary>
    public static bool PastTheLock(double x, double radius) => x > ShuttleLockX - radius;

    /// <summary>Half-height of the gap through the lock bulkhead. Three units of passage — wider than the
    /// captain with room to walk it badly, which is the bar <c>WreckLayoutTests</c> holds every doorway to.</summary>
    public const float ShuttleLockGapHalf = 1.5f;

    /// <summary>
    /// THE LIFEBOAT CRADLES, on the outboard wall where a ship actually keeps them. Owner, on walking into
    /// the compartment named for them and finding an empty box: <i>"Are the lifeboats there or not … we
    /// should somehow see this like slots that are filled or empty … on the wall."</i>
    ///
    /// <para>Right, and it is the cheapest evidence in the game: a row of cradles you can COUNT from the
    /// doorway. No console to read, no die to roll — how many are empty is a fact about the room, and what
    /// it means is the captain's problem. It is also the seam the safety-card lane
    /// (<c>docs/features/safety-card.md</c>) was filed against.</para>
    /// </summary>
    public const int CradleCount = 6;

    /// <summary>Where each cradle sits: evenly along the LIFEBOAT CRADLES compartment's outboard wall.</summary>
    public static IEnumerable<(float X, float Y)> CradleSpots()
    {
        (string _, float x0, float x1, bool _) = System.Array.Find(
            Compartments, c => c.Name == LifeboatCompartment);

        float span = x1 - x0;
        for (int i = 0; i < CradleCount; i++)
        {
            // Inset half a step at each end so the row reads as spaced along the wall rather than
            // running into the bulkheads.
            float t = (i + 0.5f) / CradleCount;
            yield return (x0 + (span * t), BottomY - 1.2f);
        }
    }

    /// <summary>The compartment the cradles are in.</summary>
    public const string LifeboatCompartment = "LIFEBOAT CRADLES";

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

        // The away team's own lock across the spine: two stubs off the corridor walls with a passage
        // between them. Present on EVERY cause, because it is the shuttle's lock and not the wreck's — the
        // team brought it with them and dogged it behind themselves.
        walls.Add(new(ShuttleLockX, -SpineHalfHeight, ShuttleLockX, -ShuttleLockGapHalf));
        walls.Add(new(ShuttleLockX, ShuttleLockGapHalf, ShuttleLockX, SpineHalfHeight));

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

    /// <summary>The way home — up in the bow, deliberately CLEAR of the spawn.
    ///
    /// <para>It sat four units from the spawn, which put it inside the interact radius of the doorway the
    /// away team arrives in and must pass through to reach anything. Playtested: stepping bow-ward at all
    /// bounced the captain straight back to the ship. The exit should be somewhere you go ON PURPOSE, not
    /// something you fall through on your way past.</para></summary>
    public static DeckReachability.Point ShuttleStation => new(24f, 0f);

    /// <summary>The cargo, and the decision about her: amidships in the near hold, where the cargo is. You
    /// cannot decide what to do with her from the bridge — you have to go and look at what she carried.</summary>
    public static DeckReachability.Point CargoStation => new(-7f, 6f);

    /// <summary>The bridge log. Clear of the BRIDGE's aft bulkhead — it sat exactly on it for a commit.</summary>
    public static DeckReachability.Point LogStation => new(16f, -6f);

    /// <summary>The cargo manifest, in the deep hold.</summary>
    public static DeckReachability.Point ManifestStation => new(-7f, -6f);

    /// <summary>The scuttling panel, right aft with the reactor — standing at it means standing next to the
    /// thing you are about to overload. Its position lives HERE rather than in the client because that is
    /// the only way a test can see it: placed by eye in the renderer it landed exactly on top of the
    /// infested hull's nest station, and the game handed the captain the nest when they pressed E at the
    /// panel (owner: <i>"I don't see the scuttling panel here"</i>).</summary>
    public static DeckReachability.Point ScuttleStation => new(-31f, 6f);

    /// <summary>Every place the captain must be able to REACH: the three evidence stations, the cargo
    /// decision, the scuttling panel, and the way home. This is the list CI walks — and, since
    /// <c>WreckLayoutTests</c> also checks they do not stand on top of each other, the list that keeps two
    /// consoles from sharing a doorstep.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> Stations(Derelict.WreckCause cause) =>
    [
        ("the way back to the shuttle", ShuttleStation),
        ("the cargo (the decision)", CargoStation),
        ("the bridge log", LogStation),
        ("the cargo manifest", ManifestStation),
        ("the scuttling panel", ScuttleStation),
        (CauseStationName(cause), CauseStation(cause)),
    ];

    /// <summary>Where the cause's own evidence stands.</summary>
    public static DeckReachability.Point CauseStation(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.ReactorCascade => new(-26f, -6f),
        Derelict.WreckCause.DriveFailure => new(-26f, 6f),
        // Not x=0: that is the bulkhead NEAR HOLD and LIFEBOAT CRADLES share, and a station standing ON a
        // wall cannot be walked to. The audit caught this on its very first run.
        Derelict.WreckCause.HullBreach => new(-3.5f, 5.5f),
        Derelict.WreckCause.LifeSupportFailure => new(7f, -6f),
        Derelict.WreckCause.NavigationalError => new(18.6f, -7.7f),
        Derelict.WreckCause.Mutiny => new(7f, -6f),
        // Deeper into the near hold than the cargo console: the two sat on the SAME POINT for a long time,
        // which the separation test found the day it was written. Both belong in this room — the stripped
        // frames and the decision about what is left — they just cannot be in the same square metre.
        Derelict.WreckCause.Piracy => new(-12f, 6f),
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
