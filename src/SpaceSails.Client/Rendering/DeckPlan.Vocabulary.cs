using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// #251 · Split from DeckPlan.cs, moved verbatim: the VOCABULARY a deck is written in — the wall, the
// door, the console spot, the backdrop, the park's grow-light rig, the filled structure and the furniture
// rectangle. Every constant (DoorOpenRadius, the reach and clearance radii, the hatch and the droid cap)
// and every array, property and field of the plan stays in DeckPlan.cs, with Move, Droid and the stone's
// questions.
public sealed partial class DeckPlan
{
    /// <summary>A wall segment. <paramref name="IsHull"/> draws it as pressure hull — bright and thick,
    /// the readable boundary of a made thing; anything else draws as a dimmer inner line.
    ///
    /// <para>#563 · <paramref name="Unseen"/> is a wall the eye NEVER sees: it collides exactly like every
    /// other wall, but nothing is drawn for it. Owner, 2026-07-31: <i>"The space ships come with outside
    /// borders but the landing site out-doors should not... at least not obviously so with square area"</i>,
    /// and <i>"if our space has limits for some technical reasons then let's not advertise it, more like
    /// hide that fact."</i> A hull IS a real boundary and drawing it as one is right — that treatment stays
    /// on ships. The open regolith has no such object, so the field's envelope must not borrow the ship's
    /// ink. An airless moon has no atmosphere to scatter light: ground the lamp never reaches is simply
    /// black, and the field does not END so much as stop being visible.</para>
    ///
    /// <para>The deeper reason to hide it (owner, same session): <i>"the reevers and supply line are kind
    /// of the invisible tether to players distance"</i>. The real edge of a landing site is the point where
    /// the magazine and the pack behind you say turn around — the #453 law that how deep you dare go is
    /// priced by sentries and nerve, NOT by geometry. A drawn rectangle competes with that tether and wins,
    /// announcing the wrong limit before the honest one can be felt.</para></summary>
    /// <param name="IsStone">#563 · Solid mass that is NOT a made pressure boundary — a monolith, a
    /// plinth, a mass-driver muzzle, an ancient spur. It draws heavy like hull, because it IS solid, but in
    /// rock rather than metal.
    ///
    /// <para>Owner's complaint was that a landing site looks artificial, and removing the rim fence only got
    /// half of it: the body's own geography flags its solid objects with <c>IsHull</c>, and on Miranda's
    /// Ridge Camp that is 16 of 25 segments drawn in the ship's cold pressure-hull stroke. The flag was
    /// never wrong — a monolith IS solid and a fallen span is not — it was the INK that was wrong. Same
    /// distinction, different material.</para></param>
    /// <param name="IsSeamless">#677 · THE THIRD IDIOM. Not a made pressure boundary and not a body's stone
    /// — a continuous, unbroken surface that carries no texture, no joints and no palette at all.
    ///
    /// <para>The game has exactly two wall materials and both of them say who built the thing: hull ink is
    /// poured, welded, bolted and paid for; stone ink is the body's own rock in the body's own colour. The
    /// found halls (#677) are neither, so they are drawn as neither. <b>The absence of texture IS the
    /// style</b> — owner's ruling: <i>"it is just built into the smooth monolith style walls"</i> — which on
    /// a crude top-down grid reads exactly as wrong as it should, because every other wall in the game
    /// belongs to somebody.</para>
    ///
    /// <para>It takes NO ink from <see cref="DeckPlan.HullInk"/> or <see cref="DeckPlan.StoneInk"/>, and that
    /// refusal is the load-bearing half: a palette is a fact about a moon or a department, and either one
    /// applied here would quietly answer the question the whole feature exists to leave open. The precedent
    /// is #649's slab, which says <i>no seam</i> by having no interior line-work rather than by being
    /// labelled.</para></param>
    public readonly record struct Wall(
        float X1, float Y1, float X2, float Y2, bool IsWindow, bool IsHull, bool Unseen = false,
        bool IsStone = false, bool IsSeamless = false);

