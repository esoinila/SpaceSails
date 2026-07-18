using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Surface — the walked bury scene (#295). The owner buried his first chest on Miranda and got
// "just the array of pop-ups"; he wanted to WALK, "as if the shuttle was the door", and to see Reevers
// pop up so the beat is a sprint back to the shuttle. So landing to bury (or to dig) now opens a walked
// surface: the shuttle bay's down-tube grows a barren moon surface (MoonSurface), you carry the chest
// to the dig site, press [E], and — if the 2D6 Reevers turned out — run back up the tube to the
// crew-only door they cannot cross. Reevers never touch the loot; they are the free watchdogs.
public partial class Map
{
    // How fast a Reever closes on foot (deck units/real-second) — a hair under the digger's AvatarSpeed
    // (9.0) so a clean, early sprint escapes, but dawdling at the dig site gets you caught.
    private const double ReeverSpeed = 7.2;

    // The live excursion, or null when we're not on a surface. Positions of the chasing Reevers are
    // client-only real-time state (never saved — same law as any NPC position).
    private SurfaceExcursion? _surface;
    private readonly List<(double X, double Y, double Facing)> _reevers = [];
    private double _lastReeverCatchMs;

    private sealed class SurfaceExcursion
    {
        public required ShuttleStop Stop { get; init; }
        public required bool IsDig { get; init; }           // false = bury a new chest
        public required string? RestoreHavenId { get; init; } // the berth to re-dock at on liftoff
        public int PendingCoin { get; init; }
        public List<CacheCargo> PendingCargo { get; init; } = [];
        public bool Done { get; set; }                       // the bury/dig has happened
        public TreasureCache? Cache { get; set; }
        public ReeverRoll Roll { get; set; }
        public int Catches { get; set; }
    }

    // ── Landing: the shuttle IS the door. Fly down, weld the surface below the bay, step off. ──

    // Fly the shuttle down to bury the chest the chooser packed. The loot stays on the ship's books
    // until it actually goes into the ground at the dig site (so aborting the trip loses nothing).
    private void LandToBury(ShuttleStop stop)
    {
        if (_ephemeris is null)
        {
            return;
        }
        int coin = Math.Clamp(_buryCoin, 0, _credits);
        var cargo = _cargoByClass
            .Where(kv => kv.Value > 0)
            .Select(kv => new CacheCargo(kv.Key, kv.Value, IsHotClass(kv.Key)))
            .ToList();
        if (coin <= 0 && cargo.Count == 0)
        {
            _buryTarget = null;
            return;
        }

        _buryTarget = null;
        BeginSurface(new SurfaceExcursion
        {
            Stop = stop,
            IsDig = false,
            RestoreHavenId = _dockedHavenId,
            PendingCoin = coin,
            PendingCargo = cargo,
        }, "⛏ BURY THE CHEST");
        ShowPulseMessage($"🚀 Shuttle down on {stop.Body.Name}. Carry the chest out to the dig site and press [E] to bury it.");
    }

    // Fly the shuttle down to dig at the X — retrieval is a walk too, and it re-rolls the same watchdogs.
    private void LandToDig(ShuttleStop stop)
    {
        if (_ephemeris is null || !_caches.HasCacheAt(stop.Body.Id))
        {
            return;
        }
        BeginSurface(new SurfaceExcursion
        {
            Stop = stop,
            IsDig = true,
            RestoreHavenId = _dockedHavenId,
        }, "🗺 LIFT THE CHEST");
        ShowPulseMessage($"🚀 Shuttle down on {stop.Body.Name}. Walk to the X and press [E] — the map never lies.");
    }

    private void BeginSurface(SurfaceExcursion excursion, string digSiteLabel)
    {
        AdvanceShuttleClock(excursion.Stop.TravelSeconds); // the flight down costs the clock
        _surface = excursion;
        _reevers.Clear();
        _shuttleBayStops = null;

        _deckPlan = MoonSurface.SurfaceDeck(
            excursion.Stop.Body.Name, digSiteLabel, 3 + ReeverRaid.MaxReevers, FillSurfaceDroids);
        (_avatarX, _avatarY, _avatarHeading) = (MoonSurface.SpawnX, MoonSurface.SpawnY, -Math.PI / 2);
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;
        _deckPanX = _deckPanY = 0;
        RendererInterop.PlayCue("board");
    }

    // ── The dig site [E]: bury or lift, then the 2D6 Reevers decide whether it's a sprint. ──

    private void DigSiteInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (ex.Done)
        {
            ShowPulseMessage(ex.IsDig
                ? "The hole's empty — the chest is aboard. Get back to the shuttle."
                : "The chest is in the ground. Back to the shuttle before the Reevers wake.");
            return;
        }

        if (ex.IsDig)
        {
            LiftChestsHere(ex);
        }
        else
        {
            BuryChestHere(ex);
        }
    }

    private void BuryChestHere(SurfaceExcursion ex)
    {
        int coin = Math.Clamp(ex.PendingCoin, 0, _credits);

        // The chest goes off the ship's books — buried is invisible to confiscation by construction.
        _credits -= coin;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();

        // The 2D6 Reevers, seeded from this ground and this instant, with the stash's standing watchdog
        // presence riding on top. The presence LEFT on the chest is the pack that turns out (0..3).
        int standing = WatchdogLevelAt(ex.Stop.Body.Id);
        ReeverRoll roll = ReeverRaid.Roll(ReeverSeed(ex.Stop.Body.Id), standing);
        int presence = Math.Max(standing, roll.Reevers);

        TreasureCache cache = _caches.Bury(ex.Stop.Body.Id, coin, ex.PendingCargo, SimTime, "you", playerOwned: true, presence);
        SeedDiscoveryWatch();

        ex.Done = true;
        ex.Cache = cache;
        ex.Roll = roll;
        RequestVaultSave();
        ShowPulseMessage($"⛏ Chest buried — {cache.ContentsLine()} off the books.");
        RaiseReevers(roll);
    }

    private void LiftChestsHere(SurfaceExcursion ex)
    {
        var lifted = _caches.CachesAt(ex.Stop.Body.Id).ToList();
        int standing = WatchdogLevelAt(ex.Stop.Body.Id);
        int coinBack = 0, unitsBack = 0, unitsLost = 0;
        foreach (TreasureCache known in lifted)
        {
            if (_caches.Dig(known.Id) is not { } c)
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
                unitsLost += line.Units - take;
            }
            CompleteFetchCacheFor(c);
        }

        // Disturbing the ground re-rolls the SAME 2D6 watchdogs, hardened by the stash's presence.
        ReeverRoll roll = ReeverRaid.Roll(ReeverSeed(ex.Stop.Body.Id), standing);
        ex.Done = true;
        ex.Roll = roll;
        RequestVaultSave();
        string lost = unitsLost > 0 ? $" ({unitsLost}u left — hold full)" : "";
        ShowPulseMessage($"🗺 Dug up {coinBack:N0} cr + {unitsBack} units{lost}. X marked the spot.");
        PayCompletedQuests();
        RaiseReevers(roll);
    }

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

    // Seed the 2D6 from the place + the (integer-second) instant — deterministic, replayable in a test.
    private ulong ReeverSeed(string bodyId) => DiceRule.Seed($"reever:{bodyId}", (long)SimTime);

    private void RaiseReevers(ReeverRoll roll)
    {
        if (!roll.Roused)
        {
            ShowPulseMessage($"🎲 {roll.Describe()} — quiet ground. Stroll back to the shuttle.");
            return;
        }

        SpawnReevers(roll.Reevers);
        RendererInterop.PlayCue("alarm");
        string pack = roll.Reevers == 1 ? "A Reever rises" : $"{roll.Reevers} Reevers rise";
        ShowPulseMessage($"🎲 {roll.Describe()} — {pack} from the regolith! They don't want the loot — they want YOU. RUN for the shuttle!");
    }

    private void SpawnReevers(int count)
    {
        _reevers.Clear();
        // They come up out of the dark downrange of the dig site, between the digger and the deep
        // surface, so the only way clear is back up the tube.
        double y = MoonSurface.DigSiteY - 6;
        for (int i = 0; i < count; i++)
        {
            double x = -14 + i * 6.0;
            _reevers.Add((x, y, Math.PI / 2));
        }
    }

    // ── The sprint: the watchdogs close, but the crew-only door pens them at the tube mouth. ──

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
        for (int i = 0; i < _reevers.Count; i++)
        {
            (double x, double y, _) = _reevers[i];
            (double nx, double ny) = ReeverChase.Step(x, y, _avatarX, _avatarY, step, MoonSurface.ReeverBarrierY);
            double facing = Math.Atan2(_avatarY - ny, _avatarX - nx);
            _reevers[i] = (nx, ny, facing);
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

    // A Reever lays hands on you: it takes no loot (that is the whole point) — it prices the danger in
    // heat, through the same lever the law's collectors use. Debounced so one brush isn't a stunlock.
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
        ShowPulseMessage("🩸 A Reever's on you — it wants blood, not the chest. Tear free and RUN!");
        RequestVaultSave();
    }

    // ── Liftoff: board the shuttle, re-dock, and (on a fresh bury) show the treasure map. ──

    private void LiftOffFromSurface()
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // The trip home. If we buried, the map is filed now (the celebration card); an unfinished trip
        // just carries the loot back aboard, uncommitted.
        bool escapedWithWatchdogs = _reevers.Count > 0;
        _surface = null;
        _reevers.Clear();

        SetDeckForDock(ex.RestoreHavenId); // rebuild the ship/complex
        // Step off the shuttle back into the bay (the surface Y is off the ship plan, so place us).
        (_avatarX, _avatarY, _avatarHeading) = (-6, -6.5, Math.PI / 2);
        RendererInterop.PlayCue("board");

        if (!ex.Done)
        {
            ShowPulseMessage("🚀 Back aboard — nothing buried this trip. The chest is still in the hold.");
            return;
        }

        if (ex.IsDig)
        {
            string tail = escapedWithWatchdogs ? " You outran the watchdogs." : "";
            ShowPulseMessage($"🚀 Lifted off {ex.Stop.Body.Name} with the haul.{tail}");
        }
        else if (ex.Cache is { } cache)
        {
            _treasureMapCard = cache;
            RendererInterop.PlayCue("reveal");
            string tail = escapedWithWatchdogs
                ? $" {cache.ReeverLevel} Reever(s) haunt this ground now — the best kind of lock."
                : "";
            ShowPulseMessage($"🚀 Lifted off {ex.Stop.Body.Name}. Map filed (🗺).{tail}");
        }
    }

    // ── The droid buffer: the ship's crew up in the bay, plus the live Reevers on the surface. ──

    private void FillSurfaceDroids(double simTime, DeckPlan.Droid[] buffer)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // [0..3): K-77, R-3B, the corridor patroller
        for (int i = 0; i < ReeverRaid.MaxReevers; i++)
        {
            int slot = 3 + i;
            if (i < _reevers.Count)
            {
                (double x, double y, double facing) = _reevers[i];
                buffer[slot] = new DeckPlan.Droid(x, y, facing, "Reever");
            }
            else
            {
                buffer[slot] = new DeckPlan.Droid(-9999, -9999, 0, "Reever"); // parked off-frame
            }
        }
    }
}
