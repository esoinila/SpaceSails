using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #563 · THE TREADMILL — the landing-site ground as ADDRESSED TILES rather than a fenced field.
///
/// <para>Owner, 2026-08-01, after watching the wandering bound land: <i>"suppose the player wants to just
/// walk left... can we just not add a map tile there and take one out from right? We don't actually need to
/// make the web page bigger .. we just need a type of scrolling effect and separation of the graphic and the
/// information model underneath?"</i> — and the image that named it: <i>"it is a bit like this Virtual
/// reality Carpet ... you can walk in any direction on it but actually you can stand still in the room you
/// play in."</i></para>
///
/// <para><b>There is no scrolling effect to build.</b> Surfaces already run under <c>DeckPlan.FollowCam</c>,
/// so the viewport already tracks the captain and the canvas never grows. What stopped a captain at the edge
/// was never the window — it was that the INFORMATION MODEL was one rectangle. This file is the model's half
/// of that separation: the ground is a lattice of tiles, each one generated from its own address, and the
/// captain's excursion carries a handful of them at a time.</para>
///
/// <para><b>THE PROPERTY THIS DEFENDS, and it is where the treadmill metaphor stops.</b> On a real
/// omnidirectional treadmill the floor is a belt and where you have been is gone. Here it must not be. The
/// ground a captain walked away from has to still be there on the way back: the cache they buried, the hut
/// they forced, the craters they were navigating by. So tiles are <b>addressed, never recycled</b> — terrain
/// and structures are a pure function of <c>(bodyId, siteSalt, tileX, tileY)</c>, and walking out and back
/// regenerates byte-identical ground. What is recycled is the MEMORY, never the CONTENT.</para>
///
/// <para>Losing that would be subtle and nasty: nothing crashes, the world just quietly becomes wallpaper,
/// and the buried-treasure loop breaks in a way that reads as a save bug. <c>TheTreadmillTests</c> generates
/// a tile, throws it away, regenerates it and compares byte for byte.</para>
///
/// <para><b>The home tile is the old field, unchanged.</b> Tile (0, 0) is exactly
/// <see cref="SurfaceLayout.DefaultField"/> and its ground is exactly what <see cref="SurfaceLayout.For"/>
/// has always laid — same call, same salt, same walls. Nothing at the tube moves, which is the one thing
/// this change must not do; every other tile is new ground that did not exist before.</para>
/// </summary>
public static class SurfaceTiles
{
    /// <summary>One tile's width in deck units — the old field's width, so tile (0, 0) IS the old field and
    /// the canon ground is preserved by construction rather than by care.</summary>
    public static double TileWidthDu =>
        SurfaceLayout.DefaultField.RightX - SurfaceLayout.DefaultField.LeftX;

    /// <summary>One tile's height in deck units — the old field's height, for the same reason.</summary>
    public static double TileHeightDu =>
        SurfaceLayout.DefaultField.TopY - SurfaceLayout.DefaultField.BottomY;

    /// <summary>A tile's address on the lattice. (0, 0) is the ground under the tube; +x is starboard,
    /// +y is back toward the landing band (so the deep is negative y, the same sense the field always
    /// used). This — not a position, not an index into a list — is what everything on a tile is keyed
    /// on, because an address survives being forgotten and a list index does not.</summary>
    public readonly record struct Address(int X, int Y);

    /// <summary>The ground under the tube: the old fenced field, byte for byte.</summary>
    public static Address Home => new(0, 0);

    /// <summary>How many tiles out from the captain's own tile are carried at once. Three by three: the
    /// tile the captain stands on plus every tile they could reach by walking off any edge of it. One is
    /// the smallest ring that guarantees ground under every step — with a radius of zero a captain walks
    /// off the world at the first tile boundary, and every larger ring is memory bought for nothing.</summary>
    public const int ChunkRadius = 1;

    /// <summary>#563 law 7 · THE BACKSTOP — how far from the tube the world is finally allowed to stop.
    ///
    /// <para>The wandering bound (#565) used to be the edge of the world. It is a backstop now: something
    /// still has to catch a captain who walks in one direction for an hour, but it must sit far enough out
    /// that no ordinary excursion ever meets it.</para>
    ///
    /// <para><b>The number is read off the tether, not chosen.</b> A full tank is
    /// <see cref="SuitAir.TankSeconds"/> at <see cref="SuitAir.WalkSpeedDu"/>, so a captain walking dead
    /// away from the tube runs the tank completely dry exactly as they arrive here — having crossed the
    /// point of no return at rather less than half this distance (<see cref="SuitAir.PastPointOfNoReturn"/>
    /// bites once the walk home costs more than what is left, around 5,000 du). So the walk back is already
    /// lethal more than twice over before the bound is anything but a rumour, which is the whole
    /// requirement: air stops you, geometry never gets the chance.</para></summary>
    public static double BackstopRadiusDu => SuitAir.TankSeconds * SuitAir.WalkSpeedDu;

