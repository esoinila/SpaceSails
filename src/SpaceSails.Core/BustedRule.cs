namespace SpaceSails.Core;

/// <summary>
/// PR-BUSTED · The catch (owner ruling §5, 2026-07-17): what the collectors take when a hunter runs
/// you down, and on what dice. GTA-BUSTED grammar — <b>never game over</b> (that lives in
/// <see cref="BoliviaEncounter"/>); the law wants you taxed, not dead. Every number here rolls on the
/// shared <see cref="DiceRule"/> so the pop-up can SHOW its math.
///
/// <para><b>Ruling 1 (confiscation):</b> the collectors take ALL hot-flagged cargo (heists committed
/// under heat) and a heat-scaled share of the carried purse; when the purse is squirreled away
/// (banked/buried) so the visible haul is thin, they take a dice-rolled MINIMUM from clean cargo
/// instead — the law never leaves empty-handed. The mercy law stands: never the last
/// <see cref="MinBerthFeeCr"/> cr (the berth fee), and fuel is never seized (that reach floor is the
/// caller's <see cref="FuelReachability"/> guarantee). Submitting clears heat to 0.</para>
///
/// <para><b>Ruling 2 (bribe):</b> a dice-rolled fee, modifiers visible; pay to keep the cargo, the
/// hunter breaks off, heat is UNCHANGED (you bought this patrol, not the law).</para>
///
/// <para><b>Ruling 3 (resist ladder):</b> heat 1–2 → one opposed dice check: win and the hunter
/// breaks off and heat climbs a notch; lose and you pay SUBMIT terms plus a harsher cut. Heat 3 →
/// the full <see cref="BoliviaEncounter"/>.</para>
/// </summary>
public static class BustedRule
{
    /// <summary>The mercy law's floor on the purse: confiscation never takes the last ~100 cr, the
    /// berth fee that keeps a busted captain from being stranded penniless at the dock.</summary>
    public const int MinBerthFeeCr = 100;

    /// <summary>Below this visible haul (coin share + hot cargo) the purse is judged squirreled away
    /// and the law falls back on a minimum clean-cargo grab (ruling 1). Small on purpose — a genuinely
    /// empty-handed bust still costs a little, but a fat purse pays its share and no clean cargo.</summary>
    public const int MinimumHaulFloorCr = 250;

    /// <summary>The dice-rolled minimum clean-cargo grab, in units, before the heat modifier — the
    /// "never leaves empty-handed" take when the purse is hidden.</summary>
    public const int MinCleanUnits = 1;
    public const int MaxCleanUnits = 3;

    /// <summary>RESIST-and-lose adds this to the coin fraction — the harsher cut for making them
    /// board the hard way (ruling 3).</summary>
    public const double ResistLossExtraFraction = 0.15;

    /// <summary>No confiscation ever takes more than this share of the purse, even a harshened one —
    /// a ceiling so a resist-loss at heat 3 can't clean you out to the last cent.</summary>
    public const double MaxCoinFraction = 0.75;

    /// <summary>The insurance rustbucket wakes with this much in the account — starter-grade, enough
    /// for a berth and a first tank, no more (resurrection, ruling 3).</summary>
    public const int InsuranceCredits = 200;

    public const int StarterSlugAmmo = 12;
    public const int StarterMissileAmmo = 4;

    /// <summary>The heat-scaled share of the CARRIED purse a submission costs (owner's proposed
    /// ladder, §0: heat 1 → 20%, heat 2 → 35%, heat 3 → 50%). Heat 0 shouldn't reach a catch, but
    /// reads as the gentlest rung if it does.</summary>
    public static double CoinFraction(int heat) => heat switch
    {
        <= 1 => 0.20,
        2 => 0.35,
        _ => 0.50,
    };

    /// <summary>One class's worth of a seizure line: what was taken, how many, its fence value, and
    /// whether it was hot (stolen-under-heat) or clean cargo the minimum-take reached for.</summary>
    public readonly record struct CargoSeizure(string CargoClass, int Units, int Value, bool Hot)
    {
        public static CargoSeizure For(string cargoClass, int units, bool hot) =>
            new(cargoClass, units, units * CargoMarket.UnitValue(cargoClass), hot);
    }

    /// <summary>The player's hold as the confiscation reads it: units of a class and how many of them
    /// are hot-flagged (stolen while heated). <see cref="HotUnits"/> is clamped to <see cref="Units"/>.</summary>
    public readonly record struct CargoLot(string CargoClass, int Units, int HotUnits)
    {
        public int CleanUnits => Math.Max(0, Units - HotUnits);
        public int UnitValue => CargoMarket.UnitValue(CargoClass);
    }

