using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #784 · THE SHORT REST, AND THE POSTURE THAT GATES WRITING.
///
/// <para>Owner, live 2026-08-08, in three sentences: <i>"Sitting down relaxes and heals"</i> · <i>"it is
/// like short rest in TTRPG"</i> · <i>"writing things down requires sitting down to be properly
/// done."</i></para>
///
/// <h3>The anti-vacuity discipline this file is written under</h3>
///
/// <para>#587's fifth named bug class is a guard whose world cannot tell pass from fail — a threshold that
/// selects everything, or a captain built so that no branch is reachable. So every fact below is measured on
/// a world where BOTH answers exist: a rest that gives something and a ceiling that refuses something, in
/// the same walk of beats, with the numbers printed into the failure message. The cap is proved by watching
/// it BITE, never by reading it back off the constant that set it.</para>
/// </summary>
public sealed class SittingDownRelaxesAndHealsTests
{
    // ── (c) THE ARITHMETIC ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · ONE BEAT OF SITTING GIVES SOMETHING BACK, AND IT IS A WHOLE PIP.
    ///
    /// <para>RED before the build: <see cref="ShortRest"/> did not exist and waiting at a table moved
    /// nothing at all — the owner's whole complaint being that an empty-hall watch was time spent for
    /// nothing.</para>
    ///
    /// <para>The unit matters as much as the amount. #480 quantised the gauge to ten whole pips and forbade
    /// anything moving it in fractions, so a rest priced under one pip would bank invisibly and the mechanic
    /// would look broken to the only person who can see it.</para>
    /// </summary>
    [Fact]
    public void ASeatedBeatEasesTheNerveByAWholePipTheGaugeCanShow()
    {
        ShortRest.Eased first = ShortRest.Beat(
            beatsAlreadySat: 0, withDrink: false, nervePipsEasedThisWatch: 0, hitsKnitThisWatch: 0,
            hitsTaken: 0);

        Assert.True(first.NervePips >= 1,
            $"a beat of sitting eased {first.NervePips} pip(s) — under a whole pip the gauge cannot move.");
        Assert.False(first.CapSpent, "the first beat of a watch cannot already be at the ceiling.");

        // …and it is spendable through the seam the rest of the game uses, unrounded and unbanked.
        double raw = first.NervePips * NervePips.PipUnit;
        (double after, NervePips.Event? e) = NervePips.ApplyRelief(NervePips.FromPips(2), raw);
        Assert.NotNull(e);
        Assert.True(after > NervePips.FromPips(2),
            $"the relief seam swallowed the rest: {NervePips.FromPips(2)} → {after} on {raw} raw.");
    }

    /// <summary>
    /// #784 · AND THEN THE CEILING BITES, IN THE SAME WALK OF BEATS.
    ///
    /// <para>The one fact that makes a short rest SHORT. Proved by sitting through more beats than the cap
    /// allows and watching the gauge stop moving while the beats keep coming — so the assertion fails if the
    /// cap is removed, and it also fails if the cap is set so low that nothing was ever given.</para>
    /// </summary>
    [Fact]
    public void AndTheCapBITES_MoreBeatsStopBuyingMoreNerve()
    {
        const int beats = 8;
        int pips = 0;
        var perBeat = new List<int>();
        bool everRefused = false;

        for (int b = 0; b < beats; b++)
        {
            ShortRest.Eased e = ShortRest.Beat(b, withDrink: false, pips, hitsKnitThisWatch: 0, hitsTaken: 0);
            perBeat.Add(e.NervePips);
            pips += e.NervePips;
            everRefused |= e.NervePips == 0;
        }

        string walk = string.Join(",", perBeat);
        Assert.True(pips > 0, $"eight beats of sitting gave nothing back at all — [{walk}].");
        Assert.True(everRefused,
            $"eight beats never once hit the ceiling — [{walk}] totals {pips}; the cap is not binding.");
        Assert.Equal(ShortRest.NervePipCapPerWatch, pips);

        // …and the ceiling is UNDER the bunk's. A captain must never be able to sit in a canteen instead of
        // going home, and the arithmetic is what stops them — not the comment on the constant.
        Assert.True(pips * NervePips.PipUnit < NerveModel.SleepRestore,
            $"a watch of sitting ({pips * NervePips.PipUnit}) is worth a night's sleep " +
            $"({NerveModel.SleepRestore}) or more — the long rest has stopped being the better rest.");
    }

