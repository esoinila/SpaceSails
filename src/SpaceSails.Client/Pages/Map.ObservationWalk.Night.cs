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
}
