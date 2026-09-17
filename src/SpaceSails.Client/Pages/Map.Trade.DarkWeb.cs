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
/// PR-6 · THE DARK SPACE WEB — the market that only answers where the light is thin: whether it will
/// trade here at all and the honest refusal when it will not, the ships for sale, the wire contacts, and
/// the tracked-ship read-model the ledger's <i>"→ dark web"</i> link opens onto.
///
/// <para>Split out of <c>Map.Trade.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // The ledger's "→ dark web" link: switch to Comms and select the dark-web node (the market view).
    private void OpenDarkWebFromLedger()
    {
        SwitchDesk(ShipDesk.Comms);
        _commsSelectedId = "darkweb";
    }

    // ---- PR-6: the dark space web ----

    /// <summary>The body the player is currently berthed at, docked near or orbiting (bound, M20 sense) —
    /// null if none of the three, in which case the dark web has nowhere to set up shop.
    ///
    /// <para>#1217: this used to ask its own question and forget the clamp, which left the desk offline at
    /// every station haven in the game — the only places it was ever meant to open, and the only places the
    /// fiction ever sends you. It now asks the one question <see cref="WhereTheShipIsStanding"/> states, so
    /// the desk and the HUD banner can never again disagree about whether the ship is anywhere.</para>
    /// </summary>
    private CelestialBody? DarkWebCurrentBody() => WhereTheShipIsStanding();

    private bool DarkWebCanTrade()
    {
        if (DarkWebCurrentBody() is not { } body)
        {
            return false;
        }

        return IntelMarket.CanTradeIntelAt(body, _ephemeris!.Position(body.Id, SimTime).Length);
    }

    private string DarkWebDisabledReason()
    {
        if (DarkWebCurrentBody() is not { } body)
        {
            return "Not orbiting or docked anywhere — get to a haven or a far trading post first.";
        }

        return IntelMarket.CanTradeIntelAt(body, _ephemeris!.Position(body.Id, SimTime).Length)
            ? ""
            : $"{body.Name} doesn't deal in stolen timetables — try a haven or a station past Mars.";
    }

    private double DarkWebDistanceFromEarth()
    {
        if (_ephemeris is null || DarkWebCurrentBody() is not { } body)
        {
            return 0;
        }

        Vector2d earth = _ephemeris.Position("earth", SimTime);
        Vector2d here = _ephemeris.Position(body.Id, SimTime);
        return (here - earth).Length;
    }

    // Thin, read-only projection of the off-the-books NPCs the market knows about — same
    // philosophy as TrackingCandidates(): the dark web component never sees Map.razor's own
    // NpcState type.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.MarketShip> DarkWebMarketShips()
    {
        var ships = new List<SpaceSails.Client.Pages.Stations.DarkWeb.MarketShip>();
        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Ship.PublishesTimetable && npc.Active && !npc.Arrived)
            {
                ships.Add(new SpaceSails.Client.Pages.Stations.DarkWeb.MarketShip(
                    npc.Ship.Id, npc.Ship.Callsign, npc.Ship.CargoClass, npc.Ship.CargoUnits, RouteLabel(npc.Ship)));
            }
        }

        return ships;
    }

    // PR-WIRE — the wire-capable contacts for the dark-web favor-bank panel: every contact we have
    // history with whose character sheet banks over the wire (ruling 6). In-person-only contacts (the
    // hermit, the Magpie) are excluded — you bank those across their table, not the dark web.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.WireContact> DarkWebWireContacts()
    {
        var rows = new List<SpaceSails.Client.Pages.Stations.DarkWeb.WireContact>();
        foreach ((string id, ContactHistory h) in _contacts.Entries)
        {
            ContactSheet sheet = ContactSheets.For(id);
            if (!sheet.CanWire)
            {
                continue;
            }
            rows.Add(new SpaceSails.Client.Pages.Stations.DarkWeb.WireContact(id, sheet.DisplayName, h.CreditBalance));
        }
        rows.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
        return rows;
    }

    // Thin, read-only projection of the tracking-post ledger + live NPC state, for the dark
    // web's sell/tight-beam/laser-ranging sections.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.TrackedShipInfo> DarkWebTrackedShips()
    {
        var list = new List<SpaceSails.Client.Pages.Stations.DarkWeb.TrackedShipInfo>();
        if (_trackingPost is null)
        {
            return list;
        }

        foreach (TrackedTarget entry in _trackingPost.Entries)
        {
            NpcState? npc = null;
            foreach (NpcState candidate in _npcStates)
            {
                if (candidate.Ship.Id == entry.ShipId) { npc = candidate; break; }
            }

            Vector2d position = npc?.State.Position ?? entry.LastObservation.Position;
            Vector2d velocity = npc?.State.Velocity ?? entry.LastObservation.Velocity;
            list.Add(new SpaceSails.Client.Pages.Stations.DarkWeb.TrackedShipInfo(
                entry.ShipId,
                npc?.Ship.Callsign ?? entry.ShipId,
                npc?.Ship.CargoClass ?? "Unknown",
                npc?.Ship.CargoUnits ?? 0,
                entry.EffectiveQuality(SimTime),
                position,
                velocity,
                npc?.Ship.PublishesTimetable ?? true,
                npc is not null ? BodyName(npc.Ship.DestinationId) : "unknown"));
        }

        return list;
    }
}
