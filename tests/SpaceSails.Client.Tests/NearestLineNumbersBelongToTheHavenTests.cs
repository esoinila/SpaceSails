using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// U02-F01 · "Nearest: Mars › The Space Bar (<i>Mars's</i> distance and speed)". The hierarchy line names a
/// berth that rides beside the planet, and the pair of numbers after the berth's name used to be the
/// planet's, inside the clamp hint that is about the berth. The pair now belongs to the last thing named.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class NearestLineNumbersBelongToTheHavenTests
{
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden) ?? throw new InvalidOperationException(field)).SetValue(o, value);

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden) ?? throw new InvalidOperationException(field)).GetValue(o)!;

    private static string Call(object o, string method, params object?[] args)
    {
        MethodInfo m = o.GetType().GetMethod(method, Hidden) ?? throw new InvalidOperationException(method);
        return (string)m.Invoke(o, args)!;
    }

    [Fact]
    public void TheNumbersAfterTheHavensNameAreTheHavensOwn()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        Set(map, "_scenarioName", TestTree.Sol.Name);
        Set(map, "_ephemeris", eph);
        Set(map, "_simulator", new Simulator(eph, timeStepSeconds: 1.0));

        MethodInfo fmt = typeof(Pages.Map).GetMethod("FormatDistance", Hidden | BindingFlags.Static)!;
        string Format(double m) => (string)fmt.Invoke(null, [m])!;

        double period = Math.Abs(TestTree.Sol.Bodies.Single(b => b.Id == "the-space-bar").OrbitPeriodS);
        int namedTheHaven = 0, hadToDiffer = 0;
        for (int step = 0; step <= 36; step++)
        {
            double t = period * step / 36.0;
            Vector2d mars = eph.Position("mars", t);
            Set(map, "SimTime", t);
            Set(map, "_ship", new ShipState(mars + mars / mars.Length * 2.0e9, Vector2d.Zero, 0.0));
            Call(map, "UpdateNearestBody");

            if (!Call(map, "NearestReadoutName").Contains('›'))
            {
                continue;
            }

            CelestialBody haven = Get<CelestialBody>(map, "_nearestHaven");
            Assert.NotEqual(Get<CelestialBody>(map, "_nearestBody").Id, haven.Id);
            namedTheHaven++;

            double toHaven = (Get<ShipState>(map, "_ship").Position - eph.Position(haven.Id, t)).Length;
            double toPlanet = (Get<ShipState>(map, "_ship").Position - mars).Length;
            string said = Call(map, "NearestReadoutNumbers");
            Assert.StartsWith($"({Format(toHaven)},", said);
            if (Format(toHaven) != Format(toPlanet))
            {
                hadToDiffer++;
            }
        }

        Assert.True(namedTheHaven > 0, "the bench never stood where the line names a berth beside the planet");
        Assert.True(hadToDiffer > 0, "the planet's distance and the berth's never printed differently - the bench cannot tell pass from fail");
    }
}
