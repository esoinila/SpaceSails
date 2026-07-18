namespace SpaceSails.Core;

using static SpaceSails.Core.OverlayLayout;

/// <summary>
/// The critical-controls registry for the ship's one non-negotiable lifeline: the rescue affordance a
/// stranded captain presses when the tank runs dry (#293). This is the #253 <c>MenuLayout</c> pattern
/// generalized from "one menu on screen" to "a named control that must stay pressable in every
/// distress state" — the geometry of the ACTUAL Map HUD, encoded as data so <see cref="OverlayLayout"/>
/// can audit it in a unit test, no browser required.
///
/// <para><b>Source-of-truth contract.</b> Every rectangle and z-index below is transcribed from
/// <c>src/SpaceSails.Client/Pages/Map.razor.css</c> at the cited line. Like a lab probe's printed
/// tables, these numbers go stale if the CSS moves — change the CSS, change the number here, and the
/// gate re-verifies. The heights are the one estimate (panels have no fixed height); they are
/// deliberately GENEROUS so the audit errs toward finding overlaps, never toward hiding them.</para>
///
/// <para><b>The reported bug.</b> "The rescue-me button was barely clickable when we ran out of power."
/// Its mechanism is documented in <c>Map.razor</c> lines 1571-1576: the original adrift strip rendered
/// at <c>top:0.75rem</c> as a child of <c>.map-topstack</c> (z-index 24), so the ship's masthead
/// painted over it (#262) — the lifeline buried under the nameplate. See <see cref="BuriedStrip"/> /
/// <see cref="MastheadBand"/> for that layout; the gate catches it.</para>
/// </summary>
public static class RescueLifeline
{
    /// <summary>CSS px per rem (Bootstrap/browser default 16px), so rem anchors map to pixels.</summary>
    public const double Rem = 16;

    /// <summary>The reopen affordance's stacking order BEFORE this lab: <c>.map-adrift { z-index: 30 }</c>
    /// (Map.razor.css:348). Clears the base HUD (topstack 24, HUD 22, desk-layer 15) but is NOT
    /// reserved — nothing stops a later pointer-events overlay in the 31..1320 band painting over it.</summary>
    public const int PreLabZIndex = 30;

    /// <summary>The reserved distress-lifeline band <c>.map-adrift</c> lives in: above every non-rescue
    /// overlay the state machine can raise (desk/deck pop-ups top out at <see cref="OverlayBands.ViewObjectBackdrop"/>),
    /// just below the rescue modal it opens (<see cref="OverlayBands.RescueBackdrop"/>). The lifeline is
    /// the last thing anything may paint over. Sourced from <see cref="OverlayBands"/> (#299), which the
    /// CSS mirrors and <c>CssZBandSyncTests</c> keeps honest — no hand-transcribed z-value here anymore.</summary>
    public const int LifelineZIndex = OverlayBands.DistressLifeline;

    /// <summary>A standard flight viewport (CSS px). Desktop 1280x800 by default; the tests also sweep
    /// narrow/short sizes to prove the affordance never leaves the screen.</summary>
    public static Rect Viewport(double width = 1280, double height = 800) => new(0, 0, width, height);

    /// <summary>
    /// The reopen affordance — <c>.map-adrift-reopen</c>, a pill anchored <c>bottom:4.5rem</c>,
    /// horizontally centred (Map.razor.css:341-362). Shown while <c>Adrift &amp;&amp; !_showRescueOffer</c>
    /// (the captain declined the tow and must be able to call it back). Sized from its padding + label.
    /// </summary>
    public static Overlay ReopenPill(Rect viewport, int zIndex)
    {
        const double w = 15.0 * Rem;   // "🛟 Adrift — request rescue" + 1.1rem side padding.
        const double h = 2.1 * Rem;    // 0.95rem label + 0.45rem*2 padding + border.
        double x = viewport.X + (viewport.W - w) / 2;
        double y = viewport.Bottom - 4.5 * Rem - h;
        return new Overlay(".map-adrift-reopen", new Rect(x, y, w, h), zIndex);
    }

