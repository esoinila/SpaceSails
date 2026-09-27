using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · SHE REACHES THE TABLE (#973 L5b) — her arrival, her card, the captain's answer, her leaving, and
/// the note she leaves behind.
///
/// <para>Split out of <c>Map.WalkIn.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every fact about her evening stays in the opening
/// file.</para>
/// </summary>
public sealed partial class Map
{
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
    /// <remarks>#1052 · …AND SHE WAITS IF SOMETHING IS ALREADY WEARING THE SCRIM. Her card is a
    /// <c>.view-object-backdrop</c> like the salesman's, and the one-scrim law forbids a second dim over a
    /// first. She is held rather than turned away — she is standing at the table, and the beat she hosts is
    /// only counted once her card really is on the screen (#777) — so the arbiter holds the WHOLE of the
    /// arrival, beat included, and lets it through on the first clear frame. <c>TheWalkInIsStillWanted</c>
    /// is asked again at that far end: a captain who stood up and walked off while reading something else
    /// has ended the scene she was walking into, exactly as the room's own stepper already has it.</remarks>
    private void SheReachesYourTable() =>
        RaiseAScrimCard(HerArrivalLands, TheWalkInIsStillWanted);

    /// <summary>The arrival itself, once the glass is hers.</summary>
    private void HerArrivalLands()
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
}
