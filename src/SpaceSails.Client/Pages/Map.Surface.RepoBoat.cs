using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the repo crew that comes down for the boat, and the writ it serves.
public partial class Map
{
    // ── #583 · A REPO CREW ON FOOT. Owner: "FBI does not arrest cars ... they look for the driver". ────
    //
    // They are not Old Ones and they do not behave like them: they walk, they spread out, they do not tire,
    // and what happens if one reaches you is a WRIT, not a mauling. Client-owned position, exactly like a
    // Reever's — never saved, rebuilt from the seeded roll on any reload.
    private sealed class Collector
    {
        public double X, Y, Facing;
        public double Vx, Vy;

        // The stable handedness ReeverChase.Step wants so a wall is rounded rather than dithered at. Spread
        // across the party so they flow around a slab from both ends instead of queueing at one corner.
        public int WallSide = 1;

        /// <summary>#731 · The route home, when their business here is over. Null while they are working:
        /// the pursuit is a straight wall-obeying line at a moving captain and wants no route at all, and a
        /// stale one is the shape a body teleporting across the field hides in (the sweep team's own note).
        /// </summary>
        public NpcWalk? Walk;

        /// <summary>#731 · How long this body has been at the head of the queue, working their own hatch.
        /// At <see cref="InspectionTeam.ThroughTheLockSeconds"/> they are through it and gone.</summary>
        public double AtTheHatch;

        /// <summary>#731 · The place in the file this body is walking to, and the thing the re-plan is keyed
        /// on rather than <c>Walk.For</c>.
        ///
        /// <para>It has to be here and not read off the route, because a route can fail to exist: a ground
        /// the lattice cannot join walks the fallback instead, and keying the re-plan on a null walk would
        /// re-decide every frame and reset the clock at the hatch every frame — a body that queues for ever
        /// without ever going through. NaN until they are going home.</para></summary>
        public double GoalX = double.NaN;

        /// <inheritdoc cref="GoalX"/>
        public double GoalY = double.NaN;
    }

    private readonly List<Collector> _collectors = [];

    // The engine ceiling for the buffer arithmetic below (CollectorLanding.PartySize is clamped to 4).
    private const int MaxCollectors = 4;

    // Lane-1 · THE TIDE (owner, Saturday-evening playtest 2026-07-18): "even with bots there is only so
    // long time to stay there." The deep hands up a Reever at seeded, jittered intervals for the WHOLE
    // excursion — no fixed total ("reevers coming from bottom of screen without any limited number … at
    // random intervals"). This supersedes the old dig-gated linger trickle: the tide runs from the moment
    // the boots hit regolith, not only after a dig, so time in the deep field is bounded on any visit. The
    // acute ReeverRaid pack (BeginDig) still turns out ON TOP of it — the tide is the ambient pressure.
    // ── #583 · THE REPO BOAT COMES DOWN ────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-08-01: "but the heat should not target the ship when the player is not on it but only
    // target the captain... we could have some other shuttle land near ours on some sites ... that would be
    // the heat when we are on land or at a ship looting it" — and, settling it, "FBI does not arrest cars ...
    // they look for the driver".
    //
    // #580 stopped the wolves from catching an empty hull, which was right and left heat meaning nothing
    // during the part of the game the captain is actually in. This is the other half: the collectors come to
    // the person. A boat sets down between you and your ride, a crew gets out, and they walk. They cannot be
    // out-burned out here, only outwalked — and the only door that closes on them is the tube's.
    private void StepCollectors(double dtRealSeconds)
    {
        if (_surface is not { } ex || _busted is not null)
        {
            return;
        }

        double dt = Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);
        ex.SecondsOnTheGround += dt;

        if (!ex.CollectorsComing)
        {
            return;
        }

        if (!ex.CollectorsLanded)
        {
            if (ex.SecondsOnTheGround < ex.CollectorsEtaSeconds)
            {
                return;
            }
            LandTheCollectors(ex);
            return; // one beat to read the sky before they start walking
        }

        // The maze is law for them too: they bump-and-slide on the SAME segments the captain's boots do, so
        // a building costs them the long way round exactly as it costs an Old One. Unlike an Old One there
        // is no crew-only leash — they came in their own boat and they have their own airlock.
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;

