namespace SpaceSails.Core;

/// <summary>
/// #251 · WHERE TO LOOK: THE MAP ITSELF IS THE CLUE — the discrepancy record, the hull's area, the
/// discrepancies a plan shows, and the speed-up a clue buys.
///
/// <para>Split out of <c>HullSounding.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class HullSounding
{
    // ── Where to look: the map itself is the clue ─────────────────────────────────────────────────────

    /// <summary>
    /// A PLACE THE BOOKS DO NOT BALANCE. Owner: <i>"some logic on selection based on map or other clues."</i>
    /// </summary>
    /// <param name="Reason">What does not add up, in the captain's own terms — never "a void is here".</param>
    /// <param name="X0">Aft edge of the suspect band, deck units.</param>
    /// <param name="X1">Forward edge.</param>
    /// <param name="Top">Which side of the spine it is on.</param>
    /// <param name="AreaSquareDu">How much ship the discrepancy accounts for — i.e. how big the search is once
    /// you have believed the clue.</param>
    public readonly record struct Discrepancy(string Reason, double X0, double X1, bool Top, double AreaSquareDu);

    /// <summary>
    /// THE HULL'S OWN AREA, from her outline. Everything below is measured against this, so if the hull is ever
    /// re-cut the arithmetic follows rather than going quietly stale.
    /// </summary>
    public static double HullArea(double aftX, double bowX, double topY, double bottomY) =>
        System.Math.Abs((bowX - aftX) * (bottomY - topY));

    /// <summary>
    /// THE DEDUCTION, and it is one a player can make by looking. Each compartment has a counterpart directly
    /// across the spine; a ship is built symmetrically about her keel because that is how frames are laid. So a
    /// room that is SHORTER than its opposite number is space that has gone somewhere, and the mimic board draws
    /// both rectangles from these very numbers — the clue is not a readout, it is the map being looked at
    /// properly.
    ///
    /// <para>Returns every band that does not balance, largest first, so a captain sounding on a clue starts with
    /// the biggest lie. An empty list means her books balance and there is nothing to find by measuring —
    /// which is itself worth knowing, and worth NOT spending two hundred seconds discovering.</para>
    /// </summary>
    public static IReadOnlyList<Discrepancy> Discrepancies(
        IReadOnlyList<(string Name, float X0, float X1, bool Top)> compartments,
        double spineHalfHeight, double topY, double bottomY)
    {
        ArgumentNullException.ThrowIfNull(compartments);

        // Lab 44 probe D found the FIRST version of this wrong, which is exactly what the lab is for. It measured
        // each room against the total overlap opposite it and then guessed where the shortfall sat — and guessed
        // both the END and the SIDE wrong: with NEAR HOLD's bulkhead moved four frames forward it pointed at
        // x −4…0 on the TOP side when the unaccounted space is x −15…−11 on the BOTTOM. A clue that names the
        // wrong wall is worse than no clue, because a captain sounds it, hears solid, and stops believing the
        // instrument.
        //
        // The honest construction does not guess at all: walk the ship's LENGTH and compare which side has a
        // room there. Space is unaccounted for exactly where one side is roomed and the other is not, and the
        // void is on the side that is MISSING the room. No inference, no reasoning about bulkheads — just an
        // interval on one list that is not on the other.
        List<Discrepancy> found = [];
        double depthTop = System.Math.Abs(-spineHalfHeight - topY);
        double depthBottom = System.Math.Abs(bottomY - spineHalfHeight);

        foreach (bool roomedSide in new[] { true, false })
        {
            var roomed = compartments.Where(c => c.Top == roomedSide)
                                     .OrderBy(c => c.X0)
                                     .ToList();
            var opposite = compartments.Where(c => c.Top != roomedSide)
                                       .OrderBy(c => c.X0)
                                       .ToList();

            foreach ((string name, float x0, float x1, bool _) in roomed)
            {
                // Subtract every opposite-side room from this one's run, and whatever is left over is ship that
                // exists on one side of the keel and not the other.
                List<(double A, double B)> remaining = [(x0, x1)];
                foreach ((string _, float ox0, float ox1, bool _) in opposite)
                {
                    List<(double A, double B)> next = [];
                    foreach ((double a, double b) in remaining)
                    {
                        if (ox1 <= a || ox0 >= b)
                        {
                            next.Add((a, b));
                            continue;
                        }
                        if (ox0 > a) { next.Add((a, ox0)); }
                        if (ox1 < b) { next.Add((ox1, b)); }
                    }
                    remaining = next;
                }

                foreach ((double a, double b) in remaining)
                {
                    double width = b - a;
                    if (width <= 0.5)
                    {
                        continue;   // balanced, or within a frame's worth of measuring error
                    }

                    // The VOID is on the side with no room in it, and it is exactly that interval long.
                    bool voidSide = !roomedSide;
                    double depth = voidSide ? depthBottom : depthTop;
                    found.Add(new Discrepancy(
                        $"{name} runs {width:0.#} frames further {(a < 0 ? "aft" : "forward")} than anything " +
                        $"across the spine from it",
                        a, b, voidSide, width * depth));
                }
            }
        }

        return [.. found.OrderByDescending(d => d.AreaSquareDu)];
    }

    /// <summary>
    /// WHAT BELIEVING THE CLUE IS WORTH, as one number a panel can print. The ratio of a blind hull-wide search
    /// to a search inside the discrepancy — the thing that decides whether any of this is a mechanic or a chore.
    /// </summary>
    public static double CluedSpeedup(Method method, double hullArea, double clueArea) =>
        clueArea <= 0 ? double.PositiveInfinity
        : SecondsToCover(method, hullArea) / System.Math.Max(1e-9, SecondsToCover(method, clueArea));
}
