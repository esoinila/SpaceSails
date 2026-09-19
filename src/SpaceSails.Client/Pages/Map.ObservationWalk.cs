using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// #1199 / #1062 slice 1 · THE TAIL, AND THE WALK AT THE END OF IT.
//
// Owner, 2026-09-13, verbatim: "Suppose we shadow somebody into a dead end and once we get there there is
// nothing there — even better if we preclude the cliché hidden door by having that place be like an
// observation tube (a Grand Canyon walk on top of the cliff with a transparent floor) in some space station,
// with only one entry / exit, and somebody we tail vanishes there. So we know they went in, and after a wait
// we wonder and go see, and nothing there. Those moments are narratively great."
//
// WHAT IS HERE. One frame of a person of interest crossing the concourse with the captain behind them, the
// notice question read over their shoulder, the moment they are no longer anywhere, the card at the blind end
// and the note under their name — plus the one later sighting at a counter somewhere else.
//
// WHAT IS DELIBERATELY NOT. No pathfinder (OnFoot, the one planner). No roll (TheTail asks #436's eye). No
// sightline (SurfaceCollision.HasLineOfSight, the one oracle, over the deck's own stone). No prose (Core's,
// authored, verbatim). No new constant for the wait (Escort.PatienceSeconds — the game's one statement of
// how long you go on expecting somebody through a doorway).
//
// AND NOTHING IS ANNOUNCED. The tail says nothing when it is noticed and says nothing when it is not. The
// vanishing says nothing — a body simply stops being on the floor, on a frame when nobody was looking at it.
// The only thing this file raises is one card at the far end of an empty room, and the card is the absence.
public partial class Map
{
    // ── THE FACTS THAT RIDE THE SAVE ─────────────────────────────────────────────────────────────────────

    /// <summary>#1199 · The one person this captain has followed onto an observation walk and not found
    /// (<see cref="ObservationWalk.Key"/>), or null while the beat is still unspent — which is most of every
    /// voyage. Goes from null to a key, once, and never back.</summary>
    private string? _observationWalkSpentOn;

    /// <summary>#1199 · …and where the world handed them back, or null while the one later sighting is still
    /// owed. Two written facts and not one parsed one, for the reason
    /// <see cref="ObservationWalk.SightingIsOwed"/> states.</summary>
    private string? _observationWalkSightingAt;

    // ── THE FACTS THAT DO NOT ────────────────────────────────────────────────────────────────────────────
    //
    // A visit's own state, exactly like the bar's feet: a different berth is a different room, and a tail
    // carried across a casting-off would be a body walking through a station it was never in.

    /// <summary>#1062 · Which berth this tail's state belongs to. Null is a room that has never had one.</summary>
    private string? _walkBerth;

    /// <summary>#1062 · The look index this person carried out of their last notice question — the "have they
    /// looked yet" half of <see cref="ReeverObservation.LookIndexAt"/>'s cadence, so one look is taken once
    /// however many frames it spans.</summary>
    private long _walkLookIndex = long.MinValue;

    /// <summary>#1062 · Have they clocked the captain on this walk? One-way for the walk, exactly as #436's
    /// own latch is one-way for an excursion — somebody who has seen you over their shoulder does not
    /// un-see you by arithmetic on the next frame.</summary>
    private bool _walkNoticed;

    /// <summary>#1199 · Whether they are no longer on the floor, and the sim second they stopped being on
    /// it — which is the second the captain last had eyes on them, and therefore the second the wait starts
    /// counting from. NaN is "still somewhere".</summary>
    private double _walkGoneSince = double.NaN;

    /// <summary>#1199 · Whether this visit has already put them on the floor, so a walk the room refused is
    /// not retried sixty times a second and a person who has set off once does not set off again.</summary>
    private bool _walkDealt;

    /// <summary>#1199 · The sim second they stopped at the THROAT because the captain was on their heels, or
    /// NaN when they are not holding there. It is the tail's own refusal — he will not walk into a blind room
    /// with somebody two paces behind him — and it is what <see cref="ObservationWalk.TheWaitSeconds"/> is
    /// counted from before he turns round and leaves.</summary>
    private double _walkHeldAtTheThroatSince = double.NaN;

