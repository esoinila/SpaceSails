namespace SpaceSails.Core;

/// <summary>
/// Sunday-morning wind · #1–#2 (owner, 2026-07-19, verbatim): <b>"Earth Moon and Miranda out-doors were
/// extremely similar maps. For Moon we should come up with something different… at least the walls of
/// buildings should not be the same layout."</b> And: the other shuttle destinations that can have an
/// outdoors should get their own too. Today every landable body's walked surface reuses ONE geometry
/// (the monolith maze); this is the pure, deterministic generator that gives each body its OWN ground.
///
/// <para>The surface LAWS stay shared and live in the client's <c>MoonSurface</c> (the landing band at
/// the top, the tube mouth, the Reever barrier, the deep field + its tide spawn edge and home range).
/// Only the GEOGRAPHY varies here: the interior ruin/maze walls and the deep landmark, laid out inside
/// the shared field envelope the caller passes in. Two mechanisms, blended:</para>
/// <list type="bullet">
/// <item><b>Authored signatures</b> for the bodies with character — Miranda keeps THE MONOLITH maze
/// (it is canon), and Luna gets the mass-driver ruins (worldbuilding §1: the lunar mass drivers), a
/// visibly different scheme of long parallel launch rails and strip foundations, never a box maze.</item>
/// <item><b>A seeded signature</b> (deterministic per body id, off the one shared <see cref="DiceRule"/>
/// engine — never <see cref="System.Random"/> or the clock) for every other landable body, so each new
/// outdoors differs by construction.</item>
/// </list>
///
/// <para>Walls are collision LAW for everyone — the captain and the Old Ones (<see cref="ReeverChase"/>)
/// bump-and-slide on the same segments (#324) — so the geography is generated in Core where a test can
/// pin it: that Luna ≠ Miranda, that the seeded ground is deterministic, and that every scheme leaves a
/// walkable corridor from the tube mouth down to the deep field (features never seal the field's width;
/// the far-left and far-right regolith lanes are always kept open, so a way down always exists).</para>
/// </summary>
public static class SurfaceLayout
{
    /// <summary>A generated interior wall in deck units. <paramref name="IsHull"/> marks a solid opaque
    /// face (a landmark's own slab, the mass-driver muzzle) versus an open ruin/maze wall; the client
    /// maps both onto its collidable <c>DeckPlan.Wall</c>, so both stop a boot and a shamble alike.</summary>
    public readonly record struct Wall(double X1, double Y1, double X2, double Y2, bool IsHull);

    /// <summary>A deep-field landmark to label on the ground: its glyph-tagged text at (X, Y).</summary>
    public readonly record struct Landmark(double X, double Y, string Label);

    /// <summary>The shared field envelope the geography is laid inside — the LAWS, handed in from the
    /// client's <c>MoonSurface</c> so Core carries no client geometry constants yet lays ground within
    /// the sane bounds. <paramref name="AnchorX"/>/<paramref name="AnchorY"/> is the deep commitment
    /// area's centre (the old monolith spot), the heart every scheme dresses differently.</summary>
    public readonly record struct Field(
        double LeftX, double RightX, double TopY, double BottomY,
        double LandingBandY, double AnchorX, double AnchorY);

    /// <summary>One body's ground: a scheme name (for the deep-area location line and tests), the
    /// interior walls, and the deep landmark(s). The fence, tube, doors, kiosk and the way home are the
    /// caller's shared law — this is only what makes the body's geography its own.</summary>
    public readonly record struct Plan(
        string Scheme, IReadOnlyList<Wall> Walls, IReadOnlyList<Landmark> Landmarks,
        // #573 · Every opening the ground's buildings carry, as the SEGMENT across it. Owner, walking past
        // them: "there seemed to be shelter like spaces that were just missing the services and the doors."
        // They had openings all along — the generator simply threw them away, so MoonSurface had nothing to
        // hang a door on and a building read as an unfinished shelter rather than a ruin somebody left.
        IReadOnlyList<Doorway>? Doorways = null,
        // #573 · The middle of each building this ground laid, in the same order they were built — so a
        // caller can put something INSIDE one. Without this the client knew a ruin's walls and had no idea
        // where its floor was.
        IReadOnlyList<(double X, double Y)>? BuildingCentres = null,
        // #585 · How much ground each building actually occupies (centre + rotation-proof radius), in the
        // same order. Owner, on a site whose structures had merged: "check this structure out... it
        // functions but is kind of funny". Publishing the real footprint is what lets a guard ask the plan
        // whether anything is standing inside anything else, instead of the guard GUESSING a radius — which
        // is just the same two-sources-of-truth bug wearing a test's clothes.
        IReadOnlyList<(double X, double Y, double R)>? BuildingFootprints = null);

    /// <summary>An opening through a building's wall, given as the segment across it so a caller can hang a
    /// real door on it rather than guessing which way the passage runs.</summary>
    public readonly record struct Doorway(double X1, double Y1, double X2, double Y2);

    /// <summary>#573 · THE FIELD ENVELOPE, in Core, as the single source of truth.
    ///
    /// <para>It lived as constants in the client's <c>MoonSurface</c>, and every test and lab that needed it
    /// kept its own hand-copied duplicate. So when the field grew sixteenfold, the client shipped a
    /// 310 x 260 du world while <c>SurfaceReachabilityTests</c> and Lab 43 went on auditing and drawing the
    /// old 78 x 64 one — and passed, and printed confident numbers about a world that no longer existed.</para>
    ///
    /// <para>That is the same drift this project keeps paying for, one level up from geometry: a test that
    /// MIRRORS a constant instead of READING it is not testing the thing that ships. One copy now; the
    /// client reads it too.</para></summary>
    public static Field DefaultField { get; } = new(
        LeftX: -160, RightX: 150, TopY: -20, BottomY: -280,
        LandingBandY: -27, AnchorX: -6, AnchorY: -232);

    /// <summary>The safe half-lane kept open at each far edge of the field — no generated feature ever
    /// intrudes here, so a walk-around always exists and the deep is always reachable from the top.</summary>
    public const double EdgeMargin = 10.0;

    /// <summary>How much ground a SEEDED body's deep landmark keeps to itself — the cleared space around the
    /// fixture at the anchor, off-limits to huts and rubble alike.
    ///
    /// <para>#649: this used to be spelled <c>Monolith.ApronRadius</c>, which was fine only for as long as
    /// nobody changed it. The monolith stands on exactly one ground and the seeded generator lays every
    /// ground that is not it, so the owner's <i>"make it bigger"</i> would have moved the buildings on eight
    /// moons that have never seen the thing. Bug class 2 — a constant governing the wrong thing — caught
    /// before it could fire.</para></summary>
    public const double AnchorReserveRadius = 15.0;

