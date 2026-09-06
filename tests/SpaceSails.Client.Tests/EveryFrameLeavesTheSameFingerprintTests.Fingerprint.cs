using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · <b>WHAT A FINGERPRINT IS</b> — the three probes read off a driven page (the named ledger
/// readings, the sweep over every instance field, and the pen), and the one renderer that turns any value
/// into the same text twice.
///
/// <para>This is the half of the guard that decides what "the same mark" MEANS. The rendering rules below
/// are load-bearing in a way that is easy to miss: sets and dictionaries are written in SORTED order
/// because .NET randomises string hashing per process, and numbers are written to thirteen significant
/// digits because two C runtimes may disagree about the last bit of <c>Math.Sin</c>. Change either and
/// every pinned row moves for a reason that has nothing to do with the game.</para>
///
/// <para><see cref="NotFingerprinted"/> and <see cref="AWallClockAndNothingElse"/> live here too: the
/// exclusions belong beside the sweep that honours them, not in a header a reader has to hold in mind.</para>
/// </summary>
public sealed partial class EveryFrameLeavesTheSameFingerprintTests
{
    // ── THE FINGERPRINT ───────────────────────────────────────────────────────────────────────────────

    private static Reading Fingerprint(World world, Sequence sequence, Pages.Map map, RecordingPen pen,
        string? stoppedAt)
    {
        var rows = new List<(string Probe, string Value)>
        {
            (StoppedAtProbe, stoppedAt ?? "ran to the end of the frame"),
        };

        // ── THE LEDGER: what the frame is for, read by name ───────────────────────────────────────────
        foreach ((string name, string member) in TheLedger)
        {
            string reading = Render(Read(map, member), 1, []);
            // A reading nobody could read is not a ledger line. Past a screenful it keeps its head —
            // enough to see WHAT it is — and folds the rest into a hash, which is all the length was
            // ever doing for us.
            if (reading.Length > 160)
            {
                reading = $"{reading[..120]}… {reading.Length} chars, sha256 {Sha256(reading)[..16]}";
            }
            rows.Add((name, reading));
        }

        // ── THE SWEEP: and nothing at all escaped ─────────────────────────────────────────────────────
        var swept = new StringBuilder();
        var roster = new List<(string Field, string Type)>();
        int fields = 0;
        foreach (FieldInfo f in typeof(Pages.Map).GetFields(Hidden).OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            if (f.IsStatic || NotFingerprinted.Contains(f.FieldType.Name)
                || AWallClockAndNothingElse.Contains(f.Name))
            {
                continue;
            }
            fields++;
            roster.Add((AsWritten(f.Name), PinLedger.TypeLabel(f.FieldType)));
            // #870 lane 6c · the walk starts with the COMPONENT already on its own path. A
            // collaborator the page hands ITSELF to (Map.Seating takes an ISeatHost) keeps a reference
            // back to the page, and that reference is not a reading: it is the very object this loop is
            // already sweeping. Seeding the path is what stops the walk re-entering the whole component
            // from inside one of its own fields -- and it is why these hashes are still the ones #905
            // captured on the old code rather than a re-baseline: with it, all thirty pinned texts are
            // byte-identical.
            swept.Append(AsWritten(f.Name)).Append('=').Append(Render(f.GetValue(map), 1, [map])).Append('\n');
        }
        rows.Add((SweepProbe, $"{fields} fields, sha256 {Sha256(swept.ToString())}"));
        if (Environment.GetEnvironmentVariable("SPACESAILS_SWEEP_DUMP") is { } dumpDir)
        {
            Directory.CreateDirectory(dumpDir);
            File.WriteAllText(Path.Combine(dumpDir, $"{world}.{sequence}.sweep.txt"), swept.ToString());
        }

        // ── THE PEN: and the picture agrees with it ───────────────────────────────────────────────────
        rows.Add((PenProbe, $"{pen.Commands} calls, sha256 {pen.Sha256()}"));
        rows.Add((BufferProbe, TheCanvasBuffer(map)));
        return new Reading(rows, roster);
    }

    /// <summary>What the frame is FOR, read by name so a red run points at a thing a person can picture.
    /// Every phase of <c>OnTick</c> writes at least one of these.</summary>
    private static readonly (string Name, string Member)[] TheLedger =
    [
        ("the real clock",       "_lastTimestampMs"),
        ("the frame's now",      "_frameNowMs"),
        ("the FrameGap clock",   "_frameGapSeconds"),
        ("the hold, said",       "_heldControlsSaid"),
        ("the accumulator",      "_simAccumulator"),
        ("the sim clock",        "SimTime"),
        ("the ship",             "_ship"),
        ("warp asked",           "Warp"),
        ("warp effective",       "_effectiveWarp"),
        ("nearest body",         "_nearestBody"),
        ("nearest body at",      "_nearestBodyPosition"),
        ("nearest body moving",  "_nearestBodyVelocity"),
        ("the pursuit trail",    "_pursuitTrail"),
        ("this frame's drag",    "_frameMaxDragDecel"),
        ("the next sweep",       "_nextSweepSimTime"),
        ("the next projection",  "_nextProjectionSimTime"),
        ("the trajectory",       "_samples"),
        ("the closest pass",     "_closestPass"),
        ("the armable pass",     "_armablePass"),
        ("the long haul",        "_longHaulReach"),
        ("the pulse",            "_pulse"),
        ("arcing",               "_wasArcing"),
        ("the camera",           "_camera"),
        ("the HUD's last paint", "_lastHudUpdateMs"),
        ("the avatar",           "_avatarX"),
        ("the avatar (y)",       "_avatarY"),
        ("the avatar's heading", "_avatarHeading"),
        ("the route",            "_autoWalk"),
        // #904 (lane 6b) moved the seat's state into Seating; the ledger reads it through the page's own
        // forwarder, which hands back the very same TableTalk — so the thirty pinned digests are unchanged.
        ("the seat",             "SeatedTable"),
        ("the nerve",            "_nerve"),
        ("the tracker",          "_chirp"),
        ("the guards",           "_guards"),
        ("the patrol clock",     "_patrolFloorSeconds"),
        ("the traffic",          "_npcStates"),
        ("the hunters",          "_hunters"),
        ("the ordnance",         "_ordnance"),
        ("the ghost",            "_beaconGhost"),
        ("the surface",          "_surface"),
        ("the pulse cooldown",   "_lastPulseSimTime"),
    ];

    /// <summary>The machinery the frame reads but is not itself: the world model, the integrators, the views
    /// and the injected browser services. Excluded BY TYPE rather than by name, so a renamed field cannot slip
    /// a whole subsystem out of the sweep.</summary>
    private static readonly HashSet<string> NotFingerprinted = new(StringComparer.Ordinal)
    {
        // The world and its integrators — constant for the whole run, and enormous.
        nameof(ICelestialEphemeris), "CircularOrbitEphemeris", nameof(Simulator),
        // The pen and the views — fingerprinted separately, as the pen.
        nameof(CanvasRenderer), nameof(DeckView), nameof(ShuttleFlightView), nameof(ScopeView),
        // Browser/framework plumbing that has no state of the frame's in it.
        "HttpClient", "NavigationManager", "IJSRuntime", "ElementReference",
        "CancellationTokenSource", "CancellationToken",
    };

    /// <summary>
    /// THE ONE FIELD THAT CANNOT BE FINGERPRINTED, and the reason, written down.
    ///
    /// <para><c>_frameServicedAtMs</c> is <c>Environment.TickCount64</c> — the wall clock, taken inside
    /// <c>MarkFrameServiced</c>, and #825's own doc says why it must be: in a starved tab the pointer event
    /// and the animation callback are two queued jobs, so a click needs to know how long ago the last frame
    /// was, not merely how long that frame took. There is nothing to seed and no clock to hand in; the bench
    /// would be fingerprinting the minute it happened to run.</para>
    ///
    /// <para>Its CONSEQUENCES are all still pinned: <c>_frameGapSeconds</c> and <c>_heldControlsSaid</c> are
    /// both in the ledger, and the banner and the threshold they feed are #825's own subject, driven end to
    /// end by <see cref="TheStallSaysSoTests"/> next door. What is excluded is one raw wall stamp.</para>
    /// </summary>
    private static readonly HashSet<string> AWallClockAndNothingElse = new(StringComparer.Ordinal)
    {
        "_frameServicedAtMs",
    };

    /// <summary>Read a field or a property or call a no-argument method — the ledger names the component's own
    /// vocabulary, and some of it is a question rather than a field (the stall banner is one).</summary>
    private static object? Read(Pages.Map map, string member)
    {
        Type t = typeof(Pages.Map);
        if (PatrolState.TryFollow(map, member, out object? onTheRound)) return onTheRound;
        if (t.GetField(member, Hidden) is { } field) return field.GetValue(map);
        if (t.GetProperty(member, Hidden) is { } prop) return prop.GetValue(map);
        if (t.GetMethod(member, Hidden, Type.EmptyTypes) is { } call) return call.Invoke(map, null);
        throw new InvalidOperationException($"the component has no `{member}` — this ledger reads a dead name.");
    }

    /// <summary>What the flight frame actually drew, read straight out of the command buffer the flush was
    /// about to hand to JavaScript. Empty on a deck world, which never opens a map frame.</summary>
    private static string TheCanvasBuffer(Pages.Map map)
    {
        object? renderer = Get(map, "_renderer");
        if (renderer is null) return "∅";
        var buffer = (float[])renderer.GetType().GetField("_buffer", Hidden)!.GetValue(renderer)!;
        int length = (int)renderer.GetType().GetField("_length", Hidden)!.GetValue(renderer)!;
        var texts = (IEnumerable)renderer.GetType().GetField("_texts", Hidden)!.GetValue(renderer)!;

        var sb = new StringBuilder();
        for (int i = 0; i < length; i++) { sb.Append(Num(buffer[i])).Append(' '); }
        int labels = 0;
        foreach (object? label in texts) { labels++; sb.Append(Render(label, 1, [])).Append('\n'); }
        return $"{length} floats, {labels} labels, sha256 {Sha256(sb.ToString())}";
    }

    // ── RENDERING A VALUE THE SAME WAY TWICE ──────────────────────────────────────────────────────────

    private const int MaxDepth = 5;
    private const int MaxItems = 64;

    private static string Render(object? v, int depth, List<object> path)
    {
        if (v is null) return "∅";
        if (v is double d) return Num(d);
        if (v is float f) return Num(f);
        if (v is string s) return "\"" + s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
        if (v is bool b) return b ? "yes" : "no";

        Type t = v.GetType();
        if (t.IsEnum) return t.Name + "." + v;
        if (v is decimal dec) return dec.ToString(CultureInfo.InvariantCulture);
        if (t.IsPrimitive) return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "?";
        if (v is DateTime or DateTimeOffset or TimeSpan or Guid) return $"<a clock or an identity: {t.Name}>";
        if (v is Delegate) return $"<{t.Name}>";
        if (NotFingerprinted.Contains(t.Name)) return $"<not fingerprinted: {t.Name}>";
        if (depth >= MaxDepth) return $"<{t.Name} …>";
        foreach (object o in path) { if (ReferenceEquals(o, v)) return $"<already above: {t.Name}>"; }

        if (v is IDictionary pairs)
        {
            var rows = new List<string>();
            foreach (DictionaryEntry e in pairs)
            {
                rows.Add(Render(e.Key, depth + 1, path) + ": " + Render(e.Value, depth + 1, path));
            }
            rows.Sort(StringComparer.Ordinal);   // .NET randomises string hashing per process
            return "{" + Trim(rows, pairs.Count) + "}";
        }

        if (v is IEnumerable items)
        {
            var rows = new List<string>();
            int n = 0;
            foreach (object? item in items)
            {
                n++;
                if (rows.Count < MaxItems) rows.Add(Render(item, depth + 1, path));
            }
            if (IsUnordered(t)) rows.Sort(StringComparer.Ordinal);
            return "[" + Trim(rows, n) + "]";
        }

        FieldInfo[] fields = [.. t.GetFields(Hidden)
            .Where(x => !x.IsStatic)
            .OrderBy(x => x.Name, StringComparer.Ordinal)];
        path.Add(v);
        var fieldRows = new List<string>();
        foreach (FieldInfo x in fields)
        {
            object? held = x.GetValue(v);

            // #870 lane 6c · …and never a field that points BACK at something already on the path.
            // A back-reference is not a reading: it is an object this walk has passed through, and
            // following it -- or even printing a placeholder for it -- would make the fingerprint
            // depend on how a collaborator is wired to its host rather than on what the frame wrote.
            if (held is not null && path.Any(o => ReferenceEquals(o, held)))
            {
                continue;
            }
            fieldRows.Add(AsWritten(x.Name) + "=" + Render(held, depth + 1, path));
        }
        path.RemoveAt(path.Count - 1);
        return t.Name + "(" + string.Join(", ", fieldRows) + ")";
    }

    /// <summary>A property's backing field, written the way the property is.
    /// <c>&lt;Charge&gt;k__BackingField</c> is the compiler talking; <c>Charge</c> is what a reader of
    /// this ledger is looking for.</summary>
    private static string AsWritten(string field) =>
        field.StartsWith('<') && field.EndsWith(">k__BackingField", StringComparison.Ordinal)
            ? field[1..^16]
            : field;

    /// <summary>A set is a bag with no order, and .NET's per-process string hash randomisation means its
    /// enumeration order is not the same twice. Rendered sorted, or the hash would be a coin toss.</summary>
    private static bool IsUnordered(Type t) =>
        t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISet<>));

    private static string Trim(List<string> rows, int total) =>
        total <= MaxItems ? string.Join(", ", rows) : string.Join(", ", rows) + $", …of {total}";

    /// <summary>
    /// A number, written the same way on every machine that runs this.
    ///
    /// <para><c>G13</c>, not round-trip. The frame's arithmetic goes through <c>Math.Sin</c>,
    /// <c>Math.Cos</c>, <c>Math.Atan2</c> and <c>Math.Pow</c>, and the C runtime under those is glibc on the
    /// CI runner and UCRT on a developer's box — both correctly rounded to well under an ulp, but not
    /// guaranteed to agree in the last bit. Thirteen significant digits is four to five orders of magnitude
    /// clear of that noise and still finer than anything a reordered phase could hide in: the smallest real
    /// difference this bench can produce is one frame of ship motion, which is hundreds of metres on a
    /// position of 1.5e11.</para>
    /// </summary>
    private static string Num(double d)
    {
        if (double.IsNaN(d)) return "NaN";
        if (double.IsPositiveInfinity(d)) return "+∞";
        if (double.IsNegativeInfinity(d)) return "-∞";
        if (Math.Abs(d) < 1e-12) return "0";
        return d.ToString("G13", CultureInfo.InvariantCulture);
    }

    private static string Num(float f) => Num((double)f);
}
