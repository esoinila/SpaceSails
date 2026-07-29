namespace SpaceSails.Core.Tests;

/// <summary>
/// MAKING SURE — the third road off a wreck. See <c>docs/features/making-sure.md</c>.
/// </summary>
public class ScuttleTests
{
    // ── What each hull can actually do ────────────────────────────────────────────────────────────────

    [Fact]
    public void AHullThatAlreadyRanAwayWithHerselfHasNothingLeftToOverload()
    {
        IReadOnlyList<Scuttle.Method> methods = Scuttle.Available(
            Derelict.WreckCause.ReactorCascade, rockInReach: true, atmosphereInReach: true, tanksAreGood: true);

        Assert.DoesNotContain(Scuttle.Method.ReactorOverload, methods);
        Assert.Contains("already done this", Scuttle.Refusal(
            Scuttle.Method.ReactorOverload, Derelict.WreckCause.ReactorCascade));
    }

    [Fact]
    public void AHullWhoseDriveIsDeadCanOnlyBeOverloaded()
    {
        // Every set-course method needs a drive that answers, which is precisely what a drive failure is
        // not. So the reactor is the method for a ship that cannot be sent anywhere — which is exactly the
        // ship most likely to be left drifting.
        IReadOnlyList<Scuttle.Method> methods = Scuttle.Available(
            Derelict.WreckCause.DriveFailure, rockInReach: true, atmosphereInReach: true, tanksAreGood: true);

        Assert.Equal([Scuttle.Method.ReactorOverload], methods);
    }

    [Fact]
    public void EveryHullCanBeEndedIfThereIsSomewhereToSendHer()
    {
        // With a destination in reach, no cause may refuse every method — otherwise "making sure" is a road
        // the game sometimes silently closes.
        foreach (Derelict.WreckCause cause in System.Enum.GetValues<Derelict.WreckCause>())
        {
            IReadOnlyList<Scuttle.Method> methods = Scuttle.Available(
                cause, rockInReach: true, atmosphereInReach: true, tanksAreGood: true);

            Assert.True(methods.Count > 0, $"{cause}: nowhere to send her and no way to end her");
        }
    }

    [Fact]
    public void ExactlyONEHullCanBeUnscuttleable_AndItIsExplicable()
    {
        // Found by the test above before it was written down. A REACTOR CASCADE with nothing in reach can
        // be ended by nothing: she has no reactor left to overload (she already did that, and the aft third
        // is the proof) and no destination to be sent to. That is a real constraint, not an oversight — she
        // is the one hull whose scuttling system is the thing that killed her — and the panel says so in
        // plain words rather than greying out in silence.
        //
        // It is survivable as a design because the pressure to be CERTAIN comes from infested hulls, and a
        // wreck has exactly one cause: a cascade wreck is never the one with something still aboard.
        var stranded = new List<Derelict.WreckCause>();
        foreach (Derelict.WreckCause cause in System.Enum.GetValues<Derelict.WreckCause>())
        {
            if (Scuttle.Available(cause, false, false, false).Count == 0)
            {
                stranded.Add(cause);
            }
        }

        Assert.Equal([Derelict.WreckCause.ReactorCascade], stranded);
    }

    [Fact]
    public void TheSlowMethodsNeedWhatTheyActuallyNeed()
    {
        Derelict.WreckCause fine = Derelict.WreckCause.Piracy;

        Assert.DoesNotContain(Scuttle.Method.SunCourse,
            Scuttle.Available(fine, false, false, tanksAreGood: false));
        Assert.Contains(Scuttle.Method.SunCourse,
            Scuttle.Available(fine, false, false, tanksAreGood: true));

        Assert.DoesNotContain(Scuttle.Method.SurfaceCourse,
            Scuttle.Available(fine, rockInReach: false, false, false));
        Assert.Contains(Scuttle.Method.SurfaceCourse,
            Scuttle.Available(fine, rockInReach: true, false, false));

        Assert.DoesNotContain(Scuttle.Method.AtmosphereBurn,
            Scuttle.Available(fine, false, atmosphereInReach: false, false));
        Assert.Contains(Scuttle.Method.AtmosphereBurn,
            Scuttle.Available(fine, false, atmosphereInReach: true, false));
    }

