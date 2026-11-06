using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Fable's 2026-11-06 batch of authored lines (owner ruling 2026-11-06), pinned word for word: #1387 the coast
/// note, #1434 the writ on foot, #1444 the open-window ledger line and the satchel's per-lock sentence, #1380's
/// reason for the greyed Board button.
/// </summary>
public sealed class TheFableLines1106Tests
{
    [Fact]
    public void ACoastAlongTheOrbitSaysItArrivesNowhere()
    {
        Assert.Equal(
            "⏭ A coast, not a crossing — 22 d along this same orbit, and nothing waiting at the far end of it.",
            WarpSkip.CoastAlongOrbitNote("22 d"));
    }

    [Fact]
    public void TheOneWayDownLineIsFablesWords()
    {
        Assert.Equal(
            "🛸 The boat will fly you down and cannot bring you back: the ship is close enough, but she is parting "
            + "company faster than the boat can fly, and a shuttle that cannot come alongside is a shuttle that does not launch.",
            ShuttleLink.OneWayDownLine);
        Assert.Equal("NO RIDE BACK — she is parting faster than the boat can fly", ShuttleLink.OneWayDownFace);
    }

    private const string Ground = CollectorLanding.GroundHunterIdPrefix + "luna";
    private const string Ship = "hunter-0";

    [Fact]
    public void OnFootTheWritsAnswersSpeakOnFootAndTheShipOnesAreUnchanged()
    {
        Assert.Equal("🏴 OUT OF THEIR REACH", CollectorLanding.OutcomeLabel(Ground, CollectorLanding.WritOutcome.Fled));
        Assert.Equal("🔫 THEY STAND OFF", CollectorLanding.OutcomeLabel(Ground, CollectorLanding.WritOutcome.ResistWon));
        Assert.Equal("💰 PAID OFF", CollectorLanding.OutcomeLabel(Ground, CollectorLanding.WritOutcome.BribedOff));
        Assert.Equal("🏴 BROKE CLEAR", CollectorLanding.OutcomeLabel(Ship, CollectorLanding.WritOutcome.Fled));
        Assert.Equal("🔫 THEY PEEL OFF", CollectorLanding.OutcomeLabel(Ship, CollectorLanding.WritOutcome.ResistWon));
        Assert.Equal("💰 PAID OFF", CollectorLanding.OutcomeLabel(Ship, CollectorLanding.WritOutcome.BribedOff));

        Assert.Equal(
            "800 cr changes hands, glove to glove. Vance logs a clean sweep and walks back to their boat — heat unchanged. You bought this crew, not the law.",
            CollectorLanding.BribeResult(Ground, 800, "Vance").Replace('\u00a0', ' '));
        Assert.Equal(
            "You break Vance's hold — they fall back to their boat nursing the bruise. Heat climbs; the next crew is meaner.",
            CollectorLanding.ResistWonResult(Ground, "Vance"));
        Assert.Equal(
            "You fight clear — Vance left picking themselves up beside their own boat. You slip away on foot carrying heat 3.",
            CollectorLanding.FleeResult(Ground, "Vance", 3));

        Assert.Equal(
            "800 cr changes hands. Vance logs a clean sweep and sheers off — heat unchanged. You bought this patrol, not the law.",
            CollectorLanding.BribeResult(Ship, 800, "Vance"));
        Assert.Equal(
            "You break Vance's boarding — they peel off nursing the dent. Heat climbs; the next wave is meaner.",
            CollectorLanding.ResistWonResult(Ship, "Vance"));
        Assert.Equal(
            "You fight clear — Vance left tied up at their own ship. You slip away carrying heat 3.",
            CollectorLanding.FleeResult(Ship, "Vance", 3));
    }

    [Fact]
    public void AnOpenWindowWithNoRunFiledSaysTheArcIsHereAndTakesARun()
    {
        string line = KaamosLore.ReachLedgerLineAt(CyclerWindow.PeriodSeconds + 3600.0, alongside: false, runFiled: false);
        Assert.Equal(
            "❄ The berth-code is entered and the ice-moon berth is listed to your hull. The cycler window is open — the arc is here, "
            + "and it takes a run, not a passenger. The berth's own consignment is still sitting on a counter somewhere, unfiled.",
            line);
        Assert.NotEqual(KaamosLore.ReachLedgerLine, line);
    }

    [Fact]
    public void TheSatchelSaysWhatEachLockIs()
    {
        Assert.Equal("It reads authorities and nothing else — a countersigned card, or nothing at all.",
            SatchelTry.SubtitleFor(SatchelTry.Target.ShaftGate));
        Assert.Equal("A mechanical lock, shut by a key that left with its owner. The wallet is the only thing worth holding up to it.",
            SatchelTry.SubtitleFor(SatchelTry.Target.RoomDoor));
        Assert.Equal("No reader on this side. You can hold a card up to it; it is the only thing in the bag that would even be a gesture.",
            SatchelTry.SubtitleFor(SatchelTry.Target.SealedWay));
    }
}
