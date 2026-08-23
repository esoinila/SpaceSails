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

    /// <summary>Which fixture on his beat he is heading for.</summary>
    private int _repPost;

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
        _repCard = null;
        _repSaid = null;
        _repBleeding = false;
        _repSaidPassing = false;
        _repPost = 0;
        _repMoveOnAt = 0;

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
            if (w.For is Errand.RepRounds or Errand.RepPitching)
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>
    /// PUT HIM ON THE FLOOR, or move him along it. The first walk of a visit comes in through a door —
    /// #731's idiom, and the same one the haulier uses — and every walk after it is a drift between the
    /// fixtures of his beat, or a crossing to the captain's table.
    /// </summary>
    private bool SendTheRepIn(SurfaceExcursion ex, UndergroundComplex.Amenity amenity, double dt)
    {
        if (ex.Walkers.Count >= WalkerBand || SimTime < _repMoveOnAt)
        {
            return false;
        }

        _ = dt;
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());

        // He crosses to the table only when the captain is sitting alone at one and has not already sent
        // him away this visit. Everything else on his beat is furniture he stands beside.
        if (TheCaptainIsSittingAlone(out int tableIndex)
            && _repMemory.MayApproach(_repVisitIndex)
            && TopOn(ex, amenity, tableIndex) is { } top
            && ChairOppositeTheCaptain(in top, walls) is { } beside)
        {
            return PlanTheRep(ex, floor, walls, beside, Errand.RepPitching, tableIndex);
        }

        IReadOnlyList<DeckReachability.Point> beat = TheRepsBeat(ex, amenity, floor, walls);
        if (beat.Count == 0)
        {
            return false;
        }

        _repPost = (_repPost + 1) % beat.Count;
        return PlanTheRep(ex, floor, walls, beat[_repPost], Errand.RepRounds, -1);
    }

    /// <summary>Plan one of his walks. He starts from where he is if he is already in the room, and from
    /// the doorstep of a door he does not have to be let through if this is his entrance.</summary>
    private bool PlanTheRep(
        SurfaceExcursion ex, UndergroundComplex.FloorPlan floor,
        IReadOnlyList<SurfaceCollision.Segment> walls, DeckReachability.Point to, Errand errand, int table)
    {
        int index = Egress.DoorFor(ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch, NebulaRep.ContactId, floor.Locked);
        if (index < 0 || index >= floor.Locked.Count)
        {
            return false;
        }

        UndergroundComplex.LockedDoor door = floor.Locked[index];
        if (Egress.StandingPlaceAt(in door, DeckPlan.AvatarRadius, walls, to.X, to.Y) is not { } doorstep)
        {
            return false;
        }

        if (OnFoot(NebulaRep.Plate, new NpcWalk.Bound("", to.X, to.Y), doorstep, walls,
                   NpcWalk.NoPersonalSpace) is not { } walk)
        {
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = walk, Table = table, For = errand });
        StateHasChanged();
        return true;
    }

    /// <summary>
    /// HIS BEAT — the two or three fixtures a man with nothing to do stands beside. The counter first,
    /// because that is where he says he will be, then the ends of the room's own tops.
    ///
    /// <para>Read off the floor's published geometry and never carved here: a second list of where the
    /// counter is would be this repo's oldest bug class with a salesman walking through it.</para>
    /// </summary>
    private static IReadOnlyList<DeckReachability.Point> TheRepsBeat(
        SurfaceExcursion ex, UndergroundComplex.Amenity amenity, UndergroundComplex.FloorPlan floor,
        IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        List<DeckReachability.Point> beat = [];

        if (amenity.Hall is { } hall)
        {
            foreach (UndergroundComplex.CounterPlace place in hall.CounterRow)
            {
                if (place.Seated || SurfaceCollision.Blocked(place.X, place.Y, DeckPlan.AvatarRadius, walls))
                {
                    continue;
                }

                beat.Add(new DeckReachability.Point(place.X, place.Y));
                break;   // one standing place at the counter is the bar; the rest of the row is other people's
            }
        }

        foreach (CanteenRegulars.TableSeat top in
                 CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, amenity, ex.CanteenWatch, ex.HallStoodUp))
        {
            if (WhereABodyStandsAt(in top, walls) is { } beside)
            {
                beat.Add(beside);
            }

            if (beat.Count >= 3)
            {
                break;
            }
        }

        _ = floor;
        return beat;
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
        if (who.Walk.State != NpcWalk.Doing.Arrived)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (who.Walk.Afoot)
            {
                return false;
            }

            if (who.Walk.State != NpcWalk.Doing.Arrived)
            {
                // The floor refused him. He simply is not there, which is honest — and nothing was said.
                afoot.RemoveAt(slot);
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
                _repMoveOnAt = SimTime + RepDwellSeconds;
            }

            who.Walk.LookTowards(_avatarX, _avatarY);
            return true;
        }

        // Standing. A man on his rounds drifts on when he has stood long enough; a man mid-pitch waits for
        // an answer however long that takes, because the answer is the whole of the scene.
        if (who.For == Errand.RepRounds && SimTime >= _repMoveOnAt)
        {
            afoot.RemoveAt(slot);
            return true;
        }

        who.Walk.LookTowards(_avatarX, _avatarY);
        return false;
    }

    // ── The pitch ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>He is at your elbow, and he is delighted. The meeting is counted here and nowhere else,
    /// because the bleed's cadence is counted in MEETINGS and a pitch that never landed was not one.</summary>
    private void HeReachesYourTable()
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
            ex.Walkers.Remove(underground);
        }

        if (TheRepAfoot(_barAfoot) is { } ashore)
        {
            _barAfoot.Remove(ashore);
        }

        _repPost = -1;                                 // the counter is the first stop on his beat
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
