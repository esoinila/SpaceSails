using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #973 L2 · HARLAN FESS ON HIS ROUNDS — the salesman, and the four verbs the old crew will inherit.
///
/// <para>Owner: <i>"the salesmen are a low-stakes place to practise NPC interaction (they move about, they
/// try to sell, nothing is at stake)."</i> So he is deliberately built out of nothing new. He is a
/// <see cref="Walker"/> like the haulier and the sweep team, planned through the one <c>OnFoot</c> that
/// claims the person's gait, drawn out of the walker band, and stepped by the same frame that steps
/// everybody else. What this file adds is the four verbs — <b>approach, address, withdraw, remember you said
/// no</b> — and every rule behind them is a pure function in <see cref="NebulaRep"/>.</para>
///
/// <h3>Where he works, and why it is the canteen floor</h3>
///
/// <para>The brief said "a concourse or bar deck", and in this game that room is the hive's upper canteen:
/// it is the only deck with a walker band, a counter, tops the captain can actually sit alone at, and doors
/// somebody can walk in from. A docked station's bar has posters and a barkeep and no seating and no
/// walkers at all — a salesman there could only teleport at a captain who cannot sit down, which is the
/// opposite of the practice the owner asked for. The presence rota is therefore keyed on the BODY being
/// visited rather than on a berth id; the law ("at most one place in three, never two visits running") is
/// unchanged and lives in Core.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>How long he stands at a fixture before drifting to the next one. Long enough to read as a
    /// man waiting for somebody to look up, short enough that the room has motion in it.</summary>
    private const double RepDwellSeconds = 9.0;

    /// <summary>How close the captain has to pass a rebuffed Fess before he says the only thing he has left
    /// to say. The captain's own interact reach, so "walking past him" means what it means everywhere else.</summary>
    private const double RepPassingReachDu = DeckPlan.InteractRadius;

    /// <summary>Which visit's room he is remembering. Null off the ground; a different body is a new
    /// visit — the same fold <c>EnsureBarVisit</c> keeps for the station bar.</summary>
    private string? _repVisitBody;

    /// <summary>The running count of ground visits this thread has made. The rota's clock.</summary>
    private int _repVisitIndex = -1;

    /// <summary>Whether the rota has him working THIS visit at all.</summary>
    private bool _repWorkingHere;

    /// <summary>#973 L2 dev cheat (<c>/map?rep=1</c>, <c>/map?rep=0</c>): force him on or off this ground.
    /// Null is the shipped rota. It forces WHETHER and never WHO or WHAT — the pitch, the prices, the
    /// rarity of the bleed and the once-per-life page are all the ones a captain gets.</summary>
    private bool? _repCheat;

    /// <summary>Remember-you-said-no, and only until the doors shut.</summary>
    private NebulaRepVisit _repMemory = NebulaRepVisit.Fresh;

    /// <summary>How many times he has reached a table and pitched, this thread. The bleed's clock.</summary>
    private int _repMeetings;

    /// <summary>When he next drifts to another fixture.</summary>
    private double _repMoveOnAt;

    /// <summary>Whether he has already said the one thing a rebuffed salesman says, this visit.</summary>
    private bool _repSaidPassing;

    /// <summary>The pitch card, when he is standing at the table with it. Null the rest of the time, and
    /// the whole of what <c>TheHostIsUp</c> asks about.</summary>
    private NebulaRep.RepPitch? _repCard;

    /// <summary>The name he read off the file for this pitch — the captain's, or a dead one's.</summary>
    private string _repNameOnFile = "";

    /// <summary>Whether this pitch is a bleed, which is the only thing that puts the extra button up.</summary>
    private bool _repBleeding;

    /// <summary>What he last said back, under the pitch. Cleared when he goes.</summary>
    private string? _repSaid;

    // ── #1061 · HE WORKS THE ROOM ──────────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-09-01: "let's at some point work on those A* walking insurance salesmen at stations :-D"
    //
    // Until this lane his beat was a ring of FURNITURE — the counter, the ends of two or three tops — walked
    // round for ever with nothing at the far end of any of it. The room contained a man drifting. What it
    // contains now is a man SELLING: he crosses to somebody else's table, stands there for a beat of patter,
    // and goes on to the next mark, and when the room is worked he leaves through a leaf that does not open
    // for the captain, like anybody whose shift has ended. Not one word is said at any of those tables — the
    // pause IS the patter (§13.8), and the point of the whole beat is that a captain sitting two tops away
    // WATCHES THE PITCH COMING.
    //
    // The round itself is Core's (Egress.Marks), frozen to the watch, so it is the same tables in the same
    // order on every machine and across a reload.

    /// <summary>#1061 · The round he is working: whose tables, in what order, and how long each pause lasts.
    /// Null is a question this visit has not asked yet; empty is an answer it gave (a room with nobody in it
    /// but the captain).</summary>
    private IReadOnlyList<Egress.Patter>? _repRound;

    /// <summary>#1061 · Which watch that round belongs to. A shift turning over is the room forgetting —
    /// #731's own law — so the people he was working went home and he starts on the ones who are here now.</summary>
    private long _repRoundWatch = long.MinValue;

    /// <summary>#1061 · How many of the round's marks he has actually finished. The counter does not count:
    /// nobody is sitting at it, and the floor under <see cref="Egress.MarksBeforeTheTable"/> is a floor about
    /// PEOPLE the captain has watched him work.</summary>
    private int _repMarksWorked;

    /// <summary>#1061 · Whether he has already stood at the counter this visit — the one stop on his round
    /// that is furniture, kept because it is where he says he will be and because a room with nobody in it
    /// still gets a man walking into it.</summary>
    private bool _repStoodAtTheCounter;

    /// <summary>#1061 · Where he was standing when the last pause ended.
    ///
    /// <para>The next leg begins THERE and not back at a doorstep, which is #973 L5b's own flag paid off for
    /// the salesman: <i>"a player watching the counter would see her vanish from it and come back out of the
    /// cellar. That is a worse lie than not retrying."</i> Null before his first walk of a visit, which is the
    /// one walk that really does begin at a door.</para></summary>
    private DeckReachability.Point? _repStandingAt;

    /// <summary>#1061 · His shift here is over. The room is worked, he has gone out through a leaf, and he
    /// does not come back — until the watch turns over, when it is a different room full of people.</summary>
    private bool _repShiftOver;

    /// <summary>#973 L2 · Which life the signing flashback has already come back in, or 0 for none.
    ///
    /// <para>The beat's own cadence is <c>EveryTime</c> — L1 chose it for the LEDGER, where the once-per-page
    /// latch is <c>FilingLine.PageState.Refused</c> and a rebirth re-greys the book. The rep has no page and
    /// no latch, so his once-per-life is kept here: the day you signed comes back to a captain once, and the
    /// NEXT captain gets it back because it is a different man reaching for it.</para></summary>
    private int _repSigningToldInLife;

    /// <summary>The name the file has. It is the captain's, except on the rare watch it is not.</summary>
    private string RepNameOnFile(bool bleeding) =>
        bleeding && ActiveThreadInfo?.Retired is { Count: > 0 } retired
            ? Captains.CleanName(retired[^1].Name)
            : ActiveCaptainName;

    /// <summary>How many captains this thread has buried.</summary>
    private int RetiredCaptainCount => ActiveThreadInfo?.Retired.Count ?? 0;

    /// <summary>Which life the captain is on, counting from one — the flashback subject's stamp.</summary>
    private int CaptainsLife => RetiredCaptainCount + 1;

    // ── The visit ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A DIFFERENT GROUND IS A DIFFERENT VISIT, and a different visit is a man who has never met you. The
    /// one place forgetting happens; everything else in this file only ever reads the memory.
    /// </summary>
    private void EnsureRepVisit(string? bodyId)
    {
        if (_repVisitBody == bodyId)
        {
            return;
        }

        _repVisitBody = bodyId;

        // #973 · …and the VOID'S WEATHER folds with him. One fold, because the rota that decides whether he
        // walked this room and the rule that decides whether the room is talking about him have to mean the
        // same room and the same visit — see Map.Weather.cs.
        EnsureTheWeathersVisit(bodyId);

        _repCard = null;
        _repSaid = null;
        _repBleeding = false;
        _repSaidPassing = false;
        _repMoveOnAt = 0;
        ForgetTheRound();

        if (bodyId is null)
        {
            _repWorkingHere = false;
            return;
        }

        _repVisitIndex++;
        _repMemory = _repMemory.AtVisit(_repVisitIndex);
        _repWorkingHere = _repCheat
            ?? NebulaRep.IsWorkingThisStation(_activeThreadId ?? "", bodyId, _repVisitIndex);
    }

    // ── One frame of his working day ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called once a frame from the surface tick, beside the room's other metabolism. Does nothing at all
    /// unless the rota has him on this ground and the captain is standing in a room with people in it.
    /// </summary>
    private void AdvanceTheRep(double dtRealSeconds)
    {
        if (_surface is not { Floor: < 0 } ex)
        {
            EnsureRepVisit(null);
            return;
        }

        EnsureRepVisit(ex.Stop.Body.Id);
        if (!_repWorkingHere || !TheCanteenOn(ex, out UndergroundComplex.Amenity amenity))
        {
            return;
        }

        // The shift turning over takes every walker off the floor, his included. Re-read the world rather
        // than trusting a field: the walker list is the truth about who is on their feet.
        if (TheRepAfoot(ex.Walkers) is null)
        {
            if (_repCard is not null)
            {
                // His card cannot outlive his body — a panel with nobody behind it is the exact state
                // #731's escort branch was written to refuse.
                CloseTheRepsCard();
            }

            _ = SendTheRepIn(ex, amenity, dtRealSeconds);
            return;
        }

        MaybeSayHeIsOnlyPassing(ex.Walkers);
        _ = dtRealSeconds;   // his stepping is AdvanceWalkers' job; this method only decides errands
    }

    /// <summary>The walker that is him, if he is on the floor. By errand, because his plate is his own.
    /// <para>#973 L0 · Handed the list rather than the excursion: he works two rooms now.</para></summary>
    private static Walker? TheRepAfoot(IReadOnlyList<Walker> afoot)
    {
        foreach (Walker w in afoot)
        {
            if (w.For is Errand.RepRounds or Errand.RepPitching or Errand.RepLeaving)
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>
    /// PUT HIM ON THE FLOOR, or move him along it. The first walk of a visit comes in through a door —
    /// #731's idiom, and the same one the haulier uses — and every walk after it begins at his own feet.
    ///
    /// <para>#1061 · The order below IS his working day, and it is written as a fall-through rather than as a
    /// state machine because each clause is a reason the one under it does not apply. He comes to the
    /// captain's table only once the captain has watched him work the room; otherwise he stands at the
    /// counter, then at somebody's table, then at somebody else's; and when there is nobody left to work he
    /// goes off shift. His approach to the CAPTAIN is untouched by any of it — same gate, same card, same
    /// memory of having been told no.</para>
    /// </summary>
    private bool SendTheRepIn(SurfaceExcursion ex, UndergroundComplex.Amenity amenity, double dt)
    {
        if (_repShiftOver || ex.Walkers.Count >= WalkerBand || SimTime < _repMoveOnAt)
        {
            return false;
        }

        _ = dt;
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        // #731 (B1 canteen) · …and the walked-in are in chairs too. One opinion about every top in the room,
        // which is why this reads the room's BOTH halves of churn and not just who stood up.
        IReadOnlyList<CanteenRegulars.TableSeat> tops = CanteenRegulars.Tables(
            ex.Stop.Body.Id, ex.Floor, amenity, ex.CanteenWatch, ex.HallStoodUp, ex.HallCameIn);

        // #1061 · Egress.SEATED and not Egress.OnTheSchedule: a salesman's round asks who is sitting there to
        // be stood beside, not who the shift may give legs to. The crowd is who an insurance man sells to,
        // and standing at their table gives them nothing to run — see the note on OnTheSchedule.
        TheRoundHeIsWorking(
            ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, () => Egress.Seated(tops));

        // He crosses to the table only when the captain is sitting alone at one, has not already sent him
        // away this visit — and has had time to watch him work the room first (#1061).
        if (TheCaptainIsSittingAlone(out int tableIndex)
            && _repMemory.MayApproach(_repVisitIndex)
            && TheCaptainHasWatchedHimWork
            && TopOn(ex, amenity, tableIndex) is { } top
            && ChairOppositeTheCaptain(in top, walls) is { } beside)
        {
            return PlanTheRep(ex, floor, walls, beside, Errand.RepPitching, tableIndex,
                              NpcWalk.NoPersonalSpace);
        }

        // The counter first, because that is where he says he will be.
        if (!_repStoodAtTheCounter)
        {
            _repStoodAtTheCounter = true;
            if (TheCounterOn(amenity, walls) is { } counter && !HeIsAlreadyStandingAt(counter))
            {
                return PlanTheRep(ex, floor, walls, counter, Errand.RepRounds, -1,
                                  NpcWalk.PersonalSpaceInRadii);
            }
        }

        // …and then the marks, in the order this watch dealt them.
        if (TheMarkHeIsOn is { } mark)
        {
            if (TheTopNumbered(tops, mark.Index) is { } theirs
                && WhereABodyStandsAt(in theirs, walls) is { } at
                && !HeIsAlreadyStandingAt(at)
                && PlanTheRep(ex, floor, walls, at, Errand.RepRounds, mark.Index,
                              NpcWalk.PersonalSpaceInRadii))
            {
                return true;
            }

            // Their top has gone (they finished and left), the stone allows nobody beside it, the floor has
            // no route to it, or he is already standing there. A man does not queue for a table nobody is at,
            // and a mark retried every frame for a whole watch is a salesman in a loop: it is worked.
            _repMarksWorked++;
            return false;
        }

        return HeGoesOffShift(ex, floor, walls);
    }

    /// <summary>Plan one of his walks. He starts from where he is standing if he is already working this room,
    /// and from the doorstep of a door he does not have to be let through if this is his entrance.</summary>
    private bool PlanTheRep(
        SurfaceExcursion ex, UndergroundComplex.FloorPlan floor,
        IReadOnlyList<SurfaceCollision.Segment> walls, DeckReachability.Point to, Errand errand, int table,
        double berth)
    {
        if (WhereHeSetsOffFrom(floor.Locked, walls, ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, to)
            is not { } from)
        {
            return false;
        }

        if (OnFoot(NebulaRep.Plate, new NpcWalk.Bound("", to.X, to.Y), from, walls, berth) is not { } walk)
        {
            // He cannot get there from where he is standing. He is not left frozen mid-floor: the next frame
            // starts him from a doorstep again, which is the one beginning this room always has for him.
            _repStandingAt = null;
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = walk, Table = table, For = errand });
        StateHasChanged();
        return true;
    }

    /// <summary>#1061 · <b>THE ROOM IS WORKED, SO HE GOES.</b> Out through a leaf the captain's own TRY is
    /// refused at, on <see cref="Egress.DoorFor"/>'s answer off the frozen watch — the same door, the same
    /// call and the same plate as his way in, so the salesman never leaves through a leaf he was never behind.
    ///
    /// <para>The shift is over whether or not the floor gives him a way out of it. A room with no locked leaf
    /// is a room he simply stops working in, which is the honest answer and never a body left drifting
    /// between fixtures for the rest of a watch.</para>
    ///
    /// <para>Nothing is said. He is not at the counter the next time you look.</para></summary>
    private bool HeGoesOffShift(
        SurfaceExcursion ex, UndergroundComplex.FloorPlan floor,
        IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        _repShiftOver = true;
        if (_repStandingAt is not { } from)
        {
            return false;
        }

        int index = Egress.DoorFor(
            ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, NebulaRep.ContactId, floor.Locked);
        if (index < 0 || index >= floor.Locked.Count)
        {
            return false;
        }

        UndergroundComplex.LockedDoor door = floor.Locked[index];
        if (Egress.StandingPlaceAt(in door, DeckPlan.AvatarRadius, walls, from.X, from.Y) is not { } doorstep
            || OnFoot(NebulaRep.Plate, new NpcWalk.Bound(door.Sign, doorstep.X, doorstep.Y), from, walls)
                is not { } away)
        {
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = away, Table = -1, For = Errand.RepLeaving });
        StateHasChanged();
        return true;
    }

    /// <summary>#1061 · The one standing place at the counter this hall allows, or null. Read off the floor's
    /// published fixtures and never carved here — a second list of where the counter is would be this repo's
    /// oldest bug class with a salesman leaning on it.</summary>
    private static DeckReachability.Point? TheCounterOn(
        UndergroundComplex.Amenity amenity, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (amenity.Hall is not { } hall)
        {
            return null;
        }

        foreach (UndergroundComplex.CounterPlace place in hall.CounterRow)
        {
            if (!place.Seated && !SurfaceCollision.Blocked(place.X, place.Y, DeckPlan.AvatarRadius, walls))
            {
                // One standing place at the counter is the bar; the rest of the row is other people's.
                return new DeckReachability.Point(place.X, place.Y);
            }
        }

        return null;
    }

    /// <summary>#1061 · One of the hall's tops by the ordinal the round names — a lookup against the list the
    /// room was drawn from, never a second geometry.</summary>
    private static CanteenRegulars.TableSeat? TheTopNumbered(
        IReadOnlyList<CanteenRegulars.TableSeat> tops, int index)
    {
        foreach (CanteenRegulars.TableSeat top in tops)
        {
            if (top.Index == index && top.Taken)
            {
                return top;
            }
        }

        return null;
    }

    // ── #1061 · THE ROUND ──────────────────────────────────────────────────────────────────────────────

    /// <summary>#1061 · Make sure the round on the page is the one THIS watch dealt. Asked once per shift and
    /// only read afterwards: <see cref="Egress.Marks"/> is frozen to the watch, and re-asking it sixty times a
    /// second for an answer that cannot change is #731's own lesson with a salesman walking through it.
    ///
    /// <para>A shift turning over wipes it, which is the room forgetting — the people he was working went
    /// home three hours ago, and the ones sitting there now have never been sold anything.</para></summary>
    private void TheRoundHeIsWorking(
        string bodyId, int level, long watch, Func<IReadOnlyList<Egress.Occupant>> seated)
    {
        if (_repRound is not null && _repRoundWatch == watch)
        {
            return;
        }

        if (_repRound is not null)
        {
            ForgetTheRound();
        }

        _repRoundWatch = watch;
        _repRound = Egress.Marks(bodyId, level, watch, NebulaRep.ContactId, seated());
    }

    /// <summary>#1061 · A round belongs to one visit and one watch; forgetting it is forgetting both.</summary>
    private void ForgetTheRound()
    {
        _repRound = null;
        _repRoundWatch = long.MinValue;
        _repMarksWorked = 0;
        _repStoodAtTheCounter = false;
        _repStandingAt = null;
        _repShiftOver = false;
    }

    /// <summary>#1061 · The mark he is on, or null when the room is worked.</summary>
    private Egress.Patter? TheMarkHeIsOn =>
        _repRound is { } round && _repMarksWorked < round.Count ? round[_repMarksWorked] : null;

    /// <summary>
    /// #1061 · <b>HAS THE CAPTAIN WATCHED HIM WORK?</b> The whole of the beat, as one predicate: he does not
    /// come to your table until you have seen him at <see cref="Egress.MarksBeforeTheTable"/> other people's.
    ///
    /// <para>It is a FLOOR and not a quota, capped by the round the room could actually deal: a hall with one
    /// sitter in it is worked out after one table, and a salesman who stood about waiting for a second one
    /// that does not exist would be a captain sitting alone for a whole shift with nothing crossing the floor
    /// at all.</para>
    /// </summary>
    private bool TheCaptainHasWatchedHimWork =>
        _repMarksWorked >= Math.Min(Egress.MarksBeforeTheTable, _repRound?.Count ?? 0);

    /// <summary>#1061 · Is he already standing where the next stop is? A walk of no length is a teleport with
    /// a plate on it, and a stop he is already at is a stop he has worked.</summary>
    private bool HeIsAlreadyStandingAt(DeckReachability.Point to) =>
        _repStandingAt is { } here
        && ((here.X - to.X) * (here.X - to.X)) + ((here.Y - to.Y) * (here.Y - to.Y))
           < DeckPlan.AvatarRadius * DeckPlan.AvatarRadius;

    /// <summary>#1061 · WHERE THIS LEG BEGINS — his own feet if he is already working the room, and otherwise
    /// the doorstep of the leaf this watch deals him. One answer for both rooms.</summary>
    private DeckReachability.Point? WhereHeSetsOffFrom(
        IReadOnlyList<UndergroundComplex.LockedDoor> leaves,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        string bodyId, int level, long watch, DeckReachability.Point to)
    {
        if (_repStandingAt is { } here)
        {
            return here;
        }

        int index = Egress.DoorFor(bodyId, level, watch, NebulaRep.ContactId, leaves);
        return index < 0 || index >= leaves.Count
            ? null
            : Egress.StandingPlaceAt(leaves[index], DeckPlan.AvatarRadius, walls, to.X, to.Y);
    }

    /// <summary>#1061 · How long he stands at THIS stop. The round's own beat at a mark, and the plain dwell
    /// at the counter, where there is nobody to talk to.</summary>
    private double HisBeatAt(int table)
    {
        if (table >= 0 && _repRound is { } round)
        {
            foreach (Egress.Patter mark in round)
            {
                if (mark.Index == table)
                {
                    return mark.BeatSeconds;
                }
            }
        }

        return RepDwellSeconds;
    }

    /// <summary>The captain, alone, at a top he can be reached at. The seat's own two answers asked rather
    /// than re-derived — a page working out for itself what the chair already knows is how two instruments
    /// come to disagree.</summary>
    private bool TheCaptainIsSittingAlone(out int tableIndex)
    {
        tableIndex = -1;
        if (!CaptainIsSeated || !SeatedAlone || SeatedTable is not { Bench: false, Office: false } t)
        {
            return false;
        }

        tableIndex = t.Index;
        return true;
    }

    // ── Stepping him, and the two errands whose arrival is not an ending ────────────────────────────────

    /// <summary>
    /// #973 L2 · HIS TWO ERRANDS BOTH END STANDING UP. A haulier's walk ends with her in a chair and a
    /// sweeper's with him through the lock; Fess arrives and then STAYS — beside a fixture with nothing to
    /// do, or at your elbow with a card. Called from <c>AdvanceWalkers</c>, which owns the clock.
    /// </summary>
    /// <returns>Whether anything happened that the page should redraw for.</returns>
    /// <param name="afoot">The room's own feet — the excursion's underground, the docked bar's ashore
    /// (#973 L0). One stepper, because he is the same man in both rooms.</param>
    private bool StepTheRep(
        IList<Walker> afoot, Walker who, double dt, IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        // #1061 · …AND ONE THAT ENDS THE ORDINARY WAY. His shift is over and he is walking out through a leaf
        // that does not open for the captain: the route running out is the end of him, exactly as it is for a
        // regular who has finished a drink, and nothing is said about it.
        if (who.For == Errand.RepLeaving)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (who.Walk.Afoot)
            {
                return false;
            }

            afoot.RemoveAt(slot);
            _repStandingAt = null;
            return true;
        }

        if (who.Walk.State != NpcWalk.Doing.Arrived)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (who.Walk.Afoot)
            {
                return false;
            }

            if (who.Walk.State != NpcWalk.Doing.Arrived)
            {
                // The floor refused him somewhere between the door and the table. He is standing wherever it
                // stopped him, which is honest — and the mark he could not reach is one he does not queue for.
                afoot.RemoveAt(slot);
                HeIsStandingHere(who);
                if (who.For == Errand.RepRounds && who.Table >= 0)
                {
                    _repMarksWorked++;
                }

                _repMoveOnAt = SimTime + RepDwellSeconds;
                return true;
            }

            // The frame he lands on is the frame he speaks on, and only that frame.
            if (who.For == Errand.RepPitching)
            {
                HeReachesYourTable();
            }
            else
            {
                // #1061 · …and at somebody ELSE'S table he says nothing at all. The pause is the patter, and
                // its length is the round's own — a fact about this watch and never a clock reading.
                _repMoveOnAt = SimTime + HisBeatAt(who.Table);
            }

            who.Walk.LookTowards(_avatarX, _avatarY);
            return true;
        }

        // Standing. A man on his rounds moves on when he has stood long enough; a man mid-pitch waits for
        // an answer however long that takes, because the answer is the whole of the scene.
        if (who.For == Errand.RepRounds && SimTime >= _repMoveOnAt)
        {
            afoot.RemoveAt(slot);

            // #1061 · A pause at the table, and then on — FROM HERE. He is taken off the list and put back on
            // it in the same frame by the planner, so the room never draws him vanishing off a top and coming
            // back out of a cellar.
            HeIsStandingHere(who);
            if (who.Table >= 0)
            {
                _repMarksWorked++;
            }

            return true;
        }

        who.Walk.LookTowards(_avatarX, _avatarY);
        return false;
    }

    /// <summary>#1061 · Remember where his feet are, so the next leg of his round begins at them.</summary>
    private void HeIsStandingHere(Walker who) =>
        _repStandingAt = new DeckReachability.Point(who.Walk.X, who.Walk.Y);

    // ── The pitch ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// He is at your elbow, and he is delighted.
    ///
    /// <para>#1052 · …UNLESS SOMETHING IS ALREADY WEARING THE SCRIM, in which case he waits. This is the
    /// live collision the one-scrim law was written for: a captain reading the galley card at a bar top
    /// while the salesman crosses the floor got two full-viewport dims on top of each other and a room the
    /// #784 dock exists to keep visible went black. He is HELD rather than turned away — his legs already
    /// brought him here and he is standing at the table — and <c>PumpTheScrimQueue</c> lets him speak on
    /// the first frame the glass is clear. The meeting is still counted where it always was, inside the
    /// raise, because the bleed's cadence counts MEETINGS and a pitch that never landed was not one.</para>
    /// </summary>
    private void HeReachesYourTable() => RaiseAScrimCard(HisPitchGoesUp, () => CaptainIsSeated);

    /// <summary>The pitch itself, once the glass is his. Everything this method does is what "he is at your
    /// elbow" means, which is why the arbiter holds the WHOLE of it rather than the card alone.</summary>
    private void HisPitchGoesUp()
    {
        _repMeetings++;
        _repBleeding = NebulaRep.BleedsThePreviousName(
            _activeThreadId ?? "", _repMeetings, RetiredCaptainCount);
        _repNameOnFile = RepNameOnFile(_repBleeding);
        _repSaid = null;
        _repCard = NebulaRep.PitchFor(_insurance.Tier, _repNameOnFile, _repBleeding);

        // He is a relationship, not a vending machine: the book knows him from the first hello.
        _contacts.AddGoodwill(NebulaRep.ContactId, NebulaRep.DisplayName, 0);
        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    /// <summary>Take him off the table and off the card. The body stays on the floor for whatever errand
    /// put it there; only the panel goes.</summary>
    private void CloseTheRepsCard()
    {
        _repCard = null;
        _repSaid = null;
        _repBleeding = false;
        StateHasChanged();
    }

    /// <summary>
    /// THE CAPTAIN ANSWERS. One door for every button on his card, so a move's meaning cannot drift from
    /// the words on it.
    /// </summary>
    private void AnswerTheRep(NebulaRep.RepMove move)
    {
        if (_repCard is null)
        {
            return;
        }

        switch (move)
        {
            case NebulaRep.RepMove.BuyBasic:
                BuyFromTheRep(InsuranceTier.Basic);
                break;

            case NebulaRep.RepMove.BuyPremium:
                BuyFromTheRep(InsuranceTier.Premium);
                break;

            case NebulaRep.RepMove.AlreadyHaveAPolicy:
                TellHimYouAlreadyHaveOne();
                break;

            case NebulaRep.RepMove.ThatsNotMyName:
                TellHimThatIsNotYourName();
                break;

            case NebulaRep.RepMove.NotToday:
            case NebulaRep.RepMove.GoodDay:
                SendHimToTheBar(move == NebulaRep.RepMove.NotToday);
                break;
        }
    }

    /// <summary>
    /// #973 L2 · THE SIGNING COMES BACK. The captain's line is available whether or not it is true, and it
    /// costs nothing and buys nothing — except that saying it out loud to a man holding the file puts you
    /// back at the counter where you signed.
    ///
    /// <para>The plate is HOSTED on his card (#777's shape, second case): the seam spends the cadence,
    /// files the seen-set and writes the words into the ledger, and the panel already on the screen carries
    /// the picture. The subject stamps the captain's LIFE, which is how "once per subject" becomes "once
    /// per life" with no rebirth hook for anybody to forget.</para>
    /// </summary>
    private void TellHimYouAlreadyHaveOne()
    {
        _repSaid = NebulaRep.PolicyClaimReply(RetiredCaptainCount);

        if (_repSigningToldInLife != CaptainsLife)
        {
            _repSigningToldInLife = CaptainsLife;
            RaiseStoryBeat(StoryBeats.Beat.Flashback, NebulaRep.SigningMemoryId);

            // #973 L3 · …and the afternoon goes into the BLACK BOOK. The plate is what was in the room; the
            // paragraph is a held memory, marked MINE and tagged money, and after a rebirth it GROWS the one
            // line about the hand. Filed on the same edge as the plate and behind the same once-per-life
            // latch — this lane adds no second condition for anybody to keep in step with the first.
            FileTheSigningSheet();
        }

        StateHasChanged();
    }

    /// <summary>He read the wrong line off the file, and the captain says so. He does not explain, and he
    /// never will — but the ship's ledger keeps the one line, so the black book can find it later.</summary>
    private void TellHimThatIsNotYourName()
    {
        _repSaid = NebulaRep.BleedApology;
        LogAutopilotEvent(NebulaRep.BleedLedgerNote(_repNameOnFile));
        _repBleeding = false;
        _repCard = NebulaRep.PitchFor(_insurance.Tier, ActiveCaptainName, bleeding: false);
        StateHasChanged();
    }

    /// <summary>
    /// A SALE. The policy is set the one way a policy is ever set — <see cref="NebulaRep.PolicyAfterBuying"/>
    /// — so #227's vendor lane re-prices one function rather than hunting for a second seam.
    /// </summary>
    private void BuyFromTheRep(InsuranceTier tier)
    {
        int price = NebulaRep.PremiumFor(tier);
        if (_credits < price)
        {
            // The page's own voice and not his: he has no line for a captain who cannot pay, and putting
            // words in his mouth here would be the one place in this feature the salesman got a new script.
            ShowPulseMessage("Not enough credits for that premium.");
            return;
        }

        _credits -= price;
        _insurance = NebulaRep.PolicyAfterBuying(tier, SimTime);
        _contacts.ApplyCredit(
            NebulaRep.ContactId, NebulaRep.DisplayName,
            new CreditTransaction(CreditKind.Premium, 0, SimTime, $"{tier} premium · {price} cr"));
        _contacts.AddGoodwill(NebulaRep.ContactId, NebulaRep.DisplayName, 1);
        LogAutopilotEvent(NebulaRep.SaleLedgerNote(tier, price, ActiveCaptainName));
        RequestVaultSave();

        // He says nothing new about a sale, and that is the character: the card simply refreshes to the
        // tier you now hold and he is already talking about the next one up. His Premium line — "Nothing to
        // sell you, then… It is a comfort, isn't it, a file in order" — is the reward for going all the way.
        _repSaid = null;
        _repCard = NebulaRep.PitchFor(_insurance.Tier, _repNameOnFile, bleeding: false);
        StateHasChanged();
    }

    /// <summary>No. He withdraws to the counter and does not come back this visit — and next visit he
    /// starts over, which is the joke.</summary>
    private void SendHimToTheBar(bool told)
    {
        if (told)
        {
            _repMemory = _repMemory.AtVisit(_repVisitIndex).WithNo();
            ShowPulseMessage(NebulaRep.WithdrawLine);
        }

        CloseTheRepsCard();

        // #973 L0 · Whichever room he is standing in. The walker list is the truth about who is afoot and
        // there are two of them now — a withdrawal that only knew about the Hive would leave a salesman
        // standing at a bar table with no card in his hand, which is the state #731's escort branch refuses.
        if (_surface is { } ex && TheRepAfoot(ex.Walkers) is { } underground)
        {
            // #1061 · …and he walks on from where his feet actually are, which is your elbow. The round is not
            // over: there are other tables in this room, and the man who has just been told no goes to them.
            HeIsStandingHere(underground);
            ex.Walkers.Remove(underground);
        }

        if (TheRepAfoot(_barAfoot) is { } ashore)
        {
            HeIsStandingHere(ashore);
            _barAfoot.Remove(ashore);
        }

        _repMoveOnAt = SimTime + RepDwellSeconds;
    }

    /// <summary>…and if the captain walks past him afterwards, he says the only thing he has left. Once a
    /// visit: a man repeating it every time you cross the room is a different, worse joke.</summary>
    private void MaybeSayHeIsOnlyPassing(IReadOnlyList<Walker> afoot)
    {
        if (_repSaidPassing || _repMemory.MayApproach(_repVisitIndex) || TheRepAfoot(afoot) is not { } who)
        {
            return;
        }

        double dx = who.Walk.X - _avatarX;
        double dy = who.Walk.Y - _avatarY;
        if ((dx * dx) + (dy * dy) > RepPassingReachDu * RepPassingReachDu)
        {
            return;
        }

        _repSaidPassing = true;
        ShowPulseMessage(NebulaRep.PassingLine);
    }
}
