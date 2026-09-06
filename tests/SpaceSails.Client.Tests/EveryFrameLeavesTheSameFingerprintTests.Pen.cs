using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceSails.Client.Rendering;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · <b>THE TWO STAND-INS AND THE REFLECTION PLUMBING</b> — a pen that records every mark instead
/// of painting it, a renderer that draws nothing at all, and the five one-line helpers every other part of
/// this guard reaches the page through.
///
/// <para>The recording pen is the reason the third named bug class (the sim doing one thing while the
/// drawn shape reports another) is catchable here: it folds each call into a running hash as it goes, so a
/// hundred and twenty frames of a Hive floor cost one hash rather than a megabyte of text.</para>
///
/// <para>The renderer that draws nothing is a bench fixture, not a fake: a component needs a render handle
/// and a dispatcher, and <c>_hasPendingQueuedRender</c> is set so <c>StateHasChanged</c> never queues a
/// batch and <c>UpdateDisplayAsync</c> is never reached.</para>
/// </summary>
public sealed partial class EveryFrameLeavesTheSameFingerprintTests
{
    // ── THE RECORDING PEN ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Every mark the walked view lays, in order, folded into one hash as it goes — so a hundred and
    /// twenty frames of a Hive floor cost one hash instead of a megabyte of text.</summary>
    private sealed class RecordingPen : IRenderer
    {
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private readonly Dictionary<string, int> _images = new(StringComparer.Ordinal);
        private string? _finished;

        public long Commands { get; private set; }

        private void Mark(string line)
        {
            Commands++;
            _hash.AppendData(Encoding.UTF8.GetBytes(line + "\n"));
        }

        public string Sha256()
        {
            _finished ??= Convert.ToHexString(_hash.GetCurrentHash()).ToLowerInvariant();
            return _finished;
        }

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) =>
            Mark($"begin {widthPx} {heightPx} {C(background)}");

        public void EndFrame() => Mark("end");

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Mark($"circle {Num(x)} {Num(y)} {Num(r)} {(fill is { } c ? C(c) : "∅")} {C(stroke)} {Num(w)}");

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            Mark($"polyline {Points(pts)} {C(stroke)} {Num(w)}");

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Mark($"polygon {Points(pts)} {(fill is { } c ? C(c) : "∅")} {C(stroke)} {Num(w)}");

        public void DrawText(float x, float y, string text, RgbaColor color, string font = "12px sans-serif",
            TextAlign align = TextAlign.Left) =>
            Mark($"text {Num(x)} {Num(y)} \"{text}\" {C(color)} {font} {align}");

        public int RegisterImage(string url)
        {
            if (!_images.TryGetValue(url, out int id))
            {
                id = _images.Count + 1;
                _images[url] = id;
            }
            Mark($"register {url} -> {id}");
            return id;
        }

        public void DrawImage(int id, float x, float y, float w, float h, float alpha = 1f) =>
            Mark($"image {id} {Num(x)} {Num(y)} {Num(w)} {Num(h)} {Num(alpha)}");

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
            float x, float y, float w, float h, float alpha = 1f) =>
            Mark($"slice {id} {Num(sx)} {Num(sy)} {Num(sw)} {Num(sh)} " +
                 $"{Num(x)} {Num(y)} {Num(w)} {Num(h)} {Num(alpha)}");

        private static string C(RgbaColor c) => $"#{c.R},{c.G},{c.B},{c.A}";

        private static string Points(ReadOnlySpan<float> pts)
        {
            var sb = new StringBuilder(pts.Length.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < pts.Length; i++) { sb.Append(':').Append(Num(pts[i])); }
            return sb.ToString();
        }
    }

    // ── A RENDERER THAT DRAWS NOTHING ─────────────────────────────────────────────────────────────────

    /// <summary>Enough of a <see cref="Renderer"/> to give the component a render handle and a dispatcher.
    /// It never paints: <c>_hasPendingQueuedRender</c> means <c>StateHasChanged</c> never queues a batch, so
    /// <see cref="UpdateDisplayAsync"/> is never reached.</summary>
#pragma warning disable BL0006 // RenderBatch is "not recommended outside the Blazor framework" — and this IS
                               // the framework's own seam: a bench that wants a component's dispatcher has to
                               // give it a renderer, and a renderer has to be able to say "I drew nothing".
    private sealed class ARendererThatDrawsNothing : Renderer
    {
        public ARendererThatDrawsNothing() : base(NoServices.Instance, NullLoggerFactory.Instance) { }

        public override Dispatcher Dispatcher { get; } = new RightHere();

        public void Attach(IComponent component) => AssignRootComponentId(component);

        protected override void HandleException(Exception exception) =>
            throw new InvalidOperationException("the frame threw inside the renderer", exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;

        /// <summary>Run it now, on this thread, and hand back a completed task — the browser's own behaviour
        /// from inside the rAF callback, and the only way a bench can fingerprint a frame that has finished.</summary>
        private sealed class RightHere : Dispatcher
        {
            public override bool CheckAccess() => true;
            public override Task InvokeAsync(Action workItem) { workItem(); return Task.CompletedTask; }
            public override Task InvokeAsync(Func<Task> workItem) => workItem();
            public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem) =>
                Task.FromResult(workItem());
            public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem) => workItem();
        }

        private sealed class NoServices : IServiceProvider
        {
            public static readonly NoServices Instance = new();
            public object? GetService(Type serviceType) => null;
        }
    }
#pragma warning restore BL0006

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static string RepoRoot() => PinLedger.RepoRoot();

    /// <summary>#870 lane 6′b · The twenty-two patrol fields live on the page's <c>_patrol</c>
    /// object now, so the lookup follows them there (<see cref="PatrolState"/>); every assertion and
    /// every pinned line below still asks for the state by the name it was written with.</summary>
    private static object? Get(object o, string member) =>
        PatrolState.TryFollow(o, member, out object? onTheRound)
            ? onTheRound
            : o.GetType().GetField(member, Hidden)!.GetValue(o);

    /// <inheritdoc cref="Get"/>
    private static void Set(object o, string field, object? value)
    {
        if (!PatrolState.TrySet(o, field, value))
        {
            (o.GetType().GetField(field, Hidden)
             ?? throw new InvalidOperationException($"the component has no `{field}`.")).SetValue(o, value);
        }
    }

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        try
        {
            return call!.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
