using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #563 · <b>A SHUT HUT DOOR HIDES YOU TOO.</b> Owner ruling, 2026-09-06:
/// <i>the Reevers do not see through a closed door — on the moon either.</i>
///
/// <para>#1154 gave the eye the shut leaf aboard a wreck and deliberately kept the regolith's sight list
/// wreck-only, byte-identical on every moon; #1157 then gave their LEGS the shut leaf on <b>every</b> ground.
/// Between the two, a hut door stopped an Old One's boot and not its look — you could shut a leaf in its
/// face, watch it stand there, and be hunted through the picture of a closed door. That is the last hiding
/// place of the one asymmetry #465, #466, #1099, #1154 and #1157 are each a fix for, and this is the ruling
/// that ends it: it is the same refuge the owner asked #563 for in the first place, <i>"rooms with doors we
/// can hide behind while we reload our guns safe from reevers"</i>.</para>
///
/// <para><b>Why a driven guard and not a geometry one.</b> Both lists are honest and both are reachable from
/// a test; the whole defect was WHICH ONE the page handed the eye on a moon, and only the page knows that. So
/// these drive real <c>StepReevers</c> frames on a shipped moon's real deck with a real contact on it, and
/// read the answer off the contact's own memory.</para>
///
/// <para><b>What is read, and why it cannot lie.</b> The contact already HAS the captain
/// (<c>EverSeen</c>) — and for such a contact <see cref="ReeverObservation.Look"/> reports
/// <c>Fixed</c> with no die cast at all, so the only thing left deciding whether it writes his live position
/// into its memory is the sightline the page hands it. No roll, no cadence, no tuning: one frame with a clear
/// line writes, and any number of frames without one does not. That is the rule under test, stated as the
/// one bit it actually is.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AShutHutDoorHidesYouTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;

    /// <summary>A shipped moon, and the one every other regolith guard in this project stands on.</summary>
    private const string Body = "luna";

    /// <summary>How far a hidden contact may end up from where the bench stood it before the case has
    /// stopped being the case the premise measured. It shivers in place (#436) and hunts a spot it is already
    /// standing on, so this is slack for the shuffle and nothing else — a contact that had actually walked
    /// would be tens of du away at a shamble's pace.</summary>
    private const double ShuffleSlack = 2.0;

    /// <summary>Far enough from the tube that the leaf under test is certainly a HUT's and not the shuttle's
    /// own hatch — the ruling is about the buildings on the ground, and a bench that quietly proved it on a
    /// ship door would be proving #465 over again.</summary>
    private const double ClearOfTheShip = 60.0;

    // ── THE PREMISE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE BENCH IS THE CASE IT CLAIMS TO BE. Four things have to hold of the spot these cases use, and if
    /// any one quietly stopped holding, every assertion below would pass for a reason that is not the rule:
    ///
    /// <list type="number">
    /// <item>the contact has a line to the captain through <b>stone alone</b> — so nothing but the leaf is
    /// ever in the way, and "he was not seen" cannot be a regolith boulder's doing;</item>
    /// <item>the hut door between them is <b>shut</b> with the captain standing where the bench puts him, and
    /// the page's own sight list therefore <b>stops</b> that same line;</item>
    /// <item>a step to the doorway <b>opens</b> it again and the line comes back — otherwise a build that had
    /// simply closed the eye would satisfy everything;</item>
    /// <item>the contact is <b>out of arm's length of the leaf</b>, so it is never a candidate to haul the
    /// thing open (#1157) and the door under test stays shut because the captain is far from it, which is the
    /// only reason this file wants it shut for.</item>
    /// </list>
    /// </summary>
    [Fact]
    public void ThePremise_OnlyTheLeafStandsBetweenTheCaptainAndTheOldOne()
    {
        Pages.Map map = OnTheRegolith();
        Spot spot = TheHutDoorWithOneBehindIt(map);
        var deck = (DeckPlan)Get(map, "_deckPlan")!;

        StandTheCaptainAt(map, spot.ShutFromX, spot.ShutFromY);

        Assert.True(
            SurfaceCollision.HasLineOfSight(spot.OldOneX, spot.OldOneY, spot.ShutFromX, spot.ShutFromY,
                deck.CollisionField),
            "stone stands between the Old One and the captain on this bench — the leaf is then not the thing "
            + "under test and every 'he was not seen' below would pass on a wall.");

        Assert.False(
            SurfaceCollision.HasLineOfSight(spot.OldOneX, spot.OldOneY, spot.ShutFromX, spot.ShutFromY,
                SightBlockers(map)),
            "the page's own sight list lets the look through — either the hut door is not shut with the "
            + "captain here, or the line does not cross it, and this file is measuring nothing.");

        StandTheCaptainAt(map, spot.OpenFromX, spot.OpenFromY);
        Assert.True(
            SurfaceCollision.HasLineOfSight(spot.OldOneX, spot.OldOneY, spot.OpenFromX, spot.OpenFromY,
                SightBlockers(map)),
            "the hut door does not open with the captain standing in it, so the second half of this file "
            + "could never tell a fixed build from a broken one.");

        double reach = ReeverDoor.Reach(DeckPlan.AvatarRadius);
        var leaf = new[] { new SurfaceCollision.Segment(spot.LeafX1, spot.LeafY1, spot.LeafX2, spot.LeafY2) };
        Assert.False(SurfaceCollision.Blocked(spot.OldOneX, spot.OldOneY, reach, leaf),
            $"the Old One is inside arm's length ({reach:F2} du) of the leaf, so it is a candidate to haul it "
            + "open (#1157) and the door could come over for a reason that has nothing to do with sight.");
    }

    // ── THE LAW ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A CAPTAIN BEHIND A SHUT HUT DOOR IS NOT SEEN. One of them stands inside the hut with the captain in
    /// its line for half a minute of real frames — no stone between them, well past the arrival grace, and
    /// already fixed on him, so nothing but the leaf can be stopping the look — and its memory of where he is
    /// never moves off the sentinel it was loaded with.
    ///
    /// <para><b>Proven RED</b> by putting the wreck-only list back (<c>OnWreck ? theirLegs : null</c>, with
    /// the eye falling back on <c>walls</c>). See the PR body for the verbatim failure.</para>
    /// </summary>
    [Fact]
    public void AShutHutDoorHidesTheCaptainFromThem()
    {
        Pages.Map map = OnTheRegolith();
        Spot spot = TheHutDoorWithOneBehindIt(map);
        object one = PutOneInTheHut(map, spot);
        StandTheCaptainAt(map, spot.ShutFromX, spot.ShutFromY);

        RunFrames(map, seconds: 30);

        Assert.Equal(spot.OldOneX, Memory(one, "LastSeenX"), 6);
        Assert.Equal(spot.OldOneY, Memory(one, "LastSeenY"), 6);

        // AND THE THIRTY SECONDS WERE SPENT IN THE GEOMETRY THE PREMISE MEASURED. It shivers where it stands
        // and hunts a spot it is already on, so it should still be in the hut with the same leaf across the
        // same line. A contact that had drifted out of the doorway would satisfy the two assertions above for
        // a reason that is not the rule.
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        double dx = Memory(one, "X") - spot.OldOneX, dy = Memory(one, "Y") - spot.OldOneY;
        Assert.True(Math.Sqrt((dx * dx) + (dy * dy)) <= ShuffleSlack,
            "the Old One left the spot the bench put it on, so the sightline it was measured against is not "
            + "the sightline it was tested on.");
        Assert.True(
            SurfaceCollision.HasLineOfSight(Memory(one, "X"), Memory(one, "Y"),
                spot.ShutFromX, spot.ShutFromY, deck.CollisionField),
            "stone has come between the two by the end of the run — the leaf is no longer the only thing in "
            + "the way and this case would pass on the old build.");
    }

    /// <summary>
    /// …AND OPENING THE DOOR IS WHAT SHOWS HIM. The same bench, the same contact: the captain simply walks up
    /// to the leaf, which is the only verb this game has for opening one (<c>Airlock.MayOpen</c> off his own
    /// distance; the renderer and the sight list read that one answer). It retracts, the look lands, and his
    /// live position goes into its memory on the first frame it can.
    ///
    /// <para>This is the half that keeps the case above a law rather than an accident: a build in which
    /// nothing on a moon can ever see the captain would satisfy the first case perfectly.</para>
    /// </summary>
    [Fact]
    public void OpeningTheHutDoorIsWhatShowsHim()
    {
        Pages.Map map = OnTheRegolith();
        Spot spot = TheHutDoorWithOneBehindIt(map);
        object one = PutOneInTheHut(map, spot);

        StandTheCaptainAt(map, spot.ShutFromX, spot.ShutFromY);
        RunFrames(map, seconds: 2);
        Assert.Equal(spot.OldOneX, Memory(one, "LastSeenX"), 6);

        StandTheCaptainAt(map, spot.OpenFromX, spot.OpenFromY);
        RunFrames(map, seconds: 1);

        Assert.Equal(spot.OpenFromX, Memory(one, "LastSeenX"), 6);
        Assert.Equal(spot.OpenFromY, Memory(one, "LastSeenY"), 6);
    }

    /// <summary>
    /// …AND A SLEEPER IN THE HUT IS NOT ROUSED THROUGH THE LEAF EITHER — #1154's own case, on a moon. The
    /// lamp that DRAWS a hibernating contact is the same lamp that WAKES it, and off a wreck that lamp used
    /// to be swept against the bare stone: a captain who had shut a hut door behind him woke whatever was
    /// folded down on the far side of it without ever laying eyes on the thing.
    ///
    /// <para>The two halves of the sight list — the sleeper's lamp and the awake contact's look — are read
    /// out of the SAME local in <c>StepReevers</c>, so this is not a second rule; it is the second faculty,
    /// and a fix that only reached one of them would go red here.</para>
    /// </summary>
    [Fact]
    public void ASleeperInTheHutIsNotRousedThroughTheLeaf()
    {
        Pages.Map map = OnTheRegolith();
        Spot spot = TheHutDoorWithOneBehindIt(map);
        object one = PutOneInTheHut(map, spot);
        Set(one, "EverSeen", false);
        Set(one, "Dormant", true);
        Set(one, "VisibleOnMap", false);
        Set(one, "WakeAtMs", double.MaxValue);   // its own clock never comes round: the lamp is the only wake

        double dx = spot.OldOneX - spot.ShutFromX, dy = spot.OldOneY - spot.ShutFromY;
        double range = Math.Sqrt((dx * dx) + (dy * dy));
        Assert.True(range <= DormantSightRange(),
            $"the sleeper is {range:F2} du off and the lamp reaches {DormantSightRange():F2} du — RANGE would "
            + "be hiding it, not the door, and this case would pass on the old build.");

        StandTheCaptainAt(map, spot.ShutFromX, spot.ShutFromY);
        RunFrames(map, seconds: 30);
        Assert.True(Flag(one, "Dormant"),
            "a shut hut door woke the thing behind it: the captain's lamp reached through the leaf on a moon, "
            + "which is exactly the ruling of 2026-09-06.");
        Assert.False(Flag(one, "VisibleOnMap"), "it is drawn on the map through a shut hut door.");

        // …and the leaf is the wake, here as aboard: stand in the doorway and the lamp finds it.
        StandTheCaptainAt(map, spot.OpenFromX, spot.OpenFromY);
        RunFrames(map, seconds: 1);
        Assert.False(Flag(one, "Dormant"),
            "the captain opened the hut door and stood in it with an Old One folded down a few du inside, "
            + "inside the lamp, and nothing came round — the fix has closed the eye rather than given it a "
            + "door.");
    }

    // ── ONE LIST, BY CONSTRUCTION ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE EYES AND THE LEGS ARE THE SAME OBJECT ON A MOON — not two lists that happen to agree today.
    ///
    /// <para><c>TheirLegs()</c> is <c>SightBlockers()</c> itself (#1157), and after this lane
    /// <c>StepReevers</c> takes that one list and hands it to the legs, the sleeper's lamp and the
    /// observation roll alike. Reference identity is the honest statement of that, and it is what the
    /// behavioural cases above cannot say: a build with two lists built from the same segments would satisfy
    /// every one of them and then drift the first time somebody edited one of the two.</para>
    ///
    /// <para>Asked on a moon <b>and</b> on a hull, because "one list" that is only true on one ground is the
    /// arrangement this whole family of bugs came out of.</para>
    /// </summary>
    [Fact]
    public void TheirEyesAndTheirLegsAreOneObject()
    {
        Pages.Map moon = OnTheRegolith();
        Assert.False((bool)Get(moon, "OnWreck")!, "the regolith bench is somehow aboard a wreck.");
        Assert.Same(Invoke(moon, "SightBlockers"), Invoke(moon, "TheirLegs"));

        var deck = (DeckPlan)Get(moon, "_deckPlan")!;
        Assert.True(deck.Doors.Length > 0,
            "this moon's deck hangs no doors at all, so 'stone plus whatever is shut' would be stone and the "
            + "whole file would be about nothing.");
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One hut door out on the regolith, with a pose either side of it: where the captain stands to
    /// have it SHUT in his face, where he stands to have it open, where the Old One waits beyond it, and the
    /// leaf itself.</summary>
    private readonly record struct Spot(
        double ShutFromX, double ShutFromY,
        double OpenFromX, double OpenFromY,
        double OldOneX, double OldOneY,
        double LeafX1, double LeafY1, double LeafX2, double LeafY2);

    /// <summary>
    /// Find that door by ASKING THE DECK, never by typing four numbers off a dump: this ground is a pure
    /// function of (body, salt, tile) and it is re-seeded whenever the generator is touched, so hand-typed
    /// coordinates would end up standing in open regolith and every case in this file would go quietly green.
    ///
    /// <para>Swept perpendicular to each leaf that the Old Ones' own law would let them work at all
    /// (<see cref="ReeverDoor.MayWork"/>) and that stands clear of the ship: the captain outside the door's
    /// own opening radius, the Old One a short way beyond it and out of arm's length of the leaf, and the
    /// whole triple accepted only when stone alone lets the look through, the shut leaf stops it, and a step
    /// to the doorway opens it again.</para>
    /// </summary>
    private static Spot TheHutDoorWithOneBehindIt(Pages.Map map)
    {
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        double reach = ReeverDoor.Reach(DeckPlan.AvatarRadius);
        double keptX = (double)Get(map, "_avatarX")!, keptY = (double)Get(map, "_avatarY")!;
        double shipX = MoonSurface.SpawnX, shipY = MoonSurface.SpawnY;
        int candidates = 0;

        try
        {
            foreach (DeckPlan.Door d in deck.Doors)
            {
                if (!ReeverDoor.MayWork(d.Locked, d.Interlock != 0, deck.DoorwayIsWalledUp(d)))
                {
                    continue;   // a locked leaf, an airlock's leaf, or a picture in front of stone
                }
                double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
                if (((mx - shipX) * (mx - shipX)) + ((my - shipY) * (my - shipY))
                    < ClearOfTheShip * ClearOfTheShip)
                {
                    continue;   // the shuttle's own leaves: #465's case, not this one
                }
                candidates++;

                double dx = d.X2 - d.X1, dy = d.Y2 - d.Y1;
                double len = Math.Sqrt((dx * dx) + (dy * dy));
                if (len < 1e-9)
                {
                    continue;
                }
                double px = -dy / len, py = dx / len;   // the leaf's own normal: across the doorway

                foreach (int side in new[] { 1, -1 })
                {
                    for (double back = DeckPlan.DoorOpenRadius + 0.5; back <= DormantSightRange() - 3.0; back += 0.25)
                    {
                        double cx = mx + (px * back * side), cy = my + (py * back * side);
                        for (double beyond = reach + 0.5; beyond <= 4.0; beyond += 0.25)
                        {
                            double sx = mx - (px * beyond * side), sy = my - (py * beyond * side);
                            if (back + beyond > DormantSightRange())
                            {
                                break;      // out of the lamp: the sleeper half could not be measured here
                            }
                            if (!SurfaceCollision.HasLineOfSight(cx, cy, sx, sy, deck.CollisionField))
                            {
                                continue;   // stone is in the way: proves nothing about a leaf
                            }

                            StandTheCaptainAt(map, cx, cy);
                            if (SurfaceCollision.HasLineOfSight(cx, cy, sx, sy, SightBlockers(map)))
                            {
                                continue;   // this leaf is not shut from here, or the line misses it
                            }

                            // …and a pose that OPENS it: half the radius in, on the captain's own side.
                            double ox = mx + (px * (DeckPlan.DoorOpenRadius / 2.0) * side);
                            double oy = my + (py * (DeckPlan.DoorOpenRadius / 2.0) * side);
                            StandTheCaptainAt(map, ox, oy);
                            if (!SurfaceCollision.HasLineOfSight(ox, oy, sx, sy, SightBlockers(map)))
                            {
                                continue;
                            }

                            return new Spot(cx, cy, ox, oy, sx, sy, d.X1, d.Y1, d.X2, d.Y2);
                        }
                    }
                }
            }
        }
        finally
        {
            StandTheCaptainAt(map, keptX, keptY);
        }

        throw new InvalidOperationException(
            $"no hut door on {Body} ({candidates} workable leaf/leaves clear of the ship, out of "
            + $"{deck.Doors.Length} door(s) and {deck.CollisionSegments.Length} segment(s)) has a pose either "
            + "side of it where the leaf and nothing else stands between the captain and a spot inside the "
            + "lamp. Either the huts have stopped hanging doors, or a shut leaf has stopped being opaque — "
            + "and #563's whole refuge rests on both.");
    }

    /// <summary>A captain out on the real regolith of a real landing site on a shipped moon, past the arrival
    /// grace, with the surface clock running and nothing else in the sim alive.</summary>
    private static Pages.Map OnTheRegolith()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);
        exType.GetProperty("LandedAtMs")!.SetValue(ex, 0.0);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        // Well past SurfaceArrival.SpotGraceSeconds: the ground is live, which is the posture every case here
        // is about. A bench inside the grace would prove that nothing sees you, for the wrong reason.
        Set(map, "_lastTimestampMs", (double?)(SurfaceArrival.SpotGraceSeconds * 1000.0 * 3));
        Set(map, "_avatarX", (double)MoonSurface.SpawnX);
        Set(map, "_avatarY", MoonSurface.SpawnY);
        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>
    /// One awake Old One inside the hut, ALREADY FIXED on the captain. That is the whole trick of this file:
    /// <see cref="ReeverObservation.Look"/> casts no die for a contact that already has you, so whether it
    /// writes his live position is decided by nothing but the sightline the page hands it — and its own hunt
    /// is aimed at the spot it is standing on, so it goes nowhere and the geometry the bench measured stays
    /// the geometry under test.
    /// </summary>
    private static object PutOneInTheHut(Pages.Map map, Spot spot)
    {
        Type reever = typeof(Pages.Map).GetNestedType("Reever", Hidden | BindingFlags.Public)!;
        object one = Activator.CreateInstance(reever, nonPublic: true)!;
        Set(one, "X", spot.OldOneX);
        Set(one, "Y", spot.OldOneY);
        Set(one, "AnchorX", spot.OldOneX);
        Set(one, "AnchorY", spot.OldOneY);
        Set(one, "Facing", 0.0);
        Set(one, "EverSeen", true);
        // Its memory of the captain starts on the spot it is standing on — which is both the sentinel and the
        // reason it goes nowhere: it hunts the last place it saw him, that place is here, so it has arrived
        // and shivers. A WRITE is then observable as a write (the captain is du away and cannot be mistaken
        // for this), and the geometry the bench measured stays the geometry under test for the whole run.
        Set(one, "LastSeenX", spot.OldOneX);
        Set(one, "LastSeenY", spot.OldOneY);
        Set(one, "JitterSeed", 0xD1B54A32D192ED03UL);
        ((IList)Get(map, "_reevers")!).Add(one);
        return one;
    }

    /// <summary>Put the captain's boots on a spot. The door's own open/shut answer and the look are both
    /// measured off this one pair of numbers, which is the point: one source of truth.</summary>
    private static void StandTheCaptainAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    /// <summary>Whole surface frames at the shipping rate, with the clock advancing — the real
    /// <c>StepReevers</c>, not a re-implementation of what it is thought to do.</summary>
    private static void RunFrames(Pages.Map map, double seconds)
    {
        const double dt = 1.0 / 60.0;
        for (int i = 0; i < (int)Math.Round(seconds / dt); i++)
        {
            Set(map, "_lastTimestampMs",
                (double?)((Get(map, "_lastTimestampMs") as double? ?? 0) + (dt * 1000.0)));
            Invoke(map, "StepReevers", dt);
        }
    }

    /// <summary>The page's OWN sight list — stone plus whatever is shut with the captain standing where he is
    /// standing this instant. Asked of the shipping method rather than rebuilt here, so a bench that
    /// disagreed with the game about what is opaque cannot exist.</summary>
    private static IReadOnlyList<SurfaceCollision.Segment> SightBlockers(Pages.Map map) =>
        (IReadOnlyList<SurfaceCollision.Segment>)Invoke(map, "SightBlockers")!;

    /// <summary>How far the lamp reaches for a hibernating contact — read off the page's own constant so a
    /// re-tuning moves this bench instead of silently making it prove nothing.</summary>
    private static double DormantSightRange() =>
        (double)typeof(Pages.Map).GetField("DormantSightRange", Hidden)!.GetValue(null)!;

    private static bool Flag(object r, string name) =>
        (bool)r.GetType().GetField(name, Hidden)!.GetValue(r)!;

    private static double Memory(object r, string name) =>
        (double)r.GetType().GetField(name, Hidden)!.GetValue(r)!;

    private static object? Get(object o, string name)
    {
        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        Assert.True(prop is not null, $"the component has no `{name}` — this guard is reading a dead name.");
        return prop!.GetValue(o);
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        o.GetType().GetMethod(method, Hidden)!.Invoke(o, args);
}
