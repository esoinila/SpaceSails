using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #731 · THE EXIT IS THE FULL STOP — the band of the deck's figure buffer that belongs to people leaving
/// and people arriving, and the frame that walks them.
///
/// <para><b>Owner, 2026-08-06, streamed during the smoke run:</b> <i>"The NPCs but not reevers could also
/// use the A* if we want to show them leaving a scene etc. If they go behind a door that is locked to us, we
/// use that as 'I guess that concludes the conversation' point in the plot / situation."</i> And the
/// limitation it fixes: <i>"Like on the bar now they have to wait for us to leave before they can sit up… or
/// leave the bar."</i></para>
///
/// <para><b>And the other direction, the same day:</b> <i>"In the space bars there are lot of cases where we
/// can have npcs arrive at bar from locked place or go to a locked place. Now it is possible to have NPC ask
/// to sit down at our table and offer a quest! This is the classic TTRPG event."</i></para>
///
/// <h3>What this file is, and what it deliberately is not</h3>
///
/// <para>It is a BAND and a STEP, and nothing else. The walking is <see cref="NpcWalk"/>'s, in Core, over
/// <c>AutoWalk</c>'s route and <c>SurfaceCollision.Slide</c>'s stone — the same two primitives the captain's
/// boots and the guard's round already spend. Who leaves and out of which door is <see cref="Egress"/>'s, in
/// Core, off the frozen watch. This file owns the two things that can only be owned here: which slots of the
/// figure buffer the walkers are written into, and when the frame steps them.</para>
///
/// <para><b>Nothing here explains anything.</b> A regular gets up, crosses the hall, and goes through a door
/// the captain's own TRY is refused at — and not one line of prose is filed, pulsed or raised about it. That
/// is the whole beat and it is §13.8 in its purest form: the room told you something and the game did not.
/// The canon sweep on this lane exists to keep it that way.</para>
/// </summary>
public partial class Map
{
    /// <summary>#731 · How many walkers the surface's figure buffer keeps room for.
    ///
    /// <para><see cref="Egress.MostAtOnce"/> and not a number of its own: the room's own law about how many
    /// people may be on their feet at once is the same law as how many slots the buffer needs, and two
    /// opinions about it is the mirrored-constant bug with a body walking through it.</para></summary>
    private const int WalkerBand = Egress.MostAtOnce;

    /// <summary>#731 · One person on their feet: the walk, and what the walk is FOR.
    ///
    /// <para>The errand is kept here rather than on <see cref="NpcWalk"/> on purpose. Core's walker knows how
    /// to cross a floor and nothing about bars, chairs or quests; what a particular walk means when it ends
    /// is this component's business, and the day a sweep team walks out of an airlock (#731 v2) it will mean
    /// something else again without Core learning a third word.</para></summary>
    private sealed class Walker
    {
        public required NpcWalk Walk { get; init; }

        /// <summary>The top they got up from, or the top they are walking TO for an arrival.</summary>
        public required int Table { get; init; }

        /// <summary>True when this is somebody coming to the captain's table (#865's
        /// <c>TheyCameToYou</c>), false when it is somebody finishing and going.</summary>
        public required bool Arriving { get; init; }
    }

    /// <summary>#731 · Every walker's slot is off-map when nobody is in it — the same idiom an unseen guard
    /// and a behind-cover Old One already use, so the buffer is always fully written and the renderer never
    /// has to know how many people are afoot.</summary>
    private void FillWalkerDroids(DeckPlan.Droid[] buffer, int firstSlot)
    {
        List<Walker> afoot = _surface?.Walkers ?? [];
        for (int i = 0; i < WalkerBand; i++)
        {
            int slot = firstSlot + i;
            if (slot >= buffer.Length)
            {
                return;
            }
            // The EXISTING NPC pen and no new one: a plate that is not a guard's, a sweeper's or an Old
            // One's falls through DrawTheFigures to the ordinary grey, which is exactly right — the person
            // crossing the hall is one of the people who were sitting in it a minute ago.
            buffer[slot] = i < afoot.Count
                ? new DeckPlan.Droid(afoot[i].Walk.X, afoot[i].Walk.Y, afoot[i].Walk.Facing, afoot[i].Walk.Plate)
                : new DeckPlan.Droid(-9999, -9999, 0, WalkerSlotName(i));
        }
    }

    /// <summary>What an EMPTY walker slot is called. Stable per slot so the buffer's shape does not change
    /// with how many people happen to be walking — the fingerprint tests read this buffer.</summary>
    private static string WalkerSlotName(int index) => $"WALKER {index + 1}";

