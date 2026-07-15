using System.Text.Json;
using SpaceSails.Core;
using SpaceSails.LabViz;
using LabVizCli = SpaceSails.LabViz.LabViz;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Tests for the lab visualization document (labs/SpaceSails.LabViz). The parity test (Test 3) is
/// the honesty gate: it recomputes body positions from the emitted scene using the exact formula
/// documented in <see cref="VizScene"/>'s XML docs and the viewer JS, and compares them to the real
/// <see cref="CircularOrbitEphemeris"/>.
/// </summary>
public class LabVizTests
{
    private static TrajectorySample[] Spiral(int count)
    {
        var samples = new TrajectorySample[count];
        for (int i = 0; i < count; i++)
        {
            double f = (double)i / (count - 1);
            double r = 1.4e11 * (1 + f);
            double a = f * 6 * Math.PI;
            double t = f * 1e7;
            samples[i] = new TrajectorySample(t, new Vector2d(r * Math.Cos(a), r * Math.Sin(a)));
        }

        return samples;
    }

    // Test 1: a scene with bodies/paths/markers round-trips and carries the required fields.
    [Fact]
    public void Json_RoundTrips_WithRequiredFields()
    {
        var scene = new VizScene("demo", "Demo Lab", "a subtitle");
        scene.AddBody(new CelestialBody("sun", "Sun", null, 1.3e20, 6.96e8, 0, 0, 0));
        scene.AddBody(new CelestialBody("earth", "Earth", "sun", 3.98e14, 6.37e6, 1.496e11, 3.15e7, 0));
        scene.AddPath("itinerary", Spiral(50), "#e8a33d", ghost: true);
        scene.AddMarker(0, new Vector2d(1.4e11, 0), "TCM-1 (12.4 m/s)", "burn");

        using JsonDocument doc = JsonDocument.Parse(scene.ToJson());
        JsonElement root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("schema").GetInt32());
        Assert.Equal("Demo Lab", root.GetProperty("title").GetString());
        Assert.Equal("a subtitle", root.GetProperty("subtitle").GetString());
        Assert.True(root.GetProperty("time").GetProperty("t0").GetDouble() <= root.GetProperty("time").GetProperty("t1").GetDouble());

        JsonElement body = root.GetProperty("bodies")[1];
        Assert.Equal("earth", body.GetProperty("id").GetString());
        Assert.Equal("sun", body.GetProperty("parentId").GetString());
        Assert.Equal(1.496e11, body.GetProperty("orbitRadius").GetDouble(), 3);

        JsonElement path = root.GetProperty("paths")[0];
        Assert.Equal("itinerary", path.GetProperty("label").GetString());
        Assert.True(path.GetProperty("ghost").GetBoolean());
        // Samples are compact [t, x, y] triples.
        Assert.Equal(3, path.GetProperty("samples")[0].GetArrayLength());

