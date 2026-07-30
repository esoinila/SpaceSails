using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #538 · RIG FOR SILENT RUNNING, AS LAWS. Owner: <i>"Shuttle should also power down to make it less of an
/// anomaly"</i>, <i>"Captain's remote"</i>, and — the part that makes it a decision — <i>"Warm up time is a cost
/// there 😎"</i>.
/// </summary>
public sealed class SilentRunningTests
{
    /// <summary>
    /// THE TRADE, AS ONE PREDICATE. A cold boat is not a ride, and a boat mid-warm-up is not a ride either — which
    /// is the whole cost of having hidden well. If this ever returned true while she was waking, going dark would
    /// be free and the scene it exists for would have no teeth.
    /// </summary>
    [Theory]
    [InlineData(1.0, false, true)]     // warm and answering
    [InlineData(0.0, true, false)]     // cold
    [InlineData(0.5, false, false)]    // coming up, not there yet
    [InlineData(0.5, true, false)]     // going down — no longer a ride the moment the order is given
    [InlineData(1.0, true, false)]     // still bright, but ordered down: she is not taking anyone anywhere
    public void OnlyAWarmBoatFlies(double warmth, bool orderedCold, bool expected) =>
        Assert.Equal(expected, SilentRunning.ReadyToFly(warmth, orderedCold));

    /// <summary>
    /// THE FOUR STATES, and the one that matters most is <b>CoolingDown</b>: a boat halfway through her shutdown
    /// is neither hidden nor a ride. Owner: <i>"A timer on the map about the cool down and warm up"</i> — asking
    /// for the cool-down to be a clock is what makes last-second hiding impossible, which is the interesting
    /// version of the rule.
    /// </summary>
    [Theory]
    [InlineData(1.0, false, SilentRunning.BoatState.Warm)]
    [InlineData(0.4, false, SilentRunning.BoatState.WarmingUp)]
    [InlineData(0.4, true, SilentRunning.BoatState.CoolingDown)]
    [InlineData(0.0, true, SilentRunning.BoatState.Cold)]
    [InlineData(0.0, false, SilentRunning.BoatState.WarmingUp)]   // ordered up from stone cold
    [InlineData(1.0, true, SilentRunning.BoatState.CoolingDown)]  // ordered down from fully warm
    public void TheDialAndTheOrderTogetherSayWhereSheIs(double warmth, bool cold, SilentRunning.BoatState expected) =>
        Assert.Equal(expected, SilentRunning.StateOf(warmth, cold));

    /// <summary>ONLY A PROPERLY COLD BOAT HIDES YOU. If a cooling boat counted as quiet, a captain could thumb the
    /// remote as the boarding tube mated and pay nothing for the timing — which would make the cool-down clock
    /// decorative.</summary>
    [Fact]
    public void AHalfShutBoatIsStillAnAnomaly()
    {
        Assert.True(SilentRunning.IsQuiet(SilentRunning.BoatState.Cold));
        Assert.False(SilentRunning.IsQuiet(SilentRunning.BoatState.CoolingDown));
        Assert.False(SilentRunning.IsQuiet(SilentRunning.BoatState.Warm));
        Assert.False(SilentRunning.IsQuiet(SilentRunning.BoatState.WarmingUp));
    }

    /// <summary>
    /// THE HATCH IS THE BOAT ANSWERING. Owner: <i>"its doors open once the warm up is complete"</i> — so the door
    /// is not something the captain opens, it is the warm-up finishing. Which is what makes the clock frightening
    /// rather than merely inconvenient: you are not waiting to depart, you are waiting to be LET IN.
    /// </summary>
    [Fact]
    public void HerHatchOpensExactlyWhenSheIsUpAndNeverBefore()
    {
        Assert.True(SilentRunning.HatchOpen(SilentRunning.BoatState.Warm));
        Assert.False(SilentRunning.HatchOpen(SilentRunning.BoatState.WarmingUp));
        Assert.False(SilentRunning.HatchOpen(SilentRunning.BoatState.Cold));
        Assert.False(SilentRunning.HatchOpen(SilentRunning.BoatState.CoolingDown));

        // …and the hatch and the ride agree on every state. Two rules that can disagree eventually will.
        foreach (SilentRunning.BoatState s in Enum.GetValues<SilentRunning.BoatState>())
        {
            Assert.Equal(s == SilentRunning.BoatState.Warm, SilentRunning.HatchOpen(s));
        }
    }

