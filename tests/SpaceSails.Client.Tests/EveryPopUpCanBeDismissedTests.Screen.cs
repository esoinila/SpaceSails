using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Components;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #992 · <b>GUARDS 2 AND 3 — THE SAME QUESTIONS, ASKED OF WHAT WAS DRAWN</b>, and the plumbing that draws
/// it.
///
/// <para>What this part owns is the half of <see cref="EveryPopUpCanBeDismissedTests"/> that costs a render:
/// guard 2 walks what the renderer actually emitted across the world matrix and asks whether anything on the
/// screen escaped the register, and guard 3 is the owner's ruling itself — every pop-up the bench can raise
/// offers a way out, proved by PRESSING it rather than by believing the row.</para>
/// </summary>
public sealed partial class EveryPopUpCanBeDismissedTests
{
    // ── Guard 2 · the same question, asked of what was drawn ──────────────────────────────────────────

    /// <summary>
    /// EVERY OVERLAY ROOT THAT REACHES THE SCREEN IS IN THE REGISTER.
    ///
    /// <para>Guard 1 reads class ATTRIBUTES out of the source, so a class list assembled in C# — a splatted
    /// dictionary, a name built from data, a <c>@(cond ? "a-backdrop" : "")</c> — would slip past it. This
    /// walks what the renderer actually emitted, across the same world matrix
    /// <see cref="EveryDeskBootsTests"/> sweeps, and asks the same question of the DOM.</para>
    /// </summary>
    [Fact]
    public async Task NoSurfaceOnTheScreenEscapesTheRegister()
    {
        var registered = TheRegister.Select(p => p.RootClass).ToHashSet(StringComparer.Ordinal);
        var strangers = new SortedSet<string>(StringComparer.Ordinal);

        foreach (string url in new[] { FreeFlying, Docked, Ashore })
        {
            using DeskBench bench = await DeskBench.BootAsync(url);

            foreach (ShipDesk desk in DeskBench.TabBarOrder)
            {
                await bench.SwitchAsync(desk);
                DeskBench.Painted painted = await bench.RenderAsync();

                foreach (string[] classes in painted.Root.Descendants().Select(n => n.Classes.ToArray())
                             .Concat(painted.MarkupBlobs.SelectMany(ClassListsIn)))
                {
                    if (IsAnUnregisteredSurface(classes, registered))
                    {
                        strangers.Add(
                            $"{string.Join(' ', classes.Where(IsAPopUpRoot))}  (drawn at {url} · {desk})");
                    }
                }
            }
        }

        Assert.True(strangers.Count == 0,
            $"{strangers.Count} surface(s) reached the screen wearing a pop-up root class that the register "
            + "does not know:\n  - " + string.Join("\n  - ", strangers));
    }

    // ── Guard 3 · the law itself, proved by pressing ──────────────────────────────────────────────────

    /// <summary>
    /// THE RULING, AS A LAW: raise it, press everything in it, and see what makes it go.
    ///
    /// <para>Every driveable row in the register is opened, and every visible control inside the surface's own
    /// subtree is pressed in turn — each from a freshly re-raised surface, so one control's press cannot be
    /// mistaken for another's. A control counts as a way out when the next render no longer shows the surface,
    /// shows it <c>d-none</c>, or shows it wearing a tile class: closed and minimised are both dismissals, and
    /// the owner's ruling names both.</para>
    ///
    /// <para>The BACKDROP is deliberately not counted. Most cards in this codebase close when the scrim behind
    /// them is clicked, and that is a fine convenience and a poor affordance — it is invisible, and a player
    /// looking at a card with three answers on it and no ✕ has no way to learn it is there. So a surface whose
    /// only exit is its own root's <c>onclick</c> fails this law, which is exactly what the face scene's
    /// answer phase did before #992 gave it a ✕.</para>
    /// </summary>
    [Fact]
    public async Task EveryPopUpTheBenchCanRaiseOffersAWayOut()
    {
        var wrong = new List<string>();
        int proved = 0;

        // ONE BENCH PER SURFACE, and it is not a tidiness preference — the first build shared a bench across
        // every row in a world and the law lied to itself. Five surfaces in this client are rooted on
        // .convergence-backdrop; the face scene's last press leaves _faceScene set (an answer is not a
        // close), so the NEXT row raised the Convergence, pressed its ✕, and then found the face scene still
        // wearing the class it was looking for and scored the Convergence as unclosable. A law that reports a
        // surface by the class of a different surface is this repository's first named bug class wearing a
        // backdrop. A fresh page per row costs a second and cannot be confused.
        foreach (PopUp popUp in TheRegister.Where(p => p.Raise is not null))
        {
            using DeskBench bench = await DeskBench.BootAsync(popUp.World);
            await bench.SwitchAsync(popUp.At);

            proved++;
            foreach (string complaint in await WhatIsWrongWith(bench, popUp))
            {
                wrong.Add($"{popUp.Name}: {complaint}");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {proved} raised pop-ups broke the owner's ruling of 2026-08-24 (\"there should "
            + $"not be a pop-up that cannot be closed or minimized\"):\n  - " + string.Join("\n  - ", wrong));
    }

    private static async Task<IEnumerable<string>> WhatIsWrongWith(DeskBench bench, PopUp popUp)
    {
        DeskBench.Painted.Node? surface = await Raise(bench, popUp);
        if (surface is null)
        {
            // The gate was set and nothing appeared. Never a pass: a law that shrugged here would be a guard
            // handed the wrong world — this repository's fifth named bug class — and it would go on passing
            // for every surface that had quietly stopped rendering.
            return [$"raising it drew NOTHING wearing .{popUp.RootClass}. The register's driver and the "
                    + "markup's gate have come apart; one of them has moved."];
        }

        // Every control the surface owns, the root itself excluded: the backdrop's own onclick is the
        // invisible exit this law does not accept as the only one.
        var controls = surface.Descendants()
            .Where(n => n.Handlers.ContainsKey("onclick") && !n.Hidden)
            .Select(n => n.Name)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (controls.Count == 0)
        {
            return [$"it has no control in it at all. It is drawn, it is on top, and there is no way out of "
                    + "it — the exact shape the ruling forbids."];
        }

        var closers = new List<string>();
        foreach (string control in controls)
        {
            if (await PressingItEndsTheSurface(bench, popUp, control))
            {
                closers.Add(control);
            }
        }

        if (closers.Count == 0)
        {
            return [$"none of its {controls.Count} control(s) took it off the screen when pressed — "
                    + $"[{string.Join(" · ", controls)}]. Whatever they do, none of them is a way out."];
        }

        if (popUp.HowItEnds == Exit.EveryControlCloses && closers.Count != controls.Count)
        {
            return [$"it is registered as a critical-DECISION modal — allowed no ✕ only because every answer "
                    + $"it offers is itself a close — but pressing them proved otherwise: "
                    + $"[{string.Join(" · ", controls.Except(closers, StringComparer.Ordinal))}] left it up. "
                    + "Either give it a ✕ or make every answer end it."];
        }

        return [];
    }

    /// <summary>Put the surface up and hand back its root node, or null when nothing was drawn.</summary>
    private static async Task<DeskBench.Painted.Node?> Raise(DeskBench bench, PopUp popUp)
    {
        popUp.Raise!(bench);
        DeskBench.Painted painted = await bench.RenderAsync();
        return TheSurface(painted, popUp.RootClass);
    }

    private static DeskBench.Painted.Node? TheSurface(DeskBench.Painted painted, string rootClass) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass(rootClass) && !n.Hidden);

