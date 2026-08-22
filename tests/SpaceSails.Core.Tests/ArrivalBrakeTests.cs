namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for #304 — 🛬 THE ARRIVAL BRAKE ASKS. Owner ruling (2026-07-18): "let's have it ask, it is
/// hard to remember in the heat of the moment otherwise." The pure timing law lives in
/// <see cref="ArrivalBrake"/> so it unit-tests without a browser: ask at window-open, HOLD means held
/// (#962), fire exactly once (the double-fire / double-bill guard), decline is stateless, and the fire math
/// sheds to the clamp window (or pro-rata on a short tank). Plus the window predicate — a clamped ship is
/// never arriving (#962) — the worthless-aerobrake gate, and the in-voice wording.
/// </summary>
public class ArrivalBrakeTests
{
    // ===== The timing law: ArrivalBrake.Advance / Snooze / Fire =====

    [Fact]
    public void Advance_RaisesTheAsk_TheFrameTheWindowOpens()
    {
        // A dormant gate with the window open → the ask is raised (never silently skipped).
        ArrivalBrake.Gate gate = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, windowOpen: true);
        Assert.True(gate.Asking);
        Assert.False(gate.HasFired);
    }

    [Fact]
    public void Advance_WithNoWindow_StaysClosed()
    {
        // No brake owed → nothing raised. (And a spent gate resets when the window shuts — see below.)
        ArrivalBrake.Gate gate = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, windowOpen: false);
        Assert.False(gate.Asking);
        Assert.Equal(ArrivalBrake.Phase.Dormant, gate.State);
    }

    [Fact]
    public void Advance_HoldsTheAskUp_WhileTheWindowRemains()
    {
        // An open ask is not re-created or dismissed frame to frame — it stays up awaiting the captain.
        ArrivalBrake.Gate asking = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true);
        ArrivalBrake.Gate next = ArrivalBrake.Advance(asking, windowOpen: true);
        Assert.True(next.Asking);
    }

    [Fact]
    public void Hold_HidesTheAsk_AndNeverReRaisesIt_ForThisArrival()
    {
        // #962 REGRESSION. Owner, of his own screenshot: "the jupiter brake re-appears after I click I'll
        // fly by hand." Hold is an ANSWER, not a snooze. Nothing fires, and no amount of frames with the
        // window still open brings the card back.
        ArrivalBrake.Gate asking = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true);
        ArrivalBrake.Gate held = ArrivalBrake.Hold(asking);

        Assert.False(held.Asking);
        Assert.False(held.HasFired);
        Assert.True(held.IsHeld);
        Assert.Equal(ArrivalBrake.Phase.Held, held.State);

        // Ten thousand frames later, the window still open: still held, still silent.
        ArrivalBrake.Gate gate = held;
        for (int frame = 0; frame < 10_000; frame++)
        {
            gate = ArrivalBrake.Advance(gate, windowOpen: true);
            Assert.False(gate.Asking);
        }

        Assert.True(gate.IsHeld);
    }

    [Fact]
    public void Hold_ThenTheWindowShuts_LetsTheNEXTArrivalAskAfresh()
    {
        // Held is terminal for THIS arrival only: the window shutting (braked, clamped on, wandered clear)
        // resets the gate, and the next hot arrival gets its question.
        ArrivalBrake.Gate held = ArrivalBrake.Hold(ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true));

        ArrivalBrake.Gate closed = ArrivalBrake.Advance(held, windowOpen: false);
        Assert.Equal(ArrivalBrake.Gate.Closed, closed);

        ArrivalBrake.Gate nextArrival = ArrivalBrake.Advance(closed, windowOpen: true);
        Assert.True(nextArrival.Asking);
    }

    [Fact]
    public void Fire_MarksFiredOnce_AndIsIdempotent_NoDoubleFire()
    {
        ArrivalBrake.Gate asking = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true);

        ArrivalBrake.Gate fired = ArrivalBrake.Fire(asking);
        Assert.True(fired.HasFired);

        // A second consent (double-click, re-entrant frame) is a no-op — the once-guard holds.
        ArrivalBrake.Gate again = ArrivalBrake.Fire(fired);
        Assert.Equal(fired, again);
        Assert.True(again.HasFired);
    }

    [Fact]
    public void Advance_KeepsAFiredGateFired_WhileTheWindowLingers_ThenResetsWhenItShuts()
    {
        ArrivalBrake.Gate fired = ArrivalBrake.Fire(ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true));

        // While the window is (briefly) still open post-fire, the gate stays Fired — it never re-asks.
        ArrivalBrake.Gate lingering = ArrivalBrake.Advance(fired, windowOpen: true);
        Assert.True(lingering.HasFired);
        Assert.False(lingering.Asking);

        // Once the speed is shed and the window shuts, the gate resets for any future arrival.
        ArrivalBrake.Gate shut = ArrivalBrake.Advance(fired, windowOpen: false);
        Assert.Equal(ArrivalBrake.Gate.Closed, shut);
    }

    // ===== #962 · The WINDOW: a clamped ship is not arriving =====

    // The Red Eye's geometry, as the owner met it: a berth well inside Jupiter's Hill sphere (5.3e10 m),
    // 3 km off the station, riding the station's own rail — which is fast relative to JUPITER.
    private const double RedEyeDistanceFromJupiter = 7.0e8;
    private const double JupiterHillRadius = 5.3e10;
    private const double BerthSpeedRelativeToJupiter = 13_000.0; // a Jovian orbit is hot by clamp-window standards

    [Fact]
    public void WindowOpen_WhileClamped_IsShut_EvenInsideTheHillSphereAtOrbitalSpeed()
    {
        // #962 REGRESSION. Owner, over a screenshot of the ship clamped at The Red Eye with a Jupiter
        // aerobrake card up: "This pop-up still shows when we are docked?" A berth is not an arrival — the
        // station holds her, and the burn the card offers would do nothing but fight the clamp.
        Assert.False(ArrivalBrake.WindowOpen(
            clamped: true, crossingTheVoid: false,
            distance: RedEyeDistanceFromJupiter, vicinityRadius: JupiterHillRadius,
            relativeSpeed: BerthSpeedRelativeToJupiter, clampWindowSpeed: LongHaul.InsertionTargetSpeed));
    }

    [Fact]
    public void WindowOpen_SameGeometryButFlying_IsOpen()
    {
        // The guard is the CLAMP and nothing else — cast off in the exact same place, at the exact same
        // speed, and the brake is genuinely owed again. (Prove the clamp test above can fail.)
        Assert.True(ArrivalBrake.WindowOpen(
            clamped: false, crossingTheVoid: false,
            distance: RedEyeDistanceFromJupiter, vicinityRadius: JupiterHillRadius,
            relativeSpeed: BerthSpeedRelativeToJupiter, clampWindowSpeed: LongHaul.InsertionTargetSpeed));
    }

    [Fact]
    public void WindowOpen_MidJump_OrFarOut_OrAlreadySlow_IsShut()
    {
        // Crossing the void: the arrival world is not there yet.
        Assert.False(ArrivalBrake.WindowOpen(false, true, 1.0, JupiterHillRadius, 30_000, LongHaul.InsertionTargetSpeed));

        // Outside the vicinity: nothing to insert into.
        Assert.False(ArrivalBrake.WindowOpen(false, false, JupiterHillRadius * 2, JupiterHillRadius, 30_000, LongHaul.InsertionTargetSpeed));

        // Already inside the clamp window: the brake is not owed, so it is not asked for.
        Assert.False(ArrivalBrake.WindowOpen(
            false, false, RedEyeDistanceFromJupiter, JupiterHillRadius,
            LongHaul.InsertionTargetSpeed, LongHaul.InsertionTargetSpeed));
    }

    // ===== #962 · An aerobrake that saves nothing is not an offer =====

    [Fact]
    public void AerobrakeWorthOffering_RefusesTheZeroPassZeroSavedNonOffer()
    {
        // #962 REGRESSION, straight off the owner's screenshot: "the aerobrake commits the ship to 0 passes
        // (≈0 p saved) — commit the pass?" That is a form to sign in exchange for nothing.
        Assert.False(ArrivalBrake.AerobrakeWorthOffering(passes: 0, pulsesSaved: 0));
        Assert.False(ArrivalBrake.AerobrakeWorthOffering(passes: 4, pulsesSaved: 0));   // passes for no saving
        Assert.False(ArrivalBrake.AerobrakeWorthOffering(passes: 0, pulsesSaved: 11));  // saving for no pass
        Assert.False(ArrivalBrake.AerobrakeWorthOffering(passes: -1, pulsesSaved: -1));
    }

    [Fact]
    public void AerobrakeWorthOffering_AcceptsARealTrade()
    {
        // The quote the owner SHOULD have been shown — a real campaign for a real saving.
        Assert.True(ArrivalBrake.AerobrakeWorthOffering(passes: 6, pulsesSaved: 11));
        Assert.True(ArrivalBrake.AerobrakeWorthOffering(passes: 1, pulsesSaved: 1));
    }

    // ===== The fire math: shed to the clamp window, pay what the tank holds =====

    [Fact]
    public void FireBrake_FundedTank_ShedsExactlyToTheClampWindow_AndPaysTheWholeBill()
    {
        // A hot arrival with a tank that covers the quoted bill: the ship is left at the clamp window exactly,
        // and the whole quoted pulse bill is spent (the one charge, no more).
        ArrivalBrake.FireResult r = ArrivalBrake.FireBrake(
            currentRelativeSpeed: 29_800, targetSpeed: LongHaul.InsertionTargetSpeed,
            quotedPulses: 120, tankPulses: 200);

        Assert.Equal(120, r.PulsesSpent);
        Assert.Equal(LongHaul.InsertionTargetSpeed, r.ResultRelativeSpeed, 1e-6);
    }

    [Fact]
    public void FireBrake_ShortTank_SpendsAllItHas_AndCoastsInProRataHot()
    {
        // Half the bill in the tank → half the shed bought; the ship coasts in the rest hot (the #262
        // warn-and-coast, now paid down as far as the tank reaches), and never more than the tank is spent.
        double from = 24_000, target = LongHaul.InsertionTargetSpeed; // 8 km/s
        ArrivalBrake.FireResult r = ArrivalBrake.FireBrake(from, target, quotedPulses: 100, tankPulses: 50);

        Assert.Equal(50, r.PulsesSpent);
        double fullShed = from - target;               // 16 km/s to shed in full
        double expected = from - fullShed * 0.5;        // half shed → 16 km/s left
        Assert.Equal(expected, r.ResultRelativeSpeed, 1e-6);
        Assert.True(r.ResultRelativeSpeed > target);    // still hot — coasting in
    }

    [Fact]
    public void FireBrake_EmptyTank_ShedsNothing_AndSpendsNothing()
    {
        ArrivalBrake.FireResult r = ArrivalBrake.FireBrake(20_000, LongHaul.InsertionTargetSpeed, quotedPulses: 80, tankPulses: 0);
        Assert.Equal(0, r.PulsesSpent);
        Assert.Equal(20_000, r.ResultRelativeSpeed, 1e-6);
    }

    [Fact]
    public void FireBrake_AlreadyInsideTheWindow_IsANoOpShed()
    {
        // Arrival already under the clamp speed — nothing to shed, whatever the tank holds.
        ArrivalBrake.FireResult r = ArrivalBrake.FireBrake(
            LongHaul.InsertionTargetSpeed - 500, LongHaul.InsertionTargetSpeed, quotedPulses: 0, tankPulses: 100);
        Assert.Equal(0, r.PulsesSpent);
        Assert.Equal(LongHaul.InsertionTargetSpeed - 500, r.ResultRelativeSpeed, 1e-6);
    }

    // ===== The one voice: the ask carries the quoted bill, the unfunded warning, and the aerobrake =====

    [Fact]
    public void AskPropulsive_CarriesTheQuotedBill_InTheOwnersShape()
    {
        string ask = ArrivalBrake.AskPropulsive("The Tilt", 120);
        Assert.Contains("The Tilt", ask);
        Assert.Contains("≈120 p", ask);
        Assert.Contains("fire?", ask);
    }

    [Fact]
    public void AskUnfunded_FoldsInTheTankWarning()
    {
        string ask = ArrivalBrake.AskUnfunded("The Tilt", 120, tankPulses: 40);
        Assert.Contains("≈120 p", ask);
        Assert.Contains("the tank holds 40", ask);
        Assert.Contains("coast in hot", ask);
        Assert.Contains("fire?", ask);
    }

    [Fact]
    public void AskAerobrake_SpeaksThePassAndTheSaving()
    {
        string ask = ArrivalBrake.AskAerobrake("Uranus", passes: 6, pulsesSaved: 11);
        Assert.Contains("🪂", ask);
        Assert.Contains("Uranus", ask);
        Assert.Contains("6 passes", ask);
        Assert.Contains("≈11 p saved", ask);
        Assert.Contains("commit the pass?", ask);
    }

    [Fact]
    public void Receipts_SpeakTheFireTheHotFireAndTheDecline()
    {
        Assert.Contains("120 p shed", ArrivalBrake.Fired("The Tilt", 120));
        Assert.Contains("coast in the rest hot", ArrivalBrake.FiredHot("The Tilt", 40));
        Assert.Contains("you have the ship", ArrivalBrake.Declined("The Tilt"));
        Assert.Contains("riding the haze", ArrivalBrake.AerobrakeCommitted("Uranus"));
    }
}
