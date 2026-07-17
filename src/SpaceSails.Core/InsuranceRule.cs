namespace SpaceSails.Core;

/// <summary>
/// PR-BUSTED · Rebirth taxes &amp; the insurance seam (owner, mid-flight, issues #227 + #225): the
/// brain-backup resurrection is not free — the wake-up at the clinic books a visible bill — and the
/// insurance rustbucket is only the UNINSURED default. This is the seam, not the system: a small,
/// plain, JSON-friendly policy record lives on game state, and the resurrection path CONSULTS it
/// through one pure function. Ship <see cref="InsuranceTier.None"/> behaviour is what the game holds
/// today; the vendor / premium / claims flow is issue #227's own lane later, and it never has to
/// reopen the catch code — it just starts selling policies this rule already reads.
/// </summary>
public static class InsuranceRule
{
    /// <summary>The brain-backup wake-up is not free (issue #227): the clinic bills this for a
    /// standard uninsured resurrection. Booked against the purse — banked/buried funds are the payer
    /// of last resort conceptually, but that reconciliation is the WIRE lane's bank at merge; this
    /// lane records the bill against the purse floor.</summary>
    public const int BaseClinicBillCr = 500;

    /// <summary>The uninsured default rebirth: <see cref="BustedRule.Resurrect"/>'s rustbucket plus the
    /// full clinic bill. The tank comes up at the caller's mercy floor.</summary>
    public static RebirthOutcome DefaultRebirth(int mercyFloorPulses) =>
        new(BustedRule.Resurrect(mercyFloorPulses), BaseClinicBillCr,
            "an uninsured insurance rustbucket — starter-grade, everything visible aboard gone");

    /// <summary>
    /// Consult a policy at rebirth. An <see cref="PirateInsurance.IsActiveAt"/> policy can cut the
    /// clinic bill and/or hand a better hull; <see cref="InsuranceTier.None"/> (or a lapsed premium)
    /// returns <paramref name="defaultOutcome"/> untouched — the rustbucket stands. Pure and total, so
    /// the catch code calls it unconditionally today and issue #227 only has to make policies real.
    /// </summary>
    public static RebirthOutcome ApplyToRebirth(PirateInsurance policy, double simTime, RebirthOutcome defaultOutcome)
    {
        if (!policy.IsActiveAt(simTime))
        {
            return defaultOutcome; // uninsured / lapsed — no claim to make
        }

        return policy.Tier switch
        {
            // Stub better tiers (proved by tests): a covered claim eases the wake-up. The premium/claims
            // economy that would SELL these lives in #227 — this rule already honours them.
            InsuranceTier.Basic => defaultOutcome with
            {
                ClinicBillCr = defaultOutcome.ClinicBillCr / 2,
                HullDescription = "a part-covered rustbucket — the clinic bill halved",
            },
            InsuranceTier.Premium => defaultOutcome with
            {
                ClinicBillCr = 0,
                Kit = defaultOutcome.Kit with
                {
                    MassLevel = 1,
                    ReactionMassPulses = Math.Max(defaultOutcome.Kit.ReactionMassPulses, 150),
                },
                HullDescription = "a covered mid-grade replacement hull — no clinic bill",
            },
            _ => defaultOutcome,
        };
    }
}

/// <summary>The player's standing pirate-insurance policy — a small, saveable thing of personal value
/// (issue #225's vault persists exactly these). Plain types, no cycles: a tier and the sim-time the
/// premium is paid through.</summary>
public readonly record struct PirateInsurance(InsuranceTier Tier, double PremiumPaidThroughSimTime)
{
    /// <summary>The default every captain starts with: no policy, the rustbucket on a bust.</summary>
    public static readonly PirateInsurance Uninsured = new(InsuranceTier.None, double.NegativeInfinity);

    /// <summary>A policy pays out only if it is a real tier AND the premium is still in force at the
    /// moment of the bust.</summary>
    public bool IsActiveAt(double simTime) =>
        Tier != InsuranceTier.None && simTime <= PremiumPaidThroughSimTime;
}

/// <summary>Pirate-insurance tiers. <see cref="None"/> is the uninsured default the game ships;
/// higher tiers are the seam issue #227 will sell.</summary>
public enum InsuranceTier
{
    None = 0,
    Basic = 1,
    Premium = 2,
}

/// <summary>What a rebirth yields after the policy is consulted: the starter-grade kit, the clinic
/// bill booked against the purse, and a one-line hull description for the wake card. Plain data —
/// JSON-friendly for the save vault.</summary>
public readonly record struct RebirthOutcome(
    BustedRule.ResurrectionKit Kit,
    int ClinicBillCr,
    string HullDescription);
