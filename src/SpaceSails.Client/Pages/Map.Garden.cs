using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1332 B · <b>THE GARDEN BEHIND GLASS, ON THE PAGE</b> — the three things the room asks of the page: which
/// bench a press landed on, the one line told the first time the captain walks in at each haven, and the dev
/// start that stands him at its door.
///
/// <para>Owner, 2026-09-29: <i>"the little garden on space ports could be used to produce salad, coffee etc.
/// comforts for levels sufficient for the restaurant."</i> The room is <c>HavenInterior.Garden.cs</c>'s and its
/// words are <see cref="HavenGarden"/>'s; nothing here measures anything.</para>
///
/// <para><b>No new page field.</b> The told-once memory is slice A's set, renamed to say what it holds
/// (<c>_toldOnce</c>, keyed by line and berth — see <c>Map.HavenLift.cs</c>). The bench's sitting is
/// the park bench's (<c>Seating.TryTakeBench</c>), reached through the one answer a seat asks the page for
/// (<c>TheBarTopUnderfoot</c>) — <see cref="Seating"/>'s host interface did not grow.</para>
/// </summary>
public partial class Map
{
    /// <summary>#1332 B · The garden's line's key in <c>_toldOnce</c>.</summary>
    private const string GardenVisitKey = ToldOnce.GardenVisit;

    /// <summary>
    /// #1332 B · <b>THE BENCH BY THE GARDEN'S GLASS, AS THE SEAT NEEDS IT.</b> Matched against the room's own
    /// published bench (<see cref="HavenInterior.TheGardenBenchAt"/>) and never measured here; the end is the
    /// park bench's own snap (<see cref="ParkBenches.Bench.EndYouTake"/>, #820 — the end you walked up to); the
    /// step-off is the room's. The setting is the room's own plate, the way the gallery's tables hand in the
    /// walk's — no new prose.
    /// </summary>
    private BarTopUnderfoot? TheGardensBenchUnderfoot(DeckPlan.ConsoleSpot spot)
    {
        if (_surface is not null || !OnTheConcourse || _dockedHavenId is not { } berth
            || HavenInterior.TheGardenBenchAt(berth) is not { } bench
            || Math.Abs(bench.X - spot.X) >= ParkBenches.SameBenchDu
            || Math.Abs(bench.Y - spot.Y) >= ParkBenches.SameBenchDu)
        {
            return null;
        }

        (double seatX, double seatY) = bench.EndYouTake(_avatarX, _avatarY);
        DeckReachability.Point off = HavenInterior.TheGardenBenchStepOff(seatX);
        return new BarTopUnderfoot(
            bench.Index, $"garden:{berth}:{BarWatch}:bench:{bench.Index}", BarWatch, seatX, seatY,
            ParkBenches.Ends, HavenGarden.Plate, ParkBenches.OwnBenchPlate,
            // Ashore, in a room anybody in the station could walk into, with nothing to dog.
            Quiet: false, Aboard: false, Bench: true, StepOff: (off.X, off.Y));
    }

    /// <summary>
    /// #1332 B · <b>THE FIRST TIME IN THE GARDEN, TOLD ONCE AT EACH HAVEN.</b> Fable canon, verbatim
    /// (<see cref="HavenGarden.FirstVisitLine"/>): <i>"Warm, wet, and quiet. Somebody comes here on
    /// purpose."</i>
    ///
    /// <para>Asked on every concourse frame and answered at most once per station: only while the captain is
    /// standing in the room, only on a FREE slot — never over a line still being read, the courtesy every
    /// told-once line keeps — at Status rank, and filed nowhere. The first ride down's twin, in the same
    /// memory.</para>
    /// </summary>
    private void TellTheGardenOnce(string berth)
    {
        if (!HavenInterior.InTheGarden(berth, _avatarX, _avatarY, _havenFloor)
            || _pulse.Message is not null
            || _toldOnce.Has(ToldOnce.Key(GardenVisitKey, berth)))
        {
            return;
        }

        _toldOnce.Tell(ToldOnce.Key(GardenVisitKey, berth));
        ShowPulseMessage(HavenGarden.FirstVisitLine, PulseRank.Status);
    }

    /// <summary>
    /// #1332 B QA · <c>?garden=1</c> — <b>STAND AT THE GARDEN'S DOOR.</b> One pace out from THE door on the hall
    /// side (<see cref="HavenInterior.TheGardenThresholdAt"/>), through the berth's own placement and its
    /// nudge. It stops outside on purpose: walking in, and the line that earns, are the tester's.
    /// </summary>
    private void StandAtTheGardensDoorIfAsked()
    {
        if (_dockedHavenId is not { } berth || HavenInterior.TheGardenThresholdAt(berth) is not { } door)
        {
            ShowPulseMessage("🌿 Test: ?garden=1 needs a berth with a bar — and so a garden. Try &dock=the-space-bar.");
            return;
        }

        StandCaptainAshoreAt(door.X, door.Y);
        ShowPulseMessage(
            $"🌿 Test: you are at the garden's door in {_havenName}. Walk in (west-north-west): the plate, four "
            + "beds, the bench by the far glass — SIT A WHILE / Stand up. The first step in says one line, once.");
    }
}
