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

    /// <summary>#1244 · Has the import above actually finished? Every <c>[JSImport]</c> in this file throws
    /// if the module is not in yet, so a caller that runs DURING the boot — rather than after the renderer
    /// stage, which is where everything else here is called from — has to ask first.</summary>
    private static bool ModuleIsLoaded => _moduleLoadTask is { IsCompletedSuccessfully: true };

    /// <summary>#1244 · Is nobody looking at this page — another tab in front, the window minimised or
    /// occluded, the machine locked? Answers <c>false</c> off a browser and before the module is in, which
    /// is the honest answer for both: a test runner has no tab, and a boot that cannot ask yet must not
    /// guess.
    ///
    /// <para><b>This is a YIELD input and nothing else.</b> The one caller is the staged boot's hand-back
    /// (<c>Map.HandTheFrameBackAsync</c>), which uses it to choose between a timer and a message tick. No
    /// world-building code may read it — the sky a URL builds is the same sky whether or not anyone is
    /// watching it being built, and <c>TheHiddenTabNeverReachesTheWorldTests</c> is the guard.</para></summary>
    internal static bool PageIsHidden() =>
        OperatingSystem.IsBrowser() && ModuleIsLoaded && TheDocumentIsHidden();

    /// <inheritdoc cref="PageIsHidden"/>
    [JSImport("pageIsHidden", ModuleName)]
    private static partial bool TheDocumentIsHidden();

    /// <summary>#1244 · Take the browser's ration off the shortest timers on this page — 4 ms and under,
    /// which is the .NET WASM timer queue's "as soon as you can" and nothing a human ever asks for — by
    /// serving them on a <c>MessageChannel</c> tick, which no browser rations. <c>false</c> puts the
    /// browser's own back. Idempotent in both directions, and a no-op (answering <c>false</c>) off a
    /// browser or before the module import has landed.
    ///
    /// <para>The one caller is the staged boot, which takes the ration off when it finds itself being
    /// rationed and puts it back the moment the boot ends or is abandoned. Nothing else on the page may:
    /// this reaches EVERY short timer in the document while it stands, and it is bearable only because the
    /// boot is short, finite, and already owns the main thread.</para></summary>
    /// <returns>whether the swap is now standing — so a caller can say honestly what it got.</returns>
    internal static bool ServeTheShortestTimersOnATick(bool on) =>
        OperatingSystem.IsBrowser() && ModuleIsLoaded && TheShortestTimersOnATick(on);

    /// <inheritdoc cref="ServeTheShortestTimersOnATick"/>
    [JSImport("serveTheShortestTimersOnATick", ModuleName)]
    private static partial bool TheShortestTimersOnATick(bool on);

    [JSImport("initCanvas", ModuleName)]
    internal static partial void InitCanvas(string canvasId, bool observeResize);

    [JSImport("startLoop", ModuleName)]
    internal static partial void StartLoop(string canvasId);

    [JSImport("stopLoop", ModuleName)]
    internal static partial void StopLoop(string canvasId);

    /// <summary>Fire an audio cue ("pulse", "vent", "board", "arc"). Decoration only — JS
    /// swallows every audio failure, so callers never need to guard.
    ///
    /// <para>#837 · …AND NEITHER DOES A BENCH WITH NO SPEAKER IN IT. The deck-audit project drives the
    /// SHIPPING acts (that is the whole reason it references the client), and an act that ends in a cue was
    /// throwing <c>PlatformNotSupportedException</c> off-browser — so the one shape of guard this repo
    /// trusts, the one that presses what the page presses, could not be written for any verb that makes a
    /// noise. A cue is decoration in a browser and it is decoration on a test runner; the only difference is
    /// that one of them has an audio context. Silent there, unchanged here.</para></summary>
    /// <param name="kind">A key of <c>renderer.js</c>'s <c>CUES</c> table. A name the table does not carry
    /// makes no sound and no complaint — <c>EveryCueTheGameFiresHasAVoiceTests</c> is the guard that keeps
    /// the two halves honest (#938 D1).</param>
    /// <param name="scale">#167 — how big the event was, 0…1: length and loudness both move with it, so a
    /// one-pulse trim and a forty-pulse orbital insertion do not sound alike. The default of 1 is the cue
    /// exactly as its table entry was tuned, which is what every caller that says nothing gets.</param>
    internal static void PlayCue(string kind, double scale = 1.0)
    {
        if (OperatingSystem.IsBrowser())
        {
            PlayTheCue(kind, scale);
        }
    }

    /// <inheritdoc cref="PlayCue"/>
    [JSImport("playCue", ModuleName)]
    private static partial void PlayTheCue(string kind, double scale);

    /// <summary>#338 addendum — THE GAME'S FIRST SOUND: the motion tracker's first-contact chirp (two short
    /// rising tones). Fired on the Core-gated 0→N tracker edge; respects the master audio switch JS-side.</summary>
    [JSImport("playChirp", ModuleName)]
    internal static partial void PlayChirp();

    /// <summary>#338 addendum item 4 — unlock the WebAudio context on a user gesture so a cue fired later
    /// from the rAF loop (the chirp) can actually sound. Safe to call on every keydown; JS is idempotent.</summary>
    [JSImport("armAudio", ModuleName)]
    internal static partial void ArmAudio();

    /// <summary>#338 addendum — flip the master audio switch (default ON). Remembered browser-locally.</summary>
    [JSImport("setAudioEnabled", ModuleName)]
    internal static partial void SetAudioEnabled(bool on);

    /// <summary>#338 addendum — the remembered audio-on state, so C# can label its toggle in step.</summary>
    [JSImport("getAudioEnabled", ModuleName)]
    internal static partial bool GetAudioEnabled();

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

    /// <summary>Preload a raster image by id so later <c>OP_IMAGE</c> draws can blit it. Fire-and-forget:
    /// JS creates an <c>Image</c>, decodes it asynchronously, and caches it under <paramref name="id"/>.
    /// The float command buffer can only carry the id + dest rect, never pixels — so the bitmap must
    /// live JS-side. Safe to call repeatedly for the same id (JS ignores a re-load).</summary>
    [JSImport("loadImage", ModuleName)]
    internal static partial void LoadImage(int id, string url);

    // ─── The personal vault (#225): localStorage + export/import file interop. ───
    // Same module as the renderer (functions appended to renderer.js). All are defensive JS-side, so
    // a private-mode storage throw or a cancelled picker never surfaces as an exception here.

    /// <summary>Read the saved vault JSON from localStorage, or null if none is stored.</summary>
    [JSImport("vaultRead", ModuleName)]
    internal static partial string? VaultRead(string key);

    /// <summary>Write the vault JSON to localStorage; false if storage refused it (quota/private mode).</summary>
    [JSImport("vaultWrite", ModuleName)]
    internal static partial bool VaultWrite(string key, string json);

    /// <summary>Forget the stored vault (a fresh start that abandons the save).</summary>
    [JSImport("vaultClear", ModuleName)]
    internal static partial void VaultClear(string key);

    /// <summary>Download the vault as a .json file the owner keeps against a server wipe.</summary>
    [JSImport("vaultDownload", ModuleName)]
    internal static partial void VaultDownload(string filename, string json);

    /// <summary>Put text on the system clipboard (the crash note's [copy] button). Defensive JS-side:
    /// a browser that refuses clipboard access simply returns false.</summary>
    [JSImport("copyText", ModuleName)]
    internal static partial bool CopyText(string text);

    /// <summary>#992 · Scroll the first element matching <paramref name="selector"/> just inside its own
    /// scrollers (<c>block: 'nearest'</c> — an element already in view is not moved).
    ///
    /// <para>Decoration, like <see cref="PlayCue"/>: the browser-only call is behind the same guard so a
    /// bench with no DOM in it can drive the shipping act without a <c>PlatformNotSupportedException</c>
    /// (#837), and the JS side swallows its own failures. A page that will not scroll for us still leaves
    /// the list scrollable by hand — nothing the captain needs depends on this landing.</para></summary>
    internal static void ScrollIntoView(string selector)
    {
        if (OperatingSystem.IsBrowser())
        {
            ScrollTheElementIntoView(selector);
        }
    }

    [JSImport("scrollIntoView", ModuleName)]
    private static partial void ScrollTheElementIntoView(string selector);

    /// <summary>Open a file picker and resolve the chosen .json file's text (empty if cancelled).</summary>
    [JSImport("vaultImport", ModuleName)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    internal static partial Task<string> VaultImport();

    /// <summary>Raised once per animation frame by <c>renderer.js</c>'s render loop.</summary>
    public static event Action<double>? FrameTick;

    /// <summary>Raised when the canvas element's on-screen size actually changes (not every frame).</summary>
    public static event Action<double, double>? CanvasResized;

    /// <summary>The rAF callback, and the one door almost everything this game does comes through. An
    /// exception thrown in here used to reach JavaScript as Blazor's textless "An unhandled error has
    /// occurred" (owner's playtest, 2026-08-22) — so it is READ on the way past and written to the ship's
    /// black box (<see cref="CrashLog"/>) before it carries on doing exactly what it did before.</summary>
    [JSExport]
    internal static void Tick(double highResTimestampMs) =>
        CrashLog.RunGuarded("frame tick", () => FrameTick?.Invoke(highResTimestampMs));

    [JSExport]
    internal static void OnResize(double widthPx, double heightPx) => CanvasResized?.Invoke(widthPx, heightPx);
}
