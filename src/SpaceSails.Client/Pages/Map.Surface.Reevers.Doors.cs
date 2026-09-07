using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the Old Ones and a door.
//
// #563 · THE OLD ONES USE DOORS (owner ruling, 2026-09-06). The fiction is in Core ReeverDoor and in
// docs/worldbuilding-notes.md §10; the state a leaf carries while it is being hauled is on the plan
// (DeckPlan.Leafs), because the pen has to learn it in the same instant the sim does. What is left is this
// file: WHICH leaf, and WHO has hands on it.
//
// THE BUG THIS FIXES, stated plainly. The chase reads `_deckPlan.CollisionField` — stone — by law, because
// a door is not collision: the passage is always walkable and a boot is stopped by stone, never by a leaf.
// That law was written for the CAPTAIN, and it was handed to the Old Ones by inheritance. So a Reever
// crossed a shut leaf as though the doorway were an empty hole, at walking pace, mid-stride: you shut a
// hatch in its face and it walked through the picture of a closed door. Every other consumer of a door had
// already been fixed — the eye (#465), the round (#466), the beam (#1099), the sleeper's lamp (#1154) —
// and the legs were the last reader still asking the wrong list.
//
// The fix is not a new mechanism. Their legs get the list the rest of them already had: stone PLUS whatever
// is shut this instant. Everything the ruling asks for then falls out of machinery that is already here and
// already tested:
//
//   · a LOCKED leaf is a wall to them — the legs stall on it (ReeverChase's Spent) or take its handrail
//     round, which is exactly "it waits on the far side, or goes round the way a walker would";
//   · an UNLOCKED shut leaf stops them too, so they stand at it — and this file gives that stand a beat,
//     at the end of which the leaf is over and stays over.
public partial class Map
{
    // One segment, reused. Both questions this file asks — "is a body within reach of THIS leaf" and "does
    // THIS leaf stand between a body and where it is going" — are ordinary SurfaceCollision queries against
    // a list of exactly one, so they are asked of the same collision law everything else in the game is,
    // rather than re-derived here with a second set of arithmetic. It is a field and not a local because
    // this runs every frame over every door on nine welded tiles.
    private readonly SurfaceCollision.Segment[] _oneLeaf = new SurfaceCollision.Segment[1];

    /// <summary>
    /// #563 · <b>THE LIST THE OLD ONES WALK AGAINST: stone, plus whatever is shut.</b>
    ///
    /// <para>It is <see cref="SightBlockers"/> itself, deliberately and not by coincidence — the same object,
    /// the same indexed grid, the same memoization. For an Old One the eye's list and the legs' list are now
    /// ONE list, and that is the ruling rather than an optimisation: <i>they do not see through a closed
    /// door</i> (#442/#1154) and <i>a door is a door to them</i> (#563) are the same sentence about the same
    /// leaf. Two lists here is precisely the arrangement that produced a gun shooting through a shut hatch
    /// (#465), a beam through one (#1099) and a sleeper drawn through one (#1154): a leaf that stops one
    /// faculty and not another.</para>
    ///
    /// <para><b>The captain keeps his own list and it does not move.</b> His boots are stepped against
    /// <c>_deckPlan.CollisionField</c> everywhere they were, because a door he walks up to opens for him —
    /// that is what an automatic door IS, and #465's law that a leaf never stops a boot is his law. This
    /// changes what walks on the far side of it.</para>
    ///
    /// <para><b>And it is the list on EVERY ground</b> (owner ruling, 2026-09-06 — <i>the Reevers do not see
    /// through a closed door on the moon either</i>). When this was written, <c>StepReevers</c> still kept a
    /// second local for the eye that was this list aboard a wreck and the bare stone anywhere else, so a hut
    /// door stopped their boot and not their look. That local is gone: <c>StepReevers</c> takes THIS list
    /// once and hands it to the legs, the sleeper's lamp and the observation roll alike. A regolith site with
    /// nothing shut on it answers exactly what the stone answered, which is why no moon moved by an
    /// inch.</para>
    /// </summary>
    private IReadOnlyList<SurfaceCollision.Segment> TheirLegs() => SightBlockers();

