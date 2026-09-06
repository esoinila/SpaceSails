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
/// #928 · WHAT THE ARM COSTS, AND THE ONE BURN THAT IS NOT AN INSERTION — the rehearsal's charged quote,
/// the plan cached for the #180 alarm, the decision to fly a planned transfer at all, and the transfer
/// impulse itself.
///
/// <para>The tenth lives here: the autopilot flies at a tenth of the tank a hand would spend, so every
/// number the captain reads about an arm is the CHARGED one and never the raw &#916;v count. The first of
/// the family's five tank debits (<c>charge</c>) is spent in <c>ApplyTransferBurn</c>, and
/// <c>TheTenthIsQuotedAndOnlyTheAutopilotsTests</c> reads all five in order across the family.</para>
///
/// <para>Split out of <c>Map.Autopilot.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    private void ResetAutopilotBudget()
    {
        _armedBudgetPulses = 0;
        _armedSpentPulses = 0;
        _armedArrivalPassSimTime = null; // #969: the plan-time promise dies with the arm that made it
        _autopilotPlanPath = null;
        _autopilotPlanClosestPass = null; // #196: plan gone — the alarm returns to the ballistic course
        _autopilotPlanBodyClearance = null; // #962: …and the park watchdog returns to the raw conic
        _armedTransferSchedule = null;
        _armedTransferSummary = null;
        _armedTransferBurnsFired = 0;
        _orbitKept = false;
        _keepTrimPulsesPerDay = 0;
        _keepTrimsFired = 0;
    }

    // #219 one-arm semantics: the collision alarm's plan-trust is only sound if EVERY arm caches BOTH
    // the plan PATH (drawn as the #148 intended track, and the `armedWithPlan` gate in UpdateShipAlerts)
    // AND the plan's collision PASS, together — one drifting without the other is exactly the #196/#219
    // bug (a plan the ballistic alarm then judges raw). All arm entry points already funnel through the
    // single ToggleArmedInsertion — the destination card's Auto-orbit button, the nav-target panel's Arm
    // button, the body context menu (ToggleArmedInsertionFromMenu), the O-key, and the #183 out-of-band
    // manual press (EnterOrbit, when the current radius is tide-chaotic) — so this is the ONE place the
    // pair is set. The pass is the plan's ACHIEVED PARK, not its powered approach: a deliverable
    // rehearsal's coarse terminal coast grazes the surface a step before the insert lifts it back, and
    // that graze must NOT fire ROCKS AHEAD on a valid armed approach (AutopilotRehearsal.PlanCollisionPass).
    private void CachePlanForAlarm(string bodyId, AutopilotRehearsal.RehearsalResult r)
    {
        _autopilotPlanPath = r.Path;
        if (_ephemeris is null)
        {
            _autopilotPlanClosestPass = null;
            _autopilotPlanBodyClearance = null;
            return;
        }

        // One judged pass list, read two ways (#962): the worst of them is the collision alarm's plan
        // pass, and the per-body distances are what the park-degradation watchdog checks the bound body
        // against. Same scan, same arrival treatment, one arm-time cost.
        IReadOnlyList<ClosestApproach.Pass> passes = AutopilotRehearsal.PlanPasses(r, _ephemeris, bodyId);
        ClosestApproach.Pass? worst = null;
        var clearance = new Dictionary<string, double>(passes.Count, StringComparer.Ordinal);
        foreach (ClosestApproach.Pass pass in passes)
        {
            clearance[pass.BodyId] = pass.Distance;
            if (worst is null || pass.Severity < worst.Value.Severity)
            {
                worst = pass;
            }
        }

        _autopilotPlanClosestPass = worst;
        _autopilotPlanBodyClearance = clearance;
    }

    // #146: does this arm ride a cheap in-well transfer rather than the legacy approach loop? The target
    // is a moon of a moon-owning giant, the ship is free-flying INSIDE that giant's Hill sphere, and it
    // is still OUTSIDE the target's honest (floor-free) Hill-scaled capture range — the exact geometry
    // where OrbitRule.Approach re-sets the velocity every step and hemorrhages, and where the Lambert
    // planner rides the well cheaply instead.
    private bool ShouldPlanTransfer(CelestialBody target, out CelestialBody parent, out double targetHill)
    {
        parent = null!;
        targetHill = 0;
        if (_ephemeris is null || target.ParentId is null)
        {
            return false;
        }
        CelestialBody? p = _ephemeris.Bodies.FirstOrDefault(b => b.Id == target.ParentId);
        if (p is null || !_ephemeris.Bodies.Any(c => c.ParentId == p.Id && c.Kind == BodyKind.Moon))
        {
            return false; // parent must be a giant that owns moons
        }
        if (!ShipInsideHill(p))
        {
            return false; // must be free-flying in the well, not out in interplanetary space
        }
        targetHill = OrbitRule.HillRadius(target, p.Mu);
        double distance = (_ship.Position - _ephemeris.Position(target.Id, SimTime)).Length;
        if (distance <= OrbitRule.CaptureRangeHillRadii * targetHill)
        {
            return false; // already honestly near the moon — the terminal capture handles it directly
        }
        parent = p;
        return true;
    }

    // #146 split-advance executor: apply one scheduled transfer impulse from the ship's TRUE state at the
    // burn epoch (the tick loop advances exactly onto it), pricing it with the same OrbitRule.PulsesFor
    // the approach/insert burns spend and guarding the reserve floor. Can't afford it without breaching
    // the reserve → the #147 loud handback (reality diverged from the rehearsed plan, externally).
    private void ApplyTransferBurn(Vector2d deltaV)
    {
        if (_armedOrbitBodyId is null || _armedTransferSchedule is null)
        {
            return;
        }
        int cost = OrbitRule.PulsesFor(deltaV.Length, _ship.Velocity.Length);
        // #928: the autopilot flies at a tenth. The tank is charged ChargeForBurn against the raw ledger,
        // so the whole armed journey costs exactly the ⌈raw/10⌉ the rehearsal quoted at arm time.
        int charge = AutopilotRehearsal.ChargeForBurn(_armedSpentPulses, cost);
        int reserveFloor = AutopilotRehearsal.ReservePulses(ReactionMassCapacity);
        if (_reactionMassPulses - charge < reserveFloor)
        {
            AutopilotStandDown($"autopilot handed back mid-transfer to {BodyName(_armedOrbitBodyId)} — fuel plan broken (reserve floor reached before a departure burn)");
            return;
        }
        _ship = _ship with { Velocity = _ship.Velocity + deltaV };
        _reactionMassPulses -= charge;
        _armedSpentPulses += cost;
        _armedTransferBurnsFired++;
        // #167 BURN KIND 3/9 - THE SCHEDULED TRANSFER BURN, and the reason this lane exists at all: this one
        // fires ITSELF at its epoch, usually at four figures of warp, with no hand anywhere near the ship.
        // The flame is on the wall clock precisely so that this burn - whose whole sim-time existence is one
        // frame - is still on the glass for a second afterwards.
        BurnFired(charge, deltaV);
        StaleFutureNodes();
        ShowPulseMessage($"Transfer burn — riding the well toward {BodyName(_armedOrbitBodyId)} ({charge} p) 🛰");
    }
}
