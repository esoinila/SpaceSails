using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #973 L5b · <b>THE WALK-IN.</b> A woman comes in through a door, crosses a classy room on the real A*, and
/// stops at the table of a captain who is sitting alone.
///
/// <para><b>Owner, Addendum 3:</b> <i>"that story is never found sitting down — she makes an entrance at some
/// classy place (a jazz bar), crosses the room, and comes to our table when we are alone, and asks."</i> The
/// entrance is the showcase: door → floor → table, watched, with the room reacting, and nothing anywhere
/// teleports.</para>
///
/// <h3>Built out of four things that already existed</h3>
///
/// <para><b>The legs</b> are #973 L0's <c>ApproachTheTable</c> — the hook that file was written for, and its
/// own summary says so: <i>"the one verb any NPC in a docked station's bar uses to walk up to the captain,
/// and the hook L5b calls."</i> <b>The chair</b> is #973 L5b's eighth seat, which is what made
/// <c>TheCaptainIsSittingAloneInTheBar</c> capable of answering true at all. <b>The cadence</b> is #664's:
/// rare, once per subject, and the subject is HER. <b>The words</b> are Fable's, in <see cref="WalkIn"/>, and
/// not one of them is typed twice.</para>
///
/// <h3>The three gates, in the order the room asks them</h3>
///
/// <para>A CLASSY VENUE (<c>ArrivalTube.Tier.GreatPort</c> — never a canteen, never a Hive floor), then a
/// RARE visit, then a captain SEATED AND ALONE at a top. The order matters: the first two are facts about a
/// world and are asked once a visit; the third is a fact about a posture and is asked every frame, because
/// standing up is how a captain says no before she has said anything.</para>
///
/// <h3>What arriving means</h3>
///
/// <para>She JOINS THE SITTING — a guest on the open <c>TableTalk</c>, which is how "the one who comes to your
/// table" has always worked — and there is NO ninth construction site here: the sitting was opened by the
/// captain's own press and she takes a chair at it. Her card carries the beat
/// (<c>StoryBeats.Presentation.Hosted</c>), so the seam spends the cadence, files the seen-set and writes her
/// entrance into the ledger while her own portrait is the picture on the screen.</para>
///
/// <para>If the captain is not alone when she gets there she does what a person in a bar does: <b>she waits
/// at the counter.</b> That is L0's own fallback and not a second behaviour of this file's.</para>
///
/// <para><b>NOT BUILT, and said out loud rather than faked: "and tries once more that visit".</b> The brief
/// asks for a second attempt, and the only way to plan one with what the room publishes today is to take her
/// off the floor and walk her in through a back-room leaf again — so a player watching the counter would see
/// her vanish from it and come back out of the cellar. That is a worse lie than not retrying, and the honest
/// version wants a "walk from where you are standing" that the bar's planner does not have yet. Flagged for
/// whoever adds it: the gate to relax is <see cref="TheWalkInAfoot"/> in the plan branch below.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Which visit's room this file is remembering. Null off a berth; a different berth is a
    /// different evening — the same fold the salesman keeps.</summary>
    private string? _walkInVisitBody;

    /// <summary>The running count of docked visits this thread has made. The rarity's clock.</summary>
    private int _walkInVisitIndex = -1;

    /// <summary>Whether the rota and the tier between them allow a walk-in at THIS berth this visit.</summary>
    private bool _walkInPossibleHere;

    /// <summary>#973 L5b dev cheat (<c>/map?walkin=1</c>, <c>/map?walkin=0</c>): force her on or off this
    /// berth. Null is the shipped rota. It forces WHETHER and never WHO, what she says, what the job is or
    /// whether it is a setup — all four are the ones a captain gets.</summary>
    private bool? _walkInCheat;

    /// <summary>Whether she has already crossed this floor this visit. She asks once an evening whatever the
    /// answer was: a woman who comes back after a no is a different, worse scene.</summary>
    private bool _walkInAskedThisVisit;

    /// <summary>Whether the room has already looked at the door this visit. The toast is a moment, not a
    /// weather report.</summary>
    private bool _walkInRoomLooked;

    /// <summary>Who is crossing the floor, or null when nobody is.</summary>
    private WalkIn.Who? _walkInWho;

    /// <summary>Her card, up only while she is standing at the table. The whole of what <c>TheHostIsUp</c>
    /// asks about, and it cannot outlive her body — the state #731's escort branch was written to refuse.</summary>
    private WalkIn.Who? _walkInCard;

    /// <summary>Whether she has been answered, which is the same thing as her not being wanted any more. The
    /// walk out is the room's: <c>StepAnApproach</c> sees the gate go false and walks her to the counter.</summary>
    private bool _walkInAnswered;

    /// <summary>#973 L5b · Which walk-ins the SPREAD has found out, by job id. <b>L3 writes this</b>; until it
    /// does the set is empty and every setup card says nothing, which is the shipped behaviour and the
    /// correct one — the player may simply go.</summary>
    private readonly HashSet<string> _walkInSetupsRevealed = new(StringComparer.Ordinal);

    // ── THE VISIT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A DIFFERENT BERTH IS A DIFFERENT EVENING. The one place forgetting happens; everything else in this
    /// file only ever reads what is remembered here.
    /// </summary>
    private void EnsureWalkInVisit(string? berth)
    {
        if (_walkInVisitBody == berth)
        {
            return;
        }

        _walkInVisitBody = berth;
        _walkInWho = null;
        _walkInCard = null;
        _walkInAnswered = false;
        _walkInAskedThisVisit = false;
        _walkInRoomLooked = false;

        if (berth is null)
        {
            _walkInPossibleHere = false;
            return;
        }

        _walkInVisitIndex++;

        // THE CLASSY-VENUE GATE, and it is a gate about the WORLD rather than about the art: a great port is
        // the tier that has a long walk in, a queue and a room worth making an entrance into (ArrivalTube).
        // Asked here, with the rota, because both are facts about this evening and neither can change while
        // the captain is standing in the room.
        bool classy = _ephemeris is not null
            && ArrivalTube.TierFor(_ephemeris, berth) == ArrivalTube.Tier.GreatPort;

        _walkInPossibleHere = _walkInCheat
            ?? (classy && WalkIn.CouldWalkInThisVisit(_activeThreadId ?? "", berth, _walkInVisitIndex));
    }

    // ── ONE FRAME OF HER EVENING ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L5b · Called once a frame from the docked bar's own metabolism, beside the salesman's. Does
    /// nothing at all unless this evening allows a walk-in, the captain is in the room, and nobody has
    /// crossed the floor yet.
    /// </summary>
    private void AdvanceTheWalkIn(in HavenInterior.BarFloor bar)
    {
        EnsureWalkInVisit(bar.BodyId);
        if (!_walkInPossibleHere || !InTheBar(in bar))
        {
            return;
        }

        // Her card cannot outlive her body. The walker list is the truth about who is standing at the table,
        // and a card left up over an empty chair is the exact state #731's escort branch refuses.
        if (_walkInCard is not null && TheWalkInAfoot() is null)
        {
            CloseHerCard();
            return;
        }

        // …AND SHE IS SENT ONCE, NOT ONCE A FRAME. `_walkInAskedThisVisit` is set when she ARRIVES, which is
        // hundreds of frames after she sets off — so the plan gate has to be her BODY, not her ask. FOUND BY
        // LOOKING: the strip read "seats 4, 0 chairs free" at a top with one woman standing at it, because
        // three of her had crossed the floor and every one of them had taken a chair on the way in. This
        // repository's third named bug class (the drawn room and the walked room disagreeing), in a bar.
        if (_walkInAskedThisVisit || TheWalkInAfoot() is not null || _barAfoot.Count >= WalkerBand
            || !TheCaptainIsSittingAloneInTheBar())
        {
            return;
        }

        // WHO, by seed and by the world: the fling if this thread cast her and posted her at THIS great
        // port's claims desk (L5a), and otherwise the stranger the bar has never seen.
        WalkIn.Who who = WalkIn.Cast(OldCrew.FlingIsAt(TheOldCrew, bar.BodyId));

        // …and the cadence is asked BEFORE anybody walks. #664's once-per-subject is a rule about a moment,
        // and a woman who crossed the floor only to have the beat refused at the far end would be a body
        // walking into a scene nobody was allowed to be told about.
        if (!BeatMaySpeak(StoryBeats.Beat.WalkIn, WalkIn.Subject(who)))
        {
            _walkInAskedThisVisit = true;
            return;
        }

        _walkInWho = who;
        if (!ApproachTheTable(WalkIn.Plate(who), TheWalkInIsStillWanted, SheReachesYourTable))
        {
            _walkInWho = null;
            return;
        }

        // THE ROOM NOTICES HER FIRST. Said as she comes through the leaf and not when she arrives — the whole
        // sentence is about the gap between the room looking up and the captain doing it.
        if (!_walkInRoomLooked)
        {
            _walkInRoomLooked = true;
            ShowPulseMessage(WalkIn.TheRoomLooks);
        }
    }

    /// <summary>The walker that is her, if she is on the floor. By plate, because her errand is the room's
    /// ordinary <see cref="Errand.Approaching"/> and the salesman is told apart by his.</summary>
    private Walker? TheWalkInAfoot()
    {
        if (_walkInWho is not { } who)
        {
            return null;
        }

        string plate = WalkIn.Plate(who);
        foreach (Walker w in _barAfoot)
        {
            if (w.For == Errand.Approaching
                && string.Equals(w.Walk.Plate, plate, StringComparison.Ordinal))
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>
    /// #973 L5b · IS SHE STILL WANTED — the gate <c>ApproachTheTable</c> asks when the walk is planned and
    /// again on the frame it lands, and every frame she is standing there afterwards.
    ///
    /// <para>Two answers, and the fork is the scene rather than a convenience. <b>Before she arrives</b> it is
    /// the seated-and-alone predicate, exactly as the brief says: a captain who stood up, walked off or was
    /// joined has ended the scene she was walking into. <b>Once she is at the table</b> the captain is not
    /// alone any more — she is the company — so the question becomes whether he is still in the chair. Asking
    /// the first one after she sat down would have her turn round and leave because she had arrived.</para>
    /// </summary>
    private bool TheWalkInIsStillWanted() =>
        !_walkInAnswered
        && (_walkInCard is null
            ? TheCaptainIsSittingAloneInTheBar()
            : CaptainIsSeated && SeatedTable is { Bench: false, Office: false });

    // ── SHE REACHES THE TABLE ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L5b · SHE IS AT YOUR ELBOW. Fired once, on the landing frame, by the room's own stepper.
    ///
    /// <para>She JOINS the sitting rather than opening one: the chair the captain took is still the chair the
    /// captain took, and a second construction site for a guest would be an eighth-and-a-half way to sit
    /// down. <c>Solo</c> goes false because somebody IS at the top now — the privacy ladder reads that, and a
    /// case laid out under a woman who walked over to look at it is not a case laid out in private.
    /// <c>TheyCameToYou</c> stays FALSE on purpose: #865's card belongs to the visitor scene's own ladder,
    /// and HER face is on her own card. Two modals with the same woman on them is #777's stacked card.</para>
    /// </summary>
    private void SheReachesYourTable()
    {
        if (_walkInWho is not { } who || SeatedTable is not { } t)
        {
            return;
        }

        _walkInAskedThisVisit = true;
        t.Solo = false;
        t.Plate = WalkIn.Plate(who);
        t.Free = Math.Max(0, t.Free - 1);

        // The card goes up BEFORE the beat is raised, and that order is the whole of #777's law: a hosted
        // beat is only counted as told once its canvas really is on the screen (TheHostIsUp reads this very
        // field, one statement later).
        _walkInCard = who;

        // She is a relationship from the first hello — the book knows her name before she has asked for
        // anything, which is what lets L3 stack her note under her own thread.
        _contacts.AddGoodwill(WalkIn.ContactId(who), WalkIn.Name(who), 0);

        RaiseStoryBeat(StoryBeats.Beat.WalkIn, WalkIn.Subject(who));
        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    /// <summary>Take her off the card. The body stays wherever the room has it; only the panel goes.</summary>
    private void CloseHerCard()
    {
        _walkInCard = null;
        StateHasChanged();
    }

    // ── THE ANSWER ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L5b · THE CAPTAIN ANSWERS, and there are only two answers because there is nothing to bargain
    /// about. Either way she goes — on her own legs, to the counter, because the gate she was walked in on
    /// stops being true the moment this method returns.
    /// </summary>
    private void AnswerTheWalkIn(bool yes)
    {
        if (_walkInCard is not { } who)
        {
            return;
        }

        _walkInAnswered = true;

        if (!yes)
        {
            // Her line, and nothing else: no note, no job, no re-ask this visit. It is pulsed rather than
            // held on a card because the card is what she is leaving.
            ShowPulseMessage(WalkIn.IfNo(who));
            CloseHerCard();
            SheLeavesTheTable();
            return;
        }

        TakeHerJob(who);
        LeaveHerNoteOnTheTable(who);
        CloseHerCard();
        SheLeavesTheTable();
    }

    /// <summary>The table is the captain's own again. The chair she was in comes back, the plate goes back to
    /// being his, and the strip carries on exactly as it did before she crossed the floor.</summary>
    private void SheLeavesTheTable()
    {
        if (SeatedTable is { } t)
        {
            t.Solo = true;
            t.Plate = SittingAlone.OwnTablePlate;
            t.Free = Math.Max(0, t.Free + 1);
        }

        RequestVaultSave();
        StateHasChanged();
    }

    // ── HER NOTE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L5b · SHE LEAVES A NOTE ON THE TABLE — a held memory marked <i>hers</i>, tagged <i>love</i>, in
    /// her hand and handed over by her rather than surfacing out of the captain's own greyed book.
    ///
    /// <para>It is the first sheet in the book that is evidence of ANOTHER PERSON'S MEMORY OF YOU, which is
    /// the whole reason the mark exists. <b>The SPREAD behaviours are L3's</b> — laying it beside the
    /// fleet-day page, or beside this job's own first slip, is the reconcile that finishes the unfinished
    /// line and can reveal a setup. This file exposes the pairing (<see cref="WalkIn.ReconcilesAgainst"/>)
    /// and creates the sheet through the shipped Core API; it renders nothing.</para>
    /// </summary>
    private void LeaveHerNoteOnTheTable(WalkIn.Who who)
    {
        string id = WalkIn.NoteId(who);
        if (HeldMemory.Find(_heldMemories, id) is not null)
        {
            return;
        }

        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            id, HeldMemory.Mark.Hers, WalkIn.Theory, WalkIn.NoteText(who), [WalkIn.Name(who)], SimTime));

        LogAutopilotEvent($"🎞 {WalkIn.Name(who)} leaves a note on the table.");
    }

    // ── THE JOB ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L5b · THE FAVOUR, AS A JOB — through the shipped quest ledger and #972's plain vocabulary, so it
    /// wears the same four lines every other job in the game wears and the player has nothing new to learn.
    ///
    /// <para>What it does not wear is a price. The verb is <b>FIND</b>, the payout line is a dash, the size
    /// word is <b>for her</b>, and the row is tagged <b>love</b> by construction — the first job in this game
    /// that is. The effort line is measured off the live world like every other job's
    /// (<c>JobFactsFor</c> → <c>JobEffort.LaneSeconds</c>); nothing about a favour is estimated.</para>
    /// </summary>
    private void TakeHerJob(WalkIn.Who who)
    {
        if (WhereWhatSheWantsIs(who) is not { } sought)
        {
            // No berth in this world to send anybody to. She still leaves the note — the evening happened —
            // and the game says nothing about the job it could not build, which is the honest answer.
            return;
        }

        var job = new Quest(
            $"walkin-{++_questSeq}", QuestKind.WalkIn, WalkIn.Name(who),
            TargetShipId: "", TargetCallsign: WalkIn.TargetName(who),
            Title: WalkIn.JobTitle(who), Blurb: WalkIn.TheStory(who),
            Reward: 0,
            // Back to HER: the person is the destination, and the berth is only where she is standing.
            DestBodyId: _dockedHavenId,
            SourceBodyId: sought,
            Pin: null,
            Theory: WalkIn.Theory);

        _quests.Add(job);
        ShowPulseMessage($"{MissionBrief.Receipt(ContractKind.WalkIn, job.Giver)} "
            + $"{MissionBrief.NextLine(FactsFor(job))}");
        RequestVaultSave();
    }

    /// <summary>
    /// #973 L5b · WHERE THE THING SHE WANTS IS — by seed, off the world's own list of berths, and never here.
    ///
    /// <para>Ilse's REACH was impounded and renamed, so her paper is at a port that keeps registries: a GREAT
    /// PORT, the same tier the claims desks are posted at (L5a's <c>OldCrew.BerthsFor</c>). Nadia's brother
    /// was last seen at a clinic, and a clinic is at any berth at all. Both are drawn on the shared dice off
    /// the thread and the woman, so the same universe sends the captain to the same place twice.</para>
    /// </summary>
    private string? WhereWhatSheWantsIs(WalkIn.Who who)
    {
        if (_ephemeris is null)
        {
            return null;
        }

        IReadOnlyList<OldCrew.Berth> all = OldCrew.BerthsOf(_ephemeris);
        List<string> choices = [];
        foreach (OldCrew.Berth b in all)
        {
            if (string.Equals(b.Id, _dockedHavenId, StringComparison.Ordinal))
            {
                continue;   // she is here; what she is looking for is not.
            }

            if (who == WalkIn.Who.Ilse && b.Tier != ArrivalTube.Tier.GreatPort)
            {
                continue;   // a renamed hull's paper is held where paper is held.
            }

            choices.Add(b.Id);
        }

        // …and if this world has no great port but the one she is standing in, the registry falls back to
        // wherever there IS a berth, exactly as L5a's postings do rather than leaving her unanswered.
        if (choices.Count == 0)
        {
            foreach (OldCrew.Berth b in all)
            {
                if (!string.Equals(b.Id, _dockedHavenId, StringComparison.Ordinal))
                {
                    choices.Add(b.Id);
                }
            }
        }

        return choices.Count == 0
            ? null
            : choices[(int)(DiceRule.Seed($"walkin|where|{_activeThreadId ?? ""}|{WalkIn.Subject(who)}")
                % (ulong)choices.Count)];
    }

    // ── THE TWO SITES ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L5b · <b>THE FIRST SITE, AND THE SENTENCE THAT DOES NOT FINISH.</b> The captain docks where she
    /// said it would be, finds it, and the line fires: <i>"I haven't felt like this since —"</i>.
    ///
    /// <para>Owner's own sentence and owner's own ruling (18): it does not finish, because the page it ends
    /// on is grey. What lands in the book is a sheet with that unfinished text on it — a real page with a
    /// real id, so that when the job completes (or L3's SPREAD reconciles her note) the SAME sheet is
    /// rewritten with the ending rather than a second one being filed beside it.</para>
    /// </summary>
    private void YouFindWhatSheAskedFor(Quest q)
    {
        AdvanceMission(q, QuestState.PickedUp,
            $"{MissionBrief.NextPrefix}{MissionBrief.Action(FactsFor(q))}");

        WalkIn.Who who = WhoAsked(q);
        FileTheSinceSheet(who, WalkIn.Unfinished);

        // …and the paper itself, which is what L3's SPREAD lays her note beside for the stranger's variant.
        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            WalkIn.FirstSlipId(q.Id), HeldMemory.Mark.NotAnyones, WalkIn.Theory,
            q.Blurb, [WalkIn.Name(who), q.TargetCallsign], SimTime));

        RaiseStoryBeat(StoryBeats.Beat.Flashback, WalkIn.SinceSubject);
        RequestVaultSave();
    }

    /// <summary>
    /// #973 L5b · <b>THE LAST SITE: YOU COME BACK AND TELL HER.</b> The sentence finishes here, and so — if
    /// the seed cast this one as a setup — does the other half of the owner's line about doing something
    /// dangerous because of her.
    /// </summary>
    private void YouComeBackAndTellHer(Quest q)
    {
        WalkIn.Who who = WhoAsked(q);

        AdvanceMission(q, QuestState.Complete, $"You tell {q.Giver} where it is. She does not write it down.");
        FileTheSinceSheet(who, WalkIn.Finished(who));

        // FEMME FATALE BY RULE. One in three, decided by the seed before the captain ever said yes, and paid
        // here: a customs post that has been waiting for the ship this errand was flown in. Owed to whoever
        // runs this berth (#715's ledger), because that is the only kind of heat this game has — never a
        // shared list, never a number in the sky.
        if (WalkIn.IsASetup(_activeThreadId ?? "", who))
        {
            BankTheCrossing(IllegalHeat.Charge(_dockedHavenId ?? "", IllegalHeat.Crossing.RefusedCardAtAGate));
            LogAutopilotEvent($"🌡 {IllegalHeat.TheyRememberYouHere}");
        }

        RequestVaultSave();
    }

    /// <summary>The sheet the unfinished line lives on, written by id so the ending REPLACES the beginning
    /// rather than filing a second page beside it. <see cref="HeldMemory.Put"/> is replace-by-id, which is
    /// exactly the shape this needs.</summary>
    private void FileTheSinceSheet(WalkIn.Who who, string text) =>
        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            WalkIn.SinceSubject, HeldMemory.Mark.Mine, WalkIn.Theory, text, [WalkIn.Name(who)], SimTime));

    /// <summary>Which of the two asked for this job, off the row itself — the giver's name is hers, and a
    /// second field carrying the same fact is a second answer to one question.</summary>
    private static WalkIn.Who WhoAsked(Quest q) =>
        string.Equals(q.Giver, WalkIn.Name(WalkIn.Who.Ilse), StringComparison.Ordinal)
            ? WalkIn.Who.Ilse
            : WalkIn.Who.Nadia;

    // ── WHAT L3 SPENDS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L5b · <b>THE GREY LINE ON A WALK-IN'S CARD, OR NOTHING.</b> The predicate L3 flips and this file
    /// publishes: until the SPREAD has laid her note beside a money-tagged slip and found that her hand and
    /// the desk's are the same hand, the card says nothing at all about the setup. The player may simply go,
    /// which is the design.
    /// </summary>
    private string? WalkInCardWarning(Quest q) =>
        q.Kind != QuestKind.WalkIn
            ? null
            : WalkIn.SetupCardLine(
                WalkIn.IsASetup(_activeThreadId ?? "", WhoAsked(q)), _walkInSetupsRevealed.Contains(q.Id));

    /// <summary>
    /// #973 L5b · <b>THE TWO AUTHORED REVEALS — the walk-in's half of L3's SPREAD.</b> Plugged into
    /// <c>Map.Book.AnAuthoredRevealFor</c>, which <see cref="SpreadReconcile.Lay"/> asks FIRST, because a
    /// fact about a story cannot be derived from names and numbers.
    ///
    /// <para><b>One · the sentence finishes.</b> Her note laid beside the page it is about — the fleet-day
    /// for the fling (the one page the filing line cannot grey, because the service filed it), this job's own
    /// first slip for the stranger — and the pairing is Core's (<see cref="WalkIn.ReconcilesAgainst"/>), so
    /// this lane and that one cannot come to two views of one pair. Owner's ruling 18: the job finishes the
    /// line, or the note does.</para>
    ///
    /// <para><b>Two · her hand and the desk's hand.</b> Her note laid beside a MONEY-tagged old-crew slip,
    /// and <b>only when this walk-in really is a setup</b>. That last clause is the whole honesty of it: the
    /// table may not say two hands are one hand about a woman who was telling the truth, so a straight
    /// walk-in falls through and the SPREAD answers the way it answers everything else.</para>
    ///
    /// <para>Both cases are tried with the pair BOTH WAYS ROUND, because which paper the player laid down
    /// first is not a fact about either of them.</para>
    /// </summary>
    private SpreadReconcile.Result? TheWalkInsRevealFor(SpreadReconcile.Paper a, SpreadReconcile.Paper b) =>
        HerNoteAgainst(a, b, a, b) ?? HerNoteAgainst(b, a, a, b);

    /// <summary>One direction of the pair: is <paramref name="note"/> hers, and does
    /// <paramref name="other"/> answer it? The last two arguments are the pair as the table laid it, because
    /// the money/love count is about the TABLE and not about which way this method happened to look.</summary>
    private SpreadReconcile.Result? HerNoteAgainst(
        SpreadReconcile.Paper note, SpreadReconcile.Paper other,
        SpreadReconcile.Paper a, SpreadReconcile.Paper b)
    {
        if (WhoseNoteIsThis(note.Id) is not { } who)
        {
            return null;
        }

        // TWO · the setup, and only if there IS one. An old shipmate's slip, tagged money: the desk's paper.
        if (WalkIn.IsASetup(_activeThreadId ?? "", who)
            && other.Kind == SpreadReconcile.Kind.Memory
            && other.Tag == HeldMemory.Theory.Money
            && other.Id.StartsWith(HeldMemory.SlipId(""), StringComparison.Ordinal))
        {
            if (HerJobId(who) is { } setupJob)
            {
                RevealTheWalkInSetup(setupJob);
            }

            return SpreadReconcile.Reveals(
                SpreadReconcile.Verdict.Disagree, WalkIn.SameHandLine, a, b, correctedId: note.Id);
        }

        // ONE · the pairing that ends the sentence.
        if (!string.Equals(other.Id, WalkIn.ReconcilesAgainst(who, HerJobId(who) ?? ""), StringComparison.Ordinal))
        {
            return null;
        }

        FileTheSinceSheet(who, WalkIn.Finished(who));
        RequestVaultSave();
        return SpreadReconcile.Reveals(
            SpreadReconcile.Verdict.Agree, WalkIn.Finished(who), a, b, corroborated: [note.Id, other.Id]);
    }

    /// <summary>Whose note this sheet is, or null for every other paper in both books.</summary>
    private static WalkIn.Who? WhoseNoteIsThis(string? id)
    {
        foreach (WalkIn.Who who in new[] { WalkIn.Who.Ilse, WalkIn.Who.Nadia })
        {
            if (string.Equals(id, WalkIn.NoteId(who), StringComparison.Ordinal))
            {
                return who;
            }
        }

        return null;
    }

    /// <summary>The job she asked for, if the captain took it. Null when he said no — and then her note
    /// reconciles against the fleet-day page alone, which is the fling's case and the only one there is
    /// without a job in it.</summary>
    private string? HerJobId(WalkIn.Who who)
    {
        foreach (Quest q in _quests)
        {
            if (q.Kind == QuestKind.WalkIn && string.Equals(q.Giver, WalkIn.Name(who), StringComparison.Ordinal))
            {
                return q.Id;
            }
        }

        return null;
    }

    /// <summary>#973 L5b · L3's door: the SPREAD found it out. Idempotent, and it never un-reveals — a thing
    /// the captain has worked out about somebody does not become unknown again.</summary>
    private void RevealTheWalkInSetup(string jobId)
    {
        if (_walkInSetupsRevealed.Add(jobId))
        {
            RequestVaultSave();
            StateHasChanged();
        }
    }

    /// <summary>
    /// #973 L5b · <b>HAS THE SENTENCE FINISHED?</b> — Core's own predicate, asked with the two facts this
    /// page owns: whether the job is done, and whether L3's SPREAD has reconciled her note against
    /// <see cref="WalkIn.ReconcilesAgainst"/>. The second argument is false until L3 ships, and the job
    /// finishes the line on its own until then.
    /// </summary>
    private bool TheSinceLineHasFinished(Quest q, bool noteReconciled) =>
        WalkIn.SinceFinishes(q.State is QuestState.Complete or QuestState.TurnedIn, noteReconciled);
}
