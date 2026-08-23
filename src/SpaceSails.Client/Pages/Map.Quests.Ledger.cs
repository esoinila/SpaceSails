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

// Subject: part of Map.Quests — the payout and the paperwork: the celebration a finished contract earns, and every section the Captain's desk draws out of these books.
public partial class Map
{

    // The fanfare (#185): a completion is a CELEBRATION, not a silent credit. One pop-up at a time;
    // extras queue so a double payout still gets its due.
    private MissionCelebration? _celebration;
    private readonly Queue<MissionCelebration> _celebrationQueue = new();

    // Turn in any finished contracts on berthing — and make the money's arrival a CELEBRATION
    // (#185, owner: "Now the money just appeared... It is a CELEBRATION"). The payout is booked
    // INSIDE the fanfare flow, the giver is grateful in their own voice, the parrot SINGS, and the
    // job is remembered as a real relationship. The reward never again just silently appears.
    private void PayCompletedQuests()
    {
        foreach (Quest q in _quests)
        {
            if (q.State != QuestState.Complete)
            {
                continue;
            }

            // PR-WIRE: a favor delivery pays no coin — working it off REPAYS the wired debt. Book the
            // principal back onto the ledger (balance climbs toward zero), clear the obligation, and
            // give it a quiet receipt rather than the coin fanfare (no money changed hands).
            if (q.Kind == QuestKind.Favor)
            {
                _contacts.ApplyCredit(q.Giver, q.Giver,
                    FavorBank.RepaymentTxn(q.Reward, SimTime, $"quiet delivery — favor to {GiverDisplay(q.Giver)} repaid"));
                _favorObligations.RemoveAll(o => string.Equals(o.ContactId, q.Giver, StringComparison.OrdinalIgnoreCase));
                AdvanceMission(q, QuestState.TurnedIn,
                    $"The favor's square with {GiverDisplay(q.Giver)} — the debt's off the books. 📡");
                continue;
            }

            _credits += q.Reward;            // the coin lands — but as part of the fanfare, not alone
            // #727 · the one writer, told nothing: the fanfare below IS this transition's beat, and it is a
            // card of its own — already on top of anything a banner would have gone behind (#736).
            AdvanceMission(q, QuestState.TurnedIn);

            // Book a real, saved fact: we now have a history with this task giver.
            ContactHistory history = _contacts.RecordCompletion(q.Giver, q.Giver, q.Reward, SimTime);
            _celebrationQueue.Enqueue(Celebrations.ForCompletion(
                q.Title, q.Giver, q.Reward, history.MissionsCompleted, _parrotCounter));
        }

        ShowNextCelebration();
        RequestVaultSave(); // #225: a payout changed the purse, contacts and obligations
    }

