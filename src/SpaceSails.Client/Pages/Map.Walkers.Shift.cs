using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #731 · THE SHIFT DEALS ITS OWN COMINGS AND GOINGS — the half of the walker band driven by the frozen
/// watch rather than by a press. Forgetting a turned-over shift, asking <see cref="Egress"/> who goes and
/// who arrives, and putting each of those decisions on its feet: out of a top and through a staff door, or
/// out of the back and down onto a stool. Split out of <c>Map.Walkers.cs</c> under #251 with no member
/// renamed, re-scoped or re-ordered.
/// </summary>
public partial class Map
{
    /// <summary>#731 · A SHIFT TURNING OVER IS THE ROOM FORGETTING. Same rule the table state already runs
    /// under, and the same reason: what happened last watch happened to people who are not here now. A lift
    /// ride does it too — a floor you have left is a floor whose walkers are nobody's business.</summary>
    private static void ForgetWalkersIfTheShiftTurned(SurfaceExcursion ex)
    {
        if (ex.WalkersWatch == ex.CanteenWatch && ex.WalkersFloor == ex.Floor)
        {
            return;
        }
        ex.Walkers.Clear();
        ex.HallStoodUp.Clear();
        ex.HallDeparted.Clear();
        // #731 · …and the other direction with them. A shift turning over deals the room fresh, and somebody
        // who walked in early on the last watch is on the new watch's own sheet or is not here at all.
        ex.HallCameIn.Clear();
        ex.HallArrived.Clear();
        // #731 v2 · …and the conversation somebody was holding a door open for. A shift turning over is the
        // room forgetting, and a parked scene is the most forgettable thing in it: the woman it belonged to
        // went home three hours ago.
        ForgetTheEscort(ex);
        // …and the shift's own list of who goes, which belonged to the shift that has just ended. Null and
        // not empty: empty is an answer this room gave, null is a question it has not been asked yet.
        ex.HallSchedule = null;
        ex.HallArrivals = null;
        ex.WalkersWatch = ex.CanteenWatch;
        ex.WalkersFloor = ex.Floor;
    }

    /// <summary>
    /// #731 · WHO HAS FINISHED, BY NOW. The ambience half — <i>scheduled for ambience, triggered for plot
    /// beats; both through one walker</i>, which is the issue's own proposal and this is the scheduled side
    /// of it.
    ///
    /// <para>The schedule is <see cref="Egress.Departures"/>'s and is a function of the frozen watch, so it
    /// does not change while it is being read. What the frame contributes is only HOW FAR INTO THE SHIFT it
    /// is, and each move is dealt exactly once (<c>HallDeparted</c>) — a schedule re-read every frame must
    /// never send the same person out of the room twice.</para>
    ///
    /// <para><b>AND IT IS DEALT ONCE PER SHIFT, NOT ONCE PER FRAME.</b> Working out who goes needs the whole
    /// floor plan, and <c>UndergroundComplex.Build</c> generates a building — every wall, every room, every
    /// door — from scratch on every call. Asking it sixty times a second for an answer that cannot change
    /// until the watch turns over is Lab 45's own lesson with a body walking through it: the schedule is
    /// frozen by construction, so it is worked out on the first frame of a shift and then only READ. The
    /// per-frame cost of a room with nobody due is a null check and a count.</para>
    /// </summary>
    /// <summary>#731 · …AND THE SAME SHIFT DECIDES WHO TURNS UP. Both lists are worked out once when the
    /// watch begins on this floor and only read afterwards; both are stepped down by the same clock hand
    /// crossing the times the frozen watch already named.</summary>
    private void DealTheShiftsOwnHours(SurfaceExcursion ex)
    {
        // The shift's own lists, worked out once. See the note above on why this is not a micro-optimisation.
        ex.HallSchedule ??= TheShiftDecidesWhoGoes(ex);
        ex.HallArrivals ??= TheShiftDecidesWhoComes(ex);

        if ((ex.HallSchedule.Count == 0 && ex.HallArrivals.Count == 0)
            || TheRoomsOwnFeet(ex) >= Egress.MostAtOnce)
        {
            return;
        }

        // How far into the shift it is. The SCHEDULE is a function of the frozen watch and does not move
        // while it is being read; this is only the clock hand crossing the times that schedule already named.
        double into = SimTime - (PatronRota.WatchIndex(SimTime) * PatronRota.WatchSeconds);

        DealTheDepartures(ex, into);
        DealTheArrivals(ex, into);
    }

    private void DealTheDepartures(SurfaceExcursion ex, double into)
    {
        foreach (Egress.Move move in ex.HallSchedule!)
        {
            if (into < move.AtSecondsIntoWatch || !ex.HallDeparted.Add(move.TableIndex))
            {
                continue;
            }
            // No route, or nowhere to stand at that door? They simply stay in their chair — the honest
            // answer, and never a body placed at the far end of a walk that could not be walked. The move
            // stays SPENT: see DealTheArrivals for why a refusal that is re-offered every frame is a bug.
            _ = TheyStandUpAndGo(ex, move);
            if (TheRoomsOwnFeet(ex) >= Egress.MostAtOnce)
            {
                return;
            }
        }
    }

