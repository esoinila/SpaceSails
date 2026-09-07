using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #731 · ONE FRAME OF THE ROOM'S METABOLISM — <see cref="AdvanceWalkers"/> and nothing else. It owns the
/// per-frame half of the walker band: deal out whatever the watch says is due, step everybody who is on
/// their feet, and retire the ones whose route has run out. What each errand MEANS at the far end of its
/// walk is decided in the sibling partials; this file only says when the frame asks. Split out of
/// <c>Map.Walkers.cs</c> under #251 with no member renamed, re-scoped or re-ordered.
/// </summary>
public partial class Map
{
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
        DealTheShiftsOwnHours(ex);

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
            if (w.For is Errand.RepRounds or Errand.RepPitching or Errand.RepLeaving)
            {
                if (StepTheRep(ex.Walkers, w, dt, walls, i))
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

            // ── #731 · THE ERRAND THAT ENDS IN A GESTURE NOBODY ANSWERS ──────────────────────────────
            //
            // "The B1 canteen: rota turnover made visible — the agency temp leaving at watch change through
            // the staff door, showing the pass nobody inside asks for."
            //
            // The walk is a departure's, verbatim. The ENDING is not: instead of the leaf clicking behind
            // them, they stop on the doorstep, turn back to the room, and hold the pass up to it. The room
            // does nothing whatsoever — that is not an omission, it is the beat, and the canon differential
            // on this lane is what keeps it that way.
            if (w.For == Errand.ShowingThePass)
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
                        // The ground refused them somewhere between the table and the leaf. They stop, and
                        // there is nothing to hold up to anybody — never a gesture performed by a body that
                        // never got to the door.
                        ex.Walkers.RemoveAt(i);
                        anybodyLanded = true;
                        continue;
                    }
                    // …and the hold begins on the frame the doorstep is reached, on the walk's own clock.
                    anybodyLanded = true;
                }
                else
                {
                    w.PassHeld += dt;
                    if (w.PassHeld >= CanteenRegulars.PassHeldSeconds)
                    {
                        // Held up, unread, and through. Nothing is said.
                        ex.Walkers.RemoveAt(i);
                        anybodyLanded = true;
                        continue;
                    }
                }

                // Turned back to the room they are leaving. Not at the captain — the captain is not who a
                // pass is shown to, and a body that swung round to face whoever walked past would be the
                // room answering, which is the one thing this beat may not do.
                w.Walk.LookTowards(w.PassToX, w.PassToY);
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
                // #731 · TWO KINDS OF ARRIVAL, told apart by whether the room FILED them. A regular who came
                // out of a leaf off the rota carries the plate the room knows them by and ends in a chair;
                // the one who came to the captain's table carries none and ends in #865's card.
                if (w.Who.Length > 0)
                {
                    TheyTakeTheTop(ex, w);
                }
                else
                {
                    SomebodyHasReachedYourTable(ex, w);
                }
            }
        }

        if (anybodyLanded)
        {
            StateHasChanged();
        }
    }
}
