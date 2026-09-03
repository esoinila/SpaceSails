using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #875 · TWO GRIPS, ONE WALK.
///
/// <para>Owner's ruling, 2026-08-15: <i>"click to walk should always be on when the arrows for walking are
/// active also. The two should be linked as alternative UI methods for walking."</i></para>
///
/// <h3>What was wrong</h3>
///
/// <para>Click-to-walk shipped behind <c>?autowalk=1</c> (#729) as a dev cheat <i>"until the owner rules on
/// always-on"</i>, and the ruling above is that ruling. Underneath the flag there was a second, quieter
/// fault, and it is the one these guards are really about: <b>the two grips on the same walk were written by
/// two authors</b>. The key handler asked <c>CaptainIsUnderEscort</c> inline; the click asked a property
/// called <c>AutoWalkAvailable</c> which spelled the same law again with a dev flag, the view mode and the
/// excursion mixed into it. Two spellings of one law is this repo's FIRST named bug class, and it had
/// already produced a floor you could cross with the arrows and not with a finger: <c>/map?designate=1</c>
/// carried no <c>autowalk</c>, so the park's silence in #866 had a duller cause sitting underneath the real
/// one.</para>
///
/// <h3>The one predicate</h3>
///
/// <para><c>Map.Deck.TheCaptainsLegsAreTheirOwn</c> — <c>_deckMode &amp;&amp; !CaptainIsUnderEscort</c> —
/// asked by <c>HandleDeckKey</c>'s WASD case and by <c>ClickToWalkAt</c>, and by nothing else. The click
/// grip's one extra question is <c>ADeckClickIsAPlaceOnTheFloor</c>, and it is about the CANVAS (is that
/// pixel a floor or the ecliptic?) rather than about the walk. Since #958 removed the walk-in view that was
/// the other term in it, that question is <c>_deckMode</c> and nothing else.</para>
///
/// <h3>Why these are driven</h3>
///
/// <para>Every law below is asked of a REAL <see cref="Pages.Map"/> on a REAL generated Hive floor, through
/// the shipping verbs: the key goes through <c>HandleDeckKey</c>, the click through <c>ClickToWalkAt</c> at
/// a pixel resolved by the renderer's own projection, and the frames are spent in <c>MoveAvatar</c>. A guard
/// that read the source for the predicate's NAME would pass on a build where one grip had quietly grown a
/// second opinion three lines lower down.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TwoGripsOneWalkTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The floor these guards drive. The GENERATOR's floor, never a hand-typed room — a guard
    /// handed a world it built itself cannot tell pass from fail.</summary>
    private const string Body = "luna";

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor to walk about on.");

    /// <summary>How far a body has to move before this file calls it walking. Two hundredths of a deck unit
    /// is a fiftieth of one frame's stride at the captain's own speed — far below anything a walk does, far
    /// above the arithmetic.</summary>
    private const double WalkedDu = 0.02;

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD (a) · WITH NO FLAG IN THE URL AT ALL, A CLICK PLANS AND WALKS.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The owner's ruling, asked of the shipping component with nothing switched on: stand on a floor, point
    /// at a fixture the Hive itself invites you to, and walk there.
    ///
    /// <para><b>Proven RED</b> by script-reverting the flag back into the click gate — <c>_autoWalkCheat</c>
    /// restored as a field, the boot parse writing to it again, and <c>|| !_autoWalkCheat</c> put back into
    /// the click's own question, which is exactly what shipped before this issue:</para>
    /// <code>
    /// Failed SpaceSails.Client.Tests.TwoGripsOneWalkTests.AClickWalksWithNoFlagInTheUrlAtAll [224 ms]
    ///   Error Message:
    ///    a click on the deck planned no route at all, on a floor with nothing held and no flag in the URL.
    ///    The owner's ruling is that the click is a control of the game: "click to walk should always be on
    ///    when the arrows for walking are active also."
    /// </code>
    /// </summary>
    [Fact]
    public void AClickWalksWithNoFlagInTheUrlAtAll()
    {
        Pages.Map map = OnTheFloor();          // no query, no flag, no field poked on the way in
        StandOnTheFloor(map);

        DeckPlan.ConsoleSpot target = SomewhereToWalkTo(map);
        (double px, double py) = ThePixelFor(map, target);
        Invoke(map, "ClickToWalkAt", px, py);

        Assert.True(Get(map, "_autoWalk") is AutoWalk { Active: true },
            "a click on the deck planned no route at all, on a floor with nothing held and no flag in the "
            + "URL. The owner's ruling is that the click is a control of the game: \"click to walk should "
            + "always be on when the arrows for walking are active also.\"");

        // …and it is WALKED, by the same frames the arrow keys are walked by. A route that is planned and
        // never spent would satisfy the sentence above and be the worse bug.
        for (int frame = 0; frame < 4000 && Get(map, "_autoWalk") is AutoWalk { Active: true }; frame++)
        {
            Invoke(map, "MoveAvatar", 1.0 / 60.0);
        }
        (double ax, double ay) = Where(map);
        Assert.True(target.DistanceFrom(ax, ay) <= DeckPlan.InteractRadius,
            $"the clicked walk stopped at ({ax:0.00},{ay:0.00}), which is not within reach of the fixture "
            + $"pointed at ({target.X:0.00},{target.Y:0.00}).");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD (b) · ONE PREDICATE: WHATEVER THE KEY ANSWERS, THE CLICK ANSWERS.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>What one grip did with the captain's legs: did the body move, and what was said about it.
    /// The pair is the point — a hold that moves nobody and says nothing is indistinguishable from a broken
    /// control, and two grips that say DIFFERENT things about one law are the bug this issue is.</summary>
    private readonly record struct Answer(bool Walked, string? Said);

    /// <summary>One way the world can be while somebody reaches for the controls: a name, and how to put the
    /// component into it. Each is applied to a floor that is otherwise identical, so the only thing the two
    /// grips can be disagreeing about is the hold itself.</summary>
    private sealed record Hold(string Name, Action<Pages.Map> Apply);

    private static IReadOnlyList<Hold> EveryHold() =>
    [
        new("nothing at all — a floor, and the captain standing on it", _ => { }),
        new("#833 · a guard walking the captain off his floor", PutUnderEscort),
        new("#847 · sitting on a stool at the counter", map => Invoke(map, "TakeAStool")),
        new("no excursion at all — a deck plan and no surface record", map => Set(map, "_surface", null)),
    ];

    /// <summary>
    /// #875 · THE GUARD THE RULING ASKED FOR. For every way the world can hold, help or charge a captain who
    /// wants to move, BOTH GRIPS ARE DRIVEN and their answers compared: the body either moved or it did not,
    /// and the same sentence came out either way.
    ///
    /// <para>The four holds are the four the issue names. The escort REFUSES both (#833). The stool CHARGES
    /// both — #847's ruling is that a seat costs the stand and then walks, by key or by finger. A floor with
    /// no excursion record behind it is the state the SHIP's own deck is in, and it used to walk under the
    /// arrows and refuse the finger, silently, because the click's gate asked for a surface the walk never
    /// needed.</para>
    ///
    /// <para><b>Proven RED twice</b>, by reintroducing exactly the second author this issue is about.
    /// First with the click gate given its own extra opinion — <c>|| _surface is null</c> — while the arrow
    /// keys keep theirs:</para>
    /// <code>
    /// Failed SpaceSails.Client.Tests.TwoGripsOneWalkTests.BothGripsAnswerTheSameOnEveryHold [306 ms]
    ///   Error Message:
    ///    the two grips disagree about the captain's legs. With no excursion at all — a deck plan and no
    ///    surface record: the ARROW KEYS walked (said: nothing), the CLICK did not (said: nothing). The
    ///    owner's ruling is that they are one walk with two grips: "The two should be linked as alternative
    ///    UI methods for walking."
    /// </code>
    /// <para>…and again with the click asking a WEAKER question than the keys (<c>if (!_deckMode) return;</c>,
    /// the escort dropped from the click path alone), which is the same disagreement running the other way —
    /// and note it is caught by the SENTENCE rather than by the legs:</para>
    /// <code>
    /// Failed SpaceSails.Client.Tests.TwoGripsOneWalkTests.BothGripsAnswerTheSameOnEveryHold [243 ms]
    ///   Error Message:
    ///    the two grips say different things about one law. With #833 · a guard walking the captain off his
    ///    floor: the arrow keys said "👮 "This way." He is walking you out, and he is walking you out." and
    ///    the click said nothing. A refusal a player meets through one control and not the other is the same
    ///    control being broken half the time.
    /// </code>
    /// </summary>
    [Fact]
    public void BothGripsAnswerTheSameOnEveryHold()
    {
        foreach (Hold hold in EveryHold())
        {
            Answer key = DriveTheKeyGrip(hold);
            Answer click = DriveTheClickGrip(hold);

            Assert.True(key.Walked == click.Walked,
                $"the two grips disagree about the captain's legs. With {hold.Name}: the ARROW KEYS "
                + $"{(key.Walked ? "walked" : "did not")} (said: {Said(key)}), the CLICK "
                + $"{(click.Walked ? "walked" : "did not")} (said: {Said(click)}). The owner's ruling is "
                + "that they are one walk with two grips: \"The two should be linked as alternative UI "
                + "methods for walking.\"");

            Assert.True(key.Said == click.Said,
                $"the two grips say different things about one law. With {hold.Name}: the arrow keys said "
                + $"{Said(key)} and the click said {Said(click)}. A refusal a player meets through one "
                + "control and not the other is the same control being broken half the time.");
        }

        static string Said(Answer a) => a.Said is null ? "nothing" : $"\"{a.Said}\"";
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD (c) · NO DEV BOOT IS WALKABLE BY KEY AND NOT BY CLICK.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #875 · Every quick-start in <see cref="DevStarts"/>, swept: the flag the URL carries (or does not) is
    /// applied the way the boot applied it, and then BOTH grips are driven on the same floor.
    ///
    /// <para>The applying is deliberately written as "set the retired field <b>if it still exists</b>". On
    /// today's build it does not exist and the loop proves the stronger thing — that nothing in a URL can
    /// reach the walk gate at all. Put the field back and the sweep immediately reports every dev boot the
    /// old flag missed, which is what #875 was filed on.</para>
    ///
    /// <para><b>Proven RED</b> by script-reverting the flag into the click gate (the field restored, the
    /// boot parse writing to it, and <c>|| !_autoWalkCheat</c> back in the click's question). Not 46 of 51,
    /// as the issue guessed off the URL table — <b>51 of 51</b>, because the two boots that "carried"
    /// autowalk never carried it in a URL at all: <c>?tablescene=</c> set the flag in a branch of the boot
    /// code, which is the second spelling this whole issue is about:</para>
    /// <code>
    /// Failed …TwoGripsOneWalkTests.NoDevStartsBootIsWalkableByKeyButNotByClick [14 ms]
    ///   Error Message:
    ///    51 of the 51 dev boots walk under the arrow keys and refuse the click:
    ///   · Phobos — THE MONOLITH (/map?dock=the-space-bar&amp;body=phobos&amp;site=0&amp;land=1)
    ///   · Phobos — something is paying attention (/map?dock=the-space-bar&amp;body=phobos&amp;site=0&amp;land=1&amp;watchers=1)
    ///   · Miranda — the false-slab maze (/map?dock=the-tilt&amp;site=0)
    ///   · Miranda — the Shadowed Rille (/map?dock=the-tilt&amp;site=1)
    ///   · Miranda — land me straight on the ground (/map?dock=the-tilt&amp;site=0&amp;land=1)
    ///   · Miranda — jumped the moment you land (/map?dock=the-tilt&amp;site=0&amp;land=1&amp;reevers=4)
    /// … a URL that is walkable by one grip and not the other is a dev boot that lies about the game.
    /// </code>
    /// </summary>
    [Fact]
    public void NoDevStartsBootIsWalkableByKeyButNotByClick()
    {
        Assert.NotEmpty(DevStarts.All);

        // ONE floor, every URL. The flag was a boot-time fact and never a floor-time one, so a fresh floor
        // per row would cost fifty generations to prove the same sentence fifty times.
        Pages.Map map = OnTheFloor();
        StandOnTheFloor(map);
        (double homeX, double homeY) = Where(map);
        string wayOut = TheDirectionWithRoomToWalk(map);
        DeckPlan.ConsoleSpot target = SomewhereToWalkTo(map);
        (double px, double py) = ThePixelFor(map, target);

        FieldInfo? retired = typeof(Pages.Map).GetField("_autoWalkCheat", Hidden);

        var refused = new List<string>();
        foreach (DevStarts.Entry entry in DevStarts.All)
        {
            // Whatever this URL says about the flag, said to the component the way the boot said it — and
            // nothing at all when the field has retired, which is the law rather than a shortcut.
            retired?.SetValue(map, AutoWalk.EnabledIn(entry.Url));

            Invoke(map, "CancelAutoWalk", false);
            Set(map, "_avatarX", homeX);
            Set(map, "_avatarY", homeY);
            Invoke(map, "ClickToWalkAt", px, py);
            bool byClick = Get(map, "_autoWalk") is AutoWalk { Active: true };

            Invoke(map, "CancelAutoWalk", false);
            Set(map, "_avatarX", homeX);
            Set(map, "_avatarY", homeY);
            Invoke(map, "HandleDeckKey", wayOut);
            Invoke(map, "MoveAvatar", 1.0 / 60.0);
            bool byKey = Math.Abs(((double)Get(map, "_avatarX")!) - homeX)
                + Math.Abs(((double)Get(map, "_avatarY")!) - homeY) > WalkedDu;
            ((HashSet<string>)Get(map, "_deckKeys")!).Clear();

            Assert.True(byKey,
                $"the arrow keys walk nobody on this floor under {entry.Url} — the bench has lost its "
                + "world and could not tell a refused click from a wall.");
            if (!byClick)
            {
                refused.Add($"{entry.Label} ({entry.Url})");
            }
        }

        Assert.True(refused.Count == 0,
            $"{refused.Count} of the {DevStarts.All.Count} dev boots walk under the arrow keys and refuse "
            + $"the click:{Environment.NewLine}  · "
            + string.Join($"{Environment.NewLine}  · ", refused.Take(6))
            + $"{Environment.NewLine}… a URL that is walkable by one grip and not the other is a dev boot "
            + "that lies about the game.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD (d) · ?autowalk=1 STILL PARSES, AND CHANGES NOTHING.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #875 · The retired flag is an ALIAS, not a deletion. An old dev link, a line of the testing guide or a
    /// UiGate canary carrying <c>?autowalk=1</c> still resolves to a world; the answer simply goes nowhere.
    ///
    /// <para>Three halves of one fact: Core still parses the spelling, the client still runs that parse over
    /// the boot query and discards it, and no gate anywhere in the client reads a flag any more.</para>
    ///
    /// <para><b>Proven RED</b> by the same script-revert that reds guards (a) and (c) — the field restored
    /// and the boot parse writing to it instead of discarding it:</para>
    /// <code>
    /// Failed …TwoGripsOneWalkTests.TheRetiredAutoWalkFlagStillParsesAndChangesNothing [4 ms]
    ///   Error Message:
    ///    Assert.Contains() Failure: Sub-string not found
    /// String:    "using System.Globalization;\nusing System."···
    /// Not found: "_ = AutoWalk.EnabledIn(uri.Query);"
    /// </code>
    /// </summary>
    [Fact]
    public void TheRetiredAutoWalkFlagStillParsesAndChangesNothing()
    {
        // It still parses — an old URL is a contract.
        Assert.True(AutoWalk.EnabledIn("?autowalk=1"));
        Assert.True(AutoWalk.EnabledIn("?secretlab=deep&autowalk=1&nerve=3"));
        Assert.False(AutoWalk.EnabledIn("?designate=1"));

        // The boot still runs the parse, and throws the answer away.
        string sim = Sim();
        Assert.Contains("_ = AutoWalk.EnabledIn(uri.Query);", sim, StringComparison.Ordinal);

        // …and nothing reads it. Not the gate, not a scene cheat, not a corner of the deck.
        foreach (string file in Directory.EnumerateFiles(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*.cs", SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("_autoWalkCheat", File.ReadAllText(file), StringComparison.Ordinal);
        }

        // And no dev start carries the dead flag, which would read as a control somebody still has to ask for.
        Assert.DoesNotContain(DevStarts.All,
            e => e.Url.Contains("autowalk", StringComparison.OrdinalIgnoreCase));
    }

    // ── DRIVING THE TWO GRIPS ─────────────────────────────────────────────────────────────────────────

    /// <summary>The arrow keys, on a floor in the given hold: press, spend a second of frames, and report
    /// whether the body moved and what was said. The direction is chosen while the captain is still FREE —
    /// a room is a room, and a heading that happens to face a wall would report a refusal that never
    /// happened.</summary>
    private static Answer DriveTheKeyGrip(Hold hold)
    {
        Pages.Map map = OnTheFloor();
        StandOnTheFloor(map);
        string key = TheDirectionWithRoomToWalk(map);
        hold.Apply(map);

        (double x0, double y0) = Where(map);
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "HandleDeckKey", key);
        for (int frame = 0; frame < 60; frame++)
        {
            Invoke(map, "MoveAvatar", 1.0 / 60.0);
        }
        (double x1, double y1) = Where(map);
        return new Answer(Math.Abs(x1 - x0) + Math.Abs(y1 - y0) > WalkedDu, ThePulse(map));
    }

    /// <summary>The same walk, the other grip: a click at the pixel a captain would have pressed to point at
    /// a fixture this floor invites them to, through the projection the renderer is drawing with.</summary>
    private static Answer DriveTheClickGrip(Hold hold)
    {
        Pages.Map map = OnTheFloor();
        StandOnTheFloor(map);
        DeckPlan.ConsoleSpot target = SomewhereToWalkTo(map);
        (double px, double py) = ThePixelFor(map, target);
        hold.Apply(map);

        (double x0, double y0) = Where(map);
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "ClickToWalkAt", px, py);
        for (int frame = 0; frame < 60; frame++)
        {
            Invoke(map, "MoveAvatar", 1.0 / 60.0);
        }
        (double x1, double y1) = Where(map);
        return new Answer(Math.Abs(x1 - x0) + Math.Abs(y1 - y0) > WalkedDu, ThePulse(map));
    }

    /// <summary>#833 · Put a guard on the captain's elbow. The escort is one field — <c>_escort</c> — and
    /// the predicate under test asks whether it is there, so an uninitialised guard is the whole of the
    /// state: this bench is measuring who may steer, not what the man says on the way.</summary>
    private static void PutUnderEscort(Pages.Map map)
    {
        Type guard = typeof(Pages.Map).GetNestedType("Guard", Hidden | BindingFlags.Public)
            ?? throw new InvalidOperationException("Map has no nested Guard — #833's escort has moved.");
        Set(map, "_escort", RuntimeHelpers.GetUninitializedObject(guard));
        Assert.True((bool)Get(map, "CaptainIsUnderEscort")!,
            "the bench failed to put the captain under escort at all.");
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component on a real Hive floor, with nothing running but the deck — and, since #875,
    /// with NOTHING SWITCHED ON: no flag, no cheat, no field poked to make a control exist. That is the
    /// whole of guard (a).
    ///
    /// <para>The one piece of theatre is the render handle: a <see cref="ComponentBase"/> that was never
    /// attached to a renderer throws out of <c>StateHasChanged</c>, so it is told it already has a render
    /// queued — the framework's own early-out, which makes the call a silent no-op.</para></summary>
    private static Pages.Map OnTheFloor()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved, and the deck verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
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

        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Put the captain somewhere a walk can start from: the floor's own step-off square, reached
    /// the way the game reaches it — sit on the bench the generator carved, then stand up.</summary>
    private static void StandOnTheFloor(Pages.Map map)
    {
        DeckPlan.ConsoleSpot bench = ThePlan(map).Consoles
            .FirstOrDefault(c => c.Kind == DeckPlan.ConsoleKind.HiveBench);
        Assert.True(bench.Kind == DeckPlan.ConsoleKind.HiveBench,
            $"{Body} B{-TheFloor} carves no bench — this bench has nowhere to start a walk from.");

        Set(map, "_avatarX", (double)bench.X);
        Set(map, "_avatarY", (double)bench.Y);
        Assert.True((bool)Invoke(map, "TryTakeBench")!, "the press at the bench was not taken.");
        Invoke(map, "StandUpBeforeWalking");
        Assert.True(Get(map, "_table") is null, "the captain is still sitting down.");
        Set(map, "_pulse", PulseSlot.Empty);
    }

    /// <summary>Somewhere on this floor worth walking to: the nearest fixture the Hive INVITES you to walk
    /// to — a promise the game already makes, rather than a coordinate a test picked out of a room.</summary>
    private static DeckPlan.ConsoleSpot SomewhereToWalkTo(Pages.Map map)
    {
        (double sx, double sy) = Where(map);
        return ThePlan(map).Consoles
            .Where(c => c.Kind is DeckPlan.ConsoleKind.HiveHaul or DeckPlan.ConsoleKind.HiveLift
                or DeckPlan.ConsoleKind.HiveAmenity or DeckPlan.ConsoleKind.HiveRefuge)
            .OrderBy(c => ((c.X - sx) * (c.X - sx)) + ((c.Y - sy) * (c.Y - sy)))
            .First();
    }

    /// <summary>The pixel a captain would have pressed to point at a fixture, through the projection the
    /// renderer is drawing with right now — never arithmetic written down a second time (this repo's first
    /// named bug class is exactly that).</summary>
    private static (double Px, double Py) ThePixelFor(Pages.Map map, DeckPlan.ConsoleSpot at)
    {
        (double sx, double sy) = Where(map);
        DeckView.Placement glass = DeckView.PlacementFor(
            ThePlan(map), (int)Get(map, "_viewportWidth")!, (int)Get(map, "_viewportHeight")!,
            sx, sy, (double)Get(map, "_deckPanX")!, (double)Get(map, "_deckPanY")!);
        return (glass.Ox + (at.X * glass.Scale), glass.Oy - (at.Y * glass.Scale));
    }

    /// <summary>Which way there is floor to walk in from here — tried through the shipping stepper, because
    /// a room is a room and any one heading may be a wall.</summary>
    private static string TheDirectionWithRoomToWalk(Pages.Map map)
    {
        var held = (HashSet<string>)Get(map, "_deckKeys")!;
        foreach (string key in new[] { "w", "a", "s", "d" })
        {
            (double x, double y) = Where(map);
            held.Clear();
            held.Add(key);
            Invoke(map, "MoveAvatar", 1.0 / 60.0);
            (double nx, double ny) = Where(map);
            Set(map, "_avatarX", x);
            Set(map, "_avatarY", y);
            if (Math.Abs(nx - x) > 1e-9 || Math.Abs(ny - y) > 1e-9)
            {
                held.Clear();
                return key;
            }
        }
        held.Clear();
        throw new InvalidOperationException("the captain is walled in on all four sides.");
    }

    private static (double X, double Y) Where(Pages.Map map) =>
        ((double)Get(map, "_avatarX")!, (double)Get(map, "_avatarY")!);

    private static string? ThePulse(Pages.Map map) => ((PulseSlot)Get(map, "_pulse")!).Message;

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

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

    private static string ClientSource(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    /// <summary>#870 · The sim page is nine partials by subject now, so "the sim" a guard reads over is all
    /// of them — exactly the text it read out of one file before the split.</summary>
    private static string Sim() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Sim*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>#870 lane 6b - The five seat fields live on the page's <c>_seating</c> object now, so this
    /// follows them there (<see cref="SeatState"/>); every assertion below still asks for the state by the
    /// name it was written with.</summary>
    private static object? Get(object o, string member) =>
        PatrolState.TryFollow(o, member, out object? onTheRound) ? onTheRound
        : SeatState.TryFollow(o, member, out object? seated) ? seated
        : o.GetType().GetField(member, Hidden) is { } f
            ? f.GetValue(o)
            : (o.GetType().GetProperty(member, Hidden)
               ?? throw new InvalidOperationException($"the component has no `{member}`.")).GetValue(o);

    private static void Set(object o, string field, object? value)
    {
        if (!PatrolState.TrySet(o, field, value))
        {
            (o.GetType().GetField(field, Hidden)
             ?? throw new InvalidOperationException($"the component has no `{field}` field."))
            .SetValue(o, value);
        }
    }

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"the component has no `{method}` method."))
        .Invoke(o, args);
}
