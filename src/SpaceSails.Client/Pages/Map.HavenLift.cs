using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1253 · <b>THE THREE CARS IN A STATION'S CONCOURSE — the press, the panel and the ride.</b>
///
/// <para>Owner, 2026-09-20: <i>"The main hall could have multiple elevators… good for tailing."</i></para>
///
/// <para>This is the berth twin of <c>Map.Surface.Hive.Ride</c>'s <c>RideTheLiftTo</c>, and it is a twin
/// rather than a caller for the reason the #1253 audit found first: <b>every shaft call underground takes
/// <c>in SurfaceLayout.Field</c> and the whole floor-change path is gated on <c>_surface is { } ex</c>, which
/// is NULL at a berth.</b> A haven has no excursion, no regolith envelope, no air, no bands, no gates and no
/// patrol — so the parts of the Hive's ride that are about a MOON cannot come along, and what is left is
/// three lines: set the floor, rebuild the deck, stand the captain where those doors open.</para>
///
/// <para>What IS shared is what a lift actually is to a player: the surface he looks at
/// (<c>LiftPanel.razor</c>) and the row it draws (<see cref="UndergroundComplex.LiftStop"/>). Which stops
/// exist is Core's (<see cref="HavenLevels.Panel"/>), so the panel a captain presses and every guard that
/// holds this floor to having a way home are reading one list.</para>
///
/// <para><b>Nothing is said.</b> No card, no pulse, no line. A captain presses a button, the doors close and
/// open, and he is somewhere else in the same building — which is what a lift is, and the one place this
/// feature would be tempted to explain itself is the place it must not.</para>
/// </summary>
public partial class Map
{
    /// <summary>#1253 · Which of the three cars the open panel belongs to — written by the press that opened
    /// it and read by the ride, so the doors that close and the doors that open are one machine. It is the
    /// whole of the tailing craft the owner asked for: a car is not "the lift", it is a PLACE, and the ride
    /// carries which one with it.</summary>
    private int _havenLiftCage;

    /// <summary>#1253 · Is the captain at a berth with floors, with the deck up? The one question this whole
    /// file is gated on, asked once so the press, the panel and the ride cannot disagree about whether there
    /// is a building here at all.</summary>
    private bool TheStationHasFloors =>
        _surface is null && _deckMode && _dockedHavenId is { } berth && HavenInterior.HasLowerLevel(berth);

    /// <summary>#1253 · …and the berth those floors belong to, or null. Null on an excursion (that building
    /// has its own cars), null off the deck, null at a station with one floor.</summary>
    private string? TheStationWithFloors =>
        TheStationHasFloors ? _dockedHavenId : null;

    /// <summary>#1253 · <b>IS THERE A CAR TO BE STANDING IN AT ALL?</b> The page's own answer for the
    /// surface that draws the panel, and it replaces <c>_surface is { } liftEx</c> in the markup — the same
    /// question, asked for a whole seven-field excursion object, in the one way that could only ever answer
    /// for a moon. A building under the regolith, or a station with floors.</summary>
    private bool ThereIsACarToBeStandingIn => _surface is not null || TheStationHasFloors;

    /// <summary>
    /// #1253 · <b>[E] AT A CAR.</b> WHICH car is the SPOT's answer and nothing else's — #801's lesson said
    /// about a berth: a press that threw the pressed console away and asked "where is the lift" would open
    /// one panel for three doors and set the captain down at whichever the arithmetic liked.
    /// </summary>
    private void HavenLiftInteract()
    {
        if (TheStationWithFloors is not { } berth
            || _deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
                { Kind: DeckPlan.ConsoleKind.HavenLift } at)
        {
            return;
        }

        _havenLiftCage = WhichCageStandsAt(berth, at.X, at.Y);
        _liftOutcome = null;
        _showLiftPanel = true;
        RendererInterop.PlayCue("board");
    }

