using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #563 · A LOCKED DOOR IS TIME, NEVER A KEY — AND ONE OF THE TIMES IS A ROUND.
///
/// <para>Owner ruling, 2026-09-13: <i>"I like the time instead of a key, considering we have firepower and
/// tools. We can create the same effect as needing a key by making it slow, too noisy, or dangerous in other
/// ways."</i> The slow road is the shoulder (<see cref="LockedDoor.ForceSeconds"/>, and it is heard every
/// second it runs — <see cref="TheHoldIsHeard"/>). <b>This file is the other one: the fast road.</b></para>
///
/// <para><b>[F] — SHOOT THE LOCK.</b> One round out of the hand-load, the door opens in the time it takes to
/// pull a trigger, <c>ReeverHearing.Noise.Gunfire</c> goes out at full earshot through the emitter every
/// other shot on this ground already uses, and <b>the leaf is destroyed</b>. Not open — gone. It can never be
/// shut again, never locked again, never leaned on again, and nothing behind you closes for the rest of the
/// excursion.</para>
///
/// <para><b>That is the danger, and it is the sentries' own law arriving at a doorway.</b> #314: <i>bots buy
/// time, never safety</i>. So does a round through a hasp — you have bought twenty-five seconds and paid for
/// them with every retreat that door would ever have covered, plus half the field's worth of ears. Nothing on
/// screen says any of that. The pack walking in through the hole later is the telling.</para>
///
/// <h3>Which door, and why only this one</h3>
///
/// <para>The verb takes a <see cref="LockedDoor.State"/> leaf — <b>the mountain lab's chamber doors</b>. That
/// is not a convenience, it is the honest answer to "where does a locked door actually stop a captain on a
/// ground he is armed on", and it was arrived at by sweeping the tree rather than by taste:</para>
///
/// <list type="bullet">
/// <item>A <c>DeckPlan.Door</c> leaf is built <c>Locked: true</c> in exactly two places — a haven's berth
/// hatches and the Hive's floors. Neither is a ground the captain carries the hand-load on, and both are
/// <c>DoorwayIsWalledUp</c> besides: a wall with a sign on it, which <c>ShootTheLock.Judge</c> has ruled
/// unshootable since #803 (<i>you cannot shoot a WALL</i>). Underground the verb that exists is the one that
/// already exists — a sentry, the handset, and <c>ShootTheLock</c>'s own six-round tariff.</item>
/// <item>Every OTHER leaf on a moon or a wreck retracts for the captain at <c>DeckPlan.DoorOpenRadius</c>
/// (4 du), which is further than he can reach to press anything (<c>InteractRadius</c>, 3 du). A door that is
/// already standing open by the time you are close enough to shoot it has no lock to shoot.</item>
/// <item>A sealed way — an expedition site's bolts, an outpost's dogged hatch — is not a lock either, by the
/// same #590 ruling. Those cost the shoulder, and now the noise with it.</item>
/// </list>
///
/// <para>So the one leaf in the game that is genuinely KEYED in front of an armed captain is a lab door
/// during the lockdown, which is precisely the scene the ruling was about: every door in the mountain keys at
/// once, and the answer is no longer a walk to a chair two rooms deeper than you are.</para>
/// </summary>
public partial class Map
{
    /// <summary>Rounds in the coat. THE HAND-LOAD IS THE ARMAMENT — there is no separate sidearm counter in
    /// this game and inventing one here would be a second answer to "what can the captain shoot with".
    /// Returns the first stack with enough in it, so the id that is spent is the id that is shown.</summary>
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
    private bool ArmedAtADoor =>
        _surface is { } ex && MoonSurface.ShovelWorksOnThisFloor(ex.Floor) && TheHandLoad() is not null;

    /// <summary>
    /// WHICH LOCK — the leaf under the captain's own hand, if it still has one. Asked through
    /// <see cref="NearestLabDoorId"/>, the same lookup the [E] press and the door's own plate already share,
    /// so the two keys can never come to two views of one doorway. Empty is "nothing here to shoot", and that
    /// is what keeps the plate off the row.
    /// </summary>
    private string TheLockUnderTheHand()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.LabDoor })
        {
            return string.Empty;
        }
        if (NearestLabDoorId() is not { Length: > 0 } id
            || !_labDoors.TryGetValue(id, out LockedDoor.State state))
        {
            return string.Empty;
        }
        return LockedDoor.MayShootTheLock(state, armed: true) ? id : string.Empty;
    }

    /// <summary>#563 · The plate, on the door prompt row, only where the verb exists — an affordance you
    /// cannot read is one you do not have (#212), and one that is read where it does not answer is worse
    /// (#723's "E — dig" over poured rockcrete). Null keeps the row exactly the length it was.</summary>
    private string? ShootTheLockPlate() =>
        ArmedAtADoor && TheLockUnderTheHand().Length > 0 ? $"🔫 {LockedDoor.ShootThePlate}" : null;

    /// <summary>
    /// [F]. One round, one door, and the door never comes back.
    ///
    /// <para>The order matters and is the order of the sentence: the lock goes, the round leaves the pocket,
    /// the ear is rung at the DOOR (a place to walk to is what a noise buys, #456), and then, once, the
    /// captain's own register says what was spent. The line is Fable-authored and said verbatim; the noise is
    /// never mentioned in it, because the game does not announce (#453/#456) and the pack arriving through
    /// the hole is the whole telling.</para>
    ///
    /// <para>The destroyed state persists exactly where every other door fact on this ground persists — the
    /// page's <c>_labDoors</c>, which the geometry is replayed from on every rebuild — so the leaf does not
    /// grow back on the rebuild this very method calls.</para>
    /// </summary>
    private void ShootTheLockNow()
    {
        if (_surface is null || AnySlowThingUnderYourHands)
        {
            return;
        }
        if (!ArmedAtADoor || TheHandLoad() is not { } load)
        {
            return;
        }
        if (TheLockUnderTheHand() is not { Length: > 0 } doorId
            || !_labDoors.TryGetValue(doorId, out LockedDoor.State was))
        {
            return;
        }

        _labDoors[doorId] = LockedDoor.Shoot(was);

        // The round leaves the pocket. One, and the satchel's own remover does it, so the stack, the id and
        // the count are kept by the one writer that keeps every other stack in the coat.
        _satchel = [.. Satchel.Remove(_satchel, Satchel.Kind.Rounds, load.Id, LockedDoor.RoundsToShootTheLock)];

        // THE NOISE, through the emitter every other shot on this ground uses — the same call
        // Map.Combat.Remote makes when a sentry fires, at the same Noise.Gunfire. Half the field learns a
        // PLACE and walks to it (ReeverHearing.RangeOf(Gunfire) = 34 du). The captain is standing at the
        // door, so his boots ARE the door's position to within an arm's length.
        MakeNoise(_avatarX, _avatarY, ReeverHearing.Noise.Gunfire);

        // A door is a WALL, so the ground has to be replayed or the map and the boot disagree about what is
        // standing — the lesson #465 paid for on the ship's own hatches. The lab's geometry adds a wall for
        // any leaf that is not Passable, and a destroyed one is passable forever, so the doorway simply is
        // not drawn: the pen and the sim read the one dictionary.
        RebuildSurfaceDeck();
        AlertSweepersToNoise(_avatarX, _avatarY);
        RendererInterop.PlayCue("fire");
        SayItWhereTheyAreLooking(LockedDoor.LockShotLine);
        LogAutopilotEvent(LockedDoor.LockShotLine);
        RequestVaultSave();
        StateHasChanged();
    }
}
