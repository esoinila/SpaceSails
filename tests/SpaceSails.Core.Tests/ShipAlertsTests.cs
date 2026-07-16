namespace SpaceSails.Core.Tests;

/// <summary>#166 — the ship-wide alert channel: edge-triggered (fire once on entry, re-arm on exit),
/// acknowledgeable (silence the shout while the condition holds), and the fuel thresholds it reads.</summary>
public class ShipAlertsTests
{
    // ---- Edge triggering ----

    [Fact]
    public void Raise_FiresOnceOnEntry_NotWhileTheConditionHolds()
    {
        var alerts = new ShipAlerts();
        Assert.True(alerts.Raise(AlertKind.Collision, AlertSeverity.Red, "ROCKS AHEAD!", nowSeconds: 10));
        // Same condition, still true a tick later — no re-fire (the parrot must not squawk forever).
        Assert.False(alerts.Raise(AlertKind.Collision, AlertSeverity.Red, "ROCKS AHEAD!", nowSeconds: 11));
        Assert.False(alerts.Raise(AlertKind.Collision, AlertSeverity.Red, "ROCKS AHEAD! (closer)", nowSeconds: 12));
        Assert.True(alerts.IsActive(AlertKind.Collision));
    }

    [Fact]
    public void Clear_ReArmsSoTheNextOnsetFiresAgain()
    {
        var alerts = new ShipAlerts();
        Assert.True(alerts.Raise(AlertKind.Collision, AlertSeverity.Red, "ROCKS AHEAD!", 10));
        Assert.True(alerts.Clear(AlertKind.Collision));   // falling edge — was active
        Assert.False(alerts.IsActive(AlertKind.Collision));
        Assert.False(alerts.Clear(AlertKind.Collision));  // already clear — nothing to fall
        // A fresh crossing fires again.
        Assert.True(alerts.Raise(AlertKind.Collision, AlertSeverity.Red, "ROCKS AHEAD!", 30));
    }

    [Fact]
    public void Raise_EscalatingSeverity_IsANewEdge_AndReStampsTheRaiseTime()
    {
        var alerts = new ShipAlerts();
        Assert.True(alerts.Raise(AlertKind.Fuel, AlertSeverity.Amber, "fuel below reserve", nowSeconds: 5));
        // Amber → Red is a NEW crossing: it fires and re-stamps first-raised.
        Assert.True(alerts.Raise(AlertKind.Fuel, AlertSeverity.Red, "can't reach a pump", nowSeconds: 40));
        ShipAlert a = alerts.Get(AlertKind.Fuel)!.Value;
        Assert.Equal(AlertSeverity.Red, a.Severity);
        Assert.Equal("can't reach a pump", a.Message);
        Assert.Equal(40, a.FirstRaisedSeconds);
    }

    [Fact]
    public void Raise_DeEscalation_DoesNotDropSeverityAndDoesNotReFire()
    {
        var alerts = new ShipAlerts();
        Assert.True(alerts.Raise(AlertKind.Fuel, AlertSeverity.Red, "can't reach a pump", 5));
        // A weaker reading while still critical must not quietly downgrade the live alert.
        Assert.False(alerts.Raise(AlertKind.Fuel, AlertSeverity.Amber, "fuel below reserve", 6));
        Assert.Equal(AlertSeverity.Red, alerts.Get(AlertKind.Fuel)!.Value.Severity);
    }

    // ---- Acknowledgement ----

    [Fact]
    public void Acknowledge_SilencesTheShout_ButTheAlertPersists()
    {
        var alerts = new ShipAlerts();
        alerts.Raise(AlertKind.OrbitDegrade, AlertSeverity.Amber, "orbit degrading", 10);
        Assert.True(alerts.AnyUnacknowledged);
        Assert.Equal(AlertSeverity.Amber, alerts.TopUnacknowledgedSeverity);

        Assert.True(alerts.Acknowledge(AlertKind.OrbitDegrade));
        Assert.True(alerts.IsActive(AlertKind.OrbitDegrade));      // still present (a dimmed chip)
        Assert.False(alerts.AnyUnacknowledged);                    // but no longer shouting
        Assert.Null(alerts.TopUnacknowledgedSeverity);
        Assert.True(alerts.Get(AlertKind.OrbitDegrade)!.Value.Acknowledged);

        Assert.False(alerts.Acknowledge(AlertKind.OrbitDegrade));  // nothing left to silence
    }

