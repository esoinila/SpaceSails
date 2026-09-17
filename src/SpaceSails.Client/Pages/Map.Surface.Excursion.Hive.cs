using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Surface.Excursion.Hive — WHAT THE VISIT REMEMBERS ABOUT THE PLACE UNDER THE GROUND.
//
// #251 · A contiguous run of Map.Surface.Excursion.cs, moved verbatim: #585's floor and its emptied
// rooms, the locks shot open and the cubicles shut, #758's curtain and door, #719 slice 2's
// maintenance break, #602's keypad (which remembers for exactly as long as this trip) and #1149's
// inspection, plus every plate, sign and hall record the Hive has already shown you once.
//
// TWO ROWS IN THIS RUN ARE NOT THE HIVE'S, and the filename is named for the run rather than
// pretending otherwise: #803's ShotsHeard/GunfireWarned and #688's Ground/HardcaseDrop sit inside
// the #758 block because that is where the base record declared them. Sending them to .Ground to
// sit beside better neighbours would have re-ordered the record, and a re-ordered record cannot
// carry the pure-move proof — a field that travels is a line the diff cannot account for.
//
// Nothing is renamed, re-scoped or re-ordered.
public partial class Map
{
    public sealed partial class SurfaceExcursion
    {
        // ── #585 · THE HIVE. Which floor the captain is on (0 = the surface), and which rooms down there
        //    have already been turned over. Persisted with the excursion, so stepping back into the lift
        //    finds the facility exactly as you left it.
        public int Floor { get; set; }
        public HashSet<int> HiveRoomsEmptied { get; } = [];
        public HashSet<int> HiveFloorsSeen { get; } = [];

        // #803 · …and which of the doors that never open somebody took the hasp off with a sentry
        // (HiveInterior.LockKey). Replayed on every rebuild, exactly the way an emptied room is, so a floor
        // does not grow its wall back while the captain is two rooms away.
        public HashSet<string> LocksShotOpen { get; } = [];

        // #821 · …and which WC cubicles have the catch over (HiveInterior.CubicleKey). Replayed on every
        // rebuild for the very same reason, and kept on the EXCURSION rather than in the vault: a catch is a
        // thing a hand is holding shut, and a save that restored a locked cubicle on a floor the captain is
        // no longer standing in would be the building keeping a secret nobody is behind any more.
        public HashSet<string> CubiclesShut { get; } = [];

        // ── #758 · THE CURTAIN AND THE DOOR ────────────────────────────────────────────────────────────
        //
        // Which cabinets have had the leaf brought out of the wall and dogged (CabinetPrivacy.Key).
        // ABSENT MEANS CURTAIN, which is the state every cabinet in the building is in until somebody
        // decides otherwise — so a fresh excursion needs no seeding, and the deck drawn on the first frame
        // and the strip pressed on the hundredth cannot come to two different views of one leaf.
        public HashSet<string> CabinetsDogged { get; } = [];

        // #758 · …and which of them the COUNTER has already written down. The keep's long memory is written
        // on the transition INTO dogged and exactly once per cabinet: a captain who dogs, undogs and dogs
        // again did one memorable thing, and a book does not un-write a line to make room for a copy of it.
        public HashSet<string> CabinetsWitnessed { get; } = [];

        // #758 · How many sensitive beats have happened in a cabinet this excursion — the beat index the leak
        // roll is seeded on, so two files put down behind one curtain are two rolls rather than one answer
        // said twice.
        public int CabinetBeats { get; set; }

        // #758 · …and WHICH CABINET last got out through the weave, still unspent. A leak is never announced
        // when it happens — that is the whole of the mechanic — so it waits here until somebody who has no
        // way of knowing says the number out loud (CabinetPrivacy.BarkThatKnows), and is spent by being said.
        // Null is a captain nobody has overheard yet.
        public int? CabinetLeaked { get; set; }

        // #821 · How many times a basin has been used this excursion, so two washes are two lines and not
        // one line said twice — the beat the pool is seeded on.
        public int WashBeats { get; set; }

        // #821 · …and whether this watch has already paid its one pip for a wash (CubicleLock.
        // WashPipsPerWatch). A row of four basins is a room, not an income.
        public long WashPaidWatch { get; set; } = long.MinValue;

        // #803 · …and what the shot itself was: the fired-shot facts this ground has heard, in the order
        // they happened. Nothing in this build reads them beyond the field book — the pack's ear is rung by
        // MakeNoise, as it always has been — and #804 prices them.
        public List<GunfireHeard.Shot> ShotsHeard { get; set; } = [];

        // #803 · Whether the captain has been told, once, what a shot indoors actually spends.
        public bool GunfireWarned { get; set; }

