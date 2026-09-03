using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #709 · THE BULLETIN BOARD IN THE BAR — and the person whose notice it is, sitting ten feet away.
///
/// <para>Owner, 2026-08-05: <i>"cool ... let's add a bulletin board to the bar"</i> and then the half that
/// makes it a mechanic rather than a prop: <i>"maybe spot the person notifying in the bar."</i></para>
///
/// <h3>A cross-reference the game never draws for you</h3>
///
/// <para>Every notice here is pinned by somebody who is <b>in the room</b> — one of
/// <see cref="CanteenRegulars"/>' own cast, at one of the tables, saying their own line. The pump written up
/// four times and still listed open is the fitter's pump. The signatory away with no date given is why the
/// carrier has sat here three days. The rota with two names in one slot is the temp whose name was changed at
/// the door.</para>
///
/// <para><b>No notice names its author and nobody ever says "that is mine."</b> The pairing exists so the
/// room is internally true, and the player either notices or does not. Same register as the odd books (#701),
/// the funding trail (#601) and the whole of §13.8: the building is consistent, and consistency is the only
/// thing offered. Being told would destroy it — so the pairing is enforced by a test and by nothing else,
/// and <see cref="Notice.Pairs"/> is never rendered, spoken or filed.</para>
///
/// <h3>Why a board earns its place</h3>
///
/// <para>It turns <i>"they're always hiring — nobody ever says what for"</i> into an object a captain can walk
/// up to and read. The job-seeker cover (#618) stops being something the fiction asserts and becomes a notice
/// on a wall with a desk to apply at. And it is the cheapest information surface in the game: no schedule, no
/// dialogue tree, no economy. Paper on cork.</para>
///
/// <para>The notices are also the natural home for generated art (owner: <i>"we can use gen AI advertisements
/// of jobs and missing things"</i>), and the text is authored so that it <b>stands alone if no picture ever
/// arrives</b> — a board that is blank without art is a board that breaks the day a manifest drifts.</para>
///
/// <para>Pure and world-blind: the client asks what is pinned up, and never keeps an opinion of its own.</para>
/// </summary>
public static class CanteenBoard
{
    /// <summary>The glyph the board's plate and every filed notice carry.</summary>
    public const string Glyph = "\U0001F4CC";

    /// <summary>What is stencilled over the cork. Named for what it looks like bolted to a wall, never for
    /// what it does — the same rule #606 set for the service panel.</summary>
    public const string Plate = "\U0001F4CC THE BOARD";

    /// <summary>How far to the room's left the board hangs, and how far up. Clear of the counter along the
    /// back (<c>cy + 3.6</c>) and clear of every table at the front, so it owns its own patch of floor and
    /// crowds nothing — <c>ConsoleCrowdingTests</c> wants 2 du between consoles and this leaves five.</summary>
    public const double OffsetX = -5.2;

    /// <summary>How far up the wall from the room's centre. See <see cref="OffsetX"/>.</summary>
    public const double OffsetY = 0.8;

    /// <summary>How many notices are pinned at once. Enough that the board rewards a second look, few enough
    /// that reading it is not a chore with a tank running.</summary>
    public const int PinnedAtOnce = 4;

    /// <summary>#1074 · THE ROSTER'S OWN HEADING, named because two things have to agree about it now: the
    /// notice in the catalogue below, and the one place that has to be able to FIND that notice
    /// (<see cref="RosterNotice"/>). Two copies of a heading is the mirrored constant this ground keeps a
    /// table of, and the day somebody re-words the rota the pin would silently go up over a lost toolbag.
    ///
    /// <para><b>The rota is not new and its words are not touched.</b> It has been on this board since #709,
    /// pinned by an agency temp whose name was changed at the door. What #1074 adds is a reason it is
    /// CERTAINLY up on one kind of ground — see <see cref="Pinned"/>.</para></summary>
    public const string RosterHead = "ROTA — WEEK 31 (CORRECTED)";

    /// <summary>One authored notice.</summary>
    /// <param name="Head">Its heading, as it reads across the room.</param>
    /// <param name="Body">What it says when a captain stands and reads it.</param>
    /// <param name="Pairs">The plate of the regular whose notice this is — <b>internal only.</b> Never
    /// rendered, never spoken, never filed. It exists so the room is consistent and so a guard can prove
    /// every notice belongs to somebody who could actually have pinned it.</param>
    public readonly record struct Notice(string Head, string Body, string Pairs);

