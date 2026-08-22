using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the kiosk, the droid buffer, the motion tracker and the surface HUD.
public partial class Map
{
    // ── The lonely automated kiosk (#313 amenity): a PLACE has shops. Pulse receipts (#119 style),
    //    house voice — last restocked before the war. ──

    // Slot 0 is the souvenir tee — its item + gag are filled from the moon underfoot at buy time
    // (SurfaceSouvenir), so Ganymede sells a Ganymede shirt, not Miranda's (#379). The placeholder
    // strings below are never shown; they only hold slot 0's price and mark the seam.
    // Owner, 2026-07-28: "The T-shirts etc everywhere where they are missing." Every HAVEN gift shop has
    // painted a tee AND a magnet since #367; the GROUND kiosk sold both and showed neither — a pulse line
    // and nothing to look at. The art column closes that: what you bought now gets held up.
    private static readonly (string Item, int Price, string Line, string Art)[] KioskStock =
    [
        ("the local souvenir tee", 15, "(keyed to the walked body — see VisitKiosk)",
            "art/souvenir-surface-tshirt.jpg"),
        ("a fridge magnet", 8, "It clamps to your suit's chestplate and refuses to let go. Value: eternal.",
            "art/souvenir-surface-magnet.jpg"),
        ("a vacuum-sealed hot meal", 12, "The label promises 'MEAT-ADJACENT'. The heater still works. Mostly.",
            ""), // no art — it is a ration pouch, and the joke is funnier unseen
    ];

    private int _kioskPicks;

    /// <summary>What the kiosk just sold you, held up for a look — the ground's answer to the haven gift
    /// shops' view-object cards. Null when nothing is being inspected.</summary>
    private readonly record struct KioskBuy(string Item, string Line, string Art);

    private KioskBuy? _kioskCard;

    private void CloseKioskCard() => _kioskCard = null;

    private void VisitKiosk()
    {
        if (_surface is not { } ex)
        {
            return; // the kiosk only sells on the ground it stands on
        }
        int slot = _kioskPicks % KioskStock.Length;
        (string item, int price, string line, string art) = KioskStock[slot];
        _kioskPicks++;
        if (slot == 0)
        {
            // The souvenir tee, keyed to the moon actually underfoot (#379): Ganymede's kiosk prints a
            // Ganymede shirt; Miranda keeps its canon line. Copy is generated, so any landable body works.
            CelestialBody body = ex.Stop.Body;
            item = SurfaceSouvenir.TeeItem(body.Name);
            line = SurfaceSouvenir.TeeGag(body.Id, body.Name);
        }
        if (_credits < price)
        {
            ShowPulseMessage($"🛒 {item} — {price} cr. The slot blinks INSUFFICIENT FUNDS in a dead language. Empty pockets, captain.");
            return;
        }
        _credits -= price;
        RendererInterop.PlayCue("board");
        ShowPulseMessage($"🧾 Bought {item} for {price} cr. {line} (The kiosk was last restocked before the war.)");
        if (art.Length > 0)
        {
            // Hold it up. The img onerror-hides, so an unpainted slot still degrades to the caption alone.
            _kioskCard = new KioskBuy(item, line, art);
        }
    }

    // ── The droid buffer: the ship's crew, plus the live Old Ones on the surface. ──

    private void FillSurfaceDroids(double simTime, DeckPlan.Droid[] buffer)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // [0..3): the crew
        // #633 · FOUR BANDS, NOT THREE, AND THEY MAY NOT OVERLAP. `main` put the sweep team at
        // `3 + ReeverEngineCeiling` because on that branch nothing else lived there; this branch had already
        // given those slots to the repo crew (#583). Reunited without this line the collectors' loop below
        // would overwrite all three sweepers every frame — one buffer written by two fillers, which is the
        // named bug class and exactly the thing a merge produces. The bands are stated ONCE, here and in
        // SurfaceDroidCount, and every filler is offset from the one before it.
        FillSweeperDroids(buffer, 3 + ReeverEngineCeiling + MaxCollectors);
        // #804 · …and the rounds, in the band after the sweepers. Their filler applies the sightline gate
        // itself: a guard the captain cannot see is parked off-map exactly as an unseen Old One is.
        FillPatrolDroids(buffer, PatrolFirstSlot);
        // #731 · …and the people who are leaving, or who have come out of a door to sit at your table. Their
        // own band after the rounds, drawn with the ordinary NPC pen because they are ordinary people.
        FillWalkerDroids(buffer, WalkerFirstSlot);
        for (int i = 0; i < ReeverEngineCeiling; i++)
        {
            int slot = 3 + i;
            // #371 Phase 3 (expedition fog): a behind-cover Old One is NOT drawn on the walked map — parked
            // off-screen exactly like an empty slot. VisibleOnMap is always true off an expedition site, so
            // Miranda and the moons draw every contact as before. The motion tracker (which reads _reevers
            // directly, not this buffer) still hears it through the wall — untouched.
            if (i < _reevers.Count && _reevers[i].VisibleOnMap)
            {
                Reever r = _reevers[i];
                buffer[slot] = new DeckPlan.Droid(r.X, r.Y, r.Facing, "Reever");
            }
            else
            {
                buffer[slot] = new DeckPlan.Droid(-9999, -9999, 0, "Reever");
            }
        }