    // ── The dial ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Each end takes exactly as long as it says, from the other end, ticked one second at a time — the
    /// arithmetic a player will actually be doing while they watch the number.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheClockOnTheMapIsTheTimeItActuallyTakes(bool orderedCold)
    {
        double warmth = orderedCold ? 1.0 : 0.0;
        double expected = orderedCold ? SilentRunning.SpinDownSeconds : SilentRunning.SpinUpSeconds;
        Assert.Equal(expected, SilentRunning.SecondsLeft(warmth, orderedCold), 6);

        int ticks = 0;
        while (SilentRunning.SecondsLeft(warmth, orderedCold) > 0 && ticks < 1000)
        {
            warmth = SilentRunning.AdvanceWarmth(warmth, orderedCold, 0.5);
            ticks++;
        }

        Assert.Equal(expected, ticks * 0.5, 6);
        Assert.Equal(orderedCold ? SilentRunning.BoatState.Cold : SilentRunning.BoatState.Warm,
                     SilentRunning.StateOf(warmth, orderedCold));
    }

    /// <summary>
    /// CHANGING YOUR MIND KEEPS YOUR PROGRESS — the reason this is a dial and not two countdowns. A captain who
    /// orders her down and reverses a moment later gets a boat that is a moment of cold away from flying. Two
    /// independent timers would have punished exactly the decision this system wants people making.
    /// </summary>
    [Fact]
    public void ReversingTheOrderDoesNotThrowAwayWhatWasAlreadySpent()
    {
        // Two seconds of shutting down, then think better of it.
        double warmth = SilentRunning.AdvanceWarmth(1.0, orderedCold: true, dtSeconds: 2.0);
        double backUp = SilentRunning.SecondsLeft(warmth, orderedCold: false);

        Assert.True(backUp > 0, "she is not warm any more");
        Assert.True(backUp < SilentRunning.SpinUpSeconds / 2,
                    "two seconds of cold must not cost most of a full warm-up");
    }

    /// <summary>The dial never leaves 0…1, however hard a long frame or a stalled tab shoves it.</summary>
    [Fact]
    public void TheDialCannotBeShovedPastEitherEnd()
    {
        Assert.Equal(1.0, SilentRunning.AdvanceWarmth(0.9, orderedCold: false, dtSeconds: 9_999));
        Assert.Equal(0.0, SilentRunning.AdvanceWarmth(0.1, orderedCold: true, dtSeconds: 9_999));
        Assert.Equal(0.5, SilentRunning.AdvanceWarmth(0.5, orderedCold: true, dtSeconds: -50));
    }

    /// <summary>GOING DARK IS FASTER THAN COMING UP. Dumping a bus is easier than bringing one up, and the game
    /// wants panic-hiding to be almost viable while panic-leaving never is.</summary>
    [Fact]
    public void SheGoesDownFasterThanSheComesUp() =>
        Assert.True(SilentRunning.SpinDownSeconds < SilentRunning.SpinUpSeconds);

    /// <summary>The warm-up has to be long enough to be a commitment and short enough not to be a death sentence
    /// — roughly the walk from amidships to the lock, so a captain who has to run arrives at a boat that is still
    /// waking up.</summary>
    [Fact]
    public void TheWarmUpIsACommitmentAndNotASentence()
    {
        Assert.True(SilentRunning.SpinUpSeconds >= 10);
        Assert.True(SilentRunning.SpinUpSeconds <= 45);
    }

