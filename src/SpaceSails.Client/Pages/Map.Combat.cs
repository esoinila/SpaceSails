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

// Map.Combat — the War room: the mass driver and fire control, ordnance in flight, the heat
// that draws hunters, boarding and plunder, the BUSTED reckoning, and running dark. No home in
// #251's original twelve — it earned its own cabinet. Motion only.

/// <summary>
/// #251 · THE HEAT ITSELF, AND EVERYTHING THIS FAMILY REMEMBERS. What raises heat, what cools it, what
/// counts as hot in the hold, the transponder the ship runs under, and the two rows the War Room desk
/// reads off the traffic.
///
/// <para>The rest of the family is three siblings, each named for the moment it owns: <c>.Hunters</c>
/// (a hunter spawned, warned, bribed, fired across the bow, and finally taken off you), <c>.Pursuit</c>
/// (the once-a-frame encounter loop and the plotted state it chases), and <c>.Draw</c> (the beacon
/// ghost, the two rings, the reticle and its brackets).</para>
///
/// <para><b>Every field of the family stays here, in the order the one file declared it</b> — the
/// instance state AND the four static colours/callsign tables. A <c>static readonly</c> moved into a
/// sibling partial is initialised in FILE order (this repo's sixth named bug class, #1163/#1175), and
/// the frame ledger is the only thing that would have caught it. Only method bodies moved.</para>
/// </summary>
public partial class Map
{
    private static string HeatFlames(int level) => level switch
    {
        <= 0 => "◌",
        1 => "🔥",
        2 => "🔥🔥",
        _ => "🔥🔥🔥",
    };

    // PR-7: the gun deck — heat, hunters, war-room (vision par. 18).
    private HeatState _heat = HeatState.None;
    private readonly List<HunterState> _hunters = [];
    private int _hunterSeq;

    // PR-BUSTED: the catch economy of consequence (ruling §5). Hot cargo is stamped at theft time
    // when heat > 0 and launders when heat cools to 0; the confiscation reads this book. The parrot
    // quotes the current exposure at each upward heat crossing (_lastAnnouncedHeat is the edge). One
    // purchasable dice helper is shipped to prove the modifier seam (owner: many small helpers later).
    private readonly HotCargoLedger _hotCargo = new();
    private int _lastAnnouncedHeat;
    // #380 item 1 (audit's cheapest half): pre-seed the resurrection fiction one beat EARLIER. The first
    // time heat reaches 1 in a run, a one-time pulse advertises the brain-backup / pirate-insurance premise
    // BEFORE the death card ever needs it. One latch, run-scoped (this component is the game session).
    private bool _heatInsuranceAdvised;
    // #422 arc 2 — how many times this SESSION the captain has woken from a brain-backup. Gates the clinic's
    // "second page" (clinic-ledger surfaces only once you've woken here before — the fragment's own fiction).
    // Run-scoped; the durable "woken before" truth is the thread's retired-captains count, checked alongside.
    private int _rebirthsSeen;
    private bool _hasNetJammer;                       // "Boarding-nets jammer" — +2 on resist initiative
    private const int NetJammerPriceCr = 350;
    private BustedEncounter? _busted;                 // the open BUSTED pop-up, null when free

    // Rebirth taxes & the insurance seam (issues #227 + #225): resurrection CONSULTS this policy through
    // InsuranceRule today, so #227's vendor lane never reopens the catch code. Ships as Uninsured — the
    // rustbucket + full clinic bill. Plain/JSON-friendly for the #225 save vault.
    private PirateInsurance _insurance = PirateInsurance.Uninsured;
    private double _hiddenAtHavenSinceSimTime = double.NaN; // NaN = not currently hidden
    private static readonly RgbaColor HunterColor = new(255, 90, 90);
    private static readonly string[] HunterCallsigns =
        ["Debt Collector", "The Adjuster", "Repo Barque", "Fair Warning", "Lien Enforcer", "Underwriter's Claw"];
    private const int CaptureWarpCap = 10;            // the 60 s window must be holdable

