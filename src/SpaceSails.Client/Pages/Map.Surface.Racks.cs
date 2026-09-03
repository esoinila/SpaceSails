using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #563 slice 3 · THE SHELTERS,
// ON EVERY TILE, AND THE RACK STATE THAT HAD TO BE ADDRESSED BEFORE THEY COULD BE.
//
// Slice 2 shipped doors, drawers and hut memory out into the lattice and stopped at the shelters, with the
// reason written out rather than waved at:
//
//     ex.ShelterReservoir, ShelterPumpNoted and ShelterUnderfoot are all keyed on an int index into
//     SheltersOn(ex), which is ONE SITE'S list. Making that list span a moving chunk would silently
//     re-point every reservoir key each time the captain crossed a tile boundary — the same failure the
//     huts had, in the one system where getting it wrong empties a rack somebody walked to on their last
//     two hundred seconds of air.
//
// That is the whole of this file. A rack is addressed now — (tile, index), the shape GroundMemory already
// keys a hut on — so the list is free to be the lattice's rather than one field's, and a captain who walks
// three tiles out, watches the chunk behind them evict, and walks back finds the rack they drew on exactly
// as they left it rather than finding the tube's fourth shelter wearing its number.
//
// WHY NONE OF IT RIDES THE VAULT, which is a deliberate answer and not an omission. A rack REFILLS ITSELF:
// SurfaceShelter.RechargeSeconds is 120 s, on the owner's own ruling that stranded must not mean dead. Any
// lift-off and return is orders of magnitude longer than that, so a rack found drawn down on the next visit
// would be the file contradicting the machine — the world quietly telling a story its own numbers say is
// over. What must survive a visit is the SEEDED half, and it does so by regeneration rather than by storage:
// SurfaceShelter.SomebodyWasHere is asked on the tile's own contents salt, so "a rack that is not full means
// somebody was here" is a fact about that ground, identical before and after any save, for ever.
public partial class Map
{
    /// <summary>#563 slice 3 · WHICH RACK. A tile address and an index into that tile's own shelter list —
    /// never an index on its own, which is the bug this slice exists to make impossible.
    ///
    /// <para><see cref="Index"/> below zero means "no shelter", which is what the bare int meant before and
    /// keeps every caller's shape; <see cref="Found"/> is the same question asked in words.</para></summary>
    private readonly record struct ShelterSpot(SurfaceTiles.Address Tile, int Index)
    {
        /// <summary>Nowhere — the answer on every square of open regolith, which is nearly all of them.</summary>
        public static ShelterSpot Nowhere => new(SurfaceTiles.Home, -1);

        /// <summary>Is the captain actually in one?</summary>
        public bool Found => Index >= 0;
    }

    /// <summary>#585/#563 · The shelters on ONE tile, worked out once and remembered for the visit.
    ///
    /// <para>#585's reason for the cache is unchanged and is the reason this is a dictionary rather than a
    /// call: <c>SurfaceShelter.SpecsFor</c> is pure but not free — it re-runs the seeded placement, up to
    /// nine drums over thirty hashed candidate spots each, with a separation check against everything placed
    /// so far — and the threshold rule (#585) asks it once PER OLD ONE PER FRAME. What changed is that the
    /// answer is per TILE now, so the cache is keyed the way the question is.</para>
    ///
    /// <para>Determinism is what makes it safe: same body, same salt, same address ⇒ the same list, every
    /// time. Cleared with the excursion, so a new site recomputes.</para></summary>
    private IReadOnlyList<SurfaceStructure.Spec> SheltersOnTile(SurfaceExcursion ex, SurfaceTiles.Address a)
    {
        if (ex.ShelterSpecs.TryGetValue(a, out IReadOnlyList<SurfaceStructure.Spec>? cached))
        {
            return cached;
        }

        // A hull has no regolith to seed and no shelters on it; that has been true since #585 and is asked
        // here so that every road into a shelter passes the same gate.
        IReadOnlyList<SurfaceStructure.Spec> specs =
            Derelict.TryParseWreckId(ex.Stop.Body.Id, out _) || !SurfaceTiles.WithinBackstop(a)
                ? []
                : SurfaceTiles.Shelters(ex.Stop.Body.Id, ex.Site.LayoutSalt, a);
        ex.ShelterSpecs[a] = specs;
        return specs;
    }

    /// <summary>Every shelter on the ground the excursion is carrying, addressed. Home tile first, then the
    /// rest of the chunk — <see cref="TilesUnderfoot"/>'s order, so the beacons, the threshold rule and the
    /// cheat all walk the same ground the renderer just drew.</summary>
    private IEnumerable<(ShelterSpot Where, SurfaceStructure.Spec Spec)> SheltersInReach(SurfaceExcursion ex)
    {
        foreach (SurfaceTiles.Address a in TilesUnderfoot(ex))
        {
            IReadOnlyList<SurfaceStructure.Spec> specs = SheltersOnTile(ex, a);
            for (int i = 0; i < specs.Count; i++)
            {
                yield return (new ShelterSpot(a, i), specs[i]);
            }
        }
    }

