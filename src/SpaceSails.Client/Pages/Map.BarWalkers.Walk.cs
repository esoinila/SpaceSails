using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.BarWalkers (the header note lives in Map.BarWalkers.cs) — THE WALKING ITSELF, and not one
// new primitive in it: the steps are NpcWalk's, over AutoWalk's route and SurfaceCollision.Slide's stone.
// `StepTheBarsFeet` is the clock and nothing else — what an arrival MEANS is decided by whoever asked for
// the walk. Around it: the approach to a table and the wait at the counter, the walk out of the room, the
// walk into it, and the four questions about WHERE a walker may stand — the top the captain is at, the
// spot beside a top, the spot beside it that is clear of him, and the first free fixture.
public partial class Map
{
    /// <summary>#973 L0 · The clock, and nothing else. What an arrival MEANS is decided by whoever asked for
    /// the walk.</summary>
    private void StepTheBarsFeet(double dtRealSeconds, in HavenInterior.BarFloor bar)
    {
        if (_barAfoot.Count == 0)
        {
            return;
        }

        double dt = Math.Min(dtRealSeconds, 0.1);
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        bool anybodyLanded = false;

        for (int i = _barAfoot.Count - 1; i >= 0; i--)
        {
            Walker w = _barAfoot[i];

            if (w.For is Errand.RepRounds or Errand.RepPitching or Errand.RepLeaving)
            {
                if (StepTheRep(_barAfoot, w, dt, walls, i))
                {
                    anybodyLanded = true;
                }

                continue;
            }

            if (w.For == Errand.Approaching)
            {
                if (StepAnApproach(bar, w, dt, walls, i))
                {
                    anybodyLanded = true;
                }

                continue;
            }

            // Everything else ends when the route runs out — a departure through a leaf that does not open
            // for the captain, which is #731 v1's full stop said in this room for the first time.
            w.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (w.Walk.Afoot)
            {
                continue;
            }

            _barAfoot.RemoveAt(i);
            anybodyLanded = true;

            // #731 · …AND AN ARRIVAL ENDS IN A CHAIR. He is in it on THIS frame and not on the one he set off
            // on: a man is not sitting somewhere he is still walking to, and a room that seated him early
            // would draw him twice. A walk the ground refused seats nobody — he simply is not here, which is
            // the honest answer.
            if (w.For == Errand.Arriving && w.Walk.State == NpcWalk.Doing.Arrived && w.Who.Length > 0)
            {
                _barCameIn[w.Who] = w.Table;
                RebuildDockedDeck();
            }
        }

        if (anybodyLanded)
        {
            StateHasChanged();
        }
    }

