namespace SpaceSails.Core;

/// <summary>
/// PR-BUSTED · Hot cargo (owner ruling §5.1, 2026-07-17): cargo stolen while heat &gt; 0 is flagged
/// HOT at theft time — the stolen-under-heat evidence the collectors confiscate in full. When heat
/// fully cools to 0 the flags LAUNDER off ("stolen cargo launders — it becomes safe(r)").
///
/// <para>A small mutable book, in the shape of <see cref="ContactLedger"/> / <see cref="ShipAlerts"/>:
/// the client owns one, stamps at the boarding moment, and launders when heat hits 0. Kept pure and
/// Core-side so the stamp/launder law and the confiscation that reads it share one truth and one
/// test. Units are tracked per cargo class; a class's hot count never exceeds what the client says is
/// aboard (the caller clamps via <see cref="BuildLots"/>).</para>
/// </summary>
public sealed class HotCargoLedger
{
    private readonly Dictionary<string, int> _hotByClass = new();

    /// <summary>Stamp <paramref name="units"/> of a class hot — but ONLY when the theft happened under
    /// heat (heat &gt; 0). A clean-space theft stamps nothing (there is no crime on the books to
    /// confiscate). Returns the class's new hot total.</summary>
    public int Stamp(string cargoClass, int units, int heatAtTheft)
    {
        if (units <= 0 || heatAtTheft <= 0)
        {
            return _hotByClass.GetValueOrDefault(cargoClass);
        }

        int total = _hotByClass.GetValueOrDefault(cargoClass) + units;
        _hotByClass[cargoClass] = total;
        return total;
    }

    /// <summary>The hot units currently flagged for a class (0 if none).</summary>
    public int HotUnits(string cargoClass) => _hotByClass.GetValueOrDefault(cargoClass);

    /// <summary>Total hot units across all classes — the "how much evidence is aboard" read.</summary>
    public int TotalHotUnits
    {
        get
        {
            int sum = 0;
            foreach (int v in _hotByClass.Values)
            {
                sum += v;
            }

            return sum;
        }
    }

    /// <summary>Any hot cargo at all is aboard.</summary>
    public bool Any => TotalHotUnits > 0;

    /// <summary>Launder every flag off — the heat has fully cooled to 0, the stolen cargo is safe now
    /// (ruling 1). Idempotent. Returns true if anything was actually cleared.</summary>
    public bool Launder()
    {
        if (_hotByClass.Count == 0)
        {
            return false;
        }

        _hotByClass.Clear();
        return true;
    }

    /// <summary>Drop a class entirely (e.g. it was sold or seized) so its flags don't outlive the
    /// cargo. Harmless if the class isn't flagged.</summary>
    public void Forget(string cargoClass) => _hotByClass.Remove(cargoClass);

    /// <summary>Project the current hold — the client's class→units aggregate — into the
    /// <see cref="BustedRule.CargoLot"/> list a confiscation reads, folding in each class's hot count
    /// (clamped to the units actually aboard: a class can never be "more hot than present"). Classes
    /// with zero units are dropped.</summary>
    public IReadOnlyList<BustedRule.CargoLot> BuildLots(IReadOnlyDictionary<string, int> cargoByClass)
    {
        var lots = new List<BustedRule.CargoLot>();
        foreach ((string cargoClass, int units) in cargoByClass)
        {
            if (units <= 0)
            {
                continue;
            }

            int hot = Math.Clamp(HotUnits(cargoClass), 0, units);
            lots.Add(new BustedRule.CargoLot(cargoClass, units, hot));
        }

        return lots;
    }
}