    /// <summary>An airlock door across a passage. An automatic door slides open as the avatar nears
    /// (a top-down flourish; it never blocks — the passage is always walkable). A <c>Locked</c> door
    /// stays shut and is drawn cold — it marks another berth's sealed hatch, decoration only, and is
    /// backed by a real wall so you can't pass.
    ///
    /// <para>#462 · <paramref name="Interlock"/> groups doors into an AIRLOCK. Owner, 2026-07-27: "The idea
    /// is that only one door in a tube is open at a time… think of airlock" — "both doors being open at the
    /// same time defeats the purpose (an abstraction though at this case)". Doors sharing a non-zero group
    /// take turns: only the one nearest the captain may stand open, so the far end is ALWAYS drawn shut.
    /// That is what finally gives the Old Ones a visible thing to stop at (#462) — the outer door closing
    /// behind you as the inner opens — and it is why a tailgater ends up shut in the tube with the built-in
    /// gun (#461) rather than following you aboard. 0 = an ordinary door with no partner.</para></summary>
    /// <param name="Imported">#592 · This door was not made here. Owner: <i>"some special color not
    /// distinctive to the site could then used to draw our attention to a place (like expensive door made
    /// with far away imported materials)."</i> Every ordinary hatch is drawn in its world's own stone, so the
    /// one that is not becomes a sentence — somebody shipped materials across the system to seal this, and
    /// nobody does that for a store cupboard.</param>
    /// <param name="Machined">#606 · Not merely off-palette — a different KIND of object. Owner, on hiding
    /// the lift head in an ordinary hut: <i>"The expensive doors would be the clue."</i>
    ///
    /// <para>Colour alone had already failed once (#585): violet marks shelters, about one ruin hatch in
    /// seven, and the way down, so it identified nothing. A tell that has to survive being one of three
    /// things has to be readable as SHAPE. This one is drawn heavy, with a second inner rail and its frame
    /// picked out at the jambs — a machined pressure door in a wall of piled regolith, next to hatches that
    /// are a single thin stroke. It still opens: sealed is what it looks like, not what it does.</para></param>
    public readonly record struct Door(
        float X1, float Y1, float X2, float Y2, bool Locked = false, int Interlock = 0,
        bool Imported = false, bool Machined = false);

