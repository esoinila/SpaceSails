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
        // has never leaned on Static's corner.
        _nebula.Clear();

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
            Nerve = new NerveSection { Nerve = _nerve, MonolithSeen = _monolithSeen }, // #317
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

    /// <summary>Restore a vault into live state. Economy first (order-independent), then dock at the
    /// resume berth LAST so the ship is built fresh alongside the haven at load-time ephemeris — the
    /// owner's law that a resume is a berth, never a stored orbit.</summary>
    private void ApplyVault(Vault vault)
    {
        // Clear the ADDITIVE ledgers first (feat/game-threads). VaultMapper.Apply / ApplyHot LOAD into a
        // ledger without clearing, and _revealedBodyIds is never vaulted — so without this, loading a save
        // (especially switching to another universe via the other-voyages door) would MERGE the old run's
        // contacts, hoards, hot flags and discoveries into the loaded one. A load must be the loaded life,
        // whole and alone. (The other sections below already replace-on-apply, so they need no pre-clear.)
        _contacts.Clear();
        _caches.Clear();
        _hotCargo.Launder();
        _revealedBodyIds.Clear();
        _scopeIntel.Clear();
        _kaamos.Clear(); // #411: the loaded life brings its OWN assembled shards (applied below), not the last run's
        _nebula.Clear(); // #422/#425: same for the Nebula shards — the load re-hydrates its own set below

        // #948 · the name comes back with the life. A vault carries the captain's name in its own payload,
        // so an IMPORTED file (which arrives into a brand-new thread with a freshly seeded roster name) still
        // wakes up as the captain who saved it, rather than as a stranger with the same purse. A pre-#948
        // file has no logbook: the seeded name stands, which is exactly what it was.
        if (!string.IsNullOrEmpty(_activeThreadId)
            && SpaceSails.Core.Captains.CleanName(vault.Logbook?.CaptainName) is { Length: > 0 } sailedBy)
        {
            Threads.Rename(_activeThreadId, sailedBy);
            RefreshThreadList();
        }

        if (vault.Purse is { } purse)
        {
            _credits = (int)Math.Clamp(purse.Credits, int.MinValue, int.MaxValue);
        }

        if (vault.Ship is { } ship)
        {
            _reactionMassPulses = (int)Math.Max(0, Math.Round(ship.ReactionMassPulses));
            _slugAmmo = Math.Max(0, ship.SlugAmmo);
            _missileAmmo = Math.Max(0, ship.MissileAmmo);

            // #314/#324: rebuild the full sentry roster (K-77, R-3B) from the saved magazines, padding any
            // missing entry to a full mag — a load never permanently shrinks the roster (the pinned Core
            // law SentryBot.RosterFromSave). A pre-#322 vault with no SentryMagazines loads as full 99s.
            _shipBots.Clear();
            IReadOnlyList<int> mags = SentryBot.RosterFromSave(ship.SentryMagazines);
            for (int i = 0; i < SentryBot.RosterUnits.Count; i++)
            {
                _shipBots.Add(new ShipBot(SentryBot.RosterUnits[i], mags[i]));
            }
        }

        if (vault.Upgrades is { } up)
        {
            _massLevel = Math.Max(0, up.MassLevel);
            _sensorLevel = Math.Max(0, up.SensorLevel);
            _holdLevel = Math.Max(0, up.HoldLevel);
            _telescopeLevel = Math.Max(0, up.TelescopeLevel);
            RebuildSensor();
        }

        ApplyCargo(vault.Cargo);

        if (vault.Heat is { } heat)
        {
            _heat = new HeatState(heat.Level, heat.RaisedAtSimTime);
        }

        VaultMapper.Apply(vault.Contacts, _contacts);
        VaultMapper.Apply(vault.Caches, _caches);
        VaultMapper.Apply(vault.Kaamos, _kaamos); // #411: rehydrate the assembled ice-moon shards (tolerant of a pre-#411 save)
        VaultMapper.Apply(vault.Nebula, _nebula); // #422/#425: rehydrate the Nebula shards (tolerant of a pre-#422 save)
        _insurance = VaultMapper.ToInsurance(vault.Insurance);
        ApplyObligationsAndQuests(vault.Quests);
        ApplyDiceItems(vault.DiceItems);

        // #292: a saved life is never a fresh captain. Restore the "played" flag (a missing section —
        // an old save from before this flag — defaults to false, which is harmless: the greeting is
        // still suppressed below because a LOAD is not a fresh Earth start), then keep the nav clear.
        _tutorialPlayed = vault.Progress?.TutorialPlayed ?? _tutorialPlayed;
        // #394: restore whether this universe's crew turned the rock aside from Ringside — so its plaque
        // keeps the appended gratitude line across a reload (a pre-#394 save defaults false, harmless).
        _ringsideSaved = vault.Progress?.RingsideSaved ?? _ringsideSaved;
        // #440: restore whether this captain has had the first-ground lesson, so a reload never re-teaches
        // someone who has already walked a moon (a pre-#440 save defaults false — they get it once, next
        // trip down, and never again).
        _groundLessonSeen = vault.Progress?.GroundLessonSeen ?? _groundLessonSeen;
        // #563: same for the map-just-grew card — a captain who has already forced a door open is not told
        // again what forcing one does (a pre-#563 save defaults false: they get it once, on their next).
        _groundGrewSeen = vault.Progress?.GroundGrewSeen ?? _groundGrewSeen;
        // #562: same for the tube-feeds-you card — a captain who has already been racked in the tube is not
        // taught the supply line again (a pre-#562 save defaults false: they get it once, on their next).
        _tubeRearmSeen = vault.Progress?.TubeRearmSeen ?? _tubeRearmSeen;
        _airCardSeen = vault.Progress?.AirCardSeen ?? _airCardSeen;   // #573
        // #1066: and the berths since anybody was ashore. A crew kept aboard for six working stops do not
        // forget it over a reload, and neither does the promise it breaks. A pre-#1066 save simply lacks the
        // field and wakes as a ship that has just come off a gangway, which is the kind direction to be
        // wrong in — nobody is owed a run ashore they were never denied.
        _workingStopsSinceShoreLeave = vault.Progress?.WorkingStopsSinceShoreLeave ?? _workingStopsSinceShoreLeave;
        TheMilkRunResumes(vault.Progress?.MilkRunLessonStep); // #160: the lesson's own place, and its row
        // #409: restore the secret labs this thread has found, so a known body's hidden door stays revealed
        // on every future landing (a pre-#409 save simply lacks the field — an empty set, harmless).
        if (vault.Progress?.SecretLabsFound is { } found)
        {
            _secretLabsFound.Clear();
            foreach (string body in found)
            {
                _secretLabsFound.Add(body);
            }
        }

        // #701: and the odd books this thread has already filed a gist for, so a reload never re-files a
        // shelf the casebook already carries (a pre-#701 save simply lacks the field — an empty list).
        if (vault.Progress?.OddBooksRead is { } books)
        {
            _oddBooksRead = [.. books];
        }

        // #677: and the disclosure clock's register — the grounds whose halls this thread has crossed into,
        // with the world-side window each was opened in. Restored rather than re-derived, because there is
        // nothing to derive it FROM: which window a captain first stood past a seam in is not a fact about
        // the world, it is the one fact about the visit the clock keeps (a pre-#677 save lacks the field, so
        // an old captain's clock starts on their next crossing — the harmless direction).
        if (vault.Progress?.HallsOpened is { } opened)
        {
            _hallsOpened = [.. opened.Select(o => new DisclosureClock.Opening(o.BodyId, o.Window))];
        }

        // #1063: and which of them have since been filled in. Restored for the same reason and one harder —
        // a reload that un-buried a ground would make the captain's own field book wrong about the one thing
        // in this game the book is guaranteed right about.
        if (vault.Progress?.HallsBuried is { } buried)
        {
            _hallsBuried = [.. buried];
        }

        // #1068: and which of them the world has since declined on, with the window each declined in.
        // Restored rather than re-derived for the hardest version of the reason again: the window is what
        // the door is chosen against, so a save that dropped it would re-open the shut leaf and shut another
        // one somewhere else — a lock that moved by itself, which is the one reading this beat may not have.
        if (vault.Progress?.HallsDeclined is { } declined)
        {
            _hallsDeclined = [.. declined.Select(d => new PoliteDecline.Decline(d.BodyId, d.Window))];
        }

        // #1068: and which of them the harbour has filed paperwork about (Map.QuietHands.cs).
        RestoreQuietHands(vault.Progress);
        // #1074: and which of them the Authority has closed the working of (Map.Stop.cs), and which of THOSE
        // it has since taken into care — that one restores and installs in one call (Map.Preserve.cs).
        RestoreStop(vault.Progress);
        RestorePreserve(vault.Progress);
        // #525: and the one collar a harbour has cleared with a reason on it (Map.BerthScuttle.cs).
        RestoreClearedCollar(vault.Progress);
        // #1151: and the claims file — the counter, the claim awaiting a representative, and the writ
        // awaiting the master (Map.Claims.Presence.cs / Map.Claims.Kiosk.cs).
        _claimsLodged = vault.Progress?.ClaimsLodged ?? 0;
        _claimOwed = vault.Progress?.ClaimOwed;
        _writPending = vault.Progress?.WritPending;
        // #1151 slice 2: …and the loss the rep's offer has already been spent on (Map.Claims.Rep.cs).
        _lodgingOfferedFor = vault.Progress?.LodgingOfferedFor;

        // …and Core is told at once, rather than waiting for the next descent: a save loaded straight onto a
        // ground must come back to a shaft that already ends where the burial left it, to the same one
        // door the world had already declined, and to the seal an office had already posted.
        InstallBurialRegister();
        InstallDeclineRegister();
        InstallStopRegister();

        // #317 — the nerve gauge rides the vault losslessly: a captain who fled shaking is still shaking
        // after a reload, and the monolith's first-sight hit stays spent. A missing section defaults calm.
        if (vault.Nerve is { } nerve)
        {
            // #480: a voyage saved before the nerve was quantized carries an arbitrary float. Snap it onto
            // the pip lattice on the way in, so a legacy 63.4 reads as a clean 6 pips and never drifts again.
            _nerve = NervePips.Snap(NerveModel.Clamp(nerve.Nerve));
            _monolithSeen = nerve.MonolithSeen;
        }

        // The "overheard at the bar" book (owner 2026-07-18): the tips/rumors a player was handed are
        // durable and revisitable — they survive the reload rather than living-and-vanishing in a toast.
        _overheard = vault.Overheard is { } book ? [.. book.Lines] : [];
        _fieldNotes = vault.FieldNotes is { } field ? [.. field.Notes] : [];   // #587

        // #741 · …and the lines drawn across it. A pair this build cannot read is dropped rather than thrown
        // over, the same tolerance the satchel gets three lines down; a pre-#741 file simply has none, and a
        // book of loose ends is exactly what it was.
        _caseThreads = [];
        foreach (string stored in vault.CaseThreads?.Threads ?? [])
        {
            if (Core.CaseThreads.Thread.TryParse(stored, out Core.CaseThreads.Thread line))
            {
                _caseThreads = [.. Core.CaseThreads.Draw(_caseThreads, line.A, line.B)];
            }
        }

        // #563 slice 2 · The marks this captain left on the ground of every moon they walked. Restored
        // wholesale rather than merged, because a load is a different life and not this one continuing; a
        // pre-slice-2 file simply has none, and every hut on every moon is honestly dogged again.
        _groundMemory = GroundMemory.Restore(vault.Ground?.Changed);

        // #836 · The captain's paper trail — which identity was handed to which man, on which floor. A row
        // this build cannot parse is dropped rather than thrown over, the same tolerance the satchel gets;
        // a pre-#836 file simply has none, and every chooser row honestly reads "never shown".
        ForgetThePaperTrail();
        foreach (string stored in vault.PapersShown?.Shown ?? [])
        {
            if (WalletChoice.Shown.TryParse(stored, out WalletChoice.Shown row))
            {
                RestoreAPaperTrailRow(row);
            }
        }

        // #973 L1 · The filing line's marks. A reload must never re-grey a page the captain won back and
        // never re-roll one they lost, so the STATE rides the file rather than being recomputed — the roll
        // is deterministic, but the latch on a refusal is a fact about a life and not about a seed.
        RestoreFilingSection(vault.Filing);

        // #973 L5b · What the SPREAD found out about a walk-in. A thing the captain has worked out about
        // somebody does not become unknown again — least of all across a save — so the knowing rides the
        // file rather than waiting for the player to lay the same two papers down a second time.
        RestoreWalkInSection(vault.WalkIn);
        RestoreFinderSection(vault.Finder);   // #417 · and the case, which is written down, never re-rolled

        // #973 · The void's weather. A sentence the captain has worn out stays worn out across a save, and a
        // station the room was on about last time is still on something else today.
        RestoreTheWeatherSection(vault.InsuranceWeather);

        // #973 L5a · The old crew, the crossings and the held memories. A seeding that comes back short is
        // re-rolled from the thread id on first read — the roll is deterministic, so a pre-#973 save wakes
        // with exactly the four shipmates it would always have had.
        RestoreOldCrewSections(vault);

        // #603 · The satchel. Unreadable entries from an edited or future save are dropped rather than
        // thrown over — the vault is tolerant everywhere else and a mystery object is not worth a lost game.
        _satchel = [];
        foreach (string stored in vault.Satchel?.Items ?? [])
        {
            if (Core.Satchel.Item.TryParse(stored, out Core.Satchel.Item item))
            {
                _satchel = [.. Core.Satchel.Add(_satchel, item)];
            }
        }

        // #1016 · …AND WHICH OF THEM ARE ALREADY IN THE BOOK. The one register every reader and writer of the
        // dig goes through, restored as it was written: a key this build does not recognise is KEPT rather
        // than dropped (unlike the satchel above, which has to be able to build an object out of its row) —
        // an unknown key costs one string and can only ever say "already dug" about a sheet nothing in this
        // build can be holding, while dropping it would silently re-open a case the captain had closed.
        _workedUp.Clear();
        foreach (string sheet in vault.WorkedUp?.Sheets ?? [])
        {
            if (!string.IsNullOrWhiteSpace(sheet))
            {
                _workedUp.Add(sheet);
            }
        }

        RestoreTheRoomsGoneThrough(vault);   // #615/#573 · …and which rooms have already been gone through

        // #590 → #603 MIGRATION. An older save carries its cards in their own section and knows nothing
        // about a satchel. They are read in rather than dropped: a captain who earned an authority eleven
        // floors down must not lose it to a refactor, and this costs one loop that does nothing forever
        // after the first load.
        foreach (string id in vault.Authorities?.Cards ?? [])
        {
            if (UndergroundComplex.AuthorityCard.TryParse(id, out _))
            {
                _satchel = [.. Core.Satchel.Add(_satchel,
                    new Core.Satchel.Item(Core.Satchel.Kind.Authority, id))];
            }
        }

        ApplyResumeBerth(vault.Resume, vault.SavedSimTime);

        // #223 · THE WATCH RESUMES WITH THE HOARD. The discovery roll's bookmark rides the vault now
        // (CacheLedger.LastCheckedPeriod, applied above) — but a save written before it did carries
        // WatchNotStarted, and a watch that never starts is a hoard nothing can ever take. Seed it HERE,
        // after ApplyResumeBerth has set SimTime, so an old voyage picks the watch back up at the clock
        // the captain woke at rather than resolving every day since the epoch in one pass.
        if (_caches.Caches.Any(c => c.PlayerOwned))
        {
            SeedDiscoveryWatch();
        }

        // #638 · THE VOID'S CLOCK RESUMES TOO — or does not exist, which is what every save written before
        // this lane says. The sweep's cache is deliberately NOT restored: what the picture holds is a fact
        // about a course, and the first tick after the load re-asks it at the clock the captain woke at.
        _voidDeclaredDay = vault.Void?.DeclaredDay ?? VoidRule.ClockNotRunning;
        _voidLastToldDay = vault.Void?.LastToldDay ?? VoidRule.NothingToldYet;
        _voidSweptDay = long.MinValue;
        _voidHavenInReach = true;

        // Loading a saved game shows NONE of the tutorial promotions (owner, 2026-07-18) — set last so
        // even the no-berth ApplyStart("earth") fallback above can't leave the greeting raised.
        _showTutorial = false;
    }

}
