using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// THE ARCHIVE NODE, ON A DECK SOMEBODY CAN ACTUALLY STAND ON.
///
/// <para><c>ArchiveNode.cs</c> and its Core tests were green and complete for days while the feature was
/// entirely unreachable: no field in the walk loop, no console, no card, no cheat. Core tests cannot catch
/// that, by construction — they can only prove the rules are right about a world nobody built. So these
/// walk the SHIPPING <see cref="DeckPlan"/> the renderer draws and the <c>[E]</c> key dispatches through,
/// and they ask the three questions the story-QA run asks of any beat:</para>
///
/// <list type="number">
/// <item><b>Does it exist on screen?</b> — the console is on the deck, at the point Core names.</item>
/// <item><b>Can it be reached on demand?</b> — <c>?archive=1</c> boots onto a hull that really has one.</item>
/// <item><b>Does the sentence match the sim?</b> — the legend is on the deck verbatim, unhedged.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheArchiveNodeIsOnTheDeckTests
{
    private const double InteractRadius = 3.0;   // DeckPlan.InteractRadius

    private static Derelict.Wreck Hull =>
        Derelict.SeededWithCause(Derelict.WreckCause.VentedByOneOfTheirOwn)
        ?? Derelict.Seeded("kestrel-3");

    private static DeckPlan Deck(bool aboard, bool purged = false) =>
        WreckInterior.WreckDeck(
            Hull, new HashSet<string>(), salvaged: false, droidCount: 0,
            fillDroids: (_, _) => { }, heldDoors: null, blockedDoors: null,
            archiveAboard: aboard, archivePurged: purged);

    private static DeckPlan.ConsoleSpot[] Of(DeckPlan deck, DeckPlan.ConsoleKind kind) =>
        [.. deck.Consoles.Where(c => c.Kind == kind)];

    // ── 1 · It exists on screen ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void AHullCarryingOneHasBothControlsOnTheDeck_AtThePointsCoreNames()
    {
        DeckPlan deck = Deck(aboard: true);

        DeckPlan.ConsoleSpot column = Assert.Single(Of(deck, DeckPlan.ConsoleKind.ArchiveNode));
        DeckPlan.ConsoleSpot handle = Assert.Single(Of(deck, DeckPlan.ConsoleKind.ArchiveSwitch));

        // Placed by eye in the renderer, the scuttling panel landed on the nest and the valve board stood in
        // a doorway. Every wreck literal that moved into Core turned out to be already wrong the moment a
        // test could see it, so the console must read Core's point rather than its own copy of it.
        Assert.Equal((float)WreckLayout.ArchiveStation.X, column.X);
        Assert.Equal((float)WreckLayout.ArchiveStation.Y, column.Y);
        Assert.Equal((float)WreckLayout.ArchiveSwitchStation.X, handle.X);
        Assert.Equal((float)WreckLayout.ArchiveSwitchStation.Y, handle.Y);
    }

    [Fact]
    public void AHullWithoutOneHasNeither()
    {
        DeckPlan deck = Deck(aboard: false);

        Assert.Empty(Of(deck, DeckPlan.ConsoleKind.ArchiveNode));
        Assert.Empty(Of(deck, DeckPlan.ConsoleKind.ArchiveSwitch));
        Assert.DoesNotContain(deck.RoomLabels, l => l.Text.Contains("SUBSTRATE SPAR"));
    }

    [Fact]
    public void TheColumnAndTheHandleAreTwoCONTROLS_NotOne()
    {
        // NearestConsoleSpot hands [E] whatever is closest, so two spots inside one interact radius is ONE
        // spot with a spare label — and the loser here would be the handle, which is the only control in the
        // feature that must stay operable without paying a throw.
        DeckPlan deck = Deck(aboard: true);
        DeckPlan.ConsoleSpot column = Of(deck, DeckPlan.ConsoleKind.ArchiveNode)[0];
        DeckPlan.ConsoleSpot handle = Of(deck, DeckPlan.ConsoleKind.ArchiveSwitch)[0];

        double gap = System.Math.Sqrt(
            ((double)column.X - handle.X) * ((double)column.X - handle.X)
            + (((double)column.Y - handle.Y) * ((double)column.Y - handle.Y)));

        Assert.True(gap > InteractRadius, $"the column and the handle are {gap:0.0} du apart — that is one console");
    }

    [Fact]
    public void StandingAtTheHandleIsNotStandingAtArmsLengthOfTheColumn()
    {
        // The same law as the Core route test, asked of the DECK: a captain who walks to the handle and
        // presses [E] must not have been made to look at the thing on the way. "You may pull the handle
        // without paying, and you will never find out what you did."
        DeckPlan deck = Deck(aboard: true);
        DeckPlan.ConsoleSpot column = Of(deck, DeckPlan.ConsoleKind.ArchiveNode)[0];
        DeckPlan.ConsoleSpot handle = Of(deck, DeckPlan.ConsoleKind.ArchiveSwitch)[0];

        Assert.False(ArchiveNode.AtArmsLength(handle.X - column.X, handle.Y - column.Y));
    }

    // ── 2 · It can be reached on demand ───────────────────────────────────────────────────────────────

    [Fact]
    public void TheArchiveCheatBootsOntoAHullThatReallyCarriesOne()
    {
        // "A scene nobody can reach on demand is a scene that ships broken" — the house rule written beside
        // these cheats. It is only true if the demand WORKS: ?archive=1 must land the captain on a hull
        // Core's own placement rule says has a node aboard, not merely on a wreck.
        Derelict.Wreck booted = Map.ArchiveCheatWreck();

        Assert.True(ArchiveNode.CauseMayCarryOne(booted.Cause),
            $"?archive=1 boots onto a {booted.Cause} hull, which may never carry a node at all");
        Assert.True(ArchiveNode.IsAboard(booted.Id, booted.Cause),
            $"?archive=1 boots onto {booted.ShipName}, and she has no node aboard");
    }

    [Fact]
    public void TheCheatHullIsTheSameShipEveryTime()
    {
        // A cheat that re-rolls is a cheat you cannot regression-test against.
        Assert.Equal(Map.ArchiveCheatWreck().Id, Map.ArchiveCheatWreck().Id);
    }

    // ── 3 · The sentence matches the sim ──────────────────────────────────────────────────────────────

    [Fact]
    public void TheLegendIsOnTheDeckVERBATIM_AndNothingHedgesIt()
    {
        // §5, and the entire joke: the label is the confirmation dialog. It is drawn where the captain can
        // read it before they decide anything, and the game never restates, softens or qualifies it. If a
        // future edit adds "(careful!)" to this label, this is the test that has to be deleted to do it.
        DeckPlan.ConsoleSpot handle = Of(Deck(aboard: true), DeckPlan.ConsoleKind.ArchiveSwitch)[0];

        Assert.Contains(ArchiveNode.SwitchLegend, handle.Label);
        foreach (string hedge in new[] { "are you sure", "careful", "warning", "confirm", "danger" })
        {
            Assert.DoesNotContain(hedge, handle.Label, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ThePainterIsToldAboutEveryVisionThePoolCanShow()
    {
        // Five authored visions, five canvases. A slot whose art was never wired renders as a bare caption
        // and nobody finds out until somebody plays it — the exact failure this whole run exists to catch.
        foreach (ArchiveNode.Vision v in ArchiveNode.Visions)
        {
            Assert.StartsWith("art/", v.ArtFile);
            Assert.EndsWith(".jpg", v.ArtFile);
        }

        Assert.Equal(ArchiveNode.Visions.Length,
            ArchiveNode.Visions.Select(v => v.ArtFile).Distinct().Count());
    }

    [Fact]
    public void APurgedColumnStopsPromisingWithoutDisappearing()
    {
        // A purge is not a tidy-up: the spar is still bolted to the deck, and a captain who comes back must
        // find the thing they did rather than an empty room that quietly forgets it.
        DeckPlan deck = Deck(aboard: true, purged: true);

        Assert.Single(Of(deck, DeckPlan.ConsoleKind.ArchiveNode));
        Assert.Single(Of(deck, DeckPlan.ConsoleKind.ArchiveSwitch));

        // …and the one thing on the deck that said it still had power stops saying so.
        Assert.DoesNotContain(deck.RoomLabels, l => l.Text.Contains("⚡"));
        Assert.Contains(deck.RoomLabels, l => l.Text.Contains("SUBSTRATE SPAR"));
    }
}
