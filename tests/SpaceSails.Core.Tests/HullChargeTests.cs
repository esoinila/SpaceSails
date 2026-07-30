using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #523 · THE CHARGE BOARD, AS LAWS. Owner: <i>"I like you make a PR about how spaceships manage their electric
/// charge build up and make us a panel about it into the rear of the ship … Now that desk is kind of lame on the
/// ship, so let's add some kind of on automatic there."</i>
/// </summary>
public sealed class HullChargeTests
{
    /// <summary>
    /// THE THRESHOLD IS ONE NUMBER NOW. It was a client literal (<c>ArcChargeThreshold = 0.9</c> in Map.Sim)
    /// beside the thunder cue, which is this repo's own named bug class — four shipped bugs have come from rules
    /// and geometry kept where no test could reach them. The sim and the board must agree about when she is
    /// arcing, and only Core can be asked.
    /// </summary>
    [Fact]
    public void TheArcingBandStartsAtTheSimsOwnThreshold()
    {
        Assert.Equal(HullCharge.Band.Arcing, HullCharge.BandOf(HullCharge.ArcThreshold));
        Assert.Equal(HullCharge.Band.Arcing, HullCharge.BandOf(1.0));
        Assert.NotEqual(HullCharge.Band.Arcing, HullCharge.BandOf(HullCharge.ArcThreshold - 0.01));
    }

    [Theory]
    [InlineData(0.0, HullCharge.Band.Quiet)]
    [InlineData(0.19, HullCharge.Band.Quiet)]
    [InlineData(0.2, HullCharge.Band.Rising)]
    [InlineData(0.54, HullCharge.Band.Rising)]
    [InlineData(0.55, HullCharge.Band.Glowing)]
    [InlineData(0.89, HullCharge.Band.Glowing)]
    public void TheBandsRunInOneDirection(double charge, HullCharge.Band expected) =>
        Assert.Equal(expected, HullCharge.BandOf(charge));

    /// <summary>
    /// THE STEALTH TAX, QUOTED FROM THE SENSORS' OWN ARITHMETIC. A charged hull has been seen from three times as
    /// far since the Electric Universe layer shipped, and nothing aboard ever said so. The board must not be able
    /// to flatter the captain, so the number comes off <see cref="SensorModel.ChargeGlowFactor"/> rather than out
    /// of a copy of it.
    /// </summary>
    [Fact]
    public void TheBoardQuotesTheSameGlowTheSensorsUse()
    {
        Assert.Equal(1.0, HullCharge.SeenFartherFactor(0.0));
        Assert.Equal(1.0 + SensorModel.ChargeGlowFactor, HullCharge.SeenFartherFactor(1.0));

        // …and it never reads below a cold hull, whatever nonsense arrives.
        Assert.Equal(1.0, HullCharge.SeenFartherFactor(-5));
        Assert.Equal(1.0 + SensorModel.ChargeGlowFactor, HullCharge.SeenFartherFactor(5));
    }

    [Fact]
    public void AColdHullIsToldItIsCold()
    {
        Assert.Contains("Cold hull", HullCharge.VisibilityLine(0), StringComparison.Ordinal);
        Assert.Contains("farther", HullCharge.VisibilityLine(1.0), StringComparison.Ordinal);
    }

    /// <summary>What is driving her is a PLACE, not a figure — a stream, the inner halo, the cold dark.</summary>
    [Fact]
    public void TheBoardNamesWhereTheChargeIsComingFrom()
    {
        Assert.Contains("stream", HullCharge.DrivenByLine(1.0), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Inner system", HullCharge.DrivenByLine(0.7), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cold", HullCharge.DrivenByLine(0.02), StringComparison.OrdinalIgnoreCase);
    }

    // ── The automatic ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A CONTACTOR CLAMPS TO THE AMBIENT, NOT TO ZERO — which is the real device and also what keeps the switch
    /// honest: running it is cheaper than being seen, never free. If this ever became zero, the automatic would
    /// be a dominant strategy with no downside and the whole board would be decoration.
    /// </summary>
    [Fact]
    public void TheAutomaticHoldsHerLowButNeverAtNothing()
    {
        Assert.True(HullCharge.ContactorHoldsAt > 0.0);
        Assert.Equal(HullCharge.Band.Quiet, HullCharge.BandOf(HullCharge.ContactorHoldsAt));
    }

    /// <summary>And it costs expellant for as long as it runs, priced per minute so a long haul feels it.</summary>
    [Fact]
    public void TheAutomaticBurnsExpellantWhileItRuns()
    {
        Assert.True(HullCharge.ContactorPulsesPerMinute > 0);
        Assert.Equal(HullCharge.ContactorPulsesPerMinute, HullCharge.ContactorPulseCost(60.0), 6);
        Assert.Equal(0.0, HullCharge.ContactorPulseCost(0.0));
    }

    // ── The one it cannot help with ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// DEEP DIELECTRIC CHARGING OBEYS THE WRECK SENSOR'S LAW. Only the high-energy end of the environment buries
    /// anything, the hull gauge reads the same either way, and the contactor does not touch it — a real
    /// limitation of a real device, which is why it is worth having in a game whose whole voice is instruments
    /// that will not tell you what you want to know.
    /// </summary>
    [Fact]
    public void ColdSpaceBuriesNothingAndAStreamBuriesPlenty()
    {
        Assert.Equal(0.0, HullCharge.InternalSoakGain(ambient: 0.1, seconds: 600));
        Assert.Equal(0.0, HullCharge.InternalSoakGain(ambient: 0.5, seconds: 600));
        Assert.True(HullCharge.InternalSoakGain(ambient: 1.0, seconds: 600) > 0);

        // A stream buries it faster than middling space does, monotonically.
        Assert.True(HullCharge.InternalSoakGain(1.0, 60) > HullCharge.InternalSoakGain(0.7, 60));
    }

    [Fact]
    public void TheHintComesBeforeTheArcAndNeitherIsANumber()
    {
        Assert.False(HullCharge.InternalHintDue(0));
        Assert.True(HullCharge.InternalHintDue(HullCharge.InternalArcSeconds));
        Assert.False(HullCharge.InternalArcDue(HullCharge.InternalArcSeconds * 0.9));
        Assert.True(HullCharge.InternalArcDue(HullCharge.InternalArcSeconds));

        // The hint is a smell and a sound, not a gauge: no digits anywhere in it.
        Assert.DoesNotContain(HullCharge.InternalHintLine, char.IsDigit);
        Assert.Contains("ozone", HullCharge.InternalHintLine, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The board says out loud that it cannot see the thing that will hurt you — the same admission the
    /// wreck's life-sign read makes, and the reason either instrument can be trusted at all.</summary>
    [Fact]
    public void TheBoardAdmitsWhatItCannotRead()
    {
        Assert.Contains("HULL", HullCharge.WhatTheGaugeCannotSee, StringComparison.Ordinal);
        Assert.Contains("contactor", HullCharge.WhatTheGaugeCannotSee, StringComparison.OrdinalIgnoreCase);
    }
}
