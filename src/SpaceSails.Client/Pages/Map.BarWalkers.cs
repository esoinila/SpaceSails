using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #973 L0 · THE DOCKED STATION BAR IS A ROOM NOW.
///
/// <para>The owner's favourite room in this game is The Red Eye's bar off Jupiter — the menu card, the offer
/// of a drink to One-Eye Silas, the regulars at their tops, the PIRATE INSURANCE poster on the starboard
/// wall — and until this lane it was the one room in the game where <b>nobody could move</b>. Eleven droid
/// slots, every one of them a stateless function of sim time: the regulars are seated where the rota put
/// them, the barkeep paces a sine, the Magpie stands on a schedule. No band, no doors an NPC could come out
/// of, no floor anybody but the captain walked.</para>
///
/// <para>#731 built the whole of that machinery — <i>"the NPCs but not reevers could also use the A* … if they
/// go behind a door that is locked to us, we use that as 'I guess that concludes the conversation'"</i> — and
/// wired it to a Hive canteen floor, because that was the only deck in the game with a walker band. #976 put
/// Harlan Fess on that same floor and its own file said so out loud: <i>"A docked station's bar has posters
/// and a barkeep and no seating and no walkers at all."</i> This file is the second half of that sentence
/// being paid off.</para>
///
/// <h3>What is here, and what is deliberately not</h3>
///
/// <para><b>Not one new primitive.</b> The walking is <see cref="NpcWalk"/>'s, over <c>AutoWalk</c>'s route
/// and <c>SurfaceCollision.Slide</c>'s stone. Which door somebody comes out of is <see cref="Egress"/>'s, off
/// the frozen docking watch, and it is a <see cref="UndergroundComplex.LockedDoor"/> because that is the type
/// that cannot be a public exit. The band's width is <see cref="Egress.BandSlots"/>. The figures are the
/// <see cref="Walker"/> record and the <see cref="Errand"/> enum this page already had, drawn through the same
/// <see cref="FillWalkerDroids"/>. What this file owns is the ROOM — which list of feet belongs to a berth,
/// when that room forgets, and the one hook L5b needs.</para>
///
/// <para><b>Nothing here explains anything.</b> A man comes out of the cellar door, crosses the floor and
/// stands at the counter; the captain's own TRY at that leaf is refused, and no card, pulse or line is raised
/// about either fact. That is §13.8 and it is the whole of #731's beat, arriving at last in the room the owner
/// actually drinks in.</para>
///
/// <h3>What is NOT here yet, stated plainly</h3>
///
/// <para><b>There is no way to sit down in a docked bar.</b> Every seat in this game is opened through
/// <c>Seating.TakeThisSeat</c>, all seven sites of it are gated on a <c>SurfaceExcursion</c>, and a docked
/// berth has none — the bar's seven tops are drawn dressing with no chairs and no console. So
/// <see cref="ApproachTheTable"/> exists, is wired, and answers the honest thing today: nobody is sitting
/// alone, therefore nobody is crossed to, therefore the visitor waits at the counter. The gate is a
/// <c>Func&lt;bool&gt;</c> and not a private opinion precisely so that the day the haven bar grows a seat, one
/// caller changes and this file does not.</para>
/// </summary>
public partial class Map
{
    /// <summary>#973 L0 · Everybody on their feet in the docked bar. The bar's own list and never the
    /// excursion's: a captain can be ashore at a berth with no excursion at all, and one list serving two
    /// rooms would be this repo's first named bug class with a barman in it.</summary>
    private readonly List<Walker> _barAfoot = [];

    /// <summary>Which berth these feet belong to. A different berth — or none — is a room that has never seen
    /// any of them, exactly as a turned shift is underground.</summary>
    private string? _barFeetBerth;

