using System;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #817 · SITTING DOWN IN SOMEBODY ELSE'S OFFICE.
///
/// <para>Owner, live in a park-view suite the evening of 2026-08-11: <i>"I mean in office people sit down …
/// what on the floor there?"</i> and <i>"Let's make some cubicles / desks / chairs we can sit in."</i> Core
/// carved the desks and published the seats (<see cref="RingOffice"/>); this is the press.</para>
///
/// <h3>The same panel, on purpose — and the owner said so himself</h3>
///
/// <para>His ruling on how to build these at all was that an office table is the restaurant's table with a
/// different outline: <i>"just more rectangular and have more table area / person. The functionality is
/// about the same otherwise."</i> So this file opens the very <c>TableTalk</c> a canteen top opens, on
/// <see cref="SittingAlone"/>'s own move ids, through the same seated frame, the same WAIT beat and the same
/// short-rest ledger. What is genuinely different about an office chair is TWO facts, and they are two flags
/// on the sitting rather than two copies of it: nobody ever comes over (the staff of this building are
/// somewhere else, and a haulier crossing a room to recruit you would be the canteen's scene played in an
/// empty office), and the room the silence is described in is an office rather than a hall.</para>
///
/// <h3>#820 · The sit SNAPS the captain onto the seat</h3>
///
/// <para>Owner's law, issued while this was in flight: sitting down puts the body IN the chair. The
/// coordinate is Core's published seat and is never re-derived here — and standing up puts the captain back
/// on <see cref="RingOffice.Chair.StandAt"/>, which is the same square, because a ring chair is not a solid
/// and the seat is the very spot you were standing on to press [E]. Nothing can trap the dot. (The park
/// bench still lacks the snap; that is #820's own sweep and deliberately not this PR's.)</para>
/// </summary>
public partial class Map
{
    /// <summary>What an office chair's watch-scoped state is keyed on. Its own prefix, so a chair, a bench
    /// and a canteen top with the same ordinal on the same floor can never share a wait counter — three
    /// seats in three rooms, and one key for all of them would be one source consumed as if it were
    /// three.</summary>
    private static string OfficeChairKey(SurfaceExcursion ex, int roomNumber, int chairIndex) =>
        $"{ex.CanteenWatch}:{ex.Floor}:office:{roomNumber}:{chairIndex}";

    /// <summary>
    /// #817 · TAKE A CHAIR — [E] at a desk in a park-view suite, and the captain sits down.
    ///
    /// <para>WHICH chair it is comes off Core's own published list, matched to the console the press landed
    /// on — a lookup rather than a decision, and never a coordinate this file measured for itself
    /// (§13.15).</para>
    /// </summary>
    private bool TryTakeOfficeChair()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return false;
        }

        // Already sitting. The press is CONSUMED — [E] is not how you stand up, "Stand up" is.
        if (_table is not null)
        {
            return true;
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveOfficeChair } spot)
        {
            return false;
        }

        if (UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park
            is not { } green)
        {
            return false;
        }

        foreach (UndergroundComplex.RingRoom room in green.Frontage)
        {
            foreach (RingOffice.Chair chair in room.Seats)
            {
                if (Math.Abs(chair.X - spot.X) >= SameChairDu
                    || Math.Abs(chair.Y - spot.Y) >= SameChairDu)
                {
                    continue;
                }

                SitInThisChair(ex, room, chair);
                return true;
            }
        }

        return false;
    }

    /// <summary>How close a console has to be to a published seat to BE that seat. A rounding tolerance and
    /// not a search radius: the console was hung on the chair's own coordinate by
    /// <c>HiveInterior</c>, and the only thing between them is a float cast.</summary>
    private const double SameChairDu = 0.5;

    /// <summary>The one place an office-chair sitting is opened, so a dev row and a captain's [E] can never
    /// open two different chairs.</summary>
    private void SitInThisChair(
        SurfaceExcursion ex, in UndergroundComplex.RingRoom room, RingOffice.Chair chair)
    {
        // #820 · THE SNAP. The body goes into the chair, at Core's own published seat — through
        // StandCaptainAt, so the pad-crew net has its say and a seat that somehow ended up inside a desk
        // would be reported rather than silently swallowed.
        StandCaptainAt(chair.X, chair.Y, "you pull out the chair and sit down");

        _table = new TableTalk
        {
            Key = OfficeChairKey(ex, room.Number, chair.Index),
            // The APPROACH ordinal, and deliberately not the chair's own — Core owns the offset
            // (RingOffice.ApproachOrdinal) for the reason the bench's does: two seats dealt the same
            // ordinal are two seats dealt the same answer on the same shift.
            Index = RingOffice.ApproachOrdinal(room.Number, chair.Index),
            Office = true,
            OfficeSeatX = chair.StandAt.X,
            OfficeSeatY = chair.StandAt.Y,
            Who = CanteenTable.Who.None,
            Plate = RingOffice.SeatPlate,
            Scene = RingOffice.TheChair(room.Plate),
            Seats = Math.Max(1, room.Seats.Count),
            Free = Math.Max(0, room.Seats.Count - 1),
            // A room with a door and nobody in it. Quiet is the cabinet's own flag and it is the true one
            // here for the same reason: nothing at a counter can see you, and the exposure ladder should
            // say so rather than treating a private office as a table in a bar.
            Quiet = true,
            Solo = true,
            // #783's relaxed register belongs to a bought glass in a canteen. There is no bar in an office
            // and no art of one — borrowing the picture would be showing a room the captain is not in.
            Relaxed = false,
            DrinkInHand = APourInFrontOfYou,
            // Nobody to ask. The chair is simply taken, and the taking is the scene's opening line.
            Joined = true,
            Outcome = RingOffice.TookAChairLine(room.Plate),
        };

        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }
}
