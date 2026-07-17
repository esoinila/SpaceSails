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

    // M20: ships and pods are clickable on the map — same effect as picking their traffic row.
    // ---- The unified picker (owner: "hard to click things that are close by"; Gemini
    // consult: forgiving radius, chooser when several objects stack, lanes always last) ----

    private readonly record struct PickCandidate(char Kind, string Id, string Label, string Icon);

    private List<PickCandidate>? _pickMenu;
    private double _pickMenuX, _pickMenuY;

    private const double PickRadiusPx = 15;     // the forgiving direct-hit radius
    private const double PickNearRadiusPx = 28; // near-miss radius for the lane-vs-planet tiebreak

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
            if (isDepot ? !LayerVisible("depots") : !LayerVisible("traffic")) continue;
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
                double hit = Math.Max(Math.Clamp(drawnPx, 14, 80), radiusPx);
                double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
                if (d2 <= hit * hit)
                {
                    // #208: a moon haven (parked-in, no ⚓ dock) carries its walk-in phrase too, so the
                    // picker's port entries all read their kind at a glance.
                    (string flavor, string icon) = body.IsHaven ? ("haven — lie low in orbit", "🏴") : ("body", "🪐");
                    found.Add((new PickCandidate('B', body.Id, $"{body.Name} · {flavor}", icon), d2, 2));
                }
            }

            // Haven docks are stations (skipped above — you clamp, not orbit), but they must still be
            // pickable: stacked over their planet, the ⚓ in the name calls out the one you can dock at
            // right there in the "which one?" list — no zooming in to tell them apart (owner's ask).
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (IsBodyHidden(body.Id)) continue; // (PR-A) — a hidden haven dock stays off the picker too
                if (!IsDockableHaven(body)) continue;
                (float sx, float sy) = _camera.WorldToScreen(_ephemeris.Position(body.Id, SimTime));
                double drawnPx = body.BodyRadius / _camera.MetersPerPixel;
                double hit = Math.Max(Math.Clamp(drawnPx, 14, 80), radiusPx);
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
                double hit = Math.Max(Math.Clamp(drawnPx, 14, 80), radiusPx);
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
        _shipMenuX = clientX + 14;
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
        _bodyMenuX = clientX + 14;
        _bodyMenuY = clientY;
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
            _corridorMenuX = clientX + 14;
            _corridorMenuY = clientY;
            StateHasChanged();
            return;
        }
    }

    // ---- Map layers (owner: "filter what is being shown… they clutter the view a lot";
    // Gemini consult: corner checkbox panel, settings remembered per desk) ----

    private static readonly (string Key, string Label, string Icon)[] MapLayerDefs =
    [
        ("lanes", "Trade lanes", "🛣"),
        ("traffic", "Ships & beacons", "🛰"),
        ("depots", "Depots & stations", "📦"),
        ("scans", "Sensor overlays", "🔭"),
    ];

    private readonly Dictionary<ShipDesk, HashSet<string>> _hiddenLayersByDesk = [];
    private bool _layersOpen;

    private HashSet<string> HiddenLayers
    {
        get
        {
            if (!_hiddenLayersByDesk.TryGetValue(_activeDesk, out HashSet<string>? hidden))
            {
                // Per-desk defaults: the sensors chief starts with the full working sky; every
                // other desk starts with the lanes off (the clutter the owner flagged). After
                // that each desk remembers its own picks.
                hidden = _activeDesk == ShipDesk.Sensors ? [] : ["lanes"];
                _hiddenLayersByDesk[_activeDesk] = hidden;
            }

            return hidden;
        }
    }

    private bool LayerVisible(string key) => !HiddenLayers.Contains(key);

    private void ToggleLayer(string key)
    {
        if (!HiddenLayers.Remove(key))
        {
            HiddenLayers.Add(key);
        }

        StateHasChanged();
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
        _skyMenuX = clientX + 14;
        _skyMenuY = clientY;
        StateHasChanged();
    }
}
