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
    /// opinions about it is the mirrored-constant bug with a body walking through it.</para>
    ///
    /// <para>#973 L2 · …PLUS THE SALESMAN'S OWN SLOT, and he needs one of his own because he is not one of
    /// the room's people. <see cref="Egress.MostAtOnce"/> is a law about how many REGULARS may be crossing
    /// the floor at a time, and on a heaving watch it is satisfied constantly — which, while the band was
    /// exactly that number, meant Harlan Fess could never get on the floor at all. Watched in a browser:
    /// the room's two leavers held both slots for a whole shift and the rep's every attempt was refused.
    /// So the band is the room's law plus his one body; the room's own departures are still capped at
    /// <see cref="Egress.MostAtOnce"/> by <see cref="TheRoomsOwnFeet"/>, which counts the REGULARS on their
    /// feet and not the visitor working them.</para></summary>
    private const int WalkerBand = Egress.MostAtOnce + NebulaRep.OnTheFloorAtOnce;

    /// <summary>#973 L2 · How many of the ROOM'S OWN people are on their feet. The salesman is not one of
    /// them: he does not live here, he did not get up from a top, and he is not who
    /// <see cref="Egress.MostAtOnce"/> is a law about. Counted rather than tracked, because the walker list
    /// is the truth about who is afoot and a second tally of it would be a second opinion.</summary>
    private static int TheRoomsOwnFeet(SurfaceExcursion ex)
    {
        int feet = 0;
        foreach (Walker w in ex.Walkers)
        {
            if (w.For is not (Errand.RepRounds or Errand.RepPitching))
            {
                feet++;
            }
        }

        return feet;
    }

    /// <summary>#731 · One person on their feet: the walk, and what the walk is FOR.
    ///
    /// <para>The errand is kept here rather than on <see cref="NpcWalk"/> on purpose. Core's walker knows how
    /// to cross a floor and nothing about bars, chairs or quests; what a particular walk means when it ends
    /// is this component's business, and the day a sweep team walks out of an airlock (#731 v2) it will mean
    /// something else again without Core learning a third word.</para></summary>
    private sealed class Walker
    {
        public required NpcWalk Walk { get; init; }

        /// <summary>The top they got up from, the top they are walking TO for an arrival, or — for an
        /// escort — the CABINET top whose door they are about to hold open.</summary>
        public required int Table { get; init; }

        /// <summary>What this walk is for. Three errands, one walker.</summary>
        public required Errand For { get; init; }

        /// <summary>#731 v2 · Which cabinet they are leading you into, as the plate reads — 0 on every other
        /// errand.</summary>
        public int Cabinet { get; init; }
    }

    /// <summary>#731 · WHY SOMEBODY IS ON THEIR FEET. Three answers, and they are three different ENDINGS,
    /// which is why the errand is a small closed enum rather than the bool it started as: two of them take
    /// the figure off the floor when the route runs out, and the third is the one where arriving is the
    /// beginning of the beat rather than the end of it.</summary>
    private enum Errand
    {
        /// <summary>Finished, and going — out through a door the captain's own TRY is refused at. The
        /// scheduled ambience and the triggered full stop are both this.</summary>
        Leaving,

        /// <summary>Coming to the captain's table out of one of those doors; #865's <c>TheyCameToYou</c> is
        /// raised on the frame they reach the chair.</summary>
        Arriving,

        /// <summary>#731 v2 · Walking you into a cabinet. <i>"It is dramatic telling when our contact wants
        /// us to follow them into kabinetti."</i> The one errand that does not end when the walk does: she
        /// gets to the door, and then she stands in it and looks back at you across the hall.</summary>
        LeadingYouIn,

        /// <summary>#973 L2 · The Nebula rep drifting between the fixtures of his beat — a standing place
        /// at the counter, the ends of the room's own tops — with nothing to do until somebody sits down
        /// alone. Arriving is not an ending here either: he stands beside the thing he walked to.</summary>
        RepRounds,

        /// <summary>#973 L2 · The rep crossing to a captain sitting alone. He STANDS at the table — he is
        /// not invited, and there is no eighth way to open a sitting in this codebase — and the pitch card
        /// goes up on the frame he lands on.</summary>
        RepPitching,
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

            // ── #973 L2 · …AND TWO MORE THAT END STANDING UP ─────────────────────────────────────────
            //
            // The salesman's two errands are the escort's shape, not the haulier's: he walks somewhere and
            // then he is THERE, beside a fixture or at your elbow, until something moves him on. The
            // decision of what that means is Map.Rep.cs's; this loop only owns the clock.
            if (w.For is Errand.RepRounds or Errand.RepPitching)
            {
                if (StepTheRep(ex, w, dt, walls, i))
                {
                    anybodyLanded = true;
                }

                continue;
            }

            // ── #731 v2 · THE ERRAND WHOSE ARRIVAL IS THE BEGINNING ──────────────────────────────────
            //
            // The other two walks END when the route runs out: the figure comes off the floor and, for an
            // arrival, the card goes up. An escort's does the opposite. She reaches the doorway and STAYS
            // in it, looking back across the hall at you, and the game says nothing whatsoever about it —
            // whether the scene resumes is the captain's legs' business.
            //
            // AND THE ARRIVING FRAME IS PART OF THAT. The first build of this checked "arrived" only BEFORE
            // the step, so the very frame the route ran out fell through to the ordinary ending and took her
            // off the floor: the walk was perfect, the wait lasted zero frames, and the doorway was empty by
            // the time the captain could have looked at it. Watched go red as `the doorway is empty after 0
            // frame(s) of waiting`.
            if (w.For == Errand.LeadingYouIn)
            {
                if (w.Walk.State != NpcWalk.Doing.Arrived)
                {
                    w.Walk.Step(dt, walls, _avatarX, _avatarY);
                    if (w.Walk.Afoot)
                    {
                        continue;
                    }
                    if (w.Walk.State != NpcWalk.Doing.Arrived)
                    {
                        // The ground refused her somewhere between your table and the booth. There is nobody
                        // at that door now, so there is nobody to follow — and a conversation left parked for
                        // a woman who is not there is exactly the state this repo has named a bug class after.
                        ex.Walkers.RemoveAt(i);
                        ForgetTheEscort(ex);
                        anybodyLanded = true;
                        continue;
                    }
                    anybodyLanded = true;
                }
                else if (SheHasWaitedLongEnough(ex, w, walls, i))
                {
                    anybodyLanded = true;
                    continue;
                }

                w.Walk.LookTowards(_avatarX, _avatarY);
                continue;
            }

            w.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (w.Walk.Afoot)
            {
                continue;
            }

            ex.Walkers.RemoveAt(i);
            anybodyLanded = true;
            if (w.For == Errand.Arriving && w.Walk.State == NpcWalk.Doing.Arrived)
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
        // #731 v2 · …and the conversation somebody was holding a door open for. A shift turning over is the
        // room forgetting, and a parked scene is the most forgettable thing in it: the woman it belonged to
        // went home three hours ago.
        ForgetTheEscort(ex);
        // …and the shift's own list of who goes, which belonged to the shift that has just ended. Null and
        // not empty: empty is an answer this room gave, null is a question it has not been asked yet.
        ex.HallSchedule = null;
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
    private void DealTheDepartures(SurfaceExcursion ex)
    {
        // The shift's own list, worked out once. See the note above on why this is not a micro-optimisation.
        ex.HallSchedule ??= TheShiftDecidesWhoGoes(ex);

        if (ex.HallSchedule.Count == 0 || TheRoomsOwnFeet(ex) >= Egress.MostAtOnce)
        {
            return;
        }

        // How far into the shift it is. The SCHEDULE is a function of the frozen watch and does not move
        // while it is being read; this is only the clock hand crossing the times that schedule already named.
        double into = SimTime - (PatronRota.WatchIndex(SimTime) * PatronRota.WatchSeconds);

        foreach (Egress.Move move in ex.HallSchedule)
        {
            if (into < move.AtSecondsIntoWatch || !ex.HallDeparted.Add(move.TableIndex))
            {
                continue;
            }
            if (!TheyStandUpAndGo(ex, move))
            {
                // No route, or nowhere to stand at that door. They simply stay in their chair — the honest
                // answer, and never a body placed at the far end of a walk that could not be walked.
                ex.HallDeparted.Remove(move.TableIndex);
            }
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

        ex.Walkers.Add(new Walker { Walk = walk, Table = move.TableIndex, For = Errand.Leaving });
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
            CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch, ex.HallStoodUp);
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
