using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// The JS interop boundary for <c>wwwroot/renderer.js</c>. Uses the fast
/// <see cref="System.Runtime.InteropServices.JavaScript"/> (<c>[JSImport]</c>/<c>[JSExport]</c>) path
/// rather than <c>IJSInProcessRuntime</c>, so the per-frame vertex buffer crosses as a zero-copy
/// <see cref="JSType.MemoryView"/> over a <c>Span&lt;float&gt;</c> instead of a JSON-serialized array.
///
/// <para>Two directions:</para>
/// <list type="bullet">
///   <item><b>C# → JS</b> (<c>[JSImport]</c>): thin wrappers around the exported functions in
///   <c>renderer.js</c>. Requires the module to be loaded once via
///   <see cref="EnsureModuleLoadedAsync"/> before first use.</item>
///   <item><b>JS → C#</b> (<c>[JSExport]</c>): <see cref="Tick"/> is called once per animation frame
///   by the <c>requestAnimationFrame</c> loop that <c>renderer.js</c> owns; <see cref="OnResize"/> is
///   called only when a <c>ResizeObserver</c> in JS detects the canvas changed size (rare — not a
///   per-frame cost). Both are plumbed to C# subscribers via events rather than being called
///   directly, so <see cref="RendererInterop"/> itself stays free of any Blazor/page state.</item>
/// </list>
/// </summary>
[SupportedOSPlatform("browser")]
internal static partial class RendererInterop
{
    private const string ModuleName = "renderer";

    private static Task? _moduleLoadTask;

    /// <summary>Imports <c>wwwroot/renderer.js</c> as an ES module. Safe to call repeatedly.</summary>
    public static Task EnsureModuleLoadedAsync() =>
        _moduleLoadTask ??= JSHost.ImportAsync(ModuleName, "../renderer.js");

    [JSImport("initCanvas", ModuleName)]
    internal static partial void InitCanvas(string canvasId);

    [JSImport("startLoop", ModuleName)]
    internal static partial void StartLoop(string canvasId);

    [JSImport("stopLoop", ModuleName)]
    internal static partial void StopLoop(string canvasId);

    /// <summary>
    /// Flushes the whole batched command buffer for one frame in a single call. <paramref name="buffer"/>
    /// is handed to JS as a <see cref="JSType.MemoryView"/> (a short-lived view over the WASM linear
    /// memory backing the C# array) — no per-primitive round trips and no JSON for the hot path.
    ///
    /// <para>The source-generated JS interop only supports <see cref="JSType.MemoryView"/> over
    /// <c>Span&lt;byte&gt;</c>, not <c>Span&lt;float&gt;</c> directly (<c>SYSLIB1072</c>), so
    /// <see cref="CanvasRenderer"/> reinterprets its float buffer as bytes with
    /// <see cref="System.Runtime.InteropServices.MemoryMarshal.AsBytes{T}(Span{T})"/> before the
    /// call; <paramref name="floatCount"/> tells JS how many floats that byte range decodes to, so
    /// it can rebuild a <c>Float32Array</c> view over the copy it receives.</para>
    /// </summary>
    [JSImport("drawFrame", ModuleName)]
    internal static partial void DrawFrame(
        string canvasId,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> buffer,
        int floatCount);

    /// <summary>Text is rare (~10 labels/frame) so it rides as a small JSON payload, not the float buffer.</summary>
    [JSImport("drawTexts", ModuleName)]
    internal static partial void DrawTexts(string canvasId, string json);

    /// <summary>Raised once per animation frame by <c>renderer.js</c>'s render loop.</summary>
    public static event Action<double>? FrameTick;

    /// <summary>Raised when the canvas element's on-screen size actually changes (not every frame).</summary>
    public static event Action<double, double>? CanvasResized;

    [JSExport]
    internal static void Tick(double highResTimestampMs) => FrameTick?.Invoke(highResTimestampMs);

    [JSExport]
    internal static void OnResize(double widthPx, double heightPx) => CanvasResized?.Invoke(widthPx, heightPx);
}
