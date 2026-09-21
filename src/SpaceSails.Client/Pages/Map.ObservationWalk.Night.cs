using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1253 slice 2 · <b>HIS NIGHT — the route grew three legs and a floor.</b>
///
/// <para>Owner, 2026-09-20: <i>"could we add a basement level to the observation deck station, so the tailing
/// task could start from the basement cabin and end at the observation deck? <b>Otherwise the followed
/// distance is easily very short.</b> The main hall could have multiple elevators… good for tailing."</i></para>
///
/// <para>#1199's route was one leg: up from a chair and out over the drop, and the owner is right that it is
/// short — a captain who happens to be looking gets the whole thing. The route is five legs now, and three of
/// them are on a floor the captain has to <b>decide</b> to follow him onto, on a car he has to <b>guess</b>.
/// The shipped last leg is not touched by a byte: everything #1254 does at the rail, in the gallery and to
/// the book happens exactly as it did.</para>
///
/// <h3>THE ONE IDEA IN THIS FILE: he is a SCHEDULE, and sometimes he is also a body</h3>
///
/// <para>A walker is a thing on a deck, and there is only ever ONE deck — the floor the captain is standing
/// on. So while the captain is on his floor he is a body, walked by the same <c>NpcWalk</c> over the same
/// stone as every other body in this game; and while the captain is on the other floor he is a CLOCK, and his
/// leg takes exactly as long as a man walking it would take
/// (<see cref="TheTailsNight.LegSeconds"/>, off <see cref="NpcWalk.PaceDu"/>).</para>
///
/// <para><b>The pace is the same either way, and that is the whole of the honesty here.</b> A leg that ran
/// faster off-screen would be a man who beats a captain who followed him properly; a leg that ran slower
/// would hold him for a captain who guessed wrong. Either one is the world arranging itself around who
/// happens to be looking, which is the one thing a tail cannot survive.</para>
///
/// <para>When the captain steps onto his floor mid-leg he is put on it <b>where the clock says he is</b> —
/// along his own line, at the fraction of it that has gone by — and walked from there. Not at the start of
/// the leg, which would hand a captain who arrived late a man who had not moved; not at the end, which would
/// hand him one who had already finished.</para>
///
/// <h3>And nothing is said</h3>
///
/// <para>No card, no pulse, no line, on any of it. A man finishes his drink, goes downstairs, is behind a
/// door for a while, comes back up and goes out to look at the view. The only thing this feature ever raises
/// is the card at the blind end of an empty room, and that card is #1199's and is unchanged.</para>
/// </summary>
public partial class Map
{
    /// <summary>#1253 · Which leg of his night he is on. The order is the route's own order and the names are
    /// the places, so a reader can follow him through the file the way a captain follows him through the
    /// station.</summary>
    private enum HisNight
    {
        /// <summary>Still in his chair. The room's own hours have not let him up yet.</summary>
        NotYet,

        /// <summary>Across the bar and the concourse, to the car he takes down.</summary>
        ToTheCarDown,

        /// <summary>Along the service corridor to his own cabin door.</summary>
        ToHisCabin,

        /// <summary>Behind it. The leaf is shut and it does not open for a captain (#563).</summary>
        Inside,

        /// <summary>Out again, and along the corridor to a car back up — which may not be the one he came
        /// down on.</summary>
        ToTheCarUp,

        /// <summary>The hall, the tube, the gallery. <b>This is #1254's leg and it is untouched</b>; from the
        /// frame it begins, every rule about the notice band, the rail, the wait, the vanish and the card is
        /// the shipped one.</summary>
        ToTheWalk,
    }

    /// <summary>#1253 · Where he is in his night. Visit state, like the rest of the tail's: a different berth
    /// is a different evening, and <see cref="ForgetTheWalk"/> is the one place that knows.</summary>
    private HisNight _nightLeg = HisNight.NotYet;

