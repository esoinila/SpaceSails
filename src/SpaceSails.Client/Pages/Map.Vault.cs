using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Vault — the personal vault: build, write, peek, resume, import and export, and the
// ApplyVault machinery that rehydrates a saved run. Split from Map.razor per #251.
public partial class Map
{

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // The personal vault (#225): the pirate's LIFE, durable across a server restart. localStorage
    // autosave + export/import a .json file; the resume is always a BERTH (owner's dock-resume law),
    // never a stored orbit. What is NOT saved (deliberate dev-kindness, documented in Vault.cs): NPC
    // positions, a hunter mid-chase (heat IS saved, but the pursuit resolves as escaped on reload),
    // and autopilot plans — all recomputed from the fresh docked state.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // Reset every scrap of durable + discovered state to a brand-new-game slate (owner 2026-07-18: "different
    // game starts don't share state", and the follow-up: "mission statuses reset from the new game starting").
    // This is the exact inverse of BuildVault (so a new game equals a blank vault) PLUS the two SESSION-scoped
    // discovery sets the vault deliberately never carries — _revealedBodyIds (where "the roadster is found"
    // actually lives) and the scope-intel cards. The sim clock itself returns to the beginning date via the
    // ApplyStart/StartDockedAtHaven that runs right after (it rebuilds the ship at epoch 0).
    private void ResetLiveStateForNewGame()
    {
        // Purse + hold: the same opening stake a fresh boot lays down (Map.Trade constants), so a New voyage
        // is byte-for-byte the standard Earth opening.
        _credits = StartingCredits;
        _cargoByClass.Clear();
        foreach ((string cargoClass, int units) in StartingManifest)
        {
            _cargoByClass[cargoClass] = _cargoByClass.GetValueOrDefault(cargoClass) + units;
        }

        _hotCargo.Launder();
        RecomputeCargoTotals();

        // Ship consumables + the sentry roster, fresh.
        _slugAmmo = 12;
        _missileAmmo = 4;
        _shipBots.Clear();
        foreach (string unit in SentryBot.RosterUnits)
        {
            _shipBots.Add(new ShipBot(unit, SentryBot.MaxMagazine));
        }

        // #325/#332 · The chandlery's two: no spare bottles in stores (they are bought, never issued), and a
        // full pill cabinet — the same stake every captain has opened with since #343.
        _extendedTanks = 0;
        _pills = Chandlery.MedKitFullStock;

        // Upgrades back to base (and the tank to the base capacity that implies), sensor rebuilt.
        _massLevel = 0;
        _sensorLevel = 0;
        _holdLevel = 0;
        _telescopeLevel = 0;
        _reactionMassPulses = ReactionMassCapacity; // = 500 at mass level 0
        _hasNetJammer = false;
        RebuildSensor();

        // Heat, nerve, insurance — the mood of the run, all calm again.
        _heat = HeatState.None;
        _nerve = NerveModel.Steady;
        _monolithSeen = false;
        _pendantFirstOpened = false;                          // #620 · a new universe has not opened the locket
        _lastQuietMinuteSimTime = double.NegativeInfinity;    // …and has never taken a quiet minute
        _insurance = PirateInsurance.Uninsured;

        // #1151 · …and the claims file with them. This method's contract is that it is the exact inverse of
        // BuildVault, and a counter that survived into a new universe would hand a fresh captain somebody
        // else's unease — the flashback card on his FIRST claim, off a number he never earned.
        _claimsLodged = 0;
        _claimOwed = null;
        _writPending = null;
        _claimDesk = null;
        // #1151 slice 2 · …and the offer's latch, for the same reason: a fresh captain whose salesman has
        // already spent the line on a hull he never lost is a scene the new universe silently owes him.
        _lodgingOfferedFor = null;
        _repOffersToLodge = false;

        // #563 · The once-per-captain teaching cards. This method's contract is that it is "the exact
        // inverse of BuildVault (so a new game equals a blank vault)", and _groundLessonSeen was quietly
        // missing from it — a new captain in the same session inherited "already taught" from the previous
        // one and silently never got the first-ground card. Both bits reset here now.
        _groundLessonSeen = false;
        _groundGrewSeen = false;
        _tubeRearmSeen = false;
        _airCardSeen = false;

        // The mission/contract slate and every relationship, wiped: a new universe owes nobody and knows
        // nobody (owner: mission statuses reset with the new game). New quest ids mint from zero again.
        _quests.Clear();
        _questSeq = 0;
        _favorObligations.Clear();
        _contacts.Clear();

        // The hoard and the bar-intel book — knowledge of a previous run's world, gone.
        _caches.Clear();
        _overheard = [];

        // #973 L1 · …and the filing line's marks with them. A new universe's captain has never died, so no
        // page of their ledger is in anybody else's hand. (This method's contract is "the exact inverse of
        // BuildVault" — the #563 lesson two blocks up — and a book of grey rows carried into a fresh voyage
        // would grey pages of a ledger that does not exist yet.)
        _filingBook = [];

        // #973 L5a · …and the old crew with them. A new universe casts its own four, rolls its own history
        // between them and lays down its own summer-party page; the crossings and the sheets are this
        // captain's and go nowhere else.
        ForgetTheOldCrew();

        // #411: a new voyage is a new universe — the KAAMOS shards this captain gathered are unknown again,
        // and so is the head office: a captain who has never been under the ice has never counted the beds,
        // and must be able to pay for it (the arc's 40) exactly once more.
        _kaamos.Clear();
        _headOfficeBeatsSeen.Clear();

        // #422/#425: likewise the NEBULA shards, and the oracle's per-visit reading state — a fresh universe
        // has never leaned on Static's corner. #640: NebulaProgress.Clear takes the closed policy with it —
        // a new captain has a pattern on file, whatever the last one did to theirs — and the pen comes back
        // up here for the same reason.
        _nebula.Clear();
        _threadIsOver = false;

        // #422 story pass · the two run-scoped counters arc 2 gates its beats on, which this method (whose
        // contract is "the exact inverse of BuildVault", the #563 lesson) was quietly not resetting. Both
        // leaked across a New voyage started without a page reload, and both then LIED on the card:
        //   · _rebirthsSeen fed the clinic-ledger gate, so a captain's FIRST death in a brand-new universe
        //     could be handed "Someone under your number has woken here before, more than once" — the shard
        //     whose whole fiction is that you have been here before. (Its durable partner, the thread's own
        //     retired-captain count, is correct and stays.)
        //   · _insurancePosterReads is what makes `fine-print` "the fine print, read TWICE"; carried over, a
        //     fresh captain's very first look at a poster read the small print they had never read.
        _rebirthsSeen = 0;
        _insurancePosterReads = 0;
        _oracleStation = null;
        _oracleOpen = false;
        _oracleLine = null;
        _oracleDraw = 0;
        _oracleDrinks = 0;

        // #394: a new universe has not saved the Ringside Exchange — its dedication plaque reads its
        // original bronze until this run's crew earns the gratitude line. Any live gig is dropped too.
        _ringsideSaved = false;
        _deflection = null;
        _deflectionResolved = null;
        _deflectionRaiseMeters = 0;
        _deflectionLeftPort = false;

        // THE leak's home: the session-scoped "found it" sets. Clearing _revealedBodyIds re-hides every
        // scenario-hidden body (the derelict roadster among them) so a new game must re-discover it; the
        // scope-intel cards (scan fixes) go with it. _hiddenBodyIds is scenario data — left untouched.
        _revealedBodyIds.Clear();
        _scopeIntel.Clear();

        TheMilkRunIsUntaught(); // #160: a new universe has not been taught the loop
        // #292 note: _tutorialPlayed is deliberately NOT reset here. Whether a fresh universe re-runs the
        // tutorial (and its date-triggered target rush) is the docked-starts lane's rework, not this one;
        // this lane owns that the persistence model resets and isolates the mission slate above.
    }

