using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #251 · split from
// Map.Surface.Dig.cs, moved verbatim: #455's chest you ran away from, the probe, lifting a chest, dropping
// and recovering it, and the 2D6 Old Ones it raises. The dig channel, the bury and every field stay in Map.Surface.Dig.cs.
public partial class Map
{
    // ── #455 rule 2 · A CHEST YOU RAN AWAY FROM IS STILL A CHEST ──────────────────────────────────────
    //
    // Owner: "buried beats dropped, BY A LOT … on a return trip the buried one should usually still be
    // there; the dropped one gets a harder roll — it is lying in the open where anyone can see it."
    //
    // Before this, a dropped chest was not a worse cache, it was not a cache at all: the pile was excursion-
    // scoped and evaporated at liftoff with the coin and cargo still on the ship's books (the story-QA audit
    // on this issue called it "a free sprint", and #648 at least made the liftoff line say so out loud).
    // That left the whole "dropped" half of the safety oracle unreachable in play — a rule with no world
    // behind it. So a chest left on the regolith now goes into the ledger EXACTLY as a bury does, with the
    // same deductions and the same map card, and one difference: it is flagged as lying in the open, which
    // is what buys it the harder roll for as long as it stays there.
    //
    // Returns the minted cache, or null when there was nothing left behind (or nothing in the chest).
    private TreasureCache? LeaveTheDroppedChestInTheOpen(SurfaceExcursion ex)
    {
        if (!ex.ChestDropped || (ex.PendingCoin <= 0 && ex.PendingCargo.Count == 0))
        {
            return null;
        }

        int coin = Math.Clamp(ex.PendingCoin, 0, _credits);
        _credits -= coin;

        // The same law a bury obeys (ShuttleExcursion.HoldAfterBurying): only what is IN THE CHEST leaves
        // the books — the hold went on living all the way down and the rest of it comes home.
        var left = ShuttleExcursion.HoldAfterBurying(_cargoByClass, ex.PendingCargo);
        _cargoByClass.Clear();
        foreach (KeyValuePair<string, int> line in left)
        {
            _cargoByClass[line.Key] = line.Value;
        }
        RecomputeCargoTotals();

        TreasureCache cache = _caches.Bury(
            ex.Stop.Body.Id, coin, ex.PendingCargo, SimTime, "you", playerOwned: true,
            reeverLevel: WatchdogLevelAt(ex.Stop.Body.Id),
            digX: ex.DropX, digY: ex.DropY, siteIndex: ex.Site.Index,
            // The two terms that make this the harder roll: no shovel went in, and no carry is credited —
            // the chest was not placed anywhere, it fell where the captain's legs gave out.
            buried: false, padDistance: CacheSafety.PadDistanceOf(ex.DropX, ex.DropY));
        SeedDiscoveryWatch();
        RequestVaultSave();
        return cache;
    }

