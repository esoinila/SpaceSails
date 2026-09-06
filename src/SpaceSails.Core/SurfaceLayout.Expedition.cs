namespace SpaceSails.Core;

/// <summary>Part of <see cref="SurfaceLayout"/> (the header note lives in SurfaceLayout.cs) — #370 · THE
/// AWAY-EXPEDITION SITES, the special outdoors the owner's away-team gigs park next to: mystical ruins, a
/// crash-landed hull, a previously sealed piece of tunnel. Three authored schemes, one per
/// <see cref="ExpeditionSiteKind"/>, each visibly its own ground and none of them Miranda, Luna or seeded
/// rubble. The client calls <see cref="ForExpedition"/> instead of <c>For</c> when the excursion is an
/// expedition; the fence, tube and tracker laws stay the caller's shared law. #585's buildings go in the
/// empty flanks through the same shared ledger, so they cannot grow into a signature or into each
/// other.</summary>
public static partial class SurfaceLayout
{
    // ── #370 · THE AWAY-EXPEDITION SITES. The special outdoors the owner's away-team gigs park next to
    //    (issue #370: "some dig site … mystical ruins or structures, crashlanded ships … a previously
    //    sealed piece of tunnel"). Three AUTHORED schemes, one per <see cref="ExpeditionSiteKind"/>, each
    //    visibly its own ground and distinct from Miranda/Luna/the seeded rubble — an homage to
    //    Alien/Prometheus energy, never a reproduction. The client calls this instead of For() when the
    //    excursion is an expedition; the fence/tube/tracker laws stay the caller's shared law. ──────────
    /// <summary>Lay out an away-expedition site's ground for its <paramref name="kind"/>. Authored, pure,
    /// and clamped inside the field's safe span exactly like every other scheme, so the way down always
    /// exists and the edge lanes stay open.</summary>
    /// <summary>#585 · The away-expedition grounds. Owner, after the Miranda rebuild: <i>"we should take
    /// these upgrades to all our outside scenes now. The biggest is the real spaces with doors... that is
    /// the place to find stuff. And clues."</i>
    ///
    /// <para>These three were exactly where Miranda was two days ago: walls and a landmark, no doorways, no
    /// buildings, nothing to walk INTO. They are authored grounds, which is precisely why they were missed —
    /// the same trap as canon site 0, which bypassed the generator and so never received a single one of the
    /// improvements everything else got (<i>"no real buildings and one-thick walls still"</i>).</para>
    ///
    /// <para>Each authored signature is untouched — the henge, the hull, the tomb are canon. The buildings go
    /// in the empty flanks around them, through the SAME shared ledger, so they cannot grow into the
    /// signature, into each other, or into a shelter.</para></summary>
    public static Plan ForExpedition(ExpeditionSiteKind kind, in Field field) => kind switch
    {
        ExpeditionSiteKind.CrashedHull => CrashedHull(field, AwayKeepOuts(kind, field)),
        ExpeditionSiteKind.SealedTunnel => SealedTunnel(field, AwayKeepOuts(kind, field)),
        _ => MysticalRuins(field, AwayKeepOuts(kind, field)),
    };

    /// <summary>#585 · Everything already spoken for on an away ground, worked out from the KIND alone.
    ///
    /// <para>This started as a parameter and the standing guards killed it in one run: the public
    /// <see cref="ForExpedition"/> is called directly by the tests, the region builder and the labs, so a
    /// keep-out list handed in only by the routed path meant two callers building two different grounds —
    /// the very failure this week has been about, committed while fixing it.</para>
    ///
    /// <para>The way out is that a kind DOES name its body: an away rock's id is
    /// <see cref="ExpeditionSite.BodyIdFor"/>, a pure function of the kind. So the ground can look up its own
    /// shelters and its own hidden chamber without being told, and every caller gets the identical ground.
    /// One function, one answer, no parameter to forget.</para></summary>
    private static System.Collections.Generic.List<(double X, double Y, double R)> AwayKeepOuts(
        ExpeditionSiteKind kind, in Field field)
    {
        string rock = ExpeditionSite.BodyIdFor(kind);
        var all = StandingClaims(rock, "", field);
        return WithRoomsToCome(kind, field, all);
    }

    /// <summary>#585 · The ground an away site's SEALED ROOMS will occupy once they are forced open.
    ///
    /// <para>An expedition ground is not finished when this layout returns it: <see cref="ExpeditionRegions"/>
    /// appends a room behind each sealed door as the captain opens it. Those rooms are laid at fixed places,
    /// and the moment these grounds gained buildings a building could be standing exactly there — which the
    /// standing guard reports as <i>"a region wall crosses the base geography"</i>, i.e. a room opening into
    /// somebody's wall.</para>
    ///
    /// <para>So the rooms are reserved BEFORE anything is placed, exactly like the shelters. Same rule as
    /// everywhere else on this ground: something that will be there is something that is there.</para></summary>
    private static System.Collections.Generic.List<(double X, double Y, double R)> WithRoomsToCome(
        ExpeditionSiteKind kind, in Field field,
        System.Collections.Generic.List<(double X, double Y, double R)>? keepOut)
    {
        var all = new System.Collections.Generic.List<(double X, double Y, double R)>(keepOut ?? []);
        foreach (ExpeditionRegions.SealedDoor door in ExpeditionRegions.AllDoors(kind, field))
        {
            ExpeditionRegions.Region room = ExpeditionRegions.ForceOpen(kind, door.Id, field);
            double cx = (room.MinX + room.MaxX) / 2, cy = (room.MinY + room.MaxY) / 2;
            double halfW = (room.MaxX - room.MinX) / 2, halfH = (room.MaxY - room.MinY) / 2;
            all.Add((cx, cy, System.Math.Sqrt((halfW * halfW) + (halfH * halfH))));

            // And the door itself, so nothing is built across the way IN to the room.
            all.Add((door.X, door.Y, 6.0));
        }
        return all;
    }