    /// <summary>#731 · …and the other half of the room's hours, stepped down the same way. Dealt exactly once
    /// per person per shift (<c>HallArrived</c>, keyed on the PLATE because an arrival's person is the
    /// schedule's and its top is this side's), so a list re-read every frame cannot walk the same body out of
    /// the same leaf twice.</summary>
    private void DealTheArrivals(SurfaceExcursion ex, double into)
    {
        if (TheRoomsOwnFeet(ex) >= Egress.MostAtOnce)
        {
            return;
        }

        foreach (Egress.Move move in ex.HallArrivals!)
        {
            if (into < move.AtSecondsIntoWatch || !ex.HallArrived.Add(move.Plate))
            {
                continue;
            }
            // …and the same: a move the floor cannot carry is spent rather than re-offered. WHY a walk is
            // refused here is a fact about the STONE — where a body can stand in front of a leaf, and whether
            // the lattice joins that spot to a chair — and none of it changes while a watch is running. So a
            // move put back on the list is a move re-attempted on every frame of a four-hour shift, and every
            // attempt lays the whole building out again (`UndergroundComplex.Build`, twice). That is #731 v1's
            // own lesson — a floor plan per frame — arriving through the door it was written to close, and it
            // cost this lane nine minutes of a test run before it was found.
            _ = TheyComeOutOfTheBack(ex, move);
            if (TheRoomsOwnFeet(ex) >= Egress.MostAtOnce)
            {
                return;
            }
        }
    }

