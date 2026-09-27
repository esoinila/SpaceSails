using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #701 · THE LIBRARY LAYER — the per-occupant shelves, and the half of the odd book that was never built.
///
/// <para>Owner's morning expansion, 2026-08-05: <i>"They have their work books and they have their freetime
/// books there... provide soft clues about what kind of people stay in those rooms."</i> So a room's shelves
/// are seeded from its OCCUPANT: <b>the work shelf says what they did, the freetime shelf says who they
/// were.</b> Three pieces make a person (§12.3) — a shelf is piece-material and never a dossier, and no
/// shelf anywhere names anybody.</para>
///
/// <h3>The engine, which is never on screen</h3>
///
/// <para><see cref="OddBooks"/>' engine, one scale down. The facility runs a books-as-intelligence function:
/// staff who know they are told nothing, reading EVERYTHING, sifting for leaks about the before-worlds. The
/// odd book is what that department left in a room nobody works in any more; <b>this</b> is what the people
/// it employed kept on their own walls. <b>The department read all of it and found nothing, and that fact is
/// nowhere stated and everywhere present.</b></para>
///
/// <h3>THE OCCUPANCY RULE — derived, never stored, never named</h3>
///
/// <para>The repo has no occupant concept and this file does not invent one. There is no name, no record, no
/// roster: there is a question, <see cref="Occupied"/>, and it is answered out of the ground the building
/// already publishes. A room is somebody's when it is <b>a room somebody was given</b>:</para>
///
/// <list type="number">
/// <item><b>A chamber — or the one room the department that reads everything was actually given.</b>
/// <see cref="UndergroundComplex.RoomKind.Chamber"/> is the module the building is made of: the room off a
/// rib, with a door and a plate. A hall is a venue, a cabinet is a booking, a cubicle and a cell are
/// plumbing, a meeting room is a room a DEPARTMENT books, and a ring suite is rank — none of them is a room
/// one person sat in every day. The single exception is <c>PRIVILEGED RECORDS · READING ROOM</c>, a
/// park-view suite in the block's own register (<see cref="UndergroundComplex.ParkViewPlates"/>), which is
/// the audit's answer to the catalog's <i>"if such a room exists"</i>: it does, it is a suite and not a
/// chamber, and it is the one room on this ground whose plate IS the engine behind the feature.</item>
/// <item><b>With a plate on it.</b> A gallery in the band nobody dug carries none (#677) and never did: it
/// was not labelled because nobody ever worked there, and a paperback down there would be the most
/// explaining object in the game — see <see cref="OddBooks.ShelvesStandHere"/>, which this asks rather than
/// re-deciding.</item>
/// <item><b>That is not the empty store.</b> The owner's own escape hatch (<see cref="ChamberFitting.IsEmptyStore"/>):
/// a store that says it is empty is empty, shelves included.</item>
/// <item><b>Whose TRADE is one somebody stands in.</b> Read off the ladder the furniture is already dealt by
/// (<see cref="ChamberFitting.KitFor"/>), so a room's shelves and a room's benches can never disagree about
/// what is done in it. <see cref="ChamberFitting.Kit.Store"/> is stock and <see cref="ChamberFitting.Kit.None"/>
/// is nothing — <b>a storeroom has no occupant and therefore no shelves</b>, which is the rule saying out
/// loud what the owner's brief predicted it would.</item>
/// </list>
///
/// <para>Deterministic per (site, floor, room) with no dice on the occupancy itself: whether a room is
/// somebody's is a fact about the plate and the department, not a roll. The FREETIME shelf is the one seeded
/// thing here, and it is seeded per floor rather than per room — see <see cref="DealOn"/>.</para>
///
/// <h3>Why there is no second reader</h3>
///
/// <para>A shelf is read exactly as the odd book is read: a fixture, an [E], a card in the #528 caption-only
/// idiom, and a gist the casebook learns once per game-thread. The read-list is the odd book's own
/// (<c>Vault.Progress.OddBooksRead</c>) and the ids are namespaced so the two can never collide. A room may
/// hold both — the odd book is one in six of the rooms nobody works in, and these are the rooms somebody
/// did.</para>
/// </summary>
public static partial class Shelves
{
    /// <summary>The glyph the shelf line and the casebook entry both carry.</summary>
    public const string Glyph = "\U0001F4DA";

