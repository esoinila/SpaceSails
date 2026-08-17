using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #841 / Lab 46 · THE DRAW-COST PROBE TIMES THE CODE IT SAYS IT TIMES.
///
/// <para>Lab 45 closed #841's sim half at 0.36% of a frame and left the draw half open, because there is no
/// headless path to a canvas and a timing read out of an MCP-driven tab is invalid here by standing law.
/// <see cref="FramePerf"/> is the answer to that: a stopwatch the GAME carries, armed by <c>?perf=1</c>, read
/// by a human in a foreground tab. CI cannot measure a draw and this file does not pretend to — <b>every
/// number this guard sees is worthless</b>, taken on a runner with no canvas at all. What it CAN prove is
/// the thing that decides whether the owner's numbers mean anything:</para>
///
/// <list type="number">
///   <item><b>THE NAMES ARE THE CONDUCTOR'S.</b> The pass names the probe files its readings under are read
///   back out of <c>DeckView.Frame.cs</c>'s own conductor, in order. This is the fifth bug class aimed at a
///   measurement: a table that labels a cost with the name of code that did not run is worse than no table,
///   because it is a table somebody will act on. <b>RED PROOF</b> — rename one pass in the conductor and
///   leave its <c>_perf?.Mark("…")</c> literal alone (which is exactly why the marks are literals and not
///   <c>nameof</c>) and this fails naming both spellings. Done, verbatim, in the PR body.</item>
///   <item><b>EVERY ROW IS THERE, EVERY FRAME.</b> Passes behind an <c>if</c> are marked outside it, so the
///   table does not change shape between a ship deck and a hive floor. A row that came and went would make a
///   rolling window a comparison between different questions.</item>
///   <item><b>THE CONSOLE LINE PARSES.</b> Its shape is a contract — the owner copies it out of the browser
///   console and into the lab's table — so the exact regex the lab's recipe tells him to grep for is applied
///   here to the real output of a real run.</item>
///   <item><b>OFF MEANS OFF.</b> An unarmed <see cref="DeckView"/> draws a byte-identical transcript to an
///   armed one, and a fresh one has no probe at all. The probe may cost a null check; it may not cost a
///   mark.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class WhatADrawCostsTests
{
    private const int WidthPx = 1200, HeightPx = 700;

    /// <summary>The exact shape the lab's recipe tells the owner to grep for. If this regex and the lab's
    /// text ever part company, the recipe stops working on the day somebody needs it.</summary>
    private static readonly Regex ARow = new(
        @"^\[perf\] pass=(?<name>[A-Za-z0-9_]+) mean=(?<mean>-?\d+\.\d{3}) p95=(?<p95>-?\d+\.\d{3}) max=(?<max>-?\d+\.\d{3})$",
        RegexOptions.CultureInvariant);

    private static readonly Regex AHeader = new(
        @"^\[perf\] window=(?<window>\d+) frames=(?<frames>\d+) rows=(?<rows>\d+)$",
        RegexOptions.CultureInvariant);

    // ── The worlds this is driven on: a real furnished hive floor, and the ship (which is the one plan
    //    that has DressTheShip in it, so both sides of that `if` are walked). ────────────────────────────

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>#841's own floor: B1 of the head office, which is where the offices, the glazing, the park,
    /// the desks and the cubicles all are — the furniture the issue proposes to cull.</summary>
    private static DeckPlan TheFurnishedFloor() =>
        HiveInterior.FloorDeck("luna", -1, Field, 3, static (_, into) =>
        {
            (double x, double y) = HiveInterior.SpawnOn(MoonSurface.ExpeditionField());
            into[0] = new DeckPlan.Droid(x + 3, y + 2, 0.4, "PATROL 2");
            into[1] = new DeckPlan.Droid(x - 4, y + 1, 2.1, "Reever");
            into[2] = new DeckPlan.Droid(x + 5, y - 3, 1.0, "Collector");
        }, [], 0);

    /// <summary>A pen that records the frame exactly the way the split's own snapshot does, so "the probe
    /// changed no mark" can be asserted as a string comparison rather than hoped for.</summary>
    private sealed class RecordingPen : IRenderer
    {
        private readonly StringBuilder _log = new();

        public string Transcript => _log.ToString();

        private static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);

        private static string C(RgbaColor c) => $"{c.R},{c.G},{c.B},{c.A}";

        private static string C(RgbaColor? c) => c is { } k ? C(k) : "-";

        private static string Pts(ReadOnlySpan<float> xy)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < xy.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append(F(xy[i]));
            }
            return sb.ToString();
        }

        public void BeginFrame(int w, int h, RgbaColor bg) => _log.Append($"begin {w} {h} {C(bg)}\n");

        public void EndFrame() => _log.Append("end\n");

        public int RegisterImage(string url)
        {
            _log.Append($"register {url}\n");
            return url.Length;
        }

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            _log.Append($"circle {F(x)} {F(y)} {F(r)} {C(fill)} {C(stroke)} {F(w)}\n");

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            _log.Append($"polyline {C(stroke)} {F(w)} [{Pts(pts)}]\n");

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            _log.Append($"polygon {C(fill)} {C(stroke)} {F(w)} [{Pts(pts)}]\n");

        public void DrawText(float x, float y, string text, RgbaColor c,
                             string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            _log.Append($"text {F(x)} {F(y)} {C(c)} {align} <{font}> \"{text}\"\n");

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) =>
            _log.Append($"image {id} {F(x)} {F(y)} {F(w)} {F(h)} {F(a)}\n");

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) =>
            _log.Append($"slice {id} {F(sx)} {F(sy)} {F(sw)} {F(sh)} {F(x)} {F(y)} {F(w)} {F(h)} {F(a)}\n");
    }

    /// <summary>Draw <paramref name="frames"/> frames of one plan through an armed probe, and hand back the
    /// probe and everything it said. simTime moves, because a frame drawn at one frozen instant would walk
    /// only one branch of every animated pass.</summary>
    private static (FramePerf Perf, List<string> Said, string Transcript) Drive(
        DeckPlan plan, int frames, bool dark = false)
    {
        var pen = new RecordingPen();
        var view = new DeckView(pen);
        var said = new List<string>();
        FramePerf perf = view.ArmThePerfProbe(said.Add);

        (double sx, double sy) = (plan.SpawnX, plan.SpawnY);
        for (int i = 0; i < frames; i++)
        {
            perf.OpenWalkFrame();
            view.Draw(plan, WidthPx, HeightPx, 1000.0 + i,
                new DeckView.State(sx, sy, 0.3, 0, 0,
                    ShuttleAway: false, ElectricUniverse: false,
                    ShowNerve: true, Nerve: 58, NerveReadout: "FRAYED", Dark: dark));
        }

        return (perf, said, pen.Transcript);
    }

    // ── LAW 1 · THE NAMES ARE THE CONDUCTOR'S OWN ──────────────────────────────────────────────────────

    /// <summary>
    /// The probe's rows are the passes <see cref="DeckView.Draw"/> actually calls, in the order it calls
    /// them, then the flush, then the two totals — and the pass list is read out of the conductor's source
    /// rather than typed in here, so this cannot be kept green by editing the expectation.
    /// </summary>
    [Fact]
    public void TheRowsAreTheConductorsOwnPassesInItsOwnOrder()
    {
        string[] conductor = ThePassesTheConductorCalls();

        // Anti-vacuous, both ways: a regex that found nothing would make the comparison below trivially
        // true, and a probe with no rows would too.
        Assert.True(conductor.Length >= 15,
            $"the conductor's pass list read as {conductor.Length} name(s) — #870 lane 7b split Draw into "
            + "eighteen, so this guard is reading the wrong region of DeckView.Frame.cs and would pass over "
            + "a probe that timed nothing:" + Environment.NewLine + string.Join(", ", conductor));

        (FramePerf perf, _, _) = Drive(TheFurnishedFloor(), frames: 3);

        Assert.Equal(conductor, perf.PassNames.ToArray());

        // …and the three rows the conductor does not contain, in their fixed order at the end.
        Assert.Equal(
            [.. conductor, FramePerf.FlushRow, FramePerf.DrawTotalRow, FramePerf.WalkFrameTotalRow],
            perf.Rows.ToArray());

        // The furniture passes the decision rule is written against have to BE passes, or the share the
        // HUD prints is a silent zero — a threshold that selects everything, in this repo's own words.
        foreach (string pass in FramePerf.TheFurniturePasses)
        {
            Assert.Contains(pass, conductor, StringComparer.Ordinal);
        }
    }

    /// <summary>A pass behind an <c>if</c> is still a row on the floor where that <c>if</c> is false. The
    /// hive floor has no ship dressing and no lamp; the ship deck has dressing; a dark floor lays the dark.
    /// All three must produce the SAME table, or a rolling window is comparing two different questions.</summary>
    [Fact]
    public void EveryRowIsPresentOnEveryWorldEvenWhenItsPassDidNothing()
    {
        (FramePerf floor, _, _) = Drive(TheFurnishedFloor(), frames: 3);
        (FramePerf ship, _, _) = Drive(Scenes.Build("ship"), frames: 3);
        (FramePerf dark, _, _) = Drive(TheFurnishedFloor(), frames: 3, dark: true);

        Assert.Equal(floor.Rows.ToArray(), ship.Rows.ToArray());
        Assert.Equal(floor.Rows.ToArray(), dark.Rows.ToArray());

        // …and the rows whose passes are gated really are the gated ones, so the law above is not vacuous:
        // the ship deck is the only one of the three that dresses a ship, and the dark floor the only one
        // that lays the dark down.
        Assert.Contains("DressTheShip", floor.Rows, StringComparer.Ordinal);
        Assert.Contains("PaintTheDark", floor.Rows, StringComparer.Ordinal);
        Assert.True(Scenes.Build("ship").ShipFixtures, "the ship deck no longer carries ship fixtures.");
        Assert.False(TheFurnishedFloor().ShipFixtures, "a hive floor now claims ship fixtures.");
    }

    // ── LAW 2 · EVERY READING IS A NUMBER, AND THE WINDOW ROLLS ────────────────────────────────────────

    /// <summary>
    /// Nothing here asserts a DURATION — a runner with no canvas cannot measure a draw and this file says
    /// so out loud. What it asserts is that every row carries a real, finite, non-negative reading over a
    /// window that never grows past <see cref="FramePerf.Window"/>, and that no reading sits above the worst
    /// sample in its own window, which is the one arithmetic relation a broken percentile would break.
    /// </summary>
    [Fact]
    public void EveryRowReadsAsAFiniteNonNegativeWindow()
    {
        const int frames = FramePerf.Window + 37;
        (FramePerf perf, _, _) = Drive(TheFurnishedFloor(), frames);

        Assert.Equal(frames, perf.Frames);

        var faults = new List<string>();
        foreach (string row in perf.Rows)
        {
            FramePerf.Reading r = perf.Read(row);
            if (r.Frames != FramePerf.Window)
            {
                faults.Add($"{row}: window is {r.Frames} frames after {frames} were drawn");
            }
            if (!double.IsFinite(r.MeanMs) || !double.IsFinite(r.P95Ms) || !double.IsFinite(r.MaxMs)
                || r.MeanMs < 0 || r.P95Ms < 0 || r.MaxMs < 0)
            {
                faults.Add($"{row}: mean {r.MeanMs} p95 {r.P95Ms} max {r.MaxMs}");
            }
            // p95 ≤ max, and mean ≤ max. NOT mean ≤ p95: one cold first frame (a JIT warm-up here, a
            // shader compile or a GC in a browser) sits above the 95th percentile and drags the mean over
            // it, which is a real property of the data and not a broken percentile. It is worth knowing
            // that it happens — Lab 46's recipe says to read the means, and this is the shape of the tail
            // that can move one.
            if (r.P95Ms > r.MaxMs + 1e-9 || r.MeanMs > r.MaxMs + 1e-9)
            {
                faults.Add($"{row}: mean {r.MeanMs}, p95 {r.P95Ms}, max {r.MaxMs} — a percentile or a mean "
                    + "is above the worst sample in its own window");
            }
        }

        Assert.True(faults.Count == 0,
            "the probe filed a reading that is not a reading:" + Environment.NewLine
            + string.Join(Environment.NewLine, faults));

        // The whole walked frame contains the whole Draw, because the page opens the bracket first.
        Assert.True(perf.Read(FramePerf.WalkFrameTotalRow).MaxMs
            >= perf.Read(FramePerf.DrawTotalRow).MeanMs - 1e-9,
            "the walked frame reads as cheaper than the Draw inside it, so the bracket is round the wrong "
            + "code.");
    }

    // ── LAW 3 · THE CONSOLE LINE IS THE CONTRACT ───────────────────────────────────────────────────────

    /// <summary>
    /// The owner's whole workflow is: open the tab, walk about, copy the console into Lab 46's table. So the
    /// block is emitted once per full window, one line per row plus a header, and every line matches the
    /// exact regex the lab's recipe tells him to grep for.
    /// </summary>
    [Fact]
    public void TheConsoleBlockIsPrintedOncePerWindowAndParses()
    {
        (FramePerf perf, List<string> said, _) = Drive(TheFurnishedFloor(), FramePerf.Window * 2);

        // Two full windows in, two blocks out — and not one line before the first window closed.
        int rows = perf.Rows.Count;
        Assert.Equal(2 * (rows + 1), said.Count);

        var headers = said.Where(l => AHeader.IsMatch(l)).ToList();
        Assert.Equal(2, headers.Count);
        Assert.Equal(FramePerf.Window.ToString(CultureInfo.InvariantCulture),
            AHeader.Match(headers[0]).Groups["window"].Value);

        var namesSeen = new List<string>();
        foreach (string line in said)
        {
            if (AHeader.IsMatch(line))
            {
                continue;
            }
            Match m = ARow.Match(line);
            Assert.True(m.Success,
                $"a [perf] line does not match the shape Lab 46's recipe greps for: {line}");
            if (namesSeen.Count < rows)
            {
                namesSeen.Add(m.Groups["name"].Value);
            }
            Assert.True(double.Parse(m.Groups["mean"].Value, CultureInfo.InvariantCulture) >= 0);
        }

        // The names in the console are the rows, in the rows' order — not a re-typed list.
        Assert.Equal(perf.Rows.ToArray(), namesSeen.ToArray());

        // Anti-vacuous: the block that is being parsed is a real one with the passes in it.
        Assert.Contains("FillTheFurniture", namesSeen, StringComparer.Ordinal);
        Assert.Contains(FramePerf.FlushRow, namesSeen, StringComparer.Ordinal);
    }

    // ── LAW 4 · OFF MEANS OFF ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A fresh <see cref="DeckView"/> carries no probe, and a probe that IS armed changes not one mark on
    /// the glass. The first half is what makes "zero cost when off" true rather than claimed; the second is
    /// the instrument's own honesty — a measurement that changes the thing it measures is not one.
    /// </summary>
    [Fact]
    public void AnUnarmedViewHasNoProbeAndAnArmedOneDrawsTheSameFrame()
    {
        var bare = new DeckView(new RecordingPen());
        Assert.Null(bare.Perf);

        DeckPlan plan = TheFurnishedFloor();

        var quietPen = new RecordingPen();
        var quiet = new DeckView(quietPen);
        for (int i = 0; i < 4; i++)
        {
            quiet.Draw(plan, WidthPx, HeightPx, 1000.0 + i,
                new DeckView.State(plan.SpawnX, plan.SpawnY, 0.3, 0, 0,
                    ShuttleAway: false, ElectricUniverse: false,
                    ShowNerve: true, Nerve: 58, NerveReadout: "FRAYED"));
        }

        (_, _, string armed) = Drive(plan, frames: 4);

        Assert.Equal(quietPen.Transcript, armed);

        // Anti-vacuous: the frame being compared is a real, furnished one and not two empty strings.
        Assert.True(armed.Length > 50_000,
            $"the compared frame transcribes to {armed.Length} chars — a furnished B1 draws far more than "
            + "that, so this guard is comparing two nearly-empty frames and would pass over a probe that "
            + "painted all over the deck.");
    }

    // ── LAW 5 · THE DEV ROW AND ITS PROSE TWIN EXIST ───────────────────────────────────────────────────

    /// <summary>The probe is worth nothing if nobody can reach it. It is a button in the front door's dev
    /// list, it points at the furnished floor the issue is about, and <c>docs/testing-guide.md</c> — the
    /// prose twin of that catalogue — carries the key.</summary>
    [Fact]
    public void TheFrontDoorOffersThePerfRowAndTheGuideExplainsIt()
    {
        DevStarts.Entry row = Assert.Single(
            DevStarts.All, e => e.Url.Contains("perf=1", StringComparison.Ordinal));
        Assert.Contains("secretlab=deep", row.Url, StringComparison.Ordinal);
        Assert.Contains("floor=1", row.Url, StringComparison.Ordinal);

        string guide = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "testing-guide.md"));
        Assert.Contains("?perf=1", guide, StringComparison.Ordinal);
        Assert.Contains(row.Url, guide, StringComparison.Ordinal);

        // …and the lab it feeds, with its results table still asking to be filled from a foreground tab.
        string lab = File.ReadAllText(Path.Combine(
            RepoRoot(), "labs", "46-what-a-draw-costs", "README.md"));
        Assert.Contains(FramePerf.Tag, lab, StringComparison.Ordinal);
        Assert.Contains(row.Url, lab, StringComparison.Ordinal);
    }

    // ── The reader: the conductor, as it is actually written ───────────────────────────────────────────

    /// <summary>Every pass <see cref="DeckView.Draw"/> calls, in source order, read out of the region the
    /// conductor's own banner opens and <c>_mask.Disarm()</c> closes. Statement-level calls in that region
    /// begin with a capital; everything else there is a field (<c>_renderer</c>, <c>_perf</c>), a keyword or
    /// a comment.</summary>
    private static string[] ThePassesTheConductorCalls()
    {
        string source = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Rendering", "DeckView.Frame.cs"));

        int from = source.IndexOf("THE ORDER IS THE PICTURE", StringComparison.Ordinal);
        Assert.True(from > 0,
            "DeckView.Frame.cs no longer carries the conductor's banner (\"THE ORDER IS THE PICTURE\"), so "
            + "this guard cannot find the pass list it is meant to read.");

        int to = source.IndexOf("_mask.Disarm();", from, StringComparison.Ordinal);
        Assert.True(to > from, "the conductor no longer ends at _mask.Disarm().");

        return [.. Regex.Matches(source[from..to], @"(?m)^\s+([A-Z][A-Za-z0-9]*)\(")
            .Select(m => m.Groups[1].Value)];
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binaries.");
    }
}
