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
using BustedEncounter = SpaceSails.Client.Pages.Map.BustedEncounter;
using Collector = SpaceSails.Client.Pages.Map.Collector;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// BustedCard — the code-behind for BustedCard.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of BustedCard.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class BustedCard
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public int _credits { get; set; }
    [Parameter] public bool _hasNetJammer { get; set; }
    [Parameter] public HotCargoLedger _hotCargo { get; set; } = default!;
    [Parameter] public int _reactionMassPulses { get; set; }
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public Action<string> BustedBoliviaChoose { get; set; } = default!;
    [Parameter] public EventCallback BustedBribe { get; set; }
    [Parameter] public EventCallback BustedResist { get; set; }
    [Parameter] public Action BustedResistLostConfirm { get; set; } = default!;
    [Parameter] public Action BustedResurrect { get; set; } = default!;
    /// <summary>#535 · Whether the fifth exit is drawn at all — the page's own <c>CarryingABlackOpsKey</c>.</summary>
    [Parameter] public bool CarryingABlackOpsKey { get; set; }
    [Parameter] public Action PresentTheBlackOpsKey { get; set; } = default!;
    [Parameter] public Action<bool> OnBustedSubmit { get; set; } = default!;
    private void BustedSubmit(bool harsher = false) => OnBustedSubmit(harsher);
    [Parameter] public Action CloseBusted { get; set; } = default!;
    [Parameter] public IReadOnlyList<string>? DeathNerveLedgerLines { get; set; }
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<int, string> HeatFlames { get; set; } = default!;
    [Parameter] public string HotGlossTitle { get; set; } = default!;
    [Parameter] public Action OpenLogbookFromDeath { get; set; } = default!;
    [Parameter] public Func<Action, Task> PressAndRefocus { get; set; } = default!;
    [Parameter] public BustedEncounter bust { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
