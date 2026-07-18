namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for #290 — 🪂 Aerobrake as a findable, priced arrival option. Pins the honest trade the
/// context-menu affordance quotes: for the owner's 29.8 km/s / 32-pulse Uranus stranding the pure-propulsive
/// capture is impossible (over 32) yet the haze pays it down inside the tank; a slow arrival reopens a
/// near-free solo corridor; a hypersonic arrival is refused as too hot; landing/airless worlds are refused;
/// and the per-pass hull/heat cost is deterministic with the dice-episode hook (owner's open Q3) ready.
/// Every priced number is measured by flying real Core drag passes (<see cref="Simulator.RunAdaptiveWithDrag"/>).
/// </summary>
public class AerobrakeTests
{
    // Uranus params MIRROR scenarios/sol.json; the shell is #290's shipped proposal (added to sol.json).
    private static CelestialBody Uranus(Atmosphere? atm) =>
        new("uranus", "Uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4, Atmosphere: atm);

    private static Atmosphere UranusAtm => new(RefDensity: 1.4e-5, ScaleHeight: 1.2e5, TopAltitude: 1.0e6);
    private static Atmosphere TitanAtm => new(RefDensity: 5.3, ScaleHeight: 4.0e4, TopAltitude: 3.0e5);

    private const double StrandVinf = 29800.0; // the owner's relative speed at the Uranus stranding
    private const int Tank = 32;               // the owner's tank

    [Fact]
    public void TheStranding_PropulsiveIsImpossible_ButTheHazePaysItInsideTheTank()
    {
        // The owner's headline (Lab 32 §D): 42 propulsive (over 32 = impossible), the haze pays ~11 pulses
        // down to a payable bridge that fits the tank. The corridor is CLOSED at 29.8 km/s — a hybrid, not solo.
        Aerobrake.Quote q = Aerobrake.Price(Uranus(UranusAtm), StrandVinf, Tank);

        Assert.Equal(Aerobrake.Outcome.HybridBridge, q.Outcome);
        Assert.True(q.Offered, "the aerobrake is offered — it saves real pulses");
        Assert.False(q.CorridorOpen, "29.8 km/s is above the corridor's close — no solo capture");
        Assert.True(q.PropulsivePulses > Tank, $"pure-propulsive capture ({q.PropulsivePulses} p) exceeds the 32-pulse tank");
        Assert.True(q.AerobrakePulses <= Tank, $"the air-assisted bridge ({q.AerobrakePulses} p) fits the tank");
        Assert.True(q.PulsesSaved >= 8, $"the haze pays a material {q.PulsesSaved} p of the bill");
        Assert.True(q.FreeShedMps > 3000, $"the free 3 g pass sheds a real chunk ({q.FreeShedMps:F0} m/s)");
        Assert.True(q.PeakG <= Aerobrake.GBudgetG, "the priced free pass stays under the 3 g hull line");
        Assert.True(q.PassesNeeded >= 2, "a capture pass plus free tightening passes");
    }

    [Fact]
    public void ASlowArrival_ReopensANearFreeSoloCorridor()
    {
        // Lab 32 §E: at a typical ~6 km/s Uranus arrival the corridor is OPEN — one survivable pass captures
        // and free passes circularise, no bridge burn owed. Speed, not aim, is the gate.
        Aerobrake.Quote q = Aerobrake.Price(Uranus(UranusAtm), 6000, Tank);

        Assert.Equal(Aerobrake.Outcome.SoloCapture, q.Outcome);
        Assert.True(q.CorridorOpen, "a 6 km/s arrival has an open single-pass corridor");
        Assert.Equal(0.0, q.BridgeMps);
        Assert.Equal(0, q.AerobrakePulses);
        Assert.True(q.PulsesSaved >= Aerobrake.MinWorthwhileSaved, "the solo capture saves the whole propulsive bill");
    }

    [Fact]
    public void AHypersonicArrival_IsRefusedTooHot_TheHazeCannotSaveIt()
    {
        // Far past the corridor's close, even the air-assisted bridge outruns a normal tank — the honest
        // refusal-with-reason the affordance must speak (not a silent failure).
        Aerobrake.Quote q = Aerobrake.Price(Uranus(UranusAtm), 60000, Tank);

        Assert.Equal(Aerobrake.Outcome.RefuseTooHot, q.Outcome);
        Assert.False(q.Offered);
        Assert.True(q.AerobrakePulses > Tank, "the bridge burn is unaffordable even with the haze's help");
        Assert.Contains("too hot", Aerobrake.Trade("Uranus", q));
    }

    [Fact]
    public void LandingAndAirlessWorlds_AreRefusedNoSkimAir()
    {
        // Titan/Venus are landing atmospheres (Lab 32 §E) and an airless world has nothing to skim — the
        // affordance must not offer at any of them.
        Assert.Equal(Aerobrake.Outcome.RefuseNoSkimAir, Aerobrake.Price(Uranus(TitanAtm), StrandVinf, Tank).Outcome);
        Assert.Equal(Aerobrake.Outcome.RefuseNoSkimAir, Aerobrake.Price(Uranus(atm: null), StrandVinf, Tank).Outcome);
    }

    [Fact]
    public void AnAlreadySlowArrival_IsMoot()
    {
        // Below the near-parabolic floor there is nothing to brake — moot (NOT the 8 km/s dock clamp: a
        // 6 km/s v∞ arrival above still wants braking, proven by the solo-corridor test).
        Aerobrake.Quote q = Aerobrake.Price(Uranus(UranusAtm), 100, Tank);
        Assert.Equal(Aerobrake.Outcome.RefuseMoot, q.Outcome);
        Assert.False(q.Offered);
    }

    [Fact]
    public void IsSkimAtmosphere_SplitsSkimWorldsFromLandingWorlds()
    {
        Assert.True(Aerobrake.IsSkimAtmosphere(UranusAtm));                       // ice giant — skims
        Assert.True(Aerobrake.IsSkimAtmosphere(new Atmosphere(1.2, 8.0e3, 1.4e5)));  // Earth — skims
        Assert.True(Aerobrake.IsSkimAtmosphere(new Atmosphere(0.02, 1.1e4, 1.2e5))); // Mars — skims
        Assert.True(Aerobrake.IsSkimAtmosphere(new Atmosphere(4.0e-6, 3.0e4, 4.0e5))); // Jupiter — skims
        Assert.False(Aerobrake.IsSkimAtmosphere(TitanAtm));                       // Titan — lands
        Assert.False(Aerobrake.IsSkimAtmosphere(new Atmosphere(65.0, 1.6e4, 1.5e5))); // Venus — lands
        Assert.False(Aerobrake.IsSkimAtmosphere(atmosphere: null));              // airless
    }

    [Fact]
    public void TheQuoteIsDeterministic_SameInputsSameQuote()
    {
        Aerobrake.Quote a = Aerobrake.Price(Uranus(UranusAtm), StrandVinf, Tank);
        Aerobrake.Quote b = Aerobrake.Price(Uranus(UranusAtm), StrandVinf, Tank);
        Assert.Equal(a, b); // readonly-record-struct equality: bit-identical
    }

    [Fact]
    public void PerPassCost_IsDeterministic_AndHolesTheSailOverTheGLine()
    {
        // v1 currency: a pure function of the flown pass. Under the line, a partial hull load; at/over it,
        // the sail holes (the same disable the gun inflicts).
        Aerobrake.PassCost gentle = Aerobrake.CostOfPass(peakG: 2.0, peakDynamicPressurePa: 1500);
        Assert.False(gentle.HolesSail);
        Assert.Equal(2.0 / Aerobrake.GBudgetG, gentle.HullLoadFraction, 6);
        Assert.Equal(1500, gentle.PeakDynamicPressurePa);

        Aerobrake.PassCost overG = Aerobrake.CostOfPass(peakG: 3.6, peakDynamicPressurePa: 5000);
        Assert.True(overG.HolesSail);
        Assert.Equal(1.0, overG.HullLoadFraction); // clamped

        Assert.Equal(gentle, Aerobrake.CostOfPass(2.0, 1500)); // deterministic run-to-run
    }

    [Fact]
    public void DiceEpisodeHook_PlugsIn_ForTheOwnersOpenQ3_AndIsNullByDefault()
    {
        // v1 ships deterministic (hook null). The BUSTED-engine dice-scripted episodes plug in at the one
        // named seam — assigning it lets a roll escalate the per-pass cost. Restored so no test bleed.
        Assert.Null(Aerobrake.DiceEpisodeHook);
        try
        {
            Aerobrake.DiceEpisodeHook = cost => cost with { HolesSail = true }; // a "bad roll tears the sail"
            Aerobrake.PassCost rolled = Aerobrake.CostOfPass(peakG: 1.0, peakDynamicPressurePa: 800);
            Assert.True(rolled.HolesSail, "the dice hook overrode the deterministic (would-be safe) cost");
        }
        finally
        {
            Aerobrake.DiceEpisodeHook = null;
        }
        Assert.Null(Aerobrake.DiceEpisodeHook);
    }

    [Fact]
    public void SolJson_NowCarriesTheSkimShells_ForTheGiantsEarthAndMars()
    {
        // #290 ships Lab 32's proposed ice-giant + Mars shells so the aerobrake has air to brake against.
        var scenario = ScenarioLoader.LoadFile(
            System.IO.Path.Combine(System.AppContext.BaseDirectory, "scenarios", "sol.json"));
        var eph = CircularOrbitEphemeris.FromScenario(scenario);

        foreach (string id in new[] { "uranus", "neptune", "mars", "jupiter", "saturn", "earth" })
        {
            CelestialBody body = System.Linq.Enumerable.First(eph.Bodies, b => b.Id == id);
            Assert.True(Aerobrake.IsSkimAtmosphere(body.Atmosphere), $"{id} carries a skim atmosphere for aerobrake");
        }
    }
}
