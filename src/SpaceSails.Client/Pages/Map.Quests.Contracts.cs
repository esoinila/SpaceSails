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

// Subject: part of Map.Quests — taking the job and working it out: Accept or Pass, the brief's facts, the pickup, the two hand-offs, and the berth that settles a run.
public partial class Map
{

    private string WreckRevealMessage(string bodyId) => bodyId == Derelict.RoadsterBodyId
        ? "🔭 There she is — a cherry-red glint on the return, right where the tip said. Contact: the Derelict Roadster."
        : $"🔭 Scan resolved a new contact — {BodyName(bodyId)} is on the charts now.";

    private void AcceptOffer()
    {
        if (_pendingOffer is not { } offer)
        {
            return;
        }
        _pendingOffer = null;

        // #635 — the returned filing. It is a contract in every respect a player can see (a card, a price,
        // Take or Pass) and in exactly one respect it is not: there is nothing afterwards to go and do, so
        // it never enters _quests. What it settles is settled at the counter, in front of you.
        if (offer.Id == KaamosBounceOfferId)
        {
            TakeKaamosBounceFiling(offer);
            return;
        }

        if (offer.Kind == QuestKind.Intel)
        {
            // A gift of information — delivered on the spot: drop the route tip into the ledger (like a
            // dark-web buy, but free) and log it as a settled entry. No hunt, no dock payout.
            _intelLedger.Add(new RouteIntel(offer.TargetShipId, SimTime, RouteIntel.DefaultValiditySeconds, Price: 0));
            _routeIntelProvenance[offer.TargetShipId] = new IntelProvenance(offer.Giver, DockedStationName(), SimTime);
            _quests.Add(offer);
            // #727 · Settled at birth, but through the one writer all the same — an exception in the law is
            // how the next author learns there is somewhere else to write this enum.
            AdvanceMission(offer, QuestState.TurnedIn,
                $"Tip logged — {offer.TargetCallsign} is on your contacts now (🕸 stale in 30 d) — filed in the Captain's ledger (0).");
            return;
        }

        _quests.Add(offer);

        // Tuesday plan PR-A: a fetch job no longer leaves the wreck labelled on the map. The Fixer
        // hands you a transponder fix instead — an intel card at the Comms desk with a 🔭 hook that
        // aims the scope. The wreck stays hidden until an actual scan resolves it.
        if (offer.Kind == QuestKind.Fetch && offer.SourceBodyId is { } wreckId && IsBodyHidden(wreckId)
            && !_scopeIntel.Any(si => si.BodyId == wreckId))
        {
            _scopeIntel.Add(BuildWreckIntel(wreckId, offer.Giver, DockedStationName()));
        }

        // #223: accepting a cache run LEARNS the target cache (same code path as a rumour map) — the
        // shuttle-bay 🗺 Dig door now appears at that body, and the map lands in the ledger's 🗺 section.
        if (offer.Kind == QuestKind.FetchCache && offer.Pin is { } drawKey && offer.SourceBodyId is { } digBody)
        {
            _caches.Learn(RumorMaps.Generate(drawKey, digBody).Cache);
        }

        // #207: accepting ANY contract kind now SPEAKS in-face — a #119-style receipt naming the job
        // and its giver, then the immediate next action — so the captain is never left guessing at the
        // moment of acceptance ("I took the parcel but the mission is quite unclear"). The live
        // next-action also rides the Captain desk chip (CaptainQuestChipLine) while the job is in hand.
        // #411 — the run gets the arc's own receipt as well as the house one: the manifest slug is the
        // cold pod's, word for word, because it is the same consignment. Nobody remarks on that.
        if (offer.Id == KaamosLore.SupplyRunQuestId)
        {
            ShowPulseMessage(KaamosLore.SupplyRunAccepted);
            return;
        }

        ContractFacts facts = FactsFor(offer);
        ShowPulseMessage($"{MissionBrief.Receipt(facts.Kind, facts.Giver)} {MissionBrief.NextLine(facts)}");
    }

