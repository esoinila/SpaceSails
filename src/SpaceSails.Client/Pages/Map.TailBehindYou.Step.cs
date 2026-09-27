using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · ONE FRAME OF BEING FOLLOWED (#1062, #1229) — <c>StepTheCoat</c> and every question it asks on the
/// way: the doorway between the two of them, whether his walk is still worth walking, where he looks last,
/// the wrong floor he goes and asks, the one place a tell becomes a saying, and the re-badge.
///
/// <para>Split out of <c>Map.TailBehindYou.cs</c> under #251 as a pure move: two runs of the base file, with
/// THE BURN (<c>Map.TailBehindYou.Burn.cs</c>) lifted out from between them. No member renamed, re-scoped or
/// re-ordered, and no field.</para>
/// </summary>
public partial class Map
{
    // ── ONE FRAME OF BEING FOLLOWED ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>ONE FRAME OF THE MAN BEHIND YOU.</b> The two clocks, the two tells, the band he keeps, and
    /// the walk out when he has finally got nothing to look at.
    ///
    /// <para>Everything is asked through <see cref="FootTail.InPlainSight"/> — the one look, at the one
    /// range, over the deck's own stone — and the mover it is asked about is minted by
    /// <see cref="TheTailBehindYou.AsAMover"/>, which is #793's seam finally being filled by something that
    /// declares itself rather than by a bench inferring a tail out of two positions.</para>
    /// </summary>
    /// <returns>Whether anything happened that the page should redraw for.</returns>
    private bool StepTheCoat(
        Walker who, double dt, in HavenInterior.BarFloor bar,
        IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        // ── HE HAS BEEN SHAKEN, AND HE IS LEAVING ────────────────────────────────────────────────────────
        if (who.For == Errand.AskingTheWrongFloor)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (who.Walk.Afoot)
            {
                return false;
            }

            _barAfoot.RemoveAt(slot);
            return true;
        }

        FootTail.Mover him = TheTailBehindYou.AsAMover(who.Walk.X, who.Walk.Y);
        bool inSight = FootTail.InPlainSight(_avatarX, _avatarY, in him, walls);
        double dx = who.Walk.X - _avatarX, dy = who.Walk.Y - _avatarY;
        double rangeDu = System.Math.Sqrt((dx * dx) + (dy * dy));

        // ── THE CLOCK THAT LOSES HIM ─────────────────────────────────────────────────────────────────────
        //
        // It is the SAME question read from his side: the look is symmetric (a wall between two people is
        // between them both ways) and the range is one distance. What breaks it ashore is stone — the bar's
        // south wall with its one doorway, the ring's sealed edges, the gangway, and at Selene Gate a glass
        // tube with a blind end. Nothing ashore can be shut, so nothing here pretends a door helps.
        _coatBlind = inSight ? 0 : _coatBlind + dt;
        if (inSight)
        {
            (_coatLastX, _coatLastY) = (_avatarX, _avatarY);
        }

        if (TheTailBehindYou.HeIsLost(_coatBlind))
        {
            return HeGoesAndAsksTheWrongFloor(who, walls, slot);
        }

        // ── (i) THE CHAIR THAT FACES THE DOOR ────────────────────────────────────────────────────────────
        //
        // The sit is the whole cost of this one: walking is refused while seated, so a captain who takes this
        // reading has given up the floor for as long as it takes. The door is the bar's own — the room has
        // exactly one way in that a person may walk through, which is what makes "you can see the door" a
        // sentence about this room rather than a figure of speech.
        (double doorX, double doorY, _) = HavenInterior.BarThreshold;
        bool watchingTheDoor =
            CaptainIsSeated
            && TheTailBehindYou.ThisChairSeesTheDoor(_avatarX, _avatarY, doorX, doorY, walls)
            && inSight;
        _coatExposure = watchingTheDoor ? _coatExposure + dt : 0;

        bool told = false;
        if (watchingTheDoor && TheTailBehindYou.NoticedFromTheChair(_coatExposure))
        {
            told = YouHaveNoticedHim(TheTailBehindYou.FromThisChairLine);
        }

        // ── (ii) THE SAME COAT THROUGH TWO DOORS ─────────────────────────────────────────────────────────
        //
        // #1229 · The doorway he is standing in is also the way OUT from where he is standing, whether or not
        // the captain happened to be looking — which is why it is written down outside the sight clause. What
        // the captain SEES is the tell; what the man knows is the room he is in and the door he came through.
        if (TheDoorwayHeIsIn(who.Walk.X, who.Walk.Y) is { } inADoorway)
        {
            _coatCameInBy = inADoorway;
        }

        if (inSight && TheDoorwayHeIsIn(who.Walk.X, who.Walk.Y) is { } leaf && _coatDoors.Add(leaf)
            && TheTailBehindYou.TwoDoorsRunning(_coatDoors.Count))
        {
            told = YouHaveNoticedHim(TheTailBehindYou.TwoDoorsLine) || told;
        }

        // ── AND HE IS A ROOM BEHIND YOU ──────────────────────────────────────────────────────────────────
        //
        // #1229 · The captain has walked out and the man is still inside. He gives it ONE look and then comes
        // through the same doorway — which is the two-door tell being an honest SEQUENCE for the first time:
        // posted inside, the captain goes, he follows through door one, the captain takes a second doorway,
        // he follows through door two, told. Until now the tell was green because he was PLANTED outside the
        // room before the captain moved and was therefore already on the exit path (#1208/#1231).
        bool aRoomBehind = !TheSameRoom(who.Walk.X, who.Walk.Y, _avatarX, _avatarY, in bar);
        _coatARoomBehind = aRoomBehind ? _coatARoomBehind + dt : 0;
        bool heMayFollow = !aRoomBehind || _coatARoomBehind >= TheTailBehindYou.SecondsBeforeHeFollowsYouOut;

        // ── AND THE ROUTE HE IS ON ───────────────────────────────────────────────────────────────────────
        //
        // The docblock has always said he is re-plotted when his route has run out AND the captain has walked
        // out from under it. #1229 · the second half of that `and` is now TRUE: a route whose far end is no
        // longer a place he could stand and see the captain from is a route to nowhere, and walking it out
        // before noticing is how a man ends up staring at a wall the captain left thirty seconds ago.
        //
        // It is asked only while he HAS the captain, for the reason the blind clause below exists: a man who
        // cannot see you has nothing to re-plot against, and dropping his route every frame would make him a
        // tracker that never commits to anything.
        if (who.Walk.Afoot)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (who.Walk.Afoot
                && !(inSight && heMayFollow && !aRoomBehind
                     && HisRouteHasGoneStale(who.Walk, in bar, walls)))
            {
                return told || !who.Walk.Afoot;
            }
        }
        else
        {
            who.Walk.LookTowards(_avatarX, _avatarY);
        }

        if (!heMayFollow)
        {
            return told;   // one look, and then he comes after you. Not this frame.
        }

        // ── AND A MAN WHO CANNOT SEE YOU DOES NOT KNOW WHERE TO GO ───────────────────────────────────────
        //
        // This clause is the whole of what makes breaking his line a MOVE rather than a pause. Without it he
        // re-plots onto wherever the captain actually is, every frame, through stone he cannot see through —
        // which is not a tail, it is a tracker, and it would put the losing rule above out of reach: he would
        // simply walk round whatever the captain hid behind and pick the line straight back up.
        //
        // So: while he HAS the captain he holds his place. Blind, the ONLY place he has any reason to walk to
        // is where he last had him — and when he gets there and it is empty, the clock above runs out and he
        // is done. That is the honest reading of the beat as well: what nine seconds of stone buys the
        // captain is not invisibility, it is the man's last good guess going stale.
        //
        // #1229 · "holds his place" is now the two behaviours and not one. In a room too small for his band
        // he is POSTED, and a post is kept until the LINE breaks — he does not shuffle along the wall after a
        // captain crossing the room, because the whole of what he is doing is standing by the door. Out where
        // the band fits, the band is what he holds, exactly as before.
        if (!aRoomBehind && inSight && !who.Walk.Afoot
            && HeIsAlreadyWhereHeShouldBe(rangeDu, in bar, walls))
        {
            return told;
        }

        // #1229 · A ROOM BEHIND, HE GOES TO THE DOORWAY — and to the doorway itself, not to a standing place
        // chosen against a captain who is no longer in the room. It is the one thing he knows: he watched the
        // man he is paid to keep go through it. Whether he can still SEE him is beside the point, and this is
        // the one place in this file where that is true — everywhere else a man who cannot see you does not
        // know where to go, and he still does not: the doorway is not where the captain IS, it is where the
        // captain WENT.
        DeckReachability.Point? going =
            aRoomBehind ? TheDoorwayBetweenYou(who.Walk.X, who.Walk.Y, in bar)
            : inSight ? WhereHeStands(in bar, walls)
            : WhereHeLooksForYouLast(who.Walk.X, who.Walk.Y, in bar, walls);

        if (going is { } spot
            && OnFoot(TheTailBehindYou.Plate, new NpcWalk.Bound("", spot.X, spot.Y),
                      new DeckReachability.Point(who.Walk.X, who.Walk.Y), walls) is { } next)
        {
            _barAfoot[slot] = RebadgeTheCoat(who, next, Errand.BehindYou);
            return true;
        }

        return told;
    }

    /// <summary>#1229 · <b>THE DOORWAY BETWEEN THE TWO OF THEM</b> — the room's own published one, taken from
    /// the side the CAPTAIN is on, so a man following him out steps through it and a man following him back in
    /// steps through it the other way. One step past the line either way
    /// (<see cref="HavenInterior.BarThreshold"/> and its mirror), never a coordinate typed here.
    ///
    /// <para>He is not on a post while he is walking through a door, so the flag comes off: what he does when
    /// he arrives is decided by the room he arrives in.</para></summary>
    private DeckReachability.Point TheDoorwayBetweenYou(
        double hisX, double hisY, in HavenInterior.BarFloor bar)
    {
        _coatPosted = false;

        // #1199's walk is a room with ONE way in, and its mouth is that way — for a captain who has stepped
        // into it and for a man who is standing in it while the captain is not.
        if (HavenInterior.InTheObservationWalk(bar.BodyId, _avatarX, _avatarY)
                != HavenInterior.InTheObservationWalk(bar.BodyId, hisX, hisY)
            && HavenInterior.TheWalksMouthAt(bar.BodyId) is { } mouth)
        {
            return mouth;
        }

        if (_avatarY > bar.FloorY)
        {
            (double inX, double inY, _) = HavenInterior.BarThreshold;
            return new DeckReachability.Point(inX, inY);
        }

        (double outX, double outY) = HavenInterior.TheDoorstepOutsideTheBar;
        return new DeckReachability.Point(outX, outY);
    }

    /// <summary>#1229 · <b>IS HE ALREADY STANDING WHERE THIS ROOM WANTS HIM?</b> A post is kept until the
    /// LINE breaks — he does not shuffle along a wall after a captain crossing the room, because the whole of
    /// what he is doing is standing by the door. A band is kept until the captain walks out of it, exactly as
    /// before. Asked only while he has the captain: a blind man is not choosing anything.</summary>
    private bool HeIsAlreadyWhereHeShouldBe(
        double rangeDu, in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        ThisRoomCanHoldHisBand(in bar, walls)
            ? !_coatPosted && TheTailBehindYou.HoldsHisBand(rangeDu)
            : _coatPosted;

    /// <summary>#1229 · <b>IS THE WALK HE IS ON STILL WORTH WALKING?</b> Two ways it stops being: the captain
    /// has walked out from under its far end, or the ROOM has changed its mind about what he should be doing
    /// — a man who set off to keep a band while the captain was by the door, and is half-way across a room
    /// that cannot hold one now the captain has sat down at a top, is walking to the wrong place. Finishing
    /// first is how he ends up standing in the middle of the floor doing nothing.</summary>
    private bool HisRouteHasGoneStale(
        NpcWalk walk, in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        TheCaptainHasWalkedOutFromUnder(walk, in bar, walls)
        || ThisRoomCanHoldHisBand(in bar, walls) == _coatPosted;

    /// <summary>
    /// #1229 · <b>WHERE A BLIND MAN GOES.</b> Where he last had the captain — and, when the room is too small
    /// for his band, a WALL SPOT with a line to that place rather than the place itself.
    ///
    /// <para>The design's own clause: <i>he re-posts only when the line is broken, and only to another wall
    /// spot</i>. A posted man who lost his line and then walked out into the middle of the room to stand on
    /// the square the captain was last on would not be keeping station any more; he would be searching, in
    /// the open, which is the opposite of what a man who has not ordered is doing. Where the band fits, the
    /// spot itself is where he goes, exactly as before — there are no walls to hug on a concourse.</para>
    ///
    /// <para>Null all the way down is a man with nowhere to go, which the caller reads as "stay put": the
    /// blind clock is running either way and it is the thing that ends him.</para></summary>
    private DeckReachability.Point? WhereHeLooksForYouLast(
        double x, double y, in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (WhereHeLastHadYou(x, y) is not { } last)
        {
            return null;
        }

        return _coatPosted
            ? ThePostInThisRoom(last.X, last.Y, in bar, walls) ?? last
            : last;
    }

    /// <summary>#1229 · <b>HAS THE CAPTAIN WALKED OUT FROM UNDER HIS ROUTE?</b> The far end of a walk is a
    /// place that was chosen because a man standing on it could see the captain from it. When it stops being
    /// one — the captain has left the room it is in, or put stone between it and himself — the walk is a walk
    /// to nowhere, and finishing it first is how a man ends up staring at a wall.
    ///
    /// <para>Asked of the route's own bound (<see cref="NpcWalk.For"/>) rather than of a copy kept here: two
    /// records of where somebody is going is the seam this house has been bitten by before.</para></summary>
    private bool TheCaptainHasWalkedOutFromUnder(
        NpcWalk walk, in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        !HeCouldStandAt(walk.For.X, walk.For.Y, _avatarX, _avatarY, in bar, walls);

    /// <summary>
    /// #1062 · <b>THE CORRIDOR BEHIND YOU IS ONLY A CORRIDOR.</b> He has had nothing to look at for as long
    /// as it takes, so he goes — out through the room's own doorway, on the one planner, and off the floor
    /// when the route runs out.
    ///
    /// <para><b>And whether anything is SAID about it depends entirely on whether the captain ever knew.</b>
    /// A captain who never noticed him is told nothing, for ever: there was a man, he stood about, he left,
    /// and nothing in this game will ever mention it. That is #1062's inference horror said in the one place
    /// it would have been easiest to spoil.</para>
    /// </summary>
    private bool HeGoesAndAsksTheWrongFloor(
        Walker who, IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        _coatLost = true;
        (double doorX, double doorY, _) = HavenInterior.BarThreshold;

        if (_coatSeen)
        {
            ShowPulseMessage(TheTailBehindYou.LostLine, PulseRank.Beat);
            string place = DockedStationName();
            FileNoteAbout(
                TheTailBehindYou.NoteLine(place), TheTailBehindYou.Glyph, TheTailBehindYou.Subjects(place));
        }

        if (OnFoot(TheTailBehindYou.Plate, new NpcWalk.Bound("", doorX, doorY),
                   new DeckReachability.Point(who.Walk.X, who.Walk.Y), walls) is not { } away)
        {
            _barAfoot.RemoveAt(slot);   // no way out from where he is standing; he is simply not here.
            return true;
        }

        _barAfoot[slot] = RebadgeTheCoat(who, away, Errand.AskingTheWrongFloor);
        return true;
    }

    /// <summary>#1062 · The one place a tell is turned into a saying, so the two tells can never come to two
    /// opinions about what noticing means. It latches, it says the authored line once, and it never says
    /// anything a second time.</summary>
    /// <returns>Whether this call was the one that did it.</returns>
    private bool YouHaveNoticedHim(string line)
    {
        if (_coatSeen)
        {
            return false;
        }

        _coatSeen = true;
        ShowPulseMessage(line, PulseRank.Beat);
        return true;
    }

    /// <summary>#1062 · The last place he had the captain, or null if he has not had him yet or is already
    /// standing on it. It is a PLACE and never a direction: a man who has lost you walks to where you were,
    /// and if you are not there any more that is the end of it.</summary>
    private DeckReachability.Point? WhereHeLastHadYou(double x, double y)
    {
        if (double.IsNaN(_coatLastX))
        {
            return null;
        }

        double dx = _coatLastX - x, dy = _coatLastY - y;
        return (dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius
            ? new DeckReachability.Point(_coatLastX, _coatLastY)
            : null;
    }

    /// <summary>#1062 · Which of the deck's own doorways he is standing in, or null. The plan's UNLOCKED
    /// doors only — a leaf that never opens is not a way through a room, and counting one would let a man
    /// leaning on the cellar door be half of a tell he never earned. The reach is
    /// <see cref="DeckPlan.DoorOpenRadius"/>, the game's own statement of how near a body has to be to a
    /// doorway to be in it.</summary>
    private int? TheDoorwayHeIsIn(double x, double y)
    {
        DeckPlan.Door[] doors = _deckPlan.Doors;
        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i].Locked)
            {
                continue;
            }

            double mx = (doors[i].X1 + doors[i].X2) / 2.0, my = (doors[i].Y1 + doors[i].Y2) / 2.0;
            double dx = mx - x, dy = my - y;
            if ((dx * dx) + (dy * dy) <= DeckPlan.DoorOpenRadius * DeckPlan.DoorOpenRadius)
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>#1062 · The same man with a new route and a new errand on him. <see cref="Walker"/> is
    /// init-only everywhere that matters, so both change by replacing the record — the same reason slice 1's
    /// own re-badge exists one file over.</summary>
    private static Walker RebadgeTheCoat(Walker who, NpcWalk walk, Errand errand) => new()
    {
        Walk = walk, Table = who.Table, For = errand, Who = who.Who,
        Cabinet = who.Cabinet, StillWanted = who.StillWanted, OnArrive = who.OnArrive,
    };
}
