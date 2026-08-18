using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #715 · <b>THE CLIENT HALF OF THE ILLEGAL-HEAT METER: one bank, one cooler, one line.</b>
///
/// <para>#929/#931 left four things in the game PUBLISHING a heat charge and nothing banking it — the lift
/// panel's refused read, #760's refused send, #763's refused press, and the round. This is where all four
/// arrive, and they arrive through <see cref="IllegalHeat.Bank"/> and through nothing else. A second banking
/// call anywhere would be the one bug this feature's fourth guard is written to catch: a crossing counted
/// twice, or a source quietly dropped.</para>
///
/// <h3>No field was invented</h3>
///
/// <para>The meter lives in <c>_contacts</c> — the book #770 already calls "#715's per-entity ledger" — so it
/// round-trips through the vault for free and <b>#905's frame ledger stays pinned</b>: no new field on this
/// component, none on <c>SurfaceExcursion</c>, and not one of the thirty committed fingerprints moves. The
/// patrol reads and banks through a ledger handed IN (see <c>Map.Patrol.cs</c>), so <c>IPatrolHost</c> keeps
/// its twenty-one members.</para>
/// </summary>
public partial class Map
{
    /// <summary>
    /// #715 · <b>WHOSE GROUND IS UNDER THE CAPTAIN'S FEET, or nobody's.</b>
    ///
    /// <para>The one question the cooler asks, and the reason it is a question rather than a field: a captain
    /// is on an outfit's ground exactly when they are on a surface excursion at a site that outfit runs, and
    /// that is already known from the excursion. In the sky, in a haven or in a hull, the answer is null and
    /// everybody with a memory of you is cooling.</para>
    /// </summary>
    private string? TheOutfitUnderfoot =>
        _surface is { } ex ? SiteOperator.Of(ex.Stop.Body.Id).Id : null;

    /// <summary>#715 · What the outfit that runs the ground underfoot remembers about this captain. Zero
    /// aboard, zero at a haven, and zero at every site of every outfit that has never been crossed.</summary>
    private int HeatHere => _surface is { } ex ? IllegalHeat.HeatAtSite(_contacts, ex.Stop.Body.Id) : 0;

    /// <summary>
    /// #715 · <b>BANK A PUBLISHED CHARGE.</b> The single seam between everything that can cross an outfit and
    /// the book that remembers it. A charge owed to nobody is a no-op, so the four callers bank
    /// unconditionally and none of them has to decide whether what just happened counted.
    /// </summary>
    private void BankTheCrossing(UndergroundComplex.HeatCharge charge)
    {
        if (charge.Points <= 0)
        {
            return;
        }
        IllegalHeat.Bank(_contacts, charge, SimTime);
        RequestVaultSave();
    }

    /// <summary>
    /// #715 · <b>THE LINE, AND IT IS THE WHOLE PRESENTATION.</b> Shown only where the outfit that runs this
    /// ground has a memory of the captain — never on the ship, never at a haven, and never at a site of any
    /// other outfit however hot the captain is elsewhere. It names nobody, for the reason written on
    /// <see cref="IllegalHeat.TheyRememberYouHere"/>: the company is the thing the player is meant to infer.
    /// </summary>
    private bool TheyRememberYouHere =>
        _surface is { } ex && IllegalHeat.TheyRememberYouAt(_contacts, ex.Stop.Body.Id);
}
