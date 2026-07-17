namespace SpaceSails.Core.Tests;

/// <summary>#207 / #208 (Friday-night playtest: "I took the parcel but the mission is quite
/// unclear"). The pure text layer behind the fixes — the acceptance receipt + kind-aware
/// next-action (MissionBrief) and the dock-wins chip truth (DeskChipStatus).</summary>
public class MissionLegibilityTests
{
    // ---- Acceptance receipt (#207 (1a)) ----

    [Fact]
    public void Receipt_NamesJobAndGiver_FiledInTheLedger()
    {
        // The owner's own example: a parcel from Madam Coil.
        Assert.Equal(
            "parcel for Madam Coil — filed in the Captain's ledger (0).",
            MissionBrief.Receipt(ContractKind.CargoRun, "Madam Coil"));
    }

    [Theory]
    [InlineData(ContractKind.CargoRun, "parcel")]
    [InlineData(ContractKind.Hunt, "bounty")]
    [InlineData(ContractKind.Intel, "tip")]
    [InlineData(ContractKind.Fetch, "recovery job")]
    [InlineData(ContractKind.Crack, "break-in")]
    public void Receipt_EveryKind_HasItsOwnNounAndFilesToTheLedger(ContractKind kind, string noun)
    {
        string receipt = MissionBrief.Receipt(kind, "The Fixer");
        Assert.StartsWith($"{noun} for The Fixer", receipt);
        Assert.EndsWith("filed in the Captain's ledger (0).", receipt);
    }

    [Fact]
    public void Receipt_FallsBackWhenGiverBlank()
    {
        Assert.Equal(
            "tip for the stranger — filed in the Captain's ledger (0).",
            MissionBrief.Receipt(ContractKind.Intel, ""));
    }

    // ---- Fetch-a-cache (#223) ----

    [Fact]
    public void FetchCache_BeforeDig_SaysDigAtTheX_OnTheBody()
    {
        string next = MissionBrief.NextLine(new ContractFacts(
            ContractKind.FetchCache, "Madam Coil", DestName: "The Rusty Roadstead", DestParent: "Mars",
            CacheBody: "Phobos", PickedUp: false));
        Assert.Contains("dig at the X on Phobos", next);
        Assert.StartsWith(MissionBrief.NextPrefix, next);
    }

    [Fact]
    public void FetchCache_AfterDig_SaysDeliverTheChestToTheGiver()
    {
        string next = MissionBrief.NextLine(new ContractFacts(
            ContractKind.FetchCache, "Madam Coil", DestName: "The Rusty Roadstead", DestParent: "Mars",
            CacheBody: "Phobos", PickedUp: true));
        Assert.Contains("deliver the chest to Madam Coil", next);
        Assert.Contains("The Rusty Roadstead, Mars", next);
    }

    // ---- Next action per kind (#207 (1b), the kind audit (3)) ----

    [Fact]
    public void NextLine_CargoRun_DeliversToDestWithPlace()
    {
        Assert.Equal(
            "NEXT: deliver to The Rusty Roadstead, Mars",
            MissionBrief.NextLine(new ContractFacts(
                ContractKind.CargoRun, "Madam Coil", DestName: "The Rusty Roadstead", DestParent: "Mars")));
    }

    [Fact]
    public void NextLine_CargoRun_OmitsPlaceWhenParentUnknown()
    {
        Assert.Equal(
            "NEXT: deliver to Cinder Roost",
            MissionBrief.NextLine(new ContractFacts(
                ContractKind.CargoRun, "Madam Coil", DestName: "Cinder Roost")));
    }

    [Fact]
    public void NextLine_Hunt_NamesThePrey()
    {
        Assert.Equal(
            "NEXT: bring down Barnacle — hole her sail or board her",
            MissionBrief.NextLine(new ContractFacts(
                ContractKind.Hunt, "One-Eye Silas", TargetName: "Barnacle")));
    }

    [Fact]
    public void NextLine_Fetch_UnchartedPointsToTheScope_Charted_FliesToTheWreck()
    {
        Assert.Equal(
            "NEXT: aim the scope from the Fixer's fix (Comms 🔭) to find the roadster",
            MissionBrief.NextLine(new ContractFacts(
                ContractKind.Fetch, "The Fixer", DestName: "Venus Roost", Charted: false)));

        Assert.Equal(
            "NEXT: fly to the roadster and prise the wallet loose",
            MissionBrief.NextLine(new ContractFacts(
                ContractKind.Fetch, "The Fixer", DestName: "Venus Roost", Charted: true)));
    }

    [Fact]
    public void NextLine_Fetch_PickedUp_HandsOffAtTheDrop()
    {
        Assert.Equal(
            "NEXT: hand the wallet to The Fixer at Venus Roost",
            MissionBrief.NextLine(new ContractFacts(
                ContractKind.Fetch, "The Fixer", DestName: "Venus Roost", Charted: true, PickedUp: true)));
    }

    [Fact]
    public void NextLine_Crack_KeysThePin_ThenHandsOff_OncePickedUp()
    {
        Assert.Equal(
            "NEXT: key 4417 into hatch V-06 here, then hand it to The Fixer",
            MissionBrief.NextLine(new ContractFacts(
                ContractKind.Crack, "The Fixer", TargetName: "V-06", Pin: "4417")));

        Assert.Equal(
            "NEXT: hand the package to The Fixer here",
            MissionBrief.NextLine(new ContractFacts(
                ContractKind.Crack, "The Fixer", TargetName: "V-06", Pin: "4417", PickedUp: true)));
    }

    [Fact]
    public void NextLine_Intel_IsEmpty_BecauseItSettlesOnTheSpot()
    {
        Assert.Equal("", MissionBrief.NextLine(new ContractFacts(ContractKind.Intel, "Gilt-Eye", TargetName: "Barnacle")));
        Assert.Equal("", MissionBrief.Action(new ContractFacts(ContractKind.Intel, "Gilt-Eye", TargetName: "Barnacle")));
    }

    // ---- Dock-wins chip truth (#207 (2)) ----

    [Fact]
    public void PrimaryLine_Docked_ReadsDockedAtHaven_NotTheUnderwayPromise()
    {
        // The exact bug: banner said "docked at Cinder Roost" while the chip still said "Make for…".
        Assert.Equal(
            "Docked at Cinder Roost",
            DeskChipStatus.PrimaryLine(docked: true, "Cinder Roost", underwayLine: "Make for: Cinder Roost orbit"));
    }

    [Fact]
    public void PrimaryLine_Underway_PassesTheObjectiveThrough()
    {
        Assert.Equal(
            "Make for: Titan orbit",
            DeskChipStatus.PrimaryLine(docked: false, havenName: null, underwayLine: "Make for: Titan orbit"));
    }

    [Fact]
    public void PrimaryLine_Docked_FallsBackWhenHavenNameMissing()
    {
        Assert.Equal("Docked at the haven", DeskChipStatus.PrimaryLine(docked: true, "", underwayLine: ""));
    }

    [Fact]
    public void EtaLine_Docked_IsSuppressed_ButSurvivesUnderway()
    {
        Assert.Null(DeskChipStatus.EtaLine(docked: true, "ETA 1 h"));
        Assert.Equal("ETA 1 h", DeskChipStatus.EtaLine(docked: false, "ETA 1 h"));
    }
}
