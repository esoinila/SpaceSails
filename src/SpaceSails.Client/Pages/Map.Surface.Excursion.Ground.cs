using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Surface.Excursion.Ground — WHAT IS OUT THERE ON THE SURFACE WITH YOU.
//
// #251 · A contiguous run of Map.Surface.Excursion.cs, moved verbatim: #583's repo boat (whether one
// is coming, when, and what is painted on it), the ruins turned over this visit, the huts resolved
// per loaded tile, #563's backstop voice and tile stream, #314's sentry bots and the tube rearm, and
// #316's husks and scars — the visit's copy of what the ship's own ground ledger holds.
//
// THE TILE STREAM IS BUILT BY THE CONSTRUCTOR, not by an initialiser, and the constructor stays in
// the opening file. The reason is written beside the property and is the same reason the tank flag
// is a constructor argument: the lattice reaches to the backstop, the backstop is the tether, and
// the tether is not known until the bottle arrives.
//
// Nothing is renamed, re-scoped or re-ordered.
public partial class Map
{
    public sealed partial class SurfaceExcursion
    {
        // ── #583 · THE REPO BOAT. Whether one is coming, when, and what is painted on it — all decided
        //    ONCE, from the heat this captain earned, at the moment the shuttle sets down. ──
        public bool CollectorsComing { get; set; }
        public double CollectorsEtaSeconds { get; set; } = double.PositiveInfinity;
        public string CollectorCallsign { get; set; } = "";
        public bool CollectorsLanded { get; set; }
        public bool CollectorsHailed { get; set; }
        public bool CollectorShelterNoted { get; set; }
        public double CollectorBoatX { get; set; }
        public double CollectorBoatY { get; set; }

        /// <summary>#731 · Whether the writ is settled and the crew are walking back to their own boat.
        ///
        /// <para>The state this scene has never had, and its absence was a bug rather than an omission: the
        /// only two things that ever took a repo crew off a moon were the captain lifting off and the captain
        /// dying, so a captain who PAID was still standing in front of them a frame later and was served
        /// again. Set where an encounter ends (<c>TheirBusinessHereIsDone</c>), read by
        /// <c>TheyFileHomeThroughTheirOwnHatch</c>, and never saved — the whole scene is client-owned and
        /// rebuilt from the seeded roll, exactly like the bodies it is about.</para></summary>
        public bool CollectorsGoingHome { get; set; }

        // How long this excursion has been running, in surface seconds. The boat's ETA is measured against
        // it, so the arrival lands MID-MISSION rather than at the hatch.
        public double SecondsOnTheGround { get; set; }

        // #580 · There is deliberately NO locker state here any more. The old HashSet of spent lockers is
        // what stranded the owner beside an empty one; a shelter now reloads whoever reaches it, every time,
        // so there is nothing left to remember. See SurfaceShelter.LockerRounds for the ruling.

        // #573 · Which ruins have been turned over this visit. A room stays entered once emptied — the walls
        // and the door remain, so it still reads as a place you have been.
        public HashSet<string> RuinsSearched { get; } = [];

        // #573 · Whether the "you are breathing shelter air" line has been said for this visit inside. Reset
        // on stepping out, so coming back in says it again — arriving in a refuge is worth noticing twice.
        public bool ShelterBreathNoted { get; set; }

        // #563 · THE GROUND IS A LATTICE, so a hut is not "the site's hut" any more. Huts is the resolved
        // placement per loaded tile — a cache of a PURE FUNCTION, so it can be dropped and recomputed at
        // will, which is exactly the property that makes it safe to keep on a visit.
        //
        // #563 slice 2 · WHAT THE CAPTAIN DID TO THEM IS NOT HERE ANY MORE, and that is the whole of the
        // slice. Forced / emptied / read were three HashSets on this record, so they were forgotten the
        // moment the shuttle lifted: a hatch shouldered open on one trip was dogged again on the next and
        // the rounds already taken were back on the shelf. They live on the ship's own ledger now
        // (Map._groundMemory / Core GroundMemory), keyed on (body, site, tile, what) and written to the
        // vault. There is deliberately NO copy of them here — a cached copy of a fact is a second source of
        // that fact, and this is the class of state that must not have two.
        public Dictionary<SurfaceTiles.Address, SurfaceOutpost.Placement> Huts { get; } = [];
        public SurfaceTiles.Address? OutpostDoorTile { get; set; }
        public DoorChannel? OutpostDoorChannel { get; set; }

