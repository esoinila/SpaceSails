using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Client.Pages.Stations;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #319 slice 2 · <b>THE GEOCACHE SALE — the page's half.</b> Core owns the price, the fee, the schedule and
/// every word (<see cref="GeocacheSale"/>); this file is the moments Core cannot reach: the row at the desk,
/// the sale, the buyer's lift, the escrow at the next desk, the sale killed by the captain's own shovel, and
/// the hole read when he walks onto it.
///
/// <h3>No field on this page</h3>
///
/// <para>Every fact rides <c>_roomsTurnedOver</c> as a tag, the register #711's quiet watches and #794's chalk
/// already write into and the vault already carries; the dev start is read off the address bar. #905's frame
/// ledger walks every instance field of this page, so a new one would move it for a feature that is not on
/// record. With no sale on record nothing here writes, draws or offers.</para>
/// </summary>
public sealed partial class Map
{
    // ── WHAT THE FENCE PAYS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #319 · What the fence would pay for what is in this hole, or null. The chip is the one satchel thing it
    /// buys, and its price is its own contract's (<see cref="CompromisingChip.FencePrice"/>), the same
    /// arithmetic the chip's desk row uses. The contract is the car's fetch job in whatever state it stands:
    /// burying the chip closes it (ending three), and the price of a betrayal does not change because the
    /// betrayal went into the ground first.
    /// </summary>
    private int? TheFencesPriceFor(TreasureCache cache) =>
        GeocacheSale.IsSellable(cache) && _quests.LastOrDefault(TheCarHasTheChip) is { } job
            ? CompromisingChip.FencePrice(job.Reward)
            : null;

    // ── THE ROW ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The first cache of the captain's the desk would sell the location of, with its price.</summary>
    private (TreasureCache Cache, int Gross)? TheLocationForSale()
    {
        if (_caches is null || !DarkWebCanTrade()
            || DarkWebCurrentBody() is not { } port
            || _roomsTurnedOver.Contains(GeocacheSale.ShutAt(port.Id)))
        {
            return null;
        }

        foreach (TreasureCache cache in _caches.Caches)
        {
            if (TheFencesPriceFor(cache) is { } gross && !GeocacheSale.WasSold(_roomsTurnedOver, cache.Id))
            {
                return (cache, gross);
            }
        }
        return null;
    }

    /// <summary>#319 · SELL THE LOCATION, as the desk draws it — null, and no row, when there is nothing to
    /// sell.</summary>
    private DarkWeb.GeocacheOffer? GeocacheOnOffer()
    {
        if (TheLocationForSale() is not { } found)
        {
            return null;
        }

        TreasureCache cache = found.Cache;
        int watchdogs = GeocacheSale.WatchdogsAt(cache);
        string ground = FieldNotes.PlaceLabel(BodyName(cache.BodyId), cache.SiteName);
        int net = GeocacheSale.Quote(found.Gross, watchdogs);
        return new DarkWeb.GeocacheOffer(
            GeocacheSale.TheSubLine(GeocacheSale.WhatTheFenceWouldBuy(cache)!, ground),
            GeocacheSale.TheQuoteLine(watchdogs),
            net,
            $"+{net.ToString("N0", CultureInfo.InvariantCulture)} cr");
    }

    /// <summary>#319 · Send the coordinates: one tag, one line, and a buyer on a schedule.</summary>
    private void SellTheLocation()
    {
        if (TheLocationForSale() is not { } found)
        {
            return;
        }

        _roomsTurnedOver.Add(GeocacheSale.SoldOn(found.Cache.Id, PatronRota.WatchIndex(SimTime)));
        ShowPulseMessage(GeocacheSale.SentLine);
        RequestVaultSave();
        StateHasChanged();
    }

