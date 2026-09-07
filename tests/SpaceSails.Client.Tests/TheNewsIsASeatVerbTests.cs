using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1052 (L2) · <b>READING THE NEWS IS A SEAT VERB, AND THE PAPER IT HANDS YOU IS THE ROOM'S.</b>
///
/// <para>Owner, 2026-09-01: <i>"Reading the news pop-up could be option when sitting at table, instead of
/// working the case… Just hope there is a way to have both the newsfeed and pop-up-bar walkers visible UI at
/// the same time… we could get even new breadcrumbs from the news into our detective book."</i></para>
///
/// <para>Four claims, and none of them is the other three. The verb is tied to the CHAIR and not to the
/// place (which is what makes it work where the desk key is refused); the masthead is tied to the place and
/// not to the chair; the panel writes no scrim, which is the whole of the owner's hope; and clipping is the
/// only thing on it that touches the book.</para>
///
/// <para>Everything here drives the SHIPPING seams — the strip's own button through the render tree, the
/// scissors through a real dispatched click, Escape typed at the page's keyboard host. A test that called
/// <c>ClipThisStory</c> by name would prove a method files a note and say nothing at all about whether
/// anything on the screen reaches it, which is #992's own lesson.</para>
/// </summary>
[SlowGate] // #251 · 42 s over 9 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheNewsIsASeatVerbTests
{
    /// <summary>A CABINET TOP IN THE B1 CANTEEN OF A DEEP SITE — the seat under a facility, on an
    /// excursion, where the digit keys are a tube ride away. <c>?spread=1</c> is the shipped dev row for it;
    /// the <c>?dock=</c> is the bench's own landing horizon and nothing else (see DeskBench's class note).
    /// </summary>
    private const string SatInALabCanteen = "/map?dock=selene-gate&spread=1";

    /// <summary>A FREE TOP IN A DOCKED BERTH'S BAR, with no excursion anywhere in the world — #1016's own
    /// dev row, and the one world in this file where key 6 is not refused.</summary>
    private const string SatAtABarTop = "/map?barcase=1";

    /// <summary>Nobody sitting anywhere: her own deck, under way.</summary>
    private const string FreeFlying = "/map?start=wreck";

    // ── (a) THE VERB IS TIED TO THE CHAIR ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE STRIP OFFERS THE PAPER AT A MOON CANTEEN TABLE, WHERE KEY 6 IS RIGHTLY REFUSED.</b>
    ///
    /// <para>This is the seat-tied claim proved at the one seat that can tell it apart from a place-tied
    /// one. Ashore in a berth, "6 opens the galley card" and "the strip offers the news" are indistinguishable
    /// — both work. On an excursion the desk keys refuse by design (<i>"the nav desk is a tube ride away"</i>),
    /// so if the verb had been hung off the ship's news door in any way it would be absent here. It is not:
    /// the button is in the strip's own subtree, at a table under a moon, and the digit key still refuses in
    /// the same breath.</para>
    ///
    /// <para><b>RED PROOF:</b> gate the strip's button on <c>_surface is null</c> and this fails —
    /// <i>"the seated strip at a moon canteen table offers no 📰 Read the news"</i> — while the bar-top row
    /// of <see cref="TheMastheadIsTheChairsPlaceAndNotTheDesks"/> stays green, which is exactly the shape of
    /// a place-tied verb.</para>
    /// </summary>
    [Fact]
    public async Task TheStripOffersThePaperAtAMoonCanteenWhereTheDeskKeyIsRefused()
    {
        using DeskBench bench = await DeskBench.BootAsync(SatInALabCanteen);
        Assert.True(bench.OnSurface, $"{SatInALabCanteen} never reached the ground — there is no seat to test.");
        Assert.True((bool)bench.Call("get_CaptainIsSeated")!, "the dev row did not sit the captain down.");

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node strip = TheStrip(painted)
            ?? throw new Xunit.Sdk.XunitException("no .seated-dock strip was drawn at a canteen table.");

        Assert.True(
            strip.SelfAndDescendants().Any(n => n.Name == SeatedSpread.ReadTheNewsLabel),
            "the seated strip at a moon canteen table offers no "
            + $"{SeatedSpread.ReadTheNewsLabel} — the verb is tied to a place and not to the chair (#1052).");

        // …and the desk key really is refused here, so the claim above is about a seat the ship's own news
        // door cannot reach. Typed at the page, not reasoned about.
        bench.Poke("_audioArmed", true);
        await bench.TypeAsync(DeskBench.TheKeyboard(painted), "6");
        Assert.False((bool)bench.Field("_galleyCardOpen")!,
            "key 6 raised the galley card on an excursion — this world no longer proves anything about a "
            + "seat verb, because the desk verb works here too.");
    }

    /// <summary>
    /// <b>WHICH PAPER THE CHAIR READS IS THE CHAIR'S PLACE, ASKED OF L1'S OWN PURE CALL.</b>
    ///
    /// <para>Three seats, three mastheads, and the client's whole contribution is
    /// <c>TheNewsPlaceHere</c> — four fields handed to <c>NewsWire.ScopeAt</c>. Nothing about a room leaks
    /// into Core beyond them, and nothing in the client decides a masthead for itself.</para>
    ///
    /// <para><b>RED PROOF:</b> drop <c>InsideSecretLab</c> from the excursion branch of
    /// <c>TheNewsPlaceHere</c> and the canteen row fails — <i>"Expected: CompanyIntranet / Actual: PortRag"</i>
    /// — the facility reading the port's rag off its own noticeboard.</para>
    /// </summary>
    [Theory]
    [InlineData(SatInALabCanteen, NewsWire.NewsScope.CompanyIntranet)]
    [InlineData(SatAtABarTop, NewsWire.NewsScope.PortRag)]
    [InlineData(FreeFlying, NewsWire.NewsScope.SystemWire)]
    public async Task TheMastheadIsTheChairsPlaceAndNotTheDesks(string url, NewsWire.NewsScope expected)
    {
        using DeskBench bench = await DeskBench.BootAsync(url);
        var place = (NewsWire.NewsPlace)bench.Call("TheNewsPlaceHere")!;

        Assert.Equal(expected, NewsWire.ScopeAt(place));

        // …and the salt is the site's own id everywhere but aboard, which is what makes two ports read
        // differently on one sim-day (L1's Salt_MakesTheSameDayReadDifferently…).
        Assert.Equal(
            expected == NewsWire.NewsScope.SystemWire ? null : place.SiteBodyId,
            NewsWire.SaltFor(place));
    }

    // ── (b) THE PANEL IS DOCKED, NOT MODAL ────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PAPER RAISES NO SCRIM, AND A REP'S CARD STANDS OVER IT AND STAYS PRESSABLE.</b>
    ///
    /// <para>The owner's hope, proved rather than promised — and proved as an ABSENCE, which is #780's
    /// lesson read in reverse and the same assertion #784's own guard makes about the seated strip: with the
    /// paper open there is no <c>.view-object-backdrop</c> in the DOM AT ALL, so there is nothing that
    /// COULD dim the hall, blur the walkers, or swallow a click meant for the floor.</para>
    ///
    /// <para>Then the second half, which is the one that answers <i>"both… visible UI at the same time"</i>
    /// literally: the salesman reaches the table through his own shipping arrival verb, and the render that
    /// follows carries BOTH surfaces — his card AND the open paper — with a pressable control inside each.
    /// Putting his card down leaves the paper open where it was.</para>
    ///
    /// <para><b>RED PROOF:</b> wrap the <c>.seated-news</c> block in a
    /// <c>&lt;div class="view-object-backdrop"&gt;</c> and the first assertion fails naming the class; drop
    /// <c>OverlayBands.SeatedNewsPanel</c> to <c>+140</c> and <c>CssZBandSyncTests</c> fails instead, which
    /// is the other half of the same claim.</para>
    /// </summary>
    [Fact]
    public async Task TheOpenPaperWritesNoScrimAndTheRepsCardStandsOverIt()
    {
        using DeskBench bench = await DeskBench.BootAsync(SatAtABarTop);
        bench.CallOnTheDispatcher("OpenSeatedNews");

        DeskBench.Painted painted = await bench.RenderAsync();
        Assert.True(Drawn(painted, "seated-news"), "the paper did not go up at a bar top.");
        Assert.False(Drawn(painted, "view-object-backdrop"),
            "the docked news panel put a full-viewport scrim on the glass. #1052: the panel is DOCKED, not "
            + "modal — the bar floor, the walkers and an arriving card have to stay visible AND clickable.");

        // The salesman crosses the floor and arrives — his own callback, the one his walker fires.
        bench.CallOnTheDispatcher("HeReachesYourTable");
        painted = await bench.RenderAsync();

        DeskBench.Painted.Node pitch = painted.Root.Descendants().FirstOrDefault(n => n.HasClass("rep-card"))
            ?? throw new Xunit.Sdk.XunitException("the rep never reached the table, so nothing was over the paper.");
        DeskBench.Painted.Node paper = painted.Root.Descendants().FirstOrDefault(n => n.HasClass("seated-news"))
            ?? throw new Xunit.Sdk.XunitException(
                "the paper went away when the rep arrived — a docked panel is not a card and must not be "
                + "peeled by one (#1052).");

        Assert.True(Pressable(pitch), "the rep's card drew nothing a player could press.");
        Assert.True(Pressable(paper), "the paper under his card drew nothing a player could press.");

        // …and putting HIM down leaves the paper exactly where it was.
        await bench.PressAsync(TheFirstPress(pitch));
        painted = await bench.RenderAsync();
        Assert.True(Drawn(painted, "seated-news"),
            "closing the rep's card took the paper with it — the two surfaces are not independent.");
    }

    /// <summary>
    /// <b>ESCAPE TAKES THE PAPER BEFORE IT ASKS ABOUT THE CHAIR.</b>
    ///
    /// <para>The rung, driven by the key rather than read off the source. On a DOCKED seat Escape's own
    /// meaning is "ask whether to stand up" (#784), and a captain who has a newspaper open did not mean
    /// that. Two presses, in order: the paper, then the question.</para>
    ///
    /// <para><b>RED PROOF:</b> move the <c>_seatedNewsOpen</c> line below the two seat rungs in
    /// <c>TryDismissTopOverlay</c> and this fails on the first assertion — <i>"Escape left the paper
    /// open"</i> — because the key stopped at the chair, which is the whole bug said out loud.</para>
    /// </summary>
    [Fact]
    public async Task EscapeTakesThePaperFirstAndTheChairSecond()
    {
        using DeskBench bench = await DeskBench.BootAsync(SatAtABarTop);
        bench.Poke("_audioArmed", true);
        bench.CallOnTheDispatcher("OpenSeatedNews");

        DeskBench.Painted painted = await bench.RenderAsync();
        ulong keyboard = DeskBench.TheKeyboard(painted);
        Assert.True(keyboard != 0, "the page drew no keyboard host — nothing could type at it.");

        await bench.TypeAsync(keyboard, "Escape");
        Assert.False((bool)bench.Field("_seatedNewsOpen")!, "Escape left the paper open.");
        Assert.False((bool)bench.Call("get_TheStandUpConfirmIsUp")!,
            "the first Escape asked about the chair while the paper was still open — the news rung is below "
            + "the seat's in TryDismissTopOverlay (#1052).");

        await bench.TypeAsync(keyboard, "Escape");
        Assert.True((bool)bench.Call("get_TheStandUpConfirmIsUp")!,
            "the second Escape did not reach the seat — the chain stopped at a panel that was already shut.");
    }

    /// <summary>
    /// <b>STANDING UP TAKES THE PAPER WITH IT, AND ESCAPE STOPS REACHING FOR IT.</b>
    ///
    /// <para>The panel is seated-only by law, and a gate left set behind a captain who has walked off is
    /// two bugs at once: a paper he cannot see, cannot reach and cannot put down, and — the one that bites
    /// — an Escape that would be CONSUMED by it, spending a press on nothing while the thing he is actually
    /// looking at sits there. That is #1027's own complaint, one surface over. So the gate goes down with
    /// the chair, beside the spread that already does.</para>
    ///
    /// <para><b>RED PROOF:</b> delete <c>CloseSeatedNews()</c> from <c>StandUpFromTable</c> and gate the
    /// panel on the bare field instead of <c>ThePaperIsOpen</c>, and this fails on the Escape claim — the
    /// key is eaten by a panel nobody can see.</para>
    /// </summary>
    [Fact]
    public async Task StandingUpPutsThePaperDownAndTheCancelKeyStopsReachingForIt()
    {
        using DeskBench bench = await DeskBench.BootAsync(SatAtABarTop);
        bench.Poke("_audioArmed", true);
        DeskBench.Painted painted = await bench.RenderAsync();
        ulong keyboard = DeskBench.TheKeyboard(painted);

        bench.CallOnTheDispatcher("OpenSeatedNews");
        Assert.True(Drawn(await bench.RenderAsync(), "seated-news"), "the paper did not go up.");

        bench.CallOnTheDispatcher("StandUpFromTable");
        painted = await bench.RenderAsync();
        Assert.False(Drawn(painted, "seated-news"), "the paper outlived the chair it was read in.");
        Assert.False((bool)bench.Field("_seatedNewsOpen")!,
            "the gate is still set with nobody sitting anywhere — the paper would fan itself open again the "
            + "next time the captain sat down.");

        // …and the cancel key no longer stops on it: nothing is open, so Escape falls through untouched.
        await bench.TypeAsync(keyboard, "Escape");
        Assert.False((bool)bench.Field("_seatedNewsOpen")!, "Escape re-raised something.");
    }

    /// <summary>
    /// <b>…AND THE SAME IS TRUE OF THE OTHER ROAD OUT OF A CHAIR.</b>
    ///
    /// <para>#847 gave a movement input its own way out of a seat — <c>StandUpBeforeWalking</c>, which
    /// reaches the seat's teardown directly and never passes the Stand-up button's forwarder. FOUND BY
    /// PLAYING IT, at a bar top in a browser: click-to-walk off the chair took the panel off the screen
    /// (<c>ThePaperIsOpen</c> saw to that) and left the GATE set behind it, so the paper would have fanned
    /// itself open again at whatever table the captain sat down at next.</para>
    ///
    /// <para><b>RED PROOF:</b> revert <c>StandUpBeforeWalking</c> to the bare forwarder and this fails —
    /// <i>"walking off the chair left the paper's gate set"</i>.</para>
    /// </summary>
    [Fact]
    public async Task WalkingOffTheChairPutsThePaperDownAsWell()
    {
        using DeskBench bench = await DeskBench.BootAsync(SatAtABarTop);
        await bench.RenderAsync();
        bench.CallOnTheDispatcher("OpenSeatedNews");

        Assert.True((bool)bench.CallOnTheDispatcher("StandUpBeforeWalking")!,
            "the captain was not in a chair, so this drive proves nothing about leaving one.");
        Assert.False((bool)bench.Field("_seatedNewsOpen")!,
            "walking off the chair left the paper's gate set — hidden is not shut (#1052).");
    }

    // ── (c) READING FILES NOTHING; CLIPPING DOES ──────────────────────────────────────────────────────

    /// <summary>
    /// <b>✂ CLIP FILES EXACTLY ONE NOTE, WITH THE 📰 AND WITH THE SUBJECTS — AND A SECOND CLIP FILES
    /// NOTHING.</b>
    ///
    /// <para>Three claims in one drive, because they are one act. Opening the paper and reading it files
    /// NOTHING (#602's ammo-as-evidence: what is in the book got there on purpose). Pressing the scissors on
    /// a story the wire wrote about a hull files one entry, under the wire's glyph, carrying the subjects the
    /// EVENT'S AUTHOR declared — so the clipping lands on the same red-pen thread the dossier's own entries
    /// stack on. Pressing it again files nothing, because <c>FieldNotes.Append</c> refuses a repeat of the
    /// entry it is standing on and this lane keeps no second register to disagree with it.</para>
    ///
    /// <para>The press is a real dispatched click at the handler id the render tree wrote for the row's own
    /// control, not a call to <c>ClipThisStory</c>.</para>
    ///
    /// <para><b>RED PROOFS:</b> drop the <c>IntelPurchased</c> arm of <c>NewsWire.SubjectsFor</c> and the
    /// subjects assertion fails — <i>Expected: "p:Kestrel" / Actual: ""</i>; delete the dedupe branch from
    /// <c>FieldNotes.Append</c> and the second clip files a second note — <i>Expected: 1 / Actual: 2</i>.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ClippingFilesOneNoteWithTheGlyphAndTheSubjectsAndASecondClipFilesNothing()
    {
        using DeskBench bench = await DeskBench.BootAsync(SatAtABarTop);

        // A story the wire itself wrote, pushed through the one funnel every news event in the game uses.
        const string Hull = "Kestrel";
        bench.CallOnTheDispatcher(
            "PushNewsEvent", NewsWire.NewsEventKind.IntelPurchased, Hull, null);

        int before = Book(bench).Count;
        bench.CallOnTheDispatcher("OpenSeatedNews");
        DeskBench.Painted painted = await bench.RenderAsync();

        Assert.Equal(before, Book(bench).Count);   // reading files NOTHING

        ulong clip = TheClipOnTheFreshestRow(painted);
        Assert.True(clip != 0, "no ✂ control was drawn on the freshest story.");

        await bench.PressAsync(clip);
        IReadOnlyList<FieldNote> after = Book(bench);
        Assert.Equal(before + 1, after.Count);

        FieldNote filed = after[^1];
        Assert.Equal("📰", filed.Glyph);
        Assert.Equal(
            NewsWire.Headline(new NewsWire.NewsEvent(NewsWire.NewsEventKind.IntelPurchased, 0, Hull)),
            filed.Text);
        Assert.Equal(CaseSubjects.Line(CaseSubjects.Place(Hull)), filed.Subjects);

        // …and a second clip of the same story files nothing new.
        painted = await bench.RenderAsync();
        await bench.PressAsync(TheClipOnTheFreshestRow(painted));
        Assert.Equal(after.Count, Book(bench).Count);
    }

    // ── Reading the page back ─────────────────────────────────────────────────────────────────────────

    private static DeskBench.Painted.Node? TheStrip(DeskBench.Painted painted) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass("seated-dock") && !n.Hidden);

    /// <summary>Was anything wearing this class drawn — in the tree OR inside a static-markup blob, which
    /// the element walk cannot see at all (#992's own finding).</summary>
    private static bool Drawn(DeskBench.Painted painted, string css) =>
        painted.Root.Descendants().Any(n => n.HasClass(css) && !n.Hidden)
        || painted.MarkupBlobs.Any(blob => blob.Contains($"class=\"{css}", StringComparison.Ordinal)
                                           || blob.Contains($" {css}\"", StringComparison.Ordinal));

    private static bool Pressable(DeskBench.Painted.Node surface) =>
        surface.SelfAndDescendants().Any(n => !n.Hidden && n.Handlers.ContainsKey("onclick"));

    private static ulong TheFirstPress(DeskBench.Painted.Node surface) =>
        surface.SelfAndDescendants()
            .Where(n => !n.Hidden && n.Handlers.ContainsKey("onclick"))
            .Select(n => n.Handlers["onclick"])
            .First();

    /// <summary>The scissors on today's story — the headline row, which is where a just-pushed event
    /// lands.</summary>
    private static ulong TheClipOnTheFreshestRow(DeskBench.Painted painted) =>
        painted.Root.Descendants()
            .Where(n => n.HasClass("news-clip") && n.Handlers.ContainsKey("onclick"))
            .Select(n => n.Handlers["onclick"])
            .FirstOrDefault();

    private static IReadOnlyList<FieldNote> Book(DeskBench bench) =>
        (IReadOnlyList<FieldNote>)bench.Peek("_fieldNotes")!;
}
