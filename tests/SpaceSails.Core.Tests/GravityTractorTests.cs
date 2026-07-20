namespace SpaceSails.Core.Tests;

/// <summary>
/// #395 — LAB 37, THE SLOW HAND. Pins the gravity-tractor Core math the lab certifies: the feeble tow
/// a = G·m_ship/d² (independent of the rock's mass), the continuous-tow miss = 1.5·a·T², the headline
/// lead-time-to-clear-Ringside (years, not days — the early-detection lesson), the √mass scaling that
/// says the slow hand can't be muscled, and the hover thrust that ties it to Lab 25's station-keeping.
/// </summary>
public class GravityTractorTests
{
    private static readonly double SafeMiss = DeflectionGig.SafeMissMeters;
    private const double MShip = GravityTractor.ReferenceShipMassKg;
    private const double Year = 365.25 * 86400.0;

    [Fact]
    public void TugAcceleration_IsGmOverDSquared_AndIndependentOfRockMass()
    {
        double d = GravityTractor.Standoff(140.0);
        Assert.Equal(GravityTractor.G * MShip / (d * d), GravityTractor.TugAcceleration(MShip, d), 1e-24);
        Assert.Equal(0.0, GravityTractor.TugAcceleration(MShip, 0.0)); // guarded
    }

    [Fact]
    public void Miss_IsContinuousTowLeverageTimesATSquared()
    {
        Assert.Equal(1.5, GravityTractor.ContinuousTowLeverage, 1e-12);
        double a = 1.0e-10;
        Assert.Equal(1.5 * a * Year * Year, GravityTractor.Miss(a, Year), 1e-6);
    }

    [Fact]
    public void RequiredLeadSeconds_InvertsMiss()
    {
        double a = GravityTractor.TugAcceleration(MShip, GravityTractor.Standoff(140.0));
        double t = GravityTractor.RequiredLeadSeconds(a, SafeMiss);
        Assert.Equal(SafeMiss, GravityTractor.Miss(a, t), 1.0);
        Assert.True(double.IsPositiveInfinity(GravityTractor.RequiredLeadSeconds(0.0, SafeMiss)));
    }

    // ── The headline: years of warning, growing with rock size ──

    [Fact]
    public void ReferenceTug_Clears140mRock_InAboutElevenAndAHalfYears()
    {
        double lead = GravityTractor.RequiredLeadSeconds(new RockType(RockComposition.SType), 140.0, MShip, SafeMiss);
        Assert.Equal(11.52, lead / Year, 0.1); // README section B headline
    }

    [Fact]
    public void BiggerRock_NeedsLongerWait_BecauseStandoffGrows()
    {
        var s = new RockType(RockComposition.SType);
        double small = GravityTractor.RequiredLeadSeconds(s, 50.0, MShip, SafeMiss);
        double big = GravityTractor.RequiredLeadSeconds(s, 1000.0, MShip, SafeMiss);
        Assert.True(big > small, "a bigger rock forces a bigger standoff, a weaker pull, a longer wait");
        // The wait is measured in YEARS — the whole early-detection lesson.
        Assert.True(small / Year > 1.0, "even a 50 m rock needs over a year of towing");
    }

    [Fact]
    public void TractorTow_IsIndependentOfRockMass_OnlyStandoffMatters()
    {
        // A C-type and an M-type of the SAME radius take the SAME lead: the tug pulls on its own mass.
        double c = GravityTractor.RequiredLeadSeconds(new(RockComposition.CType), 140.0, MShip, SafeMiss);
        double m = GravityTractor.RequiredLeadSeconds(new(RockComposition.MType), 140.0, MShip, SafeMiss);
        Assert.Equal(c, m, 1e-6);
    }

    [Fact]
    public void HeavierTug_HelpsOnlyAsSqrtMass()
    {
        double d = GravityTractor.Standoff(140.0);
        double lead1 = GravityTractor.RequiredLeadSeconds(GravityTractor.TugAcceleration(1.0e5, d), SafeMiss);
        double lead100 = GravityTractor.RequiredLeadSeconds(GravityTractor.TugAcceleration(1.0e7, d), SafeMiss);
        // 100× the tug mass cuts the wait by √100 = 10×, not 100× — the slow hand can't be muscled.
        Assert.Equal(10.0, lead1 / lead100, 0.01);
    }

    // ── The hover bill: the Lab-25 station-keeping tie ──

    [Fact]
    public void HoverThrust_CancelsTheRocksPull_AndScalesWithRockMass()
    {
        var s = new RockType(RockComposition.SType);
        double d = GravityTractor.Standoff(140.0);
        double mRock = KineticImpactor.AsteroidMass(s, 140.0);
        Assert.Equal(MShip * GravityTractor.G * mRock / (d * d), GravityTractor.HoverThrust(MShip, mRock, d), 1e-9);
        // A denser M-type of the same size pulls harder → more hover thrust than the C-type.
        double cThrust = GravityTractor.HoverThrust(MShip, KineticImpactor.AsteroidMass(new(RockComposition.CType), 140.0), d);
        double mThrust = GravityTractor.HoverThrust(MShip, KineticImpactor.AsteroidMass(new(RockComposition.MType), 140.0), d);
        Assert.True(mThrust > cThrust);
    }
}
