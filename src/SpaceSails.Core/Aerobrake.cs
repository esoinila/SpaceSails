using System;

namespace SpaceSails.Core;

/// <summary>
/// #290 — 🪂 AEROBRAKE: the ice giant's haze as a findable, PRICED arrival brake. The owner arrived at
/// Uranus stranded (playtest 2026-07-17): 29.8 km/s relative, 32 pulses in the tank, the atmosphere right
/// there. A pure-propulsive capture costs 42 pulses — flatly impossible on a 32-pulse tank — but the air
/// pays ~11 of them, dropping the burn to a payable 31. This module turns Lab 32's printed physics
/// (labs/32-aerocapture-at-the-ice-giant) into an honestly-quoted third way to pay the arrival bill,
/// beside the propulsive insertion (<see cref="LongHaul.SolveInsertion"/>) and the warn-and-coast.
///
/// <para><b>One physics, no forked model.</b> Every number is MEASURED by flying real passes through the
/// exact Core drag the game already uses (<see cref="Simulator.RunAdaptiveWithDrag"/> / drag report), in a
/// planet-centred (body-at-origin) frame so the two-body energy is clean. Drag depends only on speed
/// RELATIVE to the air, so this frame is Galilean-identical to the live sim where the shell translates with
/// the planet's rail — Lab 33 proves the very campaign converges under the live n-body integrator.</para>
///
/// <para><b>The honest trade.</b> A hyperbolic arrival gets ONE periapsis pass. Below a critical arrival
/// speed a single survivable (≤ 3 g) pass captures to a bound orbit and free tightening passes circularise
/// — the corridor is OPEN and the brake is nearly free (only time and a small trim). Above it (the owner's
/// 29.8 km/s is nearly double Uranus's ~16 km/s critical) the corridor is CLOSED: the deepest survivable
/// pass still sheds a real chunk for free, but a propellant BRIDGE burn on that same pass is mandatory to
/// go bound. Either way the quote states passes needed, Δv shed, pulses saved vs the propulsive brake, and
/// the peak g/heat taken — or refuses with the reason (a landing atmosphere, an arrival already slow enough
/// to catch, or a bridge that outruns the tank even with the haze's help).</para>
///
/// <para><b>Pure and deterministic.</b> Fixed sweep schedule, fixed slice, no wall clock, no randomness —
/// client WASM and any replay agree to the bit. The v1 per-pass hull/heat cost is deterministic; the
/// dice-scripted episode currency (owner's still-open Q3) plugs in at <see cref="DiceEpisodeHook"/>.</para>
/// </summary>
public static class Aerobrake
{
    /// <summary>Standard gravity (m/s²) — only for expressing peak drag deceleration in g.</summary>
    public const double G0 = 9.80665;

    /// <summary>The hull-damage line, shared with the live sail-hole consequence and Lab 22/32:
    /// <see cref="Atmosphere.SailHoleDecelG"/> = 3 g. A pass at or above it holes the sail.</summary>
    public const double GBudgetG = Atmosphere.SailHoleDecelG;

    /// <summary>The skim / landing split (Lab 32 Section E). Thick shells — Titan (5.3 kg/m³), Venus
    /// (65) — grab any entering pass only at brutal g: they are LANDING atmospheres, not brakes. The
    /// giants (~1×10⁻⁵), Earth (1.2) and Mars (0.02) sit below this line and give a gentle skim corridor.
    /// One documented threshold, so an aerobrake affordance offers only where a captain can actually skim.</summary>
    public const double LandingAtmosphereDensityThreshold = 2.0;

    /// <summary>Once bound, the free tightening campaign is flown at most this many passes before the
    /// quote hands off to a small circularisation trim (Lab 32 C2 needed 5). Bounds the quote's flying.</summary>
    public const int MaxTighteningPasses = 12;

    /// <summary>An aerobrake that would save fewer than this many pulses over the propulsive brake is not
    /// worth a separate affordance — the honest read is "just brake on thrust". Keeps the option from
    /// advertising a rounding-error saving on a gentle arrival that the normal insert already handles cheaply.</summary>
    public const int MinWorthwhileSaved = 2;

    /// <summary>Below this hyperbolic-excess speed (m/s) the arrival is effectively already captured — there
    /// is nothing to brake, so the quote is moot. This is the CAPTURE floor (a near-parabolic arrival), NOT
    /// the dock clamp speed: a 6 km/s v∞ Uranus arrival is genuinely hyperbolic and wants braking (Lab 32's
    /// Section E corridor), so the floor sits far below the 8 km/s <see cref="DockRule.MatchSpeed"/>.</summary>
    public const double MinExcessSpeed = 300.0;

