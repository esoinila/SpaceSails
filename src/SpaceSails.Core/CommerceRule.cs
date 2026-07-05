namespace SpaceSails.Core;

/// <summary>
/// Orbital commerce (vision par. 10, PR-5): the owner's rule is that deal-making requires being
/// *in orbit* at the same body as the counterpart, or *course-matched* with a moving partner long
/// enough for automatic cargo drones to fly the transfer. This is the honest twin of the boarding
/// shuttle (<see cref="CaptureRule"/>) — same shape of math, but cooperative: drones don't need to
/// out-fly a fleeing target, so their envelope is looser and their transit faster.
/// </summary>
public static class CommerceRule
{
    /// <summary>Course-matched trading range. Same order of magnitude as the boarding shuttle's
    /// stand-off (<see cref="CaptureRule.CaptureRadiusMeters"/>) — drones fly the same distances
    /// shuttles do — but cooperative traffic doesn't need a tighter envelope to compensate for a
    /// target trying to shake pursuit, so it stays at the shuttle's own range rather than tighter.</summary>
    public const double CourseMatchDistanceMeters = CaptureRule.CaptureRadiusMeters;

    /// <summary>Course-matched trading speed limit: 2 km/s, well under the boarding shuttle's
    /// 5 km/s ceiling (<see cref="CaptureRule.MaxRelativeSpeed"/>). A boarding shuttle chases a
    /// non-cooperative target and forgives a sloppy pass; a cargo drone's cooperative partner
    /// slows down to meet it, so the game only asks for a already-decent course match rather than
    /// rewarding a drive-by.</summary>
    public const double CourseMatchMaxRelativeSpeed = 2000;

    /// <summary>Cooperative drones' base transfer time per cargo unit at a perfect, point-blank
    /// match. Boarding shuttles need <see cref="CaptureRule.BaseBoardingSeconds"/> once, total,
    /// regardless of cargo size (the shuttle brings back everything in one trip); a drone run
    /// instead scales with how much there is to ferry — many small round trips, not one raid.</summary>
    public const double DroneBaseSecondsPerUnit = 20;

    /// <summary>Relative speed that doubles drone transfer time. Higher than the boarding
    /// shuttle's <see cref="CaptureRule.RelativeSpeedPenalty"/> (1500) — a cooperative partner
    /// trims its own course to help the drones, so the same mismatch costs proportionally less.</summary>
    public const double DroneRelativeSpeedPenalty = 2500;

    /// <summary>Stand-off distance that doubles drone transfer time. Higher than the boarding
    /// shuttle's <see cref="CaptureRule.DistancePenalty"/> (2e8) for the same reason: drones fly a
    /// friendly corridor, not a raid through defended space, so distance costs less time.</summary>
    public const double DroneDistancePenalty = 5e8;

    // ---- M29: the shuttle tier (owner, from a live pass: "we are approaching it, so the
    // trade drones could go there and back during this slow pass"). A slow flyby at millions
    // of kilometres is a real trading opportunity — the shuttles fly the long corridor while
    // the geometry holds. Looser than the drone match in range, same 5 km/s ceiling as the
    // boarding shuttle, and the transfer time grows honestly with the distance flown. ----

    /// <summary>How far the trade shuttles will fly — covers a ~10 M km slow pass with margin.</summary>
    public const double ShuttleRangeMeters = 1.2e10;

    /// <summary>Same ceiling as the boarding shuttle: past this the pass is a drive-by.</summary>
    public const double ShuttleMaxRelativeSpeed = CaptureRule.MaxRelativeSpeed;

    /// <summary>Shuttle transfer time per cargo unit at point-blank — slower per unit than
    /// drones never are, because a crewed shuttle hauls bigger loads per trip.</summary>
    public const double ShuttleBaseSecondsPerUnit = 15;

    /// <summary>Relative speed that doubles shuttle transfer time.</summary>
    public const double ShuttleRelativeSpeedPenalty = 2500;

    /// <summary>Stand-off distance that doubles shuttle transfer time — the round trip is real.</summary>
    public const double ShuttleDistancePenalty = 5e9;

    /// <summary>How a deal would move, best tier first.</summary>
    public enum TradeMode
    {
        None,
        SameOrbit,
        DroneMatch,
        Shuttle,
    }

    /// <summary>
    /// Classifies the trading geometry: parked at the same body (the classic bus-stop deal),
    /// course-matched for cargo drones, inside shuttle range for the long corridor — or out
    /// of reach entirely.
    /// </summary>
    public static TradeMode Classify(
        ShipState player,
        Vector2d partnerPosition,
        Vector2d partnerVelocity,
        string? playerOrbitBodyId,
        string? partnerOrbitBodyId)
    {
        if (playerOrbitBodyId is not null && playerOrbitBodyId == partnerOrbitBodyId)
        {
            return TradeMode.SameOrbit;
        }

        double distance = (player.Position - partnerPosition).Length;
        double relativeSpeed = (player.Velocity - partnerVelocity).Length;
        if (distance <= CourseMatchDistanceMeters && relativeSpeed <= CourseMatchMaxRelativeSpeed)
        {
            return TradeMode.DroneMatch;
        }

        if (distance <= ShuttleRangeMeters && relativeSpeed <= ShuttleMaxRelativeSpeed)
        {
            return TradeMode.Shuttle;
        }

        return TradeMode.None;
    }