    // #207: map a live quest onto the pure Core brief text (giver title-cased, delivery world named
    // off the ephemeris). Kind-specific fields: a crack names its hatch id, a hunt/intel its prey.
    private ContractFacts FactsFor(Quest q) => new(
        Kind: ToContractKind(q.Kind),
        Giver: GiverDisplay(q.Giver),
        DestName: q.TargetCallsign,
        DestParent: q.Kind is QuestKind.CargoRun or QuestKind.Favor ? DestParentName(q.DestBodyId) : null,
        TargetName: q.Kind == QuestKind.Crack ? q.TargetShipId : q.TargetCallsign,
        Pin: q.Pin,
        Charted: q.SourceBodyId is not { } src || !IsBodyHidden(src),
        PickedUp: q.State is QuestState.PickedUp,
        CacheBody: q.Kind is QuestKind.FetchCache or QuestKind.WalkIn ? BodyName(q.SourceBodyId ?? "") : null);

    private static ContractKind ToContractKind(QuestKind kind) => kind switch
    {
        QuestKind.Hunt => ContractKind.Hunt,
        QuestKind.CargoRun => ContractKind.CargoRun,
        QuestKind.Intel => ContractKind.Intel,
        QuestKind.Fetch => ContractKind.Fetch,
        QuestKind.FetchCache => ContractKind.FetchCache,
        QuestKind.Crack => ContractKind.Crack,
        QuestKind.Favor => ContractKind.CargoRun, // a favor delivery reads as a cargo run in the brief
        QuestKind.WalkIn => ContractKind.WalkIn,  // #973 L5b · a FIND whose payout line is a dash
        _ => ContractKind.CargoRun,
    };

    // The delivery world for a cargo run's "…, Mars" place tag — the destination haven's parent
    // planet, skipping a heliocentric station's parent (the sun, which reads wrong as a place).
    private string? DestParentName(string? destId)
    {
        if (BodyById(destId) is not { ParentId: { } pid }) return null;
        if (BodyById(pid) is not { } parent) return null;
        return parent.ParentId is null ? null : parent.Name;
    }

    // #349 — the PLANET-LEVEL ancestor of a body: walk up parents until the next one up is the
    // parentless root (the Sun). A moon or a station rides its planet around the Sun, so this is the
    // body whose heliocentric orbit sets both the reward's "reach" and the place's system name. A body
    // that IS a planet (its parent is the Sun) returns itself; a heliocentric station returns itself too.
    private CelestialBody? PlanetLevelAncestor(CelestialBody? body)
    {
        CelestialBody? b = body;
        while (b is { ParentId: { } pid } && BodyById(pid) is { } parent)
        {
            if (parent.ParentId is null) break; // parent is the Sun — b is already planet-level
            b = parent;
        }
        return b;
    }

    // #349 — a body's heliocentric orbit radius (metres): the orbit radius of its planet-level ancestor,
    // i.e. how far out in the solar system it actually sits. 0 for the Sun or an unknown id. This is the
    // input HaulReward scales a contract's purse on, so a Uranus berth pays for a Uranus-deep haul.
    private double HelioRadiusMeters(string? bodyId) =>
        PlanetLevelAncestor(BodyById(bodyId))?.OrbitRadius ?? 0.0;

    // #349 — a place's ADDRESS: its own name plus the PLANET whose system it rides in, in one house idiom
    // ("Ringside Exchange — SATURN system"). The owner's pain (2026-07-18): "how can I even know what
    // planet this place is on ... Am I to zoom into every planet and moon to find this place?" A planet
    // itself, or a heliocentric station with no planet above it, reads plainly by name — there is no
    // system to name. Used on every offer blurb and ledger line that points the captain at a berth.
    private string BodyAddress(string? bodyId)
    {
        CelestialBody? b = BodyById(bodyId);
        if (b is null)
        {
            return bodyId is null ? "" : BodyName(bodyId);
        }
        CelestialBody? planet = PlanetLevelAncestor(b);
        return planet is null || planet.Id == b.Id
            ? b.Name
            : $"{b.Name} — {planet.Name.ToUpperInvariant()} system";
    }

