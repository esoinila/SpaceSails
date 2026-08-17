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

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — #135/#145's plot frame: the one place a world sample is re-expressed, the frames worth offering and the picker, the speed readout, and how long a ribbon the frame's local timescale is worth.
public partial class Map
{
    // #135 — the ONE place a world sample gets re-expressed in the active frame before it hits the
    // camera. Sun/inertial (no frame body) returns the sample untouched — the pre-#135 path,
    // byte-identical. Otherwise it's ReferenceFrame.CoMoving with the frame body's ephemeris position
    // at the SAMPLE time and at "now" (_plotFrameAnchor, refreshed once per drawn frame). Every
    // time-parameterized draw funnels through this so nothing diverges. Anything drawn at "now" (live
    // bodies, the ship, NPCs) is the identity here anyway (bodyF(now) − bodyF(now) = 0), so those keep
    // their untouched WorldToScreen call and never move under a frame change.
    //
    // #143 — the frame governs BOTH views now, not just Plot. The owner hit a heliocentric ribbon/scrub
    // path while auto-orbiting Titan inside Saturn's system: #138 deliberately gated this on Plot mode,
    // which left the Play-view ribbon and prediction cone solar. One selection, both views — so there is
    // no PlotMode gate here anymore. The anchor is refreshed every drawn frame regardless of mode, and
    // the Sun frame (_plotFrameBodyId is null) is still the byte-identical short-circuit.
    private Vector2d PlotFrame(Vector2d world, double simTime)
    {
        if (_plotFrameBodyId is null || _ephemeris is null)
        {
            return world;
        }
        return ReferenceFrame.CoMoving(world, _ephemeris.Position(_plotFrameBodyId, simTime), _plotFrameAnchor);
    }

    // #135 — one entry in the plot's frame selector. Id == null is Sun/inertial.
    private sealed record FrameOption(string? Id, string Label, string Title, bool Suggested);

