using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #792 · THE SEATS ARE SEEN — free chairs, taken chairs, and who is alone versus already talking, read off
/// the deck the way a hungry traveller reads a canteen.
///
/// <para>Owner, playtest 2026-08-08: <i>"people looking to sit down look at those like hungry wild beasts
/// look at their prey"</i> · <i>"Now I have trouble finding a free table… lol story of my travelling life
/// right here."</i></para>
///
/// <h3>What was wrong, and why nothing caught it</h3>
///
/// <para>Core has known all three answers for weeks. A top has carried its seat count and its occupancy
/// since #746; the counter's eight stools have known who is on them, watch by watch, since #756; and the
/// deck drew a top as one bare grey ring and a stool as nothing whatsoever. Not one guard could see it,
/// because a fact that is never drawn is not wrong — it is absent, and absence is the one kind of defect a
/// player cannot report except by saying they cannot find a seat.</para>
///
/// <para>So every guard below is measured ON THE MARKS THE PEN ACTUALLY LAID, over all six watches, and
/// insists both states are in front of the captain at once — a sweep that only ever met full rooms would
/// pass a renderer that drew every seat as taken.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class SeatsAreDrawnTests
{
    private const int WidthPx = 1200, HeightPx = 700;
    private const string Body = "luna";
    private const int Level = -1;

    /// <summary>Six watches is a day (<c>CanteenRegulars.WatchFill</c>) and the room's whole mood is which
    /// one you walked into, so nothing here is measured on a single shift.</summary>
    private const int Watches = 6;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

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

    /// <summary>The three inks, transcribed from <c>DeckView</c> deliberately — a test that asked the
    /// renderer for its own constants could not notice one turning into another (<c>#784</c>'s own note).
    /// </summary>
    private static readonly RgbaColor SeatEmpty = new(126, 142, 164, 210);
    private static readonly RgbaColor SeatOpen = new(120, 220, 200, 235);
    private static readonly RgbaColor SeatTaken = new(228, 196, 150, 240);

    private static bool Is(RgbaColor a, RgbaColor b) => a.R == b.R && a.G == b.G && a.B == b.B;

    /// <summary>One frame of the real B1 hall on one watch, drawn by the real <see cref="DeckView"/>.</summary>
    private static (List<Mark> Marks, DeckPlan Plan, DeckView.Placement Place) Frame(long watch)
    {
        DeckPlan plan = HiveInterior.FloorDeck(Body, Level, Field, 0, (_, _) => { }, [], watch);
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

    private static UndergroundComplex.Amenity Cantina()
    {
        foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(Body, Level, Field).Amenities)
        {
            if (a.Use == UndergroundComplex.Comfort.UpperCanteen && a.Hall is not null)
            {
                return a;
            }
        }
        throw new InvalidOperationException($"{Body} B{-Level} has no cantina hall to measure.");
    }

    // ── (a) FREE vs TAKEN, ON EVERY STOOL ─────────────────────────────────────────────────────────────

    /// <summary>
    /// #792 · EVERY STOOL IS DRAWN, AND WHAT IS DRAWN IS WHAT THE SIM SAYS — BOTH WAYS.
    ///
    /// <para>Owner: <i>"a graphical indicator on every sittable place (stools AND table chairs)."</i> The
    /// row is walked seat by seat on all six watches and each mark is checked against
    /// <see cref="TheStools.Taken"/>: a taken stool must never draw free, and a free one must never draw
    /// taken. Both halves, because a renderer that filled every seat would pass the first on its own.</para>
    ///
    /// <para><b>Proven RED</b> by the plausible wiring — hand the row the FIRST watch's occupancy however
    /// the floor was built, which is what a caller who forgot the frozen watch writes:</para>
    /// <code>
    /// 22 seat(s) are drawn as something other than what the sim says:
    ///   w1 stool 2: the sim says TAKEN and the deck drew it free.
    ///   w1 stool 4: the sim says TAKEN and the deck drew it free.
    ///   w2 stool 1: the sim says TAKEN and the deck drew it free.
    ///   w3 stool 5: the sim says free and the deck drew it TAKEN.
    ///   w4 stool 8: the sim says free and the deck drew it TAKEN.
    ///   w5 stool 7: the sim says TAKEN and the deck drew it free.
    ///   …22 in all, on five of the six watches, in BOTH directions.
    /// </code>
    /// </summary>
    [Fact]
    public void EVERY_STOOL_DrawsWhatTheSimSaysIsOnIt()
    {
        UndergroundComplex.Hall hall = Cantina().Hall!.Value;
        Assert.Equal(TheStools.Count, hall.StoolRow.Count);

        var wrong = new List<string>();
        int drawnTaken = 0, drawnFree = 0, watchesWithBoth = 0;

        for (long watch = 0; watch < Watches; watch++)
        {
            (List<Mark> marks, DeckPlan plan, DeckView.Placement place) = Frame(watch);
            Assert.Equal(TheStools.Count, plan.Stools.Length);

            int taken = 0, free = 0;
            for (int s = 0; s < hall.StoolRow.Count; s++)
            {
                bool sim = TheStools.Taken(Body, Level, s, watch);
                (float X, float Y) at = At(place, hall.StoolRow[s].X, hall.StoolRow[s].Y);

                // Every circle the pen laid on this seat — one, and it says which it is by being filled or
                // hollow. A seat drawn twice would be two answers as surely as a seat drawn wrong.
                List<Mark> here = marks
                    .Where(m => m.Kind is "disc" or "ring" && Near(m, at, 0.5f * place.Scale))
                    .ToList();

                if (here.Count != 1)
                {
                    wrong.Add($"  w{watch} stool {s + 1}: {here.Count} mark(s) on the seat, expected one.");
                    continue;
                }

                bool drawn = here[0].Kind == "disc" && Is(here[0].Ink, SeatTaken);
                bool empty = here[0].Kind == "ring" && !Is(here[0].Ink, SeatTaken);

                if (drawn == sim && empty != sim)
                {
                    if (sim)
                    {
                        taken++;
                    }
                    else
                    {
                        free++;
                    }
                    continue;
                }

                wrong.Add(sim
                    ? $"  w{watch} stool {s + 1}: the sim says TAKEN and the deck drew it free."
                    : $"  w{watch} stool {s + 1}: the sim says free and the deck drew it TAKEN.");
            }

            drawnTaken += taken;
            drawnFree += free;
            if (taken > 0 && free > 0)
            {
                watchesWithBoth++;
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} seat(s) are drawn as something other than what the sim says:\n" +
            string.Join("\n", wrong));

        // ANTI-VACUITY: the sweep has to have MET both states, and met them side by side — a row that was
        // all one thing on every watch would let a renderer that ignores occupancy through untouched.
        Assert.True(drawnTaken > 0, "not one stool anywhere in the sweep was drawn as taken.");
        Assert.True(drawnFree > 0, "not one stool anywhere in the sweep was drawn as free.");
        Assert.Equal(Watches, watchesWithBoth);
    }

    // ── (b) THE OPEN SEAT AT A PARTLY FILLED TABLE ────────────────────────────────────────────────────

    /// <summary>
    /// #792 · A TOP'S CHAIRS ARE DRAWN, AND THE FREE CHAIR AT AN OCCUPIED TABLE IS A DIFFERENT MARK.
    ///
    /// <para>Owner: <i>"a table with three people and one chair is an invitation of a specific kind; the
    /// deck should show the open seat, not just the occupied dots."</i> So three counts are measured across
    /// the whole frame, against the room Core says is there:</para>
    ///
    /// <list type="bullet">
    /// <item>one BODY per occupied top, and never one at a top nobody is at;</item>
    /// <item>the chairs at an occupied top drawn in the INVITATION ink, one per chair that is left;</item>
    /// <item>the chairs at an empty top drawn in the furniture ink, one per seat the top has.</item>
    /// </list>
    ///
    /// <para><b>Proven RED</b> by drawing every free chair the same, which is the version that answers the
    /// first glance and not the second:</para>
    /// <code>
    /// Failed …THE_OPEN_SEAT_AtAPartlyFilledTopIsDrawnAsAnInvitation [112 ms]
    ///   w0: 24 chair(s) should read as an invitation (a free seat at a top somebody is already at)
    ///   and 0 were drawn in that ink.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_OPEN_SEAT_AtAPartlyFilledTopIsDrawnAsAnInvitation()
    {
        UndergroundComplex.Amenity cantina = Cantina();
        int metOccupied = 0, metFree = 0, watchesWithBoth = 0;

        for (long watch = 0; watch < Watches; watch++)
        {
            (List<Mark> marks, _, DeckView.Placement place) = Frame(watch);

            int bodies = 0, invites = 0, plain = 0, occupied = 0, empty = 0;
            foreach (CanteenRegulars.TableSeat top in
                     CanteenRegulars.Tables(Body, Level, cantina, watch))
            {
                if (top.Taken)
                {
                    occupied++;
                    bodies += top.Seats > 0 ? 1 : 0;
                    invites += Math.Max(0, top.Seats - 1);
                }
                else
                {
                    empty++;
                    plain += top.Seats;
                }
            }

            int drawnBodies = marks.Count(m =>
                m.Kind == "disc" && Is(m.Ink, SeatTaken)
                && Math.Abs(m.Points[2] - (0.30f * place.Scale)) < 0.02f * place.Scale);
            int drawnInvites = marks.Count(m => m.Kind == "polyline" && Is(m.Ink, SeatOpen));
            int drawnPlain = marks.Count(m => m.Kind == "polyline" && Is(m.Ink, SeatEmpty));

            Assert.True(drawnBodies == bodies,
                $"w{watch}: {occupied} top(s) have somebody at them and {drawnBodies} bodies were drawn " +
                $"(expected {bodies}).");
            Assert.True(drawnInvites == invites,
                $"w{watch}: {invites} chair(s) should read as an invitation (a free seat at a top somebody " +
                $"is already at) and {drawnInvites} were drawn in that ink.");
            Assert.True(drawnPlain == plain,
                $"w{watch}: {empty} empty top(s) between them have {plain} chairs and {drawnPlain} were " +
                "drawn as furniture.");

            metOccupied += occupied;
            metFree += empty;
            if (occupied > 0 && empty > 0)
            {
                watchesWithBoth++;
            }
        }

        // ANTI-VACUITY, both directions: a hall that heaved on every watch, or one that was empty on every
        // watch, would never once have put the two marks beside each other for a captain to tell apart.
        Assert.True(metOccupied > 0, "not one top in the sweep had anybody at it.");
        Assert.True(metFree > 0, "not one top in the sweep was free.");
        Assert.Equal(Watches, watchesWithBoth);
    }

    /// <summary>
    /// #782/#792 · …AND NO TABLE'S CHAIRS REACH THE NEXT TABLE'S.
    ///
    /// <para>The deck is read at phone size and this hall is the densest room in the game, so a chair ring
    /// wide enough to be legible is also a chair ring wide enough to collide with the top beside it — and
    /// two tops whose chairs interleave answer the free-seat question with a mess. Measured against the
    /// room the generator actually laid rather than against the pitch it was aiming for.</para>
    /// </summary>
    [Fact]
    public void THE_CHAIRS_OfOneTopNeverReachTheNext()
    {
        // The pen's own ring radius (DeckView.SeatRingDu), transcribed rather than asked for.
        const double ring = 1.55;

        var tops = CanteenRegulars.Tables(Body, Level, Cantina()).ToList();
        Assert.True(tops.Count > 8, $"only {tops.Count} top(s) — this is not the hall.");

        double closest = double.MaxValue;
        for (int i = 0; i < tops.Count; i++)
        {
            for (int j = i + 1; j < tops.Count; j++)
            {
                double dx = tops[i].X - tops[j].X, dy = tops[i].Y - tops[j].Y;
                closest = Math.Min(closest, Math.Sqrt((dx * dx) + (dy * dy)));
            }
        }

        Assert.True(closest > 2 * ring,
            $"the two nearest tops stand {closest:0.0} du apart and their chairs reach {2 * ring:0.0} du " +
            "across — the seats of one table are drawn inside the other.");
    }

    // ── (c) ALONE vs TALKING ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #792 · A CONVERSATION IS DRAWN AS ONE, AND A LONE SITTER IS NOT.
    ///
    /// <para>Owner: <i>"does a table hold somebody alone (approachable) or a conversation already going…
    /// it determines whether there is anything to overhear as discussion goes."</i></para>
    ///
    /// <para>Counted off the ink: the pen lays exactly one warm segment per occupied top (the chair back
    /// behind its sitter's shoulders) and exactly two more over any top that is TALKING. So the arithmetic
    /// below pins both facts at once, and is measured against
    /// <see cref="CanteenRegulars.TableSeat.Talking"/> — Core's authored answer, never a guess this file or
    /// the renderer makes about a plate's wording.</para>
    ///
    /// <para><b>Proven RED</b> by the guess the issue warns about: take TALKING to mean STRANGER, which
    /// draws a conversation over the couple who are not having one:</para>
    /// <code>
    /// Failed …A_CONVERSATION_IsDrawnAndSomebodyOnTheirOwnIsNot [113 ms]
    ///   w0: 6 top(s) hold somebody and 2 of them are conversations, so 10 warm segments were
    ///   expected and 12 were drawn.
    /// </code>
    ///
    /// <para>Twelve, because the STRANGER rule draws the overhear mark over the couple who are not
    /// talking and over the driver reading a docket on their own — which is the exact mistake the issue
    /// asked for a Core fact to prevent.</para>
    /// </summary>
    [Fact]
    public void A_CONVERSATION_IsDrawnAndSomebodyOnTheirOwnIsNot()
    {
        UndergroundComplex.Amenity cantina = Cantina();
        int metTalking = 0, metAlone = 0, watchesWithBoth = 0;

        for (long watch = 0; watch < Watches; watch++)
        {
            (List<Mark> marks, _, _) = Frame(watch);

            int occupied = 0, talking = 0, alone = 0;
            foreach (CanteenRegulars.TableSeat top in
                     CanteenRegulars.Tables(Body, Level, cantina, watch))
            {
                if (!top.Taken || top.Seats <= 0)
                {
                    continue;
                }
                occupied++;
                if (top.Talking)
                {
                    talking++;
                }
                if (top.Alone)
                {
                    alone++;
                }
            }

            // One chair back per sitter, plus two ticks over each conversation, and nothing else on this
            // deck is drawn in this ink.
            int warm = marks.Count(m => m.Kind == "polyline" && Is(m.Ink, SeatTaken));
            Assert.True(warm == occupied + (2 * talking),
                $"w{watch}: {occupied} top(s) hold somebody and {talking} of them are conversations, so " +
                $"{occupied + (2 * talking)} warm segments were expected and {warm} were drawn.");

            metTalking += talking;
            metAlone += alone;
            if (talking > 0 && alone > 0)
            {
                watchesWithBoth++;
            }
        }

        // ANTI-VACUITY: the distinction only exists if both kinds are on the floor at the same time.
        Assert.True(metTalking > 0, "not one conversation anywhere in the sweep.");
        Assert.True(metAlone > 0, "not one lone sitter anywhere in the sweep — nobody to ask to join.");
        Assert.True(watchesWithBoth >= Watches - 1,
            $"only {watchesWithBoth} of {Watches} watches put a conversation and a lone sitter in the same " +
            "room, and the whole affordance is telling them apart at a glance.");
    }

    // ── (d) THE PEN DOES NOT KNOW WHAT A STOOL IS ─────────────────────────────────────────────────────

    /// <summary>
    /// #792 · OCCUPANCY IS HANDED DOWN, EXACTLY AS #788'S POSTURE WAS.
    ///
    /// <para>#591's one-reach lesson, and the reason it is worth a source-shape guard: a renderer that
    /// works out for itself what the sim already knows is how two instruments come to disagree, and this
    /// room has TWO of them — the deck a captain reads and the [E] press that answers. So the pen is held
    /// ignorant: it has never heard of the counter, of the canteen's regulars, or of a watch. It is handed
    /// a spot and a bool, and its whole contribution is which glyph that is.</para>
    ///
    /// <para><b>Proven RED</b> by letting <c>DeckView</c> ask the row itself:</para>
    /// <code>
    /// Assert.DoesNotContain() Failure: Sub-string found
    ///                              ↓ (pos 60334)
    /// String: ···"NLY: the pen asking TheStools for itself."···
    /// Found:  "TheStools"
    /// </code>
    /// </summary>
    [Fact]
    public void THE_PEN_IsHandedTheOccupancyAndNeverWorksItOut()
    {
        string deckView = Source("Rendering", "DeckView.cs");

        // The pen must not know what a stool, a top or a shift IS.
        foreach (string forbidden in new[]
                 { "TheStools", "CanteenRegulars", "SittingAlone", "CanteenTable", "canteenWatch" })
        {
            Assert.DoesNotContain(forbidden, deckView, StringComparison.Ordinal);
        }

        // …it is handed them, on the plan, and reads the flags it was given.
        Assert.Contains("foreach (DeckPlan.StoolSpot stool in plan.Stools)", deckView, StringComparison.Ordinal);
        Assert.Contains("if (stool.Taken)", deckView, StringComparison.Ordinal);
        Assert.Contains("if (top.Talking)", deckView, StringComparison.Ordinal);
        Assert.Contains("bool invites = top.Occupied;", deckView, StringComparison.Ordinal);

        // …and the room that owns the answers is the one that fills them in — off the SAME frozen watch the
        // [E] press asks, which is #709's law and the reason a drawn room and a pressed room are one room.
        string hive = Source("Rendering", "HiveInterior.cs");
        Assert.Contains("TheStools.Taken(bodyId, level, s, canteenWatch)", hive, StringComparison.Ordinal);
        Assert.Contains("CanteenRegulars.Tables(bodyId, level, a, canteenWatch)", hive, StringComparison.Ordinal);
        Assert.Contains("top.Seats, top.Taken, top.Talking", hive, StringComparison.Ordinal);
        Assert.Contains("counter.StoolRow", hive, StringComparison.Ordinal);

        // …and Core owns WHERE the seats are, because a renderer measuring a bar it did not carve is how
        // this project has twice set a captain down inside a wall.
        Assert.DoesNotContain("HallCounterBandDu", hive, StringComparison.Ordinal);
        Assert.DoesNotContain("HallStoolStandoffDu", hive, StringComparison.Ordinal);
    }

    /// <summary>#792 · …and the tester is told what the new glyphs mean, on the rows that already open this
    /// room. A drawing convention nobody documents is a drawing convention the next tester reports as a
    /// bug.</summary>
    [Fact]
    public void THE_GUIDE_SaysHowToReadTheNewGlyphs()
    {
        string guide = Doc("testing-guide.md");
        foreach (string phrase in new[] { "#792", "invitation", "overhear" })
        {
            Assert.Contains(phrase, guide, StringComparison.OrdinalIgnoreCase);
        }
    }
}
