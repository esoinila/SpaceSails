using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #160 · THE TUTORIAL MISSION — a milk run flown by the autopilot, end to end.
///
/// <para>Owner, 2026-07-16, during the moon-run playtest: <i>"I think we should have a tutorial for mission
/// also? Some easy milk run with autopilot from moon to moon, maybe?"</i> Three lessons existed. They teach
/// a hunt, a gun and a haven — every one of them a thing that happens when the work goes wrong. Nothing
/// taught the work, which is what the eight canon lines are for and what this class holds them to.</para>
///
/// <h3>What is actually at risk here</h3>
/// <para>A tutorial is the easiest thing in a codebase to fake. A checklist that ticks its own rows off a
/// counter LOOKS identical, on screen and in a green test, to one that reads the ship — right up until the
/// captain follows it and it congratulates them on a burn they never fired. So every one of the eight rows
/// below is driven by putting the WORLD into the state the step is about (a plan with a cast-off at its
/// head, a full tank, an arm with a pass-time on it, a clamp on the delivery berth, a quest gone TurnedIn)
/// and watching the lesson notice — and <see cref="A_STEP_THAT_HAS_NOT_HAPPENED_HoldsTheLessonWhereItIs"/>
/// is the other half of that: a world that satisfies every LATER step while one earlier step is left undone
/// must move the lesson nowhere. Without that second test the first one cannot tell a real gate from
/// <c>return true</c>.</para>
///
/// <para><b>Every guard here was proven red</b> before shipping, one breakage at a time with the branch put
/// back between each: <c>true</c> returned from each of the eight gates in turn; the beat between lines set
/// to zero; the board's posting call deleted; the frame loop's call deleted; the vault's write, its read and
/// the row it restores each deleted; a ninth string added to the lesson's own files; the checklist's rows
/// detached from the lines; and the destination allowed to be any haven rather than a berth.</para>
///
/// <para>Two of them came back GREEN on that pass and were rewritten until they did not — which is the whole
/// reason the exercise is done. <c>true</c> in gate 8 changed nothing, because the drive below only watched
/// what was SAID and the last step says nothing after it; it now asserts the row has not ticked either. And
/// letting the contract end at any haven changed nothing from Earth, where the nearest haven is a berth
/// anyway; <see cref="THE_CONTRACT_EndsAtABerthAndNotAtAMoonThereIsNoCounterOn"/> moves the bench to the one
/// berth in Sol where the two answers differ. A guard handed a world that cannot tell pass from fail is this
/// repo's fifth named bug class, and both of those were it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheMilkRunWalksTheWholeLoopTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    /// <summary>The berth the bench starts ashore at — a clampable station haven, which is the only kind of
    /// place a contract is taken at and the only kind that has a counter to be paid over.</summary>
    private const string HomeBerth = "selene-gate";

    // ── (a) THE LESSON POSTS ITS OWN CONTRACT ────────────────────────────────────────────────────────

    /// <summary>THE BOARD IS EMPTY UNTIL THE LESSON IS TAKEN ON. The watcher runs on every frame of every
    /// voyage; a captain who never opened the Tutorials tab must never be handed a job by it.</summary>
    [Fact]
    public void THE_BOARD_IsEmptyForACaptainWhoNeverTookTheLesson()
    {
        Pages.Map map = ACaptainAshoreAtABerth();

        Invoke(map, "WatchTheMilkRun");

        Assert.Null(Get<Pages.Map.Quest?>(map, "_pendingOffer"));
    }

    /// <summary>AND CHOOSING IT PUTS THE JOB ON THE BOARD — #1091's ruling, reaching a notice on a wall.
    /// Priced and addressed from the berth the captain is actually standing in, at the moment they are
    /// standing in it, so there is no window to miss and no job posted half an AU away.</summary>
    [Fact]
    public void CHOOSING_THE_LESSON_PostsAMilkRunOnTheBoardWhereTheCaptainIsStanding()
    {
        Pages.Map map = ACaptainAshoreAtABerth();
        Invoke(map, "SeedMilkRun");

        Invoke(map, "WatchTheMilkRun");

        Pages.Map.Quest posted = Get<Pages.Map.Quest?>(map, "_pendingOffer")
            ?? throw new InvalidOperationException("nothing on the board — the lesson posted no contract");
        Assert.Equal(MilkRunLesson.QuestId, posted.Id);
        Assert.Equal(Pages.Map.QuestKind.CargoRun, posted.Kind);
        Assert.Equal(MilkRunLesson.BoardGiver, posted.Giver);

        // …and it is a berth-to-berth haul: the loop this lesson teaches ends at a counter, and a moon
        // haven has no ⚓ to clamp and nobody behind a desk (#175).
        CelestialBody dest = TheSky().Bodies.First(b => b.Id == posted.DestBodyId);
        Assert.True(DockableHavens.IsDockable(dest),
            $"the lesson's contract ends at {dest.Name}, which cannot be clamped — step 7 promises a counter");
        Assert.NotEqual(HomeBerth, dest.Id);
    }

    /// <summary>AND IT ENDS AT A BERTH, EVEN WHEN THE NEAREST HAVEN IS NOT ONE. Standing on the Ringside
    /// Exchange, the closest haven in the sky is Enceladus — same planet, same reach, a haven in every list
    /// the game keeps — and it is a MOON: no ⚓ to clamp, no counter, delivery by parking in its orbit
    /// (#175). A lesson that ended there would say "Dock, and the contract pays at the counter" over a
    /// contract that does neither. This is the one berth in Sol where "nearest haven" and "nearest berth"
    /// give different answers, which is exactly why the test stands here and not at Earth.</summary>
    [Fact]
    public void THE_CONTRACT_EndsAtABerthAndNotAtAMoonThereIsNoCounterOn()
    {
        Pages.Map map = ACaptainAshoreAtABerth("ringside-exchange");
        Invoke(map, "SeedMilkRun");

        Invoke(map, "WatchTheMilkRun");

        Pages.Map.Quest posted = Get<Pages.Map.Quest?>(map, "_pendingOffer")
            ?? throw new InvalidOperationException("nothing on the board — the lesson posted no contract");
        Assert.NotEqual("enceladus", posted.DestBodyId);
        Assert.True(DockableHavens.IsDockable(TheSky().Bodies.First(b => b.Id == posted.DestBodyId)),
            $"the lesson's contract ends at {posted.DestBodyId}, which cannot be clamped");
    }

    /// <summary>…AND SAYS ITS FIRST LINE AS IT DOES. Step 1 is "take the contract", so line 1 is what the
    /// captain hears the moment the lesson becomes the thing they are doing.</summary>
    [Fact]
    public void CHOOSING_THE_LESSON_SaysTheFirstLineAndNoOther()
    {
        Pages.Map map = ACaptainAshoreAtABerth();

        Invoke(map, "SeedMilkRun");

        Assert.Equal(MilkRunLesson.Lines[0], Get<PulseSlot>(map, "_pulse").Message);
    }

    /// <summary>AND A CARD THE CAPTAIN PASSED COMES BACK — but not in the same breath. The general UI law is
    /// that nothing may be un-closable; the board is a thing you walk up to, so passing it clears it and
    /// walking ashore again re-posts it.</summary>
    [Fact]
    public void PASSING_THE_CARD_ClearsItForThisTripAshoreAndTheNextOnePostsItAgain()
    {
        Pages.Map map = ACaptainAshoreAtABerth();
        Invoke(map, "SeedMilkRun");
        Invoke(map, "WatchTheMilkRun");
        Invoke(map, "DeclineOffer");

        Invoke(map, "WatchTheMilkRun");
        Assert.Null(Get<Pages.Map.Quest?>(map, "_pendingOffer"));   // it does not shove itself back

        Set(map, "_deckMode", false);   // he walks back aboard…
        Invoke(map, "WatchTheMilkRun");
        Set(map, "_deckMode", true);    // …and comes ashore again
        Invoke(map, "WatchTheMilkRun");

        Assert.NotNull(Get<Pages.Map.Quest?>(map, "_pendingOffer"));
    }

    // ── (b) THE EIGHT STEPS, DRIVEN ON THE REAL WORLD ────────────────────────────────────────────────

    /// <summary>THE WHOLE LOOP, ONE STEP AT A TIME. Each of the eight is finished by putting the world into
    /// the state that step is ABOUT — never by poking the lesson — and after each one the game says the next
    /// line and the checklist stands on the next row. At the end the lesson is over and silent.</summary>
    [Fact]
    public void THE_EIGHT_LINES_AreSaidInOrderAsEachStepBecomesTheOneToDo()
    {
        Pages.Map map = ALessonTakenOn();

        for (int step = 1; step <= MilkRunLesson.StepCount; step++)
        {
            Assert.Equal(step, Get<int>(map, "_milkRunStep"));
            Assert.Equal(MilkRunLesson.Lines[step - 1], TheChecklistsCurrentRow(map));

            // Nothing said while the step is unfinished — a line fires ONCE, when its step arrives.
            Hush(map);
            WindTheClock(map);
            Invoke(map, "WatchTheMilkRun");
            Assert.Null(Get<PulseSlot>(map, "_pulse").Message);
            Assert.Equal(step, Get<int>(map, "_milkRunStep"));   // …and the row did not tick either

            Finish(map, step);
            Hush(map);
            WindTheClock(map);
            Invoke(map, "WatchTheMilkRun");

            string? said = Get<PulseSlot>(map, "_pulse").Message;
            if (step < MilkRunLesson.StepCount)
            {
                Assert.Equal(MilkRunLesson.Lines[step], said);
            }
            else
            {
                Assert.Null(said);   // there is no ninth line, and the lesson does not repeat its last
            }
        }

        Assert.Equal(MilkRunLesson.StepCount + 1, Get<int>(map, "_milkRunStep"));
    }

    /// <summary>AND A STEP THAT DID NOT HAPPEN HOLDS THE WHOLE LESSON WHERE IT IS. This is the test the
    /// eight rows above cannot do without: it hands the lesson a world in which every LATER step is already
    /// true — armed, cast off, warping, clamped at the delivery berth, paid — with the tank left half empty,
    /// and the lesson must sit on step 3 and say nothing. A gate that returned <c>true</c>, or a lesson that
    /// counted its own rows, would walk straight to the end here.</summary>
    [Fact]
    public void A_STEP_THAT_HAS_NOT_HAPPENED_HoldsTheLessonWhereItIs()
    {
        Pages.Map map = ALessonTakenOn();
        Finish(map, 1);
        Finish(map, 2);
        WindTheClock(map);
        Invoke(map, "WatchTheMilkRun");
        WindTheClock(map);
        Invoke(map, "WatchTheMilkRun");
        Assert.Equal(3, Get<int>(map, "_milkRunStep"));   // "top her off" is the row we are on

        for (int later = 4; later <= MilkRunLesson.StepCount; later++)
        {
            Finish(map, later);
        }
        Set(map, "_reactionMassPulses", 1);   // …and the tank is still not full

        Hush(map);
        for (int frame = 0; frame < 12; frame++)
        {
            WindTheClock(map);
            Invoke(map, "WatchTheMilkRun");
        }

        Assert.Equal(3, Get<int>(map, "_milkRunStep"));
        Assert.Null(Get<PulseSlot>(map, "_pulse").Message);
    }

    /// <summary>AND TWO STEPS THAT COME TRUE AT ONCE ARE STILL TAUGHT ONE AT A TIME. Several gates can
    /// already be satisfied when their step comes round — a tank the captain filled early, a warp they never
    /// dropped — and a lesson that ticked them all inside one frame would overwrite its own teaching and
    /// leave only the last line on the glass.</summary>
    [Fact]
    public void TWO_STEPS_ReadyInTheSameFrameStillCostTwoBreaths()
    {
        Pages.Map map = ALessonTakenOn();
        Finish(map, 1);
        Finish(map, 2);
        Finish(map, 3);
        Finish(map, 4);

        WindTheClock(map);
        Invoke(map, "WatchTheMilkRun");
        int afterOneBreath = Get<int>(map, "_milkRunStep");

        Invoke(map, "WatchTheMilkRun");   // same frame, same clock
        Assert.Equal(afterOneBreath, Get<int>(map, "_milkRunStep"));

        WindTheClock(map);
        Invoke(map, "WatchTheMilkRun");
        Assert.Equal(afterOneBreath + 1, Get<int>(map, "_milkRunStep"));
    }

    // ── (c) THE PLACE IT KEEPS ───────────────────────────────────────────────────────────────────────

    /// <summary>A RELOAD PUTS THE CHECKLIST BACK ON THE ROW IT WAS ON — which is how no line is ever said
    /// twice. <c>_tutorialStep</c> is not vaulted (it rests at 0 for every captain who never took a lesson,
    /// which the prey keeper reads), so the lesson's own place is what comes back and puts it there.</summary>
    [Fact]
    public void A_RELOAD_PutsTheChecklistBackOnTheRowTheVoyageWasOn()
    {
        Pages.Map map = ACaptainAshoreAtABerth();
        Set(map, "_milkRunStep", 5);

        Invoke(map, "RestoreTheMilkRunsPlace");

        Assert.Equal(MilkRunLesson.Lines[4], TheChecklistsCurrentRow(map));
        Assert.True(Get<bool>(map, "_showTutorial") == false,
            "a loaded save raised the checklist — #292's law is that only a fresh greeting or the captain's own hand does");
    }

    /// <summary>AND A FINISHED MILK RUN LEAVES THE OTHER LESSONS ALONE. Writing the step past the last row
    /// would tick every earlier lesson's card as done in the picker for a captain who never flew one.</summary>
    [Fact]
    public void A_FINISHED_LESSON_DoesNotMarkTheOtherThreeAsFlown()
    {
        Pages.Map map = ACaptainAshoreAtABerth();
        Set(map, "_milkRunStep", MilkRunLesson.StepCount + 1);

        Invoke(map, "RestoreTheMilkRunsPlace");

        Assert.Equal(0, Get<int>(map, "_tutorialStep"));
    }

    /// <summary>THE PAGE WRITES IT AND READS IT BACK. The whole of <c>BuildVault</c>/<c>ApplyVault</c> cannot
    /// be run on a bench (the load ends by berthing the ship), so the two ends the Core round-trip cannot see
    /// are held by source-shape — the idiom <c>TheCaseIsNotTiedToAPlaceTests</c> already writes down. Narrow
    /// enough that the only way to satisfy it is to actually wire the field.</summary>
    [Fact]
    public void THE_PAGE_WritesTheLessonsPlaceAndReadsItBack()
    {
        string vault = PagesFile("Map.Vault.cs");

        Assert.Contains("MilkRunLessonStep = _milkRunStep > 0 ? _milkRunStep : null,", vault, StringComparison.Ordinal);
        Assert.Contains("TheMilkRunResumes(vault.Progress?.MilkRunLessonStep);", vault, StringComparison.Ordinal);
        Assert.Contains("TheMilkRunIsUntaught();", vault, StringComparison.Ordinal);
    }

    /// <summary>AND THE WATCHER IS ACTUALLY IN THE LOOP. A lesson nothing calls is a lesson that never
    /// teaches — and it would be green everywhere above, because every test here calls it by hand.</summary>
    [Fact]
    public void THE_WATCHER_RunsOnTheFrameLoop()
    {
        Assert.Contains("WatchTheMilkRun();", PagesFile("Map.Sim.Tick.cs"), StringComparison.Ordinal);
    }

    // ── (d) THE EIGHT ARE THE ONLY NEW STRINGS ───────────────────────────────────────────────────────

    /// <summary>THE CHECKLIST'S ROWS ARE THE LINES THEMSELVES. The row the captain reads and the line the
    /// game speaks are one string, spliced onto the end of the page's own step list — which is why there is
    /// no second place for a row's wording to drift away from the canon pass.</summary>
    [Fact]
    public void THE_CHECKLIST_ROWS_AreTheEightLinesAndTheTrackCoversExactlyThem()
    {
        string[] steps = TutorialSteps();
        Pages.Map.TutorialTrack milkRun = TutorialTracks()[^1];

        Assert.Equal(MilkRunLesson.StepCount, milkRun.Length);
        Assert.Equal(steps.Length, milkRun.Start + milkRun.Length);
        for (int i = 0; i < MilkRunLesson.StepCount; i++)
        {
            Assert.Equal(MilkRunLesson.Lines[i], steps[milkRun.Start + i]);
        }

        Assert.Equal(MilkRunLesson.Title, milkRun.Title);
        Assert.Equal(MilkRunLesson.Blurb, milkRun.Blurb);
    }

    /// <summary>AND NOTHING ELSE IN THE LANE IS AUTHORED. The canon pass' own closing law — "implement
    /// verbatim; nothing else is authored" — swept over the two files this lesson is written in: every
    /// string literal in them, comments stripped, must be one of the eight, or one of the two short
    /// machine-facing names, and nothing else. This is the guard that catches the ninth line before it is
    /// a ninth line: an empty-state, a tooltip, a "well done".</summary>
    [Fact]
    public void THE_EIGHT_AreTheOnlyStringsTheLaneWrote()
    {
        string[] machineNames = [MilkRunLesson.QuestId, MilkRunLesson.BoardGiver];

        foreach (string file in new[] { PagesPath("Map.Quests.MilkRun.cs"), CorePath("MilkRunLesson.cs") })
        {
            foreach (string literal in StringLiteralsIn(file))
            {
                Assert.True(
                    MilkRunLesson.Lines.Contains(literal, StringComparer.Ordinal)
                    || machineNames.Contains(literal, StringComparer.Ordinal),
                    $"the lane authored a string the canon pass did not: \"{literal}\"");
            }
        }
    }

    /// <summary>…and neither of the two machine-facing names is new prose either: the giver is the board the
    /// game already posts work on, word for word, and the id is an id.</summary>
    [Fact]
    public void THE_BOARD_IsTheOneTheGameAlreadyPostsWorkOn()
    {
        Assert.Contains($"\"{MilkRunLesson.BoardGiver}\"", PagesFile("Map.Cycler.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain(' ', MilkRunLesson.QuestId);
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A page in Sol, clamped to a station berth with the captain walking its concourse — the one
    /// standing this lesson begins from, because a contract is taken at a board and a tank is filled at a
    /// pump and both of those are ashore.</summary>
    private static Pages.Map ACaptainAshoreAtABerth(string berth = HomeBerth)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris sky = TheSky();
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", sky);
        Set(map, "_simulator", new Simulator(sky, timeStepSeconds: 1.0));
        Set(map, "_ship", new ShipState(sky.Position(berth, 0), Vector2d.Zero, 0));
        Set(map, "_dockedHavenId", berth);
        Set(map, "_deckMode", true);
        Set(map, "_reactionMassPulses", 1);
        return map;
    }

    /// <summary>…with the lesson taken on and its card on the board, standing on step 1.</summary>
    private static Pages.Map ALessonTakenOn()
    {
        Pages.Map map = ACaptainAshoreAtABerth();
        Invoke(map, "SeedMilkRun");
        Invoke(map, "WatchTheMilkRun");   // the board lays the card out
        return map;
    }

    /// <summary>Put the world into the state step <paramref name="step"/> is about — and nothing else. Every
    /// one of these is the real thing the captain would have done, written as state rather than as a call
    /// into the lesson, which is the whole point of the exercise.</summary>
    private static void Finish(Pages.Map map, int step)
    {
        switch (step)
        {
            case 1:   // he presses Accept on the card the board laid out
                Invoke(map, "AcceptOffer");
                break;

            case 2:   // a plan that casts off from this berth and ends clamped at the delivery berth
                var plan = (List<Pages.Map.PlanNode>)Get<object>(map, "_planNodes");
                plan.Add(new Pages.Map.PlanNode { Kind = PlanStepKind.Undock, HavenId = HomeBerth, SimTime = 60 });
                plan.Add(new Pages.Map.PlanNode { Kind = PlanStepKind.ClearHarbour, HavenId = HomeBerth, SimTime = 120 });
                Set(map, "_arrive", new Pages.Map.ArriveStep
                {
                    BodyId = TheDeliveryBerth(map),
                    Kind = ArrivalStepRule.ArrivalKind.Dock,
                });
                break;

            case 3:   // ⛽ FILL HER UP
                Set(map, "_reactionMassPulses", Read<int>(map, "ReactionMassCapacity"));
                break;

            case 4:   // armed at plan time, with a rehearsal quote to read
                Set(map, "_armedOrbitBodyId", TheDeliveryBerth(map));
                Set(map, "_armedArrivalPassSimTime", (double?)9_000);
                Set(map, "_armedBudgetPulses", 14);
                break;

            case 5:   // the cast-off fired itself at its epoch: the clamp is off and its row is gone
                ((List<Pages.Map.PlanNode>)Get<object>(map, "_planNodes"))
                    .RemoveAll(n => n.Kind == PlanStepKind.Undock);
                Set(map, "_dockedHavenId", null);
                break;

            case 6:   // warp — the captain's own clock
                Set(map, "Paused", false);
                Set(map, "_effectiveWarp", 1000);
                break;

            case 7:   // clamped at the berth the contract names
                Set(map, "_dockedHavenId", TheDeliveryBerth(map));
                break;

            case 8:   // …and the counter pays it
                TheMilkRun(map).State = Pages.Map.QuestState.TurnedIn;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(step), step, "the milk run has eight steps");
        }
    }

    private static string TheDeliveryBerth(Pages.Map map) =>
        TheMilkRun(map).DestBodyId
        ?? throw new InvalidOperationException("the lesson's contract has no destination — this bench has drifted");

    private static Pages.Map.Quest TheMilkRun(Pages.Map map) =>
        ((List<Pages.Map.Quest>)Get<object>(map, "_quests")).FirstOrDefault(q => q.Id == MilkRunLesson.QuestId)
        ?? throw new InvalidOperationException("the milk run is not in the captain's hand — this bench has drifted");

    /// <summary>The checklist row the lesson is standing on, read the way the card reads it.</summary>
    private static string TheChecklistsCurrentRow(Pages.Map map) => TutorialSteps()[Get<int>(map, "_tutorialStep")];

    /// <summary>Clear the glass, so the next assertion is about what the lesson said and not about what is
    /// left over from the last thing that happened.</summary>
    private static void Hush(Pages.Map map) => Set(map, "_pulse", PulseSlot.Empty);

    /// <summary>A beat of real time — long enough that a line said now is not landing on top of the last.
    /// </summary>
    private static void WindTheClock(Pages.Map map) =>
        Set(map, "_frameNowMs", Get<double>(map, "_frameNowMs") + PulseSlot.MinDwellMs + 1);

    private static ICelestialEphemeris TheSky() => CircularOrbitEphemeris.FromScenario(Sol.Value);

    private static string[] TutorialSteps() => (string[])StaticField("TutorialSteps");

    private static Pages.Map.TutorialTrack[] TutorialTracks() =>
        (Pages.Map.TutorialTrack[])StaticField("TutorialTracks");

    private static object StaticField(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)?.GetValue(null)
        ?? throw new InvalidOperationException($"Map has no static {name} — this bench has drifted");

    /// <summary>Every double-quoted literal in a source file, with line and block comments removed first so
    /// the prose in a doc comment is not mistaken for prose in the game.</summary>
    private static IEnumerable<string> StringLiteralsIn(string path)
    {
        string source = File.ReadAllText(path);
        source = Regex.Replace(source, @"/\*.*?\*/", "", RegexOptions.Singleline);
        source = Regex.Replace(source, @"//[^\n]*", "");
        foreach (Match m in Regex.Matches(source, "\"((?:[^\"\\\\\\n]|\\\\.)*)\""))
        {
            yield return Regex.Unescape(m.Groups[1].Value);
        }
    }

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden)
            ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o)!;

    private static T Read<T>(object o, string property) =>
        (T)(o.GetType().GetProperty(property, Hidden)
            ?? throw new InvalidOperationException($"no property {property} on Map — this bench has drifted"))
        .GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);

    /// <summary>The shipped text of a client page file — read, not named, because a guard that asserts a
    /// needle is in a PATH is green for ever.</summary>
    private static string PagesFile(string name) => File.ReadAllText(PagesPath(name));

    private static string PagesPath(string name) =>
        Path.Combine(RepoDir("src", "SpaceSails.Client"), "Pages", name);

    private static string CorePath(string name) => Path.Combine(RepoDir("src", "SpaceSails.Core"), name);

    private static string ScenarioPath(string file) => Path.Combine(RepoDir("scenarios"), file);

    private static string RepoDir(params string[] parts)
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine([dir, .. parts]);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException(
            string.Join('/', parts) + " not found above " + AppContext.BaseDirectory);
    }
}
