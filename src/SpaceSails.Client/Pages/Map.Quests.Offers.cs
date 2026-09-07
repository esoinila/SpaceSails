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

// Subject: part of Map.Quests — who is at the table and what they slide across it: the walk-up, the Magpie's rota, every Make…Offer, and the neighbourhood law that keeps the work close.
public partial class Map
{

    private void TalkToStranger()
    {
        if (_pendingOffer is not null || _patronDrink is not null)
        {
            return; // the card's already on the table
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.BarPatron } spot)
        {
            return;
        }
        string giver = spot.Label.Replace("◈", "").Trim();

        // #417 · …and a finder's case may be about this face at this port. It TAKES nothing: the regular
        // still opens their own table card and still hands over whatever work they had — what the case adds
        // is the entry in the field book, under this person's name as well as its own.
        TheWitnessMayHaveSeenIt(giver);

        // The station oracle (#425): Solenne "Static" Marsh wears a BarPatron console but is no quest-giver —
        // route her to the ranting-oracle flow before any give-work path. Matched by name (OracleRant.IsOracle),
        // the same idiom the Magpie is matched by.
        if (OracleRant.IsOracle(spot.Label))
        {
            TalkToOracle();
            return;
        }

        // The roaming Magpie (PR-F, "people cannot be static furniture"): interaction is gated on their
        // sim-time rota, so walking up to a chair they've left tells you they've moved on, not gives an
        // offer. Handled before the generic give-work paths, which assume a patron who stays put.
        if (giver.Contains("MAGPIE", StringComparison.OrdinalIgnoreCase))
        {
            TalkToMagpie(spot);
            return;
        }

        // Face-to-face hand-off (no electronic trace): a picked-up fetch job, delivered in person to
        // The Fixer at its destination station. Paid on the spot, under the table — done before any
        // "still-waiting" guard, since the fetch's giver is a Fixer at every station.
        if (giver.Contains("FIXER", StringComparison.OrdinalIgnoreCase) && _dockedHavenId is { } here
            && _quests.FirstOrDefault(q => q is { Kind: QuestKind.Fetch, State: QuestState.PickedUp } && q.DestBodyId == here) is { } drop)
        {
            DeliverFetch(drop);
            return;
        }

        // The cracked-hatch package, handed back to the Fixer at this same station.
        if (giver.Contains("FIXER", StringComparison.OrdinalIgnoreCase) && _dockedHavenId is { } berth
            && _quests.FirstOrDefault(q => q is { Kind: QuestKind.Crack, State: QuestState.PickedUp } && q.DestBodyId == berth) is { } cracked)
        {
            DeliverCrack(cracked);
            return;
        }

        // Quest-status lines. A known face drinking here still gets their own-table card (#355 doorway
        // two), so the captain can stand them a glass while a job's in the air; the status is the blurb.
        Quest? open = _quests.FirstOrDefault(q => q.Giver == giver && q.State != QuestState.TurnedIn);
        if (open is { State: QuestState.Active })
        {
            string line = $"“Still waiting on {open.TargetCallsign}. Finish the job, then we'll talk.”";
            if (OpenPatronTable(giver, line)) { return; }
            ShowPulseMessage(line);
            return;
        }
        if (open is { Kind: QuestKind.Fetch, State: QuestState.PickedUp })
        {
            string line = $"“You've got the goods — don't flash them here. Get them to my associate at {open.TargetCallsign}.”";
            if (OpenPatronTable(giver, line)) { return; }
            ShowPulseMessage(line);
            return;
        }
        if (open is { State: QuestState.Complete })
        {
            string line = $"“{open.TargetCallsign} — done. Collect at any berth; the coin's waiting.”";
            if (OpenPatronTable(giver, line)) { return; }
            ShowPulseMessage(line);
            return;
        }

        // PR-WIRE: a favor called in. If we owe this contact a wired debt and haven't yet been handed
        // the delivery, they slide it across the table now — one quiet delivery, in their own voice.
        if (MakeFavorDeliveryOffer(giver) is { } favorOffer)
        {
            _pendingOffer = favorOffer;
            return;
        }

