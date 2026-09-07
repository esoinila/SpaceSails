using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ThreadStacks — the code-behind for ThreadStacks.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of SatchelPanel under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable the panel's `@if` binds
// arrives under the name that binding gave it. That is the whole trick: it is what let the markup move
// out of SatchelPanel.razor without a single character of it changing.
public partial class ThreadStacks
{
    [Parameter] public HeldMemory.Theory? _bookTag { get; set; }
    [Parameter] public bool _penInHand { get; set; }
    [Parameter] public string? _satchelOutcome { get; set; }
    [Parameter] public Func<string, bool> NodeIsHeld { get; set; } = default!;
    [Parameter] public Func<string, bool> NodeIsOpen { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Core.FieldNote>, IReadOnlyList<Core.CaseThreads.Row>> NotebookPage { get; set; } = default!;
    [Parameter] public Action<string> NoteTitlePressed { get; set; } = default!;
    [Parameter] public Action<Core.CaseSubjects.SubjectThread> RunThePenDownTheStack { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<HeldMemory.Stack>> SheetStacks { get; set; } = default!;
    [Parameter] public EventCallback TakeUpTheRedPen { get; set; }
    [Parameter] public Func<IReadOnlyList<Core.CaseSubjects.SubjectThread>> TheBookThreads { get; set; } = default!;
    [Parameter] public Action<HeldMemory.Theory?> TheThreadsFilterTurnsTo { get; set; } = default!;

    // SatchelPanel's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
