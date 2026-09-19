using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// #1062 slice 2 · LOSING YOUR OWN TAIL — the mirror of Map.ObservationWalk.cs, in the same room, on the
// same feet, and with none of the same machinery pointed the same way.
//
// Owner, 2026-09-01, verbatim: "following one of the customers covertly without them noticing us would be
// classic spy / detective stuff :-D … or trying to lose a tail our selves :-D"
//
// WHAT IS HERE. A man who comes into the bar after the captain, takes a place he can see him from, and orders
// nothing. The two ways a captain can find that out — a chair with the door in front of it, and the same coat
// through two doorways — and the one way to be rid of him, which is to spend as long out of his sight as it
// took to notice him.
//
// #1229 · AND "A PLACE" IS TWO PLACES, because the brief said so from the start and only half of it shipped:
// "enters the room after the captain, keeps a distance band, TAKES A SEAT/STANDING SPOT WITH A SIGHTLINE TO
// HIM, orders nothing." In a room off the ring he takes a POST — inside it, against the room's own stone,
// with a line, nearest the doorway he came in by, never the counter and never a chair. Out on the ring he
// keeps the BAND, unchanged. The room chooses, by its own published walls, and the reason it has to is
// measured: a bar reaches about twenty-three deck units from its doorway and the band's far edge is thirty,
// so a man told to hold a band in one is a man the stone pushes out through the door — which is exactly what
// #1231 watched happen, three seconds at a time.
//
// WHAT IS DELIBERATELY NOT. No pathfinder (OnFoot, the one planner). No sightline (FootTail.InPlainSight →
// PatrolBeat.EyesOn → SurfaceCollision.HasLineOfSight, the one oracle, which is also the one slice 1 uses).
// NO DICE AT ALL — every question in this file is arithmetic on a range, a clock and a count of doorways,
// which is #1062's own law for this half. No new save field: whether anybody is behind you is read off
// #715's folder, which already rides the vault.
//
// AND NOTHING IS ANNOUNCED UNTIL IT IS EARNED. Until the captain has noticed him, this file says nothing at
// all: no pulse, no card, no badge, no book entry. The only thing in the game that gives him away is the
// figure on the floor, which is where a gumshoe's evidence is supposed to be.
public partial class Map
{
    // ── THE FACTS, AND NOT ONE OF THEM RIDES THE SAVE ────────────────────────────────────────────────────
    //
    // A visit's own state, exactly like the bar's feet and slice 1's tail: a different berth is a different
    // room, and a man carried across a casting-off would be somebody standing in a station he was never in.
    // WHETHER he is here at all is not kept here — it is read off the outfit's folder every frame, so there
    // is nothing to persist and nothing to migrate.

    /// <summary>#1062 · Which berth this man belongs to. Null is a room that has never had one.</summary>
    private string? _coatBerth;

    /// <summary>#1062 · Whether this visit has already put him on the floor, so a room that refused the walk
    /// is not asked again sixty times a second and a man who has come in once does not come in again.</summary>
    private bool _coatDealt;

    /// <summary>#1062 · Has the captain worked out that he is being followed? One-way for the visit: a thing
    /// you have noticed is not un-noticed by arithmetic on the next frame. It is the latch every player-facing
    /// word in this file is gated on.</summary>
    private bool _coatSeen;

    /// <summary>#1062 · Seconds of having him in front of a chair that faces the door
    /// (<see cref="TheTailBehindYou.NoticedFromTheChair"/>). Reset the moment any clause of the sit stops
    /// holding — the reading is about sitting there, not about having once sat there.</summary>
    private double _coatExposure;

    /// <summary>#1062 · …and the mirror clock: seconds he has had nothing to look at. The one that loses
    /// him.</summary>
    private double _coatBlind;

    /// <summary>#1229 · …and the third: seconds the captain has been in a DIFFERENT ROOM from him. A man
    /// posted inside a room does not leave it on the frame his subject does — he gives it one look
    /// (<see cref="TheTailBehindYou.SecondsBeforeHeFollowsYouOut"/>) and then comes out through the same
    /// doorway, which is what makes the two-door tell a SEQUENCE the captain reads rather than an accident of
    /// where the man happened to be planted beforehand. Zeroed the moment they are in one room again.</summary>
    private double _coatARoomBehind;

    /// <summary>#1229 · Is he on a POST — a standing place against the room's own stone — rather than keeping
    /// a band? Written on the frame he is SENT to one, so it is a fact about the man and not a re-derivation
    /// of the room he happens to be in this instant. A post is kept until the LINE breaks; a band is kept
    /// until the captain walks out of it. Two behaviours, one field to tell them apart.</summary>
    private bool _coatPosted;

    /// <summary>#1229 · Which doorway he last came through, by its index in the plan — the way OUT, from where
    /// he is standing. It is what a band is measured against (he keeps himself between the captain and it) and
    /// what a post is measured to (nearest it). Null until he has been in one, which is the bar's own
    /// threshold by default. A visit's state, like everything else in this file.</summary>
    private int? _coatCameInBy;

