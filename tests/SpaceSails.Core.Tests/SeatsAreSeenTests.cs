using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #792 · THE THREE GLANCES, IN CORE — the facts the deck reads to answer them.
///
/// <para>Owner, playtest 2026-08-08: <i>"people looking to sit down look at those like hungry wild beasts
/// look at their prey"</i> · <i>"Now I have trouble finding a free table… lol story of my travelling life
/// right here."</i> · <i>"it determines whether there is anything to overhear as discussion goes."</i></para>
///
/// <para>Two of the three glances were already answerable — a top knows if somebody is at it, a stool has
/// known its occupancy watch by watch since #756 — and neither was anywhere on the floor. The third,
/// ALONE versus TALKING, had no honest source at all, so this file is where the new one is pinned. The
/// DRAWING is guarded next door (<c>SeatsAreDrawnTests</c>); these are the facts under it.</para>
/// </summary>
public sealed class SeatsAreSeenTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    private static UndergroundComplex.Amenity? CantinaOn(string body, int level)
    {
        foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities)
        {
            if (a.Use == UndergroundComplex.Comfort.UpperCanteen && a.Hall is not null)
            {
                return a;
            }
        }
        return null;
    }

    /// <summary>How many watches a day is. Six, and every sweep below walks all of them — a room whose
    /// whole mood is the shift is a room one watch tells you nothing about.</summary>
    private const int Watches = 6;

    // ── (a) ALONE vs TALKING — THE NEW FACT, AND WHY IT IS NOT A WORD COUNT ───────────────────────────

    /// <summary>
    /// #792 · A TOP KNOWS WHETHER ITS PEOPLE ARE TALKING, AND IT IS NOT DERIVABLE FROM THE PLATE.
    ///
    /// <para>This is the guard that stops the cheap implementation coming back. Read the plates and count
    /// the people in them — TWO HAULIERS is two, A PAIR is two, THREE ON THE SAME CONTRACT is three — and
    /// you get the right answer nine times out of ten and the wrong one on <i>A COUPLE NOT TALKING</i>,
    /// which is two people with nothing whatever to overhear and says so in words. So the two plates below
    /// are the whole test: same headcount, opposite answer.</para>
    ///
    /// <para><b>Proven RED</b> by the plural rule, which is exactly what an author in a hurry writes:</para>
    /// <code>
    /// Failed …TALKING_IsAuthoredBesideThePlateAndIsNotThePluralRule [19 ms]
    ///   the plural rule got '◈ A COUPLE NOT TALKING' wrong — two people, and the one thing the
    ///   plate says about them is that there is nothing to overhear.
    /// </code>
    /// </summary>
    [Fact]
    public void TALKING_IsAuthoredBesideThePlateAndIsNotThePluralRule()
    {
        int couple = CanteenRegulars.StrangerPlates.ToList().IndexOf("◈ A COUPLE NOT TALKING");
        int pair = CanteenRegulars.StrangerPlates.ToList().IndexOf("◈ A PAIR FROM THE SCAFFOLD GANG");
        Assert.True(couple >= 0 && pair >= 0,
            "the two plates this law is measured on have been renamed — the guard is reading a room that " +
            "no longer exists.");

        Assert.True(CanteenRegulars.StrangerTalks(pair),
            "a pair from the scaffold gang are not talking to each other, which leaves the hall with " +
            "nothing to overhear anywhere in it.");
        Assert.False(CanteenRegulars.StrangerTalks(couple),
            "the plural rule got '◈ A COUPLE NOT TALKING' wrong — two people, and the one thing the " +
            "plate says about them is that there is nothing to overhear.");

        // …and the fact exists for every plate, not just for the two above: a plate added tomorrow has to
        // be decided tomorrow, and an eleventh face that fell off the end would answer false by accident.
        Assert.Equal(10, CanteenRegulars.StrangerPlates.Count);
        int talking = 0;
        for (int f = 0; f < CanteenRegulars.StrangerPlates.Count; f++)
        {
            if (CanteenRegulars.StrangerTalks(f))
            {
                talking++;
            }
        }
        Assert.True(talking > 0 && talking < CanteenRegulars.StrangerPlates.Count,
            $"{talking} of {CanteenRegulars.StrangerPlates.Count} plates are conversations — a fact that " +
            "selects everything or nothing cannot be read off a room.");
    }

    /// <summary>
    /// #792 · …and it reaches the tops, both ways, on the watches the game actually has.
    ///
    /// <para>The distinction is only worth drawing if a captain walking into the shipped hall meets both
    /// kinds of table, so this measures the real generator over all six watches rather than trusting the
    /// catalogue above. A NAMED regular is never a conversation — every one of the ten is one person, and
    /// that is #757's whole premise — and an empty top and a cabinet's top are nobody at all.</para>
    /// </summary>
    [Fact]
    public void BOTH_KINDS_OfOccupiedTopAreInTheShippedHallOnTheWatchesItHas()
    {
        int talking = 0, alone = 0, sweptWatchesWithBoth = 0, halls = 0;
        var wrong = new List<string>();

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || CantinaOn(body, level) is not { } hall)
            {
                continue;
            }
            halls++;

            for (long watch = 0; watch < Watches; watch++)
            {
                int here = 0, lonely = 0;
                foreach (CanteenRegulars.TableSeat top in
                         CanteenRegulars.Tables(body, level, hall, watch))
                {
                    if (!top.Taken)
                    {
                        if (top.Talking)
                        {
                            wrong.Add($"  {body} B{-level} w{watch} top {top.Index}: nobody is at it and " +
                                "it is drawn as a conversation.");
                        }
                        continue;
                    }
                    if (top.Talking && !top.Stranger)
                    {
                        wrong.Add($"  {body} B{-level} w{watch} top {top.Index}: a NAMED regular " +
                            $"('{top.Plate}') is holding a conversation with themselves.");
                    }
                    if (top.Talking && top.Alone)
                    {
                        wrong.Add($"  {body} B{-level} w{watch} top {top.Index}: talking AND alone.");
                    }
                    if (top.Talking)
                    {
                        here++;
                    }
                    else
                    {
                        lonely++;
                    }
                }

                talking += here;
                alone += lonely;
                if (here > 0 && lonely > 0)
                {
                    sweptWatchesWithBoth++;
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} top(s) report a conversation nobody is having:\n{string.Join("\n", wrong)}");

        // ANTI-VACUITY. A sweep that only ever met one kind of table would pass every assertion above
        // without the fact ever having been asked a question.
        Assert.True(halls >= 5, $"only {halls} hall(s) were walked — this sweep is not a sweep.");
        Assert.True(talking > 0, "no top anywhere on any watch is a conversation.");
        Assert.True(alone > 0, "no top anywhere on any watch holds somebody on their own.");
        Assert.True(sweptWatchesWithBoth > 0,
            "no single watch holds both a conversation and a lone sitter — the distinction is never " +
            "actually in front of a captain at one time, which is the only time it can be read.");
    }

    // ── (b) THE ROW OF TALL SEATS, PUT SOMEWHERE ─────────────────────────────────────────────────────

    /// <summary>
    /// #792 · THE STOOLS ARE ON THE FLOOR PLAN, IN THE ROW'S OWN ORDER.
    ///
    /// <para>Owner (#756, built): <i>"there should be high chairs so sitting at the bar desk is also
    /// possible."</i> Eight of them, occupied or not, every watch — and until now they existed only in
    /// <see cref="TheStools"/> and on the card, so nothing in the building was anywhere.</para>
    ///
    /// <para><b>Proven RED</b> on the shipped carve, which published no row at all:</para>
    /// <code>
    /// 9 problem(s) with the counters' rows:
    ///   luna B1: 0 stools published against TheStools.Count = 8
    ///   phobos B1: 0 stools published against TheStools.Count = 8
    ///   europa B1: 0 stools published against TheStools.Count = 8
    ///   …every serving counter in the sweep, which is every one of them.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_COUNTER_HasEightSeatsBoltedDownWhereItServes()
    {
        var wrong = new List<string>();
        int rows = 0;

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || CantinaOn(body, level) is not { Hall: { } hall } amenity)
            {
                continue;
            }

            bool serves = CounterService.For(body, UndergroundComplex.Comfort.UpperCanteen) is not null;
            IReadOnlyList<(double X, double Y)> row = hall.StoolRow;

            if (!serves)
            {
                if (row.Count != 0)
                {
                    wrong.Add($"  {body} B{-level}: {row.Count} stool(s) at a counter that serves nobody.");
                }
                continue;
            }

            rows++;
            if (row.Count != TheStools.Count)
            {
                wrong.Add($"  {body} B{-level}: {row.Count} stools published against " +
                    $"TheStools.Count = {TheStools.Count}");
                continue;
            }

            // Inside the room they belong to, distinct, and laid along one line in one direction — which
            // is what "the row's own order" means geometrically. A row published out of order would draw
            // one seat's occupancy over another seat's chair.
            for (int s = 0; s < row.Count; s++)
            {
                if (!hall.Contains(row[s].X, row[s].Y))
                {
                    wrong.Add($"  {body} B{-level}: stool {s + 1} stands outside its own hall.");
                }
            }

            bool alongX = System.Math.Abs(row[^1].X - row[0].X) > System.Math.Abs(row[^1].Y - row[0].Y);
            double first = alongX ? row[0].X : row[0].Y, last = alongX ? row[^1].X : row[^1].Y;
            int step = last > first ? 1 : -1;
            for (int s = 1; s < row.Count; s++)
            {
                double a = alongX ? row[s - 1].X : row[s - 1].Y;
                double b = alongX ? row[s].X : row[s].Y;
                if ((b - a) * step <= 0)
                {
                    wrong.Add($"  {body} B{-level}: stool {s + 1} does not follow stool {s} along the " +
                        "counter — the row is not in the row's own order.");
                }
            }

            // …and clear of the tops, because a stool laid on a table is a seat a captain would read as
            // belonging to the wrong fixture.
            foreach (CanteenRegulars.TableSeat top in CanteenRegulars.Tables(body, level, amenity))
            {
                foreach ((double sx, double sy) in row)
                {
                    if (System.Math.Abs(sx - top.X) < 1.0 && System.Math.Abs(sy - top.Y) < 1.0)
                    {
                        wrong.Add($"  {body} B{-level}: a stool stands on top {top.Index}.");
                    }
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} problem(s) with the counters' rows:\n{string.Join("\n", wrong)}");
        Assert.True(rows >= 5, $"only {rows} serving counter(s) were walked — this sweep is not a sweep.");
    }

    /// <summary>
    /// #792 · …and the row is never all one thing on a watch the game has.
    ///
    /// <para>The point of drawing occupancy is that it VARIES. If every watch left the row full, or every
    /// watch left it empty, the glyphs would be decoration — so this measures the six watches the day
    /// actually has and insists both states stand side by side on at least one of them, which is the only
    /// arrangement a captain can read a free seat OUT of.</para>
    /// </summary>
    [Fact]
    public void EVERY_WATCH_PutsAFreeStoolBesideATakenOne()
    {
        const string body = "luna";
        int level = UndergroundComplex.TopPressurisedFloor(body) ?? -1;

        var counts = new List<int>();
        for (long watch = 0; watch < Watches; watch++)
        {
            int taken = 0;
            for (int s = 0; s < TheStools.Count; s++)
            {
                if (TheStools.Taken(body, level, s, watch))
                {
                    taken++;
                }
            }
            counts.Add(taken);
            Assert.True(taken is > 0 and < TheStools.Count,
                $"watch {watch} leaves {taken} of {TheStools.Count} stools taken — a row that is all one " +
                "thing cannot show anybody a free seat beside a busy one.");
        }

        Assert.True(counts.Distinct().Count() > 1,
            $"every watch has the same {counts[0]} stools taken — the row does not read the shift at all.");
    }

    // ── (c) #823 · HOW MANY OF THEM THERE ARE ────────────────────────────────────────────────────────
    //
    // Owner, playtest 2026-08-11, sat at a canteen four-top: "it says there are two haulers eating at a
    // table that seats four, yet there is visual indication of only one seat out of four being taken? Does
    // the other try sit in the others lap while both try to eat?" — and then the law, stated:
    //
    //     "The number of people sitting at a table should match the amount of seats that are taken."
    //
    // Free was Seats - (Taken ? 1 : 0). A bool cannot seat a crew, and the plates have held crews since
    // #792. The headcount is authored beside the sentence for the same reason Talking is, and these are the
    // three questions that keeps it honest: is it there at all, does it agree with the words when the words
    // commit to a number, and does the room the game builds respect the furniture it has.

    /// <summary>
    /// #823 · EVERY PLATE HAS A HEADCOUNT, AND THE PLATES THAT NAME ONE AGREE WITH IT.
    ///
    /// <para>The authored fact is checked against the only external evidence there is — the words, on the
    /// plates that happen to commit to a number. It is NOT the plural rule reinstated: the rule cannot
    /// answer <i>A LOADER WITH A BAD WRIST</i> or <i>CAGE CREW, OFF SHIFT</i> at all, and it is asked here
    /// only where the sentence has already said the number out loud. A plate that says TWO and seats three
    /// is a caption disagreeing with a room, which is this project's third named bug class.</para>
    ///
    /// <para>Talking implies at least two, because you cannot converse alone — the one law that ties the
    /// two authored facts on a Face together, and the one an author adding an eleventh plate is most likely
    /// to get wrong.</para>
    ///
    /// <para><b>Proven RED</b> against the arithmetic that shipped the bug — every party a party of one
    /// (<c>StrangerHeads</c> returning 1, which is what <c>Seats - (Taken ? 1 : 0)</c> believed about every
    /// top in the building):</para>
    /// <code>
    /// Failed …EVERY_PLATE_SaysHowManyPeopleAreAtIt [5 ms]
    ///   9 plate(s) disagree with themselves:
    ///     '◈ CAGE CREW, OFF SHIFT' is a conversation held by 1 person.
    ///     '◈ TWO HAULIERS, EATING' is a conversation held by 1 person.
    ///     '◈ TWO HAULIERS, EATING' says TWO and seats 1.
    ///     '◈ SOMEBODY'S WHOLE CREW AT ONE TABLE' is a conversation held by 1 person.
    ///     '◈ A PAIR FROM THE SCAFFOLD GANG' is a conversation held by 1 person.
    ///     '◈ A PAIR FROM THE SCAFFOLD GANG' says PAIR and seats 1.
    ///     '◈ THREE ON THE SAME CONTRACT' is a conversation held by 1 person.
    ///     '◈ THREE ON THE SAME CONTRACT' says THREE and seats 1.
    ///     '◈ A COUPLE NOT TALKING' says COUPLE and seats 1.
    /// </code>
    /// </summary>
    [Fact]
    public void EVERY_PLATE_SaysHowManyPeopleAreAtIt()
    {
        // The words that commit to a number, and what they commit to. Nothing derives these — they are the
        // external evidence the authored field is measured against, which is the whole point of writing
        // them out here instead of counting them in the shipped code.
        var counted = new Dictionary<string, int>(System.StringComparer.Ordinal)
        {
            ["TWO"] = 2,
            ["THREE"] = 3,
            ["PAIR"] = 2,
            ["COUPLE"] = 2,
        };

        var wrong = new List<string>();
        int named = 0, parties = 0;

        for (int f = 0; f < CanteenRegulars.StrangerPlates.Count; f++)
        {
            string plate = CanteenRegulars.StrangerPlates[f];
            int heads = CanteenRegulars.StrangerHeads(f);

            if (heads < 1)
            {
                wrong.Add($"    '{plate}' seats {heads} people — nobody is at a top somebody is at.");
            }
            if (heads > 1)
            {
                parties++;
            }
            if (CanteenRegulars.StrangerTalks(f) && heads < 2)
            {
                wrong.Add($"    '{plate}' is a conversation held by {heads} person.");
            }

            foreach ((string word, int says) in counted)
            {
                if (!plate.Contains(word, System.StringComparison.Ordinal))
                {
                    continue;
                }
                named++;
                if (heads != says)
                {
                    wrong.Add($"    '{plate}' says {word} and seats {heads}.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} plate(s) disagree with themselves:\n{string.Join("\n", wrong)}");

        // ANTI-VACUITY. A catalogue of ten lone sitters would pass every line above without the field ever
        // having been asked a question, and a catalogue whose plates never say a number would have measured
        // the authored fact against nothing at all.
        Assert.Equal(10, CanteenRegulars.StrangerPlates.Count);
        Assert.True(parties > 0, "not one plate in the crowd is more than one person.");
        Assert.True(named >= 4,
            $"only {named} plate(s) name a number out loud — the words are not checking the field.");

        // …and a named regular is exactly one person, which is #757's whole premise: there is a chair, and
        // somebody to ask.
        Assert.Equal(1, CanteenRegulars.RegularHeads);
    }

    /// <summary>
    /// #823 · NO PARTY IS DEALT A TOP IT CANNOT SIT AT, AND EVERY TOP'S FREE CHAIRS ARE THE ONES IT HAS.
    ///
    /// <para>The catalogue above being right buys nothing if the DEAL ignores the furniture: a whole crew of
    /// four at a two-top is the owner's lap joke rendered by the generator instead of by the arithmetic. So
    /// this walks the shipped rooms — every site, every floor, all six watches — and asks the two questions
    /// a captain could ask by counting chairs.</para>
    ///
    /// <para><b>Proven RED</b> by putting <c>Seats - (Taken ? 1 : 0)</c> back — 331 tops in the shipped
    /// building, which is the size of the thing the owner caught one table of:</para>
    /// <code>
    /// Failed …NO_PARTY_IsSeatedAtATopThatCannotHoldIt [116 ms]
    ///   331 top(s) seat a party the furniture cannot hold:
    ///     luna B1 w0 top 12: '◈ TWO HAULIERS, EATING' — 2 at a 4-top reports 3 chairs free (expected 2).
    ///     luna B1 w1 top 0: '◈ A COUPLE NOT TALKING' — 2 at a 2-top reports 1 chairs free (expected 0).
    ///     luna B1 w2 top 3: '◈ SOMEBODY'S WHOLE CREW AT ONE TABLE' — 4 at a 4-top reports 3 chairs free
    ///       (expected 0).
    ///     …329 more, across every site and every watch.
    /// </code>
    /// </summary>
    [Fact]
    public void NO_PARTY_IsSeatedAtATopThatCannotHoldIt()
    {
        var wrong = new List<string>();
        int swept = 0, seated = 0, parties = 0, full = 0, rooms = 0;

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            foreach (UndergroundComplex.Amenity a in
                     UndergroundComplex.Build(body, level, Field).Amenities)
            {
                rooms++;
                for (long watch = 0; watch < Watches; watch++)
                {
                    foreach (CanteenRegulars.TableSeat top in
                             CanteenRegulars.Tables(body, level, a, watch))
                    {
                        swept++;
                        string where = $"    {body} B{-level} w{watch} top {top.Index}";

                        if (!top.Taken)
                        {
                            if (top.Heads != 0)
                            {
                                wrong.Add($"{where}: nobody is at it and it seats {top.Heads} people.");
                            }
                            if (top.Free != top.Seats)
                            {
                                wrong.Add($"{where}: empty, {top.Seats} seats, {top.Free} free.");
                            }
                            continue;
                        }

                        seated++;
                        if (top.Heads > 1)
                        {
                            parties++;
                        }
                        if (top.Free == 0)
                        {
                            full++;
                        }

                        if (top.Heads < 1)
                        {
                            wrong.Add($"{where}: '{top.Plate}' is at it and nobody is at it.");
                        }
                        if (top.Heads > top.Seats)
                        {
                            wrong.Add($"{where}: '{top.Plate}' — {top.Heads} at a {top.Seats}-top. " +
                                "Somebody is in somebody's lap.");
                        }
                        if (top.Free != top.Seats - top.Heads)
                        {
                            wrong.Add($"{where}: '{top.Plate}' — {top.Heads} at a {top.Seats}-top " +
                                $"reports {top.Free} chairs free (expected {top.Seats - top.Heads}).");
                        }
                    }
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} top(s) seat a party the furniture cannot hold:\n{string.Join("\n", wrong)}");

        // ANTI-VACUITY, in every direction the law can be vacuous in.
        Assert.True(rooms >= 10, $"only {rooms} room(s) were walked — this sweep is not a sweep.");
        Assert.True(swept > 500, $"only {swept} top(s) swept.");
        Assert.True(seated > 50, $"only {seated} occupied top(s) in the whole sweep.");
        Assert.True(parties > 0,
            "every occupied top in the sweep holds exactly one person — the law is never tested.");
        Assert.True(full > 0,
            "no top in the sweep is ever FULL — 'ask to join' is never once made to refuse, which is the " +
            "half of this fix that changes what the game says back.");
    }
}
