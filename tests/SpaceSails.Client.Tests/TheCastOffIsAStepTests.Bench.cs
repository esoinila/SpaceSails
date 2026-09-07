using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// <b>THE BENCH</b> — the world <see cref="TheCastOffIsAStepTests"/> flies, and the reflection plumbing it
/// flies it with (the <c>TheArrivalIsArmedThenNotOnlyNow</c> / <c>TheBerthEndsTheVoyage</c> idiom).
///
/// <para>Nothing here asserts anything about the cast-off. It is the berth, the plan, the frames and the
/// field accessors the three law parts next door stand on, kept in one file so that a reader looking for a
/// law never has to read past a helper to find one.</para>
/// </summary>
public sealed partial class TheCastOffIsAStepTests
{
    // ── The bench ──────────────────────────────────────────────────────────────────────────────────────

    private readonly record struct Flight(
        int Frames, double Days, double? CastOffSimTime, bool ClearanceFired, bool TransferFired,
        double MaxSeparationFromBerth, bool Docked)
    {
        public override string ToString() =>
            $"{Frames} frames, {Days:F1} d, cast off at {CastOffSimTime?.ToString("F0") ?? "never"}s, "
            + $"clearance={ClearanceFired} transfer={TransferFired} "
            + $"maxOffBerth={MaxSeparationFromBerth:E2} m, docked={Docked}";
    }

    /// <summary>
    /// SPEND THE CLOCK, TOUCH NOTHING ELSE. The three phases are the shipping frame's own, in the shipping
    /// order — the fixed-step loop that lands on the cast-off epoch and fires the plotted burns, the pin that
    /// keeps a berthed ship on her rail, and the accounting phase that bills fired nodes and runs
    /// <c>CheckArmedInsertion</c>. No key, no click, no arm, no re-plot.
    /// </summary>
    private Flight WarpThroughWithNoFurtherInput(Pages.Map map, string targetId, double untilSimTime)
    {
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        double start = Get<double>(map, "SimTime");
        double? castOffAt = null;
        double maxSeparation = 0;
        int frames = 0;

        while (Get<double>(map, "SimTime") < untilSimTime && frames < 4000)
        {
            if (Get<string?>(map, "_dockedHavenId") == targetId || Get<bool>(map, "_orbitKept"))
            {
                break; // arrived — the trip the plan promised is over
            }

            var ship = Get<ShipState>(map, "_ship");
            double simTime = Get<double>(map, "SimTime");
            Vector2d bodyPos = ephemeris.Position(targetId, simTime);
            Vector2d bodyVel = (ephemeris.Position(targetId, simTime + 1) - ephemeris.Position(targetId, simTime - 1)) / 2;
            double gap = (ship.Position - bodyPos).Length;
            double closing = Math.Max(1.0, (ship.Velocity - bodyVel).Length);
            // While a cast off is still pending, spend the clock in small bites: it is the only way to MEASURE
            // when the clamp let go rather than merely notice afterwards that it has. Everywhere else, the
            // frame's own warp discipline generalized — coarse across the void, tightening as the gap to the
            // target closes, which is what UpdateEffectiveWarp's near-body cap does live.
            double chunk = Invoke(map, "NextCastOffStep") is not null
                ? 2 * AdaptiveQuantumSeconds
                : Math.Clamp(gap / closing / 10.0, 60.0, 20000 * 60.0);

            Set(map, "_effectiveWarp", 10000);
            Set(map, "_simAccumulator", chunk);
            int steps = (int)Invoke(map, "ConsumeTheAccumulator", false)!;
            Invoke(map, "PinHerToTheDockAndDriftTheGhost");
            Invoke(map, "AccountForWhatTheStepsDid", steps);
            frames++;

            if (castOffAt is null && Get<string?>(map, "_dockedHavenId") is null)
            {
                castOffAt = Get<double>(map, "SimTime");
            }

            // Latch what actually fired off the nodes' own Executed flags, BEFORE the board prunes them
            // (clearing spent rows off the plan is itself part of the behaviour under test).
            _clearanceFired |= _clearanceNode is { } cn && (bool)FieldOf(cn, "Executed")!;
            _transferFired |= _transferNode is { } tn && (bool)FieldOf(tn, "Executed")!;

            if (castOffAt is not null)
            {
                double sep = (Get<ShipState>(map, "_ship").Position
                              - ephemeris.Position(Berth, Get<double>(map, "SimTime"))).Length;
                maxSeparation = Math.Max(maxSeparation, sep);
            }
        }

        double end = Get<double>(map, "SimTime");
        return new Flight(
            frames, (end - start) / Day, castOffAt,
            ClearanceFired: _clearanceFired, TransferFired: _transferFired,
            maxSeparation, Get<string?>(map, "_dockedHavenId") is not null);
    }