        JsonElement marker = root.GetProperty("markers")[0];
        Assert.Equal("burn", marker.GetProperty("kind").GetString());
        Assert.Equal("TCM-1 (12.4 m/s)", marker.GetProperty("label").GetString());
    }

    // Epoch: SetEpoch serializes an invariant round-trip UTC instant; without it, epoch is null.
    [Fact]
    public void Epoch_SerializesInvariantUtc_AndDefaultsToNull()
    {
        var scene = new VizScene("epoch", "Epoch");
        scene.AddPath("p", Spiral(10), "#ffa500");
        using (JsonDocument bare = JsonDocument.Parse(scene.ToJson()))
        {
            Assert.Equal(JsonValueKind.Null, bare.RootElement.GetProperty("epoch").ValueKind);
        }

        // Voyager 2's launch instant, offset -2h to prove normalization to UTC.
        scene.SetEpoch(new DateTimeOffset(1977, 8, 20, 16, 29, 0, TimeSpan.FromHours(2)));
        using JsonDocument doc = JsonDocument.Parse(scene.ToJson());
        string epoch = doc.RootElement.GetProperty("epoch").GetString()!;
        Assert.StartsWith("1977-08-20T14:29:00", epoch);
        var parsed = DateTimeOffset.Parse(epoch, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(TimeSpan.Zero, parsed.Offset);
    }

    // Test 2: an over-length path decimates to <= maxSamples, preserving first/last, times monotonic.
    [Fact]
    public void Decimation_CapsSamples_PreservesEndpoints_Monotonic()
    {
        TrajectorySample[] input = Spiral(10_000);
        var scene = new VizScene("dec", "Decimation");
        scene.AddPath("p", input, "#ffa500", maxSamples: 2500);

        using JsonDocument doc = JsonDocument.Parse(scene.ToJson());
        JsonElement samples = doc.RootElement.GetProperty("paths")[0].GetProperty("samples");

        int n = samples.GetArrayLength();
        Assert.True(n <= 2500, $"expected <= 2500, got {n}");

        // First and last input points preserved.
        Assert.Equal(input[0].SimTime, samples[0][0].GetDouble(), 6);
        Assert.Equal(input[0].Position.X, samples[0][1].GetDouble(), 6);
        Assert.Equal(input[^1].SimTime, samples[n - 1][0].GetDouble(), 6);
        Assert.Equal(input[^1].Position.X, samples[n - 1][1].GetDouble(), 6);

        // Times strictly non-decreasing.
        double prev = double.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            double t = samples[i][0].GetDouble();
            Assert.True(t >= prev, "times must be monotonic");
            prev = t;
        }
    }

    // Test 3 (the parity gate): positions recomputed from emitted body parameters using the
    // documented viewer formula — including a parent-chained moon AND a strongly eccentric comet
    // (Kepler rails, PR-B) AND an eccentric moon chained onto a circular planet — match
    // CircularOrbitEphemeris. The eccentric bodies are what prove the viewer's Kepler solve tracks Core.
    [Fact]
    public void EphemerisParity_ViewerFormula_MatchesCircularOrbitEphemeris()
    {
        CelestialBody[] bodies =
        [
            new("sun", "Sun", null, 1.327e20, 6.96e8, 0, 0, 0),
            new("earth", "Earth", "sun", 3.986e14, 6.371e6, 1.496e11, 6.283e7, 1.1),
            new("moon", "Moon", "earth", 4.903e12, 1.737e6, 3.844e8, 2.36e6, 2.7, BodyKind.Moon),
            // A comet on a highly eccentric sun orbit with a tilted periapsis.
            new("comet", "Comet", "sun", 1e12, 2e6, 2.7e11, 5.4e8, 0.4, BodyKind.Planet, false, null, 0.72, 1.3),
            // An eccentric moon riding a circular planet — exercises Kepler solve UNDER parent chaining.
            new("ecc-moon", "Ecc Moon", "earth", 1e12, 1e6, 5.0e8, 1.1e6, 5.9, BodyKind.Moon, false, null, 0.35, -0.6),
        ];
        var ephemeris = new CircularOrbitEphemeris(bodies);

        var scene = new VizScene("parity", "Parity");
        scene.AddBodies(bodies);

        // Parse the emitted body table exactly as the viewer would.
        using JsonDocument doc = JsonDocument.Parse(scene.ToJson());
        var byId = new Dictionary<string, JsonElement>();
        foreach (JsonElement b in doc.RootElement.GetProperty("bodies").EnumerateArray())
        {
            byId[b.GetProperty("id").GetString()!] = b;
        }

        double[] times = [0, 1e5, 1.234e6, 5e6, 3.1e7];
        foreach (double t in times)
        {
            foreach (string id in new[] { "sun", "earth", "moon", "comet", "ecc-moon" })
            {
                Vector2d expected = ephemeris.Position(id, t);
                (double x, double y) = ViewerPosition(byId, id, t);

                double scale = Math.Max(expected.Length, 1);
                Assert.Equal(expected.X, x, scale * 1e-6);
                Assert.Equal(expected.Y, y, scale * 1e-6);
            }
        }
    }

    // Test 3b (parity over the full real system): the whole sol.json body set — 22 bodies including
    // depth-2 parent chains (mercury-compute→mercury→sun, luna→earth→sun, europa→jupiter→sun) —
    // recomputed from the emitted scene with the viewer formula must match the real ephemeris.
    [Fact]
    public void EphemerisParity_FullSolSystem_MatchesCircularOrbitEphemeris()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        Assert.True(ephemeris.Bodies.Count >= 20, "expected the full sol.json body set");

        var scene = new VizScene("parity-sol", "Parity (Sol)");
        scene.AddBodies(ephemeris.Bodies);

        using JsonDocument doc = JsonDocument.Parse(scene.ToJson());
        var byId = new Dictionary<string, JsonElement>();
        foreach (JsonElement b in doc.RootElement.GetProperty("bodies").EnumerateArray())
        {
            byId[b.GetProperty("id").GetString()!] = b;
        }

        double[] times = [0, 3.7e5, 2.5e6, 4.4e7, 9.3e8];
        foreach (double t in times)
        {
            foreach (CelestialBody body in ephemeris.Bodies)
            {
                Vector2d expected = ephemeris.Position(body.Id, t);
                (double x, double y) = ViewerPosition(byId, body.Id, t);
                double scale = Math.Max(expected.Length, 1);
                Assert.Equal(expected.X, x, scale * 1e-6);
                Assert.Equal(expected.Y, y, scale * 1e-6);
            }
        }
    }

    // Test (fix 4): AddBody rejects a parentId that has not been added yet — the viewer's bodyPosition
    // recursion would otherwise dereference undefined and leave a dead black canvas.
    [Fact]
    public void AddBody_UnknownParent_Throws()
    {
        var scene = new VizScene("badparent", "Bad Parent");
        var ex = Assert.Throws<ArgumentException>(() =>
            scene.AddBody(new CelestialBody("moon", "Moon", "earth", 4.9e12, 1.7e6, 3.8e8, 2.3e6, 0, BodyKind.Moon)));
        Assert.Contains("earth", ex.Message);

        // Adding the parent first makes the child legal.
        scene.AddBody(new CelestialBody("earth", "Earth", null, 3.986e14, 6.37e6, 1.496e11, 3.15e7, 0));
        scene.AddBody(new CelestialBody("moon", "Moon", "earth", 4.9e12, 1.7e6, 3.8e8, 2.3e6, 0, BodyKind.Moon));
    }

    // Test (fix 5): a path with a non-finite sample serializes — truncated at the first blow-up —
    // rather than throwing from System.Text.Json (which cannot write NaN/Infinity).
    [Fact]
    public void AddPath_TruncatesAtFirstNonFiniteSample()
    {
        var scene = new VizScene("blowup", "Blow-up");
        TrajectorySample[] samples =
        [
            new(0, new Vector2d(1e11, 0)),
            new(1, new Vector2d(1.1e11, 1e10)),
            new(2, new Vector2d(double.NaN, double.PositiveInfinity)), // integrator diverged here
            new(3, new Vector2d(2e11, 2e10)),
        ];
        scene.AddPath("diverged", samples, "#ffa500");

        // ToJson must not throw, and the emitted path keeps only the finite prefix (2 points).
        using JsonDocument doc = JsonDocument.Parse(scene.ToJson());
        JsonElement path = doc.RootElement.GetProperty("paths")[0].GetProperty("samples");
        Assert.Equal(2, path.GetArrayLength());
        Assert.Equal(0.0, path[0][0].GetDouble(), 6);
        Assert.Equal(1.0, path[1][0].GetDouble(), 6);
    }

    // Test (fix 3, indirectly): a single-sample ghost path serializes cleanly (the JS bracket() guard
    // that prevents samples[-1] is exercised in the browser; here we at least prove the 1-sample scene
    // is well-formed and round-trips).
    [Fact]
    public void AddPath_SingleSample_Serializes()
    {
        var scene = new VizScene("ghost1", "One Sample");
        scene.AddPath("lonely", [new TrajectorySample(42, new Vector2d(1e11, 2e11))], "#ffa500", ghost: true);

        using JsonDocument doc = JsonDocument.Parse(scene.ToJson());
        JsonElement path = doc.RootElement.GetProperty("paths")[0];
        Assert.True(path.GetProperty("ghost").GetBoolean());
        JsonElement samples = path.GetProperty("samples");
        Assert.Equal(1, samples.GetArrayLength());
        Assert.Equal(42.0, samples[0][0].GetDouble(), 6);
        // A degenerate single-instant span still yields t0 <= t1 (t0+1 fallback).
        Assert.True(doc.RootElement.GetProperty("time").GetProperty("t0").GetDouble()
                 <= doc.RootElement.GetProperty("time").GetProperty("t1").GetDouble());
    }

    // Test (fix 10): AddMarker rejects an unknown kind and a non-finite time/position.
    [Fact]
    public void AddMarker_RejectsUnknownKind_AndNonFinite()
    {
        var scene = new VizScene("markers", "Markers");
        Assert.Throws<ArgumentException>(() =>
            scene.AddMarker(0, new Vector2d(1e11, 0), "bogus", "explosion"));
        Assert.Throws<ArgumentException>(() =>
            scene.AddMarker(double.NaN, new Vector2d(1e11, 0), "nan-time", MarkerKinds.Burn));
        Assert.Throws<ArgumentException>(() =>
            scene.AddMarker(0, new Vector2d(double.PositiveInfinity, 0), "inf-pos", MarkerKinds.Flyby));

        // The four known kinds are accepted.
        scene.AddMarker(0, new Vector2d(1e11, 0), "ok-burn", MarkerKinds.Burn);
        scene.AddMarker(1, new Vector2d(1e11, 0), "ok-flyby", MarkerKinds.Flyby);
        scene.AddMarker(2, new Vector2d(1e11, 0), "ok-closest", MarkerKinds.Closest);
        scene.AddMarker(3, new Vector2d(1e11, 0), "ok-event", MarkerKinds.Event);
    }

    // The documented viewer formula, in C#: parent chaining, mean anomaly M = initialPhase + 2*PI*t/period,
    // a zero-period body inherits its parent's position, e == 0 is the circular path, and e > 0 is the
    // Kepler ellipse (semi-major axis = orbitRadius, periapsis along argPeriapsis). This mirrors the
    // viewer.html JS exactly — it is the honesty gate that proves the viewer plots what Core computes.
    private static (double X, double Y) ViewerPosition(Dictionary<string, JsonElement> byId, string id, double t)
    {
        JsonElement b = byId[id];
        string? parentId = b.TryGetProperty("parentId", out JsonElement pe) && pe.ValueKind == JsonValueKind.String
            ? pe.GetString()
            : null;
        (double px, double py) = parentId is null ? (0.0, 0.0) : ViewerPosition(byId, parentId, t);

        double period = b.GetProperty("orbitPeriod").GetDouble();
        if (period == 0)
        {
            return (px, py);
        }

        double radius = b.GetProperty("orbitRadius").GetDouble();
        double phase = b.GetProperty("initialPhase").GetDouble();
        double m = phase + 2 * Math.PI * t / period;
        double e = b.TryGetProperty("eccentricity", out JsonElement ee) ? ee.GetDouble() : 0.0;
        if (e == 0.0)
        {
            return (px + radius * Math.Cos(m), py + radius * Math.Sin(m));
        }

        // Kepler solve — same seed/budget/tolerance as CircularOrbitEphemeris.SolveEccentricAnomaly.
        double reduced = Math.IEEERemainder(m, Math.Tau);
        double bigE = reduced + e * Math.Sin(reduced);
        for (int i = 0; i < 12; i++)
        {
            double delta = (bigE - e * Math.Sin(bigE) - reduced) / (1 - e * Math.Cos(bigE));
            bigE -= delta;
            if (Math.Abs(delta) < 1e-12) break;
        }

        double w = b.TryGetProperty("argPeriapsis", out JsonElement we) ? we.GetDouble() : 0.0;
        double ox = radius * (Math.Cos(bigE) - e);
        double oy = radius * Math.Sqrt(1 - e * e) * Math.Sin(bigE);
        return (px + Math.Cos(w) * ox - Math.Sin(w) * oy, py + Math.Sin(w) * ox + Math.Cos(w) * oy);
    }

    // Test 4: the HTML is self-contained — carries the spliced scene and no external-resource refs.
    [Fact]
    public void Html_IsSelfContained_WithSplicedScene()
    {
        var scene = new VizScene("selfcontained", "Self Contained");
        scene.AddBody(new CelestialBody("sun", "Sun", null, 1.3e20, 6.96e8, 0, 0, 0));
        scene.AddPath("orbit", Spiral(20), "#ffa500", ghost: true);

        string html = scene.ToHtml();

        Assert.Contains("const SCENE", html);
        Assert.Contains("\"schema\":1", html);
        Assert.Contains("Self Contained", html);
        // The splice token must be gone (replaced by real JSON), and the scene must be spliced
        // exactly once — a second copy (e.g. the token quoted in a template comment) would
        // silently double every generated file's size.
        Assert.DoesNotContain("/*__SCENE_JSON__*/null", html);
        Assert.Equal(html.IndexOf("\"schema\":1", StringComparison.Ordinal),
                     html.LastIndexOf("\"schema\":1", StringComparison.Ordinal));

        // No external-resource patterns.
        Assert.DoesNotContain("src=\"http", html);
        Assert.DoesNotContain("href=\"http", html);
        Assert.DoesNotContain("@import", html);
        Assert.DoesNotContain("fetch(", html);
    }

    // Test 5: LabViz.Wants parsing.
    [Fact]
    public void Wants_ParsesVizFlag()
    {
        Assert.True(LabVizCli.Wants(["--viz"]));
        Assert.True(LabVizCli.Wants(["a", "--viz", "--viz-out=out.html"]));
        Assert.True(LabVizCli.Wants(["--viz", "--viz-no-open"]));
        Assert.False(LabVizCli.Wants(["a", "b"]));
        Assert.False(LabVizCli.Wants([]));
    }
}
