using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #784 · SITTING DOWN IS A STATE THE GAME CAN SEE — wired, end to end.
///
/// <para>Owner, live 2026-08-08, over the #778 table: <i>"Let's make the graphics say I am sitting down at
/// the avatar level — like different graphics etc."</i> · <i>"before moving I have to stand up… so if I try
/// to move when sitting down it should ask with a pop-up whether I want to stand up again."</i> ·
/// <i>"writing things down requires sitting down to be properly done."</i></para>
///
/// <h3>What was wrong, and why nothing caught it</h3>
///
/// <para>#757 built the whole seated scene without the captain's FIGURE ever learning about it: the mark on
/// the deck was the standing circle-and-spoke whether you were mid-stride or in a chair, and WASD walked you
/// out of your own table with the panel still up. Both defects were invisible to every guard we had, because
/// there was nothing to look at in the first place — the renderer was never told, and the key handler was
/// never asked. Every fact below was watched RED on the shipped build before it was watched green.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class SittingDownIsAStateTests
{
    private const int WidthPx = 1200, HeightPx = 700;

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

    private static string Doc(string name) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "docs", name));

    // ── (a) THE SEATED DRAWING ────────────────────────────────────────────────────────────────────────

    private sealed record Mark(string Kind, float[] Points, RgbaColor Ink, float Width);

    private sealed class RecordingRenderer : IRenderer
    {
        public List<Mark> Marks { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Marks.Clear();

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("circle", [x, y, r], fill ?? stroke, w));

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("polyline", pts.ToArray(), stroke, w));

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("polygon", pts.ToArray(), fill ?? stroke, w));

        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left) =>
            Marks.Add(new Mark("text", [x, y], c, 0f));

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) =>
            Marks.Add(new Mark("image", [x, y], new RgbaColor(0, 0, 0), 0f));

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) =>
            Marks.Add(new Mark("slice", [x, y], new RgbaColor(0, 0, 0), 0f));
    }

    /// <summary>The captain's ink, transcribed from <c>DeckView</c> deliberately — a test that asked the
    /// renderer for its own constant could not notice the constant changing into something else.</summary>
    private static readonly RgbaColor Amber = new(255, 210, 80);

    private static DeckPlan HallDeck() =>
        HiveInterior.FloorDeck("luna", -1, MoonSurface.ExpeditionField(), 0, (_, _) => { }, [], 0);

    /// <summary>One frame of a real hall floor, drawn by the real <see cref="DeckView"/>, with the captain
    /// standing where the car puts them and the lights on.</summary>
    private static (List<Mark> Marks, float Ax, float Ay, float Scale) Frame(bool seated)
    {
        DeckPlan plan = HallDeck();
        (double ax, double ay) = HiveInterior.SpawnOn(MoonSurface.ExpeditionField());
        var pen = new RecordingRenderer();
        new DeckView(pen).Draw(plan, WidthPx, HeightPx, 0,
            new DeckView.State(ax, ay, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false,
                Dark: false, Seated: seated),
            0, 0, null);

        DeckView.Placement place = DeckView.PlacementFor(plan, WidthPx, HeightPx, ax, ay, 0, 0);
        return (pen.Marks, place.Ox + ((float)ax * place.Scale), place.Oy - ((float)ay * place.Scale),
            place.Scale);
    }

    /// <summary>Every amber mark whose points all sit within <paramref name="du"/> deck units of the
    /// captain — the figure, and nothing else on the floor.</summary>
    private static List<Mark> AtTheCaptain(
        IEnumerable<Mark> marks, string kind, float ax, float ay, float scale, float du)
    {
        var near = new List<Mark>();
        foreach (Mark m in marks)
        {
            if (m.Kind != kind || m.Ink.R != Amber.R || m.Ink.G != Amber.G || m.Ink.B != Amber.B)
            {
                continue;
            }
            bool all = true;
            for (int i = 0; i + 1 < m.Points.Length && all; i += 2)
            {
                double dx = m.Points[i] - ax, dy = m.Points[i + 1] - ay;
                all = Math.Sqrt((dx * dx) + (dy * dy)) <= du * scale;
            }
            if (all)
            {
                near.Add(m);
            }
        }
        return near;
    }

    /// <summary>
    /// #784 · THE DECK DRAWS A DIFFERENT FIGURE WHEN THE CAPTAIN IS SITTING DOWN.
    ///
    /// <para>Owner: <i>"Let's make the graphics say I am sitting down at the avatar level."</i> So this is
    /// measured on the PIXELS the renderer actually laid down, and not on a flag being passed: the same
    /// floor, the same captain, the same heading, drawn twice, and the two frames must not agree about the
    /// figure.</para>
    ///
    /// <para>RED on the shipped renderer: <c>DeckView.State</c> had no posture at all, so both frames were
    /// byte-identical and this could not be written, let alone pass.</para>
    /// </summary>
    [Fact]
    public void THEAVATAR_IsDrawnDifferentlyInAChair()
    {
        (List<Mark> standing, float ax, float ay, float scale) = Frame(seated: false);
        (List<Mark> sitting, _, _, _) = Frame(seated: true);

        // THE SPOKE. A standing captain has one amber segment leaving the body — it has meant "this way"
        // since the mark was first drawn. A seated one is going nowhere and must not have it.
        List<Mark> standSpokes = AtTheCaptain(standing, "polyline", ax, ay, scale, 1.4f);
        List<Mark> sitBars = AtTheCaptain(sitting, "polyline", ax, ay, scale, 1.6f);

        Assert.True(standSpokes.Count == 1,
            $"a standing captain drew {standSpokes.Count} amber segment(s) at the body — expected the one " +
            "heading spoke.");
        bool spokeStartsAtTheBody = standSpokes[0].Points.Length >= 4
            && Math.Abs(standSpokes[0].Points[0] - ax) < 0.01f
            && Math.Abs(standSpokes[0].Points[1] - ay) < 0.01f;
        Assert.True(spokeStartsAtTheBody, "the standing spoke no longer leaves the captain's own centre.");

        Assert.True(sitBars.Count >= 2,
            $"a seated captain drew {sitBars.Count} amber segment(s) — the chair back and the arms on the " +
            "table are what make the pose read as SITTING rather than as merely stopped.");
        Assert.DoesNotContain(sitBars, b =>
            b.Points.Length >= 4
            && Math.Abs(b.Points[0] - ax) < 0.01f && Math.Abs(b.Points[1] - ay) < 0.01f);

        // THE BODY. Folded into a chair takes less floor — and it is still a body, not a dot.
        float StandRadius(IEnumerable<Mark> f) =>
            AtTheCaptain(f, "circle", ax, ay, scale, 0.05f).Select(m => m.Points[2]).Max();

        float upright = StandRadius(standing), folded = StandRadius(sitting);
        Assert.True(folded < upright,
            $"the seated body is the same size as the standing one ({folded} vs {upright}).");
        Assert.True(folded > upright * 0.5f,
            $"the seated body shrank to {folded} against a standing {upright} — that is a dot, not a person.");
    }

    /// <summary>#784 · …and the posture is the SIM's answer, handed down, never worked out by the renderer.
    /// #591's one-reach lesson: an instrument that derives for itself what the sim already knows is how two
    /// instruments come to disagree, and a deck that decided on its own when the captain was sitting would
    /// be exactly that with a chair in it.</summary>
    [Fact]
    public void AndThePostureIsHandedDownRatherThanWorkedOut()
    {
        string deckView = Source("Rendering", "DeckView.cs");
        Assert.Contains("bool Seated = false)", deckView, StringComparison.Ordinal);
        Assert.Contains("if (state.Seated)", deckView, StringComparison.Ordinal);
        // The renderer must not know what a table is.
        Assert.DoesNotContain("_table", deckView, StringComparison.Ordinal);

        // …and the sim hands it over off the one seated answer, which is the open table itself.
        string sim = Source("Pages", "Map.Sim.cs");
        Assert.Contains("Seated: CaptainIsSeated", sim, StringComparison.Ordinal);
        string seated = Source("Pages", "Map.Seated.cs");
        Assert.Contains("CaptainIsSeated => _table is not null", seated, StringComparison.Ordinal);
    }

    // ── (b) WASD IN A CHAIR ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · WASD WHILE SEATED RAISES THE CONFIRM AND MOVES NOBODY.
    ///
    /// <para>Owner: <i>"if I try to move when sitting down it should ask with a pop-up whether I want to
    /// stand up again."</i> RED on the shipped build, which is #778's behaviour exactly: the movement case
    /// dropped the key straight into the held set and the captain walked out of a table they were still
    /// sitting at, panel and all.</para>
    ///
    /// <para>Read as an ORDERING, the way <c>TheAutoWalkIsWiredToTheRealLegs</c> reads its own: the seated
    /// check has to happen BEFORE the key is taken, because a refusal that arrived afterwards would already
    /// have thrown away the watch and the rest the chair is holding.</para>
    /// </summary>
    [Fact]
    public void WASD_InAChairAsksBeforeItWalks()
    {
        string deck = Source("Pages", "Map.Deck.cs");

        int movementCase = deck.IndexOf("case \"d\" or \"D\" or \"ArrowRight\":", StringComparison.Ordinal);
        Assert.True(movementCase > 0, "the movement case has moved — this guard is reading the wrong file.");

        int asks = deck.IndexOf("AskWhetherToStandUp();", movementCase, StringComparison.Ordinal);
        int takes = deck.IndexOf("_deckKeys.Add(Canonical(key));", movementCase, StringComparison.Ordinal);
        Assert.True(asks > 0, "a movement key in a chair does not raise the stand-up confirm at all.");
        Assert.True(takes > 0, "the movement case no longer takes the key.");
        Assert.True(asks < takes,
            "the seated refusal happens AFTER the key is taken — the captain walks first and is asked " +
            "afterwards, which is the bug wearing a confirm.");

        // …and the second half of the same law: a key HELD before sitting down, and a route the auto-walk
        // is mid-way through, are both still live. Refusing at the keyboard alone lets a captain sit down
        // mid-stride and keep going, chair and all.
        int move = deck.IndexOf("private void MoveAvatar(", StringComparison.Ordinal);
        int guard = deck.IndexOf("if (CaptainIsSeated)", move, StringComparison.Ordinal);
        int firstStep = deck.IndexOf("_deckPlan.Move(", move, StringComparison.Ordinal);
        Assert.True(guard > move && guard < firstStep,
            "MoveAvatar can still walk a seated captain — a held key or a live route walks them out of " +
            "their own chair.");
    }

    /// <summary>#784 · Esc keeps you seated, and it cannot answer the question by doing the thing the
    /// question is about. The confirm has to be peeled BEFORE the table in the cancel chain, or the one key
    /// the design promises means "stay" would stand you up.</summary>
    [Fact]
    public void ESCAPE_KeepsTheSeatRatherThanTakingIt()
    {
        string sim = Source("Pages", "Map.Sim.cs");
        int chain = sim.IndexOf("private bool TryDismissTopOverlay()", StringComparison.Ordinal);
        Assert.True(chain > 0);

        int ask = sim.IndexOf("if (_standUpAsk) { KeepYourSeat(); return true; }", chain, StringComparison.Ordinal);
        int table = sim.IndexOf("if (_table is not null) { CloseTable(); return true; }", chain, StringComparison.Ordinal);
        Assert.True(ask > 0, "Escape does not reach the stand-up confirm at all.");
        Assert.True(ask < table,
            "Escape peels the TABLE before the confirm — the cancel key stands the captain up, which is the " +
            "opposite of what it was promised to do.");

        // And keeping your seat is free and reversible: nothing in it closes the table.
        string seated = Source("Pages", "Map.Seated.cs");
        int keep = seated.IndexOf("private void KeepYourSeat()", StringComparison.Ordinal);
        int stand = seated.IndexOf("private void StandUpFromTable()", StringComparison.Ordinal);
        Assert.True(keep > 0 && stand > keep);
        // The BODY, and not the paragraph above the next method — a doc comment naming a method is not a
        // call to it, and a guard that could not tell them apart would be reading prose for behaviour.
        int keepBody = seated.IndexOf('{', keep);
        int keepEnd = seated.IndexOf("    }", keepBody, StringComparison.Ordinal);
        Assert.True(keepBody > 0 && keepEnd > keepBody);
        Assert.DoesNotContain("CloseTable", seated[keepBody..keepEnd], StringComparison.Ordinal);

        // …and standing up goes through #757's ONE way out of a table rather than a hand-written copy of it.
        Assert.Contains("CloseTable();", seated[stand..], StringComparison.Ordinal);
    }

    // ── (c) THE REST IS WIRED TO THE BEAT THAT ALREADY EXISTS ─────────────────────────────────────────

    /// <summary>
    /// #784 · THE WAIT BEAT IS THE SHORT REST, and it spends through the systems that already own both
    /// halves.
    ///
    /// <para>Two ways this could have been got wrong, and the guard names both. A second clock — a rest that
    /// counted its own beats — would make "how long have you been sitting there" two different numbers, and
    /// #757 already decided that question. And a bare subtraction on the gauge would move the nerve
    /// anonymously, which #480 forbids outright: every recovery is whole pips with a name on it.</para>
    /// </summary>
    [Fact]
    public void THEREST_HangsOffTheWaitBeatAndSpendsThroughTheOrdinarySeams()
    {
        string table = Source("Pages", "Map.Table.cs");
        int waited = table.IndexOf("private void TableWaited(", StringComparison.Ordinal);
        Assert.True(waited > 0);
        string body = table[waited..(waited + 2400)];
        Assert.Contains("RestOneSeatedBeat(ex, beat)", body, StringComparison.Ordinal);

        string seated = Source("Pages", "Map.Seated.cs");
        Assert.Contains("ShortRest.Beat(", seated, StringComparison.Ordinal);
        Assert.Contains("ApplyNerveRelief(", seated, StringComparison.Ordinal);
        // The gauge is never touched directly — #480's law, and the reason a recovery reads back.
        Assert.DoesNotContain("_nerve =", seated, StringComparison.Ordinal);
        Assert.DoesNotContain("_nerve +=", seated, StringComparison.Ordinal);

        // The ceiling is kept per WATCH and not per table: keyed by table it would be a cap you could reset
        // by standing up and taking the next top along.
        Assert.Contains("ex.RestPipsEased", seated, StringComparison.Ordinal);
        Assert.Contains("long watch = ex.CanteenWatch;", seated, StringComparison.Ordinal);
        Assert.DoesNotContain("RestPipsEased[t.Key]", seated, StringComparison.Ordinal);
    }

    /// <summary>#784 · …and the beat's own words go on the panel the captain pressed, after the room's
    /// answer rather than instead of it. #757's silence line is the EVENT; the body's footnote is one clause
    /// added to it, and #680 says both are said inside the panel and never pulsed under its blur.
    ///
    /// <para>#793 · RESHAPED, NOT WEAKENED. The composition gained a second footnote — the bench's tail
    /// reading, which is what a beat spent sitting still in a park is FOR — and the room gained a second
    /// silence, because a park is not a hall. The law did not move, and it is now asserted AS the law rather
    /// than as one spelling of it: <b>the room's sentence leads, and every footnote follows it, in the one
    /// join, inside the panel.</b></para></summary>
    [Fact]
    public void AndTheRestSpeaksInsideThePanelAfterTheRoomHasSpoken()
    {
        string table = Source("Pages", "Map.Table.cs");
        int waited = table.IndexOf("private void TableWaited(", StringComparison.Ordinal);
        string body = table[waited..table.IndexOf("\n    /// <summary>", waited, StringComparison.Ordinal)];

        // The room's sentence leads and the footnotes follow it — the whole of the composition law.
        // Whitespace-normalised, because a guard about ORDERING that a reformat can fail is a guard about
        // formatting.
        string flat = System.Text.RegularExpressions.Regex.Replace(body, @"\s+", " ");

        int join = flat.IndexOf("WithTheBodysFootnote(", StringComparison.Ordinal);
        int room = flat.IndexOf("NobodyCame(", join + 1, StringComparison.Ordinal);
        int look = flat.IndexOf("seen)", room + 1, StringComparison.Ordinal);
        int rest = flat.IndexOf("rested)", look + 1, StringComparison.Ordinal);
        Assert.True(
            join >= 0 && room > join && look > room && rest > look,
            "the wait beat no longer composes ROOM → footnote(s) through WithTheBodysFootnote — the silence "
            + "is the event, and everything the body or the eye has to add is a clause after it.");

        // …and BOTH silences are reachable from that one composition. A park borrowing the hall's line would
        // narrate trays and eighty chairs to somebody sitting on gravel under grow-lamps (#740).
        Assert.Contains("SittingAlone.NobodyCame(", flat, StringComparison.Ordinal);
        Assert.Contains("ParkBenches.NobodyCame(", flat, StringComparison.Ordinal);

        Assert.Contains("TableAnswered(ex, t, SittingAlone.Wait", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPulseMessage", body, StringComparison.Ordinal);

        // …and the join cannot swallow the room: handed no footnote it gives back exactly what the room
        // said, which is the branch every beat that gave nothing back takes.
        string seated = Source("Pages", "Map.Seated.cs");
        Assert.Contains(
            "? $\"{saidByTheRoom} {saidByTheBody}\" : saidByTheRoom;", seated, StringComparison.Ordinal);
    }

    // ── (d) POSTURE GATES WRITING ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · A DELIBERATE ENTRY ATTEMPTED STANDING IS REFUSED, OUT LOUD, IN CORE'S OWN WORDS.
    ///
    /// <para>Owner's law: <i>"writing things down requires sitting down to be properly done."</i> The gate
    /// is Core's (<see cref="SeatedPosture.RefusalIfStanding"/>) so the client decides only what "seated"
    /// MEANS and never what seated is FOR; and the refusal is SAID through the one seam that puts a line
    /// where the captain is actually looking (#680/#736), never dropped into the pulse under a dialog's
    /// blur.</para>
    ///
    /// <para>RED on the shipped build twice over: there was no deliberate register to gate and no gate.</para>
    ///
    /// <para><b>#784 phase two · RESHAPED, LAW INTACT.</b> The write is no longer instant — it runs through
    /// #696's hold (<c>Processing.Work.Write</c>) so it wears the digging bar, and the filing therefore
    /// happens at the FAR END, in <c>TheWriteUpLands</c>. The law this guard exists for did not move: the
    /// gate is still Core's, it is still asked before anything at all happens, the refusal is still SAID on
    /// the surface the captain is looking at, and nothing is filed until the gate has been passed. What
    /// moved is which method the filing is in, so the guard now follows the seam rather than the line
    /// number. The gate widened too — posture AND privacy (<see cref="SeatedSpread"/>) — and both halves are
    /// Core's.</para>
    /// </summary>
    [Fact]
    public void DELIBERATE_WritingOnYourFeetIsRefusedInWords()
    {
        string seated = Source("Pages", "Map.Seated.cs");
        int write = seated.IndexOf("private void WriteItUp(", StringComparison.Ordinal);
        Assert.True(write > 0, "there is no deliberate write for the posture law to gate.");
        int endOfWrite = seated.IndexOf("private void TheWriteUpLands(", StringComparison.Ordinal);
        Assert.True(endOfWrite > write, "the write no longer has a far end for the entry to land at.");
        string body = seated[write..endOfWrite];

        // THE GATE IS ASKED FIRST, and it is the one that owns both halves of the law.
        int gate = body.IndexOf("SpreadRefusal is { } refusal", StringComparison.Ordinal);
        Assert.True(gate > 0, "the deliberate write does not ask the gate at all.");
        Assert.Contains("SayItWhereTheyAreLooking(refusal);", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPulseMessage", body, StringComparison.Ordinal);

        // NOTHING IS FILED HERE. The whole point of the hold is that an interruption has nothing to undo,
        // so a FileNote in this method would be an entry that survived a stand-up.
        Assert.DoesNotContain("FileNote(", body, StringComparison.Ordinal);
        int begins = body.IndexOf("BeginProcessing(ex, Core.Processing.Work.Write", StringComparison.Ordinal);
        Assert.True(begins > gate, "the clock starts before the gate is asked — or does not start at all.");

        // …and the gate itself asks CORE, for both halves. The client decides what "seated" and "alone"
        // MEAN; it never decides what a seat is FOR.
        int refusal = seated.IndexOf("private string? SpreadRefusal", StringComparison.Ordinal);
        Assert.True(refusal > 0, "there is no one gate for the two laws to live in.");
        string ladder = seated[refusal..(refusal + 400)];
        Assert.Contains("SeatedPosture.RefusalIfStanding(", ladder, StringComparison.Ordinal);
        Assert.Contains("SeatedSpread.RefusalAt(", ladder, StringComparison.Ordinal);

        // The far end files, and it files only after the gist and the once-per-item set have both agreed.
        int lands = seated.IndexOf("private void TheWriteUpLands(", StringComparison.Ordinal);
        string landing = seated[lands..seated.IndexOf("private bool CanWriteUp(", StringComparison.Ordinal)];
        Assert.Contains("FileNote(gist, SeatedPosture.WriteGlyph);", landing, StringComparison.Ordinal);
        Assert.True(
            landing.IndexOf("ex.WrittenUpProperly.Add(", StringComparison.Ordinal)
                < landing.IndexOf("FileNote(", StringComparison.Ordinal),
            "the entry is filed before the once-per-item set has agreed to it.");

        // …and the automatic gist-once jot is UNTOUCHED. The owner's own words name it as the standing
        // register that stays — "a scrawl on a moving knee, #696's idiom" — so #696's leave-and-photograph
        // must still work with both feet on the floor, or this issue has deleted a shipped feature.
        string surface = Source("Pages", "Map.Surface.cs");
        int leave = surface.IndexOf("private void LeaveItem(", StringComparison.Ordinal);
        int setDown = surface.IndexOf("private void SetItDown(", StringComparison.Ordinal);
        Assert.True(leave > 0 && setDown > leave);
        Assert.DoesNotContain("SeatedPosture", surface[leave..setDown], StringComparison.Ordinal);
        Assert.Contains("Core.Processing.Work.File", surface[leave..setDown], StringComparison.Ordinal);
    }

    /// <summary>#784 · The pen is drawn on your FEET as well as in a chair, and it is live either way. A
    /// control that vanished when the law said no would teach the law to nobody, which is #603 read
    /// backwards — a refusal is said, not hidden.</summary>
    [Fact]
    public void AndThePenIsDrawnStandingUpSoTheLawCanBeLearnedByPressingIt()
    {
        string razor = Source("Pages", "Map.razor");
        int pen = razor.IndexOf("WriteItUp(item)", StringComparison.Ordinal);
        Assert.True(pen > 0, "the deliberate write has no control anywhere in the satchel.");

        // The row's condition is about whether there is anything left to write, not about posture.
        int rowGate = razor.LastIndexOf("@if (CanWriteUp(item))", pen, StringComparison.Ordinal);
        Assert.True(rowGate > 0 && pen - rowGate < 700,
            "the pen is drawn under some other condition than 'is there anything left to write'.");
        Assert.DoesNotContain("@if (CaptainIsSeated)", razor[rowGate..pen], StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=", razor[rowGate..pen], StringComparison.Ordinal);
    }

    // ── THE DEMO ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · BOTH HALVES OF THE REST ARE REACHABLE ON DEMAND, AND THE GUIDE SAYS HOW.
    ///
    /// <para>#693's rule, which this project keeps having to re-learn: a scene nobody can reach on demand is
    /// a scene that ships broken. A recovery mechanic demonstrated on a steady, unmarked captain
    /// demonstrates nothing at all — <c>ApplyNerveRelief</c> is honest and gives nothing back to somebody who
    /// has lost nothing, so sitting down would look exactly like a control that did not fire.</para>
    /// </summary>
    [Fact]
    public void THEDEMO_LetsATesterWatchTheRestActuallyWork()
    {
        string sim = Source("Pages", "Map.Sim.cs");
        Assert.Contains("\"low\" or \"fraying\" => 2,", sim, StringComparison.Ordinal);
        Assert.Contains("pair.StartsWith(\"hurt=\"", sim, StringComparison.Ordinal);

        // …and the hurt cheat can never boot a tester into a death card.
        Assert.Contains("blows < CaptainCondition.MaxHits", sim, StringComparison.Ordinal);

        string guide = Doc("testing-guide.md");
        Assert.Contains("nerve=low", guide, StringComparison.Ordinal);
        Assert.Contains("?hurt=", guide, StringComparison.Ordinal);

        string hive = Doc("testing-links-the-hive.md");
        Assert.Contains("nerve=low", hive, StringComparison.Ordinal);
        Assert.Contains("hurt=", hive, StringComparison.Ordinal);
    }
}