    // The two "did it actually fire" bits, latched off the nodes' own Executed flags before the list prunes
    // them (AccountForFiredNodes clears spent rows off the board, which is itself under test here).
    private bool _clearanceFired;
    private bool _transferFired;
    private object? _clearanceNode;
    private object? _transferNode;

    /// <summary>A ship CLAMPED at Selene Gate with an empty board and the scrub at now — the bar stool the
    /// owner plans his voyages from. Everything else in this file is built on top of it.</summary>
    private Pages.Map AShipClampedAtSeleneGate()
    {
        _clearanceFired = false;
        _transferFired = false;
        _clearanceNode = null;
        _transferNode = null;

        var map = new Pages.Map();
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_reactionMassPulses", 500);
        Set(map, "_horizonChoice", "400");   // the ribbon must reach the encounter it is being planned to

        // The berth, built the one way every berth in this game is built.
        ShipState berth = BerthState.CoMoving(ephemeris, Berth, 0, BerthState.BerthOffsetMeters);
        Set(map, "_ship", berth);
        Set(map, "SimTime", 0.0);
        Set(map, "_dockedHavenId", Berth);
        Set(map, "_dockOffset", berth.Position - ephemeris.Position(Berth, 0));
        return map;
    }

    /// <summary>
    /// A ship CLAMPED at Selene Gate with the owner's plan on the board: ⚓ + Cast off (the undock and the
    /// clearance), then one transfer burn solved with the game's own departure solver from the state the
    /// PLOTTED course delivers three weeks out — which is to say, from after the cast-off, exactly as the
    /// ribbon draws it. Nothing is armed.
    /// </summary>
    private Pages.Map AShipClampedAtSeleneGateWithACastOffAndATransferToMars()
    {
        Pages.Map map = AShipClampedAtSeleneGate();
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");

        // ⚓ + Cast off — the button, pressed, with the scrub where it starts: at NOW. (#989 keeps this
        // reading working — a scrub at now still departs immediately; it is the SCHEDULED departure that
        // stopped lying about its hour.)
        Invoke(map, "AddCastOffAtTop");
        Invoke(map, "ReprojectTrajectory");
        Invoke(map, "ReprojectThePassesOnTheirCadence", 1000.0);

        // She must genuinely be OUT of Earth's well by the time the transfer is solved, or a heliocentric
        // Lambert from inside it would be a lie and this bench would be proving nothing about the transfer.
        Vector2d atBurn = (Vector2d)Invoke(map, "SamplePositionAt", TransferBurnSimTime)!;
        double earthHill = OrbitRule.HillRadius(
            System.Linq.Enumerable.First(ephemeris.Bodies, b => b.Id == "earth"),
            System.Linq.Enumerable.First(ephemeris.Bodies, b => b.Id == "sun").Mu);
        double offEarth = (atBurn - ephemeris.Position("earth", TransferBurnSimTime)).Length;
        Assert.True(offEarth > earthHill,
            $"the cast-off must carry her clear of Earth's Hill sphere ({earthHill:E2} m) before the transfer "
            + $"is solved; she was {offEarth:E2} m out.");

        // The transfer burn, solved from where the PLOT puts her — cast-off, clearance and all.
        var atBurnVel = (Vector2d)Invoke(map, "SampledVelocityAt", TransferBurnSimTime)!;
        CelestialBody mars = System.Linq.Enumerable.First(ephemeris.Bodies, b => b.Id == "mars");
        LongHaul.Departure departure = LongHaul.SolveDeparture(
            new ShipState(atBurn, atBurnVel, TransferBurnSimTime), ephemeris, mars);
        Assert.True(departure.Ok, $"the bench's own departure solve must succeed: {departure.Failure}");

        Vector2d deltaV = departure.PostBurnVelocity - atBurnVel;
        AddPlottedVectorBurn(map, TransferBurnSimTime,
            deltaV.Length / atBurnVel.Length * 100.0, NodeFrame.Prograde(deltaV));

        Invoke(map, "RebuildPlan");
        Invoke(map, "ReprojectTrajectory");
        Invoke(map, "ReprojectThePassesOnTheirCadence", 2000.0);

        _clearanceNode = FirstNodeOfKind(map, PlanStepKind.ClearHarbour);
        _transferNode = FirstNodeOfKind(map, PlanStepKind.Burn);
        return map;
    }

