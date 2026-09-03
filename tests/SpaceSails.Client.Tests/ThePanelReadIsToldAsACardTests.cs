using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #684 · THE MACHINE GOES THROUGH YOUR WALLET, AND NOW IT SAYS SO.
///
/// <para>Owner's ruling on the shaft gate: the panel keeps its unprompted read — <i>that</i> is its
/// character, and a TRY verb in front of it would make the building polite. What was missing was the
/// TELLING. So a read raises a story card in the house idiom (#528), and per #736's law the outcome the
/// player acts on lives ON that card rather than only in a pulse or a row behind its backdrop.</para>
///
/// <para><b>Why these are source-shape guards</b>, like <see cref="TheTryOutcomeIsReadableTests"/> and
/// <see cref="TheLiftRefusalIsReadableTests"/> before them. Owner: <i>"note that seeing the text in DOM does
/// not mean user can see it on the screen."</i> What decides readability is PLACEMENT — the modal's own
/// subtree is the one layer a backdrop cannot blur — and the wiring being audited is a partial class behind
/// a razor page.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ThePanelReadIsToldAsACardTests
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
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static string Between(string text, string from, string to)
    {
        int start = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{from}' is no longer in the file this guard reads.");
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"'{to}' no longer follows '{from}' — this guard needs re-reading.");
        return text[start..end];
    }

    /// <summary>The refusal half of <c>PressLiftButton</c>: from the method's opening to the line that closes
    /// the panel, which is where the success path begins.</summary>
    private static string RefusalPath() => Between(
        Pages("Map.Surface.Hive.cs"), "private void PressLiftButton(", "_showLiftPanel = false");

    /// <summary>
    /// The panel reads the wallet through the MATRIX and nothing else. One source, and the second set of
    /// sentences the panel used to keep is gone from the client entirely.
    ///
    /// <para><b>Proven RED</b> by putting the old call back
    /// (<c>UndergroundComplex.WrongCardLine(-ex.Floor, HeldAuthorities())</c>):</para>
    /// <code>
    /// the lift panel's refusal never asks UndergroundComplex.TheGateReads — it is answering out of a
    /// second set of sentences again, and the sharpened refusals (#679/#683) go back to having no reader
    /// at all (#684).
    /// </code>
    /// </summary>
    [Fact]
    public void TheRefusalComesOutOfTheMatrixAndTheOldSecondSourceIsGone()
    {
        string refusal = RefusalPath();

        Assert.True(refusal.Contains("UndergroundComplex.TheGateReads(", StringComparison.Ordinal),
            "the lift panel's refusal never asks UndergroundComplex.TheGateReads — it is answering out of " +
            "a second set of sentences again, and the sharpened refusals (#679/#683) go back to having no " +
            "reader at all (#684).");

        // The whole satchel goes in, not a list derived from it: a second spelling of "what is in the
        // wallet" is the thing that drifts from what the player can open and look at (#752's own rule).
        Assert.True(refusal.Contains("_satchel", StringComparison.Ordinal),
            "the panel's read is not handed the satchel — it is judging a copy (#684).");

        foreach (string file in Directory.EnumerateFiles(
                     Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*.*", SearchOption.AllDirectories))
        {
            char s = Path.DirectorySeparatorChar;
            if (Path.GetExtension(file) is not (".cs" or ".razor")
                || file.Contains($"{s}obj{s}", StringComparison.Ordinal)
                || file.Contains($"{s}bin{s}", StringComparison.Ordinal))
            {
                continue;
            }

            // The identifier, not the word — the comments in this lane say WrongCardLine out loud, on
            // purpose, because a deletion nobody can find the name of is a deletion nobody can review.
            Assert.True(
                !File.ReadAllText(file).Contains("UndergroundComplex.WrongCardLine(", StringComparison.Ordinal),
                $"{Path.GetFileName(file)} still calls the panel's deleted second source (#684).");
        }
    }

    /// <summary>
    /// …and the read is RAISED, carrying the matrix's own line into the card's subtree.
    ///
    /// <para><b>Proven RED</b> by deleting the <c>_viewObject</c> assignment from the refusal path — the
    /// shipped behaviour, where the sharpest sentence in the game answered into a row under whatever the
    /// player was already looking at:</para>
    /// <code>
    /// the panel's read is never raised as a card — the outcome exists and is not TOLD, which is the whole
    /// of #684's ruling (art + prose in the house idiom, carrying the matrix's line).
    /// </code>
    /// </summary>
    [Fact]
    public void ARefusedReadRaisesTheStoryCardCarryingTheMatrixsOwnLine()
    {
        string refusal = RefusalPath();

        int raised = refusal.IndexOf("_viewObject = new DeckPlan.ConsoleSpot(", StringComparison.Ordinal);
        Assert.True(raised >= 0,
            "the panel's read is never raised as a card — the outcome exists and is not TOLD, which is the " +
            "whole of #684's ruling (art + prose in the house idiom, carrying the matrix's line).");

        // The card's three slots are the read's own: its title, its face, and — the one that matters for
        // #736 — the matrix's line VERBATIM as the caption. A summary here would be a second answer.
        string card = refusal[raised..];
        foreach (string slot in new[] { "read.Label", "read.ArtUrl", "read.Line" })
        {
            Assert.True(card.Contains(slot, StringComparison.Ordinal),
                $"the raised card does not carry {slot} — the story card and the gate's read have drifted " +
                "apart (#684).");
        }

        // …and the panel's own row keeps the line too (#686 is not repealed): the card is dismissible, and
        // what is behind it must still answer.
        Assert.True(refusal.Contains("_liftOutcome = read.Line", StringComparison.Ordinal),
            "the panel row no longer says the read — closing the card would leave the gate silent (#686).");

        // One event, one memory, one latch. Two would eventually show the card without the book keeping it
        // (#751's rule, learned on the hall cards).
        Assert.True(refusal.Contains("ex.HiveShaftsRefused.Add(", StringComparison.Ordinal),
            "the raise is not fenced by the shaft's own latch — a full-screen card on every press (#684).");
    }

    /// <summary>
    /// #736's LAW, asserted as PLACEMENT: the line the player acts on is rendered inside the card's own
    /// modal subtree, not merely somewhere in the DOM.
    ///
    /// <para><b>Proven RED</b> by moving the caption out of <c>&lt;div class="view-object"&gt;</c> and
    /// rendering it as a sibling of the backdrop:</para>
    /// <code>
    /// the story card's caption is rendered outside &lt;div class="view-object"&gt; — a line under the
    /// backdrop's blur is in the DOM and not on the screen (#736/#680).
    /// </code>
    /// </summary>
    [Fact]
    public void TheCardsOwnTextIsRenderedInsideTheCardsSubtree()
    {
        string razor = Pages("Map.razor");
        string block = Between(razor, "@if (_viewObject is { } vo)", "THE CAPTAIN'S SELFIE");

        int backdrop = block.IndexOf("class=\"view-object-backdrop\"", StringComparison.Ordinal);
        int modal = block.IndexOf("class=\"view-object\"", StringComparison.Ordinal);
        int caption = block.IndexOf("view-object-caption", StringComparison.Ordinal);

        // #997 wave 3 · THE CARD'S END, read off its closing tag rather than off its BUTTON. This guard used
        // to bracket the caption between the card's opening class and `view-object-close`, which was the same
        // line as long as the foot was the last thing typed inside the card. The foot is the shell's now and
        // its class is named on the card's OPENING tag (DismissClass="view-object-close"), so that anchor
        // moved to the top of the block and the sandwich inverted. The question this guard is really asking —
        // is the caption inside the card's own subtree, or under the backdrop's blur — is unchanged, and the
        // card's closing tag answers it without depending on which child happens to be last.
        int closes = block.IndexOf("</OverlayShell>", StringComparison.Ordinal);

        Assert.True(backdrop >= 0 && modal > backdrop,
            "the view-object card is no longer a modal inside a backdrop — this guard needs re-reading.");
        Assert.True(closes > modal,
            "the view-object card is no longer drawn through OverlayShell — this guard needs re-reading " +
            "(#997 wave 3 put every .view-object card in the client on the shell).");
        Assert.True(caption > modal && caption < closes,
            "the story card's caption is rendered outside the view-object card — a line under the " +
            "backdrop's blur is in the DOM and not on the screen (#736/#680).");

        // The caption slot is the card's, and it is the ConsoleSpot's own Caption — the field the raise
        // fills with the matrix's line. A hard-coded string here would make the placement assertion true
        // about text nobody computed (the house's fifth bug class).
        Assert.True(block.Contains("@cap", StringComparison.Ordinal)
            && block.Contains("vo.Caption is { Length: > 0 } cap", StringComparison.Ordinal),
            "the card's caption no longer renders the raised ConsoleSpot's own Caption (#684/#736).");
    }

    /// <summary>
    /// The other end of the same read: a gate that AGREES raises the card too, and it does it at the
    /// ARRIVAL — where the doors are open — rather than on the frame the panel closes and the floor is
    /// rebuilt, which is #689's law and the reason the beat lives there at all.
    ///
    /// <para>#693 rewrote that arrival: five hand-ordered blocks became one loop over
    /// <c>ArrivalSayings</c> and a <c>switch</c> that hangs the cards, the nerve and the flags off the beat.
    /// So the assertion is scoped to the <b>gate-accepted arm</b> and not to the method, because the arrival
    /// now raises several unrelated cards (the vacuum card among them) and a method-wide search would go
    /// green on any one of them.</para>
    /// </summary>
    [Fact]
    public void AnAcceptedReadRaisesItsCardWhenTheDoorsOPEN()
    {
        string press = Between(
            Pages("Map.Surface.Hive.cs"), "private void PressLiftButton(", "private void CloseLiftPanel(");
        Assert.True(!press.Contains("TheGateAccepted", StringComparison.Ordinal),
            "the accepted card is raised on the frame the panel closes and the floor is torn down — the " +
            "exact place the owner never saw the beat (#689).");

        string ride = Between(
            Pages("Map.Surface.Hive.cs"),
            "private void RideTheLiftTo(",
            "private (double X, double Y) SecretLabHeadSpot(");

        int arm = ride.IndexOf(
            "case UndergroundComplex.ArrivalBeat.CardAccepted", StringComparison.Ordinal);
        Assert.True(arm >= 0,
            "the arrival has no gate-accepted arm — #693's composition has moved and this guard is blind.");

        // The arm ends at its own break; everything after that belongs to some other beat.
        int ends = ride.IndexOf("break;", arm, StringComparison.Ordinal);
        Assert.True(ends > arm, "the gate-accepted arm no longer ends in a break; this guard needs re-reading.");
        string accepted = ride[arm..ends];

        Assert.True(accepted.Contains("UndergroundComplex.TheGateAccepted(", StringComparison.Ordinal),
            "the gate-accepted beat never composes its card — the gate's finest hour is a pulse line again, " +
            "and #693's law owns that slot (#684/#689).");
        Assert.True(accepted.Contains("_viewObject = new DeckPlan.ConsoleSpot(", StringComparison.Ordinal),
            "TheGateAccepted is computed inside the beat and never raised (#684).");

        // The sweep can find a raise it is looking for elsewhere in the same arrival, so the scoping above
        // is a real narrowing rather than a search that would fail anywhere (the house's fifth bug class).
        Assert.Contains("_viewObject = new DeckPlan.ConsoleSpot(", ride, StringComparison.Ordinal);
    }

    /// <summary>
    /// #684's small leak, guarded: the far-site lead a bottom-band Key points at is NAMED before the pocket
    /// is asked and ANNOUNCED only after the room has actually been emptied.
    ///
    /// <para>#678's law, in the one branch of it that reached outside the pocket: a sentence may not be
    /// composed before the act it describes. A full pocket refuses the card, the room is not struck off and
    /// the same find is offered again — but the lead had already been banked and said, and news can only be
    /// heard once. The captain kept the knowledge and never carried the card.</para>
    ///
    /// <para><b>Proven RED</b> by restoring the old single call
    /// (<c>GrantLabLead(DiceRule.Seed($"lead:hive-key:…"))</c> inside the mint):</para>
    /// <code>
    /// the Key mint calls GrantLabLead, which BANKS and SAYS the lead — and it runs before
    /// WhatGoesInThePocket can refuse the card. The captain keeps the lead without ever carrying the
    /// card (#684/#678).
    /// </code>
    /// </summary>
    [Fact]
    public void TheFarSiteLeadIsNotSpentUntilThePocketHasTakenTheCard()
    {
        string haul = Between(
            Pages("Map.Surface.Hive.cs"), "private void HiveHaulInteract(", "\n    /// <summary>#585 - A door");

        int mint = haul.IndexOf("NameAMoonWorthLookingAt(", StringComparison.Ordinal);
        int asked = haul.IndexOf("UndergroundComplex.WhatGoesInThePocket(", StringComparison.Ordinal);
        int refused = haul.IndexOf("if (!pick.RoomEmptied)", StringComparison.Ordinal);

        Assert.True(mint >= 0,
            "the Key mint no longer NAMES the far site without granting it — it is back to a call with a " +
            "side effect in the middle of an arithmetic (#684).");
        Assert.True(asked > mint, "the pocket is asked before the card is minted; this guard needs re-reading.");
        Assert.True(refused > asked, "the refusal branch has moved; this guard needs re-reading.");

        // ── #615 · THE SAYING LEFT THIS METHOD, AND THAT IS THE LAW HOLDING HARDER ──────────────────────
        //
        // A find is a decision now, so the whole pickup — the strike-off, the credits, the book and this
        // announcement — moved behind KEEP into TheFindGoesInThePocket. The law is unchanged and is
        // asserted in the two halves it now has: the search verb may not say the news AT ALL, and the
        // pickup may only say it after the room has actually been emptied.
        Assert.True(!haul.Contains("AnnounceLabLead(", StringComparison.Ordinal),
            "the search verb announces the lead itself — it can now do so on a find the captain has not " +
            "even been asked about, which is #684's leak with a card in front of it.");

        string keepOrLeave = Pages("Map.Surface.KeepOrLeave.cs");
        int pickupAt = keepOrLeave.IndexOf(
            "private void TheFindGoesInThePocket(", StringComparison.Ordinal);
        Assert.True(pickupAt >= 0, "the pickup has moved out of Map.Surface.KeepOrLeave.cs; re-read this guard.");
        string pickup = keepOrLeave[pickupAt..];   // it is the last method in the file

        int struck = pickup.IndexOf("TheRoomHasBeenGoneThrough(", StringComparison.Ordinal);
        int said = pickup.IndexOf("AnnounceLabLead(", StringComparison.Ordinal);

        Assert.True(struck >= 0, "the pickup no longer strikes the room off; this guard needs re-reading.");
        Assert.True(said > struck,
            "the lead is announced before the room has been emptied — a captain with no room left hears " +
            "the news and never carries the card (#684/#678).");

        // …and the side-effecting call is not in the mint at all. Order alone would not catch a second,
        // earlier grant slipped in beside the naming.
        string beforeThePocket = haul[..asked];
        Assert.True(!beforeThePocket.Contains("GrantLabLead(", StringComparison.Ordinal),
            "the Key mint calls GrantLabLead, which BANKS and SAYS the lead — and it runs before " +
            "WhatGoesInThePocket can refuse the card. The captain keeps the lead without ever carrying " +
            "the card (#684/#678).");

        // The sweep can find the thing it is looking for elsewhere on the pickup path, so the absence above
        // is a finding rather than a broken search (the house's fifth bug class).
        Assert.True(pickup.Contains("GrantLabLead(", StringComparison.Ordinal),
            "GrantLabLead has left the pickup entirely — this guard is now blind.");
    }
}
