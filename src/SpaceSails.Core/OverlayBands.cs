namespace SpaceSails.Core;

/// <summary>
/// The HUD's z-index scale, as ONE source of truth (#299). Lab 34 (#293) proved the rescue lifeline is
/// reachable by measuring geometry, but flagged its own weakness: the registry hand-transcribed z-indexes
/// from <c>Map.razor.css</c>, so an un-mirrored CSS edit escaped the gate. This class closes that gap.
///
/// <para><b>The band scale.</b> Every overlay the Map paints lives in one of five ordered bands, from the
/// base map at the floor to the modal dialogs at the ceiling. The distress lifeline sits in its own
/// reserved band ABOVE every routine chrome/desk/pop-up overlay and just below the rescue modal it opens —
/// so a stranded captain's one button is, by construction, the last thing anything routine may paint over.</para>
///
/// <para><b>How it stays honest.</b> The same integer values are mirrored in <c>wwwroot/css/app.css</c> as
/// the <c>:root</c> custom properties <c>--z-base-map … --z-modal</c>, and every overlay in
/// <c>Map.razor.css</c> sets its <c>z-index</c> to <c>var(--z-BAND)</c> (or <c>calc(var(--z-BAND) + N)</c>
/// for a fine layer within a band). <c>CssZBandSyncTests</c> parses both stylesheets and fails the build
/// the moment a value drifts from a constant here — so a CSS edit that would bury a critical control goes
/// red in <c>dotnet test</c>, not in a playtest. No hand-transcription survives unchecked.</para>
///
/// <para>Named members below are each overlay's EXACT z-index, expressed as <c>band + offset</c> so the
/// ordering law is explicit in the source. Local, intra-component z-indexes (a readout stacked inside its
/// own gauge, a silhouette inside the busted art) are NOT part of this scale — they never leave their
/// parent's stacking context, so they cannot occlude a HUD-level control and are left as literals.</para>
/// </summary>
public static class OverlayBands
{
    // ---- The five ordered bands (the coarse scale; mirrored in app.css :root) --------------------

    /// <summary>The live map canvas and the readouts painted directly on it — the floor of the stack.</summary>
    public const int BaseMap = 0;

    /// <summary>The HUD furniture layered over the map: toolbars, panels, context menus, desk shields,
    /// the loading splash. The routine chrome a captain works through in flight.</summary>
    public const int MapChrome = 10;

    /// <summary>Raised desk/deck cards, toasts, pin/start pickers and object views — pop-ups that come
    /// and go over the chrome but must never out-rank the distress lifeline.</summary>
    public const int DesksAndPopups = 1200;

    /// <summary>The reserved lifeline band: the rescue affordance a stranded, dry-tank captain presses.
    /// Above every routine overlay the state machine can raise, below only the rescue modal it opens.</summary>
    public const int DistressLifeline = 1340;

    /// <summary>Full-screen modal dialogs that own the screen while up — the rescue offer, the long-haul
    /// jump theatre, the celebration, the BUSTED interrupt. The ceiling of the stack.</summary>
    public const int Modal = 1360;

    // ---- MapChrome members (each = MapChrome + a fine offset that preserves today's exact value) ---

    /// <summary><c>.map-dest-panel</c> — the navigation-target box (Map.razor.css).</summary>
    public const int MapDestPanel = MapChrome + 2;   // 12

    /// <summary><c>.desk-layer</c> — the full-screen desk shield behind a station's chrome.</summary>
    public const int DeskLayer = MapChrome + 5;      // 15

    /// <summary><c>.map-body-menu</c> — the planet/ship/sky context menu (viewport-clamped, #253).</summary>
    public const int MapBodyMenu = MapChrome + 10;   // 20

    /// <summary><c>.map-dossier</c> — the target dossier / armed-capture panel.</summary>
    public const int MapDossier = MapChrome + 10;    // 20

    /// <summary><c>.map-hud</c> — the Nav toolbar carrying dock/undock, pause, long-haul, autopilot.</summary>
    public const int MapHud = MapChrome + 12;        // 22

    /// <summary><c>.map-topstack</c> — the masthead / tabs / alert strip stack.</summary>
    public const int MapTopstack = MapChrome + 14;   // 24

    /// <summary><c>.parrot-perch</c> — the comms parrot perch.</summary>
    public const int ParrotPerch = MapChrome + 16;   // 26

    /// <summary><c>.deck-view-toggle</c> — the deck/map view switch.</summary>
    public const int DeckViewToggle = MapChrome + 30; // 40

    /// <summary><c>.map-layers</c> — the layers control.</summary>
    public const int MapLayers = MapChrome + 30;     // 40

    /// <summary><c>.map-loading</c> — the in-map loading splash.</summary>
    public const int MapLoading = MapChrome + 40;    // 50

    // ---- DesksAndPopups members ------------------------------------------------------------------

    /// <summary><c>.deck-pulse-toast</c> — a transient deck toast.</summary>
    public const int DeckPulseToast = DesksAndPopups + 0;   // 1200

    /// <summary><c>.deck-offer-card</c> — a raised deck offer card.</summary>
    public const int DeckOfferCard = DesksAndPopups + 50;   // 1250

    /// <summary><c>.deck-shuttle-card</c> — the shuttle-bay card.</summary>
    public const int DeckShuttleCard = DesksAndPopups + 50; // 1250

    /// <summary><c>.start-picker-backdrop</c> — the starting-berth picker gate.</summary>
    public const int StartPickerBackdrop = DesksAndPopups + 100; // 1300

    /// <summary><c>.view-object-backdrop</c> — the object-view pop-up.</summary>
    public const int ViewObjectBackdrop = DesksAndPopups + 120;  // 1320

    /// <summary><c>.pin-backdrop</c> — the PIN pad pop-up.</summary>
    public const int PinBackdrop = DesksAndPopups + 120;    // 1320

    // ---- DistressLifeline member -----------------------------------------------------------------

    /// <summary><c>.map-adrift</c> — the reserved distress-lifeline container (the rescue reopen pill).</summary>
    public const int MapAdrift = DistressLifeline + 0;      // 1340

    // ---- Modal members ---------------------------------------------------------------------------

    /// <summary><c>.rescue-backdrop</c> — the rescue-offer modal the lifeline opens.</summary>
    public const int RescueBackdrop = Modal + 0;            // 1360

    /// <summary><c>.mission-celebration-backdrop</c> — the mission-complete celebration.</summary>
    public const int MissionCelebrationBackdrop = Modal + 40; // 1400

    /// <summary><c>.jump-overlay</c> — the diegetic long-haul jump theatre.</summary>
    public const int JumpOverlay = Modal + 40;             // 1400

    /// <summary><c>.busted-backdrop</c> — the BUSTED interrupt (out-ranks even the celebration).</summary>
    public const int BustedBackdrop = Modal + 50;          // 1410
}
