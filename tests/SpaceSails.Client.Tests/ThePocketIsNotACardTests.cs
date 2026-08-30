using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1027 · <b>THE POCKET IS NOT A CARD, AND IT STOPPED BEING FILED UNDER ONE.</b>
///
/// <para>The boot-every-scene sweep's maiden run: boot <c>?rip=1</c>, dismiss the first-ground briefing,
/// 🛗 THE SHAFT stands over the scene, press <b>I</b> — and nothing visibly happens. The satchel opened. It
/// opened UNDER the card, because the two families shared one class and one z-index (1320) and the tie was
/// broken by nothing but which block was typed first in <c>Map.razor</c>: the satchel's at ~2480, the
/// arrival card's at ~3550. With a second card queued behind the first the key looked completely dead —
/// the #603 class, a control that quietly does nothing.</para>
///
/// <para><b>The fix, and the fix that was declined.</b> The pocket could have DISMISSED or DEFERRED the
/// cards it was buried under. It does not, and this file pins why: the arrival beats are told ONCE and their
/// latches are set at the moment they are raised (<c>HiveCantinaHallShown</c>, <c>HiveFloorsSeen</c>,
/// <c>HiveUnlistedPlateShown</c>), so a satchel that waved them away would silently spend a beat the captain
/// never read — the owner's own pop-up ruling broken by the thing meant to honour it. Deferring instead
/// needs a re-show queue for <c>_viewObject</c>, which #693 declined and
/// <c>Map.Surface.Satchel.cs</c> still records as declined. So the pocket simply gets a LATER PAINT SLOT
/// and destroys nothing.</para>
///
/// <para><b>What is here and what is not.</b> The pixels are
/// <c>SpaceSails.UiGate.TheSatchelPaintsOverTheCardTests</c>'s job — it boots the real <c>?rip=1</c>, presses
/// I and asks <c>elementFromPoint</c> what is actually on top, which is the only witness that can tell this
/// bug from its fix. This is the browser-free twin: the band arithmetic, the class the markup wears, the
/// cascade order that makes it stick, and the two keyboard chains — asserted the day they are typed rather
/// than the day somebody thinks to drive to them.</para>
/// </summary>
public sealed class ThePocketIsNotACardTests
{
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";

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

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    /// <summary>One method's body, cut at the next member declaration — the idiom every source guard on this
    /// ground already uses, so a body read here is a body read there.</summary>
    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        Match stop = Regex.Match(src[(at + 1)..], @"\n\s*(private|public) ");
        int end = stop.Success ? at + 1 + stop.Index : -1;
        return src[at..(end > at ? end : src.Length)];
    }

    /// <summary>Where a line first appears in a body, asserted to exist so an ordering claim below can never
    /// be made about a line that is not there at all (two −1s compare equal and prove nothing).</summary>
    private static int RungAt(string body, string line, string what)
    {
        int at = body.IndexOf(line, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{what} is not in this chain at all: `{line}`");
        return at;
    }

    /// <summary>
    /// THE BAND, WHICH IS THE WHOLE FIX. One step over the cards it was tying with, and still under the
    /// reserved lifeline and the whole modal band — a pocket may cover a flavour picture and may never cover
    /// a stranded captain's rescue button, a death, or the first-ground briefing.
    /// </summary>
    /// <remarks>RED if <c>SatchelBackdrop</c> is set back to +120 (the tie this issue is about) or pushed
    /// past +140 (which would take it over <c>DistressLifeline</c>).</remarks>
    [Fact]
    public void ThePocketOutRanksTheCardsAndStillBowsToTheLifeline()
    {
        Assert.True(OverlayBands.SatchelBackdrop > OverlayBands.ViewObjectBackdrop,
            $"the satchel is at {OverlayBands.SatchelBackdrop} and the cards at "
            + $"{OverlayBands.ViewObjectBackdrop} — a tie is what #1027 IS, and the tie-break is then "
            + "whichever block happens to be typed first in Map.razor.");
        Assert.True(OverlayBands.SatchelBackdrop > OverlayBands.PinBackdrop);
        Assert.True(OverlayBands.SatchelBackdrop < OverlayBands.DistressLifeline,
            "the pocket may never out-rank the distress lifeline — a captain reading his own satchel must "
            + "still be able to see the button that calls the tow (#293/#299).");
        Assert.True(OverlayBands.SatchelBackdrop < OverlayBands.Modal,
            "the pocket may never out-rank the modal band: a death, a crossing and the first-ground family "
            + "take the screen off it.");
    }

    /// <summary>
    /// THE MARKUP WEARS BOTH CLASSES, and the modifier is the SECOND of them. The satchel keeps the family
    /// root (<c>view-object-backdrop</c>) so it keeps the family's looks and so #992's recogniser goes on
    /// finding a registered surface in the same attribute — the <c>rep-backdrop</c> / <c>selfie-backdrop</c>
    /// idiom. What it stops sharing is the layer.
    /// </summary>
    /// <remarks>RED if the modifier is dropped: the backdrop falls back to 1320 and the bug returns.</remarks>
    [Fact]
    public void TheSatchelsBackdropWearsItsOwnModifier()
    {
        string razor = Pages("Map.razor");
        int at = razor.IndexOf("@if (_showSatchel)", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.razor no longer opens the satchel where this guard can read it.");

        string block = razor[at..Math.Min(razor.Length, at + 600)];
        Assert.Contains(
            "<div class=\"view-object-backdrop satchel-backdrop\"", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// AND THE CASCADE MAKES IT STICK. Both rules are one class deep, so they have equal specificity and
    /// source order decides — exactly the mechanism that caused the bug, used deliberately this time. A
    /// <c>.satchel-backdrop</c> rule typed ABOVE <c>.view-object-backdrop</c> would be silently overridden
    /// by it and the pocket would sink back under the cards with the stylesheet still mentioning 1330.
    /// </summary>
    /// <remarks>RED if the two blocks are swapped, and RED if the modifier's rule is deleted.</remarks>
    [Fact]
    public void TheModifiersRuleIsWrittenAfterTheFamilysOwn()
    {
        string css = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));

        int family = css.IndexOf(".view-object-backdrop {", StringComparison.Ordinal);
        int modifier = css.IndexOf(".satchel-backdrop {", StringComparison.Ordinal);

        Assert.True(family >= 0, "Map.razor.css no longer declares .view-object-backdrop.");
        Assert.True(modifier >= 0,
            "Map.razor.css no longer declares .satchel-backdrop — without it the pocket shares the cards' "
            + "z-index again and #1027 is back.");
        Assert.True(modifier > family,
            "the .satchel-backdrop rule is written ABOVE .view-object-backdrop. They are the same "
            + "specificity, so the later one wins: typed in that order the family's 1320 overrides the "
            + "modifier's 1330 and the satchel sinks back under the cards while the stylesheet still says "
            + "otherwise (#1027).");
    }

    /// <summary>
    /// THE CANCEL KEY READS OFF THE SAME ORDER THE STYLESHEET DOES. The chain's own law is "top-most first",
    /// and the satchel had never been in it at all — Esc fell straight through the pocket to whatever card
    /// was underneath. It is listed under the <c>.convergence-backdrop</c> family (1420, which really does
    /// cover it) and above every card at 1330 or below.
    /// </summary>
    /// <remarks>RED with the <c>_showSatchel</c> rung deleted (the first assertion), and RED if it is moved
    /// above the ground family or below the story card.</remarks>
    [Fact]
    public void TheEscChainPeelsThePocketBeforeTheCardsItCovers()
    {
        string chain = Method("Map.Sim.Cancel.cs", "private bool TryDismissTopOverlay()");

        int pocket = RungAt(chain, "if (_showSatchel) { CloseSatchel(); return true; }", "the satchel");
        int lesson = RungAt(chain, "if (_groundLessonOpen)", "the first-ground lesson");
        int air = RungAt(chain, "if (_airCardOpen)", "the low-air card");
        int story = RungAt(chain, "if (_storyCard is not null)", "the story card");
        int viewObject = RungAt(chain, "if (_viewObject is not null)", "the view-object card");

        Assert.True(lesson < pocket && air < pocket,
            "the first-ground family draws at 1420 and genuinely covers the satchel, so Esc must reach it "
            + "first (#1027).");
        Assert.True(pocket < story,
            "the satchel paints over the story card (1330 vs 1320), so Esc must peel the pocket first — "
            + "otherwise the key shuts a card nobody can see and leaves the one on the screen (#1027).");
        Assert.True(pocket < viewObject,
            "…and the same for the arrival/object cards, which are the ones this issue was filed on.");
    }

    /// <summary>
    /// AND THE KEYBOARD'S YES STOPS AT THE POCKET RATHER THAN REACHING PAST IT. The satchel is a PAGE of
    /// many controls, not a card with one visible action, so Enter presses nothing on it — that is this
    /// chain's founding refusal. But falling through would be worse than doing nothing: Enter would
    /// acknowledge an arrival card the captain cannot see, spend the beat, and nothing on his screen would
    /// move.
    /// </summary>
    /// <remarks>RED if the <c>_showSatchel</c> guard is deleted, and RED if it is turned into a rung that
    /// closes the satchel (a key that answers a card it was not asked about).</remarks>
    [Fact]
    public void TheEnterChainStopsAtThePocketAndPressesNothingOnIt()
    {
        string confirm = Method("Map.Sim.Cancel.cs", "private bool TryConfirmTopOverlay()");

        int pocket = RungAt(confirm, "if (_showSatchel) { return false; }", "the satchel's stop");
        int story = RungAt(confirm, "if (_storyCard is not null)", "the story card");
        int viewObject = RungAt(confirm, "if (_viewObject is not null)", "the view-object card");

        Assert.True(pocket < story && pocket < viewObject);
        Assert.DoesNotContain("CloseSatchel", confirm, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE SAME THING, TYPED RATHER THAN READ. A card is standing, the captain opens his pocket over it, and
    /// Escape takes the pocket back off — leaving the card exactly where it was. Nothing is dismissed to
    /// make room for the satchel and nothing is dismissed by closing it, which is the whole of the direction
    /// this fix took over the one it declined.
    /// </summary>
    /// <remarks>RED, run: with the <c>_showSatchel</c> rung out of <c>TryDismissTopOverlay</c> the key falls
    /// straight through the pocket and the first assertion fails on a satchel that is still open.</remarks>
    [Fact]
    public async Task EscapeTakesThePocketBackAndLeavesTheCardStanding()
    {
        using DeskBench bench = await DeskBench.BootAsync(Ashore);

        // The first landing raises the ground briefing, which draws at 1420 and legitimately covers both
        // surfaces this guard is about. Put it away first — the claim here is about the two below it, and a
        // key consumed by a third card would prove nothing about either.
        bench.Poke("_groundLessonOpen", false);

        // The house idiom for typing at this bench: OnKeyDown's first act is to arm the WebAudio context
        // through JS interop, which no bench has. Armed already, the key goes where the player's does.
        bench.Poke("_audioArmed", true);

        bench.Poke("_viewObject", (DeckPlan.ConsoleSpot?)new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, 0f, 0f,
            UndergroundComplex.CantinaHallLabel, null, "the hall, as the captain first sees it"));
        bench.CallOnTheDispatcher("OpenSatchel");

        DeskBench.Painted painted = await bench.RenderAsync();
        Assert.True((bool)bench.Peek("_showSatchel")!, "the pocket was not opened, so this proves nothing.");
        Assert.NotNull(bench.Peek("_viewObject"));

        await bench.TypeAsync(DeskBench.TheKeyboard(painted), "Escape");
        painted = await bench.RenderAsync();

        Assert.False((bool)bench.Peek("_showSatchel")!,
            "Escape did not close the satchel — the cancel key must peel the top-most surface, and with the "
            + "pocket at 1330 the top-most surface is the pocket (#1027).");
        Assert.NotNull(bench.Peek("_viewObject"));
        Assert.Empty(bench.EscapedPastTheGate);
    }
}
