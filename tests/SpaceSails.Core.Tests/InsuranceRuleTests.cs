namespace SpaceSails.Core.Tests;

/// <summary>PR-BUSTED · Rebirth taxes &amp; the insurance seam (issues #227 + #225). The brain-backup
/// wake-up books a clinic bill; the uninsured default is the rustbucket; a covered tier eases it —
/// all through one pure rule the catch code consults today so #227 never reopens it.</summary>
public class InsuranceRuleTests
{
    [Fact]
    public void DefaultRebirth_IsTheUninsuredRustbucket_WithTheFullClinicBill()
    {
        RebirthOutcome o = InsuranceRule.DefaultRebirth(mercyFloorPulses: 40);

        Assert.Equal(InsuranceRule.BaseClinicBillCr, o.ClinicBillCr);   // the rebirth tax
        Assert.Equal(BustedRule.InsuranceCredits, o.Kit.Credits);
        Assert.Equal(40, o.Kit.ReactionMassPulses);
        Assert.Equal(0, o.Kit.MassLevel);                                // starter grade
        Assert.Contains("rustbucket", o.HullDescription);
    }

    [Fact]
    public void NoneTier_PassesTheDefaultThrough_Untouched()
    {
        RebirthOutcome def = InsuranceRule.DefaultRebirth(40);
        RebirthOutcome got = InsuranceRule.ApplyToRebirth(PirateInsurance.Uninsured, simTime: 1000, def);

        Assert.Equal(def, got); // the rustbucket stands — the seam is inert until #227 sells a policy
    }

    [Fact]
    public void LapsedPolicy_ReadsAsUninsured()
    {
        var lapsed = new PirateInsurance(InsuranceTier.Premium, PremiumPaidThroughSimTime: 500);
        RebirthOutcome def = InsuranceRule.DefaultRebirth(40);

        // Busted AFTER the premium ran out — no claim.
        RebirthOutcome got = InsuranceRule.ApplyToRebirth(lapsed, simTime: 900, def);
        Assert.Equal(def, got);
        Assert.False(lapsed.IsActiveAt(900));
        Assert.True(lapsed.IsActiveAt(400));
    }

    [Fact]
    public void BasicTier_HalvesTheClinicBill_WhenActive()
    {
        var basic = new PirateInsurance(InsuranceTier.Basic, PremiumPaidThroughSimTime: 10_000);
        RebirthOutcome got = InsuranceRule.ApplyToRebirth(basic, simTime: 1000, InsuranceRule.DefaultRebirth(40));

        Assert.Equal(InsuranceRule.BaseClinicBillCr / 2, got.ClinicBillCr);
        Assert.Equal(0, got.Kit.MassLevel); // still a rustbucket, just a lighter bill
    }

    [Fact]
    public void PremiumTier_ClearsTheBill_AndHandsABetterHull()
    {
        var premium = new PirateInsurance(InsuranceTier.Premium, PremiumPaidThroughSimTime: 10_000);
        RebirthOutcome got = InsuranceRule.ApplyToRebirth(premium, simTime: 1000, InsuranceRule.DefaultRebirth(40));

        Assert.Equal(0, got.ClinicBillCr);                 // covered
        Assert.Equal(1, got.Kit.MassLevel);                // a better hull
        Assert.True(got.Kit.ReactionMassPulses >= 150);
        Assert.Contains("covered", got.HullDescription);
    }

    [Fact]
    public void Policy_IsPlainAndSaveable()
    {
        // Simple types only (enum + double) — the #225 vault persists exactly these.
        var p = new PirateInsurance(InsuranceTier.Basic, 12345.0);
        Assert.Equal(InsuranceTier.Basic, p.Tier);
        Assert.Equal(12345.0, p.PremiumPaidThroughSimTime);
    }
}
