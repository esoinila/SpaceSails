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
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The scenario's own moons. One floor per seal is enough to drive a suit — the LAW about which
    /// floors get which seal is swept over a hundred sites in Core — but it must be a floor the generator
    /// really produced, never a hand-typed one, or this bench is a world that cannot tell pass from fail.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto", "titan", "miranda", "triton",
    ];

    /// <summary>A real floor of a real site whose refuge is in the state asked for.
    ///
    /// <para>#619 · <see cref="UndergroundComplex.RefugeState.Failed"/> is no longer one of the answers this
    /// can give, and that is the law: the FLOOR's refuge holds or is dry, and the room that failed is a
    /// second chamber. Ask <see cref="AFloorWithTheWeldedRefuge"/> for that one.</para></summary>
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

    /// <summary>#619 · A real floor of a real site that carries the one refuge that failed — welded shut,
    /// standing beside a working one. It throws rather than skipping when the scenario has none, because a
    /// bench that quietly tests nothing is the shape this repository has a name for.</summary>
    private static (string Body, int Level) AFloorWithTheWeldedRefuge()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.RefugeThatFailedIsOn(body, level))
                {
                    return (body, level);
                }
            }
        }

        throw new InvalidOperationException(
            "no site in the scenario carries the refuge that failed — every assertion about it below is "
            + "vacuously true, and this bench cannot tell pass from fail.");
    }

    /// <summary>#619 · The two states the FLOOR's own refuge can be in. Walked instead of
    /// <c>Enum.GetValues</c> so that adding a fourth state does not silently widen a sweep that is about
    /// the rooms a captain can walk into.</summary>
    private static readonly UndergroundComplex.RefugeState[] StatesARoomYouCanEnterHas =
    [
        UndergroundComplex.RefugeState.Holding,
        UndergroundComplex.RefugeState.Empty,
    ];

    // ── (a) THE PLATE OVER THE DOOR ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThePlateStopsSayingAirWhenThereIsNone()
    {
        // #612's law, pointed at the one door on a dead floor a captain would spend a tank reaching: two
        // instruments may never disagree about whether you can breathe. A room whose seal went, drawn in the
        // relief green under the word AIR, is that disagreement in its most expensive form.
        foreach (UndergroundComplex.RefugeState state in StatesARoomYouCanEnterHas)
        {
            (string body, int level) = AFloorWhoseRefugeIs(state);
            Assert.False(UndergroundComplex.RefugeThatFailedIsOn(body, level),
                $"{body} B{-level}: this bench wants an ORDINARY floor and drew the one with the story.");
            DeckPlan plan = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

            var plates = plan.BigLabels
                .Where(l => l.Text.Contains("REFUGE", StringComparison.Ordinal))
                .ToList();
            Assert.True(plates.Count == 1,
                $"{body} B{-level} ({state}): {plates.Count} refuge plate(s) drawn, expected one.");

            // #938 · THREE STATES, THREE PLATES — and it used to be two. This assertion was written as
            // `holds ? RefugeGlyph : RefugeFailedGlyph`, so an EMPTY refuge — thirty-nine per cent of them —
            // wore REFUGE · AIR over a rack whose fill line reads empty and whose valve tag is dated years
            // ago. The bench agreed with the bug because it asked the same two-way question the code did.
            // The expected plates are LITERAL here for that reason: an oracle that reads RefugeGlyphFor is
            // an oracle that cannot disagree with it.
            string expected = state == UndergroundComplex.RefugeState.Empty
                ? "🫁 REFUGE · DRY"
                : "🫁 REFUGE · AIR";
            Assert.Equal(expected, plates[0].Text);

            // Tone is what the sign MEANS: 1 = you can breathe here, 2 = you cannot and your tank is
            // running. It is the same ink the plate by the lift is already using about the floor.
            Assert.Equal(1, plates[0].Tone);

            // …and the room that opens carries the rack's own caption, because there is a rack behind it.
            DeckPlan.ConsoleSpot rack = plan.Consoles
                .Single(c => c.Kind == DeckPlan.ConsoleKind.HiveRefuge);
            Assert.Equal(UndergroundComplex.RefugeTankLabel, rack.Label);

            // …and there is no welded door anywhere on an ordinary floor.
            Assert.DoesNotContain(
                plan.Consoles, c => c.Kind == DeckPlan.ConsoleKind.HiveRefugeDark);
        }
    }

    /// <summary>#619 · <b>AND THE FLOOR WITH THE STORY ON IT CARRIES BOTH PLATES AT ONCE.</b>
    ///
    /// <para>This is the requirement the issue states as a condition of the feature existing at all — the
    /// room must be <i>"visibly distinguishable from a working refuge BEFORE a captain commits their
    /// remaining air to reaching it"</i> — read off the deck the renderer really draws. Two plates, two
    /// tones, two consoles of two kinds, and only one of them is a rack.</para></summary>
    [Fact]
    public void TheWeldedDoorIsReadableFromTheCorridorAndIsNotARack()
    {
        (string body, int level) = AFloorWithTheWeldedRefuge();
        DeckPlan plan = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

        var plates = plan.BigLabels
            .Where(l => l.Text.Contains("REFUGE", StringComparison.Ordinal))
            .ToList();
        Assert.True(plates.Count == 2,
            $"{body} B{-level}: {plates.Count} refuge plate(s) on the floor that carries the welded one — "
            + "two were owed, because the law is that it is NEVER the only refuge on its floor.");

        // Literal, for the reason the assertion above is literal: an oracle that reads RefugeGlyphFor
        // cannot disagree with RefugeGlyphFor.
        var dark = plates.Single(p => p.Text == "🫁 REFUGE — OUT OF SERVICE — REPORTED");
        var haven = plates.Single(p => p.Text != "🫁 REFUGE — OUT OF SERVICE — REPORTED");
        Assert.Contains("REFUGE ·", haven.Text, StringComparison.Ordinal);
        Assert.Equal(2, dark.Tone);     // your tank is running — the lift plate's own ink
        Assert.Equal(1, haven.Tone);    // you can breathe here

        // THE PRESSURE LAMP IS DARK, said in the only language a plate has: the word the captain crosses a
        // dead floor for is not on it.
        Assert.DoesNotContain("AIR", dark.Text, StringComparison.Ordinal);

        // One rack and one welded door, and they are different console kinds — which is the whole of how the
        // air machinery is kept from ever seeing the welded room.
        Assert.Single(plan.Consoles, c => c.Kind == DeckPlan.ConsoleKind.HiveRefuge);
        DeckPlan.ConsoleSpot welded = plan.Consoles
            .Single(c => c.Kind == DeckPlan.ConsoleKind.HiveRefugeDark);
        Assert.Equal(UndergroundComplex.RefugeFailedGlyph, welded.Label);

        // …and the weld took the lock's own 🔒 sign console with it, so there is exactly ONE press at that
        // door. Two consoles on one spot is the caption pile #1218 wrote a band book to stop.
        Assert.DoesNotContain(plan.Consoles, c =>
            c.Kind == DeckPlan.ConsoleKind.HiveSign
            && Math.Abs(c.X - welded.X) < 0.01 && Math.Abs(c.Y - welded.Y) < 0.01);

        // The door is a WALL now, and that is what "welded" means to everything that has to path past it.
        Assert.Contains(plan.Walls, w =>
            Math.Abs(((w.X1 + w.X2) / 2) - welded.X) < 0.01
            && Math.Abs(((w.Y1 + w.Y2) / 2) - welded.Y) < 0.01);
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

        foreach (UndergroundComplex.RefugeState state in StatesARoomYouCanEnterHas)
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
                    // #1149 · THIS ASSERTION USED TO BE `air == Start`, and that was #1087's mechanic: an
                    // empty rack meant a department that stopped being funded, so there was nothing behind
                    // the fill line and never would be. Under the owner's ruling an empty rack is a
                    // FOOTPRINT — the cracker is fine, somebody drew it right down — so it gives the air
                    // back on its own clock, and the clock is the price.
                    //
                    // The number is the arithmetic and not a wish: the reservoir starts at nothing, so
                    // every frame the rack makes ProductionPerSecond and the transfer moves all of it,
                    // which is thirty seconds × 2 = 60 s of tank. Pinned as a WINDOW round that, with the
                    // maintained rack's own gain as the ceiling — a build where the empty one fills like a
                    // full one is exactly as red as one where it fills not at all.
                    double madeByTheCracker = 30 * SurfaceShelter.ProductionPerSecond;
                    Assert.True(Math.Abs(air - Start - madeByTheCracker) < 1.0,
                        $"{body} B{-level}: thirty seconds in a drawn-down refuge put back {air - Start:F1} "
                        + $"s, and the cracker makes {madeByTheCracker:F0}. The rack is not a tap and it is "
                        + "not a wall — it is a machine, running at its own rate, giving back what a "
                        + "stranger took.");
                    Assert.True(air - Start < SurfaceShelter.TransferPerSecond * 30,
                        $"{body} B{-level}: the drawn-down rack filled at the maintained rack's rate. The "
                        + "reservoir is what a captain buys range with, and this one has not got one.");
                    break;

                default:
                    Assert.Fail($"{body} B{-level}: the floor's own refuge came back {state}.");
                    break;
            }

            // …and the GAUGE agrees with the drain, which is #612's whole law: the captain is never told
            // ROOM by an instrument while the sim is spending tank, nor TANKS while it is not.
            Assert.Equal(SuitAir.Supply.Room, (SuitAir.Supply)Invoke(map, "AirSupplyOf", ex)!);
        }
    }

    /// <summary>#619 · <b>THE SHELTER MACHINERY REFUSES IT THE WAY IT REFUSES A WALL.</b>
    ///
    /// <para>Standing at the welded door on the floor that carries the story: no pressure, no fill, no
    /// shelter. And the way the refusal is built is the point of the guard — the room is not in
    /// <c>RefugesOn</c> at all, so there is no branch anywhere that could be edited into letting a captain
    /// breathe in it. <c>RefugeUnderfoot</c> cannot name it and the gauge says TANKS.</para></summary>
    [Fact]
    public void TheWeldedRoomIsNoShelterAtAllAndTheTankKnowsIt()
    {
        const double Start = 600.0;
        (string body, int level) = AFloorWithTheWeldedRefuge();
        Pages.Map map = StandingAtTheWeldedDoorOn(body, level);
        object ex = Get(map, "_surface")!;

        // The geometry the air law reads cannot find the captain in a refuge here, even though they are
        // standing at one — because the welded room never joined the list.
        Assert.Equal(-1, (int)Invoke(map, "RefugeUnderfoot", ex)!);
        Assert.False((bool)Invoke(map, "BreathingRefugeUnderfoot", ex)!);
        Assert.Equal(SuitAir.Supply.Tanks, (SuitAir.Supply)Invoke(map, "AirSupplyOf", ex)!);

        SetOn(ex, "AirSeconds", Start);
        for (int frame = 0; frame < 30; frame++)
        {
            Invoke(map, "StepSuitAir", 1.0);
        }
        double air = (double)GetOn(ex, "AirSeconds")!;
        Assert.True(air < Start - 10,
            $"{body} B{-level}: thirty seconds at the welded door cost {Start - air:F1} s of tank. A room "
            + "nobody can open is a corridor, and the price of a dead floor is what depth costs.");

        // …and the floor's OWN refuge, one room over, still fills a tank. This is #619's law made concrete:
        // the captain who trusted the instrument is not dead, they are one detour away from the air.
        Pages.Map inTheGoodOne = StandingInTheRefugeOn(body, level);
        object good = Get(inTheGoodOne, "_surface")!;
        SetOn(good, "AirSeconds", Start);
        for (int frame = 0; frame < 30; frame++)
        {
            Invoke(inTheGoodOne, "StepSuitAir", 1.0);
        }
        Assert.True((double)GetOn(good, "AirSeconds")! > Start,
            $"{body} B{-level}: the working refuge on the floor that carries the welded one put nothing "
            + "back. The whole reason the beat is allowed to exist is that this room works.");
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
        foreach (UndergroundComplex.RefugeState state in StatesARoomYouCanEnterHas)
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
                + "a refuge that stops painting when its rack is drawn down is a bad map.");

            Assert.False(refuges[0].IsDead,
                $"{body} B{-level} ({state}): the fan greyed out a refuge whose door cycles.");

            // …and the cars are still on it, so this fan is the underground fan and not an empty list that
            // would have agreed with any assertion above.
            Assert.True(beacons.Exists(b => b.IsHome),
                "no way-home ring on an underground fan — this bench is reading the wrong instrument.");
        }
    }

    /// <summary>#619 · <b>THE TRACKER MUST NOT PAINT IT AS A HAVEN — at every range it is drawn.</b>
    ///
    /// <para>The issue's own condition: <i>"it must be visibly distinguishable from a working refuge BEFORE
    /// a captain commits their remaining air to reaching it."</i> The fan is where that commitment is made,
    /// so both halves are asserted here — the welded room IS painted (owner, #604: <i>"walking to one that
    /// was never marked is just a bad map"</i>) and it is never in the ink that means air.</para>
    ///
    /// <para>AT EVERY RANGE, and that clause is the one with a bug behind it. The fan CLAMPS anything past
    /// its reach to the rim, so a captain across the floor sees the ring at the edge of the glass — the
    /// range is what the clamp eats, never the ink. The captain is walked from the welded door out to the
    /// far corner of the field and the flag is read at every step.</para></summary>
    [Fact]
    public void TheFanPaintsTheWeldedRoomAndNeverAsAHaven()
    {
        (string body, int level) = AFloorWithTheWeldedRefuge();
        Pages.Map map = StandingAtTheWeldedDoorOn(body, level);
        object ex = Get(map, "_surface")!;

        (double doorX, double doorY) = ((double)Get(map, "_avatarX")!, (double)Get(map, "_avatarY")!);
        int ranges = 0;

        // Out along the line from the door toward the far corner of the field, in ten-du steps, so the ring
        // is read inside the fan's reach, at its edge, and well past the clamp.
        for (int step = 0; step <= 40; step++)
        {
            Set(map, "_avatarX", doorX + (step * 7.0));
            Set(map, "_avatarY", doorY + (step * 5.0));

            var beacons =
                (List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)>)Invoke(
                    map, "BuildBeacons", ex)!;
            var refuges = beacons.FindAll(b => !b.IsHome && !b.IsLab);

            // Two rings: the room that works and the room that does not, and the whole point is that a
            // captain can tell them apart without walking to either.
            Assert.True(refuges.Count == 2,
                $"{body} B{-level} at step {step}: {refuges.Count} refuge ring(s), expected two — one for "
                + "the room that holds and one for the room that will not open.");
            Assert.Single(refuges.FindAll(b => b.IsDead));
            Assert.Single(refuges.FindAll(b => !b.IsDead));

            // …and neither of them ever wears the lab's violet or the way-home blue, which are the other
            // two promises this fan makes.
            Assert.DoesNotContain(refuges, b => b.IsLab);
            ranges++;
        }
        Assert.True(ranges > 30, "the captain was never walked anywhere — this guard proved nothing.");

        // AND THE LEGEND FOR THE INK, because an ink a captain has to guess at is the instrument saying
        // nothing with confidence. Core's word, verbatim.
        Set(map, "_avatarX", doorX);
        Set(map, "_avatarY", doorY);
        var captions = (List<string>)Invoke(map, "BuildTrackerCaptions", ex, 0)!;
        Assert.Contains("🫁 refuge · dark", captions);

        // …and it is NOT on an ordinary floor, because a legend for a mark that is not drawn is a line about
        // somewhere else.
        (string plainBody, int plainLevel) = AFloorWhoseRefugeIs(UndergroundComplex.RefugeState.Holding);
        Pages.Map ordinary = StandingInTheRefugeOn(plainBody, plainLevel);
        var plainCaptions = (List<string>)Invoke(
            ordinary, "BuildTrackerCaptions", Get(ordinary, "_surface")!, 0)!;
        Assert.DoesNotContain("🫁 refuge · dark", plainCaptions);
    }

    // ── (d) #1149 · THE ONE THAT FAILED, AND THE PAPER ON EVERY VALVE ───────────────────────────────────

    [Fact]
    public void WalkingIntoTheRefugeThatFailedRaisesTheCardWithItsPainting()
    {
        // Owner, 2026-09-06: "If for dramatic suspense we need one that does not work, that is narrated,
        // with a gen-AI image: something scary or weird happened to the shelter." The card IS the telling
        // (#761), so the assertion is on the card and on all three things it puts on the screen.
        // #619 · AT THE DOOR, and by a press. The card was raised from the suit stepper while the captain
        // stood INSIDE the room; the room is welded shut now and nobody stands in it, so the beat is where
        // it can actually happen — [E] at the welded leaf.
        (string body, int level) = AFloorWithTheWeldedRefuge();
        Pages.Map map = StandingAtTheWeldedDoorOn(body, level);
        object ex = Get(map, "_surface")!;

        Assert.Null(FieldOn(map, "_storyCard"));
        Invoke(map, "HiveRefugeDarkInteract");

        object? card = FieldOn(map, "_storyCard");
        Assert.True(card is not null,
            $"{body} B{-level}: the captain is at the door of the refuge that failed and no card went up. "
            + "The room holds nothing, says nothing and is on the plan — without the card the whole beat is "
            + "a walk to a locked door.");

        var told = ((StoryBeats.Beat Beat, string? Subject, string? Outcome))card!;
        Assert.Equal(StoryBeats.Beat.RefugeFailed, told.Beat);
        Assert.Equal(body, told.Subject);

        // Nothing is appended to it. #736's outcome row is for a moment that SETTLED something — a fee, a
        // count, a receipt — and this one settles nothing: the room is what it is and the card may not do
        // arithmetic over it.
        Assert.Null(told.Outcome);

        // The copy the shipped card actually renders, off the component's own seam.
        var copy = ((string Title, string Art, string Caption))Invoke(
            map, "StoryBeatCopy", StoryBeats.Beat.RefugeFailed, body)!;
        Assert.Equal("art/refuge-failed.jpg", copy.Art);
        Assert.Contains("THE REFUGE THAT FAILED", copy.Title, StringComparison.Ordinal);
        Assert.Equal(
            "The door is welded from the inside, and the weld is careful. The gauge beside it reads what "
            + "the room has, which is nothing. On the rack outside are more suits than this floor ever had "
            + "staff, and the reservoir on the deck was emptied by somebody who then did not leave.",
            copy.Caption);

        // …and the painting is on disk under the name the card asks for, because a beat that names a canvas
        // nobody painted degrades to a title over an empty box and nothing on screen says why.
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(ArtRoot, "refuge-failed.jpg")),
            "the card names art/refuge-failed.jpg and the folder has not got it.");

        // #619 · AND THE BOOK KEEPS IT, under the PLACE (#741) — the card is read once and closed, and #587
        // was filed about words a player paid for that they cannot read twice.
        var book = (List<FieldNote>)Get(map, "_fieldNotes")!;
        Assert.Contains(book, n => n.Text ==
            UndergroundComplex.FailedRefugeNoteLine(body, level));

        // ONCE. The captain presses again — the card does not come back.
        Set(map, "_storyCard", null);
        Invoke(map, "HiveRefugeDarkInteract");
        Assert.Null(FieldOn(map, "_storyCard"));
    }

    [Fact]
    public void AWorkingRefugeFillsTheTankAndSaysNothingAboutIt()
    {
        // The other half, and the half that makes the one above mean anything: the ordinary case — which is
        // now four refuges in five — is a room that works, and a working room is not a story. A build that
        // raised the card in every refuge would pass the guard above and would have destroyed the feature.
        (string body, int level) = AFloorWhoseRefugeIs(UndergroundComplex.RefugeState.Holding);
        Pages.Map map = StandingInTheRefugeOn(body, level);
        object ex = Get(map, "_surface")!;

        SetOn(ex, "AirSeconds", 300.0);
        for (int frame = 0; frame < 10; frame++)
        {
            Invoke(map, "StepSuitAir", 1.0);
        }

        Assert.True((double)GetOn(ex, "AirSeconds")! > 300.0,
            $"{body} B{-level}: the maintained rack put nothing back — this bench is not in a working "
            + "refuge and proves nothing about the silence below.");
        Assert.Null(FieldOn(map, "_storyCard"));
        Assert.Null(FieldOn(map, "_deferredBeat"));
    }

    [Fact]
    public void EveryRefugeHandsOverItsInspectionTagOnceAndTheSleeveKnowsWhatItIs()
    {
        // #1149 · The paper is on EVERY refuge, whatever its seal, because a tag that appeared only on the
        // interesting room would be the game pointing at the interesting room. All three states are walked
        // for that reason, and the assertion is the same in all three.
        // #619 · Three benches now, because the two questions came apart: the STATE of the floor's refuge
        // (which the tag does not care about) and whether this FLOOR carries the story (which is where the
        // undated third entry and the inspector's card are). The floor with the story is walked last.
        var benches = new List<(string Body, int Level)>();
        foreach (UndergroundComplex.RefugeState state in StatesARoomYouCanEnterHas)
        {
            benches.Add(AFloorWhoseRefugeIs(state));
        }
        benches.Add(AFloorWithTheWeldedRefuge());

        foreach ((string body, int level) in benches)
        {
            Pages.Map map = StandingInTheRefugeOn(body, level);
            object ex = Get(map, "_surface")!;

            var before = (IReadOnlyList<Satchel.Item>)Get(map, "_satchel")!;
            Invoke(map, "HiveRefugeInteract");
            var after = (IReadOnlyList<Satchel.Item>)Get(map, "_satchel")!;

            // #1149 slice 2 · …and on the one FLOOR in the building whose story this is, the same press also
            // hands over what was in the rack drawer under the tag: the inspector's card. That is the whole
            // of the feature's first road.
            //
            // #619 · It rides this valve rather than the welded room's, because the welded room has no
            // valve a hand can reach — the door is shut and stays shut. The inspector who replaced the seal
            // was working this level, and the drawer under the tag on this level is where his card is.
            bool failed = UndergroundComplex.RefugeThatFailedIsOn(body, level);
            int expected = failed ? 2 : 1;
            Assert.True(after.Count == before.Count + expected,
                $"{body} B{-level}: the press at the rack took {after.Count - before.Count} "
                + $"thing(s) out of the room, and {expected} was owed. Every refuge in this building "
                + "carries an inspection tag; the floor with the story also holds the card.");

            Assert.Equal(failed, Inspectorate.Held(after));

            // …and the captain is TOLD, on the surface they are looking at (#761), in the only register this
            // road authors anything in: the plate that is printed on the laminate, on the tag's own pulse.
            // The room says nothing more, which is the design pass's own word for it.
            string said = ((PulseSlot)Get(map, "_pulse")!).Message ?? "";
            Assert.Equal(failed, said.Contains(Inspectorate.Plate, StringComparison.Ordinal));

            Satchel.Item tag = after[before.Count];
            Assert.Equal(Satchel.Kind.Paper, tag.Kind);
            Assert.Equal(
                UndergroundComplex.FindId(body, level, UndergroundComplex.RefugeTagRoom), tag.Id);

            // The sleeve names it off the same table the other five authored papers are named off — the
            // whole reason the tag rides a room index instead of an id of its own.
            Assert.Equal("An inspection tag", FieldClue.Title(tag.Id));
            Assert.Equal(
                "Refuge inspected. Rack full, seals within tolerance. No signature — none required.",
                FieldClue.Document(tag.Id));

            // ONCE, and the register that says so is the durable one — so it survives the flight home in
            // exactly the way an emptied room does.
            Invoke(map, "HiveRefugeInteract");
            Assert.Equal(after.Count, ((IReadOnlyList<Satchel.Item>)Get(map, "_satchel")!).Count);

            var turned = (HashSet<string>)Get(map, "_roomsTurnedOver")!;
            Assert.Contains(
                KeepOrLeave.RoomKey(body, level, UndergroundComplex.RefugeTagRoom), turned);

            // …and the press that follows is the rack's own, not a second helping of paper: on a room that
            // holds it reads the gauge, and on the one that failed it says the door will not cycle.
            Assert.Equal(level, (int)GetOn(ex, "Floor")!);
        }
    }

    /// <summary>Where the paintings live, from the test binary.</summary>
    private static string ArtRoot
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                string art = System.IO.Path.Combine(
                    dir, "src", "SpaceSails.Client", "wwwroot", "art");
                if (System.IO.Directory.Exists(art))
                {
                    return art;
                }
                dir = System.IO.Path.GetDirectoryName(dir);
            }
            throw new System.IO.DirectoryNotFoundException("Could not find wwwroot/art above the tests.");
        }
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component standing INSIDE the refuge on a real floor of a real site. The room is the
    /// generator's own (<c>HiveInterior.FloorDeck</c> put the console there off the floor plan), never a
    /// coordinate this test picked — a guard handed a world it typed itself cannot tell pass from fail.</summary>
    private static Pages.Map StandingInTheRefugeOn(string body, int level) =>
        AtTheRefugeOn(body, level, DeckPlan.ConsoleKind.HiveRefuge);

    /// <summary>#619 · A live component standing at the WELDED DOOR of the refuge that failed — outside it,
    /// because there is no inside to be on. Same construction as the one above, and the console it walks to
    /// is the generator's own.</summary>
    private static Pages.Map StandingAtTheWeldedDoorOn(string body, int level) =>
        AtTheRefugeOn(body, level, DeckPlan.ConsoleKind.HiveRefugeDark);

    private static Pages.Map AtTheRefugeOn(string body, int level, DeckPlan.ConsoleKind kind)
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
        DeckPlan.ConsoleSpot rack = plan.Consoles.Single(c => c.Kind == kind);
        Set(map, "_avatarX", (double)rack.X);
        Set(map, "_avatarY", (double)rack.Y);

        if (kind == DeckPlan.ConsoleKind.HiveRefuge)
        {
            Assert.True((int)Invoke(map, "RefugeUnderfoot", ex)! >= 0,
                $"{body} B{-level}: the captain was put on the refuge's own console and the containment law "
                + "says they are not in it — this bench is standing somewhere else.");
        }
        else
        {
            // …and at the welded door the OPPOSITE has to be true, or the bench is standing in a room the
            // whole feature says cannot be entered.
            Assert.Equal(-1, (int)Invoke(map, "RefugeUnderfoot", ex)!);

            // …and the press the captain's hand reaches there is THIS one, which is what the weld taking
            // the lock's 🔒 console with it is for: two consoles at one midpoint and the verb is a coin toss.
            Assert.Equal(
                DeckPlan.ConsoleKind.HiveRefugeDark,
                plan.NearestConsoleSpot((double)rack.X, (double)rack.Y)!.Value.Kind);
        }
        return map;
    }

    /// <summary>A private FIELD by name, even when it is holding null — which <see cref="Get"/> cannot do,
    /// because its <c>?.</c> falls through to the property lookup the moment the field's value is null. A
    /// card that has not gone up is exactly that case, and it is the case the guard is about.</summary>
    private static object? FieldOn(object o, string name) =>
        (o.GetType().GetField(name, Hidden)
         ?? throw new InvalidOperationException($"the component has no field `{name}`.")).GetValue(o);

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