    /// <summary>True when a body's air is a SKIM atmosphere an aerobrake can offer at (a giant, Earth or
    /// Mars) — not a landing atmosphere (Titan/Venus) and not airless. Drives the menu row's visibility.</summary>
    public static bool IsSkimAtmosphere(Atmosphere? atmosphere) =>
        atmosphere is not null && atmosphere.RefDensity < LandingAtmosphereDensityThreshold;

    /// <summary>The verdict of pricing an aerobrake arrival at a world.</summary>
    public enum Outcome
    {
        /// <summary>The corridor is OPEN: one survivable pass captures and free passes circularise — the
        /// brake is nearly free (time + a small trim). The cheapest possible arrival.</summary>
        SoloCapture,

        /// <summary>The corridor is CLOSED (arrival too hot for a solo capture) but the haze still pays a
        /// real chunk: a hybrid — one hot pass sheds free, an affordable bridge burn finishes the capture.
        /// The owner's Uranus stranding.</summary>
        HybridBridge,

        /// <summary>The world's air is a landing atmosphere (Titan/Venus) or airless — no skim brake here.</summary>
        RefuseNoSkimAir,

        /// <summary>The arrival is already slow enough to catch — nothing to brake (moot).</summary>
        RefuseMoot,

        /// <summary>Too hot: even with the haze's free shed, the bridge burn outruns the tank — the air
        /// cannot save this arrival (v∞ far past the corridor's close). Brake on thrust or arrive slower.</summary>
        RefuseTooHot,
    }

    /// <summary>The priced aerobrake trade for one arrival, every number measured off flown drag passes.</summary>
    /// <param name="Outcome">The verdict (offer as solo/hybrid, or a refusal-with-reason).</param>
    /// <param name="ArrivalVinf">The hyperbolic excess speed relative to the planet the brake works on (m/s).</param>
    /// <param name="EntrySpeed">Periapsis entry speed √(v∞²+v_esc²) at the priced pass depth (m/s).</param>
    /// <param name="FreeShedMps">Δv the deepest survivable (≤ 3 g) single pass sheds for FREE.</param>
    /// <param name="CaptureDeltaV">Δv that must vanish for the arrival to go bound (v_peri − local escape).</param>
    /// <param name="BridgeMps">The propellant bridge the air can't pay (Δv, 0 when the corridor is open).</param>
    /// <param name="PropulsivePulses">The pure-thrust capture bill — the number the aerobrake beats.</param>
    /// <param name="AerobrakePulses">The aerobrake's own bill: the bridge burn (0 on a solo capture).</param>
    /// <param name="PulsesSaved">Pulses the haze pays — <see cref="PropulsivePulses"/> − <see cref="AerobrakePulses"/>.</param>
    /// <param name="PassesNeeded">Total passes: the capture/bridge pass plus the free tightening passes.</param>
    /// <param name="TighteningPasses">Free bound passes that circularise after capture (Lab 32 C2 ≈ 5).</param>
    /// <param name="PeakG">Peak drag deceleration of the priced pass, in g (the hull load taken).</param>
    /// <param name="PeakDynamicPressurePa">Peak dynamic pressure — the heat proxy a thermal model would read.</param>
    /// <param name="PriceBasisSpeed">The world speed the pulses are priced against (periapsis speed).</param>
    public readonly record struct Quote(
        Outcome Outcome,
        double ArrivalVinf,
        double EntrySpeed,
        double FreeShedMps,
        double CaptureDeltaV,
        double BridgeMps,
        int PropulsivePulses,
        int AerobrakePulses,
        int PulsesSaved,
        int PassesNeeded,
        int TighteningPasses,
        double PeakG,
        double PeakDynamicPressurePa,
        double PriceBasisSpeed)
    {
        /// <summary>True when the trade is worth offering (a solo capture or an affordable hybrid).</summary>
        public bool Offered => Outcome is Outcome.SoloCapture or Outcome.HybridBridge;

        /// <summary>True when the corridor is open (a solo capture with no bridge burn owed).</summary>
        public bool CorridorOpen => Outcome == Outcome.SoloCapture;
    }

    /// <summary>Vacuum escape speed at radius r about a body of gravitational parameter μ.</summary>
    private static double Vesc(double mu, double r) => Math.Sqrt(2 * mu / r);

