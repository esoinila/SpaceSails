using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 wave 9 · <b>THE DOSSIER'S FILE, AND THE DECK-CARD FAMILY.</b>
///
/// <para>Two things, and they are one wave because #1009 recommended them together as the two shapes left
/// that could be taken without a ruling from the owner.</para>
///
/// <list type="number">
/// <item><b>The dossier takes <see cref="Components.CappedScrollPanel"/> — its second consumer, and the
/// first with a real head.</b> #997/#993 extracted that component for exactly this: a head that keeps its
/// measured height over ONE body that takes what is left and scrolls there. The head here is the SHELL'S
/// (📖 her name and her stamp on the left, the – and the ✕ on the right) and until this wave it scrolled
/// away with everything else, because the only cap the card had was <c>.map-dossier-raised</c>'s
/// <c>max-height … overflow-y: auto</c> on the ROOT. Unstacked it had no cap at all — and this card is
/// anchored to the BOTTOM of the glass and grows UPWARD, so a long dossier ran off the TOP of a short
/// window and took both its ways out with it. That is #735's own softlock on a card #735's list had never
/// been pointed at.</item>
/// <item><b>Seven deck cards take the shell</b> on the standing rules, in three shapes the earlier waves
/// already established: three plain decisions whose every answer is itself a close, one plain
/// <c>Close</c>, two <c>Close</c>es whose way out is the whole of a row the page has a rule for (wave 7's
/// <c>WaysClass</c>), and <c>FootHost</c>'s second consumer.</item>
/// </list>
///
/// <para>The dismissibility law (<see cref="EveryPopUpCanBeDismissedTests"/>) asks whether a surface can be
/// got rid of at all, and it is UNEDITED by this wave — every one of these roots was already in its
/// register or its not-a-pop-up list, and none of them changed identity. What a MIGRATION can break is
/// narrower, and this file asks that instead.</para>
/// </summary>
[SlowGate] // #251 · 55 s over 16 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheDossiersFileScrollsUnderItsHeadAndTheDeckCardsTakeTheShellTests
{
    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  1 · The dossier's file
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE HEAD IS OUTSIDE THE PANEL AND HER FILE IS INSIDE IT.
    ///
    /// <para>Both halves in one guard, because either alone is the change failing quietly: a panel with the
    /// head inside it scrolls the ✕ away exactly as the old card did, and a head outside a panel that holds
    /// nothing is a wrapper that bought nothing. So this reads the tree the shipping page painted and asks
    /// where each of the card's two parts came out.</para>
    ///
    /// <para>It also asks that the body is the panel's OWN scroller — <c>.capped-scroll-body</c>, the
    /// element that carries <c>flex: 1 1 auto; min-height: var(--capped-scroll-floor); overflow-y: auto</c>
    /// — and not merely a div inside the panel. A page that handed the panel a <c>Header</c> and no
    /// <c>ChildContent</c> would draw no body at all (<c>HasContent</c>), and everything would look right
    /// in the markup and scroll nowhere on the screen.</para>
    /// </summary>
    [Fact]
    public async Task TheDossiersHeadIsOutsideTheCappedScrollAndHerFileIsInsideIt()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        ADossierOnTheGlass(bench);

        DeskBench.Painted.Node card = await TheDossier(bench);

        DeskBench.Painted.Node head = card.Children.FirstOrDefault(n => n.HasClass("overlay-shell-head") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                "the dossier has no head row. #960 gave this card BOTH verbs — a – that tucks it and a ✕ "
                + "that ends it — and they live in the shell's head. Without it there is nothing for the "
                + "capped scroll to keep above the fold.");

        DeskBench.Painted.Node panel = card.Descendants().FirstOrDefault(n => n.HasClass("capped-scroll"))
            ?? throw new Xunit.Sdk.XunitException(
                "the dossier's file is not on a CappedScrollPanel. #997 wave 9 made this card the panel's "
                + "SECOND consumer, which is the whole reason #993's arithmetic was lifted out of the "
                + "Plotting panel: the next card that needs a head over a scroller must not get a fresh "
                + "guess at a constant.");

        Assert.DoesNotContain(panel.SelfAndDescendants(), n => n.HasClass("overlay-shell-head"));
        Assert.False(head.SelfAndDescendants().Any(n => n.HasClass("capped-scroll")),
            "the dossier's head has ended up INSIDE the capped scroll. Then the – and the ✕ scroll away "
            + "with her file, which is exactly the state this wave was written to end.");

        DeskBench.Painted.Node body = panel.Children.FirstOrDefault(n => n.HasClass("capped-scroll-body"))
            ?? throw new Xunit.Sdk.XunitException(
                "the panel drew no `.capped-scroll-body`. That element IS the scroll — the panel's own "
                + "overflow is only the backstop — so a panel without one is a head and a cap and nothing "
                + "that gives.");

        Assert.True(body.HasClass("map-dossier-file"),
            "the dossier's body has lost the name the page gave it. It is the handle its own browser gate "
            + "reaches for, and #998's lesson is that a page keeps the names its rules and its guards "
            + "already point at.");

        // …and the thing the card exists to say is IN the part that scrolls, not stranded above it.
        Assert.Contains(body.SelfAndDescendants(),
            n => n.Spoken.Contains("hunting US", StringComparison.Ordinal));

        // …and so is the button row, which is the honest reading of "one body that scrolls": nothing on
        // this card is pinned below the fold, because CappedScrollPanel has a head and no foot. If the
        // owner ever rules that 🎯 interest should stay put, that is a design change with his name on it.
        Assert.Contains(body.SelfAndDescendants(),
            n => n.Handlers.ContainsKey("onclick") && n.Name.Contains("war room", StringComparison.Ordinal));
    }

    /// <summary>
    /// THE TILE ROUND-TRIP IS UNTOUCHED BY THE PANEL.
    ///
    /// <para>#960's minimise is a CLASS SWAP on the shell's root and not an <c>@@if</c> — the M26 fix paid
    /// for that in a dark eyepiece — so the subtree survives being tucked. Dropping a component into the
    /// middle of that subtree is precisely the edit that could break it, by giving Blazor a reason to
    /// rebuild the tree around a new component boundary. So the round trip is walked rather than reasoned
    /// about: tuck it, find the tile, bring it back, and the panel and her file have to be the same
    /// mechanism on the other side.</para>
    ///
    /// <para>And the WORLD must not have moved: the tactical target is the same ship it was. A piece of
    /// chrome that changed what the sim was pointed at would be the migration doing something.</para>
    /// </summary>
    [Fact]
    public async Task TheDossiersTileRoundTripIsUnchangedByTheCappedScroll()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        ADossierOnTheGlass(bench);

        DeskBench.Painted.Node card = await TheDossier(bench);
        DeskBench.Painted.Node tuck = card.Descendants()
            .First(n => n.HasClass("overlay-shell-dismiss") && !n.Hidden);

        await bench.PressAsync(tuck.Handlers["onclick"]);
        DeskBench.Painted tucked = await bench.RenderAsync();

        DeskBench.Painted.Node tile = tucked.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("map-dossier-tile"))
            ?? throw new Xunit.Sdk.XunitException(
                "the – was pressed and no .map-dossier-tile came back. #960's sugarcube tile is the "
                + "owner's own answer to two windows at one anchor.");

        // THE SUBTREE IS STILL THERE, AND THAT IS THE POINT. #963's scope minimises by CLASS and not by
        // `@if` because the M26 fix paid for the alternative in a dark eyepiece — a destroyed element leaves
        // renderer.js holding a stale 2D context. So the tucked card must still be holding its panel and its
        // file, hidden by the shell's own `d-none` on the body rather than gone from the tree.
        DeskBench.Painted.Node hiddenBody = tile.Descendants()
            .FirstOrDefault(n => n.HasClass("overlay-shell-body"))
            ?? throw new Xunit.Sdk.XunitException(
                "the tucked dossier has no body in its tree at all — it was DESTROYED rather than hidden, "
                + "which is the shape the M26 fix was written against.");

        Assert.True(hiddenBody.Hidden,
            "the tucked dossier is still showing its body. The tile is one element and one subtree: the "
            + "open state hides and the button shows, and whatever the file was scrolled to is still there "
            + "when it comes back.");
        Assert.Contains(hiddenBody.Descendants(), n => n.HasClass("capped-scroll-body"));

        DeskBench.Painted.Node open = tile.Descendants()
            .First(n => n.HasClass("overlay-shell-tile-btn") && !n.Hidden);
        await bench.PressAsync(open.Handlers["onclick"]);

        DeskBench.Painted.Node back = await TheDossier(bench);
        Assert.Contains(back.Descendants(), n => n.HasClass("capped-scroll-body") && !n.Hidden);
        Assert.Contains(back.SelfAndDescendants(),
            n => n.Spoken.Contains("hunting US", StringComparison.Ordinal));

        Assert.Equal(HunterId, bench.Peek("_interestTargetId"));
    }

    /// <summary>
    /// THE COLUMN, THE CEILING, AND THE ARITHMETIC THAT IS NOT WRITTEN TWICE.
    ///
    /// <para>Three facts the panel cannot know from inside it, read off the page's own stylesheet: that
    /// this card is a flex column, how tall it may be, and which of its two children is the head. The
    /// FOURTH assertion is the one that makes the other three worth having — <c>.overlay-shell-body</c>
    /// must be <c>display: contents</c> and must NOT carry #993's arithmetic. A page that wrote
    /// <c>flex: 1 1 auto; min-height: 0; display: flex</c> onto the shell's body would have working pixels
    /// and a second copy of the law CappedScrollPanel exists to keep in one place, which is the shape this
    /// repository has paid for four times.</para>
    ///
    /// <para>The ceiling is asked of the UNSTACKED rule on purpose. <c>.map-dossier-raised</c> has had one
    /// since #960; the bare card never had, and that is the half that let an over-tall dossier walk off the
    /// top of a short window.</para>
    /// </summary>
    [Fact]
    public void TheDossierIsAColumnWithACeilingAndTheArithmeticIsNotSaidTwice()
    {
        string css = WithoutComments(File.ReadAllText(
            Path.Combine(ClientSource(), "Pages", "Map.razor.css")));

        string card = RuleBody(css, "::deep .map-dossier")
            ?? throw new Xunit.Sdk.XunitException("Map.razor.css has no `::deep .map-dossier` rule at all.");

        Assert.True(card.Contains("display: flex", StringComparison.Ordinal)
                    && card.Contains("flex-direction: column", StringComparison.Ordinal),
            "`.map-dossier` is not a flex column. The capped scroll inside it can only shrink if it is a "
            + "flex ITEM of a bounded column — in a block box it has auto height, its own overflow never "
            + "engages, and the card grows exactly as far as its content says.");

        Assert.True(card.Contains("max-height:", StringComparison.Ordinal),
            "`.map-dossier` names no ceiling. This card is anchored `bottom: .75rem` and grows UPWARD, so "
            + "with no cap a long dossier runs off the TOP of the window — taking the head, the – and the "
            + "✕ off the glass. That is #735's softlock, and it is why the unstacked rule needs the cap "
            + "and not only `.map-dossier-raised`.");

        string head = RuleBody(css, "::deep .map-dossier > .overlay-shell-head")
            ?? throw new Xunit.Sdk.XunitException(
                "nothing in Map.razor.css pins the dossier's head. The head is the SHELL's element, so a "
                + "rule reaching it has to be `::deep` — and without `flex: 0 0 auto` it is a flex item "
                + "like any other and gives its height away to the scroller below it.");
        Assert.Contains("flex: 0 0 auto", head, StringComparison.Ordinal);

        string body = RuleBody(css, "::deep .map-dossier > .overlay-shell-body")
            ?? throw new Xunit.Sdk.XunitException(
                "nothing in Map.razor.css says what the dossier's shell body is. It sits between the flex "
                + "column and the capped scroll, and left as an ordinary block it is the box that swallows "
                + "the remainder before the panel can have it.");
        Assert.Contains("display: contents", body, StringComparison.Ordinal);

        foreach (string restated in new[] { "flex: 1 1 auto", "min-height: 0", "overflow-y" })
        {
            Assert.False(body.Contains(restated, StringComparison.Ordinal),
                $"the dossier's shell body says `{restated}`. That is #993's arithmetic written out a "
                + "second time, in a second file, beside a component whose entire reason for existing is "
                + "that the next panel must not get a fresh guess at it. `display: contents` hands the "
                + "column straight to CappedScrollPanel and lets the component own the sums.");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  2 · The deck-card family
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The roots this wave took responsibility for. <c>stand-up-card</c>, <c>oracle-card</c> and
    /// the rest ride <c>.deck-offer-card</c> as modifiers, so naming them here would count one surface
    /// twice — #992's own lesson about <c>rep-backdrop</c>, in this file's idiom.</summary>
    private static readonly string[] TheDeckCardRoots = ["deck-offer-card", "pin-pad", "selfie-offer"];

    /// <summary>
    /// EVERY CARD IN THIS FAMILY IS DRAWN THROUGH THE SHELL, OR NAMED HERE WITH ITS REASON.
    ///
    /// <para>Read as TYPED, in #992's idiom and for its reason: the alias law keeps these roots a lowercase
    /// <c>class="…"</c> attribute precisely so a guard that reads the markup can still see them. Keyed on
    /// the class list rather than a line number (wave 4's lesson), and it fails the other way too — a
    /// straggler that HAS been migrated must leave the list, because a written-down reason for a surface
    /// that has moved on is worse than no reason at all.</para>
    /// </summary>
    [Fact]
    public void EveryDeckCardIsDrawnThroughTheShellOrNamedHereWithItsReason()
    {
        var handRolled = new List<(string Where, string Classes)>();

        foreach (string file in RazorFiles())
        {
            string[] lines = File.ReadAllLines(file);
            string shortName = Path.GetFileName(file);
            bool inComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool opens = line.Contains("@*", StringComparison.Ordinal);
                bool closes = line.Contains("*@", StringComparison.Ordinal);
                bool commented = inComment || opens;
                inComment = inComment ? !closes : (opens && !closes);

                if (commented)
                {
                    continue;
                }

                foreach (Match attribute in Regex.Matches(line, "class=\"([^\"]*)\""))
                {
                    string list = attribute.Groups[1].Value;
                    string[] classes = list.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                    if (!classes.Intersect(TheDeckCardRoots, StringComparer.Ordinal).Any())
                    {
                        continue;
                    }

                    string tag = TagOwning(lines, i, attribute.Index);
                    if (!string.Equals(tag, "OverlayShell", StringComparison.Ordinal))
                    {
                        handRolled.Add(($"{shortName}:{i + 1}  <{tag} class=\"{list}\">", list));
                    }
                }
            }
        }

        var unexplained = handRolled
            .Where(found => !TheNamedStragglers.ContainsKey(Normalised(found.Classes)))
            .Select(found => found.Where)
            .ToList();

        Assert.True(unexplained.Count == 0,
            $"{unexplained.Count} deck card(s) are NOT drawn through OverlayShell:\n  - "
            + string.Join("\n  - ", unexplained)
            + "\n\nGive it a shell (#997 wave 9) — or name it in TheNamedStragglers with the reason, which "
            + "is the edit that makes somebody say why out loud.");

        var stale = TheNamedStragglers.Keys
            .Where(named => !handRolled.Any(found => Normalised(found.Classes) == named))
            .ToList();

        Assert.True(stale.Count == 0,
            $"{stale.Count} straggler(s) are named here and are no longer hand-rolled: "
            + string.Join(" · ", stale)
            + ". Take them off the list — a written-down reason for a surface that has moved on is worse "
            + "than no reason at all.");
    }

    /// <summary>The cards of this family this wave did NOT take, and why. Keyed on the class list exactly
    /// as typed, because under the alias law that list IS the surface's identity.</summary>
    private static readonly IReadOnlyDictionary<string, string> TheNamedStragglers =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["deck-offer-card"] =
                "The BARKEEP'S COUNTER, and the bar's own rumour card behind it, still wear the bare "
                + "family class.\n"
                + "  · #997 wave 10 · THE STRANGER'S CONTRACT CAME OFF THIS LIST, and its reason was "
                + "answered rather than argued away. This entry said the card wanted \"a FootHost and a "
                + "look at whether the drink flow belongs above the foot rather than below it\"; #780 had "
                + "already answered that question at the counter, where the menu's six priced items were "
                + "sliding under `.deck-offer-actions`' sticky 12rem scrim. The drink flow moved above the "
                + "row for the same reason, and once the row is nothing but ways out it wants wave 7's "
                + "`WaysClass` (Take the job as the dismiss, Pass beside it) rather than a `FootHost`, "
                + "which is the mechanism for a foot whose OTHER controls are not ways out.\n"
                + "  · The barkeep's counter is the biggest foot in the client — the house pour, the menu "
                + "toggle, a round for the room, a rumour, two room prices, the stool's own ladder, and "
                + "`Done` last — and #780 moved its menu ABOVE that row after the sticky foot's scrim "
                + "painted over six priced items. `FootHost` would fit the markup, but the card's foot is "
                + "conditional on `keep.SelfService` and on the posture, and it is the one card in this "
                + "family a bar-behaviour change would touch. Named for wave 11 with its reason rather "
                + "than taken cheaply — it is the one card left in this family whose migration is also a "
                + "bar-behaviour question, and those belong to the owner.",
        };

    private static string Normalised(string classList) =>
        string.Join(' ', classList.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// THE PLAIN DECISIONS: NO WAY OUT BUT AN ANSWER, AND EVERY ANSWER IS ONE.
    ///
    /// <para>Each answer pressed on its own run of the theory, and the card has to be gone afterwards. That
    /// is the only honest way to hold a <c>ByDecision</c> — the claim is about BEHAVIOUR, and this
    /// repository has been burned by guards that took such a claim from a list. It is the check that caught
    /// the rep's card (#998) and the collector's demand (#1001) both claiming an exception they had not
    /// earned.</para>
    ///
    /// <para><b>Four of six, and the two that are missing are NAMED rather than faked.</b> Both are the
    /// bench's own horizon, and neither is a doubt about the answer:</para>
    /// <list type="bullet">
    /// <item>the selfie nudge's <i>Take the shot</i> runs <c>RendererInterop.PlayCue</c>, a
    /// <c>[JSImport]</c> and therefore DeskBench's documented browser gate. It throws off a browser AFTER
    /// <c>_selfieOffer</c> is cleared, so the card really is gone and the press really did work — but the
    /// throw escapes the handler and this bench cannot tell that apart from a fault;</item>
    /// <item>the arrival brake's <i>Fire the brake</i> needs a real BRAKE WINDOW and not merely a gate:
    /// <c>FireArrivalBrake</c> returns early when <c>BrakeWindowBody()</c> is null, and that window is
    /// <c>ArrivalBrake.Advance</c>'s own timing verdict on a ship coming in hot — a sim state, which is
    /// the very reason the dismissibility register gives for leaving this card undriven. A bench that
    /// poked the gate and then reported the answer broken would be asking the WRONG WORLD about the right
    /// button, so it is asked in source instead — next door, where it can be answered honestly.</item>
    /// </list>
    /// <para>Both are still asserted DRAWN AND WIRED on every run of the theory, so one of them going
    /// missing fails here rather than nowhere — and neither was walked to in a live browser either, which
    /// is said in the PR rather than glossed. Said out loud, in #1005's idiom.</para>
    /// </summary>
    [Theory]
    [InlineData("the stand-up confirm", "stand-up-card", "Stand", "_seating")]
    [InlineData("the stand-up confirm", "stand-up-card", "Stay", "_seating")]
    [InlineData("the selfie nudge", "selfie-offer", "Not now", "_selfieOffer")]
    [InlineData("the arrival brake", "arrival-brake-card", "Hold", "_brakeGate")]
    public async Task EachPlainDecisionOffersNoWayOutButAnAnswerAndEveryAnswerIsOne(
        string name, string root, string answer, string gate)
    {
        using DeskBench bench = await DeskBench.BootAsync(gate == "_brakeGate" ? FreeFlying : Ashore);
        Raise(bench, gate);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = TheCard(painted, root)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: its gate was raised and nothing wearing .{root} came back. The driver and the "
                + "markup's gate have come apart; one of them has moved.");

        Assert.True(card.HasClass("overlay-shell"),
            $"{name} is on the screen and it is not the shell's — #997 wave 9 put it on OverlayShell.");
        Assert.True(card.HasClass("overlay-shell-bare"),
            $"{name}'s shell drew it as something other than Bare. A Card frame would wrap the card's own "
            + "`.deck-offer-giver` in an overlay-shell-head no rule in Map.razor.css is written against.");

        Assert.DoesNotContain(card.SelfAndDescendants(), n => n.HasClass("overlay-shell-dismiss"));
        Assert.DoesNotContain(card.SelfAndDescendants(), n => n.HasClass("overlay-shell-choice"));

        // Every answer this card is supposed to offer is drawn and wired — including the one this bench
        // cannot press, so its disappearance is caught here rather than nowhere.
        foreach (string offered in TheAnswersOf(root))
        {
            Assert.Contains(card.Descendants(),
                n => !n.Hidden && n.Handlers.ContainsKey("onclick")
                     && n.Name.Contains(offered, StringComparison.Ordinal));
        }

        DeskBench.Painted.Node press = card.Descendants()
            .FirstOrDefault(n => !n.Hidden && n.Handlers.ContainsKey("onclick")
                                 && n.Name.Contains(answer, StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException(
                $"{name} has no control reading \"{answer}\". Its answers ARE the way out — a ByDecision "
                + "surface that has mislaid one of them is a surface with no way out at all.");

        await bench.PressAsync(press.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(TheCard(after, root) is null,
            $"{name}: \"{answer}\" was pressed and the card is still on the screen. It carries no ✕ only "
            + "because every answer it offers was supposed to be one — give it a way out, or make the "
            + "answer end it.");
    }

    /// <summary>
    /// …AND THE ONE ANSWER NO OFF-BROWSER WORLD CAN PRESS, ASKED WHERE IT CAN BE ANSWERED.
    ///
    /// <para>The arrival brake's <i>Fire the brake</i> is the half of its ByDecision claim a poked gate
    /// cannot establish, because firing needs a live brake window. So the claim is held in SOURCE, on the
    /// two facts it actually rests on: the card is drawn on <c>_brakeGate.Asking</c> and on nothing else,
    /// and <c>FireArrivalBrake</c> moves that same gate through <c>ArrivalBrake.Fire</c> — whose phase is
    /// <c>Fired</c>, not <c>Asking</c>. Between them the ask cannot survive its own answer.</para>
    ///
    /// <para>Weaker than a press, and SAID to be weaker, which is the whole of #1005's idiom: a guard that
    /// pretended to press this would be a green number never asked of the world.</para>
    /// </summary>
    [Fact]
    public void TheBrakesFireAnswerEndsTheAskAndTheAskIsTheOnlyGateOnTheCard()
    {
        string razor = File.ReadAllText(Path.Combine(ClientSource(), "Pages", "Map.razor"));
        int card = razor.IndexOf("arrival-brake-card", StringComparison.Ordinal);
        Assert.True(card >= 0, "Map.razor no longer draws an .arrival-brake-card this guard can find.");

        int gate = razor.LastIndexOf("@if (", card, StringComparison.Ordinal);
        string condition = razor[gate..razor.IndexOf(')', gate)];
        Assert.Contains("_brakeGate.Asking", condition, StringComparison.Ordinal);

        string haul = string.Concat(Directory
            .EnumerateFiles(Path.Combine(ClientSource(), "Pages"), "Map.LongHaul*.cs")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText));

        int at = haul.IndexOf("private void FireArrivalBrake()", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.LongHaul no longer has FireArrivalBrake where this guard can read it.");

        int next = haul.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        string body = haul[at..(next > at ? next : haul.Length)];
        Assert.Contains("_brakeGate = ArrivalBrake.Fire(", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE CLOSES: THE WAY OUT IS THE SHELL'S, IT IS WHERE IT WAS, AND PRESSING IT ENDS THE CARD.
    ///
    /// <para>Three questions per card and all three matter separately. It has to be the SHELL's button
    /// (<c>overlay-shell-dismiss</c>) — that is what puts it inside the shape audit and inside the
    /// dismissibility law's reach, and a hand-typed one wearing the same words would satisfy a reader and
    /// nothing else. It has to be WHERE it was and dressed as it was, because this migration moves no
    /// pixels. And it has to WORK, because a way out that does not close is the whole reason this lane
    /// exists.</para>
    ///
    /// <para>The three <c>WaysClass</c>/<c>FootHost</c> cards are asked one question more: their button is
    /// the last control of <c>.deck-offer-actions</c>, which #780 pins sticky with a scrim and #735 lists
    /// among the family's pinned feet. A dismiss that came out as the card's last DIRECT child instead
    /// would be a control on a line of its own below a row it used to sit in.</para>
    /// </summary>
    [Theory]
    [InlineData("the hatch keypad", "pin-pad", "Cancel", "pin-pad-cancel", "", "_pinJob")]
    [InlineData("the patron's table", "deck-offer-card", "Done", "btn-outline-light", "deck-offer-actions",
        "_patronDrink")]
    [InlineData("the favour bank", "deck-bank-card", "Done", "btn-outline-light", "deck-offer-actions",
        "_bankSession")]
    [InlineData("the station oracle", "oracle-card", "Done", "btn-outline-light", "deck-offer-actions",
        "_oracleLine")]
    public async Task EachCloseIsTheShellsAndStandsWhereItStoodAndPressingItEndsTheCard(
        string name, string root, string face, string dress, string row, string gate)
    {
        using DeskBench bench = await DeskBench.BootAsync(Ashore);
        Raise(bench, gate);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = TheCard(painted, root)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: its gate was raised and nothing wearing .{root} came back.");

        Assert.True(card.HasClass("overlay-shell") && card.HasClass("overlay-shell-bare"),
            $"{name} is on the screen and it is not a Bare shell — #997 wave 9 put it on OverlayShell, and "
            + "Bare is the frame whose dismiss lands in the card's own prose rather than in a head row "
            + "this family has never had.");
        Assert.True(card.HasClass(root),
            $"{name} has lost its own class. The shell is the MECHANISM and .{root} is the IDENTITY — "
            + "#992's completeness guard reads that name off the markup as typed.");

        DeskBench.Painted.Node way = card.Descendants()
            .FirstOrDefault(n => n.HasClass("overlay-shell-dismiss") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name} has no way out the shell drew. Then it is a Close with a ✕ nobody draws, and the "
                + "audit that watches for a control wired to nothing cannot see this one.");

        Assert.Equal(face, way.Name);
        Assert.True(way.HasClass(dress),
            $"{name}'s way out has lost `{dress}`. The shell owns the MECHANISM; the page's own "
            + "DismissClass owns how the button looks, and this migration does not repaint a control.");

        if (row.Length > 0)
        {
            // The row the way out is IN, and not the first element on the card wearing that class — #355's
            // drink flow renders a `.deck-offer-actions` of its OWN inside the patron's card, so a guard
            // that took the first one would be reading one row and reporting on another. This repository's
            // first named bug class, and it was this theory's first red.
            DeskBench.Painted.Node foot = card.Descendants()
                .FirstOrDefault(n => n.Children.Any(child => ReferenceEquals(child, way)))
                ?? throw new Xunit.Sdk.XunitException(
                    $"{name}'s way out is not inside any row at all — the Bare shell drew it as the card's "
                    + $"last direct child, one line below the .{row} it has always stood in.");

            Assert.True(foot.HasClass(row),
                $"{name}'s way out has come out in a row that is not .{row}. That row is wrapped by #780 "
                + "and pinned sticky with a scrim by #735; a button that has left it is a control this "
                + "migration moved.");
            Assert.Same(foot.Children.Last(n => n.Handlers.ContainsKey("onclick")), way);
        }
        else
        {
            Assert.Same(card.Children.Last(), way);
        }

        await bench.PressAsync(way.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(TheCard(after, root) is null,
            $"{name}: \"{face}\" was pressed and the card is still on the screen.");
    }

    /// <summary>
    /// THE ORACLE'S ROW IS STILL HERS, AND THE SHELL ONLY HANDED IT A BUTTON.
    ///
    /// <para><c>FootHost</c>'s second consumer, and it is here for the logbook's reason one card along: her
    /// foot carries two controls that are NOT ways out — 🌀 <i>Keep listening</i> turns the dial one line
    /// on, 🥃 <i>Buy her a drink</i> widens the channel, and the second is drawn only where there is a
    /// counter to buy from. A shell that DREW that row would have to know both facts; a shell that hands
    /// the row its button has to know neither.</para>
    ///
    /// <para>So the row must be the PAGE's element (no <c>overlay-shell-ways</c> anywhere), the way out
    /// must be the SHELL's button, and the two controls that are not ways out have to still be there and
    /// still leave her card standing — asserted by pressing <i>Keep listening</i>, because "this control
    /// does not close the card" is a behaviour claim like any other.</para>
    /// </summary>
    [Fact]
    public async Task TheOraclesFootIsHerOwnRowAndItsOtherControlsAreNotWaysOut()
    {
        using DeskBench bench = await DeskBench.BootAsync(Ashore);
        Raise(bench, "_oracleLine");

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = TheCard(painted, "oracle-card")
            ?? throw new Xunit.Sdk.XunitException("the oracle: her gate was raised and no .oracle-card came back.");

        DeskBench.Painted.Node foot = card.Descendants()
            .FirstOrDefault(n => n.HasClass("deck-offer-actions") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException("the oracle has no .deck-offer-actions row at all.");

        Assert.False(foot.HasClass("overlay-shell-ways"),
            "the oracle's foot is drawn by the SHELL now. That is the other half of wave 7's mechanism "
            + "(WaysClass) and it is the wrong half for this card: the shell would have to know that the "
            + "row carries 🌀 Keep listening and a drink button that exists only where there is a counter. "
            + "FootHost is what lets the page keep drawing its own row.");

        Assert.DoesNotContain(card.SelfAndDescendants(), n => n.HasClass("overlay-shell-ways"));

        DeskBench.Painted.Node listen = foot.Children
            .FirstOrDefault(n => !n.Hidden && n.Name.Contains("Keep listening", StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException(
                "the oracle's 🌀 Keep listening is gone. It is the control on her foot that is NOT a way "
                + "out, and it is the whole reason this card wants FootHost rather than WaysClass.");

        await bench.PressAsync(listen.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(TheCard(after, "oracle-card") is not null,
            "🌀 Keep listening closed the oracle's card. That is a control changing what it does, not a "
            + "refactor — and if her row really is all ways out now, she has earned a plain Close and this "
            + "guard is the wrong shape.");
    }

    /// <summary>
    /// AND THE RULES THAT USED TO REACH THEM FOLLOWED THEM.
    ///
    /// <para>#996's bug class, asked of the two elements this wave newly handed to the shell: the family's
    /// action row (drawn by the shell on two cards through <c>WaysClass</c>) and the keypad's Cancel. Blazor
    /// pins the page's scope attribute to the LAST compound selector, so <c>.deck-offer-actions { … }</c>
    /// compiles to <c>.deck-offer-actions[b-map]</c> and stops matching the moment the shell draws that div
    /// — present, correct and dead, with no symptom but a row that has quietly stopped being sticky.</para>
    ///
    /// <para>The general form of this is already guarded next door
    /// (<c>EveryRuleWhoseTargetTheShellDrawsIsWrittenWithDeep</c>), which is what caught these on the way
    /// in. What is here is the two by NAME, with the sentence about what each of them does, so the day one
    /// of them is reverted the failure says what was lost rather than only that a selector moved.</para>
    /// </summary>
    [Theory]
    [InlineData(".deck-offer-actions", "the family's action row, which #780 wraps and #735 pins sticky "
        + "with a 12rem scrim — unpinned, the bar menu's six priced items slide under it and read as a "
        + "greyed-out panel behind glass, which is the bug #780 was filed about")]
    [InlineData(".pin-pad-cancel", "the keypad's Cancel, which is centred by `align-self: center` and "
        + "carries its own monospace face — without the rule it falls back to a browser default button "
        + "stretched across the pad's `align-items: stretch` column")]
    public void TheRulesForWhatTheShellNowDrawsAreWrittenWithDeep(string target, string what)
    {
        string css = WithoutComments(File.ReadAllText(
            Path.Combine(ClientSource(), "Pages", "Map.razor.css")));

        var reaching = Selectors(css)
            .Where(selector => Regex.IsMatch(selector, $@"{Regex.Escape(target)}\s*(\{{|,|$|:)")
                               || selector.TrimEnd().EndsWith(target, StringComparison.Ordinal))
            .ToList();

        Assert.True(reaching.Count > 0,
            $"no rule in Map.razor.css targets `{target}` at all — it is {what}, and a family that has "
            + "lost its rule is worth knowing about even if nothing here is dead.");

        var bare = reaching.Where(s => !s.TrimStart().StartsWith("::deep", StringComparison.Ordinal)).ToList();

        Assert.True(bare.Count == 0,
            $"{bare.Count} rule(s) target `{target}` without `::deep`:\n  - {string.Join("\n  - ", bare)}"
            + $"\n\nThe shell draws that element now, so it carries the SHELL's scope attribute and not the "
            + "page's. Each of these compiles to a selector that matches nothing — present, correct and "
            + $"dead (#996). It is {what}.");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  Plumbing
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    // The three worlds this bench knows, spelled exactly as the dismissibility law spells them, so a card
    // driven here and the same card driven there can never be standing in two different places.
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";
    private const string FreeFlying = "/map?start=wreck";

    private const string HunterId = "wave9-collector";

    /// <summary>Every answer each decision offers, as a player reads them. A ByDecision surface's answers
    /// ARE its way out, so one going missing is a surface with no way out — and that has to fail even for
    /// the one answer this bench cannot press.</summary>
    private static string[] TheAnswersOf(string root) => root switch
    {
        "stand-up-card" => [SeatedPosture.StandUpYes, SeatedPosture.StandUpNo],
        "selfie-offer" => ["Take the shot", "Not now"],
        _ => ["Fire the brake", "Hold"],
    };

    /// <summary>The one card of the family that is up: not hidden, wearing the root asked for.</summary>
    private static DeskBench.Painted.Node? TheCard(DeskBench.Painted painted, string root) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass(root) && !n.Hidden);

    /// <summary>Put the page in the state that raises ONE of these cards, and put the family's siblings
    /// down first. They share <c>.deck-offer-card</c>, and a guard that reads "the first .deck-offer-card
    /// on the screen" without clearing the others reads whichever is earliest in the tree and reports it
    /// under another one's name — this repository's first named bug class, in a test.</summary>
    private static void Raise(DeskBench bench, string gate)
    {
        bench.Poke("_pendingOffer", null);
        bench.Poke("_patronDrink", null);
        bench.Poke("_bankSession", null);
        bench.Poke("_barMenu", null);
        bench.Poke("_oracleOpen", false);
        bench.Poke("_oracleLine", null);
        bench.Poke("_selfieOffer", null);
        bench.Poke("_pinJob", null);

        switch (gate)
        {
            case "_seating":
                // The confirm's gate lives on the page's one seat object (#870 lane 6b), so it is set where
                // the state actually is rather than on a name Map no longer carries.
                SeatedAtATable(bench);
                return;

            case "_selfieOffer":
                bench.Poke(gate, TheSelfieOffer());
                return;

            case "_brakeGate":
                bench.Poke("_brakeDestName", "Mercury");
                bench.Poke(gate, new ArrivalBrake.Gate(ArrivalBrake.Phase.Asking));
                return;

            case "_pinJob":
                bench.Poke(gate, TheCrackJob());
                return;

            case "_patronDrink":
                bench.Poke(gate, "harlan-fess");
                return;

            case "_bankSession":
                bench.Poke(gate, TheBankSession());
                return;

            case "_oracleLine":
                bench.Poke("_oracleOpen", true);
                bench.Poke(gate, TheOracleLine());
                return;

            default:
                throw new InvalidOperationException($"this bench has no road to `{gate}`.");
        }
    }

    /// <summary>A collector astern with the book open on her: the dossier's own gate, on the Nav desk.
    /// A hunter rather than an NPC because a hunter's card carries #962's TERMS block, which is what makes
    /// this dossier long enough for the scroll to be about something.</summary>
    private static void ADossierOnTheGlass(DeskBench bench)
    {
        bench.Poke("_activeDesk", ShipDesk.Nav);
        bench.Poke("_interestTargetId", HunterId);

        var hunters = (List<HunterState>)bench.Peek("_hunters")!;
        hunters.Clear();
        hunters.Add(EncounterRule.SpawnHunter(
            HunterId, "COLLECTOR IX", "luna", new Vector2d(3.9e8, 1.0e7), Vector2d.Zero, 0));
    }

    private static async Task<DeskBench.Painted.Node> TheDossier(DeskBench bench)
    {
        DeskBench.Painted painted = await bench.RenderAsync();
        return painted.Root.Descendants().FirstOrDefault(n => n.HasClass("map-dossier") && !n.Hidden)
               ?? throw new Xunit.Sdk.XunitException(
                   "no .map-dossier came back. The gate is a tactical target with an honest dossier behind "
                   + "it, and this bench's collector is meant to be one — if DossierFor has stopped "
                   + "answering for a hunter, this guard is asking about a card that was never drawn "
                   + "(this repository's fifth named bug class).");
    }

    // ── The records these gates hold, built by reflection off the page's own field types ──────────────

    /// <summary>
    /// The value a private-typed gate wants, built through ITS OWN TYPE rather than named again here: the
    /// widest constructor the field's type declares, filled with the plainest value each parameter can
    /// take. A renamed member or a re-shaped record fails loudly at this helper with the field's name
    /// instead of quietly raising nothing and leaving a guard asking about a card that was never drawn —
    /// this repository's fifth named bug class, which is exactly what a bench of pokes invites.
    /// </summary>
    private static object Build(string field, params (string Named, object? Value)[] said)
    {
        System.Reflection.FieldInfo declared =
            typeof(Map).GetField(field, System.Reflection.BindingFlags.Instance
                                        | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Map has no `{field}` — this bench cannot raise a surface whose gate it can no longer name.");

        Type kind = Nullable.GetUnderlyingType(declared.FieldType) ?? declared.FieldType;
        System.Reflection.ConstructorInfo made = kind.GetConstructors(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"{kind.Name} has no constructor this bench can call.");

        var spoken = said.ToDictionary(one => one.Named, one => one.Value, StringComparer.Ordinal);
        foreach (string named in spoken.Keys)
        {
            Assert.True(made.GetParameters().Any(p => p.Name == named),
                $"`{field}`'s {kind.Name} has no `{named}` parameter any more. This bench says that one out "
                + "loud because the CARD reads it — a silent default there is a card drawn with nothing on "
                + "it, which passes a guard and proves nothing.");
        }

        object?[] arguments = made.GetParameters()
            .Select(p => spoken.TryGetValue(p.Name ?? "", out object? told) ? told : Plainest(p.ParameterType))
            .ToArray();

        return made.Invoke(arguments);
    }

    /// <summary>The plainest value a parameter can hold: enough for the card to render, and nothing this
    /// guard is asserting about.</summary>
    private static object? Plainest(Type kind) =>
        kind == typeof(string) ? "wave-9"
        : Nullable.GetUnderlyingType(kind) is not null ? null
        : kind.IsValueType ? Activator.CreateInstance(kind)
        : null;

    private static object TheSelfieOffer() =>
        Build("_selfieOffer", ("Label", "The wave-9 vista"));

    private static object TheOracleLine() =>
        Build("_oracleLine", ("Text", "The dust tastes of Tuesday."));

    private static object TheBankSession() =>
        Build("_bankSession", ("DisplayName", "Harlan Fess"));

    private static object TheCrackJob() =>
        Build("_pinJob", ("Title", "Open the hatch on B2"), ("Giver", "The Fixer"));

    /// <summary>Sit the captain down and raise the confirm, where the seat's state actually lives. #870
    /// lane 6b moved the five seat fields onto <c>Map.Seating</c>, reached through the page's one
    /// <c>_seating</c> field — the same follow <see cref="SeatState"/> records for the behaviour guards, so
    /// this keeps no sixth copy of where the seat is.</summary>
    private static void SeatedAtATable(DeskBench bench)
    {
        object seat = bench.Peek("_seating")
                      ?? throw new InvalidOperationException(
                          "the page's _seating is null — #870's seat object has moved, and the stand-up "
                          + "confirm's gate cannot be reached without it.");

        System.Reflection.PropertyInfo ask = seat.GetType().GetProperty("StandUpAsk")
            ?? throw new InvalidOperationException(
                "Map.Seating has no StandUpAsk — the stand-up confirm's gate has been renamed. Follow it "
                + "here in the same commit; a bench that cannot raise the card is a guard about nothing.");

        ask.SetValue(seat, true);
    }

    // ── Reading the stylesheet ────────────────────────────────────────────────────────────────────────

    /// <summary>The body of the rule whose selector list is exactly <paramref name="selector"/>.</summary>
    private static string? RuleBody(string css, string selector)
    {
        foreach (Match rule in Regex.Matches(css, @"([^{}]+)\{([^{}]*)\}"))
        {
            if (string.Equals(Squashed(rule.Groups[1].Value), Squashed(selector), StringComparison.Ordinal))
            {
                return rule.Groups[2].Value;
            }
        }

        return null;
    }

    private static IEnumerable<string> Selectors(string css) =>
        Regex.Matches(css, @"([^{}]+)\{[^{}]*\}")
            .SelectMany(rule => rule.Groups[1].Value.Split(','))
            .Select(one => one.Trim())
            .Where(one => one.Length > 0);

    private static string Squashed(string text) =>
        Regex.Replace(text.Trim(), @"\s+", " ");

    private static string WithoutComments(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", " ", RegexOptions.Singleline);

    // ── Reading the markup ────────────────────────────────────────────────────────────────────────────

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

            if (head.Contains('>'))
            {
                break;
            }

            head = back > 0 ? lines[back - 1] : "";
        }

        return "?";
    }

    private static IEnumerable<string> RazorFiles() =>
        Directory.EnumerateFiles(ClientSource(), "*.razor", SearchOption.AllDirectories);

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
