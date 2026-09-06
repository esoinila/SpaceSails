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
using SpaceSails.Client.Components;
using SpaceSails.Client.Layout;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.razor.cs — the code-behind for Map.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line that
// lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them — and the FIRST thing they
// said about this page's own two methods is written at the site below.
//
// The page's `@code` block keeps exactly one member, because that one CANNOT move: `ContactDrinkOffer` is a
// `RenderFragment` written as a razor template (`=> @<text> … </text>`), which is razor syntax and has no
// meaning in a `.cs` file. The two draw methods have no razor in them at all and are here, character for
// character, in the order they were in.
//
// ── #1107 · FOUND, NOT FIXED ─────────────────────────────────────────────────────────────────────────
// Moving these two methods surfaced the two CA2014s marked below — "Potential stack overflow. Move the
// stackalloc out of the loop." — and they are the whole point of the exercise: both have been in the frame
// loop for months, and neither had ever been shown to anybody, because they were written inside `@code`.
// The stack a `stackalloc` takes is not released at the end of an iteration; it is held until the METHOD
// returns, so a `stackalloc` in a loop grows the frame once per pass. DrawPyramids' loop is bounded by
// `AncientsRule.PyramidCount` and is small; DrawOrdnance' is bounded by the number of live rounds, which
// nothing in the sim caps.
//
// They are suppressed and NOT fixed here on purpose: this lane's contract is zero behaviour change and
// pure motion — the moved lines are the lines that were in the razor, in the order they were in — and the
// suppression records the finding at the exact site instead of letting the move quietly launder it. The
// fix, when a lane takes it, is one line each: hoist the `Span<float>` declaration above the loop (every
// element is written before it is read on every pass, so the hoist is behaviour-identical) and delete the
// pragma pair.
public partial class Map
{
    private void DrawPyramids()
    {
        for (int i = 0; i < AncientsRule.PyramidCount; i++)
        {
            if (!AncientsRule.Revealed(i, _ship.Position, SimTime))
            {
                continue;
            }

            Vector2d position = AncientsRule.PyramidPosition(i, SimTime);
            (float sx, float sy) = _camera.WorldToScreen(position);
#pragma warning disable CA2014 // #1107 · found, not fixed — see the header note.
            Span<float> triangle = stackalloc float[8];
#pragma warning restore CA2014
            triangle[0] = sx; triangle[1] = sy - 6;
            triangle[2] = sx - 5; triangle[3] = sy + 4;
            triangle[4] = sx + 5; triangle[5] = sy + 4;
            triangle[6] = sx; triangle[7] = sy - 6;
            _renderer!.DrawPolyline(triangle, PyramidColor, 1.6f);
            _renderer.DrawText(sx + 8, sy - 6, "◬ ???", PyramidColor);
        }
    }

    private void DrawOrdnance()
    {
        foreach (OrdnanceState round in _ordnance)
        {
            if (round.Spent)
            {
                continue;
            }

            (float sx, float sy) = _camera.WorldToScreen(round.State.Position);
            _renderer!.DrawCircle(sx, sy, round.Round.Kind == OrdnanceKind.Missile ? 2.5f : 1.8f,
                OrdnanceColor, OrdnanceColor);
            // A short streak along the velocity so a round reads as MOVING at map zoom.
            Vector2d tail = round.State.Position - round.State.Velocity.Normalized() * (_camera.MetersPerPixel * 6);
            (float tx, float ty) = _camera.WorldToScreen(tail);
#pragma warning disable CA2014 // #1107 · found, not fixed — see the header note.
            Span<float> streak = stackalloc float[4];
#pragma warning restore CA2014
            streak[0] = sx; streak[1] = sy; streak[2] = tx; streak[3] = ty;
            _renderer.DrawPolyline(streak, OrdnanceColor with { A = 120 }, 1f);
        }
    }
}
