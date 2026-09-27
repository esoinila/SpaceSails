using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE JOB AND THE TWO SITES (#973 L5b) — taking her job, where what she wants is, finding it, and
/// coming back to tell her.
///
/// <para>Split out of <c>Map.WalkIn.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field.</para>
/// </summary>
public sealed partial class Map
{
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

            // #761 · …AND HE IS TOLD, on the surface he is looking at. Owner's law: a plot-significant
            // moment reaches the player where their eye is, not only in a book they may never open. This is
            // the fifth of the game's five crossings and it was the only one that reached no surface at all
            // — the Hive's gate raises a card over the lift panel, the scanner's press says it where the
            // captain is looking, the agent's remote writes it onto his own panel, and this one, where a
            // debt is taken on because somebody set him up, went into the autopilot log and nowhere else.
            // The badge that carries this fact elsewhere (`TheyRememberYouHere`, Map.razor) is drawn only
            // while an excursion is underfoot, and a walk-in's second berth is a BAR: there is no ground
            // for it to appear on, so the badge could never have been the answer here.
            //
            // The sentence is IllegalHeat's own, unchanged and un-recomposed — the same words that badge
            // shows on a moon, so two readings of one fact cannot drift apart. It goes through #736's seam,
            // which puts it on whatever pop-up is in front of him and falls through to the HUD when nothing
            // is; and at Telling.Floor, because the completion line one statement above is Status and lands
            // in the same breath. Said at Status this would be #689 on a two-line scene: the significant
            // half losing the one slot to the routine half, by write order.
            SayItWhereTheyAreLooking(IllegalHeat.TheyRememberYouHere, Telling.Floor);
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
}