    /// <summary>Which tile a point on the ground belongs to.</summary>
    public static Address At(double x, double y)
    {
        SurfaceLayout.Field home = SurfaceLayout.DefaultField;
        return new Address(
            (int)Math.Floor((x - home.LeftX) / TileWidthDu),
            (int)Math.Floor((y - home.BottomY) / TileHeightDu));
    }

    /// <summary>A tile's rectangle on the ground. Half-open in the same sense <see cref="At"/> is: a point
    /// exactly on the right or top edge belongs to the next tile along.</summary>
    public static (double LeftX, double RightX, double BottomY, double TopY) Rect(Address a)
    {
        SurfaceLayout.Field home = SurfaceLayout.DefaultField;
        double leftX = home.LeftX + (a.X * TileWidthDu);
        double bottomY = home.BottomY + (a.Y * TileHeightDu);
        return (leftX, leftX + TileWidthDu, bottomY, bottomY + TileHeightDu);
    }

    /// <summary>The northernmost row of tiles. There is none above it, and the reason is not arbitrary: the
    /// home tile's top edge IS THE SHIP'S OWN UNDERSIDE — the hull the captain just walked out of, the one
    /// edge <see cref="SurfaceEdge.Side"/> has always deliberately left straight and visible because it is a
    /// real object. The ground runs port, starboard and deep, as far as anybody can walk; it does not run up
    /// through the shuttle.</summary>
    public const int TopRow = 0;

    /// <summary>Is this tile part of the world? A tile above <see cref="TopRow"/> is behind the ship, and a
    /// tile whose nearest corner is beyond <see cref="BackstopRadiusDu"/> is past the backstop. Neither is
    /// ever generated.</summary>
    public static bool WithinBackstop(Address a)
    {
        if (a.Y > TopRow)
        {
            return false;
        }
        (double leftX, double rightX, double bottomY, double topY) = Rect(a);
        (double hx, double hy) = TubeMouth();
        double dx = Math.Max(0.0, Math.Max(leftX - hx, hx - rightX));
        double dy = Math.Max(0.0, Math.Max(bottomY - hy, hy - topY));
        return (dx * dx) + (dy * dy) <= BackstopRadiusDu * BackstopRadiusDu;
    }

    /// <summary>The northern rim of a top-row tile that is NOT the home tile — the line the landing band's own
    /// edge continues along once you have walked out from under the shuttle. The home tile draws this itself
    /// (as real hull, because there it IS hull); out here there is no object to be, so it collides and is
    /// never painted, exactly as the field envelope used to. Null for any tile that is not on the top row.
    ///
    /// <para>It stays STRAIGHT on purpose. The owner's ruling was about the side and deep edges — <i>"the
    /// landing site out-doors should not [have borders]... at least not obviously so with square area"</i> —
    /// and the top was carved out of it from the start because it is the ship. This is that same edge, going
    /// on for as long as the row does.</para></summary>
    public static (double X1, double Y1, double X2, double Y2)? NorthRim(Address a)
    {
        if (a.Y != TopRow || a == Home)
        {
            return null;
        }
        (double leftX, double rightX, double _, double topY) = Rect(a);
        return (leftX, topY, rightX, topY);
    }

    /// <summary>Where the way home stands — the tube mouth on the home tile, on the landing band. Every
    /// distance in THIS file is measured from here: the backstop's radius, and which tiles are within it.
    ///
    /// <para>#563 slice 2 · <b>It is not quite the point the SUIT measures from,</b> and saying so is worth
    /// more than pretending otherwise. The tank and the tracker measure to <c>MoonSurface.SpawnY</c> — the
    /// square just outside the tube's surface door, where the boots actually land — which is 5.5 du up-field
    /// of the landing band this returns. Both are honest answers to "where is the way home", they are the
    /// two ends of the same tube, and at a backstop ten thousand eight hundred du out the difference is five
    /// parts in ten thousand. What matters is that neither is a COORDINATE the game grades danger by (#453):
    /// they are both the route, and the route is what the suit prices.</para></summary>
    public static (double X, double Y) TubeMouth() =>
        (SurfaceLayout.DefaultField.HomeX, SurfaceLayout.DefaultField.LandingBandY);

