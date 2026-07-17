namespace SpaceSails.Core;

/// <summary>
/// #157 — "How do I fill her up?" · the price of a pulse and the arithmetic of a fill. Until now the
/// reaction-mass tank only ever <em>emptied</em> (every burn spends pulses; nothing put them back but a
/// free dockside top-off). This is the first place fuel is <b>bought</b>: a pure, deterministic rule the
/// Trade desk's ⛽ FILL HER UP button reads, so the price lives in Core beside the depot map, not
/// hardcoded in a razor.
///
/// <para><b>Where the number comes from.</b> The economy already sets the scale: a captain starts with
/// <c>1,500 cr</c>, a clean milk run sells for ~<c>2,000 cr</c>, cargo runs ~100–1,200 cr/unit, and the
/// tank is <c>500</c> pulses (#262 — sized so inner-system fuel intuitions survive the outer dark; a
/// cross-well transfer spends ~10–120 of them). We want a full-from-near-empty refill to be a
/// <em>meaningful</em> line item — felt, planned around — but never a soft-lock. At
/// <see cref="InnerPricePerPulse"/> = 3 cr/pulse:
/// <list type="bullet">
/// <item>a full 500-p tank costs <c>1,500 cr</c> — a whole starting purse: meaningful, never crushing (and
/// you never actually buy 500 from zero — an empty tank is already stranded);</item>
/// <item>the usual top-up from the amber reserve (~90 p) back to full (~410 p) is <c>1,230 cr</c> — covered
/// by a single good cargo sale;</item>
/// <item>the fuel to set up a big ~120-p cross-well haul is <c>360 cr</c>, a rounding error against the
/// thousands the He3 in the hold is worth.</item>
/// </list></para>
///
/// <para><b>Outer-system markup.</b> Fuel is refined in the inner system; out past the belt it is dear.
/// A pump whose heliocentric orbit sits at or beyond <see cref="OuterMarkupThresholdMeters"/> (between
/// Mars and Jupiter) charges <see cref="OuterPricePerPulse"/> = 4 cr/pulse — a mild, thematic surcharge
/// right where the well is deep and the captain is most fuel-stressed. One clearly named seam; a future
/// per-haven price table drops in behind <see cref="PricePerPulse"/> without touching the desk.</para>
/// </summary>
public static class FuelMarket
{
    /// <summary>Inner-system price of one reaction-mass pulse, in credits. See the class remarks for the
    /// derivation from the 1,500-cr purse / 500-p tank economy.</summary>
    public const int InnerPricePerPulse = 3;

    /// <summary>Outer-system price of one pulse — the markup past the belt where fuel is dear.</summary>
    public const int OuterPricePerPulse = 4;

    /// <summary>Heliocentric distance (m) at or beyond which a pump charges the outer price. Sits between
    /// Mars's orbit (2.28e11 m) and Jupiter's (7.79e11 m).</summary>
    public const double OuterMarkupThresholdMeters = 4e11;

    /// <summary>The price of a pulse at a pump whose depot rides <paramref name="pumpHeliocentricDistanceMeters"/>
    /// from the Sun: the inner price inside the belt, the outer price beyond it.</summary>
    public static int PricePerPulse(double pumpHeliocentricDistanceMeters) =>
        pumpHeliocentricDistanceMeters >= OuterMarkupThresholdMeters ? OuterPricePerPulse : InnerPricePerPulse;

    /// <summary>A fill quote: how many pulses the captain actually takes on and what it costs. Both are
    /// non-negative; <see cref="Cost"/> is exactly <c>Pulses × pricePerPulse</c>.</summary>
    public readonly record struct Quote(int Pulses, int Cost);

    /// <summary>
    /// Price a fill. The captain asks for <paramref name="pulsesWanted"/> pulses (pass <see cref="int.MaxValue"/>
    /// for "fill her up"); the quote is clamped by two ceilings and never goes negative:
    /// <list type="bullet">
    /// <item><b>the tank</b> — never buy past <paramref name="capacity"/> (<c>capacity − current</c> room);</item>
    /// <item><b>the purse</b> — with <paramref name="credits"/> on hand you can only afford
    /// <c>credits / pricePerPulse</c> pulses, so an under-funded captain buys what they can and no more
    /// (the honest "buy what you can afford" of #157).</item>
    /// </list>
    /// A non-positive <paramref name="pricePerPulse"/> is treated as free (cost 0). Deterministic.
    /// </summary>
    public static Quote QuoteFill(int currentPulses, int capacity, int pricePerPulse, int credits, int pulsesWanted)
    {
        int room = Math.Max(0, capacity - currentPulses);
        int wanted = Math.Clamp(pulsesWanted, 0, room);
        if (pricePerPulse <= 0)
        {
            return new Quote(wanted, 0);
        }

        int affordable = Math.Max(0, credits) / pricePerPulse;
        int pulses = Math.Min(wanted, affordable);
        return new Quote(pulses, pulses * pricePerPulse);
    }
}
