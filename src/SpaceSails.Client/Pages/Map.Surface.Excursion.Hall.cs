using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Surface.Excursion.Hall — THE PEOPLE WHO ARE ON THEIR FEET, AND WHAT HAPPENED AT WHICH TABLE.
//
// #251 · A contiguous run of Map.Surface.Excursion.cs, moved verbatim: #731's walkers in both
// directions (who stood up and left, who came in and where they sat), #731 v2's escort holding a
// cabinet door open for you, #746's per-table ledger for this watch, #757's table you took alone,
// #784's short rest and what it has already given back, and the mess chit, the kit and the dossier.
//
// Nothing is renamed, re-scoped or re-ordered.
public partial class Map
{
    public sealed partial class SurfaceExcursion
    {
        // ── #731 · THE PEOPLE WHO ARE ON THEIR FEET ───────────────────────────────────────────────────
        //
        // Owner, 2026-08-06: "The NPCs but not reevers could also use the A* if we want to show them leaving
        // a scene etc. If they go behind a door that is locked to us, we use that as 'I guess that concludes
        // the conversation' point in the plot / situation." And the limitation it fixes, in the same breath:
        // "Like on the bar now they have to wait for us to leave before they can sit up… or leave the bar."
        //
        // EXCURSION-SCOPED, and on the excursion rather than on the component for the reason every Hive flag
        // above is: a walk belongs to the trip you are on, and a captain who rides back up and comes down
        // again walks into a room whose shift is being dealt fresh. Nothing here is a second rota — the shift
        // still says who was in the room; these two say who is no longer in their chair, and where their
        // legs currently are.
        //
        // Walkers are LIVE OBJECTS and deliberately not saved: a route half-walked is not a fact about the
        // world, it is a fact about a frame. StoodUp is the fact, and it is the one the deck reads.
        public List<Walker> Walkers { get; } = [];

        // Which tops have already had somebody get up from them this watch — the deck's own draw gate, handed
        // to CanteenRegulars.Tables so a body crossing the floor is never also drawn sitting down.
        public HashSet<int> HallStoodUp { get; } = [];

        // Which of this watch's scheduled departures have already been dealt out, by table ordinal, so a
        // schedule that is re-read every frame cannot send the same person out of the room twice.
        public HashSet<int> HallDeparted { get; } = [];

        // …and the shift's OWN list of who goes and when, worked out once when a watch begins on this floor.
        // Egress.Departures needs the whole floor plan, and UndergroundComplex.Build generates a building
        // from scratch on every call; asking it sixty times a second for an answer the frozen watch has
        // already fixed is Lab 45's lesson with a body walking through it. NULL means "this shift has not
        // been asked yet" — an EMPTY list is an answer (nobody goes), and the two are not spelled the same.
        public IReadOnlyList<Egress.Move>? HallSchedule { get; set; }

        // ── #731 · …AND THE OTHER DIRECTION ───────────────────────────────────────────────────────────
        //
        // Issue #731's second customer: "The B1 canteen: rota turnover made visible." A room that only ever
        // empties is a room being evacuated slowly, and this floor has only ever emptied. The oncoming shift
        // (CanteenRegulars.ComingOnShift) walks in through the same leaves the outgoing one walks out of, on
        // the same one arithmetic (Egress.Arrivals), off the same frozen watch.

        // Which top each newcomer has SAT DOWN AT, by the plate the room draws over their head — written on
        // the frame their legs stop and never on the frame they set off, because a person is not sitting
        // somewhere they are still walking to. Handed to CanteenRegulars.Tables beside HallStoodUp, so the
        // drawn room and the pressed room have one opinion about every chair in it.
        public Dictionary<int, string> HallCameIn { get; } = new();

        // …and who has already been dealt IN this watch, by plate, so a schedule re-read every frame cannot
        // walk the same person out of the same door twice. Keyed on the plate rather than on the top because
        // an arrival's top is allotted by this side and its person is the schedule's.
        public HashSet<string> HallArrived { get; } = new(StringComparer.Ordinal);

        // …and the shift's own list of who turns up, worked out once when a watch begins on this floor, for
        // the reason HallSchedule is. NULL is a question this room has not been asked yet; EMPTY is an answer
        // it gave.
        public IReadOnlyList<Egress.Move>? HallArrivals { get; set; }

