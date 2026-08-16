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
/// and the seat is the very spot you were standing on to press [E]. Nothing can trap the dot.</para>
///
/// <para>The sweep that followed took the bench, the counter stool and the canteen chair the same way, and
/// this chair moved onto the shared placement (<c>Map.Surface.SitCaptainOn</c>) with them — see that
/// method for why one law for four seats has to be the law that works on a SOLID seat.</para>
/// </summary>
public partial class Map
{

    // -- #870 lane 6c · THE FORWARDER, AND THERE IS ONE ------------------------------------------------
    //
    // Every seat this file opens lives on Seating now (Seating.OfficeChair.cs). Measured, not assumed: the
    // only spelling anything outside the seat family still asks for is the press itself, from the [E] arm in
    // Map.Deck.Interact.cs. The three sittings under it (a ring chair, a chamber stool, a cubicle's pan) were
    // never called from anywhere but the press, so they kept no forwarder at all.

    /// <inheritdoc cref="Seating.TryTakeOfficeChair"/>
    private bool TryTakeOfficeChair() => _seating.TryTakeOfficeChair();
}
