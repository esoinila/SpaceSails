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
// WHAT IS HERE. A man who comes into the bar after the captain, keeps a distance band, stands where he can
// see him, and orders nothing. The two ways a captain can find that out — a chair with the door in front of
// it, and the same coat through two doorways — and the one way to be rid of him, which is to spend as long
// out of his sight as it took to notice him.
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

    /// <summary>#1062 · Which of the deck's own doorways he has been seen coming through, by their index in
    /// the plan. A SET, because the tell is two DISTINCT doors and a man loitering in one of them for a whole
    /// watch is a man in a doorway.</summary>
    private readonly HashSet<int> _coatDoors = [];

    /// <summary>#1062 · Whether he has given up and is on his way off the floor. Written once; the walk out is
    /// an ordinary route and ends the ordinary way.</summary>
    private bool _coatLost;

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

        // He comes in the way you came in — the room's own published doorway (HavenInterior.BarThreshold,
        // the same spot ?ashore=1 stands the captain on), never a coordinate typed into a client file.
        (double doorX, double doorY, _) = HavenInterior.BarThreshold;
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        var doorstep = new DeckReachability.Point(doorX, doorY);

        _coatDealt = true;

        if (TheSpotBehindYou(walls) is not { } spot
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
        _coatLost = false;
        _coatDoors.Clear();
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
    private DeckReachability.Point? TheSpotBehindYou(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        foreach (double bearing in TheTailBehindYou.TheSidesHeSounds)
        {
            double x = _avatarX + (System.Math.Cos(bearing) * TheTailBehindYou.ComfortableDu);
            double y = _avatarY + (System.Math.Sin(bearing) * TheTailBehindYou.ComfortableDu);

            // ── AND HE STAYS ON THE STATION SIDE ─────────────────────────────────────────────────────────
            //
            // <see cref="StationFloorY"/> is the page's own line between a berth and the umbilical, and it is
            // the one clause here that is about WHO he is rather than about the stone. A man paid to put a
            // face to a hull does not follow the captain down his own gangway and stand in his airlock: that
            // is not a tail any more, it is a boarding, and #1062 is explicit that until some other issue
            // rules otherwise this figure is a mundane human being.
            //
            // It is also what the audit left the feature instead of the move #1062 sketched. The issue offers
            // "a lift timed at the close" and a door shut between you; ashore this game has no lift, no
            // interlock and no door that blocks anything at all. What it has is a gangway he will not walk
            // down — so walking aboard your own ship is the timed close, and it is made of geography.
            if (y <= StationFloorY
                || SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, walls)
                || !SurfaceCollision.HasLineOfSight(_avatarX, _avatarY, x, y, walls))
            {
                continue;
            }

            return new DeckReachability.Point(x, y);
        }

        return null;
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
    private bool StepTheCoat(Walker who, double dt, IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
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
        if (inSight && TheDoorwayHeIsIn(who.Walk.X, who.Walk.Y) is { } leaf && _coatDoors.Add(leaf)
            && TheTailBehindYou.TwoDoorsRunning(_coatDoors.Count))
        {
            told = YouHaveNoticedHim(TheTailBehindYou.TwoDoorsLine) || told;
        }

        // ── AND THE BAND HE KEEPS ────────────────────────────────────────────────────────────────────────
        //
        // He is re-plotted only when his own route has run out AND the captain has walked out of the band he
        // is paid to hold. A man who is already standing where he can see you does not shuffle every frame,
        // and a route re-plotted under a walking body is the one thing the planner must never be asked for.
        if (who.Walk.Afoot)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            return told || !who.Walk.Afoot;
        }

        who.Walk.LookTowards(_avatarX, _avatarY);
        if (inSight && TheTailBehindYou.HoldsHisBand(rangeDu))
        {
            return told;
        }

        if (TheSpotBehindYou(walls) is { } spot
            && OnFoot(TheTailBehindYou.Plate, new NpcWalk.Bound("", spot.X, spot.Y),
                      new DeckReachability.Point(who.Walk.X, who.Walk.Y), walls) is { } next)
        {
            _barAfoot[slot] = RebadgeTheCoat(who, next, Errand.BehindYou);
            return true;
        }

        return told;
    }

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