    /// <summary>#1062 · Which of the deck's own doorways he has been seen coming through, by their index in
    /// the plan. A SET, because the tell is two DISTINCT doors and a man loitering in one of them for a whole
    /// watch is a man in a doorway.</summary>
    private readonly HashSet<int> _coatDoors = [];

    /// <summary>#1062 · Whether he has given up and is on his way off the floor. Written once; the walk out is
    /// an ordinary route and ends the ordinary way.</summary>
    private bool _coatLost;

    /// <summary>#1062 · <b>WHERE HE LAST HAD THE CAPTAIN.</b> The only thing he knows when the stone comes
    /// between them, and the only place a man who has lost you has any reason to walk to. NaN is a man who
    /// has not had you yet.</summary>
    private double _coatLastX = double.NaN;

    /// <summary>#1062 · <inheritdoc cref="_coatLastX"/></summary>
    private double _coatLastY = double.NaN;

    /// <summary>#1062 · Which ports this VISIT burned, so a burn is never told to the captain in the same
    /// breath as the act that made it. The burn itself is durable (it is a tag in the register that already
    /// rides the vault); this is only "not yet", and it is forgotten on casting off — which is exactly what
    /// <i>the next time he comes back</i> means, and is why the durable half needs no visit stamp in it.</summary>
    private readonly HashSet<string> _coatBurnedThisVisit = new(StringComparer.Ordinal);

    /// <summary>#1062 QA · <c>?tailed=1</c> — put a man behind the captain at this berth whatever the folder
    /// says. Set in the cheat parse. Null is "ask the world", which is what a captain gets.</summary>
    private bool? _tailedCheat;

    // ── IS ANYBODY BEHIND YOU ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>THE TRIGGER, AND IT IS SOMEBODY ELSE'S BOOK.</b>
    ///
    /// <para>Read off #715's folder for whoever runs this berth (<see cref="IllegalHeat.HeatAtSite"/> — the
    /// one call every effect in this game asks that question with), at the band where an outfit stops
    /// treating a hull as paperwork and starts wanting a face. Plus slice 1's own failure: a captain who was
    /// CLOCKED following one of this bar's regulars has advertised what he does for a living, and the evening
    /// answers.</para>
    ///
    /// <para><b>The audit's correction, recorded.</b> #1062 offers #804's suspicion ladder as the other
    /// candidate, and ashore there is no such thing: that ladder is an escort count on a patrolled floor of
    /// an underground complex, it is spawned and stepped only from a surface excursion, and a berth has
    /// neither. What a berth does have is the folder — which is the better trigger anyway, because it is the
    /// only one of the three that is written down somewhere a captain can do something about.</para>
    /// </summary>
    private bool TheCoatIsBehindYou(string berth) =>
        _tailedCheat
        ?? TheTailBehindYou.Follows(IllegalHeat.HeatAtSite(_contacts, berth), _walkNoticed);

    // ── ONE FRAME ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · One frame of the man behind you. Called from <c>AdvanceBarWalkers</c> beside the room's own
    /// metabolism, and it does nothing at all at a berth whose outfit has nothing written down.
    ///
    /// <para>Three refusals before anything happens, in the order that costs least: nobody is owed a look at
    /// this captain; he has already been lost (once is once — a man who has been shaken does not come back
    /// this visit); the captain is not in the room yet, because the whole shape of the beat is that he comes
    /// in AFTER you.</para>
    /// </summary>
    private void AdvanceTheCoat(in HavenInterior.BarFloor bar)
    {
        if (_coatLost || !TheCoatIsBehindYou(bar.BodyId))
        {
            return;
        }

        if (_coatDealt || !InTheBar(in bar))
        {
            return;
        }

        if (_barAfoot.Count >= WalkerBand)
        {
            // A full band is NOT NOW rather than NO — the room's own leavers hold slots for a few seconds at
            // a time and then give them back. Marking him dealt here would let a busy instant cancel the
            // whole thing for the visit, which is slice 1's lesson read straight across.
            return;
        }

        // He comes in the way you came in — the room's own published doorway, never a coordinate typed into a
        // client file. #1229 · and he is dealt on the CONCOURSE SIDE of it (HavenInterior.TheDoorstepOutside
        // TheBar) and walks in, because HavenInterior.BarThreshold is where `?ashore=1` stands the CAPTAIN: a
        // man dealt there is dealt on the captain's feet, and a tail standing on your toes on the frame you
        // boot is not a tail. He comes in AFTER you, which is the whole shape of the beat, and now the code
        // says so as well as the docblock.
        (double outsideX, double outsideY) = HavenInterior.TheDoorstepOutsideTheBar;
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        var doorstep = new DeckReachability.Point(outsideX, outsideY);

        _coatDealt = true;

        if (WhereHeStands(in bar, walls) is not { } spot
            || OnFoot(TheTailBehindYou.Plate, new NpcWalk.Bound("", spot.X, spot.Y), doorstep, walls)
               is not { } walk)
        {
            return;   // the stone allows him nowhere to stand, or there is no route. Nobody comes in.
        }

        _barAfoot.Add(new Walker { Walk = walk, Table = -1, For = Errand.BehindYou, Who = "" });
        StateHasChanged();
    }

