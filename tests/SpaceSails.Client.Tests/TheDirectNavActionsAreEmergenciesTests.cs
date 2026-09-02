using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #963 · <b>THE DIRECT NAV ACTIONS ARE EMERGENCY GEAR.</b>
///
/// <para>Owner's ruling, 2026-08-25, on the bare 🚀 Undock: it is <i>"archaic compared to the combo of undock
/// + harbour speed"</i>. He pressed it once and died colliding — <i>"it made perfect sense"</i> — and
/// <i>"flying adrift in formation is kind of crazy maneuver"</i>. On the family it belongs to: the direct
/// buttons <i>"mislead the player to not use the proper workflow"</i>. On why they stay: <i>"a boarding party
/// approaches, we undock without plan and work it out."</i></para>
///
/// <para><b>So the fix is a dress and a sentence, never a gate.</b> Nothing here asserts that a press was made
/// harder. An emergency you have to confirm is not an emergency, and the owner liked dying honestly — so
/// <see cref="TheEmergencyCastOffStillLetsGoOnOnePress"/> presses the real button through the renderer's own
/// event channel and watches the clamp open, first press, no dialog.</para>
///
/// <h3>Which buttons wear it, and the line that decides</h3>
///
/// <para>The test is <b>not</b> "does it move the ship" — ⚓ Dock and ⚓ Match &amp; clamp move her too. It is
/// whether pressing NOW produces a <b>different outcome from the one the plan would fly</b>: a shortcut, not
/// an early trigger.</para>
/// <list type="bullet">
/// <item><b>🚀 Undock</b> — the named one. The plan's ⚓ + Cast off step (#983) lays a cast-off pair AND a
/// harbour-speed departure burn; the bare press lays neither.</item>
/// <item><b>Enter orbit</b> — circularises at whatever radius she happens to hold, this instant, where the
/// #965 arrive-orbit step arms a rehearsed arrival at a chosen pass.</item>
/// <item><b>🛰 Autopilot to stable park</b> — the #180 fork of the same press; the descent begins from
/// wherever she is rather than from where the plan puts her.</item>
/// <item><b>Auto-orbit</b> (the ARM press) — points the autopilot off the current coast with no plan behind
/// it. Standing an ARMED autopilot back down is the safe direction and keeps its own green dress, which
/// <see cref="AnArmedAutopilotStandingDownIsNotDressedAsAnEmergency"/> pins so the red cannot creep.</item>
/// </list>
/// <para>⚓ Dock and ⚓ Match &amp; clamp are deliberately NOT dressed — see
/// <see cref="ClampingAlongsideIsNotAnEmergencyBecauseItIsWhatThePlanWouldDoAnyway"/>, which carries that
/// verdict and its reason so a later reader finds a decision rather than an omission.</para>
///
/// <h3>Red proof</h3>
/// <para>Put <c>btn-warning</c> back on 🚀 Undock and <see cref="TheFourEmergencyPressesWearTheEmergencyDress"/>
/// fails naming the button and the class it wears; restore the old fixed
/// <c>"Release the dock clamps and fly off"</c> title and
/// <see cref="TheCastOffHoverIsTheOwnersOwnSentence"/> fails on the missing words. Both were watched red
/// before this file was written — the runs are quoted in the pull request.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 12 s over 10 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheDirectNavActionsAreEmergenciesTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private const double AU = 1.495978707e11;

    /// <summary>The one dress an emergency press wears. Quiet red — an OUTLINE, not a filled danger button:
    /// the owner's style law is elegant minimalism, and a row of solid red would shout down the amber (#971)
    /// that belongs to Plot and + Add burn.</summary>
    private const string EmergencyDress = "btn-outline-danger";

    /// <summary>The owner's sentence for the cast-off hover, word for word.</summary>
    private const string CastOffHover =
        "Emergency cast-off — the clamp just lets go, no clearance, no plan. " +
        "⚓ + Cast off plans the departure properly.";

    /// <summary>A berth to be cast off from — the same world <see cref="EveryDeskBootsTests"/> uses.</summary>
    private const string ABerth = "/map?dock=selene-gate&body=luna&site=1";

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    /// <summary>Each emergency press, found in the markup by the words a player READS on it — never by the
    /// tooltip helper this file is also asserting, which would make the guard chase its own tail.</summary>
    public static TheoryData<string, string> TheEmergencyPresses => new()
    {
        { "🚀 Undock", "cast off bare — no clearance, no harbour-speed departure" },
        { "Enter orbit (@oi.Cost p / O)", "circularise at this radius, this instant" },
        { "🛰 Autopilot to stable park", "hand her to the autopilot from wherever she is" },
        { "Auto-orbit (≈", "arm the capture off the coast she is on" },
    };

    // ─────────────────────────── (a) the dress ───────────────────────────

    /// <summary>Every act-now nav press wears the emergency dress in its ENABLED state, and none of them is
    /// wearing the amber or the green that mark the workflow buttons and the good-news states.</summary>
    [Theory]
    [MemberData(nameof(TheEmergencyPresses))]
    public void TheFourEmergencyPressesWearTheEmergencyDress(string label, string what)
    {
        string button = ButtonAround(Razor(), label);

        Assert.True(button.Contains(EmergencyDress, StringComparison.Ordinal),
            $"#963 · the button that would {what} does not wear {EmergencyDress}. It is an emergency shortcut "
            + $"past the plan workflow and has to read as one. Its opening tag:\n{button}");

        // …and none of them is wearing the loud amber (#971) that marks the WORKFLOW keys, Plot and + Add
        // burn. That is the whole confusion the ruling names: "the direct buttons mislead the player to not
        // use the proper workflow."
        Assert.DoesNotContain("btn-warning", button, StringComparison.Ordinal);
    }

    /// <summary>THE HOVER THE OWNER WROTE, kept word for word rather than paraphrased into something
    /// nearly-right.</summary>
    [Fact]
    public void TheCastOffHoverIsTheOwnersOwnSentence()
    {
        string tip = (string)typeof(Map)
            .GetMethod("EmergencyUndockTip", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, null)!;

        Assert.Equal(CastOffHover, tip);

        // …and it is the sentence the BUTTON hovers, not a constant sitting in a file nothing reads.
        Assert.Contains("EmergencyUndockTip()", ButtonAround(Razor(), "🚀 Undock"), StringComparison.Ordinal);
    }

    /// <summary>THE SIBLING HOVERS ARE READ OFF LIVE STATE, in the same register: what this press does RIGHT
    /// NOW, then the workflow it is skipping. A fixed sentence would be true of nothing in particular — the
    /// #963 complaint that put the live tooltip on Plot in the first place.</summary>
    [Fact]
    public void EverySiblingHoverNamesTheBodyItWouldCommitHerTo()
    {
        Map map = ParkedOffMars();
        Set(map, "_destinationBodyId", "mars");
        object oi = Invoke(map, "OrbitInfo")
            ?? throw new InvalidOperationException("no orbit info off Mars — this bench has drifted");

        foreach (string tipName in new[]
                 { "EmergencyInsertionTip", "EmergencyDescentTip", "EmergencyCaptureTip" })
        {
            string tip = (string)Invoke(map, tipName, oi)!;

            Assert.StartsWith("Emergency ", tip, StringComparison.Ordinal);
            Assert.Contains("Mars", tip, StringComparison.Ordinal);
            Assert.Contains("🗺 Plot", tip, StringComparison.Ordinal);
            Assert.Contains("properly", tip, StringComparison.Ordinal);
        }
    }

    // ─────────────────────────── (b) the line, and what is on the other side of it ───────────────────────────

    /// <summary>
    /// THE VERDICT ON THE TWO CLAMP BUTTONS, PINNED WITH ITS REASON. ⚓ Dock and ⚓ Match &amp; clamp both act
    /// now and both move the ship, and neither is dressed as an emergency — because inside the clamp window
    /// they do exactly what the plan's dock arrival does, at the moment it would do it. That is an early
    /// trigger, not a shortcut, and painting it red would be a lie about the risk. If a later ruling disagrees,
    /// it is disagreeing with a decision written down here, not filling in an oversight.
    /// </summary>
    [Fact]
    public void ClampingAlongsideIsNotAnEmergencyBecauseItIsWhatThePlanWouldDoAnyway()
    {
        foreach (string clamp in new[] { "@onclick=\"ToggleDock\"", "@onclick=\"MatchAndClamp\"" })
        {
            Assert.DoesNotContain(EmergencyDress, ButtonAround(Razor(), clamp), StringComparison.Ordinal);
        }
    }

    /// <summary>Standing an armed autopilot DOWN is the safe direction, so that state keeps its own dress and
    /// its own words. Only the arm press is the emergency.</summary>
    [Fact]
    public void AnArmedAutopilotStandingDownIsNotDressedAsAnEmergency()
    {
        string button = ButtonAround(Razor(), "Auto-orbit (≈");

        // The armed branch keeps the green, and the emergency red is reached only through the OTHER branch.
        Assert.Contains("oi.Armed ? \"btn-success\"", button, StringComparison.Ordinal);
        Assert.Contains($"oi.InCaptureRange ? \"{EmergencyDress}\"", button, StringComparison.Ordinal);

        // …and it keeps its own words too: the emergency sentence is not what an armed autopilot hovers.
        Assert.Contains("oi.Armed ? \"Arm the autopilot", button, StringComparison.Ordinal);
    }

    /// <summary>THE LOUD AMBER STAYS THE WORKFLOW'S. #971 dressed Plot as the heart of navigation; the red
    /// that arrived with this ruling must not have taken its place.</summary>
    [Fact]
    public void PlotKeepsTheAmberAndTakesNoRed()
    {
        string plot = ButtonAround(Razor(), "@onclick=\"TogglePlotMode\"");

        Assert.Contains("btn-warning", plot, StringComparison.Ordinal);
        Assert.DoesNotContain(EmergencyDress, plot, StringComparison.Ordinal);
    }

    // ─────────────────────────── (c) the behaviour, unchanged ───────────────────────────

    /// <summary>
    /// PRESS IT AND THE CLAMP OPENS — first press, no dialog, no "are you sure".
    ///
    /// <para>This is the half of the ruling that is about what must NOT change, and it is proved by pressing
    /// the button the page actually drew, through the renderer's own event channel: a guard that read the
    /// markup for the absence of a confirm flag would go green on a confirm that was spelled differently.
    /// The emergency has to still work while the boarding party is coming aboard.</para>
    /// </summary>
    [Fact]
    public async Task TheEmergencyCastOffStillLetsGoOnOnePress()
    {
        using DeskBench bench = await DeskBench.BootAsync(ABerth);
        await bench.SwitchAsync(ShipDesk.Nav);
        DeskBench.Painted painted = await bench.RenderAsync();

        DeskBench.Painted.Node undock = TheUndockButton(painted);

        // The dress and the sentence, on the drawn element rather than on the source.
        Assert.Contains(EmergencyDress, undock.Classes);
        Assert.Equal(CastOffHover, undock.Attributes.GetValueOrDefault("title"));

        Assert.NotNull(bench.Field("_dockedHavenId"));
        await bench.PressAsync(undock.Handlers["onclick"]);

        Assert.Null(bench.Field("_dockedHavenId"));

        // …and the row agrees: the button that lets go is not offered to a ship that is already adrift.
        Assert.DoesNotContain(
            (await bench.RenderAsync()).Root.Descendants(),
            n => n.Element == "button" && n.Spoken.Contains("🚀 Undock", StringComparison.Ordinal));

        Assert.Empty(bench.EscapedPastTheGate);
    }

    private static DeskBench.Painted.Node TheUndockButton(DeskBench.Painted painted) =>
        Assert.Single(
            painted.Root.Descendants(),
            n => n.Element == "button"
                 && n.Spoken.Contains("🚀 Undock", StringComparison.Ordinal)
                 && n.Handlers.ContainsKey("onclick"));

    // ─────────────────────────── the bench ───────────────────────────

    /// <summary>A real Map parked off Mars in the sol scenario — the sibling hovers read live world state, so
    /// they are asked of a world and not of a stub.</summary>
    private static Map ParkedOffMars()
    {
        var map = new Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));

        Vector2d mars = ephemeris.Position("mars", 0);
        Set(map, "_ship", new ShipState(mars + (mars / mars.Length * (0.16 * AU)), Vector2d.Zero, 0.0));
        return map;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary");
    }

    /// <summary>Map.razor with the line endings normalised — git hands it out CRLF on Windows and LF on the
    /// runner, and a guard that cannot tell one machine from the other cannot tell pass from fail.</summary>
    private static string Razor() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"))
            .Replace("\r\n", "\n");

    /// <summary>The whole of a button from its opening angle bracket to the marker inside it, so a guard about
    /// one button's dress reads THAT button's classes and not a neighbour's.</summary>
    private static string ButtonAround(string razor, string marker)
    {
        int at = razor.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(at >= 0, $"\"{marker}\" is not in Map.razor any more — this bench has drifted.");
        int open = razor.LastIndexOf("<button", at, StringComparison.Ordinal);
        Assert.True(open >= 0, $"\"{marker}\" is not inside a button any more — this bench has drifted.");
        return razor[open..(at + marker.Length)];
    }

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);
}
