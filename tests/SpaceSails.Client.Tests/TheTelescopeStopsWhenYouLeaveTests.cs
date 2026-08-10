using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceSails.Client.Pages.Stations;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

// BL0006: subclassing Renderer is how you get the REAL first render, the real OnAfterRenderAsync and the
// real unhandled-exception hook — the three things this guard is about. A hand-written stand-in for the
// renderer would be a guard against the stand-in. Scoped to this file (the same licence #737 took).
#pragma warning disable BL0006

namespace SpaceSails.Client.Tests;

/// <summary>
/// #765 · THE SAME UNSTOPPED BOOT, ONE COMPONENT DOWN — and this one opens the window earlier and holds
/// it open longer than Map's did.
///
/// <para>Map.razor renders the tracking post ALWAYS (<c>FullScreen="true"</c>, <c>d-none</c> off-desk) so
/// that a desk switch can never destroy the ledger. The price is that
/// <c>TrackingPost.OnAfterRenderAsync</c> runs on the map's very FIRST render, awaits the renderer module,
/// and parks there — while Map itself only awaits that same import at the END of a multi-second boot. Back
/// out of a slow load in that gap and the continuation resumes into a component the router has already
/// discarded, and walks straight past its own <c>Dispose</c>: it subscribes a dead tracking post to the
/// STATIC <see cref="RendererInterop.FrameTick"/> — nothing will ever unsubscribe it — and names card
/// canvases by id that left the page with it, which <c>renderer.js</c> throws over rather than shrugging.
/// The <c>WebAssemblyRenderer[100]</c> shape from #734, plus a leak that outlives the page.</para>
///
/// <para>These tests drive the SHIPPING component through a real Blazor <see cref="Renderer"/> — the first
/// render, the wiring pass it starts, the <c>Dispose</c> the router performs on navigation — and watch the
/// two places production watches: the task <c>OnAfterRenderAsync</c> returned, and the renderer's own
/// unhandled-exception hook, which in the browser is the line in the ticket.</para>
///
/// <para>To watch them go red on the old behaviour, delete <c>_leftTheDesk.Cancel();</c> from
/// <c>TrackingPost.Dispose</c> — that one line is the whole difference between a telescope that stops when
/// its page goes and the one that did not.</para>
/// </summary>
public sealed class TheTelescopeStopsWhenYouLeaveTests
{
    [Fact]
    public async Task LeavingWhileTheModuleIsStillLoadingRaisesNOTHING()
    {
        // The repro, in order: the sensors desk renders, its wiring pass parks on the renderer-module
        // import, the player backs out of the slow load, and only THEN does the module land — so the
        // continuation resumes into a page that is already gone.
        using var bench = new ScopeBench();
        await bench.ArriveAtTheSensorsDeskAsync();

        await bench.NavigateAwayAsync();
        bench.LetTheModuleLand();

        await bench.Post.WiringTheScopes!;

        Assert.Empty(bench.UnhandledFromTheWiring);
    }

    [Fact]
    public async Task TheAbandonedWiringNeverRidesTheFrameTickNothingCouldUnsubscribe()
    {
        // The second half of the same fault, and the more expensive half: the wiring pass did not merely
        // throw on its way to the canvases, it went THROUGH the static frame tick — hanging a discarded
        // component off an event that outlives every page. Dispose had already run by then, so its
        // "unsubscribe if subscribed" found nothing to do and nothing was ever going to come back for it.
        using var bench = new ScopeBench();
        await bench.ArriveAtTheSensorsDeskAsync();

        await bench.NavigateAwayAsync();
        bench.LetTheModuleLand();
        await bench.Post.WiringTheScopes!;

        Assert.True(bench.Post.LeftTheDeskToken.IsCancellationRequested);
        Assert.False(bench.Post.RidingTheFrameTick);
        Assert.Equal(0, CardScopesWired(bench.Post));
    }

    [Fact]
    public async Task AWiringNOBODYLeftStillGoesAllTheWayToTheBROWSER()
    {
        // The discriminator: without it, "stops early" would be indistinguishable from "never runs". No
        // navigation here — the same bench, the same held track on the ledger, and the wiring pass walks
        // the whole way to InitCanvas, where a test host that is not a browser cannot follow it. Reaching
        // a browser API off-browser is the proof that nothing short-circuited; the abandoned pass above
        // never gets here.
        using var bench = new ScopeBench();
        await bench.ArriveAtTheSensorsDeskAsync();

        bench.LetTheModuleLand();

        await Assert.ThrowsAnyAsync<Exception>(() => bench.Post.WiringTheScopes!);
        Assert.False(bench.Post.LeftTheDeskToken.IsCancellationRequested);
        Assert.True(bench.Post.RidingTheFrameTick);
    }