    /// <summary>Lay out one landable body's ground. Miranda and Luna are authored; everything else is
    /// seeded deterministically from its id, so no two grounds are the same by construction.</summary>
    public static Plan For(string bodyId, in Field field) => For(bodyId, field, null);

    /// <summary>#585 · WHERE THE SHELTERS ALREADY STAND, so nothing else is laid on top of one.
    ///
    /// <para>Owner, on a site where the buildings had grown together: <i>"check this structure out... it
    /// functions but is kind of funny"</i>. It was funny because THREE placers wrote into this one field and
    /// none could see the others — the seeded features kept a claim ledger, the outlying buildings kept a
    /// separate and much weaker one, and <see cref="SurfaceShelter"/> kept none at all, so a life-critical
    /// pressure drum could be dropped straight through somebody's hut.</para>
    ///
    /// <para>The shelters are claimed FIRST and are never yielded, because a shelter is the answer to the
    /// air mechanic: a hut that has to move is a cosmetic loss, a shelter that has to move is a captain who
    /// dies looking for it. Everything else places around them.</para></summary>
    private static System.Collections.Generic.List<(double X, double Y, double R)> ShelterKeepOuts(
        string bodyId, string? siteSalt, in Field field)
    {
        var list = new System.Collections.Generic.List<(double X, double Y, double R)>();
        foreach (SurfaceStructure.Spec spec in SurfaceShelter.SpecsFor(bodyId ?? "", siteSalt ?? "", field))
        {
            list.Add((spec.CentreX, spec.CentreY, SurfaceStructure.KeepOutRadius(spec)));
        }

        // #585 · And the hidden lab's chamber, which is appended at runtime from a seeded door. Reserved on
        // every body whether or not this one hides one — the door spot is seeded the same way regardless, so
        // it costs a building's worth of ground and means a lab can never open into somebody's wall.
        list.Add(SecretLab.ChamberFootprint(bodyId ?? "", field, siteSalt));
        return list;
    }

    /// <summary>#320 · Lay out a body's ground for a chosen LANDING SITE (<see cref="LandingSites"/>). An
    /// EMPTY salt is the body's canon site 0 — the authored/seeded signature, byte-for-byte the same ground
    /// as <see cref="For(string, in Field)"/> (so Miranda's monolith maze and Luna's rails are preserved).
    /// A non-empty salt is a secondary site: the ground is re-seeded off <c>(bodyId ~ salt)</c>, giving a
    /// visibly different wing/feature layout on the SAME body — different site, different deck-plan. An
    /// away-expedition rock keeps its authored per-kind ground regardless of salt (those gigs are single
    /// authored sites, never a seeded board).</summary>
    public static Plan For(string bodyId, in Field field, string? siteSalt)
    {
        // #585: the shelters for THIS body and THIS site — computed from the real (bodyId, siteSalt) pair,
        // never from the combined seeded key, because that pair is what SurfaceShelter itself is keyed on.
        // Getting this wrong would keep the ground clear of shelters that are somewhere else entirely.
        // #585 · An away gig is a SINGLE authored site — "ExpeditionSite_IgnoresSalt" is a standing law — so
        // its keep-outs are taken with an EMPTY salt too. Handing the real salt in would have made the ground
        // move with it, quietly breaking that law the moment these grounds gained buildings. The guard caught
        // it; this is what it was guarding.
        // #585 · ONE EXPEDITION GROUND, ONE ANSWER. This briefly handed shelter keep-outs down here, and the
        // standing guards caught it immediately: the PUBLIC ForExpedition(kind, field) — which tests, the
        // region builder and the labs all call — takes no keep-outs, so the routed path was building a
        // DIFFERENT ground from the one everything else measured. Two sources of truth for one ground, which
        // is the exact failure this whole week has been about, introduced while fixing it.
        //
        // An away gig is a single authored site keyed on its KIND, and a kind cannot name a body, so it
        // cannot know where that body's shelters stand. So it reserves what it CAN know — the rooms its own
        // sealed doors will open (see WithRoomsToCome) — and nothing else. The shelter overlap on these
        // grounds is covered by the audit instead.
        if (ExpeditionSite.TryParseKind(bodyId, out ExpeditionSiteKind kind))
        {
            return ForExpedition(kind, field);
        }

        var keepOut = ShelterKeepOuts(bodyId ?? "", siteSalt, field);
        if (string.IsNullOrEmpty(siteSalt))
        {
            return (bodyId ?? "") switch
            {
                FalseSlab.BodyId => Miranda(field, keepOut),
                // #649 · Phobos is AUTHORED now, because it is where the monolith stands. It used to fall
                // through to the seeded rubble generator while the map cards in every captain's pocket had
                // been pacing off "the monolith" on this moon since #164.
                Monolith.BodyId => Phobos(field, keepOut),
                "luna" => Luna(field, keepOut),
                _ => Seeded(bodyId ?? "", field, keepOut),
            };
        }
        return Seeded($"{bodyId ?? ""}~{siteSalt}", field, keepOut);
    }

    /// <summary>A stable order-independent hash of a plan's wall set — the test's "Luna ≠ Miranda"
    /// ground-truth handle (owner: the walls of buildings must not be the same layout), and a cheap way
    /// for any caller to tell two grounds apart.</summary>
    public static long WallHash(Plan plan)
    {
        unchecked
        {
            long acc = 1469598103934665603L;
            foreach (Wall w in plan.Walls)
            {
                // Quantise to 0.1 du so float noise never flips the hash, then fold each endpoint in an
                // order-independent way (sum of per-wall hashes) so wall list order can't matter.
                long h = 17;
                h = (h * 31) + Q(w.X1); h = (h * 31) + Q(w.Y1);
                h = (h * 31) + Q(w.X2); h = (h * 31) + Q(w.Y2);
                h = (h * 31) + (w.IsHull ? 1 : 0);
                acc += h;
            }
            acc = (acc * 31) + plan.Walls.Count;
            return acc;
        }
    }

    private static long Q(double v) => (long)System.Math.Round(v * 10.0);

