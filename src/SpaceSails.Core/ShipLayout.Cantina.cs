namespace SpaceSails.Core;

/// <summary>
/// #251 · HER CANTINA IS A BAR (#1040) — the counter, the back bar, the stools, the service and galley
/// stations, the tops and the label.
///
/// <para>Split out of <c>ShipLayout.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Every field here is a <c>const</c>; <c>Rooms</c> and
/// <c>DeskCabins</c>, the class's two initialised statics, stay in the opening file (#1163).</para>
/// </summary>
public static partial class ShipLayout
{
    // ── #1040 · HER CANTINA IS A BAR, AND A BAR HAS A COUNTER ────────────────────────────────────────
    //
    // Owner, on 7 Deck with the room in front of him: "Our on ship bar can be upgraded to match the other
    // bars... the UI represents code long time ago." He is describing a room that was drawn before any of
    // the furniture laws existed: three rings on an empty floor with the galley console standing in the
    // middle of them, and — the thing his sentence is really about — NO COUNTER AT ALL, in a room whose own
    // backdrop is a photograph of a counter with a row of stools down it.
    //
    // What a haven bar has and this room did not (HavenInterior.BuildComplex, #247 and #973 L0): a counter
    // that is a REAL WALL you belly up to rather than walk through; a service point on the players' side of
    // it; seats along it; and tables set away from it with room to walk between. All four are below, and
    // every number is read off the CANTINA compartment's own bounds — the fifth time geometry has been moved
    // out of `DeckPlan.BuildShip` into Core, and the first four all turned out to be already wrong the
    // moment a test could reach them.
    //
    // The composition is the room's own picture, which is the standard the owner set for the havens and
    // never for this room: `BarDesks` reads every bar art the same way — "bottle shelves and counter down
    // the LEFT, planet window top-right, patron tables to the right" — and art/the-space-bar.jpg, which is
    // the cantina's backdrop, draws exactly that. So the counter runs down her AFT wall with the back-bar
    // behind it, and the tops sit forward under the panoramic window.

    /// <summary>#1040 · The room the counter is in. Named once so the geometry below and any guard about it
    /// read one string.</summary>
    public const string CantinaRoom = "CANTINA";

    /// <summary>#1040 · How far the counter's own face stands off the cantina's aft wall. Three du: enough
    /// for the back-bar shelving, the counter's depth and a keep's shoulders behind it.</summary>
    private const float CounterStandoff = 3f;

    /// <summary>#1040 · How deep the counter and the back-bar shelving are drawn — the filled rectangles that
    /// make a fitting read as furniture rather than as floor (#868, the owner's own fix: <i>"could the table
    /// just be a different color rectangle"</i>).</summary>
    public const float CounterDepth = 0.8f;

    /// <summary>#1040 · One body-width, which is the offset every seat in this game is sounded at
    /// (<c>HavenInterior.BesideATop</c> uses two avatar radii). A stool tucks exactly that far off the
    /// counter's face: any nearer and the body is inside the counter, any further and it is not at it.</summary>
    private const float BodyWidth = 1.4f;

    /// <summary>
    /// #1040 · How far the counter's near end stands clear of the corridor wall — the way IN to the servery,
    /// so a captain can walk round the end of his own bar rather than being sealed out of half the galley
    /// (never-empty-floor, and its harder sibling: floor nobody can reach is not floor).
    ///
    /// <para>It is <b>not</b> one body-width, and the first cut of this lane made exactly that mistake: 1.4
    /// du leaves a body of radius 0.7 exactly one line of squares to walk down, pinched further by the
    /// counter's own end cap, and <c>DeckReachability</c>'s flood could not get behind the bar at all. A
    /// doorway is measured in bodies-you-can-pass-in, not in bodies-you-can-stand-in.</para>
    /// </summary>
    private const float CounterEndGap = 2.6f;

    /// <summary>#1040 · How far the first and last stool sit in from the counter's two ends, so no stool is
    /// closer to a wall than a body is wide.</summary>
    private const float StoolEndInset = 1f;

    /// <summary>#1040 · How many stools the row carries — three, at 1.2 du of elbow room each, which is what
    /// the counter's usable length affords once the servery has a door at one end and the window wall closes
    /// the other.</summary>
    public const int CantinaStoolCount = 3;

    /// <summary>#1040 · How many a stool seats. One, and the number is stated for the same reason
    /// <see cref="CantinaTopSeats"/> is: the panel says it in chairs, and a second count would be the strip
    /// and the picture disagreeing about how alone the captain is.</summary>
    public const int CantinaStoolSeats = 1;

    private static Room TheCantina
    {
        get
        {
            foreach (Room r in Rooms)
            {
                if (string.Equals(r.Name, CantinaRoom, System.StringComparison.Ordinal))
                {
                    return r;
                }
            }

            throw new System.InvalidOperationException(
                $"#1040 · there is no compartment called {CantinaRoom} aboard, so her counter has no room to "
                + "stand in. Rename the constant in the same commit as the room.");
        }
    }