    /// <summary>
    /// The authored board. Every entry pairs with exactly one member of <see cref="CanteenRegulars"/>' cast.
    ///
    /// <para><b>The register test</b>, inherited from #701 and from the cast next door: every notice is dull.
    /// Nothing here is a quest, a lead or a secret — maintenance backlogs, held deliveries, a lost toolbag and
    /// a withdrawn stew. The paperwork of people doing a job in a building that pays them not to ask about it.
    /// A notice that was interesting would make the board a quest hub and the room a corridor with clues in
    /// it.</para>
    /// </summary>
    private static readonly Notice[] Catalog =
    [
        new("PUMP 2 — MAINTENANCE",
            "Written up 12/4, 19/4, 2/5, 16/5. Still listed OPEN. Please do not report it again; it is on " +
            "the list.",
            "◈ A FITTER, OFF A MAINTENANCE CONTRACT"),

        new("COUNTERSIGNATURES — HELD",
            "The signatory is away. Deliveries requiring countersignature are held until return. No date " +
            "has been given to us either.",
            "◈ A CARRIER, WAITING ON A SIGNATURE"),

        new(RosterHead,
            "Nights, slot four: disregard the first name and use the second. Agency staff are listed under " +
            "the name we were given, not the one you use.",
            "◈ AN AGENCY TEMP, FIRST WEEK"),

        new("LOST — TOOLBAG",
            "Blue canvas, initials worn off, one handle gone. Somewhere between the lift and number two. " +
            "Reward offered and meant.",
            "◈ A HAND WHO HAS BEEN HERE LONGER THAN THE CONTRACT SAID"),

        new("MACHINES — NOTICE",
            "The left-hand machine takes cards and returns them without vending. Use the right-hand machine. " +
            "This has been reported.",
            "◈ A MAN AT THE MACHINES, HAVING NO LUCK"),

        new("HIRING — GENERAL HANDS",
            "No experience necessary. Weekly pay, cleared weekly. Apply at the desk. Bring nothing.",
            "◈ A CONTRACTOR NOBODY HAS COME FOR"),

        new("STORES — SUBSTITUTIONS",
            "No soil samplers in stock. A substitute has been issued against the same line. Do not query the " +
            "description on your docket.",
            "◈ A WOMAN DOING INVOICES AT A TABLE"),

        new("CATERING — WITHDRAWN",
            "The stew is withdrawn pending review. Everything else is available as normal.",
            "◈ SOMEBODY EATING, SHIFT ENDED"),

        new("THE 0600 CAR",
            "The first car up is for nights coming off. If you are going down, wait for the second.",
            "◈ A QUIET ONE, FACING THE DOOR"),

        new("HAULAGE — NO ENQUIRIES",
            "Drivers are not to be asked what they are carrying and are not to answer. This is for their " +
            "benefit as much as anyone else's.",
            "◈ A DRIVER, NOT SAYING WHO FOR"),

        // ── #1063 · AND THE ONE THAT IS NOT ALWAYS THERE ─────────────────────────────────────────────────
        //
        // THE WORKS NOTICE. It is LAST in this array and it is dealt out of a pool that stops one short of
        // it, so on every ground nobody has been past a seam of — which is every ground in almost every
        // world — this array is the ten notices it has always been, dealt by the same dice against the same
        // length, and no board in the game changes by one character.
        //
        // It goes up when the works go on (Burial.NoticeIsUp) and comes down when the job is done, which is
        // what happens to a notice about a job. Its heading is the board's own form and its body is #1063's
        // authored sentence, verbatim: the dullest possible piece of paper, about upper walks.
        new(Burial.NoticeHead, Burial.NoticeLine, Burial.MasonPlate),
    ];

    /// <summary>How many notices are authored.</summary>
    public static int CatalogSize => Catalog.Length;

    /// <summary>#1063 · Which entry is the works notice — the last, and the one the ordinary deal below stops
    /// short of. Named rather than written as <c>Length - 1</c> at the two places that need it, because two
    /// copies of "which one is special" is the mirrored constant this ground keeps a table of.</summary>
    private static int WorksNotice => Catalog.Length - 1;

    /// <summary>#1063 · …and how many notices the ordinary seeded deal may choose from. Everything but the
    /// works notice, which is the whole reason a board that has never had works on it is byte-identical to
    /// the board it was before this shipped.</summary>
    private static int OrdinaryNotices => Catalog.Length - 1;

    /// <summary>#1063/#1074 · How many notices the ordinary seeded deal may choose from, published so a guard
    /// can pin it without reaching into the array. <b>This number must never change</b>: it is the length the
    /// dice are rolled against, and moving it re-pins every board in every world.</summary>
    public static int OrdinaryNoticeSize => OrdinaryNotices;

