using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #697 · FAN THE WALLET AT THE READER. Owner: <i>"Let's also add option to try all ID cards ... by grouping
/// them into a folder in the inventory."</i>
///
/// <para><b>Why source-shape.</b> The reasoning #680, #688 and #690 already wrote down: what is under test is
/// not a value a Core function returns — that half is <c>TheWalletIsOneThingTests</c> — it is which control
/// the razor renders, which Core call the press routes through, and which state transitions the press is
/// allowed to perform on its own. All of it was watched go RED against the build that shipped #690: the word
/// WALLET appeared nowhere in the dialog, and there was no fan.</para>
///
/// <para>The one thing these guards defend hardest is the SHARED resolution. A fan that pulsed and closed the
/// satchel by itself would be a second copy of the single try's ending, and the day one of them learns to
/// spend something the other will not — which is this repo's first named bug class, aimed at a state
/// transition rather than at a number.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheWalletFansAtTheDoorTests
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

    /// <summary>The satchel block of Map.razor — from the modal's opening guard to the view-object block that
    /// follows it, so every assertion is about THIS dialog's subtree. The cut #688 and #690 both make.</summary>
    private static string SatchelBlock()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_showSatchel)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the satchel block this guard knows how to find.");
        int end = razor.IndexOf("_viewObject is { }", start, StringComparison.Ordinal);
        Assert.True(end > start, "Map.razor's satchel block no longer ends where this guard expects.");
        return razor[start..end];
    }

    /// <summary>One method's body, cut at the next member declaration — the idiom #680's guards already use.</summary>
    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        int end = src.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        return src[at..(end > at ? end : src.Length)];
    }

    [Fact]
    public void TheCardsCollapseIntoOneFolderRow()
    {
        // The feature in one shape: the CARRIED page draws a wallet rather than four card rows, and what is
        // in the wallet is Core's own grouping of the pocket — a hand-written `Kind == Authority` filter in
        // the dialog would be a second answer to "what is in the wallet" the first time a kind moves
        // compartment (#688's CompartmentOf is the shape that would drift).
        string satchel = SatchelBlock();

        Assert.True(satchel.Contains("THE WALLET", StringComparison.Ordinal),
            "the CARRIED page draws no wallet — a captain with four authorities still reads four rows and " +
            "presses four times (#697).");
        Assert.True(satchel.Contains("_walletOpen", StringComparison.Ordinal),
            "the wallet has no open/shut state, so it is not a folder — it is a label (#697).");

        string wallet = Method("Map.Surface.Darkroom.cs", "private IReadOnlyList<Core.Satchel.Item> Wallet()");
        Assert.True(wallet.Contains("Satchel.OfKind(", StringComparison.Ordinal)
            && wallet.Contains("Kind.Authority", StringComparison.Ordinal),
            "the wallet is not Core's own grouping of the pocket — the dialog has its own idea of which " +
            "things are cards (#697/#688).");
    }

    [Fact]
    public void EveryOpenFindsTheWalletFOLDED()
    {
        // #690's ruling, applied to the folder it did not know about yet: the page a captain left the dialog
        // on last time answers a question they did not ask. Collapsed is the point of the row — a wallet that
        // reopened expanded would be four card rows with an extra line above them.
        string reset = Method("Map.Surface.Satchel.cs", "private void TheSatchelOpensOnThePocket()");
        Assert.True(reset.Contains("_walletOpen = false", StringComparison.Ordinal),
            "the open-reset leaves the wallet however the last visit left it — the collapse is the whole " +
            "row (#697/#690).");
    }

    [Fact]
    public void TheFolderRowCarriesTheOfferAndItRoutesThroughOfferWallet()
    {
        // "One press fans every card." The fold is Core's (OfferWallet, pinned in TheWalletIsOneThingTests);
        // what this guard forbids is the client growing its own — a loop over the wallet in the dialog would
        // be a second ladder, and the two would disagree the first time #683's order is refined.
        string satchel = SatchelBlock();
        Assert.True(satchel.Contains("TryTheWholeWallet", StringComparison.Ordinal),
            "the folder row offers nothing — the wallet is a label and the captain still presses four times " +
            "(#697).");

        string fan = Method("Map.Surface.Darkroom.cs", "private void TryTheWholeWallet()");
        Assert.True(fan.Contains("SatchelTry.OfferWallet(", StringComparison.Ordinal),
            "the fan does not ask Core to fold the wallet — the client is ranking refusals for itself " +
            "(#697/#683).");
        Assert.False(fan.Contains("foreach", StringComparison.Ordinal),
            "the fan walks the wallet itself, which is a second fold living in the client (#697).");

        // And it is offered exactly where an authority may be offered, asked of the same law the rows ask
        // (SatchelTry.CanOffer) rather than of a list of targets written out here (#688).
        string where = Method("Map.Surface.Darkroom.cs",
            "private (SatchelTry.Target Target, string? Context, string Label)? WalletTarget()");
        Assert.True(where.Contains("SatchelTry.CanOffer(", StringComparison.Ordinal),
            "the fan decides for itself which targets read authorities instead of asking Core (#697/#688).");
    }

    [Fact]
    public void TheFanEndsExACTLYWhereASingleTryEnds()
    {
        // THE ANTI-DOUBLE-EFFECT LAW, and the reason the two presses share one ending. A success has to do
        // what a single successful try does — no more, and not a hand-written copy of it that drifts. So the
        // fan owns no state transition of its own: it does not pulse, it does not close the satchel, and it
        // does not spend anything out of the pocket.
        string fan = Method("Map.Surface.Darkroom.cs", "private void TryTheWholeWallet()");

        foreach (string forbidden in new[]
                 {
                     "ShowPulseMessage(", "CloseSatchel()", "Satchel.Remove(", "_viewObject =",
                     "RequestVaultSave()",
                 })
        {
            Assert.False(fan.Contains(forbidden, StringComparison.Ordinal),
                $"TryTheWholeWallet performs `{forbidden}` on its own — a second copy of the single try's " +
                "ending, and the day one of them learns to spend something the other will not (#697).");
        }

        Assert.True(fan.Contains("TheOfferIsAnswered(", StringComparison.Ordinal),
            "the fan does not end through the same resolution the single try ends through (#697).");

        string one = Method("Map.Surface.Darkroom.cs", "private void TryItem(");
        Assert.True(one.Contains("TheOfferIsAnswered(", StringComparison.Ordinal),
            "the single try no longer shares its ending with the fan — there are two endings again (#697).");

        // #680's law survives the new press: a refusal is said INSIDE the dialog, which is the one layer the
        // backdrop cannot blur.
        string answered = Method("Map.Surface.Darkroom.cs", "private void TheOfferIsAnswered(");
        Assert.True(answered.Contains("_satchelOutcome", StringComparison.Ordinal),
            "a refused offer no longer stores its line for the dialog — it goes behind the blur (#680).");
    }

    [Fact]
    public void ThePerCardROWSurvivesTheFolder()
    {
        // The owner loves the card faces (#528/#695) and the deliberate single try is the captain's to make.
        // Expanding the wallet must show exactly the rows that were there before — each with its own try and
        // its own lens — and the toggle must be its own control, because #614's ruling applies twice over
        // here: opening a folder must never be one mis-click from offering everything in it to a bulkhead.
        string satchel = SatchelBlock();

        foreach (string kept in new[] { "TryItem(item)", "OpenItemCard(item)", "LeaveItem(item)", "🔍" })
        {
            Assert.True(satchel.Contains(kept, StringComparison.Ordinal),
                $"the per-card row lost `{kept}` to the folder (#697/#614).");
        }

        Assert.True(satchel.Contains("satchel-folder", StringComparison.Ordinal),
            "the folder toggle has no class of its own (#697).");
        Assert.True(satchel.Contains("satchel-fan", StringComparison.Ordinal),
            "the fan has no control of its own — expanding the wallet and offering it are one button (#697).");

        string css = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));
        foreach (string rule in new[] { ".satchel-folder", ".satchel-fan", ".satchel-wallet-cards" })
        {
            Assert.True(css.Contains(rule, StringComparison.Ordinal),
                $"{rule} has no style of its own — the folder is riding on the item list's rules (#697).");
        }
    }
}
