using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · WHERE THE MAN BEHIND YOU STANDS (#1062, #1229) — the spot a man keeping station takes, the doorway
/// he came in by, whether he could stand somewhere, whether two places are one room, and the two behaviours
/// (a distance band, or a post against the room's own stone) with the room choosing between them.
///
/// <para>Split out of <c>Map.TailBehindYou.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every fact about him stays in the opening file.</para>
/// </summary>
public partial class Map
{
    // ── WHERE A MAN KEEPING STATION STANDS ───────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>THE SPOT HE WANTS</b> — sounded against the room's own stone, in the room's own idiom, and
    /// never plotted.
    ///
    /// <para>The bearings are Core's published list (<see cref="TheTailBehindYou.TheSidesHeSounds"/>, behind
    /// the captain first) at the middle of his band; the first one the stone allows a body on, with a clear
    /// line back to the captain, is where he goes. This is <c>HavenInterior.BesideATop</c>'s shape exactly —
    /// published sides, published order, the stone decides — and it is a SOUNDING rather than a search, so
    /// two captains standing in the same place get the same man in the same corner.</para>
    ///
    /// <para>He never sounds a fixture. The counter is the one spot in this room where service happens, and
    /// the whole of the canon line about him is that he has not ordered.</para>
    /// </summary>
    private DeckReachability.Point? TheSpotBehindYou(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        foreach (double reach in TheTailBehindYou.TheRangesHeTries)
        {
            if (TheSpotBehindYouAt(reach, in bar, walls) is { } spot)
            {
                return spot;
            }
        }

        return null;
    }

    /// <summary>#1062 · …the sounding itself, at one of his two reaches.</summary>
    private DeckReachability.Point? TheSpotBehindYouAt(
        double reach, in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        foreach (double bearing in TheTailBehindYou.TheSidesHeSounds)
        {
            double x = _avatarX + (System.Math.Cos(bearing) * reach);
            double y = _avatarY + (System.Math.Sin(bearing) * reach);
            if (HeCouldStandAt(x, y, _avatarX, _avatarY, in bar, walls))
            {
                return new DeckReachability.Point(x, y);
            }
        }

        return null;
    }

    /// <summary>#1229 · <b>THE DOORWAY HE CAME IN BY</b> — the last one he was seen standing in, by the plan's
    /// own index, or the bar's published threshold before he has been in one. It is what "behind you" is
    /// measured from and what a post is measured to, so the two of them cannot disagree about which way is
    /// out.</summary>
    private (double X, double Y) TheDoorwayHeCameInBy(in HavenInterior.BarFloor bar)
    {
        DeckPlan.Door[] doors = _deckPlan.Doors;
        if (_coatCameInBy is { } leaf && leaf >= 0 && leaf < doors.Length)
        {
            return ((doors[leaf].X1 + doors[leaf].X2) / 2.0, (doors[leaf].Y1 + doors[leaf].Y2) / 2.0);
        }

        (double x, double y, _) = HavenInterior.BarThreshold;
        return (x, y);
    }

    /// <summary>
    /// #1229 · <b>CAN A MAN KEEPING STATION ACTUALLY STAND HERE?</b> The four clauses that were spread over
    /// two methods and one of which did not exist, in one place, so the band and the post are placed by one
    /// rule and cannot come to two opinions about what a standing place is.
    ///
    /// <list type="number">
    ///   <item><b>THE ROOM.</b> The captain's own side of the bar's own south wall
    ///   (<see cref="HavenInterior.BarFloor.FloorY"/>, the one line that answers "is the captain in the bar"),
    ///   because <b>this is the clause that was missing and it is the whole of #1229</b>. Until now the only
    ///   place clause in the sounding was the gangway one below, and nothing asked whether the spot was in the
    ///   room the captain was standing in — so at Selene Gate the first bearing the stone allowed was due
    ///   south, out through the one doorway and nineteen units down the concourse. The wall closed three
    ///   seconds later and he gave up, silently, having never been in the room at all.</item>
    ///   <item><b>THE GANGWAY.</b> <see cref="StationFloorY"/> is the page's own line between a berth and the
    ///   umbilical, and it is the one clause here about WHO he is rather than about the stone. A man paid to
    ///   put a face to a hull does not follow the captain down his own gangway and stand in his airlock: that
    ///   is not a tail any more, it is a boarding. It is also what the audit left this feature in place of the
    ///   lift #1062 sketched — ashore this game has no lift, no interlock and no door that blocks anything, so
    ///   walking aboard your own ship is the timed close and it is made of geography.</item>
    ///   <item><b>THE STONE</b>, and <b>the one look over it</b> — the same oracle every other question about
    ///   what can be seen from a spot on a deck goes through.</item>
    ///   <item><b>AND HE HAS NOT ORDERED.</b> Never the counter and never a chair
    ///   (<see cref="HeWouldBeAPatronAt"/>), which the canon line says and the code now asks.</item>
    /// </list>
    /// </summary>
    private bool HeCouldStandAt(
        double x, double y, double towardsX, double towardsY,
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        TheSameRoom(x, y, towardsX, towardsY, in bar)
        && y > StationFloorY
        && !SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, walls)
        && !HeWouldBeAPatronAt(x, y, in bar)
        && SurfaceCollision.HasLineOfSight(towardsX, towardsY, x, y, walls);

    /// <summary>#1229 · Are these two places in one ROOM? The bar's own south wall is the line, which is the
    /// same line <see cref="InTheBar"/> is — so a room that moves moves for the man behind the captain at the
    /// same moment it moves for the captain.
    ///
    /// <para>It takes the other place rather than reading the captain's, because a BLIND man posts himself
    /// against where he LAST had him and not against where the captain has since got to. A room test that
    /// quietly read the live position would be the tracker this file refuses to build.</para>
    ///
    /// <para><b>Two walls, because a berth has two rooms off its ring</b> — the bar, off the hall's north
    /// edge, and #1199's observation walk, off its west one. Both are published by the haven itself
    /// (<see cref="HavenInterior.BarFloor.FloorY"/> and
    /// <see cref="HavenInterior.InTheObservationWalk"/>) and neither is measured a second time here. The walk
    /// matters as much as the bar does: it is a room with ONE way in, so a captain who steps into it has put
    /// a doorway between the two of them, and the doorway is the whole of the second tell.</para></summary>
    private bool TheSameRoom(
        double x, double y, double asX, double asY, in HavenInterior.BarFloor bar) =>
        (y > bar.FloorY) == (asY > bar.FloorY)
        && HavenInterior.InTheObservationWalk(bar.BodyId, x, y)
           == HavenInterior.InTheObservationWalk(bar.BodyId, asX, asY);

    /// <summary>#1229 · <b>HE HAS NOT ORDERED.</b> The counter is the one spot in this room where service
    /// happens and a top is where a patron sits; a man standing at either is a customer, and the whole of
    /// what the canon line says about him is that he is not one. The reach is
    /// <see cref="DeckPlan.InteractRadius"/> — the game's own statement of being AT a thing — so a chair
    /// beside a top (one body off its centre) is inside it by construction.
    ///
    /// <para>#1062 shipped this as an absence: <i>"the code never so much as asks the room where its fixtures
    /// are"</i>. That was true of a man who could only ever be nineteen units behind you. A POST is against
    /// the room's own stone, and a bar's counter is against the room's own stone, so the question now has to
    /// be asked out loud.</para></summary>
    private static bool HeWouldBeAPatronAt(double x, double y, in HavenInterior.BarFloor bar)
    {
        foreach (DeckReachability.Point service in bar.Fixtures)
        {
            double fx = service.X - x, fy = service.Y - y;
            if ((fx * fx) + (fy * fy) <= DeckPlan.InteractRadius * DeckPlan.InteractRadius)
            {
                return true;
            }
        }

        foreach (DeckReachability.Point top in bar.Tops)
        {
            double tx = top.X - x, ty = top.Y - y;
            if ((tx * tx) + (ty * ty) <= DeckPlan.InteractRadius * DeckPlan.InteractRadius)
            {
                return true;
            }
        }

        // #1199 (2026-09-18) · …AND THE OBSERVATION WALK'S CAFETERIA COUNTS, though it is emphatically not
        // the bar. The gallery grew two steel tables and two coin machines, and a man standing at any of them
        // is a man buying something — which is the one thing the canon line says he never does. Asked of the
        // haven's own published lists rather than by adding them to BarFloor.Tops, because that list is what
        // the ROOM'S OWN WALKERS cross the floor to, and a regular sent out to the end of the walk for a
        // sit-down would end the feature the gallery exists to carry.
        //
        // It also keeps the stakeout seats the CAPTAIN'S. Owner, 2026-09-18: "the tables at the hat would be
        // good stakeout positions" — a chair with a line to the way in is worth taking precisely because the
        // man who is following you cannot take it first.
        foreach (DeckReachability.Point top in HavenInterior.GalleryTops(bar.BodyId))
        {
            double tx = top.X - x, ty = top.Y - y;
            if ((tx * tx) + (ty * ty) <= DeckPlan.InteractRadius * DeckPlan.InteractRadius)
            {
                return true;
            }
        }

        foreach (DeckReachability.Point machine in HavenInterior.TheVendorsAt(bar.BodyId))
        {
            double mx = machine.X - x, my = machine.Y - y;
            if ((mx * mx) + (my * my) <= DeckPlan.InteractRadius * DeckPlan.InteractRadius)
            {
                return true;
            }
        }

        return false;
    }

    // ── #1229 · THE TWO BEHAVIOURS, AND THE ROOM CHOOSES BETWEEN THEM ────────────────────────────────────

    /// <summary>
    /// #1229 · <b>WHERE HE GOES — a POST or his BAND, and the stone decides which.</b>
    ///
    /// <para>#1062's brief named both from the start — <i>"enters the room after the captain, keeps a
    /// distance band, takes a seat/standing spot with a sightline to him, orders nothing"</i> — and only the
    /// band shipped. A 9–30 du band does not fit inside a station bar, so the sounding's first answer was
    /// always outside the room and the man lost a captain he had never been in with.</para>
    ///
    /// <para>So the room is ASKED, by sounding it, and never named: if the reach he keeps has a standing
    /// place in this room he keeps his band, which is a concourse; if it has none, the room is smaller than
    /// his band and he takes a post, which is a bar. One measurement, deterministic, on the room's own
    /// stone — so a room redrawn tomorrow chooses again with no edit here.</para>
    /// </summary>
    private DeckReachability.Point? WhereHeStands(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        _coatPosted = !ThisRoomCanHoldHisBand(in bar, walls);
        return _coatPosted
            ? ThePostInThisRoom(_avatarX, _avatarY, in bar, walls)
            : TheSpotBehindYou(in bar, walls);
    }

    /// <summary>
    /// #1229 · <b>CAN THE ROOM THE CAPTAIN IS STANDING IN HOLD A BAND AT ALL?</b>
    ///
    /// <para>A berth has a RING and it has two rooms off it: the bar, off the hall's north edge, and #1199's
    /// observation walk, off its west one. Each is one room with ONE doorway, and each is <b>smaller than his
    /// band</b> — measured, not assumed: sound <see cref="TheTailBehindYou.TheReachHeKeeps"/> across the
    /// published sides from a captain sitting at any of the bar's seven tops and not one of the answers is
    /// inside the room. That is #1231's finding in one sentence, and
    /// <c>TheManTakesAPostTests.TheBarIsSmallerThanHisBandAtEveryHavenInSolJson</c> is where it is pinned, at
    /// every haven, so this predicate cannot drift away from the geometry that justifies it.</para>
    ///
    /// <para>The ring is not: it is the hall plus its welded wings plus the gangway, and the sounding finds
    /// his reach on it. So OUT THERE HE KEEPS HIS BAND, unchanged, and in here he takes a POST.</para>
    ///
    /// <para><b>Why the ROOM and not the instant.</b> A sounding taken from wherever the captain is standing
    /// this frame answers differently two paces apart — a captain on the bar's own threshold has the whole
    /// depth of the room in front of him and one at a top has not — and a man who changed his mind about what
    /// he was doing every time his subject crossed a floor would not be keeping station, he would be
    /// fidgeting. The room is the unit the behaviour belongs to, and the room's own published walls are what
    /// say which one the captain is in.</para>
    /// </summary>
    private bool ThisRoomCanHoldHisBand(
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        !InTheBar(in bar)
        && !HavenInterior.InTheObservationWalk(bar.BodyId, _avatarX, _avatarY);

    /// <summary>
    /// #1229 · <b>THE POST</b> — a standing place INSIDE the room, against the room's own stone, with a line
    /// to the captain, <b>nearest the doorway he came in by</b>.
    ///
    /// <para>Sounded rather than searched, and sounded on the room's OWN WALLS: Core cuts each wall into
    /// body-wide slices and offers the middle of each one body clear of the stone, on the captain's side
    /// (<see cref="TheTailBehindYou.PostsAlong"/>); this side keeps the ones the whole room allows
    /// (<see cref="HeCouldStandAt"/>) and takes the one nearest the doorway. No dice, no pathfinder, no new
    /// geometry — the walls are the deck plan's own <c>CollisionSegments</c>, in the order the plan holds
    /// them, so the same room gives the same man the same corner for ever.</para>
    ///
    /// <para><b>Nearest the DOORWAY and not nearest the captain</b>, which is the beat: a man who has come in
    /// after you and stopped just inside the door is a man who has not committed to being in the room. The
    /// chair reading then says exactly what it was written to say — <i>you sit facing the door, and he is the
    /// man by the door who has not ordered</i>.</para>
    /// </summary>
    /// <param name="towardsX">Whom the post must have a line to: the captain, or — when the man is blind —
    /// the last place he had him, because a man who cannot see you cannot post himself on you.</param>
    /// <param name="towardsY">As above.</param>
    private DeckReachability.Point? ThePostInThisRoom(
        double towardsX, double towardsY,
        in HavenInterior.BarFloor bar, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        (double doorX, double doorY) = TheDoorwayHeCameInBy(in bar);
        DeckReachability.Point? post = null;
        double nearestToTheDoor = double.MaxValue;

        foreach (SurfaceCollision.Segment wall in _deckPlan.CollisionSegments)
        {
            foreach ((double x, double y) in TheTailBehindYou.PostsAlong(
                         wall.X1, wall.Y1, wall.X2, wall.Y2, DeckPlan.AvatarRadius, towardsX, towardsY))
            {
                if (!HeCouldStandAt(x, y, towardsX, towardsY, in bar, walls))
                {
                    continue;
                }

                double dx = x - doorX, dy = y - doorY;
                double toTheDoor = (dx * dx) + (dy * dy);
                if (toTheDoor < nearestToTheDoor)
                {
                    nearestToTheDoor = toTheDoor;
                    post = new DeckReachability.Point(x, y);
                }
            }
        }

        return post;
    }
}
