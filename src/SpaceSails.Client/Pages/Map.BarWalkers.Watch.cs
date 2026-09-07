using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.BarWalkers (the header note lives in Map.BarWalkers.cs) — #731 · WHO GOES, WHO COMES, AND
// WHEN. The scheduled half of the owner's proposal — scheduled for ambience, triggered for plot beats,
// both through one walker — said in the room he actually drinks in. Both directions are one arithmetic
// off the frozen watch, each move is dealt exactly once, and the lists are worked out ONCE PER VISIT and
// afterwards only read: asking a whole rota sixty times a second for something that cannot change while
// the captain is standing there is the lesson #731 v1 paid for underground with a floor plan per frame.
// The room's own feet are capped, because one person crossing a bar is something a captain looks at and
// four at once is a fire drill.
public partial class Map
{
    // ── #731 · WHO GOES, WHO COMES, AND WHEN ─────────────────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>WHAT THIS SHIFT HAS DECIDED, BY NOW.</b> The scheduled half of the owner's own proposal —
    /// <i>scheduled for ambience, triggered for plot beats; both through one walker</i> — said in the room he
    /// actually drinks in.
    ///
    /// <para>Both directions are one arithmetic (<see cref="Egress"/>, off the frozen
    /// <see cref="BarWatch"/>), and each move is dealt exactly once. <b>The lists are worked out once per
    /// visit and afterwards only read:</b> answering "who goes" needs the whole rota resolved, and asking that
    /// sixty times a second for something that cannot change while the captain is standing there is the same
    /// lesson #731 v1 paid for underground with a whole floor plan per frame.</para>
    ///
    /// <para>The room's own feet are capped at <see cref="Egress.MostAtOnce"/>: one person standing up and
    /// crossing a bar is something a captain looks at, and four at once is a fire drill.</para>
    /// </summary>
    private void DealTheBarsHours(in HavenInterior.BarFloor bar)
    {
        _barGoing ??= TheWatchDecidesWhoGoes(in bar);
        _barComing ??= TheWatchDecidesWhoComes(in bar);

        if ((_barGoing.Count == 0 && _barComing.Count == 0) || TheBarsOwnFeet() >= Egress.MostAtOnce)
        {
            return;
        }

        double into = IntoTheBarsWatch;
        DealWhatIsDue(in bar, _barGoing, into, leaving: true);
        DealWhatIsDue(in bar, _barComing, into, leaving: false);
    }

    /// <summary>#731 · One direction of the schedule, stepped down until the room is full of feet. The two
    /// halves differ in exactly one line, which is why they are not two loops.</summary>
    private void DealWhatIsDue(
        in HavenInterior.BarFloor bar, IReadOnlyList<Egress.Move> schedule, double into, bool leaving)
    {
        foreach (Egress.Move move in schedule)
        {
            if (into < move.AtSecondsIntoWatch || !_barDealt.Add((leaving ? "out:" : "in:") + move.Plate))
            {
                continue;
            }

            bool afoot = leaving ? TheyStandUpAndGo(in bar, move) : TheyComeOutOfTheBack(in bar, move);
            if (!afoot)
            {
                // No route, no chair, or nowhere to stand at that leaf. Nothing happens — which is the honest
                // answer, and never a body placed at the far end of a walk that could not be walked.
                _barDealt.Remove((leaving ? "out:" : "in:") + move.Plate);
            }

            if (TheBarsOwnFeet() >= Egress.MostAtOnce)
            {
                return;
            }
        }
    }

    /// <summary>#731 · THE SHIFT'S OWN LIST OF WHO FINISHES — the regulars this watch actually seated, run
    /// through Core's one deal. Asked of the rota's UNTOUCHED answer (no churn), because the schedule is a
    /// fact about the watch and must not change as the evening it describes plays out.</summary>
    private IReadOnlyList<Egress.Move> TheWatchDecidesWhoGoes(in HavenInterior.BarFloor bar)
    {
        var seated = new List<Egress.Occupant>();
        IReadOnlyList<HavenInterior.SeatedRegular> rota =
            HavenInterior.ResolveRegulars(bar.BodyId, _dockVisitSimTime);
        for (int i = 0; i < rota.Count; i++)
        {
            if (rota[i].Present)
            {
                seated.Add(new Egress.Occupant(i, rota[i].Id));
            }
        }

        return Egress.Departures(bar.BodyId, BarIsNotAFloor, BarWatch, seated, bar.Doors);
    }