    // ── Miranda — the maze (canon, owner's #313). Reproduced exactly from the original hand-built
    //    geometry: concentric gapped corridor rows the Old Ones exploit to corner a dawdler, two spurs,
    //    and the freestanding slab at the heart. This is the ground that must NOT change.
    //
    //    #649 · The slab at the heart is THE FALSE SLAB now, not the monolith: the owner reserved that word
    //    for the one object, and it stands on Phobos, where every treasure map has paced off it since #164.
    //    Every number below is the number that was there yesterday (FalseSlab's constants are the slab's own
    //    former values), so this ground generates byte-for-byte the walls it generated before. Only the NAME
    //    and the CARD moved — the geometry the owner authored is untouched, as it must be. ──
    private static Plan Miranda(in Field f, System.Collections.Generic.List<(double X, double Y, double R)> keepOut)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        double left = ax - 18, right = ax + 18;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        AddGappedRow(walls, left, right, ay + 12, ax + 10, 3);
        AddGappedRow(walls, left, right, ay + 6, ax - 11, 3);
        AddGappedRow(walls, left, right, ay - 4, ax + 9, 3);
        walls.Add(new(ax - 6, ay + 12, ax - 6, ay + 6, false));
        walls.Add(new(ax + 4, ay + 6, ax + 4, ay - 4, false));
        // #586 / #649 · THE SLAB AT THE HEART OF THE MAZE. A wall of stacked courses inside the cell the
        // canon maze rows leave for it — the maze is canon and every row above stays exactly where it was
        // authored. Its numbers are FalseSlab's own now, so a change to the monolith's scale (which the
        // owner wants to be enormous) can never quietly resize somebody else's corridor.
        AddSolidMass(walls, ax - FalseSlab.HalfWidth, ay - FalseSlab.HalfHeight,
            ax + FalseSlab.HalfWidth, ay + FalseSlab.HalfHeight, hull: true);

        // The four approach stubs, just inside the apron: the remains of something that was walked to on
        // purpose. Small, solid, and unmistakably PLACED — the difference between a rock and a ruin.
        foreach ((double mx, double my) in new[]
        {
            (ax, ay + FalseSlab.MarkerRing), (ax, ay - FalseSlab.MarkerRing),
            (ax - FalseSlab.MarkerRing, ay), (ax + FalseSlab.MarkerRing, ay),
        })
        {
            AddSolidMass(walls, mx - FalseSlab.MarkerHalf, my - FalseSlab.MarkerHalf,
                mx + FalseSlab.MarkerHalf, my + FalseSlab.MarkerHalf, hull: true);
        }

