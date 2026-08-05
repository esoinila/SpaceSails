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

        new("ROTA — WEEK 31 (CORRECTED)",
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
    ];

    /// <summary>How many notices are authored.</summary>
    public static int CatalogSize => Catalog.Length;

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
        return Pinned(bodyId, level, amenity).Count == 0
            ? null
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
        for (int i = 0; i < Math.Min(PinnedAtOnce, Catalog.Length); i++)
        {
            int wanted =
                DiceRule.Roll(DiceRule.Seed($"hive:board:{bodyId}:{i}"), Catalog.Length).Face - 1;

            // Take the rolled notice, or the next free one after it. A re-roll loop on a seeded die is how a
            // generator stops being deterministic, and the same notice pinned twice is a board nobody wrote.
            for (int step = 0; step < Catalog.Length; step++)
            {
                int candidate = (wanted + step) % Catalog.Length;
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
