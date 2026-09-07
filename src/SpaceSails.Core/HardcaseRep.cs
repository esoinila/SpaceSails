using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1061 beat 2 · <b>BREM KOLT, HAZARDOUS ACCOUNTS</b> — the man the company sends where Harlan Fess will
/// not go.
///
/// <para>Owner, 2026-09-01: <i>"Maybe some hardcode salesman might be on moon also and runs away from
/// reevers in despair :-D"</i></para>
///
/// <h3>What he is, and what he deliberately is not</h3>
///
/// <para>He is NOT a second salesman system. Everything a salesman does in this game already exists in
/// <see cref="NebulaRep"/> — the premiums, the policy a sale leaves behind, the labels on the buttons — and
/// none of it is retyped here. What this file owns is the three things that are HIS: the three authored
/// lines, the rota that says which grounds he works, and the one sheet he drops when he runs.</para>
///
/// <h3>The law of the flight, stated where it can be read</h3>
///
/// <para><b>The running is the sentence.</b> Nothing captions it. No card explains what he is afraid of, no
/// pulse says he has seen something, and there is no line in this file for the flight because there is no
/// line in the game for it. He runs like a man who KNOWS what they are, the company sent him anyway, and the
/// captain is left to work out which of those two facts is the worse one. That silence is the whole beat and
/// <c>TheHardcaseOnTheMoonTests</c> reads this file's source to keep it.</para>
///
/// <h3>And the asymmetry is the scene</h3>
///
/// <para>He flees on the real lattice (<see cref="NpcWalk"/> over <c>AutoWalk</c>'s route). The thing he
/// flees does not: the Old Ones keep their stagger, which is canon and is refused at the door by
/// <see cref="NpcWalk.Plan"/> itself. A man who can plan a route, running from things that cannot, is the
/// only picture this beat needs.</para>
///
/// <para>Pure and deterministic, like everything else in Core.</para>
/// </summary>
public static class HardcaseRep
{
    // ── Who he is ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>His id in the <see cref="ContactLedger"/>. His OWN id and not
    /// <see cref="NebulaRep.ContactId"/>: he is a different man on the same payroll, and a sale signed with
    /// him is goodwill toward HIM — the joke of the arc is that the company remembers the policy and never
    /// the person, in either direction.</summary>
    public const string ContactId = "nebula-rep-kolt";

    /// <summary>How the book names him.</summary>
    public const string DisplayName = "Brem Kolt · Nebula Mutual";

    /// <summary>What he calls himself.</summary>
    public const string RepName = "Brem Kolt";

    /// <summary>The desk he works out of — the second line of his card. Fess has the Outer Reaches desk;
    /// this is the desk that covers the accounts the Outer Reaches desk will not put its name to.</summary>
    public const string Desk = "Nebula Mutual · hazardous accounts";

    /// <summary>What the deck draws over his head — the house's <c>◈</c> plate idiom, the same one Fess and
    /// the haulier wear.</summary>
    public const string Plate = "◈ BREM KOLT · HAZARDOUS ACCOUNTS";

    /// <summary>The letterhead both his card and his dropped sheet answer to — the OFFICE a case thread is
    /// filed under, so the schedule joins #898's stack rather than starting a second one about one
    /// company.</summary>
    public const string Company = "Nebula Mutual";

    // ── His three lines (canon, Fable, 2026-09-01 — verbatim, and there are exactly three) ──────────────

    /// <summary>Line one: the opener, said on the ground, to a face he has never seen before and will not
    /// remember.</summary>
    public const string Opener =
        "Captain. Brem Kolt, hazardous accounts. Your policy travelled better than your face did.";

    /// <summary>Line two: the pitch.</summary>
    public const string Pitch =
        "Out here the premiums write themselves. Sign, and the company remembers you kindly — somewhere it "
        + "matters.";

    /// <summary>Line three: what he says when he is turned down. It doubles as the mechanics' own permission
    /// slip — <see cref="GroundsAtMost"/> is this sentence written as arithmetic.</summary>
    public const string OnRefusal =
        "They all decline on the first moon. The book says you'll sign on the second.";

