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
//
// #251 · AND NOW IT IS FIVE FILES, for the same law that made it one: at 857 lines it was over the
// 800-line aim and the next feature to touch the record would have tripped the gate. This file keeps
// the CONSTRUCTORS — the one moment a tank flag becomes a fact about a walk — and everything the
// visit is carrying: the chest, the tide, the landing grace, the monolith, the expedition and the
// deflection, the doors and caches, the fog, the comms link, the secret lab, and the suit's air. It
// also keeps the four COMPUTED properties at the foot of the record, which read that state.
//
// The other four are each a CONTIGUOUS run of the base record, in the order the base declared them:
//
//   .Air    #325's extended tank, and the shelter and refuge reservoirs the suit breathes off
//   .Hive   everything below the surface: floors and rooms, the cabinets and the washroom, #719's
//           maintenance break, #602's keypad and #1149's inspection
//   .Hall   the people on their feet — who stood, who came in, who is holding a cabinet door, and
//           what has happened at which table this watch
//   .Ground the repo boat, the ruins, the huts, the tile stream, the bots, the husks and the scars
//
// EVERY ROW IS STILL IN THE ORDER THE ONE RECORD DECLARED IT. A run was never broken to send a
// property to sit beside a better neighbour: a field that travels is a line the diff cannot account
// for, and the arithmetic quoted in the PR is the whole proof that this is a move and nothing else.
// No member is renamed, re-scoped or re-ordered, and the record holds no static field of any kind.
public partial class Map
{
    public sealed partial class SurfaceExcursion
    {
        /// <summary>
        /// #325 · AN EXCURSION IS BUILT KNOWING WHICH BOTTLE IS ON IT, and there is no later moment at which
        /// it can be told.
        ///
        /// <para>A tank is fitted to a suit at the shuttle, and a captain cannot walk back to the ship's
        /// stores from four thousand du out — so the flag is a CONSTRUCTOR ARGUMENT and the property is
        /// get-only. That is not decoration: everything about this walk's air is derived from it at
        /// construction (the budget, the suit's starting charge, and the tile lattice the stream carries),
        /// and a settable flag would mean a captain could be handed a bigger world with their boots already
        /// on the ground, with the tiles under them evicted underfoot.</para>
        ///
        /// <para>The parameterless overload is the standard bottle, and it exists because a dozen benches
        /// build an excursion through <c>Activator.CreateInstance(t, nonPublic: true)</c> — a constructor
        /// whose only parameter is optional is not a parameterless constructor as far as reflection is
        /// concerned, and every one of them would have thrown.</para>
        /// </summary>
        public SurfaceExcursion() : this(extendedTank: false)
        {
        }

        /// <param name="extendedTank">Whether a spare bottle from the ship's stores went on this suit.</param>
        public SurfaceExcursion(bool extendedTank)
        {
            ExtendedTank = extendedTank;

            // The suit steps out FULL of what it is actually carrying. Stated here, at the one moment the
            // bottle becomes a fact about a walk, and read from the one function — an initialiser could not
            // do it, because an initialiser runs before the flag arrives.
            AirSeconds = SuitAir.PlayBudget(extendedTank);

            // …and the ground reaches as far as the suit is willing to walk. Same moment, same reason: the
            // lattice stops at the backstop and the backstop is the tether.
            Stream = new SurfaceStream(AirBudgetSeconds);
        }

        public required ShuttleStop Stop { get; init; }
        public required string? RestoreHavenId { get; init; }

        // #320 · WHERE on the body we set down — the chosen landing site (seeded set per body, picked in the
        // boarding panel). Its LayoutSalt parameterizes the surface deck-plan (a different site grows a
        // different ground); its Name rides the surface header. Persists for the whole visit; re-landing
        // re-offers the same seeded set. Defaults to site 0 (the Wild Plain, the canon ground).
        public LandingSite Site { get; init; }
        public int PendingCoin { get; set; }
        public List<CacheCargo> PendingCargo { get; init; } = [];

