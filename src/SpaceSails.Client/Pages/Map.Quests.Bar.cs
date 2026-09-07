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

/// <summary>
/// #247 · THE COUNTER — the keep's card, the visit it belongs to, and the two doorways onto the same
/// counter (#756: walking up to the keep, and using the bar fixture itself).
///
/// <para>A visit is light session state keyed to the docked bar: a round satisfies the room for THIS
/// stay and loosens tongues once. A different berth starts a fresh visit; re-docking the same bar in one
/// session keeps it. The card family is mutually exclusive by construction — opening the counter or a
/// patron's table shuts the oracle's corner card, so two never stack (#425).</para>
///
/// <para>#251 · This is the opening file of a six-part family, and the other five are named for what
/// happens over the counter: <c>.Room</c> (#410's "who's in tonight", and what is overheard),
/// <c>.Drinks</c> (the house special, a drink, a round for the room, and asking for a rumor),
/// <c>.Asking</c> (#781 — asking is being seen asking, and the room saying it back), <c>.Contacts</c>
/// (#306's glass for a known contact, and the table it is drunk at), and <c>.Tells</c> (what gets said,
/// slipped and learned over it).</para>
///
/// <para>The family declares no static field, so the #1163 initializer hazard is absent by construction.
/// No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
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
        TheWeatherComesIn();   // #973 · what this room is talking about today, decided once a visit
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
        TheWeatherComesIn();          // #973 · …and what the room is talking about today
        TheRoomSaysItBack(counter);   // #781 · and what the room has to say about you, if it has anything
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
}
