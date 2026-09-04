using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #775 · LANDSCAPE OFFICES, NOT A DORMITORY — the owner's two beats, stated as laws about the floor the
/// generator actually makes.
///
/// <para>Owner, streamed during the B1 playtest of 2026-08-08:</para>
///
/// <list type="bullet">
/// <item><i>"All the office desks — we should have landscape-style offices on B1 and below floors, with lots
/// of meeting rooms. The current layout is like a dormitory of midship windowless cabins."</i></item>
/// <item><i>"The work done there is lab work and office work related to that lab work, so the space should
/// look like that."</i></item>
/// </list>
///
/// <para>Every guard below walks the REAL generator over the REAL field and is stated against a PUBLISHED
/// list — the ring, the core, their doors, their fittings, their seats — and never against a coordinate
/// typed into this file. A law about a list nobody keeps is a law nobody can fail (#818's own sentence), and
/// a law measured off a number typed here is the house's fifth bug class.</para>
///
/// <para><b>Every number pinned below was watched print</b> over 191 floors of sixteen sites before it was
/// written down, and every one of them is a FLOOR under the measurement rather than the measurement itself:
/// a threshold resting on the case it selects is a threshold that reddens on a rounding.</para>
///
/// <para><b>Proved red.</b> Every guard here was run against <c>IsLandscapeFloor</c> returning false — the
/// tree exactly as it stood before this issue — and eight of the nine went red, each on its own
/// anti-vacuity clause rather than on a coincidence: <c>only 0 landscape floor(s) — this proved little</c>,
/// <c>only 0 laboratories floor(s) — this proved little</c>, <c>only 0 room(s) swept</c>, <c>only 49 block
/// floor(s)</c>. That is the shape a world-shaped law SHOULD fail in when the world it is about is not
/// there, and it is why the clause is in every one of them.</para>
///
/// <para>The ninth — <c>EVERY_DESK_WORK_DEPARTMENT_IsOneThisBuildingActuallyHangs</c> — stayed green,
/// correctly and deliberately: it is a law about the REGISTER (are these real department plates, and does
/// the list select fewer than all of them) rather than about the floors, and the register does not care
/// which floors were cut. A guard that reddened there would be a guard measuring the wrong thing.</para>
/// </summary>
public sealed class TheOfficeFloorIsNotADormitoryTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every floor the building cuts as a landscape office, with the plan it actually built.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.FloorPlan Floor)> EveryOffice()
    {
        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.IsLandscapeFloor(body, level))
                {
                    yield return (body, level, UndergroundComplex.Build(body, level, Field));
                }
            }
        }
    }

    /// <summary>How many floors the sweep must find before any law below means anything. A guard whose
    /// world is empty passes forever, which is the fifth bug class, and this is the one number in the file
    /// that is about the TEST rather than about the building.</summary>
    private const int EnoughFloors = 40;

    /// <summary>
    /// #775 · THE SWEEP CAN TELL PASS FROM FAIL — it found a real building, of both kinds, on both sides of
    /// every fork the laws below are stated across.
    /// </summary>
    [Fact]
    public void THE_SWEEP_FindsBothKindsOfFloorAndEnoughOfEach()
    {
        int offices = 0, labs = 0, dormitories = 0, parks = 0;
        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.HasParkBlock(body, level))
                {
                    parks++;
                }
                else if (UndergroundComplex.IsLandscapeFloor(body, level))
                {
                    offices++;
                    if (ChamberFitting.LabsOn(ChamberFitting.DepartmentOn(body, level)))
                    {
                        labs++;
                    }
                }
                else if (!UndergroundComplex.IsFound(body, level))
                {
                    dormitories++;
                }
            }
        }

        Assert.True(offices >= EnoughFloors, $"only {offices} landscape floor(s) — this would prove little.");
        Assert.True(labs >= 10, $"only {labs} laboratories floor(s) — the lab beat would be untested.");
        Assert.True(dormitories >= EnoughFloors,
            $"only {dormitories} floor(s) kept the cell module — the owner's 'the cabins stay' is untested.");
        Assert.True(parks >= 10, $"only {parks} floor(s) still have a park — #813's block is untested.");
    }

    /// <summary>
    /// #775 BEAT 1 · <b>THE DORMITORY SHAPE IS GONE.</b> The owner's complaint said in the one unit a plan
    /// can be measured in: <i>"the current layout is like a dormitory of midship windowless cabins."</i>
    ///
    /// <para>A dormitory is a floor whose every room is the same small box. So the law is that an office
    /// floor's biggest room is several times the CELL MODULE — <see cref="UndergroundComplex.RoomWidthDu"/>
    /// by <see cref="UndergroundComplex.RoomHeightDu"/>, the module the whole building is made of and the
    /// thing the owner was looking at — and the number is read off that module rather than typed, so a
    /// building that re-scales its cells takes this law with it.</para>
    ///
    /// <para>Measured: the smallest "biggest room" on any landscape floor in the sweep is <b>2,524 du²</b>,
    /// which is fourteen modules. The floor is five, which is daylight and not a fit.</para>
    /// </summary>
    [Fact]
    public void EVERY_OFFICE_FLOOR_HasAnOpenRoomManyTimesTheCellModule()
    {
        double module = UndergroundComplex.RoomWidthDu * UndergroundComplex.RoomHeightDu;
        var wrong = new List<string>();
        int seen = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryOffice())
        {
            seen++;
            double biggest = 0;
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                biggest = Math.Max(biggest, room.FloorDu2);
            }
            if (biggest < module * 5)
            {
                wrong.Add(
                    $"  {body} B{-level}: the biggest room on this floor is {biggest:F0} du² — that is the "
                    + $"cell module ({module:F0}), not an office.");
            }
        }

        Assert.True(seen >= EnoughFloors, $"only {seen} landscape floor(s) — this proved little.");
        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, ["a dormitory floor:", .. wrong]));
    }

    /// <summary>
    /// #775 BEAT 1 · <b>AND PEOPLE SIT AT DESKS IN IT, TOGETHER.</b> An open floor is a shape; what makes it
    /// a landscape OFFICE is banks of desks with places at them — the owner's <i>"the drawn shape says many
    /// people work here together"</i>.
    ///
    /// <para>Stated as: every landscape floor carries at least <see cref="OpenRoomsPerFloor"/> rooms that
    /// hold a <see cref="RingOffice.Fitting.DeskBank"/> and seat at least
    /// <see cref="RingOffice.SeatsPerViewSuite"/> — the ring's own believability bar, read rather than
    /// retyped — and at least one of them seats <see cref="SeatsInTheBiggestRoom"/>, which is a bank you walk
    /// past rather than a desk in a cupboard.</para>
    ///
    /// <para>Measured over 129 landscape floors: the thinnest carries <b>8</b> such rooms and its best one
    /// seats <b>11</b>. Both floors below are pinned under the measurement.</para>
    /// </summary>
    [Fact]
    public void EVERY_OFFICE_FLOOR_SeatsABankOfPeopleWhoCanSeeEachOther()
    {
        var wrong = new List<string>();
        int seen = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryOffice())
        {
            seen++;
            int rooms = 0, best = 0;
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                bool bank = false;
                foreach (RingOffice.Fixture fitting in room.Furniture)
                {
                    bank |= fitting.Kind == RingOffice.Fitting.DeskBank;
                }
                if (bank && room.Seats.Count >= RingOffice.SeatsPerViewSuite)
                {
                    rooms++;
                    best = Math.Max(best, room.Seats.Count);
                }
            }

            if (rooms < OpenRoomsPerFloor || best < SeatsInTheBiggestRoom)
            {
                wrong.Add(
                    $"  {body} B{-level}: {rooms} room(s) of desks and the best of them seats {best} — an "
                    + "office floor is banks of desks with people at them.");
            }
        }

        Assert.True(seen >= EnoughFloors, $"only {seen} landscape floor(s) — this proved little.");
        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, ["a floor nobody works on:", .. wrong]));
    }

    /// <summary>How many rooms of desk banks a landscape floor must carry. Measured at eight on the
    /// thinnest floor in the sweep; pinned at three, so the law has a floor under it rather than resting on
    /// the case it selects.</summary>
    private const int OpenRoomsPerFloor = 3;

    /// <summary>…and how many places the biggest of them seats. Measured at eleven; pinned at eight, which
    /// is still a room you have to walk past people to cross.</summary>
    private const int SeatsInTheBiggestRoom = 8;

    /// <summary>
    /// #775 BEAT 1 · <b>LOTS OF MEETING ROOMS</b>, and every one of them is a room you can go into and sit
    /// down in. Owner: <i>"glass-box rooms off the open floor — the small-table siblings of the #770
    /// negotiation rooms, enterable with the same universal seat verb (#757)."</i>
    ///
    /// <para>Never zero, which is the clause that matters: a feature that is present on most worlds and
    /// silently absent on some is this project's own oldest failure mode. And each of them has a table and
    /// places at it, because a meeting room with nothing in it is the very lie #818 was opened about.</para>
    ///
    /// <para>Measured over 129 landscape floors: <b>8</b> on the thinnest of them, ten places at each
    /// table, 1,032 in the sweep altogether.</para>
    /// </summary>
    [Fact]
    public void EVERY_OFFICE_FLOOR_HasSeveralMeetingRoomsAndNeverNone()
    {
        var wrong = new List<string>();
        int seen = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryOffice())
        {
            seen++;
            int furnished = 0;
            foreach (UndergroundComplex.MeetingRoom cell in floor.TheMeetingRooms)
            {
                bool table = false;
                foreach (RingOffice.Fixture fitting in cell.Furniture)
                {
                    table |= fitting.Kind == RingOffice.Fitting.Table;
                }
                if (table && cell.Seats.Count >= 2)
                {
                    furnished++;
                }
                else
                {
                    wrong.Add(
                        $"  {body} B{-level}: meeting room {cell.Number} has "
                        + $"{cell.Furniture.Count} fitting(s) and {cell.Seats.Count} seat(s).");
                }
            }

            if (furnished < UndergroundComplex.MeetingRoomsPerFloor)
            {
                wrong.Add(
                    $"  {body} B{-level}: {furnished} meeting room(s) with a table in them — the owner "
                    + "asked for lots.");
            }
        }

        Assert.True(seen >= EnoughFloors, $"only {seen} landscape floor(s) — this proved little.");
        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, ["nowhere to hold a meeting:", .. wrong]));
    }

    /// <summary>
    /// #775 BEAT 2 · <b>THE SPACE SAYS WHAT THE WORK IS.</b> Owner: <i>"the work done there is lab work and
    /// office work related to that lab work, so the space should look like that"</i>, and addendum 2:
    /// <i>"a real laboratory is a hall with benches, rigs and walking room, not a cell."</i>
    ///
    /// <para>So a LABORATORIES landscape floor carries benches with the glassware racked along them BESIDE
    /// the desk banks — the same <see cref="RingOffice.Fitting.LabBench"/> a laboratory chamber has carried
    /// since #818, in the block's own back band. And the other half of the law, which is what stops the
    /// fixture from being sprayed everywhere: an ADMINISTRATION floor's own ring has none of them, because
    /// nobody racks a test tube in a quota office.</para>
    ///
    /// <para>Measured over 129 landscape floors: the thinnest listed laboratories floor carries <b>3</b>
    /// rooms of open bench — half of that band's rooms are the closed ones (see
    /// <c>EVERY_OFFICE_FLOOR_KeepsDoorsThatDoNotOpen</c>), and a laboratory with a section you can only
    /// look into is the owner's own isolation gradient rather than a room lost.</para>
    /// </summary>
    [Fact]
    public void A_LABORATORIES_FLOOR_HasBenchesBesideTheDesksAndAnOfficeFloorHasNone()
    {
        var wrong = new List<string>();
        int labFloors = 0, officeFloors = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryOffice())
        {
            // #592 · The band nobody listed HAS no department, so the carve cannot know it is a laboratory
            // and its ring is dressed as ordinary offices. That absence is the whole tell down there and it
            // is not this law's business.
            string? department = ChamberFitting.DepartmentOn(body, level);
            if (department is null)
            {
                continue;
            }

            int benched = 0;
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (room.Kind != UndergroundComplex.RoomKind.RingSuite)
                {
                    continue;   // a chamber's own kit is #818's law and is dealt by department already
                }
                foreach (RingOffice.Fixture fitting in room.Furniture)
                {
                    if (fitting.Kind == RingOffice.Fitting.LabBench)
                    {
                        benched++;
                        break;
                    }
                }
            }

            if (ChamberFitting.LabsOn(department))
            {
                labFloors++;
                if (benched < BenchedRoomsOnALabFloor)
                {
                    wrong.Add(
                        $"  {body} B{-level}: a LABORATORIES floor with {benched} bench room(s) on its "
                        + "ring — that is an office with a lab's plate on the lift.");
                }
            }
            else
            {
                officeFloors++;
                if (benched > 0)
                {
                    wrong.Add(
                        $"  {body} B{-level}: {benched} bench room(s) on a {department} ring — nobody racks "
                        + "a test tube in a quota office.");
                }
            }
        }

        Assert.True(labFloors >= 10, $"only {labFloors} laboratories floor(s) — this proved little.");
        Assert.True(officeFloors >= 10, $"only {officeFloors} office floor(s) — the other arm proved little.");
        Assert.True(wrong.Count == 0,
            string.Join(Environment.NewLine, ["the room does not say what the work is:", .. wrong]));
    }

    /// <summary>How many rooms of laboratory bench a listed LABORATORIES landscape floor must carry, open
    /// and walkable. Measured at three on the thinnest one in the sweep; pinned at two, so the law has a
    /// floor under it rather than resting on the case it selects.</summary>
    private const int BenchedRoomsOnALabFloor = 2;

    /// <summary>
    /// #822 · <b>AND THE FIRE CODE HOLDS ON EVERY ROOM OF EVERY ONE OF THEM.</b> The owner's standing law —
    /// <i>"no space may have only one door except bedroom-small rooms"</i> — asked of the floors this issue
    /// re-cut, so the sweep that proves it is stated beside the change that could break it rather than only
    /// building-wide two files away.
    ///
    /// <para>The meeting rooms are the sharp case and the reason this guard is here: each is a box far past
    /// <see cref="UndergroundComplex.FireCodeSmallRoomDu"/> on its longest side, and its two ways out are
    /// two holes in two DIFFERENT walls — a captain leaves by a wall nobody is standing in.</para>
    /// </summary>
    [Fact]
    public void EVERY_ROOM_ON_AN_OFFICE_FLOOR_HasTwoWaysOut()
    {
        var wrong = new List<string>();
        int rooms = 0, meetings = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryOffice())
        {
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                rooms++;
                if (!room.MeetsFireCode)
                {
                    wrong.Add(
                        $"  {body} B{-level}: {room.Kind} '{room.Plate}' is "
                        + $"{room.LongestSideDu:F1} du on its longest side and has {room.Exits} way(s) out.");
                }
            }

            foreach (UndergroundComplex.MeetingRoom cell in floor.TheMeetingRooms)
            {
                meetings++;
                if (cell.Ways.Count < UndergroundComplex.FireCodeMinExits)
                {
                    wrong.Add(
                        $"  {body} B{-level}: meeting room {cell.Number} has {cell.Ways.Count} leaf/leaves.");
                }

                // …and they are in different walls, which is the stronger half of the law and the reason
                // the core is a row of boxes you can walk THROUGH rather than a row you back out of.
                var faces = new HashSet<double>();
                foreach (SurfaceLayout.Doorway leaf in cell.Ways)
                {
                    faces.Add(Math.Round((leaf.Y1 + leaf.Y2) / 2.0, 3));
                }
                if (faces.Count < 2)
                {
                    wrong.Add(
                        $"  {body} B{-level}: meeting room {cell.Number}'s leaves are all in one wall.");
                }
            }
        }

        Assert.True(rooms >= 500, $"only {rooms} room(s) swept — this proved little.");
        Assert.True(meetings >= 300, $"only {meetings} meeting room(s) swept — this proved little.");
        Assert.True(wrong.Count == 0,
            string.Join(Environment.NewLine, ["a room with one way out:", .. wrong]));
    }

    /// <summary>
    /// #775 · <b>THE FLOOR STILL IMPLIES THE REST OF ITSELF.</b> Owner's oldest note about this building:
    /// <i>"we can again use the locked doors to give the illusion of much larger space."</i>
    ///
    /// <para>The ordinary grid gets that for free — <c>AddRoomsAlong</c> shuts half its chambers. A block cut
    /// entirely of open rooms loses it, and the first cut of this issue did: two locked doors on a whole
    /// floor, which the unlisted band's own guard caught at <c>callisto B10 · nothing implies the rest of
    /// it</c>. So a share of the back band does not open, chosen off the ground rather than rolled — and
    /// because it is chosen off the ground rather than rolled, this law can be stated as EVERY floor rather
    /// than as most of them.</para>
    /// </summary>
    [Fact]
    public void EVERY_OFFICE_FLOOR_KeepsDoorsThatDoNotOpen()
    {
        var wrong = new List<string>();
        int seen = 0, shutSuites = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryOffice())
        {
            seen++;
            foreach (UndergroundComplex.RingRoom suite in floor.TheRing)
            {
                if (!suite.Shut)
                {
                    continue;
                }
                shutSuites++;

                // A shut room is not a space anybody stands in, so it is in NEITHER of the two lists a
                // captain can reach — which is exactly what a locked chamber has always been.
                foreach (UndergroundComplex.Room room in floor.TheRooms)
                {
                    if (Math.Abs(room.X - suite.X) < 0.5 && Math.Abs(room.Y - suite.Y) < 0.5)
                    {
                        wrong.Add(
                            $"  {body} B{-level}: ring room {suite.Number} does not open and is published "
                            + "as a room to walk into.");
                    }
                }

                // …and it is FURNISHED all the same, because its wall onto the core is glass and an empty
                // box behind that glass is the plan admitting the room is scenery.
                if (suite.Furniture.Count == 0)
                {
                    wrong.Add(
                        $"  {body} B{-level}: ring room {suite.Number} is shut AND bare — a captain looks "
                        + "into it through its own glass.");
                }
            }

            if (floor.Locked.Count < LockedDoorsPerFloor)
            {
                wrong.Add(
                    $"  {body} B{-level}: {floor.Locked.Count} door(s) that do not open — nothing implies "
                    + "the rest of it.");
            }
        }

        Assert.True(seen >= EnoughFloors, $"only {seen} landscape floor(s) — this proved little.");
        Assert.True(shutSuites >= 100,
            $"only {shutSuites} shut suite(s) in the whole sweep — this proved little.");
        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, ["a floor with nothing behind it:", .. wrong]));
    }

    /// <summary>The fewest doors that do not open a landscape floor may carry. Three is the number the
    /// unlisted band's own guard has demanded of every floor in the building since #592
    /// (<c>AFloorNobodyListedIsStillAPROPERFloor</c>), read here rather than invented, so the two cannot come
    /// to different opinions about what implies the rest of a building.</summary>
    private const int LockedDoorsPerFloor = 3;

    /// <summary>
    /// #775 · <b>THE PREDICATE IS A LIST OF DEPARTMENTS THIS BUILDING ACTUALLY HANGS</b>, and not a keyword
    /// match on the word OFFICE.
    ///
    /// <para><see cref="UndergroundComplex.PrincipalPlates"/> has the same guard for the same reason: a rule
    /// that selects by accident is the fifth bug class wearing a clever hat. Every entry is proved to be a
    /// department the directory deals, and both stocks are proved to contain some — a list of names nothing
    /// hangs would make <c>IsDeskWork</c> false everywhere and every law above vacuous.</para>
    /// </summary>
    [Fact]
    public void EVERY_DESK_WORK_DEPARTMENT_IsOneThisBuildingActuallyHangs()
    {
        var hung = new HashSet<string>(StringComparer.Ordinal);
        foreach (string body in new[] { "luna", "enceladus" })
        {
            foreach (string department in UndergroundComplex.DepartmentsFor(body))
            {
                hung.Add(department);
            }
        }

        foreach (string department in UndergroundComplex.DeskWorkDepartments)
        {
            Assert.Contains(department, hung);
        }

        // …and the other direction: the list SELECTS, rather than taking everything. A predicate true of
        // every department in the building would have cut a landscape office into the cold rooms and the
        // long store, which is the owner's "the cabins stay" broken by a rule that cannot say no.
        Assert.Contains(hung, d => !UndergroundComplex.IsDeskWork(d));
        Assert.True(
            UndergroundComplex.DeskWorkDepartments.Count < hung.Count,
            "every department in the building is desk work — the predicate selects everything.");

        // The two the owner named by hand.
        Assert.True(UndergroundComplex.IsDeskWork("ADMINISTRATION"));
        Assert.True(UndergroundComplex.IsDeskWork("LABORATORIES"));
        Assert.False(UndergroundComplex.IsDeskWork("LONG STORAGE"));
        Assert.False(UndergroundComplex.IsDeskWork("PLANT"));
    }

    /// <summary>
    /// #775 · <b>AND THE PARK IS STILL THE ONE FLOOR'S.</b> The whole change is that a block and a garden
    /// stopped being one question, so the guard that says so is the one that would catch the change being
    /// made the lazy way: <see cref="UndergroundComplex.HasParkBlock"/> answers exactly what it answered
    /// before, on every floor of every site, and no landscape floor grew grass.
    /// </summary>
    [Fact]
    public void NO_LANDSCAPE_FLOOR_GrewAGarden()
    {
        int blocks = 0, parks = 0;
        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.HasBlockOn(body, level))
                {
                    continue;
                }
                blocks++;

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                bool green = floor.Park is not null;
                Assert.Equal(UndergroundComplex.HasParkBlock(body, level), green);
                Assert.NotEmpty(floor.TheRing);

                if (green)
                {
                    parks++;
                    Assert.Empty(floor.TheMeetingRooms);
                }
                else
                {
                    Assert.NotEmpty(floor.TheMeetingRooms);
                }
            }
        }

        Assert.True(blocks >= 50, $"only {blocks} block floor(s) — this proved little.");
        Assert.True(parks >= 10, $"only {parks} park(s) — the other arm proved little.");
    }
}
