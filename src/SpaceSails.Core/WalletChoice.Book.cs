using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · WHAT THE BOOK REMEMBERS — the shown record, its memory, the history line, and the tags and
/// note it is filed under.
///
/// <para>Split out of <c>WalletChoice.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Its two fields are <c>const</c>s.</para>
/// </summary>
public static partial class WalletChoice
{
    // ── WHAT THE BOOK REMEMBERS ───────────────────────────────────────────────────────────────────────
    //
    // #836's other half, and the owner's own words for it: "The round log remembers the NAME ... every
    // challenge writes down which identity you showed. Two names on one face across one watch is the
    // facility's own case against you assembling itself in its own ledger."
    //
    // This is the CAPTAIN's half of that ledger — the book in their own pocket. It is filed at every read,
    // both arms, and it is the only thing a chooser row's hint is ever derived from.

    /// <summary>One line of the captain's own paper trail: which paper, where, on what floor, and how it
    /// went. Flat and trivially round-tripped, the shape every other durable list in this game uses.</summary>
    /// <param name="PaperId">The paper's satchel id — the FACT, so the prose is rebuilt at read time.</param>
    /// <param name="BodyId">The site it was shown at.</param>
    /// <param name="Level">The floor it was shown on (negative; −2 is B2).</param>
    /// <param name="How">What the man made of it.</param>
    public readonly record struct Shown(string PaperId, string BodyId, int Level, Outcome How)
    {
        /// <summary>Written down as one field. The id can contain colons (<c>badge:luna</c>), so it goes
        /// LAST and the split is bounded — <see cref="Satchel.Item.Stored"/>'s own lesson, paid for once
        /// already.</summary>
        public string Stored => $"{(int)How}:{Level}:{BodyId}:{PaperId}";

        /// <summary>Read one back. Anything this build cannot parse is dropped rather than thrown over: a
        /// line of somebody's field book is not worth losing a game for.</summary>
        public static bool TryParse(string? stored, out Shown row)
        {
            row = default;
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            string[] parts = stored.Split(':', 4);
            if (parts.Length != 4
                || !int.TryParse(parts[0], out int how) || !Enum.IsDefined(typeof(Outcome), how)
                || !int.TryParse(parts[1], out int level)
                || parts[2].Length == 0 || parts[3].Length == 0)
            {
                return false;
            }

            row = new Shown(parts[3], parts[2], level, (Outcome)how);
            return true;
        }
    }

    /// <summary>How many reads the book keeps. A pocket book, not an archive — and comfortably more than one
    /// evening's worth of challenges, so the hint on a row never goes quiet while the paper is still in
    /// use.</summary>
    public const int BookKeeps = 96;

    /// <summary>File one read. Oldest fall off the front, exactly as the field book's own notes do.</summary>
    public static IReadOnlyList<Shown> Remember(IReadOnlyList<Shown>? book, Shown one)
    {
        var kept = new List<Shown>(book ?? []) { one };
        if (kept.Count > BookKeeps)
        {
            kept.RemoveRange(0, kept.Count - BookKeeps);
        }
        return kept;
    }

    /// <summary>What the book says about THIS paper at THIS site, when it has never come up. Not a blank and
    /// not a shrug: the row says the thing that is true, which is that you have not tried it here.</summary>
    public const string NeverShownLine = "never shown";

    /// <summary>
    /// WHAT YOUR BOOK SAYS ABOUT THIS PAPER, HERE. Derived from the filed rows and from nothing else.
    ///
    /// <para>A refusal outranks a success, because a refusal is the sharper knowledge and because the most
    /// recent one is the one with a floor on it — the owner's own example, <i>refused on B2 · wrong site
    /// code</i>. Where a paper has both histories the row carries both, in the order a person would say
    /// them.</para>
    ///
    /// <para>Only rows filed at THIS site count. A pass that was refused on another moon says nothing about
    /// this building, and pretending otherwise would be the hint knowing something the captain does not.</para>
    /// </summary>
    public static string HistoryLine(Satchel.Item paper, string bodyId, IReadOnlyList<Shown>? book)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        int worked = 0;
        Shown? refused = null;
        foreach (Shown row in book ?? [])
        {
            if (!string.Equals(row.BodyId, bodyId, StringComparison.Ordinal)
                || !string.Equals(row.PaperId, paper.Id, StringComparison.Ordinal))
            {
                continue;
            }

            // #1149 · An inspection that was honoured is a read that WORKED — he read it and walked on — and
            // one that was not is a refusal. Two rungs added to the ladder, and the hint counts them on the
            // side they actually landed on rather than falling through to "never shown", which would be the
            // captain's own book quietly forgetting an evening.
            //
            // #605 · And the department rung counts as the refusal it is — through CoverBlew rather than
            // through a third list of enum members, so a rung added tomorrow lands on the side it actually
            // landed on instead of falling through to "never shown", which would be the captain's own book
            // quietly forgetting an evening. The one arm that is neither is NothingShown: an empty hand is
            // not a thing this PAPER did, and filing it against a row would be the hint blaming a pass for
            // a pocket.
            if (!CoverBlew(row.How))
            {
                worked++;
            }
            else if (row.How != Outcome.NothingShown)
            {
                refused = row;   // the LATEST one — the floor a captain would actually name.
            }
        }