    /// <summary>
    /// Trading is allowed when either party's orbit body ids match (both parked at the same body —
    /// the classic bus-stop deal), when the two ships are course-matched for cooperative cargo
    /// drones, or (M29) when the partner sits inside shuttle range on a slow enough pass.
    /// </summary>
    public static bool CanTrade(
        ShipState player,
        Vector2d partnerPosition,
        Vector2d partnerVelocity,
        string? playerOrbitBodyId,
        string? partnerOrbitBodyId) =>
        Classify(player, partnerPosition, partnerVelocity, playerOrbitBodyId, partnerOrbitBodyId) != TradeMode.None;

    /// <summary>Transfer time for the given tier and geometry: drones for the close tiers,
    /// the shuttle formula (real round trips over the long corridor) for the shuttle tier.</summary>
    public static double TransferSeconds(TradeMode mode, double relativeSpeed, double distance, int units) =>
        mode == TradeMode.Shuttle
            ? ShuttleBaseSecondsPerUnit * Math.Max(1, units)
                * (1 + relativeSpeed / ShuttleRelativeSpeedPenalty)
                * (1 + distance / ShuttleDistancePenalty)
            : DroneTransferSeconds(relativeSpeed, distance, units);

    /// <summary>
    /// Sim seconds a cooperative drone transfer needs at this instant's geometry, for the given
    /// number of cargo units. Same shape as <see cref="CaptureRule.RequiredSecondsFor"/> — a tight,
    /// slow match is fast; distance and relative speed both grow the time — but drones are honest
    /// partners in a cooperative deal, not a shuttle running down a fleeing hauler, so the base
    /// rate and penalties are gentler (see the constants above for exactly how much).
    /// </summary>
    public static double DroneTransferSeconds(double relativeSpeed, double distance, int units)
    {
        int clampedUnits = Math.Max(1, units);
        return DroneBaseSecondsPerUnit * clampedUnits
            * (1 + relativeSpeed / DroneRelativeSpeedPenalty)
            * (1 + distance / DroneDistancePenalty);
    }

    /// <summary>Thin, read-only projection of a live NPC ship (depot or hauler) — the Core helper
    /// never needs the caller's private NPC wrapper type, only id/callsign/current physical state
    /// plus the depot flag (mirrors the tracking post's TrackingCandidate's role for its own
    /// station).</summary>
    public readonly record struct LocalShip(string Id, string Callsign, ShipState State, string? DepotBodyId);

    /// <summary>What kind of thing a <see cref="LocalContact"/> is, for icon/label purposes.</summary>
    public enum LocalContactKind
    {
        Depot,
        Station,
        Moon,
        Haven,
        Ship,
    }

    /// <summary>What a local contact offers — tags only; the actions themselves live where they
    /// already live (dock panel for Trade/Fence, the boarding-shuttle flow for Board).</summary>
    [Flags]
    public enum ActionKind
    {
        None = 0,
        Trade = 1,
        Fence = 2,
        Board = 4,
    }

    /// <summary>One thing "at" a body: a depot, a station/moon/haven orbiting it, or an NPC ship
    /// caught inside its Hill sphere.</summary>
    public readonly record struct LocalContact(
        string Id,
        string Name,
        LocalContactKind Kind,
        Vector2d Position,
        Vector2d Velocity,
        double DistanceMeters,
        ActionKind Actions);

    /// <summary>Stations and pirate havens carry no real gravity in scenario data (mu = 0 — see
    /// TrafficSchedule.GenerateDepots) so the Hill-sphere formula degenerates to zero for them.
    /// This fixed "dockyard" radius stands in: comfortably wider than a station's own depot orbit
    /// (~2e6 m, see GenerateDepots) so a parked ship still reads as "local" to it.</summary>
    private const double StationProximityRadiusMeters = 2e7;

