using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using BustedEncounter = SpaceSails.Client.Pages.Map.BustedEncounter;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;

namespace SpaceSails.Client.Pages;

// AftermathCardRack — the code-behind for AftermathCardRack.razor.
//
// #251 · the rack owns the eight cards the page raises once the doing is over: the payout, the arrest,
// the convergence reveal, the ground lesson, what grew on the ground, the tube rearm, the air notice and
// the old-crew face. Its members live in a .cs file beside the component because the razor generator's
// output is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
public partial class AftermathCardRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public bool _airCardOpen { get; set; }
    [Parameter] public BustedEncounter? _busted { get; set; }
    [Parameter] public MissionCelebration? _celebration { get; set; }
    [Parameter] public bool _convergenceRevealOpen { get; set; }
    [Parameter] public int _credits { get; set; }
    [Parameter] public string? _faceScene { get; set; }
    [Parameter] public string? _faceSceneReply { get; set; }
    [Parameter] public bool _groundGrewOpen { get; set; }
    [Parameter] public string _groundGrewWhere { get; set; } = default!;
    [Parameter] public bool _groundLessonOpen { get; set; }
    [Parameter] public bool _hasNetJammer { get; set; }
    [Parameter] public HotCargoLedger _hotCargo { get; set; } = default!;
    [Parameter] public int _reactionMassPulses { get; set; }
    [Parameter] public bool _showSaveDrawer { get; set; }
    [Parameter] public bool _tubeRearmOpen { get; set; }
    [Parameter] public Action<OldCrewScene.Answer> AnswerTheFace { get; set; } = default!;
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public Action<string> BustedBoliviaChoose { get; set; } = default!;
    [Parameter] public EventCallback BustedBribe { get; set; } = default!;
    [Parameter] public EventCallback BustedResist { get; set; } = default!;
    [Parameter] public Action BustedResistLostConfirm { get; set; } = default!;
    [Parameter] public Action BustedResurrect { get; set; } = default!;
    [Parameter] public Action<bool> BustedSubmit { get; set; } = default!;
    [Parameter] public bool CarryingABlackOpsKey { get; set; }
    [Parameter] public Action CloseAirCard { get; set; } = default!;
    [Parameter] public Action CloseBusted { get; set; } = default!;
    [Parameter] public Action CloseConvergenceReveal { get; set; } = default!;
    [Parameter] public Action CloseGroundGrew { get; set; } = default!;
    [Parameter] public Action CloseGroundLesson { get; set; } = default!;
    [Parameter] public Action CloseTheFaceScene { get; set; } = default!;
    [Parameter] public Action CloseTubeRearm { get; set; } = default!;
    [Parameter] public IReadOnlyList<string>? DeathNerveLedgerLines { get; set; }
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public EventCallback DismissCelebration { get; set; } = default!;
    [Parameter] public Func<int, string> HeatFlames { get; set; } = default!;
    [Parameter] public string HotGlossTitle { get; set; } = default!;
    [Parameter] public Func<string, string> OldCrewHistoryLine { get; set; } = default!;
    [Parameter] public Action OpenLogbookFromDeath { get; set; } = default!;
    [Parameter] public Action PayCompletedQuests { get; set; } = default!;
    [Parameter] public Action PresentTheBlackOpsKey { get; set; } = default!;
    [Parameter] public Func<Action, Task> PressAndRefocus { get; set; } = default!;
    [Parameter] public Action<bool, string> Say { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
