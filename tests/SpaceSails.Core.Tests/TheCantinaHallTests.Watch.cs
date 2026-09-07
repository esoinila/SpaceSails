using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// <b>WHO IS IN THE HALL, AND WHEN</b> — sections (l), (d) and (g) of
/// <see cref="TheCantinaHallTests"/>.
///
/// <para>What this part owns is the hall as a room with people in it rather than a room with chairs in it:
/// the mess that stays empty on every watch forever, the watch that is the mood — busier at some hours than
/// at others — the named regulars keeping their tables inside that crowd, and the fourteen barks, every one
/// of them reachable, stable for its watch, wired verbatim, and explaining NOTHING.</para>
/// </summary>
public sealed partial class TheCantinaHallTests
{
    // ── (l) THE MESS STAYS EMPTY. FOREVER. ────────────────────────────────────────────────────────────

    /// <summary>
    /// Nobody is ever in the staff mess, on any watch, on any site.
    ///
    /// <para><b>Proven RED</b> by letting the crowd seeder see it — dropping the <c>Use ==
    /// UpperCanteen</c> clause from <c>CanteenRegulars.Crowd</c>, which is the one-line change that turns
    /// #743's whole room into a lunch rush:</para>
    /// <code>
    /// 480 watch(es) put somebody in a room nobody has come to:
    ///   luna B13 watch 0: 4 of 13 tops taken
    ///   luna B13 watch 1: 11 of 13 tops taken
    ///   luna B13 watch 2: 12 of 13 tops taken
    ///   luna B13 watch 5: 2 of 13 tops taken
    /// </code>
    /// </summary>
    [Fact]
    public void TheMessIsEmptyOnEveryWatchForever()
    {
        var wrong = new List<string>();
        int watches = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.StaffCanteenFloor(body) is not { } level
                || HallOn(body, level, UndergroundComplex.Comfort.StaffCanteen) is not { } mess)
            {
                continue;
            }

