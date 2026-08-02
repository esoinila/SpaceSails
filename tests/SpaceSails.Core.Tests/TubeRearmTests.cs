using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>#562 · The tube-feeds-you card. Pinned like its siblings — a card a captain sees exactly once
/// gets no second chance to be wrong — plus the pacing constant the bar is drawn against.</summary>
public class TubeRearmTests
{
    [Fact]
    public void TheCard_IsFullyDressed()
    {
        Assert.False(string.IsNullOrWhiteSpace(TubeRearm.Stamp));
        Assert.False(string.IsNullOrWhiteSpace(TubeRearm.Head));
        Assert.False(string.IsNullOrWhiteSpace(TubeRearm.Dismiss));
        Assert.False(string.IsNullOrWhiteSpace(TubeRearm.Foot));
        Assert.NotEmpty(TubeRearm.Beats);
    }

    [Fact]
    public void EveryBeat_IsRealProse_NotAStub()
    {
        foreach (string beat in TubeRearm.Beats)
        {
            Assert.True(beat.Length > 60, $"a beat barely explains itself: \"{beat}\"");
        }
    }

    [Fact]
    public void ItTeachesTheSupplyLine_NotJustTheFeature()
    {
        // The whole reason the reload is a PLACE. Owner: "the reload forces the player to plan their routes
        // … and keep their supply line safe for retreat to reload". A card that only announced "your guns
        // refill here" would have taught the button and missed the game.
        string all = string.Join(" ", TubeRearm.Beats).ToUpperInvariant();
        Assert.Contains("WALK BACK", all, System.StringComparison.Ordinal);
        Assert.Contains("ANCHOR", all, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ItSaysTheRoundsAreCheap_BecauseTheCostIsSupposedToBeTheWalk()
    {
        // If a captain thinks ammo is the expensive part, they ration it and stay aboard — the exact
        // outcome the halved price exists to prevent ("we want to encourage exploration and that takes
        // ammo"). The card has to say plainly where the real cost is.
        string all = string.Join(" ", TubeRearm.Beats).ToUpperInvariant();
        Assert.Contains("CHEAP", all, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ItPromisesNothingIsLostByLeavingEarly()
    {
        // The rearm is automatic and interruptible, and a player who does not know that will stand in a
        // tube they should be running out of. Rounds already racked are already in the magazine.
        string first = TubeRearm.Beats[0].ToUpperInvariant();
        Assert.Contains("NOTHING IS LOST", first, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TheArt_IsUnderTheArtFolder_AndIsAJpeg()
    {
        Assert.StartsWith("art/", TubeRearm.ArtFile, System.StringComparison.Ordinal);
        Assert.EndsWith(".jpg", TubeRearm.ArtFile, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ARackingIsACoupleOfSeconds_NotAnInstantAndNotAChore()
    {
        // Owner's pick, and the bar is drawn against it: long enough to feel the ship working, short enough
        // that doing it every trip never becomes tedious. Guarded on both sides because both failures are
        // real — an instant bar is a lie, and an eight-second one is a tax on exploring.
        Assert.True(SentryBot.RearmSecondsPerMagazine >= 1.0);
        Assert.True(SentryBot.RearmSecondsPerMagazine <= 4.0);
    }

    [Fact]
    public void OneMagazineAtATime_FillsTheFirstBotWholly_NotBothHalfway()
    {
        // The tube racks one bot per bar. Quoting a single magazine against the purse is the same pure Core
        // law the haven armory uses — one price, seen from another door.
        SentryBot.RestockQuote q = SentryBot.QuoteRestock([40], credits: 1_000_000);
        Assert.Equal(SentryBot.MaxMagazine - 40, q.RoundsBought);
        Assert.Equal(SentryBot.MaxMagazine, q.Magazines[0]);
    }

    /// <summary>#562 vs #580/#563 · THE TUBE IS THE ANCHOR, NOT THE ONLY SUPPLY POINT. The card said "this
    /// tube is the only place your sentries get fed" — true when it was written, and false since a shelter's
    /// emergency press started reloading magazines in full every time you ask (SurfaceShelter.LockerRounds)
    /// and an outpost hut's locker started spreading rounds across the bots you brought. A once-per-captain
    /// card that names the wrong law teaches a captain to walk past the thing that would have saved the
    /// walk. What is still true — and what the card must keep saying — is that the tube is the one that is
    /// ALWAYS there.</summary>
    [Fact]
    public void ItDoesNotClaimToBeTheOnlySupplyPoint_BecauseSheltersAndLockersExist()
    {
        string all = string.Join(" ", TubeRearm.Beats);

        Assert.DoesNotContain("only place your sentries get fed", all, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shelter", all, System.StringComparison.OrdinalIgnoreCase);

        // The other end: a shelter really does reload, and it is not a token drawer.
        Assert.True(SurfaceShelter.LockerRounds > 0);
    }
}
