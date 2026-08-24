namespace SpaceSails.Client.Components;

/// <summary>
/// #997 · HOW A SURFACE IS ALLOWED TO END. The owner's ruling of 2026-08-24 — <i>"there should not be a
/// pop-up that cannot be closed or minimized"</i> — has exactly three shapes in this client, and #992's
/// audit of all 89 surfaces found no fourth.
/// </summary>
public enum OverlayDismiss
{
    /// <summary>A ✕ takes it off the screen. The ordinary case: 47 of the 89.</summary>
    Close,

    /// <summary>A – tucks it into a tile in its own corner, and the tile brings it back. The scope (#963)
    /// and the dossier (#960) invented this idiom twice, independently; this is the one mechanism.</summary>
    Minimize,

    /// <summary>The critical-decision exception: no ✕, because every answer it offers is itself a close.
    /// Eleven of the 89. <see cref="OverlayShell"/> audits the claim in DEBUG rather than believing it.
    /// </summary>
    ByDecision,
}

/// <summary>#997 · What the shell draws AROUND the content — the second axis, independent of the first.</summary>
public enum OverlayFrame
{
    /// <summary>A titled card: a head row (title · tools · dismiss) above a body. The scope, the dossier.
    /// </summary>
    Card,

    /// <summary>No head and no body wrapper: the children are the surface's own children and the dismiss is
    /// the LAST of them. #996's story-plate idiom — the plate is one flex row of [art][words][✕], and a
    /// wrapper around the first two would break the row. The <c>.view-object</c> family's sticky foot
    /// (<c>.view-object &gt; .view-object-close</c>) needs the same direct-child relation.</summary>
    Bare,

    /// <summary>#664's <c>Presentation.Hosted</c>: the surface is drawn INSIDE a host card's frame and
    /// brings no positioned box of its own — the host owns the geometry, the shell owns the way out.
    /// </summary>
    Hosted,
}

/// <summary>#997 · Where a minimised shell's tile lands, when the page has no rule of its own for it. The
/// scope and the dossier both name their own tile class and keep their measured corners; this is what a
/// NEW surface gets for free, so that "it minimises" never has to mean "and now find somewhere to put it".
/// </summary>
public enum OverlayCorner
{
    /// <summary>The tile's own class owns the geometry — the shell adds none.</summary>
    None,

    BottomRight,

    BottomLeft,

    BottomCentre,

    TopRight,
}
