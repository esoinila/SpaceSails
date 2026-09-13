using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #563 · A LOCKED DOOR IS TIME, NEVER A KEY — AND ONE OF THE TIMES IS A ROUND.
///
/// <para>Owner ruling, 2026-09-13: <i>"I like the time instead of a key, considering we have firepower and
/// tools. We can create the same effect as needing a key by making it slow, too noisy, or dangerous in other
/// ways."</i> The slow road is the shoulder (<see cref="LockedDoor.ForceSeconds"/>) and it is heard the whole
/// time it runs. <b>This file is the other one: the fast road.</b></para>
///
/// <para><b>[F] — SHOOT THE LOCK.</b> One round out of the hand-load, the door opens in the time it takes to
/// pull a trigger, <c>ReeverHearing.Noise.Gunfire</c> goes out at full earshot through the emitter every other
/// shot on this ground already uses, and <b>the leaf is destroyed</b>. Not open — gone. It can never be shut
/// again, never locked again, never leaned on again, and nothing behind you closes for the rest of the
/// excursion.</para>
///
/// <para><b>That is the danger, and it is the sentries' own law arriving at a doorway.</b> #314: <i>bots buy
/// time, never safety</i>. So does a round through a hasp — you have bought twenty-five seconds and paid for
/// them with every retreat that door would ever have covered, plus half the field's worth of ears. Nothing on
/// screen says any of that. The pack walking in through the hole later is the telling.</para>
///
/// <para><b>Where the verb exists.</b> On the ground, on a wreck and in an expedition region — anywhere the
/// captain is carrying the hand-load. <b>Not on a Hive floor</b>, and that is not squeamishness: the Hive's
/// locked leaves are <c>DoorwayIsWalledUp</c> — a wall with a sign on it — and <c>ShootTheLock.Judge</c> has
/// ruled since #803 that <i>you cannot shoot a WALL</i>. A door down there that swung open on a round and left
/// stone behind it would be the sim doing one thing while the picture reported another, which is a bug class
/// this repo has already named and paid for three times. Underground the verb that exists is the one that
/// already exists: a sentry, the handset, and <c>ShootTheLock</c>'s own six-round tariff.</para>
/// </summary>
public partial class Map
{
    /// <summary>Rounds in the coat. THE HAND-LOAD IS THE ARMAMENT — there is no separate sidearm counter in
    /// this game and inventing one here would be a second answer to "what can the captain shoot with".
    /// Returns the first stack with anything in it, so the id that is spent is the id that is shown.</summary>
    private Satchel.Item? TheHandLoad()
    {
        foreach (Satchel.Item item in Satchel.OfKind(_satchel, Satchel.Kind.Rounds))
        {
            if (item.Count >= LockedDoor.RoundsToShootTheLock)
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// Is the captain armed, HERE? Two facts and no more: something in the pocket, and a floor the verb lives
    /// on. The floor test is <c>MoonSurface.ShovelWorksOnThisFloor</c>'s own — level 0 and up is regolith and
    /// the things built on it, below is the facility — asked of the same one fact rather than re-derived,
    /// which is what stops the plate and the press from ever disagreeing about where F works.
    /// </summary>
    private bool ArmedAtADoor => _surface is { } ex && ex.Floor >= 0 && TheHandLoad() is not null;

    /// <summary>What the round would be fired at, if [F] were pressed this instant. Exactly one of the two
    /// door models the game has, or neither.</summary>
    private readonly record struct TheLockUnderTheHand(string LabDoorId, int LeafIndex)
    {
        public bool IsLabDoor => LabDoorId.Length > 0;
        public bool IsLeaf => LeafIndex >= 0;
        public bool Exists => IsLabDoor || IsLeaf;
    }

    /// <summary>
    /// WHICH LOCK. Asked in the same order [E] is dispatched, so the two keys can never come to two views of
    /// one doorway:
    /// <list type="number">
    /// <item>A mountain-lab door, if the captain is standing at its console — the only leaf in the game whose
    /// state is a <see cref="LockedDoor.State"/>, and the only one where LOCKED actually stops a captain.</item>
    /// <item>Otherwise the nearest <c>DeckPlan.Door</c> leaf within arm's reach that is shut or locked, is not
    /// backed by stone, and has not already been shot.</item>
    /// </list>
    /// </summary>
    private TheLockUnderTheHand FindTheLock()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is { Kind: DeckPlan.ConsoleKind.LabDoor }
            && NearestLabDoorId() is { Length: > 0 } labId
            && _labDoors.TryGetValue(labId, out LockedDoor.State labState)
            && LockedDoor.MayShootTheLock(labState, armed: true))
        {
            return new(labId, -1);
        }

        int best = -1;
        double bestRange = DeckPlan.InteractRadius * DeckPlan.InteractRadius;
        DeckPlan.Door[] doors = _deckPlan.Doors;
        for (int i = 0; i < doors.Length; i++)
        {
            DeckPlan.Door d = doors[i];
            if (_deckPlan.LeafIsShot(i))
            {
                continue;   // one hole, one round
            }
            // A leaf with stone across it is not a lock, it is a wall with a sign on it (#590 call 2,
            // ShootTheLock.Verdict.NoLockToBreak). Shooting it would open a picture and change nothing.
            if (_deckPlan.DoorwayIsWalledUp(d))
            {
                continue;
            }
            double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
            double dx = mx - _avatarX, dy = my - _avatarY;
            double range = (dx * dx) + (dy * dy);
            if (range > bestRange)
            {
                continue;
            }
            bestRange = range;
            best = i;
        }

        return new(string.Empty, best);
    }

