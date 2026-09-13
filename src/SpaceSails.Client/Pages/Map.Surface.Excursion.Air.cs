using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Surface.Excursion.Air — WHAT THE SUIT IS BREATHING, AND OUT OF WHAT.
//
// #251 · A contiguous run of Map.Surface.Excursion.cs, moved verbatim: #325's extended tank (a
// constructor argument, never a settable flag — see the ctor in the opening file for why), the
// once-only lines the air says on its way down, and the two reservoirs a captain can top up from, a
// shelter's and a refuge's.
//
// Nothing is renamed, re-scoped or re-ordered. The one property this run is derived from,
// ExtendedTank, is declared here where the base declared it, and AirSeconds — which the constructor
// sets from it — stays in the opening file where the base declared THAT.
public partial class Map
{
    public sealed partial class SurfaceExcursion
    {
        // ── #325 · THE EXTENDED TANK ──────────────────────────────────────────────────────────────────
        //
        //  Owner, #325 item 5: "extended tanks / spare bottles as purchasable margin ... the tourist shop
        //  becomes an outfitter."
        //
        //  One bit, set at the SHUTTLE, never afterwards: a tank is fitted to a suit before it goes down,
        //  and a captain cannot walk back to the ship's stores from four thousand du out. It is taken by the
        //  constructor and GET-ONLY for exactly that reason — the compiler refuses the bug rather than a
        //  comment asking nicely.
        //
        //  Everything else about the tank is derived from it through the one function, never stored: the
        //  budget, the lattice's extent, the backstop radius, the meter's full mark and the rack's fill cap
        //  all ask AirBudgetSeconds. Nothing caches the answer, because a cached copy of a fact is a second
        //  source of that fact and this file already says so about the huts eight screens up.
        public bool ExtendedTank { get; }

        /// <summary>#325 · This excursion's play budget — what a FULL suit holds today. The one number the
        /// geometry, the instruments and the sentences all read.</summary>
        public double AirBudgetSeconds => SuitAir.PlayBudget(ExtendedTank);

        // #325 · Whether the suit has already said the bottle is on. One-shot per excursion, the same shape
        // as AirWarned above and for the same reason: the fitting is a FACT said once, not a status line.
        public bool ExtendedTankNoted { get; set; }

        // #573 · The low-air mark is a SEPARATE warning from the point-of-no-return, because in a bounded
        // field the point-of-no-return can never fire at all and the captain would die having been told
        // nothing. Both are one-shot per walk.
        public bool AirLowWarned { get; set; }

        // #573 · Whether the secondary pack's cut-in has been announced. One-shot, re-armed by a refill.
        public bool ReserveNoted { get; set; }

        // #573 · Whether "you can hear yourself in the helmet" has been said at the current level of
        // distress. Re-arms once the captain calms down, so it marks a CHANGE rather than nagging.
        public bool HardBreathingNoted { get; set; }

        // #573 · The deep shelter's charging rack: one charge per excursion, then it is dry.
        // #573 · Per-shelter state. #563 slice 3 · KEYED ON THE RACK'S ADDRESS (Map.ShelterRackKey — the
        // tile and the index on it), never on a bare index into one site's list. That bare index is exactly
        // what kept the shelters home-tile-only through slice 2: the moment the shelter list spans a moving
        // chunk, an index re-points on every tile crossing, and a captain would find the rack in front of
        // them reporting the charge of a drum four hundred du away. The huts got this keying in slice 2 and
        // the racks get it here.
        // #573 · Each rack's reservoir, in suit-seconds. Absent = never visited, so it is full (or partly
        // drawn by somebody else — see SurfaceShelter.SomebodyWasHere). Always producing, never "spent".
        public Dictionary<string, double> ShelterReservoir { get; } = [];
        public HashSet<string> ShelterPumpNoted { get; } = [];

        // #608 · The same three pieces of state for the underground refuges, kept SEPARATE rather than
        // sharing the shelter dictionaries. The two are indexed differently — a shelter is an index into a
        // site's shelter list, a refuge is an index into a FLOOR's — so one dictionary would have B3's
        // refuge and the site's fourth shelter arguing over the same key, and the captain would find a rack
        // mysteriously drawn down by a building they have never been in.
        public Dictionary<int, double> RefugeReservoir { get; } = [];
        public HashSet<int> RefugePumpNoted { get; } = [];
        public bool RefugeBreathNoted { get; set; }
    }
}
