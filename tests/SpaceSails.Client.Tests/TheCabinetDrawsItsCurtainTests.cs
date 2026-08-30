using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #758 · THE CURTAIN AND THE DOOR, ON THE FLOOR AND ON THE PLAN.
///
/// <para>Owner, 2026-08-06: <i>"the cabinets could have some kind of privacy curtain effect ... something
/// that prevents the sound or sight catching what happens there too easily but is not a real door."</i></para>
///
/// <para>Core's own guards (<c>TheCurtainAndTheDoorTests</c>) measure the leak law against what the seed
/// produces. THESE are the two claims Core cannot make on its own: that the stage a captain chose is the
/// stage the PEN DRAWS, and that the counter's who-was-inside line is written exactly once — asked of a real
/// <see cref="Pages.Map"/> on a real generated floor, with the seat taken through the shipping verb, on the
/// bench <c>MustStandUpBeforeWalkingTests</c> established and <c>CoSeatingIsAStripTests</c> extended.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCabinetDrawsItsCurtainTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";
    private const int WidthPx = 1200;
    private const int HeightPx = 700;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor with a hall in it.");

    // ── THE PEN ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every mark the pen was asked for, in order. Only the text matters here — a plate is a
    /// string at a coordinate, and what this guard is about is WHICH string.</summary>
    private sealed record Mark(string Kind, string Text);

    /// <summary>The transcribing pen, in this file's own copy of the idiom every drawn-thing guard in the
    /// suite uses (<c>SeatsAreDrawnTests</c>, <c>TheHallsAreDrawnInAThirdIdiomTests</c>). It records rather
    /// than renders, so an assertion can be about a DRAW CALL and not about a field somebody hoped was
    /// drawn.</summary>
    private sealed class RecordingPen : IRenderer
    {
        public List<Mark> Marks { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Marks.Clear();
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) { }
        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left) => Marks.Add(new Mark("text", text ?? ""));
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) { }
    }

    /// <summary>Every string the pen was handed for a floor drawn with this set of leaves dogged.</summary>
    private static List<string> WhatThePenSaw(IReadOnlyCollection<string>? dogged)
    {
        DeckPlan plan = HiveInterior.FloorDeck(
            Body, TheFloor, Field, 0, static (_, _) => { }, [], 0, null, null, dogged);

        var pen = new RecordingPen();
        (double ax, double ay) = HiveInterior.SpawnOn(Field);
        var state = new DeckView.State(ax, ay, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false);
        new DeckView(pen).Draw(plan, WidthPx, HeightPx, 0, in state, 0, 0, null);
        return [.. pen.Marks.Where(m => m.Kind == "text").Select(m => m.Text)];
    }

    /// <summary>The cabinets this floor actually has, as Core carved them.</summary>
    private static IReadOnlyList<UndergroundComplex.Cabinet> TheCabinets()
    {
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(Body, TheFloor, Field);
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            if (a.Hall is { } hall && hall.Cabinets.Count > 0)
            {
                return hall.Cabinets;
            }
        }
        Assert.Fail(
            $"{Body} B{-TheFloor} carves no cabinets at all — every guard in this file would be asserting " +
            "about a room the generator does not build.");
        return [];
    }

    // ── (d) THE GLYPH IS PUBLISHED BY CORE AND DRAWN BY THE PEN ───────────────────────────────────────

    /// <summary>
    /// #758 · WHAT THE CAPTAIN CAN SEE FROM ACROSS THE HALL.
    ///
    /// <para>Three claims, and the third is the one that matters: the plan's plate carries Core's glyph, the
    /// two stages are drawn as DIFFERENT strings, and a leaf that was dogged is drawn dogged while its two
    /// neighbours are still drawn as cloth. A picture that showed one state for the whole row would be the
    /// deck agreeing with itself and disagreeing with the room.</para>
    ///
    /// <para>Driven through the REAL <see cref="DeckView"/> onto a recording pen, so this is a draw call and
    /// not a field somebody hoped was drawn.</para>
    ///
    /// <para><b>The RED case.</b> Publish <c>cabinet.Plate</c> instead of
    /// <c>CabinetPrivacy.PlateFor(cabinet.Plate, stage)</c> in <c>HiveInterior</c>: the first assertion
    /// fires, with no glyph on any plate in the room.</para>
    /// </summary>
    [Fact]
    public void EVERY_CABINET_DrawsTheGlyphForTheStageItIsStandingAt()
    {
        IReadOnlyList<UndergroundComplex.Cabinet> cabinets = TheCabinets();
        Assert.True(cabinets.Count >= 2,
            "a row of one cabinet cannot show a captain that ONE of them is shut, which is what the glyph " +
            "is for.");

        // As the building keeps them: every leaf open, every way behind cloth.
        List<string> asBuilt = WhatThePenSaw(null);
        foreach (UndergroundComplex.Cabinet cabinet in cabinets)
        {
            string want = CabinetPrivacy.PlateFor(cabinet.Plate, CabinetPrivacy.Stage.Curtain);
            Assert.True(asBuilt.Contains(want, StringComparer.Ordinal),
                $"the pen never drew \"{want}\". A cabinet the plan does not mark is a state the captain " +
                "cannot read off the room, and the whole of this feature's UI is that one mark. Plates it " +
                "did draw: " + string.Join(" | ",
                    asBuilt.Where(t => t.Contains("CABINET", StringComparison.Ordinal))));
        }

        // …and now ONE of them is dogged, from inside, and the row stops reading the same.
        UndergroundComplex.Cabinet shut = cabinets[0];
        List<string> withOneShut = WhatThePenSaw([CabinetPrivacy.Key(TheFloor, shut.Number)]);

        Assert.True(
            withOneShut.Contains(
                CabinetPrivacy.PlateFor(shut.Plate, CabinetPrivacy.Stage.Door), StringComparer.Ordinal),
            $"cabinet {shut.Number} was dogged and the plan still drew it as cloth — the picture and the " +
            "sim disagreeing about a leaf the captain is sitting behind.");

        foreach (UndergroundComplex.Cabinet other in cabinets.Skip(1))
        {
            Assert.True(
                withOneShut.Contains(
                    CabinetPrivacy.PlateFor(other.Plate, CabinetPrivacy.Stage.Curtain), StringComparer.Ordinal),
                $"dogging cabinet {shut.Number} also drew cabinet {other.Number} shut. The state is per " +
                "LEAF, and a set keyed loosely enough to shut the whole row is the oldest fault on this " +
                "ground.");
        }

        // The two stages are genuinely different strings on the plan. A glyph that read the same either way
        // would pass every assertion above and tell the player nothing (the fifth bug class).
        Assert.NotEqual(
            CabinetPrivacy.PlateFor(shut.Plate, CabinetPrivacy.Stage.Curtain),
            CabinetPrivacy.PlateFor(shut.Plate, CabinetPrivacy.Stage.Door));
    }

    // ── (c) THE COUNTER'S LINE, WRITTEN ONCE, THROUGH THE REAL PRESS ──────────────────────────────────

    /// <summary>
    /// #758 · A CAPTAIN SITS IN A CABINET, WORKS THE LEAF, AND THE BOOK KEEPS ONE LINE.
    ///
    /// <para>The whole cycle, through the shipping verbs: sit down at a cabinet top, dog the door (one line
    /// in the field book), let it back open (no second line), dog it again (still no second line). The
    /// curtain never writes anything at all, which is the entire reason stage one exists.</para>
    ///
    /// <para><b>THE BOOK'S OWN DE-DUPLICATION IS NOT ALLOWED TO BE WHAT PASSES THIS.</b>
    /// <c>FieldNotes.Append</c> swallows a line that repeats the one immediately before it — so a naive
    /// version of this guard went GREEN on a build with no once-only law in it at all, which was found by
    /// running the RED case and watching it pass (the fifth named bug class, caught by the house rule that
    /// says prove a guard can fail). A DIFFERENT line is filed between the presses now: the book can no
    /// longer collapse anything, and the only thing left that can keep the counter's line single is the
    /// law.</para>
    ///
    /// <para><b>#917 · BOTH WATCHES, ONE FACT EACH.</b> The keep tends this bar only on the living watches,
    /// so the counter's line has two readings and the dead one names nobody. Driven on a watch of each kind
    /// — with the kind asserted off <see cref="SpaceSails.Core.Interior.TheKeep.KeptWatch"/> rather than assumed, so a
    /// re-tuned rota cannot leave this row quietly testing the same watch twice — and the OTHER reading is
    /// required to be absent from the book, which is what stops the two sentences from being one string with
    /// a spare.</para>
    ///
    /// <para><b>The RED case.</b> Replace the <c>TheCounterWritesItDown(from, to) &amp;&amp;
    /// ex.CabinetsWitnessed.Add(key)</c> condition in <c>Seating.DrawOrDogTheCabinet</c> with <c>true</c>:
    /// the witness set stays empty and the undogging files a second copy, and two assertions fire. Hand
    /// <c>WhoWasInsideNote</c> a constant watch instead of <c>ex.CanteenWatch</c> and the dead-watch row
    /// fires on the first press, with a man named behind an empty counter.</para>
    /// </summary>
    [Theory]
    [InlineData(2)]   // the hall at 0.95 — somebody is behind the counter
    [InlineData(5)]   // the hall at 0.15 — self-service, and #618's "nobody is back there" is literal
    public void THE_COUNTER_KeepsOneLine_HoweverManyTimesTheLeafIsWorked(long watch)
    {
        Pages.Map map = OnTheFloor(watch);
        int cabinet = SitDownInACabinet(map);

        Assert.Equal(watch, TheFrozenWatch(map));
        string theLine = CabinetPrivacy.WhoWasInsideNote(cabinet, watch);

        // The two readings are genuinely two, and only the one this watch calls for may ever be filed.
        string theOtherWatchesLine = CabinetPrivacy.WhoWasInsideNote(
            cabinet, SpaceSails.Core.Interior.TheKeep.KeptWatch(watch) ? ADeadWatch() : ALivingWatch());
        Assert.NotEqual(theLine, theOtherWatchesLine);

        Assert.Equal(CabinetPrivacy.Stage.Curtain, StageNow(map));
        Assert.Equal(0, TimesFiled(map, theLine));
        Assert.Empty(TheWitnessSet(map));

        // ONE: the leaf comes out of the wall, and the hall watches it.
        Invoke(map, "WorkTheCabinetLeaf");
        Assert.Equal(CabinetPrivacy.Stage.Door, StageNow(map));
        Assert.True(TimesFiled(map, theLine) == 1,
            $"dogging the door filed the counter's line {TimesFiled(map, theLine)} times. It is the price " +
            "of stage two and the price is one line.");
        Assert.Single(TheWitnessSet(map));

        // …and something else goes in the book, so the log's own "same line twice running" collapse cannot
        // be what keeps the count at one below. Without this, the guard passes on a build that files the
        // counter's line on every press — which is what it did the first time it was run RED.
        Invoke(map, "FileNote", "Somebody else's afternoon, filed in between.", CanteenRegulars.Glyph);

        // TWO: back to cloth. A book does not un-write, and it does not write the undoing down either.
        Invoke(map, "WorkTheCabinetLeaf");
        Assert.Equal(CabinetPrivacy.Stage.Curtain, StageNow(map));
        Assert.True(TimesFiled(map, theLine) == 1,
            $"letting the leaf back open wrote line number {TimesFiled(map, theLine)} — the curtain is not " +
            "a thing the counter can see, and that is the whole of what it is for.");
        Assert.Single(TheWitnessSet(map));

        Invoke(map, "FileNote", "And another, so nothing here can collapse.", CanteenRegulars.Glyph);

        // THREE: shut again. Still one. The captain did one memorable thing in this room tonight.
        Invoke(map, "WorkTheCabinetLeaf");
        Assert.Equal(CabinetPrivacy.Stage.Door, StageNow(map));
        Assert.True(TimesFiled(map, theLine) == 1,
            $"the same cabinet was written down {TimesFiled(map, theLine)} times in one sitting.");
        Assert.Single(TheWitnessSet(map));

        // …and the reading belonging to the OTHER kind of watch never reached the book. A keep who was
        // described looking up while the counter stood empty is the fault #917 sent this back for.
        Assert.True(TimesFiled(map, theOtherWatchesLine) == 0,
            "the book carries the other watch's sentence about the counter — on watch "
            + watch.ToString(CultureInfo.InvariantCulture) + " the keep "
            + (SpaceSails.Core.Interior.TheKeep.KeptWatch(watch) ? "IS" : "is NOT") + " behind it.");
    }

    /// <summary>A watch the keep works, and one he does not — read off the hall's own numbers rather than
    /// typed here, so the rows above cannot both end up on the same kind of shift.</summary>
    private static long ALivingWatch() => EveryWatch().First(SpaceSails.Core.Interior.TheKeep.KeptWatch);

    private static long ADeadWatch() => EveryWatch().First(w => !SpaceSails.Core.Interior.TheKeep.KeptWatch(w));

    private static IEnumerable<long> EveryWatch() =>
        Enumerable.Range(0, CanteenRegulars.WatchFill.Count).Select(i => (long)i);

    /// <summary>The watch the deck was actually built on — asked of the excursion, never of the constant the
    /// bench handed in, so a cheat that failed to take shows up as a red row instead of a green one.</summary>
    private static long TheFrozenWatch(Pages.Map map)
    {
        object ex = Get(map, "_surface")!;
        return (long)ex.GetType().GetProperty("CanteenWatch", Hidden)!.GetValue(ex)!;
    }

    /// <summary>
    /// #758 · …AND THE STRIP SAYS WHICH ROOM THE CAPTAIN IS IN, on the frame the leaf moves.
    ///
    /// <para>The clause is read off the same set the plan is drawn from, so it cannot lag a press. This is
    /// also the guard on the fault this feature would otherwise have shipped: the strip said <i>the door is
    /// shut</i> about every cabinet in the building, and after #758 most cabinets stand open.</para>
    ///
    /// <para><b>The RED case.</b> Take the stage back off <c>SeatedHud.CustomerLine</c> and the first
    /// assertion fires with a shut door reported through a curtain.</para>
    /// </summary>
    [Fact]
    public void THE_STRIP_SaysCurtainOrDoor_AndChangesOnThePress()
    {
        Pages.Map map = OnTheFloor();
        SitDownInACabinet(map);

        string behindCloth = (string)Invoke(map, "SeatedCustomerLine")!;
        Assert.Contains(SeatedHud.CabinetCurtainClause, behindCloth, StringComparison.Ordinal);
        Assert.DoesNotContain(SeatedHud.CabinetDoggedClause, behindCloth, StringComparison.Ordinal);

        Invoke(map, "WorkTheCabinetLeaf");

        string behindTheLeaf = (string)Invoke(map, "SeatedCustomerLine")!;
        Assert.Contains(SeatedHud.CabinetDoggedClause, behindTheLeaf, StringComparison.Ordinal);
        Assert.NotEqual(behindCloth, behindTheLeaf);

        // …and the BUTTON always offers the other stage, so one press is never a no-op the player has to
        // discover by pressing it.
        Assert.Equal(CabinetPrivacy.DrawTheCurtainLabel, (string)Get(map, "CabinetLeafLabel")!);
        Invoke(map, "WorkTheCabinetLeaf");
        Assert.Equal(CabinetPrivacy.DogTheDoorLabel, (string)Get(map, "CabinetLeafLabel")!);
    }

    /// <summary>#758 · There is no leaf to work at a hall table, and the strip must not draw a button for
    /// one. The room a captain is in decides which verbs exist — a "dog the door" on a top in the middle of
    /// a canteen would be a press with nothing to answer, which is exactly the absence #757 was filed
    /// on.</summary>
    [Fact]
    public void A_HALL_TABLE_HasNoLeafToWork()
    {
        Pages.Map map = OnTheFloor();
        Assert.False((bool)Get(map, "ACabinetLeafToWork")!,
            "the strip offers a cabinet's button to a captain who is not sitting down at all.");

        SitDownAtAHallTop(map);
        Assert.False((bool)Get(map, "ACabinetLeafToWork")!,
            "a top in the middle of the canteen offered to dog a door it does not have.");
        Assert.Equal(CabinetPrivacy.Stage.Curtain, StageNow(map));

        // …and pressing anyway changes nothing. A verb that is not on offer must also be inert.
        Invoke(map, "WorkTheCabinetLeaf");
        object ex = Get(map, "_surface")!;
        var dogged = (HashSet<string>)ex.GetType()
            .GetProperty("CabinetsDogged", Hidden)!.GetValue(ex)!;
        Assert.True(dogged.Count == 0,
            "a hall table dogged something. Whatever it shut, it is not a room the captain is in.");
    }

    // ── THE LEAK, PRODUCED BY PLAYING ────────────────────────────────────────────────────────────────

    /// <summary>
    /// #758 · A DIG BEHIND THE CURTAIN IS THE BEAT THAT LEAKS — sat, spread, dug, overheard, with no state
    /// planted anywhere.
    ///
    /// <para><b>Why this guard exists at all.</b> The leak roll shipped on <c>TableShow</c> — putting a paper
    /// on the table — and was <b>unreachable in a cabinet</b>: the SHOW move lives only on the named cast's
    /// scenes, a cabinet top has no plate so the sit hands it <c>SittingAlone.TheTable</c> (wait and stand),
    /// and nobody is ever brought to a quiet top. The whole of stage one was dead code and every guard on it
    /// was GREEN, because Core called <see cref="CabinetPrivacy.Leaks"/> directly and the one client guard
    /// planted <c>CabinetLeaked</c> by reflection. That is this repository's fifth named bug class exactly —
    /// a guard that could not tell pass from fail — and it was found by review rather than by any test here.
    /// So this one touches nothing: <b>every step is a verb a captain presses</b>, and the only thing read
    /// back is what a captain would hear.</para>
    ///
    /// <para><b>And it is not a test of the die.</b> The tuple is chosen by ASKING THE LAW which
    /// (cabinet, watch) leaks on a sitting's first dig, and the answer is asserted before anything is
    /// driven — so a build whose wiring never reaches <see cref="CabinetPrivacy.Leaks"/> fails on the
    /// consequence rather than passing on a lucky seed.</para>
    ///
    /// <para><b>The RED case.</b> Put the roll back where it shipped — move
    /// <c>ASensitiveBeatBehindTheCurtain()</c> out of <c>Map.TheWriteUpLands</c> and back into
    /// <c>Seating.TableShow</c> — and this fails at the last assertion with the patron's ordinary bark,
    /// which is the bug the ultrareview found, stated as a red line.</para>
    /// </summary>
    [Fact]
    public void A_DIG_IN_A_CABINET_LeaksThroughTheWeave_DrivenByTheShippingVerbs()
    {
        // ── 1 · ASK THE LAW for a watch on which this floor's cabinet leaks on the first dig of a sitting.
        Pages.Map? map = null;
        long leakyWatch = -1;
        int cabinet = 0;

        for (long candidateWatch = 0; candidateWatch < 48 && map is null; candidateWatch++)
        {
            Pages.Map candidate = OnTheFloor(candidateWatch);
            int sat = SitDownInACabinet(candidate);
            if (CabinetPrivacy.Leaks(Body, sat, candidateWatch, 0, CabinetPrivacy.Stage.Curtain))
            {
                map = candidate;
                leakyWatch = candidateWatch;
                cabinet = sat;
            }
        }

        Assert.True(map is not null,
            "no watch in four days leaks on the first dig of a sitting in this floor's cabinet — either the "
            + "law has been flattened to nothing or the sit no longer lands in a cabinet, and this guard "
            + "would be asserting about a beat the game cannot have.");
        Assert.True(CabinetPrivacy.Leaks(Body, cabinet, leakyWatch, 0, CabinetPrivacy.Stage.Curtain),
            "the tuple this guard drives is not one the law says leaks — it would be measuring the die.");
        Assert.Equal(CabinetPrivacy.Stage.Curtain, StageNow(map!));

        // ── 2 · A SHEET IN THE SLEEVE, THE SPREAD OPEN, AND THE DIG. Every one of these is the press a
        //       captain makes: [I] opens the spread, the row digs, and the surface tick runs the hold out.
        var paper = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:a");
        Set(map!, "_satchel", new List<Satchel.Item> { paper });
        Set(map!, "_processCheatSeconds", (double?)0.05);

        Invoke(map!, "OpenTheSpread");
        Invoke(map!, "WriteItUp", paper);
        for (int frame = 0; frame < 40 && TheHold(map!) is not null; frame++)
        {
            Invoke(map!, "StepSurface", 0.05);
        }

        // The dig really finished — otherwise the beat never fired for a reason that has nothing to do with
        // cabinets, and the assertion below would be red for the wrong reason.
        Assert.True(TheHold(map!) is null, "the dig never ran out; nothing reached the far end.");
        Assert.True(TheWrittenUpRegister(map!).Count == 1,
            "the write-up did not land, so no sensitive beat happened and this guard is about nothing.");

        // ── 3 · AND IT COMES BACK, LATER, OUT OF SOMEBODY WHO CANNOT KNOW IT. Stand up, let the shift turn,
        //       and sit down with one of the crowd. Nothing about the leak is read directly — what is
        //       asserted is the sentence a captain would hear.
        Invoke(map!, "CloseTable");

        long later = leakyWatch + 6 - (leakyWatch % 6) + 2;   // a heaving watch, and a later one
        Set(map!, "_watchCheat", (long?)later);
        Invoke(map!, "RebuildSurfaceDeck");

        Assert.True(JoinAStranger(map!),
            $"nobody in the crowd on watch {later.ToString(CultureInfo.InvariantCulture)} would take a "
            + "captain at their table, so there was no mouth for the leak to come out of.");
        Assert.Equal(CabinetPrivacy.BarkThatKnows(cabinet), BarkOfTheSeat(map!));
    }

    /// <summary>The processing hold, or null when nothing is being dug — the shipping clock, asked so the
    /// guard above can wait for the far end instead of guessing a frame count.
    ///
    /// <para>#1016 · It is the PAGE's now and no longer the excursion's. The law this helper serves is
    /// untouched (wait for the real far end, never a frame count); what moved is where the one hold lives,
    /// because a captain digging at a top in a docked bar has no excursion to hang a clock on.</para></summary>
    private static object? TheHold(Pages.Map map) => Get(map, "_processing");

    /// <summary>What the captain has actually dug out — proof the write-up landed, so a red line below is
    /// about the cabinet and not about a dig that never ran.
    ///
    /// <para>#1016 · The register is the CASE's now (one page-level set, vault-persisted) rather than the
    /// excursion's, per the owner's ruling that the table verbs are not tied to a location. Same set, same
    /// keys, one owner up.</para></summary>
    private static HashSet<string> TheWrittenUpRegister(Pages.Map map) =>
        (HashSet<string>)Get(map, "_workedUp")!;

    /// <summary>
    /// #758 · A LEAK COMES BACK AS SOMEBODY WHO KNOWS TOO MUCH, ONCE.
    ///
    /// <para>Nothing is said on the beat that leaks. It waits on the excursion, and the next stranger the
    /// captain sits down with out in the hall says the cabinet's number and then declines to go on —
    /// #715's per-entity memory as a sentence rather than as a meter, the seam
    /// <see cref="RipAndBin.SeenNote"/> opened. Then it is SPENT: it is one moment, not a tone the rest of
    /// the evening takes on.</para>
    ///
    /// <para>The unspent leak is planted directly on the excursion, which is where the roll puts it, so this
    /// row is about the DELIVERY and nothing else.</para>
    ///
    /// <para><b>It used to be the only client guard on stage one, and that was the bug.</b> A planted flag
    /// cannot notice that nothing in the game ever sets it — which is exactly what had happened, and what
    /// the ultrareview found. <c>A_DIG_IN_A_CABINET_LeaksThroughTheWeave_DrivenByTheShippingVerbs</c> above
    /// now produces a real leak by playing, and this row keeps the narrower question it was always asking.</para>
    ///
    /// <para><b>The RED case.</b> Return <c>top.Line</c> unconditionally from
    /// <c>Seating.TheBarkAtThisTop</c>: the first assertion fires with the patron's ordinary bark. Leave the
    /// flag unspent and the last one fires with a second stranger saying it too.</para>
    /// </summary>
    [Fact]
    public void A_LEAK_ComesBackAsABarkThatKnowsTooMuch_AndOnlyOnce()
    {
        Pages.Map map = OnTheFloor();
        object ex = Get(map, "_surface")!;
        System.Reflection.PropertyInfo leaked =
            ex.GetType().GetProperty("CabinetLeaked", Hidden)!;

        const int Cabinet = 2;
        leaked.SetValue(ex, Cabinet);

        Assert.True(JoinAStranger(map), "no stranger on this floor would take a captain at their table.");
        Assert.Equal(CabinetPrivacy.BarkThatKnows(Cabinet), BarkOfTheSeat(map));
        Assert.Null(leaked.GetValue(ex));

        // …and the next one is back to freight, signatures and the weather. One moment, not a mood.
        Invoke(map, "CloseTable");
        Assert.True(JoinAStranger(map), "the second stranger refused the chair.");
        Assert.NotEqual(CabinetPrivacy.BarkThatKnows(Cabinet), BarkOfTheSeat(map));
    }

    /// <summary>Sit down with a background patron — the crowd, never one of the ten named regulars, because
    /// the whole point of the bark is that it comes from somebody nobody would think to watch.</summary>
    private static bool JoinAStranger(Pages.Map map)
    {
        foreach (DeckPlan.ConsoleSpot spot in
            ThePlan(map).Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveRegular))
        {
            Set(map, "_avatarX", (double)spot.X);
            Set(map, "_avatarY", (double)spot.Y);
            Invoke(map, "TryOpenTable");
            if (Get(map, "_table") is { } seat
                && seat.GetType().GetProperty("Bark", Hidden)!.GetValue(seat) is string)
            {
                return true;
            }
            if (Get(map, "_table") is not null)
            {
                Invoke(map, "CloseTable");
            }
        }
        return false;
    }

    private static string? BarkOfTheSeat(Pages.Map map) =>
        Get(map, "_table") is { } seat
            ? (string?)seat.GetType().GetProperty("Bark", Hidden)!.GetValue(seat)
            : null;

    // ── THE FLOOR, AND THE SEATS TAKEN WITH THE GAME'S OWN VERBS ──────────────────────────────────────

    /// <summary>A live component on a real Hive floor — the bench <c>CoSeatingIsAStripTests.OnTheFloor</c>
    /// established, unchanged, because a second one would be a second answer to "what is a floor".</summary>
    private static Pages.Map OnTheFloor(long? watch = null)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on " +
                "has moved, and the seat verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, TheFloor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);

        // #917 · WHICH SHIFT THE ROOM IS DRAWN ON, through the game's own pin (`_watchCheat`) and set BEFORE
        // the rebuild, because the rebuild is what freezes the watch onto the excursion. A watch written
        // afterwards would be the room drawn on one shift and pressed on another, which is the very fault
        // #709 froze the watch to prevent.
        if (watch is { } pinned)
        {
            Set(map, "_watchCheat", (long?)pinned);
        }

        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Sit the captain down in a CABINET, through the press a captain sits down with — every free
    /// top on the floor is tried in turn and the first one that turns out to be behind a cabinet door is the
    /// case. Returns which cabinet it was.</summary>
    private static int SitDownInACabinet(Pages.Map map)
    {
        foreach (DeckPlan.ConsoleSpot spot in FreeTops(map))
        {
            Set(map, "_avatarX", (double)spot.X);
            Set(map, "_avatarY", (double)spot.Y);
            if ((bool)Invoke(map, "TryTakeTable")! && CabinetOfTheSeat(map) is > 0 and var number)
            {
                return number;
            }
            if (Get(map, "_table") is not null)
            {
                Invoke(map, "CloseTable");
            }
        }

        Assert.Fail(
            "no free top on this floor is inside a cabinet — every guard in this file would be asserting " +
            "about a seat the game cannot reach.");
        return 0;
    }

    /// <summary>…and the other case: a top on the hall floor, where there is no leaf at all.</summary>
    private static void SitDownAtAHallTop(Pages.Map map)
    {
        foreach (DeckPlan.ConsoleSpot spot in FreeTops(map))
        {
            Set(map, "_avatarX", (double)spot.X);
            Set(map, "_avatarY", (double)spot.Y);
            if ((bool)Invoke(map, "TryTakeTable")! && CabinetOfTheSeat(map) == 0)
            {
                return;
            }
            if (Get(map, "_table") is not null)
            {
                Invoke(map, "CloseTable");
            }
        }

        Assert.Fail("no free top on this floor is out in the hall.");
    }

    private static IEnumerable<DeckPlan.ConsoleSpot> FreeTops(Pages.Map map)
    {
        DeckPlan.ConsoleSpot[] tops =
            [.. ThePlan(map).Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveTable)];
        Assert.True(tops.Length > 0, "this floor carves no free tops at all.");
        return tops;
    }

    private static int CabinetOfTheSeat(Pages.Map map) =>
        Get(map, "_table") is { } seat
            ? (int)seat.GetType().GetProperty("Cabinet", Hidden)!.GetValue(seat)!
            : 0;

    private static CabinetPrivacy.Stage StageNow(Pages.Map map) =>
        (CabinetPrivacy.Stage)SeatState.Seat(map)!.GetType()
            .GetProperty("CabinetStage", Hidden)!.GetValue(SeatState.Seat(map))!;

    /// <summary>The cabinets the counter has already written down this excursion — the law's own ledger,
    /// asked directly so the guard does not rest on the field book being the only witness.</summary>
    private static HashSet<string> TheWitnessSet(Pages.Map map)
    {
        object ex = Get(map, "_surface")!;
        return (HashSet<string>)ex.GetType()
            .GetProperty("CabinetsWitnessed", Hidden)!.GetValue(ex)!;
    }

    /// <summary>How many times this exact sentence is in the captain's own book.</summary>
    private static int TimesFiled(Pages.Map map, string line)
    {
        var notes = (System.Collections.IEnumerable)Get(map, "_fieldNotes")!;
        int seen = 0;
        foreach (object note in notes)
        {
            string text = (string)note.GetType().GetProperty("Text")!.GetValue(note)!;
            if (string.Equals(text, line, StringComparison.Ordinal))
            {
                seen++;
            }
        }
        return seen;
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

    private static object? Get(object o, string name)
    {
        if (SeatState.TryFollow(o, name, out object? seated))
        {
            return seated;
        }

        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        Assert.True(prop is not null, $"the component has no `{name}` — this guard is reading a dead name.");
        return prop!.GetValue(o);
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        object target = map;
        if (call is null && SeatState.Seat(map) is { } seat)
        {
            call = seat.GetType().GetMethod(method, Hidden);
            target = seat;
        }
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(target, args);
    }
}