    // Surface the next queued fanfare (one pop-up at a time): the parrot SINGS, the cue plays.
    private void ShowNextCelebration()
    {
        if (_celebration is not null || _celebrationQueue.Count == 0)
        {
            return;
        }

        _celebration = _celebrationQueue.Dequeue();
        SquawkNow(Parrot.Squawk.ContractPaid, _lastTimestampMs ?? 0, force: true); // the bird sings 🦜
        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    private void DismissCelebration()
    {
        _celebration = null;
        ShowNextCelebration(); // if a second contract paid at the same berth, raise a glass to it too
    }

    // PR-WIRE — project the favor bank's accounts for the Captain ledger's 💰 section: every contact
    // with coin in the air (parked with them, or owed to them), balance both ways, newest passbook
    // lines first. Contacts with a clean-zero book and no transactions are skipped — nothing to show.
    private Stations.Captain.AccountRow[] LedgerAccounts()
    {
        var rows = new List<Stations.Captain.AccountRow>();
        foreach ((string id, ContactHistory h) in _contacts.Entries)
        {
            if (h.CreditBalance == 0 && h.Transactions.Length == 0)
            {
                continue;
            }
            ContactSheet sheet = ContactSheets.For(id);
            string channel = sheet.CanWire
                ? "🕸 wires anywhere — bank at the dark-web desk or their table"
                : "🤝 in person only — bank at their table (press B)";
            List<string> lines = h.Transactions
                .Reverse()
                .Take(6)
                .Select(t => $"{TxnIcon(t.Kind)} {(t.Amount >= 0 ? "+" : "")}{t.Amount:N0} cr — {t.Note} (day {t.SimTime / 86400:F0})")
                .ToList();
            rows.Add(new Stations.Captain.AccountRow(sheet.DisplayName, h.CreditBalance, channel, lines));
        }
        // Debts first (they need attention), then deposits, largest magnitude on top.
        rows.Sort((x, y) => Math.Sign(x.Balance).CompareTo(Math.Sign(y.Balance)) is var s && s != 0
            ? s
            : Math.Abs(y.Balance).CompareTo(Math.Abs(x.Balance)));
        return rows.ToArray();
    }

    private static string TxnIcon(CreditKind kind) => kind switch
    {
        CreditKind.Deposit => "💰",
        CreditKind.Withdrawal => "🏧",
        CreditKind.Interest => "📈",
        CreditKind.FenceCut => "✂",
        CreditKind.Borrow => "📡",
        CreditKind.Repayment => "✅",
        _ => "·",
    };

    // Project the quest ledger for the Captain's-desk Quests tab (M-Q2) — newest work on top.
    private Stations.Captain.QuestItem[] QuestCards() =>
        _quests.AsEnumerable().Reverse().Select(q =>
        {
            // #727 · The status label IS this contract's current step, and it is read out of the ONE place
            // that knows what step a contract is on — Map.Quests.Compass.CurrentStepOf. The satchel's
            // MISSIONS pane asks the same call for the same quest, which is what makes the desk and the pane
            // one model rather than two lists: a foot-level line down a lift shaft is this label, byte for
            // byte, and a wording changed here changes it there in the same edit.
            (MissionStep current, string kind) = CurrentStepOf(q);
            string label = current.Text;
            // #175: the delivery instruction is kind-aware — a MOON haven has no ⚓ dock, you park in
            // its orbit; only a STATION haven is "berth there". Saying the right one kills the trap the
            // owner hit hunting for a Dock button that a moon never has.
            CelestialBody? cargoDest = q.Kind is QuestKind.CargoRun or QuestKind.Favor ? BodyById(q.DestBodyId) : null;
            // #349: the accepted-job ledger line names the drop's ADDRESS (station — PLANET system), the
            // same idiom the offer used, so the captain can find the planet from the ledger too.
            string cargoAddress = cargoDest is not null ? BodyAddress(cargoDest.Id) : q.TargetCallsign;
            string cargoDetail = cargoDest is not null && !IsDockableHaven(cargoDest)
                ? $"Carry the parcel to {cargoAddress} — park in orbit there to deliver."
                : $"Carry the parcel to {cargoAddress} — berth there (⚓ Dock) to deliver.";
            string detail = q.Kind switch
            {
                QuestKind.Hunt => $"Hunt {q.TargetCallsign} — hole her sail or board her.",
                QuestKind.CargoRun => cargoDetail,
                QuestKind.Favor => $"{cargoDetail} Working it off clears the {q.Reward:N0} cr you owe {GiverDisplay(q.Giver)}.",
                QuestKind.Intel => $"Off-books route on {q.TargetCallsign} — now on your contacts (🕸).",
                QuestKind.Fetch => $"Prise the wallet from the derelict roadster (sunward of Mars), then hand it to The Fixer in person at {q.TargetCallsign}.",
                QuestKind.Crack => $"Key {q.Pin} into hatch {q.TargetShipId} here, lift the package, then hand it back to The Fixer.",
                QuestKind.FetchCache => $"Take the shuttle down to {BodyName(q.SourceBodyId ?? "")}, dig up the marked chest, then carry the lot to {q.TargetCallsign}.",
                _ => "",
            };
            // #175: the live next action for an in-hand cargo run, read off ship state (too far / in the
            // envelope / enter orbit). Only while Active — a delivered run drops back to the plain card.
            string? nextAction = q is { Kind: QuestKind.CargoRun or QuestKind.Favor, State: QuestState.Active } && cargoDest is not null
                ? CargoNextAction(cargoDest)
                : null;
            // #959 — the four plain lines, measured off the live sim by JobFactsFor and worded by Core's
            // JobTerms. The SAME call the offer card made before the job was accepted, so a captain who
            // took a job on the strength of "≈ 3.60 M km · ~6 d by the lanes" reads that same sentence in
            // his ledger afterwards rather than a different one.
            IReadOnlyList<string> plain = JobPlainBlock(q);
            // The foot's purse line now carries its size word ("764 cr · small") — the owner's other #959
            // complaint, that the number alone never said whether it was worth the trip. Intel and worked-off
            // favors keep their own faces, because neither pays in loose coin.
            string rewardText = q.Kind == QuestKind.Intel
                ? "🕸 route tip"
                : q.Kind == QuestKind.Favor
                    ? $"📡 clears {q.Reward.ToString("N0", CultureInfo.InvariantCulture)} cr favor"
                    : plain.Count > 0
                        ? plain[3]
                        : $"{q.Reward.ToString("N0", CultureInfo.InvariantCulture)} cr";
            (IReadOnlyList<Stations.Captain.QuestStep> steps, bool showScope) = FetchStagePlan(q);
            return new Stations.Captain.QuestItem(q.Title, detail, rewardText, label, kind, steps, showScope, nextAction, plain);
        }).ToArray();

    // The fetch hunt's staged plan for the quest card (Tuesday plan PR-A): intel → scan → fly →
    // pick up → deliver, ✅ for done, ▶ for the current stage, ▪ for ahead — the tutorial-checklist
    // shape. Also returns whether to surface the 🔭 hook (only while she's still uncharted). Any
    // non-fetch quest gets no checklist.
    private (IReadOnlyList<Stations.Captain.QuestStep> Steps, bool ShowScope) FetchStagePlan(Quest q)
    {
        if (q.Kind != QuestKind.Fetch)
        {
            return ([], false);
        }
        bool charted = q.SourceBodyId is not { } src || !IsBodyHidden(src); // a completed scan charted her
        bool pickedUp = q.State is QuestState.PickedUp or QuestState.Complete or QuestState.TurnedIn;
        bool delivered = q.State is QuestState.TurnedIn;
        (string Text, bool Done)[] flags =
        [
            ("Intel — read the Fixer's transponder fix", true),
            ("Scan — point the scope, resolve her", charted),
            ("Fly — close on the wreck", pickedUp),
            ("Pick up — prise the wallet loose", pickedUp),
            ("Deliver — hand it to The Fixer in person", delivered),
        ];
        int current = Array.FindIndex(flags, f => !f.Done);
        var steps = flags
            .Select((f, i) => new Stations.Captain.QuestStep(f.Done ? "✅" : i == current ? "▶" : "▪", f.Text))
            .ToList();
        return (steps, ShowScope: !charted);
    }

    // The ledger's "Tips & intel" section (PR-J): every scope-intel fix and every fresh route tip,
    // projected for the Captain desk. Scope tips keep their 🔭 action (jump to Sensors, scan queued);
    // route tips carry "→ dark web" always and "→ dossier" when the ship is a known contact. Provenance
    // is attached where we recorded it (Fixer fetches, Gilt-Eye tips, the cheats); older/bought entries
    // render unattributed rather than being withheld.
    // #223: the ledger's 🗺 treasure-maps section — every known cache as a viewable map card.
    private Stations.Captain.CacheMapItem[] LedgerMaps() =>
        _caches.Caches.Select(c => new Stations.Captain.CacheMapItem(
            c.Id, c.Caption(BodyName(c.BodyId)), c.BearingLine, c.ContentsLine(),
            GiverDisplay(c.Owner), c.PlayerOwned)).ToArray();

    // Open the full-screen map card for a cache the captain clicked in the ledger.
    private void ViewMapFromLedger(string cacheId)
    {
        foreach (TreasureCache c in _caches.Caches)
        {
            if (c.Id == cacheId)
            {
                _treasureMapCard = c;
                return;
            }
        }
    }

    private Stations.Captain.LedgerTip[] LedgerTips()
    {
        var tips = new List<Stations.Captain.LedgerTip>();

        // The autopilot's receipts (#147): every stand-down filed as a ledger line, newest first, so a
        // handback the owner warped past is still on the record afterward — the established tip idiom.
        // 2026-07-18 playtest: the provenance is the receipt's AGE, not the wall clock — a line cut
        // seconds ago now reads "logged just now", never "logged 0d 16h 13m" (LedgerClock).
        foreach ((double simTime, string text) in _autopilotEvents)
        {
            tips.Add(new Stations.Captain.LedgerTip(
                "🛰 Autopilot", [text], $"logged {LedgerClock.Age(simTime, SimTime)}",
                ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null,
                // #973 L1 · a dated page, and therefore one the filing line can take away. Keyed off the
                // receipt's own stamp and its text, never its rendered age — the age changes every minute.
                EntryId: FilingLine.EntryId("autopilot", simTime, text), SimTime: simTime));
        }

        // #202: the piracy receipts — the shadow ledger of the honest jobs. What, units, worth, off
        // whom, where; the sim-when rides the provenance line as an AGE, same as the autopilot receipts.
        foreach (LootRecord loot in _lootLedger)
        {
            tips.Add(new Stations.Captain.LedgerTip(
                "🏴 Plunder", [loot.Describe()], $"taken {LedgerClock.Age(loot.SimTime, SimTime)}",
                ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null,
                EntryId: FilingLine.EntryId("plunder", loot.SimTime, loot.Describe()), SimTime: loot.SimTime));
        }

        foreach (ScopeIntel si in _scopeIntel)
        {
            string? prov = si.Giver is { } giver ? ProvenanceLine(giver, si.Station ?? "ashore", si.AcquiredSimTime) : null;
            tips.Add(new Stations.Captain.LedgerTip(
                si.Headline, si.Lines, prov,
                ScopeTipId: si.Id, ShowDarkWeb: false, DossierShipId: null,
                EntryId: FilingLine.EntryId("scope", si.AcquiredSimTime, si.Id), SimTime: si.AcquiredSimTime));
        }

        foreach (RouteIntel entry in _intelLedger.Entries)
        {
            if (!entry.IsFresh(SimTime))
            {
                continue;
            }
            NpcState? npc = FindNpc(entry.ShipId);
            string ship = npc?.Ship.Callsign ?? entry.ShipId;
            string route = npc is not null ? RouteLabel(npc.Ship) : "route off the books";
            double staleDays = Math.Max(0, entry.SecondsUntilStale(SimTime) / 86400);
            string line = $"{ship} really runs {route} — a ghost, on your contacts (🕸), stale in {staleDays.ToString("F0", CultureInfo.InvariantCulture)} d.";
            bool known = _routeIntelProvenance.TryGetValue(entry.ShipId, out IntelProvenance? p);
            string? prov = known ? ProvenanceLine(p!.Giver, p.Station, p.AcquiredSimTime) : null;
            double? acquired = known ? p!.AcquiredSimTime : null;
            tips.Add(new Stations.Captain.LedgerTip(
                $"🕸 {ship}", [line], prov,
                ScopeTipId: null, ShowDarkWeb: true, DossierShipId: npc is not null ? entry.ShipId : null,
                // #973 L1 · dated only when we know who handed it over and when. A route tip bought off the
                // books carries no stamp, so there is no filing line to put it on either side of.
                EntryId: acquired is { } at ? FilingLine.EntryId("route", at, entry.ShipId) : null,
                SimTime: acquired));
        }

        // #347 — the BUG the owner hit: the rumors and tips a contact hands you over a drink (and the
        // barkeep's, and a round's volunteered whispers) were written to the durable overheard book and
        // shown AT the counter, but never crossed into the Captain's ledger — so from this desk they
        // "did not happen". Collect them here, GROUPED PER CONTACT (Core projection), each carrying who
        // told you and where. Owner's vibe for the section: "Tips, Intel, Rumors :-D".
        foreach (Core.LedgerRumor rumor in Core.OverheardLog.PerContact(_overheard))
        {
            string who = GiverDisplay(rumor.Source);
            tips.Add(new Stations.Captain.LedgerTip(
                $"👂 {who}",
                rumor.Lines.Select(l => l.Text).ToArray(),
                ProvenanceLine(who, rumor.LatestBar, rumor.LatestSimTime),
                ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null,
                EntryId: FilingLine.EntryId("overheard", rumor.LatestSimTime, rumor.Source),
                SimTime: rumor.LatestSimTime));
        }

        // #587 — THE FIELD BOOK, in the ledger. Owner, on a rebuilt site: "we should maybe collect the tips
        // to ledger if we don't show them again?" Everything found on a surface used to arrive as a pulse
        // that faded in eight seconds and was then gone for good — a sentence you walked twenty minutes
        // across a vacuum for and could never read twice. Same failure the bar had (#347), same fix, grouped
        // by PLACE rather than by contact: out there the thing you want back is where you were standing.
        foreach (Core.FieldFinding found in Core.FieldNotes.PerPlace(_fieldNotes))
        {
            tips.Add(new Stations.Captain.LedgerTip(
                $"🥾 {found.Place}",
                found.Lines.Select(l => $"{l.Glyph} {l.Text}").ToArray(),
                $"found on the ground · day {(found.LatestSimTime / 86400).ToString("F0", CultureInfo.InvariantCulture)}",
                ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null,
                EntryId: FilingLine.EntryId("field", found.LatestSimTime, found.Place),
                SimTime: found.LatestSimTime));
        }

        // #973 L5a · THE SHEETS. A held memory is a dated row like anything else, and one of them — the
        // summer party — carries the mark that says the SERVICE filed it, which is why an uninsured rebirth
        // greys the whole book around it and leaves it alone.
        tips.AddRange(HeldMemoryTips());

        // #208: a standing note explaining the haven/depot pair the picker now tags — the owner asked
        // for it "in ledger" so the twin-port confusion has one discoverable, permanent answer. Filed
        // last so live tips stay on top; it is evergreen background, no action.
        tips.Add(new Stations.Captain.LedgerTip(
            "⚓ Ports come in twos",
            ["The haven has the bar and the berth; the depot is the pod riding nearby with the goods. Dock at havens; board depots."],
            "standing note",
            ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null));

        // #411: the PROJEKTI KAAMOS intel readout — lead with the ice-moon mystery whenever any shard is in
        // hand, so it builds visibly as the player collects (the assembled shard texts stay re-readable here).
        if (KaamosLedgerTip() is { } kaamos)
        {
            tips.Insert(0, kaamos);
        }

        // #425/#422: the NEBULA MUTUAL readout — surfaced whenever a shard is in hand (the oracle is its first
        // delivery vector). Inserted above KAAMOS so the newest rabbit hole leads, both riding atop live tips.
        if (NebulaLedgerTip() is { } nebula)
        {
            tips.Insert(0, nebula);
        }

        return tips.ToArray();
    }
}
