using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #828 · THE DISPOSAL YOU WATCH, AND THE TWO WAYS OF READING A SHEET — driven in a running world.
///
/// <para>Owner: <i>"One thing the good offices would also have is a safe paper disposal trashes… that
/// visually destroy the notes as we watch… a more secure disposal than restaurant trash."</i> And, the same
/// sitting: <i>"read is like quick glance and dig at it is carefully reading and comparing the document
/// against other sources and notes."</i></para>
///
/// <h3>Why these are driven and not read</h3>
/// <para>A shape guard could assert that <c>RipItUp</c> types the word <c>Shred</c> today and would go green
/// for ever on the day the far end quietly stops consuming the sheet — this repo's fifth named bug class is
/// a guard that cannot tell a pass from a fail. So a captain is stood at a real machine on a real floor, the
/// row is pressed, the clock is run, and what is asserted is the SLEEVE and the BOOK on the other side of
/// it.</para>
///
/// <para>The three laws here, in the order a player meets them:</para>
/// <list type="number">
/// <item><b>The beat is real.</b> The press does not destroy anything — it opens the one hold this surface
/// has. Walk off mid-sheet and the paper comes back, creased and still yours: a disposal nobody watched has
/// not happened.</item>
/// <item><b>The glance costs nothing and yields nothing.</b> Looking at a sheet's face leaves the book, the
/// threads, the seated register and the sleeve byte-identical, for every kind of paper in the game.</item>
/// <item><b>The dig implies the read.</b> Nothing that has been dug may show as unworked afterwards.</item>
/// </list>
/// </summary>
public sealed class TheDisposalYouWatchTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>Long enough to run any hold out, whatever the clock is set to. Read off Core rather than
    /// typed, so a re-tuned constant does not quietly turn these into tests of a bar that never fills.</summary>
    private static double LongEnough => Core.Processing.SecondsPerDocument + 1;

    // ── (a) THE BEAT ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · THE PRESS DOES NOT DESTROY ANYTHING. THE WATCHING DOES.
    ///
    /// <para>Feed the machine and the sheet is still in the sleeve, the book still has nothing in it, and a
    /// bar is filling over the captain's own mark. Only the far end of that hold takes the paper — and it
    /// takes it through the SAME act every other rung ends in, so the filed fact, the sentence and the
    /// witness ladder are the ones #798 already shipped.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting the <c>Tier.SecureDisposal</c> arm from <c>RipItUp</c>, so
    /// the top rung destroys on the press like the three bets below it:</para>
    /// <code>
    /// the press at the machine emptied the sleeve — the top rung has stopped being a thing you watch and
    /// gone back to being a bucket.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_SECURE_RUNG_IsABeatYouStandThroughAndOnlyThenIsTheSheetGone()
    {
        (string body, int level, RipAndBin.Bin machine) = TheMachineSomewhere();
        var paper = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:the-manifest");
        Pages.Map map = Bench(body, level, machine, paper, worked: true);

        // THE PRESS. Nothing is spent: the sheet is in the sleeve and the book is empty.
        Press(map, "RipItUp", paper);
        Assert.True(TheSleeve(map).Count == 1,
            "the press at the machine emptied the sleeve — the top rung has stopped being a thing you "
            + "watch and gone back to being a bucket.");
        Assert.Empty(TheBook(map));

        // …and a hold is running, wearing the bin's own glyph rather than the darkroom's camera — a camera
        // over a pair of hands feeding a shredder is the sim doing one thing while a picture reports
        // another.
        Assert.True(TheHold(map) is not null,
            "the press at the machine opened no hold at all — there is nothing to watch.");
        var work = (Core.Processing.Work)TheHoldsWork(map)!;
        Assert.Equal(Core.Processing.Work.Shred, work);
        Assert.Equal(RipAndBin.Glyph, Core.Processing.GlyphFor(work));

        // THE WATCHING. Run the clock out where the captain stood, and the sheet stops existing.
        Step(map, LongEnough);
        Assert.Empty(TheSleeve(map));
        Assert.Contains(TheBook(map),
            line => line.Contains(RipAndBin.TheBin(RipAndBin.Tier.SecureDisposal), StringComparison.Ordinal));
        Assert.Contains(TheBook(map),
            line => line.Contains("nothing left of it to find", StringComparison.Ordinal));
    }

    /// <summary>
    /// #828 · A DISPOSAL NOBODY WATCHED HAS NOT HAPPENED.
    ///
    /// <para>Step away mid-sheet and the rollers stop: the paper comes back out creased, readable, still in
    /// the sleeve, and nothing has been filed about it. That is the top rung's promise stated backwards, and
    /// it is the reason the rung is worth walking to a premium suite for — you are buying a fact you
    /// witnessed, so a beat you did not witness buys nothing.</para>
    ///
    /// <para><b>Proven RED</b> by script-routing the secure press straight to <c>TheSheetIsGone</c> (the
    /// same edit that reds the guard above), which destroys the sheet before there is anything to walk away
    /// from:</para>
    /// <code>
    /// the sheet was gone before anybody had finished watching it go.
    /// </code>
    /// </summary>
    [Fact]
    public void WALKING_OFF_MidSheetGivesThePaperBackAndFilesNothing()
    {
        (string body, int level, RipAndBin.Bin machine) = TheMachineSomewhere();
        var paper = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:the-manifest");
        Pages.Map map = Bench(body, level, machine, paper, worked: true);

        Press(map, "RipItUp", paper);
        Step(map, LongEnough / 2);
        Assert.True(TheSleeve(map).Count == 1,
            "the sheet was gone before anybody had finished watching it go.");

        // A stride is a stride: the surface walk is 9 du/s and the hold's tolerance is a nudge.
        Set(map, "_avatarX", (double)Get(map, "_avatarX")! + (Core.Processing.StandingToleranceDu * 3));
        Step(map, 0.1);

        Assert.True(TheHold(map) is null, "the machine went on eating after the captain walked off.");
        Assert.True(TheSleeve(map).Count == 1, "walking off mid-sheet did not give the paper back.");
        Assert.Empty(TheBook(map));

        // …and it is SAID, in the machine's own voice, naming the sheet and promising the sleeve. On the
        // PULSE and not in the dialog, which is right: starting a hold shuts the satchel by design (#696 —
        // the vulnerability is the mechanic), so by the time this line exists there is no dialog to put it
        // in and #680's law ("say it where the player is looking") lands it over the captain's own mark.
        string said = WhatWasSaid(map) ?? ThePulse(map) ?? "";
        Assert.Contains(RipAndBin.Glyph, said, StringComparison.Ordinal);
        Assert.Contains("sleeve", said, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nothing", said, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>#828 · …and the three rungs that are a BET are untouched by any of it: the press IS the act
    /// at a bucket, because there is nothing to watch. Stated beside the beat so the difference between the
    /// rungs is one assertion apart from the thing it differs from.</summary>
    [Fact]
    public void THE_THREE_BETS_StillDestroyOnThePressWithNoBeatAtAll()
    {
        var checkedRungs = new HashSet<RipAndBin.Tier>();
        foreach ((string body, int level, RipAndBin.Bin bin) in ManyBins())
        {
            if (bin.Tier == RipAndBin.Tier.SecureDisposal || !checkedRungs.Add(bin.Tier))
            {
                continue;
            }

            var paper = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:the-manifest");
            Pages.Map map = Bench(body, level, bin, paper, worked: true);
            Press(map, "RipItUp", paper);

            Assert.True(TheHold(map) is null, $"{bin.Tier}: a bucket opened a hold — there is nothing to "
                + "watch at a bin you drop a thing into.");
            Assert.Empty(TheSleeve(map));
        }

        Assert.True(checkedRungs.Count >= 2,
            $"only {checkedRungs.Count} rung(s) of the ladder were pressed — this proved little.");
    }

    // ── (b) THE GLANCE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · A GLANCE YIELDS NOTHING BOOKABLE — for every kind of paper in the game.
    ///
    /// <para>Owner: <i>"read is like quick glance and dig at it is carefully reading."</i> The glance is what
    /// anybody holding the sheet would see: free, standing, anywhere, as often as they like. What it may
    /// never do is put anything in the book — <b>if a fact matters, it is dig-gated, or it was never on the
    /// sheet</b>.</para>
    ///
    /// <para>Stated as BYTE-IDENTICAL state either side of the press, over the whole of what a reading could
    /// touch: the field book, the red threads, the seated register and the sleeve. A guard that only checked
    /// the book would go green on a glance that quietly spent the sheet.</para>
    ///
    /// <para><b>Proven RED</b> by script-adding a <c>FileNote(LeftBehind.GistOf(item, WhereYouAreStanding(),
    /// kept: true)!, "📋")</c> to <c>OpenItemCard</c> — a glance that books what it saw:</para>
    /// <code>
    /// Assert.Equal() Failure: Collections differ
    /// Expected: ["FieldNote { Text = 🧾 A line that was already in t"···]
    /// Actual:   ["FieldNote { Text = 🧾 A line that was already in t"···,
    ///            "FieldNote { Text = 📋 maintenance log, two hands —"···]
    ///                                                              ↑ (pos 1)
    /// </code>
    /// </summary>
    [Fact]
    public void THE_GLANCE_LeavesTheBookAndTheCaseByteIdentical()
    {
        (string body, int level, RipAndBin.Bin bin) = ManyBins().First();
        int looked = 0;

        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>().Where(RipAndBin.IsEvidence))
        {
            foreach (string id in new[] { "hive:doc:a", "hive:doc:b", "hive:doc:c", "hive:doc:d" })
            {
                var item = new Satchel.Item(kind, id);
                Pages.Map map = Bench(body, level, bin, item, worked: false);

                // A world with something in it, so "unchanged" is a statement about a book that has pages.
                Invoke(map, "FileNote", "🧾 A line that was already in the book.", "🧾");

                IReadOnlyList<string> book = TheBook(map);
                IReadOnlyList<string> sleeve = TheSleeve(map);
                IReadOnlyList<string> threads = TheThreads(map);
                IReadOnlyList<string> worked = TheRegister(map);

                // THE GLANCE — the shipping press behind the 🔍 on a satchel row.
                Invoke(map, "OpenItemCard", item);
                looked++;

                Assert.Equal(book, TheBook(map));
                Assert.Equal(sleeve, TheSleeve(map));
                Assert.Equal(threads, TheThreads(map));
                Assert.Equal(worked, TheRegister(map));

                // …and it is REPEATABLE, which is the other half of #603's law about looking.
                Invoke(map, "OpenItemCard", item);
                Invoke(map, "OpenItemCard", item);
                Assert.Equal(book, TheBook(map));
                Assert.Equal(sleeve, TheSleeve(map));
            }
        }

        Assert.True(looked >= 8, $"only {looked} glance(s) were driven — this net proves nothing.");
    }

    /// <summary>#828 · …and the glance actually SHOWS a paper its face, which is the anti-vacuity half: a
    /// glance that raised no card at all would leave every list byte-identical for ever and the guard above
    /// would be asserting nothing. What it shows is FieldClue's own page — the one composition, so a sheet
    /// cannot read two ways depending on which door the captain came through.</summary>
    [Fact]
    public void THE_GLANCE_ShowsTheSheetsOwnFace()
    {
        (string body, int level, RipAndBin.Bin bin) = ManyBins().First();
        var paper = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:the-manifest");
        Pages.Map map = Bench(body, level, bin, paper, worked: false);

        Invoke(map, "OpenItemCard", paper);
        var card = (DeckPlan.ConsoleSpot?)Get(map, "_viewObject");
        Assert.True(card is not null, "the 🔍 on a paper row raised no card at all.");

        Assert.Contains(FieldClue.Document(paper.Id), card!.Value.Caption ?? "", StringComparison.Ordinal);
        Assert.Contains(FieldClue.Label(FieldClue.CertaintyOf(paper.Id)).ToUpperInvariant(),
            card.Value.Label, StringComparison.Ordinal);
    }

    // ── (c) THE DIG IMPLIES THE READ ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · NOTHING THAT HAS BEEN DUG EVER SHOWS AS UNWORKED.
    ///
    /// <para>Owner: <i>"there should not be any unread items after digging."</i> This game keeps no unread
    /// BADGE on a satchel row — what it has is the bin picker's quiet flag, which is the same statement in
    /// the one place it matters: the moment a captain decides whether it is safe to destroy the sheet. So
    /// the law is stated where the state is visible. Dig a paper, and the picker says it is in the book;
    /// before the dig it says it is not, and warns.</para>
    ///
    /// <para>The far end of the dig is what is driven (<c>TheWriteUpLands</c>, exactly the method and
    /// arguments <c>CompleteProcessing</c>'s seated arm calls): the POSTURE gate in front of it is #784's
    /// law and is guarded in <c>SittingIsNotACutsceneTests</c>, and re-asserting it here would be a second
    /// answer to somebody else's question.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting the <c>ex.WrittenUpProperly.Add(...)</c> line from
    /// <c>TheWriteUpLands</c> — a dig that files the entry and forgets it dug:</para>
    /// <code>
    /// Paper hive:doc:a: a sheet that has been dug still shows as unworked in the picker.
    /// </code>
    /// </summary>
    [Fact]
    public void NO_DUG_SHEET_EverShowsAsUnworked()
    {
        (string body, int level, RipAndBin.Bin bin) = ManyBins().First();
        int dug = 0;

        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>().Where(RipAndBin.IsEvidence))
        {
            foreach (string id in new[] { "hive:doc:a", "hive:doc:b", "hive:doc:c" })
            {
                var item = new Satchel.Item(kind, id);
                Pages.Map map = Bench(body, level, bin, item, worked: false);

                // BEFORE: the picker says nothing has been got out of it, and warns before the press.
                Assert.Equal(RipAndBin.NotYetWorkedFlag, Ask<string>(map, "BinRowFlag", item));
                Assert.Contains(RipAndBin.NotYetWorkedWarning, Ask<string>(map, "BinRowHint", item),
                    StringComparison.Ordinal);

                // THE DIG's far end — the entry lands and the register learns it.
                Invoke(map, "TheWriteUpLands", Get(map, "_surface")!, item,
                    Invoke<string>(map, "WhereYouAreStanding"));
                dug++;

                // AFTER: it is in the book, and the warning is gone. Both halves, because a flag that
                // changed while the hint went on warning would be the page saying two things at once.
                Assert.True(RipAndBin.AlreadyInTheBookFlag == Ask<string>(map, "BinRowFlag", item),
                    $"{kind} {id}: a sheet that has been dug still shows as unworked in the picker.");
                Assert.DoesNotContain(RipAndBin.NotYetWorkedWarning, Ask<string>(map, "BinRowHint", item),
                    StringComparison.Ordinal);

                // …and the book kept something for it, which is what "dug" MEANS (the completeness law is
                // stated over in Core's AGlanceIsNotADigTests).
                Assert.NotEmpty(TheBook(map));
            }
        }

        Assert.True(dug >= 6, $"only {dug} sheet(s) were dug — this net proves nothing.");
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A world with a captain standing at one published bin, a sleeve, and nothing else running.
    /// Everything the act reads is set here and nothing is faked: the bins are the floor's own, the deck is
    /// the one the game builds for that floor, and the excursion is the shipping type.</summary>
    private static Pages.Map Bench(
        string body, int level, RipAndBin.Bin bin, Satchel.Item paper, bool worked)
    {
        var map = new Pages.Map();

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        SetProp(ex, "Stop", stop);
        SetProp(ex, "RestoreHavenId", null);
        SetProp(ex, "Site", new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        SetProp(ex, "Floor", level);

        if (worked)
        {
            ((HashSet<string>)exType.GetProperty("WrittenUpProperly")!.GetValue(ex)!)
                .Add($"{paper.Kind}:{paper.Id}");
        }

        Set(map, "_surface", ex);
        Set(map, "_satchel", new List<Satchel.Item> { paper });
        Set(map, "_avatarX", bin.StandX);
        Set(map, "_avatarY", bin.StandY);

        typeof(Pages.Map).GetMethod("RebuildSurfaceDeck", Hidden)!.Invoke(map, null);

        // The satchel is OPEN, because it is open in play for both doors onto this verb — and #680 sends the
        // act's own sentence into the dialog rather than under the backdrop's blur.
        Set(map, "_showSatchel", true);
        return map;
    }

    /// <summary>One tick of the surface hold, in sim seconds — the shipping stepper, so an abandonment is
    /// the one the game would take rather than one this file arranged.</summary>
    private static void Step(Pages.Map map, double seconds) =>
        typeof(Pages.Map).GetMethod("StepProcessing", Hidden)!.Invoke(map, [seconds]);

    private static object? TheHold(Pages.Map map)
    {
        object ex = Get(map, "_surface")!;
        return ex.GetType().GetProperty("Processing")!.GetValue(ex);
    }

    private static object? TheHoldsWork(Pages.Map map) =>
        TheHold(map)?.GetType().GetProperty("Work")!.GetValue(TheHold(map));

    private static IReadOnlyList<string> TheBook(Pages.Map map) =>
        [.. ((List<FieldNote>)Get(map, "_fieldNotes")!).Select(n => n.ToString())];

    private static IReadOnlyList<string> TheSleeve(Pages.Map map) =>
        [.. ((List<Satchel.Item>)Get(map, "_satchel")!).Select(i => i.Stored)];

    private static IReadOnlyList<string> TheThreads(Pages.Map map) =>
        [.. ((List<CaseThreads.Thread>)Get(map, "_caseThreads")!).Select(t => t.ToString())];

    private static IReadOnlyList<string> TheRegister(Pages.Map map)
    {
        object ex = Get(map, "_surface")!;
        var set = (HashSet<string>)ex.GetType().GetProperty("WrittenUpProperly")!.GetValue(ex)!;
        return [.. set.OrderBy(s => s, StringComparer.Ordinal)];
    }

    private static string? WhatWasSaid(Pages.Map map) => (string?)Get(map, "_satchelOutcome");

    /// <summary>…and what is on the pulse over the captain's own mark, which is where a line lands when
    /// there is no dialog left open to put it in.</summary>
    private static string? ThePulse(Pages.Map map) => ((PulseSlot)Get(map, "_pulse")!).Message;

    // ── THE FLOORS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The first secure disposal this build carves. Walked out of the generator rather than typed
    /// in, so nothing here can go stale against a re-carve.</summary>
    private static (string Body, int Level, RipAndBin.Bin Bin) TheMachineSomewhere() =>
        ManyBins().First(b => b.Bin.Tier == RipAndBin.Tier.SecureDisposal);

    private static IEnumerable<(string Body, int Level, RipAndBin.Bin Bin)> ManyBins()
    {
        foreach (string body in new[] { "luna", "miranda", "europa" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor =
                    UndergroundComplex.Build(body, level, MoonSurface.ExpeditionField());
                foreach (RipAndBin.Bin bin in floor.TheBins)
                {
                    yield return (body, level, bin);
                }
            }
        }
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static void Press(Pages.Map map, string method, Satchel.Item item)
    {
        MethodInfo? act = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(act is not null, $"the component has no `{method}` for this guard to press.");
        act!.Invoke(map, [item]);
    }

    private static T Ask<T>(Pages.Map map, string method, Satchel.Item item) =>
        (T)typeof(Pages.Map).GetMethod(method, Hidden)!.Invoke(map, [item])!;

    private static void Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? m = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(m is not null, $"the component has no `{method}` for this guard to drive.");
        m!.Invoke(map, args);
    }

    private static T Invoke<T>(Pages.Map map, string method, params object?[] args) =>
        (T)typeof(Pages.Map).GetMethod(method, Hidden)!.Invoke(map, args)!;

    private static object? Get(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static void SetProp(object o, string property, object? value) =>
        o.GetType().GetProperty(property)!.SetValue(o, value);
}
