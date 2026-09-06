using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #775 · WHAT A ROOM DOWN HERE COMES WITH — the amenity gradient as the block reads it: the en-suite a
/// good room has (<see cref="EnSuite"/> and its two dimensions), which floor of a body is the pressurised
/// top and which one carries the staff canteen, and the plate and fixture an amenity hangs over itself.
/// Split out of <c>UndergroundComplex.Block.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered; the sign REGISTERS it reads (<c>PrincipalPlates</c>, <c>ParkViewPlates</c>) stay in the
/// opening file, which is #1163's static-class law and is written up there.
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>#707 · A private washroom cell hung off the back of a room that mattered.
    ///
    /// <para>Owner: <i>"the high level important rooms would have their built in bathrooms"</i> — and the
    /// design under it is that RANK IS READABLE IN PLUMBING. A captain who has learned the grammar reads
    /// "somebody with a name worked in here" off a door to a private cell, the same way sealed SECTOR doors
    /// read as scale. No card ever says it, and the cell itself carries no plate — a private washroom does
    /// not need a sign, and that absence is the last word of the tell.</para></summary>
    /// <param name="X">Centre of the cell.</param>
    /// <param name="Y">Centre of the cell.</param>
    /// <param name="Of">The plate of the room it hangs off — the reason it is there.</param>
    /// <param name="Open">Whether its parent room's own door opens. False behind a locked plate, where the
    /// cell is a thing you can only read from the corridor, exactly like the room it belongs to.</param>
    public readonly record struct EnSuite(
        double X, double Y, string Of, bool Open,
        // #822 · The gap cut in the parent's back wall — the cell's one door, and the whole of its egress.
        // Appended for the reason every optional on these records is appended.
        SurfaceLayout.Doorway? Leaf = null)
    {
        /// <summary>#822 · Every way out of it, never null. A cell is <see cref="EnSuiteDepth"/> by twice
        /// <see cref="EnSuiteHalfHeight"/> — eight du on its longest side, which is
        /// <see cref="FireCodeSmallRoomDu"/> exactly. It is the room the exemption was measured on.</summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Ways => Leaf is { } leaf ? [leaf] : [];
    }

    /// <summary>How deep the en-suite cell hangs off the back of its room, in deck units.</summary>
    public const double EnSuiteDepth = 5.0;

    /// <summary>Half the cell's height. Comfortably taller than <see cref="DoorHalf"/>, so the doorway cut
    /// in the parent's back wall always lands inside the cell rather than beside it.</summary>
    public const double EnSuiteHalfHeight = 4.0;

    /// <summary>#707 · THE TOPMOST FLOOR THAT HOLDS PRESSURE — where the bar is.
    ///
    /// <para>Derived rather than typed. It is B1 on every building in the game and writing <c>-1</c> here
    /// would be a second answer to a question <see cref="HoldsPressure"/> already owns, sitting quietly
    /// correct until somebody moves a band. Two sources, one of which never hears about a change, is the
    /// table at the top of this repo's spec.</para></summary>
    public static int? TopPressurisedFloor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        foreach (int level in FloorsOf(bodyId))
        {
            // #677 · IsPlumbed, not HoldsPressure. The bar goes on the topmost floor that breathes AND has a
            // wet stack; the halls breathe and have no plant of any kind, and on a shallow site they would
            // otherwise be eligible for a counter and three round tops.
            if (IsPlumbed(bodyId, level))
            {
                return level;
            }
        }
        return null;
    }

    /// <summary>#707 · WHERE THE STAFF CANTEEN IS: the deepest floor the building ADMITS to that still
    /// holds pressure, and null on a site too shallow to have a second one.
    ///
    /// <para><b>Deepest, and listed.</b> Two calls, each worth one line:</para>
    /// <list type="bullet">
    /// <item><b>Deepest</b> because the owner's inversion needs distance. The bar is the floor strangers
    /// walk into off the surface; the mess has to be as far from that as the building goes, so that a face
    /// nobody knows is a fact about the room rather than a matter of taste.</item>
    /// <item><b>Listed</b> (<see cref="DepthOf"/>, not <see cref="TrueDepthOf"/>) because catering is a
    /// thing a directory knows about. The band nobody listed has no department, no livery and no plate —
    /// #592's whole tell is the ABSENCE down there — and a canteen sign under it would be the building
    /// admitting to a floor in the one place it must not.</item>
    /// </list>
    ///
    /// <para>Null on a shallow site, and that is the honest answer rather than a gap: a three-floor annex
    /// has one canteen, because one canteen is the entire catering budget of a three-floor annex.</para></summary>
    public static int? StaffCanteenFloor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int? top = TopPressurisedFloor(bodyId);
        int? deepest = null;
        foreach (int level in FloorsOf(bodyId))
        {
            // #677 · …and never in the halls, which the directory could not list if it wanted to. IsPlumbed
            // already refuses them; the IsUnlisted clause stays because a floor can be listed-and-unplumbed
            // for the OTHER reason (§13.7's whole tell is the absence of a plate, not of a drain).
            if (IsPlumbed(bodyId, level) && !IsUnlisted(bodyId, level))
            {
                deepest = level;
            }
        }
        return deepest == top ? null : deepest;
    }

    /// <summary>What is stencilled beside an amenity's door, and what the fixture in the middle of it is
    /// called. Both from one place, so the sign on the wall and the console under the captain's hand can
    /// never come to describe different rooms.
    ///
    /// <para>Institutional throughout, and explaining nothing — with one deliberate exception of TONE. The
    /// branch office's bar plate is the only WARM sign in the building, because it is the only sign in the
    /// building that is a lie: a rest-house plate on a corridor of DESTRUCTION QUEUE and MORTUARY. NO PASS
    /// REQUIRED is a fact about band 0 that the lift panel has been shipping since #590, said out loud on a
    /// wall for the first time and still not explained.</para>
    ///
    /// <para>The head office answers the same law in its own vocabulary (#411): not a canteen and a
    /// washroom but a DINING ROOM and a CLOAKROOM, and its staff hall is for the ESTABLISHMENT — which is
    /// the word on its own B2 plate. Same rule, same grammar, a rank nobody has to be told about.</para></summary>
    public static (string Plate, string Fixture) AmenitySigns(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        bool hq = IsHeadOffice(bodyId);
        return use switch
        {
            Comfort.UpperCanteen => hq
                ? ("🍸 THE DINING ROOM · GUESTS & DEPUTATIONS", "🍸 THE SIDEBOARD")
                : ("🍸 CANTEEN 1 · CARRIERS & CONTRACTORS · NO PASS REQUIRED", "🍸 THE COUNTER"),
            Comfort.StaffCanteen => hq
                ? ("🍽 THE STAFF DINING HALL · ESTABLISHMENT ONLY", "🍽 THE SERVERY")
                : ("🍽 CANTEEN 2 · STAFF ONLY · PASS TO BE SHOWN", "🍽 THE MACHINES"),
            _ => hq
                ? ("🚻 CLOAKS & WASHROOMS", "🚻 THE BASIN RUN")
                : ("🚻 WASHROOMS · STAFF & VISITORS", "🚻 THE BASIN RUN"),
        };
    }

    /// <summary>What one of these rooms says when the captain stands in it. Evidence, and then it stops —
    /// every one of them is about what somebody was made to pay for and none of them is about what any of
    /// it was for.</summary>
    public static string AmenityLine(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        bool hq = IsHeadOffice(bodyId);
        return (use, hq) switch
        {
            (Comfort.UpperCanteen, false) =>
                "🍸 A long counter, a mirror behind it with the bottles gone, and the stools bolted down in " +
                "a row. Somebody kept this room WARM: the paint is a colour that appears nowhere else in " +
                "the building and the tables have been wiped. Whoever came down that shaft with a delivery " +
                "was fed and watered before they went back up, and nothing on this floor ever asked them " +
                "for a pass to do it.",

            (Comfort.UpperCanteen, true) =>
                "🍸 A dining room, and it is LAID. Cloth on the tables, glasses upended on a tray, covers " +
                "still on the sideboard. Places for eleven, set at the same spacing all the way down, and " +
                "the chair at the head pulled out by a hand's width. Somebody set this for a date, and the " +
                "date is not on anything in the room.",

            (Comfort.StaffCanteen, false) =>
                "🍽 Four machines and not a bottle of anything in the racks: soup, tea, and a wall of the " +
                "same wrapped biscuit. The tables are close together and the chairs face each other, which " +
                "is what a room for people who already know each other looks like.\n\n" +
                "📋 Pinned by the machines, a delivery manifest renewed every quarter without a break — and " +
                "the address on it is a SCHOOL, on another world entirely, costed per head for a roll of " +
                "two hundred and forty. Same account number every quarter. Signed for by a name with no " +
                "initial.",

            (Comfort.StaffCanteen, true) =>
                "🍽 Long tables, a servery with the shutters down, and trays stacked to the ceiling with " +
                "nothing between them. Every tray is clean. The rota on the wall is ruled to the end of a " +
                "year nobody has written in yet.\n\n" +
                "📋 And the standing order over the servery is the OTHER HALF of a manifest you have read " +
                "somewhere else: same account number, same quarterly quantity, addressed to a school a very " +
                "long way from here. This is the copy the office kept.",

            (_, true) =>
                "🚻 Cloakroom and washrooms. Numbered hooks, none of them used. A basin run in stone rather " +
                "than steel, and the taps run clear from the first second — somebody flushed this system " +
                "through, and not decades ago.",

            _ =>
                "🚻 Cubicles, a basin run, and a mirror with a tally scratched into the corner and mostly " +
                "rubbed out again. The taps still turn. The water comes through brown for four seconds and " +
                "then runs clear, which means a pump somewhere under your boots has never once stopped.",
        };
    }
}
