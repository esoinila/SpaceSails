using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// BarMenuCard — the code-behind for BarMenuCard.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of BarMenuCard.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class BarMenuCard
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public string? _barNotice { get; set; }
    [Parameter] public int _credits { get; set; }
    [Parameter] public bool _showBarMenu { get; set; }
    [Parameter] public EventCallback AskAboutKaamos { get; set; }
    [Parameter] public EventCallback AskAboutNebula { get; set; }
    [Parameter] public EventCallback AskBarkeepForRumor { get; set; }
    [Parameter] public Action<bool> BookARoom { get; set; } = default!;
    [Parameter] public string BookingHint { get; set; } = default!;
    [Parameter] public Action<Core.Drink> BuyDrink { get; set; } = default!;
    [Parameter] public EventCallback BuyHouseSpecial { get; set; }
    [Parameter] public EventCallback BuyRoundForRoom { get; set; }
    [Parameter] public EventCallback BuyTheCutter { get; set; }
    [Parameter] public EventCallback BuyTheKit { get; set; }
    [Parameter] public bool CaptainIsOnAStool { get; set; }
    [Parameter] public Action CloseBarkeep { get; set; } = default!;
    [Parameter] public Func<string, string, Core.Interior.Barkeep, RenderFragment> ContactDrinkOffer { get; set; } = default!;
    [Parameter] public Func<bool> CounterHasStools { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<bool> KaamosBarSeamAvailable { get; set; } = default!;
    [Parameter] public Func<string> KaamosBarSeamLabel { get; set; } = default!;
    [Parameter] public Func<string> KaamosBarSeamTitle { get; set; } = default!;
    [Parameter] public Func<bool> NebulaBarSeamAvailable { get; set; } = default!;
    [Parameter] public Func<string> NebulaBarSeamLabel { get; set; } = default!;
    [Parameter] public Func<string> NebulaBarSeamTitle { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<(string Giver, string Display)>> PresentBarContacts { get; set; } = default!;
    [Parameter] public string? SeatedStoolPlate { get; set; }
    [Parameter] public Func<string, Task> StoolMoveClicked { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, bool> StoolMoveOnOffer { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, string> StoolMoveRefusal { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Encounter.Move>> StoolMovesOnTheTable { get; set; } = default!;
    [Parameter] public EventCallback TakeAStool { get; set; }
    [Parameter] public bool TheBackCounterIsOpen { get; set; }
    [Parameter] public bool TheCounterRentsRooms { get; set; }
    [Parameter] public Func<string?> TheKeepsTell { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<string>> TheOverheardBlock { get; set; } = default!;
    [Parameter] public EventCallback ToggleBarMenu { get; set; }
    [Parameter] public Func<string?> WhoIsInTonight { get; set; } = default!;
    [Parameter] public Core.Interior.Barkeep keep { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
