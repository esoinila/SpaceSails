using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #746 / #820 · WHAT A TOP IS AND WHERE ITS CHAIRS ARE — <see cref="TableSeat"/>, one round top with its
/// place, its seat count and whoever is at it, and the ring geometry that says where each of those chairs
/// stands and which of them a party of this size has filled. One arithmetic for the whole game: the
/// renderer, the walkers and the guards all ask here rather than each drawing a ring of their own. Split
/// out of <c>CanteenRegulars.cs</c> under #251 with no member renamed, re-scoped or re-ordered.
/// </summary>
public static partial class CanteenRegulars
{
    /// <summary>One round top in an amenity: where it is, how many it seats, and who — if anybody — is in
    /// one of those seats this watch.</summary>
    /// <param name="Index">Its ordinal in the room's own table list, so a caller can key state off it.
    /// Cabinet tops carry on from the hall floor's, so one ordinal names one top in one room.</param>
    /// <param name="X">Centre, in the surface's own coordinates.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="Seats">2, 4 or 6 — furniture, decided by the building and never by the shift.</param>
    /// <param name="Plate">Who is at it, or null for an empty table.</param>
    /// <param name="Line">What they say, or null.</param>
    /// <param name="Stranger">#751 · Whether the person at it is a BACKGROUND PATRON — one of the crowd
    /// that fills a hall — rather than one of the ten named regulars. A stranger's table is a thin scene
    /// (small talk, the round, your leave) and the depth stays with the named cast.</param>
    /// <param name="Cabinet">#751 · Which cabinet this top is in, or 0 for the hall floor. It is the fact
    /// the QUIET RULE reads: the counter has eyes everywhere except in here.</param>
    /// <param name="Talking">#792 · Are the people at this top IN A CONVERSATION WITH EACH OTHER? See
    /// <see cref="StrangerTalks"/> for why this is authored rather than counted.</param>
    /// <param name="Heads">#823 · HOW MANY PEOPLE ARE AT IT — 0 for an empty top, 1 for any of the ten named
    /// regulars, and the crowd's own authored headcount (<see cref="StrangerHeads"/>) for a background
    /// patron. Owner, playtest 2026-08-11: <i>"The number of people sitting at a table should match the
    /// amount of seats that are taken."</i></param>
    public readonly record struct TableSeat(
        int Index, double X, double Y, int Seats, string? Plate, string? Line,
        bool Stranger = false, int Cabinet = 0, bool Talking = false, int Heads = 0)
    {
        /// <summary>Somebody is at this table.</summary>
        public bool Taken => Plate is not null;

        /// <summary>
        /// Chairs nobody is in — the seat count less the party's <see cref="Heads"/>, and the number "ask to
        /// join" reads.
        ///
        /// <para>#823 · It was <c>Seats - (Taken ? 1 : 0)</c> until the owner counted the chairs: <i>"it says
        /// there are two haulers eating at a table that seats four, yet there is visual indication of only one
        /// seat out of four being taken? Does the other try sit in the others lap?"</i> A bool cannot seat a
        /// crew, and the plates have held crews since #792. Clamped at zero because a table may be FULL, and
        /// a full table refusing to offer a chair is the honest answer rather than a negative one.</para>
        /// </summary>
        public int Free => Math.Max(0, Seats - Heads);

        /// <summary>#751 · Is this a table the counter cannot see? The one source for the quiet rule.</summary>
        public bool Quiet => Cabinet > 0;

        /// <summary>
        /// #792 · SOMEBODY SITTING ON THEIR OWN — the other half of <see cref="Talking"/>, named so that a
        /// caller asking the approachable question does not have to spell it as a negation.
        ///
        /// <para>Owner, playtest 2026-08-08: <i>"it determines whether there is anything to overhear as
        /// discussion goes."</i> A lone sitter is #757's ask-to-join; a conversation already going is a
        /// different affordance entirely, and a captain looking for one or the other must be able to tell
        /// them apart across a room.</para>
        /// </summary>
        public bool Alone => Taken && !Talking;

        /// <summary>Where one of this top's chairs stands. See <see cref="CanteenRegulars.ChairAt"/>, which
        /// owns the ring; this only hands it the top's own numbers.</summary>
        public (double X, double Y) Chair(int chair) => ChairAt(X, Y, Seats, chair);

        /// <summary>Is the party in this chair? See <see cref="CanteenRegulars.PartyInChair"/> — the same
        /// walk the deck draws bodies on, asked with this top's own headcount.</summary>
        public bool PartyIn(int chair) =>
            PartyInChair(chair, Seats, Taken ? Math.Clamp(Heads, 1, Math.Max(1, Seats)) : 0);

        /// <summary>
        /// #820 · THE CHAIR THE CAPTAIN TAKES, given where they were standing when they pressed [E] — null
        /// at a top with nothing left to sit on.
        ///
        /// <para>The NEAREST FREE one, for a park bench's reason one room along (see
        /// <c>ParkBenches.EndYouTake</c>): a sit that walked the captain round a six-top to the far side
        /// would be moving them further from the chair they had already chosen with their feet. Which chairs
        /// are free is not this method's opinion — it is <see cref="PartyIn"/>, which is the same arithmetic
        /// the deck draws the party's bodies on, so the chair the press hands over is a chair the player can
        /// see is empty.</para>
        /// </summary>
        public (double X, double Y)? ChairYouTake(double fromX, double fromY)
        {
            if (Seats <= 0)
            {
                return null;
            }

            (double X, double Y)? best = null;
            double bestD = double.MaxValue;
            for (int c = 0; c < Seats; c++)
            {
                if (PartyIn(c))
                {
                    continue;
                }
                (double cx, double cy) = Chair(c);
                double d = ((cx - fromX) * (cx - fromX)) + ((cy - fromY) * (cy - fromY));
                if (d < bestD)
                {
                    (bestD, best) = (d, (cx, cy));
                }
            }
            return best;
        }
    }