    // ── THE BUYER KEEPS HIS SCHEDULE ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #319 · At the seeded watch the cache leaves the ground. Nothing is animated: whenever the page next
    /// looks (a surface frame, the desk opening), a sold cache whose watch has come is lifted out of the
    /// ledger, its hole is dated to the lift, and the escrow is owed with the sums fixed from the chest that
    /// was there. A sale the captain has already dug out from under the buyer is never lifted, because there
    /// is no chest left to lift.
    /// </summary>
    private void TheBuyerKeepsHisSchedule()
    {
        if (_caches is null)
        {
            return;
        }

        bool lifted = false;
        foreach (GeocacheSale.Sale sale in GeocacheSale.SalesIn(_roomsTurnedOver))
        {
            if (!GeocacheSale.IsLiftedAt(sale.CacheId, sale.SoldWatch, SimTime)
                || GeocacheSale.IsSettled(_roomsTurnedOver, sale.CacheId)
                || !_caches.Caches.Any(c => c.Id == sale.CacheId))
            {
                continue;
            }

            TreasureCache cache = _caches.Caches.First(c => c.Id == sale.CacheId);
            int gross = TheFencesPriceFor(cache) ?? 0;
            int net = GeocacheSale.Quote(gross, GeocacheSale.WatchdogsAt(cache));
            long liftWatch = GeocacheSale.LiftWatch(sale.CacheId, sale.SoldWatch);

            _caches.Remove(sale.CacheId);
            SomebodyDugIt(cache, GeocacheSale.LiftMoment(sale.CacheId, sale.SoldWatch));
            (double x, double y) = MoonSurface.CacheSpot(cache);
            _roomsTurnedOver.Add(GeocacheSale.OwedOn(sale.CacheId, gross, net, liftWatch));
            _roomsTurnedOver.Add(GeocacheSale.HoleAt(sale.CacheId, cache.BodyId, cache.SiteIndex, x, y));
            lifted = true;
        }

        if (lifted)
        {
            RequestVaultSave();
        }
    }

    // ── AND AT THE NEXT DESK ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #319 · Called where the dark-web desk is OPENED, beside the parcel's payment. In order: the buyer's
    /// schedule is kept; a sale whose chest the captain lifted first dies here, told once, and this desk has
    /// nothing for him for the rest of the visit; and one escrow owed is paid on the payment pulse. Before the
    /// lift, nothing — the desk does not explain.
    /// </summary>
    private void TheGeocacheEscrowSettles()
    {
        TheBuyerKeepsHisSchedule();
        if (_caches is null)
        {
            return;
        }

        foreach (GeocacheSale.Sale sale in GeocacheSale.SalesIn(_roomsTurnedOver))
        {
            if (GeocacheSale.IsSettled(_roomsTurnedOver, sale.CacheId)
                || _caches.Caches.Any(c => c.Id == sale.CacheId))
            {
                continue;
            }

            _roomsTurnedOver.Add(GeocacheSale.DeadOn(sale.CacheId, PatronRota.WatchIndex(SimTime)));
            if (DarkWebCurrentBody() is { } port)
            {
                _roomsTurnedOver.Add(GeocacheSale.ShutAt(port.Id));
            }
            ShowPulseMessage(GeocacheSale.LiftedFirstLine);
            RequestVaultSave();
            StateHasChanged();
            return;
        }

        foreach (GeocacheSale.Owed owed in GeocacheSale.OwedIn(_roomsTurnedOver))
        {
            _roomsTurnedOver.Remove(owed.Tag);
            _roomsTurnedOver.Add(GeocacheSale.PaidOn(owed.CacheId, PatronRota.WatchIndex(SimTime)));
            _credits += owed.Net;
            ShowPulseMessage(
                $"💳 {GeocacheSale.ThePaymentLine(owed.Gross)} +{owed.Net.ToString("N0", CultureInfo.InvariantCulture)} cr");
            RequestVaultSave();
            StateHasChanged();
            return;
        }
    }

