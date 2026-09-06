using System;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #1149 · REFUGES ARE BUILT TO CODE, AND THEY ALMOST NEVER FAIL FROM AGE ───────────────────────────
    //
    // Owner ruling, 2026-09-06, on #608, and it reverses the seeded split #1087 shipped (21 % holding /
    // 38 % empty / 40 % failed, keyed off "funded departments keep their air"):
    //
    //     "emergency air stations are built as part of standard safety rules — like bomb shelters in every
    //      big enough house in Finland, fire extinguishers and cabin evacuation cards on a ship — built and
    //      monitored to exist by inspectors ... On a failed floor the emergency station most probably still
    //      works decades or centuries after everything else stopped — robustness and reliability were the
    //      metrics it was built to. If for dramatic suspense we need one that does not work, that is
    //      narrated, with a gen-AI image: something scary or weird happened to the shelter. They almost
    //      never fail from old age; something happened, and we tell it."
    //
    // WHY DepartmentsThatKeptTheLine IS RETIRED, in one sentence: it was OUR mechanic and not the WORLD'S.
    // The old law said a refuge holds where the department could still get a maintenance line approved —
    // which is a good sentence about budgets and a wrong sentence about pressure vessels. A refuge is not a
    // service a department buys; it is a REGULATION a building is made to satisfy, by an inspectorate that
    // does not care whose cost centre the floor is on, built to a robustness spec precisely because the
    // people who wrote the spec assumed nobody would be maintaining it on the day it was needed. A fire
    // extinguisher in an abandoned office block still discharges. That is what those things are.
    //
    // So the three states survive and their CAUSES all change:
    //
    //   HOLDING · the default, on every floor, whatever the department, in every band including the one
    //             nobody listed. Nothing is rolled for it; it is what the building was built to.
    //   EMPTY   · NOT decay. Somebody DREW on it — the #573 reservoir idiom, a visitor's footprint — and it
    //             refills on the rack's own clock, exactly as a surface shelter does. Rare, seeded, and it
    //             costs a captain time rather than range.
    //   FAILED  · an EVENT, never age. At most one per site, on a minority of sites, and never the first
    //             refuge a captain reaches on that ground. Arriving at it raises the reveal card with its
    //             own painting (StoryBeats.Beat.RefugeFailed), which is the whole of the telling.
    //
    // What is NOT touched: every airless floor still carries a refuge (13.12), the plates still read
    // REFUGE · AIR / REFUGE · DRY / PRESSURE REFUGE at range, the panel row still says a refuge is THERE and
    // never what state it is in, and the failed one still paints on the fan and reads as dead.

    /// <summary>#1149 · How many sites carry a failed refuge — <b>one in this many</b>, seeded per body.
    ///
    /// <para>The owner's word is <i>rare</i>, and rare has to be measured against the thing it is rare
    /// among: a captain works a site, not a floor. One site in four means most buildings a captain walks are
    /// buildings where the safety equipment simply works, which is the ruling, and it still leaves the beat
    /// reachable often enough to be part of the game rather than a rumour. Over the hundred-site sweep it is
    /// three per cent of refuges — see <c>TheRefugesUndergroundTests</c>, which pins the measurement.</para></summary>
    public const int SitesPerFailedRefuge = 4;

    /// <summary>#1149 · How many refuges carry a visitor's footprint — <b>one in this many</b>, seeded per
    /// floor. Rare, because an emptied rack is a person who was here and took everything, and a building
    /// where every sixth room says that is a building somebody is living in.</summary>
    public const int RefugesPerDrawnRack = 6;

    /// <summary>#1149 · <b>THE FIRST REFUGE A CAPTAIN REACHES ON THIS SITE</b>, or null on a site with none.
    ///
    /// <para>Derived from the order the plan already has rather than from a second idea of "first":
    /// <see cref="FloorsOf"/> yields a site's floors in the order the shafts serve them, top of the first
    /// band downwards, through the gap, into the band nobody listed. The first of those that carries a
    /// refuge is the first door a captain can possibly open, on any route, because there is no route that
    /// reaches a deeper floor without passing it.</para>
    ///
    /// <para>It exists so <see cref="FailedRefugeFloorOf"/> can refuse it. A captain's first refuge is where
    /// they learn what a refuge IS — the plate, the rack, the two-thirds regulator — and a first one that
    /// will not cycle teaches the opposite of the truth: that these rooms are unreliable. The beat only
    /// works against a rule the captain has already been taught.</para></summary>
    public static int? FirstRefugeFloorOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        foreach (int level in FloorsOf(bodyId))
        {
            if (RefugeOnThePlan(bodyId, level))
            {
                return level;
            }
        }
        return null;
    }

    /// <summary>#1149 · <b>WHICH FLOOR OF THIS SITE HAS THE ONE THAT FAILED</b> — null on most sites, and
    /// null on every site whose only refuge is the first one.
    ///
    /// <para><b>At most one, and it is a fact about the SITE.</b> The owner's ruling is that a failed refuge
    /// is an event and not a rate, so it cannot be a per-floor roll: a per-floor roll deals two of them on
    /// some buildings, which turns an event into weather. The site is asked once whether it has a story of
    /// this kind, and then which floor it happened on.</para>
    ///
    /// <para><b>What it costs to ask.</b> Three sites in four answer with one roll and never walk a floor
    /// list. The remaining quarter walk <see cref="FloorsOf"/> twice — once to count the candidates, once to
    /// reach the chosen one — which is thirty-odd iterations of an integer loop and no allocation beyond the
    /// iterator itself. The alternative was a cached table, and a cache is a second answer to a question
    /// this file exists to keep single.</para></summary>
    public static int? FailedRefugeFloorOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // Rare first, so the common site pays one roll and nothing else.
        if (DiceRule.Roll(DiceRule.Seed($"hive:refuge-event:{bodyId}"), SitesPerFailedRefuge).Face != 1)
        {
            return null;
        }

        int first = FirstRefugeFloorOf(bodyId) ?? int.MinValue;
        int candidates = 0;
        foreach (int level in FloorsOf(bodyId))
        {
            if (level != first && RefugeOnThePlan(bodyId, level))
            {
                candidates++;
            }
        }
        if (candidates == 0)
        {
            return null;   // a site with one refuge keeps it: the first is never the failed one
        }

        int pick = DiceRule.Roll(DiceRule.Seed($"hive:refuge-event-floor:{bodyId}"), candidates).Face - 1;
        foreach (int level in FloorsOf(bodyId))
        {
            if (level == first || !RefugeOnThePlan(bodyId, level))
            {
                continue;
            }
            if (pick-- == 0)
            {
                return level;
            }
        }
        return null;
    }

    /// <summary>#1149 · Has somebody drawn this rack right down? The <see cref="SurfaceShelter"/> idiom of
    /// #573 — <i>a rack that is not full means somebody was here</i> — asked of a room a hundred and fifty
    /// metres under a moon, where it is a colder sentence. Seeded per floor, so a captain who learns a
    /// building learns it for good, and it is the ONLY thing that makes a refuge empty: the compressor is
    /// fine, the seals are fine, and there is nothing in the bottles because a stranger took it.</summary>
    public static bool SomebodyDrewTheRackDown(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return DiceRule.Roll(DiceRule.Seed($"hive:refuge-drawn:{bodyId}:{level}"), RefugesPerDrawnRack)
            .Face == 1;
    }

    // ── #1149 · THE INSPECTION TAG — THE COVERT ORGANISATION'S PARADOX, ON PAPER ─────────────────────────
    //
    // Owner, in the same ruling: a secret lab "needs its own trusted criminals: the eternal struggle of
    // covert organizations — not to asphyxiate from unmaintained safety equipment, to keep secrets, to trust
    // employees to bend the law only as much as the company approves, while avoiding traceable bureaucracy
    // that could prove complicity if leaked."
    //
    // That is a paradox with no sentence in it, and this is the paper it leaves behind. The tag is COMPLETE
    // — dated, current, the rack signed off as full — and UNSIGNED, and it says so itself, in the flattest
    // clerical register available: "No signature — none required." A building that will not put a name on a
    // safety inspection is a building that will not put a name on anything, and it went on doing the
    // inspection anyway, on time, for years, because the alternative was people dying in its corridors.
    // Nobody on the paper notices. Nobody has to.
    //
    // THE GRAMMAR IS #1063's AND #1074's, to the comma: closed sentences in one rigid clerical form, run
    // together into one pulse string the client concatenates its pickup sentence onto. Nothing is composed
    // here but a DATE STAMP, which is a number.

    /// <summary>#1149 · The entry every inspection tag carries, twice, a year apart. Authored (canon,
    /// 2026-09-06), verbatim, and nothing may ever be appended to it: the missing name is the evidence, and
    /// the clause that reports it is the clerk's own house style rather than an observation.</summary>
    public const string InspectionTagEntry =
        "Refuge inspected. Rack full, seals within tolerance. No signature — none required.";

    /// <summary>#1149 · …and the third entry, on the one refuge in the game whose seal went. Authored,
    /// verbatim. It is the beat's second half and it is delivered by what it does NOT have: no date, in a
    /// book that has never once failed to date a line, and no tolerance clause. Somebody replaced a seal on
    /// this room and did not write down when.
    ///
    /// <para><i>Rack full</i> is the load-bearing repetition. The rack was full at the inspection and it is
    /// full now — nobody ever drew on it — which is the card's own first sentence arriving a second time,
    /// off a piece of paper, years earlier, in a clerk's hand.</para></summary>
    public const string InspectionTagSealReplaced = "Refuge inspected. Rack full. Seal replaced.";

    /// <summary>#1149 · <b>THE ROOM INDEX THE TAG ANSWERS ON.</b> Negative, and that is the whole trick: a
    /// floor's rooms are indexed from zero, so this is an index the generator can never produce, and the
    /// refuge — which is not one of the floor's rooms at all, <see cref="CarveRefuges"/> having taken it out
    /// of the list — gets a find id of its own without colliding with anything.
    ///
    /// <para>It buys the tag the whole existing paper seam for nothing: <see cref="FindId"/> mints it,
    /// <c>RoomOfFind</c> parses it back (it has always read a leading sign), <see cref="AuthoredPaperOf"/>
    /// round-trips it, and the sleeve's row, the free glance, the write-up and the threads all name it off
    /// <see cref="PaperHeads"/> exactly as they name the other five.</para></summary>
    public const int RefugeTagRoom = -1;

    /// <summary>#1149 · <b>THE SITE'S OWN CLOCK</b> — the year the first of the two inspections was stamped.
    ///
    /// <para>Seeded per body, in the era the game already keeps: <c>ShipHistory</c> lays hulls down between
    /// 2270 and 2319 against a present of roughly 2341, so a building inspected in that window was inspected
    /// decades before the captain walked into it — which is the sentence every other surface down here is
    /// already telling ("the building has been shut for decades"), arriving for once as a number instead of
    /// a phrase.</para>
    ///
    /// <para>No calendar is invented. The building's own clerical papers deliberately number instructions
    /// rather than date them (#1063, #1074), and this does not change that: a stamp on a tag is a number on
    /// a form, which is the only kind of date a form has.</para></summary>
    public static int InspectionYearOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return 2270 + (int)(DiceRule.Seed($"hive:inspection-year:{bodyId}") % 50);
    }

    /// <summary>#1149 · …and which month of it, so the two entries are a year apart to the stamp rather than
    /// merely to the year.</summary>
    public static int InspectionMonthOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return 1 + (int)(DiceRule.Seed($"hive:inspection-month:{bodyId}") % 12);
    }

    /// <summary>#1149 · One dated entry, as the tag stamps it: the date, the building's own separator, the
    /// entry. The separator is the middle dot every plate and every clerical head in this building already
    /// uses, so the tag reads as a form from the same office as everything else on the floor.</summary>
    public static string InspectionTagStamp(int year, int month) =>
        $"{year}-{month:D2} · ";

    /// <summary>#1149 · <b>WHAT IS ON THE INSPECTION TAG IN THIS REFUGE</b>, behind the paper glyph every
    /// operational-paper line in this building already wears.
    ///
    /// <para>Two entries on every tag in the game, the same entry twice, dated a year apart off
    /// <see cref="InspectionYearOf"/> — which is the whole characterisation: a regulation being satisfied,
    /// on schedule, by an organisation that will not sign its own name to anything. On the one refuge whose
    /// seal went there is a third, <see cref="InspectionTagSealReplaced"/>, and it carries no date.</para>
    ///
    /// <para><b>The paper is on EVERY refuge and that is the point.</b> A tag that appeared only on the
    /// interesting room would be the game pointing at the interesting room. A captain reads two or three of
    /// these before they ever meet the failed one, learns the form by heart, and then finds a form with one
    /// line too many in it.</para></summary>
    public static string InspectionTagLine(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int year = InspectionYearOf(bodyId), month = InspectionMonthOf(bodyId);
        string tag = PaperGlyph
            + InspectionTagStamp(year, month) + InspectionTagEntry + " "
            + InspectionTagStamp(year + 1, month) + InspectionTagEntry;
        return StateOfTheRefugeOn(bodyId, level) == RefugeState.Failed
            ? tag + " " + InspectionTagSealReplaced
            : tag;
    }

    // ── #1149 · THE CARD, AND THE ONE ROOM THAT RAISES IT ────────────────────────────────────────────────

    /// <summary>#1149 · Is the refuge on this floor the one the site's story happened in? The one question
    /// the client asks before raising <see cref="StoryBeats.Beat.RefugeFailed"/>, so the card, the plate,
    /// the fan and the suit are all reading one answer.</summary>
    public static bool RefugeThatFailedIsOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return FailedRefugeFloorOf(bodyId) == level;
    }
}
