using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — THE PACK ITSELF: where the
// Old Ones come from and how they move. `StepReevers` is the shamble — the whole per-frame chase, pack
// shove, leash and wall-slide — `ApplyIdleShiver` is the thermal twitch of one that is standing still,
// `StepTide` and `SpawnTideReever` are the deep filling the field one contact at a time, and `ReeverSeed`
// and `WatchdogLevelAt` are the two facts a ground remembers about its own watchdogs (read from
// Map.Surface.cs and Map.Surface.Dig.cs as well as here). What the pack SEES is next door in
// Map.Surface.Reevers.Sight.cs; what it does when it reaches you is in Map.Surface.Reevers.Exchange.cs.
public partial class Map
{
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
        // #563 · THE LEAFS THEY ARE HAULING, STEPPED FIRST. Owner ruling, 2026-09-06 — the Old Ones use
        // doors. A leaf that comes over on this frame has to be over for this frame's legs, sight and
        // rounds, never a frame behind the picture, so the hauling is banked before any list is taken.
        StepLeafWork(dt);
        // #324: the maze is law for the many too — the Reevers bump-and-slide on the SAME wall segments
        // the captain does, and can only see the captain when no wall stands between.
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        const double reeverRadius = DeckPlan.AvatarRadius;
        // #563 · …AND THE OLD ONES' OWN LIST: stone PLUS whatever is shut this instant (TheirLegs, which is
        // SightBlockers itself). `walls` above is the CAPTAIN's list and stays exactly what it was — a leaf
        // never stops his boot, because it opens for him. It stops theirs, because it does not.
        IReadOnlyList<SurfaceCollision.Segment> theirLegs = TheirLegs();
        // Sight for DRAWING is not the same list as sight for WALKING: a shut door stops the eye and not
        // the shamble, so the visibility test below uses the blockers (walls + shut doors) rather than the
        // collision field. #563: for THEM the two lists are now the same list, which is the point — a leaf
        // that stops the boot and not the eye is the exact asymmetry #442 was filed about.
        IReadOnlyList<SurfaceCollision.Segment>? sight = OnWreck ? theirLegs : null;
        foreach (Reever r in _reevers)
        {
            // #488 · THE ONES THAT HAVE NOT WOKEN YET. They do not move, so they cost nothing here and the
            // motion tracker cannot see them (it is a MOTION fan — a still contact is not a contact). What
            // CAN see them is the captain: within lamp range and with no bulkhead in the way, a dormant one
            // is drawn exactly as it is — folded down, not moving, and about to stop being either.
            if (r.Dormant)
            {
                // #442 · A SLEEPER IS BEHIND THE DOOR TOO. Owner ruling 2026-09-06: <i>"the Reevers should
                // not see through a closed door."</i> This read `walls` — the LEGS' list — while every awake
                // contact eighteen lines down reads `sight`, the eye's list of stone PLUS whatever is shut.
                // Opacity is not solidity, which is the whole of #442, and a shut hatch is opaque: a sleeper
                // folded down behind a dogged leaf was drawn straight through it. Worse, the lamp that DRAWS
                // it is the same lamp that WAKES it (three lines down), so a captain who had shut a hatch
                // roused what was on the far side of it without ever laying eyes on the thing.
                //
                // `sight ?? walls` is the awake path's own expression, verbatim: aboard a wreck it is stone
                // plus this instant's shut doors, and off a wreck `sight` is null and this is the old
                // walls-only test exactly, unchanged.
                double lampDx = r.X - _avatarX, lampDy = r.Y - _avatarY;
                bool inLamp = (lampDx * lampDx) + (lampDy * lampDy) <= DormantSightRange * DormantSightRange
                              && SurfaceCollision.HasLineOfSight(_avatarX, _avatarY, r.X, r.Y, sight ?? walls);
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
                ApplyIdleShiver(r, theirLegs, reeverRadius, now,
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
            // #436 · AND THE SIGHTLINE IS NOW PERMISSION TO ROLL, NOT KNOWLEDGE. Owner, 2026-07-26: "There
            // needs to be a reevers observation roll to its line of sight environment… Then the moment reever
            // discovers becomes special." This used to be the latch flipping in the same frame the geometry
            // opened; the rule that decides now lives in Core (ReeverObservation) and the beat that says so
            // lives in Map.Surface.Observation. Everything the two clauses below meant is unchanged and is
            // still asked here — the grace, and whether stone (or a shut door) stands between the two — and
            // the answer is handed to the look rather than acted on directly.
            //
            // Called with the answer either way, deliberately: a look with no sightline is how the head goes
            // back DOWN, and an un-stirring is as much of the fear window as a stirring.
            TakeALook(r, SurfaceArrival.CanBeSpotted(((_lastTimestampMs ?? 0) - (_surface?.LandedAtMs ?? 0)) / 1000.0)
                && SurfaceCollision.HasLineOfSight(r.X, r.Y, _avatarX, _avatarY, sight ?? walls));

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
                // #436 · THE HEAD COMES UP, AND IT IS DRAWN AND NOT SAID. Canon, 2026-09-05: the head coming
                // up is a POSE CHANGE ON THE EXISTING MARK, no line, no banner — the owner's own note that
                // "SECURITY ALERTED as a banner is the wrong shape". A stirred one turns to face the captain
                // (the same expression a sentry-pinned one already uses); an unaware one keeps its own
                // facing, exactly as before. Nothing else about it changes: it holds its ground, it shivers,
                // and it has not committed — which is what makes backing behind stone still work.
                ApplyIdleShiver(r, theirLegs, reeverRadius, now,
                    r.Stirred ? Math.Atan2(_avatarY - r.AnchorY, _avatarX - r.AnchorX) : r.Facing);
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
                baseX, baseY, aimX, aimY, step * VacuumDrag(r), barrier, theirLegs, reeverRadius, wallSide);

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
            //
            // #563 · AND IT IS ASKED OF THEIR LIST, WHICH NOW INCLUDES A LEAF. There is a second way to end
            // up inside a barrier and it arrived with this lane: a contact standing in a doorway while the
            // captain walks out of the leaf's own radius has the leaf close on it. Slide is a bump-and-slide
            // and has nothing to say about a body that is ALREADY inside a segment — both axes are refused
            // and it would stand there for good, wedged in a hatch. Same law, same call, same shortest way
            // out; the only change is which list is asked.
            (nx, ny) = ExtricateFromStone(nx, ny, theirLegs, reeverRadius);

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
                ApplyIdleShiver(r, theirLegs, reeverRadius, now,
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
            ReeverPack.KeepApart(spread, theirLegs, reeverRadius);
            // #453: and off the captain's own dot, on the same law. Safe here because every Reever's catch
            // test has already run this frame — reaching you still catches you; this only stops the drawn
            // dots from merging into one once that verdict is in.
            ReeverPack.KeepClearOfCaptain(spread, _avatarX, _avatarY, theirLegs, reeverRadius);
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
