using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · WHAT EACH STEP OF THE WALK ASKS (#1199) — whether the captain's eyes are elsewhere, whether he is
/// at the end of a leg, behind the captain, in the gallery or standing still, his leaving the floor, his
/// turning back, and the re-badge.
///
/// <para>Split out of <c>Map.ObservationWalk.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field.</para>
/// </summary>
public partial class Map
{
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
}
