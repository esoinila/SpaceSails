using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// CarriedPockets — the code-behind for CarriedPockets.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of SatchelPanel under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable the panel's `@if` binds
// arrives under the name that binding gave it. That is the whole trick: it is what let the markup move
// out of SatchelPanel.razor without a single character of it changing.
public partial class CarriedPockets
{
    [Parameter] public List<Core.Satchel.Item> _satchel { get; set; } = default!;
    [Parameter] public string? _satchelOutcome { get; set; }
    [Parameter] public (SatchelTry.Target Target, string? Context, string Label)? _satchelTarget { get; set; }
    /// <summary>the page's `bool _walletOpen` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_walletOpen` and the assignment still lands on the page.</summary>
    [Parameter] public bool _walletOpenValue { get; set; }
    /// <summary>The page's `_walletOpen = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _walletOpenSet { get; set; } = default!;
    private bool _walletOpen { get => _walletOpenValue; set { _walletOpenValue = value; _walletOpenSet(value); } }
    [Parameter] public Func<Core.Satchel.Item, bool> CanWriteUp { get; set; } = default!;
    [Parameter] public EventCallback CloseTheLoadChooser { get; set; }
    [Parameter] public Func<Core.Satchel.Item, string> LeaveHintFor { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> LeaveItem { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> LoadHint { get; set; } = default!;
    [Parameter] public Action<SentryHandLoad.Pick> LoadIntoIt { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> LoadIsOffered { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, CarriedObject.Reveal?> LookAtItem { get; set; } = default!;
    [Parameter] public Action<SentryHandLoad.Pick, int> NudgeShare { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> OpenItemCard { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> OpenTheLoadChooser { get; set; } = default!;
    [Parameter] public Func<string?> ProcessingUnderway { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> RipHint { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> RipIsOffered { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> RipItUp { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> BurnIsOffered { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> BurnTheBlackOpsKey { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> SatchelLabel { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> ScanHint { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> ScanIsOffered { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> ScanWithTheKit { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, (SatchelTry.Target Target, string? Context, string Label)?> TargetFor { get; set; } = default!;
    [Parameter] public Func<(Core.Satchel.Item Rounds, int Pocket, string KindName, List<SentryHandLoad.Pick> Picks)?> TheLoadChooser { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> TryItem { get; set; } = default!;
    [Parameter] public EventCallback TryTheWholeWallet { get; set; }
    [Parameter] public Func<IReadOnlyList<Core.Satchel.Item>> Wallet { get; set; } = default!;
    [Parameter] public Func<(SatchelTry.Target Target, string? Context, string Label)?> WalletTarget { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> WriteItUp { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> WriteUpHint { get; set; } = default!;

    // SatchelPanel's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
