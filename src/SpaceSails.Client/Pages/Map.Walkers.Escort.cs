using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #731 v2 · FOLLOW ME — the third errand, the one that ends at a door held OPEN instead of one shut in
/// your face. She stands up mid-sentence, crosses the hall, waits in a cabinet's mouth, and gives up on
/// you if you never come. The parked conversation, the booth she picked and the clock she is standing
/// under all live here. Split out of <c>Map.Walkers.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.
/// </summary>
public partial class Map
{
    // ── #731 v2 · FOLLOW ME ──────────────────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-08-06, on #751's cabinets: "Also it is dramatic telling when our contact wants us to follow
    // them into kabinetti :-D"
    //
    // This is the third errand, and it is the opposite of the other two. A departure ends at a door that is
    // SHUT to the captain — that is #731 v1's whole full stop, and Egress will not accept any other kind of
    // leaf. An escort ends at a door that is HELD OPEN: a cabinet's opening is a gap cut in a wall, it opens
    // for her exactly as it opens for you, and the beat is that she stands in it and waits.
    //
    // AND NOTHING IS SAID. Not a pulse, not a card, not a beat. A woman gets up in the middle of a sentence,
    // crosses a loud hall, and stops in a doorway looking back at you: that IS the sentence. §13.8 at its
    // purest, and the canon differential on this lane is what keeps it that way.

    /// <summary>#731 v2 · SHE STANDS UP AND WALKS YOU TO A CABINET. False when this floor has no booth, when
    /// this contact is not one who does it, or when there is no way through — in which case the scene simply
    /// carries on at the table it is already at, which is what it has always done.</summary>
    /// <param name="ex">The excursion, which is where the parked conversation lives.</param>
    /// <param name="tableIndex">The top she is at now.</param>
    /// <param name="scene">Her scene, so <see cref="Escort.TheDealMoveIn"/> can ask whether there is anything
    /// in it worth a private room. The question is answered off the TABLE SCENE'S OWN STATE and never off a
    /// register kept here.</param>
    /// <param name="said">What has been said to her so far — parked for the length of the walk and handed
    /// back at the new table, which is what makes the deal move at the cabinet the SAME deal move.</param>
    private bool WalkTheVisitorIntoACabinet(
        SurfaceExcursion ex, int tableIndex, Encounter.Scene scene, IReadOnlySet<string> said)
    {
        ArgumentNullException.ThrowIfNull(said);

        // A press can beat the frame here exactly as it can at the arrival — see WalkSomebodyToYourTable.
        ForgetWalkersIfTheShiftTurned(ex);

        if (ex.Floor >= 0 || ex.Walkers.Count >= WalkerBand || ex.EscortCabinetTop >= 0
            || !TheCanteenOn(ex, out UndergroundComplex.Amenity a)
            || a.Hall is not { } hall || hall.Cabinets.Count == 0)
        {
            return false;
        }

        // WHO DOES THIS, and it is not a coin this method flipped. Core asks the scene whether there is
        // anything in it worth a room at all, and only then whether this one is the sort who takes you there.
        if (!Escort.LeadsYouIn(
                ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, tableIndex, SittingAlone.VisitorPlate, scene))
        {
            return false;
        }

        if (TopOn(ex, a, tableIndex) is not { } top)
        {
            return false;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        if (ChairOppositeTheCaptain(in top, walls) is not { } from)
        {
            return false;
        }

        IReadOnlyList<CanteenRegulars.TableSeat> tops =
            CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch, ex.HallStoodUp, ex.HallCameIn);
        if (Escort.AFreeCabinet(tops, from.X, from.Y) is not { } booth
            || TheBooth(hall, booth.Cabinet) is not { } cabinet
            || Escort.WhereSheWaits(in cabinet, hall.Cabinets, DeckPlan.AvatarRadius, walls) is not { } at)
        {
            return false;
        }

        // …and she gives the captain NO berth: she is getting up from a chair a stride away from them, and a
        // body that froze there would be a beat that never starts. Same law as the walk out (see OnFoot).
        //
        // The SIGN she carries is the cabinet's own plate, which is the whole difference between this walk
        // and a departure. It is not on this floor's Locked list, because it is not that kind of leaf — and
        // the guard on this lane matches it back to the building to prove it.
        if (OnFoot(
                SittingAlone.VisitorPlate, new NpcWalk.Bound(cabinet.Plate, at.X, at.Y), from, walls,
                NpcWalk.NoPersonalSpace)
            is not { } walk)
        {
            return false;
        }

        ex.Walkers.Add(new Walker
        {
            Walk = walk, Table = booth.Index, For = Errand.LeadingYouIn, Cabinet = cabinet.Number,
        });
        ex.EscortCabinetTop = booth.Index;
        ex.EscortCabinet = cabinet.Number;
        ex.EscortFromTable = tableIndex;
        ex.EscortWho = SittingAlone.VisitorPlate;
        ex.EscortSaid.Clear();
        foreach (string move in said)
        {
            ex.EscortSaid.Add(move);
        }
        ex.EscortSince = SimTime;
        StateHasChanged();
        return true;
    }