    /// <summary>#1199 · Is he on his way back OUT — the leg he walks after being tailed too close to go in?
    /// The errand stays <c>LettingYouPass</c> on that leg (a man who has given up his view and is leaving IS
    /// letting you past him, and a new <c>Errand</c> member would join four sweeps to say what one bool says),
    /// so this is the one fact that tells the two legs apart — and it is what stops the return leg
    /// re-triggering the vanish on its way back out through the throat.</summary>
    private bool _walkTurnedBack;

    // ── ONE FRAME ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · One frame of the tail. Called from <c>AdvanceBarWalkers</c> beside the room's own metabolism,
    /// and it does nothing at all at every berth in the game but one.
    ///
    /// <para>Four refusals before anything happens, in the order that costs least: this station has no walk;
    /// the beat is already spent (afterwards the walk is a walk and the person keeps walking routes, never
    /// tailed to a vanishing again); the rota does not have them in this room this watch; they are already
    /// afoot. Each is a plain no, and none of them is said out loud.</para>
    /// </summary>
    private void AdvanceTheWalk(in HavenInterior.BarFloor bar)
    {
        if (!HavenInterior.HasObservationWalk(bar.BodyId)
            || ObservationWalk.IsSpent(_observationWalkSpentOn))
        {
            return;
        }

        string person = TheTail.ThePersonOfInterest(bar.BodyId);
        if (PatronRota.Resolve(person, bar.BodyId, _dockVisitSimTime) != PatronState.AtBar)
        {
            return;   // not in the room this watch. Nobody to follow, and nothing to say about it.
        }

        if (!_walkDealt)
        {
            // ── #731 · THE ROOM'S OWN HOURS BIND THIS TOO ───────────────────────────────────────────────
            //
            // Nobody gets out of a chair in this bar before the shift says so — a room that empties itself on
            // frame one has no hours, it has a leak, and that law is enforced over every berth and watch in
            // the game. The tail was breaking it: it stood a regular up forty seconds into a watch whose
            // first scheduled departure was two hours away, and the sweep caught it before a player could.
            //
            // So the walk is walked AFTER LAST CALL — past Egress.LastCallFraction of the watch, which is the
            // room's own statement of the point after which nothing is scheduled to happen any more. No new
            // constant, and it is the better beat: they go out to look at the view when the evening is over
            // and the room is done with them, which is when a person actually would.
            if (IntoTheBarsWatch <= PatronRota.WatchSeconds * Egress.LastCallFraction)
            {
                return;
            }

            SendThemOutOntoTheWalk(in bar, person);
            return;
        }

        TheWalkIsEmpty(in bar, person);
    }

    /// <summary>#1062 · CASTING OFF IS THE ROOM FORGETTING, here as everywhere else on this deck. Called from
    /// <c>ForgetTheBarsFeet</c>, which is the one place that knows a berth has changed — a second opinion
    /// about which room the captain is in is this repository's oldest bug class with somebody's shadow in
    /// it.</summary>
    private void ForgetTheWalk(string? berth)
    {
        if (_walkBerth == berth)
        {
            return;
        }

        _walkBerth = berth;
        _walkLookIndex = long.MinValue;
        _walkNoticed = false;
        _walkGoneSince = double.NaN;
        _walkDealt = false;
        _walkHeldAtTheThroatSince = double.NaN;
        _walkTurnedBack = false;
        ForgetTheGallerysMachines();
    }

