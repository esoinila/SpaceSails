using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #804 · THE ROUNDS, ON THE FLOOR. The brain is <see cref="PatrolBeat"/> in Core and every number, every
/// stop and every sentence below comes from it; this file is the walking, the knowing and the telling.
///
/// <para><b>They walk the A* the audits walk.</b> Owner, #729: <i>"maybe the guards can use A* also so they
/// do not come off as reevers or kind of crazy scary."</i> A leg of a round is planned with
/// <see cref="AutoWalk"/> over <c>DeckReachability</c> — the same route machinery the click-to-walk cheat
/// hands the captain, over the same collision field the captain's own stepper asks — and spent through
/// <see cref="SurfaceCollision.Slide"/> at a person's gait. A thing that finds doorways is somebody on a
/// payroll, and the player is meant to read that off the motion before any card says so.</para>
///
/// <para><b>What the captain knows and what the guard knows are different, and the difference is the
/// feature.</b> A marker is drawn only inside the captain's own sightline; outside it the motion tracker
/// hears a mover through the rock (the instrument is untouched — a guard walks, so a guard is a contact),
/// and closer-but-unseen there are boots. The guard registers the captain at a THIRD of the eye's reach, so
/// there is a real window in which you can see them and they cannot see you. That window is the whole
/// stealth verb: watch the round, wait, step out behind it.</para>
///
/// <para><b>A sighting still cannot start a chase, and that is still the default.</b> A round that registers
/// somebody standing there hails, walks over, reads the wallet, and at worst walks you back to the lift.
/// That is the owner's original law and it is untouched.</para>
///
/// <para><b>#835 · THE OTHER BRANCH, WHICH IS EARNED AND NEVER AMBIENT.</b> Owner, reversing his own
/// standing law with the implementation named: <i>"they need to catch us .... like reevers :-D we could use
/// that code :-D"</i> — and, in the same breath, <i>"just no damage by default :-D"</i>. So there are now
/// exactly three doors into a run (<see cref="PatrolBeat.Provocation"/>): walking off on a hail for the
/// SECOND time in a watch, being watched taking a hasp off with a gun, and having been booked
/// <see cref="PatrolBeat.EscortsAWatchAllows"/> times already. Every one of them is a thing the captain did,
/// and the paragraph above is what happens on every floor where none of them has. He calls it in before he
/// moves (<see cref="TheRadioCall"/>), comes on the Old Ones' own homing step at a person's gait
/// (<see cref="RunAfterTheCaptain"/> → <c>ReeverChase.Step</c>), and ends either with a hand on your arm
/// (<see cref="HeHasYou"/>) or standing in a corridor watching you go (<see cref="HeLosesYou"/>). He is
/// never removed from the floor by either. <b>Nothing on this road touches the captain's health</b> — there
/// is no <c>HitsTaken</c> in this file and there never will be — and the ladder it feeds is the escort you
/// already know, which past the threshold simply keeps going up (<see cref="TheKickOut"/>).</para>
///
/// <para><b>#833 · AND EVERY BEAT OF IT IS WALKED.</b> Two things in this file used to be sentences over
/// placements, and the owner caught both in one evening on B2. The card went up the frame he NOTICED you, at
/// up to nine deck units — <i>"I think the guard should approach us when it does the inspection"</i> — so
/// there is now a HAIL and an APPROACH between the notice and the read (<see cref="TheHail"/>,
/// <see cref="WalkUpToTheCaptain"/>), the captain's controls stay free through all of it, and walking away is
/// allowed. And the escort was <c>StandCaptainAt</c>: <i>"how did I jump to elevator there?"</i> — so the
/// walk back is now a walk (<see cref="WalkTheEscort"/>), he plans the route himself, the captain is walked
/// at his shoulder through his own collision, and both of them are moving contacts on the fan the whole way.
/// The one placement left is behind a caption that ADMITS it is a cut.</para>
///
/// <para><b>One stepper.</b> The round, the walk-up and the escort all spend their frame through
/// <see cref="SpendTheStride"/> — the captain's own sub-stepper, once. #832 was paid for by a copy of that
/// loop drifting from its original by one epsilon; three copies of it would have been three chances to do
/// that again.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>
    /// #870 lane 6′b · THE ROUND'S OWN STATE, IN ONE OBJECT (<c>Pages/Patrol/Patrol.cs</c>).
    ///
    /// <para>Twenty-two loose fields became one. <b>#870 lane 6′c · AND NOW THE VERBS TOO</b> — putting men
    /// on the floor, walking them, hailing, reading a wallet, running, walking a captain out — every one of
    /// them lives on <c>Patrol</c>'s own partials beside its state (<c>Pages/Patrol/Patrol.*.cs</c>). What a
    /// verb still needs from the page it is walked on is <see cref="IPatrolHost"/>, and that interface is
    /// the whole of the coupling: twenty-one members, written down, and it may only shrink.</para>
    ///
    /// <para><b><c>readonly</c>, and never re-assigned.</b> Leaving a floor EMPTIES the round
    /// (<see cref="SpawnPatrolFor"/>); it does not swap in a different one. A second <c>Patrol</c> would be
    /// a second answer to <i>who is walking this floor</i>, which is this repo's first named bug class
    /// aimed at a rota. There is a guard fact for exactly that.</para>
    ///
    /// <para>#870 lane 6′c · It is BUILT IN THE CONSTRUCTOR now rather than at its declaration, and that is
    /// a language rule rather than a design change: the round is handed the page it walks on, and an
    /// instance field initialiser may not name <c>this</c>. Still one round, still assigned exactly once.</para>
    /// </summary>
    private readonly Patrol _patrol;

    // ── #870 lane 6′a/6′b · WHAT THE REST OF THE PAGE MAY ASK THE ROUND ────────────────────────
    //
    // What the rest of the client actually wanted was never a field — a bench wants to know who is walking
    // the floor, a bin whether the rota has eyes on the captain, a boot-time query parser to force a round
    // onto a floor that rolled none. Those are questions, and this is where the round answers them. Each
    // one names the site that asked for it, so the day a member has no asker left it can be deleted rather
    // than inherited.
    //
    // FIFTEEN OF THEM ARE ONE-LINE FORWARDERS ONTO <see cref="Patrol"/>, in the block at the bottom of this
    // page. They are here because all fifteen are already asked for by name from outside the family and 6′b
    // is not the lane that rewrites those callers; 6′c is, and it deletes the block whole.

    /// <summary>#870 lane 6′a · IS ANYBODY ON THE ROTA WATCHING THE CAPTAIN RIGHT NOW? Asked by
    /// <c>Map.Bin.cs</c>, which needs the top rung of <see cref="RipAndBin.WhoSaw"/>'s ladder: tearing
    /// something up in front of a round is not the same as tearing it up alone.
    ///
    /// <para>It is the challenge's own predicate (<see cref="PatrolBeat.Notices"/>), over the caller's own
    /// sight blockers, INCLUDING the grace off the car (<see cref="PatrolBeat.CanBeNoticed"/>) — a guard who
    /// has not registered you yet has not seen you do anything. Both halves live here so that a second
    /// caller cannot take one of them and miss the other.</para></summary>
    private bool TheRoundHasEyesOnYou(IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        bool seen = false;
        if (PatrolBeat.CanBeNoticed(_patrol.FloorSeconds))
        {
            foreach (Guard g in _patrol.Guards)
            {
                seen |= PatrolBeat.Notices(g.X, g.Y, _avatarX, _avatarY, sight);
            }
        }
        return seen;
    }

    /// <summary>How long between mentions of the boots. Long enough that it is an event; short enough that
    /// a captain who has walked away and come back is told again.</summary>
    private const double HeardAgainSeconds = 20.0;

    /// <summary>How many figures a patrol may need drawing. The band in the droid buffer, stated once.</summary>
    private const int PatrolBand = PatrolBeat.MostOnAFloor;

    // ── PUTTING THEM ON THE FLOOR ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Build the round for the floor the captain has just arrived on, or clear it if this floor has none.
    /// Called from the lift ride — the one place a floor changes — and never from the deck rebuild, which
    /// happens every time a room is searched and would restart the round under a captain who was timing it.
    /// </summary>
    private void SpawnPatrolFor(SurfaceExcursion ex)
    {
        _patrol.Guards.Clear();
        _patrol.Beat.Clear();
        _patrol.Readables.Clear();
        _patrol.FloorSeconds = 0;
        _patrol.HeardAgo = HeardAgainSeconds;

        // #833 · …and the walk back dies with the floor it was being walked on. The car IS the destination,
        // so a captain who has ridden it is a captain the escort is over for — and an escort holding a guard
        // off a list that has just been cleared would hold the controls forever.
        _patrol.Escort = null;
        _patrol.EscortDue = null;
        _patrol.EscortSeconds = 0;
        _patrol.EscortSaidPumps = false;

        // #835 · …and so does the run and the ride it was owed. A captain who got into the car mid-run has
        // ESCAPED — rung five, and the honest one — so the man who was coming is left standing on a floor
        // this page is no longer simulating. The ride owed is cleared for the same reason it is cleared at
        // the top of TheKickOut: it has either just happened or it never will.
        _patrol.KickOutDue = false;
        _patrol.KickOutRideDue = false;

        // #836 · …and so does the paper in your hand. A hand goes to a pocket for a man who is walking over,
        // and the man is a floor above now. The BOOK is not cleared here — that is the durable half, and a
        // captain who has ridden one floor has not forgotten which name worked downstairs.
        _patrol.WalletFanOpen = false;
        _patrol.PaperInHand = null;

        // #821 · …and so does the hide. A floor change is a new set of doors and a new set of men, and a
        // "he walked past" line kept from the floor above would be a beat about a room nobody is in.
        //
        // The CATCHES go with it, and the reason is in the field's own doc: a catch is a thing a hand is
        // holding shut, and the hand has just ridden the lift. Today it cannot happen — the only way out of
        // a shut cubicle is to turn the catch back — but a door left OCCUPIED on a floor nobody is standing
        // on would be a room the building had sealed against itself, forever, with nothing to say why. The
        // set is the excursion's rather than the vault's for the same reason (see SurfaceExcursion).
        _patrol.WalkedPastSaid = false;
        ex.CubiclesShut.Clear();

        string bodyId = ex.Stop.Body.Id;
        int level = ex.Floor;

        // #835 · THE WATCH'S OWN MEMORY, turned over with the shift and with nothing else. Asked before the
        // patrolled-floor gate below, because a captain who has been thrown out and has come back down to a
        // floor with nobody on it is still the same evening.
        if (_patrol.Watch != ex.CanteenWatch)
        {
            _patrol.Watch = ex.CanteenWatch;
            _patrol.EscortsThisWatch = 0;
            _patrol.WalkedAwayThisWatch = 0;
        }

        if (!PatrolBeat.IsPatrolled(bodyId, level))
        {
            return;
        }

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(bodyId, level, field);

        _patrol.Beat.AddRange(PatrolBeat.BeatFor(bodyId, level, ex.CanteenWatch, floor, field));
        if (_patrol.Beat.Count < 2)
        {
            // A floor whose plan yields nothing to walk gets nobody. It is not a failure worth a sentence —
            // an empty corridor is what this building is mostly made of — but it must not put a guard on a
            // round with one stop in it, standing still forever at the car.
            _patrol.Beat.Clear();
            return;
        }

        // #831 · …and everything on this floor's walls a held man could be reading. Off Core, once, with the
        // round it belongs to.
        _patrol.Readables.AddRange(PatrolBeat.ReadablesOn(floor, field));

        int heads = _patrol.RoundsCheat is { } forced
            ? System.Math.Clamp(forced, 0, PatrolBeat.MostOnAFloor)
            : PatrolBeat.GuardsOn(bodyId, level, ex.CanteenWatch);

        for (int i = 0; i < heads; i++)
        {
            int leg = PatrolBeat.StartLeg(_patrol.Beat.Count, i, System.Math.Max(1, heads));
            PatrolBeat.Stop at = _patrol.Beat[leg];
            _patrol.Guards.Add(new Guard
            {
                DeckName = PatrolBeat.DeckName(i),
                Plate = PatrolBeat.PlateOf(bodyId, level, ex.CanteenWatch, i),
                X = at.X,
                Y = at.Y,
                Leg = (leg + 1) % _patrol.Beat.Count,
                Standing = PatrolBeat.StandSeconds,

                // #831 · A man is put on the floor already signing the station he is standing at, looking at
                // it. The first thing a captain stepping off the car sees is somebody doing something.
                Facing = at.Point is { } start ? start.Facing : 0,
                SignedPoint = at.Point is { } signed ? signed.Number : 0,
            });
        }
    }

    // ── THE LOOP ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Walk them, decide what each side can know about the other, and let a sighting raise the
    /// card. Called once a frame from <c>StepSurface</c>.</summary>
    private void AdvancePatrol(double dtRealSeconds)
    {
        // #835 · …and the one clause that runs where nothing else here does. The KICKED OUT plate is painted
        // on the SURFACE, which is the one place this file has no guards, no beat and no floor — so its clock
        // is above the gate rather than behind it.
        FadeTheKickedOutPlate(dtRealSeconds);

        if (_patrol.Guards.Count == 0 || _surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }

        double dt = System.Math.Min(dtRealSeconds, MaxSurfaceStepSeconds);
        _patrol.FloorSeconds += dt;
        _patrol.HeardAgo += dt;

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();

        // #833 · THE CARD HAS COME DOWN, SO THE WALK BEGINS. Asked here rather than wired into
        // CloseViewObject because this is the one place that runs every frame of every floor: whichever road
        // out of the card the captain took — Esc, Enter, E, the backdrop, Close — the walk starts on the
        // first frame after it, and there is no fifth road that could miss it.
        if (_patrol.EscortDue is { } due && _viewObject is null)
        {
            _patrol.EscortDue = null;
            BeginTheWalkBack(due, walls);
        }

        // #835 · …and the same clause for the rung above it. The ride up is armed at the car and taken here,
        // on a frame of its own: the ride empties the list of guards this method is about to walk, and a loop
        // that cleared its own collection mid-iteration is a bug looking for a rare afternoon.
        if (_patrol.KickOutRideDue && _viewObject is null)
        {
            _patrol.KickOutRideDue = false;
            TheKickOut(ex);
            return;
        }

        // #793 · DOES ANYBODY HAVE TO STOP BECAUSE THE CAPTAIN DID? Owner, on the bench: "it is a good
        // gumshoe move to see if anyone is following us by foot, as they would need to stop moving also."
        // The question is asked of Core once a frame, of every mover, and the answer today is always no —
        // a round is a published route (PatrolBeat.OnTheRound) and a published route is not a tail. This is
        // the SEAM: the hold lives on the stepper every mover already goes through, so nothing has to be
        // rebuilt the day something on this floor is actually following somebody.
        bool sitting = SeatedOnABenchInTheOpen;

        // ── #821 · IS THE CAPTAIN SHUT INTO A CUBICLE? ─────────────────────────────────────────────────
        //
        // Asked ONCE a frame and handed to everything below, so the door that is drawn shut, the door the
        // round cannot see through and the door the exposure ladder calls private are one door.
        //
        // THE SENTRY'S OWN LAW LIVES IN THE TWO BRANCHES IT OPENS, and in nothing else: a guard who watched
        // the catch go over stands outside it (WaitAtTheDoor); a guard who did not walks his beat past an
        // OCCUPIED plate without breaking stride. IT BUYS TIME, NOT SAFETY — there is no branch here in
        // which a locked door ends a challenge, and CubicleLock.OpensALockedCubicle is a constant false
        // rather than a rule, so no future edit can quietly make one.
        (RingOffice.Stall Cell, string Key)? hide = TheCubicleTheCaptainIsShutIn(ex);

        bool anythingHeard = false;
        for (int i = 0; i < _patrol.Guards.Count; i++)
        {
            Guard g = _patrol.Guards[i];
            g.SinceStop += dt;

            // #831 · One answer per frame about whether he is performing a cover act, written below by the
            // hold and by nothing else — a man on his round is not covering for anything.
            g.CoverPoint = 0;

            FootTail.Mover afoot = PatrolBeat.OnTheRound(i, g.X, g.Y);
            g.Held = FootTail.MustHold(sitting, _avatarX, _avatarY, in afoot, sight);
            if (!g.Held)
            {
                // #831 · The hold is over, so whatever he had decided to read is over with it. Cleared HERE,
                // off the law's own answer, rather than in each of the branches below that walk him away.
                g.CoverAt = null;
                g.CoverFor = 0;
            }
            TheOneThingHeIsDoingThisFrame(ex, g, i, dt, walls, sight, hide);

            // WHAT THE CAPTAIN MAY KNOW. One call, one answer, used by the marker and by nothing else — so
            // a guard behind a wall is off the deck by construction rather than by a renderer's opinion.
            // #832 · …and it is now a THREE-rung answer, because the eye's edge is not a cliff: the far
            // fifth of the reach is a distant figure with no round number on it, and only past the whole
            // reach — or behind a wall — is there nothing at all.
            g.Seen = PatrolBeat.SightingFor(_avatarX, _avatarY, g.X, g.Y, sight);
            anythingHeard |= PatrolBeat.Heard(_avatarX, _avatarY, g.X, g.Y, sight);
        }

        // …and the ear, once, cooled. It is deliberately said only for somebody the captain CANNOT see: a
        // line about boots over a marker you are looking at is the picture and the sentence disagreeing.
        if (anythingHeard && _patrol.HeardAgo >= HeardAgainSeconds)
        {
            _patrol.HeardAgo = 0;
            ShowPulseMessage(PatrolBeat.HeardLine);
            LogAutopilotEvent(PatrolBeat.HeardLine);
        }

        // #821 · A SHUT DOOR IS NOT A DISGUISE, IT IS A WALL. Nobody new registers the captain while it is
        // over — not because the door hides them, but because there is a partition between the two of them
        // and PatrolBeat.Notices is a sightline question. Asked here, once, rather than inside the loop: a
        // hail raised on the frame the catch turned would be a man challenging a door.
        if (hide is null)
        {
            StopTheRoundIfAnybodySeesYou(sight);
        }
        else
        {
            TheRoundWalkedPast();
        }
    }

    // ── #870 · THE ONE THING HE IS DOING, AND THE ORDER THAT DECIDES IT ───────────────────────────────

    /// <summary>
    /// #870 · WHAT THIS MAN IS DOING THIS FRAME — exactly one of seven things, and the ORDER IS THE FEATURE.
    ///
    /// <para>Every arm below was written into one <c>if / else if</c> chain by the six issues that built this
    /// file, and the chain's order carried five separate rulings that were named nowhere. It is a list now,
    /// read top to bottom, and the first arm that will have him takes him. Each arm's own docblock says what
    /// it is; this is the priority, and WHY it is this priority:</para>
    ///
    /// <list type="number">
    /// <item><b>He is walking you out</b> (#833) — an escort in progress is not a thing a bench can stop and
    /// not a thing a sighting can interrupt. He is not on a round at all, so nothing below applies to him.</item>
    /// <item><b>He is waiting outside the door he saw you shut</b> (#821) — ABOVE the run, and that is the
    /// whole of what a locked door is worth: a man coming at a run cannot run through a partition.</item>
    /// <item><b>He is coming after you</b> (#835) — above everything a round does, because a man who has said
    /// your floor into a radio has stopped doing his rounds.</item>
    /// <item><b>He has lost you to a door</b> (#821/#835) — only ever reached by a man who did NOT watch the
    /// catch turn, because the man who did is two arms up, knocking.</item>
    /// <item><b>He is made and covering for it</b> (#793/#831) — the hold, which is asked once a frame of
    /// every mover in <see cref="AdvancePatrol"/> and spent here.</item>
    /// <item><b>He is crossing the floor to you</b> (#833) — a hail is a detour from the round, so it sits
    /// under everything that is not the round and over the round itself.</item>
    /// <item>…and if nobody wants anything of him, <b>he walks his round</b>. The default, the ordinary case,
    /// and the one the whole floor is mostly made of.</item>
    /// </list>
    ///
    /// <para><b>Nothing here decides anything.</b> Every arm keeps the guard clause it was written with, in
    /// the order it was written in; the arms return whether they took him, and this reads as the list. The
    /// #870 lane that made it a list pinned every frame of twelve rounds first
    /// (<c>EveryRoundFingerprintsTheSameTests</c>) and reproduced every hash after — including a RED run with
    /// two of these lines swapped, which is the proof that the order below is load-bearing rather than
    /// decorative.</para>
    /// </summary>
    private void TheOneThingHeIsDoingThisFrame(
        SurfaceExcursion ex, Guard g, int index, double dt,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        IReadOnlyList<SurfaceCollision.Segment> sight,
        (RingOffice.Stall Cell, string Key)? hide)
    {
        if (HeIsWalkingYouOut(g, dt, walls)) { return; }
        if (HeIsWaitingOutsideTheDoorHeSawYouShut(g, dt, walls, hide)) { return; }
        if (HeIsComingAfterYou(ex, g, dt, walls)) { return; }
        if (HeHasLostYouToADoor(g, index, hide)) { return; }
        if (HeIsMadeAndCoveringForIt(g, dt, walls, sight)) { return; }
        if (HeIsCrossingTheFloorToYou(ex, g, index, dt, walls)) { return; }

        WalkTheRound(g, dt, walls);
    }

    /// <summary>#833 · THE ESCORT. The one guard on the floor who is not walking a round, walking the captain
    /// off it at his shoulder.</summary>
    private bool HeIsWalkingYouOut(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (!ReferenceEquals(g, _patrol.Escort))
        {
            return false;
        }

        // #833 · The one guard who is not walking a round at all. He is ahead of everything else in
        // this loop because an escort in progress is not a thing a bench can stop and not a thing a
        // sighting can interrupt: the captain is at his shoulder, and there is nothing left to see.
        g.Held = false;
        WalkTheEscort(g, dt, walls);
        return true;
    }

    /// <summary>#821 · THE KNOCK. He watched the catch go over, so he is standing outside it — and standing
    /// outside it is the whole of what he does.</summary>
    private bool HeIsWaitingOutsideTheDoorHeSawYouShut(
        Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls,
        (RingOffice.Stall Cell, string Key)? hide)
    {
        if (hide is not { } shut || !CubicleLock.WaitsAtTheDoor(g.SawYouShutIt))
        {
            return false;
        }

        // #821 · He watched the catch go over. He does not open it — nothing in this game does
        // (CubicleLock.OpensALockedCubicle) — he walks over, knocks once, and waits.
        //
        // ABOVE #835's RUN, and that is the whole of what the door is worth. A man coming at a run
        // cannot run through a partition: he arrives, and then he is a man standing outside a door.
        // What he is NOT is finished — he keeps AfterYou and he keeps his reason, so opening the
        // door gives the captain back the exact rung of the ladder they ducked out of rather than a
        // softer one. IT BUYS TIME, NOT SAFETY, and this branch is where that sentence is spent.
        g.Held = false;
        WaitOutsideTheCubicle(g, dt, walls, in shut.Cell);
        return true;
    }

    /// <summary>#835 · THE RUN. He has called it in and he is coming — the earned branch, and never the
    /// ambient one.</summary>
    private bool HeIsComingAfterYou(
        SurfaceExcursion ex, Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (!g.AfterYou)
        {
            return false;
        }

        // #835 · The other man who is not walking a round. He is held off the bench law for the
        // escort's own reason and one of his own: #793's hold is a law about a TAIL — something
        // following you covertly, which has to stop when you stop or the trick is up. A man who has
        // said your floor and your direction into a radio is not being covert about anything.
        //
        // #821 · A run that did NOT see the catch turn carries on to where you were, which is what
        // losing somebody looks like, and #835's own cap ends it. A locked door is not a smoke bomb.
        g.Held = false;
        RunAfterTheCaptain(ex, g, dt, walls);
        return true;
    }

    /// <summary>#821 · THE EMPTY WASHROOM. He was crossing the floor to somebody who is behind a door now,
    /// and that is the ground ending his hail rather than the captain doing it.</summary>
    private bool HeHasLostYouToADoor(Guard g, int index, (RingOffice.Stall Cell, string Key)? hide)
    {
        if (hide is null || !g.WalkingUp)
        {
            return false;
        }

        // …and a man who was crossing the floor to somebody who is now behind a door has lost them.
        //
        // #835 · NOT BOOKED AS WALKING OFF, and the distinction is exact rather than generous. This
        // branch is only ever reached by a guard who did NOT see the catch turn — the man who did is
        // two branches up, knocking — so what happened is that somebody came round a corner into an
        // empty washroom. That is the GROUND ending it, and #835's own rule is that a refusal by the
        // ground may never be booked against the man standing in front of it. The captain who ducks
        // in where he can see them does not get this branch at all; they get the knock, which is the
        // whole of what the door was ever worth.
        GiveUpTheHail(g, index, walkedAway: false);
        g.Held = false;
        return true;
    }

    /// <summary>#793/#831 · THE COVER ACT. A made tail that has been stopped by a captain sitting down, doing
    /// something plausible about it.</summary>
    private bool HeIsMadeAndCoveringForIt(
        Guard g, double dt,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        if (!g.Held)
        {
            return false;
        }

        // A tail that has been made cannot walk on past you. It stops where it stopped, and it drops
        // off the motion fan honestly while it does — the same clause the stand at a stop keeps.
        //
        // #831 · …and it stops AT SOMETHING. The rule is untouched; the picture is not a statue.
        TheCoverAct(g, dt, walls, sight);
        return true;
    }

    /// <summary>#833 · THE APPROACH. He has said hold on, and he is walking over to say the rest of it to
    /// your face.</summary>
    private bool HeIsCrossingTheFloorToYou(
        SurfaceExcursion ex, Guard g, int index, double dt,
        IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (!g.WalkingUp)
        {
            return false;
        }

        WalkUpToTheCaptain(ex, g, index, dt, walls);
        return true;
    }

    // ── #870 lane 6′b · THE FIFTEEN FORWARDERS, AND WHEN THEY GO ────────────────────────────
    //
    // Every one of the fifteen questions the round answers moved onto Patrol whole; every one of them is
    // ALSO called by name from a file outside this family (measured, one caller at a time — the table is in
    // the PR body). So each keeps its old spelling and its old accessibility here, which is what proves
    // nothing outside the family gained a reach it did not have. 6′c deletes this block and points those
    // callers at the host surface instead; the guard's own fact names that as the clause to relax, rather
    // than leaving a row to be quietly deleted to make a sweep go green.

    /// <inheritdoc cref="Patrol.CaptainIsUnderEscort"/>
    private bool CaptainIsUnderEscort => _patrol.CaptainIsUnderEscort;

    /// <inheritdoc cref="Patrol.TheRoundOnFoot"/>
    private IReadOnlyList<Guard> TheRoundOnFoot => _patrol.TheRoundOnFoot;

    /// <inheritdoc cref="Patrol.ForceTheRoundsTo"/>
    private void ForceTheRoundsTo(int rounds) => _patrol.ForceTheRoundsTo(rounds);

    /// <inheritdoc cref="Patrol.TheQueryHasForcedARound"/>
    private bool TheQueryHasForcedARound => _patrol.TheQueryHasForcedARound;

    /// <inheritdoc cref="Patrol.ForceARoundIfNoneAsked"/>
    private void ForceARoundIfNoneAsked() => _patrol.ForceARoundIfNoneAsked();

    /// <inheritdoc cref="Patrol.MintTheSitePassAtTheLanding"/>
    private void MintTheSitePassAtTheLanding() => _patrol.MintTheSitePassAtTheLanding();

    /// <inheritdoc cref="Patrol.TheSitePassIsMintedAtTheLanding"/>
    private bool TheSitePassIsMintedAtTheLanding => _patrol.TheSitePassIsMintedAtTheLanding;

    /// <inheritdoc cref="Patrol.TheNextHideGetsItsOwnLine"/>
    private void TheNextHideGetsItsOwnLine() => _patrol.TheNextHideGetsItsOwnLine();

    /// <inheritdoc cref="Patrol.EverybodyForgetsTheCatch"/>
    private Guard? EverybodyForgetsTheCatch() => _patrol.EverybodyForgetsTheCatch();

    /// <inheritdoc cref="Patrol.ThePaperInYourHandIs"/>
    private bool ThePaperInYourHandIs(Satchel.Item paper) => _patrol.ThePaperInYourHandIs(paper);

    /// <inheritdoc cref="Patrol.TheBookOn"/>
    private string TheBookOn(Satchel.Item paper, string bodyId) => _patrol.TheBookOn(paper, bodyId);

    /// <inheritdoc cref="Patrol.YourPaperTrail"/>
    private IReadOnlyList<WalletChoice.Shown> YourPaperTrail => _patrol.YourPaperTrail;

    /// <inheritdoc cref="Patrol.ForgetThePaperTrail"/>
    private void ForgetThePaperTrail() => _patrol.ForgetThePaperTrail();

    /// <inheritdoc cref="Patrol.RestoreAPaperTrailRow"/>
    private void RestoreAPaperTrailRow(WalletChoice.Shown row) => _patrol.RestoreAPaperTrailRow(row);

    /// <inheritdoc cref="Patrol.CloseTheWalletFan"/>
    private void CloseTheWalletFan() => _patrol.CloseTheWalletFan();
}
