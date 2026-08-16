using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #818 · NEVER AN EMPTY FLOOR, SWEPT OVER THE WHOLE BUILDING — and #853's posters riding along on the one
/// department they are about.
///
/// <para>Owner ruling, 2026-08-11 evening, generalising #817 past the ring:</para>
///
/// <list type="bullet">
/// <item><i>"Same for labs etc spaces… they have chairs and desks and equipment … never ever empty
/// floor."</i></item>
/// <item><i>"chairs / tables / vacuum chambers (cough !!), chemical test ventilation boxes [fume hoods], etc
/// where do I put my test tube? Etc furnaces … let's not have any empty storage space unless the space is
/// actually an empty storage."</i></item>
/// </list>
///
/// <para><b>The law.</b> A room's floor is evidence of what the room is for. Every carved room a captain can
/// stand in publishes at least one furnishing appropriate to its plate; bare deck is legal only where the
/// plate SAYS bare. Corridors, streets, the landing pad and the fire recesses are not rooms and are not in
/// <c>FloorPlan.Rooms</c> at all, so the sweep never has to argue about them — which is the whole reason #861
/// published that list.</para>
///
/// <para><b>Stated against a list the building keeps.</b> Every room hands over its own box, its own ways out
/// and now its own furniture (<see cref="UndergroundComplex.Room.Furniture"/>). Nothing below reconstructs a
/// room from the wall list and nothing below is measured off a coordinate typed here — the house's fifth bug
/// class is a guard whose world cannot tell pass from fail.</para>
///
/// <para><b>Proven RED on the world of 2026-08-12</b>, by making <c>ChamberFitting.Fit</c> return
/// <c>RingOffice.Furnishing.Empty</c> at the top — which is the building exactly as it shipped the day this
/// issue was filed:</para>
/// <code>
/// 39051 carved room(s) show nothing on the floor:
///   luna B2 · 15.0 x 12.0 du (SUBJECT PREP) at (-118.5,-13.0) — LABORATORIES · nothing on the floor at all.
///   luna B2 · 15.0 x 12.0 du (CALIBRATION) at (-118.5,-25.0) — LABORATORIES · nothing on the floor at all.
///   ...
/// </code>
/// <para>Green after: nought, out of 39,051 rooms held to the law across 2,338 floors.</para>
///
/// <para><b>…and proven able to fail again</b> by stripping ONE room kind — returning an empty kit for
/// <c>Kit.Store</c> alone — which reddens 8,096 rooms and leaves the other kinds green, so the guard is
/// reading each trade rather than a single global switch.</para>
/// </summary>
public sealed class NeverAnEmptyFloorTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every floor of every site in the sweep, built by the real generator over the real field.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.FloorPlan Floor)> EveryFloor()
    {
        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                yield return (body, level, UndergroundComplex.Build(body, level, Field));
            }
        }
    }

    /// <summary>
    /// #818 · WHY A ROOM MIGHT LAWFULLY SHOW NOTHING. The exemption list, written out rather than left as a
    /// set of <c>continue</c>s: an exemption nobody can read is an exemption nobody can argue with, and the
    /// owner's law is only as strong as the list of things it does not cover.
    /// </summary>
    private static string? WhyExempt(in UndergroundComplex.Room room) => room.Kind switch
    {
        // Their fixture IS the room. A cubicle is a pan behind a door and a cell is the same object hung off
        // a principal chamber, both published by the placer that laid them (#707/#821), and both bedroom-small
        // by the fire code's own measure. A second list of furniture for a WC would be two authors on one box.
        UndergroundComplex.RoomKind.Cubicle => "a WC cubicle — its fixture is the room (#821)",
        UndergroundComplex.RoomKind.Cell => "an en-suite cell — its fixture is the room (#707)",

        // Furnished since #751, in the canteen's own vocabulary: round tops, a stool row, the counter desk,
        // and a booth's own table. That is a list this sweep does not own and must not restate — #813's
        // lesson about two placers agreeing, said about furniture.
        UndergroundComplex.RoomKind.Hall => "the cantina hall — furnished by its own carve since #751",
        UndergroundComplex.RoomKind.Cabinet => "a negotiating booth — its table is carved with it (#751)",

        // #677 · A GALLERY IS NOT A ROOM WITH A PLATE ON IT. Nothing down in the found band was labelled by
        // anybody, and a bench in one would say — in the one channel #592 reserves for it — that somebody
        // shipped it here and fitted it out. The doors were refused on exactly this reasoning.
        _ when room.Plate.Length == 0 => "a gallery in the found band — nobody built it (#677)",

        // The owner's own escape hatch, and the only one he asked for: "let's not have any empty storage
        // space unless the space is actually an empty storage."
        _ when ChamberFitting.IsEmptyStore(room.Plate) => "a store that is actually an empty store (#818)",

        _ => null,
    };

    private static string Say(
        string body, int level, in UndergroundComplex.FloorPlan floor,
        in UndergroundComplex.Room room, string what) =>
        string.Create(CultureInfo.InvariantCulture,
            $"  {body} B{-level} · {room.WidthDu:F1} x {room.DepthDu:F1} du"
            + $"{(room.Plate.Length > 0 ? $" ({room.Plate})" : " (no plate)")}"
            + $" at ({room.X:F1},{room.Y:F1}) — {floor.Name} · {what}");

    // ── (a) THE LAW ITSELF ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY CARVED ROOM SHOWS WHAT IT IS FOR. The owner's ruling, as arithmetic, over every floor of every
    /// site in the sweep.
    ///
    /// <para>Anti-vacuity is four counts and all of them have to move: floors built, rooms held to the law,
    /// rooms LET OFF (an exemption list nothing falls under is not being tested), and floors carrying
    /// laboratories — because the lab kit is the half of this issue the owner wrote from his own decade of
    /// running these rooms, and a sweep that never met a laboratory would pass while proving nothing about
    /// it.</para>
    /// </summary>
    [Fact]
    public void EveryRoomACaptainCanStandInPublishesSomethingOnItsFloor()
    {
        var bare = new List<string>();
        int held = 0, exempt = 0, floors = 0, labFloors = 0, storeFloors = 0;
        var byKit = new Dictionary<ChamberFitting.Kit, int>();

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            floors++;
            string? department = ChamberFitting.DepartmentOn(body, level);
            if (ChamberFitting.LabsOn(department))
            {
                labFloors++;
            }
            if (ChamberFitting.StoresOn(department))
            {
                storeFloors++;
            }

            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (WhyExempt(in room) is not null)
                {
                    exempt++;
                    continue;
                }

                held++;
                ChamberFitting.Kit kit = ChamberFitting.KitFor(
                    room.Plate, department, UndergroundComplex.KindOn(body, level));
                byKit[kit] = byKit.GetValueOrDefault(kit) + 1;

                if (!room.IsFurnished)
                {
                    bare.Add(Say(body, level, in floor, in room, "nothing on the floor at all."));
                }
            }
        }

        Assert.True(floors > 200, $"only {floors} floors were built — this proved little.");
        Assert.True(held > 1000, $"only {held} room(s) were held to the law — this proved little.");
        Assert.True(
            exempt > 0,
            "not one room was let off, so the exemption list — the cubicles, the cells, the hall, the "
            + "galleries and the owner's own empty store — was never exercised. A list nothing falls under "
            + "is a list nobody is testing.");
        Assert.True(
            labFloors > 0,
            "the sweep never met a LABORATORIES floor, so the kit the owner spelled out — benches, fume "
            + "hoods, vacuum chambers, furnaces — was never asked for.");
        Assert.True(
            storeFloors > 0,
            "the sweep never met a store floor, so the empty-store clause was never reachable.");

        // …and every kind of kit was actually dealt to somebody. A ladder that answered one kit for the
        // whole building would satisfy the count above and tell us nothing about the other five.
        Assert.True(
            byKit.Count(p => p.Value > 0) >= 5,
            "fewer than five different kits were dealt across the whole sweep — the plate/department/trade "
            + $"ladder is collapsing to one answer: {string.Join(", ", byKit.Select(p => $"{p.Key}={p.Value}"))}");

        Assert.True(
            bare.Count == 0,
            $"{bare.Count} carved room(s) show nothing on the floor:{Environment.NewLine}"
            + string.Join(Environment.NewLine, bare.Take(12)));
    }

    // ── (b) THE EXEMPTIONS ARE FACTS ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// AN EMPTY FLOOR IS A CLAIM THE BUILDING MAKES. Every room let off for being empty says so on its own
    /// plate, and it says so only where a store floor could honestly say it.
    ///
    /// <para>This is the clause that stops the law being talked out of: a generator that started plating
    /// laboratories <c>STORE — EMPTY</c> to get past the guard above would go red here.</para>
    /// </summary>
    [Fact]
    public void AnEmptyStoreIsOnAStoreFloorAndSaysSoOnThePlate()
    {
        var wrong = new List<string>();
        int emptied = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            string? department = ChamberFitting.DepartmentOn(body, level);
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (!ChamberFitting.IsEmptyStore(room.Plate))
                {
                    continue;
                }
                emptied++;

                if (!ChamberFitting.StoresOn(department))
                {
                    wrong.Add(Say(body, level, in floor, in room,
                        "is plated as an empty store on a floor that keeps no stock."));
                }
                if (room.Kind != UndergroundComplex.RoomKind.Chamber)
                {
                    wrong.Add(Say(body, level, in floor, in room,
                        $"is plated as an empty store and is a {room.Kind}."));
                }
                if (room.IsFurnished)
                {
                    wrong.Add(Say(body, level, in floor, in room,
                        "says it is empty and has furniture in it."));
                }
            }
        }

        Assert.True(
            emptied > 0,
            "no room anywhere in the sweep is an empty store, so the owner's own escape hatch is a clause "
            + "nobody can reach — which makes the law above unfalsifiable rather than strict.");
        Assert.True(
            wrong.Count == 0,
            $"{wrong.Count} empty-store plate(s) are not facts about a store:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }

    // ── (c) NOTHING IS BUILT WHERE A BODY HAS TO GO ───────────────────────────────────────────────────

    /// <summary>
    /// NO FITTING STANDS IN A DOORWAY'S CLEARANCE, ON A WALL, OR ON THE SQUARE THE AUDIT WALKS TO.
    ///
    /// <para>This project's most expensive bugs have all been a room that was correct on the plan and wrong
    /// to walk, so the sharp guard is not "is the bench inside the room" but "is the bench out of every hole
    /// in the room's walls". Measured against the room's OWN published ways and against every en-suite leaf
    /// on the floor — the whole set the furnisher was handed, so a fitting cannot be proved clear of a
    /// shorter list than it was laid against.</para>
    /// </summary>
    [Fact]
    public void NoFittingStandsWhereABodyHasToWalk()
    {
        var wrong = new List<string>();
        int measured = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            var leaves = new List<SurfaceLayout.Doorway>();
            foreach (UndergroundComplex.EnSuite cell in floor.EnSuites)
            {
                leaves.AddRange(cell.Ways);
            }

            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (room.Kind != UndergroundComplex.RoomKind.Chamber || !room.IsFurnished)
                {
                    continue;
                }

                foreach (RingOffice.Fixture fit in room.Furniture)
                {
                    measured++;

                    if (fit.X0 < room.X0 - 1e-6 || fit.X1 > room.X1 + 1e-6
                        || fit.Y0 < room.Y0 - 1e-6 || fit.Y1 > room.Y1 + 1e-6)
                    {
                        wrong.Add(Say(body, level, in floor, in room,
                            $"has a {fit.Kind} standing outside its own walls."));
                        continue;
                    }

                    double toCentre = BoxToPoint(in fit, room.X, room.Y);
                    if (toCentre < ChamberFitting.CentreClearDu - 1e-6)
                    {
                        wrong.Add(Say(body, level, in floor, in room,
                            $"has a {fit.Kind} {toCentre:F2} du off its own centre — the audit stands there."));
                    }

                    foreach (SurfaceLayout.Doorway hole in room.Ways.Concat(leaves))
                    {
                        double gap = BoxToSegment(in fit, hole.X1, hole.Y1, hole.X2, hole.Y2);
                        if (gap < ChamberFitting.OpeningClearDu - 1e-6)
                        {
                            wrong.Add(Say(body, level, in floor, in room,
                                $"has a {fit.Kind} {gap:F2} du off an opening — the clearance is "
                                + $"{ChamberFitting.OpeningClearDu:F1} du."));
                            break;
                        }
                    }
                }

                // …and every seat is real floor too. A stool published on the square the A* audit stands on
                // would report as "something was built through this room" rather than as a chair.
                foreach (RingOffice.Chair seat in room.Seats)
                {
                    measured++;
                    double dx = seat.X - room.X, dy = seat.Y - room.Y;
                    if (Math.Sqrt((dx * dx) + (dy * dy)) < ChamberFitting.CentreClearDu - 1e-6)
                    {
                        wrong.Add(Say(body, level, in floor, in room,
                            "seats somebody on the square the audit walks to."));
                    }
                    if (!room.Contains(seat.X, seat.Y))
                    {
                        wrong.Add(Say(body, level, in floor, in room, "seats somebody outside the room."));
                    }
                }
            }
        }

        Assert.True(measured > 1000, $"only {measured} fitting(s) were measured — this proved little.");
        Assert.True(
            wrong.Count == 0,
            $"{wrong.Count} fitting(s) are in the way:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }

    // ── (d) THE OWNER'S OWN LAB KIT ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// A LABORATORIES FLOOR ANSWERS "WHERE DO I PUT MY TEST TUBE?".
    ///
    /// <para>The owner's list, held to at FLOOR scale rather than at room scale, which is how he stated it:
    /// benches with racked glassware, fume hoods, vacuum chambers and furnaces are what a laboratories floor
    /// has, and a single chamber holding all four would be a showroom. Every one of the four has to appear on
    /// every laboratories floor in the sweep, and every lab chamber has to have its bench.</para>
    /// </summary>
    [Fact]
    public void EveryLaboratoriesFloorHasBenchesFumeHoodsVacuumChambersAndFurnaces()
    {
        var wrong = new List<string>();
        int labFloors = 0, benches = 0, seats = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            if (!ChamberFitting.LabsOn(ChamberFitting.DepartmentOn(body, level)))
            {
                continue;
            }
            labFloors++;

            var kinds = new HashSet<RingOffice.Fitting>();
            int chambers = 0;
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (room.Kind != UndergroundComplex.RoomKind.Chamber || WhyExempt(in room) is not null)
                {
                    continue;
                }
                chambers++;
                foreach (RingOffice.Fixture fit in room.Furniture)
                {
                    kinds.Add(fit.Kind);
                    if (fit.Kind == RingOffice.Fitting.LabBench)
                    {
                        benches++;
                    }
                }
                seats += room.Seats.Count;
            }

            if (chambers < 8)
            {
                continue;   // too few rooms to deal the whole rotation, and that is honest rather than wrong
            }

            foreach (RingOffice.Fitting wanted in new[]
            {
                RingOffice.Fitting.LabBench, RingOffice.Fitting.FumeHood,
                RingOffice.Fitting.VacuumChamber, RingOffice.Fitting.Furnace,
            })
            {
                if (!kinds.Contains(wanted))
                {
                    wrong.Add(string.Create(CultureInfo.InvariantCulture,
                        $"  {body} B{-level} ({floor.Name}, {chambers} chambers) — no {wanted} anywhere."));
                }
            }
        }

        Assert.True(labFloors > 20, $"only {labFloors} laboratories floor(s) were built — this proved little.");
        Assert.True(benches > 200, $"only {benches} bench(es) in the whole sweep — this proved little.");
        Assert.True(
            seats > 100,
            $"only {seats} seat(s) in every laboratory in the game — the owner's list opens with the word "
            + "\"chairs\" and a bench you cannot sit at is a bench in a catalogue.");
        Assert.True(
            wrong.Count == 0,
            $"{wrong.Count} laboratories floor(s) are missing part of the kit:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }

    // ── (e) #853 · THE POSTERS ARE THE OWNER'S OWN WORDS ──────────────────────────────────────────────

    /// <summary>
    /// THE EIGHT POSTERS, VERBATIM. Owner on the pool as written: <i>"yes love those :-D"</i> — blessed as
    /// canonical, and the implementation matches with no additions and no rewrites.
    ///
    /// <para>Typed out again HERE, independently, and compared character for character. A guard that read
    /// the pool and compared it to itself would be the green test that asserts nothing.</para>
    /// </summary>
    [Fact]
    public void ThePosterPoolIsTheOwnerBlessedCopy()
    {
        string[] canonical =
        [
            "HYDROGEN: FUEL OF THE COMING CENTURY",
            "FUSION: NET GAIN WITHIN THIRTY YEARS",
            "ROOM-TEMPERATURE SUPERCONDUCTIVITY: REPLICATION RESULTS EXPECTED SHORTLY",
            "THE SPACE ELEVATOR: A GROUND-FLOOR OPPORTUNITY",
            "THORIUM: TOO CHEAP TO METER (THIS TIME)",
            "SOLID-STATE BATTERY BREAKTHROUGH DOUBLES EVERYTHING",
            "GENERAL MACHINE INTELLIGENCE ALIGNMENT WORKSHOP",
            "NEURAL STATE PRESERVATION: A CENTURY OF CHALLENGES AHEAD",
        ];

        Assert.Equal(canonical.Length, LabPosters.Pool.Count);
        for (int i = 0; i < canonical.Length; i++)
        {
            Assert.Equal(canonical[i], LabPosters.Pool[i].Title, StringComparer.Ordinal);
        }

        // …and the fine print, which is where the whole joke actually lives. Compared as one block so a
        // reordering, a dropped clause or a helpful rewrite all read as one loud diff.
        string fine = string.Join("\n", LabPosters.Pool.Select(p => p.FinePrint));
        Assert.Equal(
            "134th Annual Symposium. Voted Most Promising Energy Vector, 134th consecutive year.\n"
            + "The year on the poster is a sticker, visibly applied over at least four older stickers.\n"
            + "The poster is the most yellowed object on the floor.\n"
            + "Investment prospectus styling, tasteful, terrifying.\n"
            + "Workshop, attendance free, lunch not provided.\n"
            + "'Commercialization expected in five years' in a typeface five typefaces old.\n"
            + "Postponed. New date to follow.\n"
            + "'Keynote: Why Whole-Brain Capture Will Remain Impossible.' The poster is old. Nobody has "
            + "taken it down.",
            fine,
            StringComparer.Ordinal);

        // Eight slots, all different, all in the art-manifest idiom — published before a picture exists,
        // which is the whole reason a slot is a slot.
        var slots = LabPosters.Pool.Select((_, i) => LabPosters.ArtUrlFor(i + 1)).ToList();
        Assert.Equal(slots.Count, slots.Distinct(StringComparer.Ordinal).Count());
        Assert.All(slots, s => Assert.StartsWith("art/lab-poster-", s, StringComparison.Ordinal));

        // §13.8 · nothing on a poster says what the FACILITY is for. The kicker is the one that comes
        // closest and it comes closest by disagreeing with the building, which is not the same as
        // explaining it.
        Assert.DoesNotContain(
            LabPosters.Pool,
            p => p.Title.Contains("KAAMOS", StringComparison.OrdinalIgnoreCase)
                || p.FinePrint.Contains("KAAMOS", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// THEY HANG ON LABORATORIES WALLS AND NOWHERE ELSE, and the kicker hangs crooked at the far end.
    ///
    /// <para>Owner: <i>"(the kicker — hang it slightly crooked, in the worst corridor)"</i>. "The worst
    /// corridor" is read off the plan — the chamber furthest from the way home — rather than typed at a
    /// coordinate, so a floor whose rooms move takes its poster with it.</para>
    /// </summary>
    [Fact]
    public void ThePostersHangOnLaboratoriesFloorsAndTheKickerHangsCrooked()
    {
        var wrong = new List<string>();
        int labFloors = 0, hung = 0, kickers = 0, full = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            bool labs = ChamberFitting.LabsOn(ChamberFitting.DepartmentOn(body, level));
            IReadOnlyList<LabPosters.Poster> posters = floor.TheWalls;

            if (!labs)
            {
                if (posters.Count > 0)
                {
                    wrong.Add($"  {body} B{-level} ({floor.Name}) — {posters.Count} poster(s) on a floor "
                        + "that is not a laboratories floor.");
                }
                continue;
            }

            labFloors++;
            hung += posters.Count;
            if (posters.Count == LabPosters.Pool.Count)
            {
                full++;
            }

            var numbers = new HashSet<int>();
            foreach (LabPosters.Poster p in posters)
            {
                if (!numbers.Add(p.Number))
                {
                    wrong.Add($"  {body} B{-level} — poster {p.Number} hangs twice on one floor.");
                }
                if (p.Crooked != (p.Number == LabPosters.KickerNumber))
                {
                    wrong.Add($"  {body} B{-level} — poster {p.Number} is "
                        + $"{(p.Crooked ? "crooked" : "straight")} and should not be.");
                }
                if (p.Card.Length == 0 || p.ArtUrl.Length == 0)
                {
                    wrong.Add($"  {body} B{-level} — poster {p.Number} has nothing to read.");
                }
            }

            // …and no two posters on one floor stand on the same square, which is what would happen if the
            // kicker were dealt out of the same run as the rest.
            for (int i = 0; i < posters.Count; i++)
            {
                for (int j = i + 1; j < posters.Count; j++)
                {
                    double dx = posters[i].X - posters[j].X, dy = posters[i].Y - posters[j].Y;
                    if ((dx * dx) + (dy * dy) < 1.0)
                    {
                        wrong.Add($"  {body} B{-level} — posters {posters[i].Number} and "
                            + $"{posters[j].Number} hang on the same square.");
                    }
                }
            }

            if (posters.Any(p => p.Number == LabPosters.KickerNumber))
            {
                kickers++;
            }
        }

        Assert.True(labFloors > 20, $"only {labFloors} laboratories floor(s) — this proved little.");
        Assert.True(hung > 100, $"only {hung} poster(s) hang in the whole game — this proved little.");
        Assert.True(
            kickers == labFloors,
            $"{labFloors - kickers} laboratories floor(s) have no kicker on them — the eighth poster is the "
            + "reason the gag earns wall space.");
        Assert.True(
            full > 0,
            "not one laboratories floor anywhere carries the whole pool of eight, so most of the copy the "
            + "owner blessed is never on a wall.");
        Assert.True(
            wrong.Count == 0,
            $"{wrong.Count} poster(s) hang wrong:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }

    // ── THE MEASUREMENTS ──────────────────────────────────────────────────────────────────────────────

    /// <summary>How far a box is from a point.</summary>
    private static double BoxToPoint(in RingOffice.Fixture box, double px, double py)
    {
        double dx = Math.Max(Math.Max(box.X0 - px, px - box.X1), 0.0);
        double dy = Math.Max(Math.Max(box.Y0 - py, py - box.Y1), 0.0);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>…and how far a BOX is from a segment, sampled along the segment. #817's own measurement,
    /// borrowed whole: a rectangle-to-segment distance in closed form is three cases and a sign error, and
    /// this walks the segment far finer than any clearance being asserted.</summary>
    private static double BoxToSegment(
        in RingOffice.Fixture box, double x1, double y1, double x2, double y2)
    {
        double worst = double.MaxValue;
        const int Samples = 32;
        for (int i = 0; i <= Samples; i++)
        {
            double t = (double)i / Samples;
            double px = x1 + ((x2 - x1) * t), py = y1 + ((y2 - y1) * t);
            worst = Math.Min(worst, BoxToPoint(in box, px, py));
        }
        return worst;
    }
}
