using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Quest = SpaceSails.Client.Pages.Map.Quest;
using SelfieOffer = SpaceSails.Client.Pages.Map.SelfieOffer;
using SurfaceExcursion = SpaceSails.Client.Pages.Map.SurfaceExcursion;

namespace SpaceSails.Client.Pages;

// AtArmsLengthRack — the code-behind for AtArmsLengthRack.razor.
//
// #251 · the rack owns the run of small cards that each hold up ONE thing: the galley card, the nav help
// sheet, the wallet fan, the object in your hand, the selfie and its offer, and the hatch keypad. Its
// members live in a .cs file beside the component because the razor generator's output is NOT ANALYSED: a
// finding inside an `@code { … }` block is a finding nobody is ever shown.
public partial class AtArmsLengthRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public bool _galleyCardOpen { get; set; }
    [Parameter] public string? _lastRumLine { get; set; }
    [Parameter] public bool _navHelpOpen { get; set; }
    [Parameter] public DeckPlan.ConsoleSpot? _pinHatch { get; set; }
    [Parameter] public Quest? _pinJob { get; set; }
    [Parameter] public string? _pinOutcome { get; set; }
    [Parameter] public int _rumTots { get; set; }
    [Parameter] public SelfieOffer? _selfieOffer { get; set; }
    [Parameter] public CapturedSelfie? _selfieShot { get; set; }
    [Parameter] public SurfaceExcursion? _surface { get; set; }
    [Parameter] public TheTender.Line? _tenderLine { get; set; }
    [Parameter] public DeckPlan.ConsoleSpot? _viewObject { get; set; }
    [Parameter] public EventCallback AcceptSelfieOffer { get; set; } = default!;
    [Parameter] public EventCallback CancelPin { get; set; } = default!;
    [Parameter] public Action<Satchel.Item> ChooseThePaper { get; set; } = default!;
    [Parameter] public Action CloseGalleyCard { get; set; } = default!;
    [Parameter] public Action CloseNavHelp { get; set; } = default!;
    [Parameter] public EventCallback CloseSelfieShot { get; set; } = default!;
    [Parameter] public Action CloseTheWalletFan { get; set; } = default!;
    [Parameter] public Action CloseViewObject { get; set; } = default!;
    [Parameter] public EventCallback DeclineSelfieOffer { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public int GalleyFeedAmbientDays { get; set; }
    [Parameter] public int GalleyFeedItemCount { get; set; }
    [Parameter] public EventCallback KeepTheFind { get; set; } = default!;
    [Parameter] public EventCallback LeaveTheFind { get; set; } = default!;
    [Parameter] public string NameOnYourOwnPapers { get; set; } = default!;
    [Parameter] public Func<int, IReadOnlyList<NewsWire.NewsItem>> NewsFeed { get; set; } = default!;
    [Parameter] public EventCallback PinClear { get; set; } = default!;
    [Parameter] public string PinDisplay { get; set; } = default!;
    [Parameter] public Action<string> PinPush { get; set; } = default!;
    [Parameter] public EventCallback PourRumFromGalley { get; set; } = default!;
    [Parameter] public Action<NebulaClaims.Ask> PressTheClaim { get; set; } = default!;
    [Parameter] public Action<SdrScanner.Hit> PressTheHit { get; set; } = default!;
    [Parameter] public bool RumWobbleActive { get; set; }
    [Parameter] public double SimTime { get; set; }
    [Parameter] public EventCallback SubmitPin { get; set; } = default!;
    [Parameter] public Func<Satchel.Item, string, string> TheBookOn { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<NebulaClaims.Ask>> TheClaimAsks { get; set; } = default!;
    [Parameter] public bool TheClaimDeskIsUp { get; set; }
    [Parameter] public bool TheFindIsWaitingOnAnAnswer { get; set; }
    [Parameter] public bool TheKitsCardIsUp { get; set; }
    [Parameter] public Func<IReadOnlyList<SdrScanner.Hit>> TheKitSweeps { get; set; } = default!;
    [Parameter] public Func<Satchel.Item, bool> ThePaperInYourHandIs { get; set; } = default!;
    [Parameter] public IReadOnlyList<Satchel.Item> TheWalletFan { get; set; } = default!;
    [Parameter] public bool WalletFanIsUp { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
