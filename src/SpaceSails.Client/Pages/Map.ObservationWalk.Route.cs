using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE ROUTE (#1199) — sending the person of interest out onto the walk, and
/// <c>StepThePersonOfInterest</c>, the one frame of their walk out, their look at the rail and their going.
///
/// <para>Split out of <c>Map.ObservationWalk.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every fact about the walk stays in the opening
/// file.</para>
/// </summary>
public partial class Map
{
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
    ///   (<see cref="ObservationWalk.OnHisHeelsDu"/>, #1283), and who has already been noticed — and he does
    ///   it for <see cref="ObservationWalk.StandAsideSeconds"/> and then leads on (#1285), because letting
    ///   somebody past is a BEAT and a beat ends. That is
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
        bool inHisWay = _walkNoticed && eyesOnHim && rangeDu <= ObservationWalk.OnHisHeelsDu
            && TheCaptainIsBehindHim(who)
            && !HavenInterior.InTheObservationWalk(bar.BodyId, who.Walk.X, who.Walk.Y, _havenFloor);

        // ── #1285 · …AND STANDING ASIDE IS A BEAT, WHICH MEANS IT ENDS ───────────────────────────────────
        //
        // What was played: the documented link, booted and untouched, and he never gets out of the chair —
        // for five minutes, with the whole room frozen behind him. He DOES get up (the room's hours ran, the
        // salesman finished his round, `_barLeft` has him); he takes four strides and stops dead a body's
        // length from the captain, badged LettingYouPass, for ever. `?ashore=1` stands the captain on the
        // bar's own threshold, which is inside the two-pace band and squarely in his line to the cars — so
        // the courtesy fires on his first stride and a captain who only WATCHES never clears it. One step of
        // the captain, any step, and the whole two-leg night ran to the second.
        //
        // A courtesy with no end is not a courtesy, it is a deadlock wearing manners. He holds the doorway
        // for as long as the courtesy is FOR (ObservationWalk.StandAsideSeconds — the band's own width at a
        // body's pace, the time somebody two paces back needs to come past) and then goes on with his
        // evening. The offer is made ONCE PER APPROACH and not once per frame: having led on, he does not
        // stand aside again until the captain has been outside the band since, or he would inch one stride
        // and re-freeze, which is the same stall spelled sixty times a second.
        if (!inHisWay)
        {
            _walkStoodAsideSince = double.NaN;
        }
        else if (double.IsNaN(_walkStoodAsideSince))
        {
            _walkStoodAsideSince = SimTime;
        }

        bool holding = inHisWay
            && SimTime - _walkStoodAsideSince < ObservationWalk.StandAsideSeconds;
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
}