    // Mystical ruins — a HENGE: a ring of standing-stone slabs around a central altar, with no box maze.
    private static Plan MysticalRuins(in Field f, System.Collections.Generic.List<(double X, double Y, double R)>? keepOut = null)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        // Eight standing stones on a circle of radius ~10 du around the anchor (each a small solid slab).
        const int stones = 8;
        const double ring = 10.0;
        for (int i = 0; i < stones; i++)
        {
            double a = (2.0 * System.Math.PI * i) / stones;
            double sx = ax + (ring * System.Math.Cos(a));
            double sy = ay + (ring * System.Math.Sin(a));
            AddClampedBox(walls, f, sx - 1.1, sy - 1.1, sx + 1.1, sy + 1.1, hull: true);
        }

        // The central altar — a small freestanding hull slab at the heart.
        AddClampedBox(walls, f, ax - 1.6, ay - 1.4, ax + 1.6, ay + 1.4, hull: true);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 3, "⟁ THE STANDING STONES") };
        // #585 · REAL SPACES WITH DOORS, out in the flanks the henge never occupied. Owner:
        // "The biggest is the real spaces with doors... that is the place to find stuff. And clues." An
        // authored signature is something to LOOK at; a room with a threshold is somewhere to go.
        AddOutlyingStructures(walls, doorways, centres, f, "expedition:henge", ax, ay, keepOut, footprints);

        return new Plan("THE STANDING STONES", walls, marks, doorways, centres, footprints);
    }

    // Crash-landed ship — a long TORN FUSELAGE half-buried up the field: the hull outline as an open box
    // with the port side blown out (the tear you walk in through), plus a few internal ribs. No ring, no
    // rails — reads as a wreck.
    private static Plan CrashedHull(in Field f, System.Collections.Generic.List<(double X, double Y, double R)>? keepOut = null)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        // The fuselage: a tall open box (deep→shallow), left side torn away (gapSide 2 = left open).
        AddOpenBox(walls, f, cx: ax, cy: ay + 8, w: 9, h: 30, gapSide: 2);
        // The nose: a solid crumpled block at the deep end.
        AddClampedBox(walls, f, ax - 3, ay - 8, ax + 3, ay - 4, hull: true);
        // Internal ribs — a few short cross-spans inside the hull (bulkhead frames), open ended.
        AddClampedSpan(walls, f, ax, ay + 2, 6, horizontal: true, hull: false);
        AddClampedSpan(walls, f, ax, ay + 12, 6, horizontal: true, hull: false);
        AddClampedSpan(walls, f, ax, ay + 20, 6, horizontal: true, hull: false);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 9, "⛢ THE CRASHED HULL") };
        // #585 · REAL SPACES WITH DOORS, out in the flanks the wreck never occupied. Owner:
        // "The biggest is the real spaces with doors... that is the place to find stuff. And clues." An
        // authored signature is something to LOOK at; a room with a threshold is somewhere to go.
        AddOutlyingStructures(walls, doorways, centres, f, "expedition:hull", ax, ay, keepOut, footprints);

        return new Plan("THE CRASHED HULL", walls, marks, doorways, centres, footprints);
    }

    // The owner's Fate-system anecdote made ground: a charge arc holed the rock and revealed a SEALED
    // TUNNEL of habitants ejected in a violent event, dead there. Two long parallel tunnel walls run deep
    // from a breach at the top, cross-bulkheads rung between them, and a chamber (the tomb) at the deep end.
    private static Plan SealedTunnel(in Field f, System.Collections.Generic.List<(double X, double Y, double R)>? keepOut = null)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        double tunTop = ay + 22, tunDeep = ay - 2;
        double leftWall = ax - 4, rightWall = ax + 4;
        // The two tunnel walls (solid hull), running deep from the breach; the breach itself is the open
        // top (no wall closes it), so you enter from the field into the shaft.
        AddClampedSpan(walls, f, leftWall, (tunTop + tunDeep) / 2, tunTop - tunDeep, horizontal: false, hull: true);
        AddClampedSpan(walls, f, rightWall, (tunTop + tunDeep) / 2, tunTop - tunDeep, horizontal: false, hull: true);
        // Cross-bulkheads (rungs) — short open spans between the walls, staggered, dead-end flavour.
        AddClampedSpan(walls, f, ax, ay + 16, 8, horizontal: true, hull: false);
        AddClampedSpan(walls, f, ax, ay + 6, 8, horizontal: true, hull: false);
        // The tomb chamber at the deep end — a small open box (one side breached).
        AddOpenBox(walls, f, cx: ax, cy: ay - 6, w: 12, h: 6, gapSide: 1);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 6, "⌸ THE SEALED TOMB") };
        // #585 · REAL SPACES WITH DOORS, out in the flanks the tomb mouth never occupied. Owner:
        // "The biggest is the real spaces with doors... that is the place to find stuff. And clues." An
        // authored signature is something to LOOK at; a room with a threshold is somewhere to go.
        AddOutlyingStructures(walls, doorways, centres, f, "expedition:tomb", ax, ay, keepOut, footprints);

        return new Plan("THE SEALED TUNNEL", walls, marks, doorways, centres, footprints);
    }
}
