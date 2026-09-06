using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #731 · WHERE A BODY GOES AND HOW IT GETS THERE — the one planner every errand on this side shares
/// (<see cref="OnFoot"/>, which is why <c>Gait.Person</c> is claimed exactly once in this family) plus the
/// small questions about a floor that the errands ask before they commit: is there a canteen on it, which
/// top is that, which chair is free and farthest from the captain, and where does a body standing up from
/// a top actually stand. Split out of <c>Map.Walkers.cs</c> under #251 with no member renamed, re-scoped
/// or re-ordered.
/// </summary>
public partial class Map
{
    /// <summary>
    /// #731 · PLAN SOMEBODY'S WALK — the one place on this side that says who is walking.
    ///
    /// <para>FIVE errands use it — a regular finishing, the visitor arriving, the visitor going, the contact
    /// walking you into a cabinet (#731 v2), and a sweep team filing out of a wreck's airlock (#731 v2) — and
    /// they hand it five different plates and five different destinations, but they are all the same kind of
    /// body: a PERSON, at the captain's own width, on the captain's own lattice. So the gait is claimed
    /// exactly once here rather than five times — <c>AJambIsNotASealedDoorTests</c> counts every line in the
    /// shipping source that claims <c>Gait.Person</c>, and the owner's ruling behind that count (<i>"Lets not
    /// help reevers move in any easier if possible"</i>) is easier to keep when one file says it once.</para>
    ///
    /// <para><paramref name="berth"/> is the one thing the three errands disagree about, and it is a fact
    /// about WHOSE errand it is. A regular walking to a staff door gives the captain a body-width and stops
    /// dead if you stand in it, because you are in the way and that is the beat. The two walks that begin or
    /// end at the captain's own table give none, because the captain is not in the way at their own table —
    /// they are the reason for the walk. See <see cref="NpcWalk.NoPersonalSpace"/>.</para>
    ///
    /// <para><paramref name="pace"/> is the second: a regular finishing a drink and a black-ops team working
    /// a hull at their own clip are the same BODY at two different speeds, and the speed is the caller's
    /// because it is a fact about the errand. Everything else — the width, the lattice, the stone, the
    /// gait — is one law for all five.</para>
    ///
    /// <para>Null when the floor does not connect the two ends. That is the reachability audit's own verdict
    /// and never a reason to place the figure at the far end anyway.</para>
    /// </summary>
    private static NpcWalk? OnFoot(
        string plate, NpcWalk.Bound bound, DeckReachability.Point from,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        double berth = NpcWalk.PersonalSpaceInRadii,
        double pace = NpcWalk.PaceDu) =>
        NpcWalk.Plan(
            plate, bound, from, walls, DeckPlan.AvatarRadius, SurfaceCollision.Gait.Person,
            pace, berth);

    /// <summary>#731 · She is standing at your elbow. The strip's own rule takes over from here unchanged —
    /// #865's card, off #865's flag, said by #865's scene — and this only says WHEN.
    ///
    /// <para>The table is asked whether it is still the one she was crossing to. A captain who stood up, took
    /// their leave, or walked to another top while she was on her feet has ended the scene she was walking
    /// into, and she simply is not there when it is over — which is the honest answer and not a card raised
    /// over an empty chair.</para></summary>
    private void SomebodyHasReachedYourTable(SurfaceExcursion ex, Walker who) =>
        _seating.TheVisitorHasArrived(ex, who.Table);

    /// <summary>#731 · The free chair FARTHEST from the captain at a top they are sitting at — the seat
    /// opposite, which is where somebody who has come to talk to you sits down.
    ///
    /// <para>The ring is <c>CanteenRegulars.ChairAt</c>'s, through the top's own <c>Chair</c>, so this is a
    /// lookup and not a second geometry. Chairs the party is already in are skipped, and so is anything a
    /// body cannot stand on.</para></summary>
    private DeckReachability.Point? ChairOppositeTheCaptain(
        in CanteenRegulars.TableSeat top, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        DeckReachability.Point? best = null;
        double furthest = -1;
        for (int c = 0; c < top.Seats; c++)
        {
            if (top.PartyIn(c))
            {
                continue;
            }
            (double x, double y) = top.Chair(c);
            if (SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, walls))
            {
                continue;
            }
            double d = ((x - _avatarX) * (x - _avatarX)) + ((y - _avatarY) * (y - _avatarY));
            if (d > furthest)
            {
                furthest = d;
                best = new DeckReachability.Point(x, y);
            }
        }
        return best;
    }

    /// <summary>#731 · The room people sit in on this floor, if this floor is that floor. Core's own B1 law
    /// (<see cref="CanteenRegulars.PeopleSitHere"/>) asked rather than re-derived.</summary>
    private static bool TheCanteenOn(SurfaceExcursion ex, out UndergroundComplex.Amenity amenity)
    {
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            if (CanteenRegulars.PeopleSitHere(ex.Stop.Body.Id, ex.Floor, a))
            {
                amenity = a;
                return true;
            }
        }
        amenity = default;
        return false;
    }

    /// <summary>#731 · One top by its ordinal, off the same list the room was drawn from.</summary>
    private static CanteenRegulars.TableSeat? TopOn(
        SurfaceExcursion ex, UndergroundComplex.Amenity a, int tableIndex)
    {
        foreach (CanteenRegulars.TableSeat top in
            CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch, ex.HallStoodUp, ex.HallCameIn))
        {
            if (top.Index == tableIndex)
            {
                return top;
            }
        }
        return null;
    }

    /// <summary>#731 · Where a body standing up from a top actually stands: one of its own chairs, and the
    /// first one the stone allows. The ring is <c>CanteenRegulars.ChairAt</c>'s — never a second geometry —
    /// and a top whose every chair is against something is simply not a top anybody walks away from.</summary>
    private static DeckReachability.Point? WhereABodyStandsAt(
        in CanteenRegulars.TableSeat top, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        for (int c = 0; c < top.Seats; c++)
        {
            (double x, double y) = top.Chair(c);
            if (!SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, walls))
            {
                return new DeckReachability.Point(x, y);
            }
        }
        return null;
    }
}
