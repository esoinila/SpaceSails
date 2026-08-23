using System.Globalization;

namespace SpaceSails.Core;

// #959 · WHAT DOES THIS JOB ACTUALLY TAKE.
//
// Owner, 2026-08-18, looking at a ledger row that read
//
//     Bring down Mars Depot / Hunt Mars Depot — hole her sail or board her / ▶ On the hook / 764 cr
//
// asked: "What is this job... should I destroy a ship or rob a place? We should make sure these
// missions all tell clearly what it takes to complete them. We need to know before we decide if we
// accept or not." And, on the reward pop-up the same evening: "The offer where the price is listed
// is not very clear about what should be done and how long we must fly to do it."
//
// He was right on both counts, and the "Mars Depot" case is the sharpest version of it: the prey a
// hunt picks CAN be a depot hull — a trading ship parked in a planet's orbit (NpcShip.DepotBodyId) —
// so a job whose title says "bring down" points at something that looks, sounds and trades like a
// place. Nothing on the card said whether the answer was guns or a boarding tube, and nothing said
// how far away it was.
//
// This is the pure text layer that fixes it. FOUR PLAIN LINES stand above the flavour prose on every
// surface a job appears on — the stranger's offer card, the bar rumour, the Captain's ledger row:
//
//     DESTROY — Mars Depot, in Mars orbit                     ← the VERB, from a fixed vocabulary
//     Takes: hole her sail OR board her — either ends it.     ← completion, in game terms
//       She is a hull parked in Mars orbit, not a runner.     ← and WHAT the target is
//     ≈ 3.60 M km · ~6 d by the lanes                         ← the honest effort
//     764 cr · small                                          ← the pay, sized against the purse
//
// Three laws this file is written under:
//
//   1. THE VERB SLOT HAS NO POETRY IN IT. "Bring down", "hole her sail", "prise it loose" are good
//      voice and they STAY — one line lower. The top line is one of five words, always, so a captain
//      who has learned the vocabulary never has to parse a sentence to know what he is agreeing to.
//
//   2. THE EFFORT LINE IS BORROWED FROM THE SIM, NEVER TYPED. Distance is measured off live
//      positions; the time is a Hohmann half-orbit (TransferMath.Hohmann) about the body BOTH ends
//      actually go round — the same arithmetic the plotting table teaches. There is no invented
//      "cruise speed" constant anywhere in here, because this repo's named bug class is a sentence
//      reporting one thing while the sim does another, and a made-up ETA is that bug with a smile on.
//
//   3. NO SHIP, NO EPHEMERIS, NO UI. Everything arrives as already-derived numbers, exactly like
//      MissionLegibility beside it, so all of it unit-tests without a browser.

/// <summary>
/// The fixed vocabulary of job verbs — the whole list, owner-facing, five words. A job's top line is
/// ALWAYS one of these, so "what am I being asked to do" is answered before any prose is read.
/// </summary>
public enum JobVerb
{
    /// <summary>Stop a hull for good: hole her sail, or take her by boarding. Either ends it.</summary>
    Destroy,

    /// <summary>Get inside something that is locked and carry what is in it back out.</summary>
    BoardAndRob,

    /// <summary>Carry a thing from here to a named berth and hand it over.</summary>
    Deliver,

    /// <summary>Ride along with somebody else's hull and see it arrive intact.</summary>
    /// <remarks>Reserved. No contract kind issues an escort yet; the slot is in the vocabulary because
    /// the vocabulary is the owner's list of what a job can ask, not an inventory of today's code.
    /// <see cref="JobTerms.Verb"/> is a total switch, so the day an Escort kind lands the compiler
    /// asks for its verb rather than letting it default to something plausible-looking.</remarks>
    Escort,

    /// <summary>Go and locate a thing whose position is not on any chart yet, then bring it in.</summary>
    Find,
}

/// <summary>
/// What the target IS, in the world — the literal answer to the owner's "a ship or a place?". This is
/// the field the Mars Depot row was missing: a depot hull is a SHIP by every rule the sim knows and a
/// PLACE by every instinct a reader has, and only the card can settle it.
/// </summary>
public enum JobTargetNature
{
    /// <summary>A hull under way between two worlds — she will not be where she is now.</summary>
    Runner,

    /// <summary>A hull parked in a body's orbit and trading from it (a depot). Reads as a place,
    /// counts as a ship: guns work, boarding works, and she does not run.</summary>
    ParkedHull,

