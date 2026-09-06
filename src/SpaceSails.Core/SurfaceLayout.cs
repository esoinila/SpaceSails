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
/// <remarks>Split by concern at 1,233 lines (#251) into <c>SurfaceLayout.Grounds</c>,
/// <c>…Expedition</c> and <c>…Builders</c>. THIS file is the vocabulary and the door in: what a wall, a
/// landmark, a field, a doorway and a finished <see cref="Plan"/> are; the default field and the three
/// margins every scheme is clamped by; the standing claims a ground must lay itself around; the two
/// <see cref="For(string, in Field)"/> overloads that decide which scheme a body gets; and
/// <see cref="WallHash"/>, the one number a test pins a ground by.</remarks>
public static partial class SurfaceLayout
{
    /// <summary>A generated interior wall in deck units. <paramref name="IsHull"/> marks a solid opaque
    /// face (a landmark's own slab, the mass-driver muzzle) versus an open ruin/maze wall; the client
    /// maps both onto its collidable <c>DeckPlan.Wall</c>, so both stop a boot and a shamble alike.
    ///
    /// <para>#649 · <paramref name="Unseen"/> is a segment that COLLIDES AND IS NEVER DRAWN — the same
    /// distinction the field's own bound already makes (<see cref="SurfaceEdge"/>: it stops you and nothing
    /// is ever painted for it). It exists for the inside of a solid: <see cref="AddSolidMass"/> hatches a
    /// mass through so no gap in it can hold a body, and on a small fixture those strokes are what makes it
    /// read as mass rather than as a suspiciously thick room. On the monolith they would be forty-nine
    /// parallel lines across the one object in the game whose card says <i>no seam</i> — masonry drawn onto
    /// the thing nobody built. Same collision, no joins.</para></summary>
    public readonly record struct Wall(double X1, double Y1, double X2, double Y2, bool IsHull,
        bool Unseen = false);

    /// <summary>A deep-field landmark to label on the ground: its glyph-tagged text at (X, Y).</summary>
    public readonly record struct Landmark(double X, double Y, string Label);

    /// <summary>The shared field envelope the geography is laid inside — the LAWS, handed in from the
    /// client's <c>MoonSurface</c> so Core carries no client geometry constants yet lays ground within
    /// the sane bounds. <paramref name="AnchorX"/>/<paramref name="AnchorY"/> is the deep commitment
    /// area's centre (the old monolith spot), the heart every scheme dresses differently.</summary>
    public readonly record struct Field(
        double LeftX, double RightX, double TopY, double BottomY,
        double LandingBandY, double AnchorX, double AnchorY,
        // #681 · THE COLUMN THE WAY HOME STANDS IN — the tube mouth's x. Handed in for the same reason
        // LandingBandY is: it is a client LAW that Core has to place around. Without it the generator could
        // not know where the landing approach was, and duly built a hut across it on two sites (the owner's
        // ?land=1 drop at (-7, -39) came down inside one). Defaulted so a synthetic test field need not care.
        double HomeX = 0);

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
    /// 310 x 260 du world while <c>SurfaceReachabilityTests</c> and Lab 47 went on auditing and drawing the
    /// old 78 x 64 one — and passed, and printed confident numbers about a world that no longer existed.</para>
    ///
    /// <para>That is the same drift this project keeps paying for, one level up from geometry: a test that
    /// MIRRORS a constant instead of READING it is not testing the thing that ships. One copy now; the
    /// client reads it too.</para></summary>
    public static Field DefaultField { get; } = new(
        LeftX: -160, RightX: 150, TopY: -20, BottomY: -280,
        LandingBandY: -27, AnchorX: -6, AnchorY: -232,
        // #681 · The down-tube's own column (the client's TubeCenterX). Same category of client law as
        // LandingBandY above, and here for the same reason: a copy kept anywhere else drifts.
        HomeX: -7);

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

    /// <summary>#681 · How far below the landing band a shuttle sets boots down on the open regolith — far
    /// enough out that the ground is a place rather than a doorstep, close enough that the way home is still
    /// one run. Lives here rather than in the client because the ground has to place AROUND it, and a number
    /// the generator cannot see is a number the generator will build on.</summary>
    public const double LandingApproachDu = 12.0;

