using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #973 L0 · THE DOCKED STATION BAR IS A ROOM NOW.
///
/// <para>The owner's favourite room in this game is The Red Eye's bar off Jupiter — the menu card, the offer
/// of a drink to One-Eye Silas, the regulars at their tops, the PIRATE INSURANCE poster on the starboard
/// wall — and until this lane it was the one room in the game where <b>nobody could move</b>. Eleven droid
/// slots, every one of them a stateless function of sim time: the regulars are seated where the rota put
/// them, the barkeep paces a sine, the Magpie stands on a schedule. No band, no doors an NPC could come out
/// of, no floor anybody but the captain walked.</para>
///
/// <para>#731 built the whole of that machinery — <i>"the NPCs but not reevers could also use the A* … if they
/// go behind a door that is locked to us, we use that as 'I guess that concludes the conversation'"</i> — and
/// wired it to a Hive canteen floor, because that was the only deck in the game with a walker band. #976 put
/// Harlan Fess on that same floor and its own file said so out loud: <i>"A docked station's bar has posters
/// and a barkeep and no seating and no walkers at all."</i> This file is the second half of that sentence
/// being paid off.</para>
///
/// <h3>What is here, and what is deliberately not</h3>
///
/// <para><b>Not one new primitive.</b> The walking is <see cref="NpcWalk"/>'s, over <c>AutoWalk</c>'s route
/// and <c>SurfaceCollision.Slide</c>'s stone. Which door somebody comes out of is <see cref="Egress"/>'s, off
/// the frozen docking watch, and it is a <see cref="UndergroundComplex.LockedDoor"/> because that is the type
/// that cannot be a public exit. The band's width is <see cref="Egress.BandSlots"/>. The figures are the
/// <see cref="Walker"/> record and the <see cref="Errand"/> enum this page already had, drawn through the same
/// <see cref="FillWalkerDroids"/>. What this file owns is the ROOM — which list of feet belongs to a berth,
/// when that room forgets, and the one hook L5b needs.</para>
///
/// <para><b>Nothing here explains anything.</b> A man comes out of the cellar door, crosses the floor and
/// stands at the counter; the captain's own TRY at that leaf is refused, and no card, pulse or line is raised
/// about either fact. That is §13.8 and it is the whole of #731's beat, arriving at last in the room the owner
/// actually drinks in.</para>
///
/// <h3>The seat this file was written without, and now has</h3>
///
/// <para>This paragraph said <i>"there is no way to sit down in a docked bar"</i> until #979/#981 built one.
/// It was true when #977 wrote it — every seat in this game is opened through <c>Seating.TakeThisSeat</c>, all
/// seven sites of it were gated on a <c>SurfaceExcursion</c>, and a docked berth has none, so the bar's tops
/// were drawn dressing with no chairs and no console. <b>The eighth site closed that gap</b>
/// (<c>Seating.BarTop.cs</c>, opened off the page's own <c>TheBarTopUnderfoot</c> rather than off an
/// excursion; <c>Map.ShipSeats.cs</c> later widened the same one answer to the boat's own cantina and desks),
/// and the count in <c>EverySeatTheCaptainTakesFingerprintsTheSameTests.ThereIsOnePlaceASittingIsOpened</c>
/// moved 7 → 8 to say so.</para>
///
/// <para>So <see cref="ApproachTheTable"/>'s gate does what it was shaped for: <c>TheCaptainIsSittingAloneIn
/// TheBar</c> answered false at every berth in the game on the day it was written and answers TRUE now, and
/// exactly one caller changed. The gate is a <c>Func&lt;bool&gt;</c> and not a private opinion precisely so
/// that this file did not have to.</para>
/// </summary>
/// <remarks>Split by concern at 1,183 lines (#251) into <c>Map.BarWalkers.Watch</c>, <c>…Walk</c>,
/// <c>…Rep</c> and <c>…Top</c>. THIS file is the room and its clock: who is on their feet in it and which
/// berth those feet belong to, the four sets a visit's churn is kept in, the frozen docking watch every
/// other partial reads its arithmetic off, and <c>AdvanceBarWalkers</c> — one frame of the room, in the
/// order the room does it.</remarks>
public partial class Map
{
    /// <summary>#973 L0 · Everybody on their feet in the docked bar. The bar's own list and never the
    /// excursion's: a captain can be ashore at a berth with no excursion at all, and one list serving two
    /// rooms would be this repo's first named bug class with a barman in it.</summary>
    private readonly List<Walker> _barAfoot = [];