    // ── Where he works ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>The floor a moon's regolith is, in the excursion's own numbering (<c>0</c> is the surface;
    /// negative is a poured facility under it). Quoted rather than spelled as a literal at the two places
    /// that ask, because a level is the one fact this whole file's geography turns on.</summary>
    public const int SurfaceFloor = 0;

    /// <summary>
    /// <b>IS THIS EVEN GROUND HE COULD BE STANDING ON?</b> Asked before any seed, because no roll can rescue
    /// the wrong kind of place.
    ///
    /// <para>Three answers and they are three different refusals. A DOCKED BERTH is Fess's beat and not his —
    /// the whole of beat 2 is that this one is found on a moon, and a hardcase turning up in a station bar
    /// would simply be Fess with a worse suit. A DERELICT is nobody's beat: an insurance man does not walk a
    /// hull that has been dead for forty years, and the Old Ones aboard one are an authored, finite pack
    /// rather than the tide. And a POURED FACILITY under the regolith is the canteen floor, where the round
    /// (#1061 beat 1) already belongs to somebody else.</para>
    /// </summary>
    /// <param name="landed">Whether the captain is on an excursion at all — false in a berth.</param>
    /// <param name="onAWreck">Whether that excursion is a derelict rather than a moon.</param>
    /// <param name="floor">Which floor of it: <see cref="SurfaceFloor"/> is the regolith.</param>
    public static bool GroundLikeThis(bool landed, bool onAWreck, int floor) =>
        landed && !onAWreck && floor == SurfaceFloor;

    /// <summary>
    /// How many DISTINCT grounds he may ever work in one universe. <b>Two.</b>
    ///
    /// <para>It is not a tunable and it is not a feel call: it is line three read as a rule. <i>"They all
    /// decline on the first moon. The book says you'll sign on the second."</i> — the sentence promises a
    /// second moon and promises nothing after it. Twice is the joke (he never learns your face; the book
    /// travels); three times is a man who lives in your shadow, which is a different and much worse
    /// story.</para></summary>
    public const int GroundsAtMost = 2;

    /// <summary>Of the grounds he COULD work, how many he actually does — one in three, the same shape as
    /// <see cref="NebulaRep.RotaPeriod"/>. A hardcase who was on every moon would be scenery.</summary>
    public const int OneGroundIn = 3;

    /// <summary>How a ground is named in the book he is kept in: the body and the landing site, because
    /// #320 gives one moon two to four grounds and they are genuinely different places to be found on.
    /// The captain who sets down twice on the Wild Plain has been found once.</summary>
    public static string GroundKey(string? bodyId, int siteIndex) =>
        $"{bodyId ?? ""}:{siteIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    /// <summary>
    /// <b>IS HE ON THIS GROUND?</b> Deterministic in the thread, the body, the site and the grounds he has
    /// already been found on — no clock and no <c>Random</c>, so a reloaded save meets the same man in the
    /// same crater.
    ///
    /// <para>Three clauses, in this order, and the order is the design.</para>
    ///
    /// <para><b>A ground he has already worked is a ground he is on.</b> Asked FIRST, so a captain who walks
    /// back up the tube and sets down again finds him where he was rather than rolling him away — and, more
    /// importantly, so a revisit cannot spend one of the two. He is allowed to be on two moons; he is not
    /// allowed to be a new man each time you land on one of them.</para>
    ///
    /// <para><b>The cap is arithmetic and not a flag.</b> Two grounds in the book and the answer is no,
    /// whatever the seed says, for ever.</para>
    ///
    /// <para><b>And only then the roll.</b> One ground in <see cref="OneGroundIn"/>, seeded on the thread and
    /// the ground so that two universes do not find him in the same crater.</para>
    /// </summary>
    /// <param name="threadId">The game thread's id — the per-universe seed.</param>
    /// <param name="bodyId">The body set down on.</param>
    /// <param name="siteIndex">Which of that body's landing sites (#320).</param>
    /// <param name="worked">The grounds he has already been found on, from the vault.</param>
    public static bool WorksThisGround(
        string? threadId, string? bodyId, int siteIndex, IReadOnlyCollection<string>? worked)
    {
        string key = GroundKey(bodyId, siteIndex);
        IReadOnlyCollection<string> book = worked ?? [];

        if (AlreadyWorked(book, key))
        {
            return true;
        }

        if (book.Count >= GroundsAtMost)
        {
            return false;
        }

        return DiceRule.Roll(
            DiceRule.Seed($"hardcase:ground:{threadId ?? ""}|{key}"), OneGroundIn).Face == 1;
    }

