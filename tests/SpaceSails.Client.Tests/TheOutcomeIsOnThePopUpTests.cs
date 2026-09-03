using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #736 · THE RESULT IS UNDER THE BLUR AGAIN. Owner, restating #680's law as a general rule: <i>"When an
/// action is made on a pop-up, the result text must be readable on that pop-up — not on the blurred
/// background. All actions, like using the elevator, items, etc. should report to the pop-up so the text is
/// not blurred by the modal backdrop."</i>
///
/// <para>The fresh repro is the freight agent's docket: <c>?kaamos=bounce&amp;ashore=1&amp;start=the-tilt</c>
/// → Gilt-Eye → Take the job → the RETURNED TO SENDER card opens and <i>"the MECHANICAL outcome lands in the
/// top pulse banner BEHIND the card's backdrop… The 350 cr and the receipt — the two facts the player acts
/// on — are the dimmed text. The card in front carries neither."</i></para>
///
/// <para><b>Why these are source-shape guards, and what makes them able to fail.</b> Owner, on #680: <i>"note
/// that seeing the text in DOM does not mean user can see it on the screen."</i> Every broken build in this
/// family had the sentence in the DOM the whole time — a presence assertion would have been green on the bug,
/// which is this house's fifth named bug class. What decides readability is PLACEMENT, so each test below
/// pins a line to a SUBTREE: the card record that only the card renders, or the block of Map.razor that one
/// pop-up owns. <see cref="EveryPopUpTheSeamRoutesToRendersItsOwnOutcome"/> goes further and reads the seam's
/// own routing table out of the source, so a tenth branch added to the law with no row in the markup is red
/// without anybody remembering to come back here.</para>
///
/// <para>Sister guards, same law, older organs: <see cref="TheTryOutcomeIsReadableTests"/> (#680, the
/// satchel) and <see cref="TheLiftRefusalIsReadableTests"/> (#686, the car panel).</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheOutcomeIsOnThePopUpTests
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

    private static string Pages(string file) =>
        MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    /// <summary>One method's body, from its signature to the next member at the same indent. The same cut
    /// <see cref="TheTryOutcomeIsReadableTests"/> makes, so a body read here is a body read there.</summary>
    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        int end = src.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        return src[at..(end > at ? end : src.Length)];
    }

    /// <summary>The markup ONE pop-up owns: from its own `@if (…` to the next top-level `@if (` in Map.razor.
    /// Cutting the block is the whole point — a line that renders somewhere else in this 5,700-line file is a
    /// line on a different pop-up, which is the bug and not the fix.</summary>
    private static string BlockOf(string opening)
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf(opening, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Map.razor no longer has the `{opening}` block this guard knows how to find.");
        int end = razor.IndexOf("\n    @if (", start + opening.Length, StringComparison.Ordinal);
        return razor[start..(end > start ? end : razor.Length)];
    }

    // ── The repro, first ──────────────────────────────────────────────────────────────────────────────

    /// <summary>#736's own acceptance: taking the returned filing must put the RECEIPT on the card that same
    /// press raises. RED on the shipped behaviour, which pulsed the receipt and then raised a card on top of
    /// it — the line was in the DOM, under two millimetres of frosted glass.</summary>
    [Fact]
    public void TheReturnedFilingsReceiptRidesTheCardItRaises()
    {
        string taking = Method("Map.Kaamos.cs", "private void TakeKaamosBounceFiling(");

        // #664 · the card is raised through the ONE seam now (RaiseStoryBeat), not by hand. The law is
        // untouched: the receipt has to be an argument of the raise that opens the card.
        int card = taking.IndexOf("RaiseStoryBeat(", StringComparison.Ordinal);
        Assert.True(card >= 0,
            "TakeKaamosBounceFiling no longer raises the RETURNED TO SENDER card — this guard needs re-reading.");

        int receipt = taking.IndexOf("KaamosLore.BounceReceipt(", StringComparison.Ordinal);
        Assert.True(receipt > card,
            "the fee receipt is not an argument of the card the press raises — the 350 cr and the kept " +
            "receipt are said behind the card's own backdrop, which is the exact repro #736 was filed on.");

        Assert.DoesNotContain("ShowPulseMessage(KaamosLore.BounceReceipt", taking, StringComparison.Ordinal);
    }

    // ── The seam, and the markup it presumes ──────────────────────────────────────────────────────────

    /// <summary>The card carries the arithmetic of the act that raised it, and the card's own subtree renders
    /// it. Two halves of one fact: a record field nothing draws is a field, not a sentence.</summary>
    [Fact]
    public void TheRaisedCardHasAnOutcomeOfItsOwnAndDrawsIt()
    {
        // #664 · The record this used to read lived in Map.RevealCard.cs, the client-only twin of the story
        // card that the fork built and the merge kept. There is one card now; the Outcome channel moved to it
        // whole, and the claim below is the same claim about the same sentence on the same screen.
        string card = Pages("Map.StoryCards.cs");
        Assert.Contains("string? Outcome", card, StringComparison.Ordinal);

        string block = BlockOf("@if (_storyCard is { } told)");
        Assert.True(block.Contains("told.Outcome", StringComparison.Ordinal),
            "the story card never renders its own Outcome — an act's result is stored and then shown " +
            "nowhere (#736).");
        Assert.True(block.Contains("reveal-outcome", StringComparison.Ordinal),
            "the story card has no styled outcome row (#736).");
    }

    /// <summary>THE LAW READS ITSELF. <c>SayItWhereTheyAreLooking</c> is the one place that decides which
    /// pop-up owns an answer; every field it writes to must be RENDERED by the pop-up that owns it. The table
    /// is extracted from the seam's own source, so a branch added there without a row in Map.razor is red
    /// here without anyone having to remember this file exists.</summary>
    [Fact]
    public void EveryPopUpTheSeamRoutesToRendersItsOwnOutcome()
    {
        string seam = Method("Map.Surface.Satchel.cs", "private void SayItWhereTheyAreLooking(");
        var slots = new List<string>();
        foreach (Match m in Regex.Matches(seam, @"(_[A-Za-z]\w*) = line;"))
        {
            slots.Add(m.Groups[1].Value);
        }

        // If this drops to a handful the extraction has broken, and every assertion below would pass on a
        // world that cannot tell pass from fail — the exact vacuous-green shape this house names as a bug.
        Assert.True(slots.Count >= 9,
            $"only {slots.Count} pop-up slots were read out of SayItWhereTheyAreLooking — the seam has been " +
            "rewritten in a shape this guard cannot read, so it is proving nothing.");

        string razor = Pages("Map.razor");
        foreach (string slot in slots)
        {
            Assert.True(razor.Contains(slot, StringComparison.Ordinal),
                $"the seam routes an outcome into `{slot}` and no pop-up in Map.razor ever draws it — the " +
                "answer is stored where nobody can read it, which is worse than the pulse it replaced (#736).");
        }

        // And the card, which does not take a field: it takes the record, so it dies with the card it belongs
        // to. #664 · that record is `_storyCard` now, the one card left standing.
        Assert.Contains("_storyCard = (card.Beat, card.Subject, line);", seam, StringComparison.Ordinal);
    }

    /// <summary>Each pop-up this sweep gave an answer to draws that answer INSIDE ITS OWN BLOCK. In the file
    /// is not on the pop-up, in the same way that in the DOM is not on the screen.</summary>
    [Theory]
    [InlineData("@if (_pinJob is { } pinJob)", "_pinOutcome")]
    [InlineData("@if (_showCaptainsRemote)", "_remoteOutcome")]
    [InlineData("@if (_showDoorBoard)", "_doorBoardOutcome")]
    [InlineData("@if (_showAlarmPanel)", "_alarmOutcome")]
    public void ThePanelDrawsItsAnswerInsideItself(string opening, string slot)
    {
        string block = BlockOf(opening);
        Assert.True(block.Contains(slot, StringComparison.Ordinal),
            $"`{opening}` never renders {slot} — the panel stays open on this answer and the answer plays " +
            "behind its own backdrop (#736).");
        Assert.True(block.Contains("panel-outcome", StringComparison.Ordinal),
            $"`{opening}` has no styled outcome row (#736).");
    }

    // ── The sweep, site by site ───────────────────────────────────────────────────────────────────────

    /// <summary>Every beat that raises a card ON THE SAME PRESS that produced a result must hand the card
    /// that result, rather than pulsing it under the card it is about to raise. The pairs are the sweep;
    /// each one was RED before this issue.</summary>
    [Theory]
    [InlineData("Map.Archive.cs", "private void PullArchiveSwitch(", "ArchiveNode.PurgeLine")]
    [InlineData("Map.Outpost.cs", "private void OutpostEffectsInteract(", "SurfaceOutpost.EffectsLine(")]
    [InlineData("Map.SecretLab.cs", "private bool TrySecretLabDetectorReveal(", "SEALED DOOR")]
    [InlineData("Map.SecretLab.cs", "private void FireSecretLabReveal(", "It salvages YOU")]
    [InlineData("Map.Venting.Doors.cs", "private void ReleaseWhatWasSealedIn(", "opens BOTH ways")]
    public void TheCardCarriesWhatThePressDid(string file, string signature, string line)
    {
        string body = Method(file, signature);

        int card = body.IndexOf("RaiseStoryBeat(", StringComparison.Ordinal);
        Assert.True(card >= 0, $"`{signature}` no longer raises a card — this guard needs re-reading.");

        // Searched FROM the card, so the only occurrence that can satisfy this is one inside the raise's own
        // argument list. A site that says the line first and raises the card afterwards — the shipped shape,
        // and the exact #736 bug — has nothing after `RaiseStoryBeat(` to find.
        int said = body.IndexOf(line, card, StringComparison.Ordinal);
        Assert.True(said > card,
            $"`{signature}` says its result somewhere other than on the card it raises — the #736 shape " +
            "exactly: the sentence is in the DOM and the picture is on the screen.");

        Assert.True(body.Contains("outcome:", StringComparison.Ordinal),
            $"`{signature}` raises a card without naming the outcome it carries (#736).");
    }

    /// <summary>The two arcs assemble their shards through one helper each, and both raise a plate. The found
    /// line has to be said AFTER the plate goes up, because that is what lets the seam find it — and it must
    /// go through the seam rather than straight to the HUD.</summary>
    [Theory]
    [InlineData("Map.Kaamos.cs", "private bool TryAssembleKaamos(")]
    [InlineData("Map.Nebula.cs", "private bool TryAssembleNebula(")]
    public void AnAssembledShardIsNarratedWhereverTheCaptainIsLooking(string file, string signature)
    {
        string body = Method(file, signature);

        int plate = body.IndexOf("RaiseStoryBeat(", StringComparison.Ordinal);
        int said = body.IndexOf("SayItWhereTheyAreLooking(", StringComparison.Ordinal);
        Assert.True(plate >= 0, $"`{signature}` no longer raises a plate — this guard needs re-reading.");
        Assert.True(said > plate,
            $"`{signature}` narrates the find before the plate goes up, so the seam cannot see the card and " +
            "the biggest sentence in the arc plays under it (#736).");

        Assert.DoesNotContain("ShowPulseMessage(foundMessage", body, StringComparison.Ordinal);
    }

    /// <summary>The boards a captain ARGUES with — they stay open by design, which is what makes a pulse from
    /// inside them unreadable by construction. Every answer they give goes through the seam.</summary>
    [Theory]
    [InlineData("Map.LabSecurity.cs", "private void WorkTheDoor(")]
    [InlineData("Map.LabSecurity.cs", "private void HackTheAlarm(")]
    [InlineData("Map.Combat.FireControl.cs", "private void ToggleWeaponsTight(")]
    [InlineData("Map.Combat.Remote.cs", "private void ToggleBoatPower(")]
    [InlineData("Map.Sounding.cs", "private void ToggleQuietSearch(")]
    [InlineData("Map.Venting.Doors.cs", "private void RescueSurvivor(")]
    public void AnOpenPanelNeverPulsesItsOwnAnswer(string file, string signature)
    {
        string body = Method(file, signature);
        Assert.True(body.Contains("SayItWhereTheyAreLooking(", StringComparison.Ordinal),
            $"`{signature}` answers without asking where the captain is looking (#736).");
        Assert.DoesNotContain("ShowPulseMessage(", body);
    }

    /// <summary>The keypad is the one panel with a success that CLOSES it, so it keeps the pulse for that and
    /// routes only the buzz. Stated as a guard because the asymmetry is deliberate and reads like an
    /// oversight — a later sweep would otherwise "finish the job" and delete the receipt.</summary>
    [Fact]
    public void TheKeypadSaysTheBuzzOnThePadAndTheGreenLightOnTheHud()
    {
        string body = Method("Map.Deck.Fixtures.cs", "private void SubmitPin(");

        Assert.True(body.Contains("SayItWhereTheyAreLooking(\"The panel buzzes red", StringComparison.Ordinal),
            "the wrong-code buzz is not said on the pad — the pad stays up, so a pulsed buzz is under its " +
            "own backdrop and all the captain sees is four dots going back to dots (#736).");
        Assert.DoesNotContain("ShowPulseMessage(\"The panel buzzes red", body, StringComparison.Ordinal);

        // The right code closes the pad on the line above its receipt, so the receipt belongs to the world.
        Assert.Contains("CancelPin();", body, StringComparison.Ordinal);
    }

    /// <summary>The odd book: a rack press that opens a card, and the shelf line that explains WHY one book
    /// caught the eye. It rides the card's own story rather than a pulse under it — the same composition
    /// #603's document card already uses for the answer to a try.</summary>
    [Fact]
    public void TheShelfLineIsComposedIntoTheCardItOpens()
    {
        string body = Method("Map.Surface.Hive.cs", "private void HiveHaulInteract(");

        Assert.True(body.Contains("shelf.Line + \"\\n\\n\" + shelf.Card", StringComparison.Ordinal),
            "the odd book's shelf line is not on the card the press opens — and the comment beside it has " +
            "said since #701 that a pulse here would play under the card's own blur (#736).");
        Assert.DoesNotContain("ShowPulseMessage(shelf.Line)", body, StringComparison.Ordinal);
    }

    // ── And it has to look like the rest of the family ────────────────────────────────────────────────

    [Fact]
    public void TheOutcomeRowsAreStyledLikeThePopUpsTheySitIn()
    {
        string css = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));

        foreach (string rule in new[] { ".reveal-outcome", ".panel-outcome" })
        {
            Assert.True(css.Contains(rule, StringComparison.Ordinal),
                $"`{rule}` has no style of its own — an unstyled answer in a styled pop-up is a bug report " +
                "waiting to be filed (#680's own words, #736's organs).");
        }
    }
}
