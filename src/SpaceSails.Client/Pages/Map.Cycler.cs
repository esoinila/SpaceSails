using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Cycler — #411 THE ROUTE. The arc has promised, since its first line on a dedication plate, that a
// berth nobody files for could be filed for. This is where that stops being a predicate.
//
// The owner ruled the DESTINATION on 2026-08-03 (the head of the organization, "as fancy as the secret
// labs") and left the vehicle open; #411's comment costed three. This is option B — the berth code buys a
// LISTED SUPPLY RUN — with the cycler window as that run's timetable. The whole argument is in
// docs/features/kaamos-head-office.md §1; the short version is that the bible's own sentence is "filing
// for it answers it", and B is the only option where the captain literally files for it, at a counter,
// like any other job, and the dread is that the job is routine.
//
// Three things, and nothing else:
//
//   1. THE RUN. Once CanReachEnceladus, a contact hands over a consignment against the berth that has just
//      come back onto the listing. It is an ordinary CargoRun to a moon haven, which means the ordinary
//      completion path (CompleteBoundCargoRunQuests — park in orbit there and it's done) settles it with
//      no new code at all. The manifest slug is the cold pod's, verbatim, because it IS that consignment.
//   2. THE WINDOW. CyclerWindow is pure and world-blind: a grid over sim time, a gate, and the sentences.
//   3. THE RIDE. The crossing, cloning EngageLongHaulTo's commit order exactly — compute, refuse-or-commit,
//      vault-save, cinematic, re-seed at the arrival epoch, place the ship, announce. It charges NO pulses
//      and arms NO arrival brake, and that is not a shortcut: the arc does the flying and lets her go
//      co-moving. The ship is a passenger. It costs 38 days and nothing else.
//
// What this lane deliberately does NOT gate: whether the ice moon has anything on it. The head office is a
// fact about the world, keyed on the berth code alone, so a captain who crosses the deep black the long
// way round arrives at the same door. A route that is the only way in is a railroad.
public partial class Map
{
    /// <summary>#742 · <c>/map?kaamos=hq&amp;arrivalphase=N</c> — which of the ice moon's 24 arrival phases
    /// the head-office seat should let her go on, or null for whichever one boot time happens to be. Read
    /// at boot, applied to the clock immediately before the park is built, and touched by nothing else:
    /// the honest ride takes the phase its own 38-day crossing lands on, exactly as it always did.</summary>
    private int? _arrivalPhaseCheat;

    /// <summary>Is the KAAMOS supply run in hand — filed to this hull, not yet signed for? The run's own
    /// quest is the state; there is no second flag, because a bool beside a quest is two answers to one
    /// question and this repo has a table of what that costs.</summary>
    private bool KaamosRunFiled =>
        _quests.Any(q => q.Id == KaamosLore.SupplyRunQuestId && q.State != QuestState.TurnedIn);

    /// <summary>Has this universe ever been handed the run? (Filed, delivered or otherwise — the offer is
    /// made once.)</summary>
    private bool KaamosRunEverTaken => _quests.Any(q => q.Id == KaamosLore.SupplyRunQuestId);

    // ── 1 · The run, offered at a counter like anything else ─────────────────────────────────────────────

    /// <summary>The KAAMOS supply run, or null if this captain is not on the board or already has it.
    /// Offered by whoever is drinking here: the person handing it over has no idea what it is, which is
    /// the point. To them it is a dormant berth that came back onto the listing and a consignment nobody
    /// has moved in decades, and the fee is generous because the haul is absurd.</summary>
    private Quest? MakeKaamosSupplyRunOffer(string giver)
    {
        if (!_kaamos.CanReachEnceladus || KaamosRunEverTaken || BodyById(KaamosLore.IceMoonBodyId) is null)
        {
            return null;
        }

        int reward = HaulReward.ForHaul(
            HelioRadiusMeters(_dockedHavenId), HelioRadiusMeters(KaamosLore.IceMoonBodyId));

        return new Quest(KaamosLore.SupplyRunQuestId, QuestKind.CargoRun, giver,
            "", BodyName(KaamosLore.IceMoonBodyId), KaamosLore.SupplyRunTitle,
            KaamosLore.SupplyRunBlurb(reward), reward, DestBodyId: KaamosLore.IceMoonBodyId);
    }

    // ── 2 · The gate ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What is stopping the ride right now — Core's verdict, never a client opinion.</summary>
    private CyclerWindow.Blocker KaamosCyclerBlocker() =>
        CyclerWindow.Evaluate(_kaamos.CanReachEnceladus, KaamosRunFiled, SimTime);