        // #583 · And the repo crew, in their own slots after the Old Ones. Drawn as people, named so the
        // renderer can give them their own ink — they are not hostiles of the same kind and should not read
        // as more Reevers on the walked map.
        for (int i = 0; i < MaxCollectors; i++)
        {
            int slot = 3 + ReeverEngineCeiling + i;
            buffer[slot] = i < _collectors.Count
                ? new DeckPlan.Droid(_collectors[i].X, _collectors[i].Y, _collectors[i].Facing, "Collector")
                : new DeckPlan.Droid(-9999, -9999, 0, "Collector");
        }
    }

    // ── The motion tracker HUD (#313): a crude corner sweep of MOVING contacts, built for the renderer.
    //    Motion only — a wall-blocked, momentarily-still Old One drops off the fan. ──

    /// <summary>The fuzzy returns painted on the deck for contacts the fan hears through steel. Held here
    /// and refilled per frame, like every other HUD buffer.</summary>
    private readonly List<(double X, double Y, double Radius)> _hudSmudges = [];

    /// <summary>How wide a return is at point-blank, in deck units — already a REGION rather than a spot,
    /// because a crude fan never knew better than that.</summary>
    private const double SmudgeBaseRadius = 2.6;

    /// <summary>And how much wider per unit of range: the further off the contact, the vaguer the ear.</summary>
    private const double SmudgeRangeSpread = 0.12;

    /// <summary>Where the fan last heard each contact, and when — the raw material for the ghosts.</summary>
    private readonly Dictionary<Reever, (double X, double Y, double HeardAtMs)> _ghosts = [];

    /// <summary>The fading "movement was here" marks handed to the renderer.</summary>
    private readonly List<(double X, double Y, double Fade)> _hudGhosts = [];

    /// <summary>The nest's own motion, for the fan's benefit. It goes nowhere; it is never still. Anything
    /// above <see cref="MotionTracker.StillSpeed"/> reads as a live return, which is the truth about it.</summary>
    private const double NestChurn = 0.6;

    /// <summary>How wide the nest reads. Deliberately larger than any body smudge — the captain should be
    /// able to tell "something is in there" from "THAT is what is in there" at a glance.</summary>
    private const double NestSmudgeRadius = 4.2;

    /// <summary>Where the nest is, while it is still producing. Null once her room has been blown — a vented
    /// nest is off the tracker and off the map, and that silence is the reward for the soak.</summary>
    private (double X, double Y)? LiveNestPosition()
    {
        if (_wreck is not { Cause: Derelict.WreckCause.Infested })
        {
            return null;
        }
        if (!_ventSpaces.TryGetValue(WreckLayout.NestCompartment, out HullVenting.Space nest)
            || nest.Vented || !nest.Infested)
        {
            return null;
        }

        DeckReachability.Point at = WreckLayout.CauseStation(Derelict.WreckCause.Infested);
        return (at.X, at.Y);
    }

    /// <summary>How long a fresh return takes to settle from bright to its resting glow — the phosphor
    /// cooling, not the memory expiring. FLAGGED for tuning.</summary>
    private const double GhostSettleSeconds = 5.0;

    /// <summary>And the glow it never drops below. THE TRACKER REMEMBERS: a mark stays until the same
    /// contact is heard somewhere else. It is only wiped by better information, never by time.</summary>
    private const double GhostFloor = 0.45;

    /// <summary>How close a contact has to appear, with no warning, to land the ambush fright. Tight on
    /// purpose: this is "it was already in the room", not "I can see it down the corridor". FLAGGED.</summary>
    private const double AmbushRange = 7.0;

    /// <summary>
    /// #488 · THE PROWL — how a woken Old One that has not found you yet moves about a dead ship.
    ///
    /// <para>Deliberately NOT the regolith behaviour: out on the ground an unaware contact keeps its own
    /// deep and holds still, by the owner's own ruling, and that is untouched. Aboard, stillness would mean
    /// a motion tracker that never hears anything until the moment something is on top of you — which
    /// defeats the instrument the corridors were built around.</para>
    ///
    /// <para>Slow, aimless, and honest: it picks somewhere to be, walks there obeying the walls, and picks
    /// again. It is not searching for the captain — it does not know there is one. It is just awake.</para>
    /// </summary>
    private void Prowl(Reever r, IReadOnlyList<SurfaceCollision.Segment> walls, double radius,
                       double step, double now)
    {
        r.Idle = false;

        if (now >= r.ProwlUntilMs)
        {
            // Somewhere else on this deck, chosen off its own seed so each one wanders its own way and the
            // pack does not migrate as a blob.
            ulong pick = r.JitterSeed + (ulong)(now / ProwlLegMs);
            r.ProwlX = WreckLayout.AftX + 2 + ((pick % 53UL) / 53.0 * (WreckLayout.BowX - 8 - WreckLayout.AftX));
            r.ProwlY = ((pick / 53UL) % 3UL) switch { 0 => -6.0, 1 => 0.0, _ => 6.0 };
            r.ProwlUntilMs = now + ProwlLegMs;
        }

        double prowlStep = step * ProwlSpeedFraction;
        (double nx, double ny) = ReeverChase.Step(
            r.X, r.Y, r.ProwlX, r.ProwlY, prowlStep, double.PositiveInfinity, walls, radius,
            (r.JitterSeed & 1) == 0 ? 1 : -1);

        // Real velocity, because that is the entire point: the fan hears MOTION.
        r.Vx = prowlStep > 0 ? (nx - r.X) / (step / ReeverSpeed) : 0;
        r.Vy = prowlStep > 0 ? (ny - r.Y) / (step / ReeverSpeed) : 0;
        r.X = nx;
        r.Y = ny;
        r.Facing = System.Math.Atan2(r.ProwlY - ny, r.ProwlX - nx);

        // Wedged against something, or arrived: take a new bearing next frame rather than grinding.
        if (System.Math.Abs(r.Vx) + System.Math.Abs(r.Vy) < 0.01)
        {
            r.ProwlUntilMs = 0;
        }
    }

    /// <summary>How long a prowler holds one bearing before picking another.</summary>
    private const double ProwlLegMs = 7_000;

    /// <summary>A prowl is a wander, not a hunt — well under the chase so a contact that has actually SEEN
    /// you is unmistakably faster. FLAGGED for tuning.</summary>
    private const double ProwlSpeedFraction = 0.42;

    /// <summary>Gather the deployed sentries for the renderer. Pulled out of the full HUD build so the
    /// WRECK path can have them too: a bot on a steel deck is drawn exactly like a bot on regolith, and it
    /// was only ever invisible aboard because the whole hud was suppressed to get rid of the tracker.</summary>
    private void RefreshHudBots(SurfaceExcursion ex)
    {
        double nowMs = _lastTimestampMs ?? 0;
        _hudBots.Clear();
        foreach (SurfaceBot b in ex.Bots)
        {
            if (!b.Deployed)
            {
                continue;
            }
            _hudBots.Add((b.X, b.Y, SentryBot.Readout(b.Rounds), b.Rounds <= 0, b.FiringUntilMs > nowMs, b.AimX, b.AimY));
        }
    }

    private DeckView.SurfaceHud? BuildSurfaceHud()
    {
        if (_surface is not { } ex)
        {
            return null;
        }

        // #488: a DERELICT wears none of the regolith's INSTRUMENTS. The motion tracker sweeps for Old Ones
        // clawing out of ground that is not there; the key hints offer to DIG on a steel deck; the tracker
        // caption talks about movement in the deep. Boarded live, all three printed over the wreck's own
        // compartment labels and made her read like a moon with walls. She is a ship: the away team reads
        // her, they do not sweep her.
        //
        // THAT WAS DONE BY RETURNING NULL, AND IT TOOK THE SENTRIES WITH IT. Deployed bots are drawn from
        // this HUD, so aboard a wreck a bot went down, held its arc, pinned Old Ones — and was invisible.
        // (Owner, mid-playtest: "I tried to deploy K99 but the map does not show anything there.") A bot
        // holding a corridor while the pump runs is the loop this lane is FOR, so the wreck now gets a
        // REDUCED hud rather than none: the marks that belong on a deck, and none of the regolith's
        // instruments.
        bool onWreck = Derelict.TryParseWreckId(ex.Stop.Body.Id, out _);
        if (onWreck)
        {
            RefreshHudBots(ex);

            // THE TRACKER COMES UP WHEN THERE IS SOMETHING TO TRACK, AND THAT IS THE POINT. Owner: "we
            // could really use the motion detector here … I think we need it activating to bring it up on
            // hud — that could be the first sign we found something."
            //
            // Better than always-on, and better than my #488 call to remove it outright (which was only
            // defensible while the pack aboard was invisible, mislocated and topped up by a regolith tide).
            // On a hull you have been told is dead, the INSTRUMENT APPEARING is the beat: no caption, no
            // announcement, just a fan that was not on the screen a second ago. Once it has seen anything
            // it stays live for the rest of the boarding — an ear does not un-hear.
            _hudEntities.Clear();
            _hudEntities.AddRange(EverythingThatMoves());   // #538: the pack AND the sweep team

            // #583: a repo crew that boarded a wreck behind you is a contact like any other.
            foreach (Collector c in _collectors)
            {
                _hudEntities.Add(new MotionTracker.Entity(c.X, c.Y, c.Vx, c.Vy));
            }

            // THE NEST IS THE LOUDEST THING ABOARD. Owner: "the nest should show in the map and as movement
            // both." It never walks anywhere, so a fan that only reports travel would call it silence — but
            // a nest is not a still contact, it is a mass of small motion that never stops. So it goes on
            // the tracker with a motion of its own: a return that is always there, always in the same place,
            // and (below) far broader than a body. Once the captain has heard it they know where the ship's
            // supply is without being told, and cutting it becomes a place they can walk to.
            (double X, double Y)? nestAt = LiveNestPosition();
            if (nestAt is { } nx)
            {
                _hudEntities.Add(new MotionTracker.Entity(nx.X, nx.Y, NestChurn, 0));
            }
            IReadOnlyList<MotionTracker.Blip> aboardBlips = MotionTracker.Sweep(_avatarX, _avatarY, _hudEntities);
            // #830 · "Closing" is a fact about something TRAVELLING, so it is asked of the nearest MOVER
            // rather than of the nearest return: a blob is not going anywhere, and the honest answer to
            // whether it is closing is that the question does not apply.
            double? aboardNearest = MotionTracker.NearestMoving(aboardBlips);
            bool aboardClosing = aboardNearest is { } an && _lastNearestReeverRange is { } prevAboard
                                 && an < prevAboard - 0.01;
            _lastNearestReeverRange = aboardNearest;

            _hudBlips.Clear();
            foreach (MotionTracker.Blip b in aboardBlips)
            {
                _hudBlips.Add((b.Bearing, b.Range, b.Kind == MotionTracker.BlipKind.Blob));
            }
            _wreckTrackerLive |= aboardBlips.Count > 0;

            // A SMUDGE FOR EVERY CONTACT THE FAN HEARS AND THE CAPTAIN CANNOT SEE. Placed off the blip's
            // OWN bearing and range — the fan's actual output — rather than off the contact's true
            // position, and blurred by a radius that grows with range, because a crude fan is less sure
            // about a far return. What the captain gets is a region, which is exactly what they were told.
            _hudSmudges.Clear();
            foreach (Reever r in _reevers)
            {
                if (r.VisibleOnMap)
                {
                    // Your own eyes are better than the fan, so what you SEE also updates what the tracker
                    // remembers. Look away and the mark it leaves behind is where you last actually saw it.
                    _ghosts[r] = (r.X, r.Y, _lastTimestampMs ?? 0);
                    continue;
                }
                if (r.Dormant)
                {
                    continue;   // hibernating: nothing to hear, and nothing was ever heard
                }
                if (Math.Sqrt(((r.Vx * r.Vx) + (r.Vy * r.Vy))) < MotionTracker.StillSpeed)
                {
                    continue;   // a motion fan hears MOTION; a contact holding still is not a return
                }
                double dx = r.X - _avatarX, dy = r.Y - _avatarY;
                double range = Math.Sqrt((dx * dx) + (dy * dy));
                _hudSmudges.Add((r.X, r.Y, SmudgeBaseRadius + (range * SmudgeRangeSpread)));
                _ghosts[r] = (r.X, r.Y, _lastTimestampMs ?? 0);
            }

            // And on the map as a smear the size of the thing itself — not a contact the captain is meant to
            // shoot, a REGION they are meant to recognise. It is the one return that never moves and never
            // stops, which is how you tell it from the pack the moment you see it.
            if (nestAt is { } nm)
            {
                _hudSmudges.Add((nm.X, nm.Y, NestSmudgeRadius));
            }

            // THE GHOST OF WHERE IT WAS. Owner: "let's have the map show like a ghost of where movement was
            // last seen." A return that stops — because the contact went still, or slipped behind a hatch —
            // does not simply vanish, because the captain's knowledge does not. The mark stays where the
            // fan last had it and fades out over a few seconds, which is exactly as long as that knowledge
            // is worth anything. What it never does is follow: a ghost is a memory of a PLACE.
            // PHOSPHOR PERSISTENCE — the Aliens tracker, and the owner's own rule for it: "it was there it
            // last moved … it is probably still there until it moves away, when we will detect it again.
            // Better to have a couple of ghost detections than miss a reever."
            //
            // So a ghost NEVER expires. It burns bright where the return came in, decays to a floor, and
            // then sits there being the best information anyone has. If the contact moves again the mark
            // moves with it; if it went still, the mark is telling the truth — a thing that stopped is
            // still there. And if it slipped away without ever being heard again, the mark is a LIE the
            // captain can walk into, which is the price of an instrument that would rather be wrong than
            // quiet.
            _hudGhosts.Clear();
            double nowGhost = _lastTimestampMs ?? 0;
            foreach ((Reever ghosted, (double gx, double gy, double heardAt)) in _ghosts)
            {
                if (ghosted.VisibleOnMap)
                {
                    continue;   // your own eyes are on it — the memory is not needed
                }
                double age = (nowGhost - heardAt) / 1000.0;
                double fade = Math.Max(GhostFloor, 1.0 - (age / GhostSettleSeconds));
                _hudGhosts.Add((gx, gy, fade));
            }

            return new DeckView.SurfaceHud(
                TrackerCaptions: null,
                DigProgress: ex.DoorChannel?.Progress ?? -1,   // a forced door is a ship thing; digging is not
                HasDroppedChest: false, DropX: 0, DropY: 0,
                Blips: _hudBlips,
                // #830 law 4 · The pulse and the sentence read the SWEEP, not a number somebody kept beside
                // it — "no movement — for now" may only print when the fan is holding nothing of either kind.
                Cadence: (int)MotionTracker.CadenceOf(aboardBlips),
                Readout: MotionTracker.ReadoutOf(aboardBlips, aboardClosing),
                CacheMarks: [],                                // nothing is buried on a steel deck
                Nerve: _nerve,
                NerveReadout: NerveModel.Readout(_nerve),
                Bots: _hudBots,                                // ← the fix
                Husks: _hudHusks,
                KeyHints: BuildSurfaceKeyHints(ex),            // names [T] aboard, never DIG
                Countdown: _scuttleSecondsLeft is { } burning
                    ? (WreckLayout.ScuttleStation.X, WreckLayout.ScuttleStation.Y,
                       HullVenting.SoakLabel(burning))
                    : null,
                Instruments: _wreckTrackerLive,                // it appears when something moves. That IS the warning.
                Smudges: _hudSmudges,                          // heard through steel: a region, never a body
                Ghosts: _hudGhosts,                            // and where it was, fading
                BloodSplash: BloodShowing
                    ? Math.Clamp((_bloodUntilMs - (_lastTimestampMs ?? 0)) / 900.0, 0, 1)
                    : 0);
        }
        // #371 Phase 1 (perf): fill the reused entity buffer instead of a lazy Select — one iterator fewer
        // per frame, and MotionTracker.Sweep reads it as an IEnumerable exactly as before.
        _hudEntities.Clear();
        // #538 / #583 · The pack, the sweep team AND the repo crew — everything on this ground that is on
        // its feet, from the one accessor that lists them.
        _hudEntities.AddRange(EverythingThatMoves());

        // #591 · The sweep is cut to what the fan can hear from this floor. On the regolith that is
        // unbounded and nothing changes; eleven floors down it is the reason the corridor is quiet.
        double fanReach = FanReach();
        IReadOnlyList<MotionTracker.Blip> blips =
            MotionTracker.Sweep(_avatarX, _avatarY, _hudEntities, fanReach);
        // #830 · The nearest MOVER is what "closing" can be asked about; the nearest RETURN of either kind
        // is what the pulse and the sentence are about. Both come off this one sweep.
        double? nearest = MotionTracker.NearestMoving(blips);
        bool closing = nearest is { } n && _lastNearestReeverRange is { } prev && n < prev - 0.01;
        _lastNearestReeverRange = nearest;

        _hudBlips.Clear();
        foreach (MotionTracker.Blip b in blips)
        {
            _hudBlips.Add((b.Bearing, b.Range, b.Kind == MotionTracker.BlipKind.Blob));
        }

        // ── #591 · A CONTACT BEHIND A WALL IS A SMUDGE, NOT A CLEAN BLIP ──
        //
        // Open regolith is open: a return out there is a return, and the fan's report is as good as it gets.
        // Inside a poured facility it is not — a fan that reads a body through two bulkheads with the same
        // confidence it reads one down an open corridor is claiming a precision it does not have.
        //
        // No new mechanism: this is the same fog #371 built for wrecks (SightBlockers → a blurred region
        // whose radius grows with range, because a crude fan is less sure about a far return), pointed
        // underground. The buffer is cleared unconditionally so a floor with nothing on it cannot inherit
        // the smears of the last derelict the captain walked.
        //
        // #804 · AND NOW SOMETHING DOES WALK THEM. The Old Ones are still a regolith tide and are still
        // cleared on descent (owner: "I don't think there should be reevers down here") — what is down here
        // is a ROUND, on a payroll, and it arrived without this seam changing by a line. That is exactly
        // what #591 was betting on: make the instrument honest first, and whatever eventually comes down
        // here inherits a tracker that already behaves like it is underground. The fan hears a guard
        // through poured wall at the degraded reach, draws them as a smear rather than a dot, and does it
        // before the eye has anything to draw at all — which is the whole of the owner's "the motion
        // detector warns us before they spot us".
        _hudSmudges.Clear();
        if (ex.Floor < 0)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = SightBlockers();
            foreach (MotionTracker.Blip b in blips)
            {
                double bx = _avatarX + (Math.Cos(b.Bearing) * b.Range);
                double by = _avatarY + (Math.Sin(b.Bearing) * b.Range);
                if (SurfaceCollision.HasLineOfSight(_avatarX, _avatarY, bx, by, walls))
                {
                    continue;   // you can see it. Your own eyes beat the fan, exactly as they do aboard.
                }
                // #830 · An unsure return smears wider on the plan than a mover does, off Core's own number,
                // because the deck plan and the fan are two drawings of ONE claim and may not disagree about
                // how vague it is.
                _hudSmudges.Add((bx, by, b.Kind == MotionTracker.BlipKind.Blob
                    ? MotionTracker.BlobSpreadDu(b.Range)
                    : SmudgeBaseRadius + (b.Range * SmudgeRangeSpread)));
            }
        }

        // The own caches' ✗ marks (with the DigX/DigY-or-hash-scatter fallback, same as OwnCachePositionsAt)
        // straight into the reused buffer — no intermediate list + Select allocation.
        string bodyId = ex.Stop.Body.Id;
        _hudMarks.Clear();

        // #591 · EVERYTHING BURIED IS BURIED ON THE SURFACE. A floor of the Hive reuses the surface's own
        // coordinate envelope (#585), which is what makes depth free — and it also means a cache buried at
        // (x, y) on the regolith has an (x, y) on B3 that is several hundred metres of rock away and belongs
        // to somebody else's corridor. Drawn unguarded, the captain's own treasure ✗ appears ON the facility
        // deck, and its beacon on the fan points at it.
        //
        // Same reasoning as the beacons above: these are surface instruments reporting surface facts, and
        // underground they are not merely useless but WRONG. Gated once, here, because _hudMarks feeds both
        // the on-grid marks and BuildCacheBeacons — one source, one gate.
        // 🗺 Layers (#405) Ground finds → Treasure ✗: the buried-cache marks the excursion HUD carries.
        foreach (TreasureCache c in LayerVisible("finds.treasure") && ex.Floor >= 0 ? _caches.CachesAt(bodyId) : [])
        {
            if (!c.PlayerOwned)
            {
                continue;
            }
            (double mx, double my) = c is { DigX: { } dx, DigY: { } dy }
                ? (dx, dy)
                : MoonSurface.CachePosition(c.Id);
            _hudMarks.Add((mx, my, c.ReeverLevel > 0));
        }

        RefreshHudBots(ex);

        _hudHusks.Clear();
        // 🗺 Layers (#405) Ground finds → Husks: the downed-Old-One marks left in the regolith (#316).
        if (LayerVisible("finds.husks"))
        {
            foreach ((double hx, double hy) in ex.Husks)
            {
                _hudHusks.Add((hx, hy));
            }
        }

        // The per-visit swept grid: every beach-comber square probed this excursion, at its centre, with a
        // hard-ground flag so the deck-plan paints a bedrock mark distinct from a plain checked square. The
        // draw is BOUNDED (MaxSweptDrawn) so a fully-probed field can't paint an unbounded mark cloud.
        _hudSwept.Clear();
        // #591 · Also a surface fact: a probed regolith square has nothing to say about a poured floor
        // hundreds of metres under it.
        foreach (KeyValuePair<(int X, int Y), BeachComber.Outcome> kv in ex.Floor < 0 ? [] : ex.Swept)
        {
            if (_hudSwept.Count >= MaxSweptDrawn)
            {
                break;
            }
            (double cx, double cy) = BeachComber.SquareCenter(kv.Key.X, kv.Key.Y);
            _hudSwept.Add((cx, cy, kv.Value == BeachComber.Outcome.TooHard));
        }

        // #327 the ship calling home, now behind the COMMS-LOSS display gate: SurfaceComms wraps the honest
        // feed with the live downlink phase, so a degraded/blacked-out link freezes the orbit line at
        // last-known (banner + CommsState for the renderer's static). The true state is never touched.
        (string Line, int Severity, int CommsState)? orbit = SurfaceComms();

        return new DeckView.SurfaceHud(
            TrackerCaptions: BuildTrackerCaptions(ex, _hudMarks.Count),
            // #371 Phase 3 / #562 / #696: the one progress bar serves every slow thing — a dig, a forced
            // door, a document being photographed, or the tube racking a magazine. The rearm is last because
            // it is the only one that can be running while the captain is somewhere the others cannot happen
            // (inside the tube), so it can never actually contend; ordering it here keeps the hands-on
            // channels reading first.
            DigProgress: ex.Channel?.Progress ?? ex.DoorChannel?.Progress
                ?? (ex.Processing is { } paper
                    ? Core.Processing.Fraction(paper.Elapsed, ProcessingSeconds)
                    : ex.RearmBotIndex is not null ? ex.RearmProgress : -1),
            // #562: and it says which. A shovel over a magazine being racked would be exactly the class of
            // lie this lane exists to fix; the rearm is the ship HELPING you, so it reads cold-green.
            ChannelGlyph: SurfaceChannelGlyph(ex),
            ChannelIsAid: SurfaceChannelIsAid(ex),
            HasDroppedChest: ex.ChestDropped, DropX: ex.DropX, DropY: ex.DropY,
            Blips: _hudBlips,
            // #830 law 4 · The sentence and the sweep read the same list. This is the line that told a
            // captain a corridor was empty while a man stood in it — not because the arithmetic was wrong,
            // but because it was answering a narrower question than the fan was drawing.
            Cadence: (int)MotionTracker.CadenceOf(blips),
            Readout: MotionTracker.ReadoutOf(blips, closing),
            CacheMarks: _hudMarks,
            Nerve: _nerve,
            NerveReadout: NerveModel.Readout(_nerve),
            // #573: the places worth walking to, as calm rings on the fan — plus your own caches once they
            // are in reach, and any rumour you are working from as a wide soft wash.
            Beacons: BuildBeacons(ex),
            CacheBeacons: BuildCacheBeacons(),
            Rumours: BuildRumours(ex),
            // #564: the tank, drawn as a bar under the tracker.
            AirSeconds: ex.AirSeconds,
            // #612 · Is the tank actually running? The gauge said nothing either way until the owner asked
            // "where here does it say if I consume tanks or have air?" — and it now asks ONE function
            // (#608), the same one the drain itself is gated on.

            AirDistanceHome: DistanceToTheTube(),
            // #612 · AND WHERE IT IS COMING FROM — the sim's own answer, handed down rather than worked out
            // again in the renderer. Owner, on a pressurised floor: "where here does it say if I consume
            // tanks or have air?" The bar showed a clock and never said whether the clock was running.
            AirSupply: AirSupplyOf(ex),
            // #573 · AND, once it is low, a BIG on-grid counter anchored to the captain — the same
            // seven-segment idiom the reactor overload uses, which is the owner's own comparison
            // ("similar counter as the round count counting down seconds on the map"). A bar in the corner
            // is for glancing at; this is for when glancing is no longer enough.
            Countdown: SuitAir.RunningLow(ex.AirSeconds, DistanceToTheTube()) || SuitAir.OnTheReserve(ex.AirSeconds)
                ? (_avatarX, _avatarY + 2.6, $"O2 {(int)(ex.AirSeconds / 60)}:{(int)(ex.AirSeconds % 60):00}")
                : null,
            Bots: _hudBots,
            Husks: _hudHusks,
            KeyHints: BuildSurfaceKeyHints(ex),
            OrbitComms: orbit?.Line,          // #327: the ship's calling-home line, never buried
            OrbitSeverity: orbit?.Severity ?? 0,
            CommsState: orbit?.CommsState ?? 0, // COMMS-LOSS: 0 nominal · 1 degraded · 2 blackout — the renderer's static/grey cue
            SweptSquares: _hudSwept,
            DarkRegions: BuildDarkRegions(ex),   // #371 Phase 3: born-dark / explored appended chambers
            Echoes: BuildEchoes(ex),             // #371 Phase 3: fading "movement was here" ripples
            StandingPrompt: BuildStandingPrompt(ex),
            // #453: the blood fades over its window, so the spatter is a beat rather than a decal.
            BloodSplash: BloodShowing ? Math.Clamp((_bloodUntilMs - (_lastTimestampMs ?? 0)) / 900.0, 0, 1) : 0,
            // #591 · The fan's real reach, so the ring the captain reads is the ring the chirp heard, and
            // where they are, so depth is on the instrument rather than on the plan behind them.
            FanReach: fanReach,
            TrackerPlace: ex.Floor < 0 ? UndergroundComplex.NameOf(ex.Stop.Body.Id, ex.Floor) : null,
            // #591 · Contacts heard through a wall are a REGION, never a body. Nothing walks these corridors
            // yet (the Old Ones are a regolith tide and are cleared on descent, by the owner's ruling), so
            // today this smudges an empty floor — which is the correct order of work: make the instrument
            // honest first, and whatever eventually comes down here inherits a tracker that already behaves
            // like it is underground instead of one that has to be taught after the fact.
            Smudges: _hudSmudges);
    }

    // #440 · The standing prompt: ONE bright line above the keybar for the thing this excursion hangs on.
    // Owner, 2026-07-26: "the press T to bury treasure is not advertised clearly enough on surface… It is
    // the key to survival there" — said while misremembering the key, which is the proof. A chest in hand is
    // the whole reason you came and the whole thing you lose, so it gets a line that does not blend into
    // chrome and does not go away until the chest is in the ground. It also answers WHERE, because "where
    // you stand" is the rule and nothing on screen ever said so: out on the open regolith, past the pad.
    private string? BuildStandingPrompt(SurfaceExcursion ex)
    {
        // #696 · A HOLD OUTRANKS THE CHEST. For the seconds it runs, the one thing on the screen that can
        // still be decided is whether the captain keeps their boots where they are — and the prompt says the
        // clock, because "hold position" without a number is an instruction to wait for an unknown length of
        // time while something walks towards you.
        if (ex.Processing is { } paper)
        {
            return $"{Core.Processing.Glyph} PROCESSING — hold position " +
                $"({Core.Processing.SecondsLeft(paper.Elapsed, ProcessingSeconds):F0} s). Step away and it is lost.";
        }

        if (!ex.Carrying)
        {
            return null; // nothing owed — the ground goes quiet again
        }
        // #723 · The floor rides along, so this line stops promising a burial on a Hive corridor. Underground
        // it now reads "walk out onto the regolith" — which is the honest instruction down there, because the
        // way to bury a chest 150 m under a facility is the lift.
        return MoonSurface.IsDiggableGround(_avatarX, _avatarY, ex.Floor)
            ? "⛏ CARRYING THE CHEST — press E to BURY IT HERE"
            : "⛏ CARRYING THE CHEST — walk out onto the regolith, then E to bury it";
    }

    /// <summary>#562 + #696 · WHICH slow thing the one bar is showing. A ladder rather than four inline
    /// conditions at the call site, because the glyph, the tint and the PROGRESS all have to pick the same
    /// winner — and three copies of one precedence order is the shape that drifts.</summary>
    private static string SurfaceChannelGlyph(SurfaceExcursion ex) =>
        ex.Channel is not null || ex.DoorChannel is not null ? "⛏"
        // #784 · …and the seated register wears the PEN, not the camera. Core.Processing.GlyphFor is the one
        // place that choice is made — the control, the bar and the book entry all read it, so the glyph over
        // a captain's head can never say "photographing" while the sim writes into the field book (#562).
        : ex.Processing is { } hold ? Core.Processing.GlyphFor(hold.Work)
        : ex.RearmBotIndex is not null ? "🔫"
        : "⛏";

    /// <summary>#562 · Is the bar the ship HELPING you (cold green) or you exposing yourself (warning
    /// amber)? Only the rearm is help. The darkroom is emphatically not: standing still in the open for
    /// twenty seconds is the cost the whole mechanic is made of, and a soothing colour over it would be the
    /// picture arguing with the sim.</summary>
    private static bool SurfaceChannelIsAid(SurfaceExcursion ex) =>
        ex.Channel is null && ex.DoorChannel is null && ex.Processing is null && ex.RearmBotIndex is not null;

    // #324: the contextual surface keybar. The owner couldn't find the deploy key — so while a bot rides
    // the sling it spells out [T] deploy, and a chest in hand spells [G] drop. Affordances never hide.
    private string BuildSurfaceKeyHints(SurfaceExcursion ex)
    {
        // #488: aboard a derelict there is nothing to DIG. There is, however, very much somewhere to plant
        // a sentry — a bot holding a corridor while a compartment pumps down is the loop this whole lane is
        // for — and this bar used to say otherwise and then hide the key, which is how the owner ended up
        // pressing T at a map that showed him nothing. Affordances never hide (#212).
        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _))
        {
            var aboard = new List<string>
            {
                "WASD — move",
                // #698 · What [E] will actually do, and the ground wins. The recovery runs ahead of console
                // dispatch (#691), so standing in the ring with "E — examine / take" on the bar is the bar
                // describing a press it is not going to get.
                StandingOnWhatYouLeft() ? LeftBehind.ReachPrompt : "E — examine / take",
            };

            // #538 · the sentry remote lives on the HUD, and it never hides: an affordance you cannot see is an
            // affordance you do not have (#212), and this is the one whose absence gets a captain shot.
            if (ex.Bots.Count > 0)
            {
                aboard.Add(_weaponsTight ? "🤖 H — WEAPONS TIGHT (press to free)" : "🤖 H — weapons tight");
            }

            if (ex.Bots.Any(b => !b.Deployed))
            {
                aboard.Add("🤖 T — deploy a sentry");
            }
            else if (ex.Bots.Any(b => b.Deployed &&
                     ((b.X - _avatarX) * (b.X - _avatarX)) + ((b.Y - _avatarY) * (b.Y - _avatarY))
                         <= DeckPlan.InteractRadius * DeckPlan.InteractRadius))
            {
                aboard.Add("🤖 T — pick up the sentry");
            }
            if (_satchel.Count > 0)
            {
                aboard.Add($"🎒 I — items ({_satchel.Count})");
            }

            // #537 · A VERB NOBODY IS TOLD ABOUT IS A VERB NOBODY HAS. Caught by booting the scene and
            // reading the hint bar, which is the owner's own method: the knock was bound, the clock ran, the
            // sweep team heard it — and the strip along the bottom never mentioned K existed.
            aboard.Add(IsSounding
                ? (_soundQuietly ? "✊ K — stop knocking" : "📡 K — stop sounding")
                : (_soundQuietly ? "✊ K — knock (quiet)" : "📡 K — sound the plating (loud)"));

            aboard.Add(_audioEnabled ? "🔊 M — mute" : "🔇 M — unmute");
            return string.Join(" ∙ ", aboard);
        }

        // #440: the bar must NAME the thing that matters. "E — dig / use" is honest but generic, and it was
        // generic at the one moment it should shout — with the chest in your hands (owner, 2026-07-26: "the
        // press T to bury treasure is not advertised clearly enough on surface… It is the key to survival
        // there", having misremembered the key himself). Carrying → the bar says BURY, in the imperative.
        // #698 · AND WHAT YOU PUT DOWN OUTRANKS BOTH OF THEM. Owner, on B12 of the clinic: "I dropped 3
        // files on somebody here but there was nothing marked onto the map?" — the deck now carries the
        // mark, and this is the other half: [E] answers your feet before it answers the walls (#691), so
        // inside the recovery ring the press is the pickup, whatever else the captain is holding. A bar
        // that promised BURY THE CHEST while the key handed back a folder would be the sim doing one thing
        // and a sentence reporting another, which is a bug class this repo has named.
        // #723 · …and that is precisely what this bar was doing underground. It offered "E — dig" on poured
        // rockcrete, and with a chest in the sling it shouted BURY THE CHEST HERE over a corridor where the
        // key now — correctly — does nothing at all. So the floor is asked first, of the same one fact the
        // key is gated on. Above ground nothing moves: the pad is not diggable either, but it is one step
        // from ground that is, so the chest keeps the imperative #440 asked for.
        var parts = new List<string>
        {
            "WASD — move",
            // #828 · …and the BIN says so, in the same ladder and the same order the [E] dispatch itself
            // asks: your feet first, then the bucket you are standing at, then the ground. Underground this
            // strip read "E — use" everywhere, which is exactly nothing at the one spot where the key opens
            // the sleeve over a bin — and a verb nobody is told about is a verb nobody has (#212/#537).
            StandingOnWhatYouLeft() ? LeftBehind.ReachPrompt
                : TheBinTakingYourPress() is { } atTheBin ? RipAndBin.KeyPrompt(atTheBin.Tier)
                : !MoonSurface.ShovelWorksOnThisFloor(ex.Floor) ? "E — use"
                : ex.Carrying ? "⛏ E — BURY THE CHEST HERE"
                : "E — dig / use",
        };
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

        // #603 · The satchel, once there is anything in it. Owner: "the I key should be advertised in the
        // hud also like we do now for the other keys." Shown WITH the count, because the useful question at
        // a glance is not "do I have pockets" but "is there anything in them".
        if (_satchel.Count > 0)
        {
            parts.Add($"🎒 I — items ({_satchel.Count})");
        }
        parts.Add(_audioEnabled ? "🔊 M — mute" : "🔇 M — unmute"); // #338: the first-sound switch, always spelled out
        return string.Join(" ∙ ", parts);
    }

    // Lane-1 (owner, 2026-07-18: "advertise the dig and bot options in text under the motion detector"):
    // the short contextual lines seated below the tracker readout in the left instrument column. They
    // teach the two levers the surface offers — the DIG (the reason to come, the reason to hurry) and the
    // SENTRY (the thing that buys time against the tide, never safety). Kept to a couple of lines so the
    // column stays legible; empty entries are skipped by the renderer.
    private List<string> BuildTrackerCaptions(SurfaceExcursion ex, int ownMarkCount)
    {
        var lines = new List<string>();

        // #564 · The tank used to be the top line HERE, and the owner went looking for a meter under the
        // tracker and found nothing — because a line of dim 10px text among the key hints is a footnote, not
        // a gauge. It is a drawn BAR now (DeckView, fed by SurfaceHud.AirSeconds); this list is back to
        // being what it always was, the affordances.

        // #728 · …EXCEPT FOR THE OTHER CONSUMABLE, which had no line anywhere. The shelter's press has been
        // announcing "N rounds into your magazines" into a stat that appeared on exactly one surface in the
        // game — the two-digit counter painted over an already-deployed bot — so a captain who kept both in
        // the sling could read a receipt and never see the account.
        //
        // It goes FIRST and directly under the air bar because it is an INSTRUMENT, not an affordance: the
        // lines below teach keys, this one reports a quantity, and the two registers should not be shuffled
        // together. Composed in Core off the same roster the counters read (SentryBot.MagazinesReadout), so
        // the HUD and the bot over there cannot come to disagree about one number.
        //
        // First also settles what a SHORT SCREEN drops, and the answer is not arbitrary: DeckView stops
        // drawing captions once they would reach the keybar, and every affordance below is ALSO spelled out
        // along that keybar (BuildKeyBar names E, T, G and I). This line is told once, here, on the whole
        // screen. Between two tellings and one telling, the one telling keeps the top of the column.
        lines.Add(SentryBot.MagazinesReadout(TheSlingAsTheInstrumentReadsIt(ex)));

        // The dig affordance, honest to the sling (playtest bug #1 / owner ruling #9: the ground must SAY
        // what's possible). Carrying → bury anywhere you stand; empty → the beach-comber probe, a real
        // fishing expedition, never a dead end. An own ✗ in this ground always earns its own lift line.
        // #723 · …and it is only an affordance where the verb exists. This is the line that sent a captain
        // pressing [E] on a spine corridor: teaching the shovel on a floor whose ground is poured rockcrete
        // is teaching a key that will not answer. The same one fact the key and the bar are gated on.
        if (MoonSurface.ShovelWorksOnThisFloor(ex.Floor))
        {
            lines.Add(ex.Carrying
                ? "⛏ E on the regolith — bury the chest where you stand"
                : "🪛 E on the regolith — probe for shallow treasure");
        }
        if (ownMarkCount > 0)
        {
            lines.Add("🗺 E at your ✗ — dig the cache back up");
        }
        // #409: once the hidden lab door is revealed, advertise it until it's forced.
        if (ex.SecretLabDoorRevealed && !ex.SecretLabForced)
        {
            lines.Add("⚙ E at the ⚙ HIDDEN DOOR — force the secret lab open");
        }

        // The sentry affordance — spell out T while it matters (a bot in the sling to set, or ones holding
        // the line). The tide never stops, so the caption tells the truth: they buy time, not safety.
        //
        // #440 (owner, live 2026-07-26: "The T key for sentry planting is not mentioned there now on the
        // sentry line?"). It wasn't: once the LAST bot left the sling, this fell to the "N holding" line,
        // which names no key at all — and the keybar only says [T] while you happen to be standing on a
        // bot. So the moment you had committed both, the key that takes them back up vanished from the
        // screen entirely. Now T is named in EVERY state that has a bot in it, planted or slung.
        int carried = ex.Bots.Count(b => !b.Deployed);
        int deployed = ex.Bots.Count(b => b.Deployed);
        if (carried > 0)
        {
            lines.Add($"🤖 T — set a sentry ({carried} in the sling)");
        }
        if (deployed > 0)
        {
            lines.Add($"🤖 {deployed} sentry holding — T at one to lift it · buys time, not safety");
        }

        return lines;
    }

    /// <summary>#728 · This excursion's magazines, in the shape Core reads them — one projection, so the
    /// instrument column and the shelter's press are asking about the same list rather than each building
    /// their own view of the same bots.</summary>
    private static IReadOnlyList<SentryBot.Carried> TheSlingAsTheInstrumentReadsIt(SurfaceExcursion ex) =>
        [.. ex.Bots.Select(b => new SentryBot.Carried(b.Unit, b.Rounds, b.Deployed))];
}