    // ── #973 L0 · THE HOOK ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L0 · <b>SOMEBODY COMES TO YOUR TABLE.</b> The one verb any NPC in a docked station's bar uses to
    /// walk up to the captain, and the hook L5b (the walk-in) calls.
    ///
    /// <para>They come out of one of the bar's own back-room leaves — a door the captain's own TRY is refused
    /// at, which is <see cref="Egress"/>'s law and not a flag set here — cross the floor on the captain's own
    /// lattice, and <paramref name="onArrive"/> fires on the frame the route runs out. <b>Only if
    /// <paramref name="stillWanted"/> answers true then</b>, which is the whole of the safety in this method:
    /// a captain who stood up, walked off or was joined while somebody was mid-stride has ended the scene that
    /// body was walking into, and a card raised over an empty chair is the exact state #731's escort branch was
    /// written to refuse.</para>
    ///
    /// <para>If nobody is available when the walk is PLANNED, they do not teleport and they do not stay off
    /// the floor: they come in anyway and wait at the counter, which is what a person in a bar does. Nothing
    /// is said about any of it.</para>
    ///
    /// <h3>Why the gate is a delegate</h3>
    ///
    /// <para>Because "the captain is sitting alone" is a question about SEATING, and a docked bar cannot seat
    /// anybody yet — all seven ways to open a sitting in this codebase are gated on a
    /// <c>SurfaceExcursion</c>. Written as a delegate, the law this method keeps ("fire only if still wanted")
    /// is provable on its own, and the day the haven bar grows a top the captain can take, exactly one caller
    /// changes. Written as a private opinion about a seat, it would be a branch nothing could exercise —
    /// which is this repository's fifth named bug class.</para>
    /// </summary>
    /// <param name="plate">Their plate, as the deck draws it over their head.</param>
    /// <param name="stillWanted">Whether there is anybody to come to — asked when the walk is planned and
    /// again on the frame it lands.</param>
    /// <param name="onArrive">What arriving means. Fired once, on the landing frame, and never by this file's
    /// own opinion about what somebody has come to say.</param>
    /// <returns>True when a body is on the floor because of this call — whether it is crossing to the table or
    /// waiting at the counter. False when the room could not put one there, which is the honest answer and
    /// never a reason to place a figure at the far end of a walk that could not be walked.</returns>
    private bool ApproachTheTable(string plate, Func<bool> stillWanted, Action onArrive)
    {
        ArgumentNullException.ThrowIfNull(stillWanted);
        ArgumentNullException.ThrowIfNull(onArrive);

        if (TheDockedBar() is not { } bar || _barAfoot.Count >= WalkerBand)
        {
            return false;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        if (stillWanted() && TheTopTheCaptainIsAt(bar, walls) is { } table)
        {
            // …and NO berth: they are coming FOR the captain, and a body that stops one width short of the
            // chair it is walking to and stares forever is a deadlock, not politeness (NpcWalk.NoPersonalSpace).
            return WalkSomebodyIntoTheBar(
                bar, walls, table, plate, Errand.Approaching, NpcWalk.NoPersonalSpace,
                stillWanted, onArrive);
        }

        return TheyWaitAtTheCounter(bar, walls, plate);
    }

    /// <summary>#973 L0 · Nobody to come to, or nobody left to come to. They stand at the counter — the
    /// fixture this bar's own art draws its desk at — and that is all that happens.
    ///
    /// <para>It is the SAME errand as the crossing, with nothing to deliver: an approach with no callback on
    /// it, which lands and then simply stands. Deliberately not the rep's own <see cref="Errand.RepRounds"/> —
    /// that errand is how <c>TheRepAfoot</c> knows which body is Harlan Fess, and a stranger waiting at the
    /// counter wearing it would BE him as far as his card, his dwell and his withdrawal are concerned.</para></summary>
    private bool TheyWaitAtTheCounter(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls, string plate) =>
        TheFirstFreeFixture(bar, walls) is { } post
        && WalkSomebodyIntoTheBar(
            bar, walls, post, plate, Errand.Approaching, NpcWalk.PersonalSpaceInRadii, null, null);

    /// <summary>
    /// #973 L0 · One frame of somebody crossing the bar to the captain's table. The errand whose arrival is
    /// the beginning: they land, and then they STAND there looking at you until whatever brought them is over.
    /// </summary>
    /// <returns>Whether anything happened that the page should redraw for.</returns>
    private bool StepAnApproach(
        in HavenInterior.BarFloor bar, Walker who, double dt,
        IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        if (who.Walk.State != NpcWalk.Doing.Arrived)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (who.Walk.Afoot)
            {
                return false;
            }

            if (who.Walk.State != NpcWalk.Doing.Arrived)
            {
                // The floor refused them somewhere between the door and the table. Nobody is there, so nothing
                // arrived — and a callback fired for a body that is not standing anywhere would be the panel
                // and the floor disagreeing about where somebody is.
                _barAfoot.RemoveAt(slot);
                return true;
            }

            // THE LANDING FRAME IS PART OF THE QUESTION. #731 v2 paid for this line already: its first build
            // checked "arrived" only BEFORE the step, so the very frame a route ran out fell through to the
            // ordinary ending and took the figure off the floor before anybody could look at it.
            if (who.StillWanted?.Invoke() ?? true)
            {
                who.OnArrive?.Invoke();
                who.Walk.LookTowards(_avatarX, _avatarY);
                return true;
            }

            // Nobody is there any more. They do not announce it; they turn round where they are standing and
            // go, out through the leaf they came out of.
            _barAfoot.RemoveAt(slot);
            _ = TheyLeaveTheBar(in bar, walls, who);
            return true;
        }

        // Standing at the table. They stay while they are still wanted, and leave the moment they are not.
        if (!(who.StillWanted?.Invoke() ?? true))
        {
            _barAfoot.RemoveAt(slot);
            _ = TheyLeaveTheBar(in bar, walls, who);
            return true;
        }

        who.Walk.LookTowards(_avatarX, _avatarY);
        return false;
    }

