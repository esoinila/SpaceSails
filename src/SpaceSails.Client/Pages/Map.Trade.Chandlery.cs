using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #325 / #332 · THE CHANDLERY AT THE BERTH — the two things a haven sells to the CAPTAIN.
///
/// <para>Owner, #325 item 5: <i>"The kiosk finally sells something load-bearing: extended tanks / spare
/// bottles as purchasable margin ... the tourist shop becomes an outfitter."</i> And #332 item 2: <i>"A
/// supplies line ('medkits') as a purchasable consumable at havens."</i></para>
///
/// <para><b>No new panel, and that is the whole of the UI design.</b> The berth already has a services
/// column — the pump, then the sentry armory — sitting behind one <c>_dockedHavenId is not null</c> gate,
/// and both are a plate, a state line and one button that debits the purse. The chandlery is a third card
/// in that column in exactly that idiom. A shop that needed its own overlay to sell two things would be a
/// pop-up in a game whose general UI law (#1016, 2026-08-24) is that nothing may open that cannot be
/// closed, and the cheapest way to keep that law is not to open anything.</para>
///
/// <para><b>The refusal is the desk's existing one.</b> A row a captain cannot afford goes <c>disabled</c>
/// below its price, the same shape the armory has used since #562 and the dark-web desk uses
/// for its fence rows — not a new sentence, not a modal. The price on the plate is the whole explanation.</para>
///
/// <para>Prices are Core's (<see cref="Chandlery"/>) and are read off THIS haven's bar card, so nothing in
/// this file states a credit figure.</para>
/// </summary>
public partial class Map
{
    // ── #325 · THE SHIP'S SPARE BOTTLES ────────────────────────────────────────────────────────────────
    //
    //  A COUNT, on the ship, not in the satchel — the owner's placement, and the right one: a spare tank is
    //  stores. They stack; fitting one consumes one at the start of the next excursion; they ride the vault
    //  (Vault.Ship.ExtendedTanks) because a bought thing that evaporates over a reload is a purchase the
    //  game took the money for and did not honour.
    //
    //  This field is the ONE place the count lives. The excursion never copies it — it carries a single bool
    //  saying a tank was fitted (SurfaceExcursion.ExtendedTank), because a copy of a count is a second count.
    private int _extendedTanks;

    /// <summary>#325/#332 · The bar at the berth the ship is tied to, or null when she is not tied to one.
    /// The chandlery's whole gate: the rows are absent when docked nowhere, and the prices are this house's
    /// own. It is the same expression the med bay's own barkeep-aware lines already use — a haven with an
    /// interior has a keep, and there are exactly as many keeps as there are havens to walk into.</summary>
    private Barkeep? ChandleryHouse =>
        _dockedHavenId is { } id ? Barkeeps.For(id) : null;

    /// <summary>#325 · What a spare bottle costs at this berth, or 0 when there is no berth.</summary>
    private int ExtendedTankPrice =>
        ChandleryHouse is { } house ? Chandlery.ExtendedTankPrice(house) : 0;

    /// <summary>#332 · What topping the cabinet back up costs at this berth — one glass per pill MISSING,
    /// so a captain who swallowed one pays for one. Zero when the cabinet is full or there is no berth.</summary>
    private int MedKitRefillPrice =>
        ChandleryHouse is { } house ? Chandlery.MedKitRefillPrice(house, Chandlery.MedKitPillsMissing(_pills)) : 0;

    /// <summary>#332 · How many pills a refill would rack. The row's label and the press both read this, so
    /// the number quoted and the number bought can never be two numbers.</summary>
    private int MedKitPillsMissing => Chandlery.MedKitPillsMissing(_pills);

    /// <summary>#325 · How many spare bottles are stowed. For the row's state line.</summary>
    private int ExtendedTanksAboard => _extendedTanks;

    /// <summary>#325 · Buy one extended tank. Debits, increments, prints the berth receipt, saves.</summary>
    private void BuyExtendedTank()
    {
        if (ChandleryHouse is null)
        {
            return; // the row only shows at a berth, but never trust the caller
        }

        int price = ExtendedTankPrice;
        if (_credits < price)
        {
            return; // the desk's existing refusal is the disabled row — no second sentence for the same fact
        }

        _credits -= price;
        _extendedTanks++;
        RendererInterop.PlayCue("board");
        string receipt = Chandlery.TankReceiptLine(price, _extendedTanks);
        LogAutopilotEvent(receipt);
        ShowPulseMessage(receipt);
        RequestVaultSave();
    }

    /// <summary>#332 · Buy the cabinet's refill. THE ONE WRITER of <c>_pills</c> besides the press that
    /// swallows one, which is the shape #332 asks for: one stock count, one reader (the cabinet), one
    /// writer (this).</summary>
    private void BuyMedKitRefill()
    {
        if (ChandleryHouse is null)
        {
            return;
        }

        int pills = MedKitPillsMissing;
        int price = MedKitRefillPrice;
        if (pills <= 0 || _credits < price)
        {
            return;
        }

        _credits -= price;
        _pills += pills;
        RendererInterop.PlayCue("board");
        string receipt = Chandlery.RefillReceiptLine(pills, price);
        LogAutopilotEvent(receipt);
        ShowPulseMessage(receipt);
        RequestVaultSave();
    }

    /// <summary>
    /// #325 · FIT A TANK, IF THERE IS ONE TO FIT — called once, at the shuttle, as an excursion is built.
    ///
    /// <para>Consumed HERE and only here, at the moment the suit goes on: a bottle is fitted before the
    /// boots touch regolith, and there is no walking back to the ship's stores from four thousand du out.
    /// It returns the bit the excursion is built with rather than writing to an excursion that does not
    /// exist yet, so there is exactly one moment at which stores become a fact about a walk.</para>
    ///
    /// <para>It does NOT save the vault. The caller is mid-descent and saves on its own account once the
    /// excursion exists; a save taken between the decrement and the excursion's construction would record a
    /// ship that had spent a tank on nothing.</para>
    /// </summary>
    private bool FitExtendedTankForExcursion()
    {
        if (_extendedTanks <= 0)
        {
            return false;
        }

        _extendedTanks--;
        return true;
    }
}
