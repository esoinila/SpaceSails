using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · CARVING THE REFUGES AND WHAT THEY SAY (#608) — <c>CarveRefuges</c>, the way in, and every plate,
/// glyph and caption a refuge wears.
///
/// <para>Split out of <c>UndergroundComplex.Air.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered. Every field here is a <c>const</c>; <c>CarveRefuges</c> is one
/// 169-line method and travels whole.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>The refuges on a floor, taken out of the rooms it had already built.
    ///
    /// <para>A refuge IS one of the floor's rooms — three poured walls and a doorway cut in its corridor
    /// face — and that is deliberate rather than lazy. A room is already audited walkable from the lift
    /// (13.1), already has a door the captain can find, and already sits down a rib rather than on the
    /// spine. Inventing a second kind of chamber would be a second thing to keep reachable, and a refuge you
    /// cannot walk to is a refuge that does not exist.</para>
    ///
    /// <para>It stops being a haul room when it becomes one: a pressure vessel somebody maintained is not a
    /// drawer to turn over, and the air is what it pays.</para>
    ///
    /// <para>#619 · <b>AND ON ONE FLOOR OF ONE SITE IN FOUR, A SECOND ONE THAT FAILED — never instead of the
    /// first.</b> The owner's issue is exact about it: <i>"a SECOND refuge, on one floor, that failed … never
    /// the only one on its floor, so it can never kill anybody who trusted the instrument."</i> So the working
    /// refuge is carved FIRST, out of the same pool, by the same roll it has always used — nothing about a
    /// floor that has no story on it changes by a byte — and the failed one is taken afterwards, out of what
    /// is left. A captain who walks to the mark the fan paints as air finds air; the other room is a room
    /// they can choose to walk to, and it is welded shut.</para>
    ///
    /// <para>It is welded by the building's own lock seam (<see cref="LockedDoor"/>): every way out of that
    /// chamber becomes a leaf that never opens with a real wall behind it. That is the difference between a
    /// refuge that is out of service and a refuge that is merely empty, and it is the reason the air
    /// machinery does not have to be told anything — the room never joins the list of places that breathe,
    /// and the floor plan agrees with it.</para></summary>
    private static List<Refuge> CarveRefuges(
        string bodyId, int level, List<Room> rooms, List<LockedDoor> locked,
        in SurfaceLayout.Field field)
    {
        var refuges = new List<Refuge>();
        if (HoldsPressure(bodyId, level) || rooms.Count == 0)
        {
            return refuges;   // a pressurised floor IS the refuge — and every gallery is one (#677)
        }

        // #592 · The one room that may never be taken. On a site with a band nobody listed, room 0 of the
        // last listed floor is the card that reaches it (KeyRoomFor) — designated exactly because a rolled
        // index would sometimes miss and strand the whole feature forever. Turning it into a refuge would
        // do the same thing by a different route.
        int reserved = KeyRoomFor(bodyId) is { } key && key.Level == level ? key.RoomIndex : -1;

        var faraway = new List<int>();
        var anywhere = new List<int>();
        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == reserved)
            {
                continue;
            }
            anywhere.Add(i);

            // #801 · A DETOUR FROM EVERY CAR, not from the cage. This measured one shaft, and the day the
            // building grew a second one at the other end of the corridor it went on passing while the
            // sentence it exists to protect died: a third of the refuges in the game were four steps from
            // the goods car. The guard found it (332 of 1130 floors); the fix is that the carve asks the
            // same list the guard does.
            bool far = true;
            foreach (Shaft car in ShaftsOn(field))
            {
                double dx = rooms[i].X - car.X, dy = rooms[i].Y - car.Y;
                far &= (dx * dx) + (dy * dy) >= MinRefugeDetourDu * MinRefugeDetourDu;
            }
            if (far)
            {
                faraway.Add(i);
            }
        }

        // The detour is the design, so it is preferred — but it is NOT allowed to cost the guarantee. On a
        // floor whose rooms all happen to crowd the shaft, a near refuge beats no refuge, every time: the
        // owner's line is "at least one ... for pure safety", and a safety regulation that a seed can talk
        // out of is not one.
        // #801 · …and when NOTHING qualifies, the fallback takes the FURTHEST room rather than a rolled one.
        // With two cars at opposite ends of the spine there are floors whose every chamber is inside the
        // detour of one car or the other, and on those the old fallback rolled a room at random — which on
        // the sweep put a refuge twenty-nine du from a car on floors that had a sixty-du one going spare.
        // A safety regulation a seed can talk out of is not one, and neither is one it can shrug at.
        List<int> pool = faraway;
        if (pool.Count == 0 && anywhere.Count > 0)
        {
            int best = anywhere[0];
            double bestNear = -1;
            foreach (int i in anywhere)
            {
                double near = double.MaxValue;
                foreach (Shaft car in ShaftsOn(field))
                {
                    double dx = rooms[i].X - car.X, dy = rooms[i].Y - car.Y;
                    near = Math.Min(near, (dx * dx) + (dy * dy));
                }
                if (near > bestNear)
                {
                    (best, bestNear) = (i, near);
                }
            }
            pool = [best];
        }
        if (pool.Count == 0)
        {
            return refuges;
        }

        int pick = pool[DiceRule.Roll(DiceRule.Seed($"hive:refuge:{bodyId}:{level}"), pool.Count).Face - 1];
        Room chosen = rooms[pick];
        rooms.RemoveAt(pick);
        (double doorX, double doorY) = WayIn(chosen);
        refuges.Add(new Refuge(
            chosen.X, chosen.Y, RefugeSign(bodyId, level, 0),
            // The seal is decided by the FLOOR, not by the carve, and asked here rather than worked out
            // again: the panel, the card, the tracker and the suit all read StateOfTheRefugeOn, and a room
            // that carried a second opinion about its own door is this repo's oldest and dearest bug.
            //
            // #619 · …and what that answer can no longer be is FAILED. The floor's own refuge holds or it is
            // dry; the room that failed is the extra one below, and it is the whole reason this method can
            // still promise that the mark a captain walks a tank toward is air.
            StateOfTheRefugeOn(bodyId, level) ?? RefugeState.Holding,
            doorX, doorY));

        // ── #619 · AND THE ONE THAT FAILED, WHICH IS AN EXTRA ROOM AND NEVER THE FLOOR'S OWN ────────────
        //
        // Second, out of what the first one left, and only where the site's story happened
        // (FailedRefugeFloorOf — one site in four, one floor of it). Taken from the same pool by the same
        // preference, so it is a detour like every other refuge in the building rather than a prop set down
        // beside the lift for the captain to trip over.
        //
        // The pool can be empty here and that is allowed: a floor the generator built with exactly one
        // takeable chamber keeps it as the working refuge and simply has no story on it. The law is
        // "never the only refuge on its floor", and the way to keep a law like that is to let the beat go
        // rather than to let the safety regulation go.
        if (!RefugeThatFailedIsOn(bodyId, level))
        {
            return refuges;
        }

        var left = new List<int>();
        foreach (int i in faraway.Count > 0 ? faraway : anywhere)
        {
            // The indices were taken before the first refuge came out of the list, so they are re-walked
            // against the list as it stands now rather than arithmetically shifted — an index adjusted by
            // hand is the shape of the bug KeyRoomFor and CarveRefuges were both written to avoid.
            int after = i < pick ? i : i - 1;
            if (i != pick && after >= 0 && after < rooms.Count)
            {
                left.Add(after);
            }
        }
        if (left.Count == 0)
        {
            return refuges;
        }

        int second = left[
            DiceRule.Roll(DiceRule.Seed($"hive:refuge-failed:{bodyId}:{level}"), left.Count).Face - 1];
        Room welded = rooms[second];
        rooms.RemoveAt(second);
        (double wx, double wy) = WayIn(welded);
        refuges.Add(new Refuge(
            welded.X, welded.Y, RefugeSign(bodyId, level, 1), RefugeState.Failed, wx, wy));

        // THE WELD ITSELF, in the building's own grammar. A LockedDoor is a leaf that never opens with a
        // real wall behind it, and that is exactly what a door welded from the inside is — so the air
        // machinery, the walkers, the Reevers, the A* audit and the renderer all learn about it from the one
        // list they already read, and none of them has to be told that this room is special.
        foreach (SurfaceLayout.Doorway way in welded.Ways)
        {
            locked.Add(new(way.X1, way.Y1, way.X2, way.Y2, RefugeFailedGlyph));
        }
        return refuges;
    }

    /// <summary>#619 · How far OUT of the room the welded refuge's press stands, in deck units.
    ///
    /// <para><b>It has to be out at all, and that is a bug this lane paid for.</b> The press first sat on
    /// the doorway's own midpoint — which is exactly where the weld goes — so on the two scenario floors
    /// that carry one, the A* audit found a console inside solid wall and a card that could never be read.
    /// The doorway is a WALL now; the captain stands in the corridor in front of it.</para>
    ///
    /// <para>Two du clears the avatar (<c>DeckPlan.AvatarRadius</c> = 0.7) with room to spare, stays well
    /// inside the interact reach (3.0), and stays inside the corridor's own half-width
    /// (<see cref="CorridorHalf"/> = 3.5) — so the spot is in the rib a captain is already walking down and
    /// never through it into whatever stands on the far side.</para></summary>
    public const double WeldedRefugeStandOffDu = 2.0;

    /// <summary>#619 · Where a captain stands to read a chamber's first way out: the doorway's midpoint,
    /// stepped <see cref="WeldedRefugeStandOffDu"/> back out of the room along the line from its centre.
    ///
    /// <para>On the welded refuge that is the only place the press CAN be, because the doorway itself is a
    /// wall. On a refuge whose door cycles nothing reads it — and it is computed all the same rather than
    /// left at zero, because a field that is a lie on most rows is a field the next hand reads off the wrong
    /// row.</para>
    ///
    /// <para>Falls back to the room's own centre for a chamber with no recorded doorway, which the generator
    /// does not produce and which is not worth a second kind of answer.</para></summary>
    private static (double X, double Y) WayIn(in Room room)
    {
        if (room.Ways.Count == 0)
        {
            return (room.X, room.Y);
        }

        double mx = (room.Ways[0].X1 + room.Ways[0].X2) / 2;
        double my = (room.Ways[0].Y1 + room.Ways[0].Y2) / 2;
        double dx = mx - room.X, dy = my - room.Y;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        return len < 1e-9
            ? (mx, my)
            : (mx + (dx / len * WeldedRefugeStandOffDu), my + (dy / len * WeldedRefugeStandOffDu));
    }

    /// <summary>What the console inside is called.</summary>
    public const string RefugeTankLabel = "🫁 REFUGE RACK";

    /// <summary>What the plate over the door says at signage size — short enough to read at a run, because
    /// that is how it will be read.
    ///
    /// <para>It names the ROOM, not the floor, and that word is load-bearing (#612). The plate by the lift
    /// is simultaneously shouting NO ATMOSPHERE about the level; a sign forty du away reading only AIR
    /// would be a second instrument appearing to contradict the first, which is the one thing #612 says is
    /// worse than saying nothing. <c>REFUGE ·</c> makes the scope of the claim part of the claim.</para></summary>
    public const string RefugeGlyph = "🫁 REFUGE · AIR";

    /// <summary>#619 · The plate on the one room in the game that is out of service. Authored canon
    /// (2026-09-20), verbatim: <c>REFUGE — OUT OF SERVICE — REPORTED</c>, behind the family's own glyph that
    /// every other string in this file already wears.
    ///
    /// <para>It is the INSPECTORATE'S VOICE and it is entirely functional — the register of a form, not of a
    /// story. Three flat words a clerk would use, and the third of them is the only one doing any work:
    /// <i>REPORTED</i> says a notice went somewhere, and says nothing whatever about what was reported, who
    /// read it, or whether anybody came. Canon §13.8 at the one door in the building where the temptation to
    /// explain is worst.</para>
    ///
    /// <para>It replaces the old failed plate, which was the stencil with the word AIR quietly absent
    /// (<c>PRESSURE REFUGE</c>). That was a good tell for a seal that had perished and a bad one for a room
    /// somebody shut on purpose: it read as neglect, and #619's whole point is that this did not fail from
    /// age. It is also the sign on the WELD — <see cref="CarveRefuges"/> hands it to every
    /// <see cref="LockedDoor"/> it lays across that chamber's ways — so the plate over the door and the plate
    /// on the door are one string and cannot come to two accounts of one room.</para></summary>
    public const string RefugeFailedGlyph = "🫁 REFUGE — OUT OF SERVICE — REPORTED";

    /// <summary>#619 · Is this lock the weld on the refuge that failed? Asked by the renderer, which lets
    /// that door keep its wall and its leaf and takes its CONSOLE for itself, and by the hasp rule, which
    /// refuses to let a sentry shoot it. One predicate, so neither of them re-types the plate.</summary>
    public static bool IsTheWeldedRefugePlate(string sign) =>
        string.Equals(sign, RefugeFailedGlyph, StringComparison.Ordinal);

    /// <summary>#619 · What the instrument column says about the grey ring while one is on the fan.
    /// Authored canon (2026-09-20), verbatim — <c>refuge · dark</c> — behind the refuge family's glyph.
    ///
    /// <para>Lower case and two words, because it is a LEGEND and not an affordance: every other line in that
    /// column teaches a key, and this one teaches an ink. <i>dark</i> is the word the instrument would use
    /// about a lamp that is not lit, which is what the captain is looking at, and it promises nothing at
    /// all.</para></summary>
    public const string RefugeDarkCaption = "🫁 refuge · dark";

    /// <summary>#938 · THE PLATE ON A ROOM THAT HOLDS AND HAS NOTHING IN IT. Authored for the one
    /// line-needed marker #608 shipped with (2026-09-03), in the stencil grammar the other two speak.
    ///
    /// <para>The bug it closes: <see cref="RefugeGlyphFor"/> read the plate off a two-way test, so
    /// <see cref="RefugeState.Empty"/> — thirty-nine per cent of them — wore <see cref="RefugeGlyph"/> and
    /// went on saying AIR at range. That is the #612 fault at the worst possible door: the one word a
    /// captain crosses a dead floor for, printed over a rack whose fill line is empty and whose valve tag is
    /// dated years ago. The room is not a lie — it holds, and shelter is worth the walk — but AIR is.</para>
    ///
    /// <para>DRY is the whole correction, and it is one word because the plate is read at a run. It keeps
    /// <c>REFUGE ·</c> so the scope of the claim stays part of the claim; it does not become a warning,
    /// because the room still works; and it is a word about the RACK, which is the only thing the decades
    /// took. A captain who has read AIR on one floor and DRY on this one knows the difference before the
    /// walk, which is the same service the failed plate does by dropping the word altogether.</para></summary>
    public const string RefugeDryGlyph = "🫁 REFUGE · DRY";

    /// <summary>Which plate a refuge in this state wears. One place, so the deck plan and the tracker cannot
    /// come to disagree about what the room claims — and now three plates for three states, because a
    /// two-way test could only ever tell the captain which of them the room was NOT.</summary>
    public static string RefugeGlyphFor(RefugeState state) => state switch
    {
        RefugeState.Failed => RefugeFailedGlyph,
        RefugeState.Empty => RefugeDryGlyph,
        _ => RefugeGlyph,
    };

    /// <summary>#608 · What the lift panel prints on a floor whose plan carries a refuge. It says a refuge is
    /// THERE and never what state it is in — the plan is a drawing made when the building was new, and no
    /// drawing knows which compressors are still turning. Finding that out is the walk.</summary>
    public const string RefugeRowTag = "REFUGE";
}
