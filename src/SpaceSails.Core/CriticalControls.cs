namespace SpaceSails.Core;

using static SpaceSails.Core.OverlayLayout;

/// <summary>
/// The critical-controls registry (#299), growing Lab 34's single-lifeline gate (#293) into the roster of
/// affordances that must stay pressable — the #212 "affordances never hide" family. Each control is a real
/// Map HUD button modelled as a hit-rect whose <b>z-index comes from <see cref="OverlayBands"/></b>, not a
/// hand-copied number, so the CSS-sync test (<c>CssZBandSyncTests</c>) keeps every band honest and the
/// reachability law (<see cref="OverlayLayout"/>) proves each control survives on-screen and un-occluded at
/// every viewport size — from 1280×800 down to 320×480 — with no browser required.
///
/// <para>The heights (and, for the toolbar/menu controls, the anchors) are honest estimates in the same
/// spirit as Lab 34's registry: kept GENEROUS so the audit errs toward <em>finding</em> an overlap, never
/// toward hiding one. The load-bearing fact under test is the band ordering: a critical control out-ranks
/// every pointer-events overlay that can share its screen. Change the CSS band and the sync test fails;
/// change the ordering and this gate fails.</para>
/// </summary>
public static class CriticalControls
{
    /// <summary>CSS px per rem — the browser default, shared with <see cref="RescueLifeline.Rem"/>.</summary>
    public const double Rem = RescueLifeline.Rem;

    /// <summary>One entry in the roster: a named control, its family, and — given a viewport — the control
    /// itself plus every pointer-events overlay that can be raised at the same time.</summary>
    public sealed record Control(
        string Name,
        string Family,
        Func<Rect, Overlay> Build,
        Func<Rect, IReadOnlyList<Overlay>> CoRaised);

    /// <summary>The distress / safety family — the minimum bar: a captain must always be able to call for
    /// rescue, read and answer the tow offer, and fire the de-escalating warning shot.</summary>
    public const string SafetyFamily = "distress/safety";

    /// <summary>The #212 always-pressable affordances: docking, autopilot release, the long-haul engage,
    /// and the context menu that opens them.</summary>
    public const string AffordanceFamily = "affordance";

    /// <summary>The full roster the gate sweeps. Every control's z-index is an <see cref="OverlayBands"/>
    /// constant; every gate is the same reachability law at every viewport.</summary>
    public static IReadOnlyList<Control> Roster { get; } =
    [
        // --- distress / safety family (the minimum bar) ------------------------------------------
        new("rescue-reopen-pill", SafetyFamily,
            vp => RescueLifeline.ReopenPill(vp, OverlayBands.MapAdrift),
            vp => RescueLifeline.OutOfPowerOverlays(vp)),

        new("rescue-offer-accept", SafetyFamily,
            vp => ModalDialogButton(vp, ".rescue-offer-accept", OverlayBands.RescueBackdrop),
            // The tow-offer modal is up; the routine bottom chrome sits below it. The celebration/busted
            // modals do not co-occur with a live rescue offer, so nothing higher can bury the button.
            vp => RescueLifeline.OutOfPowerOverlays(vp)),

        new("warning-shot", SafetyFamily,
            vp => RaisedCardButton(vp, ".hunter-hail-warning-shot", OverlayBands.DeckOfferCard),
            // The hail card is a raised deck card; only the routine chrome sits under it.
            vp => RescueLifeline.OutOfPowerOverlays(vp)),

        // --- #212 always-pressable affordances ---------------------------------------------------
        new("dock-clamp-release", AffordanceFamily,
            vp => TopToolbarButton(vp, ".map-hud-dock", OverlayBands.MapHud),
            // The bottom-centre nav panels can be up in flight; they do not reach the top toolbar.
            vp => BottomChrome(vp)),

        new("autopilot-disengage", AffordanceFamily,
            vp => DossierButton(vp, ".map-dossier-disengage", OverlayBands.MapDossier),
            // The dossier's disengage row sits above the nav-target box it overlaps (dossier z > panel z).
            vp => BottomChrome(vp)),

        new("longhaul-engage", AffordanceFamily,
            vp => ContextMenuRow(vp, ".map-body-menu-longhaul", OverlayBands.MapBodyMenu, rowFromTop: 2),
            // The #253 case: opened from a bottom-right click, the menu flips fully on-screen.
            vp => BottomChrome(vp)),

        new("context-menu-first-action", AffordanceFamily,
            vp => ContextMenuRow(vp, ".map-body-menu-first", OverlayBands.MapBodyMenu, rowFromTop: 0),
            vp => BottomChrome(vp)),
    ];

    // ---- anchor helpers: honest estimates pinned to the real CSS positions ------------------------

    /// <summary>The routine bottom-centre chrome that can be raised over the map in flight — the
    /// nav-target box and the target dossier (both below the pill/menu bands they share the screen with).</summary>
    private static IReadOnlyList<Overlay> BottomChrome(Rect vp)
    {
        Overlay destPanel = BottomCentre(vp, ".map-dest-panel", widthRem: 38, heightRem: 9, z: OverlayBands.MapDestPanel);
        Overlay dossier = BottomCentre(vp, ".map-dossier", widthRem: 30, heightRem: 7, z: OverlayBands.MapDossier);
        return [destPanel, dossier];
    }

