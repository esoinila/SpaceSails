using System;

namespace SpaceSails.Core;

/// <summary>
/// #957 — <b>DON'T COMPLAIN, BRAKE.</b> The owner, flying right up to The Rusty Roadstead and pressing
/// Dock: <i>"It should just add the necessary braking step on the plot path and not complain. It is
/// annoying — nobody will ever play the 'let's fly next to it really quiet so autopilot will agree'. That
/// takes forever and ruins the fun of play. … Does it not plot the path to nearest point and add the
/// necessary steps to make it work?"</i>
///
/// <para>So before the autopilot refuses an arrival it cannot verify, it asks THIS: <i>would one burn,
/// bought inside the reserve rule, turn the refusal into a promise?</i> If yes, the burn becomes a step
/// on the plan and the ship is armed; only if no does the captain hear a refusal — and then with numbers
/// (<see cref="ArrivalStepRule.RefusalText"/>).</para>
///
/// <para><b>Two things can be wrong, so two things are tried.</b> A pass can be too FAST for the window
/// (<see cref="OrbitRule.MaxRelativeSpeed"/>) or the clamp (<see cref="DockRule.MatchSpeed"/>) — that is
/// a brake, straight back along −v_rel, and it is the owner's word for this whole feature. A pass can
/// also simply go by too WIDE, and a brake does nothing for that: scaling the relative velocity leaves
/// its DIRECTION alone, so the impact parameter — the miss distance — is unchanged to first order. That
/// one wants a nudge across the track. Both aims ride the same magnitude ladder, smallest first, so the
/// first candidate that flies is also the cheapest.</para>
///
/// <para><b>How it is honest.</b> The search does not model the capture itself — it hands each candidate
/// to <see cref="AutopilotRehearsal.Rehearse"/> as a one-burn <see cref="TransferPlanner.Schedule"/> and
/// believes only what the rehearsal flies. That is the same machinery the #146 moon transfer arms with,
/// so a burn accepted here is priced, budget-checked and flown by exactly the code that quoted it: the
/// arm-time promise ("it won't strand you") is unchanged, and the #928 tenth applies at both ends because
/// the rehearsal applies it at both ends. A candidate that fails is simply not returned; nothing here can
/// invent a capture the rehearsal did not fly.</para>
/// </summary>
public static class CaptureBrake
{
    /// <summary>The magnitudes the search tries, as fractions of the CURRENT relative speed — smallest
    /// first, so the first candidate that flies is also the cheapest one.</summary>
    public static readonly double[] ShedFractions = [0.15, 0.3, 0.5, 0.75, 1.0];

    /// <summary>Which way the correction points.</summary>
    public enum Aim
    {
        /// <summary>Straight back along −v_rel: shed relative speed. The fix when the pass is close
        /// enough but too fast for the window or the clamp.</summary>
        Brake,

        /// <summary>Across the track, one way…</summary>
        CrossPlus,

        /// <summary>…and the other. Which sign closes a wide miss depends on the geometry, and the
        /// rehearsal is the only honest judge of that, so both are flown.</summary>
        CrossMinus,
    }

    /// <summary>The aims, in the order they are tried at each magnitude: brake first (the owner's word,
    /// and the cheapest thing that can be wrong), then the two cross-track nudges.</summary>
    public static readonly Aim[] Aims = [Aim.Brake, Aim.CrossPlus, Aim.CrossMinus];

    /// <summary>Hard cap on how many candidates are rehearsed for one click. Each candidate is a real
    /// flight through Core and this runs on the arm button in WASM, so the cap — not the ladder — is what
    /// bounds the wall time. Twelve covers every aim at the four smallest magnitudes.</summary>
    public const int MaxCandidates = 12;

    /// <param name="SimTime">When the correction fires.</param>
    /// <param name="DeltaV">The impulse.</param>
    /// <param name="Aim">Which way it pointed — brake, or across the track.</param>
    /// <param name="RawPulses">Δv priced with <see cref="OrbitRule.PulsesFor"/> — what a hand would pay.</param>
    /// <param name="ChargedPulses">What the TANK loses for the whole armed journey including this burn,
    /// at the #928 tenth — the number to quote, never the raw one.</param>
    /// <param name="DeltaVMetersPerSecond">The impulse's magnitude, for the step line.</param>
    /// <param name="Schedule">The one-burn schedule to arm with: hand it straight to
    /// <see cref="AutopilotRehearsal.Rehearse"/> and to the live armed loop, so the flown burn IS the
    /// rehearsed burn.</param>
    /// <param name="Rehearsal">The proof — a Deliverable rehearsal flown WITH the burn.</param>
    public readonly record struct Solution(
        double SimTime,
        Vector2d DeltaV,
        Aim Aim,
        int RawPulses,
        int ChargedPulses,
        double DeltaVMetersPerSecond,
        TransferPlanner.Schedule Schedule,
        AutopilotRehearsal.RehearsalResult Rehearsal);