    /// <summary>#563 · The plate, on the door prompt row, only where the verb exists — an affordance you
    /// cannot read is one you do not have (#212), and one that is read where it does not work is worse. Null
    /// keeps the row exactly the length it was.</summary>
    private string? ShootTheLockPlate() =>
        ArmedAtADoor && FindTheLock().Exists ? $"🔫 {LockedDoor.ShootThePlate}" : null;

    /// <summary>
    /// [F]. One round, one door, and the door never comes back.
    ///
    /// <para>The order matters and is the order of the sentence: the lock goes, the round leaves the pocket,
    /// the ear is rung at the DOOR (not at the captain — a place to walk to is what a noise buys, #456), and
    /// then, once, the captain's own register says what was spent. The line is Fable-authored and said
    /// verbatim; the noise is never mentioned in it, because the game does not announce (#453/#456) and the
    /// pack arriving through the hole is the whole telling.</para>
    /// </summary>
    private void ShootTheLockNow()
    {
        if (_surface is not { } ex || AnySlowThingUnderYourHands)
        {
            return;
        }
        if (!ArmedAtADoor || TheHandLoad() is not { } load)
        {
            return;
        }

        TheLockUnderTheHand at = FindTheLock();
        double doorX, doorY;

        if (at.IsLabDoor)
        {
            if (!_labDoors.TryGetValue(at.LabDoorId, out LockedDoor.State was)
                || !LockedDoor.MayShootTheLock(was, armed: true))
            {
                return;
            }
            _labDoors[at.LabDoorId] = LockedDoor.Shoot(was);
            (doorX, doorY) = (_avatarX, _avatarY);   // the console IS the door; the captain is at arm's length
        }
        else if (at.IsLeaf)
        {
            DeckPlan.Door d = _deckPlan.Doors[at.LeafIndex];
            if (!_deckPlan.ShootTheLeaf(at.LeafIndex))
            {
                return;
            }
            // …and WRITTEN DOWN, because the plan under it is thrown away and rebuilt several times a minute
            // and a destroyed door that grew its leaf back would not be destroyed at all.
            ex.LeavesShotOpen.Add(LeafKey(d));
            (doorX, doorY) = ((d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0);
        }
        else
        {
            return;
        }

        // The round leaves the pocket. One, and the satchel's own remover does it, so the stack, the id and
        // the count are kept by the one writer that keeps every other stack in the coat.
        _satchel = [.. Satchel.Remove(_satchel, Satchel.Kind.Rounds, load.Id, LockedDoor.RoundsToShootTheLock)];

        // THE NOISE, through the emitter every other shot on this ground uses — the same call
        // Map.Combat.Remote makes when a sentry fires, at the same Noise.Gunfire, at the door. Half the
        // field learns a PLACE and walks to it (ReeverHearing.RangeOf(Gunfire) = 34 du).
        MakeNoise(doorX, doorY, ReeverHearing.Noise.Gunfire);

        // A leaf is a wall to the boot and to the eye, so the ground has to be replayed or the map and the
        // sim disagree about what is standing — the lesson #465 paid for on the ship's own hatches.
        RebuildSurfaceDeck();
        RendererInterop.PlayCue("fire");
        SayItWhereTheyAreLooking(LockedDoor.LockShotLine);
        LogAutopilotEvent(LockedDoor.LockShotLine);
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>One leaf, as the string a rebuild recognises it by. Its own four coordinates and nothing
    /// else — the same shape and the same reason as <c>HiveInterior.LockKey</c>: a site carries several doors
    /// that are the same object to look at, the generators are pure and deterministic per (body, site), so
    /// one leaf keys identically on every rebuild, and a captain who shot one of them has not shot the
    /// others.</summary>
    private static string LeafKey(in DeckPlan.Door d) =>
        FormattableString.Invariant($"{d.X1:F2},{d.Y1:F2},{d.X2:F2},{d.Y2:F2}");

    /// <summary>
    /// #563 · PUT THE HOLES BACK. Called at the end of every rebuild, after the last composer has appended
    /// its doors, so the fresh plan carries the same destroyed leaves the old one did.
    ///
    /// <para>This is the half of "terminal" that is easy to leave out and impossible to notice while
    /// testing a single frame: the shot itself calls <c>RebuildSurfaceDeck</c>, so without this the leaf
    /// would be back before the captain had finished reading the line about it being gone.</para>
    /// </summary>
    private void ReplayShotLeaves(SurfaceExcursion ex)
    {
        if (ex.LeavesShotOpen.Count == 0)
        {
            return;   // the ordinary case, and it stays exactly as cheap as it was
        }
        DeckPlan.Door[] doors = _deckPlan.Doors;
        for (int i = 0; i < doors.Length; i++)
        {
            if (ex.LeavesShotOpen.Contains(LeafKey(doors[i])))
            {
                _deckPlan.ShootTheLeaf(i);
            }
        }
    }
}
