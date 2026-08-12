namespace SpaceSails.Core.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

/// <summary>
/// #784 phase two · THE CUSTOMER LINE IS AN INSTRUMENT, AND THE PRIVACY LADDER IS A LAW.
///
/// <para>Owner, live 2026-08-08 night: <i>"maybe the HUD could have place for our current state as customer
/// of the restaurant, like we have really superb UI for the air use."</i> And, on the venue:
/// <i>"the gumshoe does NOT organize papers at the bar… that is the place I want to process inventory"</i>
/// (from a cabinet, at his own table).</para>
///
/// <h3>The bug class this file is aimed at</h3>
///
/// <para>TWO CLOCKS (#740/#762). The DEAD AIR card did its own arithmetic on the raw air budget and printed
/// "21 min 01 s" at a captain whose gauge read AIR 8h09 — same tank, same instant, off by a factor of
/// twenty-three. A seated strip that computed its own idea of the pour window, or its own idea of how full
/// the hall is, would be that fault with a chair under it. So every assertion below sweeps the REAL domain
/// (every watch the rota has, the whole pip range from zero to the ceiling and past it, the pour window
/// second by second) and compares the printed figure to the value the mechanic itself runs on.</para>
///
/// <h3>…and the fifth bug class, paid up front</h3>
///
/// <para>A threshold that selects everything, or nothing, is a guard that asserts nothing. The room clause
/// has three words and there are eight watches; the test asserts that all three are REACHED across the
/// watches the game actually has, so a constant tuned into uselessness fails here rather than in a
/// playtest.</para>
/// </summary>
public class TheSeatedStripIsAnInstrumentTests
{
    /// <summary>Every watch the rota has — the real domain, asked of Core rather than typed.</summary>
    private static IEnumerable<long> EveryWatch() =>
        Enumerable.Range(0, CanteenRegulars.WatchFill.Count).Select(i => (long)i);

    // ── (b) THE CUSTOMER LINE MIRRORS THE MECHANIC, FIGURE FOR FIGURE ─────────────────────────────────

    /// <summary>
    /// #784 · THE REST CLAUSE IS SHORTREST'S OWN LEDGER, printed. Swept over the whole pip range including
    /// past the ceiling, because a captain who has spent their rest is exactly who the clause has to be
    /// right for.
    /// </summary>
    [Fact]
    public void THE_REST_CLAUSE_IsTheShortRestLedgerAndNothingElse()
    {
        int cap = ShortRest.NervePipCapPerWatch;
        Assert.True(cap > 0, "a ceiling of zero would make every reading below vacuous.");

        for (int eased = 0; eased <= cap + 2; eased++)
        {
            string clause = SeatedHud.RestClause(eased, cap);

            // The figure is the ledger's own, both halves of the fraction.
            Assert.Contains($"REST {eased}/{cap} pips", clause, StringComparison.Ordinal);

            // …and the ceiling is only announced when it is actually the thing in the way — the same
            // discipline ShortRest.Beat keeps about CapSpent.
            bool spent = eased >= cap;
            Assert.Equal(spent, clause.Contains("that is all a chair gives", StringComparison.Ordinal));
        }

        // A negative ledger is a bug somewhere else, and this line must not turn it into a negative figure
        // on the captain's screen.
        Assert.Contains("REST 0/", SeatedHud.RestClause(-3, cap), StringComparison.Ordinal);
    }

