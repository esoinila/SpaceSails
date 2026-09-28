using Microsoft.AspNetCore.Components;
using TutorialTrack = SpaceSails.Client.Pages.Map.TutorialTrack;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// NavDeskCardRack — the code-behind for NavDeskCardRack.razor.
//
// #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME, typed as the
// card it is handed to already declares it — which is what let the markup above move out of Map.razor
// without a single character of it changing.
public partial class NavDeskCardRack
{
    [Parameter] public ShipDesk _activeDesk { get; set; } = default!;
    [Parameter] public string? LoudPlanAlarm { get; set; }
    [Parameter] public EventCallback DismissLoudPlanAlarm { get; set; }
    [Parameter] public string LoudPlanAlarmHail { get; set; } = default!;
    [Parameter] public string LoudPlanAlarmQuote { get; set; } = default!;
    [Parameter] public bool Adrift { get; set; }
    [Parameter] public bool _showRescueOffer { get; set; }
    [Parameter] public EventCallback OpenRescueOffer { get; set; }

    [Parameter] public bool _docked { get; set; }
    [Parameter] public int _reactionMassPulses { get; set; }
    [Parameter] public EventCallback AcceptRescue { get; set; }
    [Parameter] public EventCallback CloseRescueOffer { get; set; }
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public string HotGlossTitle { get; set; } = default!;
    [Parameter] public int ReactionMassCapacity { get; set; }
    [Parameter] public Func<IReadOnlyList<RescueOffer.FeeLine>> RescueFeeLines { get; set; } = default!;
    [Parameter] public bool _showTutorial { get; set; }

    [Parameter] public int _tutorialStep { get; set; }
    [Parameter] public Func<int> ActiveTutorialIndex { get; set; } = default!;
    [Parameter] public Action ToggleTutorial { get; set; } = default!;
    [Parameter] public string[] TutorialSteps { get; set; } = default!;
    [Parameter] public TutorialTrack[] TutorialTracks { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
