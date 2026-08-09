using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #804 · SOMEBODY IS WALKING THESE FLOORS, AND THE WALK IS A THING YOU CAN LEARN.
///
/// <para>Owner, 2026-08-09, filing the whole feature: <i>"the rotating guards on the lower more restricted
/// levels… Any how the guards using the A* to have a beat are missing."</i> And then the three sentences
/// that decide the shape of it:</para>
///
/// <list type="number">
/// <item><b>The verb is TIMING, not hiding.</b> <i>"ideally we could see them move and wait for them to pass
/// before we pass them."</i> A beat is only worth having if a captain can stand at the mouth of a rib,
/// watch a round go by, and step out behind it. That means the route is fixed for a watch, walkable, and
/// slower than the captain.</item>
/// <item><b>The knowing is asymmetric, in both directions.</b> <i>"Surely we should not know their movements
/// like 100 meters out and them need to see us like really close to register our existence. We need our
/// motion detector to warn us or that we hear a noise they make before they spot us."</i></item>
/// <item><b>Suspicion before pursuit.</b> <i>"a rolling guard has no reason to run after anyone just on
/// sight, they must suspect you do not belong there for some reason first."</i> So a sighting starts a
/// CHALLENGE, and nothing in this file can start a chase.</item>
/// </list>
///
/// <h3>What this is NOT</h3>
///
/// <para>It is not a detection meter and it is not a stealth level — #618's own canon constraint, written
/// before anybody had built this: <i>"The owner's ask is a cover that can blow, not a detection meter."</i>
/// There is no alert state, no alarm, no lockdown and no hunt. A guard who is satisfied walks on; a guard
/// who is not walks you back to the lift. The worst thing that happens on this floor is that somebody
/// writes your visit down.</para>
///
/// <h3>Where they are, and where they are deliberately not</h3>
///
/// <para><b>Below the bar, and no deeper than the building admits to.</b> #709 put people on B1 only and
/// said why: the population falling to zero is what makes the descent a gradient. This does not spend that.
/// It fills the gap between the floor where an outsider plausibly belongs (<see cref="CanteenRegulars"/>'s
/// own B1 ruling) and the last floor the directory owns up to (<c>DepthOf</c>) — the facility's WORKING
/// floors, which are exactly the floors that have a payroll to put somebody on.</para>
///
/// <para>Nobody patrols the unlisted band or the found halls. That is not an omission: the unlisted band is
/// the thing the clandestine operation was hiding <i>from its own staff</i> (§13.7), and a guard walking a
/// round down there would be the building telling on itself. Past the seam nothing belongs to anybody at
/// all. <b>The empty floors stay empty, and where the rounds stop is a fact a captain can read.</b></para>
///
/// <h3>The register</h3>
///
/// <para>A guard here is an EMPLOYEE. Bored, thorough, procedural, on a rota, halfway through a shift, and
/// wholly uninterested in you until the form says otherwise. Nothing one says, carries or does explains
/// anything (§13.8), and the closest any line comes to menace is a man being unhurried about paperwork.</para>
///
/// <para>Pure and deterministic, like everything else in Core: the beat is a function of (site, floor,
/// watch) and the floor plan, so the same round runs the same way every visit inside a shift and turns over
/// with the shift — which is how a captain learns one and how the building stays alive.</para>
/// </summary>
public static class PatrolBeat
{
    // ── WHICH FLOORS HAVE A ROUND ON THEM ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Does anybody walk a round on this floor?
    ///
    /// <para>Three clauses, and every one of them is somebody else's already-shipped ruling rather than a
    /// number typed here:</para>
    ///
    /// <list type="bullet">
    /// <item><b>Below the bar.</b> <see cref="CanteenRegulars.PeopleSitHere"/> is the floor an outsider
    /// plausibly belongs on; a round on it would ask a room of contractors for their passes, and that room's
    /// own plate says <c>NO PASS REQUIRED</c>.</item>
    /// <item><b>Not deeper than the directory admits.</b> <c>DepthOf</c> is the building's account of
    /// itself, and a payroll stops where the account does.</item>
    /// <item><b>Never the head office.</b> #411's building has no gate, no shafts to card and a different
    /// fiction entirely; putting a contract guard in the wintering hall would be this file deciding
    /// something about a place it does not own.</item>
    /// </list>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor (negative; −1 is B1).</param>
    public static bool IsPatrolled(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (level >= 0 || UndergroundComplex.IsHeadOffice(bodyId))
        {
            return false;
        }

        if (UndergroundComplex.TopPressurisedFloor(bodyId) is not { } bar || level >= bar)
        {
            return false;
        }

        // Listed floors only. IsUnlisted/IsFound are the two bands the building denies having, and denying
        // a band is incompatible with rostering somebody to walk it.
        return level >= UndergroundComplex.DepthOf(bodyId);
    }

