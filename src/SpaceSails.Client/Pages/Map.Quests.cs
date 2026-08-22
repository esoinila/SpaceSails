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

    // #292: the nav screen is not a billboard. The checklist no longer defaults ON — it is raised only
    // by the fresh-Earth greeting (ApplyStart, gated by TutorialPromotion), a deliberate desk-picked
    // lesson (StartTutorial), or the captain's own 🏴 toggle. A loaded save never raises it.
    private bool _showTutorial;
    private int _tutorialStep;                         // 0..N-1 = current task, N = complete

    // #292: whether the captain has ever started or finished a lesson. Persisted through the vault's
    // ProgressSection so a loaded save — or a later fresh Earth start — never re-greets a captain who
    // is no longer truly new. Loaded at boot from the peeked vault (PeekSavedVault) and on every resume.
    private bool _tutorialPlayed;

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
        "Dock at a station's market and sell",
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
        // #962: say which clock this step is watching. It reads the HEAT gauge — and the owner learned the
        // hard way that a cooled gauge is not what calls a collector off ("we have zero heat and are docked
        // at haven ... why is this still hunting us?"). Her own card carries the break-off clock.
        "Lie low until the heat cools to nothing (her contract has its own clock — read her card)",
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
        MarkTutorialPlayed(); // #292: a desk-picked lesson means this captain is no longer "truly new"
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

    // Decline: dismiss; the offer re-opens from the strip. 2026-07-18 playtest: closing a flight-view
    // overlay hands the keyboard back to the map div (RefocusMap), like the treasure-map card does.
    private async Task CloseRescueOffer()
    {
        _showRescueOffer = false;
        await RefocusMap();
    }

    private void ToggleTutorial() => _showTutorial = !_showTutorial;

    // #292: a lesson engaged (started or run to its end) means this captain is no longer truly new —
    // the fresh-Earth greeting must never raise itself again, this run or any future one. Persisted
    // through the vault's ProgressSection so it survives a reload. Idempotent; saves only on the edge.
    private void MarkTutorialPlayed()
    {
        if (_tutorialPlayed)
        {
            return;
        }

        _tutorialPlayed = true;
        RequestVaultSave();
    }

    private void AdvanceTutorial(int completedStep)
    {
        if (_tutorialStep == completedStep)
        {
            _tutorialStep++;
            // #292: following the auto-shown first lesson all the way through counts as having played it,
            // even for a captain who never opened the Tutorials tab — so a later fresh start stays quiet.
            if (_tutorialStep >= TutorialSteps.Length)
            {
                MarkTutorialPlayed();
            }
        }
    }
}
