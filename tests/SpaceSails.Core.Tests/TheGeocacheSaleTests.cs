using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #319 slice 2 · THE GEOCACHE SALE, as pure arithmetic: the fence's price less the ground's fee, a buyer who
/// lifts on the parcel's own seeded 2–4 watches, and a handful of tags in the turned-over register that must
/// survive the vault. Each guard was watched go RED against the revert its summary names (quoted in the PR).
/// </summary>
public sealed class TheGeocacheSaleTests
{
    private static TreasureCache Cache(
        string id = "cache-you-7", bool mine = true, int watchdogs = 0,
        IReadOnlyList<CacheCargo>? cargo = null, IReadOnlyList<Satchel.Item>? deposit = null) =>
        new(id, "phobos", "the monolith", "spinward", 40, 0, cargo ?? [], 1000, "YOU", mine, watchdogs,
            SiteIndex: 0, Buried: true, Deposit: deposit);

    private static double At(long watch) => (watch * PatronRota.WatchSeconds) + 60;

    // ── WHAT THE FENCE WOULD BUY ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE ROW EXISTS ONLY FOR A PRICED KIND, AND ONLY FOR THE CAPTAIN'S OWN HOLE. The chip is the one satchel
    /// thing the fence buys, however it went into the ground; a parcel, a relic, a chit or coin is not sellable,
    /// and neither is somebody else's cache.
    /// </summary>
    [Fact]
    public void OnlyThePricedKindInTheCaptainsOwnHoleIsSellable()
    {
        Assert.True(GeocacheSale.IsSellable(Cache(deposit: [CompromisingChip.Found()])));
        Assert.True(GeocacheSale.IsSellable(Cache(cargo: [CompromisingChip.Manifest()])));
        Assert.Equal(CompromisingChip.Name, GeocacheSale.WhatTheFenceWouldBuy(Cache(deposit: [CompromisingChip.Found()])));

        Assert.False(GeocacheSale.IsSellable(Cache(mine: false, deposit: [CompromisingChip.Found()])));
        Assert.False(GeocacheSale.IsSellable(Cache()));
        Assert.False(GeocacheSale.IsSellable(Cache(cargo: [new CacheCargo("ore", 4, false)])));
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            var item = new Satchel.Item(kind, "something-else");
            Assert.False(GeocacheSale.IsSellable(Cache(deposit: [item])),
                $"a {kind} with no price at the fence was offered for sale.");
        }
    }

    // ── THE FEE ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE FEE IS MONOTONE IN WATCHDOGS: every Old One on the ground costs more, from the minimum on an empty
    /// ground to the top at <see cref="ReeverRaid.MaxReevers"/>, and nothing past that. The quote is the
    /// fence's price less exactly that fee.
    /// </summary>
    [Fact]
    public void TheFeeRisesWithEveryWatchdogAndTheQuoteIsThePriceLessIt()
    {
        Assert.Equal(GeocacheSale.MinimumFeePercent, GeocacheSale.FeeFor(0));
        Assert.Equal(GeocacheSale.FeeFor(0), GeocacheSale.FeeFor(-3));
        for (int w = 1; w <= ReeverRaid.MaxReevers; w++)
        {
            Assert.True(GeocacheSale.FeeFor(w) > GeocacheSale.FeeFor(w - 1),
                $"the fee did not rise from {w - 1} to {w} watchdogs.");
        }
        Assert.Equal(GeocacheSale.FeeFor(ReeverRaid.MaxReevers), GeocacheSale.FeeFor(ReeverRaid.MaxReevers + 5));
        Assert.True(GeocacheSale.FeeFor(ReeverRaid.MaxReevers) < 100);

        for (int w = 0; w <= ReeverRaid.MaxReevers; w++)
        {
            Assert.Equal(1000 - (1000 * GeocacheSale.FeeFor(w) / 100), GeocacheSale.Quote(1000, w));
        }
        Assert.Equal(3, GeocacheSale.WatchdogsAt(Cache(watchdogs: 3)));
    }

    // ── THE LIFT ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE LIFT HAPPENS AT EXACTLY THE SEEDED WATCH, NEVER BEFORE: two to four watches after the sale, the
    /// parcel's own clock, and not a minute earlier.
    /// </summary>
    [Fact]
    public void TheBuyerLiftsAtExactlyTheSeededWatch()
    {
        var gaps = new HashSet<long>();
        for (int i = 0; i < 60; i++)
        {
            string id = $"cache-you-{i}";
            const long sold = 400;
            long lift = GeocacheSale.LiftWatch(id, sold);
            gaps.Add(lift - sold);
            Assert.InRange(lift - sold, ParcelDrop.FewestWatchesBeforeItLands, ParcelDrop.MostWatchesBeforeItLands);
            Assert.Equal(sold + ParcelDrop.WatchesBeforeItLands(id), lift);

            for (long w = sold; w < lift; w++)
            {
                Assert.False(GeocacheSale.IsLiftedAt(id, sold, At(w)), $"{id} lifted on watch {w}, before {lift}.");
            }
            Assert.False(GeocacheSale.IsLiftedAt(id, sold, (lift * PatronRota.WatchSeconds) - 1));
            Assert.True(GeocacheSale.IsLiftedAt(id, sold, lift * PatronRota.WatchSeconds));
            Assert.True(GeocacheSale.IsLiftedAt(id, sold, At(lift + 5)));
            Assert.Equal(lift * PatronRota.WatchSeconds, GeocacheSale.LiftMoment(id, sold));
        }
        Assert.True(gaps.Count > 1, "every sale lifted after the same gap — the seed is not spreading it.");
    }

    // ── THE TAGS ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE TAGS READ BACK WHAT WAS WRITTEN, and a register with no sale in it reads as no sale — the desk's
    /// own quiet watches and a room key beside them included.
    /// </summary>
    [Fact]
    public void TheTagsReadBackWhatWasWritten()
    {
        var register = new HashSet<string>(StringComparer.Ordinal)
        {
            ParcelDrop.NothingForThisHullOn(9), "room:3:7@12",
        };
        Assert.Empty(GeocacheSale.SalesIn(register));
        Assert.Empty(GeocacheSale.OwedIn(register));
        Assert.Empty(GeocacheSale.HolesIn(register));
        Assert.False(GeocacheSale.WasSold(register, "cache-you-1"));

        register.Add(GeocacheSale.SoldOn("cache-you-1", 88));
        register.Add(GeocacheSale.OwedOn("cache-you-1", 1200, 960, 91));
        register.Add(GeocacheSale.HoleAt("cache-you-1", "phobos", 2, -12.25, 40.5));

        GeocacheSale.Sale sale = Assert.Single(GeocacheSale.SalesIn(register));
        Assert.Equal(new GeocacheSale.Sale("cache-you-1", 88), sale);
        GeocacheSale.Owed owed = Assert.Single(GeocacheSale.OwedIn(register));
        Assert.Equal(("cache-you-1", 1200, 960, 91L), (owed.CacheId, owed.Gross, owed.Net, owed.LiftWatch));
        Assert.Equal(new GeocacheSale.Hole("cache-you-1", "phobos", 2, -12.25, 40.5), Assert.Single(GeocacheSale.HolesIn(register)));
        Assert.True(GeocacheSale.WasSold(register, "cache-you-1"));
        Assert.False(GeocacheSale.WasSold(register, "cache-you-10"));
        Assert.True(GeocacheSale.IsSettled(register, "cache-you-1"));
        Assert.False(GeocacheSale.IsSettled(register, "cache-you-10"));
    }

    /// <summary>
    /// THE TAGS SURVIVE A VAULT ROUND-TRIP (#119): the sale, the escrow owed, the death, the shut desk and the
    /// hole all ride the register the vault already carries, and come back readable.
    /// </summary>
    [Fact]
    public void TheTagsSurviveTheVault()
    {
        string[] written =
        [
            GeocacheSale.SoldOn("cache-you-3", 120),
            GeocacheSale.OwedOn("cache-you-3", 700, 560, 123),
            GeocacheSale.DeadOn("cache-you-4", 130),
            GeocacheSale.ShutAt("selene-gate"),
            GeocacheSale.HoleAt("cache-you-3", "luna", null, 1.5, -2.75),
            GeocacheSale.HoleReadOn("cache-you-3"),
        ];

        Vault back = VaultSerializer.Load(VaultSerializer.Save(new Vault
        {
            TurnedOver = new TurnedOverSection { Rooms = written },
        }));

        IReadOnlyList<string> rooms = back.TurnedOver!.Rooms;
        Assert.Equal(written.OrderBy(s => s, StringComparer.Ordinal), rooms.OrderBy(s => s, StringComparer.Ordinal));
        Assert.Equal(new GeocacheSale.Sale("cache-you-3", 120), Assert.Single(GeocacheSale.SalesIn(rooms)));
        Assert.Equal(560, Assert.Single(GeocacheSale.OwedIn(rooms)).Net);
        Assert.Null(Assert.Single(GeocacheSale.HolesIn(rooms)).SiteIndex);
        Assert.True(GeocacheSale.IsSettled(rooms, "cache-you-4"));
    }

    // ── THE LINES ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The quote card reads the design's sentence, and an empty ground reads its own phrase.</summary>
    [Fact]
    public void TheQuoteCardSaysWhatTheGroundCosts()
    {
        Assert.Equal(
            "Escrow. The buyer lifts it; you are paid when it is lifted. The fee is the ground's: "
            + $"3 watchdogs on it, {GeocacheSale.FeeFor(3)}% off.",
            GeocacheSale.TheQuoteLine(3));
        Assert.EndsWith("nothing on it, the minimum off.", GeocacheSale.TheQuoteLine(0), StringComparison.Ordinal);
        Assert.Equal("A data chip, under Phobos · The Ridge.", GeocacheSale.TheSubLine("A data chip", "Phobos · The Ridge"));
        Assert.Equal("The hole was where you said. 1,200 cr, less the fee.", GeocacheSale.ThePaymentLine(1200));
    }

    /// <summary>The dev start's address is read, not stored.</summary>
    [Fact]
    public void TheDevStartIsReadOffTheAddress()
    {
        Assert.Equal(GeocacheSale.Cheat.Buried, GeocacheSale.CheatIn("https://x/map?dock=selene-gate&geocache=1"));
        Assert.Equal(GeocacheSale.Cheat.Lifted, GeocacheSale.CheatIn("https://x/map?dock=selene-gate&geocache=lifted"));
        Assert.Equal(GeocacheSale.Cheat.None, GeocacheSale.CheatIn("https://x/map?dock=selene-gate"));
        Assert.Equal(GeocacheSale.Cheat.None, GeocacheSale.CheatIn(null));
    }
}