        // #688 · WHAT THE CAPTAIN PUT DOWN, AND WHERE. Owner: "no way to drop stuff." Excursion-scoped by
        // deliberate v1 choice — the world does not keep a ledger of every sheet of paper anybody ever set on
        // a floor, and the line the captain reads says as much out loud rather than implying a permanence the
        // sim does not have. Within the walk it is exactly where they left it, which is #615's whole law.
        public LeftBehind Ground { get; } = new();

        // #1061 beat 2 · WHAT BREM KOLT DROPPED WHEN HE RAN — where the sheet is lying (null on a ground he
        // has not bolted from) and whether it has been taken. Excursion-scoped for the store's own reason
        // above, and deliberately NOT in it; the whole argument is in Map.Hardcase.cs.
        public DeckReachability.Point? HardcaseDrop { get; set; }
        public bool HardcaseScheduleTaken { get; set; }

        // #590 · Which shaft bands this excursion has already talked its way into. Only gates the once-per-
        // shaft beat when a card is accepted; the CARD itself is durable and lives in the vault, because a
        // possession that evaporated when the shuttle lifted would not be a possession.
        public HashSet<int> HiveShaftsOpened { get; } = [];

        // And which have already refused you once. The refusal is said EVERY time — a gate that goes quiet
        // on the second press reads as a broken button — but it is only FILED once, because pressing one
        // gate eleven times is not eleven findings.
        public HashSet<int> HiveShaftsRefused { get; } = [];

        // ── #719 slice 2 · THE MAINTENANCE BREAK, AND WHY IT LIVES HERE ─────────────────────────────────
        //
        // Whether somebody has taken the car away from this captain. False is a car running, which is what
        // every excursion starts as and therefore what the next one finds — "nobody files a maintenance
        // ticket against a man who left."
        //
        // ON THE EXCURSION AND NOT IN THE VAULT, and that is the owner's ruling rather than a convenience:
        // it is the pad's own rule (LiftCodeOpened, four lines down) said about a second machine. A radio
        // call is a thing that happened to an afternoon; a shuttle that carried it away would be the
        // building holding a grudge across a launch. Nothing in Map.Vault.BuildVault reaches this class at
        // all, so the ruling is kept by construction and the guard only has to prove that stays true.
        //
        // AND IT IS NOT RESET BY WALKING AWAY. Nothing clears it but arriving on the surface (RideTheLiftTo's
        // own level == 0 arm) — not a floor change, not a refuge, not a watch turning over. A captain who
        // hides in a cubicle for two minutes comes out to the same dead panel, which is what makes the break
        // a price rather than a timer.
        public bool CarStopped { get; set; }

        // ── #602 · THE KEYPAD, WHICH REMEMBERS FOR EXACTLY AS LONG AS THIS TRIP ──────────────────────────
        //
        // ALL FOUR OF THESE ARE ON THE EXCURSION AND NOT IN THE VAULT, and that is the ruling rather than a
        // convenience. A right code opens the gate for the trip you are on and no longer; the card is the
        // durable way in and stays the durable way in — the whole difference between the paper you earned
        // and the number you read off somebody's desk. And the pad's memory is a NINETY-SECOND WINDOW, a
        // building that tolerates the curious and reacts to the persistent, so a counter that survived a
        // shuttle would be the opposite of what the owner ruled.

        // Which bands the pad has been talked into. Keyed by band, the way HiveShaftsOpened above is.
        public HashSet<int> LiftCodeOpened { get; } = [];

        // What the pad remembers: when the run of misses started, and how many stand in it. The arithmetic
        // is Core's (UndergroundComplex.LiftCode) — this is only where it is kept.
        public UndergroundComplex.LiftCode.Pad LiftPad { get; set; } = UndergroundComplex.LiftCode.Pad.Fresh;

        // The digits keyed so far, at most four. Cleared by every press of ↵, right or wrong.
        public string LiftPadEntry { get; set; } = "";

        // What the pad last said — one of the four plates, or null before anything has been pressed. A
        // receipt of the last press rather than a state of the lock, which is why it is a string here and
        // arithmetic there: OPEN is not a thing the pad remembers, it is a thing the pad answered.
        public string? LiftPadSaid { get; set; }

        // ── #1149 · THE INSPECTION, WHICH LASTS EXACTLY AS LONG AS THIS TRIP ────────────────────────────
        //
        // Whether a man on the rota has read the inspector's card and accepted that an inspection is
        // happening here (Inspectorate; WalletChoice.Outcome.Inspection). While it is true the ID CHECK row
        // defers and the SEALED row opens; when the shuttle lifts, it is over.
        //
        // ON THE EXCURSION AND NOT IN THE VAULT, and that is #602's own ruling directly above rather than a
        // new one: the CARD is durable and stays durable — it is a possession, and a possession that
        // evaporated when the shuttle lifted would not be one — while the VISIT is an afternoon. An
        // inspection that survived a launch would be a building that never stopped expecting somebody.
        //
        // It is also the once-per-excursion latch for the one authored sentence: the line is said on the
        // read that SETS this, and a man who has already been told there is an inspection on does not
        // announce it to the captain a second time (PatrolBeat.TheGuardReads).
        public bool InspectionRunning { get; set; }

