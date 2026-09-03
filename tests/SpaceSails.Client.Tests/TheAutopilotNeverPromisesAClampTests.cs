using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// THE AUTOPILOT NEVER NAMES A BUTTON THAT CANNOT BE THERE (#938 D3a · #244 item 1 · a live #212 breach).
///
/// <para><b>The bug.</b> <c>sol.json</c> carries eleven μ=0 stations and three of them have no
/// <c>haven</c> flag: Mercury Compute Farms, Highport Satellite Works and the <b>Derelict Roadster</b>.
/// The clamp gate the board obeys is <see cref="DockableHavens.IsDockable"/> — <c>IsHaven</c> AND μ≤0 —
/// so <c>UpdateDockAffordance</c> never puts a ⚓ button on the screen for any of the three. But every
/// sentence about arriving asked μ ALONE: <c>HarborClassOf</c> returned <c>Dock</c>, so the map menu
/// offered "navigate to dock" and "✈ Autopilot: dock at Derelict Roadster", the arm hint promised "you
/// press ⚓ Dock at the end", the plan step read "Dock at" — and the terminal stand-down, which branches
/// on <c>BodyKind.Station</c>, delivered the ship alongside a wreck and said <b>"hit ⚓ Dock to clamp
/// on"</b>. The autopilot flew you to a dead hull and told you to press a button that does not exist.</para>
///
/// <para><b>The law.</b> For every body in the scenario, no surface the autopilot writes promises the
/// clamp unless <see cref="DockableHavens.IsDockable"/> says there is one. Three surfaces are pressed
/// here, all through the real page: the harbour class every label keys off, the arm-menu hint, and the
/// <c>_dockReadyStatus</c> line the graceful #155 envelope stand-down posts.</para>
///
/// <para><b>The premise.</b> A law over "every body" is worth nothing if no body in the world can break
/// it. <see cref="ThePremise_TheScenarioReallyCarriesStationsYouCannotClampOnto"/> names the bodies where
/// the old predicate (μ≤0) and the real one (IsDockable) disagree — if the scenario ever loses them, that
/// test says so out loud rather than letting the law go quietly vacuous.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheAutopilotNeverPromisesAClampTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The clamp instruction, exactly as the page writes it. Any surface carrying this is
    /// telling the captain to press ⚓ Dock.</summary>
    private const string TheClampPromise = "⚓ Dock";

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    /// <summary>
    /// THE PREMISE. The world can tell pass from fail: sol.json really does carry μ=0 stations that are
    /// not dockable havens, which is the exact gap the old <c>Mu: &lt;= 0</c> test fell through.
    /// </summary>
    [Fact]
    public void ThePremise_TheScenarioReallyCarriesStationsYouCannotClampOnto()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);

        var disagree = eph.Bodies
            .Where(b => b.Mu <= 0 && !DockableHavens.IsDockable(b))
            .Select(b => $"{b.Id} ({b.Name}, kind {b.Kind})")
            .ToList();

        Assert.True(
            disagree.Count > 0,
            "no body in sol.json is μ≤0 and NOT a dockable haven — the two predicates agree everywhere, so "
            + "the law below cannot fail and proves nothing. Restore a non-haven station (the Derelict "
            + "Roadster was one) or retire this bench.");

        // …and the Derelict Roadster is one of them by name, because it is the body the reconciliation
        // found and the one a reader of this file will go looking for.
        Assert.Contains(disagree, d => d.StartsWith("derelict-roadster ", StringComparison.Ordinal));
    }

    /// <summary>
    /// THE LAW. Every body in the scenario, through the real page: nothing promises ⚓ Dock where the
    /// clamp gate would refuse to draw the button.
    /// </summary>
    [Fact]
    public void No_surface_promises_the_clamp_at_a_body_you_cannot_clamp_onto()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Assert.True(eph.Bodies.Count >= 10, $"only {eph.Bodies.Count} bodies loaded — the scan proved nothing");

        var offences = new List<string>();
        int clampsPromised = 0;

        foreach (CelestialBody body in eph.Bodies)
        {
            bool clampable = DockableHavens.IsDockable(body);

            foreach ((string surface, string text) in WhatTheAutopilotSaysAbout(body))
            {
                bool promises = text.Contains(TheClampPromise, StringComparison.Ordinal);
                if (promises)
                {
                    clampsPromised++;
                }

                if (promises && !clampable)
                {
                    offences.Add(
                        $"  {body.Id} ({body.Name}) — IsDockable is FALSE, so the board never draws the ⚓ "
                        + $"button, yet {surface} says: \"{text}\"");
                }
            }
        }

        // The other half of "can this fail": the surfaces really do carry the promise SOMEWHERE, so a
        // rename of the button would show up as this bench going quiet rather than as a pass.
        Assert.True(clampsPromised > 0,
                    $"not one surface mentioned \"{TheClampPromise}\" for any body — either the promise was "
                    + "reworded (update TheClampPromise) or this bench is reading the wrong strings");

        Assert.True(offences.Count == 0,
                    $"{offences.Count} autopilot surface(s) promise a clamp that cannot engage:\n"
                    + string.Join("\n", offences));
    }

    /// <summary>Drive the real page and collect every sentence it writes about arriving at
    /// <paramref name="body"/>.</summary>
    private static IEnumerable<(string Surface, string Text)> WhatTheAutopilotSaysAbout(CelestialBody body)
    {
        Pages.Map map = Booted();

        // The hostile flag keeps AutoDockHonest false for EVERY body, so each one takes the same branch —
        // the graceful #155 stand-down that posts the coaching line — instead of half the scenario
        // auto-clamping through ClampOntoHaven. The line under test is written on that branch.
        Set(map, "_plunderAuthorizedTargetId", body.Id);

        var harbor = (HarborClass)Invoke(map, "HarborClassOf", body.Id)!;
        yield return ("HarborClassOf → ArmAction", HarborVocabulary.ArmAction(harbor, body.Name));
        yield return ("HarborClassOf → ArrivalStep", HarborVocabulary.ArrivalStep(harbor, body.Name, "alt 313 km"));
        yield return ("ArmMenuHint", (string)Invoke(map, "ArmMenuHint", body.Id)!);

        Invoke(map, "AutopilotStandInEnvelope", body);
        yield return ("the envelope stand-down line", Get<string?>(map, "_dockReadyStatus") ?? "");
    }

    // ── The bench (the reflection idiom of NearestHoldsTheNeighbourhoodTests) ─────────────────────────

    private static Pages.Map Booted()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_ship", new ShipState(Vector2d.Zero, Vector2d.Zero, 0.0));
        return map;
    }

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden)
            ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);

    private static string ScenarioPath(string file)
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("no scenarios/ directory above the test binary")
            : System.IO.Path.Combine(dir.FullName, "scenarios", file);
    }
}
