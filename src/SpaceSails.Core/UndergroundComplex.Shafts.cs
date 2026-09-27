using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    /// <summary>The lift shaft's spot — the SAME (x, y) on every floor, so going down is legible and coming
    /// back up is never a search. Sits on the spine corridor at the field's heart.
    ///
    /// <para>#801 · This is THE CAGE now, and it is one of two. Everything that only ever wanted "the lift"
    /// still asks here and still gets the same answer it always did; the ones that mean <b>every way off
    /// this floor</b> ask <see cref="ShaftsOn"/>.</para></summary>
    public static (double X, double Y) ShaftAt(in SurfaceLayout.Field field) =>
        (field.AnchorX + 40, (field.BottomY + field.LandingBandY) / 2.0);

    /// <summary>Half-width of the lift car, and of the shaft cut through every floor.</summary>
    public const double ShaftHalf = 3.0;

    /// <summary>Corridor half-width. Wide enough for the captain and an Old One to pass and for the eye to
    /// read it as a built passage rather than a gap between two walls.</summary>
    public const double CorridorHalf = 3.5;

    // ── #801 · THE BUILDING HAS TWO CARS, AND THEY ARE NOT BESIDE EACH OTHER ──────────────────────────────
    //
    // Owner, 2026-08-09: "that elevator would be so busy it would be packed and never available… it is a
    // choke point, and the whole lab would be too easily guarded by just having the guard posted in front
    // of the one elevator. I want to remove that too-easy plot-to-catch-us plot hole."
    //
    // He is right three times over and the third one is the interesting one:
    //
    //   * TRAFFIC. A facility with a canteen for eighty, twelve growing beds and a goods hoist does not run
    //     on one personnel car. It never did; the building simply never drew the second one.
    //   * PACING. One car is a come-back-here point on every floor. Two at opposite ends of the spine turn
    //     a floor into a route with a decision in it, which is what #775 did for the park one storey up.
    //   * THE POSTED GUARD. This is the plot hole. A single car is a single square somebody stands on, and
    //     no amount of writing around that makes an escape feel earned. Two cars a hundred and seventy du
    //     apart cannot be watched by one person, and the fiction stops needing an excuse.
    //
    // What this is NOT. It is not #719's executive lift (that hangs off a principal apartment, is on no
    // panel, and costs your cover), and it is not #719's service stair. It is the ordinary, boring, second
    // car every real building of this size has, and it ships first because the other two are beats and this
    // is a topology.
    //
    // THE CARD LAW IS UNTOUCHED (§13.5). The service car runs its band and nothing else: no surface, no
    // gate, no way past the seam. A second car that could cross a band boundary would be a way to buy depth
    // without the paper, and depth past the first band is the one thing this game makes you earn.

    /// <summary>#801 · Which of the two cars this is. #719 · …and the one way out that is not a car at
    /// all.</summary>
    public enum ShaftKind
    {
        /// <summary>The cage: the one the surface head sits on top of, the one the plate is beside, and the
        /// only one that runs a gate.</summary>
        Cage,

        /// <summary>The goods car at the blind end of the corridor. Same four floors, no surface, no
        /// gate.</summary>
        Service,

        /// <summary>#719 · The service stair, at the OTHER blind end. No motor, no call button, no panel and
        /// no gate — a flight of steps a safety inspectorate made somebody build, which climbs out and is
        /// paid for in air. See <see cref="UndergroundComplex.StairShaftAt"/>.</summary>
        Stair,
    }

    /// <summary>#801 · A car, on the plan. Published from <see cref="ShaftsOn"/> so that a law about "every
    /// way off this floor" has a list to be written against — the same reason <see cref="Hall.Openings"/>
    /// and <see cref="Park.Ways"/> exist, said about the thing a captain leaves by.
    ///
    /// <para>#719 · …and the stair is one of these too (<see cref="ExitsOn"/>), so that anything asking what
    /// a way out IS gets one kind of answer whichever of the three it is holding.</para></summary>
    public readonly record struct Shaft(ShaftKind Kind, double X, double Y)
    {
        /// <summary>What is painted at the car mouth — or, for the stair, at its door.</summary>
        public string Sign => Kind switch
        {
            ShaftKind.Cage => CageSign,
            ShaftKind.Stair => StairSign,
            _ => ServiceCarSign,
        };

        /// <summary>Does this one climb all the way out? The cage does, because it has a hut on the regolith
        /// over it (#606) — and, since #719, so does the stair, which comes up under that same lid. The goods
        /// car does not, and that is still the whole of why the cage was ever the only way home.</summary>
        public bool ReachesTheSurface => Kind != ShaftKind.Service;

        /// <summary>Does this one run the gate to the band below? Only the cage. §13.5 is a law about the
        /// building, not about a car, and neither the second car nor the stair may be a way round it — the
        /// stair least of all, because it does not open onto a floor at all (<see cref="CarveStair"/>).
        /// </summary>
        public bool RunsTheGate => Kind == ShaftKind.Cage;

        /// <summary>Where a captain stands when the doors open — a pace out of the car, on the spine. The
        /// cage's alcove hangs off the spine's upper face and the service car's off the lower one, so the
        /// pace is outward in opposite directions and neither of them is a typed sign. #719 · The stair's
        /// pocket is on the upper face, like the cage's, so its doorstep is the cage's own way round.</summary>
        public (double X, double Y) Landing =>
            (X, Kind == ShaftKind.Service ? Y - 1.0 : Y + 1.0);
    }

    /// <summary>#801 · What is painted at the cage's mouth. The console has said this since #585.</summary>
    public const string CageSign = "\U0001F6D7 LIFT";

    /// <summary>#801 · …and at the other one. It says what it is for and it says what it does not do, in
    /// the inspectorate voice every plate down here is stencilled in — a car with no surface button is a
    /// car a captain has to be told about ONCE rather than discover by pressing.</summary>
    public const string ServiceCarSign = "\U0001F6D7 GOODS CAR 2 · THIS BAND ONLY";

    /// <summary>#801 · What the service car's panel says under its title, in place of the cage's own line.
    /// It names where the other car is, which is the whole of the anti-choke feature said in a sentence:
    /// a captain who finds one car has been told there is another and roughly where.</summary>
    public const string ServiceCarPanelLine =
        "The goods car. It runs these floors and it does not climb out: for the surface, and for anything "
        + "below this band, the cage is at the other end of the corridor.";

    /// <summary>#801 · How much clear corridor a car's alcove wants either side of itself before the ground
    /// counts as taking one.</summary>
    public const double ShaftClearDu = 1.5;

    /// <summary>#801 · THE WIDEST A CHAMBER EVER GETS in this building — the found band's deepest floor
    /// (<see cref="FoundGrowthPerFloor"/> compounded across a band), and the reason it is here rather than
    /// beside the growth constant: a car stands in the SAME place on every floor of a site, so the ground
    /// it needs has to be clear of the biggest room the site can produce and not merely of this floor's.
    /// A number worked out per floor would have put the second car in solid chamber four storeys down.</summary>
    public static double DeepestRoomScale => Math.Pow(FoundGrowthPerFloor, FloorsPerShaft - 1);

    /// <summary>#801 · How far apart the two cars must be before the building counts as having two. Stated
    /// as a share of the spine it is measured on rather than as a distance, because "far enough that one
    /// person cannot watch both" is a fact about the corridor's length and not about deck units. A third of
    /// the main corridor is a walk with two cross-corridors and the length of the hall in it.</summary>
    public static double MinShaftSeparationOn(in SurfaceLayout.Field field)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        return ((field.RightX - margin) - (field.LeftX + margin)) / 3.0;
    }

    /// <summary>#801 · WHERE EVERY CROSS CORRIDOR IS, as a pure function of the ground.
    ///
    /// <para>This was five lines inside <see cref="Build"/> and it had to come out: the second car is
    /// placed where no rib and no rib's chambers can reach, and a placer that worked that out from its own
    /// copy of the rib arithmetic would be the mirrored constant this file keeps a table of. One list, and
    /// <see cref="Build"/> reads it too.</para>
    ///
    /// <para>The x's are the same on every floor of every site — only which WAY a rib runs is seeded — so a
    /// spot chosen against them is a spot that holds for the whole building.</para>
    ///
    /// <para><b>The ordinal is the SLOT the field offered, not the survivor's place in this list</b>, and it
    /// is carried out of here for exactly one reason: a rib's direction is seeded on it. The slot the lift
    /// stands in is dropped, so the ordinals a real building uses have a hole in them — and a caller that
    /// re-numbered from zero would silently re-roll which way every corridor in the game runs. That is the
    /// same class of mistake as #587's out-of-order sweep: the arithmetic is right and the INDEX is not.</para></summary>
    public static IReadOnlyList<(int Ordinal, double X)> RibColumnsOn(in SurfaceLayout.Field field)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;
        (double shaftX, _) = ShaftAt(field);

        var xs = new List<(int, double)>();
        const int ribs = 5;
        for (int i = 0; i < ribs; i++)
        {
            double t = (i + 0.5) / ribs;
            double rx = Lerp(left + 16, right - 16, t);
            if (Math.Abs(rx - shaftX) < ShaftHalf + CorridorHalf + 4)
            {
                continue;   // never run a rib through the lift
            }
            xs.Add((i, rx));
        }
        return xs;
    }

    /// <summary>
    /// #801 · WHERE THE SECOND CAR STANDS, or null where this ground will not take one.
    ///
    /// <para>At the blind end of the main corridor: the stretch past the outermost cross corridor, which is
    /// the one length of spine in the building that no chamber can ever reach — every room down here hangs
    /// off a rib, so the ground beyond the last rib's own column is ground nothing will ever be laid in.
    /// That is also exactly where a goods car goes in a building anybody has ever worked in.</para>
    ///
    /// <para><b>The end FURTHER from the cage</b>, and then only if what is left is still a third of the
    /// corridor away from it (<see cref="MinShaftSeparationOn"/>). Two cars a captain can see at once are
    /// one car drawn twice, and the whole point of the feature is that they cannot both be watched.</para>
    ///
    /// <para><b>Null is a real answer.</b> A field whose ribs run out to its own end caps has no blind end,
    /// and this returns null rather than putting a car through a chamber — which is what makes the choke
    /// law provable: it binds where the generator admits two and says nothing where it does not.</para>
    /// </summary>
    public static (double X, double Y)? ServiceShaftAt(in SurfaceLayout.Field field)
    {
        IReadOnlyList<(int Ordinal, double X)> ribs = RibColumnsOn(field);
        if (ribs.Count == 0)
        {
            return null;
        }

        (double cageX, double cageY) = ShaftAt(field);
        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;

        // How far a rib's own chambers reach along the spine, at the biggest a chamber ever gets. The 1.5
        // is the claim ledger's own inflation, so this is the room's keep-out and not the room.
        double reach = CorridorHalf + (RoomWidthDu * DeepestRoomScale) + 1.5;
        double clear = ShaftHalf + ShaftClearDu;

        (double Lo, double Hi)[] ends =
        [
            (left + clear, ribs[0].X - reach - clear),
            (ribs[^1].X + reach + clear, right - clear),
        ];

        // ── #813 · WHICH END, AND THE RING DECIDES IT ────────────────────────────────────────────────────
        //
        // Owner's Manhattan ruling, clause 4: "the extra lift goes on the LESS-BUILT side of the park — the
        // ring's density decides the shaft's side, not the other way around."
        //
        // So the two blind ends are no longer ranked by how far they are from the cage. They are ranked by
        // how much ROOM FRONTAGE the block carries at that end (RingFrontageOn), and the thinner end wins.
        // Distance from the cage is what breaks a tie, which is where the old rule went — it was never
        // wrong, it was only the only thing being asked.
        //
        // This is a real measurement and it really does choose: on the shipped field the west half of the
        // block carries 91 du of frontage and the east half 98, because the rib column nearest the cage is
        // dropped (RibColumnsOn) and the gates therefore fall to the west of the park's middle while the
        // long unbroken run of suites falls to the east. Mirror that asymmetry and the car moves; the guard
        // in TheParkIsTheCentreOfTheBlockTests does exactly that.
        (double westFrontage, double eastFrontage) = RingFrontageOn(field);
        ParkBlock block = BlockOn(field);

        // …and WHERE in that end. Hard against the block's own service street, which is where a goods
        // vehicle stops in any building anybody has ever worked in — clamped into the blind end, because
        // the blind end is a fact about the chambers and this is a preference about the streets.
        (double Lo, double Hi, double Frontage, double Want)[] candidates =
        [
            (left + clear, ribs[0].X - reach - clear, westFrontage,
                block.WestOuterX - ShaftHalf - ShaftClearDu),
            (ribs[^1].X + reach + clear, right - clear, eastFrontage,
                block.EastOuterX + ShaftHalf + ShaftClearDu),
        ];

        double bestX = double.NaN, bestFrontage = double.MaxValue, bestGap = -1;
        foreach ((double lo, double hi, double frontage, double want) in candidates)
        {
            if (hi <= lo)
            {
                continue;   // the ribs run all the way to the cap: no blind end on this side
            }
            double x = Math.Clamp(want, lo, hi);
            double gap = Math.Abs(x - cageX);
            if (frontage < bestFrontage - 0.001 || (frontage < bestFrontage + 0.001 && gap > bestGap))
            {
                (bestX, bestFrontage, bestGap) = (x, Math.Min(frontage, bestFrontage), gap);
            }
        }

        return double.IsNaN(bestX) || bestGap < MinShaftSeparationOn(field) ? null : (bestX, cageY);
    }

    /// <summary>#801 · EVERY WAY OFF THIS FLOOR THAT IS A CAR. The cage first — it is the one the surface
    /// sits on and the one every older law means by "the lift" — then the goods car where the ground took
    /// one.
    ///
    /// <para>Published so that "no floor of a clandestine site has exactly one way off it" is a law that
    /// can be written down and can go red, instead of an arrangement two placers happen to agree on.</para>
    ///
    /// <para><b>#719 · CARS, and only cars.</b> This is the list a motor, a call button, a panel, a radio
    /// emitter (<c>SdrScanner</c>) and a refuge's detour are all facts about, and the stair is none of those
    /// things. A law about ESCAPE asks <see cref="ExitsOn"/>, which is this list with the stair on the end of
    /// it.</para></summary>
    public static IReadOnlyList<Shaft> ShaftsOn(in SurfaceLayout.Field field)
    {
        (double cageX, double cageY) = ShaftAt(field);
        var cars = new List<Shaft> { new(ShaftKind.Cage, cageX, cageY) };
        if (ServiceShaftAt(field) is { } service)
        {
            cars.Add(new Shaft(ShaftKind.Service, service.X, service.Y));
        }
        return cars;
    }
}
