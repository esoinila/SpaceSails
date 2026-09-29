using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #1202 · <b>RAUHA LIND HAS A SEAT OF HER OWN.</b> Owner, 2026-09-29 (evening), on her borrowing the
/// stranger's table one watch in three: <i>"own seat sounds like a known regular."</i> So she is seated the way
/// the regulars are — in one of the bar's numbered chairs (<see cref="PatronSeats"/>), with a
/// <c>BarPatron</c> console of her own plated <c>◈ RAUHA LIND</c> and a figure plated <c>Lind</c> — on her
/// own cadence (<see cref="CarryThePress.AtTheTable"/>, one watch in three per berth, the same predicate the
/// offer has always been gated on).
///
/// <para><b>Her chair</b> is the first of the pool the rota's own UNTOUCHED seating leaves free this watch, in
/// the pool's own order: a function of the station and the watch alone, so the console, the figure and the
/// free-chair list (<see cref="FreePatronSeats"/>, which reserves it) all read one answer, and a regular who
/// comes out of the back is never allotted her chair.</para>
///
/// <para><b>A fixture, like the oracle's corner — not a fifth member of the rota.</b> She is asked for beside
/// <see cref="ResolveRegulars"/> rather than inside it, so the four regulars' seeded evenings (who leaves,
/// who comes in, the barkeep's line, the walk's man) are the evenings they were, to the byte. <b>Away</b> is
/// the churn's <c>Left</c> naming her: the page puts her there while she is aboard on a contract or at her
/// pages in the gallery, so she is never in two rooms at once.</para>
/// </summary>
public static partial class HavenInterior
{
    /// <summary>Her seat this watch, or null when she is not drinking here (not her watch, the room has no
    /// chair the rota left free, or the churn says she is elsewhere).</summary>
    public static SeatedRegular? TheStringersSeat(string bodyId, double simTime, RoomChurn? churn = null)
    {
        long watch = PatronRota.WatchIndex(simTime);
        if (!CarryThePress.AtTheTable(bodyId, watch)
            || (churn is { } room && room.Left.Contains(CarryThePress.Giver))
            || TheStringersChair(bodyId, simTime) is not { } chair)
        {
            return null;
        }

        (float x, float y) = PatronSeats[chair];
        ulong seed = RegularSeed(CarryThePress.Giver, watch);
        double facing = -System.Math.PI / 2 + (ReeverIdle.FacingTwitchAt(seed, 0) * 1.5);
        return new SeatedRegular(CarryThePress.Giver, $"◈ {CarryThePress.Giver}", CarryThePress.Plate,
            true, x, y, facing, seed, PatronState.AtBar);
    }

    /// <summary>Her chair on this watch, whether or not she is in it: the first numbered chair the rota's own
    /// untouched seating leaves free. Null on a watch that is not hers or a room with no free chair.</summary>
    private static int? TheStringersChair(string bodyId, double simTime)
    {
        if (!CarryThePress.AtTheTable(bodyId, PatronRota.WatchIndex(simTime)))
        {
            return null;
        }

        var taken = new System.Collections.Generic.HashSet<int>();
        foreach (PatronSeating s in PatronRota.ResolveSeating(bodyId, simTime, PatronSeats.Length))
        {
            if (s.Present)
            {
                taken.Add(s.SeatIndex);
            }
        }

        for (int i = 0; i < PatronSeats.Length; i++)
        {
            if (!taken.Contains(i))
            {
                return i;
            }
        }

        return null;
    }
}
