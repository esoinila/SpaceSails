using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #709 · ARE THEY ACTUALLY THERE? Owner: <i>"we should have people in the bar... we have cover story"</i> ·
/// <i>"for now let's keep the people in B1."</i>
///
/// <para>Core decides who is sitting down (<c>TheCanteenRegularsTests</c> pins that law, both halves proven
/// red). What is left to get wrong is the <b>wiring</b> — a renderer that asks the predicate and then draws
/// something else, or draws nobody, or draws them on every floor. So these build the deck a captain's boots
/// actually collide with and count the people on it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ThereArePeopleInTheBarTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, MoonSurface.ExpeditionField(), 0, (_, _) => { }, []);

    private static List<DeckPlan.ConsoleSpot> RegularsOn(DeckPlan deck) =>
        [.. deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveRegular)];

    [Fact]
    public void ThePeopleAreOnB1AndOnNoOtherFloorOfAnySite()
    {
        // The owner's ruling, on the real deck. A regular leaking one floor down would spend the descent
        // gradient the whole feature is built to buy.
        var wrong = new List<string>();
        int total = 0;

        foreach (string body in Bodies)
        {
            int? top = UndergroundComplex.TopPressurisedFloor(body);
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                int drawn = RegularsOn(DeckFor(body, level)).Count;
                total += drawn;

                if (level != top && drawn > 0)
                {
                    wrong.Add($"  {body} B{-level}: {drawn} person(s) below the top pressurised floor");
                }
                if (level == top && drawn == 0)
                {
                    wrong.Add($"  {body} B{-level}: the canteen floor is deserted");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} floor(s) disagree with the B1 ruling:\n{string.Join("\n", wrong.Take(20))}");
        Assert.True(total >= Bodies.Length, $"only {total} people across {Bodies.Length} sites.");
    }

    [Fact]
    public void EverySeatedPersonIsExactlyWhereCoreSAIDTheyWere()
    {
        // The wiring test proper. The renderer may not round, nudge or re-place anybody: Core put them on
        // #707's own table tops, and a console offset by a hand-typed du would be one more caller doing
        // geometry about furniture it does not own (§13.15).
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor =
                UndergroundComplex.Build(body, level, MoonSurface.ExpeditionField());

            var expected = floor.Amenities
                .SelectMany(a => CanteenRegulars.Sitting(body, level, a))
                .Select(s => ((float)s.X, (float)s.Y, s.Plate))
                .ToList();

            var drawn = RegularsOn(DeckFor(body, level))
                .Select(c => (c.X, c.Y, c.Label))
                .ToList();

            Assert.Equal(expected.Count, drawn.Count);
            foreach ((float x, float y, string plate) in expected)
            {
                Assert.Contains((x, y, plate), drawn);
            }
        }
    }

    [Fact]
    public void TheBoardHangsOnTheSameOneFloorAndCoreChoseTheSpot()
    {
        // #709 · The cork board's wiring. Same B1 law as the people, and the spot must be CORE's arithmetic
        // rather than a renderer's nudge — the offsets were picked against the counter's line and the tables',
        // and a client that adjusted them would hang the board somewhere nothing has ever checked.
        foreach (string body in Bodies)
        {
            int? top = UndergroundComplex.TopPressurisedFloor(body);
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                var boards = DeckFor(body, level).Consoles
                    .Where(c => c.Kind == DeckPlan.ConsoleKind.HiveBoard)
                    .ToList();

                if (level != top)
                {
                    Assert.Empty(boards);
                    continue;
                }

                DeckPlan.ConsoleSpot board = Assert.Single(boards);
                Assert.Equal(CanteenBoard.Plate, board.Label);

                UndergroundComplex.Amenity canteen = UndergroundComplex
                    .Build(body, level, MoonSurface.ExpeditionField()).Amenities
                    .Single(a => a.Use == UndergroundComplex.Comfort.UpperCanteen);
                (double wantX, double wantY) = CanteenBoard.At(body, level, canteen)!.Value;

                Assert.Equal((float)wantX, board.X);
                Assert.Equal((float)wantY, board.Y);
            }
        }
    }

    [Fact]
    public void NobodyIsStandingInsideAWallOrOnTopOfTheLift()
    {
        // #681's standing lesson, one object along: a figure the sim draws must be somewhere a person could
        // be. They sit on Core's table tops, which are inside an audited-walkable amenity room — but the
        // cheap check is worth having, because the day somebody moves the tables this is what notices.
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            DeckPlan deck = DeckFor(body, level);
            (double shaftX, double shaftY) =
                UndergroundComplex.ShaftAt(MoonSurface.ExpeditionField());

            foreach (DeckPlan.ConsoleSpot who in RegularsOn(deck))
            {
                Assert.False(
                    System.Math.Abs(who.X - shaftX) < UndergroundComplex.CorridorHalf &&
                    System.Math.Abs(who.Y - shaftY) < UndergroundComplex.CorridorHalf,
                    $"{body} B{-level}: somebody is sitting in the lift car.");
            }
        }
    }
}
