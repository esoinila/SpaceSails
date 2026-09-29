using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1202 slice 1 · <b>CARRY THE PRESS — the arithmetic and the words.</b> The page's half is driven on a
/// booted page in <c>TheStringerIsCarriedTests</c>; what is pinned here is what Core decides alone: the lines
/// verbatim, the clocks, the one story out of two, the ground, the tin, and the contract's line of state.
/// Every guard below was watched go red on the sabotage its summary names.
/// </summary>
public sealed class CarryThePressTests
{
    private const double Day = 86400.0;

    private static IReadOnlyList<string> TheShippedMoons =>
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    // ── THE WORDS ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THIRTEEN LINES, VERBATIM, AND NO FOURTEENTH. Every line of the brief, letter for letter; all of them in
    /// <see cref="CarryThePress.AllProse"/>; and the slice's own files read for any sentence typed into a
    /// method that is not one of them.
    ///
    /// <para><b>RED</b> by adding a full stop to the DEV line in <c>TakeThePressForCheat</c> (it became a
    /// sentence nobody authored) and by dropping <c>FloorLine</c> from <c>AllProse</c> (12, not 13).</para>
    /// </summary>
    [Fact]
    public void TheLinesAreVerbatimAndThereIsNoFourteenth()
    {
        Assert.Equal("Passage to {site}, one bunk, one recorder. I file on the cycler window whether I am back or not, so it is in your interest that I am back. Officially I am doing the tariff pool. Officially I am always doing the tariff pool.", CarryThePress.OfferCard);
        Assert.Equal("{cr} on the return, and the story is not for sale.", CarryThePress.TermsLine);
        Assert.Equal("She stows one bag and the recorder. 'Do not tell me anything you would mind reading.'", CarryThePress.TakenLine);
        Assert.Equal("Somebody left her a word here, she says, reading it off the recorder: where the ground is soft, {spot}. She does not say who.", CarryThePress.LandingLine);
        Assert.Equal("'Same tank as yours,' she says, 'and half your patience.'", CarryThePress.WalkLine);
        Assert.Equal("A tin, wax-sealed, in a hand that did not want to be recognised: 'Not the count. The difference.'", CarryThePress.TinText);
        Assert.Equal("She reads it twice and does not put it in the recorder.", CarryThePress.DigLine);
        Assert.Equal("She sleeps the whole burn back. The recorder does not.", CarryThePress.LiftoffLine);
        Assert.Equal("She pays for the bunk in credits and for the rest with a nod. 'Watch the wire. Three days. Do not write to me.'", CarryThePress.TurnInLine);
        Assert.Equal("{Body} is not losing people, says a hired boat's master who asked not to be named — it is losing the difference between the people it counts and the people it has. Officials call the figure a clerical matter. — R. Lind, for the wire", CarryThePress.StoryWithTheTin);
        Assert.Equal("A stringer's trip to {Body} finds nothing wrong, says the hired boat's master who took her, and nothing right either. Officials did not return calls. — R. Lind, for the wire", CarryThePress.StoryWithoutTheTin);
        Assert.Equal("The floor at the Ringside Exchange is 'unmoved' by the {Body} story. Nobody on the floor has read it. Everybody on the floor has an opinion.", CarryThePress.FloorLine);
        Assert.Equal("Her story ran. It is mostly true and it is not what happened.", CarryThePress.StoryRanLine);
        Assert.Equal("Lind", CarryThePress.Plate);
        Assert.Equal("CARRY THE PRESS", CarryThePress.CardTitle);
        Assert.Equal("press-note", CarryThePress.NoteId);

        var prose = CarryThePress.AllProse().ToList();
        Assert.Equal(13, prose.Count);
        Assert.Equal(prose.Count, prose.Distinct(StringComparer.Ordinal).Count());

        var authored = new HashSet<string>(prose, StringComparer.Ordinal);
        var sentences = new List<string>();
        foreach (string file in new[]
        {
            SourceOf("src", "SpaceSails.Core", "CarryThePress.cs"),
            SourceOf("src", "SpaceSails.Client", "Pages", "Map.CarryThePress.cs"),
            SourceOf("src", "SpaceSails.Client", "Pages", "Map.CarryThePress.Walk.cs"),  // #251 · her walk, split out
        })
        {
            foreach (Match m in Regex.Matches(WithoutComments(file), "\"(?:[^\"\\\\\\n]|\\\\.)*\""))
            {
                string text = m.Value.Trim('"');
                if (text.Contains(' ', StringComparison.Ordinal) && text.EndsWith('.') && !authored.Contains(text)
                    && !prose.Any(p => p.Contains(text, StringComparison.Ordinal)))
                {
                    sentences.Add(m.Value);
                }
            }
        }

        Assert.True(sentences.Count == 0,
            "this slice's files carry a sentence nobody authored: " + string.Join(", ", sentences));
    }

