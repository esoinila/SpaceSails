using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #694 · THE BUILDING SAYS ITS NAME ON THE FLOORS YOU ENTER IT BY. Owner, on B11 of a thirteen-floor site:
///
/// <para><i>"every floor has the text 'The Clinic' on it. Some kind of artifact?"</i></para>
///
/// <para>It was neither an artifact nor a leak — <see cref="HiveInterior.FloorDeck"/> drew
/// <see cref="UndergroundComplex.TitleOf"/> beside the shaft unconditionally. His question is the finding:
/// a sign a player asks about because they suspect the renderer has gone wrong is a sign that has stopped
/// saying anything.</para>
///
/// <para><b>Why these run on the real deck and not on the source.</b> The law itself is Core's and is pinned
/// there (<c>TheFloorTellsYouWhereYouAreTests</c>). What is left to get wrong is the wiring — the renderer
/// asking the predicate and then drawing something else, or drawing the title from a second place. So these
/// build the deck a captain's boots actually collide with and count the plates on it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFacilityPlateIsALobbySignTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, MoonSurface.ExpeditionField(), 0, (_, _) => { }, []);

    /// <summary>Every facility title drawn on this floor's deck, whatever put it there.</summary>
    private static List<string> PlatesOn(DeckPlan deck)
    {
        var titles = Enum.GetValues<UndergroundComplex.Kind>()
            .Select(UndergroundComplex.TitleOf)
            .ToHashSet(StringComparer.Ordinal);
        return [.. deck.RoomLabels.Select(l => l.Text).Where(titles.Contains)];
    }

    [Fact]
    public void TheFacilityPlateDrawsOnTheLobbyFloorsAndNoOther()
    {
        var wrong = new List<string>();
        foreach (string body in Bodies)
        {
            // #592 · FloorsOf, not "−1 down to the depth" — a site with an unlisted band has a GAP in it.
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                bool want = UndergroundComplex.ShowsFacilityPlate(body, level);
                List<string> plates = PlatesOn(DeckFor(body, level));
                if (plates.Count != (want ? 1 : 0))
                {
                    wrong.Add($"  {body} B{-level}: {plates.Count} plate(s) [{string.Join(", ", plates)}], " +
                              $"wanted {(want ? 1 : 0)}");
                }
            }
        }

        if (wrong.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{wrong.Count} floor(s) draw the facility plate against the lobby law:");
            foreach (string line in wrong.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    [Fact]
    public void TheUnlistedLobbyPlateContradictsTheOneInTheEntranceLobby()
    {
        // The reason the second plate exists at all. On a site with a band nobody listed, the two signs the
        // captain sees say DIFFERENT things — and the second one is the whole of #592's storytelling.
        var hiding = Bodies.Where(UndergroundComplex.HasUnlistedBand).ToList();
        Assert.NotEmpty(hiding);

        foreach (string body in hiding)
        {
            string entrance = Assert.Single(PlatesOn(DeckFor(body, -1)));
            int lobby = UndergroundComplex.BandTop(UndergroundComplex.UnlistedBandOf(body));
            string below = Assert.Single(PlatesOn(DeckFor(body, lobby)));

            Assert.NotEqual(entrance, below);
        }
    }

    [Fact]
    public void TheFloorStillTellsYouWhichFloorItIs()
    {
        // What the plate's absence must NOT cost. The where-am-I answer was never this sign's job — the
        // plate over the car carries the depth, the department and the air (§13.13), and it draws on every
        // floor exactly as it always has. Taking the facility title off B11 removes a repetition, not an
        // answer.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                var big = DeckFor(body, level).BigLabels.Select(l => l.Text).ToList();
                Assert.Contains(UndergroundComplex.DepthPaint(level), big);
                Assert.Contains(UndergroundComplex.NameOf(body, level), big);
            }
        }
    }
}
