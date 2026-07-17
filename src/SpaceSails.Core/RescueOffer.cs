using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #266 — the adrift rescue offer's terms, formatted as pure text so the pop-up and its tests read the
/// same words. Going dry with no pump in reach is a story beat, not a strip: before the captain accepts,
/// the offer must SAY who comes, what it costs, and what happens to hot cargo. The client owns the live
/// manifest and the accept wiring (RequestRescue); this owns how the offer READS.
/// </summary>
public static class RescueOffer
{
    /// <summary>One line of the confiscation manifest shown before you accept — a hold class, its unit
    /// count, its fence value, and whether it's hot (stolen). Mirrors the Trade desk's manifest read.</summary>
    public readonly record struct FeeLine(string CargoClass, int Units, int Value, bool Hot);

    /// <summary>The tug's promise: it answers the whistle, tows you clear, and tops the tank back to
    /// <paramref name="capacity"/> pulses of reaction mass. What the rescue BRINGS, in the ship's voice.</summary>
    public static string TowPromise(int capacity) =>
        $"A rescue tug answers the whistle, tows you clear of the dark, and tops the tank back to {capacity} p of reaction mass.";

    /// <summary>The fee headline: the whole hold, confiscated. Names the total units and fence value
    /// (and how many are hot), or says the hold is empty — then the tow costs only pride. The per-class
    /// breakdown is rendered from <see cref="FeeLine"/>s alongside this line.</summary>
    public static string FeeHeadline(IReadOnlyList<FeeLine> lines)
    {
        int units = lines.Sum(l => l.Units);
        if (units <= 0)
        {
            return "The hold is empty — the tow costs you nothing but the tale of it.";
        }

        int value = lines.Sum(l => l.Value);
        int hot = lines.Where(l => l.Hot).Sum(l => l.Units);
        string unitWord = units == 1 ? "unit" : "units";
        string hotNote = hot > 0 ? $", {hot} of them hot" : "";
        return $"The fee: every {unitWord} in the hold confiscated — {units} {unitWord}{hotNote}, worth ~{value.ToString("N0", CultureInfo.InvariantCulture)} cr.";
    }
}
