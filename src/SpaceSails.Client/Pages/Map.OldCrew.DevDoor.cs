using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE DEV DOOR — <c>/map?oldcrew=1</c>'s seeding and standing the old crew where the captain is.
///
/// <para>Split out of <c>Map.OldCrew.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. The banner and <c>_oldCrewCheat</c> stay in the opening file with
/// every other field of the family.</para>
/// </summary>
public partial class Map
{
    /// <summary>
    /// <c>/map?oldcrew=1</c> — the owner's method, applied to this lane: <i>open every scene and check all
    /// the parts are in the right place</i>. Without it the face scene is behind a death and four voyages of
    /// berth-hopping, which is a scene that ships unlooked-at.
    ///
    /// <para>It grants nothing. It buries one captain (so there IS a new face to be surprised by) and it
    /// posts the four shipmates at the berth the captain is standing in — the same "hand you the person, not
    /// the truth" discipline <c>?kaamos=holder</c> and <c>?oracle=1</c> keep. Every word of the scene, every
    /// crossing and every sheet is still played.</para>
    /// </summary>
    private void SeedOldCrewCheat()
    {
        // ARMED, NOT APPLIED. A docked boot never opens the start picker, so the universe's thread is not
        // bound yet at this stage and the crew cannot be cast — which is exactly the trap the first cut of
        // this cheat fell into: it read an empty seeding, moved nobody, and reported success. So the flag
        // rides until the crew are actually seeded (`SeedTheOldCrew`, lazily, off the thread id), and the
        // move happens there — which also means it survives a universe being switched under it.
        _oldCrewCheat = true;
        StandTheOldCrewWhereTheCaptainIs();
        ShowPulseMessage(
            "🧪 Test: the four who served with you work this berth today, and there is a captain in the "
            + "ground. Open the counter — they are on the drinks list, and one of them is going to look at "
            + "your face.");
    }

    /// <summary>The cheat's two moves, applied wherever the crew end up being cast: bury one captain (so
    /// there IS a new face to be surprised by) and post all four at the berth the captain is standing at.
    /// A no-op when the flag is not armed, which is every ordinary boot.</summary>
    private void StandTheOldCrewWhereTheCaptainIs()
    {
        if (!_oldCrewCheat)
        {
            return;
        }

        if (_activeThreadId is { } thread
            && (ActiveThreadInfo?.Retired.Count ?? 0) == 0
            && Threads.IssueSuccessor(thread, (int)(SimTime / 86400)) is not null)
        {
            RefreshThreadList();
            MarkTheBookAtTheFilingLine();
        }

        if (_dockedHavenId is not { } here || _oldCrew.Count == 0)
        {
            return;
        }

        var moved = new List<OldCrew.Seeded>(_oldCrew.Count);
        foreach (OldCrew.Seeded s in _oldCrew)
        {
            moved.Add(s with { StationId = here });
        }

        _oldCrew = moved;
    }
}
