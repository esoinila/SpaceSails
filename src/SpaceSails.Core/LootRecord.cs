namespace SpaceSails.Core;

/// <summary>
/// #202 — the crime's receipt. The piracy twin of the honest payment receipts (#185): a booked line
/// for a completed boarding — WHAT was taken, HOW MANY units, its estimated fence WORTH, off WHOM,
/// WHERE, and the sim-time it happened. Pure data with one formatted <see cref="Describe"/> line so
/// the Captain's ledger, the parrot, and the tests all read one truth. "Now it is kind of unknown
/// unless one is a forensic accountant" — this is the books. Deliberately one record, not a system.
/// </summary>
/// <param name="CargoClass">What was taken (the cargo class — He3, Ice, …).</param>
/// <param name="Units">How many units crossed to our hold.</param>
/// <param name="EstimatedWorth">The fence value of the haul, priced through <see cref="CargoMarket"/>.</param>
/// <param name="VictimCallsign">Off whom — the boarded hull's callsign.</param>
/// <param name="Where">Where it happened — the nearest named body, or open space.</param>
/// <param name="SimTime">Sim time of the boarding (the ledger stamps its provenance from this).</param>
public readonly record struct LootRecord(
    string CargoClass,
    int Units,
    int EstimatedWorth,
    string VictimCallsign,
    string Where,
    double SimTime)
{
    /// <summary>Build a loot record for a haul, pricing it through <see cref="CargoMarket"/> so the
    /// receipt's worth and the credited cargo value agree.</summary>
    public static LootRecord ForHaul(string cargoClass, int units, string victimCallsign, string where, double simTime) =>
        new(cargoClass, units, units * CargoMarket.UnitValue(cargoClass), victimCallsign, where, simTime);

    /// <summary>The ledger line: e.g. "6 units of He3 off Larkspur, near Mars — est. 7,200 cr".</summary>
    public string Describe() =>
        $"{Units} units of {CargoClass} off {VictimCallsign}, near {Where} — est. {EstimatedWorth:N0} cr";
}