    /// <summary>The field envelope one tile's ground is generated inside.
    ///
    /// <para>The home tile hands back <see cref="SurfaceLayout.DefaultField"/> UNCHANGED — same numbers, same
    /// anchor, same landing band — so the canon ground regenerates byte for byte. Every other tile gets the
    /// same envelope translated onto its own rectangle, with its own seeded anchor (so each tile out there
    /// has a deep fixture of its own to walk toward) and its landing band pushed to the tile's top edge (a
    /// tile out in the world has no landing band; leaving one there would have carved an empty stripe across
    /// the lattice every 260 du, which is a rectangle drawn in a softer pencil).</para></summary>
    public static SurfaceLayout.Field GenerationField(string bodyId, string siteSalt, Address a)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        if (a == Home)
        {
            return SurfaceLayout.DefaultField;
        }

        (double leftX, double rightX, double bottomY, double topY) = Rect(a);
        string key = Key(bodyId, siteSalt, a);
        // The tile's own deep fixture and its own reserved "way home" column, seeded per address and kept
        // off the tile's own margins so nothing is generated half outside its tile.
        double inset = SurfaceLayout.EdgeMargin + SurfaceLayout.AnchorReserveRadius;
        double anchorX = Lerp(leftX + inset, rightX - inset, Frac(key, "anchor:x"));
        double anchorY = Lerp(bottomY + inset, topY - inset, Frac(key, "anchor:y"));
        double homeX = Lerp(leftX + inset, rightX - inset, Frac(key, "home:x"));

