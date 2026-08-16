using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #688 · THE SATCHEL FEEL PASS, from the live run. Owner, verbatim:
///
/// <list type="bullet">
/// <item><i>"If I press I when inventory is open, let's close it then."</i></item>
/// <item><i>"Let's make a bigger story point about finding any kind of key or keycard and only suggest those
/// at doors. Or tools, but not just like some papers."</i></item>
/// <item><i>"I find the good keycard but my pockets are full and I can not pocket it."</i></item>
/// <item><i>"The keycard story is already big, but no way to drop stuff."</i></item>
/// </list>
///
/// <para><b>Why source-shape.</b> The same reasoning #680 wrote down: what is under test here is not a value a
/// Core function returns, it is which control the razor renders and which branch a keybind takes. All four of
/// these were watched go RED against the build that shipped this morning — the I key only ever opened, the
/// door offered every row it had, the subtitle subtracted one count from one capacity, and there was no leave
/// control in the file at all.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheSatchelFeelPassTests
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

    /// <summary>#870 · The deck page is seven partials by subject now, so "the deck" a guard reads over is
    /// all of them — exactly the text it read out of one file before the split.</summary>
    private static string Deck() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Deck*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>The satchel block of Map.razor — from the modal's opening guard to the view-object block that
    /// follows it, so every assertion is about THIS dialog's subtree and not the file at large.</summary>
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
    public void PressingIWithTheSatchelOpenCLOSESIt()
    {
        // Owner: "If I press I when inventory is open, let's close it then." One line of feel, and the kind
        // that is invisible until you are standing in a corridor with a pack coming and the pocket you opened
        // by reflex will not go away by the same reflex.
        string key = Deck();
        int at = key.IndexOf("case \"i\" or \"I\":", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.Deck.cs no longer has the I keybind where this guard can read it.");
        string branch = key[at..key.IndexOf("case \"h\" or \"H\":", at, StringComparison.Ordinal)];

        Assert.True(branch.Contains("ToggleSatchel()", StringComparison.Ordinal),
            "the I key does not toggle — it can only ever open the satchel, so pressing it again does " +
            "nothing and the captain is left hunting for the close button (#688).");

        string toggle = Method("Map.Surface.Satchel.cs", "private void ToggleSatchel()");
        Assert.True(toggle.Contains("_showSatchel", StringComparison.Ordinal),
            "ToggleSatchel never looks at whether the satchel is open — it cannot be a toggle.");
        Assert.True(toggle.Contains("CloseSatchel()", StringComparison.Ordinal),
            "ToggleSatchel never closes anything (#688).");
    }

    [Fact]
    public void AtADoorTheSatchelOffersKEYSAndNothingElse()
    {
        // Owner: "only suggest those at doors. Or tools, but not just like some papers." The decision is
        // Core's (SatchelTry.CanOffer, pinned in WhatYouLeaveIsStillThereTests); what this guard forbids is
        // the client routing around it, which is exactly what it did — TargetFor handed back the open-at
        // target for every row in the pocket without asking anybody.
        string targetFor = Method("Map.Surface.Darkroom.cs",
            "private (SatchelTry.Target Target, string? Context, string Label)? TargetFor(");

        int opened = targetFor.IndexOf("_satchelTarget is { } at", StringComparison.Ordinal);
        Assert.True(opened >= 0, "TargetFor no longer reads _satchelTarget — this guard needs re-reading.");

        // The FIRST statement that hands the open-at target back is the one under test. Read as a whole
        // statement rather than as "the file mentions CanOffer somewhere", which a build that asked the
        // question and then ignored the answer would also satisfy.
        int returned = targetFor.IndexOf("return", opened, StringComparison.Ordinal);
        Assert.True(returned > opened, "TargetFor no longer returns the open-at target where this guard looks.");
        string statement = targetFor[returned..(targetFor.IndexOf(';', returned) + 1)];

        Assert.True(statement.Contains("SatchelTry.CanOffer(", StringComparison.Ordinal),
            "TargetFor hands back the door for every row in the pocket without asking SatchelTry.CanOffer — " +
            $"a shipping manifest carries a live `try it →` at a bulkhead (#688). It said: {statement}");
    }

    [Fact]
    public void TheSubtitleNeverCountsTheWholeSatchelAgainstOneCap()
    {
        // The satchel has three compartments now and only two of them can fill, so "N spaces left" computed
        // from one subtraction is a sentence that cannot be true — the third named bug class in a subtitle.
        string satchel = SatchelBlock();

        Assert.True(satchel.Contains("Satchel.SpaceLine(", StringComparison.Ordinal),
            "the satchel subtitle does not ask Core what room is left, so the dialog and the pocket can " +
            "disagree about the same satchel (#688).");
        Assert.False(satchel.Contains("Satchel.Capacity", StringComparison.Ordinal),
            "the subtitle still subtracts the whole satchel from one capacity — which counts the cards it " +
            "is not allowed to count and reports one figure for three compartments (#688).");
    }

    [Fact]
    public void EveryRowCanBeLEFTBehindAndTheConfirmationIsReadable()
    {
        // Owner: "no way to drop stuff." A leave verb per row — its own control beside the lens, never a
        // mode, so making room can never be a mis-click on offering something to a door.
        string satchel = SatchelBlock();
        Assert.True(satchel.Contains("LeaveItem(item)", StringComparison.Ordinal),
            "no satchel row offers to put anything down (#688).");
        Assert.True(satchel.Contains("satchel-leave", StringComparison.Ordinal),
            "the leave control has no class of its own — it is riding on the item button or the lens (#688).");

        // #696 · THE DROP MOVED HOUSE, AND EVERY LAW BELOW CAME WITH IT. Processing a document is a HOLD
        // now ("we take time to process the loot"), so LeaveItem is the press that starts a clock and
        // SetItDown is the drop the clock ends in — which is also what makes the far end of a hold the
        // effect the game already had rather than a second copy of it. These guards read the drop wherever
        // the drop lives; what they must never do is quietly stop asking.
        string leave = Method("Map.Surface.Satchel.cs", "private void SetItDown(");

        // #680/#686's law applies to this line too, and it is the easiest one in the game to get wrong: a
        // confirmation routed to the pulse HUD while the satchel is open renders under the backdrop's blur —
        // in the DOM and not on the screen. After #696 that stopped being a constant and became a QUESTION
        // (the hold shuts the pocket, and the captain may or may not have reopened it), so the drop asks one
        // method which answers it, rather than picking a destination it cannot know is right.
        Assert.True(leave.Contains("SayItWhereTheyAreLooking(", StringComparison.Ordinal),
            "the drop no longer routes its line through the one place that knows whether the dialog is up — " +
            "so it is guessing, and half the time it guesses into the blur (#680/#696).");
        Assert.False(leave.Contains("ShowPulseMessage(", StringComparison.Ordinal),
            "the drop pulses its line directly, which is the exact shape #680 was filed on the moment the " +
            "satchel happens to be open over it.");

        string says = Method("Map.Surface.Satchel.cs", "private void SayItWhereTheyAreLooking(");
        Assert.True(says.Contains("_satchelOutcome", StringComparison.Ordinal)
            && says.Contains("_showSatchel", StringComparison.Ordinal),
            "the say-it router does not consult the satchel — the line it places cannot be in the right " +
            "place except by luck (#680/#696).");

        // #688 refinement · Leaving a document files its gist to the field book, and it must file through
        // the RECORD HALF alone (#686): the SAYING is one line and the book must not read itself out loud
        // on the way in.
        Assert.True(leave.Contains("LeftBehind.GistOf(", StringComparison.Ordinal),
            "the drop never asks what the thing said before putting it down — a paper left is a paper " +
            "whose gist was thrown away with it (#688).");
        Assert.True(leave.Contains("FileNote(", StringComparison.Ordinal),
            "the drop never files the gist it composed (#688).");
        Assert.False(leave.Contains("ShowAndFile(", StringComparison.Ordinal),
            "the drop files through ShowAndFile, which pulses the note as well as filing it (#686).");

        // The drop itself still never closes the pocket. #696 shuts it on the way INTO a hold — deliberately,
        // because the vulnerability is the mechanic and a captain cannot watch a motion tracker through a
        // backdrop blur — but a thing set down with no clock in front of it leaves the pockets exactly where
        // they were, which is #614's ruling and the reason the leave verb exists at all.
        Assert.False(leave.Contains("CloseSatchel()", StringComparison.Ordinal),
            "setting a thing down closes the satchel — but you leave a thing in order to take another, and " +
            "reopening the pockets between each one is the friction #614 already ruled against.");

        string css = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));
        Assert.True(css.Contains(".satchel-leave", StringComparison.Ordinal),
            "the leave control has no style of its own (#688).");
    }

    [Fact]
    public void WhatYouPutDownIsFoundAgainByTheSearchVerb()
    {
        // The other half of "leaving never destroys": there has to be a way back. E is the verb that finds
        // things, so E finds what is at your feet before it finds what is in the walls.
        string surface = Pages("Map.Surface.Satchel.cs");
        Assert.True(surface.Contains("private bool TryPickUpWhatYouLeft(", StringComparison.Ordinal),
            "nothing in the client ever picks a left thing back up — the drop verb is a delete key (#688/#615).");

        string deck = Deck();
        int interact = deck.IndexOf("private void InteractAtConsole()", StringComparison.Ordinal);
        Assert.True(interact >= 0, "Map.Deck.cs no longer has InteractAtConsole where this guard can read it.");
        int recovers = deck.IndexOf("TryPickUpWhatYouLeft()", interact, StringComparison.Ordinal);
        int switched = deck.IndexOf("switch (_deckPlan.NearestConsole", interact, StringComparison.Ordinal);
        Assert.True(recovers > interact && recovers < switched,
            "E never asks what is lying at the captain's feet before it dispatches to a console — a thing " +
            "set down on a console spot could never be recovered (#688).");
    }
}