    // ── HOW MANY, AND WHO ─────────────────────────────────────────────────────────────────────────────

    /// <summary>The most who ever walk one floor at once. Two is a rota with a gap in it that a captain can
    /// time; three is a cordon, and a cordon is the stealth level #618 ruled against. FLAGGED for the
    /// owner's tuning.</summary>
    public const int MostOnAFloor = 2;

    /// <summary>How many are on the round on this floor this watch — one or two, seeded off (site, floor,
    /// watch) and nothing else. A floor with one is a floor you can walk behind; a floor with two is a floor
    /// where the gap is somewhere else. Nothing announces which you have walked into.</summary>
    public static int GuardsOn(string bodyId, int level, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsPatrolled(bodyId, level)
            ? DiceRule.Roll(DiceRule.Seed($"hive:patrol:heads:{bodyId}:{level}", watch), MostOnAFloor).Face
            : 0;
    }

    /// <summary>What is drawn over a guard on the deck. The ROUND, not the person: a rota numbers rounds,
    /// and nobody down here is going to introduce themselves. Kept short because it is drawn in 8px
    /// monospace over a moving dot.</summary>
    public static string DeckName(int index) => $"PATROL {index + 1}";

    /// <summary>Is this deck name one of ours? The renderer's ink gate, and a single answer so a fourth kind
    /// of figure on the deck cannot be told apart by a string literal typed twice.</summary>
    public static bool IsGuardName(string? name) =>
        name is not null && name.StartsWith("PATROL ", StringComparison.Ordinal);

    /// <summary>
    /// Who is walking it — the plate that stands over them when the round stops at you.
    ///
    /// <para>The register test (#701, #709) applies at full strength: not one of them is interesting.
    /// A guard who read as a threat would turn the working floors into a stealth level, and a guard who read
    /// as a character would make the round a quest.</para>
    /// </summary>
    private static readonly string[] Plates =
    [
        "◈ A CONTRACT GUARD, WALKING THE ROUND",
        "◈ A SITE WARDEN, HALFWAY THROUGH A SHIFT",
        "◈ SOMEBODY ON THE SECURITY ROTA, NOT LOOKING FOR YOU",
        "◈ A NIGHT MAN DOING THE DAY MAN'S HOURS",
    ];

    /// <summary>How many plates are authored. Public so a guard can pin the catalog's size without reaching
    /// into it.</summary>
    public static int PlateCount => Plates.Length;

