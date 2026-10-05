using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// KeepsakeShelf — the code-behind for KeepsakeShelf.razor (#620). Same house shape as its siblings: the
// members live in a .cs file so the analysers see them, and every [Parameter] is a member of the page
// under its own name.
public partial class KeepsakeShelf
{
    /// <summary>What is on the shelf (Core's <see cref="Keepsake.Shelf"/>).</summary>
    [Parameter] public Func<IReadOnlyList<Keepsake.Piece>> KeepsakePieces { get; set; } = default!;

    /// <summary>The piece whose card is folded open, if any — only a minute actually spent opens one.</summary>
    [Parameter] public string? KeepsakeOpenId { get; set; }

    /// <summary>The piece the last press was on, so a refusal is drawn under ITS card and no other.</summary>
    [Parameter] public string? KeepsakeAskedId { get; set; }

    /// <summary>The one line the last press produced — the locket's own line, or the refusal.</summary>
    [Parameter] public string? KeepsakeSaid { get; set; }

    /// <summary>The press on a card — the page asks Core what the minute does.</summary>
    [Parameter] public Action<Keepsake.Piece> PressKeepsake { get; set; } = default!;

    /// <summary>Close the folded card.</summary>
    [Parameter] public Action FoldKeepsake { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
