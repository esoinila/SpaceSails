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

    /// <summary>#1199 (2026-09-19) · The sim second he STOPPED AT THE RAIL and began looking out, or NaN
    /// while he is still on his legs. It is what <see cref="ObservationWalk.TheWaitSeconds"/> is counted from
    /// for the one ending in which nothing happens to him: a captain who never once takes his eyes off him
    /// gets a man who finishes the view and walks back out past him.
    ///
    /// <para>It replaces <c>_walkHeldAtTheThroatSince</c>, which timed a refusal that does not exist any more
    /// (the owner's <i>"act normal even if I tail from ahead"</i>). Same shape, opposite meaning: that field
    /// counted how long he would not go in; this one counts how long he has been standing there having gone
    /// in.</para></summary>
    private double _walkAtTheRailSince = double.NaN;

    /// <summary>#1199 (2026-09-19) · The look he carried out of his last look IN THE GALLERY — the one clock
    /// the vanish is asked on (<see cref="ReeverObservation.LookIndexAt"/>), so the question is put once per
    /// look however many frames a look spans, and so the first look asked is one the captain has had a chance
    /// to take. Its own field and not <see cref="_walkLookIndex"/>: the notice question and the vanish are two
    /// questions with two answers, and one cursor between them would let either eat the other's look.</summary>
    private long _walkGalleryLookIndex = long.MinValue;

    /// <summary>#1199 (2026-09-19) · Was the captain SITTING at one of the gallery's tables on the look he
    /// went? The one thing the card's reach is relaxed for — <see cref="TheWalkIsEmpty"/> states that rule
    /// once and this is the fact it reads.</summary>
    private bool _walkVanishedBehindThePaper;

    /// <summary>#1199 · Is he on his way back OUT — the leg he walks when the captain's eyes never once left
    /// him? The errand stays <c>LettingYouPass</c> on that leg (a man who has had his view and is leaving IS
    /// letting you past him, and a new <c>Errand</c> member would join four sweeps to say what one bool says),
    /// so this is the one fact that tells the two legs apart — and it is what stops the return leg
    /// re-triggering the vanish on its way back out through the throat.</summary>
    private bool _walkTurnedBack;

    /// <summary>#1285 · The sim second he STOOD ASIDE for this approach, or NaN while the captain is not in
    /// his way at all. The courtesy's own clock (<see cref="ObservationWalk.StandAsideSeconds"/>): while it is
    /// running he is holding the doorway, and once it has run out he leads on — and it is not re-armed until
    /// the captain has been outside the band since, which is what makes the offer one per approach rather
    /// than one per frame.</summary>
    private double _walkStoodAsideSince = double.NaN;

    // ── ONE FRAME ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · One frame of the tail. Called from <c>AdvanceBarWalkers</c> beside the room's own metabolism,
    /// and it does nothing at all at every berth in the game but one.
    ///
    /// <para>Four refusals before anything happens, in the order that costs least: this station has no walk;
    /// the beat is already spent (afterwards the walk is a walk and the person keeps walking routes, never
    /// tailed to a vanishing again); the rota does not have them in this room this watch; they are already
    /// afoot. Each is a plain no, and none of them is said out loud.</para>
    ///
    /// <para>#1277 · The first three are <see cref="TheManTheWalkHasClaimed"/> now, asked here in the same
    /// order for the same cost. They have a NAME because the room's own hours read them too: the tail begins
    /// where the claim begins, and the hours' departure roster defers to whoever it names.</para>
    /// </summary>
    private void AdvanceTheWalk(in HavenInterior.BarFloor bar)
    {
        if (TheManTheWalkHasClaimed(bar.BodyId) is not { } person)
        {
            return;
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
            //
            // #1253 slice 2 · …and he gets up IN THE BAR, which is a floor now. A man does not finish his
            // drink in a service corridor, so a captain standing in one while the watch turns over does not
            // start the evening early by being there.
            if (!OnTheConcourse
                || IntoTheBarsWatch <= PatronRota.WatchSeconds * Egress.LastCallFraction)
            {
                return;
            }

            // #1253 slice 2 · THE ROUTE IS FIVE LEGS AT A STATION WITH A FLOOR UNDER IT, and one at a
            // station without one. Both roads go through BeginHisNight, which is where that fork is written
            // down — a one-floor haven's night is the shipped walk, to the byte — and it is that method
            // which marks the night dealt, on the frame it actually opens a leg. A full band is NOT NOW
            // rather than NO, exactly as the shipped deal reads one.
            BeginHisNight(in bar, person);
            return;
        }

        // ── #1253 slice 2 · HIS NIGHT, ONE FRAME ────────────────────────────────────────────────────────
        //
        // bar → a car → his cabin → a wait behind a leaf → a car → the hall → the tube → the gallery. While
        // the captain is on his floor he is a body on it; while the captain is elsewhere he is a clock
        // running at the same pace. The LAST leg is #1254's, and from the frame it begins this method's own
        // tail — the card at the blind end — runs exactly as it shipped.
        StepHisNight(in bar, person);

        TheWalkIsEmpty(in bar, person);
    }

    /// <summary>
    /// #1277 · <b>THE MAN THE WALK HAS CLAIMED THIS WATCH, AND THE ONE PLACE THAT SAYS SO.</b> Null at every
    /// berth in the game but one, and on most evenings at that one too.
    ///
    /// <para><b>The ruling (#1277): the TAIL WINS.</b> #731's hours and #1199's tail both want the same man
    /// out of the same chair, and until this method existed the hours simply got there first — a scheduled
    /// departure walked GILT-EYE out through a cellar leaf an hour before last call, <c>_barLeft</c> had him,
    /// the tail found no chair to start a route from, and the whole night silently did not happen. A tester
    /// at the documented link saw an ordinary bar.</para>
    ///
    /// <para>So the claim is stated ONCE, here, and both systems read it: the tail begins with it (below,
    /// in <see cref="AdvanceTheWalk"/>) and the room's own departure roster defers to it
    /// (<c>TheWatchDecidesWhoGoes</c>). <b>He is not dropped from the evening's departures — his departure is
    /// the tail's first leg</b>, which leaves the chair on the frame his legs start by the bar's own
    /// one-body-one-place law. The room still empties a man; it is simply the walk that walks him.</para>
    ///
    /// <h3>Why the ROSTER and not a race</h3>
    ///
    /// <para>The other road was for the tail to claim him before the hours run — and it cannot, because the
    /// tail's own law is that nobody gets out of a chair before last call
    /// (<see cref="Egress.LastCallFraction"/>) while the hours deal INSIDE that fraction. Reordering the two
    /// calls in a frame would change nothing: at the second the hours take him the tail is still refusing to
    /// act, and correctly. The roster is the only seam where both systems keep their own law — the hours
    /// still deal a whole watch's worth of churn out of the room's own list, at their own moments, through
    /// their own leaves, and the walk still waits for last call.</para>
    ///
    /// <para>Asked of the same three facts the beat has always turned on, in the same order: this station has
    /// no walk, the beat is already spent, or the rota does not have him in this room this watch. Each is a
    /// plain no, and none of them is said out loud.</para>
    /// </summary>
    private string? TheManTheWalkHasClaimed(string berth)
    {
        if (!HavenInterior.HasObservationWalk(berth) || ObservationWalk.IsSpent(_observationWalkSpentOn))
        {
            return null;
        }

        string person = TheTail.ThePersonOfInterest(berth);

        // Not in the room this watch. Nobody to follow, nobody to hold a chair for, and nothing to say
        // about it — and the hours are free to schedule whoever the rota DID seat.
        return PatronRota.Resolve(person, berth, _dockVisitSimTime) == PatronState.AtBar ? person : null;
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
        _walkAtTheRailSince = double.NaN;
        _walkGalleryLookIndex = long.MinValue;
        _walkVanishedBehindThePaper = false;
        _walkTurnedBack = false;
        _walkStoodAsideSince = double.NaN;   // #1285 · the courtesy's own clock goes with the berth.
        ForgetHisNight();   // #1253 slice 2 · …and the four legs before the shipped one.
        ForgetTheGallerysMachines();
    }

    // ── THE BEAT ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>THE CAPTAIN WALKS IN, AND THE WALK IS EMPTY.</b> The one card this feature raises, at the
    /// blind end, once per universe.
    ///
    /// <para>Four conditions, and every one of them is something the captain did: they are not on the floor
    /// any more; a whole <see cref="ObservationWalk.TheWaitSeconds"/> has gone by since the last moment he
    /// had eyes on them (long enough that they must have finished the view); he is inside the walk; and he
    /// has come within reach of the rail. Waiting and then not walking in ends nothing — the beat is the
    /// captain's to reach, which is why the room is a room first.</para>
    ///
    /// <para><b>#1199 (2026-09-19) · AND FOR A CAPTAIN WHO WAS SITTING DOWN, THE REACH IS THE WHOLE GALLERY.
    /// The rule, stated once, and this is the once.</b> A captain who was STANDING when the man went has to
    /// walk out to the rail to find nothing there; the walk out IS the beat. A captain who was SITTING at one
    /// of the hat's tables has already arrived — he is in the room, four paces off the rail, looking straight
    /// at it, and the man is not at it. Sending him across the floor to be told the room is empty would be
    /// the game asking him to go and check what he is already looking at. So the clause is
    /// <see cref="_walkVanishedBehindThePaper"/> — was he sitting on the look the man went — and NOT <i>is he
    /// sitting now</i>: a captain who stands up and walks out gets the card at the rail exactly as he always
    /// did, and a captain who sits down afterwards gets nothing he did not earn.</para>
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
            || !HavenInterior.InTheObservationWalk(bar.BodyId, _avatarX, _avatarY, _havenFloor)
            || HavenInterior.TheRailAt(bar.BodyId) is not { } rail
            || !ObservationWalk.WouldSpend(bar.BodyId, _observationWalkSpentOn))
        {
            return;
        }

        double dx = rail.X - _avatarX, dy = rail.Y - _avatarY;
        bool atTheRail = (dx * dx) + (dy * dy) <= DeckPlan.InteractRadius * DeckPlan.InteractRadius;
        bool inTheGalleryHavingSatThroughIt =
            _walkVanishedBehindThePaper && HavenInterior.InTheGallery(bar.BodyId, _avatarX, _avatarY, _havenFloor);
        if (!atTheRail && !inTheGalleryHavingSatThroughIt)
        {
            return;   // in the walk, but not out at the end of it yet — and he was not sitting in the hat.
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
