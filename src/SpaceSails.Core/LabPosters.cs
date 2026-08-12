using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #853 · THE POSTERS OF THE ETERNALLY PROMISING — conference gags for the LABORATORIES walls.
///
/// <para>Owner, 2026-08-12, a postdoc's own joke: <i>"as gags we could have gen-ai conference posters …
/// jokes about how hydrogen is the new promising tech (still 100 years in future), etc :-D … you know those
/// promising technology of the future (and always will be) :-D"</i>. And on the pool as written below:
/// <i>"yes love those :-D"</i> — the eight are CANONICAL and are implemented verbatim. No additions, no
/// rewrites, without a new ruling.</para>
///
/// <h3>The register</h3>
///
/// <para>Deadpan academic, no winks. The joke is that nobody in-world finds them funny: these are real
/// posters, from real symposia, pinned up by people who meant them, and the building has simply gone on
/// standing round them for a very long time.</para>
///
/// <h3>Why the eighth one is the reason the gag earns wall space</h3>
///
/// <para>Poster 8 is an eternally-future poster for the technology humming in this building's basement. Canon
/// law holds absolutely (§13.8, and the 2026-07-30 ruling on what the Old Ones are): nothing is stated,
/// nothing is confirmed, no sensor agrees with it and no card explains it. The wall just quietly disagrees
/// with the facility it is bolted to, and NOTHING ANYWHERE IN THE GAME REMARKS ON IT — which is why that
/// sentence of the owner's brief is honoured by its absence from the copy below rather than printed in it.
/// It hangs slightly crooked, in the worst corridor on the floor, and it hangs there because somebody in
/// 1970-something thought it was a safe bet and nobody has been in to take it down.</para>
///
/// <h3>Where they hang, and who decides</h3>
///
/// <para>Placement is PUBLISHED from Core like all dressing (#818's idiom): a poster is a spot on a wall the
/// generator already built, derived off the chamber's OWN doorway — beside the leaf, on the corridor side,
/// exactly where the building already hangs its door plates ("BESIDE the door and never over it, on the side
/// the walker is coming from"). Not one coordinate is typed here. The renderer hangs a card on it and
/// measures nothing.</para>
///
/// <h3>The art</h3>
///
/// <para>Eight slots in the art-manifest idiom (<see cref="ArtUrlFor"/>), and they degrade the way every
/// unshipped slot in this game degrades: no file, no frame, and the fine print is still there to read. The
/// pictures are a later gen-AI pass — one "aged conference poster" style run so they read as a set.</para>
/// </summary>
public static class LabPosters
{
    /// <summary>
    /// #853 · ONE POSTER, AS THE COPY AND NOTHING ELSE. The canonical pool lives in this shape so a guard
    /// can compare it word for word against the ruling without knowing anything about where it hangs.
    /// </summary>
    /// <param name="Title">What is printed across the top, in the poster's own shout.</param>
    /// <param name="FinePrint">The line you have to stop and read — the whole of the gag, and the reason the
    /// press exists at all.</param>
    /// <param name="Slug">The word its art slot is named for.</param>
    public readonly record struct Copy(string Title, string FinePrint, string Slug);

