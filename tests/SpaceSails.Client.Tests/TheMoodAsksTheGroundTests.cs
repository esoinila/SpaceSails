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
/// #867 · THE MOOD ASKS THE GROUND — driven, on a real floor, with the shipping shudder.
///
/// <para>Owner, 2026-08-13, strolling the B1 park: <i>"Our mood texts still assume the vacuum of the surface
/// btw :-D ... talk of nothing carrying the sound here as we are literally taking a walk in a park :-D"</i>
/// — his nerve ledger repeating <i>"a cold breath through the hull −1"</i> in a garden.</para>
///
/// <h3>Why this is driven and not read</h3>
///
/// <para>The Core sweep next door (<c>TheMoodAsksTheGroundTests</c> in Core.Tests) proves the SENTENCES are
/// forked. It cannot prove the park ever reaches the fork, and that is the half that was broken: the flag
/// was fine and the wiring picked the wrong pool. So these press <c>FireShudder</c> — the shipping method,
/// on a shipping <see cref="Pages.Map"/>, holding a real excursion over a floor
/// <see cref="UndergroundComplex.TopPressurisedFloor"/> published — and read the pulse line and the nerve
/// ledger the player would actually read. The captain stands at the SAME coordinates in both runs, so the
/// only thing that differs is the floor underfoot.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheMoodAsksTheGroundTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    /// <summary>The floor the owner was on: the topmost one this building says holds its air.</summary>
    private static int ThePark => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor to take a walk on.");

    /// <summary>Well below the regolith's top rim, so <c>onRegolith</c> is TRUE — which it also is in the
    /// park, a floor being laid inside the surface's own coordinate envelope. Held identical across both
    /// runs on purpose: if the lines diverge, the ground is the only thing that could have done it.</summary>
    private const double DeepInTheField = -100.0;

    /// <summary>A stable seed for the schedule. Any value does; this one is written down so the indices
    /// picked off it below are reproducible by hand.</summary>
    private const ulong Seed = 0xC0FFEE_1234UL;

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component standing on <paramref name="floor"/> (0 = out on the regolith), with the
    /// KAAMOS-deep escalation armed so the chill is eligible, and the shock bank primed to within a hair of
    /// a whole pip so the chill's prickle actually tips one and lands in the ledger. Everything else is the
    /// shipping page — see <c>MustStandUpBeforeWalkingTests</c> for the render-handle theatre.</summary>
    private static Pages.Map Standing(int floor)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, floor);
        // The chill is only ever consulted at an eligible site (a secret lab, or an arc gone deep). Forcing
        // the lab is the cheapest of the two and touches nothing else the shudder reads.
        exType.GetProperty("SecretLabForced")!.SetValue(ex, true);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_avatarX", 0.0);
        Set(map, "_avatarY", DeepInTheField);
        Set(map, "_shudderSeed", Seed);
        Set(map, "_nerve", NervePips.FromPips(NervePips.MaxPips));
        // A chill is a sub-pip prickle: on its own it banks and nothing visible happens, which is #480's
        // whole design. The bank is primed so the one we fire tips a pip and the ledger has to name it.
        Set(map, "_nerveShockCarry", NervePips.PipUnit - (HullShudder.ChillNerveTick / 2.0));
        return map;
    }

    private static void Set(object target, string field, object? value) =>
        (typeof(Pages.Map).GetField(field, Hidden)
         ?? throw new InvalidOperationException($"Map has no field {field}."))
        .SetValue(target, value);

    private static T Get<T>(object target, string field) =>
        (T)(typeof(Pages.Map).GetField(field, Hidden)
            ?? throw new InvalidOperationException($"Map has no field {field}."))
        .GetValue(target)!;

    /// <summary>Fire one shudder at <paramref name="index"/> and report what the player was told: the pulse
    /// line, and the newest words in the nerve ledger (or null if no pip moved).</summary>
    private static (string Pulse, string? Ledger) OneShudder(int floor, int index)
    {
        Pages.Map map = Standing(floor);
        Set(map, "_shudderIndex", index);

        (typeof(Pages.Map).GetMethod("FireShudder", Hidden)
         ?? throw new InvalidOperationException("Map has no FireShudder — the shudder has moved."))
        .Invoke(map, [0.0]);

        var pulse = Get<PulseSlot>(map, "_pulse");
        var ledger = Get<IReadOnlyList<NervePips.Event>>(map, "_nerveLedger");
        Assert.NotNull(pulse.Message);
        return (pulse.Message!, ledger.Count > 0 ? ledger[0].Label : null);
    }

    /// <summary>The first shudder ordinal off <see cref="Seed"/> that <paramref name="chill"/> matches —
    /// asked of Core's own gate rather than guessed, so the run really is the escalation it claims.</summary>
    private static int FirstIndexWhere(bool chill) =>
        Enumerable.Range(0, 200).First(i => HullShudder.CarriesChill(Seed, i) == chill);

    // ── THE GUARDS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE OWNER'S OWN COMPLAINT, BOTH WAYS. A chill shudder in the park draughts; the same chill
    /// out on the regolith still breathes cold through the hull. One captain, one seed, one ordinal, two
    /// floors.</summary>
    [Fact]
    public void AChillInTheParkDraughtsAndAChillOnTheSurfaceStillNamesTheHull()
    {
        int chill = FirstIndexWhere(chill: true);

        (string pulse, string? ledger) park = OneShudder(ThePark, chill);
        (string pulse, string? ledger) surface = OneShudder(0, chill);

        Assert.Equal("a cold draught from somewhere the fans do not reach", park.ledger);
        Assert.Equal("a cold breath through the hull", surface.ledger);

        Assert.Contains(park.pulse.TrimStart('〰', ' '), HullShudder.ChillLinesFor(groundHoldsPressure: true));
        Assert.Contains(surface.pulse.TrimStart('〰', ' '), HullShudder.ChillLinesFor(groundHoldsPressure: false));
    }

    /// <summary>AND THE ORDINARY BEAT TOO — the pool the owner was actually walking through, since a chill
    /// is only two in five of the shudders at an eligible site and none of them anywhere else. In the park
    /// the sound ARRIVES; on the regolith there is still nothing out there to carry it.</summary>
    [Fact]
    public void AnOrdinaryShudderInTheParkIsHeardAndOnTheSurfaceIsNot()
    {
        int calm = FirstIndexWhere(chill: false);

        string park = OneShudder(ThePark, calm).Pulse.TrimStart('〰', ' ');
        string surface = OneShudder(0, calm).Pulse.TrimStart('〰', ' ');

        Assert.Contains(park, HullShudder.LinesFor(HullShudder.Setting.Pressurised));
        Assert.Contains(surface, HullShudder.LinesFor(HullShudder.Setting.Regolith));
        Assert.NotEqual(park, surface);
    }

    /// <summary>NOTHING THE PARK SAYS NAMES VACUUM FURNITURE — asked of the LIVE page rather than the pool,
    /// over a long run of shudders so both the ordinary and the chill streams are exercised. This is the
    /// guard that would have caught the shipped bug: every string below came out of a component standing on
    /// a floor the building says holds its air.</summary>
    [Fact]
    public void OverAWholeWatchInThePark_NothingSpokenNamesAHullAnAirlockOrVacuum()
    {
        string[] furniture = ["hull", "airlock", "vacuum", "regolith", "to carry a sound"];
        var offenders = new List<string>();

        for (int i = 0; i < 40; i++)
        {
            (string pulse, string? ledger) = OneShudder(ThePark, i);
            foreach (string said in ledger is null ? new[] { pulse } : [pulse, ledger])
            {
                foreach (string bad in furniture)
                {
                    if (said.Contains(bad, StringComparison.OrdinalIgnoreCase))
                    {
                        offenders.Add($"  shudder {i} names '{bad}': \"{said}\"");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} vacuum lines reachable on pressurised ground"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders.Distinct()));
    }

    /// <summary>AND THE NERVE EASE, WHICH IS WHERE THE LEDGER SAID "THE AIRLOCK CLOSES BEHIND YOU" ON A
    /// LAWN. Driven through the page's own per-frame advance rather than Core's: the fact has to travel from
    /// the floor, through <c>StepNerve</c>'s frame, into the words — and it is a whole beat of real seconds,
    /// so this is the recovery a captain resting in the park actually gets.</summary>
    [Fact]
    public void RestingInTheParkIsWarmAirAndStandingLightsNotAnAirlock()
    {
        static string EaseOn(int floor)
        {
            Pages.Map map = Standing(floor);
            // A floor of the Hive is safe by the #585 rule; the surface at this depth is not, so the ease
            // can only be observed where the ground earns it. That IS the fork: same beat, same pip.
            Set(map, "_nerve", NervePips.FromPips(NervePips.MaxPips - 2));

            (typeof(Pages.Map).GetMethod("StepNerve", Hidden)
             ?? throw new InvalidOperationException("Map has no StepNerve."))
            .Invoke(map, [NervePips.AirlockBeatSeconds]);

            var ledger = Get<IReadOnlyList<NervePips.Event>>(map, "_nerveLedger");
            return ledger.Count > 0 ? ledger[0].Label : "(the gauge said nothing)";
        }

        Assert.Equal("warm air and standing lights - the floor is holding", EaseOn(ThePark));

        // And the surface keeps the sentence it has always had, from the one place it is still true: back
        // up the tube, which is exactly what the ease means out there.
        Assert.Equal("the airlock closes behind you",
            NervePips.Name(NervePips.Cause.Airlock, groundHoldsPressure: false));
    }
}
