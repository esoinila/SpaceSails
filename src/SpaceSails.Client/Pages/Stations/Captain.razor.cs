using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Components;
using SpaceSails.Client.Layout;
using SpaceSails.Core;
using SpaceSails.Client.Pages;

namespace SpaceSails.Client.Pages.Stations;

// Captain — the code-behind for Captain.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of Captain.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class Captain
{
    [Parameter, EditorRequired] public ShipMission CurrentMission { get; set; } = ShipMission.Default;
    [Parameter, EditorRequired] public MissionOptions Options { get; set; } = new([], [], [], [], []);
    [Parameter] public EventCallback<ShipMission> OnSelectMission { get; set; }

    /// <summary>#963 follow-up — what killed the last voyage, if anything did. Null on a clean run.</summary>
    [Parameter] public SpaceSails.Core.CrashNote? LastError { get; set; }

    /// <summary>True once the note has been copied, so the button can say it took.</summary>
    [Parameter] public bool ErrorCopied { get; set; }

    [Parameter] public EventCallback OnCopyError { get; set; }
    [Parameter] public EventCallback OnDismissError { get; set; }
    [Parameter] public IReadOnlyList<DeskChips.ChipData> StatusChips { get; set; } = [];
    [Parameter] public EventCallback<ShipDesk> OnGoToDesk { get; set; }
    [Parameter] public bool ShotAuthorized { get; set; }
    [Parameter] public bool FireAtWill { get; set; }
    [Parameter] public EventCallback OnAuthorizeShot { get; set; }
    [Parameter] public EventCallback OnToggleFireAtWill { get; set; }

    /// <summary>Whether the standing WEAPONS TIGHT order is in force — bots and the tube gun stand down while it
    /// is, and the captain's own trigger is untouched (#538). READ-ONLY here: the remote is on the deck HUD (H),
    /// because the order is given on foot and not from a chair.</summary>
    [Parameter] public bool WeaponsTight { get; set; }

    /// <summary>Whether damage control holds the captain's standing authority over her compartments — the
    /// owner's "fire at will" for the other set of handles.</summary>
    [Parameter] public bool DamageControlAuthorized { get; set; }
    [Parameter] public EventCallback OnToggleDamageControlAuthority { get; set; }

    /// <summary>Whether the captain has cleared damage control's NEXT act — the limited pre-ok, spent by the
    /// act it permits. The exact shape of AUTHORIZE NEXT SHOT, for the other set of handles.</summary>
    [Parameter] public bool DamageControlNextActCleared { get; set; }
    [Parameter] public EventCallback OnAuthorizeNextDamageControlAct { get; set; }

    /// <summary>The one compartment armed by name at a board right now, if any. Read-only here: the word is
    /// given where the valve is, because it names the room it was given for.</summary>
    [Parameter] public string? ArmedCompartment { get; set; }

    /// <summary>Whether her bridge repeater still has a bus behind it. Star Trek's engineering/bridge
    /// duplication is the arrangement (the owner's own reference): NEITHER station locks the other out — the
    /// valves aft are mechanical and always answer, and the repeater answers while the bridge lives.</summary>
    [Parameter] public bool BridgeRepeaterAlive { get; set; } = true;
    // Reopens Map's start-point picker (playtest / "begin elsewhere" jump). Optional — the button
    // only shows when Map wires it.
    [Parameter] public EventCallback OnReopenStartPicker { get; set; }

    // The ship's logbook (#310): open the save/load surface (slots, banking, export/import). Optional —
    // the button only shows when Map wires it.
    [Parameter] public EventCallback OnOpenSaves { get; set; }

    // ── #948 · the captain's own name, editable from the chair. The desk holds no state of its own: Map
    //    owns the draft and the registry write (RenameCaptain), so the desk and the front-door roster card
    //    are two doors onto ONE act, and neither can drift from the other. ──

    /// <summary>The captain's display name — the renamed one, or the seeded roster name.</summary>
    [Parameter] public string CaptainName { get; set; } = "";

    /// <summary>True while THIS captain's name is being typed (Map's rename draft is open on this thread).</summary>
    [Parameter] public bool RenamingCaptain { get; set; }

    /// <summary>The name as typed so far.</summary>
    [Parameter] public string RenameDraft { get; set; } = "";

    /// <summary>Open the rename input.</summary>
    [Parameter] public EventCallback OnBeginRename { get; set; }

    /// <summary>Each keystroke's text.</summary>
    [Parameter] public EventCallback<string> OnRenameDraft { get; set; }

    /// <summary>Enter/Escape while typing the name.</summary>
    [Parameter] public EventCallback<Microsoft.AspNetCore.Components.Web.KeyboardEventArgs> OnRenameKeyDown { get; set; }

    /// <summary>Write the typed name onto the license.</summary>
    [Parameter] public EventCallback OnCommitRename { get; set; }

    /// <summary>Abandon the typed name.</summary>
    [Parameter] public EventCallback OnCancelRename { get; set; }

    // Tutorials tab (the picker used to float on the Nav map; starting a lesson is a captain's order).
    [Parameter] public IReadOnlyList<TutorialItem> Tutorials { get; set; } = [];

    /// <summary>Everything the crew have formed an opinion from. A plain aggregate of the voyage, so the
    /// sheet is a pure function of what actually happened.</summary>
    [Parameter] public CrewTemp.Voyage Voyage { get; set; }

    /// <summary>#1066 · Berths since the crew were last ashore. It reaches <see cref="Voyage"/> already,
    /// folded into <c>PromisesBroken</c> — but the sheet has to show the COUNTER and not only what it costs,
    /// because a warning the captain can only read backwards off a bar is not a warning (#761).</summary>
    [Parameter] public int WorkingStopsSinceShoreLeave { get; set; }

    [Parameter] public int ActiveTutorial { get; set; } = -1; // index of the in-progress lesson, -1 = none
    [Parameter] public EventCallback<int> OnStartTutorial { get; set; }
    // A one-shot request to open this desk straight on the lessons. (The Nav "New here? Learn the
    // ropes" banner that used to raise it was removed in #195; the hook is kept dormant for a future
    // tutorial mission to drive — see Map.Quests.cs _openCaptainToTutorials.)
    [Parameter] public bool OpenToTutorials { get; set; }

    // #124/#125 playtest: the desk component is re-created on every desk switch (see the note by
    // _view below), so the active tab used to reset to Orders each return. Map now OWNS the last
    // tab: it seeds this desk with InitialTab on creation and the desk reports every tab change
    // back through OnTabChanged, so "find the tab where I left it" holds across desk switches.
    [Parameter] public CaptainView InitialTab { get; set; } = CaptainView.Orders;
    [Parameter] public EventCallback<CaptainView> OnTabChanged { get; set; }

    /// <summary>One lesson on the Tutorials tab — title + one-line blurb. Map owns the actual steps.</summary>
    public sealed record TutorialItem(string Title, string Blurb);

    // Quests tab (go-ashore contracts, M-Q2): a read-only ledger of work picked up at haven bars.
    // Accepted at the table, tracked in Map, paid on docking — this desk is the crew's copy.
    [Parameter] public IReadOnlyList<QuestItem> Quests { get; set; } = [];

    /// <summary>Raised by a quest card's 🔭 button (Tuesday plan PR-A): the intel-fed fetch hunt is
    /// reachable from the quest card too, not just the Comms desk. Map aims the scope and jumps to
    /// Sensors.</summary>
    [Parameter] public EventCallback OnPointScope { get; set; }

    /// <summary>One contract on the Quests tab. <paramref name="StatusKind"/> is a css slug
    /// (active / complete / paid); Map owns the live quest state. <paramref name="Steps"/> is the
    /// staged plan (empty for quests without one); <paramref name="ShowScopeButton"/> surfaces the
    /// 🔭 "point the scope" hook while the fetch hunt is at its pre-scan stage.</summary>
    /// <remarks>#959 — <paramref name="Plain"/> is the verb / takes / effort block Core's
    /// <c>JobTerms.PlainBlock</c> computes, rendered ABOVE the flavour <paramref name="Detail"/>. Null or
    /// empty for a row Map could not measure; the card then simply shows the voice, as it always did.</remarks>
    public sealed record QuestItem(
        string Title, string Detail, string RewardText, string StatusLabel, string StatusKind,
        IReadOnlyList<QuestStep> Steps, bool ShowScopeButton, string? NextAction = null,
        IReadOnlyList<string>? Plain = null)
    {
        public QuestItem(string title, string detail, string rewardText, string statusLabel, string statusKind)
            : this(title, detail, rewardText, statusLabel, statusKind, [], false) { }
    }

    /// <summary>One line of a staged quest plan — an icon (✅ done / ▶ current / ▪ ahead) and its text.</summary>
    public sealed record QuestStep(string Icon, string Text);

    // The ledger's "Tips & intel" section (PR-J): every scope/route tip, projected by Map so this
    // desk only renders. A scope tip carries <paramref name="ScopeTipId"/> (its 🔭 action jumps to
    // Sensors with the scan queued); a route tip carries <paramref name="ShowDarkWeb"/> and, when the
    // ship is a known contact, <paramref name="DossierShipId"/> (the two "→" links onto Comms). A tip
    // with none of the three is background — "may matter later".
    [Parameter] public IReadOnlyList<LedgerTip> Tips { get; set; } = [];

    /// <summary>A scope tip's 🔭 button (by tip id): Map aims the prioritized scan and jumps to Sensors,
    /// exactly like the Comms intel card.</summary>
    [Parameter] public EventCallback<string> OnScopeTip { get; set; }

    /// <summary>A route tip's "→ dark web" link: Map switches to Comms and selects the dark-web node.</summary>
    [Parameter] public EventCallback OnRouteToDarkWeb { get; set; }

    /// <summary>A route tip's "→ dossier" link (by ship id): Map switches to Comms and selects the contact.</summary>
    [Parameter] public EventCallback<string> OnRouteToDossier { get; set; }

    /// <summary>One line in the ledger's Tips &amp; intel section. <paramref name="Lines"/> is the
    /// grounded numbers (scope) or the route in plain words (route); <paramref name="Provenance"/> is
    /// "&lt;giver&gt; · &lt;station&gt; · day N" or null when unknown. Exactly the actions that apply
    /// are non-null/true; a tip with no action renders as background.
    ///
    /// <para>#973 L1 · <paramref name="EntryId"/> and <paramref name="SimTime"/> are the FILING LINE's two
    /// facts about this row: an identity that survives the ledger being reassembled from six books every
    /// render, and the stamp that decides which side of the last filing it falls on. Null on the rows that
    /// have no date at all — the standing note, the two arc readouts — which is why those can never grey.
    /// <paramref name="Grey"/> is Map's answer for this captain: a page they don't remember writing, drawn
    /// with the mark and open to being read at.</para></summary>
    /// <para>#973 L5a · <paramref name="Filed"/> is the one exemption the filing line has: a page the
    /// SERVICE filed comes back whatever the policy did. Exactly one row in the game carries it (the
    /// summer-party page), and it carries it because that is what the world says about that page — the desk
    /// never decides it.</para>
    public sealed record LedgerTip(
        string Title, IReadOnlyList<string> Lines, string? Provenance,
        string? ScopeTipId, bool ShowDarkWeb, string? DossierShipId,
        string? EntryId = null, double? SimTime = null, bool Grey = false, bool Filed = false);

    /// <summary>#973 L1 · The captain sits down with a page they don't remember writing. Map rolls it once
    /// on the shared dice — it comes back, it comes back wrong, or nothing — and never again this life.</summary>
    [Parameter] public EventCallback<string> OnReadGreyPage { get; set; }

    /// <summary>#973 L1 · The mark a grey row wears (<c>FilingLine.Mark</c>), passed in rather than typed
    /// here so the glyph has one home.</summary>
    [Parameter] public string GreyPageMark { get; set; } = "";

    /// <summary>#973 L1 · What the mark means, spelled out on the row (<c>FilingLine.Label</c>).</summary>
    [Parameter] public string GreyPageLabel { get; set; } = "";

    /// <summary>#973 L5a · The captain's crossings, oldest first, already rendered as rows by Map
    /// (<c>CaptainCrossings.Row</c>). Rows rather than records because this desk renders and never derives —
    /// and because the one thing that must never appear here is a number summarising them.</summary>
    [Parameter] public IReadOnlyList<string> Crossings { get; set; } = [];

    /// <summary>#973 L5a · The section's heading (<c>CaptainCrossings.Heading</c>), passed in rather than
    /// typed here so the words have one home.</summary>
    [Parameter] public string CrossingsHeading { get; set; } = "";

    /// <summary>#223: the ledger's 🗺 treasure-maps section. Every buried chest we know of, ours and
    /// any rival's whose map we hold — clicking one throws its full-screen card back up (OnViewMap).</summary>
    [Parameter] public IReadOnlyList<CacheMapItem> Maps { get; set; } = [];

    /// <summary>Open the full-screen map card for a cache id.</summary>
    [Parameter] public EventCallback<string> OnViewMap { get; set; }

    /// <summary>#223: one line over the 🗺 section totalling what is OURS and underground — buried coin,
    /// buried units, and how many of those units are evidence. Empty when we have nothing in the ground
    /// (a rival's map is not a holding), and then the section shows nothing extra.</summary>
    [Parameter] public string HoardSummary { get; set; } = "";

    /// <summary>One map in the ledger's 🗺 section — the caption, the bearing line, the contents, and
    /// whose hoard it is. The cache id drives the "view the card" jump.
    ///
    /// <para>#455 · <paramref name="SafetyWord"/> and <paramref name="SafetyLine"/> are the hiding place's
    /// rung — "Exposed" / "Considered" / "Guarded" and its authored sentence — handed down ALREADY COMPUTED
    /// by the one oracle the return-trip discovery roll compares against. The desk never derives it; a second
    /// place deciding how safe a chest is is exactly the drift this issue exists to close.</para></summary>
    public sealed record CacheMapItem(
        string CacheId, string Caption, string Bearing, string Contents, string Owner, bool PlayerOwned,
        string SafetyWord = "", string SafetyLine = "");

    // PR-WIRE — the favor bank's accounts, projected by Map for the ledger's 💰 Accounts section.
    [Parameter] public IReadOnlyList<AccountRow> Accounts { get; set; } = [];

    /// <summary>One contact's bank standing for the ledger. <paramref name="Balance"/> is signed
    /// (+ = they hold our coin; − = we owe them); <paramref name="Channel"/> is the one-line "how they
    /// bank" hint; <paramref name="Transactions"/> is the recent passbook, newest first.</summary>
    public sealed record AccountRow(string DisplayName, long Balance, string? Channel, IReadOnlyList<string> Transactions);

    /// <summary>#973 L1 · A click anywhere on a ledger row. Only a GREY one does anything: it hands the entry
    /// id back to Map, which owns the roll, the nerve and the beat. A remembered row is an ordinary card and
    /// this is why it stays one.</summary>
    private Task ReadIfGrey(LedgerTip tip)
        => tip.Grey && tip.EntryId is { Length: > 0 } id ? OnReadGreyPage.InvokeAsync(id) : Task.CompletedTask;

    public enum CaptainView { Orders, Status, Tutorials, Ledger, Crew }
    private CaptainView _view;

    // The component is re-created each time you switch to the Captain desk. The banner's one-shot
    // "open on Tutorials" wins for that single arrival; otherwise we restore the last tab Map kept
    // for us (InitialTab), so a manual return lands where you left rather than always on Orders.
    protected override void OnInitialized()
    {
        _view = OpenToTutorials ? CaptainView.Tutorials : InitialTab;
    }

    // Set the active tab and let Map remember it (it survives this desk's re-creation).
    private async Task SetView(CaptainView view)
    {
        _view = view;
        await OnTabChanged.InvokeAsync(view);
    }

    private async Task OnStatusKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e, ShipDesk desk)
    {
        if (e.Key is "Enter" or " ")
        {
            await OnGoToDesk.InvokeAsync(desk);
        }
    }

    private async Task OnTutorialKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e, int index)
    {
        if (e.Key is "Enter" or " ")
        {
            await OnStartTutorial.InvokeAsync(index);
        }
    }

    private bool IsSelected(ShipMission m) => CurrentMission == m;

    private Task Pick(ShipMission m) => OnSelectMission.InvokeAsync(m);

    private async Task OnCardKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e, ShipMission m)
    {
        if (e.Key is "Enter" or " ")
        {
            await Pick(m);
        }
    }
}
