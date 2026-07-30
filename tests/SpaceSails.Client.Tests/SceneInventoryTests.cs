using System.Text;

using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// THE SCENE INVENTORY AUDIT. Owner, 2026-07-30, after a playtest of the sweep team turned up three bugs in
/// minutes: <i>"Starting every scene and seeing if it has all the parts in right place is the best way to find
/// bugs 😎👍"</i>
///
/// <para>He is right, and the evidence in this repo is close to embarrassing. Almost every expensive bug here was
/// invisible to reasoning and to Core tests and obvious the moment somebody opened the scene: both of her
/// atmosphere boards drawn, labelled, glowing and dead; the wreck's spine doorways never cut at all; a moon
/// constant that made Reevers unable to touch the captain aboard any derelict in any build; a motion fan reporting
/// an empty hull while three professionals walked it. <b>The parts exist — they are simply not in the right place,
/// or not wired to anything.</b> Nothing errors, the feature silently never fires, and every test stays green.</para>
///
/// <para>So this is that method turned into something CI does on every PR rather than something a person remembers
/// to do. The crowding audit already asks <i>can each console be pressed</i>; this asks the question before it:
/// <b>is the console there at all.</b></para>
/// </summary>
public sealed class SceneInventoryTests
{
    /// <summary>
    /// What each family of scene must contain to be that scene at all. Not a wish-list — every entry is something
    /// whose absence would leave a captain standing in a room that cannot do its job, and the sort of absence that
    /// reads on screen as "nothing happens when I press E".
    /// </summary>
    private static IReadOnlyList<DeckPlan.ConsoleKind> RequiredFor(string family) => family switch
    {
        // Her own ship: fly her, load her, leave her, and the three boards she got this month.
        "ship" =>
        [
            DeckPlan.ConsoleKind.Helm,
            DeckPlan.ConsoleKind.NavPost,
            DeckPlan.ConsoleKind.Cargo,
            DeckPlan.ConsoleKind.ShuttleAirlock,
            DeckPlan.ConsoleKind.ShipValves,
            DeckPlan.ConsoleKind.ShipScuttle,
            DeckPlan.ConsoleKind.ShipDoor,
            DeckPlan.ConsoleKind.Vent,          // the charge dump
            DeckPlan.ConsoleKind.MedKit,
            DeckPlan.ConsoleKind.Cantina,
        ],

        // A derelict: the way home, the two things you came for, and the four fittings every hull carries.
        "wreck" =>
        [
            DeckPlan.ConsoleKind.ShuttleAirlock,
            DeckPlan.ConsoleKind.WreckEvidence,
            DeckPlan.ConsoleKind.WreckSalvage,
            DeckPlan.ConsoleKind.WreckPlacard,
            DeckPlan.ConsoleKind.WreckValves,
            DeckPlan.ConsoleKind.WreckScuttle,
            DeckPlan.ConsoleKind.WreckBridgePanel,
            DeckPlan.ConsoleKind.WreckPressureDoor,
        ],

        // A berth: somebody to drink with, somebody to buy from, and a way ashore.
        "haven" =>
        [
            DeckPlan.ConsoleKind.Barkeep,
            DeckPlan.ConsoleKind.BarPatron,
            DeckPlan.ConsoleKind.Cantina,
            DeckPlan.ConsoleKind.Hatch,
        ],

        // Walked ground: the way back up the tube, and the kiosk that sells you the shirt.
        _ =>
        [
            DeckPlan.ConsoleKind.SurfaceAirlock,
            DeckPlan.ConsoleKind.Kiosk,
        ],
    };

    /// <summary>
    /// LAW ONE — EVERY SCENE HAS ITS PARTS. The owner's method, as a table.
    /// </summary>
    [Theory]
    [MemberData(nameof(Scenes.Every), MemberType = typeof(Scenes))]
    public void EverySceneCarriesEveryPartItsFamilyNeeds(string sceneName)
    {
        DeckPlan plan = Scenes.Build(sceneName);
        HashSet<DeckPlan.ConsoleKind> present = [.. plan.Consoles.Select(c => c.Kind)];

        var missing = RequiredFor(Scenes.FamilyOf(sceneName))
            .Where(k => !present.Contains(k))
            .ToList();

        Assert.True(missing.Count == 0,
                    $"{sceneName} is missing {string.Join(", ", missing)} — a captain would walk to where it " +
                    "should be and press E at nothing");
    }

