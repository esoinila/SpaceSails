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
/// M25 · THE LOOP THAT FLIES IT — inside capture range the armed autopilot flies the "point at it and
/// throttle" approach the owner asked for: an approach burn, tidal trims as needed, and the insertion
/// once safely deep in the Hill sphere. Every burn is &#916;v-priced in pulses and charged at the tenth.
///
/// <para>The station fork is the one that matters most and the one a guard reads structurally: a berth
/// arrival is decided on <c>DockRule.Arrived</c> and never on <c>DockRule.InEnvelope</c>, because
/// standing in the envelope is not the same event as being there.</para>
///
/// <para>Split out of <c>Map.Autopilot.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // M25: the armed autopilot. Inside capture range it flies the "point at it and throttle"
    // approach the owner asked for — an approach burn, tidal trim burns as needed, and the
    // insertion once safely deep in the Hill sphere. Every burn is Δv-priced in pulses.
    private void CheckArmedInsertion()
    {
        if (_armedOrbitBodyId is null || _ephemeris is null || Paused)
        {
            return;
        }

        CelestialBody? body = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == _armedOrbitBodyId) { body = candidate; break; }
        }
        if (body?.ParentId is null) { _armedOrbitBodyId = null; return; }

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; break; }
        }
        if (parent is null) { _armedOrbitBodyId = null; return; }

        Vector2d bodyPos = _ephemeris.Position(body.Id, SimTime);
        double h = 1.0;
        Vector2d bodyVel = (_ephemeris.Position(body.Id, SimTime + h) - _ephemeris.Position(body.Id, SimTime - h)) / (2 * h);
        double hill = OrbitRule.HillRadius(body, parent.Mu);
        // #286: the kept-orbit radius is bounded so the circularized park clears the moon's PARENT planet.
        // Inert for every shipped moon (the tide-stable park is far tighter than this cap); the guard that
        // an inner moon with a small Hill sphere can never be circled through the world beside it.
        double keptRadiusCap = KeptRadiusCap(body, parent);
        double keptPark = KeptParkRadius(body, hill, keptRadiusCap);

        // Friday §0: once parked, the autopilot HOLDS the orbit — station-keeping owns the tick, not
        // the approach/insert loop below. It stays here until the captain disarms or the tank runs dry.
        if (_orbitKept)
        {
            StationKeep(body, parent, bodyPos, bodyVel, hill);
            // #220: does keeping still earn the collision alarm's trust this tick? StationKeep flips
            // _orbitKept off the instant keeping ends (unbound, or the tank can't afford the next trim);
            // if it still holds, recompute FUNDED from the post-trim state — the next trim's pulse cost
            // against the tank. Read as `_orbitKept && _keepTrimFunded` in UpdateShipAlerts, so a healthy
            // held park's subsurface between-trim dip is trusted, and a dry-tank keep shouts immediately.
            _keepTrimFunded = _orbitKept &&
                OrbitKeeping.TrimPulseCost(_ship, bodyPos, bodyVel, body, keptPark)
                    <= _reactionMassPulses;
            return;
        }

        // #969 THE HOLD — an arm made at PLAN TIME is a promise about a pass that has not come round yet.
        // Until it does, the autopilot touches NOTHING: the captain's own plotted burns are flying the ship,
        // and every line below would be the autopilot flying its own approach instead of the plan (and the
        // convergence watchdog would stand it down for "not converging" on a trip that has not begun). The
        // hold lifts at the pass epoch OR the moment the ship is honestly near the body, whichever comes
        // first — after which this is an ordinary armed arrival and the unchanged insertion/dock path below
        // finishes the trip with no further input. That is the owner's "absolutely no steps needed".
        //
        // …and this is HOW FAR OUT SHE IS, for the whole rest of the method: #969's hold, #146's moon-run
        // gate and #136's convergence watchdog each used to recompute it under its own name from the same
        // two unchanged operands. `_ship` is a readonly record struct and `bodyPos` a local fixed above, and
        // nothing between here and the switch reassigns either, so the three reads were one number.
        double distanceToTarget = (_ship.Position - bodyPos).Length;
        if (ArrivalStepRule.ArrivalPromiseIsStillAhead(
                _armedArrivalPassSimTime, SimTime, distanceToTarget, ArrivalNearRange(body, hill)))
        {
            return; // coast the plan — the arrival is still ahead
        }

        if (_armedArrivalPassSimTime is not null)
        {
            OpenTheArrivalWindow(body); // the promise has come round: the loop below has the ship now
        }

        // #155 the last mile: a μ=0 station is never orbited — its capture is the dock envelope (DockRule),
        // mirroring the rehearsal. The rendezvous schedule's two burns fly the ship into the berth; burn 2
        // (fired in the tick-advance split at the rendezvous epoch) is what matches the station's drift, so
        // stay FULLY muzzled until every scheduled burn has fired — letting AutopilotDecision fire an
        // Approach before then would double-pay for that match. Once the schedule is done and the ship is
        // matched & alongside, stand down GRACEFULLY (AutopilotStandInEnvelope — a SUCCESS, not a #147
        // handback) and let the captain clamp on with ⚓ Dock. Insert can never fire on a μ=0 body, so if
        // the ship isn't in the envelope yet the legacy Approach loop below can only close the gap, never
        // falsely "orbit-capture" the station.
        if (body.Kind == BodyKind.Station)
        {
            if (_armedTransferSchedule is { } stSched && _armedTransferBurnsFired < stSched.Burns.Count)
            {
                return; // still flying the rendezvous — the split fires the burns; coast, decisions muzzled
            }
            // #244 · THE ENVELOPE IS NOT THE DESTINATION. Owner, arrived at the roadster: "I think we
            // dropped out of autopilot… did we miss the dock button press while warping?" The autopilot had
            // SUCCEEDED — 499,721 km out, rel 4.0 — and for a WRECK that success is wrong: a fetch pickup is
            // proximity at a three-metre object, so half a million kilometres is a car-park in the next
            // county. DockRule.Arrived asks the clamp gate (DockableHavens.IsDockable, the predicate the ⚓
            // button itself obeys) rather than BodyKind or μ: a berth with an arm to throw is arrived at
            // when the arm can reach, and one without is arrived at where the errand actually happens.
            if (DockRule.Arrived(_ship, bodyPos, bodyVel, body))
            {
                AutopilotStandInEnvelope(body);
                return;
            }
            // Schedule done (or none available) but not yet matched/alongside — fall through to the legacy
            // Approach loop to match velocity and close the last stretch; its reserve guard stays intact.
            // For a non-clamp berth that stretch is now the last mile proper, flown by the same loop that
            // always closed the gap: nothing here burns differently, it simply stops later.
        }

        // #146 the moon run: while a transfer schedule is still in flight, keep AutopilotDecision MUZZLED.
        // Titan's 3e9 m capture floor makes the ship "inside capture range" for the ENTIRE Enceladus→Titan
        // cruise, so consulting the decision now would return Approach at ~7 km/s rel and restart the
        // velocity-reset bleed straight through the cheap arc. Stay muzzled until the ship is honestly
        // near the target — within max(60 s, 1% TOF) of arrival, OR inside the honest (floor-free)
        // Hill-scaled capture range. The scheduled burns fire in the tick-advance split, not here; once
        // the gate opens the terminal capture below takes over unchanged.
        if (_armedTransferSchedule is { } sched)
        {
            double tof = sched.Burns.Count > 0 ? sched.ArrivalTime - sched.Burns[0].SimTime : 0;
            double gateTime = sched.ArrivalTime - Math.Max(60.0, 0.01 * tof);
            double honestRange = OrbitRule.CaptureRangeHillRadii * hill;
            if (SimTime < gateTime && distanceToTarget >= honestRange)
            {
                return; // the arc is still in flight — coast, do not touch AutopilotDecision
            }
        }

        // Auto-orbiting a MOON (its parent is itself a planet, not the sun): the parent is a solid
        // body the approach chord must not thread. Auto-orbiting a PLANET (parent = sun): no chord
        // obstacle — you never route around the sun — just the target's own b-plane offset applies.
        OrbitRule.ApproachObstacle? obstacle = parent.ParentId is null
            ? null
            : new OrbitRule.ApproachObstacle(
                _ephemeris.Position(parent.Id, SimTime), parent.BodyRadius * OrbitRule.ParentSafeBodyRadii);

        switch (OrbitRule.AutopilotDecision(_ship, bodyPos, bodyVel, body, hill, keptRadiusCap))
        {
            case OrbitRule.AutopilotAction.Approach:
                // Convergence watchdog: a burn that beats our closest-ever pass is progress; a run of
                // burns that don't means the approach is stuck. Stand down and keep the fuel rather
                // than firing forever with no feedback (issue #136, the owner's live complaint).
                if (distanceToTarget < _approachMinDistance * (1 - 1e-3))
                {
                    _approachMinDistance = distanceToTarget;
                    _approachStalledBurns = 0;
                }
                else
                {
                    _approachStalledBurns++;
                }
                if (_approachStalledBurns >= AutopilotMaxStalledBurns)
                {
                    // Should be near-impossible now the arm-time rehearsal proves convergence, but if
                    // geometry drifts it still stands down LOUDLY (#147), not with a 1.5-s toast.
                    AutopilotStandDown($"autopilot handed back near {body.Name} — approach not converging after {_approachBurnCount} burns; fuel preserved");
                    return;
                }

                // Hill-aware approach (issue #136): the aim's safe periapsis and closing speed scale
                // to the body's well, so a deep moon like Enceladus actually reaches its capture band.
                int approachCost = OrbitRule.ApproachPulseCost(_ship, bodyPos, bodyVel, body, obstacle, hill);
                // #928 the tenth: the raw Δv price above is what a HAND would pay; the autopilot's own
                // charge comes from the accumulator, so the flown journey costs exactly the ⌈raw/10⌉ the
                // rehearsal quoted and the refusal arithmetic used.
                int approachCharge = AutopilotRehearsal.ChargeForBurn(_armedSpentPulses, approachCost);
                // The reserve floor (#146): the autopilot never burns the tank below the reserve it
                // promised to keep. If an approach burn would breach it, reality has diverged from the
                // rehearsed plan — only possible via something external (a manual burn, damage, a
                // dock) or a harder approach than rehearsed — so hand back LOUDLY instead of bleeding
                // the tank dry. The owner's ruling: never a silent drop.
                int reserveFloor = AutopilotRehearsal.ReservePulses(ReactionMassCapacity);
                if (_reactionMassPulses - approachCharge < reserveFloor)
                {
                    AutopilotStandDown($"autopilot handed back near {body.Name} — fuel plan broken (reserve floor reached; a manual burn, damage, or a harder approach than budgeted)");
                    return;
                }

                Vector2d beforeTheApproach = _ship.Velocity;
                _ship = OrbitRule.Approach(_ship, bodyPos, bodyVel, body, obstacle, hill);
                _reactionMassPulses -= approachCharge;
                _armedSpentPulses += approachCost;
                _approachBurnCount++;
                // #167 BURN KIND 4/9 - THE AUTOPILOT'S APPROACH BURN.
                BurnFired(approachCharge, _ship.Velocity - beforeTheApproach);
                StaleFutureNodes();
                ShowPulseMessage($"Approach burn — falling toward {body.Name} ({approachCharge} p) 🛰");
                return;

            case OrbitRule.AutopilotAction.Insert:
                int cost = OrbitRule.PulseCost(_ship, bodyPos, bodyVel, body);
                int insertCharge = AutopilotRehearsal.ChargeForBurn(_armedSpentPulses, cost); // #928 the tenth
                // The insertion is the arrival — it may dip into the reserve to complete the park.
                // Only an outright can't-afford-it (post-rehearsal, only via external divergence)
                // stands it down.
                if (insertCharge > _reactionMassPulses)
                {
                    AutopilotStandDown($"autopilot handed back at {body.Name} — insertion needs {insertCharge} p, only {_reactionMassPulses} left (fuel plan broken externally)");
                    return;
                }

                Vector2d beforeTheInsertion = _ship.Velocity;
                _ship = OrbitRule.Insert(_ship, bodyPos, bodyVel, body);
                _reactionMassPulses -= insertCharge;
                _armedSpentPulses += cost;
                // #167 BURN KIND 5/9 - THE AUTOPILOT'S INSERTION - the arrival, and the biggest burn in the
                // game. The pulse scaling is what makes it read as one against a hand's single trim.
                BurnFired(insertCharge, _ship.Velocity - beforeTheInsertion);
                // Friday §0 (owner ruling): "armed auto-orbit ends in a KEPT orbit, not an achieved
                // one." The park is NOT a handback — the autopilot stays in command and now STATION-KEEPS
                // the orbit (holds it with trims, priced from Lab 25). _armedOrbitBodyId stays set to the
                // kept body; _orbitKept flips keeping on. The transfer/approach machinery is done, so its
                // budget/schedule/plan-path clear, but keeping owns the ship until the captain disarms
                // (double-confirm) or the tank runs dry (a loud handback). Never "you have the ship" while
                // circling a moon by luck (#176/#184).
                _autopilotStandDownReason = null;
                _dockReadyStatus = null;
                _armedBudgetPulses = 0;
                _armedSpentPulses = 0;
                _autopilotPlanPath = null;
                _autopilotPlanClosestPass = null; // #196: park reached — the plan is consumed; ballistic alarm resumes
                _autopilotPlanBodyClearance = null; // #962: …and the park watchdog is the keeping rule's now
                _armedTransferSchedule = null;
                _armedTransferSummary = null;
                _armedTransferBurnsFired = 0;
                ResetApproachTracking();
                double park = keptPark; // #286: the clamped park the keeper trims back to (clears the parent)
                _orbitKept = true;
                _keepTrimPulsesPerDay = OrbitKeepingTable.TrimPulsesPerDay(
                    body, hill, parent.Mu, body.OrbitRadius, _ship.Velocity.Length);
                _keepNextCheckTime = SimTime + OrbitKeeping.TrimCadenceFraction * OrbitRule.LocalOrbitPeriod(park, body.Mu);
                _keepTrimsFired = 0;
                ArrivedAt(body.Id);
                TheArrivalIsRemembered(body.Id);   // #973 L4: a place can finish a page you don't remember writing
                StaleFutureNodes();
                double parkedRadius = (_ship.Position - bodyPos).Length;
                string holds = $"🛰 AUTOPILOT HOLDS THE ORBIT — {body.Name}, {FormatDistance(parkedRadius)}, trim ≈{_keepTrimPulsesPerDay} p/day";
                LogAutopilotEvent($"autopilot parked at {body.Name} — now HOLDING the orbit at {FormatDistance(parkedRadius)}; trim ≈{_keepTrimPulsesPerDay} p/day until you disarm or the tank runs dry");
                Warp = 1;               // auto-drop so the arrival moment isn't blown past at warp
                _effectiveWarp = 1;
                CompleteBoundCargoRunQuests(); // a parcel bound for this moon haven delivers on the park (#175)
                ShowPulseMessage(holds);
                RendererInterop.PlayCue("board");
                return;
        }
    }
}
