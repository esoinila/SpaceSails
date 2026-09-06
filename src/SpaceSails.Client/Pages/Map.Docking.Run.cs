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
/// M14/M29 · THE SHUTTLE RUN, AND THE TWO THINGS DRAWN FOR IT — the range ring that says whether a
/// partner is a deal, the berthed arm between ship and dock, and the little boat crossing to a prize.
///
/// <para>A run is launched at a ship rather than at a place: it flies out, closes, and either boards or
/// comes home empty. The status the HUD reads while it is out is refreshed here too.</para>
///
/// <para>Split out of <c>Map.Docking.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // M29: situational awareness for trading — the shuttle-range ring around the ship,
    // visible whenever the zoom makes it readable. A partner inside the ring is a deal.
    private void DrawShuttleRange()
    {
        float radiusPx = (float)(CommerceRule.ShuttleRangeMeters / _camera.MetersPerPixel);
        if (radiusPx < 16 || radiusPx > 4000)
        {
            return;
        }

        (float sx, float sy) = _camera.WorldToScreen(_ship.Position);
        _renderer!.DrawCircle(sx, sy, radiusPx, null, LocalContactRingColor with { A = 60 }, 1f);
        if (radiusPx > 60)
        {
            _renderer.DrawText(sx, sy - radiusPx - 6, "shuttle range", LocalContactRingColor with { A = 150 },
                "10px monospace", TextAlign.Center);
        }
    }

    // The berth: a steel arm/clamp from the dock to the ship while it's held fast (owner's
    // "tube / arm / clamps keeps the ship connected to the dock"). Drawn under the ship marker.
    private void DrawDockArm()
    {
        if (_dockedHavenId is null || _ephemeris is null)
        {
            return;
        }

        Vector2d dockPos = _ephemeris.Position(_dockedHavenId, SimTime);
        (float dx, float dy) = _camera.WorldToScreen(dockPos);
        (float sx, float sy) = _camera.WorldToScreen(_ship.Position);

        RgbaColor clamp = new(210, 220, 235, 230); // steel
        Span<float> arm = stackalloc float[4];
        arm[0] = dx; arm[1] = dy; arm[2] = sx; arm[3] = sy;
        _renderer!.DrawPolyline(arm, clamp, 2f);
        _renderer.DrawCircle(dx, dy, 5f, null, clamp, 1.5f);          // the clamp collar at the dock
        _renderer.DrawText((dx + sx) / 2, (dy + sy) / 2 - 6, "🔗 berthed",
            clamp with { A = 210 }, "10px sans-serif", TextAlign.Center);
    }

    private void UpdateDockStatus()
    {
        _docked = false;
        _dockBodyId = null;
        foreach (string id in MarketBodies)
        {
            Vector2d pos = _ephemeris!.Position(id, _ship.SimTime);
            if ((_ship.Position - pos).Length <= DockRadiusMeters)
            {
                _docked = true;
                _dockBodyId = id;
                break;
            }
        }

    }

    // ---- M14: the boarding run ----

    private void LaunchShuttleRun(NpcState prey)
    {
        double distance = (prey.State.Position - _ship.Position).Length;
        double relSpeed = (prey.State.Velocity - _ship.Velocity).Length;
        _shuttleTarget = prey;
        _shuttleRun = ShuttleFlightView.Launch(distance, relSpeed, prey.Ship.Callsign, prey.Ship.IsPod);
        _deckKeys.Clear();
        ShowPulseMessage("Shuttle away — you have the stick");
    }

    private void UpdateShuttleRun(double dtRealSeconds)
    {
        ShuttleFlightView.Run run = _shuttleRun!;
        bool windowOpen = _captureEngaged && _shuttleTarget is { Boarded: false, Arrived: false };
        ShuttleFlightView.Update(run, dtRealSeconds,
            _deckKeys.Contains("w"), _deckKeys.Contains("s"),
            _deckKeys.Contains("a"), _deckKeys.Contains("d"),
            windowOpen);

        switch (run.State)
        {
            case ShuttleFlightView.RunState.Docked when run.StateTime > 1.6:
                if (_shuttleTarget is { } prey && !prey.Boarded)
                {
                    Board(prey); // instant: the pilot earned it
                }
                EndShuttleRun(boarded: true, null);
                break;
            case ShuttleFlightView.RunState.WindowLost when run.StateTime > 1.6:
                EndShuttleRun(boarded: false, "Window lost — shuttle recovered");
                break;
        }
    }

    private void EndShuttleRun(bool boarded, string? message)
    {
        _shuttleRun = null;
        _shuttleTarget = null;
        _deckKeys.Clear();
        if (message is not null)
        {
            ShowPulseMessage(message);
        }
    }
}