    private static void AddPlottedVectorBurn(Pages.Map map, double simTime, double percent, double heading)
    {
        Type nodeType = typeof(Pages.Map).GetNestedType("PlanNode", BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("Map.PlanNode is gone — this bench has drifted.");
        object node = Activator.CreateInstance(nodeType, nonPublic: true)!;
        SetField(node, "Kind", PlanStepKind.Burn);
        SetField(node, "SimTime", simTime);
        SetField(node, "Action", ManeuverAction.Accelerate);
        SetField(node, "Pulses", 1);
        SetField(node, "Percent", percent);
        SetField(node, "Mode", BurnMode.Vector);
        SetField(node, "HeadingDegrees", heading);
        ((IList)Get<object>(map, "_planNodes")).Add(node);
    }

    private static void RemoveTheDepartureRows(Pages.Map map)
    {
        var nodes = (IList)Get<object>(map, "_planNodes");
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (KindOf(nodes[i]!) != PlanStepKind.Burn)
            {
                nodes.RemoveAt(i);
            }
        }
        Invoke(map, "RebuildPlan");
        Invoke(map, "ReprojectTrajectory");
        Invoke(map, "ReprojectThePassesOnTheirCadence", 3000.0);
    }

    private static ArrivalStepRule.ArrivalCheck? AddTheArriveStep(
        Pages.Map map, ClosestApproach.Pass pass, ArrivalStepRule.ArrivalKind kind)
    {
        Set(map, "_scrubOffsetSeconds", pass.SimTime - Get<ShipState>(map, "_ship").SimTime);
        Invoke(map, "AddArriveAtScrub", kind);
        Assert.NotNull(Get<object?>(map, "_arrive"));
        return (ArrivalStepRule.ArrivalCheck?)Invoke(map, "ArriveCheck");
    }

    private static ClosestApproach.Pass ThePassBy(Pages.Map map, string bodyId)
    {
        object? pass = Invoke(map, "ArrivePassFor", bodyId);
        Assert.True(pass is not null, $"the plotted course must have a pass by {bodyId} — this bench has drifted.");
        return (ClosestApproach.Pass)pass!;
    }

    private static double DistanceTo(Pages.Map map, string bodyId)
    {
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        var ship = Get<ShipState>(map, "_ship");
        return (ship.Position - ephemeris.Position(bodyId, ship.SimTime)).Length;
    }

    private static double UndockPush() =>
        (double)(typeof(Pages.Map).GetField("UndockPushMps", Hidden | BindingFlags.Static)
                 ?? throw new InvalidOperationException("Map.UndockPushMps is gone — this bench has drifted"))
            .GetRawConstantValue()!;

