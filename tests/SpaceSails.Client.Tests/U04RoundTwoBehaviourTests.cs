using System.Collections;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1455 · The behavioural twins of U04-F05 and U04-F19, which <c>U04RoundTwoClientTests</c> pins only by reading
/// source. These stand up a shipping <c>Map</c> and drive the real methods.
/// <b>Proven RED</b> (F19) by dropping the <c>_hunters</c> clause from NpcName; (F05) by raising the
/// FirstShotFired beat ahead of the magazine/mass refusals in CheckLockedFire. (The ArmFire half stays a source scan
/// in <c>U04RoundTwoClientTests</c>: arming needs a live target.)
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class U04RoundTwoBehaviourTests
{
    /// <summary>U04-F19 · a hired hunter is listed by her callsign, never by her raw id.</summary>
    [Fact]
    public void ARoundAtAHunterIsListedByHerCallsign_OnTheShippingPage()
    {
        Pages.Map map = Boot("u04-f19");
        var hunters = (IList)Read(map, "_hunters")!;
        hunters.Add(new HunterState("hunter-0", "Brass Lantern", "luna", 0, 0, default, false, false));

        Assert.Equal("Brass Lantern", (string)Invoke(map, "NpcName", "hunter-0")!);
        Assert.Equal("nobody-7", (string)Invoke(map, "NpcName", "nobody-7")!);   // an unknown id still falls back
    }

    /// <summary>U04-F05 · a locked shot that is SCRUBBED (no mass, or an empty magazine) leaves the OnceEver
    /// first-shot plate unspent.</summary>
    [Theory]
    [InlineData("mass")]
    [InlineData("magazine")]
    public void AScrubbedShotDoesNotSpendTheFirstRoundPlate(string reason)
    {
        Pages.Map map = Boot("u04-f05-" + reason);
        Set(map, "_fireSolution", new FireControl.Solution(
            new Vector2d(1, 0), 0.0, 1000.0, 600.0, 0.0, 60.0, true, []));
        Set(map, "_fireAtSimTime", 0.0);                 // locked, and the moment has come
        if (reason == "mass")
        {
            Set(map, "_reactionMassPulses", 0);
        }
        else
        {
            Set(map, "_slugAmmo", 0);
        }

        Invoke(map, "CheckLockedFire");

        var spoken = (IDictionary)Read(map, "_beatsSpoken")!;
        foreach (DictionaryEntry e in spoken)
        {
            Assert.DoesNotContain("FirstShotFired", e.Key.ToString(), StringComparison.Ordinal);
        }
        Assert.Empty(spoken);   // control for the sweep above: nothing at all was filed as spoken
        Assert.Null(Read(map, "_fireSolution"));   // and the solution was scrubbed, not left half-fired
    }
}