        // Which watch and floor the sets above belong to. A shift turning over, or a lift ride, empties
        // them — the room forgetting, which is the same rule the table state upstairs already runs under.
        public long WalkersWatch { get; set; } = long.MinValue;
        public int WalkersFloor { get; set; } = int.MinValue;

        // ── #731 v2 · SOMEBODY IS HOLDING A CABINET DOOR OPEN FOR YOU ─────────────────────────────────
        //
        // Owner, 2026-08-06, on #751's cabinets: "Also it is dramatic telling when our contact wants us to
        // follow them into kabinetti :-D"
        //
        // The scene she was in is over at YOUR table and has not begun at hers: she is on her feet, crossing
        // the hall, and whether it resumes is the captain's legs' business. So the conversation has to be put
        // down somewhere for the length of a walk, and this is that somewhere — six facts, all of them the
        // minimum needed to pick the same conversation back up in a different room.
        //
        // EXCURSION-SCOPED and cleared by the shift turning over, exactly as the walkers beside them are: a
        // captain who rides up and comes back down walks into a hall whose evening is being dealt fresh, and
        // a woman waiting at a door for a conversation nobody remembers is a bug with a face on it.
        //
        // Deliberately NOT on the walker. The walker is a body crossing a floor and knows nothing about bars
        // or quests (that is NpcWalk's own first law); what a particular walk MEANS is this side's business,
        // and the conversation must outlive the walk by exactly one frame — the frame the captain sits down.

        // Which top she is holding the door of, or −1 when nobody is. A CABINET's top, by Core's own ordinal.
        public int EscortCabinetTop { get; set; } = -1;

        // …and which cabinet that is, as the plate beside its door reads — for #758's stage, which is hers to
        // decide (CabinetPrivacy.EscortsStage) and not the captain's.
        public int EscortCabinet { get; set; }

        // The top she got up FROM, so her provenance door is still the one Egress dealt her out of when she
        // gives up waiting and leaves through it.
        public int EscortFromTable { get; set; } = -1;

        // Her plate, as the hall knows her. Carried rather than assumed, because the day a second contact
        // does this the resumed panel must say the right name over the right face.
        public string EscortWho { get; set; } = "";

        // WHAT HAS ALREADY BEEN SAID TO HER. The sitting's own memory (TableTalk.Said), put down for the
        // length of the walk and handed back at the new table — which is what makes the deal move she stood
        // up before making the SAME deal move when you sit down opposite her again.
        public HashSet<string> EscortSaid { get; } = [];

        // When she stood up, in surface seconds. Escort.PatienceSeconds is measured from here: a captain who
        // never follows is answered by her going, through a door that does not open for them (#731 v1's
        // triggered departure), and never by a statue in a doorway for the rest of the shift.
        public double EscortSince { get; set; } = double.NaN;

        // ── #746 · WHAT HAS HAPPENED AT WHICH TABLE, THIS WATCH ───────────────────────────────────────
        //
        // Owner, 2026-08-06: "asking to sit is missing... offer-a-drink needs to matter."
        //
        // Every one of these is keyed "watch:floor:tableIndex" (see Map.Table.cs's TableKey) rather than by
        // position, because a table has an ordinal and a pair of doubles is a guess. WATCH-SCOPED by design:
        // the shift turning over is the room forgetting, which is what makes a fumbled ask survivable and a
        // bought round worth buying NOW rather than banking.
        //
        // Excursion-scoped like every other Hive flag above. The things that must OUTLIVE the walk — the
        // chit, and the name it was written under — are deliberately not here at all: they are in the
        // satchel, which is durable, and CanteenTable.Cover reads them. A "you have cover" boolean kept
        // beside the possession that IS the cover would be this repo's most expensive bug class with a flag
        // on it.
        public HashSet<string> TableRounds { get; } = [];     // a round was bought at that table
        public HashSet<string> TableMoves { get; } = [];      // "key:who:moveId" — moves already made there
        public HashSet<string> TableAskShut { get; } = [];    // a LOUD file closed ask-about-work there
        public HashSet<string> TableHardened { get; } = [];   // an ask was fumbled there (−1 on the next)