    /// <summary>
    /// #784 · THE POUR CLAUSE COUNTS THE SAME WINDOW THE REST ENGINE DOUBLES OFF, and quotes the multiplier
    /// ShortRest actually applies rather than a number typed into a sentence.
    /// </summary>
    [Fact]
    public void THE_POUR_CLAUSE_CountsTheWindowAndQuotesTheRealMultiplier()
    {
        // No pour is a stated fact, not a blank: the strip has to be readable when the answer is nothing.
        string dry = SeatedHud.PourClause(null);
        Assert.Contains("NO POUR", dry, StringComparison.Ordinal);
        Assert.DoesNotContain("POUR in hand", dry, StringComparison.Ordinal);
        Assert.Equal(dry, SeatedHud.PourClause(0));
        Assert.Equal(dry, SeatedHud.PourClause(-12));

        // The multiplier on the line is ShortRest's own. A "2×" typed here would go stale the first time the
        // owner tunes the ritual.
        Assert.Contains(
            $"{ShortRest.DrinkNerveMultiplier}×", SeatedHud.PourClause(120), StringComparison.Ordinal);

        // And the minutes are the window's minutes, swept second by second across the whole live range.
        // Ceiling, not truncation: a glass with fifty-nine seconds left is not "0m of cold".
        for (double left = 1; left <= 600; left += 1)
        {
            string clause = SeatedHud.PourClause(left);
            if (left < 60)
            {
                Assert.Contains("under a minute", clause, StringComparison.Ordinal);
                continue;
            }
            int minutes = (int)Math.Ceiling(left / 60.0);
            Assert.Contains($"{minutes}m of cold left", clause, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// #784 · THE ROOM CLAUSE IS SITTINGALONE.FILL, and it is the same number the approach roll is scaled
    /// by — so "the hall is heaving" and "somebody is more likely to come over" are one fact said twice.
    ///
    /// <para>Swept over EVERY watch the rota has, and the three words are all required to be reached: a
    /// threshold nothing selects is a clause that never says anything.</para>
    /// </summary>
    [Fact]
    public void THE_ROOM_CLAUSE_TracksTheSameFillTheApproachOddsAreScaledBy()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (long watch in EveryWatch())
        {
            double fill = SittingAlone.Fill(watch);
            string clause = SeatedHud.RoomClause(SeatedHud.Seat.HallTable, fill);
            seen.Add(clause);

            string expected = fill >= SeatedHud.HeavingAt ? "the hall is heaving"
                : fill >= SittingAlone.BusyAt ? "the hall is working"
                : "the hall is thin";
            Assert.Equal(expected, clause);

            // The word and the odds move together: a heavier room is never fewer faces of the die.
            int faces = SittingAlone.FacesThatBringSomebody(watch);
            Assert.True(faces >= 1 && faces <= SittingAlone.Faces);
            if (clause == "the hall is heaving")
            {
                Assert.True(faces >= SittingAlone.FacesThatBringSomebody(QuietestWatch()),
                    "the strip calls the hall heaving on a watch the approach roll thinks is emptier than " +
                    "the quietest one — the word and the odds have come apart.");
            }
        }

        Assert.True(seen.Count >= 3,
            "the room clause never reaches all three of its words across the eight watches the game has — " +
            "a threshold that selects everything, or nothing, is a clause that asserts nothing. Saw: "
            + string.Join(" / ", seen));

        // A CABINET is not a fill at all. The door is what you paid for, nobody comes (SomebodyComes refuses
        // on a quiet top), and the strip says the thing that is true about the room you are IN.
        foreach (long watch in EveryWatch())
        {
            string cabinet = SeatedHud.RoomClause(SeatedHud.Seat.Cabinet, SittingAlone.Fill(watch));
            Assert.Contains("the door is shut", cabinet, StringComparison.Ordinal);
            Assert.False(SittingAlone.SomebodyComes("test-body", -1, 0, watch, 0, quiet: true),
                "the strip says nobody is coming while the room says somebody might.");
        }
    }

    private static long QuietestWatch()
    {
        long quietest = 0;
        double least = double.MaxValue;
        foreach (long watch in EveryWatch())
        {
            if (SittingAlone.Fill(watch) < least)
            {
                least = SittingAlone.Fill(watch);
                quietest = watch;
            }
        }
        return quietest;
    }

    /// <summary>#784 · The whole line is the four clauses and nothing else — no fifth figure invented in the
    /// join, and every clause present in every combination the game can produce.</summary>
    [Fact]
    public void THE_CUSTOMER_LINE_IsTheFourClausesJoinedAndNothingMore()
    {
        foreach (SeatedHud.Seat seat in SeatedSpread.Ladder)
        {
            foreach (long watch in EveryWatch())
            {
                foreach (double? pour in new double?[] { null, 30, 130, 299 })
                {
                    for (int pips = 0; pips <= ShortRest.NervePipCapPerWatch; pips++)
                    {
                        double fill = SittingAlone.Fill(watch);
                        string line = SeatedHud.CustomerLine(
                            seat, pour, pips, ShortRest.NervePipCapPerWatch, fill);

                        Assert.Equal(4, line.Split(SeatedHud.Join).Length);
                        Assert.StartsWith($"{SeatedHud.Glyph} {SeatedHud.SeatLabel(seat)}", line,
                            StringComparison.Ordinal);
                        Assert.Contains(SeatedHud.PourClause(pour), line, StringComparison.Ordinal);
                        Assert.Contains(
                            SeatedHud.RestClause(pips, ShortRest.NervePipCapPerWatch), line,
                            StringComparison.Ordinal);
                        Assert.EndsWith(SeatedHud.RoomClause(seat, fill), line, StringComparison.Ordinal);
                    }
                }
            }
        }
    }

    // ── (e) PRIVACY GATES THE SPREAD, BOTH DIRECTIONS ─────────────────────────────────────────────────

    /// <summary>
    /// #784/#793 · THE LADDER ADMITS AND THE LADDER REFUSES — and the guard walks every rung in both
    /// states, so neither answer is reached by accident.
    ///
    /// <para>Owner's ordering, priced from the 15× anchor: <i>cabinet (guaranteed) &gt; empty park bench
    /// (conditional) &gt; hall table &gt; bar desk.</i> The two ends are absolute — the cabinet always yes,
    /// the counter always no — and the two middle rungs turn on having the seat to yourself.</para>
    /// </summary>
    [Fact]
    public void PRIVACY_AdmitsTheQualifiedSeatAndRefusesTheBarDesk()
    {
        // #821 · THE LOCKED CUBICLE: yes, unconditionally, and it is the TOP of the ladder now. Owner, in
        // the park: "the cubicle is the one place the spread ([I]) is ALWAYS allowed — smaller than a bench,
        // more private than an office. Half a bench is not a desk; a locked cubicle absolutely is :-D"
        Assert.True(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.LockedCubicle, alone: true));
        Assert.True(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.LockedCubicle, alone: false));
        Assert.Null(SeatedSpread.RefusalAt(SeatedHud.Seat.LockedCubicle, alone: false));

        // THE CABINET: yes, unconditionally, in both states. It is the owner's ruling in one assertion.
        Assert.True(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.Cabinet, alone: true));
        Assert.True(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.Cabinet, alone: false));
        Assert.Null(SeatedSpread.RefusalAt(SeatedHud.Seat.Cabinet, alone: false));

        // THE BAR DESK: no, unconditionally, in both states — the gumshoe rule.
        Assert.False(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.BarStool, alone: true));
        Assert.False(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.BarStool, alone: false));
        Assert.Equal(SeatedSpread.NotAtTheBarLine, SeatedSpread.RefusalAt(SeatedHud.Seat.BarStool, true));

        // THE TWO CONDITIONAL RUNGS: the whole table, or the whole bench, or nothing.
        foreach (SeatedHud.Seat seat in new[] { SeatedHud.Seat.HallTable, SeatedHud.Seat.ParkBench })
        {
            Assert.True(SeatedSpread.CanSpreadTheCase(seat, alone: true));
            Assert.False(SeatedSpread.CanSpreadTheCase(seat, alone: false));
            Assert.Null(SeatedSpread.RefusalAt(seat, alone: true));
            Assert.NotNull(SeatedSpread.RefusalAt(seat, alone: false));
        }

        // EVERY rung answers, and every refusal is a sentence rather than an absence (#603). Written as a
        // sweep over the enum so a fifth seat added tomorrow cannot ship without an answer and a reason.
        foreach (SeatedHud.Seat seat in SeatedSpread.Ladder)
        {
            foreach (bool alone in new[] { true, false })
            {
                string? why = SeatedSpread.RefusalAt(seat, alone);
                Assert.Equal(SeatedSpread.CanSpreadTheCase(seat, alone), why is null);
                if (why is not null)
                {
                    Assert.True(why.Length > 30, $"{seat}/{alone} refuses without explaining itself.");
                    Assert.EndsWith(".", why, StringComparison.Ordinal);
                }
            }
        }

        // …and the ladder is ORDERED private-end-first, which is the owner's own ranking and the thing the
        // 15× law will hang numbers on. #821 · The private end moved up one: a locked cubicle is more
        // private than a room you rented, because the cabinet's privacy is a door somebody else keeps and
        // this one is a catch in your own hand.
        Assert.Equal(SeatedHud.Seat.LockedCubicle, SeatedSpread.Ladder[0]);
        Assert.Equal(SeatedHud.Seat.Cabinet, SeatedSpread.Ladder[1]);
        Assert.Equal(SeatedHud.Seat.BarStool, SeatedSpread.Ladder[^1]);
    }

    // ── (d, Core half) THE DIG IS ONE LANGUAGE, AND IT SAYS WHICH DIG IT IS ───────────────────────────

    /// <summary>
    /// #784 · THE SEATED WRITE-UP RUNS ON THE ONE PROCESSING CLOCK AND WEARS THE PEN.
    ///
    /// <para>Owner: <i>"The inventory processing should have like a timer progress bar like the digging
    /// has… we are digging for info and understanding."</i> So there is no second duration, no second
    /// fraction and no second glyph decision — <see cref="Processing"/> owns all three, and the pen is
    /// <see cref="SeatedPosture.WriteGlyph"/> so the control, the bar and the book entry are one mark.</para>
    /// </summary>
    [Fact]
    public void THE_SEATED_DIG_IsTheOneProcessingClockWearingThePen()
    {
        Assert.Equal(SeatedPosture.WriteGlyph, Processing.GlyphFor(Processing.Work.Write));
        Assert.Equal(Processing.Glyph, Processing.GlyphFor(Processing.Work.File));
        Assert.Equal(Processing.Glyph, Processing.GlyphFor(Processing.Work.Read));
        Assert.NotEqual(Processing.Glyph, SeatedPosture.WriteGlyph);

        // Every sentence the seated register can print names the pen and never the camera — a glyph that
        // said "photographing" over a sim that was writing is the class of lie #562 was filed on.
        string what = "the movement order";
        foreach (Processing.Interruption why in Enum.GetValues<Processing.Interruption>())
        {
            string line = Processing.AbandonedLine(Processing.Work.Write, what, why);
            Assert.StartsWith(SeatedPosture.WriteGlyph, line, StringComparison.Ordinal);
            Assert.DoesNotContain(Processing.Glyph, line, StringComparison.Ordinal);
            Assert.Contains(what, line, StringComparison.Ordinal);
        }

        // The two interruptions a seated captain actually meets get their own sentences, because they are
        // two different events: you stood up, or somebody sat down opposite you.
        Assert.NotEqual(
            Processing.AbandonedLine(Processing.Work.Write, what, Processing.Interruption.StoodUp),
            Processing.AbandonedLine(Processing.Work.Write, what, Processing.Interruption.CompanyArrived));

        // …and so does every OTHER sentence the register can print. Swept over all four entry points at
        // once, because the one that was missed first time was the one nobody thinks about: a second press
        // on a running hold, which used to answer "you are photographing it" over a pair of hands copying
        // into a book.
        foreach (string line in new[]
        {
            Processing.StartLine(Processing.Work.Write, what, Processing.SecondsPerDocument),
            Processing.HoldLine(Processing.Work.Write, what, 9),
            Processing.AlreadyBusyLine(Processing.Work.Write, what),
            Processing.AbandonedLine(Processing.Work.Write, what, Processing.Interruption.StoodUp),
        })
        {
            Assert.StartsWith(SeatedPosture.WriteGlyph, line, StringComparison.Ordinal);
            Assert.DoesNotContain(Processing.Glyph, line, StringComparison.Ordinal);
            Assert.DoesNotContain("photograph", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("walk away", line, StringComparison.OrdinalIgnoreCase);
        }

        // The start line names the seconds, so the price of the press is known before the press.
        Assert.Contains("20 seconds", Processing.StartLine(Processing.Work.Write, what, 20), StringComparison.Ordinal);
        Assert.DoesNotContain("seconds", Processing.StartLine(Processing.Work.Write, what, 0), StringComparison.Ordinal);

        // …and the clock itself is the ONE clock: no member of Work gets a duration of its own.
        Assert.Equal(0.5, Processing.Fraction(10, Processing.SecondsPerDocument), 6);
        Assert.False(Processing.Done(Processing.SecondsPerDocument - 0.1, Processing.SecondsPerDocument));
        Assert.True(Processing.Done(Processing.SecondsPerDocument, Processing.SecondsPerDocument));
    }

    // ── THE ENTRY DOES NOT CONTRADICT THE SENTENCE THAT FILED IT ─────────────────────────────────────

    /// <summary>
    /// #784/#562 · WHAT THE BOOK SAYS HAPPENED TO THE SHEET IS WHAT HAPPENED TO THE SHEET.
    ///
    /// <para><b>Found by looking at it.</b> Booted <c>?spread=1</c>, dug a manifest out, and read the docked
    /// strip: <i>"…copy it into the book in your own hand. The sheet goes back in the sleeve. 📋 shipping
    /// manifest, torn — a description, <b>read and left on the floor of B1</b>."</i> One line, two opposite
    /// claims about the same piece of paper. It shipped in #788's instant write and was invisible there —
    /// the entry was filed once into a book nobody had open while the outcome pulsed and faded. The strip
    /// put both halves side by side and it read wrong on sight.</para>
    ///
    /// <para>The FACT is deliberately still one fact (#784's ruling: a table does not buy a better gist, it
    /// buys the sheet staying in your pocket). Only the disposition clause differs.</para>
    /// </summary>
    [Fact]
    public void THE_KEPT_ENTRY_NeverSaysTheSheetWasLeftBehind()
    {
        const string where = "on the floor of B1";

        foreach (Satchel.Kind kind in new[] { Satchel.Kind.Paper, Satchel.Kind.Dirt })
        {
            var item = new Satchel.Item(kind, "manifest-7");

            string left = LeftBehind.GistOf(item, where)!;
            string kept = LeftBehind.GistOf(item, where, kept: true)!;

            // The standing register is untouched — #696's leave verb still says what it has always said.
            Assert.Contains("left", left, StringComparison.OrdinalIgnoreCase);

            // …and the seated one never claims the paper went anywhere.
            Assert.DoesNotContain("left " + where, kept, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("what you put down", kept, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(where, kept, StringComparison.Ordinal);
            Assert.NotEqual(left, kept);

            // The whole line the strip prints has to agree with itself, end to end: it promises the sleeve
            // and then quotes the entry, and the entry must not take the promise back.
            string said = SeatedPosture.WrittenUpLine("the manifest", kept);
            Assert.Contains("back in the sleeve", said, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("and left", said, StringComparison.OrdinalIgnoreCase);
        }

        // Both dispositions still answer for the same kinds and refuse the same ones — a flag that changed
        // WHICH things have a gist would be a second answer to "is this a document".
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            var item = new Satchel.Item(kind, "x");
            Assert.Equal(
                LeftBehind.GistOf(item, where) is null,
                LeftBehind.GistOf(item, where, kept: true) is null);
        }
    }

    // ── THE CANON SWEEP ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Every new sentence is listed for the canon review, and none of them is empty or a
    /// placeholder. The house rule that makes a prose review possible at all.</summary>
    [Fact]
    public void EVERY_NEW_SENTENCE_IsListedForTheCanonSweep()
    {
        foreach (string line in SeatedHud.AllProse().Concat(SeatedSpread.AllProse()))
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.DoesNotContain("TODO", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lorem", line, StringComparison.OrdinalIgnoreCase);
        }

        // The two ends of the ladder and the two ends of the rest are all in the sweep, so a canon pass
        // reads what a player reads rather than a sample of it.
        List<string> sweep = [.. SeatedHud.AllProse(), .. SeatedSpread.AllProse()];
        Assert.Contains(SeatedSpread.NotAtTheBarLine, sweep);
        Assert.Contains(SeatedHud.RoomClause(SeatedHud.Seat.Cabinet, 0), sweep);
        Assert.Contains(
            SeatedHud.RestClause(ShortRest.NervePipCapPerWatch, ShortRest.NervePipCapPerWatch), sweep);
    }
}
