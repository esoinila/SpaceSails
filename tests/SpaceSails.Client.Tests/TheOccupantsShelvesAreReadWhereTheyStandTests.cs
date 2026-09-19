using System;
using System.IO;
using System.Linq;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #701 · THE LIBRARY LAYER IS READ WHERE IT STANDS, AND IT IS THE ODD BOOK'S OWN PATH.
///
/// <para>Core decides which rooms are somebody's, what is on their two shelves and whether a reading files
/// anything; these guard the WIRING, which is a partial class in a razor page and therefore the one part of
/// this feature no Core test can see. Same idiom and the same reason as
/// <see cref="TheShelfIsReadWhereItStandsTests"/>, which guards the other half of #701.</para>
///
/// <para>Four laws, and each is a way this half dies quietly:</para>
/// <list type="number">
/// <item><b>Nothing leaves the shelf.</b> No pocket, no credit, no lead, and above all no struck-off room —
/// a shelf is a fixture, and the press must be repeatable forever.</item>
/// <item><b>The card is caption-only.</b> There is no art for these; a wired-but-unpainted url renders an
/// img the browser hides, which is a different card from one that never claimed a picture.</item>
/// <item><b>The gist files through #741's subject law</b>, declared by the author, and the subject is the
/// PLACE — a shelf names nobody, and a personal heading out of one would be the game detecting.</item>
/// <item><b>One read-list, not two.</b> The shelves ride the odd book's own vault field; a second store
/// beside it would be two answers to one question about one captain's knowledge.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheOccupantsShelvesAreReadWhereTheyStandTests
{
    private static string Pages(string file) =>
        File.ReadAllText(
            Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static string Rendering(string file) =>
        File.ReadAllText(
            Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Rendering", file));

    /// <summary>#870 · The surface page is a family of partials by subject, so "the surface wiring" a guard
    /// counts over is all of them.</summary>
    private static string Surface() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Surface*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    private static string Press() => Pages("Map.Surface.Hive.Shelf.cs");

    [Fact]
    public void ReadingAShelfTakesNothingAndNeverStrikesTheRoomOff()
    {
        string press = Press();
        foreach (string forbidden in new[]
                 {
                     "HiveRoomsEmptied", "Satchel.Add", "_credits", "GrantLabLead",
                     "WhatGoesInThePocket", "OfferKeepOrLeave", "ApplyNerveShock",
                     "TheRoomHasBeenGoneThrough(",
                 })
        {
            Assert.DoesNotContain(forbidden, press, StringComparison.Ordinal);
        }

        // …and the press is reached by its own console kind, so it can never be answered by the haul table
        // that DOES strike rooms off.
        Assert.Contains("DeckPlan.ConsoleKind.HiveShelf", press, StringComparison.Ordinal);
        Assert.Contains(
            "case DeckPlan.ConsoleKind.HiveShelf:", Pages("Map.Deck.Interact.cs"), StringComparison.Ordinal);
        Assert.Contains("HiveShelfInteract();", Pages("Map.Deck.Interact.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheLookCardIsCaptionOnlyAndClaimsNoPicture()
    {
        string press = Press();
        Assert.Contains("DeckPlan.ConsoleKind.ViewObject", press, StringComparison.Ordinal);

        // The #528 idiom's three trailing arguments are (label, imageUrl, caption). The lifeboat-muster
        // precedent the odd book already took passes no url at all.
        Assert.Contains("look.Title, null, look.Card", press, StringComparison.Ordinal);
        Assert.DoesNotContain("ArtUrl", press, StringComparison.Ordinal);
        Assert.DoesNotContain(".jpg", press, StringComparison.Ordinal);
        Assert.DoesNotContain(".png", press, StringComparison.Ordinal);
    }

    /// <summary>The gist is FILED and never ShowAndFiled — the saying happens on the card, and a pulse would
    /// play under that card's own blur (#686/#736). And it files only when Core says this reading files
    /// anything: the once-per-shelf law is not re-decided here.</summary>
    [Fact]
    public void TheGistFilesOnlyWhenCoreSaysThisReadingFilesAnythingAndDeclaresItsSubject()
    {
        string press = Press();

        Assert.Contains("if (look.Gist is { } gist)", press, StringComparison.Ordinal);
        Assert.Contains(
            "FileNoteAbout(gist, Shelves.Glyph, Shelves.SubjectsFor(TheBooksNameForHere()));",
            press, StringComparison.Ordinal);

        // #741 · The subject is minted in Core beside the sentence that names it, and it is a PLACE. A
        // client that reached for CaseSubjects.Person here would be the game naming somebody it has never
        // printed — out of a shelf, which names nobody at all.
        Assert.DoesNotContain("CaseSubjects.Person", press, StringComparison.Ordinal);
        Assert.DoesNotContain("CaseSubjects.Office", press, StringComparison.Ordinal);

        Assert.DoesNotContain("ShowPulseMessage(", press, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowAndFile(", press, StringComparison.Ordinal);

        // And the read-list is only ever advanced to the list Core handed back.
        Assert.Contains("_oddBooksRead = [.. look.Filed];", press, StringComparison.Ordinal);
        Assert.DoesNotContain("_oddBooksRead.Add", press, StringComparison.Ordinal);
    }

    /// <summary>ONE READ-LIST, NOT TWO. Both halves of #701 ride <c>Vault.Progress.OddBooksRead</c>; the ids
    /// are namespaced in Core so they cannot collide, and the client grows no second field for the shelves.
    /// A second store is the shape this repo has paid for repeatedly — two answers to one question, and the
    /// one that got missed is the one a captain notices.</summary>
    [Fact]
    public void TheShelvesRideTheOddBooksOwnReadListAndTheClientGrowsNoSecondStore()
    {
        string surface = Surface();
        Assert.Contains("private List<string> _oddBooksRead = [];", surface, StringComparison.Ordinal);

        foreach (string invented in new[] { "_shelvesRead", "_shelfGists", "_shelvesFiled" })
        {
            Assert.DoesNotContain(invented, surface, StringComparison.Ordinal);
        }

        string vault = Pages("Map.Vault.cs");
        Assert.Contains("OddBooksRead = [.. _oddBooksRead]", vault, StringComparison.Ordinal);
        Assert.Contains("vault.Progress?.OddBooksRead is { } books", vault, StringComparison.Ordinal);
        Assert.DoesNotContain("ShelvesRead", vault, StringComparison.Ordinal);
    }

    /// <summary>THE RENDERER MEASURES NOTHING (§13.15). Core publishes every shelf's coordinate and its
    /// plate; this hangs a console on it. A renderer that worked out where a shelf goes would be a second
    /// placer, and the two would disagree the first time a doorway moved.</summary>
    [Fact]
    public void TheRendererAsksCoreWhereTheShelvesAreAndPlacesNoneItself()
    {
        string signage = Rendering("HiveInterior.Floor.Signage.cs");
        Assert.Contains("foreach (Shelves.Shelf shelf in floor.TheShelves)", signage, StringComparison.Ordinal);
        Assert.Contains(
            "(float)shelf.X, (float)shelf.Y, shelf.Plate", signage, StringComparison.Ordinal);

        // No art slot and no caption on the deck spot: the card is opened by the press, off Core's own
        // reading, so a plate carrying the card text would be a second copy of authored prose.
        Assert.DoesNotContain("shelf.Card", signage, StringComparison.Ordinal);
        Assert.DoesNotContain("Shelves.On(", signage, StringComparison.Ordinal);
        Assert.DoesNotContain("Shelves.Occupied(", signage, StringComparison.Ordinal);

        Assert.Contains("StandTheShelves(consoles, in floor);", Rendering("HiveInterior.cs"),
            StringComparison.Ordinal);
    }
}