    /// <summary>Which berth these feet belong to. A different berth — or none — is a room that has never seen
    /// any of them, exactly as a turned shift is underground.</summary>
    private string? _barFeetBerth;

    /// <summary>#973 L0 · How far off a top's centre a body stands to be AT it. One avatar ACROSS, the same
    /// step the ashore boot uses to stand the captain wholly inside a room rather than straddling its edge —
    /// a body-width and never a coordinate in this room, which is the one kind of number a client file is
    /// allowed to hold. Which SIDE is the stone's answer, sounded below.</summary>
    private const double BesideATopDu = 2 * DeckPlan.AvatarRadius;

    /// <summary>#973 L0 · The docked bar the captain is standing in a berth of, or null.
    ///
    /// <para>Null on an excursion (that room has its own metabolism and its own list), null off the deck, and
    /// null at a berth with no interior to walk. It does NOT ask whether the captain has reached the bar yet:
    /// somebody crossing a floor keeps crossing it whether or not there is anybody in the room, which is the
    /// difference between a simulation and a cutscene.</para></summary>
    private HavenInterior.BarFloor? TheDockedBar() =>
        _surface is null && _deckMode && _dockedHavenId is { } berth
            ? HavenInterior.BarBand(berth)
            : null;

    /// <summary>#973 L0 · Is the captain actually in the bar? North of the room's own south wall, which is the
    /// wall the room is built off — never a threshold typed in here.</summary>
    private bool InTheBar(in HavenInterior.BarFloor bar) => _avatarY > bar.FloorY;

    /// <summary>#973 L0 · The bar's walker band, written into the slots the docked deck reserved for it. The
    /// same filler the Hive floor uses, handed the other room's feet.</summary>
    private void FillBarWalkerDroids(DeckPlan.Droid[] buffer, int firstSlot) =>
        FillWalkerDroids(buffer, firstSlot, _barAfoot);

    /// <summary>#973 L0 · The frozen docking watch, as an index. The rota that seated the room and the roll
    /// that picks a door are the same watch, for #709's reason: a room drawn at one instant and walked at
    /// another is two rooms.</summary>
    private long BarWatch => PatronRota.WatchIndex(_dockVisitSimTime);

    /// <summary>#973 L0 · A docked berth is not a floor of a building, and <see cref="Egress.DoorFor"/> only
    /// wants a number to fold into its seed. Zero, stated once, so the door a man comes out of is the same
    /// door every time this visit.</summary>
    private const int BarIsNotAFloor = 0;

    // ── #731 · THE ROOM'S OWN HOURS ──────────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-08-06: "Like on the bar now they have to wait for us to leave before they can sit up… or
    // leave the bar." And, 2026-09-01: "also just other customers arriving and leaving in the bars already
    // does a lot… they can go behind doors that are locked to us."
    //
    // Until this lane the bar's four regulars were a pure function of the docking watch and NOTHING could
    // move them: whoever the rota seated when the captain clamped on was still in that chair when he cast
    // off. Now the same frozen watch that seats them also decides which of them finishes and goes, and which
    // of the ones it has "in the back" comes OUT of the back — both through the leaves the captain's own TRY
    // is refused at, both on the one walker, and not one line is said about any of it.

    /// <summary>#731 · Which regulars have stood up and walked out this visit, by the rota's own id. The room
    /// stops seating them the moment their legs start: one body, one place.</summary>
    private readonly HashSet<string> _barLeft = new(StringComparer.Ordinal);

    /// <summary>#731 · …and which have come out of the back and sat down, by the chair they took. Written on
    /// the frame they reach it and never on the frame they set off — a man is not in a chair he is still
    /// walking to.</summary>
    private readonly Dictionary<string, int> _barCameIn = new(StringComparer.Ordinal);

