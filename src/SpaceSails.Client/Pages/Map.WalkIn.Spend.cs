using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · WHAT L3 SPENDS (#973) — the card's warning, her note read against the spread, whose note is
/// whose, and the setup revealed.
///
/// <para>Split out of <c>Map.WalkIn.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field.</para>
/// </summary>
public sealed partial class Map
{
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
}
