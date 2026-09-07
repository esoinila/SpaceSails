namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L4 · THE OLD SHIP — the survey tender the captain would not sign for.
//
// L5a put her in the bible and nowhere else: HALCYON REACH ran sealed pods up the KAAMOS supply chain
// under a manifest that said medical, the crew opened one, the captain would not sign the manifest,
// Corwin Sallis did, and she was impounded and renamed. What was in the pods is never named — not
// here, not in her dossier, not on the sheet she files. Her old name rides `ShipHistory`, which is the
// one place in this game a hull is allowed to be haunted by what she used to be called.
//
// SHE IS AN NPC HULL AND NOTHING ELSE. No new system: she is seeded exactly the way a supply depot is
// (`TrafficSchedule.GenerateDepots`) — a hull parked on a marker orbit at a berth, riding its rail,
// going nowhere, because she is impounded and impounded ships do not go anywhere. That is why the
// fiction and the physics agree without a word of special-casing in the simulator.
//
// The berth is the GREAT PORT the seed picks, and only a great port: an impound lot is where the
// traffic and the paperwork are, which is the same rule that puts Ilse's claims desk at one.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#973 L4 · The renamed HALCYON REACH: who she was, where this thread parked her, and the
/// memory she gives back the first time the captain sees her again.</summary>
public static class TheOldShip
{
    // ── §1 · WHO SHE WAS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>The name on the boat deck, on the bunting, on the photograph — and on nothing official
    /// since the day they took her.</summary>
    public const string FormerName = "HALCYON REACH";

    /// <summary>Her class, in the one word a registry uses for it.</summary>
    public const string Class = "survey tender";

    /// <summary>What her fate was, in the parenthesis a dossier's former-name line hangs off it —
    /// <c>ShipHistories.Fates</c>' own idiom, so her line reads like every other hull's and not like a
    /// story being told at the captain.</summary>
    public const string Fate = "survey tender, impounded, renamed";

    /// <summary>Her former-name line as a dossier lists it: <i>ex-HALCYON REACH (survey tender, impounded,
    /// renamed)</i>. One string, so the scope's teaser and the comms ledger cannot drift.</summary>
    public const string FormerNameEntry = $"ex-{FormerName} ({Fate})";

    /// <summary>
    /// The name she answers to now.
    ///
    /// <para>The name the registry gave her when they impounded and renamed her — the name Teo's slip means
    /// by <i>"the name she was given when they took her"</i>. It is what a clerk types, not what anybody
    /// christened: a word about paperwork, worn by a ship that was decent.</para>
    /// </summary>
    public const string Callsign = "COMPLIANT";

    /// <summary>The one hull id this ship ever has. Constant rather than seeded: there is one REACH per
    /// universe and her identity is not a roll — only her BERTH is.</summary>
    public const string ShipId = "npc-the-reach";

    /// <summary>Is this contact her? Asked wherever a scope, a dossier or a berth wants to know.</summary>
    public static bool IsHer(string? shipId) => string.Equals(shipId, ShipId, StringComparison.Ordinal);

    /// <summary>
    /// Her service record, authored rather than rolled. <see cref="ShipHistories.For"/> hands every other
    /// hull in the game a seeded Victoria-I story; hers is the one the arc already wrote, so it is returned
    /// whole from there and the dossier needs to learn nothing new.
    ///
    /// <para>Two owners deep and one former name: the rename is one re-registration, and the impound board
    /// that holds her paper is the other. The condition line is the fleet's own key.</para>
    /// </summary>
    public static ShipHistory History { get; } = new(
        Yard: "Vellamo Drydocks (the cloud-yards, Cinder Roost)",
        Year: 2298,
        FormerNames: [FormerNameEntry],
        OwnersDeep: 2,
        Condition: "She's carried better names in better days, and she remembers them.");

    // ── §2 · WHERE THIS THREAD PARKED HER ────────────────────────────────────────────────────────────