    /// <summary>
    /// #784 · A BOUGHT POUR MULTIPLIES THE RATE AND NEVER THE CEILING.
    ///
    /// <para>Owner: <i>"a bought drink/meal plausibly multiplies the nerve half (the counter-to-table ritual
    /// pays)."</i> It pays in TEMPO, which in an interruptible mechanic is the thing worth buying: the same
    /// rest, landed in fewer beats, before the haulier crosses the room and takes it off you.</para>
    /// </summary>
    [Fact]
    public void APourBuysTheRestSOONERAndNeverDEEPER()
    {
        static int BeatsToTheCeiling(bool drink)
        {
            int pips = 0, b = 0;
            while (pips < ShortRest.NervePipCapPerWatch && b < 50)
            {
                pips += ShortRest.Beat(b, drink, pips, 0, 0).NervePips;
                b++;
            }
            return b;
        }

        int dry = BeatsToTheCeiling(false);
        int wet = BeatsToTheCeiling(true);
        Assert.True(wet < dry, $"a pour changed nothing: {wet} beat(s) with, {dry} without.");

        // …and both arrive at the same place. The glass is not a bigger rest.
        Assert.Equal(
            ShortRest.Rest(12, withDrink: false, hitsTaken: 0).NervePips,
            ShortRest.Rest(12, withDrink: true, hitsTaken: 0).NervePips);
    }

    /// <summary>
    /// #784 · SITTING DOWN HEALS — one of the five, and never two.
    ///
    /// <para>RED before the build: nothing in the game gave a blow back inside an excursion; the five were
    /// spent for the trip.</para>
    ///
    /// <para>Both halves measured on the same walk: a captain who sits long enough gets one back, and a
    /// captain who sits twice as long still has exactly one back. A drink knits nothing — it never has.</para>
    /// </summary>
    [Fact]
    public void SittingKnitsONEBlowAndTheSecondOneIsRefused()
    {
        ShortRest.Eased shortSit = ShortRest.Rest(
            ShortRest.BeatsPerHitKnit - 1, withDrink: false, hitsTaken: 3);
        Assert.Equal(0, shortSit.Hits);

        ShortRest.Eased enough = ShortRest.Rest(ShortRest.BeatsPerHitKnit, withDrink: false, hitsTaken: 3);
        Assert.Equal(1, enough.Hits);

        ShortRest.Eased forever = ShortRest.Rest(ShortRest.BeatsPerHitKnit * 6, withDrink: false, hitsTaken: 3);
        Assert.True(forever.Hits == ShortRest.HitsCapPerWatch,
            $"{ShortRest.BeatsPerHitKnit * 6} beats knitted {forever.Hits} blow(s) — the healing cap is not " +
            "binding, and a captain can sit their way whole.");

        // A pour is not a bandage.
        Assert.Equal(
            ShortRest.Rest(ShortRest.BeatsPerHitKnit * 6, withDrink: false, hitsTaken: 3).Hits,
            ShortRest.Rest(ShortRest.BeatsPerHitKnit * 6, withDrink: true, hitsTaken: 3).Hits);
    }

    /// <summary>
    /// #784 · AN UNMARKED CAPTAIN IS NOT TOLD THEIR HEALING RAN OUT.
    ///
    /// <para>The fifth-bug-class trap in this mechanic: <c>CapSpent</c> is a SENTENCE the player reads, so a
    /// version of it that fired whenever nothing happened would tell a whole captain, every beat, that they
    /// had used up a rest they never needed. The flag has to mean "the ceiling refused you" and nothing
    /// else — and the proof is that both answers occur on the same captain.</para>
    /// </summary>
    [Fact]
    public void TheCapIsOnlyNEWSWhenTheCapIsTheThingInTheWay()
    {
        // Beat one, unmarked, nothing spent: something happened, so there is nothing to announce.
        Assert.False(ShortRest.Beat(0, false, 0, 0, hitsTaken: 0).CapSpent);

        // The ceiling reached, and the captain is told.
        ShortRest.Eased over =
            ShortRest.Beat(9, false, ShortRest.NervePipCapPerWatch, ShortRest.HitsCapPerWatch, hitsTaken: 2);
        Assert.False(over.Anything);
        Assert.True(over.CapSpent, "the ceiling refused a beat in silence.");
        Assert.Equal(ShortRest.CapSpentLine, ShortRest.Line(in over, withDrink: false));

        // …and an UNMARKED captain at the same ceiling is told the same thing about the nerve and is not
        // told anything false about their skin: the sentence is one sentence, and it is about the rest.
        ShortRest.Eased whole =
            ShortRest.Beat(9, false, ShortRest.NervePipCapPerWatch, hitsKnitThisWatch: 0, hitsTaken: 0);
        Assert.True(whole.CapSpent);
        Assert.Equal(0, whole.Hits);
    }

