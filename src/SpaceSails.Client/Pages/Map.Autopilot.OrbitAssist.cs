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

// Map.Autopilot.OrbitAssist — THE BUS STOP IN SPACE (M20): what the orbit panel reads off the geometry,
// and the one line of approach coaching it says about it. `OrbitAssistInfo` is the whole readout —
// distance, relative speed, the window, the two costs, in/out of capture range, bound or not — built for
// the body that OWNS the panel (destination, then the armed target, then the nearest), and
// `OrbitStatusLine` turns it into the single sentence that says what to fix first. The two other derived
// numbers beside them read the same geometry: the achieved bound orbit's period (#265) and the circular
// speed at the ship's current radius (M16, the pilot's most-wanted number).
//
// Nothing here fires the drive. Every burn site in the family stayed in `Map.Autopilot.cs`.
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Autopilot.cs` — see the note at the head of that file for
// why it was cut and what "pure motion" is holding here.
public partial class Map
{
    // ---- M20: the bus stop in space ----
    public readonly record struct OrbitAssistInfo(
        CelestialBody Body, double Distance, double RelSpeed, double Hill, int Cost,
        bool WindowOpen, bool TooFast, bool CanEngage, bool IsDestination,
        double CaptureRange, bool InCaptureRange, bool Armed, int ApproachCost,
        bool Bound, bool RadiusInStableBand);

    private OrbitAssistInfo? OrbitInfo()
    {
        // The chosen destination owns the panel, then an armed target: "Orbit Earth?" while
        // sailing for Mars was exactly the confusion the owner reported. Nearest is the
        // fallback for players who haven't picked anywhere yet.
        string? focusId = _destinationBodyId ?? _armedOrbitBodyId;
        CelestialBody? preferred = null;
        if (focusId is not null && _ephemeris is not null)
        {
            foreach (CelestialBody candidate in _ephemeris.Bodies)
            {
                if (candidate.Id == focusId) { preferred = candidate; break; }
            }
        }

        if (preferred is not null && preferred.ParentId is not null)
        {
            Vector2d pos = _ephemeris!.Position(preferred.Id, SimTime);
            double h = 1.0;
            Vector2d vel = (_ephemeris.Position(preferred.Id, SimTime + h) - _ephemeris.Position(preferred.Id, SimTime - h)) / (2 * h);
            return BuildOrbitInfo(preferred, pos, vel);
        }

        if (_nearestBody is not CelestialBody body || body.ParentId is null || _ephemeris is null)
        {
            return null; // the sun is not a bus stop; you already orbit it
        }

        return BuildOrbitInfo(body, _nearestBodyPosition, _nearestBodyVelocity);
    }

    private OrbitAssistInfo? BuildOrbitInfo(CelestialBody body, Vector2d bodyPos, Vector2d bodyVel)
    {
        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris!.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; break; }
        }
        if (parent is null) return null;

        double hill = OrbitRule.HillRadius(body, parent.Mu);
        double distance = (_ship.Position - bodyPos).Length;
        bool destination = _destinationBodyId == body.Id;
        bool focused = destination || _armedOrbitBodyId == body.Id;
        if (!focused && distance > Math.Max(OrbitRule.IndicatorRangeHillRadii * hill, 2e9))
        {
            return null;
        }

        double relSpeed = (_ship.Velocity - bodyVel).Length;
        bool open = OrbitRule.WindowOpen(_ship, bodyPos, bodyVel, body, hill);
        int cost = OrbitRule.PulseCost(_ship, bodyPos, bodyVel, body);
        bool bound = OrbitRule.IsBound(_ship, bodyPos, bodyVel, body, hill);
        double captureRange = OrbitRule.CaptureRange(hill);
        // #180: is the ship's CURRENT radius inside the tide-stable park band? The manual press
        // circularizes here, so this decides whether Enter-orbit parks now or hands to the autopilot.
        bool radiusInBand = OrbitRule.RadiusInStableBand(distance, body, hill);
        return new OrbitAssistInfo(body, distance, relSpeed, hill, cost,
            open && !bound, relSpeed >= OrbitRule.MaxRelativeSpeed,
            open && !bound && cost <= _reactionMassPulses, destination,
            captureRange, distance <= captureRange && !bound,
            _armedOrbitBodyId == body.Id,
            OrbitRule.ApproachPulseCost(_ship, bodyPos, bodyVel),
            bound, radiusInBand);
    }

    // One line of approach coaching: what to fix first, with a ballpark number on it.
    private string OrbitStatusLine(OrbitAssistInfo oi)
    {
        string inv(double v) => v.ToString("F1", CultureInfo.InvariantCulture);
        // #176: once bound the panel used to fall through to the "inside capture range" line and the
        // button greyed out mute — say plainly that we're already parked so nobody reads it as "off".
        if (oi.Bound) return $"bound — parked at {FormatDistance(oi.Distance)}";
        // #180: inside the window but above the tide-stable band — say the press won't park HERE.
        if (oi.WindowOpen)
            return oi.RadiusInStableBand
                ? "window OPEN"
                : "window open — this radius is tide-chaotic (Lab 16); autopilot parks you deeper";
        if (oi.Armed && oi.InCaptureRange)
            // #203: altitude above the surface, unit-labelled — the SAME number the banner's
            // "orbit-insert (alt N km)" row shows, never the raw orbital radius the panel used to quote.
            return $"autopilot flying the approach — insertion at ≈{FormatAltitude(OrbitRule.ParkingRadius(oi.Body, oi.Hill) - oi.Body.BodyRadius)}";
        if (oi.InCaptureRange) return "in capture range — auto-orbit can park you";
        // #153: once inside the capture range (e.g. already bound/orbiting) the gap goes NEGATIVE —
        // the old line printed "close in -2,982,642 km to capture range". Read it honestly instead:
        // report the distance to the body, not a nonsensical negative closing distance.
        string closing = oi.Distance < oi.CaptureRange
            ? $"inside capture range — {FormatDistance(oi.Distance)} from {oi.Body.Name}"
            : $"close in {FormatDistance(oi.Distance - oi.CaptureRange)} to capture range";
        return oi.TooFast
            ? $"{closing} (autopilot sheds the {inv((oi.RelSpeed - OrbitRule.MaxRelativeSpeed) / 1000)} km/s there)"
            : closing;
    }

    // #265 — the period (s) of the ship's currently-achieved bound orbit about its dominant body, or null
    // when the ship is on a transfer/hyperbolic leg (not captured). Reads the SAME body + Hill the orbit
    // panel judges capture against (OrbitInfo), so "bound" here and the panel's "bound — parked" never
    // disagree. Cheap: a finite-difference body velocity and one energy/√ in OrbitRule.BoundOrbitPeriod.
    private double? BoundOrbitPeriodSeconds()
    {
        if (_ephemeris is null || OrbitInfo() is not { Bound: true } oi)
        {
            return null;
        }
        Vector2d pos = _ephemeris.Position(oi.Body.Id, SimTime);
        const double h = 1.0;
        Vector2d vel = (_ephemeris.Position(oi.Body.Id, SimTime + h) - _ephemeris.Position(oi.Body.Id, SimTime - h)) / (2 * h);
        return OrbitRule.BoundOrbitPeriod(_ship, pos, vel, oi.Body, oi.Hill);
    }

    // The pilot's most-wanted number (owner, M16): the speed that holds a circular sun orbit
    // at the ship's CURRENT distance. Match it (tangentially) and you coast forever — the
    // difference between "matching the radius" and "matching the orbit".
    private double CircularSpeedHere
    {
        get
        {
            double r = _ship.Position.Length;
            if (r <= 0 || _ephemeris is null) return 0;
            double mu = 0;
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (body.ParentId is null && body.Mu > mu) mu = body.Mu;
            }
            return Math.Sqrt(mu / r);
        }
    }

}
