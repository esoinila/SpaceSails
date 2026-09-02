using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Contracts;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #351 · <b>THE ROADSTER WAS IN HIS SCOPES BEFORE ANYONE HAD MENTIONED HER.</b>
///
/// <para>Owner, 2026-07-18, on a world one minute old — sim clock <c>0d 00h 01m</c>, still clamped on at
/// Selene Gate, no job taken and no scan run: <i>"After starting a new adventure I have the Roadster
/// already in my scopes? How is it there in a new adventure where I have not even taken the job or done
/// the sensor scan?"</i> The screenshot under that sentence is the Nav map's <b>frame</b> control with its
/// overflow dropdown open, and there — between Mars and Jupiter, under <c>Planets</c> — reads
/// <b>Derelict Roadster</b>. His second screenshot is the aftermath: he picked her, and the frame row
/// then carried a <c>Derelict Roadster</c> chip and a <c>v rel Derelict Roadster: 19.9 km/s</c> readout
/// for a wreck the charts are supposed not to know about.</para>
///
/// <h3>What was actually wrong</h3>
/// <para>Nothing had leaked out of a save and nothing had been seeded: <c>scenarios/sol.json</c> marks
/// <c>derelict-roadster</c> <c>"hidden": true</c>, the boot loads that into <c>_hiddenBodyIds</c>
/// (<c>Map.Sim.World.Build.BuildTheEphemerisAndAnnounceTheBerths</c>), and PR-A's rule is that such a
/// body draws nothing, answers no picker, rides no carousel and is never "Nearest" until an intel-fed
/// scan charts it. Every player-facing list of bodies in the page honours that — the click picker, the
/// nav search, the scope carousel, the Nearest sweep, the dock roster, the plot labels. <b>One did
/// not.</b> <c>FramePickerGroups</c> (#206's "All planets and moons need it" overflow) walked
/// <c>_ephemeris.Bodies</c> raw, so the one body in the scenario that exists to be FOUND announced its
/// own name in a chooser on the first minute of a fresh voyage.</para>
///
/// <h3>Why this file drives the page instead of reading it</h3>
/// <para>A shape guard ("the method mentions IsBodyHidden") is this repo's fifth named bug class waiting
/// to happen. So the bench loads the SHIPPING <c>scenarios/sol.json</c> through the SHIPPING boot step
/// that populates the hidden set, and then asks the two frame controls the captain actually reads — the
/// chip row (<c>FrameOptions</c>) and the overflow dropdown (<c>FramePickerGroups</c>) — what they would
/// print. And because a guard that only ever expects an ABSENCE cannot tell a working filter from an
/// empty world, the same bench charts the wreck the way play charts her (<c>RevealBody</c>) and requires
/// her to appear.</para>
///
/// <para><b>Red proof (run before shipping).</b> Drop the <c>!IsBodyHidden(b.Id)</c> from
/// <c>Map.Plot.Frame.FramePickerGroups</c> and <c>THE_OVERFLOW_PICKER_…</c> goes red naming her;
/// drop the <c>IsBodyHidden(id)</c> early-out from that file's <c>Add</c> and the chip tests go red.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheChartsDoNotNameWhatYouHaveNotFoundTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private const string WreckId = "derelict-roadster";
    private const string WreckName = "Derelict Roadster";

    // ── (a) THE DROPDOWN IN THE OWNER'S SCREENSHOT ────────────────────────────────────────────────────

    /// <summary>THE OVERFLOW PICKER DOES NOT NAME AN UNCHARTED WRECK. The control, and the group, from
    /// the screenshot: "Planets", one minute into a brand-new adventure.</summary>
    [Fact]
    public void THE_OVERFLOW_PICKER_OnAFreshVoyage_DoesNotNameTheUnchartedWreck()
    {
        Pages.Map map = AFreshVoyage();

        IReadOnlyList<string> named = EveryNameTheOverflowPickerPrints(map);

        Assert.DoesNotContain(WreckName, named);
        // …and the guard is not passing on an empty list: the charted neighbours are all still there.
        Assert.Contains("Mars", named);
        Assert.Contains("Jupiter", named);
        Assert.Contains("Luna", named);
    }

    /// <summary>…AND IT NAMES HER THE MOMENT SHE IS CHARTED. The same list, after the wreck is revealed
    /// exactly as play reveals her — so the filter above is a filter, not an empty world.</summary>
    [Fact]
    public void THE_OVERFLOW_PICKER_OnceTheWreckIsCharted_NamesHer()
    {
        Pages.Map map = AFreshVoyage();
        TheScanFindsHer(map);

        Assert.Contains(WreckName, EveryNameTheOverflowPickerPrints(map));
    }

    /// <summary>THE GROUP LABELS TOO. A hidden body that owned children would put its own name on the
    /// optgroup header even with every member filtered out — so the header list is asked as well.</summary>
    [Fact]
    public void THE_OVERFLOW_PICKER_OnAFreshVoyage_PutsNoUnchartedNameOnAGroupHeader()
    {
        Pages.Map map = AFreshVoyage();

        Assert.DoesNotContain(WreckName, EveryGroupHeaderTheOverflowPickerPrints(map));
    }

    // ── (b) THE CHIP ROW BESIDE IT ────────────────────────────────────────────────────────────────────

    /// <summary>NO CHIP EITHER. The chip row is fed from the same charts; a destination or a selected
    /// contact that happens to be uncharted must not put the wreck's name on a button.</summary>
    [Fact]
    public void THE_FRAME_CHIPS_OnAFreshVoyage_DoNotNameTheUnchartedWreck()
    {
        Pages.Map map = AFreshVoyage();
        Set(map, "_destinationBodyId", WreckId);
        Set(map, "_selectedTargetId", WreckId);

        IReadOnlyList<string> chips = EveryChipLabel(map);

        Assert.DoesNotContain(WreckName, chips);
        Assert.Contains("Sun", chips);   // the inertial frame is always offered — the row is not empty
    }

    /// <summary>BUT THE ACTIVE FRAME IS NEVER ORPHANED. #135's own law: the frame in use always has a
    /// chip, or the captain is standing in a ruler with no way to switch it off. Charted or not.</summary>
    [Fact]
    public void THE_FRAME_CHIPS_KeepAChipForTheFrameTheCaptainIsStandingIn()
    {
        Pages.Map map = AFreshVoyage();
        Set(map, "_plotFrameBodyId", WreckId);

        Assert.Contains(WreckName, EveryChipLabel(map));
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A world built the way the boot builds it: the shipping <c>scenarios/sol.json</c> through
    /// the shipping step that reads <c>"hidden": true</c> into <c>_hiddenBodyIds</c>. The ship sits out in
    /// the open at epoch 0, inside nobody's Hill sphere, which is the standing the screenshot was taken
    /// in bar the clamp (a clamp changes nothing either control reads).</summary>
    private static Pages.Map AFreshVoyage()
    {
        var map = new Pages.Map();

        // StateHasChanged's early-out — the same piece of theatre every page-driving bench here rides on
        // (RevealBody asks for a render, and this bench has no render handle to give it).
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);

        Invoke(map, "BuildTheEphemerisAndAnnounceTheBerths", Sol.Value);

        var ephemeris = Get<CircularOrbitEphemeris>(map, "_ephemeris");
        Assert.Contains(ephemeris.Bodies, b => b.Id == WreckId);
        Assert.True(((HashSet<string>)Get<object>(map, "_hiddenBodyIds")).Contains(WreckId),
            "the boot step did not read the wreck's \"hidden\": true — this bench is not testing the hidden case.");

        Set(map, "_ship", new ShipState(new Vector2d(2.0e11, 0), new Vector2d(0, 2.4e4), 0));
        return map;
    }

    /// <summary>The scan the Fixer's fix pays for, through the page's own reveal door (announce off: the
    /// pulse and the cue want a browser this bench does not have).</summary>
    private static void TheScanFindsHer(Pages.Map map) =>
        Invoke(map, "RevealBody", WreckId, "", false);

    /// <summary>Every &lt;option&gt; label the frame dropdown would render, flattened out of its groups —
    /// which is exactly the list the owner was looking at.</summary>
    private static IReadOnlyList<string> EveryNameTheOverflowPickerPrints(Pages.Map map)
    {
        var names = new List<string>();
        foreach (object group in (System.Collections.IEnumerable)Invoke(map, "FramePickerGroups")!)
        {
            foreach (CelestialBody b in (IEnumerable<CelestialBody>)Item(group, "Item2"))
            {
                names.Add(b.Name);
            }
        }

        return names;
    }

    /// <summary>Every &lt;optgroup&gt; header the same dropdown would render.</summary>
    private static IReadOnlyList<string> EveryGroupHeaderTheOverflowPickerPrints(Pages.Map map)
    {
        var headers = new List<string>();
        foreach (object group in (System.Collections.IEnumerable)Invoke(map, "FramePickerGroups")!)
        {
            headers.Add((string)Item(group, "Item1"));
        }

        return headers;
    }

    /// <summary>Every label on the frame chip row.</summary>
    private static IReadOnlyList<string> EveryChipLabel(Pages.Map map)
    {
        var labels = new List<string>();
        foreach (object opt in (System.Collections.IEnumerable)Invoke(map, "FrameOptions")!)
        {
            labels.Add((string)opt.GetType().GetProperty("Label")!.GetValue(opt)!);
        }

        return labels;
    }

    private static object Item(object tuple, string field) =>
        tuple.GetType().GetField(field)!.GetValue(tuple)!;

    private static readonly Lazy<ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    private static string ScenarioPath(string file)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("no scenarios/ directory above the test binary")
            : Path.Combine(dir.FullName, "scenarios", file);
    }

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden)
            ?? throw new InvalidOperationException($"no field {field} on {o.GetType().Name} — this bench has drifted"))
            .GetValue(o)!;

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on {o.GetType().Name} — this bench has drifted"))
        .SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on {o.GetType().Name} — this bench has drifted"))
        .Invoke(o, args);
}
