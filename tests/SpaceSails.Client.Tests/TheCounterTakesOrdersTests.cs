using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #756 · THE COUNTER TAKES ORDERS, AND THE HALL WEARS ITS ART — the client half.
///
/// <para>Owner, live playtest 2026-08-08, walked to the B1 cantina hall's counter: <i>"HOW DO I ORDER A
/// DRINK FROM THE BAR?????? I walk to the bar to buy a drink... not possible... WHY?"</i> — and, on the same
/// floor, <i>"let's put todo to have gen-AI Bar image on the background like we have in space ports."</i></para>
///
/// <para>Core owns whether a fixture serves and which picture a hall wears (<c>TheCounterTakesOrdersTests</c>
/// over there). What is left to get wrong is the WIRING, which is exactly what was wrong for two issues: the
/// counter existed, the card existed, the menu existed, and the key that joins them did not. So these read
/// the press path and the drawn frame.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCounterTakesOrdersTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private const int WidthPx = 1280;
    private const int HeightPx = 560;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    // ── Reading the shipped source, the way the #736 guards do ──────────────────────────────────────────
    //
    // Transcribed rather than asked for: a test that asked the page for its own call graph could not notice
    // the call going away, which is the entire failure this file exists to catch.
    private static string Pages(string file)
    {
        string here = AppContext.BaseDirectory;
        for (DirectoryInfo? d = new(here); d is not null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "src", "SpaceSails.Client", "Pages", file);
            if (File.Exists(candidate))
            {
                return MapMarkup.Read(candidate);
            }
        }
        throw new FileNotFoundException($"could not find src/SpaceSails.Client/Pages/{file} from {here}");
    }

    /// <summary>#870 lane 6c · Re-PATHED, never re-asserted. The counter's seat is TWO files now: the
    /// page's half (the <c>StoolSeat</c> record, the two dev cheats and the forwarders) and the verbs, which
    /// moved onto <c>Map.Seating</c> behind <c>ISeatHost</c>. This is both, concatenated in that order —
    /// exactly the text these guards read out of one file before the verbs moved, and concatenated rather
    /// than narrowed because two of the claims below are <c>DoesNotContain</c> over the whole stool.</summary>
    private static string Stool() =>
        Pages("Map.Stool.cs") + Pages(Path.Combine("Seating", "Seating.Stool.cs"));

    private static string Method(string file, string signature) => MethodOf(Pages(file), file, signature);

    /// <summary>The same cut, over a text already in hand. The end sentinel no longer spells out an indent
    /// depth: the verbs are one nesting level deeper now (members of <c>Map.Seating</c>), and a sentinel
    /// counting four spaces would have run to the end of the file and widened every claim.</summary>
    private static string MethodOf(string src, string label, string signature)
    {
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{label} no longer has `{signature}` where this guard can read it.");
        Match next = Regex.Match(src[(at + 1)..], @"\n\s*private ");
        int end = next.Success ? at + 1 + next.Index : -1;
        return src[at..(end > at ? end : src.Length)];
    }

    [Fact]
    public void PressingEAtTheCounterOpensTheServiceCard()
    {
        // (a) THE BUG ITSELF. HiveAmenityInteract is what [E] at a counter reaches (Map.Deck's dispatch,
        // ConsoleKind.HiveAmenity) and for two issues its whole answer was a paragraph. It must now ASK CORE
        // whether this fixture serves and, when it does, OPEN THE CARD.
        //
        // Red proof: delete the `OpenCounterService(counter);` line — or the `CounterService.For(` question
        // in front of it — and this fails naming what went missing.
        string press = Method("Map.Surface.Canteen.cs", "private void HiveAmenityInteract()");

        Assert.Contains("CounterService.For(", press, StringComparison.Ordinal);
        Assert.Contains("OpenCounterService(", press, StringComparison.Ordinal);

        // …and it must ask about the amenity the captain is actually standing at, not about the floor. The
        // press already matched `a` by position; asking with anything else would open the wrong room's card.
        Assert.Contains("CounterService.For(ex.Stop.Body.Id, a.Use)", press, StringComparison.Ordinal);

        // The room's own paragraph still lands, and lands FILED rather than flashed — a pulse raised in the
        // same breath as a card plays under that card's blur (#686/#736), which is a line not said.
        Assert.Contains("FileNote(", press, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCardItOpensIsTheOneTheHavenBarsAlreadyOpen()
    {
        // The one-seam law, read off the source. If OpenCounterService ever grows its own dialog state, the
        // B1 counter and the Tilt bar stop being one machine and this repo grows its sixth twice-told truth.
        string open = Method("Map.Quests.Bar.cs", "private void OpenCounterService(");

        Assert.Contains("_barMenu = counter;", open, StringComparison.Ordinal);
        Assert.Contains("_barNotice = counter.Greeting;", open, StringComparison.Ordinal);

        // It opens ON the menu. The reported bug is a captain who could not see anything to order; a card
        // that opens closed is that bug wearing a card.
        Assert.Contains("_showBarMenu = true;", open, StringComparison.Ordinal);

        // And it is not a second implementation of the purchase: nothing here spends, pours or debits.
        Assert.DoesNotContain("_credits", open, StringComparison.Ordinal);
        Assert.DoesNotContain("PourRum(", open, StringComparison.Ordinal);
    }

    [Fact]
    public void WhatAPurchaseAnswersIsDrawnOnTheCardThatWasPressed()
    {
        // (b) #736's law, for the verb this issue adds. BuyDrink is the one handler both counters share, so
        // its answer has to land in the slot the open card draws — and the card has to draw it.
        string buy = Method("Map.Quests.Bar.cs", "private void BuyDrink(");

        Assert.Contains("_barNotice = receipt;", buy, StringComparison.Ordinal);
        Assert.Contains("_barNotice =", buy, StringComparison.Ordinal);

        // One price, asked of Core, used by the debit. A handler that debited a different number than the
        // button printed is bug class five with a receipt attached.
        Assert.Contains("drink.PriceAt(keep.DrinkPrice)", buy, StringComparison.Ordinal);
        Assert.Contains("_credits -= cost;", buy, StringComparison.Ordinal);

        // Food is not a pour: a tray must not route through the one wobble law.
        Assert.Contains("DrinkCategory.Food", buy, StringComparison.Ordinal);

        // …and the slot it writes into is rendered INSIDE the counter card's own block, not in a banner
        // behind it. Cut the block the way the #736 guards cut theirs: from its own `@if (` to the next.
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_deckMode && _barMenu is { } keep)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the counter card block this guard knows how to find.");
        int end = razor.IndexOf("\n    @if (", start + 10, StringComparison.Ordinal);
        string block = razor[start..(end > start ? end : razor.Length)];

        Assert.Contains("_barNotice", block, StringComparison.Ordinal);
        Assert.Contains("@(() => BuyDrink(d))", block, StringComparison.Ordinal);

        // The button's own label, its enabled-ness and the debit all read the SAME number.
        Assert.Contains("d.PriceAt(keep.DrinkPrice)", block, StringComparison.Ordinal);
        Assert.Contains("_credits < cost", block, StringComparison.Ordinal);

        // Nobody is behind the B1 counter, so the two verbs that need a person are gated (#618 canon).
        Assert.Contains("keep.SelfService", block, StringComparison.Ordinal);

        // And the desk the captain is standing at is drawn on the panel they are standing at it through.
        Assert.Contains("keep.DeskArtUrl", block, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDevStartWalksTheCaptainToTheFixtureAndNotToACoordinate()
    {
        // Owner's standing rule: every new feature ships with a URL that demos it. And it must reach the
        // fixture by ASKING which one serves — a hand-typed square would drift the day the hall is recarved.
        string stand = Method("Map.Surface.Cheats.cs", "private void StandAtTheCounterIfAsked(");

        Assert.Contains("CounterService.For(", stand, StringComparison.Ordinal);
        Assert.Contains("StandCaptainAt(a.X, a.Y", stand, StringComparison.Ordinal);
        Assert.Contains("_counterCheat", stand, StringComparison.Ordinal);

        Assert.Contains(DevStarts.All, e => e.Url == "/map?counter=1");
    }

    // ── #756 · THE HIGH CHAIRS, WIRED ───────────────────────────────────────────────────────────────────

    [Fact]
    public void TakingAStoolSeatsYouOnTheCardTheMenuIsAlreadyOn()
    {
        // THE GRAMMAR, READ OFF THE SOURCE. Owner: "the #757 seat verb on the counter fixture (one E,
        // pick-or-default a free stool), the keep serves you seated (same #756 menu seam)."
        //
        // "Serves you seated" is only true if the menu is on the card the stool is on. So the seat verb has
        // to live in the COUNTER card's own block — the same block the six priced items render in — and
        // taking a stool must not open, close or swap a card. If a future refactor moves the stool onto the
        // table panel, the menu ends up on the card the captain is NOT on and this goes red.
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_deckMode && _barMenu is { } keep)", StringComparison.Ordinal);
        int end = razor.IndexOf("\n    @if (", start + 10, StringComparison.Ordinal);
        string block = razor[start..(end > start ? end : razor.Length)];

        Assert.Contains("@onclick=\"TakeAStool\"", block, StringComparison.Ordinal);
        Assert.Contains("StoolMovesOnTheTable()", block, StringComparison.Ordinal);
        Assert.Contains("@(() => BuyDrink(d))", block, StringComparison.Ordinal);   // …on the SAME card.

        // Take-a-stool picks the seat by ASKING CORE which one is free, and never by walking the row here.
        string take = MethodOf(Stool(), "the stool", "public void TakeAStool()");
        Assert.Contains("TheStools.FirstFreeStool(", take, StringComparison.Ordinal);
        Assert.Contains("ex.CanteenWatch", take, StringComparison.Ordinal);
        Assert.Contains("TheStools.RowIsFullLine", take, StringComparison.Ordinal);   // #603: refused out loud
        Assert.DoesNotContain("BarMenu = null", take, StringComparison.Ordinal);     // …and the bar stays open
        Assert.DoesNotContain("_showBarMenu = false", take, StringComparison.Ordinal);

        // #680/#736 · What the stool answers is written into the slot this card ALREADY draws, never pulsed
        // under the card's own blur. One outcome slot on one card.
        Assert.Contains("_host.BarNotice = TheStools.TookAStoolLine;", take, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPulseMessage", take, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPulseMessage", MethodOf(Stool(), "the stool", "private void StoolWaited("),
            StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPulseMessage", MethodOf(Stool(), "the stool", "private void StoolMove("),
            StringComparison.Ordinal);
    }

    [Fact]
    public void TheNeighbourTurnsOnTheRoomsOwnAnswer_AndOnlyOncePerWatch()
    {
        // The wait beat is #757's machinery, called rather than re-implemented: Core decides whether she
        // turns, the ROOM remembers the beats (so getting down and back up is not a re-roll), and one
        // approach per stool per watch — she turned to you, and whichever way that went, it went.
        //
        // RED PROOF: delete the `!ex.TableApproached.Contains(s.Key) &&` clause and the guard's own
        // one-approach assertion fails; delete the TheStools.SomebodyTurns call and it fails naming it.
        string waited = MethodOf(Stool(), "the stool", "private void StoolWaited(");

        Assert.Contains("TheStools.SomebodyTurns(", waited, StringComparison.Ordinal);
        Assert.Contains("ex.TableWaits[s.Key] = beat + 1;", waited, StringComparison.Ordinal);
        Assert.Contains("!ex.TableApproached.Contains(s.Key)", waited, StringComparison.Ordinal);
        Assert.Contains("ex.TableApproached.Add(s.Key);", waited, StringComparison.Ordinal);
        Assert.Contains("_host.NeighbourCheat", waited, StringComparison.Ordinal);

        // …and the silence is told, from the pool the ROOM picks — which of the three it is depends on
        // whether anybody is actually beside you, asked of Core rather than guessed here.
        Assert.Contains("TheStools.NobodySpoke(", waited, StringComparison.Ordinal);
        Assert.Contains("TheStools.HasNeighbour(", waited, StringComparison.Ordinal);

        // A wait is a beat inside the FROZEN watch (#709) and never a nudge to the clock: a wait that
        // re-dated the room would make the drawn room and the pressed room two rooms.
        Assert.DoesNotContain("CanteenWatch =", waited, StringComparison.Ordinal);
        Assert.DoesNotContain("CanteenWatch++", waited, StringComparison.Ordinal);
    }

    [Fact]
    public void ARungIsClimbedOnce_AndLettingItLieEndsTheVisitRatherThanTheSitting()
    {
        // FOUND BY PLAYING IT, in a browser, at /map?stool=1&neighbour=1 — which is the only way it was ever
        // going to be found. Every rung of her ladder is a Requirement.Free reply carrying a fixed line, so
        // Encounter.Available said YES to all of them for ever: STAND HER ONE could be pressed as many times
        // as you liked, debiting 7 cr and re-telling the same sentence each time. A conversation whose
        // sentences can be re-said is not a conversation, and a paid one is a leak.
        //
        // RED PROOF: drop the `!s.Said.Contains(move.Id)` clause and this fails.
        string offer = MethodOf(Stool(), "the stool", "public bool StoolMoveOnOffer(");
        Assert.Contains("!s.Said.Contains(move.Id)", offer, StringComparison.Ordinal);
        Assert.Contains("_host.Credits >= move.Credits", offer, StringComparison.Ordinal);

        // …and WAIT is the exception, because waiting is the whole verb of sitting there and the room
        // answers differently every beat. A climbed-once law that swallowed Wait would turn the stool into a
        // one-press fixture.
        Assert.Contains("move.Id == TheStools.Wait", offer, StringComparison.Ordinal);

        // Refused OUT LOUD and not silently missing (#212/#603): the button is still drawn, and its title
        // says which of the two refusals it is.
        Assert.Contains("already said that", MethodOf(Stool(), "the stool", "public string StoolMoveRefusal("),
            StringComparison.OrdinalIgnoreCase);

        // #757's wave-off, at a counter: letting it lie ends the VISIT and not the sitting. The stool is
        // yours again and the panel never blinks — one occupation of one seat all along.
        string pressed = MethodOf(Stool(), "the stool", "private void StoolMove(");
        Assert.Contains("BackToYourOwnStool(s);", pressed, StringComparison.Ordinal);

        string back = MethodOf(Stool(), "the stool", "private void BackToYourOwnStool(");
        Assert.Contains("s.WithNeighbour = false;", back, StringComparison.Ordinal);
        Assert.Contains("s.Said.Clear();", back, StringComparison.Ordinal);
        // …it does NOT touch the outcome slot: what she said on the way out is the last thing that happened,
        // and wiping it would be #680's bug committed by the fix for something else.
        Assert.DoesNotContain("_host.BarNotice", back, StringComparison.Ordinal);
        // …nor does it get you off the stool, which is a different verb with a different button.
        Assert.DoesNotContain("Stool = null", back, StringComparison.Ordinal);
    }

    [Fact]
    public void TheStoolDevStartsWalkTheWholeRouteAndForceBothAnswers()
    {
        // Owner's standing rule, and #693's: a scene nobody can reach on demand is a scene that ships
        // broken. Whether the neighbour turns sits behind a seeded roll AND a seeded occupancy, so both
        // halves need a lever or neither is testable.
        Assert.Contains(DevStarts.All, e => e.Url == "/map?stool=1&neighbour=1");
        Assert.Contains(DevStarts.All, e => e.Url == "/map?stool=1&neighbour=0");

        // …and the route's last leg opens the card and takes the seat THROUGH THE VERY HANDLERS the player
        // reaches, so a tester lands in the scene a captain gets — including which stool is free, which is
        // the room's answer and never the cheat's.
        string stand = Method("Map.Surface.Cheats.cs", "private void StandAtTheCounterIfAsked(");
        Assert.Contains("_stoolCheat", stand, StringComparison.Ordinal);
        // #781 · …through Core's own who-is-behind-it fork, which is the very call the real [E] press makes.
        // What this claim protects is unchanged — the card is opened by the shipped handler and the seat is
        // taken by the shipped verb — and the argument is now the counter the WATCH says is serving, because
        // a cheat that opened the unforked card would be demoing a counter that does not ship.
        Assert.Contains("OpenCounterService(Core.Interior.CounterService.OnWatch(counter, ex.CanteenWatch));",
            stand, StringComparison.Ordinal);
        Assert.Contains("TakeAStool();", stand, StringComparison.Ordinal);
    }

    // ── #780 · THE MENU IS ON THE SCREEN, NOT MERELY IN THE DOM ─────────────────────────────────────────

    [Fact]
    public void TheMenuIsInTheCardsBodyAndNotUnderThePinnedFootsScrim()
    {
        // THE LIVE BUG, and the reason #772's guards were green over it. Owner, at the B1 counter:
        // "How do I buy the drink here?... There is no button to pay there?" and "menu is not readable —
        // text and background are both dark."
        //
        // Both sentences are ONE fault and it was never a colour. The menu was the LAST child of the counter
        // card, AFTER .deck-offer-actions — which #735 pins with `position: sticky; z-index: 3` and a 12rem
        // box-shadow SCRIM whose entire job is to darken whatever slides under the pinned foot. A static
        // sibling that follows a positioned one paints beneath it, so the six priced items sat under 92%
        // black: in the DOM, hit-testable, and read by a captain as a greyed-out panel behind glass.
        //
        // #772's placement guard asked whether the RECEIPT was inside the card's block. It was. So was the
        // menu. "Inside the block" was never the claim that mattered — WHERE inside was.
        //
        // RED PROOF: move the `@if (_showBarMenu)` block back below `<div class="deck-offer-actions">` and
        // this fails naming the scrim.
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_deckMode && _barMenu is { } keep)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the counter card block this guard knows how to find.");
        int end = razor.IndexOf("\n    @if (", start + 10, StringComparison.Ordinal);
        string block = razor[start..(end > start ? end : razor.Length)];

        int menuAt = block.IndexOf("class=\"bar-menu", StringComparison.Ordinal);
        int footAt = block.IndexOf("<div class=\"deck-offer-actions\">", StringComparison.Ordinal);
        int buyAt = block.IndexOf("@(() => BuyDrink(d))", StringComparison.Ordinal);

        Assert.True(menuAt >= 0, "the counter card no longer renders a .bar-menu at all.");
        Assert.True(footAt >= 0, "the counter card no longer has the pinned .deck-offer-actions foot.");
        Assert.True(buyAt >= 0, "the counter card no longer has a buy button on its menu rows.");

        Assert.True(menuAt < footAt,
            "the drinks menu renders AFTER the card's sticky .deck-offer-actions foot, which paints a 12rem "
            + "0.92-alpha scrim over everything below it (Map.razor.css, #735's pinned-foot rule). That is "
            + "the owner's 'no button to pay' and 'text and background are both dark', and it is one fault.");
        Assert.True(buyAt < footAt,
            "the menu's own buy buttons render below the pinned foot — the captain cannot see what they are "
            + "pressing even when the browser will let them press it.");

        // …and the CARD is the thing that scrolls, which is the other half of the owner's standing rule:
        // content that does not fit SCROLLS and never shrinks. The counter card is in #735's capped,
        // scrolling family by name, so an illustrated six-item card on a phone is reachable rather than
        // squashed. If .deck-offer-card ever leaves that list, six drinks and five photographs are a
        // softlock.
        string css = PagesCss();
        int family = css.IndexOf(".deck-offer-card,", StringComparison.Ordinal);
        Assert.True(family >= 0,
            "Map.razor.css no longer lists .deck-offer-card in #735's capped-and-scrolling card family, so a "
            + "menu taller than the window has nowhere to go.");
        Assert.Contains("overflow-y: auto", css[family..(family + 900)], StringComparison.Ordinal);
    }

    [Fact]
    public void TheMenuIsReadableAndItsPicturesDoNotEatItsWords()
    {
        // #780/#782 · THE CONTRAST AND THE SIZE, asserted as SHAPE. Owner's law, filed the same evening:
        // text has good contrast against WHAT IT SITS ON, fonts are big enough, and content that does not
        // fit scrolls rather than shrinking.
        //
        // The old menu was `class="bar-menu small"` — 0.875em of light grey at 0.6 alpha over whatever
        // happened to be behind the card. Two of those three were fine on a bar card and none of them were
        // fine under a scrim; the fix is that the panel stops depending on what is behind it at all.
        //
        // RED PROOF: put `small` back on the div, or drop the menu's own background back to 0.6 alpha, and
        // this fails.
        string razor = Pages("Map.razor");
        Assert.DoesNotContain("class=\"bar-menu small\"", razor, StringComparison.Ordinal);

        string css = PagesCss();
        string menu = Rule(css, ".bar-menu {");
        Assert.Contains("font-size:", menu, StringComparison.Ordinal);
        Assert.True(SizeRem(menu, "font-size:") >= 0.9,
            $"the menu sets font-size {SizeRem(menu, "font-size:")}rem — below the size the owner asked to "
            + "be able to read on a phone.");
        Assert.True(Alpha(menu, "background:") >= 0.9,
            $"the menu's own ground is {Alpha(menu, "background:")} opaque, so its contrast is decided by "
            + "whatever is drawn behind the card rather than by the menu.");

        // #782 · The picture stands BESIDE the words and cannot grow into them: a fixed box, a text column
        // that is allowed to shrink, and — the load-bearing one — min-width: 0, without which a flex child
        // refuses to go narrower than its longest word and pushes the whole row off a phone.
        string art = Rule(css, ".bar-menu-art {");
        Assert.Contains("flex: 0 0 auto", art, StringComparison.Ordinal);
        Assert.Contains("object-fit: cover", art, StringComparison.Ordinal);
        string words = Rule(css, ".bar-menu-words {");
        Assert.Contains("min-width: 0", words, StringComparison.Ordinal);

        // …and the art is OPTIONAL on the item, so an unillustrated row draws as a row and not as a hole.
        Assert.Contains("d.ArtUrl is { } dishArt", razor, StringComparison.Ordinal);
        Assert.Contains("onerror=\"this.style.display='none'\"", razor, StringComparison.Ordinal);

        // The negative control that makes all of the above mean something: the card the owner is looking at
        // really does have pictures on it, and really does have one row without one.
        var card = SpaceSails.Core.Interior.CounterService.Card;
        Assert.True(card.Count(d => d.ArtUrl is not null) >= 5,
            "fewer than five of the counter's items carry art — this guard is measuring an unillustrated menu.");
        Assert.Contains(card, d => d.ArtUrl is null);
        foreach (Drink d in card)
        {
            if (d.ArtUrl is { } url)
            {
                Assert.True(File.Exists(ArtFile(url)), $"{d.Name} points at {url}, and no such file shipped.");
            }
        }
    }

    /// <summary>The scoped stylesheet, read the way <see cref="Pages"/> reads the markup.</summary>
    private static string PagesCss() => Pages("Map.razor.css");

    /// <summary>One CSS rule's body, by its selector line.</summary>
    private static string Rule(string css, string selector)
    {
        int at = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(at >= 0, $"Map.razor.css no longer has a `{selector}` rule for this guard to read.");
        int close = css.IndexOf('}', at);
        return css[at..(close > at ? close : css.Length)];
    }

    /// <summary>The rem value of a declaration in a rule body, or 0 when it names none.</summary>
    private static double SizeRem(string rule, string property)
    {
        int at = rule.IndexOf(property, StringComparison.Ordinal);
        if (at < 0)
        {
            return 0;
        }
        string value = rule[(at + property.Length)..];
        value = value[..value.IndexOf(';')];
        int rem = value.IndexOf("rem", StringComparison.Ordinal);
        if (rem < 0)
        {
            return 0;
        }
        int from = rem;
        while (from > 0 && (char.IsDigit(value[from - 1]) || value[from - 1] == '.'))
        {
            from--;
        }
        return double.TryParse(value[from..rem], System.Globalization.CultureInfo.InvariantCulture, out double v)
            ? v : 0;
    }

    /// <summary>The alpha of an rgba() in a declaration, or 1 when the colour is opaque by construction.</summary>
    private static double Alpha(string rule, string property)
    {
        int at = rule.IndexOf(property, StringComparison.Ordinal);
        Assert.True(at >= 0, $"the rule declares no `{property}` for this guard to read.");
        string value = rule[(at + property.Length)..];
        value = value[..value.IndexOf(';')];
        int rgba = value.IndexOf("rgba(", StringComparison.Ordinal);
        if (rgba < 0)
        {
            return 1.0;
        }
        string[] parts = value[(rgba + 5)..value.IndexOf(')', rgba)].Split(',');
        return double.TryParse(parts[^1], System.Globalization.CultureInfo.InvariantCulture, out double a) ? a : 1.0;
    }

    /// <summary>Where a wwwroot art url lives on disk, from the test's own working directory.</summary>
    private static string ArtFile(string url)
    {
        for (DirectoryInfo? d = new(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "src", "SpaceSails.Client", "wwwroot", url);
            if (Directory.Exists(Path.Combine(d.FullName, "src", "SpaceSails.Client", "wwwroot")))
            {
                return candidate;
            }
        }
        return url;
    }

    // ── (c) THE HALL WEARS ITS ART ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheHallFloorPlanCarriesTheBackdropOverTheHallsOwnBox()
    {
        // The PLAN half. HiveInterior handed DeckPlan a bare `[]` for backdrops on every floor of every site
        // ever generated, so the biggest social room in the game drew as grid.
        var wrong = new List<string>();
        int painted = 0;
        int headOffices = 0;

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            DeckPlan deck = DeckFor(body, level);
            UndergroundComplex.Amenity canteen = UndergroundComplex
                .Build(body, level, Field).Amenities
                .Single(a => a.Use == UndergroundComplex.Comfort.UpperCanteen);
            UndergroundComplex.Hall hall = canteen.Hall
                ?? throw new InvalidOperationException($"{body} B{-level} carved no hall");

            // THE HEAD OFFICE IS NOT A BRANCH OFFICE, and this guard found that out the hard way: the ice
            // moon's B1 is a DINING ROOM · GUESTS & DEPUTATIONS (#411), not CANTEEN 1, and its picture has
            // not been shot. Its floor must stay bare rather than borrow a branch canteen's frame — the
            // #755 art is a room with poured pillars and working people in it, and the head office
            // outclasses the branch offices in their own vocabulary.
            if (UndergroundComplex.IsHeadOffice(body))
            {
                if (deck.Backdrops.Length != 0)
                {
                    wrong.Add($"  {body} B{-level}: the HEAD OFFICE's dining room is wearing a branch canteen's art.");
                }
                headOffices++;
                continue;
            }

            // #780 · TWO PICTURES NOW, AND THEIR ORDER IS HALF THE FEATURE. The hall's own floor art first
            // and the counter's spot art second — DeckView walks this array in order, so a counter laid down
            // first would be papered back over by the room and neither picture would be seen.
            //
            // #759 · …and behind the glass there is a THIRD room, whose own floor wears its own picture cut
            // into panels (Core's ParkArtPanels) across a room six times wider than it is deep. The two
            // pictures this guard is about are still the first two and still in that order; the park's are
            // COUNTED rather than ignored, so a renderer that drew four of the hall's is still caught.
            int parkPanels = UndergroundComplex.Build(body, level, Field).Park is { } green
                ? UndergroundComplex.ParkArtPanels(green).Count
                : 0;

            if (deck.Backdrops.Length != 2 + parkPanels)
            {
                wrong.Add($"  {body} B{-level}: {deck.Backdrops.Length} backdrop(s) on the hall floor, and "
                    + $"the rooms on it account for {2 + parkPanels}.");
                continue;
            }

            DeckPlan.Backdrop bd = deck.Backdrops[0];
            if (bd.Url != UndergroundComplex.CantinaHallArtUrl)
            {
                wrong.Add($"  {body} B{-level}: the floor wears “{bd.Url}”.");
                continue;
            }
            foreach (DeckPlan.Backdrop beyond in deck.Backdrops.Skip(2))
            {
                if (beyond.Url != UndergroundComplex.ParkArtUrl)
                {
                    wrong.Add($"  {body} B{-level}: an unaccounted picture “{beyond.Url}” is on the floor.");
                }
            }

            // The rectangle is the hall's OWN box — top-left at (X0, Y1), because deck +y is up and that is
            // the ship's own convention (the CANTINA's backdrop Y is ShipLayout's y1). A renderer measuring
            // its own rectangle for a room Core carved is §13.15's second cause.
            if (Math.Abs(bd.X - hall.X0) > 0.01f
                || Math.Abs(bd.Y - hall.Y1) > 0.01f
                || Math.Abs(bd.W - (hall.X1 - hall.X0)) > 0.01f
                || Math.Abs(bd.H - (hall.Y1 - hall.Y0)) > 0.01f)
            {
                wrong.Add(
                    $"  {body} B{-level}: art drawn at ({bd.X:F2},{bd.Y:F2}) {bd.W:F2}×{bd.H:F2} over a hall "
                    + $"of ({hall.X0:F2},{hall.Y1:F2}) {hall.X1 - hall.X0:F2}×{hall.Y1 - hall.Y0:F2}.");
            }

            if (bd.W <= 0 || bd.H <= 0)
            {
                wrong.Add($"  {body} B{-level}: the backdrop has no area — it would draw nothing at all.");
            }

            if (Math.Abs(bd.Alpha - UndergroundComplex.HallArtAlpha) > 0.001f)
            {
                wrong.Add($"  {body} B{-level}: alpha {bd.Alpha}, and the building says {UndergroundComplex.HallArtAlpha}.");
            }

            // ── #780 · AND THE BAR DESK IS AT THE BAR DESK'S SPOT ───────────────────────────────────────
            //
            // Owner, live: "see how in the space bars we have the image of bar desk at the spot where the
            // bar desk is."
            //
            // THE CLAIM IS A RECTANGLE, so this measures a rectangle. The counter is three wall segments
            // Core lays at the far end of the hall (CarveHall), and the picture has to stand on exactly the
            // box those three segments enclose — not "somewhere in the hall", not "a band across the top",
            // and above all not a rectangle this test worked out for itself, which would make it a second
            // opinion about a room neither it nor the renderer carved.
            //
            // So the box is read off the WALLS THE FLOOR SHIPPED. Whatever CarveHall did, the four numbers
            // below came out of the same segments the captain walks into, and the day the hall is recarved
            // this guard moves with it.
            DeckPlan.Backdrop spot = deck.Backdrops[1];
            if (spot.Url != UndergroundComplex.CounterArtUrl)
            {
                wrong.Add($"  {body} B{-level}: the counter's spot wears “{spot.Url}”.");
            }

            if (CounterBoxFromTheWalls(body, level, hall) is not { } bar)
            {
                wrong.Add($"  {body} B{-level}: could not find the counter's own walls to measure against.");
            }
            else if (Math.Abs(spot.X - bar.X0) > 0.01f
                || Math.Abs(spot.Y - bar.Y1) > 0.01f
                || Math.Abs(spot.W - (bar.X1 - bar.X0)) > 0.01f
                || Math.Abs(spot.H - (bar.Y1 - bar.Y0)) > 0.01f)
            {
                wrong.Add(
                    $"  {body} B{-level}: the bar desk is drawn at ({spot.X:F2},{spot.Y:F2}) "
                    + $"{spot.W:F2}×{spot.H:F2}, and the counter's own walls enclose ({bar.X0:F2},{bar.Y1:F2}) "
                    + $"{bar.X1 - bar.X0:F2}×{bar.Y1 - bar.Y0:F2}.");
            }

            // …and it is FURNITURE, not more wallpaper. Harder than the floor it stands on, by the
            // building's own two constants — the alpha is the whole of "reads as an object" and a spot that
            // drifted to the hall's 0.72 would be a second sheet of wallpaper with a different photo on it.
            if (Math.Abs(spot.Alpha - UndergroundComplex.SpotArtAlpha) > 0.001f)
            {
                wrong.Add($"  {body} B{-level}: the counter is drawn at alpha {spot.Alpha}.");
            }
            // ANTI-VACUITY, both ways. The counter is a BAND across one end of a hall, so a spot that had
            // quietly become the hall's own box (the easiest way for this feature to rot into #756's) would
            // pass every equality above if the walls moved with it. It is strictly inside the hall, it has
            // real area, and it is a minority of the floor.
            if (spot.W <= 0 || spot.H <= 0)
            {
                wrong.Add($"  {body} B{-level}: the counter's spot has no area — it would draw nothing.");
            }
            if (spot.H >= bd.H * 0.5f)
            {
                wrong.Add(
                    $"  {body} B{-level}: the bar desk is {spot.H:F1} du deep in a hall {bd.H:F1} du deep — "
                    + "that is a room's worth of picture, not a piece of furniture.");
            }
            if (spot.X < bd.X - 0.01f || spot.X + spot.W > bd.X + bd.W + 0.01f
                || spot.Y > bd.Y + 0.01f || spot.Y - spot.H < bd.Y - bd.H - 0.01f)
            {
                wrong.Add($"  {body} B{-level}: the counter's picture hangs outside the hall it is in.");
            }

            painted++;

            // The negative control, on the same site: an ordinary floor with no hall wears nothing. Without
            // this, a renderer that painted EVERY floor would pass everything above.
            DeckPlan lobby = DeckFor(body, -2);
            if (lobby.Backdrops.Length != 0)
            {
                wrong.Add($"  {body} B2: {lobby.Backdrops.Length} backdrop(s) on a floor with no hall on it.");
            }
        }

        // …and FURNITURE IS HARDER THAN FLOOR, which is the whole of "it must read as an object and not as
        // wallpaper". Two constants, one inequality, stated where it can go red — a spot that drifted down
        // to the hall's 0.72 would be a second sheet of wallpaper with a different photograph on it.
        float furniture = UndergroundComplex.SpotArtAlpha, floorArt = UndergroundComplex.HallArtAlpha;
        Assert.True(furniture > floorArt,
            $"the building says furniture ({furniture}) is no harder than floor ({floorArt}).");

        Assert.True(painted >= 5, $"only {painted} painted halls in the sweep — this guard swept nothing.");
        Assert.True(headOffices >= 1, "the sweep never met a head office, so its bare-floor arm proved nothing.");
        Assert.True(wrong.Count == 0, $"the hall's art is wrong:{Environment.NewLine}{string.Join(Environment.NewLine, wrong)}");
    }

    [Fact]
    public void TheRendererActuallyDrawsIt_UnderEverythingElse()
    {
        // The RENDERER half — "the plan has to flag it AND the renderer has to honour the flag", the house's
        // own third-idiom law. A backdrop in the plan that DeckView never reached would be a green test over
        // the very bare grid the owner reported.
        DeckPlan deck = DeckFor("europa", UndergroundComplex.TopPressurisedFloor("europa")!.Value);
        List<Mark> marks = Frame(deck);

        List<Mark> images = [.. marks.Where(m => m.Kind == "image")];
        // #759 · The two pictures below are the hall's and the counter's, in that order. Everything after
        // them is the PARK's floor, behind the glass — counted off Core's own cut, so this cannot pass on a
        // renderer that quietly drew four of the hall's instead.
        int parkPanels = UndergroundComplex.ParkArtPanels(
            UndergroundComplex.Build("europa", UndergroundComplex.TopPressurisedFloor("europa")!.Value, Field)
                .Park!.Value).Count;

        Assert.True(images.Count == 2 + parkPanels,
            $"{images.Count} image(s) drawn on the floor, and the rooms on it account for {2 + parkPanels}.");
        Assert.Equal(UndergroundComplex.CantinaHallArtUrl, images[0].Url);
        Assert.All(images.Skip(2), m => Assert.Equal(UndergroundComplex.ParkArtUrl, m.Url));
        Assert.True(images[0].W > 0 && images[0].H > 0, "the art was drawn with no area.");
        Assert.True(Math.Abs(images[0].Alpha - UndergroundComplex.HallArtAlpha) < 0.001f,
            $"drawn at alpha {images[0].Alpha}.");

        // #780 · THE BAR DESK REACHES THE PEN, AND IT REACHES IT SECOND. The plan-half guard above proves
        // the rectangle; this proves DeckView actually draws it, in the order that makes it visible — a spot
        // drawn BEFORE the hall's own art would be covered by the room's wallpaper on the very next call,
        // which is a green plan over an invisible counter.
        Assert.Equal(UndergroundComplex.CounterArtUrl, images[1].Url);
        Assert.True(images[1].W > 0 && images[1].H > 0, "the bar desk was drawn with no area.");
        Assert.True(Math.Abs(images[1].Alpha - UndergroundComplex.SpotArtAlpha) < 0.001f,
            $"the bar desk drew at alpha {images[1].Alpha}.");
        Assert.True(images[1].H < images[0].H,
            "the bar desk drew as tall as the whole hall — that is wallpaper, not furniture.");

        // LEGIBILITY IS THE WHOLE CONSTRAINT. The list is in draw order, so "the picture is under the room"
        // is simply: no wall, no plate and no console dot was laid down before it.
        int lastImage = marks.FindLastIndex(m => m.Kind == "image");
        int firstStroke = marks.FindIndex(m => m.Kind is "polyline" or "text" or "circle");
        Assert.True(firstStroke > lastImage,
            $"the first vector mark landed at {firstStroke} and the LAST picture at {lastImage} — a picture "
            + "is drawing OVER the room. #780 raised the counter's alpha to 0.96, so a spot that also drew "
            + "over the walls would hide the very furniture it is a picture of.");

        // And the room is still there to be drawn over: a frame with nothing in it would pass the line above.
        Assert.True(marks.Count(m => m.Kind == "polyline") > 20,
            "the hall frame has almost no strokes in it — this guard is measuring an empty screen.");

        // Negative control: an ordinary floor draws no picture at all.
        Assert.DoesNotContain(Frame(DeckFor("europa", -2)), m => m.Kind == "image");
    }

    /// <summary>
    /// #780 · THE COUNTER'S OWN BOX, RECONSTRUCTED FROM THE WALLS THE FLOOR SHIPPED.
    ///
    /// <para>The point of this method is that it does NOT know the counter's arithmetic. It knows the SHAPE
    /// a counter is — a long segment across one end of the hall with a short segment running from each of
    /// its ends to the hall's far edge, enclosing the service side nobody may walk into — and it finds the
    /// one rectangle in the floor's wall list that is that shape.</para>
    ///
    /// <para>So the guard measures the picture against the FURNITURE THE CAPTAIN COLLIDES WITH, which is the
    /// only claim the owner's sentence actually makes. A guard that recomputed
    /// <c>length - HallCounterBandDu</c> here would be asserting that two copies of one formula agree, which
    /// is the fifth bug class with a ruler in its hand.</para>
    /// </summary>
    private static (double X0, double Y0, double X1, double Y1)? CounterBoxFromTheWalls(
        string body, int level, UndergroundComplex.Hall hall)
    {
        IReadOnlyList<SurfaceLayout.Wall> walls =
            UndergroundComplex.Build(body, level, Field).Walls;

        static bool Near(double a, double b) => Math.Abs(a - b) < 0.01;

        // ONLY THE HALL'S OWN WALLS. FloorPlan.Walls is the whole floor — twenty poured rooms off a rib —
        // and the first draft of this method happily found a store cupboard three rooms away that happened
        // to be the same shape. A guard that measured the wrong room would have been §13.15 inside the very
        // test written to catch §13.15.
        bool InTheHall(SurfaceLayout.Wall w) =>
            Math.Min(w.X1, w.X2) >= hall.X0 - 0.01 && Math.Max(w.X1, w.X2) <= hall.X1 + 0.01
            && Math.Min(w.Y1, w.Y2) >= hall.Y0 - 0.01 && Math.Max(w.Y1, w.Y2) <= hall.Y1 + 0.01;

        var mine = walls.Where(InTheHall).ToList();

        foreach (SurfaceLayout.Wall run in mine)
        {
            // A horizontal run, strictly inside the hall's y-range and not one of the hall's own edges.
            if (!Near(run.Y1, run.Y2) || Near(run.X1, run.X2))
            {
                continue;
            }
            double yc = run.Y1;
            if (yc <= hall.Y0 + 0.01 || yc >= hall.Y1 - 0.01)
            {
                continue;
            }
            double xa = Math.Min(run.X1, run.X2), xb = Math.Max(run.X1, run.X2);

            // …with a stub at each end running to ONE of the hall's two far edges. That last clause is what
            // makes this the COUNTER and not a cabinet: a cabinet is a box in the middle of a wall, and the
            // counter is the one fixture that shuts off a whole end of the room.
            foreach (double edge in new[] { hall.Y0, hall.Y1 })
            {
                bool left = false, right = false;
                foreach (SurfaceLayout.Wall stub in mine)
                {
                    if (!Near(stub.X1, stub.X2) || Near(stub.Y1, stub.Y2))
                    {
                        continue;
                    }
                    double lo = Math.Min(stub.Y1, stub.Y2), hi = Math.Max(stub.Y1, stub.Y2);
                    if (!Near(lo, Math.Min(yc, edge)) || !Near(hi, Math.Max(yc, edge)))
                    {
                        continue;
                    }
                    left |= Near(stub.X1, xa);
                    right |= Near(stub.X1, xb);
                }
                if (left && right)
                {
                    return (xa, Math.Min(yc, edge), xb, Math.Max(yc, edge));
                }
            }
        }
        return null;
    }

    // ── The recording pen ───────────────────────────────────────────────────────────────────────────────
    //
    // The house's own RecordingRenderer idiom (TheHallsAreDrawnInAThirdIdiomTests), widened to keep the url,
    // the size and the alpha — because "an image was drawn" is not the claim being made here. Which picture,
    // how big and how opaque are the three things this issue is about.
    private sealed record Mark(string Kind, float X, float Y, string? Url, float W, float H, float Alpha);

    private sealed class RecordingRenderer : IRenderer
    {
        public List<Mark> Marks { get; } = [];

        private readonly List<string> _images = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Marks.Clear();

        public void EndFrame()
        {
        }

        public int RegisterImage(string url)
        {
            int at = _images.IndexOf(url);
            if (at < 0)
            {
                _images.Add(url);
                at = _images.Count - 1;
            }
            return at + 1;
        }

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new("circle", x, y, null, r, r, 1f));

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new("polyline", pts.Length > 0 ? pts[0] : 0f, pts.Length > 1 ? pts[1] : 0f, null, 0f, 0f, 1f));

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new("polygon", pts.Length > 0 ? pts[0] : 0f, pts.Length > 1 ? pts[1] : 0f, null, 0f, 0f, 1f));

        public void DrawText(
            float x, float y, string text, RgbaColor c,
            string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Marks.Add(new("text", x, y, text, 0f, 0f, 1f));

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) =>
            Marks.Add(new("image", x, y, UrlOf(id), w, h, a));

        public void DrawImageSlice(
            int id, float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh, float a = 1f) =>
            Marks.Add(new("slice", dx, dy, UrlOf(id), dw, dh, a));

        private string? UrlOf(int id) => id >= 1 && id <= _images.Count ? _images[id - 1] : null;
    }

    private static List<Mark> Frame(DeckPlan plan)
    {
        (double ax, double ay) = HiveInterior.SpawnOn(Field);
        var pen = new RecordingRenderer();
        new DeckView(pen).Draw(
            plan, WidthPx, HeightPx, 0,
            new DeckView.State(ax, ay, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false, Dark: false),
            0, 0, null);
        return pen.Marks;
    }
}
