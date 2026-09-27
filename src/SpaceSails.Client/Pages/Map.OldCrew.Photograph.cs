using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE PHOTOGRAPH AND THE SLIP (#973 L5a) — the held-memory sheet a shipmate hands over, and the
/// sheet slipped across the table when the glass goes well.
///
/// <para>Split out of <c>Map.OldCrew.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public partial class Map
{
    // ── THE PHOTOGRAPH ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// He hands it over. Only the one the thread put it with, only after the face scene has played, and only
    /// once — a second witness holding the same picture twice is one picture.
    ///
    /// <para>The beat is raised with the photograph's own subject rather than a ledger id, because this is
    /// not a page of the captain's book: it is somebody else's memory of him, which is exactly what makes it
    /// worth more than anything in the ledger.</para>
    /// </summary>
    private void HandOverThePhotograph(string shipmateId, string display)
    {
        if (!string.Equals(shipmateId, OldCrewScene.PhotographHeldBy(TheOldCrew), StringComparison.Ordinal)
            || HeldMemory.Find(_heldMemories, HeldMemory.PhotographId) is not null)
        {
            return;
        }

        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            HeldMemory.PhotographId,
            HeldMemory.Mark.His,
            HeldMemory.Theory.Love,
            OldCrewScene.Photograph,
            OldCrewScene.PhotographFaces(_activeThreadId ?? "", TheOldCrew),
            SimTime,
            // #973 L3 · …and the book says WHO. A memory somebody is holding for you is the strongest kind
            // of evidence in the game, and the sheet is only that if it names the second witness.
            HandedBy: display));

        LogAutopilotEvent($"🎞 {display} hands you a photograph.");
        RaiseStoryBeat(StoryBeats.Beat.Flashback, OldCrewScene.PhotographSubject);
    }

    /// <summary>
    /// A GOOD GLASS SHAKES SOMETHING LOOSE. With an old shipmate the shared-drink roll's good arms do one
    /// thing more than they do with a fixer: a sheet goes into the book, marked theirs and tagged by what
    /// they were to the captain — the fling and the best friend are LOVE, everybody else is MONEY.
    ///
    /// <para>One sheet per person per universe: a second good glass with the same friend is a second good
    /// evening, not a second page. Nobody who is not one of them slips anything, so every other drink in the
    /// game rolls exactly what it always rolled.</para>
    /// </summary>
    private void SlipASheet(string giver, string display)
    {
        if (!OldCrew.IsAnOldShipmate(giver))
        {
            return;
        }

        string id = giver[OldCrew.LedgerPrefix.Length..];
        string sheetId = HeldMemory.SlipId(id);
        if (OldCrew.ById(id) is not { } who || HeldMemory.Find(_heldMemories, sheetId) is not null)
        {
            return;
        }

        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            sheetId,
            who.Id == OldCrew.FlingId ? HeldMemory.Mark.Hers : HeldMemory.Mark.His,
            OldCrewScene.SlipTag(id),
            OldCrewScene.Slip(id),
            [who.Name],
            SimTime,
            HandedBy: display));

        LogAutopilotEvent($"🎞 {display} lets a sheet go.");
    }
}
