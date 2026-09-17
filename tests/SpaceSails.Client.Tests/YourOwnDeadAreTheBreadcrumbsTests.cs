using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #563 · <b>YOUR OWN DEAD ARE THE BREADCRUMBS</b> — the page half, driven on a live
/// <see cref="Pages.Map"/>.
///
/// <para>Owner ruling, 2026-09-13, closing the last of #563's three open questions, verbatim: <i>"I love the
/// own lineage. If not enough material, fill in with strangers, preferably NPCs we know something
/// about."</i> And the loop, #455: <i>"after pirate insurance rebirth you can come see if your loot is still
/// there."</i></para>
///
/// <para>Core's guards (<c>TheGroundKeepsYourOwnDeadTests</c>) prove the record and the words. None of it
/// reaches a player unless the page is wired, and every way the wiring can be half-done here is a SILENT
/// forget rather than a crash — which is exactly why this file stands a real page up and presses the real
/// key rather than reading a claim about one:</para>
/// <list type="bullet">
///   <item>the grave is read AFTER the wake folds the excursion away, so it is always null and the roster
///   never learns anything;</item>
///   <item>the mark is recorded and never seeded on arrival, so the ground is clean anyway;</item>
///   <item>it is seeded and the press does not answer it, so there is nothing to walk up to;</item>
///   <item>the page files the note with no subject, so THREADS never stacks a life under the man;</item>
///   <item>it files the page again every visit, until the thread is six copies of one sentence.</item>
/// </list>
///
/// <para><b>Proven RED</b> (watched, quoted in the pull request) — see the per-case notes.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class YourOwnDeadAreTheBreadcrumbsTests
{
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;

    private const string Body = "phobos";
    private const string Gone = "Captain Otho Renn";

    /// <summary>The day the license changed hands, and the spot he fell on. Both arbitrary and both
    /// harmless: everything downstream is asserted against these, never against a second copy of them.</summary>
    private const int DiedOnDay = 42;
    private const double FellX = 12.5;
    private const double FellY = -7.25;

    /// <summary>A real site of a real body, asked of the generator — the fifth named bug class is a world
    /// that cannot tell pass from fail, and a hand-typed salt is exactly that.</summary>
    private static LandingSite Site(int index = 0) => LandingSites.At(Body, index);

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A page on a moon, with a roster behind it. <paramref name="graveSite"/> is which of the
    /// body's grounds the predecessor died on — pass a different one from the site being walked and the
    /// lineage is somewhere else entirely, which is a case this file needs.</summary>
    private static (Pages.Map Map, object Ex) OnAGround(
        int walkedSite = 0, int? graveSite = 0, DeathCause cause = DeathCause.Reevers, double nowDay = 44)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved, and the page's verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("Site")!.SetValue(ex, Site(walkedSite));
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "SimTime", nowDay * GroundMemory.DaySeconds);
        Set(map, "_groundMemory", new GroundMemory());

        // The roster, as the registry would hand it over: one buried captain, with a grave or without one.
        CaptainGrave? grave = graveSite is { } g
            ? new CaptainGrave(Body, Site(g).LayoutSalt, FellX, FellY, cause)
            : null;
        Set(map, "_activeThreadId", "u-1");
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)
        [
            new GameThreadInfo
            {
                Id = "u-1",
                CaptainName = "Captain Mabel Vane",
                AvatarIndex = 1,
                Retired = [new RetiredCaptain(Gone, DiedOnDay) { Grave = grave }],
            },
        ]);

        Invoke(map, "SeedTheLineageLyingHere", ex);
        return (map, ex);
    }

    private static List<GroundMemory.Scar> ScarsOf(object ex) =>
        (List<GroundMemory.Scar>)ex.GetType().GetProperty("Scars", Hidden | BindingFlags.Public)!.GetValue(ex)!;

    private static IReadOnlyList<FieldNote> BookOf(Pages.Map map) =>
        (IReadOnlyList<FieldNote>)Get(map, "_fieldNotes")!;

    private static void StandOn(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    // ── 1 · THE MARK IS ON THE GROUND HE DIED ON, AND ON NO OTHER ────────────────────────────────────

    /// <summary>
    /// ARRIVAL PUTS THE SUIT WHERE HE FELL. Derived from the roster on the way in — there is no ledger row
    /// for it and there must not be, because the roster already IS the record.
    ///
    /// <para><b>Red</b> by deleting the <c>SeedTheLineageLyingHere</c> call, and by seeding from
    /// <c>_groundMemory</c> instead of the roster (nothing is ever written there, so the ground stays
    /// clean).</para>
    /// </summary>
    [Fact]
    public void TheSuitIsLyingWhereTheLicenseChangedHands()
    {
        (_, object ex) = OnAGround();

        GroundMemory.Scar suit = Assert.Single(ScarsOf(ex));
        Assert.Equal(GroundMemory.ScarKind.Suit, suit.What);
        Assert.Equal(FellX, suit.X);
        Assert.Equal(FellY, suit.Y);
        Assert.Equal(DiedOnDay * GroundMemory.DaySeconds, suit.AtSimTime);
    }

    /// <summary>A body offers 2–4 grounds and every one of them rebuilds the same local frame (#320/#650),
    /// so a mark filtered on the body alone is a body lying on a plain the man never walked. And a captain
    /// whose death had no ground at all leaves nothing anywhere.</summary>
    [Fact]
    public void HeIsNotLyingOnTheGroundNextDoor()
    {
        Assert.Empty(ScarsOf(OnAGround(walkedSite: 1, graveSite: 0).Ex));
        Assert.Empty(ScarsOf(OnAGround(walkedSite: 0, graveSite: null).Ex));
    }

    // ── 2 · THE PRESS, AND THE PAGE IT FILES ────────────────────────────────────────────────────────

    /// <summary>
    /// WALK TO IT, PRESS THE KEY, AND THE BOOK HAS A PAGE ABOUT HIM. The sentence is Core's, asserted by
    /// CALLING Core — a transcription here would agree with the shipped words until the day one of them was
    /// edited. The SUBJECT is the whole of #741's promise: the entry is filed under the predecessor's own
    /// name, so THREADS stacks a life under the man who lived it.
    ///
    /// <para><b>Red</b> by filing through <c>FileNote</c> instead of <c>FileNoteAbout</c> (the page lands
    /// with no subject and the stack never forms), and by dropping the mark arm out of the [E] ladder.</para>
    /// </summary>
    [Fact]
    public void ThePressFilesThePageUnderHisOwnName()
    {
        (Pages.Map map, object ex) = OnAGround();
        StandOn(map, FellX, FellY);

        Assert.True((bool)Invoke(map, "TryReadTheMarkAtYourFeet")!);

        FieldNote page = Assert.Single(BookOf(map));
        var gone = new RetiredCaptain(Gone, DiedOnDay)
        {
            Grave = new CaptainGrave(Body, Site().LayoutSalt, FellX, FellY, DeathCause.Reevers),
        };

        Assert.Contains(LineageMark.YoursNote(gone), page.Text, StringComparison.Ordinal);
        Assert.Contains(CaptainSuccession.RetiredLine(gone), page.Text, StringComparison.Ordinal);
        Assert.Contains(DeathNarration.CauseWord(DeathCause.Reevers), page.Text, StringComparison.Ordinal);

        CaseSubjects.Subject about = Assert.Single(CaseSubjects.On(page));
        Assert.Equal(CaseSubjects.Kind.Person, about.Of);
        Assert.Equal(Gone, about.Name);

        _ = ex;
    }

    /// <summary>
    /// THE AGE IS THE GROUND'S OWN, AND IT IS THE REAL ELAPSED TIME. Two days after the death reads the
    /// middle band; a month after it reads the far one. The sentence is #1127's, asserted by calling it —
    /// dating a suit is the same question as dating a body, and this repo's third named bug class is two
    /// reporters of one truth.
    ///
    /// <para><b>Red</b> by stamping the mark with <c>SimTime</c> instead of the day he died (every band
    /// collapses to "Still smoking"), and by dropping the age from the page.</para>
    /// </summary>
    [Fact]
    public void TheAgeBandReadsTheRealElapsedTime()
    {
        foreach (double nowDay in new[] { 42.5, 44.0, 80.0 })
        {
            (Pages.Map map, _) = OnAGround(nowDay: nowDay);
            StandOn(map, FellX, FellY);
            Assert.True((bool)Invoke(map, "TryReadTheMarkAtYourFeet")!);

            Assert.EndsWith(
                GroundMemory.AgeLine(DiedOnDay * GroundMemory.DaySeconds, nowDay * GroundMemory.DaySeconds),
                Assert.Single(BookOf(map)).Text,
                StringComparison.Ordinal);
        }

        // …and the three days really are three different sentences, so the assertion above is not agreeing
        // with itself.
        Assert.Equal(3, new[] { 42.5, 44.0, 80.0 }
            .Select(d => GroundMemory.AgeLine(DiedOnDay * GroundMemory.DaySeconds, d * GroundMemory.DaySeconds))
            .Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// ONE PAGE, AND THEN THE GROUND IS HIS AGAIN. [E] on the regolith is BURY THE CHEST HERE (#440); a mark
    /// that ate that press for ever would be a grave a captain can never bury a chest beside, which is the
    /// complaint #316 already records against hanging a reading on this key. And the latch is DURABLE, so
    /// the next trip does not file the same sentence on top of the last one.
    ///
    /// <para><b>Red</b> by latching on the excursion instead of the ground ledger (the second visit files a
    /// duplicate), and by never latching at all (the second press answers).</para>
    /// </summary>
    [Fact]
    public void ThePageIsFiledOnceAndThenTheKeyGoesBackToTheGround()
    {
        (Pages.Map map, object ex) = OnAGround();
        StandOn(map, FellX, FellY);

        Assert.True((bool)Invoke(map, "TryReadTheMarkAtYourFeet")!);
        Assert.False((bool)Invoke(map, "TryReadTheMarkAtYourFeet")!);
        Assert.Single(BookOf(map));

        // A LATER TRIP: a fresh excursion over the same ground, seeded again — and the ledger the latch
        // lives on is the one that rides the vault, so the book stays at one page.
        object ledger = Get(map, "_groundMemory")!;
        (Pages.Map next, _) = OnAGround();
        Set(next, "_groundMemory", ledger);
        Set(next, "_fieldNotes", new List<FieldNote>(BookOf(map)));
        StandOn(next, FellX, FellY);

        Assert.False((bool)Invoke(next, "TryReadTheMarkAtYourFeet")!);
        Assert.Single(BookOf(next));

        _ = ex;
    }

    // ── 3 · THE STRANGERS, AND WHERE THEY MAY NOT GO ────────────────────────────────────────────────

    /// <summary>
    /// A HOLE ON A GROUND YOUR LINE NEVER DIED ON GETS A NAME — and the same name every time you come back,
    /// off the game's own cast. A hole on a ground where one of yours IS lying stays anonymous: the fill-in
    /// does not get to write over the material it exists to stand in for, which is the owner's own order of
    /// precedence in one branch.
    ///
    /// <para><b>Red</b> by dropping the "your own line died here" guard out of <c>WhatThisMarkSays</c> (the
    /// stranger names your predecessor's own crime scene).</para>
    /// </summary>
    [Fact]
    public void AStrangerNamesTheHoleOnlyWhereYourOwnLineNeverDied()
    {
        // A ground with no grave of yours on it: the rivals' hole gets a tag.
        (Pages.Map clean, object cleanEx) = OnAGround(graveSite: null);
        ScarsOf(cleanEx).Add(new GroundMemory.Scar(GroundMemory.ScarKind.Pit, 5, 5, 10 * GroundMemory.DaySeconds));
        StandOn(clean, 5, 5);

        Assert.True((bool)Invoke(clean, "TryReadTheMarkAtYourFeet")!);
        FieldNote tag = Assert.Single(BookOf(clean));
        string who = LineageMark.StrangerFor(Body, Site().LayoutSalt);
        Assert.Contains(LineageMark.StrangerNote(who), tag.Text, StringComparison.Ordinal);
        Assert.Contains(who, LineageMark.Cast);
        Assert.Equal(who, Assert.Single(CaseSubjects.On(tag)).Name);

        // The same ground with one of YOURS lying on it: the hole says nothing.
        (Pages.Map mine, object mineEx) = OnAGround();
        ScarsOf(mineEx).Add(new GroundMemory.Scar(GroundMemory.ScarKind.Pit, 5, 5, 10 * GroundMemory.DaySeconds));
        StandOn(mine, 5, 5);

        Assert.False((bool)Invoke(mine, "TryReadTheMarkAtYourFeet")!);
        Assert.Empty(BookOf(mine));
    }

    // ── 4 · WHAT THE PAGE ONLY CLAIMS, READ OFF ITS SOURCE ──────────────────────────────────────────
    //
    // Two facts this bench cannot drive — the grave is read inside a wake that flies a brain-backup to a
    // clinic, and the mark is drawn by a HUD build that wants a renderer — and both are exactly the shape of
    // half-wiring that ships silently. Read off the shipping method bodies, the way this file's siblings do.

    /// <summary>
    /// THE GROUND IS READ BEFORE THE WAKE TAKES IT AWAY. Ordering is the whole guard: <c>BustedResurrect</c>
    /// folds the excursion away as a failed gig, and a read placed after that fold is a read of null — for
    /// ever, silently, on the one path a player can never re-run.
    ///
    /// <para><b>Red</b> by moving the <c>TheGroundThatTookHim</c> call below <c>_surface = null</c>.</para>
    /// </summary>
    [Fact]
    public void TheWakeReadsTheGroundBeforeItFoldsTheExcursionAway()
    {
        string wake = Pages("Map.Combat.Busted.Wake.cs");

        int read = wake.IndexOf("TheGroundThatTookHim(b)", StringComparison.Ordinal);
        int folded = wake.IndexOf("_surface = null", StringComparison.Ordinal);
        int issued = wake.IndexOf("IssueSuccessorCaptain(b, grave)", StringComparison.Ordinal);

        Assert.True(read >= 0, "the wake reads no ground at all");
        Assert.True(folded >= 0, "the wake no longer folds the excursion away");
        Assert.True(read < folded, "the ground is read AFTER the excursion is gone — it can only be null");
        Assert.True(issued > folded, "the succession no longer carries the grave");

        // …and the SALT is what goes into the record, not the site index: it is the second half of every
        // ground key, and the thing a buried cache resolves to (#455's loop lives on that agreement).
        Assert.Contains("ex.Site.LayoutSalt, _avatarX, _avatarY", wake, StringComparison.Ordinal);
    }

    /// <summary>THE SUIT IS DRAWN, THROUGH THE MARK #316 ALREADY PAINTS. Seeded and never drawn is a silent
    /// forget: the press would answer at a spot with nothing on the screen. No new renderer and no new layer
    /// switch — it joins the husks, under the husk layer.</summary>
    [Fact]
    public void TheSuitIsDrawnThroughTheHuskMark()
    {
        string hud = Pages("Map.Surface.Hud.cs");
        int layer = hud.IndexOf("LayerVisible(\"finds.husks\")", StringComparison.Ordinal);
        int drawn = hud.IndexOf("GroundMemory.ScarKind.Suit", StringComparison.Ordinal);

        Assert.True(layer >= 0, "the husk layer gate is gone");
        Assert.True(drawn > layer, "the suit is not drawn under the husk layer");
        Assert.Contains("_hudHusks.Add((scar.X, scar.Y))", hud, StringComparison.Ordinal);

        // …and the keybar names the press, because a verb nobody is told about is a verb nobody has.
        Assert.Contains("TheMarkTakingYourPress() is not null", Pages("Map.Surface.Hud.Prompts.cs"),
            StringComparison.Ordinal);
    }

    // ── PLUMBING ─────────────────────────────────────────────────────────────────────────────────────

    private static string Pages(string file)
    {
        string path = Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", file);
        Assert.True(File.Exists(path), $"{file} has moved — this guard is reading a dead path");
        return File.ReadAllText(path);
    }

    private static object? Get(object o, string name) =>
        o.GetType().GetField(name, Hidden)!.GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the page has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(map, args);
    }
}
