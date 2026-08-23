using System;
using System.Collections.Generic;
using System.IO;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #963 follow-up · THE FRAME THAT THREW LEAVES A SENTENCE BEHIND.
///
/// <para>Owner's playtest, 2026-08-22: two screenshots end at Blazor's <i>"An unhandled error has
/// occurred. Reload."</i> and nothing else. The console had the real text; the console died with his tab,
/// so the bug arrived as a photograph of a sentence that names nothing.</para>
///
/// <para>Nearly everything this game does happens inside the rAF tick, which means the tick is where a
/// crash escapes. This drives the ACTUAL door — <c>RendererInterop.Tick</c>, the [JSExport] JavaScript
/// calls sixty times a second — with a handler that throws, and asks whether the ship's log ends up
/// holding something a captain could paste into an issue. Asking a copy of the wiring would prove
/// nothing: the whole bug class here is a guard that is not actually in the path.</para>
///
/// <para>These tests share one static log, so they run in a collection of their own.</para>
/// </summary>
[Collection(nameof(TheShipKeepsItsOwnBlackBoxTests))]
[CollectionDefinition(nameof(TheShipKeepsItsOwnBlackBoxTests), DisableParallelization = true)]
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheShipKeepsItsOwnBlackBoxTests : IDisposable
{
    public TheShipKeepsItsOwnBlackBoxTests() => CrashLog.Reset();

    public void Dispose() => CrashLog.Reset();

    /// <summary>THE LAW: an exception thrown inside a frame handler lands in the ledger, with the words
    /// that identify it, and still goes on to do exactly what it did before.</summary>
    [Fact]
    public void AnExceptionInAFrameHandlerLandsInTheLedger()
    {
        void Thrower(double _) => throw new InvalidOperationException("the lift only went down");

        RendererInterop.FrameTick += Thrower;
        try
        {
            // It still throws — the black box changes what is KNOWN about a crash, never what it does.
            Assert.Throws<InvalidOperationException>(() => RendererInterop.Tick(16.7));
        }
        finally
        {
            RendererInterop.FrameTick -= Thrower;
        }

        CrashNote note = Assert.Single(CrashLog.Entries);
        Assert.Equal("InvalidOperationException", note.TypeName);
        Assert.Equal("the lift only went down", note.Message);
        Assert.Contains("frame tick", note.Source);
        Assert.NotEmpty(note.Frames);
    }

    /// <summary>…and it goes somewhere that OUTLIVES the reload. This is the entire point: the console is
    /// gone by the time the owner is filing the issue, so the note has to be waiting for him instead.</summary>
    [Fact]
    public void TheNoteIsWrittenSomewhereThatSurvivesTheReload()
    {
        var storage = new Dictionary<string, string>();
        CrashLog.Persist = note => storage[CrashLog.StorageKey] = note.ToStorage();

        void Thrower(double _) => throw new InvalidOperationException("the sail would not furl");

        RendererInterop.FrameTick += Thrower;
        try
        {
            Assert.Throws<InvalidOperationException>(() => RendererInterop.Tick(1));
        }
        finally
        {
            RendererInterop.FrameTick -= Thrower;
        }

        Assert.True(storage.ContainsKey(CrashLog.StorageKey), "nothing was written for the next voyage to read");

        // …and the next voyage can read it back whole.
        Assert.True(CrashNote.TryParse(storage[CrashLog.StorageKey], out CrashNote restored));
        Assert.Equal("the sail would not furl", restored.Message);
        Assert.Contains("the sail would not furl", restored.Describe());
    }

    /// <summary>A handler that throws throws EVERY frame. Sixty identical notes a second is a memory leak
    /// wearing a bug report's clothes, so consecutive repeats fold into one.</summary>
    [Fact]
    public void TheSameCrashEveryFrameIsStillOneNote()
    {
        void Thrower(double _) => throw new InvalidOperationException("every single frame");

        RendererInterop.FrameTick += Thrower;
        try
        {
            for (int i = 0; i < 120; i++)
            {
                Assert.Throws<InvalidOperationException>(() => RendererInterop.Tick(i));
            }
        }
        finally
        {
            RendererInterop.FrameTick -= Thrower;
        }

        Assert.Single(CrashLog.Entries);
    }

    /// <summary>A clean frame writes nothing — the desk must not cry wolf at a voyage that went fine.</summary>
    [Fact]
    public void ACleanFrameLeavesTheLogEmpty()
    {
        int frames = 0;
        void Counter(double _) => frames++;

        RendererInterop.FrameTick += Counter;
        try
        {
            RendererInterop.Tick(1);
            RendererInterop.Tick(2);
        }
        finally
        {
            RendererInterop.FrameTick -= Counter;
        }

        Assert.Equal(2, frames);
        Assert.Empty(CrashLog.Entries);
        Assert.Null(CrashLog.Latest);
    }

    /// <summary>THE REPORTER MAY NOT CRASH. A localStorage that refuses the write (private mode, quota)
    /// must lose the note, never take the game down with it.</summary>
    [Fact]
    public void AStorageThatRefusesTheWriteDoesNotBecomeASecondCrash()
    {
        CrashLog.Persist = _ => throw new InvalidOperationException("quota exceeded");

        CrashNote note = CrashLog.Report("frame tick", new FormatException("a heading that is not a number"));

        Assert.Equal("FormatException", note.TypeName);
        Assert.Single(CrashLog.Entries);
    }

    /// <summary>The [copy] target: the desk shows the note, and clearing it leaves the desk quiet.</summary>
    [Fact]
    public void ClearingTheNoteLeavesTheDeskQuiet()
    {
        CrashLog.Report("component", new InvalidOperationException("filed already"));
        Assert.NotNull(CrashLog.Latest);

        CrashLog.Forget();

        Assert.Null(CrashLog.Latest);
        Assert.Empty(CrashLog.Entries);
    }

    /// <summary>The JS half of the black box (index.html) catches what never reaches .NET at all — a
    /// renderer.js throw, a rejected interop promise — and it must write the SAME format this side reads.
    /// A bridge that writes a dialect nobody parses is a bridge to nowhere.</summary>
    [Fact]
    public void TheJavaScriptBridgeWritesTheFormatThisSideReads()
    {
        string html = File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "wwwroot", "index.html"));

        Assert.Contains(CrashLog.StorageKey, html);
        Assert.Contains("'crash-v1'", html);
        Assert.Contains("unhandledrejection", html);

        // …and the format itself is the one CrashNote.TryParse accepts, header and all.
        Assert.True(CrashNote.TryParse(
            string.Join("\n", "crash-v1", "browser", "JsError", "renderer.js blew up", "638000000000000000", "at draw"),
            out CrashNote fromJs));
        Assert.Equal("renderer.js blew up", fromJs.Message);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary");
    }
}
