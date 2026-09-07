using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #813 · <b>THE BLOCK IS ALL OF ITS OWN SIDE</b> — sections (f), (g) and (g1) of
/// <see cref="TheParkIsTheCentreOfTheBlockTests"/>.
///
/// <para>What this part owns is the block's FOOTPRINT: the goods car standing at the thin end of it, the
/// block owning its whole side of the spine and never the cages' side — and (g1), the same two laws re-asked
/// on ground the generator has never been handed, so that neither of them can be a fact about the thirteen
/// bodies that happen to be in the sweep.</para>
/// </summary>
public sealed partial class TheParkIsTheCentreOfTheBlockTests
{
    // ── (f) THE CAR GOES WHERE THE BUILDING IS THINNEST ───────────────────────────────────────────────

    /// <summary>
    /// THE RING'S DENSITY DECIDES WHICH END OF THE BLOCK THE GOODS CAR STANDS AT, and not the other way
    /// round — the owner's clause 4, and the one law here that had to be shown CHOOSING rather than merely
    /// agreeing with where the car already was.
    ///
    /// <para>So it is asked twice: once of the shipped ground, and once of a ground whose asymmetry runs the
    /// other way. <see cref="UndergroundComplex.RibColumnsOn"/> drops the rib column nearest the cage, and
    /// that dropped column is what makes one half of the block carry more unbroken frontage than the other.
    /// Move the cage and a DIFFERENT column is dropped — and the car has to move with it. A rule that
    /// answered "west" on both fields would be a rule that was not reading anything.</para>
    /// </summary>
    [Fact]
    public void TheGoodsCarStandsAtTheLessBuiltEndOfTheBlock()
    {
        // ── THE SHIPPED GROUND. The gates fall west of the park's middle, so the west half carries less
        //    unbroken frontage, so the car is at the west end.
        (double west, double east) = UndergroundComplex.RingFrontageOn(Field);
        Assert.True(west < east,
            $"the shipped field is symmetric ({west:F1} vs {east:F1}) — this law cannot be shown choosing.");

        UndergroundComplex.ParkBlock block = UndergroundComplex.BlockOn(Field);
        (double X, double Y)? car = UndergroundComplex.ServiceShaftAt(Field);
        Assert.NotNull(car);
        Assert.True(car!.Value.X < block.WestOuterX,
            $"the car stands at {car.Value.X:F1}, which is not outside the block's west street "
            + $"({block.WestOuterX:F1}) — the thinner end.");

        // …and it is hard against that street rather than adrift in the rock behind it.
        Assert.True(
            block.WestOuterX - car.Value.X
                < UndergroundComplex.ShaftHalf + UndergroundComplex.ShaftClearDu + 1.0,
            $"the car stands {block.WestOuterX - car.Value.X:F1} du off the street it serves.");

        // ── AND THE MIRROR. Move the cage so RibColumnsOn drops a different column: the gates now fall EAST
        //    of the middle, the east half is the thinner one, and the car must cross the building.
        SurfaceLayout.Field flipped = Field with { AnchorX = -94 };
        (double fw, double fe) = UndergroundComplex.RingFrontageOn(flipped);
        Assert.True(fe < fw,
            $"the mirrored ground did not flip the asymmetry ({fw:F1} vs {fe:F1}) — this proved nothing.");

        UndergroundComplex.ParkBlock flippedBlock = UndergroundComplex.BlockOn(flipped);
        (double X, double Y)? flippedCar = UndergroundComplex.ServiceShaftAt(flipped);
        Assert.NotNull(flippedCar);
        Assert.True(flippedCar!.Value.X > flippedBlock.EastOuterX,
            $"the ground's thin end moved and the car did not: it is still at {flippedCar.Value.X:F1}, "
            + $"west of the east street at {flippedBlock.EastOuterX:F1}.");

        // Whatever end it takes, it is still a car a captain cannot see from the other one (#801).
        Assert.True(
            Math.Abs(flippedCar.Value.X - UndergroundComplex.ShaftAt(flipped).X)
                >= UndergroundComplex.MinShaftSeparationOn(flipped));
    }