    /// <summary>Should the body menu show the cycler row at all for this body? Only at the ice moon, and
    /// only for a captain who is at least on the board — a control offered to somebody who has never heard
    /// of the arc is the fiction arriving early (#380), which is the one thing this arc has always got
    /// right and must keep getting right.</summary>
    private bool KaamosCyclerRowVisible(string bodyId) =>
        bodyId == KaamosLore.IceMoonBodyId && _kaamos.CanReachEnceladus && !_jumpInProgress;

    /// <summary>The row's label: the action when it can go, the wait when the arc is simply not here.</summary>
    private string KaamosCyclerLabel() =>
        KaamosCyclerBlocker() == CyclerWindow.Blocker.None
            ? CyclerWindow.MenuAction
            : CyclerWindow.MenuWaiting(SimTime);

    /// <summary>Why it will not go, or null when it will. Visible-but-disabled with the reason — #212's
    /// law, and the reason every refusal sentence in <see cref="CyclerWindow"/> exists.</summary>
    private string? KaamosCyclerBlock()
    {
        CyclerWindow.Blocker blocker = KaamosCyclerBlocker();
        return blocker == CyclerWindow.Blocker.None ? null : CyclerWindow.RefusalText(blocker, SimTime);
    }

    // ── 3 · The ride ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Ride the window in. The commit order is <c>EngageLongHaulTo</c>'s, step for step, because
    /// that order was paid for: gate first, compute before any side effect, vault-save BEFORE the clock
    /// moves (a tab death mid-crossing then loses only the crossing), then the cinematic, then the atomic
    /// re-seed-place-advance.</summary>
    private async Task RideTheCyclerWindow()
    {
        CloseBodyMenu();
        if (_ephemeris is null || _jumpInProgress)
        {
            return;
        }

        if (KaamosCyclerBlock() is { } block)
        {
            ShowPulseMessage("❄ " + block);
            return;
        }

        // #742 — the free park, not the clamped berth. Nobody is holding her out here, so the standoff's
        // direction IS the orbit she flies while the captain is 23 floors down, and laying it along the
        // Sun's radius put one arrival phase in 24 into the ice at +9.54 h. BerthState.CoOrbital lays it
        // along the moon's own track instead; the arrival phase stops deciding anything.
        double arrivalEpoch = SimTime + CyclerWindow.CrossingSeconds;
        ShipState arrival = BerthState.CoOrbital(
            _ephemeris, KaamosLore.IceMoonBodyId, arrivalEpoch, CyclerWindow.ArrivalOffsetMeters);

        // Committing the departure. No burn fires and no pulses are charged: the ship is not flying this,
        // the arc is. What it costs is the one thing a captain cannot buy back.
        if (_dockedHavenId is not null)
        {
            Undock();
        }

        ShowPulseMessage(CyclerWindow.Departing);
        LogAutopilotEvent(CyclerWindow.Departing);
        RequestVaultSave();
        FlushVaultSaveIfDirty();

        _jumpTotalYears = LongHaul.VoidYears(CyclerWindow.CrossingSeconds);
        _jumpYear = 0;
        _jumpDestName = BodyName(KaamosLore.IceMoonBodyId);
        _jumpFlavor = VoidFlavor(_jumpTotalYears);
        _jumpActive = true;
        _jumpInProgress = true;
        RendererInterop.PlayCue("voidjump");
        StateHasChanged();

        await RunVoidCinematic();

        ReseedWorldForJump(arrivalEpoch);
        _ship = arrival;
        SimTime = arrivalEpoch;

        StaleFutureNodes();
        ResetAutopilotBudget();
        _autopilotStandDownReason = null;
        _skipActive = false;
        _passDirty = true;
        _scrubOffsetSeconds = 0;

        _jumpInProgress = false;
        _jumpActive = false;

        ShowPulseMessage(CyclerWindow.Arrived);
        LogAutopilotEvent(CyclerWindow.Arrived);
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        ReprojectTrajectory();
        StateHasChanged();
    }

