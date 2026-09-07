using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using AwayWindow = SpaceSails.Client.Pages.Map.AwayWindow;
using BankSession = SpaceSails.Client.Pages.Map.BankSession;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using Quest = SpaceSails.Client.Pages.Map.Quest;
using RouteWindowRow = SpaceSails.Client.Pages.Map.RouteWindowRow;
using ShuttleFarStop = SpaceSails.Client.Pages.Map.ShuttleFarStop;
using ShuttleStop = SpaceSails.Client.Pages.Map.ShuttleStop;

namespace SpaceSails.Client.Pages;

// AshoreCardRack — the code-behind for AshoreCardRack.razor.
//
// #251 · the rack owns what can be in front of a captain who is off her ship: the deck pulse toast, the
// pending offer, the patron's table, the bank session, the barkeep's counter, the oracle and the shuttle
// bay. Its members live in a .cs file beside the component because the razor generator's output is NOT
// ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
public partial class AshoreCardRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public BankSession? _bankSession { get; set; }
    [Parameter] public Core.Interior.Barkeep? _barMenu { get; set; }
    [Parameter] public string? _barNotice { get; set; }
    [Parameter] public ContactLedger _contacts { get; set; } = default!;
    [Parameter] public int _credits { get; set; }
    [Parameter] public bool _deckMode { get; set; }
    [Parameter] public HeatState _heat { get; set; } = default!;
    [Parameter] public int _oracleDrinks { get; set; }
    [Parameter] public OracleLine? _oracleLine { get; set; }
    [Parameter] public string? _oracleNotice { get; set; }
    [Parameter] public bool _oracleOpen { get; set; }
    [Parameter] public string? _patronDrink { get; set; }
    [Parameter] public string? _patronDrinkBlurb { get; set; }
    [Parameter] public Quest? _pendingOffer { get; set; }
    [Parameter] public PulseSlot _pulse { get; set; } = default!;
    [Parameter] public bool _showBarMenu { get; set; }
    [Parameter] public List<ShuttleFarStop> _shuttleBayFarStops { get; set; } = default!;
    [Parameter] public List<ShuttleStop>? _shuttleBayStops { get; set; }
    [Parameter] public Action AcceptOffer { get; set; } = default!;
    [Parameter] public EventCallback AskAboutKaamos { get; set; } = default!;
    [Parameter] public EventCallback AskAboutNebula { get; set; } = default!;
    [Parameter] public EventCallback AskBarkeepForRumor { get; set; } = default!;
    [Parameter] public Func<string, double?> AwayReopenSeconds { get; set; } = default!;
    [Parameter] public Func<AwayWindow, string> AwayWindowWord { get; set; } = default!;
    [Parameter] public long[] BankAmounts { get; set; } = default!;
    [Parameter] public Action<long> BankDeposit { get; set; } = default!;
    [Parameter] public long BankLoanPrincipal { get; set; }
    [Parameter] public Action<long> BankRepay { get; set; } = default!;
    [Parameter] public Action<long> BankWithdraw { get; set; } = default!;
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public Action<bool> BookARoom { get; set; } = default!;
    [Parameter] public string BookingHint { get; set; } = default!;
    [Parameter] public Action BorrowFavorFromBank { get; set; } = default!;
    [Parameter] public Action<Core.Drink> BuyDrink { get; set; } = default!;
    [Parameter] public EventCallback BuyHouseSpecial { get; set; } = default!;
    [Parameter] public EventCallback BuyOracleDrink { get; set; } = default!;
    [Parameter] public EventCallback BuyRoundForRoom { get; set; } = default!;
    [Parameter] public EventCallback BuyTheCutter { get; set; } = default!;
    [Parameter] public EventCallback BuyTheKit { get; set; } = default!;
    [Parameter] public bool CaptainIsOnAStool { get; set; }
    [Parameter] public Action CloseBank { get; set; } = default!;
    [Parameter] public Action CloseBarkeep { get; set; } = default!;
    [Parameter] public Action CloseOracle { get; set; } = default!;
    [Parameter] public Action ClosePatronTable { get; set; } = default!;
    [Parameter] public Action CloseShuttleBayDoor { get; set; } = default!;
    [Parameter] public Func<string, string, Core.Interior.Barkeep, RenderFragment> ContactDrinkOffer { get; set; } = default!;
    [Parameter] public Func<bool> CounterHasStools { get; set; } = default!;
    [Parameter] public Core.Interior.Barkeep? CurrentKeep { get; set; }
    [Parameter] public Action DeclineOffer { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDuration { get; set; } = default!;
    [Parameter] public Func<string, string> GiverDisplay { get; set; } = default!;
    [Parameter] public Func<Quest, IReadOnlyList<string>> JobPlainBlock { get; set; } = default!;
    [Parameter] public Func<bool> KaamosBarSeamAvailable { get; set; } = default!;
    [Parameter] public Func<string> KaamosBarSeamLabel { get; set; } = default!;
    [Parameter] public Func<string> KaamosBarSeamTitle { get; set; } = default!;
    [Parameter] public Func<bool> NebulaBarSeamAvailable { get; set; } = default!;
    [Parameter] public Func<string> NebulaBarSeamLabel { get; set; } = default!;
    [Parameter] public Func<string> NebulaBarSeamTitle { get; set; } = default!;
    [Parameter] public EventCallback NextOracleLine { get; set; } = default!;
    [Parameter] public Action<ShuttleStop> OpenBoardingPanel { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<(string Giver, string Display)>> PresentBarContacts { get; set; } = default!;
    [Parameter] public Func<string, double?> RouteReturnBySimTime { get; set; } = default!;
    [Parameter] public Func<RouteWindowRow, string> RouteWindowRowText { get; set; } = default!;
    [Parameter] public string? SeatedStoolPlate { get; set; }
    [Parameter] public Func<double, string> ShuttleReachText { get; set; } = default!;
    [Parameter] public Func<List<RouteWindowRow>> ShuttleRouteWindowRows { get; set; } = default!;
    [Parameter] public Func<double, string> ShuttleSeparationText { get; set; } = default!;
    [Parameter] public Func<ShuttleExcursion.RangeTrend, string> ShuttleTrendWord { get; set; } = default!;
    [Parameter] public Func<string, Task> StoolMoveClicked { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, bool> StoolMoveOnOffer { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, string> StoolMoveRefusal { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Encounter.Move>> StoolMovesOnTheTable { get; set; } = default!;
    [Parameter] public EventCallback TakeAStool { get; set; } = default!;
    [Parameter] public Action<ShuttleStop> TakeShuttleTo { get; set; } = default!;
    [Parameter] public bool TheBackCounterIsOpen { get; set; }
    [Parameter] public bool TheCounterRentsRooms { get; set; }
    [Parameter] public Func<string?> TheKeepsTell { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<string>> TheOverheardBlock { get; set; } = default!;
    [Parameter] public EventCallback ToggleBarMenu { get; set; } = default!;
    [Parameter] public Func<string?> WhoIsInTonight { get; set; } = default!;
    [Parameter] public Func<string, AwayWindow> WindowOn { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