    // The frames worth offering, in reading order: Sun (inertial), then any body whose Hill sphere
    // currently holds the ship (the local giant) and that giant's moons, then the nav target / picked
    // contact, and always the frame in use so you can never get stranded in a frame with no chip.
    private List<FrameOption> FrameOptions()
    {
        string? suggestId = SuggestedFrameBodyId();
        var opts = new List<FrameOption> { new(null, "Sun", "Heliocentric (inertial) — the default solar frame", false) };
        if (_ephemeris is null)
        {
            return opts;
        }

        var seen = new HashSet<string>();
        void Add(string? id)
        {
            if (id is null || id == "sun" || !seen.Add(id))
            {
                return;
            }
            CelestialBody? b = _ephemeris.Bodies.FirstOrDefault(x => x.Id == id);
            if (b is null)
            {
                return;
            }
            opts.Add(new FrameOption(b.Id, b.Name, $"Draw the plotted course co-moving with {b.Name}", b.Id == suggestId));
        }

        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (!ShipInsideHill(body))
            {
                continue;
            }
            Add(body.Id);
            foreach (CelestialBody child in _ephemeris.Bodies)
            {
                if (child.ParentId == body.Id && child.Kind != BodyKind.Station)
                {
                    Add(child.Id);
                }
            }
        }
        Add(_destinationBodyId);
        if (_selectedTargetId is not null && _ephemeris.Bodies.Any(b => b.Id == _selectedTargetId))
        {
            Add(_selectedTargetId);
        }
        Add(_plotFrameBodyId);   // never orphan the active frame
        return opts;
    }

    // The frame to nudge the player toward: the giant (a body that owns a moon system) whose Hill
    // sphere currently holds the ship. Largest Hill wins, so from inside a moon's Hill sphere it's
    // still the giant that gets suggested, not the moon. Never auto-applied — just highlighted.
    private string? SuggestedFrameBodyId()
    {
        if (_ephemeris is null)
        {
            return null;
        }
        string? best = null;
        double bestHill = 0;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            bool ownsMoons = _ephemeris.Bodies.Any(c => c.ParentId == body.Id && c.Kind != BodyKind.Station);
            if (!ownsMoons || !ShipInsideHill(body, out double hill))
            {
                continue;
            }
            if (hill > bestHill)
            {
                (bestHill, best) = (hill, body.Id);
            }
        }
        return best;
    }

    private bool ShipInsideHill(CelestialBody body) => ShipInsideHill(body, out _);

    private bool ShipInsideHill(CelestialBody body, out double hill)
    {
        hill = 0;
        if (_ephemeris is null || body.ParentId is null)
        {
            return false;
        }
        CelestialBody? parent = _ephemeris.Bodies.FirstOrDefault(b => b.Id == body.ParentId);
        if (parent is null)
        {
            return false;
        }
        hill = OrbitRule.HillRadius(body, parent.Mu);
        double distance = (_ship.Position - _ephemeris.Position(body.Id, SimTime)).Length;
        return distance < hill;
    }

    // #135 — the ship's speed IN the selected frame, labelled with it, so the number on the plot
    // panel never silently disagrees with the frame chip (the documented mixed-frame trap).
    private string FrameSpeedReadout()
    {
        if (_ephemeris is null)
        {
            return string.Empty;
        }
        Vector2d frameVel = Vector2d.Zero;
        if (_plotFrameBodyId is not null)
        {
            const double h = 1.0;
            frameVel = (_ephemeris.Position(_plotFrameBodyId, SimTime + h) - _ephemeris.Position(_plotFrameBodyId, SimTime - h)) / (2 * h);
        }
        double relKmps = (_ship.Velocity - frameVel).Length / 1000.0;
        string label = _plotFrameBodyId is null ? "v helio" : $"v rel {BodyName(_plotFrameBodyId)}";
        return $"{label}: {relKmps.ToString("N1", CultureInfo.InvariantCulture)} km/s";
    }

    // #926 — the name of the frame the plan is being READ in. Derived, never stored: the one truth is
    // _plotFrameBodyId, and null is the Sun / inertial frame everywhere in this family.
    private string PlotFrameName() => _plotFrameBodyId is null ? "Sun" : BodyName(_plotFrameBodyId);

    // #926 · THE TRIP'S FRAME, AND WHETHER IT IS WORTH OFFERING. Owner (2026-08-17, playing #916's vector
    // planner): "the real thrust amounts are dependent on the coordinate origin. I had to remember to
    // switch to Sun to get the ship to really start moving from Earth towards Mars."
    //
    // The offer stands exactly when all of these hold: the plan HAS a destination, and the trip's frame —
    // the common parent of the ghost at THIS node and that destination, Core's TripFrame — is not the
    // frame being read. Nothing is cached and no new field is kept: both halves of the state already
    // exist (_plotFrameBodyId + _destinationBodyId), so this is a question asked as the panel draws.
    //
    // Ruling A, not B: the planner OFFERS. It never switches by itself.
    private (string? FrameId, string FrameName, string DestinationName)? TripFrameOffer(double simTime)
    {
        if (_ephemeris is null || _destinationBodyId is null)
        {
            return null;
        }
        if (TripFrame.At(SamplePositionAt(simTime), _destinationBodyId, _plotFrameBodyId, _ephemeris, simTime)
            is not { } offer)
        {
            return null;   // no trip, or already read in the frame the trip is in — nothing to say
        }
        return (offer.TripFrameBodyId,
                offer.TripFrameBodyId is null ? "Sun" : BodyName(offer.TripFrameBodyId),
                BodyName(_destinationBodyId));
    }

    private void SetPlotFrame(string? bodyId)
    {
        _plotFrameBodyId = bodyId;
        if (bodyId is not null && _ephemeris is not null)
        {
            _plotFrameAnchor = _ephemeris.Position(bodyId, SimTime);
        }
    }

    // #206 — the every-body frame overflow. Controlled: its value mirrors the live frame (empty = the
    // "frame… ▾" placeholder, which is the Sun / inertial default), so picking never leaves the control
    // out of step with the active origin. The pick also flows through SetPlotFrame, so the chip row and
    // both views stay one truth (#144).
    private void OnFramePicked(ChangeEventArgs e)
    {
        string? picked = e.Value?.ToString();
        SetPlotFrame(string.IsNullOrEmpty(picked) ? null : picked);
    }

    // #206 — every body in the scenario, grouped by parent for the overflow picker: the Sun's children
    // (the planets, plus any sun-orbiting station) under "Planets", then each planet's moons + stations
    // under the planet's name. Body order within a group follows the ephemeris. A parent with no
    // children yields no group.
    private List<(string Label, List<CelestialBody> Members)> FramePickerGroups()
    {
        var groups = new List<(string, List<CelestialBody>)>();
        if (_ephemeris is null)
        {
            return groups;
        }
        foreach (CelestialBody parent in _ephemeris.Bodies)
        {
            List<CelestialBody> members = _ephemeris.Bodies.Where(b => b.ParentId == parent.Id).ToList();
            if (members.Count == 0)
            {
                continue;
            }
            groups.Add((parent.Id == "sun" ? "Planets" : parent.Name, members));
        }
        return groups;
    }

    // #145 — the DISPLAYED trajectory length, scaled to the frame's local timescale. null in the
    // Sun/inertial frame (draw the full ribbon, byte-identical to pre-#145) or when the frame body
    // has no local orbit to scale to (the Sun itself, a mass-less dock). Otherwise: how many seconds
    // of the ribbon to draw — ~1.25 local orbital periods at the ship's CURRENT radius around the
    // frame body (one sqrt), floored so the imminent step is never hidden and ceilinged at the full
    // projection. Recomputed per frame, so the arc tightens as the ship falls in.
    private double? FrameDisplayWindowSeconds()
    {
        if (FrameLocalWindowSeconds() is null)
        {
            return null; // the Sun / inertial frame, or a mass-less dock — no truncation, full ribbon
        }
        return ResolveRibbon().DrawnSeconds;
    }

    // #145/#209 — ~1.25 local orbital periods of the current frame body (one sqrt), or null when there
    // is nothing to scale to: the Sun / inertial frame, or a mass-less dock. Null is the byte-identical
    // full-length draw path; a value is the local timescale the drawn ribbon is scaled to.
    private double? FrameLocalWindowSeconds()
    {
        if (_plotFrameBodyId is null || _ephemeris is null)
        {
            return null;
        }
        CelestialBody? frame = _ephemeris.Bodies.FirstOrDefault(b => b.Id == _plotFrameBodyId);
        if (frame is null || frame.ParentId is null || frame.Mu <= 0)
        {
            return null;
        }
        double radius = (_ship.Position - _ephemeris.Position(frame.Id, SimTime)).Length;
        if (!(radius > 0))
        {
            return null;
        }
        return FrameWindowLocalPeriods * OrbitRule.LocalOrbitPeriod(radius, frame.Mu);
    }

    // #145.5 — the frame-INDEPENDENT floor: never hide the near-term course. A few hours, stretched to
    // the next imminent FUTURE plan node + margin so the flight plan's NEXT line and the ribbon can't
    // contradict. Plan-awareness (the FURTHEST encounter) is folded in by PlotHorizon.DrawnWindow.
    private double FrameWindowBaseFloorSeconds()
    {
        double floor = FrameWindowFloorSeconds;
        double nextNode = double.PositiveInfinity;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && node.SimTime > SimTime && node.SimTime < nextNode)
            {
                nextNode = node.SimTime;
            }
        }
        if (double.IsFinite(nextNode))
        {
            floor = Math.Max(floor, (nextNode - SimTime) + FrameWindowNodeMarginSeconds);
        }
        return floor;
    }

    // #145/#209 — the resolved drawn ribbon window + the honest note explaining its length. ONE call, so
    // the drawn ribbon (DrawShipTrajectory) and the panel note (RibbonHorizonNote) never disagree.
    private PlotHorizon.RibbonResult ResolveRibbon() =>
        PlotHorizon.DrawnWindow(
            CurrentPlotHorizonSeconds, FrameLocalWindowSeconds() ?? 0, PlanFurthestEpochSeconds(), FrameWindowBaseFloorSeconds());

    // #209 — the say-the-state note for the Plotting panel: when the ribbon is cropped shorter than the
    // full picture, name WHY, so the captain never reads a silently short path. null when the ribbon
    // reaches the plan / the full projection.
    private (string Text, bool Warn)? RibbonHorizonNote()
    {
        PlotHorizon.RibbonResult r = ResolveRibbon();
        return r.Note switch
        {
            // FrameLocalPeriods implies a scalable frame body (local window > 0), so it is never null here.
            PlotHorizon.RibbonNote.FrameLocalPeriods =>
                ($"ribbon: {FrameWindowLocalPeriods.ToString("0.##", CultureInfo.InvariantCulture)} {BodyName(_plotFrameBodyId!)} periods (frame auto)", false),
            PlotHorizon.RibbonNote.CappedShortOfPlan =>
                ($"ribbon capped at {FormatHorizon(r.DrawnSeconds)} — plan runs to {FormatHorizon(PlanFurthestEpochSeconds())}", true),
            _ => null,
        };
    }

}
