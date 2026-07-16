namespace SpaceSails.Core.Tests;

/// <summary>
/// Friday §0 (owner ruling): "armed auto-orbit ends in a KEPT orbit, not an achieved one." These
/// tests pin the station-keeping contract Lab 25 measured and the game ships: keeping engages after
/// a park, trims HOLD the orbit inside tolerance over a long propagation in the real N-body sim, the
/// arm-time quote carries the trim rate, the tank running dry loses the orbit (the loud handback's
/// backstop), and the status surface reads "AUTOPILOT HOLDS THE ORBIT", never "you have the ship".
/// </summary>
public class OrbitKeepingTests
{
    // A compact but faithful Saturn system: the sun and Saturn raise the tide that pumps an Enceladus
    // parking orbit's eccentricity — the same field the lab flew (Lab 17's specs).
    private const double SunMu = 1.32712440018e20;
    private const double SaturnMu = 3.7931187e16;

    private static CircularOrbitEphemeris SaturnSystem() => new(
    [
        new CelestialBody("sun", "Sun", null, SunMu, 6.9634e8, 0, 0, 0),
        new CelestialBody("saturn", "Saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
        new CelestialBody("enceladus", "Enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon),
        new CelestialBody("titan", "Titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
    ]);

    private static Vector2d BodyVel(ICelestialEphemeris field, string id, double t) =>
        (field.Position(id, t + 1.0) - field.Position(id, t - 1.0)) / 2.0;

    private static ShipState CircularParkAt(ICelestialEphemeris field, string moonId, double t0)
    {
        CelestialBody moon = field.Bodies.First(b => b.Id == moonId);
        double hill = OrbitRule.HillRadius(moon, field.Bodies.First(b => b.Id == moon.ParentId).Mu);
        double park = OrbitRule.ParkingRadius(moon, hill);
        double vCirc = Math.Sqrt(moon.Mu / park);
        return new ShipState(field.Position(moonId, t0) + new Vector2d(park, 0), BodyVel(field, moonId, t0) + new Vector2d(0, vCirc), t0);
    }

    // Mirror the game's StationKeep loop honestly: a quarter-period cadence, a park-pinning trim, and a
    // pulse budget that, when it can't cover the next trim, ENDS keeping (the dry-tank handback). Returns
    // whether the orbit was held (never touched the surface, still bound), the pulses spent, and the
    // closest the periapsis came to the surface.
    private static (bool Held, int PulsesSpent, int Trims, double PeriMinR, bool RanDry) FlyKeeping(
        string moonId, double days, int pulseBudget)
    {
        CircularOrbitEphemeris field = SaturnSystem();
        var sim = new Simulator(field, timeStepSeconds: 60);
        CelestialBody moon = field.Bodies.First(b => b.Id == moonId);
        CelestialBody parent = field.Bodies.First(b => b.Id == moon.ParentId);
        double hill = OrbitRule.HillRadius(moon, parent.Mu);
        double park = OrbitRule.ParkingRadius(moon, hill);
        double period = OrbitRule.LocalOrbitPeriod(park, moon.Mu);
        double stride = period / 64.0;
        double nextCheck = OrbitKeeping.TrimCadenceFraction * period;
        int strides = (int)(days * 86400.0 / stride);

        ShipState ship = CircularParkAt(field, moonId, 0.0);
        int spent = 0, trims = 0;
        double periMinR = double.MaxValue;
        bool held = true, ranDry = false;
        for (int i = 0; i < strides; i++)
        {
            Vector2d mPos = field.Position(moonId, ship.SimTime);
            Vector2d mVel = BodyVel(field, moonId, ship.SimTime);
            OrbitKeeping.Elements el = OrbitKeeping.OrbitElements(ship, mPos, mVel, moon);
            if (el.Bound)
            {
                periMinR = Math.Min(periMinR, el.SemiMajorAxis * (1 - el.Eccentricity) / moon.BodyRadius);
            }
            if ((ship.Position - mPos).Length < moon.BodyRadius) { held = false; break; }
            if (ship.SimTime >= nextCheck)
            {
                nextCheck += OrbitKeeping.TrimCadenceFraction * period;
                if (OrbitKeeping.NeedsTrim(ship, mPos, mVel, moon))
                {
                    int cost = OrbitKeeping.TrimPulseCost(ship, mPos, mVel, moon, park);
                    if (cost > pulseBudget - spent) { ranDry = true; continue; } // dry-tank: stop trimming
                    ship = OrbitKeeping.Trim(ship, mPos, mVel, moon, park);
                    spent += cost;
                    trims++;
                }
            }
            ship = sim.RunAdaptive(ship, stride, maxTimeStep: stride);
        }
        return (held, spent, trims, periMinR, ranDry);
    }

    // ---- the trim primitive -------------------------------------------------------------------

    [Fact]
    public void NeedsTrim_FreshCircularPark_IsFalse_ThenTrueOncePumped()
    {
        CircularOrbitEphemeris field = SaturnSystem();
        CelestialBody moon = field.Bodies.First(b => b.Id == "titan");
        ShipState fresh = CircularParkAt(field, "titan", 0.0);
        Vector2d mPos = field.Position("titan", 0), mVel = BodyVel(field, "titan", 0);

        Assert.False(OrbitKeeping.NeedsTrim(fresh, mPos, mVel, moon)); // e≈0 at a clean park

        // Pump the eccentricity by a radial kick well past the tolerance.
        var pumped = fresh with { Velocity = fresh.Velocity + (fresh.Position - mPos).Normalized() * 200 };
        Assert.True(OrbitKeeping.NeedsTrim(pumped, mPos, mVel, moon));
    }

    [Fact]
    public void Trim_PinsTheSemiMajorAxisToThePark_NotTheCurrentRadius()
    {
        CircularOrbitEphemeris field = SaturnSystem();
        CelestialBody moon = field.Bodies.First(b => b.Id == "titan");
        double hill = OrbitRule.HillRadius(moon, SaturnMu);
        double park = OrbitRule.ParkingRadius(moon, hill);
        Vector2d mPos = field.Position("titan", 0), mVel = BodyVel(field, "titan", 0);

        // Ship drifted OUT to 1.15× the park radius with a pumped, radial-laden velocity.
        double r = 1.15 * park;
        double vc = Math.Sqrt(moon.Mu / r);
        var drifted = new ShipState(mPos + new Vector2d(r, 0), mVel + new Vector2d(80, 1.1 * vc), 0);

        ShipState trimmed = OrbitKeeping.Trim(drifted, mPos, mVel, moon, park);
        OrbitKeeping.Elements el = OrbitKeeping.OrbitElements(trimmed, mPos, mVel, moon);

        Assert.True(el.Bound);
        // The energy is pinned to the PARK orbit, so a ≈ park despite trimming at 1.15× park radius —
        // the fix for the deep-well semi-major-axis walk that crashed the naive re-circularize (Lab 25).
        Assert.Equal(park, el.SemiMajorAxis, park * 0.02);
    }

    // ---- keeping HOLDS over a long propagation --------------------------------------------------

    [Fact]
    public void Keeping_HoldsTheEnceladusPark_OverManyDays()
    {
        // Enceladus is the hard case: its 0.33-Hill park is 1.24 R and the tide crashes an UNKEPT
        // orbit within half a day. Keeping must hold it — bound, never touching the surface — for days.
        var kept = FlyKeeping("enceladus", days: 6, pulseBudget: int.MaxValue);
        Assert.True(kept.Held, "keeping should hold the Enceladus orbit off the surface");
        Assert.True(kept.Trims > 0, "keeping must actually be firing trims to hold this park");
        // The osculating periapsis dips toward the surface (Lab 25 measured ≈0.86 R) but the real
        // trajectory — Held above — never touches it; the tide never lets the extrapolation complete.
        Assert.True(kept.PeriMinR > 0.8, $"the kept orbit did not collapse to the ground (osc. peri min {kept.PeriMinR:F2} R)");
    }

    [Fact]
    public void Keeping_HoldsCheaply_AtRoomyTitan()
    {
        // Titan has a stable core — keeping only nips the secular drift, so it is cheap and safe.
        var kept = FlyKeeping("titan", days: 12, pulseBudget: int.MaxValue);
        Assert.True(kept.Held);
        Assert.True(kept.PeriMinR > 3.0, $"Titan's park sits well clear of the surface (min {kept.PeriMinR:F2} R)");
    }

    [Fact]
    public void Keeping_UnkeptEnceladusOrbit_CrashesWithoutTrims()
    {
        // The contrast that makes the whole lane necessary: with NO trim budget the same park is lost.
        var starved = FlyKeeping("enceladus", days: 6, pulseBudget: 0);
        Assert.False(starved.Held, "an unkept Enceladus park must crash — that is the owner's stranded ship");
        Assert.True(starved.RanDry, "the loop should have wanted to trim but had no pulses (the dry-tank handback)");
    }

    // ---- the arm-time quote ---------------------------------------------------------------------

    [Fact]
    public void TrimPulsesPerDay_QuotesAPositiveRate_ForAMeasuredMoon()
    {
        CircularOrbitEphemeris field = SaturnSystem();
        CelestialBody enc = field.Bodies.First(b => b.Id == "enceladus");
        double hill = OrbitRule.HillRadius(enc, SaturnMu);
        double world = BodyVel(field, "enceladus", 0).Length;

        int pDay = OrbitKeepingTable.TrimPulsesPerDay(enc, hill, SaturnMu, enc.OrbitRadius, world);
        Assert.True(pDay > 0, "the arm-time quote must carry a real trim rate for a kept moon");

        // Enceladus's deep, tide-battered park is the expensive one — dearer than roomy Titan's.
        CelestialBody titan = field.Bodies.First(b => b.Id == "titan");
        double tHill = OrbitRule.HillRadius(titan, SaturnMu);
        int tDay = OrbitKeepingTable.TrimPulsesPerDay(titan, tHill, SaturnMu, titan.OrbitRadius, BodyVel(field, "titan", 0).Length);
        Assert.True(pDay > tDay, "Enceladus must quote a higher trim budget than Titan (Lab 25)");
    }

    [Fact]
    public void OrbitKeepingTable_ReturnsMeasuredForShippedMoons_EstimateOtherwise()
    {
        CircularOrbitEphemeris field = SaturnSystem();
        CelestialBody enc = field.Bodies.First(b => b.Id == "enceladus");
        double hill = OrbitRule.HillRadius(enc, SaturnMu);

        Assert.True(OrbitKeepingTable.ByBody.ContainsKey("enceladus"));
        OrbitKeeping.KeepProfile measured = OrbitKeepingTable.For(enc, hill, SaturnMu, enc.OrbitRadius);
        Assert.Equal("enceladus", measured.BodyId);
        Assert.True(measured.TrimDvPerDay > 0 && measured.TrimsPerDay > 0);

        // A moon the lab never measured falls back to the physics estimate — never a silent zero.
        var unknown = new CelestialBody("mimas", "Mimas", "saturn", 2.5e9, 1.98e5, 1.855e8, 8.14e4, 0, BodyKind.Moon);
        double mHill = OrbitRule.HillRadius(unknown, SaturnMu);
        OrbitKeeping.KeepProfile est = OrbitKeepingTable.For(unknown, mHill, SaturnMu, unknown.OrbitRadius);
        Assert.Equal("mimas", est.BodyId);
        Assert.True(est.TrimDvPerDay > 0);
    }

    // ---- the status surface: HOLDS THE ORBIT, never "you have the ship" ------------------------

    // The verbatim kept-orbit NOW line the game composes (Map.razor) and feeds through main's #190
    // HoldingLine seam — one code path in the builder.
    private const string HoldsLine = "🛰 AUTOPILOT HOLDS THE ORBIT — Enceladus, 313 km, trim ≈27 p/day";

    [Fact]
    public void FlightPlanStatus_HoldingLine_RendersVerbatimAsTheNowRow()
    {
        var status = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: false, AutopilotBodyName: "Enceladus",
            NextStepLabel: null, NextStepEta: null,
            HoldingLine: HoldsLine));

        Assert.Equal(HoldsLine, status.NowLine);           // verbatim, one code path
        Assert.Equal(HoldsLine, status.Rows[0].Text);      // and it is the pinned NOW row
        Assert.Contains("trim ≈27 p/day", status.NowLine);
        Assert.DoesNotContain("YOU HAVE THE SHIP", status.NowLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FlightPlanStatus_Holding_OutranksArmed_ButDockOutranksHolding()
    {
        // The holding line beats every flying phase (even the inserting/approach line)...
        var holding = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: true, AutopilotBodyName: "Enceladus",
            NextStepLabel: null, NextStepEta: null,
            AutopilotInserting: true, HoldingLine: HoldsLine));
        Assert.Equal(HoldsLine, holding.NowLine);
        Assert.DoesNotContain("approach", holding.NowLine);
        Assert.DoesNotContain("inserting", holding.NowLine);

        // ...but a dock wins (the ship is clamped on; keeping is over).
        var docked = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: true, DockedHavenName: "Ringside",
            AutopilotArmed: true, AutopilotFlyingApproach: false, AutopilotBodyName: "Enceladus",
            NextStepLabel: null, NextStepEta: null,
            HoldingLine: HoldsLine));
        Assert.Contains("docked at Ringside", docked.NowLine);
        Assert.DoesNotContain("HOLDS THE ORBIT", docked.NowLine);
    }

    [Fact]
    public void FlightPlanStatus_AfterDryTankHandback_ReadsManual_NotHolding()
    {
        // The dry-tank handback clears keeping (HoldingLine null) and sets the persistent reason: the
        // NOW line goes manual, and the #180 degradation alert takes over as the backstop.
        var status = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: null, NextStepEta: null,
            HandbackReason: "TANK DRY at Enceladus — the orbit will now decay",
            HoldingLine: null));

        Assert.Contains("TANK DRY at Enceladus", status.NowLine);
        Assert.DoesNotContain("HOLDS THE ORBIT", status.NowLine);
    }
}
