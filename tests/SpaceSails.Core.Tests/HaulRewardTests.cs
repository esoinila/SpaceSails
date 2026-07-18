namespace SpaceSails.Core.Tests;

/// <summary>#349 — the honest contract purse. Pins the pure reward-scaling law (<see cref="HaulReward"/>)
/// against the owner's live playtest complaint (2026-07-18): a Uranus→Saturn parcel that paid a flat 300 cr,
/// "ridiculously little for such a long trip". The bands below are keyed to the real Sol-scenario
/// heliocentric orbit radii, so a regression that flattens the scale again fails here.</summary>
public class HaulRewardTests
{
    // The Sol scenario's heliocentric orbit radii (metres), straight from scenarios/sol.json — the
    // planet distances a station/moon inherits as its own "how far out" for reward purposes.
    private const double EarthM = 149_600_000_000.0;    // 1.00 AU
    private const double MarsM = 227_940_000_000.0;    // 1.52 AU
    private const double JupiterM = 778_570_000_000.0;    // 5.20 AU
    private const double SaturnM = 1_433_530_000_000.0;   // 9.58 AU
    private const double UranusM = 2_872_460_000_000.0;   // 19.20 AU
    private const double NeptuneM = 4_495_060_000_000.0;   // 30.05 AU

    // ---- The floor: a local hop stays near its old ~300 cr, never insulting ----

    [Fact]
    public void LocalHop_PaysNearTheBaseFloor()
    {
        // Origin and destination in the same neighbourhood (both ~1 AU — e.g. Earth station to a Luna berth,
        // a moon that rides Earth's heliocentric distance): no gap, minimal reach. Modest, as it should be.
        int reward = HaulReward.ForHaul(EarthM, EarthM);
        Assert.InRange(reward, 300, 520);
    }

    // ---- The complaint case: a cross-system haul must pay like one ----

    [Fact]
    public void UranusToSaturn_TheComplaintCase_PaysAHeartyCrossSystemPurse()
    {
        // The very run the owner screenshotted (The Tilt @ Uranus → Ringside Exchange @ Saturn), which the
        // old law flattened to 300 cr. The new law reads the void it actually crosses.
        int reward = HaulReward.ForHaul(UranusM, SaturnM);
        Assert.InRange(reward, 5_700, 6_050);
    }

    [Fact]
    public void MarsToUranus_TheTenYearRun_PaysTheMostOfAll()
    {
        // "It took like 10 years from Mars" — the widest inner-to-outer reach in the scenario.
        int reward = HaulReward.ForHaul(MarsM, UranusM);
        Assert.InRange(reward, 7_500, 7_800);
    }

    [Fact]
    public void CrossSystemHaul_PaysManyTimesALocalHop()
    {
        int local = HaulReward.ForHaul(MarsM, MarsM);
        int crossSystem = HaulReward.ForHaul(MarsM, UranusM);
        // The owner's whole point: the long trip must dwarf the hop next door.
        Assert.True(crossSystem > local * 10,
            $"cross-system {crossSystem} cr should dwarf local hop {local} cr");
    }

    // ---- Structural guarantees of the law ----

    [Fact]
    public void Reward_IsOrderFree_InboundEqualsOutbound()
    {
        // The void between two orbits is the same void whichever way you cross it.
        Assert.Equal(HaulReward.ForHaul(MarsM, UranusM), HaulReward.ForHaul(UranusM, MarsM));
        Assert.Equal(HaulReward.ForHaul(SaturnM, UranusM), HaulReward.ForHaul(UranusM, SaturnM));
    }

    [Fact]
    public void Reward_RisesMonotonically_WithHowFarOutTheRunReaches()
    {
        int toMars = HaulReward.ForHaul(EarthM, MarsM);
        int toSaturn = HaulReward.ForHaul(EarthM, SaturnM);
        int toUranus = HaulReward.ForHaul(EarthM, UranusM);
        int toNeptune = HaulReward.ForHaul(EarthM, NeptuneM);
        Assert.True(toMars < toSaturn, "Saturn run should out-pay a Mars run");
        Assert.True(toSaturn < toUranus, "Uranus run should out-pay a Saturn run");
        Assert.True(toUranus < toNeptune, "Neptune run should out-pay a Uranus run");
    }

    [Fact]
    public void Reward_IsBasePlusPremium_ByConstruction()
    {
        int premium = HaulReward.Premium(MarsM, JupiterM);
        Assert.Equal(HaulReward.BaseReward + premium, HaulReward.ForHaul(MarsM, JupiterM));
    }

    [Fact]
    public void ZeroRadiusEnd_IsMeasuredFromTheSun_NeverBelowBase()
    {
        // A zero radius means the Sun itself at that end, so the haul is measured from the sun out to the
        // other end: the gap (0 → 1 AU) AND the reach (1 AU) both count. Base + 220 + 180 per AU = 700.
        int reward = HaulReward.ForHaul(0.0, EarthM);
        Assert.True(reward >= HaulReward.BaseReward);
        Assert.InRange(reward, 690, 710);
    }

    [Fact]
    public void Premium_IsZeroForAPointBlankHaul()
    {
        // Same orbit radius at both ends and effectively at the sun: no gap, no reach — nothing to add.
        Assert.Equal(0, HaulReward.Premium(0.0, 0.0));
    }

    // ---- The signature-job overload (a bounty/favor keeps its authored floor, plus the haul premium) ----

    [Fact]
    public void WithFloor_NeverPaysBelowTheAuthoredFloor()
    {
        Assert.Equal(2_600, HaulReward.WithFloor(2_600, 0.0, 0.0));
        Assert.True(HaulReward.WithFloor(2_600, EarthM, EarthM) >= 2_600);
    }

    [Fact]
    public void WithFloor_AddsTheSameHaulPremiumOnTop()
    {
        int floor = 2_600;
        int expected = floor + HaulReward.Premium(MarsM, UranusM);
        Assert.Equal(expected, HaulReward.WithFloor(floor, MarsM, UranusM));
        // And a long haul lifts a fixed-fee job well above its floor.
        Assert.True(HaulReward.WithFloor(floor, MarsM, UranusM) > floor + 5_000);
    }
}
