using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
/// <para>There are SEVEN places in this client where a sitting is opened — <c>Table = new TableTalk { … }</c>
/// in <c>Seating.Bench.cs</c> (×1), <c>Seating.OfficeChair.cs</c> (×3) and <c>Seating.Table.cs</c> (×3; it
/// was ×2 until #731 v2 gave a contact a booth to lead you into) —
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
    /// <remarks>
    /// #758 · ALL SIXTEEN WERE RE-PINNED, and here is exactly what moved. This snapshot reflects over every
    /// property of <c>TableTalk</c> and of the seat, so the curtain adding state to both changed every row —
    /// including a park bench, which has no cabinet anywhere near it. Five names appeared:
    /// <c>TableTalk.Cabinet</c>, and the seat's <c>ACabinetLeafToWork</c>, <c>CabinetLeafHint</c>,
    /// <c>CabinetLeafLabel</c> and <c>CabinetStage</c>. One sentence changed: the cabinet's room clause, which
    /// said <i>the door is shut</i> about a leaf that now usually stands open.
    ///
    /// <para><b>Proved rather than asserted.</b> Every one of the sixteen transcripts was dumped, the five new
    /// lines struck out of it and the old room clause put back, and the result hashed: all sixteen reproduce
    /// their OLD digest byte for byte. Nothing else about any sitting moved.</para>
    ///
    /// <para>#1016 · <b>ALL SIXTEEN AGAIN, and it is the same STATE-SHAPE kind of change with the same
    /// proof.</b> Owner, on 7 Deck: <i>"Why no table here to sit at?"</i>, <i>"Why no table in cabin
    /// either?"</i>, <i>"I expect to have a bar table like this in this ships galley also.... feature
    /// complete."</i> The ship's two seats have no <c>SurfaceExcursion</c> behind them, so three facts moved
    /// onto the sitting itself — <c>Aboard</c> (whose floor this is, which decides that nobody ever crosses
    /// it and which silence a wait gets), <c>Waits</c> (the beat counter, which is the ROOM's ledger where
    /// there is a room) and <c>Watch</c> (the frozen shift, likewise). Every row here is ashore on a Hive
    /// floor, so all three are at their defaults in all sixteen: <c>Aboard = False</c>, <c>Waits = 0</c>,
    /// <c>Watch = 0</c>.</para>
    ///
    /// <para><b>Proved the same way, and it is the whole of the diff.</b> The property sweep in
    /// <see cref="Transcribe"/> was temporarily filtered to skip those three names and the suite re-run
    /// against the OLD pins: <b>all sixteen reproduce byte for byte, 4/4 green</b>. So the only difference
    /// between the two sides is three added lines at their defaults — no line removed, none changed, and no
    /// seat read, pulse or card moved at all.</para>
    ///
    /// <para><b>#1040 · ALL SIXTEEN AGAIN, ONE FIELD THIS TIME, AND THE SAME PROOF.</b> Owner, on the same
    /// deck: <i>"Our on ship bar can be upgraded to match the other bars... the UI represents code long time
    /// ago."</i> Her cantina grew the counter its own backdrop has always drawn, and a counter has stools —
    /// so <c>TableTalk</c> gained <c>Stool</c>, the one fact that moves a sitting onto the <c>BarStool</c>
    /// rung, where the gumshoe rule refuses the spread out loud. Every row here is ashore on a Hive floor
    /// and not one of them is a counter stool, so it reads its default in all sixteen:
    /// <c>Stool = False</c>.</para>
    ///
    /// <para><b>Proved exactly as the three above were.</b> The property sweep in <see cref="Transcribe"/>
    /// was temporarily filtered to skip <c>Stool</c> and the suite re-run against the OLD pins: <b>all
    /// sixteen reproduce byte for byte, 4/4 green</b>. One added line at its default; nothing removed,
    /// nothing changed, and not one seat read, pulse or card moved.</para>
    ///
    /// <para><b>#1055 · THE SIXTEEN DIGESTS THEMSELVES MOVED OUT OF THIS FILE</b> and into
    /// <c>Ledgers/SeatFingerprints.ledger.txt</c>, two rows per sitting — <c>chars | &lt;sitting&gt;</c> and
    /// <c>sha256 | &lt;sitting&gt;</c> — machine-written by the re-pin command, never transcribed by hand.
    /// The history above stays here, because the arithmetic in it is the reason each digest is what it is.
    /// The <c>chars</c> row is new and is the one thing this ledger adds: every re-pin note above had to
    /// count the transcript's characters by hand to say how wide the change was (<i>"1111 → 1121 chars"</i>),
    /// and now the report says it.</para>
    /// </remarks>
    internal const string Suite = "SeatFingerprints";

    private const string CharsProbe = "chars", ShaProbe = "sha256";

    /// <summary>What the ledger's own header says about where these numbers came from.</summary>
    internal const string Preamble =
        "EVERY DISTINCT SITTING THE SIX CONSTRUCTION SITES CAN OPEN, hashed (#870 lane 6d).\n"
        + "Taken on c906855, the six untouched `Table = new TableTalk { … }` sites, before a line of them\n"
        + "moved. `sha256` is the whole transcript of the sitting — the world, the console, the snap, every\n"
        + "property of the TableTalk read off the running object, every seat read, the pulse and the card.\n"
        + "`chars` is that transcript's length, which is what says how WIDE a change is at a glance.\n"
        + "The re-pin history is in the remarks on EverySeatTheCaptainTakesFingerprintsTheSameTests.";

    /// <summary>Every sitting taken and transcribed, as ledger rows — the measurement the re-pin command
    /// writes down, and the same one the guard below compares against what is written down.</summary>
    internal static IReadOnlyList<PinLedger.Row> MeasureEveryRow()
    {
        var rows = new List<PinLedger.Row>();
        foreach (bool pour in new[] { false, true })
        {
            foreach ((string site, string text) in
                     EverySitting(pour).OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                string name = Named(site, pour);
                rows.Add(new PinLedger.Row(CharsProbe, name, text.Length.ToString(Inv)));
                rows.Add(new PinLedger.Row(ShaProbe, name, Sha256(text)));
            }
        }
        return rows;
    }

    private static string Named(string site, bool pour) =>
        pour ? site + " (with a pour in front of you)" : site;

    /// <summary>
    /// WHICH OF THE SIX CONSTRUCTION SITES EACH SITTING CAME OUT OF — the anti-vacuous half.
    ///
    /// <para>Sixteen pinned hashes prove nothing about a site no case entered. So the map below is asserted
    /// TOTAL in both directions: every sitting the search finds is attributed to one of the six, and every
    /// one of the six is entered by at least one sitting. A seventh way to open a sitting, or a site that
    /// stops being reachable, reddens here by name rather than passing quietly.</para>
    ///
    /// <para><b>These are the six a CONSOLE PRESS can reach.</b> #731 v2's <c>SheLedYouHere</c> is a seventh
    /// construction site and is deliberately not below: it cannot be entered by pressing a table on a floor
    /// nobody has walked yet — a contact has to be standing in the doorway first — so listing it here would
    /// be a row that could never be entered. <c>ThereIsOnePlaceASittingIsOpened</c> counts it, and
    /// <c>FollowMeIntoTheCabinetTests</c> drives it.</para>
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
        IReadOnlyDictionary<string, PinLedger.Row> pinned = PinLedger.Pinned(Suite);
        var wrong = new List<string>();
        var seen = new List<string>();

        foreach (bool pour in new[] { false, true })
        {
            foreach ((string site, string text) in EverySitting(pour).OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                string name = Named(site, pour);
                seen.Add(name);
                string got = Sha256(text);
                string chars = text.Length.ToString(Inv);

                if (!pinned.TryGetValue(PinLedger.Key(ShaProbe, name), out PinLedger.Row want))
                {
                    wrong.Add($"  {name} — {chars} chars, sha256 {got}   // pinned nothing at all");
                    continue;
                }
                string pinnedChars = pinned.TryGetValue(PinLedger.Key(CharsProbe, name), out PinLedger.Row c)
                    ? c.Value : "(nothing)";
                if (!string.Equals(want.Value, got, StringComparison.Ordinal) || pinnedChars != chars)
                {
                    wrong.Add($"  {name} — {chars} chars, sha256 {got}"
                        + $"\n      pinned {pinnedChars} chars, sha256 {want.Value}");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} sitting(s) of {seen.Count} do not match what they hashed to on the old code:\n"
            + string.Join("\n", wrong)
            + $"\n\nIf the change is intended, re-pin BY MEASUREMENT and paste the printed report into the "
            + $"PR:\n  {PinLedger.Invocation}");

        foreach (PinLedger.Row row in pinned.Values.Where(r => r.Probe == ShaProbe))
        {
            Assert.Contains(row.Scene, seen);
        }
        Assert.Equal(seen.Count * 2, pinned.Count);
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

    /// <summary>
    /// THERE IS ONE PLACE A SITTING IS OPENED — the half of this lane the fingerprint above is blind to.
    ///
    /// <para><c>RendererInterop.PlayCue</c> is a <c>[JSImport]</c> behind <c>OperatingSystem.IsBrowser()</c>:
    /// off a browser it compiles to nothing, and <c>StateHasChanged</c> on a component with a render handle
    /// already pending is a no-op by construction. Neither can be read off a running object, so neither is
    /// in a hash — which means that without this fact a lane could drop the cue from <c>TakeThisSeat</c>, or
    /// play it BEFORE the seat is in the field, and sixteen digests would still reproduce. So the tail is
    /// held where it can be seen: in the source.</para>
    ///
    /// <para>Three laws. <b>Seven sites, and every one goes through the one method</b> — an eighth
    /// <c>new TableTalk</c>, or a site that goes back to building the record and assigning it itself,
    /// reddens by count. <b>One assignment</b> — exactly one line in the whole family puts a sitting into
    /// <c>Table</c>, and it is in <c>Seating.Sit.cs</c> (clearing it is not opening one, so
    /// <c>Table = null</c> is not counted). <b>And the tail is these three statements in THIS order</b>,
    /// spelled out, because the order is the behaviour: the panel the captain is about to read is a
    /// function of the seat, so the draw has to be the frame after the seat is in.</para>
    ///
    /// <para><b>#973 L5b · 7 → 8, and the eighth is <c>Seating.BarTop.cs · TryTakeBarTop</c>.</b> A top in a
    /// DOCKED STATION'S BAR — the first sitting in this game that is not on a surface excursion. #973 L0
    /// found the gap and wrote it down in its own file: <i>"all seven sites of it are gated on a
    /// <c>SurfaceExcursion</c>, and a docked berth has none — the bar's seven tops are drawn dressing with no
    /// chairs and no console."</i> It is a construction site like the other seven and obeys the same three
    /// laws.</para>
    ///
    /// <para>It has no row in <see cref="WhichSite"/> and no pin above, for the SAME reason the seventh has
    /// not: those two are keyed on cases the console sweep can reach, and the sweep drives a
    /// <c>SurfaceExcursion</c> on a Hive floor — there is no berth in it, no docked deck and therefore no
    /// <c>BarTop</c> console to press. The sitting it opens is fingerprinted end to end by
    /// <c>TheEighthSeatIsInTheDockedBarTests</c> instead, which docks a berth and presses the top.</para>
    ///
    /// <para><b>#1016 · AND THE COUNT STAYS AT EIGHT WHILE THE SHIP GROWS TWO SEATS, which is the point.</b>
    /// Owner, on 7 Deck: <i>"Why no table here to sit at?"</i>, <i>"Why no table in cabin either?"</i>, <i>"I
    /// expect to have a bar table like this in this ships galley also.... feature complete."</i> A top in her
    /// cantina and the desk in CABIN 1 are the same VERB as a top in a station bar, so they come through
    /// <c>Seating.BarTop.cs · TryTakeBarTop</c> and build no ninth <c>new TableTalk</c>: what differs between
    /// the three rooms — the plate, the setting, whether it is behind a door, whether anybody could ever walk
    /// up — travels on the page's answer (<c>BarTopUnderfoot</c>) as VALUES. That is #870 lane 6d's own rule
    /// kept rather than bent, and it is why this number did not move. Those two sittings are driven end to
    /// end by <c>TheShipHasSeatsAboardTests</c>.</para>
    ///
    /// <para><b>#731 v2 · 6 → 7, and the seventh is <c>Seating.Table.cs · SheLedYouHere</c>.</b> A contact
    /// stands up mid-sentence, crosses the hall and holds a booth door open; the press that would otherwise
    /// take a free top resumes HER conversation instead, with what was already said to her carried in on the
    /// initializer. It is a construction site like the other six and it obeys the same three laws.</para>
    ///
    /// <para>It has no row in <see cref="WhichSite"/> and no pin above, and that is not an oversight: those
    /// two are keyed on cases the console sweep can REACH, and no press on a virgin floor reaches this one —
    /// it needs a walker standing in a doorway first. The sitting it opens is fingerprinted end to end by
    /// <c>FollowMeIntoTheCabinetTests</c> instead, which drives the walk that makes it reachable.</para>
    /// </summary>
    [Fact]
    public void ThereIsOnePlaceASittingIsOpened()
    {
        IReadOnlyList<(string Name, string Text)> family = TheSeatFamily();

        int built = family.Sum(f => Occurrences(f.Text, "new TableTalk"));
        int through = family.Sum(f => Occurrences(f.Text, "TakeThisSeat(new TableTalk"));
        Assert.True(built == 8 && through == 8,
            $"#870 lane 6d · the seat family builds {built} sitting(s) and {through} of them go through "
            + "`TakeThisSeat`. There are EIGHT construction sites and every one of them must, because the "
            + "reveal cue and the draw live in that method and nowhere else. A ninth way to open a "
            + "sitting needs this count moved, a line in the PR body, and — if a console press can reach it "
            + "— a row in `WhichSite` and a pin above.\n\n"
            + string.Join("\n", family.Select(f =>
                $"  {f.Name}: {Occurrences(f.Text, "new TableTalk")} built, "
                + $"{Occurrences(f.Text, "TakeThisSeat(new TableTalk")} through")));

        string[] assigns =
        [
            .. family.SelectMany(f => Code(f.Text)
                .Where(l => l.StartsWith("Table = ", StringComparison.Ordinal) && l != "Table = null;")
                .Select(l => $"{f.Name}: {l}")),
        ];
        Assert.True(assigns.Length == 1 && assigns[0].StartsWith("Seating.Sit.cs:", StringComparison.Ordinal),
            "#870 lane 6d · a sitting goes into the field in exactly ONE place, and it is "
            + "`Seating.Sit.cs · TakeThisSeat`. Found:\n  " + string.Join("\n  ", assigns));

        string[] tail =
        [
            .. Code(BodyOf(Named(family, "Seating.Sit.cs"), "private void TakeThisSeat(TableTalk seat)")),
        ];
        string[] theThreeLines =
        [
            "Table = seat;",
            "RendererInterop.PlayCue(\"reveal\");",
            "_host.StateHasChanged();",
        ];
        Assert.Equal(theThreeLines, tail);
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The seat's own files, by name, in ordinal order — the same text the six sites were read out
    /// of before the tail moved.</summary>
    private static IReadOnlyList<(string Name, string Text)> TheSeatFamily()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
        {
            at = at.Parent;
        }

        Assert.True(at is not null, $"could not find the repo root above {AppContext.BaseDirectory}");
        string dir = Path.Combine(at!.FullName, "src", "SpaceSails.Client", "Pages", "Seating");
        (string Name, string Text)[] files =
        [
            .. Directory.EnumerateFiles(dir, "Seating*.cs")
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => (Path.GetFileName(p), File.ReadAllText(p))),
        ];
        Assert.True(files.Length >= 6,
            $"the seat family is {files.Length} file(s) — this guard is reading a dead directory: {dir}");
        return files;
    }

    private static string Named(IReadOnlyList<(string Name, string Text)> family, string name)
    {
        string? hit = family.Where(f => f.Name == name).Select(f => f.Text).FirstOrDefault();
        Assert.True(hit is not null, $"the seat family has no `{name}` — this guard reads a dead path.");
        return hit!;
    }

    private static int Occurrences(string text, string needle)
    {
        int n = 0;
        for (int at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }

        return n;
    }

    /// <summary>Statements only. Doc comments, whole-line comments and blanks are skipped — the moved
    /// docblocks travelled byte-identical and are not what that fact is about.</summary>
    private static IEnumerable<string> Code(string text) =>
        text.Split('\n')
            .Select(l => l.Trim().TrimEnd('\r'))
            .Where(l => l.Length > 0 && !l.StartsWith("//", StringComparison.Ordinal));

    /// <summary>The body of a member, brace-matched off its signature.</summary>
    private static string BodyOf(string text, string signature)
    {
        int at = text.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"`{signature}` is not there — this guard is reading a dead name.");
        int open = text.IndexOf('{', at);
        Assert.True(open >= 0, $"`{signature}` has no body.");

        int depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            depth += text[i] == '{' ? 1 : text[i] == '}' ? -1 : 0;
            if (depth == 0)
            {
                return text[(open + 1)..i];
            }
        }

        throw new InvalidOperationException($"`{signature}`'s body is not brace-balanced.");
    }

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