    /// <summary>
    /// Every pointer-events overlay that can be raised at the same time as the reopen pill in the
    /// out-of-power (adrift, undocked) state. The three bottom-centre HUD panels share the pill's
    /// anchor zone; the full-screen desk shield can be up if the captain switched desks. All are below
    /// the lifeline band — which is the whole point, and what the gate verifies.
    /// </summary>
    public static IReadOnlyList<Overlay> OutOfPowerOverlays(Rect viewport)
    {
        // .map-dest-panel — nav-target box, bottom:0.75rem, width:38rem (Map.razor.css). z from the band scale.
        Overlay destPanel = BottomCentre(viewport, ".map-dest-panel", widthRem: 38, heightRem: 9, zIndex: OverlayBands.MapDestPanel);

        // .map-dossier — target dossier, bottom:0.75rem, width:30rem (Map.razor.css). z from the band scale.
        Overlay dossier = BottomCentre(viewport, ".map-dossier", widthRem: 30, heightRem: 7, zIndex: OverlayBands.MapDossier);

        // .map-scope — Nav scope canvas, 280px, bottom/right 0.75rem, no z-index → base map (Map.razor.css).
        const double scope = 280;
        Overlay scopePanel = new(
            ".map-scope",
            new Rect(viewport.Right - 0.75 * Rem - scope, viewport.Bottom - 0.75 * Rem - scope, scope, scope),
            OverlayBands.BaseMap);

        // .desk-layer — full-screen near-opaque desk shield (Map.razor.css). A full-screen gate below the
        // pill; harmless, included so the audit sees the whole stack. z from the band scale.
        Overlay deskLayer = new(".desk-layer", viewport, OverlayBands.DeskLayer);

        return [destPanel, dossier, scopePanel, deskLayer];
    }

    /// <summary>The pre-#262 buried affordance: the old adrift strip at <c>top:0.75rem</c>, a child of
    /// <c>.map-topstack</c> so it could never rise above z-index 24 (Map.razor:1571-1576). Modelled at
    /// the top anchor, below the masthead.</summary>
    public static Overlay BuriedStrip(Rect viewport)
    {
        const double w = 24.0 * Rem;
        const double h = 2.5 * Rem;
        double x = viewport.X + (viewport.W - w) / 2;
        double y = viewport.Y + 0.75 * Rem;
        return new Overlay("legacy-adrift-strip", new Rect(x, y, w, h), ZIndex: 20);
    }

    /// <summary>The ship's masthead / pilot-in-command banner that buried the old strip — top-centre,
    /// z-index 24 as a child of <c>.map-topstack</c> (Map.razor.css:135-146, 707). Higher than the
    /// strip and overlapping it: the #262 occlusion.</summary>
    public static Overlay MastheadBand(Rect viewport)
    {
        const double w = 44.0 * Rem;
        const double h = 4.0 * Rem;
        double x = viewport.X + (viewport.W - w) / 2;
        return new Overlay("masthead/pilot-banner", new Rect(x, viewport.Y, w, h), ZIndex: OverlayBands.MapTopstack);
    }

    /// <summary>A stand-in for ANY pointer-events pop-up authored into the desk/deck overlay band
    /// (z-index up to 1320) that comes to rest over the bottom-centre. Not a state that co-occurs with
    /// adrift today — a FORWARD guard: the lifeline band exists so that if one ever does, the affordance
    /// still wins. Feed this to the audit against a pill at <see cref="PreLabZIndex"/> vs
    /// <see cref="LifelineZIndex"/> to see the band earn its place.</summary>
    public static Overlay DeskBandPopupOverBottom(Rect viewport)
    {
        const double w = 38.0 * Rem;
        const double h = 16.0 * Rem;
        double x = viewport.X + (viewport.W - w) / 2;
        double y = viewport.Bottom - 3.0 * Rem - h; // sinks over the pill's bottom anchor.
        return new Overlay("desk-band-popup (z≤1320)", new Rect(x, y, w, h), ZIndex: OverlayBands.ViewObjectBackdrop);
    }

    private static Overlay BottomCentre(Rect viewport, string name, double widthRem, double heightRem, int zIndex)
    {
        double w = widthRem * Rem;
        double h = heightRem * Rem;
        double x = viewport.X + (viewport.W - w) / 2;
        double y = viewport.Bottom - 0.75 * Rem - h;
        return new Overlay(name, new Rect(x, y, w, h), zIndex);
    }
}
