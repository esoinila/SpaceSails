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

// Map.Vault — #251 · split from Map.Vault.cs, moved verbatim and whole: ApplyVault, the machinery that
// rehydrates a saved run. Its inverse, BuildVault, and the new-game reset stay in Map.Vault.cs.
public partial class Map
{
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

        // #640 · …and the pen comes back up. Loading a banked moment is boarding a life that was still being
        // lived; whether ITS captain had a pattern on file is a fact in the payload below
        // (NebulaSection.PolicyClosed), never a fact about whichever run happened to end in this tab.
        _threadIsOver = false;

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

        // ── #325/#332 · THE CHANDLERY'S STORES ─────────────────────────────────────────────────────────
        //
        //  Its own section, and read outside the ship block on purpose: a vault written before this lane
        //  simply has no chandlery section, and such a file has nothing to say about either count. It opens
        //  the way every captain has opened until now — nothing in stores, a full cabinet — which is what
        //  the field initialisers already hold, so the absent case is the no-op it should be.
        if (vault.Chandlery is { } chandlery)
        {
            // Spare bottles: clamped at zero and nothing else. A count of stores has no ceiling the game
            // imposes — they stack by the owner's own ask — and a save is not the place to invent one.
            _extendedTanks = Math.Max(0, chandlery.ExtendedTanks);

            // The cabinet. A NULL here is a section that carries no pill key and genuinely has nothing to
            // say, so it loads FULL. A recorded zero is an EMPTY cabinet and stays empty: rounding it up on
            // load would be a free restock for anybody who reloads, which is the same exploit shape the heat
            // section exists to refuse.
            _pills = Math.Clamp(
                chandlery.MedKitPills ?? Core.Chandlery.MedKitFullStock, 0, Core.Chandlery.MedKitFullStock);
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

        // #1063 slice 2: and the one seal already found empty. Assigned rather than guarded on null, so a
        // thread that never spent it loads as unspent and a file written before this shipped does too — the
        // one rule this latch has is that it only ever goes from null to a key, never back.
        _emptySealSpentOn = vault.Progress?.EmptySealSpentOn;

        // #1199: and the one person already followed and not found, with the counter the sighting was paid
        // at. Assigned rather than guarded on null for the identical reason — a voyage that never spent it
        // loads unspent, and a file written before this shipped does too.
        _observationWalkSpentOn = vault.Progress?.ObservationWalkSpentOn;
        _observationWalkSightingAt = vault.Progress?.ObservationWalkSightingAt;

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
        RestoreShuttle(vault.Progress);   // #1074 beat 5
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
            _pendantFirstOpened = nerve.PendantFirstOpened; // #620
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
