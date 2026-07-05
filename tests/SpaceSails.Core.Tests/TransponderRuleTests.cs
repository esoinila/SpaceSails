namespace SpaceSails.Core.Tests;

/// <summary>
/// M29: the transponder — the AIS of the solar lanes. ON hands anyone in beacon range our
/// true state; DARK falls back to the optical eyes race; FAKE broadcasts the ghost of our
/// declared course, and the lie holds per observer until their own optics see the real hull.
/// </summary>
public class TransponderRuleTests
{
    private static SightAdvantage Optical(double distance, bool weSeeThem, bool theySeeUs) =>
        new(distance, weSeeThem ? distance : distance / 2, theySeeUs ? distance : distance / 2,
            weSeeThem, theySeeUs);

    [Fact]
    public void BeaconHeard_OnAndFakeCarry_DarkNever()
    {
        double inRange = TransponderRule.BeaconRangeMeters * 0.5;
        double outOfRange = TransponderRule.BeaconRangeMeters * 1.5;

        Assert.True(TransponderRule.BeaconHeard(TransponderMode.On, inRange));
        Assert.True(TransponderRule.BeaconHeard(TransponderMode.Fake, inRange));
        Assert.False(TransponderRule.BeaconHeard(TransponderMode.Dark, inRange));
        Assert.False(TransponderRule.BeaconHeard(TransponderMode.On, outOfRange));
    }

    [Fact]
    public void WithBeacon_LitBeaconWinsTheEyesRaceForThem_DarkChangesNothing()
    {
        SightAdvantage blindToUs = Optical(1e10, weSeeThem: true, theySeeUs: false);

        Assert.True(TransponderRule.WithBeacon(blindToUs, TransponderMode.On).TheySeeUs);
        Assert.True(TransponderRule.WithBeacon(blindToUs, TransponderMode.Fake).TheySeeUs);
        Assert.False(TransponderRule.WithBeacon(blindToUs, TransponderMode.Dark).TheySeeUs);
    }

    [Fact]
    public void PictureFor_TheLieHoldsPerObserver_UntilTheirOwnOpticsBeatIt()
    {
        // A far observer in beacon range, optically blind to us: believes the ghost.
        SightAdvantage far = Optical(1e10, weSeeThem: false, theySeeUs: false);
        Assert.Equal(BeaconPicture.Ghost, TransponderRule.PictureFor(TransponderMode.Fake, far));

        // A close observer whose optics resolve the real hull: the lie is BLOWN to them —
        // and only to them.
        SightAdvantage close = Optical(1e9, weSeeThem: true, theySeeUs: true);
        Assert.Equal(BeaconPicture.LieBlown, TransponderRule.PictureFor(TransponderMode.Fake, close));

        // Beyond the beacon band entirely: nothing, lie or no lie.
        SightAdvantage deep = Optical(TransponderRule.BeaconRangeMeters * 2, false, false);
        Assert.Equal(BeaconPicture.Nothing, TransponderRule.PictureFor(TransponderMode.Fake, deep));
    }

    [Fact]
    public void PictureFor_OnIsTrueContact_DarkIsOpticsOnly()
    {
        SightAdvantage far = Optical(1e10, weSeeThem: false, theySeeUs: false);
        Assert.Equal(BeaconPicture.TrueContact, TransponderRule.PictureFor(TransponderMode.On, far));
        Assert.Equal(BeaconPicture.Nothing, TransponderRule.PictureFor(TransponderMode.Dark, far));

        SightAdvantage seen = Optical(1e9, weSeeThem: false, theySeeUs: true);
        Assert.Equal(BeaconPicture.TrueContact, TransponderRule.PictureFor(TransponderMode.Dark, seen));
    }

    [Fact]
    public void Parrot_KnowsTheNewTricks_Deterministically()
    {
        Assert.Equal(Parrot.Line(Parrot.Squawk.RunningDark, 7), Parrot.Line(Parrot.Squawk.RunningDark, 7));
        Assert.Contains("colors", Parrot.Line(Parrot.Squawk.FalseColors, 0));
    }
}
