using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #955 NAV-2 · <b>THE WINDOW THAT COMES BACK, ON THE PAGE.</b>
///
/// <para>Owner's corner case, 2026-08-23: <i>"docked at a Jupiter/Saturn haven the shuttle windows to the
/// moons are PERIODIC by default … so a window that closes while you are ashore must NOT mark the team
/// dead/stranded when it will reopen on its own; and it is the case of a window lost and regained without
/// any plotted course."</i> And the payoff half: <i>"with a pre-plotted, armed route you can LEAVE the ship
/// by shuttle mid-voyage … with a return-window clock on the captain's remote."</i></para>
///
/// <h3>RED PROOF (watched before this shipped)</h3>
/// <para>Before this lane, <c>Map.Expedition</c>'s docked branch declared the geometry INFINITE while
/// clamped — it never stranded anyone from a berth, but only by refusing to look at the moon. Reinstate that
/// <c>return double.PositiveInfinity;</c> and the wait-it-out test below goes red at once: the away line
/// reads "HOLDING course-match" while Ganymede is a hop and a half away, and the board's beyond-reach row
/// has no reopening to name. Reinstate the old three-argument <c>Classify</c> (no reopening folded in) and
/// the same test reads OUT OF REACH and the stranding toll rolls on a team that was never in danger.</para>
///
/// <para>Everything here plays the shipping <c>scenarios/sol.json</c>: The Red Eye is a real berth on a real
/// rail around Jupiter, and Ganymede is a real moon that really does swing in and out of a shuttle hop of
/// it every ~17 days.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheShuttleWindowComesBackTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public TheShuttleWindowComesBackTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private const string Berth = "red-eye";
    private const string Moon = "ganymede";

    // ── (1) THE CORNER CASE: ashore off a Jupiter berth, the window shuts, and nobody dies ─────────────

    /// <summary>
    /// THE OWNER'S RULING, PLAYED. Clamped to The Red Eye with the captain standing on Ganymede, the sim
    /// clock is spent across one full synodic period — the moon really does leave shuttle reach and really
    /// does come back. Through the whole of it the away line NEVER reads OUT OF REACH, the shut stretch says
    /// <i>next window in X</i>, and the expedition's stranding toll never rolls.
    /// </summary>
    [Fact]
    public void ASHORE_OFF_A_JUPITER_BERTH_TheWindowShutsAndReopens_AndTheTeamIsNeverLost()
    {
        Pages.Map map = AShipClampedAtTheRedEyeWithTheCaptainOnGanymede();
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");
        double synodic = ExpeditionWindow.SynodicPeriodSeconds(
            Body(eph, Berth).OrbitPeriod, Body(eph, Moon).OrbitPeriod);
        _out.WriteLine($"synodic period of the berth and the moon: {synodic / 86400:F1} d");

        object excursion = Get<object>(map, "_surface");
        object plan = Get<object>(map, "_expedition");

        int shut = 0, open = 0, sawTheWaitNamed = 0;
        for (double t = 0; t <= synodic; t += 7_200.0)
        {
            Set(map, "SimTime", t);
            object window = Invoke(map, "WindowOn", Moon)!;
            var status = (WindowStatus)window.GetType().GetProperty("Status", Hidden)!.GetValue(window)!;
            string word = (string)Invoke(map, "AwayWindowWord", window)!;

            // The gig's own reading of the same moment must agree. The docked branch no longer answers with
            // a fiction of its own — it used to declare the geometry INFINITE while clamped, which is how a
            // berth "never stranded anyone": by never looking at the moon.
            Assert.Equal(status, (WindowStatus)Invoke(map, "ExpeditionStatus", plan, excursion)!);

            Assert.NotEqual(WindowStatus.Lost, status);
            Assert.DoesNotContain("OUT OF REACH", word, StringComparison.Ordinal);

            if (status == WindowStatus.Closed)
            {
                shut++;
                Assert.Contains("next window in", word, StringComparison.Ordinal);
                sawTheWaitNamed++;
            }
            else
            {
                open++;
            }
        }

        _out.WriteLine($"across one period: {open} readings in reach, {shut} closed — and every closed one named the wait");
        Assert.True(shut > 0, "the moon never left shuttle reach — this bench cannot test the ruling");
        Assert.True(open > 0, "the moon never came back into reach — this bench cannot test the ruling");
        Assert.Equal(shut, sawTheWaitNamed);
    }

    /// <summary>
    /// AND THE TOLL NEVER ROLLS. The same berth, wound to a moment the window is genuinely shut, then given
    /// the expedition's own frame step. Before this lane the shut window drove the clock to zero and
    /// <c>ResolveExpeditionStranding</c> diced a team that was standing safely on a rock waiting for a moon.
    /// </summary>
    [Fact]
    public void THE_STRANDING_TOLL_DoesNotRollOnAWindowThatWillReopen()
    {
        Pages.Map map = AShipClampedAtTheRedEyeWithTheCaptainOnGanymede();
        Set(map, "SimTime", TheFirstMomentTheWindowIsShut(map));

        object excursion = Get<object>(map, "_surface");
        object plan = Get<object>(map, "_expedition");

        // The window IS shut — the bench is standing on the case under test, not beside it.
        Assert.Equal(WindowStatus.Closed, (WindowStatus)Invoke(map, "ExpeditionStatus", plan, excursion)!);

        // …and the away line says the wait out loud rather than counting to zero.
        object? comms = Invoke(map, "ExpeditionComms");
        Assert.NotNull(comms);
        string line = ((ValueTuple<string, int>)comms!).Item1;
        _out.WriteLine($"away line while shut: {line}");
        Assert.Contains("CLOSED", line, StringComparison.Ordinal);
        Assert.Contains("next window in", line, StringComparison.Ordinal);

        // Spend the expedition's own frame step over and over. Nobody dices anybody.
        for (int i = 0; i < 30; i++)
        {
            Invoke(map, "StepExpedition", 1.0);
        }
        Assert.False((bool)excursion.GetType().GetProperty("ExpeditionStrandingFired", Hidden)!.GetValue(excursion)!,
            "a window that reopens on its own must never roll the stranding toll");
        Assert.Equal(0, (int)excursion.GetType().GetProperty("ExpeditionScientistsLost", Hidden)!.GetValue(excursion)!);
    }

    /// <summary>ANTI-VACUITY: the toll is not simply switched off. Wind the SAME bench past the sponsor's
    /// contracted on-site budget and the gig ends exactly as it always did — the ship breaks station, the
    /// dice come out. Only the periodic geometry was ever the false alarm.</summary>
    [Fact]
    public void THE_STRANDING_TOLL_StillRollsWhenTheContractActuallyRunsOut()
    {
        Pages.Map map = AShipClampedAtTheRedEyeWithTheCaptainOnGanymede();
        object excursion = Get<object>(map, "_surface");
        SetProp(excursion, "ExpeditionOnSiteSeconds", ExpeditionWindow.DefaultHoldWindowSeconds + 1.0);

        Invoke(map, "StepExpedition", 0.0);
        Assert.True((bool)excursion.GetType().GetProperty("ExpeditionStrandingFired", Hidden)!.GetValue(excursion)!,
            "the sponsor's budget running out is still the end of the gig");
    }

    // ── (2) THE REMOTE HOLDS THE RETURN BY WHILE THE SHIP FLIES ON ────────────────────────────────────

    /// <summary>
    /// THE STARGATE CLOCK. The ship is flying an ARMED plotted route whose ribbon carries her inside a
    /// shuttle hop of Ganymede for a stretch; the captain is standing on it. The captain's remote reads
    /// RETURN BY — the egress of that window minus the ride home — and says in as many words that she is not
    /// waiting. Without the arm there is no promise to quote and the remote says nothing.
    /// </summary>
    [Fact]
    public void ON_AN_ARMED_ROUTE_TheRemoteReadsRETURNBY_AndTheShipFliesOn()
    {
        Pages.Map map = AShipClampedAtTheRedEyeWithTheCaptainOnGanymede();
        Set(map, "_dockedHavenId", null);            // cast off: she is flying, not clamped
        Set(map, "SimTime", 0.0);
        Set(map, "_ship", new ShipState(
            Get<ICelestialEphemeris>(map, "_ephemeris").Position(Moon, 0), Vector2d.Zero, 0));

        // A ribbon that closes on Ganymede, sits inside a hop of it, and pulls away again — the plotted
        // route's own pass, laid on the SAME TrajectorySample list the ARRIVE step reads.
        double windowOpens = 100_000.0, windowCloses = 400_000.0, ribbonEnds = 600_000.0;
        Set(map, "_samples", ARibbonPassing(map, Moon, ribbonEnds, windowOpens, windowCloses));

        // No arm = no promise. The board and the remote must not quote a RETURN BY nobody made.
        Set(map, "_armedArrivalPassSimTime", null);
        Assert.Null(Invoke(map, "RouteReturnBySimTime", Moon));
        Assert.Empty((System.Collections.IEnumerable)Invoke(map, "RouteWindowsOn", Moon)!);

        // ARM it (#969's plan-time promise): now she is going to fly this whether or not you are aboard.
        Set(map, "_armedArrivalPassSimTime", (double?)ribbonEnds);
        Set(map, "SimTime", (windowOpens + windowCloses) / 2.0);

        double? returnBy = (double?)Invoke(map, "RouteReturnBySimTime", Moon);
        Assert.NotNull(returnBy);
        double hop = ShuttleRange.TravelSeconds(ShuttleRange.RangeMeters);
        _out.WriteLine($"window {windowOpens:F0}–{windowCloses:F0} s; RETURN BY {returnBy!.Value:F0} s (hop {hop:F0} s)");

        // THE LAW: RETURN BY is the egress minus the ride home, and the ride home at the egress is a
        // full-reach crossing. Within one stride of the ribbon this bench laid (600,000 s over 400
        // samples = 1,500 s), because the egress is interpolated between two of its samples.
        Assert.Equal(windowCloses - hop, returnBy.Value, 1_500.0);

        string remote = (string)Invoke(map, "AwayWindowRemoteLine")!;
        _out.WriteLine($"remote: {remote}");
        Assert.Contains("RETURN BY", remote, StringComparison.Ordinal);
        Assert.Contains(RouteShuttleWindow.Stamp(returnBy.Value), remote, StringComparison.Ordinal);

        string small = (string)Invoke(map, "AwayWindowRemoteSubLine")!;
        _out.WriteLine($"remote sub: {small}");
        Assert.Contains("without you", small, StringComparison.Ordinal);
    }

    /// <summary>TWO DISJOINT SPANS ARE TWO ROWS — the owner's separate INGRESS and EGRESS windows, on the
    /// board, from one armed plan.</summary>
    [Fact]
    public void A_ROUTE_THAT_PASSES_TWICE_OffersBothTheIngressAndTheEgressWindow()
    {
        Pages.Map map = AShipClampedAtTheRedEyeWithTheCaptainOnGanymede();
        Set(map, "_dockedHavenId", null);
        Set(map, "SimTime", 0.0);
        Set(map, "_ship", new ShipState(
            Get<ICelestialEphemeris>(map, "_ephemeris").Position(Moon, 0), Vector2d.Zero, 0));
        Set(map, "_surface", null);   // aboard, reading the board rather than standing on the rock

        var samples = new List<TrajectorySample>();
        AppendPass(map, samples, Moon, from: 0, to: 400_000, opens: 40_000, closes: 160_000);
        AppendPass(map, samples, Moon, from: 400_000, to: 800_000, opens: 480_000, closes: 600_000);
        Set(map, "_samples", (IReadOnlyList<TrajectorySample>)samples);
        Set(map, "_armedArrivalPassSimTime", (double?)800_000.0);

        var rows = (System.Collections.IList)Invoke(map, "ShuttleRouteWindowRows")!;
        foreach (object row in rows)
        {
            _out.WriteLine((string)Invoke(map, "RouteWindowRowText", row)!);
        }

        Assert.Equal(2, rows.Count);
        Assert.Equal("ingress", RoleOf(rows[0]!));
        Assert.Equal("egress", RoleOf(rows[1]!));
        Assert.Contains("RETURN BY", (string)Invoke(map, "RouteWindowRowText", rows[0]!)!, StringComparison.Ordinal);
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    private static string RoleOf(object row) => (string)row.GetType().GetProperty("Role", Hidden)!.GetValue(row)!;

    private static CelestialBody Body(ICelestialEphemeris eph, string id) =>
        System.Linq.Enumerable.First(eph.Bodies, b => b.Id == id);

    /// <summary>The first moment (walking the shipping rails from t=0) the berth↔moon gap is past a shuttle
    /// hop — the instant the owner's corner case actually begins.</summary>
    private static double TheFirstMomentTheWindowIsShut(Pages.Map map)
    {
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");
        double t = 0;
        while ((eph.Position(Moon, t) - eph.Position(Berth, t)).Length < ShuttleRange.RangeMeters)
        {
            t += 3_600.0;
        }
        return t + 3_600.0;   // a step past the edge, so the bench is not balanced on the boundary
    }

    /// <summary>A ribbon that dives inside a shuttle hop of a body between two epochs and is well outside it
    /// the rest of the time — the plotted route's pass, expressed in the sample list the planner uses.</summary>
    private static IReadOnlyList<TrajectorySample> ARibbonPassing(
        Pages.Map map, string bodyId, double until, double opens, double closes)
    {
        var samples = new List<TrajectorySample>();
        AppendPass(map, samples, bodyId, 0, until, opens, closes);
        return samples;
    }

    private static void AppendPass(
        Pages.Map map, List<TrajectorySample> into, string bodyId,
        double from, double to, double opens, double closes)
    {
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");
        double step = (to - from) / 400.0;
        for (double t = from; t <= to; t += step)
        {
            // Far outside a hop except across [opens, closes], where the ribbon sits a fifth of a hop off.
            double gap = t >= opens && t <= closes
                ? 0.2 * ShuttleRange.RangeMeters
                : 3.0 * ShuttleRange.RangeMeters;
            into.Add(new TrajectorySample(t, eph.Position(bodyId, t) + new Vector2d(gap, 0)));
        }
    }

    /// <summary>A ship clamped to The Red Eye with an away-expedition running on Ganymede and the captain
    /// standing on it. The berth, the moon and their rails are the shipping scenario's own.</summary>
    private static Pages.Map AShipClampedAtTheRedEyeWithTheCaptainOnGanymede()
    {
        var map = new Pages.Map();
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComponentBase's render early-out has moved.");
        pending.SetValue(map, true);

        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_ephemeris", eph);
        Set(map, "_simulator", new Simulator(eph, timeStepSeconds: 1.0));
        Set(map, "SimTime", 0.0);
        Set(map, "_ship", new ShipState(eph.Position(Berth, 0), Vector2d.Zero, 0));
        Set(map, "_dockedHavenId", Berth);
        Set(map, "_dockOffset", Vector2d.Zero);

        // The gig, and the captain standing on its site — an ordinary moon, not a spawned rock, because the
        // corner case is precisely about ground that is on a rail and therefore comes back.
        Set(map, "_expedition", new ExpeditionPlan(
            ExpeditionFlavor.Science, ExpeditionSiteKind.MysticalRuins, Moon, "Ganymede",
            TeamSize: 4, BaseFee: ExpeditionReward.BaseFee, AcceptedSimTime: 0.0));
        Set(map, "_surface", AnExcursionOn(map, Moon));
        return map;
    }

    /// <summary>An away-expedition excursion standing on a body — built through the page's own private
    /// ShuttleStop / SurfaceExcursion shapes, so a rename of either breaks this bench loudly.</summary>
    private static object AnExcursionOn(Pages.Map map, string bodyId)
    {
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Map.ShuttleStop is gone — this bench has drifted.");
        object stop = Activator.CreateInstance(
            stopType, Body(eph, bodyId), 0.0, 0.0, false, true, false)!;

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Map.SurfaceExcursion is gone — this bench has drifted.");
        object excursion = Activator.CreateInstance(exType, nonPublic: true)!;
        SetProp(excursion, "Stop", stop);
        SetProp(excursion, "RestoreHavenId", Berth);
        SetProp(excursion, "Expedition", true);
        return excursion;
    }

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

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

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on {o.GetType().Name} — this bench has drifted"))
        .SetValue(o, value);

    private static void SetProp(object o, string name, object? value) =>
        (o.GetType().GetProperty(name, Hidden)
         ?? throw new InvalidOperationException($"no property {name} on {o.GetType().Name} — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string name) =>
        (T)(o.GetType().GetField(name, Hidden)
            ?? throw new InvalidOperationException($"no field {name} on Map — this bench has drifted"))
            .GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);
}