    /// <summary>A dead hull adrift — nothing aboard resists, but nothing aboard helps either.</summary>
    Wreck,

    /// <summary>A haven you berth or park at: a station, or a moon with no dock.</summary>
    Haven,

    /// <summary>Something on a surface, reached on foot after a shuttle hop.</summary>
    Ground,

    /// <summary>A hatch, a lockup, a door with a code — a thing at a place, not the place.</summary>
    Lockup,

    /// <summary>A person you hand something to, face to face.</summary>
    Person,
}

/// <summary>
/// Everything the four plain lines need, already resolved by the caller off the live sim. Companion to
/// <see cref="ContractFacts"/>, which owns the flavour-side next-action text; these two are read by the
/// same cards and must never be given different worlds.
/// </summary>
/// <param name="Kind">The contract kind — the one input that fixes the verb.</param>
/// <param name="TargetName">What the job points at, named: "Mars Depot", "The Rusty Roadstead".</param>
/// <param name="Nature">What that target actually is.</param>
/// <param name="WhereName">Where it is, in the captain's words: "Mars orbit", "SATURN system".
/// Null when the target is under way and has no fixed address.</param>
/// <param name="DistanceMeters">Ship to target, RIGHT NOW, measured off live positions. Negative or
/// NaN means the caller could not measure it, and the effort line says so rather than guessing.</param>
/// <param name="LaneSeconds">The rough transfer time from <see cref="JobEffort.LaneSeconds"/>. Zero or
/// negative means "not computable", and the line drops the time half instead of printing "~0 d".</param>
/// <param name="Reward">The payout in credits. Zero means the job pays in something else.</param>
/// <param name="PurseCredits">What the captain is holding — the yardstick the size word is measured
/// against, because "764 cr" is a fortune to a beggar and pocket change to a fleet owner.</param>
public readonly record struct JobFacts(
    ContractKind Kind,
    string TargetName,
    JobTargetNature Nature,
    string? WhereName = null,
    double DistanceMeters = double.NaN,
    double LaneSeconds = 0.0,
    int Reward = 0,
    int PurseCredits = 0,
    bool ForHer = false);

