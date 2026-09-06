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
/// Map.UiState — THE DESK SWITCH AND THE CHIPS ON IT. Which duty station the captain is standing at, the
/// order the tab bar deals them in, what each key press does to that, and the one-line summary every desk
/// hangs on its own chip. Carved off <c>Map.razor</c> for #251, motion only: nothing here decides anything
/// about the world, it only says what the chrome is currently showing.
///
/// <para>The rest of the chrome moved out under #251 into partials of its own, by concern —
/// <c>Map.UiState.Menus.cs</c> (the click menus and the picker), <c>Map.UiState.Visibility.cs</c> (hidden
/// bodies, layers, the peek), <c>Map.UiState.Cheats.cs</c> (the dev-start injections) and
/// <c>Map.UiState.NavSearch.cs</c> (the search box). <see cref="TabBarOrder"/> is the file's only static
/// field and stays HERE with the desks it orders, which is #1163's static-class law: initializers of a
/// partial class run in the order the compiler reads the files.</para>
/// </summary>
public partial class Map
{
    // ---- PR-11: duty-station desks (docs/SaturdayPlan/StationDesks.md) ----
    // Every station's Visible flag is now derived from _activeDesk rather than its own bool —
    // the old per-station toggles (_showTracking et al.) are gone; a desk is either the one
    // you're sitting at (full screen) or it isn't (a summary chip on DeskChips).
    private ShipDesk _activeDesk = ShipDesk.Nav;

    // #125 playtest ("I was expecting to find the tab where I left it"): the Captain desk is
    // re-created on every switch, so its own tab state resets. Map holds the last tab here and
    // seeds/receives it via the Captain component's InitialTab / OnTabChanged.
    private SpaceSails.Client.Pages.Stations.Captain.CaptainView _captainTab;

    // PR-15: the tab bar's display order isn't the enum's declaration order — Captain's key is
    // `0`, ahead of Nav's `1`, so it leads the bar too even though its enum value (8, tacked on
    // after Deck so existing desk numbering never shifts) sorts last.
    private static readonly ShipDesk[] TabBarOrder =
        [ShipDesk.Captain, ShipDesk.Nav, ShipDesk.Sensors, ShipDesk.WarRoom, ShipDesk.Trade, ShipDesk.Comms, ShipDesk.Galley, ShipDesk.Deck];

    private static string DeskLabel(ShipDesk desk) => desk switch
    {
        ShipDesk.Nav => "Nav",
        ShipDesk.Sensors => "Sensors",
        ShipDesk.WarRoom => "War room",
        ShipDesk.Trade => "Trade",
        ShipDesk.Comms => "Comms",
        ShipDesk.Galley => "Galley",
        ShipDesk.Deck => "Deck",
        ShipDesk.Captain => "Captain",
        _ => desk.ToString(),
    };

    // Captain's keyboard shortcut is `0`, not its enum value (8) — see OnKeyDown's explicit '0'
    // case and ShipDesk's doc comment for why it's numbered last but keyed first.
    private static string DeskKeyLabel(ShipDesk desk) => desk == ShipDesk.Captain ? "0" : ((int)desk).ToString();

