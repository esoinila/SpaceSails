namespace SpaceSails.Core.Tests;

/// <summary>
/// #327 · The ship calls home. The owner was marooned on Miranda when his mothership's orbit degraded
/// during a surface excursion — and LOVED it, but demanded warning first ("got to get some warning
/// about that before it just happens"). These pin the pure instrument that speaks the truth the keeper
/// lives by: the hold clock, the escalating ladder, and the one forbidden outcome — silence.
/// </summary>
public class OrbitHoldTests
{
    // ── The hold clock: pulses ÷ the Lab 25 trim bill ──

    [Fact]
    public void HoldSeconds_IsPulsesOverTrimRate_InSimDays()
    {
        // 54 pulses at 27 p/day (Enceladus, the expensive park) = exactly 2 sim-days.
        double hold = OrbitHold.HoldSeconds(pulsesOnHand: 54, trimPulsesPerDay: 27);
        Assert.Equal(2 * 86400.0, hold, precision: 6);
    }

    [Fact]
    public void HoldSeconds_FreePark_NeverStrands()
    {
        // No tide bill (a park that costs nothing to hold) → holds indefinitely.
        Assert.True(double.IsPositiveInfinity(OrbitHold.HoldSeconds(100, trimPulsesPerDay: 0)));
    }

    [Fact]
    public void HoldSeconds_EmptyTank_HoldsNothing()
    {
        Assert.Equal(0, OrbitHold.HoldSeconds(pulsesOnHand: 0, trimPulsesPerDay: 27));
    }

    // ── The ladder: derived from the real clock, strictly ordered ──

    [Fact]
    public void StageFor_StepsSteadyThroughLost_AsHoldErodes()
    {
        double boarding = OrbitHold.HoldSeconds(200, 27); // a roomy boarding hold
        Assert.Equal(OrbitHold.Stage.Steady, OrbitHold.StageFor(boarding, boarding));
        Assert.Equal(OrbitHold.Stage.Steady, OrbitHold.StageFor(boarding * 0.50, boarding));
        Assert.Equal(OrbitHold.Stage.Slipping, OrbitHold.StageFor(boarding * 0.40, boarding));
        Assert.Equal(OrbitHold.Stage.Slipping, OrbitHold.StageFor(boarding * 0.20, boarding));
        Assert.Equal(OrbitHold.Stage.Failing, OrbitHold.StageFor(boarding * 0.15, boarding));
        Assert.Equal(OrbitHold.Stage.Failing, OrbitHold.StageFor(boarding * 0.01, boarding));
        Assert.Equal(OrbitHold.Stage.Lost, OrbitHold.StageFor(0, boarding));
    }

    [Fact]
    public void StageFor_FreeHold_IsForeverSteady()
    {
        Assert.Equal(OrbitHold.Stage.Steady,
            OrbitHold.StageFor(double.PositiveInfinity, double.PositiveInfinity));
    }

    [Fact]
    public void Ladder_FiresEveryRungInOrder_BeforeAnyDegradation()
    {
        // Sweep the hold down from full to empty in fine steps and record the sequence of DISTINCT
        // rungs. The only acceptable sequence is the full ladder, in order — never a silent jump to Lost.
        double boarding = OrbitHold.HoldSeconds(120, 27);
        var seen = new List<OrbitHold.Stage>();
        for (int i = 1000; i >= 0; i--)
        {
            OrbitHold.Stage s = OrbitHold.StageFor(boarding * i / 1000.0, boarding);
            if (seen.Count == 0 || seen[^1] != s)
            {
                seen.Add(s);
            }
        }
        Assert.Equal(
            new[] { OrbitHold.Stage.Steady, OrbitHold.Stage.Slipping, OrbitHold.Stage.Failing, OrbitHold.Stage.Lost },
            seen);
    }

    [Fact]
    public void StageFor_NeverReportsLost_WhileHoldRemains()
    {
        // Degradation (Lost) is only ever reported once the clock hits zero — the ladder guarantees the
        // warnings are exhausted first. While any hold remains, the stage is one of the warned rungs.
        double boarding = OrbitHold.HoldSeconds(120, 27);
        for (int i = 1; i <= 1000; i++)
        {
            Assert.NotEqual(OrbitHold.Stage.Lost, OrbitHold.StageFor(boarding * i / 1000.0, boarding));
        }
    }

    // ── The keeper holds while the tank pays — a deterministic simulated excursion ──