    /// <summary>
    /// Raise it again, find the named control, PRESS IT, and re-read the page.
    ///
    /// <para>Re-raised per press on purpose: pressing a control that closes the surface and then looking for
    /// the next one would find nothing and score every later control as a non-closer, which would fail the
    /// decision exception on every modal that has one.</para>
    /// </summary>
    private static async Task<bool> PressingItEndsTheSurface(DeskBench bench, PopUp popUp, string control)
    {
        DeskBench.Painted.Node? surface = await Raise(bench, popUp);
        DeskBench.Painted.Node? button = surface?.Descendants()
            .FirstOrDefault(n => !n.Hidden
                                 && n.Handlers.ContainsKey("onclick")
                                 && string.Equals(n.Name, control, StringComparison.Ordinal));

        if (button is null)
        {
            return false;
        }

        await bench.PressAsync(button.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();
        DeskBench.Painted.Node? still = TheSurface(after, popUp.RootClass);

        // Gone, hidden, or tucked into a tile. #963's scope and #960's dossier both minimise by staying in
        // the tree, so "it is still in the DOM" is not the question — "is it still taking the screen" is.
        return still is null || still.Classes.Any(c => c.EndsWith("-tile", StringComparison.Ordinal));
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Every <c>class="…"</c> in a run of text, as its own token list. Used on the source and on the
    /// static-markup blobs the render tree hands out, which are the same shape.
    ///
    /// <para><b>The scan is PAREN-AWARE, and that is the difference between a law that reads the file and a
    /// law that reads most of it.</b> The first build matched the attribute with <c>class="([^"]*)"</c>,
    /// which is right for HTML and wrong for Razor: a class CHOSEN BY AN EXPRESSION carries double quotes
    /// inside its own value, so that regex stopped at the first of them and handed this guard a token list
    /// made of C# fragments. The station oracle's card is written exactly that way — its root class is one
    /// arm of a ternary — and it had been invisible to guard 1 since the day the guard was written. Nothing
    /// was WRONG (the class it wears is registered, by another row), but the guard was not covering it, and
    /// the next ternary-classed pop-up would have escaped in the same silence. Found by re-running #992's
    /// audit rather than by anything failing, which is the whole reason an audit gets re-run.</para>
    ///
    /// <para>So the value's end is found by scanning rather than by matching: a <c>"</c> closes it only at
    /// paren depth zero, which leaves every quote inside an <c>@@( … )</c> where it belongs. The value is
    /// then split on quotes as well as on whitespace, so BOTH arms of a ternary come out as tokens and a
    /// class named in either one is read. The C# fragments that come out with them are harmless: they match
    /// no root and are in no register.</para>
    /// </summary>
    private static IEnumerable<string[]> ClassListsIn(string text)
    {
        foreach (Match opening in Regex.Matches(text, "class=\""))
        {
            int at = opening.Index + opening.Length;
            int depth = 0;
            int end = at;
            while (end < text.Length)
            {
                char here = text[end];
                if (here == '(')
                {
                    depth++;
                }
                else if (here == ')')
                {
                    depth--;
                }
                else if (here == '"' && depth <= 0)
                {
                    break;
                }

                end++;
            }

            yield return text[at..end]
                .Split([' ', '\t', '\r', '\n', '"'], StringSplitOptions.RemoveEmptyEntries);
        }
    }

    private static IEnumerable<string> RazorFiles() =>
        Directory.EnumerateFiles(ClientSource(), "*.razor", SearchOption.AllDirectories);

    /// <summary>The shipping client's source, found from the test binary the way this repo's other
    /// source-shape guards find it — up out of bin/ and across.</summary>
    private static string ClientSource()
    {
        var at = new DirectoryInfo(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException(
            "src/SpaceSails.Client is not above the test binary — this guard reads the markup as typed and "
            + "cannot do its job without it.");
    }
}
