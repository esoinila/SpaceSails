using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #690 · THE NOTEBOOK IS IN THE SATCHEL. Owner, mid-run, designing the paper-shedding loop:
/// <i>"should we have notes / clues section in our inventory ui?"</i> — and, on the register the tab's prose
/// had to be written in: <i>"it's like our detective notepad :-D"</i>.
///
/// <para>The field book (#587) rendered only in the Captain's ledger, a ship-brain surface, so a captain on
/// foot at a sealed door could not consult their own notes. #688 made that a real cost rather than an
/// inconvenience: leaving a paper files its gist to the book, which means knowledge was being deliberately
/// moved into a place unreachable from the ground it came off.</para>
///
/// <para><b>Why source-shape.</b> The same reasoning #680 and #688 wrote down: what is under test is not a
/// value a Core function returns, it is which control the razor renders, which store it reads and which page
/// an open lands on. The one law with a Core half — the place filter naming a ground the way the book names
/// it — is pinned in Core too (<c>TheBookOpensWhereYouAreStandingTests</c>).</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheNotebookIsInTheSatchelTests
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

    /// <summary>The satchel block of Map.razor — from the modal's opening guard to the view-object block that
    /// follows it, so every assertion is about THIS dialog's subtree and not the file at large. Same cut
    /// #688's guards make.</summary>
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

    /// <summary>An expression-bodied member, cut at its own semicolon — <see cref="Method"/> would carry the
    /// NEXT member's doc comment along with it, and a guard that forbids a character cannot be reading
    /// somebody else's prose.</summary>
    private static string Expression(string file, string signature)
    {
        string body = Method(file, signature);
        int end = body.IndexOf(';', StringComparison.Ordinal);
        Assert.True(end > 0, $"`{signature}` is not the one-expression member this guard expects.");
        return body[..(end + 1)];
    }

    [Fact]
    public void TheSatchelHasASecondPageAndItIsTheFieldBook()
    {
        // The whole feature in one shape: the dialog offers a NOTES page, and what that page draws comes out
        // of the book the ledger draws. RED against the build that shipped #688, where the satchel had one
        // page and the word NOTES appeared nowhere in it.
        string satchel = SatchelBlock();

        Assert.True(satchel.Contains("NOTES", StringComparison.Ordinal),
            "the satchel dialog has no NOTES page — on foot at a sealed door the captain still cannot read " +
            "their own field book, which is the whole of #690.");
        Assert.True(satchel.Contains("_satchelPage", StringComparison.Ordinal),
            "the satchel draws no page state, so it cannot have two pages (#690).");
        Assert.True(satchel.Contains("_fieldNotes", StringComparison.Ordinal),
            "the satchel never reads _fieldNotes — whatever its NOTES page is showing, it is not the book " +
            "the ledger shows (#690/#587).");
        Assert.True(satchel.Contains("FieldNotes.PerPlace(", StringComparison.Ordinal),
            "the every-ground view does not use the ledger's own grouping — a second grouping law is a " +
            "second answer to `what have I found here` (#690).");
    }

    [Fact]
    public void TheBookIsREADONLYInTheSatchel()
    {
        // The satchel holds the book open; it does not hold a pen. The book is capped, deduplicated and
        // vault-persisted by its own laws (#587), and an edit or a delete in a dialog would be a second set
        // of rules for the same pages.
        string satchel = SatchelBlock();

        int notes = satchel.IndexOf("_satchelPage == SatchelPage.Notes", StringComparison.Ordinal);
        Assert.True(notes >= 0, "the satchel no longer branches on its NOTES page where this guard looks.");

        Assert.False(satchel.Contains("_fieldNotes =", StringComparison.Ordinal),
            "the satchel dialog ASSIGNS the field book — the notebook page has grown a pen (#690).");
        foreach (string mutation in new[] { "_fieldNotes.Remove", "_fieldNotes.Clear", "_fieldNotes.Add" })
        {
            Assert.False(satchel.Contains(mutation, StringComparison.Ordinal),
                $"the satchel dialog calls {mutation} — the book is read-only from here (#690).");
        }
    }

    [Fact]
    public void ThereIsSTILLExactlyOneNoteStoreInTheClient()
    {
        // #587's founding law: ONE recorder, one place that can never be forgotten about. A tab that kept its
        // own copy — "the notes this excursion filed", say — would be the second store this repo has already
        // paid for once (#590's card set, replaced by the satchel for exactly this reason), and the two would
        // disagree on the first vault load.
        //
        // Fields only, and only in the client: Core's FieldNotesSection is the SAVE FILE's shape, not a live
        // store, and the projections (PerPlace, Here) build lists that never outlive the call.
        // A FIELD, not a method that happens to return notes: the name has to be followed by `=`, `;` or `{`,
        // which is what tells a store apart from the projections that read one.
        var collections = new Regex(
            @"(?:private|internal|public|protected)[^\n=;()]*"
            + @"\b(?:List|IReadOnlyList|IList|HashSet|Queue|Stack)\s*<\s*(?:SpaceSails\.)?(?:Core\.)?FieldNote\s*>"
            + @"\s*(\w+)(?=\s*[=;{])",
            RegexOptions.Compiled);

        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || (!file.EndsWith(".cs", StringComparison.Ordinal) && !file.EndsWith(".razor", StringComparison.Ordinal)))
            {
                continue;
            }

            foreach (Match m in collections.Matches(File.ReadAllText(file)))
            {
                found.Add($"{Path.GetFileName(file)}:{m.Groups[1].Value}");
            }
        }

        // Arrays of notes are the same store wearing a different type.
        Assert.DoesNotContain(
            Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "*.cs"),
            f => Regex.IsMatch(File.ReadAllText(f),
                @"(?:private|internal|public|protected)[^\n=;()]*\bFieldNote\s*\[\s*\]\s*\w+"));

        Assert.True(found.Count == 1 && found[0].EndsWith(":_fieldNotes", StringComparison.Ordinal),
            "the client keeps more than one collection of field notes — #587's one-recorder law is what makes " +
            "the book trustworthy, and #690's NOTES tab was specified to READ it, not to copy it. Found: "
            + string.Join(", ", found));
    }

    [Fact]
    public void ThisGroundIsNamedTheWayTheBOOKNamesIt()
    {
        // The filter's one real trap. The book files a note under FieldNotes.PlaceLabel(body, site); a tab
        // that built "Miranda · The Ridge Camp" for itself would match today and drift the first time the
        // label changes — the repo's first named bug class (two sources of truth) aimed at a string.
        string place = Expression("Map.Surface.Satchel.cs", "private string? PlaceUnderfoot()");

        Assert.True(place.Contains("FieldNotes.PlaceLabel(", StringComparison.Ordinal),
            "the NOTES tab re-derives the name of the ground it is standing on instead of asking " +
            "FieldNotes.PlaceLabel — two formats for one place (#690).");
        Assert.False(place.Contains(" · ", StringComparison.Ordinal),
            "the NOTES tab spells the book's place separator out by hand (#690).");

        string satchel = SatchelBlock();
        Assert.True(satchel.Contains("PlaceUnderfoot()", StringComparison.Ordinal)
            || satchel.Contains("FieldNotes.PlaceLabel(", StringComparison.Ordinal),
            "the satchel's this-ground view never names the ground through the book's own label (#690).");

        // And the filtering itself is Core's grouping, not a predicate written in the dialog.
        string here = Expression("Map.Surface.Satchel.cs", "private IReadOnlyList<Core.FieldNote> NotesFromThisGround()");
        Assert.True(here.Contains("Core.FieldNotes.Here(", StringComparison.Ordinal),
            "the this-ground filter is not the ledger's own grouping filtered — it is a second reading of " +
            "the log (#690).");
        Assert.True(here.Contains("_fieldNotes", StringComparison.Ordinal),
            "the this-ground view reads something other than the one book (#690/#587).");
    }

    [Fact]
    public void EveryOpenLandsOnThePocketAndOnThisGround()
    {
        // The pocket is the primary tool: I is pressed to see what you are carrying, and a satchel that
        // remembered you were last reading notes would answer a different question than the one the reflex
        // asked. Opening AT a door lands on CARRIED too — the offer flow is exactly as #688 left it, and the
        // notebook is one tap away, which is the point.
        foreach (string opener in new[]
                 {
                     "private void OpenSatchel()",
                     "private void OpenSatchelAtTheDoor()",
                 })
        {
            string body = Method("Map.Surface.cs", opener);
            Assert.True(body.Contains("TheSatchelOpensOnThePocket()", StringComparison.Ordinal),
                $"{opener} does not land the dialog on CARRIED — the page a captain left it on last time " +
                "answers a question they did not ask (#690).");
        }

        string reset = Method("Map.Surface.Satchel.cs", "private void TheSatchelOpensOnThePocket()");
        Assert.True(reset.Contains("SatchelPage.Carried", StringComparison.Ordinal),
            "the open-reset does not select the CARRIED page (#690).");
        // #741 · The two-state ground filter became a three-state READING of the book (this ground / every
        // ground / the case the red lines have made of it). The law is unchanged and so is this guard: an
        // open lands on THIS GROUND, whichever reading was left open last.
        Assert.True(reset.Contains("_notesView = NotesView.Here", StringComparison.Ordinal),
            "the open-reset leaves the notebook on whatever reading it was last on — at a door the " +
            "captain wants THIS building, not the memoirs (#690).");

        // #688's I toggle survives the second page: the key still shuts the pocket it opens, whichever half
        // of it is showing.
        string toggle = Method("Map.Surface.Satchel.cs", "private void ToggleSatchel()");
        Assert.True(toggle.Contains("CloseSatchel()", StringComparison.Ordinal),
            "I no longer closes the satchel (#688).");
        Assert.False(toggle.Contains("SatchelPage", StringComparison.Ordinal),
            "the I key has learned about pages — it toggles the satchel and nothing else (#690).");
    }

    [Fact]
    public void TheNotebookSaysEverythingInsideTheDialogsOwnLayer()
    {
        // #680/#686's law, applied from birth rather than after a playtest: the pulse HUD renders UNDER the
        // modal backdrop's blur, so anything this tab says from behind that backdrop would be in the DOM and
        // not on the screen. Switching pages or filters says nothing anywhere — it is a view change.
        string satchel = SatchelBlock();
        int notes = satchel.IndexOf("_satchelPage == SatchelPage.Notes", StringComparison.Ordinal);
        Assert.True(notes >= 0, "the satchel no longer branches on its NOTES page where this guard looks.");

        Assert.False(satchel.Contains("ShowPulseMessage(", StringComparison.Ordinal),
            "the satchel dialog pulses a line from behind its own backdrop (#680).");

        string css = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));
        foreach (string rule in new[] { ".satchel-tab", ".satchel-note", ".satchel-filter" })
        {
            Assert.True(css.Contains(rule, StringComparison.Ordinal),
                $"{rule} has no style of its own — the notebook is riding on the item list's rules (#690).");
        }
    }
}
