using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1332 C · <b>THE CLERK WHO KEEPS THE OFFICE'S HOURS.</b> On his watch he steps out of the office door, walks to
/// the car nearest it and rides up, and the office stands empty behind him for the rest of the watch. He never
/// speaks, carries no card and has no name: the deck draws the plate <see cref="PreservationOffice.ClerkPlate"/>
/// over a walker, and that is the whole of him (Kosh: the clerk is a walker, not a card).
///
/// <para><b>He is a body on the captain's floor and a clock on every other one</b>, GILT-EYE's night's pattern
/// (#1253 slice 2): the walk is timed from the first second of his watch at a body's own pace
/// (<see cref="NpcWalk.PaceDu"/>), so a captain who rides down part-way through it meets him where the clock says
/// he is, along his own line, and a captain who comes down after it finds the door ajar and nobody. A captain who
/// was waiting in the corridor as the watch turned sees the leaf go over and the man come out.</para>
///
/// <para>He is an ordinary <see cref="Errand.Leaving"/> walker in the room's own band — Kolt's shape: the generic
/// step walks him and the route running out at the car is his full stop. Found again by his plate, so the page
/// keeps no field for him.</para>
/// </summary>
public partial class Map
{
    /// <summary>#1332 C · The clerk, if he is on his feet on this floor.</summary>
    private Walker? TheClerkAfoot()
    {
        foreach (Walker w in _barAfoot)
        {
            if (w.For == Errand.Leaving
                && string.Equals(w.Walk.Plate, PreservationOffice.ClerkPlate, System.StringComparison.Ordinal))
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>
    /// #1332 C · <b>HIS LEG</b>: when he set off, from where, to where — or null when this is not his watch (or the
    /// berth has no office). From his doorstep to the landing of <see cref="HavenInterior.TheClerksCarAt"/>, both
    /// the room's own published squares. On the dev start's forced watch he sets off
    /// <see cref="PreservationOffice.DevSetOffSeconds"/> after the boot docked, so a tester sees the doorway first.
    /// </summary>
    private (double SetOff, DeckReachability.Point From, DeckReachability.Point To)? TheClerksLeg(string berth)
    {
        if (!ItIsTheClerksWatch()
            || HavenInterior.TheOfficeDoorstepAt(berth) is not { } door
            || HavenInterior.TheClerksCarAt(berth) is not { } car
            || HavenInterior.TheCageLandingAt(berth, car) is not { } landing)
        {
            return null;
        }

        double setOff = TheOfficeCheat == PreservationOffice.Cheat.Open
            ? _dockVisitSimTime + PreservationOffice.DevSetOffSeconds
            : PreservationOffice.SetsOffAt(PatronRota.WatchIndex(SimTime));
        return (setOff, door, landing);
    }

    /// <summary>
    /// #1332 C · <b>ONE FRAME OF THE CLERK.</b> While his clock has him on the walk and he is not already afoot, he
    /// is put on this floor where the clock says he is — along the straight line from his door to his car, at the
    /// fraction of the walk that has gone by, nudged clear of stone — and walked the rest of the way on the one
    /// planner. A leg with less than a body's width left is over: he is at the car.
    /// </summary>
    private void AdvanceTheClerk(in HavenInterior.BarFloor bar)
    {
        if (TheClerkAfoot() is { } afoot)
        {
            RideHimUpIfHeIsAtHisCar(bar.BodyId, afoot);
            return;
        }

        if (_barAfoot.Count >= WalkerBand || TheClerksLeg(bar.BodyId) is not { } leg)
        {
            return;
        }

        double dx = leg.To.X - leg.From.X, dy = leg.To.Y - leg.From.Y;
        double length = System.Math.Sqrt((dx * dx) + (dy * dy));
        if (PreservationOffice.Along(leg.SetOff, length / NpcWalk.PaceDu, SimTime) is not { } along
            || (1 - along) * length < AtHisCarDu)
        {
            return;   // not his walk — or so little of it left that he is at the car, which is where he stops being
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        double x = leg.From.X + (dx * along), y = leg.From.Y + (dy * along);
        SpawnNudge.Result spot = SpawnNudge.Clear(x, y, DeckPlan.AvatarRadius, walls);
        if (!spot.Failed)
        {
            (x, y) = (spot.X, spot.Y);
        }

        if (OnFoot(
                PreservationOffice.ClerkPlate, new NpcWalk.Bound("", leg.To.X, leg.To.Y),
                new DeckReachability.Point(x, y), walls) is not { } walk)
        {
            return;   // the floor refuses him from there. He stays a clock; nothing is placed at the car.
        }

        _barAfoot.Add(new Walker { Walk = walk, Table = -1, For = Errand.Leaving });
        StateHasChanged();
    }

    /// <summary>#1332 C · How near his car's landing he has to be to be AT it: the courtesy's own width plus a body
    /// — the reach GILT-EYE's night calls arrived (#1281), because a car's landing is the one square in the
    /// building that never clears, and a captain standing on it would otherwise hold him a body-width off it for
    /// ever. The same number keeps him from being dealt again: by the time the route has brought him this near,
    /// the clock has less than this left of his line.</summary>
    private static double AtHisCarDu => (NpcWalk.PersonalSpaceInRadii + 1) * DeckPlan.AvatarRadius;

    /// <summary>#1332 C · <b>AT THE CAR, HE RIDES UP</b> — and is not on this floor any more. Nothing is said.</summary>
    private void RideHimUpIfHeIsAtHisCar(string berth, Walker clerk)
    {
        if (HavenInterior.TheClerksCarAt(berth) is not { } car
            || HavenInterior.TheCageLandingAt(berth, car) is not { } landing)
        {
            return;
        }

        double dx = clerk.Walk.X - landing.X, dy = clerk.Walk.Y - landing.Y;
        if ((dx * dx) + (dy * dy) <= AtHisCarDu * AtHisCarDu)
        {
            _barAfoot.Remove(clerk);
            StateHasChanged();
        }
    }
}
