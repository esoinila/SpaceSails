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
using SpaceSails.Client.Components;
using SpaceSails.Client.Layout;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using ShuttleStop = SpaceSails.Client.Pages.Map.ShuttleStop;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// BoardTargetCard — the code-behind for BoardTargetCard.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of BoardTargetCard.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class BoardTargetCard
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public int _boardBots { get; set; }
    [Parameter] public int _boardCoin { get; set; }
    /// <summary>the page's `bool _boardEmptyConfirm` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_boardEmptyConfirm` and the assignment still lands on the page.</summary>
    [Parameter] public bool _boardEmptyConfirmValue { get; set; }
    /// <summary>The page's `_boardEmptyConfirm = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _boardEmptyConfirmSet { get; set; } = default!;
    private bool _boardEmptyConfirm { get => _boardEmptyConfirmValue; set { _boardEmptyConfirmValue = value; _boardEmptyConfirmSet(value); } }
    [Parameter] public int _boardSiteIndex { get; set; }
    [Parameter] public IReadOnlyList<LandingSite> _boardSites { get; set; } = default!;
    [Parameter] public int _cargoUnits { get; set; }
    [Parameter] public int _credits { get; set; }
    [Parameter] public Action<int> AdjustBoardBots { get; set; } = default!;
    [Parameter] public Action<int> AdjustBoardCoin { get; set; } = default!;
    [Parameter] public int AvailableBots { get; set; }
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public bool BoardingAWreck { get; set; }
    [Parameter] public Func<string> BoardingHoldQuote { get; set; } = default!;
    [Parameter] public Func<int> BoardingHoldSeverity { get; set; } = default!;
    [Parameter] public string BotMusterLine { get; set; } = default!;
    [Parameter] public EventCallback CancelBoarding { get; set; }
    [Parameter] public Func<ShuttleStop, Task> ConfirmBoarding { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDuration { get; set; } = default!;
    [Parameter] public Func<int> HotHoldUnits { get; set; } = default!;
    [Parameter] public Func<string> LoadedBotsSummary { get; set; } = default!;
    [Parameter] public Action<int> SelectBoardSite { get; set; } = default!;
    [Parameter] public Action<int> SetBoardCoin { get; set; } = default!;
    [Parameter] public Action SurfaceGroundInteract { get; set; } = default!;
    [Parameter] public Func<ShuttleStop, Task> TryBoard { get; set; } = default!;
    [Parameter] public ShuttleStop boardStop { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