    /// <summary>
    /// The berth she is tied up at in this universe: the great port the seed picks, or — in a scenario with
    /// no great port at all — the berth the seed picks out of whatever the world has.
    ///
    /// <para>The fallback is <see cref="OldCrew.BerthsFor"/>'s own law, and it is here for the same reason:
    /// a rule that could return nothing would make her exist in some scenarios and not others, and a lane
    /// that quietly does not happen is worse than one that happens somewhere slightly wrong.</para>
    /// </summary>
    /// <returns>The berth's body id, or null when the world has no dockable berth at all.</returns>
    public static string? BerthFor(string threadId, IReadOnlyList<OldCrew.Berth> berths)
    {
        ArgumentNullException.ThrowIfNull(threadId);
        ArgumentNullException.ThrowIfNull(berths);
        if (berths.Count == 0)
        {
            return null;
        }

        var greatPorts = new List<OldCrew.Berth>();
        foreach (OldCrew.Berth b in berths)
        {
            if (b.Tier == ArrivalTube.Tier.GreatPort)
            {
                greatPorts.Add(b);
            }
        }

        IReadOnlyList<OldCrew.Berth> choices = greatPorts.Count > 0 ? greatPorts : berths;
        return choices[(int)(DiceRule.Seed($"the-reach|berth|{threadId}") % (ulong)choices.Count)].Id;
    }

    /// <summary>What the manifest of an impounded hull says. Clerical, never a cargo class the fence has a
    /// price for, and paired with a manifest of zero — she is a lot on an impound board, not a hull anybody
    /// can prise anything out of.</summary>
    public const string Manifest = "Impounded";

    /// <summary>
    /// HER, AS A HULL IN THE WORLD. The shape is a supply depot's exactly (<see cref="TrafficSchedule.
    /// GenerateDepots"/>): a marker orbit at a body, an empty plan, no maneuver budget, and a rails state
    /// the simulator reads closed-form rather than integrating — which is what an impounded ship IS, and
    /// which is why she needs no special case anywhere in the sim.
    /// </summary>
    /// <param name="ephemeris">The world she is parked in.</param>
    /// <param name="bodyId">The berth <see cref="BerthFor"/> picked.</param>
    /// <param name="threadId">The universe — the only thing her berthing phase is seeded off.</param>
    public static NpcShip Berthed(ICelestialEphemeris ephemeris, string bodyId, string threadId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(threadId);

        CelestialBody body = ephemeris.Bodies.First(b => b.Id == bodyId);
        double radius = Math.Max(body.BodyRadius * 8, 2e6);

        // A whole turn of phase, in one ten-thousandth steps — deterministic in the universe, so a reload
        // finds her tied up on the same side of the port she was on when the captain last looked at her.
        double phase = DiceRule.Seed($"the-reach|phase|{threadId}") % 10000UL / 10000.0 * (Math.PI * 2);

        return new NpcShip(
            Id: ShipId,
            Callsign: Callsign,
            CargoClass: Manifest,
            OriginId: bodyId,
            DestinationId: bodyId,
            Personality: RoutePersonality.Economical,
            DepartureTime: 0,
            ActivationTime: 0,
            InitialState: TrafficSchedule.DepotState(ShipId, bodyId, radius, phase, ephemeris, 0),
            Plan: new ManeuverPlan([]),
            EstimatedArrivalTime: double.MaxValue,
            CargoUnits: 0,
            ManeuverBudget: 0,
            IsPod: false,
            DepotBodyId: bodyId,
            DepotOrbitRadius: radius,
            DepotPhase: phase);
    }

    // ── §3 · WHAT SHE GIVES BACK ─────────────────────────────────────────────────────────────────────

    /// <summary>The sheet she files, once per thread, the first time the captain has her on the scope or
    /// ties up alongside her. Marked <i>mine</i> and tagged <b>love</b> — she is the only piece of the
    /// decent past the captain can walk up to and put a hand on.</summary>
    public const string SheetId = "the-reach";

    /// <summary>Fable's words, verbatim. Nothing on this page names what was in the pods, and nothing on it
    /// says which of the two of you put the dent in the rail.</summary>
    public const string SheetText =
        "The boat deck. The davit where the bunting hung. A dent in the rail at the height of your hip "
        + "that you put there, or he did. You know the sound the hatch makes before it makes it.";
}
