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

// #251 · Split from Map.Alerts.cs, moved verbatim: the parrot's squawk and its per-tick watch, and #166's
// ship-wide alert channel driven each tick and acknowledged. The parrot's fields and the channel itself
// stay in Map.Alerts.cs.
public partial class Map
{
    private void SquawkNow(Parrot.Squawk kind, double nowMs, string? subject = null, bool force = false)
    {
        // #580 · THE BIRD IS ON THE SHIP. Owner, on a moon: "why does the parrot talk about debt collectors
        // now" / "we do not want any ship type warnings received here on the surface". The perch is aboard a
        // docked hull and the captain is in a suit somewhere else; a squawk has no way to reach them and no
        // business trying. Gated HERE as well as at the tick, because `force: true` callers (the Busted
        // crossing was the one he caught) bypass every other brake this method has.
        if (_surface is not null)
        {
            return;
        }

        if (!force && nowMs < _parrotCooldownUntilMs)
        {
            return; // one squawk at a time; the bird sulks between
        }

        _parrotSquawk = Parrot.Line(kind, _parrotCounter++, subject);
        _parrotBubbleUntilMs = nowMs + Parrot.BubbleSeconds * 1000;
        _parrotCooldownUntilMs = nowMs + Parrot.CooldownSeconds * 1000;
        RendererInterop.PlayCue("pulse");
    }

    /// <summary>Rising-edge detectors over live ship state, priority ordered — the parrot
    /// yells once when a thing BECOMES true, never per frame.</summary>
    private void UpdateParrot(double nowMs)
    {
        if (_parrotSquawk is not null && nowMs > _parrotBubbleUntilMs)
        {
            _parrotSquawk = null;
        }

        // #166: the collision squawk now rides the ShipAlerts channel (UpdateShipAlerts) so the banner
        // strip, the ledger, and the parrot all speak from one crossing — see there for the ROCKS AHEAD.

        bool prey = SelectedCaptureTarget() is { } target && CaptureRule.IsInWindow(_ship, target.State);
        if (prey && !_parrotSawPrey)
        {
            SquawkNow(Parrot.Squawk.PreyInGlass, nowMs);
        }

        _parrotSawPrey = prey;

        bool hunterNear = NearestHunterDistance() is { } hunterDistance && hunterDistance < 5e9;
        if (hunterNear && !_parrotSawHunter)
        {
            SquawkNow(Parrot.Squawk.HunterNear, nowMs);
        }

        _parrotSawHunter = hunterNear;

        bool arcing = _plasma is not null && _ship.Charge >= ArcChargeThreshold;
        if (arcing && !_parrotSawArc)
        {
            SquawkNow(Parrot.Squawk.Arcing, nowMs);
        }

        _parrotSawArc = arcing;

        bool wobble = RumWobbleActive;
        if (wobble && !_parrotSawWobble)
        {
            SquawkNow(Parrot.Squawk.DrunkDriver, nowMs);
        }

        _parrotSawWobble = wobble;

        // Off the books: the sweep found a ship that publishes no timetable — once per ship.
        if (_trackingPost is not null)
        {
            foreach (TrackedTarget entry in _trackingPost.Entries)
            {
                if (_parrotOffBooks.Contains(entry.ShipId))
                {
                    continue;
                }

                if (FindNpc(entry.ShipId) is { } secretive && !secretive.Ship.PublishesTimetable)
                {
                    _parrotOffBooks.Add(entry.ShipId);
                    SquawkNow(Parrot.Squawk.OffTheBooks, nowMs);
                    break;
                }
            }
        }

        bool pyramidInSky = false;
        for (int i = 0; i < AncientsRule.PyramidCount; i++)
        {
            if (AncientsRule.Revealed(i, _ship.Position, SimTime))
            {
                pyramidInSky = true;
                break;
            }
        }

        if (pyramidInSky && !_parrotSawPyramid)
        {
            SquawkNow(Parrot.Squawk.PyramidSighted, nowMs);
        }

        _parrotSawPyramid = pyramidInSky;
    }

