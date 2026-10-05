using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #653 slice 1 · THE DEAD STATION, FROM THE CAPTAIN'S SIDE. Core knows where its walls, modules and ways in
/// are (<see cref="StationWreck"/>) and what flying between its parts costs (<see cref="StationHop"/>);
/// <see cref="StationInterior"/> dresses that into a deck. This file is the client's half: be standing on it,
/// read a tube that will not let you through, and take the boat from one part of her to another.
///
/// <para><b>Three decisions, each the house idiom's own.</b>
/// (1) <i>No new Map field.</i> The station is a pseudo-body (<c>station-…</c>, the trick the derelict and the
/// expedition rocks use) and everything a visit carries — which module the boat is mated to, which faces have
/// been cut, which canon lines have been told — rides on the <see cref="SurfaceExcursion"/>, where a visit's
/// state already lives.
/// (2) <i>The hop is a console, not a popup.</i> The destinations the boat can reach stand at her dock as
/// consoles (one per destination, its label the price), so there is nothing new to dismiss: the UI law (no
/// pop-up that cannot be closed) is kept by having no pop-up at all, and the decision closes by being made.
/// (3) <i>A relocate is not a rest.</i> The hop spends clock (<see cref="AdvanceShuttleClock"/>, the same one
/// the crossing down spends) and moves the captain. It touches no air, no nerve and no magazine — and the
/// test that pins it compares them before and after.</para>
/// </summary>
public partial class Map
{
    public sealed partial class SurfaceExcursion
    {
        /// <summary>#653 · What this visit to a DEAD STATION is carrying, or null on every other ground — which is
        /// why it is ONE object and not four fields: the frame ledger prints an excursion's properties, and a
        /// ground that is not a station should show one <c>Station=null</c> and nothing else (the man at the
        /// door, <see cref="Gate"/>, is held the same way).</summary>
        public StationVisit? Station { get; init; }
    }

    /// <summary>
    /// #653 · ONE VISIT TO A DEAD STATION: which module's access the boat is mated to, the faces this away team
    /// has cut, and the canon lines already told. Nothing in it outlives the excursion — carrying the cuts across
    /// visits is a TODO on the PR (slice 2's keyed told-once set is the right home for it).
    /// </summary>
    public sealed class StationVisit
    {
        /// <summary>The module whose access the boat is mated to. The hub until she is flown: it is the crew's
        /// own lock, the only access guaranteed serviceable, and where every boarding begins.</summary>
        public StationWreck.ModuleId Dock { get; set; } = StationWreck.ModuleId.Hub;

        /// <summary>The access behind <see cref="Dock"/>, kept so the per-frame questions about "back at the
        /// boat" do not re-roll the station's seeded locks sixty times a second.</summary>
        public StationWreck.Access? DockAccess { get; set; }

        /// <summary>Faces cut this visit. A cut is permanent for the visit: the hop to a cut face is no longer
        /// refused.</summary>
        public HashSet<StationWreck.ModuleId> Cuts { get; } = [];
    }

    /// <summary>Is the away team inside a dead station?</summary>
    private bool OnStation =>
        _surface is { } ex && SiteRoute.IsStation(ex.Stop.Body.Id);

    /// <summary>Is the away team inside ANY hull rather than on a ground — a derelict or a station? The one
    /// question the generic "no regolith here" guards ask, so a station is never quietly treated as a moon.</summary>
    private bool OnADeadHull => OnWreck || OnStation;

    /// <summary>The id of the station the away team is inside, or null.</summary>
    private string? TheStationId =>
        _surface is { } ex ? SiteRoute.StationIdOf(ex.Stop.Body.Id) : null;

    /// <summary>The access the boat is mated to, or null off a station. Every "back at the boat" question —
    /// the air, the nerve, the comms — is asked of it (<see cref="AwayTeamSide"/>).</summary>
    private StationWreck.Access? StationDock
    {
        get
        {
            if (_surface is not { Station: { } visit } ex || TheStationId is not { } id)
            {
                return null;
            }
            return visit.DockAccess ??= StationAboard.AccessOf(id, visit.Dock);
        }
    }

    private StationInterior.StationState TheStationState(SurfaceExcursion ex) =>
        ex.Station is { } visit ? new(visit.Dock, visit.Cuts) : new(StationWreck.ModuleId.Hub, []);