    // The beach-comber probe resolves (the fishing expedition's payoff, or its honest shrug). The D100
    // already ruled out bedrock at BeginDig, so this hole turned up either nothing (the common case,
    // "unlucky … but still possible") or a rare shallow find — a little coin and maybe a scrap. Modest by
    // design: luck, never an economy. Either way the square joins the per-visit swept grid.
    private void ProbeHere(SurfaceExcursion ex, int squareX, int squareY)
    {
        Probe probe = BeachComber.Roll(ex.Stop.Body.Id, squareX, squareY);
        ex.Swept[(squareX, squareY)] = probe.Outcome;

        // #1202 · …and on a stringer's ground, where the ground is soft, this may be the hole her source left.
        if (TheTinComesUp(ex, squareX, squareY))
        {
            return;
        }

        // #411: a rare seeded square on an outer icy moon hides a cold KAAMOS supply pod — a cargo run that
        // never arrived, distinct from ordinary treasure. Sweeping it the first time assembles cold-pod (and
        // may open the reach). Once held, the square is ordinary regolith and the normal probe result stands.
        if (!_kaamos.Has("cold-pod") && KaamosPodHere(ex.Stop.Body.Id, squareX, squareY))
        {
            TryAssembleKaamos("cold-pod",
                "❄ Your probe rings off metal a foot down — not a coin, a HULL. You clear the frost and it's a " +
                "SEALED SUPPLY POD, decades cold. " + KaamosLore.ById("cold-pod")!.Lore);
            return;
        }

        // #409: a near-miss on a hidden lab door — the detector shrieks that something big and metal is very
        // close, keep sweeping the squares around here (tacked onto the honest probe result).
        string labTail = SecretLabProximityTail(ex, squareX, squareY);

        if (!probe.IsFind)
        {
            RendererInterop.PlayCue(labTail.Length > 0 ? "reveal" : "board");
            ShowPulseMessage("🕳 Nothing but regolith down there. The detector stays quiet — you mark the square and move on." + labTail);
            return;
        }

        // A shallow find: pocket the coin, and take the scrap if the hold has room (else leave it — a
        // scrap's not worth a sprint). Small numbers on purpose.
        _credits += probe.FindCoin;
        int scrapTaken = 0;
        if (probe.FindScrapUnits > 0 && _cargoUnits < CargoCapacity)
        {
            int take = Math.Min(probe.FindScrapUnits, CargoCapacity - _cargoUnits);
            _cargoUnits += take;
            _cargoValue += take * CargoMarket.UnitValue(BeachComber.FindCargoClass);
            _cargoByClass[BeachComber.FindCargoClass] = _cargoByClass.GetValueOrDefault(BeachComber.FindCargoClass) + take;
            scrapTaken = take;
        }
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        string scrapTail = scrapTaken > 0 ? $" + {scrapTaken} scrap of salvage" : "";
        ShowPulseMessage($"✨ The detector chirps — you turn up {probe.FindCoin:N0} cr{scrapTail} a few inches down. Luck, not a fortune. Mark it and keep moving." + labTail);
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
        // #319 · …and whatever the captain buried out of his own coat goes back into it, through the satchel's
        // own CanTake. The ONE recovery, on the ONE dig: there is no second verb and no second ✗ — the issue's
        // "recoverable by the same dig at the ✗", to the letter.
        string coat = TheCoatTakesBackWhatItCan(ex, c);
        CompleteFetchCacheFor(c);
        _ = roll; // the pack already turned out at channel start
        RebuildSurfaceDeck(); // the ✗ is gone
        RequestVaultSave();
        string lost = unitsLost > 0 ? $" ({unitsLost}u left — hold full)" : "";
        ShowPulseMessage($"🗺 Dug up {c.Coin:N0} cr + {unitsBack} units{lost}.{coat} Back to the shuttle.");
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
        // #456: a chest hitting regolith is one sharp report. You dropped it to run — and the sound tells
        // anything close where you just were, which is exactly the cost of that trade.
        MakeNoise(_avatarX, _avatarY, ReeverHearing.Noise.Clatter);
        if (ex.Channel is not null)
        {
            ex.Channel = null;
        }
        RebuildSurfaceDeck();
        RendererInterop.PlayCue("alarm");
        // #455 rule 3 · TELL HIM AT COMMIT TIME. A drop is a real hiding place now — lift off without it and
        // it stays in the ground as an OPEN cache (Map.Surface, the liftoff seam) — so the same oracle that
        // prices a bury prices this, read here for the chest as it would be LEFT: in the open, on this
        // ground. He is deciding whether to come back for it, and this is the number that decides it.
        // #316 law 3 · …priced against the fight this ground is already carrying, exactly as the bury line
        // is. A chest abandoned in the middle of a firefight is the loudest hiding place in the game.
        CacheSafetyRead read = CacheSafety.Read(
            CacheSafety.PadDistanceOf(_avatarX, _avatarY), buried: false, WatchdogLevelAt(ex.Stop.Body.Id),
            TheFightThisGroundCarries(ex));
        ShowPulseMessage($"🪤 Chest dropped! {read.Sentence} Full sprint now — come back for it when the ground's clear.");
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
        double baseY = Math.Min(_avatarY - 4, MoonSurface.AnchorY + 10);
        for (int i = 0; i < count; i++)
        {
            if (_reevers.Count >= ReeverEngineCeiling)
            {
                break;
            }
            double frac = count > 1 ? i / (double)(count - 1) : 0.5;
            double x = -40 + frac * 70 + (i % 2 == 0 ? -3 : 3);
            double y = baseY - (i % 3) * 4;
            _reevers.Add(new Reever
            {
                X = x, Y = Math.Min(y, MoonSurface.ReeverBarrierY - 1), Facing = Math.PI / 2,
                // Seed the thermal shuffle off the excursion threat seed + the spawn ordinal so each pack
                // member shivers on its own phase (client-only, like the position itself — never saved).
                JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)i + 1UL,

                // #459 (owner, live 2026-07-27: "I did not see any reevers last time… were there any?" —
                // "Not having any is major bug"). THIS pack is roused BY the shovel: the line the player is
                // reading as they spawn literally says they "shamble up from the regolith, CONVERGING".
                // After #446 they were born unaware, so they converged on nothing — they stood where they
                // rose, and standing still they are invisible to a motion-only tracker too. The whole
                // dig-under-threat loop silently became an empty field.
                //
                // They know the DIG, not the captain: LastSeen is the hole, exactly as #456's ear hands out
                // a PLACE rather than a target. Walk away from the noise you made and they still arrive at
                // it. #446's unaware feature is untouched — it governs the Old Ones already standing on the
                // ground when you get there (the tide's), which is the case the owner described.
                EverSeen = true,
                LastSeenX = _avatarX,
                LastSeenY = _avatarY,
            });
        }
    }
}