    /// <summary>#973 L0 · How far off a top's centre a body stands to be AT it. One avatar ACROSS, the same
    /// step the ashore boot uses to stand the captain wholly inside a room rather than straddling its edge —
    /// a body-width and never a coordinate in this room, which is the one kind of number a client file is
    /// allowed to hold. Which SIDE is the stone's answer, sounded below.</summary>
    private const double BesideATopDu = 2 * DeckPlan.AvatarRadius;

    /// <summary>#973 L0 · The docked bar the captain is standing in a berth of, or null.
    ///
    /// <para>Null on an excursion (that room has its own metabolism and its own list), null off the deck, and
    /// null at a berth with no interior to walk. It does NOT ask whether the captain has reached the bar yet:
    /// somebody crossing a floor keeps crossing it whether or not there is anybody in the room, which is the
    /// difference between a simulation and a cutscene.</para></summary>
    private HavenInterior.BarFloor? TheDockedBar() =>
        _surface is null && _deckMode && _dockedHavenId is { } berth
            ? HavenInterior.BarBand(berth)
            : null;

    /// <summary>#973 L0 · Is the captain actually in the bar? North of the room's own south wall, which is the
    /// wall the room is built off — never a threshold typed in here.</summary>
    private bool InTheBar(in HavenInterior.BarFloor bar) => _avatarY > bar.FloorY;

    /// <summary>#973 L0 · The bar's walker band, written into the slots the docked deck reserved for it. The
    /// same filler the Hive floor uses, handed the other room's feet.</summary>
    private void FillBarWalkerDroids(DeckPlan.Droid[] buffer, int firstSlot) =>
        FillWalkerDroids(buffer, firstSlot, _barAfoot);

    /// <summary>#973 L0 · The frozen docking watch, as an index. The rota that seated the room and the roll
    /// that picks a door are the same watch, for #709's reason: a room drawn at one instant and walked at
    /// another is two rooms.</summary>
    private long BarWatch => PatronRota.WatchIndex(_dockVisitSimTime);

    /// <summary>#973 L0 · A docked berth is not a floor of a building, and <see cref="Egress.DoorFor"/> only
    /// wants a number to fold into its seed. Zero, stated once, so the door a man comes out of is the same
    /// door every time this visit.</summary>
    private const int BarIsNotAFloor = 0;

    // ── ONE FRAME OF THE BAR'S METABOLISM ────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L0 · Step everybody who is on their feet in the docked bar, then let the salesman decide what to
    /// do next. Called from the walked frame beside the ship's own pumps, and it does nothing at all anywhere
    /// but a berth with a bar behind it.
    ///
    /// <para>The order is the Hive's, and for the Hive's reason: the decision about what somebody should do
    /// next is a decision about a floor whose bodies have already been stepped this frame.</para>
    /// </summary>
    private void AdvanceBarWalkers(double dtRealSeconds)
    {
        if (TheDockedBar() is not { } bar)
        {
            ForgetTheBarsFeet(null);

            // …and his VISIT is only this file's to forget when there is no excursion either. On a moon
            // <c>AdvanceTheRep</c> owns that fold, and two owners of one field is a visit counter that ticks
            // once a frame: he would arrive as a stranger sixty times a second and the rota would be noise.
            if (_surface is null)
            {
                EnsureRepVisit(null);
                EnsureWalkInVisit(null);
            }

            return;
        }

        ForgetTheBarsFeet(bar.BodyId);
        StepTheBarsFeet(dtRealSeconds, bar);
        AdvanceTheRepAshore(bar);
        AdvanceTheWalkIn(bar);   // #973 L5b · …and whoever the evening has crossing the floor to your table
    }

    /// <summary>#973 L0 · CASTING OFF IS THE ROOM FORGETTING. Same law a turned shift is underground: what
    /// happened at the last berth happened to people who are not here, and a body left on a list across a
    /// re-dock would be drawn walking through a station it was never in.</summary>
    private void ForgetTheBarsFeet(string? berth)
    {
        if (_barFeetBerth == berth)
        {
            return;
        }

        _barFeetBerth = berth;
        _barAfoot.Clear();
    }

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

