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
/// #241 (owner ruling, 2026-09-05) · <b>THE MONOLITH STILL READ PLANET.</b>
///
/// <para>#1121 gave the dead car its own class and left the other half of #241 open, because a
/// <see cref="Landmark"/> is deliberately <i>"a small Core datum, NOT an ephemeris body"</i> and nobody
/// wanted a landmark promoted onto rails just to have something to aim at. The ruling closes it without
/// promoting anything: the monolith reaches the glass the only way it can — as the rock it stands on — and
/// the glass says <b>LANDMARK</b>, asking it neither of the two questions an ephemeris body is asked. No
/// orbit note, no radius class.</para>
///
/// <para>Both questions were being answered wrong for the same reason. The corner tag's only question of a
/// body was <c>BodyRadius &gt; 1e8 ? "STAR" : "PLANET"</c> — an 11 km rock, so <b>PLANET</b> — and the AUTO
/// branch printed <c>orbits Mars</c> underneath it. That is a fact about Phobos; nobody points a telescope at
/// an 11 km rock for Phobos.</para>
///
/// <h3>The word</h3>
///
/// <para>The label is LANDMARK and not the reserved word of <c>docs/worldbuilding-notes.md</c> §8: <i>"There
/// is ONE monolith. Not a class of object, not a kind of landmark that a generator can roll twice — the word
/// is reserved."</i> An instrument that starts printing it is exactly how a second one gets born, so
/// <see cref="TheGlassStillNeverSpeaksTheReservedWord"/> reads every string this instrument can write,
/// landmark included.</para>
///
/// <para><b>Proven RED</b> on today's code: with <c>ScopeKindOf</c> back to derelict-or-body, the storied
/// ground reaches the glass as <c>Body</c> and the corner tag prints PLANET with <c>orbits Mars</c> under
/// it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheStoriedGroundIsNotAPlanetTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const int SizePx = 220;

    /// <summary>docs/worldbuilding-notes.md §8: "There is ONE monolith … the word is reserved."</summary>
    private const string TheReservedWord = "monolith";

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    // ── THE PREMISE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE WORLD CAN TELL PASS FROM FAIL. The scenario really carries the storied ground, it really
    /// is a body the glass can be aimed at, it really is small enough that the old radius question could only
    /// ever answer PLANET, and <see cref="Landmarks.HasNamedSite"/> really separates it from the rest of the
    /// sky — a predicate that answered YES to every moon, or to none, would make every law below pass on a
    /// broken instrument.</summary>
    [Fact]
    public void ThePremise_OneStoriedGround_SmallEnoughThatTheOldTagCouldOnlySayPlanet()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);

        var storied = eph.Bodies.Where(b => Landmarks.HasNamedSite(b.Id)).Select(b => b.Id).ToList();
        Assert.Equal([Monolith.BodyId], storied);

        CelestialBody ground = eph.Bodies.First(b => b.Id == Monolith.BodyId);
        Assert.Equal(BodyKind.Moon, ground.Kind);
        Assert.False(ground.IsHaven, "the storied ground has become a port, and a port's plate wins here.");
        Assert.True(ground.BodyRadius < 1e8,
            $"the storied ground's radius is {ground.BodyRadius:0} m — above the STAR line the old tag split "
            + "on, so the PLANET branch this bench is about is no longer the one it would take.");

        // …and there are ordinary moons it is NOT. A predicate that had widened to "any moon" would fail here.
        Assert.Contains(eph.Bodies, b => b.Kind == BodyKind.Moon && !Landmarks.HasNamedSite(b.Id));

        // The ground really does have a parent to print an orbit note about — the second half of the bug.
        Assert.False(string.IsNullOrEmpty(ground.ParentId),
            "the storied ground orbits nothing, so the 'no orbit note' law below could not fail.");
    }

    // ── THE LAWS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE PICKER IS THE BUG SITE. <c>ScopeView</c> can only draw the kind it is handed, so driven
    /// through the real <c>ResolveScopeTarget</c> over every body in the scenario at once — the storied
    /// ground is the landmark and nothing else is.</summary>
    [Fact]
    public void ThePageHandsTheGlassALandmark_AndNeverMistakesAWorldForOne()
    {
        Pages.Map map = Booted();
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);

        var wrong = new List<string>();
        int landmarksSeen = 0;

        foreach (CelestialBody body in eph.Bodies)
        {
            object? resolved = Invoke(map, "ResolveScopeTarget", body.Id);
            Assert.True(resolved is not null, $"the page cannot point the scope at {body.Id} at all");

            var target = (ScopeView.Target)resolved!;
            bool storied = Landmarks.HasNamedSite(body.Id) && !body.IsHaven;
            if (storied)
            {
                landmarksSeen++;
            }

            if (storied != (target.Kind == ScopeView.TargetKind.Landmark))
            {
                wrong.Add($"  {body.Id} ({body.Name}) — HasNamedSite={storied} but the scope is handed "
                    + $"{target.Kind}, which the corner tag prints as \"{TagFor(target)}\"");
            }
        }

        Assert.True(landmarksSeen == 1,
            $"the page resolved {landmarksSeen} storied grounds — §8 says there is one.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} body/bodies reach the telescope as the wrong class:\n" + string.Join("\n", wrong));
    }

    /// <summary>THE CORNER TAG. LANDMARK, and neither of the two things an ephemeris body is sorted into.
    /// Read off the glass, not off the enum, because the enum is not what a captain sees.</summary>
    [Fact]
    public void TheStoriedGround_ReadsLandmark_AndNoRadiusClass()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        CelestialBody ground = eph.Bodies.First(b => b.Id == Monolith.BodyId);

        Pages.Map map = Booted();
        var target = (ScopeView.Target)Invoke(map, "ResolveScopeTarget", ground.Id)!;

        var pen = new RecordingPen();
        new ScopeView(pen).Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, target);

        Assert.Contains("LANDMARK", pen.Texts);
        Assert.DoesNotContain("PLANET", pen.Texts);
        Assert.DoesNotContain("STAR", pen.Texts);
        Assert.DoesNotContain("DERELICT", pen.Texts);
        Assert.Contains(ground.Name.ToUpperInvariant(), pen.Texts);
    }

    /// <summary>THE ORBIT LINE — the other half, and it lives on the AUTO branch rather than on
    /// <c>ResolveScopeTarget</c>, so it has to be driven there. The page is stood in front of the storied
    /// ground with its parent named exactly as the hierarchy note wants, which is the state that used to
    /// print <c>orbits Mars</c> under an 11 km rock.</summary>
    [Fact]
    public void TheStoriedGround_IsGivenNoOrbitLine_EvenOnTheAutoBranch()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        CelestialBody ground = eph.Bodies.First(b => b.Id == Monolith.BodyId);
        CelestialBody parent = eph.Bodies.First(b => b.Id == ground.ParentId);
        CelestialBody ordinary = eph.Bodies.First(
            b => b.Kind == BodyKind.Moon && !Landmarks.HasNamedSite(b.Id) && b.ParentId is { Length: > 0 });
        CelestialBody ordinaryParent = eph.Bodies.First(b => b.Id == ordinary.ParentId);

        // ANTI-VACUOUS FIRST: the same bench, on an ordinary moon, really does print the note. Without this
        // "no orbit line" would be satisfied by a branch that had simply stopped working for everybody.
        ScopeView.Target ordinaryTarget = AutoPickedAt(ordinary, ordinaryParent);
        Assert.Equal(NearestRule.OrbitsNote(ordinaryParent.Name), ordinaryTarget.Detail);
        Assert.Equal(ScopeView.TargetKind.Body, ordinaryTarget.Kind);

        ScopeView.Target storied = AutoPickedAt(ground, parent);
        Assert.Equal(ScopeView.TargetKind.Landmark, storied.Kind);
        Assert.True(storied.Detail is null or { Length: 0 },
            $"the glass printed \"{storied.Detail}\" under the storied ground. A landmark has no rail of its "
            + "own, and its host's is a fact about a different object.");

        var pen = new RecordingPen();
        new ScopeView(pen).Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, storied);
        Assert.DoesNotContain(NearestRule.OrbitsNote(parent.Name), pen.Texts);
        Assert.Contains("LANDMARK", pen.Texts);
    }

    /// <summary>THE RESERVED WORD IS STILL NOT THIS INSTRUMENT'S TO SPEND (docs/worldbuilding-notes.md §8),
    /// and the new kind is the likeliest place it would have been spent. Every string the glass can write for
    /// every body in the scenario, plus the storied ground as the page actually resolves it.</summary>
    [Fact]
    public void TheGlassStillNeverSpeaksTheReservedWord()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Pages.Map map = Booted();
        var said = new List<string>();

        foreach (CelestialBody body in eph.Bodies)
        {
            var pen = new RecordingPen();
            new ScopeView(pen).Draw(
                SizePx, 0, Vector2d.Zero, Vector2d.Zero,
                (ScopeView.Target)Invoke(map, "ResolveScopeTarget", body.Id)!);
            said.AddRange(pen.Texts);
        }

        Assert.True(said.Count > 20, $"only {said.Count} string(s) came off the glass — nothing was scanned");
        Assert.Contains(said, s => s.Contains("LANDMARK", StringComparison.Ordinal));
        Assert.DoesNotContain(said, s => s.Contains(TheReservedWord, StringComparison.OrdinalIgnoreCase));
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The AUTO branch's own answer for a body: the page stood in front of it, with the hierarchy
    /// note's two fields set exactly as <c>NearestRule</c> leaves them. Nothing here re-derives the note —
    /// the shipping <c>PickScopeTarget</c> is what is asked.</summary>
    private static ScopeView.Target AutoPickedAt(CelestialBody body, CelestialBody parent)
    {
        Pages.Map map = Booted();
        Set(map, "_nearestBody", body);
        Set(map, "_nearestBodyPosition", new Vector2d(body.OrbitRadius + 1e9, 0));
        Set(map, "_nearestBodyVelocity", new Vector2d(1_000, 0));
        Set(map, "_nearestParentName", parent.Name);
        Set(map, "_nearestChildName", body.Name);
        return (ScopeView.Target)Invoke(map, "PickScopeTarget")!;
    }

    private static string TagFor(in ScopeView.Target target)
    {
        var pen = new RecordingPen();
        new ScopeView(pen).Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, target);
        return string.Join(" · ", pen.Texts);
    }

    /// <summary>Records rather than renders, the idiom every drawn-thing guard in this suite uses.</summary>
    private sealed class RecordingPen : IRenderer
    {
        public List<string> Texts { get; } = [];

        public List<float[]> Polylines { get; } = [];

        public List<(float X, float Y, float R)> Circles { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background)
        {
            Texts.Clear();
            Polylines.Clear();
            Circles.Clear();
        }

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Circles.Add((x, y, r));

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            Polylines.Add(pts.ToArray());

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px monospace",
                             TextAlign align = TextAlign.Left) => Texts.Add(text ?? "");

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) { }
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
            ? throw new InvalidOperationException("no scenarios/ folder above the test binary")
            : System.IO.Path.Combine(dir.FullName, "scenarios", file);
    }
}
