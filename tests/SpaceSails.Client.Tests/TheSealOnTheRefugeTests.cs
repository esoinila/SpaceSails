using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #608 · THE ROOM IS A FACT AND THE SEAL IS A STORY — what the suit, the plate and the tracker do with a
/// pressure refuge that has spent decades with nobody paying for it.
///
/// <para>Owner, filing the refuges: <i>"their state after decades is the story. The ones that still hold are
/// the ones somebody maintained; the ones that do not are the ones somebody stopped being paid to"</i> — and
/// the warning that makes this worth building at all: <i>"If every ADMINISTRATION floor is safe, deep
/// ADMINISTRATION floors stop costing anything. The state of the seal is what keeps it honest."</i></para>
///
/// <para>Core decides the seal (<c>UndergroundComplex.StateOfTheRefugeOn</c>, guarded in
/// <c>TheRefugesUndergroundTests</c>). What is asserted HERE is the half a Core test cannot reach: that the
/// suit spends what the seal says it spends, that the plate over the door stops saying AIR when there is
/// none, and that the tracker keeps painting a dead refuge and paints it as dead — the owner's three
/// requirements for the fan, in his own order.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheSealOnTheRefugeTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The scenario's own moons. One floor per seal is enough to drive a suit — the LAW about which
    /// floors get which seal is swept over a hundred sites in Core — but it must be a floor the generator
    /// really produced, never a hand-typed one, or this bench is a world that cannot tell pass from fail.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto", "titan", "miranda", "triton",
    ];

    /// <summary>A real floor of a real site whose refuge is in the state asked for.</summary>
    private static (string Body, int Level) AFloorWhoseRefugeIs(UndergroundComplex.RefugeState state)
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.StateOfTheRefugeOn(body, level) == state)
                {
                    return (body, level);
                }
            }
        }

        throw new InvalidOperationException(
            $"no site in the scenario has a {state} refuge on any floor — this bench is auditing a world "
            + "that cannot tell pass from fail.");
    }

    // ── (a) THE PLATE OVER THE DOOR ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThePlateStopsSayingAirWhenThereIsNone()
    {
        // #612's law, pointed at the one door on a dead floor a captain would spend a tank reaching: two
        // instruments may never disagree about whether you can breathe. A room whose seal went, drawn in the
        // relief green under the word AIR, is that disagreement in its most expensive form.
        foreach (UndergroundComplex.RefugeState state in Enum.GetValues<UndergroundComplex.RefugeState>())
        {
            (string body, int level) = AFloorWhoseRefugeIs(state);
            DeckPlan plan = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

            var plates = plan.BigLabels
                .Where(l => l.Text.Contains("REFUGE", StringComparison.Ordinal))
                .ToList();
            Assert.True(plates.Count == 1,
                $"{body} B{-level} ({state}): {plates.Count} refuge plate(s) drawn, expected one.");

            bool holds = state != UndergroundComplex.RefugeState.Failed;

            // #938 · THREE STATES, THREE PLATES — and it used to be two. This assertion was written as
            // `holds ? RefugeGlyph : RefugeFailedGlyph`, so an EMPTY refuge — thirty-nine per cent of them —
            // wore REFUGE · AIR over a rack whose fill line reads empty and whose valve tag is dated years
            // ago. The bench agreed with the bug because it asked the same two-way question the code did.
            // The expected plates are LITERAL here for that reason: an oracle that reads RefugeGlyphFor is
            // an oracle that cannot disagree with it.
            string expected = state switch
            {
                UndergroundComplex.RefugeState.Holding => "🫁 REFUGE · AIR",
                UndergroundComplex.RefugeState.Empty => "🫁 REFUGE · DRY",
                _ => "🫁 PRESSURE REFUGE",
            };
            Assert.Equal(expected, plates[0].Text);

            // Tone is what the sign MEANS: 1 = you can breathe here, 2 = you cannot and your tank is
            // running. It is the same ink the plate by the lift is already using about the floor.
            Assert.Equal(holds ? 1 : 2, plates[0].Tone);

            // …and the console's caption stops advertising a rack behind a door that will not cycle.
            DeckPlan.ConsoleSpot rack = plan.Consoles
                .Single(c => c.Kind == DeckPlan.ConsoleKind.HiveRefuge);
            Assert.Equal(
                holds ? UndergroundComplex.RefugeTankLabel : UndergroundComplex.RefugeFailedGlyph,
                rack.Label);
        }
    }

    /// <summary>
    /// #938 · THE DRY PLATE, AND WHAT IT MAY NOT SAY. #608 shipped with one <c>// FABLE: line needed</c>
    /// marker on this family and a two-way <c>RefugeGlyphFor</c>, so the state between holding and failed —
    /// a room that holds and has nothing in it — was signed as though the rack were full. The plate is
    /// Fable's, authored on #608 (2026-09-03), in the stencil the other two already speak.
    ///
    /// <para>The half that can tell pass from fail is what it must NOT be: it may not carry AIR, which is
    /// the exact word the empty rack cannot honour and the reason this bug existed; it may not be either of
    /// the other two plates, because a state that shares a plate is a state the captain cannot read at
    /// range; and it may not name the reserved word (worldbuilding-notes §8). The marker itself has to be
    /// gone from the source, or the next reconciliation finds it again and files the same defect.</para>
    /// </summary>
    [Fact]
    public void TheDryPlateIsTheAuthoredStencilAndPromisesNoAir()
    {
        Assert.Equal("🫁 REFUGE · DRY", UndergroundComplex.RefugeDryGlyph);
        Assert.Equal(UndergroundComplex.RefugeDryGlyph,
                     UndergroundComplex.RefugeGlyphFor(UndergroundComplex.RefugeState.Empty));

        // Every state wears its own plate — the two-way test could only ever say which one it was NOT.
        var plates = Enum.GetValues<UndergroundComplex.RefugeState>()
                         .Select(UndergroundComplex.RefugeGlyphFor)
                         .ToList();
        Assert.Equal(plates.Count, plates.Distinct(StringComparer.Ordinal).Count());

        // It keeps the scope of the claim (#612) and drops the claim itself.
        Assert.StartsWith("🫁 REFUGE ·", UndergroundComplex.RefugeDryGlyph, StringComparison.Ordinal);
        foreach (string forbidden in new[] { "AIR", "monolith" })
        {
            Assert.DoesNotContain(forbidden, UndergroundComplex.RefugeDryGlyph, StringComparison.OrdinalIgnoreCase);
        }

        // …and the marker that asked for it is off the source, so the backlog stops re-finding it.
        string air = System.IO.File.ReadAllText(System.IO.Path.Combine(
            CoreRoot, "UndergroundComplex.Air.cs"));
        Assert.Contains("RefugeDryGlyph", air, StringComparison.Ordinal);
        Assert.DoesNotContain("FABLE: line needed", air, StringComparison.Ordinal);
    }

    /// <summary>Where <c>UndergroundComplex.Air.cs</c> lives, from the test binary.</summary>
    private static string CoreRoot
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                string core = System.IO.Path.Combine(dir, "src", "SpaceSails.Core");
                if (System.IO.Directory.Exists(core))
                {
                    return core;
                }
                dir = System.IO.Path.GetDirectoryName(dir);
            }
            throw new System.IO.DirectoryNotFoundException("Could not find src/SpaceSails.Core above the test assembly.");
        }
    }

    // ── (b) THE SUIT ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnlyAMaintainedRackPutsAnythingBackInTheTank()
    {
        // The issue's own "Done when": "its state is part of the story (holds / holds but empty / failed),
        // and a working one can refill a tank". Three floors, one stepper, three different outcomes — and
        // the empty one is the interesting middle: the drain STOPS (it is a room you can wait out a busy
        // lift in, which is the owner's whole reason for the feature) and nothing is put back.
        const double Dt = 1.0;
        const double Start = 600.0;

        foreach (UndergroundComplex.RefugeState state in Enum.GetValues<UndergroundComplex.RefugeState>())
        {
            (string body, int level) = AFloorWhoseRefugeIs(state);
            Pages.Map map = StandingInTheRefugeOn(body, level);
            object ex = Get(map, "_surface")!;

            SetOn(ex, "AirSeconds", Start);
            for (int frame = 0; frame < 30; frame++)
            {
                Invoke(map, "StepSuitAir", Dt);
            }
            double air = (double)GetOn(ex, "AirSeconds")!;

            switch (state)
            {
                case UndergroundComplex.RefugeState.Holding:
                    Assert.True(air > Start + 10,
                        $"{body} B{-level}: the rack still has bottles in it and thirty seconds in the room "
                        + $"put back {air - Start:F1} s. A working refuge is the only thing under a moon "
                        + "that refills a tank.");
                    break;

                case UndergroundComplex.RefugeState.Empty:
                    Assert.True(Math.Abs(air - Start) < 1e-6,
                        $"{body} B{-level}: an empty refuge moved the tank by {air - Start:F1} s. It holds "
                        + "pressure and it has nothing to give — the drain stops and the gauge does not "
                        + "climb.");
                    break;

                default:
                    Assert.True(air < Start - 10,
                        $"{body} B{-level}: standing in a refuge whose seal went cost {Start - air:F1} s. "
                        + "A room that will not cycle is a room on a dead floor, and the tank knows it even "
                        + "if the plan does not.");
                    break;
            }

            // …and the GAUGE agrees with the drain, which is #612's whole law: the captain is never told
            // ROOM by an instrument while the sim is spending tank, nor TANKS while it is not.
            var supply = (SuitAir.Supply)Invoke(map, "AirSupplyOf", ex)!;
            Assert.Equal(
                state == UndergroundComplex.RefugeState.Failed
                    ? SuitAir.Supply.Tanks
                    : SuitAir.Supply.Room,
                supply);
        }
    }

    [Fact]
    public void AWorkingRackIsSpentAsItFillsYou()
    {
        // #573's law, underground: the rack is a reservoir and not a tap. It is what stops a working refuge
        // from being a floor that costs nothing — the captain buys RANGE, and the bottles are finite.
        (string body, int level) = AFloorWhoseRefugeIs(UndergroundComplex.RefugeState.Holding);
        Pages.Map map = StandingInTheRefugeOn(body, level);
        object ex = Get(map, "_surface")!;

        SetOn(ex, "AirSeconds", 300.0);
        double before = (double)Invoke(map, "RefugeReservoirNow", ex, 0)!;
        Assert.True(before > 0, "the working refuge started with an empty rack — nothing can be proved here.");

        for (int frame = 0; frame < 60; frame++)
        {
            Invoke(map, "StepSuitAir", 1.0);
        }

        double after = (double)Invoke(map, "RefugeReservoirNow", ex, 0)!;
        Assert.True(after < before,
            $"{body} B{-level}: the rack held {before:F0} s before the fill and {after:F0} s after it. A "
            + "reservoir that does not go down is a tap, and a tap makes the floor free.");
    }

    // ── (c) THE TRACKER ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ADeadRefugeStillPaintsAndPaintsAsDead()
    {
        // Owner, on the fan, in one sentence with both halves load-bearing: "A refuge whose seal has failed
        // must still paint, and must read as failed. Walking to one and finding it dead is a real beat;
        // walking to one that was never marked is just a bad map."
        foreach (UndergroundComplex.RefugeState state in Enum.GetValues<UndergroundComplex.RefugeState>())
        {
            (string body, int level) = AFloorWhoseRefugeIs(state);
            Pages.Map map = StandingInTheRefugeOn(body, level);
            object ex = Get(map, "_surface")!;

            var beacons =
                (List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)>)Invoke(
                    map, "BuildBeacons", ex)!;

            // The lift cars carry HOME down here (#591/#801); everything else on this fan is the refuge.
            var refuges = beacons.FindAll(b => !b.IsHome && !b.IsLab);
            Assert.True(refuges.Count == 1,
                $"{body} B{-level} ({state}): the fan paints {refuges.Count} refuge ring(s), expected one — "
                + "a refuge that stops painting when its seal goes is the bad map the owner filed against.");

            Assert.Equal(state == UndergroundComplex.RefugeState.Failed, refuges[0].IsDead);

            // …and the cars are still on it, so this fan is the underground fan and not an empty list that
            // would have agreed with any assertion above.
            Assert.True(beacons.Exists(b => b.IsHome),
                "no way-home ring on an underground fan — this bench is reading the wrong instrument.");
        }
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component standing INSIDE the refuge on a real floor of a real site. The room is the
    /// generator's own (<c>HiveInterior.FloorDeck</c> put the console there off the floor plan), never a
    /// coordinate this test picked — a guard handed a world it typed itself cannot tell pass from fail.</summary>
    private static Pages.Map StandingInTheRefugeOn(string body, int level)
    {
        var map = new Pages.Map();

        // The framework's own render early-out, so the verbs that end in StateHasChanged are silent no-ops
        // rather than throwing off a bench with no renderer — the same theatre MustStandUpBeforeWalkingTests
        // and TheStallSaysSoTests ride on.
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, level);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Invoke(map, "RebuildSurfaceDeck");

        var plan = (DeckPlan)Get(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot rack = plan.Consoles
            .Single(c => c.Kind == DeckPlan.ConsoleKind.HiveRefuge);
        Set(map, "_avatarX", (double)rack.X);
        Set(map, "_avatarY", (double)rack.Y);

        Assert.True((int)Invoke(map, "RefugeUnderfoot", ex)! >= 0,
            $"{body} B{-level}: the captain was put on the refuge's own console and the containment law says "
            + "they are not in it — this bench is standing somewhere else.");
        return map;
    }

    private static object? Get(object o, string member) =>
        o.GetType().GetField(member, Hidden)?.GetValue(o)
        ?? (o.GetType().GetProperty(member, Hidden)
            ?? throw new InvalidOperationException($"the component has no `{member}`.")).GetValue(o);

    private static object? GetOn(object o, string member) =>
        (o.GetType().GetProperty(member, Hidden)
         ?? throw new InvalidOperationException($"no `{member}`.")).GetValue(o);

    private static void SetOn(object o, string member, object? value) =>
        (o.GetType().GetProperty(member, Hidden)
         ?? throw new InvalidOperationException($"no `{member}`.")).SetValue(o, value);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(map, args);
    }
}
