using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #608 · <b>AND WHAT THE DECADES DID TO IT</b> — the third part of
/// <see cref="TheRefugesUndergroundTests"/>.
///
/// <para>What this part owns is the owner's reversal of 2026-09-06: a refuge is built to code and inspected,
/// so it HOLDS unless somebody did something to it. Holding is the default on every department in every
/// band; at most one refuge per site is ever found failed, and never the first one a captain reaches; and
/// the inspection tag is the canon's own entries read off the site's own clock rather than a constant
/// wearing a function's clothes.</para>
/// </summary>
public sealed partial class TheRefugesUndergroundTests
{
    // ── #608 · AND WHAT THE DECADES DID TO IT ───────────────────────────────────────────────────────────
    //
    // The guards above are about what was BUILT, and none of them is touched: every airless floor still
    // carries a refuge, it is still never beside the lift, and the plan still marks it. What follows is the
    // other half of the issue's own "Done when" — "its state is part of the story (holds / holds but empty /
    // failed), and a working one can refill a tank" — and the reason it matters is the owner's warning in
    // the same comment: "If every ADMINISTRATION floor is safe, deep ADMINISTRATION floors stop costing
    // anything. The state of the seal is what keeps it honest."

    [Fact]
    public void ARefugeHoldsUnlessSomebodyDidSomethingToIt()
    {
        // #1149 · THE RARITY PIN, RE-MEASURED, and it is the owner's ruling turned into three numbers.
        // #1087 pinned 21.2 / 38.5 / 40.2 off a maintenance line that either survived or did not; that was
        // our mechanic. The world's answer is that a refuge is built to a robustness spec and holds — so
        // HOLDING is now the overwhelming majority, EMPTY is a visitor's footprint, and FAILED is an event
        // that most buildings simply do not have.
        //
        // WHAT MAKES THIS ABLE TO FAIL, which is the house rule this repo names out loud: it counts all
        // THREE states and demands all three in quantity, with a ceiling as well as a floor on each. An
        // assertion that "most refuges hold" would pass beautifully on a build where every refuge in the
        // game holds — which is the version of this feature that costs nothing and is exactly what the
        // owner's "if for dramatic suspense we need one that does not work" forbids. Both ends are nailed
        // down on all three, so the only build that goes green is one that deals all three states at these
        // rates.
        int holding = 0, empty = 0, failed = 0;
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                switch (UndergroundComplex.StateOfTheRefugeOn(body, level))
                {
                    case UndergroundComplex.RefugeState.Holding: holding++; break;
                    case UndergroundComplex.RefugeState.Empty: empty++; break;
                    case UndergroundComplex.RefugeState.Failed: failed++; break;
                    default: break;   // a floor that holds pressure has no refuge to have a state
                }
            }
        }

        int dead = holding + empty + failed;
        Assert.True(dead > 500, $"only {dead} dead floor(s) swept — this net proves nothing.");

        // Measured at this commit over 100 sites: 816 dead floors — 660 holding (80.9%), 133
        // drawn down (16.3%), 23 failed (2.8%), the last of those on 23 of the 100 sites.
        double works = 100.0 * holding / dead;
        Assert.True(works is > 72 and < 90,
            $"{holding} of {dead} refuges ({works:F1}%) still have air in the rack. Pinned at 80.9 %: "
            + "the owner's ruling is that these things almost never fail, and a build that put working air "
            + "on only half of them would be back to #1087's mechanic by a different route.");

        double drawn = 100.0 * empty / dead;
        Assert.True(drawn is > 9 and < 25,
            $"{empty} of {dead} racks were drawn down before the captain arrived ({drawn:F1}%) — pinned at "
            + "16.3 %. It is #573's footprint and not decay: rare enough that finding one still means "
            + "somebody was here, common enough that a captain meets one.");

        double gone = 100.0 * failed / dead;
        Assert.True(gone is > 1 and < 7,
            $"{failed} of {dead} seals have gone ({gone:F1}%) — pinned at 2.8 %. It is an EVENT and the "
            + "card with the painting is the whole of it; at this rate a captain who works a dozen sites "
            + "meets it a few times, which is a story rather than weather.");
    }

    [Fact]
    public void HoldingIsTheDefaultOnEveryDepartmentInEveryBand()
    {
        // #1149 · THE GUARD THAT REDDENS #1087. The old law was a biconditional — a refuge holds if and only
        // if its department kept a maintenance line (ADMINISTRATION and LABORATORIES, plus the head office,
        // minus the band nobody listed). This asserts the opposite, in the shape that can tell pass from
        // fail: every one of the eight departments, the head office, the branch offices AND the band nobody
        // listed all deal HOLDING refuges in quantity.
        //
        // Restore DepartmentsThatKeptTheLine and this goes red on six departments at once — and on the
        // unlisted band, which under the old law kept none of its seals because "a maintenance line is a
        // budget code and there is no budget code for a floor the building refuses to admit it has". That
        // sentence was good and it was ours; the inspectorate that made them build the room does not read
        // the org chart.
        var holdingBy = new Dictionary<string, int>(StringComparer.Ordinal);
        var seenBy = new Dictionary<string, int>(StringComparer.Ordinal);
        int unlistedHolding = 0, unlisted = 0, headHolding = 0, head = 0, branchHolding = 0, branch = 0;

        foreach (string body in ManySites())
        {
            bool headOffice = UndergroundComplex.IsHeadOffice(body);
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.StateOfTheRefugeOn(body, level) is not { } state)
                {
                    continue;
                }
                bool holds = state == UndergroundComplex.RefugeState.Holding;
                string department = UndergroundComplex.DepartmentOf(body, level);

                seenBy[department] = seenBy.GetValueOrDefault(department) + 1;
                if (holds)
                {
                    holdingBy[department] = holdingBy.GetValueOrDefault(department) + 1;
                }

                if (UndergroundComplex.IsUnlisted(body, level))
                {
                    unlisted++;
                    if (holds) { unlistedHolding++; }
                }
                else if (headOffice)
                {
                    head++;
                    if (holds) { headHolding++; }
                }
                else
                {
                    branch++;
                    if (holds) { branchHolding++; }
                }
            }
        }

        // The world can tell pass from fail, and the shape of "every department" is not the whole plate
        // list — which is worth writing down because it is the sharpest thing this sweep found.
        //
        // A branch cycles eight plates (DepartmentsFor) and the top floor of every four-floor band holds
        // pressure (HoldsPressure), so the two plates at indices 0 and 4 — ADMINISTRATION and ARCHIVE — are
        // the LOBBY plates and never carry a refuge at all. #1087's law gave working air to
        // "ADMINISTRATION and LABORATORIES"; half of that was a department with no refuge in it on any
        // floor of any site in the game. Six branch plates are the ones a refuge can wear, and all six have
        // to be here.
        var expected = new List<string>();
        for (int i = 0; i < UndergroundComplex.Departments.Length; i++)
        {
            if (i % UndergroundComplex.FloorsPerShaft != 0)
            {
                expected.Add(UndergroundComplex.Departments[i]);
            }
        }
        Assert.Equal(6, expected.Count);

        Assert.True(unlisted > 20, $"only {unlisted} floor(s) of the band nobody listed — untested.");
        Assert.True(head > 10, $"only {head} head-office floor(s) — untested.");
        Assert.True(branch > 300, $"only {branch} branch-office floor(s) — untested.");

        foreach (string department in expected)
        {
            int seen = seenBy.GetValueOrDefault(department);
            Assert.True(seen > 30,
                $"{department}: only {seen} refuge(s) swept over a hundred sites — this says nothing.");
            int kept = holdingBy.GetValueOrDefault(department);
            Assert.True(100.0 * kept / seen > 60,
                $"{department}: {kept} of {seen} refuges hold ({100.0 * kept / seen:F1}%). A refuge is a "
                + "regulation and not a maintenance line — no department in this building may be a "
                + "department whose safety equipment does not work.");
        }

        // …and the head office's own un-repeated plates are in here too, so the rule is proved blind to
        // twenty-four more words and not merely to six.
        Assert.True(seenBy.Count > 20,
            $"only {seenBy.Count} distinct plates over a hundred sites — the head office is not in this "
            + "sweep and the rank exception is therefore untested.");

        Assert.True(100.0 * unlistedHolding / unlisted > 60,
            $"{unlistedHolding} of {unlisted} refuges on the band nobody listed hold "
            + $"({100.0 * unlistedHolding / unlisted:F1}%). The floor the building will not admit to still "
            + "had people working on it in suits, and the same inspectorate made somebody pay for the room.");
        Assert.True(100.0 * headHolding / head > 60, "the head office's refuges stopped holding.");
        Assert.True(100.0 * branchHolding / branch > 60, "a branch office's refuges stopped holding.");
    }

    [Fact]
    public void AtMostOneRefugeFailedPerSiteAndNeverTheFirstOneReached()
    {
        // #1149 · The owner's word is that a failed refuge is a thing that HAPPENED, and a thing that
        // happened happens once. Two on one site would be weather.
        //
        // And never the first one a captain reaches, which is the half that makes the beat work: a captain's
        // first refuge is where they learn what a refuge IS, and a first one that will not cycle teaches the
        // opposite of the truth. "First" is read off the order the plan already has (FloorsOf), so it is the
        // first door on any route rather than a second idea of first.
        //
        // WHAT MAKES IT ABLE TO FAIL: it counts the sites that HAVE one and the sites that do not, and
        // demands both in quantity. "At most one per site" is satisfied trivially by a build with none.
        int sitesWithOne = 0, sitesWithNone = 0, reached = 0;

        foreach (string body in ManySites())
        {
            int failedFloors = 0;
            int? first = UndergroundComplex.FirstRefugeFloorOf(body);
            Assert.NotNull(first);

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.StateOfTheRefugeOn(body, level)
                    != UndergroundComplex.RefugeState.Failed)
                {
                    continue;
                }
                failedFloors++;
                Assert.True(level != first,
                    $"{body} B{-level}: the FIRST refuge a captain can reach on this site is the one that "
                    + "failed. That is the one room in the building that has to work — it is where the rule "
                    + "is taught, and the beat only lands against a rule the captain has already learnt.");
                reached++;

                // …and the site's own answer agrees with the floor's, which is what stops the card, the
                // plate, the fan and the suit reading two different rooms.
                Assert.Equal(level, UndergroundComplex.FailedRefugeFloorOf(body));
                Assert.True(UndergroundComplex.RefugeThatFailedIsOn(body, level));
            }

            Assert.True(failedFloors <= 1,
                $"{body}: {failedFloors} refuges failed on one site. It is an event, not a rate.");
            if (failedFloors == 1) { sitesWithOne++; } else { sitesWithNone++; }
        }

        Assert.True(sitesWithOne is > 10 and < 45,
            $"{sitesWithOne} of 100 sites carry the one that failed — pinned at one site in four. Rare is "
            + "measured against the thing it is rare among, and a captain works a site, not a floor.");
        Assert.True(sitesWithNone > 50,
            $"only {sitesWithNone} sites have no failed refuge at all — most buildings a captain walks are "
            + "buildings where the safety equipment simply works, which is the whole ruling.");
        Assert.True(reached > 10, "no failed refuge was ever reached — this guard proved nothing.");
    }

    [Fact]
    public void TheInspectionTagIsTheCanonsEntriesOnTheSitesOwnClock()
    {
        // #1149 · THE COVERT ORGANISATION'S PARADOX, ON PAPER. Owner: a secret lab's eternal struggle is
        // "not to asphyxiate from unmaintained safety equipment ... while avoiding traceable bureaucracy
        // that could prove complicity if leaked". So: complete, current, unsigned — and the paper says so
        // itself, as a house rule, which is the whole of the characterisation.
        //
        // The two entries are RETYPED from the issue here rather than read off the constants, for the reason
        // ThePapersOwnHeadsTests states: a guard asserting InspectionTagEntry == InspectionTagEntry passes
        // on any sentence anybody ever writes into it.
        const string Entry =
            "Refuge inspected. Rack full, seals within tolerance. No signature — none required.";
        const string Replaced = "Refuge inspected. Rack full. Seal replaced.";
        Assert.Equal(Entry, UndergroundComplex.InspectionTagEntry);
        Assert.Equal(Replaced, UndergroundComplex.InspectionTagSealReplaced);

        int ordinary = 0, failed = 0;
        var years = new HashSet<int>();

        foreach (string body in ManySites())
        {
            int year = UndergroundComplex.InspectionYearOf(body);
            int month = UndergroundComplex.InspectionMonthOf(body);
            years.Add(year);

            // The two stamps are a year apart to the month, off the site's own clock, and the clock sits in
            // the era the rest of the game keeps (ShipHistory lays hulls down 2270..2319 against a present
            // of roughly 2341) — so an inspection on this tag is decades old, which is the sentence every
            // other surface down here is already telling in words.
            Assert.InRange(year, 2270, 2319);
            Assert.InRange(month, 1, 12);
            string first = UndergroundComplex.InspectionTagStamp(year, month);
            string second = UndergroundComplex.InspectionTagStamp(year + 1, month);
            Assert.NotEqual(first, second);

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.StateOfTheRefugeOn(body, level) is not { } state)
                {
                    // No refuge on the plan, no tag: the paper is a property of the room and there is no
                    // room. (A floor that breathes IS the refuge and carries no valve to hang one on.)
                    Assert.Null(UndergroundComplex.AuthoredPaperOf(
                        UndergroundComplex.FindId(body, level, UndergroundComplex.RefugeTagRoom)));
                    continue;
                }

                string tag = UndergroundComplex.InspectionTagLine(body, level);

                // Every tag in the game: the same entry twice, stamped a year apart, in that order.
                Assert.Contains(first + Entry, tag, StringComparison.Ordinal);
                Assert.Contains(second + Entry, tag, StringComparison.Ordinal);
                Assert.True(
                    tag.IndexOf(first, StringComparison.Ordinal)
                        < tag.IndexOf(second, StringComparison.Ordinal),
                    $"{body} B{-level}: the tag reads back to front.");

                if (state == UndergroundComplex.RefugeState.Failed)
                {
                    failed++;

                    // THE THIRD ENTRY, AND IT HAS NO DATE ON IT. That is the beat's second half and it is
                    // delivered by an absence: a book that has never once failed to stamp a line did not
                    // stamp this one.
                    //
                    // The assertion is that the second entry runs STRAIGHT into the third with one space
                    // between them, which is the only shape that leaves nowhere for a stamp to be. This was
                    // first written as "no year appears after the third entry begins", and that read the
                    // wrong side of the join: a stamp sits BEFORE its entry, so a dated third entry sailed
                    // through it. A guard handed a world that cannot tell pass from fail is a bug class this
                    // repo has a name for, and it was this one.
                    Assert.EndsWith(Entry + " " + Replaced, tag, StringComparison.Ordinal);
                    for (int y = year - 1; y <= year + 3; y++)
                    {
                        Assert.Equal(
                            state == UndergroundComplex.RefugeState.Failed && (y == year || y == year + 1),
                            tag.Contains(
                                y.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                StringComparison.Ordinal));
                    }
                }
                else
                {
                    ordinary++;
                    Assert.DoesNotContain(Replaced, tag, StringComparison.Ordinal);
                    Assert.EndsWith(Entry, tag, StringComparison.Ordinal);
                }

                // …and the paper is reachable through the one seam every other authored paper is read
                // through, on a room index no floor's room list can hold.
                string findId = UndergroundComplex.FindId(body, level, UndergroundComplex.RefugeTagRoom);
                Assert.Equal(PaperHeads.Paper.InspectionTag, UndergroundComplex.AuthoredPaperOf(findId));
                Assert.Equal("An inspection tag", FieldClue.Title(findId));
                Assert.Equal(Entry, FieldClue.Document(findId));
            }
        }

        // The world can tell pass from fail: both kinds of tag really occur, and the clock really is the
        // SITE'S — one year for every site would be a constant wearing a function's clothes.
        Assert.True(ordinary > 500, $"only {ordinary} ordinary tag(s) swept.");
        Assert.True(failed > 10, $"only {failed} failed refuge(s) swept — the third entry is untested.");
        Assert.True(years.Count > 20,
            $"only {years.Count} distinct inspection years over 100 sites — this is not a site's own clock.");
    }
}
