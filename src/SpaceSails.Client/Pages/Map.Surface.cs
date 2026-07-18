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

    private sealed class Reever
    {
        public double X, Y, Facing, Vx, Vy;
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

        // A chest is in hand right now: something was loaded, not yet buried, not dropped.
        public bool Carrying => (PendingCoin > 0 || PendingCargo.Count > 0) && !Buried && !ChestDropped;
        public bool Channeling => Channel is not null;
    }

    // ── Boarding: pick a surface, optionally load a chest, and grow the tube IN PLACE. ──

    // Destination-first entry (#313). Called from the shuttle bay when the captain chooses a landable
    // surface. The chest is optional cargo already packed by the boarding panel; boarding empty-handed
    // is a complete, valid sightseeing hop. NO teleport: the captain keeps standing at the bay and the
    // down-tube + surface weld on below, so they walk down continuously.
    private void BeginSurfaceExcursion(ShuttleStop stop, ShuttleExcursion.ChestLoad chest)
    {
        if (_ephemeris is null)
        {
            return;
        }
        _boardTarget = null;
        _shuttleBayStops = null;

        AdvanceShuttleClock(stop.TravelSeconds); // the flight down (abstracted by the tube) costs the clock

        _surface = new SurfaceExcursion
        {
            Stop = stop,
            RestoreHavenId = _dockedHavenId,
            PendingCoin = chest.Coin,
            PendingCargo = [.. chest.Cargo],
            ThreatSeed = ReeverSeed(stop.Body.Id),
        };
        _reevers.Clear();
        _lastNearestReeverRange = null;

        RebuildSurfaceDeck();
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;
        _deckPanX = _deckPanY = 0;
        RendererInterop.PlayCue("board");
        string load = chest.IsEmpty
            ? "No chest loaded — a look around, nothing to declare."
            : "A chest rides in the cargo sling.";
        ShowPulseMessage($"🛸 Shuttle mated to {stop.Body.Name}. {load} Walk down the tube. [E] the kiosk, wander, or dig — your call.");
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
        StepReevers(dtRealSeconds);
        StepLingerTrickle(dtRealSeconds);
        TryRecoverDroppedChest();
        StepNerve(dtRealSeconds);
    }

    // ── #317 The nerve gauge: the regolith frays it, the ship's safety eases it, the monolith gores it. ──

    // The stressors tick ONLY out on the regolith (owner: "a sanity bar when on planet"). Moving contacts
    // on the tracker, a live chase, digging under threat and being cornered each drain the nerve; the first
    // sight of the monolith is the big one-time hit (the #226 hook #318 named). Up through the airlock the
    // ship is safety — the ease-off is StepNerveRecovery's job, so this frame does nothing there.
    private void StepNerve(double dtRealSeconds)
    {
        if (_surface is not { } ex || MoonSurface.IsSafeAboard(_avatarY))
        {
            return;
        }

        var stressors = new NerveModel.Stressors(
            MovingContacts: CountMovingReevers(),
            ChaseActive: _reevers.Count > 0,
            Digging: ex.Channeling,
            Cornered: IsCornered());
        _nerve = NerveModel.Drain(_nerve, in stressors, dtRealSeconds);

        // First sight of the monolith — fires exactly once in a captain's life (the flag is vault-persisted).
        if (!_monolithSeen && SeesMonolith())
        {
            _monolithSeen = true;
            _nerve = NerveModel.Shock(_nerve, NerveModel.MonolithSightShock);
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage("👁 The monolith resolves out of the dark — too regular, too old, too patient. Something behind your eyes lurches, and your hands remember it.");
            RequestVaultSave();
        }
    }

    // The ease-off (owner: "back aboard through the airlock eases the nerve over time; the ship is safety").
    // Runs every tick from the sim loop: whenever the captain is NOT out on the regolith — flying, docked,
    // or stood back up through the airlock mid-excursion — the nerve returns gently toward steady. The
    // deeper restoration economy (sleep, R&R, a drink with a friend — #306/#308 seams) stays with #226.
    private void StepNerveRecovery(double dtRealSeconds)
    {
        bool onRegolith = _surface is not null && !MoonSurface.IsSafeAboard(_avatarY);
        if (!onRegolith && _nerve < NerveModel.Max)
        {
            _nerve = NerveModel.Recover(_nerve, dtRealSeconds);
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
        foreach (Reever r in _reevers)
        {
            // Crude encirclement: aim a little toward the tube choke so the pack cuts the escape angle
            // instead of trailing single-file — the cornering loss-condition becomes real geometry.
            double aimX = _avatarX + (MoonSurface.SpawnX - _avatarX) * EncircleBias;
            double aimY = _avatarY + (MoonSurface.SurfaceTopY - _avatarY) * EncircleBias;
            double ox = r.X, oy = r.Y;
            (double nx, double ny) = ReeverChase.Step(r.X, r.Y, aimX, aimY, step, MoonSurface.ReeverBarrierY);
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

    // Lingering wakes more (owner: "the longer you linger, the more turn out") — but ONLY once a pack is
    // already up from a dig, so a sightseeing visit still rolls no dice. A dice-gated trickle: overstaying
    // converts margin into a closing net; a brisk captain never reaches a second tick.
    private void StepLingerTrickle(double dtRealSeconds)
    {
        if (_surface is not { } ex || _reevers.Count == 0 || _reevers.Count >= MaxSurfaceReevers)
        {
            return;
        }
        ex.LingerSeconds += dtRealSeconds;
        int dueTicks = (int)(ex.LingerSeconds / ReeverRaid.LingerTickSeconds);
        while (ex.LingerTicks < dueTicks && _reevers.Count < MaxSurfaceReevers)
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
        _surface = null;
        _reevers.Clear();
        _lastNearestReeverRange = null;

        SetDeckForDock(ex.RestoreHavenId); // rebuild the ship/complex; folds the surface away
        (_avatarX, _avatarY, _avatarHeading) = (-6, -6.5, Math.PI / 2); // step off into the bay
        RendererInterop.PlayCue("board");

        if (buried is { } cache)
        {
            _treasureMapCard = cache;
            RendererInterop.PlayCue("reveal");
            string tail = escapedWithWatchdogs
                ? $" {cache.ReeverLevel} Old One(s) haunt this ground now — the best kind of lock."
                : "";
            ShowPulseMessage($"🛸 Lifted off {ex.Stop.Body.Name}. Map filed (🗺).{tail}");
        }
        else
        {
            string tail = escapedWithWatchdogs ? " You outran the Old Ones." : "";
            ShowPulseMessage($"🛸 Back aboard from {ex.Stop.Body.Name}.{tail}");
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

        return new DeckView.SurfaceHud(
            DigProgress: ex.Channel?.Progress ?? -1,
            SiteX: MoonSurface.DigFieldX, SiteY: MoonSurface.DigFieldY,
            HasDroppedChest: ex.ChestDropped, DropX: ex.DropX, DropY: ex.DropY,
            Blips: blips.Select(b => (b.Bearing, b.Range)).ToList(),
            Cadence: (int)MotionTracker.CadenceFor(nearest),
            Readout: MotionTracker.Readout(nearest, closing),
            CacheMarks: marks,
            Nerve: _nerve,
            NerveReadout: NerveModel.Readout(_nerve));
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
