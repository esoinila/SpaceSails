using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// EVERY DESK BOOTS — the owner's method, made a law.
///
/// <para><b>Why this file exists.</b> The Trade desk crashed the render on this base branch for weeks and
/// nothing noticed. A Razor comment wedged between a button's attributes is not a comment: Razor compiles the
/// whole <c>@* … *@</c> run into an ATTRIBUTE NAME, the browser answers
/// <c>InvalidCharacterError: Failed to execute 'setAttribute'</c>, the render dies, and the desk shows
/// "An unhandled error has occurred. Reload." and nothing else. Every unit test in the repo was green
/// throughout, because <b>no test had ever RENDERED the desk</b>. The owner's own bug-finding method — "open
/// EVERY scene and check all the parts are in the right place" — would have caught it in one pass, and this
/// file is that pass, run by CI on every commit instead of by a person on a good day.
///
/// <para><b>What it does.</b> It boots the SHIPPING <see cref="Map"/> component into a set of world states,
/// and in each state asks the SHIPPING desk switch (<c>SwitchDesk</c> — "the one place a desk switch
/// happens") to sit down at every desk on the top bar, rendering the page through a real Blazor renderer each
/// time. See <see cref="DeskBench"/> for how far off-browser that gets and where the horizon is.</para>
///
/// <para><b>The five things asked of every cell.</b>
/// <list type="number">
/// <item>No exception escapes the render — save the one documented browser gate (see <see cref="DeskBench"/>).</item>
/// <item>The page agrees which desk it is at: exactly one tab in the bar carries <c>btn-info</c>, and it is
/// the desk that was asked for.</item>
/// <item>The desk's ROOT element is on screen — its own class, on an element that is not <c>d-none</c>.</item>
/// <item>The page carries at least one NAMED control — a button/input/select/textarea/link wearing a title or
/// a readable label. A desk that renders an empty box has not rendered.</item>
/// <item>Every attribute name the render tree emits is a name a browser will accept. <b>This is the one that
/// caught the Trade crash</b> — see the red proof below.</item>
/// </list></para>
///
/// <para><b>Where this sits next to the two laws that landed with the fix (#985).</b> Three angles on one
/// class of bug, and none of them is the other:
/// <list type="bullet">
/// <item><see cref="TheRazorCommentIsNotAnAttributeTests"/> reads what was TYPED — no <c>@* … *@</c> between a
/// start tag's angle brackets, in any client <c>.razor</c>. The cheap one; catches the next typist before the
/// code ever runs.</item>
/// <item><c>SpaceSails.UiGate.TheTradeDeskRendersTests</c> boots the PUBLISHED artifact in a real Chromium at
/// one berth and proves the Trade desk paints. The expensive one that cannot be fooled — but it is one desk in
/// one state, because a real browser per state costs minutes.</item>
/// <item><b>This file</b> reads what the compiled component EMITS, at EVERY desk in EVERY world, for the price
/// of a unit test. It covers a bad attribute name no comment produced — a splatted <c>@attributes</c>
/// dictionary with a bad key, a name built from data — and it covers the other four ways a desk can fail to
/// stand up, which have nothing to do with attribute names at all.</item>
/// </list>
/// The three overlap on exactly one cell (Trade, docked) and agree there. If a fourth desk law is ever wanted,
/// the harness to share is <see cref="DeskBench"/>, not any of the three tests.</para>
///
/// <para><b>Red proof.</b> Run on the base commit BEFORE #985 landed, this file failed with:
/// <c>2 of 40 desk x world cells did not stand up (8 desks x 5 worlds)</c> — "docked at Selene Gate · Trade"
/// and "docked at the Red Eye · Trade", both naming the attribute the Razor comment had become. Two, and not
/// forty: the sweep is sensitive to the real thing and not to everything.</para>
///
/// <para><b>The matrix is data-driven</b> (<see cref="TheDesks"/> × <see cref="TheWorlds"/>) so a new desk is
/// ONE row — and <see cref="ATenthDeskCannotSkipTheLaw"/> compares that table against the tab bar the
/// component itself renders, so a tenth desk cannot be added without joining the law.</para>
///
/// <para><b>The dev-start half</b> is <see cref="EveryDevStartRendersTheDeskItLandsOn"/>, and it deliberately
/// does NOT duplicate <see cref="TheBootBuildsTheSameWorldTests"/>: that sweep already boots every URL the
/// game's own front door offers and pins the world each one builds, so "the boot completes with no unhandled
/// exception" is covered and re-asserting it would be noise. What was never asked is whether the PAGE those
/// boots hand you RENDERS — so this sweep reuses that file's own
/// <see cref="TheBootBuildsTheSameWorldTests.EveryBootUrl"/> enumeration and adds the render half.</para>
/// </summary>
public sealed class EveryDeskBootsTests
{
    // ── The matrix: one row per desk, one row per world ──────────────────────────────────────────────