    // ── Standing on her ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The first boarding, said once. The pulse carries the first standing-aboard line; the book gets its 📍
    /// entry — and the BOOK IS THE LATCH, the way the anomaly's is (<c>FileTheAnomalyOnce</c>): "have I written
    /// this down?" is a question the book can answer across a reload, which a flag on the page could not — and a
    /// page field would move the frame ledger for a boolean the vault already knows. A captain who has the entry
    /// has already stood here, so both lines stay quiet on every later boarding.
    /// </summary>
    private void ArriveAtTheStation()
    {
        // The crew lock this boarding has just cycled through is the first lock of the visit: its line is told
        // here, in the log (the pulse is the first-standing line's), and never again this visit.
        if (_surface is { Station: not null } here && here.Told.Tell(ToldOnce.StationLock))
        {
            LogAutopilotEvent(StationAboard.LockLine);
        }

        foreach (Core.FieldNote already in _fieldNotes)
        {
            if (string.Equals(already.Text, StationAboard.FieldBookLine, StringComparison.Ordinal))
            {
                return;
            }
        }

        ShowPulseMessage(StationAboard.FirstStandingLine);
        LogAutopilotEvent(StationAboard.FirstStandingLine);
        FileNote(StationAboard.FieldBookLine, "📍");
    }

    /// <summary>The lock's own line, told once a visit, in the log — and returned as the pulse the first
    /// time so the captain reads it where they are looking.</summary>
    private void TellOnce(SurfaceExcursion ex, string key, string line)
    {
        if (ex.Told.Tell(key))
        {
            ShowPulseMessage(line);
            LogAutopilotEvent(line);
        }
    }

    // ── A tube that will not let you through ─────────────────────────────────────────────────────────────

    /// <summary>Press E at the end of a severed tube: read why. The line is Core's own
    /// (<see cref="StationWreck.BlockageLine"/>) — it is read off the tube itself, so the way on being OUTSIDE is
    /// something understood from in here rather than from a menu.</summary>
    private void ReadTheTube()
    {
        if (TheStationId is not { } id
            || _deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.StationTube } spot
            || StationAboard.ArmOfTubeEnd(spot.X, spot.Y) is not { } arm)
        {
            return;
        }

