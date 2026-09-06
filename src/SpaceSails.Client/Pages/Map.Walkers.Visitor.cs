using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #731 · THE OTHER DIRECTION: SOMEBODY COMES TO YOU — the visitor's walk to the captain's table and her
/// walk back out of the room. Both are refusable: when the floor has no locked leaf, no standing place at
/// the one it dealt, no free chair or no way through, the caller does the arrival the old way rather than
/// teleporting somebody over a walk that could not be walked. Split out of <c>Map.Walkers.cs</c> under
/// #251 with no member renamed, re-scoped or re-ordered.
/// </summary>
public partial class Map
{
    // ── #731 · THE OTHER DIRECTION: SOMEBODY COMES TO YOU ────────────────────────────────────────────────
    //
    // Owner, 2026-08-06: "In the space bars there are lot of cases where we can have npcs arrive at bar from
    // locked place or go to a locked place. Now it is possible to have NPC ask to sit down at our table and
    // offer a quest! This is the classic TTRPG event."
    //
    // #865's strip already has the rule — somebody came to you, therefore a card — and this changes nothing
    // about it. What changes is the sentence before it: she USED to become true where she stood, and now
    // she comes out of a door and crosses the floor on the captain's own lattice, and the card is raised on
    // the frame she reaches the chair. WHICH door is Egress's, off the frozen watch, and it is one the
    // captain's own TRY is refused at — which is the whole cold open, and nothing says a word about it.

    /// <summary>#731 · Try to walk the visitor over. False when this floor has no locked door, no standing
    /// place at the one it dealt, no free chair, or no way through — in which case the caller does the
    /// arrival the old way rather than teleporting her over a walk that could not be walked.</summary>
    private bool WalkSomebodyToYourTable(SurfaceExcursion ex, int tableIndex)
    {
        // A PRESS CAN GET HERE BEFORE THE FRAME DOES — a captain who sits down and waits on the first tick of
        // an excursion puts somebody on their feet before AdvanceWalkers has ever run, and the frame that
        // followed used to look at an unstamped shift, decide the room had turned over, and clear her off the
        // floor mid-stride. So the room is brought up to date HERE too, through the one method that owns what
        // forgetting means.
        ForgetWalkersIfTheShiftTurned(ex);

        if (ex.Floor >= 0 || ex.Walkers.Count >= WalkerBand
            || !TheCanteenOn(ex, out UndergroundComplex.Amenity a))
        {
            return false;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        IReadOnlyList<UndergroundComplex.LockedDoor> locked = floor.Locked;
        int which = Egress.ArrivalDoor(ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, tableIndex, locked);
        if (which < 0 || which >= locked.Count)
        {
            return false;
        }

        CanteenRegulars.TableSeat? found = TopOn(ex, a, tableIndex);
        if (found is not { } top)
        {
            return false;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        UndergroundComplex.LockedDoor door = locked[which];

        // THE CHAIR FIRST, AND THEN THE DOORSTEP — in that order, deliberately.
        //
        // A leaf has two sides and both of them can be standable: the hall, and whatever the building put
        // behind the door. Asked with no hint, Egress.StandingPlaceAt answers with one of them, and half the
        // time that is the room the captain has never been in — from which there is no route to the table at
        // all, and the whole arrival silently falls back to the old teleport. So she is stepped out onto the
        // side of the door THE TABLE IS ON, which is the side a person walking into this hall comes out on.
        if (ChairOppositeTheCaptain(in top, walls) is not { } chair
            || Egress.StandingPlaceAt(in door, DeckPlan.AvatarRadius, walls, chair.X, chair.Y)
                is not { } from)
        {
            return false;
        }

        // …and she gives the captain NO berth: she is coming to them. See OnFoot.
        if (OnFoot(
                SittingAlone.VisitorPlate, new NpcWalk.Bound(door.Sign, chair.X, chair.Y), from, walls,
                NpcWalk.NoPersonalSpace)
            is not { } walk)
        {
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = walk, Table = tableIndex, For = Errand.Arriving });
        StateHasChanged();
        return true;
    }

    /// <summary>
    /// #731 · …AND THE SCENE ENDS THE WAY THE OWNER SAID IT SHOULD. <i>"If they go behind a door that is
    /// locked to us, we use that as 'I guess that concludes the conversation' point in the plot /
    /// situation."</i>
    ///
    /// <para>This is the TRIGGERED half of the issue's own proposal, and it is the same walker as the
    /// scheduled one — one class, two reasons to use it. She stands up from the chair she crossed the room to
    /// and goes back out through <b>the door she came out of</b>: <see cref="Egress.ArrivalDoor"/> is asked
    /// again, off the same frozen watch and the same top, so the provenance holds in both directions and
    /// nobody leaves the building through a door they were never behind.</para>
    ///
    /// <para>Not one word is filed about it. The panel simply becomes the captain's own table again, and the
    /// room says the rest by walking her out of it.</para>
    /// </summary>
    private bool WalkTheVisitorOut(SurfaceExcursion ex, int tableIndex)
    {
        // …and the same reason as the arrival's: a press may reach this before any frame has stamped the
        // shift, and an unstamped shift is one the next frame decides has turned over.
        ForgetWalkersIfTheShiftTurned(ex);

        if (ex.Floor >= 0 || ex.Walkers.Count >= WalkerBand
            || !TheCanteenOn(ex, out UndergroundComplex.Amenity a))
        {
            return false;
        }

        IReadOnlyList<UndergroundComplex.LockedDoor> locked =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Locked;
        int which = Egress.ArrivalDoor(ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, tableIndex, locked);
        if (which < 0 || which >= locked.Count || TopOn(ex, a, tableIndex) is not { } top)
        {
            return false;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        if (ChairOppositeTheCaptain(in top, walls) is not { } from)
        {
            return false;
        }

        UndergroundComplex.LockedDoor door = locked[which];
        if (Egress.StandingPlaceAt(in door, DeckPlan.AvatarRadius, walls, from.X, from.Y) is not { } at)
        {
            return false;
        }

        // …and no berth on the way out either: she is standing up from the captain's OWN table, a chair away
        // from them, and a body that froze there would be a scene that never ends.
        if (OnFoot(
                SittingAlone.VisitorPlate, new NpcWalk.Bound(door.Sign, at.X, at.Y), from, walls,
                NpcWalk.NoPersonalSpace)
            is not { } walk)
        {
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = walk, Table = tableIndex, For = Errand.Leaving });
        StateHasChanged();
        return true;
    }
}