    [Fact]
    public void Keeper_HoldsWhileTankPays_ThenDegradesOnlyWhenExhausted()
    {
        // A long simulated excursion at the expensive park: the tank pays one trim per cadence tick.
        // Assert the ship stays out of Lost every tick a trim is still funded, walks the full ladder,
        // and reaches Lost only on the tick the pulses can no longer buy the next trim.
        const int trimPulsesPerDay = 27;      // Enceladus, Lab 25
        const int trimsPerDay = 3;            // ~9 pulses/trim
        int pulsesPerTrim = trimPulsesPerDay / trimsPerDay;
        int tank = 90;                        // ~30 trims of runway
        double boardingHold = OrbitHold.HoldSeconds(tank, trimPulsesPerDay);

        var seen = new List<OrbitHold.Stage>();
        int trimsPaid = 0;

        // Step the excursion trim by trim until the orbit is lost.
        while (true)
        {
            double remaining = OrbitHold.HoldSeconds(tank, trimPulsesPerDay);
            OrbitHold.Stage stage = OrbitHold.StageFor(remaining, boardingHold);
            if (seen.Count == 0 || seen[^1] != stage)
            {
                seen.Add(stage);
            }

            bool canPayNextTrim = tank >= pulsesPerTrim;
            if (stage == OrbitHold.Stage.Lost)
            {
                // The keeper's law: it degrades only once the tank can't fund the next trim.
                Assert.False(canPayNextTrim, "orbit was lost while the tank could still pay a trim");
                break;
            }

            // Pay the trim, spend the pulses, advance the excursion.
            tank -= pulsesPerTrim;
            trimsPaid++;
            Assert.True(trimsPaid < 1000, "runaway — the hold never resolved");
        }

        // The full ladder was walked before the maroon — every warning rung fired, in order.
        Assert.Equal(
            new[] { OrbitHold.Stage.Steady, OrbitHold.Stage.Slipping, OrbitHold.Stage.Failing, OrbitHold.Stage.Lost },
            seen);
    }

    // ── The comms channel is never silent ──

    [Fact]
    public void Comms_EverySpeaks_IncludingSteady()
    {
        foreach (OrbitHold.Stage stage in Enum.GetValues<OrbitHold.Stage>())
        {
            string line = OrbitHold.Comms(stage, 3600);
            Assert.False(string.IsNullOrWhiteSpace(line), $"{stage} fell silent");
        }
    }

    [Fact]
    public void Comms_Escalates_InWording()
    {
        Assert.Contains("holds the orbit", OrbitHold.Comms(OrbitHold.Stage.Steady, 3600));
        Assert.Contains("slipping", OrbitHold.Comms(OrbitHold.Stage.Slipping, 3600));
        Assert.Contains("come home NOW", OrbitHold.Comms(OrbitHold.Stage.Failing, 3600));
        Assert.Contains("adrift", OrbitHold.Comms(OrbitHold.Stage.Lost, 0));
    }

    [Fact]
    public void Severity_RisesWithTheLadder()
    {
        Assert.Equal(0, OrbitHold.Severity(OrbitHold.Stage.Steady));
        Assert.Equal(1, OrbitHold.Severity(OrbitHold.Stage.Slipping));
        Assert.Equal(2, OrbitHold.Severity(OrbitHold.Stage.Failing));
        Assert.Equal(2, OrbitHold.Severity(OrbitHold.Stage.Lost));
    }

    // ── The boarding quote: in-voice, honest, from the live tank ──

    [Fact]
    public void BoardingQuote_KeptOrbit_StatesTheHold()
    {
        string q = OrbitHold.BoardingQuote(orbitKept: true, OrbitHold.HoldSeconds(200, 27));
        Assert.Contains("can hold this orbit", q);
    }

    [Fact]
    public void BoardingQuote_UnkeptOrbit_SaysSoPlainly()
    {
        string q = OrbitHold.BoardingQuote(orbitKept: false, holdSeconds: 0);
        Assert.Contains("NOT holding", q);
    }

    [Fact]
    public void BoardingQuote_FreePark_SaysTakeYourTime()
    {
        string q = OrbitHold.BoardingQuote(orbitKept: true, double.PositiveInfinity);
        Assert.Contains("for free", q);
    }

    // ── #331 follow-up: docked means the station keeps the orbit, no fuel spent ──

    [Fact]
    public void DockedComms_SaysNoFuelSpent_NotACountdown()
    {
        // Owner ruling: docked at a station, its mass holds the orbit for us. The line must say that
        // plainly — no "min"/"h"/"days" countdown, no misleading "∞" — a calm, explicit reassurance.
        string line = OrbitHold.DockedComms;
        Assert.False(string.IsNullOrWhiteSpace(line));
        Assert.Contains("docked", line);
        Assert.Contains("no fuel", line);
        Assert.DoesNotContain("min", line);
        Assert.DoesNotContain("∞", line);
        Assert.DoesNotContain("indefinitely", line);
    }

    // ── The humaniser reads honestly across scales ──

    [Theory]
    [InlineData(0, "no time")]
    [InlineData(1800, "30 min")]
    [InlineData(7200, "2 h")]
    [InlineData(3 * 86400, "3 days")]
    public void Humanize_ReadsAtTheRightScale(double seconds, string expected)
    {
        Assert.Equal(expected, OrbitHold.Humanize(seconds));
    }

    [Fact]
    public void Humanize_FreePark_ReadsIndefinitely()
    {
        Assert.Equal("indefinitely", OrbitHold.Humanize(double.PositiveInfinity));
    }
}
