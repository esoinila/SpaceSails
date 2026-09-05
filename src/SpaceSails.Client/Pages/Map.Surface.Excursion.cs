using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Surface.Excursion — THE LIVE EXCURSION'S STATE, and nothing else.
//
// A PURE MOVE out of Map.Surface.cs (#563). Not a refactor: every line below is the line that was
// there, in the order it was in. The reason is the 1,500-line file law (NoSourceFileIsTooLongTests) —
// Map.Surface.cs stood at 1,474 with a 25-line margin, so the treadmill's tile streaming had nowhere
// to go. SurfaceExcursion is the largest self-contained region in the file (a nested record of what
// one visit to one ground is carrying) and it depends on nothing else in the partial, so it is the
// cheapest 600 lines to lift.
public partial class Map
{
    public sealed class SurfaceExcursion
    {
        public required ShuttleStop Stop { get; init; }
        public required string? RestoreHavenId { get; init; }

        // #320 · WHERE on the body we set down — the chosen landing site (seeded set per body, picked in the
        // boarding panel). Its LayoutSalt parameterizes the surface deck-plan (a different site grows a
        // different ground); its Name rides the surface header. Persists for the whole visit; re-landing
        // re-offers the same seeded set. Defaults to site 0 (the Wild Plain, the canon ground).
        public LandingSite Site { get; init; }
        public int PendingCoin { get; set; }
        public List<CacheCargo> PendingCargo { get; init; } = [];
        public bool ChestDropped { get; set; }
        public double DropX, DropY;
        public bool Buried { get; set; }                 // the carried chest went into the ground
        public DigChannel? Channel { get; set; }

        // #696 → #1016 · THE HOLD USED TO LIVE HERE, and it does not any more. It rode the excursion so that
        // the one interruption nothing could sensibly listen for — the shuttle lifting — took the clock with
        // it. Owner's ruling of 2026-08-30 ("refactor the working the case etc table options to not be tied
        // to any location") moved it onto the PAGE (`_processing`, Map.Surface.Darkroom.cs), because a
        // captain sitting at a bar top in a docked berth has no excursion at all and the dig has to take its
        // twenty seconds there too. Nothing was duplicated: there is still exactly one hold in the game, and
        // the lift-off interruption is still the explicit ProcessingIsInterrupted(LiftedOff) it always was,
        // fired on the way out (below), rather than a field that happened to be thrown away with the object.

        // Lane-1 · the tide clock (owner, 2026-07-18): the deep hands up a Reever every seeded gap, for
        // the whole excursion, with no fixed total. TideSeconds accrues real time; when it crosses the
        // seeded TideNextGap a Reever claws out and the index advances (which re-seeds the next gap and
        // its spawn x). Pure cadence in ReeverTide; this is just the client's accumulator.
        public double TideSeconds { get; set; }
        public double TideNextGap { get; set; }
        public int TideSpawnIndex { get; set; }
        public bool TideAnnounced { get; set; }          // the one-time "the deep stirs" notice has fired

        // #461 · when the shuttle mated, in surface seconds. The arrival grace is measured off this: a hull
        // setting down is not news to the Old Ones (they take it for one of their own), so nothing may notice
        // the captain until SurfaceArrival.SpotGraceSeconds have passed.
        // #469: REAL-TIME milliseconds (_lastTimestampMs), NOT SimTime. SimTime is the ship's orbital sim
        // clock; standing on a regolith it barely advances, so a grace measured on it never expired — and a
        // never-expiring grace means no Old One may EVER notice the captain. They walked to whatever spot
        // they were born knowing and froze there. The surface's own clock is the rAF one, the same one the
        // swing cooldown, the blood fade and the dig settle already use.
        public double LandedAtMs { get; set; }
        public bool GraceEndedAnnounced { get; set; }
        public bool SentryHintShown { get; set; }         // #380 item 7: the one-time first-deploy sentry hint has fired
        public bool NerveBandDropAnnounced { get; set; }  // #380 item 2: the one-time "nerves fraying" band-drop pulse has fired

        // #649 · The one-per-excursion beat for the moment the monolith stops being a shape and becomes the
        // sky. Separate from _monolithSeen, which is the once-in-a-LIFE nerve hit: the first sight is a
        // milestone and happens once ever, but ARRIVING at the foot of it is worth a line every time you make
        // the walk, and it is the walk the owner wants to be long enough to feel the thing grow.
        public bool MonolithApproachAnnounced { get; set; }

        // #649 · THE WATCH. How long the captain has stood inside the monolith's sight this excursion, and
        // whether the ground has already done its one strange thing. Real-time seconds, like every other
        // surface clock (#469: SimTime is the ship's orbital clock and barely advances on a regolith, so a
        // dwell measured on it would never come due — the bug that froze the Old Ones where they were born).
        //
        // Per EXCURSION, never persisted: this is not a milestone and there is no ledger of it anywhere in
        // the game. Nothing is counting; that is rather the point.
        public double MonolithDwellSeconds { get; set; }
        public bool MonolithWatchSpent { get; set; }

        public ulong ThreatSeed { get; set; }
        public TreasureCache? Cache { get; set; }        // set on a completed bury (for the map card)