        // #563 law 7 · Has the backstop already refused a step this excursion? One voice per visit, so the
        // line is said once and a boundary a captain can lean on never becomes a nag.
        public SurfaceEdge.BackstopVoice Backstop { get; } = new();

        // #563 · Which tiles are carried right now, and how many times that has changed. Never null: an
        // excursion always stands on ground, even before it has walked a step.
        //
        // #325 · Built by the constructor rather than by an initialiser, because it carries the tile lattice
        // out to the backstop, the backstop is the tether, and the tether is AirBudgetSeconds — which is not
        // known until the tank flag arrives. An initialiser runs first, so every excursion would have carried
        // a standard-tank lattice however many bottles it left with.
        public SurfaceStream Stream { get; }

        public List<SurfaceBot> Bots { get; init; } = [];  // #314: sentries carried + deployed this excursion

        // #562 · The tube rearm in progress: which shouldered bot is being racked, and how far along (0..1).
        // Null whenever nobody is being fed — which is most of the time, including the instant the captain
        // steps out of the tube. Session state only: walking out abandons it, and the rounds already bought
        // are already in the magazine, so there is nothing half-finished to persist.
        public int? RearmBotIndex { get; set; }
        public double RearmProgress { get; set; }
        // #314/#316 · The downed Old Ones this visit can SEE, left where they fell — carrying the sim-time
        // they fell at, because a husk's whole value as a clue is how old it is.
        //
        // #316 law 1 · IT IS NOT WHERE THEY ARE KEPT ANY MORE, and that was the bug. This was the only
        // record of a firefight and lift-off threw it away with the visit, so the footprints died with the
        // shuttle and a captain could never come back and read what had happened in a field. What the GROUND
        // kept lives on the ship's ledger now (Map._groundMemory / Core GroundMemory), keyed on
        // (body, site, tile, position, when) and written to the vault, and this list is SEEDED FROM IT on
        // arrival (SeedTheHusksLeftHere) — so what is drawn on a return visit is what was written on the
        // last one.
        //
        // It is still a list on the visit because it is what the RENDERER walks, and because it holds the
        // ones the ground has no opinion about: a husk on a poured floor two hundred metres down, or on
        // somebody else's steel deck, is not a mark in the regolith. Everything in it that IS a mark in the
        // regolith went through the one writer, so the two cannot disagree.
        public List<GroundMemory.Husk> Husks { get; init; } = [];

        // #316 law 1, second half · …AND THE MARKS THAT ARE NOT BODIES: a hole where one of our ✗ marks used
        // to be, and a sentry somebody else left standing at 00. Same story as the husks above and the same
        // shape — the GROUND holds them (Map._groundMemory, keyed and vaulted) and this is the visit's copy,
        // seeded on arrival (SeedTheScarsLeftHere) because it is what the renderer walks.
        //
        // Nothing on this list is ever written HERE. A rival's visit happens while the captain is in orbit
        // and is filed straight to the ledger (TheRivalsLeftTheirMarks); an excursion only ever reads it.
        public List<GroundMemory.Scar> Scars { get; init; } = [];
        public double FireTimer { get; set; }              // #314: accrues to the SentryBot fire cadence

        // #316 law 2 · Which husks this visit has already been told about, by their ledger key. Presentation
        // state and per-visit by design, the same class as ShelterBreathNoted above: a line said once as you
        // walk over the pile is a scene, and the same line every frame is a nag.
        public HashSet<string> HusksRead { get; } = [];
    }
}