    /// <summary>
    /// #731 · ONE FRAME OF THE ROOM'S METABOLISM.
    ///
    /// <para>Deal out whatever this watch has decided should happen by now, then step everybody who is on
    /// their feet. Called from <c>StepSurface</c> beside the round, and it does nothing at all anywhere but
    /// an underground floor — the walkers are a fact about a room with people in it.</para>
    /// </summary>
    private void AdvanceWalkers(double dtRealSeconds)
    {
        if (_surface is not { Floor: < 0 } ex)
        {
            return;
        }

        ForgetWalkersIfTheShiftTurned(ex);
        DealTheDepartures(ex);

        if (ex.Walkers.Count == 0)
        {
            return;
        }

        double dt = Math.Min(dtRealSeconds, 0.1);
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        bool anybodyLanded = false;

        for (int i = ex.Walkers.Count - 1; i >= 0; i--)
        {
            Walker w = ex.Walkers[i];
            w.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (w.Walk.Afoot)
            {
                continue;
            }

            ex.Walkers.RemoveAt(i);
            anybodyLanded = true;
            if (w.Arriving && w.Walk.State == NpcWalk.Doing.Arrived)
            {
                SomebodyHasReachedYourTable(ex, w);
            }
        }

        if (anybodyLanded)
        {
            StateHasChanged();
        }
    }

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
    /// </summary>
    private void DealTheDepartures(SurfaceExcursion ex)
    {
        if (ex.Walkers.Count >= WalkerBand || !TheCanteenOn(ex, out UndergroundComplex.Amenity a))
        {
            return;
        }

        IReadOnlyList<UndergroundComplex.LockedDoor> locked =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Locked;
        if (locked.Count == 0)
        {
            return;
        }

        // How far into the shift it is. The SCHEDULE is a function of the frozen watch and does not move
        // while it is being read; this is only the clock hand crossing the times that schedule already named.
        double into = SimTime - (PatronRota.WatchIndex(SimTime) * PatronRota.WatchSeconds);
        IReadOnlyList<CanteenRegulars.TableSeat> tops =
            CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch);

        foreach (Egress.Move move in
            Egress.Departures(ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, tops, locked))
        {
            if (into < move.AtSecondsIntoWatch || !ex.HallDeparted.Add(move.TableIndex))
            {
                continue;
            }
            if (!TheyStandUpAndGo(ex, move, tops, locked))
            {
                // No route, or nowhere to stand at that door. They simply stay in their chair — the honest
                // answer, and never a body placed at the far end of a walk that could not be walked.
                ex.HallDeparted.Remove(move.TableIndex);
            }
            if (ex.Walkers.Count >= WalkerBand)
            {
                return;
            }
        }
    }

    /// <summary>#731 · Somebody gets up. Their chair comes back empty in the same breath their legs start —
    /// one body, one place — and the deck is rebuilt so the drawn room agrees with the walked one.</summary>
    private bool TheyStandUpAndGo(
        SurfaceExcursion ex,
        Egress.Move move,
        IReadOnlyList<CanteenRegulars.TableSeat> tops,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked)
    {
        CanteenRegulars.TableSeat? top = null;
        foreach (CanteenRegulars.TableSeat t in tops)
        {
            if (t.Index == move.TableIndex)
            {
                top = t;
                break;
            }
        }
        if (top is not { } seat || move.Door < 0 || move.Door >= locked.Count)
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

        NpcWalk? walk = NpcWalk.Plan(
            move.Plate, new NpcWalk.Bound(door.Sign, at.X, at.Y), from, walls,
            DeckPlan.AvatarRadius, SurfaceCollision.Gait.Person);
        if (walk is null)
        {
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = walk, Table = move.TableIndex, Arriving = false });
        ex.HallStoodUp.Add(move.TableIndex);
        RebuildSurfaceDeck();
        return true;
    }

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
        if (Egress.StandingPlaceAt(in door, DeckPlan.AvatarRadius, walls) is not { } from
            || ChairOppositeTheCaptain(in top, walls) is not { } chair)
        {
            return false;
        }

        NpcWalk? walk = NpcWalk.Plan(
            SittingAlone.VisitorPlate, new NpcWalk.Bound(door.Sign, chair.X, chair.Y), from, walls,
            DeckPlan.AvatarRadius, SurfaceCollision.Gait.Person);
        if (walk is null)
        {
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = walk, Table = tableIndex, Arriving = true });
        StateHasChanged();
        return true;
    }

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
            CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch, ex.HallStoodUp))
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