    private void AssertTheBannerSays(Pages.Map map, string phrase)
    {
        var status = (FlightPlanStatus)Invoke(map, "FlightNowNext")!;
        bool found = false;
        foreach (FlightPlanRow row in status.Rows)
        {
            if (row.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
            }
        }

        Assert.True(found,
            $"the banner should carry \"{phrase}\"; it had: "
            + string.Join(" | ", System.Linq.Enumerable.Select(status.Rows, r => r.Text)));
    }

    private void AssertTheBannerDoesNotSay(Pages.Map map, string phrase)
    {
        var status = (FlightPlanStatus)Invoke(map, "FlightNowNext")!;
        foreach (FlightPlanRow row in status.Rows)
        {
            Assert.False(row.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase),
                $"the banner must NOT carry \"{phrase}\"; it had: "
                + string.Join(" | ", System.Linq.Enumerable.Select(status.Rows, r => r.Text)));
        }
    }

    /// <summary>#989's second sighting as an assertion: the owner read <i>"NOW: casting off from The Red Eye
    /// in 0 h · NEXT: ⚓ cast off from The Red Eye in 0 h"</i> — one live row, spoken from two slots. No row
    /// of the banner may name the departure more than once, however many rows the banner has.</summary>
    private void AssertTheBannerNamesTheCastOffOnce(Pages.Map map)
    {
        var status = (FlightPlanStatus)Invoke(map, "FlightNowNext")!;
        int mentions = 0;
        foreach (FlightPlanRow row in status.Rows)
        {
            if (row.Text.Contains("cast", StringComparison.OrdinalIgnoreCase)
                && row.Text.Contains("off", StringComparison.OrdinalIgnoreCase))
            {
                mentions++;
            }
        }

        Assert.True(mentions == 1,
            $"the departure must be named exactly once in the banner, not {mentions} times: "
            + string.Join(" | ", System.Linq.Enumerable.Select(status.Rows, r => r.Text)));
    }

    // ── Reflection plumbing (the TheArrivalIsArmedThenNotOnlyNow / TheBerthEndsTheVoyage idiom) ─────────

    private static IReadOnlyList<object> PlanNodes(Pages.Map map)
    {
        var list = new List<object>();
        foreach (object? node in (IList)Get<object>(map, "_planNodes"))
        {
            list.Add(node!);
        }
        return list;
    }

    private static object FirstNodeOfKind(Pages.Map map, PlanStepKind kind)
    {
        foreach (object node in PlanNodes(map))
        {
            if (KindOf(node) == kind)
            {
                return node;
            }
        }

        throw new InvalidOperationException($"the bench's plan has no {kind} row — it has drifted");
    }

    private static PlanStepKind KindOf(object node) => (PlanStepKind)FieldOf(node, "Kind")!;

    private static int CountOfKind(Pages.Map map, PlanStepKind kind)
    {
        int n = 0;
        foreach (object node in PlanNodes(map))
        {
            if (KindOf(node) == kind)
            {
                n++;
            }
        }
        return n;
    }

    private static int PulsesOf(object node) => (int)FieldOf(node, "Pulses")!;

    private static double NodeEpochOf(object node) => (double)FieldOf(node, "SimTime")!;

    private static object? FieldOf(object node, string field) =>
        (node.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on PlanNode — this bench has drifted"))
        .GetValue(node);

    private static void Set(object o, string field, object? value) => SetField(o, field, value);

    private static void SetField(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on {o.GetType().Name} — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string name) =>
        (T)(o.GetType().GetField(name, Hidden)
            ?? throw new InvalidOperationException($"no field {name} on Map — this bench has drifted"))
            .GetValue(o)!;

    private static object? Property(object o, string name) =>
        (o.GetType().GetProperty(name, Hidden)
         ?? throw new InvalidOperationException($"no property {name} on Map — this bench has drifted"))
        .GetValue(o);

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);
}
