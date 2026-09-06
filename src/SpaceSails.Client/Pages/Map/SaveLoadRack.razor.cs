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
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// SaveLoadRack — the code-behind for SaveLoadRack.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of SaveLoadRack.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class SaveLoadRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public string? _activeThreadId { get; set; }
    /// <summary>the page's `string _renameDraft` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_renameDraft` and the assignment still lands on the page.</summary>
    [Parameter] public string _renameDraftValue { get; set; } = default!;
    /// <summary>The page's `_renameDraft = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<string> _renameDraftSet { get; set; } = default!;
    private string _renameDraft { get => _renameDraftValue; set { _renameDraftValue = value; _renameDraftSet(value); } }
    [Parameter] public string? _renamingThreadId { get; set; }
    [Parameter] public bool _resumeAvailable { get; set; }
    [Parameter] public string? _resumeHavenName { get; set; }
    [Parameter] public bool _resumeTampered { get; set; }
    /// <summary>the page's `bool _showDevStarts` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_showDevStarts` and the assignment still lands on the page.</summary>
    [Parameter] public bool _showDevStartsValue { get; set; }
    /// <summary>The page's `_showDevStarts = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _showDevStartsSet { get; set; } = default!;
    private bool _showDevStarts { get => _showDevStartsValue; set { _showDevStartsValue = value; _showDevStartsSet(value); } }
    [Parameter] public bool _showSaveDrawer { get; set; }
    [Parameter] public bool _showStartPicker { get; set; }
    [Parameter] public IReadOnlyList<SaveSlotMeta> _slotList { get; set; } = default!;
    [Parameter] public IReadOnlyList<GameThreadGroup> _voyageGroups { get; set; } = default!;
    /// <summary>#161 · the page's `bool FrontDoorReady` — the berths are known and the vault is read, which
    /// is stages before `_worldReady`. See the property's own note in Map.Vault.</summary>
    [Parameter] public bool FrontDoorReady { get; set; }
    [Parameter] public string[] AllSlotIds { get; set; } = default!;
    [Parameter] public Action<string> BeginBankToSlot { get; set; } = default!;
    [Parameter] public Action<string, string> BeginEditSlotPage { get; set; } = default!;
    [Parameter] public EventCallback BeginExportMoment { get; set; }
    [Parameter] public Action<string> BeginRenameCaptain { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<(string Id, string Name)>> BerthStarts { get; set; } = default!;
    [Parameter] public EventCallback CancelRenameCaptain { get; set; }
    [Parameter] public Func<string, string> CaptainInitial { get; set; } = default!;
    [Parameter] public Func<string, string> CaptainMonoColor { get; set; } = default!;
    [Parameter] public Func<GameThreadInfo, string> CaptainRetiredSummary { get; set; } = default!;
    [Parameter] public Func<GameThreadInfo, string> CaptainWhen { get; set; } = default!;
    [Parameter] public Func<string, Task> ChooseBerthStart { get; set; } = default!;
    [Parameter] public EventCallback CloseSaveDrawer { get; set; }
    [Parameter] public EventCallback CommitRenameCaptain { get; set; }
    [Parameter] public EventCallback ContinueFromSave { get; set; }
    [Parameter] public Action<string, string> DeleteThreadSlot { get; set; } = default!;
    [Parameter] public Action<string, string> ExportThreadSlot { get; set; } = default!;
    [Parameter] public Action<SpaceSails.Core.DevStarts.Entry> GoToDevStart { get; set; } = default!;
    [Parameter] public Func<string, string, Task> ImportIntoThreadSlot { get; set; } = default!;
    [Parameter] public EventCallback ImportVault { get; set; }
    [Parameter] public Func<string, string, Task> LoadThreadSlot { get; set; } = default!;
    [Parameter] public Func<string, string, bool> NoteIsOpen { get; set; } = default!;
    [Parameter] public EventCallback<KeyboardEventArgs> RenameKeyDown { get; set; }
    [Parameter] public bool ShowDevStarts { get; set; }
    [Parameter] public Action<CapturedSelfie> ShowSelfie { get; set; } = default!;
    [Parameter] public Action<string> StartDockedAtHaven { get; set; } = default!;
    [Parameter] public Action<string, string> ToggleNote { get; set; } = default!;
    [Parameter] public string TutorialHomeHavenId { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