    /// <summary>#1253 · The sim second the leg he is on began. What the clock counts from when nobody is
    /// watching him, and what a mid-leg placement reads to work out how far along he is.</summary>
    private double _nightLegSince;

    /// <summary>#1253 · How long this leg takes a man to walk, in sim seconds — measured off the leg's own
    /// two ends when it is begun, so the clock and the legs are one distance rather than two.</summary>
    private double _nightLegSeconds;

    /// <summary>#1253 · Which floor the leg he is on happens on.</summary>
    private int _nightLegFloor = HavenLevels.Concourse;

    /// <summary>#1253 · Is he on the floor as a BODY right now? Read rather than inferred from the walker
    /// list, because the list is mutated under the sim and an absence from it means three different things
    /// (he has not been put on it, he has arrived, he has gone behind a door).</summary>
    private bool _nightAfoot;

    /// <summary>#1253 · Where this leg started and where it ends — kept so a captain who arrives mid-leg can
    /// be shown the man at the point on that line the clock has reached.</summary>
    private (double X, double Y) _nightLegFrom;

    private (double X, double Y) _nightLegTo;

    /// <summary>#1253 · Reset with the rest of the tail's visit state. Called from <c>ForgetTheWalk</c>,
    /// which is called from <c>ForgetTheBarsFeet</c>, which is the one place that knows a berth has
    /// changed.</summary>
    private void ForgetHisNight()
    {
        _nightLeg = HisNight.NotYet;
        _nightLegSince = 0;
        _nightLegSeconds = 0;
        _nightLegFloor = HavenLevels.Concourse;
        _nightAfoot = false;
        _nightLegFrom = default;
        _nightLegTo = default;
    }

    // ── ONE FRAME OF HIS NIGHT ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1253 · One frame of the whole route. Three questions in order: has the clock finished this leg, is
    /// the captain on the floor it happens on, and — if he is — is there a body on that floor yet.
    ///
    /// <para><b>The clock runs whether or not anybody is watching, and it runs at the same pace.</b> When the
    /// captain IS on his floor the body is the clock — the leg ends when his feet land — and when he is not,
    /// the leg ends when a man walking it would have arrived.</para>
    /// </summary>
    private void StepHisNight(in HavenInterior.BarFloor bar, string person)
    {
        if (_nightLeg == HisNight.NotYet)
        {
            return;   // the room's own hours have not let him up.
        }

        bool hisFloor = _havenFloor == _nightLegFloor;

        if (_nightLeg == HisNight.ToTheWalk)
        {
            StepHisLastLeg(in bar, person, hisFloor);
            return;
        }

        // ── #1287 · A MAN BEHIND A LEAF IS A CLOCK ON EVERY FLOOR, AND ON THE ONE HE IS BEHIND IT MOST ───
        //
        // What was played (QA, 2026-09-21, the row that says "Wait in the corridor"): the captain follows him
        // down, watches the leaf shut, stands there for nine and a half minutes, and NOTHING EVER COMES OUT.
        // Ride the wrong car and the corridor is empty for five minutes instead — on that car the captain
        // never comes within twenty du of him, so it is not a courtesy freeze.
        //
        // WHY. `Inside` is a leg on the SERVICE LEVEL, so a captain in the corridor made `hisFloor` true and
        // took the body branch below — which has no arm for a man who is not a body. `_nightAfoot` is false
        // (he went through the leaf), so every frame called `PutHimOnThisFloor`, which read the leg's two
        // ends — still the CABIN leg's, because the wait re-used them — and dealt him back onto the corridor
        // at the car's landing to walk to his own door a second time. Three minutes of standing behind a leaf
        // became twelve seconds of walking a corridor the captain had already watched him walk, and the leaf
        // never opened for anybody. The `_nightLegSeconds` clock was compared only in the branch below, which
        // that floor never reaches: the wait was ticked by exactly the one observer who could not see it.
        //
        // So the wait is ONE CLOCK and it runs off sim-time whoever is standing where. He is not a body on
        // any floor while the leaf is shut — there is no body behind a closed door — and when the clock runs
        // out he steps out onto the up-leg, which is placed by the ordinary road on the next frame and is
        // therefore the leaf opening in front of a captain who waited for it.
        if (_nightLeg == HisNight.Inside)
        {
            _nightAfoot = false;
            if (SimTime - _nightLegSince < _nightLegSeconds)
            {
                return;
            }

            BeginTheNextLeg(in bar, person);
            return;
        }

        // ── HE IS A BODY, AND THE BODY IS THE CLOCK ──────────────────────────────────────────────────────
        if (hisFloor)
        {
            if (!_nightAfoot)
            {
                PutHimOnThisFloor(in bar, person);
                return;
            }

            // The walker list is what says whether he is still walking. He comes off it on the frame his
            // route runs out (Map.BarWalkers.Walk's own ending), and that frame is the leg finishing.
            if (HeIsStillAfoot(person))
            {
                return;
            }

            _nightAfoot = false;
            BeginTheNextLeg(in bar, person);
            return;
        }

        // ── NOBODY IS WATCHING, SO HE IS A CLOCK ─────────────────────────────────────────────────────────
        if (_nightAfoot)
        {
            // The captain rode away mid-leg. Take the body off the floor it is no longer drawn on — the
            // feet list belongs to the floor and has already been cleared by the ride (ForgetTheBarsFeet),
            // so this only stops THIS file believing there is still a man on it.
            _nightAfoot = false;
        }

        if (SimTime - _nightLegSince < _nightLegSeconds)
        {
            return;
        }

        BeginTheNextLeg(in bar, person);
    }

