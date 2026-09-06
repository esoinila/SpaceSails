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
using DesignateTarget = SpaceSails.Client.Pages.Map.DesignateTarget;
using SurfaceBot = SpaceSails.Client.Pages.Map.SurfaceBot;
using SurfaceExcursion = SpaceSails.Client.Pages.Map.SurfaceExcursion;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// CaptainsRemotePanel — the code-behind for CaptainsRemotePanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of CaptainsRemotePanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class CaptainsRemotePanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public bool _boatOrderedCold { get; set; }
    [Parameter] public double _boatWarmth { get; set; }
    [Parameter] public bool _designateOpen { get; set; }
    [Parameter] public List<DesignateTarget> _designateTargets { get; set; } = default!;
    [Parameter] public string? _designateUnit { get; set; }
    [Parameter] public string? _remoteOutcome { get; set; }
    [Parameter] public bool _soundQuietly { get; set; }
    [Parameter] public SurfaceExcursion? _surface { get; set; }
    [Parameter] public bool _weaponsTight { get; set; }
    [Parameter] public Func<string?> AwayWindowRemoteLine { get; set; } = default!;
    [Parameter] public Func<string> AwayWindowRemoteSubLine { get; set; } = default!;
    [Parameter] public EventCallback BackToTheSwitches { get; set; }
    [Parameter] public SilentRunning.BoatState BoatState { get; set; } = default!;
    [Parameter] public Action CloseCaptainsRemote { get; set; } = default!;
    [Parameter] public Func<List<SurfaceBot>> DesignatableGuns { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Action<DesignateTarget> FireOnTheLock { get; set; } = default!;
    [Parameter] public bool OnWreck { get; set; }
    [Parameter] public EventCallback OpenDesignate { get; set; }
    [Parameter] public Action<string> PickTheGun { get; set; } = default!;
    [Parameter] public EventCallback SendTheStanding { get; set; }
    [Parameter] public HullSounding.Method SoundingGear { get; set; } = default!;
    [Parameter] public EventCallback ToggleBoatPower { get; set; }
    [Parameter] public EventCallback ToggleQuietSearch { get; set; }
    [Parameter] public EventCallback ToggleWeaponsTight { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
