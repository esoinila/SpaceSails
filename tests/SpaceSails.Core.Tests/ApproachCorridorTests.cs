namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for the safe-approach corridor (Lab 29 — The harbor pattern, issues #160/#175/#180).
/// See labs/29-the-harbor-pattern/README.md and Probe.cs for the measured tables behind these numbers.
/// The corridor is derived entirely from the SAME Core constants the autopilot flies with
/// (<see cref="DockRule"/>, <see cref="OrbitRule"/>), so every gate here is a machinery threshold, not
/// a parallel model. The two harbors are the scenario's own: Ringside Exchange (μ=0 station clamp) and
/// Enceladus (μ&gt;0 moon park).
/// </summary>
public class ApproachCorridorTests
{
    private const double SaturnMu = 3.7931187e16;

    private static CelestialBody Ringside() =>
        new("ringside-exchange", "ringside-exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station);

    private static CelestialBody Enceladus() =>
        new("enceladus", "enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon);

    // ---- construction: the gates come straight from DockRule / OrbitRule ----

    [Fact]
    public void G1_Station_DoorAndCap_AreTheDockRuleEnvelope()
    {
        var c = ApproachCorridor.For(Ringside(), SaturnMu);

        Assert.Equal(CorridorClass.StationClamp, c.Class);
        Assert.Equal(DockRule.EnvelopeMeters, c.DoorRangeMeters);        // 500,000 km
        Assert.Equal(DockRule.MatchSpeed, c.DoorSpeedMps);               // 8 km/s shear cap
        Assert.Equal(OrbitRule.ApproachClosingSpeed(Ringside(), 0), c.TerminalCloseMps); // 4 km/s
        Assert.Equal(4000.0, c.TerminalCloseMps, 6);
        // τ = door / close = 5e8 / 4000 = 125,000 s
        Assert.Equal(125_000.0, c.GlideslopeSeconds, 6);
    }

    [Fact]
    public void G2_Moon_DoorIsTheHillSphere_CapIsTheInsertionWindow()
    {
        CelestialBody enc = Enceladus();
        double hill = OrbitRule.HillRadius(enc, SaturnMu);
        var c = ApproachCorridor.For(enc, SaturnMu);

        Assert.Equal(CorridorClass.MoonPark, c.Class);
        Assert.Equal(hill, c.DoorRangeMeters);
        Assert.Equal(OrbitRule.MaxRelativeSpeed, c.DoorSpeedMps);        // 5 km/s window limit
        Assert.Equal(OrbitRule.ApproachClosingSpeed(enc, hill), c.TerminalCloseMps);
        Assert.Equal(OrbitRule.ParkingRadius(enc, hill), c.BerthRangeMeters);
        // Enceladus' Hill sphere is under 1,000 km — the whole reason the moon door is unforgiving.
        Assert.True(hill < 1e6, $"Enceladus Hill sphere should be under 1,000 km; got {hill / 1e3:F0} km");
    }

    [Fact]
    public void G3_For_DispatchesOnMu()
    {
        Assert.Equal(CorridorClass.StationClamp, ApproachCorridor.For(Ringside(), SaturnMu).Class);
        Assert.Equal(CorridorClass.MoonPark, ApproachCorridor.For(Enceladus(), SaturnMu).Class);
    }

    // ---- the glideslope: range / τ, capped at the door speed ----

    [Fact]
    public void G4_MaxSpeedAt_IsCappedFarOut_AndEqualsTerminalCloseAtTheDoor()
    {
        var c = ApproachCorridor.For(Ringside(), SaturnMu);

        // Far outside the knee (R/τ > cap) → flat at the 8 km/s shear cap.
        Assert.Equal(DockRule.MatchSpeed, c.MaxSpeedAt(3e9), 6);
        // Exactly at the door the glideslope equals the autopilot's terminal closing speed.
        Assert.Equal(c.TerminalCloseMps, c.MaxSpeedAt(c.DoorRangeMeters), 6);
        // Halfway in to the door, half the speed — a true constant-time-to-go line.
        Assert.Equal(c.TerminalCloseMps / 2, c.MaxSpeedAt(c.DoorRangeMeters / 2), 6);
    }

    // ---- Read: on-pattern / hot / missed at the boundaries ----

    [Fact]
    public void G5_Station_OnPattern_AtAndUnderTheGlideslope()
    {
        var c = ApproachCorridor.For(Ringside(), SaturnMu);

        // Exactly on the clamp-window gate: on pattern, zero margin.
        CorridorReading atGate = c.Read(DockRule.EnvelopeMeters, c.TerminalCloseMps);
        Assert.Equal(CorridorVerdict.OnPattern, atGate.Verdict);
        Assert.Equal(0.0, atGate.MarginMps, 6);

        // Under the ceiling far out: on pattern with positive margin.
        Assert.Equal(CorridorVerdict.OnPattern, c.Read(2e9, 6000).Verdict);
    }

    [Fact]
    public void G6_Station_Hot_InsideTheBubbleOverPatternButUnderShearCap()
    {
        var c = ApproachCorridor.For(Ringside(), SaturnMu);

        // Inside the bubble at 7 km/s: over the 4 km/s glideslope, still under the 8 km/s shear cap.
        CorridorReading hot = c.Read(DockRule.EnvelopeMeters, 7000);
        Assert.Equal(CorridorVerdict.Hot, hot.Verdict);
        Assert.True(hot.MarginMps < 0);
    }

    [Fact]
    public void G7_Station_Missed_InsideTheBubbleOverTheShearCap()
    {
        var c = ApproachCorridor.For(Ringside(), SaturnMu);

        // Inside the bubble, over the 8 km/s clamp shear cap → too hot to grab, overshooting (#175).
        Assert.Equal(CorridorVerdict.Missed, c.Read(3e8, 9000).Verdict);
        // Exactly at the cap is still recoverable (Hot), not yet Missed.
        Assert.Equal(CorridorVerdict.Hot, c.Read(3e8, DockRule.MatchSpeed).Verdict);
    }

    [Fact]
    public void G8_FarOutAndFast_IsHotNotMissed_ThereIsStillRoomToSlow()
    {
        var c = ApproachCorridor.For(Ringside(), SaturnMu);

        // Way outside the door and screaming: over pattern, but Missed is reserved for INSIDE the door.
        Assert.Equal(CorridorVerdict.Hot, c.Read(2e9, 20000).Verdict);
    }

    [Fact]
    public void G9_Moon_Verdicts_AtTheTightDoor()
    {
        CelestialBody enc = Enceladus();
        double hill = OrbitRule.HillRadius(enc, SaturnMu);
        var c = ApproachCorridor.For(enc, SaturnMu);

        // On the terminal-close line at the Hill sphere → on pattern.
        Assert.Equal(CorridorVerdict.OnPattern, c.Read(hill, c.TerminalCloseMps).Verdict);
        // Inside Hill, over pattern but under the 5 km/s window → hot (courts the overshoot).
        Assert.Equal(CorridorVerdict.Hot, c.Read(hill / 2, 3000).Verdict);
        // Inside Hill, over the 5 km/s insertion-window cap → missed (blows through, #180 "by luck").
        Assert.Equal(CorridorVerdict.Missed, c.Read(hill / 2, 6000).Verdict);
    }

    // ---- NextGate: the outermost gate still ahead ----

    [Fact]
    public void G10_NextGate_IsTheNextOneCrossedGoingInward()
    {
        var c = ApproachCorridor.For(Ringside(), SaturnMu);

        // Between handover and the clamp window → next is the clamp window.
        Assert.Equal("clamp window", c.NextGate(2e9).Name);
        // Farther out than the handover gate → next is the handover.
        Assert.Equal("handover", c.NextGate(5e9).Name);

        var m = ApproachCorridor.For(Enceladus(), SaturnMu);
        double hill = OrbitRule.HillRadius(Enceladus(), SaturnMu);
        // Just outside the Hill sphere → the Hill sphere is the next gate.
        Assert.Equal("Hill sphere", m.NextGate(hill * 1.5).Name);
        // Inside the Hill sphere → the park is next.
        Assert.Equal("park", m.NextGate(hill * 0.5).Name);
    }

    [Fact]
    public void G11_Gates_AreDescendingByRange_AndReadReturnsThem()
    {
        foreach (var c in new[] { ApproachCorridor.For(Ringside(), SaturnMu), ApproachCorridor.For(Enceladus(), SaturnMu) })
        {
            for (int i = 1; i < c.Gates.Count; i++)
            {
                Assert.True(c.Gates[i].RangeMeters < c.Gates[i - 1].RangeMeters,
                    "gates must be listed outermost-first (descending range)");
            }
            // Read hands back the same next-gate NextGate computes.
            CorridorReading r = c.Read(c.DoorRangeMeters * 1.1, c.TerminalCloseMps);
            Assert.Equal(c.NextGate(c.DoorRangeMeters * 1.1), r.NextGate);
        }
    }
}
