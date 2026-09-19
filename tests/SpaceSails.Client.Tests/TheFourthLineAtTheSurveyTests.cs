using System;
using System.IO;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #533 · <b>THE FOURTH LINE</b> — where a derived anomaly is allowed to be said, and where it is not.
///
/// <para>The rule the issue sets is about PLACE as much as about words: the anomaly reads at the survey the
/// captain already walks to (the cause's own station — the one station on the ship they came here to read),
/// as two numbers side by side under the evidence, and it goes in the field book. There is no fifth
/// surface, no banner, no marker on the deck and no second card, because a hull that carries one must be
/// indistinguishable from a hull that does not until she is read.</para>
///
/// <para>So these are placement guards and they are read off the COMPOSED page, which is what the player
/// actually gets — plus the one behavioural question a doc row can get wrong: does the link in the testing
/// file really boot a hull that carries one.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFourthLineAtTheSurveyTests
{
    private static string TheSurveyCard() =>
        MapMarkup.Read(Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map",
            "WreckLookCard.razor"));

    private static string TheWreckFile() =>
        File.ReadAllText(Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages",
            "Map.Wreck.cs"));

    // ── WHERE IT IS SAID ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT IS THE FOURTH THING ON THE CARD, INSIDE THE CARD. Under the title, the painting and the evidence
    /// caption, and inside the modal's own subtree rather than under the backdrop's blur (#736/#680 — a line
    /// beneath the blur is in the DOM and not on the screen).
    /// </summary>
    [Fact]
    public void TheAnomalyIsTheFourthThingOnTheSurveyCardAndItIsInsideTheCard()
    {
        string card = TheSurveyCard();

        int modal = card.IndexOf("class=\"view-object\"", StringComparison.Ordinal);
        int title = card.IndexOf("view-object-title", StringComparison.Ordinal);
        int art = card.IndexOf("view-object-img", StringComparison.Ordinal);
        int caption = card.IndexOf("view-object-caption", StringComparison.Ordinal);
        int anomaly = card.IndexOf("view-object-anomaly", StringComparison.Ordinal);
        int closes = card.IndexOf("</OverlayShell>", StringComparison.Ordinal);

        Assert.True(modal > 0 && title > modal && art > title && caption > art,
            "the survey card is no longer title → painting → caption; this guard needs re-reading");
        Assert.True(anomaly > caption,
            "the anomaly is not the FOURTH line — it has moved above the evidence it sits under");
        Assert.True(anomaly < closes,
            "the anomaly line is rendered outside the card's own subtree (#736/#680)");
    }

    /// <summary>
    /// AND AN EMPTY ONE DRAWS NOTHING. Nearly every hull has no anomaly, and on those hulls the card must be
    /// the card it always was — not a slot with nothing in it, which is a tell that there is something to
    /// find on the hulls where the slot is full.
    /// </summary>
    [Fact]
    public void AHullWithNoAnomalyDrawsNoFourthLineAtAll()
    {
        string card = TheSurveyCard();

        Assert.Contains("@if (look.Anomaly.Length > 0)", card, StringComparison.Ordinal);
        Assert.Contains("@look.Anomaly", card, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>THE ONE HONEST PLACE, AND THE BOOK.</b> The line is composed into the survey card's own record and
    /// filed in the field book with the author's declared subjects (#741). It is never pulsed — a pulse is a
    /// banner that fades in eight seconds, and the whole reason the book exists (#587) is that a sentence
    /// the captain paid a boarding for may not be unreadable afterwards.
    /// </summary>
    [Fact]
    public void TheAnomalyIsSaidOnTheSurveyCardAndWrittenInTheBookAndNowhereElse()
    {
        string wreck = TheWreckFile();

        Assert.Contains("_wreckLook = new WreckLook(spot.Label.Replace(\"✔ \", \"\"), art, caption, fourth);",
            wreck, StringComparison.Ordinal);
        Assert.Contains("FileNoteAbout(reading.Gist, WreckAnomaly.Glyph, reading.Subjects);",
            wreck, StringComparison.Ordinal);

        // Nothing hands it to a banner. The pulse lines on this ship are the three station findings and the
        // hull's own noises; an anomaly among them would be gone in eight seconds and gone forever.
        foreach (string line in wreck.Split('\n'))
        {
            if (line.Contains("ShowPulseMessage", StringComparison.Ordinal))
            {
                Assert.DoesNotContain("Anomaly", line, StringComparison.Ordinal);
                Assert.DoesNotContain("FourthLine", line, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// A ROAD NOBODY ASKED ABOUT IS NOT A ROAD WITH NO TRAFFIC. Without a sky, or on a body whose parent
    /// cannot be named, the client answers "no anomaly" rather than handing Core a zero tonnage and inviting
    /// it to deal one: every anomaly is a thing the captain can go and check, and a fact nobody asked for is
    /// the one thing one may never be built out of.
    /// </summary>
    [Fact]
    public void AHullWeCannotAskAboutCarriesNothing()
    {
        string wreck = TheWreckFile();
        int asks = wreck.IndexOf("private WreckAnomaly.Reading? TheAnomalyOnThisHull()", StringComparison.Ordinal);

        Assert.True(asks > 0, "the client no longer asks the hull for an anomaly");
        string body = wreck[asks..(asks + 1400)];
        Assert.Contains("_ephemeris is not { } sky", body, StringComparison.Ordinal);
        Assert.Contains("_surface?.Stop.Body.ParentId is not { } berth", body, StringComparison.Ordinal);
        Assert.Contains("return null;", body, StringComparison.Ordinal);
    }

    // ── THE ROW IN THE TESTING FILE ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE TESTING LINK REALLY BOOTS A HULL THAT CARRIES ONE.</b> A documented URL that quietly stopped
    /// reaching the thing it is documented for is this repo's own recurring expense (#1216, and the wreck
    /// lane's own lift that only went down). The row names <c>?wreck=mutiny</c>, whose hull the cheat seeds
    /// as <c>lost-1</c> and parks off THE TILT — a berth #541's rule already says nothing is listed at — so
    /// the hull, the berth and the sentence are all re-derived here from the shipped facts, and the file is
    /// read to check it quotes the sentence the station will actually print.
    /// </summary>
    [Fact]
    public void TheTestingLinkBootsAHullThatReallyCarriesOne()
    {
        var sol = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        var road = new WreckAnomaly.Facts(ArrivalTube.ScheduledTonnage(sol, "the-tilt"));

        Assert.Equal(0, road.ListedTonnageOnHerRoad);

        Derelict.Wreck hull = Derelict.SeededWithCause(Derelict.WreckCause.Mutiny)!.Value;
        WreckAnomaly.Reading? reading = WreckAnomaly.For(hull, road);

        Assert.NotNull(reading);
        Assert.Equal("lost-1", hull.Id);

        string links = File.ReadAllText(Path.Combine(
            TestTree.RepoRoot(), "docs", "testing-links-2026-09-17.md"));

        Assert.Contains("wreck=mutiny", links, StringComparison.Ordinal);
        Assert.Contains(reading!.Value.Line, links, StringComparison.Ordinal);
        Assert.Contains(reading.Value.Gist, links, StringComparison.Ordinal);
    }

    /// <summary>
    /// …AND THE HULLS BESIDE IT DO NOT, which is what makes the row worth walking. Eight of the ten cause
    /// hulls carry nothing at all on the same berth — including one that is rich enough and simply was not
    /// dealt one — so a captain who reads two wrecks sees the ordinary ship first.
    /// </summary>
    [Fact]
    public void MostOfTheTenCauseHullsCarryNothing()
    {
        var sol = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        var road = new WreckAnomaly.Facts(ArrivalTube.ScheduledTonnage(sol, "the-tilt"));

        int carrying = Enum.GetValues<Derelict.WreckCause>()
            .Select(c => Derelict.SeededWithCause(c))
            .Where(w => w is not null)
            .Count(w => WreckAnomaly.For(w!.Value, road) is not null);

        Assert.InRange(carrying, 1, 3);
    }
}