    /// <summary>Is this ground already in his book? A plain ordinal scan rather than a set, so the caller may
    /// hand over whatever collection the vault gave it.</summary>
    public static bool AlreadyWorked(IReadOnlyCollection<string>? worked, string key)
    {
        foreach (string one in worked ?? [])
        {
            if (string.Equals(one, key, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The book after he has been found on a ground — at most <see cref="GroundsAtMost"/> entries,
    /// in the order they happened, and never the same ground twice. Written here rather than at the call
    /// site so the cap cannot be enforced by one function and written by another.</summary>
    public static IReadOnlyList<string> WithGroundWorked(IReadOnlyList<string>? worked, string key)
    {
        var book = new List<string>(worked ?? []);
        if (AlreadyWorked(book, key) || book.Count >= GroundsAtMost)
        {
            return book;
        }

        book.Add(key);
        return book;
    }

    // ── The pitch: his line, and NebulaRep's buttons ───────────────────────────────────────────────────

    /// <summary>
    /// The ways out of his card. <b>Not a second offer ladder</b> — the moves, the prices and the words on
    /// the buttons are <see cref="NebulaRep"/>'s, because it is one firm selling one policy and a second set
    /// of prices would be two answers to one question the day #227's vendor lane re-prices them.
    ///
    /// <para>What he does NOT get is Fess's two conversational moves. <i>"I already have a policy"</i> is a
    /// line the captain says to a man holding their FILE, and it opens the signing flashback; Kolt is holding
    /// a rate schedule on a moon and has no file to be reassuring about. <i>"That's not my name"</i> belongs
    /// to the bleed, which is a fact about a desk reading a dead captain's line off a page — his own opener
    /// already says he does not know your face, out loud, and answering it would be the captain explaining
    /// the joke.</para>
    /// </summary>
    public static IReadOnlyList<NebulaRep.RepOffer> OffersFor(InsuranceTier tier)
    {
        List<NebulaRep.RepOffer> offers = [];
        switch (tier)
        {
            case InsuranceTier.Premium:
                offers.Add(new NebulaRep.RepOffer(NebulaRep.RepMove.GoodDay, NebulaRep.GoodDayLabel, 0));
                break;

            case InsuranceTier.Basic:
                offers.Add(new NebulaRep.RepOffer(
                    NebulaRep.RepMove.BuyPremium,
                    $"Premium · {NebulaRep.PremiumPremiumCr} cr", NebulaRep.PremiumPremiumCr));
                offers.Add(new NebulaRep.RepOffer(NebulaRep.RepMove.NotToday, NebulaRep.NotTodayLabel, 0));
                break;

            default:
                offers.Add(new NebulaRep.RepOffer(
                    NebulaRep.RepMove.BuyBasic,
                    $"Put me on the file — Basic · {NebulaRep.BasicPremiumCr} cr", NebulaRep.BasicPremiumCr));
                offers.Add(new NebulaRep.RepOffer(
                    NebulaRep.RepMove.BuyPremium,
                    $"Premium · {NebulaRep.PremiumPremiumCr} cr", NebulaRep.PremiumPremiumCr));
                offers.Add(new NebulaRep.RepOffer(NebulaRep.RepMove.NotToday, NebulaRep.NotTodayLabel, 0));
                break;
        }

        return offers;
    }

    /// <summary>The receipt a sale with HIM leaves in the ship's ledger — the same sentence
    /// <see cref="NebulaRep.SaleLedgerNote"/> writes, said about the man who actually took the money. Engine
    /// voice, not his: a receipt is a number and a date.</summary>
    public static string SaleLedgerNote(InsuranceTier tier, int priceCr, string captainName) =>
        $"🧾 NEBULA MUTUAL — {tier} policy taken out with {RepName}, {priceCr} cr, "
        + $"on the file under Capt. {NebulaRep.BareName(captainName)}.";

    // ── The flight ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// How far he can see one. <see cref="NerveModel.DreadRangeDeckUnits"/> — the range at which Core has
    /// already ruled an Old One stops being scenery and starts being a thing in the room with you — because
    /// the alternative is a second opinion about the same distance, and the first one is the owner's.
    ///
    /// <para>The RANGE is only half the question; the other half is a clear line, and that half is
    /// <see cref="SurfaceCollision.HasLineOfSight"/>, the identical call the Old Ones' own eyes, the tube's
    /// gun and a swinging arm all make. Nothing about sight is invented for him.</para>
    /// </summary>
    public const double SeesOneAtDu = NerveModel.DreadRangeDeckUnits;

    /// <summary>
    /// The despair gait, in deck units a second — <see cref="PatrolBeat.AfterYouSpeed"/>, which is the
    /// fastest anybody in this game moves on foot who is not the captain.
    ///
    /// <para>Quoted rather than chosen. It is already the speed of the one other beat in the game where a
    /// person's walk turns into something else because of what is behind them, and a number of its own here
    /// would be a fourth pace nobody could argue about. Three times a walker's amble
    /// (<see cref="NpcWalk.PaceDu"/>), which is what makes the break legible from across a field: the room
    /// contains a man ambling, and then it contains a man running.</para>
    /// </summary>
    public const double DespairPaceDu = PatrolBeat.AfterYouSpeed;

    // ── The sheet he drops ─────────────────────────────────────────────────────────────────────────────

    /// <summary>The durable id of the one sheet he ever drops. <b>One document and not one per ground</b>:
    /// it is the company's rate book, the same rate book on both moons, and a captain holding two of them
    /// would be holding a bug.
    ///
    /// <para>The id is also load-bearing in a way ids usually are not here. Everything the pocket says about
    /// a <see cref="Satchel.Kind.Paper"/> is seeded off it, and this one seeds
    /// <see cref="FieldClue.Certainty.Vague"/> — <i>"a place mentioned the way you mention a place you have
    /// never had to find"</i> — which is exactly what a schedule of rates by site is. It is pinned by a test
    /// so a rename cannot quietly promote a price list into a position.</para></summary>
    public const string ScheduleFindId = "kolt-premium-schedule";

    /// <summary>Is this find that one? The <c>UndergroundComplex.IsHallRecord</c> idiom, one kind up.</summary>
    public static bool IsTheSchedule(string? findId) =>
        string.Equals(findId, ScheduleFindId, StringComparison.Ordinal);

    /// <summary>What the sheet is called — the plate on the regolith, the title in the sleeve and the head of
    /// its card, all one string, because they are all the same piece of paper.</summary>
    public const string ScheduleLabel = "A dropped premium schedule";

    /// <summary>What is on it. It prices what it never names.</summary>
    public const string ScheduleBody =
        "Rates by site. The Hive moons are priced like weather: certain, seasonal, and nobody's fault.";

    /// <summary>The glyph the book's entry leads with — a sheet of paper, which is what it is.</summary>
    public const string ScheduleGlyph = "📄";

    /// <summary>
    /// What the book's entry is ABOUT: the company that priced the ground, and the ground it priced.
    ///
    /// <para>Two subjects and both of them are already printed for the captain to read — the letterhead is on
    /// his card and the site's name is on the plate the shuttle sets you down under — which is
    /// <see cref="CaseSubjects"/>'s own first law. The OFFICE is what joins this to #898's stack: every other
    /// thing the captain has written down about Nebula Mutual sits under the same heading, and clipping a
    /// rate schedule in beside them is the whole of why the sheet is worth walking over to.</para>
    /// </summary>
    public static string ScheduleSubjects(string? siteName) =>
        CaseSubjects.Line(CaseSubjects.Office(Company), CaseSubjects.Place(siteName ?? ""));
}
