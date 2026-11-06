using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Contracts;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// ROUND 2 · TEST USER U01 (Mira) · issue #1415 · the Core halves. Each guard was watched RED with its fix
/// reverted (see the lane's pull request).
/// </summary>
public sealed class TheRoundTwoU01CoreFixesTests
{
    private static CircularOrbitEphemeris Sol() => CircularOrbitEphemeris.FromScenario(TestTree.Sol);

    // ── U01-F15 · a place is named once ──────────────────────────────────────────────────────────────────

    [Fact]
    public void AJobHeaderNamesItsPlaceOnce()
    {
        string find = JobTerms.VerbLine(new JobFacts(ContractKind.Fetch, "Derelict Roadster",
            JobTargetNature.Wreck, WhereName: "Derelict Roadster"));
        Assert.Equal("FIND — Derelict Roadster", find);

        string deliver = JobTerms.VerbLine(new JobFacts(ContractKind.CargoRun, "Selene Gate",
            JobTargetNature.Haven, WhereName: "Selene Gate — EARTH system"));
        Assert.Equal("DELIVER — Selene Gate — EARTH system", deliver);

        // Different target and place still read as target, place.
        string hunt = JobTerms.VerbLine(new JobFacts(ContractKind.Hunt, "Mars Depot",
            JobTargetNature.ParkedHull, WhereName: "in Mars orbit"));
        Assert.Equal("DESTROY — Mars Depot, in Mars orbit", hunt);
    }

    // ── U01-F17 · the wire reads right ───────────────────────────────────────────────────────────────────

    [Fact]
    public void TheCargoLineReadsAShipmentOfTheCargo()
    {
        var eph = Sol();
        int cargoLines = 0;
        foreach (NewsWire.NewsScope scope in new[] { NewsWire.NewsScope.SystemWire, NewsWire.NewsScope.PortRag })
        {
            foreach (string? salt in new string?[] { null, "cinder-roost", "selene-gate", "ceres" })
            {
                foreach (NewsWire.NewsItem item in NewsWire.Ambient(eph, 0, 400, scope, salt))
                {
                    if (item.Headline.Contains("went 'missing' in transit", StringComparison.Ordinal))
                    {
                        cargoLines++;
                        Assert.StartsWith("A shipment of ", item.Headline, StringComparison.Ordinal);
                    }
                }
            }
        }

        // Anti-vacuous: the sweep really met the line, and an Ice one among them.
        Assert.True(cargoLines > 20, $"the sweep met {cargoLines} cargo lines.");
        Assert.Contains(NewsWire.Ambient(eph, 0, 4000).Select(i => i.Headline),
            h => h.StartsWith("A shipment of Ice", StringComparison.Ordinal));
    }

    [Fact]
    public void OneTickerNeverPrintsTheSameLineTwice()
    {
        var eph = Sol();
        foreach (NewsWire.NewsScope scope in new[] { NewsWire.NewsScope.SystemWire, NewsWire.NewsScope.PortRag })
        {
            foreach (string? salt in new string?[] { null, "cinder-roost", "selene-gate", "ceres", "ringside-exchange" })
            {
                for (int start = 0; start < 60; start++)
                {
                    var heads = NewsWire.Ambient(eph, start * NewsWire.SecondsPerDay, 9, scope, salt)
                        .Select(i => i.Headline).ToList();
                    Assert.True(heads.Distinct(StringComparer.Ordinal).Count() == heads.Count,
                        $"{scope}/{salt}/day {start}: a ticker repeated a line:\n{string.Join("\n", heads)}");
                }
            }
        }
    }

    // ── U01-F09 · one name per hull in one sky ───────────────────────────────────────────────────────────

    [Fact]
    public void TwoHullsNeverShareACallsignInTheFoundingSky()
    {
        var eph = Sol();
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(eph, seed: 11, count: 4);
        IReadOnlyList<NpcShip> driver = MassDriverSchedule.GenerateCadence(
            eph, MassDriverSchedule.MassDriverRun.LunaMilkRun(), baseSimTime: 0, count: 4);
        var sky = driver.Concat(pods).ToList();

        // The bug as the game built it: the two lists open on the same nickname.
        Assert.True(sky.GroupBy(s => s.Callsign).Any(g => g.Count() > 1),
            "this bench has no clash in it, so nothing below proves anything.");

        IReadOnlyList<NpcShip> fixedSky = TrafficSchedule.WithDistinctCallsigns(sky);
        Assert.Equal(sky.Count, fixedSky.Count);
        Assert.Equal(fixedSky.Count, fixedSky.Select(s => s.Callsign).Distinct().Count());
        // The first holder keeps the name; ids, plans and order are untouched.
        Assert.Equal(sky.Select(s => s.Id), fixedSky.Select(s => s.Id));
        Assert.Equal(sky[0].Callsign, fixedSky[0].Callsign);
        // A sky with no clash comes back unchanged.
        Assert.Equal(driver.Select(s => s.Callsign), TrafficSchedule.WithDistinctCallsigns(driver).Select(s => s.Callsign));
    }

    // ── U01-F25 · the button answers her ask ─────────────────────────────────────────────────────────────

    [Fact]
    public void NadiaIsAnsweredWithHimAndIlseWithIt()
    {
        Assert.Equal("Yes — I'll find him", WalkIn.YesFor(WalkIn.Who.Nadia));
        Assert.Equal("Yes — I'll find it", WalkIn.YesFor(WalkIn.Who.Ilse));
        Assert.Contains("the man, not the file", WalkIn.TheStory(WalkIn.Who.Nadia));
    }

    [Fact]
    public void ADaysLineDoesNotDependOnTheWindowItIsPrintedIn()
    {
        // "Revisiting a day never contradicts what was already read": day D reads the same whether the
        // window ends at D, D+1 or D+20.
        var eph = Sol();
        foreach (NewsWire.NewsScope scope in new[] { NewsWire.NewsScope.SystemWire, NewsWire.NewsScope.PortRag })
        {
            foreach (string? salt in new string?[] { null, "cinder-roost", "ceres" })
            {
                for (long d = 40; d < 140; d++)
                {
                    string at = NewsWire.Ambient(eph, d * NewsWire.SecondsPerDay, 1, scope, salt)[0].Headline;
                    string plusOne = NewsWire.Ambient(eph, (d + 1) * NewsWire.SecondsPerDay, 2, scope, salt)[1].Headline;
                    string plusTwenty = NewsWire.Ambient(eph, (d + 20) * NewsWire.SecondsPerDay, 21, scope, salt)[20].Headline;
                    Assert.Equal(at, plusOne);
                    Assert.Equal(at, plusTwenty);
                }
            }
        }
    }
}