    /// <summary>The one place a desk switch happens, so Deck's special case (it rides the
    /// existing _deckMode flag rather than its own bool) only needs handling once: number keys,
    /// the tab bar, chip clicks and bridge-seat E-interactions (future PR-14) all funnel here.</summary>
    private void SwitchDesk(ShipDesk desk)
    {
        // #585 · THE SHIP'S DESKS ARE ON THE SHIP. Owner, on the regolith: "no more quick buttons when on
        // surface ... we don't just jump to nav of the ship from there", and the rule that follows from it —
        // "we have to go back with shuttle from the shuttle or with other shuttle".
        //
        // He is right and it is the same law as everything else fixed today: the captain is in a suit on a
        // moon and the hull is docked and empty somewhere above. Nav, sensors, the war room, the captain's
        // desk — none of them have a person sitting at them, and a number key that teleports you to the helm
        // makes the excursion a menu you can leave at any time. The walk back to the tube is the ONLY exit,
        // which is what gives the air, the pack and the long way home any weight at all.
        //
        // Gated HERE because this method is documented as the one place a desk switch happens: number keys,
        // the tab bar, the chips and the seat interactions all funnel through it. Nothing new can leak past
        // by forgetting to ask.
        if (_surface is not null && desk != ShipDesk.Deck)
        {
            ShowPulseMessage(
                "🚫 That console is aboard the ship, and you are not. The way back is the shuttle.");
            return;
        }

        // #1021 · THE GALLEY IS NOT A DESK ANY MORE, AND THIS IS THE ONLY PLACE THAT HAS TO KNOW IT.
        //
        // Owner: "this UI MUST GO!... keep the features but I want it done in pop-up style like the work the
        // case is." The full-screen Galley screen is gone from the desk band; what 6 raises is a card over
        // whatever is already on the glass, so the room behind it stays visible — which is the other half of
        // the complaint ("no ... visibility to the bar surroundings").
        //
        // FORKED HERE, ABOVE EVERYTHING, because this method is documented as the one place a desk switch
        // happens: the digit keys, the "6 Galley" tab, the right-rail Galley chip and the captain's status
        // board all funnel through it, so all four become doors onto the card by arriving here and none of
        // them needs to know that. `_activeDesk` is deliberately NOT written — the captain stays sitting
        // where they were sitting, which is what makes this a pop-up and not a desk with a smaller frame.
        //
        // …and it does not put the deck down either. Pressing 6 on the deck used to walk you off it; now the
        // card opens over the deck you are standing on, which at the CANTINA is the whole point.
        if (desk == ShipDesk.Galley)
        {
            ToggleGalleyCard();   // #688's law: the key that opens it closes it
            return;
        }

        if (desk == ShipDesk.Deck)
        {
            if (!_deckMode)
            {
                ToggleDeck();
            }
            _activeDesk = ShipDesk.Deck;
            CloseGalleyCard();
            return;
        }

        if (_deckMode)
        {
            ToggleDeck();
        }
        // #160 routing hook: a one-shot request to land the Captain desk on its Tutorials tab is spent
        // the moment you leave the captain's chair for any other desk. (The #195-removed Nav pop-up used
        // to raise it; the tutorial mission will drive it next.)
        if (desk != ShipDesk.Captain)
        {
            _openCaptainToTutorials = false;
        }
        // #1021 · …and the card does not follow the captain to the next desk. A number key means "take me
        // there", and a pop-up that came along wearing the anchor of the desk it was raised on is #1012's
        // own finding one family over — a card that followed him somewhere and lied about where it was.
        CloseGalleyCard();
        _activeDesk = desk;
    }

    // 2026-07-18 playtest: the mouse's route into a desk. Every clickable desk switch — the tab bar, the
    // pilot banner, the desk chips, the "⚔ war room" jump — funnels through here so the switch happens
    // AND the keyboard comes home to the map div (RefocusMap), instead of dying on the clicked button.
    private async Task SwitchDeskFromClick(ShipDesk desk)
    {
        SwitchDesk(desk);
        await RefocusMap();
    }

    // Addendum (owner, 2026-07-04 evening): a chip is the station's tightest CURRENT-OBJECTIVE
    // summary, not a raw-stats dump — see docs/SaturdayPlan/StationDesks.md.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.DeskChips.ChipData> BuildDeskChips()
    {
        var chips = new List<SpaceSails.Client.Pages.Stations.DeskChips.ChipData>();
        // PR-15: the captain's mission chip docks at the TOP of the strip on every desk but the
        // captain's own — the reserved slot DeskChips.razor's comment refers to.
        if (_activeDesk != ShipDesk.Captain) chips.Add(CaptainChip());
        if (_activeDesk != ShipDesk.Nav) chips.Add(NavChip());
        if (_activeDesk != ShipDesk.Sensors) chips.Add(SensorsChip());
        if (_activeDesk != ShipDesk.WarRoom) chips.Add(WarRoomChip());
        if (_activeDesk != ShipDesk.Trade) chips.Add(TradeChip());
        if (_activeDesk != ShipDesk.Comms) chips.Add(CommsChip());
        // #1021 · THE GALLEY CHIP IS ALWAYS ON THE STRIP NOW, and the `if` that used to guard it is gone
        // rather than left standing as a condition that can no longer be false. The rule the others follow
        // is "every desk but the one you are sitting at", and nobody sits at the galley any more — it is a
        // card over whatever desk you ARE at. The chip stays because it is chrome and a door (clicking it
        // raises the card), which is exactly what the owner kept: "keep the features".
        chips.Add(GalleyChip());
        return chips;
    }

    // M26: the captain's status board — every station's chip, no desk excluded except his own.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.DeskChips.ChipData> AllStationChips() =>
        [NavChip(), SensorsChip(), WarRoomChip(), TradeChip(), CommsChip(), GalleyChip()];

