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

    /// <summary>#973 L5b · Which walk-ins the SPREAD has found out, by job id. Written by the reconcile
    /// below; empty until it fires, and until then every setup card says nothing, which is the shipped
    /// behaviour and the correct one — the player may simply go.
    ///
    /// <para><b>AND IT SURVIVES A SAVE.</b> L5b shipped this set with no vault row and said so, and a captain
    /// who worked the setup out, saved and came back found the card quiet again — a thing he had worked out
    /// about somebody, un-known by a reload. It has its own section now
    /// (<see cref="WalkInSection"/>, <c>BuildWalkInSection</c>/<c>RestoreWalkInSection</c> below), written the
    /// way every other book in this arc is written: independently optional, so a file from before this row
    /// existed loads with nothing revealed — which is the truth about a captain who never laid the two papers
    /// side by side.</para></summary>
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

    // ── THE KEEPING ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#973 L5b · The knowings, as the vault stores them — job ids and nothing else, the house
    /// idiom. Null when the SPREAD has found nothing out, so a captain who never laid the two papers side by
    /// side writes no section at all.</summary>
    private WalkInSection? BuildWalkInSection() =>
        _walkInSetupsRevealed.Count == 0
            ? null
            : new WalkInSection { SetupsRevealed = [.. _walkInSetupsRevealed] };

    /// <summary>Read them back. A pre-L5b-finisher file simply has none and wakes with every setup card
    /// quiet, which is exactly what it was — and a blank or repeated id is dropped rather than thrown over,
    /// the same tolerance the filing line and the satchel get.</summary>
    private void RestoreWalkInSection(WalkInSection? section)
    {
        _walkInSetupsRevealed.Clear();
        foreach (string jobId in section?.SetupsRevealed ?? [])
        {
            if (!string.IsNullOrWhiteSpace(jobId))
            {
                _walkInSetupsRevealed.Add(jobId);
            }
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