    [Fact]
    public void DisposeIsWhatTELLSTheTelescopeItsPageIsGone()
    {
        // The seam itself, on a post that never wired anything: Dispose must be safe on a tracking post
        // whose renderer stage was never reached (no subscription to drop, no canvas to name) and must
        // leave the wiring pass's token cancelled.
        var post = new TrackingPost();

        Assert.False(post.LeftTheDeskToken.IsCancellationRequested);
        post.Dispose();

        Assert.True(post.LeftTheDeskToken.IsCancellationRequested);
    }

    /// <summary>How many live per-card scope views the post holds — anything above zero means the wiring
    /// pass got past the gate and named a canvas by id.</summary>
    private static int CardScopesWired(TrackingPost post) =>
        ((System.Collections.ICollection)typeof(TrackingPost)
            .GetField("_cardScopes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(post)!).Count;

    /// <summary>
    /// A real Blazor renderer with a hand-held clock: the tracking post renders and wires for real, but the
    /// renderer-module import it parks on only lands when the test says so — which is what makes
    /// "mid-load" an instant the test can name rather than a race it has to win.
    /// </summary>
    private sealed class ScopeBench : Renderer
    {
        private readonly TaskCompletionSource _module = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly List<Exception> _unhandled = [];
        private int _componentId;

        public ScopeBench()
            : base(new Services(), NullLoggerFactory.Instance)
        {
            Post = new PostOnAHeldModule(_module.Task);

            // One held track, seeded through the post's own public door (the same one laser ranging uses),
            // so that a wiring pass which gets past the gate has an actual card canvas to name. An empty
            // ledger would let the discriminator pass for the wrong reason.
            Post.ApplyObservation(new Observation("victoria-i", 0, new Vector2d(2e11, 0), new Vector2d(0, 1.5e4)));
        }

        public TrackingPost Post { get; }

        /// <summary>What the renderer would have logged as <c>WebAssemblyRenderer[100]</c> for the tracking
        /// post's own wiring pass.</summary>
        public IEnumerable<Exception> UnhandledFromTheWiring =>
            _unhandled.Where(e => e.ToString().Contains("TrackingPost", StringComparison.Ordinal));

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public async Task ArriveAtTheSensorsDeskAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                _componentId = AssignRootComponentId(Post);
                return RenderRootComponentAsync(_componentId, ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        ["SimTime"] = 0.0,
                        ["ShipPosition"] = new Vector2d(1.5e11, 0),
                        ["ShipVelocity"] = new Vector2d(0, 2.9e4),
                        ["Visible"] = true,
                        ["FullScreen"] = true,
                    }));
            });

            // The first render is what starts the wiring pass, and its first await is the module import —
            // so by here the desk is genuinely up and waiting, exactly as it is during a slow boot.
            Assert.NotNull(Post.WiringTheScopes);
            Assert.False(Post.WiringTheScopes!.IsCompleted);
        }

        public Task NavigateAwayAsync() => Dispatcher.InvokeAsync(Post.Dispose);

        public void LetTheModuleLand() => _module.TrySetResult();

        protected override void HandleException(Exception exception) => _unhandled.Add(exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;

        protected override void Dispose(bool disposing)
        {
            // Dispose the post FIRST: a wiring pass still parked on the import has to learn that its page
            // is gone before the import is cancelled under it, or the cancellation would not be ours and
            // the catch would rightly refuse to swallow it. Also drops any FrameTick subscription the
            // discriminator left on the static event, so no test leaks a live handler into the next one.
            Post.Dispose();
            _module.TrySetCanceled();
            base.Dispose(disposing);
        }

        /// <summary>The tracking post injects nothing; the base renderer still asks for its logging.</summary>
        private sealed class Services : IServiceProvider
        {
            public object? GetService(Type serviceType) =>
                serviceType == typeof(ILoggerFactory) ? NullLoggerFactory.Instance : null;
        }

        /// <summary>The shipping tracking post with its ONE test seam taken: the renderer-module import,
        /// held open so "the player left mid-load" is an instant this bench can choose. Everything the
        /// guard is actually about — OnAfterRenderAsync, the token through the yield, the gate before the
        /// DOM, Dispose — is the shipping code, untouched.</summary>
        private sealed class PostOnAHeldModule(Task heldModule) : TrackingPost
        {
            internal override Task LoadTheRendererModuleAsync() => heldModule;
        }
    }
}
#pragma warning restore BL0006
