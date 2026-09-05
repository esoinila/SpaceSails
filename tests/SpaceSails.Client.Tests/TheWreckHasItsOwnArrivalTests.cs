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
/// #244 items 1 and 3 (canon pass, Fable, 2026-09-05) · <b>THE ARRIVAL THAT SAID NOTHING, AND THE VERB THAT
/// SAID THE WRONG THING.</b>
///
/// <para>Owner, arrived at the roadster: <i>"I think we dropped out of autopilot… did we miss the dock button
/// press while warping?"</i> #1121 made the DISTANCE honest (a fetch pickup is proximity at a three-metre
/// object, so the old 499,721 km "success" was a car-park in the next county) and #1104 stopped the autopilot
/// promising a clamp that cannot exist there. Between them they left the arrival with nothing of its own to
/// say — the marker in <c>AutopilotStandInEnvelope</c> read <c>// FABLE: line needed</c>.</para>
///
/// <para>The same hole on the way in: #938 D3a took the dock vocabulary off the three μ=0 stations that carry
/// no haven flag and left them speaking the ORBIT vocabulary instead — "🛰 Arm auto-orbit at Derelict
/// Roadster" at three metres of dead car with no μ to orbit. Two wrong verbs is not a fix.</para>
///
/// <para>Canon closes both, and #244's follow-up closes the third hole they left between them — the arm-menu
/// TOOLTIP, which kept promising orbit under a button that had stopped offering it. Three strings: the arm
/// verb <see cref="HarborVocabulary.PickupArmVerb"/>, the arrival line
/// <see cref="HarborVocabulary.PickupArrival"/>, and the hint <see cref="HarborVocabulary.PickupArmHint"/>.
/// This file holds them VERBATIM (the text is pinned here
/// character for character, so a silent reword shows up as a red test and not as a shipped rewrite), holds
/// the line to ONCE per arrival, and holds both to the wreck and to nothing else.</para>
///
/// <para><b>Proven RED</b> on today's code: with the arrival branch removed the channel carries no such
/// sentence; with the once-flag removed three stand-downs put three copies on it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheWreckHasItsOwnArrivalTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The canon arm verb, pinned character for character. Fable, on #244, 2026-09-05.</summary>
    private const string TheVerb = "Close to pickup";

    /// <summary>The canon arrival line, pinned character for character. Fable, on #244, 2026-09-05.</summary>
    private const string TheLine = "Alongside. Inside pickup range, and nobody is answering.";

    /// <summary>The canon arm-menu hint, pinned character for character. Fable, on #244, 2026-09-05 — the
    /// follow-up this crew noted when the verb moved and the tooltip under it did not.</summary>
    private const string TheHint = "No orbit to slip into. She closes to pickup range and holds.";

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    // ── THE WORDS THEMSELVES ──────────────────────────────────────────────────────────────────────────

    /// <summary>VERBATIM. All three strings, as canon authored them — the whole point of pinning them in a
    /// second place is that a reword has to be a deliberate act in two files rather than a typo in one.
    /// </summary>
    [Fact]
    public void TheThreeNewStrings_AreExactlyWhatCanonAuthored()
    {
        Assert.Equal(TheVerb, HarborVocabulary.PickupArmVerb);
        Assert.Equal(TheLine, HarborVocabulary.PickupArrival);
        Assert.Equal(TheHint, HarborVocabulary.PickupArmHint);
    }

    /// <summary>ONE PUBLISHER. Each sentence is written down once in the source tree — in Core, where the
    /// harbour's one voice lives — so the two arm buttons and the arrival cannot drift into three readings of
    /// the same line. (#573's shape: two copies of a fact are two facts.)</summary>
    [Fact]
    public void EachSentence_IsWrittenDownExactlyOnceInTheSource()
    {
        foreach (string sentence in new[] { TheVerb, TheLine, TheHint })
        {
            var carriers = new List<string>();
            foreach (string file in Directory.EnumerateFiles(
                         Path.Combine(RepoRoot(), "src"), "*.*", SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(file);
                if (ext is not (".cs" or ".razor") || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }
                if (File.ReadAllText(file).Contains($"\"{sentence}", StringComparison.Ordinal))
                {
                    carriers.Add(Path.GetFileName(file));
                }
            }

            Assert.True(carriers.Count == 1,
                $"\"{sentence}\" is typed as a literal in {carriers.Count} file(s) ({string.Join(", ", carriers)}). "
                + "One voice, one place — every reader asks HarborVocabulary.");
            Assert.Equal("HarborVocabulary.cs", carriers[0]);
        }
    }

    /// <summary>THE MARKER IS GONE. <c>AutopilotStandInEnvelope</c> shipped with a <c>FABLE: line needed</c>
    /// standing in for this sentence; a marker left behind after the line lands is a lie in a comment.</summary>
    [Fact]
    public void TheFableMarker_IsGoneFromTheAutopilot()
    {
        string source = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Autopilot.cs"));

        Assert.Contains("AutopilotStandInEnvelope", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FABLE: line needed", source, StringComparison.Ordinal);
    }

    // ── ITEM 1 · THE ARRIVAL ──────────────────────────────────────────────────────────────────────────

    /// <summary>THE PREMISE. sol.json really carries a wreck the autopilot can be flown to, and it really is
    /// not a clamp berth — the exact case the line is for. Without this every law below could pass on a world
    /// that has no wreck in it.</summary>
    [Fact]
    public void ThePremise_TheScenarioCarriesAWreckThatIsNotAClampBerth()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);

        var wrecks = eph.Bodies.Where(b => Derelict.IsWreckBody(b.Id)).Select(b => b.Id).ToList();
        Assert.Equal([Derelict.RoadsterBodyId], wrecks);

        CelestialBody wreck = eph.Bodies.First(b => b.Id == Derelict.RoadsterBodyId);
        Assert.False(DockableHavens.IsDockable(wreck), "the wreck has become a clamp berth.");
        Assert.Equal(BodyKind.Station, wreck.Kind);

        // …and there really are berths that are NOT wrecks, so "only at a wreck" can fail.
        Assert.Contains(eph.Bodies, b => b.Kind == BodyKind.Station && !Derelict.IsWreckBody(b.Id));
    }

    /// <summary>THE LINE LANDS, VERBATIM, ON THE AUTOPILOT'S OWN CHANNEL — driven through the shipping
    /// <c>AutopilotStandInEnvelope</c>, the method the terminal <c>DockRule.Arrived</c> branch calls.</summary>
    [Fact]
    public void ArrivingAtTheWreck_SaysItOnTheAutopilotsOwnChannel()
    {
        Pages.Map map = StoodOffFrom(Derelict.RoadsterBodyId, out CelestialBody wreck);
        Invoke(map, "AutopilotStandInEnvelope", wreck);

        Assert.Contains(TheLine, Channel(map));
    }

    /// <summary>…AND ONCE. The stand-down sits on the autopilot's per-tick path, and a berth that repeats
    /// itself every tick is noise — the complaint was that the moment went UNNOTICED, which more of the same
    /// sentence does not fix.</summary>
    [Fact]
    public void ArrivingAtTheWreck_SaysItOnce_HoweverManyTicksArriveAtIt()
    {
        Pages.Map map = StoodOffFrom(Derelict.RoadsterBodyId, out CelestialBody wreck);

        for (int tick = 0; tick < 5; tick++)
        {
            Invoke(map, "AutopilotStandInEnvelope", wreck);
        }

        int said = Channel(map).Count(t => t == TheLine);
        Assert.True(said == 1, $"five arrivals put {said} copies of the wreck's line on the channel.");
    }

    /// <summary>…and the once is not a GAG. A fresh touch of the arm button — the captain deciding to fly
    /// this approach again — supersedes it, because coming back to the same hull is a thing that happens and
    /// a berth that never speaks twice is the original complaint with an extra step.
    ///
    /// <para>Driven through the shipping <c>ToggleArmedInsertion</c> on the already-armed body, which is the
    /// cheap first-click-confirms branch (#179): the clear sits above that branch and runs on every touch.
    /// </para></summary>
    [Fact]
    public void AFreshTouchOfTheArmButton_LetsTheNextArrivalSpeak()
    {
        Pages.Map map = StoodOffFrom(Derelict.RoadsterBodyId, out CelestialBody wreck);
        Invoke(map, "AutopilotStandInEnvelope", wreck);
        Assert.Equal(1, Channel(map).Count(t => t == TheLine));

        Set(map, "_armedOrbitBodyId", wreck.Id);
        Invoke(map, "ToggleArmedInsertion", wreck.Id);
        Invoke(map, "AutopilotStandInEnvelope", wreck);

        Assert.Equal(2, Channel(map).Count(t => t == TheLine));
    }

    /// <summary>AND NOWHERE ELSE. Every other body in the scenario — havens, farms, satellite works, moons —
    /// arrives in silence as far as this sentence is concerned. "The wreck says it" is trivially true of a
    /// channel that says it about everything.</summary>
    [Fact]
    public void NoOtherBerth_EverSpeaksTheWrecksLine()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        var wrong = new List<string>();

        foreach (CelestialBody body in eph.Bodies)
        {
            if (Derelict.IsWreckBody(body.Id))
            {
                continue;
            }

            Pages.Map map = StoodOffFrom(body.Id, out CelestialBody target);
            Invoke(map, "AutopilotStandInEnvelope", target);
            if (Channel(map).Any(t => t == TheLine))
            {
                wrong.Add($"  {body.Id} ({body.Name}, kind {body.Kind})");
            }
        }

        Assert.True(wrong.Count == 0,
            "the wreck's own arrival was spoken at berths that are not wrecks:\n" + string.Join("\n", wrong));
    }

    // ── ITEM 3 · THE ARM VERB ─────────────────────────────────────────────────────────────────────────

    /// <summary>BOTH ARM BUTTONS. The map's body menu and the plot ribbon arm the SAME thing, so a captain
    /// who read either one used to be told the ship was going to orbit a car. Read off the composed page
    /// (<see cref="MapMarkup"/>), because that is where the two buttons are written.</summary>
    [Fact]
    public void BothArmButtons_OfferThePickupVerbAtAWreck()
    {
        string page = MapMarkup.Read(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"));

        int offers = Occurrences(page, $"{nameof(HarborVocabulary)}.{nameof(HarborVocabulary.PickupArmVerb)}");
        Assert.True(offers == 2,
            $"the composed page offers the pickup verb on {offers} arm button(s); the body menu and the plot "
            + "ribbon both arm the same insertion and must both say what it will do.");

        // …and each offer is gated on the wreck question, not on the harbour class, which is what the orbit
        // vocabulary was wrongly falling through to.
        Assert.Equal(2, Occurrences(page, $"{nameof(Derelict)}.{nameof(Derelict.IsWreckBody)}"));
    }

    /// <summary>
    /// …AND THE SENTENCE UNDER THOSE BUTTONS SAYS THE SAME THING THEY DO. #1125 moved the verb and left the
    /// tooltip behind: <c>ArmMenuHint</c> still promised a captain hovering the arm button at three metres of
    /// dead car that the ship <i>slips into orbit here when the capture window opens</i>. One press, two
    /// sentences, and they disagreed — the third named bug class, and the reason the follow-up was filed on
    /// #244 rather than left alone.
    ///
    /// <para>Asked of the page's OWN method with the shipping scenario in it, so this is what a captain reads
    /// and not what a literal in this file says. Both arms are asserted: the wreck gets the canon hint
    /// verbatim, and every other berth in sol.json keeps the dock or orbit hint it has always had — a branch
    /// that answered the wreck's line everywhere would pass the first half on its own.</para>
    ///
    /// <para><b>Proven RED</b> two ways: the <c>IsWreckBody</c> arm removed from <c>ArmMenuHint</c> (the
    /// roadster promises orbit again, which is the follow-up's own words reproduced), and that arm widened
    /// to answer for every body (Titan and the havens start refusing to orbit).</para>
    /// </summary>
    [Fact]
    public void TheArmMenuHint_SaysPickupAtTheWreckAndOrbitOrDockEverywhereElse()
    {
        Assert.Equal(TheHint, HarborVocabulary.PickupArmHint);

        Pages.Map map = Booted();
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);

        int wrecksSeen = 0;
        int others = 0;
        foreach (CelestialBody body in eph.Bodies)
        {
            string hint = (string)Invoke(map, "ArmMenuHint", body.Id)!;
            if (Derelict.IsWreckBody(body.Id))
            {
                Assert.Equal(TheHint, hint);
                wrecksSeen++;
                continue;
            }

            Assert.NotEqual(TheHint, hint);

            // …and it is one of the two hints that already shipped, not merely "something else": a branch
            // that answered the empty string for every other berth would pass a NotEqual on its own.
            Assert.Contains("The autopilot flies the approach", hint, StringComparison.Ordinal);
            others++;
        }

        Assert.Equal(1, wrecksSeen);
        Assert.True(others > 5, $"only {others} non-wreck berths were asked; the control proves little.");
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A shipping page holding the scenario, with the hostile flag set on the target so
    /// <c>AutoDockHonest</c> is false for every body and each one takes the SAME branch — the graceful #155
    /// stand-down this line is written on — rather than half the sky auto-clamping.</summary>
    private static Pages.Map StoodOffFrom(string bodyId, out CelestialBody body)
    {
        Pages.Map map = Booted();
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        body = eph.Bodies.First(b => b.Id == bodyId);
        Set(map, "_plunderAuthorizedTargetId", bodyId);
        return map;
    }

    /// <summary>The autopilot's OWN channel — the event log the ledger renders, not the pulse toast.</summary>
    private static List<string> Channel(Pages.Map map) =>
        Get<List<(double SimTime, string Text)>>(map, "_autopilotEvents").Select(e => e.Text).ToList();

    private static int Occurrences(string haystack, string needle)
    {
        int n = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }

        return n;
    }

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

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary");
    }

    private static string ScenarioPath(string file) => Path.Combine(RepoRoot(), "scenarios", file);
}