    /// <summary>#731 · WHO THIS SHIFT SENDS HOME, worked out from the building itself — asked once when a
    /// watch begins on a floor, never again until one ends. An empty list is the answer for every floor of
    /// every site that is not the one people sit in, which is most of them.</summary>
    private IReadOnlyList<Egress.Move> TheShiftDecidesWhoGoes(SurfaceExcursion ex)
    {
        if (!TheCanteenOn(ex, out UndergroundComplex.Amenity a))
        {
            return [];
        }

        IReadOnlyList<UndergroundComplex.LockedDoor> locked =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Locked;
        return locked.Count == 0
            ? []
            : Egress.Departures(
                ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch,
                CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch),
                locked);
    }

    /// <summary>
    /// #731 · <b>…AND WHO THIS SHIFT BRINGS IN.</b> The issue's second customer — <i>"the B1 canteen: rota
    /// turnover made visible"</i> — and it is <see cref="Egress"/>'s one arithmetic run the other way, not a
    /// canteen-flavoured copy of it.
    ///
    /// <para>Who is eligible is the ROTA'S OWN ANSWER and never a roll invented here:
    /// <see cref="CanteenRegulars.ComingOnShift"/> is the next watch's sheet less the people this watch
    /// already seated. That is the whole inference the room has been computing and telling nobody — the board
    /// on this room's wall says <c>ROTA — WEEK 31</c>, and until this lane the turnover happened in the one
    /// frame the floor was rebuilt in, which is the frame nobody is looking at.</para>
    ///
    /// <para>The top each of them would take is allotted HERE, off the room's own free list in its own order,
    /// because a free chair is a fact about a room and not about a schedule — the same split
    /// <c>Egress.Arrivals</c>' own docs name. Asked of the rota's UNTOUCHED answer (no churn), because the
    /// schedule is a fact about the watch and must not change as the evening it describes plays out.</para>
    /// </summary>
    private IReadOnlyList<Egress.Move> TheShiftDecidesWhoComes(SurfaceExcursion ex)
    {
        if (!TheCanteenOn(ex, out UndergroundComplex.Amenity a))
        {
            return [];
        }

        IReadOnlyList<string> coming =
            CanteenRegulars.ComingOnShift(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch);
        if (coming.Count == 0)
        {
            return [];
        }

        IReadOnlyList<UndergroundComplex.LockedDoor> locked =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Locked;
        if (locked.Count == 0)
        {
            return [];
        }

        // A HALL TOP WITH NOBODY AT IT — not a cabinet (nobody eats in one) and not a chair the shift has
        // already dealt to somebody, whether one of the ten or one of the crowd. In the room's own order, so
        // two people walking in never head for one chair.
        var free = new List<int>();
        foreach (CanteenRegulars.TableSeat top in
            CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
        {
            if (top is { Taken: false, Quiet: false })
            {
                free.Add(top.Index);
            }
        }

        var expected = new List<Egress.Occupant>();
        for (int i = 0; i < coming.Count && i < free.Count; i++)
        {
            expected.Add(new Egress.Occupant(free[i], coming[i]));
        }

        return expected.Count == 0
            ? []
            : Egress.Arrivals(ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, expected, locked);
    }

    /// <summary>#731 · <b>SOMEBODY COMES OUT OF THE STAFF DOOR AND TAKES A TOP.</b> They step out of a leaf
    /// the captain's own TRY is refused at, cross the hall on the captain's own lattice, and sit — and the
    /// chair is theirs only on the frame they reach it (see <see cref="TheyTakeTheTop"/>). They give the
    /// captain a body's berth like anybody going about their own business, so standing in their way stops
    /// them and being looked at is the content. Nothing is said.</summary>
    private bool TheyComeOutOfTheBack(SurfaceExcursion ex, Egress.Move move)
    {
        if (!TheCanteenOn(ex, out UndergroundComplex.Amenity a))
        {
            return false;
        }

        IReadOnlyList<UndergroundComplex.LockedDoor> locked =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Locked;
        if (TopOn(ex, a, move.TableIndex) is not { } top
            || move.Door < 0 || move.Door >= locked.Count)
        {
            return false;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        if (WhereABodyStandsAt(top, walls) is not { } chair)
        {
            return false;
        }

        // THE CHAIR FIRST, AND THEN THE DOORSTEP — #731 v1 paid for this order already. A leaf has two sides
        // and both can be standable; asked with no hint, half the time the answer is the room the captain has
        // never been in, from which there is no route to any chair at all.
        UndergroundComplex.LockedDoor door = locked[move.Door];
        if (Egress.StandingPlaceAt(in door, DeckPlan.AvatarRadius, walls, chair.X, chair.Y)
                is not { } doorstep
            || OnFoot(move.Plate, new NpcWalk.Bound(door.Sign, chair.X, chair.Y), doorstep, walls)
                is not { } walk)
        {
            return false;
        }

        ex.Walkers.Add(new Walker
        {
            Walk = walk, Table = move.TableIndex, For = Errand.Arriving, Who = move.Plate,
        });
        StateHasChanged();
        return true;
    }

    /// <summary>#731 · They have reached the chair, so the chair is theirs — said to the ROOM, in the one
    /// function that answers who is in which top, and the deck re-welded so the drawn room agrees with the
    /// walked one. A top somebody took in the meantime is not taken twice: the walk simply ends and they are
    /// not here, which is the honest answer.</summary>
    private void TheyTakeTheTop(SurfaceExcursion ex, Walker who)
    {
        if (ex.HallCameIn.TryAdd(who.Table, who.Who))
        {
            RebuildSurfaceDeck();
        }
    }

    /// <summary>#731 · Somebody gets up. Their chair comes back empty in the same breath their legs start —
    /// one body, one place — and the deck is rebuilt so the drawn room agrees with the walked one.
    ///
    /// <para>The floor is rebuilt HERE rather than handed down from the deal, because this runs on the two or
    /// three frames of a whole four-hour shift on which somebody actually stands up, and the deal runs on
    /// every frame of it.</para></summary>
    private bool TheyStandUpAndGo(SurfaceExcursion ex, Egress.Move move)
    {
        if (!TheCanteenOn(ex, out UndergroundComplex.Amenity a))
        {
            return false;
        }

        IReadOnlyList<UndergroundComplex.LockedDoor> locked =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Locked;
        if (TopOn(ex, a, move.TableIndex) is not { } seat
            || move.Door < 0 || move.Door >= locked.Count)
        {
            return false;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        if (WhereABodyStandsAt(seat, walls) is not { } from)
        {
            return false;
        }

        UndergroundComplex.LockedDoor door = locked[move.Door];
        if (Egress.StandingPlaceAt(in door, DeckPlan.AvatarRadius, walls, from.X, from.Y) is not { } at)
        {
            return false;
        }

        if (OnFoot(move.Plate, new NpcWalk.Bound(door.Sign, at.X, at.Y), from, walls) is not { } walk)
        {
            return false;
        }

        // #731 · …AND ONE OF THE TEN DOES NOT SIMPLY GO. Who that is is Core's law and never a coin flipped
        // here (CanteenRegulars.ShowsThePassOnTheWayOut); what it changes is the ENDING, which is why it is a
        // different errand rather than a flag. The gesture is aimed at the chair they are getting up from,
        // carried now rather than looked up at the doorstep — a lookup there would be a whole floor plan on
        // every frame of the hold.
        bool gesture = CanteenRegulars.ShowsThePassOnTheWayOut(move.Plate);
        ex.Walkers.Add(new Walker
        {
            Walk = walk,
            Table = move.TableIndex,
            For = gesture ? Errand.ShowingThePass : Errand.Leaving,
            PassToX = gesture ? from.X : double.NaN,
            PassToY = gesture ? from.Y : double.NaN,
        });
        ex.HallStoodUp.Add(move.TableIndex);
        RebuildSurfaceDeck();
        return true;
    }
}
