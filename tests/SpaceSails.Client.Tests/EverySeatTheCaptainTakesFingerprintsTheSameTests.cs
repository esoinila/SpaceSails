using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6d · THE SNAPSHOT, TAKEN ON THE OLD CODE.
///
/// <para>There are SIX places in this client where a sitting is opened — <c>Table = new TableTalk { … }</c>
/// in <c>Seating.Bench.cs</c> (×1), <c>Seating.OfficeChair.cs</c> (×3) and <c>Seating.Table.cs</c> (×2) —
/// and each of them ends with the same three lines: the seat goes into the field, the reveal cue plays, the
/// page is asked to draw. Lane 6d makes that tail ONE method. Nothing about that can be proved by counting
/// members: the whole content of a sit site is WHICH properties it sets and TO WHAT, so the guard is a
/// fingerprint — a real <see cref="Pages.Map"/> on a real generated Hive floor, the seat taken with the
/// shipping verb, and everything the sitting then IS written down, sha256'd per case and PINNED.</para>
///
/// <para><b>It went in as its own commit before a line of the six sites moved</b>, so there is no chance of
/// pinning what the new code happens to do. Every digest below was measured against the untouched six.</para>
///
/// <h3>What is written down</h3>
///
/// <para>Per case: the world (body, floor, watch, whether there is a bought pour in front of you), the
/// console the press landed on and its coordinates, where the captain was standing, what the verb returned,
/// where the captain ended up (#820's snap — the one effect a sit site has that is not the record itself),
/// EVERY property of the resulting <c>TableTalk</c> read off the running object by reflection (so a property
/// some other lane adds is in the hash the day it is added), every read on the seat object that is a
/// question about the sitting, the pulse slot, and whether any card came up. Numbers are rounded to six
/// decimals for the same reason lane 7d's transcript rounds them: a platform's libm last bit must not redden
/// a guard about a code move.</para>
///
/// <h3>The cases, and how they are found</h3>
///
/// <para>Not hand-placed coordinates: the floors are the generator's own, and every console of a seat kind
/// is pressed in the plan's own order until each SITE has been entered. A case is named by what the captain
/// sat on, and the world that produced it is in the transcript, so a floor that changed shape reddens
/// loudly rather than quietly pinning a different chair.</para>
///
/// <h3>What this guard cannot see, and says so</h3>
///
/// <para><c>RendererInterop.PlayCue</c> is a <c>[JSImport]</c> guarded by <c>OperatingSystem.IsBrowser()</c>:
/// off a browser it compiles to nothing and no test in this repository can observe a cue. So the cue and the
/// <c>StateHasChanged</c> beside it are not in the fingerprint — they are held by
/// <c>ThereIsOnePlaceASittingIsOpened</c>, which reads the sites themselves.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class EverySeatTheCaptainTakesFingerprintsTheSameTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>The watch every case is taken in. Fixed: the plates, the head count and which tops are free
    /// are all functions of it, and a guard that let the shift roll would pin a different room every run.</summary>
    private const long Watch = 3;

    /// <summary>The bodies whose Hive is walked, in this order, until every site has been entered. The same
    /// list <c>TheParkBenchIsAGumshoeMoveTests</c> walks.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    /// <summary>How many floors down from the top pressurised one are searched. Two, because the chamber
    /// stool and the cubicle are not on every floor and the ring is not on most of them.</summary>
    private const int FloorsDown = 2;

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component on a real Hive floor, with nothing running but the deck. The one piece of
    /// theatre is the render handle — see the docblock on <c>MustStandUpBeforeWalkingTests.OnTheFloor</c>,
    /// which this is the same bench as.</summary>
    private static Pages.Map OnTheFloor(string body, int floor, bool pour)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved, and the seat verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, floor);

        // #751's own `?watch=N`, and it is set rather than the excursion's field written directly BECAUSE
        // `RebuildSurfaceDeck` is the one place the shift is ever frozen and it overwrites that field on
        // every rebuild — a watch poked onto the excursion would be silently replaced by the clock's answer
        // the moment the captain sat down, which is the one-source-consumed-in-the-wrong-order trap.
        Set(map, "_watchCheat", (long?)Watch);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_fpMode", false);

        if (pour)
        {
            // #783/#784's own window, set the way the counter sets it: a tot poured, and a clock that has not
            // run since. Nothing here reads a real clock, so the pour simply IS in front of you.
            Set(map, "_rumTots", 1);
            Set(map, "_lastRumMs", 0.0);
            Set(map, "_lastTimestampMs", (double?)0.0);
        }

        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    // ── THE SIX SITES, NAMED BY WHAT THE CAPTAIN SAT ON ───────────────────────────────────────────────

    /// <summary>Which construction site a sitting came out of, read off the sitting itself. Every one of the
    /// six is distinguishable from its own record, which is the point: the classifier asks the same question
    /// the panel asks.</summary>
    private static string SiteOf(object t)
    {
        if ((bool)Prop(t, "Bench")!)
        {
            return (bool)Prop(t, "SharedSeat")! ? "a park bench with somebody on the far end" : "a park bench";
        }
        if (Prop(t, "CubicleKey") is not null)
        {
            return "a cubicle, with the catch still open";
        }
        if ((bool)Prop(t, "Office")!)
        {
            return ((string)Prop(t, "Key")!).Contains(":bench:", StringComparison.Ordinal)
                ? "a stool at a chamber worktop"
                : "a chair in a ring office";
        }
        bool quiet = (bool)Prop(t, "Quiet")!;
        return (CanteenTable.Who)Prop(t, "Who")! == CanteenTable.Who.None
            ? quiet ? "a free top in a cabinet" : "a free top in the hall"
            : quiet ? "a top in a cabinet somebody is already at" : "a top somebody is already at";
    }

    /// <summary>The presses, and the console kind each is made at. Every seat kind the six sites serve is
    /// reached through the verb a captain reaches it through — never by calling the private opener.</summary>
    private static readonly (DeckPlan.ConsoleKind Kind, string Verb)[] Presses =
    [
        (DeckPlan.ConsoleKind.HiveTable, "TryTakeTable"),
        (DeckPlan.ConsoleKind.HiveRegular, "TryOpenTable"),
        (DeckPlan.ConsoleKind.HiveBench, "TryTakeBench"),
        (DeckPlan.ConsoleKind.HiveOfficeChair, "TryTakeOfficeChair"),
    ];

    private sealed record Sat(string Site, string Transcript);

    /// <summary>
    /// Every distinct sitting the six sites can open, found by pressing every seat console on every floor
    /// searched, in the plan's own order, on a FRESH page each time. First one into a site wins and the
    /// search for that site stops — so the case set is a function of the generator and not of a coordinate
    /// somebody typed.
    /// </summary>
    private static IReadOnlyDictionary<string, string> EverySitting(bool pour)
    {
        var found = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } top)
            {
                continue;
            }

            for (int down = 0; down < FloorsDown; down++)
            {
                int floor = top - down;
                Pages.Map probe;
                try
                {
                    probe = OnTheFloor(body, floor, pour);
                }
                catch (Exception)
                {
                    continue;   // not a floor this body has; the search simply moves on
                }

                DeckPlan plan = (DeckPlan)Get(probe, "_deckPlan")!;
                foreach ((DeckPlan.ConsoleKind kind, string verb) in Presses)
                {
                    DeckPlan.ConsoleSpot[] spots = [.. plan.Consoles.Where(c => c.Kind == kind)];
                    for (int i = 0; i < spots.Length; i++)
                    {
                        Pages.Map map = OnTheFloor(body, floor, pour);
                        Set(map, "_avatarX", (double)spots[i].X);
                        Set(map, "_avatarY", (double)spots[i].Y);

                        double standX = (double)Get(map, "_avatarX")!;
                        double standY = (double)Get(map, "_avatarY")!;
                        object? answer = Invoke(map, verb);
                        if (Get(map, "_table") is not { } t)
                        {
                            continue;
                        }

                        string site = SiteOf(t);
                        if (found.ContainsKey(site))
                        {
                            continue;
                        }

                        found[site] = Transcribe(
                            site, body, floor, pour, kind, verb, i, spots[i], standX, standY, answer, map, t);
                    }
                }
            }
        }

        return found;
    }

    // ── WHAT IS WRITTEN DOWN ──────────────────────────────────────────────────────────────────────────

    /// <summary>Read-only questions asked of the seat by name. Properties are swept reflectively; these are
    /// the ones that are METHODS, and calling a verb by accident is what the explicit list prevents.</summary>
    private static readonly string[] SeatReads =
    [
        "SeatedCompanyLine", "SeatedCustomerLine", "SeatedOverheardLine",
        "TableMovesOnTheTable", "StoolMovesOnTheTable", "TableShowables", "CounterHasStools",
    ];

    private static string Transcribe(
        string site, string body, int floor, bool pour, DeckPlan.ConsoleKind kind, string verb,
        int spotIndex, DeckPlan.ConsoleSpot spot, double standX, double standY, object? answer,
        Pages.Map map, object t)
    {
        var w = new StringBuilder();
        w.Append("case ").Append(site).Append('\n');
        // The watch is read back off the excursion rather than repeated from the constant, because the deck
        // rebuild is what freezes it — so the transcript records the shift the room was actually drawn on.
        object surface = Get(map, "_surface")!;
        w.Append("  world     ").Append(body).Append(" floor ").Append(floor.ToString(Inv))
            .Append(" watch ").Append(Show(Prop(surface, "CanteenWatch")))
            .Append(" pour ").Append(pour).Append('\n');
        w.Append("  console   ").Append(kind).Append(" #").Append(spotIndex.ToString(Inv))
            .Append(" at ").Append(R(spot.X)).Append(',').Append(R(spot.Y)).Append('\n');
        w.Append("  standing  ").Append(R(standX)).Append(',').Append(R(standY)).Append('\n');
        w.Append("  press     ").Append(verb).Append(" -> ").Append(answer ?? "-").Append('\n');
        // #820's snap is the one effect a sit site has that is not the record, so it is written down beside it.
        w.Append("  seated    ").Append(R((double)Get(map, "_avatarX")!))
            .Append(',').Append(R((double)Get(map, "_avatarY")!)).Append('\n');

        w.Append("  table\n");
        foreach (PropertyInfo p in t.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            w.Append("    ").Append(p.Name).Append(" = ").Append(Show(p.GetValue(t))).Append('\n');
        }

        object seat = SeatState.Seat(map)!;
        w.Append("  seat\n");
        foreach (PropertyInfo p in seat.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.GetIndexParameters().Length == 0
                && !string.Equals(p.Name, "Table", StringComparison.Ordinal))
            .OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            w.Append("    ").Append(p.Name).Append(" = ").Append(Show(p.GetValue(seat))).Append('\n');
        }

        foreach (string read in SeatReads)
        {
            MethodInfo m = seat.GetType().GetMethod(read, Hidden)
                ?? throw new InvalidOperationException(
                    $"#870 lane 6d · the seat has no `{read}` — this snapshot is reading a dead name. "
                    + "Re-spell it here in the same commit; never drop the row, which would quietly shrink "
                    + "what the fingerprint covers.");
            w.Append("    ").Append(read).Append("() = ").Append(Show(m.Invoke(seat, null))).Append('\n');
        }

        w.Append("  pulse     ").Append(Show(Get(map, "_pulse"))).Append('\n');
        w.Append("  cards     view ").Append(Show(Get(map, "_viewObject")))
            .Append(" story ").Append(Show(Get(map, "_storyCard"))).Append('\n');

        return Normalize(w.ToString());
    }

    private static string Show(object? v) => v switch
    {
        null => "null",
        string s => s,
        bool b => b ? "True" : "False",
        IEnumerable<string> many => "[" + string.Join(" | ", many.OrderBy(x => x, StringComparer.Ordinal)) + "]",
        IEnumerable<Encounter.Move> moves => "[" + string.Join(" | ", moves.Select(m => m.Id)) + "]",
        IEnumerable<Core.Satchel.Item> items => "[" + string.Join(" | ", items.Select(i => i.Stored)) + "]",
        ValueTuple<double, double> pair => R(pair.Item1) + "," + R(pair.Item2),
        Encounter.Scene scene => "scene[" + scene.Id + " | " + scene.Opening + "]",
        IFormattable f => f.ToString(null, Inv),
        IEnumerable rest => "[" + string.Join(" | ", rest.Cast<object?>().Select(Show)) + "]",
        _ => v.ToString() ?? "-",
    };

    private static string R(double v) =>
        Math.Round(v, 6, MidpointRounding.AwayFromZero).ToString("0.######", Inv);

    private static string R(float v) => R((double)v);

    /// <summary>The one normalisation, and the reason is lane 7d's: six decimals, so a platform's libm last
    /// bit cannot redden a guard about a code move.</summary>
    private static readonly Regex Numbers = new(
        @"-?\d+\.\d+(?:[eE][-+]?\d+)?|-?\d+[eE][-+]?\d+", RegexOptions.Compiled);

    private static string Normalize(string raw) => Numbers.Replace(raw, m =>
    {
        double v = double.Parse(m.Value, NumberStyles.Float, Inv);
        double r = Math.Round(v, 6, MidpointRounding.AwayFromZero);
        return (r == 0 ? 0 : r).ToString("0.######", Inv);
    });

    private static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    // ── THE PINS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What each sitting hashed to on the SIX untouched <c>Table = new TableTalk { … }</c> sites of
    /// <c>c906855</c>, before a line of them moved. A change here is either a behaviour change at a seat or a
    /// change to the floor the seat is on; both are things a human should have to look at.</summary>
    private static readonly Dictionary<string, string> Pinned = new(StringComparer.Ordinal)
    {
        ["a chair in a ring office"] = "dd01762b91cd650326758a40cc8c8892dc3ba1015bca86756d004fd525529762",
        ["a cubicle, with the catch still open"] = "49a33d689cf200ac8d335301f1bf89893a911caa821914f8c388059b57610ed1",
        ["a free top in a cabinet"] = "1ca62fd30d3dcfba6f4765f2873c2aa4043bcfdc66aa2e959843fce5ae1cdc66",
        ["a free top in the hall"] = "495bf1bddc1daa583846d83ff31a1540be251cbf45e690253cb6237dfca44772",
        ["a park bench"] = "d2736e6da8f4b5d53fc30636155a84c174ccafdbb842b4c87f50a9c28ac24c5b",
        ["a park bench with somebody on the far end"] = "1f558306e53e74d0231733102f6678b19af296fe9b2dabb2e17e4a9d09428098",
        ["a stool at a chamber worktop"] = "afffec58d73d8f0cd815813c1763e49b2933493cf825e90a23e4109a3b3ef698",
        ["a top somebody is already at"] = "d5393719eb4ccb1550c737d64e9157e3ee9cd9ba7aa951bde9f1c2089601901c",
        ["a chair in a ring office (with a pour in front of you)"] = "79e0f93bc53418b6fc1e8d32a9c791b344904b73339dc78e101fcf6561361821",
        ["a cubicle, with the catch still open (with a pour in front of you)"] = "a0f7380aa73123e08a3ae8f57aefac92d31e784d08510d242b93aa30b1cf3c67",
        ["a free top in a cabinet (with a pour in front of you)"] = "cf32c871b67a7ecd85b4130bc4c853268da0c6f0084ba243c7c8b7d7d080b5b0",
        ["a free top in the hall (with a pour in front of you)"] = "7ae26a8070f04511ec1365180dca794b3186e294fee391bb1c31cd7b96cb0b70",
        ["a park bench (with a pour in front of you)"] = "2aa96a73ef6247ae15b1d45744354a6876c11a19a3dd328726b3364fd568d12e",
        ["a park bench with somebody on the far end (with a pour in front of you)"] = "cb60539c56f91049d85a59f437f95a79076953b2c08e763f2b08b0cdc8d5bfa6",
        ["a stool at a chamber worktop (with a pour in front of you)"] = "8c604ca085d688f57637448c9389ae1949c2156a4887104a50e39c07a08df209",
        ["a top somebody is already at (with a pour in front of you)"] = "1136622309338c079480744f2e72ec69a251e162d9bf99d2d195ffdae46278ba",
    };

    /// <summary>
    /// WHICH OF THE SIX CONSTRUCTION SITES EACH SITTING CAME OUT OF — the anti-vacuous half.
    ///
    /// <para>Sixteen pinned hashes prove nothing about a site no case entered. So the map below is asserted
    /// TOTAL in both directions: every sitting the search finds is attributed to one of the six, and every
    /// one of the six is entered by at least one sitting. A seventh way to open a sitting, or a site that
    /// stops being reachable, reddens here by name rather than passing quietly.</para>
    /// </summary>
    private static readonly Dictionary<string, string> WhichSite = new(StringComparer.Ordinal)
    {
        ["a park bench"] = "Seating.Bench.cs · SitOnThisBench",
        ["a park bench with somebody on the far end"] = "Seating.Bench.cs · SitOnThisBench",
        ["a cubicle, with the catch still open"] = "Seating.OfficeChair.cs · SitInThisCubicle",
        ["a stool at a chamber worktop"] = "Seating.OfficeChair.cs · SitOnThisStool",
        ["a chair in a ring office"] = "Seating.OfficeChair.cs · SitInThisChair",
        ["a top somebody is already at"] = "Seating.Table.cs · TryOpenTable",
        ["a top in a cabinet somebody is already at"] = "Seating.Table.cs · TryOpenTable",
        ["a free top in the hall"] = "Seating.Table.cs · TryTakeTable",
        ["a free top in a cabinet"] = "Seating.Table.cs · TryTakeTable",
    };

    // ── THE FACTS ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>EVERY SEAT THE CAPTAIN TAKES FINGERPRINTS THE SAME.</summary>
    [Fact]
    public void EverySeatHashesToWhatItHashedToOnTheOldCode()
    {
        var wrong = new List<string>();
        var seen = new List<string>();

        foreach (bool pour in new[] { false, true })
        {
            foreach ((string site, string text) in EverySitting(pour).OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                string name = pour ? site + " (with a pour in front of you)" : site;
                seen.Add(name);
                string got = Sha256(text);
                if (!Pinned.TryGetValue(name, out string? want)
                    || !string.Equals(want, got, StringComparison.Ordinal))
                {
                    wrong.Add($"  [\"{name}\"] = \"{got}\",   // pinned {want ?? "(nothing)"}");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} sitting(s) of {seen.Count} do not match what they hashed to on the old code:\n"
            + string.Join("\n", wrong));

        foreach (string name in Pinned.Keys)
        {
            Assert.Contains(name, seen);
        }
    }

    /// <summary>EVERY ONE OF THE SIX SITES IS ENTERED BY SOME CASE, and every case is attributed to one of
    /// them. Without this the pins above would be a snapshot that cannot tell pass from fail at whichever
    /// seat nobody happened to sit on.</summary>
    [Fact]
    public void EveryOneOfTheSixSitesIsEnteredBySomeSitting()
    {
        var entered = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string site in EverySitting(pour: false).Keys)
        {
            Assert.True(WhichSite.TryGetValue(site, out string? where),
                $"#870 lane 6d · a sitting opened that this file cannot attribute to a construction site: "
                + $"`{site}`. Either a seventh way to open a sitting has been added — in which case it needs "
                + "a row here, a pin above, and a line in the PR body — or the classifier has drifted.");
            entered.Add(where!);
        }

        string[] six =
        [
            "Seating.Bench.cs · SitOnThisBench",
            "Seating.OfficeChair.cs · SitInThisCubicle",
            "Seating.OfficeChair.cs · SitOnThisStool",
            "Seating.OfficeChair.cs · SitInThisChair",
            "Seating.Table.cs · TryOpenTable",
            "Seating.Table.cs · TryTakeTable",
        ];
        string[] missed = [.. six.Where(s => !entered.Contains(s))];
        Assert.True(missed.Length == 0,
            "#870 lane 6d · these construction sites were never entered, so the pins prove nothing about "
            + "them:\n  " + string.Join("\n  ", missed)
            + "\n\nEntered:\n  " + string.Join("\n  ", entered));
    }

    /// <summary>…and the same press hashes the same TWICE, from two fresh floors. The clause that catches a
    /// sitting built off an unordered set, a cache with memory in it, or a seat that reads a clock.</summary>
    [Fact]
    public void TheSameSitTakenTwiceIsTheSameSitting()
    {
        IReadOnlyDictionary<string, string> a = EverySitting(pour: false);
        IReadOnlyDictionary<string, string> b = EverySitting(pour: false);
        Assert.Equal(a.Keys.OrderBy(k => k, StringComparer.Ordinal), b.Keys.OrderBy(k => k, StringComparer.Ordinal));
        foreach ((string site, string text) in a)
        {
            Assert.Equal(Sha256(text), Sha256(b[site]));
        }
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static object? Prop(object o, string name) =>
        o.GetType().GetProperty(name, Hidden)!.GetValue(o);

    /// <summary>#870 lane 6b · the five seat fields live on the page's <c>_seating</c> object, so the lookup
    /// follows them there and every read below asks for the state by the name it was written with.</summary>
    private static object? Get(object o, string name)
    {
        if (SeatState.TryFollow(o, name, out object? seated))
        {
            return seated;
        }

        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        Assert.True(prop is not null, $"the component has no `{name}` — this guard is reading a dead name.");
        return prop!.GetValue(o);
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    /// <summary>#870 lane 6c · the lookup follows the verb onto the seat object, exactly as
    /// <c>CoSeatingIsAStripTests</c> does — same live page, same real handler, same arguments.</summary>
    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        object target = map;
        if (call is null && SeatState.Seat(map) is { } seat)
        {
            call = seat.GetType().GetMethod(method, Hidden);
            target = seat;
        }
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(target, args);
    }
}
