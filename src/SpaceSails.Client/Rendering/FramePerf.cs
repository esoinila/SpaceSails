using System.Diagnostics;
using System.Globalization;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #841 / Lab 46 · WHAT A DRAW COSTS — the stopwatch the walked view carries when, and only when, it is
/// asked to.
///
/// <para><b>Why this exists.</b> Lab 45 measured the SIM side of #841 to the microsecond and closed with a
/// sentence it could not act on: <i>"if culling is worth doing it has to be justified on DRAW cost, and
/// this lab could not measure draw cost."</i> There is no headless path to a canvas and this repo's own
/// standing law says a timing taken from an MCP-driven tab is invalid — the tab is <c>document.hidden</c>,
/// so rAF is throttled and the timers are clamped. The only honest instrument is one the GAME carries, read
/// by a human in a foreground tab. This is that instrument.</para>
///
/// <para><b>Where the seam is.</b> #870 lane 7b cut <see cref="DeckView.Draw"/> into named passes and wrote
/// "THE ORDER IS THE PICTURE" over them. That conductor is the natural place to put a clock: one timestamp
/// after each pass, and a pass's cost is the gap to the one before it. Nothing is wrapped, nothing is
/// re-entered, and the passes are timed under their own names — so the answer #841 actually needs (is the
/// FURNITURE the expensive part, or is it the flush?) is read straight off the table instead of inferred.
/// </para>
///
/// <para><b>Zero cost when it is off.</b> <see cref="DeckView"/> holds this as a nullable field and every
/// mark site is <c>_perf?.Mark(…)</c> — one null check per pass per frame, no allocation, no call into
/// <see cref="Stopwatch"/> at all. The probe is created only by <c>?perf=1</c>, and nothing else in the
/// client holds a reference to it. (It deliberately lives HERE and not on the <c>Map</c> component: #905's
/// frame ledger sweeps every instance field of that component into a pinned hash, and a rolling window of
/// wall-clock readings is precisely the kind of field that would make thirty pinned fingerprints a coin
/// toss. <see cref="DeckView"/> is on that sweep's <c>NotFingerprinted</c> list, so state kept here is
/// invisible to it by construction rather than by an exception somebody had to add.)</para>
///
/// <para><b>What the clock can and cannot resolve.</b> <see cref="Stopwatch.GetTimestamp"/> works in WASM,
/// but in a browser it is ultimately <c>performance.now()</c>, which is deliberately coarsened against
/// timing attacks — 5 µs on a cross-origin-isolated page and as much as 100 µs on an ordinary one. A single
/// pass of a single frame is therefore NOT a reading. The rolling window is: 120 frames of a quantised
/// clock recover a mean far finer than one tick, because the quantisation dithers against a duration that
/// moves. Read the means. Treat any single-frame max under one tick as noise, and say so in the lab.</para>
/// </summary>
public sealed class FramePerf
{
    /// <summary>How many frames a reading is averaged over — two seconds at 60 fps, which is also how often
    /// the console line is printed. Long enough to dither a coarse clock, short enough that walking into a
    /// furnished room changes the number while you are still standing in it.</summary>
    public const int Window = 120;

    /// <summary>The one greppable prefix. The owner copies the console straight into Lab 46's table, so the
    /// shape of this line is a contract and the guard next door parses it.</summary>
    public const string Tag = "[perf]";

    /// <summary>The flush of the batched command buffer across the JS boundary —
    /// <see cref="CanvasRenderer.EndFrame"/>, which is the ONE line of the frame that reaches the canvas.
    /// Marked like a pass because that is exactly what the reader wants it beside.</summary>
    public const string FlushRow = "FlushToTheCanvas";

    /// <summary>Everything <see cref="DeckView.Draw"/> did, flush included.</summary>
    public const string DrawTotalRow = "TOTAL_Draw";

    /// <summary>…and the walked frame around it: the shudder offset and the whole surface HUD the page
    /// builds before it can call Draw at all. Equal to <see cref="DrawTotalRow"/> when nobody opened it
    /// (a bench that calls Draw directly), which is honest rather than zero.</summary>
    public const string WalkFrameTotalRow = "TOTAL_DrawWalkFrame";

    /// <summary>How often the on-screen line is recomputed — four times a second, so the HUD moves while
    /// you walk. The console line stays on the full <see cref="Window"/>.</summary>
    private const int HudEvery = 15;

