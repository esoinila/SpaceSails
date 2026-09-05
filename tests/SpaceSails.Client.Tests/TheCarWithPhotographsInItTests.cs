using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #233 · THE CAR WITH PHOTOGRAPHS IN IT — the played half.
///
/// <para>The Core suite proves the rules: one car in four, the fence's derived price, the three sentences.
/// This one proves the GAME: the errand walked from the scope press to the hole in the ground, on the
/// shipping page, through the shipping methods, with the shipping scenario in it.</para>
///
/// <para>The arc is four beats — scan (the bird asks where the car is), glint (#240's existing first
/// responder), alongside (the bird answers itself), aboard — and then three doors, each driven on its own
/// fresh page so no ending can pass by leaning on another's leftovers.</para>
///
/// <para><b>Proven RED</b>, guard by guard, by reverting the arm each one watches — noted on each fact.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCarWithPhotographsInItTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const string TheHunt = "DUDE. WHERE. Is. The CAR?!";
    private const string TheFind = "You found your CAAAR!";
    private const int TheContract = 4200;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    // ── BEAT ONE · THE SCOPE GOES HUNTING ─────────────────────────────────────────────────────────────

    /// <summary>The bird asks where the car is when the hunt starts — once per hunt, however many times the
    /// captain presses 🔭.
    ///
    /// <para><b>Proven RED</b> two ways: the latch removed (three presses, three squawks), and the
    /// <c>force: true</c> dropped (the second press inside the cooldown says nothing, which is the same
    /// failure wearing a different hat).</para></summary>
    [Fact]
    public void PointingTheScopeAtTheRoadster_AsksWhereTheCarIs_Once()
    {
        Pages.Map map = Booted();

        Invoke(map, "SquawkTheCarHunt", Derelict.RoadsterBodyId);
        Assert.Equal(TheHunt, Bubble(map));

        Set(map, "_parrotSquawk", null);
        Invoke(map, "SquawkTheCarHunt", Derelict.RoadsterBodyId);
        Invoke(map, "SquawkTheCarHunt", Derelict.RoadsterBodyId);
        Assert.Null(Bubble(map));
    }

    /// <summary>…AND ONLY FOR THE CAR. Every other intel fix in the game is somebody's timetable, and the
    /// joke does not survive being told about a freighter.</summary>
    [Fact]
    public void NoOtherIntelFix_EverAsksWhereTheCarIs()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);

        foreach (CelestialBody body in eph.Bodies.Where(b => !Derelict.IsWreckBody(b.Id)))
        {
            Pages.Map map = Booted();
            Invoke(map, "SquawkTheCarHunt", body.Id);
            Assert.Null(Bubble(map));
        }
    }

    /// <summary>THE PRESS REALLY REACHES IT. The latch above is only worth having if the shipping button
    /// calls it — and all three 🔭 doors (the quest card, the ledger row, the sky menu's intel fix) come
    /// through the one funnel, which is where the call is written.</summary>
    [Fact]
    public void TheOneScopeFunnel_IsWhereTheAskingHangs()
    {
        string scope = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Deck.Scope.cs"));

        int at = scope.IndexOf("private void PointScopeWhereIntelSays(", StringComparison.Ordinal);
        Assert.True(at >= 0, "the scope's one funnel has been renamed — this guard has drifted.");
        int ends = scope.IndexOf("\n    }", at, StringComparison.Ordinal);
        Assert.Contains("SquawkTheCarHunt(intel.BodyId)", scope[at..ends], StringComparison.Ordinal);
    }

    // ── BEAT TWO · THE GLINT ──────────────────────────────────────────────────────────────────────────

    /// <summary>#240's crossing keeps its own first responder, unchanged — the pulse and the reveal cue that
    /// have always answered a resolved wreck. This feature adds nothing here and must not: a second sentence
    /// at the glint would be two things speaking for one moment.</summary>
    [Fact]
    public void TheGlint_IsStillTheFirstResponderThatAlreadyShipped()
    {
        Pages.Map map = Booted();
        Get<HashSet<string>>(map, "_hiddenBodyIds").Add(Derelict.RoadsterBodyId); // she is off the charts
        string said = (string)Invoke(map, "WreckRevealMessage", Derelict.RoadsterBodyId)!;

        Assert.True((bool)Invoke(map, "IsBodyHidden", Derelict.RoadsterBodyId)!);
        Invoke(map, "RevealBody", Derelict.RoadsterBodyId, said, true);
        Assert.False((bool)Invoke(map, "IsBodyHidden", Derelict.RoadsterBodyId)!);

        Assert.Contains("cherry-red glint", Pulse(map), StringComparison.Ordinal);
        Assert.Null(Bubble(map)); // the bird does not talk over the scope's own moment
    }

    // ── BEATS THREE AND FOUR · ALONGSIDE, AND ABOARD ──────────────────────────────────────────────────

    /// <summary>THE WHOLE PICKUP, on the shipping <c>CheckFetchPickup</c>: the bird answers itself, the chip
    /// comes aboard into the pocket, the job advances, and what the captain is told is the object's own
    /// canon line rather than a report about a wallet.
    ///
    /// <para><b>Proven RED</b> by removing the <c>TheCarHasTheChip</c> arm: the wallet's sentence lands and
    /// the pocket stays empty.</para></summary>
    [Fact]
    public void ComingAlongsideTheTwin_AnswersTheBirdAndPutsTheChipInThePocket()
    {
        (Pages.Map map, Pages.Map.Quest job) = AFetchJobAtTheWreck(DockRule.AlongsideMeters * 0.95, chip: true);

        Invoke(map, "CheckFetchPickup");

        Assert.Equal(TheFind, Bubble(map));
        Assert.Equal(Pages.Map.QuestState.PickedUp, job.State);
        Assert.NotNull(CompromisingChip.InThePocket(Carried(map)));
        Assert.Equal(CompromisingChip.LookCardLine, Pulse(map));
        Assert.DoesNotContain("wallet", Pulse(map), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>…AND THE ORDINARY CAR IS UNTOUCHED. Three cars in four still hold the wallet, still say the
    /// sentence they always said, and still put nothing in anybody's pocket — the bird's punchline is the
    /// only thing the twin lends them.</summary>
    [Fact]
    public void ComingAlongsideTheOrdinaryCar_StillPrisesTheWalletAndCarriesNothing()
    {
        (Pages.Map map, Pages.Map.Quest job) = AFetchJobAtTheWreck(DockRule.AlongsideMeters * 0.95, chip: false);

        Invoke(map, "CheckFetchPickup");

        Assert.Equal(TheFind, Bubble(map));
        Assert.Equal(Pages.Map.QuestState.PickedUp, job.State);
        Assert.Null(CompromisingChip.InThePocket(Carried(map)));
        Assert.Contains("wallet was wedged between the seats", Pulse(map), StringComparison.Ordinal);
    }

    /// <summary>THE PREMISE. Standing off at the distance the owner's own frame was taken at, nothing
    /// happens at all — so every law above is about arriving and not about existing.</summary>
    [Fact]
    public void StoodOffHalfAMillionKilometres_NothingIsPrisedAndNobodySpeaks()
    {
        (Pages.Map map, Pages.Map.Quest job) = AFetchJobAtTheWreck(4.99721e8, chip: true);

        Invoke(map, "CheckFetchPickup");

        Assert.Equal(Pages.Map.QuestState.Active, job.State);
        Assert.Null(Bubble(map));
        Assert.Null(CompromisingChip.InThePocket(Carried(map)));
    }

    // ── ENDING ONE · THE COUNTER ──────────────────────────────────────────────────────────────────────

    /// <summary>GIVE IT BACK. The contract pays the contract's own number — the chip does not make the job
    /// worth more to the man who commissioned it — the pocket lets go, the history is seeded quietly, and
    /// the client says his one sentence.
    ///
    /// <para><b>Proven RED</b> by falling through to the wallet's hand-off: the coin is right and the words
    /// are the wrong man's.</para></summary>
    [Fact]
    public void HandingItBack_PaysTheContractAndTheClientSaysHisOneSentence()
    {
        (Pages.Map map, Pages.Map.Quest job) = ACarriedChip();
        Set(map, "_credits", 100);

        Invoke(map, "DeliverFetch", job);

        Assert.Equal(100 + TheContract, Get<int>(map, "_credits"));
        Assert.Equal(Pages.Map.QuestState.TurnedIn, job.State);
        Assert.Null(CompromisingChip.InThePocket(Carried(map)));
        Assert.Equal(CompromisingChip.ClientLine, Pulse(map));
    }

    /// <summary>…AND ONCE. A second press on the same man pays nothing: the job is closed and the pocket is
    /// empty, and either of those alone has to be enough to stop him.</summary>
    [Fact]
    public void HandingItBackTwice_PaysOnce()
    {
        (Pages.Map map, Pages.Map.Quest job) = ACarriedChip();
        Set(map, "_credits", 0);

        Invoke(map, "DeliverFetch", job);
        Invoke(map, "DeliverFetch", job);

        Assert.Equal(TheContract, Get<int>(map, "_credits"));
    }

    // ── ENDING TWO · THE DESK ─────────────────────────────────────────────────────────────────────────

    /// <summary>SELL IT. More than the contract, priced off the contract, and the difference is not free:
    /// one band of heat lands on the book of the outfit that runs the ground the desk was worked from. The
    /// fence names an appetite and never a buyer.
    ///
    /// <para><b>Proven RED</b> three ways: the heat call removed (the outfit's book stays clean), the price
    /// replaced by the contract's own reward (the sale stops being worth anything), and the pocket left full
    /// (the captain sells and then hands back the same chip).</para></summary>
    [Fact]
    public void SellingItAtTheDesk_PaysMoreThanTheContractAndCostsOneBandOfHeat()
    {
        (Pages.Map map, Pages.Map.Quest job) = ACarriedChip();
        string haven = DockTheShipWhereTheDeskWorks(map);
        string outfit = SiteOperator.Of(haven).Id;
        var book = Get<ContactLedger>(map, "_contacts");
        Set(map, "_credits", 0);

        Assert.Equal(0, IllegalHeat.HeatAt(book, outfit));
        int quoted = (int)Invoke(map, "ChipFencePrice")!;
        Assert.Equal(CompromisingChip.FencePrice(TheContract), quoted);
        Assert.True(quoted > TheContract, "the desk quoted no more than the man who ordered the car.");

        Invoke(map, "SellTheChipToTheFence");

        Assert.Equal(quoted, Get<int>(map, "_credits"));
        Assert.Equal(IllegalHeat.ABand, IllegalHeat.HeatAt(book, outfit));
        Assert.Equal(Pages.Map.QuestState.TurnedIn, job.State);
        Assert.Null(CompromisingChip.InThePocket(Carried(map)));
        Assert.Equal(CompromisingChip.FenceLine, Pulse(map));
    }

    /// <summary>SOLD IS SOLD. Walk from the desk to the Fixer's table afterwards and there is nothing to
    /// hand over and nothing to be paid — which is the whole reason the three endings are three and not a
    /// menu you can order twice from.</summary>
    [Fact]
    public void SellingIt_LeavesNothingForTheClientToPayFor()
    {
        (Pages.Map map, Pages.Map.Quest job) = ACarriedChip();
        DockTheShipWhereTheDeskWorks(map);
        Invoke(map, "SellTheChipToTheFence");

        Set(map, "_credits", 0);
        Invoke(map, "DeliverFetch", job);

        Assert.Equal(0, Get<int>(map, "_credits"));
    }

    /// <summary>NO CHIP, NO ROW. The desk's verb is drawn where it applies and nowhere else — a captain who
    /// is not carrying one is never shown a button that would refuse him.</summary>
    [Fact]
    public void TheDeskOffersNothing_WhenThereIsNoChipInThePocket()
    {
        (Pages.Map map, Pages.Map.Quest job) = AFetchJobAtTheWreck(DockRule.AlongsideMeters * 0.95, chip: false);
        DockTheShipWhereTheDeskWorks(map);
        job.State = Pages.Map.QuestState.PickedUp;

        Assert.Null(Invoke(map, "ChipFencePrice"));
    }

    // ── ENDING THREE · THE HOLE ───────────────────────────────────────────────────────────────────────

    /// <summary>BURY IT. The chest's manifest lists it by the chip's own name and flags it hot, the pocket
    /// lets go, the job closes — and NOTHING IS SAID. There is no counterparty to a hole, so the silence is
    /// the ending rather than a line nobody has written yet.
    ///
    /// <para><b>Proven RED</b> two ways: a sentence added to the bury arm (the silence guard goes), and the
    /// manifest line dropped (the chest goes into the ground holding nothing).</para></summary>
    [Fact]
    public void BuryingIt_ListsTheChipOnTheManifestAndSaysNothingAtAll()
    {
        (Pages.Map map, Pages.Map.Quest job) = ACarriedChip();
        Invoke(map, "ShowPulseMessage", "⛏ …", PulseRank.Status);
        string before = Pulse(map);

        var manifest = new List<CacheCargo> { new("He3", 3, Hot: false) };
        Invoke(map, "TheChipGoesInTheChest", manifest);

        Assert.Contains(manifest, c => c.CargoClass == CompromisingChip.Name && c.Units == 1 && c.Hot);
        Assert.Contains(manifest, c => c.CargoClass == "He3"); // the rest of the chest is untouched
        Assert.Null(CompromisingChip.InThePocket(Carried(map)));
        Assert.Equal(Pages.Map.QuestState.TurnedIn, job.State);
        Assert.Equal(before, Pulse(map));
    }

    /// <summary>AN EMPTY POCKET BURIES NOTHING. Every other chest in the game goes into the ground exactly
    /// as it always did.</summary>
    [Fact]
    public void BuryingAChestWithoutTheChip_ChangesNothingAboutTheChest()
    {
        (Pages.Map map, _) = AFetchJobAtTheWreck(DockRule.AlongsideMeters * 0.95, chip: false);

        var manifest = new List<CacheCargo> { new("He3", 3, Hot: false) };
        Invoke(map, "TheChipGoesInTheChest", manifest);

        Assert.Single(manifest);
    }

    /// <summary>THE BURY REALLY REACHES IT — asked of the shipping method, because a hook nobody calls is a
    /// feature that only exists in a test.</summary>
    [Fact]
    public void TheChestThatGoesInTheGround_IsWhereTheChipJoinsIt()
    {
        string dig = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Surface.Dig.cs"));

        int at = dig.IndexOf("private void BuryChestHere(", StringComparison.Ordinal);
        Assert.True(at >= 0, "the bury has been renamed — this guard has drifted.");
        int ends = dig.IndexOf("\n    }", at, StringComparison.Ordinal);
        string body = dig[at..ends];

        Assert.Contains("TheChipGoesInTheChest(ex.PendingCargo)", body, StringComparison.Ordinal);
        // …and it joins the manifest AFTER the hold has been settled, because the chip was never in the hold
        // and HoldAfterBurying must not be asked to subtract it from anything.
        Assert.True(
            body.IndexOf("HoldAfterBurying", StringComparison.Ordinal)
            < body.IndexOf("TheChipGoesInTheChest", StringComparison.Ordinal),
            "the chip joins the manifest before the hold is settled — the hold will be asked to give up a "
            + "unit it never had.");
    }

    // ── THE VAULT ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>WHICH CAR THIS IS SURVIVES THE VAULT. The deal is made once, at the offer, and never rolled
    /// again — so a captain who saves at the scope and resumes at the wreck has to find the same car. Driven
    /// through the shipping save and load halves.
    ///
    /// <para><b>Proven RED</b> by dropping the field from the saved bag: the resumed job prises a wallet out
    /// of a car that had photographs in it.</para></summary>
    [Fact]
    public void TheDealSurvivesTheVault_AndSoDoesTheOrdinaryCar()
    {
        foreach (bool chip in new[] { true, false })
        {
            (Pages.Map map, Pages.Map.Quest job) = AFetchJobAtTheWreck(4.99721e8, chip);

            object section = Invoke(map, "BuildQuestsSection")!;
            Pages.Map resumed = Booted();
            Invoke(resumed, "ApplyObligationsAndQuests", section);

            var restored = Get<List<Pages.Map.Quest>>(resumed, "_quests");
            Assert.Single(restored);
            Assert.Equal(job.Id, restored[0].Id);
            Assert.Equal(chip, (bool)Invoke(resumed, "TheCarHasTheChip", restored[0])!);
        }
    }

    /// <summary>THE TAG IS PLUMBING AND NEVER PROSE. The deal rides the quest's <c>Pin</c>, which the crack
    /// job prints on its own checklist step — so the one thing that could go wrong with reusing it is that
    /// somebody one day prints a fetch's. Asked of every sentence the job can produce at every state it can
    /// be in, off the shipping compass and the shipping brief.
    ///
    /// <para><b>Proven RED</b> by widening <c>Map.Quests.Compass</c>'s crack step to answer for a fetch: the
    /// captain is told to key <c>roadster-data-chip</c> into a hatch.</para></summary>
    [Fact]
    public void TheCarsTagNeverReachesASentence()
    {
        (Pages.Map map, Pages.Map.Quest job) = AFetchJobAtTheWreck(4.99721e8, chip: true);

        foreach (Pages.Map.QuestState state in Enum.GetValues<Pages.Map.QuestState>())
        {
            job.State = state;

            object step = Invoke(map, "CurrentStepOf", job)!;
            string said = step.ToString() ?? "";
            var facts = (ContractFacts)Invoke(map, "FactsFor", job)!;
            said += " " + MissionBrief.Action(facts) + " " + MissionBrief.NextLine(facts);

            Assert.DoesNotContain(CompromisingChip.FindId, said, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("chip-between-the-seats", said, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>…AND THE CHIP ITSELF RIDES THE SATCHEL HOME. It is a file on somebody, in the document
    /// sleeve, and the satchel's own round-trip is what carries it — which is why this feature added no
    /// vault section of its own.</summary>
    [Fact]
    public void TheChipItselfRoundTripsThroughTheSatchelsOwnSerialisation()
    {
        Satchel.Item chip = CompromisingChip.Found();

        Assert.True(Satchel.Item.TryParse(chip.Stored, out Satchel.Item back));
        Assert.Equal(chip, back);
        Assert.True(CompromisingChip.IsTheChip(back));
        Assert.Equal(Satchel.Compartment.Sleeve, Satchel.CompartmentOf(back.Kind));
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A fetch job in hand, the ship <paramref name="metresOut"/> from the roadster, and the car
    /// dealt one way or the other by hand — the deal itself is Core's and is swept in the Core suite; what
    /// is being driven here is what the page does with the answer.</summary>
    private static (Pages.Map Map, Pages.Map.Quest Job) AFetchJobAtTheWreck(double metresOut, bool chip)
    {
        Pages.Map map = Booted();
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);

        Vector2d wreck = eph.Position(Derelict.RoadsterBodyId, 0);
        Set(map, "_ship", new ShipState(wreck + new Vector2d(metresOut, 0), Vector2d.Zero, 0));

        var job = new Pages.Map.Quest("fetch-bench", Pages.Map.QuestKind.Fetch, "THE FIXER", "", "The Fixer",
            "Fetch the roadster's lost wallet", "[bench]", TheContract,
            DestBodyId: "the-space-bar", SourceBodyId: Derelict.RoadsterBodyId,
            Pin: chip ? CompromisingChip.FindId : null)
        {
            State = Pages.Map.QuestState.Active,
        };
        Get<List<Pages.Map.Quest>>(map, "_quests").Add(job);

        return (map, job);
    }

    /// <summary>The state every ending starts from: the twin prised, the chip in the pocket, the job in
    /// hand. Reached by actually flying it — the pickup above is the only way into this state, so an ending
    /// can never be tested against a pocket a test filled by hand.</summary>
    private static (Pages.Map Map, Pages.Map.Quest Job) ACarriedChip()
    {
        (Pages.Map map, Pages.Map.Quest job) = AFetchJobAtTheWreck(DockRule.AlongsideMeters * 0.95, chip: true);
        Invoke(map, "CheckFetchPickup");
        Assert.Equal(Pages.Map.QuestState.PickedUp, job.State);
        return (map, job);
    }

    /// <summary>Berth the ship where the dark web will actually deal — a haven with an interior, which is
    /// where a fetch's drop is anyway. Returns the berth's body id.</summary>
    private static string DockTheShipWhereTheDeskWorks(Pages.Map map)
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        CelestialBody haven = eph.Bodies.First(b => b.IsHaven && SpaceSails.Client.Rendering.HavenInterior.HasInterior(b.Id));

        Set(map, "_docked", true);
        Set(map, "_dockBodyId", haven.Id);
        Set(map, "_dockedHavenId", haven.Id);
        Assert.True((bool)Invoke(map, "DarkWebCanTrade")!, $"the desk will not deal at {haven.Id}.");
        return haven.Id;
    }

    private static Pages.Map Booted()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_ship", new ShipState(Vector2d.Zero, Vector2d.Zero, 0.0));
        return map;
    }

    /// <summary>What the bird is saying right now, or null when it is sulking.</summary>
    private static string? Bubble(Pages.Map map) => Get<string?>(map, "_parrotSquawk");

    /// <summary>The HUD's one slot — where a beat lands when there is no card in front of the captain.</summary>
    private static string Pulse(Pages.Map map) => Get<PulseSlot>(map, "_pulse").Message ?? "";

    private static IReadOnlyList<Satchel.Item> Carried(Pages.Map map) =>
        Get<IReadOnlyList<Satchel.Item>>(map, "_satchel");

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden)
            ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary");
    }

    private static string ScenarioPath(string file) => Path.Combine(RepoRoot(), "scenarios", file);
}
