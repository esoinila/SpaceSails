using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #638 · <b>THE VOID GETS ITS LANE.</b> <see cref="DeathCause.Void"/> has shipped a painting
/// (<c>art/death-void.jpg</c>), three lines of house prose (<c>VoidLines</c>), a headline and — since #636 — a
/// <see cref="DeathNarration.CanHappen"/> law of its own, and in two years no client code path has ever set
/// it. A cause with art and prose and no lane is the shape <c>QAHandoff-StoryTelling.md</c> criterion 1 names:
/// <i>a truth that lives only in Core constants is not being told.</i>
///
/// <para><b>The ruling (Fable, 2026-09-01), option 1 of the issue:</b> reaction mass at zero with no way home
/// IS the void death the prose already carries — the suffocation story one scale up, and the only candidate
/// that routes a shipped painting through machinery that already exists. No EVA scene; no orbit-decay overlap
/// with <see cref="DeathCause.Impact"/>.</para>
///
/// <para><b>The law.</b> She counts as ADRIFT when three things are true at once:
/// <list type="number">
///   <item>reaction mass is zero (and she is not clamped to a berth — a clamped ship is home);</item>
///   <item>no burn or arrival step of the current plan can still execute;</item>
///   <item>the current trajectory reaches no haven's capture.</item>
/// </list>
/// Twenty consecutive sim-days of that and the void has her.</para>
///
/// <para><b>No second reachability oracle.</b> Question 3 is answered by the machinery that already answers
/// it for the arrive row: the plotted course is swept by <see cref="ClosestApproach.Passes"/> and each pass
/// is judged by <see cref="ArrivalStepRule.Check"/> — the very ✓/✗ bit the plan's own ending stands on, with
/// its thresholds borrowed from <see cref="OrbitRule"/> and <see cref="DockRule"/>. This class adds the
/// question, never a second answer to it; <see cref="AHavenStillTakesHer"/> is a fold over that oracle and
/// nothing else, and it honours #1041's edge law (a pass sitting on the ribbon's own end is the end of the
/// picture, not an encounter).</para>
///
/// <para>Pure and deterministic — no ship, no ephemeris, no clock. The caller measures; this decides.</para>
/// </summary>
public static class VoidRule
{
    // ===== THE OWNER'S DIAL =====

    /// <summary>
    /// <b>How many consecutive sim-days adrift the void takes to close.</b> Twenty, per the ruling.
    ///
    /// <para><b>FLAGGED — this number is the owner's dial.</b> It is the whole tuning surface of the lane:
    /// everything else (the three tellings, the sweep's look-ahead, the death itself) is derived from it or
    /// pinned to it, so moving this one integer moves the entire feature and nothing else has to be touched.
    /// It is deliberately long: an adrift captain has a rescue tug one press away (#266), and the void is what
    /// happens to somebody who spends three weeks not pressing it.</para>
    /// </summary>
    public const int DaysAdrift = 20;

    /// <summary>The whole sim-day, in seconds — the same period <see cref="DiscoveryRule.PeriodSeconds"/>
    /// counts in, because a day is a day and the game may only have one of them.</summary>
    public const double DaySeconds = DiscoveryRule.PeriodSeconds;

    /// <summary>
    /// How far ahead the haven sweep looks: exactly the countdown, so the question the picture is asked is the
    /// question the clock is asking — <i>is there a berth in the time you have left?</i>
    ///
    /// <para>Borrowed from <see cref="DaysAdrift"/> rather than picked, and the consequence is worth writing
    /// down: the window SLIDES. A capture nineteen days out cancels the clock on the day it is declared; one
    /// twenty-five days out cancels it on day five, before the twenty are up. A capture further out than the
    /// captain's own remaining twenty days is not a rescue he lives to see, and the law does not pretend
    /// otherwise.</para>
    /// </summary>
    public static double LookAheadSeconds => DaysAdrift * DaySeconds;

    // ===== THE PREDICATE =====

