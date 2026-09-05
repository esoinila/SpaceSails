using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #244 item 2 · THE ARRIVAL DELIVERS THE ERRAND — the page's half.
///
/// <para>Owner, arrived at the roadster: <i>"I think we dropped out of autopilot… did we miss the dock
/// button press while warping?"</i> He had not: the autopilot had SUCCEEDED, 499,721 km out. #1104 stopped
/// it promising a clamp there. What it still did was stop there — and a fetch pickup is proximity at a
/// three-metre object, so the trip ended five times further out than the job it was flown for.</para>
///
/// <para>Two claims. (a) The page's PICKUP really does happen at the range the arrival now closes to, and
/// really does not at the envelope the ship used to be left in — driven on the real page, through the real
/// handler, with a real fetch job in the ledger. (b) The armed arrival's terminal branch really asks the new
/// question: a Core rule nothing calls is a Core rule that does nothing, which is the whole reason #938 D3a
/// existed at all.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheArrivalEndsWhereTheErrandIsTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    // ── (a) THE ERRAND HAPPENS WHERE THE SHIP IS LEFT ─────────────────────────────────────────────────

    /// <summary>
    /// THE PICKUP FIRES AT THE ARRIVAL RANGE. The autopilot's new stopping distance and the fetch job's
    /// reach are one number now; this drives the page at that distance and watches the wallet come loose.
    /// </summary>
    [Fact]
    public void AFetchJobIsPickedUpAtTheDistanceTheArrivalNowClosesTo()
    {
        (Pages.Map map, Pages.Map.Quest job) = AFetchJobAtTheWreck(DockRule.AlongsideMeters * 0.95);

        Invoke(map, "CheckFetchPickup");

        Assert.Equal(Pages.Map.QuestState.PickedUp, job.State);
    }

    /// <summary>
    /// AND NOT WHERE IT USED TO BE LEFT. The owner's own frame — inside the clamp envelope, matched, and
    /// nowhere near the car. If the pickup fired here the fix would be pointless; if the arrival still
    /// stopped here the job would never advance and the captain would be back at "did we miss the button?".
    /// </summary>
    [Fact]
    public void TheOldStandDownDistanceIsStillTooFarOutToTouchAnything()
    {
        (Pages.Map map, Pages.Map.Quest job) = AFetchJobAtTheWreck(4.99721e8);

        Invoke(map, "CheckFetchPickup");

        Assert.Equal(Pages.Map.QuestState.Active, job.State);

        // …and the reason is exactly the one this lane fixed: that distance is no longer an arrival.
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        CelestialBody wreck = eph.Bodies.First(b => b.Id == Derelict.RoadsterBodyId);
        Vector2d at = eph.Position(wreck.Id, 0);
        Assert.False(DockRule.Arrived(Get<ShipState>(map, "_ship"), at, Vector2d.Zero, wreck));
    }

    /// <summary>
    /// THE TWO NUMBERS ARE ONE NUMBER. The pickup used to carry its own private literal; if it drifts from
    /// the arrival range again, the autopilot goes back to delivering the ship to a place the errand does not
    /// happen — silently, because both halves would still be internally consistent.
    /// </summary>
    [Fact]
    public void ThePickupsReachIsTheArrivalsRange()
    {
        object? pickup = typeof(Pages.Map)
            .GetField("FetchPickupRangeM", Hidden)
            ?.GetValue(null);

        Assert.True(pickup is double,
            "Map has no FetchPickupRangeM — the pickup's reach has moved and this bench has drifted");
        Assert.Equal(DockRule.AlongsideMeters, (double)pickup!);
    }

    // ── (b) THE ARMED ARRIVAL ASKS THE NEW QUESTION ───────────────────────────────────────────────────

    /// <summary>
    /// THE TERMINAL BRANCH REALLY CALLS IT. A predicate nothing consults is a predicate that does nothing —
    /// which is precisely how the clamp promise survived for months. Read out of the shipping source, from
    /// the station branch of the armed-arrival loop itself, located structurally.
    /// </summary>
    [Fact]
    public void TheStationArrivalBranchAsksWhetherTheShipHasArrivedNotWhetherItIsInTheEnvelope()
    {
        string branch = TheStationArrivalBranch();

        Assert.Contains("DockRule.Arrived(", branch, StringComparison.Ordinal);
        Assert.DoesNotContain("DockRule.InEnvelope(", branch, StringComparison.Ordinal);
    }

    /// <summary>
    /// …AND SO DOES THE REHEARSAL THAT PRICES IT. The arm-time estimate and the flown journey have to be
    /// about the same destination, or the captain is quoted a trip he does not take.
    /// </summary>
    [Fact]
    public void TheRehearsalThatQuotesTheTripAsksTheSameQuestion()
    {
        string rehearsal = File.ReadAllText(CoreFile("AutopilotRehearsal.cs"));

        Assert.Contains("DockRule.Arrived(", rehearsal, StringComparison.Ordinal);
        Assert.DoesNotContain("DockRule.InEnvelope(", rehearsal, StringComparison.Ordinal);
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The page, a real fetch job on the roadster, and the ship parked
    /// <paramref name="metresOut"/> from the wreck's true position.</summary>
    private static (Pages.Map Map, Pages.Map.Quest Job) AFetchJobAtTheWreck(double metresOut)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));

        Vector2d wreck = ephemeris.Position(Derelict.RoadsterBodyId, 0);
        Set(map, "_ship", new ShipState(wreck + new Vector2d(metresOut, 0), Vector2d.Zero, 0));

        var job = new Pages.Map.Quest("fetch-bench", Pages.Map.QuestKind.Fetch, "THE FIXER", "", "The Fixer",
            "Fetch the roadster's lost wallet", "[bench]", 4200,
            DestBodyId: "the-space-bar", SourceBodyId: Derelict.RoadsterBodyId)
        {
            State = Pages.Map.QuestState.Active,
        };
        ((List<Pages.Map.Quest>)Get<object>(map, "_quests")!).Add(job);

        return (map, job);
    }

    /// <summary>The station branch of <c>CheckArmedInsertion</c> — from the <c>BodyKind.Station</c> fork to
    /// the moon-run comment that follows it. Located structurally, so this reads the code that actually
    /// decides an arrival rather than a string that happens to be somewhere in the file.</summary>
    private static string TheStationArrivalBranch()
    {
        string source = File.ReadAllText(ClientFile("Map.Autopilot.cs"));
        int start = source.IndexOf("if (body.Kind == BodyKind.Station)", StringComparison.Ordinal);
        Assert.True(start > 0,
            "Map.Autopilot no longer forks the armed arrival on BodyKind.Station — this bench has drifted");

        int end = source.IndexOf("#146 the moon run", start, StringComparison.Ordinal);
        Assert.True(end > start, "the moon-run branch no longer follows it — this bench has drifted");
        return source[start..end];
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

    private static string ClientFile(string name) =>
        Path.Combine(RepoDir("src", "SpaceSails.Client"), "Pages", name);

    private static string CoreFile(string name) => Path.Combine(RepoDir("src", "SpaceSails.Core"), name);

    private static string ScenarioPath(string file) => Path.Combine(RepoDir("scenarios"), file);

    private static string RepoDir(params string[] parts)
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine([dir, .. parts]);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException(
            string.Join('/', parts) + " not found above " + AppContext.BaseDirectory);
    }
}