    /// <summary>The fills put the game's words where the canon's braces are, and nowhere else.</summary>
    [Fact]
    public void TheFillsReplaceOnlyTheirOwnBraces()
    {
        Assert.Equal("1,250 cr on the return, and the story is not for sale.", CarryThePress.Terms(1250));
        Assert.StartsWith("Passage to Luna · The Ridge Camp, one bunk", CarryThePress.Offer("Luna · The Ridge Camp"), StringComparison.Ordinal);
        Assert.Contains("where the ground is soft, 40 paces spinward of the landing beacon. She does not say who.",
            CarryThePress.Landing("40 paces spinward of the landing beacon"), StringComparison.Ordinal);
        foreach (string line in new[]
        {
            CarryThePress.Story("Luna", true), CarryThePress.Story("Luna", false), CarryThePress.Floor("Luna"),
        })
        {
            Assert.DoesNotContain("{", line, StringComparison.Ordinal);
            Assert.Contains("Luna", line, StringComparison.Ordinal);
        }
    }

    // ── THE STORY ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EXACTLY ONE OF THE TWO, CHOSEN BY THE TIN — the two stories are different sentences, the tin picks one,
    /// and neither ever contains the other.
    ///
    /// <para><b>RED</b> by returning <c>StoryWithTheTin</c> for both arms of <c>Story</c>.</para>
    /// </summary>
    [Fact]
    public void ExactlyOneOfTheTwoStoriesIsChosenByTheTin()
    {
        foreach (string body in new[] { "Luna", "Phobos", "Titan" })
        {
            string with = CarryThePress.Story(body, withTheTin: true);
            string without = CarryThePress.Story(body, withTheTin: false);
            Assert.NotEqual(with, without);
            Assert.Equal(CarryThePress.StoryWithTheTin.Replace("{Body}", body, StringComparison.Ordinal), with);
            Assert.Equal(CarryThePress.StoryWithoutTheTin.Replace("{Body}", body, StringComparison.Ordinal), without);
            Assert.DoesNotContain("difference", without, StringComparison.Ordinal);
            Assert.Contains("difference", with, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// THE STORY NEVER PRINTS BEFORE THREE SIM-DAYS AFTER THE TURN-IN, and the floor a day after that. A
    /// contract she has not paid for has no story at all.
    ///
    /// <para><b>RED</b> by making <c>StoryAfterSeconds</c> two days: the story is due a day early.</para>
    /// </summary>
    [Fact]
    public void TheStoryNeverPrintsBeforeThreeSimDaysAfterTheTurnIn()
    {
        const double paid = 40 * Day;
        var p = new CarryThePress.Passage(1, TurnedIn: paid);

        Assert.False(CarryThePress.StoryIsDue(new CarryThePress.Passage(1), 400 * Day), "an unpaid trip has a story");
        Assert.False(CarryThePress.StoryIsDue(p, paid));
        Assert.False(CarryThePress.StoryIsDue(p, paid + (3 * Day) - 1));
        Assert.True(CarryThePress.StoryIsDue(p, paid + (3 * Day)));
        Assert.Equal(paid + (3 * Day), CarryThePress.StoryAt(p));

        Assert.False(CarryThePress.FloorIsDue(p, paid + (4 * Day) - 1));
        Assert.True(CarryThePress.FloorIsDue(p, paid + (4 * Day)));
    }

    /// <summary>The wire prints her story as it was filed (a pass-through, the ArcBeatBreaks contract), files it
    /// under her byline and the body, and the floor's reaction prints on a port rag and nowhere else.
    ///
    /// <para><b>RED</b> by letting <c>PrintsIn</c> answer true for the floor on the system wire.</para></summary>
    [Fact]
    public void TheWirePrintsHerStoryAsFiledAndTheFloorOnlyOnAPortsRag()
    {
        string story = CarryThePress.Story("Luna", true);
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.PressStoryFiled, 9 * Day, story, "Luna");
        Assert.Equal(story, NewsWire.Headline(evt));
        Assert.Equal(CaseSubjects.Line(CaseSubjects.Person("R. Lind"), CaseSubjects.Place("Luna")), NewsWire.SubjectsFor(evt));

        Assert.True(NewsWire.PrintsIn(NewsWire.NewsEventKind.PressStoryFiled, NewsWire.NewsScope.SystemWire));
        Assert.True(NewsWire.PrintsIn(NewsWire.NewsEventKind.PressStoryFiled, NewsWire.NewsScope.PortRag));
        Assert.False(NewsWire.PrintsIn(NewsWire.NewsEventKind.PressStoryFiled, NewsWire.NewsScope.CompanyIntranet));
        Assert.True(NewsWire.PrintsIn(NewsWire.NewsEventKind.PressFloorReaction, NewsWire.NewsScope.PortRag));
        Assert.False(NewsWire.PrintsIn(NewsWire.NewsEventKind.PressFloorReaction, NewsWire.NewsScope.SystemWire));
        Assert.False(NewsWire.PrintsIn(NewsWire.NewsEventKind.PressFloorReaction, NewsWire.NewsScope.CompanyIntranet));

        // …and every kind that existed before prints exactly where it always did.
        foreach (NewsWire.NewsEventKind kind in Enum.GetValues<NewsWire.NewsEventKind>())
        {
            if (kind == NewsWire.NewsEventKind.PressFloorReaction)
            {
                continue;
            }

            Assert.True(NewsWire.PrintsIn(kind, NewsWire.NewsScope.SystemWire));
            Assert.True(NewsWire.PrintsIn(kind, NewsWire.NewsScope.PortRag));
            Assert.False(NewsWire.PrintsIn(kind, NewsWire.NewsScope.CompanyIntranet));
        }
    }

    // ── THE GROUND AND THE TIN ──────────────────────────────────────────────────────────────────────────

    /// <summary>Her ground is one of the pool's, a real site of that body, the same answer twice, and it does
    /// not depend on the order the pool was handed in.</summary>
    [Fact]
    public void HerGroundIsSeededOffTheContractAndIsARealSite()
    {
        var reversed = TheShippedMoons.Reverse().ToList();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < 200; i++)
        {
            string contract = $"selene-gate#{i}";
            ParcelDrop.Destination? a = CarryThePress.For(contract, TheShippedMoons);
            ParcelDrop.Destination? b = CarryThePress.For(contract, reversed);
            Assert.NotNull(a);
            Assert.Equal(a, b);
            Assert.Contains(a!.Value.BodyId, TheShippedMoons);
            Assert.InRange(a.Value.SiteIndex, 0, LandingSites.Count(a.Value.BodyId) - 1);
            Assert.Equal(LandingSites.At(a.Value.BodyId, a.Value.SiteIndex).Name, a.Value.SiteName);
            seen.Add(a.Value.BodyId);
        }

        Assert.True(seen.Count > 5, $"200 contracts reached only {seen.Count} moon(s) — the roll is not a roll.");
        Assert.Null(CarryThePress.For("x", []));
        Assert.Null(CarryThePress.For(null, TheShippedMoons));
    }

    /// <summary>
    /// THE TIN IS WHERE THE GROUND IS SOFT: every square it may lie under is not bedrock, is inside the diggable
    /// field below the landing band, and is near the pad; its words are the game's own for a buried thing; and it
    /// is not the captain's (it never becomes a ✗).
    ///
    /// <para><b>RED</b> by deleting the bedrock clause from <c>TinSquares</c>: a square rings off bedrock.</para>
    /// </summary>
    [Fact]
    public void TheTinLiesWhereTheGroundIsSoftNearThePad()
    {
        SurfaceLayout.Field field = SurfaceLayout.DefaultField;
        int checkedSquares = 0;
        foreach (string body in TheShippedMoons)
        {
            for (int i = 0; i < 20; i++)
            {
                string quest = $"press-{i}";
                var squares = CarryThePress.TinSquares(quest, body, field).Take(12).ToList();
                Assert.NotEmpty(squares);
                Assert.Equal(squares, CarryThePress.TinSquares(quest, body, field).Take(12).ToList());
                foreach ((int x, int y) in squares)
                {
                    checkedSquares++;
                    Assert.False(BeachComber.Roll(body, x, y).IsTooHard, $"{body} {quest}: the tin is under bedrock");
                    (double cx, double cy) = BeachComber.SquareCenter(x, y);
                    Assert.True(cy < field.LandingBandY && cy > field.BottomY, $"{body}: the tin is off the field");
                    Assert.True(Math.Abs(cx - field.HomeX) < CarryThePress.TinSpreadDu + 40, $"{body}: the tin is nowhere near the pad");
                }

                TreasureCache tin = CarryThePress.TheTin(quest, body, 1);
                Assert.False(tin.PlayerOwned);
                Assert.Equal(CarryThePress.NoteId, Assert.Single(tin.Deposit!).Id);
                Assert.Matches(@"^\d+ paces .+ of .+$", tin.BearingLine);
            }
        }

        Assert.True(checkedSquares > 1000, "the sweep checked too few squares to mean anything");
        Assert.True(CarryThePress.FindsTheTin((10, -20), 12, -18));
        Assert.False(CarryThePress.FindsTheTin((10, -20), 13, -20));
    }

    /// <summary>The tin reads as the one sheet it is, through the same three readers every paper does.</summary>
    [Fact]
    public void TheTinReadsAsItsOwnText()
    {
        Assert.True(FieldClue.IsAuthored(CarryThePress.NoteId));
        Assert.Equal(CarryThePress.TinText, FieldClue.Document(CarryThePress.NoteId));
        Assert.Equal("A tin, wax-sealed", FieldClue.Title(CarryThePress.NoteId));
        CarriedObject.Reveal card = CarriedObject.PaperReveal(CarryThePress.NoteId);
        Assert.Equal(CarryThePress.TinText, card.Story);
        Assert.Equal(Satchel.Kind.Paper, CarryThePress.TheNote().Kind);
        Assert.Equal(Satchel.Compartment.Sleeve, Satchel.CompartmentOf(CarryThePress.TheNote().Kind));
    }

    // ── THE CONTRACT'S STATE ────────────────────────────────────────────────────────────────────────────

    /// <summary>The line of state survives being written and read back, every flag and the clock to the bit;
    /// an empty or foreign line reads as not-yet.</summary>
    [Fact]
    public void ThePassageRoundTrips()
    {
        var p = new CarryThePress.Passage(2, Landed: true, Walked: false, Tin: true, TurnedIn: 123456.789012345,
            Printed: true, Floored: false);
        Assert.Equal(p, CarryThePress.Passage.Read(p.Write()));
        Assert.Equal(new CarryThePress.Passage(0), CarryThePress.Passage.Read(null));
        Assert.Equal(new CarryThePress.Passage(0), CarryThePress.Passage.Read("V-06"));
        Assert.Null(CarryThePress.Passage.Read(new CarryThePress.Passage(3).Write()).TurnedIn);
    }

    /// <summary>The dev latch reads only its own key.</summary>
    [Fact]
    public void TheDevLatchReadsOnlyItsOwnKey()
    {
        Assert.Equal(CarryThePress.Cheat.Aboard, CarryThePress.CheatIn("http://x/map?dock=selene-gate&press=1"));
        Assert.Equal(CarryThePress.Cheat.Filed, CarryThePress.CheatIn("http://x/map?dock=selene-gate&press=filed"));
        Assert.Equal(CarryThePress.Cheat.Pending, CarryThePress.CheatIn("http://x/map?dock=selene-gate&press=pending"));
        Assert.Equal(CarryThePress.Cheat.None, CarryThePress.CheatIn("http://x/map?dock=selene-gate"));
        Assert.Equal(CarryThePress.Cheat.None, CarryThePress.CheatIn("http://x/map?pressure=1"));
        Assert.Equal(CarryThePress.Cheat.None, CarryThePress.CheatIn(null));
    }

    /// <summary>She is at a berth's table one watch in about three, and never at no berth.</summary>
    [Fact]
    public void SheIsAtTheTableOneWatchInAboutThree()
    {
        int at = 0;
        for (long w = 0; w < 3000; w++)
        {
            if (CarryThePress.AtTheTable("selene-gate", w))
            {
                at++;
            }
        }

        Assert.InRange(at, 800, 1200);
        Assert.False(CarryThePress.AtTheTable(null, 7));
        Assert.False(CarryThePress.AtTheTable("", 7));
    }

    // ── helpers ─────────────────────────────────────────────────────────────────────────────────────────

    private static string WithoutComments(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, @"//[^\n]*", " ");
    }

    private static string SourceOf(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), .. parts]));
}