    private readonly Action<string> _say;
    private readonly List<string> _rows = [];
    private readonly List<double[]> _samples = [];
    private readonly List<double> _thisFrame = [];

    private long _walkFrameOpenedAt;
    private bool _walkFrameOpen;
    private long _drawBeganAt;
    private long _lastMarkAt;
    private int _cursor;
    private int _slot;
    private int _frames;

    public FramePerf(Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(say);
        _say = say;
    }

    /// <summary>The rows, in the order the conductor marks them: every pass, then the flush, then the two
    /// totals. Empty until the first frame has been drawn.</summary>
    public IReadOnlyList<string> Rows => _rows;

    /// <summary>The conductor's own passes — everything before the flush. This is the list the guard
    /// compares against the source of <see cref="DeckView.Draw"/>: a pass renamed in the conductor and not
    /// at its mark would show up here under the old name, which is a probe labelling a cost with the name
    /// of code that did not run.</summary>
    public IReadOnlyList<string> PassNames
    {
        get
        {
            int flush = _rows.IndexOf(FlushRow);
            return flush < 0 ? _rows : _rows.GetRange(0, flush);
        }
    }

    /// <summary>How many complete frames have gone into the window (never more than <see cref="Window"/>
    /// are kept, but this keeps counting so the console line can say how long it has been watching).</summary>
    public int Frames => _frames;

    /// <summary>The line the HUD paints. Empty until the first window has something in it.</summary>
    public string HudLine { get; private set; } = "";

    /// <summary>The page is starting a walked frame: everything from here to the end of
    /// <see cref="DeckView.Draw"/> is the draw side of the frame. Optional — a caller that only has a
    /// <see cref="DeckView"/> may skip it, and the walked-frame total then reads as the Draw total.</summary>
    public void OpenWalkFrame()
    {
        _walkFrameOpenedAt = Stopwatch.GetTimestamp();
        _walkFrameOpen = true;
    }

    /// <summary>The conductor is about to lay the first pass down.</summary>
    public void BeginDraw()
    {
        _drawBeganAt = Stopwatch.GetTimestamp();
        _lastMarkAt = _drawBeganAt;
        _cursor = 0;
        _thisFrame.Clear();
    }

    /// <summary>One pass is done. <paramref name="pass"/> is written out at the mark site as a LITERAL and
    /// not as <c>nameof</c>, on purpose: the guard reads the conductor's source for the methods it actually
    /// calls and compares them with these strings, so renaming a pass without renaming its mark is red.</summary>
    public void Mark(string pass)
    {
        long now = Stopwatch.GetTimestamp();
        double ms = (now - _lastMarkAt) * 1000.0 / Stopwatch.Frequency;
        _lastMarkAt = now;
        Record(pass, ms);
    }

    /// <summary>The frame is finished. Rolls the window, refreshes the HUD line on its own cadence, and
    /// prints the console block every <see cref="Window"/> frames.</summary>
    public void CloseDraw()
    {
        long now = Stopwatch.GetTimestamp();
        Record(DrawTotalRow, (now - _drawBeganAt) * 1000.0 / Stopwatch.Frequency);
        Record(WalkFrameTotalRow,
            (now - (_walkFrameOpen ? _walkFrameOpenedAt : _drawBeganAt)) * 1000.0 / Stopwatch.Frequency);
        _walkFrameOpen = false;

        // A frame that marked fewer rows than a previous one (a pass behind an `if`, moved) must not leave
        // a stale reading standing in the window under its name.
        for (int i = _cursor; i < _rows.Count; i++)
        {
            _samples[i][_slot] = 0;
        }

        _slot = (_slot + 1) % Window;
        _frames++;

        if (_frames % HudEvery == 0)
        {
            HudLine = ComposeTheHudLine();
        }

        if (_frames % Window == 0)
        {
            foreach (string line in TheConsoleBlock())
            {
                _say(line);
            }
        }
    }

    /// <summary>Mean, 95th percentile and worst of one row, in milliseconds, over the frames in the window.
    /// A row nobody has marked yet reads as all zeroes rather than throwing.</summary>
    public Reading Read(string row)
    {
        int at = _rows.IndexOf(row);
        return at < 0 ? default : Read(at);
    }

    /// <inheritdoc cref="Read(string)"/>
    public Reading Read(int index)
    {
        if (index < 0 || index >= _samples.Count)
        {
            return default;
        }

        int n = Math.Min(_frames, Window);
        if (n == 0)
        {
            return default;
        }

        double[] held = _samples[index];
        var sorted = new double[n];
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            sorted[i] = held[i];
            sum += held[i];
        }
        Array.Sort(sorted);

