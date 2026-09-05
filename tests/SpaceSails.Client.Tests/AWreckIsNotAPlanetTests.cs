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
/// #241 · A WRECK GETS A PLANET'S PORTRAIT.
///
/// <para><b>The bug.</b> <c>ScopeView.TargetKind</c> knew five kinds and none of them was a dead hull, so
/// every ephemeris body reached the glass as <c>Body</c> — and the corner tag's only question of a body was
/// its radius (<c>BodyRadius &gt; 1e8 ? "STAR" : "PLANET"</c>). The Derelict Roadster is three metres of
/// dead car on its own rail round the sun. The telescope drew it as a lit disc with a terminator painted on
/// it and tagged it <b>PLANET</b>.</para>
///
/// <para><b>The fix.</b> A kind per class, which is what #241 asks for ("a wireframe-per-body-class seam …
/// future wrecks and oddities each get a portrait without new plumbing"): <see cref="Derelict.IsWreckBody"/>
/// is the one question, and the portrait is <see cref="WreckLayout.HullOutline"/> — the very outline the
/// away team's boots collide with, so no second set of hull numbers is typed into a renderer.</para>
///
/// <para><b>What is NOT here.</b> The issue also wants the Phobos landmark to have a scope portrait of its
/// own. It cannot: <c>Landmark</c> is deliberately "a small Core datum, NOT an ephemeris body" (its own
/// docblock), so it is not a thing the telescope can be aimed at — there is no target to draw and no tag to
/// write. <see cref="TheGlassNeverSpeaksTheReservedWord"/> holds the line that matters in the meantime: the
/// reserved word of <c>docs/worldbuilding-notes.md</c> §8 appears in nothing this instrument writes.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AWreckIsNotAPlanetTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const int SizePx = 220;

    /// <summary>docs/worldbuilding-notes.md §8: "There is ONE monolith … the word is reserved." An
    /// instrument that starts calling slabs by it is how a class of object gets born.</summary>
    private const string TheReservedWord = "monolith";

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    // ── THE PREMISE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WORLD CAN TELL PASS FROM FAIL. sol.json really carries a wreck, it really is a body the scope can
    /// be pointed at, and <see cref="Derelict.IsWreckBody"/> really separates it from everything else out
    /// there — a predicate that answered YES to nothing, or to every station, would make every law below
    /// pass on a broken renderer.
    /// </summary>
    [Fact]
    public void ThePremise_TheScenarioCarriesExactlyOneDeadHullAndTheRestAreNotWrecks()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Assert.True(eph.Bodies.Count >= 10, $"only {eph.Bodies.Count} bodies loaded — nothing was proved");

        var wrecks = eph.Bodies.Where(b => Derelict.IsWreckBody(b.Id)).Select(b => b.Id).ToList();
        Assert.Equal([Derelict.RoadsterBodyId], wrecks);

        // …and it is the case the bug was about: a station-kind body small enough that the old radius test
        // could only ever call it a planet.
        CelestialBody wreck = eph.Bodies.First(b => b.Id == Derelict.RoadsterBodyId);
        Assert.Equal(BodyKind.Station, wreck.Kind);
        Assert.True(wreck.BodyRadius < 1e8,
            $"the roadster's radius is {wreck.BodyRadius:0} m — above the STAR line the old tag split on, so "
            + "the PLANET branch this bench is about is no longer the one it would take.");

        // Every other μ=0 station is NOT a wreck: farms and satellite works keep whatever they had, and a
        // predicate that had quietly widened to "any non-haven station" would fail right here.
        Assert.Contains(eph.Bodies, b => b.Mu <= 0 && !b.IsHaven && !Derelict.IsWreckBody(b.Id));
    }

    // ── THE LAW ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A DERELICT READS DERELICT, AND IT IS THE WRECK'S OWN SHAPE ON THE GLASS.
    ///
    /// <para>Both halves matter and they fail differently: a tag with no portrait is a lie about a picture,
    /// and a portrait with no tag is the old bug wearing a new hull. The shape is checked against Core's
    /// outline rather than against a transcript of pixels — every drawn point must be the same point of
    /// <see cref="WreckLayout.HullOutline"/> under ONE scale, so a renderer that drew any other polyline
    /// (a freighter, a box, half the hull) cannot satisfy it.</para>
    /// </summary>
    [Fact]
    public void ADerelictWearsItsOwnNameAndItsOwnHull()
    {
        var pen = new RecordingPen();
        new ScopeView(pen).Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, AWreckAt(1.7e11));

        Assert.Contains("DERELICT", pen.Texts);
        Assert.DoesNotContain("PLANET", pen.Texts);
        Assert.DoesNotContain("STAR", pen.Texts);

        IReadOnlyList<(float X, float Y)> outline = WreckLayout.HullOutline();
        float[]? drawn = pen.Polylines.FirstOrDefault(p => p.Length == outline.Count * 2);
        Assert.True(drawn is not null,
            $"the scope drew no polyline of {outline.Count} points, so the hull is not on the glass at all. "
            + $"It drew: {string.Join(", ", pen.Polylines.Select(p => p.Length / 2))} point(s).");

        // The one free parameter is the scale; solve it off the hull's own length and hold every point to it.
        float midX = (WreckLayout.BowX + WreckLayout.TransomX) / 2f;
        float u = (drawn![6] - drawn[0]) / ((outline[3].X - midX) - (outline[0].X - midX));
        Assert.True(u > 0.5f, $"the hull is drawn at {u:0.000} px per deck unit — a smear, not a portrait");

        float cx = SizePx / 2f, cy = SizePx / 2f;
        for (int i = 0; i < outline.Count; i++)
        {
            Assert.Equal(cx + ((outline[i].X - midX) * u), drawn[i * 2], 2);
            Assert.Equal(cy + (outline[i].Y * u), drawn[(i * 2) + 1], 2);
        }

        // And she is drawn COLD. Every other sprite on this instrument puts a real disc on the glass — a
        // planet's limb, a freighter's bridge light, a pod's beacon, the dart's own engine. A dead hull puts
        // nothing bigger than the backdrop's stars, which is the whole reading.
        Assert.DoesNotContain(pen.Circles, c => c.R > 3f);
    }

    /// <summary>
    /// AND NOTHING ELSE CHANGED. A moon and a star still read exactly what they read before — a fix that
    /// re-tagged the sky would be a bigger bug than the one it cured, and "a wreck reads DERELICT" is
    /// trivially true of a renderer that says DERELICT about everything.
    /// </summary>
    [Fact]
    public void AMoonStillReadsItsOwnKindAndSoDoesAStar()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        CelestialBody moon = eph.Bodies.First(b => b.Kind == BodyKind.Moon);
        CelestialBody sun = eph.Bodies.First(b => b.BodyRadius > 1e8);

        var moonPen = new RecordingPen();
        new ScopeView(moonPen).Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, ABodyLike(moon));
        Assert.Contains("PLANET", moonPen.Texts);
        Assert.DoesNotContain("DERELICT", moonPen.Texts);
        Assert.Contains(moonPen.Circles, c => c.R > SizePx * 0.2f);   // a disc, not a wireframe

        var starPen = new RecordingPen();
        new ScopeView(starPen).Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, ABodyLike(sun));
        Assert.Contains("STAR", starPen.Texts);
        Assert.DoesNotContain("DERELICT", starPen.Texts);
    }

    /// <summary>
    /// THE PICKER IS THE BUG SITE. <c>ScopeView</c> can only draw the kind it is handed, so the law above
    /// proves nothing unless the page really classes the wreck as one. Driven through the real
    /// <c>ResolveScopeTarget</c> — the method that answers "point the glass at this id" — over every body
    /// in the scenario at once.
    /// </summary>
    [Fact]
    public void ThePageHandsTheGlassAWreckAndNeverMistakesAWorldForOne()
    {
        Pages.Map map = Booted();
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);

        var wrong = new List<string>();
        int wrecksSeen = 0;

        foreach (CelestialBody body in eph.Bodies)
        {
            object? resolved = Invoke(map, "ResolveScopeTarget", body.Id);
            Assert.True(resolved is not null, $"the page cannot point the scope at {body.Id} at all");

            var target = (ScopeView.Target)resolved!;
            bool wreck = Derelict.IsWreckBody(body.Id);
            if (wreck)
            {
                wrecksSeen++;
            }

            if (wreck != (target.Kind == ScopeView.TargetKind.Derelict))
            {
                wrong.Add($"  {body.Id} ({body.Name}) — IsWreckBody={wreck} but the scope is handed "
                    + $"{target.Kind}, which the corner tag prints as \"{TagFor(target)}\"");
            }
        }

        Assert.True(wrecksSeen > 0, "the page resolved no wreck at all — this bench read nothing");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} body/bodies reach the telescope as the wrong class:\n" + string.Join("\n", wrong));
    }

    /// <summary>
    /// THE RESERVED WORD IS NOT THIS INSTRUMENT'S TO SPEND (docs/worldbuilding-notes.md §8). Read off the
    /// glass for every body in the scenario plus the wreck, so a future portrait that reaches for the word
    /// as a corner tag trips here rather than quietly minting a second one.
    /// </summary>
    [Fact]
    public void TheGlassNeverSpeaksTheReservedWord()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        var said = new List<string>();

        foreach (CelestialBody body in eph.Bodies)
        {
            var pen = new RecordingPen();
            new ScopeView(pen).Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, ABodyLike(body));
            said.AddRange(pen.Texts);
        }

        var wreckPen = new RecordingPen();
        new ScopeView(wreckPen).Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, AWreckAt(1.7e11));
        said.AddRange(wreckPen.Texts);

        Assert.True(said.Count > 20, $"only {said.Count} string(s) came off the glass — nothing was scanned");
        Assert.DoesNotContain(said, s => s.Contains(TheReservedWord, StringComparison.OrdinalIgnoreCase));
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A wreck coasting along +X, so the sprite's heading is zero and the drawn hull is the Core
    /// outline scaled and centred with no rotation to unpick.</summary>
    private static ScopeView.Target AWreckAt(double x) => new(
        ScopeView.TargetKind.Derelict, "Derelict Roadster", null,
        new Vector2d(x, 0), new Vector2d(1_000, 0), 3, new RgbaColor(180, 180, 180), InPlasma: false);

    private static ScopeView.Target ABodyLike(CelestialBody body) => new(
        body.Kind == BodyKind.Station && Derelict.IsWreckBody(body.Id)
            ? ScopeView.TargetKind.Derelict
            : ScopeView.TargetKind.Body,
        body.Name, null, new Vector2d(body.OrbitRadius + 1e9, 0), new Vector2d(1_000, 0),
        body.BodyRadius, new RgbaColor(180, 180, 180), InPlasma: false,
        IsHaven: body.IsHaven, Dockable: DockableHavens.IsDockable(body));

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
            ? throw new InvalidOperationException("no scenarios/ directory above the test binary")
            : System.IO.Path.Combine(dir.FullName, "scenarios", file);
    }
}
