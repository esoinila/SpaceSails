using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.BarWalkers (the header note lives in Map.BarWalkers.cs) — #973 L0 · HARLAN FESS WORKS
// STATION BARS TOO. #976 keyed his presence rota on the BODY being visited rather than on a berth id, and
// said why: the law — at most one place in three, never two visits running — is unchanged and lives in
// Core. A docked haven IS a body, so not one line of NebulaRep changes to put him in a station bar; what
// was missing was a room with a floor in it. This is his working day ashore: nothing at all unless the
// rota has him at this berth and the captain is in the room, then the walk in, the walk across, and going
// off shift — plus the two lookups that say who is seated where when the round is named.
public partial class Map
{
    // ── #973 L0 · HARLAN FESS WORKS STATION BARS TOO ─────────────────────────────────────────────────────
    //
    // #976 keyed his presence rota on the BODY being visited rather than on a berth id, and said why: "the law
    // (at most one place in three, never two visits running) is unchanged and lives in Core." A docked haven
    // IS a body, so not one line of NebulaRep changes to put him in a station bar — what was missing was a
    // room with a floor in it, which is the rest of this file.

    /// <summary>#973 L0 · One frame of his working day, ashore. Nothing at all unless the rota has him at this
    /// berth and the captain is standing in the bar — a salesman working a room the customer is not in is a
    /// figure nobody will ever see walk.</summary>
    private void AdvanceTheRepAshore(in HavenInterior.BarFloor bar)
    {
        EnsureRepVisit(bar.BodyId);
        if (!_repWorkingHere || !InTheBar(in bar))
        {
            return;
        }

        if (TheRepAfoot(_barAfoot) is null)
        {
            if (_repCard is not null)
            {
                // His card cannot outlive his body — the state #731's escort branch was written to refuse.
                CloseTheRepsCard();
            }

            _ = SendTheRepIntoTheBar(in bar);
            return;
        }

        MaybeSayHeIsOnlyPassing(_barAfoot);
    }

