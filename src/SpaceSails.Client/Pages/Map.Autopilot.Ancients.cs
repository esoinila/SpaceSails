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

// Map.Autopilot.Ancients — THE ANCIENTS' PILOT (M28, Sunday PR-D). The pyramid satellites grant charges
// to a ship that comes close enough to touch, and a charge spends into one Simulator-evaluated course to
// the current destination — scarce alien assistance beside the autopilot, never a replacement for it.
// Manual flight stays the taught skill.
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Autopilot.cs` — see the note at the head of that file for
// why it was cut and what "pure motion" is holding here.
public partial class Map
{
    // ---- M28 (Sunday PR-D): the Ancients' pilot — pyramid satellites & auto-plot charges ----
    private int _ancientCharges;
    private readonly double[] _ancientLastGrant =
        [double.NegativeInfinity, double.NegativeInfinity];
    private static readonly RgbaColor PyramidColor = new(255, 215, 120);

    /// <summary>Runs on the sensor cadence: a pyramid close enough to touch grants charges.</summary>
    private void CheckPyramids()
    {
        for (int i = 0; i < AncientsRule.PyramidCount; i++)
        {
            if (SimTime - _ancientLastGrant[i] < AncientsRule.GrantCooldownSeconds
                || !AncientsRule.InGrantRange(i, _ship.Position, SimTime))
            {
                continue;
            }

            _ancientLastGrant[i] = SimTime;
            _ancientCharges += AncientsRule.ChargesPerVisit;
            ShowPulseMessage($"◬ The pyramid regards you. {AncientsRule.ChargesPerVisit} plottings are granted.");
            RendererInterop.PlayCue("board");
            StateHasChanged();
        }
    }

    /// <summary>Spends a charge: the ancient pilot replaces the maneuver plan with a course
    /// to the current destination — the same Simulator-evaluated search that plans NPC
    /// routes, offered as scarce alien assistance. Manual flight stays the taught skill.</summary>
    private void UseAncientsPilot()
    {
        if (_ancientCharges <= 0 || _destinationBodyId is null || _ephemeris is null)
        {
            return;
        }

        ShowPulseMessage("◬ The ancient pilot considers the sky…");
        if (AncientsRule.AutoPlot(_ephemeris, _ship, _destinationBodyId) is not { } result)
        {
            return;
        }

        _ancientCharges--;
        _planNodes.Clear();
        foreach (ManeuverNode node in result.Plan.Nodes)
        {
            _planNodes.Add(new PlanNode { SimTime = node.SimTime, Action = node.Action, Pulses = node.Pulses });
        }

        RebuildPlan();
        ReprojectTrajectory();
        ShowPulseMessage($"◬ Course laid — closest approach {FormatDistance(result.MissDistance)} at {FormatSimTime(result.ClosestApproachSimTime)}. Mind HOW it flies.");
        StateHasChanged();
    }
}
