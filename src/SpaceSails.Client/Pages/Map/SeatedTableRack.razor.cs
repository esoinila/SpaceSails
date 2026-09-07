using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using TableTalk = SpaceSails.Client.Pages.Map.TableTalk;

namespace SpaceSails.Client.Pages;

// SeatedTableRack — the code-behind for SeatedTableRack.razor.
//
// #251 · the rack owns the seated region — the docked strip or the table card, whichever frame the seat is
// in — and the two things a seated captain raises: the paper and the stand-up confirm. Its members live in
// a .cs file beside the component because the razor generator's output is NOT ANALYSED: a finding inside
// an `@code { … }` block is a finding nobody is ever shown.
public partial class SeatedTableRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public int _credits { get; set; }
    [Parameter] public List<Core.Satchel.Item> _satchel { get; set; } = default!;
    [Parameter] public bool _seatedNewsOpen { get; set; }
    [Parameter] public bool ACabinetLeafToWork { get; set; }
    [Parameter] public Func<NewsWire.NewsItem, bool> AlreadyClipped { get; set; } = default!;
    [Parameter] public string CabinetLeafHint { get; set; } = default!;
    [Parameter] public string CabinetLeafLabel { get; set; } = default!;
    [Parameter] public bool CanSpreadTheCaseHere { get; set; }
    [Parameter] public Action<NewsWire.NewsItem> ClipThisStory { get; set; } = default!;
    [Parameter] public Action CloseSeatedNews { get; set; } = default!;
    [Parameter] public Action CloseTable { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Action KeepYourSeat { get; set; } = default!;
    [Parameter] public EventCallback OpenTheSpread { get; set; } = default!;
    [Parameter] public Func<double?> ProcessingFraction { get; set; } = default!;
    [Parameter] public Func<string?> ProcessingUnderway { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> SatchelLabel { get; set; } = default!;
    [Parameter] public Func<string?> SeatedCompanyLine { get; set; } = default!;
    [Parameter] public Func<string?> SeatedCustomerLine { get; set; } = default!;
    [Parameter] public bool SeatedIsDocked { get; set; }
    [Parameter] public Func<IReadOnlyList<NewsWire.NewsItem>> SeatedNewsFeed { get; set; } = default!;
    [Parameter] public int SeatedNewsItemCount { get; set; }
    [Parameter] public Func<string?> SeatedOverheardLine { get; set; } = default!;
    [Parameter] public TableTalk? SeatedTable { get; set; }
    [Parameter] public double SimTime { get; set; }
    [Parameter] public string SpreadDoorHint { get; set; } = default!;
    [Parameter] public string SpreadDoorLabel { get; set; } = default!;
    [Parameter] public bool StandingUpCostsARest { get; set; }
    [Parameter] public Action StandUpFromTable { get; set; } = default!;
    [Parameter] public Func<string, Task> TableMoveClicked { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, bool> TableMoveIsUrged { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, bool> TableMoveOnOffer { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, string> TableMoveRefusal { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Encounter.Move>> TableMovesOnTheTable { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> TableShow { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Core.Satchel.Item>> TableShowables { get; set; } = default!;
    [Parameter] public bool ThePaperIsOpen { get; set; }
    [Parameter] public bool TheStandUpConfirmIsUp { get; set; }
    [Parameter] public EventCallback ToggleSeatedNews { get; set; } = default!;
    [Parameter] public EventCallback WorkTheCabinetLeaf { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