    /// <summary>A centred button inside a full-screen modal backdrop (the rescue offer's Accept). The
    /// backdrop owns the screen; the button sits mid-canvas.</summary>
    private static Overlay ModalDialogButton(Rect vp, string name, int z)
    {
        const double w = 14.0 * Rem;
        const double h = 2.6 * Rem;
        double x = vp.X + (vp.W - w) / 2;
        double y = vp.Y + (vp.H - h) / 2;
        return new Overlay(name, Clamp(new Rect(x, y, w, h), vp), z);
    }

    /// <summary>A button inside a centred, raised card (a hunter-hail / deck card) — mid-canvas, sized
    /// from a label plus padding.</summary>
    private static Overlay RaisedCardButton(Rect vp, string name, int z)
    {
        const double w = 13.0 * Rem;
        const double h = 2.4 * Rem;
        double x = vp.X + (vp.W - w) / 2;
        double y = vp.Y + (vp.H - h) / 2 + 3.0 * Rem; // action row sits below the card's message.
        return new Overlay(name, Clamp(new Rect(x, y, w, h), vp), z);
    }

    /// <summary>A button in the top Nav toolbar (<c>.map-hud</c>) — anchored top-left, below the masthead
    /// stack, wide enough for the "⚓ Match &amp; clamp — ≈N p" label.</summary>
    private static Overlay TopToolbarButton(Rect vp, string name, int z)
    {
        const double w = 13.0 * Rem;
        const double h = 2.0 * Rem;
        double x = vp.X + 1.0 * Rem;
        double y = vp.Y + 4.5 * Rem; // clears the masthead / tabs stack above it.
        return new Overlay(name, Clamp(new Rect(x, y, w, h), vp), z);
    }

    /// <summary>The disengage/disarm row inside the bottom-centre dossier panel (<c>.map-dossier</c>).</summary>
    private static Overlay DossierButton(Rect vp, string name, int z)
    {
        const double w = 12.0 * Rem;
        const double h = 1.9 * Rem;
        double x = vp.X + (vp.W - w) / 2;
        double y = vp.Bottom - 0.75 * Rem - 2.2 * Rem; // in the dossier's action row, above its bottom edge.
        return new Overlay(name, Clamp(new Rect(x, y, w, h), vp), z);
    }

    /// <summary>A row of the planet/ship context menu (<c>.map-body-menu</c>), placed by the same
    /// <see cref="MenuLayout"/> clamp the razor uses (#253) from a hostile bottom-right click, so the whole
    /// menu — first row and the long-haul row alike — lands on screen at every size.</summary>
    private static Overlay ContextMenuRow(Rect vp, string name, int z, int rowFromTop)
    {
        const double menuW = 16.0 * Rem;   // .map-body-menu max-width.
        const double rowH = 2.2 * Rem;     // one menu row (label + padding), estimated generous.
        const int rows = 3;                // set destination · (…) · 🚀 long haul.
        double menuH = rows * rowH;

        // The #253 playtest: a click at the bottom-right corner. MenuLayout flips the box up-and-left so
        // it never spills past the viewport — exactly the guarantee under test.
        double clickX = vp.Right - 1.0 * Rem;
        double clickY = vp.Bottom - 1.0 * Rem;
        (double mx, double my) = MenuLayout.ClampMenuPosition(clickX, clickY, menuW, menuH, vp.W, vp.H);

        double rowY = my + rowFromTop * rowH;
        return new Overlay(name, new Rect(mx, rowY, menuW, rowH), z);
    }

    private static Overlay BottomCentre(Rect vp, string name, double widthRem, double heightRem, int z)
    {
        double w = widthRem * Rem;
        double h = heightRem * Rem;
        double x = vp.X + (vp.W - w) / 2;
        double y = vp.Bottom - 0.75 * Rem - h;
        return new Overlay(name, new Rect(x, y, w, h), z);
    }

    /// <summary>Keep a modelled hit-rect inside the viewport (real panels use <c>max-width</c> /
    /// <c>translateX(-50%)</c> to the same effect), so the on-screen check tests band ordering rather than
    /// a fixed rect running off a narrow canvas.</summary>
    private static Rect Clamp(Rect r, Rect vp)
    {
        double w = Math.Min(r.W, vp.W - 2 * OverlayLayout.DefaultMarginPx);
        double h = Math.Min(r.H, vp.H - 2 * OverlayLayout.DefaultMarginPx);
        double x = Math.Clamp(r.X, vp.X + OverlayLayout.DefaultMarginPx, vp.Right - OverlayLayout.DefaultMarginPx - w);
        double y = Math.Clamp(r.Y, vp.Y + OverlayLayout.DefaultMarginPx, vp.Bottom - OverlayLayout.DefaultMarginPx - h);
        return new Rect(x, y, w, h);
    }
}
