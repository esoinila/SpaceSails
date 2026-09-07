using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1135 · A SURFACE MAY NOT SWALLOW THE RE-RENDER.
///
/// <para><see cref="NoSurfaceSwallowsAWriteTests"/> guards the value going OUT of a surface. This guards the
/// paint coming BACK. They are two different holes in the same wall and the second one is the quieter of the
/// two, because the write does land: the page's field is correct within microseconds of the press and the
/// SCREEN goes on showing the old one.</para>
///
/// <para><b>The mechanism, measured rather than assumed — and it is not quite the one #1135 was filed on.</b>
/// Blazor re-renders the component that HANDLED the event, and every <c>@on…</c> binding funnels through
/// <c>EventCallbackFactory</c>, whose whole rule is one line:</para>
/// <code>
/// new EventCallback(callback?.Target as IHandleEvent ?? receiver as IHandleEvent, callback)
/// </code>
/// <para>Two candidates, in that order. <b>The delegate's own TARGET wins</b>, and <c>receiver</c> — the
/// component whose markup wrote the binding, i.e. the surface — is only the fallback. So:</para>
/// <list type="bullet">
/// <item>A page METHOD GROUP crossing as a bare <c>Action</c> (<c>StartSweep="StartSweep"</c> then
/// <c>@onclick="StartSweep"</c>) has the PAGE as its target, and a page is an <c>IHandleEvent</c>. The page
/// is the receiver already. So is a page lambda that captures only <c>this</c>, which the C# compiler emits
/// as an instance method on the page rather than a closure.</item>
/// <item>A lambda that captures a LOCAL — <c>@onclick="() =&gt; Drop(entry.ShipId)"</c> inside a
/// <c>@foreach</c>, which is what every per-row button in this repo is — has a compiler-generated display
/// class as its target. That is not an <c>IHandleEvent</c>, so the fallback takes it, and the fallback is
/// the SURFACE. <b>That</b> is the swallow, and changing the parameter's type cannot reach it: an
/// <c>EventCallback</c> built from the same lambda in the same markup lands on the same fallback.</item>
/// </list>
/// <para><b>This was checked in the bench, not reasoned about.</b> Put <c>ScopeControls.StartSweep</c> back
/// to <c>[Parameter] public Action</c> — #1134's own before-state — and
/// <c>ThePressPaintsOnItsOwnTickTests</c> stays GREEN: the press repaints the desk (component 11 in the
/// renderer's own batch). #1134's paragraph about that handler is the one thing in it that does not survive
/// being driven, and this law says so rather than inheriting it.</para>
///
/// <para><b>Why this law has two regimes and not one rule.</b> The repo has two decomposed pages and they
/// dispatch events in OPPOSITE ways, deliberately:</para>
/// <list type="bullet">
/// <item><b><c>Map.razor</c> switches the automatic re-render OFF</b> — <c>@implements IHandleEvent</c> with
/// <c>HandleEventAsync =&gt; callback.InvokeAsync(arg)</c> (Map.Sim.cs) — because the HUD's refresh belongs to
/// the rAF-driven 200 ms throttle in <c>OnTick</c> and not to every mouse press. All 117 surfaces under
/// <c>Pages/Map/</c> — 98 of them plus the 19 <c>NavHud</c> cut out of one of those — repeat that override,
/// which is what makes their crossings inert: with the suppression on
/// both sides, an <c>Action</c> and an <c>EventCallback</c> re-render exactly the same thing, which is
/// nothing. Where Map wants a repaint on the press it calls <c>StateHasChanged</c> in its own method, and it
/// does so in some seven hundred places.</item>
/// <item><b><c>TrackingPost.razor</c> leaves it ON</b> — no override, on the desk or on any of its eleven
/// surfaces. So on the desk, and only on the desk, who the receiver is decides what the glass says.</item>
/// </list>
///
/// <para>That difference decides where a swallow can HURT, never where the crossing is allowed to be sloppy.
/// Clause 2 is therefore stated over both pages and all 128 surfaces: it costs nothing, and what it buys is
/// that the guarantee stops depending on <b>what the page happens to pass</b>. An <c>Action</c> parameter
/// lands on the page only while the page keeps handing it a method group; the day someone passes a closure
/// instead, the receiver silently becomes the surface and nothing anywhere says so. An
/// <c>EventCallback</c> parameter is bound at the PAGE's own <c>Create(this, …)</c>, so the page is pinned as
/// receiver at the boundary and cannot be lost further down. Clause 3 is stated only where a swallow can be
/// seen — a page that has said "no press repaints me" has already answered the question.</para>
///
/// <para><b>The three clauses.</b>
/// <list type="number">
/// <item><see cref="EverySurfaceDispatchesEventsTheWayItsPageDoes"/> — a surface either repeats its page's
/// <c>IHandleEvent</c> suppression or, like its page, has none. A MISMATCH is the real hazard: a surface that
/// re-renders itself on a press while the page that owns the state never hears about it is precisely how one
/// half of a desk ends up showing a different world from the other half.</item>
/// <item><see cref="EveryHandlerBoundStraightToAnEventCrossesAsAnEventCallback"/> — on EVERY surface, a
/// parameter wired directly (<c>@onclick="StartSweep"</c>) must be
/// <c>EventCallback</c>/<c>EventCallback&lt;T&gt;</c>, so the receiver is pinned by the TYPE and not by the
/// page's habits. This costs nothing: razor binds an EventCallback to <c>@onclick</c> as readily as an
/// Action, so not one character of markup changes — 126 of them crossed here, across 38 of the Map's
/// surfaces (104 <c>Action</c>, 6 <c>Action&lt;T&gt;</c>, 16 <c>Func&lt;…Task&gt;</c>), and the client built
/// with zero errors and zero warnings on the first attempt because there was nothing else to change.</item>
/// <item><see cref="EveryHandlerReachedFromALambdaLeavesThePagePainting"/> — the live one. Where the page has
/// NOT suppressed, a parameter reached from a markup LAMBDA (<c>@onclick="() =&gt; Drop(id)"</c>) has a
/// closure for a target and so lands on the surface, and no parameter type can move it: the fix would be
/// rewriting moved markup, which is the one thing the decomposition promised not to do. So the other end
/// answers for it — the page's method of that name must call <c>StateHasChanged</c>. Before the cut that
/// button was ON the page and the press re-rendered the page; this is that, restored, and it is the
/// follow-up #1134 asked for in its own found-not-fixed.</item>
/// </list></para>
///
/// <para><b>What counts as a handler.</b> A delegate parameter that hands the markup nothing back:
/// <c>Action</c>, <c>Action&lt;T…&gt;</c>, <c>Func&lt;…, Task&gt;</c>. Those exist to DO something, and doing
/// something is what a paint follows. A <c>Func&lt;bool&gt;</c> or <c>Func&lt;double, string&gt;</c> is a
/// READ — the markup asks the page a question while painting — and it can never be an EventCallback, because
/// an EventCallback has no return value. Sweeping those in would have made the law unsatisfiable, which is a
/// law nobody can keep.</para>
///
/// <para><b>Proven RED, every clause</b> — <see cref="THE_RERENDER_AUDIT_CanTellPassFromFail"/> holds the
/// synthetic halves, and the PR body carries the runs against real files. Clause 2 was red on this branch's
/// base with 126 named parameters and clause 3 with six; clause 1 goes red on adding one
/// <c>@implements IHandleEvent</c> line to <c>TrackingPost.razor</c> — the same plant that reddens
/// <c>ThePressPaintsOnItsOwnTickTests</c>, which is the pair worth having: the source law and the driven
/// law falling over the same break, from the two different ends.</para>
/// </summary>
public sealed class NoSurfaceSwallowsARerenderTests
{
    private static string RepoRoot()
    {
        for (DirectoryInfo? at = new(AppContext.BaseDirectory); at is not null; at = at.Parent)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Client => Path.Combine(RepoRoot(), "src", "SpaceSails.Client");

    /// <summary>Every decomposed page's surfaces directory — the same list
    /// <see cref="NoSurfaceSwallowsAWriteTests"/> sweeps, for the same reason: a law that knew about one of
    /// them is a law the next decomposition walks straight past.</summary>
    private static IReadOnlyList<string> AllSurfaceDirs =>
    [
        Path.Combine(Client, "Pages", "Map"),
        // #251 · the surfaces of a surface. NavHud.razor is Map's own markup one file out, and its
        // markup is cut into Pages/Map/NavHud/ in its turn — under NavHud's dispatch, which is Map's.
        Path.Combine(Client, "Pages", "Map", "NavHud"),
        // #251 · and the same again for the satchel: SatchelPanel.razor's six pages are
        // Pages/Map/SatchelPanel/ now, under SatchelPanel's dispatch, which is Map's.
        Path.Combine(Client, "Pages", "Map", "SatchelPanel"),
        Path.Combine(Client, "Pages", "Stations", "TrackingPost"),
    ];

    // ── What the source says ─────────────────────────────────────────────────────────────────────────

    /// <summary>A surface, its page, and everything either of them has to say for itself.</summary>
    internal sealed record Surface(string Path, string Text, string Markup, Page Page)
    {
        public string Name => System.IO.Path.GetFileName(Path);

        /// <summary>Its own event dispatch: does it repeat the page's "no automatic re-render" override.</summary>
        public bool Suppresses => Text.Contains("IHandleEvent", StringComparison.Ordinal);
    }

    /// <summary>The page a surfaces directory was cut out of: <c>Pages/Map/</c> came out of
    /// <c>Pages/Map.razor</c> + <c>Pages/Map.*.cs</c>, <c>Pages/Stations/TrackingPost/</c> out of
    /// <c>Pages/Stations/TrackingPost.razor</c> + its five partials. Read as ONE text, because a page split
    /// across partials is still one component and its handler may live in any of them.</summary>
    internal sealed record Page(string Name, string Text)
    {
        /// <summary>The page has switched the automatic per-event re-render OFF for itself.</summary>
        public bool Suppresses => Text.Contains("@implements IHandleEvent", StringComparison.Ordinal);
    }

    private static Page PageOf(string surfacesDir)
    {
        string name = Path.GetFileName(surfacesDir);
        string beside = Path.GetDirectoryName(surfacesDir)!;
        string[] files =
        [
            .. Directory.EnumerateFiles(beside, $"{name}.razor", SearchOption.TopDirectoryOnly),
            .. Directory.EnumerateFiles(beside, $"{name}.*.cs", SearchOption.TopDirectoryOnly),
        ];
        if (files.Length == 0)
        {
            throw new FileNotFoundException(
                $"#1135 · {surfacesDir} has no page beside it — this law reads the PAGE to know which "
                + "dispatch regime its surfaces are under, and a surfaces directory with no page is a "
                + "decomposition this law cannot judge.");
        }

        return new Page(name, string.Join("\n", files.OrderBy(f => f, StringComparer.Ordinal)
                                                     .Select(File.ReadAllText)));
    }

    internal static IEnumerable<Surface> EverySurface()
    {
        foreach (string dir in AllSurfaceDirs)
        {
            Page page = PageOf(dir);
            foreach (string path in Directory.EnumerateFiles(dir, "*.razor", SearchOption.TopDirectoryOnly)
                                             .OrderBy(p => p, StringComparer.Ordinal))
            {
                // #1107 · the COMPONENT, not the file: a surface's `@code` block lives in <Name>.razor.cs now.
                string text = SurfaceComposition.ComponentText(path);
                int begins = text.IndexOf(MapMarkup.MarkupBegins, StringComparison.Ordinal);
                int ends = text.IndexOf(MapMarkup.MarkupEnds, StringComparison.Ordinal);
                yield return new Surface(path, text, begins >= 0 && ends > begins ? text[begins..ends] : "", page);
            }
        }
    }

    /// <summary>A parameter as the source declares it: the name it crosses under and the type it crosses
    /// as.</summary>
    internal readonly record struct Crossing(string Name, string Type)
    {
        /// <summary>A delegate that hands the markup nothing back is an EVENT. <c>Action</c>,
        /// <c>Action&lt;T…&gt;</c> and any <c>Func&lt;…, Task&gt;</c> DO something; every other
        /// <c>Func&lt;…&gt;</c> is the markup asking the page a question mid-paint, and no read can be an
        /// EventCallback because an EventCallback returns nothing to return.</summary>
        public bool IsAHandler =>
            Type == "Action"
            || Type.StartsWith("Action<", StringComparison.Ordinal)
            || (Type.StartsWith("Func<", StringComparison.Ordinal)
                && (Type.EndsWith("Task>", StringComparison.Ordinal)
                    || Type.EndsWith("ValueTask>", StringComparison.Ordinal)));

        public bool IsAnEventCallback =>
            Type == "EventCallback" || Type.StartsWith("EventCallback<", StringComparison.Ordinal);
    }

    internal static IReadOnlyList<Crossing> Parameters(string file) =>
    [
        .. Regex.Matches(file, @"\[Parameter\] public (?<type>[^\n]*?) (?<name>[A-Za-z_]\w*) \{ get; set; \}")
            .Select(m => new Crossing(m.Groups["name"].Value, m.Groups["type"].Value.Trim()))
    ];

    /// <summary>Every <c>@on…</c> handler expression the moved markup wrote, unwrapped from its quotes. This
    /// is the whole population of "things the surface will be the receiver for".</summary>
    internal static IReadOnlyList<string> EventExpressions(string markup) =>
    [
        .. Regex.Matches(markup, @"@on[a-z]+=""(?<v>[^""]*)""").Select(m => m.Groups["v"].Value.Trim())
    ];

    /// <summary>Wired STRAIGHT to the event: <c>@onclick="StartSweep"</c>. The receiver is whatever the
    /// delegate's type says, so the type is the whole fix and the markup never moves.</summary>
    internal static bool BoundStraight(IReadOnlyList<string> events, string name) =>
        events.Any(v => v == name || v == "@" + name);

    /// <summary>Reached from a LAMBDA: <c>@onclick="() =&gt; Drop(entry.ShipId)"</c>. The lambda is the
    /// handler and the surface is its receiver whatever the parameter's type is.</summary>
    internal static bool ReachedFromALambda(IReadOnlyList<string> events, string name) =>
        events.Any(v => v.Contains("=>", StringComparison.Ordinal)
                        && Regex.IsMatch(v, $@"(?<![\w.]){Regex.Escape(name)}\s*\("));

    /// <summary>One method's body out of a page, expression-bodied or braced. Anchored on an access modifier
    /// so <c>_ledger.Drop(id)</c> is never mistaken for the declaration of <c>Drop</c>.</summary>
    internal static string? BodyOf(string page, string method)
    {
        Match at = Regex.Match(
            page, $@"(?m)^[ \t]*(?:private|public|internal|protected)[^\n=;(]*?\b{Regex.Escape(method)}\s*\(");
        if (!at.Success)
        {
            return null;
        }

        int from = at.Index;
        int arrow = page.IndexOf("=>", from, StringComparison.Ordinal);
        int brace = page.IndexOf('{', from);
        if (arrow >= 0 && (brace < 0 || arrow < brace))
        {
            int semicolon = page.IndexOf(';', arrow);
            return semicolon < 0 ? page[arrow..] : page[arrow..semicolon];
        }

        if (brace < 0)
        {
            return null;
        }

        int depth = 0;
        for (int i = brace; i < page.Length; i++)
        {
            if (page[i] == '{')
            {
                depth++;
            }
            else if (page[i] == '}' && --depth == 0)
            {
                return page[brace..(i + 1)];
            }
        }

        return page[brace..];
    }

    // ── The law ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Clause 1 · A surface dispatches events the way the page it was cut out of does. A mismatch is
    /// the hazard in either direction: a surface that re-renders itself while the page never hears the press
    /// shows one half of a desk a world the other half does not have, and a surface that has silenced itself
    /// under a page that has not is a control that never repaints at all.</summary>
    [Fact]
    public void EverySurfaceDispatchesEventsTheWayItsPageDoes()
    {
        var wrong = new List<string>();

        foreach (Surface surface in EverySurface())
        {
            if (surface.Suppresses != surface.Page.Suppresses)
            {
                wrong.Add($"  {surface.Name}: {(surface.Suppresses ? "suppresses" : "does not suppress")} the "
                          + $"automatic re-render, and {surface.Page.Name}.razor "
                          + $"{(surface.Page.Suppresses ? "does" : "does not")}.");
            }
        }

        Assert.True(wrong.Count == 0,
            $"#1135 · {wrong.Count} surface(s) handle events differently from the page they were cut out of:\n"
            + string.Join("\n", wrong) + "\n\n"
            + "A surface is a piece of its page's markup living in another file; the page decides whether a\n"
            + "press repaints, and the surface has to be under the same decision. Map.razor turns the\n"
            + "automatic re-render OFF (its HUD refreshes on OnTick's 200 ms throttle) and every surface under\n"
            + "Pages/Map/ repeats the override. TrackingPost.razor leaves it on and none of its surfaces have\n"
            + "one. Either is right; the two of them disagreeing about one surface is not.");
    }

    /// <summary>Clause 2 · Where the page has not suppressed, a handler wired straight to an event crosses as
    /// an <c>EventCallback</c> so the PAGE is the receiver. Free: razor binds one to <c>@onclick</c> exactly
    /// as it binds an Action, so the markup does not move.</summary>
    [Fact]
    public void EveryHandlerBoundStraightToAnEventCrossesAsAnEventCallback()
    {
        var wrong = new List<string>();

        foreach (Surface surface in EverySurface())
        {
            IReadOnlyList<string> events = EventExpressions(surface.Markup);
            foreach (Crossing p in Parameters(surface.Text).Where(p => p.IsAHandler))
            {
                if (BoundStraight(events, p.Name))
                {
                    wrong.Add($"  {Relative(surface.Path)}: {p.Name} is {p.Type}");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"#1135 · {wrong.Count} handler(s) are wired straight to an event and cross as a plain delegate:\n"
            + string.Join("\n", wrong) + "\n\n"
            + "The receiver of the press is therefore the SURFACE, and the page — which owns the state the\n"
            + "press changed — is not re-rendered. Change the parameter's type to EventCallback (or\n"
            + "EventCallback<T> where the event carries a value) and invoke it with InvokeAsync from any C#\n"
            + "that calls it. No markup changes: razor compiles a method group crossing into an EventCallback\n"
            + "parameter as EventCallback.Factory.Create(this, …), with `this` the page.");
    }

    /// <summary>Clause 3 · Where the page has not suppressed, a handler the markup reaches through a LAMBDA
    /// keeps the surface as receiver whatever its type — so the page's own method has to re-render the page.
    /// Before the decomposition that button was on the page and the press repainted the page; this is that,
    /// put back.</summary>
    [Fact]
    public void EveryHandlerReachedFromALambdaLeavesThePagePainting()
    {
        var wrong = new List<string>();

        foreach (Surface surface in EverySurface().Where(s => !s.Page.Suppresses))
        {
            IReadOnlyList<string> events = EventExpressions(surface.Markup);
            foreach (Crossing p in Parameters(surface.Text).Where(p => p.IsAHandler))
            {
                if (!ReachedFromALambda(events, p.Name))
                {
                    continue;
                }

                string? body = BodyOf(surface.Page.Text, p.Name);
                if (body is null)
                {
                    wrong.Add($"  {Relative(surface.Path)}: {p.Name} is {p.Type}, and "
                              + $"{surface.Page.Name} declares no method of that name for this law to read");
                }
                else if (!body.Contains("StateHasChanged", StringComparison.Ordinal))
                {
                    wrong.Add($"  {Relative(surface.Path)}: {p.Name} is {p.Type}, and "
                              + $"{surface.Page.Name}.{p.Name} never calls StateHasChanged");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"#1135 · {wrong.Count} handler(s) are pressed through a lambda and leave the page unpainted:\n"
            + string.Join("\n", wrong) + "\n\n"
            + "`@onclick=\"() => Drop(id)\"` makes the LAMBDA the handler, and the lambda's receiver is the\n"
            + "surface that drew the button — so only that surface repaints, and the sibling surface reading\n"
            + "the same page field goes on showing the old one until the next HUD tick. Changing the\n"
            + "parameter's type cannot help here (the markup CALLS it, and rewriting moved markup is the one\n"
            + "thing the decomposition promised not to do), so the fix is on the page: end the page's own\n"
            + "method with StateHasChanged(), which is what the press did when the button was still on it.");
    }

    private static string Relative(string path) =>
        Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/');

    /// <summary>#1135 · The world this law is stated against can tell pass from fail — every clause is shown
    /// finding its violation on a planted source AND leaving the legitimate shape alone. A law whose regexes
    /// matched nothing would pass on a repo that had unwound the whole pattern.</summary>
    [Fact]
    public void THE_RERENDER_AUDIT_CanTellPassFromFail()
    {
        // It reads a parameter's TYPE, and it knows an event from a read.
        Assert.True(new Crossing("StartSweep", "Action").IsAHandler);
        Assert.True(new Crossing("Drop", "Action<string>").IsAHandler);
        Assert.True(new Crossing("Dismiss", "Func<Action, Task>").IsAHandler);
        Assert.False(new Crossing("CounterHasStools", "Func<bool>").IsAHandler);
        Assert.False(new Crossing("FormatWallDistance", "Func<double, string>").IsAHandler);
        Assert.False(new Crossing("Card", "RenderFragment").IsAHandler);
        Assert.True(new Crossing("OnClose", "EventCallback").IsAnEventCallback);
        Assert.True(new Crossing("OnZoomMap", "EventCallback<double>").IsAnEventCallback);

        // It finds parameters in the shape the surfaces really write them.
        IReadOnlyList<Crossing> read = Parameters(
            "    [Parameter] public Action<string> Drop { get; set; } = default!;\n"
            + "    [Parameter] public EventCallback StartSweep { get; set; }\n"
            + "    [Parameter] public double SimTime { get; set; }\n");
        Assert.Equal(["Drop", "StartSweep", "SimTime"], read.Select(p => p.Name));
        Assert.Equal("Action<string>", read[0].Type);

        // It tells a straight wire from a lambda, and both from a mention that is neither.
        IReadOnlyList<string> events = EventExpressions(
            """<button @onclick="StartSweep">Start</button>"""
            + """<button @onclick="() => Drop(entry.ShipId)">Drop</button>"""
            + """<input @oninput="@OnArcInput" />""");
        Assert.True(BoundStraight(events, "StartSweep"));
        Assert.True(BoundStraight(events, "OnArcInput"));
        Assert.False(BoundStraight(events, "Drop"));
        Assert.True(ReachedFromALambda(events, "Drop"));
        Assert.False(ReachedFromALambda(events, "StartSweep"));
        Assert.False(ReachedFromALambda(events, "Confirm"));
        Assert.False(ReachedFromALambda(EventExpressions("""<div title="Drop (frees a slot)"></div>"""), "Drop"));

        // It reads a page method's body — braced, expression-bodied, and not the CALL that looks like one.
        Assert.Contains("StateHasChanged", BodyOf(
            "    private void Drop(string shipId)\n    {\n        _ledger.Drop(shipId);\n"
            + "        StateHasChanged();\n    }\n", "Drop")!);
        Assert.DoesNotContain("StateHasChanged", BodyOf(
            "    private void Drop(string shipId) => _ledger.Drop(shipId);\n", "Drop")!);
        Assert.Null(BodyOf("        _lostTracks.Drop(shipId);\n", "Drop"));

        // And there is a real world under it: both decomposed pages, both regimes, and enough surfaces that
        // a law finding nothing would be news.
        Surface[] surfaces = [.. EverySurface()];
        Assert.True(surfaces.Length > 80, $"only {surfaces.Length} surface(s) — this law is guarding an empty room.");
        foreach (string dir in AllSurfaceDirs)
        {
            Assert.Contains(surfaces, s => Path.GetDirectoryName(s.Path) == dir);
        }

        Assert.Contains(surfaces, s => s.Page.Suppresses);
        Assert.Contains(surfaces, s => !s.Page.Suppresses);

        // The surfaces really do carry handlers of both wirings — otherwise clauses 2 and 3 would be green
        // because they had nothing to look at, which is this repo's fifth named bug class. Clause 2's
        // population is the whole repo (a straight wire costs nothing to get right, on either page); clause
        // 3's is the desk, the one page that has not silenced the automatic re-render.
        int wiredStraight = surfaces.Count(s => Parameters(s.Text).Any(
            p => p.IsAnEventCallback && BoundStraight(EventExpressions(s.Markup), p.Name)));
        Assert.True(wiredStraight >= 30,
            $"only {wiredStraight} surface(s) wire an EventCallback straight to an event — clause 2 is "
            + "looking at nothing.");

        // …and the desk's own five, by name: #1134's fix is the case this law was written from, and a law
        // that stopped seeing it would be green on the bug it exists for.
        Surface scope = surfaces.Single(s => s.Name == "ScopeControls.razor");
        foreach (string wired in (string[])["StartSweep", "StopSweep", "TogglePassiveWatch", "OnBearingInput", "OnArcInput"])
        {
            Assert.True(BoundStraight(EventExpressions(scope.Markup), wired),
                $"ScopeControls no longer wires {wired} straight to an event — the case #1135 was written "
                + "from has moved and this law has not.");
            Assert.True(Parameters(scope.Text).Single(p => p.Name == wired).IsAnEventCallback);
        }

        Surface[] desk = [.. surfaces.Where(s => !s.Page.Suppresses)];
        Assert.True(
            desk.Count(s => Parameters(s.Text).Any(
                p => p.IsAHandler && ReachedFromALambda(EventExpressions(s.Markup), p.Name))) >= 3,
            "no surface on an unsuppressed page reaches a handler through a lambda — clause 3 is looking at "
            + "nothing.");
    }
}