        // The per-visit swept grid (owner, 2026-07-18: "some kind of grid system onto planet Miranda for
        // marking the checked squares on that visit"). Every beach-comber square probed THIS excursion,
        // keyed by its integer BeachComber square → what the throw turned up, so the deck-plan can paint a
        // subtle checked/bedrock mark. Client-only and per-visit — a fresh SurfaceExcursion on the next
        // landing starts empty, exactly like the Reever positions (never saved).
        public Dictionary<(int X, int Y), BeachComber.Outcome> Swept { get; } = [];
        public int Catches { get; set; }

        // #453: blows that got PAST the block this excursion. Five and the captain is down (the piracy
        // insurance issues the next one). Per-excursion: you come back down whole, having healed aboard.
        public int HitsTaken { get; set; }
        // #370 · the away-expedition state, live only when this landing is on the gig's site. Expedition
        // gates OFF the endless tide and arms the diced on-site beats (AwayExpeditionEvents). The accruals
        // are settled into the payout on liftoff (ExpeditionReward): the ground-time clock, the last beat
        // ordinal fired, banked discovery bonus, and scientists lost to the dark.
        public bool Expedition { get; init; }
        public double ExpeditionOnSiteSeconds { get; set; }
        public int ExpeditionLastOrdinal { get; set; } = -1;
        public int ExpeditionBonus { get; set; }
        public int ExpeditionScientistsLost { get; set; }
        public bool ExpeditionStrandingFired { get; set; } // the one-time "the window closed" toll has rolled
        public bool ExpeditionRevealFired { get; set; }    // #370: the bigger picture has surfaced (darkens the table, earns the truth bonus)

        // #394 · the away-DEFLECTION state, live only when this landing is on the inbound rock. Like the
        // expedition it gates OFF the endless tide (the horror is the CLOCK, not the pack) and arms the
        // diced complications (DeflectionGig). DrillProgress fills 0→1 as the charge is bored; ChargeArmed
        // when it completes; BurnFired once the ablation charge fires (the rail bends). CrewLost docks the
        // pay. Settled on liftoff (or resolved as an impact if the clock runs out).
        public bool Deflection { get; init; }
        public double DeflectionOnSiteSeconds { get; set; }
        public int DeflectionLastOrdinal { get; set; } = -1;
        public double DrillProgress { get; set; }          // 0..1 — the charge bore (persists across snaps)
        public bool ChargeArmed { get; set; }              // the drill reached depth; the charge is set
        public bool BurnFired { get; set; }                // the ablation charge fired (once)
        public int DeflectionCrewLost { get; set; }
        public bool DeflectionResolved { get; set; }       // the one-time impact/abort resolution has run
        public DrillChannel? DrillChannel { get; set; }

        // #371 Phase 3 · THE DOOR-OPEN DREAM. The forced-door channel and the appended-region state — live
        // ONLY on an expedition excursion. OpenedDoors are every sealed door (outer + nested) forced this
        // visit; LootedCaches every discovery cache claimed. Both key the region compose on a RebuildSurfaceDeck
        // (bury/lift/drop) so a full rebuild replays exactly what the incremental appends grew. Session-only,
        // never saved — a fresh landing starts sealed (same law as the Reever positions).
        public DoorChannel? DoorChannel { get; set; }
        public HashSet<string> OpenedDoors { get; } = [];
        public HashSet<string> LootedCaches { get; } = [];

        // #584 · THE MOUTHS OF EVERY GROUND THAT JOINED THE PLAN THIS VISIT, and which floor each joined on.
        //
        // Owner: "I was left totally un-aware what that did and where?" The card names the place once; this
        // is what lets the INSTRUMENT go on pointing at it, so the notification can still be acted on ten
        // seconds later when the card is gone. BuildBeacons rings every entry that belongs to the floor the
        // captain is standing on — a chamber forced on B2 is not a place on B3.
        //
        // Session-only and never saved, the same law as OpenedDoors above it: a fresh landing starts sealed,
        // so a fresh landing has no new ground to point at either.
        public List<(double X, double Y, int Floor)> NewGround { get; } = [];

        // #371 Phase 3 · fog-of-war state (expedition sites only). SeenRegions = every appended region the
        // captain's line of sight has ever reached (stays "explored", drawn dim); VisibleRegions = those in
        // sight right now (drawn lit). Echoes = the fading "movement was here" ripples a contact leaves when it
        // slips behind cover while moving. LastFogCell throttles the region recompute to captain-cell moves.
        public HashSet<string> SeenRegions { get; } = [];
        public HashSet<string> VisibleRegions { get; } = [];
        public List<(double X, double Y, double Born)> Echoes { get; } = [];
        public (int Cx, int Cy)? LastFogCell { get; set; }

