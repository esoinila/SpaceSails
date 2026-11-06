using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Beat 8 (#1406) · R1-F15, R1-F17, R1-F48, R1-F51. Four places where the head office, or any floor under the
/// ground, was handed the surface's or the Hive's furniture.
///
/// <para><b>RED</b> (each by reverting its one hunk): F15 - <c>MaybeRaiseHeadOfficeBeat</c> writes
/// <c>_viewObject</c> directly again (the FORTY-ONE card is no longer among the cards of the arrival);
/// F17 - the head-office guard in <c>ComposeSecretLabSite</c> (a forceable HIDDEN DOOR beside the shed);
/// F48 - <c>onRegolith</c> loses its <c>Floor: &gt;= 0</c> (the regolith horizon is spoken under the ground);
/// F51 - the <c>ex.Floor &gt;= 0</c> in the tracker captions (the hidden-door prompt listed at B20).</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHeadOfficeIsNotTheHivesSecretLabTests
{
    private const string StraightToB23 = "/map?kaamos=hq&arrivalphase=2&land=1&floor=23";
    private const string TheShed = "/map?kaamos=hq&land=1";
    private const string DeepHive = "/map?dock=the-tilt&site=0&secretlab=deep&land=1&floor=20&air=90";

    private static IEnumerable<string> CardsOfTheArrival(DeskBench bench)
    {
        if (bench.Peek("_viewObject") is DeckPlan.ConsoleSpot up)
        {
            yield return up.Label;
        }
        foreach (DeckPlan.ConsoleSpot waiting in (IEnumerable<DeckPlan.ConsoleSpot>)bench.Peek("_viewObjectWaiting")!)
        {
            yield return waiting.Label;
        }
    }

    private static List<string> Captions(DeskBench bench) =>
        (List<string>)bench.Call("BuildTrackerCaptions", bench.Peek("_surface")!, 0)!;

    [Fact]
    public async Task ABootStraightToB23StillShowsTheFortyOneCardAmongTheArrivalsCards()
    {
        using DeskBench bench = await DeskBench.BootAsync(StraightToB23);
        Assert.True(bench.OnSurface);

        string[] cards = [.. CardsOfTheArrival(bench)];
        // No card is lost to another: the establishing card, the hall's, and the air's, in the order raised.
        Assert.Equal(UndergroundComplex.FirstDescentCard(KaamosLore.IceMoonBodyId).Label, cards[0]);
        Assert.Contains(UndergroundComplex.WinteringHallLabel, cards);
        Assert.Equal(3, cards.Length);
    }

    [Fact]
    public async Task TheHeadOfficeShedHasNoForceableHiddenDoor()
    {
        using DeskBench bench = await DeskBench.BootAsync(TheShed);
        var plan = (DeckPlan)bench.Peek("_deckPlan")!;

        Assert.Contains(plan.Consoles, c => c.Kind == DeckPlan.ConsoleKind.HiveHead);
        Assert.DoesNotContain(plan.Consoles, c => c.Kind == DeckPlan.ConsoleKind.SecretDoor);
        Assert.DoesNotContain(Captions(bench), c => c.Contains("HIDDEN DOOR", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheHiddenDoorPromptIsNotListedUnderTheGround()
    {
        // The Hive's own door is still advertised on the regolith…
        using DeskBench onTop = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&secretlab=deep&land=1");
        Assert.Contains(Captions(onTop), c => c.Contains("HIDDEN DOOR", StringComparison.Ordinal));

        // …and stops being advertised the moment the captain is inside the building, in either building.
        foreach (string url in new[] { DeepHive, StraightToB23 })
        {
            using DeskBench below = await DeskBench.BootAsync(url);
            Assert.DoesNotContain(Captions(below), c => c.Contains("HIDDEN DOOR", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task NoTremorUnderTheGroundSpeaksTheSurfacesHorizon()
    {
        using DeskBench bench = await DeskBench.BootAsync(StraightToB23);
        bench.Poke("_viewObject", null);

        var said = new HashSet<string>();
        for (int i = 0; i < 120; i++)
        {
            bench.Poke("_shudderSeed", (ulong)(1000 + (i * 7919)));
            bench.Poke("_shudderIndex", i);
            bench.Poke("_pulse", null);
            bench.Call("FireShudder", 0.0);
            said.Add(bench.Pulse);
        }

        Assert.True(said.Count > 3, "the sweep must have heard several different lines");
        Assert.DoesNotContain(said, s => s.Contains("horizon", StringComparison.OrdinalIgnoreCase)
            || s.Contains("regolith", StringComparison.OrdinalIgnoreCase));
    }
}
