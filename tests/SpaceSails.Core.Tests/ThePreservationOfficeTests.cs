using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1332 C · <b>THE PRESERVATION OFFICE — THE WORDS, THE HOURS AND THE SHEET.</b> Fable canon, verbatim; the clerk's
/// watch is one in four, seeded on the run and pure; the sheet reads as papers read away from their room and files
/// under the Authority and the haven; nothing spends a reserved word and nothing names anybody.
/// </summary>
public sealed class ThePreservationOfficeTests
{
    /// <summary>
    /// <b>THE CANON, TO THE BYTE, AND NOTHING ELSE.</b> Retyped from the brief so the guard has a source the
    /// implementation cannot move.
    ///
    /// <para><b>Proven RED</b> by a hyphen for the plate's middle dot, and by an eighth string in
    /// <c>AllProse</c>.</para>
    /// </summary>
    [Fact]
    public void EveryLineIsFablesVerbatim()
    {
        Assert.Equal("PRESERVATION · BY APPOINTMENT", PreservationOffice.DoorPlate);
        Assert.Equal(
            "Appointments are made by the office. The office does not make appointments.",
            PreservationOffice.ShutLine);
        Assert.Equal(
            "A door with the cost centre on it. Somebody pays the rent on Preservation.",
            PreservationOffice.PlateReadLine);
        Assert.Equal("Clerk", PreservationOffice.ClerkPlate);
        Assert.Equal(
            "Warm still. A chair pushed back the way a chair is pushed back by somebody who means to return.",
            PreservationOffice.WarmStillLine);
        Assert.Equal("preservation-disbursements", PreservationOffice.SheetId);
        Assert.Equal("A disbursement sheet, quarter to date", PreservationOffice.SheetTitle);
        Assert.Equal(
            "Perimeter rail. Site watch. Structural remediation. Editorial services. Charged to Preservation.",
            PreservationOffice.SheetDocument);
        Assert.Equal("📍", PreservationOffice.PlateReadGlyph);

        Assert.Equal(7, PreservationOffice.AllProse().Count());
        Assert.Equal(7, PreservationOffice.AllProse().Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("ringside-exchange", PreservationOffice.HavenId);
    }

    /// <summary>
    /// <b>NOT ONE WORD SPENDS A RESERVED WORD, AND NOT ONE NAMES ANYBODY.</b> The enforcer is an office (#1074): no
    /// regular's name, no digit, no amount, and the only mid-sentence capital in the prose is the cost centre.
    ///
    /// <para><b>Proven RED</b> by a figure in the sheet ("…Charged to Preservation, 900 cr.").</para>
    /// </summary>
    [Fact]
    public void NotOneWordSpendsAReservedWordOrNamesAnybody()
    {
        string[] reserved =
        [
            "monolith", "reever", "old one", "old ones", "ancient", "alien", "not ours", "not natural",
            "restore", "backup", "kaamos", "minister", "donor", "they were people", "whose", "who made",
        ];

        foreach (string line in PreservationOffice.AllProse())
        {
            foreach (string word in reserved)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }

            foreach (string regular in PatronRota.Roster)
            {
                Assert.DoesNotContain(regular, line, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain(line, char.IsDigit);
        }
    }

    /// <summary>
    /// <b>ONE WATCH IN FOUR, SEEDED ON THE RUN, AND PURE.</b> Over many runs and many watches the door stands ajar on
    /// exactly one watch of every four consecutive ones; the same run answers the same twice; and the offset really
    /// moves with the run — a clock that ignored its seed would put every run's clerk on the same watch.
    ///
    /// <para><b>Proven RED</b> by <c>OffsetFor</c> returning 0 (the offsets collapse to one), and by
    /// <c>IsTheClerksWatch</c> answering <c>watch % 2 == 0</c> (two in four).</para>
    /// </summary>
    [Fact]
    public void TheClerksWatchIsOneInFourSeededOnTheRunAndPure()
    {
        var offsets = new HashSet<int>();
        for (int run = 0; run < 64; run++)
        {
            ulong seed = DiceRule.Seed(0UL, $"thread-{run}");
            offsets.Add(PreservationOffice.OffsetFor(seed));
            for (long start = -8; start < 40; start++)
            {
                int ajar = 0;
                for (long w = start; w < start + PreservationOffice.WatchesPerTurn; w++)
                {
                    bool once = PreservationOffice.IsTheClerksWatch(seed, w);
                    Assert.Equal(once, PreservationOffice.IsTheClerksWatch(seed, w));
                    ajar += once ? 1 : 0;
                }

                Assert.Equal(1, ajar);
            }
        }

        Assert.Equal(PreservationOffice.WatchesPerTurn, offsets.Count);
    }

    /// <summary>
    /// <b>THE CLERK'S WALK IS A CLOCK.</b> Nothing before he sets off, the fraction gone by while he walks, nothing
    /// once he is at the car; and he sets off on his watch's first second.
    ///
    /// <para><b>Proven RED</b> by dropping the end clause (he walks for ever).</para>
    /// </summary>
    [Fact]
    public void TheClerksWalkIsAClock()
    {
        Assert.Null(PreservationOffice.Along(100, 10, 99.9));
        Assert.Equal(0.0, PreservationOffice.Along(100, 10, 100));
        Assert.Equal(0.5, PreservationOffice.Along(100, 10, 105)!.Value, 9);
        Assert.Null(PreservationOffice.Along(100, 10, 110));
        Assert.Null(PreservationOffice.Along(100, 0, 100));
        Assert.Equal(7 * PatronRota.WatchSeconds, PreservationOffice.SetsOffAt(7));
    }

    /// <summary>
    /// <b>THE SHEET IS A PAPER, READ THE SAME IN EVERY DOOR.</b> An authored sheet (so it is never torn into pages
    /// and never reads as a clue), titled and worded as the canon wrote it by the sleeve, the glance and the card.
    ///
    /// <para><b>Proven RED</b> by dropping the sheet from <c>FieldClue.IsAuthored</c> (it reads as a clue), and
    /// by dropping the <c>CarriedObject.PaperReveal</c> arm (the card shows a certainty).</para>
    /// </summary>
    [Fact]
    public void TheSheetIsAnAuthoredPaperReadTheSameEverywhere()
    {
        string id = PreservationOffice.SheetId;
        Assert.True(PreservationOffice.IsTheSheet(id));
        Assert.False(PreservationOffice.IsTheSheet(id + "x"));
        Assert.Equal(new Satchel.Item(Satchel.Kind.Paper, id), PreservationOffice.TheSheet);

        Assert.True(FieldClue.IsAuthored(id));
        Assert.False(FieldClue.ReadsAsAClue(id));
        Assert.Equal(PreservationOffice.SheetTitle, FieldClue.Title(id));
        Assert.Equal(PreservationOffice.SheetDocument, FieldClue.Document(id));

        CarriedObject.Reveal card = CarriedObject.PaperReveal(id);
        Assert.Equal(PreservationOffice.SheetTitle, card.Label);
        Assert.Equal(PreservationOffice.SheetDocument, card.Story);
        Assert.Equal("", card.ArtUrl);
    }

    /// <summary>
    /// <b>FILED UNDER THE AUTHORITY AND RINGSIDE EXCHANGE, AND NOTHING ELSE</b> — #1074 beat 3's own door, so the
    /// plate and the sheet stack under the same office as the rail, the rota, the pour and the receipt.
    ///
    /// <para><b>Proven RED</b> by a third subject (the cost centre as an office of its own).</para>
    /// </summary>
    [Fact]
    public void TheBookFilesItUnderTheAuthorityAndTheHaven()
    {
        string subjects = PreservationOffice.SubjectsFor("Ringside Exchange");
        Assert.Equal(MoneyTrail.SubjectsFor("Ringside Exchange"), subjects);
        Assert.Equal(
            CaseSubjects.Line(MoneyTrail.TheOffice, MoneyTrail.TheSite("Ringside Exchange")),
            subjects);
    }

    /// <summary>
    /// <b>THE DEV LATCH READS ITS OWN KEY AND NOTHING ELSE.</b>
    ///
    /// <para><b>Proven RED</b> by matching <c>office=</c> anywhere in the query (a <c>headoffice=1</c> would force
    /// the door).</para>
    /// </summary>
    [Fact]
    public void TheDevLatchReadsItsOwnKey()
    {
        const string at = "https://localhost/map?dock=ringside-exchange&ashore=1&havenfloor=-1";
        Assert.Equal(PreservationOffice.Cheat.None, PreservationOffice.CheatIn(at));
        Assert.Equal(PreservationOffice.Cheat.Shut, PreservationOffice.CheatIn(at + "&office=shut"));
        Assert.Equal(PreservationOffice.Cheat.Open, PreservationOffice.CheatIn(at + "&office=open"));
        Assert.Equal(PreservationOffice.Cheat.None, PreservationOffice.CheatIn(at + "&headoffice=1"));
        Assert.Equal(PreservationOffice.Cheat.None, PreservationOffice.CheatIn(null));
    }
}
