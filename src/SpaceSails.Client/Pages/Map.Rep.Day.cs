using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// ONE FRAME OF HIS WORKING DAY — called once a frame from the surface tick, beside the room's other
/// metabolism. Does nothing at all unless the rota has him on this ground and the captain is standing in
/// a room with people in it.
///
/// <para>Whether he is afoot, whether to send him in, where he stands next, and when his shift is over.</para>
///
/// <para>Split out of <c>Map.Rep.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── One frame of his working day ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called once a frame from the surface tick, beside the room's other metabolism. Does nothing at all
    /// unless the rota has him on this ground and the captain is standing in a room with people in it.
    /// </summary>
    private void AdvanceTheRep(double dtRealSeconds)
    {
        if (_surface is not { Floor: < 0 } ex)
        {
            EnsureRepVisit(null);
            return;
        }

        EnsureRepVisit(ex.Stop.Body.Id);
        if (!_repWorkingHere || !TheCanteenOn(ex, out UndergroundComplex.Amenity amenity))
        {
            return;
        }

        // The shift turning over takes every walker off the floor, his included. Re-read the world rather
        // than trusting a field: the walker list is the truth about who is on their feet.
        if (TheRepAfoot(ex.Walkers) is null)
        {
            if (_repCard is not null)
            {
                // His card cannot outlive his body — a panel with nobody behind it is the exact state
                // #731's escort branch was written to refuse.
                CloseTheRepsCard();
            }

            _ = SendTheRepIn(ex, amenity, dtRealSeconds);
            return;
        }

        MaybeSayHeIsOnlyPassing(ex.Walkers);
        _ = dtRealSeconds;   // his stepping is AdvanceWalkers' job; this method only decides errands
    }

    /// <summary>The walker that is him, if he is on the floor. By errand, because his plate is his own.
    /// <para>#973 L0 · Handed the list rather than the excursion: he works two rooms now.</para></summary>
    private static Walker? TheRepAfoot(IReadOnlyList<Walker> afoot)
    {
        foreach (Walker w in afoot)
        {
            if (w.For is Errand.RepRounds or Errand.RepPitching or Errand.RepLeaving)
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>
    /// PUT HIM ON THE FLOOR, or move him along it. The first walk of a visit comes in through a door —
    /// #731's idiom, and the same one the haulier uses — and every walk after it begins at his own feet.
    ///
    /// <para>#1061 · The order below IS his working day, and it is written as a fall-through rather than as a
    /// state machine because each clause is a reason the one under it does not apply. He comes to the
    /// captain's table only once the captain has watched him work the room; otherwise he stands at the
    /// counter, then at somebody's table, then at somebody else's; and when there is nobody left to work he
    /// goes off shift. His approach to the CAPTAIN is untouched by any of it — same gate, same card, same
    /// memory of having been told no.</para>
    /// </summary>
    private bool SendTheRepIn(SurfaceExcursion ex, UndergroundComplex.Amenity amenity, double dt)
    {
        if (_repShiftOver || ex.Walkers.Count >= WalkerBand || SimTime < _repMoveOnAt)
        {
            return false;
        }

        _ = dt;
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        // #731 (B1 canteen) · …and the walked-in are in chairs too. One opinion about every top in the room,
        // which is why this reads the room's BOTH halves of churn and not just who stood up.
        IReadOnlyList<CanteenRegulars.TableSeat> tops = CanteenRegulars.Tables(
            ex.Stop.Body.Id, ex.Floor, amenity, ex.CanteenWatch, ex.HallStoodUp, ex.HallCameIn);

        // #1061 · Egress.SEATED and not Egress.OnTheSchedule: a salesman's round asks who is sitting there to
        // be stood beside, not who the shift may give legs to. The crowd is who an insurance man sells to,
        // and standing at their table gives them nothing to run — see the note on OnTheSchedule.
        TheRoundHeIsWorking(
            ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, () => Egress.Seated(tops));

        // He crosses to the table only when the captain is sitting alone at one, has not already sent him
        // away this visit — and has had time to watch him work the room first (#1061).
        if (TheCaptainIsSittingAlone(out int tableIndex)
            && _repMemory.MayApproach(_repVisitIndex)
            && TheCaptainHasWatchedHimWork
            && TopOn(ex, amenity, tableIndex) is { } top
            && ChairOppositeTheCaptain(in top, walls) is { } beside)
        {
            return PlanTheRep(ex, floor, walls, beside, Errand.RepPitching, tableIndex,
                              NpcWalk.NoPersonalSpace);
        }

        // The counter first, because that is where he says he will be.
        if (!_repStoodAtTheCounter)
        {
            _repStoodAtTheCounter = true;
            if (TheCounterOn(amenity, walls) is { } counter && !HeIsAlreadyStandingAt(counter))
            {
                return PlanTheRep(ex, floor, walls, counter, Errand.RepRounds, -1,
                                  NpcWalk.PersonalSpaceInRadii);
            }
        }

        // …and then the marks, in the order this watch dealt them.
        if (TheMarkHeIsOn is { } mark)
        {
            if (TheTopNumbered(tops, mark.Index) is { } theirs
                && WhereABodyStandsAt(in theirs, walls) is { } at
                && !HeIsAlreadyStandingAt(at)
                && PlanTheRep(ex, floor, walls, at, Errand.RepRounds, mark.Index,
                              NpcWalk.PersonalSpaceInRadii))
            {
                return true;
            }

            // Their top has gone (they finished and left), the stone allows nobody beside it, the floor has
            // no route to it, or he is already standing there. A man does not queue for a table nobody is at,
            // and a mark retried every frame for a whole watch is a salesman in a loop: it is worked.
            _repMarksWorked++;
            return false;
        }

        return HeGoesOffShift(ex, floor, walls);
    }

    /// <summary>Plan one of his walks. He starts from where he is standing if he is already working this room,
    /// and from the doorstep of a door he does not have to be let through if this is his entrance.</summary>
    private bool PlanTheRep(
        SurfaceExcursion ex, UndergroundComplex.FloorPlan floor,
        IReadOnlyList<SurfaceCollision.Segment> walls, DeckReachability.Point to, Errand errand, int table,
        double berth)
    {
        if (WhereHeSetsOffFrom(floor.Locked, walls, ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, to)
            is not { } from)
        {
            return false;
        }

        if (OnFoot(NebulaRep.Plate, new NpcWalk.Bound("", to.X, to.Y), from, walls, berth) is not { } walk)
        {
            // He cannot get there from where he is standing. He is not left frozen mid-floor: the next frame
            // starts him from a doorstep again, which is the one beginning this room always has for him.
            _repStandingAt = null;
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = walk, Table = table, For = errand });
        StateHasChanged();
        return true;
    }

    /// <summary>#1061 · <b>THE ROOM IS WORKED, SO HE GOES.</b> Out through a leaf the captain's own TRY is
    /// refused at, on <see cref="Egress.DoorFor"/>'s answer off the frozen watch — the same door, the same
    /// call and the same plate as his way in, so the salesman never leaves through a leaf he was never behind.
    ///
    /// <para>The shift is over whether or not the floor gives him a way out of it. A room with no locked leaf
    /// is a room he simply stops working in, which is the honest answer and never a body left drifting
    /// between fixtures for the rest of a watch.</para>
    ///
    /// <para>Nothing is said. He is not at the counter the next time you look.</para></summary>
    private bool HeGoesOffShift(
        SurfaceExcursion ex, UndergroundComplex.FloorPlan floor,
        IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        _repShiftOver = true;
        if (_repStandingAt is not { } from)
        {
            return false;
        }

        int index = Egress.DoorFor(
            ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, NebulaRep.ContactId, floor.Locked);
        if (index < 0 || index >= floor.Locked.Count)
        {
            return false;
        }

        UndergroundComplex.LockedDoor door = floor.Locked[index];
        if (Egress.StandingPlaceAt(in door, DeckPlan.AvatarRadius, walls, from.X, from.Y) is not { } doorstep
            || OnFoot(NebulaRep.Plate, new NpcWalk.Bound(door.Sign, doorstep.X, doorstep.Y), from, walls)
                is not { } away)
        {
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = away, Table = -1, For = Errand.RepLeaving });
        StateHasChanged();
        return true;
    }

    /// <summary>#1061 · The one standing place at the counter this hall allows, or null. Read off the floor's
    /// published fixtures and never carved here — a second list of where the counter is would be this repo's
    /// oldest bug class with a salesman leaning on it.</summary>
    private static DeckReachability.Point? TheCounterOn(
        UndergroundComplex.Amenity amenity, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (amenity.Hall is not { } hall)
        {
            return null;
        }

        foreach (UndergroundComplex.CounterPlace place in hall.CounterRow)
        {
            if (!place.Seated && !SurfaceCollision.Blocked(place.X, place.Y, DeckPlan.AvatarRadius, walls))
            {
                // One standing place at the counter is the bar; the rest of the row is other people's.
                return new DeckReachability.Point(place.X, place.Y);
            }
        }

        return null;
    }

    /// <summary>#1061 · One of the hall's tops by the ordinal the round names — a lookup against the list the
    /// room was drawn from, never a second geometry.</summary>
    private static CanteenRegulars.TableSeat? TheTopNumbered(
        IReadOnlyList<CanteenRegulars.TableSeat> tops, int index)
    {
        foreach (CanteenRegulars.TableSeat top in tops)
        {
            if (top.Index == index && top.Taken)
            {
                return top;
            }
        }

        return null;
    }
}
