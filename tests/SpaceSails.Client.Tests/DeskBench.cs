#pragma warning disable BL0006 // the render tree is exactly what this bench exists to read — see the note below
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceSails.Client.Pages;

namespace SpaceSails.Client.Tests;

/// <summary>
/// THE DESK BENCH — a real Blazor renderer with the shipping <see cref="Map"/> in it.
///
/// <para><b>Why a renderer and not a fingerprint.</b> <see cref="TheBootBuildsTheSameWorldTests"/> proves what
/// the boot BUILDS: it reads the component's fields. It cannot prove the page DRAWS, and the Trade-desk crash
/// lived exactly in that gap — the world was built perfectly and the desk showed "An unhandled error has
/// occurred." So this bench takes the same booted component one step further and runs the Razor-generated
/// render code through a real <see cref="Renderer"/>, child components and all, which is the only way a
/// mistake in the MARKUP can be seen at all.</para>
///
/// <para><b>How far off-browser gets, exactly.</b> Two walls, both already documented by the guards next
/// door, and both crossed deliberately rather than faked:</para>
/// <list type="number">
/// <item><b>The boot's browser gate.</b> <c>WireTheRendererToTheBrowserAsync</c> ends in
/// <c>JSHost.ImportAsync</c>, which off a browser throws <c>PlatformNotSupportedException</c> —
/// <see cref="TheBootBuildsTheSameWorldTests"/> calls that "the fingerprint's horizon" and
/// <c>TheBootStopsWhenYouLeaveTests</c> asserts the same wall from the other side. Everything the boot does
/// AFTER that gate — the start point, the cheats that need a live world, the landing — is four ordinary
/// private methods with no browser under them, so this bench <b>calls those four itself</b>, in the boot's own
/// order, with the query re-read through the boot's own <c>ReadEveryQueryKey</c> and defaulted through its own
/// <c>DefaultABerthForTheCheatsThatNeedOne</c>. That is what makes a berth, an excursion or a Hive floor
/// reachable here at all, and it is the shipping code that makes it, not a stand-in.</item>
/// <item><b><c>_worldReady</c>.</b> The one field the gate sets on its far side (the last line of the method
/// the gate is in). It is set here directly, because without it the page draws nothing but a loading door, and
/// a loading door is not a desk.</item>
/// </list>
///
/// <para><b>What still throws, and why that is not a failure.</b> <c>TrackingPost</c> reaches for the same JS
/// module from its own <c>OnAfterRenderAsync</c>, so every render raises one
/// <c>PlatformNotSupportedException</c> out of <c>System.Runtime.InteropServices.JavaScript</c>. That is the
/// documented gate again, arriving through the renderer's error channel, and <see cref="EscapedPastTheGate"/>
/// filters it — <b>by its type AND its message, never by swallowing everything</b>, so any other exception a
/// render raises is reported. A filter that hid them all would be this repo's fifth named bug class: a guard
/// that cannot tell pass from fail.</para>
///
/// <para><b>The one world this bench cannot reach, measured rather than guessed.</b> The rAF loop starts
/// INSIDE the gate (<c>RendererInterop.StartLoop</c>), so in a browser several sim ticks have already run by
/// the time the post-gate stages fire; here, none have. That only matters to one thing —
/// <c>?land=1</c> with no <c>?dock=</c>, where <c>ShuttleDestinationsInRange()</c> is read synchronously at
/// <c>SimTime</c> 0 and comes back empty ("nothing landable in shuttle reach from this berth"). Checked in a
/// real Chromium on the dev server with localStorage cleared: <c>/map?found=1&amp;land=1</c> DOES land there,
/// so this is the bench's horizon and not a bug in the cheat. It is why every world in
/// <see cref="EveryDeskBootsTests"/>'s matrix names its berth, and why
/// <see cref="EveryDeskBootsTests.EveryWorldInTheMatrixIsTheWorldItClaims"/> exists at all: a row that had
/// quietly ended at the front door would run eight desk checks against a start picker and pass.</para>
///
/// <para><b>On BL0006.</b> <c>Microsoft.AspNetCore.Components.RenderTree</c> is warned against for application
/// code because its shape may change between releases. This is a test whose entire subject is what the render
/// tree CONTAINS; there is no supported API that answers "what attribute names did this component emit", and
/// a framework change that moved these types would fail loudly at compile time, in one file. Suppressed with a
/// pragma here rather than in the project file, so nothing else picks up the habit.</para>
/// </summary>
internal sealed class DeskBench : Renderer
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const BindingFlags Shared = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The four stages of <c>BootTheWorldAsync</c> that run BEHIND the browser gate, in the order the
    /// boot runs them. Named rather than re-implemented: if one is renamed or a fifth is added, this bench
    /// throws on the missing method instead of quietly booting a smaller world.</summary>
    private static readonly string[] TheStagesBehindTheGate =
    [
        "ApplyTheStartPoint",
        "StandTheCaptainWhereTheCheatsAsk",
        "SeedTheArcsAndTheJobs",
        "SeedTheApproachesAndThePurse",
    ];

    /// <summary>The landing cheat is fire-and-forget (<c>_ = AutoLandThenStageDeathAsync(…)</c>, by design — it
    /// narrates its own descent phases and yields between them), so there is no task to await. The bench lets
    /// the scheduler run instead, one second of it, and only when a landing was actually asked for — most boot
    /// URLs never leave the ship and waiting on them would add a minute to the dev-start sweep for nothing.
    /// <see cref="EveryDeskBootsTests.EveryWorldInTheMatrixIsTheWorldItClaims"/> is what proves the wait is
    /// long enough: a world that had not landed yet fails its own row rather than quietly testing the wrong
    /// scene.</summary>
    private const int DescentSpins = 40;

    private readonly Map _map;
    private readonly List<Exception> _escaped = [];
    private int _rootId = -1;

    private DeskBench(IServiceProvider services, Map map)
        : base(services, NullLoggerFactory.Instance) => _map = map;

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    protected override void HandleException(Exception e) => _escaped.Add(e);

    protected override Task UpdateDisplayAsync(in RenderBatch batch) => Task.CompletedTask;

    // ── Booting ──────────────────────────────────────────────────────────────────────────────────────

    public static async Task<DeskBench> BootAsync(string url)
    {
        var map = new Map();
        TheBootBuildsTheSameWorldTests.NeverRender(map);
        System.Net.Http.HttpClient http = TheBootBuildsTheSameWorldTests.ScenariosFromDisk();
        var navigation = new TheBootBuildsTheSameWorldTests.Bench(url);
        TheBootBuildsTheSameWorldTests.Hand(map, "Http", http);
        TheBootBuildsTheSameWorldTests.Hand(map, "Navigation", navigation);

        var bench = new DeskBench(new OnlyWhatThePageInjects(http, navigation), map);

        try
        {
            await (Task)Method("BootTheWorldAsync").Invoke(map, [CancellationToken.None])!;
        }
        catch (TargetInvocationException)
        {
            // the browser gate, reached synchronously
        }
        catch (Exception)
        {
            // the browser gate, reached from a continuation
        }

        if (Read(map, "_ephemeris") is null)
        {
            throw new InvalidOperationException(
                $"{url}: the boot stopped before it built an ephemeris — that is not the browser gate, it is a "
                + "world that was never built, and rendering it would prove nothing.");
        }

        // Past the gate, by hand — see the class note.
        object query = Method("ReadEveryQueryKey").Invoke(map, [new Uri(navigation.Uri)])!;
        Method("DefaultABerthForTheCheatsThatNeedOne").Invoke(map, [query]);
        Write(map, "_worldReady", true);

        foreach (string stage in TheStagesBehindTheGate)
        {
            try
            {
                Method(stage).Invoke(map, [query]);
            }
            catch (TargetInvocationException ex)
            {
                bench._escaped.Add(ex.InnerException ?? ex);
            }
        }

        if ((bool)Read(map, "_landCheat")!)
        {
            for (int spin = 0; spin < DescentSpins; spin++)
            {
                await Task.Delay(25);
            }
        }

        // The page is now where the boot would have left it. Two latches so the renderer does not boot it a
        // SECOND time: `_started` is the page's own "the boot began" flag (OnAfterRenderAsync early-outs on
        // it), and `_hasPendingQueuedRender` is the early-out NeverRender set for the boot, which now has to be
        // released so StateHasChanged reaches this renderer.
        Write(map, "_started", true);
        typeof(ComponentBase)
            .GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, false);
        return bench;
    }

    // ── Driving ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Ask the SHIPPING desk switch to take us there. Number keys, tab clicks, chips and bridge-seat
    /// interactions all funnel through <c>SwitchDesk</c> — "the one place a desk switch happens" — so a
    /// refusal here is the refusal a player gets, and a desk this bench can reach is a desk a player can.</summary>
    public Task SwitchAsync(ShipDesk desk) =>
        Dispatcher.InvokeAsync(() => Method("SwitchDesk").Invoke(_map, [desk]));

    public async Task<Painted> RenderAsync()
    {
        if (_rootId < 0)
        {
            _rootId = AssignRootComponentId(_map);
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(_rootId));
        }
        else
        {
            await Dispatcher.InvokeAsync(() =>
                typeof(ComponentBase).GetMethod("StateHasChanged", Hidden)!.Invoke(_map, null));
        }

        var painted = new Painted();
        WalkComponent(_rootId, painted, new StringBuilder(), painted.Root);
        return painted;
    }

    /// <summary>
    /// #992 · <b>PRESS IT.</b> The renderer's own event channel, handed the handler id the render tree wrote
    /// for a control — so this is the click a player's mouse makes, dispatched through the same
    /// <c>DispatchEventAsync</c> the browser's JS side calls, and NOT the component's method reached by name.
    ///
    /// <para>That distinction is the whole reason the dismissibility law can be trusted. A test that invoked
    /// <c>CloseStoryCard</c> by reflection would prove that a method called <c>Close…</c> clears a field; it
    /// would say nothing about whether any control on the screen is WIRED to it, which is exactly the way a
    /// pop-up ends up with no way out. Pressing the button the tree actually drew proves the wire.</para>
    ///
    /// <para>Whatever the handler raises lands in the renderer's error channel (<see cref="HandleException"/>)
    /// rather than here — that is how <c>TrackingPost</c>'s browser gate has always arrived — so the caller
    /// reads <see cref="EscapedPastTheGate"/> afterwards to see whether the press hurt anything.</para>
    /// </summary>
    public Task PressAsync(ulong handlerId) =>
        Dispatcher.InvokeAsync(() => DispatchEventAsync(handlerId, null, new MouseEventArgs()));

    /// <summary>#992 · Read one of the page's own fields by name, for a law that needs to know what a press
    /// did to the state as well as to the markup.</summary>
    public object? Field(string name) => Read(_map, name);

    /// <summary>
    /// #992 · <b>PUT THE PAGE IN THE STATE THAT RAISES THIS POP-UP.</b> A field write, by name, on the
    /// shipping component.
    ///
    /// <para>Written down rather than apologised for: this is the one thing in the bench that is NOT the
    /// shipping road. A pop-up's real road is a captain walking somewhere and pressing something, and there
    /// is no off-browser way to walk. What the dismissibility law needs is the SURFACE on the screen, and the
    /// surface is a pure function of the gate field — <c>@if (_showSatchel)</c> draws the same satchel however
    /// <c>_showSatchel</c> came to be true. So the state is set here and everything the law then asserts —
    /// that a control exists, that it is the one the markup wired, that pressing it takes the surface away —
    /// runs entirely through the shipping render and the shipping handler.</para>
    ///
    /// <para>The safety on it is that the field is named: a gate that is renamed or deleted fails loudly here
    /// with the name it used to have, instead of quietly raising nothing and passing a law about a surface
    /// that was never on the screen (this repository's fifth named bug class).</para>
    /// </summary>
    public void Poke(string field, object? value) => TheField(field).SetValue(_map, value);

    // ── Reading the page back ────────────────────────────────────────────────────────────────────────

    public static ShipDesk[] TabBarOrder =>
        (ShipDesk[])(typeof(Map).GetField("TabBarOrder", Shared)
            ?? throw new InvalidOperationException("Map has no TabBarOrder — the tab bar's own ordering has moved."))
            .GetValue(null)!;

    public ShipDesk ActiveDesk => (ShipDesk)Read(_map, "_activeDesk")!;

    public bool DeckMode => (bool)Read(_map, "_deckMode")!;

    public bool OnSurface => Read(_map, "_surface") is not null;

    public bool Docked => Read(_map, "_dockedHavenId") is not null;

    public string Pulse => Read(_map, "_pulse")?.ToString() ?? "";

    /// <summary>Every exception the page raised that is NOT the documented browser gate. Matched on type AND
    /// message: <c>PlatformNotSupportedException</c> is a real failure everywhere else, so hiding the type
    /// outright would blind the sweep to it.</summary>
    public IEnumerable<Exception> EscapedPastTheGate =>
        _escaped.SelectMany(Unwrap).Where(e => !IsTheBrowserGate(e));

    private static IEnumerable<Exception> Unwrap(Exception e) =>
        e is AggregateException aggregate ? aggregate.InnerExceptions.SelectMany(Unwrap) : [e];

    private static bool IsTheBrowserGate(Exception e) =>
        e is PlatformNotSupportedException
        && e.Message.Contains("System.Runtime.InteropServices.JavaScript", StringComparison.Ordinal);

    // ── The render tree, walked ──────────────────────────────────────────────────────────────────────

    private void WalkComponent(int componentId, Painted into, StringBuilder spokenHere, Painted.Node parent) =>
        Walk(GetCurrentRenderTreeFrames, componentId, into, spokenHere, parent);

    /// <summary>
    /// #997 · THE WALK, LIFTED OUT OF THIS BENCH. It was private and it was only ever about frames, not
    /// about the Map: the only thing it needed from the renderer was "hand me component N's frames", which
    /// is now the first parameter. <see cref="ShellBench"/> mounts a single component instead of the whole
    /// page and reads its tree with exactly this code — one reading of a render tree in the test project,
    /// so a component test and the dismissibility law can never disagree about what was drawn.
    /// </summary>
    internal static void Walk(
        Func<int, ArrayRange<RenderTreeFrame>> framesOf,
        int componentId,
        Painted into,
        StringBuilder spokenHere,
        Painted.Node parent)
    {
        ArrayRange<RenderTreeFrame> frames = framesOf(componentId);
        WalkRange(framesOf, frames.Array, 0, frames.Count, into, spokenHere, parent);
    }

    /// <summary>One pass over a frame range, honouring the subtree lengths the renderer wrote — so an
    /// element's attributes are ITS attributes and the text under it is ITS text, which is what lets a control
    /// be named and a class list be attached to the element that wears it.</summary>
    private static void WalkRange(
        Func<int, ArrayRange<RenderTreeFrame>> framesOf,
        RenderTreeFrame[] all, int start, int end, Painted into, StringBuilder spokenHere, Painted.Node parent)
    {
        int at = start;
        while (at < end)
        {
            ref RenderTreeFrame frame = ref all[at];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                {
                    int subtreeEnd = Math.Min(end, at + Math.Max(1, frame.ElementSubtreeLength));
                    string element = frame.ElementName;
                    var attributes = new Dictionary<string, string?>(StringComparer.Ordinal);

                    // #992 · the handler IDS as well as the attribute names. An event-handler attribute
                    // carries no string value — its payload is the id the renderer will match a dispatched
                    // event against — so it has to be read off its own field or the tree remembers only that
                    // "onclick" was written and nothing about what could press it.
                    var handlers = new Dictionary<string, ulong>(StringComparer.Ordinal);

                    int child = at + 1;
                    while (child < subtreeEnd && all[child].FrameType == RenderTreeFrameType.Attribute)
                    {
                        into.Attributes.Add(all[child].AttributeName);
                        attributes[all[child].AttributeName] = all[child].AttributeValue as string;
                        if (all[child].AttributeEventHandlerId != 0)
                        {
                            handlers[all[child].AttributeName] = all[child].AttributeEventHandlerId;
                        }

                        child++;
                    }

                    var node = new Painted.Node(element, attributes, handlers);
                    parent.Children.Add(node);

                    var inside = new StringBuilder();
                    WalkRange(framesOf, all, child, subtreeEnd, into, inside, node);
                    node.Spoken = inside.ToString().Trim();
                    into.Element(element, attributes, node.Spoken);
                    spokenHere.Append(inside);
                    at = subtreeEnd;
                    break;
                }

                case RenderTreeFrameType.Text:
                    spokenHere.Append(frame.TextContent).Append(' ');
                    at++;
                    break;

                case RenderTreeFrameType.Markup:
                    // #992 · …and the blob is KEPT, not just spoken. Razor collapses a run of static HTML
                    // with no dynamic content in it into ONE AddMarkupContent call, so those elements never
                    // become Element frames and an element walk cannot see them at all. Found the hard way:
                    // a deliberately-planted <div class="ghost-backdrop"> was invisible to the pop-up law's
                    // DOM guard while the source guard named it at once. A guard that cannot see a whole
                    // class of markup is a guard that cannot tell pass from fail, so the markup is handed
                    // out and the law reads the classes in it too.
                    into.MarkupBlobs.Add(frame.MarkupContent);
                    spokenHere.Append(frame.MarkupContent).Append(' ');
                    at++;
                    break;

                case RenderTreeFrameType.Component:
                {
                    into.Components.Add(frame.ComponentType?.Name ?? "?");
                    Walk(framesOf, frame.ComponentId, into, spokenHere, parent);
                    at += Math.Max(1, frame.ComponentSubtreeLength);
                    break;
                }

                case RenderTreeFrameType.Region:
                    at++;
                    break;

                default:
                    at++;
                    break;
            }
        }
    }

    // ── Plumbing ─────────────────────────────────────────────────────────────────────────────────────

    private static MethodInfo Method(string name) =>
        typeof(Map).GetMethod(name, Hidden)
        ?? throw new InvalidOperationException(
            $"Map has no {name} — this bench drives the shipping boot by name, and that name has moved.");

    private static FieldInfo TheField(string name) =>
        typeof(Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no field {name} — this bench reads it by name.");

    private static object? Read(Map map, string name) => TheField(name).GetValue(map);

    private static void Write(Map map, string name, object value) => TheField(name).SetValue(map, value);

    /// <summary>The two services <c>Map.razor</c>'s <c>@inject</c> lines ask for, and nothing else. A component
    /// that starts asking for a third gets a null and a NullReferenceException naming it — louder and more
    /// useful than a container that invents one.</summary>
    private sealed class OnlyWhatThePageInjects(System.Net.Http.HttpClient http, NavigationManager navigation)
        : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(System.Net.Http.HttpClient) ? http
            : serviceType == typeof(NavigationManager) ? navigation
            : null;
    }

    /// <summary>What one render produced: every attribute NAME the tree emitted, every class list, the desk
    /// tabs the bar drew (and which of them is lit), and the controls carrying a name a player could read.</summary>
    internal sealed class Painted
    {
        private static readonly string[] Controls = ["button", "input", "select", "textarea", "a"];

        /// <summary>
        /// #992 · THE PAGE AS A TREE, not as five flat lists.
        ///
        /// <para>The lists above answer "did the page draw X anywhere". The dismissibility law needs a harder
        /// question answered — "is there a control INSIDE this surface that ends it" — and a flat list of
        /// every class the page emitted cannot tell a ✕ that belongs to the card in front from a ✕ on the
        /// panel behind it. So the walk keeps the nesting the renderer already wrote down, and the law asks
        /// each surface about its own subtree.</para>
        /// </summary>
        public Node Root { get; } = new("#document", new Dictionary<string, string?>(StringComparer.Ordinal),
                                        new Dictionary<string, ulong>(StringComparer.Ordinal));

        /// <summary>#992 · Every run of static HTML the component emitted as one blob. See the note at the
        /// Markup case in the walk: these elements are not in <see cref="Root"/> and never can be.</summary>
        public List<string> MarkupBlobs { get; } = [];

        public List<string> Attributes { get; } = [];

        public List<string> Components { get; } = [];

        public List<string> ClassLists { get; } = [];

        /// <summary>Controls a player could name out loud: a button/input/select/textarea/link carrying text of
        /// its own, a <c>title</c>, or an <c>aria-label</c>.</summary>
        public List<string> NamedControls { get; } = [];

        /// <summary>The labels the desk tab bar drew, in order — read off the buttons wearing
        /// <c>.desk-tab</c>, which is the bar telling us itself which desks it offers.</summary>
        public List<string> DeskTabLabels { get; } = [];

        /// <summary>…and the ones it drew LIT (<c>btn-info</c>): the page saying which desk is up.</summary>
        public List<string> LitDeskTabs { get; } = [];

        internal void Element(string element, Dictionary<string, string?> attributes, string spoken)
        {
            string classList = attributes.GetValueOrDefault("class") ?? "";
            if (classList.Length > 0)
            {
                ClassLists.Add(classList);
            }

            string name = spoken.Length > 0 ? spoken
                : attributes.GetValueOrDefault("title") ?? attributes.GetValueOrDefault("aria-label") ?? "";
            name = name.Trim();

            if (name.Length > 0 && Controls.Contains(element, StringComparer.Ordinal))
            {
                NamedControls.Add(name);
            }

            string[] classes = classList.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (element == "button" && classes.Contains("desk-tab", StringComparer.Ordinal))
            {
                DeskTabLabels.Add(name);
                if (classes.Contains("btn-info", StringComparer.Ordinal))
                {
                    LitDeskTabs.Add(name);
                }
            }
        }

        /// <summary>#992 · One element the page drew, with the nesting kept and the press-ids kept.</summary>
        internal sealed class Node(
            string element,
            Dictionary<string, string?> attributes,
            Dictionary<string, ulong> handlers)
        {
            public string Element { get; } = element;

            public IReadOnlyDictionary<string, string?> Attributes { get; } = attributes;

            /// <summary>Event-handler attribute name (<c>onclick</c>) → the id <see cref="DeskBench.PressAsync"/>
            /// dispatches against. Empty on an element nothing can press.</summary>
            public IReadOnlyDictionary<string, ulong> Handlers { get; } = handlers;

            public List<Node> Children { get; } = [];

            /// <summary>The text of this element's whole subtree, trimmed.</summary>
            public string Spoken { get; set; } = "";

            public string ClassList => Attributes.GetValueOrDefault("class") ?? "";

            public IEnumerable<string> Classes =>
                ClassList.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            /// <summary>Bootstrap's own way of saying "drawn but not on the screen". A surface wearing it is
            /// not up, and a CONTROL wearing it is not a way out of anything.</summary>
            public bool Hidden => Classes.Contains("d-none", StringComparer.Ordinal);

            /// <summary>What a player could call this control out loud — its own words, else its title, else
            /// its aria-label. The same reading <see cref="NamedControls"/> takes.</summary>
            public string Name =>
                (Spoken.Length > 0 ? Spoken
                 : Attributes.GetValueOrDefault("title") ?? Attributes.GetValueOrDefault("aria-label") ?? "")
                .Trim();

            public bool HasClass(string css) => Classes.Contains(css, StringComparer.Ordinal);

            public IEnumerable<Node> Descendants()
            {
                foreach (Node child in Children)
                {
                    yield return child;
                    foreach (Node deeper in child.Descendants())
                    {
                        yield return deeper;
                    }
                }
            }

            /// <summary>Every element in the page, this one included.</summary>
            public IEnumerable<Node> SelfAndDescendants() => new[] { this }.Concat(Descendants());
        }
    }
}
