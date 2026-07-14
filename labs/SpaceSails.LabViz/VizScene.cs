using System.Text.Json;
using SpaceSails.Core;

namespace SpaceSails.LabViz;

/// <summary>
/// A post-hoc visualization document for a gravity lab: celestial bodies (on analytic circular
/// rails), named trajectory polylines, and event markers. A lab builds one from the sample lists
/// it already computes, then <see cref="LabViz.Show"/> serializes it to JSON, splices it into the
/// embedded <c>viewer.html</c> template, and opens the result in a browser.
///
/// <para><b>Units.</b> SI meters/seconds, heliocentric ecliptic plane — identical to
/// <see cref="Vector2d"/>/<c>ShipState</c> in Core.</para>
///
/// <para><b>Body position convention (parity gate).</b> Body positions in the viewer are recomputed
/// analytically and MUST match <see cref="CircularOrbitEphemeris"/> exactly. The formula, applied
/// recursively through <c>parentId</c> chains, is:
/// <code>
///   parent = parentId is null ? (0, 0) : position(parentId, t)
///   if orbitPeriod == 0:  position = parent            // e.g. the sun sits at its parent/origin
///   else:                 angle    = initialPhase + 2*PI * t / orbitPeriod
///                         position = parent + (orbitRadius*cos(angle), orbitRadius*sin(angle))
/// </code>
/// The phase grows with time (prograde / counter-clockwise), the angle is measured from +X, and a
/// zero-period body inherits its parent's position. The same formula is documented in the viewer
/// JS comment and exercised by the ephemeris-parity unit test.</para>
/// </summary>
public sealed class VizScene
{
    /// <summary>The current scene JSON schema version emitted in the document.</summary>
    public const int Schema = 1;

    /// <summary>Default cap on samples per path; longer inputs are decimated to this length.</summary>
    public const int DefaultMaxSamples = 2500;

    // The splice token in viewer.html. It is a JS comment followed by a literal `null`, so the raw
    // template is valid JavaScript if opened un-spliced, and the exact string never appears inside
    // serialized JSON. The template must contain it exactly once (ToHtml enforces this — a stray
    // second occurrence, e.g. quoted in a template comment, would silently double the file size).
    private const string SceneToken = "/*__SCENE_JSON__*/null";

    // Well-known heliocentric bodies get a fixed hue matching the game's Map.razor BodyColor(id)
    // switch (~line 7249), so Earth is the same blue in every lab's pop-up. Unknown ids fall through
    // to the insertion-order palette below.
    private static readonly Dictionary<string, string> WellKnownBodyColors = new(StringComparer.Ordinal)
    {
        ["sun"] = "#ffd60a",      // RgbaColor(255,214,10)
        ["mercury"] = "#a0a0a0",  // RgbaColor(160,160,160)
        ["venus"] = "#e6c88c",    // RgbaColor(230,200,140)
        ["earth"] = "#4682e6",    // RgbaColor(70,130,230)
        ["mars"] = "#d2643c",     // RgbaColor(210,100,60)
        ["jupiter"] = "#d2aa78",  // RgbaColor(210,170,120)
        ["saturn"] = "#dcc896",   // RgbaColor(220,200,150)
        ["uranus"] = "#96dce6",   // RgbaColor(150,220,230)
        ["neptune"] = "#5a6ee6",  // RgbaColor(90,110,230)
    };

