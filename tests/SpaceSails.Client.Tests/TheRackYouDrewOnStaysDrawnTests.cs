using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #563 slice 3 · THE RACK YOU DREW ON STAYS DRAWN — the half of the shelters-per-tile change that only a
/// live component can be asked about.
///
/// <para>Slice 2 named this exactly and refused to ship it: <i>"ex.ShelterReservoir, ShelterPumpNoted and
/// ShelterUnderfoot are all keyed on an int index into SheltersOn(ex), which is one site's list. Making that
/// list span a moving chunk would silently re-point every reservoir key each time the captain crossed a tile
/// boundary — the same failure the huts had, in the one system where getting it wrong empties a rack
/// somebody walked to on their last two hundred seconds of air."</i></para>
///
/// <para>Core measures the ground (<c>TheSheltersGoOutIntoTheWorldTests</c>): every tile carries air at the
/// tube's own density, the tube's own drums did not move, nothing is built through one. What is measured
/// HERE is the thing Core cannot see — a captain standing in a shed nine hundred du out, drawing on its
/// rack, walking far enough that the chunk under it is evicted, and finding it as they left it while the
/// shelter of the same NUMBER beside the ship is still untouched.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRackYouDrewOnStaysDrawnTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    /// <summary>A tile a long way from the tube, and one nobody could reach by accident from the home
    /// build's own arithmetic — which is the point: everything about it comes from its address.</summary>
    private static readonly SurfaceTiles.Address Away = new(2, -2);

    // ── (a) THE GROUND ARRIVES WITH SOMEWHERE TO BREATHE ────────────────────────────────────────────────

    /// <summary>
    /// <b>A FAR CHUNK COMES BACK WITH SHELTERS ON IT, WITH THEIR SERVICES.</b> #573's original report was
    /// <i>"there seemed to be shelter like spaces that were just missing the services and the doors"</i>, and
    /// slice 2 fixed exactly that for ruins while leaving the actual shelters at home. A drum welded without
    /// its charging rack would be that same complaint again, on the one building whose whole purpose is a
    /// service.
    ///
    /// <para>Read off the region the live deck is grown by, so a guard cannot be satisfied by a call that is
    /// present and does nothing. Every rack must stand on a drum Core says is there, and every drum must have
    /// a rack — which is #573's map-lies law pointed at the lattice.</para>
    /// </summary>
    [Fact]
    public void AFarChunk_ComesBackWithSheltersAndTheirServices()
    {
        foreach (LandingSite site in LandingSites.For(Body))
        {
            object region = ComposeChunk(Body, site.LayoutSalt, Away);
            List<(float X, float Y, string Kind)> consoles = Consoles(region);

            var promised = new List<(double X, double Y)>();
            foreach (SurfaceTiles.Address a in SurfaceTiles.Chunk(Away))
            {
                if (a == SurfaceTiles.Home)
                {
                    continue;   // the home tile is the base deck, never welded through this door
                }
                promised.AddRange(SurfaceTiles.Shelters(Body, site.LayoutSalt, a)
                    .Select(s => (s.CentreX, s.CentreY)));
            }

            Assert.True(promised.Count > 0, "this chunk carries no shelters — nothing can be proved here.");

            List<(float X, float Y, string Kind)> racks =
                [.. consoles.Where(c => c.Kind == "ShelterTank")];
            Assert.Equal(promised.Count, racks.Count);

            foreach ((double px, double py) in promised)
            {
                Assert.True(
                    racks.Any(r => Math.Abs(r.X - px) < 0.5 && Math.Abs(r.Y - py) < 0.5),
                    $"{Body}/{site.Name}: a shelter at ({px:F0}, {py:F0}) was welded with no charging rack.");
                Assert.True(
                    consoles.Any(c => c.Kind == "ShelterLocker" && Math.Abs(c.X - px) < 0.5),
                    $"{Body}/{site.Name}: a shelter at ({px:F0}, {py:F0}) with no emergency locker.");
                Assert.True(
                    consoles.Any(c => c.Kind == "ShelterDoor"
                        && Math.Abs(c.X - px) < 26 && Math.Abs(c.Y - py) < 26),
                    $"{Body}/{site.Name}: a shelter at ({px:F0}, {py:F0}) with no door to read a suit.");
            }
        }
    }

    // ── (b) THE RACK ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE RACK IS ADDRESSED, NOT NUMBERED.</b> The whole slice, as one walk.
    ///
    /// <para>A captain stands in a shed two tiles out and draws on it. The chunk under them is thrown away
    /// and re-welded — which is what happens every time somebody walks a few hundred du and comes back, and
    /// which regenerates the shelter list from its address. The rack must be as they left it.</para>
    ///
    /// <para>And the second assertion is the one that can tell pass from fail: the shelter of the SAME INDEX
    /// on the home tile must be untouched. Under the old keying both halves of this test passed on the first
    /// assertion and failed on the second — the captain's draw landed on the tube's own rack, and the shed
    /// they were standing in was reporting a charge from a building four hundred du away.</para>
    /// </summary>
    [Fact]
    public void ARackDrawnDownOnAFarTile_IsFoundAsItWasLeftAndTheTubesIsUntouched()
    {
        LandingSite site = LandingSites.For(Body)[0];
        string salt = site.LayoutSalt;

        IReadOnlyList<SurfaceStructure.Spec> outThere = SurfaceTiles.Shelters(Body, salt, Away);
        Assert.True(outThere.Count > 1, "the far tile has no shelters — this bench is measuring nothing.");

        // The LAST one, so an off-by-one that quietly reads index 0 cannot pass by luck.
        int which = outThere.Count - 1;
        SurfaceStructure.Spec drum = outThere[which];

        Pages.Map map = StandingIn(Body, site, drum);
        object ex = Get(map, "_surface")!;

        object spot = Invoke(map, "ShelterUnderfoot", ex)!;
        Assert.True((int)spot.GetType().GetProperty("Index")!.GetValue(spot)!  == which,
            "the captain was put in the middle of the last drum on the far tile and the containment law "
            + "says they are standing in a different one — this bench is somewhere else.");
        Assert.Equal(Away, (SurfaceTiles.Address)spot.GetType().GetProperty("Tile")!.GetValue(spot)!);

        double full = (double)Invoke(map, "ShelterReservoirNow", ex, spot)!;

        // Drain it: the suit is well under the two-thirds ceiling, so the rack pumps for as long as the
        // captain stands there. Sixty seconds is far more than the reservoir holds at the transfer rate.
        SetOn(ex, "AirSeconds", 60.0);
        for (int frame = 0; frame < 60; frame++)
        {
            Invoke(map, "StepSuitAir", 1.0);
        }

        double drawn = (double)Invoke(map, "ShelterReservoirNow", ex, spot)!;
        Assert.True(drawn < full - 1.0,
            $"standing in the shed for a minute left the rack at {drawn:F0} s of {full:F0} — nothing was "
            + "drawn, so nothing about persistence can be proved from here.");

        // ── THE CHUNK GOES AND COMES BACK ──
        //
        // Walked out of the chunk and back rather than poked: the stream is what evicts, and the specs cache
        // is what a re-weld regenerates. Both are exercised the way an excursion exercises them.
        WalkTo(map, ex, SurfaceTiles.Rect(new SurfaceTiles.Address(Away.X + 4, Away.Y)).LeftX + 20, drum.CentreY);
        ((System.Collections.IDictionary)GetOn(ex, "ShelterSpecs")!).Clear();
        WalkTo(map, ex, drum.CentreX, drum.CentreY);

        object again = Invoke(map, "ShelterUnderfoot", ex)!;
        Assert.Equal(Away, (SurfaceTiles.Address)again.GetType().GetProperty("Tile")!.GetValue(again)!);
        Assert.Equal(drawn, (double)Invoke(map, "ShelterReservoirNow", ex, again)!, 6);

        // …and the tube's own shelter of the same number never heard about any of it. This is the assertion
        // the bare int could not pass.
        object atTheTube = Activator.CreateInstance(spot.GetType(), SurfaceTiles.Home, which)!;
        double expected = SurfaceShelter.SomebodyWasHere(Body, salt, which)
            ? SurfaceShelter.ReservoirSeconds * 0.42
            : SurfaceShelter.ReservoirSeconds;
        Assert.Equal(expected, (double)Invoke(map, "ShelterReservoirNow", ex, atTheTube)!, 6);
    }

    // ── (c) THE INSTRUMENT ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE FAN SAYS WHAT IT CAN HEAR, AND THEN POINTS.</b> A lattice carries several times the shelters
    /// one field did, and everything past the fan's reach clamps to the RIM — so painting all of them would
    /// have put a fence of identical circles round the edge of the instrument with the way home lost inside
    /// it. #585 already filed that complaint once: <i>a beacon that cannot be told apart from its neighbours
    /// is decoration.</i>
    ///
    /// <para>So: every shelter inside the reach places, and beyond it exactly one ring is drawn — the
    /// nearest roof. Both halves are asserted, because a gate with no pointer would be the map going quiet
    /// about air, and a pointer with no gate would be the fence.</para>
    ///
    /// <para><b>And the way home is not gated at all.</b> Asserted here too, on the same fan, because a
    /// range rule written for the shelters is exactly the edit that would one day reach the one ring #563
    /// slice 2 guards at every distance.</para>
    /// </summary>
    [Fact]
    public void TheFan_PaintsTheAirItCanHearAndPointsAtTheNearestItCannot()
    {
        LandingSite site = LandingSites.For(Body)[0];
        SurfaceStructure.Spec drum = SurfaceTiles.Shelters(Body, site.LayoutSalt, Away)[0];
        Pages.Map map = StandingIn(Body, site, drum);
        object ex = Get(map, "_surface")!;

        double reach = (double)typeof(Pages.Map)
            .GetField("SurfaceVisualHalfWidthDu", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetRawConstantValue()!;
        reach = MotionTracker.DetectionRange(reach);

        var beacons = (List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)>)
            Invoke(map, "BuildBeacons", ex)!;

        List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)> homes =
            [.. beacons.Where(b => b.IsHome)];
        Assert.True(homes.Count == 1, $"the fan drew {homes.Count} ways home; there is one tube.");
        Assert.True(homes[0].Range > reach,
            "the bench is standing inside the fan's reach of the tube, so the unconditional HOME ring "
            + "proves nothing here.");

        List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)> shelters =
            [.. beacons.Where(b => !b.IsHome && !b.IsLab)];

        int carried = SurfaceTiles.Chunk(Away).Sum(a => SurfaceTiles.Shelters(Body, site.LayoutSalt, a).Count);
        Assert.True(carried > shelters.Count,
            $"every one of the {carried} shelters the captain is carrying got a ring — that is the fence of "
            + "identical circles this rule exists to prevent.");

        int within = shelters.Count(b => b.Range <= reach);
        Assert.True(within > 0, "not one shelter inside the fan's own reach was painted.");
        Assert.True(shelters.Count - within == 1,
            $"{shelters.Count - within} rings were drawn beyond the fan's reach; beyond it the instrument "
            + "says WHICH WAY the nearest air is, once.");
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component standing in the middle of one drum on the open regolith, with the ground
    /// streamed around it exactly as an excursion streams it.</summary>
    private static Pages.Map StandingIn(string body, LandingSite site, in SurfaceStructure.Spec drum)
    {
        var map = new Pages.Map();

        // The framework's own render early-out, so the verbs that end in StateHasChanged are silent no-ops
        // on a bench with no renderer.
        typeof(ComponentBase)
            .GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex, site);
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        WalkTo(map, ex, drum.CentreX, drum.CentreY);
        return map;
    }

    /// <summary>Put the boots somewhere and let the ground catch up — the stream steps and the deck is
    /// rebuilt, which is what a walk does.</summary>
    private static void WalkTo(Pages.Map map, object ex, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
        object stream = GetOn(ex, "Stream")!;
        stream.GetType().GetMethod("Step")!.Invoke(stream, [x, y, null, null]);
        Invoke(map, "RebuildSurfaceDeck");
    }

    private static object ComposeChunk(string body, string salt, SurfaceTiles.Address centre)
    {
        MethodInfo compose = typeof(Pages.Map)
            .GetMethod("TileRegion", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("TileRegion is gone — the composer moved.");
        return compose.Invoke(null, [body, salt, SurfaceTiles.Chunk(centre)])!;
    }

    private static List<(float X, float Y, string Kind)> Consoles(object region)
    {
        var found = new List<(float, float, string)>();
        var spots = (Array)region.GetType().GetProperty("Consoles")!.GetValue(region)!;
        foreach (object? spot in spots)
        {
            Type t = spot!.GetType();
            found.Add((
                (float)t.GetProperty("X")!.GetValue(spot)!,
                (float)t.GetProperty("Y")!.GetValue(spot)!,
                t.GetProperty("Kind")!.GetValue(spot)!.ToString()!));
        }
        return found;
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