        // #609 · Whether this excursion has had the DEAD AIR card. Once: after that the pulse line is
        // enough, because by then it is knowledge rather than news.
        public bool HiveVacuumWarned { get; set; }

        // #592 · Whether this excursion has already had the floor-with-no-plate beat. Once is the whole
        // point: the second time you step out down there it is just a corridor, and it should be.
        public bool HiveUnlistedSeen { get; set; }

        // #725 · Whether this excursion has already had THE PLATE card, and whether it has already had THE
        // STAFF MESS. Two flags in the DEAD AIR family and for its reason: the first time is the find and
        // every time after is a lobby and a canteen, which is exactly what they should become. Excursion-
        // scoped like every one of their siblings above — a captain who lands again is walking in for the
        // first time again, and that is the same ruling the vacuum warning already makes.
        public bool HiveUnlistedPlateShown { get; set; }
        public bool HiveStaffMessShown { get; set; }

        // #751 · …and the two rooms the hall rule adds, in the same family and with the same latch
        // discipline. THE HALL is the B1 cantina walked into for the first time; THE CABINET is ANY of the
        // three doors along its back wall, once TOTAL and never once per door — three identical cards in a
        // row would spend the beat on the second one. The field book's own line about a cabinet files
        // alongside the card, off the same latch, so they can never double up or race.
        public bool HiveCantinaHallShown { get; set; }
        public bool HiveCabinetShown { get; set; }

        // #759 · …and the park behind the hall's glass. ATTENDANCE IS RECORDED is what the plate at the gate
        // says, so the book records that you were there, once, and then the poll stands down. The
        // surveillance is a LINE and not a system — nothing counts anything, and nothing ever refers to it
        // again.
        public bool HiveParkNoteFiled { get; set; }

        // #677 · Whether this excursion has already crossed the seam, and already stepped out into the
        // halls. Two flags and not one, because they are two different events on the same ride and either
        // can happen without the other on a later trip — a captain who rode straight down on a card they
        // were already carrying crosses the seam without the shaft ever having been a mystery.
        public bool HiveSeamCrossed { get; set; }
        public bool HiveFoundSeen { get; set; }

        // #677 · …and whether the wall has already been raised as a card. There are several records in a
        // band and they are all the same wall, so the card and the casebook gist are once per excursion the
        // way the authority card's are — while the POCKET line is said every time, because something goes in
        // every time and #678's law is that a pickup line is printed for something that actually went in.
        public bool HiveHallRecordShown { get; set; }

        // #528 · Whether this excursion has already had the two reveal cards the Hive earns — the sealed way
        // on, and the first authority card. Once each: a card that pops at every sealed door in a corridor
        // of sealed doors is a slideshow, and the second one is never the beat the first one was.
        public bool HiveSealedWayShown { get; set; }
        public bool HiveAuthorityShown { get; set; }

        // #707 · Which amenity rooms this excursion has already written up, by the same room key the haul
        // rooms use. The plate is pulsed every time you stand at the counter — a console that goes silent
        // reads as broken (#212) — but the write-up is filed ONCE, because leaning on a bar eleven times is
        // not eleven findings. Exactly the shape the refused-shaft line above already uses.
        public HashSet<int> HiveAmenitiesRead { get; } = [];

        // #709 · Which of the canteen's regulars this excursion has already heard. Keyed off the same room
        // key the amenities use, offset clear of any real room index, so one person's breath is filed once
        // and the plate pulses on every visit after.
        public HashSet<int> HiveRegularsHeard { get; } = [];

        // #709 · Which notice on the cork board comes next. A counter and not a set, because the board is the
        // one thing down here worth re-reading in ORDER — four notices, one per press, round and round.
        public int HiveBoardNext { get; set; }

        // #709 · WHICH SHIFT the canteen's people are on. Owner: "let's have some random element of who is in
        // the bar and where they got to sit down."
        //
        // Frozen ONCE, when this excursion's underground floor is drawn, and read by everything afterwards.
        // The roster turns over with the watch (PatronRota's own, upstairs) — but the deck is built at one
        // instant and the [E] press happens at another, so reading the clock a second time would let the
        // figure on screen and the person the game answers about be two different people. That is bug class
        // three with a face on it, and a watch chosen once cannot drift into it.
        public long CanteenWatch { get; set; }
    }
}