    // Title-case a giver's shout-name for prose ("MADAM COIL" → "Madam Coil", "GILT-EYE" →
    // "Gilt-Eye", "ONE-EYE SILAS" → "One-Eye Silas"). The offer card keeps the loud upper-case ◈
    // name; the ledger receipt and the next-action line read as sentences, so they title-case it.
    private static string GiverDisplay(string giver)
    {
        if (string.IsNullOrWhiteSpace(giver)) return giver;
        // #973 L5a — an old shipmate's row is keyed by a prefixed id and their name is authored rather than
        // shouted, so title-casing it would turn Teodor "Teo" Brask into Teodor "teo" Brask.
        if (Core.OldCrew.IsAnOldShipmate(giver)
            && Core.OldCrew.ById(giver[Core.OldCrew.LedgerPrefix.Length..]) is { } shipmate)
        {
            return shipmate.Name;
        }

        return string.Join(' ', giver
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(TitleWord));

        static string TitleWord(string w) => string.Join('-',
            w.Split('-').Select(p => p.Length == 0
                ? p
                : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    // #207: the live next action for the Captain chip — the contract still in hand, its immediate
    // step. An Active cargo run overlays the POSITIONAL detail (too far / in the envelope), read off
    // ship state; every other kind uses the static per-kind step (MissionBrief.Action).
    private string? CaptainQuestChipLine()
    {
        Quest? q = _quests.FirstOrDefault(x => x.State is QuestState.Active or QuestState.PickedUp);
        if (q is null) return null;
        string action = q is { Kind: QuestKind.CargoRun, State: QuestState.Active } && BodyById(q.DestBodyId) is { } dest
            ? $"deliver to {q.TargetCallsign} — {CargoNextAction(dest)}"
            : MissionBrief.Action(FactsFor(q));
        return action.Length == 0 ? null : MissionBrief.NextPrefix + action;
    }

    private void DeclineOffer()
    {
        if (_pendingOffer is null)
        {
            return;
        }
        _pendingOffer = null;
        ShowPulseMessage("“Suit yourself.” The stranger turns back to their drink.");
    }

    // A brought-down target settles any matching hunt contract. Called where a ship is holed or
    // boarded (its two "brought down" moments), keyed on the ship id the quest stored.
    private void CompleteHuntQuests(string shipId)
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.Hunt, State: QuestState.Active } && q.TargetShipId == shipId)
            {
                // #727 · through the one writer, and said where the captain is looking (#736).
                AdvanceMission(q, QuestState.Complete,
                    $"Contract met — {q.TargetCallsign} is down. {q.Reward:N0} cr waiting at any haven. 🎯");
            }
        }
    }

    // #244: the pickup's range and the autopilot's arrival are ONE number now — the armed arrival at a
    // berth you cannot clamp onto closes to exactly this, so the trip ends where the errand happens.
    private const double FetchPickupRangeM = DockRule.AlongsideMeters;

    // Coasting close to a fetch job's derelict prises the goods loose — flips Active → PickedUp. Called
    // each tick while flying; player-driven state, never read by the physics sim, and idempotent (only
    // an Active fetch matches, so it fires once).
    private void CheckFetchPickup()
    {
        if (_ephemeris is null || _dockedHavenId is not null)
        {
            return;
        }
        foreach (Quest q in _quests)
        {
            if (q is not { Kind: QuestKind.Fetch, State: QuestState.Active } || q.SourceBodyId is null)
            {
                continue;
            }
            Vector2d wreck = _ephemeris.Position(q.SourceBodyId, SimTime);
            if ((_ship.Position - wreck).Length <= FetchPickupRangeM)
            {
                // #727 · the one writer. This leg IS the chair's — a proximity the ship earns by coasting,
                // and the compass collapses it accordingly — but it goes through the same door all the same,
                // so "the chair advanced it" and "the boots advanced it" stay one code path.
                AdvanceMission(q, QuestState.PickedUp,
                    $"Got it — the wallet was wedged between the seats. Now get it to {q.TargetCallsign}, quiet-like. 💾");
                RendererInterop.PlayCue("board");
            }
        }
    }

    // Hand the fetched goods to The Fixer at the destination station — face to face, paid under the
    // table, no electronic trace. Unlike a cargo run (settled on berthing), this only completes when
    // you walk to the bar and talk to the contact.
    private void DeliverFetch(Quest q)
    {
        _credits += q.Reward;
        // A history builds even in the shadows — but quietly: no fanfare would suit an under-the-
        // table hand-off, so the relationship is seeded (#185) without the pop-up the bar job gets.
        _contacts.RecordCompletion(q.Giver, q.Giver, q.Reward, SimTime);
        // #727/#736 · A STEP FINISHED ON FOOT — you walked to his table and pressed [E] on him. It goes
        // through the one writer the chair's own legs go through, and the receipt lands on whatever pop-up
        // that press left in front of the captain rather than on a banner behind its backdrop.
        AdvanceMission(q, QuestState.TurnedIn,
            $"The wallet changes hands under the table — +{q.Reward:N0} cr, and we never met. 🕶");
    }

    // Hand the cracked-hatch package back to the Fixer, same station, same under-the-table terms.
    private void DeliverCrack(Quest q)
    {
        _credits += q.Reward;
        _contacts.RecordCompletion(q.Giver, q.Giver, q.Reward, SimTime); // seed the history, keep it quiet
        // #727/#736 · The other hand-off, on foot at the same table, through the same writer.
        AdvanceMission(q, QuestState.TurnedIn,
            $"The package slides across the table — +{q.Reward:N0} cr, no receipt. 🕶");
    }

    // Berthing at a haven settles any cargo-run contract bound for it. Called from ToggleDock.
    private void CompleteCargoRunQuests(string dockId)
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.CargoRun or QuestKind.Favor, State: QuestState.Active } && q.DestBodyId == dockId)
            {
                AdvanceMission(q, QuestState.Complete, q.Kind == QuestKind.Favor
                    ? $"Quiet parcel delivered to {q.TargetCallsign} — the favor's worked off. 📡"
                    : $"Parcel delivered to {q.TargetCallsign} — {q.Reward:N0} cr on the counter. 📦");
            }
            // #223: a fetch-a-cache job pays when the DUG chest is carried back to the giver's bar.
            else if (q is { Kind: QuestKind.FetchCache, State: QuestState.PickedUp } && q.DestBodyId == dockId)
            {
                AdvanceMission(q, QuestState.Complete,
                    $"Chest delivered to {q.TargetCallsign} — {q.Reward:N0} cr for the recovery. 🗺");
            }
            // #973 L5b · HER FAVOUR HAS TWO BERTHS AND NO COIN AT EITHER. The first is where the thing is;
            // the second is her, and the second is the point. Both legs are settled by the same dock event
            // every other job is, so nothing new watches the ship — what is different is what ARRIVING
            // MEANS, and that is the walk-in's own file to say.
            else if (q is { Kind: QuestKind.WalkIn, State: QuestState.Active } && q.SourceBodyId == dockId)
            {
                YouFindWhatSheAskedFor(q);
            }
            else if (q is { Kind: QuestKind.WalkIn, State: QuestState.PickedUp } && q.DestBodyId == dockId)
            {
                YouComeBackAndTellHer(q);
            }
        }
    }

    // #223: digging up a cache advances any fetch-a-cache job that pointed at it — the chest is now in
    // hand (PickedUp); the giver's bar is the drop. Idempotent per quest.
    private void CompleteFetchCacheFor(TreasureCache cache)
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.FetchCache, State: QuestState.Active } && q.TargetShipId == cache.Id)
            {
                // #727/#736 · THE DIG is the purest on-foot completion in the game: a shovel, a hole, and
                // the chair three hundred thousand kilometres up. Through the one writer, and the next-step
                // line lands on whatever the shovel left in front of the captain.
                AdvanceMission(q, QuestState.PickedUp,
                    $"{MissionBrief.NextPrefix}{MissionBrief.Action(FactsFor(q))}");
            }
        }
    }

    // #175: a MOON haven (mu > 0) has no ⚓ dock to clamp — the same as the lie-low rule, where
    // IsHiddenAtHaven treats being BOUND in a haven moon's orbit as "at the haven". So a cargo run to
    // a moon haven delivers the instant the ship is bound in its orbit; only STATION havens (mu = 0)
    // deliver on the dock, which CompleteCargoRunQuests handles from ToggleDock. This closes the trap
    // the owner hit: "berth there to deliver" at Enceladus (a moon) pointed at a door that never existed.
    private bool IsBoundAtMoonHaven(CelestialBody dest)
    {
        if (_ephemeris is null || dest.ParentId is null || IsDockableHaven(dest))
        {
            return false; // a station haven delivers on ⚓ Dock, not by orbit
        }

        if (BodyById(dest.ParentId) is not { } parent)
        {
            return false;
        }

        Vector2d pos = _ephemeris.Position(dest.Id, SimTime);
        const double h = 1.0;
        Vector2d vel = (_ephemeris.Position(dest.Id, SimTime + h) - _ephemeris.Position(dest.Id, SimTime - h)) / (2 * h);
        double hill = OrbitRule.HillRadius(dest, parent.Mu);
        return OrbitRule.IsBound(_ship, pos, vel, dest, hill);
    }

    // #175: settle any moon-haven cargo run the instant the ship is parked in its orbit. Runs on every
    // insertion (manual + autopilot) AND once per tick from UpdateEncounters, so a captain who was
    // ALREADY orbiting when the parcel loaded still gets paid — there is no dock event to hang it on.
    // Station-haven runs stay on the ToggleDock path (CompleteCargoRunQuests) and are skipped here.
    private void CompleteBoundCargoRunQuests()
    {
        if (_ephemeris is null) return;
        bool delivered = false;
        foreach (Quest q in _quests)
        {
            if (q is not { Kind: QuestKind.CargoRun or QuestKind.Favor, State: QuestState.Active, DestBodyId: { } destId }) continue;
            if (BodyById(destId) is not { } dest || IsDockableHaven(dest)) continue; // stations: ⚓ Dock path
            if (IsBoundAtMoonHaven(dest))
            {
                AdvanceMission(q, QuestState.Complete, q.Kind == QuestKind.Favor
                    ? $"Quiet parcel delivered to {q.TargetCallsign} — the favor's worked off. 📡"
                    : $"Parcel delivered to {q.TargetCallsign} — {q.Reward:N0} cr on the counter. 📦");
                delivered = true;
            }
        }

        // No berthing event fires for a moon-haven park, so settle the payout here — the orbit IS the
        // berth. PayCompletedQuests is idempotent (only Complete → TurnedIn), so this can't double-pay.
        if (delivered) PayCompletedQuests();
    }

    // #175: the in-hand cargo run whose destination is this body, or null. Used to paint the 📦 map
    // marker and the live next-action line only while a run to it is actually Active.
    private Quest? ActiveCargoRunTo(string bodyId) =>
        _quests.FirstOrDefault(q => q is { Kind: QuestKind.CargoRun, State: QuestState.Active } && q.DestBodyId == bodyId);
}
