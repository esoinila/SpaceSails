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
/// WHAT IS POURED — the house special, a named drink off the menu, the favourite a keep remembers, a
/// round for the whole room, and the rumor you ask for while the glass is in your hand.
///
/// <para>Every drink rides through <c>PourRum</c>, so a third round ashore makes the deck just as tilty
/// as one aboard, and every spend gets a #119-style receipt naming the drink and the coin.</para>
///
/// <para>#247 · <b>…AND WHAT IS EATEN</b>, which is the one thing in this file that is not poured. The
/// Special off the bar's board (<c>OrderTheSpecial</c>) is bought over the same counter with the same coin
/// through the same <c>Drink.PriceAt</c> seam, and that is exactly where it parts company with the glasses:
/// a plate does not route through <c>PourRum</c>, counts no tot and never tilts the deck (#756's law). It
/// reaches the nerve the way the med bay's pill does — the same <c>NerveModel</c> relief seam,
/// un-diminished, at the one bigger step #247 states — and the kitchen serves one a sitting.</para>
///
/// <para>Split out of <c>Map.Quests.Bar.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
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
        // #1006 · ONE ROUND, ONE MEMORY. The vague-colour pick is salted per speaker (Core.RoomColor), and
        // this round-scoped memory is what stops two regulars reading the same card in the same breath —
        // see RoomColor.Round for what happens when the room outgrows the four-line pool.
        var colour = new RoomColor.Round();
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
                if (VolunteeredTipLine(giver, GiverDisplay(giver), tier, colour) is { } tip)
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

        // #973 · …and the loosened room turns to the weather. Never a second draw: the line already in the
        // air this visit is the one that gets said out loud, which is why this returns words rather than
        // choosing any (Map.Weather.cs).
        string weather = TheRoundMakesItTheRoomsTopic(loosenTongues);
        _barNotice = tab.Line + cheers + tips + weather;
        // The words the player paid a round to hear ride the durable book (above) AND a lingering toast.
        ShowPulseMessage($"{receipt}{cheers}{tips}{weather} (−{tab.Cost:N0} cr)");
        RequestVaultSave(); // #225: the purse moved, goodwill booked, the overheard book grew
    }

    // The line a round-loosened regular volunteers, by how good their roll turned out. Solid/Choice hand
    // real intel (the same #308 OpensUp material — a dark-running ship, a heat warning, a price whisper);
    // vague is atmosphere only. Null when they stay quiet.
    private string? VolunteeredTipLine(
        string giver, string display, TipTier tier, RoomColor.Round colour) => tier switch
    {
        TipTier.Choice or TipTier.Solid => $"🍻 {display}, loosened by the round, leans in: {OpenIntelLine(giver)}",
        TipTier.Vague => $"🍻 {display} raises the glass: {colour.LineFor(giver, SimTime)}",
        _ => null,
    };

    // ── #247 · THE BOARD, AND WHAT COMES OFF IT ───────────────────────────────────────────────────────
    //
    // Owner: "Eating = the meal-sized shore-leave beat beside the drink (bigger #226 relief, small cr), and
    // ordering the Special is a tiny dice moment — usually delicious, occasionally a story."
    //
    // WHICH WATCH THE BOARD IS CHALKED FOR: the FROZEN docking watch this room was welded at (BarWatch, off
    // _dockVisitSimTime), never the live clock.
    //
    // This is #709's law and it is load-bearing twice over. The chairs, the rota, the rumour and the door a
    // man comes out of are all read off that one instant, so a board on the live clock would be the only
    // thing in the room keeping a different time. And it is the difference between a bug and no bug: a card
    // stays open across sim-seconds — at warp, across watches — so a price PRINTED on the button off
    // SimTime and a price DEBITED off SimTime one press later are two different numbers, which is this
    // repo's most common bug by measure. Frozen, both halves ask the same shift and get the same answer.
    //
    // The outcome rides the same number for the same reason: one plate, one watch, one supper.

    // What is on the board at the counter we are standing at, priced for this visit's watch — or null where
    // the counter has no kitchen behind it (the Hive's, whose card under the glass IS its food).
    private Core.Drink? TheSpecialOnTheBoard =>
        _barMenu is { } keep ? TheMenuBoard.SpecialOn(keep.BodyId, BarWatch) : null;

    // The board's own chalked line, for the plate above the row.
    private string? TheBoardLine =>
        _barMenu is { } keep ? TheMenuBoard.For(keep.BodyId)?.BoardLine : null;

    // ONE SPECIAL A SITTING. The kitchen chalks one dish and serves it once a visit — the same visit-scoped
    // idiom the round for the room already uses (_roundThisVisit), cleared by EnsureBarVisit when the berth
    // changes. It is what keeps a flat, level-independent relief from becoming a button you hold down: the
    // limiter is a sitting, never drunkenness, because a plate is not a pour (#756).
    private bool _specialThisVisit;

    /// <summary>#247 QA · <c>?special=story</c> — force the board's rare outcome. Set in the boot-query parse
    /// (Map.Sim.World.QueryArcs.cs) and read once, below.
    ///
    /// <para>It forces the ROLL and never the content, which is <c>?roll=</c>'s own philosophy (#746) and
    /// <c>?tender=flash</c>'s (#1022). The reason it has to exist is particular to this roll: the outcome is
    /// seeded on the CAPTAIN as well as on the bar and the watch, so no URL can be written that shows a
    /// tester the rare line — every universe rolls its own — and a one-in-ten sentence with no lever at all
    /// is an authored beat nobody can reach.</para></summary>
    private bool _specialStoryCheat;

    // Order the Special: ONE debit through the same Drink.PriceAt seam every row on this card is bought
    // through, then the dice moment, then the relief. The plate does NOT route through PourRum — food does
    // not tilt the deck (#756's law) — so it reaches the nerve the way the med bay's pill does: the same
    // NerveModel relief seam, un-diminished, at the one bigger step #247 states (NerveModel.MealRestore).
    private void OrderTheSpecial()
    {
        if (_barMenu is not { } keep || TheSpecialOnTheBoard is not { } plate
            || TheMenuBoard.For(keep.BodyId) is not { } board)
        {
            return;
        }

        if (_specialThisVisit)
        {
            // Refused OUT LOUD in the keep's own words, never by a control that will not press (#212/#603).
            _barNotice = keep.SelfService
                ? "The reader declines: one Special a sitting, and the board has already served yours."
                : "“One Special a sitting, spacer. Come back next time you're through.”";
            ShowPulseMessage(_barNotice);
            return;
        }

        int cost = plate.PriceAt(keep.DrinkPrice);
        if (_credits < cost)
        {
            _barNotice = keep.SelfService
                ? $"The reader declines: the Special is {cost} cr and the purse is short."
                : $"“Special's {cost} cr — come back when the purse can cover it, spacer.”";
            ShowPulseMessage(_barNotice);
            return;
        }

        _credits -= cost;
        _specialThisVisit = true;

        // The tiny dice moment (#247), cast on the ONE rule every consequence in this game rolls on:
        // deterministic per (bodyId, watch, captain), so a captain who eats the same plate twice gets the
        // same plate twice, and two captains at one counter on one shift can get different suppers. The
        // die is CAST either way — ?special=story only decides which side of the threshold to read it on,
        // which is why the face below is the real face and not a number typed in for a tester.
        Core.DiceRoll roll = TheMenuBoard.RollTheSpecial(keep.BodyId, BarWatch, ActiveCaptainName);

        double beforeNerve = _nerve;
        ApplyNerveRelief(NerveModel.RestoreAmount(NerveModel.DrinkKind.Meal, _nerve, totNumber: 1));
        double restored = _nerve - beforeNerve;
        string steadying = NerveModel.SteadyingNote(NerveModel.DrinkKind.Meal, totNumber: 1, restored);

        // The #119 receipt idiom, exactly as a pour gets one: what it was, what it cost, what happened.
        string receipt =
            $"🍽 {TheMenuBoard.SpecialLabel} — {cost} cr. {board.PlateLine} "
            + $"{TheMenuBoard.OutcomeOf(roll, _specialStoryCheat)} (d20 {roll.Face}) — {steadying}";
        _barNotice = receipt;
        ShowPulseMessage($"{receipt} (−{cost:N0} cr)");
        RequestVaultSave(); // #225: the purse moved (and the relief moved the nerve)
    }

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
        AskingIsBeingSeenAsking(keep);
    }
}
