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

/// <summary>
/// #306 · THE DRINK AS A TWO-EDGED TRUST MANEUVER. Owner ruling 2026-07-18: <i>"having a drink at a bar
/// with somebody is a sign of trust and should open up new business opportunities, or give access to
/// information. Of course we might slip information… Keeping two realities in one's mind at the same time
/// [is] a lot."</i>
///
/// <para>When a KNOWN contact is drinking in this room the bar menu grows a "buy &lt;name&gt; a drink"
/// row — a stronger trust play than a round for the house. The salted 2D6, rolled on the ONE shared
/// <c>DiceRule</c>, decides which edge cuts. Refusing their glass has a price too, and backing out of your
/// OWN offer has none: punishing somebody for reconsidering their own idea is theatre, not a decision.</para>
///
/// <para>Split out of <c>Map.Quests.Bar.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // --- #306 The drink as a two-edged trust maneuver -------------------------------------------------
    // Owner ruling (2026-07-18): "having a drink at a bar with somebody is a sign of trust and should
    // open up new business opportunities, or give access to information. Of course we might slip
    // information… Keeping two realities in one's mind at the same time [is] a lot." So when a KNOWN
    // contact (ContactLedger history) is drinking in this room, the bar menu grows a "buy <name> a
    // drink" row: a stronger trust play than a round for the house. The salted-2D6 (ContactDrink,
    // rolled on the ONE shared DiceRule) decides which edge cuts — they open up to you, or you slip a
    // tell to them. Refusing their glass has a price too. The whole thing round-trips through the Vault.

    // The known contacts actually drinking here right now — a BarPatron console whose giver we have
    // ContactLedger history with (a job done, coin in the air, a round stood, a tell already slipped).
    // Empty when the room holds only strangers, so the drink rows never show without a real
    // relationship to deepen. Mirrors the BuyRoundForRoom scan (incl. the roaming Magpie's rota gate).
    private IReadOnlyList<(string Giver, string Display)> PresentBarContacts()
    {
        if (!_deckMode || CurrentKeep is null)
        {
            return []; // #355: keyed to the docked bar's keep, not the counter card — the table card reads it too
        }
        bool backOpen = _dockedHavenId is { } st
            && UnlockedHatchesFor(st).Any(h => HavenInterior.HatchGrowsWing(st, h));
        var found = new List<(string, string)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DeckPlan.ConsoleSpot c in _deckPlan.Consoles)
        {
            if (c.Kind != DeckPlan.ConsoleKind.BarPatron)
            {
                continue;
            }
            string giver = c.Label.Replace("◈", "").Trim();
            if (OracleRant.IsOracle(c.Label))
            {
                continue; // #425: the oracle is not a ledger contact — no drink-contact row for her
            }
            if (!seen.Add(giver))
            {
                continue; // one contact can hold two consoles (the roaming Magpie) — list them once
            }
            if (giver.Contains("MAGPIE", StringComparison.OrdinalIgnoreCase)
                && !HavenInterior.ResolveMagpie(SimTime, backOpen).Present)
            {
                continue; // the Magpie only drinks with the room when their rota has them in it
            }
            if (!_contacts.For(giver).HasHistory)
            {
                continue; // #306: only KNOWN contacts — a stranger has no relationship to deepen yet
            }
            found.Add((giver, GiverDisplay(giver)));
        }

        // #973 L5a · …AND THE PEOPLE WHO KNEW THE FACE. An old shipmate posted at this berth is in the room
        // whether or not the deck plan drew them a console: they are at a claims desk, a registrar's, a
        // customs post, a clinic counter or behind these very taps, and the captain came here to see them.
        // They join the same list the fixers do, so the drink, the offer moment and the two doorways are the
        // shipped ones rather than a second flow beside them.
        foreach (Core.OldCrew.Seeded s in OldCrewHere)
        {
            string id = Core.OldCrew.LedgerId(s.Id);
            if (seen.Add(id))
            {
                found.Add((id, GiverDisplay(id)));
            }
        }

        return found;
    }

    // Offer a known contact drinking here a glass — OFFER FIRST (#347, owner playtest 2026-07-18: "The
    // person may refuse the drink here. That possibility should be determined first… If we just buy it
    // then we don't know what they would have and if they accept it"). So before a single credit moves,
    // the contact decides — deterministically from seed (ContactDrink.OfferDrink) — whether to take the
    // glass. A refusal costs nothing but the ask. Only an ACCEPTED glass is poured, and only then does the
    // shared-drink salted-2D6 (#306) decide the edge: they open up (concrete intel, or business once trust
    // runs deep), or YOU slip a tell that lands on their book. You drink too — the shared glass is sanity
    // relief and rides the one wobble law via PourRum. Both rolls are shown (#306 item 5; the shared dice
    // tray is TODO(#305)). All state moves through the Vault (RequestVaultSave).
    private void BuyContactDrink(string giver, bool offeringUsual = false)
    {
        if (CurrentKeep is not { } keep)
        {
            return;
        }
        _pendingContactDrink = null; // the offer moment resolves into the ask
        if (!PresentBarContacts().Any(c => c.Giver.Equals(giver, StringComparison.OrdinalIgnoreCase)))
        {
            return; // not present, or not a known contact, just now — no effect
        }

        string display = GiverDisplay(giver);
        if (_credits < keep.DrinkPrice)
        {
            _barNotice = $"“{keep.DrinkName}'s {keep.DrinkPrice} cr — you're a little short to stand {display} one.”";
            ShowPulseMessage(_barNotice);
            return;
        }

        // #5 SundayMorningWind — THE CHOICE IS THE TELL. When we offer generically, the contact reaches
        // for a pour off THIS bar's menu (usually their favourite); when we specifically stand them their
        // usual (an option that only shows once we KNOW it and the bar pours it), we hand them that glass.
        // What lands in their hand colours what they let slip (DrinkTell.ChannelFor).
        IReadOnlyList<Core.Drink> menu = Core.DrinkMenu.For(keep);
        Core.Drink favorite = Core.DrinkFavorites.FavoriteFor(giver);
        bool favoriteOnMenu = menu.Any(d => d.Id == favorite.Id);
        bool offeringFavorite = offeringUsual && favoriteOnMenu;
        Core.Drink chosen = offeringFavorite ? favorite : Core.DrinkChoice.ChoosesDrink(giver, menu);

        int goodwillBefore = _contacts.For(giver).Goodwill;
        bool holdingSecret = _heat.Level > 0 || HotHoldUnits() > 0; // the second reality to keep steady

        // OFFER FIRST: the contact may wave the glass off before anything is bought. A warm contact takes
        // it gladly; a wary one (you're running heat / hot cargo) may pass. Standing them their usual is a
        // small honest edge (+1 "their usual"). Nothing debited on a refusal.
        ulong offerSeed = DiceRule.Seed($"drink-offer:{giver}", (long)SimTime);
        // #973 L5a · the three named modifiers, read off the room the captain is actually standing in and
        // handed to BOTH rolls, so the offer and the glass can never disagree about who else is here.
        ContactDrink.TheRoom room = TheRoomFor(giver);
        DrinkOfferResult offered = ContactDrink.OfferDrink(
            offerSeed, goodwillBefore, holdingSecret, offeringFavorite, room);
        if (!offered.Accepted)
        {
            _barNotice = $"🚫 {RefusalLine(display, holdingSecret)}  🎲 {offered.Describe()}";
            ShowPulseMessage(_barNotice); // no coin moved, no goodwill booked — the glass never left the bar
            return;
        }

        _credits -= keep.DrinkPrice;

        // The contact's choice reveals their taste — we LEARN their favourite the first time we watch them
        // reach for it (progress the owner wants a drink to give). Recorded on the saved ledger, so an
        // "offer their usual" edge is available next time. The favourite they'd truly reach for is the tell,
        // even if this bar can't pour it — you now know what to bring.
        bool firstLearn = !_contacts.For(giver).FavoriteKnown;
        _contacts.RecordKnownFavorite(giver, giver, favorite.Id);

        ulong seed = DiceRule.Seed($"drink:{giver}", (long)SimTime);
        DrinkParley parley = ContactDrink.Roll(seed, goodwillBefore, holdingSecret, offeringFavorite, room);

        // …and the man who signed writes the meeting down. Once per visit, owed to this port's authority and
        // to nobody else's book (#715), whoever the glass was actually for.
        TheSignerReports();

        _contacts.AddGoodwill(giver, giver, parley.GoodwillDelta);

        // SANITY-RELIEF SEAM (#226), WIRED: a shared drink is the real medicine — conversation AND the
        // glass. NerveModel restores it at ANY nerve level (owner's ruling), the whole point of company
        // over a lone drink. Still rides the one wobble/tot law via PourRum.
        PourRum($"{chosen.Name} with {display} — {chosen.Flavor}", NerveModel.DrinkKind.SharedWithContact);

        // The little channel of info: the pour the contact chose decides WHICH kind of tell opens.
        Core.TellChannel channel = Core.DrinkTell.ChannelFor(chosen);
        string learn = firstLearn
            ? $" You know what {display} drinks now — the {favorite.Name}."
            : string.Empty;
        string chose = $"{display} takes the {chosen.Name}.";

        string line;
        switch (parley.Outcome)
        {
            case DrinkOutcome.Slip:
                string tell = SlipTell();
                _contacts.RecordKnownTell(giver, giver, tell);
                // Priced through the ledger today (the honest minimum — the contact now KNOWS this).
                // The heat / false-colors / contract seams can later read KnownTells to make a leaked
                // hot-cargo or heat tell actually bite (#306 kin: heat/contract consequence systems).
                line = $"🍷 {chose} The glass loosened YOUR guard — they clocked {tell}. {display} files it away behind a smile.{learn}";
                break;

            case DrinkOutcome.OpensUp:
                // #973 L5a · with an old shipmate a good roll is not only intel: they slip a SHEET into the
                // book — a held memory marked theirs and tagged by what they were to the captain.
                SlipASheet(giver, display);
                // LeadFor already names the drink they took, so no separate "takes the …" here.
                line = $"🍷 {Core.DrinkTell.LeadFor(chosen, display)} {OpenIntelLine(giver, channel)}{learn}";
                Overhear(line, giver); // durable — intel you paid for doesn't auto-vanish (#212, owner)
                break;

            case DrinkOutcome.BusinessUnlock:
                SlipASheet(giver, display);
                Quest? offer = MakeContactOffer(giver);
                if (offer is not null)
                {
                    CloseBarkeep();
                    ClosePatronTable();  // the drink's door swings the contract card up in place of the table card
                    _pendingOffer = offer; // set AFTER the closers, which never touch _pendingOffer, so the card shows
                    ShowPulseMessage($"🍷 {chose} A drink with {display} opens a door (🎲 {parley.Describe()}). They slide a proposition across the table.{learn} (−{keep.DrinkPrice:N0} cr)");
                    RequestVaultSave();
                    return;
                }
                line = $"🍷 {chose} {display} trusts you now — but has no work to hand just yet. “Next time, friend.”{learn}";
                break;

            default: // Warm
                line = $"🍷 A good glass with {display} — they took the {chosen.Name}. Nothing said that matters, but the ice is thinner between you now.{learn}";
                break;
        }

        _barNotice = $"{line}  🎲 {parley.Describe()}";
        ShowPulseMessage($"{_barNotice} (−{keep.DrinkPrice:N0} cr)");
        RequestVaultSave(); // #225: the purse moved, goodwill/tells/favourite were booked
    }

    // Open the "offer <name> a drink" OFFER MOMENT — a small confirm (offer it / cancel) on the card.
    // Owner ruling 2026-07-18 ("what decision does the wave off represent?"): extending the offer is the
    // captain's OWN idea, so it opens a moment you can back out of freely — there is no standing wave-off.
    // Confirming (BuyContactDrink) is where the CONTACT then decides accept/refuse (#347).
    private void OfferContactDrink(string giver)
    {
        if (CurrentKeep is null
            || !PresentBarContacts().Any(c => c.Giver.Equals(giver, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }
        _pendingContactDrink = giver;
    }

    // #355 doorway two — open a bar patron's OWN-TABLE drink card. Same offer flow the counter card hosts,
    // but keyed to the one contact you're sitting with. Returns false for a stranger with no ledger history
    // (or no keep on this berth), so callers fall back to the plain quip — you cannot deepen a bond that
    // isn't there yet, exactly as the counter card's PresentBarContacts gate already decides.
    private bool OpenPatronTable(string giver, string? blurb = null)
    {
        if (CurrentKeep is null
            || !PresentBarContacts().Any(c => c.Giver.Equals(giver, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }
        // One doorway open at a time: the counter card and the table card are two faces of the same offer
        // flow, so opening the table shuts the counter (the BusinessUnlock path sets the same precedent).
        if (_barMenu is not null)
        {
            CloseBarkeep();
        }
        CloseOracleFromBar(); // #425: only one deck card open at a time — a patron's table shuts the oracle's corner
        _patronDrink = giver;
        _patronDrinkBlurb = blurb;
        return true;
    }

    // Close the patron's table card. Leaves _pendingOffer untouched (a contract may be opening in its
    // place) but clears any half-open offer moment, which does not survive stepping away from the table.
    private void ClosePatronTable()
    {
        _patronDrink = null;
        _patronDrinkBlurb = null;
        _pendingContactDrink = null;
    }

    // Back out of your OWN offer — a plain CANCEL. No debit, no "unwet glass" line: punishing someone for
    // reconsidering their own idea is theater, not a decision (owner ruling 2026-07-18).
    private void CancelContactDrinkOffer() => _pendingContactDrink = null;

    // The line a contact says when they wave off the offered glass (#347). Deterministic flavor keyed to
    // sim time; a wary read (you're running heat / hot cargo) gets its own cooler tone. No goodwill moves —
    // a refused offer is information, not an insult, and the captain paid nothing for it.
    private string RefusalLine(string display, bool holdingSecret)
    {
        if (holdingSecret)
        {
            string[] wary =
            [
                $"{display} looks at your jumpy hands and slides the glass back. “Not from you, not tonight.”",
                $"{display} reads something off you and passes. “Buy me one when you're travelling lighter.”",
            ];
            return wary[(int)((SimTime / 60) % wary.Length)];
        }
        string[] plain =
        [
            $"{display} lifts a hand — “I'm alright, friend. Maybe next round.”",
            $"{display} shakes their head, easy about it. “Not just now. Thanks all the same.”",
            $"{display} waves the glass off with a tired smile. “Another time.”",
        ];
        return plain[(int)((SimTime / 60) % plain.Length)];
    }

    // NAMED SEAM (#226/#306, owner 2026-07-18) — NOT WIRED. The −2 "unwet glass" debit belongs to a
    // future NPC-INITIATED offer: when a CONTACT buys/invites the captain to drink and the captain
    // declines, THAT refusal (a social expectation pointing AT the captain) reads as suspicion and costs
    // goodwill. Today no such NPC-initiated flow exists, so this is deliberately unreferenced — the home
    // for ContactDrink.RefusalDebit when that flow is built. Do not wire it to a standing menu button:
    // you cannot decline an offer nobody made.
    private void DeclineNpcInitiatedDrink(string giver)
    {
        if (_barMenu is null
            || !PresentBarContacts().Any(c => c.Giver.Equals(giver, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }
        _contacts.AddGoodwill(giver, giver, -ContactDrink.RefusalDebit);
        string display = GiverDisplay(giver);
        _barNotice = $"✋ You wave off their round. {display} studies your unwet glass, and something cools between you.";
        ShowPulseMessage(_barNotice);
        RequestVaultSave(); // #225: goodwill moved
    }
}