    // ── The dev seat ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#411 · <c>/map?kaamos=hq</c> — the whole route already walked: every shard assembled, the
    /// berth code resolved, the supply run filed and the ship let go alongside the ice. Add <c>&amp;land=1</c>
    /// and the shuttle takes it from there.
    ///
    /// <para>It exists because of this file's own rule, written next to the cheats in <c>Map.Sim</c>: "a
    /// scene nobody can reach on demand is a scene that ships broken." The honest way in is five shards,
    /// a capstone and a forty-day wait, and nobody is regression-testing that twice.</para>
    ///
    /// <para>Applied EARLY — before <c>?land=</c> fires — because the shuttle board is computed off where
    /// the ship floats, and a cheat that lands you on the wrong moon is worse than one that refuses.</para></summary>
    private void SeedKaamosHeadOfficeCheat()
    {
        foreach (KaamosFragment f in KaamosLore.Fragments)
        {
            _kaamos.Assemble(f.Id);
        }

        // …and this cheat IS a clandestine-site cheat, so it says so, in the field the surface path already
        // reads. Without it `?kaamos=hq&land=1` sets the captain down on open regolith and `?floor=N` does
        // nothing — because both of those hang off `_secretLabForceBodyId`, and a testing-guide row that
        // promised `&floor=23` would have been this project's third bug class in its own documentation.
        // An organic landing at the ice moon is untouched: nothing sets this but a cheat.
        _secretLabForceBodyId = KaamosLore.IceMoonBodyId;

        // And name the ground, rather than trusting `?land=1` to pick the nearest thing in reach. It would
        // pick the ice moon today; a cheat that silently lands you somewhere else is worse than one that
        // refuses, and "somewhere else" is a thing a future scenario edit could quietly introduce.
        _forcedLandingBodyId = KaamosLore.IceMoonBodyId;

        if (!KaamosRunEverTaken && BodyById(KaamosLore.IceMoonBodyId) is not null)
        {
            // Priced the way the offer would have priced it, and BEFORE the undock below, because the purse
            // scales on the haul from the berth the job was taken at. A cheat that hands over the same job
            // for a different number is a cheat that playtests a contract the game does not issue.
            int reward = HaulReward.ForHaul(
                HelioRadiusMeters(_dockedHavenId), HelioRadiusMeters(KaamosLore.IceMoonBodyId));

            _quests.Add(new Quest(KaamosLore.SupplyRunQuestId, QuestKind.CargoRun, "THE BOARD",
                "", BodyName(KaamosLore.IceMoonBodyId), KaamosLore.SupplyRunTitle,
                KaamosLore.SupplyRunBlurb(reward), reward, DestBodyId: KaamosLore.IceMoonBodyId));
        }

        if (_ephemeris is not null)
        {
            // Through the real undock, not by clearing the field: a berth is a clamp, a resume point and a
            // deck, and a cheat that leaves two of those behind is a cheat that playtests a state the game
            // can never be in.
            if (_dockedHavenId is not null)
            {
                Undock();
            }

            // #742 · &arrivalphase=N — wind the clock to the named arrival phase, immediately before the
            // park is built off it, so the ride lets her go on the phase the tester named rather than on
            // whichever one boot time happened to be. The moon's period is READ OFF THE MOON rather than
            // typed in here: a moon's number belongs to the moon, and a copy of it in a client file is a
            // bug class this repo keeps a table of. Set here and nowhere earlier, so the reward above is
            // still priced at the epoch the offer would have priced it at — and the hull is rebuilt at the
            // new epoch on the very next statement, so the clock never runs ahead of the ship (that is
            // #733's freeze, and it is not being re-introduced in order to demonstrate #742).
            if (_arrivalPhaseCheat is { } phase && BodyById(KaamosLore.IceMoonBodyId) is { } ice)
            {
                SimTime = CyclerWindow.ArrivalPhaseEpoch(ice.OrbitPeriod, phase);
            }

            // The same free park the honest ride builds (#742) — a cheat that parks you on a different
            // orbit from the one the arc actually leaves you on is a cheat that playtests nothing.
            _ship = BerthState.CoOrbital(
                _ephemeris, KaamosLore.IceMoonBodyId, SimTime, CyclerWindow.ArrivalOffsetMeters);
            _passDirty = true;
        }

        RequestVaultSave();
        ShowPulseMessage(
            "🧪 Test: PROJEKTI KAAMOS assembled, the berth-code resolved, the supply run filed, and the ship " +
            "let go alongside the ice moon — the whole route already ridden. Add &land=1 to put boots on it." +
            (_arrivalPhaseCheat is { } p
                ? $" Arrival phase {p}/{CyclerWindow.ArrivalPhases}, clock wound to t = {SimTime:F0} s — " +
                  "#742's drift used to start here."
                : ""));
    }
}