    private SpaceSails.Client.Pages.Stations.DeskChips.ChipData CaptainChip()
    {
        // #207 dock-wins: a berthed ship's chip reads the same truth as the pilot banner — "Docked
        // at X" — never a stale "Make for X · ETA" from a navigation that already completed.
        bool docked = NavLockedByDock;
        string primary = DeskChipStatus.PrimaryLine(docked, DockedBodyName(), _mission.Describe());
        string? eta = DeskChipStatus.EtaLine(docked, _mission.Kind == MissionKind.FlyTo ? DestinationEta() : null);
        // #207: the live contract's next action rides the captain's chip while a job is in hand, so
        // "what to do next" is there where the captain is looking, not only in the Quests tab.
        string? quest = CaptainQuestChipLine();
        return new(ShipDesk.Captain, "☠", "Captain", primary, quest ?? eta, quest is null ? null : eta);
    }

    // The docked haven's real name (always populated while berthed), or null when flying.
    private string? DockedBodyName() => _dockedHavenId is { } id ? BodyName(id) : null;

    private SpaceSails.Client.Pages.Stations.DeskChips.ChipData NavChip()
    {
        // #207 dock-wins: a berthed ship's Nav chip mirrors the banner — "Docked at X" — instead of
        // the stale "→ X" a completed navigation would otherwise leave standing.
        if (NavLockedByDock)
        {
            return new(ShipDesk.Nav, "🧭", "Nav", DeskChipStatus.PrimaryLine(true, DockedBodyName(), ""), null, null);
        }

        // M26: the ETA rides the objective line, so time-to-destination shows on every desk.
        string eta = DestinationEta() is { } e ? $" · {e}" : "";
        // #147 coherence: when the autopilot has stood down, no desk chip may claim a mission it no
        // longer flies. Derive the objective from the SAME source of truth as the pilot banner — the
        // FlightPlanStatus now line — so the chip says "you have the ship", not "→ Titan orbit".
        string objective = AutopilotStoodDown
            ? FlightNowNext().NowLine.Replace("NOW: ", "")
            : _armedOrbitBodyId is not null
                ? $"→ {BodyName(_armedOrbitBodyId)} orbit{eta}"
            : _destinationBodyId is not null
                ? $"→ {BodyName(_destinationBodyId)}{eta}"
                : _planNodes.Count > 0
                    ? "on plotted course"
                    : "free sailing";
        string speedLine = $"{(_ship.Velocity.Length / 1000).ToString("F1", CultureInfo.InvariantCulture)} km/s · {(Paused ? "∥" : $"{Warp}×")}";
        string? passLine = _closestPass is { } cp && (cp.Impact || cp.Severity < 5)
            ? $"pass: {cp.BodyName} {FormatDistance(cp.Distance)}{(cp.Impact ? " IMPACT" : "")}"
            : null;
        return new(ShipDesk.Nav, "🧭", "Nav", objective, speedLine, passLine);
    }

    // #203: which one voice a body speaks — a mass-less (μ≤0) dock haven is clamped, not orbited.
    //
    // #938 D3a / #244 item 1 / a live #212 breach: this used to ask μ ALONE, and μ alone is not the
    // question. sol.json carries three μ=0 stations with no `haven` flag — Mercury Compute Farms, Highport
    // Satellite Works and the Derelict Roadster — while the clamp gate everything else obeys is
    // DockableHavens.IsDockable (IsHaven AND μ≤0). So the whole dock vocabulary fired at a wreck: the map
    // menu offered "navigate to dock" and "✈ Autopilot: dock at Derelict Roadster", the arm hint promised
    // "you press ⚓ Dock at the end", the plan step read "Dock at" — and UpdateDockAffordance, which asks
    // IsDockableHaven, would never put a ⚓ button on the board for any of them. Every one of those
    // sentences named a button that cannot exist. Ask the predicate the clamp itself asks and they all
    // speak orbit instead, in words that already shipped.
    private HarborClass HarborClassOf(string? bodyId)
    {
        CelestialBody? body = bodyId is null ? null : _ephemeris?.Bodies.FirstOrDefault(b => b.Id == bodyId);
        return body is not null && DockableHavens.IsDockable(body) ? HarborClass.Dock : HarborClass.Orbit;
    }

    // #203: captain-facing distance-to-a-body is ALWAYS altitude above the surface, unit-labelled
    // ("alt 313 km") — never the raw orbital radius, which stays in engineering/lab surfaces.
    private static string FormatAltitude(double metersAboveSurface) => $"alt {FormatDistance(metersAboveSurface)}";

