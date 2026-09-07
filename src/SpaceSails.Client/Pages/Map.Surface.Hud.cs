using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the kiosk, the droid buffer, the motion tracker and the surface HUD.

/// <summary>
/// THE SURFACE HUD ITSELF — the one method that composes everything the captain sees while he is on foot,
/// out of the buffers the rest of this family fills.
///
/// <para>#251 · It stayed one method through the cut, for the reason <c>UndergroundComplex.Hall</c>'s
/// carve did: its sections are passes over ONE running set of locals, so naming them is a
/// behaviour-bearing split held to the snapshot-first standard rather than the pure-move standard this
/// lane works to.</para>
///
/// <para>The other four partials are named for what they fill it with: <c>.Kiosk</c> (#313's amenity),
/// <c>.Droids</c> (the buffer the renderer walks), <c>.Tracker</c> (the motion sweep and the prowl), and
/// <c>.Prompts</c> (every word the HUD says — the standing line, the channel glyph, the keybar and the
/// instrument column). No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
public partial class Map
{
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

            // #316 · …and the husks THIS boarding made. The regolith's refill lives past the early return
            // below, so a hull boarded after a moon walk was drawn with the MOON's husks still in the buffer
            // — somebody else's dead scattered across her compartments. A wreck keeps no ground ledger
            // (ex.Husks is this visit only, and nothing on a steel deck is ever written to one), so this is
            // the honest list: what went down here, since the airlock.
            _hudHusks.Clear();
            foreach (GroundMemory.Husk husk in ex.Husks)
            {
                _hudHusks.Add((husk.X, husk.Y));
            }

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
        //
        // #650 · AND ON ONE GROUND OF IT. The same argument one step sideways: a body's 2–4 landing sites
        // (#320) rebuild that same coordinate envelope, so a chest dug out on the Wild Plain has an (x, y)
        // on the Ridge Camp that is a different place entirely — a mark, and a beacon pointing at it, on
        // ground the captain has never walked. Site-filtered for exactly the reason the floor is gated.
        // 🗺 Layers (#405) Ground finds → Treasure ✗: the buried-cache marks the excursion HUD carries.
        foreach (TreasureCache c in LayerVisible("finds.treasure") && ex.Floor >= 0 ? _caches.CachesAt(bodyId, ex.Site.Index) : [])
        {
            if (!c.PlayerOwned)
            {
                continue;
            }
            (double mx, double my) = MoonSurface.CacheSpot(c);
            _hudMarks.Add((mx, my, c.ReeverLevel > 0));
        }

        RefreshHudBots(ex);

        _hudHusks.Clear();
        _hudPits.Clear();
        // 🗺 Layers (#405) Ground finds → Husks: the downed-Old-One marks left in the regolith (#316) —
        // and, on the same layer and for the same reason, the holes somebody else's shovel left (#316 law 1's
        // second half). A robbed ✗ and the bodies around it are ONE piece of evidence; a captain who has
        // turned the husk layer off is not asking to be shown half a crime scene.
        if (LayerVisible("finds.husks"))
        {
            foreach (GroundMemory.Husk husk in ex.Husks)
            {
                _hudHusks.Add((husk.X, husk.Y));
            }
            foreach (GroundMemory.Scar scar in ex.Scars)
            {
                if (scar.What == GroundMemory.ScarKind.Pit)
                {
                    _hudPits.Add((scar.X, scar.Y));
                }
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
                ?? (_processing is { } paper
                    ? Core.Processing.Fraction(paper.Elapsed, ProcessingSeconds)
                    : ex.RearmBotIndex is not null ? ex.RearmProgress : -1),
            // #562: and it says which. A shovel over a magazine being racked would be exactly the class of
            // lie this lane exists to fix; the rearm is the ship HELPING you, so it reads cold-green.
            ChannelGlyph: SurfaceChannelGlyph(ex, _processing),
            ChannelIsAid: SurfaceChannelIsAid(ex, _processing),
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
            Pits: _hudPits,                   // #316: the holes somebody else's shovel left at our ✗
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
}
