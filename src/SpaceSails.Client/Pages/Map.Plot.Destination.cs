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

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — M24/M25/M26's destination: the body the captain MEANS to reach, its ETA, whether its closest pass can become an insertion, and everything the navigation-target panel says about that pass.
public partial class Map
{
    // ---- M24: destination — the body the captain MEANS to reach. Clicking a planet on the
    // map sets it; the orbit-assist panel then coaches the approach to that body instead of
    // whichever happens to be nearest ("Orbit Venus?" while aiming for Mercury). ----
    private string? _destinationBodyId;

    private void SetDestination(string? bodyId)
    {
        if (bodyId is null && _armedOrbitBodyId is not null && _armedOrbitBodyId == _destinationBodyId)
        {
            _armedOrbitBodyId = null; // clearing the destination also stands down its auto-insert
        }

        _destinationBodyId = bodyId;
        _bodyMenuBody = null;
        _passDirty = true; // recompute the destination's closest pass on the next tick

        // M26: keep the captain's articles honest — a nav destination becomes a Fly to order
        // when the ship had no standing order; a bigger order (Hunt, Trade run…) stands.
        if (bodyId is not null && _mission.Kind is MissionKind.FreeSailing or MissionKind.FlyTo)
        {
            _mission = new ShipMission(MissionKind.FlyTo, DestinationBodyId: bodyId);
        }
        else if (bodyId is null && _mission.Kind == MissionKind.FlyTo)
        {
            _mission = ShipMission.Default;
        }

        bool destDocks = bodyId is not null
            && _ephemeris?.Bodies.FirstOrDefault(b => b.Id == bodyId) is { } destBody
            && IsDockableHaven(destBody);
        ShowPulseMessage(bodyId is null
            ? "Destination cleared"
            : destDocks
                ? $"Destination set — {BodyName(bodyId)} ⚓ dock assist tracks it now"
                : $"Destination set — {BodyName(bodyId)} 🎯 orbit assist tracks it now");
        StateHasChanged();
    }

    // M26: bound into orbit at the destination — the voyage is over, the orders complete.
    private void ArrivedAt(string bodyId)
    {
        if (_destinationBodyId == bodyId)
        {
            _destinationBodyId = null;
        }

        if (_mission.Kind == MissionKind.FlyTo && _mission.DestinationBodyId == bodyId)
        {
            _mission = ShipMission.Default;
        }
    }

    // M26: time to destination, from the projected course's closest pass by it. A ballpark by
    // design — the projection refreshes every few sim-hours and on every plan edit.
    private string? DestinationEta()
    {
        // #147: while the autopilot has stood down, do NOT quote an arrival ETA on any desk chip —
        // nothing is flying the ship there, so "ETA 6 d" would be the very lie the owner caught.
        if (AutopilotStoodDown || _destinationBodyId is null || _destinationPass is not { } dp
            || dp.BodyId != _destinationBodyId || dp.SimTime <= SimTime)
        {
            return null;
        }

        return $"ETA {FormatDuration(dp.SimTime - SimTime)}";
    }

    // A closest pass can be turned into a planned insertion when it is a planet (not the sun)
    // and tight enough to matter. Returns the estimated pulse cost, or null when not orbitable.
    private int? PassIsOrbitable(ClosestApproach.Pass cp)
    {
        if (_ephemeris is null) return null;
        CelestialBody? body = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == cp.BodyId) { body = candidate; break; }
        }
        if (body is null || body.ParentId is null || body.Kind == BodyKind.Station) return null;

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; break; }
        }
        if (parent is null) return null;
        // Armable within capture range: from there the autopilot (M25) flies the rest.
        if (cp.Distance > OrbitRule.CaptureRange(OrbitRule.HillRadius(body, parent.Mu))) return null;

        // Estimate the burn from the sampled path: finite-difference ship velocity at the pass.
        Vector2d shipVel = SampledVelocityAt(cp.SimTime);
        double h = 1.0;
        Vector2d bodyVel = (_ephemeris.Position(cp.BodyId, cp.SimTime + h) - _ephemeris.Position(cp.BodyId, cp.SimTime - h)) / (2 * h);
        var passState = new ShipState(cp.ShipPosition, shipVel, cp.SimTime);
        Vector2d bodyPos = _ephemeris.Position(cp.BodyId, cp.SimTime);
        return OrbitRule.PulseCost(passState, bodyPos, bodyVel, body);
    }

    // M25: everything the navigation-target panel says about the destination's closest pass.
    private readonly record struct DestPassInfo(double CaptureRange, bool InRange, double RelSpeed, int EstPulses);

    private DestPassInfo? DestinationPassInfo(ClosestApproach.Pass cp)
    {
        if (_ephemeris is null) return null;
        CelestialBody? body = null;
        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == cp.BodyId) { body = candidate; }
        }
        if (body?.ParentId is null) return null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; }
        }
        if (parent is null) return null;

        double captureRange = OrbitRule.CaptureRange(OrbitRule.HillRadius(body, parent.Mu));
        Vector2d shipVel = SampledVelocityAt(cp.SimTime);
        double h = 1.0;
        Vector2d bodyVel = (_ephemeris.Position(cp.BodyId, cp.SimTime + h) - _ephemeris.Position(cp.BodyId, cp.SimTime - h)) / (2 * h);
        Vector2d bodyPos = _ephemeris.Position(cp.BodyId, cp.SimTime);
        var passState = new ShipState(cp.ShipPosition, shipVel, cp.SimTime);
        int estPulses = OrbitRule.PulseCost(passState, bodyPos, bodyVel, body);
        return new DestPassInfo(captureRange, cp.Distance <= captureRange, (shipVel - bodyVel).Length, estPulses);
    }

    private void ScrubToDestinationPass(ClosestApproach.Pass cp) =>
        _scrubOffsetSeconds = Math.Max(0, cp.SimTime - _ship.SimTime);

    // #838: the secant of the bracketing sample pair — the ghost's velocity — now lives in Core
    // (NodeFrame.VelocityAt), so the pass estimate here and the planner's four quick selects read the
    // ribbon the same way.
    private Vector2d SampledVelocityAt(double simTime) =>
        NodeFrame.VelocityAt(_samples, simTime, _ship.Velocity);

}
