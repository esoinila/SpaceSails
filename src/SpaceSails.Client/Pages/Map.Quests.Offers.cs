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

        // #1202 slice 4 · …while she is away after a SPIKED window, the regular beside her empty chair answers the
        // one question it raises, once; and her first sitting after the absence is her one line, before any card.
        if (TheEmptySeatIsAskedAbout(giver) || HerReturnIsTold(giver))
        {
            return;
        }

        // #1202 · Rauha Lind at her own chair: her card, before any give-work path a regular's table runs (a
        // favour called in, a KAAMOS docket) — she books passage and nothing else.
        if (string.Equals(giver, CarryThePress.Giver, StringComparison.Ordinal) && MakePressOffer() is { } passage)
        {
            _pendingOffer = passage;
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
}