    /// <summary>A desk, the class its ROOT element wears, and what that root is. A new desk needs exactly
    /// this one row.</summary>
    /// <param name="OwnsNoDomRoot">The Deck desk is a CANVAS. It owns no DOM root of its own — the deck is
    /// PAINTED on the same <c>&lt;canvas class="map-canvas"&gt;</c> the solar map uses, and switching to it
    /// flips <c>_deckMode</c> rather than raising a panel. That is a fact about the game, not a hole in this
    /// law, so the row says so out loud, <see cref="ARootThatIsAlwaysThereProvesNothing"/> excuses exactly
    /// this row, and the cell is checked on <c>_deckMode</c> instead — which is the thing the Deck desk IS.</param>
    private sealed record Desk(ShipDesk Which, string RootClass, string WhatTheRootIs, bool OwnsNoDomRoot = false);

    private static readonly Desk[] TheDesks =
    [
        new(ShipDesk.Captain,  "captain-desk",       "the captain's chart-room panel"),
        new(ShipDesk.Nav,      "map-hud",            "the helm's warp / follow / plot toolbar over the solar map"),
        new(ShipDesk.Sensors,  "tracking-post-desk", "the tracking post, full-screen"),
        new(ShipDesk.WarRoom,  "war-room-desk-grid", "the war room's grid over the dimmed map"),
        new(ShipDesk.Trade,    "desk-trade-grid",    "the trading floor, master-detail"),
        new(ShipDesk.Comms,    "desk-comms-room",    "the comms room"),
        new(ShipDesk.Galley,   "galley-desk",        "the galley"),
        new(ShipDesk.Deck,     "map-canvas",         "the canvas the deck is painted on", OwnsNoDomRoot: true),
    ];

    /// <summary>The worlds. Every URL here is a SHIPPED dev start or a shipped cheat combination, and every
    /// one is checked to actually REACH the state it claims by
    /// <see cref="EveryWorldInTheMatrixIsTheWorldItClaims"/> — a "docked at the Red Eye" row that quietly
    /// ended at the front door would run all eight desk checks against a start picker and pass, which is this
    /// repo's fifth named bug class (a guard handed the wrong world).</summary>
    private sealed record World(string Name, string Url, bool Docked, bool OnSurface);

    private static readonly World[] TheWorlds =
    [
        new("free-flying in Sol, alongside the derelict", "/map?start=wreck",                       Docked: false, OnSurface: false),
        new("docked at Selene Gate",              "/map?dock=selene-gate&body=luna&site=1",         Docked: true,  OnSurface: false),
        new("docked at the Red Eye",              "/map?dock=red-eye&body=ganymede&site=1",         Docked: true,  OnSurface: false),
        new("on the ground at Miranda",           "/map?dock=the-tilt&site=0&land=1",               Docked: true,  OnSurface: true),
        new("on the KAAMOS ice moon, Hive floor 23", "/map?kaamos=hq&arrivalphase=2&land=1&floor=23", Docked: false, OnSurface: true),
    ];

    /// <summary>The matrix's world states, for a law that wants the same five worlds without keeping a copy of
    /// them (#953's lane archive sweeps them with a pen). One table, so a sixth world joins both at once.</summary>
    internal static IEnumerable<string> EveryWorldUrl() => TheWorlds.Select(w => w.Url);

