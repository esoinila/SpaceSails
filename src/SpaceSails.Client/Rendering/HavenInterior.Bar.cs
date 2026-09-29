using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · THE BAR AS A ROOM (#973 L0, #428, #1229) — where the walk ends at its door, the two leaves off its
/// back wall, its walkable band, its name, its tops and the places beside them.
///
/// <para>Split out of <c>HavenInterior.cs</c> under #251 as a pure move: two runs of the base file, no
/// member renamed, re-scoped or re-ordered. <c>BarTopY</c> and <c>BarTops</c> are measured off
/// <c>HallTopY</c> and so stay in the opening file with the door constants beside them, in their original
/// order (#1163).</para>
/// </summary>
public static partial class HavenInterior
{
    /// <summary>WHERE THE WALK ENDS — the position the <c>?ashore=1</c> boot cheat (#428) stands the
    /// captain at: one step past the hall's north door, the exact spot the REAL walk (ship → tube →
    /// immigration hall → this door) puts them the moment the bar becomes the room they are standing in.
    ///
    /// <para>Derived from the doorway itself — its two jambs and the hall's north edge — and never typed
    /// in. A cheat that invented its own coordinates would be a second source of truth for a fact the
    /// geometry already owns, and unaudited client geometry literals are this project's oldest and most
    /// reliably wrong bug class. The heading is <c>+Y</c>: facing into the room, the way you were walking
    /// when you came through (the deck's own convention — <c>atan2(dy, dx)</c>).</para></summary>
    public static (double X, double Y, double Heading) BarThreshold =>
        ((BarDoorLeft + BarDoorRight) / 2.0, HallTopY + AshoreStepDeckUnits, System.Math.PI / 2);

    /// <summary>How far past the door line the ashore boot stands: one avatar ACROSS, so the captain is
    /// wholly inside the room rather than straddling the door line they just crossed.</summary>
    private const double AshoreStepDeckUnits = 2 * DeckPlan.AvatarRadius;

    /// <summary>#1229 · <b>THE SAME DOORWAY, FROM THE OTHER SIDE</b> — one step SHORT of the bar's south
    /// wall, out on the concourse. Where somebody who is about to follow the captain into the room is
    /// standing before he does.
    ///
    /// <para>It exists because <see cref="BarThreshold"/> is where the <c>?ashore=1</c> boot stands the
    /// CAPTAIN, and a man dealt onto that spot is dealt on the captain's feet — which is what shipped, and
    /// it is not a tail, it is a collision. Derived from the same two jambs and the same hall edge as its
    /// twin, mirrored across the wall by the same one step, so the two of them cannot come to two views of
    /// where this doorway is.</para></summary>
    public static (double X, double Y) TheDoorstepOutsideTheBar =>
        ((BarDoorLeft + BarDoorRight) / 2.0, HallTopY - AshoreStepDeckUnits);

    /// <summary>
    /// #973 L0 · THE TWO LEAVES OFF THE BAR, as the building's own <see cref="UndergroundComplex.LockedDoor"/>
    /// — the type <see cref="Egress"/> insists on, and it insists for the reason that IS this beat: every
    /// member of that list is refused to the captain by construction, so a walker cannot be given a public
    /// door to vanish through by an oversight.
    ///
    /// <para>They are the cellar and the storeroom the bar has always had, hung on the room's two side walls
    /// (which are unbroken stone — the leaf is drawn on the wall, not cut into it), with the plate that is
    /// already painted on them. <see cref="BuildComplex"/> takes its doors and its knockable hatch consoles
    /// from this one call, so the sign a walker carries and the sign the captain reads are one string.</para>
    /// </summary>
    private static UndergroundComplex.LockedDoor[] BarBackRoomLeaves(char authority) =>
    [
        new(BarLeft, HallTopY + 9, BarLeft, HallTopY + 13, $"🔒 CELLAR · {authority}-B1"),
        new(BarRight, HallTopY + 9, BarRight, HallTopY + 13, $"🔒 STOREROOM · {authority}-B2"),
    ];

    /// <summary>
    /// #973 L0 · WHAT A DOCKED STATION'S BAR IS, TO SOMEBODY WHO HAS TO WALK ACROSS IT.
    /// </summary>
    /// <param name="BodyId">The berth, which is also the rota key — <see cref="SpaceSails.Core.NebulaRep"/>'s
    /// presence law is keyed on the BODY being visited, so a docked station needs no second kind of id.</param>
    /// <param name="FloorY">The bar's south wall. North of it is the room; the immigration hall is south. The
    /// one line that answers "is the captain in the bar", and it is the wall the room is built off rather than
    /// a threshold typed in somewhere else.</param>
    /// <param name="Doors">The leaves somebody may come out of, in the room's own order.</param>
    /// <param name="Fixtures">Where a person with nothing to do stands — the counter's service point, which is
    /// the spot this bar's own art draws its desk at (<c>BarDesks</c>) and the same one the captain bellies up
    /// to.</param>
    /// <param name="Tops">The room's tables, by their centres. Where a BODY stands at one is the caller's to
    /// sound against the stone, exactly as a canteen top's chair is.</param>
    public readonly record struct BarFloor(
        string BodyId,
        double FloorY,
        IReadOnlyList<UndergroundComplex.LockedDoor> Doors,
        IReadOnlyList<DeckReachability.Point> Fixtures,
        IReadOnlyList<DeckReachability.Point> Tops);

    /// <summary>#973 L5b · What this berth's bar is CALLED — THE STORMWATCH BAR, THE EARTHRISE, THE DEEP END.
    /// The same string the deck's own location strip reads off the spec, published because the strip's company
    /// clause needs it: a top in a station bar that announced itself as a canteen table was the sentence and
    /// the room disagreeing, and it was found by looking.</summary>
    public static string? BarNameOf(string bodyId) =>
        System.Array.Find(Specs, s => s.BodyId == bodyId) is { } spec ? spec.BarName : null;

    /// <summary>#973 L5b · How many a bar top seats. The room's own number, stated once — the sitting says it
    /// in chairs ("one of them is yours now"), and a second count anywhere would be the panel and the picture
    /// disagreeing about how alone the captain is.</summary>
    public const int BarTopSeats = 4;

    /// <summary>#973 L5b · The plate over a top nobody is at. The same shape the canteen's free top wears —
    /// the verb is TAKE THE TABLE, and the label is what the captain reads before pressing [E].</summary>
    public const string BarTopLabel = "🍸 A TOP NOBODY'S AT";

    /// <summary>
    /// #973 L5b · WHERE A BODY STANDS AT A BAR TOP — one body-width off its centre, on the first side the
    /// stone allows, hall side sounded first because that is the side somebody crossing this room comes from.
    ///
    /// <para>Published here, with the tops themselves, rather than kept private to whoever asked first. TWO
    /// callers need it and they must not disagree: the walker planning a crossing (<c>Map.BarWalkers</c>) and
    /// the seat putting the captain in a chair (<c>Seating.BarTop</c>). A second sounding would put the woman
    /// and the captain on the same square, which is the drawn room and the walked room disagreeing about a
    /// lap — this repository's third named bug class, at a table for two.</para>
    /// </summary>
    /// <returns>The place, or null when the stone allows no side of this top at all.</returns>
    /// <param name="clearOf">#973 L5b · Somebody who is already standing (or sitting) at this top, whose side
    /// is therefore taken. A top is a place for more than one body now — the captain in a chair at it and the
    /// woman who crossed the room to it — and a sounding that could not be told about the first would put the
    /// two of them on one square. Null when nobody is there yet.</param>
    public static DeckReachability.Point? BesideATop(
        DeckReachability.Point top, double radius, IReadOnlyList<SurfaceCollision.Segment> walls,
        DeckReachability.Point? clearOf = null)
    {
        double off = 2 * radius;
        (double X, double Y)[] sides =
        [
            (top.X, top.Y - off), (top.X + off, top.Y), (top.X - off, top.Y), (top.X, top.Y + off),
        ];
        foreach ((double x, double y) in sides)
        {
            if (SurfaceCollision.Blocked(x, y, radius, walls))
            {
                continue;
            }

            if (clearOf is { } taken)
            {
                double dx = x - taken.X;
                double dy = y - taken.Y;
                if ((dx * dx) + (dy * dy) < off * off)
                {
                    continue;   // that side is somebody's; a second body on it is a lap, not a table.
                }
            }

            return new DeckReachability.Point(x, y);
        }

        return null;
    }

    /// <summary>#973 L0 · The walkable band of a docked station's bar, or null at a berth with no interior to
    /// walk. Pure: it reads the same constants the room is carved from and builds nothing.</summary>
    /// <param name="level">#1253 · Which floor the captain is standing on. At
    /// <see cref="HavenLevels.Concourse"/> this is the bar, to the byte, exactly as it has always been. Below
    /// it, the room is the LOWER CONCOURSE — the same record, because everything that walks a haven floor
    /// wants the same four facts about it (which berth, where the room starts, which leaves somebody may come
    /// out of, where a body with nothing to do stands) and a second record would be a second set of walkers.
    /// A floor with no bar on it has no TOPS, which is what keeps the bar's own beats upstairs.</param>
    public static BarFloor? BarBand(string bodyId, int level = HavenLevels.Concourse)
    {
        if (System.Array.Find(Specs, s => s.BodyId == bodyId) is not { } spec)
        {
            return null;
        }

        if (level != HavenLevels.Concourse)
        {
            return spec.Lower is null ? null : TheLowerConcourseBand(spec);
        }

        BarDesk desk = BarDesks.For(spec.BodyId) ?? DefaultBarDesk(spec.BodyId);
        var tops = new List<DeckReachability.Point>(BarTops.Length);
        foreach ((float X, float Y) top in BarTops)
        {
            tops.Add(new DeckReachability.Point(top.X, top.Y));
        }

        return new BarFloor(
            spec.BodyId,
            HallTopY,
            BarBackRoomLeaves(spec.Authority[0]),
            [new DeckReachability.Point(desk.ServiceX, HallTopY + desk.ServiceYOffset)],
            tops);
    }

    /// <summary>The safe fallback desk for a bar whose art has not been measured — one place, so the band and
    /// the build cannot come to two different views of where the counter is.</summary>
    private static BarDesk DefaultBarDesk(string bodyId) => new(bodyId, 0.26f, 0.60f, 4.5f);

    /// <summary>#973 L0 · How many figures the docked complex draws before any walker: the ship's three, the
    /// customs officer, the four seated regulars, the Magpie, the barkeep, the oracle's corner and (#1202)
    /// Rauha Lind's own chair. Named
    /// because <see cref="BuildComplex"/> hands it to the plan and the walker band is written after it, and a
    /// buffer offset that is two opinions about one number is how this game has twice thrown
    /// <c>IndexOutOfRangeException</c> at the renderer.</summary>
    public const int SeatedFigureCount = 12;
}
