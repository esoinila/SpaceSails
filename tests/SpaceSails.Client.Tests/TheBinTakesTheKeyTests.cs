using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #828 · THE BIN TAKES [E] — and there is still only ONE verb underneath it.
///
/// <para>Owner, evening playtest at his own table in the upper canteen (2026-08-11): <i>"I think the trash
/// could be an e-use … where we select from inventory the processed items we rip and deposit into
/// trash."</i> #798 shipped rip-and-bin PAPER-FIRST — the verb on a satchel row, the bin a range check —
/// and this is the mirror. The issue states its own law: <b>both doors end in the same filed fact and the
/// same satchel mutation for the same (paper, bin), proven by driving each door in a test and comparing the
/// books.</b></para>
///
/// <h3>Why this file DRIVES the component instead of reading it</h3>
/// <para>A shape guard could assert that both call sites type the same method name today, and would go
/// green for ever on the day somebody gives the picker its own quiet copy of the act — the fifth named bug
/// class in this repo is a guard that cannot tell a pass from a fail. So the two doors are found in the
/// SHIPPING RAZOR (the handler each button is actually wired to, read off the markup) and then invoked, on
/// two identical worlds, and the two books are compared line for line. Rewire either button to anything
/// that files one word differently and this goes red, because the test presses whatever the page presses.
/// </para>
///
/// <para>Proven red on purpose: give <c>BinnableFinds</c>' rows a second act — a <c>FileNote</c> of their
/// own, or a bin chosen from anywhere but the boots — and the comparison fails on the entry the other door
/// never wrote.</para>
/// </summary>
public sealed class TheBinTakesTheKeyTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    // ── (a) THE TWO DOORS, DRIVEN ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · ONE VERB, TWO GRIPS. The spread row and the bin picker are driven over the same paper at the
    /// same bucket, and the book, the sleeve and the sentence come out identical.
    ///
    /// <para>The bin door is driven the WHOLE way in — the [E] press at the bucket, the page it opens, the
    /// row that page offers — so what is compared is the feature a player touches and not a method a test
    /// picked out.</para>
    /// </summary>
    [Fact]
    public void BOTH_DOORS_EndInTheSameFiledFactAndTheSameSleeve()
    {
        (string body, int level, RipAndBin.Bin bin) = ABinSomewhere();
        var paper = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:the-manifest");

        // Door one · the spread row: "I am working and I want this gone."
        Pages.Map atTheTable = Bench(body, level, bin, paper, worked: true);
        OnPage(atTheTable, "Spread");
        Press(atTheTable, TheHandlerOn(TheSpreadsRipControl(), "dig"), paper);

        // Door two · the bin: "I am leaving and dumping what is done." Pressed at the bucket, exactly as a
        // captain meets it — nothing here reaches past the [E] key for the page or past the page for the row.
        Pages.Map atTheBin = Bench(body, level, bin, paper, worked: true);
        PressE(atTheBin);
        Assert.True(ThePickerIsOpen(atTheBin),
            "[E] at a published standing spot did not open the bin over the sleeve.");
        Satchel.Item offered = Assert.Single(WhatThePickerOffers(atTheBin));
        Press(atTheBin, TheHandlerOn(TheBinPicker(), "paper"), offered);

        // THE LAW. One line at a time, so a failure says which of the three drifted.
        Assert.Equal(TheBook(atTheTable), TheBook(atTheBin));
        Assert.Equal(TheSleeve(atTheTable), TheSleeve(atTheBin));
        Assert.Equal(WhatWasSaid(atTheTable), WhatWasSaid(atTheBin));

        // …and it ACTUALLY HAPPENED. Two doors that both did nothing would agree perfectly for ever, which
        // is the whole of this project's fifth bug class — so the act is asserted, at this bin, by name.
        Assert.Empty(TheSleeve(atTheBin));
        Assert.Contains(TheBook(atTheBin),
            line => line.Contains(RipAndBin.DisposalNote(
                "📋", bin.Tier)[..30], StringComparison.Ordinal));
        Assert.Contains(TheBook(atTheBin),
            line => line.Contains(RipAndBin.TheBin(bin.Tier), StringComparison.Ordinal));
    }

    /// <summary>
    /// #828 · …AND A SHEET NOTHING HAS BEEN DUG OUT OF GOES THE SAME WAY, through both doors again.
    ///
    /// <para>The owner's ruling is <i>warn, then allow</i>: an unworked paper is flagged, never refused, and
    /// a refusal that crept into one door and not the other would be a captain's own paper held hostage by
    /// which page they were on.</para>
    /// </summary>
    [Fact]
    public void AN_UNWORKED_SHEET_IsBinnableThroughEitherDoorAndFlaggedInThePicker()
    {
        (string body, int level, RipAndBin.Bin bin) = ABinSomewhere();
        var paper = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:not-yet-read");

        Pages.Map atTheTable = Bench(body, level, bin, paper, worked: false);
        OnPage(atTheTable, "Spread");
        Press(atTheTable, TheHandlerOn(TheSpreadsRipControl(), "dig"), paper);

        Pages.Map atTheBin = Bench(body, level, bin, paper, worked: false);
        PressE(atTheBin);
        Satchel.Item offered = Assert.Single(WhatThePickerOffers(atTheBin));

        // The flag is quiet and the warning is in the hint — BEFORE the press, which is where a price
        // belongs (#696). Neither of them is a gate.
        Assert.Equal(RipAndBin.NotYetWorkedFlag, Ask<string>(atTheBin, "BinRowFlag", offered));
        Assert.StartsWith(RipAndBin.NotYetWorkedWarning, Ask<string>(atTheBin, "BinRowHint", offered),
            StringComparison.Ordinal);

        Press(atTheBin, TheHandlerOn(TheBinPicker(), "paper"), offered);

        Assert.Empty(TheSleeve(atTheBin));
        Assert.Equal(TheBook(atTheTable), TheBook(atTheBin));
        Assert.Equal(TheSleeve(atTheTable), TheSleeve(atTheBin));
    }

    /// <summary>#828 · THE ORDER THE PICKER LISTS IN: what the book already has leads, what it never got
    /// follows, and the rounds in the same sleeve are not on the page at all — a bin does not take
    /// property.</summary>
    [Fact]
    public void THE_PICKER_LeadsWithWhatIsAlreadyInTheBook()
    {
        (string body, int level, RipAndBin.Bin bin) = ABinSomewhere();
        var glanced = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:glanced");
        var dug = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:dug");
        var rounds = new Satchel.Item(Satchel.Kind.Rounds, "loose", 12);

        // The sleeve holds the unworked sheet FIRST, so leading with the worked one is an order and not the
        // list arriving that way by luck.
        Pages.Map map = Bench(body, level, bin, [glanced, rounds, dug], worked: [dug]);

        List<Satchel.Item> rows = WhatThePickerOffers(map);
        Assert.Equal([dug, glanced], rows);
    }

    /// <summary>#828 · AND AN EMPTY SLEEVE IS REFUSED POLITELY, on the page, in the bin's own words. A
    /// picker that opened onto an unexplained blank is #603's founding sin with a lid on it.</summary>
    [Fact]
    public void AN_EMPTY_SLEEVE_GetsTheBinsOwnRefusal()
    {
        (string body, int level, RipAndBin.Bin bin) = ABinSomewhere();
        Pages.Map map = Bench(body, level, bin, [], worked: []);

        PressE(map);
        Assert.True(ThePickerIsOpen(map), "[E] at a bin with an empty sleeve did not open the page at all.");
        Assert.Empty(WhatThePickerOffers(map));

        // …and the page says the line rather than showing nothing.
        Assert.Contains("SpaceSails.Core.RipAndBin.NothingToBinLine", TheBinPicker(), StringComparison.Ordinal);
    }

    // ── (b) THE PRESS ITSELF ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · EVERY PUBLISHED STANDING SPOT ANSWERS [E].
    ///
    /// <para>A bin whose own <c>StandX/StandY</c> does not open the picker is a fixture the owner cannot
    /// use, and it would be invisible to every other test in the repo — this is the "boot every scene" rule
    /// pointed at one fixture: walk to each bucket on real floors and press the key.</para>
    ///
    /// <para>It also proves the claim is NARROW: a step away from the bin, [E] belongs to the room again.</para>
    /// </summary>
    [Fact]
    public void EVERY_BIN_TakesTheKeyFromItsOwnStandingSpotAndFromNowhereElse()
    {
        var checkedBins = 0;
        var bad = new List<string>();
        foreach ((string body, int level, RipAndBin.Bin bin) in ManyBins())
        {
            checkedBins++;
            Pages.Map map = Bench(body, level, bin, new Satchel.Item(Satchel.Kind.Paper, "p"), worked: true);
            PressE(map);
            if (!ThePickerIsOpen(map))
            {
                bad.Add($"  {body} B{-level} {bin.Plate}: [E] on the published spot opened nothing.");
                continue;
            }

            // …and far from it the key is the room's again — the picker is a thing you walk to.
            Pages.Map away = Bench(body, level, bin, new Satchel.Item(Satchel.Kind.Paper, "p"), worked: true);
            Set(away, "_avatarX", bin.X + (RipAndBin.ReachDu * 3));
            Set(away, "_avatarY", bin.Y + (RipAndBin.ReachDu * 3));
            PressE(away);
            if (ThePickerIsOpen(away))
            {
                bad.Add($"  {body} B{-level} {bin.Plate}: the bin took [E] from three reaches away.");
            }
        }

        Assert.True(checkedBins >= 8, $"only {checkedBins} bin(s) pressed — this net proves nothing.");
        Assert.True(bad.Count == 0, string.Join("\n", ["a bin nobody can use:", .. bad]));
    }

    /// <summary>
    /// #828 · THE BAR SAYS SO. A verb nobody is told about is a verb nobody has (#212/#537 — the owner
    /// pressing T at a map that never mentioned it).
    ///
    /// <para>Underground the keybar reads <i>"E — use"</i> on every square of the building, which is exactly
    /// nothing at the one spot where the key opens the sleeve over a bucket. So the strip names the RUNG
    /// while the captain stands at it, and goes back to the room's own words a few paces off.</para>
    /// </summary>
    [Fact]
    public void THE_KEYBAR_NamesTheBucketYouAreStandingAt()
    {
        (string body, int level, RipAndBin.Bin bin) = ABinSomewhere();
        Pages.Map map = Bench(body, level, bin, new Satchel.Item(Satchel.Kind.Paper, "p"), worked: true);

        Assert.Contains(RipAndBin.KeyPrompt(bin.Tier), TheKeyBar(map), StringComparison.Ordinal);

        Set(map, "_avatarX", bin.X + (RipAndBin.ReachDu * 3));
        Set(map, "_avatarY", bin.Y + (RipAndBin.ReachDu * 3));
        Assert.DoesNotContain(RipAndBin.Glyph, TheKeyBar(map), StringComparison.Ordinal);
    }

    // ── (c) NO SECOND SHREDDER ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · THE PICKER DECIDES NOTHING. It is a page: rows, an order and words. Every law it appears to
    /// have is asked of Core or of the act, and the markup may never grow a hand of its own — the day it
    /// files a note or empties a sleeve, the two doors are two features wearing one name.
    /// </summary>
    [Fact]
    public void THE_PICKER_IsAPageAndNotASecondShredder()
    {
        string page = TheBinPicker();

        foreach (string hand in new[]
        {
            "Satchel.Remove", "FileNote(", "_fieldNotes", "_caseThreads", "_workedUp",
            "RequestVaultSave", "disabled=",
        })
        {
            Assert.False(page.Contains(hand, StringComparison.Ordinal),
                $"the bin picker's markup contains `{hand}` — the page has started deciding things, and the "
                + "two doors have begun to be two features.");
        }

        // The words are Core's, every one of them.
        Assert.Contains("RipAndBin.TierBet(", page, StringComparison.Ordinal);
        Assert.Contains("RipAndBin.NoBinLine", page, StringComparison.Ordinal);
        Assert.Contains("RipAndBin.Glyph", page, StringComparison.Ordinal);

        // And the tab follows the FIXTURE, the way the spread tab follows the posture.
        string razor = Razor();
        Assert.Contains("@if (BinWithinReach() is { } binTab)", razor, StringComparison.Ordinal);
        Assert.Contains("_satchelPage = SatchelPage.Bin", razor, StringComparison.Ordinal);
    }

    /// <summary>#828 · The key is claimed AHEAD of the console dispatch and only by the nearer fixture, so
    /// nothing else on the floor loses its [E]. Read off the dispatch itself, because the order of those two
    /// lines IS the rule.</summary>
    [Fact]
    public void THE_KEY_IsClaimedByWhicheverFixtureIsNearer()
    {
        string deck = Deck();

        int press = deck.IndexOf("if (TryOpenTheBinOverTheSleeve())", StringComparison.Ordinal);
        int dispatch = deck.IndexOf("switch (_deckPlan.NearestConsole(", StringComparison.Ordinal);
        Assert.True(press > 0, "[E] never reaches the bin.");
        Assert.True(press < dispatch, "the bin is asked after the console switch, where it can never answer.");

        string bin = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Bin.cs"));
        int claim = bin.IndexOf("private RipAndBin.Bin? TheBinTakingYourPress()", StringComparison.Ordinal);
        Assert.True(claim > 0, "the claim has no method this guard can see.");
        string body = bin[claim..bin.IndexOf("\n    }", claim, StringComparison.Ordinal)];
        Assert.Contains("NearestConsoleSpot(_avatarX, _avatarY)", body, StringComparison.Ordinal);
        Assert.Contains("BinWithinReach()", body, StringComparison.Ordinal);
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A world with a captain standing at one published bin, a sleeve, and nothing else running.
    /// Everything the act reads is set here and nothing is faked: the bins are the floor's own, the deck is
    /// the one the game builds for that floor, and the excursion is the shipping type.</summary>
    private static Pages.Map Bench(
        string body, int level, RipAndBin.Bin bin, Satchel.Item paper, bool worked) =>
        Bench(body, level, bin, [paper], worked ? [paper] : []);

    private static Pages.Map Bench(
        string body, int level, RipAndBin.Bin bin,
        IReadOnlyList<Satchel.Item> sleeve, IReadOnlyList<Satchel.Item> worked)
    {
        var map = new Pages.Map();

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        SetProp(ex, "Stop", stop);
        SetProp(ex, "RestoreHavenId", null);
        SetProp(ex, "Site", new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        SetProp(ex, "Floor", level);

        // #1016 · The register is the CASE's now: one page-level set, vault-persisted, because the owner
        // ruled the table verbs are not tied to a location. Same keys, same law, one owner up.
        var register = (HashSet<string>)Get(map, "_workedUp")!;
        foreach (Satchel.Item item in worked)
        {
            register.Add($"{item.Kind}:{item.Id}");
        }

        Set(map, "_surface", ex);
        Set(map, "_satchel", new List<Satchel.Item>(sleeve));
        Set(map, "_avatarX", bin.StandX);
        Set(map, "_avatarY", bin.StandY);

        // The floor the game would draw, built the way the game builds it — so the [E] press meets the real
        // consoles and the witness ladder asks the real walls.
        typeof(Pages.Map).GetMethod("RebuildSurfaceDeck", Hidden)!.Invoke(map, null);

        // The satchel is OPEN for both doors, because it is open in play for both doors — and #680 sends the
        // act's own sentence into the dialog rather than under the backdrop's blur.
        Set(map, "_showSatchel", true);
        return map;
    }

    /// <summary>The contextual strip along the bottom of the surface HUD, built for the live excursion —
    /// the shipping method, so what is asserted is what a player reads.</summary>
    private static string TheKeyBar(Pages.Map map) =>
        (string)typeof(Pages.Map).GetMethod("BuildSurfaceKeyHints", Hidden)!
            .Invoke(map, [Get(map, "_surface")])!;

    private static void PressE(Pages.Map map) =>
        typeof(Pages.Map).GetMethod("InteractAtConsole", Hidden)!.Invoke(map, null);

    private static bool ThePickerIsOpen(Pages.Map map) =>
        (bool)Get(map, "_showSatchel")! && Get(map, "_satchelPage")!.ToString() == "Bin";

    private static void OnPage(Pages.Map map, string page) =>
        Set(map, "_satchelPage",
            Enum.Parse(typeof(Pages.Map).GetNestedType("SatchelPage", Hidden | BindingFlags.Static)!, page));

    private static List<Satchel.Item> WhatThePickerOffers(Pages.Map map) =>
        (List<Satchel.Item>)typeof(Pages.Map).GetMethod("BinnableFinds", Hidden)!.Invoke(map, null)!;

    private static T Ask<T>(Pages.Map map, string method, Satchel.Item item) =>
        (T)typeof(Pages.Map).GetMethod(method, Hidden)!.Invoke(map, [item])!;

    private static void Press(Pages.Map map, string handler, Satchel.Item item)
    {
        MethodInfo? act = typeof(Pages.Map).GetMethod(handler, Hidden);
        Assert.True(act is not null, $"the razor presses `{handler}`, which the component does not have.");
        act!.Invoke(map, [item]);
    }

    private static IReadOnlyList<string> TheBook(Pages.Map map) =>
        [.. ((List<FieldNote>)Get(map, "_fieldNotes")!).Select(n => n.ToString())];

    private static IReadOnlyList<string> TheSleeve(Pages.Map map) =>
        [.. ((List<Satchel.Item>)Get(map, "_satchel")!).Select(i => i.Stored)];

    private static string? WhatWasSaid(Pages.Map map) => (string?)Get(map, "_satchelOutcome");

    // ── THE FLOORS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The first published bin this build carves that DESTROYS ON THE PRESS, on a floor with air in it.
    /// Chosen by walking the generator rather than typed in, so nothing here can go stale against a
    /// re-carve.
    ///
    /// <para>#828 · The secure disposal is stepped over on purpose and is driven by its own file: the top
    /// rung opens a watched beat in front of the act (<c>Processing.Work.Shred</c>), so a test that pressed
    /// it and then looked at the sleeve would be asserting the middle of an interaction rather than the end
    /// of one. Every law in THIS file is about the two doors ending in one act, and it is stated on the
    /// rungs where the press IS the act.</para>
    /// </summary>
    private static (string Body, int Level, RipAndBin.Bin Bin) ABinSomewhere() =>
        ManyBins().First(b => b.Bin.Tier != RipAndBin.Tier.SecureDisposal);

    /// <summary>Bins off several real sites — the same net the Core audit casts, cut to a size a client test
    /// can afford to build a deck for.</summary>
    private static IEnumerable<(string Body, int Level, RipAndBin.Bin Bin)> ManyBins()
    {
        foreach (string body in new[] { "luna", "miranda", "europa" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor =
                    UndergroundComplex.Build(body, level, MoonSurface.ExpeditionField());
                foreach (RipAndBin.Bin bin in floor.TheBins)
                {
                    yield return (body, level, bin);
                }
            }
        }
    }

    // ── THE DOORS, READ OFF THE SHIPPING MARKUP ───────────────────────────────────────────────────────

    /// <summary>The method a button is really wired to. Read from the razor rather than typed here, which is
    /// the whole point of this file: a door rewired to a second implementation is still the door this test
    /// presses, so the comparison above cannot be got round by adding a method.</summary>
    private static string TheHandlerOn(string markup, string rowVariable)
    {
        Match m = Regex.Match(markup, @"@onclick=""\(\) => (\w+)\(" + rowVariable + @"\)""");
        Assert.True(m.Success, $"no press on a `{rowVariable}` row in this markup — the door has moved.");
        return m.Groups[1].Value;
    }

    /// <summary>#828's page, sliced whole.</summary>
    private static string TheBinPicker()
    {
        string razor = Razor();
        int at = razor.IndexOf("else if (_satchelPage == SatchelPage.Bin)", StringComparison.Ordinal);
        Assert.True(at > 0, "the satchel has no bin page.");
        // To the next `else` at the PAGE's own indent — sixteen spaces. Written without a trailing newline
        // because the file is CRLF and a guard that only works on one line ending is a guard nobody can move.
        int end = razor.IndexOf("\n                else", at, StringComparison.Ordinal);
        Assert.True(end > at, "the bin page has no end this guard can find.");
        return razor[at..end];
    }

    /// <summary>…and #798's control on the spread row, which must go on working exactly as it did.</summary>
    private static string TheSpreadsRipControl()
    {
        string razor = Razor();
        int at = razor.IndexOf("@if (RipIsOffered(dig))", StringComparison.Ordinal);
        Assert.True(at > 0, "the spread row has lost its shredder.");
        return razor[at..(at + 400)];
    }

    /// <summary>#870 · The deck page is seven partials by subject now, so "the deck" a guard reads over is
    /// all of them — exactly the text it read out of one file before the split.</summary>
    private static string Deck() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Deck*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    private static string Razor() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"));

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static object? Get(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static void SetProp(object o, string property, object? value) =>
        o.GetType().GetProperty(property)!.SetValue(o, value);

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }
}
