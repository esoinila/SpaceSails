using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the Old Ones: sight, the chase, the tide, the sentries and the exchange.
public partial class Map
{
    // #465 · A SHUT DOOR IS OPAQUE. Owner, 2026-07-27: "the gun would be behind one door and not shooting
    // through it." Doors are not collision segments — the passage is always walkable, by law — so they never
    // entered the sight test, and the tube's built-in gun happily shot straight through a closed airlock.
    //
    // Opacity and solidity are NOT the same property (this is exactly the distinction #442 is about): a shut
    // door stops the eye and the round while never stopping the captain's boots. So sight queries get the
    // walls PLUS whatever doors are shut this instant, and collision keeps getting the walls alone.
    private readonly List<SurfaceCollision.Segment> _sightBlockers = [];

    // #858 · AND THE EYE IS HANDED THE INDEX, like everything that walks already is.
    //
    // Lab 45 measured what this list cost: the sightline sweep is strictly O(walls) at ~18–25 ns a segment,
    // it is 63% of a guard's whole per-frame bill on the 465-segment floor, and the SAME query against the
    // SAME walls filed into the SAME grid DeckPlan already carries (_deckPlan.CollisionField, #448) is 29×
    // faster at 436 segments and FLAT — 1.6× for 8× the stone. The legs were handed the index and the eye
    // was handed a plain list, one line apart, and nobody had noticed because the eye is small until it is
    // not. It also refilled that list EVERY frame, at O(walls), whether or not anybody was looking at
    // anything (0.0011 ms on B1) — and some callers ask for it inside a loop, once per candidate pair.
    //
    // So the list is filed into a WallIndex and KEPT. One source of truth: the index is built FROM
    // _sightBlockers, the very list this method used to return, so what the eye sweeps and what a hand-swept
    // list would sweep are the same segments by construction rather than by two authorities agreeing.
    //
    // WHEN IT IS REBUILT is the whole of the caching, and it is derived rather than timed: the stone changes
    // only when the plan does (a fresh deck, or #371's AppendRegion — both of which hand CollisionSegments a
    // NEW array, so reference identity is the honest generation token), and the door set changes only when a
    // door's shut-state actually flips. Those flips are still ASKED every frame — IsDoorShut is a handful of
    // doors and the renderer's own answer must never lag the sim's by a frame — but they are cheap, and a
    // frame that answers "the same doors are shut as last frame" does no work at all.
    private SurfaceCollision.WallIndex? _sightIndex;
    private SurfaceCollision.Segment[]? _sightStone;   // the stone _sightIndex was filed from, by identity
    private bool[] _sightDoorShut = [];                // …and which doors were shut when it was

    private IReadOnlyList<SurfaceCollision.Segment> SightBlockers()
    {
        DeckPlan.Door[] doors = _deckPlan.Doors;
        bool asFiled = _sightIndex is not null && ReferenceEquals(_sightStone, _deckPlan.CollisionSegments);
        if (_sightDoorShut.Length != doors.Length)
        {
            _sightDoorShut = new bool[doors.Length];
            asFiled = false;
        }
        for (int i = 0; i < doors.Length; i++)
        {
            bool shut = IsDoorShut(doors[i]);
            if (_sightDoorShut[i] != shut)
            {
                _sightDoorShut[i] = shut;
                asFiled = false;
            }
        }
        if (asFiled)
        {
            return _sightIndex!;
        }

        _sightBlockers.Clear();
        foreach (SurfaceCollision.Segment seg in _deckPlan.CollisionSegments)
        {
            _sightBlockers.Add(seg);
        }
        for (int i = 0; i < doors.Length; i++)
        {
            if (!_sightDoorShut[i])
            {
                continue; // standing open — you can see (and shoot) straight down the tube
            }
            DeckPlan.Door d = doors[i];
            _sightBlockers.Add(new SurfaceCollision.Segment(d.X1, d.Y1, d.X2, d.Y2));
        }
        _sightStone = _deckPlan.CollisionSegments;
        _sightIndex = SurfaceCollision.WallIndex.Build(_sightBlockers);
        return _sightIndex;
    }

    // The same rule DeckView draws with (Core Airlock), so what blocks a shot is exactly what the player
    // sees closed — one door open at a time, the far end of an interlocked tube always shut.
    private bool IsDoorShut(DeckPlan.Door d)
    {
        if (d.Locked)
        {
            return true;
        }
        double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
        double toDoor = Math.Sqrt(((_avatarX - mx) * (_avatarX - mx)) + ((_avatarY - my) * (_avatarY - my)));

        // ONE RULE, AND IT IS THE ONE THE PLAYER CAN SEE. I briefly opened doors here for Reevers too, on
        // the owner's "unlocked doors should open for reevers" — and it broke the invariant this method
        // exists to hold, stated in the comment above it: the RENDERER decides a door is open from the
        // CAPTAIN's distance and nothing else. Adding a second opener here made the sim treat a door as
        // open while the deck drew it shut, so a gun fired through a door the player could see was closed
        // (owner, twice: "a reever was shot through a closed door").
        //
        // What blocks a shot must be exactly what the player sees closed. If Reevers are ever to work
        // doors, the RENDERER has to learn it at the same moment — one source of truth or none.
        double nearestPartner = double.PositiveInfinity;
        if (d.Interlock != 0)
        {
            foreach (DeckPlan.Door other in _deckPlan.Doors)
            {
                if (other.Interlock != d.Interlock || other.Locked || other.Equals(d))
                {
                    continue;
                }
                double ox = (other.X1 + other.X2) / 2.0, oy = (other.Y1 + other.Y2) / 2.0;
                nearestPartner = Math.Min(nearestPartner,
                    Math.Sqrt(((_avatarX - ox) * (_avatarX - ox)) + ((_avatarY - oy) * (_avatarY - oy))));
            }
        }
        return !Airlock.MayOpen(toDoor, nearestPartner, DeckPlan.DoorOpenRadius);
    }

