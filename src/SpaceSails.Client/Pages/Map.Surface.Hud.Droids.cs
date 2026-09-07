using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE DROID BUFFER — the ship's crew, plus the live Old Ones on the surface, packed into the one array
/// the renderer walks.
///
/// <para>Split out of <c>Map.Surface.Hud.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ── The droid buffer: the ship's crew, plus the live Old Ones on the surface. ──

    private void FillSurfaceDroids(double simTime, DeckPlan.Droid[] buffer)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // [0..3): the crew
        // #633 · FOUR BANDS, NOT THREE, AND THEY MAY NOT OVERLAP. `main` put the sweep team at
        // `3 + ReeverEngineCeiling` because on that branch nothing else lived there; this branch had already
        // given those slots to the repo crew (#583). Reunited without this line the collectors' loop below
        // would overwrite all three sweepers every frame — one buffer written by two fillers, which is the
        // named bug class and exactly the thing a merge produces. The bands are stated ONCE, here and in
        // SurfaceDroidCount, and every filler is offset from the one before it.
        FillSweeperDroids(buffer, 3 + ReeverEngineCeiling + MaxCollectors);
        // #804 · …and the rounds, in the band after the sweepers. Their filler applies the sightline gate
        // itself: a guard the captain cannot see is parked off-map exactly as an unseen Old One is.
        FillPatrolDroids(buffer, PatrolFirstSlot);
        // #731 · …and the people who are leaving, or who have come out of a door to sit at your table. Their
        // own band after the rounds, drawn with the ordinary NPC pen because they are ordinary people.
        FillWalkerDroids(buffer, WalkerFirstSlot, _surface?.Walkers ?? []);
        for (int i = 0; i < ReeverEngineCeiling; i++)
        {
            int slot = 3 + i;
            // #371 Phase 3 (expedition fog): a behind-cover Old One is NOT drawn on the walked map — parked
            // off-screen exactly like an empty slot. VisibleOnMap is always true off an expedition site, so
            // Miranda and the moons draw every contact as before. The motion tracker (which reads _reevers
            // directly, not this buffer) still hears it through the wall — untouched.
            if (i < _reevers.Count && _reevers[i].VisibleOnMap)
            {
                Reever r = _reevers[i];
                buffer[slot] = new DeckPlan.Droid(r.X, r.Y, r.Facing, "Reever");
            }
            else
            {
                buffer[slot] = new DeckPlan.Droid(-9999, -9999, 0, "Reever");
            }
        }

        // #583 · And the repo crew, in their own slots after the Old Ones. Drawn as people, named so the
        // renderer can give them their own ink — they are not hostiles of the same kind and should not read
        // as more Reevers on the walked map.
        for (int i = 0; i < MaxCollectors; i++)
        {
            int slot = 3 + ReeverEngineCeiling + i;
            buffer[slot] = i < _collectors.Count
                ? new DeckPlan.Droid(_collectors[i].X, _collectors[i].Y, _collectors[i].Facing, "Collector")
                : new DeckPlan.Droid(-9999, -9999, 0, "Collector");
        }
    }
}