    private double _captureProgress;                   // boarding progress fraction [0,1)
    private bool _captureEngaged;
    private string? _captureTargetCallsign;
    private double _captureRequiredSeconds;            // wall-clock secs for the CURRENT pass geometry

    // ---- M29: the transponder (the AIS of the solar lanes) ----
    private TransponderMode _transponderMode = TransponderMode.On; // honest traffic runs lit
    private ShipState? _beaconGhost;

    private void SetTransponder(TransponderMode mode)
    {
        if (mode == _transponderMode)
        {
            return;
        }

        // Entering FAKE snapshots the innocent course at the moment of the lie; the ghost
        // coasts it from here. Leaving FAKE burns the ghost.
        _beaconGhost = mode == TransponderMode.Fake ? _ship : null;
        _transponderMode = mode;
        switch (mode)
        {
            case TransponderMode.Dark:
                SquawkNow(Parrot.Squawk.RunningDark, _lastTimestampMs ?? 0, force: true);
                break;
            case TransponderMode.Fake:
                SquawkNow(Parrot.Squawk.FalseColors, _lastTimestampMs ?? 0, force: true);
                break;
        }

        StateHasChanged();
    }

    // ---- PR-7: the gun deck — encounters, heat, hunters ----

    // Whether a boarding gets the compliance speed bonus: bribed always qualifies (an inside job
    // needs no warning shot); otherwise the target must have been warned AND actually be the
    // compliant type — a stubborn ship never heaves to, warned or not. Pods have no crew to
    // comply at all.
    private bool IsCompliantBoarding(NpcState npc)
    {
        if (npc.Ship.IsPod)
        {
            return false;
        }

        if (npc.Bribed)
        {
            return true;
        }

        return npc.WarningShotFired && EncounterRule.ComplianceOf(npc.Ship, _heat.Level) == ComplianceState.Compliant;
    }

    // Robbing a ship is what actually raises heat — the warning shot only narrates the ship's
    // reaction (heave to vs. call for help); a bribed ship pays for silence, so no heat at all.
    private void RaiseHeatFromRobbery(NpcState npc)
    {
        if (npc.Ship.IsPod || npc.Bribed)
        {
            return;
        }

        ComplianceState compliance = EncounterRule.ComplianceOf(npc.Ship, _heat.Level);
        int amount = compliance == ComplianceState.Stubborn ? 2 : 1;
        _heat = EncounterRule.RaiseHeat(_heat, amount, SimTime);
        PushNewsEvent(NewsWire.NewsEventKind.RobberyCommitted, npc.Ship.Callsign);
        // #962: the hull we just took is the WRIT. This method had that name in hand and threw it away at
        // the spawn call, which is why "why is this still hunting us" had no answer on any screen — a
        // collector could name herself and nothing else. She carries the job now.
        SpawnHunterForHeatEvent(npc.Ship.Callsign);
        ShowPulseMessage(compliance == ComplianceState.Stubborn
            ? "Her muscle's already inbound. Heat rising fast."
            : "Word travels. Heat rising.");
    }

