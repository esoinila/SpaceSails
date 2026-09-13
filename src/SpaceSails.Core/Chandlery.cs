using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #325 / #332 · THE HAVEN CHANDLERY — the two things a berth sells to the CAPTAIN rather than to the
/// ship, and the one place either of them is priced.
///
/// <para><b>Why it exists at all.</b> Owner, #325 item 5: <i>"The kiosk finally sells something
/// load-bearing: extended tanks / spare bottles as purchasable margin ... the tourist shop becomes an
/// outfitter."</i> And #332 item 2, which had been sitting unfinished in a comment ever since: <i>"A
/// supplies line ('medkits') as a purchasable consumable at havens."</i> Until now every berth service
/// bought something for the VESSEL — reaction mass in her tank, rounds in her bots' magazines. Nothing
/// anywhere sold the person walking her a thing she could carry. The chandlery is that counter.</para>
///
/// <para><b>The prices are read off the bar, never typed.</b> This file contains no credit figure, and
/// that is deliberate: a number typed here would be a second opinion about what a haven charges, and this
/// project's fifth named bug class is exactly two places holding one fact. A haven already prices two
/// things for a person — a glass and a round — in <see cref="Barkeep.DrinkPrice"/> and
/// <see cref="Barkeep.RoundPrice"/>, per haven, and they already climb with distance from the Sun (6 cr a
/// glass on Mars, 8 out at Neptune). The chandlery charges in those units, so the far ports are dearer for
/// air and pills exactly as they are dearer for whisky, and nobody had to invent an economy to make it
/// so.</para>
///
/// <para><b>And the multiplier is not invented either.</b> An extended tank costs
/// <see cref="SuitAir.ExtendedTankFactor"/> rounds because that is the same number that doubles the walk.
/// A refill costs one glass per pill because a pill and a glass are the same size of favour in this
/// game's own reckoning (<see cref="NerveModel.DrinkKind.CalmingPill"/> rides the very same relief seam as
/// the tot). Re-tune either side and the price follows on its own.</para>
///
/// <para>Pure and deterministic: no dice, no clock, no position. A quote asked twice is the same
/// quote.</para>
/// </summary>
public static class Chandlery
{
    /// <summary>#325 · The extended tank's row plate. A PLATE, not prose — it names a thing on a counter
    /// the way the armory's own row names rounds, and the authored sentence about it is said by the suit
    /// when the bottle is actually fitted (<see cref="TankFittedLine"/>).</summary>
    public const string ExtendedTankPlate = "EXTENDED TANK";

    /// <summary>#332 · The med-kit refill's row plate. Same register as above.</summary>
    public const string MedKitRefillPlate = "MED-KIT REFILL";

    /// <summary>#332 · How many calming pills a full cabinet holds — and therefore how many a refill buys
    /// and how many glasses it is charged as. THE stock count: the cabinet reads it, the restock writes to
    /// it, and <see cref="MedKitRefillPrice"/> prices it. It lived in the client as
    /// <c>MedBayPillStock</c> beside the comment that said restocking was a later lane; the lane has
    /// arrived, so the number moved to where both halves can see it rather than being spelled twice.
    ///
    /// <para>Six, unchanged from #343 — this lane buys the cabinet a resupply, not a bigger cabinet.</para>
    /// </summary>
    public const int MedKitFullStock = 6;

    /// <summary>
    /// #325 · WHAT A SPARE BOTTLE COSTS HERE — <see cref="SuitAir.ExtendedTankFactor"/> rounds at this
    /// haven's own bar.
    ///
    /// <para>Read it as the fiction reads it: a tank that doubles your walk costs what standing the room
    /// two rounds costs, and out at The Deep both are dearer than they are at the Roadstead. The only
    /// number in the expression that is not somebody else's is the factor, and that one is the bottle's
    /// own — see <see cref="SuitAir.ExtendedTankFactor"/> for why it is shared rather than copied.</para>
    /// </summary>
    /// <param name="house">The bar at the berth the ship is tied to.</param>
    public static int ExtendedTankPrice(Barkeep house)
    {
        ArgumentNullException.ThrowIfNull(house);
        return house.RoundPrice * SuitAir.ExtendedTankFactor;
    }