/// <summary>
/// The plain block: four lines above the flavour, on every card a job is offered or carried on (#959).
/// Pure text over caller-supplied numbers, exactly like <see cref="MissionBrief"/>.
/// </summary>
public static class JobTerms
{
    /// <summary>The verb every contract kind asks for — a TOTAL switch with no default arm, so a new
    /// kind cannot slip in wearing somebody else's verb. Kinds may share a verb (three different jobs
    /// all amount to FIND); a kind may never carry two.</summary>
    public static JobVerb Verb(ContractKind kind) => kind switch
    {
        // A hunt ends when the prey stops being a going concern, and the sim accepts EITHER route —
        // a slug through her sail or a boarding tube (Map.Quests: "brought down (holed or boarded)").
        // DESTROY is the headline because that is the outcome bought; the Takes line names both roads.
        ContractKind.Hunt => JobVerb.Destroy,
        ContractKind.CargoRun => JobVerb.Deliver,
        // A tip is bought and settled across the table; what it buys you is the ability to FIND a hull
        // that no public board carries.
        ContractKind.Intel => JobVerb.Find,
        // Both fetches are the same shape: the thing is somewhere nobody has charted for you.
        ContractKind.Fetch => JobVerb.Find,
        ContractKind.FetchCache => JobVerb.Find,
        // #973 L5b · a woman crossed a room and asked for something FOUND. Same verb, and it has to be the
        // same verb: the plain block is the ONE vocabulary a player learns jobs in, and a favour that spoke
        // its own dialect would be a second grammar for the sake of one scene.
        ContractKind.WalkIn => JobVerb.Find,
        // A crack is a locked hatch with a code — you get inside and carry out what is in it.
        ContractKind.Crack => JobVerb.BoardAndRob,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "every contract kind must name its verb"),
    };

    /// <summary>The verb's face, upper-case and in the fixed vocabulary. Never composed, never
    /// interpolated — a card that wants to say something else says it on the Takes line.</summary>
    public static string VerbWord(JobVerb verb) => verb switch
    {
        JobVerb.Destroy => "DESTROY",
        JobVerb.BoardAndRob => "BOARD & ROB",
        JobVerb.Deliver => "DELIVER",
        JobVerb.Escort => "ESCORT",
        JobVerb.Find => "FIND",
        _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, "every verb must have a face"),
    };

    /// <summary>Line 1 — the verb, the target, and where it is: <c>DESTROY — Mars Depot, in Mars
    /// orbit</c>. A target under way carries no place, and says so on line 2 instead of pretending.</summary>
    public static string VerbLine(JobFacts f)
    {
        string head = $"{VerbWord(Verb(f.Kind))} — {NameOr(f.TargetName, "the mark")}";
        return string.IsNullOrWhiteSpace(f.WhereName) ? head : $"{head}, {f.WhereName}";
    }

    /// <summary>Line 2 — what completion actually takes, in game terms, plus what the target IS. The
    /// second half is the owner's whole question ("a ship or a place?") answered in one clause.</summary>
    public static string TakesLine(JobFacts f) => $"Takes: {Completion(f)} {NatureClause(f)}";

    /// <summary>The completion condition, per kind — the physical act that closes the contract.</summary>
    private static string Completion(JobFacts f) => f.Kind switch
    {
        ContractKind.Hunt => "hole her sail OR board her — either one ends it.",
        ContractKind.CargoRun => f.Nature == JobTargetNature.Haven
            ? $"berth (or park in orbit) at {NameOr(f.TargetName, "the drop")} with the parcel aboard."
            : $"hand the parcel over at {NameOr(f.TargetName, "the drop")}.",
        ContractKind.Intel => "nothing to fly — the tip is yours the moment you take it.",
        ContractKind.Fetch => "find the wreck with the scope, fly to her, prise the wallet loose, then hand it over in person.",
        ContractKind.FetchCache => "shuttle down, walk the paces on the map, dig, then carry the chest back.",
        ContractKind.Crack => "key the code into the hatch on foot, lift the package, then hand it back in person.",
        // #973 L5b · two berths and a face. The second half is what makes it hers rather than a delivery: it
        // is not filed, not banked and not handed to a desk — it is told to a person, in a room, out loud.
        ContractKind.WalkIn => $"go to {NameOr(f.WhereName, "the berth she named")}, find {NameOr(f.TargetName, "it")}, then come back and tell her yourself.",
        _ => throw new ArgumentOutOfRangeException(nameof(f), f.Kind, "every contract kind must say what completes it"),
    };

    /// <summary>The half-sentence that settles what the target is. The ParkedHull clause is the one the
    /// owner actually needed; the rest exist so no kind is silently left without an answer.</summary>
    private static string NatureClause(JobFacts f) => f.Nature switch
    {
        JobTargetNature.ParkedHull => $"She is a hull parked{At(f.WhereName)}, not a runner — she trades where she sits and will not flee.",
        JobTargetNature.Runner => "She is a ship under way, so she will not be where she is now — plan for where she is going.",
        JobTargetNature.Wreck => "It is a dead hull adrift — nothing shoots back, and nothing helps.",
        JobTargetNature.Haven => "It is a place, not a ship — no shooting, no boarding; you arrive and hand it over.",
        JobTargetNature.Ground => "It is on the ground — the last stretch is a shuttle hop and a walk.",
        JobTargetNature.Lockup => "It is a locked hatch, not a ship — the work is on foot, with a code.",
        JobTargetNature.Person => "It is a person, face to face — no electronic trace.",
        _ => throw new ArgumentOutOfRangeException(nameof(f), f.Nature, "every target nature must be describable"),
    };

    /// <summary>Line 3 — the honest effort: how far, and roughly how long by the lanes.
    /// <c>≈ 0.61 AU · ~259 d by the lanes</c>. Either half drops out cleanly when the caller could not
    /// measure it, because a missing number is better than an invented one.</summary>
    public static string EffortLine(JobFacts f)
    {
        // The two kinds with no voyage in them at all say so, rather than printing a distance of zero
        // and letting the captain wonder what he is meant to fly to.
        if (f.Kind == ContractKind.Intel)
        {
            return "No flying — the tip settles across this table.";
        }
        if (f.Kind == ContractKind.Crack)
        {
            return "No flying — the hatch is on this station, and the work is on foot.";
        }
        bool haveDistance = !double.IsNaN(f.DistanceMeters) && f.DistanceMeters >= 0;
        bool haveTime = f.LaneSeconds > 0 && !double.IsNaN(f.LaneSeconds) && !double.IsInfinity(f.LaneSeconds);
        if (!haveDistance && !haveTime)
        {
            return "Distance: not measured from here.";
        }
        if (!haveTime)
        {
            return $"≈ {ArrivalStepRule.FormatDistance(f.DistanceMeters)} from the ship now";
        }
        if (!haveDistance)
        {
            return $"~{FormatLaneTime(f.LaneSeconds)} by the lanes";
        }
        return $"≈ {ArrivalStepRule.FormatDistance(f.DistanceMeters)} · ~{FormatLaneTime(f.LaneSeconds)} by the lanes";
    }

    /// <summary>Line 4 — the payout with its one-word size, measured against the purse:
    /// <c>764 cr · small</c>. A job that pays in something other than coin says what instead.</summary>
    public static string PayLine(JobFacts f) => f.ForHer
        // #973 L5b · no coin at all, and the card says so with a dash rather than an explanation. The reason
        // the captain went is on the other three lines and in the woman's own face.
        ? $"{NoPayout} · {SizeWord(f.Reward, f.PurseCredits, forHer: true)}"
        : f.Reward <= 0
        ? "No coin — the job pays in something else."
        : $"{f.Reward.ToString("N0", CultureInfo.InvariantCulture)} cr · {SizeWord(f.Reward, f.PurseCredits)}";

    /// <summary>The four lines, in the order a card stacks them. One call so no surface can render
    /// three of them and quietly drop the fourth.</summary>
    public static IReadOnlyList<string> PlainBlock(JobFacts f) =>
        [VerbLine(f), TakesLine(f), EffortLine(f), PayLine(f)];

    // ===== The size word =====

    /// <summary>Below this the purse is treated as this much anyway, so a captain who is flat broke
    /// does not read every 50-cr errand as the score of his life. Set at a night's fuel and a berth.</summary>
    public const int PurseFloorCredits = 500;

    /// <summary>Under this fraction of the purse the job is pocket change. 764 cr against the ~8 000 cr
    /// the owner was holding is 0.10 — which is why that row deserved to say "small" out loud.</summary>
    public const double SmallCeiling = 0.15;

    /// <summary>Under this it is worth the trip but will not change the week.</summary>
    public const double FairCeiling = 0.60;

    /// <summary>Under this it is a real payday; above it the job is worth more than everything aboard.</summary>
    public const double GoodCeiling = 2.00;

    /// <summary>#973 L5b · THE FIFTH WORD, and the only one that is not about money. The owner named it
    /// himself: a walk-in's card does not say what the job is worth, it says who it is for.</summary>
    public const string ForHerWord = "for her";

    /// <summary>#973 L5b · …and what stands where the credits would be. A dash, and not a sentence: the
    /// vocabulary's whole discipline is that the payout line is read at a glance, and "nothing" is a lie
    /// about a job somebody took anyway.</summary>
    public const string NoPayout = "—";

    /// <summary>One word for how big this purse is TO YOU. The vocabulary is five words, so the hint is read
    /// at a glance and never argued with.</summary>
    /// <param name="forHer">#973 L5b · Whether this is the walk-in's favour, which is sized by WHO rather
    /// than by how much. Asked first, because a ratio against a purse is a question about a job that pays.</param>
    public static string SizeWord(int reward, int purseCredits, bool forHer = false)
    {
        if (forHer)
        {
            return ForHerWord;
        }
        if (reward <= 0)
        {
            return "nothing";
        }
        double yardstick = Math.Max(PurseFloorCredits, purseCredits);
        double ratio = reward / yardstick;
        if (ratio < SmallCeiling) return "small";
        if (ratio < FairCeiling) return "fair";
        if (ratio < GoodCeiling) return "good";
        return "fortune";
    }

    // ===== Formatting =====

    /// <summary>Lane time on the ladder the nav desk already speaks: hours under a day, days under two
    /// years, then years. One decimal only where it carries information.</summary>
    public static string FormatLaneTime(double seconds)
    {
        if (seconds < 86_400.0)
        {
            return (seconds / 3600.0).ToString("0.#", CultureInfo.InvariantCulture) + " h";
        }
        double days = seconds / 86_400.0;
        if (days < 730.0)
        {
            return days.ToString("0.#", CultureInfo.InvariantCulture) + " d";
        }
        return (days / 365.25).ToString("0.#", CultureInfo.InvariantCulture) + " y";
    }

    private static string At(string? where) =>
        string.IsNullOrWhiteSpace(where) ? "" : $" in {where}";

    private static string NameOr(string? name, string fallback) =>
        string.IsNullOrWhiteSpace(name) ? fallback : name;
}