    /// <summary>
    /// #1253 · <b>THE LAST LEG, WHICH IS #1254'S AND WHICH THIS FILE ONLY DECIDES WHETHER TO DRAW.</b>
    ///
    /// <para>While the captain is on the concourse there is a body on it and the shipped code owns him
    /// completely — the notice band, the rail, the wait, the turning back and the vanish are every one of
    /// them exactly as they were. This method's whole job on that floor is to make sure the body EXISTS,
    /// because a captain who followed him down, lost him, and came up a minute later must find the same man
    /// walking the same tube rather than a concourse the beat forgot to put anybody on.</para>
    ///
    /// <para><b>And while the captain is somewhere else, the ending is the shipped ending.</b> The rule is
    /// <i>he is off the floor on the first look nobody is watching him</i> — and a captain on another floor
    /// is nobody watching him, on every look there is. So when the clock says he has reached the rail, he is
    /// gone, and the second he went is the second the wait is counted from, exactly as it is when the captain
    /// blinks. The card is still there to be walked to, which is the whole of the beat: the captain rides up,
    /// walks the tube, and the walk is empty.</para>
    /// </summary>
    private void StepHisLastLeg(in HavenInterior.BarFloor bar, string person, bool hisFloor)
    {
        if (!double.IsNaN(_walkGoneSince) || _walkTurnedBack)
        {
            return;   // this beat has had its ending, one way or the other.
        }

        if (hisFloor)
        {
            if (!_nightAfoot)
            {
                PutHimOnThisFloor(in bar, person);
                return;
            }

            if (!HeIsStillAfoot(person))
            {
                _nightAfoot = false;   // the shipped code took him off the floor; it also said why.
            }

            return;
        }

        _nightAfoot = false;
        if (SimTime - _nightLegSince >= _nightLegSeconds)
        {
            _walkGoneSince = SimTime;
            _walkAtTheRailSince = double.NaN;
            StateHasChanged();
        }
    }