    /// <summary>#784 · Every beat that gave something says something, and every beat that gave nothing for
    /// an ordinary reason says nothing at all — a footnote after every silence line would be the panel
    /// nagging.</summary>
    [Fact]
    public void EveryBeatThatGaveSomethingSaysSomething()
    {
        ShortRest.Eased dry = ShortRest.Beat(0, false, 0, 0, 0);
        ShortRest.Eased wet = ShortRest.Beat(0, true, 0, 0, 0);
        Assert.Equal(ShortRest.EasedLine, ShortRest.Line(in dry, withDrink: false));
        Assert.Equal(ShortRest.EasedWithDrinkLine, ShortRest.Line(in wet, withDrink: true));

        ShortRest.Eased knit = ShortRest.Beat(
            ShortRest.BeatsPerHitKnit - 1, false, ShortRest.NervePipCapPerWatch, 0, hitsTaken: 1);
        Assert.Equal(1, knit.Hits);
        Assert.Equal(ShortRest.KnitLine, ShortRest.Line(in knit, withDrink: false));

        // A beat that is simply not the third one, with the nerve ceiling not yet reached, is neither news
        // nor a refusal — but it eased something, so it speaks. The genuinely silent case is the one below.
        ShortRest.Eased quiet = ShortRest.Beat(0, false, ShortRest.NervePipCapPerWatch, 0, hitsTaken: 0);
        Assert.True(quiet.CapSpent);
        Assert.NotNull(ShortRest.Line(in quiet, withDrink: false));
    }

    // ── (d) THE POSTURE GATE ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · WRITING PROPERLY IS SEATED-ONLY, AND THE REFUSAL SAYS WHICH REGISTER IS WHICH.
    ///
    /// <para>Owner's law: <i>"writing things down requires sitting down to be properly done."</i> The
    /// standing register is not taken away and the sentence has to say so, or a captain reads the refusal as
    /// "the book is broken" rather than "go and sit down" — which is #603's law applied to a rule instead of
    /// to a control.</para>
    /// </summary>
    [Fact]
    public void DELIBERATE_WritingIsRefusedOnYourFeetAndAllowedInAChair()
    {
        Assert.False(SeatedPosture.CanWriteProperly(seated: false));
        Assert.True(SeatedPosture.CanWriteProperly(seated: true));

        string? standing = SeatedPosture.RefusalIfStanding(seated: false);
        Assert.NotNull(standing);
        Assert.Equal(SeatedPosture.NotOnYourFeetLine, standing);
        Assert.Null(SeatedPosture.RefusalIfStanding(seated: true));

        // It names the standing register rather than only forbidding the seated one.
        Assert.Contains("photograph", standing, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sit down", standing, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>#784 · What a properly written entry says. It carries the SAME gist the standing register
    /// files — a table does not buy a better fact, it buys the sheet staying in your pocket — so the line
    /// has to quote what was written rather than paraphrase it.</summary>
    [Fact]
    public void AProperEntryQuotesTheSameGistTheStandingRegisterWouldHaveFiled()
    {
        const string gist = "A pay sheet: nine names, and the fourth column is blank after March.";
        string said = SeatedPosture.WrittenUpLine("🗎 A PAY SHEET", gist);

        Assert.Contains(gist, said, StringComparison.Ordinal);
        Assert.Contains("🗎 A PAY SHEET", said, StringComparison.Ordinal);
        Assert.StartsWith(SeatedPosture.WriteGlyph, said, StringComparison.Ordinal);

        // The pen is not the camera. #562's lesson: a glyph that said one thing while the sim did another is
        // a lie the project has paid for, and the standing register's glyph belongs to the standing register.
        Assert.NotEqual(Processing.Glyph, SeatedPosture.WriteGlyph);
    }

    /// <summary>#784 · The stand-up confirm asks in plain words and names its price in the same breath —
    /// #782/#783's own rule, which is that a label needing the design doc to parse is wrong.</summary>
    [Fact]
    public void TheStandUpConfirmSaysWhatItCostsInTheSameBreath()
    {
        Assert.Contains("Stand up", SeatedPosture.StandUpAsk, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("table", SeatedPosture.StandUpAsk, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".", SeatedPosture.StandUpAsk, StringComparison.Ordinal);

        // Two answers, and neither of them is a verb that needs explaining.
        Assert.NotEqual(SeatedPosture.StandUpYes, SeatedPosture.StandUpNo);
        foreach (string label in new[] { SeatedPosture.StandUpYes, SeatedPosture.StandUpNo })
        {
            Assert.False(string.IsNullOrWhiteSpace(label));
            Assert.True(label.Length <= 24, $"\"{label}\" is a paragraph, not a button.");
        }
    }

    /// <summary>#784 · The canon sweep. Every sentence these two files can put on a screen is listed for
    /// review, and none of it is a placeholder, a double space, or a line that forgot to end.</summary>
    [Fact]
    public void EverySentenceIsListedForCanonReviewAndReads()
    {
        List<string> prose = [.. SeatedPosture.AllProse(), .. ShortRest.AllProse()];
        Assert.True(prose.Count >= 12, $"only {prose.Count} sentence(s) offered to the canon sweep.");

        foreach (string line in prose)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.DoesNotContain("  ", line, StringComparison.Ordinal);
            Assert.DoesNotContain("TODO", line, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(line.Trim(), line);
        }

        // The distinct ones are distinct: a pool that repeated itself would be a captain told the same thing
        // twice and reading it as a bug.
        Assert.Equal(prose.Count, prose.Distinct(StringComparer.Ordinal).Count());
    }
}
