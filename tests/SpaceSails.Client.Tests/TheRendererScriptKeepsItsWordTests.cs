using System;
using System.IO;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// The page's two scripts (<c>renderer.js</c>, <c>index.html</c>) run only in a browser, so no sim test can
/// see them break. These guards read the shipped text for the five promises the review found broken:
/// the timer swap never drops a tick the runtime already posted, never hands the runtime a handle a native
/// timer could share, a refused clipboard write is caught, the first-contact chirp keeps the hidden-tab
/// mute, and a missing art image is not a crash.
/// </summary>
public class TheRendererScriptKeepsItsWordTests
{
    private static string Wwwroot() => Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "wwwroot");

    private static string Js() => File.ReadAllText(Path.Combine(Wwwroot(), "renderer.js")).Replace("\r\n", "\n");

    private static string Between(string text, string from, string to)
    {
        int a = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(a >= 0, $"'{from}' is gone from the script");
        int b = text.IndexOf(to, a + from.Length, StringComparison.Ordinal);
        Assert.True(b > a, $"'{to}' no longer follows '{from}'");
        return text[a..b];
    }

    [Fact]
    public void Turning_the_swap_off_does_not_drop_the_ticks_already_posted()
    {
        string swap = Between(Js(), "export function serveTheShortestTimersOnATick", "function rgba(");

        // The old off-branch cleared the map under a pending pump tick, which left the runtime's
        // "already scheduled" latch set for good and stalled every thread-pool continuation.
        Assert.DoesNotContain("tickTimers.clear()", swap, StringComparison.Ordinal);
        Assert.Contains("finishTheDrain", swap, StringComparison.Ordinal);
    }

    [Fact]
    public void A_tick_handle_cannot_be_mistaken_for_a_native_timer_id()
    {
        string js = Js();
        string swap = Between(js, "export function serveTheShortestTimersOnATick", "function rgba(");

        // A bare `return id;` hands out 1, 2, 3 … from the number space Chrome's own timers share.
        Assert.DoesNotContain("return id;", swap, StringComparison.Ordinal);
        Assert.Contains("return tickHandle(id);", swap, StringComparison.Ordinal);
        Assert.Contains("Object.freeze({ tick })", js, StringComparison.Ordinal);
    }

    [Fact]
    public void A_refused_clipboard_write_is_caught_so_it_cannot_overwrite_the_crash_note()
    {
        string copy = Between(Js(), "export function copyText", "/** Forget the vault");

        Assert.Contains("writeText(text).catch(", copy, StringComparison.Ordinal);
    }

    [Fact]
    public void The_first_contact_chirp_keeps_the_hidden_tab_mute_every_other_cue_has()
    {
        string chirp = Between(Js(), "export function playChirp", "audioCtx ??=");

        Assert.Contains("document.visibilityState === 'hidden'", chirp, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_art_image_is_not_recorded_as_a_crash()
    {
        string html = File.ReadAllText(Path.Combine(Wwwroot(), "index.html")).Replace("\r\n", "\n");
        string handler = Between(html, "window.addEventListener('error'", "window.addEventListener('unhandledrejection'");

        int skip = handler.IndexOf("tag === 'IMG'", StringComparison.Ordinal);
        int note = handler.IndexOf("noteCrash('page load'", StringComparison.Ordinal);
        Assert.True(skip >= 0, "the error listener no longer lets an <img> failure through unrecorded");
        Assert.True(skip < note, "the <img> exemption must come before the page-load note is written");
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("repo root");
    }
}
