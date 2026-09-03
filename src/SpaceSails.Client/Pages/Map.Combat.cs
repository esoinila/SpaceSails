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
public partial class Map
{

    private static string HeatFlames(int level) => level switch
    {
        <= 0 => "◌",
        1 => "🔥",
        2 => "🔥🔥",
        _ => "🔥🔥🔥",
    };

    private double? NearestHunterDistance()
    {
        double? best = null;
        foreach (HunterState hunter in _hunters)
        {
            double d = (hunter.State.Position - _ship.Position).Length;
            if (best is null || d < best)
            {
                best = d;
            }
        }

        return best;
    }

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

    // M29: the fake beacon's ghost, shown to US so the captain always knows what story the
    // beacon is telling — a hollow marker flying the abandoned course.
    private void DrawBeaconGhost()
    {
        if (_transponderMode != TransponderMode.Fake || _beaconGhost is not { } ghost
            || !LayerVisible("traffic.beacons")) // 🗺 Layers (#405): Traffic → Beacons
        {
            return;
        }

        (float gx, float gy) = _camera.WorldToScreen(ghost.Position);
        if (gx < -40 || gy < -40 || gx > _viewportWidth + 40 || gy > _viewportHeight + 40)
        {
            return;
        }

        _renderer!.DrawCircle(gx, gy, 6, null, GhostShipColor, 1.5f);
        _renderer.DrawText(gx + 9, gy + 3, "🎭 beacon ghost", GhostShipColor, "10px monospace", TextAlign.Left);
    }

    /// <summary>Clicking a hunter on the map locks it as the war room's interest target — the same
    /// lock as the War Room 🎯 button (corner brackets, a firing solution, a warning shot that
    /// breaks its nerve). Clicking the locked hunter again clears the lock.</summary>
    private void MarkHunterOfInterest(string hunterId)
    {
        bool wasLocked = _interestTargetId == hunterId;
        SetInterestTarget(hunterId); // toggles; also nulls the stale intercept and re-scans the pass
        string name = _hunters.FirstOrDefault(h => h.Id == hunterId).Callsign ?? "the hunter";
        ShowPulseMessage(wasLocked
            ? $"Lock released — {name} is no longer the war room's mark"
            : $"🎯 {name} marked — the war room has the fire-control lock; a warning shot will test its nerve");
    }

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

    // One hunter per heat event, fitting out at the nearest policed body (Earth/Mars-like —
    // never a haven). A pure outer-reaches scenario with nothing policed in range simply sends
    // no muscle — there's no cavalry to call.
    private void SpawnHunterForHeatEvent(string? warrant = null)
    {
        if (_ephemeris is null)
        {
            return;
        }

        CelestialBody? origin = EncounterRule.NearestPolicedBody(_ephemeris, _ship.Position, SimTime);
        if (origin is null)
        {
            return;
        }

        Vector2d originPosition = _ephemeris.Position(origin.Id, SimTime);
        const double h = 1.0;
        Vector2d originVelocity = (_ephemeris.Position(origin.Id, SimTime + h) - _ephemeris.Position(origin.Id, SimTime - h)) / (2 * h);

        string callsign = HunterCallsigns[_hunterSeq % HunterCallsigns.Length];
        string id = $"hunter-{_hunterSeq++}";
        _hunters.Add(EncounterRule.SpawnHunter(id, callsign, origin.Id, originPosition, originVelocity, SimTime, warrant));
        PushNewsEvent(NewsWire.NewsEventKind.HunterDispatched, callsign, origin.Name);
        // #380 item 5 (owner ruling 2026-07-19: "new players are left mystified") — the robbery bought
        // this hunter, but the fit-out delay meant muscle appeared days later with no causal link. This
        // pulse draws the chain in-voice the moment the collector is spawned; the callsign rides the news
        // headline behind it.
        //
        // #761 · …AT A RANK THAT CANNOT LOSE IT. The line is unchanged; what changes is that a hull
        // temperature or a fuel reading written in the same breath can no longer stand on top of it. This
        // is the sentence #380 item 5 exists to deliver — the causal chain from the robbery to the muscle
        // that arrives days later — and a captain who does not read it is the mystified new player that
        // ruling was about, whatever the news wire says at a desk he has not walked to yet. Somebody is
        // now coming for this ship: it changes what he can do, which is the whole of what Telling.Floor
        // means.
        ShowPulseMessage($"Word's out — your last job bought you a collector ({callsign}). It's fitting out at {origin.Name}; days, not weeks.", Telling.Floor);
    }