    /// <summary>The panel names both costs out loud, because a captain should be able to read the trade before
    /// committing to it rather than discovering it at the lock.</summary>
    [Fact]
    public void ThePanelAdmitsBothCostsBeforeYouCommit()
    {
        Assert.Contains("will not defend you", SilentRunning.WhatItCostsLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("will not fly you", SilentRunning.WhatItCostsLine, StringComparison.OrdinalIgnoreCase);
        // …and the third cost, once the belts turned out to live aboard her too: "arm you".
        Assert.Contains("arm you", SilentRunning.WhatItCostsLine, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A COLD BOAT ARMS NOBODY. Owner, listing what makes the wait bite: <i>"As the ammo count runs down and reload
    /// place is warming up to allow use and its gun"</i> — the belts are aboard her, so going dark takes away the
    /// resupply as well as the ride, and the refusal has to quote the same clock the map is showing.
    /// </summary>
    [Fact]
    public void TheReloadRefusalQuotesHerClockToo() =>
        Assert.Contains(HullVenting.SoakLabel(9), SilentRunning.ReloadNeedsHerAwakeLine(9), StringComparison.Ordinal);

    /// <summary>Every state gets a tag and a note for the map strip, and none of them is blank — a clock line with
    /// an empty column reads as a bug rather than as a state.</summary>
    [Fact]
    public void EveryStateHasSomethingToPutOnTheStrip()
    {
        foreach (SilentRunning.BoatState s in Enum.GetValues<SilentRunning.BoatState>())
        {
            Assert.False(string.IsNullOrWhiteSpace(SilentRunning.ClockTag(s)));
            Assert.False(string.IsNullOrWhiteSpace(SilentRunning.ClockNote(s)));
            Assert.False(string.IsNullOrWhiteSpace(SilentRunning.BoatButtonLabel(s)));
        }
    }

    /// <summary>The panel's own read-out names the consequence in every state, and quotes the clock in the two that
    /// have one.</summary>
    [Fact]
    public void TheReadOutQuotesTheClockWhileEitherOneRuns()
    {
        Assert.Contains(HullVenting.SoakLabel(SilentRunning.SpinUpSeconds),
                        SilentRunning.SpinUpLabel(0.0, orderedCold: false), StringComparison.Ordinal);
        Assert.Contains(HullVenting.SoakLabel(SilentRunning.SpinDownSeconds),
                        SilentRunning.SpinUpLabel(1.0, orderedCold: true), StringComparison.Ordinal);

        Assert.Contains("hatch open", SilentRunning.SpinUpLabel(1.0, false), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dogged", SilentRunning.SpinUpLabel(0.0, true), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AND THE CROSS-SYSTEM INEQUALITY THAT KEEPS THE SCENE FAIR. Owner: <i>"Or say self destruct tics down but
    /// shuttle still warms up 🤭"</i> — which is only a good scene if a captain who arms the charges and THEN
    /// remembers the boat can still make it. If the warm-up ever exceeded the overload, arming while dark would be
    /// suicide by arithmetic rather than a race, and nobody would ever do it twice.
    ///
    /// <para>Pinned with the walk in it: coming up from stone cold plus the run to the lock must fit inside the
    /// countdown, and by a margin big enough to be frightening rather than impossible.</para>
    /// </summary>
    [Fact]
    public void SheCanComeUpFromColdInsideAnOverload()
    {
        Assert.True(SilentRunning.SpinUpSeconds < Scuttle.OverloadSeconds,
                    "arming the charges with a cold boat must be a race, not a death sentence");

        // The margin has to leave a captain time to actually get there — she is not the only thing that moves.
        Assert.True(Scuttle.OverloadSeconds - SilentRunning.SpinUpSeconds >= 30,
                    "the margin must cover the walk to the lock as well as the warm-up");
    }

    /// <summary>The scene the owner sketched — <i>"self destruct tics down but shuttle still warms up 🤭"</i> — is
    /// named in one line, and it promises nothing.</summary>
    [Fact]
    public void TheTwoClocksLineOffersNoWayToHurryEither()
    {
        Assert.Contains("Two clocks", SilentRunning.TwoClocksLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("faster", SilentRunning.TwoClocksLine, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>It is a CAPTAIN'S REMOTE, not a settings menu — the owner's own name for it, and the name is the
    /// design: a handheld thing thumbed behind a bulkhead.</summary>
    [Fact]
    public void ItIsAHandheldNotAMenu() =>
        Assert.Contains("CAPTAIN'S REMOTE", SilentRunning.PanelTitle, StringComparison.Ordinal);

    /// <summary>And the refusal quotes the time left, so the cost is a number rather than a mood.</summary>
    [Fact]
    public void TheRefusalQuotesWhatIsLeft() =>
        Assert.Contains(HullVenting.SoakLabel(12), SilentRunning.NotARideYetLine(12), StringComparison.Ordinal);
}
