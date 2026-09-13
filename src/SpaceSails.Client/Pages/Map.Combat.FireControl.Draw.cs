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

// Subject: part of Map.Combat.FireControl (#251 split; the header note lives in
// Map.Combat.FireControl.cs) — THE ORRERY VIEW.

/// <summary>
/// #251 · THE LONG SHOT ON THE MAP. A 100 M km shot cannot live inside a 5 M km tactical circle, so
/// when the war room is up the live map behind it carries the whole geometry — the prey's predicted
/// track to t_hit, the planned round's transfer, the aim point and the honest dispersion circle.
///
/// <para>Split out of <c>Map.Combat.FireControl.cs</c> under #251 with no member renamed, re-scoped
/// or re-ordered. <b>The three <c>static readonly RgbaColor</c>s this pass paints with stayed in the
/// opening file</b> — a static field initializer of a partial class runs in the order the compiler
/// reads the FILES, which is this repo's sixth named bug class; the banner above them is here,
/// because it is the design record of this method and not of the colours.</para>
/// </summary>
public partial class Map
{
    // ---- The orrery view (owner + Gemini consult, 2026-07-05): the long shot on the map ----
    // A 100 M km shot cannot live inside a 5 M km tactical circle: when the war room is up, the
    // live map behind it carries the whole geometry — the prey's predicted track to t_hit, the
    // planned round's transfer, the aim point and the honest dispersion circle.

    private void DrawFirePlan()
    {
        // The interest target gets brackets even before a solution exists — the war room's
        // subject is never invisible on its own backdrop again.
        if (_interestTargetId is not null && InterestTargetState() is { } interest)
        {
            (float ix, float iy) = _camera.WorldToScreen(interest.Position);
            DrawCornerBrackets(ix, iy, 12f, FirePlanColor with { A = 160 });
            _renderer!.DrawText(ix + 15, iy - 8, $"🎯 {InterestTargetName() ?? _interestTargetId}",
                FirePlanColor with { A = 190 }, "11px sans-serif", TextAlign.Left);

            // The owner's ask, verbatim: "a graphical line from my ship to the target… showing
            // distance and direct shot options" — the raw geometry, before any solution.
            (float px, float py) = _camera.WorldToScreen(_ship.Position);
            Span<float> ray = stackalloc float[4];
            ray[0] = px; ray[1] = py; ray[2] = ix; ray[3] = iy;
            _renderer.DrawPolyline(ray, FirePlanColor with { A = 70 }, 1f);
            double distance = (interest.Position - _ship.Position).Length;
            double shortestFlight = distance / MaxMuzzleSpeed;
            string reachNote = shortestFlight > OrdnanceRule.SlugLifetimeSeconds ? "missile territory" : "slug or missile";
            _renderer.DrawText((px + ix) / 2, (py + iy) / 2 - 6,
                $"{FormatDistance(distance)} · shortest flight ≈ {FormatFlightTime(shortestFlight)} · {reachNote}",
                FirePlanColor with { A = 150 }, "11px sans-serif", TextAlign.Center);
        }

        // The fired round rides the drawn plan — mark the LIVE bullet loudly on this desk
        // (owner: "schedule shot… then its position should be tracked on this view").
        foreach (OrdnanceState round in _ordnance)
        {
            if (round.Spent)
            {
                continue;
            }

            (float ox, float oy) = _camera.WorldToScreen(round.State.Position);
            _renderer!.DrawCircle(ox, oy, 6f, null, OrdnanceColor, 1.5f);
            _renderer.DrawText(ox + 9, oy + 4,
                round.Round.Kind == OrdnanceKind.Missile ? "missile" : "slug",
                OrdnanceColor with { A = 190 }, "11px sans-serif", TextAlign.Left);
        }

        if (_fireSolutionPath.Count < 2)
        {
            return;
        }

        DrawWorldPolyline(_fireTargetPath, FirePlanTargetColor, 1f);
        DrawWorldPolyline(_fireSolutionPath, FirePlanColor, 1.6f);

        (float ax, float ay) = _camera.WorldToScreen(_fireAimPoint);
        float dispersionPx = (float)Math.Max(4, _fireDispersionMeters / _camera.MetersPerPixel);
        _renderer!.DrawCircle(ax, ay, dispersionPx, FirePlanDispersionColor with { A = 18 }, FirePlanDispersionColor, 1f);
        Span<float> cross = stackalloc float[4];
        cross[0] = ax - 6; cross[1] = ay - 6; cross[2] = ax + 6; cross[3] = ay + 6;
        _renderer.DrawPolyline(cross, FirePlanColor, 1.5f);
        cross[0] = ax - 6; cross[1] = ay + 6; cross[2] = ax + 6; cross[3] = ay - 6;
        _renderer.DrawPolyline(cross, FirePlanColor, 1.5f);
        double impactIn = _fireSolutionPath[^1].SimTime - SimTime;
        _renderer.DrawText(ax + 10, ay + 14,
            $"impact {(impactIn > 0 ? $"in {FormatFlightTime(impactIn)}" : "point")} · ±{FormatDistance(_fireDispersionMeters)}",
            FirePlanColor with { A = 190 }, "11px sans-serif", TextAlign.Left);
    }
}
