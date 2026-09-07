using System.Globalization;
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
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE CLAMP — match-and-berth, undock, the dock affordance and reach envelope, and the shuttle runs that
/// ferry the last mile. Moved out of <c>Map.razor</c> for #251.
///
/// <para>#251 · THIS FILE IS THE BERTH'S STATE AND WHAT THE HUD SAYS ABOUT IT — whether she is docked, to
/// what, and the sentences the nav target reads. The rest is six siblings, each named for the moment it
/// owns: <c>.Run</c> (the little boat crossing to a prize, and the two things drawn for it), <c>.Clamp</c>
/// (the affordance, the reach envelope and the deferred fuel tab), <c>.Board</c> (the destination board and
/// the landables just beyond reach), <c>.Boarding</c> (the panel: site, coin, sentries), <c>.Crossing</c>
/// (the door IS the flight, and the loiter clock that must not run while she does not), and <c>.Berth</c>
/// (match, clamp, and the shove off).</para>
///
/// <para>The one static field in the family — <c>DockedStarts</c> — is declared HERE, in the opening file,
/// and reads nothing but literals. See <c>NoPartialClassSpreadsItsStaticFieldsTests</c> for why that is not
/// a coincidence.</para>
/// </summary>
public partial class Map
{

    // M6 additions — piracy economy, capture, dock, tutorial
    // A market body's "port zone" (0.067 AU). Deliberately generous: with ±10% prograde-only
    // pulses, *returning* to a body you've left is the hardest maneuver in the game — offline
    // search showed post-capture orbits graze 5.1e9 from Earth for weeks without re-threading
    // anything tighter. The player spawns inside Earth's zone (fence available immediately —
    // natural onboarding), and the Luna-pod tutorial hunt completes within it; the real hunts
    // (He3 haulers) still demand full interplanetary voyages between zones.
    private const double DockRadiusMeters = 1e10;

    private bool _docked;
    private string? _dockBodyId;
    private ShuttleFlightView? _shuttleView;
    private ShuttleFlightView.Run? _shuttleRun;
    private NpcState? _shuttleTarget;

    // The berth we're tied up at, for a tip's provenance line; "ashore" when not docked to a named body.
    private string DockedStationName() =>
        _dockedHavenId is { } id && _ephemeris is not null
            ? _ephemeris.Bodies.FirstOrDefault(b => b.Id == id)?.Name ?? id
            : "ashore";

    // Nav commands that begin flight are refused while clamped to a station: the berthing arm
    // holds the ship fast (HoldAtDock pins it back onto the dock every tick), so an approach or
    // insertion burn would just fight the clamp — Undock first (issue #126). NB the OTHER dock
    // flag, _docked, is mere market proximity (within DockRadiusMeters of a trade body): the ship
    // still flies freely there, so it is deliberately NOT a nav lock. Only the clamp locks.
    private bool NavLockedByDock => _dockedHavenId is not null;
    private const string DockNavLockTip = "Undock first — you're clamped to the station";

    // Returns true (and toasts) when a flight command must be refused because we're clamped on.
    private bool RejectNavWhileDocked()
    {
        if (!NavLockedByDock)
        {
            return false;
        }
        ShowPulseMessage($"⚓ {DockNavLockTip}.");
        return true;
    }

    // Approach coaching for a mass-less haven dock — no orbit, you clamp on (the dock mirror of
    // OrbitStatusLine). Keyed off the same DockReachMeters/DockMatchSpeedMps gates as the ⚓ button.
    // #200: the four sentences moved to Core (DockFocus) — unchanged in wording — so the focus panel's
    // verdict and this line are one copy of one telling, not two that can drift.
    private string DockStatusLine(OrbitAssistInfo oi)
    {
        if (_dockedHavenId == oi.Body.Id) return DockFocus.ClampedOnLine;
        if (oi.Distance > DockReachMeters) return DockFocus.CoastCloserLine();
        // #213: in range but too hot — the ⚓ Match & clamp button flies the terminal match; the line
        // names the same act instead of the old "slow it down yourself, then hit ⚓ Dock".
        if (oi.RelSpeed > DockMatchSpeedMps) return DockFocus.MatchClampLine;
        return DockFocus.ClampNowLine;
    }

    // ---- #200 · THE DOCKING FOCUS PANEL ----
    // Owner: "I want to see CLEARLY how close I am to docking distance and speed limits… Own pop-up of the
    // numbers to hit, just like in piracy-hold." The piracy model is the autosteal criterion box (Map.razor,
    // the prey dossier): one row per gate, reading vs required, green inside. These two members are the
    // panel's whole data path — and both go through DockFocus, which reads the ALREADY-RESOLVED affordance
    // and DockRule's own constants. Nothing about the panel is computed a second time here, so the rows
    // cannot claim "inside" about a number the clamp refuses.

    // The panel is on the glass exactly when docking is the live intent — a dock haven armed or chosen as
    // the destination, or the clamp in (latched) range of one. Never while already clamped on: the
    // affordance is Hidden there (UpdateDockAffordance) and the 🚀 Undock button owns that moment.
    private bool DockFocusLive => DockFocus.IsLive(_dockAffordance);

    // The tank the panel quotes is the tank the affordance was JUDGED against (#268: pulses already on a
    // pending match tab are spoken for), so the "match burn" row and the ⚓ button's own refusal agree.
    private int EffectiveDockTankPulses => Math.Max(0, _reactionMassPulses - _matchLedger.Pulses);

    // Start ids that begin already DOCKED at a station, mapped to the station body. Owner ruling
    // (2026-07-18 playtest, verbatim): "All starting points should be the docked positions… We walk to
    // a ship either from airlock or arrive by shuttle. We have no jobs ready if we start from space
    // already." — so every start clamps onto a haven, never a free-flying "fresh out of Earth orbit"
    // spawn, and NEVER Earth by default ("never ever Earth as default… we came to space to avoid
    // thinking about Earth"). Selene Gate, in Luna's orbit, is the cislunar tutorial home that replaces
    // the old free-flying Earth spawn — the Luna compute-core pod the first lesson chases launches right
    // there. Every start routes through the one shared clamp (ApplyStart → StartDockedAtHaven →
    // ClampOntoHaven), which welds a walkable interior where the haven has one and otherwise sits the
    // ship on the Nav map, clamped on. Friendly aliases (jupiter → the Red Eye, saturn → Ringside) let a
    // gas-giant start still mean "docked at that giant's berth." A new interior station is a scenario
    // body + a HavenInterior spec + one line here.
    private static readonly Dictionary<string, string> DockedStarts = new()
    {
        ["earth"] = "selene-gate",          // the fallback/tutorial home — Luna orbit, NOT free-flying Earth
        ["selene-gate"] = "selene-gate",
        ["cinder-roost"] = "cinder-roost",
        ["space-bar"] = "the-space-bar",
        ["jupiter"] = "red-eye",
        ["red-eye"] = "red-eye",
        ["saturn"] = "ringside-exchange",
        ["ringside"] = "ringside-exchange",
        ["the-tilt"] = "the-tilt",
        ["the-deep"] = "the-deep",
    };

    // The cislunar tutorial home: the berth a brand-new captain casts off from (in place of the retired
    // free-flying Earth spawn). The first-hunt lesson's Luna pod is seeded here on acceptance.
    private const string TutorialHomeHavenId = "selene-gate";
}