    /// <summary>
    /// Everything logically "at" the given body right now: any depot whose DepotBodyId matches it,
    /// every station/moon/haven that orbits it as a child body, and any (non-depot) NPC ship caught
    /// inside its Hill sphere — the proximity affordances the nav screen wants (vision par. 10):
    /// "orbiting a planet, you should see what else orbits there."
    /// </summary>
    public static IReadOnlyList<LocalContact> ContactsAt(
        ICelestialEphemeris ephemeris,
        IReadOnlyList<LocalShip> ships,
        double simTime,
        string bodyId)
    {
        var contacts = new List<LocalContact>();

        CelestialBody? body = null;
        foreach (CelestialBody candidate in ephemeris.Bodies)
        {
            if (candidate.Id == bodyId)
            {
                body = candidate;
                break;
            }
        }

        if (body is null)
        {
            return contacts;
        }

        Vector2d bodyPosition = ephemeris.Position(bodyId, simTime);
        double hillRadius = HillRadiusFor(ephemeris, body);

        foreach (LocalShip ship in ships)
        {
            if (ship.DepotBodyId != bodyId)
            {
                continue;
            }

            ActionKind actions = ActionKind.Trade | (body.IsHaven ? ActionKind.Fence : ActionKind.None);
            contacts.Add(new LocalContact(
                ship.Id, ship.Callsign, LocalContactKind.Depot,
                ship.State.Position, ship.State.Velocity,
                (ship.State.Position - bodyPosition).Length, actions));
        }

        foreach (CelestialBody child in ephemeris.Bodies)
        {
            if (child.ParentId != bodyId)
            {
                continue;
            }

            Vector2d childPosition = ephemeris.Position(child.Id, simTime);
            // M29: children ride rails but they MOVE — a zero velocity made course-matching a
            // station mathematically impossible (rel speed read as the ship's full heliocentric
            // speed). Same finite difference the nav HUD uses everywhere.
            Vector2d childVelocity = (ephemeris.Position(child.Id, simTime + 1) - ephemeris.Position(child.Id, simTime - 1)) / 2;
            (LocalContactKind kind, ActionKind actions) = ClassifyChild(child);
            contacts.Add(new LocalContact(
                child.Id, child.Name, kind, childPosition, childVelocity,
                (childPosition - bodyPosition).Length, actions));
        }

        foreach (LocalShip ship in ships)
        {
            if (ship.DepotBodyId is not null)
            {
                continue; // depots are handled above, keyed to their own host body
            }

            double distance = (ship.State.Position - bodyPosition).Length;
            if (distance <= hillRadius)
            {
                contacts.Add(new LocalContact(
                    ship.Id, ship.Callsign, LocalContactKind.Ship,
                    ship.State.Position, ship.State.Velocity, distance, ActionKind.Board));
            }
        }

        return contacts;
    }

    /// <summary>
    /// M29: every tradable partner within shuttle range of the PLAYER, wherever it happens to
    /// be keyed — the "send the shuttles" opportunity list. Depots (with their host's fence
    /// flag) and rails-riding stations/havens; distance here is measured from the player, not
    /// a host body, because the shuttle corridor is what the desk is deciding about.
    /// </summary>
    public static IReadOnlyList<LocalContact> ContactsWithinShuttleRange(
        ICelestialEphemeris ephemeris,
        IReadOnlyList<LocalShip> ships,
        double simTime,
        ShipState player)
    {
        var contacts = new List<LocalContact>();

        foreach (LocalShip ship in ships)
        {
            if (ship.DepotBodyId is null)
            {
                continue; // free-flying ships trade by boarding, not by cooperative shuttle
            }

            double distance = (ship.State.Position - player.Position).Length;
            if (distance > ShuttleRangeMeters)
            {
                continue;
            }

            bool haven = false;
            foreach (CelestialBody body in ephemeris.Bodies)
            {
                if (body.Id == ship.DepotBodyId) { haven = body.IsHaven; break; }
            }

            contacts.Add(new LocalContact(
                ship.Id, ship.Callsign, LocalContactKind.Depot,
                ship.State.Position, ship.State.Velocity, distance,
                ActionKind.Trade | (haven ? ActionKind.Fence : ActionKind.None)));
        }

        foreach (CelestialBody child in ephemeris.Bodies)
        {
            if (child.ParentId is null)
            {
                continue;
            }

            (LocalContactKind kind, ActionKind actions) = ClassifyChild(child);
            if ((actions & ActionKind.Trade) == 0)
            {
                continue;
            }

            Vector2d position = ephemeris.Position(child.Id, simTime);
            double distance = (position - player.Position).Length;
            if (distance > ShuttleRangeMeters)
            {
                continue;
            }

            Vector2d velocity = (ephemeris.Position(child.Id, simTime + 1) - ephemeris.Position(child.Id, simTime - 1)) / 2;
            contacts.Add(new LocalContact(child.Id, child.Name, kind, position, velocity, distance, actions));
        }

        contacts.Sort((a, b) => a.DistanceMeters.CompareTo(b.DistanceMeters));
        return contacts;
    }

    private static (LocalContactKind Kind, ActionKind Actions) ClassifyChild(CelestialBody child)
    {
        if (child.IsHaven)
        {
            return (LocalContactKind.Haven, ActionKind.Trade | ActionKind.Fence);
        }

        return child.Kind switch
        {
            BodyKind.Station => (LocalContactKind.Station, ActionKind.Trade),
            BodyKind.Moon => (LocalContactKind.Moon, ActionKind.None),
            _ => (LocalContactKind.Moon, ActionKind.None),
        };
    }

    private static double HillRadiusFor(ICelestialEphemeris ephemeris, CelestialBody body)
    {
        if (body.ParentId is null)
        {
            return double.MaxValue; // the sun: "local space" here is the whole system
        }

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId)
            {
                parent = candidate;
                break;
            }
        }

        if (parent is null || body.Mu <= 0)
        {
            return StationProximityRadiusMeters;
        }

        return OrbitRule.HillRadius(body, parent.Mu);
    }
}
