using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #774 · THE DOSSIER SAID FOUR SENTENCES UNDER ITS OWN CARD — the wiring half.
///
/// <para>Filed by the owner off the #768/#773 crew's verification, and deliberately left out of that PR:
/// <c>AssembleSomebody</c> raised the field dossier's card and <i>then</i> fired two to four
/// <c>ShowAndFile</c> lines beneath it. All of them were kept; none of them was readable; the captain closed
/// the card onto silence. #768's hold is the wrong remedy and was refused as such — it releases ONE winner,
/// and a same-rank sequence's survivor would be decided by which line was appended last, the
/// ordering-as-contract bug #693 killed. The remedy is #736's law: the sentences go ON the card.</para>
///
/// <para>The composition and its reading order are Core's, and are swept there
/// (<c>TheDossierIsReadOnItsOwnCardTests</c>). What Core cannot see is the thing that actually broke — a
/// sentence sent to the wrong PLACE from a partial class inside a razor page — so these are source-shape
/// guards, the house shape of #736's <see cref="TheOutcomeIsOnThePopUpTests"/> and #768's
/// <see cref="TheArrivalHoldsItsLineForTheCardTests"/>. Owner's own words on why presence is not enough:
/// <i>"note that seeing the text in DOM does not mean user can see it on the screen."</i> Each test below
/// pins a line to a SUBTREE.</para>
///
/// <para><b>Red proof.</b> Revert <c>Map.Surface.cs</c>, <c>Map.razor</c>, <c>Map.Sim.cs</c> and
/// <c>DeckPlan.cs</c> to the shipped versions and every test here goes red, naming the site.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDossierCardCarriesItsOwnSayingsTests
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

    /// <summary>#870 · The sim page is nine partials by subject now, so "the sim" a guard reads over is all
    /// of them — exactly the text it read out of one file before the split.</summary>
    private static string Sim() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Sim*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    private static string Rendering(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Rendering", file));

    /// <summary>One method's body, from its signature to the next member at the same indent — the cut
    /// <see cref="TheOutcomeIsOnThePopUpTests"/> makes, so a body read here is a body read there.</summary>
    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        int end = src.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        return src[at..(end > at ? end : src.Length)];
    }

    /// <summary>The markup ONE pop-up owns: from its own `@if (…` to the next top-level `@if (`. Cutting the
    /// block is the whole point — a line rendered elsewhere in this 5,700-line file is a line on a different
    /// pop-up, which is the bug and not the fix.</summary>
    private static string BlockOf(string opening)
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf(opening, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Map.razor no longer has the `{opening}` block this guard knows how to find.");
        int end = razor.IndexOf("\n    @if (", start + opening.Length, StringComparison.Ordinal);
        return razor[start..(end > start ? end : razor.Length)];
    }

    // ── The issue's own case ──────────────────────────────────────────────────────────────────────────

    /// <summary>#774's acceptance: the assembly hands the card everything it has to say, as an argument of
    /// the raise itself. Searched FROM the raise, so the only occurrence that can satisfy it is one inside
    /// the card's own argument list — a site that raises the card and says its lines afterwards, which is the
    /// shipped shape and the exact bug, has nothing after the raise to find.</summary>
    [Fact]
    public void TheAssemblyComposesEverySentenceOntoTheCardItRaises()
    {
        string body = Method("Map.Surface.Hive.cs", "private void AssembleSomebody(");

        int card = body.IndexOf("_viewObject = new DeckPlan.ConsoleSpot(", StringComparison.Ordinal);
        Assert.True(card >= 0, "AssembleSomebody no longer raises the dossier card — this guard needs re-reading.");

        int composed = body.IndexOf("FieldDossier.DebriefBlock(", StringComparison.Ordinal);
        Assert.True(composed > card,
            "the assembly's sentences are not an argument of the card it raises — the person, the next of " +
            "kin, what the family knows and the in are pulsed UNDER the dossier's own backdrop, which is " +
            "the whole of #774.");

        // …and the sentences come from Core, ordered there. A client that picked its own order would be
        // the #693 contract back in a different file.
        Assert.Contains("FieldDossier.Debrief(", body, StringComparison.Ordinal);
    }

    /// <summary>NOTHING IS SAID UNDER THE CARD ANY MORE. The four <c>ShowAndFile</c> calls were the bug; a
    /// fifth pulse added back here would be it again, in the one method the issue is about.</summary>
    [Fact]
    public void TheAssemblyNeverPulsesASentenceUnderItsOwnBackdrop()
    {
        string body = Method("Map.Surface.Hive.cs", "private void AssembleSomebody(");

        Assert.DoesNotContain("ShowAndFile(", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPulseMessage(", body, StringComparison.Ordinal);
    }

    /// <summary>THE BOOK IS UNTOUCHED. #587's law — a find shown once is a find that is lost — does not care
    /// where the sentence was read: every one of them is still filed, under its own glyph, in the order it
    /// was said. A fix that moved the sentences onto the card and stopped recording them would have traded
    /// one bug for a worse one.</summary>
    [Fact]
    public void TheBookStillKeepsEverySentenceTheAssemblySays()
    {
        string body = Method("Map.Surface.Hive.cs", "private void AssembleSomebody(");

        // #741 v1 · FileNoteAbout — FileNote plus the one thing this author uniquely knows: what each
        // sentence is ABOUT (a person, an office, a door, declared in Core beside the words). The record
        // half is unchanged: same text, same glyph, same order, still every one of them.
        Assert.True(body.Contains("FileNoteAbout(said.Text, said.Glyph, said.Subjects)", StringComparison.Ordinal),
            "the assembly no longer files what it says — the dossier's four sentences are readable for as " +
            "long as the card is up and then gone forever (#587/#774).");
    }

    /// <summary>…and the moon the family's knowledge names, which is the FIFTH thing this one press can say.
    /// It arrives from inside the assembly, with the dossier card already up, so a pulse is a pulse behind a
    /// backdrop exactly like the other four.</summary>
    [Fact]
    public void TheMoonTheFamilyNamesIsSaidWhereTheCaptainIsLooking()
    {
        string body = Method("Map.Surface.Hive.cs", "private void AnnounceLabLead(");

        Assert.True(body.Contains("SayWhereTheyAreLookingAndFile(", StringComparison.Ordinal),
            "the named moon is announced without asking where the captain is looking — called from the " +
            "dossier assembly it plays under the dossier's own card (#774/#736).");
        Assert.DoesNotContain("ShowAndFile(", body, StringComparison.Ordinal);

        // The pair is the point: said where it can be read AND kept. Either half alone is a bug this house
        // has already shipped once.
        string helper = Method("Map.Surface.Satchel.cs", "private void SayWhereTheyAreLookingAndFile(");
        Assert.Contains("SayItWhereTheyAreLooking(text)", helper, StringComparison.Ordinal);
        Assert.Contains("FileNote(text, glyph)", helper, StringComparison.Ordinal);
    }

    // ── The card, and the subtree that draws it ───────────────────────────────────────────────────────

    /// <summary>The object card carries an outcome of its own AND its own subtree renders it. Two halves of
    /// one fact: a record field nothing draws is a field, not a sentence (#736's own guard, restated for the
    /// other full-screen card).</summary>
    [Fact]
    public void TheObjectCardHasAnOutcomeOfItsOwnAndDrawsIt()
    {
        Assert.Contains("string? Outcome", Rendering("DeckPlan.cs"), StringComparison.Ordinal);

        string block = BlockOf("@if (_viewObject is { } vo)");
        Assert.True(block.Contains("vo.Outcome", StringComparison.Ordinal),
            "the object card never renders its own Outcome — the dossier's sentences are stored and then " +
            "shown nowhere, which is worse than the pulse they replaced (#774/#736).");
        Assert.True(block.Contains("reveal-outcome", StringComparison.Ordinal),
            "the object card has no styled outcome row — an unstyled answer in a styled pop-up (#680/#736).");
    }

    /// <summary>The seam knows about the object card, and knows it is ON TOP. Both full-screen cards are
    /// drawn with the same backdrop class, so DOM order decides, and the object card is written after the
    /// story card in Map.razor — which the outpost's effects console reaches for real, raising the plate and
    /// the dossier on one press.
    ///
    /// <para>#664 · The card on the other side of this comparison used to be <c>_revealCard</c>, the
    /// client-only twin the fork built. It is <c>_storyCard</c> now — same backdrop class, same DOM-order
    /// rule, same z-order claim, one fewer card.</para></summary>
    [Fact]
    public void TheSeamAnswersOnTheObjectCardBeforeTheStoryCardBecauseItIsInFront()
    {
        string seam = Method("Map.Surface.Satchel.cs", "private void SayItWhereTheyAreLooking(");

        int viewObject = seam.IndexOf("if (_viewObject is", StringComparison.Ordinal);
        int revealCard = seam.IndexOf("if (_storyCard is", StringComparison.Ordinal);
        Assert.True(viewObject >= 0,
            "the seam does not know the object card exists — every answer given while one is up plays " +
            "behind its backdrop (#774/#736).");
        Assert.True(revealCard > viewObject,
            "the seam answers on the story card first, and the object card is drawn in front of it — the " +
            "answer would land on the card the captain cannot see (#774).");

        // And it APPENDS. A slot has one winner; a region has room for all of them, which is the difference
        // between this fix and the hold #774 refused.
        string branch = seam[viewObject..revealCard];
        Assert.True(branch.Contains("already", StringComparison.Ordinal)
            && branch.Contains("\\n\\n", StringComparison.Ordinal),
            "the object card's outcome REPLACES instead of appending — an event with several things to say " +
            "in one breath is back to letting write order pick a survivor (#774/#693).");

        string razor = Pages("Map.razor");
        Assert.True(razor.IndexOf("@if (_viewObject is { } vo)", StringComparison.Ordinal)
            > razor.IndexOf("@if (_storyCard is { } told)", StringComparison.Ordinal),
            "the two cards have swapped places in Map.razor, so the seam's z-order is now upside down — " +
            "same backdrop class means the LATER block is the one in front (#774).");
    }

    // ── The demo, on demand ───────────────────────────────────────────────────────────────────────────

    /// <summary>A scene nobody can reach on demand is a scene that ships broken — and this one is three
    /// papers rooms in one excursion, at one room in eight, plus two one-in-three rolls for the full four
    /// sentences. So it gets a cheat, a front-door button, and a row in the testing guide.</summary>
    [Fact]
    public void TheFourSentenceDossierIsOneUrlAway()
    {
        Assert.Contains("kit=", Sim(), StringComparison.Ordinal);
        Assert.True(Sim().Contains("_kitCheat = candidate is", StringComparison.Ordinal),
            "?kit= is not parsed into the cheat the assembly reads (#774).");

        string assembly = Method("Map.Surface.Hive.cs", "private void AssembleSomebody(");
        Assert.True(assembly.Contains("_kitCheat", StringComparison.Ordinal)
            && assembly.Contains("everySaying: _kitCheat", StringComparison.Ordinal),
            "the cheat does not reach BOTH gates — a dev seat that assembles a two-sentence dossier cannot " +
            "show anybody the bug #774 is about (#774).");

        string starts = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Core", "DevStarts.cs"));
        Assert.True(starts.Contains("kit=1", StringComparison.Ordinal),
            "the dossier has no front-door button — the owner's rule for that catalogue is that these " +
            "special places to start are shown in the UI (#439/#774).");

        string guide = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "testing-guide.md"));
        Assert.True(guide.Contains("kit=1", StringComparison.Ordinal),
            "docs/testing-guide.md is the prose twin of the DevStarts catalogue and has no row for ?kit= (#774).");
    }
}
