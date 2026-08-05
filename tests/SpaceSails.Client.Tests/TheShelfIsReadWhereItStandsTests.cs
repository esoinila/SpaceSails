using System;
using System.IO;
using System.Linq;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #701 · THE ODD BOOK IS READ WHERE IT STANDS, AND NOTHING LEAVES THE ROOM.
///
/// <para>Core decides whether a room has a book in it and what the book says; these guard the WIRING, which
/// is a partial class in a razor page and therefore the one part of this feature no Core test can see. Same
/// idiom and the same reason as <see cref="TheCardOpensTheBottomFloorTests"/>: what must never come back is
/// a payout, a struck-off room or a second source of truth appearing in the wrong PLACE.</para>
///
/// <para>Three laws, and each of them is a way the feature dies quietly:</para>
/// <list type="number">
/// <item><b>Nothing enters the satchel and the room is never struck off.</b> The moment the book branch
/// falls through into the haul path it becomes loot: a credit, a pocket, a room that cannot be walked back
/// into — and re-reading, which the once-per-book law exists to allow, becomes impossible.</item>
/// <item><b>The card is caption-only.</b> There is no art for these; a wired-but-unpainted url renders an
/// img the browser hides, which is a different card from one that never claimed a picture.</item>
/// <item><b>The cheat is an ARGUMENT, never a second answer at the call site</b> (§13.18) — the rule this
/// project wrote down after an <c>||</c> nearly blacked out the regolith at noon.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheShelfIsReadWhereItStandsTests
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

    /// <summary>The branch itself: everything between the one ask and the haul path it must never reach.</summary>
    private static string BookBranch() =>
        Between(Pages("Map.Surface.cs"), "if (OddBooks.Search(", "// Everything found down here is FILED");

    [Fact]
    public void TheBookIsAskedForBEFORETheRoomIsEverTurnedIntoAHaul()
    {
        string surface = Pages("Map.Surface.cs");
        int asks = surface.IndexOf("if (OddBooks.Search(", StringComparison.Ordinal);
        int pocket = surface.IndexOf("UndergroundComplex.WhatGoesInThePocket(", StringComparison.Ordinal);
        int struck = surface.IndexOf("ex.HiveRoomsEmptied.Add(roomKey);", StringComparison.Ordinal);

        Assert.True(asks > 0, "nothing in the surface wiring asks Core whether this room has a book in it.");
        Assert.True(asks < pocket,
            "the odd book is decided AFTER the pocket has been offered the room — a book that has already " +
            "been through WhatGoesInThePocket is loot.");
        Assert.True(asks < struck,
            "the odd book is decided after the room is struck off — the shelf could never be read twice.");

        // And the branch leaves the method rather than falling through into any of it.
        Assert.Contains("return;", BookBranch(), StringComparison.Ordinal);
    }

    [Fact]
    public void NothingEverLeavesTheRoomAndTheRoomIsNeverStruckOff()
    {
        string branch = BookBranch();
        foreach (string forbidden in new[]
                 {
                     "HiveRoomsEmptied", "Satchel.Add", "_credits", "GrantLabLead", "PlayCue",
                     "WhatGoesInThePocket", "ApplyNerveShock",
                 })
        {
            Assert.DoesNotContain(forbidden, branch, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheLookCardIsCaptionOnlyAndClaimsNoPicture()
    {
        string branch = BookBranch();
        Assert.Contains("DeckPlan.ConsoleKind.ViewObject", branch, StringComparison.Ordinal);

        // The #528 idiom's three trailing arguments are (label, imageUrl, caption). The lifeboat-muster
        // precedent passes no url at all; anything else here would render an img and hide it on error.
        Assert.Contains("shelf.Title, null, shelf.Card", branch, StringComparison.Ordinal);
        Assert.DoesNotContain("ArtUrl", branch, StringComparison.Ordinal);
        Assert.DoesNotContain(".jpg", branch, StringComparison.Ordinal);
        Assert.DoesNotContain(".png", branch, StringComparison.Ordinal);
    }

    /// <summary>The gist is FILED and never ShowAndFiled — the saying has already happened in the pulse, and
    /// a second pulse would play under the card's own blur (#686). And it files only when Core says this
    /// reading files anything: the once-per-book law is not re-decided here.</summary>
    [Fact]
    public void TheGistIsFiledOnlyWhenCoreSaysThisReadingFilesAnything()
    {
        string branch = BookBranch();

        Assert.Contains("if (shelf.Gist is { } gist)", branch, StringComparison.Ordinal);
        Assert.Contains("FileNote(gist, OddBooks.Glyph);", branch, StringComparison.Ordinal);

        // The room's own line is a pulse and is never filed: the casebook keeps the GIST, and filing both
        // would put one shelf in the book twice, in two registers, on every search.
        Assert.Contains("ShowPulseMessage(shelf.Line);", branch, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowAndFile(", branch, StringComparison.Ordinal);

        // And the read-list is only ever advanced to the list Core handed back.
        Assert.Contains("_oddBooksRead = [.. shelf.Filed];", branch, StringComparison.Ordinal);
        Assert.DoesNotContain("_oddBooksRead.Add", branch, StringComparison.Ordinal);
    }

    /// <summary>§13.18's rule, applied to the second cheat that could break it: the switch is handed to the
    /// one ask and is never consulted anywhere else — no <c>||</c>, no <c>?:</c>, no <c>if</c> of its own.
    /// Outside the URL parser the token may appear exactly twice: its declaration, and that argument.</summary>
    [Fact]
    public void TheCheatIsAnArgumentToTheOneAskAndNeverASecondAnswer()
    {
        string surface = Pages("Map.Surface.cs");
        int uses = surface.Split("_bookCheat").Length - 1;
        Assert.True(uses == 2,
            $"_bookCheat appears {uses} time(s) in the surface wiring — it may appear exactly twice, as a " +
            "field and as an argument to OddBooks.Search.");

        Assert.Contains("private int? _bookCheat;", surface, StringComparison.Ordinal);
        Assert.Contains(
            "OddBooks.Search(ex.Stop.Body.Id, ex.Floor, which, _oddBooksRead, _bookCheat)",
            surface, StringComparison.Ordinal);

        // Read it back off the raw text so a future `_bookCheat ||` or `_bookCheat ?` cannot slip in
        // anywhere in the client at all.
        string sim = Pages("Map.Sim.cs");
        foreach (string line in (surface + "\n" + sim).Split('\n')
                     .Where(l => l.Contains("_bookCheat", StringComparison.Ordinal)))
        {
            Assert.DoesNotContain("||", line, StringComparison.Ordinal);
            Assert.DoesNotContain("&&", line, StringComparison.Ordinal);
        }

        // The cheat is parsed, and its range is the catalog's own rather than a number typed in twice.
        Assert.Contains("pair.StartsWith(\"book=\"", sim, StringComparison.Ordinal);
        Assert.Contains("Core.OddBooks.Catalog.Count", sim, StringComparison.Ordinal);
    }

    /// <summary>The read-list rides the vault, because the one-shot is about KNOWLEDGE and knowledge does not
    /// un-happen on a reload — the same idiom as the found secret labs (#409).</summary>
    [Fact]
    public void TheBooksThisThreadHasReadAreSavedAndLoadedBack()
    {
        string vault = Pages("Map.Vault.cs");
        Assert.Contains("OddBooksRead = [.. _oddBooksRead]", vault, StringComparison.Ordinal);
        Assert.Contains("vault.Progress?.OddBooksRead is { } books", vault, StringComparison.Ordinal);
    }
}