    /// <summary>An interaction point on the deck. A <see cref="ConsoleKind.ViewObject"/> spot also
    /// carries an <paramref name="ImageUrl"/> and <paramref name="Caption"/> — press E and the game
    /// pops up that Gen-AI image (a souvenir, a lore prop) with its caption.</summary>
    /// <param name="Outcome">#774 · What the act that raised this card actually FOUND — the reveal card's
    /// own field (#736), grown here because the surface's full-screen cards are these ones. The caption is
    /// the fiction and this is the record: the name that came out of the kit, the family who are waiting,
    /// the moon somebody named. It is a REGION and not a slot, which is the whole of #774 — an event with
    /// four things to say in one breath says all four, instead of writing three of them into a backdrop
    /// nobody can read and letting append order pick the survivor (the contract #693 killed).</param>
    /// <param name="Run">#791 · THE FIXTURE'S REACH ALONG ITSELF, as the SEGMENT it actually is — so the
    /// interaction point is a line rather than a dot, and <see cref="NearestConsoleSpot"/> measures to the
    /// nearest point on it.
    ///
    /// <para>Owner, live at the B1 bar: <i>"The Bar desk is really long now, but there is only one spot to
    /// get service on it… we would need an E-bus of the bar desk length instead of one bar keep cashier at a
    /// single spot."</i> The desk is eighty-odd deck units and the press reached six of them.</para>
    ///
    /// <para><b>Null everywhere else, which is every console in the game but this one.</b> A helm is a chair
    /// and a valve is a wheel; they are points and they stay points — a fixture with no run is its own
    /// endpoint, so the distance below is the distance it always was. Nothing about any other deck changes,
    /// and no caller has to learn a new idea to keep working.</para>
    ///
    /// <para>#827 · <b>It is the segment and not a half-span off (X, Y), because the two are not concentric
    /// any more.</b> A counter's PLATE stands in front of the desk, on the square a body can stand on and
    /// every walkability audit walks to; the desk's own front FACE is a step behind it, and the face is what
    /// you order over. While the run was a span about the plate, the deck drew a cyan service rail two du
    /// clear of the bar's own photograph and the owner read the picture as the counter — because the picture
    /// WAS the counter.</para>
    ///
    /// <para>The segment is HANDED DOWN from whoever carved the fixture (for the counter: the hall's own
    /// <c>Service</c> run, which is its desk's front face). A renderer that worked out how long a bar is
    /// would be doing geometry about a room it did not carve, which is §13.15 and the reason this project
    /// has twice put a captain in a wall.</para></param>
    public readonly record struct ConsoleSpot(ConsoleKind Kind, float X, float Y, string Label,
        string? ImageUrl = null, string? Caption = null, string? Outcome = null,
        (float X0, float Y0, float X1, float Y1)? Run = null)
    {
        /// <summary>#791 · Is this fixture a RUN rather than a point? Asked, rather than compared against
        /// zero at four call sites.</summary>
        public bool IsRun => Run.HasValue;

        /// <summary>#791 · One end of the run — (X, Y) itself where the fixture is a point.</summary>
        public (float X, float Y) End0 => Run is { } r ? (r.X0, r.Y0) : (X, Y);

        /// <summary>#791 · The other end.</summary>
        public (float X, float Y) End1 => Run is { } r ? (r.X1, r.Y1) : (X, Y);

        /// <summary>#791 · How far this spot is from the fixture — to the nearest point ON it, which for a
        /// point console is the point itself. <see cref="SurfaceCollision.DistanceToSegment"/>, so the reach
        /// the key uses, the reach the pen draws and the reach a guard measures are one function.</summary>
        public double DistanceFrom(double x, double y)
        {
            (float ax, float ay) = End0;
            (float bx, float by) = End1;
            return SurfaceCollision.DistanceToSegment(x, y, ax, ay, bx, by);
        }

        /// <summary>#791 · The point on the fixture nearest a captain — where the [E] prompt is drawn, so a
        /// captain at the far end of a long desk sees the offer beside THEM rather than forty du away at the
        /// plate. Clamped to the segment, which for a point console is the point.</summary>
        public (float X, float Y) NearestPointTo(double x, double y)
        {
            if (Run is not { } r)
            {
                return (X, Y);
            }
            double ex = r.X1 - r.X0, ey = r.Y1 - r.Y0;
            double len2 = (ex * ex) + (ey * ey);
            if (len2 <= 0)
            {
                return (r.X0, r.Y0);
            }
            double t = Math.Clamp((((x - r.X0) * ex) + ((y - r.Y0) * ey)) / len2, 0.0, 1.0);
            return ((float)(r.X0 + (t * ex)), (float)(r.Y0 + (t * ey)));
        }
    }

    /// <summary>A room backdrop image: top-left at (X, Y) in deck units, W×H deck units, drawn
    /// under the vector overlay. The top-down renderer walks these.
    ///
    /// <para>#759 · <c>GrowLight</c> marks the panels lit by the park's own lamps rather than by the
    /// building's. It is a FLAG and not a number, because the number is a function of sim-time
    /// (<see cref="SpaceSails.Core.ParkDay"/>) and this plan is built once per floor and then drawn for as
    /// long as the captain stands on it — bake the alpha in here and the park's day stops the moment you
    /// stop walking through doors. False on every other backdrop in the game, which is all of them.</para>
    /// </summary>
    public readonly record struct Backdrop(
        string Url, float X, float Y, float W, float H, float Alpha, bool GrowLight = false);

    /// <summary>
    /// #759 · <b>THE PARK'S OWN DAY, AS THE TWO THINGS A FRAME NEEDS TO DRAW IT</b> — the site whose cycle
    /// it is (the per-site phase offset is seeded off that id) and where the floodlight masts stand.
    ///
    /// <para>The LEVEL is deliberately absent, for <see cref="Backdrop"/>'s reason: a plan is built when a
    /// captain arrives on a floor and drawn thousands of times after, so a level stored here would be the
    /// light as it was when you walked in — which is precisely the thing a captain who lingers is meant to
    /// be able to watch change. The plan carries the site and the posts; <see cref="DeckView"/> asks
    /// <see cref="SpaceSails.Core.ParkDay"/> what they look like on the frame it is drawing.</para>
    /// </summary>
    public readonly record struct GrowLight(string SiteId, IReadOnlyList<(float X, float Y)> Masts);

