using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1216 · <b>A BOOT URL THE WORLD CANNOT HONOUR REFUSES. IT DOES NOT LAND ON THE ERROR PAGE.</b>
///
/// <para>Found by playing §3 of <c>docs/testing-links-2026-09-17.md</c> headless, twice, in two browsers.
/// Two URLs the docs themselves hand a tester ended on the red <i>"The voyage hit an error"</i> screen:</para>
/// <list type="number">
///   <item><c>/map?scenario=electric</c> — <c>HttpRequestException … 404</c>. There is no
///   <c>scenarios/electric.json</c>; the plasma world is <c>sol-eu</c>. The game's own contract, written in
///   <c>docs/testing-guide.md</c> Appendix A and exercised by step 8 of its scenario checklist, is
///   <i>"unknown → silent fall back to Sol"</i>. The name was already sanitised to a slug; the FETCH was the
///   half nobody kept.</item>
///   <item><c>/map?scenario=sol-eu&amp;start=wreck</c> — <c>KeyNotFoundException: derelict-roadster</c>, out of
///   <c>CircularOrbitEphemeris.Position</c> through <c>BerthState.CoOrbital</c>. The start registry is
///   scenario-blind, so a legal pair of whitelisted cheats was a hard crash.</item>
/// </list>
///
/// <para><b>Why no existing guard saw either.</b> <see cref="TheBootBuildsTheSameWorldTests"/> pins
/// eighty-odd boot URLs — but every one of them names a scenario that exists, and its fingerprint is taken
/// at the browser gate, which is <i>four stages before</i> <c>ApplyTheStartPoint</c> runs at all. The crash
/// lived in the gap on both sides: a world nobody boots, and a stage nobody reaches.</para>
///
/// <para>So this file boots the shipping page all the way past that gate (<see cref="PastTheGateBench"/>) and
/// sweeps <b>every registered start against every shipped scenario</b> — the whole cross product, taken off
/// the disk and off the registry rather than typed here, so a new scenario or a new start joins the sweep by
/// existing.</para>
/// </summary>
public sealed class ABootUrlRefusesRatherThanCrashesTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>Every world the game actually ships, by the name the URL spells — off the folder the page
    /// fetches from, never a list typed in here.</summary>
    private static IReadOnlyList<string> EveryShippedScenario() =>
        [.. Directory.EnumerateFiles(
                Path.Combine(TheBootBuildsTheSameWorldTests.RepoRoot(), "scenarios"), "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)];

    /// <summary>…and every start <c>?start=</c> accepts — read off the page's own registry by reflection, so
    /// a start added to <c>StartPoints</c> is swept whether or not anybody remembers this file.</summary>
    private static IReadOnlyList<string> EveryRegisteredStart()
    {
        object starts = typeof(Map)
            .GetField("StartPoints", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
            .GetValue(null)!;
        return [.. ((System.Collections.IEnumerable)starts)
            .Cast<object>()
            .Select(s => (string)s.GetType().GetProperty("Id")!.GetValue(s)!)];
    }

    // ── CASE 1 · AN UNKNOWN SCENARIO IS SOL ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("electric")]          // the §3 row's own name, and the one that 404'd on a tester
    [InlineData("not-a-real-scenario")] // the name docs/testing-guide.md step 8 tells the owner to type
    [InlineData("sol-e")]             // a plausible typo of a real one
    public async Task AnUnknownScenarioSilentlyFallsBackToSol(string unknown)
    {
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync($"/map?scenario={unknown}");

        Assert.Null(booted.Threw);
        Assert.Equal(TheNameOf("sol"), booted.Read<string>("_scenarioName"));
        Assert.Contains("selene-gate", TheBodiesOf(booted));
    }

    [Fact]
    public async Task ButAScenarioThatEXISTSIsStillTheOneThatLoads()
    {
        // The anti-vacuous half. A fallback that fired on everything would pass the theory above and would
        // have quietly deleted three of the four worlds this game ships.
        foreach (string shipped in EveryShippedScenario())
        {
            PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync($"/map?scenario={shipped}");

            Assert.Null(booted.Threw);
            Assert.Equal(TheNameOf(shipped), booted.Read<string>("_scenarioName"));
        }
    }

    // ── CASE 2 · EVERY START, IN EVERY SKY, WITHOUT A CRASH ──────────────────────────────────────────────

    [Fact]
    public async Task EveryRegisteredStartBootsInEveryShippedScenario()
    {
        var wrong = new List<string>();
        int swept = 0, honoured = 0, refused = 0;

        foreach (string scenario in EveryShippedScenario())
        {
            foreach (string start in EveryRegisteredStart())
            {
                string url = $"/map?scenario={scenario}&start={start}";
                PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync(url);
                swept++;

                if (booted.Threw is { } threw)
                {
                    wrong.Add($"{url}\n  {threw.GetType().Name}: {threw.Message}");
                    continue;
                }

                if (booted.Read<bool>("_showStartPicker"))
                {
                    refused++;
                    continue;
                }

                honoured++;

                // A start that WAS honoured must have been laid down against a body this sky actually holds
                // — which is the whole of the law, said about the world rather than about the check.
                if (TheBodyThisStartHangsOff(start) is { } anchor && !TheBodiesOf(booted).Contains(anchor))
                {
                    wrong.Add($"{url}\n  honoured a start anchored on '{anchor}', a body this sky does not hold.");
                }
            }
        }

        Assert.True(swept >= 4 * 9, $"only {swept} (scenario × start) pairs were swept — the sweep has shrunk.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {swept} (scenario × start) boots did not survive the start point:\n\n"
            + string.Join("\n\n", wrong));

        // …and the sweep saw BOTH answers. A pass in which everything was refused would prove nothing at all,
        // and neither would one in which nothing ever was.
        Assert.True(honoured > 0 && refused > 0,
            $"the sweep saw {honoured} honoured and {refused} refused — a sweep with only one outcome in it "
            + "cannot tell a working check from a check that is always on.");
    }

    [Fact]
    public async Task InSolEVERYStartIsHONOURED()
    {
        // The other half of the same law, and the reason the sweep above is not simply "show the picker and
        // nothing can crash". Sol carries every body the registry names, so in Sol nothing may be refused.
        foreach (string start in EveryRegisteredStart())
        {
            PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync($"/map?scenario=sol&start={start}");

            Assert.Null(booted.Threw);
            Assert.False(booted.Read<bool>("_showStartPicker"),
                $"?start={start} was refused in Sol, which holds every body the start registry names.");
        }
    }

    [Theory]
    [InlineData("sol-eu", "wreck")]   // the crash as it was reported: no derelict-roadster in the electric sky
    [InlineData("sol-eu", "earth")]   // …and no Selene Gate either
    [InlineData("wheel", "saturn")]   // the Wheel has eight planets and not one berth
    public async Task AStartThisSkyCannotAnchorIsRefusedToThePicker(string scenario, string start)
    {
        // …and the third half: the predicate has to be able to say NO, or "every start boots" is a sentence
        // about a check that never fires.
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync($"/map?scenario={scenario}&start={start}");

        Assert.Null(booted.Threw);
        Assert.True(booted.Read<bool>("_showStartPicker"),
            $"?scenario={scenario}&start={start} has no anchor body in that sky, so Appendix A's own answer "
            + "to an unhonourable start — the picker shows — is what the captain must get.");
        Assert.Null(booted.Field("_dockedHavenId"));
    }

    // ── The two readings this file makes of a booted page ────────────────────────────────────────────────

    /// <summary>The <c>Name</c> a shipped scenario file declares, read off the file rather than off the page,
    /// so the assertion and the thing asserted have two sources.</summary>
    private static string TheNameOf(string scenario)
    {
        string json = File.ReadAllText(Path.Combine(
            TheBootBuildsTheSameWorldTests.RepoRoot(), "scenarios", scenario + ".json"));
        return ScenarioLoader.Parse(json).Name;
    }

    private static IReadOnlyList<string> TheBodiesOf(PastTheGateBench.Boot booted) =>
        [.. booted.Read<ICelestialEphemeris>("_ephemeris").Bodies.Select(b => b.Id)];

    /// <summary>The body a start is laid down alongside, off the page's own one table — null for the plain
    /// spawn, which hangs off nothing a scenario could be missing.</summary>
    private static string? TheBodyThisStartHangsOff(string start)
    {
        object? anchor = typeof(Map)
            .GetMethod("WhatAStartHangsOff", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
            .Invoke(null, [start]);
        return anchor is null ? null : ((ValueTuple<string, double>)anchor).Item1;
    }
}