    /// <summary>What a confiscation actually took, itemised for the pop-up and re-checkable in tests.</summary>
    /// <param name="CoinTaken">Credits seized from the carried purse.</param>
    /// <param name="CoinLeft">Credits left aboard (respects the berth-fee mercy floor).</param>
    /// <param name="Seizures">Every cargo line taken — hot lots in full, plus any minimum-take clean lots.</param>
    /// <param name="CargoValueTaken">Total fence value of all seized cargo.</param>
    /// <param name="UsedMinimumTake">The purse was thin, so clean cargo was grabbed to fill the floor.</param>
    /// <param name="Breakdown">The math surfaced: the coin fraction and any dice lines, as named modifiers.</param>
    public readonly record struct Confiscation(
        int CoinTaken,
        int CoinLeft,
        IReadOnlyList<CargoSeizure> Seizures,
        int CargoValueTaken,
        bool UsedMinimumTake,
        IReadOnlyList<DiceModifier> Breakdown);

    /// <summary>
    /// Compute a submission's confiscation (ruling 1). Takes all hot cargo and a heat-scaled coin
    /// share; if the visible haul falls under <see cref="MinimumHaulFloorCr"/>, a dice-rolled minimum
    /// of clean cargo (richest class first — the law grabs value) tops it up. Never breaches the
    /// <see cref="MinBerthFeeCr"/> purse floor; never touches fuel. <paramref name="harsher"/> is the
    /// RESIST-and-lose path: a steeper coin fraction plus an extra dice-rolled clean-cargo cut.
    /// Deterministic in <paramref name="seed"/>.
    /// </summary>
    public static Confiscation Confiscate(int heat, int coin, IReadOnlyList<CargoLot> hold, ulong seed, bool harsher = false)
    {
        coin = Math.Max(0, coin);
        double fraction = Math.Min(MaxCoinFraction, CoinFraction(heat) + (harsher ? ResistLossExtraFraction : 0));

        var breakdown = new List<DiceModifier>
        {
            new($"heat {heat} share ({fraction * 100:0}% of purse)", (int)Math.Round(coin * fraction)),
        };

        // The purse: the heat share, but never below the berth-fee mercy floor.
        int coinShare = (int)Math.Round(coin * fraction);
        int spendableCoin = Math.Max(0, coin - MinBerthFeeCr);
        int coinTaken = Math.Clamp(coinShare, 0, spendableCoin);

        // All hot cargo, in full — the stolen-under-heat evidence goes with them.
        var seizures = new List<CargoSeizure>();
        int hotValue = 0;
        foreach (CargoLot lot in hold)
        {
            int hot = Math.Clamp(lot.HotUnits, 0, lot.Units);
            if (hot > 0)
            {
                var s = CargoSeizure.For(lot.CargoClass, hot, hot: true);
                seizures.Add(s);
                hotValue += s.Value;
            }
        }

        // Minimum take: when coin + hot haul is thin (the purse is banked/buried), the law grabs clean
        // cargo so it never leaves empty-handed. Richest class first. Harsher always grabs an extra cut.
        bool usedMinimumTake = false;
        int cleanUnitsToTake = 0;
        if (coinTaken + hotValue < MinimumHaulFloorCr)
        {
            usedMinimumTake = true;
            DiceRoll grab = DiceRule.RollAmount(
                DiceRule.Seed(seed, "confiscate-min"), MinCleanUnits, MaxCleanUnits,
                [new DiceModifier($"heat {heat}", Math.Max(0, heat - 1))]);
            cleanUnitsToTake += Math.Max(0, grab.Total);
            breakdown.Add(new DiceModifier($"minimum take (rolled {grab.Face} + heat)", grab.Total));
        }

        if (harsher)
        {
            DiceRoll extra = DiceRule.RollAmount(
                DiceRule.Seed(seed, "confiscate-harsher"), 1, 2, [new DiceModifier("resisted", 1)]);
            cleanUnitsToTake += Math.Max(0, extra.Total);
            breakdown.Add(new DiceModifier("resisted — harsher cut (units)", extra.Total));
        }

        if (cleanUnitsToTake > 0)
        {
            TakeCleanCargo(hold, cleanUnitsToTake, seizures);
        }

        int cargoValueTaken = 0;
        foreach (CargoSeizure s in seizures)
        {
            cargoValueTaken += s.Value;
        }

        return new Confiscation(coinTaken, coin - coinTaken, seizures, cargoValueTaken, usedMinimumTake, breakdown);
    }

