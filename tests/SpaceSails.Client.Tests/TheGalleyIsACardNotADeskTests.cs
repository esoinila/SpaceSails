using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1021 · <b>THE GALLEY IS A CARD, NOT A DESK.</b>
///
/// <para>Owner, on the full-screen Galley desk, verbatim: <i>"We want to keep the news feed... refactor it
/// so it can be elsewhere also, but this UI MUST GO!... This was our first version, it has no gen AI or
/// visibility to the bar surroundings. So keep the features but I want it done in pop-up style like the
/// work the case is."</i></para>
///
/// <para><b>Why this file rather than a line in three others.</b> The change has two halves that can each
/// come back on their own, and neither is visible to the laws already in the repo. The desk can come back —
/// a branch on <c>_activeDesk == ShipDesk.Galley</c> is one line, and the enum member is deliberately still
/// there for it to be written against. And the card can go quiet — a pop-up whose ✕ is wired, whose Esc is
/// listed and whose key toggles is four separate wires, and #997's own finding is that a wire nobody presses
/// is a wire nobody knows is broken.</para>
///
/// <para><b>Everything here presses or types.</b> The bench dispatches through the renderer's own event
/// channel at the handler id the render tree wrote — the click a player's mouse makes and the key a
/// player's keyboard sends. A test that called <c>CloseGalleyCard</c> by name would prove a method clears a
/// field and say nothing about whether anything on the screen or on the keyboard reaches it, which is the
/// exact shape of bug #992 was written to catch.</para>
///
/// <para><b>Red proofs, each run before this file shipped and written down beside its guard.</b></para>
/// </summary>
public sealed class TheGalleyIsACardNotADeskTests
{
    private const string FreeFlying = "/map?start=wreck";
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";

    /// <summary>The class the killed desk wore. Named once, so a guard cannot drift off it.</summary>
    private const string TheDeadDesk = "galley-desk";

    /// <summary>…and the class the card wears.</summary>
    private const string TheCard = "galley-card";

    // ── (a) THE DESK BAND CANNOT RENDER IT ────────────────────────────────────────────────────────────

    /// <summary>
    /// ASKING FOR THE GALLEY NEVER MAKES IT THE ACTIVE DESK — asked from every desk there is.
    ///
    /// <para><c>SwitchDesk</c> is documented as the one place a desk switch happens: the digit keys, the tab
    /// bar, the desk chips and the bridge seats all funnel through it. So this asks it the question from
    /// every seat in the game, through the shipping method, and reads back the field the desk band draws
    /// off. The captain must still be where they were, and the card must be up instead.</para>
    ///
    /// <para><b>RED PROOF:</b> put <c>_activeDesk = desk;</c> back under the Galley fork in
    /// <c>SwitchDesk</c> and every one of the seven rows fails, naming the desk it was asked from.</para>
    /// </summary>
    [Fact]
    public async Task AskingForTheGalleyRaisesTheCardAndLeavesTheCaptainWhereTheyWere()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        var wrong = new List<string>();
        int asked = 0;

        foreach (ShipDesk from in DeskBench.TabBarOrder.Where(d => d != ShipDesk.Galley))
        {
            await bench.SwitchAsync(from);
            ShipDesk before = bench.ActiveDesk;

            await bench.SwitchAsync(ShipDesk.Galley);
            asked++;

            if (bench.ActiveDesk != before)
            {
                wrong.Add($"from {from}: asking for the Galley moved the captain to {bench.ActiveDesk} — the "
                          + "pop-up is a desk switch wearing a card's clothes (#1021)");
            }

            if (bench.Field("_galleyCardOpen") is not true)
            {
                wrong.Add($"from {from}: asking for the Galley raised no card at all");
            }

            // …and the second ask puts it down again (#688's law, from every door).
            await bench.SwitchAsync(ShipDesk.Galley);
            if (bench.Field("_galleyCardOpen") is not false)
            {
                wrong.Add($"from {from}: asking twice left the card up — the door that opens it is not the "
                          + "door that shuts it (#688)");
            }
        }

