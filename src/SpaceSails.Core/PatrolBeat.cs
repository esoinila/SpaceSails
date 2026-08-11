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
/// CHALLENGE — and that is still the DEFAULT and still the only thing a sighting on its own can ever
/// buy. See the #835 block below for the other branch, which the owner added later and deliberately,
/// and which no ambient round can reach.</item>
/// </list>
///
/// <h3>#835 · THE ONE LAW THAT WAS REVERSED, AND HOW MUCH OF IT SURVIVED</h3>
///
/// <para>This header used to say <i>"nothing in this file can start a chase"</i>. Owner, evening playtest
/// 2026-08-11: <i>"they need to catch us .... like reevers :-D we could use that code :-D"</i>. So there is
/// now a second branch — and it is a BRANCH, not a mood. <b>A guard who merely sees a captain still hails
/// and still walks over and still at worst walks you back to the car.</b> Pursuit needs one of exactly
/// three earned things (<see cref="Provocation"/>) and nothing else in the game can hand it one. The old
/// law is the default; the new branch is the exception; and being caught costs no health at all, ever
/// (<i>"just no damage by default :-D"</i>).</para>
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

    /// <summary>
    /// #793 · A GUARD, AS A BENCH SEES THEM — and the answer is always the same: NOT A TAIL.
    ///
    /// <para>A round is <see cref="BeatFor"/>'s stop list, a pure function of (site, floor, watch) and the
    /// floor plan. It is laid before the captain steps off the car and it does not read them, which is
    /// precisely <see cref="FootTail.Mover.OnAPublishedRound"/> — so #793's counter-surveillance question is
    /// answered <b>no</b> by every guard in the building by construction, rather than by a clause somebody
    /// remembered to write at a call site.</para>
    ///
    /// <para>That is a LAW and not a gap. A round that started following people would not be a round: the
    /// whole of this file is somebody doing a job on a rota, and a captain who sits down on a bench and
    /// watches a patrol keep its pace has learned something true about the building.</para>
    /// </summary>
    public static FootTail.Mover OnTheRound(int index, double x, double y) =>
        FootTail.OnARound(DeckName(index), x, y);

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

    /// <summary>#832 · How many times a leg may be re-planned after the ground refuses a step before the
    /// round gives up on that stop and walks to the next one. A snag is not an arrival and must not buy a
    /// stand — but a stop nothing can reach must not be ground at forever either, and an A* plan per frame
    /// is not free in WASM. Small on purpose: the audit (§13.1) says every leg of every round on every floor
    /// this generator builds connects, so this is the belt on top of the braces.</summary>
    public const int RePlansPerLeg = 6;

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

    // ── #832 · THE RETIRED-COP LAW, which tunes every number in this block ────────────────────────────
    //
    // Owner, filing the whole detection stack: "Also the guard looks like retired cop, not ninja, let's make
    // them easy to detect :-D" — and then the consequence, stated as a rule: "A challenge should essentially
    // NEVER feel like an ambush. If a captain is surprised by a guard, the captain was not looking, and the
    // instruments can prove it."
    //
    // THE ASYMMETRY IS THE DESIGN. A payroll guard is the loudest thing on a floor: heavy tread, keys, a
    // radio, a man who has not needed to surprise anybody in twenty years. The Old Ones are the quiet ones,
    // and a guard being scary-quiet steals their register and wastes his own. So a guard gets the GENEROUS
    // end of every instrument here, and the difficulty of the stealth game lives in the round's coverage and
    // in the book's memory — never in the guard's stealth. (The one exception the owner carved is a class
    // that does not exist yet: "no more ninja guards :-D ... those only maybe at the most sensitive levels".
    // When it is built it is a SECOND class with its own numbers, not a nudge to these.)

    /// <summary>How far the captain's own eye reaches for a guard's marker to be drawn at all. A corridor's
    /// length: far enough to watch a round work, nowhere near the hundred metres the owner ruled out. The
    /// last stretch of it is a smear rather than a marker — see <see cref="SmearFromFraction"/>.</summary>
    public const double MarkerSightDu = 30.0;

    /// <summary>How close a guard has to be before they register that somebody is standing there. A third of
    /// the eye's reach, and the whole of the timing window. FLAGGED for the owner's tuning.</summary>
    public const double NoticeDu = 9.0;

    /// <summary>
    /// How far the boots carry — AS FAR AS THE EYE ITSELF, by the retired-cop law above.
    ///
    /// <para>It was 18 du, deliberately short of the marker, and that left a band nobody meant to build:
    /// between 18 and 30 du a guard on the other side of a wall was neither drawn nor heard, which is
    /// #832's <i>"at no range may a walking man be neither seen nor heard while the instruments claim to be
    /// working"</i>. Raised to the eye's own reach so the ladder closes: anything the eye could have shown
    /// you, the ear will tell you about when a wall is in the way.</para>
    ///
    /// <para>It does not overtake the marker, because <see cref="Heard"/> is silent about anybody you can
    /// actually see — the line is a warning about a corridor you cannot look into, never a narrator for one
    /// you are looking at.</para>
    /// </summary>
    public const double EarshotDu = MarkerSightDu;

    /// <summary>#830 · WHAT A GUARD RETURNS ON THE FAN WHEN HE IS NOT WALKING. Declared here, in Core, and
    /// read by the client's one accessor — so the register of a figure is a fact about the figure rather
    /// than a flag somebody remembered to pass. A man on a rota standing at a stop is the owner's own
    /// example of a thing that has NOT earned quiet: <i>"if the guard is still then it would be a blurry
    /// blob"</i>.</summary>
    public const MotionTracker.AtRest FanRegister = MotionTracker.AtRest.Restless;

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

    // ── #832 · THE EYE'S EDGE IS NOT A CLIFF ──────────────────────────────────────────────────────────
    //
    // Owner, watching a guard walk a straight, open corridor: "Now the guard just vanishes into thin air ..
    // that is like huge magic trick".
    //
    // MarkerSightDu was a hard cutoff, and the circle it cuts on is invisible to the player. Inside it, a
    // crisp marker with a round number over it; one deck unit outside, nothing at all, in open air, with no
    // wall to blame. Popping at a WALL reads as physics — that is what a wall is for, and it stays exactly
    // as it was. Popping in the open reads as the building cheating.
    //
    // A person does not vanish at a range; they stop being resolvable. So the last fifth of the eye's reach
    // is a DISTANT FIGURE: a silhouette with no plate and no round number on it — somebody is down there,
    // and you cannot yet say who or which. It is the same idiom as #830's fan blob and the tracker's own
    // smudge: when the instrument is unsure, it says so by drawing less, never by drawing nothing.

    /// <summary>#832 · WHAT THE CAPTAIN CAN MAKE OF A GUARD AT THIS RANGE. Ordered, so a test can assert
    /// that the ladder never skips a rung — the whole failure was a jump from the top of it to the
    /// bottom.</summary>
    public enum Sighting
    {
        /// <summary>Nothing on the deck. Past the eye's reach, or behind a wall.</summary>
        None = 0,

        /// <summary>A distant figure: the silhouette, without the plate or the round's number. You know
        /// somebody is at the far end of the corridor; you do not know which round it is.</summary>
        Smear = 1,

        /// <summary>The marker as it has always been drawn — body, facing and the round's number.</summary>
        Plain = 2,
    }

    /// <summary>#832 · Where along the eye's reach a marker stops being a marker and becomes a distant
    /// figure. The outer fifth, which is far enough down a corridor that "I cannot tell yet" is the truth
    /// and close enough that it is not most of the feature. FLAGGED for the owner's tuning — and by the
    /// retired-cop law it may only ever move OUTWARD for a guard, never in.</summary>
    public const double SmearFromFraction = 0.8;

    /// <summary>The range at which the figure starts to go, in deck units.</summary>
    public static double SmearFromDu => MarkerSightDu * SmearFromFraction;

    /// <summary>
    /// #832 · The one ranging question, answered in three rungs. Wall occlusion still cuts to
    /// <see cref="Sighting.None"/> instantly and without a smear tier: a body stepping behind shotcrete IS
    /// gone, that is physics, and softening it would take back the cover the whole timing game is played
    /// against.
    /// </summary>
    public static Sighting SightingFor(
        double captainX, double captainY, double guardX, double guardY,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        if (!EyesOn(captainX, captainY, guardX, guardY, MarkerSightDu, walls))
        {
            return Sighting.None;
        }
        double dx = guardX - captainX, dy = guardY - captainY;
        return (dx * dx) + (dy * dy) >= SmearFromDu * SmearFromDu ? Sighting.Smear : Sighting.Plain;
    }

    /// <summary>Is this guard on the captain's deck AT ALL? The captain's eye, at the eye's reach — the same
    /// one question <see cref="SightingFor"/> answers, so the marker and the smear can never be gated by two
    /// different predicates.</summary>
    public static bool DrawnFor(
        double captainX, double captainY, double guardX, double guardY,
        IReadOnlyList<SurfaceCollision.Segment>? walls) =>
        SightingFor(captainX, captainY, guardX, guardY, walls) != Sighting.None;

    /// <summary>Has this guard registered that somebody is standing there? Their eye, at their own much
    /// shorter reach — the same predicate, the other way round.</summary>
    public static bool Notices(
        double guardX, double guardY, double captainX, double captainY,
        IReadOnlyList<SurfaceCollision.Segment>? walls) =>
        EyesOn(guardX, guardY, captainX, captainY, NoticeDu, walls);

    // ── #833 · THE APPROACH: HE COMES OVER, AND ONLY THEN DOES HE READ ────────────────────────────────
    //
    // Owner, filing the beat the challenge never had: "I think the guard should approach us when it does the
    // inspection." Until now the card went up on the frame Notices() fired — at up to NoticeDu, with the man
    // standing wherever the round had left him. A man reading your wallet from nine deck units off is
    // telepathy wearing a uniform.
    //
    // The choreography, and every rung of it is a fact this file owns rather than a client's timer:
    //
    //   1. NOTICE   — unchanged. He registers somebody standing there, at his own short reach.
    //   2. THE HAIL — he turns and says HailLine. One second of warning that the next thing is happening.
    //   3. THE WALK — he crosses to CardReachDu on his own A*/slide gait, live on the fan the whole way. The
    //                 captain's controls stay FREE: walking away is allowed, and is its own tell.
    //   4. THE READ — the card raises on ARRIVAL and nowhere else. Inspections happen where inspections
    //                 happen.

    /// <summary>#833 · How close a man has to be standing to take a pass out of your hand and look at it. An
    /// arm and a step: the distance two people talk at, and the distance the card may raise at. FLAGGED for
    /// the owner's tuning — and it may never be raised toward <see cref="NoticeDu"/> without the whole beat
    /// going back to being a magic trick.</summary>
    public const double CardReachDu = 2.0;

    /// <summary>#833 · Is he close enough to read it? The ONE question the card is gated on, asked here so
    /// the walk-up and the guard that proves it cannot be looking at two different numbers.</summary>
    public static bool AtCardReach(double guardX, double guardY, double captainX, double captainY)
    {
        double dx = captainX - guardX, dy = captainY - guardY;
        return (dx * dx) + (dy * dy) <= CardReachDu * CardReachDu;
    }

    /// <summary>#833 · How far the captain may get before a hail is abandoned. A stride or two past the reach
    /// he registered you at — a man who has said "hold on" does not give up because you took one step, and he
    /// does not follow you round the building either.
    ///
    /// <para>#835 · …the FIRST time. Walking away from a hail is still free, still allowed and still ends in
    /// <see cref="WalkedAwayLine"/>; it is doing it TWICE in one watch that is a
    /// <see cref="Provocation.WalkedAwayTwice"/>. This number is untouched by that: he still stops coming
    /// here, at the same range. What changed is what he does next.</para></summary>
    public const double GivesUpBeyondDu = NoticeDu + 4.0;

    /// <summary>#833 · The longest a walk-up may take before he thinks better of it. The belt on top of the
    /// braces: a captain circling a pillar out of arm's reach is walking away by any honest reading, and a
    /// guard must never be left crossing a corridor forever.</summary>
    public const double WalkUpSeconds = 20.0;

    /// <summary>#833 · Is he still coming? False the moment the captain is out past
    /// <see cref="GivesUpBeyondDu"/> or the walk-up has run past <see cref="WalkUpSeconds"/> — and the caller
    /// then simply puts him back on his round, which is the whole of the consequence in this phase.</summary>
    public static bool StillComing(
        double secondsWalkingUp, double guardX, double guardY, double captainX, double captainY)
    {
        if (double.IsNaN(secondsWalkingUp) || secondsWalkingUp > WalkUpSeconds)
        {
            return false;
        }
        double dx = captainX - guardX, dy = captainY - guardY;
        return (dx * dx) + (dy * dy) <= GivesUpBeyondDu * GivesUpBeyondDu;
    }

    /// <summary>#833 · How often a walk-up re-plans on the moving target the captain is. Not every frame: an
    /// A* is not free in WASM, and a man crossing a corridor does not re-decide his route sixty times a
    /// second either.</summary>
    public const double RePlanEverySeconds = 0.5;

    /// <summary>#833 · THE HAIL. Terse on purpose — the whole beat is the second of warning it buys, and a
    /// paragraph would spend that second. Nothing in it explains anything and nothing in it threatens.</summary>
    public const string HailLine =
        "👮 \"You there — hold on.\" He turns, tucks the clipboard under his arm, and starts walking over. " +
        "He is in no hurry at all.";

    /// <summary>#833 · What it looks like when a captain simply keeps walking. Allowed — the controls are
    /// never taken during the approach — and it is its own tell: the round does not follow, it writes.</summary>
    public const string WalkedAwayLine =
        "👮 He stops where he is, watches you go the length of the corridor, and writes something short on " +
        "the clipboard.";

    // ── #833 · THE ESCORT, WALKED ─────────────────────────────────────────────────────────────────────
    //
    // Owner, evening playtest 2026-08-11, four challenges on B2: "how did I jump to elevator there?" … "So
    // the guard walk me back to the car". The sentence said WALK and the sim did a placement — the
    // sentence-vs-sim bug class, verbatim, in the one feature whose entire register is procedure.
    //
    // So the numbers below exist to make EscortLine literally true: he plans a route to the car, the captain
    // is walked along at his shoulder through the captain's own collision, and neither of them is ever put
    // anywhere. Both are moving contacts on the fan the whole way, which is the one guaranteed long walk
    // beside a guard this game has.

    /// <summary>#833 · How far from him the captain is walked — half a pace back and half a pace to the
    /// side, which is where you end up beside somebody who is showing you out.</summary>
    public const double ShoulderDu = 1.3;

    /// <summary>#833 · How far the captain may lag before the guard WAITS. He is escorting, not racing: a man
    /// who walked off and left you would not be walking you anywhere. It is also what guarantees the pair
    /// arrive together rather than the escort ending with the captain still down the corridor.</summary>
    public const double TetherDu = 2.6;

    /// <summary>#833 · How much brisker than the guard the captain's own legs are worked so a lag closes
    /// rather than becoming permanent. Above one, and modest: this is somebody keeping up, not a tow.</summary>
    public const double CatchUpFactor = 1.6;

    /// <summary>#833 · Close enough to the car to BE at it — the end of the escort, measured on the captain,
    /// because the captain arriving is the thing the escort was about.</summary>
    public const double AtTheCarDu = 0.9;

    /// <summary>#833 · The whole escort's bound in seconds. Past it the walk is abandoned and the cut is
    /// ADMITTED (<see cref="EscortCutLine"/>) rather than narrated as a walk that did not happen.</summary>
    public const double EscortSecondsCap = 90.0;

    /// <summary>#833 · When the small talk lands, in seconds into the walk. Far enough in that it is
    /// something said on a walk rather than a line fired at a placement.</summary>
    public const double PumpsAfterSeconds = 2.5;

    /// <summary>#833 · The small talk that is the punishment's whole texture: a man so unbothered by you that
    /// he makes conversation about the plant while he walks you off his floor.</summary>
    public const string PumpsLine =
        "👮 \"They've had the pumps out on three since Tuesday,\" he says, to nobody in particular. \"Same " +
        "three.\"";

    /// <summary>#833 · The end of the walk, and the moment the captain has the controls back. A hand-back
    /// with no sentence on it is the sim doing something the prose never mentioned.</summary>
    public const string EscortDoneLine =
        "👮 The doors open on an empty car. He waits until you are in it, and then goes back to the round " +
        "without another word.";

    /// <summary>#833 · The one honest way to keep a jump-cut: SAY it is one. Used only when the ground
    /// refuses to give him a route to the car at all — the audit (§13.1) says that cannot happen on a floor
    /// this generator builds, and a guard is not the place to find out otherwise. The sentence may never
    /// claim a walk the sim did not take, so this one does not.</summary>
    public const string EscortCutLine =
        "👮 Next thing you know, you are at the lift. Whatever the walk back was, none of it stayed with you.";

    /// <summary>#833 · What he says to a captain who tries to steer while being walked off the floor. The
    /// controls ARE held for this stretch — the only stretch in the feature where they are — so the refusal
    /// has to be said, and it has to be said the way a man on a rota would say it.</summary>
    public const string EscortHeldLine = "👮 \"This way.\" He is walking you out, and he is walking you out.";

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

    /// <summary>#804 · The painting the card wears — a contract guard, palm up, clipboard under the arm, a
    /// laminated pass on his chest, in a shotcrete corridor of bolt plates with faint chalk scrawls on the
    /// walls (an accidental #794 nod the owner spotted in his own generation).
    ///
    /// <para>ONE picture for all four rungs, deliberately. The card's body is the same sentence whatever
    /// comes out of the wallet — <see cref="ChallengeCard"/> describes a man who has not read it yet — and a
    /// per-outcome plate would be the picture telling the captain how the read went before he has finished
    /// reading. The verdict lives in the amber row under the image and nowhere else (#736).</para>
    ///
    /// <para>A constant here rather than a literal in the client, for the reason every other plate is one: a
    /// razor that names a jpg is a second answer to "which picture is this moment", and the one that cannot
    /// be kept in step with the prose beside it. #804 shipped caption-only under the degradation law; this
    /// closes that gap without the degradation law changing at all.</para></summary>
    public const string ChallengeArtUrl = "art/the-round-stops-at-you.jpg";

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
    ///
    /// <para>#833 · <b>And every clause of it is now literally true.</b> It shipped over a placement — the
    /// captain was PUT at the lift and the guard stayed where he was, so the sentence claimed a walk the sim
    /// did not take, in the feature whose whole register is procedure. The walk is real now (the client's
    /// escort: he plans a route, the captain is walked at his shoulder, both are contacts on the fan the
    /// whole way, <see cref="PumpsLine"/> is said on it), so the only line here that had to change is the one
    /// that is kept for when the ground refuses a route at all — <see cref="EscortCutLine"/>, which admits
    /// the cut instead of narrating it.</para>
    ///
    /// <para>#835 · <b>And it is no longer the whole of it — it is the whole of it for the first
    /// <see cref="EscortsAWatchAllows"/> times.</b> Past that the same walk keeps going, into the car and up
    /// (<see cref="KickOutDueLine"/>). Nothing about this rung changed; a rung was added above it.</para>
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

    // ── #835 · THE OTHER BRANCH: HE COMES AFTER YOU, AND HE CATCHES YOU ───────────────────────────────
    //
    // Owner, evening playtest 2026-08-11, having been escorted four times off one floor by one man who then
    // just kept walking: "they need to catch us .... like reevers :-D we could use that code :-D" — and,
    // in the same breath, the constraint that keeps the register: "just no damage by default :-D".
    //
    // EVERY RUNG BELOW IS PROCEDURE. He does not shout, he does not draw anything, and nothing that happens
    // when he reaches you touches the body. He says a number into a radio, he runs — badly, for a man of his
    // age — he takes your arm, and then the building does the only two things it knows how to do: it walks
    // you to a lift, and if you have used up its patience it walks you all the way to the sky.
    //
    // WHAT IS DELIBERATELY NOT HERE. There is no alert state, no floor-wide hunt, no second guard summoned
    // by the radio, and no memory of a chase past the shift. #618's canon constraint is unspent: this is a
    // cover that can blow, not a detection meter. One man comes; one man gives up or one man catches you.

    /// <summary>
    /// #835 · WHY HE IS RUNNING. THE WHOLE LIST, and a captain can read every one of them off their own
    /// evening — which is the difference between an escalation and an ambush.
    ///
    /// <para><see cref="None"/> is what a sighting buys and what almost every stop on almost every floor
    /// will ever be. Nothing in the game hands out any other value except the three places named on each
    /// member, so "ambient rounds never chase a walking captain" is a fact about this enum rather than a
    /// clause somebody remembered to write.</para>
    /// </summary>
    public enum Provocation
    {
        /// <summary>Nothing has happened. He hails, he walks over, he reads (#833), and the worst of it is
        /// a walk back to the car (#804). THE DEFAULT, and the only answer an ordinary round ever gets.</summary>
        None = 0,

        /// <summary>You have walked off on him before, this watch, and now you are doing it again. The first
        /// one is free and stays free — <see cref="WalkedAwayLine"/>, the man who stops and writes — because
        /// a guard who followed you the first time would make #833's whole approach a trap.</summary>
        WalkedAwayTwice,

        /// <summary>He watched you take a hasp off a door with a gun. The one crime these floors actually
        /// have a verb for (#803's DESIGNATE, and the shot that goes in <c>GunfireHeard</c>'s own ledger),
        /// so it is the one crime listed. It is deliberately not "he heard it somewhere": the ledger already
        /// keeps the noise, and a man who came running at a bang three corridors away would be the
        /// floor-wide hunt this feature does not have.</summary>
        SeenAtTheHasp,

        /// <summary>He has written you down <see cref="EscortsAWatchAllows"/> times already this watch. The
        /// owner's own finding: <i>the fiction strains when the same guard books the same face four times
        /// and just keeps walking.</i></summary>
        BookedTooManyTimes,
    }

    /// <summary>Is this a reason to come after somebody? The one gate, so the answer cannot be spelled twice.
    /// A caller with <see cref="Provocation.None"/> in its hand is a caller that must walk away.</summary>
    public static bool EarnsIt(Provocation why) => why != Provocation.None;

    /// <summary>#835 · HOW MANY TIMES ONE WATCH WILL WRITE YOU DOWN AND STILL JUST WALK YOU TO THE LIFT.
    /// Three, because the owner hit four in one evening and the fourth was the one that read as a bug in the
    /// fiction. It is the SAME number on both sides of the ladder — the stop that stops being an escort is
    /// the stop that becomes the way out of the building — so the escalation cannot grow a second threshold
    /// that drifts from this one. FLAGGED for the owner's tuning.</summary>
    public const int EscortsAWatchAllows = 3;

    /// <summary>Has this watch already spent its patience? Asked with the escorts BEFORE the current one, so
    /// the fourth stop is the different one.</summary>
    public static bool BookedTooOften(int escortsThisWatch) => escortsThisWatch >= EscortsAWatchAllows;

    /// <summary>#835 · How many hails a captain may simply walk away from before the round stops accepting
    /// it. One: the first is #833's own beat and its own tell, and taking that away would make the approach
    /// something to be frightened of rather than something to decide about.</summary>
    public const int HailsYouMayWalkAwayFrom = 1;

    /// <summary>Is walking off this time the one too many? Asked with the walk-aways INCLUDING this one.</summary>
    public static bool WalkingOffEarnsIt(int walkedAwayThisWatch) =>
        walkedAwayThisWatch > HailsYouMayWalkAwayFrom;

    /// <summary>#835 · The beat of radio before he moves. He does not run and talk at once — he stands still
    /// for this, which is the warning, and it is the same courtesy the hail was: a second and a half in which
    /// a captain who is quick can already be round the corner. FLAGGED.</summary>
    public const double CallItInSeconds = 1.6;

    /// <summary>
    /// #835 · HOW FAST HE COMES. Twice his round's own pace and still comfortably short of the captain's own
    /// legs (9.0 du/s on the deck), which is not a compromise — it is the whole of rung five: <b>a captain
    /// who runs for the lift gets to the lift.</b> He catches people who hesitate, people who are cornered
    /// and people who thought a locked door would open for them. FLAGGED for the owner's tuning, and it may
    /// never be raised past the captain's walk without the escape stopping being an escape.
    /// </summary>
    public const double AfterYouSpeed = WalkSpeed * 2;

    /// <summary>#835 · How long he will keep it up. He is a retired cop and not a wolf: past this he is
    /// blowing, and a captain who has stayed ahead of him for forty seconds has genuinely got away.</summary>
    public const double AfterYouSecondsCap = 40.0;

    /// <summary>#835 · How far ahead of him you have to get before he has lost you. Comfortably outside the
    /// eye's own reach at the moment he gives up, so the end of it is never a man staring straight at you and
    /// deciding not to bother.</summary>
    public const double LosesYouBeyondDu = MarkerSightDu - 4.0;

    /// <summary>#835 · Is he still coming? Same shape as <see cref="StillComing"/>, on purpose: the walk-up
    /// and the run are the same question at two speeds, and a second shape would be a second set of edge
    /// cases. False ends it in a clean escape and never in a despawn — he is still standing there.</summary>
    public static bool StillAfterYou(
        double secondsAfterYou, double guardX, double guardY, double captainX, double captainY)
    {
        if (double.IsNaN(secondsAfterYou) || secondsAfterYou > AfterYouSecondsCap)
        {
            return false;
        }
        double dx = captainX - guardX, dy = captainY - guardY;
        return (dx * dx) + (dy * dy) <= LosesYouBeyondDu * LosesYouBeyondDu;
    }

    /// <summary>#835 · Has he got you? <see cref="ReeverChase.Caught"/> verbatim — the owner named that code
    /// and this is it, down to the radius: a catch is two bodies touching, and #469 already settled what
    /// touching means on this ground. A hand on your arm and a hand on your throat are the same geometry;
    /// everything that makes them different happens afterwards, and none of it is in the body.</summary>
    public static bool HasYou(double guardX, double guardY, double captainX, double captainY) =>
        ReeverChase.Caught(guardX, guardY, captainX, captainY);

    /// <summary>#835 · THE WARNING THAT THE NEXT THING IS HAPPENING. Four words of radio, said standing
    /// still, and then he moves — the whole beat exists so that being run down is something a captain
    /// watched start.</summary>
    public const string CallsItInLine =
        "📻 Two fingers on the shoulder mic, a floor number and a direction, and that is the entire message. " +
        "Then the clipboard goes under his arm and he comes after you, and he is quicker than he looks.";

    /// <summary>#835 · What he says when he has you, and it is always the reason. #603's law: a consequence
    /// names why. Nothing in any arm of this is a threat; every arm of it is a man filling in the part of
    /// the form that asks what happened.</summary>
    public static string WhyHeCame(Provocation why) => why switch
    {
        Provocation.WalkedAwayTwice => WalkedOffTwiceLine,
        Provocation.SeenAtTheHasp => SawYouAtTheHaspLine,
        Provocation.BookedTooManyTimes => BookedLine(EscortsAWatchAllows + 1),
        _ => WalkedOffTwiceLine,
    };

    /// <summary>He asked you to hold on, twice, and twice you kept walking.</summary>
    public const string WalkedOffTwiceLine =
        "🔒 \"Twice,\" he says, when he has the breath for it. \"You walked off on me twice.\" He is not " +
        "angry about it. He is filling in the part of the form that asks why.";

    /// <summary>He watched the hasp come off. Said the way a man reads an address back over a counter.</summary>
    public const string SawYouAtTheHaspLine =
        "🔒 \"I watched you do that,\" he says. \"So there's no version of tonight where I didn't.\" He does " +
        "not say what it was and he does not need to; he says it the way you read an address back over a " +
        "counter.";

    /// <summary>The clipboard, turned round. The number in the sentence is the constant, so the man and the
    /// rule can never come to be counting different evenings.</summary>
    public static string BookedLine(int times) =>
        $"🔒 \"That's {times} times tonight,\" he says, and turns the clipboard round, and it genuinely is " +
        $"{times} lines with times written against them. \"I can keep doing these. I'd rather not.\"";

    /// <summary>#835 · The card. It names what has happened to your arm, because that is the whole of what
    /// has happened to you.</summary>
    public const string CaughtLabel = "👮 A HAND ON YOUR ARM";

    /// <summary>
    /// #835 · WHAT BEING CAUGHT IS. Evidence, and then it stops (§13.9) — and the evidence is that nothing
    /// has been done to you. <b>Zero damage is not a number this card hides; it is the thing the card is
    /// about.</b>
    /// </summary>
    public static string CaughtCard(string plate) =>
        $"{plate} — and he is out of breath, which is somehow the worst of it. No lamp, no raised hand, " +
        "nothing swung. He takes your upper arm the way a steward takes an elbow, waits until you have " +
        "stopped moving, and lets go of it again.\n\n" +
        "Nothing is broken and nothing is bleeding, and none of that was ever on the table. What has " +
        "happened is that the rest of your evening has become somebody's shift, and there is a form for it.";

    /// <summary>#835 · The read a catch hands back: the reason, on the card, with what it costs under it.
    /// Same two-armed <see cref="Read"/> the wallet uses, so the client raises the identical card for a
    /// challenge and for a catch and neither can grow a presentation of its own.</summary>
    /// <param name="plate">Who has you (<see cref="PlateOf"/>).</param>
    /// <param name="why">What earned it.</param>
    /// <param name="escortsThisWatch">Escorts BEFORE this one — the same number
    /// <see cref="BookedTooOften"/> is asked at the walk, so the sentence and the sim cannot disagree about
    /// which floor this walk ends on.</param>
    public static Read TheGuardHasYou(string plate, Provocation why, int escortsThisWatch)
    {
        ArgumentNullException.ThrowIfNull(plate);
        return new(
            false, WhyHeCame(why), CaughtLabel, CaughtCard(plate),
            BookedTooOften(escortsThisWatch) ? KickOutDueLine : EscortLine);
    }

    /// <summary>#835 · A CLEAN ESCAPE, AND HE IS STILL STANDING THERE. Never a despawn: the man who stopped
    /// running is the same man, on the same rota, on the same floor, and he has now seen which way you went.</summary>
    public const string LostYouLine =
        "👮 The boots stop somewhere behind you. There is one more word on the radio, and then the corridor " +
        "is a corridor again — except that somebody now knows which way you went.";

    // ── #835 · THE TOP RUNG: BACK TO THE SKY ──────────────────────────────────────────────────────────
    //
    // Owner: "If we get kicked out then maybe we end up back to the surface :-D" — and the reason it is the
    // right top rung is that it costs the game's two oldest currencies without inventing anything: the walk
    // back is half a tank of air, and the way back in is a piece of paper you no longer have.
    //
    // It is the escort (#833) that simply keeps going: to the car, into the car, up. The building does not
    // do drama; it does paperwork and doors.

    /// <summary>#835 · What the card says the walk is this time. He is not pressing the button for your
    /// floor.</summary>
    public const string KickOutDueLine =
        "👮 This time he does not press the button for your floor. He gets in with you, and the panel is " +
        "already lit for the top.";

    /// <summary>#835 · THE BIG TEXT, as the tube doors part. Authored by the owner, verbatim, and it wears
    /// the descent plate's own typography — the same wall stencil that says <c>−162 m</c> on the way down,
    /// because the ejection is the same building saying the same kind of thing. No red, no klaxon, no shake:
    /// the vacuum banner immediately under it is the actual punchline.</summary>
    public const string KickedOutBigText = "KICKED OUT";

    /// <summary>#835 · The one quiet line, once, at pulse size, as the doors close behind you. Owner's copy,
    /// verbatim and unglyphed — a sentence this flat does not want a picture in front of it.</summary>
    public const string DoorsCloseLine = "The doors do not slam. They just close.";

    /// <summary>#835 · How long the big text stays painted on the shed wall before it comes down. Long enough
    /// to read twice standing still, short enough that it never becomes wallpaper — #694's lesson, paid for
    /// by a facility name that drew on all thirteen floors. FLAGGED.</summary>
    public const double KickedOutPlateSeconds = 14.0;

    /// <summary>#835 · THE PASS, TAKEN — and SAID, because a possession that leaves the satchel silently is
    /// the sim doing something the prose never mentioned. Only ever said when there was one to take.</summary>
    public const string PassRevokedLine =
        "🪪 At the top he puts his hand out for the pass and does not give it back. \"You'll want a different " +
        "one of these,\" he says, and puts it in his breast pocket with the others.";

    /// <summary>What the book keeps of that. It is the mechanic's whole future consequence stated as a fact:
    /// the way back in is now somebody else's paperwork.</summary>
    public const string PassRevokedNote =
        "The site pass went into a man's breast pocket at the top of the cage. Getting back down this shaft " +
        "means turning up with a different piece of paper.";

    /// <summary>What the book keeps of the ejection itself. It records what happened and never the
    /// mechanic.</summary>
    public const string KickOutNote =
        "Walked to the car, taken all the way up and put out through the tube onto the regolith. No voices " +
        "and nothing taken but the pass. The tank started running again on the doorstep.";

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
        yield return HailLine;
        yield return WalkedAwayLine;
        yield return PumpsLine;
        yield return EscortDoneLine;
        yield return EscortCutLine;
        yield return EscortHeldLine;
        yield return ChallengeLabel;
        yield return ChallengeCard("◈ A PLATE");
        yield return SatisfiedLine;
        yield return WrongSiteLine("luna");
        yield return WrongPaperLine;
        yield return NothingLine;
        yield return EscortLine;
        yield return EscortNote;
        yield return CallsItInLine;
        yield return WalkedOffTwiceLine;
        yield return SawYouAtTheHaspLine;
        yield return BookedLine(EscortsAWatchAllows + 1);
        yield return CaughtLabel;
        yield return CaughtCard("◈ A PLATE");
        yield return LostYouLine;
        yield return KickOutDueLine;
        yield return KickedOutBigText;
        yield return DoorsCloseLine;
        yield return PassRevokedLine;
        yield return PassRevokedNote;
        yield return KickOutNote;
        yield return BadgeIssuedLine;
        yield return BadgeGist;
        yield return BadgeTitle("luna");
        yield return BadgeTier;
    }
}
