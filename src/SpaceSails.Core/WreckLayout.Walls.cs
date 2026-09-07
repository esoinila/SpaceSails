namespace SpaceSails.Core;

/// <summary>
/// EVERY WALL ON THE WRECK — hull, spine with its doorways, compartment bulkheads, and the damage that
/// killed her.
///
/// <para>This is the exact geometry the client turns into a <c>DeckPlan</c>, so what the audit walks is
/// what the captain walks. On the vented hull the damage is not structural at all: it is which side of
/// every hatch the dogs are on, and the vacuum behind them.</para>
///
/// <para>Split out of <c>WreckLayout.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class WreckLayout
{
    /// <summary>Every wall on the wreck: hull, spine (with its doorways), compartment bulkheads, and the
    /// damage that killed her. This is the exact geometry the client turns into a DeckPlan, so what the
    /// audit walks is what the captain walks.</summary>
    public static IReadOnlyList<SurfaceCollision.Segment> Walls(Derelict.WreckCause cause) =>
        Walls(cause, null);

    /// <summary>
    /// #537 slice 3 · THE SAME HULL, WITH A HOLE CUT IN HER. <paramref name="opened"/> is the one void this
    /// captain has cut into on this boarding, or null on every hull nobody has opened — which is every hull,
    /// almost always, so the ordinary geometry above is byte-for-byte what it was.
    ///
    /// <para><b>Two changes and no more.</b> The pressure hull is broken by a
    /// <see cref="HullStowage.PlateHalfWidth"/> gap at the plate — unless the plate is fitted back in, in
    /// which case it is a wall again and the captain behind it is hidden by #324's law rather than by a
    /// stealth flag. And the pocket gets an end at each of its own ends, so a cut into six frames of
    /// shielding is a hole six frames long and not the run of the ship: a captain who could walk the whole
    /// band would be able to enter any compartment through its outboard wall, which is not a hiding place,
    /// it is a second corridor.</para>
    /// </summary>
    public static IReadOnlyList<SurfaceCollision.Segment> Walls(
        Derelict.WreckCause cause, HullStowage.OpenVoid? opened)
    {
        var walls = new List<SurfaceCollision.Segment>();

        // Outer shell. The bow tapers; the aft is a flat transom where the drive used to be — and it now sits
        // a MACHINERY SPACE aft of the last bulkhead rather than flush against it, because a ship is her rooms
        // plus everything that makes the rooms work.
        AddPressureHull(walls, TopY, top: true, opened);
        AddPressureHull(walls, BottomY, top: false, opened);
        walls.Add(new(BowX - 6, TopY, BowX, -NoseHalfHeight));
        walls.Add(new(BowX - 6, BottomY, BowX, NoseHalfHeight));
        walls.Add(new(BowX, -NoseHalfHeight, BowX, NoseHalfHeight));
        walls.Add(new(TransomX, TopY, TransomX, BottomY));

        // …and the aft bulkhead that closes the pressure hull off from it. The machinery space is OUTSIDE the
        // part of her that ever held air, which is why nothing walks into it by accident.
        walls.Add(new(AftX, TopY, AftX, BottomY));

        // #537 · THE SHIELDING BAND. Two long enclosed boxes outboard of the pressure hull, closed at both
        // ends — normally solid ship, and on a hull with something to hide, one section of it is not. Present on
        // EVERY cause: a band that only appeared on ships with a void would announce them.
        walls.Add(new(TransomX, OuterTopY, ShieldingForwardEnd, OuterTopY));
        walls.Add(new(TransomX, OuterBottomY, ShieldingForwardEnd, OuterBottomY));
        walls.Add(new(TransomX, OuterTopY, TransomX, TopY));
        walls.Add(new(TransomX, BottomY, TransomX, OuterBottomY));
        walls.Add(new(ShieldingForwardEnd, OuterTopY, ShieldingForwardEnd, TopY));
        walls.Add(new(ShieldingForwardEnd, BottomY, ShieldingForwardEnd, OuterBottomY));

        // …and the two ends of a pocket somebody has cut into it. Present only once the plate is out, because
        // until then there is nothing in there to be at either end of.
        if (opened is { } pocket)
        {
            float yOut = pocket.Top ? OuterTopY : OuterBottomY;
            float yIn = pocket.Top ? TopY : BottomY;
            walls.Add(new((float)pocket.X0, yIn, (float)pocket.X0, yOut));
            walls.Add(new((float)pocket.X1, yIn, (float)pocket.X1, yOut));
        }

        // The spine corridor: two long walls, broken by a doorway into each compartment.
        foreach ((float x0, float x1) in SpineSegments())
        {
            walls.Add(new(x0, -SpineHalfHeight, x1, -SpineHalfHeight));
            walls.Add(new(x0, SpineHalfHeight, x1, SpineHalfHeight));
        }

        // Compartment bulkheads. The hull's own ends stay single lines — there is machinery space behind one
        // and the bow taper behind the other — but every bulkhead with a room on BOTH sides is a thin closed box
        // with a technical run inside it, because a wall that holds an atmosphere is not a line and the ship's
        // pipework has to go somewhere.
        foreach ((string _, float x0, float x1, bool top) in Compartments)
        {
            float yIn = top ? -SpineHalfHeight : SpineHalfHeight;
            float yOut = top ? TopY : BottomY;

            foreach (float x in new[] { x0, x1 })
            {
                if (x == AftX || x == BowX - 6)
                {
                    walls.Add(new(x, yIn, x, yOut));
                }
            }
        }

        foreach (bool top in new[] { true, false })
        {
            float yIn = top ? -SpineHalfHeight : SpineHalfHeight;
            float yOut = top ? TopY : BottomY;
            float half = BulkheadDepth / 2f;

            foreach (float x in InteriorBulkheads(top))
            {
                walls.Add(new(x - half, yIn, x - half, yOut));
                walls.Add(new(x + half, yIn, x + half, yOut));
                walls.Add(new(x - half, yIn, x + half, yIn));
                walls.Add(new(x - half, yOut, x + half, yOut));
            }
        }

        // The away team's own lock across the spine: two stubs off the corridor walls with a passage
        // between them. Present on EVERY cause, because it is the shuttle's lock and not the wreck's — the
        // team brought it with them and dogged it behind themselves.
        walls.Add(new(ShuttleLockX, -SpineHalfHeight, ShuttleLockX, -ShuttleLockGapHalf));
        walls.Add(new(ShuttleLockX, ShuttleLockGapHalf, ShuttleLockX, SpineHalfHeight));

        walls.AddRange(DamageWalls(cause));
        return walls;
    }

    /// <summary>
    /// ONE SIDE OF THE PRESSURE HULL, WITH OR WITHOUT A HOLE IN IT. The hole is cut the same way the spine's
    /// doorways are — by laying two runs and leaving a gap between them, never by drawing a wall and then
    /// pretending it is transparent. A gap the collision field does not have is a gap nothing can walk
    /// through, and a wall the player is shown open that still stops a body is this repo's third named bug
    /// class (the sim doing one thing while a drawn shape reports another).
    /// </summary>
    private static void AddPressureHull(
        List<SurfaceCollision.Segment> walls, float y, bool top, HullStowage.OpenVoid? opened)
    {
        if (opened is not { PlateShut: false } pocket || pocket.Top != top)
        {
            walls.Add(new(TransomX, y, BowX - 6, y));
            return;
        }

        float gapAft = (float)(pocket.PlateX - HullStowage.PlateHalfWidth);
        float gapFwd = (float)(pocket.PlateX + HullStowage.PlateHalfWidth);

        walls.Add(new(TransomX, y, gapAft, y));
        walls.Add(new(gapFwd, y, BowX - 6, y));
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
                // DriveFailure, LifeSupportFailure, NavigationalError, InsuranceJob and
                // VentedByOneOfTheirOwn — she is INTACT, which is its own kind of wrong. Nothing to draw;
                // that IS the finding. On the vented hull the damage is not structural at all: it is which
                // side of every hatch the dogs are on, and the vacuum behind them.
                break;
        }
    }
}
