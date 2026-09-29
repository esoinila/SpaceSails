using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1202 slice 4 · <b>CHARGED TO PRESERVATION — what Core decides alone.</b> A spiked story is the watchers' own
/// grammar done by a hired hand, so the client was an office and the office pays like one: a line item in the
/// satchel (#1074 beat 3's idiom) and, the cycle after a SPIKED window, the port rag noticing the hole. The page's
/// half is driven on a live page in <c>TheOfficePaysWithPaperTests</c>. Every guard here was watched go red on the
/// sabotage its summary names.
/// </summary>
public sealed class ChargedToPreservationTests
{
    /// <summary>
    /// THE OFFICE PAYS WITH PAPER ONLY WHEN IT PAYS: SPIKED (the full purse) and ALTERED (half) — never LATE, which
    /// pays nothing, and never before the window has decided. <b>RED</b> by letting LATE land it
    /// (<c>ReceiptLands</c> answering true for every decided outcome).
    /// </summary>
    [Fact]
    public void TheReceiptLandsOnlyWithAPayout()
    {
        foreach (SpikeIt.Outcome outcome in Enum.GetValues<SpikeIt.Outcome>())
        {
            bool pays = SpikeIt.Pays(outcome, 1000) > 0;
            Assert.Equal(pays, SpikeIt.ReceiptLands(outcome));
        }

        Assert.True(SpikeIt.ReceiptLands(SpikeIt.Outcome.Spiked));
        Assert.True(SpikeIt.ReceiptLands(SpikeIt.Outcome.Altered));
        Assert.False(SpikeIt.ReceiptLands(SpikeIt.Outcome.Late));
        Assert.False(SpikeIt.ReceiptLands(SpikeIt.Outcome.None));
    }

    /// <summary>
    /// THE RAG NOTICES ONLY A SPIKED HOLE: an ALTERED story ran under her byline and a LATE one ran her own, so
    /// neither cycle is missing a byline. <b>RED</b> by answering true for ALTERED too.
    /// </summary>
    [Fact]
    public void TheRagNoticesOnlyASpikedHole()
    {
        Assert.True(SpikeIt.TheRagNoticesTheHole(SpikeIt.Outcome.Spiked));
        Assert.False(SpikeIt.TheRagNoticesTheHole(SpikeIt.Outcome.Altered));
        Assert.False(SpikeIt.TheRagNoticesTheHole(SpikeIt.Outcome.Late));
        Assert.False(SpikeIt.TheRagNoticesTheHole(SpikeIt.Outcome.None));
    }

    /// <summary>
    /// THE RECEIPT IS AN AUTHORED SHEET, READ THE SAME IN EVERY DOOR: <c>spike-receipt:{Body}</c>, a single sheet
    /// the tear verb never splits, titled and bodied with the canon wherever the book reads a paper away from its
    /// room (the sleeve's row, the 🔍 card, the dig). <b>RED</b> by dropping <c>IsTheReceipt</c> from
    /// <c>SpikeIt.IsAuthored</c> (the seeded generic title came back).
    /// </summary>
    [Fact]
    public void TheReceiptIsAnAuthoredSheetReadTheSameEverywhere()
    {
        Satchel.Item receipt = SpikeIt.TheReceipt("Luna");
        Assert.Equal(Satchel.Kind.Paper, receipt.Kind);
        Assert.Equal("spike-receipt:Luna", receipt.Id);
        Assert.True(SpikeIt.IsTheReceipt(receipt.Id));
        Assert.Equal("Luna", SpikeIt.ReceiptBody(receipt.Id));
        Assert.False(SpikeIt.IsTheReceipt(SpikeIt.TheSwap("Luna").Id));
        Assert.False(SpikeIt.IsTheReceipt(SpikeIt.ReceiptId));
        Assert.False(SpikeIt.IsTheSwap(receipt.Id));
        Assert.False(SpikeIt.IsThePages(receipt.Id));
        Assert.True(Satchel.Item.TryParse(receipt.Stored, out Satchel.Item back));
        Assert.Equal(receipt, back);

        Assert.True(FieldClue.IsAuthored(receipt.Id));
        Assert.Equal(1, PageGranularity.PagesIn(receipt.Id));
        Assert.Equal(SpikeIt.ReceiptTitle, FieldClue.Title(receipt.Id));
        Assert.Equal(SpikeIt.ReceiptDocument, FieldClue.Document(receipt.Id));
        CarriedObject.Reveal card = CarriedObject.PaperReveal(receipt.Id);
        Assert.Equal(("", SpikeIt.ReceiptTitle, SpikeIt.ReceiptDocument), (card.ArtUrl, card.Label, card.Story));

        // …and the two papers slice 2 wrote still read as they did.
        Assert.Equal("A page arrives with the terms", FieldClue.Title(SpikeIt.TheSwap("Luna").Id));
        Assert.Equal(SpikeIt.TakeThePagesLine, FieldClue.Document(SpikeIt.PagesId));
    }