            if (w.For is Errand.RepRounds or Errand.RepPitching)
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

            // Nobody is there any more. They do not announce it; they go and stand at the bar like anybody
            // else whose evening did not go the way they expected.
            _barAfoot.RemoveAt(slot);
            _ = TheyWaitAtTheCounter(bar, walls, who.Walk.Plate);
            return true;
        }

        // Standing at the table. They stay while they are still wanted, and leave the moment they are not.
        if (!(who.StillWanted?.Invoke() ?? true))
        {
            _barAfoot.RemoveAt(slot);
            _ = TheyWaitAtTheCounter(bar, walls, who.Walk.Plate);
            return true;
        }

        who.Walk.LookTowards(_avatarX, _avatarY);
        return false;
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
    /// #973 L0 · PUT HIM IN THE BAR, or move him along it. The same two answers as underground: he crosses to
    /// a captain sitting alone if there is one, and otherwise he drifts between the fixtures of his beat.
    ///
    /// <para>The crossing goes through <see cref="ApproachTheTable"/> and not through a second planner, which
    /// is the point of that hook existing: whatever L5b's walk-in ends up being, it and the salesman reach the
    /// captain's table by the same legs.</para>
    /// </summary>
    private bool SendTheRepIntoTheBar(in HavenInterior.BarFloor bar)
    {
        if (_barAfoot.Count >= WalkerBand || SimTime < _repMoveOnAt)
        {
            return false;
        }

        // He crosses to the table only when there IS somebody sitting alone at one and he has not been sent
        // away this visit — everything else in this room is furniture he stands beside. The crossing itself
        // goes through the hook, so his legs and L5b's are one set of legs; the hook's own fallback (come in
        // and wait at the counter) belongs to a caller who has been ASKED to come, which he has not.
        if (_repMemory.MayApproach(_repVisitIndex)
            && TheCaptainIsSittingAloneInTheBar()
            && ApproachTheTable(NebulaRep.Plate, TheCaptainIsSittingAloneInTheBar, HeReachesYourTable))
        {
            return true;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        List<DeckReachability.Point> beat = TheRepsBeatInTheBar(in bar, walls);
        if (beat.Count == 0)
        {
            return false;
        }

        _repPost = (_repPost + 1) % beat.Count;
        return WalkSomebodyIntoTheBar(
            in bar, walls, beat[_repPost], NebulaRep.Plate, Errand.RepRounds,
            NpcWalk.PersonalSpaceInRadii, null, null);
    }

    /// <summary>
    /// #973 L0 · HIS BEAT IN A STATION BAR — the counter first, because that is where he says he will be, then
    /// the ends of the room's own tops. The same shape his hive beat has, off this room's published geometry.
    /// </summary>
    private List<DeckReachability.Point> TheRepsBeatInTheBar(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        List<DeckReachability.Point> beat = [];
        if (TheFirstFreeFixture(in bar, walls) is { } counter)
        {
            beat.Add(counter);
        }

        foreach (DeckReachability.Point top in bar.Tops)
        {
            if (BesideThisTopClearOfTheCaptain(top, walls) is { } beside)
            {
                beat.Add(beside);
            }

            if (beat.Count >= 3)
            {
                break;
            }
        }

        return beat;
    }

    /// <inheritdoc cref="Seating.TryTakeBarTop"/>
    private bool TryTakeBarTop() => _seating.TryTakeBarTop();

    // ── #973 L5b · THE EIGHTH SEAT'S ONE QUESTION ────────────────────────────────────────────────────────
    //
    // THE ANSWER'S TYPE LIVES HERE AND NOT IN `ISeatHost.cs`, and the reason is the ratchet itself: the guard
    // that counts what a chair asks the page for reads that file a DECLARATION at a time, and a positional
    // record struct is spelled exactly like a method. Declared next door it counted as a thirty-third member
    // the seat had never asked for. It is a nested type of the page either way, so the seat still names it
    // bare — where a type is declared changes nothing about who can see it.

    /// <summary>
    /// #973 L5b · A TOP IN A DOCKED STATION'S BAR, AS THE SEAT NEEDS IT — the page's whole answer to "what
    /// did that [E] land on", and nothing behind it.
    ///
    /// <para>It is an ANSWER and never machinery, which is the rule <see cref="ISeatHost"/>'s own summary
    /// states: the deck, the room's published tops and the stone the chair is sounded against are all things
    /// the PAGE owns, and a chair handed a lattice is exactly what lane 6c took away. Everything about the
    /// SITTING — which scene it is, whether it reads relaxed, how many chairs are left — the seat decides
    /// from these six facts, the same way it decides them from a <c>CanteenRegulars.TableSeat</c>.</para>
    /// </summary>
    /// <param name="Index">The top's ordinal in the room's own list.</param>
    /// <param name="Key">What every fact about this sitting is keyed on: the berth, the frozen docking watch
    /// and the ordinal. A berth has no excursion and no canteen watch, so it cannot be the Hive's key — and a
    /// key that could collide with one would file two rooms' business in one drawer.</param>
    /// <param name="Watch">The frozen docking watch, for the one question the SCENE asks of a clock: whether
    /// a sit at this hour reads relaxed (<c>SittingAlone.SitReadsAsRelaxed</c>).</param>
    /// <param name="ChairX">Where the body goes — Core's own sounding against the room's own stone
    /// (<c>HavenInterior.BesideATop</c>), never a coordinate the seat measured (§13.15).</param>
    /// <param name="ChairY"><inheritdoc cref="ChairX"/></param>
    /// <param name="Seats">How many the top seats — the room's own number.</param>
    /// <param name="Setting">Where the captain is sitting, in the scene's own words — the one clause the
    /// strip's company line is built out of. A canteen's setting is a constant; a berth's is per-station and
    /// the ship's are her own two rooms, so the finished sentence travels with the answer rather than being
    /// reassembled by a chair that would have to know which building it is in.</param>
    /// <param name="Plate">#1016 · What the panel calls this seat — <c>YOUR OWN TABLE</c> at a top,
    /// <c>YOUR OWN DESK</c> at the one in the captain's berth. Carried for the same reason
    /// <paramref name="Setting"/> is: it is the ROOM's word for its own furniture.</param>
    /// <param name="Quiet">#1016 · Whether this seat is behind a door — the one fact the exposure ladder
    /// reads (<c>Seating.SeatedIn</c>), and therefore whether the case may be spread here unconditionally. A
    /// station bar is one loud room with a window in it and answers false; a cabin has a leaf a step away
    /// and answers true.</param>
    /// <param name="Aboard">#1016 · Whether this seat is on the captain's OWN SHIP rather than ashore. Two
    /// things hang off it and nothing else does: nobody ever crosses the floor to it (there is nobody
    /// aboard to do the crossing), and the silence when you wait is the boat's own rather than a hall's.</param>
    private readonly record struct BarTopUnderfoot(
        int Index, string Key, long Watch, double ChairX, double ChairY, int Seats, string Setting,
        string Plate, bool Quiet, bool Aboard);


    /// <summary>
    /// #973 L5b · <b>WHICH BAR TOP THAT [E] LANDED ON, AND WHERE A BODY SITS AT IT.</b> The page's whole
    /// answer to the eighth sitting site (<c>Seating.BarTop.cs</c>), and the one member #973 L5b added to
    /// <see cref="ISeatHost"/>.
    ///
    /// <para>The shape is <c>TryTakeTable</c>'s, one room over: find the console the press landed on, match
    /// it back against the room's OWN published list rather than against anything measured here, and hand
    /// back the ordinal. What is different is what a berth does not have — no excursion, no canteen watch, no
    /// <c>CanteenRegulars.Tables</c> — so the key is the bar's own (berth · docking watch · ordinal) and the
    /// chair is <see cref="HavenInterior.BesideATop"/>'s, the same sounding a walker crossing to this top
    /// uses.</para>
    ///
    /// <para><b>The chair is sounded with nobody excluded, and that is the right way round.</b> The captain
    /// is the FIRST body at this top — the woman who crosses the floor to it afterwards is the one who has to
    /// be told about him, which is what <see cref="BesideThisTopClearOfTheCaptain"/> is for.</para>
    ///
    /// <para><b>#1016 · ONE MEMBER, THREE ROOMS.</b> Owner, on 7 Deck: <i>"Why no table here to sit at?"</i>,
    /// <i>"Why no table in cabin either?"</i>, <i>"I expect to have a bar table like this in this ships
    /// galley also.... feature complete."</i> Her cantina tops and her cabin desk are the same VERB as a top
    /// in a station bar, so they are answered by the same member rather than by a ninth thing a chair asks
    /// the page for — the ratchet on <see cref="ISeatHost"/> says the list may only shrink, and this lane had
    /// no argument for growing it. The ship's half of the answer lives next door in
    /// <c>Map.ShipSeats.cs</c>.</para>
    ///
    /// <para><b>And the two rooms can be on ONE DECK at the same time</b>, which is why the fall-through is
    /// written the way it is rather than as an <c>else</c>. A docked complex is welded onto the ship's own
    /// plan and keeps every console she has (<c>HavenInterior.BuildComplex</c> seeds itself from
    /// <c>DeckPlan.Ship.Consoles</c>), so a captain clamped on can walk down the tube and sit in his own
    /// cantina — and a press there is a <c>BarTop</c> that matches no top the BAR published. Answering null
    /// on that would be the ship's seats going dead the moment she docked, which is the one state a player
    /// would find in the first minute.</para>
    /// </summary>
    private BarTopUnderfoot? TheBarTopUnderfoot()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { } spot
            || spot.Kind is not (DeckPlan.ConsoleKind.BarTop or DeckPlan.ConsoleKind.ShipDesk))
        {
            return null;
        }

        if (spot.Kind == DeckPlan.ConsoleKind.BarTop && TheDockedBar() is { } bar)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
            for (int i = 0; i < bar.Tops.Count; i++)
            {
                DeckReachability.Point top = bar.Tops[i];
                if (Math.Abs(top.X - spot.X) >= 0.5 || Math.Abs(top.Y - spot.Y) >= 0.5)
                {
                    continue;
                }

                // No place at it, so there is no seat here — answered as an absence rather than by sitting
                // the captain down inside the counter, which is §13.15's own sentence about measured
                // coordinates.
                if (BesideThisTop(top, walls) is not { } chair)
                {
                    return null;
                }

                return new BarTopUnderfoot(
                    i, $"bar:{bar.BodyId}:{BarWatch}:{i}", BarWatch, chair.X, chair.Y,
                    HavenInterior.BarTopSeats,
                    SittingAlone.BarSetting(HavenInterior.BarNameOf(bar.BodyId) ?? ""),
                    SittingAlone.OwnTablePlate,
                    // A station bar is one loud room with a window in it: no cabinets, no curtains, nothing
                    // to dog. And it is ashore, so the room's own people can and do cross the floor to it.
                    Quiet: false, Aboard: false);
            }
        }

        return TheShipsOwnSeatUnderfoot(spot);
    }

    /// <summary>#1016 QA · <c>?barcase=1</c> — the owner's own bug, in one URL. Set in Map.Sim's cheat
    /// parse, which turns <c>?ashore=1</c> on with it: a case worked in a bar needs a bar to be standing
    /// in.</summary>
    private bool _barCaseCheat;

    /// <summary>
    /// #1016 QA · <b>SIT THE CAPTAIN AT A TOP IN THE DOCKED BAR, WITH PAPERS IN THE SLEEVE.</b>
    ///
    /// <para>The exact seat the owner filed this issue from — a takeable top in a station bar, a sleeve with
    /// something in it, and the strip's <b>Work the case</b> button one press away. Until #973 L5b there was
    /// no such seat to boot into, and until this issue there was nothing behind the button when you got
    /// there; the shortest honest route to the bug was launch, dock, walk ship → airlock → tube →
    /// immigration hall → bar, find a free top, and already be carrying paperwork off a moon. That is not a
    /// route anybody re-runs, which is a large part of why a dead button survived a whole lane.</para>
    ///
    /// <para>It walks the LAST leg only, and through the room's own verb: the tops are the bar's published
    /// list, the chair is <c>HavenInterior.BesideATop</c>'s sounding (asked inside <c>TheBarTopUnderfoot</c>,
    /// never measured here), and the sit itself is <c>Seating.TryTakeBarTop</c> — the same [E] a player
    /// presses. A cheat that assembled its own sitting would be demonstrating a seat that does not ship,
    /// which is this repo's first named bug class wearing a dev row.</para>
    ///
    /// <para>Called straight after the ashore walk, which is what puts a deck under the captain's feet.</para>
    /// </summary>
    private void SitAtABarTopIfAsked()
    {
        if (!_barCaseCheat)
        {
            return;
        }

        if (TheDockedBar() is not { } bar || bar.Tops.Count == 0)
        {
            ShowPulseMessage(
                "🧪 DEV ?barcase=1: this berth has no bar with tops in it. Try &dock=the-space-bar.");
            return;
        }

        SeedTheSpreadFinds();

        // A bar top is drawn and does not collide (#973 L5b), so the console under the captain's feet is the
        // one they are standing on — which is exactly the question [E] asks of the room.
        foreach (DeckReachability.Point top in bar.Tops)
        {
            StandCaptainAt(top.X, top.Y, "you stop at a free top");
            if (TryTakeBarTop())
            {
                ShowPulseMessage(
                    "🧪 DEV ?barcase=1: sat at a top in "
                    + (HavenInterior.BarNameOf(bar.BodyId) ?? "the bar")
                    + " with three finds in the sleeve — and NO excursion under you (#1016). Press \"Work "
                    + "the case\" on the strip, then dig a paper: the bar fills on the strip, the book takes "
                    + "the entry, and the register remembers it across a save.");
                return;
            }
        }

        ShowPulseMessage("🧪 DEV ?barcase=1: no top in this room had a place at it to stand.");
    }

    /// <summary>
    /// #973 L0 · THE CAPTAIN, ALONE, AT A TOP IN THIS BAR — the seat family's own two answers, asked and never
    /// re-derived.
    ///
    /// <para><b>It is false at every berth in the game today, and that is not a bug in this line.</b> There is
    /// no way to sit down in a docked bar: all seven sitting sites are gated on a <c>SurfaceExcursion</c> and a
    /// berth has none. So Fess works his beat ashore and never pitches, which is honest — a salesman
    /// teleporting a card at a captain who cannot sit down is the opposite of the practice the owner asked
    /// for. When the haven bar grows a top the captain can take, this one predicate starts answering true and
    /// the rest of the walk is already built.</para>
    ///
    /// <para><b>#1016 · AND A SEAT ON YOUR OWN BOAT IS NOT A SEAT IN THIS BAR.</b> A docked complex is the
    /// ship's own plan with a station welded onto it, so a captain clamped on can walk down the tube and take
    /// a top in his own cantina — and every other clause here would say yes to that. The two walkers who read
    /// this predicate are both gated on <see cref="InTheBar"/> as well and would not have moved; the flag is
    /// added anyway, because a predicate that is only right because of where its callers happen to be checked
    /// is the shape of a bug rather than of a law. Sitting alone is a choice to be findable BY THIS ROOM.</para>
    /// </summary>
    private bool TheCaptainIsSittingAloneInTheBar() =>
        CaptainIsSeated && SeatedAlone
        && SeatedTable is { Bench: false, Office: false, Aboard: false };
}