    /// <summary>Gather the whole durable life into a vault envelope. Physics (orbit/trajectory) is
    /// deliberately absent — the resume section names the berth to wake at instead.
    /// <para>#948 — every vault carries WHOSE it is; a deliberate bank or export also carries the title and
    /// the note the captain wrote on it (the rolling autosave passes blanks, because nobody sat down and
    /// named it).</para></summary>
    private Vault BuildVault(string title = "", string note = "")
    {
        List<CargoLine> hold = _cargoByClass
            .Where(kv => kv.Value > 0)
            .Select(kv => new CargoLine(kv.Key, kv.Value))
            .ToList();

        return new Vault
        {
            SavedSimTime = _ship.SimTime,
            Purse = new PurseSection(_credits),
            Ship = new ShipSection
            {
                ReactionMassPulses = _reactionMassPulses,
                SlugAmmo = _slugAmmo,
                MissileAmmo = _missileAmmo,
                SentryMagazines = _shipBots.Select(b => b.Rounds).ToList(), // #314
            },
            Cargo = new CargoSection(hold, VaultMapper.ToHotLines(_hotCargo)),

            // #325/#332 · The chandlery's stores, always written by a live game: a captain always has a
            // cabinet, even an empty one, and "what is in it" is exactly the fact this section exists to
            // carry. Only a vault written before the lane lacks the section.
            Chandlery = new ChandlerySection { ExtendedTanks = _extendedTanks, MedKitPills = _pills },
            Heat = new HeatSection(_heat.Level, _heat.RaisedAtSimTime),
            Contacts = VaultMapper.ToSection(_contacts),
            Caches = VaultMapper.ToSection(_caches),
            Quests = BuildQuestsSection(),
            Insurance = VaultMapper.ToSection(_insurance),
            Upgrades = new UpgradesSection
            {
                MassLevel = _massLevel,
                SensorLevel = _sensorLevel,
                HoldLevel = _holdLevel,
                TelescopeLevel = _telescopeLevel,
            },
            DiceItems = BuildDiceItemsSection(),
            Progress = new ProgressSection // #292 / #394 / #409
            {
                TutorialPlayed = _tutorialPlayed,
                RingsideSaved = _ringsideSaved,
                SecretLabsFound = [.. _secretLabsFound],
                GroundLessonSeen = _groundLessonSeen, // #440: the first-ground card greets a captain once, ever
                GroundGrewSeen = _groundGrewSeen,     // #563: so does the map-just-grew card
                TubeRearmSeen = _tubeRearmSeen,       // #562: and the tube-feeds-you card
                AirCardSeen = _airCardSeen,           // #573: and the tank-is-low card
                OddBooksRead = [.. _oddBooksRead],    // #701: the shelves whose gist this thread already has

                // #1066 · Berths since the crew were last ashore — and NULL while nobody is counting, which
                // is not tidiness: the checksum is taken over the payload, so writing a zero here on every
                // save would change the digest of every vault ever written and hang the 📛 tampered marker
                // on an honest voyage (#1057/#1072's pattern; the bytes are pinned in
                // ShoreLeaveIsALedgerLineTests against a real pre-#1066 file).
                WorkingStopsSinceShoreLeave =
                    _workingStopsSinceShoreLeave > 0 ? _workingStopsSinceShoreLeave : null,
                MilkRunLessonStep = _milkRunStep > 0 ? _milkRunStep : null, // #160 · null while untaken
                // #677 · the disclosure clock's register — which grounds this thread has been past the seam
                // of, and the world-side window each was opened in. A clock that forgot across a reload
                // would not be a clock. Null while empty: an eager [] would move every legacy vault's
                // checksum (the #1078 byte guard's law).
                HallsOpened = _hallsOpened.Count > 0
                    ? [.. _hallsOpened.Select(o => new HallOpeningRecord(o.BodyId, o.Window))]
                    : null,
                // #1063 · …and which of those grounds the neighbours have since filled in. Same null-while-
                // empty law, same reason. A burial that forgot across a reload would un-bury a ground the
                // field book says is gone, and the book being the only witness is the whole feature.
                HallsBuried = _hallsBuried.Count > 0 ? [.. _hallsBuried] : null,
                // #1063 slice 2 · …and the one seal this captain has forced and found empty. Same
                // null-while-unspent law and the same reason one rung harder: a spend a reload forgot would
                // put the cache back into a room the book says was bare AND leave him a second empty room to
                // find later, and two of them is a rate rather than a disappointment (EmptySeal).
                EmptySealSpentOn = _emptySealSpentOn,
                // #1199 · …and the one person this captain has followed onto an observation walk and not
                // found, with the counter the world has since handed them back at. Same null-while-unspent
                // law and the same reason one rung harder again: a spend a reload forgot would let the same
                // person be walked into the same tube twice, and a thing that happens twice is a mechanic
                // rather than a moment (ObservationWalk).
                ObservationWalkSpentOn = _observationWalkSpentOn,
                ObservationWalkSightingAt = _observationWalkSightingAt,
                // #1068 · …and which of them the world has since declined on, WITH the window each declined
                // in. Same null-while-empty law, same reason; the window rides along because the door is
                // chosen against it, and a reload that forgot the number would shut a different leaf.
                HallsDeclined = _hallsDeclined.Count > 0
                    ? [.. _hallsDeclined.Select(d => new HallDeclineRecord(d.BodyId, d.Window))]
                    : null,
                // #1068 · …and which of them the harbour has filed paperwork about (Map.QuietHands.cs).
                HallsHandled = QuietHandRows(),
                // #1074 · …and which of them the Authority has closed the working of (Map.Stop.cs).
                HallsStopped = StopRows(),
                // #1074 beat 2 · …and which of THOSE it has since fenced and signed (Map.Preserve.cs).
                HallsPreserved = PreserveRows(),
                // #1074 beat 5 · ...and the returning shuttle: the preserved grounds he has stood on, and the beat's one row.
                ShuttleSeen = ShuttleSeenRows(),
                Shuttle = _shuttle,
                // #525 · …and the one collar a harbour has cleared with a reason on it (Map.BerthScuttle.cs).
                CollarCleared = ClearedCollarRow(),
                // #1151 · …and the file the captain is building on himself: how many claims he has lodged,
                // the one that is lodged and not yet paid, and the writ waiting for him at a port. All three
                // null while there is nothing to say — the same checksum law as every row above it.
                ClaimsLodged = _claimsLodged > 0 ? _claimsLodged : null,
                ClaimOwed = _claimOwed,
                WritPending = _writPending,
                // #1151 slice 2 · …and the loss the rep has already offered to take, so the line stays
                // once-per-loss across a reload (Map.Claims.Rep.cs).
                LodgingOfferedFor = _lodgingOfferedFor,
            },
            Nerve = new NerveSection { Nerve = _nerve, MonolithSeen = _monolithSeen, PendantFirstOpened = _pendantFirstOpened }, // #317
            Overheard = _overheard.Count > 0 ? new OverheardSection { Lines = _overheard } : null, // bar intel, durable
            // #587 · the field book: what was found on the ground, kept so it can be re-read.
            FieldNotes = _fieldNotes.Count > 0 ? new FieldNotesSection { Notes = _fieldNotes } : null,
            // #741 · the red lines the captain drew between two of those entries. Opaque pair strings, both
            // ends derived from the notes' own words — so the book and the lines come back together.
            CaseThreads = _caseThreads.Count > 0
                ? new CaseThreadsSection { Threads = [.. _caseThreads.Select(t => t.Stored)] }
                : null,
            // #836 · which name the captain gave, and where. Opaque row strings (WalletChoice.Shown.Stored)
            // for the satchel's own reason one line down: the file carries the FACT and the sentences on a
            // chooser row are rebuilt from it. Durable because "worked here, twice" is worth nothing if the
            // book forgets between excursions.
            PapersShown = YourPaperTrail.Count > 0
                ? new PapersShownSection { Shown = [.. YourPaperTrail.Select(r => r.Stored)] }
                : null,
            // #563 slice 2 · WHAT THIS CAPTAIN CHANGED ON A MOON — the hatches forced, the lockers lifted,
            // the effects read, keyed on (body, site, tile). Durable because a hut is a PLACE: walking away
            // from one and finding it dogged again on the next trip is the world becoming wallpaper, which
            // is the exact failure the treadmill decision names. Written only when there is something to
            // write, so a voyage that never set foot on a moon leaves the section out of the file entirely.
            Ground = _groundMemory.Count > 0
                ? new GroundSection { Changed = _groundMemory.Stored }
                : null,
            // #603 · the satchel — everything carried on foot, durable because a thing found eleven floors
            // under a moon has to still be in the pocket a month and a world later. Opaque item strings, so
            // the save carries the FACT and never the words.
            Satchel = _satchel.Count > 0
                ? new SatchelSection { Items = [.. _satchel.Select(i => i.Stored)] }
                : null,
            // #1016 · …and which of those sheets are already in the book in the captain's own hand. Opaque
            // keys, for the satchel's own reason one line up: the file carries the FACT and every sentence
            // the book prints about a sheet is rebuilt from the paper at read time. Durable because the
            // owner's ruling makes this the CASE's register rather than the GROUND's — a sheet dug once is
            // dug for good, wherever it was dug, and a register that died with the shuttle was the ground's.
            WorkedUp = _workedUp.Count > 0
                ? new WorkedUpSection { Sheets = [.. _workedUp] }
                : null,
            TurnedOver = TheRoomsGoneThrough(),   // #615/#573 · the rooms already gone through, by site
            // #973 L1 · the filing line's marks on the Captain's ledger: which pages this captain does not
            // remember writing, which have been read at already, and the hidden originals of the ones that
            // came back wrong. Opaque rows — the file carries the FACT, never the sentences.
            Filing = BuildFilingSection(),
            // #973 L5b · which walk-ins the SPREAD has read the same hand off. Job ids only — the grey line
            // itself is rebuilt from Core every render, so the file carries the knowing and never the words.
            WalkIn = BuildWalkInSection(),
            Finder = BuildFinderSection(),   // #417 · the case's graph, and how far along it he has got
            // #973 L5a · the old crew: who this universe cast, the history rolled between them, and where
            // they ended up working. Opaque rows — the file carries the FACT and the book's sentences are
            // rebuilt from the pool.
            OldCrew = BuildOldCrewSection(),
            // …the captain's crossings (the-captains-character.md §3), and the held-memory sheets.
            Crossings = BuildCrossingsSection(),
            HeldMemories = BuildHeldMemoriesSection(),
            // #973 · the void's weather: how often each of the eight lines has been heard and which visit it
            // last blew through each station on. Counts and ordinals — the file never carries a sentence.
            InsuranceWeather = BuildTheWeatherSection(),
            Kaamos = VaultMapper.ToSection(_kaamos), // #411: the assembled ice-moon shards, per game-thread
            Nebula = VaultMapper.ToSection(_nebula), // #422/#425: the assembled Nebula-Mutual shards (oracle-leaked)
            Resume = BuildResumeSection(),
            // #948 · the logbook page. The captain's name rides in the PAYLOAD, not only in the slot label,
            // so an exported .json still says who sailed it on a machine that has never heard of this thread.
            Logbook = new LogbookSection
            {
                CaptainName = ActiveCaptainName,
                Title = SaveSlotLabels.CleanTitle(title),
                Note = SaveSlotLabels.CleanNote(note),
            },
            // #638 · the void's countdown, and ONLY when one is running. A ship that has never gone dry with
            // nowhere to go writes no section at all, which is what keeps every save made before this lane
            // existed byte-identical through a load and a re-save (see Vault.Void).
            Void = _voidDeclaredDay == VoidRule.ClockNotRunning
                ? null
                : new VoidSection { DeclaredDay = _voidDeclaredDay, LastToldDay = _voidLastToldDay },
        };
    }
}