        return new SurfaceLayout.Field(
            LeftX: leftX, RightX: rightX, TopY: topY, BottomY: bottomY,
            LandingBandY: topY, AnchorX: anchorX, AnchorY: anchorY, HomeX: homeX);
    }

    /// <summary>The seeded key one tile's ground is generated from — <c>(bodyId, siteSalt, tileX, tileY)</c>
    /// and nothing else, which is the whole law of this file in one line.</summary>
    public static string Key(string bodyId, string siteSalt, Address a)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return $"{bodyId}~{siteSalt}~t{a.X}_{a.Y}";
    }

    /// <summary>#563 slice 2 · THE SALT A TILE'S CONTENTS ARE SEEDED FROM — what is IN the buildings, which
    /// of their doors somebody paid to import, which papers are in which drawer.
    ///
    /// <para>The home tile answers with the site's own salt UNCHANGED, so every roll the ground under the
    /// tube has ever made comes out the same. Every other tile answers with its own address key, so a ruin
    /// out in the world holds its own things rather than a copy of the home tile's — the same law
    /// <see cref="Ground"/> and <see cref="Terrain"/> already obey, stated ONCE so that the code which lays
    /// a tile's contents and the code which later hands them over cannot ask the question two different
    /// ways. That is this project's fourth named bug class, and it costs a whole console when it lands.</para></summary>
    public static string ContentSalt(string bodyId, string siteSalt, Address a)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return a == Home ? siteSalt : Key(bodyId, siteSalt, a);
    }

    /// <summary>ONE TILE'S GROUND. Pure in <c>(bodyId, siteSalt, address)</c>: walls, doorways, buildings
    /// and the deep fixture. Generate it, forget it, generate it again — the same bytes come back.
    ///
    /// <para>The home tile is routed through <see cref="SurfaceLayout.For"/> exactly as it always was, so
    /// Miranda's monolith maze, Luna's rails and Phobos's rim are untouched and every keep-out ledger
    /// (shelters, the hidden lab, the monolith, the landing square) still applies where it means something.
    /// A tile out in the world has no landing, no shelter beacons and no hidden lab, so it is laid by the
    /// same seeded rubble-and-buildings generator every non-authored site has used since #320, on its own
    /// key.</para></summary>
    public static SurfaceLayout.Plan Ground(string bodyId, string siteSalt, Address a)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        if (a == Home)
        {
            return SurfaceLayout.For(bodyId, SurfaceLayout.DefaultField, siteSalt);
        }
        return SurfaceLayout.SeededGround(Key(bodyId, siteSalt, a), GenerationField(bodyId, siteSalt, a));
    }

    /// <summary>ONE TILE'S TERRAIN — #570's scenery layer, per tile instead of per field. Drawn, never
    /// collidable, so it is free to run right up to the tile's own edges and cover the seam the generated
    /// walls have to leave clear.</summary>
    public static IReadOnlyList<SurfaceScenery.Mark> Terrain(string bodyId, string siteSalt, Address a)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        if (a == Home)
        {
            return SurfaceScenery.For(bodyId, siteSalt, SurfaceLayout.DefaultField);
        }
        return SurfaceScenery.For(bodyId, Key(bodyId, siteSalt, a), GenerationField(bodyId, siteSalt, a));
    }

    /// <summary>A door hung in one of a tile's own doorways, and whether somebody paid to fly it here.</summary>
    public readonly record struct HungDoor(double X1, double Y1, double X2, double Y2, bool Imported);

    /// <summary>A building on a tile that still has something in it — which index it is, where its middle
    /// is, and what is in the drawer.</summary>
    public readonly record struct Drawer(int Index, double X, double Y, SurfaceSalvage.Find Find);

    /// <summary>
    /// #563 slice 2 · WHAT IS HUNG IN ONE TILE'S DOORWAYS.
    ///
    /// <para>Owner, #573, walking the ground: <i>"there seemed to be shelter like spaces that were just
    /// missing the services and the doors.... let's fix those."</i> The buildings had openings the whole
    /// time — the generator hands them back — and a thick-walled ruin with a gap in it reads as an
    /// unfinished shelter rather than somewhere people used to live.</para>
    ///
    /// <para>#592 · One in seven is IMPORTED, off the palette: <i>"some special color not distinctive to the
    /// site could then be used to draw our attention to a place (like expensive door made with far away
    /// imported materials)."</i> Rare on purpose — a signal that fires on every ruin is wallpaper — and
    /// seeded, so the room worth breaking into is a fact about the ground rather than a fresh die.</para>
    ///
    /// <para><b>Here, in Core, because two grounds ask it.</b> The home tile's build (the client's
    /// <c>MoonSurface</c>) and the lattice's tile compose both hang these, and a rule expressed twice is
    /// this project's fourth named bug class — the version that lands is the one where one of the two gets
    /// edited. The home tile answers on the site's own salt (<see cref="ContentSalt"/>), so every door the
    /// ground under the tube has ever had comes out the same colour it always was.</para></summary>
    public static IReadOnlyList<HungDoor> Doors(string bodyId, string siteSalt, Address a)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        string contents = ContentSalt(bodyId, siteSalt, a);
        var hung = new List<HungDoor>();
        int index = 0;
        foreach (SurfaceLayout.Doorway d in Ground(bodyId, siteSalt, a).Doorways ?? [])
        {
            bool imported = DiceRule.Roll(
                DiceRule.Seed($"imported-door:{bodyId}:{contents}:{index++}"), 7).Face == 1;
            hung.Add(new HungDoor(d.X1, d.Y1, d.X2, d.Y2, imported));
        }
        return hung;
    }

    /// <summary>
    /// #563 slice 2 · WHAT IS STILL IN ONE TILE'S BUILDINGS — the buildings worth pressing [E] in, and what
    /// each one holds.
    ///
    /// <para>Owner, on the whole reason for the structure generator: <i>"the idea is that we can then use
    /// those places to have supplies and clues we can find on the way to somewhere. I want the illusion of a
    /// big world, even if it is generated with random seed and some code."</i> That illusion is exactly what
    /// a lattice of walls with nothing in it destroys.</para>
    ///
    /// <para>About half of them hold something and the empty ones are load-bearing (#573): if every building
    /// paid out, walking into them would stop being a decision and become a chore performed on all of them.
    /// The weighting is <see cref="SurfaceSalvage"/>'s and is untouched — what this adds is that the
    /// question is asked on the TILE'S contents salt, so a ruin nine hundred du out holds its own wallet
    /// rather than a copy of the one beside the tube.</para></summary>
    public static IReadOnlyList<Drawer> Drawers(string bodyId, string siteSalt, Address a)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        string contents = ContentSalt(bodyId, siteSalt, a);
        IReadOnlyList<(double X, double Y)> centres = Ground(bodyId, siteSalt, a).BuildingCentres ?? [];
        var found = new List<Drawer>();
        for (int i = 0; i < centres.Count; i++)
        {
            SurfaceSalvage.Find find = SurfaceSalvage.WhatIsInside(bodyId, contents, i);
            if (find != SurfaceSalvage.Find.Nothing)
            {
                found.Add(new Drawer(i, centres[i].X, centres[i].Y, find));
            }
        }
        return found;
    }

    /// <summary>The tiles carried at once around <paramref name="centre"/> — the chunk. Ordered, so two
    /// callers asking the same question get the same list in the same order and a comparison of two chunks
    /// is a comparison of two lists.</summary>
    public static IReadOnlyList<Address> Chunk(Address centre, int radius = ChunkRadius)
    {
        var ring = new List<Address>();
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                var a = new Address(centre.X + dx, centre.Y + dy);
                if (WithinBackstop(a))
                {
                    ring.Add(a);
                }
            }
        }
        return ring;
    }

    // ── seeding ────────────────────────────────────────────────────────────────────────────────────────
    private const int Resolution = 4096;

    private static double Frac(string key, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"surfacetile:{key}:{tag}"), Resolution).Face;
        return (face - 1) / (double)Resolution;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}