        string workedLine = worked switch
        {
            0 => "",
            1 => "worked here, once",
            2 => "worked here, twice",
            _ => $"worked here, {worked} times",
        };

        if (refused is not { } bad)
        {
            return workedLine.Length > 0 ? workedLine : NeverShownLine;
        }

        string refusedLine = $"refused on {FloorTag(bodyId, bad.Level)} · {ReasonTag(bad.How)}";
        return workedLine.Length > 0 ? $"{workedLine}, {refusedLine}" : refusedLine;
    }

    /// <summary>Which floor, said the way the wall says it. The building's own plate
    /// (<see cref="UndergroundComplex.NameOf"/>), cut at its separator so the row carries <c>B2</c> and not
    /// the whole stencil — read rather than re-derived, so a building that renames its floors renames
    /// them here too.</summary>
    public static string FloorTag(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        string plate = UndergroundComplex.NameOf(bodyId, level);
        int cut = plate.IndexOf(" · ", StringComparison.Ordinal);
        return cut > 0 ? plate[..cut] : plate;
    }

    /// <summary>Why it was refused, in the fewest words that are still true. It is the captain's shorthand
    /// for what the man said, never a new fact.</summary>
    public static string ReasonTag(Outcome how) => how switch
    {
        Outcome.WrongSite => "wrong site code",
        Outcome.WrongPaper => "wrong paper for this floor",
        Outcome.Worked => "read and handed back",

        // #605 · The department rung's shorthand, in the same clerical register as its two siblings: what
        // was wrong with the paper HERE, in the fewest words that are still true. It is also the sentence a
        // captain says afterwards — "it was the tier" — which is the whole of the owner's second property.
        Outcome.WrongDepartment => "wrong tier for this floor",

        // #1149 · The shorthand for the two inspection rungs is the card's own PLATE, composed rather than
        // authored: what a row has to be able to say is WHICH paper it is about, and the plate is the only
        // thing printed on that one. No new sentence is invented for a book that is a list of shorthands.
        Outcome.Inspection or Outcome.NoInspectionDue => Inspectorate.Plate,

        _ => "nothing to show",
    };

    /// <summary>
    /// THE NOTE THE BOOK KEEPS OF A READ — the escort note's own idiom, one system along: it records what
    /// happened and never the mechanic, and it NAMES THE PAPER, which is the whole of #836's second half.
    ///
    /// <para>It is filed on BOTH arms now. The satisfied read used to file nothing at all — <i>a notebook
    /// full of manners is a notebook nobody reads</i> — and that was right while the only thing a clean read
    /// produced was politeness. It produces a NAME now: the paper that worked here is the paper you will
    /// reach for next time, and the row that tells you so is only ever built out of these lines.</para>
    /// </summary>
    /// <param name="paper">What was handed over, or null when nothing was — an empty hand is a thing that
    /// happened to you, and the book keeps it without a name on it.</param>
    public static string ShownNote(
        Satchel.Item? paper, string bodyId, int level, Outcome how, string captainName)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(captainName);

        string face = paper is { } held ? $"{NameOn(held, captainName)} · {Claims(held)}" : "";
        string where = FloorTag(bodyId, level);

        return how switch
        {
            Outcome.Worked =>
                $"Showed {face} to a man on the security rota on {where}. He read it, handed it back, and " +
                "walked on. That name is now a name that has been in this building.",
            Outcome.WrongSite =>
                $"Showed {face} to a man on the security rota on {where}. He read the site code off it and " +
                "it was not this site. He wrote it down anyway.",
            Outcome.WrongPaper =>
                $"Showed {face} to a man on the security rota on {where}. Real paper, and for somewhere " +
                "else entirely. He wrote it down anyway.",

            // #605 · The department rung. It names the FLOOR'S plate and the pass's tier and draws no
            // conclusion between them — the book keeps no opinion (#741's law) — which is what makes it the
            // line a captain reads back and says "it was the tier" off. The plate is quoted off the
            // building's own signage rather than re-spelled here, so a floor that renames itself renames
            // itself in the book too.
            Outcome.WrongDepartment =>
                $"Showed {face} to a man on the security rota on {where}. This site's own pass, and this " +
                $"floor is {UndergroundComplex.DepartmentOf(bodyId, level)}. He read it twice and wrote it " +
                "down.",

            // #1149 · THE BOOK QUOTES HIM, and that is the whole of what it can honestly keep about an
            // inspection: the two rungs are indistinguishable at the moment they happen — he says the same
            // sentence to a man he is about to wave past and to a man he is about to walk out — and a note
            // that told the captain which one it had been would be the book knowing the roster. The opener
            // is this file's own clerical form, unchanged; the sentence inside the quotes is canon.
            Outcome.Inspection or Outcome.NoInspectionDue =>
                $"Showed {face} to a man on the security rota on {where}. \"{Inspectorate.HonouredLine}\"",

            _ =>
                $"Nothing came out of the wallet for a man on the security rota on {where}, and he waited " +
                "the whole time you were looking.",
        };
    }
}
