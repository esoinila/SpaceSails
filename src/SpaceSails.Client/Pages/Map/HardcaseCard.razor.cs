using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// HardcaseCard — the code-behind for HardcaseCard.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of HardcaseCard.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class HardcaseCard
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public Action<NebulaRep.RepMove> AnswerTheHardcase { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Action TellKoltNo { get; set; } = default!;
    [Parameter] public IReadOnlyList<NebulaRep.RepOffer> koltOffers { get; set; } = default!;

    // #1151 slice 2 · THE CLAIM AT THE TABLE. Five doors back to the page and not one rule of its own:
    // whether he is offering, the door that accepts, what the counter is saying, the rows it deals and the
    // press that takes one. Every one of them is the seam the kiosk's card reads (Map.Claims.Kiosk.cs /
    // Map.Claims.Rep.cs), under the member's own name.
    [Parameter] public Action LodgeItWithHim { get; set; } = default!;
    [Parameter] public Action<NebulaClaims.Ask> PressTheClaim { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<NebulaClaims.Ask>> TheClaimAsks { get; set; } = default!;
    [Parameter] public string? TheDeskSays { get; set; }
    [Parameter] public bool TheLodgingOfferIsUp { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