    /// <summary>
    /// WHOSE ROOM THIS IS, said as a trade and never as a person. Six posts, one per authored work shelf.
    ///
    /// <para><see cref="None"/> is not a person with no books: it is a room with nobody in it, and a room
    /// with nobody in it has no shelves at all.</para>
    /// </summary>
    public enum Post
    {
        /// <summary>Nobody. A storeroom, a gallery, the store that says it is empty.</summary>
        None,

        /// <summary>The plant floors and the rooms plated POWER or PLANT — the people who keep it running.
        /// The department livery has called them <i>engineering rust</i> since #605.</summary>
        Engineering,

        /// <summary>ISOLATION, and every room plated in the clinic's own register.</summary>
        Clinic,

        /// <summary>ADMINISTRATION, ARCHIVE, and the clerks of a depot, a transit station and the head
        /// office. The commonest post in the building, which is the honest arithmetic of a place whose
        /// horror is administrative.</summary>
        Records,

        /// <summary>LABORATORIES, and the assay and calibration rooms of the band nobody listed.</summary>
        Lab,

        /// <summary>The rooms whose plate is about WHO COMES THROUGH THE DOOR — the only place in this
        /// building where somebody's job is the door itself. See <see cref="IsAPost"/>.</summary>
        Security,

        /// <summary>The department that reads everything, where the block gave it a room with a view.
        /// <c>PRIVILEGED RECORDS · READING ROOM</c> and nowhere else in the game.</summary>
        ReadingRoom,
    }

    /// <summary>One authored shelf. <see cref="Shelf"/> is what the room shows, <see cref="Card"/> is what
    /// [E] reads, <see cref="Gist"/> is what the casebook keeps.
    ///
    /// <para>All three are AUTHORED TEXT, lifted verbatim from #701's library-layer catalog. Nothing in this
    /// file may reword them; the framing glyph on the shelf line is house prose and the authored fragment
    /// inside it is asserted present character-for-character by the guards.</para>
    ///
    /// <para><see cref="Id"/> is what the read-list stores — short, stable, namespaced against the odd
    /// book's own ids, and never shown. Renaming one re-files a shelf a captain has already read, so they
    /// are as fixed as the prose.</para></summary>
    public readonly record struct Entry(string Id, string Shelf, string Card, string Gist);

    // ── THE WORK SHELVES · what the occupant DID ──────────────────────────────────────────────────────

    /// <summary>THE AUTHORED WORK CATALOG — one per <see cref="Post"/>, verbatim.</summary>
    public static IReadOnlyList<Entry> Work { get; } =
    [
        new Entry("work:engineering",
            "a university standard in its twenty-seventh edition, the spine cracked at the chapter on " +
            "transfer orbits",
            "Twenty-seven editions. The chapter that falls open is the one on getting from one orbit to " +
            "another cheaply. Somebody needed it often.",
            "the engineer's shelf — a twenty-seventh edition, opened always at the same chapter"),

        new Entry("work:clinic",
            "a pharmacopoeia with a hospital's stamp inside the cover, the dosages pencilled over in a " +
            "smaller hand",
            "The stamp is a hospital's, somewhere with weather. The dosages have been changed in pencil, " +
            "all of them downward, in a hand that was sure.",
            "the clinic's shelf — a hospital's book, every dose pencilled down"),

        new Entry("work:records",
            "a binder of filing conventions, three revisions deep, every revision initialled",
            "Three revisions of how to file things, each initialled by the same person. The third one is " +
            "shorter than the first.",
            "the clerk's shelf — three revisions of how to file, the last the shortest"),

        new Entry("work:lab",
            "a bench manual on cold storage, the tables of hold-times worn to grey",
            "A manual on keeping things cold for a long time. The tables are worn where a thumb ran down " +
            "them, looking for one number.",
            "the lab's shelf — a cold-storage manual, thumbed at one column"),

        new Entry("work:security",
            "a patrol manual, unopened, and a paperback under it that has been opened a great deal",
            "The manual has never been read. The paperback under it has been read to pieces.",
            "the guard's shelf — the manual unread, the paperback read to pieces"),

        new Entry("work:reading-room",
            "catalogue cards in a language nobody here was born speaking",
            "Cards, thousands, in a hand-drawn script. Whoever catalogued this did not learn the alphabet " +
            "here.",
            "the reading room — catalogue cards in a borrowed alphabet"),
    ];

