using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using SpaceSails.Client.Pages;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1407 beat 9 · R1-F22 / F59 / F60 / F61. Each law was watched going RED with its fix reverted:
/// F22 flash seeded on the first rebirth, F59 the second canvas drawn, F60 the bubble kept, F61 the `-` burn
/// reaching the sim under the card.
/// </summary>
public sealed class TheDeathCardKeepsItsWordsTests
{
    private const string AnImpact = "/map?death=impact&start=earth";

    [Fact]
    public void TheRebirthThatGathersTheShardFlashesTheLineTheLedgerWillQuote()
    {
        string shard = NebulaLore.ById("rebirth-glitch")!.Lore;
        for (ulong seed = 0; seed < 64; seed++)
        {
            string flash = Map.NebulaGlitchFlash(seed, shardJustGathered: true);
            Assert.Contains(flash, shard, StringComparison.Ordinal);
            Assert.Contains("PATTERN 40", flash, StringComparison.Ordinal);
        }

        // …and later rebirths still vary, (the canonical line and variants 1-2 carry PATTERN 40; 3-4 are other tells and are not asserted).
        var seen = new System.Collections.Generic.HashSet<string>();
        for (ulong seed = 0; seed < 64; seed++)
        {
            seen.Add(Map.NebulaGlitchFlash(seed));
        }
        Assert.True(seen.Count > 1);
    }

    [Fact]
    public async Task TheFirstWakeOnTheCardIsTheLineInTheShard()
    {
        using DeskBench bench = await DeskBench.BootAsync(AnImpact);
        await bench.RenderAsync();
        object busted = bench.Peek("_busted")!;
        // seed % 4 != 0, so the unfixed seeded pick lands on a variant and not on the canonical line.
        busted.GetType().GetProperty("Seed")!.SetValue(busted, 1UL);
        bench.CallOnTheDispatcher("BustedResurrect");
        string flash = (string)busted.GetType().GetProperty("RebirthGlitch")!.GetValue(busted)!;
        Assert.Contains(flash, NebulaLore.ById("rebirth-glitch")!.Lore, StringComparison.Ordinal);
    }

    [Fact]
    public void AnImpactOrAScuttlingDoesNotPaintTheFireballTwiceOnTheWakeCard()
    {
        Assert.False(DeathNarration.WakeCardRepeatsTheFireball(DeathCause.Collector, DeathPlace.OwnShip)
            && DeathNarration.ArtFile(DeathCause.Collector, DeathPlace.OwnShip) != "busted-ship-explosion.jpg");
        foreach (DeathCause c in Enum.GetValues<DeathCause>())
        {
            foreach (DeathPlace p in Enum.GetValues<DeathPlace>())
            {
                Assert.Equal(
                    DeathNarration.ArtFile(c, p) == "busted-ship-explosion.jpg",
                    DeathNarration.WakeCardRepeatsTheFireball(c, p));
            }
        }
        Assert.True(DeathNarration.WakeCardRepeatsTheFireball(DeathCause.Impact, DeathPlace.OwnShip));
    }

    [Fact]
    public void TheDefaultRustbucketSaysTheLossOnceNotTwice()
    {
        var outcome = InsuranceRule.DefaultRebirth(100);
        Assert.DoesNotContain("everything visible aboard", outcome.HullDescription, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheBirdsLastWarningDoesNotOutliveTheCaptainItWasFor()
    {
        using DeskBench bench = await DeskBench.BootAsync(AnImpact);
        await bench.RenderAsync();
        bench.Poke("_parrotSquawk", "WE'RE GOING TO CRASH — Selene Gate!");
        bench.CallOnTheDispatcher("BustedResurrect");
        Assert.Null(bench.Peek("_parrotSquawk"));
    }

    [Fact]
    public async Task TheMuteKeyAndTheMapPeekStillWorkUnderTheDeathCard()
    {
        using DeskBench bench = await DeskBench.BootAsync(AnImpact);
        await bench.RenderAsync();
        Assert.NotNull(bench.Peek("_busted"));
        bench.Poke("_audioArmed", true);
        bool audioBefore = (bool)bench.Peek("_audioEnabled")!;
        bool peekBefore = (bool)bench.Peek("_peekMap")!;

        // ToggleAudio flips the field and then calls the JS interop, which throws in a test host: the field
        // flipping is the proof the key got past the death-card guard.
        try { bench.CallOnTheDispatcher("OnKeyDown", new KeyboardEventArgs { Key = "m" }); }
        catch (System.Reflection.TargetInvocationException) { }
        Assert.NotEqual(audioBefore, (bool)bench.Peek("_audioEnabled")!);

        try { bench.CallOnTheDispatcher("OnKeyDown", new KeyboardEventArgs { Key = "`" }); }
        catch (System.Reflection.TargetInvocationException) { }
        Assert.NotEqual(peekBefore, (bool)bench.Peek("_peekMap")!);
    }

    [Fact]
    public async Task APulseBurnDoesNotReachTheSimWhileTheDeathCardIsUp()
    {
        using DeskBench bench = await DeskBench.BootAsync(AnImpact);
        await bench.RenderAsync();
        Assert.NotNull(bench.Peek("_busted"));
        bench.Poke("_audioArmed", true);
        bench.Poke("_lastPulseSimTime", -1e12);
        bench.Poke("_dockedHavenId", null);
        int before = (int)bench.Peek("_reactionMassPulses")!;
        Assert.True(before > 0);

        bench.CallOnTheDispatcher("OnKeyDown", new KeyboardEventArgs { Key = "-" });

        Assert.Equal(before, (int)bench.Peek("_reactionMassPulses")!);
    }
}