    // Fallback palette for bodies added without an explicit color and not in the well-known map,
    // cycled by insertion order so a system reads as distinguishable dots even before a lab bothers
    // to color them.
    private static readonly string[] DefaultBodyPalette =
        ["#f4d060", "#c9a15a", "#6fa8dc", "#e06666", "#b45f06", "#e8a33d", "#9fc5e8", "#76a5af", "#8e7cc3"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly List<BodyDoc> _bodies = [];
    private readonly List<PathDoc> _paths = [];
    private readonly List<MarkerDoc> _markers = [];

    /// <summary>Create a scene. <paramref name="slug"/> becomes the output filename (labviz/&lt;slug&gt;.html).</summary>
    public VizScene(string slug, string title, string? subtitle = null)
    {
        Slug = slug;
        Title = title;
        Subtitle = subtitle;
    }

    /// <summary>Output filename stem: the generated file is <c>labviz/&lt;slug&gt;.html</c>.</summary>
    public string Slug { get; }

    /// <summary>Overlay title shown in the viewer.</summary>
    public string Title { get; }

    /// <summary>Optional overlay subtitle.</summary>
    public string? Subtitle { get; }

    /// <summary>
    /// Record a body's circular-orbit parameters straight off a Core <see cref="CelestialBody"/>.
    /// The viewer animates its position analytically per the class-level formula.
    /// </summary>
    public void AddBody(CelestialBody body, string? colorHex = null)
    {
        // A parent must already be in the scene: the viewer's bodyPosition recursion chains through
        // parentId and would dereference undefined otherwise, leaving a dead black canvas. Same
        // contract (and message shape) as CircularOrbitEphemeris's constructor.
        if (body.ParentId is not null && !_bodies.Exists(b => b.Id == body.ParentId))
        {
            throw new ArgumentException(
                $"Body '{body.Id}' orbits unknown parent '{body.ParentId}' — add the parent first.", nameof(body));
        }

        string color = colorHex
            ?? (WellKnownBodyColors.TryGetValue(body.Id, out string? known)
                ? known
                : DefaultBodyPalette[_bodies.Count % DefaultBodyPalette.Length]);
        _bodies.Add(new BodyDoc(
            body.Id, body.Name, body.ParentId, body.BodyRadius,
            body.OrbitRadius, body.OrbitPeriod, body.InitialPhase, color));
    }

    /// <summary>Record a batch of bodies (e.g. an ephemeris' whole <see cref="ICelestialEphemeris.Bodies"/> list).</summary>
    public void AddBodies(IEnumerable<CelestialBody> bodies)
    {
        foreach (CelestialBody body in bodies)
        {
            AddBody(body);
        }
    }

    /// <summary>
    /// Add a trajectory polyline. <paramref name="group"/> is the legend grouping ("main", "sweep",
    /// ...); groups toggle together in the viewer. <paramref name="ghost"/> puts an interpolated
    /// ship dot on the path that the time scrubber animates. Samples beyond
    /// <paramref name="maxSamples"/> are decimated evenly, always preserving the first and last point.
    ///
    /// <para>The sample list is truncated at the first non-finite time or position (NaN/±Infinity),
    /// keeping only the finite prefix: a diverged integration (labs 02/03's deliberate blow-ups) draws
    /// up to the point where it exploded rather than crashing <c>System.Text.Json</c>, which cannot
    /// serialize NaN/Infinity.</para>
    /// </summary>
    public void AddPath(string label, IReadOnlyList<TrajectorySample> samples, string colorHex,
                        string group = "main", double width = 1.5, double opacity = 1.0,
                        bool ghost = false, int maxSamples = DefaultMaxSamples)
    {
        _paths.Add(new PathDoc(label, group, colorHex, width, opacity, ghost,
            Decimate(FinitePrefix(samples), maxSamples)));
    }

    /// <summary>
    /// Add a point event. <paramref name="kind"/> must be one of <see cref="MarkerKinds"/>
    /// ("burn" | "flyby" | "closest" | "event"), each selecting a distinct glyph; the label always
    /// renders regardless of zoom. Throws on a non-finite time/position or an unknown kind.
    /// </summary>
    public void AddMarker(double simTime, Vector2d position, string label, string kind)
    {
        if (!double.IsFinite(simTime) || !double.IsFinite(position.X) || !double.IsFinite(position.Y))
        {
            throw new ArgumentException(
                $"Marker '{label}' has a non-finite time or position (t={simTime}, x={position.X}, y={position.Y}).");
        }

        if (!MarkerKinds.IsKnown(kind))
        {
            throw new ArgumentException(
                $"Marker '{label}' has unknown kind '{kind}'; use one of MarkerKinds (burn|flyby|closest|event).", nameof(kind));
        }

        _markers.Add(new MarkerDoc(simTime, position.X, position.Y, label, kind));
    }

    /// <summary>Serialize the scene to the JSON document embedded in the viewer (schema v1).</summary>
    public string ToJson()
    {
        (double t0, double t1) = TimeSpan();
        SceneDoc doc = new(Schema, Title, Subtitle, new TimeDoc(t0, t1), _bodies, _paths, _markers);
        return JsonSerializer.Serialize(doc, JsonOptions);
    }

    /// <summary>Return the embedded viewer template with this scene's JSON spliced in.</summary>
    public string ToHtml()
    {
        string template = ReadTemplate();
        int at = template.IndexOf(SceneToken, StringComparison.Ordinal);
        if (at < 0 || template.IndexOf(SceneToken, at + SceneToken.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException("viewer.html must contain the scene token exactly once.");
        return string.Concat(template.AsSpan(0, at), ToJson(), template.AsSpan(at + SceneToken.Length));
    }

    // Infer the scrub window [t0, t1] from the path samples (the times the ship actually exists);
    // fall back to marker times, then to a unit window so the scrubber is never degenerate.
    private (double T0, double T1) TimeSpan()
    {
        double t0 = double.PositiveInfinity;
        double t1 = double.NegativeInfinity;

        foreach (PathDoc path in _paths)
        {
            foreach (double[] s in path.Samples)
            {
                t0 = Math.Min(t0, s[0]);
                t1 = Math.Max(t1, s[0]);
            }
        }

        if (double.IsInfinity(t0))
        {
            foreach (MarkerDoc m in _markers)
            {
                t0 = Math.Min(t0, m.T);
                t1 = Math.Max(t1, m.T);
            }
        }

        if (double.IsInfinity(t0))
        {
            return (0.0, 1.0);
        }

        return t0 == t1 ? (t0, t0 + 1.0) : (t0, t1);
    }

    // Return the leading run of samples whose time and position are all finite. A diverged
    // integration (labs 02/03) yields NaN/Infinity once it blows up; System.Text.Json cannot
    // serialize those, so we draw the honest finite prefix and stop at the blow-up. The common case
    // (all finite) returns the input unchanged with no allocation.
    private static IReadOnlyList<TrajectorySample> FinitePrefix(IReadOnlyList<TrajectorySample> samples)
    {
        int n = samples.Count;
        for (int i = 0; i < n; i++)
        {
            TrajectorySample s = samples[i];
            if (!double.IsFinite(s.SimTime) || !double.IsFinite(s.Position.X) || !double.IsFinite(s.Position.Y))
            {
                TrajectorySample[] prefix = new TrajectorySample[i];
                for (int k = 0; k < i; k++)
                {
                    prefix[k] = samples[k];
                }

                return prefix;
            }
        }

        return samples;
    }

    // Even-stride decimation to at most maxSamples points, preserving the first and last sample and
    // keeping times monotonic (indices are non-decreasing). Mirrors the game's stride-decimation
    // intent (Map.razor DrawWorldPolyline) but done once at scene-build time.
    private static double[][] Decimate(IReadOnlyList<TrajectorySample> samples, int maxSamples)
    {
        int n = samples.Count;
        if (n == 0)
        {
            return [];
        }

        if (maxSamples < 2)
        {
            maxSamples = 2;
        }

        if (n <= maxSamples)
        {
            double[][] all = new double[n][];
            for (int i = 0; i < n; i++)
            {
                all[i] = [samples[i].SimTime, samples[i].Position.X, samples[i].Position.Y];
            }

            return all;
        }

        double[][] result = new double[maxSamples][];
        for (int k = 0; k < maxSamples; k++)
        {
            // Maps k=0 -> 0 and k=maxSamples-1 -> n-1 exactly, so endpoints are preserved.
            int idx = (int)((long)k * (n - 1) / (maxSamples - 1));
            result[k] = [samples[idx].SimTime, samples[idx].Position.X, samples[idx].Position.Y];
        }

        return result;
    }

    private static string ReadTemplate()
    {
        System.Reflection.Assembly asm = typeof(VizScene).Assembly;
        // Resource logical name is "<RootNamespace>.viewer.html"; match by suffix to stay robust to
        // any default-namespace change.
        string resourceName = Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("viewer.html", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Embedded viewer.html resource not found in SpaceSails.LabViz.");

        using Stream stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not open embedded resource '{resourceName}'.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    // ---- JSON document shape (schema v1). camelCase policy turns these into the documented keys. ----

    private sealed record SceneDoc(
        int Schema,
        string Title,
        string? Subtitle,
        TimeDoc Time,
        IReadOnlyList<BodyDoc> Bodies,
        IReadOnlyList<PathDoc> Paths,
        IReadOnlyList<MarkerDoc> Markers);

    private sealed record TimeDoc(double T0, double T1);

    private sealed record BodyDoc(
        string Id,
        string Name,
        string? ParentId,
        double BodyRadius,
        double OrbitRadius,
        double OrbitPeriod,
        double InitialPhase,
        string Color);

    private sealed record PathDoc(
        string Label,
        string Group,
        string Color,
        double Width,
        double Opacity,
        bool Ghost,
        double[][] Samples);

    private sealed record MarkerDoc(double T, double X, double Y, string Label, string Kind);
}
