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
        ForgetHisNight();   // #1253 slice 2 · …and the four legs before the shipped one.
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
    /// <param name="from">#1253 slice 2 · Where the leg starts, when it is not the chair. Null is the shipped
    /// route — up from the top the rota seated him at — and a point is the doors of the car he has just come
    /// back up on. It is a PARAMETER and not a second planner: the walk, the plate and the errand are the
    /// same ones either way, and the only thing a night with a basement in it changes about this leg is where
    /// the man is standing when it begins.</param>
    private void SendThemOutOntoTheWalk(
        in HavenInterior.BarFloor bar, string person, DeckReachability.Point? from = null)
    {
        if (_barAfoot.Count >= WalkerBand)
        {
            // A full band is NOT NOW rather than NO — the room's own leavers hold slots for a few seconds at
            // a time and then give them back. Marking the walk dealt here would let a busy instant cancel the
            // whole beat for the visit, which is the opposite of what dealing-once is for.
            return;
        }

        _walkDealt = true;

        // #1253 slice 2 · THIS METHOD IS THE LAST LEG, and it says so itself. Whoever called it — the
        // night's own state machine coming up out of a car, or a haven with no floor under it walking the
        // shipped one-leg route — the man it puts on the floor is walking to the rail, and everything
        // downstream that asks which leg he is on (the gallery's vanish, the hold's tube clause) has to be
        // reading the same answer as the walker actually on the deck. Set HERE rather than at the call
        // sites, because a leg a caller has to remember to declare is a leg somebody will forget to.
        _nightLeg = HisNight.ToTheWalk;

        if (HavenInterior.TheRailAt(bar.BodyId) is not { } rail)
        {
            return;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;

        // ── #1253 slice 2 · HE MAY BE COMING OUT OF A CAR RATHER THAN OUT OF A CHAIR ────────────────────
        //
        // On a night with a basement in it this leg begins at the doors he came back up on, and the loop
        // below cannot find him: the room was told he had gone (`_barLeft`) an hour ago when his legs
        // started, so the rota answers GONE and there is no seat to stand beside. His plate comes off the
        // room's own short name for him instead, which is the same string that seat would have carried —
        // one spelling, so a man followed out of a lift is the man who was sitting at that top.
        if (from is { } doors)
        {
            if (OnFoot(
                    HisShortName(bar.BodyId, person), new NpcWalk.Bound("", rail.X, rail.Y), doors, walls)
                is not { } fromTheCar)
            {
                return;   // the concourse refuses him from those doors. Nothing is placed at the far end.
            }

            _barAfoot.Add(new Walker
            {
                Walk = fromTheCar, Table = -1, For = Errand.WalkingTheRoute, Who = person,
            });
            StateHasChanged();
            return;
        }

        IReadOnlyList<HavenInterior.SeatedRegular> rota =
            HavenInterior.ResolveRegulars(bar.BodyId, _dockVisitSimTime, TheBarsChurn);

        foreach (HavenInterior.SeatedRegular seated in rota)
        {
            if (!seated.Present || !string.Equals(seated.Id, person, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (BesideThisTop(new DeckReachability.Point(seated.X, seated.Y), walls) is not { } beside
                || OnFoot(seated.ShortName, new NpcWalk.Bound("", rail.X, rail.Y), beside, walls) is not { } walk)
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
    /// <h3>#1199 (2026-09-19) · THE NEWSPAPER WITH EYE HOLES — ONE STATE MACHINE, STATED ONCE</h3>
    ///
    /// <para><b>What was played</b> (owner, live on the T): <i>"I think the tailed one should go to the
    /// observation deck even if I am there before they arrive… They should act normal even if I tail from
    /// ahead."</i> #1245's throat held him for a captain within two paces and then sent him back out again,
    /// and #1201's en-route hold stopped him for anybody in his line at all. Both are a man reacting to a
    /// stranger in a public room, and neither of them is acting normal.</para>
    ///
    /// <para><b>So there are exactly three things he can be doing</b>, and no fourth:</para>
    /// <list type="number">
    ///   <item><b>Walking his errand</b> — across the concourse and down the tube. He stops for one thing
    ///   only: a captain who is BEHIND him, whose eyes are on him, <b>within two paces</b>
    ///   (<see cref="ObservationWalk.OnHisHeelsDu"/>, #1283), and who has already been noticed. That is
    ///   letting somebody past you on a floor, which is a thing people do; stopping dead for somebody
    ///   standing between you and where you are going is not, and it is what let a captain who walked in
    ///   first stall the whole beat. Neither is stopping for a stranger ten paces back across a lit hall,
    ///   which is what the legibility band made him do for nine and a half minutes at a car's landing.</item>
    ///   <item><b>In the gallery</b> — walking to the rail, then standing at it looking out. Once a look
    ///   (<see cref="ReeverObservation.LookIntervalSeconds"/>, the one clock) the room asks whether he is
    ///   still watched, and the first time the answer is no <b>he is off the floor</b>. While he is WALKING
    ///   that means sight of him (the tube's corner, the island machine — a moving man is tracked whatever
    ///   else you are doing); once he is AT THE RAIL it means
    ///   <see cref="ObservationWalk.TheCaptainHasEyesOnHim"/> (the paper, the eyepiece — a man standing still
    ///   at a glass wall is scenery, and scenery is what you look away from). Four ways to lose him, one
    ///   clock under all four.</item>
    ///   <item><b>Walking back out past you</b> — the ending in which nothing happens. A captain who never
    ///   once took his eyes off him for a whole <see cref="ObservationWalk.TheWaitSeconds"/> at the rail gets
    ///   a man who has finished the view and leaves. No vanish, no card, no note, <b>and nothing spent</b>:
    ///   he was never shown anything, so there is nothing to take off him, and the walk is there on a later
    ///   visit.</item>
    /// </list>
    ///
    /// <para>Nothing blinks out in plain sight — the horror is still a body you were NOT looking at.</para>
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

        // #1199 · THE ONE READING, taken once, so the hold and the vanish cannot come to two opinions about
        // how close is close and about what being looked at is.
        bool eyesOnHim = ObservationWalk.TheCaptainHasEyesOnHim(
            clearLine, rangeDu, eyesElsewhere: TheCaptainsEyesAreElsewhere(bar.BodyId));

        // ── THE NOTICE QUESTION, and it is #436's eye with a person in front of it ───────────────────────
        //
        // On the GEOMETRY and not on the eyes: whether somebody clocks you over their shoulder is a fact
        // about the room, and a captain reading a paper is still a shape at a table that a man can half-see.
        // What the captain's eyes are doing decides the VANISH, which is the captain's own beat.
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

        // ── HE HAS HAD HIS VIEW AND IS WALKING BACK OUT PAST YOU ─────────────────────────────────────────
        //
        // FIRST, above every other branch. He must not hold again on the way out — a man who has decided to
        // leave, stopping dead every time the captain is inside thirty du of him, would never get out of his
        // own tube — and he must not vanish on the way back through the gallery either, which is the one
        // thing _walkTurnedBack exists to say.
        if (_walkTurnedBack)
        {
            if (who.Walk.Afoot)
            {
                who.Walk.Step(dt, walls, _avatarX, _avatarY);
                return !who.Walk.Afoot;
            }

            // He is out, and NOTHING IS SPENT. The captain watched a man look at a view and walk away again,
            // which is all that happened; the walk is still there the next time he ties up at this berth.
            _barAfoot.RemoveAt(slot);
            return true;
        }

        // ── THE GALLERY — the one room the vanish can happen in ──────────────────────────────────────────
        //
        // #1253 slice 2 · …and the one LEG it can happen on. The crossbar is on the concourse and the lower
        // level is laid in the same coordinate space, so a man walking a service corridor stands on the
        // gallery's own coordinates twice a night — and a vanish there would be him going out like a light
        // in a corridor with five shut doors on it. The level clause below and this leg clause are two
        // spellings of one fact and both are cheap; a room is a floor as well as a rectangle.
        if (_nightLeg == HisNight.ToTheWalk
            && HavenInterior.InTheGallery(bar.BodyId, who.Walk.X, who.Walk.Y, _havenFloor))
        {
            return HeIsInTheGallery(
                who, dt, in bar, walls, slot,
                lineOnHim: clearLine && rangeDu <= FootTail.LegibleDu,
                eyesOnHim: eyesOnHim);
        }

        // ── STOPPED, TURNED, WAITING FOR YOU TO GO PAST ──────────────────────────────────────────────────
        //
        // …ON A FLOOR SOMEBODY IS CROSSING, and nowhere else. Standing aside is a thing you do in a hall: the
        // other person goes round you and you both get on. In a tube with one end there is nothing to stand
        // aside FOR — the captain cannot pass, so a man who stops there stops for ever, which is the stall
        // the owner watched twice. Inside the walk he walks; the room has one way out and he is using it.
        // #1253 slice 2 · The tube's clause asks the FLOOR as well. Down below there is no tube — the
        // corridor is a floor somebody is crossing, with three ways off it — so standing aside is exactly
        // the thing a man does there, and a level-blind rectangle would have suppressed it on the two
        // squares of that corridor the T happens to lie over one storey up.
        // #1283 · …AND WITHIN TWO PACES, WHICH IS WHAT "TOO CLOSE" MEANS ON A FLOOR SOMEBODY IS CROSSING.
        // The hold read the LEGIBILITY band until today, and Selene Gate's hall is 34 du across against a
        // 30 du band: a captain following anywhere in his line held him for as long as he cared to stand
        // there — nine and a half minutes of it, watched (#1282/#1283). Being near enough to be NOTICED and
        // being near enough to be LET PAST were one number by accident; they are two now, and the second is
        // the small-room constant the throat already used. The rest of the clause is unchanged: he has to
        // have clocked the captain, the captain has to be behind him, and it is never done in the tube.
        bool holding = _walkNoticed && eyesOnHim && rangeDu <= ObservationWalk.OnHisHeelsDu
            && TheCaptainIsBehindHim(who)
            && !HavenInterior.InTheObservationWalk(bar.BodyId, who.Walk.X, who.Walk.Y, _havenFloor);
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

            // ── #1281 · A LEG IS OVER WHERE IT ENDS, NOT WHERE THE ROUTE OBJECT GIVES UP ─────────────────
            //
            // Owner's QA, on the service level: a body appears at the captain's own elbow at a car's landing
            // eighteen seconds after the doors open, and is still standing in exactly that spot four minutes
            // later. It is not the coat and it is not a floor key — it is HIM, on the leg that ends at the
            // car he rides back up on, standing one body-width off his own destination for ever.
            //
            // WHY. NpcWalk's courtesy stops a walker before a step that would bring it inside one body-width
            // of the captain, sets Doing.Waiting, and KEEPS THE ROUTE — "so the walk finishes itself the
            // moment the doorway clears". That is right in a bar, where the captain is passing through. A
            // CAR'S LANDING is the one square in this building the captain does not pass through: it is
            // where a ride sets him down and where [E] finds the panel, so he is standing on it precisely
            // when somebody else's leg ends there. The courtesy never clears, the leg never finishes, and
            // the whole night behind it stops — he never rides, never comes up, and the beat the floor was
            // built for cannot happen at all.
            //
            // So the NIGHT'S legs end at their far end. The reach is the courtesy's OWN width, read off the
            // two constants that make it, so the distance a walker stops at and the distance this file calls
            // arrived cannot come to two numbers. #1254's leg is untouched: its far end is the rail, inside
            // the gallery, and the gallery branch above has already had that frame.
            bool theLegIsOver = _nightLeg != HisNight.ToTheWalk && HeIsAtTheEndOfThisLeg(who);
            if (!theLegIsOver)
            {
                return !who.Walk.Afoot;
            }

            _barAfoot.RemoveAt(slot);
            return true;
        }

        // ── AND THEN THERE IS NOBODY THERE ───────────────────────────────────────────────────────────────
        //
        // The route has run out somewhere that is NOT the hat. On the shipped walk that cannot happen — the
        // route ends at the rail and the rail is in the gallery, so the branch above has already had this
        // frame — and it is kept for the haven that has a walk and no crossbar at the end of it
        // (TheGalleryBox answers null there, so InTheGallery is false for every point). The same one rule
        // applies: he comes off the floor on the first frame the captain's eyes are not on him and never on
        // a frame he is being watched, because a body that blinks out in plain sight is a bug and the horror
        // here is that it is not one.
        // #1253 slice 2 · …AND ONLY ON THE LAST LEG. A route that runs out on one of the others has run out
        // at a car's doors or at his own cabin leaf, and what that MEANS is the night's to decide — he rides,
        // or he goes in and the leaf shuts. He comes off the floor either way (there is no body in a car and
        // none behind a closed door), but nothing is spent, no clock starts and no card is ever owed: this
        // is a man walking through a building, and the only thing that is ever "not there" is the one at the
        // end of an empty room.
        if (_nightLeg != HisNight.ToTheWalk)
        {
            _barAfoot.RemoveAt(slot);
            return true;
        }

        if (eyesOnHim)
        {
            who.Walk.LookTowards(_avatarX, _avatarY);
            return false;
        }

        HeIsNotOnTheFloorAnyMore(slot);
        return true;
    }

    /// <summary>
    /// #1199 (2026-09-19) · <b>IS THE CAPTAIN LOOKING AT SOMETHING THAT IS NOT THE ROOM?</b> The
    /// <i>elsewhere</i> half of <see cref="ObservationWalk.TheCaptainHasEyesOnHim"/>, and the only part of
    /// the rule that is about the captain rather than about the deck.
    ///
    /// <para><b>The paper.</b> Sitting at one of the gallery's own tables, which is the owner's whole ruling:
    /// <i>"It is the classic sit at a café with a newspaper with eye holes gumshoe cliché."</i> A captain
    /// SEATED there is not a tail, he is a customer; the seat panel's existing <c>Read the news</c> is the
    /// eye holes and needed no wiring, because the sitting IS the cover. Asked as <i>seated AND in the
    /// gallery</i> — the seat system's own state (<c>Seating.TryTakeBarTop</c> snaps the captain onto the
    /// top's chair and <c>SeatedTable</c> is the page's answer) crossed with the room, so not one seat
    /// anywhere else in the game learns a thing about this beat.</para>
    ///
    /// <para><b>The eyepiece.</b> Anything wearing a full-viewport scrim — the coin binoculars' own card
    /// among them, which is the owner's <i>E on the binoculars</i>, and the vending machine's, and the
    /// satchel. <see cref="AScrimIsUp"/> is #1052's census and is already the page's one answer to <i>is
    /// something standing in front of the world</i>; a second list here would be a second opinion, and it
    /// would drift the first time a card was added.</para>
    /// </summary>
    private bool TheCaptainsEyesAreElsewhere(string bodyId) =>
        (SeatedTable is not null && HavenInterior.InTheGallery(bodyId, _avatarX, _avatarY, _havenFloor))
        || AScrimIsUp;

    /// <summary>
    /// #1281 · <b>HAS HE REACHED THE FAR END OF THE LEG HE IS ON?</b> Asked of the walk's own bound, so the
    /// place this file calls <i>arrived</i> is the place the route was plotted to and never a second copy of
    /// it.
    ///
    /// <para><b>The reach is the courtesy's own width plus a body</b>, derived from the two published
    /// constants rather than chosen. <see cref="NpcWalk.PersonalSpaceInRadii"/> is how near the captain a
    /// walker will come, so a captain standing ON the far end leaves the walker exactly that far off it — and
    /// a hair further, because the courtesy is tested BEFORE a sub-step rather than after one, so he comes to
    /// rest a stride outside the line rather than on it. One body radius is the slack that covers the stride.
    /// A reach at the courtesy's own width would be a leg that can never end by about three hundredths of a
    /// deck unit, which is the measurement this lane was opened by.</para>
    /// </summary>
    private static bool HeIsAtTheEndOfThisLeg(Walker who)
    {
        double dx = who.Walk.For.X - who.Walk.X, dy = who.Walk.For.Y - who.Walk.Y;
        double reach = (NpcWalk.PersonalSpaceInRadii + 1) * DeckPlan.AvatarRadius;
        return (dx * dx) + (dy * dy) <= reach * reach;
    }

    /// <summary>#1199 (2026-09-19) · Is the captain BEHIND him — on the far side of him from where he is
    /// going? The half-plane his own route puts him in: his BOUND and never his facing, which swings round
    /// to look at whoever he has stopped for and would otherwise un-stop him on the very next frame.
    ///
    /// <para>It is the whole of what <i>letting you pass</i> means: you stand aside for somebody coming up
    /// behind you. A captain in front of him is a person in the room, and a man does not stop walking because
    /// somebody is standing where he is headed — he goes round them, which is what the walker's own personal
    /// space has always done.</para></summary>
    private bool TheCaptainIsBehindHim(Walker who)
    {
        double aheadX = who.Walk.For.X - who.Walk.X, aheadY = who.Walk.For.Y - who.Walk.Y;
        double toCaptainX = _avatarX - who.Walk.X, toCaptainY = _avatarY - who.Walk.Y;
        return ((aheadX * toCaptainX) + (aheadY * toCaptainY)) <= 0;
    }

    /// <summary>
    /// #1199 (2026-09-19) · <b>HE IS IN THE HAT, AND THIS IS THE FRAME THE BEAT TURNS ON.</b> He walks to the
    /// rail and stands there looking out; the room asks once a look whether anybody is watching.
    ///
    /// <para><b>The look clock, not the frame.</b> <see cref="ReeverObservation.LookIndexAt"/> is the cadence
    /// every watched-from-somewhere beat in this game already runs on, and it is borrowed here for two
    /// reasons: the question is put ONCE per look however many frames a look spans, and the first look asked
    /// is one the captain has had time to take. A vanish decided on the frame he crosses the throat would be
    /// a body going out like a light in the middle of a step.</para>
    ///
    /// <para><b>And the ending in which nothing happens.</b> If a whole
    /// <see cref="ObservationWalk.TheWaitSeconds"/> goes by at the rail without one unwatched look, he has
    /// had his view: he turns round and walks back out past the captain, and the beat is NOT spent. Staring
    /// somebody down is not a way to lose the scene for ever — it is a way to not get it today.</para>
    /// </summary>
    private bool HeIsInTheGallery(
        Walker who, double dt, in HavenInterior.BarFloor bar,
        IReadOnlyList<SurfaceCollision.Segment> walls, int slot, bool lineOnHim, bool eyesOnHim)
    {
        bool changed = false;

        if (who.Walk.Afoot)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            changed = !who.Walk.Afoot;
        }

        bool heIsStandingStill = HeIsStandingStill(who);

        if (heIsStandingStill && double.IsNaN(_walkAtTheRailSince))
        {
            // He has arrived at the rail, and the wait he is allowed to stand there starts NOW — on the very
            // frame his legs stop, not the one after it. A clock started a frame late is a clock, and this
            // one decides whether a scene happens.
            _walkAtTheRailSince = SimTime;
            changed = true;
        }
        else if (!heIsStandingStill && !double.IsNaN(_walkAtTheRailSince))
        {
            // …and a man who is walking again is not standing at a rail. The only way back out of standing
            // still is the captain stepping out of his road, and when that happens the wait he is allowed at
            // the glass has not begun — it begins when he next stops.
            _walkAtTheRailSince = double.NaN;
            changed = true;
        }

        // ── THE LOOK ─────────────────────────────────────────────────────────────────────────────────────
        //
        // WHILE HE IS WALKING it takes losing SIGHT of him — a man crossing a room in front of you is a
        // moving thing, and you track a moving thing whatever else you are doing. ONCE HE IS STANDING it
        // takes only your eyes: a man standing still at a glass wall is scenery, and scenery is what a
        // captain looks away from. That is the whole difference between the two readings, and it is why the
        // paper works at the rail and not in the doorway.
        bool stillWatched = heIsStandingStill ? eyesOnHim : lineOnHim;

        long look = ReeverObservation.LookIndexAt(TheTail.SeedFor(_walkBerth ?? "", who.Who), SimTime);
        if (look != _walkGalleryLookIndex)
        {
            if (_walkGalleryLookIndex != long.MinValue && !stillWatched)
            {
                // …and there is nobody there. Behind the paper, behind the eyepiece, behind the island
                // machine, or round the corner of the tube — the room does not distinguish between the four,
                // and neither does the book.
                _walkVanishedBehindThePaper =
                    SeatedTable is not null
                    && HavenInterior.InTheGallery(bar.BodyId, _avatarX, _avatarY, _havenFloor);
                HeIsNotOnTheFloorAnyMore(slot);
                return true;
            }

            _walkGalleryLookIndex = look;
        }

        if (heIsStandingStill && SimTime - _walkAtTheRailSince >= ObservationWalk.TheWaitSeconds)
        {
            return HeTurnsAndWalksBackOut(who, in bar, walls, slot) || changed;
        }

        return changed;
    }

    /// <summary>
    /// #1259 · <b>IS HE STANDING STILL?</b> — the question the gallery's two readings actually turn on, and
    /// for one issue it was asked as <c>Walk.Afoot</c>, which is a different question.
    ///
    /// <para><c>Afoot</c> is <i>has he route left</i>. It is true of a man walking and also true of a man who
    /// has <b>stopped dead because the captain is in his road</b> (<see cref="NpcWalk.Doing.Waiting"/> —
    /// stopped, looking at you, route kept). The two are the same fact to a pathfinder and opposite facts to
    /// a tail: one is a moving thing you track whatever else you are doing, and the other is a man standing
    /// two feet from you at a glass wall, which is the definition of the scenery the eyes rule is about.</para>
    ///
    /// <para><b>What it cost (#1259, played 2026-09-20).</b> The coin binoculars are bolted to the rail, and
    /// the rail is where his route ENDS — so a captain with his eye to the eyepiece is standing on the man's
    /// own destination. He stopped one body-width short, <c>Afoot</c> stayed true for ever, the room went on
    /// reading SIGHT of him rather than the captain's eyes, and the one card in this room that exists to take
    /// a captain's eyes off the world bought four credits' worth of nothing. The wait never started either,
    /// so he never gave up and walked out: he stood there as long as anybody watched.</para>
    ///
    /// <para>So the room asks whether he MOVED, which is <see cref="NpcWalk.Doing.Walking"/> and nothing
    /// else — arrived, snagged and waiting are all a man standing still, and the deck already tells them
    /// apart by name.</para>
    /// </summary>
    private static bool HeIsStandingStill(Walker who) => who.Walk.State != NpcWalk.Doing.Walking;

    /// <summary>#1199 · He is off the floor, and the second he went is the second the captain last had eyes
    /// on him — which is therefore the second <see cref="ObservationWalk.TheWaitSeconds"/> is counted from.
    /// One writer, so the gallery's vanish and the hatless walk's cannot start two different clocks.</summary>
    private void HeIsNotOnTheFloorAnyMore(int slot)
    {
        _barAfoot.RemoveAt(slot);
        _walkGoneSince = SimTime;
        _walkAtTheRailSince = double.NaN;
    }

    /// <summary>
    /// #1199 · <b>HE TURNS ROUND AND LEAVES, PAST YOU.</b> The return leg is planned on the one planner
    /// (<c>OnFoot</c>) from where he is standing back to the standing room beside his own top — the exact
    /// reverse of the leg <see cref="SendThemOutOntoTheWalk"/> plotted, off the same rota and the same
    /// <c>BesideThisTop</c>, so there is no second pathfinder and no second idea of where he came from.
    ///
    /// <para><b>Nothing is spent here</b> (2026-09-19). #1245 spent the beat on this leg as the price of
    /// tailing too close; the owner's ruling took the hold away, and with it the crime. What is left is a
    /// captain who watched a man unblinkingly for three minutes and saw exactly what there was to see — a
    /// regular at a rail — and a walk that is still there on his next visit.</para>
    /// </summary>
    private bool HeTurnsAndWalksBackOut(
        Walker who, in HavenInterior.BarFloor bar,
        IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        _walkAtTheRailSince = double.NaN;

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
        // refusal rather than as a reason to place a body at the far end anyway. He is simply not there any
        // more, and nothing is spent for that either.
        _barAfoot.RemoveAt(slot);
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
