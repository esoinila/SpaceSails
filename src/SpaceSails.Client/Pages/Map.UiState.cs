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

// Map.UiState — the chrome: the desk switch and chips, the pick/ship/body/corridor context
// menus, map layers, the cheats, and assorted panel toggles. Carved off Map.razor for #251, motion only.
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
        if (desk == ShipDesk.Deck)
        {
            if (!_deckMode)
            {
                ToggleDeck();
            }
            _activeDesk = ShipDesk.Deck;
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
        if (_activeDesk != ShipDesk.Galley) chips.Add(GalleyChip());
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
    private HarborClass HarborClassOf(string? bodyId)
    {
        CelestialBody? body = bodyId is null ? null : _ephemeris?.Bodies.FirstOrDefault(b => b.Id == bodyId);
        return body is { Mu: <= 0 } ? HarborClass.Dock : HarborClass.Orbit;
    }

    // #203: captain-facing distance-to-a-body is ALWAYS altitude above the surface, unit-labelled
    // ("alt 313 km") — never the raw orbital radius, which stays in engineering/lab surfaces.
    private static string FormatAltitude(double metersAboveSurface) => $"alt {FormatDistance(metersAboveSurface)}";

    // #203 item 3: the arm action's consequence in one sentence, harbor-aware, for the context menu
    // and the nav-target arm button (extends #197's tooltip standard to the map menus).
    private string ArmMenuHint(string? bodyId) => HarborClassOf(bodyId) == HarborClass.Dock
        ? "The autopilot flies the approach, matches speed, and brings you into the dock envelope — you press ⚓ Dock at the end"
        : "The autopilot flies the approach and slips into orbit here when the capture window opens";

    // #203 item 3: each disambiguation-picker entry states what opening it will do.
    private static string PickHint(PickCandidate pick) => pick.Kind switch
    {
        'S' => "Open this contact — track it, mark a target of interest, or set up a trade run",
        'H' => "Open this hunter — put the scope on it, or work a firing solution in the war room",
        'B' => "Open this body — set it as your destination or arm the autopilot to it",
        _ => "Open the one you meant",
    };

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

    // Tuesday plan PR-A (the hunt is the quest): a hidden body is on its rail but off the charts —
    // it doesn't draw, answer the picker, ride the scope carousel, count as "Nearest", or open a
    // body menu until a targeted, intel-fed scan reveals it. `_hiddenBodyIds` is the scenario's
    // "hidden":true set (loaded once); `_revealedBodyIds` is the session's found set (reveal is
    // session-scoped, matching the save model). A body is CHARTED when it isn't hidden, or has been
    // revealed.
    private readonly HashSet<string> _hiddenBodyIds = [];
    private readonly HashSet<string> _revealedBodyIds = [];

    // A hidden body still on the charts' blind side — everything player-facing must skip it.
    private bool IsBodyHidden(string id) => _hiddenBodyIds.Contains(id) && !_revealedBodyIds.Contains(id);

    // Bring a hidden body onto the charts for the rest of the session: fires a payoff line + cue,
    // repaints. Idempotent — a second reveal (or revealing a plainly-visible body) is a no-op.
    private void RevealBody(string id, string reason, bool announce = true)
    {
        if (!_hiddenBodyIds.Contains(id) || !_revealedBodyIds.Add(id))
        {
            return;
        }
        if (announce)
        {
            ShowPulseMessage(reason);
            RendererInterop.PlayCue("reveal");
        }
        _passDirty = true;
        StateHasChanged();
    }
    private CelestialBody? _bodyMenuBody;   // planet click menu: which body, where on screen
    private double _bodyMenuX, _bodyMenuY;

    private void CloseBodyMenu()
    {
        _bodyMenuBody = null;
        StateHasChanged();
    }

    // ---- #253: keep every click menu inside the viewport ----
    // The owner's playtest: The Tilt sat bottom-right, its menu ran off the bottom and hid the 🚀
    // Long haul action. The map menus can't measure themselves without interop, so we estimate the
    // box deterministically — the CSS max-width, plus a per-row height × the rows the menu will draw
    // (header chrome folded into the base) — and hand it to MenuLayout, which flips it above/left of
    // the click near an edge. Over-estimating rows only flips a touch early; it never overflows.
    private const double MenuBoxWidthPx = 256;  // .map-body-menu max-width: 16rem
    private const double MenuRowPx = 30;        // one btn-sm/info line + the d-grid gap
    private const double MenuChromePx = 44;      // p-2 padding + the title/close header row

    /// <summary>The on-screen top-left for a menu anchored at the click, sized from its visible row
    /// count, clamped so no action ever renders past a viewport edge (#253).</summary>
    private (double X, double Y) ClampMenu(double anchorX, double anchorY, int rows) =>
        MenuLayout.ClampMenuPosition(
            anchorX, anchorY,
            MenuBoxWidthPx, MenuChromePx + rows * MenuRowPx,
            _viewportWidth, _viewportHeight);

    // Dev cheat (/map?fetch=intel|active|picked): drop a fetch job straight into the ledger at a
    // stage, so a playtester can test each leg without flying the ones between.
    //   intel  = the new first stage: accepted, wreck HIDDEN, transponder fix in the Comms ledger.
    //   active = post-scan (backward-compatible): accepted AND wreck already charted.
    //   picked = charted AND already lifted (fly nowhere — hand off on the spot).
    // The drop-off is the interior station you're standing in (so "picked" can be delivered on the
    // spot), else the first interior station. Pair intel with ?start=wreck to test the proximity pickup.
    private void InjectFetchCheat(string stage)
    {
        const string wreckId = "derelict-roadster";
        if (_ephemeris is null || _quests.Any(q => q.Kind == QuestKind.Fetch)
            || _ephemeris.Bodies.All(b => b.Id != wreckId))
        {
            return;
        }
        string? dest = _dockedHavenId is { } here && HavenInterior.HasInterior(here)
            ? here
            : _ephemeris.Bodies.FirstOrDefault(b => b.IsHaven && HavenInterior.HasInterior(b.Id))?.Id;
        if (dest is null)
        {
            return;
        }
        string destName = _ephemeris.Bodies.First(b => b.Id == dest).Name;
        _quests.Add(new Quest($"fetch-{++_questSeq}", QuestKind.Fetch, "THE FIXER",
            "", destName, "Fetch the roadster's lost wallet",
            "[test] a fetch job, dropped straight into the ledger.", 4200,
            DestBodyId: dest, SourceBodyId: wreckId)
        {
            State = stage == "picked" ? QuestState.PickedUp : QuestState.Active,
        });

        if (stage == "intel")
        {
            // Pre-scan: leave the wreck hidden and seed the transponder fix (the hunt's first stage).
            if (IsBodyHidden(wreckId) && !_scopeIntel.Any(si => si.BodyId == wreckId))
            {
                _scopeIntel.Add(BuildWreckIntel(wreckId, "THE FIXER", DockedStationName()));
            }
        }
        else
        {
            // active / picked are post-scan states: the wreck is already charted.
            RevealBody(wreckId, "", announce: false);
        }
        ShowPulseMessage($"🧪 Test: fetch job injected ({stage}) — hand off to The Fixer at {destName} — filed in the Captain's ledger (0).");
    }

    // Dev cheat (/map?start=<station>&crack=active|picked): drop a hatch-crack job into the ledger at a
    // stage. Targets the first locked department hatch on the docked station's deck, quoting its real
    // code, so a playtester can key the pad (active) or just hand off (picked) without taking a fetch first.
    private void InjectCrackCheat(string stage)
    {
        if (_ephemeris is null || _dockedHavenId is not { } here || _quests.Any(q => q.Kind == QuestKind.Crack))
        {
            return;
        }
        DeckPlan.ConsoleSpot? target = _deckPlan.Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.Hatch && c.Label.Contains("🔒", StringComparison.Ordinal))
            .OrderBy(c => c.Label, StringComparer.Ordinal)
            .Cast<DeckPlan.ConsoleSpot?>()
            .FirstOrDefault();
        if (target is not { } hatch)
        {
            return;
        }
        string id = HatchId(hatch.Label);
        string dept = HatchDept(hatch.Label);
        string pin = MakePin(id);
        _quests.Add(new Quest($"crack-{++_questSeq}", QuestKind.Crack, "THE FIXER", id,
            $"the {dept.ToLowerInvariant()} package", $"Crack hatch {id}",
            "[test] a hatch-crack job, dropped straight into the ledger.", 2600,
            DestBodyId: here, SourceBodyId: here, Pin: pin)
        {
            State = stage == "picked" ? QuestState.PickedUp : QuestState.Active,
        });
        ShowPulseMessage($"🧪 Test: crack job injected ({stage}) — hatch {id}, code {pin}.");
    }

    // Dev cheat (/map?start=cinder-roost&backroom=open|quest): the "doors that grow the world" test hook
    // (PR-F). `open` welds the station's authored wing (Cinder Roost's Bonded Stores back room) on right
    // away, so the grown room is walkable in seconds. `quest` stages the crack job that opens it — with
    // the hatch's real code — so a playtester can key the pad and watch the room appear.
    private void InjectBackroomCheat(string stage)
    {
        if (_dockedHavenId is not { } here)
        {
            ShowPulseMessage("🧪 backroom cheat needs a docked station — try ?start=cinder-roost&backroom=open.");
            return;
        }
        string? hatchId = HavenInterior.WingCatalog(here).FirstOrDefault()?.UnlockHatchId;
        if (hatchId is null)
        {
            ShowPulseMessage($"🧪 No runtime wing is authored at {DockedStationName()} — try Cinder Roost.");
            return;
        }
        if (stage == "open")
        {
            UnlockHatch(here, hatchId);
            ShowPulseMessage($"🧪 Test: {hatchId}'s back room welded open — head west off the concourse (📂) and walk in.");
            return;
        }
        // stage == "quest": drop the crack job in Active (no fetch prerequisite), quoting the real code.
        if (_quests.Any(q => q.Kind == QuestKind.Crack))
        {
            return; // one break-in at a time
        }
        string pin = MakePin(hatchId);
        _quests.Add(new Quest($"crack-{++_questSeq}", QuestKind.Crack, "THE FIXER", hatchId,
            "the bonded stores package", $"Crack hatch {hatchId}",
            "[test] the world-growing crack job — key the pad, then step into the room it opens.", 2600,
            DestBodyId: here, SourceBodyId: here, Pin: pin)
        {
            State = QuestState.Active,
        });
        ShowPulseMessage($"🧪 Test: crack job staged — hatch {hatchId}, code {pin}. Knock to key it, then walk into the back room it opens.");
    }

    // Dev cheat (/map?tip=route): drop a representative route tip — with provenance — into the ledger,
    // so the Captain's-ledger Tips & intel rendering (route line, "→ dark web"/"→ dossier", the day-N
    // attribution) is reachable in seconds without walking a bar. Prefers an off-books ghost so the tip
    // has real teeth (a ship you couldn't otherwise see), else any live ship.
    private void InjectTipCheat()
    {
        NpcState? subject = _npcStates
            .Where(n => n.Active && !n.Ship.PublishesTimetable && !_intelLedger.Knows(n.Ship.Id, SimTime))
            .OrderBy(n => n.Ship.Id, StringComparer.Ordinal)
            .FirstOrDefault()
            ?? _npcStates.FirstOrDefault(n => n.Active)
            ?? _npcStates.FirstOrDefault();
        if (subject is null)
        {
            return;
        }
        _intelLedger.Add(new RouteIntel(subject.Ship.Id, SimTime, RouteIntel.DefaultValiditySeconds, Price: 0));
        _routeIntelProvenance[subject.Ship.Id] = new IntelProvenance("GILT-EYE", DockedStationName(), SimTime);
        ShowPulseMessage($"🧪 Test: route tip on {subject.Ship.Callsign} — filed in the Captain's ledger (0).");
    }

    // #223 dev cheat: seed the ledger's 🗺 section without a full bury run. "mine" buries one of OUR
    // chests on Phobos; "rumor" is the standalone PURCHASE path — pay a barfly for a map to an NPC hoard
    // (deliverable 5), no delivery strings, keep whatever we dig; "both" seeds one of each.
    private void InjectHoardCheat(string mode)
    {
        if (mode is "mine" or "both")
        {
            _caches.Bury("phobos", coin: 1800, [new CacheCargo("He3", 3, Hot: true)], SimTime, "you", playerOwned: true);
            SeedDiscoveryWatch();
        }
        if (mode is "rumor" or "both")
        {
            BuyRumorMap($"cheat|{DockedStationName()}");
        }
        ShowPulseMessage("🧪 Test: hoard seeded — open the Captain's ledger (0) → 🗺 Treasure maps.");
    }

    // Owner request: momentarily hide every panel to read the map, then bring them back. Pure
    // presentation — the sim, the active desk and all state are untouched; only the overlay
    // opacity/hit-testing changes (see .map-peek in Map.razor.css).
    private bool _peekMap;

    private void TogglePeekMap() => _peekMap = !_peekMap;

    // 2026-07-18 playtest: the peek button shares the desk-tab bar, so a click stole focus off the map div
    // the same way a tab did. The mouse toggles peek through here so the keyboard comes home; the ` hotkey
    // still calls TogglePeekMap directly (it already owns focus).
    private async Task TogglePeekMapFromClick()
    {
        TogglePeekMap();
        await RefocusMap();
    }

    // M20: ships and pods are clickable on the map — same effect as picking their traffic row.
    // ---- The unified picker (owner: "hard to click things that are close by"; Gemini
    // consult: forgiving radius, chooser when several objects stack, lanes always last) ----

    private readonly record struct PickCandidate(char Kind, string Id, string Label, string Icon);

    private List<PickCandidate>? _pickMenu;
    private double _pickMenuX, _pickMenuY;

    private const double PickRadiusPx = 15;     // the forgiving direct-hit radius
    private const double PickNearRadiusPx = 28; // near-miss radius for the lane-vs-planet tiebreak
    // #402 follow-up: the per-body pick radius (and the deflection threat rock's widened, always-one-
    // click-away tolerance) is the pure, tested MapPick rule in Core — see the picker loops below.

    private void ClosePickMenu()
    {
        _pickMenu = null;
        StateHasChanged();
    }

    /// <summary>Everything a click at (x, y) could plausibly mean, ranked by likely intent:
    /// live contacts first, then depots, then bodies (each group nearest-first). Corridors and
    /// the empty-sky scan are appended by the pointer-up path — never here — so they always
    /// rank last, per the owner's rule that a lane is the LEAST likely meaning near a planet.</summary>
    private List<PickCandidate> CollectPointCandidates(double x, double y, double radiusPx)
    {
        var found = new List<(PickCandidate Pick, double DistSq, int Rank)>();
        double r2 = radiusPx * radiusPx;
        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Active || npc.Arrived) continue;
            bool isDepot = npc.Ship.DepotBodyId is not null;
            // PR-C (the Barnacle case): the dim last-seen marker answers clicks too — its menu
            // just offers a scan instead of live vitals. Nothing visible on the sky is mute.
            Vector2d position;
            if (npc.CurrentlyObserved)
            {
                position = npc.State.Position;
            }
            else if (npc.LastObservation is { } lastSeen)
            {
                position = lastSeen.Position;
            }
            else
            {
                continue;
            }

            // 🗺 Layers (#405): a hidden class stops answering clicks too, matched to the draw path —
            // depots ride ports.depots; a live contact vs its last-seen ghost split traffic's leaves.
            string trafficLeaf = isDepot ? "ports.depots" : npc.CurrentlyObserved ? "traffic.live" : "traffic.ghosts";
            if (!LayerVisible(trafficLeaf)) continue;

            (float sx, float sy) = _camera.WorldToScreen(position);
            double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
            if (d2 <= r2)
            {
                // #208: the depot twin of a port carries its kind AND a phrase, so it never reads as
                // a second look-alike of the dock haven ("Rusty Roadstead Depot · depot — cargo pod"
                // vs "Rusty Roadstead · dock haven — walk ashore"). Dock at havens; board depots.
                string flavor = isDepot ? "depot — cargo pod" : npc.CurrentlyObserved ? "contact" : "last seen";
                found.Add((new PickCandidate('S', npc.Ship.Id, $"{npc.Ship.Callsign} · {flavor}", isDepot ? "📦" : "🛰"),
                    d2, isDepot ? 1 : 0));
            }
        }

        // A hunter isn't in _npcStates, so it was never in this list — clicking the Debt Collector
        // hit empty sky. It's the most click-worthy thing on the screen: picking it locks the
        // war-room interest target (bracket + firing solution). Ranked ahead of haulers on a tie.
        foreach (HunterState hunter in _hunters)
        {
            if (hunter.BrokenOff || hunter.CaughtPlayer) continue;
            (float sx, float sy) = _camera.WorldToScreen(hunter.State.Position);
            double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
            if (d2 <= r2)
            {
                found.Add((new PickCandidate('H', hunter.Id, $"{hunter.Callsign} · hunter", "🐺"), d2, -1));
            }
        }

        if (_ephemeris is not null)
        {
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (IsBodyHidden(body.Id)) continue;          // a hidden body doesn't answer the picker (PR-A)
                if (body.ParentId is null) continue;          // the sun is not a destination you orbit
                if (body.Kind == BodyKind.Station) continue;  // a barge has no Hill sphere — dock, don't orbit
                (float sx, float sy) = _camera.WorldToScreen(_ephemeris.Position(body.Id, SimTime));
                // Hit within the drawn disc, floored for pinprick planets and capped so a
                // zoomed-in world doesn't swallow every camera drag on the screen.
                double drawnPx = body.BodyRadius / _camera.MetersPerPixel;
                // #402: the deflection inbound rock is the most click-worthy thing in a station
                // cluster — name it as the threat (not a nameless "body") so the pick-menu answer at
                // the Ringside knot reads "⚠ Inbound rock — deflection target" against the depots.
                bool isDeflectionRock = _deflection is { } dgig && body.Id == dgig.RockBodyId;
                // #402 follow-up: the threat rock gets MapPick's widened tolerance so it's always one
                // click away — a click on the station knot lands it even when its own disc is a pinprick.
                double hit = isDeflectionRock
                    ? MapPick.ThreatRockHitRadiusPx(drawnPx, radiusPx)
                    : MapPick.BodyHitRadiusPx(drawnPx, radiusPx);
                double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
                if (d2 <= hit * hit)
                {
                    // #208: a moon haven (parked-in, no ⚓ dock) carries its walk-in phrase too, so the
                    // picker's port entries all read their kind at a glance.
                    (string flavor, string icon) = isDeflectionRock
                        ? ("deflection target — on a collision course", "⚠")
                        : body.IsHaven ? ("haven — lie low in orbit", "🏴") : ("body", "🪐");
                    // #339-follow: a shuttle-landable ground (a moon) names its 🛬 mark and its live reach —
                    // "shuttle range" when the map glyph is bright, "out of shuttle reach" when it's dim — so
                    // the click hint says the same thing the glyph shows (the #195 all-controls-hinted law).
                    if (ShuttleExcursion.IsLandableSurface(body.Kind))
                    {
                        flavor += _landableInRangeIds.Contains(body.Id)
                            ? " · 🛬 landable — shuttle range"
                            : " · 🛬 landable — out of shuttle reach";
                    }
                    // #402: rank the threat rock ahead of ordinary bodies/depots so it heads the
                    // "which one?" list in a cluster (like the hunter is promoted above haulers).
                    found.Add((new PickCandidate('B', body.Id, $"{body.Name} · {flavor}", icon), d2, isDeflectionRock ? -1 : 2));
                }
            }

            // Haven docks are stations (skipped above — you clamp, not orbit), but they must still be
            // pickable: stacked over their planet, the ⚓ in the name calls out the one you can dock at
            // right there in the "which one?" list — no zooming in to tell them apart (owner's ask).
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (IsBodyHidden(body.Id)) continue; // (PR-A) — a hidden haven dock stays off the picker too
                if (!IsDockableHaven(body)) continue;
                if (!LayerVisible("ports.havens")) continue; // 🗺 Layers (#405): Dock havens off → its ⚓ pick answers no clicks
                (float sx, float sy) = _camera.WorldToScreen(_ephemeris.Position(body.Id, SimTime));
                double drawnPx = body.BodyRadius / _camera.MetersPerPixel;
                double hit = MapPick.BodyHitRadiusPx(drawnPx, radiusPx);
                double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
                if (d2 <= hit * hit)
                {
                    found.Add((new PickCandidate('B', body.Id, $"{body.Name} · dock haven — walk ashore", "⚓"), d2, 2));
                }
            }

            // The owner's roadster lesson: a REVEALED non-haven station (a wreck, a compute farm, a
            // factory) fell through both loops above — visible on the map, untargetable by click. They
            // are destinations too: the μ=0 arm flow flies you alongside (no clamp — that's the point;
            // the fetch pickup is proximity). Hidden ones stay off the picker until charted (PR-A).
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (IsBodyHidden(body.Id)) continue;
                if (body.Kind != BodyKind.Station || IsDockableHaven(body)) continue;
                (float sx, float sy) = _camera.WorldToScreen(_ephemeris.Position(body.Id, SimTime));
                double drawnPx = body.BodyRadius / _camera.MetersPerPixel;
                double hit = MapPick.BodyHitRadiusPx(drawnPx, radiusPx);
                double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
                if (d2 <= hit * hit)
                {
                    found.Add((new PickCandidate('B', body.Id, $"{body.Name} · station — come alongside", "🛰"), d2, 2));
                }
            }
        }

        return [.. found.OrderBy(f => f.Rank).ThenBy(f => f.DistSq).Select(f => f.Pick)];
    }

    private void OpenPickMenu(List<PickCandidate> picks, double x, double y)
    {
        _pickMenu = picks;
        _pickMenuX = x;
        _pickMenuY = y;
        StateHasChanged();
    }

    private void OpenPickCandidate(PickCandidate pick)
    {
        double x = _pickMenuX, y = _pickMenuY;
        _pickMenu = null;
        OpenPickCandidateAt(pick, x, y);
    }

    private void OpenPickCandidateAt(PickCandidate pick, double x, double y)
    {
        switch (pick.Kind)
        {
            case 'S': OpenShipMenuFor(pick.Id, x, y); break;
            case 'H': MarkHunterOfInterest(pick.Id); break;
            case 'B': OpenBodyMenuFor(pick.Id, x, y); break;
            case 'C': OpenCorridorMenuFor(pick.Id, x, y); break;
            default: OpenSkyMenu(x, y); break;
        }
    }

    /// <summary>M29: clicking a contact SELECTS it (scope tracks, prediction pins — unchanged)
    /// AND opens its menu: track with the telescope, mark interest, read the vitals.</summary>
    private void OpenShipMenuFor(string shipId, double clientX, double clientY)
    {
        if (_selectedTargetId != shipId)
        {
            SelectTarget(shipId);
        }

        _shipMenuId = shipId;
        // #253: store the raw click anchor — ClampMenu applies the offset AND the edge flip at render.
        _shipMenuX = clientX;
        _shipMenuY = clientY;
        StateHasChanged();
    }

    private void OpenBodyMenuFor(string bodyId, double clientX, double clientY)
    {
        CelestialBody? body = _ephemeris?.Bodies.FirstOrDefault(b => b.Id == bodyId);
        if (body is null)
        {
            return;
        }

        _bodyMenuBody = body;
        _bodyMenuX = clientX; // #253: raw anchor; ClampMenu offsets + flips at render
        _bodyMenuY = clientY;
        ComputeAerobrakeQuote(body); // #290: price the aerobrake once on open (it flies drag — never per render)
        StateHasChanged(); // pointer events don't auto-render (IHandleEvent) — show the menu now
    }

    private void OpenCorridorMenuFor(string corridorKey, double clientX, double clientY)
    {
        // CorridorRegion is a struct: search explicitly rather than FirstOrDefault, whose
        // "not found" is a degenerate default lane, not null.
        foreach (CorridorRegion lane in _mapCorridors)
        {
            if (CorridorKey(lane) != corridorKey)
            {
                continue;
            }

            _corridorMenuLane = lane;
            _selectedCorridorKey = corridorKey;
            _corridorMenuX = clientX; // #253: raw anchor; ClampMenu offsets + flips at render
            _corridorMenuY = clientY;
            StateHasChanged();
            return;
        }
    }

    // ---- Map layers (owner: "filter what is being shown… they clutter the view a lot";
    // #405: rebuilt as a collapsible TREE — parent families you fold away, each a cascading
    // tri-state over its leaf toggles. The tree shape + all the resolution logic (visibility,
    // cascade, per-desk defaults, the threats-never-hidden invariant) is the pure, Core-tested
    // MapLayerTree; this partial only holds the per-desk hidden set and the UI collapse state. ----

    private readonly Dictionary<ShipDesk, HashSet<string>> _hiddenLayersByDesk = [];
    private bool _layersOpen;

    // Which parent families are folded shut in the panel. UI-only (not per desk, not gating any
    // draw) — seeded once from the tree's DefaultCollapsed flags (Routes rides collapsed).
    private HashSet<string>? _collapsedLayerGroups;

    private HashSet<string> CollapsedLayerGroups =>
        _collapsedLayerGroups ??= [.. MapLayerTree.Groups.Where(g => g.DefaultCollapsed).Select(g => g.Key)];

    private void ToggleLayerGroupCollapsed(string groupKey)
    {
        if (!CollapsedLayerGroups.Remove(groupKey))
        {
            CollapsedLayerGroups.Add(groupKey);
        }

        StateHasChanged();
    }

    private HashSet<string> HiddenLayers
    {
        get
        {
            if (!_hiddenLayersByDesk.TryGetValue(_activeDesk, out HashSet<string>? hidden))
            {
                // Per-desk defaults live in Core (preserves the lanes-off default; sensors sees all).
                hidden = MapLayerTree.DefaultHidden(_activeDesk == ShipDesk.Sensors);
                _hiddenLayersByDesk[_activeDesk] = hidden;
            }

            return hidden;
        }
    }

    // The single source of truth the draw path + the click-picker resolve through. A pinned leaf
    // (Threats) is always visible no matter the hidden set — the safety invariant lives in Core.
    private bool LayerVisible(string key) => MapLayerTree.IsVisible(HiddenLayers, key);

    private void ToggleLayer(string key)
    {
        MapLayerTree.ToggleLeaf(HiddenLayers, key);
        StateHasChanged();
    }

    // Parent checkbox: tri-state cascade to the family (On→Off, Off/Mixed→On); pinned groups inert.
    private void ToggleLayerGroup(MapLayerTree.Group group)
    {
        MapLayerTree.CascadeGroup(HiddenLayers, group);
        StateHasChanged();
    }

    // The panel's bottom line: drop this desk back to its shipped default visibility.
    private void ResetLayersToDeskDefaults()
    {
        _hiddenLayersByDesk[_activeDesk] = MapLayerTree.DefaultHidden(_activeDesk == ShipDesk.Sensors);
        StateHasChanged();
    }

    // #405 pick-menu hint (owner: "some UI hint about adding more layers when wanted… maybe to the
    // selection pop-up"): the "Which one?" chooser's footer link closes itself and opens the Layers
    // panel, so a crowded knot points the captain straight at the filter. Reuses existing state.
    private void OpenLayersFromPick()
    {
        _pickMenu = null;
        _layersOpen = true;
        StateHasChanged();
    }

    // ---- #406 Nav search: type-to-find a jump target instead of zoom-hunting. Composes with the
    // existing machinery — the candidates are the same set the click-picker knows (bodies + depots +
    // live/last-seen contacts + hunters), the rows reuse the "name · kind · flavor" idiom, and the
    // select action reuses SetPlotFrame + the camera. The pure match/rank seam is Core's NavSearch. ----

    // One found target. Kind mirrors PickCandidate's ('B' body, 'S' contact/depot, 'H' hunter) so the
    // jump reuses the same select paths. Hidden = the Layers filter would keep it OFF the map even after
    // we jump there — true only for a layer-filtered contact/depot; a charted body's disc always draws,
    // and an UNCHARTED body is excluded from search entirely (see CollectSearchCandidates).
    private readonly record struct NavSearchRow(char Kind, string Id, string Name, string Flavor, string Icon, bool Hidden);

    private ElementReference _navSearchInput;
    private string _navSearchQuery = "";
    private bool _navSearchOpen;
    private int _navSearchIndex;                 // which row the keyboard has highlighted
    private List<NavSearchRow> _navSearchRows = [];
    private const int NavSearchMaxRows = 12;     // a busy scenario has dozens of bodies — cap the dropdown

    // `/` (OnKeyDown) opens the box and hands it the keyboard. Rendered only on the map desks, so the
    // @ref is live whenever this runs.
    private async Task FocusNavSearch()
    {
        _navSearchOpen = true;
        RecomputeNavSearch();       // in case a query is already typed (re-opening)
        await _navSearchInput.FocusAsync();
    }

    private void OnNavSearchInput(ChangeEventArgs e)
    {
        _navSearchQuery = e.Value?.ToString() ?? "";
        RecomputeNavSearch();
    }

    private void RecomputeNavSearch()
    {
        List<NavSearchRow> ranked = NavSearch.FilterAndRank(_navSearchQuery, CollectSearchCandidates(), r => r.Name);
        _navSearchRows = ranked.Count > NavSearchMaxRows ? ranked.GetRange(0, NavSearchMaxRows) : ranked;
        _navSearchIndex = 0;
    }

    /// <summary>Everything the search can jump to, in the same intent order the click-picker ranks by:
    /// threats first, then live/last-seen contacts and depots, then every CHARTED body. Unlike the
    /// picker this ignores the screen position and the forgiving radius (you're searching, not clicking)
    /// AND ignores the Layers filter (a search should find what you explicitly asked for even if your own
    /// filter is hiding it) — but it marks a layer-hidden contact so the row says so. Uncharted bodies
    /// (IsBodyHidden, PR-A) stay OUT: naming an undiscovered wreck would spoil it, and the picker excludes
    /// them too.</summary>
    private List<NavSearchRow> CollectSearchCandidates()
    {
        var rows = new List<NavSearchRow>();

        foreach (HunterState hunter in _hunters)
        {
            if (hunter.BrokenOff || hunter.CaughtPlayer)
            {
                continue;
            }

            rows.Add(new NavSearchRow('H', hunter.Id, hunter.Callsign, "hunter", "🐺", false));
        }

        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Active || npc.Arrived)
            {
                continue;
            }

            // A never-observed contact has no known place to jump to and no name on the plot — skip it,
            // matching the picker (it needs a live position or a last-seen ghost to answer at all).
            if (!npc.CurrentlyObserved && npc.LastObservation is null)
            {
                continue;
            }

            bool isDepot = npc.Ship.DepotBodyId is not null;
            string leaf = isDepot ? "ports.depots" : npc.CurrentlyObserved ? "traffic.live" : "traffic.ghosts";
            string flavor = isDepot ? "depot — cargo pod" : npc.CurrentlyObserved ? "contact" : "last seen";
            string icon = isDepot ? "📦" : "🛰";
            rows.Add(new NavSearchRow('S', npc.Ship.Id, npc.Ship.Callsign, flavor, icon, !LayerVisible(leaf)));
        }

        if (_ephemeris is not null)
        {
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (body.ParentId is null)
                {
                    continue;   // the sun is not a jump target you frame on (nothing orbits it locally here)
                }

                if (IsBodyHidden(body.Id))
                {
                    continue;   // uncharted — off the search, same as the picker (PR-A: don't spoil undiscovered finds)
                }

                (string flavor, string icon) = ClassifyBodyForSearch(body);
                rows.Add(new NavSearchRow('B', body.Id, body.Name, flavor, icon, false));
            }
        }

        return rows;
    }

    // The picker's flavor/icon rules for a body, distilled into one classifier (the picker spreads them
    // across three position-gated loops). Kept in sync so a searched body reads the same as a clicked one.
    private (string Flavor, string Icon) ClassifyBodyForSearch(CelestialBody body)
    {
        bool isDeflectionRock = _deflection is { } dgig && body.Id == dgig.RockBodyId;
        (string flavor, string icon) =
            isDeflectionRock ? ("deflection target — on a collision course", "⚠")
            : IsDockableHaven(body) ? ("dock haven — walk ashore", "⚓")
            : body.IsHaven ? ("haven — lie low in orbit", "🏴")
            : body.Kind == BodyKind.Station ? ("station — come alongside", "🛰")
            : ("body", "🪐");

        if (ShuttleExcursion.IsLandableSurface(body.Kind))
        {
            flavor += _landableInRangeIds.Contains(body.Id)
                ? " · 🛬 landable — shuttle range"
                : " · 🛬 landable — out of shuttle reach";
        }

        return (flavor, icon);
    }

    private async Task OnNavSearchKeyDown(KeyboardEventArgs e)
    {
        // The input's own @onkeydown:stopPropagation keeps these keys out of OnKeyDown, so typing a name
        // (w/s/o/1-7…) or steering with the arrows never drives the ship or switches desks while you search.
        switch (e.Key)
        {
            case "ArrowDown":
                if (_navSearchRows.Count > 0)
                {
                    _navSearchIndex = (_navSearchIndex + 1) % _navSearchRows.Count;
                }

                break;
            case "ArrowUp":
                if (_navSearchRows.Count > 0)
                {
                    _navSearchIndex = (_navSearchIndex - 1 + _navSearchRows.Count) % _navSearchRows.Count;
                }

                break;
            case "Enter":
                if (_navSearchIndex >= 0 && _navSearchIndex < _navSearchRows.Count)
                {
                    await JumpToSearchResult(_navSearchRows[_navSearchIndex]);
                }

                break;
            case "Escape":
                CloseNavSearch();
                await RefocusMap();   // hand the keyboard back to the helm
                break;
        }
    }

    // #406 chosen SELECT action — "focus + frame + centre", the single most useful "take me there" for
    // planning; the target's context menu (arm autopilot, set destination, track) is then one click away
    // now that it sits under the camera. We deliberately do NOT auto-open that menu (it would need a
    // screen anchor and pre-empts the choice the captain may not want yet — the issue's steer).
    //   • a BODY becomes the plot-frame origin (SetPlotFrame — the map + ribbon co-move with it) AND the
    //     camera centres on it.
    //   • a CONTACT/HUNTER can't be a frame origin (not an ephemeris body), so we SELECT it (scope + the
    //     war-room interest bracket) and centre on its last-known spot.
    private async Task JumpToSearchResult(NavSearchRow row)
    {
        Vector2d? world = null;
        switch (row.Kind)
        {
            case 'B':
                if (_ephemeris is not null)
                {
                    SetPlotFrame(row.Id);
                    world = _ephemeris.Position(row.Id, SimTime);
                }

                break;
            case 'S':
                if (FindNpc(row.Id) is { } npc)
                {
                    if (_selectedTargetId != row.Id)
                    {
                        SelectTarget(row.Id);
                    }

                    world = npc.CurrentlyObserved ? npc.State.Position : npc.LastObservation?.Position;
                }

                break;
            case 'H':
                foreach (HunterState hunter in _hunters)
                {
                    if (hunter.Id != row.Id)
                    {
                        continue;
                    }

                    MarkHunterOfInterest(row.Id);
                    world = hunter.State.Position;
                    break;
                }

                break;
        }

        if (world is { } target)
        {
            FollowShip = false;         // centring must stick — follow-ship would snap back to the ship next frame
            _camera.CenterOn(target);
        }

        CloseNavSearch();
        await RefocusMap();             // the keyboard goes back to the helm so w/s/o etc. work at once
    }

    private void CloseNavSearch()
    {
        _navSearchOpen = false;
        _navSearchQuery = "";
        _navSearchRows = [];
        _navSearchIndex = 0;
    }

    // ---- M29: the contact menu + target dossier ----
    private string? _shipMenuId;
    private double _shipMenuX, _shipMenuY;

    private void CloseShipMenu()
    {
        _shipMenuId = null;
        StateHasChanged();
    }

    // ---- SundaySecondPlan PR-C: point at the sky and ask ----
    // On the Sensors desk every click answers with scan options: ships (fixed or not), planets,
    // trade lanes, and empty sky all open scan-contextual menus that enqueue telescope work.

    private CorridorRegion? _corridorMenuLane;
    private double _corridorMenuX, _corridorMenuY;
    private Vector2d? _skyMenuWorld;
    private double _skyMenuX, _skyMenuY, _skyMenuRadius;
    private bool _suppressClickMenu;

    private void CloseCorridorMenu()
    {
        _corridorMenuLane = null;
        _selectedCorridorKey = null;
        StateHasChanged();
    }

    private void CloseSkyMenu()
    {
        _skyMenuWorld = null;
        StateHasChanged();
    }

    private CorridorRegion? CorridorAt(double clientX, double clientY)
    {
        Vector2d world = _camera.ScreenToWorld(clientX, clientY);
        CorridorRegion? best = null;
        double bestDistance = double.MaxValue;
        foreach (CorridorRegion lane in _mapCorridors)
        {
            double d = lane.DistanceTo(world);
            if (d <= lane.Radius && d < bestDistance)
            {
                (bestDistance, best) = (d, lane);
            }
        }

        return best;
    }

    private void OpenSkyMenu(double clientX, double clientY)
    {
        _skyMenuWorld = _camera.ScreenToWorld(clientX, clientY);
        // Scan size follows the zoom: what looks like "about here" on screen is what gets
        // scanned — zoom in for a tight expensive-per-area look, out for a broad survey.
        _skyMenuRadius = Math.Clamp(120 * _camera.MetersPerPixel, 2e9, 5e10);
        _skyMenuX = clientX; // #253: raw anchor; ClampMenu offsets + flips at render
        _skyMenuY = clientY;
        StateHasChanged();
    }
}
