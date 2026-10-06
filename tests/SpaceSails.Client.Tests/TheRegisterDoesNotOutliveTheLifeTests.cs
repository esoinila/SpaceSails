using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1372 · The #615 tag register (<c>_roomsTurnedOver</c>) rides the vault, and a New voyage in the same browser
/// session must not carry the old life's tags into the new one: not in memory, and not into the fresh thread's first
/// save (which would make the leak permanent there). <c>ResetLiveStateForNewGame</c> is "the exact inverse of BuildVault".
/// </summary>
public sealed class TheRegisterDoesNotOutliveTheLifeTests
{
    private static HashSet<string> Register(Pages.Map map) => (HashSet<string>)Read(map, "_roomsTurnedOver")!;

    /// <summary>
    /// <b>A TAG PLANTED IN THE OLD LIFE IS GONE AFTER A NEW VOYAGE, AND THE NEW THREAD'S FIRST VAULT CARRIES NONE OF IT.</b>
    /// A room tag, a claim tag and the unease latch are planted; the new-voyage reset (the register-touching half of EnterNewGameThread) runs; the register is
    /// empty and a vault built straight after has no TurnedOver section at all.
    ///
    /// <para><b>Proven RED</b> on the unfixed code (the clear absent from <c>ResetLiveStateForNewGame</c>): the
    /// register still held all three tags and the first vault wrote them.</para>
    /// </summary>
    [Fact]
    public void ANewVoyageStartsWithAnEmptyRegisterAndItsFirstVaultCarriesNoOldTag()
    {
        Pages.Map map = Boot("register-new-voyage");
        string[] planted =
        [
            KeepOrLeave.RoomKey("earth", 0, 1),
            ClaimInterview.SettledTag(12345L),
            ClaimInterview.UneaseTag,
        ];
        foreach (string tag in planted)
        {
            Register(map).Add(tag);
        }

        var before = (Vault)Invoke(map, "BuildVault", "", "")!;
        Assert.Equal(planted.Length, before.TurnedOver!.Rooms.Count); // the control: the old life saves them

        Invoke(map, "ResetLiveStateForNewGame"); // EnterNewGameThread = this + BeginNewGameThread (mints a thread id, JS interop: unbootable under test)

        Assert.Empty(Register(map));
        var after = (Vault)Invoke(map, "BuildVault", "", "")!;
        Assert.Null(after.TurnedOver);
        Assert.True(planted.All(t => !(after.TurnedOver?.Rooms ?? []).Contains(t)));
    }
}
