using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1213 / #1216 · <b>THE BOOT, ALL THE WAY TO THE START POINT, WITH NO RENDERER UNDER IT.</b>
///
/// <para><see cref="TheBootBuildsTheSameWorldTests"/> stops at the browser gate and hashes what it has — its
/// own docblock calls that "the fingerprint's horizon" — and the four stages that run BEHIND the gate
/// (<c>ApplyTheStartPoint</c> and the three cheat seeders) are therefore outside every guard it makes. Both
/// bugs this bench exists for live exactly there: <c>?start=</c> resolved against a scenario that has no such
/// body threw <c>KeyNotFoundException</c> onto the red error page, and <c>?simhours=</c> moved the clock four
/// stages after the berth had already frozen its watch. Neither is visible one line earlier.</para>
///
/// <para><see cref="DeskBench"/> already crosses that gate, and it is the right tool when the question is
/// what the page DRAWS. This one is for when the question is what the page IS: no
/// <c>Microsoft.AspNetCore.Components.RenderTree</c>, no root component, no render batches — just the
/// shipping <see cref="Map"/> booted at a URL, walked past the gate through the boot's OWN stage list, with
/// <b>every exception those stages raise handed back rather than swallowed</b>. A bench that ate them would
/// be this repository's fifth named bug class: a guard against a crash that cannot see the crash.</para>
/// </summary>
internal static class PastTheGateBench
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The stages of <c>BootTheWorldAsync</c> that run behind the browser gate, in the boot's own
    /// order. Named rather than re-implemented — a stage that is renamed or removed throws here by name
    /// instead of quietly leaving this bench booting a smaller world than the game does.</summary>
    private static readonly string[] TheStagesBehindTheGate =
    [
        "ApplyTheStartPoint",
        "StandTheCaptainWhereTheCheatsAsk",
        "SeedTheArcsAndTheJobs",
        "SeedTheApproachesAndThePurse",
    ];

    /// <summary>What one boot at one URL came to: the page, and whatever the stages behind the gate threw.
    /// <see cref="Threw"/> is null for a boot that ran all the way through, which is what every URL the game
    /// documents is supposed to do.</summary>
    internal sealed record Boot(Map Page, Exception? Threw)
    {
        internal object? Field(string name) => typeof(Map).GetField(name, Hidden)!.GetValue(Page);

        internal T Read<T>(string name) => (T)Field(name)!;
    }

    /// <summary>Boot the shipping page at <paramref name="url"/> and walk it past the gate.</summary>
    internal static async Task<Boot> BootAsync(string url)
    {
        var page = new Map();
        TheBootBuildsTheSameWorldTests.NeverRender(page);
        System.Net.Http.HttpClient http = TheBootBuildsTheSameWorldTests.ScenariosFromDisk();
        var navigation = new TheBootBuildsTheSameWorldTests.Bench(url);
        TheBootBuildsTheSameWorldTests.Hand(page, "Http", http);
        TheBootBuildsTheSameWorldTests.Hand(page, "Navigation", navigation);

        Exception? beforeTheGate = null;
        try
        {
            await (Task)Method("BootTheWorldAsync").Invoke(page, [CancellationToken.None])!;
        }
        catch (TargetInvocationException ex)
        {
            beforeTheGate = ex.InnerException ?? ex;
        }
        catch (Exception ex)
        {
            beforeTheGate = ex;
        }

        // The boot always ends in a throw off a browser (JSHost.ImportAsync, the documented horizon). A throw
        // that left NO ephemeris behind is a different thing entirely — a world that was never built — and it
        // is handed back rather than walked past, because running the start stages over a null sky would
        // report the wrong crash.
        if (typeof(Map).GetField("_ephemeris", Hidden)!.GetValue(page) is null)
        {
            return new Boot(page, beforeTheGate ?? new InvalidOperationException(
                $"{url}: the boot stopped before it built an ephemeris, and said nothing about why."));
        }

        object query = Method("ReadEveryQueryKey").Invoke(page, [new Uri(navigation.Uri)])!;
        Method("DefaultABerthForTheCheatsThatNeedOne").Invoke(page, [query]);
        typeof(Map).GetField("_worldReady", Hidden)!.SetValue(page, true);

        foreach (string stage in TheStagesBehindTheGate)
        {
            try
            {
                Method(stage).Invoke(page, [query]);
            }
            catch (TargetInvocationException ex)
            {
                return new Boot(page, ex.InnerException ?? ex);
            }
        }

        return new Boot(page, null);
    }

    /// <summary>Run <paramref name="frames"/> frames of the docked bar's own metabolism — the same call the
    /// walked frame makes, at the same fixed step, so what happens here is what happens ashore.</summary>
    internal static void StepTheBar(Map page, int frames, double dtSeconds = 1.0 / 60.0)
    {
        MethodInfo advance = typeof(Map).GetMethod("AdvanceBarWalkers", Hidden)
            ?? throw new InvalidOperationException("Map has no AdvanceBarWalkers to step.");
        for (int frame = 0; frame < frames; frame++)
        {
            advance.Invoke(page, [dtSeconds]);
        }
    }

    /// <summary>Everybody on their feet in the docked bar, as the page holds them.</summary>
    internal static IReadOnlyList<Map.Walker> Afoot(Map page) =>
        (IReadOnlyList<Map.Walker>)typeof(Map).GetField("_barAfoot", Hidden)!.GetValue(page)!;

    private static MethodInfo Method(string name) =>
        typeof(Map).GetMethod(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no {name} — the boot's stages have moved.");
}
