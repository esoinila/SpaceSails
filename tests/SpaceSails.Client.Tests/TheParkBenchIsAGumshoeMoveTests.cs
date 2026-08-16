using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #793 · THE BENCHES TAKE THE SIT VERB, THE PLANK SAYS WHICH HALF IS FREE, AND A TAIL THAT HAS TO STOP IS
/// DRAWN STOPPING.
///
/// <para>Owner, from play in the new park: <i>"E at a steel bench seats you"</i> · <i>"it is a good gumshoe
/// move to see if anyone is following us by foot, as they would need to stop moving also"</i> · <i>"the park
/// bench might be used to go through inventory info loot, if we get the whole bench to ourselves."</i></para>
///
/// <h3>The test-only tail, and why it is not a cheat</h3>
///
/// <para>Nothing in this game follows the captain yet — #804's rounds are laid before the captain steps off
/// the car and can never be tails, which Core's own guards prove. That makes the HOLD a branch no shipped
/// world can enter, and a drawing guard that could not enter it would be asserting nothing. So the pixel
/// guard below builds a figure with <c>Held: true</c> straight onto the plan and renders it with the real
/// <see cref="DeckView"/>: the seam is proved from both directions on the marks the pen actually laid, and
/// no NPC was invented to do it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheParkBenchIsAGumshoeMoveTests
{
    private const int WidthPx = 1200, HeightPx = 700;
    private const int Watches = 6;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

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

    private static string Seated() =>
        Source("Pages", "Map.Seated.cs") + Source("Pages", "Seating", "Seating.Seated.cs");

    private static string Bench() =>
        Source("Pages", "Map.Bench.cs") + Source("Pages", "Seating", "Seating.Bench.cs");


    /// <summary>#870 · The deck page is seven partials by subject now, so "the deck" a guard reads over is
    /// all of them — exactly the text it read out of one file before the split.</summary>
    private static string Deck() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Deck*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>#870 · The round is six partials by subject now, so the page this guard reads is all six —
    /// concatenated in the order the one file laid them out, which is exactly the text it read before the
    /// split. The count is asserted, so a seventh part can never go unread.</summary>
    private static string Patrol()
    {
        string dir = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages");
        string[] order =
        [
            "Map.Patrol.cs", "Map.Patrol.Hide.cs", "Map.Patrol.Round.cs",
            "Map.Patrol.Challenge.cs", "Map.Patrol.Escort.cs", "Map.Patrol.Run.cs",
        ];
        Assert.Equal(order.Length, Directory.GetFiles(dir, "Map.Patrol*.cs").Length);

        // #870 lane 6′b · RE-PATHED, never re-asserted. The round's twenty-two fields and the Guard they
        // are made of moved into Pages/Patrol/, so the page this guard reads is EIGHT files now — the six
        // verbs first, in the order the one file laid them out, then the state. Both directories are
        // counted, so a ninth part can never go unread.
        string own = Path.Combine(dir, "Patrol");
        string[] state = ["Patrol.cs", "Guard.cs"];
        Assert.Equal(state.Length, Directory.GetFiles(own, "*.cs").Length);

        var parts = new string[order.Length + state.Length];
        for (int i = 0; i < order.Length; i++)
        {
            parts[i] = File.ReadAllText(Path.Combine(dir, order[i]));
        }
        for (int i = 0; i < state.Length; i++)
        {
            parts[order.Length + i] = File.ReadAllText(Path.Combine(own, state[i]));
        }
        return string.Concat(parts);
    }

    private static string CoreSource(string name) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Core", name));

    private static string Doc(string name) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "docs", name));

    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park)> EveryPark()
    {
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || UndergroundComplex.IsHeadOffice(body))
            {
                continue;
            }
            if (UndergroundComplex.Build(body, level, Field).Park is { } park)
            {
                yield return (body, level, park);
            }
        }
    }

    // ── THE PEN THAT REMEMBERS ────────────────────────────────────────────────────────────────────────

    private sealed record Mark(string Kind, float[] Points, RgbaColor Ink, float Width);

    private sealed class RecordingRenderer : IRenderer
    {
        public List<Mark> Marks { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Marks.Clear();

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark(fill is null ? "ring" : "disc", [x, y, r], fill ?? stroke, w));

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

    /// <summary>The three inks, transcribed from <c>DeckView</c> deliberately — #792's own note: a test that
    /// asked the renderer for its own constants could not notice one turning into another.</summary>
    private static readonly RgbaColor SeatEmpty = new(126, 142, 164, 210);
    private static readonly RgbaColor SeatOpen = new(120, 220, 200, 235);
    private static readonly RgbaColor SeatTaken = new(228, 196, 150, 240);

    private static bool Is(RgbaColor a, RgbaColor b) => a.R == b.R && a.G == b.G && a.B == b.B;

    private static (List<Mark> Marks, DeckPlan Plan, DeckView.Placement Place) Frame(
        string body, int level, long watch)
    {
        DeckPlan plan = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], watch);
        (double ax, double ay) = HiveInterior.SpawnOn(Field);
        var pen = new RecordingRenderer();
        new DeckView(pen).Draw(plan, WidthPx, HeightPx, 0,
            new DeckView.State(ax, ay, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false),
            0, 0, null);
        return (pen.Marks, plan, DeckView.PlacementFor(plan, WidthPx, HeightPx, ax, ay, 0, 0));
    }

    private static (float X, float Y) At(DeckView.Placement p, double x, double y) =>
        (p.Ox + ((float)x * p.Scale), p.Oy - ((float)y * p.Scale));

    private static bool Near(Mark m, (float X, float Y) at, float px) =>
        Math.Sqrt(((m.Points[0] - at.X) * (m.Points[0] - at.X))
                + ((m.Points[1] - at.Y) * (m.Points[1] - at.Y))) <= px;

    // ── (a) THE BENCH ENDS ARE DRAWN, FREE AND TAKEN, BOTH WAYS ──────────────────────────────────────

    /// <summary>
    /// #793/#795 · EVERY BENCH END IS DRAWN AND WHAT IS DRAWN IS WHAT THE SIM SAYS.
    ///
    /// <para>The privacy predicate is <b>the whole bench</b>, so "is this bench free" has to be answerable
    /// from across a 278 du room. Every end of every bench in every park is walked and checked against
    /// <see cref="ParkBenches.On"/>: the figure's end must be a filled warm disc, the far end must be a
    /// hollow ring, and a free end BESIDE somebody must go the invitation green a free chair at an occupied
    /// top goes — one language for one question, rather than the park having its own dialect.</para>
    ///
    /// <para><b>Proven RED</b> by drawing every end the same (dropping <c>Taken</c> and
    /// <c>BenchHasSomebody</c> from the seats <c>HiveInterior</c> publishes), which is what the room looked
    /// like before this issue:</para>
    /// <code>
    /// 18 bench end(s) are drawn as something other than what the sim says:
    ///   luna B1 bench 0 end 0: the sim says TAKEN and the deck drew it free.
    ///   luna B1 bench 0 end 1: the sim says free BESIDE SOMEBODY and the deck drew it as an empty room's
    ///     chair.
    ///   phobos B1 bench 0 end 0: the sim says TAKEN and the deck drew it free.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void EVERY_BENCH_END_DrawsWhatTheSimSaysIsOnIt()
    {
        var wrong = new List<string>();
        int parks = 0, drawnTaken = 0, drawnOpen = 0, drawnEmpty = 0;

        foreach ((string body, int level, UndergroundComplex.Park park) in EveryPark())
        {
            parks++;
            (List<Mark> marks, DeckPlan plan, DeckView.Placement place) = Frame(body, level, 0);

            IReadOnlyList<ParkBenches.Bench> seats = ParkBenches.On(in park);
            Assert.Equal(seats.Count * ParkBenches.Ends, plan.BenchSeats.Length);

            foreach (ParkBenches.Bench bench in seats)
            {
                for (int end = 0; end < ParkBenches.Ends; end++)
                {
                    bool sim = bench.Taken && end == ParkBenches.TakenEnd;
                    bool beside = bench.Taken && !sim;

                    (double ex, double ey) = bench.End(end);
                    (float X, float Y) at = At(place, ex, ey);

                    List<Mark> here = marks
                        .Where(m => m.Kind is "disc" or "ring" && Near(m, at, 0.5f * place.Scale))
                        .ToList();

                    if (here.Count != 1)
                    {
                        wrong.Add($"  {body} B{-level} bench {bench.Index} end {end}: {here.Count} mark(s) "
                            + "on the seat, expected one.");
                        continue;
                    }

                    bool taken = here[0].Kind == "disc" && Is(here[0].Ink, SeatTaken);
                    bool open = here[0].Kind == "ring" && Is(here[0].Ink, SeatOpen);
                    bool empty = here[0].Kind == "ring" && Is(here[0].Ink, SeatEmpty);

                    if (sim && taken)
                    {
                        drawnTaken++;
                    }
                    else if (beside && open)
                    {
                        drawnOpen++;
                    }
                    else if (!sim && !beside && empty)
                    {
                        drawnEmpty++;
                    }
                    else if (sim)
                    {
                        wrong.Add($"  {body} B{-level} bench {bench.Index} end {end}: the sim says TAKEN "
                            + "and the deck drew it free.");
                    }
                    else if (beside)
                    {
                        wrong.Add($"  {body} B{-level} bench {bench.Index} end {end}: the sim says free "
                            + "BESIDE SOMEBODY and the deck drew it as an empty room's chair.");
                    }
                    else
                    {
                        wrong.Add($"  {body} B{-level} bench {bench.Index} end {end}: the sim says free "
                            + "and the deck drew it TAKEN.");
                    }
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} bench end(s) are drawn as something other than what the sim says:\n"
            + string.Join("\n", wrong.Take(8)));

        // ANTI-VACUITY, and it is the whole point: all THREE states are met, in real rooms. A park where
        // every plank drew the same glyph would sail through the loop above.
        Assert.True(parks >= 8, $"only {parks} parks were read — this proved little.");
        Assert.True(drawnTaken >= parks,
            $"{drawnTaken} taken end(s) over {parks} parks — the lone figure never reached the deck.");
        Assert.True(drawnOpen >= parks,
            $"{drawnOpen} invitation(s) over {parks} parks — the free end of a shared bench is never drawn "
            + "as the offer it is.");
        Assert.True(drawnEmpty >= parks * 2,
            $"{drawnEmpty} plain empty end(s) over {parks} parks — the room is not drawing its free seats.");
    }

    // ── (b) THE HOLD IS DRAWN, AND ONLY WHEN SOMETHING IS HOLDING ────────────────────────────────────

    /// <summary>One figure on an otherwise bare deck, held or not — the test-only tail.</summary>
    private static DeckPlan OneFigure(bool held) => new(
        walls: [], consoles: [], roomLabels: [], backdrops: [],
        spawnX: 0, spawnY: 0, droidCount: 1,
        fillDroids: (_, buffer) =>
            buffer[0] = new DeckPlan.Droid(6, 0, 0, PatrolBeat.DeckName(0), held),
        location: (_, _) => "the walk");

    private static List<Mark> FrameOf(DeckPlan plan)
    {
        var pen = new RecordingRenderer();
        new DeckView(pen).Draw(plan, WidthPx, HeightPx, 0,
            new DeckView.State(0, 0, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false),
            0, 0, null);
        return pen.Marks;
    }

    /// <summary>
    /// #793 · A TAIL THAT HAS HAD TO STOP IS DRAWN STOPPING — and nothing else is.
    ///
    /// <para>Owner: <i>"they would need to stop moving also."</i> The hold is only worth having if a player
    /// can SEE it, so it wears #792's own warm seated ink: a bar struck across the back of somebody who has
    /// settled, which is the mark this deck already uses for a figure in a chair.</para>
    ///
    /// <para>Measured on the marks the real pen laid, twice, over one figure at one coordinate — the only
    /// difference between the two frames is the <c>Held</c> flag the sim hands down. <b>Proven RED</b> by
    /// deleting the <c>if (droid.Held)</c> block from <c>DeckView</c>:</para>
    /// <code>
    /// a figure the sim says has STOPPED because the captain sat down is drawn exactly like one still
    /// walking — 0 warm segment(s) over it, against 0 when nothing is holding.
    /// </code>
    ///
    /// <para>…and RED the other way by drawing the bar unconditionally, which would mark every guard in the
    /// building as a caught tail:</para>
    /// <code>
    /// a figure that is NOT holding wears the stopped mark — 1 warm segment(s) over somebody who is walking.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_HOLD_IsDrawnAndOnlyWhenSomethingIsHolding()
    {
        static int WarmBarsOverTheFigure(DeckPlan plan)
        {
            DeckView.Placement place = DeckView.PlacementFor(plan, WidthPx, HeightPx, 0, 0, 0, 0);
            (float X, float Y) at = (place.Ox + (6f * place.Scale), place.Oy);
            return FrameOf(plan)
                .Count(m => m.Kind == "polyline"
                    && Is(m.Ink, SeatTaken)
                    && Math.Abs(m.Points[0] - at.X) <= 2f * place.Scale
                    && Math.Abs(m.Points[1] - at.Y) <= 2f * place.Scale);
        }

        int walking = WarmBarsOverTheFigure(OneFigure(held: false));
        int stopped = WarmBarsOverTheFigure(OneFigure(held: true));

        Assert.True(walking == 0,
            $"a figure that is NOT holding wears the stopped mark — {walking} warm segment(s) over somebody "
            + "who is walking.");
        Assert.True(stopped == 1,
            "a figure the sim says has STOPPED because the captain sat down is drawn exactly like one still "
            + $"walking — {stopped} warm segment(s) over it, against {walking} when nothing is holding.");
    }

    /// <summary>#793 · …and the flag really is HANDED DOWN. The pen must not be able to work out for itself
    /// whether anybody is following the captain — that is a fact about a route, and #788's one-reach lesson
    /// applies to somebody else's feet exactly as it applies to the captain's posture. <b>Proven RED</b> by
    /// letting <c>DeckView</c> ask Core: <c>Assert.DoesNotContain("FootTail")</c> goes red the moment the
    /// pen names it (asserted in <c>SeatsAreDrawnTests.THE_PEN_IsHandedTheOccupancy…</c>, which sweeps the
    /// whole forbidden list).</summary>
    [Fact]
    public void THE_HELD_FLAG_RidesDownFromTheSimOnTheDroid()
    {
        string plan = Source("Rendering", "DeckPlan.cs");
        Assert.Contains(
            "double X, double Y, double FacingRad, string Name, bool Held = false", plan,
            StringComparison.Ordinal);

        // …written by the one stepper, read by the one filler, so the figure that has stopped and the
        // figure drawn as stopped are one figure.
        string patrol = Patrol();
        Assert.Contains("g.Held = FootTail.MustHold(", patrol, StringComparison.Ordinal);
        Assert.Contains("_patrol.Guards[i].DeckName, _patrol.Guards[i].Held", patrol, StringComparison.Ordinal);
    }

    // ── (c) THE SIT VERB IS WIRED, AND IT IS WIRED TO CORE'S OWN BENCH ───────────────────────────────

    /// <summary>
    /// #793 · [E] AT A BENCH SITS YOU DOWN, THROUGH CORE'S OWN LIST.
    ///
    /// <para>#790 drew the benches and said, in the renderer's own comment, that the verb would arrive with
    /// #778. This asserts it arrived: the dispatcher answers <c>HiveBench</c>, the press finds WHICH bench by
    /// asking <see cref="ParkBenches.At"/> rather than by measuring one, and nothing anywhere places the
    /// captain onto the furniture — the seat is the spot you walked to, which is #757's own posture and the
    /// reason this file has never moved anybody to sit down.</para>
    ///
    /// <para><b>Proven RED</b> by removing the dispatch arm — the plate says SIT DOWN and the key answers
    /// nothing, which is #757's original complaint restated in a garden:</para>
    /// <code>
    /// [E] no longer answers at a park bench.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_SIT_VERB_IsWiredToTheBenchAndAsksCoreWhichOne()
    {
        string deck = Deck();
        int at = deck.IndexOf("case DeckPlan.ConsoleKind.HiveBench:", StringComparison.Ordinal);
        Assert.True(at >= 0, "[E] no longer answers at a park bench.");
        Assert.Contains("TryTakeBench();", deck[at..(at + 700)], StringComparison.Ordinal);

        string bench = Bench();
        Assert.Contains("{ Kind: DeckPlan.ConsoleKind.HiveBench } spot", bench, StringComparison.Ordinal);
        Assert.Contains("ParkBenches.At(in green, spot.X, spot.Y)", bench, StringComparison.Ordinal);

        // The press itself places nobody: it is a lookup, and the placing is the SITTING's (#820, where the
        // captain is snapped onto the end they walked up to). Keeping the two apart is what lets the dev row
        // and the key press open one bench through one method — and StandCaptainAt in particular must never
        // appear on this path, because that nudge walks a body OUT of collision and a plank is solid.
        int sit = bench.IndexOf("public bool TryTakeBench()", StringComparison.Ordinal);
        string press = bench[sit..bench.IndexOf("\n        /// <summary>", sit, StringComparison.Ordinal)];
        Assert.DoesNotContain("StandCaptainAt", press, StringComparison.Ordinal);
        Assert.DoesNotContain("SitCaptainOn", press, StringComparison.Ordinal);

        // …and the sitting is opened in ONE place — one definition, and every way in goes through it, so a
        // dev row and a key press cannot open two different benches. A second sitting built in this file
        // would be the first named bug class with a plank under it.
        //
        // #870 lane 6d · RE-NEEDLED, and it is the one needle this lane moved. The claim is untouched — ONE
        // sitting is built in this file — but the spelling of building one is now `TakeThisSeat(new
        // TableTalk { … })`, because the tail every site shared (the field, the reveal cue, the draw) is one
        // method in `Seating.Sit.cs`. The old needle `Table = new TableTalk` no longer exists ANYWHERE in
        // the client, so leaving it would have been a guard counting to one over a string that can never
        // appear — a green test that asserts nothing. That six sites build exactly six sittings and all six
        // go through the one method is held by `EverySeatTheCaptainTakesFingerprintsTheSameTests`.
        Assert.Equal(1, bench.Split("private void SitOnThisBench(").Length - 1);
        Assert.Equal(1, bench.Split("TakeThisSeat(new TableTalk").Length - 1);
        Assert.True(bench.Split("SitOnThisBench(").Length - 1 >= 3,
            "the bench sitting is no longer reached from both the [E] press and the dev row.");
    }

    /// <summary>
    /// #793 · WHICH RUNG, AND WHETHER THE BENCH IS YOURS — both asked of the room, and they are two facts.
    ///
    /// <para><c>Solo</c> means <i>this is not a conversation</i> (it docks the frame and runs the wait beat);
    /// <c>SharedSeat</c> means <i>somebody can read what is on your knee</i>. A stranger on the far end of a
    /// plank is the second without being the first, and collapsing them would either raise a full
    /// conversation card over a park or call a shared bench private.</para>
    ///
    /// <para><b>Proven RED</b> by reading <c>Solo</c> alone for the privacy gate, which is what the file did
    /// before benches existed:</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// Not found: "return t.Solo &amp;&amp; !t.SharedSeat;"
    /// </code>
    /// </summary>
    [Fact]
    public void THE_RUNG_AndTheWholeBenchFact_AreBothAskedOfTheRoom()
    {
        string seated = Seated();

        // The rung. #799 left the bench in the enum with nothing pointing at it; this is the one line.
        Assert.Contains("t.Bench ? SeatedHud.Seat.ParkBench", seated, StringComparison.Ordinal);
        // …and the cabinet mapping it was added in front of is untouched (#799's own guard reads this too).
        Assert.Contains("t.Quiet ? SeatedHud.Seat.Cabinet : SeatedHud.Seat.HallTable", seated,
            StringComparison.Ordinal);

        // The privacy fact: BOTH questions, asked together.
        Assert.Contains("return t.Solo && !t.SharedSeat;", seated, StringComparison.Ordinal);

        // The gate itself is still Core's one predicate and this file still decides only what the two words
        // mean here (#784's founding division of labour).
        Assert.Contains("SeatedSpread.CanSpreadTheCase(seat, SeatedAlone)", seated, StringComparison.Ordinal);
    }

    /// <summary>
    /// #793/#799 · SOMEBODY SITS DOWN NEXT TO YOU AND THE PAPERS GO AWAY — before the privacy does.
    ///
    /// <para>Owner: <i>"somebody sitting down next to your open case files is a BEAT."</i> #799 built the
    /// seam (<c>Interruption.CompanyArrived</c>, ended BEFORE the flag flips, so the hold has nothing to
    /// undo); this asserts a bench uses it, and that it does NOT borrow the table's arrival — which flips
    /// <c>Solo</c> and would raise the full conversation card over a stranger with a cup who has not looked
    /// up.</para>
    ///
    /// <para><b>Proven RED</b> by pointing a bench's arrival at <c>SomebodyTakesTheChair</c>:</para>
    /// <code>
    /// the papers are not put away BEFORE the bench stops being yours — an interruption that fires after
    /// the gate has shut is an interruption with nothing left to undo honestly.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_COMPANY_BEAT_PutsThePapersAwayBeforeThePrivacyGoes()
    {
        string bench = Bench();
        int at = bench.IndexOf(
            "private void SomebodyTakesTheOtherEnd(", StringComparison.Ordinal);
        Assert.True(at >= 0, "a bench no longer has an arrival of its own.");
        string beat = bench[at..bench.IndexOf("\n        // ──", at, StringComparison.Ordinal)];

        int away = beat.IndexOf(
            "AbandonProcessing(ex, Core.Processing.Interruption.CompanyArrived)", StringComparison.Ordinal);
        int shared = beat.IndexOf("t.SharedSeat = true;", StringComparison.Ordinal);
        Assert.True(away >= 0 && shared > away,
            "the papers are not put away BEFORE the bench stops being yours — an interruption that fires "
            + "after the gate has shut is an interruption with nothing left to undo honestly.");

        // …and it is NOT the table's arrival. Solo is untouched: you are still sitting, still resting, and
        // nobody has started a conversation with you.
        Assert.DoesNotContain("t.Solo", beat, StringComparison.Ordinal);
        Assert.DoesNotContain("SomebodyTakesTheChair", beat, StringComparison.Ordinal);
        Assert.Contains("ParkBenches.SomebodyTookTheOtherEndLine", beat, StringComparison.Ordinal);

        // …and the wait beat routes a bench to it rather than to the hall's.
        string table = Table();
        Assert.Contains("SomebodyTakesTheOtherEnd(ex, t);", table, StringComparison.Ordinal);
        Assert.Contains("ParkBenches.TheOtherEndIsFree(t.SharedSeat)", table, StringComparison.Ordinal);
    }

    // ── (d) THE TAIL-CHECK SEAM ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #793 · THE BENCH ASKS THE QUESTION, ON THE BEAT, AND EVERY MOVER IS BUILT THROUGH THE ONE FACTORY.
    ///
    /// <para>The reading is taken on the beat spent SITTING STILL, never on the press that sat you down —
    /// sitting still is what makes it possible, so an answer handed over before you had sat would be an
    /// answer to a question nobody asked. And every mover the bench is shown is built through
    /// <see cref="PatrolBeat.OnTheRound"/>, which stamps the disqualifying clause: this file cannot hand the
    /// question a guard that has quietly become a tail.</para>
    ///
    /// <para><b>Proven RED</b> by building the movers by hand
    /// (<c>new FootTail.Mover(name, x, y)</c>), which compiles, ships, and silently drops the one clause
    /// that makes a round not a tail:</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// Not found: "afoot.Add(PatrolBeat.OnTheRound(i, _guards[i]"···
    /// </code>
    ///
    /// <para>That RED is kept verbatim because it is what was read that day. #870 lane 6′a has since
    /// renamed the receiver: the bench asks the patrol family for <c>TheRoundOnFoot</c> rather than reading
    /// its <c>_patrol.Guards</c>, so the needle below spells it that way. The claim is the same one.</para>
    /// </summary>
    [Fact]
    public void THE_TAIL_QUESTION_IsAskedOnTheBeatAndOfCore()
    {
        string bench = Bench();
        Assert.Contains(
            "afoot.Add(PatrolBeat.OnTheRound(i, round[i].X, round[i].Y));", bench,
            StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<Guard> round = TheRoundOnFoot;", bench, StringComparison.Ordinal);
        Assert.Contains(
            "FootTail.AnythingTailing(_avatarX, _avatarY, MoversAfoot(), SightBlockers())", bench,
            StringComparison.Ordinal);
        Assert.Contains("FootTail.Reading(AnythingTailingYou)", bench, StringComparison.Ordinal);

        // The client decides nothing about what a tail IS. No geometry, no headings, no distance of its own.
        int reading = bench.IndexOf("private string TheTailReading()", StringComparison.Ordinal);
        Assert.True(reading >= 0);
        Assert.DoesNotContain("Math.Atan2", bench, StringComparison.Ordinal);

        // The beat asks it, and only on a bench: a hall table gives everybody a reason to sit, so the same
        // move there proves nothing and must not print a sentence claiming otherwise.
        string table = Table();
        int waited = table.IndexOf("private void TableWaited(", StringComparison.Ordinal);
        string beat = table[waited..table.IndexOf("\n        /// <summary>", waited, StringComparison.Ordinal)];
        Assert.Contains("t.Bench ? _host.TheTailReading() : null", beat, StringComparison.Ordinal);

        // …and the press that sits you down says nothing about it.
        int sit = bench.IndexOf("private void SitOnThisBench(", StringComparison.Ordinal);
        string press = bench[sit..bench.IndexOf("\n        // ──", sit, StringComparison.Ordinal)];
        Assert.DoesNotContain("TheTailReading", press, StringComparison.Ordinal);
    }

    /// <summary>
    /// #793 · AND THE HOLD IS ON THE STEPPER EVERY MOVER GOES THROUGH.
    ///
    /// <para>It is asked once a frame, of every walker, and the answer today is always no. That is the point
    /// of putting it here rather than in a branch somebody would have to remember later: the day something
    /// on these floors is following the captain, the bench already stops it.</para>
    ///
    /// <para><b>Proven RED</b> by walking the round unconditionally (dropping the <c>g.Held</c> fork), which
    /// leaves a caught tail strolling past a captain who has just sat down and looked straight at them. On
    /// the shipped chain of 2026-08-11 that read:</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// Not found: "else"
    /// </code>
    ///
    /// <para><b>#870 · Re-NEEDLED, and proven RED again in the new shape.</b> The <c>if / else if</c> chain
    /// is a LIST now: <c>AdvancePatrol</c> asks the law and hands the answer to
    /// <c>TheOneThingHeIsDoingThisFrame</c>, whose whole body is the priority order. So the fork is pinned as
    /// the POSITION of the cover-act arm in that list — asked, then forked on, then the round — rather than
    /// as the word <c>else</c>, and the arm's own body is pinned to its guard clause. Deleting
    /// <c>if (HeIsMadeAndCoveringForIt(…)) { return; }</c> from the conductor gives:</para>
    /// <code>
    /// the stepper walks every mover whatever a bench has decided — a hold nothing consults is a law
    /// with no teeth.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_HOLD_IsSpentOnTheOneStepperEveryMoverGoesThrough()
    {
        string patrol = Patrol();
        int at = patrol.IndexOf("private void AdvancePatrol(", StringComparison.Ordinal);

        // #870 · The window is the step AND the conductor it hands each man to, which is everything from
        // AdvancePatrol's signature down to the first arm — the two of them are the one stepper now.
        int conductor = patrol.IndexOf(
            "private void TheOneThingHeIsDoingThisFrame(", at, StringComparison.Ordinal);
        int firstArm = patrol.IndexOf("private bool HeIsWalkingYouOut(", conductor, StringComparison.Ordinal);
        Assert.True(at >= 0 && conductor > at && firstArm > conductor,
            "the step, its conductor and its arms are not where this guard thinks they are.");
        string loop = patrol[at..firstArm];

        int asked = loop.IndexOf("g.Held = FootTail.MustHold(", StringComparison.Ordinal);
        int forked = loop.IndexOf("if (HeIsMadeAndCoveringForIt(", StringComparison.Ordinal);
        int walked = loop.IndexOf("WalkTheRound(g, dt, walls);", StringComparison.Ordinal);
        Assert.True(asked >= 0 && forked > asked && walked > forked,
            "the stepper walks every mover whatever a bench has decided — a hold nothing consults is a law "
            + "with no teeth.");

        // A stopped mover drops off the motion fan honestly, the same clause a stand at a stop keeps — or
        // the tracker would report travel that is not happening.
        //
        // #831 · IT IS ONE CALL FURTHER DOWN NOW, and it is the same law. A made tail no longer freezes BARE
        // — owner: "A MADE tail performs a COVER ACT instead of freezing bare: turns to the nearest wall
        // fixture, checks a plate, reads a docket — same hold, same honest Vx=0, but the picture says 'man
        // with business' not 'statue'." So the held branch calls TheCoverAct and the zeroing this guard was
        // written to protect lives inside it, on every path out of it. FOLLOWED rather than relaxed: the
        // clause is asserted where it now is, and the stepper is still pinned to the branch that reaches it.
        //
        // #870 · …and the branch is a named arm now, so the call and the guard clause that reaches it are
        // asserted TOGETHER: an arm that called TheCoverAct on somebody who is not held would be the hold
        // selecting everybody, which is the same law failing the other way round.
        int arm = patrol.IndexOf("private bool HeIsMadeAndCoveringForIt(", StringComparison.Ordinal);
        Assert.True(arm > 0, "the cover-act arm has moved — this guard can no longer see the fork it guards.");
        string heldArm = patrol[arm..patrol.IndexOf("\n    /// <summary>", arm, StringComparison.Ordinal)];
        Assert.Contains("if (!g.Held)", heldArm, StringComparison.Ordinal);
        Assert.Contains("TheCoverAct(g, dt, walls, sight);", heldArm, StringComparison.Ordinal);

        int act = patrol.IndexOf("private void TheCoverAct(", StringComparison.Ordinal);
        Assert.True(act >= 0, "TheCoverAct has moved — this guard can no longer see the hold it guards.");
        string cover = patrol[act..patrol.IndexOf("\n    // ──", act, StringComparison.Ordinal)];
        Assert.Contains("g.Vx = 0;", cover, StringComparison.Ordinal);
        Assert.Contains("g.Vy = 0;", cover, StringComparison.Ordinal);

        // …and the seated fact is the bench's, asked once for the frame.
        Assert.Contains("bool sitting = SeatedOnABenchInTheOpen;", loop, StringComparison.Ordinal);
        // #870 lane 6b - re-PATHED: the predicate is a pure function of the seat's own state, so it moved
        // onto Seating with the other fourteen. Same one line, same claim, asserted where it now is.
        Assert.Contains(
            "public bool SeatedOnABenchInTheOpen => Table is { Bench: true };",
            Source("Pages", "Seating", "Seating.cs"), StringComparison.Ordinal);
    }

    /// <summary>#793 · Core carries the law and says out loud that no shipped mover can be a tail, so a
    /// future crew reads the seam rather than rediscovering it.</summary>
    [Fact]
    public void CORE_SaysOutLoudThatARoundCanNeverBeATail()
    {
        // #870 · The module is one partial class spread over PatrolBeat*.cs. Same needle, same code, new
        // path — the source read here is the concatenation of every part, in ordinal order.
        string patrol = string.Concat(Directory
            .EnumerateFiles(Path.Combine(RepoRoot(), "src", "SpaceSails.Core"), "PatrolBeat*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));
        Assert.Contains(
            "FootTail.OnARound(DeckName(index), x, y)", patrol, StringComparison.Ordinal);

        string tail = CoreSource("FootTail.cs");
        Assert.Contains("OnAPublishedRound: true, Tailing: false", tail, StringComparison.Ordinal);
        Assert.Contains(
            "mover.Tailing && !mover.OnAPublishedRound", tail, StringComparison.Ordinal);
    }

    // ── (e) THE DEMO ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>#793 · The row a tester opens, and it goes through the same handler [E] does. A scene nobody
    /// can reach on demand is a scene that ships broken (#693).</summary>
    [Fact]
    public void THE_DEMO_ROW_SitsYouOnABenchWithThePapersOut()
    {
        string bench = Bench();
        int at = bench.IndexOf("public bool SitOnAFreeBenchIfAsked(", StringComparison.Ordinal);
        Assert.True(at >= 0, "?park=1&spread=1 no longer sits anybody down.");
        string row = bench[at..];

        Assert.Contains("ParkBenches.On(in green)", row, StringComparison.Ordinal);
        Assert.Contains("SeedTheSpreadFinds();", row, StringComparison.Ordinal);
        Assert.Contains("SitOnThisBench(in green, bench);", row, StringComparison.Ordinal);
        // It takes a FREE bench: the whole point of the row is the spread being allowed.
        Assert.Contains("if (bench.Taken)", row, StringComparison.Ordinal);

        // …and the park's landing calls it before it stands the captain at the gate, so the row lands ON the
        // bench rather than beside it.
        string surface = Source("Pages", "Map.Surface.Cheats.cs");
        int park = surface.IndexOf("private void StandInTheParkIfAsked(", StringComparison.Ordinal);
        string landing = surface[park..(park + 1600)];
        int sits = landing.IndexOf("SitOnAFreeBenchIfAsked(in green)", StringComparison.Ordinal);
        int gate = landing.IndexOf("StandCaptainAt(green.X, green.Y", StringComparison.Ordinal);
        Assert.True(sits >= 0 && gate > sits,
            "the park's dev row walks to the gate before it looks for a bench, so the bench row can never "
            + "fire.");

        // It asks the ROOM which way a bench faces rather than typing an offset. The carve puts every bench
        // on the OUTSIDE of its bend — some above the walk, some below it — so "a bit under the plank" is a
        // guess, and §13.15's second cause is a caller doing geometry about furniture it did not bolt down.
        Assert.Contains("TowardTheWalk(in green, sx, sy)", row, StringComparison.Ordinal);
        // #820 · …and that arithmetic is CORE'S now, because standing up off a bench spends it too — the
        // walk side of a plank stopped being a dev row's private measurement the moment it became the
        // square a captain is stood back up on.
        Assert.Contains("ParkBenches.TowardTheWalk(in green,", bench, StringComparison.Ordinal);

        // The tester is told the row exists, on the page that already opens this room.
        string guide = Doc("testing-guide.md");
        Assert.Contains("?park=1&spread=1", guide, StringComparison.Ordinal);
        Assert.Contains(DevStarts.All.First(r => r.Url.Contains("park=1&spread=1", StringComparison.Ordinal)).Label,
            guide, StringComparison.Ordinal);
    }

    /// <summary>
    /// #793 QA · …AND THE SPOT IT WALKS THEM TO IS ON THE GRAVEL, IN FRONT OF THE BENCH, WITHIN REACH.
    ///
    /// <para>The owner's own method as a test (<i>"open the scene and check the parts are in the right
    /// place"</i>), and §13.15's second cause as an assertion: this project has set a captain down inside a
    /// wall twice, both times because a caller did geometry about a room it did not own. So the standoff the
    /// dev row computes is measured against the REAL collision field of the REAL floor, for every park the
    /// generator lays: inside the room, out of the furniture, inside <see cref="DeckPlan.InteractRadius"/>
    /// of the bench it walked to — and <b>on the walk side of the plank</b>.</para>
    ///
    /// <para>That last clause is the one that catches the mistake. <b>Proven RED</b> by the standoff this row
    /// was first written with — one and a bit du <i>below</i> the bench, which is a guess about which way a
    /// bench faces on a walk that bends three times down a 278 du room:</para>
    /// <code>
    /// 2 park(s) walk the dev row somewhere a tester should not be:
    ///   ganymede B1 bench 1: the captain is set down BEHIND the bench — 6.8 du from the walk, where the
    ///     bench end is 5.1.
    ///   titan B1 bench 1: the captain is set down BEHIND the bench — 6.8 du from the walk, where the bench
    ///     end is 5.1.
    /// </code>
    ///
    /// <para>TWO of the ten, which is exactly why the typed offset is the dangerous kind of wrong: it is
    /// right on eight parks and quietly wrong on the two whose first free bench happens to sit on the other
    /// side of its bend, and nobody would have found those two except by booting them.</para>
    ///
    /// <para>Neither the collision check nor the reach check fires on that version — the ground behind a
    /// bench is perfectly solid ground to stand on and the console is still in range. It is simply the wrong
    /// side, with the seat between the tester and the room, and only a law about the WALK can say so.</para>
    /// </summary>
    [Fact]
    public void THE_DEMO_ROW_LandsOnGravelInFrontOfTheBench()
    {
        var wrong = new List<string>();
        int landed = 0;

        foreach ((string body, int level, UndergroundComplex.Park park) in EveryPark())
        {
            DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], 0);

            foreach (ParkBenches.Bench bench in ParkBenches.On(in park))
            {
                if (bench.Taken)
                {
                    continue;
                }

                // The very arithmetic Map.Bench.TowardTheWalk spends — the nearest published walk sample
                // gives the bearing, the standoff gives the distance.
                (double ex, double ey) = bench.YourEnd;
                (double wx, double wy) = NearestWalkSample(in park, ex, ey);
                double dx = wx - ex, dy = wy - ey;
                double len = Math.Sqrt((dx * dx) + (dy * dy));
                Assert.True(len > 1e-6, $"{body} B{-level}: the park publishes no walk to stand on.");

                double standoff = DeckPlan.AvatarRadius + 1.0;
                double sx = ex + (dx / len * standoff), sy = ey + (dy / len * standoff);
                landed++;

                if (!park.Contains(sx, sy))
                {
                    wrong.Add($"  {body} B{-level} bench {bench.Index}: the captain is set down at "
                        + $"({sx:F1}, {sy:F1}), which is outside the park.");
                }
                if (SurfaceCollision.Blocked(sx, sy, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    wrong.Add($"  {body} B{-level} bench {bench.Index}: the captain is set down at "
                        + $"({sx:F1}, {sy:F1}), which is inside something solid.");
                }

                double reach = Math.Sqrt(((sx - bench.X) * (sx - bench.X))
                                       + ((sy - bench.Y) * (sy - bench.Y)));
                if (reach > DeckPlan.InteractRadius)
                {
                    wrong.Add($"  {body} B{-level} bench {bench.Index}: the captain is set down {reach:F1} du "
                        + $"from the bench — out of [E] reach ({DeckPlan.InteractRadius:F1} du).");
                }

                // …and ON THE GRAVEL, which is the half a typed offset gets wrong. A bench stands on the
                // OUTSIDE of its bend, so "a bit below the plank" is the walk side for some of them and the
                // back of the seat for the rest — and a tester dropped behind a bench is standing in the
                // planting with the room on the far side of the thing they came to sit on.
                double fromSpot = Math.Sqrt(((sx - wx) * (sx - wx)) + ((sy - wy) * (sy - wy)));
                if (fromSpot >= len)
                {
                    wrong.Add($"  {body} B{-level} bench {bench.Index}: the captain is set down BEHIND the "
                        + $"bench — {fromSpot:F1} du from the walk, where the bench end is {len:F1}.");
                }

                break;   // the row takes the FIRST free bench, and so does this.
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) walk the dev row somewhere a tester should not be:\n"
            + string.Join("\n", wrong.Take(8)));
        Assert.True(landed >= 8, $"only {landed} parks were walked — this proved little.");
    }

    private static (double X, double Y) NearestWalkSample(
        in UndergroundComplex.Park park, double fromX, double fromY)
    {
        double bx = fromX, by = fromY, best = double.MaxValue;
        foreach ((double wx, double wy) in park.Walk ?? [])
        {
            double d = ((wx - fromX) * (wx - fromX)) + ((wy - fromY) * (wy - fromY));
            if (d < best)
            {
                (best, bx, by) = (d, wx, wy);
            }
        }
        return (bx, by);
    }
}