    // Grab up to `units` of clean cargo, richest fence-class first (the law takes value, not volume),
    // folding into any existing clean seizure line for the same class.
    private static void TakeCleanCargo(IReadOnlyList<CargoLot> hold, int units, List<CargoSeizure> seizures)
    {
        var clean = new List<CargoLot>();
        foreach (CargoLot lot in hold)
        {
            if (lot.CleanUnits > 0)
            {
                clean.Add(lot);
            }
        }

        clean.Sort((a, b) => b.UnitValue != a.UnitValue
            ? b.UnitValue.CompareTo(a.UnitValue)
            : string.CompareOrdinal(a.CargoClass, b.CargoClass));

        int remaining = units;
        foreach (CargoLot lot in clean)
        {
            if (remaining <= 0)
            {
                break;
            }

            int take = Math.Min(lot.CleanUnits, remaining);
            remaining -= take;
            seizures.Add(CargoSeizure.For(lot.CargoClass, take, hot: false));
        }
    }

    /// <summary>The bribe demand (ruling 2): a dice-rolled fee scaled by heat, modifiers visible. Pay
    /// it and the cargo stays, the hunter breaks off, heat is unchanged. Returned as a
    /// <see cref="DiceRoll"/> so the pop-up shows the roll and the stack.</summary>
    public static DiceRoll BribeDemand(int heat, ulong seed, IReadOnlyList<DiceModifier>? modifiers = null)
    {
        (int min, int max) = heat switch
        {
            <= 1 => (150, 400),
            2 => (400, 800),
            _ => (800, 1500),
        };

        return DiceRule.RollAmount(DiceRule.Seed(seed, "bribe"), min, max, modifiers);
    }

    /// <summary>The heat 1–2 resist check (ruling 3): the player's opposed roll against the collector,
    /// whose defence stiffens with heat (grittier muscle on a fat bounty). Win → the caller breaks the
    /// hunter off and raises heat a notch; lose → the caller applies <see cref="Confiscate"/> with
    /// <c>harsher: true</c>. <paramref name="playerModifiers"/> carries any purchased dice helpers.</summary>
    public static OpposedRoll ResistCheck(int heat, ulong seed, IReadOnlyList<DiceModifier>? playerModifiers = null)
    {
        var collectorMods = new List<DiceModifier> { new("collector's grip (heat)", heat) };
        return DiceRule.Opposed(DiceRule.Seed(seed, "resist"), playerModifiers, collectorMods);
    }

    /// <summary>The starter-grade ship state a brain-backup wakes into (resurrection, ruling 3): the
    /// insurance rustbucket. Everything visible aboard is gone (the caller clears cargo, hot flags, and
    /// visible coin); buried/banked survives untouched because it lives off-ship. The tank comes up at
    /// the mercy floor — the reach-a-pump reserve the law never drains — so you wake grounded near a
    /// pump, not stranded. Never a dead save.</summary>
    public readonly record struct ResurrectionKit(
        int Credits,
        int ReactionMassPulses,
        int SlugAmmo,
        int MissileAmmo,
        int MassLevel,
        int SensorLevel,
        int HoldLevel,
        int TelescopeLevel);

    /// <summary>Build the insurance rustbucket: insurance credits, a mercy-floor tank, starter ammo,
    /// and every upgrade level reset to base.</summary>
    public static ResurrectionKit Resurrect(int mercyFloorPulses) =>
        new(InsuranceCredits, Math.Max(0, mercyFloorPulses), StarterSlugAmmo, StarterMissileAmmo,
            MassLevel: 0, SensorLevel: 0, HoldLevel: 0, TelescopeLevel: 0);

    /// <summary>The parrot's phrasing of the current confiscation exposure, quoted at each heat
    /// crossing (owner: "they'll take a third of the purse if they catch us!"). Names the coin share
    /// and the hot-cargo law in one breath. Fills the {0} slot of <see cref="Parrot.Squawk.Busted"/>.</summary>
    public static string ExposurePhrase(int heat) => heat switch
    {
        <= 0 => "nothing yet — fly clean and they've no claim",
        1 => "a fifth of the purse and every hot crate if they catch us",
        2 => "a THIRD of the purse and all the hot cargo, captain",
        _ => "HALF the purse and the whole hot hold — and they'll board to get it",
    };
}
