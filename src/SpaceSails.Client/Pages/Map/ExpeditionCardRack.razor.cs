using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using DeflectionStoryPanel = SpaceSails.Client.Pages.Map.DeflectionStoryPanel;
using ExpeditionBriefCard = SpaceSails.Client.Pages.Map.ExpeditionBriefCard;
using ExpeditionRevealCard = SpaceSails.Client.Pages.Map.ExpeditionRevealCard;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using ShuttleStop = SpaceSails.Client.Pages.Map.ShuttleStop;

namespace SpaceSails.Client.Pages;

// ExpeditionCardRack — the code-behind for ExpeditionCardRack.razor.
//
// #251 · the rack owns the five cards of an excursion: the boarding target, the treasure map, the
// expedition brief, its reveal, and the deflection story. Its members live in a .cs file beside the
// component because the razor generator's output is NOT ANALYSED: a finding inside an `@code { … }` block
// is a finding nobody is ever shown.
public partial class ExpeditionCardRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public int _boardBots { get; set; }
    [Parameter] public int _boardCoin { get; set; }
    /// <summary>the page's `_boardEmptyConfirm = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _boardEmptyConfirmSet { get; set; } = default!;
    /// <summary>the page's `bool _boardEmptyConfirm` — and this markup WRITES it, so it crosses as a pair: the value in, the page's own setter out.</summary>
    [Parameter] public bool _boardEmptyConfirmValue { get; set; }
    [Parameter] public int _boardSiteIndex { get; set; }
    [Parameter] public IReadOnlyList<LandingSite> _boardSites { get; set; } = default!;
    [Parameter] public ShuttleStop? _boardTarget { get; set; }
    [Parameter] public int _cargoUnits { get; set; }
    [Parameter] public int _credits { get; set; }
    [Parameter] public List<DeflectionStoryPanel>? _deflectionStory { get; set; }
    [Parameter] public int _deflectionStoryIndex { get; set; }
    [Parameter] public ExpeditionBriefCard? _expeditionBriefCard { get; set; }
    [Parameter] public ExpeditionRevealCard? _expeditionRevealCard { get; set; }
    [Parameter] public TreasureCache? _treasureMapCard { get; set; }
    [Parameter] public Action<int> AdjustBoardBots { get; set; } = default!;
    [Parameter] public Action<int> AdjustBoardCoin { get; set; } = default!;
    [Parameter] public EventCallback AdvanceDeflectionStory { get; set; } = default!;
    [Parameter] public int AvailableBots { get; set; }
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public bool BoardingAWreck { get; set; }
    [Parameter] public Func<string> BoardingHoldQuote { get; set; } = default!;
    [Parameter] public Func<int> BoardingHoldSeverity { get; set; } = default!;
    [Parameter] public Func<string, string> BodyName { get; set; } = default!;
    [Parameter] public string BotMusterLine { get; set; } = default!;
    [Parameter] public EventCallback CancelBoarding { get; set; } = default!;
    [Parameter] public Func<ShuttleStop, Task> ConfirmBoarding { get; set; } = default!;
    [Parameter] public Func<DeflectionStoryPanel, string> DeflectionPanelArtCss { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public EventCallback DismissExpeditionBrief { get; set; } = default!;
    [Parameter] public EventCallback DismissExpeditionReveal { get; set; } = default!;
    [Parameter] public EventCallback DismissMapCard { get; set; } = default!;
    [Parameter] public Func<ExpeditionSiteKind, string> ExpeditionBriefArtCss { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDuration { get; set; } = default!;
    [Parameter] public Func<int> HotHoldUnits { get; set; } = default!;
    [Parameter] public Func<string> LoadedBotsSummary { get; set; } = default!;
    [Parameter] public Action<int> SelectBoardSite { get; set; } = default!;
    [Parameter] public Action<int> SetBoardCoin { get; set; } = default!;
    [Parameter] public Action SurfaceGroundInteract { get; set; } = default!;
    [Parameter] public Func<string, string> TreasureMapArtCss { get; set; } = default!;
    [Parameter] public Func<ShuttleStop, Task> TryBoard { get; set; } = default!;

    /// <summary>#251 · the member's OWN NAME, so the moved markup still reads and assigns `_boardEmptyConfirm`
    /// and the assignment still lands on the page (NoSurfaceSwallowsAWriteTests).</summary>
    private bool _boardEmptyConfirm { get => _boardEmptyConfirmValue; set { _boardEmptyConfirmValue = value; _boardEmptyConfirmSet(value); } }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