        ShowPulseMessage(StationWreck.BlockageLine(id, arm));
    }

    // ── The boat ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Press E at a destination console. A refused one that needs a cut spends the captain's own hull cutter
    /// (<see cref="StationEntry"/>, which is <see cref="HullCutter.Force"/>); an open one flies the boat.
    /// </summary>
    private void PressTheHop()
    {
        if (_surface is not { } ex || TheStationId is not { } id
            || _deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.StationHop } spot
            || StationAboard.DestinationAt(id, ex.Station!.Dock, ex.Station.Cuts, spot.X, spot.Y) is not { } picked)
        {
            return;
        }

        (StationWreck.ModuleId target, StationHop.Quote quote) = picked;
        StationWreck.Access arrival = StationAboard.AccessOf(id, target);

        if (quote.Refused == StationHop.Refusal.NeedsACut)
        {
            CutIntoTheFace(ex, arrival, quote);
            return;
        }

        FlyTheBoat(ex, id, arrival, StationAboard.WithTheLock(quote, arrival));
    }

    /// <summary>The cut: a clock, a cell's worth of cutter, and a face that stays cut. With no rig, the
    /// refusal in Core's own words and the cutter's own — and nothing changes.</summary>
    private void CutIntoTheFace(SurfaceExcursion ex, StationWreck.Access arrival, StationHop.Quote quote)
    {
        StationEntry.Order order = StationEntry.Enter(arrival, ex.Station!.Cuts, _satchel);
        if (!order.Admitted)
        {
            ShowPulseMessage(StationHop.RefusalLine(quote, StationWreck.ModuleOf(ex.Station!.Dock).Name));
            LogAutopilotEvent(order.Line);
            return;
        }

        _satchel = [.. order.Carried];
        ex.Station!.Cuts.Add(arrival.Module);
        _roomsTurnedOver.Add(StationAboard.CutTag(TheStationId!, arrival.Module));   // remembered across visits (the vault carries it)
        AdvanceShuttleClock(order.Seconds);
        if (_busted is not null)
        {
            return;
        }

        RendererInterop.PlayCue("reveal");
        TellOnce(ex, ToldOnce.StationCut, order.Line);
        LogAutopilotEvent(order.CellLine);
        RebuildSurfaceDeck();   // the console now names a flight, not a cut
        RequestVaultSave();
    }

    /// <summary>Cast off, cross, come alongside: the clock pays the flight and the lock, the captain is put
    /// down inside the new access, and NOTHING is topped up on the way — a relocate is not a rest.</summary>
    private void FlyTheBoat(SurfaceExcursion ex, string id, StationWreck.Access arrival, StationHop.Quote quote)
    {
        string castOff = StationHop.CastOffLine(quote);
        ShowPulseMessage(castOff);
        LogAutopilotEvent(castOff);

        AdvanceShuttleClock(quote.Seconds);
        if (_busted is not null)
        {
            return;
        }

        ex.Station!.Dock = arrival.Module;
        ex.Station.DockAccess = arrival;
        RendererInterop.PlayCue("board");
        StandCaptainAt(quote.LandX, quote.LandY, "the boat comes alongside");

        if (arrival.Kind == StationWreck.AccessKind.ServiceableLock)
        {
            TellOnce(ex, ToldOnce.StationLock, StationAboard.LockLine);
        }
    }

    // ── What the HUD says ────────────────────────────────────────────────────────────────────────────────

    private string StationKeyHints(SurfaceExcursion ex)
    {
        var hints = new List<string> { "WASD — move", "E — read / press" };
        // #212 · AFFORDANCES NEVER HIDE: the wreck branch's sentry lines, mirrored — a bot riding the sling can be set
        // down aboard, and the remote is in the captain's hand.
        if (ex.Bots.Count > 0)
        {
            hints.Add(_weaponsTight ? "🤖 H — WEAPONS TIGHT (press to free)" : "🤖 H — weapons tight");
        }
        if (ex.Bots.Any(b => !b.Deployed))
        {
            hints.Add($"🤖 T — {SentryDoctrine.DeployHereLabel}");
            hints.Add($"🤖 ⇧T — {SentryDoctrine.HoldMyLineHomeLabel}");
        }
        if (_satchel.Count > 0)
        {
            hints.Add($"🎒 I — items ({_satchel.Count})");
        }
        hints.Add(_audioEnabled ? "🔊 M — mute" : "🔇 M — unmute");
        return string.Join(" ∙ ", hints);
    }

    /// <summary>
    /// The station's HUD: the marks that belong on any deck, the tank (it runs here, and a silent timer that
    /// kills you is the one thing the suit's rule forbids — #564) and none of the regolith's instruments:
    /// there is no tide, so there is nothing for a fan to sweep.
    /// </summary>
    private DeckView.SurfaceHud BuildStationHud(SurfaceExcursion ex)
    {
        RefreshHudBots(ex);
        _hudHusks.Clear();
        _hudBlips.Clear();

        (string Line, int Severity, int CommsState)? orbit = SurfaceComms();
        double home = DistanceToTheTube();

        return new DeckView.SurfaceHud(
            DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            Blips: _hudBlips, Cadence: 0, Readout: "", CacheMarks: [],
            Nerve: _nerve, NerveReadout: NerveModel.Readout(_nerve),
            Instruments: false,
            Bots: _hudBots, Husks: _hudHusks,
            KeyHints: StationKeyHints(ex),
            OrbitComms: orbit?.Line, OrbitSeverity: orbit?.Severity ?? 0, CommsState: orbit?.CommsState ?? 0,
            AirSeconds: ex.AirSeconds, AirDistanceHome: home, AirBudgetSeconds: ex.AirBudgetSeconds,
            AirSupply: AirSupplyOf(ex),
            Countdown: SuitAir.RunningLow(ex.AirSeconds, home) || SuitAir.OnTheReserve(ex.AirSeconds)
                ? (_avatarX, _avatarY + 2.6, $"O2 {(int)(ex.AirSeconds / 60)}:{(int)(ex.AirSeconds % 60):00}")
                : null,
            ShuttleLegs: TheShuttleLegsRing());
    }
}