    /// <summary>#731 · This shift's own list of who goes, worked out ONCE when the visit begins and only read
    /// afterwards. Null is a question the room has not been asked yet; empty is an answer it gave.</summary>
    private IReadOnlyList<Egress.Move>? _barGoing;

    /// <summary>#731 · …and the same list run the other way: who turns up.</summary>
    private IReadOnlyList<Egress.Move>? _barComing;

    /// <summary>#731 · Which moves have been dealt already, so a schedule re-read every frame never sends the
    /// same person through the same door twice. Keyed on the direction and the id, because a man may both
    /// leave a room and come into it and they are two different evenings.</summary>
    private readonly HashSet<string> _barDealt = new(StringComparer.Ordinal);

    /// <summary>#731 · The room as this evening has left it — handed to the deck build, the droid fill and the
    /// barkeep's line through one call, so the three of them cannot come to three views of who is here.</summary>
    private HavenInterior.RoomChurn TheBarsChurn => new(_barLeft, _barCameIn);

    /// <summary>#731 · How far into the frozen docking watch the clock has got. The SCHEDULE is a function of
    /// <see cref="BarWatch"/> and does not move while it is being read; this is only the hand crossing the
    /// times that schedule already named — the same shape the canteen's own deal has, and never a wall
    /// clock.</summary>
    private double IntoTheBarsWatch => SimTime - (BarWatch * PatronRota.WatchSeconds);
    /// <summary>#731 · How many of the ROOM'S OWN people are on their feet. The salesman and the walk-in are
    /// not: they do not live here and they are not who <see cref="Egress.MostAtOnce"/> is a law about. Counted
    /// off the walker list rather than tracked, because that list is the truth about who is afoot.</summary>
    private int TheBarsOwnFeet()
    {
        int feet = 0;
        foreach (Walker w in _barAfoot)
        {
            if (w.For is Errand.Leaving or Errand.Arriving)
            {
                feet++;
            }
        }

        return feet;
    }

    // ── ONE FRAME OF THE BAR'S METABOLISM ────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L0 · Step everybody who is on their feet in the docked bar, then let the salesman decide what to
    /// do next. Called from the walked frame beside the ship's own pumps, and it does nothing at all anywhere
    /// but a berth with a bar behind it.
    ///
    /// <para>The order is the Hive's, and for the Hive's reason: the decision about what somebody should do
    /// next is a decision about a floor whose bodies have already been stepped this frame.</para>
    /// </summary>
    private void AdvanceBarWalkers(double dtRealSeconds)
    {
        if (TheDockedBar() is not { } bar)
        {
            ForgetTheBarsFeet(null);

            // …and his VISIT is only this file's to forget when there is no excursion either. On a moon
            // <c>AdvanceTheRep</c> owns that fold, and two owners of one field is a visit counter that ticks
            // once a frame: he would arrive as a stranger sixty times a second and the rota would be noise.
            if (_surface is null)
            {
                EnsureRepVisit(null);
                EnsureWalkInVisit(null);
            }

            return;
        }

        ForgetTheBarsFeet(bar.BodyId);
        DealTheBarsHours(bar);
        StepTheBarsFeet(dtRealSeconds, bar);
        AdvanceTheRepAshore(bar);
        AdvanceTheWalkIn(bar);   // #973 L5b · …and whoever the evening has crossing the floor to your table
        AdvanceTheFinder(bar);   // #417 · …and the finder, when there is a case or an account to settle
    }

    /// <summary>#973 L0 · CASTING OFF IS THE ROOM FORGETTING. Same law a turned shift is underground: what
    /// happened at the last berth happened to people who are not here, and a body left on a list across a
    /// re-dock would be drawn walking through a station it was never in.</summary>
    private void ForgetTheBarsFeet(string? berth)
    {
        if (_barFeetBerth == berth)
        {
            return;
        }

        _barFeetBerth = berth;
        _barAfoot.Clear();

        // …and the evening with them. A different berth is a different room, and a chair emptied at the last
        // one is a chair belonging to a station this captain is no longer tied to. Null and not empty for the
        // schedules: empty is an answer this room gave, null is a question it has not been asked yet.
        _barLeft.Clear();
        _barCameIn.Clear();
        _barDealt.Clear();
        _barGoing = null;
        _barComing = null;
    }
}