        // …and the three facts the NO-AND scatters across the ROOM rather than across one table. Not keyed
        // by table on purpose: the fitter being worth asking and the temp having overheard are the SCENE
        // moving, and the scene is the room.
        public bool TableFitterOpen { get; set; }
        public bool TableTempOverheard { get; set; }
        public bool TableHouseWays { get; set; }

        // ── #757 · AND WHAT HAS HAPPENED AT A TABLE YOU TOOK ALONE ────────────────────────────────────
        //
        // Owner, live in the hall: "I have empty table but I cannot sit down", and then the sharpening the
        // same evening: "Suppose I just want to sit down and wait to be disturbed?"
        //
        // Same key, same watch scope, same reason as the four sets above. HOW MANY BEATS you have sat
        // through at a top is the ROOM's memory of you rather than the conversation's — standing up and
        // sitting down again must not buy a fresh set of dice, because the approach is seeded on the beat
        // and re-rolling by standing up is exactly the "press it again for a better answer" this game
        // refuses everywhere else.
        public Dictionary<string, int> TableWaits { get; } = [];

        // …and whether somebody has already crossed the room to a given top this watch. ONE approach per
        // table per shift: she came over, and whichever way that went, it went. Waving her off is an
        // answer, not a re-roll.
        public HashSet<string> TableApproached { get; } = [];

        // ── #784 · WHAT THE SHORT REST HAS ALREADY GIVEN BACK, THIS WATCH ─────────────────────────────
        //
        // Owner: "Sitting down relaxes and heals" — and, naming the shape, "it is like short rest in TTRPG",
        // which is bounded recovery and not a tap. ShortRest owns the ceiling; these two are the ledger it
        // is measured against.
        //
        // Keyed on the WATCH and deliberately not on the table, because the cap is a fact about the SHIFT.
        // Keyed by table it would be a cap you could reset by standing up and taking the next top along —
        // the same "press it again for a better answer" #757 closed on the approach roll, wearing a chair.
        public Dictionary<long, int> RestPipsEased { get; } = [];   // nerve pips handed back this watch
        public Dictionary<long, int> RestHitsKnit { get; } = [];    // blows knitted this watch

        // #784 → #1016 · THE WRITE-UP REGISTER USED TO LIVE HERE, and it does not any more. It was
        // excursion-scoped ("the BOOK is what is durable"), which quietly made "have I dug this sheet" a
        // question about a WALK: fly home with the paper still in the sleeve and the pen offered to write the
        // same page again, while the book already had it. Owner's ruling of 2026-08-30 — "refactor the
        // working the case etc table options to not be tied to any location" — makes it the CASE's, so it
        // is one page-level set (`_workedUp`, Map.Seated.cs) that rides the vault beside the satchel and the
        // book. There is no second register: every reader and writer goes through the one accessor pair.

        // #743/#746 · Whether the staff mess has already had its chit beat. Once per excursion, in the DEAD
        // AIR family: the first time you show a pass to an empty room and eat is the beat, and every time
        // after it is lunch.
        public bool MessChitBeatShown { get; set; }

        // #752 · …and whether the cage's gate has already read it. Same family, same reason: the first time
        // a piece of paper you talked somebody out of gets you through a door is the beat, and every trip
        // after it is the commute. Not keyed by band — the chit opens exactly one gate (Core's rule), so a
        // set of bands would be a set that never holds two things.
        public bool ChitGateBeatShown { get; set; }

        // #588 · Which rooms' kit this excursion has turned up, and whether the person has assembled.
        public HashSet<int> KitPieces { get; } = [];
        public bool DossierShown { get; set; }

        // #585 · This ground's shelters, worked out once per TILE and remembered. See SheltersOnTile for
        // why this is a field and not a call: the threshold rule asks the question once per hunter per
        // frame, and a tile's answer is fixed for the whole excursion.
        //
        // #563 slice 3 · Per tile, because the shelters are per tile now — the same cache shape Huts above
        // already uses, and for the same reason: a cache of a pure function, keyed the way the function is
        // asked, which is the only kind of cache that cannot go stale.
        public Dictionary<SurfaceTiles.Address, IReadOnlyList<SurfaceStructure.Spec>> ShelterSpecs { get; } = [];
    }
}