    /// <summary>
    /// Find the cheapest single burn that makes the armed arrival at <paramref name="targetBodyId"/>
    /// deliverable inside <paramref name="budgetPulses"/>, or null when no candidate on the ladder does.
    /// </summary>
    /// <param name="ship">The state at the instant of the click.</param>
    /// <param name="ephemeris">The same rails the live sim flies.</param>
    /// <param name="simulator">Advances the coasts inside the rehearsal.</param>
    /// <param name="targetBodyId">The body the captain asked to arrive at.</param>
    /// <param name="budgetPulses">Tank minus the <see cref="AutopilotRehearsal.ReservePulses"/> floor —
    /// the same budget the plain arm rehearses against, so a correction can never be bought out of the
    /// reserve.</param>
    /// <param name="burnEpoch">When the burn may fire. Null means "now", which is also the epoch with the
    /// most leverage over a miss distance — a nudge across the track buys more the earlier it is spent.</param>
    /// <param name="maxHorizonSeconds">How far each candidate is flown before it is given up on. The
    /// caller shortens this to a few times the encounter's own time-to-pass: a candidate that only pays
    /// off months later is not the answer to "I am right next to it, dock". Shortening can MISS a
    /// solution; it can never invent one.</param>
    /// <param name="capturePath">Record the rehearsed path (for the #148 intended-track polyline).</param>
    public static Solution? Solve(
        ShipState ship,
        ICelestialEphemeris ephemeris,
        Simulator simulator,
        string targetBodyId,
        int budgetPulses,
        double? burnEpoch = null,
        double maxHorizonSeconds = AutopilotRehearsal.DefaultMaxHorizonSeconds,
        bool capturePath = true)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        ArgumentNullException.ThrowIfNull(simulator);

        CelestialBody? body = null;
        foreach (CelestialBody candidate in ephemeris.Bodies)
        {
            if (candidate.Id == targetBodyId) { body = candidate; break; }
        }
        if (body?.ParentId is null || budgetPulses <= 0)
        {
            return null;
        }

        double epoch = Math.Max(ship.SimTime, burnEpoch ?? ship.SimTime);
        ShipState at = epoch > ship.SimTime ? simulator.RunAdaptive(ship, epoch - ship.SimTime) : ship;
        Vector2d bodyVel = BodyVelocity(ephemeris, targetBodyId, at.SimTime);
        Vector2d relVel = at.Velocity - bodyVel;
        double relSpeed = relVel.Length;
        if (relSpeed <= 0)
        {
            return null;
        }

        Vector2d along = relVel / relSpeed;
        var across = new Vector2d(-along.Y, along.X);

        int tried = 0;
        foreach (double fraction in ShedFractions)
        {
            double magnitude = fraction * relSpeed;
            int raw = OrbitRule.PulsesFor(magnitude, at.Velocity.Length);
            if (AutopilotRehearsal.Charged(raw) > budgetPulses)
            {
                // Even this impulse alone outruns the budget; every larger one will too.
                break;
            }

            foreach (Aim aim in Aims)
            {
                if (tried >= MaxCandidates)
                {
                    return null;
                }
                tried++;

                Vector2d direction = aim switch
                {
                    Aim.Brake => -along,
                    Aim.CrossPlus => across,
                    _ => -across,
                };
                Vector2d deltaV = direction * magnitude;
                var schedule = new TransferPlanner.Schedule(
                    [new TransferPlanner.BurnStep(at.SimTime, deltaV)], at.SimTime);

                AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
                    ship, ephemeris, simulator, targetBodyId, budgetPulses,
                    capturePath: capturePath, maxHorizonSeconds: maxHorizonSeconds, schedule: schedule);

                if (r.Deliverable)
                {
                    return new Solution(
                        at.SimTime, deltaV, aim, raw, r.PulsesCharged, magnitude, schedule, r);
                }
            }
        }

        return null;
    }

    /// <summary>The word for what the burn does, in the captain's language.</summary>
    public static string AimWord(Aim aim) => aim == Aim.Brake ? "brake" : "trim across the track";

    /// <summary>The burn's one-line receipt as a flight-plan row — what the autopilot ADDED instead of
    /// complaining (#957). Quotes the CHARGED number, the only one the tank ever feels (#928).</summary>
    public static string StepLine(Solution s, string bodyName) =>
        $"🛑 {AimWord(s.Aim)} {ArrivalStepRule.FormatSpeed(s.DeltaVMetersPerSecond)} for {bodyName}"
        + $" — autopilot, ≈{s.ChargedPulses} p the whole way";

    /// <summary>The pulse message when the refusal became a burn instead.</summary>
    public static string AddedText(Solution s, string bodyName) =>
        $"🛰 Not declining — the autopilot lays a step instead: {AimWord(s.Aim)} "
        + $"{ArrivalStepRule.FormatSpeed(s.DeltaVMetersPerSecond)} and arrive at {bodyName}. "
        + $"Whole journey ≈{s.ChargedPulses} p at the autopilot's tenth.";

    private static Vector2d BodyVelocity(ICelestialEphemeris ephemeris, string id, double simTime) =>
        (ephemeris.Position(id, simTime + 1.0) - ephemeris.Position(id, simTime - 1.0)) / 2.0;
}