    /// <summary>#681 · The ground kept clear around that spot. Room to stand, room to turn, room to take the
    /// first step in any direction — the three rungs of the spawn law, expressed as a radius. Small, because
    /// every du of keep-out is variety taken away from the rest of the world (#587's lesson).</summary>
    public const double LandingApproachRadius = 6.0;

    /// <summary>#681 · WHERE A LANDING PUTS THE CAPTAIN, and how much room it needs, as ONE answer.
    ///
    /// <para>Owner, on a boot that pinned him in a wall: <i>"I cannot move."</i> The hidden half of that
    /// report is that nothing had ever reserved the square a landing uses. The lift head has had a claim in
    /// this ledger since #585 and the shelters since before that; the spot the captain is actually set down
    /// on had none, so on <c>luna · The Depot Apron</c> a seeded hut was built straight through it and the
    /// audit found the drop inside its wall.</para>
    ///
    /// <para>The client reads this to know where to drop, and the ledger reads it to know what to avoid — one
    /// fact, two uses, which is the only arrangement this file has ever found to work.</para></summary>
    public static (double X, double Y, double R) LandingApproach(in Field field) =>
        (field.HomeX, field.LandingBandY - LandingApproachDu, LandingApproachRadius);

    /// <summary>Lay out one landable body's ground. Miranda and Luna are authored; everything else is
    /// seeded deterministically from its id, so no two grounds are the same by construction.</summary>
    public static Plan For(string bodyId, in Field field) => For(bodyId, field, null);

    /// <summary>#585 · WHERE THINGS ALREADY STAND on one site's ground, so nothing else is laid on top of
    /// one. #563 made it public: the outpost hut is placed AFTER the ground now (it is no longer confined to
    /// an edge lane, because an unbounded world has no edge lane), and it has to place around the same
    /// ledger everything else places around — asked of this one function rather than re-derived, which is
    /// the whole reason the ledger exists.
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
    public static System.Collections.Generic.List<(double X, double Y, double R)> StandingClaims(
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

        // #649 · AND THE MONOLITH, on the one ground that has one. It is 54 du across and its swept apron is
        // 86; a placer that had never heard of it would put a hut inside the stone. Asked of the object
        // itself rather than restated as a number here, so growing it moves everything that must move.
        if (Monolith.KeepOutOn(bodyId, siteSalt, field) is { } slab)
        {
            list.Add(slab);
        }

        // #681 · AND THE GROUND A LANDING SETS BOOTS DOWN ON. The last unclaimed patch on the whole field,
        // and the one every excursion starts and ends on. Every other placer on this ground has kept a claim
        // here for two issues now; the captain's own square never had one, which is how a hut came to be
        // standing on it.
        list.Add(LandingApproach(field));
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

        var keepOut = StandingClaims(bodyId ?? "", siteSalt, field);
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

    /// <summary>#563 · GROUND FOR AN ARBITRARY SEEDED KEY, on an arbitrary field — the treadmill's per-tile
    /// entry point (<see cref="SurfaceTiles"/>).
    ///
    /// <para>This is the same generator every non-authored site has been laid by since #320; it merely had
    /// no public door. A tile out in the world is not a body and not a landing site — it has no shelters to
    /// place around, no hidden lab, no monolith and no landing square — so it hands in its own key and its
    /// own rectangle and gets rubble, buildings and a deep fixture, exactly as a seeded site does.</para>
    ///
    /// <para><paramref name="keepOut"/> is anything ALREADY STANDING on that tile which the generator must
    /// not build through. Kept as a parameter rather than looked up here for the reason this file keeps
    /// re-learning: a generator that finds its own keep-outs has a second opinion about what is on the
    /// ground, and two opinions is how a hut ends up inside a shelter.</para></summary>
    public static Plan SeededGround(
        string key, in Field field,
        System.Collections.Generic.IReadOnlyList<(double X, double Y, double R)>? keepOut = null)
    {
        System.ArgumentNullException.ThrowIfNull(key);
        return Seeded(key, field, keepOut is null
            ? []
            : new System.Collections.Generic.List<(double X, double Y, double R)>(keepOut));
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
}
