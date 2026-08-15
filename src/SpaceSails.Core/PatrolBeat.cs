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
/// <para><b>From the floor that breathes down to the last one the building admits to.</b> #709 put people on
/// B1 only and said why: the population falling to zero is what makes the descent a gradient. This does not
/// spend that. It walks the facility's WORKING floors — everything from the top pressurised floor
/// (<see cref="UndergroundComplex.TopPressurisedFloor"/>) to the last one the directory owns up to
/// (<c>DepthOf</c>) — which are exactly the floors that have a payroll to put somebody on.</para>
///
/// <para>#863 · <b>THE BAR FLOOR IS ONE OF THEM NOW.</b> It used to be the exception — the one working floor
/// nobody walked, on the grounds that a round would ask a room of contractors for passes under a plate
/// reading <c>NO PASS REQUIRED</c>. Owner, 2026-08-13: <i>"let's try to have round because the guard looks
/// kind of silly just standing in the middle of an aisle."</i> The plate is untouched and so is the descent:
/// what B1 gains is a man on a rota walking past people who are eating, which is the most ordinary thing in
/// the building and therefore the best possible floor to learn a round on.</para>
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
    /// <item><b>Underground, and no shallower than the building breathes.</b>
    /// <see cref="UndergroundComplex.TopPressurisedFloor"/> is where the facility starts; the regolith over
    /// it is nobody's rota.</item>
    /// <item><b>Not deeper than the directory admits.</b> <c>DepthOf</c> is the building's account of
    /// itself, and a payroll stops where the account does.</item>
    /// <item><b>Never the head office.</b> #411's building has no gate, no shafts to card and a different
    /// fiction entirely; putting a contract guard in the wintering hall would be this file deciding
    /// something about a place it does not own.</item>
    /// </list>
    ///
    /// <h3>#863 · THE ONE CLAUSE THAT WAS REVERSED: B1 IS WALKED NOW</h3>
    ///
    /// <para>This used to read <c>level &gt;= bar</c> and the bullet above it used to say <i>below the
    /// bar</i>: the top pressurised floor — the one with the park, the canteen, the offices and the
    /// washrooms on it — was the one working floor in the building nobody walked, on the grounds that a
    /// round would ask a room of contractors for passes under a plate reading <c>NO PASS REQUIRED</c>.</para>
    ///
    /// <para><b>Owner ruling, 2026-08-13,</b> watching a man stand in a furnished aisle with nothing to do:
    /// <i>"let's try to have round because the guard looks kind of silly just standing in the middle of an
    /// aisle."</i> The plate is untouched and so is the argument behind it — a captain with no paper is
    /// still walked out and never harmed — but a guard with no beat is a statue, and a statue is worse
    /// storytelling than a man who wants to see your pass.</para>
    ///
    /// <para>And the cost that had been assumed was measured and is not there. Lab 45 (§C) flagged the
    /// irony first: the FURNITURE and the ROUNDS had never once stood on the same floor, so every
    /// heavy-floor sightline number in that lab was a what-if. Post-#860 — the WallIndex eye and the plan
    /// walked during the stand — the frame cost of a round on the densest floor in the game is
    /// near-nothing, so the reason to withhold it went and the reason to have it is on the screen.</para>
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

        // #863 · …and the top pressurised floor is INSIDE this now (`> bar`, not `>= bar`). The clause still
        // does its own work — a site whose upper floors do not breathe has no rota on them either — it just
        // no longer spends the floor the owner watched a guard stand still on.
        if (UndergroundComplex.TopPressurisedFloor(bodyId) is not { } bar || level > bar)
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
    /// <param name="Point">#831 · The watchclock station this stop SNAPPED to, or null when the floor had no
    /// wall to offer within <see cref="CheckpointReachDu"/>. Carried on the stop rather than kept in a list
    /// beside it because <see cref="BeatFor"/> reverses and rotates the round: a parallel list would be one
    /// shift away from a guard facing somebody else's plate.</param>
    public readonly record struct Stop(double X, double Y, string What, Checkpoint? Point = null);

    // ── #831 · THE CHECKPOINTS, AND WHY A STAND FINALLY HAS A BODY ────────────────────────────────────
    //
    // Owner, evening playtest 2026-08-11, watching a man motionless in a corridor: "why would it just stand
    // there if there is no inspection point etc" — and then, completing it himself with the real-world
    // anchor: "they actually in real life like have these check points they electronically sign on rounds to
    // prove they did their round."
    //
    // Guard-tour watchclock stations. That one sentence turns the five seconds of StandSeconds from a timing
    // constant into a THING THE MAN IS DOING: he walks up to a plate on a wall, faces it, signs in, walks on.
    // "Why is he standing there" is answered forever, and it is answered by the building rather than by a
    // caption.
    //
    // THE FIXTURE IS PUBLISHED, and the stop SNAPS TO IT. A checkpoint is a point on a wall the generator
    // already built, clear of every opening the generator already cut, with the square a body signs it from
    // published beside it — the #862 measured-wall idiom, one scale down. The renderer bolts a plate on it
    // and measures nothing. Nothing here computes a wall of its own (§13.15).
    //
    // WHAT IS DELIBERATELY NOT HERE. The round LOG — the clipboard at the station, the ledger behind the
    // desk, the paper a gumshoe steals to read the beat's timing off it — is the #828 satchel economy's
    // work and is filed as the release after. So is the forged log: a guard who skips a leg has a lever on
    // him, and levers are not this file's business.

    /// <summary>#831 · What a checkpoint wears. A small round contact plate, which is what a watchclock
    /// station has looked like since somebody first had to prove they walked a building.</summary>
    public const string CheckpointGlyph = "🔘";

    /// <summary>#831 · What is stencilled on one. §13.8's register exactly: it names the FIXTURE and says
    /// nothing whatever about what the facility is for. The number is the station's own, fixed to the place
    /// rather than to the order a shift happens to walk them in — a plate that renumbered itself every watch
    /// would be a building that cannot count its own doors.</summary>
    public static string CheckpointPlate(int number) => $"{CheckpointGlyph} ROUND POINT {number}";

    /// <summary>#831 · Is this plate one of ours? The renderer's and the audit's one answer, so a fourth kind
    /// of stencil cannot be told apart by a string literal typed twice.</summary>
    public static bool IsCheckpointPlate(string? plate) =>
        plate is not null && plate.StartsWith(CheckpointGlyph, StringComparison.Ordinal);

    /// <summary>
    /// #831 · HOW FAR OFF A WALL A ROUND WALKS AND STANDS — the ONE number the lane and the sign-in share.
    ///
    /// <para>Owner, in the same breath as the checkpoints: <i>"they should respect right side traffic, and
    /// not walk in the middle of the corridor."</i> A man trained in a facility walks a du or two off the
    /// wall on his own side, and the man signing a plate stands at exactly that distance from it — because
    /// it is the same distance, and two numbers for it would be a lane that does not end at the station it
    /// is walking to.</para>
    ///
    /// <para>Comfortably outside a body's own width (<see cref="SignInClearDu"/>) and comfortably inside the
    /// corridor's half-width, so the lane is a lane rather than a scrape along the shotcrete. FLAGGED for
    /// the owner's tuning.</para>
    /// </summary>
    public const double LaneFromWallDu = 1.5;

    /// <summary>#831 · How far off the CENTRE LINE of a corridor the lane runs — the corridor's own half
    /// width less the distance from the wall, read off <see cref="UndergroundComplex.CorridorHalf"/> rather
    /// than typed, so a generator that widens its passages widens this with them. Two guards on opposite
    /// legs are twice this apart, which is the whole of "they pass on opposite sides".</summary>
    public static double LaneOffsetDu => UndergroundComplex.CorridorHalf - LaneFromWallDu;

    /// <summary>#831 · How straight a stretch has to be before the lane is worth keeping on it, as the cosine
    /// between the step into a waypoint and the step out of it. The lane is a PREFERENCE and never a wall:
    /// at a corner, a doorway or a rib mouth the offset is simply dropped and the round takes the middle,
    /// which is what a person does and what keeps a laned route walking through a 6.4 du leaf.</summary>
    public const double LaneStraightEnough = 0.995;

    /// <summary>#831 · How far from its stop a checkpoint may be bolted. A room's own width, so a stop in the
    /// open middle of a chamber still finds a wall — and short enough that a "checkpoint" is never a plate in
    /// the next corridor with a guard standing at it for no visible reason.</summary>
    public const double CheckpointReachDu = UndergroundComplex.RoomWidthDu;

    /// <summary>#831 · How much clear floor the square a man signs from needs. A body's own width and no
    /// more — <see cref="RipAndBin.StandClearDu"/>'s own reasoning about a bin, said again about a plate:
    /// somebody stands at one, they do not park a vehicle at one.</summary>
    public const double SignInClearDu = RipAndBin.StandClearDu;

    /// <summary>#831 · How far a checkpoint keeps from a published opening. <see cref="RingOffice.DoorClearDu"/>
    /// — the building's own answer to "how much of a wall does a doorway claim", read rather than re-derived,
    /// so a station can never end up on the jamb of a door somebody has to walk through.</summary>
    public static double ClearOfOpeningDu => RingOffice.DoorClearDu;

    /// <summary>
    /// #831 · ONE WATCHCLOCK STATION: the plate on the wall, and the square it is signed from.
    /// </summary>
    /// <param name="Number">Which station this is on the floor, 1-based and fixed to the PLACE. It is what is
    /// stencilled on it and it does not turn over with the shift.</param>
    /// <param name="X">Where the plate is bolted, on a wall the generator built.</param>
    /// <param name="Y">The same.</param>
    /// <param name="StandX">Where a body stands to sign it — <see cref="LaneFromWallDu"/> off the wall, on
    /// the side the stop was on. THIS is what the beat stop snaps to.</param>
    /// <param name="StandY">The same.</param>
    /// <param name="What">The stop it belongs to, for a red audit's printout. Never shown to a player.</param>
    public readonly record struct Checkpoint(
        int Number, double X, double Y, double StandX, double StandY, string What)
    {
        /// <summary>What is stencilled on it.</summary>
        public string Plate => CheckpointPlate(Number);

        /// <summary>Which way a man signing it is looking: AT THE FIXTURE. The whole of the owner's law in
        /// one expression — a guard standing at a stop is looking at the thing he is standing at.</summary>
        public double Facing => Math.Atan2(Y - StandY, X - StandX);

        /// <summary>Is a body at this station? The same <see cref="AtTheStopDu"/> tolerance the round's own
        /// arrival uses, because arriving at the stop and arriving at the plate are the same arrival.</summary>
        public bool Signing(double x, double y)
        {
            double dx = x - StandX, dy = y - StandY;
            return (dx * dx) + (dy * dy) <= AtTheStopDu * AtTheStopDu;
        }

        /// <summary>How far a body is from the square this is signed from.</summary>
        public double GapFrom(double x, double y) =>
            Math.Sqrt(((x - StandX) * (x - StandX)) + ((y - StandY) * (y - StandY)));
    }

    /// <summary>#831 · Every station on this floor, in the circuit's own order. The renderer's one call — it
    /// bolts a plate on each and measures nothing.</summary>
    public static IReadOnlyList<Checkpoint> CheckpointsOn(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        var found = new List<Checkpoint>();
        foreach (Stop stop in Circuit(floor, field))
        {
            if (stop.Point is { } at)
            {
                found.Add(at);
            }
        }
        return found;
    }

    /// <summary>#831 · Everything on this floor a body collides with, as the client will build it — the
    /// walls, the glazing (#759 keeps it out of <c>Walls</c> so nothing paints it as concrete, and it stops a
    /// body all the same) and the doors that never open. A station whose standing square were measured
    /// against a shorter list than the one the game collides with would be the drawn-versus-simulated split
    /// this house has a name for.</summary>
    private static List<SurfaceCollision.Segment> Blockers(in UndergroundComplex.FloorPlan floor)
    {
        var segs = new List<SurfaceCollision.Segment>();
        foreach (SurfaceLayout.Wall w in floor.Walls ?? [])
        {
            segs.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));
        }
        foreach (SurfaceLayout.Wall w in floor.Windows ?? [])
        {
            segs.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));
        }
        foreach (UndergroundComplex.LockedDoor l in floor.Locked ?? [])
        {
            segs.Add(new SurfaceCollision.Segment(l.X1, l.Y1, l.X2, l.Y2));
        }
        return segs;
    }

    /// <summary>
    /// #831 · WHERE THIS STOP'S STATION IS BOLTED — the nearest wall the building offers, measured.
    ///
    /// <para>Three tests and a station has to pass all three, which is <see cref="RipAndBin"/>'s own idiom
    /// one fixture along: the plate is ON a published wall, it is <see cref="ClearOfOpeningDu"/> from every
    /// published hole a body walks through, and there is somewhere to STAND at it that a body fits on. A
    /// candidate that fails any of them is skipped and the next wall round is tried.</para>
    ///
    /// <para>It is the NEAREST passing wall and not a chosen one, and that is deliberate: a facility bolts a
    /// watchclock station to whatever is at hand where the round has to stop — a corridor face, a room's own
    /// wall, the end of a bench somebody installed in 40-something. What it never does is put one across a
    /// doorway.</para>
    /// </summary>
    private static Checkpoint? PointFor(
        in Stop stop, int number,
        in UndergroundComplex.FloorPlan floor,
        List<SurfaceCollision.Segment> blockers)
    {
        Checkpoint? best = null;
        double bestGap = double.MaxValue;

        foreach (SurfaceLayout.Wall w in floor.Walls ?? [])
        {
            (double px, double py) = NearestOnSegment(stop.X, stop.Y, w.X1, w.Y1, w.X2, w.Y2);
            double dx = stop.X - px, dy = stop.Y - py;
            double gap = Math.Sqrt((dx * dx) + (dy * dy));

            // Cheap tests first, and the incumbent's gap is one of them: the expensive two below then run
            // only for a wall that would actually win, which is what keeps this off the frame budget.
            if (gap < 1e-6 || gap > CheckpointReachDu || gap >= bestGap)
            {
                continue;
            }
            if (!ClearOfEveryOpening(px, py, in floor))
            {
                continue;
            }

            double sx = px + (dx / gap * LaneFromWallDu);
            double sy = py + (dy / gap * LaneFromWallDu);
            if (SurfaceCollision.Blocked(sx, sy, SignInClearDu, blockers))
            {
                continue;
            }

            bestGap = gap;
            best = new Checkpoint(number, px, py, sx, sy, stop.What);
        }

        return best;
    }

    /// <summary>#831 · Is this spot on the wall far enough from every hole in it? Doorways and the doors that
    /// never open alike — a locked leaf already carries a sign, and two stencils on one door is a wall
    /// nobody can read.</summary>
    private static bool ClearOfEveryOpening(
        double px, double py, in UndergroundComplex.FloorPlan floor)
    {
        foreach (SurfaceLayout.Doorway d in floor.Doorways ?? [])
        {
            if (SurfaceCollision.DistanceToSegment(px, py, d.X1, d.Y1, d.X2, d.Y2) < ClearOfOpeningDu)
            {
                return false;
            }
        }
        foreach (UndergroundComplex.LockedDoor l in floor.Locked ?? [])
        {
            if (SurfaceCollision.DistanceToSegment(px, py, l.X1, l.Y1, l.X2, l.Y2) < ClearOfOpeningDu)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>The point on a segment nearest a spot. The one bit of arithmetic a placer is allowed to do,
    /// because it is about a line somebody else drew.</summary>
    private static (double X, double Y) NearestOnSegment(
        double px, double py, double x1, double y1, double x2, double y2)
    {
        double vx = x2 - x1, vy = y2 - y1;
        double len2 = (vx * vx) + (vy * vy);
        if (len2 < 1e-12)
        {
            return (x1, y1);
        }
        double t = Math.Clamp((((px - x1) * vx) + ((py - y1) * vy)) / len2, 0.0, 1.0);
        return (x1 + (t * vx), y1 + (t * vy));
    }

    // ── #831 · THE COVER ACT: WHAT A MADE TAIL DOES INSTEAD OF FREEZING BARE ──────────────────────────
    //
    // Owner's law, generalised from the same sighting: "A MADE tail performs a COVER ACT instead of freezing
    // bare: turns to the nearest wall fixture, checks a plate, reads a docket — same hold, same honest Vx=0,
    // but the picture says 'man with business' not 'statue'."
    //
    // FootTail.MustHold is untouched and stays untouched: a made tail still stops dead where the law says he
    // stops, and he still drops off the motion fan honestly while he does it. What changes is the PICTURE —
    // he turns to the nearest station and reads it, and if there is nothing near he takes the few du to one
    // first. Nothing is said, no card goes up, and the tell is a thing the player watches rather than a thing
    // the game announces: a man signing a plate he signed ninety seconds ago is the gumshoe's confirmation,
    // and it costs this file no prose at all.

    /// <summary>#831 · SOMETHING ON A WALL A MAN CAN PLAUSIBLY BE READING. A watchclock station, the
    /// department painted on a door that never opens, a framed poster nobody has taken down since
    /// 1970-something. Published as one flat list so the cover act asks ONE question of the floor rather than
    /// growing an opinion per fixture kind.</summary>
    /// <param name="X">Where the thing is, on the wall it is bolted to.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Point">The checkpoint's own number when it is one, and 0 when it is not — which is what
    /// makes the DOUBLE SIGN-IN readable at all.</param>
    public readonly record struct WallThing(double X, double Y, int Point);

    /// <summary>#831 · How close he gets to the thing he has decided to read. Arm's length and a step, the
    /// same distance <see cref="CardReachDu"/> says two people talk at.</summary>
    public const double CoverStandDu = 2.0;

    /// <summary>#831 · …and how far he will DRIFT to find one. Owner: <i>"Mid-corridor with nothing near: he
    /// does not hold there — he drifts the few du to the nearest fixture first, then holds."</i> A few du and
    /// no more: a man who walked half a corridor to find a plate would be walking on past you, which is the
    /// one thing the hold exists to stop.</summary>
    public static double CoverDriftDu => MarkerSightDu / 2.0;

    /// <summary>#831 · The longest he will spend getting to it — exactly the time the drift's own distance
    /// takes to walk, so the bound is the same fact as the reach rather than a second number that could drift
    /// from it. Past it he reads whatever he chose from where he has got to, which is what a person does when
    /// a bench or a rail turns out to be in the way: he does not keep shuffling at it forever, and a body
    /// still shuffling is a body the motion fan would honestly call a mover.</summary>
    public static double CoverDriftSeconds => CoverDriftDu / WalkSpeed;

    /// <summary>#831 · Everything on this floor's walls a held man could be reading — the stations, the shut
    /// doors' own signs, the posters, the plated fittings in every room and the bins somebody has to empty.
    /// One call, in the renderer's own idiom: it is a fact about the floor rather than a search a stepper
    /// does per frame.
    ///
    /// <para>It is deliberately WIDE. The owner's clause is <i>"turns to the nearest wall fixture, checks a
    /// plate, reads a docket"</i> — the point is that a facility is covered in things a man with a clipboard
    /// has business with, and a list that only knew about the round's own stations would leave a held tail
    /// standing bare halfway down every leg, which is the exact picture this issue exists to end.</para></summary>
    public static IReadOnlyList<WallThing> ReadablesOn(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        var things = new List<WallThing>();
        foreach (Checkpoint p in CheckpointsOn(in floor, in field))
        {
            things.Add(new WallThing(p.X, p.Y, p.Number));
        }
        foreach (UndergroundComplex.LockedDoor l in floor.Locked ?? [])
        {
            things.Add(new WallThing((l.X1 + l.X2) / 2.0, (l.Y1 + l.Y2) / 2.0, 0));
        }
        foreach (LabPosters.Poster poster in floor.TheWalls)
        {
            things.Add(new WallThing(poster.X, poster.Y, 0));
        }
        foreach (SurfaceLayout.Doorway d in floor.Doorways ?? [])
        {
            things.Add(new WallThing((d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0, 0));
        }
        foreach (SurfaceLayout.Landmark m in floor.Labels ?? [])
        {
            things.Add(new WallThing(m.X, m.Y, 0));
        }
        foreach (RipAndBin.Bin bin in floor.TheBins)
        {
            things.Add(new WallThing(bin.X, bin.Y, 0));
        }
        foreach (UndergroundComplex.Room room in floor.TheRooms)
        {
            foreach (RingOffice.Fixture fitting in room.Furniture)
            {
                if (fitting.Plate.Length > 0)
                {
                    things.Add(new WallThing(fitting.X, fitting.Y, 0));
                }
            }
        }
        return things;
    }

    /// <summary>#831 · Which of them a held man makes his business, or null when there is nothing he can both
    /// see and reach within <see cref="CoverDriftDu"/> — in which case the hold stays honestly bare and the
    /// audit counts it.
    ///
    /// <para><b>He has to be able to SEE it.</b> A plate through a wall is not a thing a man was about to
    /// read, and a cover act that set off toward one would be a body sliding along shotcrete for ten seconds.
    /// The look is <see cref="SurfaceCollision.HasLineOfSight"/> — the game's one wall law — taken to the
    /// square he would end up standing on rather than to the plate itself, because the plate is bolted to a
    /// wall and a sightline that ends inside one answers nothing.</para></summary>
    public static WallThing? CoverFor(
        double x, double y,
        IReadOnlyList<WallThing>? things,
        IReadOnlyList<SurfaceCollision.Segment>? sight)
    {
        if (things is null)
        {
            return null;
        }

        WallThing? best = null;
        double bestGap = CoverDriftDu;
        foreach (WallThing t in things)
        {
            double dx = t.X - x, dy = t.Y - y;
            double gap = Math.Sqrt((dx * dx) + (dy * dy));
            if (gap >= bestGap || gap < 1e-9)
            {
                continue;
            }

            double reach = Math.Max(0, gap - CoverStandDu);
            if (!SurfaceCollision.HasLineOfSight(
                    x, y, x + (dx / gap * reach), y + (dy / gap * reach), sight))
            {
                continue;
            }

            bestGap = gap;
            best = t;
        }
        return best;
    }

    /// <summary>#831 · Is he there yet, or is he still taking the few du to it?</summary>
    public static bool AtTheCover(double x, double y, in WallThing thing)
    {
        double dx = thing.X - x, dy = thing.Y - y;
        return (dx * dx) + (dy * dy) <= CoverStandDu * CoverStandDu;
    }

    /// <summary>#831 · THE TELL. Is this man signing a station he already signed on this round? The gumshoe's
    /// confirmation that the tail is made, stated once here so the sim and any audit of it read one sentence
    /// — and deliberately not said out loud anywhere: the double sign-in is watched, never narrated.</summary>
    public static bool DoubleSignIn(int signingNow, int alreadySigned) =>
        signingNow > 0 && signingNow == alreadySigned;

    // ── #831 · THE LANE ───────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "they should respect right side traffic, and not walk in the middle of the corridor."
    //
    // Three things it buys, and the owner named all three: it reads as PROCEDURE, in the same register as
    // the checkpoints; two guards passing each other pass cleanly on opposite sides instead of meeting in
    // the middle; and a walker who holds the centre of a corridor is — visibly, learnably — somebody who does
    // not know the house rules. (The captain's own lane-keeping as a badge tell is design headroom and is
    // deliberately not built here.)
    //
    // IT OFFSETS THE WAYPOINT LINE AND NEVER THE COLLISION. The A* is the same A* over the same field; what
    // changes is where along the width of a corridor the walked line runs. At a corner, a doorway or a rib
    // mouth the offset is simply given up and the round takes the middle — a lane is a preference, and a
    // preference that could wedge a man against a jamb would be a wall.

    /// <summary>#831 · Over how many waypoints the lane is EASED IN and out. A route is a lattice path at
    /// half a deck unit a cell, and an offset that appeared all at once would move a body two du sideways in
    /// one step — a diagonal lunge across a corridor that the slide refuses at every jamb it meets, which is
    /// a snag cycle wearing a feature's clothes. Four cells taper the two du into steps no longer than the
    /// walk's own, so a man drifts onto his side of the corridor the way a person does.</summary>
    public const int LaneEaseCells = 4;

    /// <summary>
    /// #831 · THE ROUND, PUT ON ITS OWN SIDE OF THE CORRIDOR.
    ///
    /// <para>Every waypoint in the middle of a STRAIGHT run is shifted to the right hand of travel by
    /// <see cref="LaneOffsetDu"/>; every waypoint at a turn keeps the centre line, and so do both ends. The
    /// shift is EASED in over <see cref="LaneEaseCells"/> — see that constant — and a shift a body would not
    /// fit on is stepped back down until it does, so the offset is never allowed to put a man inside
    /// anything and never asks him to lunge.</para>
    ///
    /// <para><b>Right of travel, in this game's own frame.</b> +y is up on the deck (the renderer flips it
    /// for the screen), so the right hand of a heading (ax, ay) is (ay, −ax): a man walking EAST walks on the
    /// SOUTH side of the corridor, and the man coming back walks on the north side. That is the sentence a
    /// test asserts, rather than this formula copied into it.</para>
    ///
    /// <para>Plan-time and not per-frame: it runs once on the route the leg was planned with, over a list
    /// that is already in hand. Lab 45's frame budget is untouched.</para>
    /// </summary>
    public static IReadOnlyList<DeckReachability.Point> KeepRight(
        IReadOnlyList<DeckReachability.Point>? route,
        IReadOnlyList<SurfaceCollision.Segment>? walls,
        double radius)
    {
        int n = route?.Count ?? 0;
        if (route is null || n < 3)
        {
            return route ?? [];
        }

        // ── WHICH WAYPOINTS ARE ON A STRAIGHT RUN AT ALL, and which way is right there.
        var rightX = new double[n];
        var rightY = new double[n];
        var straight = new bool[n];
        for (int i = 1; i < n - 1; i++)
        {
            double bx = route[i].X - route[i - 1].X, by = route[i].Y - route[i - 1].Y;
            double ax = route[i + 1].X - route[i].X, ay = route[i + 1].Y - route[i].Y;
            double bl = Math.Sqrt((bx * bx) + (by * by)), al = Math.Sqrt((ax * ax) + (ay * ay));
            if (bl < 1e-9 || al < 1e-9)
            {
                continue;
            }
            bx /= bl; by /= bl; ax /= al; ay /= al;

            // A CORNER GIVES THE LANE UP. This is the doorway clause and the mouth clause both — the route
            // turns at exactly those places — and it is what keeps the offset from ever being asked to hold
            // through a 6.4 du leaf.
            if ((bx * ax) + (by * ay) < LaneStraightEnough)
            {
                continue;
            }
            straight[i] = true;
            rightX[i] = ay;
            rightY[i] = -ax;
        }

        // ── HOW FAR EACH ONE IS FROM THE NEAREST TURN, in cells, so the shift can be eased.
        var ease = new int[n];
        int run = 0;
        for (int i = 0; i < n; i++)
        {
            run = straight[i] ? run + 1 : 0;
            ease[i] = run;
        }
        run = 0;
        for (int i = n - 1; i >= 0; i--)
        {
            run = straight[i] ? run + 1 : 0;
            ease[i] = Math.Min(ease[i], run);
        }

        var laned = new List<DeckReachability.Point>(n) { route[0] };
        for (int i = 1; i < n - 1; i++)
        {
            double scale = straight[i] ? Math.Min(1.0, ease[i] / (double)LaneEaseCells) : 0.0;

            // …and the GROUND may refuse any of it. Stepped back down rather than dropped, so a waypoint
            // beside something bulky rejoins its neighbours instead of jumping to the middle and back.
            while (scale > 0)
            {
                double tx = route[i].X + (rightX[i] * LaneOffsetDu * scale);
                double ty = route[i].Y + (rightY[i] * LaneOffsetDu * scale);
                if (!SurfaceCollision.Blocked(tx, ty, radius, walls))
                {
                    break;
                }
                scale -= 1.0 / LaneEaseCells;
            }

            laned.Add(scale > 0
                ? new DeckReachability.Point(
                    route[i].X + (rightX[i] * LaneOffsetDu * scale),
                    route[i].Y + (rightY[i] * LaneOffsetDu * scale))
                : route[i]);
        }
        laned.Add(route[^1]);
        return laned;
    }

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
    ///
    /// <h3>#831 · AND EVERY STOP SNAPS TO ITS CHECKPOINT</h3>
    ///
    /// <para>Owner: <i>"why would it just stand there if there is no inspection point etc"</i> → <i>"they
    /// actually in real life like have these check points they electronically sign on rounds to prove they
    /// did their round."</i> So the coordinates above are where the round WANTS to be, and the coordinates it
    /// walks to are the square the nearest watchclock station is signed from (<see cref="PointFor"/>) — a
    /// pace and a half off a wall, facing a plate. A stop that finds no wall within
    /// <see cref="CheckpointReachDu"/> keeps its own place and says so by carrying a null
    /// <see cref="Stop.Point"/>; the audit counts those, because a stop nobody can explain is the thing this
    /// whole issue is about.</para>
    /// </summary>
    /// <param name="floor">The floor, as Core built it.</param>
    /// <param name="field">The site's envelope — the shaft's position comes from it.</param>
    public static IReadOnlyList<Stop> Circuit(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        IReadOnlyList<Stop> wanted = WantedCircuit(floor, field);
        List<SurfaceCollision.Segment> blockers = Blockers(in floor);

        var snapped = new List<Stop>(wanted.Count);
        for (int i = 0; i < wanted.Count; i++)
        {
            Stop stop = wanted[i];
            snapped.Add(PointFor(in stop, i + 1, in floor, blockers) is { } at
                ? stop with { X = at.StandX, Y = at.StandY, Point = at }
                : stop);
        }
        return snapped;
    }

    /// <summary>#831 · The circuit as the floor plan alone decides it, BEFORE the stations move it. Split out
    /// so the numbering of the stations is a fact about the PLACE — station 3 is station 3 on every watch —
    /// and so the snap has exactly one caller.</summary>
    private static IReadOnlyList<Stop> WantedCircuit(
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

    /// <summary>#858 · How much of the NEXT leg's A* a standing guard walks per frame
    /// (<see cref="AutoWalk.Planner"/>), in lattice cells.
    ///
    /// <para>Lab 45 measured the plan the round asks for at a median 1.6–2.2 ms and a worst 6.4 ms — 38.6%
    /// of a 60 fps frame — spent whole on the frame he leaves a stop, which is the one number in that lab
    /// that can miss a frame. He is standing for <see cref="StandSeconds"/> either way; this is the rate
    /// that gets the same work done during it.</para>
    ///
    /// <para><b>Derived, not tuned.</b> The stand is 5 s ≈ 300 frames, and the biggest lattice
    /// <see cref="LatticeFor"/> poses on the floors this generator builds is 29,002 cells (Lab 45 §C). At
    /// 128 a frame the whole of that lattice is walked in ~227 frames — inside the stand with a quarter of
    /// it to spare — while one frame's share of that worst 6.4 ms plan is about a fiftieth of it. Being a
    /// CELL budget rather than a millisecond one is the point: it means the same thing in WASM, where the
    /// clock this was measured on does not.</para></summary>
    public const int PlanCellsAFrame = 128;

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
    ///
    /// <para>#836 · <b>AND IT READS WHAT WAS HANDED OVER.</b> Owner: <i>"I think I should be able to pick the
    /// badge I show the guard... like Fletch"</i>. This used to take the whole satchel and walk it — pass
    /// first, then anybody's pass, then the chit — which meant the answer was always the BEST paper in the
    /// wallet. A captain who chose the bad one was quietly rescued by the sim, and a wallet with four names
    /// in it would have had no game in it at all.</para>
    ///
    /// <para>So it takes ONE paper, chosen during #833's approach (<see cref="WalletChoice"/>), and the
    /// judgement itself is <see cref="WalletChoice.WhatHappens"/> — one ladder, read by this card and by the
    /// line the book files about it, so the sentence and the paper trail cannot disagree.</para>
    ///
    /// <para>The 2026-08-08 ruling is untouched: there is no TRY verb here, nothing is pressed, and the read
    /// is as automatic as it ever was. What moved is WHEN the captain decided, not whether the man asks.</para>
    /// </summary>
    /// <param name="bodyId">The site whose floor you are standing on.</param>
    /// <param name="plate">Who stopped you (<see cref="PlateOf"/>).</param>
    /// <param name="shown">The paper that went into his hand, or null when nothing did — an empty wallet, or
    /// a captain who had nothing a palm is for.</param>
    public static Read TheGuardReads(string bodyId, string plate, Satchel.Item? shown)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(plate);

        string card = ChallengeCard(plate);

        switch (WalletChoice.WhatHappens(bodyId, shown))
        {
            case WalletChoice.Outcome.Worked:
                return new(true, SatisfiedLine, ChallengeLabel, card);

            // Somebody else's building. Named, because a refusal that reads the site code out loud is worth
            // carrying (#679) — and because a captain who has worked two sites should learn that the second
            // pass is still worth keeping.
            case WalletChoice.Outcome.WrongSite when shown is { } wrong
                                                     && SiteOfBadge(wrong.Id) is { Length: > 0 } site:
                return new(false, WrongSiteLine(site), ChallengeLabel, card, EscortLine);

            // The cage chit. It is a real paper and it is real cover — for the cage. He says so the way you
            // would say a platform number, which is the most bureaucratic refusal available.
            case WalletChoice.Outcome.WrongPaper:
                return new(false, WrongPaperLine, ChallengeLabel, card, EscortLine);

            default:
                return new(false, NothingLine, ChallengeLabel, card, EscortLine);
        }
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
