using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #741 v1 · THE THREADS PAGE — the client half of the subjects view.
///
/// <para>Owner's issue: <i>"a third tab — THREADS — grouping existing entries by the entities they already
/// name… Rendering: subject as heading, its entries beneath, chronological."</i> And the nice-to-have that
/// is really the beat: <i>"a thread badge on new gists ('second entry about OFFICE OF WORKS'), because
/// noticing a thread FORM is the detective-fiction dopamine."</i></para>
///
/// <para><b>Why source-shape.</b> The same reasoning #690's guards wrote down: what is under test is not a
/// value a Core function returns — Core's half is pinned in <c>TheThreadsAreTheAuthorsTests</c> — it is
/// which control the razor renders, which store it reads, and WHERE the badge is said. The last of those is
/// the one that has bitten this repo twice (#774, #768): a sentence pulsed under a card's own backdrop is a
/// sentence in the DOM and not on the screen.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheThreadsPageIsInTheSatchelTests
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

    /// <summary>The satchel block of Map.razor, cut the way #690's guards cut it — every assertion is about
    /// THIS dialog's subtree and not the file at large.</summary>
    private static string SatchelBlock()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_showSatchel)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the satchel block this guard knows how to find.");
        int end = razor.IndexOf("_viewObject is { }", start, StringComparison.Ordinal);
        Assert.True(end > start, "Map.razor's satchel block no longer ends where this guard expects.");
        return razor[start..end];
    }

    /// <summary>One method's body, cut at the next member declaration — #680's idiom.</summary>
    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        int end = src.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        return src[at..(end > at ? end : src.Length)];
    }

    // ── THE TAB ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// There is a THREADS tab, it is beside NOTES, and it is ALWAYS DRAWN — like the compass and unlike the
    /// spread. An empty threads page is an answer ("nothing in this book names the same thing twice, yet");
    /// a tab that appeared only once a case was forming would be missing exactly while a captain wondered
    /// whether the game did this at all.
    /// </summary>
    [Fact]
    public void TheSatchelHasAThreadsTabAndItIsAlwaysDrawn()
    {
        string satchel = SatchelBlock();

        int tabs = satchel.IndexOf("<div class=\"satchel-tabs\">", StringComparison.Ordinal);
        Assert.True(tabs >= 0, "the satchel no longer has a tab strip where this guard looks.");
        int tabsEnd = satchel.IndexOf("</div>\n\n", tabs, StringComparison.Ordinal);
        string strip = satchel[tabs..(tabsEnd > tabs ? tabsEnd : satchel.Length)];

        int notes = strip.IndexOf("SatchelPage.Notes", StringComparison.Ordinal);
        int threads = strip.IndexOf("SatchelPage.Threads", StringComparison.Ordinal);
        Assert.True(notes >= 0, "the NOTES tab has moved out of the tab strip.");
        Assert.True(threads > notes, "the THREADS tab is not drawn beside (and after) the book (#741 v1).");

        // The label and the hint are CORE's, so the tab and the page cannot come to two views of one word.
        Assert.Contains("CaseSubjects.TabLabel", strip, StringComparison.Ordinal);
        Assert.Contains("CaseSubjects.TabHint", strip, StringComparison.Ordinal);

        // Always drawn: no @if between the NOTES button and the THREADS button. The spread and the bin are
        // conditional and sit AFTER it, which is why the cut ends at the threads tab's own button.
        string between = strip[notes..threads];
        Assert.False(between.Contains("@if", StringComparison.Ordinal),
            "the THREADS tab has been put behind a condition — an empty threads page is an answer (#741 v1).");
    }

    /// <summary>The page renders CORE's stacks and groups nothing itself. A razor that grouped notes by hand
    /// would be a second arrangement of one book, which is the repo's first named bug class aimed at a
    /// page.</summary>
    [Fact]
    public void TheThreadsPageAsksCoreAndDecidesNothing()
    {
        string satchel = SatchelBlock();
        // The BRANCH, not the tab button's own class attribute — which mentions the page one screen higher.
        int page = satchel.IndexOf("else if (_satchelPage == SatchelPage.Threads)", StringComparison.Ordinal);
        Assert.True(page >= 0, "the satchel no longer branches on its THREADS page.");
        int end = satchel.IndexOf("else if (_satchelPage == SatchelPage.Bin)", page, StringComparison.Ordinal);
        string block = satchel[page..(end > page ? end : satchel.Length)];

        Assert.Contains("TheBookThreads()", block, StringComparison.Ordinal);
        Assert.Contains("CaseSubjects.Blurb", block, StringComparison.Ordinal);
        Assert.Contains("CaseSubjects.NothingTwiceLine", block, StringComparison.Ordinal);
        Assert.Contains("stack.Heading", block, StringComparison.Ordinal);
        Assert.Contains("CaseSubjects.EntriesLabel", block, StringComparison.Ordinal);

        // The rows are the notebook's own nodes, so an entry reads the same on both pages.
        Assert.Contains("<NotebookNodes", block, StringComparison.Ordinal);
        Assert.Contains("NotebookPage(stack.Entries)", block, StringComparison.Ordinal);

        // …and the red pen works from here: the same press handler the notebook uses.
        Assert.Contains("OnTitle=\"NoteTitlePressed\"", block, StringComparison.Ordinal);
        Assert.Contains("CaseThreads.PenLabel", block, StringComparison.Ordinal);
        Assert.Contains("RunThePenDownTheStack", block, StringComparison.Ordinal);

        // It arranges nothing of its own and writes nothing back.
        foreach (string forbidden in new[]
        {
            "GroupBy", "OrderBy", "Sort(", ".Where(", "_fieldNotes =", "_fieldNotes.Add",
        })
        {
            Assert.False(block.Contains(forbidden, StringComparison.Ordinal),
                $"the THREADS page does `{forbidden}` — the grouping, the order and the headings are "
                + "CaseSubjects' (#741 v1), and the book is read-only from the satchel (#690).");
        }
    }

    // ── THE BADGE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// "second entry about OFFICE OF WORKS" goes ON THE CARD IN FRONT OF THE CAPTAIN — #736's law — and is
    /// never pulsed, never bannered, and never appended twice to one card.
    ///
    /// <para>This is the assertion that matters most on this side. The dossier raises a full-screen card and
    /// then files four sentences under it; a badge sent to the pulse HUD would play under that card's own
    /// backdrop and be gone in eight seconds, which is exactly the bug #774 was opened for.</para>
    /// </summary>
    [Fact]
    public void TheBadgeIsComposedOntoTheCardAndIsNeverABanner()
    {
        string badge = Method("Map.RedPen.cs", "private void TheThreadBadgeGoesOnTheCard(");

        Assert.Contains("CaseSubjects.NewThreadBadge(", badge, StringComparison.Ordinal);
        Assert.Contains("_viewObject", badge, StringComparison.Ordinal);
        Assert.Contains("Outcome", badge, StringComparison.Ordinal);

        // ONCE: an existing copy of the same line is not appended a second time.
        Assert.Contains("Contains(badge", badge, StringComparison.Ordinal);

        foreach (string banner in new[]
        {
            "ShowPulseMessage", "ShowAndFile", "_pulse", "_held", "HoldSaying", "SayItWhereTheyAreLooking",
        })
        {
            Assert.False(badge.Contains(banner, StringComparison.Ordinal),
                $"the thread badge goes through `{banner}` — it must be composed onto the card the captain "
                + "is already reading (#736), never fired at a HUD a backdrop is standing in front of.");
        }
    }

    /// <summary>The filing funnel carries the author's subjects, and the dossier — the one author that has
    /// any — hands its own field across rather than the client working one out.</summary>
    [Fact]
    public void TheFilingFunnelCarriesTheAuthorsSubjects()
    {
        string satchel = Pages("Map.Surface.Satchel.cs");

        // The two-argument form four dozen sites call is still exactly that — a defaulted third parameter
        // would have changed the signature under every one of them and under the guards that reach for it
        // by reflection.
        Assert.Contains("private void FileNote(string text, string glyph) => FileNoteAbout(text, glyph, \"\");",
            satchel, StringComparison.Ordinal);

        string file = Method("Map.Surface.Satchel.cs", "private void FileNoteAbout(");
        Assert.Contains("glyph, subjects)", file, StringComparison.Ordinal);
        Assert.Contains("TheThreadBadgeGoesOnTheCard(", file, StringComparison.Ordinal);

        // The client never invents one. No literal subject anywhere in the Pages tree: every subject in the
        // game is minted by the Core author that wrote the sentence.
        foreach (string page in Directory.EnumerateFiles(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*.*", SearchOption.AllDirectories))
        {
            if (page.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || page.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || (!page.EndsWith(".cs", StringComparison.Ordinal) && !page.EndsWith(".razor", StringComparison.Ordinal)))
            {
                continue;
            }

            string src = File.ReadAllText(page);
            foreach (string mint in new[] { "CaseSubjects.Office(", "CaseSubjects.Place(", "CaseSubjects.Person(" })
            {
                Assert.False(src.Contains(mint, StringComparison.Ordinal),
                    $"{Path.GetFileName(page)} mints a subject — subjects are the AUTHOR's, declared in Core "
                    + "beside the sentence they describe (#741 v1).");
            }
        }

        // The dossier site passes the record's own field, byte for byte.
        Assert.Contains("FileNoteAbout(said.Text, said.Glyph, said.Subjects)",
            Pages("Map.Surface.Hive.cs"), StringComparison.Ordinal);
    }

    // ── #905 · THE FRAME LEDGER ─────────────────────────────────────────────────────────────────────────

    /// <summary>#905 · This build adds NO new state to <c>Map</c>. The threads are a reading of the book and
    /// the badge is a line on a card that is already up — both are computed where they are drawn. The four
    /// fields the red pen already owned are still the only ones in this file.</summary>
    [Fact]
    public void TheThreadsAddNoFieldToTheMap()
    {
        var fields = new Regex(
            @"^\s*private\s+(?:readonly\s+)?[^\n=;()]*?\b(_\w+)\s*[=;]",
            RegexOptions.Multiline | RegexOptions.Compiled);

        List<string> found =
            [.. fields.Matches(Pages("Map.RedPen.cs")).Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal)];
        found.Sort(StringComparer.Ordinal);

        Assert.Equal(["_caseThreads", "_notesExpanded", "_penHoldingId", "_penInHand"], found);
    }
}
