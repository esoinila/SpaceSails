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
/// #251 · THE HUNTER, FROM THE WARRANT TO THE LAST WORD. A heat event puts one on the board; a
/// warning shot tests its nerve; a warning, a bribe or a cold trail ends the errand — and
/// <c>RemoveHunter</c> is the ONE call that ends it in both places, the sky and the ground.
///
/// <para>Split out of <c>Map.Combat.cs</c> under #251 with no member renamed, re-scoped or re-ordered,
/// and not one field moved: the hunter roster, the callsign table and the colour all stay in the
/// opening file.</para>
/// </summary>
public partial class Map
{
    private double? NearestHunterDistance()
    {
        double? best = null;
        foreach (HunterState hunter in _hunters)
        {
            double d = (hunter.State.Position - _ship.Position).Length;
            if (best is null || d < best)
            {
                best = d;
            }
        }

        return best;
    }

    /// <summary>Clicking a hunter on the map locks it as the war room's interest target — the same
    /// lock as the War Room 🎯 button (corner brackets, a firing solution, a warning shot that
    /// breaks its nerve). Clicking the locked hunter again clears the lock.</summary>
    private void MarkHunterOfInterest(string hunterId)
    {
        bool wasLocked = _interestTargetId == hunterId;
        SetInterestTarget(hunterId); // toggles; also nulls the stale intercept and re-scans the pass
        string name = _hunters.FirstOrDefault(h => h.Id == hunterId).Callsign ?? "the hunter";
        ShowPulseMessage(wasLocked
            ? $"Lock released — {name} is no longer the war room's mark"
            : $"🎯 {name} marked — the war room has the fire-control lock; a warning shot will test its nerve");
    }

    // One hunter per heat event, fitting out at the nearest policed body (Earth/Mars-like —
    // never a haven). A pure outer-reaches scenario with nothing policed in range simply sends
    // no muscle — there's no cavalry to call.
    private void SpawnHunterForHeatEvent(string? warrant = null)
    {
        if (_ephemeris is null)
        {
            return;
        }

        CelestialBody? origin = EncounterRule.NearestPolicedBody(_ephemeris, _ship.Position, SimTime);
        if (origin is null)
        {
            return;
        }

        Vector2d originPosition = _ephemeris.Position(origin.Id, SimTime);
        const double h = 1.0;
        Vector2d originVelocity = (_ephemeris.Position(origin.Id, SimTime + h) - _ephemeris.Position(origin.Id, SimTime - h)) / (2 * h);

        string callsign = HunterCallsigns[_hunterSeq % HunterCallsigns.Length];
        string id = $"hunter-{_hunterSeq++}";
        _hunters.Add(EncounterRule.SpawnHunter(id, callsign, origin.Id, originPosition, originVelocity, SimTime, warrant));
        PushNewsEvent(NewsWire.NewsEventKind.HunterDispatched, callsign, origin.Name);
        // #380 item 5 (owner ruling 2026-07-19: "new players are left mystified") — the robbery bought
        // this hunter, but the fit-out delay meant muscle appeared days later with no causal link. This
        // pulse draws the chain in-voice the moment the collector is spawned; the callsign rides the news
        // headline behind it.
        ShowPulseMessage($"Word's out — your last job bought you a collector ({callsign}). It's fitting out at {origin.Name}; days, not weeks.");
    }

    private void FireWarningShot(string npcId)
    {
        NpcState? npc = FindNpc(npcId);
        if (npc is null)
        {
            // Not a freighter — it's the hunter itself. A warning shot erodes a collector's nerve.
            WarnHunter(npcId);
            return;
        }

        if (npc.Ship.IsPod || !EncounterRule.InWeaponRange(_ship, npc.State))
        {
            return;
        }

        if (_slugAmmo <= 0)
        {
            ShowPulseMessage("No slugs left for a warning shot — buy dockside");
            return;
        }

        _slugAmmo--;
        npc.WarningShotFired = true;

        // M28: the warning shot is a REAL slug now — flung wide on purpose (AcrossTheBow
        // rounds never hit-check) but genuinely in flight on the map. Same reaction rules.
        Vector2d toTarget = (npc.State.Position - _ship.Position).Normalized();
        var wide = new Vector2d(-toTarget.Y, toTarget.X) * 0.03;
        FireOrdnance(OrdnanceKind.Slug, (toTarget + wide).Normalized(), MaxMuzzleSpeed,
            npc.Ship.Id, acrossTheBow: true);

        ComplianceState compliance = EncounterRule.ComplianceOf(npc.Ship, _heat.Level);
        ShowPulseMessage(compliance == ComplianceState.Stubborn
            ? $"WARNING SHOT ACROSS THE BOW — {npc.Ship.Callsign} answers with a tight-beam call for help, not her colours"
            : "WARNING SHOT ACROSS THE BOW — she heaves to");
        RendererInterop.PlayCue("pulse");

        // Second hunt, step 2: the warning shot teaches that a stubborn hull won't heave to — she
        // calls muscle instead, so the soft path is a dead end and the gun is the only way in.
        if (npcId == TrafficSchedule.StarterFreighterId)
        {
            AdvanceTutorial(StepWarnFreighter);
        }
    }

