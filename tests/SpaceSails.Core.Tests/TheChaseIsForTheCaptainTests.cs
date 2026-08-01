using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #580 · HEAT FOLLOWS THE CAPTAIN, NOT THE SHIP.
///
/// <para>Owner, walking Miranda while his ship sat docked and empty behind him, in four messages:
/// <i>"moving on the planet should NOT cause HEAT"</i> · <i>"any heat should happen on the surface or site,
/// not in space"</i> · <i>"the heat must follow the captain, not the ship"</i> · <i>"zero heat against empty
/// ship (we don't have any playing there, clearly there is some lights on keeper on ship but we do not play
/// that)"</i> — and the play argument that settles all of it: <i>"we don't want to be guarding our parking
/// lot ... that is not good game play :-D"</i>.</para>
///
/// <para>Two live bugs sat under that. An Old One laying hands on a suit raised the SHIP's heat by one, so a
/// long excursion quietly taxed a hull nobody was aboard. And the pursuit loop kept running through the
/// excursion, so a hunter could close on that empty hull and CATCH it — a boarding demand delivered to a
/// captain standing in a vacuum suit on a moon.</para>
///
/// <para>The half that lives in Core is the hold: what a chase does while there is nobody at the controls.
/// The other half — the reever catch no longer touching <c>_heat</c> — is one deleted line in
/// <c>Map.Surface.cs</c>, guarded by nothing but the comment explaining why it must stay deleted.</para>
/// </summary>
public sealed class TheChaseIsForTheCaptainTests
{
    private static HunterState Wolf(double atSimTime) => EncounterRule.SpawnHunter(
        "wolf-1", "MERIDIAN", "mars", new Vector2d(0, 0), new Vector2d(0, 0), atSimTime);

    [Fact]
    public void HoldingStationDoesNotMoveTheWolfOneMetre()
    {
        HunterState held = EncounterRule.HoldStation(Wolf(0), 600);

        Assert.Equal(0, held.State.Position.X);
        Assert.Equal(0, held.State.Position.Y);
        Assert.Equal(0, held.State.Velocity.X);
        Assert.Equal(0, held.State.Velocity.Y);
    }

    [Fact]
    public void HoldingStationCarriesTheClockSoTheChaseCannotSPRINGOnYourReturn()
    {
        // The point of carrying SimTime forward rather than freezing it. If the hunter's clock stayed at the
        // moment of descent, the pursuit loop would integrate the ENTIRE excursion in one burst the instant
        // the captain came back aboard — landing the wolf on top of a ship that had, from the player's view,
        // just been sitting at a dock. Waiting is not the same as being owed the time.
        HunterState held = EncounterRule.HoldStation(Wolf(0), 3_600);

        Assert.Equal(3_600, held.State.SimTime);
    }

    [Fact]
    public void AWolfThatHeldStationAllExcursionHasClosedNoDistance()
    {
        // The whole excursion, held: at the end of it the wolf is exactly where it started. Compare with the
        // same wolf allowed to chase, which is the behaviour that reached and caught an empty hull.
        ShipState parked = new(new Vector2d(4e8, 0), new Vector2d(0, 0), 0);

        HunterState held = Wolf(0);
        HunterState chasing = Wolf(0);
        for (double t = 0; t <= 3_000; t += EncounterRule.HunterStepSeconds)
        {
            held = EncounterRule.HoldStation(held, t);
            chasing = EncounterRule.AdvanceHunter(chasing, parked with { SimTime = t }, t);
        }

        double heldGap = (parked.Position - held.State.Position).Length;
        double chasedGap = (parked.Position - chasing.State.Position).Length;

        Assert.Equal(4e8, heldGap, 1e-6);
        Assert.True(chasedGap < heldGap,
            "the comparison is meaningless unless an un-held wolf actually closes — check the fixture.");
    }

    [Fact]
    public void HoldingStationLeavesEverythingELSEAboutTheWolfAlone()
    {
        // It is a pause, not a reset: identity, origin, activation and the warning shots it has already
        // taken all survive, so the chase resumes as the same chase.
        HunterState wolf = Wolf(120) with { WarningShotsTaken = 2 };
        HunterState held = EncounterRule.HoldStation(wolf, 999);

        Assert.Equal(wolf.Id, held.Id);
        Assert.Equal(wolf.Callsign, held.Callsign);
        Assert.Equal(wolf.OriginBodyId, held.OriginBodyId);
        Assert.Equal(wolf.ActivationSimTime, held.ActivationSimTime);
        Assert.Equal(2, held.WarningShotsTaken);
        Assert.False(held.CaughtPlayer);
    }
}
