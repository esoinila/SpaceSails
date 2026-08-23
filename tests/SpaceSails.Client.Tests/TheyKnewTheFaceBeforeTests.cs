using System;
using System.IO;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #973 L5a · THE OLD CREW, ON THE SCREEN. Core decides who is cast, who is bound to whom, where they work
/// and what everybody says (<c>TheOldCrewTests</c> holds all of that). What is left is the half only the
/// client can get wrong, and it is the half this repo has paid for repeatedly: a rule that is true in the
/// model and unreachable, double-told or silently once-a-life-too-often on the surface.
///
/// <para><b>Why these are source-shape guards.</b> This project has no component renderer, so every client
/// audit here reads the shipping markup and the shipping method bodies. That is also the honest shape for
/// the claims below, because all of them are about PLACEMENT and ROUTING rather than about a value.</para>
/// </summary>
public sealed class TheyKnewTheFaceBeforeTests
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

    private static string Pages(params string[] file) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", "Pages", .. file]));

    /// <summary>One method body, from its signature to the next member at the same indent — the same cut the
    /// sibling client guards make, so a body read here is a body read there.</summary>
    private static string Method(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"the client no longer has `{signature}` — this guard cannot find what it audits.");
        int end = source.IndexOf("\n    }", start, StringComparison.Ordinal);
        Assert.True(end > start, $"`{signature}` does not close where this guard expects.");
        return source[start..end];
    }

    // ── THE FACE PLAYS ONCE PER CONTACT PER LIFE ─────────────────────────────────────────────────────

    /// <summary>
    /// THE THREE CONDITIONS, ALL OF THEM ON THE ONE PREDICATE. A person may only say <i>you look different</i>
    /// when they knew the old face, when there IS a different face (the thread has buried a captain), and
    /// when this life has not already had the scene with them. Three separate places asking three separate
    /// halves of this is how a scene ends up playing twice in one evening.
    /// </summary>
    [Fact]
    public void OnlySomebodyWhoKnewTheFaceAfterARebirthAndNotYetThisLifeMayNoticeIt()
    {
        string body = Method(Pages("Map.OldCrew.cs"), "private bool TheyWouldNoticeTheFace(string giver)");

        Assert.Contains("KnewTheOldFace", body, StringComparison.Ordinal);
        Assert.Contains("Retired.Count", body, StringComparison.Ordinal);
        Assert.Contains("_facesExplained.Contains(giver)", body, StringComparison.Ordinal);
    }

    /// <summary>…and answering LATCHES it, in the same method that writes the crossing. A scene whose latch
    /// lived anywhere else would be a scene that could be answered twice by clicking twice.</summary>
    [Fact]
    public void AnsweringLatchesTheSceneAndWritesTheCrossing()
    {
        string body = Method(Pages("Map.OldCrew.cs"), "private void AnswerTheFace(OldCrewScene.Answer answer)");

        Assert.Contains("_facesExplained.Add(giver)", body, StringComparison.Ordinal);
        Assert.Contains("CaptainCrossings.OwnFace(answer", body, StringComparison.Ordinal);
        Assert.Contains("_crossings = CaptainCrossings.Add(", body, StringComparison.Ordinal);
    }

    /// <summary>The latch is PER LIFE, not per game: a rebirth is a new face and every one of them has to
    /// explain itself again. The set is cleared on the load path with the rest of this lane's per-life state.</summary>
    [Fact]
    public void TheLatchIsClearedWhenALifeIsLoadedOrForgotten()
    {
        string source = Pages("Map.OldCrew.cs");

        Assert.Contains("_facesExplained.Clear();",
            Method(source, "private void RestoreOldCrewSections(Vault vault)"), StringComparison.Ordinal);
        Assert.Contains("_facesExplained.Clear();",
            Method(source, "private void ForgetTheOldCrew()"), StringComparison.Ordinal);
    }

    /// <summary>Only the LIE is marked on their page, and it is marked through the ledger's own mutator
    /// rather than by reaching into the book. The truth and the policy line cost nothing but the saying.</summary>
    [Fact]
    public void OnlyTheLieIsWrittenOnTheirPage()
    {
        string body = Method(Pages("Map.OldCrew.cs"), "private void AnswerTheFace(OldCrewScene.Answer answer)");

        Assert.Contains("OldCrewScene.Answer.ALie", body, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(body, "_contacts.RecordLie("));

        // …and the mark sits INSIDE the lie's branch. A RecordLie above the `if` would mark every answer,
        // which is the whole of the failure: a captain who told the truth carrying a book that says he did
        // not.
        int branch = body.IndexOf("OldCrewScene.Answer.ALie", StringComparison.Ordinal);
        Assert.True(body.IndexOf("_contacts.RecordLie(", StringComparison.Ordinal) > branch,
            "the lie is marked before the answer is known to be a lie.");
    }

    // ── THE SCENE IS ON THE SCREEN, AND IT BLOCKS THE GLASS ──────────────────────────────────────────

    /// <summary>
    /// The card really renders the three answers off Core's own words — the buttons, what the captain says,
    /// and the reply — rather than three sentences typed into the markup. Every in-fiction line in this lane
    /// is Fable's and has exactly one home.
    /// </summary>
    [Fact]
    public void TheCardSpeaksCoresWordsAndNobodyElses()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("_faceScene is { } faceGiver", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer raises the face scene this guard knows how to find.");
        string card = razor[start..razor.IndexOf("parrot-perch", start, StringComparison.Ordinal)];

        Assert.Contains("OldCrewScene.Opening(", card, StringComparison.Ordinal);
        Assert.Contains("OldCrewScene.Button(", card, StringComparison.Ordinal);
        Assert.Contains("OldCrewScene.Said(", card, StringComparison.Ordinal);
        Assert.Contains("_faceSceneReply", card, StringComparison.Ordinal);
        Assert.Contains("AnswerTheFace(", card, StringComparison.Ordinal);
    }

    /// <summary>…and until it has been answered, the glass is not on offer. They are looking at the face
    /// before they are looking at the drink, which is the order the scene only reads right in.</summary>
    [Fact]
    public void TheGlassWaitsUntilTheFaceHasBeenExplained()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("private RenderFragment ContactDrinkOffer(", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the shared drink-offer fragment.");
        string fragment = razor[start..razor.IndexOf("</text>;", start, StringComparison.Ordinal)];

        Assert.Contains("TheyWouldNoticeTheFace(giver)", fragment, StringComparison.Ordinal);
        Assert.Contains("OpenTheFaceScene(giver)", fragment, StringComparison.Ordinal);

        // The face gate comes BEFORE the offer moment, and the offer moment is its `else`: two independent
        // ifs would render both, which is a captain being asked what happened to his face and offered a
        // whisky in the same breath.
        int gate = fragment.IndexOf("TheyWouldNoticeTheFace(giver)", StringComparison.Ordinal);
        int offer = fragment.IndexOf("else if (_pendingContactDrink == giver)", StringComparison.Ordinal);
        Assert.True(offer > gate, "the drink offer no longer hangs off the face gate's else.");
    }

    /// <summary>The bond is readable BEFORE the captain knocks — the whole of the Fail Forward adoption.
    /// It is drawn from Core's own history line, in the shared fragment both doorways render.</summary>
    [Fact]
    public void TheBondShowsBeforeYouWalkIn()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("private RenderFragment ContactDrinkOffer(", StringComparison.Ordinal);
        string fragment = razor[start..razor.IndexOf("</text>;", start, StringComparison.Ordinal)];

        Assert.Contains("OldCrewHistoryLine(giver)", fragment, StringComparison.Ordinal);
        int line = fragment.IndexOf("old-crew-history", StringComparison.Ordinal);
        int gate = fragment.IndexOf("TheyWouldNoticeTheFace(giver)", StringComparison.Ordinal);
        Assert.True(line >= 0 && line < gate, "the bond line no longer renders ahead of the face gate.");
    }

    // ── THE PHOTOGRAPH ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SHEET EXISTS AND IT HAS FOUR THREADS. The four names come out of Core's own roll (which may put
    /// the dead man among them), it is marked <b>his</b> because a person handed it over, and it raises the
    /// flashback beat with the photograph's own subject rather than a ledger key.
    /// </summary>
    [Fact]
    public void ThePhotographIsAHeldMemoryWithFourThreadsAndItRaisesTheBeat()
    {
        string body = Method(Pages("Map.OldCrew.cs"),
            "private void HandOverThePhotograph(string shipmateId, string display)");

        Assert.Contains("HeldMemory.PhotographId", body, StringComparison.Ordinal);
        Assert.Contains("HeldMemory.Mark.His", body, StringComparison.Ordinal);
        Assert.Contains("OldCrewScene.Photograph", body, StringComparison.Ordinal);
        Assert.Contains("OldCrewScene.PhotographFaces(", body, StringComparison.Ordinal);
        Assert.Contains("StoryBeats.Beat.Flashback, OldCrewScene.PhotographSubject", body, StringComparison.Ordinal);

        // …and the four threads really are four, asked of the world rather than counted in the markup.
        OldCrew.Berth[] berths =
        [
            new("ringside", ArrivalTube.Tier.GreatPort),
            new("selene-gate", ArrivalTube.Tier.WorkingBerth),
        ];
        Assert.Equal(4, OldCrewScene.PhotographFaces("a-thread", OldCrew.Seed("a-thread", berths)).Count);
    }

    /// <summary>It is handed over ONCE, by the one person the thread put it with — a second witness holding
    /// the same picture twice is one picture.</summary>
    [Fact]
    public void ThePhotographIsHandedOverOnceAndOnlyByItsHolder()
    {
        string body = Method(Pages("Map.OldCrew.cs"),
            "private void HandOverThePhotograph(string shipmateId, string display)");

        Assert.Contains("OldCrewScene.PhotographHeldBy(TheOldCrew)", body, StringComparison.Ordinal);
        Assert.Contains("HeldMemory.Find(_heldMemories, HeldMemory.PhotographId) is not null", body, StringComparison.Ordinal);
    }

    // ── THE PRICES ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SIGNER FILES ONCE PER VISIT, and he files through #715's ONE banking seam. A second banking path
    /// anywhere is the bug that feature's fourth guard exists to catch, and a report that fired on every
    /// glass would empty a port's patience in an evening.
    /// </summary>
    [Fact]
    public void TheSignerFilesOncePerVisitThroughTheOneSeam()
    {
        string body = Method(Pages("Map.OldCrew.cs"), "private void TheSignerReports()");

        Assert.Contains("FreshVisitIfMoved();", body, StringComparison.Ordinal);
        Assert.Contains("_signerReportedFor.Add(here)", body, StringComparison.Ordinal);
        Assert.Contains("BankTheCrossing(OldCrew.SignerReport(here))", body, StringComparison.Ordinal);

        // …and nothing in this lane banks heat any other way.
        string source = Pages("Map.OldCrew.cs");
        Assert.DoesNotContain("IllegalHeat.Bank(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyHeat(", source, StringComparison.Ordinal);
    }

    /// <summary>The visit ends by being LEFT: the once-per-visit books roll over when the captain is tied up
    /// somewhere new. It is one method and both books empty in it, so a third price added later cannot
    /// forget to reset.</summary>
    [Fact]
    public void AVisitEndsWhenTheCaptainTiesUpSomewhereElse()
    {
        string body = Method(Pages("Map.OldCrew.cs"), "private void FreshVisitIfMoved()");

        Assert.Contains("_dockedHavenId", body, StringComparison.Ordinal);
        Assert.Contains("_signerReportedFor.Clear();", body, StringComparison.Ordinal);
        Assert.Contains("_knockedThisVisit.Clear();", body, StringComparison.Ordinal);
    }

    /// <summary>The pip is spent at the door and only at that door: the best friend, at a berth where the
    /// book already says she is with him, once per visit, through the one nerve seam.</summary>
    [Fact]
    public void ThePipIsSpentAtTheOneDoorAndOncePerVisit()
    {
        string body = Method(Pages("Map.OldCrew.cs"), "private void PayForTheKnock(string giver)");

        Assert.Contains("OldCrew.BestFriendId", body, StringComparison.Ordinal);
        Assert.Contains("OldCrew.KnockingCostsNerve(", body, StringComparison.Ordinal);
        Assert.Contains("_knockedThisVisit.Add(giver)", body, StringComparison.Ordinal);
        Assert.Contains("ApplyNerveShock(NervePips.PipUnit * OldCrewScene.KnockNervePips", body, StringComparison.Ordinal);
        Assert.Contains("OldCrewScene.AtTheRegistrarsDoor", body, StringComparison.Ordinal);
    }

    // ── THE PAGE THE LINE CANNOT TAKE ────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MARK REACHES THE LAW. The summer-party row carries <c>Filed: true</c> out of the held-memory book,
    /// and the filing line's page list passes that mark straight through — a row that carried the mark to the
    /// desk but not to the rule would be a page the player is told is filed and that greys anyway.
    /// </summary>
    [Fact]
    public void TheFiledMarkRidesFromTheSheetAllTheWayToTheGreying()
    {
        Assert.Contains("Filed: sheet.Filed",
            Method(Pages("Map.OldCrew.cs"), "private IEnumerable<Stations.Captain.LedgerTip> HeldMemoryTips()"),
            StringComparison.Ordinal);
        Assert.Contains("tip.Provenance ?? \"\", tip.Filed",
            Method(Pages("Map.FilingLine.cs"), "private IReadOnlyList<LedgerPage> LedgerPagesForFiling()"),
            StringComparison.Ordinal);
    }

    /// <summary>The seeded page really is in the ledger the desk draws — it is added to the same list the
    /// six books are, so it greys (or does not) by exactly the rule everything else does.</summary>
    [Fact]
    public void TheHeldMemoriesAreRowsOfTheOneLedger()
    {
        Assert.Contains("tips.AddRange(HeldMemoryTips());",
            Pages("Map.Quests.Ledger.cs"), StringComparison.Ordinal);
    }

    // ── THE DESK ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The Captain's desk shows the crossings as ROWS and shows no number about them. §1 of
    /// the-captains-character.md is explicit that a meter invites grinding, and "add a summary line" is the
    /// first thing anybody reaches for.</summary>
    [Fact]
    public void TheDeskShowsTheCrossingsAndNeverAScore()
    {
        string razor = Pages("Stations", "Captain.razor");
        int start = razor.IndexOf("@CrossingsHeading", StringComparison.Ordinal);
        Assert.True(start >= 0, "the Captain desk no longer has a crossings section.");
        string section = razor[start..razor.IndexOf("💰 Accounts", start, StringComparison.Ordinal)];

        Assert.Contains("foreach (string row in Crossings)", section, StringComparison.Ordinal);
        Assert.DoesNotContain("Crossings.Count.ToString", section, StringComparison.Ordinal);
        Assert.DoesNotContain("Sum(", section, StringComparison.Ordinal);
    }

    /// <summary>…and Map hands it Core's rows and Core's heading, rather than sentences built at the desk.</summary>
    [Fact]
    public void MapHandsTheDeskCoresRowsAndCoresHeading()
    {
        Assert.Contains("Crossings=\"CrossingRows()\" CrossingsHeading=\"@CaptainCrossings.Heading\"",
            Pages("Map.razor"), StringComparison.Ordinal);
    }

    // ── THE ROOM ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>An old shipmate posted at this berth joins the SAME contact list the fixers are on, so the
    /// drink, the offer moment and both doorways are the shipped ones rather than a second flow beside
    /// them.</summary>
    [Fact]
    public void TheOldCrewJoinTheOneContactList()
    {
        string body = Method(Pages("Map.Quests.Bar.cs"),
            "private IReadOnlyList<(string Giver, string Display)> PresentBarContacts()");

        Assert.Contains("OldCrewHere", body, StringComparison.Ordinal);
        Assert.Contains("Core.OldCrew.LedgerId(s.Id)", body, StringComparison.Ordinal);
    }

    /// <summary>Both rolls are handed the SAME room. Two calls building their own would be two answers to
    /// "who else is standing here", which is the two-meters-that-must-agree bug this house keeps a table
    /// of.</summary>
    [Fact]
    public void TheOfferAndTheGlassReadOneRoom()
    {
        string body = Method(Pages("Map.Quests.Bar.cs"),
            "private void BuyContactDrink(string giver, bool offeringUsual = false)");

        Assert.Contains("ContactDrink.TheRoom room = TheRoomFor(giver);", body, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(body, "TheRoomFor("));
        Assert.Contains("offeringFavorite, room)", body, StringComparison.Ordinal);
        Assert.Contains("offeringFavorite, room);", body, StringComparison.Ordinal);
    }

    private static int Occurrences(string haystack, string needle)
    {
        int count = 0;
        for (int at = haystack.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = haystack.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
