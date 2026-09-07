using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #813 · THE MANHATTAN RULING — the park is the middle of a block, and every side of it is somebody's
/// front wall.
///
/// <para>Owner, 2026-08-09 evening: <i>"The central park needs to be in the center of all the other rooms…
/// not on the side. Think of New York, is the park on one side or is it in the center?"</i> And the clause
/// every guard below is a measurement of: <i>"make sure the park prime real estate is not wasted and not
/// unused, not on any side. It is the best real estate."</i></para>
///
/// <para>#759 shipped the green as a BAND: the hall's glass on one long side and painted rock on the other
/// three. #801 put a row of doors in the far wall, which was the same complaint from inside — <i>"walking
/// through the park is fun, it should not be the edge."</i> This suite is the law that the other three
/// sides can never go back to being a horizon.</para>
///
/// <para>Every guard walks the REAL generator over the REAL field, and every one of them is stated against
/// a PUBLISHED list rather than a coordinate: the ring, the windows, the doorways, the gates. A law about a
/// list nobody keeps is a law nobody can fail, and a law measured off a number typed into the test is the
/// house's fifth bug class.</para>
/// </summary>
public sealed partial class TheParkIsTheCentreOfTheBlockTests
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

    /// <summary>Every floor that has a block on it, with the four things a law here needs.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Hall Hall,
        UndergroundComplex.Park Park, UndergroundComplex.FloorPlan Floor)> EveryBlock()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || !UndergroundComplex.HasParkBlock(body, level))
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            UndergroundComplex.Hall? hall = null;
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (a.Hall is { } h && a.Use == UndergroundComplex.Comfort.UpperCanteen)
                {
                    hall = h;
                }
            }

            Assert.True(hall is not null, $"{body} B{-level}: the block has no hall on its near band.");
            Assert.True(floor.Park is not null, $"{body} B{-level}: the block has no park in the middle.");
            yield return (body, level, hall!.Value, floor.Park!.Value, floor);
        }
    }

    /// <summary>How much of <paramref name="want"/> is left uncovered by <paramref name="pieces"/>, as the
    /// gaps themselves. Interval arithmetic rather than a sampled sweep, because "not one du of frontage is
    /// wasted" is a claim about a measure and a sampler can only ever say "not at the places I looked".</summary>
    private static List<(double Lo, double Hi)> Gaps(
        (double Lo, double Hi) want, IEnumerable<(double Lo, double Hi)> pieces)
    {
        List<(double Lo, double Hi)> sorted = pieces
            .Where(p => p.Hi > want.Lo + 0.001 && p.Lo < want.Hi - 0.001)
            .OrderBy(p => p.Lo)
            .ToList();

        var gaps = new List<(double Lo, double Hi)>();
        double cursor = want.Lo;
        foreach ((double lo, double hi) in sorted)
        {
            if (lo > cursor + 0.01)
            {
                gaps.Add((cursor, lo));
            }
            cursor = Math.Max(cursor, hi);
        }
        if (cursor < want.Hi - 0.01)
        {
            gaps.Add((cursor, want.Hi));
        }
        return gaps;
    }

    /// <summary>The span a ring room presents to the park, in the axis its side runs along.</summary>
    private static (double Lo, double Hi) Frontage(in UndergroundComplex.RingRoom room) =>
        room.Side is UndergroundComplex.RingSide.Near or UndergroundComplex.RingSide.Far
            ? (room.X0, room.X1)
            : (room.Y0, room.Y1);

    /// <summary>Which of the park's four walls a gate is cut in, or null where it is somewhere else.</summary>
    private static UndergroundComplex.RingSide? GateSide(
        in UndergroundComplex.Park park, in SurfaceLayout.Doorway gate)
    {
        bool horizontal = Math.Abs(gate.Y1 - gate.Y2) < 0.001;
        if (horizontal && Math.Abs(gate.Y1 - park.Y1) < 0.001)
        {
            return UndergroundComplex.RingSide.Near;
        }
        if (horizontal && Math.Abs(gate.Y1 - park.Y0) < 0.001)
        {
            return UndergroundComplex.RingSide.Far;
        }
        if (!horizontal && Math.Abs(gate.X1 - park.X0) < 0.001)
        {
            return UndergroundComplex.RingSide.West;
        }
        if (!horizontal && Math.Abs(gate.X1 - park.X1) < 0.001)
        {
            return UndergroundComplex.RingSide.East;
        }
        return null;
    }

    /// <summary>The span a gate presents to the park, in the axis its side runs along — the DOORWAY itself,
    /// which is what a body walks through.</summary>
    private static (double Lo, double Hi) GateSpan(in SurfaceLayout.Doorway gate) =>
        Math.Abs(gate.Y1 - gate.Y2) < 0.001
            ? (Math.Min(gate.X1, gate.X2), Math.Max(gate.X1, gate.X2))
            : (Math.Min(gate.Y1, gate.Y2), Math.Max(gate.Y1, gate.Y2));

    /// <summary>The GROUND a gate stands on, which is wider than the gate: a spur is a corridor, and the
    /// two jambs either side of its doorway are the corridor's own walls arriving at the park. Frontage is
    /// measured against this rather than against <see cref="GateSpan"/>, because the rooms either side of a
    /// gate stop at the CORRIDOR and a law written against the doorway would call a doorjamb "wasted
    /// frontage" — which is a law failing on the thickness of a wall.</summary>
    private static (double Lo, double Hi) GateGround(in SurfaceLayout.Doorway gate)
    {
        (double lo, double hi) = GateSpan(gate);
        double mid = (lo + hi) / 2.0;
        return (mid - UndergroundComplex.CorridorHalf, mid + UndergroundComplex.CorridorHalf);
    }

    // ── (a) CENTRALITY ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PARK IS IN THE MIDDLE OF THE FABRIC — carved building on all four sides of it, and no side of it
    /// anywhere near the edge of the world.
    ///
    /// <para><b>Proven RED</b> by carving only the NEAR band — the far band's loop and the two ends' loop
    /// each given an empty list, which is the green as #759 had it: one glazed side and three that face
    /// whatever the field left:</para>
    /// <code>
    /// 156 park(s) are still on the side of the map:
    ///   luna B1: the FAR side of the park has nothing behind it — 203.0 du of frontage and 0 rooms.
    ///   luna B1: the WEST side of the park has nothing behind it — 45.0 du of frontage and 0 rooms.
    ///   luna B1: the EAST side of the park has nothing behind it — 45.0 du of frontage and 0 rooms.
    ///   phobos B1: the FAR side of the park has nothing behind it — 203.0 du of frontage and 0 rooms.
    /// </code>
    /// </summary>
    [Fact]
    public void ThereIsBuildingOnAllFourSidesOfIt()
    {
        var wrong = new List<string>();
        int blocks = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;

            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                int rooms = park.Frontage.Count(r => r.Side == side && r.HasView);
                bool hallHere = side == UndergroundComplex.RingSide.Near;
                if (rooms == 0 && !hallHere)
                {
                    double run = side is UndergroundComplex.RingSide.Near or UndergroundComplex.RingSide.Far
                        ? park.X1 - park.X0
                        : park.Y1 - park.Y0;
                    wrong.Add($"  {body} B{-level}: the {side.ToString().ToUpperInvariant()} side of the "
                        + $"park has nothing behind it — {run:F1} du of frontage and 0 rooms.");
                }
            }

            // …and the fabric on each side is REAL DEPTH, not a skin. A room a du deep against the edge of
            // the world would satisfy a count and satisfy nothing the owner asked for.
            (string Name, double Off)[] margins =
            [
                ("WEST", park.X0 - (Field.LeftX + SurfaceLayout.EdgeMargin)),
                ("EAST", (Field.RightX - SurfaceLayout.EdgeMargin) - park.X1),
                ("FAR", park.Y0 - (Field.BottomY + SurfaceLayout.EdgeMargin)),
                ("NEAR", (Field.LandingBandY - SurfaceLayout.EdgeMargin) - park.Y1),
            ];
            foreach ((string name, double off) in margins)
            {
                if (off < UndergroundComplex.RingRoomMinDu)
                {
                    wrong.Add($"  {body} B{-level}: the park's {name} wall stands {off:F1} du off the "
                        + "field's own edge.");
                }
            }

            // The hall is one of the ring's rooms and it is on the near band, which is what makes the near
            // side's frontage used even where no office stands.
            if (Math.Abs(hall.Y0 - park.Y1) > 0.001)
            {
                wrong.Add($"  {body} B{-level}: the hall ({hall.Y0:F1}) is not standing on the park's near "
                    + $"wall ({park.Y1:F1}).");
            }
            Assert.NotEmpty(floor.RoomCentres);
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) are still on the side of the map:\n" + string.Join("\n", wrong));
    }

    // ── (b) THE RING IS COMPLETE ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NOT ONE DU OF THE PARK'S PERIMETER IS WASTED. Every side of it, end to end, is either a room's front
    /// wall or a gate — the owner's "not unused, not on any side", in the one unit a guard can measure.
    ///
    /// <para><b>Proven RED</b> by laying the far band's rooms at <see cref="UndergroundComplex.RingRoomTargetDu"/>
    /// each from the start of their segment and letting the remainder be whatever it turned out to be —
    /// which is the tempting version of this carve, and the one that leaves a strip of the best frontage in
    /// the building facing nothing:</para>
    /// <code>
    /// 156 park(s) have frontage facing nothing:
    ///   luna B1: 8.8 du of the FAR wall (-66.5…-57.7) is neither a room nor a gate.
    ///   luna B1: 2.2 du of the FAR wall (-10.7…-8.5) is neither a room nor a gate.
    ///   luna B1: 18.0 du of the FAR wall (78.5…96.5) is neither a room nor a gate.
    ///   phobos B1: 8.8 du of the FAR wall (-66.5…-57.7) is neither a room nor a gate.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryDuOfTheFrontageIsARoomOrAGate()
    {
        var wrong = new List<string>();
        int blocks = 0, du = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;

            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                bool horizontal = side is UndergroundComplex.RingSide.Near
                    or UndergroundComplex.RingSide.Far;
                (double Lo, double Hi) wall = horizontal ? (park.X0, park.X1) : (park.Y0, park.Y1);

                var pieces = new List<(double Lo, double Hi)>();
                foreach (UndergroundComplex.RingRoom r in park.Frontage)
                {
                    if (r.Side == side && r.HasView)
                    {
                        pieces.Add(Frontage(r));
                    }
                }
                if (side == UndergroundComplex.RingSide.Near)
                {
                    pieces.Add((hall.X0, hall.X1));   // the hall's own glass, laid by the hall's carver
                }
                foreach (SurfaceLayout.Doorway g in park.Ways)
                {
                    if (GateSide(park, g) == side)
                    {
                        pieces.Add(GateGround(g));
                    }
                }

                du += (int)(wall.Hi - wall.Lo);
                foreach ((double lo, double hi) in Gaps(wall, pieces))
                {
                    wrong.Add($"  {body} B{-level}: {hi - lo:F1} du of the "
                        + $"{side.ToString().ToUpperInvariant()} wall ({lo:F1}…{hi:F1}) is neither a room "
                        + "nor a gate.");
                }
            }

            Assert.NotEmpty(floor.Windows!);
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(du > 20_000, $"only {du} du of frontage was walked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) have frontage facing nothing:\n" + string.Join("\n", wrong));
    }
}
