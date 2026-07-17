namespace SpaceSails.Core.Tests;

/// <summary>PR-BUSTED · The Bolivia (owner ruling §5.3). A heat-3 resist runs the dice-scripted last
/// stand — initiative, three beats, mostly auto-played — ending FLEE (break clear at heat 2) or the
/// FREEZE-FRAME (sepia game-over → brain-backup resurrection). Deterministic; both endings
/// reachable.</summary>
public class BoliviaEncounterTests
{
    [Fact]
    public void Script_HasBeats_EachWithChoices()
    {
        Assert.Equal(3, BoliviaEncounter.Script.Count);
        Assert.All(BoliviaEncounter.Script, b => Assert.NotEmpty(b.Choices));
    }

    [Fact]
    public void Resolve_IsDeterministic_PerSeed()
    {
        BoliviaEncounter.Resolution a = BoliviaEncounter.Resolve(1234, heat: 3);
        BoliviaEncounter.Resolution b = BoliviaEncounter.Resolve(1234, heat: 3);
        Assert.Equal(a.NetMargin, b.NetMargin);
        Assert.Equal(a.Ending, b.Ending);
    }

    [Fact]
    public void BothEndings_AreReachable_AcrossSeeds()
    {
        bool sawFlee = false, sawFreeze = false;
        for (ulong seed = 0; seed < 500 && !(sawFlee && sawFreeze); seed++)
        {
            BoliviaEncounter.Ending e = BoliviaEncounter.Resolve(seed, heat: 3).Ending;
            sawFlee |= e == BoliviaEncounter.Ending.Flee;
            sawFreeze |= e == BoliviaEncounter.Ending.FreezeFrame;
        }

        Assert.True(sawFlee, "some seeds must let the player fight clear");
        Assert.True(sawFreeze, "some seeds must end in the freeze-frame");
    }

    [Fact]
    public void Decide_NetMargin_MapsToEnding()
    {
        Assert.Equal(BoliviaEncounter.Ending.Flee, BoliviaEncounter.Decide(0));   // dead-even breaks clear
        Assert.Equal(BoliviaEncounter.Ending.Flee, BoliviaEncounter.Decide(5));
        Assert.Equal(BoliviaEncounter.Ending.FreezeFrame, BoliviaEncounter.Decide(-1));
    }

    [Fact]
    public void AggressiveChoices_ImproveFleeOdds_OverGivingGround()
    {
        int chargeFlees = 0, boatFlees = 0;
        var charge = new[] { "charge", "return", "sprint" };
        var giveGround = new[] { "boat", "dig", "cover" };
        for (ulong seed = 0; seed < 500; seed++)
        {
            if (BoliviaEncounter.Resolve(seed, 3, charge).Ending == BoliviaEncounter.Ending.Flee) chargeFlees++;
            if (BoliviaEncounter.Resolve(seed, 3, giveGround).Ending == BoliviaEncounter.Ending.Flee) boatFlees++;
        }

        Assert.True(chargeFlees > boatFlees, "guns blazing beats giving ground for breaking clear");
    }

    [Fact]
    public void StandingHelpers_ImproveFleeOdds()
    {
        int helped = 0, bare = 0;
        var jammer = new List<DiceModifier> { new("Boarding-nets jammer", +2) };
        for (ulong seed = 0; seed < 500; seed++)
        {
            if (BoliviaEncounter.Resolve(seed, 3, null, jammer).Ending == BoliviaEncounter.Ending.Flee) helped++;
            if (BoliviaEncounter.Resolve(seed, 3).Ending == BoliviaEncounter.Ending.Flee) bare++;
        }

        Assert.True(helped > bare, "a purchased helper improves the last stand");
    }

    [Fact]
    public void Resolve_RecordsEveryBeat_WithItsRoll()
    {
        BoliviaEncounter.Resolution r = BoliviaEncounter.Resolve(9, heat: 3, new[] { "brace", "smoke", "cover" });
        Assert.Equal(3, r.Beats.Count);
        Assert.Equal("brace", r.Beats[0].ChoiceId);
        Assert.Equal("smoke", r.Beats[1].ChoiceId);
        Assert.Equal("cover", r.Beats[2].ChoiceId);
    }

    [Fact]
    public void UnknownChoice_AutoPlaysFirstOption()
    {
        BoliviaEncounter.Resolution r = BoliviaEncounter.Resolve(9, heat: 3, new[] { "nonsense" });
        Assert.Equal(BoliviaEncounter.Script[0].Choices[0].Id, r.Beats[0].ChoiceId);
    }
}
