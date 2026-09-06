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
/// #251 · THE DEV-START INJECTIONS — the query-string cheats that seed a scenario mid-story so a scene can
/// be reached in one boot instead of played to: a fetch job, a crack, a backroom, a tip, a hoard. Each one
/// writes the same state the honest route would have written and nothing else; none of them is reachable
/// from the game's own chrome. Split out of <c>Map.UiState.cs</c> under #251 with no member renamed,
/// re-scoped or re-ordered.
/// </summary>
public partial class Map
{
    // Dev cheat (/map?fetch=intel|active|picked): drop a fetch job straight into the ledger at a
    // stage, so a playtester can test each leg without flying the ones between.
    //   intel  = the new first stage: accepted, wreck HIDDEN, transponder fix in the Comms ledger.
    //   active = post-scan (backward-compatible): accepted AND wreck already charted.
    //   picked = charted AND already lifted (fly nowhere — hand off on the spot).
    // #233 · Any stage may be suffixed with "-chip" (/map?fetch=intel-chip) to deal the roadster the
    // blackmail twin's cargo rather than the wallet, so the three endings can be walked without waiting for
    // the one-in-four to land.
    // The drop-off is the interior station you're standing in (so "picked" can be delivered on the
    // spot), else the first interior station. Pair intel with ?start=wreck to test the proximity pickup.
    private void InjectFetchCheat(string stage)
    {
        const string wreckId = Derelict.RoadsterBodyId;
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
            DestBodyId: dest, SourceBodyId: wreckId,
            Pin: stage.Contains("chip", StringComparison.Ordinal) ? CompromisingChip.FindId : null)
        {
            State = stage.StartsWith("picked", StringComparison.Ordinal) ? QuestState.PickedUp : QuestState.Active,
        });
        if (stage.StartsWith("picked", StringComparison.Ordinal) && stage.Contains("chip", StringComparison.Ordinal))
        {
            _satchel = [.. Core.Satchel.Add(_satchel, CompromisingChip.Found())]; // already prised, in the pocket
        }

        if (stage.StartsWith("intel", StringComparison.Ordinal))
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
    // #650: deliberately BODY-WIDE (no siteIndex). The cheat exists so a tester can set down anywhere on
    // Phobos and find a ✗ waiting; picking a ground here would mean guessing which of the four sites they
    // will land at, and guessing wrong would read exactly like the bug this cheat is used to inspect.
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
}