    private void FireWarningShot(string npcId)
    {
        NpcState? npc = FindNpc(npcId);
        if (npc is null)
        {
            // Not a freighter — it's the hunter itself. A warning shot erodes a collector's nerve.
            WarnHunter(npcId);
            return;
        }

        if (npc.Ship.IsPod || !EncounterRule.InWeaponRange(_ship, npc.State))
        {
            return;
        }

        if (_slugAmmo <= 0)
        {
            ShowPulseMessage("No slugs left for a warning shot — buy dockside");
            return;
        }

        _slugAmmo--;
        npc.WarningShotFired = true;

        // M28: the warning shot is a REAL slug now — flung wide on purpose (AcrossTheBow
        // rounds never hit-check) but genuinely in flight on the map. Same reaction rules.
        Vector2d toTarget = (npc.State.Position - _ship.Position).Normalized();
        var wide = new Vector2d(-toTarget.Y, toTarget.X) * 0.03;
        FireOrdnance(OrdnanceKind.Slug, (toTarget + wide).Normalized(), MaxMuzzleSpeed,
            npc.Ship.Id, acrossTheBow: true);

        ComplianceState compliance = EncounterRule.ComplianceOf(npc.Ship, _heat.Level);
        ShowPulseMessage(compliance == ComplianceState.Stubborn
            ? $"WARNING SHOT ACROSS THE BOW — {npc.Ship.Callsign} answers with a tight-beam call for help, not her colours"
            : "WARNING SHOT ACROSS THE BOW — she heaves to");
        RendererInterop.PlayCue("pulse");

        // Second hunt, step 2: the warning shot teaches that a stubborn hull won't heave to — she
        // calls muscle instead, so the soft path is a dead end and the gun is the only way in.
        if (npcId == TrafficSchedule.StarterFreighterId)
        {
            AdvanceTutorial(StepWarnFreighter);
        }
    }

    // A warning shot flung across a Debt Collector's bow: each one erodes its nerve. Most peel off
    // (coast, stop closing) for a stretch that grows with every shot; enough of them and the
    // collector voids the contract for good. A rare "La Dolce Vita" sort quits at the very first.
    private void WarnHunter(string hunterId)
    {
        int index = _hunters.FindIndex(h => h.Id == hunterId);
        if (index < 0)
        {
            return;
        }

        HunterState hunter = _hunters[index];
        if (hunter.CaughtPlayer || hunter.BrokenOff || !EncounterRule.InWeaponRange(_ship, hunter.State))
        {
            return;
        }

        if (_slugAmmo <= 0)
        {
            ShowPulseMessage("No slugs left for a warning shot — buy dockside");
            return;
        }

        _slugAmmo--;

        // A real slug flung wide (AcrossTheBow rounds never hit-check, so its sail is never holed).
        Vector2d toTarget = (hunter.State.Position - _ship.Position).Normalized();
        var wide = new Vector2d(-toTarget.Y, toTarget.X) * 0.03;
        FireOrdnance(OrdnanceKind.Slug, (toTarget + wide).Normalized(), MaxMuzzleSpeed,
            hunter.Id, acrossTheBow: true);
        RendererInterop.PlayCue("pulse");

        bool goodLifeFirstShot = hunter.WarningShotsTaken == 0
            && EncounterRule.PrefersTheGoodLife(hunter.Id, _heat.Level);
        HunterState after = EncounterRule.WarnOff(hunter, _heat.Level, SimTime);

        if (after.BrokenOff)
        {
            // Gave up. Remove it here so the generic "loses your scent" path in StepEncounters
            // doesn't also fire with the wrong flavor.
            _hunters.RemoveAt(index);
            if (_interestTargetId == hunterId)
            {
                _interestTargetId = null;
            }

            ShowPulseMessage(goodLifeFirstShot
                ? $"⚠ {hunter.Callsign} watches the slug drift past, shrugs, and turns for the nearest cantina — la dolce vita 🍸"
                : $"⚠ {hunter.Callsign} has had enough — she sheers off and voids the contract");
            PushNewsEvent(NewsWire.NewsEventKind.HunterBrokeOff, hunter.Callsign, _nearestBody?.Name);
            SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        }
        else
        {
            _hunters[index] = after;
            double peelDays = (after.PeeledUntilSimTime - SimTime) / 86400.0;
            string nerve = after.WarningShotsTaken switch
            {
                1 => "wavers",
                2 => "is rattled",
                _ => "is losing her nerve",
            };
            ShowPulseMessage($"⚠ WARNING SHOT — {hunter.Callsign} {nerve} and sheers off (peels away ~{peelDays:0.#} d)");
            SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        }
    }

