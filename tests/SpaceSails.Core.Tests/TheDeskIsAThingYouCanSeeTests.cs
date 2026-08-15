using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #868 · JUST SAY TABLE — the four things a room has to be able to say about a chair standing in it.
///
/// <para>Owner, 2026-08-13, sitting at an office chair in <c>❄ COLD ROOM · TO CANTEEN 1</c> on the back
/// street of B1 and reading the room off the plan:</para>
///
/// <list type="bullet">
/// <item><i>"The graphics kind of does not show there being a table"</i></item>
/// <item><i>"The bench is a line"</i> · <i>"and too far to use as a bench"</i></item>
/// <item><i>"The Shelving is clear as furniture goes"</i> — the positive control, three paces away</item>
/// <item><i>"could the table just be a different color rectangle in front of the chair, so arms (and papers)
/// could rest on it?"</i> → <i>"I think table should be similar just say table."</i></item>
/// </list>
///
/// <para>…while the sentence the game printed as he sat down said <i>the worktop is clear, the screen is
/// dark, and the whole wall in front of you is the garden</i>. There was no worktop. There was no screen.
/// The room was the back of house behind a canteen.</para>
///
/// <h3>Which end was lying</h3>
///
/// <para><b>Core's.</b> Not the renderer's — the deck drew exactly what it was handed, and what it was
/// handed for a plain room was a run of shelving, a degenerate bench box and two chairs. There was no desk
/// in the room to fail to draw, and the sit line was reading the PLATE, which says nothing about what is
/// standing on a floor. So all four guards below are stated against PUBLISHED GEOMETRY and measured here
/// rather than asked of the very predicate that ships — a guard that asks the code the same question the
/// code asks itself is the house's fifth bug class wearing a clean shirt.</para>
///
/// <para>Scoped to the RING, which is where the owner was standing. The building's other rooms are furnished
/// by <see cref="ChamberFitting"/>, whose kit is laid as SEGMENTS on purpose — see that file's class summary
/// on Lab 45 and what four rectangles per room in fifteen hundred rooms would cost a frame.</para>
/// </summary>
public sealed class TheDeskIsAThingYouCanSeeTests
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

    /// <summary>Every ring room of every block floor in the sweep, walked through the REAL generator.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.RingRoom Room)> EveryRingRoom()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || !UndergroundComplex.HasParkBlock(body, level))
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            if (floor.Park is not { } park)
            {
                continue;
            }
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                yield return (body, level, room);
            }
        }
    }

    /// <summary>How far a point is from a published box. The one measurement every clearance below is stated
    /// in, written here rather than borrowed from Core so that the code under test and the guard over it are
    /// not the same arithmetic twice.</summary>
    private static double ToBox(double px, double py, in RingOffice.Fixture f)
    {
        double dx = Math.Max(Math.Max(f.X0 - px, px - f.X1), 0.0);
        double dy = Math.Max(Math.Max(f.Y0 - py, py - f.Y1), 0.0);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static bool Inside(double px, double py, in RingOffice.Fixture f) =>
        px >= f.X0 - 0.01 && px <= f.X1 + 0.01 && py >= f.Y0 - 0.01 && py <= f.Y1 + 0.01;

    /// <summary>Is there something somebody works at within arm's reach of this seat? Measured off the boxes,
    /// at the very setback the placer lays a chair at.</summary>
    private static bool SurfaceInReach(in UndergroundComplex.RingRoom room, RingOffice.Chair c)
    {
        foreach (RingOffice.Fixture f in room.Furniture)
        {
            if (RingOffice.IsWorkSurface(f.Kind) && ToBox(c.X, c.Y, in f) <= RingOffice.ChairSetbackDu + 0.01)
            {
                return true;
            }
        }
        return false;
    }

    // ── (a) A CHAIR WITH NOTHING TO WORK AT ───────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY SEAT HAS A WORKTOP WITHIN ARM'S REACH — and a seat that has not got one is never told it has.
    ///
    /// <para>Two clauses, and they fail on two different reverts on purpose. The first is the owner's
    /// finding as arithmetic: a plain room published two chairs and not one thing to sit at, and a place to
    /// WAIT (a privacy booth you are inside, the bench in the block's washroom) is a published fact about the
    /// room rather than a room that forgot its desk. The second is the sentence: the clause <i>the worktop is
    /// clear</i> is reachable only where a worktop actually is.</para>
    ///
    /// <para><b>Proven RED</b> (clause one) by dropping the worktop out of <c>RingOffice.Plain</c> — the very
    /// piece this issue adds:</para>
    /// <code>
    /// 364 seat(s) have nothing to sit at:
    ///   luna B1: room 4 (NEAR) — RECOVERY 2 — chair 0 has no work surface within 2.4 du, and it is not a
    ///   booth's seat or a washroom's bench.
    ///   luna B1: room 5 (FAR) — 🚿 WASH-DOWN — chair 0 has no work surface within 2.4 du, and it is not a
    ///   booth's seat or a washroom's bench.
    /// </code>
    ///
    /// <para><b>Proven RED</b> (clause two) by hard-wiring the sentence back to a desk
    /// (<c>desk = true;</c> at the top of <c>RingOffice.TookAChairLine</c>):</para>
    /// <code>
    /// 312 seat(s) have nothing to sit at:
    ///   luna B1: room 1 (NEAR) — 🚻 PUBLIC WASHROOMS · NO PASS REQUIRED — chair 0 is told "You pull out a
    ///   chair and sit down at somebody's desk in 🚻 PUBLIC WASHROOMS · NO PASS REQUIRED. The worktop is
    ///   clear, …" with no worktop within 2.4 du of it.
    ///   luna B1: room 2 (NEAR) — SENIOR ROTA · GREEN SIDE — chair 0 is told "… The worktop is clear, the
    ///   screen is dark, and the whole wall in front of you is the garden." with no worktop within 2.4 du
    ///   of it.
    /// </code>
    ///
    /// <para>…and the second of those is worth reading twice: it is a PRIVACY BOOTH in a premium suite. The
    /// clause was being told to somebody sitting in a phone box.</para>
    /// </summary>
    [Fact]
    public void EverySeatHasAWorktopInReachOrIsNeverToldItHas()
    {
        var wrong = new List<string>();
        int seats = 0, atADesk = 0, waiting = 0;

        foreach ((string body, int level, UndergroundComplex.RingRoom room) in EveryRingRoom())
        {
            foreach (RingOffice.Chair c in room.Seats)
            {
                seats++;
                string what = $"  {body} B{-level}: room {room.Number} "
                    + $"({room.Side.ToString().ToUpperInvariant()}) — {room.Plate} — chair {c.Index}";

                bool surface = SurfaceInReach(in room, c);
                bool inABooth = room.Furniture.Any(
                    f => f.Kind == RingOffice.Fitting.Booth && Inside(c.X, c.Y, in f));
                bool waits = inABooth || RingOffice.IsWashroom(in room);

                if (surface)
                {
                    atADesk++;
                }
                else if (waits)
                {
                    waiting++;
                }
                else
                {
                    wrong.Add($"{what} has no work surface within "
                        + $"{RingOffice.ChairSetbackDu:F1} du, and it is not a booth's seat or a "
                        + "washroom's bench.");
                }

                // …and the SENTENCE, taken through the very path the client shows it on.
                string line = RingOffice.TookAChairLine(
                    room.Plate, RingOffice.LooksAtTheGarden(in room),
                    RingOffice.WorksAtASurface(in room, in c));
                bool namesADesk = line.Contains("worktop", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("desk", StringComparison.OrdinalIgnoreCase);
                if (namesADesk && !surface)
                {
                    wrong.Add($"{what} is told \"{line}\" with no worktop within "
                        + $"{RingOffice.ChairSetbackDu:F1} du of it.");
                }
            }
        }

        Assert.True(seats > 1500, $"only {seats} ring seat(s) were measured — this proved little.");
        Assert.True(atADesk > 1000, $"only {atADesk} seat(s) were at a work surface at all.");
        Assert.True(waiting > 100,
            $"only {waiting} seat(s) were places to WAIT — the half of this guard that proves the exemption "
            + "is not a blanket never ran.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} seat(s) have nothing to sit at:\n" + string.Join("\n", wrong.Take(20)));
    }

    // ── (b) THE GARDEN, NAMED IN A ROOM THAT HAS NOT GOT ONE ──────────────────────────────────────────

    /// <summary>
    /// THE GARDEN IS ONLY EVER NAMED IN A ROOM THAT FACES IT.
    ///
    /// <para>The owner's own finding: the view-suite line firing in a windowless cold store. What makes it
    /// answerable is that <i>looks at the garden</i> is a fact about the DRESSING and not about a pane of
    /// glass — the far band's park face IS glass with the gravel gate cut through it, so a test written on
    /// the geometry alone would go on saying yes in the very room the complaint came from. The rooms that
    /// look at the green are the rooms somebody rented for the aspect, which is exactly the set
    /// <c>DressingFor</c> does NOT dress plain.</para>
    ///
    /// <para><b>Proven RED</b> by putting the clause back where it was
    /// (<c>public static bool LooksAtTheGarden(...) =&gt; true;</c>):</para>
    /// <code>
    /// 364 seat(s) are told about a garden they cannot see:
    ///   luna B1: room 4 (NEAR) — RECOVERY 2 — chair 0 is told the whole wall in front of it is the garden,
    ///   in a room dressed Plain.
    ///   luna B1: room 9 (FAR) — ❄ COLD ROOM · TO CANTEEN 1 — chair 0 is told the whole wall in front of it
    ///   is the garden, in a room dressed Plain.
    /// </code>
    /// </summary>
    [Fact]
    public void TheGardenIsOnlyEverNamedInARoomThatFacesIt()
    {
        var wrong = new List<string>();
        int seats = 0, gardens = 0, backs = 0;

        foreach ((string body, int level, UndergroundComplex.RingRoom room) in EveryRingRoom())
        {
            // What the guard believes about this room, measured here off the room's own published facts
            // rather than asked of the predicate under test: a suite with the view, on a band that is not
            // the back of house, and not the one room on the ring that is a washroom.
            bool faces = room.HasView
                && room.Side != UndergroundComplex.RingSide.Far
                && !RingOffice.IsWashroom(in room);

            foreach (RingOffice.Chair c in room.Seats)
            {
                seats++;
                string line = RingOffice.TookAChairLine(
                    room.Plate, RingOffice.LooksAtTheGarden(in room),
                    RingOffice.WorksAtASurface(in room, in c));

                bool namesTheGarden = line.Contains("garden", StringComparison.OrdinalIgnoreCase);
                if (namesTheGarden)
                {
                    gardens++;
                }
                else
                {
                    backs++;
                }

                if (namesTheGarden && !faces)
                {
                    wrong.Add($"  {body} B{-level}: room {room.Number} "
                        + $"({room.Side.ToString().ToUpperInvariant()}) — {room.Plate} — chair {c.Index} "
                        + "is told the whole wall in front of it is the garden, in a room dressed "
                        + $"{RingOffice.DressingFor(in room)}.");
                }
            }
        }

        Assert.True(seats > 1500, $"only {seats} ring seat(s) were measured — this proved little.");
        Assert.True(gardens > 500,
            $"only {gardens} seat(s) were told about the garden at all — a guard that never sees the clause "
            + "it exists to bound is a guard that cannot fail.");
        Assert.True(backs > 100,
            $"only {backs} seat(s) were in rooms with no garden — the half of this guard that BITES never "
            + "ran.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} seat(s) are told about a garden they cannot see:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    // ── (c) A CHAIR FACING AN EMPTY ROOM ──────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY WORKING SEAT FACES THE THING IT WORKS AT.
    ///
    /// <para>#817's placement law — <i>a chair faces its desk</i> — extended past the desk banks to the whole
    /// ring, and it is the sharpest of the four because it is the one the owner was actually looking at when
    /// he said <i>too far to use as a bench</i>: a plain room's chairs faced the GLASS with the bench behind
    /// them and five du of empty aisle in front. A seat with its back to the only fixture in reach is not a
    /// place to work, whatever is drawn under it.</para>
    ///
    /// <para>Measured by MARCHING along the published facing vector, which is what a body at that seat is
    /// looking down. A dot product would pass a chair aimed at the corner of a desk on the far side of a
    /// room; this asks whether the surface is actually there.</para>
    ///
    /// <para><b>Proven RED</b> by facing a plain room's chair at the glass again
    /// (<c>lay.Chair(in room, seatU, (vA + setV1) / 2.0, lay.Frame.TowardTheGlass);</c>):</para>
    /// <code>
    /// 364 seat(s) are looking at an empty floor:
    ///   luna B1: room 4 (NEAR) — RECOVERY 2 — chair 0 at (111.8,-165.0) faces (0.0,-1.0) and there is no
    ///   work surface anywhere down that line.
    ///   luna B1: room 9 (FAR) — ❄ COLD ROOM · TO CANTEEN 1 — chair 0 at (90.6,-259.5) faces (0.0,1.0) and
    ///   there is no work surface anywhere down that line.
    /// </code>
    ///
    /// <para>…and it went red the first time it was ever run, on code nobody had touched: every NEGOTIATION
    /// ROOM on the ring's EAST band seated four people facing away from their own table. See
    /// <c>RingOffice.LongTable</c> — the facing was a rotation of the glass normal, which is +u on three
    /// bands and −u on the one whose depth axis runs backwards.</para>
    /// </summary>
    [Fact]
    public void EveryWorkingSeatFacesTheSurfaceItWorksAt()
    {
        var wrong = new List<string>();
        int seats = 0;

        foreach ((string body, int level, UndergroundComplex.RingRoom room) in EveryRingRoom())
        {
            foreach (RingOffice.Chair c in room.Seats)
            {
                if (!SurfaceInReach(in room, c))
                {
                    continue;   // a place to wait. It is not facing anything and is not pretending to.
                }
                seats++;

                // Down the line the body is looking, as far as a chair is ever laid off a worktop plus a
                // body — anything further away is not something you can put your arms on.
                bool lands = false;
                for (double r = 0.1; r <= RingOffice.ChairSetbackDu + 0.8 && !lands; r += 0.1)
                {
                    double px = c.X + (c.FaceX * r), py = c.Y + (c.FaceY * r);
                    lands = room.Furniture.Any(
                        f => RingOffice.IsWorkSurface(f.Kind) && Inside(px, py, in f));
                }

                if (!lands)
                {
                    wrong.Add($"  {body} B{-level}: room {room.Number} "
                        + $"({room.Side.ToString().ToUpperInvariant()}) — {room.Plate} — chair {c.Index} at "
                        + $"({c.X:F1},{c.Y:F1}) faces ({c.FaceX:F1},{c.FaceY:F1}) and there is no work "
                        + "surface anywhere down that line.");
                }
            }
        }

        Assert.True(seats > 1000, $"only {seats} working seat(s) were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} seat(s) are looking at an empty floor:\n" + string.Join("\n", wrong.Take(20)));
    }

    // ── (d) A PIECE OF FURNITURE DRAWN AS ONE STROKE ──────────────────────────────────────────────────

    /// <summary>
    /// NOTHING ON THE RING'S FLOOR IS A STROKE, AND NO BENCH STANDS IN THE MIDDLE OF IT.
    ///
    /// <para>Owner: <i>"The bench is a line."</i> It was a DEGENERATE BOX, which <c>Lay.Box</c> honestly lays
    /// as one segment, and one segment is one stroke — while the shelving beside it, a real rectangle, was
    /// the thing he pointed at and called clear. So every published fitting on the ring has two dimensions,
    /// and the deck has a rectangle to fill (the client's own half of this is
    /// <c>TheFurnitureIsDrawnTests</c>).</para>
    ///
    /// <para>The second clause is the spacing ruling — <i>too far to use as a bench</i>: a bench stands
    /// against a WALL (a pier's own clearance, <see cref="RingOffice.PierClearDu"/>) or against a SURFACE
    /// somebody sits at it for, and never floating in the middle of a floor.</para>
    ///
    /// <para>A PARTITION is exempt and stays a segment: it is a screen between two workstations, and a screen
    /// IS a line. A CUBICLE and a BOOTH are exempt from the spacing clause because they are little rooms
    /// rather than furniture.</para>
    ///
    /// <para><b>Proven RED</b> by laying the plain room's bench flat again
    /// (<c>lay.Box(Fitting.Bench, benchLo, vA, uB, vA, BenchPlate, Seating.OneSide);</c>):</para>
    /// <code>
    /// 208 fitting(s) are a stroke or a float:
    ///   luna B1: room 6 (FAR) — 📋 GROUNDS OFFICE · ROTA POSTED — its Bench is 2.0 x 0.0 du: a stroke, not
    ///   a piece of furniture.
    ///   luna B1: room 7 (FAR) — 🌱 POTTING · SOIL, TRAYS, GRIT — its Bench is 2.0 x 0.0 du: a stroke, not a
    ///   piece of furniture.
    /// </code>
    /// </summary>
    [Fact]
    public void NoRingFittingIsAStrokeAndNoBenchFloats()
    {
        var wrong = new List<string>();
        int fittings = 0, benches = 0;

        foreach ((string body, int level, UndergroundComplex.RingRoom room) in EveryRingRoom())
        {
            foreach (RingOffice.Fixture f in room.Furniture)
            {
                if (f.Kind == RingOffice.Fitting.Partition)
                {
                    continue;   // a screen between two workstations. It IS a line.
                }
                fittings++;

                string what = $"  {body} B{-level}: room {room.Number} "
                    + $"({room.Side.ToString().ToUpperInvariant()}) — {room.Plate} — its {f.Kind}";

                double w = f.X1 - f.X0, h = f.Y1 - f.Y0;
                if (w < 0.01 || h < 0.01)
                {
                    wrong.Add($"{what} is {w:F1} x {h:F1} du: a stroke, not a piece of furniture.");
                }

                if (f.Kind != RingOffice.Fitting.Bench)
                {
                    continue;
                }
                benches++;

                // Against one of the room's own four walls…
                double toWall = new[]
                {
                    Math.Abs(f.X0 - room.X0), Math.Abs(room.X1 - f.X1),
                    Math.Abs(f.Y0 - room.Y0), Math.Abs(room.Y1 - f.Y1),
                }.Min();

                // …or against something somebody sits at it for.
                double toFixture = double.MaxValue;
                foreach (RingOffice.Fixture other in room.Furniture)
                {
                    if (other.Kind == RingOffice.Fitting.Bench || !RingOffice.IsWorkSurface(other.Kind))
                    {
                        continue;
                    }
                    double dx = Math.Max(Math.Max(other.X0 - f.X1, f.X0 - other.X1), 0.0);
                    double dy = Math.Max(Math.Max(other.Y0 - f.Y1, f.Y0 - other.Y1), 0.0);
                    toFixture = Math.Min(toFixture, Math.Sqrt((dx * dx) + (dy * dy)));
                }

                if (toWall > RingOffice.PierClearDu + 0.01
                    && toFixture > (2 * RingOffice.ChairSetbackDu) + 0.01)
                {
                    wrong.Add($"{what} stands {toWall:F1} du off the nearest wall and {toFixture:F1} du off "
                        + "the nearest worktop — it is floating in the middle of the floor.");
                }
            }
        }

        Assert.True(fittings > 600, $"only {fittings} ring fitting(s) were measured — this proved little.");
        Assert.True(benches > 40,
            $"only {benches} bench(es) were met — the fixture this guard was opened about barely ran.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} fitting(s) are a stroke or a float:\n" + string.Join("\n", wrong.Take(20)));
    }
}