        /// <summary>#319 · THE THING OUT OF THE COAT. One satchel row the chooser picked at the shuttle door
        /// to put in the ground (<see cref="Core.ShuttleExcursion.ChestLoad.Deposit"/>), carried down.
        ///
        /// <para>It is a POINTER at a row that is still in the captain's satchel, never a copy lifted out of
        /// it: a coat is not a cargo manifest, and the satchel goes on being the satchel for the whole walk —
        /// it can be offered at a door, spread on a desk, torn up at a bin. The shovel asks
        /// <see cref="CacheDeposit.AsCarried"/> whether it is still there and buries what it finds, so a paper
        /// picked at the door and then read, filed or set down is simply not there to bury and nothing
        /// anywhere has to be told about it.</para>
        ///
        /// <para>Nulled the instant it goes in the hole, which is what takes the shovel back off [E].</para></summary>
        public Core.Satchel.Item? PendingDeposit { get; set; }

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

        // #436 · Whether the one authored sentence for the moment an Old One FIXES has been said this trip.
        // Once per excursion, because the latch is one-way: after the first fix the ground is never clean
        // again, and a second announcement would be old news said over the sound of the thing coming.
        public bool FixedOnYouSaid { get; set; }

        // #719 · …and the same shape for the climb out by stair. Once on arrival, and only for the trip that
        // was actually made on the captain's own legs — the car has its own line and may not be spent on it.
        public bool StairArrivalSaid { get; set; }

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

        // #563 · …AND THE SHOULDER ON A KEYED LAB DOOR. Owner ruling, 2026-09-13: "a locked door is TIME,
        // never a key" — so a keyed leaf in the mountain is not a wall to the captain any more, it is
        // LockedDoor.ForceSeconds of standing still in a corridor with the garrison awake. Same channel
        // class, same abort-by-stepping-away law, same one progress bar; what it costs is the constant its
        // own kind owns (25 s, not the 5 s a sealed way costs) because a bolt shot into a frame by a living
        // security system is not a seal that rotted.
        public DoorChannel? LabDoorChannel { get; set; }

        // #563 · The outpost hut on this site, if it has one: where it stands, whether the hatch has been
        // forced this visit, whether its locker and its effects have been taken/read, and the force channel
        // while it is running. Session state — a hut re-seals between excursions, which is honest enough:
        // nobody out here is maintaining a door you levered off its dogs.
        // #564 · THE TANK. Seconds of suit air left, and whether the captain has already been told they
        // crossed the point of no return (the warning is a LINE you cross, said once — not a nag).
        //
        // #325 · AirSeconds STARTS at this excursion's budget, not at the constant, because a fitted
        // extended tank is fitted before the boots touch regolith and a suit that filled to the standard
        // mark and then had the bottle "added" would be two tanks arguing. The initialiser reads the same
        // one function every other reader does — see the constructor, which is the only place it can be
        // stated, and the only place it is.
        public double AirSeconds { get; set; }
        public bool AirWarned { get; set; }

        // A chest is in hand right now: something was loaded, not yet buried, not dropped.
        public bool Carrying => (PendingCoin > 0 || PendingCargo.Count > 0) && !Buried && !ChestDropped;

        /// <summary>#319 · IS THERE ANYTHING TO PUT IN A HOLE — the question the shovel asks, and the ONLY
        /// thing #319 widened. <see cref="Carrying"/> is deliberately left exactly as it was, because four
        /// other systems read it and every one of them is about the WEIGHT of a chest in your arms: the walk
        /// speed (<c>CurrentWalkSpeed</c>), the block roll a captain throws with his hands full
        /// (<c>CaptainCondition.BlockRoll</c>), what an Old One sees him doing
        /// (<c>ReeverObservation.Doing.Hauling</c>) and whether [G] can drop it. A file folded into a coat
        /// pocket does not slow a man down, does not stop him getting an arm up, and cannot be dropped to
        /// sprint — so widening <see cref="Carrying"/> would have quietly re-priced four scenes that have
        /// nothing to do with this issue.</summary>
        public bool ShovelHasSomethingToBury => Carrying || PendingDeposit is not null;
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
            || SecretLabDoorChannel is not null || OutpostDoorChannel is not null || LabDoorChannel is not null;
    }
}
