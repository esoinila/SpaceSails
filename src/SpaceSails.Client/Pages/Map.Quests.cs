using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Quests — the work: missions and the tutorial tracks, the stranger's offers, contracts
// and caches, banking and favors, and the ledger receipts that pay out. #251 motion, no logic touched.
public partial class Map
{

    // #160 routing hook: when set, the Captain desk opens on its Tutorials tab. The #195-removed Nav
    // pop-up used to raise it via a "Show me" button; kept dormant for the tutorial mission to drive.
    private bool _openCaptainToTutorials;

    /// <summary>Sets the ship's mission — the only writer of <see cref="_mission"/> (the captain's
    /// desk calls this on every card click; no confirm dialog, per the addendum). The explicit
    /// StateHasChanged matters: this page suppresses Blazor's automatic per-event re-render (see
    /// IHandleEvent.HandleEventAsync below), so without it the articles headline would wait for
    /// OnTick's 200 ms HUD throttle — a visible beat of lag on a deliberate, rare click.</summary>
    private void SetMission(ShipMission mission)
    {
        ShipMission previous = _mission;
        _mission = mission;
        // M26: the captain's Fly to order steers the nav destination, and explicitly choosing
        // Free sailing rescinds it — the two desks can never disagree about where we're bound.
        if (mission.Kind == MissionKind.FlyTo && mission.DestinationBodyId is not null)
        {
            _destinationBodyId = mission.DestinationBodyId;
            _passDirty = true;
        }
        else if (mission.Kind == MissionKind.LayLow && mission.HavenBodyId is not null)
        {
            // "Lay low" is "make for this haven" — steer nav there like Fly to, so ordering it from
            // the captain's chair actually points the ship at the haven (and the haven tutorial flows).
            _destinationBodyId = mission.HavenBodyId;
            _passDirty = true;
            AdvanceTutorial(StepOrderLayLow);
        }
        else if (mission.Kind == MissionKind.FreeSailing && previous.Kind == MissionKind.FlyTo)
        {
            _destinationBodyId = null;
        }

        StateHasChanged();
    }

    // SATURDAY-ANCHOR: fields — parallel lanes append their station fields directly below
    // PR-15: the captain's position — session-only mission state (no save system exists yet, so
    // this resets on reload same as everything else). Default is Free sailing until the captain
    // gives an order (docs/SaturdayPlan/StationDesks.md addendum).
    private ShipMission _mission = ShipMission.Default;
    private MissionOptions _missionOptions = new([], [], [], [], []);

    private bool _showTutorial = true;
    private int _tutorialStep;                         // 0..N-1 = current task, N = complete

    // Two hunts: the first teaches the soft catch (a compliant pod you just board); the second
    // teaches the gun (a stubborn He3 freighter that won't stop — the only way to take her is to
    // hole her sail). docs/MondayPonder/UIUsabilityNotes.md — "the gun tutorial" (owner's idea).
    private const int FirstHuntSteps = 6;              // indices 0..5 belong to the first hunt

    // Second-hunt (the gun) step indices — kept named so the AdvanceTutorial wiring stays legible.
    private const int StepSelectFreighter = 6;
    private const int StepWarnFreighter = 7;
    private const int StepAuthorizeShot = 8;
    private const int StepHoleFreighter = 9;
    private const int StepBoardFreighter = 10;
    private const int StepSellHe3 = 11;

    // Third tutorial (use a haven) step indices — the heat/hunter/lie-low loop.
    private const int StepOrderLayLow = 12;
    private const int StepInsertHaven = 13;
    private const int StepCoolHeat = 14;

    private static readonly string[] TutorialSteps =
    [
        // First hunt — the soft catch (a compliant Luna pod)
        "Open the traffic board and select the Luna pod",
        "Plot an intercept — enter Plot, add a burn, watch the ribbon cross its cone",
        "Close to boarding range and match velocity",
        "Hold the window — 🏴 authorize the board (piracy needs the captain's word)",
        "Dock at Earth and sell",
        "Spend it — buy an upgrade",
        // Second hunt — the gun (the stubborn He3 freighter Nervous Lark)
        "Find the stubborn He3 freighter (Nervous Lark) and select her",
        "Close to weapon range, fire a warning shot — she won't heave to",
        "Captain's desk (0): authorize the shot — a gun needs the captain's word",
        "War room (3): AIM, SOLVE, then FIRE a slug to hole her sail",
        "Board the drifting hulk — take the He3",
        "Run the loot home and sell it",
        // Third — use a haven (cool the heat your piracy earned)
        "Captain's desk (0): order Lay low at a haven",
        "Reach the haven — orbit a moon, or coast in slow and clamp its ⚓ dock",
        "Lie low — let the heat cool until the hunter breaks off",
    ];

    // The tutorials are independent tracks over ranges of TutorialSteps — the Captain's Tutorials tab
    // lists them, one card each, and starting one (re)seeds its scenario. Order here IS play order:
    // finishing a track flows _tutorialStep into the next (rob in "the gun" → arrive in "use a haven"
    // already carrying heat), while the picker can jump to any.
    private sealed record TutorialTrack(int Start, int Length, string Title, string Blurb);

    private static readonly TutorialTrack[] TutorialTracks =
    [
        new(0, FirstHuntSteps, "The soft catch", "A compliant Luna pod — learn the intercept and the board."),
        new(StepSelectFreighter, 6, "The gun", "A runner who won't heave to — hole her sail, take her cargo."),
        new(StepOrderLayLow, 3, "Use a haven", "You've made enemies. Cool the heat and shake the hunter at a haven."),
    ];

    // The track _tutorialStep currently sits in, or -1 once every step is behind you.
    private int ActiveTutorialIndex()
    {
        for (int i = 0; i < TutorialTracks.Length; i++)
        {
            TutorialTrack t = TutorialTracks[i];
            if (_tutorialStep >= t.Start && _tutorialStep < t.Start + t.Length)
            {
                return i;
            }
        }

        return -1;
    }

    // The tutorial tracks projected for the Captain's Tutorials tab (title + blurb, in play order).
    private IReadOnlyList<Stations.Captain.TutorialItem> TutorialCards() =>
        TutorialTracks.Select(t => new Stations.Captain.TutorialItem(t.Title, t.Blurb)).ToArray();

    // --- Ashore quests (M-Q1) — contracts a bar stranger slides across the table. Reuses the
    // tutorial-lesson *mechanic* (an objective tracked to completion) but is kept separate from
    // _tutorialStep, which linear-chains its steps. A hunt is met when its target ship is brought
    // down (holed or boarded); turning in at any haven pays the reward. State is a plain list of
    // records — player-driven, never read by the physics sim. ---
    private enum QuestKind { Hunt, CargoRun, Intel, Fetch, Crack, Favor, FetchCache }
    // Fetch adds a PickedUp step between Active and Complete: fly to the SourceBodyId derelict to grab
    // the goods, then hand them over in person at the DestBodyId station's bar (no electronic trace).
    // Crack is the same face-to-face shape but the pickup is a locked hatch *here*: walk to the named
    // hatch, key in the Pin the Fixer gave you, then hand the package back to the Fixer at this station.
    private enum QuestState { Active, PickedUp, Complete, TurnedIn }
    // A hunt stores the prey's ship id in TargetShipId; a cargo run / fetch stores the delivery haven's
    // body id in DestBodyId (TargetCallsign holds the human name in all cases). A fetch also stores the
    // pickup derelict's body id in SourceBodyId. A crack stores the target hatch's id (e.g. "V-06") in
    // TargetShipId and its access code in Pin.
    private sealed record Quest(string Id, QuestKind Kind, string Giver, string TargetShipId,
        string TargetCallsign, string Title, string Blurb, int Reward, string? DestBodyId = null,
        string? SourceBodyId = null, string? Pin = null)
    {
        public QuestState State { get; set; } = QuestState.Active;
    }
    private readonly List<Quest> _quests = [];
    private Quest? _pendingOffer;   // the stranger's current table offer, awaiting Accept/Pass
    private int _questSeq;          // monotonic id source for accepted quests

    // The standalone rumour-map purchase (deliverable 5): a barfly sells a map to some NPC's forgotten
    // hoard, dice-priced. Buying LEARNS the cache (the same dig path as our own chests) — no delivery
    // obligation, the loot is ours to keep. Skips the buy if the purse can't cover the asking price.
    private bool BuyRumorMap(string drawKey)
    {
        RumorMaps.Rumor rumor = RumorMaps.Generate(drawKey);
        if (_credits < rumor.PriceCredits)
        {
            ShowPulseMessage($"🗺 A rumour map's on the table — {rumor.PriceCredits:N0} cr — but the purse won't cover it.");
            return false;
        }
        _credits -= rumor.PriceCredits;
        _caches.Learn(rumor.Cache);
        RendererInterop.PlayCue("reveal");
        ShowPulseMessage($"🗺 Bought a rumour map for {rumor.PriceCredits:N0} cr — {rumor.Cache.Owner}'s hoard on {BodyName(rumor.Cache.BodyId)}. Dig it up before someone else does.");
        return true;
    }

    private string WreckRevealMessage(string bodyId) => bodyId == "derelict-roadster"
        ? "🔭 There she is — a cherry-red glint on the return, right where the tip said. Contact: the Derelict Roadster."
        : $"🔭 Scan resolved a new contact — {BodyName(bodyId)} is on the charts now.";

    // The tutorial hunts are independent lessons (owner: "playable in any order — start from the
    // second if you've done the first before"). Jumping to a hunt sets its first step and (re)seeds
    // its prey relative to where the player is NOW, so the lesson is always deliverable on the spot.
    // Start (or replay) a tutorial track from the Captain's Tutorials tab. Jumps _tutorialStep to the
    // track's first step, (re)seeds whatever that lesson needs, and drops the player at the Nav map —
    // the helm — where the checklist rides along. `trackIndex` is the card's position in TutorialTracks.
    private void StartTutorial(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= TutorialTracks.Length)
        {
            return;
        }

        _tutorialStep = TutorialTracks[trackIndex].Start;
        _showTutorial = true;
        switch (trackIndex)
        {
            case 0: SeedFirstHuntTarget(); break;
            case 1: SeedSecondHuntTarget(); break;
            case 2: SeedHavenLesson(); break;
        }