/// <summary>
/// How long a job's trip takes "by the lanes", asked of the sim rather than typed (#959).
/// </summary>
/// <remarks>
/// <para>The model is the one the plotting table itself teaches: a Hohmann half-orbit between the two
/// rails, about the body BOTH ends actually go round. Earth to Mars is read about the Sun and comes out
/// near 259 d; a Mars haven to a depot in Mars orbit is read about MARS and comes out in days, because
/// the shared primary — not the size of the number — is what decides the picture.</para>
/// <para>It is a rough figure and the card says <c>~</c> out loud. It ignores phasing (the transfer
/// window may be months away), it ignores what the tank can pay for, and on two ends of the SAME rail
/// it degenerates to half that rail's period — which is honest, because catching something on your own
/// orbit IS a phasing problem and not a short hop. What it will never do is invent a cruise speed.</para>
/// </remarks>
public static class JobEffort
{
    /// <summary>The rough transfer time between two circular rails about a primary of parameter
    /// <paramref name="muPrimary"/>. Returns 0 when the caller cannot supply a real geometry — an
    /// unknown body, a primary with no mass — and the effort line then simply omits the time.</summary>
    public static double LaneSeconds(double muPrimary, double radius1Meters, double radius2Meters)
    {
        if (muPrimary <= 0 || radius1Meters <= 0 || radius2Meters <= 0
            || double.IsNaN(muPrimary) || double.IsNaN(radius1Meters) || double.IsNaN(radius2Meters))
        {
            return 0.0;
        }
        return TransferMath.Hohmann(radius1Meters, radius2Meters, muPrimary).TransferSeconds;
    }

