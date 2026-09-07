using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE COUNTER AT THE BERTH — what is sold, what is bought, what it is bought on credit with, and what
/// the desk says when there is no pump within reach.
///
/// <para>Selling the hold, buying reaction mass at whatever the pump is asking today (#1068), restocking
/// the sentries' rounds, the wire lender who will float the difference, and the #157 directions line that
/// answers <i>"how do I refuel from here"</i> with a route instead of a dead button — priced by
/// <c>FuelReachability</c> against the current well and cached on a coarse position/tank signature so the
/// transfer solve is not re-run every render while the captain stands at the desk.</para>
///
/// <para>Split out of <c>Map.Trade.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    private void SellCargo()
    {
        if (_cargoValue <= 0)
        {
            return;
        }

        _credits += _cargoValue;
        ShowPulseMessage($"Sold {_cargoUnits} units for {_cargoValue:N0} credits {PlunderLines[(int)((SimTime / 61) % PlunderLines.Length)]}");
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();
        _hotCargo.Launder(); // fenced — the hot flags go with the cargo
        AdvanceTutorial(4); // step 5: first sale
        AdvanceTutorial(StepSellHe3); // second hunt, step 6: fencing the He3 closes the tutorial
        RequestVaultSave(); // #225: a sale changed the purse and emptied the hold
    }

    // #157 "How do I fill her up?" — the FIRST place the tank is ever REFILLED (every other flow only
    // spends pulses). Buys reaction mass at the pump the ship is alongside: caps at the tank, caps at the
    // purse (buys what you can afford and says so), decrements credits by exactly the quote, and logs a
    // ledger line. The alongside gate reuses FuelReachability.AlongsidePump — the same truth the fuel
    // alarm reads — so the button and the alarm can never disagree. pulsesWanted = int.MaxValue is "fill
    // her up"; a small number is the +10 p top-up.
    private void BuyFuel(int pulsesWanted)
    {
        if (_ephemeris is null || FuelReachability.AlongsidePump(_ephemeris, _ship) is not { } pump)
        {
            return; // not alongside a pump — the button isn't shown, but never trust the caller
        }

        int capacity = ReactionMassCapacity;
        int room = capacity - _reactionMassPulses;
        if (room <= 0)
        {
            ShowPulseMessage("Tank's already topped off.");
            return;
        }

        // #1068 · …at whatever the pump is asking today. The market's own overnight move rides on top of
        // the belt price, bounded by the belt markup itself, and is zero at every pump in almost every
        // world. Nothing says it moved; the receipt is simply a credit a pulse different.
        int price = FuelMarket.PricePerPulse(
            _ephemeris.Position(pump.Id, SimTime).Length, QuietHands.PulsePriceMoveAt(_ephemeris, pump.Id));
        FuelMarket.Quote quote = FuelMarket.QuoteFill(_reactionMassPulses, capacity, price, _credits, pulsesWanted);
        if (quote.Pulses <= 0)
        {
            ShowPulseMessage($"Not enough credits — reaction mass is {price} cr/pulse and you hold {_credits:N0} cr.");
            return;
        }

        _credits -= quote.Cost;
        _reactionMassPulses += quote.Pulses;
        LogAutopilotEvent($"⛽ Took on {quote.Pulses} p at {BodyName(pump.Id)} — {quote.Cost:N0} cr");
        ShowPulseMessage(quote.Pulses < room
            ? $"⛽ Bought the {quote.Pulses} p you could afford at {BodyName(pump.Id)} ({quote.Cost:N0} cr) — {capacity - _reactionMassPulses} p of room left"
            : $"⛽ Filled her up at {BodyName(pump.Id)} — {quote.Pulses} p for {quote.Cost:N0} cr");
        _fuelDirSig = null; // any cached "nearest pump" directions are stale now
    }

    // #314 — the sentry rearm line. A docked haven's armory tops off the ship's bot magazines at one
    // honest price (SentryBot.RestockPricePerRound), buying only what the purse affords and printing a
    // #119 receipt. Sits with the fuel/repair services — a berth chore, never available in open space.
    private int SentryRoundsMissing() => _shipBots.Sum(b => SentryBot.RestockCost(b.Rounds)) / SentryBot.RestockPricePerRound;

    private int SentryRestockCost() => _shipBots.Sum(b => SentryBot.RestockCost(b.Rounds));

    private void RestockSentries()
    {
        if (_dockedHavenId is null)
        {
            return; // the button only shows at a berth, but never trust the caller
        }
        if (_shipBots.Count == 0)
        {
            ShowPulseMessage("No sentry bots aboard to rearm — they're down on the surface (or written off).");
            return;
        }

        var mags = _shipBots.Select(b => b.Rounds).ToList();
        SentryBot.RestockQuote quote = SentryBot.QuoteRestock(mags, _credits);
        if (quote.RoundsBought <= 0)
        {
            ShowPulseMessage(SentryRoundsMissing() <= 0
                ? "🤖 Magazines already read full — nothing to rack."
                : $"Not enough credits — sentry rounds are {SentryBot.RestockPricePerRound} cr each and you hold {_credits:N0} cr.");
            return;
        }

        for (int i = 0; i < _shipBots.Count; i++)
        {
            _shipBots[i].Rounds = quote.Magazines[i];
        }
        _credits -= quote.Cost;
        RendererInterop.PlayCue("board");
        string receipt = SentryBot.RestockReceiptLine(quote.RoundsBought, quote.Cost);
        LogAutopilotEvent(receipt);
        ShowPulseMessage(receipt);
        RequestVaultSave();
    }

    // PR-WIRE — the broke-at-a-pump borrow (the dream's anonymized gas-by-wire). A trusted, dark-web-
    // native contact we have history with will wire fuel money on a favor. Picks the most-trusted such
    // contact (most jobs done), or null if none qualifies — the button only shows when this is non-null.
    private string? BestWireLenderId()
    {
        string? best = null;
        int bestMissions = -1;
        foreach ((string id, ContactHistory h) in _contacts.Entries)
        {
            if (!FavorBank.CanWireLoan(ContactSheets.For(id), h.MissionsCompleted))
            {
                continue;
            }
            if (h.MissionsCompleted > bestMissions)
            {
                bestMissions = h.MissionsCompleted;
                best = id;
            }
        }
        return best;
    }

    // The favor line a pump borrow draws: enough to fill the room at the current price, capped so a
    // single favor never wires a fortune (BankLoanPrincipal is the ceiling — a good top-up, no more).
    private long PumpLoanPrincipal(int room, int pricePerPulse) =>
        Math.Min(BankLoanPrincipal, Math.Max(100L, (long)Math.Max(0, room) * Math.Max(1, pricePerPulse)));

    // Wire the favor at the pump and fill what it buys — the stranded captain's rescue. Books the debt
    // and raises the quiet-delivery obligation; then spends the wired coin straight into the tank.
    private void CallInFavorAtPump()
    {
        if (_ephemeris is null || FuelReachability.AlongsidePump(_ephemeris, _ship) is not { } pump)
        {
            return;
        }
        if (BestWireLenderId() is not { } lender)
        {
            return;
        }
        int room = ReactionMassCapacity - _reactionMassPulses;
        // #1068 · …at whatever the pump is asking today. The market's own overnight move rides on top of
        // the belt price, bounded by the belt markup itself, and is zero at every pump in almost every
        // world. Nothing says it moved; the receipt is simply a credit a pulse different.
        int price = FuelMarket.PricePerPulse(
            _ephemeris.Position(pump.Id, SimTime).Length, QuietHands.PulsePriceMoveAt(_ephemeris, pump.Id));
        long principal = PumpLoanPrincipal(room, price);
        if (!BankBorrowFavor(lender, principal, viaWire: true))
        {
            return;
        }
        string lenderName = ContactSheets.For(lender).DisplayName;
        ShowPulseMessage($"📡 {lenderName} wires {principal:N0} cr, no questions — you owe them one quiet delivery. Fill her up.");
        BuyFuel(int.MaxValue); // spend the fresh coin straight into the tank
    }

    // The honest "how do I refuel from here" line for the Trade desk when the ship is NOT alongside a pump
    // (#157 item 3). Priced by FuelReachability against the current well so the desk always answers the
    // question — with directions, not a dead button. Cached on a coarse position/tank signature so the
    // (transfer-solving) assessment isn't recomputed every render while the captain parks in the desk.
    private string? _fuelDirLine;
    private string? _fuelDirSig;

    private string FuelDirectionsLine()
    {
        if (_ephemeris is null || _simulator is null)
        {
            return "Dock at a station or a haven to take on reaction mass.";
        }

        if (CurrentWellBodyId() is not { } well)
        {
            return "No fuel port in range — steer for a planet's stations or a haven, then dock to fill up.";
        }

        // Round the position to ~1e9 m so the pump assessment recomputes only when the ship really moves.
        string sig = $"{well}|{_reactionMassPulses}|{ReactionMassCapacity}|{(long)(_ship.Position.X / 1e9)}|{(long)(_ship.Position.Y / 1e9)}";
        if (sig != _fuelDirSig)
        {
            _fuelDirSig = sig;
            try
            {
                FuelReachability.Assessment a = FuelReachability.Assess(
                    _simulator, _ephemeris, _ship, _reactionMassPulses, ReactionMassCapacity, well);
                _fuelDirLine = a.NearestDepotBodyId is { } id && a.NearestDepotPulses != int.MaxValue
                    ? $"Nearest pump: {BodyName(id)}, about {a.NearestDepotPulses} p away — plot a course there and dock to fill up."
                    : "No pump reachable from here — burn back toward a planet's stations or a haven to refuel.";
            }
            catch
            {
                _fuelDirLine = "Dock at a station or a haven to take on reaction mass.";
            }
        }

        return _fuelDirLine ?? "Dock at a station or a haven to take on reaction mass.";
    }

    // The heliocentric well the ship sits in: the top-level body (a planet — direct child of the Sun)
    // above the nearest body, whose station/haven children are the pumps FuelReachability prices against.
    private string? CurrentWellBodyId()
    {
        if (_ephemeris is null || _nearestBody is null)
        {
            return null;
        }

        CelestialBody body = _nearestBody;
        while (body.ParentId is { } parentId)
        {
            CelestialBody? parent = null;
            foreach (CelestialBody candidate in _ephemeris.Bodies)
            {
                if (candidate.Id == parentId) { parent = candidate; break; }
            }

            if (parent is null || parent.ParentId is null)
            {
                break; // parent is the parentless root (the Sun) — `body` is the planet-level well
            }

            body = parent;
        }

        return body.Id;
    }
}