    /// <summary>Which of them this round is. Seeded on (site, floor, watch, index), so the face on the round
    /// turns over with the shift exactly as the canteen's does.</summary>
    public static string PlateOf(string bodyId, int level, long watch, int index)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return Plates[DiceRule.Roll(
            DiceRule.Seed($"hive:patrol:who:{bodyId}:{level}:{index}", watch), Plates.Length).Face - 1];
    }

    // ── THE BEAT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One stop on a round: where it is, and what the guard is standing at when they are there.
    /// The name is diagnostic — it is what a red audit prints — and never shown to a player.</summary>
    /// <param name="X">Deck units, in the surface's own coordinates.</param>
    /// <param name="Y">Deck units.</param>
    /// <param name="What">The thing at this stop: the car, a corridor mouth, a room.</param>
    public readonly record struct Stop(double X, double Y, string What);

    /// <summary>
    /// THE CIRCUIT, TAKEN OFF THE FLOOR PLAN AND NEVER OFF A CONSTANT.
    ///
    /// <para>The lift, then every rib mouth in ASCENDING X, and after each mouth the room on that rib that
    /// stands furthest from the spine. Three things about that are load-bearing:</para>
    ///
    /// <list type="bullet">
    /// <item><b>Every stop is published geometry.</b> <see cref="UndergroundComplex.ShaftAt"/>,
    /// <c>FloorPlan.Ribs</c> (published by #587 precisely so nothing outside the generator has to do
    /// arithmetic that copies the placement) and <c>FloorPlan.RoomCentres</c>. This file computes no
    /// position of its own, which is §13.15 — a seam that re-derived a fact about a building it does not own
    /// would be the mirrored-constant bug this ground keeps paying for.</item>
    /// <item><b>Sorted before it is walked.</b> #587's own lesson, one floor along: a list built by
    /// appending is not a list in order, and a round that jumped back and forth along the spine would be
    /// unlearnable — which is the whole feature.</item>
    /// <item><b>Every stop is a place the A* audit already walks.</b> Room centres are what
    /// <c>HiveInterior</c> hangs its SEARCH THE ROOM consoles on, and §13.1's sweep proves every one of them
    /// is reachable from the car on every floor of every site. A beat built out of them is walkable by
    /// construction rather than by hope — and there is a guard that walks it anyway.</item>
    /// </list>
    /// </summary>
    /// <param name="floor">The floor, as Core built it.</param>
    /// <param name="field">The site's envelope — the shaft's position comes from it.</param>
    public static IReadOnlyList<Stop> Circuit(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        var stops = new List<Stop>();

        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(field);

        // The car. It is the first stop on every round because it is the first thing anybody checks and the
        // one place on the floor a captain has to come back to.
        stops.Add(new Stop(shaftX, shaftY + 1.0, "the car"));

        // The ribs, in x order — SORTED, not as the plan happens to list them.
        var ribs = new List<UndergroundComplex.Rib>(floor.Ribs ?? []);
        ribs.Sort((a, b) => a.X.CompareTo(b.X));

        var taken = new HashSet<int>();
        foreach (UndergroundComplex.Rib rib in ribs)
        {
            // The mouth: where the cross corridor opens off the spine. This is the square a captain watches
            // from, so it has to be on the round.
            stops.Add(new Stop(rib.X, shaftY, $"the mouth at x{rib.X:F0}"));

            // …and the far room down it. Which room "belongs" to this rib is decided by nearness in x and
            // by which side of the spine the rib runs — both facts the plan publishes.
            (int Index, double X, double Y)? room = FurthestRoomOn(floor, rib, shaftY, taken);
            if (room is { } far)
            {
                taken.Add(far.Index);
                stops.Add(new Stop(far.X, far.Y, $"the far room off x{rib.X:F0}"));
            }
        }

        return stops;
    }

    /// <summary>The room furthest down one rib: nearest to the rib in x, on the rib's own side of the spine,
    /// and the deepest of those. The ordinal comes back with it so the caller can CLAIM it — two ribs
    /// sharing a stop would be a round that doubles back on a room it has just left.</summary>
    private static (int Index, double X, double Y)? FurthestRoomOn(
        in UndergroundComplex.FloorPlan floor, UndergroundComplex.Rib rib, double spineY, HashSet<int> taken)
    {
        (int Index, double X, double Y)? best = null;
        double bestDepth = -1;

        IReadOnlyList<(double X, double Y)> rooms = floor.RoomCentres ?? [];
        for (int i = 0; i < rooms.Count; i++)
        {
            if (taken.Contains(i))
            {
                continue;
            }

            (double rx, double ry) = rooms[i];

            // The rib's own side of the spine. Down means toward the deep field, away from the landing band,
            // which is the flag the plan carries for exactly this question.
            bool below = ry < spineY;
            if (below != rib.Down)
            {
                continue;
            }

            // Rooms hang off the rib they were placed along, so a room more than a room's width away in x is
            // somebody else's. RoomWidthDu is the generator's own number, read rather than guessed.
            if (Math.Abs(rx - rib.X) > UndergroundComplex.RoomWidthDu)
            {
                continue;
            }

            double depth = Math.Abs(ry - spineY);
            if (depth > bestDepth)
            {
                bestDepth = depth;
                best = (i, rx, ry);
            }
        }

        return best;
    }

    /// <summary>
    /// THE ROUND AS IT IS ACTUALLY WALKED THIS WATCH — the circuit, turned over by the shift.
    ///
    /// <para>Two things rotate, and both are seeded on (site, floor, watch): which DIRECTION the round runs,
    /// and which stop it starts from. That is the whole of "rotating guards": the same floor, the same
    /// stops, a different round — so a captain who learned one shift's round has learned the floor and not
    /// the answer, and has to watch again.</para>
    ///
    /// <para>Every guard on the floor shares this ONE list and differs only by which leg they are on
    /// (<see cref="StartLeg"/>) — the sweep team's own idiom, and for its reason: <i>being hidden from has
    /// to be legible.</i> Two rounds with two different routes would be two things to learn at once.</para>
    /// </summary>
    public static IReadOnlyList<Stop> BeatFor(
        string bodyId, int level, long watch,
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        IReadOnlyList<Stop> circuit = Circuit(floor, field);
        if (circuit.Count == 0)
        {
            return circuit;
        }

        var walked = new List<Stop>(circuit);
        if (DiceRule.Roll(DiceRule.Seed($"hive:patrol:dir:{bodyId}:{level}", watch), 2).Face == 1)
        {
            walked.Reverse();
        }

        int offset = DiceRule.Roll(
            DiceRule.Seed($"hive:patrol:from:{bodyId}:{level}", watch), walked.Count).Face - 1;
        if (offset == 0)
        {
            return walked;
        }

        var rotated = new List<Stop>(walked.Count);
        for (int i = 0; i < walked.Count; i++)
        {
            rotated.Add(walked[(offset + i) % walked.Count]);
        }
        return rotated;
    }

    /// <summary>
    /// #804 · HOW BIG A LATTICE ONE LEG IS WALKED OVER, and why it is not the whole floor.
    ///
    /// <para><see cref="AutoWalk.BoundsFor"/> spans every wall it is handed, which is right for a captain
    /// clicking an arbitrary spot and wrong for a guard walking forty du down a rib: a floor-sized lattice
    /// at <see cref="DeckReachability.DefaultStep"/> is a third of a million cells, and this repo runs in
    /// WASM where a Debug build is a hundred times slower than the machine this is written on. A round that
    /// planned one of those every time it reached a stop would hitch the frame it arrived on.</para>
    ///
    /// <para><b>The margin is the whole of the safety.</b> A leg may bulge around a room to reach the next
    /// stop, so the box is the two stops plus a room's own depth on every side — derived from
    /// <c>RoomHeightDu</c> and the corridor's width rather than typed, so a generator that grows its rooms
    /// grows this with them. Clamped to the field, because nothing is built outside it.</para>
    ///
    /// <para><b>And it lives here so the audit and the game ask ONE question.</b> If the client shrank the
    /// box and the sweep walked the whole floor, the sweep would be proving a route the game never plans —
    /// two pathfinders agreeing only by luck, which is the drift <c>DeckReachability</c>'s own lattice was
    /// consolidated to stop.</para>
    /// </summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) LatticeFor(
        in Stop from, in Stop to, in SurfaceLayout.Field field)
    {
        double margin = UndergroundComplex.RoomHeightDu + (UndergroundComplex.CorridorHalf * 2);
        double minX = Math.Max(field.LeftX, Math.Min(from.X, to.X) - margin);
        double maxX = Math.Min(field.RightX, Math.Max(from.X, to.X) + margin);
        double minY = Math.Max(field.BottomY, Math.Min(from.Y, to.Y) - margin);
        double maxY = Math.Min(field.LandingBandY, Math.Max(from.Y, to.Y) + margin);
        return (minX, minY, maxX, maxY);
    }

    /// <summary>Where the <paramref name="index"/>th guard starts on a beat of <paramref name="legs"/> stops
    /// — spread evenly around it, so two of them cover the floor instead of walking in a queue. The sweep
    /// team's arithmetic, because it is the same problem.</summary>
    public static int StartLeg(int legs, int index, int count) =>
        legs <= 0 ? 0 : index * legs / Math.Max(1, count) % legs;

    /// <summary>
    /// How fast a round walks, deck units per second. SLOWER than the captain's walk on purpose: a captain
    /// can always out-walk a guard and it never helps, because the thing they are bad at is not speed.
    /// FLAGGED for the owner's tuning — this and <see cref="StandSeconds"/> are the whole timing window.
    /// </summary>
    public const double WalkSpeed = 3.2;

    /// <summary>How long they stand at a stop before walking on. THE NUMBER THE FEATURE IS ABOUT: it is the
    /// gap a captain watches for and steps into. Long enough to see from the far end of a corridor, short
    /// enough that waiting is a decision rather than a wait. FLAGGED.</summary>
    public const double StandSeconds = 5.0;

    /// <summary>Close enough to a stop to be standing at it. The A* leg lands the guard adjacent to the
    /// stop rather than on it (a room centre has a console on it), so this is the same "arrived" tolerance
    /// the sweep team's route uses.</summary>
    public const double AtTheStopDu = 1.5;

    // ── WHAT IS KNOWN, AND BY WHOM ────────────────────────────────────────────────────────────────────
    //
    // Owner: "we should not know their movements like 100 meters out and them need to see us like really
    // close to register our existence."
    //
    // Four bands, and a captain meets them in this order as a round comes down a corridor:
    //
    //   1. THE FAN hears it, through the rock, as a smudge with a bearing on it. (#591's tracker, untouched:
    //      a guard walks, so a guard is a contact. Nothing here special-cases the instrument.)
    //   2. THE EAR hears it — boots, out of sight, unhurried. (EarshotDu, and deliberately NOT wall-aware:
    //      sound goes round corners, which is why the noise line is the warning and the marker is not.)
    //   3. THE EYE sees it — and only then is a marker drawn on the deck. (MarkerSightDu + line of sight.)
    //   4. THEY see YOU. (NoticeDu + line of sight — a third of the eye's reach, because they are doing a
    //      job and you are watching a corridor.)
    //
    // The asymmetry between 3 and 4 IS the feature. If they saw you the moment you saw them there would be
    // nothing to time, and "wait for them to pass" would not be a sentence anybody could act on.

    /// <summary>How far the captain's own eye reaches for a guard's marker to be drawn at all. A corridor's
    /// length: far enough to watch a round work, nowhere near the hundred metres the owner ruled out.</summary>
    public const double MarkerSightDu = 30.0;

    /// <summary>How close a guard has to be before they register that somebody is standing there. A third of
    /// the eye's reach, and the whole of the timing window. FLAGGED for the owner's tuning.</summary>
    public const double NoticeDu = 9.0;

    /// <summary>How far the boots carry. Between <see cref="NoticeDu"/> and <see cref="MarkerSightDu"/> on
    /// purpose: a captain who cannot see a corridor can still hear that it is not empty.</summary>
    public const double EarshotDu = 18.0;

    /// <summary>How long a captain stepping out of the car has before anybody looks up. Not the moon's
    /// twenty-second grace (<see cref="SurfaceArrival.SpotGraceSeconds"/> is about a hull setting down and
    /// a deep full of Old Ones) — this is a lift door opening on a working floor, and four seconds is the
    /// beat it takes to read the plate and decide.</summary>
    public const double OffTheCarSeconds = 4.0;

    /// <summary>May anybody notice the captain yet? False for the first moments on a floor, whatever is in
    /// front of them.</summary>
    public static bool CanBeNoticed(double secondsOnThisFloor) =>
        !double.IsNaN(secondsOnThisFloor) && secondsOnThisFloor >= OffTheCarSeconds;

    /// <summary>
    /// THE ONE LOOK, read in both directions.
    ///
    /// <para>Clear line of sight over the walls, and inside <paramref name="reachDu"/>. It is one function
    /// with a range parameter rather than two functions, because the drawn marker and the guard's notice are
    /// the same question asked twice and a second copy of it is how they end up disagreeing — which on this
    /// ground would mean a guard challenging a captain who cannot see them, or standing visible three metres
    /// away and never looking up.</para>
    ///
    /// <para>The wall law is <see cref="SurfaceCollision.HasLineOfSight"/>, the same one the captain, the
    /// pack and the sweep team all obey.</para>
    /// </summary>
    public static bool EyesOn(
        double fromX, double fromY, double toX, double toY, double reachDu,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        double dx = toX - fromX, dy = toY - fromY;
        if ((dx * dx) + (dy * dy) > reachDu * reachDu)
        {
            return false;
        }
        return SurfaceCollision.HasLineOfSight(fromX, fromY, toX, toY, walls);
    }

    /// <summary>Is this guard DRAWN on the captain's deck? The captain's eye, at the eye's reach.</summary>
    public static bool DrawnFor(
        double captainX, double captainY, double guardX, double guardY,
        IReadOnlyList<SurfaceCollision.Segment>? walls) =>
        EyesOn(captainX, captainY, guardX, guardY, MarkerSightDu, walls);

    /// <summary>Has this guard registered that somebody is standing there? Their eye, at their own much
    /// shorter reach — the same predicate, the other way round.</summary>
    public static bool Notices(
        double guardX, double guardY, double captainX, double captainY,
        IReadOnlyList<SurfaceCollision.Segment>? walls) =>
        EyesOn(guardX, guardY, captainX, captainY, NoticeDu, walls);

    /// <summary>Can the captain HEAR this one — close, and not visible? Range only, because sound goes round
    /// corners; and explicitly not when they are already drawn, because a line describing boots you can see
    /// is the picture and the sentence disagreeing.</summary>
    public static bool Heard(
        double captainX, double captainY, double guardX, double guardY,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        double dx = guardX - captainX, dy = guardY - captainY;
        if ((dx * dx) + (dy * dy) > EarshotDu * EarshotDu)
        {
            return false;
        }
        return !DrawnFor(captainX, captainY, guardX, guardY, walls);
    }

    /// <summary>What a corridor you cannot see into sounds like when a round is in it. Said sparingly — it
    /// is a warning, not a narrator — and it names no direction, because the bearing is the fan's job and
    /// two instruments answering one question is how they come to disagree.</summary>
    public const string HeardLine =
        "👣 Boots on shotcrete, out of sight and in no hurry — the tread of somebody walking a line they " +
        "have walked all week.";

    // ── THE BADGE ─────────────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "our own badge once we get a gig". #618 said why it is the load-bearing object: "A disguise is
    // worthless without somebody to fool."
    //
    // It is SITE-SCOPED, exactly like an authority card, and for the card's reason (#679): a pass that
    // worked everywhere would be a skeleton key, and a pass that is read out loud as somebody else's site
    // is the best sentence a refusal can say. One tier in this phase — GENERAL HANDS, off the board's own
    // HIRING notice — because the department ladder is #605's question and not this one's.

    /// <summary>The id a site's pass rides under in the wallet. Same shape as an authority card's: a fact
    /// the vault can store, with the words rebuilt at read time.</summary>
    public static string BadgeId(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return $"badge:{bodyId}";
    }

    /// <summary>Which site a pass is for, or null when nothing can read it.</summary>
    public static string? SiteOfBadge(string? id) =>
        id is { Length: > 6 } && id.StartsWith("badge:", StringComparison.Ordinal) ? id[6..] : null;

    /// <summary>The pass as a thing in the wallet.</summary>
    public static Satchel.Item Badge(string bodyId) => new(Satchel.Kind.Badge, BadgeId(bodyId));

    /// <summary>The glyph the satchel row wears. A card with a face on it, which is the whole difference
    /// between this and every other piece of paper down here.</summary>
    public const string BadgeGlyph = "🪪";

    /// <summary>The one tier this phase issues. It is the board's own <c>HIRING — GENERAL HANDS</c> notice
    /// arriving as an object, which is the cheapest possible way for a pass to mean something.</summary>
    public const string BadgeTier = "GENERAL HANDS";

    /// <summary>What is printed on it. Seeded off nothing — a pass says the site and the tier, and a pass
    /// that said more would be a department, which is a question nobody has ruled on.</summary>
    public static string BadgeTitle(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return $"SITE PASS · {BadgeTier} · {BodyNames.Designation(bodyId)} SITE";
    }

    /// <summary>Is the captain carrying this site's own pass? The possession IS the state — no flag, no
    /// parallel ledger, the discipline <see cref="CanteenTable.Cover"/> already keeps.</summary>
    public static bool BadgeHeld(string bodyId, IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return Satchel.CountOf(carried, Satchel.Kind.Badge, BadgeId(bodyId)) > 0;
    }

    /// <summary>#804 · WHERE IT COMES FROM: the shift you actually turned up for. The Hand's chit says
    /// <i>take this to the lift</i>; the cage's gate reads it and takes you down (#752); and at the bottom
    /// the site does the thing a site does with a body that has arrived on somebody's account — it puts you
    /// on its books. The gig is not the paper; the gig is having gone down.</summary>
    public const string BadgeIssuedLine =
        "🪪 At the bottom of the cage somebody photographs you against a wall, prints a pass while you wait " +
        "and hands it over without looking up. GENERAL HANDS. No department, no expiry, and your name " +
        "spelled the way the rota spells it.";

    /// <summary>What the field book keeps of that. Not "you got a badge" — what the badge turns out to be.</summary>
    public const string BadgeGist =
        "You are on this site's books. There is a pass in your wallet with your face on it and somebody " +
        "else's idea of your name.";

    // ── THE CHALLENGE ─────────────────────────────────────────────────────────────────────────────────
    //
    // #684's ruling, one building along: the panel's unprompted wallet-read IS its character, and the answer
    // is TOLD on a card rather than muttered into a pulse. A man doing a round is the same gesture with a
    // face on it — he does not ask you to press anything, he puts his hand out and reads what is in it.
    //
    // So this is a CARD with two arms and no buttons, and the read is automatic. #746 built the encounter
    // machine for the day a guard stop needs MOVES ("show ID, explain yourself"); this stop has exactly one
    // move and it is one the captain has already made by carrying the thing or not carrying it. Growing it
    // into an Encounter.Scene is the next phase's work and needs no new mechanics — which is that file's
    // whole claim, and this phase leaves it true rather than pre-empting it.

    /// <summary>What the story card is called. It names the moment rather than the man: a round has a
    /// direction and a rhythm, and the frightening thing is that it has stopped.</summary>
    public const string ChallengeLabel = "👮 THE ROUND STOPS AT YOU";

    /// <summary>
    /// What is happening, before anything is read. Evidence, and then it stops (§13.9's discipline) — the
    /// card describes a man doing a task and says nothing at all about what happens next.
    /// </summary>
    public static string ChallengeCard(string plate) =>
        $"{plate} — and they see you before you hear them stop. No shout and no lamp in the face: this " +
        "floor has lights, and you are plainly not what the lights are for.\n\n" +
        "What you get is a hand out, palm up, for the thing everybody who works this corridor carries. " +
        "Nothing about it is urgent. That is the part worth being frightened of — he has done this a " +
        "hundred times, it has come to nothing a hundred times, and he will write down whichever way it " +
        "goes tonight either way.";

    /// <summary>#804 · The guard's read of the wallet, and the card it is told on. Deliberately the same
    /// shape as <see cref="UndergroundComplex.GateRead"/>: two arms, one sentence, one label — so the client
    /// raises the identical card for a pass that works and a pass that does not, and neither arm can grow a
    /// presentation of its own.</summary>
    /// <param name="Satisfied">Whether he walks on. False is a refusal, and <paramref name="Line"/> names
    /// its reason either way (#603's law).</param>
    /// <param name="Line">The read itself, verbatim. Nothing downstream rewrites it.</param>
    /// <param name="Label">The card's title.</param>
    /// <param name="Card">The card's body — who stopped you and what they are doing.</param>
    /// <param name="Consequence">What happens next, or null when nothing does. #736's law is why this is
    /// carried rather than pulsed: the sentence a captain ACTS ON has to live on the card that is up, and a
    /// card is exactly what is up at this moment. Composed with <see cref="Line"/> by <see cref="Told"/>, so
    /// no caller can put the read on the card and the consequence behind the backdrop.</param>
    public readonly record struct Read(
        bool Satisfied, string Line, string Label, string Card, string? Consequence = null)
    {
        /// <summary>The whole of what the card says under the picture: the read, and — when there is one —
        /// what it cost. One string, one region, one source.</summary>
        public string Told => Consequence is { Length: > 0 } cost ? $"{Line}\n\n{cost}" : Line;
    }

    /// <summary>
    /// #804 · WHAT HE FINDS IN YOUR WALLET. Four rungs, and each one teaches something different — #683's
    /// ladder, kept because the ladder is the storytelling: this site's pass, somebody else's site's pass,
    /// the wrong class of paper entirely, and nothing.
    /// </summary>
    /// <param name="bodyId">The site whose floor you are standing on.</param>
    /// <param name="plate">Who stopped you (<see cref="PlateOf"/>).</param>
    /// <param name="carried">The satchel. Anything that is not a pass or a chit is not in this wallet.</param>
    public static Read TheGuardReads(string bodyId, string plate, IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(plate);

        string card = ChallengeCard(plate);

        if (BadgeHeld(bodyId, carried))
        {
            return new(true, SatisfiedLine, ChallengeLabel, card);
        }

        // Somebody else's building. Named, because a refusal that sorts the wallet is worth carrying
        // (#679) — and because a captain who has worked two sites should learn that the second pass is
        // still worth keeping.
        foreach (Satchel.Item held in Satchel.OfKind(carried, Satchel.Kind.Badge))
        {
            if (SiteOfBadge(held.Id) is { Length: > 0 } site)
            {
                return new(false, WrongSiteLine(site), ChallengeLabel, card, EscortLine);
            }
        }

        // The cage chit. It is a real paper and it is real cover — for the cage. He says so the way you
        // would say a platform number, which is the most bureaucratic refusal available.
        if (CanteenTable.Cover.Held(carried))
        {
            return new(false, WrongPaperLine, ChallengeLabel, card, EscortLine);
        }

        return new(false, NothingLine, ChallengeLabel, card, EscortLine);
    }

    /// <summary>The pass works. He is not pleased and he is not suspicious; the paperwork balances and he
    /// has four more corridors to do.</summary>
    public const string SatisfiedLine =
        "👮 He reads it the way a man reads a pass at the end of a shift — the face, the site code, the " +
        "tier — and puts it back in your hand. \"Mind the wet floor round the corner.\" The round picks up " +
        "where it left off.";

    /// <summary>A pass for another site. He reads it correctly and is entirely unmoved by it.</summary>
    public static string WrongSiteLine(string site) =>
        $"🔒 He reads it, and he reads it properly: this one was issued for {BodyNames.Designation(site)} " +
        "SITE. \"That's not us.\" He does not ask how you came by it, and he does not hand it back quickly, " +
        "and neither of those is a threat. It is a man being thorough about somebody else's paperwork.";

    /// <summary>The day-labour chit, on a floor that is not the cage.</summary>
    public const string WrongPaperLine =
        "🔒 He turns the chit over once. \"That's for the cage. This isn't the cage.\" He is not wrong, and " +
        "he says it the way you would say a platform number.";

    /// <summary>Nothing at all. The waiting is the whole of it.</summary>
    public const string NothingLine =
        "🔒 Nothing comes out of your wallet that this floor has ever heard of. He waits the entire time you " +
        "are looking — longer than he needs to, and exactly as long as the form says.";

    /// <summary>
    /// #804 · THE MILDEST HONEST CONSEQUENCE, and the whole of it.
    ///
    /// <para>Owner's law: <i>"a rolling guard has no reason to run after anyone just on sight."</i> Nothing
    /// here escalates. He walks you back to the car, at your pace, and the cost is that somebody now knows
    /// you were on this floor — which is #715's per-entity memory arriving as one line in a book rather than
    /// as a meter, because a meter would be the announcement that issue's canon section rules out.</para>
    /// </summary>
    public const string EscortLine =
        "👮 He walks you back to the car himself, at your pace, talking about the pumps. He presses the " +
        "button for you and stands there while the doors shut. Nothing is taken, nobody is called, and " +
        "somewhere a line goes into a book with the time on it.";

    /// <summary>What the field book keeps of an escort. It records what happened and never the mechanic —
    /// and it is the first thing this game has ever filed about being KNOWN somewhere.</summary>
    public const string EscortNote =
        "Walked back to the lift by a man on the security rota. No voices, no confiscation, and a line in " +
        "a book with the time on it.";

    /// <summary>#804 · How long a guard leaves it before the round stops at you again. Long enough that an
    /// escort is one event rather than a loop at the lift doors, short enough that it is not a free pass for
    /// the rest of the excursion. FLAGGED for the owner's tuning.</summary>
    public const double AfterTheStopSeconds = 45.0;

    /// <summary>Every authored sentence this feature owns, for the canon grep. It walks THIS, so a line
    /// added tomorrow is checked tomorrow — #709's discipline, and the reason a catalog is a method rather
    /// than a list somebody remembers to update.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (string plate in Plates)
        {
            yield return plate;
        }
        yield return HeardLine;
        yield return ChallengeLabel;
        yield return ChallengeCard("◈ A PLATE");
        yield return SatisfiedLine;
        yield return WrongSiteLine("luna");
        yield return WrongPaperLine;
        yield return NothingLine;
        yield return EscortLine;
        yield return EscortNote;
        yield return BadgeIssuedLine;
        yield return BadgeGist;
        yield return BadgeTitle("luna");
        yield return BadgeTier;
    }
}