    // A warning shot flung across a Debt Collector's bow: each one erodes its nerve. Most peel off
    // (coast, stop closing) for a stretch that grows with every shot; enough of them and the
    // collector voids the contract for good. A rare "La Dolce Vita" sort quits at the very first.
    private void WarnHunter(string hunterId)
    {
        int index = _hunters.FindIndex(h => h.Id == hunterId);
        if (index < 0)
        {
            return;
        }

        HunterState hunter = _hunters[index];
        if (hunter.CaughtPlayer || hunter.BrokenOff || !EncounterRule.InWeaponRange(_ship, hunter.State))
        {
            return;
        }

        if (_slugAmmo <= 0)
        {
            ShowPulseMessage("No slugs left for a warning shot — buy dockside");
            return;
        }

        _slugAmmo--;

        // A real slug flung wide (AcrossTheBow rounds never hit-check, so its sail is never holed).
        Vector2d toTarget = (hunter.State.Position - _ship.Position).Normalized();
        var wide = new Vector2d(-toTarget.Y, toTarget.X) * 0.03;
        FireOrdnance(OrdnanceKind.Slug, (toTarget + wide).Normalized(), MaxMuzzleSpeed,
            hunter.Id, acrossTheBow: true);
        RendererInterop.PlayCue("pulse");

        bool goodLifeFirstShot = hunter.WarningShotsTaken == 0
            && EncounterRule.PrefersTheGoodLife(hunter.Id, _heat.Level);
        HunterState after = EncounterRule.WarnOff(hunter, _heat.Level, SimTime);

        if (after.BrokenOff)
        {
            // Gave up. Remove it here so the generic "loses your scent" path in StepEncounters
            // doesn't also fire with the wrong flavor.
            _hunters.RemoveAt(index);
            if (_interestTargetId == hunterId)
            {
                _interestTargetId = null;
            }

            ShowPulseMessage(goodLifeFirstShot
                ? $"⚠ {hunter.Callsign} watches the slug drift past, shrugs, and turns for the nearest cantina — la dolce vita 🍸"
                : $"⚠ {hunter.Callsign} has had enough — she sheers off and voids the contract");
            PushNewsEvent(NewsWire.NewsEventKind.HunterBrokeOff, hunter.Callsign, _nearestBody?.Name);
            SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        }
        else
        {
            _hunters[index] = after;
            double peelDays = (after.PeeledUntilSimTime - SimTime) / 86400.0;
            string nerve = after.WarningShotsTaken switch
            {
                1 => "wavers",
                2 => "is rattled",
                _ => "is losing her nerve",
            };
            ShowPulseMessage($"⚠ WARNING SHOT — {hunter.Callsign} {nerve} and sheers off (peels away ~{peelDays:0.#} d)");
            SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        }
    }

    private void BribeShip(string npcId)
    {
        NpcState? npc = FindNpc(npcId);
        if (npc is null || npc.Bribed || npc.Ship.IsPod)
        {
            return;
        }

        int price = EncounterRule.BribePrice(npc.Ship);
        if (_credits < price)
        {
            ShowPulseMessage("Not enough credits to grease this crew.");
            return;
        }

        _credits -= price;
        npc.Bribed = true;
        ShowPulseMessage($"{npc.Ship.Callsign}'s crew take the coin — an inside job, quiet as the void.");
    }

    /// <summary>This hunter is off you now — the one place in the game that means it.
    ///
    /// <para>#731 · And it means it on the GROUND too. A repo crew serves its writ on foot under an id of its
    /// own (<see cref="CollectorLanding.GroundHunterIdPrefix"/>) and was never in <c>_hunters</c>, so every
    /// caller here — the bribe, the resist, the Bolivia flee — removed nothing, and the captain who had just
    /// been told the crew <i>"sheers off"</i> was served again by the same people on the next frame. They
    /// walk back to their own boat now (<c>TheirBusinessHereIsDone</c>), which is #731's full stop and not a
    /// despawn: the ONE call that ends an encounter ends it in both places, so a future caller cannot end
    /// half of one.</para></summary>
    private void RemoveHunter(string hunterId)
    {
        for (int i = _hunters.Count - 1; i >= 0; i--)
        {
            if (_hunters[i].Id == hunterId)
            {
                _hunters.RemoveAt(i);
            }
        }

        TheirBusinessHereIsDone(hunterId);
    }
}
