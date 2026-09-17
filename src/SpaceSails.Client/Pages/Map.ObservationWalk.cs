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
    /// turn and wait, and they start walking again the moment the captain is no longer behind them. A tail
    /// that was noticed still ends at the rail; what it does not end in is a vanishing, because the game
    /// never takes a body off the floor that the captain is looking at.</para>
    /// </summary>
    /// <returns>Whether anything happened that the page should redraw for.</returns>
    private bool StepThePersonOfInterest(
        Walker who, double dt, IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        bool clearLine = SurfaceCollision.HasLineOfSight(
            _avatarX, _avatarY, who.Walk.X, who.Walk.Y, walls);
        double dx = who.Walk.X - _avatarX, dy = who.Walk.Y - _avatarY;
        double rangeDu = System.Math.Sqrt((dx * dx) + (dy * dy));

        // ── THE NOTICE QUESTION, and it is #436's eye with a person in front of it ───────────────────────
        if (!_walkNoticed)
        {
            ReeverObservation.Glance glance = TheTail.Notice(
                clearLine && rangeDu <= FootTail.LegibleDu,
                alreadyNoticed: false,
                TheTail.SeedFor(_walkBerth ?? "", who.Who),
                SurfaceSeconds,
                _walkLookIndex,
                rangeDu,
                _captainSpeedDu);

            _walkLookIndex = glance.LookIndex;
            _walkNoticed = TheTail.HasNoticed(in glance);
        }

        // ── STOPPED, TURNED, WAITING FOR YOU TO GO PAST ──────────────────────────────────────────────────
        bool holding = _walkNoticed && clearLine && rangeDu <= FootTail.LegibleDu;
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
        // The route has run out: they are at the rail, at the blind end of a lit tube with one way in. They
        // come off the floor on the first frame the captain has NO line to them — never on a frame he is
        // looking, because a body that blinks out in plain sight is a bug and the horror here is that it is
        // not one. If he watches the whole way, they are simply standing at the rail when he arrives, and
        // the beat is not spent. That is a legitimate ending and nothing is said about it either.
        if (clearLine)
        {
            who.Walk.LookTowards(_avatarX, _avatarY);
            return false;
        }

        _barAfoot.RemoveAt(slot);
        _walkGoneSince = SimTime;
        return true;
    }

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
