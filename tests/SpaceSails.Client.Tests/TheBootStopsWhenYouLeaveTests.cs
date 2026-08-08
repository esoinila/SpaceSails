using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// BL0006: subclassing Renderer is how you get the REAL first render, the real OnAfterRenderAsync and the
// real unhandled-exception hook — the three things this guard is about. A hand-written stand-in for the
// renderer would be a guard against the stand-in. Scoped to this file.
#pragma warning disable BL0006

namespace SpaceSails.Client.Tests;

/// <summary>
/// #737 · A PLAYER WHO LEAVES DURING A LONG BOOT IS NOT A CORNER CASE.
///
/// <para>Boot pegs the main thread for tens of seconds, so clicking away from /map before the world is
/// finished is the ordinary thing a phone user does to a slow load — and it raised
/// <c>crit: WebAssemblyRenderer[100] Unhandled exception rendering component</c> three times out of three
/// in the #734 instrumented runs, where letting the boot finish first raised none.</para>
///
/// <para>The cause is the shape of the boot, not any one line of it: it is a chain of awaits, and its
/// continuations used to resume into a component the router had already torn down. The end of that chain
/// is the renderer stage — <c>initCanvas</c>, <c>startLoop</c>, <c>FocusAsync</c> — which names DOM by id,
/// and <c>renderer.js</c> throws rather than shrugging when the id resolves to nothing
/// (<c>no canvas element with id "map-canvas"</c>). The throw escaped <c>OnAfterRenderAsync</c>, and the
/// renderer logged it as its own.</para>
///
/// <para>These tests drive the SHIPPING component through a real Blazor <see cref="Renderer"/> — the
/// first render, the boot it starts, the <c>Dispose</c> the router performs on navigation — and watch the
/// same two places production watches: the task <c>OnAfterRenderAsync</c> returned, and the renderer's
/// own unhandled-exception hook, which in the browser is the line in the ticket.</para>
///
/// <para>To watch them go red on the old behaviour, delete <c>_bootAbandoned.Cancel();</c> from
/// <c>Map.Dispose</c> — that one line is the whole difference between a boot that stops when its page
/// goes and the boot that did not.</para>
/// </summary>
public sealed class TheBootStopsWhenYouLeaveTests
{
    [Fact]
    public async Task NavigatingAwayMidBootRaisesNOTHING()
    {
        // The repro, in order: arrive at /map, let the boot get as far as the scenario fetch, click away,
        // and only THEN let the fetch land — so the continuation resumes into a page that is already gone.
        using var bench = new BootBench();
        await bench.ArriveAtMapAsync();

        await bench.NavigateAwayAsync();
        bench.LetTheScenarioLand();

        await bench.Map.Boot!;

        Assert.Empty(bench.UnhandledFromTheBoot);
    }

    [Fact]
    public async Task TheAbandonedBootNeverStartsARenderLoopNothingCouldStop()
    {
        // The second half of the same fault, and the more expensive half: the boot did not merely throw on
        // its way through the renderer stage, it went THROUGH it — subscribing a discarded component to the
        // static frame tick and pointing a rAF loop at a canvas that had left the DOM. Dispose had already
        // run by then, so nothing was ever going to unsubscribe or stop them.
        using var bench = new BootBench();
        await bench.ArriveAtMapAsync();

        await bench.NavigateAwayAsync();
        bench.LetTheScenarioLand();
        await bench.Map.Boot!;

        Assert.True(bench.Map.BootAbandonedToken.IsCancellationRequested);
        Assert.False(RendererReached(bench.Map));
    }

    [Fact]
    public async Task ABootNOBODYLeftStillGoesAllTheWayToTheBROWSER()
    {
        // The guard has to be able to tell the two apart, or "stops early" would just be "never runs". No
        // navigation here: the same bench, the same world build, and the boot walks the whole way to the
        // renderer stage — where a test host that is not a browser cannot follow it. Reaching a browser API
        // off-browser is the proof that nothing short-circuited; the abandoned boot above never gets here.
        using var bench = new BootBench();
        await bench.ArriveAtMapAsync();

        bench.LetTheScenarioLand();

        await Assert.ThrowsAnyAsync<Exception>(() => bench.Map.Boot!);
        Assert.False(bench.Map.BootAbandonedToken.IsCancellationRequested);
    }