    // Hidden at a haven (vision par. 18): either bound in orbit around a haven MOON, or CLAMPED in
    // the dock of a haven STATION (the mass-less grey-market docks have no Hill sphere to orbit —
    // you berth at them instead). Both cool heat 4x and, held long enough, break a hunter's pursuit.
    private bool IsHiddenAtHaven()
    {
        if (_nearestBody is not { IsHaven: true } haven)
        {
            return false;
        }

        // Clamped in this haven's dock — held fast, lying low (no orbit to bind, so short-circuit).
        if (_dockedHavenId == haven.Id)
        {
            return true;
        }

        // A haven moon: bound in its Hill sphere the ordinary way.
        if (_ephemeris is null || haven.ParentId is null)
        {
            return false;
        }

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == haven.ParentId)
            {
                parent = candidate;
                break;
            }
        }

        if (parent is null)
        {
            return false;
        }

        double hill = OrbitRule.HillRadius(haven, parent.Mu);
        return OrbitRule.IsBound(_ship, _nearestBodyPosition, _nearestBodyVelocity, haven, hill);
    }

    // Is a cargo class hot (stolen)? Today the loot ledger is the evidence of a heist — a class we've
    // ever boarded reads as hot in the hold. The BUSTED lane owns the authoritative per-unit flag; this
    // is the honest read HOARD has until then (the seam both agree on: hot = stolen-flagged).
    private bool IsHotClass(string cargoClass) =>
        _lootLedger.Any(l => string.Equals(l.CargoClass, cargoClass, StringComparison.OrdinalIgnoreCase));

    // Hot units currently in the hold (what confiscation would see as evidence — until it's buried).
    private int HotHoldUnits() => _cargoByClass.Where(kv => IsHotClass(kv.Key)).Sum(kv => kv.Value);

    // #380 item 9 (owner ruling 2026-07-19: "new players are left mystified") — the 🔥 hot-cargo flag
    // rode the confiscation and rescue manifests unglossed. Hung as a hover title on the flag wherever it
    // appears (a one-time pulse is awkward inside the manifest markup), so its first sight explains it.
    private const string HotGlossTitle = "🔥 hot = taken under heat — collectors seize it in full, fences launder it.";

    // #202: the crimes' books — a loot line per completed boarding, newest first, projected into the
    // Captain's ledger alongside the honest autopilot receipts (the established tip idiom).
    private readonly List<LootRecord> _lootLedger = [];

    // ---- Pursuit steering by the quantum trail (aim-solution follow-up, 2026-07-06) ----
    // At warp a frame spans hundreds of sim-seconds, and the old catch-up steered EVERY hunter
    // quantum toward the single frame-end player position — so hunter paths depended on frame
    // cadence (not sim-deterministic; against the working agreement) and a long fire-control
    // prediction chased a target no model could reproduce. The trail records the ship's actual
    // integrated positions through the frame at the pursuit cadence; steering looks up the
    // position AT each quantum's time. Residual frame dependence is only interpolation sag
    // between 60 s knots (~km) — was tens of thousands of km at 10000x.
    //
    // ABORT SWITCH: set false to restore the old frame-end steering exactly (one flag, no other
    // code path touched) if playtesting turns up trouble.
    private const bool SteerHuntersByQuantumTrail = true;
    private readonly List<TrajectorySample> _pursuitTrail = [];

    private static readonly RgbaColor DriverReachColor = new(120, 210, 255, 170);

    private static readonly RgbaColor ReticleColor = new(255, 70, 70);
    private static readonly RgbaColor TrackBracketColor = new(150, 255, 210, 200);

    // Thin, read-only projections for the war-room — it never sees Map.razor's private NpcState
    // or HunterState, only the NpcShip/live-state pairs it needs to render (mirrors TrackingCandidates()).
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.Contact> WarRoomContacts()
    {
        var contacts = new List<SpaceSails.Client.Pages.Stations.WarRoom.Contact>();
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Active && !npc.Arrived && !npc.Boarded && npc.CurrentlyObserved)
            {
                contacts.Add(new SpaceSails.Client.Pages.Stations.WarRoom.Contact(
                    npc.Ship, npc.State, npc.WarningShotFired, npc.Bribed));
            }
        }

        return contacts;
    }

    private IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.HunterContact> WarRoomHunters()
    {
        var hunters = new List<SpaceSails.Client.Pages.Stations.WarRoom.HunterContact>(_hunters.Count);
        foreach (HunterState hunter in _hunters)
        {
            // #962: the war room states the same terms the dossier does, off the same Core sentences —
            // it is the other desk a captain is staring at while a collector closes.
            HuntTerms terms = TermsOfTheHunt(hunter);
            hunters.Add(new SpaceSails.Client.Pages.Stations.WarRoom.HunterContact(
                hunter.Id, hunter.Callsign, hunter.State, terms.Warrant, terms.Hiding, terms.Nerve));
        }

        return hunters;
    }
}