    /// <summary>
    /// THE BODY BOTH ENDS GO ROUND — the one choice the effort line lives or dies on.
    /// </summary>
    /// <remarks>
    /// The plotting table's own law (#926, the owner: <i>"I had to remember to switch to Sun to get the
    /// ship to really start moving from Earth towards Mars"</i>): a trip is read in the frame of the body
    /// both ends go round. Here that is a planet when the ship and the target are in the SAME planet's
    /// system — a berth and a depot both circling Mars — and the root (the Sun) in every other case,
    /// including when either end cannot be placed at all.
    /// <para>Getting this wrong is not a rounding error. A Mars-local hop read against the Sun puts both
    /// ends on one rail and returns half a Martian year for a trip of days.</para>
    /// </remarks>
    public static CelestialBody? SharedPrimary(ICelestialEphemeris ephemeris, string? shipAnchorId, string? targetAnchorId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        CelestialBody? shipPlanet = PlanetLevelAncestor(ephemeris, shipAnchorId);
        CelestialBody? targetPlanet = PlanetLevelAncestor(ephemeris, targetAnchorId);
        return shipPlanet is not null && targetPlanet is not null && shipPlanet.Id == targetPlanet.Id
            ? shipPlanet
            : Root(ephemeris);
    }

    /// <summary>The planet-level ancestor of a body: walk up parents until the next one up is the
    /// parentless root. A moon or a station rides its planet round the sun, so this is the body whose
    /// heliocentric orbit the place actually keeps. A planet returns itself; so does a heliocentric
    /// station with no planet above it. Null for an unknown id, and for the root itself — the sun is
    /// nobody's neighbourhood.</summary>
    public static CelestialBody? PlanetLevelAncestor(ICelestialEphemeris ephemeris, string? bodyId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        CelestialBody? b = Find(ephemeris, bodyId);
        if (b is null || b.ParentId is null)
        {
            return null;
        }
        while (Find(ephemeris, b.ParentId) is { } parent)
        {
            if (parent.ParentId is null)
            {
                break;   // the parent is the root — b is already planet-level
            }
            b = parent;
        }
        return b;
    }

    /// <summary>The parentless body at the middle of the system — the sun in every shipped scenario.</summary>
    public static CelestialBody? Root(ICelestialEphemeris ephemeris)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        foreach (CelestialBody b in ephemeris.Bodies)
        {
            if (b.ParentId is null)
            {
                return b;
            }
        }
        return null;
    }

    private static CelestialBody? Find(ICelestialEphemeris ephemeris, string? id)
    {
        if (id is null)
        {
            return null;
        }
        foreach (CelestialBody b in ephemeris.Bodies)
        {
            if (b.Id == id)
            {
                return b;
            }
        }
        return null;
    }
}
