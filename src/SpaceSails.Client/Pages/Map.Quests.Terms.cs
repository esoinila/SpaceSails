using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Quests — #959, the four plain lines every job wears. The bridge between the live
// sim (positions, bodies, the purse) and the pure text layer in Core's JobTerms.
//
// Owner, 2026-08-18, on a ledger row: "What is this job... should I destroy a ship or rob a place? We
// should make sure these missions all tell clearly what it takes to complete them. We need to know
// before we decide if we accept or not." And on the offer card: "The offer where the price is listed is
// not very clear about what should be done and how long we must fly to do it."
//
// Nothing here writes a sentence — Core does that. Everything here MEASURES: what the target is, where
// it is, how far away it is right now, and which body both ends of the trip actually go round. That last
// one is the whole trick of the effort line, and it is why there is no cruise-speed constant anywhere:
// the time comes out of a Hohmann half-orbit (JobEffort.LaneSeconds → TransferMath.Hohmann) about the
// SHARED PRIMARY, so a Mars-orbit depot reads in days and a Saturn parcel reads in years, both from the
// same three lines of arithmetic.
public partial class Map
{
    /// <summary>The four plain lines for a job — offered or in hand, the SAME call, because the owner's
    /// ask was that the card say the same thing before and after accepting.</summary>
    private IReadOnlyList<string> JobPlainBlock(Quest q) => JobTerms.PlainBlock(JobFactsFor(q));

    /// <summary>Measure this job against the live world. Every number below is asked of the sim; the only
    /// literals are names and natures, which are facts about what a thing IS, not about where it is.</summary>
    private JobFacts JobFactsFor(Quest q)
    {
        ContractKind kind = ToContractKind(q.Kind);

        // ── Who or what the job points at, and where it sits ────────────────────────────────────────
        // Three shapes: a live NPC hull (hunt), a body (everything that flies somewhere), or nothing at
        // all (intel across the table, a hatch on this station).
        string targetName = q.TargetCallsign;
        JobTargetNature nature;
        string? where = null;
        Vector2d? targetPos = null;
        string? targetAnchorId = null;   // the body whose system the target lives in

        switch (q.Kind)
        {
            case QuestKind.Hunt:
            {
                NpcState? prey = FindNpc(q.TargetShipId);
                // THE MARS DEPOT CASE. A depot is an NpcShip with DepotBodyId set: a trading hull parked
                // in a planet's orbit. It is a ship by every rule the sim knows and a PLACE by every
                // instinct a reader has — which is exactly the confusion the owner reported, and the one
                // sentence the card was missing.
                bool depot = prey?.Ship.DepotBodyId is not null;
                nature = depot ? JobTargetNature.ParkedHull : JobTargetNature.Runner;
                targetAnchorId = depot ? prey!.Ship.DepotBodyId : prey?.Ship.DestinationId;
                where = depot ? $"{BodyName(prey!.Ship.DepotBodyId!)} orbit" : null;
                targetPos = prey?.State.Position;
                break;
            }
            case QuestKind.CargoRun:
            case QuestKind.Favor:
            {
                CelestialBody? dest = BodyById(q.DestBodyId);
                nature = JobTargetNature.Haven;
                targetName = dest is not null ? dest.Name : q.TargetCallsign;
                targetAnchorId = dest?.Id;
                where = dest is not null ? BodyAddress(dest.Id) : null;
                targetPos = dest is not null ? _ephemeris?.Position(dest.Id, SimTime) : null;
                break;
            }
            case QuestKind.Fetch:
            {
                CelestialBody? wreck = BodyById(q.SourceBodyId);
                nature = JobTargetNature.Wreck;
                targetName = wreck?.Name ?? "the derelict roadster";
                targetAnchorId = wreck?.Id;
                where = wreck is not null ? BodyAddress(wreck.Id) : null;
                targetPos = wreck is not null ? _ephemeris?.Position(wreck.Id, SimTime) : null;
                break;
            }
            case QuestKind.FetchCache:
            {
                CelestialBody? ground = BodyById(q.SourceBodyId);
                nature = JobTargetNature.Ground;
                targetName = ground?.Name ?? q.TargetCallsign;
                targetAnchorId = ground?.Id;
                where = ground is not null ? BodyAddress(ground.Id) : null;
                targetPos = ground is not null ? _ephemeris?.Position(ground.Id, SimTime) : null;
                break;
            }
            case QuestKind.Crack:
                // A hatch on the station the captain is standing in. Core's effort line knows this kind
                // has no voyage in it and says "on foot" rather than "0 km".
                nature = JobTargetNature.Lockup;
                targetName = $"hatch {q.TargetShipId}";
                where = _dockedHavenId is { } here ? BodyAddress(here) : null;
                break;
            default:
                // Intel — a tip bought across the table. Core prints "no flying" for it.
                nature = JobTargetNature.Runner;
                break;
        }

        (double distance, double lane) = JobTripFrom(targetPos, targetAnchorId);

        return new JobFacts(
            Kind: kind,
            TargetName: targetName,
            Nature: nature,
            WhereName: where,
            DistanceMeters: distance,
            LaneSeconds: lane,
            Reward: q.Reward,
            PurseCredits: _credits);
    }

    /// <summary>How far the target is RIGHT NOW, and roughly how long the trip runs by the lanes.
    /// A target we cannot place hands back NaN and 0, and the effort line drops the halves it has no
    /// number for — never a plausible-looking guess.</summary>
    private (double DistanceMeters, double LaneSeconds) JobTripFrom(Vector2d? targetPos, string? targetAnchorId)
    {
        if (targetPos is not { } tp || _ephemeris is null)
        {
            return (double.NaN, 0.0);
        }

        double distance = (tp - _ship.Position).Length;

        // THE FRAME THE TRIP IS READ IN. The plotting table's own law (#926): a trip is best read in the
        // frame of the body BOTH ends go round. Docked at a Mars haven and hunting a Mars depot, that is
        // MARS and the answer is days; Earth to a Saturn berth, it is the Sun and the answer is years.
        // Reading a Mars-local hop against the Sun would return half a Martian year, which is the kind of
        // sentence that disagrees with the sim.
        CelestialBody? primary = SharedPrimaryFor(_dockedHavenId ?? _orbitedBodyId, targetAnchorId);
        if (primary is null || primary.Mu <= 0)
        {
            return (distance, 0.0);
        }

        Vector2d origin = _ephemeris.Position(primary.Id, SimTime);
        double lane = JobEffort.LaneSeconds(primary.Mu, (_ship.Position - origin).Length, (tp - origin).Length);
        return (distance, lane);
    }

    /// <summary>The body both ends of the trip go round. The choice lives in Core
    /// (<see cref="JobEffort.SharedPrimary"/>) rather than here, because it is the one piece of this file
    /// that can be silently WRONG rather than merely absent — a Mars-local hop read against the Sun
    /// returns half a Martian year — and Core is where it can be unit-tested against the shipped
    /// scenario instead of only being looked at.</summary>
    private CelestialBody? SharedPrimaryFor(string? shipAnchorId, string? targetAnchorId) =>
        _ephemeris is null ? null : JobEffort.SharedPrimary(_ephemeris, shipAnchorId, targetAnchorId);
}
