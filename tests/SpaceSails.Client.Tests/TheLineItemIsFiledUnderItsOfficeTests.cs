using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1074 beat 3 · THE LINE ITEM REACHES THE BOOK WITH ITS TWO NAMES ON IT.
///
/// <para>Core decides which rooms hold a cost-centre paper, what it says and what it is about
/// (<c>TheMoneyTrailTests</c>); this guards the WIRING, which is a partial class in a razor page and
/// therefore the one part of this beat no Core test can see. Same idiom and the same reason as
/// <c>TheShelfIsReadWhereItStandsTests</c>: the failure that matters is not a wrong answer, it is the
/// answer never being ASKED FOR — a find filed through the two-argument <c>FileNote</c> seam is a find
/// that names nothing, and the THREADS page it was written for would simply be empty with every Core
/// guard still green.</para>
///
/// <para>Read off the source rather than driven through a descent, which is
/// <c>TheShelfIsReadWhereItStandsTests</c>' own call and is made for its reason: the room search wants a
/// built deck, a nearest console and an excursion, and a guard that stood all that up would be testing the
/// scaffolding. What can go wrong here is one call site reverting to the seam that drops the subjects, and
/// that is a thing the text can see.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheLineItemIsFiledUnderItsOfficeTests
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

    /// <summary>#870 · The surface page is a stack of partials by subject, so "the surface wiring" a guard
    /// reads over is all of them — exactly the text it read out of one file before the split.</summary>
    private static string Surface() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Surface*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>
    /// #1074/#741 · THE ROOM'S FIND IS ANNOUNCED AND FILED THROUGH THE SEAM THAT CARRIES SUBJECTS, AND CORE
    /// IS THE ONE THAT SAYS WHAT THEY ARE.
    ///
    /// <para>Two halves and the second is the one with teeth. The haul's own sentence must go through
    /// <c>ShowAndFileAbout</c> — the sibling that keeps subjects — and the subjects handed to it must come
    /// from <c>UndergroundComplex.MoneyTrailSubjectsFor</c> rather than from anything composed in the page.
    /// The page composing its own would be the client minting a heading for an office it does not own, which
    /// is exactly what <c>CaseSubjects</c>' first law forbids: a subject comes from the AUTHOR of the
    /// sentence, and the author of this sentence is Core.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the call site put back to <c>ShowAndFile(find.RoomLine +
    /// pick.Line, …)</c> — <i>"Assert.All() Failure: 1 out of 1 items in the collection did not pass.
    /// Error: a room's find is not filed through a seam that carries subjects"</i>, every line item landing
    /// in the book as a loose entry and the Authority thread never forming; and the subjects argument
    /// replaced with <c>CaseSubjects.Office("AUTHORITY")</c> composed in the page —
    /// <i>"Assert.Contains() Failure: Sub-string not found"</i>, Core never asked and the client minting a
    /// heading for an office it does not own.</para>
    /// </summary>
    [Fact]
    public void TheHiveRoomsFindIsFiledThroughTheSeamThatCarriesSubjects()
    {
        string surface = Surface();

        // THE call site that says what a searched room yielded. #615 made there be exactly one of these — a
        // find the captain decides about and a find that simply falls into the pocket arrive at one pickup
        // body — and the whole SET is asked for rather than the first hit, so a second copy of the call
        // could never hide behind this one passing.
        MatchCollection said =
            Regex.Matches(surface, @"ShowAndFile(About)?\(\s*\r?\n?\s*find\.RoomLine \+ pick\.Line");
        Assert.Single(said);
        Assert.All(said, m => Assert.True(m.Groups[1].Success,
            "a room's find is not filed through a seam that carries subjects — "
            + "a cost-centre line item would land in the book as a loose entry (#1074 beat 3)"));

        // …and what it files them under is Core's answer, never a string built here.
        Assert.Contains("UndergroundComplex.MoneyTrailSubjectsFor(", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("CaseSubjects.Office(", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("MoneyTrail.SubjectsFor(", surface, StringComparison.Ordinal);
    }
}
