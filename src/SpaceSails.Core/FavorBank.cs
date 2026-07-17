namespace SpaceSails.Core;

/// <summary>
/// PR-WIRE — the favor bank (FridaySecondPlan §0/PR-WIRE, rulings 5 &amp; 6). A contact's goodwill is
/// a number you can literally bank on: park coin with a trusted contact (off the purse, invisible to
/// confiscation by construction — see the BUSTED seam below), and, when you're broke at a pump, borrow
/// anonymized gas money against a favor owed. Every price here is a pure, deterministic Core rule so
/// the desk, the pop-up and the tests read ONE book — the same discipline <see cref="FuelMarket"/>
/// keeps for fuel.
///
/// <para><b>The two moods, ruling 5 — the bank runs on distress.</b>
/// <list type="bullet">
/// <item><b>CALM (heat 0): deposits earn decent interest.</b> Parked coin grows at
/// <see cref="DailyInterestRate"/> = 0.25%/day. The economy sets the scale (<see cref="FuelMarket"/>:
/// 1,500-cr start, ~2,000-cr milk run): 1,000 cr left with a fence over a 20-day lay-low
/// (<see cref="EncounterRule.HeatDecayDays"/>) earns ~50 cr; a 2,000-cr purse over a 40-day quiet
/// stretch earns ~200 cr — a real reason to bank between runs, never a bank you can farm.</item>
/// <item><b>HEATED (heat &gt; 0): depositing is FENCING, and it costs.</b> A cut proportional to heat
/// is taken on the way in — dice-rolled (ruling 0, the dice are the engine), and <b>always strictly
/// less than the collector would confiscate at that same heat</b> (the whole point: safety has a
/// price, but the fence is cheaper than the law). See <see cref="FenceCutFraction"/> for the proven
/// inequality.</item>
/// </list></para>
///
/// <para><b>The confiscation seam (BUSTED owns the authority).</b> The fence-cut inequality is stated
/// against <see cref="ConfiscationShare"/> — the collectors' heat-scaled take, mirrored here from the
/// owner's PR-BUSTED proposal (heat 1 → 20%, heat 2 → 35%, heat 3 → 50%). <c>second/busted</c> owns
/// the canonical confiscation rule; when it lands, this method points at it and the inequality test
/// still guards the seam. Likewise the DICE: this lane rolls on the local, deterministic
/// <see cref="Roll"/> (SplitMix64, a marked TODO-seam) until <c>second/busted</c>'s shared DiceRule
/// lands, at which point <see cref="FenceCutFraction"/>/<see cref="AccrueInterest"/> take the shared
/// roll unchanged — every function here already takes the roll as a parameter, so the engine swaps
/// under them without a signature change.</para>
///
/// <para><b>Borrowing (ruling 5, strings attached).</b> A <see cref="TrustTier.Trusted"/> contact
/// (three jobs done, <see cref="ContactSheets.TrustedAtMissions"/>) wires you gas money with one of
/// two strings: an <b>interest debt</b> booked on the ledger (you owe principal +
/// <see cref="BorrowInterestFraction"/>), or a <b>favor debt</b> — no interest, but you owe them one
/// quiet delivery that arrives later in their voice (<see cref="FavorObligation"/>). Working that
/// delivery off IS the repayment: it books the principal back and clears the debt.</para>
/// </summary>
public static class FavorBank
{
    // ---- CALM: interest on parked coin (heat 0 only) ----

    /// <summary>Daily interest a calm deposit earns — 0.25%/day. Derivation in the class remarks: sized
    /// off the 1,500-cr purse / 20-day lay-low economy so a bank between runs is worth it, never a farm.</summary>
    public const double DailyInterestRate = 0.0025;

    /// <summary>Interest earned on a parked <paramref name="balance"/> over <paramref name="days"/> —
    /// but ONLY while calm. Ruling 5: interest is the reward for banking in peacetime; the moment the
    /// captain is heated (<paramref name="heatLevel"/> &gt; 0) the money stops growing (a hot account
    /// draws no favors). Non-positive balance or span earns nothing. Deterministic.</summary>
    public static long AccrueInterest(long balance, double days, int heatLevel)
    {
        if (heatLevel > 0 || balance <= 0 || days <= 0)
        {
            return 0;
        }

        return (long)Math.Round(balance * DailyInterestRate * days);
    }

    // ---- The dice engine (TODO-seam: replace with second/busted's shared DiceRule when it lands) ----

    /// <summary>A deterministic dice roll in [0,1) from a string seed — the local engine this lane rolls
    /// the fence cut on until <c>second/busted</c>'s shared DiceRule lands (ruling 0, "the dice are the
    /// engine"). Same SplitMix64 the rest of Core rolls on (<see cref="DeterministicRandom"/>), salted so
    /// a favor-bank roll never correlates with an encounter roll on the same id. When the shared engine
    /// arrives, callers pass its roll into <see cref="FenceCutFraction"/> unchanged — this helper simply
    /// retires.</summary>
    public static double Roll(string seed) =>
        new DeterministicRandom(HashSeed(seed) ^ 0x466176_426E6BUL).NextDouble();