    /// <summary>
    /// #973 L0 · PUT HIM IN THE BAR, or move him along it. The same working day as underground, said in this
    /// room's geometry: the counter, then the marks this watch dealt him, then the captain once the captain
    /// has watched him work, and then out through a leaf.
    ///
    /// <para>The crossing to the CAPTAIN goes through <see cref="ApproachTheTable"/> and not through a second
    /// planner, which is the point of that hook existing: whatever L5b's walk-in ends up being, it and the
    /// salesman reach the captain's table by the same legs.</para>
    ///
    /// <para>#1061 · The order below is <c>SendTheRepIn</c>'s, clause for clause, because it is one man's
    /// working day and not two. What differs is only where the counter and somebody else's table ARE — which
    /// is a fact about a room, and the room is the thing that answers it.</para>
    /// </summary>
    private bool SendTheRepIntoTheBar(in HavenInterior.BarFloor bar)
    {
        if (_repShiftOver || _barAfoot.Count >= WalkerBand || SimTime < _repMoveOnAt)
        {
            return false;
        }

        IReadOnlyList<HavenInterior.SeatedRegular> rota =
            HavenInterior.ResolveRegulars(bar.BodyId, _dockVisitSimTime, TheBarsChurn);
        TheRoundHeIsWorking(bar.BodyId, BarIsNotAFloor, BarWatch, () => TheBarsSeated(rota));

        // He crosses to the table only when there IS somebody sitting alone at one, he has not been sent away
        // this visit — and they have had time to watch him work the room first (#1061).
        if (_repMemory.MayApproach(_repVisitIndex)
            && TheCaptainHasWatchedHimWork
            && TheCaptainIsSittingAloneInTheBar()
            && ApproachTheTable(NebulaRep.Plate, TheCaptainIsSittingAloneInTheBar, HeReachesYourTable))
        {
            return true;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;

        // The counter first, because that is where he says he will be.
        if (!_repStoodAtTheCounter)
        {
            _repStoodAtTheCounter = true;
            if (TheFirstFreeFixture(in bar, walls) is { } counter && !HeIsAlreadyStandingAt(counter))
            {
                return WalkTheRepAcrossTheBar(
                    in bar, walls, counter, Errand.RepRounds, -1, NpcWalk.PersonalSpaceInRadii);
            }
        }

        // …and then the marks, in the order this watch dealt them.
        if (TheMarkHeIsOn is { } mark)
        {
            if (TheRegularSeatedAt(rota, mark.Index) is { Present: true } theirs
                && BesideThisTopClearOfTheCaptain(
                       new DeckReachability.Point(theirs.X, theirs.Y), walls) is { } at
                && !HeIsAlreadyStandingAt(at)
                && WalkTheRepAcrossTheBar(
                       in bar, walls, at, Errand.RepRounds, mark.Index, NpcWalk.PersonalSpaceInRadii))
            {
                return true;
            }

            // They stood up and went, the stone allows nobody beside their chair, the room has no route to
            // it, or he is already standing there. A man does not queue for a chair nobody is in, and a mark
            // retried every frame for a whole watch is a salesman in a loop: it is worked.
            _repMarksWorked++;
            return false;
        }

        return HeGoesOffShiftFromTheBar(in bar, walls);
    }

    /// <summary>#1061 · One leg of his round in a station bar — from his own feet if he is already working
    /// this room, and from the doorstep of the leaf this watch deals him if this is his entrance. The same
    /// two beginnings <c>PlanTheRep</c> has underground, asked through the one answer both rooms share.</summary>
    private bool WalkTheRepAcrossTheBar(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls,
        DeckReachability.Point to, Errand errand, int table, double berth)
    {
        if (WhereHeSetsOffFrom(bar.Doors, walls, bar.BodyId, BarIsNotAFloor, BarWatch, to) is not { } from)
        {
            return false;
        }

        if (OnFoot(NebulaRep.Plate, new NpcWalk.Bound("", to.X, to.Y), from, walls, berth) is not { } walk)
        {
            // He cannot get there from where he is standing. The next frame starts him at a doorstep again,
            // which is the one beginning this room always has for him.
            _repStandingAt = null;
            return false;
        }

        _barAfoot.Add(new Walker { Walk = walk, Table = table, For = errand });
        StateHasChanged();
        return true;
    }

    /// <summary>#1061 · <b>THE BAR IS WORKED, SO HE GOES</b> — out through the leaf
    /// <see cref="Egress.DoorFor"/> deals him off the frozen docking watch, from where he is standing, exactly
    /// as <see cref="TheyLeaveTheBar"/> walks out a stranger who has been refused. Nothing is said.</summary>
    private bool HeGoesOffShiftFromTheBar(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        _repShiftOver = true;
        if (_repStandingAt is not { } from)
        {
            return false;
        }

        // #1061 · His OWN key — the contact id, the same one <see cref="WhereHeSetsOffFrom"/> deals his way in
        // with, in both rooms. So the leaf he goes out of is the leaf he came in through, and nobody in this
        // bar ever leaves by a door they were never behind.
        int which = Egress.DoorFor(bar.BodyId, BarIsNotAFloor, BarWatch, NebulaRep.ContactId, bar.Doors);
        if (which < 0 || which >= bar.Doors.Count)
        {
            return false;
        }

        UndergroundComplex.LockedDoor leaf = bar.Doors[which];
        if (Egress.StandingPlaceAt(in leaf, DeckPlan.AvatarRadius, walls, from.X, from.Y) is not { } doorstep
            || OnFoot(NebulaRep.Plate, new NpcWalk.Bound(leaf.Sign, doorstep.X, doorstep.Y), from, walls)
                is not { } away)
        {
            return false;
        }

        _barAfoot.Add(new Walker { Walk = away, Table = -1, For = Errand.RepLeaving });
        StateHasChanged();
        return true;
    }

    /// <summary>#1061 · WHO THIS BAR ACTUALLY HAS IN IT, as the round needs them — the rota's own answer with
    /// this evening's churn over the top of it, projected onto <see cref="Egress.Occupant"/>. The same shape
    /// <see cref="Egress.Seated"/> gives a canteen hall, so one arithmetic works both rooms.</summary>
    private static IReadOnlyList<Egress.Occupant> TheBarsSeated(
        IReadOnlyList<HavenInterior.SeatedRegular> rota)
    {
        var seated = new List<Egress.Occupant>(rota.Count);
        for (int i = 0; i < rota.Count; i++)
        {
            if (rota[i].Present)
            {
                seated.Add(new Egress.Occupant(i, rota[i].ShortName));
            }
        }

        return seated;
    }

    /// <summary>#1061 · The regular the round names, by their place in the rota's own list — a lookup and
    /// never a second seating.</summary>
    private static HavenInterior.SeatedRegular? TheRegularSeatedAt(
        IReadOnlyList<HavenInterior.SeatedRegular> rota, int index) =>
        index >= 0 && index < rota.Count ? rota[index] : null;
}