    /// <summary>#1253 · Is he still on his feet in the room? Asked of the room's own list by the id every
    /// walker of his carries, never by a slot index — the list is mutated under the sim, and #1062's own note
    /// about seeding on an index is the same lesson.</summary>
    private bool HeIsStillAfoot(string person)
    {
        foreach (Walker w in _barAfoot)
        {
            if (string.Equals(w.Who, person, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    // ── THE LEGS ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1253 · <b>HE FINISHES HIS DRINK.</b> The first leg, and the only one that takes a man out of a chair:
    /// the room is told he has gone (<c>_barLeft</c>) on the frame his legs start, which is the bar's own
    /// one-body-one-place law.
    ///
    /// <para>At a station with a walk and NO floor under it this is still the shipped route, unchanged: the
    /// night is one leg, it is <see cref="HisNight.ToTheWalk"/>, and everything below this line is skipped.
    /// That branch is not dead code kept for tidiness — it is what the route IS at any haven that grows an
    /// observation walk without growing a basement, and it is the shape every guard written before this lane
    /// is stated against.</para>
    /// </summary>
    private void BeginHisNight(in HavenInterior.BarFloor bar, string person)
    {
        if (_barAfoot.Count >= WalkerBand)
        {
            return;   // NOT NOW rather than NO, exactly as the shipped deal reads a full band.
        }

        int car = TheTailsNight.TheCarHeTakesDown(
            bar.BodyId, person, HavenInterior.TheCagesAt(bar.BodyId).Count);

        // At a haven with no floor under it — and at one whose cars will not answer — the night is the
        // SHIPPED walk, in one leg, out of the shipped method. Not dead code kept for tidiness: that is what
        // this route IS at any station that grows an observation walk without growing a basement, and it is
        // the shape every guard written before this lane is stated against.
        if (!HavenInterior.HasLowerLevel(bar.BodyId)
            || HavenInterior.TheCageLandingAt(bar.BodyId, car) is not { } doors
            || WhereHisChairIs(bar.BodyId, person) is not { } chair)
        {
            _nightLeg = HisNight.ToTheWalk;
            SendThemOutOntoTheWalk(in bar, person);
            return;
        }

        _walkDealt = true;
        _barLeft.Add(person);
        RebuildDockedDeck();
        OpenTheLeg(
            HisNight.ToTheCarDown, HavenLevels.Concourse, (chair.X, chair.Y), (doors.X, doors.Y));
        PutHimOnThisFloor(in bar, person);
    }

    /// <summary>
    /// #1253 · <b>WHAT HAPPENS WHEN A LEG RUNS OUT.</b> One switch, in the route's own order, and every arm
    /// of it is two facts: which floor the next leg is on, and which two points it runs between.
    ///
    /// <para><b>The two RIDES are not legs.</b> A car is travel and the captain cannot be in one with him, so
    /// arriving at a car's doors simply means the next leg begins on the other floor — which is what makes
    /// the guess worth making: a captain who took a different car is standing in a corridor somewhere else
    /// when this happens, and nothing tells him so.</para>
    /// </summary>
    private void BeginTheNextLeg(in HavenInterior.BarFloor bar, string person)
    {
        string berth = bar.BodyId;
        int cars = HavenInterior.TheCagesAt(berth).Count;

        switch (_nightLeg)
        {
            case HisNight.ToTheCarDown:
            {
                // He rides. The next leg is the corridor, and it begins where THIS car's doors open below —
                // the same square, one floor down, which is the building's own law about a car.
                int car = TheTailsNight.TheCarHeTakesDown(berth, person, cars);
                int cabin = TheTailsNight.HisCabin(berth, person, HavenLevels.Cabins);
                if (HavenInterior.TheCageLandingAt(berth, car) is not { } below
                    || HavenInterior.TheCabinDoorstepAt(berth, cabin) is not { } door)
                {
                    HisNightEndsHere();
                    return;
                }

                OpenTheLeg(
                    HisNight.ToHisCabin, HavenLevels.ServiceLevel, (below.X, below.Y), (door.X, door.Y));
                return;
            }

            case HisNight.ToHisCabin:
            {
                // He goes in. The leaf shuts behind him and it does not open for a captain — #563's ruling,
                // and the reason this wait is the escort's fiction read from the other side.
                _nightLeg = HisNight.Inside;
                _nightLegFloor = HavenLevels.ServiceLevel;
                _nightLegSince = SimTime;
                _nightLegSeconds = TheTailsNight.CabinWaitSeconds;
                _nightAfoot = false;

                // #1287 · …and the leg he is ON is the doorstep, at both ends. The wait used to inherit the
                // CABIN leg's two points — the car's landing and this leaf — so anything that asked where he
                // was got a LINE through a corridor he had already walked, and dealt a body onto it. A man
                // behind a door is at the door; a leg with one point has nowhere for a walker to be put.
                _nightLegFrom = _nightLegTo;
                StateHasChanged();
                return;
            }

            case HisNight.Inside:
            {
                int cabin = TheTailsNight.HisCabin(berth, person, HavenLevels.Cabins);
                int car = TheTailsNight.TheCarHeTakesUp(berth, person, cars);
                if (HavenInterior.TheCabinDoorstepAt(berth, cabin) is not { } door
                    || HavenInterior.TheCageLandingAt(berth, car) is not { } below)
                {
                    HisNightEndsHere();
                    return;
                }

                OpenTheLeg(
                    HisNight.ToTheCarUp, HavenLevels.ServiceLevel, (door.X, door.Y), (below.X, below.Y));
                return;
            }

            case HisNight.ToTheCarUp:
            {
                // He rides back up, and out of this file's hands: the last leg is #1254's, planned from the
                // doors he actually came out of rather than from the chair he left an hour ago.
                int car = TheTailsNight.TheCarHeTakesUp(berth, person, cars);
                if (HavenInterior.TheCageLandingAt(berth, car) is not { } above)
                {
                    HisNightEndsHere();
                    return;
                }

                if (HavenInterior.TheRailAt(berth) is not { } rail)
                {
                    HisNightEndsHere();
                    return;
                }

                OpenTheLeg(
                    HisNight.ToTheWalk, HavenLevels.Concourse, (above.X, above.Y), (rail.X, rail.Y));

                // …and if the captain is standing on that concourse, he sees the doors open. The leg is
                // planted through the SHIPPED method so the walker, the plate and the errand on this leg are
                // #1254's own — the only thing a basement changes about it is where the man is standing when
                // it begins.
                if (_havenFloor == HavenLevels.Concourse)
                {
                    SendThemOutOntoTheWalk(in bar, person, above);
                    _nightAfoot = HeIsStillAfoot(person);
                }

                return;
            }

            default:
                return;
        }
    }

    /// <summary>#1253 · The floor would not give him a way on. He is simply not out tonight — no card,
    /// nothing spent, and the walk is there the next time the captain ties up. The same refusal the shipped
    /// route gives a route that will not plot, said about a longer one.</summary>
    private void HisNightEndsHere()
    {
        _nightLeg = HisNight.ToTheWalk;
        _nightAfoot = false;
    }

    /// <summary>#1253 · Open a leg: what it is, which floor it happens on, and the two points it runs
    /// between — and from those two points, how long it takes. ONE place, so the clock and the walk cannot
    /// come to two views of how far it is.</summary>
    private void OpenTheLeg(HisNight leg, int floor, (double X, double Y) from, (double X, double Y) to)
    {
        _nightLeg = leg;
        _nightLegFloor = floor;
        _nightLegFrom = from;
        _nightLegTo = to;
        _nightLegSince = SimTime;
        _nightAfoot = false;

        double dx = to.X - from.X, dy = to.Y - from.Y;
        _nightLegSeconds = TheTailsNight.LegSeconds(System.Math.Sqrt((dx * dx) + (dy * dy)));
        StateHasChanged();
    }

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
