using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1061 · THE ROUND HE IS WORKING — the marks this watch dealt him, asked ONCE per shift and only read
/// afterwards.
///
/// <para><c>Egress.Marks</c> is frozen to the watch, and re-asking it sixty times a second for an answer
/// that cannot change is #731's own lesson with a salesman walking through it. A shift turning over wipes
/// the round, which is the room forgetting: the people he was working went home three hours ago, and the
/// ones sitting there now have never been sold anything.</para>
///
/// <para>Split out of <c>Map.Rep.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── #1061 · THE ROUND ──────────────────────────────────────────────────────────────────────────────

    /// <summary>#1061 · Make sure the round on the page is the one THIS watch dealt. Asked once per shift and
    /// only read afterwards: <see cref="Egress.Marks"/> is frozen to the watch, and re-asking it sixty times a
    /// second for an answer that cannot change is #731's own lesson with a salesman walking through it.
    ///
    /// <para>A shift turning over wipes it, which is the room forgetting — the people he was working went
    /// home three hours ago, and the ones sitting there now have never been sold anything.</para></summary>
    private void TheRoundHeIsWorking(
        string bodyId, int level, long watch, Func<IReadOnlyList<Egress.Occupant>> seated)
    {
        if (_repRound is not null && _repRoundWatch == watch)
        {
            return;
        }

        if (_repRound is not null)
        {
            ForgetTheRound();
        }

        _repRoundWatch = watch;
        _repRound = Egress.Marks(bodyId, level, watch, NebulaRep.ContactId, seated());
    }

    /// <summary>#1061 · A round belongs to one visit and one watch; forgetting it is forgetting both.</summary>
    private void ForgetTheRound()
    {
        _repRound = null;
        _repRoundWatch = long.MinValue;
        _repMarksWorked = 0;
        _repStoodAtTheCounter = false;
        _repStandingAt = null;
        _repShiftOver = false;
    }

    /// <summary>#1061 · The mark he is on, or null when the room is worked.</summary>
    private Egress.Patter? TheMarkHeIsOn =>
        _repRound is { } round && _repMarksWorked < round.Count ? round[_repMarksWorked] : null;

    /// <summary>
    /// #1061 · <b>HAS THE CAPTAIN WATCHED HIM WORK?</b> The whole of the beat, as one predicate: he does not
    /// come to your table until you have seen him at <see cref="Egress.MarksBeforeTheTable"/> other people's.
    ///
    /// <para>It is a FLOOR and not a quota, capped by the round the room could actually deal: a hall with one
    /// sitter in it is worked out after one table, and a salesman who stood about waiting for a second one
    /// that does not exist would be a captain sitting alone for a whole shift with nothing crossing the floor
    /// at all.</para>
    /// </summary>
    private bool TheCaptainHasWatchedHimWork =>
        _repMarksWorked >= Math.Min(Egress.MarksBeforeTheTable, _repRound?.Count ?? 0);

    /// <summary>#1061 · Is he already standing where the next stop is? A walk of no length is a teleport with
    /// a plate on it, and a stop he is already at is a stop he has worked.</summary>
    private bool HeIsAlreadyStandingAt(DeckReachability.Point to) =>
        _repStandingAt is { } here
        && ((here.X - to.X) * (here.X - to.X)) + ((here.Y - to.Y) * (here.Y - to.Y))
           < DeckPlan.AvatarRadius * DeckPlan.AvatarRadius;

    /// <summary>#1061 · WHERE THIS LEG BEGINS — his own feet if he is already working the room, and otherwise
    /// the doorstep of the leaf this watch deals him. One answer for both rooms.</summary>
    private DeckReachability.Point? WhereHeSetsOffFrom(
        IReadOnlyList<UndergroundComplex.LockedDoor> leaves,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        string bodyId, int level, long watch, DeckReachability.Point to)
    {
        if (_repStandingAt is { } here)
        {
            return here;
        }

        int index = Egress.DoorFor(bodyId, level, watch, NebulaRep.ContactId, leaves);
        return index < 0 || index >= leaves.Count
            ? null
            : Egress.StandingPlaceAt(leaves[index], DeckPlan.AvatarRadius, walls, to.X, to.Y);
    }

    /// <summary>#1061 · How long he stands at THIS stop. The round's own beat at a mark, and the plain dwell
    /// at the counter, where there is nobody to talk to.</summary>
    private double HisBeatAt(int table)
    {
        if (table >= 0 && _repRound is { } round)
        {
            foreach (Egress.Patter mark in round)
            {
                if (mark.Index == table)
                {
                    return mark.BeatSeconds;
                }
            }
        }

        return RepDwellSeconds;
    }

    /// <summary>The captain, alone, at a top he can be reached at. The seat's own two answers asked rather
    /// than re-derived — a page working out for itself what the chair already knows is how two instruments
    /// come to disagree.</summary>
    private bool TheCaptainIsSittingAlone(out int tableIndex)
    {
        tableIndex = -1;
        if (!CaptainIsSeated || !SeatedAlone || SeatedTable is not { Bench: false, Office: false } t)
        {
            return false;
        }

        tableIndex = t.Index;
        return true;
    }
}