    // ── THE FREETIME SHELVES · who the occupant WAS ───────────────────────────────────────────────────

    /// <summary>THE AUTHORED FREETIME CATALOG — seven personas, verbatim, in the catalog's own order.
    ///
    /// <para>The order is part of the contract: it is what <see cref="DealOn"/> permutes and what the
    /// <c>?shelf=</c> cheat names, so reordering this list renames every row in the testing guide.</para></summary>
    public static IReadOnlyList<Entry> Freetime { get; } =
    [
        new Entry("free:ships-with-opinions",
            "far-future paperbacks with cracked spines, the kind where the ships have names and opinions",
            "Ships with names, ships with opinions, ships that outlive everyone aboard. Somebody down here " +
            "read these for comfort.",
            "freetime — paperbacks where the ships have opinions"),

        new Entry("free:chess-problems",
            "a book of chess problems, every one solved in pencil, the last one not",
            "Every problem solved, in pencil, in order. The last one has a single move written and then " +
            "nothing.",
            "freetime — chess problems, the last one unfinished"),

        new Entry("free:bird-guide",
            "a field guide to birds of a coast nobody here has seen",
            "Birds, by colour and call, of a coast with tides. Somebody kept it where they could reach it.",
            "freetime — a bird guide for a coast with tides"),

        new Entry("free:cookbook",
            "a cookbook, and no kitchen on this floor",
            "Recipes for a kitchen that is not on this floor, or on any floor. The pages for bread are the " +
            "dirtiest.",
            "freetime — a cookbook on a floor with no kitchen"),

        new Entry("free:one-poem",
            "poetry in a small edition, one page dog-eared so often it is soft",
            "A small book, one page folded and unfolded until the corner is cloth. It is not a long poem.",
            "freetime — one poem, folded soft"),

        new Entry("free:childs-primer",
            "a child's primer, kept where nobody would have to explain it",
            "Letters and animals. It is on the low shelf, behind the others.",
            "freetime — a child's primer, behind the others"),

        new Entry("free:collision-book",
            "the collision book — a real title on the spine and nobody left to argue with",
            "A book that says the planets used to be somewhere else, and the old stories remember it. It " +
            "has been argued with in the margins, and then the arguing stops.",
            "freetime — the collision book, argued with and then not"),
    ];

    /// <summary>How many personas there are. Named so the deal, the guards and the testing guide all read
    /// one number rather than a 7 typed in four places.</summary>
    public static int Personas => Freetime.Count;

    /// <summary>The work shelf for a post. Throws on <see cref="Post.None"/>, deliberately: a caller asking
    /// what is on nobody's shelf has skipped <see cref="Occupied"/>, and a quiet empty here would be a room
    /// with half a library in it.</summary>
    public static Entry WorkShelf(Post post) => post switch
    {
        Post.Engineering => Work[0],
        Post.Clinic => Work[1],
        Post.Records => Work[2],
        Post.Lab => Work[3],
        Post.Security => Work[4],
        Post.ReadingRoom => Work[5],
        _ => throw new ArgumentOutOfRangeException(
            nameof(post), post, "nobody works in this room, so nothing is on its shelf"),
    };
}