    /// <summary>#1074 · Which entry is the ROSTER — found by its own heading rather than written down as an
    /// index, for <see cref="RosterHead"/>'s reason and for <c>HeadOfficeLevelOf</c>'s: a beat pointed at a
    /// row that does not exist should fail loudly at the first call rather than go quietly missing on some
    /// worlds forever. Unlike the works notice it is an ORDINARY notice and stays in the ordinary pool: on
    /// every board in the game it is dealt or not dealt exactly as it always was.</summary>
    private static int RosterNotice
    {
        get
        {
            for (int i = 0; i < Catalog.Length; i++)
            {
                if (string.Equals(Catalog[i].Head, RosterHead, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            throw new InvalidOperationException("the board has no roster notice on it");
        }
    }

    /// <summary>Every authored heading and body, for the canon grep. The pairing is deliberately absent: it is
    /// never shown to anybody, and grepping it would only be grepping the cast's plates twice.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (Notice n in Catalog)
        {
            yield return n.Head;
            yield return n.Body;
        }
    }

    /// <summary>Which regular each notice belongs to — <b>for the consistency guard only.</b> Nothing in the
    /// client may read this: the whole point is that the connection is the player's to make or to miss.</summary>
    public static IEnumerable<string> AllPairings() => Catalog.Select(n => n.Pairs);

    /// <summary>Where the board hangs in this room, if this room has one.</summary>
    public static (double X, double Y)? At(string bodyId, int level, UndergroundComplex.Amenity amenity)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (Pinned(bodyId, level, amenity).Count == 0)
        {
            return null;
        }

        // #751 · A HALL PUBLISHES ITS OWN SPOT. The offsets below are measured off a 15 x 12 room's counter
        // and tables; in a room fifty du deep, whose door is on whichever face the rib happens to be on,
        // they would hang the rota in the middle of the floor or inside the bar. The hall knows where its
        // door is — it is the only thing that does — so it says, and this reads.
        return amenity.Hall is { } hall
            ? (hall.BoardX, hall.BoardY)
            : (amenity.X + OffsetX, amenity.Y + OffsetY);
    }

    /// <summary>
    /// What is pinned up in this room, if this room has a board.
    ///
    /// <para>Only the upper canteen on the site's top pressurised floor — the same law as the people, for the
    /// same reason (the owner's B1 ruling), and living here where a test can reach it.</para>
    /// </summary>
    public static IReadOnlyList<Notice> Pinned(
        string bodyId, int level, UndergroundComplex.Amenity amenity)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (amenity.Use != UndergroundComplex.Comfort.UpperCanteen)
        {
            return [];
        }
        if (UndergroundComplex.TopPressurisedFloor(bodyId) != level)
        {
            return [];
        }

        // Seeded off the site, so a moon's board says what it says on every visit — the same discipline as
        // everything else down here. A board that reshuffled between excursions would make re-reading it a
        // slot machine, and would break the one thing it is for: matching a notice to a face in the room.
        var up = new List<Notice>(PinnedAtOnce);
        var used = new List<int>(PinnedAtOnce);

        // #1063 · THE WORKS NOTICE GOES UP FIRST, and only while the works are on. It takes one of the four
        // slots rather than making a fifth: a board that grew a row would say, in its own shape, that
        // something new had happened here — and the whole beat is that nothing did. The three below it are
        // dealt by the same dice, against the same length, in the same order they always were.
        // ── #1074 · THE ROSTERED CREW WHO NEVER DUG ──────────────────────────────────────────────────────
        //
        // The canon pass authored NO LINE for this beat, and that is the beat: "the roster on the intranet
        // still lists the shift, and the working is closed; THE GAP IS THE SENTENCE." So nothing is written
        // here and nothing new is pinned. What happens on a stopped ground is that the rota the board has
        // carried since #709 is CERTAINLY up — a shift listed for a working nobody can get to any more, on
        // the same cork as a lost toolbag and a withdrawn stew, in an ordinary week's ordinary handwriting.
        //
        // It takes one of the four slots and never a fifth (#1063's rule, for its reason: a board that grew a
        // row would say in its own shape that something new had happened here), and it goes into `used` so
        // the deal below cannot pin it twice. On every ground nobody has stopped — which is every ground in
        // almost every world — the rota is dealt or not dealt exactly as it always was.
        //
        // ── #1074 beat 4 · …AND THE REGISTER ROW UNDER IT ────────────────────────────────────────────────
        //
        // The rota lists the shift; this names one hand off it and says where they went, in a personnel
        // system's own four words. It is pinned on a stopped ground only, beside the rota it belongs to, and
        // it takes a third of the four slots rather than a fifth — the same rule, for the same reason.
        //
        // IT IS NOT IN THE CATALOGUE AT ALL. It carries a NAME, dealt off the ground (CareerCost.HandOn), so
        // it is not a constant the array could hold; and keeping it out means the ordinary deal below runs
        // against exactly the length it always ran against, with no index anybody could reach it by. The
        // board a captain reads on a ground nobody has stopped is the board it was before this shipped, to
        // the byte.
        bool closed = StopOrder.On(bodyId);
        if (closed)
        {
            up.Add(Catalog[RosterNotice]);
            used.Add(RosterNotice);
            up.Add(CareerCost.RegisterRow(bodyId));
        }

        bool works = Burial.NoticeIsUp(bodyId);
        if (works)
        {
            up.Add(Catalog[WorksNotice]);
        }

        for (int i = 0; up.Count < Math.Min(PinnedAtOnce, OrdinaryNotices); i++)
        {
            int wanted =
                DiceRule.Roll(DiceRule.Seed($"hive:board:{bodyId}:{i}"), OrdinaryNotices).Face - 1;

            // Take the rolled notice, or the next free one after it. A re-roll loop on a seeded die is how a
            // generator stops being deterministic, and the same notice pinned twice is a board nobody wrote.
            for (int step = 0; step < OrdinaryNotices; step++)
            {
                int candidate = (wanted + step) % OrdinaryNotices;
                if (!used.Contains(candidate))
                {
                    used.Add(candidate);
                    up.Add(Catalog[candidate]);
                    break;
                }
            }
        }

        return up;
    }
}
