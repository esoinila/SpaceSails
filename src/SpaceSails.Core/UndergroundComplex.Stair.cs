using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #719 · A SECOND WAY OUT, AND IT SHIPS BEFORE ANYTHING IS ALLOWED TO STOP THE CAR ─────────────────
    //
    // Owner, 2026-08-05: "I wonder if there should be more than one way out of the lab, what if the elevator
    // needs maintenance break :-)" / "yeah let's make that because just stopping the elevator by remote of
    // radio message would stop all escape way too easy :-d" / "going up would use more air".
    //
    // THE ORDERING IS THE WHOLE ISSUE. Until this file existed, ShaftAt was the single way home — the goods
    // car runs its band and does not climb out (#801), and Shaft.ReachesTheSurface said so — so one radio
    // call would have ended every escape in the game. A maintenance break added before a second exit is not
    // a horror beat, it is a softlock with atmosphere. The stair ships first; the break may be designed once
    // this is proven.
    //
    // WHY THE BUILDING ALREADY HAS ONE. The refuges exist because "somebody with a clipboard made the owners
    // pay for somewhere to go when a tank ran short" — HiveRefuge's own words. A building this determined to
    // look legitimate satisfies the same paperwork about a SECOND MEANS OF ESCAPE, and so it is the same
    // joke as the canteen, the washrooms and the en-suites: the pretence of legitimacy is what saves your
    // life. Canon holds absolutely (§13.8): a fire regulation explains nothing about the Old Ones, and this
    // one never tries.
    //
    // WHAT IT IS NOT. It is not the executive lift (that hangs off a principal apartment, is on no panel and
    // costs your cover — a beat, and a different lane), and it is not the maintenance break.

    /// <summary>#719 · What is painted at the stair's door. <b>The only new string this feature has</b>, and
    /// it is a plate rather than a sentence for the reason every other plate down here is one: the building
    /// labels its own fabric and never narrates it.
    ///
    /// <para>It says STAIR and it does not say LIFT or CAR, which is a rule and not a preference — a stair
    /// wearing the cars' vocabulary would be a sign reporting a machine that is not there, which is this
    /// project's house bug class printed on a wall (see <see cref="CageSign"/>, <see cref="ServiceCarSign"/>
    /// and #802's surface row).</para></summary>
    public const string StairSign = "\U0001FA9C STAIR";

    /// <summary>#719 · How deep the stair's own pocket runs off the spine — <b>the lift alcove's five du</b>,
    /// the same number <see cref="SpecimenRecessDu"/> takes and for the same stated reason: a pocket the
    /// building already knows how to cut, and not a room.</summary>
    public const double StairRecessDu = SpecimenRecessDu;

    /// <summary>
    /// #719 · <b>DOES THIS FLOOR HAVE A STAIR DOOR?</b> — every floor the building ADMITS TO, and no other.
    ///
    /// <para>From the listed bottom (<see cref="DepthOf"/>) up to B1. That boundary is the fiction and the
    /// law at once: the second means of escape is a thing an inspectorate signed off, and nobody files a
    /// means-of-escape drawing for a working they never declared. So the band nobody listed (#592) and the
    /// halls nobody dug (#677) have no stair — the same silence their own lift row keeps.</para>
    ///
    /// <para>It is also what keeps §13.5 whole. The stair spans bands, and the gates it passes are gates
    /// about DEPTH: a captain may not buy their way further down without the paper. Nothing here sells
    /// depth, because nothing here opens onto a floor — see <see cref="StairShaftAt"/>'s door rule.</para>
    /// </summary>
    public static bool HasStairOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return level < 0 && level >= DepthOf(bodyId);
    }

    /// <summary>
    /// #719 · <b>WHERE THE STAIR STANDS</b>, decided from the field alone so it is in the same place on every
    /// floor of every site — the cage's own law (<see cref="ShaftAt"/>), for the cage's own reason: a way out
    /// a captain has to look for twice is a way out they will not reach in the dark.
    ///
    /// <para><b>A blind end of the spine</b>, by the very finder the goods car and the preserved recess are
    /// placed with: the stretch past the outermost cross corridor, which is the one length of corridor in the
    /// building no chamber can ever reach. Three numbers, measured the same way in all three placers.</para>
    ///
    /// <para><b>The end NOTHING ELSE IS IN, and then the end furthest from the cage.</b> Two exclusions and
    /// one preference:</para>
    /// <list type="bullet">
    /// <item>never the end the goods car stands in — two ways out at one end of a corridor is one way out
    /// with a spare, and the whole point of a second means of escape is that a single posted guard, a single
    /// collapse or a single radio call cannot take both;</item>
    /// <item>never the end the pocket is cut into (<see cref="SpecimenRecessAt"/>, which is also where a stop
    /// order's seal stands — <see cref="StopSealRecessAt"/> delegates to it). That pocket is on the upper
    /// face and so is this, and a stair cut through the seal would be an escape route that opens into a
    /// sealed working: exactly the thing #1074 beat 1 forbids;</item>
    /// <item>and of what is left, the end furthest from the cage — and then <b>hard against that end's own
    /// cap</b>, which is where a means-of-escape stair goes in any building anybody has ever worked in, and
    /// which is also the furthest from the cage the ground allows.</item>
    /// </list>
    ///
    /// <para><b>The "never beside the cage" law is the BLIND END itself, and not a distance.</b> The goods
    /// car clears <see cref="MinShaftSeparationOn"/> — a third of the corridor — and the stair deliberately
    /// does not use that bar: the cage sits on the spine's heart, so the corridor is not halved about it and
    /// on the shipped field the far cap is 92.5 du away against a third of 92.7. Ranking a stair out of the
    /// building over two tenths of a deck unit would be a typed number overruling the ground. What is TRUE
    /// here, and is the stronger statement, is structural: the stair stands beyond the outermost cross
    /// corridor, so <b>every room in the building lies between it and the cage</b>. Two exits that bracket
    /// the whole plan are not one exit drawn twice, whatever the arithmetic of thirds says. The one distance
    /// still enforced is <see cref="SpecimenRecessAt"/>'s own — never shoulder to shoulder with the car
    /// everybody arrives in.</para>
    ///
    /// <para><b>Null is a real answer</b>, exactly as it is for the goods car: a field whose ribs run out to
    /// its own end caps has no blind end, and this says nothing rather than cutting a stair through somebody
    /// else's room. What makes the law provable is that a guard then asks the SITE LIST whether that ever
    /// happens on a building the game ships (it does not).</para>
    ///
    /// <para>Returned on the SPINE's own centre line, like <see cref="ServiceShaftAt"/> — the recess is cut
    /// off the upper face by whoever is doing the cutting, and a placer that handed back a face coordinate
    /// would have two callers each undoing the other's <see cref="CorridorHalf"/>.</para>
    /// </summary>
    public static (double X, double Y)? StairShaftAt(in SurfaceLayout.Field field)
    {
        IReadOnlyList<(int Ordinal, double X)> ribs = RibColumnsOn(field);
        if (ribs.Count == 0)
        {
            return null;
        }

        (double cageX, double cageY) = ShaftAt(field);
        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;

        // The same two numbers ServiceShaftAt and SpecimenRecessAt measure a blind end with, asked the same
        // way: how far a rib's chambers reach along the spine at their biggest, and how much air a box in
        // that wall wants.
        double reach = CorridorHalf + (RoomWidthDu * DeepestRoomScale) + 1.5;
        double clear = ShaftHalf + ShaftClearDu;

        (double Lo, double Hi)[] ends =
        [
            (left + clear, ribs[0].X - reach - clear),
            (ribs[^1].X + reach + clear, right - clear),
        ];

        double? goodsCarX = ServiceShaftAt(field) is { } car ? car.X : null;
        double? pocketX = SpecimenRecessAt(field) is { } pocket ? pocket.X : null;

        double bestX = double.NaN, bestGap = -1;
        foreach ((double lo, double hi) in ends)
        {
            if (hi - lo < 2 * ShaftHalf)
            {
                continue;   // not enough wall to cut a mouth in
            }
            if (Taken(goodsCarX, lo, hi, clear) || Taken(pocketX, lo, hi, clear))
            {
                continue;   // somebody already has this end
            }

            // Hard against this end's own cap — the side of the stretch that is further from the cage, which
            // is the end of the corridor. Held a half-width in, so the pocket is cut inside the ground the
            // blind end actually offers rather than through the cap.
            double x = Math.Abs(lo - cageX) > Math.Abs(hi - cageX) ? lo + ShaftHalf : hi - ShaftHalf;
            if (Math.Abs(x - cageX) < (2 * ShaftHalf) + ShaftClearDu)
            {
                continue;   // never shoulder to shoulder with the car everybody arrives in
            }

            double gap = Math.Abs(x - cageX);
            if (gap > bestGap)
            {
                (bestX, bestGap) = (x, gap);
            }
        }

        return double.IsNaN(bestX) ? null : (bestX, cageY);

        static bool Taken(double? x, double lo, double hi, double clear) =>
            x is { } at && at >= lo - clear && at <= hi + clear;
    }

    /// <summary>#719 · The stair as a published <see cref="Shaft"/>, or null where the ground would not take
    /// one. Same shape as the cars so that anything asking "what is this way out and where does it let me
    /// stand" gets one kind of answer (<see cref="Shaft.Landing"/>, <see cref="Shaft.Sign"/>,
    /// <see cref="Shaft.ReachesTheSurface"/>) whichever of the three it is holding.</summary>
    public static Shaft? StairOn(in SurfaceLayout.Field field) =>
        StairShaftAt(field) is { } at ? new Shaft(ShaftKind.Stair, at.X, at.Y) : null;

    /// <summary>
    /// #719 · <b>EVERY WAY OFF THIS FLOOR</b> — the cars (<see cref="ShaftsOn"/>) and then the stair.
    ///
    /// <para><see cref="ShaftsOn"/> is, and stays, the list of CARS: it is what a motor, a call button, a
    /// panel, a radio emitter and a refuge's detour are all facts about, and a stair is none of those things.
    /// This is the list a law about ESCAPE is written against, which is a different question and now has a
    /// different list — the same split #801 made between "the lift" and "every way off this floor", one rung
    /// further along.</para>
    /// </summary>
    public static IReadOnlyList<Shaft> ExitsOn(in SurfaceLayout.Field field)
    {
        var exits = new List<Shaft>(ShaftsOn(field));
        if (StairOn(field) is { } stair)
        {
            exits.Add(stair);
        }
        return exits;
    }

    // ── #719 · WHAT THE CLIMB COSTS, AND WHY IT IS NOT A NUMBER ANYBODY TYPED ────────────────────────────
    //
    // Owner: "going up would use more air". The issue's own note is that the price is already implemented —
    // SuitAir has the exertion band for exactly this, and MetresDown already reports the climb — so there is
    // no new constant and no new tuning knob here, only an arrangement of four things the game already
    // measures:
    //
    //   * MetresDown(level)          — the building's own depth, in metres, overburden included (#600).
    //   * SurfaceScale.DeckUnits     — the ONE conversion between metres and the grid the boots walk (#649).
    //   * SuitAir.WalkHomeSeconds    — the seconds a deck unit of travel costs (#325's own arithmetic).
    //   * SuitAir.Breathing.HeavyLabour — 2.2x Walking: "physical chores ... could cost more even though not
    //                                  taking as long". Hauling yourself up a shaft is the owner's example
    //                                  in a different posture.
    //
    // THE CAR CHARGES NOTHING, and that is the shape of the decision this puts in the player's hands: the
    // car is fast and free and somebody else's; the stair is always there and it is paid for out of the tank.
    // A twenty-minute tank with a thirty-minute reserve prices itself, and the plate by the lift already
    // states the depth (§13.13), so the climb can be reckoned before it is started rather than discovered
    // half way up.
    //
    // AND IT IS CHARGED ON EVERY FLOOR, INCLUDING THE ONE THAT BREATHES. B1 holds pressure; the regolith
    // does not, and a stairwell that discharges under the lid on the surface is open to the moon for its
    // whole length. The floor's door is the seal. So the climb spends the tank wherever it starts, which is
    // the honest reading of a shaft whose top is outside.

    /// <summary>#719 · The climb from <paramref name="level"/>, expressed as the walk it actually is, in deck
    /// units. The RISE — the building's own <see cref="MetresDown"/>, through the one metres-to-grid
    /// conversion the game has — plus the TRAVERSE, because a stair that starts at the blind end of the spine
    /// and comes up under the head's own lid has to cross the building as it climbs.</summary>
    public static double ClimbDu(in SurfaceLayout.Field field, int level)
    {
        double rise = SurfaceScale.DeckUnits(MetresDown(level));
        double traverse = StairShaftAt(field) is { } at ? Math.Abs(at.X - ShaftAt(field).X) : 0.0;
        return rise + traverse;
    }

    /// <summary>#719 · What the climb out from <paramref name="level"/> takes out of the tank, in the suit's
    /// own play-seconds: <see cref="ClimbDu"/> of travel, priced by <see cref="SuitAir.WalkHomeSeconds"/> the
    /// way the walk back is priced, at <see cref="SuitAir.Breathing.HeavyLabour"/> rather than at a stroll.
    /// Zero on the surface, where there is nothing left to climb.</summary>
    public static double ClimbAirSeconds(in SurfaceLayout.Field field, int level) =>
        level >= 0
            ? 0.0
            : SuitAir.WalkHomeSeconds(ClimbDu(field, level)) * SuitAir.Breathing.HeavyLabour;

    /// <summary>
    /// #719 · Cut the stair into the floor being built: the pocket's two sides and its back in ordinary
    /// poured hull, the mouth handed to the spine's own sweep at the pocket's own width (#819's law), a LEAF
    /// hung across that mouth in the building's own door vocabulary, the plate beside it on the corridor side
    /// (#775's rule: a walker is told what the wall beside them is before they have to wonder), and the
    /// ground claimed so no later placer lays a room across it.
    ///
    /// <para><b>THE DOOR OPENS ONE WAY, AND THAT IS THE WHOLE OF THE SAFETY ARGUMENT.</b> A floor's door
    /// lets a captain INTO the shaft; the shaft lets them out at the top and nowhere else — which is what a
    /// fire stair in a real building is (re-entry is locked off from the stair side, at every level but the
    /// discharge). Said in this game's own terms: there is a console on every listed floor and there is none
    /// in the shaft, so nothing can be ridden or walked DOWN it.</para>
    ///
    /// <para>That is not a scope cut, it is what makes the stair shippable at all. If the stair opened onto
    /// floors it would be a second road past every gate the cage runs — the SEALED row (#590), the ID CHECK
    /// band (#715), the stop order's seal (#1074 beat 1) — and §13.5's one earned thing, depth, would be
    /// buyable by walking. An escape route is not an entrance. Nothing below the listed bottom has a door at
    /// all (<see cref="HasStairOn"/>), and the one end of the corridor the seal's pocket stands in is the one
    /// end this stair is forbidden (<see cref="StairShaftAt"/>).</para>
    /// </summary>
    private static void CarveStair(
        string bodyId, int level, in SurfaceLayout.Field field,
        List<SurfaceLayout.Wall> walls,
        List<(double Y, double Lo, double Hi)> alcoveMouths,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<SurfaceLayout.Doorway> doorways,
        List<SurfaceLayout.Landmark> labels)
    {
        if (!HasStairOn(bodyId, level) || StairShaftAt(field) is not { } at)
        {
            return;
        }

        double face = at.Y + CorridorHalf;
        double far = face + StairRecessDu;
        walls.Add(new(at.X - ShaftHalf, face, at.X - ShaftHalf, far, true));
        walls.Add(new(at.X + ShaftHalf, face, at.X + ShaftHalf, far, true));
        walls.Add(new(at.X - ShaftHalf, far, at.X + ShaftHalf, far, true));
        alcoveMouths.Add((face, at.X - ShaftHalf, at.X + ShaftHalf));
        doorways.Add(new(at.X - ShaftHalf, face, at.X + ShaftHalf, face));

        // On the corridor side, never over the leaf — #775 watched a captain stand squarely on top of a plate
        // centred on its own doorway, and a sign you have to step off to read is not signage.
        labels.Add(new(at.X, face - 2.0, StairSign));
        claimed.Add((at.X - ShaftHalf - 1.5, face, at.X + ShaftHalf + 1.5, far + 1.5));
    }
}