        // #563 · THE CANON GROUND GETS BUILDINGS TOO. Owner, standing on it: "no real buildings and
        // one-thick walls still" — because site 0 is AUTHORED and routes here, bypassing the seeded
        // generator where the structures live. So the flagship ground, the one carrying the story and the
        // one every captain lands on by default, was the only one that never got the new content: Lab 43
        // measured it at 12 wall segments over 12% of the field, the emptiest site on the moon.
        //
        // The maze itself is untouched — it is canon and stays exactly as authored. These stand OUT in the
        // empty flanks and shallows the maze never occupied, which is most of the field.
        AddOutlyingStructures(walls, doorways, centres, f, "miranda", ax, ay, keepOut, footprints);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 3, FalseSlab.ConsoleLabel) };
        return new Plan(FalseSlab.Scheme, walls, marks, doorways, centres, footprints);
    }

    // ── Phobos — THE MONOLITH (#164 / #586 / #649). The one object, on the one ground, on the moon whose
    //    name has been printed on every treasure map since the first one was minted.
    //
    //    Owner's ruling (worldbuilding §8): it must NOT sit in a fenced little plot with the set dressing
    //    around it — "whatever ground carries it has to be open enough that the object IS the horizon, not a
    //    prop in a room." So this scheme is defined as much by what it does not lay as by what it does:
    //    there is no maze, no corridor rows, no ruin field between you and it. Open regolith from the
    //    landing band all the way down, and the thing standing in it. The buildings this ground gets are
    //    AddOutlyingStructures' four, which are held clear of the signature by construction and end up out
    //    in the flanks — a rim camp on the edge of a plain, not a courtyard.
    //
    //    What the slab and its ceremony actually MEASURE is #649's second half (the scale pass) and is not
    //    decided here: every number below is read from Monolith, which is the one source. ──
    private static Plan Phobos(in Field f, System.Collections.Generic.List<(double X, double Y, double R)> keepOut)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        // THE MONOLITH. One solid mass, clamped into the field like every other authored signature so the
        // edge lanes stay open and a way down always exists however large it grows.
        AddClampedSolidMass(walls, f, ax - Monolith.HalfWidth, ay - Monolith.HalfHeight,
            ax + Monolith.HalfWidth, ay + Monolith.HalfHeight, hull: true);

        // The four stubs at the compass points, just inside the swept apron: the remains of an approach.
        // Small, solid, and unmistakably PLACED — the difference between a rock and a ruin. They are the
        // only other made thing within sight of it.
        foreach ((double mx, double my) in new[]
        {
            (ax, ay + Monolith.MarkerRing), (ax, ay - Monolith.MarkerRing),
            (ax - Monolith.MarkerRing, ay), (ax + Monolith.MarkerRing, ay),
        })
        {
            AddClampedSolidMass(walls, f, mx - Monolith.MarkerHalf, my - Monolith.MarkerHalf,
                mx + Monolith.MarkerHalf, my + Monolith.MarkerHalf, hull: true);
        }

        // #563 · Real buildings with real doors, the same as every other ground gets — but only out in the
        // flanks. AddOutlyingStructures refuses to place anything near the signature, which is exactly the
        // "not a boxed backyard" rule expressed as code rather than as a wish.
        AddOutlyingStructures(walls, doorways, centres, f, Monolith.BodyId, ax, ay, keepOut, footprints);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 3, Monolith.ConsoleLabel) };
        return new Plan(MonolithScheme, walls, marks, doorways, centres, footprints);
    }

    /// <summary>The location line the deep area reads on the monolith's ground. Named for the place, not the
    /// object: the Stickney rim is the real feature the real 85 m boulder sits on
    /// (<see cref="Landmarks.PhobosMonolith"/>), and the house rule holds — the PLACE is real, what happens
    /// in its shadow is ours.</summary>
    public const string MonolithScheme = "THE STICKNEY RIM";

    // ── Luna — the MASS-DRIVER RUINS (worldbuilding §1: the lunar mass drivers). A visibly different
    //    scheme (owner: "come up with something different… at least the walls of buildings should not be
    //    the same layout"): NO box maze. Instead the wreck of the old launcher — a long twin launch RAIL
    //    running up the field (broken into staggered segments so you weave lane to lane), the muzzle
    //    block at the deep head, and a scatter of rectangular STRIP FOUNDATIONS (the factory footings)
    //    that read as strips, not cells. The rails sit in the central band; the field's flanks stay open
    //    regolith, so combing the ruins is a very different walk from Miranda's concentric maze. ──
    private static Plan Luna(in Field f, System.Collections.Generic.List<(double X, double Y, double R)> keepOut)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        // The twin launch rail: two parallel lines running up-field from the deep head, each broken into
        // three segments with OFFSET gaps so the lanes cross-connect (a walker weaves through the breaks).
        double railTop = ay + 26, railBot = ay - 4;
        double leftRail = ax - 3.5, rightRail = ax + 3.5;
        AddBrokenVertical(walls, leftRail, railBot, railTop, gapAt: ay + 4, gapHalf: 3);
        AddBrokenVertical(walls, leftRail, railBot, railTop, gapAt: ay + 18, gapHalf: 3);
        AddBrokenVertical(walls, rightRail, railBot, railTop, gapAt: ay + 11, gapHalf: 3);
        // Cross-ties between the rails (the sleepers), a couple of short rungs — dead-end flavour.
        walls.Add(new(leftRail, ay + 22, rightRail, ay + 22, false));
        walls.Add(new(leftRail, ay + 8, rightRail, ay + 8, false));

        // The muzzle: a solid launch head block at the deep end, OFFSET to port (not centred like the
        // monolith) — the mass driver fired that way.
        AddBox(walls, ax - 6.5, ay - 8, ax - 1.5, ay - 4, hull: true);

        // Strip foundations: the factory footings, each two long parallel low walls (a strip outline,
        // open ended), staggered left and right up the deep field. Kept inside the edge margins.
        AddStrip(walls, f, cx: ax - 16, cy: ay + 4, len: 12, gap: 3);
        AddStrip(walls, f, cx: ax + 14, cy: ay + 14, len: 10, gap: 3);
        AddStrip(walls, f, cx: ax - 13, cy: ay + 20, len: 9, gap: 2.5);

        AddOutlyingStructures(walls, doorways, centres, f, "luna", ax, ay, keepOut, footprints);   // #563: the authored ground gets buildings too

        var marks = new System.Collections.Generic.List<Landmark>
        {
            new(ax - 4, ay - 9, "⛓ MASS-DRIVER MUZZLE"),
            new(ax + 14, ay + 17, "▭ STRIP FOUNDATIONS"),
        };
        return new Plan("THE MASS-DRIVER RUINS", walls, marks, doorways, centres, footprints);
    }

    // ── Every other landable body — a SEEDED signature. A deterministic scatter of ruin blocks and
    //    broken arcs across the deep field, salted per body id off the shared dice engine, so each new
    //    outdoors differs by construction while always leaving the flanks open (pathability by design).
    //    Miranda and Luna never reach here; this serves phobos, europa, ganymede, callisto, titan,
    //    enceladus and any future landable body. ──
    private static Plan Seeded(string bodyId, in Field f, System.Collections.Generic.List<(double X, double Y, double R)> keepOut)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        // The safe span features may occupy — inside the kept-open edge lanes.
        double minX = f.LeftX + EdgeMargin, maxX = f.RightX - EdgeMargin;
        double minY = f.BottomY + 4, maxY = f.LandingBandY - 6;

        // #573 · SCALED TO THE FIELD. This was a flat 5..9, sized for a 78 x 64 du field; dropping the same
        // handful into a field sixteen times the area would have made "more explorable space" read as a
        // bigger emptiness. One feature per ~700 du^2, so density holds however the field is sized.
        double area = (maxX - minX) * (maxY - minY);
        int features = System.Math.Clamp(5 + (int)(area / 700.0), 5, 90) + Face(bodyId, "count", 5);

        // #563 · FEATURES MAY NOT BE LAID ON TOP OF ONE ANOTHER. Harmless while every shape was an open
        // span or a U — two overlapping rubble walls are just messier rubble. The moment buildings arrived
        // it stopped being harmless: a second feature's wall laid across a doorway SEALS the room behind
        // it, and the reachability flood caught exactly that on four sites (21-29 du^2 of interior nobody
        // could reach). So each placement claims a footprint with a little elbow room, and anything that
        // would land on a claim is skipped rather than squeezed — a slightly emptier field beats a building
        // you can see into and never enter.
        // Elbow room is a GAP between footprints, not a exclusion zone: 3 du rejected so much that the
        // Ridge Camp fell from 25 segments to 10 and the "more interesting landscape" came out emptier than
        // before. 1.5 is enough to keep one feature's wall off another's doorway.
        var claimed = new System.Collections.Generic.List<(double X0, double Y0, double X1, double Y1)>();
        const double Elbow = 1.5;

        bool Claim(double cx, double cy, double halfW, double halfH)
        {
            (double x0, double y0, double x1, double y1) =
                (cx - halfW - Elbow, cy - halfH - Elbow, cx + halfW + Elbow, cy + halfH + Elbow);
            foreach ((double ax0, double ay0, double ax1, double ay1) in claimed)
            {
                if (x0 < ax1 && x1 > ax0 && y0 < ay1 && y1 > ay0)
                {
                    return false;
                }
            }
            claimed.Add((x0, y0, x1, y1));
            return true;
        }

        // #585 · RECORDING IS NOT ASKING. Reserve puts a footprint in the ledger unconditionally, for things
        // that are ALREADY STANDING and are not up for negotiation.
        //
        // The distinction cost a site. The shelters were pre-claimed through Claim() above — which REJECTS on
        // overlap — so two shelters that are legally placed (52 du apart, their own rule) but whose square
        // claim boxes happened to touch knocked each other out: the second Claim returned false and never
        // recorded, leaving that shelter invisible to the ledger and the ground under it free for a building.
        // The audit caught exactly that on luna/The Shadowed Rille, where a hut ended up 21.5 du from a
        // shelter needing 31.3.
        //
        // Claim() asks "may I build here?". Reserve() says "something is here." Using the asking one for the
        // saying job is the same category error as sharing a console kind between two different doors.
        void Reserve(double cx, double cy, double halfW, double halfH) =>
            claimed.Add((cx - halfW - Elbow, cy - halfH - Elbow, cx + halfW + Elbow, cy + halfH + Elbow));

        // The deep landmark's own fixture is laid AFTER this loop but stands on real ground. Claim it first,
        // or a building can be seeded around the anchor and then have the fixture dropped across its door.
        //
        // #586: claimed at the CEREMONY's radius, not the fixture's. A deep landmark is not just the stone —
        // there is cleared ground around it — and a building seeded on that would put somebody's hut in the
        // middle of the one ceremonial space on the moon.
        //
        // #649 · This read Monolith.ApronRadius, which is bug class 2 in waiting: the monolith stands on ONE
        // ground and this function lays every ground that is NOT it, so growing the slab (which the owner has
        // asked for) would have silently moved the huts on eight moons that have never seen it. Same value,
        // its own name, no shared fate.
        Reserve(ax, ay, AnchorReserveRadius, AnchorReserveRadius);

        // #585 · AND THE SHELTERS, which are laid by SurfaceShelter on a completely separate pass and were
        // therefore invisible to this ledger — so a seeded feature could be dropped straight through a
        // pressure drum. They are claimed first and never yielded: a hut that has to move costs nothing, a
        // shelter that has to move costs a captain who walked to where the beacon said it was.
        foreach ((double sx, double sy, double sr) in keepOut ?? [])
        {
            Reserve(sx, sy, sr, sr);
        }

        for (int i = 0; i < features; i++)
        {
            double cx = Lerp(minX, maxX, Frac(bodyId, $"x:{i}"));
            double cy = Lerp(minY, maxY, Frac(bodyId, $"y:{i}"));
            double len = 5 + (7 * Frac(bodyId, $"len:{i}")); // 5..12 du
            int shape = Face(bodyId, $"shape:{i}", 4);        // 0..3
            bool horizontal = Frac(bodyId, $"rot:{i}") < 0.5;

            // Try a few seeded spots before abandoning a feature. Skipping on the first clash threw most of
            // the field away; a handful of retries keeps the ground full while still never overlapping.
            // Buildings are bigger than `len` (SurfaceStructure clamps them up to a workable size) and
            // carry thick walls, so they claim a footprint sized like the thing that will actually be laid.
            // #585: a BUILDING claims the footprint it will really stand on — the clamped centre and the
            // rotation-proof radius — instead of a flat 13 taken on trust. Rubble keeps its own span.
            bool placed;
            if (shape == 2)
            {
                (double bx, double by, double br) = StructureFootprint(f, cx, cy, len, bodyId, $"bld:{i}");
                placed = Claim(bx, by, br, br);
                for (int attempt = 1; attempt < 5 && !placed; attempt++)
                {
                    cx = Lerp(minX, maxX, Frac(bodyId, $"x:{i}:{attempt}"));
                    cy = Lerp(minY, maxY, Frac(bodyId, $"y:{i}:{attempt}"));
                    (bx, by, br) = StructureFootprint(f, cx, cy, len, bodyId, $"bld:{i}");
                    placed = Claim(bx, by, br, br);
                }
            }
            else
            {
                double claimHalf = len / 2;
                placed = Claim(cx, cy, claimHalf, claimHalf);
                for (int attempt = 1; attempt < 5 && !placed; attempt++)
                {
                    cx = Lerp(minX, maxX, Frac(bodyId, $"x:{i}:{attempt}"));
                    cy = Lerp(minY, maxY, Frac(bodyId, $"y:{i}:{attempt}"));
                    placed = Claim(cx, cy, claimHalf, claimHalf);
                }
            }
            if (!placed)
            {
                continue;
            }

            switch (shape)
            {
                case 0: // a bare rubble wall (a fallen span)
                    AddClampedSpan(walls, f, cx, cy, len, horizontal, hull: false);
                    break;
                case 1: // an L — two spans meeting at a corner (a collapsed room angle)
                    AddClampedSpan(walls, f, cx, cy, len, horizontal, hull: false);
                    AddClampedSpan(walls, f, cx, cy, len * 0.7, !horizontal, hull: false);
                    break;
                case 2: // a real BUILDING — thick walls, a doorway through the mass, seeded shape and angle
                    AddStructure(walls, doorways, centres, f, cx, cy, len, bodyId, $"bld:{i}", footprints);
                    break;
                default: // a small solid slab (an ancient spur / a plinth)
                    AddClampedBox(walls, f, cx - 1.4, cy - 1.4, cx + 1.4, cy + 1.4, hull: true);
                    break;
            }
        }

        // The deep landmark: a seeded ancient fixture near the anchor, with a glyph from a small palette.
        string[] glyphs = ["◭ ANCIENT SPUR", "⬡ SHATTERED DOME", "✶ SLAG FIELD", "⌂ COLLAPSED OUTPOST"];
        string glyph = glyphs[Face(bodyId, "glyph", glyphs.Length)];
        AddClampedBox(walls, f, ax - 2, ay - 2, ax + 2, ay + 2, hull: true); // the fixture's own footprint
        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 3, glyph) };

        return new Plan("THE DEEP RUINS", walls, marks, doorways, centres, footprints);
    }

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
        var all = ShelterKeepOuts(rock, "", field);
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

    // ── Builders. Every span is clamped into the field's safe span so no feature ever intrudes on the
    //    kept-open edge lanes — that is what guarantees a way down for the flood-fill test. ──

    private static void AddGappedRow(System.Collections.Generic.List<Wall> walls,
        double x1, double x2, double y, double gapCenter, double gapHalf)
    {
        walls.Add(new(x1, y, gapCenter - gapHalf, y, false));
        walls.Add(new(gapCenter + gapHalf, y, x2, y, false));
    }

    // A vertical line from y1 (deep) up to y2, broken by one gap centred at gapAt — the rail with a
    // washed-out sleeper section you weave through.
    private static void AddBrokenVertical(System.Collections.Generic.List<Wall> walls,
        double x, double y1, double y2, double gapAt, double gapHalf)
    {
        if (y1 < y2)
        {
            walls.Add(new(x, y1, x, gapAt - gapHalf, false));
            walls.Add(new(x, gapAt + gapHalf, x, y2, false));
        }
    }

    /// <summary>#586 · A box that is SOLID MASS, not a room with walls round it.
    ///
    /// <para>The monolith is a slab of stone. Drawn as four lines it has an INTERIOR, and on a crude grid an
    /// interior is somewhere a captain could stand if only they could get in — which the reachability audit
    /// correctly reports as sealed ground. At 2.4 x 5 du that cavity was under the guard's threshold and
    /// nobody noticed; widening the slab to something worth walking to made it 99 cells and the guard spoke
    /// up immediately, which is exactly what it is for.</para>
    ///
    /// <para>So a solid is hatched through, at closer than a captain's own width — the same trick
    /// <see cref="SurfaceStructure"/> already uses to make metres of piled regolith read as mass rather than
    /// as a suspiciously thick room. Nothing about the outline changes; the inside simply stops being a
    /// place.</para></summary>
    private static void AddSolidMass(System.Collections.Generic.List<Wall> walls,
        double x1, double y1, double x2, double y2, bool hull)
    {
        AddBox(walls, x1, y1, x2, y2, hull);

        // Closer than the captain's 1.4 du diameter, so no gap between hatches can ever hold a body.
        const double hatch = 1.1;
        double lo = System.Math.Min(x1, x2), hi = System.Math.Max(x1, x2);
        for (double x = lo + hatch; x < hi - 1e-9; x += hatch)
        {
            walls.Add(new(x, y1, x, y2, hull));
        }
    }

    private static void AddBox(System.Collections.Generic.List<Wall> walls,
        double x1, double y1, double x2, double y2, bool hull)
    {
        walls.Add(new(x1, y1, x2, y1, hull));
        walls.Add(new(x1, y2, x2, y2, hull));
        walls.Add(new(x1, y1, x1, y2, hull));
        walls.Add(new(x2, y1, x2, y2, hull));
    }

    private static void AddStrip(System.Collections.Generic.List<Wall> walls, in Field f,
        double cx, double cy, double len, double gap)
    {
        // Two long parallel walls (the strip's two footings), open at both ends.
        double x1 = System.Math.Max(f.LeftX + EdgeMargin, cx - len / 2);
        double x2 = System.Math.Min(f.RightX - EdgeMargin, cx + len / 2);
        walls.Add(new(x1, cy - gap / 2, x2, cy - gap / 2, false));
        walls.Add(new(x1, cy + gap / 2, x2, cy + gap / 2, false));
    }

    private static void AddClampedSpan(System.Collections.Generic.List<Wall> walls, in Field f,
        double cx, double cy, double len, bool horizontal, bool hull)
    {
        if (horizontal)
        {
            double x1 = System.Math.Max(f.LeftX + EdgeMargin, cx - len / 2);
            double x2 = System.Math.Min(f.RightX - EdgeMargin, cx + len / 2);
            walls.Add(new(x1, cy, x2, cy, hull));
        }
        else
        {
            double y1 = System.Math.Max(f.BottomY + 2, cy - len / 2);
            double y2 = System.Math.Min(f.LandingBandY - 2, cy + len / 2);
            walls.Add(new(cx, y1, cx, y2, hull));
        }
    }

    /// <summary>#563 · Scatter a few real buildings across an AUTHORED ground's empty parts, well clear of
    /// its signature. Miranda's maze and Luna's rails are canon and are not touched; what gets filled is the
    /// open regolith around them, which on Miranda is some 88% of the field.
    ///
    /// <para>Kept away from the deep anchor by <paramref name="anchorX"/>/<paramref name="anchorY"/>, so a
    /// structure can never crowd the monolith or the mass-driver muzzle — the thing you walked out there to
    /// see must stay the thing you see.</para></summary>
    private static void AddOutlyingStructures(
        System.Collections.Generic.List<Wall> walls,
        System.Collections.Generic.List<Doorway> doorways,
        System.Collections.Generic.List<(double X, double Y)> centres,
        in Field f, string bodyId, double anchorX, double anchorY,
        System.Collections.Generic.List<(double X, double Y, double R)>? keepOut = null,
        System.Collections.Generic.List<(double X, double Y, double R)>? footprints = null)
    {
        double minX = f.LeftX + EdgeMargin, maxX = f.RightX - EdgeMargin;
        double minY = f.BottomY + 4, maxY = f.LandingBandY - 6;
        const double ClearOfSignature = 26.0;

        // #585 · THE LEDGER THAT KNOWS ABOUT EVERYTHING. Owner, on a Miranda site: "check this structure
        // out... it functions but is kind of funny" — a shelter drum, an outlying hut and a maze fixture had
        // grown into one accidental mega-complex, with doorways opening onto another building's solid mass.
        //
        // Two mistakes, both here. This kept its OWN claim list, so it could not see the shelters at all.
        // And it tested a bare 30 du between CENTRES, which is only honest for a circle: AddStructure lays a
        // freely-rotated box up to 20 x 16 with walls up to 3 du thick, which sweeps a radius near 16 — so
        // two buildings 30 du apart genuinely overlap, and the generator was doing what it was told.
        //
        // Now it claims real radii (SurfaceStructure.KeepOutRadius, right at every angle) in a ledger that
        // starts with the shelters already in it, plus a little elbow so a wall never lands on a doorway.
        const double Elbow = 1.5;
        var claimed = new System.Collections.Generic.List<(double X, double Y, double R)>(keepOut ?? []);
        int placed = 0;

        for (int i = 0; i < 24 && placed < 4; i++)
        {
            double cx = Lerp(minX, maxX, Frac(bodyId, $"outly:x:{i}"));
            double cy = Lerp(minY, maxY, Frac(bodyId, $"outly:y:{i}"));
            double size = 8 + (4 * Frac(bodyId, $"outly:size:{i}"));

            // The spot and radius this building will ACTUALLY occupy, edge clamp included.
            (cx, cy, double radius) = StructureFootprint(f, cx, cy, size, bodyId, $"outly:{i}");

            // Never near the signature, and never on top of ANYTHING already standing.
            if (System.Math.Sqrt(((cx - anchorX) * (cx - anchorX)) + ((cy - anchorY) * (cy - anchorY))) < ClearOfSignature)
            {
                continue;
            }
            bool clash = false;
            foreach ((double px, double py, double pr) in claimed)
            {
                double gap = System.Math.Sqrt(((cx - px) * (cx - px)) + ((cy - py) * (cy - py)));
                clash |= gap < radius + pr + Elbow;
            }
            if (clash)
            {
                continue;
            }

            claimed.Add((cx, cy, radius));
            AddStructure(walls, doorways, centres, f, cx, cy, size, bodyId, $"outly:{i}", footprints);
            placed++;
        }
    }

    /// <summary>#563 · Place one of <see cref="SurfaceStructure"/>'s buildings on the ground: seeded shape,
    /// angle, door count and wall thickness, clamped so the whole footprint stays inside the safe span (a
    /// building clipped by the edge lane would lose the face its doorway was on and become a solid block).
    ///
    /// <para>Thickness is seeded 1.2..2.4 du — the owner's Greenland longhouse: on a cold world you build
    /// out of what is under your boots, and if the wall is also holding an atmosphere you build it fat.</para></summary>
    /// <summary>#585 · Where a structure of this size will ACTUALLY end up, and how much room it will take.
    ///
    /// <para>The last hiding place of the same bug. <see cref="AddStructure"/> clamps its centre inward so
    /// the walls stay off the edge lanes — and it did that AFTER the caller had already claimed the
    /// unclamped spot. So a building seeded near the rim was claimed in one place and built in another, and
    /// the ledger that was supposed to keep buildings apart was recording a position nothing stood at.</para>
    ///
    /// <para>Both placers now claim what this returns, so the ledger and the ground agree.</para></summary>
    private static (double X, double Y, double R) StructureFootprint(
        in Field f, double cx, double cy, double size, string bodyId, string tag)
    {
        SurfaceStructure.Spec spec = OrdinaryStructure(f, cx, cy, size, bodyId, tag);
        return (spec.CentreX, spec.CentreY, SurfaceStructure.KeepOutRadius(spec));
    }

    /// <summary>#606 · ONE description of an outlying building, asked twice.
    ///
    /// <para>This function and <see cref="AddStructure"/> used to hold the same four rolls and the same
    /// clamps side by side — the ledger's copy and the builder's copy of one fact, which is the failure this
    /// whole file is annotated with. They are the same sentence now, and <see cref="SurfaceStructure.Ordinary"/>
    /// is where "what a hut on this ground looks like" actually lives, so a lift head that wants to pass for
    /// one has something to ask instead of numbers to imitate.</para></summary>
    private static SurfaceStructure.Spec OrdinaryStructure(
        in Field f, double cx, double cy, double size, string bodyId, string tag)
    {
        SurfaceStructure.Spec spec = SurfaceStructure.Ordinary(
            cx, cy, size,
            thickFrac: Frac(bodyId, $"{tag}:thick"),
            angleFrac: Frac(bodyId, $"{tag}:angle"),
            doors: 1 + Face(bodyId, $"{tag}:doors", 2),
            shapeFace: Face(bodyId, $"{tag}:shape", 3));

        // Keep the whole thing (walls included) off the edge lanes.
        double halfW = (spec.Width / 2) + spec.WallThickness, halfH = (spec.Height / 2) + spec.WallThickness;
        return spec with
        {
            CentreX = System.Math.Clamp(cx, f.LeftX + EdgeMargin + halfW, f.RightX - EdgeMargin - halfW),
            CentreY = System.Math.Clamp(cy, f.BottomY + 2 + halfH, f.LandingBandY - 2 - halfH),
        };
    }

    private static void AddStructure(
        System.Collections.Generic.List<Wall> walls,
        System.Collections.Generic.List<Doorway> doorways,
        System.Collections.Generic.List<(double X, double Y)> centres,
        in Field f, double cx, double cy, double size, string bodyId, string tag,
        System.Collections.Generic.List<(double X, double Y, double R)>? footprints = null)
    {
        // 1.6..3.0 du of piled regolith — the owner's Greenland longhouse, and comfortably above the
        // captain's own 1.4 du width so the hatching never emits a segment shorter than a body. The numbers
        // and the clamps live in OrdinaryStructure now, because the claim ledger has to agree with them.
        SurfaceStructure.Spec spec = OrdinaryStructure(f, cx, cy, size, bodyId, tag);

        SurfaceStructure.Built built = SurfaceStructure.Build(spec);
        centres.Add((spec.CentreX, spec.CentreY));
        footprints?.Add((spec.CentreX, spec.CentreY, SurfaceStructure.KeepOutRadius(spec)));
        walls.AddRange(built.Walls);
        foreach (SurfaceStructure.Doorway d in built.Doorways)
        {
            doorways.Add(new Doorway(d.X1, d.Y1, d.X2, d.Y2));
        }
    }

    /// <summary>
    /// #563 · A SMALL BUILDING — four walls, a doorway you walk through, and usually a room inside it.
    ///
    /// <para>Owner, 2026-08-01: <i>"as for content there needs to be more than silly U shapes... more stuff
    /// like small buildings with actual walls and doors."</i> He is right, and the U was the weakest thing
    /// the generator made: a rectangle with one side left off is not a ruin, it is a rectangle somebody
    /// forgot to finish. It has no inside, so there is nothing to enter and nothing to find.</para>
    ///
    /// <para>A building has a real threshold. You walk THROUGH something to be inside it, and inside there
    /// is a partition with its own doorway, so even a small footprint gives two spaces and a reason to walk
    /// the second one. That is what turns a shape on the ground into a place.</para>
    ///
    /// <para>Every opening is <see cref="DoorwayHalf"/> × 2 wide — comfortably more than the captain's
    /// diameter — and the partition's doorway is deliberately offset from the outer one so the two are
    /// never in line. A straight shot from the street to the back wall makes the interior read as a corridor
    /// rather than as rooms, and it also means one glance from outside tells you everything.</para>
    ///
    /// <para>These are ruins, so the openings are OPENINGS — no hinges, nothing to force. The lockable
    /// version is the outpost hut (<see cref="SurfaceOutpost"/>), which is a different thing on purpose: one
    /// is scenery you can step into, the other is a decision with a locker behind it.</para>
    /// </summary>
    private static void AddBuilding(System.Collections.Generic.List<Wall> walls, in Field f,
        double cx, double cy, double w, double h, string bodyId, string tag)
    {
        // Keep the whole footprint inside the safe span; a building clipped by the edge lane would have its
        // doorway cut off and become a solid block.
        double halfW = System.Math.Min(w, 14) / 2, halfH = System.Math.Min(h, 12) / 2;
        double x0 = System.Math.Max(f.LeftX + EdgeMargin, cx - halfW);
        double x1 = System.Math.Min(f.RightX - EdgeMargin, cx + halfW);
        double y0 = System.Math.Max(f.BottomY + 2, cy - halfH);
        double y1 = System.Math.Min(f.LandingBandY - 2, cy + halfH);

        // Too small to hold a doorway and a room? Then it is rubble, and rubble is what it should look like.
        //
        // Emphatically NOT a closed box: that was the first thing written here, and a footprint clipped by
        // the edge lane can still be large, so "too small for a door" was quietly producing big SEALED
        // interiors — precisely the failure the doorway exists to avoid. Two walls that meet at a corner
        // enclose nothing, whatever size they are.
        if (x1 - x0 < MinDooredFace + 2 || y1 - y0 < MinDooredFace + 2)
        {
            walls.Add(new(x0, y0, x1, y0, false));
            walls.Add(new(x0, y0, x0, y1, false));
            return;
        }

        int doorWall = Face(bodyId, $"{tag}:door", 4);   // which face carries the way in
        double doorAlong = Lerp(0.3, 0.7, Frac(bodyId, $"{tag}:doorat"));

        // Bottom, top, left, right — each solid unless it is the one with the doorway in it.
        AddFace(walls, x0, y0, x1, y0, horizontal: true, gapAt: doorWall == 0 ? Lerp(x0, x1, doorAlong) : null);
        AddFace(walls, x0, y1, x1, y1, horizontal: true, gapAt: doorWall == 1 ? Lerp(x0, x1, doorAlong) : null);
        AddFace(walls, x0, y0, x0, y1, horizontal: false, gapAt: doorWall == 2 ? Lerp(y0, y1, doorAlong) : null);
        AddFace(walls, x1, y0, x1, y1, horizontal: false, gapAt: doorWall == 3 ? Lerp(y0, y1, doorAlong) : null);

        // One interior partition, across the building's SHORT axis so both rooms stay usably wide, with its
        // own doorway pushed to the far side from the outer door.
        bool splitVertically = (x1 - x0) >= (y1 - y0);
        double innerDoor = Lerp(0.25, 0.75, 1.0 - doorAlong);
        if (splitVertically)
        {
            double px = Lerp(x0, x1, Lerp(0.4, 0.6, Frac(bodyId, $"{tag}:split")));
            AddFace(walls, px, y0, px, y1, horizontal: false, gapAt: Lerp(y0, y1, innerDoor));
        }
        else
        {
            double py = Lerp(y0, y1, Lerp(0.4, 0.6, Frac(bodyId, $"{tag}:split")));
            AddFace(walls, x0, py, x1, py, horizontal: true, gapAt: Lerp(x0, x1, innerDoor));
        }
    }

    /// <summary>One wall face, solid or split around a doorway at <paramref name="gapAt"/>. The gap is
    /// clamped so it can never run off the end of the face and quietly delete a whole wall.</summary>
    private static void AddFace(System.Collections.Generic.List<Wall> walls,
        double x0, double y0, double x1, double y1, bool horizontal, double? gapAt)
    {
        if (gapAt is not { } g)
        {
            walls.Add(new(x0, y0, x1, y1, false));
            return;
        }

        // MinStub, not 0.5. A doorway clamped hard against the end of a face leaves a stub shorter than the
        // captain is wide, and DegenerateWallScan is right to call that an invisible wall: you cannot see it,
        // you cannot walk through it, and it reads as the game cheating. Either a face has room for a door
        // with real jambs either side, or it does not get the door.
        if (horizontal)
        {
            if (x1 - x0 < MinDooredFace) { walls.Add(new(x0, y0, x1, y1, false)); return; }
            g = System.Math.Clamp(g, x0 + DoorwayHalf + MinStub, x1 - DoorwayHalf - MinStub);
            walls.Add(new(x0, y0, g - DoorwayHalf, y0, false));
            walls.Add(new(g + DoorwayHalf, y1, x1, y1, false));
        }
        else
        {
            if (y1 - y0 < MinDooredFace) { walls.Add(new(x0, y0, x1, y1, false)); return; }
            g = System.Math.Clamp(g, y0 + DoorwayHalf + MinStub, y1 - DoorwayHalf - MinStub);
            walls.Add(new(x0, y0, x0, g - DoorwayHalf, false));
            walls.Add(new(x1, g + DoorwayHalf, x1, y1, false));
        }
    }

    /// <summary>Half a doorway's width. The captain is 1.4 du across; this leaves room to walk it badly,
    /// which is the bar every doorway in this game is held to (#498's "a bit narrow but navigatable").</summary>
    private const double DoorwayHalf = 1.6;

    /// <summary>The shortest jamb a doorway may leave beside it. Anything less is a stub nobody can see and
    /// nobody can pass — an invisible wall, which DegenerateWallScan exists to refuse.</summary>
    private const double MinStub = 1.6;

    /// <summary>The shortest face that can carry a doorway at all: the opening plus a real jamb each side.</summary>
    private const double MinDooredFace = (DoorwayHalf + MinStub) * 2;

    private static void AddOpenBox(System.Collections.Generic.List<Wall> walls, in Field f,
        double cx, double cy, double w, double h, int gapSide)
    {
        double x1 = System.Math.Max(f.LeftX + EdgeMargin, cx - w / 2);
        double x2 = System.Math.Min(f.RightX - EdgeMargin, cx + w / 2);
        double y1 = System.Math.Max(f.BottomY + 2, cy - h / 2);
        double y2 = System.Math.Min(f.LandingBandY - 2, cy + h / 2);
        if (gapSide != 0) { walls.Add(new(x1, y1, x2, y1, false)); } // bottom
        if (gapSide != 1) { walls.Add(new(x1, y2, x2, y2, false)); } // top
        if (gapSide != 2) { walls.Add(new(x1, y1, x1, y2, false)); } // left
        if (gapSide != 3) { walls.Add(new(x2, y1, x2, y2, false)); } // right
    }

    private static void AddClampedBox(System.Collections.Generic.List<Wall> walls, in Field f,
        double x1, double y1, double x2, double y2, bool hull)
    {
        x1 = System.Math.Max(f.LeftX + EdgeMargin, x1);
        x2 = System.Math.Min(f.RightX - EdgeMargin, x2);
        AddBox(walls, x1, y1, x2, y2, hull);
    }

    /// <summary>#649 · <see cref="AddSolidMass"/>, clamped into the field's kept-open edge lanes — what an
    /// authored signature needs once it is big enough to reach them.
    ///
    /// <para>The monolith went onto Phobos as a clamped BOX and the reachability audit caught it in one run:
    /// 99 cells of sealed ground at (−8, −234.5), which is the inside of the slab. A solid drawn as four
    /// lines has an interior, and an interior is somewhere a captain could stand if only they could get in.
    /// The two properties are independent and the object needs both, so there is a helper that has both
    /// rather than a caller that remembers to.</para></summary>
    private static void AddClampedSolidMass(System.Collections.Generic.List<Wall> walls, in Field f,
        double x1, double y1, double x2, double y2, bool hull)
    {
        x1 = System.Math.Max(f.LeftX + EdgeMargin, x1);
        x2 = System.Math.Min(f.RightX - EdgeMargin, x2);
        AddSolidMass(walls, x1, y1, x2, y2, hull);
    }

    // ── Seeded sampling: pure and deterministic per (bodyId, tag) off the shared dice engine. ──
    private const int Resolution = 4096;

    private static double Frac(string bodyId, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"surface:{bodyId}:{tag}"), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }

    private static int Face(string bodyId, string tag, int sides) =>
        DiceRule.Roll(DiceRule.Seed($"surface:{bodyId}:{tag}"), sides).Face - 1; // 0..sides-1

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}