    /// <summary>A single-body, body-at-origin pinned sim (parentId null + orbitPeriod 0 pins the body at
    /// the origin with zero rail velocity), so v_rel is just the ship's velocity and the two-body energy is
    /// clean — the same rig Lab 32's probe and QA gates fly. One physics: the live drag, isolated.</summary>
    private static Simulator PinnedSim(double mu, double radius, Atmosphere atmosphere) =>
        new(new CircularOrbitEphemeris([new CelestialBody("b", "b", null, mu, radius, 0, 0, 0, Atmosphere: atmosphere)]),
            timeStepSeconds: 1.0);

    /// <summary>Incoming hyperbolic state on the +x axis at radius rStart whose vacuum periapsis is rPeri.</summary>
    private static ShipState HyperbolicArrival(double mu, double rStart, double rPeri, double vInf)
    {
        double vPeri = Math.Sqrt(vInf * vInf + 2 * mu / rPeri);
        double h = rPeri * vPeri;
        double v = Math.Sqrt(vInf * vInf + 2 * mu / rStart);
        double vt = h / rStart;
        double vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
        return new ShipState(new Vector2d(rStart, 0), new Vector2d(vr, vt), 0);
    }

    /// <summary>Incoming BOUND state on the ellipse with the given periapsis and apoapsis (seeds the free
    /// tightening campaign from a just-captured wide orbit — Lab 32 C2's symmetry trick).</summary>
    private static ShipState BoundArrival(double mu, double rStart, double rPeri, double rApo)
    {
        double a = (rPeri + rApo) / 2.0;
        double vPeri = Math.Sqrt(mu * (2.0 / rPeri - 1.0 / a));
        double h = rPeri * vPeri;
        double v = Math.Sqrt(Math.Max(0, mu * (2.0 / rStart - 1.0 / a)));
        double vt = h / rStart;
        double vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
        return new ShipState(new Vector2d(rStart, 0), new Vector2d(vr, vt), 0);
    }

    private static double Energy(ShipState s, double mu) => s.Velocity.LengthSquared / 2.0 - mu / s.Position.Length;

    private static double Apoapsis(ShipState s, double mu)
    {
        double e0 = Energy(s, mu);
        if (e0 >= 0) return double.PositiveInfinity;
        double a = -mu / (2 * e0);
        double h = Math.Abs(s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X);
        double ecc = Math.Sqrt(Math.Max(0, 1 + 2 * e0 * h * h / (mu * mu)));
        return a * (1 + ecc);
    }

    /// <summary>One flown pass: from an above-shell entry, down through periapsis, until the ship climbs
    /// back above the shell top (measured, so a captured pass is never double-counted). The trapped-orbit
    /// guard (a bound orbit whose apoapsis stays below the shell top can never climb clear — a landing, not
    /// a capture) ends the pass as a crash. Identical to Lab 32's probe/QA FlyPass.</summary>
    private readonly record struct PassResult(
        ShipState Post, double PeakDecel, double DvShed, double PeakDynamicPressure, double MinAltitude, bool Crashed);

    private static PassResult FlyPass(Simulator sim, ShipState entry, double bodyRadius, double topAltitude, double mu)
    {
        double shellTop = bodyRadius + topAltitude;
        double peak = 0, shed = 0, minAlt = double.PositiveInfinity, peakQ = 0;
        ShipState s = entry;
        bool entered = false, crashed = false;
        while (s.SimTime - entry.SimTime < 3 * 3600)
        {
            (ShipState next, Simulator.DragReport rep) =
                sim.RunAdaptiveWithDrag(s, 20.0, null, minTimeStep: 0.05, maxTimeStep: 2.0);
            peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
            peakQ = Math.Max(peakQ, rep.PeakDynamicPressurePascal);
            shed += rep.DeltaVShedMetersPerSecond;
            if (!double.IsNaN(rep.MinAltitudeMeters)) minAlt = Math.Min(minAlt, rep.MinAltitudeMeters);
            s = next;
            double r = s.Position.Length;
            if (r < bodyRadius) { crashed = true; break; }
            if (r < shellTop)
            {
                entered = true;
                if (Energy(s, mu) < 0 && Apoapsis(s, mu) < shellTop) { crashed = true; break; }
            }
            else if (entered) break;
        }
        if (crashed) minAlt = 0;
        return new PassResult(s, peak, shed, peakQ, double.IsPositiveInfinity(minAlt) ? double.NaN : minAlt, crashed);
    }

