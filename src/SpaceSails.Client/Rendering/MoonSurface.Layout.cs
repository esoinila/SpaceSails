using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #371 · THE BUILD ITSELF — the one pure function the memo above stores: hull, airlock, tube, the
/// barren field and everything standing on it, assembled into the arrays a <c>Layout</c> is made of.
///
/// <para>It is a single method by design rather than by neglect: every accumulator it fills is read by
/// the pass after it, and the order the ground is laid in IS the picture. #1167 turned a method this
/// shape into forty named passes when it reached 1,106 lines; this one is 416 and the seam that would
/// pay for itself is not obvious yet.</para>
///
/// <para>Split out of <c>MoonSurface.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class MoonSurface
{
    private static Layout BuildLayout(
        string bodyId,
        string bodyDisplayName,
        IReadOnlyList<(string Id, double X, double Y, int ReeverLevel)> ownCaches,
        string siteSalt, string siteName, long monolithEpoch, bool hasSecretSite, bool preserved)
    {
        DeckPlan ship = DeckPlan.Ship;

        // Start from the ship, minus the sealed bottom-hull hatch (the surface opens it) — the same move
        // the docked complex makes with the top airlock hatch, so the walk grammar is identical.
        var sealedHatch = new DeckPlan.Wall(TubeLeft, DeckPlan.ShuttleHatchY, TubeRight, DeckPlan.ShuttleHatchY, false, true);
        var walls = new List<DeckPlan.Wall>(ship.Walls.Where(w => !w.Equals(sealedHatch)));
        var doors = new List<DeckPlan.Door>(ship.Doors.Where(d => !IsHatchDoor(d)));
        var labels = new List<(float X, float Y, string Text)>(ship.RoomLabels);

        // ── The dual-door airlock + down-tube (owner: "that airlock vibe on the docking... to the shuttle
        //    bay also"). Door / chamber / door, exactly like the topside station tube: two hull walls with
        //    an auto-door at each end. The ship-end door is the crew-only Reever lock. ──
        walls.Add(new(TubeLeft, DeckPlan.ShuttleHatchY, TubeLeft, SurfaceTopY, false, true));
        walls.Add(new(TubeRight, DeckPlan.ShuttleHatchY, TubeRight, SurfaceTopY, false, true));
        // #462: the tube IS an airlock — its two doors share an interlock group, so only the end nearest the
        // captain may stand open and the far end is always drawn SHUT (owner: "only one door in a tube is
        // open at a time… think of airlock"). That is the barrier the Old Ones visibly stop at, and it is
        // what shuts a tailgater in the tube with the built-in gun (#461) instead of letting it follow you
        // aboard. Before this, standing at the threshold held BOTH ends retracted — so the pack piled up
        // against a gap painted wide open.
        const int TubeAirlock = 1;
        doors.Add(new(TubeLeft, DeckPlan.ShuttleHatchY, TubeRight, DeckPlan.ShuttleHatchY, Interlock: TubeAirlock)); // ship-end: crew-only door
        doors.Add(new(TubeLeft, SurfaceTopY, TubeRight, SurfaceTopY, Interlock: TubeAirlock));                       // surface-end
        // The shuttle glyph mid-tube — the map winking at its own abstraction (this corridor IS the ride).
        labels.Add((TubeCenterX, (DeckPlan.ShuttleHatchY + SurfaceTopY) / 2f, "🛸"));

        // ── The wide barren field. #563 · TWO DIFFERENT KINDS OF EDGE, and they must not be drawn alike.
        //
        //    The TOP rim is the ship's own underside — the hull you just walked out of, a made thing with a
        //    real outside. Owner, 2026-07-31: "The space ships come with outside borders but the landing
        //    site out-doors should not." So this one keeps its hull ink; it is honest.
        walls.Add(new(SurfaceLeftX, SurfaceTopY, TubeLeft, SurfaceTopY, false, true));   // top rim, port of the tube
        walls.Add(new(TubeRight, SurfaceTopY, SurfaceRightX, SurfaceTopY, false, true)); // top rim, starboard of the tube

        //    The other three sides are the FIELD ENVELOPE — a technical limit on how far the ground is
        //    generated, with no object in the world to be. Drawn as bright hull lines they made a square
        //    fence around a moon ("it seems artificial on a Moon… it spoils the site feeling"), and worse,
        //    they announced a boundary that is not the real one: the honest edge of a landing site is where
        //    the magazine and the pack behind you say turn around — "the reevers and supply line are kind of
        //    the invisible tether to players distance" — the #453 law that depth is priced by sentries and
        //    nerve, never by geometry. So they are UNSEEN: they collide, and nothing is ever drawn for them.
        //
        //    That first pass hid the fence and left the SHAPE alone, which the owner went straight to:
        //    "But the limit to movement is still a box here?" It was. So the bound is no longer three
        //    straight walls but a wandering chain (SurfaceEdge), seeded per site — the limit to movement is
        //    not a rectangle in the collision either, not merely in the picture.
        //
        //    It only ever bulges OUTWARD from the nominal rectangle, which is what makes it safe to lay
        //    under a game already generating near the edge: the outpost huts (#563) are built INTO the far
        //    edge lane, and a boundary free to wander inward would eventually eat one. Outward can only add
        //    bare regolith. The bulge tapers to nothing at every corner, so the chain closes exactly and
        //    there is no gap for a captain to walk out of the world through.
        // #585 · ONE FIELD, ONE EXPRESSION. This used to re-type the same seven constants that
        // ExpeditionField() is built from — identical values, and a SECOND COPY of the arithmetic that the
        // beacons, the shelter placer, the audits and the labs all read through ExpeditionField(). That is
        // the precise shape of the bug that made the map lie (SpecFor vs SpecsFor) and of the envelope drift
        // before it. There is nothing to keep in sync if there is only one of it.
        SurfaceLayout.Field field = ExpeditionField();

        // #563 · AND THEN THE BOUND CAME OFF ALTOGETHER. The wandering chain used to be laid here as three
        // unseen-but-solid runs, and it was the last thing making the ground finite. It is a far BACKSTOP now
        // (SurfaceEdge.BeyondBackstop, ten thousand du out, enforced as a radius rather than as stone), and
        // what stops a captain at any distance a captain actually walks is the tether: the tank, the
        // magazine, the walk home. The ground itself simply carries on — SurfaceTiles lays the next tile.

        // ── The PER-BODY geography (Sunday-morning wind #1–#2): the deep-field ruin/maze walls and the
        //    landmark vary by body — Miranda keeps THE MONOLITH maze (canon), Luna gets the mass-driver
        //    ruins, every other landable body a seeded signature — so no two grounds are the same. The
        //    field envelope above is the shared LAW; only what's inside it is the body's own. Walls are
        //    collision law for everyone (the pure Core SurfaceLayout is where a test pins the geography). ──
        // #320: the chosen landing site parameterizes the ground — an empty salt is the canon site 0
        // (Miranda's maze, Luna's rails, the seeded signature), a non-empty salt re-seeds a distinct wing.
        SurfaceLayout.Plan layout = SurfaceLayout.For(bodyId, field, siteSalt);
        foreach (SurfaceLayout.Wall w in layout.Walls)
        {
            // #563 · A body's own geography is ROCK, never pressure hull. SurfaceLayout's IsHull flag means
            // "solid mass" — the monolith, Luna's mass-driver muzzle, a seeded plinth or ancient spur — as
            // opposed to a fallen span, and that distinction is worth keeping. What was wrong was the ink:
            // it drew in the ship's cold blue-white hull stroke, so 16 of the Ridge Camp's 25 segments were
            // painted as spaceship. Same flag, translated to stone.
            // #649 · Unseen carries through: a solid's internal hatch may be collision-only (the monolith's
            // is), and the renderer already knows how to stop you at something it never paints — that is what
            // the field's own bound has always been.
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, false,
                Unseen: w.Unseen, IsStone: w.IsHull));
        }

        // #573 · EVERY BUILDING GETS A REAL DOOR. Owner: "there seemed to be shelter like spaces that were
        // just missing the services and the doors.... let's fix those." They had openings the whole time —
        // the generator was discarding them — so a thick-walled ruin read as an unfinished shelter instead
        // of somewhere people used to live. An auto-door on each one makes it a building you enter.
        // #592 · AND ONE OF THEM COST SOMEBODY MONEY. Owner: "some special color not distinctive to the site
        // could then used to draw our attention to a place (like expensive door made with far away imported
        // materials)."
        //
        // Every ordinary hatch is drawn in the local stone, so an off-palette one is a SENTENCE — somebody
        // shipped materials across the system to seal this room, and nobody does that for a store cupboard.
        // Rare on purpose (one in seven): a signal that fires on every ruin is wallpaper. It is seeded per
        // doorway, so the room worth breaking into is a fact about the site rather than a fresh die.
        // #563 slice 2 · ASKED IN CORE, because the lattice's tiles hang exactly these and a rule written
        // out twice is the bug class where one copy gets edited. SurfaceTiles.Doors answers the HOME tile on
        // the site's own salt, so every door on this ground is the colour it has always been.
        foreach (SurfaceTiles.HungDoor d in SurfaceTiles.Doors(bodyId, siteSalt, SurfaceTiles.Home))
        {
            doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Imported: d.Imported));
        }

        // #573 · AND SOMETHING INSIDE ABOUT HALF OF THEM — the "services" half of the same report. A
        // thick-walled room you can enter and find nothing in is worse than no room at all, because the walk
        // in cost air and taught you not to bother next time. But if EVERY building paid out, entering them
        // would stop being a decision and become a chore performed on all of them, so the empty ones are
        // load-bearing: they are what make the others worth the suit-air.
        //
        // #563 slice 2 · …and this question, too, is Core's now, for the same reason the doors above are:
        // every tile in the lattice asks it and there must be exactly one asking.
        IReadOnlyList<SurfaceTiles.Drawer> salvageSpots =
            SurfaceTiles.Drawers(bodyId, siteSalt, SurfaceTiles.Home);

        var consoles = new List<DeckPlan.ConsoleSpot>(
            ship.Consoles.Where(c => c.Kind != DeckPlan.ConsoleKind.Airlock))
        {
            // The way home: board the shuttle just off the tube mouth (kept clear of the tube walls). Always here.
            new(DeckPlan.ConsoleKind.SurfaceAirlock, TubeCenterX + 3.5f, SurfaceTopY - 2.5f, "🛸 BOARD THE SHUTTLE"),
            // The lonely automated kiosk — a PLACE has amenities (owner addendum 2). Near the landing, port
            // of the tube. Last restocked before the war.
            new(DeckPlan.ConsoleKind.Kiosk, TubeCenterX - 9f, LandingBandY, "🛒 SOUVENIR KIOSK"),
        };

        // No fixed ⛏ console any more (beach-comber kit): burying is free-form, E where you stand. Only an
        // own cache's ✗ gets a dig console at its mark (contextual 'dig at the X').
        foreach ((string _, double cx, double cy, int _) in ownCaches)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.DigSite, (float)cx, (float)cy, "🗺 DIG AT THE X"));
        }

        labels.Add((TubeCenterX, SurfaceTopY - 3.5f, $"— {bodyDisplayName.ToUpperInvariant()} SURFACE —"));
        // #320: the surface header names WHERE you set down — the chosen landing site, plainly, at the
        // landing band (the crude-grid deck aesthetic: a label on the ground, no new chrome). Site 0's
        // name still reads even though its ground is the canon signature.
        if (siteName.Length > 0)
        {
            labels.Add((TubeCenterX, SurfaceTopY - 5.2f, $"🛬 SET DOWN AT: {siteName.ToUpperInvariant()}"));
        }
        foreach (SurfaceLayout.Landmark m in layout.Landmarks)
        {
            labels.Add(((float)m.X, (float)m.Y, m.Label));
        }
        labels.Add((SurfaceRightX - 8, SurfaceBottomY + 3, "REGOLITH · NO ATMOSPHERE"));

        var backdrops = new List<DeckPlan.Backdrop>(ship.Backdrops);

        // #649 · THE ONE FILLED MASS ON ANY MOON. Every other solid object on a landing site is drawn as a
        // hatched outline — the established idiom for piled regolith, and the reason a plinth reads as mass
        // rather than as a room. The monolith is drawn as neither: a single unbroken filled rectangle, with
        // no join anywhere in it, using the same primitive the ship's solid shielding uses (#537: "drawn
        // filled so it cannot be mistaken for somewhere you could be").
        //
        // This is the owner's "not having been built by us", in the picture rather than in a sentence. The
        // proportions are 1 : 4 : 9 and no quarry cuts that; the face has no seam and no course; and neither
        // fact is ever stated anywhere the captain can read it.
        var structures = new List<DeckPlan.Structure>();
        if (Monolith.StandsOn(bodyId, siteSalt))
        {
            structures.Add(new DeckPlan.Structure(
                AnchorX - (float)Monolith.HalfWidth, AnchorY - (float)Monolith.HalfHeight,
                AnchorX + (float)Monolith.HalfWidth, AnchorY + (float)Monolith.HalfHeight));
        }

        // #649 · HOW WIDE "THE DEEP AREA" IS depends on what is standing in it. This was a flat 16 du either
        // side of the anchor, which is honest for a six-du fixture and a lie for a fifty-four-du one: a
        // captain with a glove flat on the west end of the monolith would have been told they were on
        // PHOBOS SURFACE, which is the sim doing one thing and the sentence saying another (bug class 3), on
        // the one square of ground in the game where the sentence matters most.
        double deepHalfW = Math.Max(16.0, Monolith.StandsOn(bodyId, siteSalt)
            ? Monolith.ApronRadius
            : 0.0);
        double deepTopY = AnchorY + Math.Max(8.0, Monolith.StandsOn(bodyId, siteSalt)
            ? Monolith.HalfHeight + 8.0
            : 0.0);

        // The location line is a pure function of (position, bodyDisplayName, layout.Scheme, ship) — all
        // deterministic per body id, none of it component-bound — so the closure is safe to cache.
        Func<double, double, string> location =
            (x, y) => y > DeckPlan.ShuttleHatchY ? ship.Location(x, y)
                    : y > SurfaceTopY ? "DOWN-TUBE (the shuttle ride)"
                    : y > LandingBandY - 2 ? "LANDING AREA"
                    : y < deepTopY && Math.Abs(x - AnchorX) < deepHalfW ? layout.Scheme
                    : $"{bodyDisplayName.ToUpperInvariant()} SURFACE";

        // #563 · The terrain. Owner: "put something more interesting in the landscape." Crater rims, scree
        // fans, scarps and rilles, seeded per site and spread across the WHOLE field — including the flanks,
        // which are kept clear of WALLS so a walk-around always exists and were therefore the emptiest and
        // most walkable third of every site. Scenery cannot obstruct, so it is free to go exactly there.
        var sceneryList = new List<SurfaceScenery.Mark>(SurfaceScenery.For(bodyId, siteSalt, field));

        // ── #585 · THE CAMOUFLAGED LIFT HEAD. Owner: "on surface we would only need a camouflaged elevator.
        //    I think there are a lot of movie references to masked elevators to underground sites (The Hive
        //    in Resident Evil for example)."
        //
        // A LIFT LOBBY, not a ruin with a secret in the middle. The first attempt was both of those mistakes:
        // it was built through SurfaceStructure — which adds an interior PARTITION, so the middle of the shed
        // could be walled off from its own doorway — and the call button sat at the building's centre while
        // DeckPlan.InteractRadius is 3 du and the shed was fourteen across. The owner stood in the doorway,
        // which is exactly where a person stands at a door, and reported "it says nothing here", then
        // "there is no console ... I tried E to dig to find anything and found nothing but regolith."
        //
        // So: one small room, one door, one button a pace inside it where a lobby puts it. An affordance you
        // can see and cannot use is worse than none (#212).
        //
        // Drawn from hasSecretSite — the fact the EXCURSION resolved, which honours ?secretlab= — never from
        // SecretLab.Present(bodyId), the unforced seed. That was the other half of the same report: on a
        // cheat rock whose seed said "no lab", the head was never built and the feature was unreachable from
        // the one URL that exists to reach it.
        if (hasSecretSite)
        {
            // ── #606 · ONE MORE HUT AMONG THE HUTS. Owner, after a fresh look at the ground: "the elevator
            //    still stands out on surface like a sore thumb" — and, on the fix he wanted: "it could be in
            //    an ordinary hut, with 2 doors .. we have those. The expensive doors would be the clue... a
            //    clue we can get tipped about or find it in papers."
            //
            // What made it a sore thumb was never its colour. It was five thin lines in a 10 x 8 rectangle
            // standing on a moon where every other building is piled regolith with hatched thickness and a
            // seeded angle — the only structure on the ground drawn in a different hand. That is visible from
            // anywhere, to anybody, and no violet door was ever going to hide it.
            //
            // So it is a SurfaceStructure hut like its neighbours: same builder, same size range, same
            // masonry, seeded angle. Nothing about the silhouette says anything.
            //
            // #585's answer was the opposite — a maintenance plate above the door and "THE CAR IS STILL HERE"
            // below it — and both are gone. They were the game announcing itself, which is the one thing this
            // ground has a house rule against (docs/art-manifest-hive.md: the object is mundane, the
            // implication is not). The findability they were paying for moves to the INFORMATION: the tracker
            // beacon a tip lights (BuildBeacons), the detector gradient, the papers that name a moon. A clue
            // chain is a better game than a caption, and it is the one the owner asked for.
            LiftHeadBox head = LiftHead(bodyId, siteSalt, field);
            SurfaceStructure.Built built = SurfaceStructure.Build(head.Hut);

            foreach (SurfaceLayout.Wall w in built.Walls)
            {
                walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, false, IsStone: true));
            }

            // ── AND THE ONE THING THAT DOES NOT MATCH. Every hatch on a landing site is swaged out of the
            //    hill it is set in (#592); these were flown here. IMPORTED puts them off the world's palette,
            //    and MACHINED draws them as what they are — a heavy sealed leaf in a wall of piled rubble,
            //    where every other hut on the moon has a thin one. A captain who never looks closely walks
            //    past. A captain who reads doors has just found a receipt, which is the only thing this
            //    operation has ever been careless with.
            foreach (SurfaceStructure.Doorway d in built.Doorways)
            {
                doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2,
                    Imported: true, Machined: true));
            }

            // Named for what it looks like bolted to a wall, never for what it does. The panel says the rest
            // once it is pressed — and until it is, this is a shack with an odd fitting in it.
            consoles.Add(new(DeckPlan.ConsoleKind.HiveHead,
                (float)head.CarX, (float)head.CarY, "▤ SERVICE PANEL"));

            // ── #1074 beat 2 · AND, ON A SITE THE OFFICE HAS TAKEN INTO CARE, A RAIL AND A SIGN.
            //
            // A working the Authority closed a shift ago is fenced, signed and under study a shift after
            // that: a small closed ring of rail round the shed, one gap in it, and the notice posted at the
            // gap. Two objects, and between them they are the entire beat — there is no card, no pulse, no
            // marker and nothing on the wire (#761: the world tells it plainly or not at all).
            //
            // THE RAILS ARE DRAWN AS ORDINARY INNER LINES. Not hull, which is the rim fence #565 took off
            // this ground for announcing a boundary that was not the real one; not stone, because a rail is
            // not mass; not unseen, because being seen is its whole job. It is the same ink every fallen
            // span and low ruin wall on every landing site already wears, which is what makes it read as a
            // thing somebody put up rather than as a new piece of chrome.
            //
            // AND THE GAP FACES THE TUBE, which is a law and not a flourish — see PreservationZone. The car
            // comes up in the middle of this ring, and a captain must never be fenced away from his own boat.
            if (preserved)
            {
                PreservationZone.Fence fence = PreservationZone.FenceAround(head.Hut, field);
                foreach (SurfaceLayout.Wall r in fence.Rails)
                {
                    walls.Add(new((float)r.X1, (float)r.Y1, (float)r.X2, (float)r.Y2, false, false));
                }

                // The notice, in the ground-label idiom this surface has posted every sign in since #313 —
                // a line of text on the regolith, no new chrome (the crude-grid deck aesthetic). It stands a
                // pace outside the gate on the approach, so it is read on the way in.
                labels.Add(((float)fence.SignX, (float)fence.SignY, PreservationZone.Notice));
            }
        }

        // #586 · THE SWEPT APRON. Owner: "it is supposed to be impressive... now it looks like a box in
        // closet." Widening the slab alone could never fix that — on a crude grid every rectangle is a
        // rectangle, and the grid is the aesthetic, not a limitation.
        //
        // What DOES read at this scale is ground that is visibly cleared. A ring of swept regolith around the
        // slab, in a field where everything else is rubble and drift, is legible from a long way off and says
        // the one thing the stone cannot say by itself: SOMEBODY CARED ABOUT THIS SPOT. Drawn as scenery, so
        // it can never become a fence around the landmark and turn a pilgrimage into a puzzle.
        //
        // #649 · AND ONLY WHERE THERE IS SOMETHING TO SWEEP AROUND. This loop ran unconditionally, so every
        // ground in the game — Luna's mass-driver muzzle, every seeded ancient spur, every slag field, every
        // second and third landing site on every moon — was drawn standing inside the monolith's ceremony
        // ring. That is the same fault as #648 one layer out in the picture instead of the predicate: a
        // ceremony that belongs to ONE object, laid on grounds that have never seen it, telling the captain
        // somebody cared about a patch of rubble. The apron belongs to the thing it is swept around, so it is
        // asked for by the thing it is swept around.
        AddApron(sceneryList, Monolith.StandsOn(bodyId, siteSalt)
            ? (Monolith.ApronRadius, Monolith.ApronSegments)
            : FalseSlab.StandsOn(bodyId, siteSalt)
                ? (FalseSlab.ApronRadius, FalseSlab.ApronSegments)
                : null);

        // #586 · THE PICTURE, AND WHAT IS AT ITS FOOT. Owner: "let's have gen AI image at the monolith and
        // some items appearing there now and then ... it is supposed to be impressive."
        //
        // The monolith's own ground only. Every body dresses this same deep anchor differently — Miranda's
        // false slab, Luna's mass-driver muzzle, a seeded plinth elsewhere — and putting THE MONOLITH's card
        // on a launch head would be the borrowed-prose bug (#574) wearing a landmark.
        // Monolith.StandsOn, not a literal body id: the slab's GEOMETRY is only laid on the canon empty-salt
        // ground (SurfaceLayout routes site 1+ to the seeded generator), so a bare body-id test put the
        // ▮ THE MONOLITH card and the foot-offering on that moon's seeded sites too, pointing at open
        // regolith. That is precisely what the note below forbids — a marker pointing at nothing.
        if (Monolith.StandsOn(bodyId, siteSalt))
        {
            // #649 · THE SHADOW, which is the only way a top-down plan can say 121 deck units TALL.
            //
            // Owner: it must "read as large from a long way off, and keep getting larger as you walk, the way
            // only genuinely big things do." Nothing about a footprint does that — a rectangle on a grid is a
            // rectangle whatever size it is, and the camera only frames about 64 du, so from the landing band
            // even a slab this big is simply off-screen. What IS visible from every square of the field is
            // what an object that tall does to the light: at 18° of sun it throws a lane of dark roughly 370
            // du up-field, which is longer than the walked world is deep.
            //
            // So the captain steps off the pad into shade that runs off the bottom of the map, and the only
            // way to find out what is casting it is to walk down it. Nothing says so. Drawn as scenery —
            // never collided — because a shadow is not a thing you can bump into, and because the one law
            // this ground has is that what stops you and what you can see are separate arrays.
            AddShadow(sceneryList, field);

            // The card, on the APPROACH side. It used to sit deep of the slab, which was harmless at six deck
            // units and is a walk of fifty round solid rock at the real size — an affordance you can see and
            // cannot reach is worse than none (#212). The captain always comes from up-field; the console
            // meets them at the face.
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject,
                AnchorX, AnchorY + (float)Monolith.HalfHeight + 2.5f,
                Monolith.ConsoleLabel, Monolith.ArtUrl, Monolith.Lore));

            // And whatever somebody left, if this window has anything. NOT a console that is always there
            // with an empty payload — a marker pointing at nothing is the map lying, which is the one thing
            // this ground is not allowed to do. Along the same face, a good way down it: things are left at
            // the FOOT of the thing, and the foot is now long enough to walk.
            Monolith.Offering left = Monolith.AtTheFoot(bodyId, siteSalt, monolithEpoch);
            if (left != Monolith.Offering.Nothing)
            {
                consoles.Add(new(DeckPlan.ConsoleKind.MonolithFoot,
                    AnchorX + (float)(Monolith.HalfWidth * 0.55),
                    AnchorY + (float)Monolith.HalfHeight + 2.5f,
                    Monolith.FootLabel(left)));
            }
        }

        // #649 · AND MIRANDA'S OWN CARD. The maze centre had a picture and a card for as long as it was
        // called the monolith; taking the word back must not take the content with it, or the canon ground
        // the owner walks by default would quietly lose the one thing at the end of its long walk. It gets
        // its OWN card, which describes a stacked, mortared, weathering object and accounts for nothing —
        // reusing the monolith's plate here would have been #574 all over again.
        else if (FalseSlab.StandsOn(bodyId, siteSalt))
        {
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject,
                AnchorX, AnchorY - (float)FalseSlab.HalfHeight - 2f,
                FalseSlab.ConsoleLabel, FalseSlab.ArtUrl, FalseSlab.Lore));
        }

        SurfaceScenery.Mark[] scenery = [.. sceneryList];

        // #573 · THE SHELTER, deep in the field: one guaranteed building with a REAL door and air inside.
        // Owner: "just make one building into the middle there with working door" → "or lets put that near
        // the bottom there" → "there needs to be explorable space around that building we go to refill."
        //
        // The door is an actual DeckPlan.Door hung on the opening SurfaceStructure hands back — which is why
        // that returns the doorway as a segment rather than a midpoint: a point cannot say which way a
        // passage runs, and a door needs to know.
        // #573 · EVERY shelter, not just the first. THE MAP LIED, and this was why: SurfaceShelter grew
        // from one shelter to several (SpecsFor), the tracker beacons were switched to show all of them, and
        // THIS — the code that actually builds them — was left calling SpecFor and laying exactly one. So
        // two to four rings on the fan pointed at buildings that had never been built, including one the
        // owner was standing directly on top of.
        //
        // Same shape as the field-envelope drift earlier today: two places that had to agree, only one of
        // them updated. The fix here is that there is now ONE loop, and the beacons read the same list.
        // #563 slice 3 · ASKED OF THE TILE, so the ground under the tube and the ground nine hundred du out
        // are furnished by one question rather than two. SurfaceTiles.Shelters hands the home tile the site's
        // own salt and the default field — the exact pair this line used to pass by hand — so every drum at
        // the tube keeps its place, its angle and its seeded story.
        foreach (SurfaceStructure.Spec shelter in
                 SurfaceTiles.Shelters(bodyId, siteSalt, SurfaceTiles.Home))
        {
            FurnishShelter(shelter, walls, doors, consoles, labels);
        }

        foreach (SurfaceTiles.Drawer drawer in salvageSpots)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.RuinSalvage, (float)drawer.X, (float)drawer.Y,
                SurfaceSalvage.LabelFor(drawer.Find)));
        }


        return new Layout(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(), location,
            doors.ToArray(), scenery, structures.ToArray());
    }
}
