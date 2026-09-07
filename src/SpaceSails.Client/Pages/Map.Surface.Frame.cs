using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the descent hand-off, the surface deck the renderer reads, where the captain stands, and the surface tick.
public partial class Map
{
    // #329 follow-up: narrate a coarse descent phase and hand the frame back to the browser so the queued
    // render paints (the flying-🛸 door repaints with the new sub-line) before the next synchronous block.
    // Task.Delay(1) parks on a browser timer — the yield that resets Chrome's page-unresponsive timer, so
    // each phase's block is measured on its own and never chains into a multi-second freeze.
    private async Task DescentPhaseAsync(string phase)
    {
        _descentPhase = phase;
        StateHasChanged();
        await Task.Delay(1);
    }

    // #348: pay the first surface frame HERE, under the descent door, so the live rAF loop never has to
    // cold-run it as one long block (the surviving page-unresponsive dialog). Two isolated halves, each
    // fronted by a yield: first StepSurface(0) warms the tide/chase/tracker code without advancing time,
    // then one DrawWalkFrame() paints the enlarged deck once (invisible under the door) to tier up the
    // batched DeckView.Draw + its text JSON. Guarded and try/caught — a warm-up is a nicety, never a
    // thing that may break the walk down; if anything is not ready yet, the live loop simply pays it as
    // before (still just the one dialog we had), so this can only help.
    private async Task WarmFirstSurfaceFrameAsync()
    {
        if (_deckView is null || _renderer is null || _surface is null)
        {
            return;
        }
        try
        {
            _descentPhase = "reading the ground — the sweep…";
            StateHasChanged();
            await Task.Delay(1);
            StepSurface(0); // zero dt: advances nothing, only tiers up the first cold surface step

            _descentPhase = "reading the ground — the ground…";
            StateHasChanged();
            await Task.Delay(1);
            DrawWalkFrame(); // one throwaway paint under the door — warms the cold DeckView.Draw
            await Task.Delay(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"surface warm-up skipped: {ex}");
        }
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
        // #488: a DERELICT is not a world. She gets a dead ship to walk — a spine, compartments, and the
        // evidence bolted to her decks — instead of the regolith field and its tube. Routed by body id, the
        // same trick the expedition sites use, so nothing else in the excursion has to know the difference.
        // #585 · UNDERGROUND. A floor of the Hive is laid inside the SURFACE'S OWN envelope, so the whole
        // facility costs no new coordinate space - the owner's insight, and the reason "down" beat "wider".
        // Routed here, the same way a derelict is, so nothing else in the excursion has to know.
        if (ex.Floor < 0)
        {
            // #709 · Freeze which shift the canteen is on before the room is drawn, and hand the deck that
            // number rather than a clock. Everything afterwards — the [E] press, a rebuild after searching a
            // room — reads the same frozen watch, so the people drawn at the tables stay the people the game
            // answers about.
            // #751 · …or the one a tester pinned with ?watch=N. Applied HERE, at the one place the watch is
            // ever frozen, so the cheat cannot become a second answer to "which shift is this".
            ex.CanteenWatch = _watchCheat ?? PatronRota.WatchIndex(SimTime);
            // #804 · The Hive's deck used to stop counting at the repo crew, which was right while nothing
            // walked a floor. A round does, and it lives in the LAST band — so the count has to be the whole
            // buffer or the guards would be written past DroidCount and drawn by nobody. One number, the
            // same one the regolith and the derelict use, rather than a second arithmetic that has to be
            // kept in step: this file has paid for that mistake once already (#633).
            _deckPlan = HiveInterior.FloorDeck(
                ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField(),
                SurfaceDroidCount, FillSurfaceDroids, ex.HiveRoomsEmptied,
                ex.CanteenWatch, ex.LocksShotOpen, ex.CubiclesShut, ex.CabinetsDogged,
                // #770 · …and the negotiation room the captain is holding this watch, read off the counter's
                // own book (#715) rather than out of a field on this component. Null on every floor nobody
                // has booked a room on, which is every floor of every site until somebody asks at the
                // counter — so #905's thirty pinned fingerprints see exactly what they saw before.
                TheRoomYouHold(ex),
                // #731 · …and who has already stood up and walked off this watch. One body, one place: a
                // regular crossing the hall on real legs must not ALSO be drawn in the chair they left, and
                // the one function that can be made to agree about it is the one that seats them.
                ex.HallStoodUp,
                // #731 · …and who has walked IN off the oncoming rota and taken a top. The mirror of the
                // line above and for the mirror reason: somebody the player watched cross the floor and sit
                // down has to be drawn in that chair by the same one function, or the room turns over in the
                // one frame nobody is looking at, which is what it did before this lane.
                ex.HallCameIn);
            // #411 · the head office's two floors with a beat on them get one console apiece, APPENDED the
            // way the hidden door and the outpost hut are — so the Hive's generator, and the A* audit that
            // walks every floor of it, are untouched.
            ComposeHeadOfficeFloor(ex);
            ComposeWhatYouLeft(ex);
            return;
        }

        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _) && _wreck is { } aboard)
        {
            _deckPlan = WreckInterior.WreckDeck(
                aboard, _wreckExamined, _wreckSalvaged, SurfaceDroidCount, FillSurfaceDroids,
                HeldDoors(), BlockedDoors(), _archiveAboard, _archivePurged,
                keyAboard: _keyAboard);   // #535 · the code in her crew spaces, if she is still holding one
            ComposeWhatYouLeft(ex);
            return;
        }

        _deckPlan = MoonSurface.SurfaceDeck(
            ex.Stop.Body.Id, ex.Stop.Body.Name, OwnCachePositionsAt(ex.Stop.Body.Id, ex.Site.Index),
            SurfaceDroidCount, FillSurfaceDroids,
            siteSalt: ex.Site.LayoutSalt, siteName: ex.Site.Name, // #320: the picked site seeds the ground + names the header
            // #586: which visit-window the monolith's foot is showing. Bucketed off sim time so it holds
            // still for a whole excursion (a captain who comes back the same afternoon finds what they left —
            // his object-persistence law) and has moved on by the time it is worth walking out there again.
            monolithEpoch: Monolith.EpochAt(SimTime),
            // #585: whether this ground carries a clandestine site is ALREADY decided (ResolveSecretLab, which
            // honours ?secretlab=1). The renderer is told; it never rolls again.
            hasSecretSite: ex.Lab is { HasLab: true },
            // #835 · KICKED OUT, on the shed wall, in the descent plate's own typography — and null on every
            // other rebuild this game has ever done, which is nearly all of them.
            bigLabels: TheKickedOutPlate(ex));

        // #371 Phase 3: on an expedition site, compose the sealed doors and replay every region already
        // forced open this visit onto the freshly-built base — so a bury/lift/drop rebuild grows back exactly
        // what the incremental door-force appends had. The base build is memoized (Phase 1), so this is one
        // cheap append on top, never a regeneration.
        if (ex.Expedition)
        {
            ComposeExpeditionSite(ex);
        }
        // #394: on the inbound rock, compose the marked DRILL POINT (the channeled charge-bore console).
        if (ex.Deflection)
        {
            ComposeDeflectionSite(ex);
        }
        // #409: on ANY body that hides a lab (expedition deep field or a rare ordinary moon), compose the
        // revealed hidden door and — once forced — replay the appended lab region onto the freshly-built base.
        ComposeSecretLabSite(ex);
        ComposeTiles(ex);            // #563: the ground beyond the home tile — the treadmill's carried chunk
        ComposeOutpost(ex);          // #563: the huts — its dogged hatch, or the room once it is forced
        ComposeWhatYouLeft(ex);      // #698: and whatever the captain themselves put down on this ground
        // #1061 beat 2 · …and the one thing on this ground somebody ELSE put down, which is why it is not in
        // the store above: every sentence that store prints says "where YOU left it".
        ComposeTheDroppedSchedule(ex);
    }

    /// <summary>
    /// #698 · THE MARK FOR WHAT YOU PUT DOWN. Owner, on B12 of the clinic: <i>"I dropped 3 files on somebody
    /// here but there was nothing marked onto the map?"</i>
    ///
    /// <para>Called on EVERY branch of the rebuild — the Hive's floors, a derelict's steel and the open
    /// regolith — because a captain can set a thing down on any of them, and a marker that only worked on
    /// the deck somebody happened to test is #691's flagged call shipped a second time.</para>
    ///
    /// <para>One mark per SPOT, from <see cref="LeftBehind.SpotsOn"/>, appended the way the hidden door and
    /// the outpost hut are — so nothing about the three generators changes, and the A* audits that walk them
    /// are untouched. It carries no walls, so it cannot block: the owner asked for scenery, and a mark you
    /// can be pinned against is worse than no mark at all.</para>
    /// </summary>
    private void ComposeWhatYouLeft(SurfaceExcursion ex) =>
        _deckPlan.AppendRegion(LeftMarks.Region(ex.Ground, ex.Floor));

    /// <summary>
    /// #681 · THE ONE DOOR THE CAPTAIN IS EVER PUT THROUGH. Owner, on a boot that pinned him inside a wall of
    /// the deep site's compound: <i>"The second url put me into the wall... I cannot move."</i> — and, while
    /// stuck there: <i>"Maybe some code to move the character either side instead of spawning it so it cannot
    /// move?"</i>
    ///
    /// <para>Every place the sim <b>places</b> the captain rather than letting them walk — the landing, the
    /// car coming back up, the tube mouth, the hull's airlock — goes through here. The deck is built first
    /// (the walls have to exist before anything can ask about them), then the chosen square is handed to
    /// <see cref="SpawnNudge"/>, and if it is inside collision the captain is walked out to the nearest square
    /// that is not.</para>
    ///
    /// <para><b>And it says so.</b> A rescue that happened quietly is a bug that shipped quietly. The line
    /// goes to the pulse AND to the excursion log, so a nudged spawn shows up in play and in a report — which
    /// is the only thing that keeps this from becoming the place generator bugs go to be forgotten. The CI
    /// ladder (<c>TheLandingPutsYouSomewhereYouCanWALKTests</c>) asserts the square BEFORE this runs, for
    /// exactly the same reason.</para>
    ///
    /// <para>Past <see cref="SpawnNudge.MaxDu"/> it refuses to help and shouts instead. A site with no
    /// standing room within six paces of its own spawn is built wrong, and papering that over would hand the
    /// player a working-looking excursion on ground that is not.</para>
    ///
    /// <para>#820 · There is now exactly one placement that does NOT come through here, and it is the one
    /// that is not a spawn at all: <see cref="SitCaptainOn"/>, which puts a captain ON a seat that may be
    /// solid by design. Standing back UP off one is this method's job again, which is what keeps a bench
    /// from being able to trap the dot.</para>
    /// </summary>
    /// <param name="where">What the sim was trying to do, in the captain's own words — it is what the loud
    /// failure names, so "the shuttle sets you down at the lift head" beats "spawn".</param>
    private void StandCaptainAt(double x, double y, string where)
    {
        (_avatarX, _avatarY) = (x, y);
        RebuildSurfaceDeck();

        SpawnNudge.Result spot = SpawnNudge.Clear(x, y, DeckPlan.AvatarRadius, _deckPlan.CollisionField);
        if (spot.Failed)
        {
            ShowAndFile(SpawnNudge.BrokenGroundLine(where), "⚠");
            return;
        }
        if (!spot.Moved)
        {
            return;
        }

        (_avatarX, _avatarY) = (spot.X, spot.Y);
        ShowAndFile(SpawnNudge.ClearedLine(spot.MovedDu), "🧤");
    }

    /// <summary>
    /// #820 · THE SNAP — the one place SITTING DOWN moves the body, and the only placement in the game that
    /// deliberately does not go through <see cref="StandCaptainAt"/>.
    ///
    /// <para>Owner, evening playtest 2026-08-11, on a park bench: <i>"I like the sit down symbol. The bench
    /// is nice also. I would move the avatar on top of the bench when I sit… just snap it into the correct
    /// position."</i> Every seat verb in the game used to leave the captain standing wherever they had been
    /// when they pressed [E], so the dot rested its legs a full step from the plank while the rest pips
    /// ticked.</para>
    ///
    /// <para><b>Why not the nudge.</b> <c>StandCaptainAt</c> walks a body OUT of collision, and a park bench
    /// is a solid segment in the collision field on purpose — you cannot walk through a bench. Putting the
    /// sit through it would therefore undo the very snap the owner asked for, one frame after it happened,
    /// and print a rescue line about it. A seat is not a spawn: it is a place the captain is being PUT ON,
    /// and the thing that keeps a solid one from ever trapping the dot is the other half of this law —
    /// nothing walks while seated (<c>MoveAvatar</c> refuses on <see cref="CaptainIsSeatedAnywhere"/>, which
    /// is #847's widening of that stop to every seat kind) and every standing up goes back out through
    /// <c>StandCaptainAt</c>, nudge and all, at the seat's own published step-off square
    /// (<c>TableTalk.StepOff</c>).</para>
    ///
    /// <para>The coordinate is always Core's — a bench end, a stool in the counter's published row, a chair
    /// off a top's ring, a ring-office chair — and never one this file measured (§13.15).</para>
    /// </summary>
    private void SitCaptainOn(double x, double y)
    {
        (_avatarX, _avatarY) = (x, y);
        RebuildSurfaceDeck();

        // #865 · …AND THE PLAYER IS OWED THE SIGHT OF IT. Owner, at a canteen top: "I sat down the table but
        // the pop ups blocked my view of my avatar sitting down", and on the clean retry, "Oh now it picked
        // the chair and it worked." The snap two lines up is the animation #820 and #846 both exist for, and
        // the tick after it was exactly when a first-entry card raised — because THIS METHOD IS WHY: the
        // coordinates it writes are the ones the room's polls read, so sitting down was itself what walked
        // the captain into the card. Armed here rather than in five separate seat verbs, on the same
        // discipline the snap already keeps: one placement, one law, every seat kind.
        OweTheSitBeat();
    }

    // ✗ marks the REAL spot (playtest bug #5): a free-form bury recorded the actual dug coords, so the
    // mark and the 'dig at the X' console land where the shovel did. A legacy/rumour cache with no stored
    // spot falls back to the deterministic hash-scatter, so every old save still plants a stable ✗.
    //
    // #650 · …AND ON THE RIGHT GROUND. The coords are LOCAL to the surface deck, and since #320 every one of
    // a body's 2–4 landing sites rebuilds that same local frame. Filtered by body alone — as this was — a
    // chest buried out on the Wild Plain drew its ✗ at the identical x/y on the Ridge Camp, on ground the
    // captain had never walked, and [E] dug it straight back out of a place it was not under. So the site is
    // part of the question now, and the projection itself moved down to MoonSurface.OwnCacheMarks so the
    // guard that proves it can stand on the very code the deck is built from, not on a copy of it.
    private List<(string Id, double X, double Y, int ReeverLevel)> OwnCachePositionsAt(string bodyId, int siteIndex) =>
        MoonSurface.OwnCacheMarks(_caches, bodyId, siteIndex);

    // The surface tick: dig channel, sentries, the chase, and the ambient tide — all cheap, no pathfinding.
    private void StepSurface(double dtRealSeconds)
    {
        if (_surface is null)
        {
            return;
        }

        // #472 · THE WORLD HOLDS STILL WHILE THE CARD IS UP. The first-ground lesson is a full-screen modal:
        // the captain cannot walk, cannot dig, cannot plant. The Old Ones did not care. Landing with a pack
        // out meant they closed, laid hands on and killed the captain BEHIND the card — the tutorial was a
        // death sentence, and the very first thing a new captain ever reads is the last thing they read.
        // Watched it happen live: dismissed the lesson straight onto the WHAT HAPPENED card.
        //
        // So the surface clock stops with the card. Nothing steps, and the arrival grace (#461) is rolled
        // forward by the paused span so the twenty seconds the captain is owed start when they can actually
        // use them — reading the rules must never spend the head start the rules are describing.
        // #563 · The map-just-grew card holds the world for exactly the same reason, and needs it MORE: the
        // lesson at least fires on arrival, inside the #461 grace, while this one fires the instant a door
        // gives — deep in a site, after a five-second channel that anything nearby has had time to walk
        // toward. Reading why the map grew must not be what gets you killed.
        // #562 · The tube-rearm card holds it too. The tube is the safest square on the moon, so this is
        // belt-and-braces rather than a rescue — but a modal that leaves the world running is a bug waiting
        // for the one player who opens it with something already in the tube mouth.
        // #865 · THE SIT BEAT IS SPENT FIRST, and above the card hold rather than below it. It is a debt in
        // real seconds owed to the player for having pressed [E] (Map.Seated.cs), and a debt that could only
        // be paid on ticks where nothing was covering the screen would never be paid on the one tick that
        // matters. Spending it here also means it can never be left standing across an excursion: the surface
        // clock is the only clock a seated captain has.
        SpendTheSitBeat(dtRealSeconds);

        if (_groundLessonOpen || _groundGrewOpen || _tubeRearmOpen || _airCardOpen)
        {
            _surface.LandedAtMs += dtRealSeconds * 1000.0;
            return;
        }

        // #563 · FIRST, THE GROUND UNDER THE BOOTS. If the last step crossed a tile boundary the world is
        // re-welded here, before anything asks a question about it — a Reever stepped against walls that are
        // about to be replaced is a Reever stepped against the wrong ground.
        StepGroundStream(_surface);

        StepSuitAir(dtRealSeconds);     // #564: the tank, the line, and the walk home
        StepTubeRearm(dtRealSeconds);   // #562: the ship feeds your sentries while you stand in her tube
        StepDigChannel(dtRealSeconds);
        // #696 · The darkroom hold, AFTER the tank. Order matters and it is this way round on purpose: the
        // air for this tick is charged on whatever ground the captain is standing on before the hold is
        // allowed to notice the tick at all, so a hold can never finish a frame "for free" and the alarm
        // that breaks a hold has already fired by the time the bar is asked to fill.
        StepProcessing(dtRealSeconds);
        AdvanceVacuumClocks(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds)); // #488: the vacuum soak
        AdvancePump(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds));         // #488: the thrifty road
        ServeStandingPumpOrder();                                                   // #488: …and the corridor last
        AdvanceScuttleClock(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds)); // #488: the overload
        AdvanceNests(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds));        // #488: the nest is a source
        AdvanceFire(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds));         // #524: and the fire eats
        AdvanceVacuumExposure(Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds)); // #488: vacuum is ground
        // #865 · THE FOUR ROOM POLLS, AND NOTHING MODAL COVERS THE SIT BEAT.
        //
        // Every one of these asks WHERE THE CAPTAIN'S COORDINATES ARE and raises a centred card the first
        // time the answer is a room that still owes one. #820's snap writes those coordinates: pressing [E]
        // at a top puts the dot on the chair, and the chair is inside the hall's box and inside a cabinet's.
        // So the tick after a sit is exactly when a full-screen card used to come up over the half-second the
        // snap exists to be watched — and because each latch is one-shot per excursion, it covered the FIRST
        // sit of a session and never the second. That is the whole of what the owner read as "that was
        // different than last time".
        //
        // GATED IN ONE PLACE, not in four. Each poll is a law about a room; the beat is one law about
        // PRESENTATION, and four copies of it in four methods is this repo's first named bug class. Nothing
        // is lost either way: every one of these is latched and re-polled every tick, so the card lands a beat
        // later, on a frame where the strip is up and the captain is visibly in the chair.
        if (!TheSitBeatIsSettling)
        {
            CheckVentPayoffUnderfoot();   // #488: the room shows what the vacuum left — when you walk into it
            CheckStaffMessUnderfoot();    // #725: …and the one room down here that is a find rather than a route
            CheckCantinaHallUnderfoot();  // #751: the hall, and the doors along the back of it
            CheckTheParkUnderfoot();      // #759: …and the park behind its glass, which records attendance
            CheckHusksUnderfoot();        // #316: …and what the last visit left lying in the regolith
        }
        StepDoorChannel(dtRealSeconds); // #371 Phase 3: the forced-door progress bar
        StepSecretLabDoorChannel(dtRealSeconds); // #409: the hidden lab door's force channel
        StepSecretLabDetector();                 // #585: the needle climbs as you close on a named moon
        StepOutpostDoorChannel(dtRealSeconds);   // #563: the outpost hatch's force channel
        StepDrillChannel(dtRealSeconds); // #394: the drilling — sinking the charge into the rock
        // #326 · THE BODYGUARDS WALK FIRST. A bot set down in the escort stance re-posts to the middle of
        // the captain→home line, and it does so BEFORE the volley so it shoots from where it is standing
        // this frame rather than from where it stood last one — a zap line drawn from a spot the bot has
        // already left is the third named bug class, a drawn shape reporting what the sim never said.
        StepEscorts(dtRealSeconds);
        StepSentries(dtRealSeconds);
        // #585 · NOTHING SHAMBLES DOWN HERE. Owner, stepping out of the car: "I don't think there should be
        // reevers down here", then "now the reevers are on surface right, so they should not be visible here
        // on screen now?" — both correct, and the second is the sharper point: they are still up there, and
        // a captain underground should neither see them nor hear them on the fan.
        //
        // Same law a derelict already runs under: the tide claws up out of REGOLITH, and a poured, sealed,
        // still-powered facility is not regolith. Clearing the live pack means a chase that followed you
        // across the field is simply over — and the pack you meet coming back up is a fresh one, which is a
        // better scene than one that rode down in the lift with you.
        if (_surface is { Floor: < 0 })
        {
            _reevers.Clear();
        }
        // #436 · How fast the captain is going, measured BEFORE anything looks at him and OUTSIDE the pack's
        // own step — StepReevers returns early on an empty field, and a measure that skipped those frames
        // would hand the first contact of an excursion a speed computed across every frame since the last one
        // existed. The observation roll reads this; nothing else does.
        MeasureTheCaptainsMotion(dtRealSeconds);
        StepReevers(dtRealSeconds);
        // #1061 beat 2 · …and the one person out here who is frightened of them. AFTER the pack, deliberately
        // and for the rep's own reason further down: what he decides about is a field whose Old Ones have
        // already moved this frame, so the sightline he breaks on is this frame's and not the last one's. And
        // BEFORE the walkers, so a captain who steps into a lift finds him already off the excursion's band
        // rather than standing in a corridor of B1.
        AdvanceTheHardcase(dtRealSeconds);
        StepCollectors(dtRealSeconds); // #583: the repo boat, and the people who got out of it
        // #804 · …and the ROUNDS, which are the other thing about the clause above: the pack is cleared on
        // descent and what walks the restricted floors instead is somebody on a payroll. Stepped AFTER the
        // clear, deliberately — a guard is not a contact of the same kind and never shares a list with one.
        AdvancePatrol(dtRealSeconds);
        // #731 · …and the people who are not on anybody's payroll: a regular finishing and leaving through a
        // door the captain's own TRY is refused at, and the one who comes out of one to sit at your table.
        // After the round, deliberately — a walker is not a guard and never shares a list with one.
        AdvanceWalkers(dtRealSeconds);
        // #973 L2 · …and the one who is on a payroll and is not the law: the Nebula rep, deciding whether to
        // come in through a door, drift to the next fixture on his beat, or cross the floor to a captain
        // sitting alone. AFTER the walkers, deliberately — the decision is about a floor whose bodies have
        // already been stepped this frame, his included.
        AdvanceTheRep(dtRealSeconds);
        StepExpeditionFog(dtRealSeconds); // #371 Phase 3: born-dark regions + behind-cover contacts + echoes
        // #370/#394: an away site runs NO endless tide (owner: "not a continuous endless stream like on
        // Miranda"). The expedition's beats may rouse a LIMITED pack; the deflection rock runs the pack OFF
        // entirely (the horror is the clock). The tracker stays live either way.
        if (_surface is { Deflection: true })
        {
            StepDeflection(dtRealSeconds);
        }
        else if (_surface is { Expedition: true })
        {
            StepExpedition(dtRealSeconds);
        }
        else if (!OnWreck)
        {
            StepTide(dtRealSeconds);
        }
        // #488 · A DERELICT RUNS NO TIDE. She is not ground: nothing crawls up out of a steel deck, and
        // SpawnReevers places its pack in REGOLITH coordinates — off the monolith line, against the moon's
        // barrier — so every contact the tide raised aboard her materialised OUTSIDE THE HULL, in space
        // (owner, once they were finally drawable: "now the reevers are outside the ship … they are space
        // reevers :-D").
        //
        // It also matters mechanically, not just visually: her pack is AUTHORED and FINITE (SpawnWreckPack),
        // which is the whole reason venting can clear her. An endless stream would make the vacuum soak,
        // the pump and the airlock gun all pointless — you cannot out-wait a tide.
        StepFirstContactChirp(dtRealSeconds);
        StepComms(dtRealSeconds); // COMMS-LOSS: advance the mothership downlink phase + snapshot the last-known feed
        TryRecoverDroppedChest();
    }
}
