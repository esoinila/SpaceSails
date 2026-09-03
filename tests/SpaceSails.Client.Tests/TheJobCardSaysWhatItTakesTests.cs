using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #959 · THE PLAIN BLOCK IS ON EVERY SURFACE A JOB APPEARS ON, AND IT IS THE SAME BLOCK.
///
/// <para>Owner, 2026-08-18, over a Captain's-ledger row: <i>"What is this job... should I destroy a ship
/// or rob a place? We should make sure these missions all tell clearly what it takes to complete them.
/// We need to know before we decide if we accept or not."</i></para>
///
/// <para><b>"Before we decide" is half the ask, and it is the half a Core test cannot reach.</b>
/// <see cref="JobTerms"/> is unit-tested to death in <c>EveryJobSaysWhatItTakesTests</c> — but text that
/// is correct and rendered on only ONE of the two surfaces is exactly the bug the owner reported: a card
/// that said one thing at the table and something else in the ledger. So this sweep asks the shipped
/// razor whether both surfaces render the block, in the same order, from the SAME call.</para>
///
/// <para>The other half is the failure mode this repo has named: a class that is written into the markup
/// and has no rule anywhere is invisible text. Both scoped stylesheets are checked for the three
/// classes, because Blazor's scoped CSS is per-component and the two copies can drift apart in one
/// careless edit.</para>
/// </summary>
public sealed class TheJobCardSaysWhatItTakesTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Source(params string[] parts) =>
        MapMarkup.Read(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    private static string Flat(string s) => Regex.Replace(s, @"\s+", " ");

    /// <summary>Source with its razor and C# comments taken out — a guard about what a card DOES must
    /// never be satisfied by prose SAYING it does.</summary>
    private static string CodeOnly(string source)
    {
        source = Regex.Replace(source, @"@\*.*?\*@", " ", RegexOptions.Singleline);
        source = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(source, @"^\s*//.*$", " ", RegexOptions.Multiline);
    }

    /// <summary>The three classes the block is built from, in the order it stacks them.</summary>
    private static readonly string[] BlockClasses = ["job-plain-verb", "job-plain-takes", "job-plain-effort"];

    // ── LAW 1 · BOTH SURFACES CARRY THE BLOCK ──────────────────────────────────────────────────────

    /// <summary>THE OFFER CARD — the "before we decide" half. RED PROOF: delete the block from the bar's
    /// contract card and this goes red on the first class.</summary>
    [Fact]
    public void TheStrangersOfferCardCarriesThePlainBlock()
    {
        string card = OfferCardMarkup();
        foreach (string cls in BlockClasses)
        {
            Assert.Contains(cls, card, StringComparison.Ordinal);
        }
        Assert.Contains("JobPlainBlock(offer)", card, StringComparison.Ordinal);
    }

    /// <summary>THE LEDGER ROW — the "and after" half, and the row the owner photographed.</summary>
    [Fact]
    public void TheCaptainsLedgerRowCarriesThePlainBlock()
    {
        string row = LedgerRowMarkup();
        foreach (string cls in BlockClasses)
        {
            Assert.Contains(cls, row, StringComparison.Ordinal);
        }
        Assert.Contains("q.Plain", row, StringComparison.Ordinal);
    }

    /// <summary>ONE MODEL, TWO PROJECTIONS. Both surfaces reach the same <c>JobPlainBlock</c>, so a
    /// captain who took a job on the strength of "≈ 3.60 M km · ~6 d by the lanes" reads that same
    /// sentence afterwards. RED PROOF: give either surface its own text and the call count drops.</summary>
    [Fact]
    public void BothSurfacesReadTheSameCall()
    {
        Assert.Contains("JobPlainBlock(offer)", CodeOnly(Source("Pages", "Map.razor")), StringComparison.Ordinal);
        Assert.Contains("JobPlainBlock(q)", CodeOnly(Source("Pages", "Map.Quests.Ledger.cs")), StringComparison.Ordinal);
        // …and there is exactly ONE place that builds it, so there is nowhere for a second wording to live.
        string terms = CodeOnly(Source("Pages", "Map.Quests.Terms.cs"));
        Assert.Single(Regex.Matches(terms, @"JobTerms\.PlainBlock\("));
    }

    // ── LAW 2 · THE PLAIN LINES COME FIRST, THE VOICE SECOND ───────────────────────────────────────

    /// <summary>The owner's ask was not "add a line" — it was that the card SAY WHAT THE JOB IS before it
    /// starts talking. On both surfaces the block sits above the flavour prose. RED PROOF: move either
    /// block below its blurb and the index comparison flips.</summary>
    [Fact]
    public void ThePlainBlockStandsAboveTheFlavour()
    {
        // Sliced, not searched: .deck-offer-blurb is a FAMILY class — the arrival-brake card, the table
        // card and the oracle card all wear it — so a whole-file IndexOf would answer about whichever
        // card happens to be written first, which is not a fact about this one.
        AssertRunsBefore(OfferCardMarkup(), "job-plain-verb", "deck-offer-blurb",
            "the offer card's pitch now runs before the plain block");
        AssertRunsBefore(LedgerRowMarkup(), "job-plain-verb", "@q.Detail",
            "the ledger row's flavour now runs before the plain block");
    }

    /// <summary>…and the flavour SURVIVES. The pitch and the ledger's kind-aware detail keep every word
    /// they had; the plain block was added, never substituted. RED PROOF: delete either and this fires.
    /// </summary>
    [Fact]
    public void TheVoiceIsKept()
    {
        Assert.Contains("@offer.Blurb", CodeOnly(Source("Pages", "Map.razor")), StringComparison.Ordinal);
        Assert.Contains("@q.Detail", CodeOnly(Source("Pages", "Stations", "Captain.razor")), StringComparison.Ordinal);
        Assert.Contains("hole her sail or board her", Source("Pages", "Map.Quests.Ledger.cs"), StringComparison.Ordinal);
    }

    /// <summary>The three lines are rendered in the order Core stacks them — verb, then what it takes,
    /// then the effort. A block whose lines are shuffled is a block that reads as noise.</summary>
    [Theory]
    [InlineData("offer")]
    [InlineData("ledger")]
    public void TheBlocksLinesStandInCoresOwnOrder(string surface)
    {
        string markup = surface == "offer" ? OfferCardMarkup() : LedgerRowMarkup();
        int[] at = BlockClasses.Select(c => markup.IndexOf(c, StringComparison.Ordinal)).ToArray();
        Assert.All(at, i => Assert.True(i >= 0));
        Assert.True(at[0] < at[1] && at[1] < at[2], $"the {surface} card's block is out of order");

        // …and each line is drawn from its own index of the block, never one index twice.
        var indices = Regex.Matches(markup, @"(?:_offerPlain|plain)\[(\d)\]")
            .Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(indices.Length, indices.Distinct().Count());
    }

    // ── LAW 3 · THE PAY CARRIES ITS SIZE WORD ──────────────────────────────────────────────────────

    /// <summary>The ledger's purse line is Core's pay line for a coin job — "764 cr · small" — rather
    /// than the bare number that told the owner nothing. Intel and worked-off favors keep their own
    /// faces, because neither pays in loose coin. RED PROOF: put the raw interpolation back and the
    /// <c>plain[3]</c> assert goes red.</summary>
    [Fact]
    public void TheLedgersPurseLineIsCoresPayLine()
    {
        string ledger = CodeOnly(Source("Pages", "Map.Quests.Ledger.cs"));
        Assert.Contains("? plain[3]", ledger, StringComparison.Ordinal);
        Assert.Contains("route tip", ledger, StringComparison.Ordinal);
        Assert.Contains("cr favor", ledger, StringComparison.Ordinal);
    }

    /// <summary>The offer card's reward slot likewise, and it keeps the house's voice when there is no
    /// coin in the job.</summary>
    [Fact]
    public void TheOfferCardsRewardSlotIsCoresPayLine()
    {
        string card = Flat(OfferCardMarkup());
        Assert.Contains("_offerPlain[3]", card, StringComparison.Ordinal);
        Assert.Contains("On the house", card, StringComparison.Ordinal);
    }

    /// <summary>The completion fanfare — the owner's OTHER #959 screenshot, the pop-up he called puny —
    /// sizes the payment too, and against the purse as it stood BEFORE the coin landed. RED PROOF: size
    /// it against <c>_credits</c> unchanged and this names the missing subtraction.</summary>
    [Fact]
    public void TheRewardPopUpSizesThePaymentAgainstThePurseBeforeIt()
    {
        string flat = Flat(CodeOnly(Source("Pages", "Map.razor")));
        Assert.Contains("JobTerms.SizeWord(party.PaidCredits, _credits - party.PaidCredits)", flat, StringComparison.Ordinal);
    }

    // ── LAW 3b · AN OFFER THAT IS NOT A JOB SAYS NOTHING, RATHER THAN SOMETHING FALSE ──────────────

    /// <summary>
    /// FOUND BY PLAYING IT, at the very bar the owner filed #959 from. The KAAMOS returned-filing docket
    /// is minted as a <c>CargoRun</c> — the shape the table card knows how to slide across — but it has
    /// no destination body and adds no quest: its own comment reads <i>"No quest is added — there is
    /// nothing to go and do, which is the point."</i> Fed to the block it produced a sentence false in
    /// three ways at once: <c>DELIVER — Ringside Exchange</c>, <c>Takes: berth … with the parcel
    /// aboard</c>, and <c>Distance: not measured from here</c>.
    /// <para>So the block has an off switch, and both surfaces render NOTHING rather than a promise. RED
    /// PROOF: drop the <c>IsLedgerlessOffer</c> arm and the docket claims a delivery again; drop either
    /// surface's count guard and the card throws on an empty block.</para>
    /// </summary>
    [Fact]
    public void AnOfferThatIsADoorRatherThanAJobWearsNoBlockAtAll()
    {
        string terms = CodeOnly(Source("Pages", "Map.Quests.Terms.cs"));
        Assert.Contains("IsLedgerlessOffer(q) ? [] : JobTerms.PlainBlock(", terms, StringComparison.Ordinal);
        Assert.Contains("KaamosBounceOfferId", terms, StringComparison.Ordinal);

        // Both surfaces must actually HONOUR the empty block rather than indexing into it.
        Assert.Contains("_offerPlain.Count > 0", CodeOnly(Source("Pages", "Map.razor")), StringComparison.Ordinal);
        Assert.Contains("q.Plain is { Count: > 0 }",
            CodeOnly(Source("Pages", "Stations", "Captain.razor")), StringComparison.Ordinal);
        Assert.Contains("plain.Count > 0", CodeOnly(Source("Pages", "Map.Quests.Ledger.cs")), StringComparison.Ordinal);
    }

    // ── LAW 4 · A CLASS WITH NO RULE IS INVISIBLE TEXT ─────────────────────────────────────────────

    /// <summary>Both scoped stylesheets carry all three rules. Blazor's scoped CSS is per-component so
    /// the pair cannot be shared; this is the guard that keeps them from drifting. RED PROOF: delete
    /// either copy and the sweep names the file.</summary>
    [Theory]
    [InlineData("Map.razor.css")]
    [InlineData("Captain.razor.css")]
    public void BothScopedStylesheetsDressTheBlock(string sheet)
    {
        string css = sheet == "Map.razor.css"
            ? Source("Pages", "Map.razor.css")
            : Source("Pages", "Stations", "Captain.razor.css");

        Assert.Contains(".job-plain {", css, StringComparison.Ordinal);
        foreach (string cls in BlockClasses)
        {
            Assert.Contains("." + cls + " {", css, StringComparison.Ordinal);
        }
    }

    /// <summary>The block reads left-aligned even inside the centre-aligned offer card — a spec sheet is
    /// a list and the eye needs one left edge to run down it.</summary>
    [Fact]
    public void TheBlockIsLeftAlignedInsideTheCentredOfferCard()
    {
        string rule = Regex.Match(Source("Pages", "Map.razor.css"), @"\.job-plain \{[^}]*\}").Value;
        Assert.Contains("text-align: left", rule, StringComparison.Ordinal);
    }

    // ── LAW 5 · THE MEASUREMENTS ARE ASKED OF THE SIM ──────────────────────────────────────────────

    /// <summary>
    /// The bridge measures; it never invents. No literal metres, seconds or credits may appear in the
    /// numbers it hands Core — the distance is a live position difference, the lane time comes from
    /// <see cref="JobEffort"/>, and the purse is the real <c>_credits</c> field.
    /// <para>RED PROOF: hard-code any fallback distance or a cruise speed in Map.Quests.Terms.cs and the
    /// bare-number sweep names the line.</para>
    /// </summary>
    [Fact]
    public void TheBridgeMeasuresAndNeverInvents()
    {
        string terms = CodeOnly(Source("Pages", "Map.Quests.Terms.cs"));

        Assert.Contains("(tp - _ship.Position).Length", terms, StringComparison.Ordinal);
        Assert.Contains("JobEffort.LaneSeconds(primary.Mu", terms, StringComparison.Ordinal);
        Assert.Contains("JobEffort.SharedPrimary(", terms, StringComparison.Ordinal);
        Assert.Contains("PurseCredits: _credits", terms, StringComparison.Ordinal);

        // The only numeric literals allowed in this file are the zeroes that mean "nothing to report"
        // (the no-lane-time sentinel beside a NaN distance) and a bare 0 in a comparison. ANY other
        // number here — a fallback distance, a cruise speed, a padding factor — is a measurement the
        // world was never asked for, which is precisely the bug class this whole file exists to avoid.
        var invented = Regex.Matches(terms, @"(?<![\w.])\d[\d_]*(?:\.\d+)?(?:[eE][-+]?\d+)?(?![\w])")
            .Select(m => m.Value)
            .Where(v => v is not ("0" or "0.0"))
            .ToArray();
        Assert.Empty(invented);
    }

    /// <summary>The unmeasurable case really is handed back as "no number", which is the only reason
    /// Core's honest-omission branches ever run.</summary>
    [Fact]
    public void AnUnplaceableTargetIsHandedBackAsNoNumber()
    {
        string terms = CodeOnly(Source("Pages", "Map.Quests.Terms.cs"));
        Assert.Contains("return (double.NaN, 0.0);", terms, StringComparison.Ordinal);
    }

    // ── Slicing helpers ────────────────────────────────────────────────────────────────────────────

    /// <summary>Both needles are present AND the first runs before the second. Asserting presence first
    /// matters: an IndexOf of −1 for a DELETED block would otherwise satisfy "comes earlier" and let the
    /// whole block go missing under a passing test — a green test that asserts nothing.</summary>
    private static void AssertRunsBefore(string markup, string first, string second, string because)
    {
        int a = markup.IndexOf(first, StringComparison.Ordinal);
        int b = markup.IndexOf(second, StringComparison.Ordinal);
        Assert.True(a >= 0, $"'{first}' is not on this card at all");
        Assert.True(b >= 0, $"'{second}' is not on this card at all");
        Assert.True(a < b, because);
    }


    /// <summary>
    /// The bar stranger's contract card, sliced out of Map.razor the way #838's guards slice a panel.
    ///
    /// <para>#997 wave 10 · It is an <c>&lt;OverlayShell&gt;</c> now, so both of the old anchors are gone:
    /// the root is no longer <c>&lt;div class="deck-offer-card"&gt;</c>, and the actions row it used to end
    /// at is drawn by the shell rather than typed. Anchoring on the class alone found the RUMOUR card's
    /// plain <c>&lt;div class="deck-offer-card"&gt;</c> further down the file and reported this card's whole
    /// plain block missing — a guard handed the wrong world, which is this repo's fifth named bug class,
    /// rather than a card that had changed at all.</para>
    ///
    /// <para>So the slice is anchored on the verb only this card has — <c>OnClose="AcceptOffer"</c>, taking
    /// the job — walked back to the tag carrying it and forward to that tag's own close. Two shells in this
    /// file wear <c>deck-offer-card</c> on a Bare frame (this one and the patron's table); only one of them
    /// accepts an offer.</para>
    /// </summary>
    private static string OfferCardMarkup()
    {
        string map = CodeOnly(Source("Pages", "Map.razor"));
        int verb = map.IndexOf("OnClose=\"AcceptOffer\"", StringComparison.Ordinal);
        Assert.True(verb >= 0,
            "the bar's offer card no longer takes the job: OnClose=\"AcceptOffer\" is gone from Map.razor.");
        int start = map.LastIndexOf("<OverlayShell", verb, StringComparison.Ordinal);
        Assert.True(start >= 0, "the bar's offer card is gone from Map.razor");
        int end = map.IndexOf("</OverlayShell>", verb, StringComparison.Ordinal);
        Assert.True(end > start, "the offer card's shell is never closed");
        return map[start..end];
    }

    /// <summary>One ledger job row, from the card div to the foot that carries status and purse.</summary>
    private static string LedgerRowMarkup()
    {
        string captain = CodeOnly(Source("Pages", "Stations", "Captain.razor"));
        int start = captain.IndexOf("captain-quest-@q.StatusKind", StringComparison.Ordinal);
        Assert.True(start >= 0, "the Captain desk's job row is gone");
        int end = captain.IndexOf("captain-quest-foot", start, StringComparison.Ordinal);
        Assert.True(end > start, "the job row no longer closes with its foot");
        return captain[start..end];
    }
}