    /// <summary>#731 v2 · One of the hall's booths by its number, or null — a lookup off the building's own
    /// list, never a second geometry.</summary>
    private static UndergroundComplex.Cabinet? TheBooth(UndergroundComplex.Hall hall, int number)
    {
        foreach (UndergroundComplex.Cabinet c in hall.Cabinets)
        {
            if (c.Number == number)
            {
                return c;
            }
        }
        return null;
    }

    /// <summary>
    /// #731 v2 · AND IF YOU NEVER COME. She waits <see cref="Escort.PatienceSeconds"/>, and then she goes —
    /// through a door that does not open for you, which is #731 v1's full stop arriving from the other
    /// direction and needs no new machinery at all.
    ///
    /// <para>The door is <see cref="Egress.ArrivalDoor"/>'s, asked off the SAME frozen watch and the SAME
    /// top she crossed the room from, so the provenance holds through the whole evening: she came out of that
    /// door, she offered to take you somewhere, and when you did not come she went back through it. A woman
    /// who left through a leaf she had never been behind would be this repo's third named bug class with a
    /// plate on it.</para>
    ///
    /// <para>Nothing is said. The doorway is empty the next time you look at it.</para>
    /// </summary>
    /// <returns>True when she has been taken off the escort and is walking out (or could not be, and is
    /// simply gone) — i.e. when the caller must not go on treating her as waiting.</returns>
    private bool SheHasWaitedLongEnough(
        SurfaceExcursion ex, Walker who, IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        if (double.IsNaN(ex.EscortSince) || SimTime - ex.EscortSince < Escort.PatienceSeconds)
        {
            return false;
        }

        int from = ex.EscortFromTable;
        ex.Walkers.RemoveAt(slot);
        ForgetTheEscort(ex);

        IReadOnlyList<UndergroundComplex.LockedDoor> locked =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Locked;
        int which = Egress.ArrivalDoor(ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, from, locked);
        if (which < 0 || which >= locked.Count)
        {
            return true;
        }

        UndergroundComplex.LockedDoor door = locked[which];
        var standing = new DeckReachability.Point(who.Walk.X, who.Walk.Y);
        if (Egress.StandingPlaceAt(in door, DeckPlan.AvatarRadius, walls, standing.X, standing.Y)
                is not { } at
            || OnFoot(who.Walk.Plate, new NpcWalk.Bound(door.Sign, at.X, at.Y), standing, walls)
                is not { } away)
        {
            return true;
        }

        ex.Walkers.Add(new Walker { Walk = away, Table = from, For = Errand.Leaving });
        return true;
    }

    /// <summary>#731 v2 · Nobody is holding a door open any more, and the conversation that was parked for
    /// the length of a walk is over. One place that says what forgetting an escort means, so no caller has to
    /// remember six fields.</summary>
    private static void ForgetTheEscort(SurfaceExcursion ex)
    {
        ex.EscortCabinetTop = -1;
        ex.EscortCabinet = 0;
        ex.EscortFromTable = -1;
        ex.EscortWho = "";
        ex.EscortSaid.Clear();
        ex.EscortSince = double.NaN;
    }
}
