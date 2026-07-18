using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Surface — the walked surface excursion (#295 walked bury; #313 destination-first). The shuttle
// asks WHERE, not WHY: boarding offers a destination, the tube grows in place, and the captain walks
// down and commits to nothing. Digging is a timed, abortable channel; the Old Ones (Reevers) shamble
// but come in numbers, converging from the edges — the motion tracker is the early warning, the
// crew-only door is the sanctuary, and nothing on the moon ever self-resolves.
public partial class Map
{
    // Old-Ones tuning (#313, owner: "little more goblin like in their speed... their fear is that they
    // are so many"): a shamble well under the captain's 9.0 — even a chest-carrying captain (0.8×9=7.2)
    // outpaces them, so the menace is numbers + persistence, not pace. FLAGGED for the owner's verdict.
    private const double ReeverSpeed = 5.6;

    // Carrying a chest slows the captain to 7.2 du/s — still faster than the shamble, but DROPPING it
    // (panic key) restores full 9.0. The honest carry-speed default the owner left unruled (flagged).
    private const double CarryChestSpeedFactor = 0.8;

    // A dig (bury OR lift) is a channeled action: several real seconds of shovel-work you can be caught
    // mid. The vulnerability window the owner asked for.
    private const double DigChannelSeconds = 3.6;

    // Encirclement: each Old One leans this fraction of its aim toward the tube mouth (the choke), so
    // the pack cuts angles to corner the captain instead of trailing single-file. Cheap, no pathfinding.
    private const double EncircleBias = 0.28;

    private const int MaxSurfaceReevers = ReeverRaid.MaxReevers; // buffer: 3 crew + 6 ≤ MaxDroids(10)

    // #318 false-hang follow-up: per-frame ceilings for the linger trickle. The step delta is clamped to
    // MaxSurfaceStepSeconds (the same 0.1 s cap StepReevers uses) so a background-tab resume can't hand a
    // multi-second delta into the wake accumulator, and at most MaxLingerWakesPerFrame wake-checks run in
    // any one frame — a hard guard so the loop can never spin the frame (see ReeverRaid.LingerTicksDue).
    private const double MaxSurfaceStepSeconds = 0.1;
    private const int MaxLingerWakesPerFrame = 8;

    // #317 · The nerve gauge (first slice of #226's Fail Forward sanity). The captain's nerve, 0..100:
    // full = steady hands, empty = nerves shot. Drains from the regolith's stressors, eases off aboard,
    // and — unlike Reever positions — PERSISTS in the vault (a captain who fled shaking is still shaking
    // after a reload). Display-first: the bar bottoming out only SPEAKS; consequences stay with #226.
    private double _nerve = NerveModel.Steady;
    private bool _monolithSeen; // the Lovecraftian first-sight hit fires once in a life (persisted)

    // First sight of the monolith: within this many deck-units of it, the captain lays eyes on the thing
    // (owner's #313 maze). Reaches the maze approach (outer wall ~12 du out) with margin. FLAGGED for tuning.
    private const double MonolithSightRange = 26.0;

    // Cornered: a Reever wedged up-field of the captain and this close laterally reads as a net across the
    // escape (owner: "being cornered"). A cheap geometry check — no pathfinding. FLAGGED for tuning.
    private const double CornerLateralRange = 7.0;

    // The live excursion, or null when we're not on a surface. Reever state is client-only real-time
    // (never saved — same law as any NPC position).
    private SurfaceExcursion? _surface;
    private readonly List<Reever> _reevers = [];
    private double _lastReeverCatchMs;
    private double? _lastNearestReeverRange; // for the tracker's closing/drifting read

    // #327 the ship calls home: the mothership's station-keeping hold (sim-seconds) at the moment the
    // captain boarded DOWN — the reference the escalating ladder measures against (OrbitHold). Positive
    // = boarded with a real kept-orbit hold; 0 = boarded onto an orbit no one is keeping (a standing red
    // on the surface). Set in BeginSurfaceExcursion, read by SurfaceOrbitComms.
    private double _orbitHoldAtBoarding;

    // #327: the in-voice orbit line the surface HUD shows — the ship calling down as its hold erodes. The
    // owner's Miranda maroon was LOVED as story; the SILENCE was the bug. While the shuttle is down and
    // the mothership floats FREE (a moon is no dockable berth), the ship reports its hold every tick:
    // steady → slipping → failing → lost, never buried. Null only OFF-surface; on an excursion it always
    // speaks. A docked ship gets its own calm line (#331 follow-up) — the station holds it, no fuel spent
    // — instead of a hold countdown, and the ladder can never fire (this returns before any StageFor).
    private (string Line, int Severity)? SurfaceOrbitComms()
    {
        if (_surface is null)
        {
            return null; // not on a surface — nothing to report
        }

        if (_dockedHavenId is not null)
        {
            // Owner ruling (#331 follow-up): docked at a station, its mass holds the orbit for us — no
            // fuel spent, no hold to count down. Say so plainly rather than a countdown or a false "∞".
            return (OrbitHold.DockedComms, 0);
        }

        if (_orbitKept)
        {
            double remaining = OrbitHold.HoldSeconds(_reactionMassPulses, _keepTrimPulsesPerDay);
            double boarding = _orbitHoldAtBoarding > 0 ? _orbitHoldAtBoarding : remaining;
            OrbitHold.Stage stage = OrbitHold.StageFor(remaining, boarding);
            return (OrbitHold.Comms(stage, remaining), OrbitHold.Severity(stage));
        }

        // Not keeping. If we boarded WITH a hold, the keeper has since given up (the tank ran dry, a loud
        // handback) — the orbit is degrading: the maroon, announced. If we never had a hold, no one was
        // ever trimming it — a standing red the whole excursion. Either way, loud, never silent.
        return _orbitHoldAtBoarding > 0
            ? (OrbitHold.Comms(OrbitHold.Stage.Lost, 0), OrbitHold.Severity(OrbitHold.Stage.Lost))
            : (OrbitHold.NotHoldingComms, 2);
    }

    // #318 false-hang follow-up: true while the tube + wide-surface plan welds on after 'Board' — the
    // brief synchronous build the loading-style descent door covers (a flying 🛸), so a slow build reads
    // as the shuttle ride, not a frozen click. See BeginSurfaceExcursion.
    private bool _shuttleDescending;

    // #314: the ship's sentry roster — the two real boarding troopers (K-77, R-3B), each with a 99-round
    // magazine that survives a berth-to-berth save (Map.Vault). Full on a fresh ship; drained by use,
    // refilled at a haven's rearm line (Map.Trade). Bots carried down to a surface leave this list for the
    // excursion and return (unless abandoned).
    private readonly List<ShipBot> _shipBots =
        [.. SentryBot.RosterUnits.Select(u => new ShipBot(u, SentryBot.MaxMagazine))];

    private sealed class ShipBot(string unit, int rounds)
    {
        public string Unit { get; } = unit;
        public int Rounds { get; set; } = rounds;
    }

    private sealed class Reever
    {
        public double X, Y, Facing, Vx, Vy;
        public int HitsTaken;   // #314: rounds a sentry has ground into it (downs at RoundsPerReever)

        // #324: crude line-of-sight memory. A Reever only tracks the captain's LIVE position while it can
        // SEE them (no wall between); blind, it shambles to where it last laid eyes, then leans on the tube
        // choke. Duck behind a wall and it loses your live position — the maze becomes a real instrument.
        public double LastSeenX, LastSeenY;
        public bool EverSeen;
    }

    // #314: a sentry on the surface — carried in the sling or deployed and holding the line, with its
    // dwindling magazine. Deployed bots fire the SentryBot volley; a firing bot flags a brief zap line.
    private sealed class SurfaceBot
    {
        public required string Unit { get; init; }
        public int Rounds { get; set; }
        public bool Deployed { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double AimX { get; set; }
        public double AimY { get; set; }
        public double FiringUntilMs { get; set; }
    }

    private sealed class DigChannel
    {
        public double Progress;       // 0..1
        public bool BuryingNew;       // true = bury a carried chest; false = lift a cache
        public string? CacheId;       // the cache being lifted (null for a new bury)
        public ReeverRoll Roll;       // rolled at channel START so the threat can interrupt the bar
        public bool Rolled;           // reevers spawned for this channel
    }

    private sealed class SurfaceExcursion
    {
        public required ShuttleStop Stop { get; init; }
        public required string? RestoreHavenId { get; init; }
        public int PendingCoin { get; set; }
        public List<CacheCargo> PendingCargo { get; init; } = [];
        public bool ChestDropped { get; set; }
        public double DropX, DropY;
        public bool Buried { get; set; }                 // the carried chest went into the ground
        public DigChannel? Channel { get; set; }
        public double LingerSeconds { get; set; }        // time a pack has been up (drives the trickle)
        public int LingerTicks { get; set; }
        public ulong ThreatSeed { get; set; }
        public TreasureCache? Cache { get; set; }        // set on a completed bury (for the map card)
        public int Catches { get; set; }
        public List<SurfaceBot> Bots { get; init; } = [];  // #314: sentries carried + deployed this excursion
        public List<(double X, double Y)> Husks { get; init; } = [];  // #314: downed Old Ones, left where they fell (#316)
        public double FireTimer { get; set; }              // #314: accrues to the SentryBot fire cadence

        // A chest is in hand right now: something was loaded, not yet buried, not dropped.
        public bool Carrying => (PendingCoin > 0 || PendingCargo.Count > 0) && !Buried && !ChestDropped;
        public bool Channeling => Channel is not null;
    }

    // ── Boarding: pick a surface, optionally load a chest, and grow the tube IN PLACE. ──

    // Destination-first entry (#313). Called from the shuttle bay when the captain chooses a landable
    // surface. The chest is optional cargo already packed by the boarding panel; boarding empty-handed
    // is a complete, valid sightseeing hop. NO teleport: the captain keeps standing at the bay and the
    // down-tube + surface weld on below, so they walk down continuously.
    private async Task BeginSurfaceExcursion(ShuttleStop stop, ShuttleExcursion.ChestLoad chest, int botsToBring = 0)
    {
        if (_ephemeris is null)
        {
            return;
        }
        _boardTarget = null;
        _shuttleBayStops = null;

        // #318 follow-up: the tube + wide-surface plan build below is synchronous (small on Release,
        // amplified on the Debug bundle). Raise the honest 'shuttle descending…' door — a 🛸 flying on a
        // compositor-thread CSS animation — and let it PAINT before the block, so a slow build reads as
        // the ride down, not a frozen click. Dropped once the surface is welded on and walkable.
        _shuttleDescending = true;
        StateHasChanged();
        await Task.Delay(1);

        AdvanceShuttleClock(stop.TravelSeconds); // the flight down (abstracted by the tube) costs the clock

        var excursion = new SurfaceExcursion
        {
            Stop = stop,
            RestoreHavenId = _dockedHavenId,
            PendingCoin = chest.Coin,
            PendingCargo = [.. chest.Cargo],
            ThreatSeed = ReeverSeed(stop.Body.Id),
        };

        // #314: pull up to botsToBring sentries off the ship's roster into the sling (carried, not yet
        // deployed). They leave _shipBots for the excursion and return on liftoff unless abandoned.
        int take = Math.Clamp(botsToBring, 0, _shipBots.Count);
        for (int i = 0; i < take; i++)
        {
            ShipBot b = _shipBots[0];
            _shipBots.RemoveAt(0);
            excursion.Bots.Add(new SurfaceBot { Unit = b.Unit, Rounds = b.Rounds, Deployed = false });
        }

        _surface = excursion;
        _reevers.Clear();
        _lastNearestReeverRange = null;

        // #327: snapshot the mothership's hold at the moment of boarding DOWN — the reference the surface
        // ladder erodes against. A kept orbit quotes pulses ÷ Lab-25 trim rate; an unkept one is 0 (the
        // surface then flies a standing "not holding" red). A berthed ship carries no orbit risk (0 too;
        // SurfaceOrbitComms gates it out by _dockedHavenId anyway).
        _orbitHoldAtBoarding = _orbitKept && _dockedHavenId is null
            ? OrbitHold.HoldSeconds(_reactionMassPulses, _keepTrimPulsesPerDay)
            : 0;

        RebuildSurfaceDeck();
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;
        _deckPanX = _deckPanY = 0;
        _shuttleDescending = false; // the surface is welded on and walkable — drop the descent door
        RendererInterop.PlayCue("board");
        string load = chest.IsEmpty
            ? "No chest loaded — a look around, nothing to declare."
            : "A chest rides in the cargo sling.";
        string bots = take > 0
            ? $" {take} sentry bot{(take == 1 ? "" : "s")} in the sling — press T on the surface to set one down."
            : "";
        ShowPulseMessage($"🛸 Shuttle mated to {stop.Body.Name}. {load}{bots} Walk down the tube. [E] the kiosk, wander, or dig — your call.");
    }

    // (Re)build the ship + tube + surface plan for the live excursion, honoring what we carry and which
    // of our caches are in this ground. Keeps the avatar where they stand — the world grows, nobody
    // teleports (the #133 "opened wing appears without teleporting anyone" law, pointed downward).
    private void RebuildSurfaceDeck()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        _deckPlan = MoonSurface.SurfaceDeck(
            ex.Stop.Body.Name, ex.Carrying, OwnCachePositionsAt(ex.Stop.Body.Id),
            3 + MaxSurfaceReevers, FillSurfaceDroids);
    }

    private List<(string Id, double X, double Y, int ReeverLevel)> OwnCachePositionsAt(string bodyId)
    {
        var list = new List<(string, double, double, int)>();
        foreach (TreasureCache c in _caches.CachesAt(bodyId))
        {
            if (!c.PlayerOwned)
            {
                continue;
            }
            (double x, double y) = MoonSurface.CachePosition(c.Id);
            list.Add((c.Id, x, y, c.ReeverLevel));
        }
        return list;
    }

    // ── The dig site [E]: a timed, abortable channel. The 2D6 roll fires at channel START so the pack
    //    can turn out and close on you WHILE the bar fills — the watch is the gameplay. ──

    private void DigSiteInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (ex.Channeling)
        {
            return; // already digging — stepping away aborts, [E] doesn't re-trigger
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.DigSite } spot)
        {
            return;
        }

        bool buryHere = spot.Label.StartsWith("⛏", StringComparison.Ordinal);
        if (buryHere)
        {
            if (!ex.Carrying)
            {
                ShowPulseMessage("Nothing in the sling to bury.");
                return;
            }
            BeginDig(ex, buryingNew: true, cacheId: null);
        }
        else
        {
            // 'Dig at the X': the own cache nearest this mark.
            string? nearest = NearestOwnCacheId(ex.Stop.Body.Id, spot.X, spot.Y);
            if (nearest is null)
            {
                ShowPulseMessage("The X is scuffed to nothing — no chest here.");
                return;
            }
            BeginDig(ex, buryingNew: false, cacheId: nearest);
        }
    }

    private string? NearestOwnCacheId(string bodyId, double x, double y)
    {
        string? best = null;
        double bestSq = double.MaxValue;
        foreach ((string id, double cx, double cy, int _) in OwnCachePositionsAt(bodyId))
        {
            double d = (cx - x) * (cx - x) + (cy - y) * (cy - y);
            if (d < bestSq)
            {
                (bestSq, best) = (d, id);
            }
        }
        return best;
    }

    // Start the channel and ROLL THE WATCHDOGS NOW — the pack (if any) turns out at the edges and begins
    // to shamble in while the shovel-bar fills. No modal: the dice reveal rides the pulse line, the grid
    // stays visible so the captain watches the tide.
    private void BeginDig(SurfaceExcursion ex, bool buryingNew, string? cacheId)
    {
        int standing = WatchdogLevelAt(ex.Stop.Body.Id);
        ReeverRoll roll = ReeverRaid.Roll(ReeverSeed(ex.Stop.Body.Id), standing);
        ex.Channel = new DigChannel { BuryingNew = buryingNew, CacheId = cacheId, Roll = roll };
        RendererInterop.PlayCue("board");
        RaiseReevers(roll); // spawn the pack (if roused) so it's already closing during the bar
        ex.Channel.Rolled = true;
        ShowPulseMessage(buryingNew
            ? "⛏ Digging a hole… hold position. Watch the tracker — step away to abort."
            : "⛏ Working the X open… hold position. Step away to abort.");
    }

    // Advance the channel each frame. Stepping off the site aborts (chest back in hand, hole abandoned,
    // sprint begins); filling the bar completes the act.
    private void StepDigChannel(double dtRealSeconds)
    {
        if (_surface is not { Channel: { } ch } ex)
        {
            return;
        }
        // Away from the site → abort.
        double siteDist = double.MaxValue;
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is { Kind: DeckPlan.ConsoleKind.DigSite })
        {
            siteDist = 0;
        }
        if (siteDist > DeckPlan.InteractRadius)
        {
            AbortDig(ex);
            return;
        }

        ch.Progress += dtRealSeconds / DigChannelSeconds;
        if (ch.Progress >= 1.0)
        {
            CompleteDig(ex, ch);
        }
    }

    private void AbortDig(SurfaceExcursion ex)
    {
        ex.Channel = null;
        ShowPulseMessage(_reevers.Count > 0
            ? "🩸 You drop the shovel — the ground's abandoned. RUN (or drop the chest: press G)."
            : "You stop digging and shoulder the chest. The hole's left half-dug.");
    }

    private void CompleteDig(SurfaceExcursion ex, DigChannel ch)
    {
        ex.Channel = null;
        if (ch.BuryingNew)
        {
            BuryChestHere(ex, ch.Roll);
        }
        else if (ch.CacheId is { } id)
        {
            LiftChestHere(ex, id, ch.Roll);
        }
    }

    // The carried chest goes into the ground — invisible to confiscation by construction. The presence
    // LEFT on the chest is the pack that turned out (the standing watchdog level, hardened by this roll).
    private void BuryChestHere(SurfaceExcursion ex, ReeverRoll roll)
    {
        int coin = Math.Clamp(ex.PendingCoin, 0, _credits);
        _credits -= coin;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();

        int standing = WatchdogLevelAt(ex.Stop.Body.Id);
        int presence = Math.Max(standing, roll.Reevers);
        TreasureCache cache = _caches.Bury(ex.Stop.Body.Id, coin, ex.PendingCargo, SimTime, "you", playerOwned: true, presence);
        SeedDiscoveryWatch();

        ex.Buried = true;
        ex.Cache = cache;
        RebuildSurfaceDeck(); // the ⛏ site is spent; the new ✗ joins the ground
        RequestVaultSave();
        ShowPulseMessage($"⛏ Chest buried — {cache.ContentsLine()} off the books. Now get back to the shuttle.");
    }

    private void LiftChestHere(SurfaceExcursion ex, string cacheId, ReeverRoll roll)
    {
        if (_caches.Dig(cacheId) is not { } c)
        {
            return;
        }
        _credits += c.Coin;
        int unitsBack = 0, unitsLost = 0;
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
            unitsLost += line.Units - take;
        }
        CompleteFetchCacheFor(c);
        _ = roll; // the pack already turned out at channel start
        RebuildSurfaceDeck(); // the ✗ is gone
        RequestVaultSave();
        string lost = unitsLost > 0 ? $" ({unitsLost}u left — hold full)" : "";
        ShowPulseMessage($"🗺 Dug up {c.Coin:N0} cr + {unitsBack} units{lost}. Back to the shuttle.");
        PayCompletedQuests();
    }

    // The panic choice (owner's unruled carry-speed, settled): DROP the chest to run full speed. The
    // dropped chest stays on the grid to recover (walk back onto it and [E]).
    private void DropChest()
    {
        if (_surface is not { Carrying: true } ex)
        {
            return;
        }
        ex.ChestDropped = true;
        ex.DropX = _avatarX;
        ex.DropY = _avatarY;
        if (ex.Channel is not null)
        {
            ex.Channel = null;
        }
        RebuildSurfaceDeck();
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage("🪤 Chest dropped! Full sprint now — come back for it when the ground's clear.");
    }

    private void TryRecoverDroppedChest()
    {
        if (_surface is not { ChestDropped: true } ex)
        {
            return;
        }
        double d = Math.Sqrt((_avatarX - ex.DropX) * (_avatarX - ex.DropX) + (_avatarY - ex.DropY) * (_avatarY - ex.DropY));
        if (d <= DeckPlan.InteractRadius)
        {
            ex.ChestDropped = false;
            RebuildSurfaceDeck();
            RendererInterop.PlayCue("board");
            ShowPulseMessage("🧰 Chest back in the sling.");
        }
    }

    // ── The 2D6 Old Ones: turn out, spawn converging from the edges, and NEVER stop. ──

    private void RaiseReevers(ReeverRoll roll)
    {
        if (!roll.Roused)
        {
            ShowPulseMessage($"🎲 {roll.Describe()} — the ground stays quiet. For now.");
            return;
        }
        SpawnReevers(roll.Reevers);
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage($"🎲 {roll.Describe()} — the OLD ONES stir! {roll.Reevers} shamble up from the regolith, converging. Patient, ancient, and many. Don't get cornered.");
    }

    // Spawn a pack spread across the deep field so they converge from several bearings (not single file)
    // onto the captain and the tube line — the motion-tracker "wall of signal" moment.
    private void SpawnReevers(int count)
    {
        double baseY = Math.Min(_avatarY - 4, MoonSurface.MonolithY + 10);
        for (int i = 0; i < count; i++)
        {
            if (_reevers.Count >= MaxSurfaceReevers)
            {
                break;
            }
            double frac = count > 1 ? i / (double)(count - 1) : 0.5;
            double x = -40 + frac * 70 + (i % 2 == 0 ? -3 : 3);
            double y = baseY - (i % 3) * 4;
            _reevers.Add(new Reever { X = x, Y = Math.Min(y, MoonSurface.ReeverBarrierY - 1), Facing = Math.PI / 2 });
        }
    }

    // The surface tick: chase, dig channel, and the linger trickle — all cheap, no pathfinding.
    private void StepSurface(double dtRealSeconds)
    {
        if (_surface is null)
        {
            return;
        }
        StepDigChannel(dtRealSeconds);
        StepSentries(dtRealSeconds);
        StepReevers(dtRealSeconds);
        StepLingerTrickle(dtRealSeconds);
        TryRecoverDroppedChest();
    }

    // ── #317 The nerve gauge: the regolith frays it, the ship's safety eases it, the monolith gores it. ──

    // The one per-frame nerve advance, called from the sim loop every tick (not just on the surface): the
    // pure NerveModel.Advance owns the whole on-planet law — drain only out on the regolith (moving contacts,
    // a live chase, digging under threat, being cornered), the once-in-a-life monolith first-sight hit (the
    // #226 hook #318 named), and the airlock/off-planet ease-off (the ship is safety). The client's only job
    // is to read the live situation and, when the big hit fires, sound the cue and speak.
    private void StepNerve(double dtRealSeconds)
    {
        bool onExcursion = _surface is { } ex;
        bool onRegolith = onExcursion && !MoonSurface.IsSafeAboard(_avatarY);

        var frame = new NerveModel.Frame(
            OnExcursion: onExcursion,
            OnRegolith: onRegolith,
            SeesMonolith: onRegolith && SeesMonolith(),
            Stressors: onRegolith
                ? new NerveModel.Stressors(CountMovingReevers(), _reevers.Count > 0, _surface!.Channeling, IsCornered())
                : default,
            DtSeconds: dtRealSeconds);

        NerveModel.Step step = NerveModel.Advance(_nerve, _monolithSeen, in frame);
        _nerve = step.Nerve;
        _monolithSeen = step.MonolithSeen;

        if (step.MonolithHitFired)
        {
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage("👁 The monolith resolves out of the dark — too regular, too old, too patient. Something behind your eyes lurches, and your hands remember it.");
            RequestVaultSave();
        }
    }

    private int CountMovingReevers()
    {
        int n = 0;
        foreach (Reever r in _reevers)
        {
            if (MotionTracker.IsMoving(r.Vx, r.Vy))
            {
                n++;
            }
        }
        return n;
    }

    // A net between the captain and the tube: an Old One wedged up-field (nearer the tube mouth than the
    // captain) and laterally close enough to block the sprint. Cheap geometry, matching the encirclement
    // the pack already leans into — the "cornered" the owner named, priced as a stressor.
    private bool IsCornered()
    {
        foreach (Reever r in _reevers)
        {
            if (r.Y > _avatarY + 1.0 && r.Y <= MoonSurface.SurfaceTopY + 0.5 &&
                Math.Abs(r.X - _avatarX) < CornerLateralRange)
            {
                return true;
            }
        }
        return false;
    }

    private bool SeesMonolith()
    {
        double dx = _avatarX - MoonSurface.MonolithX;
        double dy = _avatarY - MoonSurface.MonolithY;
        return (dx * dx) + (dy * dy) <= MonolithSightRange * MonolithSightRange;
    }

    // #314: the sentry line. Every SentryBot.FireIntervalSeconds, deployed non-dry bots each put one
    // round into the nearest Old One in their arc — the counter ticks down, the Reever soaks a hit, and
    // at RoundsPerReever hits it drops to a husk left where it fell. Pure resolution in Core; this owns
    // the cadence, the zap-line flash, and the husk ledger. Dry bots freeze silent.
    private void StepSentries(double dtRealSeconds)
    {
        if (_surface is not { } ex || ex.Bots.Count == 0)
        {
            return;
        }
        ex.FireTimer += dtRealSeconds;
        if (ex.FireTimer < SentryBot.FireIntervalSeconds)
        {
            return;
        }
        ex.FireTimer = 0;

        var live = ex.Bots.Where(b => b.Deployed && b.Rounds > 0).ToList();
        if (live.Count == 0 || _reevers.Count == 0)
        {
            return;
        }

        var deployed = live.Select(b => new SentryBot.Deployed(b.Unit, b.X, b.Y, b.Rounds)).ToList();
        var targets = _reevers.Select(r => new SentryBot.Target(r.X, r.Y, r.HitsTaken)).ToList();
        SentryBot.Volley volley = SentryBot.Step(deployed, targets);

        // Fold the drained magazines back and flash a zap line from each bot that fired.
        double nowMs = _lastTimestampMs ?? 0;
        for (int i = 0; i < live.Count; i++)
        {
            SurfaceBot bot = live[i];
            bool fired = volley.Bots[i].Rounds < bot.Rounds;
            bot.Rounds = volley.Bots[i].Rounds;
            if (fired && NearestReeverInArc(bot) is { } aim)
            {
                bot.AimX = aim.X;
                bot.AimY = aim.Y;
                bot.FiringUntilMs = nowMs + 120;
            }
        }

        // Re-map surviving Reevers' hit counts (position-match; the list order is preserved by Step's
        // survivor pass, which drops downed ones in index order). Rebuild from the survivor list.
        ApplyReeverSurvivors(volley.Reevers);

        if (volley.Husks.Count > 0)
        {
            foreach (SentryBot.Husk h in volley.Husks)
            {
                ex.Husks.Add((h.X, h.Y));
            }
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage($"🔫 Zap — {volley.Husks.Count} Old One{(volley.Husks.Count == 1 ? "" : "s")} down, {(volley.Husks.Count == 1 ? "a husk" : "husks")} left in the regolith. The sentries hold — watch the counters.");
        }
        // No per-shot cue: the guns fire five times a second — the zap-line flash and the ticking
        // counter carry the feedback; only a downed Old One earns a sound.
    }

    // Rebuild _reevers from the SentryBot survivor snapshot: downed ones are gone, survivors carry their
    // new hit counts. Matches by index over the live list Step was fed (same order, downed dropped).
    private void ApplyReeverSurvivors(IReadOnlyList<SentryBot.Target> survivors)
    {
        // Survivors preserve the fed order with downed entries removed, so walk both lists in step.
        int s = 0;
        var kept = new List<Reever>(survivors.Count);
        foreach (Reever r in _reevers)
        {
            if (s < survivors.Count && Math.Abs(survivors[s].X - r.X) < 1e-6 && Math.Abs(survivors[s].Y - r.Y) < 1e-6)
            {
                r.HitsTaken = survivors[s].HitsTaken;
                kept.Add(r);
                s++;
            }
            // else: this Reever was downed this volley — drop it.
        }
        if (kept.Count != _reevers.Count)
        {
            _reevers.Clear();
            _reevers.AddRange(kept);
        }
    }

    private (double X, double Y)? NearestReeverInArc(SurfaceBot bot)
    {
        double bestSq = SentryBot.RangeDeckUnits * SentryBot.RangeDeckUnits;
        (double, double)? best = null;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - bot.X, dy = r.Y - bot.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 <= bestSq)
            {
                bestSq = d2;
                best = (r.X, r.Y);
            }
        }
        return best;
    }

    // #314: deploy a carried sentry at the captain's feet, or retrieve a deployed one they're standing on.
    // The [E]-style act on the bare ground — no console, so it's the T key (Map.Deck). Retrieval wins when
    // you're on top of a bot (dry or not); else you set one down.
    private void DeployOrRetrieveSentry()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        // Retrieve: a deployed bot within reach → back into the sling (keeps its remaining rounds).
        SurfaceBot? onFoot = null;
        double bestSq = DeckPlan.InteractRadius * DeckPlan.InteractRadius;
        foreach (SurfaceBot b in ex.Bots)
        {
            if (!b.Deployed)
            {
                continue;
            }
            double dx = b.X - _avatarX, dy = b.Y - _avatarY;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 <= bestSq)
            {
                bestSq = d2;
                onFoot = b;
            }
        }
        if (onFoot is not null)
        {
            onFoot.Deployed = false;
            RendererInterop.PlayCue("board");
            ShowPulseMessage($"🤖 {onFoot.Unit} shouldered — counter at {SentryBot.Readout(onFoot.Rounds)}. Back in the sling.");
            return;
        }

        // Deploy: the first carried bot goes down where you stand, facing the field.
        SurfaceBot? carried = ex.Bots.FirstOrDefault(b => !b.Deployed);
        if (carried is null)
        {
            ShowPulseMessage(ex.Bots.Count == 0
                ? "No sentry bots loaded — bring them down at boarding next time."
                : "Every bot's already deployed. Walk onto one and press T to pick it up.");
            return;
        }
        carried.Deployed = true;
        carried.X = _avatarX;
        carried.Y = _avatarY;
        RendererInterop.PlayCue("board");
        ShowPulseMessage($"🤖 {carried.Unit} deployed — magazine {SentryBot.Readout(carried.Rounds)}. It'll hold this arc until the counter reads 00. Bots buy time, not safety.");
    }

    private void StepReevers(double dtRealSeconds)
    {
        if (_surface is null || _reevers.Count == 0)
        {
            return;
        }
        double dt = Math.Min(dtRealSeconds, 0.1);
        double step = ReeverSpeed * dt;
        bool onSurface = !MoonSurface.IsSafeAboard(_avatarY);
        bool caught = false;
        // #324: the maze is law for the many too — the Reevers bump-and-slide on the SAME wall segments
        // the captain does, and can only see the captain when no wall stands between.
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionSegments;
        const double reeverRadius = DeckPlan.AvatarRadius;
        foreach (Reever r in _reevers)
        {
            // #314: a live sentry pins the Old Ones on its arc — a Reever under a deployed, non-dry bot's
            // guns is held where it stands (stopped, not slowed) while it's ground down. Once the counter
            // reads 00 the gun goes quiet and the shamble resumes. This is "bots buy time, never safety".
            if (PinnedBySentry(r))
            {
                r.Vx = 0;
                r.Vy = 0;
                r.Facing = Math.Atan2(_avatarY - r.Y, _avatarX - r.X);
                if (onSurface && ReeverChase.Caught(r.X, r.Y, _avatarX, _avatarY))
                {
                    caught = true;
                }
                continue;
            }
            // #324 line-of-sight: a Reever tracks the captain's LIVE position only while it can see them.
            // A wall between the two breaks the look — then it shambles to the last spot it saw them, and
            // (having never seen them, or arrived there) leans on the tube choke it always knows. Duck
            // behind stone and the hunter loses your live position; a stopped Reever also drops off the
            // motion tracker (motion-only law) — breaking sight in the maze is now real play.
            if (SurfaceCollision.HasLineOfSight(r.X, r.Y, _avatarX, _avatarY, walls))
            {
                r.LastSeenX = _avatarX;
                r.LastSeenY = _avatarY;
                r.EverSeen = true;
            }
            double tgtX = r.EverSeen ? r.LastSeenX : MoonSurface.SpawnX;
            double tgtY = r.EverSeen ? r.LastSeenY : MoonSurface.SurfaceTopY;

            // Crude encirclement: aim a little toward the tube choke so the pack cuts the escape angle
            // instead of trailing single-file — the cornering loss-condition becomes real geometry.
            double aimX = tgtX + (MoonSurface.SpawnX - tgtX) * EncircleBias;
            double aimY = tgtY + (MoonSurface.SurfaceTopY - tgtY) * EncircleBias;
            double ox = r.X, oy = r.Y;
            (double nx, double ny) = ReeverChase.Step(r.X, r.Y, aimX, aimY, step, MoonSurface.ReeverBarrierY, walls, reeverRadius);
            r.Vx = dt > 0 ? (nx - ox) / dt : 0;
            r.Vy = dt > 0 ? (ny - oy) / dt : 0;
            r.X = nx;
            r.Y = ny;
            r.Facing = Math.Atan2(_avatarY - ny, _avatarX - nx);
            if (onSurface && ReeverChase.Caught(nx, ny, _avatarX, _avatarY))
            {
                caught = true;
            }
        }
        if (caught)
        {
            ReeverCatch();
        }
    }

    // True if any deployed, non-dry sentry has this Old One inside its firing arc — the pin that holds it.
    private bool PinnedBySentry(Reever r)
    {
        if (_surface is not { } ex)
        {
            return false;
        }
        foreach (SurfaceBot b in ex.Bots)
        {
            if (b.Deployed && b.Rounds > 0 && SentryBot.InRange(b.X, b.Y, r.X, r.Y))
            {
                return true;
            }
        }
        return false;
    }

    // Lingering wakes more (owner: "the longer you linger, the more turn out") — but ONLY once a pack is
    // already up from a dig, so a sightseeing visit still rolls no dice. A dice-gated trickle: overstaying
    // converts margin into a closing net; a brisk captain never reaches a second tick.
    private void StepLingerTrickle(double dtRealSeconds)
    {
        if (_surface is not { } ex || _reevers.Count == 0 || _reevers.Count >= MaxSurfaceReevers)
        {
            return;
        }
        // #318 false-hang follow-up: clamp the frame delta before it feeds an accumulator + loop. A tab
        // resumed from the background (rAF suspended) can hand us a delta of many seconds; StepReevers
        // already caps its own step at MaxSurfaceStepSeconds for the same reason. Then advance at most a
        // small, fixed number of wake-checks this frame (ReeverRaid.LingerTicksDue caps it) — the honest
        // fallback is to catch any backlog up over the next few frames, never spin one frame.
        ex.LingerSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);
        int advance = ReeverRaid.LingerTicksDue(ex.LingerSeconds, ex.LingerTicks, MaxLingerWakesPerFrame);
        for (int i = 0; i < advance && _reevers.Count < MaxSurfaceReevers; i++)
        {
            ex.LingerTicks++;
            if (ReeverRaid.WakesOnLingerTick(ex.ThreatSeed, ex.LingerTicks))
            {
                SpawnReevers(1);
                RendererInterop.PlayCue("alarm");
                ShowPulseMessage("🎲 Another Old One claws free — the net thickens. Leave, captain.");
            }
        }
    }

    // A caught digger: no loot taken (the whole point) — it prices the danger in heat, the same lever the
    // law's collectors use. Debounced so one brush isn't a stunlock.
    private void ReeverCatch()
    {
        double now = _lastTimestampMs ?? 0;
        if (now - _lastReeverCatchMs < 1500)
        {
            return;
        }
        _lastReeverCatchMs = now;
        if (_surface is { } ex)
        {
            ex.Catches++;
        }
        _heat = EncounterRule.RaiseHeat(_heat, 1, SimTime);
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage("🩸 An Old One lays hands on you — it wants no loot, only you. Tear free and RUN!");
        RequestVaultSave();
    }

    // ── Liftoff: board the shuttle (player-initiated ONLY — nothing self-resolves). ──

    private void LiftOffFromSurface()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        ex.Channel = null;
        bool escapedWithWatchdogs = _reevers.Count > 0;
        TreasureCache? buried = ex.Cache;

        // #314: carried sentries come home (with their drained magazines); any left DEPLOYED on the
        // ground is abandoned — a write-off with a ledger line (#119 voice). Retrieve them before liftoff
        // to keep them.
        int abandoned = 0;
        foreach (SurfaceBot b in ex.Bots)
        {
            if (b.Deployed)
            {
                abandoned++;
                LogAutopilotEvent(SentryBot.AbandonLedgerLine(b.Unit, b.Rounds));
            }
            else
            {
                _shipBots.Add(new ShipBot(b.Unit, b.Rounds));
            }
        }

        _surface = null;
        _reevers.Clear();
        _lastNearestReeverRange = null;

        SetDeckForDock(ex.RestoreHavenId); // rebuild the ship/complex; folds the surface away
        (_avatarX, _avatarY, _avatarHeading) = (-6, -6.5, Math.PI / 2); // step off into the bay
        RendererInterop.PlayCue("board");

        string botTail = abandoned > 0
            ? $" {abandoned} sentry bot{(abandoned == 1 ? "" : "s")} left behind — written off."
            : "";
        if (buried is { } cache)
        {
            _treasureMapCard = cache;
            RendererInterop.PlayCue("reveal");
            string tail = escapedWithWatchdogs
                ? $" {cache.ReeverLevel} Old One(s) haunt this ground now — the best kind of lock."
                : "";
            ShowPulseMessage($"🛸 Lifted off {ex.Stop.Body.Name}. Map filed (🗺).{tail}{botTail}");
        }
        else
        {
            string tail = escapedWithWatchdogs ? " You outran the Old Ones." : "";
            ShowPulseMessage($"🛸 Back aboard from {ex.Stop.Body.Name}.{tail}{botTail}");
        }
    }

    // ── The lonely automated kiosk (#313 amenity): a PLACE has shops. Pulse receipts (#119 style),
    //    house voice — last restocked before the war. ──

    private static readonly (string Item, int Price, string Line)[] KioskStock =
    [
        ("a MIRANDA souvenir tee", 15, "The print's cracked; the sizing is 'optimistic pre-war human'."),
        ("a fridge magnet", 8, "It clamps to your suit's chestplate and refuses to let go. Value: eternal."),
        ("a vacuum-sealed hot meal", 12, "The label promises 'MEAT-ADJACENT'. The heater still works. Mostly."),
    ];

    private int _kioskPicks;

    private void VisitKiosk()
    {
        (string item, int price, string line) = KioskStock[_kioskPicks % KioskStock.Length];
        _kioskPicks++;
        if (_credits < price)
        {
            ShowPulseMessage($"🛒 {item} — {price} cr. The slot blinks INSUFFICIENT FUNDS in a dead language. Empty pockets, captain.");
            return;
        }
        _credits -= price;
        RendererInterop.PlayCue("board");
        ShowPulseMessage($"🧾 Bought {item} for {price} cr. {line} (The kiosk was last restocked before the war.)");
    }

    // ── The droid buffer: the ship's crew, plus the live Old Ones on the surface. ──

    private void FillSurfaceDroids(double simTime, DeckPlan.Droid[] buffer)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // [0..3): the crew
        for (int i = 0; i < MaxSurfaceReevers; i++)
        {
            int slot = 3 + i;
            if (i < _reevers.Count)
            {
                Reever r = _reevers[i];
                buffer[slot] = new DeckPlan.Droid(r.X, r.Y, r.Facing, "Reever");
            }
            else
            {
                buffer[slot] = new DeckPlan.Droid(-9999, -9999, 0, "Reever");
            }
        }
    }

    // ── The motion tracker HUD (#313): a crude corner sweep of MOVING contacts, built for the renderer.
    //    Motion only — a wall-blocked, momentarily-still Old One drops off the fan. ──

    private DeckView.SurfaceHud? BuildSurfaceHud()
    {
        if (_surface is not { } ex)
        {
            return null;
        }
        var entities = _reevers.Select(r => new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy));
        IReadOnlyList<MotionTracker.Blip> blips = MotionTracker.Sweep(_avatarX, _avatarY, entities);
        double? nearest = blips.Count > 0 ? blips[0].Range : null;
        bool closing = nearest is { } n && _lastNearestReeverRange is { } prev && n < prev - 0.01;
        _lastNearestReeverRange = nearest;

        var marks = OwnCachePositionsAt(ex.Stop.Body.Id)
            .Select(c => (c.X, c.Y, c.ReeverLevel > 0)).ToList();

        double nowMs = _lastTimestampMs ?? 0;
        var bots = ex.Bots
            .Where(b => b.Deployed)
            .Select(b => (b.X, b.Y, SentryBot.Readout(b.Rounds), b.Rounds <= 0, b.FiringUntilMs > nowMs, b.AimX, b.AimY))
            .ToList();
        var husks = ex.Husks.Select(h => (h.X, h.Y)).ToList();

        (string Line, int Severity)? orbit = SurfaceOrbitComms(); // #327: the ship calling home

        return new DeckView.SurfaceHud(
            DigProgress: ex.Channel?.Progress ?? -1,
            SiteX: MoonSurface.DigFieldX, SiteY: MoonSurface.DigFieldY,
            HasDroppedChest: ex.ChestDropped, DropX: ex.DropX, DropY: ex.DropY,
            Blips: blips.Select(b => (b.Bearing, b.Range)).ToList(),
            Cadence: (int)MotionTracker.CadenceFor(nearest),
            Readout: MotionTracker.Readout(nearest, closing),
            CacheMarks: marks,
            Nerve: _nerve,
            NerveReadout: NerveModel.Readout(_nerve),
            Bots: bots,
            Husks: husks,
            KeyHints: BuildSurfaceKeyHints(ex),
            OrbitComms: orbit?.Line,          // #327: the ship's calling-home line, never buried
            OrbitSeverity: orbit?.Severity ?? 0);
    }

    // #324: the contextual surface keybar. The owner couldn't find the deploy key — so while a bot rides
    // the sling it spells out [T] deploy, and a chest in hand spells [G] drop. Affordances never hide.
    private string BuildSurfaceKeyHints(SurfaceExcursion ex)
    {
        var parts = new List<string> { "WASD — move", "E — dig / use" };
        bool carryingBot = ex.Bots.Any(b => !b.Deployed);
        bool deployedUnderfoot = ex.Bots.Any(b => b.Deployed &&
            ((b.X - _avatarX) * (b.X - _avatarX)) + ((b.Y - _avatarY) * (b.Y - _avatarY))
                <= DeckPlan.InteractRadius * DeckPlan.InteractRadius);
        if (carryingBot)
        {
            parts.Add("🤖 T — deploy a sentry");
        }
        else if (deployedUnderfoot)
        {
            parts.Add("🤖 T — pick up the sentry");
        }
        if (ex.Carrying)
        {
            parts.Add("G — drop the chest & sprint");
        }
        parts.Add("F — first person");
        return string.Join(" ∙ ", parts);
    }

    // Seed the 2D6 from place + integer-second instant — deterministic, replayable in a test.
    private ulong ReeverSeed(string bodyId) => DiceRule.Seed($"reever:{bodyId}", (long)SimTime);

    // The highest watchdog presence standing over any chest already at this body (the ground's memory).
    private int WatchdogLevelAt(string bodyId)
    {
        int level = 0;
        foreach (TreasureCache c in _caches.CachesAt(bodyId))
        {
            level = Math.Max(level, c.ReeverLevel);
        }
        return level;
    }
}