    /// <summary>
    /// #332 · WHAT A CABINET OF PILLS COSTS HERE — one glass per pill, at this haven's own card.
    ///
    /// <para><paramref name="pills"/> is how many the cabinet is short, so a captain who took one pill pays
    /// for one. Charging a flat cabinet price would have made the honest thing (swallow one, restock at the
    /// next berth) cost the same as the wasteful one, and a price that does not follow the thing it buys is
    /// how a consumable stops being a decision.</para>
    /// </summary>
    /// <param name="house">The bar at the berth the ship is tied to.</param>
    /// <param name="pills">How many pills the refill puts back. Clamped to a full cabinet.</param>
    public static int MedKitRefillPrice(Barkeep house, int pills)
    {
        ArgumentNullException.ThrowIfNull(house);
        return Math.Clamp(pills, 0, MedKitFullStock) * house.DrinkPrice;
    }

    /// <summary>#332 · How many pills a restock would put back into a cabinet holding
    /// <paramref name="pills"/>. The one place the shortfall is worked out, so the price on the row and the
    /// number the press actually racks can never be two different numbers.</summary>
    public static int MedKitPillsMissing(int pills) =>
        MedKitFullStock - Math.Clamp(pills, 0, MedKitFullStock);

    /// <summary>
    /// #325 · WHAT THE SUIT SAYS WHEN THE BOTTLE GOES ON — Fable-authored, shipped verbatim, said once per
    /// excursion it applies to and on the suit's own channel (the same register as the point-of-no-return
    /// line and the backstop refusal).
    ///
    /// <para>It says the arithmetic out loud because the arithmetic is the purchase: the second sentence is
    /// the promise <see cref="SuitAir.ReserveFactor"/> keeps — the turn-back point moved out with the tank
    /// and is still in the same place relative to it — and saying so is what stops a fitted tank reading as
    /// a licence to go twice as far and turn round at the same moment.</para>
    /// </summary>
    public const string TankFittedLine =
        "Extended tank fitted. The arithmetic is kinder today: twice the walk, and the walk back is still " +
        "half.";

    /// <summary>
    /// #332 · WHAT THE CABINET SAYS WHEN IT IS EMPTY — Fable-authored, shipped verbatim.
    ///
    /// <para>It replaces a line that ended <i>"(Restock is a later lane.)"</i>, which was the game telling
    /// the captain about the backlog instead of about the world. The new one names where the refill is,
    /// which is what a captain standing at an empty cabinet actually needs to know.</para>
    /// </summary>
    public const string CabinetEmptyLine =
        "MED KIT: the pill cabinet is empty — the calming stock is spent. Any haven's chandlery sells the " +
        "refill.";

    /// <summary>#325 · The receipt a bought tank prints — the berth-services receipt shape the armory and
    /// the pump already print (<c>SentryBot.RestockReceiptLine</c>), so a chandlery purchase reads like
    /// every other thing bought at a berth rather than like a new register.</summary>
    public static string TankReceiptLine(int price, int tanksAboard) =>
        $"🧾 {ExtendedTankPlate} — {price:N0} cr. {tanksAboard} aboard, stowed with the suits.";

    /// <summary>#332 · …and the receipt a refill prints, in the same shape.</summary>
    public static string RefillReceiptLine(int pills, int price) =>
        $"🧾 {MedKitRefillPlate} — {pills} {(pills == 1 ? "pill" : "pills")}, {price:N0} cr. The cabinet is full.";

    /// <summary>#325/#332 · EVERY FIXED STRING THIS COUNTER CAN PUT IN FRONT OF A CAPTAIN — the two
    /// authored lines and the two plates, enumerated so the canon sweep has something to sweep.
    ///
    /// <para>A prose guard that has to go and find the strings it is guarding is a guard that silently
    /// stops covering the next one somebody adds. The receipts are not here because they are FORMATS rather
    /// than sentences — they are built from a plate and a number, and both halves are already listed.</para>
    /// </summary>
    public static IReadOnlyList<string> AllProse() =>
        [TankFittedLine, CabinetEmptyLine, ExtendedTankPlate, MedKitRefillPlate];
}