    [Fact]
    public void Acknowledge_ThenEscalation_ReRaisesTheShout()
    {
        var alerts = new ShipAlerts();
        alerts.Raise(AlertKind.Fuel, AlertSeverity.Amber, "fuel below reserve", 10);
        alerts.Acknowledge(AlertKind.Fuel);
        Assert.False(alerts.AnyUnacknowledged);

        // A new crossing (amber → red) un-silences it.
        Assert.True(alerts.Raise(AlertKind.Fuel, AlertSeverity.Red, "can't reach a pump", 20));
        Assert.True(alerts.AnyUnacknowledged);
        Assert.False(alerts.Get(AlertKind.Fuel)!.Value.Acknowledged);
    }

    [Fact]
    public void Acknowledge_ThenClearAndReRaise_ShoutsAgain()
    {
        var alerts = new ShipAlerts();
        alerts.Raise(AlertKind.Collision, AlertSeverity.Red, "ROCKS AHEAD!", 10);
        alerts.Acknowledge(AlertKind.Collision);
        alerts.Clear(AlertKind.Collision);
        Assert.True(alerts.Raise(AlertKind.Collision, AlertSeverity.Red, "ROCKS AHEAD!", 50));
        Assert.True(alerts.AnyUnacknowledged);
    }

    // ---- Aggregate readers ----

    [Fact]
    public void TopUnacknowledgedSeverity_IsRedWhenAnyUnackedRed()
    {
        var alerts = new ShipAlerts();
        alerts.Raise(AlertKind.Fuel, AlertSeverity.Amber, "fuel low", 1);
        alerts.Raise(AlertKind.Collision, AlertSeverity.Red, "ROCKS AHEAD!", 2);
        Assert.Equal(AlertSeverity.Red, alerts.TopUnacknowledgedSeverity);
        // Silencing the red leaves amber as the top shout.
        alerts.Acknowledge(AlertKind.Collision);
        Assert.Equal(AlertSeverity.Amber, alerts.TopUnacknowledgedSeverity);
        Assert.Equal(2, alerts.Active.Count());
    }

    // ---- Fuel thresholds ----

    [Fact]
    public void FuelAlertRule_AmberAtReserve_RedAtFloor_NullAboveReserve()
    {
        const int cap = 250;
        // Comfortably fuelled — no alarm.
        Assert.Null(FuelAlertRule.Evaluate(pulses: 200, capacity: cap));
        // At/under the 18% reserve — amber.
        int reserve = (int)(FuelAlertRule.AmberReserveFraction * cap); // 45
        Assert.Equal(AlertSeverity.Amber, FuelAlertRule.Evaluate(reserve, cap));
        Assert.Equal(AlertSeverity.Amber, FuelAlertRule.Evaluate(reserve - 1, cap));
        // At/under the conservative red floor (5%) — red.
        int floor = (int)(FuelAlertRule.RedFloorFraction * cap); // 12
        Assert.Equal(AlertSeverity.Red, FuelAlertRule.Evaluate(floor, cap));
        Assert.Equal(AlertSeverity.Red, FuelAlertRule.Evaluate(0, cap));
    }

    [Fact]
    public void FuelAlertRule_JustAboveReserve_IsClear()
    {
        const int cap = 250;
        // 46/250 = 18.4% — above the 18% reserve, no alarm.
        Assert.Null(FuelAlertRule.Evaluate(46, cap));
    }

    [Fact]
    public void FuelAlertRule_ReserveFraction_TracksTheAutopilotReserve()
    {
        // The amber threshold IS the autopilot's own reserve, so the flight planner and the alarm agree.
        Assert.Equal(AutopilotRehearsal.ReserveFraction, FuelAlertRule.AmberReserveFraction);
    }

    [Fact]
    public void FuelAlertRule_NonPositiveCapacity_IsNoReading()
    {
        Assert.Null(FuelAlertRule.Evaluate(0, 0));
        Assert.Null(FuelAlertRule.Evaluate(10, -5));
    }
}