        Assert.Equal(7, asked);
        Assert.True(wrong.Count == 0, string.Join("\n  - ", wrong));
    }

    /// <summary>
    /// NOTHING IN THE CLIENT DRAWS THE OLD DESK ANY MORE — read off the markup as typed AND off the glass.
    ///
    /// <para>Two halves because two things can go wrong. The source half catches the branch coming back
    /// (<c>_activeDesk == ShipDesk.Galley</c> is one line and the enum member is still there to write it
    /// against). The rendered half catches a class list assembled in C# and splatted in, which the source
    /// half cannot see — #992's own lesson about these two guards.</para>
    ///
    /// <para><b>RED PROOF:</b> restore the <c>else if (_activeDesk == ShipDesk.Galley)</c> branch and its
    /// component and both halves fail — the first naming Map.razor and the line, the second naming
    /// <c>.galley-desk</c> on the glass.</para>
    /// </summary>
    [Fact]
    public async Task TheDeskBandHasNoGalleyBranchLeftInItAndDrawsNoGalleyDesk()
    {
        var inTheSource = new List<string>();
        foreach (string file in RazorFiles())
        {
            string[] lines = File.ReadAllLines(file);

            // A COMMENT SAYING THE BRANCH IS GONE IS NOT THE BRANCH, and this repository's register means
            // the comments QUOTE the code they replaced — the migration note beside the card quotes the
            // desk's own `class="galley-desk text-light"` root by name. The first cut skipped only lines
            // that BEGAN with a comment marker and reported that note as a live surface: a guard that cries
            // wolf is a guard somebody loosens. So a razor block comment is tracked across lines the way it
            // is actually written, and C#'s `//` is skipped where it starts a line. Neither kind of comment
            // can carry a live comparison, and the same line written as code is still seen.
            bool insideRazorComment = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool opensHere = line.Contains("@*", StringComparison.Ordinal);
                bool closesHere = line.Contains("*@", StringComparison.Ordinal);
                bool commented = insideRazorComment || opensHere
                                 || line.TrimStart().StartsWith("//", StringComparison.Ordinal);
                insideRazorComment = (insideRazorComment || opensHere) && !closesHere;

                if (commented)
                {
                    continue;
                }

                if (Regex.IsMatch(line, @"_activeDesk\s*(==|is)[^;]*ShipDesk\.Galley")
                    || line.Contains($"class=\"{TheDeadDesk}", StringComparison.Ordinal))
                {
                    inTheSource.Add($"{Path.GetFileName(file)}:{i + 1}  {line.Trim()}");
                }
            }
        }

        Assert.True(inTheSource.Count == 0,
            $"{inTheSource.Count} line(s) still make the Galley an ACTIVE DESK or draw the desk it used to "
            + "be. #1021: \"this UI MUST GO!\" — the enum member stays (removing it would renumber Deck and "
            + "Captain under everything keyed on them) and its ability to take the screen does not:\n  - "
            + string.Join("\n  - ", inTheSource));

        // …and the same question of what actually reached the glass, at every desk in two worlds.
        var onTheGlass = new List<string>();
        foreach (string url in new[] { FreeFlying, Docked })
        {
            using DeskBench bench = await DeskBench.BootAsync(url);
            foreach (ShipDesk desk in DeskBench.TabBarOrder)
            {
                await bench.SwitchAsync(desk);
                DeskBench.Painted painted = await bench.RenderAsync();
                if (painted.ClassLists.Any(list => Tokens(list).Contains(TheDeadDesk, StringComparer.Ordinal)))
                {
                    onTheGlass.Add($".{TheDeadDesk} drawn at {url} · {desk}");
                }
            }
        }

        Assert.True(onTheGlass.Count == 0, string.Join("\n  - ", onTheGlass));
    }

    // ── (b) THE KEY OPENS IT, AND FOUR THINGS CLOSE IT ────────────────────────────────────────────────

    /// <summary>
    /// <b>6 OPENS IT AND 6 CLOSES IT — typed at the page, not called by name.</b>
    ///
    /// <para>The key's road is the player's: <c>.map-page</c>'s <c>onkeydown</c> → <c>OnKeyDown</c> → the
    /// digit gate → <c>SwitchDesk</c> → the fork. Every gate above the fork is included, which is the point:
    /// the digit branch runs before the deck-walk keys and before the pulse switch, and a card wired
    /// anywhere else would be shadowed by one of them.</para>
    ///
    /// <para><b>RED PROOF:</b> change the fork in <c>SwitchDesk</c> from <c>ToggleGalleyCard()</c> to
    /// <c>OpenGalleyCard()</c> and the second press fails: the card is still up.</para>
    /// </summary>
    [Fact]
    public async Task TheSixKeyOpensTheCardAndTheSixKeyShutsItAgain()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        ArmTheAudioGate(bench);
        DeskBench.Painted painted = await bench.RenderAsync();
        ulong keyboard = DeskBench.TheKeyboard(painted);
        Assert.True(keyboard != 0, "the page drew no keyboard host — nothing could type at it.");

        Assert.False(Up(painted), "the card is up before anybody asked for it.");

        await bench.TypeAsync(keyboard, "6");
        painted = await bench.RenderAsync();
        Assert.True(Up(painted), "pressing 6 drew no galley card.");
        Assert.Equal(ShipDesk.Nav, bench.ActiveDesk);   // …and it did not move the captain

        await bench.TypeAsync(keyboard, "6");
        painted = await bench.RenderAsync();
        Assert.False(Up(painted),
            "pressing 6 a second time left the card up. Owner, of the satchel's I key (#688): \"If I press I "
            + "when inventory is open, let's close it then.\" — a card raised by reflex has to fall by it.");

        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>
    /// <b>ESCAPE SHUTS IT — and does not walk the captain to Nav on the way.</b>
    ///
    /// <para>The second half is the whole reason the card had to join the cancel chain rather than be left
    /// to its ✕. Escape's fall-through is <c>SwitchDesk(ShipDesk.Nav)</c>, so a card the chain did not know
    /// about would have been left standing while the key moved the captain to another desk — #1012's own
    /// finding, one family over: the key did not do nothing, it did something else and then lied about it.
    /// </para>
    ///
    /// <para><b>RED PROOF, and it is better than the one this guard was written expecting.</b> Delist
    /// <c>_galleyCardOpen</c> from <c>TryDismissTopOverlay</c> and the FIRST assertion still passes — the
    /// card is gone. It is gone because Escape fell through to <c>SwitchDesk(ShipDesk.Nav)</c>, which closes
    /// it on the way past, so a guard that only asked "did Escape shut the card" would have been green on a
    /// key that had just moved the captain to another desk. The second assertion is the one that goes red
    /// (<c>Comms</c> ≠ <c>Nav</c>), which is the whole reason it is written down.</para>
    /// </summary>
    [Fact]
    public async Task EscapeShutsTheCardAndTheDeskUnderneathDoesNotMove()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        ArmTheAudioGate(bench);
        await bench.SwitchAsync(ShipDesk.Comms);
        bench.CallOnTheDispatcher("OpenGalleyCard");

        DeskBench.Painted painted = await bench.RenderAsync();
        Assert.True(Up(painted), "the card was not raised, so this proves nothing about Escape.");

        await bench.TypeAsync(DeskBench.TheKeyboard(painted), "Escape");
        painted = await bench.RenderAsync();

        Assert.False(Up(painted), "Escape left the galley card on the screen.");
        Assert.Equal(ShipDesk.Comms, bench.ActiveDesk);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>
    /// <b>THE ✕ AND THE BACKDROP ARE BOTH REAL WAYS OUT</b> — pressed, not read.
    ///
    /// <para>The ✕ is the shell's, wearing the page's <c>view-object-close</c>; the backdrop is the
    /// satchel's own grammar, and it is a convenience rather than the affordance (the dismissibility law
    /// deliberately refuses to count a backdrop as a card's only exit — it is invisible). Both are asserted
    /// because a card that lost either would still pass the other.</para>
    ///
    /// <para><b>RED PROOF:</b> drop <c>OnClose</c> off the shell and the ✕ half fails while the backdrop
    /// half still passes — which is exactly the "control that looks like a way out and is not one" shape.
    /// </para>
    /// </summary>
    [Fact]
    public async Task TheCrossAndTheBackdropEachTakeTheCardDown()
    {
        foreach (string what in new[] { "the ✕", "the backdrop" })
        {
            using DeskBench bench = await DeskBench.BootAsync(Docked);
            bench.CallOnTheDispatcher("OpenGalleyCard");
            DeskBench.Painted painted = await bench.RenderAsync();

            DeskBench.Painted.Node card = TheCardNode(painted)
                ?? throw new Xunit.Sdk.XunitException("the card was not raised at all.");

            DeskBench.Painted.Node? pressable = what == "the ✕"
                ? card.Descendants().FirstOrDefault(n => n.HasClass("view-object-close") && !n.Hidden)
                : painted.Root.Descendants()
                    .FirstOrDefault(n => n.HasClass("view-object-backdrop")
                                         && n.Handlers.ContainsKey("onclick")
                                         && n.Descendants().Any(d => d.HasClass(TheCard)));

            Assert.True(pressable is not null, $"{what} is not on the card at all.");
            await bench.PressAsync(pressable!.Handlers["onclick"]);

            painted = await bench.RenderAsync();
            Assert.False(Up(painted), $"{what} was pressed and the galley card is still on the screen.");

            // NO EscapedPastTheGate ASSERTION HERE, and it is a wall rather than a shrug. Both ways out go
            // through `Dismiss(…)`, whose second half is #470's RefocusMap — `_focusableDiv.FocusAsync()`,
            // which off a browser answers "ElementReference has not been configured correctly". That is the
            // same documented horizon TrackingPost's canvas arrives through, one interop along, and it lands
            // AFTER the state change: the card is already down, which is what this guard measures.
        }
    }

    // ── (c) THE CANTINA CONSOLE ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>[E] AT THE CANTINA RAISES THE CARD AND KEEPS THE CAPTAIN ON THE DECK.</b>
    ///
    /// <para>This is the owner's second complaint made into a law. The old press was
    /// <c>SwitchDesk(ShipDesk.Galley)</c>: standing IN the cantina, with the cantina drawn under your feet,
    /// pressing E swapped the room for a darkened photograph of one — <i>"it has no gen AI or visibility to
    /// the bar surroundings."</i> The card hangs over the room instead, so <c>_deckMode</c> must still be
    /// true and <c>_activeDesk</c> must still be Deck when it is up.</para>
    ///
    /// <para>Driven through the shipping <c>InteractAtConsole</c> with the captain stood on the console's
    /// own square — the same road the E key takes (Map.Deck.Walk's key switch calls exactly this), so a
    /// dispatch that stopped reaching the cantina arm fails here rather than nowhere.</para>
    ///
    /// <para><b>RED PROOF:</b> put <c>SwitchDesk(ShipDesk.Galley)</c> back in the Cantina arm and the card
    /// half fails on the spot (the fork toggles the card too, so the press still raises it — but restore the
    /// old desk branch with it and the deck-mode assertion fails, which is the bug the owner reported).</para>
    /// </summary>
    [Fact]
    public async Task PressingTheCantinaConsoleOpensTheCardWithoutLeavingTheDeck()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        await bench.SwitchAsync(ShipDesk.Deck);
        Assert.True(bench.DeckMode, "the bench never got onto the deck, so this proves nothing.");

        // The page's OWN plan, not a fresh DeckPlan.Ship: the deck under the captain is whichever one the
        // boot built, and asking a different object where the cantina is would be a guard handed the wrong
        // world (this repo's fifth named bug class).
        var deck = (DeckPlan)bench.Peek("_deckPlan")!;
        DeckPlan.ConsoleSpot cantina =
            deck.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.Cantina);

        // Anti-vacuous: the square really does answer as the cantina. Without this the press below could be
        // reaching ConsoleKind.None and the card could be coming from somewhere else entirely.
        Assert.Equal(DeckPlan.ConsoleKind.Cantina, deck.NearestConsole(cantina.X, cantina.Y));

        bench.Poke("_avatarX", (double)cantina.X);
        bench.Poke("_avatarY", (double)cantina.Y);
        bench.CallOnTheDispatcher("InteractAtConsole");

        DeskBench.Painted painted = await bench.RenderAsync();
        Assert.True(Up(painted), "[E] at the CANTINA raised no galley card.");
        Assert.True(bench.DeckMode,
            "[E] at the CANTINA took the captain off the deck they were standing on — which is the owner's "
            + "own complaint (#1021: \"no ... visibility to the bar surroundings\"). The room behind the "
            + "card IS the art this issue asked for.");
        Assert.Equal(ShipDesk.Deck, bench.ActiveDesk);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    // ── (d) ONE RUM LEDGER ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE CARD'S TOT IS THE SHIP'S TOT.</b> Pressing "Pour a tot" on the card moves the deck locker's
    /// own ledger — the tot count and the timestamp <c>PourRum</c> keeps — rather than a second count the
    /// card kept for itself.
    ///
    /// <para>Asserted on the LEDGER FIELDS and not on the button's wiring, because "it calls
    /// PourRumFromGalley" is a claim about source and this is a claim about the world: a re-implemented pour
    /// would raise a number somewhere and leave <c>_lastRumMs</c> at its sentinel.</para>
    ///
    /// <para><b>RED PROOF:</b> point the card's button at a local increment instead of
    /// <c>PourRumFromGalley</c> and the timestamp assertion fails while the count one still passes.</para>
    /// </summary>
    [Fact]
    public async Task PouringFromTheCardGoesThroughTheOneRumFunnel()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        bench.CallOnTheDispatcher("OpenGalleyCard");
        DeskBench.Painted painted = await bench.RenderAsync();

        DeskBench.Painted.Node card = TheCardNode(painted)
            ?? throw new Xunit.Sdk.XunitException("the card was not raised at all.");

        Assert.Equal(0, (int)bench.Field("_rumTots")!);
        Assert.Equal(double.MinValue, (double)bench.Field("_lastRumMs")!);

        DeskBench.Painted.Node pour = card.Descendants()
            .FirstOrDefault(n => n.Handlers.ContainsKey("onclick")
                                 && n.Name.Contains("Pour a tot", StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException(
                "the card has no \"Pour a tot\" button — #1021 kept the features, and that is one of them.");

        await bench.PressAsync(pour.Handlers["onclick"]);

        Assert.Equal(1, (int)bench.Field("_rumTots")!);
        Assert.True((double)bench.Field("_lastRumMs")! > double.MinValue,
            "the pour raised a tot count and never touched _lastRumMs — the ledger PourRum keeps. That is a "
            + "second rum locker, and the two would disagree the moment either was used.");
        Assert.NotNull(bench.Field("_lastRumLine"));
        Assert.Empty(bench.EscapedPastTheGate);
    }

    // ── (e) IT IS ONE OF THE SHELL'S ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE CARD IS DRAWN THROUGH <c>OverlayShell</c></b>, and the way out is the shell's own.
    ///
    /// <para>Read off the RENDER TREE rather than the markup: the shell's root classes
    /// (<c>overlay-shell</c>, <c>overlay-shell-bare</c>) and its dismiss's own class
    /// (<c>overlay-shell-dismiss</c>) are written by the component and by nothing else, so a hand-rolled
    /// card wearing <c>.galley-card</c> could not produce them however carefully it was copied.</para>
    ///
    /// <para>The Bare frame matters and is asserted by name: the family's foot is pinned by
    /// <c>::deep .view-object-close</c> on a button that must be a DIRECT child of the card, which is what
    /// <c>OverlayFrame.Bare</c> guarantees and what a Card frame's body wrapper would quietly break.</para>
    ///
    /// <para><b>RED PROOF:</b> hand-roll the card as a plain <c>&lt;div class="galley-card"&gt;</c> with its
    /// own <c>&lt;button class="view-object-close"&gt;</c> and every assertion here fails while the card
    /// still opens and closes — which is why this guard is not the same guard as the ones above.</para>
    /// </summary>
    [Fact]
    public async Task TheCardTakesTheShellAndItsWayOutIsTheShellsOwn()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        bench.CallOnTheDispatcher("OpenGalleyCard");
        DeskBench.Painted painted = await bench.RenderAsync();

        DeskBench.Painted.Node card = TheCardNode(painted)
            ?? throw new Xunit.Sdk.XunitException("the card was not raised at all.");

        Assert.True(card.HasClass("overlay-shell"),
            "`.galley-card` is not an OverlayShell root — #997's mechanism is what gives every pop-up in "
            + "this client one head, one way out and one audit.");
        Assert.True(card.HasClass("overlay-shell-bare"),
            "the card is a shell and not a Bare one. The family's foot is pinned on a button that must be a "
            + "DIRECT child of the card, which only the Bare frame guarantees.");

        DeskBench.Painted.Node way = card.Descendants()
            .FirstOrDefault(n => n.HasClass("overlay-shell-dismiss"))
            ?? throw new Xunit.Sdk.XunitException(
                "the card carries no shell-drawn dismiss at all — the owner's ruling of 2026-08-24 failing "
                + "quietly instead of loudly.");

        Assert.True(way.HasClass("view-object-close"),
            "the page named `view-object-close` for its way out and the shell did not put it on: the CSS "
            + "that dresses that button reaches nothing.");
        Assert.True(way.Handlers.ContainsKey("onclick"), "the way out is wired to nothing.");

        // The backdrop is the satchel's own, and the card must not close itself under the captain's hand:
        // StopClicks is what stops a click on the card reaching the backdrop's onclick behind it. Read off
        // the attribute Blazor actually emits for `@onclick:stopPropagation` rather than off a handler —
        // there is no handler, which is the point: the shell asks the browser not to bubble, and nothing is
        // wired to the card's own click at all.
        Assert.True(card.Attributes.ContainsKey("__internal_stopPropagation_onclick"),
            "the card does not stop its own clicks (StopClicks), so reading it would shut it — the exact "
            + "bug `.view-object` cards carry StopClicks for.");
    }

    // ── (f) THE WIRE IS A COMPONENT, AND IT HAS TWO CONSUMERS ─────────────────────────────────────────

    /// <summary>
    /// <b>THE FEED IS SOMEWHERE ELSE TOO.</b> Owner: <i>"keep the news feed... refactor it so it can be
    /// elsewhere also."</i> A component with one caller is a file move wearing a component's clothes, so the
    /// law is that <c>NewsWirePanel</c> is drawn BOTH by the galley card and by the Comms desk's ticker —
    /// asked of the render tree, which is the only place a component's actual use can be seen.
    ///
    /// <para><b>RED PROOF:</b> put the comms ticker's markup back inline in Map.razor and the Comms half
    /// fails, naming a desk that draws a wire nothing shares.</para>
    /// </summary>
    [Fact]
    public async Task TheWireComponentDrawsInBothTheCardAndTheCommsTicker()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);

        await bench.SwitchAsync(ShipDesk.Comms);
        DeskBench.Painted onComms = await bench.RenderAsync();
        Assert.Contains("NewsWirePanel", onComms.Components);
        Assert.Contains(onComms.ClassLists, list => Tokens(list).Contains("comms-ticker", StringComparer.Ordinal));

        await bench.SwitchAsync(ShipDesk.Nav);
        bench.CallOnTheDispatcher("OpenGalleyCard");
        DeskBench.Painted onTheCard = await bench.RenderAsync();

        DeskBench.Painted.Node card = TheCardNode(onTheCard)
            ?? throw new Xunit.Sdk.XunitException("the card was not raised at all.");

        Assert.Contains("NewsWirePanel", onTheCard.Components);
        Assert.True(card.Descendants().Any(n => n.HasClass("news-wire")),
            "the galley card draws no news wire — #1021 kept the feed, and this is where it lives now.");

        // …and the wire it draws is the wire, not a placeholder: the day-labelled scrollback is under it.
        Assert.Contains("Earlier on the wire", card.Spoken, StringComparison.Ordinal);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#338's gesture unlock, paid for in advance. <c>OnKeyDown</c>'s first act is
    /// <c>RendererInterop.ArmAudio()</c>, a <c>[JSImport]</c> that reaches for a browser this bench does not
    /// have; off-browser it throws and the throw takes the whole handler with it, so the FIRST key a bench
    /// types is swallowed by the same documented gate <c>TrackingPost</c>'s canvas arrives through. Found by
    /// running it: without this the 6-key guard reported "pressing 6 drew no galley card", which was true of
    /// the bench and false of the game. Set here rather than paid for with a warm-up keystroke, so a guard
    /// about which card a key reaches is not quietly measuring one press behind.</summary>
    private static void ArmTheAudioGate(DeskBench bench) => bench.Poke("_audioArmed", true);

    private static DeskBench.Painted.Node? TheCardNode(DeskBench.Painted painted) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass(TheCard) && !n.Hidden);

    private static bool Up(DeskBench.Painted painted) => TheCardNode(painted) is not null;

    private static string[] Tokens(string classList) =>
        classList.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static IEnumerable<string> RazorFiles() =>
        Directory.EnumerateFiles(ClientSource(), "*.razor", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(ClientSource(), "*.cs", SearchOption.AllDirectories));

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