    [Fact]
    public void EveryMethodTHISHullCannotDoSaysWhyNot()
    {
        // A greyed control that does not say why is a bug wearing a tooltip's clothes. Only methods that
        // are actually refused need a reason — asking one of an available method is meaningless.
        foreach (Derelict.WreckCause cause in System.Enum.GetValues<Derelict.WreckCause>())
        {
            IReadOnlyList<Scuttle.Method> can = Scuttle.Available(cause, false, false, false);

            foreach (Scuttle.Method m in System.Enum.GetValues<Scuttle.Method>())
            {
                if (can.Contains(m))
                {
                    continue;
                }
                Assert.False(
                    string.IsNullOrWhiteSpace(Scuttle.Refusal(m, cause)),
                    $"{cause}/{m} is off the table and does not say why");
            }
        }
    }

    [Fact]
    public void EveryMethodSaysHowLongItTakes()
    {
        foreach (Scuttle.Method m in System.Enum.GetValues<Scuttle.Method>())
        {
            Assert.False(string.IsNullOrWhiteSpace(Scuttle.Speed(m)));
        }
    }

    // ── The clock ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheOverloadGivesYouTheWalkAndNotMuchElse()
    {
        // Long enough to get from the machinery aft to the lock at the bow at a walk; short enough that the
        // doors you dogged and the rooms you vented are suddenly your problem.
        Assert.True(Scuttle.OverloadSeconds >= 60);
        Assert.True(Scuttle.OverloadSeconds <= 150);
    }

    [Fact]
    public void ThereIsAPointOfNoReturn_SoArmingItIsADecisionAndNotAToggle()
    {
        Assert.True(Scuttle.CanAbort(Scuttle.OverloadSeconds));
        Assert.False(Scuttle.CanAbort(1.0));
        Assert.False(Scuttle.CanAbort(0.0));

        // And the window is a real part of the run, not a formality at either end.
        Assert.True(Scuttle.AbortWindowSeconds > 0);
        Assert.True(Scuttle.AbortWindowSeconds < Scuttle.OverloadSeconds);
    }

    // ── What you hear ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WhenSomethingWasStillAboard_YouHearIt_AndAreNeverToldWhatItWas()
    {
        // THE RULE THIS WHOLE LANE IS BUILT ON. The instrument has never once been able to say what is warm
        // and moving in there. If the ending finally showed the captain, it would spend the thing the lane
        // was made of — at the exact moment they can no longer go and check.
        string line = Scuttle.SheGoes(Scuttle.Method.ReactorOverload, somethingWasStillAliveAboard: true);

        Assert.Contains("Clawing", line);
        Assert.Contains("It stops when she does", line);
        Assert.Contains("will not find out what it was", line);

        foreach (string spoiler in new[] { "reever", "old one", "nest", "creature", "survivor" })
        {
            Assert.DoesNotContain(spoiler, line, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AnEmptyHullGoesQuiet_AndTheCardSaysSo()
    {
        // The other half of the confirmation: if there was nothing aboard, the captain deserves to know
        // they spent a ship on nothing. Certainty cuts both ways.
        string line = Scuttle.SheGoes(Scuttle.Method.ReactorOverload, somethingWasStillAliveAboard: false);

        Assert.Contains("stays quiet", line);
        Assert.Contains("empty ship", line);
        Assert.DoesNotContain("Clawing", line);
    }

    [Fact]
    public void EveryMethodHasItsOwnEnding()
    {
        var endings = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (Scuttle.Method m in System.Enum.GetValues<Scuttle.Method>())
        {
            string line = Scuttle.SheGoes(m, false);
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.True(endings.Add(line), $"{m} shares its ending with another method");
        }
    }

    [Fact]
    public void BeingCertainIsWorthMoreWhenThereWasSomethingToBeCertainAbout()
    {
        Assert.True(Scuttle.Relief(true) > Scuttle.Relief(false));
        Assert.True(Scuttle.Relief(false) > 0);
    }

    // ── The price ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThePanelNamesWhatYouAreGivingUp_BEFORETheKeysTurn()
    {
        // A cost the captain finds out about afterwards is not a cost, it is a punishment.
        string line = Scuttle.PriceLine(120_000, 14_500);

        Assert.Contains("120,000", line);
        Assert.Contains("14,500", line);
        Assert.Contains("Nobody pays you to be sure", line);
    }

    [Fact]
    public void MakingSureNeverPays()
    {
        // There is no credits-yielding API here at all, and that is the design: the moment certainty has a
        // price it stops being a decision and becomes an exploit. This test exists so that adding one has
        // to be a deliberate act with a red suite in front of it.
        Assert.DoesNotContain(
            typeof(Scuttle).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
            m => m.ReturnType == typeof(int) && m.Name.Contains("Credit", System.StringComparison.Ordinal));
    }
}
