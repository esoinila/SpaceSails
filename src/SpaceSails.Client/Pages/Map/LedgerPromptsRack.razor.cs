using Microsoft.AspNetCore.Components;
using BankPromptKind = SpaceSails.Client.Pages.Map.BankPromptKind;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// LedgerPromptsRack — the code-behind for LedgerPromptsRack.razor.
//
// #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME, typed as the
// card it is handed to already declares it — which is what let the markup above move out of Map.razor
// without a single character of it changing.
public partial class LedgerPromptsRack
{
    [Parameter] public string _bankNoteValue { get; set; } = default!;
    [Parameter] public Action<string> _bankNoteSet { get; set; } = default!;
    private string _bankNote { get => _bankNoteValue; set { _bankNoteValue = value; _bankNoteSet(value); } }
    [Parameter] public string _bankTitleValue { get; set; } = default!;
    [Parameter] public Action<string> _bankTitleSet { get; set; } = default!;
    private string _bankTitle { get => _bankTitleValue; set { _bankTitleValue = value; _bankTitleSet(value); } }
    [Parameter] public BankPromptKind? _bankPrompt { get; set; }
    [Parameter] public string _bankSlotId { get; set; } = default!;
    [Parameter] public string BankPromptHeading { get; set; } = default!;
    [Parameter] public EventCallback CancelBankPrompt { get; set; }
    [Parameter] public EventCallback CommitBankPrompt { get; set; }
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;

    [Parameter] public bool _importConfirming { get; set; }
    [Parameter] public SaveSlotMeta? _importPreview { get; set; }
    [Parameter] public Func<Task> ApplyPendingImport { get; set; } = default!;
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public EventCallback CancelImport { get; set; }
    [Parameter] public EventCallback ConfirmImportBankingFirst { get; set; }
    [Parameter] public EventCallback ConfirmImportReplace { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