    /// <summary>
    /// One haven pass, measured off the plotted course, ready to be judged by the arrive row's own oracle.
    /// Everything in it is a measurement the caller already takes for the arrive step
    /// (<c>Map.Plot.Arrive.CheckArrival</c>): the pass distance from <see cref="ClosestApproach.Passes"/>, the
    /// relative speed from the ribbon's own velocity minus the body's, and the body's Hill radius.
    /// </summary>
    /// <param name="BodyId">The body this pass belongs to.</param>
    /// <param name="BodyName">Its name, for the check's own sentence.</param>
    /// <param name="Kind">How this haven would take her — an orbit insertion or a ⚓ clamp.</param>
    /// <param name="Distance">Closest-approach distance on the plotted course (m).</param>
    /// <param name="RelSpeed">Ship speed relative to the body at that pass (m/s).</param>
    /// <param name="HillRadius">The body's Hill radius (m); ignored for a dock.</param>
    /// <param name="PassSimTime">When the pass happens.</param>
    public readonly record struct HavenPass(
        string BodyId,
        string BodyName,
        ArrivalStepRule.ArrivalKind Kind,
        double Distance,
        double RelSpeed,
        double HillRadius,
        double PassSimTime);

    /// <summary>
    /// <b>Does any plotted future touch a berth?</b> True when at least one swept haven pass would be a valid
    /// arrival — <see cref="ArrivalStepRule.ArrivalCheck.Valid"/>, the same bit the plan's own ending wears.
    ///
    /// <para>A pass sitting on the ribbon's own end is skipped, not counted: #1041's law says that reading is
    /// where the picture stopped rather than where the ship comes nearest, and a fabricated ✓ would be exactly
    /// as bad as the fabricated ✗ that law was written to stop.</para>
    /// </summary>
    /// <param name="passes">Every haven pass on the plotted course.</param>
    /// <param name="ribbonEndSimTime">The epoch of the projection's last sample.</param>
    /// <param name="sampleStepSeconds">The projection's own spacing at that end.</param>
    public static bool AHavenStillTakesHer(
        IReadOnlyList<HavenPass> passes, double ribbonEndSimTime, double sampleStepSeconds)
    {
        ArgumentNullException.ThrowIfNull(passes);
        foreach (HavenPass pass in passes)
        {
            if (ArrivalStepRule.PassIsOffTheEndOfTheRibbon(pass.PassSimTime, ribbonEndSimTime, sampleStepSeconds))
            {
                continue;
            }

            if (ArrivalStepRule.Check(pass.Kind, pass.BodyName, pass.Distance, pass.RelSpeed, pass.HillRadius).Valid)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <b>Is she ADRIFT — the void's own sense of the word?</b> All four arms, and every one of them is a fact
    /// the client already holds.
    ///
    /// <para>Note this is STRICTLY stronger than the ship's existing dry-tank <c>Adrift</c> flag (which raises
    /// the #266 rescue offer the moment the tank hits zero, and is right to). A captain with an empty tank and
    /// a berth in front of him is stranded, not doomed; a captain with an empty tank, a plan that can no
    /// longer fire and a course that touches nothing is the one this clock is for.</para>
    /// </summary>
    /// <param name="reactionMassPulses">Pulses in the tank. Zero — the exact-integer sense the ship's own
    /// <c>Adrift</c> uses — is the first arm.</param>
    /// <param name="docked">Clamped to a berth. A clamped ship is home, whatever the tank says.</param>
    /// <param name="aPlanStepCanStillFire">Any non-stale, unexecuted, still-future step of the current plan —
    /// a plotted burn or an armed arrival. While the plan has a move left in it, she is flying, not drifting.</param>
    /// <param name="aHavenStillTakesHer"><see cref="AHavenStillTakesHer"/> over the plotted course.</param>
    public static bool IsAdrift(
        int reactionMassPulses, bool docked, bool aPlanStepCanStillFire, bool aHavenStillTakesHer) =>
        reactionMassPulses <= 0 && !docked && !aPlanStepCanStillFire && !aHavenStillTakesHer;

    // ===== THE CLOCK =====

    /// <summary>The sentinel <see cref="DeclaredDay"/> carries while no clock is running — and the value every
    /// vault written before #638 loads with, so an old voyage resumes with nothing counting down.</summary>
    public const long ClockNotRunning = -1;

    /// <summary>The sentinel the "last day the captain was told about" carries before anything has been said.
    /// Deliberately below day 0, because day 0 IS a telling.</summary>
    public const int NothingToldYet = -1;

    /// <summary>Whole sim-days since the epoch — the same day index the hoard's discovery watch counts in
    /// (<see cref="DiscoveryRule.PeriodIndex"/>), so the game has one notion of "a day has rolled over".</summary>
    public static long DayIndex(double simTime) => DiscoveryRule.PeriodIndex(simTime);

    /// <summary>How many whole days the current adrift run has lasted. Never negative: a clock stamped in the
    /// future (a loaded save, a scenario clock wound back) reads zero rather than counting backwards.</summary>
    public static int DaysElapsed(long declaredDay, double simTime) =>
        declaredDay == ClockNotRunning ? 0 : (int)Math.Max(0, DayIndex(simTime) - declaredDay);

    /// <summary>The twenty are up.</summary>
    public static bool TimeIsUp(long declaredDay, double simTime) =>
        declaredDay != ClockNotRunning && DaysElapsed(declaredDay, simTime) >= DaysAdrift;

    // ===== THE TELLINGS (#761 — the player is told clearly, thrice, in escalating register) =====
    //
    // Authored in the ruling and reproduced here verbatim. They live as constants for the reason every other
    // sentence in this repo does: a line the panel types and a line a test types are two lines, and this
    // project has paid four times for a sentence that disagreed with the sim it describes.

    /// <summary>Day 0 — the declaration, said on the banner AND in the log.</summary>
    public const string DeclaredLine = "Reaction mass is spent. Nothing answers the helm.";

    /// <summary>Day 10 — the halfway banner.</summary>
    public const string HalfwayLine = "Half the ledger gone. The sail holds its line; the line goes nowhere.";

    /// <summary>Day 19 — the story pop-up card, in the authority-card idiom (#684's precedent: an outcome is
    /// told on a card, not muttered into a banner behind a backdrop).</summary>
    public const string OneDayLeftLine = "The long dark has a schedule now. One day remains on it.";

    /// <summary>The title the day-19 card wears. The card carries no picture on purpose — the painting is the
    /// death's, and showing it a day early would spend the one image this whole lane exists to reach.</summary>
    public const string OneDayLeftCardLabel = "🕯 THE LONG DARK";

    /// <summary>The day the halfway banner falls on.</summary>
    public const int HalfwayDay = DaysAdrift / 2;

    /// <summary>The day the card falls on — the last whole day there is.</summary>
    public const int OneDayLeftDay = DaysAdrift - 1;

    /// <summary>The three days something is said on, in order. Derived from the dial: move
    /// <see cref="DaysAdrift"/> and all three move with it.</summary>
    public static IReadOnlyList<int> TellingDays { get; } = [0, HalfwayDay, OneDayLeftDay];

    /// <summary>What is said on a telling day, or null on a day with nothing to say.</summary>
    public static string? Telling(int day) => day == 0
        ? DeclaredLine
        : day == HalfwayDay
            ? HalfwayLine
            : day == OneDayLeftDay ? OneDayLeftLine : null;

    /// <summary>
    /// <b>Every telling a span of days crosses</b> — the ones in <c>(lastToldDay, daysNow]</c>, in order.
    ///
    /// <para>A span rather than an equality test because warp exists: at 10,000× a single frame can carry the
    /// ship over a fortnight, and a beat that fires only on <c>days == 10</c> is a beat the captain never sees.
    /// Same skip-proofing, and the same shape, as <see cref="DiscoveryRule.DiscoveredWithin"/>.</para>
    /// </summary>
    public static IReadOnlyList<int> TellingsBetween(int lastToldDay, int daysNow)
    {
        var due = new List<int>();
        foreach (int day in TellingDays)
        {
            if (day > lastToldDay && day <= daysNow)
            {
                due.Add(day);
            }
        }

        return due;
    }
}
