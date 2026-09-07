namespace SpaceSails.Client.Rendering;

// Subject: THE SEATS A ROOM IS FURNISHED WITH, and every one of them HANDED DOWN (part of DeckPlan).
//
// A top, a chair round it, a stool at a counter, one end of a park bench. Four records and the three
// arrays that carry them, and they are one subject because they share one law — #788's one-reach lesson,
// pointed at furniture: the instrument that DRAWS a seat must not be the instrument that decides where
// it is or who is in it. Every field here comes down from the sim per frame, which is why the pen has
// never counted a chair, measured a ring, or worked out from a plate's wording whether the people at a
// table are talking.
//
// #251 · MOVED HERE BY PURE MOTION out of `DeckPlan.cs` — see the note at the head of that file for why
// it was cut and what "pure motion" is holding across the family.

public sealed partial class DeckPlan
{
    /// <summary>
    /// #792 · ONE ROUND TOP, AND WHAT A HUNGRY TRAVELLER NEEDS TO KNOW ABOUT IT WITHOUT PRESSING ANYTHING.
    ///
    /// <para>Owner, playtest 2026-08-08: <i>"people looking to sit down look at those like hungry wild
    /// beasts look at their prey… Now I have trouble finding a free table."</i></para>
    ///
    /// <para><b>Every field is HANDED DOWN, and none of it is worked out by the pen.</b> #788's own lesson,
    /// one fixture along: the seated captain's posture came down from the sim as a bool because an
    /// instrument deriving what the sim already knows is how two instruments come to disagree. A renderer
    /// that counted chairs, or decided from a plate's wording whether the people at a top were talking,
    /// would be a second opinion about a room it did not furnish.</para>
    /// </summary>
    /// <param name="X">The top's centre, in deck units.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Seats">How many chairs stand round it — 0 for plain dressing (the ship's cantina, a
    /// haven bar), which draws the bare ring it always drew and nothing more.</param>
    /// <param name="Occupied">Somebody is at it. The chairs that are left read as an INVITATION rather than
    /// as furniture, which is the whole of the second glance: a table with three people and one chair is
    /// not the same offer as an empty one.</param>
    /// <param name="Talking">…and the people at it are talking to each other, rather than sitting there on
    /// their own. The third glance, and the affordance a future overhear verb will hang off.</param>
    /// <param name="Heads">#823 · HOW MANY of them there are, so the picture can seat the party the sim
    /// seated. Owner, playtest 2026-08-11: <i>"there are two haulers eating at a table that seats four, yet
    /// there is visual indication of only one seat out of four being taken."</i> The pen drew one body
    /// because one body was all it had been handed — it was not simplifying, it had nothing to count.</param>
    /// <param name="Chairs">#820 · WHERE ITS CHAIRS ACTUALLY STAND, and who is in them — one entry per
    /// seat, in the top's own chair order, handed down exactly as a stool's spot and a bench end are.
    ///
    /// <para>The pen used to work these out from a radius and an angle of its own, which was survivable
    /// while nobody sat in them. #820 seats the CAPTAIN in one — <c>CanteenRegulars.TableSeat.ChairYouTake</c>
    /// — so the drawn chair and the sat chair had to become one piece of furniture rather than two authors,
    /// and the way this deck does that is to be told. Empty for plain dressing (the ship's cantina, a haven
    /// bar), which draws the bare ring it always drew.</para></param>
    public readonly record struct TableTop(
        float X, float Y, int Seats = 0, bool Occupied = false, bool Talking = false, int Heads = 0,
        IReadOnlyList<TableChair>? Chairs = null)
    {
        /// <summary>Its chairs, never null — a pen drawing a room must not have to tell an empty list from
        /// a missing one.</summary>
        public IReadOnlyList<TableChair> Seating => Chairs ?? [];
    }

    /// <summary>
    /// #820 · ONE CHAIR ROUND A CANTEEN TOP. The third seat this deck is handed rather than deriving, after
    /// #792's stool and #793's bench end, and for the same reason all three exist: a seat is a place a body
    /// can be, and the instrument that draws one must not be the instrument that decides where it is.
    /// </summary>
    /// <param name="X">Where the chair stands — Core's own ring.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Taken">One of the party is in it. Core's own walk over the headcount (#823), so the
    /// bodies drawn here are the bodies the [E] press refuses to seat the captain on top of.</param>
    public readonly record struct TableChair(float X, float Y, bool Taken = false);

    /// <summary>
    /// #792 · ONE TALL SEAT AT A COUNTER. Occupancy comes down from the sim exactly as a top's does — the
    /// pen is never told what a stool IS, only that there is one here and whether anybody is on it.
    /// </summary>
    /// <param name="X">Where it is bolted down.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Taken">Somebody is on it.</param>
    /// <param name="RowHasSomebody">Anybody at all is on this counter. A free seat beside somebody is the
    /// same offer an empty chair at an occupied table is, and it is drawn in the same ink — one language
    /// for one question, rather than the counter having its own dialect of "you may sit here".</param>
    public readonly record struct StoolSpot(
        float X, float Y, bool Taken = false, bool RowHasSomebody = false);

    /// <summary>
    /// #793 · ONE END OF A PARK BENCH. A bench is a seat with two ends
    /// (<see cref="SpaceSails.Core.ParkBenches"/>), and which of them is free is the whole privacy
    /// predicate — so the deck answers it the way #792 taught it to answer a stool: one entry per SEAT,
    /// occupancy handed down, and the pen told nothing about what a park is.
    /// </summary>
    /// <param name="X">Where that end is.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Taken">Somebody is on it.</param>
    /// <param name="BenchHasSomebody">Anybody at all is on this bench. A free end beside somebody is the
    /// same offer a free chair at an occupied top is, and it is drawn in the same ink — one language for one
    /// question (#795), rather than the park having its own dialect of "you may sit here".</param>
    public readonly record struct BenchSpot(
        float X, float Y, bool Taken = false, bool BenchHasSomebody = false);

    /// <summary>Round table tops drawn as a ring on the floor — cantina/bar dressing. Plan-driven so
    /// any room (the ship's cantina, a haven bar) can lay out its own.</summary>
    public TableTop[] Tables { get; }

    /// <summary>#792 · The tall seats along a counter, in the row's own order. Empty everywhere there is no
    /// counter, which is every deck in the game but the Hive's cantina hall.</summary>
    public StoolSpot[] Stools { get; }

    /// <summary>#793 · The park's bench ends, two per bench, in the room's own bench order — bench <c>i</c>
    /// is entries <c>2i</c> and <c>2i+1</c>. Empty everywhere there is no park, which is every deck in the
    /// game but B1 of a branch office.</summary>
    public BenchSpot[] BenchSeats { get; }
}
