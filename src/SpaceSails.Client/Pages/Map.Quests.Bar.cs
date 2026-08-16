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

// Subject: part of Map.Quests — the bar ashore: the counter and its other doorway, the drinks, a round for the room, a glass for a contact, and what gets said over it.
public partial class Map
{

    // --- #247 The barkeep: buying a drink ashore ---------------------------------------------------
    // Owner ashore at the Rusty Roadstead: "How do I get a drink at the Rusty bar here? Did we forget
    // to add the bar-keep :-D". Drinking already lived aboard (the Galley 'Pour a tot'); this is the
    // same beat ashore. The barkeep card is opened by pressing E at the counter; the per-bar house
    // special, name and rumors are pure Core data (Barkeeps). Same drunkenness law both places — a
    // poured drink routes through the very PourRum the Galley calls (one tot count, one wobble).
    private Core.Interior.Barkeep? _barMenu;   // the open barkeep card (null = shut)
    private string? _barNotice;                 // the last thing the keep said, shown on the card
    private bool _showBarMenu;                   // #4: the full drinks menu (with Larry flavour) is open on the card

    // #355 doorway two — the keep of the bar we're docked at, resolved the SAME way the counter card is
    // (Barkeeps.For the berth). The offer-a-drink flow leans on this instead of _barMenu, so it works
    // when opened at a patron's own table (counter shut) as well as from the counter itself.
    private Core.Interior.Barkeep? CurrentKeep =>
        _dockedHavenId is { } id ? Barkeeps.For(id) : null;

    private string? _patronDrink;       // the bar patron whose OWN-TABLE drink card is open (null = shut)
    private string? _patronDrinkBlurb;  // an optional line the patron just said, shown atop that table card

    private void ToggleBarMenu() => _showBarMenu = !_showBarMenu;

    // ── The bar VISIT (owner 2026-07-18): a round satisfies the room for THIS stay, and loosens tongues
    // once. Kept as light session state keyed to the docked bar — no new persistence (the coordinator's
    // "trivially cheap through existing session state"). A different berth (or undock → _dockedHavenId
    // clears) starts a fresh visit; re-docking the SAME bar in one session keeps the visit, which is fine.
    private string? _barVisitStation;      // which docked station this visit's social state belongs to
    private bool _roundThisVisit;          // a round for the room has been stood this visit
    private string? _pendingContactDrink;  // the giver whose "pour it / cancel" offer moment is open

    // #308/#283 → owner 2026-07-18 ("may not hide"; "autodisappears which is not convenient"): every bar
    // tip/rumor is written to a DURABLE, revisitable book that rides the vault, not lived-and-lost in a
    // toast. The transient line is just the doorbell; this is the record.
    private List<Core.OverheardLine> _overheard = [];

    // Fold this bar visit's state to the current berth: a new (or no) berth wipes the "round stood" and
    // any half-open offer moment, so satisfied/loosened state never leaks across visits.
    private void EnsureBarVisit()
    {
        if (_barVisitStation != _dockedHavenId)
        {
            _barVisitStation = _dockedHavenId;
            _roundThisVisit = false;
            _pendingContactDrink = null;
            _patronDrink = null;
            _patronDrinkBlurb = null;
        }
    }

