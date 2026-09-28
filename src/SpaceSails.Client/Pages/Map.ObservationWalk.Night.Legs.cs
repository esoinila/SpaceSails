using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE LEGS OF HIS NIGHT (#1253) — beginning his night, the next leg, where it ends, and opening a
/// leg.
///
/// <para>Split out of <c>Map.ObservationWalk.Night.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public partial class Map
{
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
}