    /// <summary>
    /// #853 · THE POOL, VERBATIM AND OWNER-BLESSED. Eight, in the order they were written and approved.
    ///
    /// <para>Changing a character of this list changes canon. The guard hashes it for that reason.</para>
    /// </summary>
    public static readonly IReadOnlyList<Copy> Pool =
    [
        new("HYDROGEN: FUEL OF THE COMING CENTURY",
            "134th Annual Symposium. Voted Most Promising Energy Vector, 134th consecutive year.",
            "hydrogen"),

        new("FUSION: NET GAIN WITHIN THIRTY YEARS",
            "The year on the poster is a sticker, visibly applied over at least four older stickers.",
            "fusion"),

        new("ROOM-TEMPERATURE SUPERCONDUCTIVITY: REPLICATION RESULTS EXPECTED SHORTLY",
            "The poster is the most yellowed object on the floor.",
            "superconductivity"),

        new("THE SPACE ELEVATOR: A GROUND-FLOOR OPPORTUNITY",
            "Investment prospectus styling, tasteful, terrifying.",
            "space-elevator"),

        new("THORIUM: TOO CHEAP TO METER (THIS TIME)",
            "Workshop, attendance free, lunch not provided.",
            "thorium"),

        new("SOLID-STATE BATTERY BREAKTHROUGH DOUBLES EVERYTHING",
            "'Commercialization expected in five years' in a typeface five typefaces old.",
            "solid-state"),

        new("GENERAL MACHINE INTELLIGENCE ALIGNMENT WORKSHOP",
            "Postponed. New date to follow.",
            "alignment"),

        new("NEURAL STATE PRESERVATION: A CENTURY OF CHALLENGES AHEAD",
            "'Keynote: Why Whole-Brain Capture Will Remain Impossible.' The poster is old. Nobody has taken "
            + "it down.",
            "neural-state"),
    ];

    /// <summary>Which of the eight is the kicker — the one about the thing this building actually does, and
    /// the one that hangs crooked at the far end of the worst corridor. Named rather than written as a
    /// literal 8 at three call sites: it is the LAST of the pool, and a pool that grew would take its kicker
    /// with it.</summary>
    public static int KickerNumber => Pool.Count;

