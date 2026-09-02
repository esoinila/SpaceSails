using System;
using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #351 · <b>THE SOFT-CATCH CHECKLIST WAS TALKING ABOUT SHIPS THAT WERE NOT THERE.</b>
///
/// <para>Owner, 2026-07-18, six sim-days into the voyage: <i>"It showed me the tutorial soft catch window
/// here even though all the targets it talks about are long gone. I closed it though. A schedule based
/// tutorial only works at certain time. It is kind of a bad design like this. <b>The tutorial selection
/// should trigger the launch of the target vehicles.</b>"</i></para>
///
/// <h3>The half that was already answered</h3>
/// <para>Ruling-2, the same day, took the first hunt's pod off the boot clock: she is no longer cast at
/// world-load off a T=0 Earth position (the note is in <c>Map.Sim.World.Build.PlanTheTrafficAsync</c>),
/// she is launched when the lesson is TAKEN ON — <c>MaybeGreetTutorialHome</c> and the Captain's-tab
/// <c>StartTutorial</c> both call <c>SeedFirstHuntTarget</c>, which places her abeam wherever the ship
/// actually is then. <see cref="THE_SELECTION_LaunchesHerAbeamTheShipNowAndNotOffAT0Clock"/> pins that
/// sentence so it cannot quietly go back to a schedule.</para>
///
/// <h3>The half that was still open, and is what the screenshot shows</h3>
/// <para>A launch is a MOMENT; the checklist is a thing that stays up. Between the two, the world can take
/// the prey away outright: <c>ReseedWorldForJump</c> (Map.LongHaul) drops every non-depot mover on a long
/// haul, a cycler crossing and a vault resume; <c>StepNpcs</c> retires one the clock has left an epoch
/// behind; the pod's own 60-day expiry despawns her at her destination. None of that told the checklist,
/// which went on naming a Sitting Duck that was nowhere in the world — the owner's sentence exactly. So
/// the lesson now keeps its own prey in the world while it still needs her: relaunched, abeam the ship
/// NOW, at the checklist's own door (<c>ToggleTutorial</c>) and once a sim-hour from the sensor sweep, for
/// a window left open across the jump.</para>
///
/// <para><b>Red proof (run before shipping).</b> Delete the <c>RelaunchTheLessonsPreyIfSheIsGone()</c> call
/// from <c>Map.Quests.ToggleTutorial</c> and the reopen test goes red with the pod nowhere in the world;
/// delete the <c>KeepTheLessonsPreyInTheWorld()</c> call from <c>Map.Npc.SweepSensors</c> — or the
/// method's body — and the sweep test goes red the same way. Both were red on the base commit.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheLessonLaunchesItsOwnTargetsTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private const double Day = 86_400.0;

    // Where she was when the lesson was taken on, and where she is six days and one long haul later.
    private static readonly Vector2d WhereSheWas = new(1.5e11, 0);
    private static readonly Vector2d WhereSheIsNow = new(0, 2.3e11);

    // -- (a) THE RULING'S OWN SENTENCE ----------------------------------------------------------------

    /// <summary>THE SELECTION LAUNCHES THE TARGET VEHICLE — abeam the ship NOW, on the ship's own clock.
    /// This is Ruling-2's half, pinned: a pod declared at SimTime 0 while the captain is six days and half
    /// an AU away is the orphan the owner was looking at.</summary>
    [Fact]
    public void THE_SELECTION_LaunchesHerAbeamTheShipNowAndNotOffAT0Clock()
    {
        Pages.Map map = ALessonInProgress();
        TheShipIsHereNow(map, WhereSheIsNow, 6 * Day);

        Invoke(map, "SeedFirstHuntTarget");

        NpcShip pod = ThePod(map);
        Assert.Equal(6 * Day, pod.InitialState.SimTime);
        Assert.Equal(TrafficSchedule.StarterPodStandoffMeters,
            (pod.InitialState.Position - WhereSheIsNow).Length, 3);
    }

    // -- (b) THE SCREENSHOT: THE WINDOW OUTLIVED THE TARGET -------------------------------------------

    /// <summary>THE JUMP TAKES HER, AND REOPENING THE LESSON LAUNCHES HER AGAIN. The long haul's own
    /// re-seed drops every mover that belonged to the world we left — the lesson's pod with them — and the
    /// checklist went on naming her. Raising the checklist is the captain's selection; it launches her.
    /// </summary>
    [Fact]
    public void REOPENING_THE_CHECKLIST_AfterAJumpTookHer_LaunchesHerAgainAbeamTheShip()
    {
        Pages.Map map = ALessonInProgress();
        Invoke(map, "SeedFirstHuntTarget");
        Assert.NotNull(ThePodOrNull(map));

        TheLongHaulCrossesTheVoid(map);
        Assert.Null(ThePodOrNull(map));   // the world took her — the standing the owner's window was in

        Invoke(map, "ToggleTutorial");    // he closed it...
        Invoke(map, "ToggleTutorial");    // ...and opened it again

        NpcShip pod = ThePod(map);
        Assert.Equal(6 * Day, pod.InitialState.SimTime);
        Assert.Equal(TrafficSchedule.StarterPodStandoffMeters,
            (pod.InitialState.Position - WhereSheIsNow).Length, 3);
    }

    /// <summary>AND A WINDOW LEFT OPEN ACROSS THE JUMP DOES NOT HAVE TO BE TOGGLED. The sweep that already
    /// keeps the sky from emptying keeps the lesson's prey in it too.</summary>
    [Fact]
    public void THE_SWEEP_WithTheChecklistStillUp_PutsHerBackWithoutAnyGesture()
    {
        Pages.Map map = ALessonInProgress();
        Invoke(map, "SeedFirstHuntTarget");
        TheLongHaulCrossesTheVoid(map);

        Invoke(map, "KeepTheLessonsPreyInTheWorld");

        Assert.NotNull(ThePodOrNull(map));
    }

    /// <summary>...AND ONLY WITH THE CHECKLIST UP. <c>_tutorialStep</c> rests at 0 for every captain who
    /// never took a lesson (it is not vaulted), so a keeper that read the step alone would have hung a
    /// Sitting Duck off the beam of every ship in the game. The window being raised is what says a lesson
    /// is running.</summary>
    [Fact]
    public void THE_SWEEP_WithNoChecklistUp_LaunchesNothingAtACaptainWhoNeverTookTheLesson()
    {
        Pages.Map map = ALessonInProgress();
        Set(map, "_showTutorial", false);

        Invoke(map, "KeepTheLessonsPreyInTheWorld");

        Assert.Null(ThePodOrNull(map));
    }

    /// <summary>AND A PREY THAT IS STILL OUT THERE IS LEFT STRICTLY ALONE. Relaunching on every reopen
    /// would yank a plotted intercept out from under the captain mid-hunt — the checklist is a thing you
    /// close and open while you work.</summary>
    [Fact]
    public void REOPENING_THE_CHECKLIST_WithHerStillOutThere_DoesNotMoveHer()
    {
        Pages.Map map = ALessonInProgress();
        Invoke(map, "SeedFirstHuntTarget");
        Vector2d launched = ThePod(map).InitialState.Position;

        TheShipIsHereNow(map, WhereSheIsNow, 6 * Day);   // the captain has flown a long way at her
        Invoke(map, "ToggleTutorial");
        Invoke(map, "ToggleTutorial");

        Assert.Equal(launched.X, ThePod(map).InitialState.Position.X, 3);
        Assert.Equal(launched.Y, ThePod(map).InitialState.Position.Y, 3);
    }

    /// <summary>AND ONCE SHE IS BOARDED AND THE LESSON HAS MOVED ON TO THE MARKET, NOTHING IS LAUNCHED.
    /// The last two steps of the soft catch are the sell and the spend, with the catch already in the
    /// hold — a fresh pod appearing abeam then would be the schedule bug wearing the other face.</summary>
    [Fact]
    public void ONCE_THE_CATCH_IS_IN_THE_HOLD_NothingIsLaunchedAtTheMarketSteps()
    {
        Pages.Map map = ALessonInProgress();
        Set(map, "_tutorialStep", 4);   // "Dock at a station's market and sell"

        Invoke(map, "RelaunchTheLessonsPreyIfSheIsGone");

        Assert.Null(ThePodOrNull(map));
    }

    // -- The bench ------------------------------------------------------------------------------------

    /// <summary>A captain who has taken the soft catch on and has its checklist up, in a Sol world.</summary>
    private static Pages.Map ALessonInProgress()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);

        Set(map, "_scenarioName", "Sol");
        Set(map, "_showTutorial", true);
        Set(map, "_tutorialStep", 0);
        TheShipIsHereNow(map, WhereSheWas, 0);
        return map;
    }

    private static void TheShipIsHereNow(Pages.Map map, Vector2d at, double simTime)
    {
        Set(map, "_ship", new ShipState(at, new Vector2d(0, 2.9e4), simTime));
        Set(map, "SimTime", simTime);
    }

    /// <summary>The long haul's OWN re-seed — the shipping method, not a hand-emptied roster — followed by
    /// the ship arriving where and when it arrives. This is the order <c>ConsumeCoastClosedForm</c> uses.
    /// </summary>
    private static void TheLongHaulCrossesTheVoid(Pages.Map map)
    {
        Invoke(map, "ReseedWorldForJump", 6 * Day);
        TheShipIsHereNow(map, WhereSheIsNow, 6 * Day);
    }

    private static NpcShip ThePod(Pages.Map map) =>
        ThePodOrNull(map) ?? throw new InvalidOperationException(
            "the soft catch's Sitting Duck is nowhere in the world — the checklist is naming a ship that is not there.");

    private static NpcShip? ThePodOrNull(Pages.Map map)
    {
        foreach (object npc in (IEnumerable)Get<object>(map, "_npcStates"))
        {
            var ship = (NpcShip)npc.GetType().GetField("Ship")!.GetValue(npc)!;
            if (ship.Id == TrafficSchedule.StarterPodId)
            {
                return ship;
            }
        }

        return null;
    }

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden)
            ?? throw new InvalidOperationException($"no field {field} on {o.GetType().Name} — this bench has drifted"))
            .GetValue(o)!;

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on {o.GetType().Name} — this bench has drifted"))
        .SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on {o.GetType().Name} — this bench has drifted"))
        .Invoke(o, args);
}