    /// <summary>#319 · The desk that had nothing for the captain has something again once he casts off —
    /// "one visit" is the berth he was told at.</summary>
    private void TheDeskIsOpenAgainBehindYou()
    {
        foreach (string shut in _roomsTurnedOver.Where(t => t.StartsWith(GeocacheSale.ShutTag + ":", System.StringComparison.Ordinal)).ToList())
        {
            _roomsTurnedOver.Remove(shut);
        }
    }

    // ── THE HOLE ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #319 · Walking onto a sold spot after the lift: the ground's own forensics already say <i>somebody
    /// dug</i> (the pit the lift left); the book files one entry, once.
    /// </summary>
    private void TheLiftedHoleIsReadHere()
    {
        if (_surface is not { Floor: >= 0 } ex)
        {
            return;
        }

        foreach (GeocacheSale.Hole hole in GeocacheSale.HolesIn(_roomsTurnedOver))
        {
            if (hole.BodyId != ex.Stop.Body.Id
                || (hole.SiteIndex is { } site && site != ex.Site.Index)
                || _roomsTurnedOver.Contains(GeocacheSale.HoleReadOn(hole.CacheId)))
            {
                continue;
            }

            double dx = _avatarX - hole.X, dy = _avatarY - hole.Y;
            if ((dx * dx) + (dy * dy) > GeocacheSale.HoleReachDu * GeocacheSale.HoleReachDu)
            {
                continue;
            }

            _roomsTurnedOver.Add(GeocacheSale.HoleReadOn(hole.CacheId));
            FileNote(GeocacheSale.HoleEntry, GeocacheSale.Glyph);
            RequestVaultSave();
            return;
        }
    }

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #319 QA · <c>?dock=&lt;berth&gt;&amp;geocache=1</c> — a cache of the captain's buried on a named ground
    /// with the chip in it and the chip's contract on the books, the desk in reach;
    /// <c>&amp;geocache=lifted</c> — the location already sold and the buyer already been, so the next desk
    /// opened pays.
    ///
    /// <para>What it PLANTS is what play would have left: the car's fetch job, closed the way burying the chip
    /// closes it, and the chest in the ground (and, for <c>lifted</c>, the sale on a watch far enough back that
    /// its seeded lift has come). Everything after that is the shipped path: the row, the quote, the sale, the
    /// lift, the escrow.</para>
    /// </summary>
    private void BuryAGeocacheForCheat()
    {
        GeocacheSale.Cheat cheat = Navigation is { } address ? GeocacheSale.CheatIn(address.Uri) : GeocacheSale.Cheat.None;
        if (cheat == GeocacheSale.Cheat.None || _caches is null || TheLandableGround() is not { Count: > 0 } ground)
        {
            return;
        }

        if (!_quests.Any(TheCarHasTheChip))
        {
            var job = new Quest("geocache-dev-chip", QuestKind.Fetch, "THE FIXER", Derelict.RoadsterBodyId,
                "the roadster", "A car with photographs in it", "", 600, Pin: CompromisingChip.FindId);
            _quests.Add(job);
            AdvanceMission(job, QuestState.TurnedIn);
        }

        string body = ground[0];
        TreasureCache cache = _caches.Bury(
            body, 0, [CompromisingChip.Manifest()], SimTime, "YOU", playerOwned: true,
            reeverLevel: 2, siteIndex: 0, buried: true);

        if (cheat == GeocacheSale.Cheat.Lifted)
        {
            long now = PatronRota.WatchIndex(SimTime);
            long sold = now - ParcelDrop.MostWatchesBeforeItLands - 1;
            _roomsTurnedOver.Add(GeocacheSale.SoldOn(cache.Id, sold));
        }

        ShowPulseMessage(
            $"🧪 DEV ?geocache={(cheat == GeocacheSale.Cheat.Lifted ? "lifted" : "1")} — "
            + $"{CompromisingChip.Name} under {FieldNotes.PlaceLabel(BodyName(body), cache.SiteName)}; open the dark-web desk.");
    }
}
