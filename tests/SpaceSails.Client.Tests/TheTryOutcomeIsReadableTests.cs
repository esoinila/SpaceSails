using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #680 · UNREADABLE TEXT AS RESULT OF ITEM USE TRY. Owner, live, in caps: <i>"pressing Try IT on item
/// produces a text that is IMPOSSIBLE to read"</i> — <i>"it is behind the blurring effect... so we don't
/// tell the story."</i>
///
/// <para>The sim obeyed #603's law to the letter — every refusal named its reason — and the z-order defeated
/// it: a failed try keeps the satchel open (correct, #614: compare three cards without reopening your
/// pockets) while the outcome line went to the pulse HUD, which renders UNDER the modal backdrop and its
/// blur. The exact lines the law exists for were the ones nobody could read.</para>
///
/// <para><b>Why these are source-shape guards.</b> Owner: <i>"note that seeing the text in DOM does not mean
/// user can see it on the screen."</i> The broken build had the line in the DOM the whole time — a guard
/// that asserts presence would have been green on the bug, a guard that asserts nothing. What decides
/// readability here is PLACEMENT: the modal's own subtree is the one layer above the backdrop, so the
/// outcome must be rendered inside the satchel block itself, and the failure path must never route its line
/// to the pulse instead.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheTryOutcomeIsReadableTests
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

    /// <summary>The satchel block of Map.razor — from the modal's opening guard to the view-object block
    /// that follows it. Cut so the assertion is about THIS dialog's subtree, not the file at large.</summary>
    private static string SatchelBlock()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_showSatchel)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the satchel block this guard knows how to find.");
        int end = razor.IndexOf("_viewObject is { }", start, StringComparison.Ordinal);
        Assert.True(end > start, "Map.razor's satchel block no longer ends where this guard expects.");
        return razor[start..end];
    }

    [Fact]
    public void TheOutcomeIsSaidInsideTheDialogItself()
    {
        // The one layer the backdrop cannot blur is the dialog's own subtree. The outcome of a try must be
        // rendered there — a line the captain reads exactly where their eyes already are.
        string satchel = SatchelBlock();
        Assert.True(satchel.Contains("_satchelOutcome", StringComparison.Ordinal),
            "the satchel dialog never renders _satchelOutcome — a failed try's answer is said somewhere " +
            "the player is not looking (#680).");
        Assert.True(satchel.Contains("satchel-outcome", StringComparison.Ordinal),
            "the satchel dialog has no styled outcome row (#680).");
    }

    [Fact]
    public void AFailedTryNeverRoutesItsLineToThePulseHud()
    {
        // The broken shape, stated as its own negation: the try pulsed the line FIRST and only then asked
        // whether the offer worked — so refusals, which keep the modal open, played behind the frosted
        // glass. The failure path must store the line for the dialog before it returns.
        //
        // #697 moved that ending out of TryItem into the one resolution BOTH presses share — the single card
        // and the fanned wallet — which is where this law now has to hold. That is a strengthening, not a
        // relocation: there is exactly one place left that can get it wrong.
        string surface = Pages("Map.Surface.Darkroom.cs");
        int at = surface.IndexOf("private void TheOfferIsAnswered(", StringComparison.Ordinal);
        Assert.True(at >= 0,
            "Map.Surface.cs no longer has TheOfferIsAnswered where this guard can read it — the offer's " +
            "ending has moved and #680's law has to move with it.");
        int end = surface.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        string answered = surface[at..(end > at ? end : surface.Length)];

        int stored = answered.IndexOf("_satchelOutcome = outcome.Line", StringComparison.Ordinal);
        int worked = answered.IndexOf("outcome.Worked", StringComparison.Ordinal);
        Assert.True(stored >= 0,
            "the offer's ending never stores the outcome for the dialog — the refusal goes back behind the " +
            "blur (#680).");
        Assert.True(worked >= 0,
            "the offer's ending no longer consults outcome.Worked — this guard needs re-reading.");

        int pulsed = answered.IndexOf("ShowPulseMessage(outcome.Line)", StringComparison.Ordinal);
        Assert.True(pulsed < 0 || pulsed > worked,
            "the offer pulses its outcome before it knows whether the modal will stay open — the exact " +
            "broken shape #680 was filed on: a refusal keeps the satchel up, and a line pulsed under the " +
            "backdrop is in the DOM and not on the screen.");

        // And every press that can produce an outcome ends here. A press that kept its own ending would be
        // a second place for #680 to come back (#697).
        foreach (string press in new[] { "private void TryItem(", "private void TryTheWholeWallet()" })
        {
            int from = surface.IndexOf(press, StringComparison.Ordinal);
            Assert.True(from >= 0, $"Map.Surface.cs no longer has `{press}` where this guard can read it.");
            string body = surface[from..surface.IndexOf("\n    private ", from + 1, StringComparison.Ordinal)];
            Assert.True(body.Contains("TheOfferIsAnswered(", StringComparison.Ordinal),
                $"`{press}` answers an offer without going through the one ending that keeps the line " +
                "inside the dialog (#680/#697).");
        }
    }

    [Fact]
    public void TheOutcomeRowIsStyledLikeTheDialogItSitsIn()
    {
        string css = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));
        Assert.True(css.Contains(".satchel-outcome", StringComparison.Ordinal),
            "the satchel outcome row has no style of its own — an unstyled answer in a styled dialog is a " +
            "bug report waiting to be filed (#680).");
    }
}
