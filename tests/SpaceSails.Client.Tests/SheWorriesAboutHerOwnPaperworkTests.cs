using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #426 · SHE WORRIES ABOUT HER OWN PAPERWORK — driven, on her own deck, with the shipping shudder.
///
/// <para><b>Owner, 2026-07-20, sailing through a storm:</b> <i>"The word 'storm' was spoken... makes one
/// think the ship's long chain of owners and maintenance organizations and insurers.. hope it all was
/// checked through the whole chain. 😅"</i></para>
///
/// <h3>Why this is driven and not read</h3>
///
/// <para>The Core sweep next door (<c>HerChainOfOwnersTests</c>) proves the three sentences compose off a
/// record. It cannot prove the shudder ever reaches them, and it cannot prove the record they reach is
/// HERS — which is the half that was missing for two years: <c>ShipHistories.For</c> had three call sites
/// and all three were other people's hulls (#938's audit row). So these press <c>FireShudder</c> — the
/// shipping method, on a shipping <see cref="Pages.Map"/>, standing on her own deck — and read the pulse
/// line and its rank, which is what the player actually gets.</para>
///
/// <para>The bench is <c>TheMoodAsksTheGroundTests</c>' (#867), with the excursion left null: that IS her
/// own deck.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class SheWorriesAboutHerOwnPaperworkTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>A stable seed for the deck's schedule, written down so the windows below are reproducible
    /// by hand.</summary>
    private const ulong Seed = 0xC0FFEE_1234UL;

    /// <summary>A haven with a walkable interior — the concourse whose settling is NOT her paperwork.</summary>
    private const string AHavenWithABar = "the-space-bar";

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component walking an interior deck. With no excursion and no docked haven that is
    /// HER OWN DECK (<see cref="HullShudder.Setting.Ship"/>); hand a haven id and it is a station
    /// concourse instead.</summary>
    private static Pages.Map OnDeck(string? dockedHaven = null)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        Set(map, "_deckMode", true);
        Set(map, "_surface", null);
        Set(map, "_dockedHavenId", dockedHaven);
        Set(map, "_shudderSeed", Seed);
        Set(map, "_nerve", NervePips.FromPips(NervePips.MaxPips));
        return map;
    }

    private static void Set(object target, string field, object? value) =>
        (typeof(Pages.Map).GetField(field, Hidden)
         ?? throw new InvalidOperationException($"Map has no field {field}."))
        .SetValue(target, value);

    private static T Get<T>(object target, string field) =>
        (T)(typeof(Pages.Map).GetField(field, Hidden)
            ?? throw new InvalidOperationException($"Map has no field {field}."))
        .GetValue(target)!;

    private static void Fire(Pages.Map map) =>
        (typeof(Pages.Map).GetMethod("FireShudder", Hidden)
         ?? throw new InvalidOperationException("Map has no FireShudder — the shudder has moved."))
        .Invoke(map, [0.0]);

    /// <summary>Open a storm window at <paramref name="index"/> and report the pulse the opening tremor
    /// wrote — the line the captain reads, and what the slot thinks it is.</summary>
    private static PulseSlot OpeningTremor(int index, string? dockedHaven = null)
    {
        Pages.Map map = OnDeck(dockedHaven);
        Set(map, "_shudderIndex", index);
        Set(map, "_cautionRun", 0); // no run yet: the next tremor OPENS the window
        Fire(map);
        return Get<PulseSlot>(map, "_pulse");
    }

    /// <summary>Every sentence her own record can honestly compose, over a wide spread of windows. Three
    /// at most, and for her (never renamed, two owners deep, her plate's yard) exactly two.</summary>
    private static HashSet<string> HerLines { get; } =
        [.. Enumerable.Range(0, 500)
            .Select(i => ChainOfCustody.Line(ShipHistories.Hers, DiceRule.Seed(0xABCDEFUL, $"w:{i}")))
            .OfType<string>()];

    /// <summary>Every ordinary hull-shudder pool line for her deck — what the tremor says when it is only
    /// weather.</summary>
    private static HashSet<string> ThePoolForHerDeck { get; } =
        [.. Enumerable.Range(0, 500)
            .Select(i => HullShudder.Line(HullShudder.Setting.Ship, Seed, i))];

    private static bool IsChainOfCustody(string? pulse) =>
        pulse is not null && HerLines.Any(l => pulse.Contains(l, StringComparison.Ordinal));

    // ── The window speaks, once, in her voice ─────────────────────────────────────────────────────────

    [Fact]
    public void TheTremorThatOpensAStormWindow_SpeaksHerChainOfCustody()
    {
        // Not "some window somewhere": EVERY opening tremor on her own deck carries the worry.
        foreach (int index in Enumerable.Range(0, 40))
        {
            PulseSlot pulse = OpeningTremor(index);
            Assert.True(
                IsChainOfCustody(pulse.Message),
                $"the opening tremor of window {index} said '{pulse.Message}', which is not one of her "
                + "chain-of-custody lines");
        }
    }

    [Fact]
    public void TheLineIsComposedFromHERRecord_NotSomeOtherHulls()
    {
        // The bug this guard exists for: a line composed off a hull that is not the captain's would still
        // BE one of the three sentences. So the yard and the year on screen have to be HER plate's, and no
        // other house yard may appear on her deck.
        var seen = Enumerable.Range(0, 40).Select(i => OpeningTremor(i).Message!).ToList();

        string herYardLine = seen.First(m => m.Contains(ShipHistories.KoskiAndDaughters, StringComparison.Ordinal));
        Assert.Contains(
            Core.Interior.Plaques.ShipLaidDownYear.ToString(System.Globalization.CultureInfo.InvariantCulture),
            herYardLine,
            StringComparison.Ordinal);

        // Nobody else's yard, and nobody else's glory name, is ever spoken on her deck.
        ShipHistory anotherHull = ShipHistories.For("npc-0");
        foreach (string line in seen)
        {
            Assert.DoesNotContain(TheOldShip.FormerName, line, StringComparison.Ordinal);
            if (!string.Equals(anotherHull.Yard, ShipHistories.KoskiAndDaughters, StringComparison.Ordinal))
            {
                Assert.DoesNotContain(anotherHull.Yard, line, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void OncePerWindow_TheRestOfTheStormIsOnlyWeather()
    {
        // The second, third and fourth tremors of the same rough patch speak the ordinary pool again. One
        // line per storm window (Fable's law), and the pool is not displaced for the rest of it.
        Pages.Map map = OnDeck();
        Set(map, "_shudderIndex", 3);
        Set(map, "_cautionRun", 0);

        Fire(map);
        Assert.True(IsChainOfCustody(Get<PulseSlot>(map, "_pulse").Message), "the opening tremor must worry");

        foreach (int _ in Enumerable.Range(0, 3))
        {
            Fire(map);
            string? later = Get<PulseSlot>(map, "_pulse").Message;
            Assert.False(IsChainOfCustody(later), $"a later tremor in the same window worried again: '{later}'");
            Assert.Contains(ThePoolForHerDeck, pool => later!.Contains(pool, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ANewWindow_MayWorryAgain()
    {
        // A rough patch that lapses (or is announced by the caution PA) resets the run, and the next
        // tremor opens a fresh window — otherwise the dread would fire once per session and never again.
        Pages.Map map = OnDeck();
        Set(map, "_shudderIndex", 11);
        Set(map, "_cautionRun", 0);
        Fire(map);
        Assert.True(IsChainOfCustody(Get<PulseSlot>(map, "_pulse").Message));

        Set(map, "_cautionRun", 0); // the storm blew over; the next run is a new window
        Fire(map);
        Assert.True(IsChainOfCustody(Get<PulseSlot>(map, "_pulse").Message));
    }

    // ── Never outside a window, and never off her own hull ────────────────────────────────────────────

    [Fact]
    public void AHavensConcourse_IsNotHerPaperwork()
    {
        // A station settling on its clamps is somebody else's hull. Every tremor there is the haven pool.
        foreach (int index in Enumerable.Range(0, 20))
        {
            PulseSlot pulse = OpeningTremor(index, AHavenWithABar);
            Assert.False(
                IsChainOfCustody(pulse.Message),
                $"a haven tremor spoke her chain of custody: '{pulse.Message}'");
        }
    }

    [Fact]
    public void NothingIsSaidWithoutAWindowBeingOpened()
    {
        // The gate is the run, not the deck: with a rough patch already under way, no tremor may worry.
        Pages.Map map = OnDeck();
        foreach (int index in Enumerable.Range(0, 20))
        {
            Set(map, "_shudderIndex", index);
            Set(map, "_cautionRun", 2); // mid-storm, the window long since opened and spoken for
            Fire(map);
            Assert.False(
                IsChainOfCustody(Get<PulseSlot>(map, "_pulse").Message),
                "a tremor inside an already-open window worried");
        }
    }

    // ── #761's ratchet is untouched ───────────────────────────────────────────────────────────────────

    [Fact]
    public void TheWorryIsWeatherWithAMemory_NeverPlotSignificant()
    {
        // It changes nothing the captain knows, owes or can do — and it must never be dressed up to win
        // the slot off a real climax (#693/#761).
        foreach (int index in Enumerable.Range(0, 40))
        {
            PulseSlot pulse = OpeningTremor(index);
            Assert.Equal(PulseRank.Status, pulse.Rank);
            Assert.False(pulse.Rank.IsPlotSignificant());
        }
    }

    [Fact]
    public void TheWorryNeverCostsANervePip()
    {
        // Zero mechanics: nothing rolls, nothing breaks, and the dread is not priced. The chill is a deep
        // site's job and her own deck has never had one.
        Pages.Map map = OnDeck();
        Set(map, "_shudderIndex", 0);
        Set(map, "_cautionRun", 0);
        Fire(map);

        Assert.Empty(Get<IReadOnlyList<NervePips.Event>>(map, "_nerveLedger"));
        Assert.Equal(NervePips.FromPips(NervePips.MaxPips), Get<double>(map, "_nerve"));
    }
}
