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
/// PR-5 · ORBITAL COMMERCE — trading from orbit or course-matched, with drones doing the crossing.
///
/// <para>The context body is the one the ship is actually bound to, or — per the owner's
/// <i>"orbiting/near a body"</i> phrasing — whatever body is nearest when she is bound to nothing, so the
/// panel still has something useful to show while cruising past a bus stop. The buy and the sell are the
/// same drone run priced two ways, and a transfer in flight is cancelled by name rather than abandoned.</para>
///
/// <para>Split out of <c>Map.Trade.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ---- PR-5: orbital commerce — trade from orbit or course-matched with drones ----

    // The context body Local Space shows contacts for: the body the ship is actually bound to, or
    // (per the owner's "orbiting/near a body" phrasing) whatever body is nearest when not bound —
    // so the panel still has something useful to show while just cruising close to a bus stop.
    private string? LocalSpaceBodyId => _orbitedBodyId ?? _nearestBody?.Id;

    // Thin, read-only projection of the live NPC/depot list for CommerceRule — mirrors
    // TrackingCandidates()'s role for the tracking post.
    private IReadOnlyList<CommerceRule.LocalShip> LocalShips()
    {
        var ships = new List<CommerceRule.LocalShip>(_npcStates.Length);
        foreach (NpcState npc in _npcStates)
        {
            // StepNpcs's despawn check flags a depot "Arrived" the instant it's created (its
            // destination IS its own body, always within despawn tolerance) — correct for the
            // traffic board's status column, wrong here: a depot is a perpetual fixture, never
            // actually gone. Only exclude Arrived for ships that can genuinely leave.
            if (npc.Arrived && npc.Ship.DepotBodyId is null)
            {
                continue;
            }

            ships.Add(new CommerceRule.LocalShip(npc.Ship.Id, npc.Ship.Callsign, npc.State, npc.Ship.DepotBodyId,
                npc.Ship.CargoClass, RemainingStock(npc)));
        }

        return ships;
    }

    /// <summary>What a post still has for sale: its manifest minus what the player has bought,
    /// zero once boarded (plundered empty is empty).</summary>
    private static int RemainingStock(NpcState npc) =>
        npc.Boarded ? 0 : Math.Max(0, npc.Ship.CargoUnits - npc.CargoSoldToPlayer);

    private IReadOnlyList<CommerceRule.LocalContact> LocalContacts()
    {
        if (_ephemeris is null)
        {
            return [];
        }

        // M29: what's "here" (keyed to the context body) UNION everything the shuttles could
        // reach — a slow pass millions of km from a station is a real trading opportunity now.
        var contacts = new List<CommerceRule.LocalContact>();
        var seen = new HashSet<string>();
        if (LocalSpaceBodyId is { } bodyId)
        {
            foreach (CommerceRule.LocalContact c in CommerceRule.ContactsAt(_ephemeris, LocalShips(), SimTime, bodyId))
            {
                if (seen.Add(c.Id))
                {
                    contacts.Add(c);
                }
            }
        }

        foreach (CommerceRule.LocalContact c in CommerceRule.ContactsWithinShuttleRange(_ephemeris, LocalShips(), SimTime, _ship))
        {
            if (seen.Add(c.Id))
            {
                contacts.Add(c);
            }
        }

        return contacts;
    }

    // M29: the tier the deal would move by, for a given contact — chip, badge, button and
    // break-off all agree because they all ask this.
    private CommerceRule.TradeMode TradeModeFor(CommerceRule.LocalContact contact)
    {
        string? partnerOrbitBodyId = contact.Kind == CommerceRule.LocalContactKind.Ship ? null : LocalSpaceBodyId;
        return CommerceRule.Classify(_ship, contact.Position, contact.Velocity, _orbitedBodyId, partnerOrbitBodyId);
    }

    private void StartLocalTrade(string contactId)
    {
        if (_localTradeTargetId is not null)
        {
            return;
        }

        if (_cargoValue <= 0)
        {
            // The button used to silently no-op here — never a mute click again.
            ShowPulseMessage("Hold empty — nothing to sell. Buy from a post or board a prize first.");
            return;
        }

        _localTradeTargetId = contactId;
        _localTradeIsBuy = false;
        _localTradeProgress = 0;
        CommerceRule.TradeMode mode = CommerceRule.TradeMode.DroneMatch;
        foreach (CommerceRule.LocalContact c in LocalContacts())
        {
            if (c.Id == contactId) { mode = TradeModeFor(c); break; }
        }

        _localTradeMessage = mode == CommerceRule.TradeMode.Shuttle
            ? "Shuttles away — long corridor, cargo transfer in progress 🚀"
            : "Drones away — cargo transfer in progress";
    }

    // ---- Buying (owner: "How do I buy anything from Earth Depot?"): the honest other half.
    // Same transfer clock, envelope rules and break-off as selling; the units and the price
    // are locked when the shuttles leave, credits change hands when they're back. ----
    private bool _localTradeIsBuy;
    private int _localBuyUnits;
    private int _localBuyCostCr;
    private string _localBuyClass = "";

    private void StartLocalBuy(string contactId)
    {
        if (_localTradeTargetId is not null)
        {
            return;
        }

        CommerceRule.LocalContact? contact = null;
        foreach (CommerceRule.LocalContact c in LocalContacts())
        {
            if (c.Id == contactId) { contact = c; break; }
        }

        if (contact is not { CargoClass: { } cargoClass, CargoUnits: > 0 } post)
        {
            ShowPulseMessage("Nothing left on that post's manifest");
            return;
        }

        CommerceRule.TradeMode mode = TradeModeFor(post);
        if (mode == CommerceRule.TradeMode.None)
        {
            ShowPulseMessage($"Out of reach — close within {FormatDistance(CommerceRule.ShuttleRangeMeters)} under {CommerceRule.ShuttleMaxRelativeSpeed / 1000:F0} km/s rel");
            return;
        }

        int units = CommerceRule.MaxBuyableUnits(
            mode, post.CargoUnits, CargoCapacity - _cargoUnits, _credits, CargoMarket.UnitValue(cargoClass));
        if (units <= 0)
        {
            ShowPulseMessage(CargoCapacity - _cargoUnits <= 0
                ? "Hold full — sell or fence something first"
                : "Not enough credits for even one unit plus the ferry fee");
            return;
        }

        _localTradeTargetId = contactId;
        _localTradeIsBuy = true;
        _localTradeProgress = 0;
        _localBuyUnits = units;
        _localBuyClass = cargoClass;
        _localBuyCostCr = CommerceRule.BuyCostCr(mode, units, CargoMarket.UnitValue(cargoClass));
        _localTradeMessage = mode == CommerceRule.TradeMode.Shuttle
            ? $"Shuttles away — buying {units}u {cargoClass} for {_localBuyCostCr:N0} cr 🚀"
            : $"{(mode == CommerceRule.TradeMode.SameOrbit ? "Dockside crew" : "Drones")} loading {units}u {cargoClass} — {_localBuyCostCr:N0} cr";
    }

    private void CancelLocalTrade(string message)
    {
        _localTradeTargetId = null;
        _localTradeProgress = 0;
        _localTradeMessage = message;
    }

    // Drone transfer progress accrues in REAL time (M14's boarding-shuttle pattern) so warping
    // doesn't fast-forward a transfer. Breaks off — progress lost — the moment the envelope
    // (CommerceRule.CanTrade) stops holding, e.g. the target ship burns away mid-transfer.
    private void UpdateLocalTrade(double dtRealSeconds)
    {
        if (_localTradeTargetId is not { } targetId)
        {
            return;
        }

        if (_ephemeris is null || (!_localTradeIsBuy && _cargoValue <= 0))
        {
            CancelLocalTrade("Drone transfer aborted");
            return;
        }

        CommerceRule.LocalContact? target = null;
        foreach (CommerceRule.LocalContact c in LocalContacts())
        {
            if (c.Id == targetId) { target = c; break; }
        }

        if (target is not { } contact || (contact.Actions & CommerceRule.ActionKind.Trade) == 0)
        {
            CancelLocalTrade("Drones lost the contact — transfer aborted");
            return;
        }

        CommerceRule.TradeMode mode = TradeModeFor(contact);
        if (mode == CommerceRule.TradeMode.None)
        {
            CancelLocalTrade("Envelope lost — shuttles and drones recalled, transfer aborted");
            return;
        }

        bool shuttle = mode == CommerceRule.TradeMode.Shuttle;
        double relSpeed = (_ship.Velocity - contact.Velocity).Length;
        double distance = (_ship.Position - contact.Position).Length;
        int transferUnits = _localTradeIsBuy ? _localBuyUnits : _cargoUnits;
        double seconds = CommerceRule.TransferSeconds(mode, relSpeed, distance, transferUnits);
        _localTradeProgress += Math.Clamp(dtRealSeconds, 0, 0.1) / seconds;

        if (_localTradeProgress >= 1)
        {
            if (_localTradeIsBuy)
            {
                CompleteLocalBuy(contact, mode);
            }
            else
            {
                CompleteLocalSell(mode, shuttle);
            }

            _localTradeTargetId = null;
            _localTradeProgress = 0;
            RendererInterop.PlayCue("board");
        }
        else
        {
            _localTradeMessage = _localTradeIsBuy
                ? $"{(shuttle ? "Shuttles hauling" : "Loading")} {_localBuyUnits}u {_localBuyClass} — {(int)(_localTradeProgress * 100)}%"
                : $"{(shuttle ? "Shuttles flying the corridor" : "Drones ferrying")} — {(int)(_localTradeProgress * 100)}%";
        }
    }

    private void CompleteLocalSell(CommerceRule.TradeMode mode, bool shuttle)
    {
        int units = _cargoUnits;
        int payout = CommerceRule.SellPayoutCr(mode, units, _cargoValue);
        int fee = CommerceRule.TransferFeeCr(mode, units);
        _credits += payout;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();
        _localTradeMessage = fee > 0
            ? $"{(shuttle ? "Shuttles" : "Drones")} delivered {units} units — {payout:N0} cr after the {fee:N0} cr ferry fee"
            : $"Delivered {units} units for {payout:N0} credits";
        AdvanceTutorial(4); // step 5: first sale (same milestone the dock's SellCargo hits)
        AdvanceTutorial(StepSellHe3); // second hunt, step 6: fencing the He3 closes the tutorial
    }

    private void CompleteLocalBuy(CommerceRule.LocalContact contact, CommerceRule.TradeMode mode)
    {
        // Re-check at handover: credits may have been spent and the manifest may have been
        // plundered while the shuttles were flying. Take what is still takeable, pay for that.
        NpcState? seller = FindNpc(contact.Id);
        int stock = seller is null ? contact.CargoUnits : RemainingStock(seller);
        int units = CommerceRule.MaxBuyableUnits(
            mode, Math.Min(stock, _localBuyUnits), CargoCapacity - _cargoUnits, _credits,
            CargoMarket.UnitValue(_localBuyClass));
        if (units <= 0)
        {
            _localTradeMessage = "Shuttles returned empty — the deal fell through at handover";
            return;
        }

        int cost = CommerceRule.BuyCostCr(mode, units, CargoMarket.UnitValue(_localBuyClass));
        _credits -= cost;
        _cargoUnits += units;
        _cargoValue += units * CargoMarket.UnitValue(_localBuyClass);
        _cargoByClass[_localBuyClass] = _cargoByClass.GetValueOrDefault(_localBuyClass) + units;
        if (seller is not null)
        {
            seller.CargoSoldToPlayer += units;
        }

        int fee = CommerceRule.TransferFeeCr(mode, units);
        _localTradeMessage = fee > 0
            ? $"Bought {units}u {_localBuyClass} for {cost:N0} cr ({fee:N0} cr of it ferry fee)"
            : $"Bought {units}u {_localBuyClass} dockside for {cost:N0} cr — no ferry fee";
    }
}
