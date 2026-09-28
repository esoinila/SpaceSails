using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · PUTTING HIM ON A FLOOR (#1253) — placing him, keeping his leg clear of the captain, his short
/// name, and where his chair is.
///
/// <para>Split out of <c>Map.ObservationWalk.Night.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public partial class Map
{
    // ── PUTTING HIM ON A FLOOR ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1253 · <b>THE CAPTAIN IS ON HIS FLOOR, SO THERE HAS TO BE A MAN ON IT.</b> He is placed where the
    /// CLOCK says he is — along his own line, at the fraction of it that has gone by — and walked the rest of
    /// the way on the one planner.
    ///
    /// <para>Not at the leg's start, which would hand a captain who arrived late a man who had not moved; not
    /// at its end, which would hand him one who had already finished. The point is nudged clear of stone
    /// (<see cref="SpawnNudge"/>) for the reason every placement in this game is: a body dropped inside a
    /// wall is #602 with somebody else's shoulder in it.</para>
    ///
    /// <para>#1286 · <b>…AND CLEAR OF THE CAPTAIN, WHICH IS THE SAME SENTENCE.</b> The one square this floor
    /// is guaranteed to share is a car's LANDING — it is where the ride sets the captain down, where
    /// <c>[E]</c> finds the panel, and where the clock has this man standing on the frame a captain who
    /// followed him onto his own car arrives. So the placement dropped a body on the captain's own feet, and
    /// the route planned from there had its first lattice node back on the captain's square: every sub-step
    /// was inside <see cref="NpcWalk.PersonalSpaceInRadii"/> and pointed at him, so the walk was refused and
    /// kept for ever. Twelve screenshots over four minutes, byte-identical — <i>"a man standing exactly where
    /// the car put you, as though he had waited."</i></para>
    ///
    /// <para><b>It is the placement that moves, not the captain, and that is measured rather than preferred.</b>
    /// A ride cannot step the captain clear of a body, because at the moment of the ride there IS no body:
    /// <c>ForgetTheBarsFeet</c> empties the feet list on the way through the shaft and this man is dealt onto
    /// the new floor on the NEXT frame. What is left is a captain shifted a body-width unasked on every ride
    /// in the game, which is a bigger lie than the one it would fix. So he is put down at the first point of
    /// <b>his own line</b> that is a place a body can be — forward along the leg, never back, because the
    /// clock never runs backwards — and if no point of what is left of the leg is clear of the captain's
    /// elbow then the leg is shorter than a body-width and he is simply at the end of it.</para>
    /// </summary>
    private void PutHimOnThisFloor(in HavenInterior.BarFloor bar, string person)
    {
        if (_nightAfoot || _barAfoot.Count >= WalkerBand)
        {
            return;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        double gone = _nightLegSeconds <= 0
            ? 1
            : System.Math.Clamp((SimTime - _nightLegSince) / _nightLegSeconds, 0, 1);

        double x = _nightLegFrom.X + ((_nightLegTo.X - _nightLegFrom.X) * gone);
        double y = _nightLegFrom.Y + ((_nightLegTo.Y - _nightLegFrom.Y) * gone);
        (x, y) = AlongThisLegClearOfTheCaptain(x, y);
        SpawnNudge.Result spot = SpawnNudge.Clear(x, y, DeckPlan.AvatarRadius, walls);
        if (!spot.Failed)
        {
            (x, y) = (spot.X, spot.Y);
        }

        string plate = HisShortName(bar.BodyId, person);
        if (OnFoot(
                plate, new NpcWalk.Bound("", _nightLegTo.X, _nightLegTo.Y),
                new DeckReachability.Point(x, y), walls) is not { } walk)
        {
            return;   // the floor refuses him from there. He stays a clock; nothing is placed at the far end.
        }

        _barAfoot.Add(new Walker
        {
            Walk = walk, Table = -1, For = Errand.WalkingTheRoute, Who = person,
        });
        _nightAfoot = true;
        StateHasChanged();
    }

    /// <summary>
    /// #1286 · <b>THE FIRST POINT OF WHAT IS LEFT OF HIS LEG THAT IS NOT INSIDE THE CAPTAIN.</b> Forward
    /// along the line and never back — the clock does not run backwards, so a man the clock has got this far
    /// is never put down behind where it says he is.
    ///
    /// <para>The reach is the courtesy's OWN width plus a body — the same expression
    /// <see cref="HeIsAtTheEndOfThisLeg"/> reads, so the distance a walker is placed at and the distance the
    /// night calls arrived cannot come to two numbers. One body more than the berth because the ROUTE is
    /// planned from here: <c>AutoWalk</c> snaps its first node onto the lattice, which can put that node a
    /// stride back down the line, and a first node inside the berth is the freeze again with an extra step
    /// in front of it.</para>
    ///
    /// <para>When no point of the rest of the leg is clear, the leg itself is shorter than that — he is at
    /// the end of it, which is the answer <see cref="HeIsAtTheEndOfThisLeg"/> would give on the next frame
    /// anyway.</para>
    /// </summary>
    private (double X, double Y) AlongThisLegClearOfTheCaptain(double x, double y)
    {
        double reach = (NpcWalk.PersonalSpaceInRadii + 1) * DeckPlan.AvatarRadius;
        double dx = _nightLegTo.X - x, dy = _nightLegTo.Y - y;
        double left = System.Math.Sqrt((dx * dx) + (dy * dy));
        if (left <= 0)
        {
            return (x, y);
        }

        // Sampled at a fraction of a body's width, so the first clear point is the first one there is
        // rather than the first one a coarse stride happens to land on.
        double stride = DeckPlan.AvatarRadius / 2;
        for (double along = 0; along <= left; along += stride)
        {
            double px = x + (dx / left * along), py = y + (dy / left * along);
            double cx = px - _avatarX, cy = py - _avatarY;
            if ((cx * cx) + (cy * cy) >= reach * reach)
            {
                return (px, py);
            }
        }

        return (_nightLegTo.X, _nightLegTo.Y);
    }

    /// <summary>#1253 · The plate the deck draws over his head — the room's own short name for him, so a man
    /// followed down a corridor is the same man who was sitting at a top, and never a second spelling.</summary>
    private string HisShortName(string berth, string person)
    {
        foreach (HavenInterior.SeatedRegular r in
                 HavenInterior.ResolveRegulars(berth, _dockVisitSimTime, TheBarsChurn))
        {
            if (string.Equals(r.Id, person, System.StringComparison.Ordinal))
            {
                return r.ShortName;
            }
        }

        return person;
    }

    /// <summary>#1253 · Where his chair is, as a place a body can stand beside it — the rota's own seat,
    /// sounded by the room's own <c>BesideATop</c>, which is the same call the shipped first leg makes.
    /// Null when the rota is not seating him or the stone allows no side of his top.</summary>
    private DeckReachability.Point? WhereHisChairIs(string berth, string person)
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        foreach (HavenInterior.SeatedRegular seated in
                 HavenInterior.ResolveRegulars(berth, _dockVisitSimTime, TheBarsChurn))
        {
            if (seated.Present && string.Equals(seated.Id, person, System.StringComparison.Ordinal))
            {
                return BesideThisTop(new DeckReachability.Point(seated.X, seated.Y), walls);
            }
        }

        return null;
    }
}