    [Fact]
    public void DisposeIsWhatTELLSTheBootItsPageIsGone()
    {
        // The seam itself, on a component that never booted at all: Dispose must be safe to call on a Map
        // whose renderer stage was never reached (no loop to stop, no canvas to name) and must leave the
        // boot's token cancelled.
        var map = new Pages.Map();

        Assert.False(map.BootAbandonedToken.IsCancellationRequested);
        map.Dispose();

        Assert.True(map.BootAbandonedToken.IsCancellationRequested);
    }

    /// <summary>True once the boot has wired the canvas renderer up — the first thing past the last gate.</summary>
    private static bool RendererReached(Pages.Map map) =>
        typeof(Pages.Map)
            .GetField("_renderer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(map) is not null;

    /// <summary>
    /// A real Blazor renderer with a hand-held clock: the Map renders and boots for real, but the scenario
    /// fetch it parks on only lands when the test says so, which is what makes "mid-boot" an instant the
    /// test can name rather than a race it has to win.
    /// </summary>
    private sealed class BootBench : Renderer
    {
        private readonly TaskCompletionSource<string> _scenario = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _componentId;

        public BootBench()
            : this(new Services())
        {
        }

        private BootBench(Services services)
            : base(services, NullLoggerFactory.Instance)
        {
            services.Scenarios = new HttpClient(new HeldScenario(_scenario)) { BaseAddress = new Uri("http://localhost/") };
            Map = new Pages.Map();

            // AssignRootComponentId attaches a component the caller built; only ComponentFactory does
            // [Inject], and only for components IT instantiates. So the two services Map declares are
            // handed over here, from the same provider the renderer was given.
            Hand(Map, "Http", services.GetService(typeof(HttpClient))!);
            Hand(Map, "Navigation", services.GetService(typeof(NavigationManager))!);
        }

        private static void Hand(Pages.Map map, string property, object service) =>
            typeof(Pages.Map)
                .GetProperty(property, System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!
                .SetValue(map, service);

        public Pages.Map Map { get; }

        /// <summary>What the renderer would have logged as <c>WebAssemblyRenderer[100]</c> FOR THE MAP'S OWN
        /// BOOT. Sibling components on the page do their own browser wiring after the first render, and off
        /// a browser that wiring throws — bench noise, and not the fault the ticket recorded, so it is not
        /// laid at the boot's door.</summary>
        public IEnumerable<Exception> UnhandledFromTheBoot =>
            _unhandled.Where(e => e.ToString().Contains("SpaceSails.Client.Pages.Map.", StringComparison.Ordinal));

        private readonly List<Exception> _unhandled = [];

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public async Task ArriveAtMapAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                _componentId = AssignRootComponentId(Map);
                return RenderRootComponentAsync(_componentId);
            });

            // The first render is what starts the boot, and the boot's first await is the scenario fetch —
            // so by here the world is genuinely half-built and waiting, exactly as it is on a slow phone.
            Assert.NotNull(Map.Boot);
            Assert.False(Map.Boot!.IsCompleted);
        }

        public Task NavigateAwayAsync() => Dispatcher.InvokeAsync(Map.Dispose);

        public void LetTheScenarioLand() =>
            _scenario.TrySetResult(File.ReadAllText(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

        protected override void HandleException(Exception exception) => _unhandled.Add(exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;

        protected override void Dispose(bool disposing)
        {
            // Never leave the handler parked on a fetch no test is going to release.
            _scenario.TrySetCanceled();
            base.Dispose(disposing);
        }

        private static string RepoRoot()
        {
            DirectoryInfo? at = new(AppContext.BaseDirectory);
            while (at is not null)
            {
                if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
                {
                    return at.FullName;
                }
                at = at.Parent;
            }
            throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
        }

        /// <summary>The two services Map injects, plus the logging the base renderer asks for.</summary>
        private sealed class Services : IServiceProvider
        {
            private readonly Bench _navigation = new();

            public HttpClient? Scenarios { get; set; }

            public object? GetService(Type serviceType) =>
                serviceType == typeof(ILoggerFactory) ? NullLoggerFactory.Instance
                : serviceType == typeof(NavigationManager) ? _navigation
                : serviceType == typeof(HttpClient) ? Scenarios
                : null;

            private sealed class Bench : NavigationManager
            {
                public Bench() => Initialize("http://localhost/", "http://localhost/map");
            }
        }

        /// <summary>The scenario file, held on the wire until the test lets it land.</summary>
        private sealed class HeldScenario(TaskCompletionSource<string> release) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string json = await release.Task.WaitAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            }
        }
    }
}
#pragma warning restore BL0006
