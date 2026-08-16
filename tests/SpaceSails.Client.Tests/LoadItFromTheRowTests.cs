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
/// #837 · LOAD IT FROM THE ROW — and there is still only ONE verb underneath it.
///
/// <para>Owner, evening playtest with the satchel open on <i>12 loose rounds</i> (2026-08-11): <i>"I would
/// expect like load it into one or both guns."</i> #803 shipped the put verb POSITION-FIRST — press [I] over
/// a set-down sentry — and this is the mirror: the row, the chooser and a count. The issue states its own
/// law: <b>both grips end in the identical magazine mutation and pocket remainder for the same (rounds,
/// target), proven by driving each in a test and comparing.</b></para>
///
/// <h3>Why this file DRIVES the component instead of reading it</h3>
/// <para>The same reasoning #828's guard wrote down. A shape guard could assert that both call sites type
/// the same method name today and would go green for ever on the day somebody gives the chooser its own
/// quiet copy of the act — the fifth named bug class in this repo is a guard that cannot tell a pass from a
/// fail. So both grips are found in the SHIPPING RAZOR (the handler each button is actually wired to, read
/// off the markup), invoked on two identical worlds, and the drums and the pockets are compared.</para>
///
/// <para><b>Proven red on purpose.</b> Wiring the chooser's press to a second arithmetic — <c>gun.Rounds +=
/// pick.Share</c> and a <c>Satchel.Remove</c> of its own, which is exactly the shortcut a picker invites —
/// fails <c>BOTH_GRIPS</c> on the drum (99 against 99 is agreement; 105 against 99 is not) and fails
/// <c>THE_CHOOSER_CountsAreTheMagazinesReadoutsOwnNumbers</c> on the readout, which cannot print 105 at
/// all.</para>
/// </summary>
public sealed class LoadItFromTheRowTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>A gun on the ground, as the bench sets one up.</summary>
    private readonly record struct Gun(
        string Unit, int Rounds, bool Deployed, double X, double Y, string Ammo);

    // ── (a) THE TWO GRIPS, DRIVEN ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #837 · ONE VERB, TWO GRIPS. The [I]-over-the-bot press and the satchel row's chooser are driven over
    /// the same handful of rounds into the same gun, and the drum, the kind and the pocket come out
    /// identical.
    ///
    /// <para>Both grips are driven the WHOLE way in — the row's own control, the chooser it opens, the row
    /// that chooser offers — so what is compared is the feature a player touches and not a method a test
    /// picked out.</para>
    /// </summary>
    [Fact]
    public void BOTH_GRIPS_EndInTheSameDrumAndTheSamePocket()
    {
        var rounds = new Satchel.Item(Satchel.Kind.Rounds, Ammunition.Issue.Id, 12);
        Gun standing = new("R-3B", 11, Deployed: true, X: 1.0, Y: 0.0, Ammunition.Issue.Id);

        // Grip one · [I] over the bot: the captain standing on it, offering the whole handful. This is the
        // press the row has made since #803, reached through the row's own `read it →` control.
        Pages.Map overTheBot = Bench([standing], rounds);
        Press(overTheBot, TheHandlerOn(TheRoundsRow(), "item"), rounds);

        // Grip two · the satchel row: load →, then the gun's row in the chooser. Nothing here reaches past
        // the control for the page or past the page for the row.
        Pages.Map fromTheRow = Bench([standing], rounds);
        Open(fromTheRow, TheOpenerOn(TheRoundsRow()), rounds);
        Assert.True(TheChooserIsOpen(fromTheRow), "load → on the rounds row opened no chooser at all.");
        SentryHandLoad.Pick only = Assert.Single(WhatTheChooserOffers(fromTheRow));
        Assert.Equal("R-3B", only.Unit);
        PressRow(fromTheRow, TheHandlerOn(TheChooser(), "gun"), only);

        // THE LAW, one fact at a time so a failure says which of the three drifted.
        Assert.Equal(TheDrums(overTheBot), TheDrums(fromTheRow));
        Assert.Equal(ThePocket(overTheBot), ThePocket(fromTheRow));
        Assert.Equal(WhatWasSaid(overTheBot), WhatWasSaid(fromTheRow));

        // …and it ACTUALLY HAPPENED. Two grips that both did nothing would agree perfectly for ever, which
        // is this project's fifth bug class — so the act is asserted, by number: eleven plus twelve.
        Assert.Equal(["R-3B 23 " + Ammunition.Issue.Id], TheDrums(fromTheRow));
        Assert.Empty(ThePocket(fromTheRow));
    }

    /// <summary>
    /// #837 · …AND WITH A CEILING IN THE WAY, both grips leave the SAME remainder in the pocket.
    ///
    /// <para>The seam between a pocket and a drum is where this repo's third named bug class lives with
    /// ammunition in it, and a ceiling is what opens the seam: ninety-four rounds in the drum, twelve in the
    /// pocket, five go in and seven stay. Both grips must count the seven the same way or one of them is
    /// quietly rounding a captain out of their property.</para>
    /// </summary>
    [Fact]
    public void BOTH_GRIPS_LeaveTheSameRoundsInThePocketWhenTheDrumFillsUp()
    {
        var rounds = new Satchel.Item(Satchel.Kind.Rounds, Ammunition.Issue.Id, 12);
        Gun nearlyFull = new("R-3B", SentryBot.MaxMagazine - 5, Deployed: true, 1.0, 0.0, Ammunition.Issue.Id);

        Pages.Map overTheBot = Bench([nearlyFull], rounds);
        Press(overTheBot, TheHandlerOn(TheRoundsRow(), "item"), rounds);

        Pages.Map fromTheRow = Bench([nearlyFull], rounds);
        Open(fromTheRow, TheOpenerOn(TheRoundsRow()), rounds);
        SentryHandLoad.Pick only = Assert.Single(WhatTheChooserOffers(fromTheRow));

        // The price is known BEFORE the press (#696): the row already says the drum will read 99/99 and that
        // seven stay behind.
        Assert.Equal(5, only.Ceiling);
        Assert.Equal(5, only.Share);
        Assert.Equal(7, only.Kept);
        Assert.Equal(SentryBot.MagazineCell(SentryBot.MaxMagazine), only.After);

        PressRow(fromTheRow, TheHandlerOn(TheChooser(), "gun"), only);

        Assert.Equal(TheDrums(overTheBot), TheDrums(fromTheRow));
        Assert.Equal(ThePocket(overTheBot), ThePocket(fromTheRow));
        Assert.Equal([$"R-3B {SentryBot.MaxMagazine} {Ammunition.Issue.Id}"], TheDrums(fromTheRow));
        Assert.Equal([$"{Satchel.Kind.Rounds}:{Ammunition.Issue.Id}:7"], ThePocket(fromTheRow));
    }

    // ── (b) THE CHOOSER'S NUMBERS ARE THE INSTRUMENT'S NUMBERS ────────────────────────────────────────

    /// <summary>
    /// #837 · THE PICKER SHOWS WHAT THE MAGAZINES LINE WILL READ, BEFORE THE PRESS COMMITS.
    ///
    /// <para>The issue's second law, and the owner's own discipline from #696 applied to a magazine: whatever
    /// the picker shows must equal what the readout says afterwards. Both halves are asserted — the
    /// <c>Now</c> cell against the line as it stands, and the <c>After</c> cell against the line once the row
    /// has been pressed — because a preview that is right about the present and wrong about the future is the
    /// sim and the sentence disagreeing one press later.</para>
    ///
    /// <para>There is no second arithmetic anywhere in it: both figures are
    /// <see cref="SentryBot.MagazineCell"/>, which is the function <see cref="SentryBot.MagazinesReadout"/>
    /// is itself built out of.</para>
    /// </summary>
    [Fact]
    public void THE_CHOOSER_CountsAreTheMagazinesReadoutsOwnNumbers()
    {
        var rounds = new Satchel.Item(Satchel.Kind.Rounds, Ammunition.Issue.Id, 12);
        Pages.Map map = Bench(
            [
                new("K-77", 12, Deployed: false, 0.0, 0.0, Ammunition.Issue.Id),
                new("R-3B", 40, Deployed: true, 1.0, 0.0, Ammunition.Issue.Id),
            ],
            rounds);

        Open(map, TheOpenerOn(TheRoundsRow()), rounds);
        List<SentryHandLoad.Pick> picks = WhatTheChooserOffers(map);
        Assert.Equal(2, picks.Count);

        string before = TheMagazinesLine(map);
        foreach (SentryHandLoad.Pick pick in picks)
        {
            Assert.Contains($"{pick.Unit} {pick.Now}", before, StringComparison.Ordinal);
        }

        // …and the promise each row makes about the future is kept. Half into the sling first, so there is
        // still a pocket to feed the second gun with — which is the owner's split, and the reason the
        // chooser survives a press.
        for (var i = 0; i < 6; i++)
        {
            Nudge(map, Live(map, "K-77"), -1);
        }

        SentryHandLoad.Pick sling = Live(map, "K-77");
        Assert.Equal(6, sling.Share);
        PressRow(map, TheHandlerOn(TheChooser(), "gun"), sling);
        Assert.Contains($"K-77 {sling.After}", TheMagazinesLine(map), StringComparison.Ordinal);

        SentryHandLoad.Pick standing = Live(map, "R-3B");
        PressRow(map, TheHandlerOn(TheChooser(), "gun"), standing);
        Assert.Contains($"R-3B {standing.After}", TheMagazinesLine(map), StringComparison.Ordinal);

        // Both promises, still on the one line, together: 12 + 6 in the sling and 40 + 6 set down.
        Assert.Contains("K-77 18/99", TheMagazinesLine(map), StringComparison.Ordinal);
        Assert.Contains("R-3B 46/99", TheMagazinesLine(map), StringComparison.Ordinal);
    }

    /// <summary>One chooser row as it stands RIGHT NOW — never a row remembered across a press, because the
    /// pocket it was built against has moved.</summary>
    private static SentryHandLoad.Pick Live(Pages.Map map, string unit) =>
        WhatTheChooserOffers(map).Single(p => p.Unit == unit);

    /// <summary>
    /// #837 · THE SPLIT. Owner: <i>"and like how many each .. by default I guess it would be all into one in
    /// most cases, but we should give flexibility here."</i>
    ///
    /// <para>The default is ALL — one press, done — and the stepper is what buys the 8/4. This drives the
    /// stepper down to eight, presses, and then puts the remaining four in the other gun, checking after
    /// every step that the pocket still holds exactly what nobody has spent. Nothing may vanish in the seam
    /// (the third named bug class, with ammunition in it).</para>
    /// </summary>
    [Fact]
    public void THE_STEPPER_SplitsTheHandfulAndThePocketKeepsTheRemainder()
    {
        var rounds = new Satchel.Item(Satchel.Kind.Rounds, Ammunition.Issue.Id, 12);
        Pages.Map map = Bench(
            [
                new("K-77", 0, Deployed: false, 0.0, 0.0, Ammunition.Issue.Id),
                new("R-3B", 0, Deployed: true, 1.0, 0.0, Ammunition.Issue.Id),
            ],
            rounds);

        Open(map, TheOpenerOn(TheRoundsRow()), rounds);

        // The default is all twelve into whichever gun is pressed first.
        Assert.All(WhatTheChooserOffers(map), p => Assert.Equal(12, p.Share));

        // Four notches down the stepper: eight into the sling, four kept.
        for (var i = 0; i < 4; i++)
        {
            Nudge(map, Live(map, "K-77"), -1);
        }
        SentryHandLoad.Pick sling = Live(map, "K-77");
        Assert.Equal(8, sling.Share);
        Assert.Equal(4, sling.Kept);

        PressRow(map, TheHandlerOn(TheChooser(), "gun"), sling);

        Assert.Equal([$"{Satchel.Kind.Rounds}:{Ammunition.Issue.Id}:4"], ThePocket(map));
        Assert.Contains("K-77 08/99", TheMagazinesLine(map), StringComparison.Ordinal);

        // The chooser is still open, on a pocket of four — which is what makes an 8/4 two presses rather
        // than a second control.
        Assert.True(TheChooserIsOpen(map), "the chooser shut with rounds still in the pocket.");
        SentryHandLoad.Pick standing = Live(map, "R-3B");
        Assert.Equal(4, standing.Share);
        Assert.Equal(4, standing.Pocket);

        PressRow(map, TheHandlerOn(TheChooser(), "gun"), standing);

        Assert.Empty(ThePocket(map));
        Assert.Contains("K-77 08/99", TheMagazinesLine(map), StringComparison.Ordinal);
        Assert.Contains("R-3B 04/99", TheMagazinesLine(map), StringComparison.Ordinal);

        // …and with nothing left to place, the page shuts itself rather than offering to spend an empty
        // pocket.
        Assert.False(TheChooserIsOpen(map));
    }

    // ── (c) THE REACH RULE ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #837 · THE BOTS ON YOUR BACK ARE ALWAYS IN RANGE, and the one across the room is not.
    ///
    /// <para>The owner's amendment, and the half #803 refused outright: loading your own carried gun is the
    /// least demanding version of this verb, and the build was answering it with <i>"set it down first"</i>.
    /// A set-down bot beyond arm's length keeps the old rule — it is a walk, and the chooser does not list
    /// what the hands cannot reach.</para>
    ///
    /// <para>The tube's own gun is on none of these lists either. GATE-1 never runs dry, is not counted
    /// against your sling by the MAGAZINES line, and a row for it would be a count the instrument does not
    /// have — which is the second arithmetic this issue forbids, wearing a uniform.</para>
    /// </summary>
    [Fact]
    public void THE_REACH_IsTheSlingAlwaysAndArmsLengthOtherwise()
    {
        var rounds = new Satchel.Item(Satchel.Kind.Rounds, Ammunition.Issue.Id, 12);
        Pages.Map map = Bench(
            [
                // On the captain's back, and parked far from the avatar to prove the sling is not a radius.
                new("K-77", 12, Deployed: false, 400.0, 400.0, Ammunition.Issue.Id),
                new("R-3B", 12, Deployed: true, DeckPlan.InteractRadius * 4, 0.0, Ammunition.Issue.Id),
                new(SurfaceArrival.DoorSentryUnit, 99, Deployed: true, 0.5, 0.0, Ammunition.Issue.Id),
            ],
            rounds);

        Open(map, TheOpenerOn(TheRoundsRow()), rounds);
        SentryHandLoad.Pick only = Assert.Single(WhatTheChooserOffers(map));
        Assert.Equal("K-77", only.Unit);
        Assert.Equal(SentryBot.WhereItIs(false), only.Where);

        // …and the slung gun really takes them, which is the whole of the amendment.
        PressRow(map, TheHandlerOn(TheChooser(), "gun"), only);
        Assert.Contains("K-77 24/99", TheMagazinesLine(map), StringComparison.Ordinal);
        Assert.Empty(ThePocket(map));

        // Walk to the one holding the line, and it joins the list.
        Pages.Map walked = Bench(
            [new("R-3B", 12, Deployed: true, DeckPlan.InteractRadius * 4, 0.0, Ammunition.Issue.Id)], rounds);
        Open(walked, TheOpenerOn(TheRoundsRow()), rounds);
        Assert.Empty(WhatTheChooserOffers(walked));

        Set(walked, "_avatarX", DeckPlan.InteractRadius * 4);
        Assert.Equal("R-3B", Assert.Single(WhatTheChooserOffers(walked)).Unit);
    }

    /// <summary>
    /// #837 · A REFUSED ROW IS STILL A ROW, AND IT SAYS WHY. The house law (#212/#603): a full drum says it
    /// is full, a wrong-kind round says what is already in the magazine, and neither of them is a control
    /// that has quietly gone missing.
    /// </summary>
    [Fact]
    public void A_REFUSED_TargetIsListedAndNamesItsReason()
    {
        var lab = new Satchel.Item(Satchel.Kind.Rounds, Ammunition.LabTwoStage.Id, 6);
        Pages.Map map = Bench(
            [
                new("K-77", SentryBot.MaxMagazine, Deployed: false, 0.0, 0.0, Ammunition.LabTwoStage.Id),
                new("R-3B", 12, Deployed: true, 1.0, 0.0, Ammunition.Issue.Id),
            ],
            lab);

        Open(map, TheOpenerOn(TheRoundsRow()), lab);
        List<SentryHandLoad.Pick> picks = WhatTheChooserOffers(map);
        Assert.Equal(2, picks.Count);

        SentryHandLoad.Pick full = picks.Single(p => p.Unit == "K-77");
        SentryHandLoad.Pick wrongKind = picks.Single(p => p.Unit == "R-3B");

        Assert.False(full.Worked);
        Assert.False(wrongKind.Worked);
        Assert.Equal(0, full.Ceiling);
        Assert.Equal(0, wrongKind.Ceiling);
        Assert.NotEqual(full.Note, wrongKind.Note);
        Assert.Contains($"{SentryBot.MaxMagazine}", full.Note, StringComparison.Ordinal);
        Assert.Contains(Ammunition.Issue.Name, wrongKind.Note, StringComparison.Ordinal);

        // Pressed, the refusal is SAID — in the dialog, where the captain is looking (#680) — and nothing
        // moved.
        PressRow(map, TheHandlerOn(TheChooser(), "gun"), wrongKind);
        Assert.Equal(wrongKind.Note, WhatWasSaid(map));
        Assert.Equal([$"{Satchel.Kind.Rounds}:{Ammunition.LabTwoStage.Id}:6"], ThePocket(map));
    }

    // ── (d) NO SECOND ARITHMETIC ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #837 · THE CHOOSER DECIDES NOTHING. It is a page: rows, an order and words. Every figure on it is
    /// asked of Core, and the markup may never grow a hand of its own — the day it moves a round or formats
    /// a count, the two grips are two features wearing one name.
    /// </summary>
    [Fact]
    public void THE_CHOOSER_IsAPageAndNotASecondArithmetic()
    {
        string page = TheChooser();

        foreach (string hand in new[]
        {
            "Satchel.Remove", "Satchel.Add", "gun.Rounds", ".Magazine", "MaxMagazine",
            "RequestVaultSave", "disabled=", "/99",
        })
        {
            Assert.False(page.Contains(hand, StringComparison.Ordinal),
                $"the load chooser's markup contains `{hand}` — the page has started deciding things, and "
                + "the two grips have begun to be two features.");
        }

        // The words are Core's, every one of them.
        Assert.Contains("SentryHandLoad.ChooserBlurb(", page, StringComparison.Ordinal);
        Assert.Contains("SentryHandLoad.NothingInReachLine", page, StringComparison.Ordinal);
        Assert.Contains("SentryHandLoad.LoadLabel", page, StringComparison.Ordinal);
        Assert.Contains("SentryHandLoad.BackLabel", page, StringComparison.Ordinal);

        // …and the counts are the readout's own function and not a second format string, anywhere in the
        // client. Two places print a magazine into a sentence: the instrument, and this chooser. They are
        // the same call.
        string handLoad = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.HandLoad.cs"));
        Assert.DoesNotContain("MaxMagazine", handLoad, StringComparison.Ordinal);
        Assert.DoesNotContain("Readout(", handLoad, StringComparison.Ordinal);
    }

    /// <summary>#837 · BOTH GRIPS ASK THE SAME REACH QUESTION, and it is Core's. A client that spelled the
    /// rule out at two call sites would have a sling that is loadable from one door and not the other, which
    /// is the bug the owner filed wearing a different hat.</summary>
    [Fact]
    public void THE_REACH_RuleIsAskedInOnePlace()
    {
        string surface = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Surface.Darkroom.cs"));
        string handLoad = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.HandLoad.cs"));

        Assert.Contains("SentryHandLoad.WithinHands(", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("SentryHandLoad.WithinHands(", handLoad, StringComparison.Ordinal);
        Assert.Contains("WithinHandsOf(gun)", handLoad, StringComparison.Ordinal);

        // …and the act itself asks it, rather than trusting whichever page sent the press.
        int at = surface.IndexOf("private SatchelTry.Outcome TheRoundsGoInByHand(", StringComparison.Ordinal);
        Assert.True(at > 0, "the act has moved and this guard can no longer read it.");
        string act = surface[at..surface.IndexOf("\n    }", at, StringComparison.Ordinal)];
        Assert.Contains("WithinHandsOf(gun)", act, StringComparison.Ordinal);
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A world with a captain at the origin, some guns on the ground and one thing in the pocket.
    /// Nothing is faked: the bots are the shipping type, the sleeve is the shipping list, and the satchel is
    /// OPEN because it is open in play for both grips.</summary>
    private static Pages.Map Bench(IReadOnlyList<Gun> guns, Satchel.Item pocket)
    {
        var map = new Pages.Map();

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type botType = typeof(Pages.Map).GetNestedType("SurfaceBot", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;

        var bots = (System.Collections.IList)exType.GetProperty("Bots")!.GetValue(ex)!;
        foreach (Gun gun in guns)
        {
            object bot = Activator.CreateInstance(botType, nonPublic: true)!;
            SetProp(bot, "Unit", gun.Unit);
            SetProp(bot, "Rounds", gun.Rounds);
            SetProp(bot, "Deployed", gun.Deployed);
            SetProp(bot, "X", gun.X);
            SetProp(bot, "Y", gun.Y);
            SetProp(bot, "AmmoId", gun.Ammo);
            bots.Add(bot);
        }

        Set(map, "_surface", ex);
        Set(map, "_satchel", new List<Satchel.Item> { pocket });
        Set(map, "_avatarX", 0.0);
        Set(map, "_avatarY", 0.0);
        Set(map, "_showSatchel", true);
        return map;
    }

    /// <summary>The instrument's own line for this world — the shipping projection and the shipping
    /// composer, so what is asserted is what a captain reads off the HUD.</summary>
    private static string TheMagazinesLine(Pages.Map map) =>
        SentryBot.MagazinesReadout((IReadOnlyList<SentryBot.Carried>)
            typeof(Pages.Map).GetMethod("TheSlingAsTheInstrumentReadsIt", Hidden | BindingFlags.Static)!
                .Invoke(null, [Get(map, "_surface")])!);

    private static bool TheChooserIsOpen(Pages.Map map) =>
        typeof(Pages.Map).GetMethod("TheLoadChooser", Hidden)!.Invoke(map, null) is not null;

    private static List<SentryHandLoad.Pick> WhatTheChooserOffers(Pages.Map map)
    {
        object? open = typeof(Pages.Map).GetMethod("TheLoadChooser", Hidden)!.Invoke(map, null);
        return open is null ? [] : (List<SentryHandLoad.Pick>)open.GetType().GetField("Item4")!.GetValue(open)!;
    }

    private static void Nudge(Pages.Map map, SentryHandLoad.Pick pick, int by) =>
        typeof(Pages.Map).GetMethod("NudgeShare", Hidden)!.Invoke(map, [pick, by]);

    private static void Press(Pages.Map map, string handler, Satchel.Item item)
    {
        MethodInfo? act = typeof(Pages.Map).GetMethod(handler, Hidden);
        Assert.True(act is not null, $"the razor presses `{handler}`, which the component does not have.");
        act!.Invoke(map, [item]);
    }

    private static void Open(Pages.Map map, string handler, Satchel.Item item) => Press(map, handler, item);

    private static void PressRow(Pages.Map map, string handler, SentryHandLoad.Pick pick)
    {
        MethodInfo? act = typeof(Pages.Map).GetMethod(handler, Hidden);
        Assert.True(act is not null, $"the chooser presses `{handler}`, which the component does not have.");
        act!.Invoke(map, [pick]);
    }

    /// <summary>Every drum on this ground, as a comparable line — unit, rounds and what is in it. The three
    /// facts a hand-load may move and the only three.</summary>
    private static IReadOnlyList<string> TheDrums(Pages.Map map)
    {
        var ex = Get(map, "_surface")!;
        var bots = (System.Collections.IEnumerable)ex.GetType().GetProperty("Bots")!.GetValue(ex)!;
        var said = new List<string>();
        foreach (object bot in bots)
        {
            said.Add(
                $"{GetProp(bot, "Unit")} {GetProp(bot, "Rounds")} {GetProp(bot, "AmmoId")}");
        }
        return said;
    }

    private static IReadOnlyList<string> ThePocket(Pages.Map map) =>
        [.. ((List<Satchel.Item>)Get(map, "_satchel")!).Select(i => $"{i.Kind}:{i.Id}:{i.Count}")];

    /// <summary>What the captain was told — wherever the grip put it. The row's press closes the satchel and
    /// pulses; the chooser stays open and says it into the dialog (#680). Two destinations, and the law is
    /// about the SENTENCE.</summary>
    private static string? WhatWasSaid(Pages.Map map) =>
        (string?)Get(map, "_satchelOutcome") ?? ((PulseSlot)Get(map, "_pulse")!).Message;

    // ── THE GRIPS, READ OFF THE SHIPPING MARKUP ───────────────────────────────────────────────────────

    /// <summary>The method a button is really wired to. Read from the razor rather than typed here, which is
    /// the whole point of this file: a grip rewired to a second implementation is still the grip this test
    /// presses, so the comparison above cannot be got round by adding a method.</summary>
    private static string TheHandlerOn(string markup, string rowVariable)
    {
        Match m = Regex.Match(markup, @"@onclick=""\(\) => (\w+)\(" + rowVariable + @"\)""");
        Assert.True(m.Success, $"no press on a `{rowVariable}` row in this markup — the grip has moved.");
        return m.Groups[1].Value;
    }

    /// <summary>…and the OTHER control on the same row: the one that opens the chooser. Matched on the
    /// control that carries Core's own load label, so it cannot be confused with the row's try.</summary>
    private static string TheOpenerOn(string markup)
    {
        Match m = Regex.Match(
            markup,
            @"@onclick=""\(\) => (\w+)\(item\)"">@SpaceSails\.Core\.SentryHandLoad\.LoadLabel");
        Assert.True(m.Success, "the rounds row has no control wearing SentryHandLoad.LoadLabel (#837).");
        return m.Groups[1].Value;
    }

    /// <summary>#837's page, sliced whole.</summary>
    private static string TheChooser()
    {
        string razor = Razor();
        int at = razor.IndexOf("@if (TheLoadChooser() is { } loading)", StringComparison.Ordinal);
        Assert.True(at > 0, "the satchel has no load chooser.");
        int end = razor.IndexOf("\n                else if (_satchel.Count == 0)", at, StringComparison.Ordinal);
        Assert.True(end > at, "the load chooser has no end this guard can find.");
        return razor[at..end];
    }

    /// <summary>…and the satchel row itself, which carries both grips: the try that #803 shipped, and the
    /// load → this issue adds.</summary>
    private static string TheRoundsRow()
    {
        string razor = Razor();
        int at = razor.IndexOf("RenderFragment<SpaceSails.Core.Satchel.Item> Row = item =>", StringComparison.Ordinal);
        Assert.True(at > 0, "the satchel has lost the row fragment this guard reads.");
        int end = razor.IndexOf("\n                                 </div>;", at, StringComparison.Ordinal);
        Assert.True(end > at, "the satchel row has no end this guard can find.");
        return razor[at..end];
    }

    private static string Razor() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"));

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static object? Get(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? GetProp(object o, string property) =>
        o.GetType().GetProperty(property)!.GetValue(o);

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
