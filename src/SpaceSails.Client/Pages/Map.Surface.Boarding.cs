using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;
/// <summary>
/// #313 · BOARDING, DESTINATION-FIRST — pick a surface, optionally load a chest, and grow the tube IN
/// PLACE.
///
/// <para>There is no teleport in here: the captain keeps standing at the bay, the down-tube and the
/// surface weld on below, and they walk down continuously. Boarding empty-handed is a complete, valid
/// sightseeing hop. This is also the one place an excursion is BUILT, which is why the ground's memory —
/// the husks and the scars somebody else left here — is seeded from this method and nowhere else.</para>
///
/// <para>Split out of <c>Map.Surface.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ── Boarding: pick a surface, optionally load a chest, and grow the tube IN PLACE. ──

    // Destination-first entry (#313). Called from the shuttle bay when the captain chooses a landable
    // surface. The chest is optional cargo already packed by the boarding panel; boarding empty-handed
    // is a complete, valid sightseeing hop. NO teleport: the captain keeps standing at the bay and the
    // down-tube + surface weld on below, so they walk down continuously.
    private async Task BeginSurfaceExcursion(ShuttleStop stop, ShuttleExcursion.ChestLoad chest, int botsToBring = 0, LandingSite? site = null)
    {
        if (_ephemeris is null)
        {
            return;
        }
        // #320: which of the body's seeded landing sites did the captain pick? Default to site 0 (the Wild
        // Plain, the canon ground) when none was chosen — an empty-salt site keeps today's ground exactly.
        LandingSite chosenSite = site ?? LandingSites.At(stop.Body.Id, 0);
        _boardTarget = null;
        _shuttleBayStops = null;

        // #318/#329 follow-up: the descent runs several FIRST-TIME synchronous blocks back to back — the
        // clock jump + buried-cache discovery scan, then the tube/surface/monolith-maze + collision weld,
        // then the first (cold-interpreted) render of the enlarged deck. On the ~100×-slower Debug bundle
        // each can pass Chrome's page-unresponsive threshold, so the owner saw the dialog fire TWICE.
        // Same cure as the boot: raise the flying-🛸 descent door and yield to the browser BETWEEN the
        // coarse phases (each narrated), so no single phase blocks the main thread long enough to trip it.
        // We do NOT restructure any generation logic — only phase-yield around the existing calls.
        _shuttleDescending = true;

        // Phase 1 — clear the bay: advance the clock across the crossing (and the discovery scan the
        // time-jump can trigger for buried caches).
        await DescentPhaseAsync("clearing the bay…");
        AdvanceShuttleClock(stop.TravelSeconds); // the flight down (abstracted by the tube) costs the clock

        // #1063 · …and the clock having moved, the neighbours have had their shift. THE BURIAL IS EVALUATED
        // HERE and only here: after the crossing's time is spent and before one wall of this ground has been
        // laid, which is the only moment a filled ground can be filled without a captain standing in it. See
        // Map.Burial.cs — on every voyage where nobody has been past a seam this does nothing at all.
        BuryWhatWasOpened();

        // #733 · …and the mothership FLIES that crossing now instead of standing still through it, so a
        // free-flying ship whose track really was diving ends the flight there rather than tunnelling
        // through the rock. Nothing further belongs under a captain who has just been collected by a
        // surface: welding a ground and a tube behind the freeze-frame would be building the wrong scene.
        if (_busted is not null)
        {
            _shuttleDescending = false;
            return;
        }

        // #370: is this landing the away-team's gig site? If so the excursion arms the expedition (no tide,
        // diced beats, the away clock) instead of a normal surface visit.
        bool isExpeditionSite = _expedition is { } plan && plan.SiteBodyId == stop.Body.Id;
        // #394: is this landing the deflection gig's inbound rock? Then the excursion arms the drilling.
        bool isDeflectionRock = _deflection is { } dgig && dgig.RockBodyId == stop.Body.Id;

        var excursion = new SurfaceExcursion
        {
            Stop = stop,
            RestoreHavenId = _dockedHavenId,
            PendingCoin = chest.Coin,
            PendingCargo = [.. chest.Cargo],
            ThreatSeed = ReeverSeed(stop.Body.Id),
            Expedition = isExpeditionSite,
            Deflection = isDeflectionRock,
            Site = chosenSite,
        };

        // #314: pull up to botsToBring sentries off the ship's roster into the sling (carried, not yet
        // deployed). They leave _shipBots for the excursion and return on liftoff unless abandoned.
        int take = Math.Clamp(botsToBring, 0, _shipBots.Count);
        for (int i = 0; i < take; i++)
        {
            ShipBot b = _shipBots[0];
            _shipBots.RemoveAt(0);
            excursion.Bots.Add(new SurfaceBot
            {
                Unit = b.Unit,
                // #728 QA · ?mags=N — the sling comes down holding what the URL asked for. Here, at the one
                // place a magazine crosses into an excursion: the readout, the shelter's receipt and both of
                // the locker's refusals all read this number, so a cheat applied any later would have shown a
                // tester one captain in the instrument and a different one at the press.
                Rounds = _magazineCheat ?? b.Rounds,
                Deployed = false,
            });
        }

        // #784 QA · ?hurt=N — the captain steps out already marked, so the short rest's HEALING half is
        // watchable. Here, at the one place an excursion's blow count begins, and never later: the condition
        // marker, the block roll's modifier stack and the breathing rate all read this number, and a cheat
        // that wrote it after they had started reading would show a tester three different captains.
        if (_hurtCheat is { } blowsAlready)
        {
            excursion.HitsTaken = Math.Clamp(blowsAlready, 0, CaptainCondition.MaxHits - 1);
        }

        // #615/#573 · AND THE BUILDING REMEMBERS BEING WALKED. The rooms this captain has already gone
        // through under THIS moon, out of the register that rides the vault and into the live set the deck
        // builder reads. Here, before the first frame of the walk, because a floor drawn ahead of the seeding
        // would put a console back on a room that was emptied a month ago — and because LEAVE's whole promise
        // is that a declined find is still there, which is a promise you can only keep if a KEPT one is not.
        SeedTurnedOverRooms(excursion);

        // #316 law 1 · …AND THE HUSKS THE LAST VISIT LEFT LYING HERE. Same moment, same reason: what the
        // ground kept is on the ship's ledger, and a field is meant to still be the field you shot it up.
        SeedTheHusksLeftHere(excursion);

        // #316 law 1, second half · …AND WHAT SOMEBODY ELSE LEFT. A hole where one of our ✗ marks used to
        // be, and any sentry a rival crew walked away from. Same moment, same reason, same ledger.
        SeedTheScarsLeftHere(excursion);

        _surface = excursion;

        // ── #583 · DOES THE HEAT FOLLOW YOU DOWN? Rolled ONCE, here, off the heat this captain earned and
        //    this excursion's threat seed. Decided at the hatch and never re-rolled, so the answer is a fact
        //    about this trip rather than a die thrown at the player every minute. ──
        _collectors.Clear();
        // Regolith only for now. The owner wants this "on land OR at a ship looting it", and he is right —
        // but a boat cannot set down inside a derelict, so that arrival is a docking and a walk in through
        // somebody else's airlock, which is its own build (#584). Landing a boat on a hull's deck plan would
        // be the geometry lying about the fiction, which is the one bug this project keeps paying for.
        excursion.CollectorsComing = !OnWreck
            && (_collectorCheatSeconds is not null
                || CollectorLanding.WillFollowYouDown(_heat.Level, excursion.ThreatSeed));
        if (excursion.CollectorsComing)
        {
            excursion.CollectorsEtaSeconds = _collectorCheatSeconds
                ?? CollectorLanding.ArrivesAfterSeconds(_heat.Level, excursion.ThreatSeed);
            excursion.CollectorCallsign = CollectorLanding.CallsignFor(excursion.ThreatSeed);
        }

        // #580 · The bird stops mid-sentence as the hatch closes. Anything it was saying was about the ship,
        // and the captain has just stopped being aboard her — leaving the bubble hanging over a moon is the
        // stale half of the same bug. (Everything that would ADD one is gated in SquawkNow.)
        _parrotSquawk = null;

        ResolveSecretLab(excursion); // #409: does this body hide one of Vantar's labs? (seed, or a known/cheat pre-reveal)
        if (_airCheatSeconds is { } startingAir)
        {
            excursion.AirSeconds = startingAir;   // #564 ?air=N — a short tank, for testing the line
        }
        _reevers.Clear();
        _sweepers.Clear();
        _lastNearestReeverRange = null;
        _chirp = MotionTracker.ChirpState.Fresh; // #338: the long ear starts armed — the first mover chirps
        _sightings = NerveModel.SightingSpell.Fresh; // #379: a fresh watch — the first fright of it lands full

        // #327: snapshot the mothership's hold at the moment of boarding DOWN — the reference the surface
        // ladder erodes against. A kept orbit quotes pulses ÷ Lab-25 trim rate; an unkept one is 0 (the
        // surface then flies a standing "not holding" red). A berthed ship carries no orbit risk (0 too;
        // SurfaceOrbitComms gates it out by _dockedHavenId anyway).
        _orbitHoldAtBoarding = _orbitKept && _dockedHavenId is null
            ? OrbitHold.HoldSeconds(_reactionMassPulses, _keepTrimPulsesPerDay)
            : 0;

        // Phase 2 — weld the tube + wide surface + monolith maze + collision segments onto the deck.
        await DescentPhaseAsync("welding the tube…");
        RebuildSurfaceDeck();

        // Phase 3 — read the ground: flip to the deck view, then paint the FIRST surface frame HERE,
        // under the still-up door, before ever handing control to the live loop.
        await DescentPhaseAsync("reading the ground…");
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;
        _deckPanX = _deckPanY = 0;

        // #348 (owner, 2026-07-18 playtest: "let's also try to fix this timeout … we basically just add
        // dynamically some web-page content … it was just one dialog"). #333 split the descent so no
        // dialog fired TWICE, but ONE remained: the first LIVE deck frame. The renderer batches a whole
        // frame into two interop calls, so DeckView.Draw is almost pure managed work — and its FIRST run
        // for the enlarged regolith (all the wall/maze/HUD paths + the text JSON) is cold-interpreted on
        // the ~100×-slower Debug bundle. The rAF loop fires it as a single un-yielded block the instant
        // _deckMode flips, which is the surviving page-unresponsive dialog. The boot's cure, pointed here:
        // pay that first frame NOW, off the rAF loop, split into its two heavy halves each on its own
        // yield (the surface step, then the paint), so the cold tiering lands in isolated slices the
        // browser breathes between. When the live loop takes over, the paths are warm and the frame cheap.
        await WarmFirstSurfaceFrameAsync();

        StateHasChanged();
        await Task.Delay(1);
        _shuttleDescending = false; // surface welded, walkable, and painted once — drop the descent door
        RendererInterop.PlayCue("board");
        string load = chest.IsEmpty
            ? "Empty sling — a fishing expedition: probe the regolith for shallow treasure (E where you stand)."
            : "A chest rides in the cargo sling — bury it anywhere on the regolith (E where you stand).";
        string bots = take > 0
            ? $" {take} sentry bot{(take == 1 ? "" : "s")} in the sling — press T on the surface to set one down."
            : "";
        if (isExpeditionSite && _expedition is { } gig)
        {
            string who = gig.Flavor == ExpeditionFlavor.Science ? "science team" : "survey crew";
            ShowPulseMessage($"🛸 Shuttle mated to {stop.Body.Name}. The {who} scrambles down the tube and fans out across the site. The ship holds the course-match above — watch the away clock. Walk them through it.");
        }
        else
        {
            ShowPulseMessage($"🛸 Shuttle mated to {stop.Body.Name}. {load}{bots} Walk down the tube. [E] the kiosk, wander, or dig — your call.");
        }
        _descentPhase = null;

        // #973 L4 · SETTING DOWN IS AN ARRIVAL TOO. A page you don't remember writing that NAMES this ground
        // is finished by standing on it — no roll. Said after the boat's own receipt above and before the
        // wreck branch below, so a derelict boarding gets it exactly as a regolith landing does.
        TheArrivalIsRemembered(stop.Body.Id);

        // #461 · the clock the arrival grace is measured off, and the house sentry that makes walking out of
        // the door possible at all (owner: "there should always be one un-paid-for sentry at the door" — he
        // had to spend one of his own just to get clear). It is the shuttle's own fixture: never bought,
        // never counted against the sling, and left behind without a ledger complaint.
        _surface!.LandedAtMs = _lastTimestampMs ?? 0;

        // #488 · A DERELICT GETS THE SAME GUN, and needs it more. Owner: "there might be an infested ship
        // where the cannons are needed also :-D … we should have a cannon in the airlock there to cover the
        // retreat." Same fixture, same law — the shuttle's own, never bought, never dry — but placed on the
        // wreck's spine just inboard of her airlock, so it covers the corridor you will be running back
        // down. On an INFESTED hull that is the difference between a salvage run and a burial.
        if (Derelict.TryParseWreckId(stop.Body.Id, out _))
        {
            _surface!.Bots.Add(new SurfaceBot
            {
                Unit = SurfaceArrival.DoorSentryUnit,
                Rounds = SurfaceArrival.DoorSentryRounds,
                Deployed = true,
                X = WreckLayout.SpawnX + 2,
                Y = WreckLayout.SpawnY,
            });

            // #488: build the valve board for this hull — which rooms the thing got into, and who sealed
            // themselves in where. Seeded off the wreck, so a reload finds the same ship.
            if (_wreck is { } aboardWreck)
            {
                PrepareVenting(aboardWreck);

                // …and whether she is carrying the one warm thing. Seeded off her id, decided ONCE here, so
                // a rebuild of the deck can never roll a node onto a hull that did not have one.
                PrepareArchiveNode(aboardWreck);

                // #535 · …and whether one of her people was carrying a code nobody was ever meant to read.
                // The same law and the same seam: rolled ONCE, here, off her id, so no rebuild of the deck
                // can put a key on a hull that never had one — or a second one on a hull that has already
                // given hers up.
                PrepareBlackOpsKey(aboardWreck);
            }

            // A fresh boarding is a fresh hull: nothing has woken, the fan has not come up, and the tracker
            // remembers nothing about her yet.
            _anythingHasWokenAboard = false;
            _wreckTrackerLive = false;
            _ghosts.Clear();

            // …and if she is infested, what got in is still aboard. Deep aft, around the nest, already
            // aware — this is the one wreck you read on the way OUT.
            // #488: ?reevers=N works ABOARD now too. The ambush cheat lives further down this method, past
            // the wreck's early return, so it has never once reached a derelict — and it could not have
            // helped if it had: SpawnReevers places its pack in regolith coordinates. Routed to the wreck's
            // own spawner instead, so the owner can dial the hull hot for the fight the airlock gun exists
            // for ("I want to test triggering the reevers :-D").
            // #538 · AND SOMEBODY ELSE MAY ALREADY BE ABOARD. The INSURANCE JOB hosts the sweep team, because
            // her own fiction already says "she was LOST ON PURPOSE… the most valuable thing aboard is the
            // evidence" — so what they came to remove is exactly what the captain came to take. Nothing had to be
            // invented for the owner's "they want to keep their secrets, but the rewards could be big also".
            // #537 · WHAT SHE IS HIDING, decided once, off her id — so a captain who comes back finds the
            // same ship rather than a fresh roll.
            ResolveHullVoid();

            if (_wreck is { Cause: Derelict.WreckCause.InsuranceJob } || _sweepTeamCheat > 0)
            {
                SpawnSweepTeam(_sweepTeamCheat > 0 ? _sweepTeamCheat : InspectionTeam.TeamSize);
            }

            if (_wreck is { Cause: Derelict.WreckCause.Infested } || _reeverAmbushCheat > 0)
            {
                SpawnWreckPack(_reeverAmbushCheat > 0 ? _reeverAmbushCheat : 4);
                ShowPulseMessage(
                    "🕷 Your lamp finds movement deep aft — she is not empty. GATE-1 is live in the airlock behind you. Read what you can and GET OUT.");
                RendererInterop.PlayCue("alarm");
            }
            return;
        }

        _surface!.Bots.Add(new SurfaceBot
        {
            Unit = SurfaceArrival.DoorSentryUnit,
            Rounds = SurfaceArrival.DoorSentryRounds,
            Deployed = true,
            // INSIDE the tube, above the mouth — owner: "inside the tube there is always an unlimited ammo
            // sentry built in… so if a reever tailgates through the door that fixed sentry prevents reever
            // from getting into the shuttle." It covers the threshold from the safe side, so the one that
            // slips in behind you dies in the corridor rather than aboard.
            X = MoonSurface.SpawnX,
            Y = MoonSurface.SurfaceTopY + 2,
        });

        // #458: the ambush cheat. #461 (owner: "That makes no sense… how did they know the shuttle would
        // land just there") — it no longer sets them down ON the pad, which was absurd fiction AND unplayable.
        // They start out in the deep, aware, and COME. That still exercises the chase, the spacing and the
        // exchange in seconds; it just does not pretend the Old Ones knew where the shuttle was going.
        if (_reeverAmbushCheat > 0)
        {
            SpawnReevers(_reeverAmbushCheat);
            ShowPulseMessage($"🧪 DEV: {_reeverAmbushCheat} Old One(s) roused in the deep and inbound — walk down and meet them.");
        }

        // #440 · THE FIRST GROUND (owner, 2026-07-26: "Definitely we need a landing site tutorial also for
        // new captains"). The surface is the only place that can take everything from you in ninety seconds,
        // and it used to explain itself in 10px of dimmed corner text. So the FIRST time a captain's boots
        // touch regolith — after the descent door has dropped and the ground is painted behind it, never
        // over the flying-🛸 door — the lesson goes up: three keys, four laws, nothing else. Once per
        // captain, persisted, then never again (#292: only greet the truly new).
        if (!_groundLessonSeen)
        {
            _groundLessonSeen = true;
            // #448: give the card its own frame. The pulse message above has just queued a render, and
            // raising a full-screen modal in the SAME synchronous stretch chains a second one onto it —
            // exactly the back-to-back blocks #333 broke apart everywhere else in this descent. One yield
            // costs a frame nobody sees and keeps the browser's clock reset between the two.
            await Task.Delay(1);
            _groundLessonOpen = true;
            StateHasChanged();
        }
    }

    // #440: the captain has read the first-ground card. The bit is already set (and saved with the vault),
    // so this only takes the card back down — a reload never re-teaches them.
    //
    // #470: the razor dismisses this through the Dismiss() seam, which hands the keyboard back to the map
    // div afterwards. It matters most here of all: this card is the FIRST thing a new captain ever sees on
    // the ground, and without the way home the tutorial that teaches three keys switched all three off.
    private void CloseGroundLesson()
    {
        _groundLessonOpen = false;
    }
}
