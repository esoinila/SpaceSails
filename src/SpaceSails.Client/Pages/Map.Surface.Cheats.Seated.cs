namespace SpaceSails.Client.Pages;

// #1363 finding 2 · The seat-cheats boot a captain who is ALREADY in the room under test, and a first-run
// story card over that room is not the room. Part of Map.Surface (#870 split).
public partial class Map
{
    /// <summary>
    /// #1363 · TRUE WHEN THIS BOOT IS ONE OF THE SEAT-CHEATS — the ones whose whole point is a captain already
    /// sat or standing in a hive room: <c>?tablescene=</c> (and <c>?spread=</c>, <c>?rip=</c>, <c>?threads=</c>,
    /// which imply it), <c>?counter=</c> (and <c>?stool=</c>) and <c>?park=</c>. Read off the flags the query
    /// reader already set, never off a second parse of the URL.
    ///
    /// <para>The <i>standing</i> cheats (<c>?frontdoor=</c>, <c>?freight=</c>, <c>?goodscar=</c>, <c>?ringoffice=</c>,
    /// the garden walk) and the landing cheats (<c>?land=</c>, <c>?secretlab=</c>, <c>?station=</c>) are NOT in
    /// this set: they boot the captain at a door or a gate, and the first ground is part of what they show.
    /// The ashore berth cheats (<c>?barcase=</c> and the rest of <c>?ashore=</c>) never land and never ride
    /// the hive lift, so they raise neither card and need no mark.</para>
    /// </summary>
    private bool ASeatCheatIsBooting => _tableSceneCheat || _counterCheat || _parkCheat;

    /// <summary>The first-ground lesson, spent for this boot: the SAME latch the real landing writes
    /// (<see cref="_groundLessonSeen"/>, read at the end of the descent), set before the descent reads it. A
    /// real landing never reaches this, because the cheat path is the only caller.</summary>
    private void SpendTheFirstGroundLessonForTheSeatCheat()
    {
        if (ASeatCheatIsBooting)
        {
            _groundLessonSeen = true;
        }
    }

    /// <summary>The 🛗 THE SHAFT card, spent for this boot: its own latch is <c>HiveFloorsSeen.Count == 0</c>
    /// on the excursion (the first descent of it), so the surface — floor 0 — is recorded as seen, exactly as
    /// the descent would have recorded the floor a real ride left. Floor 0 is never a floor anyone arrives
    /// at by lift, so no arrival's first-sight beat is touched.</summary>
    private void SpendTheShaftCardForTheSeatCheat(SurfaceExcursion ex)
    {
        if (ASeatCheatIsBooting)
        {
            ex.HiveFloorsSeen.Add(0);
        }
    }
}
