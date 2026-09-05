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

    /// <summary>
    /// #537 · THE SHIELDING BAND, outboard of the pressure hull the whole length of her parallel middle body.
    ///
    /// <para>Owner, on being shown that a hidden void had nowhere to physically BE — the compartments are
    /// contiguous, so every square metre of her was already spoken for: <i>"I guess making outside walls thicker
    /// (shielding etc) might offer less audited dimensions? Or having like technical plumbing space in the between
    /// walls?"</i> That is the right answer and it is also how spacecraft are actually built: whipple layers,
    /// radiation shielding, tankage, cable and plumbing runs all live between an inner pressure wall and an outer
    /// skin.</para>
    ///
    /// <para>It solves the problem structurally rather than by fiddling with numbers. A band outboard of the
    /// rooms has <b>no doorway to respect</b>, so a void can sit anywhere along her length — which the previous
    /// attempt could not manage: respecting each compartment's own door left the bow rooms with NEGATIVE margin
    /// and only the aft holds able to host anything. And it answers the owner's question directly: the room is
    /// not smaller on the inside. The space is between the walls, where it belongs.</para>
    ///
    /// <para><b>Every hull has it</b>, and that is the anti-tell rule (his own, from the valve boards): if only
    /// ships with something to hide carried a shielding band, finding a shielding band would name the ship.</para>
    /// </summary>
    public const float ShieldingDepth = 2.5f;

    /// <summary>The outer skin — the pressure hull plus her shielding. Only along the parallel middle body; the
    /// bow taper carries no band, because shielding runs down the sides of a ship and not around her nose.</summary>
    public const float OuterTopY = TopY - ShieldingDepth;

    /// <summary>…and the same on the other side.</summary>
    public const float OuterBottomY = BottomY + ShieldingDepth;

    /// <summary>Where the band runs forward to. Aft it runs to the transom.</summary>
    public const float ShieldingForwardEnd = BowX - 6;

    /// <summary>Half the flat of her nose, where the bow taper stops. Named rather than typed twice because
    /// the silhouette and the collision shell have to be cut from the same number: the shape a captain walks
    /// and the shape an instrument draws are one ship, or one of them is lying.</summary>
    public const float NoseHalfHeight = 2f;

    /// <summary>
    /// #241 · HER SILHOUETTE — the outer skin as one closed polyline, in deck units, bow to +X, tracing
    /// exactly what <see cref="Walls"/> lays: the transom, the shielding band, the forward end of the band,
    /// the bow taper, the flat of the nose, and back down the other side.
    ///
    /// <para>It exists so the SCOPE has a wreck to draw (#241 asks for "a wireframe-per-body-class seam …
    /// future wrecks and oddities each get a portrait without new plumbing") without a second set of numbers
    /// being typed into a renderer. Bug class 1 in this repo is a literal in a drawing that nothing derives
    /// or checks, found wrong three times out of three; the cure is that the picture and the deck read the
    /// same constants, so a hull that changes shape changes shape in both places or in neither.</para>
    /// </summary>
    public static IReadOnlyList<(float X, float Y)> HullOutline() =>
    [
        (TransomX, OuterTopY),
        (ShieldingForwardEnd, OuterTopY),
        (ShieldingForwardEnd, TopY),
        (BowX, -NoseHalfHeight),
        (BowX, NoseHalfHeight),
        (ShieldingForwardEnd, BottomY),
        (ShieldingForwardEnd, OuterBottomY),
        (TransomX, OuterBottomY),
        (TransomX, OuterTopY),
    ];

    /// <summary>
    /// #537 · AND SHE IS LONGER THAN HER ROOMS. Owner, on being told the side band was the fix: <i>"It would make
    /// sense that the walls that can hold vacuum are not thin and all kinds of tech needs to exist on the ship
    /// somewhere"</i>, and then the shortcut — <i>"That padding to every wall is a whole job in itself. Just make
    /// every ship longer. 😅"</i>
    ///
    /// <para>He is right on both counts, and they are the same point twice: a ship is not a row of rooms with a
    /// skin painted on. She is rooms, plus everything that makes the rooms work — plant, tankage, the drive, the
    /// runs between them — and until now her compartments went edge to edge and the drive lived nowhere at all.
    /// So the transom moves aft of the last bulkhead and the gap is MACHINERY SPACE: unassigned, unaudited, and
    /// exactly where a ship's tech actually is.</para>
    ///
    /// <para><b>The compartments do not move.</b> <see cref="AftX"/> stays the aft edge of ENGINEERING and REACTOR
    /// SPACES; only the shell goes further. Anything else would have re-cut eight rooms to buy one space.</para>
    /// </summary>
    public const float MachineryDepth = 8f;

    /// <summary>The transom — aft of the last bulkhead by a machinery space, not flush with it.</summary>
    public const float TransomX = AftX - MachineryDepth;

    /// <summary>
    /// #537 · AND HER INTERIOR BULKHEADS ARE NOT LINES EITHER. Owner, after the shielding band shipped, giving the
    /// reason he had wanted padding on the INSIDE walls all along: <i>"the reason I wanted padding on interior
    /// walls was to not make finding the hidden spaces too easy. Still a room with a wall to technical space is a
    /// good bet on large enough hiding space. 😎👍"</i>
    ///
    /// <para>He is right and it is the sharper half of the idea. A shielding band on the OUTSIDE ONLY is itself a
    /// tell: a captain learns in one boarding that hidden space is always outboard, never knocks anywhere else,
    /// and the deduction collapses into a reflex. Give every transverse bulkhead its own thin technical run and a
    /// void can be almost anywhere — so the clue has to be read rather than guessed at.</para>
    ///
    /// <para><b>And the heuristic survives, which is the good bit.</b> A bulkhead run is
    /// <see cref="BulkheadDepth"/> deep against the band's <see cref="ShieldingDepth"/>, so an outboard wall
    /// really is the better bet for anything BIG — a folded gun mount, a cold locker with somebody in it — while a
    /// bulkhead will take papers and a rack of keys and nothing else. Where a thing can be hidden is decided by
    /// what it is, which is exactly "a good bet" rather than a rule.</para>
    /// </summary>
    public const float BulkheadDepth = 1.2f;

    /// <summary>
    /// #537 · WHERE HER STRUCTURE IS, as filled rectangles — the shielding band and every bulkhead's technical
    /// run. Owner, looking at the deck after the padding shipped: <i>"we should cover those narrow spaces … all
    /// of them … if we can see into them from the hall then they don't hide anything."</i>
    ///
    /// <para>He is exactly right and it was a bad miss. The runs were drawn as two lines with the gap between
    /// them left BLACK — the same black as a room — so a captain could read every hiding place off the map
    /// without knocking on anything. The whole search collapses: the clue is redundant, the sounder is a
    /// formality, and the noise it costs buys nothing. A hidden space that is drawn as a space is not hidden.</para>
    ///
    /// <para>So the runs are FILLED, and they read as what they are: steel, tankage and pipework with a ship
    /// built round them. A void inside one looks exactly like every other stretch of it until somebody knocks —
    /// which is the entire mechanic, and it did not work until now.</para>
    /// </summary>
    public static IEnumerable<(float X0, float Y0, float X1, float Y1)> StructuralFills() =>
        StructuralFills(null);

    /// <summary>
    /// #537 slice 3 · …AND WHAT A CAPTAIN HAS ALREADY CUT INTO. The fill is the ship's own ignorance made
    /// visible: every run is drawn solid because a captain who has not knocked on it has no reason to think
    /// it is anything else. Once a plate has come out, the section behind it is space he has stood in, and
    /// the map draws it as space — the rest of the band stays hatched, because the rest of the band is still
    /// only a guess.
    ///
    /// <para>The band is split around the pocket rather than dropped: cutting one section of shielding does
    /// not tell a captain anything about the sixty frames either side of it, and a map that quietly opened
    /// the whole run would be the map knowing more than the man drawing it.</para>
    /// </summary>
    public static IEnumerable<(float X0, float Y0, float X1, float Y1)> StructuralFills(
        HullStowage.OpenVoid? opened)
    {
        // The shielding band, both sides, the length of the parallel middle body.
        foreach (bool top in new[] { true, false })
        {
            float y0 = top ? OuterTopY : BottomY;
            float y1 = top ? TopY : OuterBottomY;

            if (opened is not { } pocket || pocket.Top != top)
            {
                yield return (TransomX, y0, ShieldingForwardEnd, y1);
                continue;
            }

            // Aft of the pocket, then forward of it. Either stretch can be nothing at all when the void sits
            // hard against one end of the band, and a zero-width fill is a drawing bug rather than a cover.
            if (pocket.X0 > TransomX + 0.01)
            {
                yield return (TransomX, y0, (float)pocket.X0, y1);
            }
            if (pocket.X1 < ShieldingForwardEnd - 0.01)
            {
                yield return ((float)pocket.X1, y0, ShieldingForwardEnd, y1);
            }
        }

        // …and every interior bulkhead's own run.
        float half = BulkheadDepth / 2f;
        foreach (bool top in new[] { true, false })
        {
            float yIn = top ? -SpineHalfHeight : SpineHalfHeight;
            float yOut = top ? TopY : BottomY;
            foreach (float x in InteriorBulkheads(top))
            {
                yield return (x - half, System.Math.Min(yIn, yOut), x + half, System.Math.Max(yIn, yOut));
            }
        }
    }

    /// <summary>The transverse bulkhead positions that have a room on BOTH sides — the ones with a technical run
    /// inside them. The hull's own ends are not in here: they have the machinery space and the bow behind them.</summary>
    public static IEnumerable<float> InteriorBulkheads(bool top)
    {
        var seen = new HashSet<float>();
        var ends = new HashSet<float> { AftX, BowX - 6 };

        foreach ((string _, float x0, float x1, bool isTop) in Compartments)
        {
            if (isTop != top)
            {
                continue;
            }
            foreach (float x in new[] { x0, x1 })
            {
                if (!ends.Contains(x) && seen.Add(x))
                {
                    yield return x;
                }
            }
        }
    }

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

    /// <summary>The valve board itself — the mimic panel, aft with the machinery, because her bridge panel
    /// has no bus behind it. In Core so the audit walks to it and the separation test can see it: it was a
    /// literal in the client, which is exactly how the nest ended up two compartments from its own name.</summary>
    /// <remarks>Not (−24, −6), where it lived as a client literal: that is two units from the reactor
    /// cascade's own evidence AND standing in the ENGINEERING doorway. The separation test found both the
    /// moment the station was written down somewhere a test could see it — which is the argument for
    /// putting geometry in Core, made twice in one weekend.</remarks>
    public static DeckReachability.Point ValveStation => new(-19f, -6f);

    /// <summary>The damage-control placard, on the corridor wall just inboard of the shuttle lock — the
    /// first thing on the ship, at the only point every boarding passes through.
    ///
    /// <para>Owner, thinking past his own tenth boarding: <i>"did we tell somewhere where to find the manual
    /// airlock controls? Just thinking about first time player of that ship, could they go directly to the
    /// right space."</i> They could not. The one signpost was the dead bridge panel, which only speaks if
    /// you walk to the BOW and press it — so a captain who turned aft, or who never touched the bridge, was
    /// told nothing at all. The deck said ATMOSPHERE VALVES on a label that means nothing until you already
    /// know you want it.</para>
    ///
    /// <para>Every real ship answers this with a placard at the lock, which is also the safety card the
    /// owner filed a design for. So she gets one, where you come in.</para>
    ///
    /// <para>NOT (16.5, 2), where it was first bolted: 1.62 du from the FORWARD LOCKER's hatch control, so
    /// the first thing the captain meets on a derelict was two labels drawn on top of each other. Nothing
    /// caught it, because <see cref="StandardFittings"/> is audited against ITSELF and the hatch controls are
    /// generated per compartment — the same blind spot her own ship had. The deck audit walks the built plan
    /// now, consoles and all (<c>ConsoleCrowdingTests</c>).</para>
    ///
    /// <para>Nor (20.5, 2), which was my first correction and traded one law for another: it is half a unit
    /// off the shuttle-lock wall, and <c>WreckLayoutTests</c> walks to every station at half again the
    /// captain's width — <i>"only a thinner captain could reach the damage-control placard"</i>. A plate you
    /// have to squeeze past is not a plate anybody reads.</para>
    ///
    /// <para>(15, 1.7) clears every wall by 1.3 du and every other console by 3, and it is still the first
    /// plate of the boarding: the captain comes through the lock at x 21 and walks straight past it on the
    /// only road there is.</para></summary>
    public static DeckReachability.Point PlacardStation => new(15f, 1.7f);

    /// <summary>Her dead bridge panel — the master that has no bus behind it, and therefore a signpost
    /// rather than a control.
    ///
    /// <para>THE LAST LITERAL. It was <c>(19f, -7.5f)</c> in the client, which put it 3.4 du from the bridge
    /// log and, because it was not in <see cref="Stations"/>, outside everything CI walks: not reachability,
    /// not separation. Every wreck literal moved into Core this weekend turned out to be already wrong the
    /// moment a test could see it — the nest two compartments from its own name, the valves standing in the
    /// ENGINEERING doorway. Two for two is not a coincidence, it is the argument.</para></summary>
    /// <remarks>And 20.5 was my second wrong answer: the BRIDGE ends at <c>BowX − 6</c> = 20, so that put the
    /// panel in the hull taper where nobody can stand. The walkability audit said so immediately, which is
    /// the entire point of moving it somewhere the audit can see.</remarks>
    public static DeckReachability.Point BridgePanelStation => new(19f, -4.2f);

    /// <summary>
    /// WHAT EVERY SHIP HAS, WHATEVER KILLED HER. The fittings a hull is built with rather than the ones her
    /// ending gave her: the way home, the cargo, her two sets of paperwork, the scuttling panel, the placard
    /// at her lock and her atmosphere valves.
    ///
    /// <para>ABSENCE OF A TOOL IS INFORMATION, AND THAT IS THE WHOLE REASON THIS LIST EXISTS. Owner:
    /// <i>"even without reevers we should have those tech we used here available, to not give a clue that
    /// they might not be needed."</i> He is naming a leak the evidence system cannot survive. If a valve
    /// board only appears on infested hulls, then FINDING a valve board tells the captain what killed her
    /// before they have read one line of her log — and the careful business of <see cref="Derelict.WreckCause"/>
    /// and its <c>MisreadsAs</c> misdirection is undone by a console being present.</para>
    ///
    /// <para>So the fittings are the same on every ship in the fleet, and a wreck is distinguished only by
    /// her EVIDENCE. A pressurised hull with nothing living in her still has valves, and pulling them still
    /// works; it just does not help. That is exactly the shape a red herring should have — a real tool,
    /// available, that answers a question nobody is asking on this particular ship.</para>
    /// </summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> StandardFittings =>
    [
        ("the way back to the shuttle", ShuttleStation),
        ("the cargo (the decision)", CargoStation),
        ("the bridge log", LogStation),
        ("the cargo manifest", ManifestStation),
        ("the scuttling panel", ScuttleStation),
        ("the damage-control placard", PlacardStation),
        ("the atmosphere valves", ValveStation),
        ("the dead bridge panel", BridgePanelStation),
    ];

    /// <summary>Every place the captain must be able to REACH: her standard fittings plus the one station
    /// her ending put aboard. This is the list CI walks — and, since <c>WreckLayoutTests</c> also checks they
    /// do not stand on top of each other, the list that keeps two consoles from sharing a doorstep.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> Stations(Derelict.WreckCause cause) =>
        [.. StandardFittings, (CauseStationName(cause), CauseStation(cause))];

    // ── The archive node, when a hull is carrying one ────────────────────────────────────────────────
    //
    // NOT a standard fitting: it is CARGO nobody invoiced, and it is aboard about one eligible hull in three
    // (ArchiveNode.IsAboard). It is written down HERE rather than in the renderer for the reason every other
    // literal on this ship moved into Core — placed by eye, the scuttling panel landed on the nest and the
    // valve board stood in a doorway, and no test could see either.

    /// <summary>The column itself, in the deep hold — <see cref="ArchiveNode.HoldCompartment"/>, because that
    /// is where freight nobody invoiced ends up, and because it puts the field between the away team and the
    /// far end of the ship.
    ///
    /// <para>#633 · IT WAS AGAINST THE AFT BULKHEAD AT <c>(-13.8, -7.8)</c> AND HAD TO COME FORWARD. Two of
    /// #537's laws, built on `main` while this node was being built here, closed that corner between them.
    /// The interior bulkhead runs turn the DEEP HOLD's aft wall at <c>x = -15</c> from a line into a
    /// <see cref="BulkheadDepth"/>-deep closed box spanning <c>-15.6 … -14.4</c>, which left the column
    /// 0.6 du of clearance where the walk audit wants 1.05 — unreachable on all ten causes. And the room's
    /// aft end is already spoken for on one of them: the nest sits at <c>(-11, -6)</c>, so everything the
    /// bulkhead run allows is inside the 3 du no-two-stations-share-a-doorstep rule.
    ///
    /// <para>So it moves to the hold's FORWARD end, which turns out to be the better staging anyway: the away
    /// team comes aft down the spine, turns in at the hold's door, and the column is the first thing in the
    /// room rather than the last. The structural law does not move — it governs every bulkhead on every hull,
    /// and this governs one crate.</para></para></summary>
    public static DeckReachability.Point ArchiveStation => new(-3.5f, -7.7f);

    /// <summary>The handle plate at the inboard end of the same housing — <see cref="ArchiveNode.SwitchLegend"/>
    /// stencilled on it.
    ///
    /// <para>It is a SEPARATE doorstep on purpose, and the separation is the mechanic rather than tidiness:
    /// the design's law is that a captain <i>may pull the handle without paying, and never find out what they
    /// did</i>. Put the handle inside the confrontation's card and pulling it would first cost a throw, which
    /// is the one thing the whole Ren &amp; Stimpy joke cannot survive. So the column and the handle are 3.5 du
    /// apart — far enough that <c>NearestConsoleSpot</c> can tell them apart, close enough to be one object.</para>
    ///
    /// <para>#633 · Moved forward with the column, keeping the 3.5 du between them EXACTLY, because that
    /// distance is the mechanic and not a layout preference.</para></summary>
    public static DeckReachability.Point ArchiveSwitchStation => new(-3.5f, -4.2f);

    /// <summary>The reachability/separation list for a hull that IS carrying a node. Kept apart from
    /// <see cref="Stations"/> so the "every ship has identical fittings" law stays literally true — but
    /// audited on every cause anyway, because geometry that is only checked where it is currently used is
    /// geometry that breaks the day somebody widens the eligibility rule.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> StationsWithArchive(
        Derelict.WreckCause cause) =>
        [.. Stations(cause),
         ("the archive node", ArchiveStation),
         ("the purge handle", ArchiveSwitchStation)];

    // ── The black-ops key, when a hull that fought is carrying one ───────────────────────────────────
    //
    // NOT a standard fitting, for the archive node's reason one section up: it is not something a ship is
    // built with, it is something that was ABOARD her. Written down here rather than in the renderer because
    // every wreck literal placed by eye in the client turned out to be wrong the moment a test could see it
    // — the scuttling panel on the nest, the valve board standing in a doorway, three for three.

    /// <summary>
    /// #535 · WHERE THE KEY IS LYING: in the CREW SPACES, in somebody's kit, at the outboard end of the room.
    ///
    /// <para>The placement is the cheap half of the object's canon. A code nobody was ever meant to read is
    /// not in the safe with the manifest and it is not invoiced into the hold — it is in the personal effects
    /// of whoever was carrying it when she stopped being a ship, which is exactly the drawer this game
    /// already puts loose rounds and somebody's wallet in (#563's outpost effects).</para>
    ///
    /// <para>Not the DEEP HOLD, which is the tempting room: that is freight nobody invoiced — a different
    /// sentence about a different kind of secret — and it is already spoken for by the column
    /// (<see cref="ArchiveStation"/>) on the very cause this key is dealt on.</para>
    ///
    /// <para>(11, −7.5) clears the CREW SPACES bulkhead at x 13 and the hull at y −9 by more than the walk
    /// audit's margin, and stands clear of every other station on every cause by more than the separation
    /// rule's three units — the nearest is the life-support/mutiny evidence at (7, −6), four and a quarter
    /// away.</para></summary>
    public static DeckReachability.Point KeyStation => new(11f, -7.5f);

    /// <summary>The reachability/separation list for a hull that IS carrying a key. Kept apart from
    /// <see cref="Stations"/> so the "every ship has identical fittings" law stays literally true — and
    /// audited on every cause anyway, for <see cref="StationsWithArchive"/>'s reason: geometry checked only
    /// where it is currently used is geometry that breaks the day somebody widens the eligibility rule.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> StationsWithKey(
        Derelict.WreckCause cause) =>
        [.. Stations(cause), ("the black-ops key", KeyStation)];

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
        // THE NEST IN THE DEEP HOLD, and now actually in the deep hold. It sat at (-24, 6) — the reactor
        // spaces — while every line of text about it, its own station name included, said deep hold. Owner,
        // finding them coming out of half the ship: "I thought there was only one nest?" There is exactly
        // one, and this is where it is.
        Derelict.WreckCause.Infested => new(-11f, -6f),
        Derelict.WreckCause.InsuranceJob => new(7f, 6f),
        // AMIDSHIPS IN THE SPINE, AND THE ONLY CAUSE STATION THAT IS NOT IN A ROOM — because her evidence is
        // not in a room. "Every door was thrown from the SPINE side": you stand in the corridor, look fore
        // and aft, and every hatch on the ship has its dogs on YOUR side of it. The tenth cause was the only
        // one with no arm in this switch, so it fell through to the fallback below — which happens to be this
        // same point, reached by accident and named "the wreck". Declared now, so the station is a decision.
        Derelict.WreckCause.VentedByOneOfTheirOwn => new(0f, 0f),
        _ => new(0f, 0f),
    };

    /// <summary>Which compartment a point stands in, or null out in the spine. The one place that answer is
    /// computed, so the map, the board and the rules can never disagree about what room something is in.</summary>
    public static string? CompartmentAt(double x, double y)
    {
        foreach ((string name, float x0, float x1, bool top) in Compartments)
        {
            if (x >= x0 && x <= x1 && (top ? y < -SpineHalfHeight : y > SpineHalfHeight))
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>THE room the infestation comes out of — there is one, and it is wherever her station is.
    /// Derived rather than declared, so moving the station moves the nest and nothing drifts apart.</summary>
    public static string NestCompartment =>
        CompartmentAt(CauseStation(Derelict.WreckCause.Infested).X,
                      CauseStation(Derelict.WreckCause.Infested).Y)
        ?? "DEEP HOLD";

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
        Derelict.WreckCause.VentedByOneOfTheirOwn => "the hatch dogs — spine side",
        _ => "the wreck",
    };
}