    // ── The law ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE SWEEP. Every desk, in every world, rendered — and every failure collected rather than
    /// thrown at the first one, so a red run names every desk that fell over, not the unluckiest.</summary>
    [Fact]
    public async Task EveryDeskStandsUpInEveryWorld()
    {
        var wrong = new List<string>();
        int cells = 0;

        foreach (World world in TheWorlds)
        {
            using DeskBench bench = await DeskBench.BootAsync(world.Url);

            foreach (Desk desk in TheDesks)
            {
                cells++;
                await bench.SwitchAsync(desk.Which);
                DeskBench.Painted painted;
                try
                {
                    painted = await bench.RenderAsync();
                }
                catch (Exception ex)
                {
                    wrong.Add($"{world.Name} · {desk.Which}: the RENDER threw {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                foreach (string complaint in WhatIsWrongWith(painted, bench, world, desk))
                {
                    wrong.Add($"{world.Name} · {desk.Which}: {complaint}");
                }
            }

            foreach (Exception escaped in bench.EscapedPastTheGate)
            {
                wrong.Add($"{world.Name}: an exception escaped the page — {escaped.GetType().Name}: "
                          + escaped.Message.Split('\n')[0]);
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {cells} desk x world cells did not stand up "
            + $"({TheDesks.Length} desks x {TheWorlds.Length} worlds):\n  - " + string.Join("\n  - ", wrong));
    }

    private static IEnumerable<string> WhatIsWrongWith(
        DeskBench.Painted painted, DeskBench bench, World world, Desk desk)
    {
        // (5) The attribute names. First, because it is the one the Trade desk needed.
        foreach (string complaint in EveryAttributeNameIsOneABrowserTakes(painted))
        {
            yield return complaint;
        }

        if (world.OnSurface)
        {
            // #585 · THE SHIP'S DESKS ARE ON THE SHIP. On an excursion SwitchDesk refuses everything but the
            // Deck, and #330 hides the tab bar outright. So the cell's law here is the refusal itself — and
            // that the page still stands up while refusing.
            if (bench.ActiveDesk != ShipDesk.Deck)
            {
                yield return "a surface excursion let the captain sit down at a console that is aboard the "
                           + $"ship (#585) — active desk is {bench.ActiveDesk}";
            }

            if (painted.ClassLists.Any(list => Has(list, "desk-tab-bar")))
            {
                yield return "the desk tab bar is up on a surface excursion (#330 hides it — the desks are a "
                           + "tube ride away)";
            }

            if (!Visible(painted, "map-canvas"))
            {
                yield return "the excursion rendered no canvas to stand on";
            }

            if (painted.NamedControls.Count == 0)
            {
                yield return "the excursion carries no named control at all";
            }

            yield break;
        }

        // (2) The page agrees which desk it is at.
        string[] litTabs = [.. painted.LitDeskTabs];
        if (litTabs.Length != 1)
        {
            yield return litTabs.Length == 0
                ? "no tab in the desk bar is lit — the bar cannot say which desk is up"
                : $"{litTabs.Length} tabs are lit at once ({string.Join(", ", litTabs)})";
        }
        else if (!litTabs[0].Contains(DeskWord(desk.Which), StringComparison.OrdinalIgnoreCase))
        {
            yield return $"asked for {desk.Which} and the bar lit {Quote(litTabs[0])}";
        }

        if (bench.ActiveDesk != desk.Which)
        {
            yield return $"SwitchDesk refused: the page is at {bench.ActiveDesk}. {bench.Pulse}";
        }

        // (3) The root is on screen.
        if (!Visible(painted, desk.RootClass))
        {
            yield return $"nothing on screen wears .{desk.RootClass} ({desk.WhatTheRootIs}) — the desk did "
                       + "not render, or rendered inside a d-none";
        }

        // (4) …and the page carries a NAMED control. An empty box is not a rendered desk.
        if (painted.NamedControls.Count == 0)
        {
            yield return "the page carries no named control at all (no titled or labelled button, input, "
                       + "select, textarea or link)";
        }

        // The Deck desk owns no DOM root, so the thing it IS gets checked instead.
        if (desk.OwnsNoDomRoot && !bench.DeckMode)
        {
            yield return "the Deck desk is up and _deckMode is false — the canvas is still painting the "
                       + "solar map, not the deck";
        }
    }

    private static IEnumerable<string> EveryAttributeNameIsOneABrowserTakes(DeskBench.Painted painted)
    {
        foreach (string name in painted.Attributes.Distinct(StringComparer.Ordinal))
        {
            if (!ANameTheBrowserWillAccept(name))
            {
                yield return $"the render tree emits an attribute named {Quote(name)} — setAttribute answers "
                           + "InvalidCharacterError on that, which kills the render and shows "
                           + "\"An unhandled error has occurred\" instead of the desk";
            }
        }
    }

    // ── The completeness guards ──────────────────────────────────────────────────────────────────────

    /// <summary>A TENTH DESK CANNOT SKIP THE LAW. The desk table above is compared against the tab bar the
    /// COMPONENT ITSELF renders — not a copy of it, and not the enum either (which carries its own trap:
    /// Captain is <c>8</c> in the enum and <c>0</c> on the bar). A desk added to <c>TabBarOrder</c> and not to
    /// <see cref="TheDesks"/> reddens here.</summary>
    [Fact]
    public async Task ATenthDeskCannotSkipTheLaw()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?start=wreck");
        DeskBench.Painted painted = await bench.RenderAsync();

        // What the component's own ordering field says…
        Assert.Equal(DeskBench.TabBarOrder, TheDesks.Select(d => d.Which));

        // …and what it actually PAINTED. The bar carries one button per desk plus the Peek toggle, which is
        // not a desk and has no row: it is named here so that a NINTH button appearing on the bar has to be
        // explained to this test rather than slipping in behind "well, Peek isn't a desk either".
        string[] tabs = [.. painted.DeskTabLabels];
        Assert.Equal(TheDesks.Length + 1, tabs.Length);
        Assert.Contains(tabs, t => t.Contains("Peek", StringComparison.Ordinal));
        foreach (Desk desk in TheDesks)
        {
            Assert.Contains(tabs, t => t.Contains(DeskWord(desk.Which), StringComparison.OrdinalIgnoreCase));
        }

        // And every value of the enum is on the bar — a desk declared and never given a tab is a desk nobody
        // can reach, which is its own bug.
        Assert.Equal(
            Enum.GetValues<ShipDesk>().OrderBy(d => d.ToString(), StringComparer.Ordinal),
            TheDesks.Select(d => d.Which).OrderBy(d => d.ToString(), StringComparer.Ordinal));
    }

    /// <summary>A ROOT THAT IS ALWAYS THERE PROVES NOTHING — this repo's fifth named bug class, applied to
    /// the table above. Each desk's root class must be ABSENT (or d-none'd) while a different desk is up;
    /// otherwise check (3) is green on every desk including the broken one.</summary>
    [Fact]
    public async Task ARootThatIsAlwaysThereProvesNothing()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-space-bar");
        var alwaysUp = new List<string>();

        foreach (Desk desk in TheDesks.Where(d => !d.OwnsNoDomRoot))
        {
            ShipDesk elsewhere = desk.Which == ShipDesk.Nav ? ShipDesk.Galley : ShipDesk.Nav;
            await bench.SwitchAsync(elsewhere);
            DeskBench.Painted painted = await bench.RenderAsync();
            if (Visible(painted, desk.RootClass))
            {
                alwaysUp.Add($".{desk.RootClass} ({desk.Which}) is on screen while the {elsewhere} desk is up "
                           + "— it cannot tell a rendered desk from an absent one");
            }
        }

        Assert.True(alwaysUp.Count == 0, string.Join("\n  ", alwaysUp));
    }

    /// <summary>THE WORLDS ARE THE WORLDS THEY CLAIM.</summary>
    [Fact]
    public async Task EveryWorldInTheMatrixIsTheWorldItClaims()
    {
        var wrong = new List<string>();
        foreach (World world in TheWorlds)
        {
            using DeskBench bench = await DeskBench.BootAsync(world.Url);
            if (bench.OnSurface != world.OnSurface)
            {
                wrong.Add($"{world.Name} ({world.Url}): the row says on-surface={world.OnSurface} and _surface is "
                          + (bench.OnSurface ? "set" : $"null. {bench.Pulse}"));
            }
            if (bench.Docked != world.Docked)
            {
                wrong.Add($"{world.Name} ({world.Url}): the row says docked={world.Docked} and _dockedHavenId is "
                          + (bench.Docked ? "set" : "null"));
            }
        }
        Assert.True(wrong.Count == 0, string.Join("\n  ", wrong));

        // …and the matrix must be worth running at all: three genuinely different kinds of world.
        Assert.True(TheWorlds.Length >= 3, "the sweep needs at least three world states");
        Assert.Contains(TheWorlds, w => !w.Docked && !w.OnSurface);
        Assert.Contains(TheWorlds, w => w.Docked && !w.OnSurface);
        Assert.Contains(TheWorlds, w => w.OnSurface);
    }

    // ── The dev-start half ───────────────────────────────────────────────────────────────────────────

    /// <summary>EVERY DEV START RENDERS THE PAGE IT LANDS ON. The saves dialog offers a catalogue of quick
    /// starts; <see cref="TheBootBuildsTheSameWorldTests"/> already boots every one of them and pins the world
    /// it builds, so "the boot completes with no unhandled exception" is covered there and is not re-asserted
    /// here. What was never asked is whether the PAGE those boots hand you renders.</summary>
    [Fact]
    public async Task EveryDevStartRendersTheDeskItLandsOn()
    {
        var wrong = new List<string>();
        int booted = 0;

        foreach (string url in TheBootBuildsTheSameWorldTests.EveryBootUrl())
        {
            booted++;
            using DeskBench bench = await DeskBench.BootAsync(url);
            DeskBench.Painted painted;
            try
            {
                painted = await bench.RenderAsync();
            }
            catch (Exception ex)
            {
                wrong.Add($"{url}: the RENDER threw {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            foreach (string complaint in EveryAttributeNameIsOneABrowserTakes(painted))
            {
                wrong.Add($"{url}: {complaint}");
            }

            if (painted.NamedControls.Count == 0)
            {
                wrong.Add($"{url}: the page rendered no named control at all");
            }

            foreach (Exception escaped in bench.EscapedPastTheGate)
            {
                wrong.Add($"{url}: an exception escaped the page — {escaped.GetType().Name}: "
                          + escaped.Message.Split('\n')[0]);
            }
        }

        Assert.True(booted >= 60, $"only {booted} boot URLs were rendered — the sweep has shrunk.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} findings across {booted} booted URLs:\n  - " + string.Join("\n  - ", wrong));
    }

    // ── The rules ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>WHAT A BROWSER WILL TAKE AS AN ATTRIBUTE NAME. HTML's own rule: anything except whitespace,
    /// NUL, <c>"</c>, <c>'</c>, <c>&gt;</c>, <c>/</c> and <c>=</c> — and it is precisely this rule that
    /// <c>Element.setAttribute</c> enforces with <c>InvalidCharacterError</c>. Blazor's own generated names
    /// (<c>onclick</c>, <c>__internal_preventDefault_onwheel</c>, the scoped-CSS <c>b-xxxxxxxxxx</c>) all
    /// pass; a Razor comment compiled into an attribute name does not, because it is full of spaces before it
    /// is anything else.</summary>
    internal static bool ANameTheBrowserWillAccept(string name) =>
        name.Length > 0
        && !name.Any(c => char.IsWhiteSpace(c) || c is '\0' or '"' or '\'' or '>' or '/' or '=' || char.IsControl(c));

    /// <summary>On screen: the class token appears on an element whose class list does NOT also carry
    /// <c>d-none</c>. Both halves matter — several desks render their panel always and hide it with
    /// <c>d-none</c>, so a plain substring search would answer "yes" for a desk nobody can see.</summary>
    private static bool Visible(DeskBench.Painted painted, string classToken) =>
        painted.ClassLists.Any(list => Has(list, classToken) && !Has(list, "d-none"));

    private static bool Has(string classList, string token) =>
        classList.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(token, StringComparer.Ordinal);

    /// <summary>The word the tab bar prints for a desk — the enum's own spelling ("WarRoom") is not what a
    /// player reads.</summary>
    private static string DeskWord(ShipDesk desk) => desk switch
    {
        ShipDesk.WarRoom => "War room",
        _ => desk.ToString(),
    };

    private static string Quote(string text)
    {
        string flat = text.Replace("\r", "").Replace("\n", "/");
        return "\"" + (flat.Length > 140 ? flat[..140] + "…" : flat) + "\"";
    }
}