    /// <summary>Where a poster's picture lives, in the art-manifest idiom every other slot in this game uses.
    /// Nothing is shot yet; the slot degrades cleanly, which is the whole reason a slot is published before
    /// a picture exists.</summary>
    /// <param name="number">1-based, as the pool reads.</param>
    public static string ArtUrlFor(int number)
    {
        if (number < 1 || number > Pool.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(number), number, "there are only eight posters and they are numbered from one");
        }
        return $"art/lab-poster-{Pool[number - 1].Slug}.jpg";
    }

    /// <summary>
    /// #853 · ONE POSTER ON ONE WALL — the copy, the slot, and the spot the generator hung it on.
    /// </summary>
    /// <param name="Number">1-based, as the pool reads.</param>
    /// <param name="X">Where it hangs, in the surface's own coordinates — beside the door, on the corridor
    /// side, which is where this building hangs everything a walker is meant to read.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Crooked">#853 · Does it hang askew? True of the kicker and nothing else. Published as a
    /// FACT about the poster rather than left to a renderer's mood — see the class summary. The deck says so
    /// in words today (the plate carries it); a renderer that can rotate a label cheaply has the flag
    /// waiting for it.</param>
    public readonly record struct Poster(int Number, double X, double Y, bool Crooked)
    {
        /// <summary>Its copy.</summary>
        public Copy Text => Pool[Number - 1];

        /// <summary>What is printed across the top of it.</summary>
        public string Title => Text.Title;

        /// <summary>The line you stop and read.</summary>
        public string FinePrint => Text.FinePrint;

        /// <summary>Its art slot.</summary>
        public string ArtUrl => ArtUrlFor(Number);

        /// <summary>What is drawn on the deck beside it. The glyph names the object, the shout names the
        /// conference, and the kicker's own plate says it is askew because that is a thing you can see from
        /// across a corridor and the renderer draws its labels straight.</summary>
        public string Plate => Crooked ? $"{Glyph} {Title} (askew)" : $"{Glyph} {Title}";

        /// <summary>What [E] puts on the card — the whole of the gag, which is fine print by design.</summary>
        public string Card => FinePrint;
    }

    /// <summary>A framed thing on a wall, as this game writes one.</summary>
    public const string Glyph = "🖼";

    /// <summary>How far off the wall the spot stands, so the plate is read in the corridor rather than
    /// inside the wall it is bolted to. Deliberately less than the corridor's own half-width, so a poster
    /// never stands where a body walks down the middle of a passage.</summary>
    public const double StandoffDu = 1.2;

    /// <summary>…and how far ALONG the wall it hangs from the middle of the leaf — past the door's own jamb,
    /// which is <see cref="UndergroundComplex.DoorHalf"/>, and then a hand's breadth. Beside the door and
    /// never over it: a sign you have to step off to read is not signage (#775's own finding, said again
    /// about a picture).</summary>
    public static double AsideDu => UndergroundComplex.DoorHalf + 1.6;

    /// <summary>
    /// #853 · WHICH WALLS OF THIS FLOOR CARRY A POSTER.
    ///
    /// <para>LABORATORIES floors and nowhere else — the department the gag is about, asked of
    /// <see cref="ChamberFitting.LabsOn"/> so one sentence decides it for the furniture and the wall
    /// dressing alike. Empty everywhere else, which is a true statement about those floors rather than a
    /// missing one.</para>
    ///
    /// <para>ONE PER CHAMBER, in the order the pool was written, walking OUT from the lift — so a captain who
    /// steps off the car meets HYDROGEN first and the corridor gets less promising the further in they go.
    /// The kicker takes the far end: the room furthest from the way home, on the floor's worst corridor,
    /// which is the owner's <i>"hang it slightly crooked, in the worst corridor"</i> read off the plan rather
    /// than typed at a coordinate.</para>
    /// </summary>
    /// <param name="bodyId">The moon this building is under.</param>
    /// <param name="level">Which floor.</param>
    /// <param name="rooms">The floor as carved — <c>FloorPlan.Rooms</c>.</param>
    /// <param name="shaftX">The cage's x, so "out from the lift" is measured off the way home itself.</param>
    /// <param name="shaftY">The same.</param>
    public static IReadOnlyList<Poster> On(
        string bodyId, int level, IReadOnlyList<UndergroundComplex.Room> rooms,
        double shaftX, double shaftY)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(rooms);

        if (!ChamberFitting.LabsOn(ChamberFitting.DepartmentOn(bodyId, level)))
        {
            return [];
        }

        // Every chamber with a door in it and a name on it. A gallery has neither (#677) and a suite is not
        // on a laboratories corridor.
        var walls = new List<(double Gap, double X, double Y)>();
        foreach (UndergroundComplex.Room room in rooms)
        {
            if (room.Kind != UndergroundComplex.RoomKind.Chamber
                || room.Plate.Length == 0 || room.Ways.Count == 0)
            {
                continue;
            }

            // The FIRST way is the room's own corridor door — the one its face was cut for. The corridor is
            // whichever side of that leaf the room is not on, which is a fact the two published boxes answer
            // between them and nothing here has to be told.
            SurfaceLayout.Doorway leaf = room.Ways[0];
            double lx = (leaf.X1 + leaf.X2) / 2.0, ly = (leaf.Y1 + leaf.Y2) / 2.0;
            bool vertical = Math.Abs(leaf.X1 - leaf.X2) < Math.Abs(leaf.Y1 - leaf.Y2);

            double px, py;
            if (vertical)
            {
                px = lx + (Math.Sign(lx - room.X) * StandoffDu);
                py = ly + AsideDu;
            }
            else
            {
                px = lx + AsideDu;
                py = ly + (Math.Sign(ly - room.Y) * StandoffDu);
            }

            double dx = room.X - shaftX, dy = room.Y - shaftY;
            walls.Add(((dx * dx) + (dy * dy), px, py));
        }

        if (walls.Count == 0)
        {
            return [];
        }

        walls.Sort((a, b) => a.Gap.CompareTo(b.Gap));

        var hung = new List<Poster>(Math.Min(walls.Count, Pool.Count));

        // The kicker, first — it takes the far end, and taking it before the run is dealt is what stops the
        // seventh poster and the eighth landing on the same wall on a floor with exactly eight chambers.
        (double _, double kx, double ky) = walls[^1];
        hung.Add(new Poster(KickerNumber, kx, ky, Crooked: true));

        int n = Math.Min(walls.Count - 1, Pool.Count - 1);
        for (int i = 0; i < n; i++)
        {
            (double _, double x, double y) = walls[i];
            hung.Add(new Poster(i + 1, x, y, Crooked: false));
        }

        return hung;
    }

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (Copy c in Pool)
        {
            yield return c.Title;
            yield return c.FinePrint;
        }
    }
}
