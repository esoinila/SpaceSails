namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for #304 — 🛬 THE ARRIVAL BRAKE ASKS. Owner ruling (2026-07-18): "let's have it ask, it is
/// hard to remember in the heat of the moment otherwise." The pure timing law lives in
/// <see cref="ArrivalBrake"/> so it unit-tests without a browser: ask at window-open, re-raise on snooze
/// while the window remains, fire exactly once (the double-fire / double-bill guard), decline is stateless,
/// and the fire math sheds to the clamp window (or pro-rata on a short tank). Plus the in-voice wording.
/// </summary>
public class ArrivalBrakeTests
{
    // ===== The timing law: ArrivalBrake.Advance / Snooze / Fire =====

    [Fact]
    public void Advance_RaisesTheAsk_TheFrameTheWindowOpens()
    {
        // A dormant gate with the window open → the ask is raised (never silently skipped).
        ArrivalBrake.Gate gate = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, windowOpen: true, nowMs: 1_000);
        Assert.True(gate.Asking);
        Assert.False(gate.HasFired);
    }

    [Fact]
    public void Advance_WithNoWindow_StaysClosed()
    {
        // No brake owed → nothing raised. (And a spent gate resets when the window shuts — see below.)
        ArrivalBrake.Gate gate = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, windowOpen: false, nowMs: 1_000);
        Assert.False(gate.Asking);
        Assert.Equal(ArrivalBrake.Phase.Dormant, gate.State);
    }

    [Fact]
    public void Advance_HoldsTheAskUp_WhileTheWindowRemains()
    {
        // An open ask is not re-created or dismissed frame to frame — it stays up awaiting the captain.
        ArrivalBrake.Gate asking = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true, 0);
        ArrivalBrake.Gate next = ArrivalBrake.Advance(asking, windowOpen: true, nowMs: 16);
        Assert.True(next.Asking);
    }

    [Fact]
    public void Snooze_HidesTheAsk_ThenReRaisesAfterTheInterval_WhileTheWindowRemains()
    {
        ArrivalBrake.Gate asking = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true, 0);

        // The captain waves it off: the ask hides, nothing fires.
        ArrivalBrake.Gate snoozed = ArrivalBrake.Snooze(asking, nowMs: 1_000);
        Assert.False(snoozed.Asking);
        Assert.False(snoozed.HasFired);
        Assert.Equal(ArrivalBrake.Phase.Snoozed, snoozed.State);

        // Still snoozed just before the deadline — the window is open but the nag interval hasn't elapsed.
        ArrivalBrake.Gate stillSnoozed = ArrivalBrake.Advance(snoozed, windowOpen: true, nowMs: 1_000 + ArrivalBrake.SnoozeReraiseMs - 1);
        Assert.False(stillSnoozed.Asking);

        // Past the deadline, window still open → the ask re-raises (never silently skipped).
        ArrivalBrake.Gate reRaised = ArrivalBrake.Advance(snoozed, windowOpen: true, nowMs: 1_000 + ArrivalBrake.SnoozeReraiseMs);
        Assert.True(reRaised.Asking);
    }

    [Fact]
    public void Snooze_WhenTheWindowShutsBeforeReRaise_ResetsRatherThanReRaising()
    {
        // The captain declined and then sheds by hand (or docks): the window shuts, the gate resets — a later
        // arrival asks afresh, this one is not re-raised into a shut window.
        ArrivalBrake.Gate snoozed = ArrivalBrake.Snooze(ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true, 0), 1_000);
        ArrivalBrake.Gate closed = ArrivalBrake.Advance(snoozed, windowOpen: false, nowMs: 99_999);
        Assert.Equal(ArrivalBrake.Gate.Closed, closed);
    }

    [Fact]
    public void Fire_MarksFiredOnce_AndIsIdempotent_NoDoubleFire()
    {
        ArrivalBrake.Gate asking = ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true, 0);

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
        ArrivalBrake.Gate fired = ArrivalBrake.Fire(ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, true, 0));

        // While the window is (briefly) still open post-fire, the gate stays Fired — it never re-asks.
        ArrivalBrake.Gate lingering = ArrivalBrake.Advance(fired, windowOpen: true, nowMs: 50);
        Assert.True(lingering.HasFired);
        Assert.False(lingering.Asking);

        // Once the speed is shed and the window shuts, the gate resets for any future arrival.
        ArrivalBrake.Gate shut = ArrivalBrake.Advance(fired, windowOpen: false, nowMs: 60);
        Assert.Equal(ArrivalBrake.Gate.Closed, shut);
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