    // FNV-1a 64-bit, matching EncounterRule's stable cross-process hash (string.GetHashCode is
    // per-run randomized — determinism is law in Core).
    private static ulong HashSeed(string id)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis;
        foreach (char c in id ?? string.Empty)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }

    // ---- HEATED: fencing coin to safety costs a heat-scaled, dice-rolled cut ----

    /// <summary>The collectors' confiscation share at a heat level — the BUSTED seam (owner's proposal:
    /// heat 1 → 20%, heat 2 → 35%, heat 3 → 50%). Mirrored here only so the fence-cut inequality can be
    /// stated and tested; <c>second/busted</c> owns the canonical rule. Heat 0 (or below) confiscates
    /// nothing.</summary>
    public static double ConfiscationShare(int heatLevel) => heatLevel switch
    {
        <= 0 => 0.0,
        1 => 0.20,
        2 => 0.35,
        _ => 0.50,
    };

    /// <summary>How far the fence ALWAYS undercuts the collector — the safety margin baked into the
    /// fence cut so it is strictly below <see cref="ConfiscationShare"/> at every heat, whatever the
    /// dice say. 5 percentage points.</summary>
    public const double ConfiscationMargin = 0.05;

    /// <summary>The dice floor: a fence still takes at least half the collector's share (fencing isn't
    /// free — it's just cheaper than getting caught).</summary>
    public const double FenceFloorFactor = 0.5;

    /// <summary>The most a fence can take at this heat: the collector's share minus the margin — so it
    /// is provably below confiscation. (Heat 0 fences for free.)</summary>
    public static double MaxFenceFraction(int heatLevel) =>
        heatLevel <= 0 ? 0.0 : Math.Max(0.0, ConfiscationShare(heatLevel) - ConfiscationMargin);

    /// <summary>The least a fence takes at this heat.</summary>
    public static double MinFenceFraction(int heatLevel) =>
        heatLevel <= 0 ? 0.0 : ConfiscationShare(heatLevel) * FenceFloorFactor;

    /// <summary>
    /// The dice-rolled fence cut fraction for depositing while heated: interpolates
    /// <see cref="MinFenceFraction"/>…<see cref="MaxFenceFraction"/> by <paramref name="roll"/> ∈ [0,1).
    /// <b>The inequality (ruling 5, tested):</b> for every heat &gt; 0 and every roll, the result is
    /// ≤ <see cref="MaxFenceFraction"/> = <see cref="ConfiscationShare"/> − <see cref="ConfiscationMargin"/>
    /// &lt; <see cref="ConfiscationShare"/> — the fence is ALWAYS cheaper than the law. Heat 0 is 0.
    /// </summary>
    public static double FenceCutFraction(int heatLevel, double roll)
    {
        if (heatLevel <= 0)
        {
            return 0.0;
        }

        double lo = MinFenceFraction(heatLevel);
        double hi = MaxFenceFraction(heatLevel);
        return lo + Math.Clamp(roll, 0.0, 1.0) * (hi - lo);
    }

    /// <summary>The coin a fence takes off a deposit of <paramref name="amount"/> while heated —
    /// <see cref="FenceCutFraction"/> of it, rounded. Never negative.</summary>
    public static long FenceCut(long amount, int heatLevel, double roll) =>
        (long)Math.Round(Math.Max(0, amount) * FenceCutFraction(heatLevel, roll));

    /// <summary>What a deposit actually banks: the gross handed over, the fence's cut (0 when calm), and
    /// the net that lands on the balance. <see cref="Credited"/> = <see cref="Gross"/> − <see cref="Cut"/>.</summary>
    public readonly record struct DepositQuote(long Gross, long Cut, long Credited, double CutFraction);

    /// <summary>Price a deposit of <paramref name="amount"/> at the given heat, with a dice
    /// <paramref name="roll"/> for the (heated-only) fence cut. Calm deposits land in full; heated ones
    /// lose the fence's cut — always less than the collector's share (see <see cref="FenceCutFraction"/>).</summary>
    public static DepositQuote PriceDeposit(long amount, int heatLevel, double roll)
    {
        long gross = Math.Max(0, amount);
        double frac = FenceCutFraction(heatLevel, roll);
        long cut = FenceCut(gross, heatLevel, roll);
        return new DepositQuote(gross, cut, gross - cut, frac);
    }

    // ---- BORROWING: the favor wire (trusted contacts only) ----

    /// <summary>The premium on an interest-bearing loan: you owe principal + 20%. (A favor debt owes no
    /// interest — you owe a delivery instead.)</summary>
    public const double BorrowInterestFraction = 0.20;

    /// <summary>What an interest debt of <paramref name="principal"/> costs to clear — principal plus the
    /// <see cref="BorrowInterestFraction"/> premium, rounded.</summary>
    public static long InterestDebtTotal(long principal) =>
        Math.Max(0, principal) + (long)Math.Round(Math.Max(0, principal) * BorrowInterestFraction);

    /// <summary>Will this contact wire us money at all? Ruling 5: only a <see cref="TrustTier.Trusted"/>
    /// contact (≥ <see cref="ContactSheets.TrustedAtMissions"/> jobs done) stakes you; and only a
    /// dark-web-native contact can WIRE it (an in-person-only contact can hand it over, but not while
    /// you're stranded at a pump). This gate is the pump-side one: trusted AND wire-capable.</summary>
    public static bool CanWireLoan(ContactSheet sheet, int missionsCompleted) =>
        sheet.CanWire && ContactSheets.WillStake(missionsCompleted);

    // ---- Channel gating for deposits / withdrawals (ruling 6) ----

    /// <summary>Banking across the wire (dark-web desk, from anywhere) is only for dark-web-native
    /// contacts — the asteroid hermit's coin lives on their rock. Mirror of <see cref="ContactSheet.CanWire"/>,
    /// named for the gate it enforces.</summary>
    public static bool CanBankRemotely(ContactSheet sheet) => sheet.CanWire;

    /// <summary>Banking in person (at their bar table) works for everyone — it's the hermit's only
    /// channel and every wire-native contact's too.</summary>
    public static bool CanBankInPerson(ContactSheet sheet) => true;

    // ---- Ledger transaction builders (shared by Map + tests, so both post one shape) ----

    /// <summary>The passbook line for a calm deposit (or the net of a heated one): balance ↑ by the net.</summary>
    public static CreditTransaction DepositTxn(long netCredited, double simTime, string note) =>
        new(CreditKind.Deposit, Math.Max(0, netCredited), simTime, note);

    /// <summary>The passbook line for a fence's distress cut: balance ↓ by the cut.</summary>
    public static CreditTransaction FenceCutTxn(long cut, double simTime, string note) =>
        new(CreditKind.FenceCut, -Math.Max(0, cut), simTime, note);

    /// <summary>The passbook line for interest paid on parked coin: balance ↑.</summary>
    public static CreditTransaction InterestTxn(long interest, double simTime, string note) =>
        new(CreditKind.Interest, Math.Max(0, interest), simTime, note);

    /// <summary>The passbook line for drawing coin back out: balance ↓ toward zero.</summary>
    public static CreditTransaction WithdrawalTxn(long amount, double simTime, string note) =>
        new(CreditKind.Withdrawal, -Math.Max(0, amount), simTime, note);

    /// <summary>The passbook line for a wired loan: balance ↓ below zero by what we now owe (principal,
    /// or principal + interest premium for an interest debt).</summary>
    public static CreditTransaction BorrowTxn(long owed, double simTime, string note) =>
        new(CreditKind.Borrow, -Math.Max(0, owed), simTime, note);

    /// <summary>The passbook line for paying a debt down (coin, or a favor worked off): balance ↑.</summary>
    public static CreditTransaction RepaymentTxn(long amount, double simTime, string note) =>
        new(CreditKind.Repayment, Math.Max(0, amount), simTime, note);
}

