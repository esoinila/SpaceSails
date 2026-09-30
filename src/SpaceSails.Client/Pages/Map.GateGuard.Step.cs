using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #618 · <b>ONE FRAME OF THE MAN AT THE DOOR.</b> Called from the surface tick straight after the room's own
/// walkers, so a shift turning over (which clears the band) has already happened when he is put back on it. On
/// his floor: he is kept on his feet, walked (at the door, on his round, or on your heels), and the captain
/// reaching him raises his card. On the surface: the mouth of the tube (<c>Map.GateGuard.Mouth.cs</c>).
/// Anywhere else he is not drawn.
/// </summary>
public partial class Map
{
    private void AdvanceTheManAtTheDoor(double dtRealSeconds)
    {
        if (_surface is not { } ex || OnWreck || ex.Gate is not { } man)
        {
            return;
        }

        double dt = Math.Min(dtRealSeconds, 0.1);
        if (ex.Floor == 0)
        {
            StepHimAtTheMouth(ex, man, dt);
            return;
        }

        Walker? him = TheManAfoot(ex.Walkers);
        if (ex.Floor != man.Floor || man.Taken || man.AtTheMouthFor >= 0
            || man.Visit.Posting == GateGuard.Posting.Absent)
        {
            if (him is not null)
            {
                ex.Walkers.Remove(him);
            }

            if (ex.Floor == man.Floor && man.Visit.Posting == GateGuard.Posting.Absent)
            {
                TheEmptyChairIsTold(man);
            }

            return;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        if (him is null)
        {
            _ = PutHimOnHisFeet(ex, man, walls);
            return;
        }

        if (man.Following)
        {
            HeFollowsYou(ex, man, him, dt, walls);
            return;
        }

        if (man.Visit.Posting == GateGuard.Posting.OnRound && man.Canteen is not null)
        {
            WalkHisRound(ex, man, him, dt, walls);
        }
        else
        {
            StepHimWhereHeIsGoing(man, him, dt, walls);
        }

        if (TheManKeepsTheWayDown(ex))
        {
            TheCaptainReachesHim(ex, man, him);
        }
    }

    // ── ON HIS FEET ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Put him on the floor, where his feet were — or at the door the first time. Nudged clear by the same
    /// <see cref="SpawnNudge"/> every placement goes through; if the ground will not take him he is simply not
    /// there this frame, and what is not drawn keeps nothing (<see cref="TheManKeepsTheWayDown"/>).
    /// </summary>
    private bool PutHimOnHisFeet(SurfaceExcursion ex, ManAtTheDoor man, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (ClearSpotFor(man.StandingAt ?? man.Post, walls) is not { } from)
        {
            return false;
        }

        DeckReachability.Point to = man.Following ? from
            : man.Leg == RoundLeg.WalkingOut && man.Canteen is { } canteen ? canteen
            : man.Leg == RoundLeg.AtTheCanteen && man.Canteen is { } there ? there
            : man.Post;
        Errand errand = man.Following ? Errand.GateFollowing
            : man.Leg == RoundLeg.AtTheDoor ? Errand.GateKeeping
            : Errand.GateOnHisRound;
        return PlanTheManAtTheDoor(ex, man, from, to, errand, walls);
    }

    /// <summary>A spot a body fits on, as near the one asked for as the stone allows.</summary>
    private static DeckReachability.Point? ClearSpotFor(
        DeckReachability.Point want, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        SpawnNudge.Result spot = SpawnNudge.Clear(want.X, want.Y, DeckPlan.AvatarRadius, walls);
        return spot.Failed ? null : new DeckReachability.Point(spot.X, spot.Y);
    }

    /// <summary>Plan one of his walks and put him first in the band — he is the one figure the floor always has
    /// room for, because a door kept by a man nobody can see would be a sentence about a world not drawn. A walk
    /// of no length (he is already where he is going) is planned from where he stands to itself, and arrives on
    /// its first step: a man standing still.</summary>
    private bool PlanTheManAtTheDoor(
        SurfaceExcursion ex, ManAtTheDoor man, DeckReachability.Point from, DeckReachability.Point to,
        Errand errand, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        DeckReachability.Point end = ClearSpotFor(to, walls) ?? to;
        NpcWalk? walk = OnFoot(GateGuard.Plate, new NpcWalk.Bound("", end.X, end.Y), from, walls,
                               errand == Errand.GateFollowing ? NpcWalk.NoPersonalSpace : NpcWalk.PersonalSpaceInRadii);
        if (walk is null)
        {
            return false;
        }

        if (TheManAfoot(ex.Walkers) is { } old)
        {
            ex.Walkers.Remove(old);
        }

        ex.Walkers.Insert(0, new Walker { Walk = walk, Table = -1, For = errand });
        man.StandingAt = from;
        StateHasChanged();
        return true;
    }

    /// <summary>Step him along whatever walk he is on, keep his feet on the record, and when he is standing,
    /// have him look at the captain's hands.</summary>
    private void StepHimWhereHeIsGoing(
        ManAtTheDoor man, Walker him, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (him.Walk.Afoot)
        {
            him.Walk.Step(dt, walls, _avatarX, _avatarY);
            man.StandingAt = new DeckReachability.Point(him.Walk.X, him.Walk.Y);
            return;
        }

        him.Walk.LookTowards(_avatarX, _avatarY);
    }

    // ── THE ROUND ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #618 · <b>HIS ROUND.</b> At the door for <see cref="GateGuard.AtTheDoorSeconds"/>; then the walk to the
    /// canteen, told once, the first time; then the canteen until the away clock says six minutes less the walk
    /// home (<see cref="GateGuard.HeHeadsBack"/>); then home. The door is manned when he is back at it and not
    /// before (<see cref="HeIsAtTheDoor"/>), which is the whole of what "watch him and time him" asks.
    /// </summary>
    private void WalkHisRound(
        SurfaceExcursion ex, ManAtTheDoor man, Walker him, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        man.RoundClock += dt;
        StepHimWhereHeIsGoing(man, him, dt, walls);

        // The walk away is told once, the first time — into a free pulse slot, never over another line.
        if (!man.RoundTold && man.Leg != RoundLeg.AtTheDoor && _pulse.Message is null)
        {
            man.RoundTold = true;
            ShowPulseMessage(GateGuard.RoundLine);
        }

        DeckReachability.Point here = new(him.Walk.X, him.Walk.Y);

        switch (man.Leg)
        {
            case RoundLeg.AtTheDoor when man.RoundClock >= GateGuard.AtTheDoorSeconds && man.Canteen is { } canteen:
                if (PlanTheManAtTheDoor(ex, man, here, canteen, Errand.GateOnHisRound, walls))
                {
                    man.Leg = RoundLeg.WalkingOut;
                    man.RoundClock = 0;
                }

                break;

            case RoundLeg.WalkingOut when !him.Walk.Afoot:
                man.Leg = RoundLeg.AtTheCanteen;
                man.WalkOutSeconds = man.RoundClock;
                break;

            case RoundLeg.AtTheCanteen when GateGuard.HeHeadsBack(man.RoundClock, man.WalkOutSeconds):
                if (PlanTheManAtTheDoor(ex, man, here, man.Post, Errand.GateOnHisRound, walls))
                {
                    man.Leg = RoundLeg.WalkingBack;
                }

                break;

            case RoundLeg.WalkingBack when !him.Walk.Afoot:
                man.Leg = RoundLeg.AtTheDoor;
                man.RoundClock = 0;
                if (PlanTheManAtTheDoor(ex, man, here, man.Post, Errand.GateKeeping, walls))
                {
                    man.AtHisElbow = false;
                }

                break;
        }
    }

    // ── ON YOUR HEELS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #618 · <b>HE FOLLOWS.</b> The stringer's own shape (<c>Map.CarryThePress.Walk.cs</c>): he keeps the
    /// coat's band behind the captain (<see cref="TheTailBehindYou.HoldsHisBand"/>) and plans a new place in it
    /// from his own feet whenever the captain walks out of it. He says nothing. What ends it is the captain
    /// coming out under the lid (<see cref="HeFollowsYouIntoTheLight"/>).
    /// </summary>
    private void HeFollowsYou(
        SurfaceExcursion ex, ManAtTheDoor man, Walker him, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        // He comes OFF the wall: the first frame after the noise is a walk toward you whatever the range, so a
        // captain who made the noise from inside the band still sees him move.
        bool offTheWall = him.For != Errand.GateFollowing;
        if (him.Walk.Afoot && !offTheWall)
        {
            him.Walk.Step(dt, walls, _avatarX, _avatarY);
            man.StandingAt = new DeckReachability.Point(him.Walk.X, him.Walk.Y);
            return;
        }

        double dx = him.Walk.X - _avatarX, dy = him.Walk.Y - _avatarY;
        if (!offTheWall && TheTailBehindYou.HoldsHisBand(Math.Sqrt((dx * dx) + (dy * dy))))
        {
            him.Walk.LookTowards(_avatarX, _avatarY);
            return;
        }

        DeckReachability.Point from = new(him.Walk.X, him.Walk.Y);
        // "Behind" is measured from HIM, not from the captain's nose: he closes on you from the side he is
        // coming from, rather than walking round you to stand where the coat would. (Watched headless: keyed to
        // the captain's heading he walked AWAY into the hall behind a captain who stood facing the car.)
        double heading = Math.Atan2(_avatarY - him.Walk.Y, _avatarX - him.Walk.X);
        // Off the wall he closes to the NEAR edge of the band first — a man who has been waiting all shift for a
        // reason comes right up; after that he keeps the coat's own order, the middle of the band first.
        IEnumerable<double> ranges = offTheWall
            ? TheTailBehindYou.TheRangesHeTries.Reverse()
            : TheTailBehindYou.TheRangesHeTries;
        foreach (double range in ranges)
        {
            foreach (double side in TheTailBehindYou.TheSidesHeSounds)
            {
                DeckReachability.Point want = new(
                    _avatarX + (Math.Cos(heading + side) * range), _avatarY + (Math.Sin(heading + side) * range));
                if (ClearSpotFor(want, walls) is { } spot
                    && PlanTheManAtTheDoor(ex, man, from, spot, Errand.GateFollowing, walls))
                {
                    return;
                }
            }
        }

        him.Walk.LookTowards(_avatarX, _avatarY);
    }

    // ── THE EMPTY CHAIR ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Absent: the chair is told once, the first time the captain comes within reach of the door — and,
    /// with the stringer's courtesy, only into a FREE pulse slot, so the car's own arrival line is not written over
    /// and this one is not lost under it (watched headless: the two were said on the same frame).</summary>
    private void TheEmptyChairIsTold(ManAtTheDoor man)
    {
        if (man.AbsentTold || _pulse.Message is not null
            || !Within(_avatarX, _avatarY, man.Post.X, man.Post.Y, GateGuard.ReachDu))
        {
            return;
        }

        man.AbsentTold = true;
        ShowPulseMessage(GateGuard.AbsentLine);
    }

    /// <summary>
    /// #618 · <b>THE CHAIR BY THE DOOR, with the coat on it.</b> Drawn only on an Absent window, on the floor he
    /// keeps, at his post: the chair is a plate in the building's glyph idiom and carries no word. Appended the
    /// way what you left is, so the Hive's generator and the audit that walks it are untouched, and every other
    /// floor and every other window builds exactly the deck it always did.
    /// </summary>
    private void ComposeTheEmptyChair(SurfaceExcursion ex)
    {
        if (ex.Gate is not { Visit.Posting: GateGuard.Posting.Absent } man || ex.Floor != man.Floor)
        {
            return;
        }

        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            [], [], [((float)man.Post.X, (float)man.Post.Y, ChairWithACoat)], []));
    }

    /// <summary>The chair, and the coat on it. Two glyphs and no word.</summary>
    private const string ChairWithACoat = "🪑🧥";
}