    // ── THE ROUTE ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 slice 1 · <b>THE FIRST ROUTE ENDS AT THE WALK.</b> They get up from the top the rota seated them
    /// at and cross the concourse to the rail, on the one planner, with no second pathfinder anywhere near
    /// it.
    ///
    /// <para>The room is told they have gone (<c>_barLeft</c>) on the frame their legs start, which is the
    /// bar's own one-body-one-place law: a regular who is walking is not also sitting in a chair. And the
    /// walk is dealt ONCE per visit whether or not it could be plotted — a route the floor refuses is a
    /// refusal, not a reason to ask again next frame, which is the lesson #731's schedule paid for.</para>
    /// </summary>
    private void SendThemOutOntoTheWalk(in HavenInterior.BarFloor bar, string person)
    {
        if (_barAfoot.Count >= WalkerBand)
        {
            // A full band is NOT NOW rather than NO — the room's own leavers hold slots for a few seconds at
            // a time and then give them back. Marking the walk dealt here would let a busy instant cancel the
            // whole beat for the visit, which is the opposite of what dealing-once is for.
            return;
        }

        _walkDealt = true;

        if (HavenInterior.TheRailAt(bar.BodyId) is not { } rail)
        {
            return;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        IReadOnlyList<HavenInterior.SeatedRegular> rota =
            HavenInterior.ResolveRegulars(bar.BodyId, _dockVisitSimTime, TheBarsChurn);

        foreach (HavenInterior.SeatedRegular seated in rota)
        {
            if (!seated.Present || !string.Equals(seated.Id, person, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (BesideThisTop(new DeckReachability.Point(seated.X, seated.Y), walls) is not { } from
                || OnFoot(seated.ShortName, new NpcWalk.Bound("", rail.X, rail.Y), from, walls) is not { } walk)
            {
                return;   // no standing room beside their chair, or no route. Nothing happens.
            }

            _barLeft.Add(seated.Id);
            _barAfoot.Add(new Walker
            {
                Walk = walk, Table = -1, For = Errand.WalkingTheRoute, Who = seated.Id,
            });
            RebuildDockedDeck();
            StateHasChanged();
            return;
        }
    }

    /// <summary>
    /// #1062 slice 1 · <b>ONE FRAME OF SOMEBODY BEING FOLLOWED.</b> The notice question first, then the legs,
    /// then the moment there is nobody there.
    ///
    /// <para>The question is asked only when the walls leave them something to see and the captain is inside
    /// the range at which a body on a deck is legible at all (<see cref="FootTail.LegibleDu"/> — the game's
    /// own number for that, borrowed rather than re-typed). Geometry is permission to roll and never
    /// knowledge, which is #436's law and is why a corner is worth taking.</para>
    ///
    /// <para><b>Noticed and they stop.</b> No card, no line, no facing change anybody announces — they simply
    /// turn and wait, and they start walking again the moment the captain is no longer behind them.</para>
    ///
    /// <h3>#1199 · THE END OF THE ROUTE IS A BAND NOW, AND THE VANISH HAPPENS AT THE THROAT</h3>
    ///
    /// <para><b>What was played</b> (inspector, live on Selene Gate, 2026-09-18): the person of interest
    /// reached the far end and <i>held there for as long as the captain was anywhere behind him</i> — and
    /// after #1237 gave the walk its crossbar, for as long as he was anywhere in a 24 × 8 glass gallery. The
    /// vanish never happened, so the wait never started, so the card was unreachable by standing anywhere a
    /// person following somebody would stand.</para>
    ///
    /// <para><b>The EN-ROUTE hold was never the problem</b> and is untouched: it has read the band since
    /// slice 1 (<c>_walkNoticed &amp;&amp; clearLine &amp;&amp; rangeDu &lt;= FootTail.LegibleDu</c>). The
    /// branch that was line-of-sight ONLY, with no band at all, is the one below that decides whether he
    /// comes off the floor — and a guard written about the en-route rule would have been green on the
    /// shipped tree, which is this repository's own named hazard.</para>
    ///
    /// <para><b>The rule, at the throat</b> (the tube's blind end, which is the crossbar's own east face —
    /// one edge, shared by construction):</para>
    /// <list type="bullet">
    ///   <item><b>Captain outside <see cref="ObservationWalk.TooCloseToGoInDu"/> (or with no line):</b> he is
    ///   off the floor <i>there and then</i>. From down the tube, or at its mouth, the captain saw a figure
    ///   turn into the hat; he arrives, and the gallery is empty. The wait and the card run as shipped.</item>
    ///   <item><b>Inside it:</b> he holds at the throat — a man does not walk into a blind room with somebody
    ///   two paces behind him — and after <see cref="ObservationWalk.TheWaitSeconds"/> he turns round and
    ///   walks back out <i>past</i> the captain. The beat is SPENT and no card is raised: tailing too close
    ///   costs you the scene, which is the whole of what a tail can do wrong.</item>
    /// </list>
    ///
    /// <para>Nothing blinks out in plain sight at close quarters — the horror is still a body you were NOT
    /// looking closely at.</para>
    /// </summary>
    /// <returns>Whether anything happened that the page should redraw for.</returns>
    private bool StepThePersonOfInterest(
        Walker who, double dt, in HavenInterior.BarFloor bar,
        IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        bool clearLine = SurfaceCollision.HasLineOfSight(
            _avatarX, _avatarY, who.Walk.X, who.Walk.Y, walls);
        double dx = who.Walk.X - _avatarX, dy = who.Walk.Y - _avatarY;
        double rangeDu = System.Math.Sqrt((dx * dx) + (dy * dy));

        // #1199 · THE ONE READING the notice question and the en-route hold have always shared, taken once so
        // they cannot come to two opinions about how close is close.
        bool tailedTooClose = clearLine && rangeDu <= FootTail.LegibleDu;

        // ── THE NOTICE QUESTION, and it is #436's eye with a person in front of it ───────────────────────
        if (!_walkNoticed)
        {
            ReeverObservation.Glance glance = TheTail.Notice(
                tailedTooClose,
                alreadyNoticed: false,
                TheTail.SeedFor(_walkBerth ?? "", who.Who),
                SurfaceSeconds,
                _walkLookIndex,
                rangeDu,
                _captainSpeedDu);

            _walkLookIndex = glance.LookIndex;
            _walkNoticed = TheTail.HasNoticed(in glance);
        }

        // ── HE HAS GIVEN UP AND IS WALKING BACK OUT PAST YOU ─────────────────────────────────────────────
        //
        // FIRST, above every other branch. He must not hold again on the way out — a man who has already
        // decided to leave, stopping dead every time the captain is inside thirty du of him, would never get
        // out of his own tube — and he must not vanish on the way back through the throat either, which is
        // the one thing _walkTurnedBack exists to say.
        if (_walkTurnedBack)
        {
            if (who.Walk.Afoot)
            {
                who.Walk.Step(dt, walls, _avatarX, _avatarY);
                return !who.Walk.Afoot;
            }

            // He is out. The beat is spent with NO card: what standing on his heels bought the captain is the
            // absence of the scene, and a card would be the game explaining the thing it just withheld.
            TheWalkIsSpentWithNothingSaid(in bar);
            _barAfoot.RemoveAt(slot);
            return true;
        }

        // ── THE THROAT ───────────────────────────────────────────────────────────────────────────────────
        //
        // On its OWN band, and the measurement is written out on ObservationWalk.TooCloseToGoInDu: the stem
        // is 24 du and FootTail.LegibleDu is 30, so a captain at the MOUTH is inside the legibility band and
        // a throat gated on legibility would hold from everywhere a follower can stand — the reported bug,
        // re-shipped. Two paces is on his heels; the length of a corridor is a stranger in a station.
        if (HavenInterior.InTheGallery(bar.BodyId, who.Walk.X, who.Walk.Y))
        {
            return HeHasReachedTheThroat(
                who, in bar, walls, slot,
                onHisHeels: clearLine && rangeDu <= ObservationWalk.TooCloseToGoInDu);
        }

        // ── STOPPED, TURNED, WAITING FOR YOU TO GO PAST ──────────────────────────────────────────────────
        bool holding = _walkNoticed && tailedTooClose;
        if (holding)
        {
            who.Walk.LookTowards(_avatarX, _avatarY);
            if (who.For != Errand.LettingYouPass)
            {
                _barAfoot[slot] = Rebadge(who, Errand.LettingYouPass);
                return true;
            }

            return false;
        }

        // ── THEY LEAD ON ─────────────────────────────────────────────────────────────────────────────────
        if (who.For == Errand.LettingYouPass && who.Walk.Afoot)
        {
            _barAfoot[slot] = Rebadge(who, Errand.WalkingTheRoute);
        }

        if (who.Walk.Afoot)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            return !who.Walk.Afoot;
        }

        // ── AND THEN THERE IS NOBODY THERE ───────────────────────────────────────────────────────────────
        //
        // The route has run out somewhere that is NOT the hat. On the shipped walk that cannot happen — the
        // route ends at the rail and the rail is in the gallery, so the throat branch above has already had
        // this frame — and it is kept for the haven that has a walk and no crossbar at the end of it
        // (TheGalleryBox answers null there, so InTheGallery is false for every point). The old law stands
        // unchanged in that case: they come off the floor on the first frame the captain has NO line to them,
        // never on a frame he is looking, because a body that blinks out in plain sight is a bug and the
        // horror here is that it is not one.
        if (tailedTooClose)
        {
            who.Walk.LookTowards(_avatarX, _avatarY);
            return false;
        }

        _barAfoot.RemoveAt(slot);
        _walkGoneSince = SimTime;
        return true;
    }

    /// <summary>
    /// #1199 · <b>AT THE THROAT — the one frame this whole beat turns on.</b> The tube's blind end is the
    /// crossbar's east face, one edge shared by construction (#1237), so "he is in the hat" is asked of
    /// <c>HavenInterior.InTheGallery</c> and never of a coordinate typed here.
    ///
    /// <para>Outside <see cref="ObservationWalk.TooCloseToGoInDu"/> he is gone <i>there and then</i>; inside
    /// it he holds, and after <see cref="ObservationWalk.TheWaitSeconds"/> he turns round. A captain who
    /// backs off while he is holding gets the vanish on the very next frame, which is correct and is the
    /// craft: give him room and he goes in.</para>
    /// </summary>
    private bool HeHasReachedTheThroat(
        Walker who, in HavenInterior.BarFloor bar,
        IReadOnlyList<SurfaceCollision.Segment> walls, int slot, bool onHisHeels)
    {
        if (!onHisHeels)
        {
            _barAfoot.RemoveAt(slot);
            _walkGoneSince = SimTime;
            _walkHeldAtTheThroatSince = double.NaN;
            return true;
        }

        who.Walk.LookTowards(_avatarX, _avatarY);

        if (double.IsNaN(_walkHeldAtTheThroatSince))
        {
            _walkHeldAtTheThroatSince = SimTime;
            _barAfoot[slot] = Rebadge(who, Errand.LettingYouPass);
            return true;
        }

        return SimTime - _walkHeldAtTheThroatSince >= ObservationWalk.TheWaitSeconds
            && HeTurnsAndWalksBackOut(who, in bar, walls, slot);
    }

    /// <summary>
    /// #1199 · <b>HE TURNS ROUND AND LEAVES, PAST YOU.</b> The return leg is planned on the one planner
    /// (<c>OnFoot</c>) from where he is standing back to the standing room beside his own top — the exact
    /// reverse of the leg <see cref="SendThemOutOntoTheWalk"/> plotted, off the same rota and the same
    /// <c>BesideThisTop</c>, so there is no second pathfinder and no second idea of where he came from.
    ///
    /// <para>The beat is spent on the frame he gets there (or here, if the floor refuses the route): spent,
    /// and silent. A card would explain the thing the captain has just been denied.</para>
    /// </summary>
    private bool HeTurnsAndWalksBackOut(
        Walker who, in HavenInterior.BarFloor bar,
        IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        _walkHeldAtTheThroatSince = double.NaN;

        foreach (HavenInterior.SeatedRegular seated in
                 HavenInterior.ResolveRegulars(bar.BodyId, _dockVisitSimTime, TheBarsChurn))
        {
            if (!string.Equals(seated.Id, who.Who, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (BesideThisTop(new DeckReachability.Point(seated.X, seated.Y), walls) is { } home
                && OnFoot(
                       seated.ShortName, new NpcWalk.Bound("", home.X, home.Y),
                       new DeckReachability.Point(who.Walk.X, who.Walk.Y), walls) is { } back)
            {
                _walkTurnedBack = true;
                _barAfoot[slot] = new Walker
                {
                    Walk = back, Table = who.Table, For = Errand.LettingYouPass, Who = who.Who,
                    Cabinet = who.Cabinet, StillWanted = who.StillWanted, OnArrive = who.OnArrive,
                };
                return true;
            }

            break;
        }

        // The floor will not give him a way back — the same refusal SendThemOutOntoTheWalk treats as a
        // refusal rather than a reason to place a body at the far end anyway. He is simply not there any
        // more, and the beat is spent the same silent way.
        TheWalkIsSpentWithNothingSaid(in bar);
        _barAfoot.RemoveAt(slot);
        return true;
    }

    /// <summary>
    /// #1199 · <b>SPENT, AND NOTHING SAID.</b> The same one key <see cref="TheWalkIsEmpty"/> writes
    /// (<see cref="ObservationWalk.Key"/>, through the same field, so a reload can never hand the walk back)
    /// — and then nothing: no card, no note, no pulse.
    ///
    /// <para>That asymmetry is the point. The card is the ABSENCE, and a captain who stayed on the man's
    /// heels never got an absence: he got a man who turned round and went back to his drink. There is
    /// nothing to show him, because the scene did not happen.</para>
    /// </summary>
    private void TheWalkIsSpentWithNothingSaid(in HavenInterior.BarFloor bar) =>
        _observationWalkSpentOn = ObservationWalk.Key(
            bar.BodyId, TheTail.ThePersonOfInterest(bar.BodyId));

    /// <summary>#1062 · The same walker with a different errand on it. <see cref="Walker"/> is init-only
    /// everywhere that matters, so the errand changes by replacing the record rather than by a setter
    /// nothing else in this family has.</summary>
    private static Walker Rebadge(Walker who, Errand errand) => new()
    {
        Walk = who.Walk, Table = who.Table, For = errand, Who = who.Who,
        Cabinet = who.Cabinet, StillWanted = who.StillWanted, OnArrive = who.OnArrive,
    };

    // ── THE BEAT ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>THE CAPTAIN WALKS IN, AND THE WALK IS EMPTY.</b> The one card this feature raises, at the
    /// blind end, once per universe.
    ///
    /// <para>Four conditions, and every one of them is something the captain did: they are not on the floor
    /// any more; a whole <see cref="ObservationWalk.TheWaitSeconds"/> has gone by since the last moment he
    /// had eyes on them (long enough that they must have finished the view); he is inside the walk; and he
    /// has gone all the way out to the rail. Waiting and then not walking in ends nothing — the beat is the
    /// captain's to reach, which is why the room is a room first.</para>
    ///
    /// <para>The spend is written BEFORE the card goes up, so a card dismissed and a game reloaded can never
    /// hand the same walk back — and the book is written in the same breath, under the person's own name
    /// (#741), so THREADS stacks it with everything else the captain has ever written about them. The note is
    /// filed and NOT pulsed: there is a card standing in front of the HUD and a line played behind a backdrop
    /// is the bug #774 was opened for.</para>
    /// </summary>
    private void TheWalkIsEmpty(in HavenInterior.BarFloor bar, string person)
    {
        if (double.IsNaN(_walkGoneSince)
            || SimTime - _walkGoneSince < ObservationWalk.TheWaitSeconds
            || !HavenInterior.InTheObservationWalk(bar.BodyId, _avatarX, _avatarY)
            || HavenInterior.TheRailAt(bar.BodyId) is not { } rail
            || !ObservationWalk.WouldSpend(bar.BodyId, _observationWalkSpentOn))
        {
            return;
        }

        double dx = rail.X - _avatarX, dy = rail.Y - _avatarY;
        if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius)
        {
            return;   // in the walk, but not out at the end of it yet.
        }

        _observationWalkSpentOn = ObservationWalk.Key(bar.BodyId, person);
        FileNoteAbout(
            ObservationWalk.NoteLine(person), ObservationWalk.Glyph, ObservationWalk.Subjects(person));
        RaiseStoryBeat(StoryBeats.Beat.TheObservationWalk);
    }

    /// <summary>
    /// #1199 · <b>AND LATER, AT A COUNTER, THERE THEY ARE.</b> One pulse, authored, said once, and then never
    /// again for the rest of the voyage.
    ///
    /// <para>It fires where the person's own rota would have put them anyway — asked of
    /// <see cref="PatronRota.Resolve"/> at THIS watch and this body, so the world is not moved to make the
    /// beat happen and nothing about the sighting is arranged. They are at a counter because that is where
    /// they drink. The line says so and says nothing else; <i>unhurried</i> is the whole of it.</para>
    ///
    /// <para>At <see cref="PulseRank.Beat"/> rather than Status: it is plot-significant by
    /// <c>Telling.IsPlotSignificant</c>'s own floor, which is what stops it being swept away by the next
    /// routine line before the captain has read it.</para>
    /// </summary>
    private void TheyAreAtTheCounter(string bodyId)
    {
        if (!ObservationWalk.SightingIsOwed(_observationWalkSpentOn, _observationWalkSightingAt))
        {
            return;
        }

        string person = TheTail.ThePersonOfInterest(ObservationWalk.HavenId);
        if (!ObservationWalk.IsSpentOn(_observationWalkSpentOn, ObservationWalk.HavenId, person)
            || PatronRota.Resolve(person, bodyId, SimTime) != PatronState.AtBar)
        {
            return;
        }

        _observationWalkSightingAt = bodyId;
        ShowPulseMessage(ObservationWalk.CounterLine(person), PulseRank.Beat);
    }
}
