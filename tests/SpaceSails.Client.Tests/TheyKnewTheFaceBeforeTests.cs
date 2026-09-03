using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #973 L5a · THE OLD CREW, ON THE SCREEN. Core decides who is cast, who is bound to whom, where they work
/// and what everybody says (<c>TheOldCrewTests</c> holds all of that). What is left is the half only the
/// client can get wrong, and it is the half this repo has paid for repeatedly: a rule that is true in the
/// model and unreachable, double-told or silently once-a-life-too-often on the surface.
///
/// <para><b>Why some of these are source-shape guards.</b> This project has no component renderer, so a
/// client audit of MARKUP reads the shipping markup, and a claim about ROUTING (who calls what, and where
/// from) reads the call site. Everything that is about a VALUE is driven on a real page instead — see the
/// bench at the foot of this file. That line moved on 2026-08-23 and it cost something: the reload guard
/// below used to read two method bodies for the words that write and read the vault's old-crew section, and
/// it passed happily for the whole time <see cref="VaultSerializer"/> had no such section and every save
/// dropped the latch, the crossing and the sheets on the floor.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
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
        MapMarkup.Read(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", "Pages", .. file]));

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

    /// <summary>
    /// THE LATCH IS PER LIFE, AND IT SURVIVES THE FILE — <b>driven, not read</b>.
    ///
    /// <para>The first cut of this guard asserted the SOURCE TEXT of <c>BuildOldCrewSection</c> and
    /// <c>RestoreOldCrewSections</c>: that the words <c>Explained = [.. _facesExplained]</c> appear in the
    /// one and <c>vault.OldCrew?.Explained</c> in the other. Both were true, both stayed true, and the latch
    /// was dropped on every save anyway — because <see cref="VaultSerializer"/> had no <c>oldcrew</c>
    /// section at all, so the page built one and it went straight in the bin. A guard that reads two method
    /// bodies cannot see the third place.</para>
    ///
    /// <para>So it is played instead: a real page seeds the crew, the captain answers a shipmate who knew
    /// the old face, the page's own builder writes the section, the serializer's REAL JSON carries it
    /// (checksum, canonicalization, per-section harvest), and a SECOND page reads it back and is asked the
    /// live question — <i>would this person still notice the face?</i> The answer has to be no, or the
    /// reload just handed the player the same question a second time and a second crossing for it.</para>
    /// </summary>
    [Fact]
    public void TheLatchSurvivesARealSaveAndLoad()
    {
        Pages.Map map = ACaptainWithANewFace();
        string giver = ThePhotographHolder(map);

        Assert.True((bool)Invoke(map, "TheyWouldNoticeTheFace", giver)!);
        Invoke(map, "OpenTheFaceScene", giver);
        Invoke(map, "AnswerTheFace", OldCrewScene.Answer.TheTruth);
        Assert.False((bool)Invoke(map, "TheyWouldNoticeTheFace", giver)!,
            "answering did not latch the scene on the live page — nothing below would be testing a reload.");

        // The page's own builder → the serializer's real bytes.
        var written = new Vault { OldCrew = (OldCrewSection?)Invoke(map, "BuildOldCrewSection") };
        Assert.NotNull(written.OldCrew);
        Assert.Contains(giver, written.OldCrew!.Explained);

        string json = VaultSerializer.Save(written);
        Assert.Contains(giver, json, StringComparison.Ordinal);   // it REACHED THE FILE. This is the bug.

        Vault back = VaultSerializer.Load(json);
        Assert.False(back.Tampered, "the old-crew section broke the checksum it is folded into.");
        Assert.NotNull(back.OldCrew);

        // A SECOND page, same universe, same buried captain — it would ask again, until the file says not to.
        Pages.Map reloaded = ACaptainWithANewFace();
        Assert.True((bool)Invoke(reloaded, "TheyWouldNoticeTheFace", giver)!);

        Invoke(reloaded, "RestoreOldCrewSections", back);

        Assert.Contains(giver, (IEnumerable<string>)Field(reloaded, "_facesExplained")!);
        Assert.False((bool)Invoke(reloaded, "TheyWouldNoticeTheFace", giver)!,
            "the reloaded captain is about to be asked what happened to his face for the second time.");
    }

    /// <summary>
    /// …AND SO DO THE CROSSING AND THE SHEETS. The same drive, the same file, the other two sections that
    /// were declared on the vault and never written: the captain's ⚖ crossing (#973 L5a) and the black
    /// book's held-memory pages (#978 — the fleet-day page, and the photograph a person hands over).
    ///
    /// <para>Asked of the far side the way the game asks: the desk's own <c>CrossingRows()</c>, and
    /// <see cref="HeldMemory.Find"/> over the reloaded book. A section that does not reach the file comes
    /// back null, and the page comes back with an empty desk and a book that lost a photograph.</para>
    /// </summary>
    [Fact]
    public void TheCrossingAndTheHeldSheetsSurviveARealSaveAndLoad()
    {
        Pages.Map map = ACaptainWithANewFace();
        string giver = ThePhotographHolder(map);

        Invoke(map, "OpenTheFaceScene", giver);
        Invoke(map, "AnswerTheFace", OldCrewScene.Answer.ALie);

        var crossings = (IReadOnlyList<CaptainCrossings.Crossing>)Field(map, "_crossings")!;
        Assert.Single(crossings);
        var rowsBefore = (IReadOnlyList<string>)Invoke(map, "CrossingRows")!;

        // The photograph really came out of the scene, so the book below has something to lose.
        Assert.NotNull(HeldMemory.Find(Book(map), HeldMemory.PhotographId));
        Assert.NotNull(HeldMemory.Find(Book(map), OldCrewScene.SummerPartyId));

        var written = new Vault
        {
            OldCrew = (OldCrewSection?)Invoke(map, "BuildOldCrewSection"),
            Crossings = (CrossingsSection?)Invoke(map, "BuildCrossingsSection"),
            HeldMemories = (HeldMemoriesSection?)Invoke(map, "BuildHeldMemoriesSection"),
        };
        Assert.NotNull(written.Crossings);
        Assert.NotNull(written.HeldMemories);

        string json = VaultSerializer.Save(written);
        Assert.Contains(HeldMemory.PhotographId, json, StringComparison.Ordinal);
        Assert.Contains(OldCrewScene.SummerPartyId, json, StringComparison.Ordinal);
        // …and the crossing's own row, asked of the loaded sections rather than of the raw text: the witness
        // is a person's name and the writer escapes what a name contains, which is the file doing its job.
        Vault back = VaultSerializer.Load(json);
        Assert.False(back.Tampered);
        Assert.Empty(back.Warnings);
        Assert.NotNull(back.Crossings);
        Assert.Contains(crossings[0].Stored, back.Crossings!.Crossings);
        Assert.NotNull(back.HeldMemories);

        // A second page: a desk with nothing on it and a book with no photograph in it, until the file lands.
        Pages.Map reloaded = ACaptainWithANewFace();
        Assert.Empty((IReadOnlyList<string>)Invoke(reloaded, "CrossingRows")!);
        Assert.Null(HeldMemory.Find(Book(reloaded), HeldMemory.PhotographId));

        Invoke(reloaded, "RestoreOldCrewSections", back);

        Assert.Equal(rowsBefore, (IReadOnlyList<string>)Invoke(reloaded, "CrossingRows")!);
        Assert.NotNull(HeldMemory.Find(Book(reloaded), OldCrewScene.SummerPartyId));

        // …and the photograph came back WHOLE — the words a person handed over, not a rebuilt sentence.
        HeldMemory.Sheet photo = HeldMemory.Find(Book(reloaded), HeldMemory.PhotographId)
            ?? throw new InvalidOperationException("the photograph did not survive the file.");
        Assert.Equal(OldCrewScene.Photograph, photo.Text);
        Assert.Equal(HeldMemory.Mark.His, photo.Mark);
        Assert.Equal(4, photo.Threads.Count);
    }

    /// <summary>
    /// A REBIRTH EMPTIES IT — driven on the page, and routed from the succession seam. The clear itself is
    /// played (a new face has nothing explained, so the person who asked once asks again); the CALL SITE is
    /// the one claim here that is genuinely about placement, so it is read where it is written.
    /// </summary>
    [Fact]
    public void ARebirthEmptiesTheLatchAndANewUniverseForgetsThemEntirely()
    {
        Pages.Map map = ACaptainWithANewFace();
        string giver = ThePhotographHolder(map);

        Invoke(map, "OpenTheFaceScene", giver);
        Invoke(map, "AnswerTheFace", OldCrewScene.Answer.ThePolicyLine);
        Assert.False((bool)Invoke(map, "TheyWouldNoticeTheFace", giver)!);

        Invoke(map, "ANewFaceHasNothingExplained");

        Assert.True((bool)Invoke(map, "TheyWouldNoticeTheFace", giver)!,
            "the next captain walks past the one person who would not know him.");
        Assert.Null(Field(map, "_faceScene"));

        // …and it is called at the succession, which is a routing claim and reads as one.
        Assert.Contains("ANewFaceHasNothingExplained();",
            Pages("Map.Combat.Busted.cs"), StringComparison.Ordinal);

        // A NEW UNIVERSE forgets them entirely — driven too: the crew, the book and the latch all go.
        Invoke(map, "OpenTheFaceScene", giver);
        Invoke(map, "AnswerTheFace", OldCrewScene.Answer.TheTruth);
        Invoke(map, "ForgetTheOldCrew");
        Assert.Empty((IEnumerable<string>)Field(map, "_facesExplained")!);
        Assert.Empty((IReadOnlyList<CaptainCrossings.Crossing>)Field(map, "_crossings")!);
        Assert.Empty(Book(map));
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

    // ── THE BENCH ────────────────────────────────────────────────────────────────────────────────────

    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const string ThreadId = "9f3c71ab54d8402e8c17ba26d0e5391f";
    private const string TheRedEye = "red-eye";
    private const string TheOtherPort = "ringside-exchange";

    /// <summary>
    /// A page in a universe that has buried a captain, tied up at a berth, with the four shipmates cast and
    /// booked. That is every condition the face scene asks for and no more: the crew are seeded lazily off
    /// the thread id (touching <c>TheOldCrew</c> is what casts them), the contacts book gets its four rows
    /// with the flag that says they knew the face, and the retired captain is the reason there IS a new one.
    /// </summary>
    private static Pages.Map ACaptainWithANewFace()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_ephemeris", TheWorld());
        Set(map, "_dockedHavenId", TheRedEye);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)
        [
            new GameThreadInfo { Id = ThreadId, Retired = [new RetiredCaptain("Someone Who Died", 12)] },
        ]);

        // Cast them. The property is the seeding seam, so this is the game's own first read and not a poke.
        Assert.Equal(OldCrew.SeededPerThread, Crew(map).Count);
        return map;
    }

    /// <summary>The ledger id of the shipmate this thread put the photograph with — asked of Core, because
    /// which of the four it is a property of the roll and never of this test.</summary>
    private static string ThePhotographHolder(Pages.Map map) =>
        OldCrew.LedgerId(OldCrewScene.PhotographHeldBy(Crew(map)));

    private static IReadOnlyList<OldCrew.Seeded> Crew(Pages.Map map) =>
        (IReadOnlyList<OldCrew.Seeded>)Invoke(map, "get_TheOldCrew")!;

    private static IReadOnlyList<HeldMemory.Sheet> Book(Pages.Map map) =>
        (IReadOnlyList<HeldMemory.Sheet>)Field(map, "_heldMemories")!;

    /// <summary>Two great ports and the planets to hang them off — enough of a world for the postings.</summary>
    private static ICelestialEphemeris TheWorld() =>
        new CircularOrbitEphemeris(
        [
            new CelestialBody("sol", "Sol", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("jupiter", "Jupiter", "sol", 1.267e17, 6.99e7, 7.78e11, 3.7e5, 0),
            new CelestialBody("saturn", "Saturn", "sol", 3.79e16, 5.82e7, 1.43e12, 2.2e5, 0),
            new CelestialBody(TheRedEye, "The Red Eye", "jupiter", 0, 0, 5e8, 4e4, 0,
                BodyKind.Station, IsHaven: true),
            new CelestialBody(TheOtherPort, "Ringside Exchange", "saturn", 0, 0, 5e8, 4e4, 0,
                BodyKind.Station, IsHaven: true),
        ]);

    private static object? Field(Pages.Map map, string name) =>
        (typeof(Pages.Map).GetField(name, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name."))
        .GetValue(map);

    private static void Set(Pages.Map map, string name, object? value)
    {
        if (typeof(Pages.Map).GetField(name, Hidden) is { } field)
        {
            field.SetValue(map, value);
            return;
        }

        (typeof(Pages.Map).GetProperty(name, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{name}`.")).SetValue(map, value);
    }

    private static object? Invoke(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{method}` — this guard is reading a dead name."))
        .Invoke(map, args);

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