    /// <summary>
    /// Price an aerobrake arrival at <paramref name="planet"/> for a ship arriving with hyperbolic excess
    /// speed <paramref name="arrivalVinf"/> relative to it, against a tank of <paramref name="budgetPulses"/>.
    /// Flies a depth sweep and (when bound) a short tightening campaign through the live Core drag to MEASURE
    /// the deepest survivable free shed, whether a solo capture corridor exists, the propellant bridge, and
    /// the pulse bills. Pure and deterministic. A landing/airless world, an already-slow arrival, or a bridge
    /// that outruns the tank even with the haze's help each return a refusal outcome with zeroed numbers where
    /// they don't apply.
    /// </summary>
    public static Quote Price(CelestialBody planet, double arrivalVinf, int budgetPulses)
    {
        if (!IsSkimAtmosphere(planet.Atmosphere))
        {
            return new Quote(Outcome.RefuseNoSkimAir, arrivalVinf, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        Atmosphere atm = planet.Atmosphere!;
        double mu = planet.Mu, R = planet.BodyRadius, top = atm.TopAltitude;

        // An arrival already all but captured (near-parabolic) has nothing to brake (moot).
        if (arrivalVinf <= MinExcessSpeed)
        {
            return new Quote(Outcome.RefuseMoot, arrivalVinf, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var sim = PinnedSim(mu, R, atm);
        double start = R + top + 3.0e5;              // enter 300 km above the shell top

        // Sweep depth (as km altitude) for the DEEPEST survivable (≤ 3 g) pass — the free shed the air pays —
        // and whether any survivable pass CAPTURES to an orbit that clears the shell (the open corridor).
        // Step ~0.15 scale heights so the exponentially-sharp corridor is resolved (Uranus H=120 km → 18 km),
        // from a shallow floor down to half the shell — deep enough to find the g-limit pass, cheap enough
        // for a one-shot menu quote (~30 flown passes). One physics, measured, no hand-tuned curve.
        double stepKm = Math.Max(8.0, 0.15 * atm.ScaleHeight / 1000.0);
        double floorKm = Math.Max(stepKm, 0.02 * top / 1000.0);
        double deepKm = 0.55 * top / 1000.0;

        double bestFreeShed = 0, bestFreeAltKm = double.NaN, bestFreeG = 0, bestFreeQ = 0;
        bool corridorOpen = false;
        for (double altKm = floorKm; altKm <= deepKm; altKm += stepKm)
        {
            double rPeri = R + altKm * 1000.0;
            PassResult pass = FlyPass(sim, HyperbolicArrival(mu, start, rPeri, arrivalVinf), R, top, mu);
            double peakG = pass.PeakDecel / G0;
            bool survivable = !pass.Crashed && peakG <= GBudgetG;
            bool captured = !pass.Crashed && Energy(pass.Post, mu) < 0;
            bool orbits = captured && Apoapsis(pass.Post, mu) > R + top;
            if (survivable && pass.DvShed > bestFreeShed)
            {
                bestFreeShed = pass.DvShed;
                bestFreeAltKm = altKm;
                bestFreeG = peakG;
                bestFreeQ = pass.PeakDynamicPressure;
            }
            if (survivable && orbits) corridorOpen = true;
        }

        // No survivable pass shed anything worth pricing (arrival so thin/shallow the air barely bites) —
        // treat as no useful brake here.
        if (double.IsNaN(bestFreeAltKm) || bestFreeShed <= 0)
        {
            return new Quote(Outcome.RefuseMoot, arrivalVinf, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        // The bill, priced at the periapsis speed of the priced pass (the speed a capture burn actually
        // fires at) — the same OrbitRule.PulsesFor kernel the autopilot spends with, matching Lab 32 §D.
        double rPeriPriced = R + bestFreeAltKm * 1000.0;
        double vPeri = Math.Sqrt(arrivalVinf * arrivalVinf + 2 * mu / rPeriPriced);
        double vEscPeri = Vesc(mu, rPeriPriced);
        double entrySpeed = vPeri;
        double captureDeltaV = Math.Max(0, vPeri - vEscPeri);
        double bridge = Math.Max(0.0, captureDeltaV - bestFreeShed);

        int propulsivePulses = OrbitRule.PulsesFor(captureDeltaV, vPeri);
        int aerobrakePulses = bridge > 0 ? OrbitRule.PulsesFor(bridge, vPeri) : 0;
        int saved = propulsivePulses - aerobrakePulses;

        int tighteningPasses = CountTighteningPasses(sim, mu, R, top, start);
        int passesNeeded = 1 + tighteningPasses; // the capture/bridge pass, then the free tightening laps

        Outcome outcome;
        if (corridorOpen && bridge <= 0)
        {
            outcome = Outcome.SoloCapture;
        }
        else if (aerobrakePulses > budgetPulses && budgetPulses > 0)
        {
            outcome = Outcome.RefuseTooHot;     // even the air-assisted bridge outruns the tank
        }
        else if (saved < MinWorthwhileSaved)
        {
            outcome = Outcome.RefuseMoot;       // the haze saves too little to bother — just brake on thrust
        }
        else
        {
            outcome = Outcome.HybridBridge;
        }

        return new Quote(
            outcome, arrivalVinf, entrySpeed, bestFreeShed, captureDeltaV, bridge,
            propulsivePulses, aerobrakePulses, saved, passesNeeded, tighteningPasses,
            bestFreeG, bestFreeQ, vPeri);
    }

    /// <summary>Fly Lab 32 C2's free tightening campaign once bound (a just-captured wide orbit at a gentle
    /// periapsis) and count the passes until the apoapsis is tight enough to hand to a small circularisation
    /// trim. Each next entry is reconstructed from the exit orbit's energy + |h| exactly (the symmetry trick),
    /// so the long coast between passes is skipped, not approximated. Bounded by <see cref="MaxTighteningPasses"/>.</summary>
    private static int CountTighteningPasses(Simulator sim, double mu, double R, double top, double start)
    {
        double campaignPeriKm = Math.Max(50.0, 0.30 * top / 1000.0); // a gentle shallow periapsis
        ShipState ship = BoundArrival(mu, start, R + campaignPeriKm * 1000.0, 60 * R);
        int passes = 0;
        for (int n = 1; n <= MaxTighteningPasses; n++)
        {
            PassResult pass = FlyPass(sim, ship, R, top, mu);
            passes = n;
            double eOut = Energy(pass.Post, mu);
            double apo = Apoapsis(pass.Post, mu);
            if (pass.Crashed || eOut >= 0) break;
            if (apo < 2 * R) break; // tight enough for a small circularisation burn
            double hExit = Math.Abs(pass.Post.Position.X * pass.Post.Velocity.Y - pass.Post.Position.Y * pass.Post.Velocity.X);
            double vNext = Math.Sqrt(Math.Max(0, 2 * (eOut + mu / start)));
            double vtNext = hExit / start;
            double vrNext = -Math.Sqrt(Math.Max(0, vNext * vNext - vtNext * vtNext));
            ship = new ShipState(new Vector2d(start, 0), new Vector2d(vrNext, vtNext), 0);
        }
        return passes;
    }

    // ===== The per-pass hull/heat cost — v1 DETERMINISTIC, with the dice-episode hook left ready (owner Q3) =====

    /// <summary>The deterministic cost a single flown pass charges the ship: whether it holed the sail (peak
    /// g crossed the line), and a normalised hull/heat load off peak-g against that line (the game's damage
    /// currency; peak dynamic pressure is the physical heat proxy a future thermal model would read).</summary>
    /// <param name="PeakG">Peak drag deceleration of the pass, in g.</param>
    /// <param name="HolesSail">True when the pass reached/crossed <see cref="GBudgetG"/> — a sail-holing dip.</param>
    /// <param name="HullLoadFraction">Peak g as a fraction of the 3 g line, clamped to [0, 1].</param>
    /// <param name="PeakDynamicPressurePa">The heat proxy (dynamic pressure) the pass reached.</param>
    public readonly record struct PassCost(double PeakG, bool HolesSail, double HullLoadFraction, double PeakDynamicPressurePa);

    /// <summary>The v1 DETERMINISTIC per-pass cost — a pure function of the flown pass, no randomness. This is
    /// the owner's Q3 default (deterministic currency). The BUSTED-engine house style's dice-scripted episodes
    /// (a torn sail on a bad roll, a heat spike) plug in at <see cref="DiceEpisodeHook"/>, which is null in v1
    /// so the cost stays fully deterministic. Flip the currency by assigning that hook — flagged for the owner.</summary>
    public static PassCost CostOfPass(double peakG, double peakDynamicPressurePa)
    {
        var cost = new PassCost(
            peakG,
            peakG >= GBudgetG,
            Math.Clamp(peakG / GBudgetG, 0.0, 1.0),
            peakDynamicPressurePa);
        return DiceEpisodeHook is { } hook ? hook(cost) : cost;
    }

    /// <summary>THE DICE-EPISODE HOOK (owner's Q3, resolved for dice in #305: "Let's use dice there, I love
    /// it"). It takes the deterministic <see cref="PassCost"/> and returns the rolled one: null = deterministic,
    /// assigned = dice. The SHIPPED implementation is <see cref="AerobrakeEpisodes.Roll"/> — a seeded 2D6
    /// episode table (clean pass, heat spike, g wobble, corridor drama, a torn sail on a bad roll) that also
    /// raises a <see cref="DiceEvent"/> for the on-screen dice tray. The live per-pass execution calls
    /// <see cref="AerobrakeEpisodes.Roll"/> directly (it needs the pass's SEED, which this bare cost→cost seam
    /// cannot carry) and applies its <see cref="AerobrakeEpisodes.Episode.Cost"/>; this hook remains the
    /// low-level manual override for a test or a bespoke currency. Kept out of <see cref="Price"/> so quotes
    /// stay pure — only the flown pass rolls.</summary>
    public static Func<PassCost, PassCost>? DiceEpisodeHook { get; set; }

    // ===== The one voice for the aerobrake's words (HarborVocabulary-style; pure text, unit-tested) =====

    /// <summary>The MAP CONTEXT-MENU action label — the findable affordance, quoting the honest trade.</summary>
    public static string MenuAction(string planetName, Quote quote) => quote.Outcome switch
    {
        Outcome.SoloCapture =>
            $"🪂 Aerobrake at {planetName} — the haze captures you in {quote.PassesNeeded} passes, ≈{quote.PulsesSaved} p saved",
        Outcome.HybridBridge =>
            $"🪂 Aerobrake at {planetName} — the haze pays ≈{quote.PulsesSaved} p (bridge burn ≈{quote.AerobrakePulses} p vs {quote.PropulsivePulses} propulsive)",
        _ => $"🪂 Aerobrake at {planetName}",
    };

    /// <summary>The tooltip / spoken trade, stating passes, Δv shed, and the g taken — or the refusal reason.</summary>
    public static string Trade(string planetName, Quote quote) => quote.Outcome switch
    {
        Outcome.SoloCapture =>
            $"one survivable pass captures at {quote.PeakG:F1} g, then {quote.TighteningPasses} free passes circularise — " +
            $"the haze sheds {quote.FreeShedMps / 1000:F1} km/s for ≈{quote.PulsesSaved} p saved over the propulsive brake",
        Outcome.HybridBridge =>
            $"the corridor is closed at {quote.ArrivalVinf / 1000:F1} km/s v∞, but the haze pays {quote.FreeShedMps / 1000:F1} km/s free " +
            $"on one {quote.PeakG:F1} g pass — you buy only the {quote.BridgeMps / 1000:F1} km/s bridge (≈{quote.AerobrakePulses} p vs {quote.PropulsivePulses} on thrust alone)",
        Outcome.RefuseNoSkimAir => $"{planetName} has no skimmable air — a landing atmosphere or none; brake on thrust",
        Outcome.RefuseMoot => $"you're already slow enough to catch at {planetName} — no brake needed",
        Outcome.RefuseTooHot =>
            $"too hot for the haze at {quote.ArrivalVinf / 1000:F1} km/s v∞ — even air-assisted the bridge wants ≈{quote.AerobrakePulses} p; " +
            "brake on thrust or arrive slower",
        _ => string.Empty,
    };

    /// <summary>The step filed into the flight-plan log when an aerobrake arrival is armed (the third way to
    /// pay the arrival bill, beside <see cref="LongHaul.InsertionStep"/>).</summary>
    public static string ArmStep(string planetName, Quote quote) => quote.CorridorOpen
        ? $"🪂 aerobrake arrival at {planetName} — ride the haze down over {quote.PassesNeeded} passes; the tank keeps ≈{quote.PulsesSaved} p"
        : $"🪂 aerobrake arrival at {planetName} — one hot pass sheds {quote.FreeShedMps / 1000:F1} km/s, then a ≈{quote.AerobrakePulses} p bridge burn caps the capture";

    /// <summary>The refusal spoken when the captain reaches for an aerobrake that cannot be offered.</summary>
    public static string Refusal(string planetName, Quote quote) => Trade(planetName, quote);
}
