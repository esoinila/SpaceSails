using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · WHAT IS LEFT AND WHO SAW — what the bin holds afterwards, the watcher, and every line of prose
/// for the sweeps.
///
/// <para>Split out of <c>RipAndBin.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no field.</para>
/// </summary>
public static partial class RipAndBin
{
    /// <summary>
    /// #798 item 2 · …AND WHAT WHOEVER EMPTIES IT MAKES OF WHAT IS THERE.
    ///
    /// <para>Owner: <i>"bin the innocent bulk, which is ALSO cover — a file in the bin that reads boring
    /// explains itself; a missing file explains nothing."</i> <see cref="LeavesSomethingToFind"/> answers
    /// whether there is anything in the drawer at all, which is a fact about the RUNG. This answers what the
    /// thing in the drawer says about the person who put it there, which is a fact about WHAT WENT IN — and
    /// the two were one predicate until a captain could put half a file in a bin.</para>
    ///
    /// <para><b>Ordinal order IS the ranking, worst first</b>, exactly as <see cref="Tier"/>'s is and for the
    /// same reason: a caller comparing two disposals compares two enum values rather than consulting a table
    /// somebody has to remember to keep in step. Nothing is stored under these ordinals.</para>
    ///
    /// <para>Nothing here tells a captain they got away with it — that is still the ladder's discipline and
    /// #649's. A boring folder is a better bet than a torn-up dossier and the game never once says it was
    /// enough.</para>
    /// </summary>
    public enum WhatIsLeft
    {
        /// <summary>A file somebody went to the trouble of getting rid of. Torn up, in a bin, in a building
        /// where professionals empty the bins — and the tearing is itself the thing that says it mattered.
        /// The worst answer, and the one binning a whole document has always given.</summary>
        AFileSomebodyGotRidOf,

        /// <summary>A file about nothing much. It leaves something to find, and what there is to find reads
        /// as nothing: the pages nobody would keep, thrown out by somebody who had no reason to keep
        /// them.</summary>
        NothingMuch,

        /// <summary>Nothing at all — the secure rung, and the only one that is not a bet.</summary>
        Nothing,
    }

    /// <summary>What is left in the bin for whoever empties it, given the rung and whether what went in was
    /// the bulk of a split file. One function, so the act, the filed note and any later arc that decides
    /// whether something comes back all read ONE answer.</summary>
    public static WhatIsLeft LeftInTheBin(Tier tier, bool bulkOfASplitFile) =>
        !LeavesSomethingToFind(tier) ? WhatIsLeft.Nothing
        : bulkOfASplitFile ? WhatIsLeft.NothingMuch
        : WhatIsLeft.AFileSomebodyGotRidOf;

    /// <summary>Who was looking. The client decides which of these is true — what "watched" means at a
    /// counter, at a table and on a corridor are three questions about rooms, and this file does not have
    /// one.</summary>
    public enum Watcher
    {
        /// <summary>The bar desk. #781: the keep is security, and everything on that counter is read by him,
        /// by whoever is waiting to be served and by the man on the next stool.</summary>
        TheKeep,

        /// <summary>Somebody in the chair opposite — #784's own drama beat, one verb over.</summary>
        TheChairOpposite,

        /// <summary>An occupied top close enough to read a hand.</summary>
        TheNextTable,

        /// <summary>#804 · A round, with its eyes on you.</summary>
        TheRota,
    }

    /// <summary>
    /// WHO SAW IT — one ladder, asked top-down, stopping at the first true rung.
    ///
    /// <para>The four flags are the client's own facts about a running world; the ORDER is this file's, and
    /// it lives here for the reason every other law in this project lives in Core: a ladder spelled out at a
    /// call site is a ladder nobody can test both directions of, and this one has to be provable to answer
    /// <b>null</b> as often as it answers a name.</para>
    ///
    /// <para>The rota is first because a man whose job is looking at people is a different fact from a
    /// stranger who happened to glance up. It files ONE line and not a census: this cut records that it was
    /// seen, and nothing reacts.</para>
    /// </summary>
    /// <param name="rotaEyesOn">#804 · A guard on a round has registered the captain.</param>
    /// <param name="atTheCounter">The captain is on a bar stool — where the keep is security (#781) and
    /// everyone waiting to be served is behind you.</param>
    /// <param name="companyAtTheTable">Somebody is in the chair opposite.</param>
    /// <param name="overlooked">Somebody at a nearby seat has a clear line to the captain's hands.</param>
    public static Watcher? WhoSaw(
        bool rotaEyesOn, bool atTheCounter, bool companyAtTheTable, bool overlooked)
    {
        if (rotaEyesOn)
        {
            return Watcher.TheRota;
        }
        if (atTheCounter)
        {
            return Watcher.TheKeep;
        }
        if (companyAtTheTable)
        {
            return Watcher.TheChairOpposite;
        }
        return overlooked ? Watcher.TheNextTable : null;
    }

    /// <summary>Who it was, in a clause a sentence can be built round.</summary>
    public static string WatcherClause(Watcher who) => who switch
    {
        Watcher.TheKeep => "the keep behind the counter",
        Watcher.TheChairOpposite => "a face in the chair opposite",
        Watcher.TheNextTable => "people at the next table",
        _ => "a man on the security rota",
    };

    /// <summary>What is SAID when it was done in front of somebody. It never says what follows, because
    /// nothing follows yet — this cut files the fact and stops, which is #715's per-entity memory arriving
    /// as a line in a book rather than as a meter.</summary>
    public static string SeenLine(Watcher who) =>
        $"{SeenGlyph} You do it with {WatcherClause(who)} in plain view. Nobody says anything, and that is "
        + "not the same as nobody noticing.";

    /// <summary>…and what the book keeps of having been seen doing it. The captain's own note, in the
    /// captain's own flat register.</summary>
    public static string SeenNote(Watcher who) =>
        $"Tore a document up and binned it with {WatcherClause(who)} watching. Nothing was said about it.";

    /// <summary>Every authored sentence this feature owns, for the canon sweep. It walks the ladder and the
    /// witnesses rather than listing lines somebody has to remember to add — #709's discipline, and the
    /// reason a catalog is a method rather than a list that goes stale.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Label;
        yield return NoBinLine;
        yield return NotEvidenceLine;
        yield return NothingToBinLine;
        yield return NotYetWorkedFlag;
        yield return NotYetWorkedWarning;
        yield return AlreadyInTheBookFlag;
        foreach (Tier tier in Ladder)
        {
            yield return TheBin(tier);
            yield return PlateFor(tier);
            yield return TierBet(tier);
            yield return Hint(tier);
            yield return PickerTitle(tier);
            yield return KeyPrompt(tier);
            yield return RippedLine("the manifest", tier);
            yield return DisposalNote("the manifest", tier);
            // #798 item 2 · …and the same note for a split file's bulk, which is a fifth sentence this
            // ladder can print and would otherwise be a line the canon sweep never sees.
            yield return DisposalNote("the manifest", tier, boring: true);
        }
        foreach (Watcher who in (Watcher[])Enum.GetValues(typeof(Watcher)))
        {
            yield return WatcherClause(who);
            yield return SeenLine(who);
            yield return SeenNote(who);
        }
    }
}
