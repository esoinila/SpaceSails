using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>Part of <see cref="HavenInterior"/> — #251 · THE WELD'S STEPS. The thirteen named steps
/// <see cref="BuildComplex"/> is made of, extracted from it with every statement verbatim and in its original
/// order: the tube, the ring, the immigration desk, the bar, the observation walk, the back-room leaves, the
/// bar's own fixtures, the concourse, the tables, the backdrops, the wings, the free tops and the gallery.
/// <c>BuildComplex</c> calls them in that order and nothing else changed; no field is declared here.</summary>
public static partial class HavenInterior
{
    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void WeldTheTube(List<DeckPlan.Wall> walls, List<DeckPlan.Door> doors)
    {
        // Tube: the umbilical from the ship's hatch up to the hall's south edge.
        walls.Add(new(TubeLeft, ShipHatchY, TubeLeft, HallBottomY, false, true));
        walls.Add(new(TubeRight, ShipHatchY, TubeRight, HallBottomY, false, true));
        doors.Add(new(TubeLeft, ShipHatchY + 1, TubeRight, ShipHatchY + 1)); // ship-end auto door
        doors.Add(new(TubeLeft, HallBottomY - 1, TubeRight, HallBottomY - 1)); // hall-end auto door
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void CutTheRing(StationSpec spec, HashSet<string> openHatchIds, List<DeckPlan.Wall> walls, List<DeckPlan.Door> doors, List<DeckPlan.ConsoleSpot> hatches)
    {
        // The round hall ring. Vertices at (15 + 30k)°, so edges are centred on the compass points;
        // edge 8 faces south (our tube) and edge 2 faces north (the bar). Every other edge is a
        // sealed berth — a real wall with a cold "locked" hatch drawn on it and a BERTH sign inside.
        var v = new (float X, float Y)[HallSides];
        for (int k = 0; k < HallSides; k++)
        {
            v[k] = HallVertex(k);
        }

        // The ring's sealed edges: a few other captains' berths and the station's own departments,
        // nearly all locked to us — so the concourse reads as one hub of a much bigger complex. Each
        // is a numbered Hatch console: walk up and it names itself + shows locked; press E to knock.
        // A cracked hatch that grows a wing is drawn open (📂) and its edge is a real doorway.
        string[] ringTags =
        [
            "⚓ BERTH", "🔒 CUSTOMS", "🔒 HABITAT RING", "⚓ BERTH", "🔒 MEDBAY",
            "🔒 BONDED STORES", "⚓ BERTH", "🔒 DOCKMASTER", "🔒 TRANSIT", "🔒 SECURITY",
        ];
        int sealedIdx = 0;
        for (int k = 0; k < HallSides; k++)
        {
            (float X, float Y) a = v[k], b = v[(k + 1) % HallSides];
            if (k == 8) // south edge: our tube mouth (gap x 1..4)
            {
                walls.Add(new(a.X, a.Y, TubeLeft, a.Y, false, true));
                walls.Add(new(TubeRight, b.Y, b.X, b.Y, false, true));
            }
            else if (k == 2) // north edge: the wide door to the bar (gap x BarDoorLeft..BarDoorRight)
            {
                walls.Add(new(a.X, a.Y, BarDoorRight, a.Y, false, true));
                walls.Add(new(BarDoorLeft, b.Y, b.X, b.Y, false, true));
                doors.Add(new(BarDoorLeft, a.Y, BarDoorRight, a.Y)); // wide auto door
            }
            else if (k == ObservationWalkEdge && HasObservationWalk(spec.BodyId))
            {
                // #1199 · THE OBSERVATION WALK, at the one station that has one. The edge is cut with the SAME
                // two stubs and unlocked auto-door every other opened joint on this ring gets — the doorway is
                // ordinary, because the room's whole precaution is that nothing about it is special until it
                // is. The tube itself is welded on below; here we only open the wall.
                //
                // The sealed-edge counter is still stepped, one line down, so this station's OTHER nine edges
                // keep the department name and the hatch id they have always had. What the walk costs is the
                // one panel this edge would have carried, and a station with a view instead of a medbay is a
                // decision a port made long before the captain got here.
                (WingWall stubA, WingWall stubB, WingDoor way) =
                    DeckExpansions.CarveDoorway(a.X, a.Y, b.X, b.Y, 0.30f, 0.70f);
                walls.Add(new(stubA.X1, stubA.Y1, stubA.X2, stubA.Y2, false, true));
                walls.Add(new(stubB.X1, stubB.Y1, stubB.X2, stubB.Y2, false, true));
                doors.Add(new(way.X1, way.Y1, way.X2, way.Y2));
                sealedIdx++;
            }
            else if (TheGardenOpensOnEdge(spec, k))
            {
                // #1332 B · THE GARDEN'S TWO DOORWAYS, cut exactly as the walk's is — two stubs and an unlocked
                // auto-door — and the sealed-edge counter stepped over each, so every OTHER edge on this station
                // keeps the department name and the hatch id it always had. What the garden costs is the two
                // panels these faces carried. The room itself is welded on in LayTheGarden.
                (WingWall stubA, WingWall stubB, WingDoor way) = CarveTheGardenDoor(k);
                walls.Add(new(stubA.X1, stubA.Y1, stubA.X2, stubA.Y2, false, true));
                walls.Add(new(stubB.X1, stubB.Y1, stubB.X2, stubB.Y2, false, true));
                doors.Add(new(way.X1, way.Y1, way.X2, way.Y2));
                sealedIdx++;
            }
            else // a sealed berth / department — or an opened expansion joint
            {
                string tag = ringTags[sealedIdx % ringTags.Length];
                string id = $"{spec.Authority[0]}-{k:D2}"; // e.g. M-05: findable, distinct per station
                sealedIdx++;
                float px = HallCenterX + ((a.X + b.X) / 2 - HallCenterX) * 0.9f;
                float py = HallCenterY + ((a.Y + b.Y) / 2 - HallCenterY) * 0.9f;
                if (ACageStandsOnEdge(spec, k))
                {
                    // ── #1253 · A CAR TOOK THIS PANEL ───────────────────────────────────────────────────
                    //
                    // The wall and the cold locked leaf are the ones this edge has always had — a car is a
                    // door in a wall and not a gap in it — and the ONLY thing that changes is what is
                    // written on the console standing in front of it. The sealed-edge counter is stepped
                    // above, exactly as the observation walk steps it, so every other edge on this station
                    // keeps the department name and the hatch id it already had.
                    //
                    // What the station gives up for three lifts is three department plates. That is the
                    // same bargain the walk made for a window, and a port that has been running since
                    // before anybody alive made it a long time ago without mentioning it.
                    walls.Add(new(a.X, a.Y, b.X, b.Y, false, true));
                    doors.Add(new(Lerp(a.X, b.X, 0.25f), Lerp(a.Y, b.Y, 0.25f),
                                  Lerp(a.X, b.X, 0.75f), Lerp(a.Y, b.Y, 0.75f), Locked: true));
                    hatches.Add(new(DeckPlan.ConsoleKind.HavenLift, px, py, CagePlateAbove(spec, k)));
                }
                else if (openHatchIds.Contains(id))
                {
                    // Cracked: carve a walkable doorway (two stubs + an unlocked auto-door), and draw
                    // the panel open (📂). The wing's own walls, added below, close the room beyond.
                    (WingWall stubA, WingWall stubB, WingDoor door) =
                        DeckExpansions.CarveDoorway(a.X, a.Y, b.X, b.Y, 0.30f, 0.70f);
                    walls.Add(new(stubA.X1, stubA.Y1, stubA.X2, stubA.Y2, false, true));
                    walls.Add(new(stubB.X1, stubB.Y1, stubB.X2, stubB.Y2, false, true));
                    doors.Add(new(door.X1, door.Y1, door.X2, door.Y2)); // unlocked — you walk through
                    string dept = string.Join(' ', tag.Split(' ').Where(t => t.All(char.IsLetter)));
                    hatches.Add(new(DeckPlan.ConsoleKind.Hatch, px, py, $"📂 {dept} · {id}"));
                }
                else
                {
                    // Sealed: a real wall with a cold locked hatch drawn on it and a knockable panel.
                    walls.Add(new(a.X, a.Y, b.X, b.Y, false, true));
                    doors.Add(new(Lerp(a.X, b.X, 0.25f), Lerp(a.Y, b.Y, 0.25f),
                                  Lerp(a.X, b.X, 0.75f), Lerp(a.Y, b.Y, 0.75f), Locked: true));
                    hatches.Add(new(DeckPlan.ConsoleKind.Hatch, px, py, $"{tag} · {id}"));
                }
            }
        }
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void LayTheImmigrationDesk(StationSpec spec, List<DeckPlan.Wall> walls, List<(float X, float Y, string Text)> labels)
    {
        // Immigration desk (Total Recall): two counters with a central GATE aligned to the tube, so
        // you walk straight off the umbilical through the checkpoint. Officer to one side.
        float deskY = HallBottomY + 6;
        walls.Add(new(-7, deskY, 1, deskY, false, false)); // counter, port of the gate
        walls.Add(new(4, deskY, 9, deskY, false, false));  // counter, starboard of the gate (gate gap x 1..4)
        labels.Add((HallCenterX, HallBottomY + 7.5f, $"{spec.Authority} IMMIGRATION"));
        labels.Add((HallCenterX, HallBottomY + 2.5f, spec.Quip));
        // #380 item 10 — the officer at that gate is not mute any more. He stands at CustomsDesk and the
        // console that gives him his sentence goes on the same square, below, where the concourse's other
        // fixtures are hung. See ArrivalTube.CustomsLine for what he says and why it is per-tier.
        //
        // A big lobby welcome poster so you know at a glance which port you're standing in.
        labels.Add((HallCenterX, HallCenterY + 8, $"★  WELCOME TO {spec.Name}  ★"));
        labels.Add((HallCenterX, HallCenterY + 3, $"⚓ {spec.Authority} ORBIT"));
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void LayTheBar(StationSpec spec, List<DeckPlan.Wall> walls, List<(float X, float Y, string Text)> labels)
    {
        // The bar, off the hall's north door.
        walls.Add(new(BarLeft, HallTopY, BarDoorLeft, HallTopY, false, true));   // bar floor wall, port of the door
        walls.Add(new(BarDoorRight, HallTopY, BarRight, HallTopY, false, true)); // bar floor wall, starboard of the door
        walls.Add(new(BarLeft, HallTopY, BarLeft, BarTopY, false, true));
        walls.Add(new(BarRight, HallTopY, BarRight, BarTopY, false, true));
        walls.Add(new(BarLeft, BarTopY, BarRight, BarTopY, true, true)); // spinward window onto space
        labels.Add((HallCenterX, BarTopY - 6.5f, spec.BarName));
        labels.Add((8f, HallTopY + 1.5f, "🎁 GIFT SHOP")); // every place has one (owner)
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void LayTheObservationWalk(StationSpec spec, List<DeckPlan.Wall> walls, List<(float X, float Y, string Text)> labels)
    {
        // ── #1199 · THE TUBE ITSELF ──────────────────────────────────────────────────────────────────────
        //
        // Three walls and a plate, and that is the whole room. Two sides running due west from the jambs the
        // ring was just cut at, and a rail square across the blind end — no second opening anywhere on it,
        // which is the one fact about this room the player has to be able to see for himself.
        //
        // ALL THREE ARE GLASS (IsWindow), drawn in the pen's own WindowLine exactly as the bar's spinward
        // window already is. No new renderer, no new ink, no new token: the walk reads as glass because the
        // game already has one way of saying glass, and a tube that needed a new one would be a room the
        // renderer had to be taught about.
        if (HasObservationWalk(spec.BodyId))
        {
            walls.Add(new(TheWalk.MouthX, TheWalk.NorthJambY, TheWalk.BlindX, TheWalk.NorthJambY, true, true));
            walls.Add(new(TheWalk.MouthX, TheWalk.SouthJambY, TheWalk.BlindX, TheWalk.SouthJambY, true, true));

            // ── #1199 (2026-09-18) · AND THE BLIND END IS NOT BLIND ANY MORE: THE GALLERY ────────────────
            //
            // Owner, live: "a tube, then an area to view… like the letter T — now we have the foot of the
            // letter ready." The rail that used to close this end is now the far wall of a room, and what
            // stood here is a doorless opening across the tube's whole width — no leaf, no jambs, nothing to
            // walk through, the way a corridor opens into the room it belongs to. That is what keeps the
            // whole T at ONE doorway (ObservationWalk.Doorways), which is what keeps #822's named exemption
            // honest and what keeps the two-door tell reading the walk's tube as the SECOND doorway (#1233).
            //
            // The back wall is therefore two stubs off the tube's own jambs, and they are the only STONE in
            // this room: the machines are bolted to them and there is a concourse on the other side. The
            // other three are glass, exactly as the tube's three were — the same IsWindow the bar's spinward
            // window is drawn with, the same WindowLine, no new renderer and no new ink.
            walls.Add(new(TheGallery.EastX, TheGallery.NorthY, TheWalk.BlindX, TheWalk.NorthJambY, false, true));
            walls.Add(new(TheGallery.EastX, TheGallery.SouthY, TheWalk.BlindX, TheWalk.SouthJambY, false, true));
            walls.Add(new(TheGallery.EastX, TheGallery.NorthY, TheGallery.WestX, TheGallery.NorthY, true, true));
            walls.Add(new(TheGallery.EastX, TheGallery.SouthY, TheGallery.WestX, TheGallery.SouthY, true, true));
            walls.Add(new(TheGallery.WestX, TheGallery.NorthY, TheGallery.WestX, TheGallery.SouthY, true, true));

            // …and the machines are SOLID, the way the bar's counter is a real wall you belly up to rather
            // than a picture you walk through. Face and two flanks; for the two BOLTED to the back wall the
            // fourth side is the station's own stone, laid above. They are drawn as filled blocks below
            // (DeckPlan.FurnitureSpot, #868's grammar) so the walked room and the drawn room are one room —
            // this repository's third named bug class, with a drinks cabinet in it.
            //
            // #1199 (2026-09-19) · AND THE ISLAND GETS ITS FOURTH SIDE. The third machine stands clear of
            // every wall in the room, so the side the other two share with the station has to be built for
            // it — and it is the load-bearing one: it is the face the throat looks at, and a machine you can
            // see straight through is a machine nobody can walk behind. The test is the box's own east edge
            // against the room's, so nothing here has to know which of the three is the island.
            foreach ((double X0, double Y0, double X1, double Y1) box in TheVendingMachineBlocks(spec.BodyId))
            {
                walls.Add(new((float)box.X0, (float)box.Y0, (float)box.X0, (float)box.Y1, false, false));
                walls.Add(new((float)box.X0, (float)box.Y0, (float)box.X1, (float)box.Y0, false, false));
                walls.Add(new((float)box.X0, (float)box.Y1, (float)box.X1, (float)box.Y1, false, false));
                if (box.X1 < TheGallery.EastX)
                {
                    walls.Add(new((float)box.X1, (float)box.Y0, (float)box.X1, (float)box.Y1, false, false));
                }
            }

            // The plate, a third of the way out along the STEM, so it is read on the way IN and is not
            // sitting on top of whoever is standing at the rail. ONE plate for the whole T: Core's string,
            // never retyped, because the map layer's plate and the card's title come from one place — and
            // because the tube and the gallery are one room and a second plate would say they were two.
            labels.Add((
                TheWalk.MouthX - ((float)ObservationWalk.LengthDu / 3f),
                (TheWalk.NorthJambY + TheWalk.SouthJambY) / 2f,
                ObservationWalk.Plate));
        }
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void HangTheBackRoomLeaves(StationSpec spec, List<DeckPlan.Door> doors, List<DeckPlan.ConsoleSpot> hatches)
    {
        // Two locked back-room hatches off the bar — more of the place you can't get into (yet), and since
        // #973 L0 the two leaves people come OUT of. The records are BarBackRoomLeaves' and not typed again
        // here: a walker's plate and the plate the captain is refused at are one string, by construction.
        UndergroundComplex.LockedDoor[] backRooms = BarBackRoomLeaves(spec.Authority[0]);
        foreach (UndergroundComplex.LockedDoor leaf in backRooms)
        {
            doors.Add(new((float)leaf.X1, (float)leaf.Y1, (float)leaf.X2, (float)leaf.Y2, Locked: true));
            // The knockable panel sits two du INTO the room from its leaf, on whichever side wall it hangs on.
            float inward = leaf.X1 <= BarLeft ? 2f : -2f;
            hatches.Add(new(DeckPlan.ConsoleKind.Hatch,
                (float)leaf.X1 + inward, (float)((leaf.Y1 + leaf.Y2) / 2), leaf.Sign));
        }
    }
}
