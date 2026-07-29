namespace SpaceSails.Core.Tests;

/// <summary>
/// THE TENTH CAUSE — the ship one of her own opened to space. Owner, 2026-07-29: <i>"one ship destiny
/// might be that somebody went crazy due to an object and vented most of the ship and the survivors also
/// fell to some other craziness after that. So there would be a theme."</i>
///
/// <para>What makes her worth building is that the player has ALREADY DONE THIS — stood at that board,
/// read a life sign the instrument could not identify, and pulled the handle anyway. The other nine
/// wrecks are stories about strangers.</para>
/// </summary>
public class VentedWreckTests
{
    [Fact]
    public void EveryCompartmentButOneIsAlreadyVacuum()
    {
        int vented = WreckLayout.Compartments.Count(
            c => HullVenting.StartsVented(Derelict.WreckCause.VentedByOneOfTheirOwn, c.Name));

        Assert.Equal(WreckLayout.Compartments.Length - 1, vented);
    }

    [Fact]
    public void TheOneRoomWithAirIsTheRoomTheValveBoardIsIn()
    {
        // THE POINT OF THE WHOLE CAUSE, and it is not a detail we invented: HullVenting.Readiness refuses
        // CaptainInside — the board will not blow the room you are standing in. So the single warm room on
        // a dead ship is where the person who did this was standing, and the evidence for who did it is a
        // rule the player learned by being refused it themselves.
        Assert.False(HullVenting.StartsVented(
            Derelict.WreckCause.VentedByOneOfTheirOwn, HullVenting.ValveCompartment));

        Assert.Equal(HullVenting.ValveCompartment, HullVenting.TheRoomTheyWereStandingIn);

        // And prove the interlock that implies it is still the live rule, not a comment about one.
        var standingThere = new HullVenting.Space(
            HullVenting.ValveCompartment, DoorShut: true, Vented: false,
            Infested: false, HoldsSurvivor: false)
        { CaptainInside = true };

        Assert.Equal(HullVenting.VentReadiness.CaptainInside, HullVenting.Readiness(standingThere));
    }

    [Fact]
    public void NoOtherCauseArrivesPreVented()
    {
        foreach (Derelict.WreckCause cause in System.Enum.GetValues<Derelict.WreckCause>())
        {
            if (cause == Derelict.WreckCause.VentedByOneOfTheirOwn)
            {
                continue;
            }

            Assert.All(WreckLayout.Compartments,
                c => Assert.False(HullVenting.StartsVented(cause, c.Name)));
        }
    }

    [Fact]
    public void SheIsIntact_BecauseNothingBrokeAboardHer()
    {
        // No damage geometry: she was vented, not holed. An intact hull full of vacuum is its own finding,
        // the same way the life-support wreck's working indicators are.
        Assert.Empty(WreckLayout.DamageWalls(Derelict.WreckCause.VentedByOneOfTheirOwn));
    }

    [Fact]
    public void TheCauseIsFullyDressed_LikeEveryOtherWreck()
    {
        Derelict.WreckCause cause = Derelict.WreckCause.VentedByOneOfTheirOwn;

        Assert.False(string.IsNullOrWhiteSpace(Derelict.CauseHeadline(cause)));
        Assert.False(string.IsNullOrWhiteSpace(Derelict.Evidence(cause)));
        Assert.StartsWith("art/", Derelict.ArtFile(cause));
    }

    [Fact]
    public void SheReadsAsAnAirPlantFailure_WhichIsTheMisreadTheCaptainIsInvitedToMake()
    {
        // Unlike every other misread in the set, this one is not a mistake the WRECK makes — the evidence
        // is plain at the valve board. Naming it truly means accusing a named dead crewmember on
        // circumstantial evidence; the comfortable answer pays the same and leaves the thing in her hold
        // out of the record entirely.
        Assert.Equal(Derelict.WreckCause.LifeSupportFailure,
                     Derelict.MisreadsAs(Derelict.WreckCause.VentedByOneOfTheirOwn));
    }

    [Fact]
    public void TheSecondMadnessIsInTheLog_AndTheLogSaysNothingHappened()
    {
        // "The survivors also fell to some other craziness after that." They kept the ship running for a
        // crew that was no longer aboard. The horror is administrative.
        string log = HullVenting.VentedShipLogLine;

        Assert.Contains("forty names", log);
        Assert.Contains("Nothing is ever recorded as having happened", log);
    }

    [Fact]
    public void TheFortyEchoesTheIceMoonAndTheGlitchCard()
    {
        // The same motif in a third place, and the first where the captain finds it as an object rather
        // than a rumour: KAAMOS's forty souls, the resurrection card's PATTERN 40, and now a watch bill.
        Assert.Contains("forty", HullVenting.VentedShipLogLine, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("40", KaamosLore.Fragments.First(f => f.Id == "cold-pod").Lore);
    }

    [Fact]
    public void TheThingThatKilledHerIsStillAboard()
    {
        Assert.True(ArchiveNode.CauseMayCarryOne(Derelict.WreckCause.VentedByOneOfTheirOwn));
        Assert.True(ArchiveNode.IsAboard("any-hull-at-all", Derelict.WreckCause.VentedByOneOfTheirOwn));
    }
}