    /// <summary>#1062 · CASTING OFF IS THE ROOM FORGETTING, here as everywhere else on this deck — called
    /// from <c>ForgetTheBarsFeet</c>, the one place that knows a berth has changed. An exposure clock carried
    /// across a casting-off would be a captain half-way to noticing a man at a station he has left.</summary>
    private void ForgetTheCoat(string? berth)
    {
        if (_coatBerth == berth)
        {
            return;
        }

        _coatBerth = berth;
        _coatDealt = false;
        _coatSeen = false;
        _coatExposure = 0;
        _coatBlind = 0;
        _coatARoomBehind = 0;
        _coatPosted = false;
        _coatCameInBy = null;
        _coatLost = false;
        _coatLastX = double.NaN;
        _coatLastY = double.NaN;
        _coatDoors.Clear();
        _coatBurnedThisVisit.Clear();
    }

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

    // ── THE BURN ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1062 · Is there a man on this floor right now with the captain's back in front of him? The
    /// walker list is the truth about who is afoot, so it is counted rather than tracked — and a man who has
    /// been shaken is not on it, which is the whole of what shaking him buys.</summary>
    private bool TheCoatIsAfoot()
    {
        foreach (Walker w in _barAfoot)
        {
            if (w.For == Errand.BehindYou)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// #1062 · <b>THE PLACE IS BURNED, AND NOTHING IS SAID ABOUT IT.</b> Called from the ONE strike-off both
    /// of a berth's quiet verbs already share, so the favour across a table and the code bought at a desk
    /// cannot come to two views of what being watched costs.
    ///
    /// <para><b>It is DETERMINISTIC and it is SILENT.</b> No roll decides it — a man was standing behind the
    /// captain while he did a thing that says where he goes, and that is the whole condition. And not one
    /// word is raised on this frame: the captain gets what he came for, walks out, and finds out later. That
    /// is #761 read exactly as it is written, and it is also the only version of this beat that is any good —
    /// a warning at the moment of the act would turn the tail into a mechanic the player manages, and the
    /// entire feature is that he does not know.</para>
    ///
    /// <para><b>He has to be able to see it.</b> A captain who shook the man first, or who never let him onto
    /// the floor, does his business unwatched — which is the counter-play, and it is made of the same nine
    /// seconds of stone the rest of this file is made of.</para>
    /// </summary>
    private void TheyBurnThisPlaceIfSomebodyIsWatching(string portId)
    {
        if (_coatLost || !TheCoatIsAfoot())
        {
            return;
        }

        _roomsTurnedOver.Add(TheTailBehindYou.BurnTag(portId));
        _coatBurnedThisVisit.Add(portId);
        RequestVaultSave();
    }

    /// <summary>#1062 · <b>HAS SOMEBODY BEEN THROUGH THIS PLACE AHEAD OF THE CAPTAIN?</b> The ONE burn
    /// predicate, and the only reader the tag has.
    ///
    /// <para>Slice 2 shipped it with a single caller — the strike-off the fence's key and the bar favour
    /// already shared — so those two could not come to two views of what being watched costs. Slice 2c adds
    /// the third quiet verb at a port, the unlisted parcel's row (<c>ParcelOnOffer</c>), and adds it by
    /// asking THIS question rather than by minting a second predicate or a second tag: three verbs, one
    /// question, one register. The burn still needs no refusal of its own anywhere in the game, because
    /// every row that answers to it is drawn where it applies and absent where it does not.</para></summary>
    private bool ThisPlaceWasWalkedFirst(string portId) =>
        _roomsTurnedOver.Contains(TheTailBehindYou.BurnTag(portId));

    /// <summary>
    /// #1062 · <b>AND WHEN HE COMES BACK, IT IS TIDY.</b> One pulse and one line in the book, at the burned
    /// place, on the visit AFTER the one that burned it — and then the burn is spent.
    ///
    /// <para>Spending it here rather than counting watches is the smallest honest shape: the cost is one
    /// visit's worth of what that port deals, the telling and the spending are the same event, and there is
    /// no window a captain can miss. A burn that expired on a clock would be a consequence the player could
    /// fail to be told about, which is the one thing #761 forbids.</para>
    ///
    /// <para>The note goes through the one funnel under the PLACE, so it stacks on THREADS with the losing
    /// note from the same evening and the captain reads the two of them in the order they happened — which
    /// is the whole of the inference: <i>he was behind me, and then this place had been gone through</i>. The
    /// book never says that. It says what happened.</para>
    /// </summary>
    private void TheBurnIsToldHere(string portId)
    {
        if (_coatBurnedThisVisit.Contains(portId) || !ThisPlaceWasWalkedFirst(portId) || !_ashore)
        {
            return;
        }

        _roomsTurnedOver.Remove(TheTailBehindYou.BurnTag(portId));
        ShowPulseMessage(TheTailBehindYou.TheBurnLine, PulseRank.Beat);

        string place = DockedStationName();
        FileNoteAbout(
            TheTailBehindYou.BurnNote(place), TheTailBehindYou.Glyph, TheTailBehindYou.BurnSubjects(place));
        RequestVaultSave();
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
