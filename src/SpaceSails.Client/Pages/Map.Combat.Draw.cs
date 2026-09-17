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
/// #251 · WHAT THE HUNT LOOKS LIKE ON THE MAP — the fake beacon's ghost (M29), #962's two labelled
/// rings, the target reticle and the brackets it is drawn from.
///
/// <para>Split out of <c>Map.Combat.cs</c> under #251 with no member renamed, re-scoped or re-ordered.
/// <b>The three <c>static readonly RgbaColor</c>s these passes paint with stayed in the opening
/// file</b> — a static field initializer of a partial class runs in the order the compiler reads the
/// FILES, so a colour that travels with its only reader is a colour that can be initialised late, and
/// nothing about the build would say so.</para>
/// </summary>
public partial class Map
{
    // M29: the fake beacon's ghost, shown to US so the captain always knows what story the
    // beacon is telling — a hollow marker flying the abandoned course.
    private void DrawBeaconGhost()
    {
        if (_transponderMode != TransponderMode.Fake || _beaconGhost is not { } ghost
            || !LayerVisible("traffic.beacons")) // 🗺 Layers (#405): Traffic → Beacons
        {
            return;
        }

        (float gx, float gy) = _camera.WorldToScreen(ghost.Position);
        if (gx < -40 || gy < -40 || gx > _viewportWidth + 40 || gy > _viewportHeight + 40)
        {
            return;
        }

        _renderer!.DrawCircle(gx, gy, 6, null, GhostShipColor, 1.5f);
        _renderer.DrawText(gx + 9, gy + 3, "🎭 beacon ghost", GhostShipColor, "10px monospace", TextAlign.Left);
    }

    /// <summary>
    /// #962 · TWO CIRCLES, BOTH LABELLED, SO THE CAPTAIN KNOWS WHEN TO REACT. Owner: <i>"The debt collector
    /// catch distance / speed distance is not shown on the maps in any way. We don't know how much we should
    /// react and when visually now."</i>
    ///
    /// <para>Her CATCH ENVELOPE rides with her; our DRIVER REACH rides with us; and after #961's ruling the
    /// second is always the larger of the two, which is the fact the picture has to make obvious — the moment
    /// she crosses our ring we can answer, and she cannot lay a hand on us until she crosses hers. The catch
    /// ring was already drawn, unlabelled, which the war room's own Gemini playtest had already flagged once
    /// on a different circle: <i>"unlabeled ring — is it weapon range?"</i></para>
    ///
    /// <para>Our reach ring is drawn only while a collector is actually on the map — a permanent circle round
    /// the ship would be furniture, and this is meant to be read.</para>
    /// </summary>
    private void DrawHunters()
    {
        bool anyOnTheMap = false;
        foreach (HunterState hunter in _hunters)
        {
            (float sx, float sy) = _camera.WorldToScreen(hunter.State.Position);
            _renderer!.DrawCircle(sx, sy, 5f, HunterColor, HunterColor);
            _renderer!.DrawText(sx + 8, sy - 6, $"🐺 {hunter.Callsign}", HunterColor);

            double distance = (hunter.State.Position - _ship.Position).Length;
            if (distance > EncounterRule.WeaponRangeMeters * 3)
            {
                continue;
            }

            anyOnTheMap = true;
            float catchPx = (float)Math.Clamp(EncounterRule.CatchRadiusMeters / _camera.MetersPerPixel, 4, 200);
            _renderer!.DrawCircle(sx, sy, catchPx, null, HunterColor, 1.5f);
            if (catchPx > 22)
            {
                _renderer!.DrawText(sx, sy - catchPx - 4, $"catch {FormatDistance(EncounterRule.CatchRadiusMeters)}",
                    HunterColor, "10px monospace", TextAlign.Center);
            }
        }

        if (!anyOnTheMap)
        {
            return;
        }

        (float px, float py) = _camera.WorldToScreen(_ship.Position);
        float reachPx = (float)Math.Clamp(EncounterRule.WeaponRangeMeters / _camera.MetersPerPixel, 4, 600);
        _renderer!.DrawCircle(px, py, reachPx, null, DriverReachColor, 1.2f);
        if (reachPx > 26)
        {
            _renderer!.DrawText(px, py - reachPx - 4, $"driver reach {FormatDistance(EncounterRule.WeaponRangeMeters)}",
                DriverReachColor, "10px monospace", TextAlign.Center);
        }
    }