    // #446: the movers CLOSE ENOUGH TO FRIGHTEN — the same count, fenced to the dread range. The tracker
    // still hears every mover on the field (its fan is untouched, and a far blip is exactly the dread the
    // fan is for); this is only what the nerve is priced from, so a hunter you have time to walk away from
    // costs nothing. It also feeds the sighting spell, so a dot on the far rim no longer lands a jolt.
    private int CountMovingReeversWithin(double range)
    {
        double r2 = range * range;
        int n = 0;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - _avatarX, dy = r.Y - _avatarY;
            if (MotionTracker.IsMoving(r.Vx, r.Vy) && (dx * dx) + (dy * dy) <= r2)
            {
                n++;
            }
        }
        return n;
    }

    // #446: how far off the nearest Old One is, in deck units — infinity on an empty ground. Core prices the
    // whole sustained dread through this one number (NerveModel.Dread).
    private double NearestReeverRange()
    {
        double best = double.PositiveInfinity;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - _avatarX, dy = r.Y - _avatarY;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < best)
            {
                best = d2;
            }
        }
        return double.IsPositiveInfinity(best) ? best : Math.Sqrt(best);
    }

    // A net between the captain and the tube: an Old One wedged up-field (nearer the tube mouth than the
    // captain) and laterally close enough to block the sprint. Cheap geometry, matching the encirclement
    // the pack already leans into — the "cornered" the owner named, priced as a stressor.
    // #475 · CORNERED HAS TO MEAN CORNERED. Core prices this as "a net wedged between the captain and the
    // tube mouth" and charges the sharpest routine drain in the game for it — 5.0/s, more than a full-contact
    // chase — deliberately NOT discounted by range, because being cut off is not a distance term
    // (NerveModelTests.BeingCornered_IsCloseByDefinition_AndIsNeverDiscountedByRange pins that on purpose).
    //
    // The law was right; this predicate was not keeping its side of the bargain. It asked only for a contact
    // somewhere ABOVE the captain in a lateral lane, with no bound on how far above — so a single Old One
    // drifting forty deck units up, nowhere near anything, read as a net and billed the full 5.0/s. Three
    // captains in a row died on that: full gauge, never touched, killed by a dot on the far rim.
    //
    // A hunter you can comfortably walk around is not wedged between you and anywhere. So it only counts once
    // it is near enough to contest the escape — the same range at which Core says an Old One stops being
    // scenery, which keeps the two halves of the owner's ruling ("not unless they get REALLY close") agreeing.
    private bool IsCornered()
    {
        foreach (Reever r in _reevers)
        {
            if (r.Y > _avatarY + 1.0 && r.Y <= MoonSurface.SurfaceTopY + 0.5 &&
                Math.Abs(r.X - _avatarX) < CornerLateralRange)
            {
                double dx = r.X - _avatarX, dy = r.Y - _avatarY;
                if ((dx * dx) + (dy * dy) <= NerveModel.DreadRangeDeckUnits * NerveModel.DreadRangeDeckUnits)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // #586 · IN SIGHT OF THE MONOLITH — and only where the monolith actually IS.
    //
    // This used to be pure distance to MoonSurface.AnchorX/Y, which is the DEEP ANCHOR of every ground
    // there is: every seeded site puts its own fixture there (Luna's mass-driver muzzle, a plinth
    // elsewhere), so walking up to any of them fired the once-in-a-life Lovecraftian hit — 24 nerve, the
    // line "👁 The monolith resolves out of the dark", and the FirstMonolith selfie against the monolith
    // plate — over a broken launch machine. And _monolithSeen is kept FOR LIFE, so the captain who did
    // that could never be shown the real slab's beat again. Constant governing the wrong thing, and the
    // sentence disagreeing with the sim, in one line.
    //
    // Monolith.StandsOn is the same predicate the renderer builds the slab's card from, so the beat cannot
    // drift from the object again.
    /// <summary>#649 · THE DWELL, AND THE ONE STRANGE THING.
    ///
    /// <para>Three gates, all of them Core's (<see cref="MonolithWatch"/>): the PLACE (the monolith's own
    /// ground, and inside its sight), the WINDOW (about one visit-window in three is attentive, on the same
    /// slow clock the foot-offerings use, so it holds still for a whole excursion), and the DWELL — you have
    /// to STAY. Nothing is watching to see you arrive. It is watching to see whether you stand there.</para>
    ///
    /// <para>Walking out of sight resets the clock, which is the difference between standing at a thing and
    /// passing it. Once per excursion at most, and the beat costs the captain nothing —
    /// <see cref="MonolithWatch.NerveCost"/> carries the reasoning and is the one number to change.</para>
    ///
    /// <para>Deliberately NOT a story card or a plate. The picture idiom (#528) is the right instrument for
    /// almost everything and the wrong one here: a frame around a thing says THIS IS A THING, and the whole
    /// ruling is that anything happening near this stone stays deniable.</para></summary>
    private void StepMonolithWatch(double dtRealSeconds)
    {
        if (_surface is not { } ex || !MonolithWatch.CanHappenOn(ex.Stop.Body.Id, ex.Site.LayoutSalt))
        {
            return;
        }

        if (!SeesMonolith())
        {
            ex.MonolithDwellSeconds = 0;   // you walked away; standing at a thing is not passing it
            return;
        }

        ex.MonolithDwellSeconds += dtRealSeconds;

        // ?watchers=1 — the beat is rare BY DESIGN (one window in three, then forty seconds of standing
        // still), which makes it the exact shape of scene Map.Sim's own rule is about: "a scene nobody can
        // reach on demand is a scene that ships broken." The cheat opens the window and shortens the dwell;
        // it does not change what happens, so what a tester sees is what a captain sees.
        double dwell = _watchersCheat ? MonolithWatch.DwellSeconds * 0.05 : MonolithWatch.DwellSeconds;
        if (ex.MonolithWatchSpent || ex.MonolithDwellSeconds < dwell)
        {
            return;
        }

        long epoch = Monolith.EpochAt(SimTime);
        if (!_watchersCheat && !MonolithWatch.AttentiveIn(ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch))
        {
            ex.MonolithWatchSpent = true;   // this window is not one of them; do not keep asking
            return;
        }

        ex.MonolithWatchSpent = true;
        MonolithWatch.What what = MonolithWatch.Which(
            ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch, packOnTheField: _reevers.Count > 0);
        ShowAndFile(MonolithWatch.Line(what), MonolithWatch.Glyph);

        // NerveCost is 0.0 and the call is left in on purpose: the number is a feel call the owner may want
        // to make, and a call site that has to be re-found is a decision that quietly never gets made.
        if (MonolithWatch.NerveCost > 0)
        {
            ApplyNerveShock(MonolithWatch.NerveCost, "something out here was paying attention");
        }
    }

    /// <summary>How far the captain is from the deep anchor, squared. One expression, because the sight beat
    /// and the arrival beat must measure from the same point or they can disagree about where the thing
    /// is.</summary>
    private double DistanceToAnchorSquared()
    {
        double dx = _avatarX - MoonSurface.AnchorX;
        double dy = _avatarY - MoonSurface.AnchorY;
        return (dx * dx) + (dy * dy);
    }

    private bool SeesMonolith()
    {
        if (_surface is not { } ex || !Monolith.StandsOn(ex.Stop.Body.Id, ex.Site.LayoutSalt))
        {
            return false;
        }
        return DistanceToAnchorSquared() <= Monolith.SightRangeDu * Monolith.SightRangeDu;
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
        // #437: the guns obey the maze too — a slab between a bot and an Old One breaks the shot, on the
        // SAME segments the captain collides with and the Reevers sight along (owner, live 2026-07-26:
        // "Now the cannons shot though the walls").
        // #538 · WEAPONS TIGHT. While the order stands, nothing of the captain's fires — not a deployed
        // bot, not the tube gun that never runs dry. Skipping the volley entirely is the honest
        // implementation: no rounds leave, no magazines drain, and no noise is made, which is the point.
        if (!SentryBot.MayOpenFire(_weaponsTight))
        {
            return;
        }

        // #603 · And what does leave is what is IN them: a bot loaded with the lab round drops a queue in
        // one shot and one loaded with issue ball grinds them down.
        var loaded = live.Select(b => Core.Ammunition.ById(b.AmmoId)).ToList();
        // #326 · …and WHO it goes at. Both stances shoot under the same doctrine: anything standing in the
        // corridor between the captain and the way home outranks anything that is merely close to the gun.
        // The line is handed in live — the captain moved this frame — and it is null underground, where the
        // way out is a lift on another map and there is no corridor to hold.
        SentryBot.Volley volley = SentryBot.Step(deployed, targets, SightBlockers(), loaded, TheRetreatLine);

        // Fold the drained magazines back and flash a zap line from each bot that fired.
        double nowMs = _lastTimestampMs ?? 0;
        for (int i = 0; i < live.Count; i++)
        {
            SurfaceBot bot = live[i];
            bool fired = volley.Bots[i].Rounds < bot.Rounds;
            // #461: the tube's built-in gun never runs dry — it is the shuttle's fixture, not your magazine.
            // Everything else about it is an ordinary sentry (it obeys the walls, it can only shoot what it
            // can see), it simply never stops being able to hold the threshold.
            bot.Rounds = SurfaceArrival.IsDoorSentry(bot.Unit)
                ? SurfaceArrival.DoorSentryRounds
                : volley.Bots[i].Rounds;
            if (fired)
            {
                // #456: your own guns are the loudest thing on the moon. A volley calls the deep to the BOT
                // — so bringing sentries still buys time (#314), but now it is paid for by being found.
                MakeNoise(bot.X, bot.Y, ReeverHearing.Noise.Gunfire);

                // #488 · AND ABOARD, IT WAKES THEM. Owner: "when the guns start singing the reevers nearby
                // start to wake up." A hull that has been silent for forty years, and the first thing that
                // happens is automatic fire in a steel corridor — nothing sleeps through that.
                //
                // It goes through the wreck's own noise rule, so it obeys the same hard cap as everything
                // else the captain does: the NEAREST two, and no more. A firefight will steadily wake the
                // ship because it keeps happening, which is the right consequence and still never a summons.
                MakeNoiseAboard(bot.X, bot.Y, LoudEarshot);
            }
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
                // #316 · The ONE writer: the visit gets the mark it draws and the ground gets the row it
                // keeps, so a field a captain stood in is still a field he stood in next month.
                AHuskFallsAt(ex, h.X, h.Y);
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

    // Where a bot that just fired should be DRAWN aiming. Owner, live 2026-07-27: "See it fire through wall
    // now." #437/#438 taught the SHOT and the PIN to respect stone — but this, the third caller, still picked
    // by bare distance, so the gun legitimately shot the nearest thing it could SEE while the zap line was
    // drawn at the nearest thing FULL STOP. A beam painted across a monolith at a target the bot never
    // engaged: the fire was honest, the picture was not. Same CanEngage gate as the volley, so the beam can
    // only ever be drawn at the target the volley could actually have spent its round on.
    private (double X, double Y)? NearestReeverInArc(SurfaceBot bot)
    {
        // #603 · WHAT IS LOADED DECIDES WHAT IT WILL SHOOT AT. Owner: "some lab found exploding rounds
        // might be too dangerous to use to close by targets."
        //
        // A two-stage round arms after travel, so at arm's length the second charge goes off level with the
        // gun and whoever is standing beside it. The sentry simply will not take that shot — the interlock
        // idiom this ground already speaks (#462's airlock, #523's automatic, the vent readiness refusal).
        //
        // The consequence is the frightening part and it is entirely the captain's own doing: a gun loaded
        // with the wrong thing is SILENT with the pack on top of it, because of a choice made three rooms
        // ago. The override the owner asked for ("the gun complains but also gives override option to just
        // fire") belongs at the HUD, on a captain's word — not here, where it would fire itself.
        double minimum = Core.Ammunition.ById(bot.AmmoId).MinimumRangeDu;
        double minimumSq = minimum * minimum;

        double bestSq = SentryBot.RangeDeckUnits * SentryBot.RangeDeckUnits;
        (double, double)? best = null;

        // #442 · THE SAME STONE THE VOLLEY IS MEASURED AGAINST, WHICH IS WHAT THE PARAGRAPH ABOVE ALREADY
        // CLAIMED. This read `_deckPlan.CollisionField` while StepSentries three hundred lines down hands
        // SentryBot.Step the SightBlockers — so the beam and the round were asked about two different
        // worlds, and the one thing between them is exactly the thing #465 exists for: a SHUT DOOR stops an
        // eye and a round and never stops a boot, so it is in the sight list and never in the collision
        // list. A bot behind a dogged hatch therefore painted its beam at an Old One the volley had already
        // refused to spend a round on. Caught by OneWallOneTruthTests, which now reads this page and insists
        // every SentryBot sight call is handed the list that knows about doors — spelled out at each call
        // site rather than hoisted into a local, so the guard reads a literal and cannot be satisfied by a
        // variable that once held the right thing. SightBlockers() is memoized on the stone's identity and
        // the doors' shut-state (#858), so asking it per candidate is a handful of comparisons, which is
        // the same idiom PinnedBySentry already uses one screen down.
        foreach (Reever r in _reevers)
        {
            double dx = r.X - bot.X, dy = r.Y - bot.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < minimumSq)
            {
                continue;   // inside the arming distance: it would take the gun with it
            }
            if (d2 <= bestSq && SentryBot.CanEngage(bot.X, bot.Y, r.X, r.Y, SightBlockers()))
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
    // #326: …and WHICH STANCE it goes down in. The press carries the choice (⇧T is the second one) rather
    // than raising a question over a real-time field — a modal between a captain and the bot he needs on the
    // ground is the one shape this verb must never take. Retrieval ignores it: you pick a bot up the same
    // way whichever stance it was standing in.
    private void DeployOrRetrieveSentry(bool holdTheLine = false)
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
        carried.HoldsTheLine = holdTheLine;
        carried.X = _avatarX;
        carried.Y = _avatarY;
        RendererInterop.PlayCue("board");
        // #380 item 7 (owner ruling 2026-07-19: "new players are left mystified") — the FIRST deploy of an
        // excursion spells the whole doctrine out once, before the bots bite: they run dry, and a bot left
        // behind at liftoff is a write-off. Later deploys keep the short line.
        if (!ex.SentryHintShown)
        {
            ex.SentryHintShown = true;
            ShowPulseMessage($"🤖 {carried.Unit} deployed — magazine {SentryBot.Readout(carried.Rounds)}. The bot holds the line while its magazine lasts — a siege always outlasts the ammo. Bots buy time, not safety; don't forget them at liftoff.");
            return;
        }
        ShowPulseMessage($"🤖 {carried.Unit} deployed — magazine {SentryBot.Readout(carried.Rounds)}. It'll hold this arc until the counter reads 00. Bots buy time, not safety.");
    }

    /// <summary>
    /// FILL A CARRIED SENTRY AT THE LOCK. Owner: <i>"Carrying the autogun to our shuttle air-lock should reload
    /// it ( might ve needed for big ship) 😎"</i>
    ///
    /// <para>The boat carries the belts; the bot carries only what you last gave it. So a drained sentry is a
    /// WALK rather than a write-off — a stroll on a small hull, and a real decision on the 4× hauler of #531
    /// with a pack somewhere behind you. Free of any other currency on purpose: the cost is time and exposure,
    /// the same shape as the pump's.</para>
    ///
    /// <para>Returns true when it actually did something, so the lock can say that instead of opening the
    /// destination list — pressing E again gets you the list, and nothing is taken away.</para>
    /// </summary>
    private bool TryFillCarriedSentryAtTheLock()
    {
        if (_surface is not { } ex)
        {
            return false;
        }

        SurfaceBot? carried = ex.Bots.FirstOrDefault(b => !b.Deployed);
        if (carried is null)
        {
            return false;   // nothing in the sling; the lock has its usual job to do
        }

        if (!SentryBot.NeedsFilling(carried.Rounds))
        {
            ShowPulseMessage(SentryBot.AlreadyFullLine(carried.Unit));
            return false;   // it said its piece, but the lock should still open
        }

        // #540 · A COLD BOAT ARMS NOBODY. Owner, on what makes the wait bite: "As the ammo count runs down and
        // reload place is warming up to allow use and its gun." The belts live aboard her, behind a hatch that is
        // dogged while she sleeps — so going dark takes away the resupply and the covering gun as well as the ride.
        if (!SilentRunning.HatchOpen(BoatState))
        {
            ShowPulseMessage(SilentRunning.ReloadNeedsHerAwakeLine(BoatSecondsLeft));
            RendererInterop.PlayCue("block");
            return true;    // handled: an empty sling and a shut boat is not a reason to offer a ride out
        }

        int was = carried.Rounds;
        carried.Rounds = SentryBot.MaxMagazine;
        ShowPulseMessage(SentryBot.FilledLine(carried.Unit, was));
        LogAutopilotEvent($"🤖 {carried.Unit} refilled at the lock ({SentryBot.Readout(was)} → " +
                          $"{SentryBot.Readout(SentryBot.MaxMagazine)}).");
        RendererInterop.PlayCue("board");
        RequestVaultSave();
        return true;
    }

    private void StepReevers(double dtRealSeconds)
    {
        if (_surface is null || _reevers.Count == 0)
        {
            return;
        }
        double dt = Math.Min(dtRealSeconds, 0.1);
        double step = ReeverSpeed * dt;
        // The other half of the same bug: being CAUGHT was gated on the moon's safe line too, so aboard a
        // wreck nothing was ever found by anything — no blow, and no nerve either.
        bool onSurface = !CaptainBeyondReach;
        bool caught = false;
        double now = SimTime; // the thermal shuffle's time base (sim-seconds; the surface runs at 1×)
        // A Reever that advances less than this in a frame made effectively NO progress — it's at its leash,
        // wedged on a wall, or already on target. Tied to the tracker's own motion floor: sub-floor motion
        // this frame is "still" by the same law the fan reads, so we hold it and let it shiver in place.
        double idleProgress = MotionTracker.StillSpeed * dt;
        // #324: the maze is law for the many too — the Reevers bump-and-slide on the SAME wall segments
        // the captain does, and can only see the captain when no wall stands between.
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        const double reeverRadius = DeckPlan.AvatarRadius;
        // Sight for DRAWING is not the same list as sight for WALKING: a shut door stops the eye and not
        // the shamble, so the visibility test below uses the blockers (walls + shut doors) rather than the
        // collision field.
        IReadOnlyList<SurfaceCollision.Segment>? sight = OnWreck ? SightBlockers() : null;
        foreach (Reever r in _reevers)
        {
            // #488 · THE ONES THAT HAVE NOT WOKEN YET. They do not move, so they cost nothing here and the
            // motion tracker cannot see them (it is a MOTION fan — a still contact is not a contact). What
            // CAN see them is the captain: within lamp range and with no bulkhead in the way, a dormant one
            // is drawn exactly as it is — folded down, not moving, and about to stop being either.
            if (r.Dormant)
            {
                double lampDx = r.X - _avatarX, lampDy = r.Y - _avatarY;
                bool inLamp = (lampDx * lampDx) + (lampDy * lampDy) <= DormantSightRange * DormantSightRange
                              && SurfaceCollision.HasLineOfSight(_avatarX, _avatarY, r.X, r.Y, walls);
                r.VisibleOnMap = inLamp;
                r.Vx = 0;
                r.Vy = 0;

                // Its own clock, or the away team walking into it — whichever comes first.
                if (now >= r.WakeAtMs || inLamp)
                {
                    WakeTheSleeper(r);
                }
                continue;
            }

            // #488 · WHAT THE CAPTAIN CAN SEE, decided BEFORE anything below can `continue` past it. It
            // used to sit at the bottom of the loop, so an awake-but-unaware contact never reached it and
            // kept the VisibleOnMap it woke up with — drawn through steel (owner: "but also I see the
            // reever on map through the walls now"). Every path needs the same answer, so it is taken here.
            if (OnWreck)
            {
                bool wasSeen = r.VisibleOnMap;
                r.VisibleOnMap = SurfaceCollision.HasLineOfSight(_avatarX, _avatarY, r.X, r.Y, sight);

                // THE AMBUSH JOLT. Owner: "the surprise was there … but it had zero effect on my sanity?"
                // The #379 sighting spell charges only the first fright of a spell, which is right for a
                // horizon and wrong for a ship made of corners: a thing arriving INSIDE arm's reach with no
                // warning is a different event from one you watched cross a field.
                if (!wasSeen && r.VisibleOnMap)
                {
                    double jx = r.X - _avatarX, jy = r.Y - _avatarY;
                    if ((jx * jx) + (jy * jy) <= AmbushRange * AmbushRange)
                    {
                        ApplyNerveShock(
                            NervePips.SightingPips * (int)NervePips.PipUnit,
                            "it was already in the room with you");
                        RendererInterop.PlayCue("alarm");
                    }
                }
            }

            // #314: a live sentry pins the Old Ones on its arc — a Reever under a deployed, non-dry bot's
            // guns is held where it stands (stopped, not slowed) while it's ground down. Once the counter
            // reads 00 the gun goes quiet and the shamble resumes. This is "bots buy time, never safety".
            if (PinnedBySentry(r))
            {
                // The pin is law: the Old One is held where it stands while it's ground down. It is NOT a
                // statue, though (owner, cruise 2026-07-19) — it shivers in place. Capture the anchor once
                // so the mean-zero shuffle never creeps the pinned spot, and keep the tracker-facing
                // velocity a hard 0 so a pinned contact still reads honestly STILL on the fan (option a).
                if (!r.Idle)
                {
                    r.Idle = true;
                    r.AnchorX = r.X;
                    r.AnchorY = r.Y;
                }
                r.Vx = 0;
                r.Vy = 0;
                ApplyIdleShiver(r, walls, reeverRadius, now,
                    Math.Atan2(_avatarY - r.AnchorY, _avatarX - r.AnchorX));
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
            // #461: the arrival grace. A hull setting down is not news — they take it for one of their own
            // (owner: "ship in itself does not attract them. They expect it is their ship"). It is the warm
            // body walking out that is news, and even that gets a beat: nothing may notice the captain, by
            // eye OR by ear, until the grace has run. It is what makes stepping out of the door possible.
            // #488: aboard, a SHUT DOOR breaks their look as well as a wall — otherwise a hull full of
            // dogged hatches is no cover at all, and closing one behind you buys nothing. `sight` is walls
            // plus shut doors; off a wreck it is null and this is the old walls-only test exactly.
            if (SurfaceArrival.CanBeSpotted(((_lastTimestampMs ?? 0) - (_surface?.LandedAtMs ?? 0)) / 1000.0)
                && SurfaceCollision.HasLineOfSight(r.X, r.Y, _avatarX, _avatarY, sight ?? walls))
            {
                r.LastSeenX = _avatarX;
                r.LastSeenY = _avatarY;
                r.EverSeen = true;
            }

            // Owner, 2026-07-26: "make sure reevers behind walls can be unaware of the player being there
            // if they have not seen the player." An Old One that has NEVER laid eyes on the captain does
            // not know there is anyone out here to hunt — so it keeps its own ground and shivers there. It
            // no longer leans on the tube choke on spec, which read as knowing where you'd be before it had
            // any right to. It joins the hunt the frame stone stops standing between you (and once it has
            // seen you, losing sight only demotes it to the last-seen shamble — it does not forget).
            if (!r.EverSeen)
            {
                // #446 — and the owner's ruling on it, 2026-07-27: "The unaware reevers is a feature, not a
                // bug. As the player ventures deeper they can see the player then." An Old One that has
                // never laid eyes on the captain KEEPS ITS GROUND, holding whatever deep it claimed. The
                // stillness is the point: the field is quiet until you walk far enough in to be seen, and
                // then it is not. (A wander was tried here and reverted on that ruling — do not re-add it.)
                // #488 · AND ABOARD TOO. Owner: "I like them to be unaware… is there a problem with that in
                // the space scenario?" — there is not, and a prowl briefly added here was the wrong answer.
                // The only thing stillness costs a wreck is a motion fan with nothing to hear, and the fix
                // for that is not to make THEM noisy. It is to notice that the noisy thing on a dead ship is
                // the CAPTAIN: the pump, the handle, the valve, the hatch, the PA. See MakeNoiseAboard —
                // the racket you make is what puts contacts on the tracker, walking to the place it came
                // from. So the ship stays silent until you touch something, and then it does not.
                if (!r.Idle)
                {
                    r.Idle = true;
                    r.AnchorX = r.X;
                    r.AnchorY = r.Y;
                }
                r.Vx = 0;
                r.Vy = 0;
                ApplyIdleShiver(r, walls, reeverRadius, now, r.Facing);
                if (onSurface && ReeverChase.Caught(r.X, r.Y, _avatarX, _avatarY))
                {
                    caught = true; // walked right into it in the dark — that counts as being found
                }
                continue;
            }
            // Past the unaware gate above, this one HAS seen the captain: it hunts the last place it laid
            // eyes on them (their live position while the look holds).
            double tgtX = r.LastSeenX;
            double tgtY = r.LastSeenY;

            // Where this one actually stands right now (the anchor while idle) — needed to know how far the
            // run still is, so the encirclement can fade as it closes.
            double baseXPre = r.Idle ? r.AnchorX : r.X;
            double baseYPre = r.Idle ? r.AnchorY : r.Y;

            // Crude encirclement: aim a little toward the tube choke so the pack cuts the escape angle
            // instead of trailing single-file — the cornering loss-condition becomes real geometry.
            // #472 · THE BIAS SHAPES THE APPROACH, NOT THE DESTINATION. Owner: "still the reevers seem to
            // stop before the airlock" / "there is nothing between player and reevers still they do not
            // close the distance?" — and playtested: the pack parks a few units off the captain and hovers.
            //
            // The encirclement pulled the AIM POINT a fixed 28% toward the tube choke, at every range. So a
            // Reever standing on the captain was still steering at a spot offset up-field, arrived THERE,
            // and stopped — for good. It could never actually reach anybody; the cornering geometry was
            // quietly a no-contact rule.
            //
            // Fade the bias with distance: cut the escape angle while the run is long (which is the whole
            // point of it), and go straight for the captain once close. Contact is never sacrificed to
            // cleverness.
            double toTarget = Math.Sqrt(((tgtX - baseXPre) * (tgtX - baseXPre)) + ((tgtY - baseYPre) * (tgtY - baseYPre)));
            double bias = EncircleBias * Math.Clamp((toTarget - EncircleCloseRange) / EncircleFadeRange, 0, 1);
            // The encircle bias aims a little toward the WAY OUT, so they cut the escape rather than merely
            // following. Aboard a wreck the way out is her airlock, not the regolith's tube mouth — the moon
            // constants here would have them drifting toward a doorway on another map while they chased.
            double outX = OnWreck ? WreckLayout.SpawnX : MoonSurface.SpawnX;
            double outY = OnWreck ? WreckLayout.SpawnY : MoonSurface.SurfaceTopY;
            double aimX = tgtX + (outX - tgtX) * bias;
            double aimY = tgtY + (outY - tgtY) * bias;
            // #453 · ONE LEASH, AND IT IS A DOOR — not a distance. Owner, live 2026-07-27: "Let's not have
            // any don't venture too far set-up by y-coordinate. If you can get away with it with the help of
            // the sentries then do it but you might get killed by the reevers (or end up joining them)."
            //
            // This retires the 2026-07-18 tide home-range. That invisible horizontal line was the thing he
            // watched a charge halt on — "they were charging towards and just stopped… as if their path was
            // blocked by static distance from the airlock… why did they stop charging just to be shot while
            // standing still." Because ReeverChase clamped their y there, and a clamped Reever makes no
            // progress, so the client latched it Idle at zero velocity: a free target frozen on a line the
            // player could neither see nor shoot through.
            //
            // Now EVERY Old One — tide or dig-roll pack — chases to the one barrier that is real fiction: the
            // crew-only door at the tube mouth. How deep you dare go is priced by the sentries you brought
            // and your nerve, not by a number in the geometry.
            // #468 (owner, live 2026-07-27: "see how the dead reever is in the middle of the door… the reever
            // collision to door is just the centerpoint?"). It was. The crew-only clamp stopped their CENTRE
            // at the threshold, so a 0.7-radius body sat half inside the doorway — husks lay across the door
            // line, and worse, the gun's line to that centre never crossed the door segment, which is why a
            // round appeared to go THROUGH a shut door. Stop the BODY instead: they halt a full radius short
            // and the threshold stays clear, so what the player sees and what the geometry believes agree.
            // #488 · NOT ABOARD A WRECK. ReeverBarrierY is the REGOLITH's crew-only tube line (−20), and
            // ReeverChase caps every contact at it: `if (ny > barrierY) ny = barrierY`. A derelict's hull is
            // y ∈ [−9, +9], so on the first frame the cap threw the whole pack down to −20.7 — eleven units
            // below her keel, outside the ship, sitting in space at the bottom of the screen (owner, with
            // six of them out there: "that works on Miranda but not here").
            //
            // She has no such line. Her barrier is the CREW-ONLY LOCK at x = 21, which is a separate clamp
            // and already holds. Vertically the hull's own walls are the only thing that should stop them.
            double barrier = OnWreck ? double.PositiveInfinity : MoonSurface.ReeverBarrierY - reeverRadius;

            // Chase from the CANONICAL spot: while idle, r.X/r.Y carry the cosmetic shiver, so we step from
            // the fixed anchor instead (else the shuffle would feed itself and the anchor would drift). A
            // moving Reever's anchor is unset, so this is just its live position.
            double baseX = r.Idle ? r.AnchorX : r.X;
            double baseY = r.Idle ? r.AnchorY : r.Y;
            // #324 follow-up: which way this one skirts a wall it can't push through. Read off the shiver
            // seed, so the hand is FIXED per contact (no dithering at the face) and a pack splits — half
            // work a slab left, half right, and the two streams meet you around its ends.
            int wallSide = (r.JitterSeed & 1) == 0 ? 1 : -1;
            (double nx, double ny) = ReeverChase.Step(
                baseX, baseY, aimX, aimY, step * VacuumDrag(r), barrier, walls, reeverRadius, wallSide);

            // #585 · AND OUT OF THE SHELTERS. Owner, playing: "lol I saw one reever get into a shelter :-D",
            // then "3 reevers waiting in the shelter :-D". A doorway has to be a real gap or the captain
            // could not use it either, so geometry alone was always going to let a body through — but the
            // building's own arrival line promises "Nothing outside can work that door", and the whole
            // reason it exists is his ask for "rooms with doors we can hide behind while we reload our guns
            // safe from reevers". A refuge you can be followed into is just a smaller room to die in.
            //
            // Same fiction that already pens them off the shuttle: the door reads a SUIT. They may crowd the
            // threshold and wait there — which is its own good scene — and they may not come in.
            (nx, ny) = HoldOutsideShelters(nx, ny);

            // #585 · AND OUT OF ANYTHING ELSE THEY ENDED UP INSIDE. Owner, playing: "I think we landed a
            // building on top of two reevers here :-D".
            //
            // He did. The pack is spawned in regolith coordinates and the ground is BUILT around them — the
            // lift head, the outpost hut and the seeded structures all appear on a deck the Old Ones are
            // already standing on. Bump-and-slide keeps a body out of a wall it walks into; it has nothing to
            // say about a wall that arrives around a body already there, so they were sealed in, shuffling
            // inside somebody's masonry.
            //
            // So: if a contact is standing IN stone, walk it out along the shortest way. Cheap — the test is
            // one collision query that says "no" for every Old One on an ordinary frame.
            (nx, ny) = ExtricateFromStone(nx, ny, walls, reeverRadius);

            double progressed = Math.Sqrt(((nx - baseX) * (nx - baseX)) + ((ny - baseY) * (ny - baseY)));

            if (progressed < idleProgress)
            {
                // No real progress — it's at its home-range leash, wedged on a wall, or already on the
                // captain: hold it and let it shiver (owner, cruise 2026-07-19). Anchor the resting spot
                // once; keep the tracker-facing velocity 0 so a held contact reads honestly still (option a).
                if (!r.Idle)
                {
                    r.Idle = true;
                    r.AnchorX = nx;
                    r.AnchorY = ny;
                }
                r.Vx = 0;
                r.Vy = 0;
                ApplyIdleShiver(r, walls, reeverRadius, now,
                    Math.Atan2(_avatarY - r.AnchorY, _avatarX - r.AnchorX));
            }
            else
            {
                // A live shamble — measured from the canonical base so a Reever breaking out of its idle
                // hold reports honest velocity from its true resting spot, not from the shivered position.
                r.Idle = false;
                r.Vx = dt > 0 ? (nx - baseX) / dt : 0;
                r.Vy = dt > 0 ? (ny - baseY) / dt : 0;
                r.X = nx;
                r.Y = ny;
                r.Facing = Math.Atan2(_avatarY - ny, _avatarX - nx);
            }
            // #488 · THE LOCK IS CREW-ONLY. Owner: "we don't want any uninvited infestations going there."
            // The lock bulkhead has a passage in it — walls alone would let the pack walk it, the same way
            // the captain does — so the rule that stops them is the one the ship's own tube already runs on:
            // a hatch keyed to the crew. It can reach the door. It cannot open the door.
            if (OnWreck && WreckLayout.PastTheLock(r.X, DeckPlan.AvatarRadius))
            {
                r.X = WreckLayout.HeldAtLock(r.X, DeckPlan.AvatarRadius);
                r.Vx = Math.Min(r.Vx, 0);
            }

            // #488 · THE MAP IS YOUR EYES, NOT AN X-RAY. Owner: "if there is a reever behind a closed door
            // should I see it so clearly on the map … the reevers can never surprise when opening a door
            // now :-D" — dead right, and it was making the tracker pointless as well: why read a fan when
            // the deck plan already draws every body through every bulkhead?
            //
            // So aboard a wreck a contact is DRAWN only with a clear line to it — walls and SHUT DOORS both
            // count, which is what puts the surprise back into opening one. It stays on the motion tracker
            // the whole time, because a motion fan hears through steel; that is the entire point of owning
            // one. Two instruments, two jobs: the fan says something is moving over there, and your own
            // eyes say what it is and exactly where.
            if (onSurface && ReeverChase.Caught(r.X, r.Y, _avatarX, _avatarY))
            {
                caught = true;
            }
        }

        // #441: the whole pack has stepped — now make them keep their elbows out (owner: "reevers merging
        // into a one blob… they should not"). AFTER the chase, never instead of it, so the shove can never
        // cancel forward progress or deadlock a queue at a corner back into #435's stall. Only the MOVING
        // ones are shoved: an idling contact is anchored on purpose (its shiver is mean-zero around that
        // anchor), and nudging it would creep the resting spot the anchor exists to hold still.
        if (_reevers.Count > 1)
        {
            Span<(double X, double Y)> spread = stackalloc (double X, double Y)[_reevers.Count];
            for (int i = 0; i < _reevers.Count; i++)
            {
                spread[i] = (_reevers[i].X, _reevers[i].Y);
            }
            ReeverPack.KeepApart(spread, walls, reeverRadius);
            // #453: and off the captain's own dot, on the same law. Safe here because every Reever's catch
            // test has already run this frame — reaching you still catches you; this only stops the drawn
            // dots from merging into one once that verdict is in.
            ReeverPack.KeepClearOfCaptain(spread, _avatarX, _avatarY, walls, reeverRadius);
            for (int i = 0; i < _reevers.Count; i++)
            {
                Reever moved = _reevers[i];
                if (moved.Idle)
                {
                    // #466 (owner: "Why did the reevers freeze into a blob there?… it's almost like blood
                    // clotting :-D"). Idling contacts used to be SKIPPED by the shove, to protect the anchor
                    // their mean-zero shiver orbits — but stopped is exactly when a pack piles up, so the
                    // spacing switched itself off at the one moment it was needed and they clotted at the
                    // door. Space them too, and carry the ANCHOR with them so the shiver stays centred on
                    // where the body actually is instead of dragging it back into the clot.
                    double ax = spread[i].X - moved.X, ay = spread[i].Y - moved.Y;
                    moved.AnchorX += ax;
                    moved.AnchorY += ay;
                }
                moved.X = spread[i].X;
                moved.Y = spread[i].Y;
                // The tracker reads velocity, and being shoved aside IS movement — but it is not the
                // hunter's own approach, so it never re-reports as closing. Leave Vx/Vy as the chase set
                // them; the shove is a correction to where it ended, not a claim about where it was going.
            }
        }

        if (caught)
        {
            ReeverCatch();
        }

        // #453: and then the swings themselves. AFTER the pack has stepped and been spaced, so "touching"
        // is measured on where the bodies actually ended up this frame.
        ResolveReeverSwings(_lastTimestampMs ?? 0);
    }

    // Thermal motion (owner, cruise 2026-07-19: "the reevers could be more active, like little thermal
    // motion so they don't just stay still"). Shiver a STILL Old One around its fixed anchor: a tiny,
    // seeded, mean-zero positional shuffle (ReeverIdle.JitterAt) plus a slow facing twitch. The shuffle is
    // wall-slid from the anchor with the SAME bump-and-slide the shamble uses, so it can never wedge the
    // body through stone even a hair. Velocity is the caller's to zero (option a keeps the fan honest);
    // this only moves the cosmetic position and facing, never the anchor.
    private void ApplyIdleShiver(Reever r, IReadOnlyList<SurfaceCollision.Segment> walls, double radius,
        double t, double baseFacing)
    {
        (double jx, double jy) = ReeverIdle.JitterAt(r.JitterSeed, t);
        // #724 · A SHIVER LOOKS FOR NOTHING. The gait is Stagger both because this is an Old One (the
        // owner's ruling) and because a cosmetic mean-zero twitch that could funnel itself sideways into a
        // doorway would be a still body slowly walking off through the door it happened to be idling beside.
        (r.X, r.Y) = SurfaceCollision.Slide(
            r.AnchorX, r.AnchorY, jx, jy, radius, walls, SurfaceCollision.Gait.Stagger);
        r.Facing = baseFacing + ReeverIdle.FacingTwitchAt(r.JitterSeed, t);
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
            // #437: a bot only holds what it can SEE — stone between the two breaks the pin exactly as it
            // breaks the shot, so a Reever that rounds a corner genuinely breaks contact with the gun
            // grinding it down.
            if (b.Deployed && b.Rounds > 0
                && SentryBot.CanEngage(b.X, b.Y, r.X, r.Y, SightBlockers()))
            {
                return true;
            }
        }
        return false;
    }

    private void StepTide(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // #488: the tide is Old Ones clawing UP OUT OF THE REGOLITH. A derelict is a steel hull in vacuum —
        // there is no ground for them to come out of, and a wreck that quietly filled with Reevers would be
        // a different (and unearned) story than the one her evidence tells. Whatever is aboard a wreck gets
        // put there on purpose, not by the ground's own cadence.
        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _))
        {
            return;
        }
        // #318-style guard: clamp the frame delta before it feeds the accumulator so a background-tab
        // resume (rAF suspended, a multi-second delta) can't spawn a wall of Reevers in one frame — and
        // resolve at most MaxTideSpawnsPerFrame claw-outs this frame, letting any backlog trail over the
        // next few. TideSeconds only ever grows by a clamped ≤0.1 s, so in practice this loops 0–1 times.
        ex.TideSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);
        if (ex.TideNextGap <= 0.0)
        {
            ex.TideNextGap = ReeverTide.NextGap(ex.ThreatSeed, ex.TideSpawnIndex);
        }

        int resolved = 0;
        while (ex.TideSeconds >= ex.TideNextGap && resolved < MaxTideSpawnsPerFrame)
        {
            resolved++;
            ex.TideSeconds -= ex.TideNextGap;
            // The engine ceiling is a perf guard, not a gameplay cap: at the ceiling the claw-out is
            // skipped this beat but the tide clock rolls right on, so the deep resumes handing them up the
            // instant a sentry drops one and frees a slot.
            if (_reevers.Count < ReeverEngineCeiling)
            {
                SpawnTideReever(ex);
            }
            ex.TideSpawnIndex++;
            ex.TideNextGap = ReeverTide.NextGap(ex.ThreatSeed, ex.TideSpawnIndex);
        }

        // Don't bank unbounded seconds while pinned at the ceiling — hold at a single gap's worth so the
        // tide resumes promptly (not in a sudden flood) once a slot frees.
        if (_reevers.Count >= ReeverEngineCeiling && ex.TideSeconds > ex.TideNextGap)
        {
            ex.TideSeconds = ex.TideNextGap;
        }
    }

    // One tide Reever claws out of the deep edge at its seeded spawn point and begins to shamble up the
    // field. Silent by design — the motion tracker is the warning, not a klaxon (owner: "they should show
    // in the motion detector long before on the map"); only the first of an excursion earns a line so the
    // player learns the deep is alive. Marked Tide so StepReevers leashes it to the home range.
    private void SpawnTideReever(SurfaceExcursion ex)
    {
        (double x, double y) = MoonSurface.TideSpawnPoint(ex.ThreatSeed, ex.TideSpawnIndex, _avatarX, _avatarY);
        _reevers.Add(new Reever
        {
            // #563 · Facing THE CAPTAIN, not "up". It used to claw out of the bottom rim and could only ever
            // be looking one way; it rises on a ring around the captain now, so half of them would have been
            // born with their back to the only thing on the moon they care about.
            X = x, Y = y, Facing = Math.Atan2(_avatarY - y, _avatarX - x), Tide = true,
            // A distinct phase per tide contact (the spawn index, salted apart from the pack stream) so a
            // deep field of leash-held Old Ones all shiver independently at their home range.
            JitterSeed = (ex.ThreatSeed * 0xD1B54A32D192ED03UL) + (ulong)ex.TideSpawnIndex + 1UL,
        });

        // #459: a tide Reever claws out UNAWARE — it holds the deep it rose into until it sees or hears you
        // (#446's feature; the deep fills up and you meet it by venturing down). But if you are digging right
        // now, it rose into the sound of a shovel: replay the noise so anything in earshot — including the
        // one that just arrived — learns where the hole is. Otherwise MakeNoise only ever reached the Old
        // Ones that already existed when you started digging, and every latecomer was born deaf to it.
        if (ex.Channel is { } digging)
        {
            MakeNoise(digging.AnchorX, digging.AnchorY, ReeverHearing.Noise.Digging);
        }

        if (!ex.TideAnnounced)
        {
            ex.TideAnnounced = true;
            // #380 item 3: the one-time tide notice is the natural slot to say what a Reever IS — the first
            // time the deep stirs, name the Old Ones and the escape (fleeing works; they want YOU, not loot).
            ShowPulseMessage("〜 The tracker stirs — something's moving in the deep, far below. The regolith never stays empty for long. Don't linger. Reevers — the Old Ones. They don't want your loot; they want YOU. Grab what you came for and run.");
        }
    }

    // A caught digger: no loot taken (the whole point) — it prices the danger in heat, the same lever the
    // law's collectors use. Debounced so one brush isn't a stunlock.
    //
    // #380 item 1 — NOT a death today (owner constraint: don't build the surface-death / insurance-captain
    // mechanic here, just route what exists). A Reever's hand raises heat + shocks the nerve; the captain is
    // told to RUN, not resurrected. When the surface-death lane lands, this is the site that would classify
    // the death via DeathNarration.SurfaceEnd(_nerve, seed) → DeathCause.Reevers / .Joined and hand it to the
    // shared BUSTED resurrection (Cause + DeathBodyName on the encounter); the art + lines are already wired.
    private void ReeverCatch()
    {
        double now = _lastTimestampMs ?? 0;
        if (now - _lastReeverCatchMs < 1500)
        {
            return;
        }
        _lastReeverCatchMs = now;

        // #380 item 1 / Evening wind #20 — THE OVERDRAW. Nerves already bottomed out and an Old One lays
        // hands ANYWAY: this qualifying hit breaks the captain. Read on the nerve BEFORE the touch shock
        // (already empty + more damage), routed place-dependently (the Old Ones took you — or, rarely, you
        // joined them) into the shared BUSTED resurrection, where the piracy insurance issues a new captain.
        // Fail Forward — the run continues (ledger, ship and hoards persist). Below empty is where it breaks;
        // above it, the touch only floors the gauge and the captain is told to RUN, as before.
        if (_surface is { } dying && _busted is null && CaptainSuccession.OverdrawQualifies(_nerve))
        {
            TriggerSurfaceOverdrawDeath(dying, nerveRanOut: true); // the gauge broke first
            return;
        }

        if (_surface is { } ex)
        {
            ex.Catches++;
        }
        // #580 · NO SHIP HEAT FROM A HAND ON YOUR SUIT. This used to raise _heat by one per catch, and that
        // was wrong twice over. Owner: "moving on the planet should NOT cause HEAT" / "any heat should happen
        // on the surface or site, not in space" / "we don't want to be guarding our parking lot ... that is
        // not good game play :-D".
        //
        // He is right on the fiction and on the play. HEAT is what the collectors and the law hold against
        // your SHIP, earned by robbery and piracy and hot cargo — an Old One grabbing a suit on Miranda tells
        // nobody anything, and there is no ledger out here to be entered in. On the play side the coupling
        // was worse than untidy: it turned every excursion into a slow tax on the parked ship, so a good long
        // walk came home to wolves. The site's own pressure is ex.Catches, above, and that stays local.
        //
        // Same class as the Debt Collector deaths he caught earlier: a space-side consequence reaching down
        // onto a moon where it has no business being.
        // #480 · The nerve price of a hand on you is decided by NervePips, not here: ONE pip, ONCE per
        // encounter (owner: "repeated strikes should not cost more of sanity … we already take medical hit
        // from reever"), and again on every hand once the captain is nearly gone. We only report the event.
        _touchedThisFrame = true;
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage("🩸 An Old One lays hands on you — it wants no loot, only you. Tear free and RUN!");
        RequestVaultSave();
    }

    // ── #453 · THE EXCHANGE: five blows, and a die between each one and your skin ──────────────────────
    //
    // Owner, 2026-07-27: "player health could be like 5 reever hits but the reever sphere must touch the
    // player sphere when a hit is received. Player should have some melee blocking ability. Dice throw. We
    // should narrate what happens to the player. Maybe a splash of blood when reever hit goes through
    // players attempt to block it. :-D"
    //
    // A swing resolves ONLY on real contact — the two bodies touching, not merely near — and every Old One
    // winds up on its own cadence, so being held at arm's length by the pack shove (#441) is not a blender.
    private double _bloodUntilMs = double.NegativeInfinity;

    // Blood on the regolith for a moment after a blow gets through — the surface has never had visual
    // punctuation for being hurt, and "you are bleeding" should not be something you read in a corner.
    private bool BloodShowing => (_lastTimestampMs ?? 0) < _bloodUntilMs;

    // #466: a blow lands only when the two bodies TOUCH and nothing stands between them. Stone (and a shut
    // door) stops an arm exactly as it stops a round — otherwise a Reever pressed against the far face of a
    // slab is close enough to kill you through it.
    private bool CanSwingAt(Reever r, IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        // #471: contact is "at arm's length or nearer", and it must include EXACTLY arm's length. The
        // keep-off-the-captain shove (#453) parks a crowding Old One at precisely PersonalSpace — the very
        // same 1.4 that is the touch distance — so a strict comparison left every one of them a floating
        // hair too far away to ever swing. Playtested: three pressed against the captain, nerve shot, and
        // the condition still read "unmarked" because not one blow could register. A hair of tolerance.
        const double reach = CaptainCondition.TouchDistance + 0.05;
        double dx = r.X - _avatarX, dy = r.Y - _avatarY;
        if ((dx * dx) + (dy * dy) > reach * reach)
        {
            return false;
        }
        return SurfaceCollision.HasLineOfSight(r.X, r.Y, _avatarX, _avatarY, sight);
    }

    /// <summary>
    /// WHERE NOTHING CAN REACH THE CAPTAIN, on whichever thing they are standing.
    ///
    /// <para>Owner, standing shoulder to shoulder with an Old One aboard a wreck with a full nerve bar and
    /// five unmarked condition pips: <i>"look I take no damage or sanity loss from reever now."</i> He was
    /// exactly right, and it was never once possible. Both the blow and the being-caught were gated on
    /// <c>MoonSurface.IsSafeAboard</c>, which asks whether the captain is above the regolith's top rim at
    /// y = −20 — and a wreck's ENTIRE deck runs from −9 to +9. Every square metre of every derelict has
    /// always been "safely up the tube at the ship".</para>
    ///
    /// <para>The FOURTH bug of exactly this shape this weekend (the regolith tide aboard, the moon barrier
    /// clamping the pack outside the hull, the moon spawn point, and now this). The pattern is a MOON
    /// CONSTANT GOVERNING A SHIP, and it hides so well because the moon's number is not absurd for a wreck
    /// — it is merely satisfied everywhere, so the feature silently never fires and nothing ever errors.</para>
    ///
    /// <para>Aboard, safety is not a latitude. It is the shuttle's own lock: past that bulkhead is the away
    /// team's side and nothing follows you there, which is the same crew-only-door law the tube obeys.</para>
    /// </summary>
    /// <para>#621 · And the answer now lives in <see cref="AwayTeamSide.BackAtTheShuttle"/>, because the AIR
    /// needed the same fact and worked it out for itself with the moon's rule alone — the same bug, in the
    /// one instrument a captain cannot survive being lied to by. Two places computing one fact is the bug
    /// even while they agree.</para>
    private bool CaptainBeyondReach =>
        AwayTeamSide.BackAtTheShuttle(OnWreck, _avatarX, _avatarY, DeckPlan.AvatarRadius);

    private void ResolveReeverSwings(double nowMs)
    {
        if (_surface is not { } ex || _busted is not null || CaptainBeyondReach)
        {
            return; // up the tube, or past the shuttle lock — nothing reaches you there
        }

        // Who has a hand on you RIGHT NOW: bodies touching, the owner's rule. Counted first, because being
        // swarmed is itself a penalty on the block — every one past the first is another thing to watch.
        // #466 (owner, live 2026-07-27: "The reevers killed me through a wall there"). Touching is not
        // enough — a body a hair from yours on the FAR SIDE of a thin slab is still 1.4 units away, and the
        // swing landed through the stone. A blow needs a clear line as well as contact: the same sight law
        // the eyes and the guns obey (#324/#438), shut doors included (#465).
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();
        int touching = 0;
        foreach (Reever r in _reevers)
        {
            if (CanSwingAt(r, sight))
            {
                touching++;
            }
        }
        if (touching == 0)
        {
            return;
        }

        foreach (Reever r in _reevers)
        {
            if (!CanSwingAt(r, sight))
            {
                continue;
            }
            if (nowMs - r.LastSwingMs < CaptainCondition.SwingCooldownSeconds * 1000.0)
            {
                continue; // still winding up
            }
            r.LastSwingMs = nowMs;

            // #696 · SOMETHING GOT A HAND ON YOU, AND THE EXPOSURE IS GONE. Placed before the block roll on
            // purpose: being REACHED is what ends the hold, not being hurt by it. A captain who turns a blow
            // aside has still had somebody's arm come through the space they were photographing into, and a
            // darkroom that survived that would be telling them the ground is safer than it is — which is
            // the whole thing the twenty seconds were bought to say.
            ProcessingIsInterrupted(Core.Processing.Interruption.Reached);

            // The die, seeded off this contact and its swing count so a long fight never repeats itself.
            r.Swings++;
            ulong seed = DiceRule.Seed(r.JitterSeed, $"swing:{r.Swings}");
            DiceRoll roll = CaptainCondition.BlockRoll(seed, _nerve, ex.Carrying, touching);

            if (CaptainCondition.Resolve(roll) == CaptainCondition.Exchange.Blocked)
            {
                // #467: its own voice. A block RINGS — bright, hard, over in a blink — so it can never be
                // confused with the blow that gets through (owner: "I should know when I'm hurt").
                ShowPulseMessage($"🛡 {CaptainCondition.BlockLine(seed)}");
                RendererInterop.PlayCue("block");
                if (_showVentPanel)
                {
                    _ventMessage = $"🛡 {CaptainCondition.BlockLine(seed)}";
                }
                continue;
            }

            // It got through. One of the five, blood on the ground, and the old touch cost on top.
            ex.HitsTaken++;
            _bloodUntilMs = nowMs + 900;
            ShowPulseMessage($"🩸 {CaptainCondition.HitLine(seed)}");
            if (_showVentPanel)
            {
                // The pulse message lives on the canvas, and the board is standing on top of the canvas.
                // A blow landed while reading the panel has to arrive ON the panel or it never happened.
                _ventMessage = $"🩸 {CaptainCondition.HitLine(seed)}";
            }
            // #467: low, wet and wrong — nothing else in the game sounds like this. And at one pip left the
            // game stops being subtle about it: a floor-level dread tone on top, every single time.
            RendererInterop.PlayCue("wound");
            if (CaptainCondition.MaxHits - ex.HitsTaken == 1)
            {
                RendererInterop.PlayCue("last");
            }
            // #480: the blow already charged the body. The nerve is charged once for being CAUGHT (and
            // again every time once you are nearly gone) — NervePips decides, we only report it.
            _touchedThisFrame = true;
            RequestVaultSave();

            if (CaptainCondition.IsDown(ex.HitsTaken))
            {
                // The fifth blow. Routed into the SAME staged death the overdraw uses, so the piracy
                // insurance issues a new captain and the run continues (Fail Forward) — the ship, the
                // ledger and every buried cache outlive you (#455's rebirth thread).
                // The FIFTH BLOW — the condition marker decided, not the nerve. Since #480 this is the
                // common surface death, and it must not narrate as an overdraw.
                TriggerSurfaceOverdrawDeath(ex, nerveRanOut: false);
                return;
            }
        }
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