    /// <summary>#1253 · Which of the ring's three cars is standing on this square — matched back against the
    /// room's own published list rather than carried on the console, because a console that had to remember
    /// its own index would be a second tally of a fact the geometry already owns. The nearest, so a plate
    /// that is ever nudged a tenth of a du still names its own car; zero when the station has none, which is
    /// unreachable through the gate above and is the honest answer rather than a throw.</summary>
    private static int WhichCageStandsAt(string berth, double x, double y)
    {
        IReadOnlyList<DeckReachability.Point> cars = HavenInterior.TheCagesAt(berth);
        int best = 0;
        double nearest = double.MaxValue;
        for (int i = 0; i < cars.Count; i++)
        {
            double dx = cars[i].X - x, dy = cars[i].Y - y;
            double d = (dx * dx) + (dy * dy);
            if (d < nearest)
            {
                (nearest, best) = (d, i);
            }
        }

        return best;
    }

    /// <summary>#1253 · What this car's panel offers, standing where the captain is standing. Core's, so the
    /// row the player presses and the row a guard walks are one object.</summary>
    private IReadOnlyList<UndergroundComplex.LiftStop> HavenLiftStops() =>
        TheStationHasFloors ? HavenLevels.Panel(_havenFloor) : [];

    /// <summary>
    /// #1253 · <b>THE BERTH TWIN OF <c>RideTheLiftTo</c>.</b> Set the floor, rebuild the deck under the
    /// captain's feet, and stand him at THIS car's landing on the floor he asked for.
    ///
    /// <para>The landing is the room's own (<see cref="HavenInterior.TheCageLandingAt"/>) and never an offset
    /// worked out here — which is the whole feature. Three cars land in three places, so a man who went down
    /// at the south-west door comes up at the south-west door, and a captain who guessed the north-east one
    /// arrives at an empty car and has lost him. Nothing tells him he guessed wrong.</para>
    ///
    /// <para>The walkers go with the floor, through <see cref="ForgetTheBarsFeet"/> and its (berth, level)
    /// key: bodies crossing the concourse are not on this floor, and a list carried down a lift shaft would
    /// draw somebody walking through a corridor they were never in — the same law a turned shift keeps
    /// underground and a casting-off keeps at a berth.</para>
    /// </summary>
    /// <returns>False when this berth has no such floor or no such car, so a caller can say so rather than
    /// teleporting the captain into a building that does not have a basement.</returns>
    private bool RideTheHavenLiftTo(int level, int cage)
    {
        if (TheStationWithFloors is not { } berth
            || !HavenLevels.IsALevel(level)
            || level == _havenFloor)
        {
            return false;
        }

        _havenFloor = level;
        _havenLiftCage = cage;
        RebuildDockedDeck();

        // …and the feet, on the way through. The key is (berth, level), so this is the same forgetting a
        // casting-off does and it happens for the same reason.
        ForgetTheBarsFeet(berth, level);

        if (HavenInterior.TheCageLandingAt(berth, cage) is { } landing)
        {
            StandCaptainAshoreAt(landing.X, landing.Y);
        }

        RefreshAshore();
        _deckPanX = _deckPanY = 0;   // a fresh floor: drop any drag-pan so the follow-cam is not offset
        RendererInterop.PlayCue("board");
        StateHasChanged();
        return true;
    }

    /// <summary>#1253 · The berth's own <c>StandCaptainAt</c>. Same net under it — the placement is nudged
    /// clear of stone by <see cref="SpawnNudge"/>, because a car that let a captain out inside a wall is a
    /// bug this project has already paid for once (#602, on the regolith) — and the rebuild it runs first is
    /// the DOCKED one, since there is no surface excursion here to rebuild.</summary>
    private void StandCaptainAshoreAt(double x, double y)
    {
        (_avatarX, _avatarY) = (x, y);
        RebuildDockedDeck();

        SpawnNudge.Result spot = SpawnNudge.Clear(x, y, DeckPlan.AvatarRadius, _deckPlan.CollisionField);
        if (spot is { Failed: false, Moved: true })
        {
            (_avatarX, _avatarY) = (spot.X, spot.Y);
        }
    }
}
