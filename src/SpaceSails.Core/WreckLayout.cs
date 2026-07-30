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

        // Outer shell. The bow tapers; the aft is a flat transom where the drive used to be — and it now sits
        // a MACHINERY SPACE aft of the last bulkhead rather than flush against it, because a ship is her rooms
        // plus everything that makes the rooms work.
        walls.Add(new(TransomX, TopY, BowX - 6, TopY));
        walls.Add(new(TransomX, BottomY, BowX - 6, BottomY));
        walls.Add(new(BowX - 6, TopY, BowX, -2f));
        walls.Add(new(BowX - 6, BottomY, BowX, 2f));
        walls.Add(new(BowX, -2f, BowX, 2f));
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
        _ => "the wreck",
    };
}
