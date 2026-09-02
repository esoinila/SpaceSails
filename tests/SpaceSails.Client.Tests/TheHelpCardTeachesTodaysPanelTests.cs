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
/// #949 · <b>THE PLOTTING CARD — IT OPENS, IT SHUTS, AND IT IS RIGHT ABOUT TODAY'S PANEL.</b>
///
/// <para>Owner, 2026-08-18, posting a screenshot of his own multi-step plan: <i>"We should have a help page
/// where we show multi-step plan and the use of the schrub and burn. New player seeing this image would
/// understand how to play. I really like the increment - and decrement options."</i></para>
///
/// <para>#972 answered the first half with <c>/help/nav</c>, a full page in a second tab. This card is the
/// other half — the same lesson raised OVER the map, because the <c>?</c> is pressed mid-plan and a page in
/// another tab answers a question about the panel by taking the panel off the screen.</para>
///
/// <h3>The two things that can go wrong with a help card, and they are not the same kind of thing</h3>
///
/// <list type="number">
/// <item><b>IT TRAPS THE READER.</b> A card raised by a confused player that then ignores the key a confused
/// player reaches for would be teaching, by its own behaviour, the opposite of everything it says. §1 opens
/// it by each of its three doors and shuts it by each of its three ways out, by PRESSING and by TYPING —
/// never by calling a closer and believing it.</item>
/// <item><b>IT LIES.</b> This is the one with teeth, and it is this repository's own named bug class living
/// where it is hardest to see: a sentence reporting one thing while the sim does another. Nothing else in
/// the client is made of sentences ABOUT the client, so nothing else can go stale in silence the way a help
/// page can — somebody moves a button, the card goes on describing the old one, and no test in the world
/// notices. §2 and §3 are the answer: every face the card prints is asked of the SAME Core member the panel
/// draws that button with, first of what was actually rendered and then of the source, which may not
/// contain a typed copy of any of them.</item>
/// </list>
///
/// <h3>Why §3 bans the literal rather than just requiring the call</h3>
///
/// <para>Because "the card mentions ±5 p" passes on a card that mentions it by typing it, and that card is
/// wrong the afternoon <c>NudgePulsesCoarse</c> changes. The ban is what makes the borrowing load-bearing:
/// with it there is no way to name a button on this card except by asking the panel's own Core for its
/// face. Rename the member and the card stops compiling; change its value and the card follows without an
/// edit; type it by hand and this goes red naming the string.</para>
/// </summary>
[SlowGate] // #251 · 11 s over 7 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheHelpCardTeachesTodaysPanelTests
{
    /// <summary>Docked at Selene Gate — the same world the pop-up law raises its cards in, and a berth is
    /// where a trip gets planned.</summary>
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";

    /// <summary>The class the card's root wears. Located by name because that is what the stylesheet, the
    /// pop-up law and this file all point at.</summary>
    private const string CardClass = "nav-help-card";

    // ── §1 · IT OPENS AND IT SHUTS ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE <c>?</c> KEY RAISES IT, AND A SECOND <c>?</c> PUTS IT AWAY.
    ///
    /// <para>Typed at the page's own <c>onkeydown</c>, so the road is the player's: every gate in
    /// <c>OnKeyDown</c> above the <c>?</c> rung is crossed on the way, including the <c>/</c> that opens the
    /// Nav search box one line above it.</para>
    ///
    /// <para><b>RED, run:</b> delete the <c>?</c> rung from <c>Map.Sim.Keys.cs</c> and the first assertion
    /// fails — "the ? key drew no .nav-help-card".</para>
    /// </summary>
    [Fact]
    public async Task TheQuestionMarkKeyRaisesTheCardAndTypingItAgainPutsItAway()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        await bench.SwitchAsync(ShipDesk.Nav);

        Assert.Null(TheCard(await bench.RenderAsync()));

        await TypeAsync(bench, "?");
        Assert.NotNull(TheCard(await bench.RenderAsync()));

        await TypeAsync(bench, "?");
        Assert.Null(TheCard(await bench.RenderAsync()));
    }

    /// <summary>
    /// ESCAPE CLOSES IT — and the key is TYPED, not the closer called.
    ///
    /// <para>The distinction is the whole discipline of <c>TryDismissTopOverlay</c>: it is a chain of early
    /// returns, so a test that invoked the method would prove a rung exists and say nothing about whether
    /// the key ever REACHES it. Typed at the keyboard host, this crosses every gate above it — the peek, the
    /// convergence band, the satchel, the story card, the seat — exactly as a player's press does.</para>
    ///
    /// <para><b>RED, run:</b> comment the <c>_navHelpOpen</c> rung out of <c>TryDismissTopOverlay</c> and
    /// this fails: the card is still up after Escape, and — worse, which is why the second assertion is here
    /// — Escape fell through to <c>SwitchDesk(Nav)</c> and did something else instead.</para>
    /// </summary>
    [Fact]
    public async Task EscapeClosesIt()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        await bench.SwitchAsync(ShipDesk.Nav);

        bench.CallOnTheDispatcher("OpenNavHelp");
        Assert.NotNull(TheCard(await bench.RenderAsync()));

        await TypeAsync(bench, "Escape");
        Assert.Null(TheCard(await bench.RenderAsync()));
        Assert.False((bool)bench.Peek("_navHelpOpen")!);
    }

    /// <summary>
    /// THE TOOLBAR <c>?</c> IS A DOOR, PROVED BY PRESSING IT.
    ///
    /// <para>Not a source read of an <c>href</c>: the <c>?</c> used to be an anchor to <c>/help/nav</c> and
    /// is a button onto this card now, and the thing worth guarding is that pressing it puts the card up —
    /// which a regex over the markup cannot tell you. The control is found by the words it wears, the same
    /// reading a player takes.</para>
    ///
    /// <para><b>RED, run:</b> unwire the button's <c>@@onclick</c> and the press does nothing.</para>
    /// </summary>
    [Fact]
    public async Task TheNavToolbarQuestionMarkRaisesTheCard()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        await bench.SwitchAsync(ShipDesk.Nav);

        DeskBench.Painted before = await bench.RenderAsync();
        Assert.Null(TheCard(before));

        DeskBench.Painted.Node question = before.Root.Descendants().Single(
            n => n.Element == "button"
                 && !n.Hidden
                 && n.Spoken == "?"
                 && n.Handlers.ContainsKey("onclick"));

        await bench.PressAsync(question.Handlers["onclick"]);
        Assert.NotNull(TheCard(await bench.RenderAsync()));
    }

    // ── §2 · WHAT IT SAYS IS WHAT THE PANEL SAYS ──────────────────────────────────────────────────────

    /// <summary>
    /// Every face the panel draws with a Core member, as that member gives it today. This list IS the
    /// specification of what the card must teach, and it is asked of Core rather than written out — so the
    /// day <c>NudgePulsesCoarse</c> becomes 10 the expectation moves with the button and the card has to
    /// have moved too.
    /// </summary>
    private static IEnumerable<(string What, string Face)> TheLiveFaces()
    {
        yield return ("the scrub's own label", NodeFrame.ScrubLabel);
        yield return ("the compose row's loud button", NodeFrame.AddBurnAtScrubButton);
        yield return ("the orbit arrival's compose button",
            ArrivalStepRule.AddAtScrubButton(ArrivalStepRule.ArrivalKind.Orbit));
        yield return ("the dock arrival's compose button",
            ArrivalStepRule.AddAtScrubButton(ArrivalStepRule.ArrivalKind.Dock));

        // The three pairs the owner named as the whole point — "I really like the increment - and decrement
        // options" — every one of the ten faces, coarse and fine, both signs.
        yield return ("aim, down", NodeFrame.NudgeLabel(-1));
        yield return ("aim, up", NodeFrame.NudgeLabel(1));
        yield return ("size, coarse down", NodeFrame.NudgeMagnitudeLabel(-1, true));
        yield return ("size, fine down", NodeFrame.NudgeMagnitudeLabel(-1, false));
        yield return ("size, fine up", NodeFrame.NudgeMagnitudeLabel(1, false));
        yield return ("size, coarse up", NodeFrame.NudgeMagnitudeLabel(1, true));
        yield return ("when, coarse earlier", NodeFrame.NudgeEpochLabel(-1, true));
        yield return ("when, fine earlier", NodeFrame.NudgeEpochLabel(-1, false));
        yield return ("when, fine later", NodeFrame.NudgeEpochLabel(1, false));
        yield return ("when, coarse later", NodeFrame.NudgeEpochLabel(1, true));

        // The arrive row: the arm control, the promise it makes, and all three badge states.
        yield return ("the arm control", ArrivalStepRule.ArmButtonLabel("Mars"));
        yield return ("the armed step's promise", ArrivalStepRule.ArmedAndNothingMore);
        yield return ("the ✓ badge word", ArrivalStepRule.StatusValid);
        yield return ("the ✗ badge word", ArrivalStepRule.StatusInvalid);
        yield return ("the ⌛ badge word", ArrivalStepRule.StatusNotJudged);
        yield return ("the ⌛ badge glyph", ArrivalStepRule.PendingBadge);
    }

    /// <summary>
    /// THE CARD, AS DRAWN, CARRIES EVERY ONE OF THEM.
    ///
    /// <para>Read off the RENDERED subtree of the card itself rather than off the razor file, and that is
    /// the difference between this and §3: a face that is in the source inside an <c>@@if</c> nobody
    /// satisfies is a face the reader never sees. What is asserted here is what a captain who pressed
    /// <c>?</c> has in front of him.</para>
    ///
    /// <para><b>RED, run:</b> drop the size row from the card and this names the four faces that went with
    /// it; change <c>NudgePulsesCoarse</c> in Core and it stays green, which is the whole point.</para>
    /// </summary>
    [Fact]
    public async Task TheCardPrintsEveryFaceThePanelDraws()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        await bench.SwitchAsync(ShipDesk.Nav);
        bench.CallOnTheDispatcher("OpenNavHelp");

        DeskBench.Painted.Node? card = TheCard(await bench.RenderAsync());
        Assert.NotNull(card);

        string said = card!.Spoken;
        var missing = TheLiveFaces()
            .Where(f => !said.Contains(f.Face, StringComparison.Ordinal))
            .Select(f => $"{f.What}: \"{f.Face}\"")
            .ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} of the panel's own faces are not on the help card, so a captain reading it is "
            + "being taught a panel that no longer exists:\n  - " + string.Join("\n  - ", missing));
    }

    // ── §3 · …AND IT BORROWED THEM RATHER THAN TYPING THEM ────────────────────────────────────────────

    /// <summary>
    /// NOT ONE OF THOSE FACES IS TYPED INTO THE CARD.
    ///
    /// <para>§2 alone would pass on a card that had every face pasted in as a literal — and that card is
    /// wrong the afternoon somebody moves a step constant, with nothing going red. This is what makes the
    /// borrowing load-bearing rather than a style the next editor can quietly drop.</para>
    ///
    /// <para>The razor COMMENT block is stripped first: it explains this very rule, and quoting a face in
    /// the sentence that forbids typing one is the one place a literal is harmless.</para>
    ///
    /// <para><b>RED, run:</b> replace <c>@@NodeFrame.NudgeMagnitudeLabel(1, true)</c> on the card with the
    /// text it renders and this fails naming "+5 p" — proved by doing exactly that.</para>
    /// </summary>
    [Fact]
    public void NoFaceOnTheCardIsATypedCopy()
    {
        string prose = ProseAndAttributesOf(TheCardSource());

        var typed = TheLiveFaces()
            .Where(f => prose.Contains(f.Face, StringComparison.Ordinal))
            .Select(f => $"{f.What}: \"{f.Face}\" is typed into the card")
            .ToList();

        Assert.True(typed.Count == 0,
            $"{typed.Count} of the panel's faces are TYPED on the help card instead of being asked of the "
            + "same Core member the panel draws them with. A typed face is a second source of truth about "
            + "the UI, and it stops agreeing with the first one silently:\n  - "
            + string.Join("\n  - ", typed));

        // …and the card really does ASK, rather than passing this guard by not naming the buttons at all —
        // which is the hole every ban of this shape has, and the reason a ban is never the whole law.
        string card = WithoutRazorComments(TheCardSource());
        foreach (string call in new[]
        {
            "NodeFrame.ScrubLabel",
            "NodeFrame.AddBurnAtScrubButton",
            "NodeFrame.NudgeLabel",
            "NodeFrame.NudgeMagnitudeLabel",
            "NodeFrame.NudgeEpochLabel",
            "ArrivalStepRule.AddAtScrubButton",
            "ArrivalStepRule.ArmButtonLabel",
            "ArrivalStepRule.ArmedAndNothingMore",
            "ArrivalStepRule.StatusValid",
            "ArrivalStepRule.StatusInvalid",
            "ArrivalStepRule.StatusNotJudged",
            "ArrivalStepRule.PendingBadge",
            "ArrivalStepRule.Badge",
        })
        {
            Assert.Contains(call, card, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// …AND NEITHER IS THE PANEL'S OWN COPY.
    ///
    /// <para>The other end of the same rope, and the half that would rot first. Moving these strings into
    /// Core only helps while BOTH readers go on reading: a panel that quietly re-typed <c>NOT JUDGED</c>
    /// into its badge would leave the card printing Core's word and the row printing its own, which is the
    /// exact drift this whole arrangement exists to prevent — with the card looking innocent.</para>
    ///
    /// <para><b>RED, run:</b> put the literal back in Map.razor's badge and this names it.</para>
    /// </summary>
    [Fact]
    public void ThePanelDoesNotRetypeTheWordsItMovedIntoCore()
    {
        // BOTH comment styles, because this file has both: `@* … *@` around the markup and plain `//` inside
        // the `@{ }` blocks. Line 6624's `// must read NOT JUDGED, never INVALID` is the razor code's own
        // note about WHY the badge says what it says — a sentence that has to be able to name the word it
        // is about. Found by running this guard, which named it.
        string map = Regex.Replace(
            WithoutRazorComments(Source("Pages", "Map.razor")), @"^\s*//.*$", " ", RegexOptions.Multiline);

        var retyped = new[]
        {
            ArrivalStepRule.StatusNotJudged,
            ArrivalStepRule.ArmedAndNothingMore,
            NodeFrame.AddBurnAtScrubButton,
            NodeFrame.AddBurnAtScrubHint,
        }.Where(face => map.Contains(face, StringComparison.Ordinal)).ToList();

        Assert.True(retyped.Count == 0,
            "Map.razor has gone back to typing a string it shares with the help card, so the two can drift "
            + "again:\n  - " + string.Join("\n  - ", retyped.Select(f => $"\"{f}\"")));
    }

    // ── §4 · IT IS A DOOR AND NOT A DEAD END ──────────────────────────────────────────────────────────

    /// <summary>The short read hands the reader on to the long one. The route is read off the compiled
    /// page's own <see cref="Microsoft.AspNetCore.Components.RouteAttribute"/> in
    /// <see cref="TheNavHelpPageTeachesTheWholeLoopTests"/>; here it is only asserted that the card carries
    /// both onward links, so pressing <c>?</c> can never be a smaller answer with no way to a bigger one.
    /// </summary>
    [Fact]
    public void TheCardHandsTheReaderOnToThePageAndTheGuide()
    {
        string card = TheCardSource();
        Assert.Contains("href=\"/help/nav\"", card, StringComparison.Ordinal);
        Assert.Contains("href=\"/guide\"", card, StringComparison.Ordinal);

        // In a SECOND TAB, both of them. A same-tab navigation off /map would drop the world the captain is
        // standing in to read a help page — the exact trade this card was written to stop making.
        Assert.Equal(2, Regex.Matches(card, @"target=""_blank""").Count);
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Dispatch a real key at the page's own keyboard host, the way a player's press arrives.
    ///
    /// <para><c>_audioArmed</c> is poked first, and it is not optional — the house idiom, borrowed from
    /// <c>ThePeekIsAModeYouCanSeeYourWayOutOfTests</c>. <c>OnKeyDown</c>'s very first act on a cold page is
    /// to arm WebAudio through JS interop (#338), which off-browser raises and takes the whole handler down
    /// with it, so the first key of a session would be swallowed and every one-press test would report the
    /// feature broken. Found by running this file without it: the <c>?</c> key "did nothing" and Escape
    /// "ignored the card", neither of which was true.</para>
    /// </summary>
    private static async Task TypeAsync(DeskBench bench, string key)
    {
        bench.Poke("_audioArmed", true);
        ulong keyboard = DeskBench.TheKeyboard(await bench.RenderAsync());
        Assert.True(keyboard != 0,
            "the page draws no `.map-page` element with an onkeydown handler on it — the div every key in "
            + "this game lands on (Map.razor:14). A page without one has gone deaf.");

        await bench.TypeAsync(keyboard, key);
    }

    private static DeskBench.Painted.Node? TheCard(DeskBench.Painted painted) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass(CardClass) && !n.Hidden);

    private static string TheCardSource() => Source("Components", "PlottingHelp.razor");

    /// <summary>
    /// The razor file with everything that is not markup or code taken out of it, then with every DOTTED
    /// IDENTIFIER CHAIN taken out too.
    ///
    /// <para>The comments go for the obvious reason: they explain the ban, and a rule its own explanation
    /// broke would be a rule nobody could write down.</para>
    ///
    /// <para>The identifier chains go for a reason found by running this: <c>NodeFrame.ScrubLabel</c>'s own
    /// VALUE is "Scrub", which is a substring of the member's NAME — so a naive scan reported the borrowing
    /// itself as a typed copy, and would have gone on doing that for every face whose value is a word its
    /// constant is named after. Stripping <c>Foo.Bar</c> tokens leaves exactly the prose and the attributes,
    /// which is where a typed copy would have to be for a reader to see it.</para>
    /// </summary>
    private static string ProseAndAttributesOf(string razor)
    {
        string words = Regex.Replace(razor, @"@\*.*?\*@", " ", RegexOptions.Singleline);
        words = Regex.Replace(words, @"^\s*///.*$", " ", RegexOptions.Multiline);
        return Regex.Replace(words, @"[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)+", " ");
    }

    private static string WithoutRazorComments(string razor) =>
        Regex.Replace(razor, @"@\*.*?\*@", " ", RegexOptions.Singleline);

    private static string Source(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", .. parts]));

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