    /// <summary>
    /// IT FILES UNDER THE AUTHORITY AND THE STORY'S BODY — beat 3's own door, so a receipt stacks in THREADS under
    /// the same office as the rail, the rota and the pour (<see cref="MoneyTrail.TheOffice"/>, which is
    /// <see cref="StopOrder.Stamp"/>), and under the ground the story was about. Two subjects and never a third.
    /// <b>RED</b> by filing it under the byline as well (<c>CaseSubjects.Person(CarryThePress.Byline)</c> added).
    /// </summary>
    [Fact]
    public void TheReceiptFilesUnderTheAuthorityAndTheBody()
    {
        string line = SpikeIt.ReceiptSubjects("Luna");
        Assert.Equal(MoneyTrail.SubjectsFor("Luna"), line);

        var subjects = CaseSubjects.On(new FieldNote(SpikeIt.ReceiptDocument, 0, "", SpikeIt.ReceiptGlyph, line));
        Assert.Equal(2, subjects.Count);
        Assert.Equal(MoneyTrail.TheOffice, subjects[0]);
        Assert.Equal(CaseSubjects.Office(StopOrder.Stamp), subjects[0]);
        Assert.Equal(CaseSubjects.Place("Luna"), subjects[1]);

        // …and a receipt beside a line item off a stopped ground stacks under the one office.
        var book = new List<FieldNote>
        {
            new(MoneyTrail.PourLineItem, 1, "", "🔦", MoneyTrail.SubjectsFor("Somewhere")),
            new(SpikeIt.ReceiptDocument, 2, "", SpikeIt.ReceiptGlyph, line),
        };
        CaseSubjects.SubjectThread office = Assert.Single(CaseSubjects.ThreadsOf(book));
        Assert.Equal(MoneyTrail.TheOffice, office.Subject);
        Assert.Equal(2, office.Entries.Count);
    }

    /// <summary>§8's list, the one <c>TheMoneyTrailTests</c> keeps: a line item never borrows the word.</summary>
    private static readonly string[] Forbidden =
    [
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
        "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
    ];

    /// <summary>
    /// SCULLY READS A TRUE INVOICING LINE: no amount (no digit at all), no signature, no department but the cost
    /// centre (every capitalised word not starting a sentence is <i>Preservation</i>), no name — not hers, not her
    /// byline's, not the captain's — and not a word §8 reserves. The rag's line names nobody either, and no body.
    /// <b>RED</b> by "Editorial services, one item, 900 cr. Charged to Preservation." (a figure) and by "Charged to
    /// Preservation, Plant." (a second office).
    /// </summary>
    [Fact]
    public void NothingOnTheReceiptIsFalseAndNothingOnItIsAName()
    {
        foreach (string text in new[] { SpikeIt.ReceiptTitle, SpikeIt.ReceiptDocument, SpikeIt.BylineMissingLine })
        {
            Assert.DoesNotMatch(@"\d", text);
            foreach (string word in Forbidden)
            {
                Assert.DoesNotContain(word, text, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain(CarryThePress.Plate, text, StringComparison.Ordinal);
            Assert.DoesNotContain(CarryThePress.Byline, text, StringComparison.Ordinal);
            Assert.DoesNotContain("Rauha", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("captain", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("{", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Authority", text, StringComparison.OrdinalIgnoreCase);
        }

        // Every capitalised word that does not open a sentence.
        var capitalised = new HashSet<string>(StringComparer.Ordinal);
        string[] sentences = SpikeIt.ReceiptDocument.Split(". ", StringSplitOptions.RemoveEmptyEntries);
        foreach (string sentence in sentences)
        {
            foreach (Match m in Regex.Matches(sentence, @"\b[A-Z][A-Za-z]*\b"))
            {
                if (m.Index > 0)
                {
                    capitalised.Add(m.Value);
                }
            }
        }

        Assert.Equal(["Preservation"], capitalised.ToArray());
        Assert.EndsWith("Charged to Preservation.", SpikeIt.ReceiptDocument, StringComparison.Ordinal);
    }

    // ── THE RAG AND THE WIRE LAW ────────────────────────────────────────────────────────────────────────

    /// <summary>The wire law's own two patterns (<c>TheWireNeverNamesTheCaptainTests</c>, untouched), read here
    /// against the one new line the wire can print.</summary>
    private static readonly Regex Personal =
        new(@"\b(you|your|yours|yourself|we|us|our|ours|my|mine|I)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex NamesTheCaptain =
        new(@"\bthe captain\b|\bcaptain\s*[,.!?]|\bcapt\.|\bskipper\b|\byour\s+(ship|hull|sail|boat|vessel|crew|tab|cargo|hold|berth)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// THE RAG'S LINE RIDES THE FLOOR REACTION'S OWN KIND: printed as written (a pass-through), on a port's rag and
    /// nowhere else, about nothing the book has a heading for (no body, no byline), and inside the wire law — no
    /// first or second person, nobody titled or addressed.
    /// </summary>
    [Fact]
    public void TheRagsLineIsAPortRagsOwnAndNamesNobody()
    {
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.PressFloorReaction, 10 * 86400.0, SpikeIt.BylineMissingLine, "");
        Assert.Equal(SpikeIt.BylineMissingLine, NewsWire.Headline(evt));
        Assert.Equal("", NewsWire.SubjectsFor(evt));
        Assert.True(NewsWire.PrintsIn(evt.Kind, NewsWire.NewsScope.PortRag));
        Assert.False(NewsWire.PrintsIn(evt.Kind, NewsWire.NewsScope.SystemWire));
        Assert.False(NewsWire.PrintsIn(evt.Kind, NewsWire.NewsScope.CompanyIntranet));

        Assert.DoesNotMatch(Personal, SpikeIt.BylineMissingLine);
        Assert.DoesNotMatch(NamesTheCaptain, SpikeIt.BylineMissingLine);
    }
}