    private void TalkToBarkeep()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.Barkeep })
        {
            return;
        }
        if (_dockedHavenId is not { } id || Barkeeps.For(id) is not { } keep)
        {
            ShowPulseMessage("The bar's unattended just now — nobody behind the counter.");
            return;
        }
        EnsureBarVisit();
        if (_patronDrink is not null)
        {
            ClosePatronTable(); // the counter and a patron's table are one flow, two doorways — never both open
        }
        CloseOracleFromBar(); // #425: the counter and the oracle's corner are the same one-card-at-a-time family
        _barMenu = keep;
        _barNotice = keep.Greeting;
    }

    // ── #756 · THE OTHER DOORWAY ONTO THE SAME COUNTER ──────────────────────────────────────────────────
    //
    // Owner, live playtest, standing at the B1 cantina hall's counter: "HOW DO I ORDER A DRINK FROM THE
    // BAR?????? I walk to the bar to buy a drink... not possible... WHY?" — and on the fix: "just copy the
    // stuff from the space bars as needed" / "we could copy a lot of those from say the Tilt bar at Uranus."
    //
    // SO NOTHING IS COPIED. This is TalkToBarkeep's sibling and not its twin: the six lines below are the
    // whole of the difference between a spaceport bar and a counter 150 m under a rock, because everything
    // after them — the card, the menu, the buy, the debit, the receipt, the Esc, the #736 outcome slot —
    // is the same machine holding the same state (_barMenu, _barNotice). A second implementation of "order
    // a drink" would have been one more thing in this repo that worked twice and told the truth once.
    //
    // WHICH counter is asked of Core (CounterService.For); this decides nothing about the venue.
    //
    // AND IT OPENS ON THE MENU. The bug being fixed is a captain who could not see anything to order. A
    // counter that opens closed and asks you to find "See the menu" first is that same bug wearing a card.
    private void OpenCounterService(Core.Interior.Barkeep counter)
    {
        if (_patronDrink is not null)
        {
            ClosePatronTable(); // one card at a time — the doorway family the haven counter already joins
        }
        CloseOracleFromBar();
        _barMenu = counter;
        _barNotice = counter.Greeting;
        _showBarMenu = true;
    }

    private void CloseBarkeep()
    {
        _barMenu = null;
        _barNotice = null;
        _showBarMenu = false;
        _pendingContactDrink = null; // a half-open offer moment does not survive stepping back from the bar
        LeaveTheStoolBehind();       // #756 · and you are not still on a stool at a counter you walked away from
    }

    // The oracle's corner card is one of the same mutually-exclusive doorway family — opening the counter or a
    // patron's table shuts her card so two cards never stack. (#425)
    private void CloseOracleFromBar() => _oracleOpen = false;

    // ── "Who's in tonight" — the empty chair gets a sentence (issue #410, story pass 2026-08-02) ───────
    //
    // #410's rota shipped complete: each regular is present at a port only sometimes, in a seeded seat, and
    // an away one is Gone or InTheBack. But an away regular gets NO CONSOLE — so the player walks up to an
    // empty chair, presses E, and NOTHING HAPPENS. Three-quarters of the roster could be out and the room
    // would never say so; PatronState.Gone vs InTheBack was computed every watch and told to nobody. That
    // is criterion 1 — "a truth that lives only in Core is not being told" — and the fix is a sentence, in
    // the one voice that would actually know: the barkeep's.
    //
    // Read at _dockVisitSimTime, the SAME frozen watch the deck was welded at (Map.Deck), NOT the live
    // clock. Reading SimTime here would let the line drift out of step with the chairs mid-dock — the sim
    // saying one thing and the sentence another, which is this repo's most common bug by measure.
    private string? WhoIsInTonight()
    {
        if (_dockedHavenId is not { } id)
        {
            return null;
        }

        var seatedHere = new List<string>();
        var steppedOut = new List<string>();
        var inTheBack = new List<string>();
        foreach (HavenInterior.SeatedRegular r in HavenInterior.ResolveRegulars(id, _dockVisitSimTime))
        {
            switch (r.State)
            {
                case PatronState.AtBar: seatedHere.Add(r.ShortName); break;
                case PatronState.InTheBack: inTheBack.Add(r.ShortName); break;
                default: steppedOut.Add(r.ShortName); break;
            }
        }

        var said = new List<string>();
        if (seatedHere.Count > 0)
        {
            said.Add($"{Names(seatedHere)} {(seatedHere.Count == 1 ? "is" : "are")} in tonight.");
        }
        else
        {
            said.Add("Quiet house tonight — none of the usual faces.");
        }
        if (steppedOut.Count > 0)
        {
            said.Add($"{Names(steppedOut)} stepped out.");
        }
        if (inTheBack.Count > 0)
        {
            said.Add($"{Names(inTheBack)} {(inTheBack.Count == 1 ? "is" : "are")} somewhere in the back.");
        }
        return string.Join(" ", said);

        static string Names(IReadOnlyList<string> who) => who.Count switch
        {
            1 => who[0],
            2 => $"{who[0]} and {who[1]}",
            _ => $"{string.Join(", ", who.Take(who.Count - 1))} and {who[^1]}",
        };
    }

    // Append a heard line to the durable "overheard at the bar" book, capped, and persist it. The receipt
    // (#119 idiom) so the words the captain paid for are revisitable, not gone with the toast.
    private void Overhear(string text, string source)
    {
        string bar = _barMenu?.BarName ?? (_dockedHavenId is { } id ? Barkeeps.For(id)?.BarName : null) ?? "THE BAR";
        _overheard = [.. Core.OverheardLog.Append(_overheard, new Core.OverheardLine(text, SimTime, source, bar))];
        RequestVaultSave(); // #225: the book grew
    }

    // The recent lines overheard in THIS bar, newest first — the card's revisitable "overheard here"
    // strip, so a tip you paid a round to hear is still readable when you lean back on the counter.
    private IReadOnlyList<Core.OverheardLine> OverheardHere(int max)
    {
        string? bar = _barMenu?.BarName;
        if (bar is null)
        {
            return [];
        }
        var here = new List<Core.OverheardLine>();
        for (int i = _overheard.Count - 1; i >= 0 && here.Count < max; i--)
        {
            if (string.Equals(_overheard[i].BarName, bar, StringComparison.OrdinalIgnoreCase))
            {
                here.Add(_overheard[i]);
            }
        }
        return here;
    }

    // Buy the house special: debit the purse, then apply the SAME drunkenness the Galley tot does — the
    // drink rides through PourRum, so a third round ashore makes the deck just as tilty as one aboard.
    // A #119-style receipt names the drink and the spend (the repo loves receipts).
    private void BuyHouseSpecial()
    {
        if (_barMenu is not { } keep)
        {
            return;
        }
        Core.Interior.BarTab tab = keep.PourHouseSpecial(_credits);
        if (!tab.Poured)
        {
            _barNotice = tab.Line;
            ShowPulseMessage(tab.Line);
            return;
        }
        _credits = tab.RemainingCredits;
        // A lone drink at the counter — weak medicine (NerveModel), steadier the higher your nerve already
        // is, and just one point at the shot floor. The receipt carries the steadying note PourRum builds.
        string receipt = PourRum($"{keep.DrinkName} — {keep.DrinkFlavor}", NerveModel.DrinkKind.BarSpecial, withExcuse: true);
        _barNotice = receipt;
        ShowPulseMessage($"{receipt} (−{tab.Cost:N0} cr)");
        RequestVaultSave(); // #225: the purse moved (and PourRum saved the nerve)
    }

    // #4 SundayMorningWind — the menu now pours more than one type. Buy any drink on THIS bar's menu for
    // yourself (the Larry-coloured staples + the house special), all at the bar's going rate. Same one
    // wobble/tot law via PourRum, same #119 receipt naming the pour and the spend.
    private void BuyDrink(Core.Drink drink)
    {
        if (_barMenu is not { } keep)
        {
            return;
        }
        // #756 · The item's OWN price, asked of Core. A card with a 2 cr coffee and a 12 cr double on it
        // cannot be charged at one flat rate, and the button's label, the button's enabled-ness and this
        // debit all read the same PriceAt so they can never come to three different answers.
        int cost = drink.PriceAt(keep.DrinkPrice);
        if (_credits < cost)
        {
            _barNotice = keep.SelfService
                ? $"The reader declines: {drink.Name} is {cost} cr and the purse is short."
                : $"“{drink.Name}'s {cost} cr — come back when the purse can cover it, spacer.”";
            ShowPulseMessage(_barNotice);
            return;
        }
        _credits -= cost;
        // #756 · FOOD IS NOT A POUR. Owner, at the counter: "you don't have drink or food… what kind of bar
        // is that." A tray is bought at the same counter with the same coin as a glass, and the tot law is
        // exactly where the two part company — a fry-up does not tilt the deck, so it must not route
        // through the ONE wobble law the Galley and every bar ashore share.
        string receipt = drink.Category == Core.DrinkCategory.Food
            ? $"🍽 {drink.Name} — {cost} cr. {drink.Flavor}"
            : PourRum($"{drink.Name} — {drink.Flavor}", NerveModel.DrinkKind.BarSpecial, withExcuse: true);
        _barNotice = receipt;
        ShowPulseMessage($"{receipt} (−{cost:N0} cr)");
        RequestVaultSave(); // #225: the purse moved (and PourRum saved the nerve)
    }

    // The favourite drink we've LEARNED for a contact (#5), or null if we've never watched them choose.
    // The card shows it on a known contact and offers to stand them "their usual" for the +1 edge.
    private Core.Drink? KnownFavoriteDrink(string giver)
    {
        ContactHistory h = _contacts.For(giver);
        return h.FavoriteKnown ? Core.DrinkMenu.ById(h.KnownFavorite) : null;
    }

    // Does THIS bar pour the contact's known favourite? Gates the "stand them their usual" edge row —
    // you can only hand them their usual where it's on the menu.
    private bool BarPoursFavorite(string giver) =>
        CurrentKeep is { } keep && KnownFavoriteDrink(giver) is { } fav
        && Core.DrinkMenu.For(keep).Any(d => d.Id == fav.Id);

    // Buy a round for the whole room — a bigger spend that WARMS the regulars actually drinking here
    // (#247 kin #224: the cheap way to thaw a cold contact). Goodwill is booked on the ContactLedger,
    // the same saved book that holds mission history and bank balances — a future relationship layer
    // reads it. You drink too, so the round counts as a tot on your own legs.
    private void BuyRoundForRoom()
    {
        if (_barMenu is not { } keep)
        {
            return;
        }
        Core.Interior.BarTab tab = keep.BuyRound(_credits);
        if (!tab.Poured)
        {
            _barNotice = tab.Line;
            ShowPulseMessage(tab.Line);
            return;
        }
        _credits = tab.RemainingCredits;

        // A round SATISFIES the room for this visit: only the FIRST round loosens tongues (owner: "their
        // initiative … not a vending machine"). A second round the same visit still warms goodwill (#283)
        // but the tongues are already loose — no re-roll.
        bool loosenTongues = !_roundThisVisit;

        bool backOpen = _dockedHavenId is { } st
            && UnlockedHatchesFor(st).Any(h => HavenInterior.HatchGrowsWing(st, h));
        var warmed = new List<string>();
        var volunteered = new List<string>();
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
                continue; // #425: the oracle isn't a contact — she has her own corner flow, not the round
            }
            if (!seen.Add(giver))
            {
                continue; // one contact can hold two consoles (the roaming Magpie) — warm them once
            }
            // The roaming Magpie only drinks with the room when their rota has them in it this watch.
            if (giver.Contains("MAGPIE", StringComparison.OrdinalIgnoreCase)
                && !HavenInterior.ResolveMagpie(SimTime, backOpen).Present)
            {
                continue;
            }

            // Owner 2026-07-18 — a round loosens tongues: each regular who drank rolls, on their own
            // initiative, whether to volunteer something. Known contacts (goodwill-weighted) offer better
            // material; strangers give vague color. Seeded per-NPC + this bar visit, deterministic. Once
            // per visit only (the gate above), routed into the durable overheard book (no auto-vanish).
            if (loosenTongues)
            {
                bool known = _contacts.For(giver).HasHistory;
                ulong seed = DiceRule.Seed($"round-tip:{giver}:{_dockedHavenId}", (long)SimTime);
                TipTier tier = RoundTips.Volunteer(seed, _contacts.For(giver).Goodwill, known);
                if (VolunteeredTipLine(giver, GiverDisplay(giver), tier) is { } tip)
                {
                    Overhear(tip, giver);
                    volunteered.Add(tip);
                }
            }

            _contacts.AddGoodwill(giver, giver, 1);
            warmed.Add(GiverDisplay(giver));
        }

        _roundThisVisit = true;
        // The captain's own glass is in the round — a lone drink for the nerve (you're pouring, not sharing
        // a table). NerveModel's weak-solo curve + the tot-count drunk gate apply.
        string receipt = PourRum($"{keep.DrinkName}, all round — {keep.DrinkFlavor}", NerveModel.DrinkKind.BarSpecial);
        string cheers = warmed.Count > 0 ? $" {string.Join(", ", warmed)} raise a glass to you." : "";
        string tips = volunteered.Count > 0 ? "  " + string.Join("  ", volunteered) : "";
        _barNotice = tab.Line + cheers + tips;
        // The words the player paid a round to hear ride the durable book (above) AND a lingering toast.
        ShowPulseMessage($"{receipt}{cheers}{tips} (−{tab.Cost:N0} cr)");
        RequestVaultSave(); // #225: the purse moved, goodwill booked, the overheard book grew
    }

    // The line a round-loosened regular volunteers, by how good their roll turned out. Solid/Choice hand
    // real intel (the same #308 OpensUp material — a dark-running ship, a heat warning, a price whisper);
    // vague is atmosphere only. Null when they stay quiet.
    private string? VolunteeredTipLine(string giver, string display, TipTier tier) => tier switch
    {
        TipTier.Choice or TipTier.Solid => $"🍻 {display}, loosened by the round, leans in: {OpenIntelLine(giver)}",
        TipTier.Vague => $"🍻 {display} raises the glass: {VagueColorLine()}",
        _ => null,
    };

    private static readonly string[] VagueColor =
    [
        "“Quiet season. Too quiet, if you ask me.”",
        "“Watch the docks after dark. That's all I'll say.”",
        "“Somebody always owes somebody out here.”",
        "“The good runs dried up. Or the good runners got careful.”",
    ];

    private string VagueColorLine() => VagueColor[(int)((SimTime / 60) % VagueColor.Length)];

    // Ask the barkeep what they've heard — a cheap tip line for flavor (deterministic per sim-hour).
    private void AskBarkeepForRumor()
    {
        if (_barMenu is not { } keep)
        {
            return;
        }
        string rumor = keep.RumorAt(SimTime);
        _barNotice = rumor;
        Overhear($"🍺 {keep.Name}: {rumor}", keep.Name); // durable — a rumor heard doesn't auto-vanish (#212)
        ShowPulseMessage($"🍺 {keep.Name}: {rumor}");
    }

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
        DrinkOfferResult offered = ContactDrink.OfferDrink(offerSeed, goodwillBefore, holdingSecret, offeringFavorite);
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
        DrinkParley parley = ContactDrink.Roll(seed, goodwillBefore, holdingSecret, offeringFavorite);

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
                // LeadFor already names the drink they took, so no separate "takes the …" here.
                line = $"🍷 {Core.DrinkTell.LeadFor(chosen, display)} {OpenIntelLine(giver, channel)}{learn}";
                Overhear(line, giver); // durable — intel you paid for doesn't auto-vanish (#212, owner)
                break;

            case DrinkOutcome.BusinessUnlock:
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

    // The concrete thing you let slip on a bad roll, chosen deterministically from what you are ACTUALLY
    // carrying: a hot-cargo hold first (the costliest tell), then live heat, then your current plan,
    // then — with nothing to hide — a harmless read of your purse. Always a real fact they could use.
    private string SlipTell()
    {
        string? hot = _cargoByClass
            .Where(kv => kv.Value > 0 && IsHotClass(kv.Key))
            .OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key).FirstOrDefault();
        if (hot is not null)
        {
            return $"the hot {hot} in your hold";
        }
        if (_heat.Level > 0)
        {
            return $"that you're running heat (level {_heat.Level})";
        }
        Quest? plan = _quests.FirstOrDefault(q => q.State is QuestState.Active or QuestState.PickedUp);
        if (plan is { } p)
        {
            string where = BodyById(p.DestBodyId)?.Name ?? p.TargetCallsign;
            if (!string.IsNullOrWhiteSpace(where))
            {
                return $"where you're really bound — {where}";
            }
        }

        return "how thin your purse really runs";
    }

    // The concrete intel a contact hands you when they open up — a rumor made real. Prefer a live
    // off-books ship the public board wouldn't show (name + route), the actionable kind; fall back to a
    // solid heat or price tip. Deterministic per sim-second + berth (OfferIndex), so it never flickers.
    private string OpenIntelLine(string giver) => OpenIntelLine(giver, Core.TellChannel.Business);

    // #5 SundayMorningWind — the tell rides the channel the CHOSEN drink opened. A gin/the hard stuff
    // (Business) hands the sharp, actionable tip — an off-books ghost, a heat warning. A beer (SmallTalk)
    // names one plain trading fact. The local specialty (LocalRumor) loosens the neighbourhood's own
    // gossip, the keep's kind of word. Same live game state, three depths of tell.
    private string OpenIntelLine(string giver, Core.TellChannel channel)
    {
        if (channel == Core.TellChannel.LocalRumor)
        {
            // The house's own pour loosens the house's own gossip — the barkeep's neighbourhood word.
            return CurrentKeep is { } keep ? $"“{keep.RumorAt(SimTime).Trim('“', '”')}”" : SmallTalkFact();
        }
        if (channel == Core.TellChannel.SmallTalk)
        {
            return SmallTalkFact(); // a beer names one plain fact, no more.
        }

        // Business (a gin / the hard stuff): the sharp, actionable tell.
        List<NpcState> ghosts = _npcStates
            .Where(n => n.Active && !n.Arrived && !n.Boarded && !n.Ship.IsPod && !n.Ship.PublishesTimetable)
            .OrderBy(n => n.Ship.Id, StringComparer.Ordinal)
            .ToList();
        if (ghosts.Count > 0)
        {
            NpcShip g = ghosts[OfferIndex(ghosts.Count)].Ship;
            return $"“{g.Callsign} runs dark, {RouteLabel(g)} — carrying, light on guns. Worth more than the drink cost you. You didn't hear it from me.”";
        }
        if (_heat.Level > 0)
        {
            return "“Word on the wire has your face on it — the collectors are asking after you. Lie low a watch before you run anything hot through here.”";
        }

        return "“Prices at the next berth run soft on ice, hard on ore this cycle. Trade accordingly, friend.”";
    }

    // A single plain trading fact — the small-talk tell a beer hands you (a fact, never a proposition).
    private string SmallTalkFact() =>
        _heat.Level > 0
            ? "“Heard the docks are jumpy this cycle — extra eyes at the gate. Just so you know, friend.”"
            : "“Prices at the next berth run soft on ice, hard on ore this cycle. That much I'll say over a beer.”";
}