        // #635 — PROJEKTI KAAMOS's front door. Before this, the longest-prepared arc in the game was
        // invisible until a captain happened to read the whole of one dedication plate among seven. A
        // freight agent holding a docket the board keeps returning is the arc arriving through the system
        // the player already reads (paperwork), and it hands over no shard — only the question. Offered
        // only while the captain has nothing of the arc at all, so it can never elbow a live thread aside.
        if (MakeKaamosBounceOffer(giver) is { } kaamosBounce)
        {
            _pendingOffer = kaamosBounce;
            return;
        }

        // #411 — the far end of the same arc. Once the berth-code has resolved, the ice-moon berth is
        // listed to this hull and a standing consignment has come back onto the board with it. Whoever is
        // drinking here hands it over as the ordinary, absurdly-well-paid haul they believe it to be.
        if (MakeKaamosSupplyRunOffer(giver) is { } kaamosRun)
        {
            _pendingOffer = kaamosRun;
            return;
        }

        Quest? offer = MakeContactOffer(giver);
        if (offer is not null)
        {
            _pendingOffer = offer; // the contract slides across — the card also lets you stand them a glass
            return;
        }

        // No work to hand. A face you KNOW, drinking here, still earns their own-table card so you can
        // buy them one (#355 doorway two); a true stranger with no ledger history just gets the brush-off.
        if (OpenPatronTable(giver))
        {
            return;
        }
        ShowPulseMessage("The stranger swirls their drink. “Nothing worth your time right now. Check back.”");
    }

    // The Magpie, a fence's runner who won't sit still (PR-F). Their position is a pure function of
    // sim time (HavenInterior.MagpieRota); talking is gated on them actually being at the booth you
    // walked up to. So a captain who chatted them at the bar can return a watch later to an empty
    // chair — "they change place and go behind locked doors or move" (owner's ruling, verbatim). Once
    // the Bonded Stores back room is open, that's one of their stops — find them inside.
    private void TalkToMagpie(DeckPlan.ConsoleSpot spot)
    {
        bool backOpen = _dockedHavenId is { } st
            && UnlockedHatchesFor(st).Any(h => HavenInterior.HatchGrowsWing(st, h));
        NpcPost m = HavenInterior.ResolveMagpie(SimTime, backOpen);
        double d = m.Present
            ? Math.Sqrt((spot.X - m.X) * (spot.X - m.X) + (spot.Y - m.Y) * (spot.Y - m.Y))
            : double.MaxValue;
        if (d > DeckPlan.InteractRadius)
        {
            ShowPulseMessage("The Magpie's chair is empty — they've drifted off. Nobody sits still here; try another watch, or look where a door's just opened. 🐦");
            return;
        }

        Quest? job = _quests.FirstOrDefault(q =>
            q.Kind == QuestKind.Crack && q.SourceBodyId == _dockedHavenId
            && _dockedHavenId is { } s && HavenInterior.HatchGrowsWing(s, q.TargetShipId));
        string line = job switch
        {
            { State: QuestState.PickedUp } or { State: QuestState.Complete } or { State: QuestState.TurnedIn }
                => "“Good hands. Get that parcel to the Fixer and we never spoke.”",
            _ when backOpen && m.Location == "BACK ROOM"
                => "“You made it in. The parcel's right there on the shelf — lift it before the dockmaster's rounds.”",
            _ when backOpen
                => "“You're through the hatch. Package is on the back shelf — go on, it won't bite.”",
            { State: QuestState.Active }
                => "“The lockup's the easy part — crack V-06 and there's a parcel with nobody's name on it. I'll be around. Somewhere.”",
            _ => "“Bonded Stores — V-06 — holds a parcel that never made a manifest. The Fixer sets the price; I just know where things are. And I don't linger.”",
        };
        // The Magpie roams, but while they're at this booth and we KNOW them, the table card lets you
        // stand them a glass too (#355 doorway two); their line rides atop it as the blurb. If they're a
        // stranger still, fall back to the plain quip.
        string mgiver = spot.Label.Replace("◈", "").Trim();
        if (OpenPatronTable(mgiver, line)) { return; }
        ShowPulseMessage(line);
    }

    // The standing offer a contact would make you across the table, by who they are — the same switch
    // TalkToStranger runs after its special cases. Extracted (#306) so a trust-unlocked drink opens the
    // same door a walk-up would, one truth for both. Null when they've nothing to hand right now.
    private Quest? MakeContactOffer(string giver) => giver switch
    {
        _ when giver.Contains("COIL", StringComparison.OrdinalIgnoreCase) => MakeCargoRunOffer(giver),
        _ when giver.Contains("GILT", StringComparison.OrdinalIgnoreCase) => MakeIntelOffer(giver),
        _ when giver.Contains("FIXER", StringComparison.OrdinalIgnoreCase) => MakeFetchOffer(giver) ?? MakeFetchCacheOffer(giver) ?? MakeCrackOffer(giver),
        _ => MakeHuntOffer(giver),
    };

    // Pick a live target for a hunt contract — prefer off-books ships (the kind you couldn't just read
    // off the public traffic board, so the stranger's tip is actually worth something). Chosen from
    // sim time + the current berth so the booth's offer is stable frame to frame, and skips ships
    // already under contract.
    private Quest? MakeHuntOffer(string giver)
    {
        List<NpcState> candidates = _npcStates
            .Where(n => n.Active && !n.Arrived && !n.Boarded && !n.Ship.IsPod
                        && _quests.All(q => q.TargetShipId != n.Ship.Id))
            .OrderByDescending(n => !n.Ship.PublishesTimetable)          // off-books first
            .ThenBy(n => n.Ship.Id, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        // The neighbourhood law (owner 2026-07-19): weight the bounty toward a prey whose run stays in
        // the neighbourhood, so the chase a barfly hands you is usually a nearby one — the cross-system
        // hunt is the rare saga the HaulReward chase premium (below) pays for.
        NpcState prey = candidates[WeightedOfferIndex(candidates, n => n.Ship.DestinationId)];
        // #349: a bounty keeps its cargo-weighted floor, plus the HAUL premium for how deep into the
        // system the chase drags you — hunting a runner bound for the outer dark pays for the long chase,
        // not just her hold. Reach is the prey's destination measured from this berth.
        int reward = HaulReward.WithFloor(250 + prey.Ship.CargoUnits * 60,
            HelioRadiusMeters(_dockedHavenId), HelioRadiusMeters(prey.Ship.DestinationId));
        string route = RouteLabel(prey.Ship);
        // The rare cross-system chase reads like the saga it is — the reward already carries the long-chase
        // premium (HaulReward.WithFloor above), so the pitch just names the distance out loud.
        string chaseNote = DestBand(prey.Ship.DestinationId) == MissionBand.CrossSystem
            ? " It's a long chase into the deep — but that's why the purse is fat."
            : "";
        string blurb = prey.Ship.PublishesTimetable
            ? $"“See {prey.Ship.Callsign}, running {route}? She's carrying, and I want her stopped. Bring her down — {reward:N0} cr in it for you.{chaseNote}”"
            : $"“There's a ghost called {prey.Ship.Callsign} running dark, {route}. Won't show on any board. Hole her, board her — {reward:N0} cr, quiet-like.{chaseNote}”";
        return new Quest($"hunt-{++_questSeq}", QuestKind.Hunt, giver,
            prey.Ship.Id, prey.Ship.Callsign, $"Bring down {prey.Ship.Callsign}", blurb, reward);
    }

    // A parcel to carry to another haven — completes when you berth there. Destination is any haven
    // other than the one you're standing in, chosen by sim time + berth so it's stable per booth.
    // #160 · `destOverride` is the milk-run lesson's one entry into this method: the tutorial has already
    // chosen the berth it wants taught (the nearest OTHER clampable berth to the one the captain is standing
    // in) and needs the card, the purse and the pitch to be the ones the board would really have written. It
    // comes through here rather than being built beside it so the lesson can never issue a contract the game
    // does not issue — the #742 lesson, one arc over: a job handed over for a different number is a job that
    // playtests nothing. Null (the ordinary walk-up) keeps the weighted neighbourhood pick untouched.
    private Quest? MakeCargoRunOffer(string giver, CelestialBody? destOverride = null)
    {
        CelestialBody dest;
        if (destOverride is not null)
        {
            dest = destOverride;
        }
        else
        {
            List<CelestialBody> havens = (_ephemeris?.Bodies ?? [])
                .Where(b => b.IsHaven && b.Id != _dockedHavenId
                            && _quests.All(q => q.DestBodyId != b.Id))
                .OrderBy(b => b.Id, StringComparer.Ordinal)
                .ToList();
            if (havens.Count == 0)
            {
                return null;
            }

            // The neighbourhood law (owner 2026-07-19): weight the pick toward nearby systems so most parcels
            // are a local hop, a neighbour planet is the occasional stretch, and a cross-system saga is rare.
            dest = havens[WeightedOfferIndex(havens, b => b.Id)];
        }

        // #349: the purse scales with the actual HAUL — the heliocentric void from where the job is taken
        // (this berth) to the destination — not the old flat 300 that read a station's tiny local orbit and
        // paid the same to Luna as to Neptune. A cross-system parcel now pays like the long trip it is.
        int reward = HaulReward.ForHaul(HelioRadiusMeters(_dockedHavenId), HelioRadiusMeters(dest.Id));
        // #175: a moon haven has no ⚓ dock — you deliver by parking in its orbit — so the pitch names
        // the right last move instead of promising a "berth" that a moon never has.
        string drop = IsDockableHaven(dest) ? "Berth there and it's done." : "Park in orbit there and it's done.";
        // When the rare cross-system saga DOES surface, the pitch acknowledges the haul in voice — the
        // purse (HaulReward) already priced it, so the captain can see the exception is paid for (#357).
        string haulNote = DestBand(dest.Id) == MissionBand.CrossSystem
            ? " It's a long haul out to there — but the purse says so, look for yourself."
            : "";
        // #349: name the destination's ADDRESS (station — PLANET system), so the captain knows what
        // planet the drop is on without zooming every moon.
        string blurb = $"“Quiet parcel, no questions. Gets to {BodyAddress(dest.Id)} in one piece, you walk with {reward:N0} cr. {drop}{haulNote}”";
        return new Quest($"run-{++_questSeq}", QuestKind.CargoRun, giver,
            "", dest.Name, $"Run a parcel to {BodyAddress(dest.Id)}", blurb, reward, DestBodyId: dest.Id);
    }

    // PR-WIRE — the favor called in. When we owe a contact a wired debt (a FavorObligation from a
    // borrow) and don't already have their delivery in hand, they hand us one quiet parcel now, in
    // their voice. Delivering it works the debt off (PayCompletedQuests books the repayment). The
    // destination is a haven other than this one, picked stably by sim time + berth.
    private Quest? MakeFavorDeliveryOffer(string giver)
    {
        FavorObligation? match = null;
        foreach (FavorObligation o in _favorObligations)
        {
            if (string.Equals(o.ContactId, giver, StringComparison.OrdinalIgnoreCase)) { match = o; break; }
        }
        if (match is not { } debt)
        {
            return null;
        }
        if (_quests.Any(q => q.Kind == QuestKind.Favor && string.Equals(q.Giver, giver, StringComparison.OrdinalIgnoreCase) && q.State != QuestState.TurnedIn))
        {
            return null; // already carrying their favor
        }

        List<CelestialBody> havens = (_ephemeris?.Bodies ?? [])
            .Where(b => b.IsHaven && b.Id != _dockedHavenId)
            .OrderBy(b => b.Id, StringComparer.Ordinal)
            .ToList();
        if (havens.Count == 0)
        {
            return null;
        }

        // The neighbourhood law (owner 2026-07-19): a called-in favor still prefers a nearby drop.
        CelestialBody dest = havens[WeightedOfferIndex(havens, b => b.Id)];
        string drop = IsDockableHaven(dest) ? "Berth there and we're square." : "Park in orbit there and we're square.";
        // #349: name the drop's address (station — PLANET system) so the favor points at a place the
        // captain can find. (The purse is the debt principal — a favor clears what you owe, it isn't paid.)
        string blurb = $"{debt.VoiceLine} Get it to {BodyAddress(dest.Id)} in one piece. {drop}";
        return new Quest($"favor-{++_questSeq}", QuestKind.Favor, giver,
            "", dest.Name, $"Quiet delivery for {GiverDisplay(giver)}", blurb, (int)debt.PrincipalCredits, DestBodyId: dest.Id);
    }

    // A whisper on an off-books ghost — the payoff IS the tip. Accepting drops a fresh route-intel
    // entry into the ledger (exactly like a dark-web buy, but on the house), so a ship that never
    // shows on the public board joins your contacts, 🕸-tagged, for 30 days. Instant: no task to do.
    private Quest? MakeIntelOffer(string giver)
    {
        List<NpcState> ghosts = _npcStates
            .Where(n => n.Active && !n.Arrived && !n.Ship.PublishesTimetable
                        && !_intelLedger.Knows(n.Ship.Id, SimTime)
                        && _quests.All(q => q.TargetShipId != n.Ship.Id))
            .OrderBy(n => n.Ship.Id, StringComparer.Ordinal)
            .ToList();
        if (ghosts.Count == 0)
        {
            return null;
        }

        NpcState ghost = ghosts[OfferIndex(ghosts.Count)];
        string blurb = $"“I know where {ghost.Ship.Callsign} really runs — {RouteLabel(ghost.Ship)}. A ghost; she'll never show on any board. This one's on the house. Want it?”";
        return new Quest($"intel-{++_questSeq}", QuestKind.Intel, giver,
            ghost.Ship.Id, ghost.Ship.Callsign, $"Whisper on {ghost.Ship.Callsign}", blurb, 0);
    }

    // The Fixer's one confidential job: fly out to the derelict roadster, prise the untraceable wallet
    // from between the seats, then hand it over in person at another station's bar. A one-off signature
    // hunt — offered only if it isn't already in the ledger (in any state), so the wallet is unique.
    // Destination is an interior station other than this one, picked by sim time + berth so it's stable.
    private Quest? MakeFetchOffer(string giver)
    {
        if (_ephemeris is null || _quests.Any(q => q.Kind == QuestKind.Fetch))
        {
            return null; // no world yet, or there is only one lost roadster
        }
        if (_ephemeris.Bodies.All(b => b.Id != Derelict.RoadsterBodyId))
        {
            return null; // scenario without the wreck
        }
        List<CelestialBody> dests = _ephemeris.Bodies
            .Where(b => b.IsHaven && b.Id != _dockedHavenId && HavenInterior.HasInterior(b.Id))
            .OrderBy(b => b.Id, StringComparer.Ordinal)
            .ToList();
        if (dests.Count == 0)
        {
            return null;
        }

        // The neighbourhood law (owner 2026-07-19): prefer a nearby hand-off so the drop stays local even
        // though the wreck itself is a fixed sunward-of-Mars saga.
        CelestialBody dest = dests[WeightedOfferIndex(dests, b => b.Id)];
        const int reward = 4200; // a dead man's fortune, in a currency nobody can trace
        // #349: name the hand-off's address (station — PLANET system) so the captain knows where the
        // associate waits without hunting every moon.
        string blurb = $"“Word is a dead tycoon's cherry-red roadster is drifting sunward of Mars — shot up as a stunt, never came down. There's a hardware wallet wedged between the seats: a fortune, and untraceable. Fetch it, bring it quiet to my associate at {BodyAddress(dest.Id)}. {reward:N0} cr, and we never spoke.”";
        // #233 · …and what is ACTUALLY between the seats is dealt here, off the same booth seed the hand-off
        // address above was picked with. The brief still says wallet, because the client believes it says
        // wallet — one car in four is wrong about that, and nobody finds out until the seats. See
        // Map.Blackmail for why the answer rides Pin.
        return new Quest($"fetch-{++_questSeq}", QuestKind.Fetch, giver,
            "", dest.Name, "Fetch the roadster's lost wallet", blurb, reward,
            DestBodyId: dest.Id, SourceBodyId: Derelict.RoadsterBodyId, Pin: WhatIsBetweenTheSeats());
    }

    // #223: the Fixer's cache run — a map to SOMEONE ELSE'S buried hoard. The recovery flow and the
    // mission flow are one code path: accept learns the cache (so the shuttle-bay 🗺 Dig appears at the
    // body), digging it up sets PickedUp, and carrying the chest to the Fixer's bar pays out. Offered
    // only when a landable moon carries a named landmark to pace off (the monolith is the storied one).
    private Quest? MakeFetchCacheOffer(string giver)
    {
        if (_ephemeris is null || _dockedHavenId is not { } here)
        {
            return null;
        }
        if (_quests.Any(q => q.Kind == QuestKind.FetchCache && q.State != QuestState.TurnedIn))
        {
            return null; // one cache run at a time
        }
        // Only send them to a storied moon they can actually reach from this bar — one sharing the
        // station's planet, so it's in the same neighbourhood the shuttle can cross.
        string? planetId = BodyById(here)?.ParentId;
        CelestialBody? dig = _ephemeris.Bodies
            .Where(b => b.Kind == BodyKind.Moon && Landmarks.HasNamedSite(b.Id) && b.ParentId == planetId)
            .OrderBy(b => b.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (dig is null)
        {
            return null; // no storied landing site within reach of this bar
        }
        string barName = BodyName(here);
        const int reward = 3200;
        string drawKey = $"{here}|fetchcache|{(int)(SimTime / 86400)}";
        RumorMaps.Rumor rumor = RumorMaps.Generate(drawKey, dig.Id);
        // #349: name the dig moon's address (moon — PLANET system) so the captain knows which sky to fly to.
        string blurb = $"“A client wants a chest lifted — {rumor.Cache.Owner} buried it out on {BodyAddress(dig.Id)} and won't be collecting. Here's the map: {rumor.Cache.BearingLine}. Dig it up, bring the lot to me here at {barName}. {reward:N0} cr, clean.”";
        // TargetShipId carries the (deterministic) cache id so the dig can match it; Pin carries the draw
        // key so AcceptOffer re-mints and LEARNS the exact same cache.
        return new Quest($"fetchcache-{++_questSeq}", QuestKind.FetchCache, giver,
            rumor.Cache.Id, barName, "Lift a buried chest", blurb, reward,
            DestBodyId: here, SourceBodyId: dig.Id, Pin: drawKey);
    }

    // The Fixer's other line of work, when the roadster job is spoken for: crack a locked hatch here at
    // this station. Picks one of the deck's locked departments deterministically, quotes its real access
    // code, and pays on hand-over — a quick, self-contained job with no flying (contrast the fetch).
    private Quest? MakeCrackOffer(string giver)
    {
        if (_ephemeris is null || _dockedHavenId is not { } here)
        {
            return null;
        }
        if (_quests.Any(q => q.Kind == QuestKind.Crack && q.State != QuestState.TurnedIn))
        {
            return null; // one break-in at a time
        }
        List<DeckPlan.ConsoleSpot> locked = _deckPlan.Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.Hatch && c.Label.Contains("🔒", StringComparison.Ordinal))
            .OrderBy(c => c.Label, StringComparer.Ordinal)
            .ToList();
        if (locked.Count == 0)
        {
            return null;
        }

        // Prefer a hatch that GROWS A ROOM here (PR-F) — so the natural bar flow at a station with a
        // wing (Cinder Roost's Bonded Stores) hands out the world-growing job — else the usual rotation.
        int wingIdx = locked.FindIndex(c => HavenInterior.HatchGrowsWing(here, HatchId(c.Label)));
        DeckPlan.ConsoleSpot target = wingIdx >= 0 ? locked[wingIdx] : locked[OfferIndex(locked.Count)];
        string id = HatchId(target.Label);
        string dept = HatchDept(target.Label);
        string pin = MakePin(id);
        const int reward = 2600;
        string blurb = $"“That hatch — {id}, the {dept.ToLowerInvariant()} lockup. There's a package behind it that isn't on any manifest. Code's {pin} — I never told you that. Crack it, bring it straight back here, and it stays between us. {reward:N0} cr.”";
        return new Quest($"crack-{++_questSeq}", QuestKind.Crack, giver, id, $"the {dept.ToLowerInvariant()} package",
            $"Crack hatch {id}", blurb, reward, DestBodyId: here, SourceBodyId: here, Pin: pin);
    }

    // Deterministic pick index for a booth's offer: sim time (slow rotation) mixed with the berth id
    // (a stable char-sum, not the randomized string hash), so different docks surface different work
    // and it doesn't flicker frame to frame.
    private int OfferIndex(int count) =>
        count <= 0 ? 0 : (int)(((long)(SimTime / 1000) + (_dockedHavenId ?? "").Sum(ch => ch)) % count);

    // ── The neighbourhood law (owner 2026-07-19, Sunday-morning-wind §6): "adjust the missions to prefer
    // staying in relatively nearby places. Having 10 year flights should be an exception in mid mission,
    // not anything casual :-D". The flat OfferIndex above treats Luna and Neptune alike; these pick a
    // destination WEIGHTED by MissionRange bands — ~70% local system, ~25% a neighbour planet, ~5% a
    // cross-system saga — so most work stays close and the long haul is the rare, HaulReward-priced
    // exception (#357). Same booth-stable, per-berth, slowly-rotating seed idiom as OfferIndex, but folded
    // through the one DiceRule so the weighted roll is deterministic per booth (same seed → same mix).

    // A weighted pick over a candidate set, keyed on each candidate's destination body id. Classifies
    // every candidate into its MissionRange band, then rolls MissionRange.PickIndex on the booth seed.
    private int WeightedOfferIndex<T>(IReadOnlyList<T> candidates, Func<T, string?> destBodyOf)
    {
        if (candidates.Count <= 1)
        {
            return 0;
        }

        IReadOnlyList<double> planetRadii = PlanetHelioRadii();
        string originSystem = SystemIdOf(_dockedHavenId);
        double originRadius = HelioRadiusMeters(_dockedHavenId);
        var bands = new MissionBand[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            string? destId = destBodyOf(candidates[i]);
            bands[i] = MissionRange.Classify(
                originSystem, SystemIdOf(destId),
                originRadius, HelioRadiusMeters(destId), planetRadii);
        }

        return MissionRange.PickIndex(MissionRangeSeed(), bands);
    }

    // The band a chosen destination fell into — for the offer copy that acknowledges a cross-system saga.
    private MissionBand DestBand(string? destBodyId) => MissionRange.Classify(
        SystemIdOf(_dockedHavenId), SystemIdOf(destBodyId),
        HelioRadiusMeters(_dockedHavenId), HelioRadiusMeters(destBodyId), PlanetHelioRadii());

    // The seed for a booth's weighted destination roll — the OfferIndex stability idiom (slow sim-time
    // rotation + a per-berth salt) folded through the one DiceRule, so the mix is deterministic per booth
    // and doesn't flicker frame to frame.
    private ulong MissionRangeSeed() =>
        DiceRule.Seed("mission-range", (long)(SimTime / 1000), (_dockedHavenId ?? "").Sum(ch => ch));

    // A body's SYSTEM id — its planet-level ancestor (the sun-orbiting planet it rides around), the same
    // ancestor HelioRadiusMeters/BodyAddress read. Null/unknown collapses to the raw id (or "").
    private string SystemIdOf(string? bodyId) =>
        PlanetLevelAncestor(BodyById(bodyId))?.Id ?? bodyId ?? "";

    // Every planet's heliocentric orbit radius in this scenario — the ranking set MissionRange orders the
    // systems by. A "planet" here is a sun-orbiting body of planet kind (a heliocentric station like the
    // derelict roadster is excluded from the ranks, but still ranks cleanly by where its radius sits).
    private IReadOnlyList<double> PlanetHelioRadii() =>
        (_ephemeris?.Bodies ?? [])
            .Where(b => b.Kind == BodyKind.Planet && b.ParentId is { } pid && BodyById(pid)?.ParentId is null)
            .Select(b => b.OrbitRadius)
            .ToList();
}