    /// <summary>Which tile a point on this ground belongs to, for the purpose of asking what is built on it.
    ///
    /// <para>A ground that is not a lattice — a derelict's deck, an away-expedition site, a deflection — is
    /// one authored field that ends where it ends, so every point on it is the home tile's. Asked here once
    /// rather than at each of the three call sites, because a point that resolved to a different tile in one
    /// of them would be a shelter you can walk into and cannot use.</para></summary>
    private static SurfaceTiles.Address ShelterTileAt(SurfaceExcursion ex, double x, double y) =>
        GroundIsALattice(ex) ? SurfaceTiles.At(x, y) : SurfaceTiles.Home;

    /// <summary>Which shelter the captain is standing inside, or <see cref="ShelterSpot.Nowhere"/>.
    ///
    /// <para><b>One tile is asked, not nine.</b> A drum never straddles a tile boundary — its placer keeps
    /// it at least 24 du off every edge of its envelope and its keep-out radius is under 20
    /// (<see cref="SurfaceTiles.Shelters"/> argues it, and a guard measures it) — so the tile under the
    /// boots is the only tile whose shelters could possibly contain them. This is asked once per hunter per
    /// frame by the threshold rule, so nine answers instead of eighty-one is not a micro-optimisation, it is
    /// the reason the lattice can carry shelters at all.</para></summary>
    private ShelterSpot ShelterUnderfoot(SurfaceExcursion ex)
    {
        SurfaceTiles.Address a = ShelterTileAt(ex, _avatarX, _avatarY);
        IReadOnlyList<SurfaceStructure.Spec> all = SheltersOnTile(ex, a);
        for (int i = 0; i < all.Count; i++)
        {
            if (SurfaceShelter.Contains(all[i], _avatarX, _avatarY))
            {
                return new ShelterSpot(a, i);
            }
        }
        return ShelterSpot.Nowhere;
    }

    private bool StandingInTheShelter(SurfaceExcursion ex) => ShelterUnderfoot(ex).Found;

    /// <summary>#585 · Push a body back out of any shelter it has ended up inside. The door reads a suit;
    /// nothing else on this ground gets to be in there. Cheap for the same reason
    /// <see cref="ShelterUnderfoot"/> is: only the tile the body is standing on can be holding it.</summary>
    private (double X, double Y) HoldOutsideShelters(double x, double y)
    {
        if (_surface is not { } ex)
        {
            return (x, y);
        }
        foreach (SurfaceStructure.Spec spec in SheltersOnTile(ex, ShelterTileAt(ex, x, y)))
        {
            (x, y) = SurfaceShelter.HoldAtTheThreshold(spec, x, y);
        }
        return (x, y);
    }

    /// <summary>One key per rack per tile. <see cref="GroundMemory.HutKey"/>'s shape without the body and
    /// the site, because these dictionaries live on the excursion and an excursion is one site — and because
    /// the moment they stopped being a bare int is the moment the bug this slice fixes became impossible to
    /// write.</summary>
    private static string ShelterRackKey(ShelterSpot where) =>
        $"{where.Tile.X}_{where.Tile.Y}:{where.Index}";

    /// <summary>A rack's reservoir right now, in suit-seconds. A rack nobody in this run has touched is full
    /// — unless somebody ELSE drew on it, which is the whole "finding one not full means somebody was here"
    /// story, told by state rather than by a card.
    ///
    /// <para>The seeded roll is asked on the TILE's own contents salt (<see cref="SurfaceTiles.ContentSalt"/>),
    /// which is the site's own salt at the tube and the tile's address key everywhere else. So a rack nine
    /// hundred du out carries its own story rather than a copy of the story of the shelter beside the ship —
    /// and every rack at the tube rolls exactly what it has always rolled.</para></summary>
    private double ShelterReservoirNow(SurfaceExcursion ex, ShelterSpot where)
    {
        if (!where.Found)
        {
            return 0;
        }

        string key = ShelterRackKey(where);
        if (ex.ShelterReservoir.TryGetValue(key, out double held))
        {
            return held;
        }
        double start = SurfaceShelter.SomebodyWasHere(
                ex.Stop.Body.Id,
                SurfaceTiles.ContentSalt(ex.Stop.Body.Id, ex.Site.LayoutSalt, where.Tile),
                where.Index)
            ? SurfaceShelter.ReservoirSeconds * 0.42
            : SurfaceShelter.ReservoirSeconds;
        ex.ShelterReservoir[key] = start;
        return start;
    }
}