    // #166: drive the ship-wide alert channel from live sim state each tick. Collision and fuel are
    // evaluated here; the orbit-degradation alert is raised/cleared from UpdateParkStability (its own
    // edge detector) through RaiseOrbitDegrade/ClearOrbitDegrade. On a rising edge the channel returns
    // true — the cue to squawk the parrot, log a receipt, and (for a red) drop warp so it can't be
    // blown past. The banner strip and desk chips read the channel; nothing here re-shouts per tick.
    private void UpdateShipAlerts(double nowMs)
    {
        // Collision: the course has a ballistic impact / sub-surface pass in the horizon — UNLESS the
        // autopilot is armed with a valid rehearsed plan, in which case the alarm trusts the PLAN the
        // rehearsal flew (#196/#148). The insert burn resolves the ballistic impact, so that impact is
        // the plan working, not news; the ballistic alarm returns the instant the plan is gone (disarm/
        // handback → _autopilotPlanClosestPass null). A plan whose OWN path goes subsurface leaves an
        // Impact pass cached and shouts red immediately — a bad plan shouts LOUDER, not softer.
        // #220: while the autopilot HOLDS the park (keeping active AND the next trim is funded), the
        // ballistic projection's between-trim dip toward the surface is the keeper working, not danger —
        // the alarm trusts the kept orbit. `_orbitKept` alone is the "keeping ended" edge (StationKeep
        // clears it on an unbound orbit or a dry tank; a disarm clears it via ResetApproachTracking), so
        // the alarm returns to the ballistic course the instant keeping ends (#183/#193 backstop).
        bool armedWithPlan = _armedOrbitBodyId is not null && _autopilotPlanPath is { Count: >= 2 };
        bool keepingHoldsOrbit = _orbitKept && _keepTrimFunded;
        ClosestApproach.Pass? collision =
            CollisionAlertRule.Evaluate(armedWithPlan, keepingHoldsOrbit, _closestPass, _autopilotPlanClosestPass);
        if (collision is { } cp)
        {
            string body = cp.BodyName;
            if (_shipAlerts.Raise(AlertKind.Collision, AlertSeverity.Red, $"ROCKS AHEAD! — impact with {body}", SimTime))
            {
                SquawkNow(Parrot.Squawk.Impact, nowMs, body, force: true);
                LogAutopilotEvent($"⚠ collision alarm — impact course with {body}");
                Warp = 1; // an impact at 10,000× is unwatchable
            }
        }
        else
        {
            _shipAlerts.Clear(AlertKind.Collision);
        }

        // Fuel: amber at the 18% autopilot reserve, red at the reach-a-pump floor (FuelAlertRule).
        switch (FuelAlertRule.Evaluate(_reactionMassPulses, ReactionMassCapacity))
        {
            case AlertSeverity.Red:
                if (_shipAlerts.Raise(AlertKind.Fuel, AlertSeverity.Red,
                        $"fuel critical — {_reactionMassPulses} p left, can't reach a pump", SimTime))
                {
                    SquawkNow(Parrot.Squawk.FuelLow, nowMs, force: true);
                    LogAutopilotEvent($"⚠ fuel critical — {_reactionMassPulses} p, below the reach-a-pump floor");
                    Warp = 1;
                }
                break;
            case AlertSeverity.Amber:
                if (_shipAlerts.Raise(AlertKind.Fuel, AlertSeverity.Amber,
                        $"fuel low — {_reactionMassPulses} p left, under the autopilot reserve", SimTime))
                {
                    SquawkNow(Parrot.Squawk.FuelLow, nowMs);
                    LogAutopilotEvent($"fuel low — {_reactionMassPulses} p, under the 18% reserve");
                    // #172: a fuel-amber crossing mid-skip is an event the captain must see (amber never
                    // yanks warp on its own, so cancel skip here). Skip never blows past a fuel warning.
                    EndSkipIfActive("fuel low — under the autopilot reserve");
                }
                break;
            default:
                _shipAlerts.Clear(AlertKind.Fuel);
                break;
        }

        // #266: ADRIFT — the tank is empty and we're not docked. The founding "we're stranded" beat rides
        // this same channel (like ROCKS AHEAD and orbit-decay): the strip says the state, the ledger logs
        // it, the parrot proposes the tow — and the rescue POP-UP opens on the rising edge so the one
        // button the moment exists to offer is in the captain's face, never buried in the masthead's
        // shadow (the #262 stranding). The action lives in the pop-up, not the banner (#236).
        if (Adrift)
        {
            if (_shipAlerts.Raise(AlertKind.Adrift, AlertSeverity.Red,
                    "ADRIFT — out of reaction mass. The parrot's whistling for a tow; open the rescue offer.", SimTime))
            {
                SquawkNow(Parrot.Squawk.Adrift, nowMs, force: true);
                LogAutopilotEvent("⚠ adrift — out of reaction mass; rescue offered");
                Warp = 1; // stranded is not a thing to watch at 10,000×
                _showRescueOffer = true; // the offer pops up the instant we go dry
            }
        }
        else
        {
            // Under way again (rescued, or a burn found a pump): clear the alert and dismiss any stale offer.
            if (_shipAlerts.Clear(AlertKind.Adrift))
            {
                _showRescueOffer = false;
            }
        }
    }

    // #166: the captain silences one alert's shout — it lingers as a dimmed chip while the condition
    // holds, and a fresh crossing (or an escalation) shouts again.
    private void AcknowledgeAlert(AlertKind kind) => _shipAlerts.Acknowledge(kind);
}
