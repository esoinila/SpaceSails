using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1261 · <b>"EYES MEETING EYES ACROSS THE ROOM" IN A ROOM WITH NOBODY IN IT.</b>
///
/// <para><b>What was played</b> (2026-09-20, twice and independently, seated and standing): the concourse
/// shudder fired with the captain out at the end of #1199's observation walk —</para>
/// <para><i>"A shudder walks through the concourse and every conversation stops mid-word. A held beat, eyes
/// meeting eyes across the room — then, as one, everybody agrees it was just the station shifting on its
/// moorings, and the noise floods back."</i></para>
/// <para>…in the room the game's own card describes thirty seconds later as <i>"The walk is lit the whole way
/// out… There is nobody here, and there is nowhere here to be."</i></para>
///
/// <para><b>Why.</b> #1249/#1248 partitioned <c>HullShudder.HavenLines</c> into the BAR's and
/// <i>everything else</i>, and the gallery is neither: it is a two-pace crossbar at the end of a dead-end
/// tube with one doorway and no people. So it drew the concourse's share — a room, a crowd and a unison,
/// three things that room is built not to have. Fourth time on this floor after #1215 (the stranger-bond),
/// #1222 (its fix) and #1199 (the buzzer), which is why the partition grew a third ROOM rather than one more
/// gate.</para>
///
/// <para><b>No prose moved.</b> The walk's share of the pool is empty, the other two shares are the authored
/// lines in the authored order, and <c>AHavenLineKnowsItsRoomTests</c> holds both halves of that.</para>
///
/// <para>Asked from both sides, because a guard that only watched the gallery would be satisfied by a beat
/// that had been deleted from the station altogether.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheConcourseShudderNeedsTheConcourseTests
{
    private const string CanvasId = "concourse-shudder-canvas";

    private static double TheBarsFloorY =>
        HavenInterior.BarBand(Port)?.FloorY
        ?? throw new InvalidOperationException($"{Port} draws no bar band, so there is no room to be in.");

    // ── THE LAW, FROM BOTH SIDES ────────────────────────────────────────────────────────────────────────

    /// <summary>#1261 · <b>THE ANTI-VACUOUS HALF, AND IT COMES FIRST.</b> On the concourse the beat still
    /// fires and it is still the concourse's own line — so the refusal below is a room's silence and not a
    /// feature quietly taken off the station.</summary>
    [Fact]
    public void OutOnTheConcourseTheShudderStillWalksThroughIt()
    {
        SpaceSails.Client.Pages.Map map = ADockedBerth();
        StandHimOnTheConcourse(map);

        RunTheShuddersClock(map);

        Assert.True((bool)Read(map, "_shudderActive")!, "the shudder never came on the concourse at all.");
        var pulse = (PulseSlot)Read(map, "_pulse")!;
        Assert.NotNull(pulse.Message);
        Assert.Contains(
            "concourse", pulse.Message!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// #1261 · <b>AND OUT ON THE WALK IT DOES NOT COME AT ALL.</b> Not the line, not the shake, not the held
    /// breath — the room has nobody in it to look up, so the beat is not a scene there.
    ///
    /// <para><b>RED on the shipped tree:</b> the gallery answered the concourse's pool and the line landed,
    /// which is the issue's own two screenshots.</para>
    /// </summary>
    [Fact]
    public void ButOutInTheGalleryNothingIsSaid()
    {
        SpaceSails.Client.Pages.Map map = ADockedBerth();
        StandHimAtTheRail(map);

        RunTheShuddersClock(map);

        Assert.False(
            (bool)Read(map, "_shudderActive")!,
            "a shudder walked through the concourse while the captain was standing at the rail of a room "
            + "the game's own card calls empty.");
        var pulse = (PulseSlot)Read(map, "_pulse")!;
        Assert.True(
            pulse.Message is null || !pulse.Message.Contains("〰", StringComparison.Ordinal),
            $"the walk was told a mood line anyway: {pulse.Message}");
    }

    /// <summary>#1261 · …and the schedule is CONSUMED rather than held. A refused tremor that kept its slot
    /// would be due again on the very next frame and on every frame after it for as long as the captain stood
    /// at the rail — a hundred refusals a second, and a shudder waiting in ambush for the walk back in.
    /// The ordinal moves and the next gap is redrawn, which is the tremor having happened and not having been
    /// a scene.</summary>
    [Fact]
    public void AndTheWALKSOwnTremorsAreSPENTRatherThanSavedUp()
    {
        SpaceSails.Client.Pages.Map map = ADockedBerth();
        StandHimAtTheRail(map);

        RunTheShuddersClock(map);

        Assert.True(
            (int)Read(map, "_shudderIndex")! > 0,
            "no tremor ever came due out on the walk, so this law is about a clock that never ran.");
        Assert.False((bool)Read(map, "_shudderActive")!);
        Assert.True(
            (double)Read(map, "_shudderNextGap")! < 0
                || (double)Read(map, "_shudderNextGap")! > (double)Read(map, "_shudderSeconds")!,
            "the walk's refused tremor is still due, so it will fire again on the very next frame.");
    }

    // ── The berth, and the two places to stand in it ─────────────────────────────────────────────────────

    private static SpaceSails.Client.Pages.Map ADockedBerth()
    {
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);

        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        Assert.True(HavenInterior.HasInterior(Port), $"{Port} has no interior, so it has no concourse.");
        Assert.True(HavenInterior.HasObservationWalk(Port), $"{Port} has no observation walk to stand in.");
        CelestialBody berth = sky.Bodies.First(b => b.Id == Port);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Port, (double)Read(map, "SimTime")!), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
        return map;
    }

    /// <summary>South of the bar's own south wall — the one wall <c>InTheBar</c> reads — and, asserted here,
    /// outside the walk, so the pair below differ by the room and by nothing else.</summary>
    private static void StandHimOnTheConcourse(SpaceSails.Client.Pages.Map map)
    {
        Set(map, "_avatarY", TheBarsFloorY - 6.0);
        Invoke(map, "RefreshAshore");
        Assert.True((bool)Read(map, "_ashore")!, "the captain never got past the tube.");
        Assert.False(
            HavenInterior.InTheObservationWalk(
                Port, (double)Read(map, "_avatarX")!, (double)Read(map, "_avatarY")!),
            "the concourse stand is inside the walk, so both halves of this law are the same room.");
    }

    /// <summary>…and out at the far end of the walk, on the rail the room is built around — asked of
    /// <c>HavenInterior</c> rather than typed, and asserted to be in the walk the page's own predicate
    /// reads.</summary>
    private static void StandHimAtTheRail(SpaceSails.Client.Pages.Map map)
    {
        DeckReachability.Point rail = HavenInterior.TheRailAt(Port)
            ?? throw new InvalidOperationException($"{Port}'s walk has no rail.");
        Set(map, "_avatarX", rail.X);
        Set(map, "_avatarY", rail.Y);
        Invoke(map, "RefreshAshore");
        Assert.True((bool)Read(map, "_ashore")!, "the captain never got past the tube.");
        Assert.True(
            HavenInterior.InTheObservationWalk(Port, rail.X, rail.Y),
            "the rail is not in the observation walk, so this law is about nowhere.");
    }

    /// <summary>Walk the page's own shudder clock through the shipping <c>StepShudder</c> — long enough that
    /// the schedule is due however it jittered — so what is under test is the road to the beat rather than
    /// the beat's own door. Stops the moment one is playing.</summary>
    private static void RunTheShuddersClock(SpaceSails.Client.Pages.Map map)
    {
        for (double spent = 0; spent < HullShudder.MeanGapSeconds * 6; spent += 0.1)
        {
            Invoke(map, "StepShudder", 0.1, 10_000.0 + (spent * 1000.0));
            if ((bool)Read(map, "_shudderActive")!)
            {
                return;
            }
        }
    }
}