        // COMMS-LOSS (owner, cruise 2026-07-19: "loss of comms.. that also is great horror element"). The
        // mothership's telemetry downlink can DEGRADE or DROP for a while, freezing the away HUD's ship-line
        // at "last known" while the suit instruments run on. Pure cadence in CommsLink; these are the client's
        // live accumulator + the active-episode shape + the last-known snapshot the freeze paints from.
        // Client-only, per-visit, never saved (same law as the Reever positions).
        public double CommsSeconds { get; set; }              // on-site seconds — the link's clock
        public double CommsNextOnset { get; set; } = -1;      // CommsSeconds at which the next episode starts (-1 = unscheduled)
        public int CommsOnsetIndex { get; set; }              // the monotonic episode ordinal (seeds the cadence)
        public bool CommsActive { get; set; }                 // an episode is underway right now
        public double CommsEpisodeStart { get; set; }         // its start (CommsSeconds), and…
        public double CommsEpisodeDuration { get; set; }      // …its length, and…
        public bool CommsEpisodeDeepens { get; set; }         // …whether it drops all the way to blackout
        public CommsLink.Phase CommsPhase { get; set; } = CommsLink.Phase.Nominal; // the live phase this frame
        // The last-known mothership readout, snapshotted every frame the link is NOMINAL — what the freeze
        // paints (stale, honestly labelled) while the downlink is down. The TRUE state keeps advancing
        // underneath in the real fields; only this DISPLAY is withheld (the honesty law, see CommsLink).
        public string? CommsLastLine { get; set; }
        public int CommsLastSeverity { get; set; }
        public double CommsLastContactSeconds { get; set; }   // the CommsSeconds of the last nominal contact
        public bool CommsFirstLossAnnounced { get; set; }     // the one-time "static — the feed drops" notice has fired

        // #409 · THE SECRET LAB. Live only when this body hides one of Dr. Vantar's sealed labs. Placement is
        // the seeded hidden-door spot; DoorRevealed once a beach-comber probe pings the right square (or a
        // revisit to an already-found body, or the cheat); Forced once the door is wrenched open (appends the
        // lab region); CacheLooted / LogsRead / RevealFired track the interior. The DoorChannel is the forced-
        // door progress bar (reuses the door-force idiom). Session-only EXCEPT the "found" fact, which persists
        // per game-thread in _secretLabsFound (the vault/thread idiom).
        public SecretLab.Placement? Lab { get; set; }
        public bool SecretLabDoorRevealed { get; set; }
        public bool SecretLabForced { get; set; }

        // #822 · …and whether the crawl at the back of THE HEART has been forced. The fire code's second
        // exit, and it is hidden the way the front door is: nothing marks it, nothing is on the tracker, and
        // it stays a wall until a captain sets their shoulder to it. Session-only, like Forced.
        public bool SecretLabCrawlForced { get; set; }
        public bool SecretLabCacheLooted { get; set; }
        public bool SecretLabRevealFired { get; set; }
        public HashSet<string> SecretLabLogsRead { get; } = [];
        public DoorChannel? SecretLabDoorChannel { get; set; }

        // #563 · The outpost hut on this site, if it has one: where it stands, whether the hatch has been
        // forced this visit, whether its locker and its effects have been taken/read, and the force channel
        // while it is running. Session state — a hut re-seals between excursions, which is honest enough:
        // nobody out here is maintaining a door you levered off its dogs.
        // #564 · THE TANK. Seconds of suit air left, and whether the captain has already been told they
        // crossed the point of no return (the warning is a LINE you cross, said once — not a nag).
        public double AirSeconds { get; set; } = SuitAir.TankSeconds;
        public bool AirWarned { get; set; }

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
        public SurfaceStream Stream { get; } = new();

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
        public double FireTimer { get; set; }              // #314: accrues to the SentryBot fire cadence

        // #316 law 2 · Which husks this visit has already been told about, by their ledger key. Presentation
        // state and per-visit by design, the same class as ShelterBreathNoted above: a line said once as you
        // walk over the pile is a scene, and the same line every frame is a nag.
        public HashSet<string> HusksRead { get; } = [];

        // A chest is in hand right now: something was loaded, not yet buried, not dropped.
        public bool Carrying => (PendingCoin > 0 || PendingCargo.Count > 0) && !Buried && !ChestDropped;
        public bool Channeling => Channel is not null;
        // #371 Phase 3 / #394: any channel underway (a dig, a door-force, OR the drill) — mutually exclusive.
        //
        // #696 · AND THE DARKROOM IS ONE OF THEM. A captain photographing a pay sheet has both hands full;
        // more to the point, all of these draw the SAME progress bar (#562), so two at once would be one bar
        // reporting one of them and the captain watching the wrong clock. The exclusion runs both ways —
        // BeginProcessing refuses while a channel is up, and every [E] that starts a channel already asks it.
        //
        // #1016 · …and the question is asked one level up now. The GROUND's channels are still this list; the
        // darkroom hold left the excursion for the page (a bar top in a docked berth has no excursion and the
        // dig has to take its seconds there too), so the property every starter actually asks is
        // Map.AnySlowThingUnderYourHands, which is this OR the one hold. Splitting it that way is what
        // keeps a single answer to "is a bar already filling" on every ground the captain can stand on.
        public bool AnyChannel => Channel is not null || DoorChannel is not null || DrillChannel is not null
            || SecretLabDoorChannel is not null || OutpostDoorChannel is not null;
    }
}