    // ── #820 · WHERE THE CHAIRS ROUND A TOP ACTUALLY ARE ──────────────────────────────────────────────
    //
    // Owner, evening playtest 2026-08-11: "I would move the avatar on top of the bench when I sit... just
    // snap it into the correct position." A seat you can be snapped onto is a seat with a COORDINATE, and a
    // canteen top's chairs had none: the deck drew them out of a radius and an angle it kept to itself, and
    // the [E] press knew only where the TABLE was. Two authors for one piece of furniture is this repo's
    // most expensive bug class, and the half that could not be checked was the half a body sits on.
    //
    // So the ring lives here, both callers ask it, and the chair a captain lands in is by construction the
    // chair the room drew empty.

    /// <summary>How far out from a top's centre its chairs stand. The deck has drawn them at this radius
    /// since #792 (it was <c>DeckView.SeatRingDu</c>, and that constant now reads this one) — far enough
    /// out to be clear of the top's own plate and close enough to read as belonging to it.</summary>
    public const double ChairRingDu = 1.55;

    /// <summary>
    /// Where chair <paramref name="chair"/> of a <paramref name="seats"/>-seat top centred on
    /// (<paramref name="topX"/>, <paramref name="topY"/>) stands, in the surface's own coordinates.
    ///
    /// <para>Evenly round the ring from due east, anticlockwise, which is the order the deck has drawn them
    /// in since #792 — the ordinal is the drawing's and the sim's at once, so "the party is in chairs 0 and
    /// 3" is one sentence about one room.</para>
    /// </summary>
    public static (double X, double Y) ChairAt(double topX, double topY, int seats, int chair)
    {
        if (seats <= 0)
        {
            return (topX, topY);
        }
        double ang = chair * 2 * Math.PI / seats;
        return (topX + (Math.Cos(ang) * ChairRingDu), topY + (Math.Sin(ang) * ChairRingDu));
    }

    /// <summary>
    /// #823 · Is the party sitting in this chair? A party sits SPREAD ROUND a top rather than stacked in
    /// chair one — three on the same contract at a six-top leave a gap between each of them — and the even
    /// walk below lands on exactly <paramref name="heads"/> chairs for any seat count the building has.
    ///
    /// <para>It was the deck's own loop until #820 needed the same answer for the [E] press: a captain
    /// snapped into a chair the room had already drawn somebody in would be the drawn room and the pressed
    /// room disagreeing about a lap.</para>
    /// </summary>
    public static bool PartyInChair(int chair, int seats, int heads) =>
        seats > 0 && heads > 0 && (chair * heads) % seats < heads;
}
