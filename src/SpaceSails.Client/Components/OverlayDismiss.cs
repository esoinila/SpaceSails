namespace SpaceSails.Client.Components;

/// <summary>
/// #997 · HOW A SURFACE IS ALLOWED TO END. The owner's ruling of 2026-08-24 — <i>"there should not be a
/// pop-up that cannot be closed or minimized"</i> — has exactly three shapes in this client, and #992's
/// audit of every positioned surface in it found no fourth.
///
/// <para><b>HOW MANY SURFACES TAKE EACH SHAPE IS NOT WRITTEN DOWN HERE ANY MORE, AND THAT IS THE REPAIR
/// (#1170).</b> This docblock used to carry #992's own tally — so many of the surfaces close, so many decide
/// — from the afternoon in August the audit was made. A fortnight later it was wrong: #1169's stale-fact sweep
/// could prove the split had moved and could not say what it had moved to, because an audit is not a grep
/// and re-running one is a lane of its own. It was re-run, and what came back is the reason there is no
/// number in its place. On the day #992 counted, one shape was declared by a boolean here and a hand-rolled
/// ✕ there; today it is declared once, on <see cref="OverlayShell"/>, by every surface that has one — so the
/// split is not a fact about a comment, it is a fact about the tree, and the tree can be asked.</para>
///
/// <para><b>What asks it is <c>EveryPopUpCanBeDismissedTests</c></b>, the law #992 shipped instead of a
/// sweep. It derives the surfaces from the source and from the render tree on every run, raises each one it
/// can reach and PRESSES its controls to see which of them is a way out — and its fourth guard holds the
/// recogniser itself honest, by requiring every file that draws a shell to wear a root the law can see. A
/// count in a comment is right on the afternoon it is typed. A law is right on the morning somebody adds the
/// next surface, which is the morning this matters.</para>
/// </summary>
public enum OverlayDismiss
{
    /// <summary>A ✕ takes it off the screen. The ordinary case, by a distance — and by how far is the law's
    /// question, not this line's.</summary>
    Close,

    /// <summary>A – tucks it into a tile in its own corner, and the tile brings it back. The scope (#963)
    /// and the dossier (#960) invented this idiom twice, independently; this is the one mechanism.</summary>
    Minimize,

    /// <summary>The critical-decision exception: no ✕, because every answer it offers is itself a close.
    /// <see cref="OverlayShell"/> audits the claim in DEBUG rather than believing it, and the law proves it
    /// the only honest way — by pressing every answer and watching which of them ends the surface.</summary>
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