    /// <summary>
    /// #731 · …AND OF WHO COMES OUT OF THE BACK. <i>"In the space bars there are lot of cases where we can
    /// have npcs arrive at bar from locked place."</i>
    ///
    /// <para>Only the regulars the rota has <see cref="PatronState.InTheBack"/> — <i>"away in the back / a
    /// locked room"</i> — are eligible, and that is the whole inference: a man who comes out of a leaf the
    /// captain cannot open was, according to the room's own bookkeeping, behind it. A regular who is simply
    /// <see cref="PatronState.Gone"/> is not at this station at all and does not get to materialise in the
    /// cellar. That distinction has been computed every watch since #410 and told to nobody but the barkeep;
    /// it is load-bearing now.</para>
    ///
    /// <para>The chair each would take is allotted here, off the room's own free list in the pool's own order,
    /// so two people walking in never head for one chair.</para>
    /// </summary>
    private IReadOnlyList<Egress.Move> TheWatchDecidesWhoComes(in HavenInterior.BarFloor bar)
    {
        IReadOnlyList<int> free = HavenInterior.FreePatronSeats(bar.BodyId, _dockVisitSimTime);
        if (free.Count == 0)
        {
            return [];
        }

        var expected = new List<Egress.Occupant>();
        IReadOnlyList<HavenInterior.SeatedRegular> rota =
            HavenInterior.ResolveRegulars(bar.BodyId, _dockVisitSimTime);
        int next = 0;
        foreach (HavenInterior.SeatedRegular r in rota)
        {
            if (r.State != PatronState.InTheBack || next >= free.Count)
            {
                continue;
            }

            expected.Add(new Egress.Occupant(free[next], r.Id));
            next++;
        }

        return expected.Count == 0
            ? []
            : Egress.Arrivals(bar.BodyId, BarIsNotAFloor, BarWatch, expected, bar.Doors);
    }

    /// <summary>
    /// #731 · <b>SOMEBODY FINISHES AND GOES.</b> The chair comes back empty in the same breath their legs
    /// start — one body, one place — and the deck is re-welded so the drawn room agrees with the walked one.
    ///
    /// <para>They give the captain a body's berth (<see cref="NpcWalk.PersonalSpaceInRadii"/>), which is the
    /// ruling on this issue said out loud: a captain standing in the door of a room somebody is trying to
    /// leave stops them, and being looked at is the content. They never clip and they never push.</para>
    /// </summary>
    private bool TheyStandUpAndGo(in HavenInterior.BarFloor bar, Egress.Move move)
    {
        if (TheRegular(bar.BodyId, move.Plate) is not { Present: true } who
            || move.Door < 0 || move.Door >= bar.Doors.Count)
        {
            return false;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        var from = new DeckReachability.Point(who.X, who.Y);
        UndergroundComplex.LockedDoor leaf = bar.Doors[move.Door];
        if (SurfaceCollision.Blocked(from.X, from.Y, DeckPlan.AvatarRadius, walls)
            || Egress.StandingPlaceAt(in leaf, DeckPlan.AvatarRadius, walls, from.X, from.Y) is not { } doorstep
            || OnFoot(who.ShortName, new NpcWalk.Bound(leaf.Sign, doorstep.X, doorstep.Y), from, walls)
                is not { } walk)
        {
            return false;
        }

        _barAfoot.Add(new Walker
        {
            Walk = walk, Table = move.TableIndex, For = Errand.Leaving, Who = who.Id,
        });

        // He is on his feet, so he is not in the chair. Said to the ROOM before anything else, because the
        // frame that draws him crossing the floor must not also draw him sitting where he was.
        _barLeft.Add(who.Id);
        RebuildDockedDeck();
        StateHasChanged();
        return true;
    }

    /// <summary>#731 · <b>…AND SOMEBODY COMES OUT OF THE BACK.</b> They step out of a leaf that has never
    /// opened for the captain, cross the floor on the captain's own lattice, and take a chair — and the chair
    /// is theirs only on the frame they reach it (see <see cref="StepTheBarsFeet"/>). Nothing is said.</summary>
    private bool TheyComeOutOfTheBack(in HavenInterior.BarFloor bar, Egress.Move move)
    {
        if (TheRegular(bar.BodyId, move.Plate) is not { } who
            || move.Door < 0 || move.Door >= bar.Doors.Count
            || HavenInterior.PatronSeatAt(move.TableIndex) is not { } chair)
        {
            return false;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        UndergroundComplex.LockedDoor leaf = bar.Doors[move.Door];

        // THE CHAIR FIRST, THEN THE DOORSTEP — #731 v1 paid for this order already. A leaf has two sides and
        // both can be standable; asked with no hint, half the time the answer is the room the captain has
        // never been in, from which there is no route to any chair at all.
        if (SurfaceCollision.Blocked(chair.X, chair.Y, DeckPlan.AvatarRadius, walls)
            || Egress.StandingPlaceAt(in leaf, DeckPlan.AvatarRadius, walls, chair.X, chair.Y)
                is not { } doorstep
            || OnFoot(who.ShortName, new NpcWalk.Bound(leaf.Sign, chair.X, chair.Y), doorstep, walls)
                is not { } walk)
        {
            return false;
        }

        _barAfoot.Add(new Walker
        {
            Walk = walk, Table = move.TableIndex, For = Errand.Arriving, Who = who.Id,
        });
        StateHasChanged();
        return true;
    }

    /// <summary>#731 · One of the bar's regulars by the rota's own id, off the rota's UNTOUCHED answer — the
    /// seat they were dealt this watch, whatever this evening has since done to the room.</summary>
    private HavenInterior.SeatedRegular? TheRegular(string bodyId, string id)
    {
        foreach (HavenInterior.SeatedRegular r in HavenInterior.ResolveRegulars(bodyId, _dockVisitSimTime))
        {
            if (string.Equals(r.Id, id, StringComparison.Ordinal))
            {
                return r;
            }
        }

        return null;
    }
}