            for (long watch = 0; watch < 12; watch++)
            {
                watches++;
                int taken = CanteenRegulars.OccupiedTops(body, level, mess, watch);
                if (taken > 0)
                {
                    wrong.Add(
                        $"  {body} B{-level} watch {watch}: {taken} of {mess.Tables.Count} tops taken");
                }
                Assert.Empty(CanteenRegulars.Sitting(body, level, mess, watch));
            }
        }

        Assert.True(watches > 300, $"only {watches} watches were walked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} watch(es) put somebody in a room nobody has come to:\n"
            + string.Join("\n", wrong.Take(12)));
    }

    // ── (d) THE WATCH IS THE MOOD ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The hall heaves on one watch and echoes on another, and nothing anywhere says so.
    ///
    /// <para><b>Proven RED</b> by pinning the fill to one watch's figure — the shape #709 had to fix once
    /// already, a room seeded off the site alone and therefore the same room forever:</para>
    /// <code>
    /// luna B1: the busy watch holds 19 tables and the small one 19 — walking in at two different hours
    ///          must be walking into two different rooms.
    /// </code>
    /// </summary>
    [Fact]
    public void TheHallIsBusierOnSomeWatchesThanOnOthers()
    {
        var thin = new List<string>();
        int halls = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || HallOn(body, level, UndergroundComplex.Comfort.UpperCanteen) is not { } hall)
            {
                continue;
            }

            halls++;
            var counts = new List<int>();
            for (long watch = 0; watch < UndergroundComplex.CabinetsPerHall * 4; watch++)
            {
                counts.Add(CanteenRegulars.OccupiedTops(body, level, hall, watch));
            }

            int distinct = counts.Distinct().Count();
            if (distinct < 4)
            {
                thin.Add(
                    $"  {body} B{-level}: {distinct} distinct occupancy(ies) across {counts.Count} watches "
                    + $"({string.Join(", ", counts.Take(6))}, …)");
            }

            // TWO WATCHES, MEASURED DIFFERENT — the owner's heaving day and dozen-soul night, named.
            int heaving = CanteenRegulars.OccupiedTops(body, level, hall, 2);
            int quiet = CanteenRegulars.OccupiedTops(body, level, hall, 5);
            Assert.True(heaving > quiet + 5,
                $"{body} B{-level}: the busy watch holds {heaving} tables and the small one {quiet} — "
                + "walking in at two different hours must be walking into two different rooms.");

            // …and both of them are the same room every time you look at them, which is #709's own law.
            Assert.Equal(heaving, CanteenRegulars.OccupiedTops(body, level, hall, 2));
            Assert.Equal(
                CanteenRegulars.Tables(body, level, hall, 2),
                CanteenRegulars.Tables(body, level, hall, 2));
        }

        Assert.True(halls > 40, $"only {halls} halls were measured — this proved little.");
        Assert.True(thin.Count == 0,
            $"the hall does not change with the shift:\n{string.Join("\n", thin.Take(12))}");
    }

    /// <summary>The named regulars are still the named regulars: never doubled up with the crowd, never
    /// wearing a stranger's plate, and still never more than #709's three at once.</summary>
    [Fact]
    public void TheNamedRegularsKeepTheirTablesInsideTheCrowd()
    {
        int seenNamed = 0, seenStrangers = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || HallOn(body, level, UndergroundComplex.Comfort.UpperCanteen) is not { } hall)
            {
                continue;
            }

            for (long watch = 0; watch < 6; watch++)
            {
                var tops = CanteenRegulars.Tables(body, level, hall, watch);
                var named = tops.Where(t => t.Taken && !t.Stranger).ToList();
                var crowd = tops.Where(t => t.Stranger).ToList();

                seenNamed += named.Count;
                seenStrangers += crowd.Count;

                Assert.InRange(named.Count, 1, CanteenRegulars.MostAtOnce);

                // Nobody is at two tables, and no top holds two people.
                Assert.Equal(tops.Count, tops.Select(t => t.Index).Distinct().Count());
                Assert.Equal(named.Count, named.Select(t => t.Plate).Distinct().Count());

                // A stranger never wears a name the #746 machine would read as one of the three.
                foreach (CanteenRegulars.TableSeat t in crowd)
                {
                    Assert.Equal(CanteenTable.Who.None, CanteenTable.WhoIs(t.Plate));
                    Assert.Contains(t.Plate!, CanteenRegulars.StrangerPlates);
                    Assert.Equal(0, t.Cabinet);
                }

                // …and the ones the room seats against #709's rota are exactly the ones Sitting names.
                var sitting = CanteenRegulars.Sitting(body, level, hall, watch);
                Assert.Equal(sitting.Count, named.Count);
                foreach (CanteenRegulars.Seated s in sitting)
                {
                    Assert.Contains(named, t => t.Plate == s.Plate && t.X == s.X && t.Y == s.Y);
                }
            }
        }

        Assert.True(seenNamed > 100, $"only {seenNamed} regulars seated — this proved little.");
        Assert.True(seenStrangers > 1000, $"only {seenStrangers} strangers seated — the hall is not full.");
    }

    // ── (g) THE BARKS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Every one of the fourteen barks is actually reachable, and each is stable per (patron, watch).
    ///
    /// <para><b>Proven RED</b> by the exact mistake the second addendum invited: the two FANCY barks were
    /// APPENDED to a pool of twelve, and the draw was left rolling over the old width
    /// (<c>Roll(seed, Barks.Count - 2)</c>). Nothing breaks, nothing looks wrong, and two authored lines
    /// are simply never said by anybody:</para>
    /// <code>
    /// 2 bark(s) nobody ever says:
    ///   Real coffee. On a rock like this. Somebody's writing it off against something.
    ///   Brass pillars on a freight stop. I stopped asking the questions I like the answers to.
    /// </code>
    /// </summary>
    [Fact]
    public void AllFourteenBarksAreReachableAndEachIsStableForItsWatch()
    {
        Assert.Equal(14, CanteenRegulars.Barks.Count);

        var heard = new HashSet<string>(StringComparer.Ordinal);
        int drawn = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || HallOn(body, level, UndergroundComplex.Comfort.UpperCanteen) is not { } hall)
            {
                continue;
            }

            for (long watch = 0; watch < 6; watch++)
            {
                foreach (CanteenRegulars.TableSeat t in CanteenRegulars.Tables(body, level, hall, watch))
                {
                    if (!t.Stranger)
                    {
                        continue;
                    }

                    drawn++;
                    Assert.Contains(t.Line!, CanteenRegulars.Barks);
                    heard.Add(t.Line!);

                    // Deterministic per (patron, watch): the same table on the same shift says the same
                    // thing, and the index the room drew is the index the law hands out.
                    Assert.Equal(
                        CanteenRegulars.Barks[CanteenRegulars.BarkIndex(body, t.Index, watch)], t.Line);
                    Assert.Equal(
                        CanteenRegulars.BarkIndex(body, t.Index, watch),
                        CanteenRegulars.BarkIndex(body, t.Index, watch));
                }
            }
        }

        Assert.True(drawn > 1000, $"only {drawn} barks drawn — this proved little.");

        var unsaid = CanteenRegulars.Barks.Where(b => !heard.Contains(b)).ToList();
        Assert.True(unsaid.Count == 0,
            $"{unsaid.Count} bark(s) nobody ever says:\n  {string.Join("\n  ", unsaid)}");
    }

    /// <summary>The pool is the owner's twelve plus the two the FANCY register added, verbatim and in
    /// order. Pinned literally, because "wired verbatim, change none of them" is the brief.</summary>
    [Fact]
    public void TheBarkPoolIsWiredVerbatim()
    {
        Assert.Equal(
            "Two more runs and I'm wintering somewhere with weather.", CanteenRegulars.Barks[0]);
        Assert.Equal(
            "They pay on the nail here. You don't ask what the nail's in.", CanteenRegulars.Barks[1]);
        Assert.Equal("Cage crew again? Wear the good gloves.", CanteenRegulars.Barks[2]);
        Assert.Equal(
            "I had a mate went down-contract. Sends money home regular. Never writes.",
            CanteenRegulars.Barks[3]);
        Assert.Equal(
            "The coffee's the same on every rock. That's either comforting or it isn't.",
            CanteenRegulars.Barks[4]);
        Assert.Equal(
            "Don't sit near the board on rota day unless you want work.", CanteenRegulars.Barks[5]);
        Assert.Equal(
            "Somebody asked the counter what's below. Funniest thing — nobody remembers who.",
            CanteenRegulars.Barks[6]);
        Assert.Equal(
            "Freight doesn't weigh what the manifest says. Freight never weighs what the manifest says.",
            CanteenRegulars.Barks[7]);
        Assert.Equal("First week? It shows. Sit down, it wears off.", CanteenRegulars.Barks[8]);
        Assert.Equal(
            "The lift's polite. That's more than I can say for the last three sites.",
            CanteenRegulars.Barks[9]);
        Assert.Equal(
            "You get used to the hum. Then one day it stops and you find out you liked it.",
            CanteenRegulars.Barks[10]);
        Assert.Equal("Keep your name simple here. They'll shorten it anyway.", CanteenRegulars.Barks[11]);

        // …and the fancy register (#601's funding trail, overheard rather than stated).
        Assert.Equal(
            "Real coffee. On a rock like this. Somebody's writing it off against something.",
            CanteenRegulars.Barks[12]);
        Assert.Equal(
            "Brass pillars on a freight stop. I stopped asking the questions I like the answers to.",
            CanteenRegulars.Barks[13]);
    }

    /// <summary>§13.8, over the crowd. Eighty strangers is eighty chances to explain something, which makes
    /// this the largest single body of prose in the building and the most dangerous.</summary>
    [Fact]
    public void NotOneStrangerExplainsANYTHING()
    {
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        var prose = CanteenRegulars.AllStrangerProse().ToList();
        Assert.Equal(CanteenRegulars.StrangerPlates.Count + CanteenRegulars.Barks.Count, prose.Count);

        foreach (string line in prose)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // The register test, one door along from #701's: every plate reads as a person at a glance and
        // every one of them is boring.
        foreach (string plate in CanteenRegulars.StrangerPlates)
        {
            Assert.StartsWith(CanteenRegulars.Glyph, plate, StringComparison.Ordinal);
            Assert.Equal(plate.ToUpperInvariant(), plate);
            Assert.DoesNotContain(plate, CanteenRegulars.AllProse());
        }
    }
}
