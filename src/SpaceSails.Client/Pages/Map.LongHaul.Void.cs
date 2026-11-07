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
/// #255 · THE VOID ITSELF — the arrival-epoch re-seed and the crossing cinematic the captain watches
/// while it happens.
///
/// <para>The re-seed is the delicate half: the depots are pure rails and are kept, every transient actor
/// belonged to the world we departed and is dropped, and ambient traffic repopulates AT the arrival epoch
/// through the tested refill wave rather than being planned on the dramatic beat.</para>
///
/// <para>Split out of <c>Map.LongHaul.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // #255 — re-seed the world at the arrival epoch WITHOUT integrating the void or freezing the tab. The
    // depots are pure rails (StepNpcs recomputes each from the new sim time — correct at any epoch, zero
    // cost), so they are kept as-is. Every transient stateful actor — mid-flight strangers, pods, a decoy
    // ghost, rounds in flight — belonged to the world we departed a decade ago and is dropped (a decade past
    // its last plotted node it has long arrived/expired anyway; see the DecadeJump_Retires… test). Fresh
    // ambient traffic repopulates AT the arrival epoch through the existing, tested RefillTraffic wave path
    // over the following sim-hour — deliberately NOT run here, because NPC route-planning is far too heavy to
    // do at the (frozen) engage moment on the interpreted-WASM build (measured: ~20-40 s per hauler). This is
    // the owner's architecture — reuse the tested spawn mechanism, physics preserved — with the planning cost
    // kept off the dramatic beat. Transient world reset is acceptable (the hunter-refusal law already bars
    // jumping out of a live chase).
    private void ReseedWorldForJump(double epoch)
    {
        _npcStates = _npcStates.Where(n => n.Ship.DepotBodyId is not null).ToArray();
        _lastRefillCheckSimTime = epoch; // let RefillTraffic repopulate movers over the next sim-hour of play
        _ordnance.Clear();               // rounds in flight belonged to the world we left
        _beaconGhost = null;             // any decoy ghost is a decade stale
        _pursuitTrail.Clear();
    }

    // The crossing cinematic: tick the year counter 1 → total over ~2.2 s of theater, then hold the full bar
    // a beat before the void lets out. Pure UI — the tab paints every frame (no heavy work runs during it).
    private async Task RunVoidCinematic()
    {
        // R1-F44 · a crossing counted in days ticks at most 30 times, so a 300-day hop is not half a minute.
        int steps = _jumpInDays ? Math.Min(_jumpTotalYears, 30) : _jumpTotalYears;
        int perTickMs = Math.Clamp(2200 / Math.Max(1, steps), 90, 400);
        for (int step = 1; step <= steps; step++)
        {
            _jumpYear = (int)Math.Ceiling((double)_jumpTotalYears * step / steps);
            StateHasChanged();
            await Task.Delay(perTickMs);
        }

        await Task.Delay(260);
    }
}
