using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE THREAT LINE AND THE STORYBOARD (#394) — the rock's rail drawn on the nav map, and the
/// aftermath panels.
///
/// <para>Split out of <c>Map.Deflection.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. The threat line's banner, its segment count and its three <c>static
/// readonly</c> colours stay in the opening file in their original order (#1163).</para>
/// </summary>
public partial class Map
{
    private void DrawAsteroidThreat()
    {
        // 🗺 Layers (#405) — SAFETY INVARIANT: the inbound rock / collision warning is PINNED. It
        // deliberately consults NO LayerVisible gate (Threats is a pinned family, and even so the
        // rock's leaf resolves always-visible in Core). A hidden layer must never be able to swallow
        // the one thing that can end the run — do not add a layer check here.
        if (_deflection is not { } plan || _ephemeris is null || _renderer is null)
        {
            return;
        }
        if (_ephemeris.Bodies.All(b => b.Id != plan.ParentBodyId))
        {
            return;
        }

        RgbaColor color = _deflectionResolved switch
        {
            DeflectionOutcome.FullDeflection => ClearGreen,
            DeflectionOutcome.GrazingMiss => GrazeAmber,
            DeflectionOutcome.Impact => ThreatRed,
            _ => ThreatRed, // unresolved — inbound
        };

        Vector2d parent = _ephemeris.Position(plan.ParentBodyId, SimTime);
        DeflectionGig.RockRail rail = DeflectionGig.RaisePeriapsis(plan.BaseRail, _deflectionRaiseMeters);

        // The rail ellipse (perifocal → rotate by ω → parent-relative → world), swept in eccentric anomaly.
        Span<float> ring = stackalloc float[(DeflectionRailSegments + 1) * 2];
        double a = rail.SemiMajorAxis, e = rail.Eccentricity;
        double semiMinor = a * Math.Sqrt(1.0 - e * e);
        double cosW = Math.Cos(rail.ArgPeriapsis), sinW = Math.Sin(rail.ArgPeriapsis);
        for (int i = 0; i <= DeflectionRailSegments; i++)
        {
            double t = Math.Tau * i / DeflectionRailSegments;
            double px = a * (Math.Cos(t) - e);
            double py = semiMinor * Math.Sin(t);
            Vector2d world = parent + new Vector2d(cosW * px - sinW * py, sinW * px + cosW * py);
            (float sx, float sy) = _camera.WorldToScreen(world);
            ring[i * 2] = sx;
            ring[i * 2 + 1] = sy;
        }
        _renderer.DrawPolyline(ring, color, 2f);

        // The intersect ⚠ — the rail's periapsis point (on the station's orbit before deflection; lifted off
        // after). Drawn where the collision WOULD be.
        Vector2d impactWorld = parent + new Vector2d(
            rail.PeriapsisMeters * cosW, rail.PeriapsisMeters * sinW);
        (float ix, float iy) = _camera.WorldToScreen(impactWorld);

        // The threat line: from the rock (its live position on the map) to the intersect point.
        if (!_deflectionResolved.HasValue || _deflectionResolved == DeflectionOutcome.Impact)
        {
            Vector2d rockWorld = _ephemeris.Bodies.Any(b => b.Id == plan.RockBodyId)
                ? _ephemeris.Position(plan.RockBodyId, SimTime)
                : parent + DeflectionGig.RockPosition(rail, SimTime);
            (float rx, float ry) = _camera.WorldToScreen(rockWorld);
            Span<float> line = [rx, ry, ix, iy];
            _renderer.DrawPolyline(line, color, 1.6f);
        }

        // The marker glyph + label.
        (string glyph, string text) = _deflectionResolved switch
        {
            DeflectionOutcome.FullDeflection => ("✔", $"{plan.TargetName} — CLEAR"),
            DeflectionOutcome.GrazingMiss => ("➰", $"{plan.TargetName} — GRAZED"),
            DeflectionOutcome.Impact => ("💥", $"{plan.TargetName} — STRUCK"),
            _ => ("⚠", $"IMPACT — {plan.TargetName}"),
        };
        _renderer.DrawCircle(ix, iy, 5f, color, color);
        // #402: the threat rock's ⚠/name is the deflection money-moment — it sits at the top of the
        // label priority ladder so FlushNavLabels never lets a depot's name smear over it.
        EnqueueNavLabel(ix + 8, iy - 6, $"{glyph} {text}", color, LabelPriorityThreatRock);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE STORYBOARD — the aftermath, told in staged panels with delivered art (the BUSTED staged idiom):
    //  a full deflection is the 4-panel money shot; a graze and an impact each get 2. Per-panel gradient
    //  fallback, house-voice captions.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    public readonly record struct DeflectionStoryPanel(string ArtFile, int Hue, string Caption);
    private List<DeflectionStoryPanel>? _deflectionStory;
    private int _deflectionStoryIndex;

    private void ShowDeflectionStory(DeflectionOutcome outcome)
    {
        _deflectionStory = outcome switch
        {
            DeflectionOutcome.FullDeflection =>
            [
                new("deflect-success-0.jpg", 210, "Hold the light, Ringside — this one's ours. And she sets her thumb on the plunger."),
                new("deflect-success-1.jpg", 30, "The charge goes down the bore, all of it. The rock doesn't know yet."),
                new("deflect-success-2.jpg", 45, "Then the mountain comes apart — a white flash and a million glittering stones, and not one of them is aimed at the Exchange anymore."),
                new("deflect-success-3.jpg", 190, $"Back at Ringside they're still trading. The crew of {DeflectionShipName} drinks for free tonight — and every night the rings keep turning."),
            ],
            DeflectionOutcome.GrazingMiss =>
            [
                new("deflect-partial-1.jpg", 35, "The charge bites, but the rock is stubborn — it heels over, groaning, and slides past the Exchange close enough to scratch the paint."),
                new("deflect-partial-2.jpg", 15, "Ringside is bleeding — a dock gone, a deck open to the black — but she is standing. Half the fee, and honest about why."),
            ],
            _ =>
            [
                new("deflect-impact-1.jpg", 8, "No burn in time. The rock keeps its appointment. Ringside fills the sky and there is nothing left to do but hold on."),
                new("deflect-impact-2.jpg", 12, "It hits. The trade decks are wreckage and the berths are a ruin — but the Exchange held, and crews are already clearing the dark. She'll trade again. She always does."),
            ],
        };
        _deflectionStoryIndex = 0;
    }

    private void AdvanceDeflectionStory()
    {
        if (_deflectionStory is null)
        {
            return;
        }
        _deflectionStoryIndex++;
        if (_deflectionStoryIndex >= _deflectionStory.Count)
        {
            _deflectionStory = null;
        }
    }

    // The panel's hero image over a per-panel gradient, so a missing/404 asset still reads as a tinted card
    // (the ExpeditionBriefArtCss / souvenir onerror-hide idiom).
    private static string DeflectionPanelArtCss(DeflectionStoryPanel panel)
    {
        string gradient = $"radial-gradient(circle at 42% 36%, hsl({panel.Hue}, 55%, 30%), hsl({(panel.Hue + 24) % 360}, 60%, 8%) 74%)";
        return $"url('art/{panel.ArtFile}'), {gradient}";
    }
}