    /// <summary>The leaf's own width, which is the distance it has to travel to be open — the numerator of
    /// the beat (<see cref="ReeverDoor.HaulSeconds"/>).</summary>
    private static double LeafWidth(in DeckPlan.Door d)
    {
        double dx = d.X2 - d.X1, dy = d.Y2 - d.Y1;
        return System.Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>
    /// #563 · <b>ONE FRAME OF HAULING.</b> Walk the doors; for each leaf that may be worked at all and is
    /// shut this instant, ask whether anything has its hands on it, and bank the frame or let it go.
    ///
    /// <para>Called at the top of <c>StepReevers</c>, <b>before</b> the legs' list is taken, so a leaf that
    /// comes over on this frame is open to the same frame's step, its sight and its rounds — never a frame
    /// behind the picture. That ordering is the whole of "one source of truth or none".</para>
    ///
    /// <para>Nothing is drawn, said or logged here. The leaf sliding on its own with nothing behind it yet
    /// is the entire telling (canon point 4), so this method publishes no string of any kind.</para>
    /// </summary>
    private void StepLeafWork(double dt)
    {
        DeckPlan.Door[] doors = _deckPlan.Doors;
        double reach = ReeverDoor.Reach(DeckPlan.AvatarRadius);
        for (int i = 0; i < doors.Length; i++)
        {
            DeckPlan.Door d = doors[i];
            if (_deckPlan.LeafHeldOpen(i))
            {
                continue;   // over for good — they do not close doors behind them
            }
            if (!ReeverDoor.MayWork(d.Locked, d.Interlock != 0, _deckPlan.DoorwayIsWalledUp(d)))
            {
                continue;   // a locked leaf, an airlock's leaf, or a picture in front of stone
            }
            if (!IsDoorShut(d, i))
            {
                // The captain is standing at it and it has retracted for him. There is nothing to haul, and
                // a beat banked while it was open would be a beat nobody spent.
                _deckPlan.LetGoOfLeaf(i);
                continue;
            }

            _oneLeaf[0] = new SurfaceCollision.Segment(d.X1, d.Y1, d.X2, d.Y2);
            if (SomethingHasHandsOnThisLeaf(reach))
            {
                _deckPlan.HaulLeaf(i, dt, ReeverDoor.HaulSeconds(LeafWidth(d), ReeverSpeed));
            }
            else
            {
                _deckPlan.LetGoOfLeaf(i);
            }
        }
    }

    /// <summary>
    /// Is one of them at <see cref="_oneLeaf"/>, and does it actually want through? Two conditions, and both
    /// of them are asked of the collision law rather than invented here:
    ///
    /// <list type="number">
    /// <item><b>Within arm's length of the leaf</b> — <see cref="ReeverDoor.Reach"/>, one body-width, the
    /// same distance at which it could lay hands on the captain.</item>
    /// <item><b>The leaf stands between it and where it is going.</b> A contact merely shambling past a
    /// doorway on its way somewhere else does not work the door — that would put every hut on a moon open
    /// within a minute of anything walking near it, and the leaf standing open would stop meaning anything.
    /// It is the same sightline test the rest of the file uses, asked of one segment: no line from the body
    /// to the spot it is hunting means that segment is in the way.</item>
    /// </list>
    ///
    /// <para>A dormant one is folded down and going nowhere; one that has never laid eyes on the captain
    /// keeps its own ground by law (#446) and has nowhere to be; a sentry-pinned one is held where it stands
    /// (#314) and is not reaching for anything. None of the three is at a door.</para>
    /// </summary>
    private bool SomethingHasHandsOnThisLeaf(double reach)
    {
        foreach (Reever r in _reevers)
        {
            if (r.Dormant || !r.EverSeen || PinnedBySentry(r))
            {
                continue;
            }
            if (!SurfaceCollision.Blocked(r.X, r.Y, reach, _oneLeaf))
            {
                continue;   // not at this leaf
            }
            if (SurfaceCollision.HasLineOfSight(r.X, r.Y, r.LastSeenX, r.LastSeenY, _oneLeaf))
            {
                continue;   // the leaf is beside its road, not across it
            }
            return true;
        }
        return false;
    }
}