    /// <summary>
    /// LAW TWO — AND HER OWN CONTROLS TRAVEL WITH HER. A captain clamped on at a berth or set down on a moon is
    /// still aboard a ship: her valves, her hatches and her charges have to be reachable from the docked and
    /// landed decks too, not only from the bare one.
    ///
    /// <para>This is a real seam rather than a theoretical one — <c>HavenInterior</c> seeds its consoles FROM
    /// <c>DeckPlan.Ship.Consoles</c>, so anything added to her that forgets to travel would be quietly absent
    /// exactly where a captain spends most of their time.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Scenes.Every), MemberType = typeof(Scenes))]
    public void HerOwnBoardsTravelToEveryPlaceSheGoes(string sceneName)
    {
        if (Scenes.FamilyOf(sceneName) == "wreck")
        {
            return;   // aboard somebody else's hull, hers is over the lock and out of reach by design
        }

        DeckPlan plan = Scenes.Build(sceneName);
        HashSet<DeckPlan.ConsoleKind> present = [.. plan.Consoles.Select(c => c.Kind)];

        foreach (DeckPlan.ConsoleKind hers in new[]
                 {
                     DeckPlan.ConsoleKind.ShipValves,
                     DeckPlan.ConsoleKind.ShipScuttle,
                     DeckPlan.ConsoleKind.ShipDoor,
                     DeckPlan.ConsoleKind.Helm,
                 })
        {
            Assert.True(present.Contains(hers),
                        $"{sceneName} cannot reach the ship's own {hers} — she is still your ship while you are here");
        }
    }

    /// <summary>
    /// LAW THREE — EVERY WRECK PRESENTS THE IDENTICAL FITTINGS, and this one enforces an owner ruling that a
    /// separation test cannot see. He put it plainly: <i>"even without reevers we should have those tech we used
    /// here available, to not give a clue that they might not be needed."</i>
    ///
    /// <para>Fittings were once gated on the infested hull, so FINDING a valve board named the cause before any
    /// evidence had been read — which undid every <c>Derelict.MisreadsAs</c> misdirection in the game. A wreck
    /// must differ only in her EVIDENCE. Comparing whole inventories catches the reverse mistake too: a fitting
    /// added to one hull for one scene's sake and to no other.</para>
    /// </summary>
    [Fact]
    public void EveryWreckIsFittedIdenticallySoTheFittingsNeverNameTheCause()
    {
        var byCause = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string scene in Scenes.Names().Where(n => Scenes.FamilyOf(n) == "wreck"))
        {
            IEnumerable<string> kinds = Scenes.Build(scene).Consoles
                .Select(c => c.Kind.ToString())
                .Distinct()
                .OrderBy(k => k, StringComparer.Ordinal);
            byCause[scene] = string.Join(" ", kinds);
        }