    /// <summary>
    /// #1040 · <b>THE COUNTER'S OWN FACE</b> — the segment a captain leans on, and the wall that stops him
    /// walking through it.
    ///
    /// <para>It is ONE definition, exactly as <see cref="DoorSegment"/> is, and for the wreck's hard-won
    /// reason: the thing that DRAWS the counter, the thing that WALKS into it and the thing that measures a
    /// stool's standoff off it must be handed the same two points. A counter drawn where nothing collides is
    /// a bar you walk through; a counter that collides where nothing is drawn is a wall in an empty room.</para>
    ///
    /// <para>It runs from a body-width clear of the corridor wall right up into the window wall, so the
    /// servery has exactly one way in — round the near end — and nobody is ever standing behind the bar by
    /// accident.</para>
    /// </summary>
    public static (float X1, float Y1, float X2, float Y2) CantinaCounter
    {
        get
        {
            Room r = TheCantina;
            float x = r.X0 + CounterStandoff;
            return (x, r.Y0 + CounterEndGap, x, r.Y1);
        }
    }

    /// <summary>#1040 · The BACK-BAR — the shelving strip against the cantina's aft wall, behind the counter.
    /// A rectangle (X0, Y0, X1, Y1) in the room's own units; the pen fills it in the ink it fills every other
    /// thing-you-keep-things-in with.</summary>
    public static (float X0, float Y0, float X1, float Y1) CantinaBackBar
    {
        get
        {
            Room r = TheCantina;
            (float _, float y0, float _, float y1) = CantinaCounter;
            return (r.X0, y0, r.X0 + CounterDepth, y1);
        }
    }

    /// <summary>#1040 · …and the counter's own top, as the rectangle standing behind its face.</summary>
    public static (float X0, float Y0, float X1, float Y1) CantinaCounterTop
    {
        get
        {
            (float x, float y0, float _, float y1) = CantinaCounter;
            return (x - CounterDepth, y0, x, y1);
        }
    }

    /// <summary>
    /// #1040 · <b>THE STOOL ROW</b>, in the row's own order — one body-width off the counter's face, spaced
    /// evenly down its length with a stool's own inset at each end.
    ///
    /// <para>Derived and never listed, because a row of typed coordinates beside a counter of typed
    /// coordinates is two numbers for one fixture, which is this ship's named console bug (she has had four
    /// of them). Move the counter and the stools go with it; lengthen the room and the row spreads out.</para>
    /// </summary>
    public static IReadOnlyList<DeckReachability.Point> CantinaStools
    {
        get
        {
            (float x, float y0, float _, float y1) = CantinaCounter;
            float first = y0 + StoolEndInset, last = y1 - StoolEndInset;
            var row = new List<DeckReachability.Point>(CantinaStoolCount);
            for (int i = 0; i < CantinaStoolCount; i++)
            {
                double t = CantinaStoolCount == 1 ? 0.5 : (double)i / (CantinaStoolCount - 1);
                row.Add(new(x + BodyWidth, first + ((last - first) * t)));
            }

            return row;
        }
    }

    /// <summary>#1040 · Where the plate for the whole row is drawn and where [E] is answered FROM — the
    /// middle of the row, on a square a body can stand on (#827: a counter's plate stands in front of the
    /// desk; the desk's front face is what you order over, and the face is <see cref="CantinaCounter"/>).</summary>
    public static DeckReachability.Point CantinaCounterService
    {
        get
        {
            IReadOnlyList<DeckReachability.Point> row = CantinaStools;
            return new(row[0].X, (row[0].Y + row[^1].Y) / 2.0);
        }
    }

    /// <summary>
    /// #1040 · <b>THE GALLEY CONSOLE</b> — the one that opens the galley card, moved off the middle of the
    /// floor and onto the forward window corner where the food machines are.
    ///
    /// <para>It stood at (11, 7.5) — dead centre of the room, with a drawn table 1.5 du under it, which is
    /// how #1016 came to publish a seat on two of her three tops and refuse the third. That is the owner's
    /// own <i>"not at the middle of the empty floor"</i> ruling about the havens' barkeeps, unenforced in the
    /// one room he owns; the counter it belonged on did not exist until this lane built it.</para>
    /// </summary>
    public static DeckReachability.Point CantinaGalleyStation
    {
        get
        {
            Room r = TheCantina;
            return new(r.X1 - 1.5, r.Y1 - 1.0);
        }
    }

    /// <summary>
    /// #1040 · <b>HER TOPS</b>, forward of the counter and under the panoramic window — where a bar art puts
    /// its patron tables, and where the one view aboard actually is.
    ///
    /// <para>They were (8, 7.5), (11, 6) and (14, 7.5), laid across the middle of the room with the galley
    /// console on top of the middle one. They are read off the cantina's forward bounds now, and all three
    /// clear every fixture in the room — so the label law that decides which tops may carry a seat
    /// (<c>DeckPlan.LabelClearance</c>) passes all three rather than two.</para>
    /// </summary>
    public static IReadOnlyList<DeckReachability.Point> CantinaTops
    {
        get
        {
            Room r = TheCantina;
            return
            [
                new(r.X1 - 4.5, r.Y0 + 2.8),
                new(r.X1 - 1.5, r.Y0 + 2.5),
                new(r.X1 - 4.5, r.Y1 - 1.0),
            ];
        }
    }

    /// <summary>#1040 · Where the room writes its own name on the floor — clear of the stool row, the tops
    /// and the galley console, because a label under a fixture is the one thing this deck's crude grid cannot
    /// draw twice.</summary>
    public static DeckReachability.Point CantinaLabelStation
    {
        get
        {
            Room r = TheCantina;
            return new(r.X1 - 3.5, ((r.Y0 + r.Y1) / 2.0) + 0.9);
        }
    }
}