        // ── #731 · …AND WHEN THE WRIT IS SETTLED THEY WALK BACK TO THEIR OWN BOAT ────────────────────
        //
        // Checked before the pursuit, because a crew who are going home are not hunting anybody: a captain
        // who blunders into the file is not served a second writ, which is the whole of the bug this branch
        // closes.
        if (ex.CollectorsGoingHome)
        {
            TheyFileHomeThroughTheirOwnHatch(ex, dt, walls);
            return;
        }

        bool reachable = !CaptainBeyondReach;

        foreach (Collector c in _collectors)
        {
            double wasX = c.X, wasY = c.Y;
            (c.X, c.Y) = CollectorLanding.Step(
                c.X, c.Y, _avatarX, _avatarY, dt, walls, DeckPlan.AvatarRadius, c.WallSide);

            // #585: the repo crew waits outside too — which is exactly what their own line already says they
            // do ("they take up positions and settle in to wait"). A writ that walks through the door would
            // make that sentence a lie, and would take the one decision out of the scene: whether to sit on
            // your air or run for the tube.
            (c.X, c.Y) = HoldOutsideShelters(c.X, c.Y);

            c.Vx = dt > 0 ? (c.X - wasX) / dt : 0;
            c.Vy = dt > 0 ? (c.Y - wasY) / dt : 0;
            if (Math.Abs(c.Vx) > 1e-6 || Math.Abs(c.Vy) > 1e-6)
            {
                c.Facing = Math.Atan2(c.Vy, c.Vx);
            }

            // A shelter is a pressure vessel, not a sanctuary — and the game says so out loud rather than
            // letting the player discover it by being taken inside one they thought was safe.
            if (!ex.CollectorShelterNoted && ShelterUnderfoot(ex) >= 0
                && CollectorLanding.HasYou(c.X, c.Y, _avatarX, _avatarY) is false
                && Distance(c.X, c.Y, _avatarX, _avatarY) < 24)
            {
                ex.CollectorShelterNoted = true;

                // #768 · HELD: the siege plate goes up two lines below, and this is the one sentence in the
                // scene that tells a captain the shelter they are standing in will not save them. It is a
                // rule of the world learned once — a Beat — and it was being said under a backdrop.
                HoldSaying(CollectorLanding.ShelterIsNotSanctuaryLine, PulseRank.Beat);

                // #528 · AND THE PICTURE DOES THE SAME JOB THE LINE DOES: it shows them SETTLED, not
                // attacking. Nothing in this frame is a fight. The clock is your tank, and what makes it
                // horrible is how comfortable everyone else looks.
                //
                // #664 · ONCE EVER, and NOT DEFERRABLE — and the two decisions are the same decision, taken
                // from the line above: this is "a rule of the world learned once", and a rule you are told
                // once has to be told BEFORE the thing it is about. A deferrable card here waits until
                // nothing is trying to kill the captain, which on this ground means until after they have
                // been taken inside the shelter they thought was safe, at which point the card is a receipt.
                RaiseStoryBeat(StoryBeats.Beat.ShelterIsNotSanctuary, ex.CollectorCallsign);
                ReleaseHeldSayingsUnlessACardStopsTheWorld();   // #768 — it does; the shelter line waits
            }

            if (reachable && CollectorLanding.HasYou(c.X, c.Y, _avatarX, _avatarY))
            {
                TheWritIsServed(ex);
                return;
            }
        }
    }

    private static double Distance(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>#583 · The boat touches down, off to one side of the tube — near enough to be between you
    /// and the way home, never on top of the hatch (a boat parked on the door would end the excursion by
    /// geometry instead of by decision).</summary>
    private void LandTheCollectors(SurfaceExcursion ex)
    {
        ex.CollectorsLanded = true;
        ex.CollectorBoatX = CollectorLanding.SetsDownX(MoonSurface.SpawnX, ex.ThreatSeed);
        ex.CollectorBoatY = MoonSurface.ReeverBarrierY - 6;

        int party = CollectorLanding.PartySize(_heat.Level);
        _collectors.Clear();
        for (int i = 0; i < party && i < MaxCollectors; i++)
        {
            _collectors.Add(new Collector
            {
                X = ex.CollectorBoatX + ((i - ((party - 1) / 2.0)) * 3.5),
                // #731 · …and the standoff is Core's, because the place they GOT OUT at is the place they
                // queue for when they go back in. Two literals for one spot is two spots.
                Y = ex.CollectorBoatY - CollectorLanding.StandsOffTheBoatDu,
                Facing = -Math.PI / 2,
                WallSide = i % 2 == 0 ? 1 : -1,
            });
        }

        RendererInterop.PlayCue("alarm");

        // #768 · HELD, because the plate below goes up in the same breath and both of these would play under
        // its backdrop — the same family as the Hive's arrival, arising the same way: the world acting, not a
        // press on a pop-up. The RANK is what decides which one the captain is left with, and it is a
        // judgement about what the lines ARE: a boat you did not call setting down beside yours is a thing
        // that happened once and the book will keep (Beat); the hail that follows it is radio (Status).
        HoldSaying(CollectorLanding.ArrivalLine(ex.CollectorCallsign), PulseRank.Beat);
        if (!ex.CollectorsHailed)
        {
            ex.CollectorsHailed = true;
            HoldSaying(CollectorLanding.HailLine);
        }

        // #528 · THE ONLY WARNING THE PLAYER GETS. Four loaded lines narrate this pursuit and every one of
        // them was a toast; the arrival is the worst place for that, because after it the only information
        // in the world is a tracker fan. A sentence that fades in a second and a half is not a warning.
        // Core owns the words — and the caption is ClosingLine, which was written, reviewed and shipped and
        // then referenced by nothing at all until now.
        //
        // #664 · The one beat of the eleven that keeps EVERY TIME, and the sentence above is the reason. A
        // cadence exists to stop a card becoming wallpaper; a warning that is suppressed for being repetitive
        // is not wallpaper, it is a warning the player did not get — and after this card the only information
        // in the world really is a tracker fan. It is rare by its own nature (a heat threshold, and at most
        // one landing per excursion), which is the clause EveryTime is reserved for. Not deferrable for the
        // same reason: the collectors are walking as it is raised.
        RaiseStoryBeat(StoryBeats.Beat.CollectorsSetDown, ex.CollectorCallsign);
        ReleaseHeldSayingsUnlessACardStopsTheWorld();   // #768 — it does, so the two lines wait for the ✕

        // It is a fright, and a specific one: the ground just stopped being only about the Old Ones.
        ApplyNerveShock(4.0, "a boat you did not call, setting down beside yours");
        RequestVaultSave();
    }

    /// <summary>#583 · A hand on your carry loop, on foot, on somebody else's moon. It opens the SAME demand
    /// the same people open on your own deck — submit, bribe, or resist — because it is the same writ and
    /// they want the same thing. What is different is that you walked into it and cannot burn away.</summary>
    private void TheWritIsServed(SurfaceExcursion ex)
    {
        RendererInterop.PlayCue("board");
        ShowPulseMessage(CollectorLanding.ContactLine(ex.CollectorCallsign));

        ulong seed = DiceRule.Seed(ex.ThreatSeed, $"busted-on-foot:{(long)SimTime}");
        _busted = new BustedEncounter
        {
            HunterId = $"{CollectorLanding.GroundHunterIdPrefix}{ex.Stop.Body.Id}",
            HunterCallsign = ex.CollectorCallsign,
            Heat = Math.Max(1, _heat.Level),
            Seed = seed,
            Bribe = BustedRule.BribeDemand(Math.Max(1, _heat.Level), seed),
            Cause = DeathCause.Collector,
            DeathBodyName = ex.Stop.Body.Name,
        };

        // #777 · The same demand, so the same beat. It is HOSTED (StoryBeats.Presentation.Hosted): the seam
        // keeps the books and the panel we just opened is the canvas. Raised here as well as on her deck
        // because the hail is a thing that HAPPENS, and this is one of the two places it happens — a beat
        // wired at one of its edges is a beat that silently stops being told at the other.
        RaiseStoryBeat(StoryBeats.Beat.CollectorHail, ex.CollectorCallsign);

        RequestVaultSave();
    }

    // ── #731 · AND THEN THEY GO. THE ONE VISITING CREW IN THIS GAME THAT NEVER DID ────────────────────

    /// <summary>
    /// #731 · <b>THEIR BUSINESS HERE IS OVER.</b> Called from <c>RemoveHunter</c>, which is the one place in
    /// the game that means <i>this hunter is off you now</i>.
    ///
    /// <para><b>The bug it closes, and it is this repository's third named class.</b> Paying the bribe prints
    /// <i>"{callsign} logs a clean sweep and sheers off"</i> and calls <c>RemoveHunter</c> — which searches
    /// <c>_hunters</c>, the list of boats in SPACE. A crew standing on regolith was never in it. So on the
    /// ground the sentence was about somebody who had not moved: the card closed, the very next frame found
    /// them still inside <c>CollectorLanding.ReachDu</c> of the captain, and the writ was served again, with
    /// a fresh seed and a fresh demand, for ever. Nothing anywhere set <c>CollectorsComing</c> back to false
    /// and nothing anywhere took a body off the ground.</para>
    ///
    /// <para><b>And the fix is the issue's own beat rather than a flag.</b> #731: <i>"if they go behind a
    /// door that is locked to us, we use that as 'I guess that concludes the conversation' point"</i>. They
    /// came down in their own boat and the code has always said they have their own airlock; so they turn
    /// round from where they are standing and walk back to it, single file, and go in one at a time, and the
    /// scene is over because you watched it end. <b>Not one word is said about any of it</b> — the sentence
    /// that was already lying is now simply true.</para>
    /// </summary>
    private void TheirBusinessHereIsDone(string hunterId)
    {
        if (CollectorLanding.IsAGroundCrew(hunterId) && _surface is { CollectorsLanded: true } ex)
        {
            ex.CollectorsGoingHome = true;
        }
    }

    /// <summary>
    /// #731 · <b>SINGLE FILE, UNHURRIED, BACK INTO THEIR OWN BOAT.</b> The sweep team's airlock file
    /// (<c>Map.SweepTeam.cs</c>, #731 v2) walked on this ground instead of a wreck's spine — the SAME walker,
    /// the SAME queue arithmetic (<see cref="Egress.PlaceInTheFile"/>), the SAME spacing and the same time at
    /// the hatch, because it is the same behaviour and a second copy of it would be a second opinion.
    ///
    /// <h3>Their legs begin at their feet</h3>
    ///
    /// <para>The route is planned from where each body is STANDING, on the frame the card closes — never
    /// from the boat, never from the spot they got out at. #1064 killed exactly this lie in the bar (a
    /// stranger re-planned from a cellar doorstep seven deck units away) and it may not be reintroduced on a
    /// moon. The walking is <see cref="NpcWalk"/>'s over the captain's own lattice and the captain's own
    /// stone at the person's gait, so nothing here can step where the captain could not.</para>
    ///
    /// <h3>Why it is a walk and not the pursuit they came in on</h3>
    ///
    /// <para>The pursuit is a straight wall-obeying line at a moving target — right for closing on somebody,
    /// and wrong for getting from wherever you happen to be standing to one particular hatch without grinding
    /// along a building on the way. The pace drops too: <see cref="NpcWalk.PaceDu"/> is well under
    /// <see cref="CollectorLanding.PaceDuPerSecond"/>, which is the point. They are not chasing anybody any
    /// more; they are finished. If the lattice cannot join the two points at all they fall back to the plain
    /// walk they arrived on, so a ground the A* cannot cross still gets a crew that leaves.</para>
    /// </summary>
    private void TheyFileHomeThroughTheirOwnHatch(
        SurfaceExcursion ex, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        for (int i = _collectors.Count - 1; i >= 0; i--)
        {
            Collector c = _collectors[i];

            // Rank is the list's own order, so it is the same every frame and on every machine — and every
            // body still on the ground is going home, so there is nobody else to count past.
            (double gx, double gy) = Egress.PlaceInTheFile(
                ex.CollectorBoatX, ex.CollectorBoatY,
                // The queue forms on the side they got out on: deeper than the boat, away from the tube.
                0, -1, i, InspectionTeam.FileSpacingDu);

            if (double.IsNaN(c.GoalX)
                || Math.Abs(c.GoalX - gx) > 1e-9 || Math.Abs(c.GoalY - gy) > 1e-9)
            {
                (c.GoalX, c.GoalY) = (gx, gy);
                // The head goes through and everybody's rank drops: the file steps forward on the floor, on
                // the lattice, re-planned from where they are standing rather than nudged.
                //
                // …AND NO BERTH, which is the one place this differs from the sweep team's file.
                //
                // A walker normally gives the captain NpcWalk.PersonalSpaceInRadii — 1.4 du — and stops and
                // looks at him rather than pushing past. That rule is right for somebody crossing a hall and
                // WRONG here for the same reason #945 found it wrong at a table: these people had a hand on
                // the captain's carry loop one frame ago, so the berth is unsatisfiable from the very place
                // the walk begins, and the polite answer is not politeness but a deadlock — a bailiff who
                // stands looking at you for ever because you did not step back. Watched go red exactly that
                // way: two of them still on the regolith after three thousand frames, Vx and Vy zero.
                c.Walk = OnFoot(
                    ex.CollectorCallsign, new NpcWalk.Bound("", gx, gy),
                    new DeckReachability.Point(c.X, c.Y), walls, NpcWalk.NoPersonalSpace);
                c.AtTheHatch = 0;
            }

            double wasX = c.X, wasY = c.Y;
            bool stillWalking;
            if (c.Walk is { } walk)
            {
                walk.Step(dt, walls, _avatarX, _avatarY);
                (c.X, c.Y) = (walk.X, walk.Y);
                stillWalking = walk.Afoot;
            }
            else
            {
                (c.X, c.Y) = CollectorLanding.Step(
                    c.X, c.Y, gx, gy, dt, walls, DeckPlan.AvatarRadius, c.WallSide);
                stillWalking = Distance(c.X, c.Y, gx, gy) > 1.5;
            }

            StepTheCollectorTo(c, wasX, wasY, dt);
            if (stillWalking)
            {
                continue;
            }

            // At the head of the file, working the hatch. Anybody behind them stands and waits their turn,
            // which is what a queue is.
            if (i > 0)
            {
                continue;
            }

            c.AtTheHatch += dt;
            if (c.AtTheHatch >= InspectionTeam.ThroughTheLockSeconds)
            {
                _collectors.RemoveAt(i);
            }
        }

        if (_collectors.Count == 0)
        {
            // The last one is in, and the boat is not a monument. Nothing lands here again this excursion —
            // which is the same sentence as "the writ is settled", said in the only place that can enforce it.
            ex.CollectorsComing = false;
            ex.CollectorsLanded = false;
            return;
        }

        // THE HATCH IS THEIRS, and only while somebody is actually working it. The wreck's crew-only lock
        // holds a captain out while the sweep team files through it (WreckLayout.HeldAtLock); this is that
        // law on a boat that is not the captain's, and no line explains either of them.
        if (_collectors[0].AtTheHatch > 0)
        {
            (_avatarX, _avatarY) = CollectorLanding.HeldOffTheirHatch(
                _avatarX, _avatarY, ex.CollectorBoatX, ex.CollectorBoatY, DeckPlan.AvatarRadius);
        }
    }

    /// <summary>#731 · Put the body where its stride actually got to, and point it the way it actually moved.
    /// The motion fan reads <c>Vx/Vy</c> and the deck reads <c>Facing</c>, so a walked leg has to leave both
    /// saying what a slid one does — one place, so the two movers cannot give two accounts of one stride.
    /// (The sweep team's <c>StepTheBodyTo</c>, on this side's record.)</summary>
    private static void StepTheCollectorTo(Collector c, double wasX, double wasY, double dt)
    {
        double mx = c.X - wasX, my = c.Y - wasY;
        c.Vx = dt > 0 ? mx / dt : 0;
        c.Vy = dt > 0 ? my / dt : 0;
        if ((mx * mx) + (my * my) > 1e-8)
        {
            c.Facing = Math.Atan2(my, mx);
        }
    }
}
