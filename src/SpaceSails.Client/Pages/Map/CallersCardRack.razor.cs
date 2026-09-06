using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using ShipEpitaph = SpaceSails.Client.Pages.Map.ShipEpitaph;

namespace SpaceSails.Client.Pages;

// CallersCardRack — the code-behind for CallersCardRack.razor.
//
// #251 · the rack owns the story beat and the six people who arrive with something to settle: the rep, the
// hardcase, the walk-in, the finder, her reveal, and the ship's epitaph. Its members live in a .cs file
// beside the component because the razor generator's output is NOT ANALYSED: a finding inside an
// `@code { … }` block is a finding nobody is ever shown.
public partial class CallersCardRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public (FinderCase.Case Case, bool Paying)? _finderCard { get; set; }
    [Parameter] public string? _finderOutcome { get; set; }
    [Parameter] public FinderCase.Case? _finderReveal { get; set; }
    [Parameter] public IReadOnlyList<NebulaRep.RepOffer>? _hardcaseCard { get; set; }
    [Parameter] public NebulaRep.RepPitch? _repCard { get; set; }
    [Parameter] public string? _repSaid { get; set; }
    [Parameter] public ShipEpitaph? _shipEpitaph { get; set; }
    [Parameter] public (StoryBeats.Beat Beat, string? Subject, string? Outcome)? _storyCard { get; set; }
    [Parameter] public WalkIn.Who? _walkInCard { get; set; }
    [Parameter] public Action<bool> AnswerTheFinder { get; set; } = default!;
    [Parameter] public Action<NebulaRep.RepMove> AnswerTheHardcase { get; set; } = default!;
    [Parameter] public Action<NebulaRep.RepMove> AnswerTheRep { get; set; } = default!;
    [Parameter] public Action<bool> AnswerTheWalkIn { get; set; } = default!;
    [Parameter] public Action CloseShipEpitaph { get; set; } = default!;
    [Parameter] public Action CloseStoryCard { get; set; } = default!;
    [Parameter] public Action CloseTheRepsCard { get; set; } = default!;
    [Parameter] public Action CloseTheReveal { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public EventCallback LodgeItWithHim { get; set; } = default!;
    [Parameter] public Action<NebulaClaims.Ask> PressTheClaim { get; set; } = default!;
    [Parameter] public Action<FinderCase.Outcome> SettleTheCase { get; set; } = default!;
    [Parameter] public Func<StoryBeats.Beat, string?, (string Title, string Art, string Caption)> StoryBeatCopy { get; set; } = default!;
    [Parameter] public Action TellKoltNo { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<NebulaClaims.Ask>> TheClaimAsks { get; set; } = default!;
    [Parameter] public string? TheDeskSays { get; set; }
    [Parameter] public bool TheLodgingOfferIsUp { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
