namespace SpaceSails.Core.Tests;

/// <summary>
/// #456 · The ear. Owner, live 2026-07-27, on why an un-leashed pack (#453) is still fair: <i>"the reevers
/// do not have perfect knowledge that you are there in the first place… they can hear digging etc loud
/// noises, but generally they have to spot you by hearing or by seeing before they give chase. So when they
/// are initially behind obstructions except maybe one or two they do not participate in chasing you if they
/// don't know where you are. That should balance it."</i>
/// </summary>
public class ReeverHearingTests
{
    [Fact]
    public void TheLoudnessLadder_RunsFromBootsToGunfire()
    {
        // The ladder IS the balance dial, so its ORDER is the law worth pinning: moving is nearly free,
        // digging is an announcement, and your own guns are the loudest thing you can do.
        Assert.True(ReeverHearing.RangeOf(ReeverHearing.Noise.Footfall)
            < ReeverHearing.RangeOf(ReeverHearing.Noise.Clatter));
        Assert.True(ReeverHearing.RangeOf(ReeverHearing.Noise.Clatter)
            < ReeverHearing.RangeOf(ReeverHearing.Noise.Digging));
        Assert.True(ReeverHearing.RangeOf(ReeverHearing.Noise.Digging)
            < ReeverHearing.RangeOf(ReeverHearing.Noise.Gunfire));
    }

    [Fact]
    public void WalkingIsNearlySilent_SoSneakingDeepIsARealOption()
    {
        // If boots carried, the whole "venture deeper quietly" play would collapse — you could never cross
        // the field without waking it. A footfall reaches barely past arm's length.
        Assert.False(ReeverHearing.Hears(6, ReeverHearing.Noise.Footfall));
        Assert.True(ReeverHearing.Hears(2, ReeverHearing.Noise.Footfall));
    }

    [Fact]
    public void ADigCallsTheGroundAroundIt_ButNotTheWholeField()
    {
        // The signature trade of the surface: the thing worth doing is the thing that announces you. It has
        // to reach far enough to matter and stop well short of waking every Old One on the moon.
        Assert.True(ReeverHearing.Hears(15, ReeverHearing.Noise.Digging));
        Assert.False(ReeverHearing.Hears(40, ReeverHearing.Noise.Digging));
    }

    [Fact]
    public void GunfireCarriesFurthestOfAll_SoBotsBuyTimeAtThePriceOfBeingFound()
    {
        // #314's law, finally paid for: sentries hold the line, and the holding is what gives you away.
        Assert.True(ReeverHearing.Hears(30, ReeverHearing.Noise.Gunfire));
        Assert.True(ReeverHearing.RangeOf(ReeverHearing.Noise.Gunfire) > 25,
            "a volley must reach a good part of the field, or bringing bots costs nothing");
    }

    [Fact]
    public void HearingIsNotFooledByDistanceEdgeCases()
    {
        Assert.True(ReeverHearing.Hears(0, ReeverHearing.Noise.Digging));   // right on top of it
        Assert.False(ReeverHearing.Hears(double.NaN, ReeverHearing.Noise.Gunfire));
        Assert.False(ReeverHearing.Hears(double.PositiveInfinity, ReeverHearing.Noise.Gunfire));
        // Exactly at the rim still counts — a boundary that swallows sound would be a silent surprise.
        Assert.True(ReeverHearing.Hears(ReeverHearing.RangeOf(ReeverHearing.Noise.Digging),
            ReeverHearing.Noise.Digging));
    }

    [Fact]
    public void SoundIsDeliberatelyNotWallAware_StoneHidesYouFromEyesOnly()
    {
        // The whole point of the ear: the maze made the deep entirely safe once sight was blocked (#324).
        // Hearing takes no walls at all — there is no overload that accepts them — so a captain digging
        // behind a monolith is invisible and perfectly audible at the same time.
        System.Reflection.MethodInfo[] hears = [.. typeof(ReeverHearing).GetMethods()
            .Where(m => m.Name == nameof(ReeverHearing.Hears))];
        Assert.Single(hears);
        Assert.DoesNotContain(hears[0].GetParameters(),
            p => p.ParameterType.ToString().Contains("Segment", System.StringComparison.Ordinal));
    }
}