    // #203 item 3: the arm action's consequence in one sentence, harbor-aware, for the context menu
    // and the nav-target arm button (extends #197's tooltip standard to the map menus).
    //
    // #244 item 3, follow-up · A WRECK IS ASKED FIRST, and it is asked with the same predicate the two arm
    // buttons above it ask (Derelict.IsWreckBody) rather than with the harbour class. #1125 moved the VERB
    // on those buttons to HarborVocabulary.PickupArmVerb and this tooltip kept promising the ship would slip
    // into orbit here — the button and the sentence under it saying two different things about one press,
    // which is the bug class #938 D3a opened when it took the dock words off a wreck and left the orbit ones.
    private string ArmMenuHint(string? bodyId) =>
        Derelict.IsWreckBody(bodyId) ? HarborVocabulary.PickupArmHint
        : HarborClassOf(bodyId) == HarborClass.Dock
            ? "The autopilot flies the approach, matches speed, and brings you into the dock envelope — you press ⚓ Dock at the end"
            : "The autopilot flies the approach and slips into orbit here when the capture window opens";

    private SpaceSails.Client.Pages.Stations.DeskChips.ChipData SensorsChip()
    {
        string objective = _trackingPost?.ObjectiveSummary(SimTime) ?? "no watch set";
        string tracks = $"{_trackingPost?.Entries.Count ?? 0}/{_telescopeLevel + 1} tracks";
        // M29: the beacon state rides the chip — the captain should never wonder aloud what
        // story the transponder is telling.
        string? beacon = _transponderMode switch
        {
            TransponderMode.Dark => "🕶 running dark",
            TransponderMode.Fake => "🎭 FALSE COLORS — ghost on course",
            _ => null,
        };
        return new(ShipDesk.Sensors, "📡", "Sensors", objective, tracks, beacon);
    }

    private SpaceSails.Client.Pages.Stations.DeskChips.ChipData WarRoomChip()
    {
        double? nearest = NearestHunterDistance();
        string line = _heat.Level <= 0 && nearest is null
            ? "quiet skies"
            : $"heat {HeatFlames(_heat.Level)} · hunter {(nearest is { } d ? FormatDistance(d) : "—")}";
        // M27: the intercept clock rides the chip — the countdown to the initiative roll.
        // M28: and the gun deck's lock countdown outranks it.
        return new(ShipDesk.WarRoom, "⚔", "War room", line, FireChipLine() ?? InterceptChipLine());
    }

    private SpaceSails.Client.Pages.Stations.DeskChips.ChipData TradeChip()
    {
        if (_localTradeTargetId is not null)
        {
            string name = _localTradeTargetId;
            foreach (CommerceRule.LocalContact c in LocalContacts())
            {
                if (c.Id == _localTradeTargetId)
                {
                    name = c.Name;
                    break;
                }
            }

            return new(ShipDesk.Trade, "🛰", "Trade", $"drones → {name} {(int)(_localTradeProgress * 100)}%");
        }

        // M29: advertise the opportunity — partners the shuttles could reach RIGHT NOW.
        int tradable = 0;
        foreach (CommerceRule.LocalContact c in LocalContacts())
        {
            if ((c.Actions & CommerceRule.ActionKind.Trade) != 0 && TradeModeFor(c) != CommerceRule.TradeMode.None)
            {
                tradable++;
            }
        }

        return new(ShipDesk.Trade, "🛰", "Trade", $"{_credits:N0} cr", $"{_cargoUnits}/{CargoCapacity} cargo",
            tradable > 0 ? $"🚀 {tradable} partner{(tradable == 1 ? "" : "s")} in shuttle reach" : null);
    }

    private SpaceSails.Client.Pages.Stations.DeskChips.ChipData CommsChip()
    {
        string? freshest = FreshestIntelCallsign();
        return new(ShipDesk.Comms, "🕸", "Comms", freshest is not null ? $"intel: {freshest}" : "no whispers");
    }

    private SpaceSails.Client.Pages.Stations.DeskChips.ChipData GalleyChip()
    {
        string tots = $"{_rumTots} tot{(_rumTots == 1 ? "" : "s")} poured";
        string wobble = RumWobbleActive ? "wobble active 🍹" : "steady legs";
        return new(ShipDesk.Galley, "🍹", "Galley", tots, wobble);
    }
}
