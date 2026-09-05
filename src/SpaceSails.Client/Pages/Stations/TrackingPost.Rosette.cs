// #251 item 1 · THE TRACKING POST'S CODE-BEHIND, ON ITS OWN FILE.
//
// THE ROSETTE (SVG): the sun-relative detection envelope, drawn from TelescopeModel.Range
// itself so the picture can never drift from what the sweep logic actually does — plus the
// wedge, the target bearings, the prograde arrow and the two sentences under the dial.
//
// PURE MOTION: the members below are the members that stood in TrackingPost.razor's @code block,
// character for character, at the same indentation and in the same order. Nothing was renamed,
// resignatured or reordered — the whole change is which file the lines sit in. The desk is one
// `partial class TrackingPost`; the razor file keeps the markup and nothing else. (Map.razor's own
// code came out into 155 `Map.*.cs` partials in 2026-07 for exactly this reason; the tracking post
// is the desk that never got the same treatment, which is how it reached 1,473 lines.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

public partial class TrackingPost
{
    // ---- Rosette (SVG) ----

    // The envelope is drawn in a LOCAL frame where angle 0 = straight up = sunward (worst range)
    // and angle π = straight down = anti-sunward (best range) — the "sun-direction egg" from the
    // worldbuilding notes. Reusing TelescopeModel.Range itself (rather than re-deriving the
    // formula here) means the picture can never drift from what the sweep logic actually does:
    // a synthetic ship position on +X has its sunward direction dead on -X, so sampling look
    // directions around that circle traces exactly the same envelope Range() would compute for
    // any real ship, just re-expressed in sun-relative angle instead of world bearing.
    private double EnvelopeFraction(double localAngleRad)
    {
        var syntheticShip = new Vector2d(1, 0);
        var lookDir = new Vector2d(-Math.Cos(localAngleRad), Math.Sin(localAngleRad));
        return _telescope.Range(syntheticShip, lookDir) / _telescope.BaseRange;
    }

    private double SunwardBearingRad => TrackingStation.Bearing(Vector2d.Zero - ShipPosition);

    /// <summary>World bearing converted into the rosette's sun-relative local angle.</summary>
    private double ToLocalAngle(double worldBearingRad)
    {
        double diff = (worldBearingRad - SunwardBearingRad) % Math.Tau;
        if (diff < 0)
        {
            diff += Math.Tau;
        }

        return diff;
    }

    private (double X, double Y) RosettePoint(double localAngleRad, double radiusFraction)
    {
        double a = localAngleRad - Math.PI / 2; // 0 => straight up on screen
        double r = radiusFraction * RosetteMaxRadiusPx;
        return (RosetteCenterPx + r * Math.Cos(a), RosetteCenterPx + r * Math.Sin(a));
    }