    /// <summary>
    /// #731 · <b>THE FULL STOP, TRIGGERED.</b> <i>"If they go behind a door that is locked to us, we use that
    /// as 'I guess that concludes the conversation' point in the plot / situation."</i>
    ///
    /// <para>This is the beat the owner named for THIS room, and it is the same walker as the scheduled one —
    /// one class, two reasons to use it. A stranger who has just been refused your last offer stands up and
    /// goes, out through the leaf <see cref="Egress.DoorFor"/> dealt her when she came in: the same call, off
    /// the same frozen watch and the same plate, so nobody in this bar ever leaves through a door they were
    /// never behind.</para>
    ///
    /// <para><b>She walks from WHERE SHE IS STANDING</b>, and that sentence is the whole of this method. Until
    /// this lane, an answered walk-in was taken off the floor and re-planned from a back-room doorstep — a
    /// body that vanished from the captain's elbow and reappeared out of the cellar. #973 L5b's own file
    /// flagged it in as many words: <i>"a player watching the counter would see her vanish from it and come
    /// back out of the cellar. That is a worse lie than not retrying, and the honest version wants a 'walk
    /// from where you are standing' that the bar's planner does not have yet."</i> It has one now.</para>
    ///
    /// <para><b>And no berth</b>, for the reason the walk in had none: she is a stride from the captain at his
    /// own table, and a body that froze there out of politeness would be a scene that never ends (#731 v1's
    /// first bug, at 1.45 du). The doorway courtesy belongs to somebody crossing a room they have no business
    /// with the captain in — which is every scheduled departure this room deals.</para>
    ///
    /// <para>Nothing is said. Not a pulse, not a card, not a line. The room says it by walking her out.</para>
    /// </summary>
    /// <returns>False when there is no leaf, no doorstep or no route — in which case she is simply gone,
    /// because she is already on her feet and a body left frozen mid-floor is worse than an empty room.</returns>
    private bool TheyLeaveTheBar(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls, Walker who)
    {
        string plate = who.Walk.Plate;
        int which = Egress.DoorFor(bar.BodyId, BarIsNotAFloor, BarWatch, plate, bar.Doors);
        if (which < 0 || which >= bar.Doors.Count)
        {
            return false;
        }

        var from = new DeckReachability.Point(who.Walk.X, who.Walk.Y);
        UndergroundComplex.LockedDoor leaf = bar.Doors[which];
        if (Egress.StandingPlaceAt(in leaf, DeckPlan.AvatarRadius, walls, from.X, from.Y) is not { } doorstep
            || OnFoot(
                   plate, new NpcWalk.Bound(leaf.Sign, doorstep.X, doorstep.Y), from, walls,
                   NpcWalk.NoPersonalSpace)
               is not { } away)
        {
            return false;
        }

        _barAfoot.Add(new Walker
        {
            Walk = away, Table = who.Table, For = Errand.Leaving, Who = who.Who,
        });
        StateHasChanged();
        return true;
    }