    // ── (g) THE BLOCK IS ALL OF ITS OWN SIDE ──────────────────────────────────────────────────────────

    /// <summary>
    /// NOTHING ELSE IS BUILT ON THE BLOCK'S SIDE OF THE SPINE. Every cross corridor on the block's floor
    /// either runs away from it into the ordinary grid, or IS one of its gates — because a rib running down
    /// anywhere else would arrive in the middle of somebody's office.
    ///
    /// <para>And the side it takes is the one the cage's alcove does not, every time: the alcove hangs off
    /// the spine's upper face, and the block would otherwise have to be carved around the captain's own way
    /// home.</para>
    /// </summary>
    [Fact]
    public void TheBlockOwnsItsWholeSideOfTheSpineAndNeverTheCagesSide()
    {
        var wrong = new List<string>();
        int blocks = 0;

        foreach ((string body, int level, UndergroundComplex.Hall _, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;
            UndergroundComplex.ParkBlock block = UndergroundComplex.BlockOn(Field);
            (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(Field);

            // The cage's alcove is on the far side of the spine from the green, always.
            if (park.Y1 > shaftY)
            {
                wrong.Add($"  {body} B{-level}: the block is on the cage's own side of the spine.");
            }

            foreach (UndergroundComplex.Rib rib in floor.Ribs)
            {
                if (!rib.Down)
                {
                    continue;
                }
                if (!block.SpurXs.Any(sx => Math.Abs(sx - rib.X) < 0.001))
                {
                    wrong.Add($"  {body} B{-level}: a rib at x={rib.X:F1} runs into the block and is not "
                        + "one of its gates.");
                }
            }

            // …and the gates really are the columns the cross corridors stand on, so a gate and a corridor
            // can never disagree about where the crossings are.
            foreach (double sx in block.SpurXs)
            {
                Assert.Contains(floor.Ribs, r => Math.Abs(r.X - sx) < 0.001 && r.Down);
                Assert.True(Math.Abs(sx - shaftX) > UndergroundComplex.ShaftHalf,
                    $"{body} B{-level}: a gate is cut down the lift's own column.");
            }
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} floor(s) build on the block's ground:\n" + string.Join("\n", wrong));
    }

    // ── (g1) THE SAME LAWS, ON GROUND THE BLOCK HAS NEVER SEEN ────────────────────────────────────────

    /// <summary>
    /// THE BLOCK IS A FUNCTION OF THE GROUND, SO IT IS ASKED OF MORE THAN ONE PIECE OF GROUND.
    ///
    /// <para>Every other guard in this file sweeps fifty-odd sites — and every one of them lays the SAME
    /// block, because the block's geometry is a pure function of the field and the game ships one field.
    /// That is right for the fiction (one company drawing, poured at every site: the branch offices differ
    /// in what is behind the doors, never in where the doors are) and it is a trap for a test suite. Fifty
    /// floors of identical geometry prove one floor of geometry fifty times.</para>
    ///
    /// <para>So the laws are asked again of grounds the generator has never been handed: narrower, deeper,
    /// shallower, and one with the cage moved far enough to change which rib column
    /// <see cref="UndergroundComplex.RibColumnsOn"/> drops — which moves the gates, which moves the
    /// segments, which moves every room on the ring. If the block only works on 310 × 260, this is where
    /// that is found out.</para>
    ///
    /// <para><b>Proven RED</b> by the same break as
    /// <see cref="EveryDuOfTheFrontageIsARoomOrAGate"/> — the far band laid at a fixed room width with the
    /// remainder left over. On the shipped field that leaves 8.8 du facing nothing; on the narrow field it
    /// leaves a different number, which is the point of asking twice.</para>
    /// </summary>
    [Fact]
    public void TheLawsHoldOnGroundTheGeneratorHasNeverBeenHanded()
    {
        (string Name, SurfaceLayout.Field Field)[] grounds =
        [
            ("the shipped field", Field),
            ("a narrower field", Field with { LeftX = -130, RightX = 120 }),
            ("a much narrower field", Field with { LeftX = -100, RightX = 90 }),
            ("a much wider field", Field with { LeftX = -220, RightX = 210 }),
            ("a deeper field", Field with { BottomY = -320 }),
            ("a shallower field", Field with { BottomY = -258 }),
            ("the cage moved west", Field with { AnchorX = -94 }),
        ];

        var wrong = new List<string>();
        var shapes = new HashSet<string>(StringComparer.Ordinal);

        foreach ((string name, SurfaceLayout.Field ground) in grounds)
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build("luna", -1, ground);
            if (floor.Park is not { } park)
            {
                wrong.Add($"  {name}: no block was carved at all.");
                continue;
            }

            UndergroundComplex.Hall? hall = null;
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (a.Hall is { } h && a.Use == UndergroundComplex.Comfort.UpperCanteen)
                {
                    hall = h;
                }
            }
            if (hall is not { } bar)
            {
                wrong.Add($"  {name}: a block with no hall on its near band.");
                continue;
            }

            // The shape this ground produced, so the guard can say out loud that it met more than one.
            shapes.Add($"{park.X1 - park.X0:F1}x{park.Y1 - park.Y0:F1}/{park.Frontage.Count}");

            // LAW 1 · a room on every side.
            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                if (side != UndergroundComplex.RingSide.Near
                    && !park.Frontage.Any(r => r.Side == side && r.HasView))
                {
                    wrong.Add($"  {name}: the {side.ToString().ToUpperInvariant()} side faces nothing.");
                }
            }

            // LAW 2 · not one du of the perimeter wasted.
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
                    pieces.Add((bar.X0, bar.X1));
                }
                foreach (SurfaceLayout.Doorway g in park.Ways)
                {
                    if (GateSide(park, g) == side)
                    {
                        pieces.Add(GateGround(g));
                    }
                }
                foreach ((double lo, double hi) in Gaps(wall, pieces))
                {
                    wrong.Add($"  {name}: {hi - lo:F1} du of the "
                        + $"{side.ToString().ToUpperInvariant()} wall ({lo:F1}…{hi:F1}) faces nothing.");
                }
            }

            // LAW 3 · every room opens onto a street, never onto the green.
            UndergroundComplex.ParkBlock made = UndergroundComplex.BlockOn(ground);
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                double want = room.Side switch
                {
                    UndergroundComplex.RingSide.Near => room.Y1,
                    UndergroundComplex.RingSide.Far => room.Y0,
                    UndergroundComplex.RingSide.West => room.X0,
                    _ => room.X1,
                };
                double street = room.Side switch
                {
                    UndergroundComplex.RingSide.Near => made.SpineFaceY,
                    UndergroundComplex.RingSide.Far => made.BackStreetY1,
                    UndergroundComplex.RingSide.West => made.WestInnerX,
                    _ => made.EastInnerX,
                };
                if (Math.Abs(want - street) > 0.001)
                {
                    wrong.Add($"  {name}: ring room {room.Number} does not open onto a street.");
                }
            }

            // LAW 4 · a way through every wall.
            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                if (!park.Ways.Any(g => GateSide(park, g) == side))
                {
                    wrong.Add($"  {name}: the {side.ToString().ToUpperInvariant()} wall has no gate.");
                }
            }

            // …and the green never runs off the end of the world it was carved in.
            if (park.X0 < ground.LeftX + SurfaceLayout.EdgeMargin
                || park.X1 > ground.RightX - SurfaceLayout.EdgeMargin
                || park.Y0 < ground.BottomY)
            {
                wrong.Add($"  {name}: the park ({park.X0:F1},{park.Y0:F1})-({park.X1:F1},{park.Y1:F1}) "
                    + "leaves the field.");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} problem(s) on ground the generator has never been handed:\n"
            + string.Join("\n", wrong));

        // ANTI-VACUITY, and it is the whole reason this guard exists: the five grounds really did produce
        // five different buildings. A suite that swept a hundred sites and met one shape would pass this
        // file without ever having asked the carve a second question.
        Assert.True(shapes.Count >= 4,
            "the mutated grounds all produced the same block, so this proved no more than the sweep did: "
            + string.Join(", ", shapes.Order()));
    }
}