        var distinct = byCause.Values.Distinct(StringComparer.Ordinal).ToList();
        Assert.True(distinct.Count == 1,
                    "wreck fittings differ between causes, which tells the captain what she died of before they " +
                    "have read a thing:\n" +
                    string.Join("\n", byCause.Select(kv => $"  {kv.Key} :: {kv.Value}")));
    }

    /// <summary>
    /// LAW FOUR — AND THE ONE THAT CATCHES THE BUG CLASS DIRECTLY. Every console kind the [E] dispatcher answers
    /// must actually be PLACED somewhere — either by one of these scenes, or by a runtime lane named in the
    /// exemption list below with a reason.
    ///
    /// <para>A kind that nothing places is a feature wired to a key that no captain can ever reach: the exact
    /// shape of "the parts are not in the right place", and completely invisible to every other test in the
    /// repository because the code compiles, the dispatcher handles it, and the deck simply never contains one.
    /// Adding a new kind now means placing it or explaining where it comes from.</para>
    /// </summary>
    [Fact]
    public void EveryConsoleKindIsPlacedSomewhereOrIsExplained()
    {
        HashSet<DeckPlan.ConsoleKind> placed = [];
        foreach (string scene in Scenes.Names())
        {
            foreach (DeckPlan.ConsoleSpot spot in Scenes.Build(scene).Consoles)
            {
                placed.Add(spot.Kind);
            }
        }

        var unplaced = new StringBuilder();
        foreach (DeckPlan.ConsoleKind kind in Enum.GetValues<DeckPlan.ConsoleKind>())
        {
            if (kind == DeckPlan.ConsoleKind.None || placed.Contains(kind) || PlacedAtRuntime.ContainsKey(kind))
            {
                continue;
            }
            unplaced.AppendLine($"  {kind} — placed by nothing, reachable by nobody");
        }

        Assert.True(unplaced.Length == 0,
                    "console kinds the [E] key dispatches on that no scene ever puts on a deck. Either place " +
                    "one, or add it to PlacedAtRuntime with the lane that spawns it:\n" + unplaced);
    }

    /// <summary>
    /// The kinds that legitimately do not appear in a freshly built deck, and WHERE they come from instead. Each
    /// one is spawned into a live plan by a lane the static builders cannot know about — so the exemption is a
    /// statement about the lane, not a shrug.
    /// </summary>
    private static readonly Dictionary<DeckPlan.ConsoleKind, string> PlacedAtRuntime = new()
    {
        [DeckPlan.ConsoleKind.DigSite] = "the excursion seeds dig sites per landing (#346 beach-comber)",
        [DeckPlan.ConsoleKind.Stash] = "a buried cache appears where the captain buried it",
        [DeckPlan.ConsoleKind.SealedDoor] = "#371 expedition regions: a forced door appended mid-excursion",
        [DeckPlan.ConsoleKind.DiscoveryCache] = "#371 expedition regions: the cache behind a forced door",
        [DeckPlan.ConsoleKind.DrillPoint] = "#371 expedition regions: a drill point inside a forced chamber",
        [DeckPlan.ConsoleKind.SecretDoor] = "the secret-lab lane appends its own door when the site is found",
        [DeckPlan.ConsoleKind.LabConsole] = "the secret-lab lane's own interior",
        [DeckPlan.ConsoleKind.LabCache] = "the secret-lab lane's own interior",
        [DeckPlan.ConsoleKind.Airlock] = "the bare ship's tube; a docked or landed deck replaces it with the " +
                                         "berth's Hatch or the surface's SurfaceAirlock",
        [DeckPlan.ConsoleKind.Shuttle] = "the shuttle in its cradle — present on her own deck, absent once flown",
        [DeckPlan.ConsoleKind.SelfieSpot] = "#379 selfie spots exist at four of the seven berths by design",
    };

    /// <summary>
    /// LAW FIVE — NOTHING THE CAPTAIN NEEDS EXACTLY ONE OF IS DOUBLED. Two helms or two airlocks on one deck is
    /// not a crowding problem (they could be a hull apart and pass the separation audit); it is a scene assembled
    /// twice, and the second one will be the dead one.
    ///
    /// <para><b>`ShipValves` is deliberately NOT on this list, and that is a finding rather than an omission.</b>
    /// Its first run reported "ship has 2 × ShipValves" on her and on every deck derived from her — and the second
    /// one is the BRIDGE REPEATER, which the owner asked for by name off the back of Star Trek: <i>"At Star Trek
    /// Enterprise in the Next Gen they also had that kind of Engineering / Bridge duplication, so it makes sense to
    /// not lock the engineering station to uselessness while the bridge works."</i> Two consoles opening one board
    /// is the whole point of owning a ship rather than salvaging one. Written down here so that nobody — including
    /// me, in three months — reads the duplicate as a bug and deletes the feature.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Scenes.Every), MemberType = typeof(Scenes))]
    public void TheSingularPartsAreSingular(string sceneName)
    {
        DeckPlan plan = Scenes.Build(sceneName);

        foreach (DeckPlan.ConsoleKind once in new[]
                 {
                     DeckPlan.ConsoleKind.Helm,
                     DeckPlan.ConsoleKind.NavPost,
                     DeckPlan.ConsoleKind.ShuttleAirlock,
                     DeckPlan.ConsoleKind.ShipScuttle,
                     DeckPlan.ConsoleKind.WreckScuttle,
                     DeckPlan.ConsoleKind.WreckPlacard,
                 })
        {
            int count = plan.Consoles.Count(c => c.Kind == once);
            Assert.True(count <= 1, $"{sceneName} has {count} × {once} — the second one will be the dead one");
        }
    }

    /// <summary>
    /// …AND THE REPEATER IS STILL THERE. The other half of the exemption above: having decided two valve consoles
    /// are legal, this pins that there are exactly TWO, so the day somebody trims the "duplicate" the audit says
    /// so instead of the owner discovering it at the bridge with a fire aft.
    /// </summary>
    [Fact]
    public void SheHasBothAValveStationAndABridgeRepeater()
    {
        DeckPlan.ConsoleSpot[] valves =
            [.. DeckPlan.Ship.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.ShipValves)];

        Assert.Equal(2, valves.Length);
        Assert.Contains(valves, v => v.Label.Contains("repeater", StringComparison.OrdinalIgnoreCase));

        // …and they are at opposite ends of her, which is the entire point of a repeater.
        double span = Math.Abs(valves[0].X - valves[1].X);
        Assert.True(span > 20, $"a repeater {span:0.#} du from the station it repeats is not a repeater");
    }
}
