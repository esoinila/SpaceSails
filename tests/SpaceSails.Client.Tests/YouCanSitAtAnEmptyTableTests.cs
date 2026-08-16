using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #757 · AN EMPTY TABLE NO LONGER REFUSES YOU — wired, end to end.
///
/// <para>Owner, live in the hall: <i>"I have empty table but I cannot sit down"</i>, and, minutes later,
/// <i>"the normal way to operate in a bar or restaurant is still not implemented."</i></para>
///
/// <h3>The defect was an ABSENCE, which is why it survived every guard we had</h3>
///
/// <para>#746's press dispatches on <see cref="DeckPlan.ConsoleKind.HiveRegular"/>, and the renderer only
/// laid one of those over a top with somebody AT it. An empty top was a ring drawn on the floor with no
/// console under it at all — so [E] there did not refuse, it did not answer, and it did not even reach the
/// dispatch. Nothing in the test suite could see it, because there was nothing to look at. Every guard below
/// was watched RED on the shipped renderer before it was watched green.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class YouCanSitAtAnEmptyTableTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

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

    private static string Source(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    /// <summary>#870 lane 6c · Re-PATHED, never re-asserted. The seat family is TWO files per subject now:
    /// the page's half — the records, the dev rows, the things a seat is a GATE on, and the forwarders — and
    /// the seat's own verbs, which moved onto <c>Map.Seating</c> behind <c>ISeatHost</c>. The source a guard
    /// reads over is BOTH, concatenated in that order, which is exactly the text it read out of one file
    /// before the verbs moved. Concatenated rather than narrowed to one half on purpose: several claims here
    /// are <c>DoesNotContain</c> over the whole subject, and pointing one at a single file would be a silent
    /// weakening.</summary>
    private static string Table() =>
        Source("Pages", "Map.Table.cs") + Source("Pages", "Seating", "Seating.Table.cs");


    /// <summary>#870 · The sim page is nine partials by subject now, so "the sim" a guard reads over is all
    /// of them — exactly the text it read out of one file before the split.</summary>
    private static string Sim() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Sim*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>#870 · The deck page is seven partials by subject now, so "the deck" a guard reads over is
    /// all of them — exactly the text it read out of one file before the split.</summary>
    private static string Deck() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Deck*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    private static DeckPlan DeckFor(string body, int level, long watch = 0) =>
        HiveInterior.FloorDeck(body, level, MoonSurface.ExpeditionField(), 0, (_, _) => { }, [], watch);

    // ── (a) THE PRESS LANDS ON A FREE TABLE ───────────────────────────────────────────────────────────

    /// <summary>
    /// #757 · EVERY TOP IN THE ROOM IS PRESSABLE, AND WHICH VERB IT OFFERS IS THE ROOM'S OWN FACT.
    ///
    /// <para>The whole of the owner's complaint, stated as geometry: a top with somebody at it is
    /// <c>HiveRegular</c> (ask to join, #746) and a top with nobody at it is <c>HiveTable</c> (take it,
    /// #757). Not one top in the building may be neither — that is what "I cannot sit down" was.</para>
    ///
    /// <para>RED on the shipped renderer: it emitted a console only inside <c>if (top.Plate is { } plate)</c>,
    /// so on a sweep of ten sites, every floor, six watches, thousands of free tops came back with nothing
    /// over them at all.</para>
    /// </summary>
    [Fact]
    public void EVERY_FREE_TOP_CarriesASitConsoleAndEveryTakenOneCarriesItsPerson()
    {
        int free = 0, taken = 0, elsewhere = 0;
        var missing = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                for (long watch = 0; watch < 6; watch++)
                {
                    DeckPlan deck = DeckFor(body, level, watch);

                    foreach (UndergroundComplex.Amenity a in
                        UndergroundComplex.Build(body, level, MoonSurface.ExpeditionField()).Amenities)
                    {
                        // …and ONLY in the room outsiders are admitted to. B17's staff mess is hall-class
                        // as well and just as full of tops, and it is empty on every watch FOREVER by
                        // design — a table you could take in there would be this feature quietly rewriting
                        // that room. Core's own B1 ruling decides, not this guard.
                        if (!CanteenRegulars.PeopleSitHere(body, level, a))
                        {
                            foreach (CanteenRegulars.TableSeat idle in
                                CanteenRegulars.Tables(body, level, a, watch))
                            {
                                Assert.DoesNotContain(deck.Consoles, c =>
                                    c.Kind == DeckPlan.ConsoleKind.HiveTable
                                    && Math.Abs(c.X - (float)idle.X) < 0.01f
                                    && Math.Abs(c.Y - (float)idle.Y) < 0.01f);
                                elsewhere++;
                            }
                            continue;
                        }

                        foreach (CanteenRegulars.TableSeat top in
                            CanteenRegulars.Tables(body, level, a, watch))
                        {
                            DeckPlan.ConsoleKind want = top.Taken
                                ? DeckPlan.ConsoleKind.HiveRegular
                                : DeckPlan.ConsoleKind.HiveTable;

                            bool there = deck.Consoles.Any(c =>
                                c.Kind == want
                                && Math.Abs(c.X - (float)top.X) < 0.01f
                                && Math.Abs(c.Y - (float)top.Y) < 0.01f);

                            if (!there)
                            {
                                missing.Add(
                                    $"  {body} B{-level} watch {watch}: top {top.Index} at " +
                                    $"({top.X:F1}, {top.Y:F1}) has no {want} over it.");
                            }

                            if (top.Taken)
                            {
                                taken++;
                            }
                            else
                            {
                                free++;
                            }
                        }
                    }
                }
            }
        }

        Assert.True(missing.Count == 0,
            $"{missing.Count} tops in this facility answer nothing at all:\n" +
            string.Join("\n", missing.Take(12)));

        // The sweep has to have SEEN both kinds, or it is a guard measuring an empty world.
        Assert.True(free > 200, $"only {free} free tops swept — this guard is not walking rooms.");
        Assert.True(taken > 20, $"only {taken} taken tops swept — nobody is in the bar.");

        // …and the other half of the law was actually exercised. A staff-mess clause nobody's sweep ever
        // reached is a clause that could be deleted without going red — the fifth bug class exactly.
        Assert.True(elsewhere > 100,
            $"only {elsewhere} tops outside the canteen were checked — the room-with-people-in-it rule is " +
            "not being tested against anything.");
    }

    [Fact]
    public void A_FREE_TOPS_PLATE_SaysWhatPressingItWillDo()
    {
        // #603 · A dot with no words on it is a dot nobody presses. The label is Core's — one place says
        // what a free table is called, and the renderer does not invent a second name for it.
        Assert.Contains("SittingAlone.FreeTablePlate", Source("Rendering", "HiveInterior.cs"),
            StringComparison.Ordinal);

        DeckPlan deck = DeckFor("luna", UndergroundComplex.TopPressurisedFloor("luna") ?? -1, 5);
        var tops = deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveTable).ToList();
        Assert.NotEmpty(tops);
        Assert.All(tops, c => Assert.Equal(SittingAlone.FreeTablePlate, c.Label));
    }

    /// <summary>ONE CONSOLE PER TABLE — never one per chair. Owner, live: <i>"do NOT put an E-prompt at
    /// every chair. ONE interaction point on the TABLE fixture."</i> A six-seat top and a two-seat top are
    /// one dot each, and the seat count changes nothing about how many things there are to press.</summary>
    [Fact]
    public void ONE_DOT_PER_TABLE_NeverOnePerChair()
    {
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }
            for (long watch = 0; watch < 6; watch += 2)
            {
                DeckPlan deck = DeckFor(body, level, watch);
                var tops = deck.Consoles
                    .Where(c => c.Kind is DeckPlan.ConsoleKind.HiveTable or DeckPlan.ConsoleKind.HiveRegular)
                    .ToList();

                int expected = 0;
                int chairs = 0;
                foreach (UndergroundComplex.Amenity a in
                    UndergroundComplex.Build(body, level, MoonSurface.ExpeditionField()).Amenities)
                {
                    foreach (CanteenRegulars.TableSeat t in CanteenRegulars.Tables(body, level, a, watch))
                    {
                        expected++;
                        chairs += t.Seats;
                    }
                }

                Assert.Equal(expected, tops.Count);
                Assert.True(chairs > expected,
                    $"{body} B{-level}: the tops seat {chairs} between them — if this were per-chair the " +
                    "count above would have caught it, so make sure it still could.");
            }
        }
    }

    /// <summary>The other end of the wire: [E] on a free top reaches the taking, and the taking asks Core's
    /// own list off the frozen watch — the same call the deck was drawn with (#709's law).</summary>
    [Fact]
    public void THE_PRESS_OnAFreeTopTakesTheTable()
    {
        string deck = Deck();
        int at = deck.IndexOf("case DeckPlan.ConsoleKind.HiveTable:", StringComparison.Ordinal);
        Assert.True(at >= 0, "[E] does not dispatch a free canteen top anywhere — #757's whole defect.");
        Assert.Contains("TryTakeTable()",
            deck[at..deck.IndexOf("break;", at, StringComparison.Ordinal)], StringComparison.Ordinal);

        string table = Table();
        int take = table.IndexOf("public bool TryTakeTable()", StringComparison.Ordinal);
        Assert.True(take >= 0, "Map.Table.cs has no TryTakeTable.");
        string body = table[take..table.IndexOf("\n        /// <summary>Stand up.", take, StringComparison.Ordinal)];

        Assert.Contains("CanteenRegulars.Tables(", body, StringComparison.Ordinal);
        Assert.Contains("ex.CanteenWatch", body, StringComparison.Ordinal);
        Assert.DoesNotContain("SimTime", body, StringComparison.Ordinal);

        // …and a top with somebody at it is NOT this verb. Two presses answering one table would be #746 and
        // #757 disagreeing about the same chair.
        Assert.Contains("top.Taken", body, StringComparison.Ordinal);

        // #820 · The seat is one of the top's OWN chairs — the nearest the party is not in — and the captain
        // is snapped into it. No new geometry all the same: the coordinate is asked of Core and the body is
        // moved by the one placement, because this project has set the captain down inside a wall twice by a
        // caller typing coordinates about a room it did not own (§13.15).
        Assert.Contains("top.ChairYouTake(_host.AvatarX, _host.AvatarY)", body, StringComparison.Ordinal);
        Assert.Contains("SitCaptainOn(sit.X, sit.Y)", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StandCaptainAt", body, StringComparison.Ordinal);
        Assert.DoesNotContain("_avatarX =", body, StringComparison.Ordinal);
    }

    // ── (b) WAITING BRINGS SOMEBODY, AND THE LADDER IS CORE'S ─────────────────────────────────────────

    [Fact]
    public void WAITING_AsksCoreAndTheApproachOpensTheSAME_PanelWithHerInIt()
    {
        string table = Table();

        int at = table.IndexOf("private void TableWaited(", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.Table.cs has no TableWaited — waiting is not wired.");
        string waited = table[at..table.IndexOf("\n        /// <summary>", at, StringComparison.Ordinal)];

        // The law is Core's, and the beat is the ROOM's memory — not the sitting's, or standing up and
        // sitting down again would be a re-roll.
        Assert.Contains("SittingAlone.SomebodyComes(", waited, StringComparison.Ordinal);
        Assert.Contains("ex.TableWaits", waited, StringComparison.Ordinal);
        Assert.Contains("ex.CanteenWatch", waited, StringComparison.Ordinal);
        Assert.DoesNotContain("SimTime", waited, StringComparison.Ordinal);

        // And the arrival swaps the SCENE rather than opening a second panel — one continuous sitting at one
        // table, so the line the captain is reading does not blink (#680).
        int chair = table.IndexOf("private void SomebodyTakesTheChair(", StringComparison.Ordinal);
        Assert.True(chair >= 0, "nobody can take the chair opposite — the approach is not wired.");
        string arrival = table[chair..table.IndexOf("\n        /// <summary>", chair, StringComparison.Ordinal)];
        Assert.Contains("SittingAlone.TheVisitor()", arrival, StringComparison.Ordinal);
        Assert.Contains("t.Outcome = t.Scene.Opening", arrival, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPulseMessage", arrival, StringComparison.Ordinal);
        Assert.DoesNotContain("CloseTable()", arrival, StringComparison.Ordinal);
    }

    // ── (c) NOBODY CAME IS TOLD, ON THE PANEL THE CAPTAIN PRESSED ─────────────────────────────────────

    /// <summary>
    /// #736/#680 · A WAIT THAT PRODUCED NOBODY STILL ANSWERS, and it answers where the captain is looking.
    ///
    /// <para>This is the guard that keeps the owner's own event from reading as a bug: an eighty-seat hall
    /// that used to be loud and now sends nobody over IS the beat, and a beat delivered as silence on a
    /// button is indistinguishable from a control that broke (#603). It goes through
    /// <c>TableAnswered</c> — the one ending — which writes the panel's own outcome line and never pulses
    /// while the modal is up.</para>
    /// </summary>
    [Fact]
    public void NOBODY_CAME_IsSaidOnThePanelThroughTheOneEnding()
    {
        string table = Table();
        int at = table.IndexOf("private void TableWaited(", StringComparison.Ordinal);
        string waited = table[at..table.IndexOf("\n        /// <summary>", at, StringComparison.Ordinal)];

        Assert.Contains("SittingAlone.NobodyCame(", waited, StringComparison.Ordinal);

        int said = waited.IndexOf("SittingAlone.NobodyCame(", StringComparison.Ordinal);
        int answered = waited.LastIndexOf("TableAnswered(", said, StringComparison.Ordinal);
        Assert.True(answered >= 0 && answered < said,
            "the nobody-came line does not go through TableAnswered — #680's one ending.");
        Assert.DoesNotContain("ShowPulseMessage", waited, StringComparison.Ordinal);
    }

    // ── (d) STANDING UP GIVES THE DECK BACK ───────────────────────────────────────────────────────────

    /// <summary>
    /// #757 · STANDING UP LEAVES CLEANLY — the panel goes, the keys come back, and the table is a table.
    ///
    /// <para>The way home matters more here than anywhere: a solo table is entered by pressing [E] and left
    /// by clicking a button, and a click leaves focus on a control that has just stopped existing — the deck
    /// goes deaf and the captain presses W and nothing walks. That bug was found by playing #746 and the seam
    /// that fixes it is <c>TableMoveClicked</c>, which every move in this panel goes through.</para>
    /// </summary>
    [Fact]
    public void STANDING_UP_ShutsThePanelAndGivesTheKeysBack()
    {
        string table = Table();

        int at = table.IndexOf("public async Task TableMoveClicked(", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.Table.cs no longer routes mouse presses through TableMoveClicked.");
        string clicked = table[at..table.IndexOf("\n        // ──", at, StringComparison.Ordinal)];
        Assert.Contains("Table is null", clicked, StringComparison.Ordinal);
        Assert.Contains("RefocusMap()", clicked, StringComparison.Ordinal);

        // …and the razor's own two ways out — the backdrop and the Close button — go through Dismiss, which
        // is the same seam. A third way out of this dialog would be a way out that does not restore anything.
        string razor = Source("Pages", "Map.razor");
        int start = razor.IndexOf("@if (SeatedTable is { } tab)", StringComparison.Ordinal);
        int end = razor.IndexOf("@if (_showSatchel)", start, StringComparison.Ordinal);
        string block = razor[start..end];
        Assert.Equal(3, block.Split("Dismiss(CloseTable)").Length - 1);

        // The leave line is the SCENE's, so standing up from your own table is not made to say the sentence
        // for standing up from somebody else's.
        Assert.Contains("TheLeaveMove(t)", table, StringComparison.Ordinal);
    }

    // ── THE CHEATS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #693's rule, applied to a beat that is otherwise reachable only by luck: <i>a scene nobody can reach
    /// on demand is a scene that ships broken.</i> Whether anybody crosses the room is a seeded roll at one
    /// top on one shift, so BOTH halves get a lever — somebody comes, and nobody ever does.
    /// </summary>
    [Fact]
    public void BOTH_HALVES_OfTheWaitAreReachableOnDemandAndTheGuideSaysHow()
    {
        string sim = Sim();
        Assert.Contains("approach=", sim, StringComparison.Ordinal);
        Assert.Contains("_approachCheat = candidate switch", sim, StringComparison.Ordinal);

        // …and the boot that stands you AT a free table, which is the table the issue is named after.
        Assert.Contains("\"free\"", sim, StringComparison.Ordinal);
        Assert.Contains("_freeTableCheat", sim, StringComparison.Ordinal);

        string table = Table();
        Assert.Contains("_host.ApproachCheat", table, StringComparison.Ordinal);
        Assert.Contains("FirstFreeTop(", table, StringComparison.Ordinal);

        // The cheat forces WHETHER and never WHO: it may not reach for the visitor's own content.
        int at = table.IndexOf("private void TableWaited(", StringComparison.Ordinal);
        string waited = table[at..table.IndexOf("\n        /// <summary>", at, StringComparison.Ordinal)];
        Assert.Contains("_host.ApproachCheat", waited, StringComparison.Ordinal);
        Assert.Contains("SittingAlone.SomebodyComes(", waited, StringComparison.Ordinal);

        // The front door offers it as a button, and the guide says how by hand.
        Assert.Contains(DevStarts.All, e => e.Url.Contains("tablescene=free", StringComparison.Ordinal));
        Assert.Contains(DevStarts.All, e => e.Url.Contains("approach=1", StringComparison.Ordinal));

        string guide = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "testing-guide.md"));
        Assert.Contains("tablescene=free", guide, StringComparison.Ordinal);
        Assert.Contains("approach=1", guide, StringComparison.Ordinal);
        Assert.Contains("approach=0", guide, StringComparison.Ordinal);

        // #783 · …and the cheat's own row says the labels a tester will actually read on the screen. A
        // testing guide that still says "take the table" sends the owner looking for a button nobody drew.
        Assert.Contains("SIT DOWN", guide, StringComparison.Ordinal);
        Assert.Contains("SIT A WHILE", guide, StringComparison.Ordinal);
    }

    // ── #783 · THE PANEL WEARS THE TABLE, AND THE RIGHT ONE OF ITS TWO PICTURES ───────────────────────

    /// <summary>
    /// #783 · THE SIT PANEL DRAWS THE TABLE IT IS A PANEL FOR.
    ///
    /// <para>Owner, live at a taken table: <i>"the pop up could have Gen AI here."</i> Same source-shape
    /// #772's counter guard uses for <c>keep.DeskArtUrl</c>: cut the card's own block out of the razor and
    /// prove the picture is IN it, because an image rendered anywhere else is an image behind the backdrop's
    /// blur (#680's whole lesson, applied to pixels).</para>
    ///
    /// <para><b>Proven RED</b> on the shipped panel — no image in the block at all:
    /// <i>Assert.Contains() Failure: Sub-string not found. String: "@if (_table is { } tab)…"</i> — quoted as
    /// it actually read that day; #870 lane 6a has since renamed that receiver to <c>SeatedTable</c>.</para>
    /// </summary>
    [Fact]
    public void THE_SIT_PANEL_DrawsTheTableItIsAPanelFor()
    {
        string razor = Source("Pages", "Map.razor");
        int start = razor.IndexOf("@if (SeatedTable is { } tab)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the table card this guard knows how to find.");
        string block = razor[start..razor.IndexOf("@if (_showSatchel)", start, StringComparison.Ordinal)];

        Assert.Contains("tab.ArtUrl", block, StringComparison.Ordinal);
        Assert.Contains("class=\"table-art\"", block, StringComparison.Ordinal);

        // The url is CORE's, per state. A razor that named a jpg would be a second answer to "which picture
        // is this sitting" — and the one that could not be kept in step with the prose beside it.
        Assert.DoesNotContain(".jpg", block, StringComparison.Ordinal);

        // #782 · The picture is framed, and the prose is NOT written over it — the card's own text keeps its
        // own class on its own background, which is how the contrast law is kept here by construction.
        Assert.Contains("class=\"table-outcome\"", block, StringComparison.Ordinal);
        Assert.True(
            block.IndexOf("class=\"table-art\"", StringComparison.Ordinal)
                < block.IndexOf("class=\"table-outcome\"", StringComparison.Ordinal),
            "the answer is drawn above the picture — #782's contrast law is only free while no text sits " +
            "on the art.");

        string css = Source("Pages", "Map.razor.css");
        int at = css.IndexOf(".table-art {", StringComparison.Ordinal);
        Assert.True(at >= 0, "no .table-art rule — a bare image stretches a 16:9 plate across the card.");
        string rule = css[at..css.IndexOf('}', at)];
        Assert.Contains("object-fit: cover", rule, StringComparison.Ordinal);
        Assert.Contains("max-height", rule, StringComparison.Ordinal);

        // Both plates are on disk. The art seam hides its own failure, so this is the only place a missing
        // painting can be made to say anything at all.
        foreach (string art in new[] { SittingAlone.WaitingArtUrl, SittingAlone.RestingArtUrl })
        {
            string file = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "wwwroot", art);
            Assert.True(File.Exists(file),
                $"{art} is not in wwwroot/art — the panel would draw a hole and never say so.");
        }
    }

    /// <summary>
    /// #783 · WHICH PICTURE AND WHICH SENTENCE COME OUT OF THE ONE ANSWER.
    ///
    /// <para>The register is <see cref="SittingAlone.SitReadsAsRelaxed"/>'s, decided once when the captain sits, and
    /// the opening line is the SCENE's rather than a constant the client reached for. That is the whole
    /// anti-mismatch law here: a client free to pin one of the two registers by hand is a client free to
    /// print "you put your boots up on the spare chair" over a picture of an empty one.</para>
    ///
    /// <para><b>Proven RED</b> on #778's own <c>TryTakeTable</c>, which read
    /// <c>Outcome = SittingAlone.TookTheTableLine</c> — the <c>DoesNotContain</c> below fails on it.</para>
    /// </summary>
    [Fact]
    public void WHICH_PICTURE_AND_WHICH_SENTENCE_ComeFromTheOneAnswer()
    {
        string table = Table();
        int take = table.IndexOf("public bool TryTakeTable()", StringComparison.Ordinal);
        Assert.True(take >= 0, "Map.Table.cs has no TryTakeTable.");
        string body = table[take..table.IndexOf("\n        /// <summary>Stand up.", take, StringComparison.Ordinal)];

        Assert.Contains("SittingAlone.SitReadsAsRelaxed(", body, StringComparison.Ordinal);
        Assert.Contains("SittingAlone.TheTable(relaxed, drink)", body, StringComparison.Ordinal);
        Assert.Contains("Outcome = sat.Opening", body, StringComparison.Ordinal);
        Assert.DoesNotContain("SittingAlone.TookTheTableLine", body, StringComparison.Ordinal);
        Assert.DoesNotContain("RelaxedSitLine", body, StringComparison.Ordinal);

        // The picture is chosen off the SAME flag, in Core, and never in the markup.
        Assert.Contains("SittingAlone.ArtFor(Relaxed)", table, StringComparison.Ordinal);

        // THE DRINK IS ASKED OF #784, NOT ANSWERED AGAIN HERE. Map.Seated.cs owns the one reading of the
        // counter's pour (its window, and its exclusion of a drunk captain), and it is the same fact the
        // short rest doubles its rate on — so a panel that kept a second window could print "the cold glass
        // sweat into your hand" on a beat the rest engine had already decided there was no pour.
        Assert.Contains("APourInFrontOfYou", table, StringComparison.Ordinal);
        Assert.DoesNotContain("_lastRumMs", table, StringComparison.Ordinal);
        Assert.DoesNotContain("_rumTots", table, StringComparison.Ordinal);
        Assert.DoesNotContain("_lastTimestampMs", table, StringComparison.Ordinal);

        // …and when she leaves, the register is ASKED again rather than remembered — a glass goes warm.
        int back = table.IndexOf("private void BackToYourOwnTable(", StringComparison.Ordinal);
        Assert.True(back >= 0, "the table stops being yours again — BackToYourOwnTable is gone.");
        string again = table[back..table.IndexOf("\n        /// <summary>", back, StringComparison.Ordinal)];
        Assert.Contains("SittingAlone.SitReadsAsRelaxed(", again, StringComparison.Ordinal);
        Assert.Contains("SittingAlone.TheTable(t.Relaxed, t.DrinkInHand)", again, StringComparison.Ordinal);
    }
}