    // ── PLANNING A WALK ACROSS THIS ROOM ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L0 · PLAN SOMEBODY'S WALK INTO THE BAR — the one place on this side that says who is walking, and
    /// it does not claim a gait of its own: it goes through <see cref="OnFoot"/>, which is where this whole
    /// codebase claims <c>Gait.Person</c> exactly once (the owner's ruling behind that count: <i>"Lets not help
    /// reevers move in any easier if possible"</i>).
    ///
    /// <para>They start from the doorstep of one of the bar's back-room leaves, chosen by
    /// <see cref="Egress.DoorFor"/> off the frozen docking watch and the walker's own plate — so one visit
    /// always sends the same person out of the same door, and the captain who was watching can learn which
    /// leaf in this bar opens for whom. The standing place is sounded on the side the DESTINATION is on, for
    /// the reason #731 wrote down: a leaf has two sides and asked with no hint, half the time the answer is
    /// the room the captain has never been in.</para>
    /// </summary>
    private bool WalkSomebodyIntoTheBar(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls,
        DeckReachability.Point to, string plate, Errand errand, double berth,
        Func<bool>? stillWanted, Action? onArrive)
    {
        int which = Egress.DoorFor(bar.BodyId, BarIsNotAFloor, BarWatch, plate, bar.Doors);
        if (which < 0 || which >= bar.Doors.Count)
        {
            return false;
        }

        UndergroundComplex.LockedDoor leaf = bar.Doors[which];
        if (Egress.StandingPlaceAt(in leaf, DeckPlan.AvatarRadius, walls, to.X, to.Y) is not { } doorstep)
        {
            return false;
        }

        // The SIGN is empty on the way in, exactly as the rep's entrance underground is: the plate is what a
        // walk is ABOUT, and a man arriving at a counter is not about a door. What the door did — it opened
        // for somebody and it will not open for you — is said by the room and by nothing else.
        if (OnFoot(plate, new NpcWalk.Bound("", to.X, to.Y), doorstep, walls, berth) is not { } walk)
        {
            return false;
        }

        _barAfoot.Add(new Walker
        {
            Walk = walk, Table = -1, For = errand, StillWanted = stillWanted, OnArrive = onArrive,
        });
        StateHasChanged();
        return true;
    }

    /// <summary>#973 L0 · The top the captain is at, as a place a BODY can stand beside — or null when they
    /// are not at one. The nearest of the room's own published tops within a body's reach of them, so a
    /// captain who is nowhere near a table is never crossed to.</summary>
    private DeckReachability.Point? TheTopTheCaptainIsAt(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        DeckReachability.Point? nearest = null;
        double best = double.MaxValue;
        foreach (DeckReachability.Point top in bar.Tops)
        {
            double d = ((top.X - _avatarX) * (top.X - _avatarX)) + ((top.Y - _avatarY) * (top.Y - _avatarY));
            if (d < best)
            {
                (best, nearest) = (d, top);
            }
        }

        // At it, not merely in the room with it. The captain's own interact reach, so "at a table" means what
        // it means everywhere else in this game.
        return nearest is { } t && best <= DeckPlan.InteractRadius * DeckPlan.InteractRadius
            ? BesideThisTopClearOfTheCaptain(t, walls)
            : null;
    }

    /// <summary>#973 L0 · Where a body stands at a top: one body-width off its centre, on the first side the
    /// stone allows — the same "first one the floor allows" idiom a canteen top's chair ring already uses. The
    /// hall side is sounded first, because that is the side somebody crossing this room comes from.
    ///
    /// <para>#973 L5b · THE SOUNDING MOVED TO THE ROOM (<see cref="HavenInterior.BesideATop"/>) and this is
    /// the name it kept. It has two callers now — the walker crossing to the table, and the SEAT putting the
    /// captain in a chair at one — and two soundings would put the two of them on the same square, which is
    /// the drawn room and the walked room disagreeing about a lap. The body width is still stated here,
    /// because it is the avatar's and not the room's.</para></summary>
    private static DeckReachability.Point? BesideThisTop(
        DeckReachability.Point top, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        HavenInterior.BesideATop(top, BesideATopDu / 2.0, walls);

    /// <summary>#973 L5b · …and the same sounding with the CAPTAIN'S OWN BODY taken out of it. Every walk in
    /// this room is a walk toward somebody who is already there, and the first side the stone allows is
    /// exactly the side they are sitting on since the bar grew a seat.</summary>
    private DeckReachability.Point? BesideThisTopClearOfTheCaptain(
        DeckReachability.Point top, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        HavenInterior.BesideATop(
            top, BesideATopDu / 2.0, walls, new DeckReachability.Point(_avatarX, _avatarY));

    /// <summary>#973 L0 · The first place at the counter the stone allows a body to stand. Read off the room's
    /// published fixtures and never carved here — a second list of where this bar's desk is would be this
    /// repo's oldest bug class with a salesman leaning on it.</summary>
    private static DeckReachability.Point? TheFirstFreeFixture(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        foreach (DeckReachability.Point post in bar.Fixtures)
        {
            if (!SurfaceCollision.Blocked(post.X, post.Y, DeckPlan.AvatarRadius, walls))
            {
                return post;
            }
        }

        return null;
    }
}