    /// <summary>
    /// #962 · THE TARGET IS A PLACE ON THE SKY, NOT A BOX IN THE CORNER. Owner, with the Debt Collector's
    /// dossier open: <i>"There is no visual indicator that we have targeted the debt collector in any way.
    /// Just the disconnected box. There should be a marker on our tracked targets also in nav map... red X
    /// aim reticle maybe?"</i>
    ///
    /// <para>Two marks, because there are two different claims to make. The RED X is the tactical target —
    /// the one contact the dossier is open on, the one fire control is solving for — and it is deliberately
    /// the loudest thing on the map. The small green brackets are everything the telescope is holding
    /// custody of: the Sensors desk kept that list as names in a ledger, with nothing on the sky to say
    /// where any of those names actually is.</para>
    ///
    /// <para>Both read positions through <see cref="ContactPosition"/>, which walks traffic AND hired
    /// muscle — the reticle must not repeat the bug that made 📡 sharpen fix a dead button, where a
    /// hunter's id resolved to nothing because the lookup only knew about <c>_npcStates</c>.</para>
    /// </summary>
    private void DrawTargetReticle()
    {
        if (_renderer is null)
        {
            return;
        }

        if (_trackingPost is not null)
        {
            foreach (TrackedTarget entry in _trackingPost.Entries)
            {
                if (entry.ShipId == TacticalTargetId || ContactPosition(entry.ShipId) is not { } held)
                {
                    continue; // the reticle below says it louder; an id we can't place gets no mark at all
                }

                (float bx, float by) = _camera.WorldToScreen(held);
                DrawBracket(bx, by, 8f, TrackBracketColor);
            }
        }

        if (TacticalTargetId is not { } id || ContactPosition(id) is not { } position)
        {
            return;
        }

        (float sx, float sy) = _camera.WorldToScreen(position);
        // A slow breath, so the reticle reads as LIVE rather than as another painted glyph.
        byte pulse = (byte)(190 + 60 * Math.Sin(_frameNowMs / 320.0));
        RgbaColor ink = ReticleColor with { A = pulse };

        const float inner = 5f, outer = 14f;
        const float diagonal = 0.70710678f; // the X's arms, at 45° off the axes
        ReadOnlySpan<(float X, float Y)> arms = [(1, 1), (1, -1), (-1, 1), (-1, -1)];
        foreach ((float dx, float dy) in arms)
        {
            _renderer.DrawPolyline(
                [sx + dx * diagonal * inner, sy + dy * diagonal * inner,
                 sx + dx * diagonal * outer, sy + dy * diagonal * outer],
                ink, 2f);
        }

        _renderer.DrawCircle(sx, sy, inner + 1.5f, null, ink, 1.2f);
        _renderer.DrawText(sx, sy + outer + 12, $"🎯 {ContactCallsign(id)}", ink, "11px monospace", TextAlign.Center);
    }

    /// <summary>A custody bracket: four corners of a box, drawn open — the telescope is holding this
    /// contact, which is a weaker claim than the reticle's and is drawn as one.</summary>
    private void DrawBracket(float x, float y, float half, RgbaColor color)
    {
        float arm = half * 0.55f;
        ReadOnlySpan<(float X, float Y)> corners = [(-1, -1), (1, -1), (-1, 1), (1, 1)];
        foreach ((float cx, float cy) in corners)
        {
            float px = x + cx * half, py = y + cy * half;
            _renderer!.DrawPolyline([px - cx * arm, py, px, py, px, py - cy * arm], color, 1.2f);
        }
    }
}
