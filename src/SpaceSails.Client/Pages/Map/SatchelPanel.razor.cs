using Microsoft.AspNetCore.Components;
using NotesView = SpaceSails.Client.Pages.Map.NotesView;
using Quest = SpaceSails.Client.Pages.Map.Quest;
using SatchelPage = SpaceSails.Client.Pages.Map.SatchelPage;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// SatchelPanel — the code-behind for SatchelPanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of SatchelPanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class SatchelPanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public HeldMemory.Theory? _bookTag { get; set; }
    [Parameter] public List<Core.FieldNote> _fieldNotes { get; set; } = default!;
    [Parameter] public List<SpreadReconcile.Paper> _laid { get; set; } = default!;
    [Parameter] public NotesView _notesView { get; set; } = default!;
    [Parameter] public bool _penInHand { get; set; }
    [Parameter] public SpreadReconcile.Result? _reconciled { get; set; }
    [Parameter] public List<Core.Satchel.Item> _satchel { get; set; } = default!;
    [Parameter] public string? _satchelOutcome { get; set; }
    /// <summary>the page's `SatchelPage _satchelPage` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_satchelPage` and the assignment still lands on the page.</summary>
    [Parameter] public SatchelPage _satchelPageValue { get; set; } = default!;
    /// <summary>The page's `_satchelPage = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<SatchelPage> _satchelPageSet { get; set; } = default!;
    private SatchelPage _satchelPage { get => _satchelPageValue; set { _satchelPageValue = value; _satchelPageSet(value); } }
    [Parameter] public (SatchelTry.Target Target, string? Context, string Label)? _satchelTarget { get; set; }
    /// <summary>the page's `bool _walletOpen` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_walletOpen` and the assignment still lands on the page.</summary>
    [Parameter] public bool _walletOpenValue { get; set; }
    /// <summary>The page's `_walletOpen = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _walletOpenSet { get; set; } = default!;
    private bool _walletOpen { get => _walletOpenValue; set { _walletOpenValue = value; _walletOpenSet(value); } }
    [Parameter] public Action<Core.Processing.Work, Core.Satchel.Item, string, (SatchelTry.Target Target, string? Context, string Label)?> BeginProcessing { get; set; } = default!;
    [Parameter] public Func<List<Core.Satchel.Item>> BinnableFinds { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> BinRowFlag { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> BinRowHint { get; set; } = default!;
    [Parameter] public Func<RipAndBin.Bin?> BinWithinReach { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> CanWriteUp { get; set; } = default!;
    [Parameter] public bool CaptainIsSeatedAnywhere { get; set; }
    [Parameter] public bool CaseHasBegun { get; set; }
    [Parameter] public EventCallback ClearTheSpread { get; set; }
    [Parameter] public Action CloseSatchel { get; set; } = default!;
    [Parameter] public EventCallback CloseTheLoadChooser { get; set; }
    [Parameter] public Func<IReadOnlyList<CompassLine>> CompassOnFoot { get; set; } = default!;
    [Parameter] public Func<Quest, (MissionStep Step, string StatusKind)> CurrentStepOf { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<string, bool> IsLaid { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpreadReconcile.Paper>> LayablePapers { get; set; } = default!;
    [Parameter] public Action<SpreadReconcile.Paper> LayItOnTheSpread { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> LeaveHintFor { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> LeaveItem { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> LoadHint { get; set; } = default!;
    [Parameter] public Action<SentryHandLoad.Pick> LoadIntoIt { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> LoadIsOffered { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, CarriedObject.Reveal?> LookAtItem { get; set; } = default!;
    [Parameter] public Func<string, bool> NodeIsHeld { get; set; } = default!;
    [Parameter] public Func<string, bool> NodeIsOpen { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Core.FieldNote>, IReadOnlyList<Core.CaseThreads.Row>> NotebookPage { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Core.FieldNote>> NotesFromThisGround { get; set; } = default!;
    [Parameter] public Action<string> NoteTitlePressed { get; set; } = default!;
    [Parameter] public Action<SentryHandLoad.Pick, int> NudgeShare { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> OpenItemCard { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> OpenTheLoadChooser { get; set; } = default!;
    [Parameter] public Func<string> PlaceUnderfoot { get; set; } = default!;
    [Parameter] public double ProcessingSeconds { get; set; }
    [Parameter] public Func<string?> ProcessingUnderway { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> RipHint { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> RipIsOffered { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> RipItUp { get; set; } = default!;
    [Parameter] public Action<Core.CaseSubjects.SubjectThread> RunThePenDownTheStack { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> BurnIsOffered { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> BurnTheBlackOpsKey { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> SatchelLabel { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> ScanHint { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> ScanIsOffered { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> ScanWithTheKit { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<HeldMemory.Stack>> SheetStacks { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> SplitHint { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> SplitIsOffered { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> SplitTheDocument { get; set; } = default!;
    [Parameter] public Func<List<Core.Satchel.Item>> SpreadableFinds { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, Task> SpreadDigClicked { get; set; } = default!;
    [Parameter] public string SpreadDoorHint { get; set; } = default!;
    [Parameter] public string? SpreadRefusal { get; set; }
    [Parameter] public Func<Core.Satchel.Item, string> SpreadRowVerb { get; set; } = default!;
    [Parameter] public EventCallback TakeUpTheRedPen { get; set; }
    [Parameter] public Func<Core.Satchel.Item, (SatchelTry.Target Target, string? Context, string Label)?> TargetFor { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Core.CaseSubjects.SubjectThread>> TheBookThreads { get; set; } = default!;
    [Parameter] public Func<(Core.Satchel.Item Rounds, int Pocket, string KindName, List<SentryHandLoad.Pick> Picks)?> TheLoadChooser { get; set; } = default!;
    [Parameter] public Action<NotesView> TheNotebookTurnsTo { get; set; } = default!;
    [Parameter] public Action<SatchelPage> TheSatchelTurnsTo { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<HeldMemory.Sheet>> TheSheets { get; set; } = default!;
    [Parameter] public Action<HeldMemory.Theory?> TheThreadsFilterTurnsTo { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Core.FieldNote>> TheWholeCase { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> TryItem { get; set; } = default!;
    [Parameter] public EventCallback TryTheWholeWallet { get; set; }
    [Parameter] public Func<IReadOnlyList<Core.Satchel.Item>> Wallet { get; set; } = default!;
    [Parameter] public Func<(SatchelTry.Target Target, string? Context, string Label)?> WalletTarget { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> WriteItUp { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> WriteUpHint { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
