using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1151 slice 3 · <b>THE UNEASE FLASHBACK — ON THE PAGE.</b> The first ADJUSTED settlement of the run raises the
/// Flashback plate (the generic stamp, the clause-in-her-voice caption), costs the nerve its dab and leaves exactly ONE
/// record: the signing sheet's margin line when the sheet is held, the book's 📍 entry when it is not. Everything about
/// WHETHER and WHAT is Core's (<see cref="ClaimInterview.RaisesTheUnease"/> and its consts); this file applies it,
/// AFTER the outcome (the credits, the paper, the closed row) has been applied — the eighth shape's care.
/// </summary>
public partial class Map
{
    /// <summary>Called by <c>SettleTheClaim</c> once the outcome is applied. Silent for PAID, DECLINED and every ADJUSTED
    /// after the first (the latch is a register tag, so a reload cannot spend it twice).</summary>
    private void TheUneaseComes(HullClaim.Loss loss, ClaimInterview.Settlement settled)
    {
        if (!ClaimInterview.RaisesTheUnease(settled.Outcome, _roomsTurnedOver))
        {
            return;
        }

        // The unease belongs to a SETTLED claim: the row closed and the settlement's paper in the sleeve. Asked of the page's own
        // state, so a call made before the outcome is applied is silent rather than a plate over an unsettled desk.
        if (ClaimInterview.IsOpen(_roomsTurnedOver, loss.DoneAt) || !_satchel.Any(i => i.Id == settled.PaperId))
        {
            return;
        }

        _roomsTurnedOver.Add(ClaimInterview.UneaseTag);

        if (HeldMemory.Find(_heldMemories, NebulaRep.SigningMemoryId) is { } sheet)
        {
            _heldMemories = HeldMemory.Put(_heldMemories, sheet with { Text = ClaimInterview.WithTheMargin(sheet.Text) });
        }
        else
        {
            FileNoteAbout(ClaimInterview.UneaseBookLine, HullClaim.BookGlyph, ClaimInterview.UneaseSubjects);
        }

        ApplyNerveShock(ClaimInterview.UneaseNerve, ClaimInterview.UneaseShockLabel);
        RaiseStoryBeat(StoryBeats.Beat.Flashback, ClaimInterview.FlashbackSubject);
        RequestVaultSave();
    }
}