/// <summary>
/// A favor owed (ruling 5, the favor-debt string): when a contact wires gas money against a favor
/// rather than interest, the ledger books the principal as debt AND records this obligation — "you owe
/// &lt;contact&gt; one quiet delivery." It surfaces later, in their voice, through the existing offer
/// machinery as a modest delivery contract; completing that delivery IS the repayment (it books the
/// principal back, clearing the debt). One obligation kind, shipped end to end.
/// </summary>
public readonly record struct FavorObligation(
    string ContactId,
    string DisplayName,
    long PrincipalCredits,
    double IncurredSimTime,
    string VoiceLine)
{
    /// <summary>Raise the favor a contact calls in when they wire you money on a promise. The voice
    /// line reads in their character (<see cref="ContactSheet.VoiceStyle"/>).</summary>
    public static FavorObligation ForLoan(ContactSheet sheet, long principal, double simTime) =>
        new(sheet.ContactId, sheet.DisplayName, Math.Max(0, principal), simTime, ObligationVoice(sheet));

    /// <summary>The favor called in, in the contact's own voice — the line that arrives later ("you owe
    /// &lt;contact&gt; one quiet delivery").</summary>
    public static string ObligationVoice(ContactSheet sheet) => sheet.VoiceStyle switch
    {
        "warm-underworld" => $"“I staked you when the tank ran dry, captain. Now run a quiet parcel for me — no questions, and we're square.”",
        "clipped-discreet" => $"“You took my coin. There's a package needs moving, discreetly. Deliver it and the debt's gone. We never spoke.”",
        "appraising" => $"“Gas money, paid forward on your good name. Carry a small consignment for me and I'll mark the slate clean.”",
        "flighty" => $"“Lent you a little, didn't I? Fetch this along to where I say — quick now — and I'll forget you owe me.”",
        "gruff" => $"“I floated you the fuel. Haul this where I tell you, no fuss, and we're even.”",
        _ => $"“I covered your fuel. Run one quiet delivery for me and the favor's repaid.”",
    };
}
