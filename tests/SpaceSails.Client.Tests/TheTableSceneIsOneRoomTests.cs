using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #746 · THE TABLE SCENE, WIRED — one room, one answer, and the outcome where the player is looking.
///
/// <para>Core owns who is sitting where and how many a top seats (<c>TheTableSceneTests</c> pins those laws,
/// red-proven). What is left to get wrong is the WIRING, and there are exactly two ways to get it wrong:</para>
///
/// <list type="number">
/// <item><b>Two sources.</b> The renderer walking <c>Amenity.Tables</c> for the round tops and separately
/// asking who is <c>Sitting</c> at them is two readings of one rota — the drawn room and the pressed room
/// disagreeing, which is #709's own warning and this repo's most expensive bug class.</item>
/// <item><b>#680.</b> A conversation whose answers are pulsed while its own modal is up puts every line the
/// scene exists for behind a backdrop blur. The panel has to say it.</item>
/// </list>
///
/// <para>Both guards below were watched red before they were watched green.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheTableSceneIsOneRoomTests
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

    private static DeckPlan DeckFor(string body, int level, long watch = 0) =>
        HiveInterior.FloorDeck(body, level, MoonSurface.ExpeditionField(), 0, (_, _) => { }, [], watch);

    // ── ONE SOURCE ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ONESOURCE_EveryDrawnTopAndEverySeatedPersonComesFromTheOneCall()
    {
        // The real values: UndergroundComplex.Build over MoonSurface.ExpeditionField, every floor of ten
        // sites, four watches each. If the deck and Core's table list ever part company, the [E] press is
        // answering about a room the player is not looking at.
        int tops = 0, people = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                for (long watch = 0; watch < 4; watch++)
                {
                    DeckPlan deck = DeckFor(body, level, watch);

                    var expectedTops = new List<(float X, float Y)>();
                    var expectedPeople = new List<(float X, float Y, string Plate)>();
                    foreach (UndergroundComplex.Amenity a in
                        UndergroundComplex.Build(body, level, MoonSurface.ExpeditionField()).Amenities)
                    {
                        foreach (CanteenRegulars.TableSeat t in
                            CanteenRegulars.Tables(body, level, a, watch))
                        {
                            expectedTops.Add(((float)t.X, (float)t.Y));
                            if (t.Plate is { } plate)
                            {
                                expectedPeople.Add(((float)t.X, (float)t.Y, plate));
                                // A top somebody is at still has room for the captain — which is the whole
                                // premise of "ask to join", and it is checked HERE against the drawn room.
                                Assert.True(t.Free >= 1,
                                    $"{body} B{-level}: a table with somebody at it has no chair left.");
                            }
                        }
                    }

                    var drawnTops = deck.Tables.Select(t => (t.X, t.Y)).ToList();
                    var drawnPeople = deck.Consoles
                        .Where(c => c.Kind == DeckPlan.ConsoleKind.HiveRegular)
                        .Select(c => (c.X, c.Y, c.Label))
                        .ToList();

                    // The SHIP's cantina tops ride the same array on a ship deck; underground the only tops
                    // are the amenities', so the two lists must be equal, not merely overlapping.
                    Assert.Equal(expectedTops.Count, drawnTops.Count);
                    foreach ((float x, float y) in expectedTops)
                    {
                        Assert.Contains((x, y), drawnTops);
                    }

                    Assert.Equal(expectedPeople.Count, drawnPeople.Count);
                    foreach ((float x, float y, string plate) in expectedPeople)
                    {
                        Assert.Contains((x, y, plate), drawnPeople);
                        // …and nobody is drawn anywhere but on a top. Standing beside the furniture is how
                        // an [E] press lands on the wrong object.
                        Assert.Contains((x, y), drawnTops);
                    }

                    tops += drawnTops.Count;
                    people += drawnPeople.Count;
                }
            }
        }

        Assert.True(tops > 200, $"only {tops} tops drawn across the sweep — this guard is not walking rooms.");
        Assert.True(people > 20, $"only {people} people drawn — nobody is in the bar.");
    }

    [Fact]
    public void ONESOURCE_TheRendererAsksTheONECallAndNeverAssemblesTheRoomItself()
    {
        // Source-shape, and it has to be: the behavioural guard above is green on BOTH shapes today, because
        // two readings of one rota agree right up until the day one of them changes. What is actually under
        // test is that there is only one reading left to change.
        string hive = Source("Rendering", "HiveInterior.cs");

        Assert.Contains("CanteenRegulars.Tables(", hive, StringComparison.Ordinal);
        Assert.DoesNotContain("CanteenRegulars.Sitting(", hive,
            StringComparison.Ordinal);

        // …and it does not walk the amenity's raw table list for the tops either, which is the other half of
        // the same shape: the seat counts live on Core's TableSeat, and a renderer iterating a.Tables has no
        // way to have heard of them.
        Assert.DoesNotContain("in a.Tables", hive, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePressAsksTheSameCallTheDeckWasDrawnWith()
    {
        // The other end of the wire. The panel's opener must read Core's table list off the SAME frozen
        // watch the deck was welded at — a press that re-read a clock would be bug class three with a face.
        string table = Source("Pages", "Map.Table.cs");
        Assert.Contains("CanteenRegulars.Tables(", table, StringComparison.Ordinal);
        Assert.Contains("ex.CanteenWatch", table, StringComparison.Ordinal);
        Assert.DoesNotContain("SimTime", table, StringComparison.Ordinal);
    }

    // ── #680 ──────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The table block of Map.razor — from the modal's opening guard to the satchel block that
    /// follows it. Cut so the assertions are about THIS dialog's subtree, not the file at large. The idiom
    /// #680's own guards introduced.</summary>
    private static string TableBlock()
    {
        string razor = Source("Pages", "Map.razor");
        int start = razor.IndexOf("@if (_table is { } tab)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the table block this guard knows how to find.");
        int end = razor.IndexOf("@if (_showSatchel)", start, StringComparison.Ordinal);
        Assert.True(end > start, "Map.razor's table block no longer ends where this guard expects.");
        return razor[start..end];
    }

    [Fact]
    public void THE_OUTCOME_IsSaidInsideThePanelItself()
    {
        // The one layer the backdrop cannot blur is the dialog's own subtree. Every answer at this table is
        // rendered there — the whole scene is conversation, and a conversation behind frosted glass is the
        // exact bug #680 was filed on, in the one feature that is nothing but text.
        string block = TableBlock();
        Assert.Contains("tab.Outcome", block, StringComparison.Ordinal);
        Assert.Contains("table-outcome", block, StringComparison.Ordinal);

        // …and it is styled like the dialog it sits in. An unstyled answer in a styled dialog is a bug
        // report waiting to be filed (#680's own words).
        Assert.Contains(".table-outcome",
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css")),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// #749 · THE PANEL DRAWS WHAT IS ON THE TABLE, and asks Core which that is.
    ///
    /// <para>The other half of #749's first defect, at the end of the wire the owner was looking at. Core's
    /// law (<c>THE_OFFERS_ANSWERS_AreNotOnTheTable…</c>) is worth nothing if the markup walks the scene's
    /// whole move list anyway — which is precisely what it did, drawing the fitter's two answers greyed with
    /// "Not yet." on them before he had said a word.</para>
    /// </summary>
    [Fact]
    public void THE_PANEL_DrawsOnlyTheMovesThatExistYetAndAsksCoreWhichThoseAre()
    {
        string block = TableBlock();

        // The markup asks a question; it does not compute the answer, and it no longer walks every move the
        // scene has ever had.
        Assert.Contains("TableMovesOnTheTable()", block, StringComparison.Ordinal);
        Assert.DoesNotContain("tab.Scene.Moves", block, StringComparison.Ordinal);

        // …and the question is Core's, over the SITTING's own memory. A client that kept its own idea of when
        // an answer exists would hand the guard stop a different rule than the canteen (#746's whole claim).
        string table = Source("Pages", "Map.Table.cs");
        Assert.Contains("Encounter.OnTheTable(", table, StringComparison.Ordinal);
        Assert.Contains("t.Said", table, StringComparison.Ordinal);

        // The refusal path stays for everything that is a DOOR: a disabled control still says why (#603).
        Assert.Contains("TableMoveRefusal(m)", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// #749/#680 · EVERY MOVE WITH A LINE SPEAKS IT — including the ones nobody wrote a case for.
    ///
    /// <para>The dodge's authored reply reached the screen only because a client author had written
    /// <c>case CanteenTable.DodgeScaffold:</c> by hand; the dispatch had never once read
    /// <c>Encounter.Move.Says</c>, the framework's own "the outcome is FIXED" field and the field a guard
    /// stop's content will be written on. Everything not enumerated fell off the end of that switch in
    /// silence, which is #680 with the volume at zero.</para>
    ///
    /// <para>This asserts the CHAIN, link by link, because that is as far as a source guard can carry a line
    /// to a screen: the move carries it (Core pins that), the dispatch hands whatever it carries to the one
    /// ending, that ending writes <c>t.Outcome</c> (the guard below), and the panel renders <c>tab.Outcome</c>
    /// inside its own subtree (the guard above).</para>
    /// </summary>
    [Fact]
    public void A_FIXED_OUTCOME_SPEAKS_BecauseTheMoveCarriesTheLineAndNotBecauseOfItsName()
    {
        string table = Source("Pages", "Map.Table.cs");

        int at = table.IndexOf("private void TableMove(", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.Table.cs no longer has TableMove where this guard can read it.");
        int end = table.IndexOf("\n    /// <summary>", at, StringComparison.Ordinal);
        string dispatch = table[at..(end > at ? end : table.Length)];

        // The dispatch reads what the move CARRIES…
        Assert.Contains("move.Says", dispatch, StringComparison.Ordinal);
        Assert.Contains("CanteenTable.SaidPlainly(move)", dispatch, StringComparison.Ordinal);

        // …and no longer knows the dodge's name at all. One id hand-cased into speaking is one content file
        // away from being the only one that does.
        Assert.DoesNotContain("CanteenTable.DodgeScaffold", table, StringComparison.Ordinal);

        // And it says it the only way anything is said at this table: through the one ending, in the panel.
        int says = dispatch.IndexOf("move.Says", StringComparison.Ordinal);
        int answered = dispatch.IndexOf("TableAnswered", says, StringComparison.Ordinal);
        Assert.True(answered > says,
            "the fixed-outcome path no longer routes through TableAnswered — #680's one ending.");
    }

    /// <summary>One method's body, cut around an offset — the idiom #680's guards introduced. Backwards to
    /// the nearest member declaration, forwards to the next one.</summary>
    private static string MethodBodyAround(string source, int at)
    {
        int start = source.LastIndexOf("\n    private ", at, StringComparison.Ordinal);
        Assert.True(start >= 0, "no member declaration above this call — the guard cannot cut a body.");
        int end = source.IndexOf("\n    private ", at, StringComparison.Ordinal);
        return source[start..(end > start ? end : source.Length)];
    }

    [Fact]
    public void NoMoveEverPulsesItsLineWhileThePanelIsStillUp()
    {
        // Stated as its own negation, the way #680's guards state it. Every move ends at TableAnswered, and
        // TableAnswered stores the line for the panel and files the note — it may never pulse.
        string table = Source("Pages", "Map.Table.cs");

        int at = table.IndexOf("private void TableAnswered(", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.Table.cs no longer has TableAnswered where this guard can read it.");
        int end = table.IndexOf("\n    // ──", at + 1, StringComparison.Ordinal);
        string answered = table[at..(end > at ? end : table.Length)];

        Assert.Contains("t.Outcome = said.Line", answered, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPulseMessage", answered, StringComparison.Ordinal);

        // FileNote and not ShowAndFile: the book must remember what the pulse never said (#686).
        Assert.Contains("FileNote(", answered, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowAndFile(", answered, StringComparison.Ordinal);

        // And every move that produces an answer goes through it. A branch keeping its own ending would be a
        // second place for #680 to come back (#697's lesson).
        foreach (string call in new[]
        {
            "CanteenTable.MadeSmallTalk(", "CanteenTable.BoughtTheRound(", "CanteenTable.ScaffoldTaken(",
            // #749 · The dodge is no longer named here, and that is the fix: it comes back through
            // SaidPlainly, which is every fixed outcome in the game and not one string somebody remembered.
            "CanteenTable.SaidPlainly(", "CanteenTable.HandAsksAboutWork(",
            "CanteenTable.FitterAsksAboutWork(", "CanteenTable.PutOnTheTable(",
        })
        {
            int from = table.IndexOf(call, StringComparison.Ordinal);
            Assert.True(from >= 0, $"Map.Table.cs no longer calls {call} — this guard needs re-reading.");

            // The METHOD that asks Core for an answer must be the method that hands it to the one ending —
            // the method-body cut #680's own guards use. A branch that produced an answer and said it some
            // other way is exactly what this is looking for.
            string method = MethodBodyAround(table, from);
            Assert.Contains("TableAnswered(", method, StringComparison.Ordinal);
        }

        // …and the whole file may pulse exactly twice: the leave line (correct — the panel is gone, see the
        // next guard) and the dev cheat's own notice. A third pulse appearing here is a line somebody
        // decided to say over the top of an open modal, which is #680 coming back.
        int pulses = table.Split("ShowPulseMessage(").Length - 1;
        Assert.True(pulses == 2,
            $"Map.Table.cs pulses {pulses} lines; only taking your leave and the dev notice may, because " +
            "every other line in this scene is said while the panel is still up (#680).");
    }

    [Fact]
    public void TakingYourLeaveIsTheONEPulseAndItIsCorrectBecauseThePanelIsGone()
    {
        // The exception that proves the law. Standing up shuts the panel, so there is no dialog subtree left
        // to say anything in — the pulse HUD is the only surface there is, and #680 is about which surface
        // the player is looking at rather than about pulses being wrong.
        string table = Source("Pages", "Map.Table.cs");
        int at = table.IndexOf("if (moveId == CanteenTable.Leave)", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.Table.cs no longer handles taking your leave where this guard can read it.");
        string leaving = table[at..table.IndexOf("\n        if (moveId == CanteenTable.Show)", at, StringComparison.Ordinal)];

        int closed = leaving.IndexOf("CloseTable()", StringComparison.Ordinal);
        int pulsed = leaving.IndexOf("ShowPulseMessage(", StringComparison.Ordinal);
        Assert.True(closed >= 0 && pulsed > closed,
            "the leave line is pulsed before the panel shuts — which is exactly the blur #680 was filed on.");
    }

    // ── THE CHEATS ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void THE_SCENE_IS_REACHABLE_OnDemandAndTheGuideSaysHow()
    {
        // "A scene nobody can reach on demand is a scene that ships broken", and this one sits behind more
        // doors than anything we have shipped.
        string sim = Source("Pages", "Map.Sim.cs");
        Assert.Contains("tablescene=", sim, StringComparison.Ordinal);
        Assert.Contains("roll=", sim, StringComparison.Ordinal);

        // ?tablescene=1 implies the whole route rather than adding a fourth spelling of it.
        int at = sim.IndexOf("pair.StartsWith(\"tablescene=\"", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.Sim.cs no longer parses ?tablescene= where this guard can read it.");
        string branch = sim[at..sim.IndexOf("else if (pair.StartsWith(\"roll=\"", at, StringComparison.Ordinal)];
        Assert.Contains("secretlabCheat = true", branch, StringComparison.Ordinal);
        Assert.Contains("secretlabDeep = true", branch, StringComparison.Ordinal);
        Assert.Contains("_landCheat = true", branch, StringComparison.Ordinal);
        Assert.Contains("_startingFloorCheat = -1", branch, StringComparison.Ordinal);

        // …and the testing guide documents both, because a cheat nobody knows about is a cheat nobody uses.
        string guide = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "testing-guide.md"));
        Assert.Contains("tablescene=1", guide, StringComparison.Ordinal);
        Assert.Contains("roll=hi", guide, StringComparison.Ordinal);
    }

    [Fact]
    public void THE_CHITS_PAYOFF_IsWiredToTheRoomThePassExistsFor()
    {
        // #743's staff mess, entered with cover. The beat is additive — the room's own card still fires
        // first — and it never plays in the same frame as that card, because a pulse under an open card is
        // #680 again.
        string surface = Source("Pages", "Map.Surface.cs");
        int at = surface.IndexOf("private void CheckStaffMessUnderfoot()", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.Surface.cs no longer has CheckStaffMessUnderfoot.");
        string body = surface[at..surface.IndexOf("\n    // ──", at, StringComparison.Ordinal)];

        Assert.Contains("ShowMessChitBeat(ex)", body, StringComparison.Ordinal);

        int card = body.IndexOf("UndergroundComplex.StaffMessCard", StringComparison.Ordinal);
        int beat = body.IndexOf("ShowMessChitBeat(ex)", StringComparison.Ordinal);
        Assert.True(card >= 0 && beat > card,
            "the chit beat is raised before #743's own room card — the find has to come first.");

        // The poll survives the room card, or a captain who got the chit AFTER walking through the mess
        // could never be handed the beat at all.
        Assert.Contains("ex.MessChitBeatShown", body, StringComparison.Ordinal);
    }
}
