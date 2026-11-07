using System;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// 11-07 · Fable's authored lines, batch A (R1-F33, R1-F34, R1-F44, U04-F22, U06-F12): each string is pinned
/// byte for byte, and each behavioural change is a test that goes RED when the change is reverted.
/// </summary>
public sealed class FablesLinesBatchATests
{
    // ── R1-F34 · the scuttle caption ──────────────────────────────────────────────────────────────────

    [Fact]
    public void TheScuttleHasItsOwnCaptionOnTheSurfaceCard()
    {
        Assert.Equal(
            "\"…nothing came for you. You set it, and then you stayed.\"",
            DeathNarration.SurfaceCaption(DeathCause.Scuttled, nerveRanOut: true));
        Assert.Equal(
            DeathNarration.SurfaceCaption(DeathCause.Scuttled, nerveRanOut: true),
            DeathNarration.SurfaceCaption(DeathCause.Scuttled, nerveRanOut: false));
        Assert.DoesNotContain("nerve", DeathNarration.SurfaceCaption(DeathCause.Scuttled, true), StringComparison.Ordinal);
    }

    // ── R1-F44 · CROSSING THE VOID counts days under a year ───────────────────────────────────────────

    [Fact]
    public void AThirtyEightDayCrossingReadsDayNOfThirtyEight()
    {
        double seconds = CyclerWindow.CrossingSeconds;
        Assert.True(LongHaul.VoidInDays(seconds));
        int total = LongHaul.VoidUnits(seconds);
        Assert.Equal(38, total);
        Assert.Equal("day 12 of 38", LongHaul.VoidSpanLine(12, total, LongHaul.VoidInDays(seconds)));
        Assert.Equal("day 1 of 38", LongHaul.VoidSpanLine(0, total, true));
        Assert.Equal("day 38 of 38", LongHaul.VoidSpanLine(99, total, true));
    }

    [Fact]
    public void ACrossingOfAYearOrMoreStillReadsYears()
    {
        double decade = 3655.0 * 86400.0;
        Assert.False(LongHaul.VoidInDays(decade));
        Assert.False(LongHaul.VoidInDays(365.0 * 86400.0));
        Assert.Equal(10, LongHaul.VoidUnits(decade));
        Assert.Equal("year 3 of 10", LongHaul.VoidSpanLine(3, 10, inDays: false));
        Assert.Equal(LongHaul.VoidYearLine(3, 10), LongHaul.VoidSpanLine(3, 10, inDays: false));
    }

    [Fact]
    public void TheParrotCountsDaysUnderAYear()
    {
        Assert.Equal("🦜 the parrot counts the days and loses count somewhere past thirty", LongHaul.VoidFlavor(38, inDays: true));
        Assert.Equal(
            "🦜 the parrot sleeps through most of it, and never once asks where the engines went",
            LongHaul.VoidFlavor(38, inDays: true, cycler: true));
        Assert.Equal("🦜 the parrot is greying at the crest — mind the long years out here", LongHaul.VoidFlavor(10, inDays: false));
        Assert.Equal("🦜 the parrot preens through the quiet years", LongHaul.VoidFlavor(3, inDays: false, cycler: true));
    }

    // ── R1-F33 · the plaque card and Marsh's adjuster line ────────────────────────────────────────────

    [Fact]
    public void ThePlaqueCardIsTheLeadInAloneAndTheLedgerKeepsTheLore()
    {
        Assert.Equal(
            "You stay at the plate past the point where people move on, and the last two lines stop reading like a dedication. "
            + "Somebody wrote down a berth that is still waiting, on a plate meant to be glanced at, and nobody has taken the plate down.",
            KaamosLore.ListedBerthPlaqueCard);
        Assert.DoesNotContain(KaamosLore.ListedBerthSaidBeside, KaamosLore.ListedBerthPlaqueCard, StringComparison.Ordinal);
        Assert.EndsWith(KaamosLore.ListedBerthSaidBeside, KaamosLore.ById("listed-berth")!.Lore, StringComparison.Ordinal);
    }

    [Fact]
    public void MarshsAdjusterPerceptionIsFablesHandInTheStatic()
    {
        var line = OracleRant.Truths.Single(l => l.FragmentId == "adjuster-tell" && l.Kind == OracleTruthKind.NebulaFragment);
        Assert.Equal(
            "I audited for them, you know, before the frequency took me. There's a hand I keep shaking in the static — same grip, different sleeve, every time certain it's the first time. They pay you not to count. I counted.",
            line.Perception);
    }

    // ── U04-F22 · the amber row ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheGuardsOpeningIsTheQuotedPassAndThenTheUnmovedPalm()
    {
        Assert.Equal("\"Pass.\" Said once, to the palm, which has not moved.", GuardStop.Opening);
        Assert.Contains(GuardStop.Opening, GuardStop.AllProse());
    }

    // ── U06-F12 · the round's last sentence ───────────────────────────────────────────────────────────

    [Fact]
    public void TheChallengeCardEndsOnHeWillWriteItDownWhicheverWayItGoes()
    {
        string card = PatrolBeat.ChallengeCard("A guard");
        Assert.EndsWith(
            "That is the part worth being frightened of — he has done this a hundred times, it has come to nothing a hundred times, and he will write it down whichever way it goes.",
            card, StringComparison.Ordinal);
        Assert.DoesNotContain("tonight", card, StringComparison.Ordinal);
    }
}