        int p95 = (int)Math.Ceiling(0.95 * n) - 1;
        return new Reading(sum / n, sorted[Math.Clamp(p95, 0, n - 1)], sorted[n - 1], n);
    }

    /// <summary>The exact block the owner copies out of the console and into Lab 46's table. One line per
    /// row, fixed shape, never localised — three decimals is a microsecond, which is finer than the browser
    /// clock can resolve and therefore leaves nothing of the reading on the floor.</summary>
    public IReadOnlyList<string> TheConsoleBlock()
    {
        var lines = new List<string>(_rows.Count + 1)
        {
            string.Create(CultureInfo.InvariantCulture,
                $"{Tag} window={Math.Min(_frames, Window)} frames={_frames} rows={_rows.Count}"),
        };
        for (int i = 0; i < _rows.Count; i++)
        {
            Reading r = Read(i);
            lines.Add(string.Create(CultureInfo.InvariantCulture,
                $"{Tag} pass={_rows[i]} mean={r.MeanMs:F3} p95={r.P95Ms:F3} max={r.MaxMs:F3}"));
        }
        return lines;
    }

    /// <summary>The furniture's share of the whole draw — #841's actual question, worked out once here so
    /// the HUD, the console and the lab's decision rule cannot disagree about it. The three passes are the
    /// ones that lay fixtures, seating and consoles down; they are named as literals for the same reason
    /// the marks are.</summary>
    public static readonly string[] TheFurniturePasses =
        ["FillTheFurniture", "DrawTheSeats", "DrawTheConsoles"];

    /// <inheritdoc cref="TheFurniturePasses"/>
    public double FurnitureShare()
    {
        double total = Read(DrawTotalRow).MeanMs;
        if (total <= 0)
        {
            return 0;
        }
        double furniture = 0;
        foreach (string pass in TheFurniturePasses)
        {
            furniture += Read(pass).MeanMs;
        }
        return furniture / total;
    }

    private void Record(string row, double ms)
    {
        if (_cursor == _rows.Count)
        {
            _rows.Add(row);
            _samples.Add(new double[Window]);
        }
        else if (!string.Equals(_rows[_cursor], row, StringComparison.Ordinal))
        {
            // The conductor changed shape under us (a pass added, removed or reordered between frames).
            // Start the window again rather than file a reading under somebody else's name — the fifth
            // bug class, aimed at a measurement: a number that cannot tell which code it timed.
            _rows.RemoveRange(_cursor, _rows.Count - _cursor);
            _samples.RemoveRange(_cursor, _samples.Count - _cursor);
            _rows.Add(row);
            _samples.Add(new double[Window]);
        }

        _samples[_cursor][_slot] = ms;
        _cursor++;
    }

    private string ComposeTheHudLine()
    {
        Reading whole = Read(WalkFrameTotalRow);
        Reading draw = Read(DrawTotalRow);
        Reading flush = Read(FlushRow);

        // The three dearest passes, so the line says WHERE the time went and not merely how much.
        var worst = new List<(string Row, double Mean)>();
        foreach (string pass in PassNames)
        {
            worst.Add((pass, Read(pass).MeanMs));
        }
        worst.Sort((a, b) => b.Mean.CompareTo(a.Mean));

        var sb = new System.Text.StringBuilder();
        sb.Append(string.Create(CultureInfo.InvariantCulture,
            $"PERF · frame {whole.MeanMs:F2} ms mean · {whole.P95Ms:F2} p95 · {whole.MaxMs:F2} max"));
        sb.Append(string.Create(CultureInfo.InvariantCulture,
            $" · draw {draw.MeanMs:F2} · flush {flush.MeanMs:F2} · furniture {FurnitureShare() * 100:F0}%"));
        sb.Append(" · dearest:");
        for (int i = 0; i < Math.Min(3, worst.Count); i++)
        {
            sb.Append(string.Create(CultureInfo.InvariantCulture,
                $" {worst[i].Row} {worst[i].Mean:F2}"));
        }
        sb.Append(string.Create(CultureInfo.InvariantCulture, $" · n={Math.Min(_frames, Window)}"));
        return sb.ToString();
    }

    /// <summary>One row's window, in milliseconds.</summary>
    public readonly record struct Reading(double MeanMs, double P95Ms, double MaxMs, int Frames);
}