    /// <summary>
    /// #537 · A RECTANGLE OF SOLID SHIP. Shielding bands, bulkhead runs, machinery spaces — anything that is
    /// STRUCTURE rather than room, drawn filled so it cannot be mistaken for somewhere you could be.
    ///
    /// <para>Owner: <i>"if we can see into them from the hall then they don't hide anything."</i> A narrow gap
    /// left black reads as a space, and a hiding place drawn as a space is not a hiding place — the whole
    /// knock-on-the-walls search was readable off the map until these were filled.</para>
    /// </summary>
    public readonly record struct Structure(float X0, float Y0, float X1, float Y1);

    /// <summary>
    /// #868 · ONE PIECE OF FURNITURE, AS THE RECTANGLE IT STANDS ON — filled, so a captain can see it.
    ///
    /// <para>Owner, reading a back-of-house room off the plan on 2026-08-13: <i>"The graphics kind of does
    /// not show there being a table"</i> · <i>"The bench is a line"</i> · and, as the positive control three
    /// paces away, <i>"The Shelving is clear as furniture goes."</i> His own fix, quoted: <i>"could the table
    /// just be a different color rectangle in front of the chair, so arms (and papers) could rest on
    /// it?"</i>, sealed with <i>"I think table should be similar just say table."</i></para>
    ///
    /// <para><b>Why the deck needed a new primitive at all.</b> Until this record, every fixture down here
    /// reached the screen as its own SOLIDS — the wall segments Core lays a box out of — so a rectangle drew
    /// as four strokes and a bench, which is one degenerate box, drew as one. That is honest about the
    /// collision field and useless as furniture: an outline reads as a space you could stand in, which is
    /// exactly the complaint #537 answered for structure with a fill. This is the same answer for the things
    /// standing IN a room rather than the things a room is made of, and it is drawn from the box Core
    /// published rather than measured off the wall list, so the picture and the plan cannot drift.</para>
    /// </summary>
    /// <param name="X0">Left edge, in deck units.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Tone">What the piece IS, never a colour — <see cref="BigLabels"/>'s own rule, because
    /// the plan is Core-shaped data and the ink lives in the renderer. 0 = a surface you work at (a desk
    /// bank, a table, a counter, a worktop), 1 = something you keep things in (shelving, a kitchenette),
    /// 2 = something you sit on (a bench).</param>
    public readonly record struct FurnitureSpot(float X0, float Y0, float X1, float Y1, int Tone)
    {
        /// <summary>#868 · Which tone a published fitting draws in. Asked of the KIND and never of the
        /// plate's wording, so a fixture renamed tomorrow keeps its ink — and asked here, once, so the pen
        /// and any guard about the pen read one answer.</summary>
        public static int ToneOf(SpaceSails.Core.RingOffice.Fitting kind) => kind switch
        {
            SpaceSails.Core.RingOffice.Fitting.Shelving
                or SpaceSails.Core.RingOffice.Fitting.Racking
                or SpaceSails.Core.RingOffice.Fitting.FilingCabinet
                or SpaceSails.Core.RingOffice.Fitting.Kitchenette
                // #828 · The secure disposal is a MACHINE you stand at rather than a surface you work on,
                // so it takes the kitchenette's ink and nobody reads it as a spare desk.
                or SpaceSails.Core.RingOffice.Fitting.SecureDisposal => 1,
            SpaceSails.Core.RingOffice.Fitting.Bench => 2,
            _ => 0,
        };

        /// <summary>#868 · Is this the kind of fitting that gets FILLED at all? A cubicle and a booth are
        /// little ROOMS — a captain steps inside one and shuts the door — so filling them would draw the one
        /// hiding place in the building as a solid block. A partition is a screen, which IS a line and is
        /// honestly drawn as one.</summary>
        public static bool IsFurniture(SpaceSails.Core.RingOffice.Fitting kind) =>
            kind is not (SpaceSails.Core.RingOffice.Fitting.Cubicle
                or SpaceSails.Core.RingOffice.Fitting.Booth
                or SpaceSails.Core.RingOffice.Fitting.Partition);
    }
}