        SwitchDesk(ShipDesk.Nav); // the hunt/haven all play out on the map; go to the helm
        StateHasChanged();
    }

    // The haven lesson only bites if you've earned some heat. If you jump straight to it with a clean
    // record, seed a spot of trouble — a couple points of heat and a hunter fitting out — so there's
    // something real to run from. Coming off "the gun" you already carry heat, so leave it be.
    private void SeedHavenLesson()
    {
        if (_heat.Level > 0)
        {
            return;
        }

        _heat = EncounterRule.RaiseHeat(_heat, 2, SimTime);
        SpawnHunterForHeatEvent();
        ShowPulseMessage("Word's out on your work — heat's up and a hunter's fitting out. Time to find a haven.");
    }

    // (Re)seed the second hunt's prey co-moving with the player's CURRENT state — not at t=0 — so her
    // escape jink is always ~2 days out from when the hunt begins, never stale from a slow first hunt.
    // Drops any prior Lark first, so restarting the hunt always yields a fresh, catchable target.
    private void SeedSecondHuntTarget()
    {
        if (!_scenarioName.Contains("Sol", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _npcStates = _npcStates
            .Where(n => n.Ship.Id != TrafficSchedule.StarterFreighterId)
            .Append(KnownContact(TrafficSchedule.StarterFreighter(_ship)))
            .ToArray();
        ShowPulseMessage("Sensors ping a fat He3 hauler close by — the Nervous Lark. She won't stop for anyone.");
    }

    // A seeded contact the player is meant to find right away spawns already KNOWN — a fix at its
    // spawn state — so it draws (labelled) on the map the instant the hunt starts, even while paused
    // (owner: "I always want to see all ships... surely not hidden at this close").
    private static NpcState KnownContact(NpcShip ship) => new()
    {
        Ship = ship,
        LastObservation = new Observation(ship.Id, ship.InitialState.SimTime, ship.InitialState.Position, ship.InitialState.Velocity),
        CurrentlyObserved = true,
    };

    // (Re)seed the first hunt's pod for the hunt picker — "play the soft catch again" always gets a
    // fresh Sitting Duck abeam the player's current position.
    private void SeedFirstHuntTarget()
    {
        if (!_scenarioName.Contains("Sol", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _npcStates = _npcStates
            .Where(n => n.Ship.Id != TrafficSchedule.StarterPodId)
            .Append(KnownContact(TrafficSchedule.StarterPod(_ship)))
            .ToArray();
    }

    // #266 — the rescue offer pop-up (piracy-pop-up family): a real modal with the terms visible before
    // accepting. Auto-opens the instant we go adrift (UpdateShipAlerts); re-openable from the inline
    // adrift affordance while stranded; Decline just dismisses (the offer stands until we're under way).
    private bool _showRescueOffer;

    private void OpenRescueOffer() => _showRescueOffer = true;

    private void CloseRescueOffer() => _showRescueOffer = false; // Decline: dismiss; the offer re-opens from the strip

    private void ToggleTutorial() => _showTutorial = !_showTutorial;

    private void AdvanceTutorial(int completedStep)
    {
        if (_tutorialStep == completedStep)
        {
            _tutorialStep++;
        }
    }

    // ---- #223 buried treasure: the shuttle door's second life ----
    // A landable moon/asteroid grows two doors on the shuttle-bay pop-up: ⛏ Bury (sink coin/cargo off
    // the ship) and 🗺 Dig (lift a known cache). Both fly the shuttle DOWN and back — the mothership
    // loiters, the clock advances by the crossing (heat bleeds, traffic drifts), the ship never
    // relocates. Buried loot lives in _caches, never in _credits/_cargoByClass, so a confiscation that
    // reads only carried goods can never see it. X always marks the spot.

    // A body you can put a chest on: a moon or asteroid (a surface to walk), never a station or planet.
    private bool IsLandableForCache(CelestialBody b) => b.Kind == BodyKind.Moon;

    private void OpenBuryChooser(ShuttleStop stop)
    {
        _buryTarget = stop;
        _buryCoin = Math.Min(_credits, 500); // a sensible opening bid; the dial and "All" adjust it
        _shuttleBayStops = null;             // the chooser replaces the door list
    }

    private void CancelBury() => _buryTarget = null;

    private void AdjustBuryCoin(int delta) => _buryCoin = Math.Clamp(_buryCoin + delta, 0, _credits);

    private void SetBuryCoin(int amount) => _buryCoin = Math.Clamp(amount, 0, _credits);

    // Fly the shuttle down and bury the chest: take the loot OFF the ship, advance the clock by the
    // crossing, mint the cache + its map, and show the map card. Nothing here touches confiscation —
    // buried is invisible by construction.
    private void ConfirmBury(ShuttleStop stop)
    {
        if (_ephemeris is null)
        {
            return;
        }
        int coin = Math.Clamp(_buryCoin, 0, _credits);
        // The whole hold goes in (it's small): snapshot the manifest into cache lines, hot-flagged.
        var cargo = _cargoByClass
            .Where(kv => kv.Value > 0)
            .Select(kv => new CacheCargo(kv.Key, kv.Value, IsHotClass(kv.Key)))
            .ToList();
        if (coin <= 0 && cargo.Count == 0)
        {
            _buryTarget = null;
            return;
        }

        // The trip costs the clock (heat bleeds, traffic drifts) — the mothership loiters in place.
        AdvanceShuttleClock(stop.TravelSeconds);

        // Loot leaves the ship's books.
        _credits -= coin;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();

        TreasureCache cache = _caches.Bury(stop.Body.Id, coin, cargo, SimTime, "you", playerOwned: true);
        SeedDiscoveryWatch();

        _buryTarget = null;
        _treasureMapCard = cache;
        RendererInterop.PlayCue("reveal");
        ShowPulseMessage($"⛏ Chest buried on {stop.Body.Name} — {cache.ContentsLine()} off the books. Map filed (🗺).");
        RequestVaultSave(); // #225: the hoard changed — persist coin/cargo/cache moves
    }

    // Fly the shuttle down and dig at the X: lift EVERY cache we know of at this body back onto the
    // ship (coin to the purse, cargo to the hold up to capacity), advance the clock by the crossing.
    private void DigAt(ShuttleStop stop)
    {
        if (_ephemeris is null || !_caches.HasCacheAt(stop.Body.Id))
        {
            return;
        }
        AdvanceShuttleClock(stop.TravelSeconds);

        var lifted = _caches.CachesAt(stop.Body.Id).ToList();
        int coinBack = 0, unitsBack = 0, unitsLost = 0;
        foreach (TreasureCache known in lifted)
        {
            TreasureCache? dug = _caches.Dig(known.Id);
            if (dug is not { } c)
            {
                continue;
            }
            _credits += c.Coin;
            coinBack += c.Coin;
            foreach (CacheCargo line in c.Cargo)
            {
                int room = CargoCapacity - _cargoUnits;
                int take = Math.Min(room, line.Units);
                if (take > 0)
                {
                    _cargoUnits += take;
                    _cargoValue += take * CargoMarket.UnitValue(line.CargoClass);
                    _cargoByClass[line.CargoClass] = _cargoByClass.GetValueOrDefault(line.CargoClass) + take;
                    unitsBack += take;
                }
                unitsLost += line.Units - take; // no room in the hold — left in the dust
            }
            // A recovered chest can close a fetch-a-cache job that pointed at it (#223).
            CompleteFetchCacheFor(c);
        }

        RendererInterop.PlayCue("board");
        string lost = unitsLost > 0 ? $" ({unitsLost}u left — hold full)" : "";
        ShowPulseMessage($"🗺 Dug up {coinBack:N0} cr + {unitsBack} units on {stop.Body.Name}{lost}. X marked the spot.");
        PayCompletedQuests();
        RequestVaultSave(); // #225: a dig moves coin/cargo/caches — persist it
    }

    private void DismissMapCard() => _treasureMapCard = null;

    // A per-body placeholder art fill for the map card's big image slot until the grok image lane fills
    // the manifest (docs/FridaySecondPlan/hoard-image-manifest.md). Deterministic tint per body so
    // Phobos always reads the same. A real asset would be art/treasure-<bodyId>.jpg dropped in behind this.
    private static string TreasureMapArtCss(string bodyId)
    {
        int h = Math.Abs(bodyId.GetHashCode());
        int hue = h % 360;
        return $"radial-gradient(circle at 38% 32%, hsl({hue}, 40%, 34%), hsl({(hue + 28) % 360}, 45%, 12%) 70%)";
    }

    // ---- The discovery watch (ruling 4): rivals find our hoards on a slow roll ----

    // Start the watch at the current day the first time we bury, so a just-dug chest isn't rolled for
    // the partial current day.
    private void SeedDiscoveryWatch()
    {
        if (_lastCacheCheckPeriod < 0)
        {
            _lastCacheCheckPeriod = DiscoveryRule.PeriodIndex(SimTime);
        }
    }

    // Resolve the per-cache discovery roll across every whole day elapsed since the last check (so a
    // warp that skips days can't skip a roll). A found cache is GONE — a ledger squawk marks the loss.
    private void RunCacheDiscoveryWatch()
    {
        if (_lastCacheCheckPeriod < 0)
        {
            return; // nothing buried yet
        }
        long nowPeriod = DiscoveryRule.PeriodIndex(SimTime);
        if (nowPeriod <= _lastCacheCheckPeriod)
        {
            return;
        }
        foreach (TreasureCache c in _caches.Caches.Where(c => c.PlayerOwned).ToList())
        {
            // Never roll a cache for days before it was in the ground: start its scan at the later of
            // the global last-check and its own burial day.
            long from = Math.Max(_lastCacheCheckPeriod, DiscoveryRule.PeriodIndex(c.BuriedSimTime));
            if (DiscoveryRule.DiscoveredWithin(c.Id, from, SimTime) is not null)
            {
                _caches.Remove(c.Id);
                RendererInterop.PlayCue("alarm");
                ShowPulseMessage($"🏴‍☠️ Someone dug up our chest on {BodyName(c.BodyId)} — {c.ContentsLine()} gone. Split the hoards next time.");
            }
        }
        _lastCacheCheckPeriod = nowPeriod;
    }

    // Knock on a locked station hatch (a ring department or a bar back-room). Nobody answers — for now.
    // Each hatch carries an id in its label (e.g. "🔒 MEDBAY · M-05") so a mission can name one to
    // open. PIN entry / mission unlock is the next layer; today every knock goes unanswered.
    // Lift the fence's package off the back-room shelf (PR-F, the indoor quest that uses the grown
    // room). The stash console only exists once the wing is welded on, so reaching it proves the room
    // exists — the quest gates on the room, as the room gated on the crack. Advances the crack job
    // from Active to PickedUp (the pickup that a plain lockup did at the keypad); hand-off is unchanged.
    private void LiftStash()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.Stash })
        {
            return;
        }
        Quest? job = _quests.FirstOrDefault(q =>
            q.Kind == QuestKind.Crack && q.SourceBodyId == _dockedHavenId
            && _dockedHavenId is { } st && HavenInterior.HatchGrowsWing(st, q.TargetShipId));
        if (job is { State: QuestState.Active })
        {
            job.State = QuestState.PickedUp;
            RendererInterop.PlayCue("board");
            ShowPulseMessage("You peel the package from behind the shelf and pocket it. Now get it back to the Fixer. 📦");
        }
        else if (job is not null)
        {
            ShowPulseMessage("The shelf's bare now — you already lifted what was here.");
        }
        else
        {
            ShowPulseMessage("Dusty shelving and a cold draught. Nothing here worth pocketing — unless someone sent you for it.");
        }
    }

    private void TalkToStranger()
    {
        if (_pendingOffer is not null)
        {
            return; // the card's already on the table
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.BarPatron } spot)
        {
            return;
        }
        string giver = spot.Label.Replace("◈", "").Trim();

        // The roaming Magpie (PR-F, "people cannot be static furniture"): interaction is gated on their
        // sim-time rota, so walking up to a chair they've left tells you they've moved on, not gives an
        // offer. Handled before the generic give-work paths, which assume a patron who stays put.
        if (giver.Contains("MAGPIE", StringComparison.OrdinalIgnoreCase))
        {
            TalkToMagpie(spot);
            return;
        }

        // Face-to-face hand-off (no electronic trace): a picked-up fetch job, delivered in person to
        // The Fixer at its destination station. Paid on the spot, under the table — done before any
        // "still-waiting" guard, since the fetch's giver is a Fixer at every station.
        if (giver.Contains("FIXER", StringComparison.OrdinalIgnoreCase) && _dockedHavenId is { } here
            && _quests.FirstOrDefault(q => q is { Kind: QuestKind.Fetch, State: QuestState.PickedUp } && q.DestBodyId == here) is { } drop)
        {
            DeliverFetch(drop);
            return;
        }

        // The cracked-hatch package, handed back to the Fixer at this same station.
        if (giver.Contains("FIXER", StringComparison.OrdinalIgnoreCase) && _dockedHavenId is { } berth
            && _quests.FirstOrDefault(q => q is { Kind: QuestKind.Crack, State: QuestState.PickedUp } && q.DestBodyId == berth) is { } cracked)
        {
            DeliverCrack(cracked);
            return;
        }

        Quest? open = _quests.FirstOrDefault(q => q.Giver == giver && q.State != QuestState.TurnedIn);
        if (open is { State: QuestState.Active })
        {
            ShowPulseMessage($"“Still waiting on {open.TargetCallsign}. Finish the job, then we'll talk.”");
            return;
        }
        if (open is { Kind: QuestKind.Fetch, State: QuestState.PickedUp })
        {
            ShowPulseMessage($"“You've got the goods — don't flash them here. Get them to my associate at {open.TargetCallsign}.”");
            return;
        }
        if (open is { State: QuestState.Complete })
        {
            ShowPulseMessage($"“{open.TargetCallsign} — done. Collect at any berth; the coin's waiting.”");
            return;
        }

        // PR-WIRE: a favor called in. If we owe this contact a wired debt and haven't yet been handed
        // the delivery, they slide it across the table now — one quiet delivery, in their own voice.
        if (MakeFavorDeliveryOffer(giver) is { } favorOffer)
        {
            _pendingOffer = favorOffer;
            return;
        }

        Quest? offer = giver switch
        {
            _ when giver.Contains("COIL", StringComparison.OrdinalIgnoreCase) => MakeCargoRunOffer(giver),
            _ when giver.Contains("GILT", StringComparison.OrdinalIgnoreCase) => MakeIntelOffer(giver),
            _ when giver.Contains("FIXER", StringComparison.OrdinalIgnoreCase) => MakeFetchOffer(giver) ?? MakeFetchCacheOffer(giver) ?? MakeCrackOffer(giver),
            _ => MakeHuntOffer(giver),
        };
        if (offer is null)
        {
            ShowPulseMessage("The stranger swirls their drink. “Nothing worth your time right now. Check back.”");
            return;
        }
        _pendingOffer = offer;
    }

    // The Magpie, a fence's runner who won't sit still (PR-F). Their position is a pure function of
    // sim time (HavenInterior.MagpieRota); talking is gated on them actually being at the booth you
    // walked up to. So a captain who chatted them at the bar can return a watch later to an empty
    // chair — "they change place and go behind locked doors or move" (owner's ruling, verbatim). Once
    // the Bonded Stores back room is open, that's one of their stops — find them inside.
    private void TalkToMagpie(DeckPlan.ConsoleSpot spot)
    {
        bool backOpen = _dockedHavenId is { } st
            && UnlockedHatchesFor(st).Any(h => HavenInterior.HatchGrowsWing(st, h));
        NpcPost m = HavenInterior.ResolveMagpie(SimTime, backOpen);
        double d = m.Present
            ? Math.Sqrt((spot.X - m.X) * (spot.X - m.X) + (spot.Y - m.Y) * (spot.Y - m.Y))
            : double.MaxValue;
        if (d > DeckPlan.InteractRadius)
        {
            ShowPulseMessage("The Magpie's chair is empty — they've drifted off. Nobody sits still here; try another watch, or look where a door's just opened. 🐦");
            return;
        }

        Quest? job = _quests.FirstOrDefault(q =>
            q.Kind == QuestKind.Crack && q.SourceBodyId == _dockedHavenId
            && _dockedHavenId is { } s && HavenInterior.HatchGrowsWing(s, q.TargetShipId));
        string line = job switch
        {
            { State: QuestState.PickedUp } or { State: QuestState.Complete } or { State: QuestState.TurnedIn }
                => "“Good hands. Get that parcel to the Fixer and we never spoke.”",
            _ when backOpen && m.Location == "BACK ROOM"
                => "“You made it in. The parcel's right there on the shelf — lift it before the dockmaster's rounds.”",
            _ when backOpen
                => "“You're through the hatch. Package is on the back shelf — go on, it won't bite.”",
            { State: QuestState.Active }
                => "“The lockup's the easy part — crack V-06 and there's a parcel with nobody's name on it. I'll be around. Somewhere.”",
            _ => "“Bonded Stores — V-06 — holds a parcel that never made a manifest. The Fixer sets the price; I just know where things are. And I don't linger.”",
        };
        ShowPulseMessage(line);
    }

    // --- #247 The barkeep: buying a drink ashore ---------------------------------------------------
    // Owner ashore at the Rusty Roadstead: "How do I get a drink at the Rusty bar here? Did we forget
    // to add the bar-keep :-D". Drinking already lived aboard (the Galley 'Pour a tot'); this is the
    // same beat ashore. The barkeep card is opened by pressing E at the counter; the per-bar house
    // special, name and rumors are pure Core data (Barkeeps). Same drunkenness law both places — a
    // poured drink routes through the very PourRum the Galley calls (one tot count, one wobble).
    private Core.Interior.Barkeep? _barMenu;   // the open barkeep card (null = shut)
    private string? _barNotice;                 // the last thing the keep said, shown on the card

    private void TalkToBarkeep()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.Barkeep })
        {
            return;
        }
        if (_dockedHavenId is not { } id || Barkeeps.For(id) is not { } keep)
        {
            ShowPulseMessage("The bar's unattended just now — nobody behind the counter.");
            return;
        }
        _barMenu = keep;
        _barNotice = keep.Greeting;
    }

    private void CloseBarkeep()
    {
        _barMenu = null;
        _barNotice = null;
    }

    // Buy the house special: debit the purse, then apply the SAME drunkenness the Galley tot does — the
    // drink rides through PourRum, so a third round ashore makes the deck just as tilty as one aboard.
    // A #119-style receipt names the drink and the spend (the repo loves receipts).
    private void BuyHouseSpecial()
    {
        if (_barMenu is not { } keep)
        {
            return;
        }
        Core.Interior.BarTab tab = keep.PourHouseSpecial(_credits);
        if (!tab.Poured)
        {
            _barNotice = tab.Line;
            ShowPulseMessage(tab.Line);
            return;
        }
        _credits = tab.RemainingCredits;
        PourRum($"{keep.DrinkName} — {keep.DrinkFlavor}"); // one wobble law, aboard and ashore
        _barNotice = tab.Line;
        ShowPulseMessage($"{tab.Line} (−{tab.Cost:N0} cr)");
        RequestVaultSave(); // #225: the purse moved
    }

    // Buy a round for the whole room — a bigger spend that WARMS the regulars actually drinking here
    // (#247 kin #224: the cheap way to thaw a cold contact). Goodwill is booked on the ContactLedger,
    // the same saved book that holds mission history and bank balances — a future relationship layer
    // reads it. You drink too, so the round counts as a tot on your own legs.
    private void BuyRoundForRoom()
    {
        if (_barMenu is not { } keep)
        {
            return;
        }
        Core.Interior.BarTab tab = keep.BuyRound(_credits);
        if (!tab.Poured)
        {
            _barNotice = tab.Line;
            ShowPulseMessage(tab.Line);
            return;
        }
        _credits = tab.RemainingCredits;

        bool backOpen = _dockedHavenId is { } st
            && UnlockedHatchesFor(st).Any(h => HavenInterior.HatchGrowsWing(st, h));
        var warmed = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DeckPlan.ConsoleSpot c in _deckPlan.Consoles)
        {
            if (c.Kind != DeckPlan.ConsoleKind.BarPatron)
            {
                continue;
            }
            string giver = c.Label.Replace("◈", "").Trim();
            if (!seen.Add(giver))
            {
                continue; // one contact can hold two consoles (the roaming Magpie) — warm them once
            }
            // The roaming Magpie only drinks with the room when their rota has them in it this watch.
            if (giver.Contains("MAGPIE", StringComparison.OrdinalIgnoreCase)
                && !HavenInterior.ResolveMagpie(SimTime, backOpen).Present)
            {
                continue;
            }
            _contacts.AddGoodwill(giver, giver, 1);
            warmed.Add(GiverDisplay(giver));
        }

        PourRum($"{keep.DrinkName}, all round — {keep.DrinkFlavor}"); // your glass is in it too
        string cheers = warmed.Count > 0 ? $" {string.Join(", ", warmed)} raise a glass to you." : "";
        _barNotice = tab.Line + cheers;
        ShowPulseMessage($"{tab.Line}{cheers} (−{tab.Cost:N0} cr)");
        RequestVaultSave(); // #225: the purse moved and goodwill was booked
    }

    // Ask the barkeep what they've heard — a cheap tip line for flavor (deterministic per sim-hour).
    private void AskBarkeepForRumor()
    {
        if (_barMenu is not { } keep)
        {
            return;
        }
        string rumor = keep.RumorAt(SimTime);
        _barNotice = rumor;
        ShowPulseMessage($"🍺 {keep.Name}: {rumor}");
    }

    // Pick a live target for a hunt contract — prefer off-books ships (the kind you couldn't just read
    // off the public traffic board, so the stranger's tip is actually worth something). Chosen from
    // sim time + the current berth so the booth's offer is stable frame to frame, and skips ships
    // already under contract.
    private Quest? MakeHuntOffer(string giver)
    {
        List<NpcState> candidates = _npcStates
            .Where(n => n.Active && !n.Arrived && !n.Boarded && !n.Ship.IsPod
                        && _quests.All(q => q.TargetShipId != n.Ship.Id))
            .OrderByDescending(n => !n.Ship.PublishesTimetable)          // off-books first
            .ThenBy(n => n.Ship.Id, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        NpcState prey = candidates[OfferIndex(candidates.Count)];
        int reward = 250 + prey.Ship.CargoUnits * 60;
        string route = RouteLabel(prey.Ship);
        string blurb = prey.Ship.PublishesTimetable
            ? $"“See {prey.Ship.Callsign}, running {route}? She's carrying, and I want her stopped. Bring her down — {reward:N0} cr in it for you.”"
            : $"“There's a ghost called {prey.Ship.Callsign} running dark, {route}. Won't show on any board. Hole her, board her — {reward:N0} cr, quiet-like.”";
        return new Quest($"hunt-{++_questSeq}", QuestKind.Hunt, giver,
            prey.Ship.Id, prey.Ship.Callsign, $"Bring down {prey.Ship.Callsign}", blurb, reward);
    }

    // A parcel to carry to another haven — completes when you berth there. Destination is any haven
    // other than the one you're standing in, chosen by sim time + berth so it's stable per booth.
    private Quest? MakeCargoRunOffer(string giver)
    {
        List<CelestialBody> havens = (_ephemeris?.Bodies ?? [])
            .Where(b => b.IsHaven && b.Id != _dockedHavenId
                        && _quests.All(q => q.DestBodyId != b.Id))
            .OrderBy(b => b.Id, StringComparer.Ordinal)
            .ToList();
        if (havens.Count == 0)
        {
            return null;
        }

        CelestialBody dest = havens[OfferIndex(havens.Count)];
        int reward = 300 + (int)(dest.OrbitRadius / 1e10); // farther berths pay a little more
        // #175: a moon haven has no ⚓ dock — you deliver by parking in its orbit — so the pitch names
        // the right last move instead of promising a "berth" that a moon never has.
        string drop = IsDockableHaven(dest) ? "Berth there and it's done." : "Park in orbit there and it's done.";
        string blurb = $"“Quiet parcel, no questions. Gets to {dest.Name} in one piece, you walk with {reward:N0} cr. {drop}”";
        return new Quest($"run-{++_questSeq}", QuestKind.CargoRun, giver,
            "", dest.Name, $"Run a parcel to {dest.Name}", blurb, reward, DestBodyId: dest.Id);
    }

    // PR-WIRE — the favor called in. When we owe a contact a wired debt (a FavorObligation from a
    // borrow) and don't already have their delivery in hand, they hand us one quiet parcel now, in
    // their voice. Delivering it works the debt off (PayCompletedQuests books the repayment). The
    // destination is a haven other than this one, picked stably by sim time + berth.
    private Quest? MakeFavorDeliveryOffer(string giver)
    {
        FavorObligation? match = null;
        foreach (FavorObligation o in _favorObligations)
        {
            if (string.Equals(o.ContactId, giver, StringComparison.OrdinalIgnoreCase)) { match = o; break; }
        }
        if (match is not { } debt)
        {
            return null;
        }
        if (_quests.Any(q => q.Kind == QuestKind.Favor && string.Equals(q.Giver, giver, StringComparison.OrdinalIgnoreCase) && q.State != QuestState.TurnedIn))
        {
            return null; // already carrying their favor
        }

        List<CelestialBody> havens = (_ephemeris?.Bodies ?? [])
            .Where(b => b.IsHaven && b.Id != _dockedHavenId)
            .OrderBy(b => b.Id, StringComparer.Ordinal)
            .ToList();
        if (havens.Count == 0)
        {
            return null;
        }

        CelestialBody dest = havens[OfferIndex(havens.Count)];
        string drop = IsDockableHaven(dest) ? "Berth there and we're square." : "Park in orbit there and we're square.";
        string blurb = $"{debt.VoiceLine} Get it to {dest.Name} in one piece. {drop}";
        return new Quest($"favor-{++_questSeq}", QuestKind.Favor, giver,
            "", dest.Name, $"Quiet delivery for {GiverDisplay(giver)}", blurb, (int)debt.PrincipalCredits, DestBodyId: dest.Id);
    }

    // A whisper on an off-books ghost — the payoff IS the tip. Accepting drops a fresh route-intel
    // entry into the ledger (exactly like a dark-web buy, but on the house), so a ship that never
    // shows on the public board joins your contacts, 🕸-tagged, for 30 days. Instant: no task to do.
    private Quest? MakeIntelOffer(string giver)
    {
        List<NpcState> ghosts = _npcStates
            .Where(n => n.Active && !n.Arrived && !n.Ship.PublishesTimetable
                        && !_intelLedger.Knows(n.Ship.Id, SimTime)
                        && _quests.All(q => q.TargetShipId != n.Ship.Id))
            .OrderBy(n => n.Ship.Id, StringComparer.Ordinal)
            .ToList();
        if (ghosts.Count == 0)
        {
            return null;
        }

        NpcState ghost = ghosts[OfferIndex(ghosts.Count)];
        string blurb = $"“I know where {ghost.Ship.Callsign} really runs — {RouteLabel(ghost.Ship)}. A ghost; she'll never show on any board. This one's on the house. Want it?”";
        return new Quest($"intel-{++_questSeq}", QuestKind.Intel, giver,
            ghost.Ship.Id, ghost.Ship.Callsign, $"Whisper on {ghost.Ship.Callsign}", blurb, 0);
    }

    // The Fixer's one confidential job: fly out to the derelict roadster, prise the untraceable wallet
    // from between the seats, then hand it over in person at another station's bar. A one-off signature
    // hunt — offered only if it isn't already in the ledger (in any state), so the wallet is unique.
    // Destination is an interior station other than this one, picked by sim time + berth so it's stable.
    private Quest? MakeFetchOffer(string giver)
    {
        if (_ephemeris is null || _quests.Any(q => q.Kind == QuestKind.Fetch))
        {
            return null; // no world yet, or there is only one lost roadster
        }
        if (_ephemeris.Bodies.All(b => b.Id != "derelict-roadster"))
        {
            return null; // scenario without the wreck
        }
        List<CelestialBody> dests = _ephemeris.Bodies
            .Where(b => b.IsHaven && b.Id != _dockedHavenId && HavenInterior.HasInterior(b.Id))
            .OrderBy(b => b.Id, StringComparer.Ordinal)
            .ToList();
        if (dests.Count == 0)
        {
            return null;
        }

        CelestialBody dest = dests[OfferIndex(dests.Count)];
        const int reward = 4200; // a dead man's fortune, in a currency nobody can trace
        string blurb = $"“Word is a dead tycoon's cherry-red roadster is drifting sunward of Mars — shot up as a stunt, never came down. There's a hardware wallet wedged between the seats: a fortune, and untraceable. Fetch it, bring it quiet to my associate at {dest.Name}. {reward:N0} cr, and we never spoke.”";
        return new Quest($"fetch-{++_questSeq}", QuestKind.Fetch, giver,
            "", dest.Name, "Fetch the roadster's lost wallet", blurb, reward,
            DestBodyId: dest.Id, SourceBodyId: "derelict-roadster");
    }

    // #223: the Fixer's cache run — a map to SOMEONE ELSE'S buried hoard. The recovery flow and the
    // mission flow are one code path: accept learns the cache (so the shuttle-bay 🗺 Dig appears at the
    // body), digging it up sets PickedUp, and carrying the chest to the Fixer's bar pays out. Offered
    // only when a landable moon carries a named landmark to pace off (the monolith is the storied one).
    private Quest? MakeFetchCacheOffer(string giver)
    {
        if (_ephemeris is null || _dockedHavenId is not { } here)
        {
            return null;
        }
        if (_quests.Any(q => q.Kind == QuestKind.FetchCache && q.State != QuestState.TurnedIn))
        {
            return null; // one cache run at a time
        }
        // Only send them to a storied moon they can actually reach from this bar — one sharing the
        // station's planet, so it's in the same neighbourhood the shuttle can cross.
        string? planetId = BodyById(here)?.ParentId;
        CelestialBody? dig = _ephemeris.Bodies
            .Where(b => b.Kind == BodyKind.Moon && Landmarks.HasNamedSite(b.Id) && b.ParentId == planetId)
            .OrderBy(b => b.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (dig is null)
        {
            return null; // no storied landing site within reach of this bar
        }
        string barName = BodyName(here);
        const int reward = 3200;
        string drawKey = $"{here}|fetchcache|{(int)(SimTime / 86400)}";
        RumorMaps.Rumor rumor = RumorMaps.Generate(drawKey, dig.Id);
        string blurb = $"“A client wants a chest lifted — {rumor.Cache.Owner} buried it out on {dig.Name} and won't be collecting. Here's the map: {rumor.Cache.BearingLine}. Dig it up, bring the lot to me here at {barName}. {reward:N0} cr, clean.”";
        // TargetShipId carries the (deterministic) cache id so the dig can match it; Pin carries the draw
        // key so AcceptOffer re-mints and LEARNS the exact same cache.
        return new Quest($"fetchcache-{++_questSeq}", QuestKind.FetchCache, giver,
            rumor.Cache.Id, barName, "Lift a buried chest", blurb, reward,
            DestBodyId: here, SourceBodyId: dig.Id, Pin: drawKey);
    }

    // The Fixer's other line of work, when the roadster job is spoken for: crack a locked hatch here at
    // this station. Picks one of the deck's locked departments deterministically, quotes its real access
    // code, and pays on hand-over — a quick, self-contained job with no flying (contrast the fetch).
    private Quest? MakeCrackOffer(string giver)
    {
        if (_ephemeris is null || _dockedHavenId is not { } here)
        {
            return null;
        }
        if (_quests.Any(q => q.Kind == QuestKind.Crack && q.State != QuestState.TurnedIn))
        {
            return null; // one break-in at a time
        }
        List<DeckPlan.ConsoleSpot> locked = _deckPlan.Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.Hatch && c.Label.Contains("🔒", StringComparison.Ordinal))
            .OrderBy(c => c.Label, StringComparer.Ordinal)
            .ToList();
        if (locked.Count == 0)
        {
            return null;
        }

        // Prefer a hatch that GROWS A ROOM here (PR-F) — so the natural bar flow at a station with a
        // wing (Cinder Roost's Bonded Stores) hands out the world-growing job — else the usual rotation.
        int wingIdx = locked.FindIndex(c => HavenInterior.HatchGrowsWing(here, HatchId(c.Label)));
        DeckPlan.ConsoleSpot target = wingIdx >= 0 ? locked[wingIdx] : locked[OfferIndex(locked.Count)];
        string id = HatchId(target.Label);
        string dept = HatchDept(target.Label);
        string pin = MakePin(id);
        const int reward = 2600;
        string blurb = $"“That hatch — {id}, the {dept.ToLowerInvariant()} lockup. There's a package behind it that isn't on any manifest. Code's {pin} — I never told you that. Crack it, bring it straight back here, and it stays between us. {reward:N0} cr.”";
        return new Quest($"crack-{++_questSeq}", QuestKind.Crack, giver, id, $"the {dept.ToLowerInvariant()} package",
            $"Crack hatch {id}", blurb, reward, DestBodyId: here, SourceBodyId: here, Pin: pin);
    }

    // Deterministic pick index for a booth's offer: sim time (slow rotation) mixed with the berth id
    // (a stable char-sum, not the randomized string hash), so different docks surface different work
    // and it doesn't flicker frame to frame.
    private int OfferIndex(int count) =>
        count <= 0 ? 0 : (int)(((long)(SimTime / 1000) + (_dockedHavenId ?? "").Sum(ch => ch)) % count);

    private void AcceptOffer()
    {
        if (_pendingOffer is not { } offer)
        {
            return;
        }
        _pendingOffer = null;

        if (offer.Kind == QuestKind.Intel)
        {
            // A gift of information — delivered on the spot: drop the route tip into the ledger (like a
            // dark-web buy, but free) and log it as a settled entry. No hunt, no dock payout.
            _intelLedger.Add(new RouteIntel(offer.TargetShipId, SimTime, RouteIntel.DefaultValiditySeconds, Price: 0));
            _routeIntelProvenance[offer.TargetShipId] = new IntelProvenance(offer.Giver, DockedStationName(), SimTime);
            offer.State = QuestState.TurnedIn;
            _quests.Add(offer);
            ShowPulseMessage($"Tip logged — {offer.TargetCallsign} is on your contacts now (🕸 stale in 30 d) — filed in the Captain's ledger (0).");
            return;
        }

        _quests.Add(offer);

        // Tuesday plan PR-A: a fetch job no longer leaves the wreck labelled on the map. The Fixer
        // hands you a transponder fix instead — an intel card at the Comms desk with a 🔭 hook that
        // aims the scope. The wreck stays hidden until an actual scan resolves it.
        if (offer.Kind == QuestKind.Fetch && offer.SourceBodyId is { } wreckId && IsBodyHidden(wreckId)
            && !_scopeIntel.Any(si => si.BodyId == wreckId))
        {
            _scopeIntel.Add(BuildWreckIntel(wreckId, offer.Giver, DockedStationName()));
        }

        // #223: accepting a cache run LEARNS the target cache (same code path as a rumour map) — the
        // shuttle-bay 🗺 Dig door now appears at that body, and the map lands in the ledger's 🗺 section.
        if (offer.Kind == QuestKind.FetchCache && offer.Pin is { } drawKey && offer.SourceBodyId is { } digBody)
        {
            _caches.Learn(RumorMaps.Generate(drawKey, digBody).Cache);
        }

        // #207: accepting ANY contract kind now SPEAKS in-face — a #119-style receipt naming the job
        // and its giver, then the immediate next action — so the captain is never left guessing at the
        // moment of acceptance ("I took the parcel but the mission is quite unclear"). The live
        // next-action also rides the Captain desk chip (CaptainQuestChipLine) while the job is in hand.
        ContractFacts facts = FactsFor(offer);
        ShowPulseMessage($"{MissionBrief.Receipt(facts.Kind, facts.Giver)} {MissionBrief.NextLine(facts)}");
    }

    // #207: map a live quest onto the pure Core brief text (giver title-cased, delivery world named
    // off the ephemeris). Kind-specific fields: a crack names its hatch id, a hunt/intel its prey.
    private ContractFacts FactsFor(Quest q) => new(
        Kind: ToContractKind(q.Kind),
        Giver: GiverDisplay(q.Giver),
        DestName: q.TargetCallsign,
        DestParent: q.Kind is QuestKind.CargoRun or QuestKind.Favor ? DestParentName(q.DestBodyId) : null,
        TargetName: q.Kind == QuestKind.Crack ? q.TargetShipId : q.TargetCallsign,
        Pin: q.Pin,
        Charted: q.SourceBodyId is not { } src || !IsBodyHidden(src),
        PickedUp: q.State is QuestState.PickedUp,
        CacheBody: q.Kind == QuestKind.FetchCache ? BodyName(q.SourceBodyId ?? "") : null);

    private static ContractKind ToContractKind(QuestKind kind) => kind switch
    {
        QuestKind.Hunt => ContractKind.Hunt,
        QuestKind.CargoRun => ContractKind.CargoRun,
        QuestKind.Intel => ContractKind.Intel,
        QuestKind.Fetch => ContractKind.Fetch,
        QuestKind.FetchCache => ContractKind.FetchCache,
        QuestKind.Crack => ContractKind.Crack,
        QuestKind.Favor => ContractKind.CargoRun, // a favor delivery reads as a cargo run in the brief
        _ => ContractKind.CargoRun,
    };

    // The delivery world for a cargo run's "…, Mars" place tag — the destination haven's parent
    // planet, skipping a heliocentric station's parent (the sun, which reads wrong as a place).
    private string? DestParentName(string? destId)
    {
        if (BodyById(destId) is not { ParentId: { } pid }) return null;
        if (BodyById(pid) is not { } parent) return null;
        return parent.ParentId is null ? null : parent.Name;
    }

    // Title-case a giver's shout-name for prose ("MADAM COIL" → "Madam Coil", "GILT-EYE" →
    // "Gilt-Eye", "ONE-EYE SILAS" → "One-Eye Silas"). The offer card keeps the loud upper-case ◈
    // name; the ledger receipt and the next-action line read as sentences, so they title-case it.
    private static string GiverDisplay(string giver)
    {
        if (string.IsNullOrWhiteSpace(giver)) return giver;
        return string.Join(' ', giver
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(TitleWord));

        static string TitleWord(string w) => string.Join('-',
            w.Split('-').Select(p => p.Length == 0
                ? p
                : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    // #207: the live next action for the Captain chip — the contract still in hand, its immediate
    // step. An Active cargo run overlays the POSITIONAL detail (too far / in the envelope), read off
    // ship state; every other kind uses the static per-kind step (MissionBrief.Action).
    private string? CaptainQuestChipLine()
    {
        Quest? q = _quests.FirstOrDefault(x => x.State is QuestState.Active or QuestState.PickedUp);
        if (q is null) return null;
        string action = q is { Kind: QuestKind.CargoRun, State: QuestState.Active } && BodyById(q.DestBodyId) is { } dest
            ? $"deliver to {q.TargetCallsign} — {CargoNextAction(dest)}"
            : MissionBrief.Action(FactsFor(q));
        return action.Length == 0 ? null : MissionBrief.NextPrefix + action;
    }

    private void DeclineOffer()
    {
        if (_pendingOffer is null)
        {
            return;
        }
        _pendingOffer = null;
        ShowPulseMessage("“Suit yourself.” The stranger turns back to their drink.");
    }

    // A brought-down target settles any matching hunt contract. Called where a ship is holed or
    // boarded (its two "brought down" moments), keyed on the ship id the quest stored.
    private void CompleteHuntQuests(string shipId)
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.Hunt, State: QuestState.Active } && q.TargetShipId == shipId)
            {
                q.State = QuestState.Complete;
                ShowPulseMessage($"Contract met — {q.TargetCallsign} is down. {q.Reward:N0} cr waiting at any haven. 🎯");
            }
        }
    }

    private const double FetchPickupRangeM = 1e8; // coast within ~100,000 km of the wreck to grab the goods

    // Coasting close to a fetch job's derelict prises the goods loose — flips Active → PickedUp. Called
    // each tick while flying; player-driven state, never read by the physics sim, and idempotent (only
    // an Active fetch matches, so it fires once).
    private void CheckFetchPickup()
    {
        if (_ephemeris is null || _dockedHavenId is not null)
        {
            return;
        }
        foreach (Quest q in _quests)
        {
            if (q is not { Kind: QuestKind.Fetch, State: QuestState.Active } || q.SourceBodyId is null)
            {
                continue;
            }
            Vector2d wreck = _ephemeris.Position(q.SourceBodyId, SimTime);
            if ((_ship.Position - wreck).Length <= FetchPickupRangeM)
            {
                q.State = QuestState.PickedUp;
                ShowPulseMessage($"Got it — the wallet was wedged between the seats. Now get it to {q.TargetCallsign}, quiet-like. 💾");
                RendererInterop.PlayCue("board");
            }
        }
    }

    // Hand the fetched goods to The Fixer at the destination station — face to face, paid under the
    // table, no electronic trace. Unlike a cargo run (settled on berthing), this only completes when
    // you walk to the bar and talk to the contact.
    private void DeliverFetch(Quest q)
    {
        _credits += q.Reward;
        q.State = QuestState.TurnedIn;
        // A history builds even in the shadows — but quietly: no fanfare would suit an under-the-
        // table hand-off, so the relationship is seeded (#185) without the pop-up the bar job gets.
        _contacts.RecordCompletion(q.Giver, q.Giver, q.Reward, SimTime);
        ShowPulseMessage($"The wallet changes hands under the table — +{q.Reward:N0} cr, and we never met. 🕶");
    }

    // Hand the cracked-hatch package back to the Fixer, same station, same under-the-table terms.
    private void DeliverCrack(Quest q)
    {
        _credits += q.Reward;
        q.State = QuestState.TurnedIn;
        _contacts.RecordCompletion(q.Giver, q.Giver, q.Reward, SimTime); // seed the history, keep it quiet
        ShowPulseMessage($"The package slides across the table — +{q.Reward:N0} cr, no receipt. 🕶");
    }

    // Berthing at a haven settles any cargo-run contract bound for it. Called from ToggleDock.
    private void CompleteCargoRunQuests(string dockId)
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.CargoRun or QuestKind.Favor, State: QuestState.Active } && q.DestBodyId == dockId)
            {
                q.State = QuestState.Complete;
                ShowPulseMessage(q.Kind == QuestKind.Favor
                    ? $"Quiet parcel delivered to {q.TargetCallsign} — the favor's worked off. 📡"
                    : $"Parcel delivered to {q.TargetCallsign} — {q.Reward:N0} cr on the counter. 📦");
            }
            // #223: a fetch-a-cache job pays when the DUG chest is carried back to the giver's bar.
            else if (q is { Kind: QuestKind.FetchCache, State: QuestState.PickedUp } && q.DestBodyId == dockId)
            {
                q.State = QuestState.Complete;
                ShowPulseMessage($"Chest delivered to {q.TargetCallsign} — {q.Reward:N0} cr for the recovery. 🗺");
            }
        }
    }

    // #223: digging up a cache advances any fetch-a-cache job that pointed at it — the chest is now in
    // hand (PickedUp); the giver's bar is the drop. Idempotent per quest.
    private void CompleteFetchCacheFor(TreasureCache cache)
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.FetchCache, State: QuestState.Active } && q.TargetShipId == cache.Id)
            {
                q.State = QuestState.PickedUp;
                ShowPulseMessage($"{MissionBrief.NextPrefix}{MissionBrief.Action(FactsFor(q))}");
            }
        }
    }

    // #175: a MOON haven (mu > 0) has no ⚓ dock to clamp — the same as the lie-low rule, where
    // IsHiddenAtHaven treats being BOUND in a haven moon's orbit as "at the haven". So a cargo run to
    // a moon haven delivers the instant the ship is bound in its orbit; only STATION havens (mu = 0)
    // deliver on the dock, which CompleteCargoRunQuests handles from ToggleDock. This closes the trap
    // the owner hit: "berth there to deliver" at Enceladus (a moon) pointed at a door that never existed.
    private bool IsBoundAtMoonHaven(CelestialBody dest)
    {
        if (_ephemeris is null || dest.ParentId is null || IsDockableHaven(dest))
        {
            return false; // a station haven delivers on ⚓ Dock, not by orbit
        }

        if (BodyById(dest.ParentId) is not { } parent)
        {
            return false;
        }

        Vector2d pos = _ephemeris.Position(dest.Id, SimTime);
        const double h = 1.0;
        Vector2d vel = (_ephemeris.Position(dest.Id, SimTime + h) - _ephemeris.Position(dest.Id, SimTime - h)) / (2 * h);
        double hill = OrbitRule.HillRadius(dest, parent.Mu);
        return OrbitRule.IsBound(_ship, pos, vel, dest, hill);
    }

    // #175: settle any moon-haven cargo run the instant the ship is parked in its orbit. Runs on every
    // insertion (manual + autopilot) AND once per tick from UpdateEncounters, so a captain who was
    // ALREADY orbiting when the parcel loaded still gets paid — there is no dock event to hang it on.
    // Station-haven runs stay on the ToggleDock path (CompleteCargoRunQuests) and are skipped here.
    private void CompleteBoundCargoRunQuests()
    {
        if (_ephemeris is null) return;
        bool delivered = false;
        foreach (Quest q in _quests)
        {
            if (q is not { Kind: QuestKind.CargoRun or QuestKind.Favor, State: QuestState.Active, DestBodyId: { } destId }) continue;
            if (BodyById(destId) is not { } dest || IsDockableHaven(dest)) continue; // stations: ⚓ Dock path
            if (IsBoundAtMoonHaven(dest))
            {
                q.State = QuestState.Complete;
                ShowPulseMessage(q.Kind == QuestKind.Favor
                    ? $"Quiet parcel delivered to {q.TargetCallsign} — the favor's worked off. 📡"
                    : $"Parcel delivered to {q.TargetCallsign} — {q.Reward:N0} cr on the counter. 📦");
                delivered = true;
            }
        }

        // No berthing event fires for a moon-haven park, so settle the payout here — the orbit IS the
        // berth. PayCompletedQuests is idempotent (only Complete → TurnedIn), so this can't double-pay.
        if (delivered) PayCompletedQuests();
    }

    // #175: the in-hand cargo run whose destination is this body, or null. Used to paint the 📦 map
    // marker and the live next-action line only while a run to it is actually Active.
    private Quest? ActiveCargoRunTo(string bodyId) =>
        _quests.FirstOrDefault(q => q is { Kind: QuestKind.CargoRun, State: QuestState.Active } && q.DestBodyId == bodyId);

    // The relationship system's seed (#185): the SAVED history of who we've done jobs for — a real
    // fact ("we now have a history with the lady at the Ringside bar") the future system reads.
    // PR-WIRE (FridaySecondPlan §0): the same book now also carries the favor bank's signed credit
    // balances (deposits, loans) via ContactLedger.ApplyCredit.
    private readonly ContactLedger _contacts = new();

    // PR-WIRE — the favor bank. One open bank card at a time (deposit / withdraw / borrow at a
    // contact), plus the favor debts we've taken on (each surfaces later as one quiet delivery in the
    // contact's voice — FavorObligation). Both are session state; the future save layer serializes the
    // ledger balances, not these transient UI holders.
    private BankSession? _bankSession;
    private readonly List<FavorObligation> _favorObligations = [];

    // The open bank card: whose desk we're at, their character sheet, and whether we reached them over
    // the wire (dark-web desk) or in person (their bar table) — the channel gates what's allowed.
    private sealed record BankSession(string ContactId, string DisplayName, ContactSheet Sheet, bool ViaWire)
    {
        public string? Notice { get; set; } // the last action's receipt, shown on the card
    }

    // Standard bank denominations for the card's quick buttons (the purse and balances are in credits).
    private static readonly long[] BankAmounts = [100, 500, 1000];

    // Open the favor-bank card for a contact. Channel-checked by the caller: in person (their bar) works
    // for anyone; over the wire only for a dark-web-native contact (ruling 6).
    private void OpenBank(string contactId, bool viaWire)
    {
        ContactSheet sheet = ContactSheets.For(contactId);
        if (viaWire && !FavorBank.CanBankRemotely(sheet))
        {
            ShowPulseMessage($"{sheet.DisplayName} won't touch the wire — that account is in person only. 🤝");
            return;
        }
        _bankSession = new BankSession(contactId, sheet.DisplayName, sheet, viaWire);
    }

    private void CloseBank() => _bankSession = null;

    // Open the bank at the bar patron you're standing at (the 'b' key, in person). Their character sheet
    // decides nothing here — every contact banks in person — but a stranger with no history still opens,
    // so a first deposit can seed a relationship you later borrow against.
    private void OpenBankAtBar()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.BarPatron } spot)
        {
            ShowPulseMessage("Stand at a contact's table to open an account. 💰");
            return;
        }
        string giver = spot.Label.Replace("◈", "").Trim();
        OpenBank(giver, viaWire: false);
    }

    // Park coin with a contact (the bank). Calm → the whole sum lands and earns interest; heated →
    // fencing: a dice-rolled cut proportional to heat is taken on the way in, ALWAYS less than the
    // collector would confiscate (FavorBank, ruling 5). Deposited coin is off the purse — invisible to
    // confiscation by construction (the BUSTED lane reads only _credits + carried cargo, never the
    // ledger balances). Returns quietly if the purse can't cover it.
    private void BankDeposit(long amount)
    {
        if (_bankSession is not { } session || amount <= 0)
        {
            return;
        }
        long park = Math.Min(amount, _credits);
        if (park <= 0)
        {
            session.Notice = "Nothing in the purse to park.";
            return;
        }

        int heat = _heat.Level;
        double roll = FavorBank.Roll($"{session.ContactId}|deposit|{SimTime:F0}|{park}");
        FavorBank.DepositQuote quote = FavorBank.PriceDeposit(park, heat, roll);

        _credits -= (int)park;                       // the whole sum leaves the purse
        _contacts.ApplyCredit(session.ContactId, session.DisplayName,
            FavorBank.DepositTxn(quote.Credited, SimTime,
                heat > 0 ? $"fenced {park:N0} cr (heat {heat})" : $"parked {park:N0} cr"));
        if (quote.Cut > 0)
        {
            _contacts.ApplyCredit(session.ContactId, session.DisplayName,
                FavorBank.FenceCutTxn(quote.Cut, SimTime, $"fence's cut, {quote.CutFraction * 100:F0}%"));
            session.Notice = $"Fenced {park:N0} cr while hot — {session.DisplayName} took {quote.Cut:N0} cr ({quote.CutFraction * 100:F0}%); {quote.Credited:N0} cr banked, safe from the collectors.";
        }
        else
        {
            session.Notice = $"Parked {park:N0} cr with {session.DisplayName} — off the purse, earning while it's quiet. 💰";
        }
        RequestVaultSave(); // #225: a bank move changed both the purse and the ledger balance
    }

    // Draw parked coin back out. In person or (for a wire contact) over the dark web — the channel was
    // enforced when the card opened. Only the positive part of the balance is ours to withdraw.
    private void BankWithdraw(long amount)
    {
        if (_bankSession is not { } session || amount <= 0)
        {
            return;
        }
        long balance = _contacts.For(session.ContactId).CreditBalance;
        long take = Math.Min(amount, balance);
        if (take <= 0)
        {
            session.Notice = "No coin of yours parked here to draw.";
            return;
        }

        _credits += (int)take;
        _contacts.ApplyCredit(session.ContactId, session.DisplayName,
            FavorBank.WithdrawalTxn(take, SimTime, $"drew {take:N0} cr"));
        session.Notice = $"Drew {take:N0} cr back into the purse from {session.DisplayName}.";
        RequestVaultSave(); // #225: purse + ledger balance changed
    }

    // Pay off an interest-bearing debt with coin on hand (drives a negative balance back toward zero).
    private void BankRepay(long amount)
    {
        if (_bankSession is not { } session || amount <= 0)
        {
            return;
        }
        long owed = -_contacts.For(session.ContactId).CreditBalance; // positive = what we owe
        long pay = Math.Min(Math.Min(amount, owed), _credits);
        if (pay <= 0)
        {
            session.Notice = owed <= 0 ? "You owe them nothing." : "Not enough coin to pay it down.";
            return;
        }

        _credits -= (int)pay;
        _contacts.ApplyCredit(session.ContactId, session.DisplayName,
            FavorBank.RepaymentTxn(pay, SimTime, $"repaid {pay:N0} cr"));
        session.Notice = $"Paid {pay:N0} cr toward what you owe {session.DisplayName}.";
        RequestVaultSave(); // #225: purse + ledger balance changed
    }

    // A modest, standard favor line — roughly a good top-up (≈ half a tank at the inner price). Kept
    // flat so the string (one quiet delivery) is always proportionate; never a fortune on a promise.
    private const long BankLoanPrincipal = 600;

    // Borrow the standard favor line from the contact whose bank card is open (in person or by wire).
    private void BorrowFavorFromBank()
    {
        if (_bankSession is not { } session)
        {
            return;
        }
        if (BankBorrowFavor(session.ContactId, BankLoanPrincipal, session.ViaWire))
        {
            session.Notice = $"{session.DisplayName} wires you {BankLoanPrincipal:N0} cr. You owe them one quiet delivery — it'll come. 📡";
        }
    }

    // Borrow gas money against a favor (the dream's anonymized wire). Books the principal as debt and
    // raises ONE quiet-delivery obligation that arrives later in the contact's voice; working that
    // delivery off IS the repayment. Trusted contacts only. Returns true if the wire went through.
    private bool BankBorrowFavor(string contactId, long principal, bool viaWire)
    {
        ContactSheet sheet = ContactSheets.For(contactId);
        int missions = _contacts.For(contactId).MissionsCompleted;
        if (!ContactSheets.WillStake(missions))
        {
            ShowPulseMessage($"{sheet.DisplayName} won't stake a captain they barely know — do a few jobs first. 🤝");
            return false;
        }
        if (viaWire && !sheet.CanWire)
        {
            ShowPulseMessage($"{sheet.DisplayName} deals in person only — no wire from here. 🤝");
            return false;
        }
        if (principal <= 0)
        {
            return false;
        }

        _credits += (int)principal;
        _contacts.ApplyCredit(contactId, sheet.DisplayName,
            FavorBank.BorrowTxn(principal, SimTime, $"favor wire — {principal:N0} cr gas money"));
        _favorObligations.Add(FavorObligation.ForLoan(sheet, principal, SimTime));
        RequestVaultSave(); // #225: a borrow books coin + a favor-debt obligation
        return true;
    }

    // #223: the captain's hoards — every buried chest we know of, ours and any rival's whose map we
    // hold. Buried loot lives HERE, never in _credits / _cargoByClass, so a boarding confiscation
    // (which reads only carried goods) can never see it. The map card and the ledger's 🗺 section
    // read this book; the discovery watch prunes it.
    private readonly CacheLedger _caches = new();

    // The discovery watch (ruling 4): the last whole day we resolved the per-cache discovery roll, so
    // a warp that skips days can't skip a roll. -1 = nothing resolved yet.
    private long _lastCacheCheckPeriod = -1;

    // The treasure-map card currently on screen (the full-screen artifact), or null. Shown on burying
    // a fresh chest and any time the captain opens a map from the ledger's 🗺 section.
    private TreasureCache? _treasureMapCard;

    // The bury-a-chest chooser: the body we're about to bury on and how much of the purse to sink. Null
    // when the chooser is shut. Cargo goes in whole (the hold is small); coin is the dial.
    private ShuttleStop? _buryTarget;
    private int _buryCoin;

    // The fanfare (#185): a completion is a CELEBRATION, not a silent credit. One pop-up at a time;
    // extras queue so a double payout still gets its due.
    private MissionCelebration? _celebration;
    private readonly Queue<MissionCelebration> _celebrationQueue = new();

    // Turn in any finished contracts on berthing — and make the money's arrival a CELEBRATION
    // (#185, owner: "Now the money just appeared... It is a CELEBRATION"). The payout is booked
    // INSIDE the fanfare flow, the giver is grateful in their own voice, the parrot SINGS, and the
    // job is remembered as a real relationship. The reward never again just silently appears.
    private void PayCompletedQuests()
    {
        foreach (Quest q in _quests)
        {
            if (q.State != QuestState.Complete)
            {
                continue;
            }

            // PR-WIRE: a favor delivery pays no coin — working it off REPAYS the wired debt. Book the
            // principal back onto the ledger (balance climbs toward zero), clear the obligation, and
            // give it a quiet receipt rather than the coin fanfare (no money changed hands).
            if (q.Kind == QuestKind.Favor)
            {
                q.State = QuestState.TurnedIn;
                _contacts.ApplyCredit(q.Giver, q.Giver,
                    FavorBank.RepaymentTxn(q.Reward, SimTime, $"quiet delivery — favor to {GiverDisplay(q.Giver)} repaid"));
                _favorObligations.RemoveAll(o => string.Equals(o.ContactId, q.Giver, StringComparison.OrdinalIgnoreCase));
                ShowPulseMessage($"The favor's square with {GiverDisplay(q.Giver)} — the debt's off the books. 📡");
                continue;
            }

            _credits += q.Reward;            // the coin lands — but as part of the fanfare, not alone
            q.State = QuestState.TurnedIn;

            // Book a real, saved fact: we now have a history with this task giver.
            ContactHistory history = _contacts.RecordCompletion(q.Giver, q.Giver, q.Reward, SimTime);
            _celebrationQueue.Enqueue(Celebrations.ForCompletion(
                q.Title, q.Giver, q.Reward, history.MissionsCompleted, _parrotCounter));
        }

        ShowNextCelebration();
        RequestVaultSave(); // #225: a payout changed the purse, contacts and obligations
    }

    // Surface the next queued fanfare (one pop-up at a time): the parrot SINGS, the cue plays.
    private void ShowNextCelebration()
    {
        if (_celebration is not null || _celebrationQueue.Count == 0)
        {
            return;
        }

        _celebration = _celebrationQueue.Dequeue();
        SquawkNow(Parrot.Squawk.ContractPaid, _lastTimestampMs ?? 0, force: true); // the bird sings 🦜
        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    private void DismissCelebration()
    {
        _celebration = null;
        ShowNextCelebration(); // if a second contract paid at the same berth, raise a glass to it too
    }

    // PR-WIRE — project the favor bank's accounts for the Captain ledger's 💰 section: every contact
    // with coin in the air (parked with them, or owed to them), balance both ways, newest passbook
    // lines first. Contacts with a clean-zero book and no transactions are skipped — nothing to show.
    private Stations.Captain.AccountRow[] LedgerAccounts()
    {
        var rows = new List<Stations.Captain.AccountRow>();
        foreach ((string id, ContactHistory h) in _contacts.Entries)
        {
            if (h.CreditBalance == 0 && h.Transactions.Length == 0)
            {
                continue;
            }
            ContactSheet sheet = ContactSheets.For(id);
            string channel = sheet.CanWire
                ? "🕸 wires anywhere — bank at the dark-web desk or their table"
                : "🤝 in person only — bank at their table (press B)";
            List<string> lines = h.Transactions
                .Reverse()
                .Take(6)
                .Select(t => $"{TxnIcon(t.Kind)} {(t.Amount >= 0 ? "+" : "")}{t.Amount:N0} cr — {t.Note} (day {t.SimTime / 86400:F0})")
                .ToList();
            rows.Add(new Stations.Captain.AccountRow(sheet.DisplayName, h.CreditBalance, channel, lines));
        }
        // Debts first (they need attention), then deposits, largest magnitude on top.
        rows.Sort((x, y) => Math.Sign(x.Balance).CompareTo(Math.Sign(y.Balance)) is var s && s != 0
            ? s
            : Math.Abs(y.Balance).CompareTo(Math.Abs(x.Balance)));
        return rows.ToArray();
    }

    private static string TxnIcon(CreditKind kind) => kind switch
    {
        CreditKind.Deposit => "💰",
        CreditKind.Withdrawal => "🏧",
        CreditKind.Interest => "📈",
        CreditKind.FenceCut => "✂",
        CreditKind.Borrow => "📡",
        CreditKind.Repayment => "✅",
        _ => "·",
    };

    // Project the quest ledger for the Captain's-desk Quests tab (M-Q2) — newest work on top.
    private Stations.Captain.QuestItem[] QuestCards() =>
        _quests.AsEnumerable().Reverse().Select(q =>
        {
            (string label, string kind) = (q.Kind, q.State) switch
            {
                (QuestKind.Intel, _) => ("🕸 Tip taken", "paid"),
                (QuestKind.Fetch, QuestState.Active) when q.SourceBodyId is { } src && IsBodyHidden(src)
                    => ("🔭 Work the tip — scan to find her", "active"),
                (QuestKind.Fetch, QuestState.Active) => ("▶ Fly to the roadster, prise the wallet", "active"),
                (QuestKind.Fetch, QuestState.PickedUp) => ($"📦 Carrying — hand off at {q.TargetCallsign}", "active"),
                (QuestKind.Crack, QuestState.Active) => ($"▶ Crack hatch {q.TargetShipId} — code {q.Pin}", "active"),
                (QuestKind.Crack, QuestState.PickedUp) => ("📦 Package lifted — take it to The Fixer", "active"),
                (QuestKind.FetchCache, QuestState.Active) => ($"🗺 Dig at the X on {BodyName(q.SourceBodyId ?? "")}", "active"),
                (QuestKind.FetchCache, QuestState.PickedUp) => ($"📦 Chest lifted — carry it to {q.TargetCallsign}", "active"),
                (QuestKind.Favor, QuestState.Active) => ($"📡 Favor owed — quiet delivery to {q.TargetCallsign}", "active"),
                (_, QuestState.Active) => ("▶ On the hook", "active"),
                (_, QuestState.Complete) => ("✓ Done — collect at any berth", "complete"),
                _ => ("💰 Paid", "paid"),
            };
            // #175: the delivery instruction is kind-aware — a MOON haven has no ⚓ dock, you park in
            // its orbit; only a STATION haven is "berth there". Saying the right one kills the trap the
            // owner hit hunting for a Dock button that a moon never has.
            CelestialBody? cargoDest = q.Kind is QuestKind.CargoRun or QuestKind.Favor ? BodyById(q.DestBodyId) : null;
            string cargoDetail = cargoDest is not null && !IsDockableHaven(cargoDest)
                ? $"Carry the parcel to {q.TargetCallsign} — park in orbit there to deliver."
                : $"Carry the parcel to {q.TargetCallsign} — berth there (⚓ Dock) to deliver.";
            string detail = q.Kind switch
            {
                QuestKind.Hunt => $"Hunt {q.TargetCallsign} — hole her sail or board her.",
                QuestKind.CargoRun => cargoDetail,
                QuestKind.Favor => $"{cargoDetail} Working it off clears the {q.Reward:N0} cr you owe {GiverDisplay(q.Giver)}.",
                QuestKind.Intel => $"Off-books route on {q.TargetCallsign} — now on your contacts (🕸).",
                QuestKind.Fetch => $"Prise the wallet from the derelict roadster (sunward of Mars), then hand it to The Fixer in person at {q.TargetCallsign}.",
                QuestKind.Crack => $"Key {q.Pin} into hatch {q.TargetShipId} here, lift the package, then hand it back to The Fixer.",
                QuestKind.FetchCache => $"Take the shuttle down to {BodyName(q.SourceBodyId ?? "")}, dig up the marked chest, then carry the lot to {q.TargetCallsign}.",
                _ => "",
            };
            // #175: the live next action for an in-hand cargo run, read off ship state (too far / in the
            // envelope / enter orbit). Only while Active — a delivered run drops back to the plain card.
            string? nextAction = q is { Kind: QuestKind.CargoRun or QuestKind.Favor, State: QuestState.Active } && cargoDest is not null
                ? CargoNextAction(cargoDest)
                : null;
            string rewardText = q.Kind == QuestKind.Intel
                ? "🕸 route tip"
                : q.Kind == QuestKind.Favor
                    ? $"📡 clears {q.Reward.ToString("N0", CultureInfo.InvariantCulture)} cr favor"
                    : $"{q.Reward.ToString("N0", CultureInfo.InvariantCulture)} cr";
            (IReadOnlyList<Stations.Captain.QuestStep> steps, bool showScope) = FetchStagePlan(q);
            return new Stations.Captain.QuestItem(q.Title, detail, rewardText, label, kind, steps, showScope, nextAction);
        }).ToArray();

    // The fetch hunt's staged plan for the quest card (Tuesday plan PR-A): intel → scan → fly →
    // pick up → deliver, ✅ for done, ▶ for the current stage, ▪ for ahead — the tutorial-checklist
    // shape. Also returns whether to surface the 🔭 hook (only while she's still uncharted). Any
    // non-fetch quest gets no checklist.
    private (IReadOnlyList<Stations.Captain.QuestStep> Steps, bool ShowScope) FetchStagePlan(Quest q)
    {
        if (q.Kind != QuestKind.Fetch)
        {
            return ([], false);
        }
        bool charted = q.SourceBodyId is not { } src || !IsBodyHidden(src); // a completed scan charted her
        bool pickedUp = q.State is QuestState.PickedUp or QuestState.Complete or QuestState.TurnedIn;
        bool delivered = q.State is QuestState.TurnedIn;
        (string Text, bool Done)[] flags =
        [
            ("Intel — read the Fixer's transponder fix", true),
            ("Scan — point the scope, resolve her", charted),
            ("Fly — close on the wreck", pickedUp),
            ("Pick up — prise the wallet loose", pickedUp),
            ("Deliver — hand it to The Fixer in person", delivered),
        ];
        int current = Array.FindIndex(flags, f => !f.Done);
        var steps = flags
            .Select((f, i) => new Stations.Captain.QuestStep(f.Done ? "✅" : i == current ? "▶" : "▪", f.Text))
            .ToList();
        return (steps, ShowScope: !charted);
    }

    // The ledger's "Tips & intel" section (PR-J): every scope-intel fix and every fresh route tip,
    // projected for the Captain desk. Scope tips keep their 🔭 action (jump to Sensors, scan queued);
    // route tips carry "→ dark web" always and "→ dossier" when the ship is a known contact. Provenance
    // is attached where we recorded it (Fixer fetches, Gilt-Eye tips, the cheats); older/bought entries
    // render unattributed rather than being withheld.
    // #223: the ledger's 🗺 treasure-maps section — every known cache as a viewable map card.
    private Stations.Captain.CacheMapItem[] LedgerMaps() =>
        _caches.Caches.Select(c => new Stations.Captain.CacheMapItem(
            c.Id, c.Caption(BodyName(c.BodyId)), c.BearingLine, c.ContentsLine(),
            GiverDisplay(c.Owner), c.PlayerOwned)).ToArray();

    // Open the full-screen map card for a cache the captain clicked in the ledger.
    private void ViewMapFromLedger(string cacheId)
    {
        foreach (TreasureCache c in _caches.Caches)
        {
            if (c.Id == cacheId)
            {
                _treasureMapCard = c;
                return;
            }
        }
    }

    private Stations.Captain.LedgerTip[] LedgerTips()
    {
        var tips = new List<Stations.Captain.LedgerTip>();

        // The autopilot's receipts (#147): every stand-down filed as a ledger line, newest first, so a
        // handback the owner warped past is still on the record afterward — the established tip idiom.
        foreach ((double simTime, string text) in _autopilotEvents)
        {
            tips.Add(new Stations.Captain.LedgerTip(
                "🛰 Autopilot", [text], $"logged {FormatSimTime(simTime)}",
                ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null));
        }

        // #202: the piracy receipts — the shadow ledger of the honest jobs. What, units, worth, off
        // whom, where; the sim-when rides the provenance line, same as the autopilot receipts above.
        foreach (LootRecord loot in _lootLedger)
        {
            tips.Add(new Stations.Captain.LedgerTip(
                "🏴 Plunder", [loot.Describe()], $"taken {FormatSimTime(loot.SimTime)}",
                ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null));
        }

        foreach (ScopeIntel si in _scopeIntel)
        {
            string? prov = si.Giver is { } giver ? ProvenanceLine(giver, si.Station ?? "ashore", si.AcquiredSimTime) : null;
            tips.Add(new Stations.Captain.LedgerTip(
                si.Headline, si.Lines, prov,
                ScopeTipId: si.Id, ShowDarkWeb: false, DossierShipId: null));
        }

        foreach (RouteIntel entry in _intelLedger.Entries)
        {
            if (!entry.IsFresh(SimTime))
            {
                continue;
            }
            NpcState? npc = FindNpc(entry.ShipId);
            string ship = npc?.Ship.Callsign ?? entry.ShipId;
            string route = npc is not null ? RouteLabel(npc.Ship) : "route off the books";
            double staleDays = Math.Max(0, entry.SecondsUntilStale(SimTime) / 86400);
            string line = $"{ship} really runs {route} — a ghost, on your contacts (🕸), stale in {staleDays.ToString("F0", CultureInfo.InvariantCulture)} d.";
            string? prov = _routeIntelProvenance.TryGetValue(entry.ShipId, out IntelProvenance? p)
                ? ProvenanceLine(p.Giver, p.Station, p.AcquiredSimTime)
                : null;
            tips.Add(new Stations.Captain.LedgerTip(
                $"🕸 {ship}", [line], prov,
                ScopeTipId: null, ShowDarkWeb: true, DossierShipId: npc is not null ? entry.ShipId : null));
        }

        // #208: a standing note explaining the haven/depot pair the picker now tags — the owner asked
        // for it "in ledger" so the twin-port confusion has one discoverable, permanent answer. Filed
        // last so live tips stay on top; it is evergreen background, no action.
        tips.Add(new Stations.Captain.LedgerTip(
            "⚓ Ports come in twos",
            ["The haven has the bar and the berth; the depot is the pod riding nearby with the goods. Dock at havens; board depots."],
            "standing note",
            ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null));

        return tips.ToArray();
    }
}