    private string RosettePolygonPoints()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i <= RosetteSteps; i++)
        {
            double localAngle = Math.Tau * i / RosetteSteps;
            (double x, double y) = RosettePoint(localAngle, EnvelopeFraction(localAngle));
            sb.Append(x.ToString("F1")).Append(',').Append(y.ToString("F1")).Append(' ');
        }

        return sb.ToString();
    }

    private string WedgePolygonPoints()
    {
        // #962: one telescope, one aim — the rosette wedge reads the same CurrentScan the heading line
        // does, so a SCHEDULED pass (not just a manual sweep) visibly swings the drawn wedge too.
        ScanJob? look = CurrentScan?.Job ?? _activeJob;
        double bearingRad = look?.CenterBearingRad ?? _centerBearingDeg * Math.PI / 180.0;
        double arcDeg = look?.ArcWidthRad * 180.0 / Math.PI ?? _arcWidthDeg;
        double halfArc = (arcDeg * Math.PI / 180.0) / 2;

        double centerLocal = ToLocalAngle(bearingRad);

        var sb = new System.Text.StringBuilder();
        sb.Append(RosetteCenterPx.ToString("F1")).Append(',').Append(RosetteCenterPx.ToString("F1")).Append(' ');
        const int wedgeSteps = 16;
        for (int i = 0; i <= wedgeSteps; i++)
        {
            double localAngle = centerLocal - halfArc + (2 * halfArc) * i / wedgeSteps;
            (double x, double y) = RosettePoint(localAngle, EnvelopeFraction(localAngle));
            sb.Append(x.ToString("F1")).Append(',').Append(y.ToString("F1")).Append(' ');
        }

        return sb.ToString();
    }

    private IEnumerable<(double X, double Y, string Color, bool Threat)> TrackedTargetDots()
    {
        foreach (TrackedTarget entry in _ledger.Entries)
        {
            TrackingCandidate? candidate = FindCandidate(entry.ShipId);
            Vector2d position = candidate?.State.Position ?? entry.LastObservation.Position;
            Vector2d toTarget = position - ShipPosition;
            if (toTarget.LengthSquared == 0)
            {
                continue;
            }

            double bearing = TrackingStation.Bearing(toTarget);
            double localAngle = ToLocalAngle(bearing);
            double range = _telescope.Range(ShipPosition, toTarget);
            double fraction = range > 0 ? Math.Clamp(toTarget.Length / range, 0, 1) : 1;
            (double x, double y) = RosettePoint(localAngle, fraction);

            bool threat = candidate?.IsThreat ?? false;
            double quality = entry.EffectiveQuality(SimTime);
            string color = threat ? "#ff5a5a" : quality > 0.5 ? "#7dffb0" : quality > 0.2 ? "#ffd27d" : "#ff8d7d";
            yield return (x, y, color, threat);
        }
    }

    // M27: the prograde arrow — where the ship's nose actually points on the rosette.
    private (double X, double Y)? ProgradePoint()
    {
        if (ShipVelocity.LengthSquared == 0)
        {
            return null;
        }

        double localAngle = ToLocalAngle(TrackingStation.Bearing(ShipVelocity));
        return RosettePoint(localAngle, 0.55);
    }

    /// <summary>
    /// #962 · THE READOUT NAMES THE LOOK THE INSTRUMENT IS ACTUALLY TAKING. Owner, after ordering the
    /// scope onto the collector: <i>"why is our scan looking at our destination when I press sharpen fix
    /// on the debt collector. It is like our telescope pirate is high on drugs."</i>
    ///
    /// <para>Half of that was a dead button (Map.Npc.TrackShipFromMenu, which bailed on every hunter id) —
    /// but the other half was this readout lying on its own account, and that is the repo's own named bug
    /// class: the sim doing one thing while a SENTENCE reports another. <c>_centerBearingDeg</c> is written
    /// by exactly one thing in the whole codebase — the manual Bearing slider. Every queued pass aims the
    /// instrument through <see cref="JobFor"/> into a different variable entirely, so whatever the telescope
    /// was truly looking at, this line went on quoting the slider's last resting place.</para>
    ///
    /// <para>So the aim is read from <see cref="CurrentScan"/> — the ONE answer the wedge overlay already
    /// draws from (one telescope, one truth) — and the line names the job being served, so "looking 146°
    /// off the bow" can be checked against "🎯 Debt Collector" in the same breath. The slider is the
    /// fallback only when nothing is queued and nothing is sweeping: the one case where it IS the aim.</para>
    /// </summary>
    private double LookBearingDeg()
    {
        double rad = CurrentScan is { } scan ? scan.Job.CenterBearingRad : _centerBearingDeg * Math.PI / 180.0;
        double deg = rad * 180.0 / Math.PI % 360;
        return deg < 0 ? deg + 360 : deg;
    }

    // M27: are we looking ahead or astern? The scope aim vs the velocity vector, in words.
    private string HeadingLine()
    {
        double scopeDeg = LookBearingDeg();
        string job = CurrentScan is { } scan ? $" · {scan.Label}" : "";
        if (ShipVelocity.LengthSquared == 0)
        {
            return $"scope {(int)scopeDeg}° · ship adrift{job}";
        }

        double progradeDeg = TrackingStation.Bearing(ShipVelocity) * 180.0 / Math.PI;
        if (progradeDeg < 0)
        {
            progradeDeg += 360;
        }

        double off = Math.Abs(scopeDeg - progradeDeg) % 360;
        if (off > 180)
        {
            off = 360 - off;
        }

        return $"scope {(int)scopeDeg}° · prograde {(int)progradeDeg}° — looking {(int)off}° off the bow ({(off <= 90 ? "ahead" : "astern")}){job}";
    }
}
