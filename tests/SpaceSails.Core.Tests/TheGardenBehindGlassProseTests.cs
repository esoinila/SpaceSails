using System;
using System.Linq;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1332 B · <b>THE GARDEN BEHIND GLASS — THE WORDS.</b> One room plate, four bed plates in canon order, the
/// bar's menu line and the one told line: Fable canon, verbatim; the menu line is chalked on every board a
/// haven bar has; and nothing here spends a reserved word.
/// </summary>
public sealed class TheGardenBehindGlassProseTests
{
    /// <summary>
    /// <b>THE CANON, TO THE BYTE, AND THE BEDS IN THEIR ORDER.</b>
    ///
    /// <para><b>Proven RED</b> by swapping BASIL and COFFEE in <c>BedPlates</c> (the order assert), and by a
    /// hyphen for the middle dot in the room plate (the verbatim assert).</para>
    /// </summary>
    [Fact]
    public void ThePlatesTheMenuLineAndTheToldLineAreFablesVerbatim()
    {
        Assert.Equal("GARDEN · GROWERS ONLY AFTER SECOND WATCH", HavenGarden.Plate);
        Assert.Equal(
            ["LETTUCE · 14 DAYS", "COFFEE · DO NOT PICK", "BASIL", "TOMATO · STAKED"],
            HavenGarden.BedPlates);
        Assert.Equal("Salad from the garden. Coffee when the coffee is ready.", HavenGarden.MenuLine);
        Assert.Equal("Warm, wet, and quiet. Somebody comes here on purpose.", HavenGarden.FirstVisitLine);

        Assert.Equal(7, HavenGarden.AllProse().Count());
        Assert.Equal(7, HavenGarden.AllProse().Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// <b>THE MENU LINE IS ON EVERY BOARD.</b> Every haven bar with a board carries the one sentence, and a
    /// berth with no board (a counter with no kitchen, or no counter at all) carries none.
    ///
    /// <para><b>Proven RED</b> by <c>MenuLineAt</c> answering null: seven boards without the line.</para>
    /// </summary>
    [Fact]
    public void TheMenuLineIsChalkedOnEveryBoard()
    {
        Assert.Equal(7, TheMenuBoard.AllBoards.Count);
        foreach (TheMenuBoard.Board board in TheMenuBoard.AllBoards)
        {
            Assert.Equal(HavenGarden.MenuLine, HavenGarden.MenuLineAt(board.BodyId));
        }

        Assert.Null(HavenGarden.MenuLineAt(null));
        Assert.Null(HavenGarden.MenuLineAt("no-such-berth"));
    }

    /// <summary><b>NOT ONE WORD SPENDS A RESERVED WORD</b> (worldbuilding-notes §8, and the words beside it)
    /// and not one of them names anybody — a garden that knew whose it was would be the building confirming
    /// something about someone.</summary>
    [Fact]
    public void NotOneWordSpendsAReservedWord()
    {
        string[] reserved =
        [
            "monolith", "reever", "old one", "old ones", "ancient", "alien", "not ours", "not natural",
            "restore", "backup", "kaamos", "minister", "donor", "they were people", "whose", "who made",
        ];

        foreach (string line in HavenGarden.AllProse())
        {
            foreach (string word in reserved)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
