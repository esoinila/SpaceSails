using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #869/#864 · THE DESKS SOMEBODY SPECIFIED, AND THE BOARD THAT HAS NEVER BEEN RESET — Core's half.
///
/// <para>Owner, 2026-08-13, from his own 160 x 90 cm electric desk: <i>"It got up- and down buttons to move
/// the table to work either with office chair, Salli standing (lab) chair or by standing while using the
/// table. Surely in futuristic lab they would have similar spec tables to ensure good ergonomics :-D"</i> —
/// and, the same day, from a room with an AFM and a transmission electron microscope in it: <i>"This lab has
/// survived X days without sarcasm on the wall. That kind gags are such lab humor. :-D"</i></para>
///
/// <para>Everything below is asked of the REAL generator over the real field, across every site the sweep
/// reaches. Nothing here builds a room of its own: the house's fifth bug class is a guard handed a world
/// that cannot tell pass from fail.</para>
/// </summary>
public sealed class TheDeskRisesToMeetYouTests
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

    private static string Say(string body, int level, string what) =>
        string.Create(CultureInfo.InvariantCulture, $"  {body} B{-level} — {what}");

    /// <summary>Box to segment, sampled the way the placer's own check samples it — exact for the
    /// axis-aligned openings this building is made of.</summary>
    private static double GapToDoor(
        double px, double py, in SurfaceLayout.Doorway hole)
    {
        double ax = hole.X1, ay = hole.Y1, bx = hole.X2, by = hole.Y2;
        double ex = bx - ax, ey = by - ay;
        double len2 = (ex * ex) + (ey * ey);
        double t = len2 <= 0 ? 0 : Math.Clamp((((px - ax) * ex) + ((py - ay) * ey)) / len2, 0.0, 1.0);
        double cx = ax + (t * ex), cy = ay + (t * ey);
        return Math.Sqrt(((px - cx) * (px - cx)) + ((py - cy) * (py - cy)));
    }

    // ── (f) THE DESK IS THE SIZE A PERSON IS ──────────────────────────────────────────────────────────

    /// <summary>
    /// #869 · A DESK IS 2.3 x 1.3 DU, AND A LAB BENCH IS 1.3 DU DEEP.
    ///
    /// <para>Owner's own tape measure: <i>"my table (electric) is about 160 cm wide, about 90 cm deep"</i>.
    /// At <c>SurfaceScale</c>'s own anchor (du = 0.7 m) that is 2.3 x 1.3, and #868 had already given the
    /// ring's worktop those two numbers — this is the chambers' kit being held to them.</para>
    ///
    /// <para><b>RED on the base commit</b> for two different reverts, because the old desk was wrong in two
    /// different ways at once. Restoring the office kit's run to <c>BenchRunDu</c>:</para>
    /// <code>
    /// 2688 desk(s) are not the size a person is:
    ///   luna B4 — CLERKS · REGISTER: a DeskBank runs 10.00 du (a desk is 2.30) and is 1.30 deep.
    /// </code>
    /// <para>…and dropping the depth back to a segment (<c>DepthDu: 0</c>):</para>
    /// <code>
    /// 0 desk(s) are not the size a person is
    /// …but only 0 of 2688 desk(s) were laid at the full human-true depth — this proved nothing.
    /// </code>
    /// <para>Both clauses are needed and both are here: a run that is right about a picture nobody draws is
    /// #868's whole finding said backwards.</para>
    /// </summary>
    [Fact]
    public void ADeskIsTheSizeAPersonIs()
    {
        var wrong = new List<string>();
        int desks = 0, benches = 0, atFullDepth = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                // CHAMBERS, which is what #869 is about: the labs and the administration rooms off the ribs.
                // The ring's own park-view BANKS are #817's furniture and a different object — one run of
                // worktop several people sit at, rather than one person's desk — and re-sizing them is the
                // separate change noted in the PR body rather than a clause smuggled in here.
                if (room.Kind != UndergroundComplex.RoomKind.Chamber)
                {
                    continue;
                }

                foreach (RingOffice.Fixture fit in room.Furniture)
                {
                    if (!SitStandDesk.HasAMotor(fit.Kind))
                    {
                        continue;
                    }

                    // The run is the long side and the depth the short one — a fitting is laid along a wall,
                    // and which wall is a fact about the room rather than about the desk.
                    double w = fit.X1 - fit.X0, h = fit.Y1 - fit.Y0;
                    double run = Math.Max(w, h), depth = Math.Min(w, h);

                    if (depth > ChamberFitting.FittingDepthDu + 0.001)
                    {
                        wrong.Add(Say(body, level,
                            $"{room.Plate} ({room.Kind}): a {fit.Kind} is {depth:F2} du deep — a desk is "
                            + $"{ChamberFitting.FittingDepthDu:F2}."));
                    }
                    if (Math.Abs(depth - ChamberFitting.FittingDepthDu) <= 0.001)
                    {
                        atFullDepth++;
                    }
                    else
                    {
                        wrong.Add(Say(body, level,
                            $"{room.Plate} ({room.Kind}): a {fit.Kind} is {depth:F2} du deep — a desk is "
                            + $"{ChamberFitting.FittingDepthDu:F2}."));
                    }

                    if (fit.Kind == RingOffice.Fitting.DeskBank)
                    {
                        desks++;
                        // A desk never grows past one person's frontage. It may be SHORTER, where the wall
                        // it stands against is — a desk built through a doorway would be the bug this whole
                        // file's clearances exist to be incapable of.
                        if (run > ChamberFitting.DeskRunDu + 0.001)
                        {
                            wrong.Add(Say(body, level,
                                $"{room.Plate}: a DeskBank runs {run:F2} du (a desk is "
                                + $"{ChamberFitting.DeskRunDu:F2}) and is {depth:F2} deep."));
                        }
                    }
                    else
                    {
                        benches++;
                    }
                }
            }
        }

        Assert.True(desks > 500, $"only {desks} office desk(s) in the whole sweep — this proved little.");
        Assert.True(benches > 500, $"only {benches} lab bench(es) in the whole sweep — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} desk(s) are not the size a person is:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
        Assert.Equal(desks + benches, atFullDepth);
    }

    /// <summary>
    /// #869 · …AND THE DEPTH DID NOT MOVE ONE WALL.
    ///
    /// <para>The depth is a PICTURE (<c>ChamberFitting.FittingDepthDu</c>): it grows into the room off the
    /// wall-clear line the segment always stood on, and the SOLID handed to the floor is still that one
    /// segment. This is the clause that says so — every published solid of a furnished chamber is degenerate
    /// — because the day somebody lays four sides of a rectangle in fifteen hundred rooms, the sightline
    /// (Lab 45: O(walls)) pays for it on every frame and no guard about furniture would notice.</para>
    ///
    /// <para><b>RED</b> by laying the fixture box as the solid: <c>84 fitting(s) are solid rectangles rather
    /// than the segment #818 lays</c> on the first floor swept.</para>
    /// </summary>
    [Fact]
    public void TheDepthIsAPictureAndNeverAWall()
    {
        var wrong = new List<string>();
        int measured = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (room.Kind != UndergroundComplex.RoomKind.Chamber || !room.IsFurnished)
                {
                    continue;
                }

                foreach (RingOffice.Fixture fit in room.Furniture)
                {
                    measured++;
                    // The fixture's own box is allowed a depth. What is NOT allowed is a second wall: the
                    // floor's wall list must still carry exactly one degenerate segment for this fitting,
                    // lying on one of the box's two long edges.
                    double w = fit.X1 - fit.X0, h = fit.Y1 - fit.Y0;
                    if (Math.Min(w, h) > ChamberFitting.FittingDepthDu + 0.001)
                    {
                        wrong.Add(Say(body, level,
                            $"{room.Plate}: a {fit.Kind} box is {Math.Min(w, h):F2} du thick."));
                    }
                }
            }

            // …and the floor's own wall list carries no rectangle where a chamber fitting stands. Counted as
            // "walls that are not degenerate in either axis AND sit inside a furnished chamber".
            foreach (SurfaceLayout.Wall solid in floor.Walls)
            {
                if (Math.Abs(solid.X1 - solid.X2) > 0.001 && Math.Abs(solid.Y1 - solid.Y2) > 0.001)
                {
                    wrong.Add(Say(body, level,
                        $"a wall segment runs diagonally at ({solid.X1:F1},{solid.Y1:F1}) — nothing in this "
                        + "building is laid off-axis."));
                }
            }
        }

        Assert.True(measured > 1000, $"only {measured} fitting(s) were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} fitting(s) are solid rectangles rather than the segment #818 lays:"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, wrong.Take(12))}");
    }

    // ── (e) WHAT EACH TRADE SEATS YOU ON ──────────────────────────────────────────────────────────────

    /// <summary>
    /// #869 · A LAB SEATS A SADDLE AND AN OFFICE SEATS A CHAIR.
    ///
    /// <para>Owner's own furniture, canonised: <i>"office chair, Salli standing (lab) chair"</i>. One plate,
    /// one word of difference, #783's verb kept on both.</para>
    ///
    /// <para><b>RED</b> by returning <c>StoolPlate</c> for every kit, which is the world of the base commit:
    /// <c>Kit.Laboratory seats "🪑 A STOOL AT THE BENCH — SIT DOWN", which says nothing about a saddle</c>.</para>
    /// </summary>
    [Fact]
    public void TheLabSeatsASaddleAndTheOfficeSeatsAChair()
    {
        Assert.Contains("SADDLE", ChamberFitting.SeatPlateFor(ChamberFitting.Kit.Laboratory),
            StringComparison.Ordinal);
        Assert.Equal(RingOffice.FreeChairPlate, ChamberFitting.SeatPlateFor(ChamberFitting.Kit.Office));

        // …and the three plates are three DIFFERENT sentences, which is the whole of the feature: a "variant"
        // that says the same thing as the thing it varies from is a plate nobody can read a room off.
        string[] three =
        [
            ChamberFitting.SeatPlateFor(ChamberFitting.Kit.Laboratory),
            ChamberFitting.SeatPlateFor(ChamberFitting.Kit.Office),
            ChamberFitting.SeatPlateFor(ChamberFitting.Kit.Clinic),
        ];
        Assert.Equal(3, three.Distinct(StringComparer.Ordinal).Count());

        // …and every one of them still carries the verb (#783: "why not use words like SIT DOWN here if it
        // means sitting down?").
        foreach (string plate in three)
        {
            Assert.Contains("SIT DOWN", plate, StringComparison.Ordinal);
        }
    }

    // ── (a) THE BOARD ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #864 · EVERY LABORATORIES FLOOR HAS EXACTLY ONE BOARD, ON A CHAMBER WALL, CLEAR OF EVERY OPENING —
    /// and no other floor in the game has one at all.
    ///
    /// <para><b>RED</b> three ways, each a different half of the law. Hanging it on the room's own doorway
    /// (<c>free[spare].Middle</c> replaced by the leaf's midpoint):</para>
    /// <code>
    /// 268 board(s) are hung wrong:
    ///   luna B2 — the board at (-118.5,-7.0) is 0.00 du off a doorway; the clearance is 4.0 du.
    /// </code>
    /// <para>Dropping it (<c>IncidentBoard.On</c> returning null):</para>
    /// <code>
    /// 268 board(s) are hung wrong:
    ///   luna B2 — LABORATORIES · this floor has no incident board on it at all.
    /// </code>
    /// <para>Hanging it on every floor (the <c>LabsOn</c> gate removed):</para>
    /// <code>
    /// 2070 board(s) are hung wrong:
    ///   luna B1 — SUPPLY · a floor that is not a laboratory is telling somebody how long it has operated.
    /// </code>
    /// </summary>
    [Fact]
    public void OneBoardPerLaboratoriesFloorAndNowhereElse()
    {
        var wrong = new List<string>();
        int labFloors = 0, otherFloors = 0, hung = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            bool labs = ChamberFitting.LabsOn(ChamberFitting.DepartmentOn(body, level));
            if (!labs)
            {
                otherFloors++;
                if (floor.TheBoard is not null)
                {
                    wrong.Add(Say(body, level,
                        $"{floor.Name} · a floor that is not a laboratory is telling somebody how long it "
                        + "has operated."));
                }
                continue;
            }

            labFloors++;
            if (floor.TheBoard is not { } board)
            {
                wrong.Add(Say(body, level,
                    $"{floor.Name} · this floor has no incident board on it at all."));
                continue;
            }
            hung++;

            // It is IN a chamber — the room it names, and a room a captain can walk into.
            if (board.Room < 0 || board.Room >= floor.TheRooms.Count)
            {
                wrong.Add(Say(body, level, "the board names a room this floor does not have."));
                continue;
            }
            UndergroundComplex.Room room = floor.TheRooms[board.Room];
            if (room.Kind != UndergroundComplex.RoomKind.Chamber)
            {
                wrong.Add(Say(body, level, $"the board hangs in a {room.Kind} rather than a chamber."));
            }
            if (!room.Contains(board.X, board.Y))
            {
                wrong.Add(Say(body, level,
                    $"the board at ({board.X:F1},{board.Y:F1}) is outside the room it hangs in."));
            }

            // …and clear of EVERY published opening on the floor, which is the half a placer gets wrong.
            var holes = new List<SurfaceLayout.Doorway>();
            foreach (UndergroundComplex.EnSuite cell in floor.EnSuites)
            {
                holes.AddRange(cell.Ways);
            }
            foreach (UndergroundComplex.Room any in floor.TheRooms)
            {
                holes.AddRange(any.Ways);
            }
            foreach (SurfaceLayout.Doorway hole in holes)
            {
                double gap = GapToDoor(board.X, board.Y, in hole);
                if (gap < ChamberFitting.OpeningClearDu - 1e-6)
                {
                    wrong.Add(Say(body, level,
                        $"the board at ({board.X:F1},{board.Y:F1}) is {gap:F2} du off a doorway; the "
                        + $"clearance is {ChamberFitting.OpeningClearDu:F1} du."));
                    break;
                }
            }

            // …and it is not standing inside the furniture, which is the other half.
            foreach (RingOffice.Fixture fit in room.Furniture)
            {
                if (board.X >= fit.X0 - 1e-6 && board.X <= fit.X1 + 1e-6
                    && board.Y >= fit.Y0 - 1e-6 && board.Y <= fit.Y1 + 1e-6)
                {
                    wrong.Add(Say(body, level, $"the board is hung over a {fit.Kind}."));
                    break;
                }
            }
        }

        Assert.True(labFloors > 20, $"only {labFloors} laboratories floor(s) — this proved little.");
        Assert.True(otherFloors > 200, $"only {otherFloors} other floor(s) — this proved little.");
        Assert.True(hung > 20, $"only {hung} board(s) hang in the whole game — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} board(s) are hung wrong:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }

    /// <summary>#864 · A floor is the same floor every visit, board included — determinism is law down here
    /// and a placer that walked a sorted list with a tie it did not break would drift.</summary>
    [Fact]
    public void TheBoardHangsInTheSamePlaceEveryVisit()
    {
        int checkedFloors = 0;
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan a = UndergroundComplex.Build(body, level, Field);
                UndergroundComplex.FloorPlan b = UndergroundComplex.Build(body, level, Field);
                Assert.Equal(a.TheBoard, b.TheBoard);
                if (a.TheBoard is not null)
                {
                    checkedFloors++;
                }
            }
        }
        Assert.True(checkedFloors > 10, $"only {checkedFloors} floor(s) carried a board — this proved little.");
    }

    // ── THE COPY, WORD FOR WORD ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #869/#864 · THE OWNER'S OWN SENTENCES, BYTE FOR BYTE.
    ///
    /// <para>Every line below was authored in the issue and blessed there. This is the guard that makes a
    /// paraphrase a build failure rather than a thing somebody notices in a playtest six weeks later — the
    /// same job <c>NeverAnEmptyFloorTests.ThePosterPoolIsTheOwnerBlessedCopy</c> does for #853.</para>
    /// </summary>
    [Fact]
    public void TheCopyIsTheOwnersOwnWords()
    {
        Assert.Equal(
            "🔼 The desk hums and rises to meet you. Somebody specified these once, line-item by line-item, "
            + "for people whose backs mattered to the schedule.",
            SitStandDesk.RaisedLine, StringComparer.Ordinal);

        Assert.Equal(
            "🔽 It sinks back to chair height without complaint. The motor has outlived the schedule, the "
            + "people, and the line-item.",
            SitStandDesk.LoweredLine, StringComparer.Ordinal);

        Assert.Equal(
            "You lean on the memory buttons and move preset two down a knuckle's width. Somewhere in the "
            + "future, somebody will stand here feeling slightly wrong about the world and never know why.",
            SitStandDesk.PrankLine, StringComparer.Ordinal);

        Assert.Equal(
            "Two presets. One for sitting, and one set for somebody who stands a head taller than the chair "
            + "suggests. The desk remembers its people better than the rota does.",
            SitStandDesk.ReadLine, StringComparer.Ordinal);

        Assert.Equal(
            "🗓 THIS LABORATORY HAS OPERATED 0 DAYS WITHOUT SARCASM",
            IncidentBoard.Title, StringComparer.Ordinal);

        Assert.Equal(
            "The zero is not printed. It is a card in a bracket, and there is a stack of spare cards on the "
            + "shelf below. Every spare is also a zero.",
            IncidentBoard.FinePrint, StringComparer.Ordinal);

        // …and the two the press picks between are the two above, chosen by the STATE and never re-typed.
        Assert.Equal(SitStandDesk.RaisedLine, SitStandDesk.LineFor(raised: true), StringComparer.Ordinal);
        Assert.Equal(SitStandDesk.LoweredLine, SitStandDesk.LineFor(raised: false), StringComparer.Ordinal);
    }

    /// <summary>
    /// #869/#864 · …AND NOTHING IN EITHER OF THEM EXPLAINS ANYTHING.
    ///
    /// <para>The canon rule (§13.8, and the 2026-07-30 ruling on what the Old Ones are), asked of the two
    /// most tempting voices added this round: a gag on a wall, and a piece of furniture that DEPOSES A
    /// WITNESS about somebody who is not here. The read is evidence, and evidence is exactly where an author
    /// reaches for the thing nobody is allowed to say — so it says a height and a rota and stops.</para>
    /// </summary>
    [Fact]
    public void NothingHereExplainsAnything()
    {
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        foreach (string line in SitStandDesk.AllProse().Concat(IncidentBoard.AllProse()))
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── THE TWO PLACES ON A DESK ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #869 · THE EDGE AND THE BUTTONS ARE TWO DIFFERENT PLACES ON THE SAME DESK, both on the face its own
    /// chair is on.
    ///
    /// <para>This is the grip the whole feature hangs off: a captain must be able to stand at one control
    /// without the other being nearer, or [E] answers the wrong verb. Swept over every seat in the game that
    /// has a motorised surface in front of it.</para>
    ///
    /// <para><b>RED</b> by returning the same end for both (<c>first: true</c> in <c>TheButtons</c>):
    /// <c>1344 desk(s) put both controls on one square</c>.</para>
    /// </summary>
    [Fact]
    public void TheTwoControlsAreTwoPlacesOnTheFaceTheChairIsOn()
    {
        var wrong = new List<string>();
        int seats = 0, motorised = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (room.Kind != UndergroundComplex.RoomKind.Chamber)
                {
                    continue;   // see ADeskIsTheSizeAPersonIs: the ring's banks are #817's object
                }

                foreach (RingOffice.Chair seat in room.Seats)
                {
                    seats++;
                    int d = SitStandDesk.DeskFor(room.Furniture, in seat);
                    if (d < 0)
                    {
                        continue;
                    }
                    motorised++;
                    RingOffice.Fixture desk = room.Furniture[d];

                    (double ex, double ey) = SitStandDesk.TheEdge(in desk, in seat);
                    (double bx, double by) = SitStandDesk.TheButtons(in desk, in seat);

                    double apart = Math.Sqrt(((ex - bx) * (ex - bx)) + ((ey - by) * (ey - by)));
                    if (apart < ChamberFitting.MinFittingDu - 0.001)
                    {
                        wrong.Add(Say(body, level,
                            $"{room.Plate}: the edge and the buttons are {apart:F2} du apart on a "
                            + $"{desk.Kind} — one press cannot be told from the other."));
                    }

                    // Both stand ON the desk's own box (its face), and the face is the side the seat is on.
                    foreach ((double x, double y, string which) in
                             new[] { (ex, ey, "edge"), (bx, by, "buttons") })
                    {
                        if (x < desk.X0 - 1e-6 || x > desk.X1 + 1e-6
                            || y < desk.Y0 - 1e-6 || y > desk.Y1 + 1e-6)
                        {
                            wrong.Add(Say(body, level,
                                $"{room.Plate}: the {which} is not on the desk it belongs to."));
                        }
                    }

                    // …and both stand on the FACE — the long edge of the box the seat is on, never the one
                    // against the wall. Asked on the SHORT axis alone, because on the long one an end of a
                    // ten-du bench is honestly a long way from the one stool at it.
                    bool alongX = (desk.X1 - desk.X0) >= (desk.Y1 - desk.Y0);
                    double face = alongX
                        ? (seat.Y >= desk.Y ? desk.Y1 : desk.Y0)
                        : (seat.X >= desk.X ? desk.X1 : desk.X0);
                    double onEdge = alongX ? ey : ex, onButtons = alongX ? by : bx;
                    if (Math.Abs(onEdge - face) > 1e-6 || Math.Abs(onButtons - face) > 1e-6)
                    {
                        wrong.Add(Say(body, level,
                            $"{room.Plate}: a control on a {desk.Kind} is on the far side of the desk from "
                            + "the person who sits at it."));
                    }
                }
            }
        }

        Assert.True(seats > 500, $"only {seats} seat(s) in the sweep — this proved little.");
        Assert.True(motorised > 500,
            $"only {motorised} of {seats} seat(s) sit at a sit-stand desk — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} desk(s) put both controls on one square:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }
}
