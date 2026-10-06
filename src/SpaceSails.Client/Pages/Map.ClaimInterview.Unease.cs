using System;
using System.Collections.Generic;
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
    private void TheUneaseComes(ClaimInterview.Settlement settled)
    {
        if (!ClaimInterview.RaisesTheUnease(settled.Outcome, _roomsTurnedOver))
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
            FileNote(ClaimInterview.UneaseBookLine, HullClaim.BookGlyph);
        }

        ApplyNerveShock(ClaimInterview.UneaseNerve, ClaimInterview.UneaseShockLabel);
        RaiseStoryBeat(StoryBeats.Beat.Flashback, ClaimInterview.FlashbackSubject);
        RequestVaultSave();
    }
}
