using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #836 · THE FAN IS UP WHILE HE CROSSES THE FLOOR, AND THE CARD READS WHAT WAS IN THE HAND.
///
/// <para>Owner, evening playtest 2026-08-11: <i>"I think I should be able to pick the badge I show the
/// guard... like Fletch"</i> — and the ruling that decides where it may live: the 2026-08-08 call stands, so
/// there is no TRY verb, the read stays automatic, and the choice happens BEFORE arrival, inside #833's
/// approach.</para>
///
/// <para><b>Why source-shape.</b> This project has no component renderer: the client tests read the shipped
/// source, the way <c>TheWalletFansAtTheDoorTests</c> and <c>TheHailIsHostedByTheCardItArrivesOnTests</c> do.
/// What is under test here is not a value Core returns — that half is <c>TheFletchWalletTests</c> — it is
/// WHICH beat opens the fan, WHICH paper the read is handed, and which region of a 6,000-line razor the
/// dialog lives in. Every one of those failures is a shape.</para>
///
/// <para>Each guard below was watched go RED against a deliberately broken edit; the exact revert is in each
/// one's remarks.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheWalletFansWhileHeWalksOverTests
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

    /// <summary>One method's body, cut at the next member declaration — the idiom every guard on this ground
    /// already uses, so a body read here is a body read there.</summary>
    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        int end = src.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        return src[at..(end > at ? end : src.Length)];
    }

    /// <summary>The chooser's own subtree of Map.razor, cut from its opening guard to the view-object block
    /// that follows it — so every assertion below is about THIS dialog and not about a line that renders
    /// somewhere else in the file.</summary>
    private static string FanBlock()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (WalletFanIsUp", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer opens a wallet fan this guard can find.");
        int end = razor.IndexOf("@if (_viewObject is { } vo)", start, StringComparison.Ordinal);
        Assert.True(end > start, "the wallet fan's block no longer ends where this guard expects.");
        return razor[start..end];
    }

    /// <summary>
    /// THE FAN OPENS ON THE HAIL — the approach beat and nowhere else, which is the whole of how #836 keeps
    /// the no-TRY ruling. A chooser raised at the read would BE a try verb with a different name.
    /// </summary>
    /// <remarks>RED when <c>FanTheWallet();</c> is taken out of <c>TheHail</c>: "the hail no longer fans the
    /// wallet".</remarks>
    [Fact]
    public void TheHailIsWhatOpensTheFan()
    {
        string hail = Method("Map.Patrol.Challenge.cs", "private void TheHail(Guard g)");
        Assert.Contains("FanTheWallet();", hail, StringComparison.Ordinal);

        // …and the read does not open one. The card is the answer, not a second question.
        string read = Method("Map.Patrol.Challenge.cs", "private void TheRoundStopsAtYou(");
        Assert.DoesNotContain("FanTheWallet(", read, StringComparison.Ordinal);
        Assert.Contains("_walletFanOpen = false;", read, StringComparison.Ordinal);
    }

    /// <summary>
    /// WHETHER THERE IS A CHOICE AT ALL IS CORE'S ANSWER, ASKED ONCE. With one paper in the wallet nothing
    /// happens that did not happen before this feature existed — and the dialog and the read must not be able
    /// to disagree about whether the captain had a decision to make.
    /// </summary>
    /// <remarks>RED when <c>FanTheWallet</c> counts the satchel itself instead of asking
    /// <c>WalletChoice.Fans</c>.</remarks>
    [Fact]
    public void OnePaperIsExactlyTodayAndCoreIsWhatSaysSo()
    {
        string fan = Method("Map.Patrol.Challenge.cs", "private void FanTheWallet()");
        Assert.Contains("WalletChoice.Fans(", fan, StringComparison.Ordinal);
        Assert.Contains("WalletChoice.DefaultFor(", fan, StringComparison.Ordinal);

        // The razor asks the same one question rather than a second copy of it.
        Assert.Contains("Count: > 1", FanBlock(), StringComparison.Ordinal);
    }

    /// <summary>
    /// THE READ IS HANDED ONE PAPER, AND IT IS THE ONE THAT WAS CHOSEN. The load-bearing wiring of the whole
    /// issue: the satchel may not reach <c>TheGuardReads</c> at all, because a read that could see the wallet
    /// is a read that can quietly optimise.
    /// </summary>
    /// <remarks>RED when <c>TheRoundStopsAtYou</c> passes <c>_satchel</c> (the shipped behaviour) or when
    /// <c>ThePaperHandedOver</c> ignores <c>_paperInHand</c> and always returns the default: the captain's
    /// choice is thrown away and the guard reads whatever the habit was.</remarks>
    [Fact]
    public void TheGuardIsHandedTheChosenPaperAndNeverTheWallet()
    {
        string read = Method("Map.Patrol.Challenge.cs", "private void TheRoundStopsAtYou(");
        Assert.Contains("ThePaperHandedOver(", read, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.TheGuardReads(bodyId, g.Plate, handed)", read, StringComparison.Ordinal);
        Assert.DoesNotContain("TheGuardReads(bodyId, g.Plate, _satchel", read, StringComparison.Ordinal);

        string handed = Method("Map.Patrol.Challenge.cs", "private Satchel.Item? ThePaperHandedOver(");
        Assert.Contains("_paperInHand", handed, StringComparison.Ordinal);
        Assert.Contains("WalletChoice.StillHeld(", handed, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE ROUND LOG REMEMBERS THE NAME — on BOTH arms. The clean read used to file nothing at all, and it is
    /// exactly the read whose line the next chooser row is built out of: a book that only kept the bad nights
    /// could never say <i>worked here, twice</i>.
    /// </summary>
    /// <remarks>RED when <c>FileTheNameYouGave</c> is moved below the <c>if (read.Satisfied)</c> early
    /// return: the satisfied arm files nothing and the wallet forgets every paper that ever worked.</remarks>
    [Fact]
    public void EveryReadIsFiledIncludingTheOneThatWentWell()
    {
        string read = Method("Map.Patrol.Challenge.cs", "private void TheRoundStopsAtYou(");

        int filed = read.IndexOf("FileTheNameYouGave(", StringComparison.Ordinal);
        int satisfied = read.IndexOf("if (read.Satisfied)", StringComparison.Ordinal);
        Assert.True(filed >= 0, "nothing files which name was given at the read.");
        Assert.True(satisfied > filed,
            "the name is filed after the satisfied arm returns, so a pass that worked is never written down.");

        // …and the line and the row are composed off the SAME outcome the card was.
        string file = Method("Map.Patrol.Challenge.cs", "private void FileTheNameYouGave(");
        Assert.Contains("WalletChoice.WhatHappens(", file, StringComparison.Ordinal);
        Assert.Contains("WalletChoice.ShownNote(", file, StringComparison.Ordinal);
        Assert.Contains("WalletChoice.Remember(", file, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE ROW IS THE CAPTAIN'S OWN KNOWLEDGE AND NOTHING ELSE: the glyph, the name on it, what it claims,
    /// and what the book filed about it here. Nothing in the dialog asks the guard's ladder how it would go —
    /// an omniscient hint is the sim leaking through the fiction.
    /// </summary>
    /// <remarks>RED when the row is given a verdict — e.g. a class or caption derived from
    /// <c>PatrolBeat.TheGuardReads</c> or <c>WalletChoice.WhatHappens</c> — which is precisely the oracle the
    /// owner ruled out.</remarks>
    [Fact]
    public void TheRowShowsPrintedFieldsAndTheBookAndNoOracle()
    {
        string block = FanBlock();

        foreach (string must in new[]
                 {
                     "WalletChoice.GlyphOf(", "WalletChoice.NameOn(", "WalletChoice.Claims(",
                     "WalletChoice.HistoryLine(", "WalletChoice.ChooserLabel", "WalletChoice.ChooserHint",
                 })
        {
            Assert.Contains(must, block, StringComparison.Ordinal);
        }

        foreach (string oracle in new[] { "TheGuardReads", "WhatHappens", "Satisfied", "%" })
        {
            Assert.DoesNotContain(oracle, block, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// THE CANCEL KEY REACHES IT, AND MEANS KEEP THE ONE YOU HAVE. #351's family law — a dialog that takes
    /// the screen and ignores Esc is the complaint that named that issue — with #784's discipline on top: the
    /// key may shut the question, never answer it. It is deliberately absent from the Enter chain, which
    /// presses only the primary action of cards that ask nothing.
    ///
    /// <para>And the rung is gated on the SAME property the dialog is drawn from, so Esc can never swallow a
    /// keystroke for a fan that is not on the screen.</para>
    /// </summary>
    /// <remarks>RED when the Esc rung is removed, and RED when it is gated on <c>_walletFanOpen</c> while the
    /// dialog is gated on the fan's own count.</remarks>
    [Fact]
    public void EscapeShutsTheFanAndEnterDoesNotAnswerIt()
    {
        string chain = Method("Map.Sim.Cancel.cs", "private bool TryDismissTopOverlay()");
        Assert.Contains(
            "if (WalletFanIsUp) { CloseTheWalletFan(); return true; }", chain, StringComparison.Ordinal);

        // The keyboard YES may not pick a name for the captain.
        string confirm = Method("Map.Sim.Cancel.cs", "private bool TryConfirmTopOverlay()");
        Assert.DoesNotContain("WalletFan", confirm, StringComparison.Ordinal);
        Assert.DoesNotContain("_paperInHand", confirm, StringComparison.Ordinal);
    }

    /// <summary>
    /// IT IS A CHALLENGE-TIME DIALOG AND NOT A SATCHEL PAGE. The fan opens over the walk-up, in its own
    /// subtree, above the view-object card it will be replaced by — so it never touches the pocket's own
    /// dialog, whose rows belong to another lane entirely.
    /// </summary>
    /// <remarks>RED if the block is moved inside <c>@if (_showSatchel)</c>: the cut this guard makes would
    /// land inside the pocket and the satchel's Close button would appear in the fan's own subtree.</remarks>
    [Fact]
    public void TheFanIsItsOwnDialogAndNotAPageOfThePocket()
    {
        string razor = Pages("Map.razor");
        int satchel = razor.IndexOf("@if (_showSatchel)", StringComparison.Ordinal);
        int fan = razor.IndexOf("@if (WalletFanIsUp", StringComparison.Ordinal);
        int viewObject = razor.IndexOf("@if (_viewObject is { } vo)", StringComparison.Ordinal);

        Assert.True(satchel >= 0 && fan > satchel && viewObject > fan,
            "the wallet fan is no longer its own block between the pocket and the card.");
        Assert.DoesNotContain("_satchelPage", FanBlock(), StringComparison.Ordinal);
        Assert.DoesNotContain("SatchelPage", FanBlock(), StringComparison.Ordinal);
    }
}
