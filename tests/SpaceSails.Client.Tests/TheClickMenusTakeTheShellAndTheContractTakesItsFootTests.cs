using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 wave 10 · <b>THE CLICK MENUS, THE STRANGER'S CONTRACT, AND A DOOR ONTO THE DOSSIER.</b>
///
/// <para>#1010 recommended the four <c>.map-body-menu</c> surfaces next and asked a PRE-QUESTION before
/// anybody assumed the answer: <i>does the shell fit a list-shaped menu at all?</i> §1 answers it, with the
/// evidence, and the answer is yes without a new mode — because the premise of the question turns out to be
/// wrong in an interesting way. A menu's rows are not its ways out. They are the actions the click MEANT.
/// What ends a menu is the ✕ that has sat in its head row since M24, and a head with a name on the left and
/// a way out on the right is the shell's oldest shape, drawn by <c>OverlayFrame.Card</c>.</para>
///
/// <para><b>The finding worth writing down is the one that could have gone the other way.</b> Pressed, every
/// row of every one of these four menus DOES end its menu — measured here rather than assumed. So the
/// critical-decision exception's own behaviour test would pass on this family, and a migration that reached
/// for <c>ByDecision</c> would have been green. It would also have been wrong, and the guard below says why
/// in the failure message: <c>ByDecision</c> means no ✕ is NEEDED, and the shell then draws none, which
/// would leave a captain who opened a menu by accident with no way to shut it except to set a destination,
/// arm an insertion or spend a telescope pass. A menu is not a question. It is a list of things you MAY do,
/// and "none of them" has to stay sayable.</para>
///
/// <para>§2 is the stranger's contract, which #1009 named a straggler and #1010 said was blocked on a layout
/// question #780 had already answered. It had — and the answer transfers exactly: #355's drink flow moves
/// ABOVE the pinned foot, for the same reason the counter's six priced items did.</para>
///
/// <para>§3 is <c>?target=</c>, the dev door onto #960's dossier. Three waves of this migration measured
/// that card by hand because no URL could raise it; this is the URL.</para>
/// </summary>
[SlowGate] // #251 · 71 s over 13 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheClickMenusTakeTheShellAndTheContractTakesItsFootTests
{
    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  1 · The four click menus
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EVERY CLICK MENU IS DRAWN THROUGH THE SHELL, read off the markup as typed.
    ///
    /// <para>The whole family is one root class and one shape, so unlike the deck cards there is no
    /// straggler list here to hold a reason: either all four are the shell's or the wave did not finish. It
    /// fails the other way too — a count that dropped would mean a menu had quietly left the client.</para>
    /// </summary>
    [Fact]
    public void EveryClickMenuIsDrawnThroughTheShellAndNoneIsHandRolled()
    {
        string map = MapMarkup.Read(Path.Combine(ClientSource(), "Pages", "Map.razor"));
        string[] lines = map.Split('\n');

        var handRolled = new List<string>();
        int menus = 0;
        foreach (Match found in Regex.Matches(map, "class=\"(?<list>[^\"]*\\bmap-body-menu\\b[^\"]*)\""))
        {
            menus++;
            int line = map.Take(found.Index).Count(c => c == '\n');
            int column = found.Index - (line == 0 ? 0 : map.LastIndexOf('\n', found.Index - 1) + 1);
            string tag = TagOwning(lines, line, column);
            if (!string.Equals(tag, "OverlayShell", StringComparison.Ordinal))
            {
                handRolled.Add($"Map.razor:{line + 1} <{tag} class=\"{found.Groups["list"].Value}\">");
            }
        }

        Assert.True(menus == 4,
            $"{menus} surfaces wear `.map-body-menu` and this family is four: the pick-candidate chooser, "
            + "the body menu, the contact menu and the open-sky menu. A fifth has joined without a guard, "
            + "or one has left without anybody noticing.");

        Assert.True(handRolled.Count == 0,
            $"{handRolled.Count} click menu(s) are NOT drawn through OverlayShell:\n  - "
            + string.Join("\n  - ", handRolled)
            + "\n\nThis family is one root, one head and one way out — the shell's oldest shape (Card · "
            + "Close). A hand-rolled one keeps its own head row, its own ✕ and its own wiring, which is the "
            + "duplication #992 counted and #997 exists to end.");
    }

    /// <summary>
    /// THE PRE-QUESTION'S ANSWER, PINNED — <b>every row ends the menu, and the ✕ is still drawn.</b>
    ///
    /// <para>#1010 asked whether the shell fits a menu whose ways out are LISTS. This is the measurement
    /// that answers it, and it is the one that could have gone the other way: every row of this menu is
    /// pressed, in turn, from a freshly re-raised menu, and every one of them takes the menu off the screen
    /// (🎯 Set destination and 🚀 Long haul clear the gate; 🛰 arm, ❄ the cycler and 🪂 aerobrake close it
    /// on the way in to their own verb; 🔭 scan closes all three menus at once). So on BEHAVIOUR alone this
    /// family satisfies the critical-decision exception.</para>
    ///
    /// <para>And it is a <c>Close</c> anyway, which is the ruling this guard holds. The second half asserts
    /// the ✕ is still there and is still the shell's — because a <c>ByDecision</c> shell draws no dismiss at
    /// all, and the difference between the two shapes is exactly whether a captain who opened this menu by
    /// accident can shut it without spending something.</para>
    /// </summary>
    [Fact]
    public async Task EveryRowOfAClickMenuEndsItAndTheWayOutIsStillDrawnAnyway()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        await bench.SwitchAsync(ShipDesk.Nav);

        DeskBench.Painted.Node menu = await RaiseTheBodyMenu(bench);

        // The rows, named as a player reads them — everything in the menu that is not the shell's own ✕.
        var rows = menu.Descendants()
            .Where(n => n.Handlers.ContainsKey("onclick") && !n.Hidden && !n.HasClass("overlay-shell-dismiss"))
            .Select(n => n.Name)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(rows.Count > 0,
            "the body menu drew no action rows at all, so this guard would be measuring an empty list — the "
            + "fifth named bug class, asking the wrong world about the right question.");

        var standing = new List<string>();
        foreach (string row in rows)
        {
            DeskBench.Painted.Node again = await RaiseTheBodyMenu(bench);
            DeskBench.Painted.Node? button = again.Descendants()
                .FirstOrDefault(n => !n.Hidden && n.Handlers.ContainsKey("onclick")
                                     && string.Equals(n.Name, row, StringComparison.Ordinal));
            Assert.NotNull(button);

            await bench.PressAsync(button!.Handlers["onclick"]);
            if (TheMenu(await bench.RenderAsync()) is not null)
            {
                standing.Add(row);
            }
        }

        Assert.True(standing.Count == 0,
            $"{standing.Count} of {rows.Count} rows left the body menu standing — [{string.Join(" · ", standing)}]"
            + ".\n\nThat is a finding rather than a failure and this guard is where it is written down: the "
            + "family's shape was chosen on the basis that EVERY row ends its menu. If that has stopped "
            + "being true, the ruling below still holds (it is about the ✕, not about the rows) but this "
            + "sentence in Map.razor no longer does, and one of the two must move.");

        // …AND THE ✕ IS STILL THERE, which is the ruling. A ByDecision shell draws no dismiss at all.
        DeskBench.Painted.Node last = await RaiseTheBodyMenu(bench);
        DeskBench.Painted.Node cross = last.Descendants()
            .FirstOrDefault(n => n.HasClass("overlay-shell-dismiss") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                "the body menu has no shell-drawn way out. Every row of it ends the menu, so the "
                + "critical-decision exception's behaviour test would pass here — and taking the ✕ away on "
                + "that basis is exactly what this guard exists to stop. ByDecision means \"no ✕ is needed "
                + "because every ANSWER is a close\"; these are not answers, they are things the captain "
                + "may do, and a menu opened by accident must be shuttable without setting a destination, "
                + "arming an insertion or spending a telescope pass.");

        await bench.PressAsync(cross.Handlers["onclick"]);
        Assert.Null(TheMenu(await bench.RenderAsync()));
    }

    /// <summary>
    /// EACH MENU IS A CARD WHOSE HEAD IS THE SHELL'S, WEARING THE PAGE'S OWN DRESS, AND ITS ✕ ENDS IT.
    ///
    /// <para>Four surfaces, one theory. Each is raised with its three siblings put DOWN first: they share
    /// <c>.map-body-menu</c>, and a guard that read "the first one on the screen" would report one menu
    /// under another one's name — this repository's first named bug class, in a test.</para>
    ///
    /// <para>What is asserted is the migration's whole promise: the root still wears the page's class list
    /// (so <c>::deep .map-body-menu</c>'s <c>position: absolute</c> and its band still reach it), the head
    /// is the shell's <c>.overlay-shell-head</c> wearing the page's own <c>d-flex … mb-1</c>, the title is
    /// the string it was, the ✕ is the SHELL's and still dressed <c>btn-close btn-close-white btn-sm</c>
    /// with the tooltip it carried — and pressing it takes the menu away.</para>
    /// </summary>
    [Theory]
    [InlineData("the pick-candidate chooser", "Which one?", "Close this chooser", "text-secondary me-2")]
    [InlineData("the body menu", "Luna", "Close this body's menu", "fw-bold me-2")]
    [InlineData("the contact menu", null, "Close this contact's menu", "fw-bold me-2")]
    [InlineData("the open-sky menu", "✨ Open sky", "Close this patch's menu", "fw-bold me-2")]
    public async Task EachClickMenuIsAShellCardWhoseCrossEndsIt(
        string what, string? title, string tooltip, string titleDress)
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        DeskBench.Painted.Node menu = await Raise(bench, what);

        Assert.True(menu.HasClass("overlay-shell"), $"{what}'s root is not a shell at all.");
        Assert.True(menu.HasClass("overlay-shell-card"),
            $"{what} is not the shell's Card frame. Its head — a name on the left, the way out on the "
            + "right — IS the Card frame, and a Bare one would leave the ✕ loose at the end of the rows.");
        Assert.True(menu.HasClass("map-body-menu"),
            $"{what} has lost the family's class. That class is where the menu's `position: absolute`, its "
            + "12–16rem width and its z-band live (#253, #299), so without it the menu falls into the "
            + "document flow at the top-left of the page and ignores the inline anchor it is still given.");

        DeskBench.Painted.Node head = menu.Children.FirstOrDefault(n => n.HasClass("overlay-shell-head") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException($"{what} has no shell head row.");
        Assert.True(head.HasClass("mb-1"),
            $"{what}'s head has lost the page's own `mb-1`. That quarter-rem is the whole gap between the "
            + "name and the first action row, and it is the page's opinion rather than the shell's.");

        DeskBench.Painted.Node name = head.Descendants().First(n => n.HasClass("overlay-shell-title"));
        foreach (string dress in titleDress.Split(' '))
        {
            Assert.True(name.HasClass(dress), $"{what}'s title has lost `{dress}`.");
        }

        if (title is not null)
        {
            Assert.Contains(title, name.Spoken, StringComparison.Ordinal);
        }

        DeskBench.Painted.Node cross = head.Descendants()
            .FirstOrDefault(n => n.HasClass("overlay-shell-dismiss") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                $"{what}'s ✕ is not the shell's. It is the one control on this surface that ends it without "
                + "doing anything to the ship, and the point of the migration is that it is now the "
                + "mechanism's button rather than a fourth hand-typed copy of one.");

        Assert.True(cross.HasClass("btn-close") && cross.HasClass("btn-close-white") && cross.HasClass("btn-sm"),
            $"{what}'s ✕ has lost its dress — it is Bootstrap's own glyph plate and nothing else draws it.");
        Assert.Equal(tooltip, cross.Attributes.GetValueOrDefault("title"));

        await bench.PressAsync(cross.Handlers["onclick"]);
        Assert.Null(TheMenu(await bench.RenderAsync()));
    }

    /// <summary>
    /// AND THE RULE THAT USED TO REACH THE ROOT FOLLOWED IT.
    ///
    /// <para>#996's bug class, asked by name of the one element this wave newly handed to the shell. The
    /// general form is guarded next door (<c>EveryRuleWhoseTargetTheShellDrawsIsWrittenWithDeep</c>); this
    /// is the one selector with the sentence about what losing it would cost, so the day it is reverted the
    /// failure says what was lost rather than only that a selector moved.</para>
    /// </summary>
    [Fact]
    public void TheRuleForTheMenusRootIsWrittenWithDeep()
    {
        string css = WithoutComments(File.ReadAllText(
            Path.Combine(ClientSource(), "Pages", "Map.razor.css")));

        var reaching = Selectors(css)
            .Where(one => one.TrimEnd().EndsWith(".map-body-menu", StringComparison.Ordinal))
            .ToList();

        Assert.True(reaching.Count > 0,
            "no rule in Map.razor.css targets `.map-body-menu` at all — that rule carries the family's "
            + "`position: absolute`, its width and its z-band, and a family that has lost it is worth "
            + "knowing about even if nothing here is dead.");

        var bare = reaching.Where(one => !one.TrimStart().StartsWith("::deep", StringComparison.Ordinal)).ToList();

        Assert.True(bare.Count == 0,
            $"{bare.Count} rule(s) target `.map-body-menu` without `::deep`:\n  - {string.Join("\n  - ", bare)}"
            + "\n\nOverlayShell draws this root now, so it carries the SHELL's scope attribute and not the "
            + "page's: each of these compiles to `.map-body-menu[b-map]` and matches nothing — present, "
            + "correct and dead (#996). The menu then loses `position: absolute` and falls into the "
            + "document flow at the top-left of the page, wearing an inline left/top it no longer obeys; it "
            + "also loses the z-band, which is the half CssZBandSyncTests would stay green over, because "
            + "that gate parses this file for the declaration and never asks whether the selector matches.");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  2 · The stranger's contract
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// BOTH ANSWERS ARE THE SHELL'S, THEY STAND IN THE ROW THE PAGE NAMED, AND EACH OF THEM ENDS THE CARD.
    ///
    /// <para>#1009 left this card on the straggler list because <c>ByDecision</c> would have been a false
    /// claim — the card also carries #355's drink flow, whose controls leave it standing. The shape it
    /// wanted was a <c>Close</c> with TWO ways out: wave 7's <c>WaysClass</c> for the row and wave 7's
    /// <c>OnBeside</c> for <i>Pass</i>. Each answer is pressed on its own run, from a freshly re-raised
    /// card, so one press cannot be mistaken for another's.</para>
    /// </summary>
    [Theory]
    [InlineData("Take the job", "overlay-shell-dismiss", "btn-warning")]
    [InlineData("Pass", "overlay-shell-beside", "btn-outline-light")]
    public async Task TheContractsAnswersAreTheShellsAndEachOfThemEndsTheCard(
        string face, string mechanism, string dress)
    {
        using DeskBench bench = await DeskBench.BootAsync(Ashore);
        DeskBench.Painted.Node card = await RaiseTheContract(bench);

        DeskBench.Painted.Node foot = card.Children
            .FirstOrDefault(n => n.HasClass("deck-offer-actions") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                "the contract's answers are not in a `.deck-offer-actions` row that is a DIRECT child of "
                + "the card. #735 pins that row sticky with a 12rem scrim through "
                + "`.deck-offer-card > .deck-offer-actions`, and anything that wraps it unpins it.");

        Assert.True(foot.HasClass("overlay-shell-ways"),
            "the contract's foot is not the row the SHELL draws. Both controls on it are ways out, which is "
            + "what WaysClass is for; FootHost is the other half of the mechanism and is for a foot whose "
            + "other controls are NOT ways out (the logbook's ⬇ Export, the oracle's 🌀 Keep listening).");

        DeskBench.Painted.Node answer = foot.Descendants()
            .FirstOrDefault(n => !n.Hidden && string.Equals(n.Name, face, StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException($"the contract no longer offers \"{face}\".");

        Assert.True(answer.HasClass(mechanism),
            $"\"{face}\" is not the shell's {mechanism} — it is a hand-typed button standing in the shell's "
            + "own row, which is the duplication this migration removes.");
        Assert.True(answer.HasClass(dress),
            $"\"{face}\" has lost `{dress}`. Not one word or colour moved in this migration: the warning "
            + "fill is what makes taking the job the card's primary, and the outline is what makes passing "
            + "the quiet one.");

        await bench.PressAsync(answer.Handlers["onclick"]);
        Assert.True(TheCard(await bench.RenderAsync(), "deck-offer-card") is null,
            $"\"{face}\" was pressed and the stranger's contract is still on the table. Both of its answers "
            + "end it — that is why it is a Close with two ways out rather than a card with a ✕.");
    }

    /// <summary>
    /// #780'S ANSWER, TRANSFERRED: THE FOOT IS LAST AND THE DRINK FLOW IS ABOVE IT.
    ///
    /// <para>The layout question #1010 left open, and the reason this card could not simply be migrated.
    /// <c>.deck-offer-actions</c> is pinned <c>position: sticky; bottom: 0</c> with a 12 rem scrim, and
    /// #355's drink flow used to render AFTER it — the identical shape #780 was filed about at the counter,
    /// where the menu's six priced items slid under that scrim and read as a greyed-out panel behind glass.
    /// #780's answer was to put the flow above the row, and that is what the shell's <c>WaysClass</c>
    /// requires anyway (the row it draws is a Bare surface's last child), so the fix and the migration are
    /// one edit.</para>
    ///
    /// <para>Two halves, and both are needed: the tree says the foot is the card's LAST child, and the
    /// source says the drink flow is still inside the card. Either alone would pass over a flow that had
    /// been deleted or hoisted out of the card altogether.</para>
    /// </summary>
    [Fact]
    public async Task TheContractsFootIsLastAndTheDrinkFlowStandsAboveIt()
    {
        using DeskBench bench = await DeskBench.BootAsync(Ashore);
        DeskBench.Painted.Node card = await RaiseTheContract(bench);

        DeskBench.Painted.Node last = card.Children.Last(n => !n.Hidden);
        Assert.True(last.HasClass("deck-offer-actions"),
            "the stranger's contract does not end with its action row. #735 pins that row sticky at the "
            + "card's bottom with a 12rem scrim, so anything drawn after it is drawn UNDER the scrim — "
            + "which is the bug #780 was filed about, at the counter, one card along.");

        string card_source = TheContractAsTyped();
        Assert.Contains("ContactDrinkOffer(offer.Giver", card_source, StringComparison.Ordinal);
        Assert.DoesNotContain("<div class=\"deck-offer-actions\">", card_source, StringComparison.Ordinal);
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  3 · The door onto the dossier
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <c>?target=collector</c> BOOTS WITH HER FILE OPEN, AND IT CAN BE PUT DOWN.
    ///
    /// <para>The whole point of the cheat, asked of a real boot rather than of the method: the URL alone —
    /// no poke, no field write — must leave the dossier on the glass at the desk that draws it, and the
    /// card must be the COLLECTOR's (the fullest file the game has, #962's terms) rather than a stub.</para>
    ///
    /// <para><b>Free-flying, and a browser walk is why.</b> The first cut of this cheat's own dev start was
    /// <c>&amp;dock=selene-gate</c> and it passed here — this bench runs no sim ticks at all. In a real
    /// Chrome the dossier came up and was GONE a second later, which is the game being right rather than
    /// the cheat being wrong: a haven is exactly where a collector loses the scent (#580), so she breaks
    /// off, leaves <c>_hunters</c>, and <c>DossierFor</c> has nothing to draw. The world moved here; the
    /// warning the cheat now prints for the docked case is pinned next door.</para>
    /// </summary>
    [Fact]
    public async Task TheTargetCheatBootsWithACollectorsDossierOnTheGlass()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying + "&target=collector");

        Assert.True(bench.ActiveDesk is ShipDesk.Nav or ShipDesk.Sensors,
            $"?target= left the captain at the {bench.ActiveDesk} desk, where the dossier is not drawn at "
            + "all. A cheat that points the tactical UI at a contact and then stands the captain somewhere "
            + "he cannot see her is a cheat that did nothing.");

        DeskBench.Painted.Node file = TheCard(await bench.RenderAsync(), "map-dossier")
            ?? throw new Xunit.Sdk.XunitException(
                "?target=collector booted no dossier. Either no muscle was sent (SpawnHunterForHeatEvent "
                + "found nothing policed within reach of the wreck) or DossierFor has stopped answering "
                + "for a hunter — and the second is #962's own bug, which is that a hunter is never in "
                + "_npcStates.");

        Assert.Contains("hunting US", Spoken(file), StringComparison.Ordinal);

        DeskBench.Painted.Node cross = file.Descendants()
            .FirstOrDefault(n => n.HasClass("overlay-shell-close") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException("the dossier the cheat raised carries no ✕.");

        await bench.PressAsync(cross.Handlers["onclick"]);
        Assert.Null(TheCard(await bench.RenderAsync(), "map-dossier"));
    }

    /// <summary>
    /// …AND AT A BERTH IT SAYS THE FILE WILL NOT STAY, WHICH ONLY A BROWSER COULD HAVE TAUGHT.
    ///
    /// <para>The cheat's first dev start was <c>?dock=selene-gate&amp;target=collector</c>, and it was green
    /// on this bench. In a real Chrome the dossier came up and then vanished: a haven is where a collector
    /// loses the scent (#580 / <c>EncounterRule.ApplyBreakOff</c>), so within a tick or two she breaks off,
    /// leaves <c>_hunters</c>, and <c>DossierFor</c> has nothing left to draw. The bench could not see it
    /// because the bench runs NO sim ticks — its own documented horizon, and the honest reading of a green
    /// run here.</para>
    ///
    /// <para>The answer was not to weaken the game. It was to move the dev start free-flying and to have
    /// the cheat SAY so at a berth, and this guard holds the saying — because a warning that goes missing
    /// is a playtester back where wave 10 started: watching a card disappear and not knowing that was the
    /// rules working.</para>
    /// </summary>
    [Fact]
    public async Task TheTargetCheatWarnsThatAHavenIsWhereACollectorLosesYou()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked + "&target=collector");
        Assert.Contains("berthed at a HAVEN", bench.Pulse, StringComparison.Ordinal);
        Assert.Contains("start=wreck&target=collector", bench.Pulse, StringComparison.Ordinal);

        // …and it is NOT said where it would be false. The wreck is nobody's harbour.
        using DeskBench adrift = await DeskBench.BootAsync(FreeFlying + "&target=collector");
        Assert.DoesNotContain("berthed at a HAVEN", adrift.Pulse, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE CHEAT WALKS BOTH ROSTERS, AND SAYS SO WHEN NOTHING ANSWERS.
    ///
    /// <para>#962's bug was that <c>FindNpc</c> walks <c>_npcStates</c> only, so every collector id fell out
    /// of the first guard and 📡 <i>sharpen fix</i> did nothing, in silence. The cheat is written not to
    /// repeat it: a hunter id resolves, a traffic id resolves, and an id that is neither answers OUT LOUD
    /// with what this sky does hold rather than booting to a blank map.</para>
    /// </summary>
    [Fact]
    public async Task TheTargetCheatFindsTrafficAndNamesWhatItCannotFind()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        await bench.SwitchAsync(ShipDesk.Nav);
        _ = await bench.RenderAsync();

        string contact = AContactWithAFix(bench);
        DeskBench.Painted.Node? traffic = TheCard(await bench.RenderAsync(), "map-dossier");
        Assert.True(traffic is not null,
            $"?target={contact} pointed the tactical UI at a scheduled contact and no dossier came back. "
            + "DossierFor refuses a contact that has never been observed — which is right — so the cheat "
            + "has to PAY for her with the fix a completed telescope pass would have entered, and that is "
            + "the half that has stopped working.");

        using DeskBench nobody = await DeskBench.BootAsync(Docked + "&target=nothing-out-here");
        Assert.Null(TheCard(await nobody.RenderAsync(), "map-dossier"));
        Assert.Contains("DEV ?target=", nobody.Pulse, StringComparison.Ordinal);
        Assert.Contains("nothing out there answers to that id", nobody.Pulse, StringComparison.Ordinal);
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  Plumbing
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    // Spelled exactly as the dismissibility law spells them, so a surface driven here and the same surface
    // driven there can never be standing in two different places.
    internal const string Docked = "/map?dock=selene-gate&body=luna&site=1";
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";
    internal const string FreeFlying = "/map?start=wreck";

    internal static DeskBench.Painted.Node? TheMenu(DeskBench.Painted painted) =>
        TheCard(painted, "map-body-menu");

    internal static DeskBench.Painted.Node? TheCard(DeskBench.Painted painted, string root) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass(root) && !n.Hidden);

    private static string Spoken(DeskBench.Painted.Node node) =>
        string.Join(" ", node.SelfAndDescendants().Select(n => n.Spoken));

    /// <summary>Raise ONE of the four and put its three siblings down first. They share the family class,
    /// and a guard that read "the first .map-body-menu on the screen" would answer about whichever is
    /// earliest in the tree — one surface reported under another's name, which is this repository's first
    /// named bug class.</summary>
    internal static async Task<DeskBench.Painted.Node> Raise(DeskBench bench, string what)
    {
        bench.Poke("_pickMenu", null);
        bench.Poke("_bodyMenuBody", null);
        bench.Poke("_shipMenuId", null);
        bench.Poke("_skyMenuWorld", null);

        switch (what)
        {
            case "the pick-candidate chooser":
                await bench.SwitchAsync(ShipDesk.Nav);
                bench.Poke("_pickMenu", APickList());
                break;

            case "the body menu":
                await bench.SwitchAsync(ShipDesk.Nav);
                bench.Poke("_bodyMenuBody", ABody(bench, "luna"));
                break;

            case "the contact menu":
                await bench.SwitchAsync(ShipDesk.Nav);
                // Painted once first: the cheat below ends in StateHasChanged, and a component that has
                // never been rendered has no render handle to hand it to.
                _ = await bench.RenderAsync();
                _ = AContactWithAFix(bench);
                bench.Poke("_shipMenuId", bench.Peek("_interestTargetId"));
                break;

            default:
                await bench.SwitchAsync(ShipDesk.Sensors);
                bench.Poke("_skyMenuRadius", 5e9);
                bench.Poke("_skyMenuWorld", (Vector2d?)new Vector2d(2.0e11, 1.0e11));
                break;
        }

        return TheMenu(await bench.RenderAsync())
               ?? throw new Xunit.Sdk.XunitException(
                   $"raising {what} drew nothing wearing `.map-body-menu`. The gate this bench sets and the "
                   + "gate the markup reads have come apart; one of them has moved.");
    }

    private static Task<DeskBench.Painted.Node> RaiseTheBodyMenu(DeskBench bench) =>
        Raise(bench, "the body menu");

    /// <summary>A body the click menu can be about, taken off the world the boot actually built rather than
    /// invented — a menu drawn over a planet that is not in this ephemeris would be a guard asking the
    /// wrong world.</summary>
    private static object ABody(DeskBench bench, string id)
    {
        var sky = (ICelestialEphemeris)bench.Peek("_ephemeris")!;
        return sky.Bodies.FirstOrDefault(b => b.Id == id)
               ?? throw new Xunit.Sdk.XunitException(
                   $"the booted world has no `{id}` to open a menu on.");
    }

    /// <summary>One candidate in the chooser's list, built through the page's OWN private record so a
    /// renamed member fails loudly here instead of quietly raising nothing.</summary>
    private static object APickList()
    {
        FieldInfo declared = typeof(Map).GetField("_pickMenu", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Map has no `_pickMenu` — the chooser's gate has moved.");

        Type list = declared.FieldType;
        Type candidate = list.GetGenericArguments()[0];
        ConstructorInfo made = candidate
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        object one = made.Invoke(made.GetParameters()
            .Select(p => (object?)(p.Name switch
            {
                "Kind" => 'B',
                "Id" => "luna",
                "Label" => "Luna",
                "Icon" => "🌑",
                _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
            }))
            .ToArray());

        object built = Activator.CreateInstance(list)!;
        list.GetMethod("Add")!.Invoke(built, [one]);
        return built;
    }

    /// <summary>A scheduled contact this boot can honestly draw a dossier for, put there by the SHIPPING
    /// cheat — the first of the roster whose departure is already behind us. Hands back her id.</summary>
    private static string AContactWithAFix(DeskBench bench)
    {
        var roster = (Array)bench.Peek("_npcStates")!;
        FieldInfo ship = roster.GetType().GetElementType()!
            .GetField("Ship", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "the page's NpcState has no `Ship` — this bench cannot name a contact any more.");

        foreach (object? one in roster)
        {
            string id = ((NpcShip)ship.GetValue(one)!).Id;
            bench.CallOnTheDispatcher("SeedTargetCheat", id);
            if (string.Equals(bench.Peek("_interestTargetId") as string, id, StringComparison.Ordinal))
            {
                return id;
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"not one of this world's {roster.Length} scheduled contacts is in the sky at boot, so there is "
            + "nothing for ?target= to point at. Either the traffic schedule has stopped putting ships "
            + "mid-flight at t=0, or the cheat has stopped bringing them onto their route.");
    }

    /// <summary>The contract on the table, with the family's other cards put down first.</summary>
    private static async Task<DeskBench.Painted.Node> RaiseTheContract(DeskBench bench)
    {
        bench.Poke("_patronDrink", null);
        bench.Poke("_bankSession", null);
        bench.Poke("_barMenu", null);
        bench.Poke("_oracleOpen", false);
        bench.Poke("_deckMode", true);
        bench.Poke("_pendingOffer", AnOffer());

        return TheCard(await bench.RenderAsync(), "deck-offer-card")
               ?? throw new Xunit.Sdk.XunitException(
                   "raising the stranger's contract drew nothing wearing `.deck-offer-card`.");
    }

    private static object AnOffer()
    {
        FieldInfo declared = typeof(Map).GetField("_pendingOffer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Map has no `_pendingOffer` — the contract's gate has moved.");

        Type quest = Nullable.GetUnderlyingType(declared.FieldType) ?? declared.FieldType;
        ConstructorInfo made = quest
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var said = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Id"] = "wave10-contract",
            ["Giver"] = "Madam Coil",
            ["TargetShipId"] = "",
            ["TargetCallsign"] = "",
            ["Title"] = "A quiet delivery",
            ["Blurb"] = "Nothing you would want opened at the far end.",
            ["Reward"] = 640,
        };

        return made.Invoke(made.GetParameters()
            .Select(p => said.TryGetValue(p.Name ?? "", out object? told)
                ? told
                : p.ParameterType.IsValueType && Nullable.GetUnderlyingType(p.ParameterType) is null
                    ? Activator.CreateInstance(p.ParameterType)
                    : null)
            .ToArray());
    }

    /// <summary>The contract's markup as typed, sliced on the one verb only this card has.</summary>
    private static string TheContractAsTyped()
    {
        string map = Regex.Replace(
            MapMarkup.Read(Path.Combine(ClientSource(), "Pages", "Map.razor")),
            @"@\*.*?\*@", " ", RegexOptions.Singleline);

        int verb = map.IndexOf("OnClose=\"AcceptOffer\"", StringComparison.Ordinal);
        Assert.True(verb >= 0, "the stranger's contract no longer takes the job.");
        int start = map.LastIndexOf("<OverlayShell", verb, StringComparison.Ordinal);
        int end = map.IndexOf("</OverlayShell>", verb, StringComparison.Ordinal);
        Assert.True(end > start, "the contract's shell is never closed.");
        return map[start..end];
    }

    // ── Reading the stylesheet and the markup ─────────────────────────────────────────────────────────

    private static IEnumerable<string> Selectors(string css) =>
        Regex.Matches(css, @"([^{}]+)\{[^{}]*\}")
            .SelectMany(rule => rule.Groups[1].Value.Split(','))
            .Select(one => one.Trim())
            .Where(one => one.Length > 0);

    private static string WithoutComments(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", " ", RegexOptions.Singleline);

    /// <summary>The element a <c>class="…"</c> belongs to: the nearest <c>&lt;</c> at or before it, looking
    /// back up the file when the attribute sits on its own line.</summary>
    private static string TagOwning(string[] lines, int line, int column)
    {
        string head = lines[line][..column];
        for (int back = line; back >= 0 && line - back < 8; back--)
        {
            int open = head.LastIndexOf('<');
            if (open >= 0)
            {
                Match named = Regex.Match(head[(open + 1)..], @"^[A-Za-z][\w.]*");
                return named.Success ? named.Value : "?";
            }

            if (back == 0)
            {
                break;
            }

            head = lines[back - 1];
        }

        return "?";
    }

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