    private void BribeShip(string npcId)
    {
        NpcState? npc = FindNpc(npcId);
        if (npc is null || npc.Bribed || npc.Ship.IsPod)
        {
            return;
        }

        int price = EncounterRule.BribePrice(npc.Ship);
        if (_credits < price)
        {
            ShowPulseMessage("Not enough credits to grease this crew.");
            return;
        }

        _credits -= price;
        npc.Bribed = true;
        ShowPulseMessage($"{npc.Ship.Callsign}'s crew take the coin — an inside job, quiet as the void.");
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

    /// <summary>The player state a pursuit quantum steers at: position interpolated on this
    /// frame's trail, falling back to the live ship outside it (or with the switch off). The
    /// velocity stays the frame-end ship's — AdvanceHunter only reads it for the catch check's
    /// relative speed, where a frame of gravity barely moves the needle.</summary>
    private ShipState PlayerStateForPursuit(double stepTime)
    {
        if (!SteerHuntersByQuantumTrail || _pursuitTrail.Count < 2 || stepTime >= _pursuitTrail[^1].SimTime)
        {
            return _ship;
        }

        for (int i = _pursuitTrail.Count - 2; i >= 0; i--)
        {
            if (_pursuitTrail[i].SimTime <= stepTime)
            {
                TrajectorySample a = _pursuitTrail[i], b = _pursuitTrail[i + 1];
                double span = b.SimTime - a.SimTime;
                double f = span > 0 ? (stepTime - a.SimTime) / span : 1;
                return new ShipState(a.Position + (b.Position - a.Position) * f, _ship.Velocity, stepTime);
            }
        }

        return _ship;
    }

    // Heat decay, hunter pursuit and break-off — all in sim time (like NPC stepping), so it
    // scales naturally with warp instead of crawling at wall-clock rate.
    private void UpdateEncounters()
    {
        if (_ephemeris is null)
        {
            return;
        }

        // PR-BUSTED: while a boarding pop-up is open, encounters freeze — the captain is making a
        // choice at 1×, no new hunter runs him down over the top of it.
        if (_busted is not null)
        {
            return;
        }

        // #175: settle any moon-haven cargo run whose ship is parked in orbit — the owner who was
        // ALREADY orbiting Enceladus when the parcel loaded gets paid here, since no dock event fires.
        CompleteBoundCargoRunQuests();

        // #223: resolve the buried-cache discovery roll as sim time rolls past whole days — rivals find
        // our hoards on a slow roll whether we're flying, warping, or docked.
        RunCacheDiscoveryWatch();

        // #638: and the other whole-day watch — the void's. Same cadence and the same skip-proofing, because
        // a countdown that a warp jump can leap over is not a countdown (Map.Void).
        RunTheVoidWatch();

        bool wasHidden = !double.IsNaN(_hiddenAtHavenSinceSimTime);
        bool hidden = IsHiddenAtHaven();

        // Rising edge of "hidden at a haven" — whether you orbited a haven moon or clamped onto a
        // dock. Drop the quiet news line the regulars notice, and advance the haven lesson. (Moved
        // here from the orbit-bind loop so a mass-less dock, which never binds, still triggers it.)
        if (hidden && !wasHidden && _nearestBody is { IsHaven: true } arrivedHaven)
        {
            PushNewsEvent(NewsWire.NewsEventKind.OrbitEnteredHaven, arrivedHaven.Name);
            AdvanceTutorial(StepInsertHaven);

            // Easter egg: settle in at The Rusty Roadstead and the bird cracks wise about a break.
            if (arrivedHaven.Id == "the-space-bar")
            {
                SquawkNow(Parrot.Squawk.SpaceBarBreak, _lastTimestampMs ?? 0, force: true);
            }
        }

        _hiddenAtHavenSinceSimTime = hidden
            ? (wasHidden ? _hiddenAtHavenSinceSimTime : SimTime)
            : double.NaN;
        double hiddenDuration = hidden ? SimTime - _hiddenAtHavenSinceSimTime : 0;

        _heat = EncounterRule.DecayHeat(_heat, SimTime, hidden);

        // #715 · …and the OTHER heat, which is a DIFFERENT NUMBER WITH A DIFFERENT HOLDER. The line above
        // is what the law thinks of a hull; this is what one company thinks of a captain, and the two are
        // never read off each other — the guard next door raises either one and proves the other did not
        // move. It cools in ABSENCE and in nothing else (the owner's own word: "get out"), so the outfit
        // whose ground is underfoot is handed in and is the one outfit this call does not cool.
        //
        // It creates nothing: a captain who has never crossed anybody has an empty book after ten
        // thousand frames, which is what keeps #905's fingerprints where they were.
        IllegalHeat.Cool(_contacts, TheOutfitUnderfoot, SimTime);

        // PR-BUSTED (ruling §5.1): when heat fully cools, the stolen cargo launders — the evidence
        // leaves the books. And at each UPWARD heat crossing the parrot names the confiscation exposure
        // (owner: "Heat two, captain — they'll take a third of the purse if they catch us!"), riding the
        // same #166 alert edges the rest of the ship's voice does.
        if (_heat.Level == 0 && _hotCargo.Any)
        {
            _hotCargo.Launder();
            ShowPulseMessage("The trail's cold — your hot cargo just became honest freight again.");
        }

        if (_heat.Level > _lastAnnouncedHeat)
        {
            SquawkNow(Parrot.Squawk.Busted, _lastTimestampMs ?? 0, BustedRule.ExposurePhrase(_heat.Level), force: true);
        }

        // #380 item 1: the FIRST time heat reaches 1, advertise the safety net one beat before the death card
        // would have to. Fires whatever raised the heat (a robbery, a Reever's hand), once per run.
        if (!_heatInsuranceAdvised && _heat.Level >= 1)
        {
            _heatInsuranceAdvised = true;
            ShowPulseMessage("Word of advice, captain — your brain-backup's current and the pirate-insurance stake is paid. Getting caught is expensive. Getting killed is survivable.");
        }

        _lastAnnouncedHeat = _heat.Level;

        // #580 · NOBODY IS AT THE CONTROLS. While the captain is walking a moon, the ship is a docked hull
        // with the lights on and no one aboard — so the wolves hold station instead of closing, and cannot
        // catch her. See EncounterRule.HoldStation for the owner's ruling; the short of it is that heat is
        // the CAPTAIN's, and a game where a good long excursion means coming home to a boarding party is a
        // game about guarding a parking lot.
        bool captainIsAboard = _surface is null;

        for (int i = _hunters.Count - 1; i >= 0; i--)
        {
            HunterState hunter = _hunters[i];
            if (!captainIsAboard)
            {
                _hunters[i] = EncounterRule.HoldStation(hunter, SimTime);
                continue;
            }

            while (hunter.State.SimTime < SimTime && !hunter.CaughtPlayer && !hunter.BrokenOff)
            {
                double stepTime = Math.Min(SimTime, hunter.State.SimTime + EncounterRule.HunterStepSeconds);
                hunter = EncounterRule.AdvanceHunter(hunter, PlayerStateForPursuit(stepTime), stepTime);
                if (hidden)
                {
                    hunter = EncounterRule.ApplyBreakOff(hunter, hiddenDuration);
                }
            }

            if (hunter.CaughtPlayer)
            {
                ApplyHunterCatch(hunter);
                _hunters.RemoveAt(i);
            }
            else if (hunter.BrokenOff)
            {
                ShowPulseMessage($"{hunter.Callsign} loses your scent — safe at anchor.");
                SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
                _hunters.RemoveAt(i);
            }
            else
            {
                _hunters[i] = hunter;
            }
        }

        // Haven tutorial completes when the heat your piracy earned has fully cooled (the haven's
        // 4x decay is what gets you there in reasonable time — the lesson is "lying low works").
        if (_tutorialStep == StepCoolHeat && _heat.Level == 0)
        {
            AdvanceTutorial(StepCoolHeat);
            ShowPulseMessage("The trail's gone cold — you've learned to lie low. The haven kept you.");
        }
    }

    /// <summary>This hunter is off you now — the one place in the game that means it.
    ///
    /// <para>#731 · And it means it on the GROUND too. A repo crew serves its writ on foot under an id of its
    /// own (<see cref="CollectorLanding.GroundHunterIdPrefix"/>) and was never in <c>_hunters</c>, so every
    /// caller here — the bribe, the resist, the Bolivia flee — removed nothing, and the captain who had just
    /// been told the crew <i>"sheers off"</i> was served again by the same people on the next frame. They
    /// walk back to their own boat now (<c>TheirBusinessHereIsDone</c>), which is #731's full stop and not a
    /// despawn: the ONE call that ends an encounter ends it in both places, so a future caller cannot end
    /// half of one.</para></summary>
    private void RemoveHunter(string hunterId)
    {
        for (int i = _hunters.Count - 1; i >= 0; i--)
        {
            if (_hunters[i].Id == hunterId)
            {
                _hunters.RemoveAt(i);
            }
        }

        TheirBusinessHereIsDone(hunterId);
    }

    private static readonly RgbaColor DriverReachColor = new(120, 210, 255, 170);

    /// <summary>
    /// #962 · TWO CIRCLES, BOTH LABELLED, SO THE CAPTAIN KNOWS WHEN TO REACT. Owner: <i>"The debt collector
    /// catch distance / speed distance is not shown on the maps in any way. We don't know how much we should
    /// react and when visually now."</i>
    ///
    /// <para>Her CATCH ENVELOPE rides with her; our DRIVER REACH rides with us; and after #961's ruling the
    /// second is always the larger of the two, which is the fact the picture has to make obvious — the moment
    /// she crosses our ring we can answer, and she cannot lay a hand on us until she crosses hers. The catch
    /// ring was already drawn, unlabelled, which the war room's own Gemini playtest had already flagged once
    /// on a different circle: <i>"unlabeled ring — is it weapon range?"</i></para>
    ///
    /// <para>Our reach ring is drawn only while a collector is actually on the map — a permanent circle round
    /// the ship would be furniture, and this is meant to be read.</para>
    /// </summary>
    private void DrawHunters()
    {
        bool anyOnTheMap = false;
        foreach (HunterState hunter in _hunters)
        {
            (float sx, float sy) = _camera.WorldToScreen(hunter.State.Position);
            _renderer!.DrawCircle(sx, sy, 5f, HunterColor, HunterColor);
            _renderer!.DrawText(sx + 8, sy - 6, $"🐺 {hunter.Callsign}", HunterColor);

            double distance = (hunter.State.Position - _ship.Position).Length;
            if (distance > EncounterRule.WeaponRangeMeters * 3)
            {
                continue;
            }

            anyOnTheMap = true;
            float catchPx = (float)Math.Clamp(EncounterRule.CatchRadiusMeters / _camera.MetersPerPixel, 4, 200);
            _renderer!.DrawCircle(sx, sy, catchPx, null, HunterColor, 1.5f);
            if (catchPx > 22)
            {
                _renderer!.DrawText(sx, sy - catchPx - 4, $"catch {FormatDistance(EncounterRule.CatchRadiusMeters)}",
                    HunterColor, "10px monospace", TextAlign.Center);
            }
        }

        if (!anyOnTheMap)
        {
            return;
        }

        (float px, float py) = _camera.WorldToScreen(_ship.Position);
        float reachPx = (float)Math.Clamp(EncounterRule.WeaponRangeMeters / _camera.MetersPerPixel, 4, 600);
        _renderer!.DrawCircle(px, py, reachPx, null, DriverReachColor, 1.2f);
        if (reachPx > 26)
        {
            _renderer!.DrawText(px, py - reachPx - 4, $"driver reach {FormatDistance(EncounterRule.WeaponRangeMeters)}",
                DriverReachColor, "10px monospace", TextAlign.Center);
        }
    }

    private static readonly RgbaColor ReticleColor = new(255, 70, 70);
    private static readonly RgbaColor TrackBracketColor = new(150, 255, 210, 200);

    /// <summary>
    /// #962 · THE TARGET IS A PLACE ON THE SKY, NOT A BOX IN THE CORNER. Owner, with the Debt Collector's
    /// dossier open: <i>"There is no visual indicator that we have targeted the debt collector in any way.
    /// Just the disconnected box. There should be a marker on our tracked targets also in nav map... red X
    /// aim reticle maybe?"</i>
    ///
    /// <para>Two marks, because there are two different claims to make. The RED X is the tactical target —
    /// the one contact the dossier is open on, the one fire control is solving for — and it is deliberately
    /// the loudest thing on the map. The small green brackets are everything the telescope is holding
    /// custody of: the Sensors desk kept that list as names in a ledger, with nothing on the sky to say
    /// where any of those names actually is.</para>
    ///
    /// <para>Both read positions through <see cref="ContactPosition"/>, which walks traffic AND hired
    /// muscle — the reticle must not repeat the bug that made 📡 sharpen fix a dead button, where a
    /// hunter's id resolved to nothing because the lookup only knew about <c>_npcStates</c>.</para>
    /// </summary>
    private void DrawTargetReticle()
    {
        if (_renderer is null)
        {
            return;
        }

        if (_trackingPost is not null)
        {
            foreach (TrackedTarget entry in _trackingPost.Entries)
            {
                if (entry.ShipId == TacticalTargetId || ContactPosition(entry.ShipId) is not { } held)
                {
                    continue; // the reticle below says it louder; an id we can't place gets no mark at all
                }

                (float bx, float by) = _camera.WorldToScreen(held);
                DrawBracket(bx, by, 8f, TrackBracketColor);
            }
        }

        if (TacticalTargetId is not { } id || ContactPosition(id) is not { } position)
        {
            return;
        }

        (float sx, float sy) = _camera.WorldToScreen(position);
        // A slow breath, so the reticle reads as LIVE rather than as another painted glyph.
        byte pulse = (byte)(190 + 60 * Math.Sin(_frameNowMs / 320.0));
        RgbaColor ink = ReticleColor with { A = pulse };

        const float inner = 5f, outer = 14f;
        const float diagonal = 0.70710678f; // the X's arms, at 45° off the axes
        ReadOnlySpan<(float X, float Y)> arms = [(1, 1), (1, -1), (-1, 1), (-1, -1)];
        foreach ((float dx, float dy) in arms)
        {
            _renderer.DrawPolyline(
                [sx + dx * diagonal * inner, sy + dy * diagonal * inner,
                 sx + dx * diagonal * outer, sy + dy * diagonal * outer],
                ink, 2f);
        }

        _renderer.DrawCircle(sx, sy, inner + 1.5f, null, ink, 1.2f);
        _renderer.DrawText(sx, sy + outer + 12, $"🎯 {ContactCallsign(id)}", ink, "11px monospace", TextAlign.Center);
    }

    /// <summary>A custody bracket: four corners of a box, drawn open — the telescope is holding this
    /// contact, which is a weaker claim than the reticle's and is drawn as one.</summary>
    private void DrawBracket(float x, float y, float half, RgbaColor color)
    {
        float arm = half * 0.55f;
        ReadOnlySpan<(float X, float Y)> corners = [(-1, -1), (1, -1), (-1, 1), (1, 1)];
        foreach ((float cx, float cy) in corners)
        {
            float px = x + cx * half, py = y + cy * half;
            _renderer!.DrawPolyline([px - cx * arm, py, px, py, px, py - cy * arm], color, 1.2f);
        }
    }

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
