namespace SpaceSails.Core.Tests;

/// <summary>
/// #519 · The captain's word, on her own compartments. Owner: <i>"venting a space would require captain's
/// consent … like we have for shooting and looting."</i> These pin the promise that gate makes.
/// </summary>
public class ShipAuthorityTests
{
    [Fact]
    public void SelectingARoomIsNeverConsent()
    {
        // The whole rule, and the one the boarding gate was written for after autopilot committed a felony
        // on the owner's behalf. Looking at a compartment must never be able to open it.
        Assert.Equal(ShipAuthority.VentIntent.Opportunity,
                     ShipAuthority.EvaluateVent("CABIN 1", authorizedRoom: null));
    }

    [Fact]
    public void ConsentOnlyCountsForTheRoomItNamed()
    {
        // Not a confirmation dialogue: the authority carries a NAME, so it cannot be given for the cabin and
        // spent on the bridge.
        Assert.Equal(ShipAuthority.VentIntent.Authorized,
                     ShipAuthority.EvaluateVent("CABIN 1", "CABIN 1"));
        Assert.Equal(ShipAuthority.VentIntent.Opportunity,
                     ShipAuthority.EvaluateVent("BRIDGE", "CABIN 1"));
    }

    [Fact]
    public void ChangingTheSelectionRevokesIt()
    {
        // Same re-arming behaviour the boarding window has. The captain who armed CABIN 1, thought better of
        // it and clicked the hold must not find the hold armed.
        const string armed = "CABIN 1";

        Assert.Equal(ShipAuthority.VentIntent.Authorized, ShipAuthority.EvaluateVent(armed, armed));
        Assert.Equal(ShipAuthority.VentIntent.Opportunity, ShipAuthority.EvaluateVent("CARGO HOLD", armed));

        // And the board says so out loud rather than letting it be discovered.
        Assert.Contains(armed, ShipAuthority.LapsedFrom(armed), System.StringComparison.Ordinal);
    }

    [Fact]
    public void NothingSelectedIsItsOwnAnswer()
    {
        Assert.Equal(ShipAuthority.VentIntent.NothingSelected,
                     ShipAuthority.EvaluateVent(null, "CABIN 1"));
    }

    [Fact]
    public void EveryLineNamesTheRoomItIsAbout()
    {
        // A prompt that does not name the compartment is a prompt that can be answered about the wrong one.
        foreach (string room in new[] { "CABIN 1", "THE HEAD", "ENGINE ROOM" })
        {
            Assert.Contains(room, ShipAuthority.AskFor(room), System.StringComparison.Ordinal);
            Assert.Contains(room, ShipAuthority.ArmedFor(room), System.StringComparison.Ordinal);
            Assert.Contains(room, ShipAuthority.LapsedFrom(room), System.StringComparison.Ordinal);
            Assert.Contains(room, ShipAuthority.IsolatedLine(room), System.StringComparison.Ordinal);
            Assert.Contains(room, ShipAuthority.ReleasedLine(room), System.StringComparison.Ordinal);
        }
    }

    [Fact]
    public void IsolatingACompartmentAsksNobody()
    {
        // The everyday half. Quarantine a berth, contain a fire, keep a leak in one room — reversible, free,
        // and deliberately NOT wearing the same ceremony as the irreversible half. A ship where dogging a
        // hatch needed the captain's word would teach the crew to leave every hatch open.
        Assert.DoesNotContain("captain", ShipAuthority.IsolatedLine("CABIN 1"),
                              System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authority", ShipAuthority.IsolatedLine("CABIN 1"),
                              System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheGateIsShapedLikeTheBoardingGateItWasBuiltFrom()
    {
        // Owner: "like we have for shooting and looting." Same three states, same naming rule — so a captain
        // who has learned one has learned both, and a future edit to either has an obvious sibling to check.
        Assert.Equal(3, System.Enum.GetValues<ShipAuthority.VentIntent>().Length);
        Assert.Equal(3, System.Enum.GetValues<CaptureRule.BoardingIntent>().Length);

        Assert.Equal(CaptureRule.BoardingIntent.Authorized,
                     CaptureRule.EvaluateBoarding(inWindow: true, "hull-7", "hull-7"));
        Assert.Equal(CaptureRule.BoardingIntent.Opportunity,
                     CaptureRule.EvaluateBoarding(inWindow: true, "hull-7", "hull-9"));
    }
}
