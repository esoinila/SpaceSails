using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #314 · THE SENTRY LINE. The
// firing cadence and the zap-line flash, the husk ledger a killed Old One leaves behind, which target is in
// which bot's arc, the deploy/retrieve verb and #806's refill at the lock — and `PinnedBySentry`, the
// question the chase and the doors both ask: is this one being held by a gun that can actually SEE it.
// #437's rule runs through all of it: a bot only holds what it can see, so stone between the two breaks the
// pin exactly as it breaks the shot.
public partial class Map
{
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
}
