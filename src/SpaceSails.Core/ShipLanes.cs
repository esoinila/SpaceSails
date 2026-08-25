namespace SpaceSails.Core;

/// <summary>
/// #953 · THE SHIP LANES ARE ARCHIVED — one flag, so a future redesign has one thing to flip.
///
/// <para><b>The ruling.</b> Owner, 2026-08-25, on the trade-lane overlay: <i>"we have never used them to find
/// anything."</i> It follows a first pass (#971) that only turned the display OFF by default, after he opened
/// the sensors desk onto a sky <i>"covered in faint lines with no intersection"</i> — <i>"It must always be
/// much more filtered and off by default. This is just ugly here by default."</i> A checkbox nobody ticks is
/// not a feature; it is a row in a panel and a branch in the paint loop. So the DISPLAY is gone: the
/// <c>routes.lanes</c> layer row, the corridor quads and their name labels, and the last remnants of the
/// per-lane click target #971 had already unhooked.</para>
///
/// <para><b>What "archived" means here, exactly.</b> Not deleted, and not commented out (a commented-out
/// block is neither running code nor a decision). The lane GEOMETRY — <see cref="TradeCorridors"/>,
/// <see cref="CorridorRegion"/>, <see cref="ScanPrograms"/> — stays compiled and stays tested, because it is
/// dormant only as far as DRAWING goes. It is still live gameplay data underneath the telescope: the
/// open-sky menu's two sweep actions (<c>📡 Sweep the … lane</c> / <c>🔁 Standing watch</c>) name a lane, aim
/// the scope over it with <see cref="TradeCorridors.SweepJobFor"/>, and the completed pass runs a real
/// detection sweep that puts real contacts in the tracking ledger. Those found ships are the point of the
/// sensors desk, so the actions and the minimal data behind them are KEPT — see
/// <c>TheShipLanesAreArchivedTests</c>, which pins that verdict with its reason.</para>
///
/// <para><b>Flipping it back.</b> Set this to <c>false</c> and the <c>routes.lanes</c> layer row returns to
/// the Layers panel (<see cref="MapLayerTree"/> is the one consumer). What does NOT come back on its own is
/// the corridor painting itself — that client code was deleted rather than left dozing behind an
/// <c>if</c>, which is the whole point of an archive: the decision is recorded here, the dead pixels are
/// not carried around. A redesign that wants lanes on the sky again writes the draw it actually wants, and
/// finds the geometry waiting for it.</para>
/// </summary>
public static class ShipLanes
{
    /// <summary>True while the owner's #953 ruling stands: the lanes are not drawn and not offered.</summary>
    public const bool Archived = true;
}
