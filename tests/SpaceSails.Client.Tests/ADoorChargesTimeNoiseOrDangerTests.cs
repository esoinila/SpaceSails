using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #563 · <b>A LOCKED DOOR IS TIME, NEVER A KEY — the page half.</b>
///
/// <para>Owner ruling, 2026-09-13: <i>"I like the time instead of a key, considering we have firepower and
/// tools. We can create the same effect as needing a key by making it slow, too noisy, or dangerous in other
/// ways."</i> <c>ADoorIsTimeNotAKeyTests</c>, one project along, holds the law. This file holds the two
/// things only the page knows: <b>what a hold actually does to the field around it</b>, and <b>what a round
/// actually does to the leaf</b>.</para>
///
/// <para><b>Nothing here re-implements the game.</b> Every case drives the shipping methods on a real deck —
/// <c>StepOutpostDoorChannel</c>, <c>ShootTheLockNow</c>, <c>WorkTheDoor</c>, <c>RebuildSurfaceDeck</c> — and
/// reads the answer off the contact's own memory or the plan's own walls. A bench that asserted against a
/// re-derived copy of the rule would be the fifth named bug class with extra steps.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ADoorChargesTimeNoiseOrDangerTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static
        | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

    /// <summary>A shipped moon, and the one every other regolith guard in this project stands on.</summary>
    private const string Body = "luna";

    // ── NOISY: the hold is heard, and it has a radius ─────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PREMISE FOR THE NOISE CASES.</b> Four things have to hold, or "it woke up" and "it did not"
    /// would both pass for reasons that are not the rule:
    ///
    /// <list type="number">
    /// <item>the contacts start <b>dormant</b> — never having seen the captain — so a wake is observable;</item>
    /// <item>their remembered spot starts where they are standing, so a WRITE is observable as a write;</item>
    /// <item>the near one is inside <c>Clatter</c> and the far one outside it, measured off
    /// <c>ReeverHearing.RangeOf</c> itself rather than a typed-in number;</item>
    /// <item>the arrival grace is spent, or <c>MakeNoise</c> refuses every emit and the whole file is about
    /// nothing (#461).</item>
    /// </list>
    /// </summary>
    [Fact]
    public void ThePremise_TheFieldIsDormantAndTheTwoContactsStraddleClatter()
    {
        Pages.Map map = OnTheRegolith();
        double reach = ReeverHearing.RangeOf(ReeverHearing.Noise.Clatter);

        object near = PutOneAt(map, AvatarX(map) + reach - 1.0, AvatarY(map));
        object far = PutOneAt(map, AvatarX(map) + reach + 2.0, AvatarY(map));

        Assert.False(Flag(near, "EverSeen"), "the near contact starts awake — a wake would prove nothing.");
        Assert.False(Flag(far, "EverSeen"), "the far contact starts awake.");

        Assert.True(ReeverHearing.Hears(reach - 1.0, ReeverHearing.Noise.Clatter),
            "the near contact is out of Clatter range on this bench.");
        Assert.False(ReeverHearing.Hears(reach + 2.0, ReeverHearing.Noise.Clatter),
            "the far contact is INSIDE Clatter range on this bench, so 'it did not hear' could not fail.");

        Assert.True(
            SurfaceArrival.CanBeSpotted(
                (((double?)Get(map, "_lastTimestampMs") ?? 0) - 0.0) / 1000.0),
            "the bench is still inside the arrival grace, so MakeNoise refuses every emit (#461) and every "
            + "'it did not hear' below would pass on the grace rather than on the range.");
    }

    /// <summary>
    /// <b>A HELD FORCE WAKES WHAT IS INSIDE CLATTER RANGE, AND NOTHING BEYOND IT.</b>
    ///
    /// <para>One tick of a real hold — <c>StepOutpostDoorChannel</c>, the shipping method, with the captain's
    /// boots on the anchor and the bar nowhere near full — and the near contact has learned a PLACE and the
    /// far one has not. The place it learned is the DOOR and not the captain, which is #456's whole point: a
    /// noise buys a spot to walk to.</para>
    ///
    /// <para><b>RED</b> by deleting <c>TheHoldIsHeard(ch);</c> from <c>StepOutpostDoorChannel</c> — which is
    /// the state of the tree before this lane, where a captain could lever a hatch in a field of thirty Old
    /// Ones in total silence. The near contact then stays dormant and the first assert fails.</para>
    /// </summary>
    [Fact]
    public void AHeldForceWakesASleeperInsideClatterRangeAndNotBeyond()
    {
        Pages.Map map = OnTheRegolith();
        double reach = ReeverHearing.RangeOf(ReeverHearing.Noise.Clatter);
        double ax = AvatarX(map), ay = AvatarY(map);

        object near = PutOneAt(map, ax + reach - 1.0, ay);
        object far = PutOneAt(map, ax + reach + 2.0, ay);

        HoldTheHatch(map, ax, ay);
        Invoke(map, "StepOutpostDoorChannel", 0.1);

        Assert.True(Flag(near, "EverSeen"),
            "a contact one du inside Clatter range slept through a man putting a bar to a dogged hatch. "
            + "Owner ruling 2026-09-13: a locked door costs time, NOISE or danger.");
        Assert.Equal(ax, Memory(near, "LastSeenX"), 3);
        Assert.Equal(ay, Memory(near, "LastSeenY"), 3);

        Assert.False(Flag(far, "EverSeen"),
            "a contact two du OUTSIDE Clatter range heard it — the hold is ringing the whole field, and a "
            + "noise that reaches everything is not a decision with a radius on it.");

        // …and the hold is still a hold: one tick of a five-second bar has not opened anything.
        Assert.NotNull(Get(Excursion(map), "OutpostDoorChannel"));
    }

    /// <summary>
    /// <b>AND THE HOLD DOES NOT SHOUT.</b> The same tick at the same anchor leaves anything outside
    /// <c>Clatter</c> alone even when it is well inside <c>Gunfire</c>'s reach — because a man leaning on a
    /// frame is not a gun going off, and the two loudnesses are the owner's own dial.
    ///
    /// <para><b>RED</b> by pointing <c>TheHoldIsHeard</c> at <c>Noise.Gunfire</c>: the contact at
    /// three-quarters of gunfire's reach wakes and this fails.</para>
    /// </summary>
    [Fact]
    public void AHeldForceIsNotAGunshot()
    {
        Pages.Map map = OnTheRegolith();
        double gun = ReeverHearing.RangeOf(ReeverHearing.Noise.Gunfire);
        double ax = AvatarX(map), ay = AvatarY(map);

        object wayOut = PutOneAt(map, ax + (gun * 0.75), ay);
        Assert.True(ReeverHearing.Hears(gun * 0.75, ReeverHearing.Noise.Gunfire),
            "the bench put this contact outside GUNFIRE too, so it could not tell the two apart.");

        HoldTheHatch(map, ax, ay);
        Invoke(map, "StepOutpostDoorChannel", 0.1);

        Assert.False(Flag(wayOut, "EverSeen"),
            "forcing a door is being heard as far as a gun is. A door and a volley are the same event to the "
            + "field, and the whole point of the fast road is that it costs MORE noise than the slow one.");
    }

    // ── DANGEROUS: the round, and what it takes with it ───────────────────────────────────────────────

    /// <summary>
    /// <b>THE PREMISE FOR THE ROUND.</b> The bench is a real mountain lab on a real moon with real keyed
    /// doors on the plan and the captain standing at one of them, and the pocket has rounds in it. If any of
    /// this stopped holding, the plate cases below would pass on an empty scene.
    /// </summary>
    [Fact]
    public void ThePremise_TheCaptainIsStandingAtAKeyedLabDoorWithRoundsInHisPocket()
    {
        Pages.Map map = InTheMountain(out string doorId);

        Assert.Equal(LockedDoor.State.Locked, LabDoors(map)[doorId]);
        Assert.Equal(doorId, (string)Invoke(map, "NearestLabDoorId")!);

        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        Assert.Equal(DeckPlan.ConsoleKind.LabDoor,
            deck.NearestConsoleSpot(AvatarX(map), AvatarY(map))!.Value.Kind);

        Assert.True((bool)Get(map, "ArmedAtADoor")!, "the pocket is empty, so nothing below is armed.");
        Assert.Equal(0, Excursion(map).GetType().GetProperty("Floor")!.GetValue(Excursion(map)));
    }

    /// <summary>
    /// <b>THE PLATE IS OFFERED ONLY WHERE ARMED, AND ONLY AT A LEAF THAT STILL HAS A LOCK.</b> Four poses,
    /// four answers, and the sentence itself is the authored one.
    ///
    /// <para><b>RED</b> by dropping the <c>ArmedAtADoor</c> conjunct from <c>ShootTheLockPlate</c> (the empty
    /// pocket then reads the plate), or by writing <c>TheLockUnderTheHand</c> without its
    /// <c>MayShootTheLock</c> test (a destroyed leaf then goes on offering a round).</para>
    /// </summary>
    [Fact]
    public void ThePlateAppearsOnlyWhereArmedAndOnlyOnALockedOrShutLeaf()
    {
        Pages.Map map = InTheMountain(out string doorId);

        // Armed, at a keyed leaf: the authored plate, and it is on the key row the other keys are on.
        Assert.Equal($"🔫 {LockedDoor.ShootThePlate}", Invoke(map, "ShootTheLockPlate"));
        Assert.Contains(LockedDoor.ShootThePlate,
            (string)Invoke(map, "BuildSurfaceKeyHints", Excursion(map))!, StringComparison.Ordinal);

        // Unarmed: not a refusal, an ABSENCE. Nothing on screen offers a trade the pocket cannot pay for.
        Set(map, "_satchel", NewSatchel());
        Assert.Null(Invoke(map, "ShootTheLockPlate"));
        Assert.DoesNotContain(LockedDoor.ShootThePlate,
            (string)Invoke(map, "BuildSurfaceKeyHints", Excursion(map))!, StringComparison.Ordinal);

        // Armed again, but the leaf is OPEN: there is no lock on it to shoot.
        Arm(map);
        LabDoors(map)[doorId] = LockedDoor.State.Open;
        Assert.Null(Invoke(map, "ShootTheLockPlate"));

        // …and a leaf that is merely SHUT does carry the verb: it is a lock, and hardware breaks.
        LabDoors(map)[doorId] = LockedDoor.State.Shut;
        Assert.NotNull(Invoke(map, "ShootTheLockPlate"));
    }

    /// <summary>
    /// <b>ONE ROUND, AND THE LEAF IS OUT OF THE WORLD.</b> The press costs exactly
    /// <see cref="LockedDoor.RoundsToShootTheLock"/> out of the pocket, the door is open in the same frame
    /// (no channel, no bar, no hold), the wall that WAS that doorway is gone from the plan, and the leaf
    /// cannot be shut, locked or shot a second time however many times it is pressed.
    ///
    /// <para><b>RED</b> at every assert in turn: leave the <c>Satchel.Remove</c> line out (the round is
    /// free); write <c>Shoot</c> to return <c>State.Open</c> (the door can be shut again, and the last block
    /// fails); leave the <c>RebuildSurfaceDeck</c> out (the wall is still on the plan while the captain reads
    /// the line about it being gone — the third named bug class).</para>
    /// </summary>
    [Fact]
    public void ShootingCostsOneRoundOpensInstantlyAndTheLeafNeverComesBack()
    {
        Pages.Map map = InTheMountain(out string doorId);
        int had = RoundsInPocket(map);
        Assert.True(had > LockedDoor.RoundsToShootTheLock, "the bench must be able to see a round LEAVE.");

        (double lx, double ly) = LabDoorLine(map, doorId);
        Assert.True(WallStandsOn(map, lx, ly), "the keyed leaf is not on the plan as a wall, so the case "
            + "cannot tell a door that opened from one that never blocked anything.");

        Invoke(map, "ShootTheLockNow");

        // The state, the price, and the picture — in that order, and all three from the shipping run.
        Assert.Equal(LockedDoor.State.Destroyed, LabDoors(map)[doorId]);
        Assert.Equal(had - LockedDoor.RoundsToShootTheLock, RoundsInPocket(map));
        Assert.False(WallStandsOn(map, lx, ly),
            "the leaf came off in the sentence and is still a wall on the plan. A rebuild after the shot is "
            + "the whole of 'sim and pen agree'.");

        // …and it does not grow back on the next ordinary rebuild either.
        Invoke(map, "RebuildSurfaceDeck");
        Assert.False(WallStandsOn(map, lx, ly));

        // TERMINAL. Press it, press it again, shoot it again: a hole is a hole.
        Invoke(map, "WorkTheDoor", doorId);
        Assert.Equal(LockedDoor.State.Destroyed, LabDoors(map)[doorId]);
        Assert.Null(Invoke(map, "ShootTheLockPlate"));

        int left = RoundsInPocket(map);
        Invoke(map, "ShootTheLockNow");
        Assert.Equal(LockedDoor.State.Destroyed, LabDoors(map)[doorId]);
        Assert.Equal(left, RoundsInPocket(map));
    }

    /// <summary>
    /// <b>AND THE SHOT IS HEARD AT FULL EARSHOT.</b> A contact standing three-quarters of the way to
    /// gunfire's reach — far outside anything a hold would reach — learns where the captain is standing.
    /// This is the price that makes the fast road a decision rather than a shortcut.
    ///
    /// <para><b>RED</b> by deleting the <c>MakeNoise</c> line from <c>ShootTheLockNow</c>, or by giving it
    /// <c>Noise.Clatter</c>: the contact stays dormant either way.</para>
    /// </summary>
    [Fact]
    public void ShootingALockIsHeardAtFullEarshot()
    {
        Pages.Map map = InTheMountain(out _);
        double gun = ReeverHearing.RangeOf(ReeverHearing.Noise.Gunfire);
        object wayOut = PutOneAt(map, AvatarX(map) + (gun * 0.75), AvatarY(map));

        Invoke(map, "ShootTheLockNow");

        Assert.True(Flag(wayOut, "EverSeen"),
            "a gun went off in a mountain and something three-quarters of a field away did not look up.");
        Assert.Equal(AvatarX(map), Memory(wayOut, "LastSeenX"), 3);
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A captain out on the real regolith of a real landing site on a shipped moon, past the arrival
    /// grace, with nothing else in the sim alive.</summary>
    private static Pages.Map OnTheRegolith(bool withLab = false)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);
        exType.GetProperty("LandedAtMs")!.SetValue(ex, 0.0);

        if (withLab)
        {
            exType.GetProperty("Lab")!.SetValue(ex,
                SecretLab.For(Body, MoonSurface.ExpeditionField(), forcePresent: true));
            exType.GetProperty("SecretLabForced")!.SetValue(ex, true);
        }

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        // Well past SurfaceArrival.SpotGraceSeconds: the ground is live, which is the posture every case here
        // is about. A bench inside the grace would prove that nothing hears you, for the wrong reason.
        Set(map, "_lastTimestampMs", (double?)(SurfaceArrival.SpotGraceSeconds * 1000.0 * 3));
        Set(map, "_avatarX", (double)MoonSurface.SpawnX);
        Set(map, "_avatarY", MoonSurface.SpawnY);
        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>
    /// The captain standing at the first chamber door of a real mountain lab, with every leaf in the place
    /// KEYED — which is the lockdown, the scene the ruling was about — and rounds in the pocket.
    ///
    /// <para>The lab is asked of <c>SecretLab</c> itself and the door spot is read off the composed deck, not
    /// typed in: the generator is pure per body and re-seeded whenever it is touched, so hand-typed
    /// coordinates would end up in open regolith and every case would go quietly green.</para>
    /// </summary>
    private static Pages.Map InTheMountain(out string doorId)
    {
        Pages.Map map = OnTheRegolith(withLab: true);
        var ex = (object)Excursion(map);
        SecretLab.Placement p = (SecretLab.Placement)ex.GetType().GetProperty("Lab")!.GetValue(ex)!;
        SecretLab.Region region = SecretLab.Build(Body, MoonSurface.ExpeditionField(), p.DoorX, p.DoorY);

        Assert.True(region.Doors.Count > 0, "this lab hangs no chamber doors, so the file is about nothing.");
        SecretLab.LabDoor d = region.Doors[0];
        doorId = d.Id;

        Dictionary<string, LockedDoor.State> doors = LabDoors(map);
        foreach (SecretLab.LabDoor any in region.Doors)
        {
            doors[any.Id] = LockedDoor.State.Locked;   // the lockdown: every door in the mountain, at once
        }

        Arm(map);
        Invoke(map, "RebuildSurfaceDeck");

        // Stand him at the door's own console — the spot the composer put there, half a du off the leaf.
        Set(map, "_avatarX", d.X);
        Set(map, "_avatarY", d.Y - LabDoorHalf() - 0.9);
        return map;
    }

    /// <summary>A bar on a dogged hatch, anchored where the captain's boots are. The shipping channel record,
    /// so the hold under test is the hold the game runs.</summary>
    private static void HoldTheHatch(Pages.Map map, double anchorX, double anchorY)
    {
        Type ch = typeof(Pages.Map).GetNestedType("DoorChannel", Hidden)!;
        object hold = Activator.CreateInstance(ch, nonPublic: true)!;
        ch.GetField("DoorId", Hidden)!.SetValue(hold, "outpost");
        ch.GetField("AnchorX", Hidden)!.SetValue(hold, anchorX);
        ch.GetField("AnchorY", Hidden)!.SetValue(hold, anchorY);
        object ex = Excursion(map);
        ex.GetType().GetProperty("OutpostDoorChannel")!.SetValue(ex, hold);
    }

    /// <summary>One DORMANT Old One at a spot, remembering only the ground under its own feet — so a write
    /// into its memory is observable as a write, and it walks nowhere in the meantime.</summary>
    private static object PutOneAt(Pages.Map map, double x, double y)
    {
        Type reever = typeof(Pages.Map).GetNestedType("Reever", Hidden)!;
        object one = Activator.CreateInstance(reever, nonPublic: true)!;
        Set(one, "X", x);
        Set(one, "Y", y);
        Set(one, "AnchorX", x);
        Set(one, "AnchorY", y);
        Set(one, "Facing", 0.0);
        Set(one, "EverSeen", false);
        Set(one, "LastSeenX", x);
        Set(one, "LastSeenY", y);
        Set(one, "JitterSeed", 0xD1B54A32D192ED03UL);
        ((IList)Get(map, "_reevers")!).Add(one);
        return one;
    }

    private static void Arm(Pages.Map map)
    {
        List<Satchel.Item> pocket = NewSatchel();
        pocket.Add(new Satchel.Item(Satchel.Kind.Rounds, Ammunition.Issue.Id, 6));
        Set(map, "_satchel", pocket);
    }

    private static List<Satchel.Item> NewSatchel() => [];

    private static int RoundsInPocket(Pages.Map map) =>
        Satchel.OfKind((List<Satchel.Item>)Get(map, "_satchel")!, Satchel.Kind.Rounds).Sum(i => i.Count);

    private static Dictionary<string, LockedDoor.State> LabDoors(Pages.Map map) =>
        (Dictionary<string, LockedDoor.State>)Get(map, "_labDoors")!;

    private static double LabDoorHalf() =>
        (double)typeof(Pages.Map).GetField("LabDoorHalf", Hidden)!.GetValue(null)!;

    /// <summary>The line a keyed lab door occupies on the plan — its own X, and the mid-height of its leaf.
    /// Read off the generator so the probe follows the geometry rather than a remembered number.</summary>
    private static (double X, double Y) LabDoorLine(Pages.Map map, string doorId)
    {
        object ex = Excursion(map);
        SecretLab.Placement p = (SecretLab.Placement)ex.GetType().GetProperty("Lab")!.GetValue(ex)!;
        SecretLab.LabDoor d = SecretLab.Build(Body, MoonSurface.ExpeditionField(), p.DoorX, p.DoorY)
            .Doors.Single(x => x.Id == doorId);
        return (d.X, d.Y);
    }

    /// <summary>Is there a wall across that spot on the plan RIGHT NOW? Asked of the deck's own collision
    /// field, which is the list the captain's boots are stepped against — so "the door is gone" means the
    /// one thing it has to mean.</summary>
    private static bool WallStandsOn(Pages.Map map, double x, double y)
    {
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        return SurfaceCollision.Blocked(x, y, 0.05, deck.CollisionField);
    }

    private static object Excursion(Pages.Map map) => Get(map, "_surface")!;

    private static double AvatarX(Pages.Map map) => (double)Get(map, "_avatarX")!;

    private static double AvatarY(Pages.Map map) => (double)Get(map, "_avatarY")!;

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
