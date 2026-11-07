namespace SpaceSails.Core.Tests;

/// <summary>
/// R1-F40 · A chill is a crowd's line ("heads come up as one", "the room freezes as one"). A captain alone on
/// open regolith must never be handed one, whatever the chill roll said; every other setting still carries it.
/// </summary>
public class HullShudderChillVoiceTests
{
    [Fact]
    public void OnOpenRegolith_AWonChillRollFallsThroughToTheRegolithPool_EverySeed()
    {
        var chillPools = new HashSet<string>(
            HullShudder.ChillLinesFor(false).Concat(HullShudder.ChillLinesFor(true)));
        var regolith = HullShudder.LinesFor(HullShudder.Setting.Regolith, HullShudder.HavenRoom.Concourse);

        for (ulong seed = 1; seed <= 200; seed++)
        {
            for (int i = 0; i < 12; i++)
            {
                foreach (bool pressure in new[] { false, true })
                {
                    (string? line, bool chilled) = HullShudder.Voice(
                        HullShudder.Setting.Regolith, HullShudder.HavenRoom.Concourse, pressure, true, seed, i);
                    Assert.False(chilled);
                    Assert.NotNull(line);
                    Assert.DoesNotContain(line!, chillPools);
                    Assert.Contains(line!, regolith);
                }
            }
        }
    }

    [Fact]
    public void InASealedSite_AWonChillRollStillSpeaksTheChill()
    {
        for (ulong seed = 1; seed <= 50; seed++)
        {
            (string? line, bool chilled) = HullShudder.Voice(
                HullShudder.Setting.DeepSite, HullShudder.HavenRoom.Concourse, false, true, seed, 3);
            Assert.True(chilled);
            Assert.Contains(line!, HullShudder.ChillLinesFor(false));
        }
    }
}
