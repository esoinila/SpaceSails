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
/// THE FREEZE-FRAME → BRAIN-BACKUP RESURRECTION — waking at the nearest haven's clinic in an insurance
/// rustbucket, owing for the wake-up, with a new face and a book re-greyed at the filing line.
///
/// <para>Everything visible aboard is gone; the tank comes up FULL — the same fuel a new voyage starts
/// with (#477), so the wake puts the captain back in the game instead of into a fuel hunt. Buried and
/// banked survives, because it lives off-ship. Never a dead save.</para>
///
/// <para>The family's one static field lives here: <c>NebulaGlitchFlashes</c>, four literal shard tells
/// seeded off the death so the glitch varies per rebirth yet stays deterministic. It reads no other
/// static, so nothing about the cut can initialise it in the wrong order.</para>
///
/// <para>Split out of <c>Map.Combat.Busted.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // THE FREEZE-FRAME → brain-backup resurrection: wake at the nearest haven's clinic in an insurance
    // rustbucket. Everything VISIBLE aboard is gone; the tank comes up FULL — the same fuel a new voyage
    // starts with (#477), so the wake puts you back in the game instead of into a fuel hunt. Buried/banked
    // survives (it lives off-ship — other lanes). Never a dead save.
    private void BustedResurrect()
    {
        // #735 · …and only OUT OF THE FREEZE. The guard used to be "is there a death open at all", which was
        // true right up until Enter joined the mouse on this button: a keystroke on a focused button fires
        // the keydown handler AND the browser's own click, so the wake could be asked for twice — and a
        // second wake is a second clinic bill, a second succession and a second captain, off one press. The
        // stage is the fact that decides, so the stage is what is asked. Every road in is one of these three.
        if (_busted is not { } b
            || b.Phase is not (BustedEncounter.Stage.FreezeFrame
                or BustedEncounter.Stage.Impact
                or BustedEncounter.Stage.SurfaceEnd))
        {
            return;
        }

        // #422 arc 2 — how many times this THREAD has woken before (durable), read BEFORE the succession adds
        // this death's retiree, so it counts prior lives only. Gates the clinic's second page below.
        int priorDeaths = ActiveThreadInfo?.Retired.Count ?? 0;

        RemoveHunter(b.HunterId);

        // Evening wind #20 — a surface-overdraw death happens mid-excursion, so the away gig ends here as a
        // failed/aborted trip (no payout) and the surface is folded away, before the resurrection flies the
        // brain-backup to the nearest clinic. The buried/banked hoards live off-ship and are untouched.
        bool wasOnSurface = _surface is not null;
        if (wasOnSurface)
        {
            _surface = null;
            _reevers.Clear();
            _lastNearestReeverRange = null;
            LogAutopilotEvent("🛬 The expedition is written off — the away team scrubs the gig and the body path goes to backup.");
        }

        int wakeTank = WakeTankPulses();

        // Consult the insurance policy at rebirth (issue #227 seam): None-tier returns the uninsured
        // rustbucket + full clinic bill untouched; a covered tier would ease it — the pure rule decides.
        RebirthOutcome outcome = InsuranceRule.ApplyToRebirth(
            _insurance, SimTime, InsuranceRule.DefaultRebirth(wakeTank));
        BustedRule.ResurrectionKit kit = outcome.Kit;

        // The rebirth tax (issue #227): the clinic bill is booked against the stake, floored at 0 — a
        // shortfall is a debt the WIRE lane's bank will reconcile against banked/buried funds at merge.
        int afterBill = Math.Max(0, kit.Credits - outcome.ClinicBillCr);
        LogAutopilotEvent($"🏥 Clinic bill: −{outcome.ClinicBillCr:N0} cr (brain-backup wake-up). Insurance: {_insurance.Tier}.");

        _credits = afterBill;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();
        _hotCargo.Launder();
        _reactionMassPulses = Math.Min(kit.ReactionMassPulses, ReactionMassCapacityFor(kit.MassLevel));
        _slugAmmo = kit.SlugAmmo;
        _missileAmmo = kit.MissileAmmo;
        _massLevel = kit.MassLevel;
        _sensorLevel = kit.SensorLevel;
        _holdLevel = kit.HoldLevel;
        _telescopeLevel = kit.TelescopeLevel;
        _hasNetJammer = false;

        // A NEW CAPTAIN GETS THEIR OWN NERVE. Nothing reset this before, so the brain-backup woke a fresh
        // captain already carrying the dead one's shattered gauge — and since the commonest death IS the
        // nerve running out, the replacement routinely woke at or near the floor, one fright from dying the
        // same way. Found while wiring #480, which makes it plainly visible: without this the LEDGER would
        // hand the new captain someone else's last bad minute too. The beat clocks, the banked sub-pip
        // pressure and the ledger all go with them.
        // …but the DEAD captain's ledger is the one thing worth keeping across the seam: the card is about
        // to ask "what broke you?", and the answer is in the last few pips they spent. Snapshot it here,
        // THEN wipe the live one — the first cut cleared it before the card ever rendered, so the block
        // silently never appeared (caught in the browser, not by a test).
        _deathNerveLedger = _nerveLedger;
        _nerve = NerveModel.Steady;
        _nerveBeats = NervePips.Beats.Fresh;
        _nerveShockCarry = 0;
        _nerveLedger = [];
        _nerveFlash = null;

        RebuildSensor();
        _heat = HeatState.None;
        _lastAnnouncedHeat = 0;
        b.ClinicBillCr = outcome.ClinicBillCr;
        b.StakeCr = kit.Credits;   // #621: the receipt reads the kit that paid, not a constant beside it
        b.HullDescription = outcome.HullDescription;

        // Wake at the nearest haven's clinic: reset the ship state onto that haven, riding its rails.
        string clinicName = WakeAtNearestHaven();
        b.ClinicName = clinicName;

        // A surface death folded the excursion; fold the surface DECK away too, back to the bare ship deck
        // in flight (the clinic haven the wake placed us at), so the map view resumes under the modal.
        if (wasOnSurface)
        {
            SetDeckForDock(null);
        }

        // Evening wind #20 — THE NEW CAPTAIN, on ANY death-resurrection (this overdraw AND the collector /
        // Bolivia / impact paths): the piracy insurance rolls the thread's identity — a fresh seeded name and
        // a differing face — and the roster keeps the retiree. The corner chip and roster re-read the change.
        IssueSuccessorCaptain(b);

        // #422 arc 2 — THE GLITCH IN THE REBIRTH. You experience the Nebula thread by DYING: for one flat
        // second the wake card reads a line it should not (a seeded flash off this death), and reading it
        // assembles the shard. The oracle (#427) may already have leaked it; this is the first-hand vector,
        // delivered ON the card. The flash shows every rebirth; the shard is gathered once.
        b.RebirthGlitch = NebulaGlitchFlash(b.Seed);
        AssembleNebulaSilently("rebirth-glitch");

        // #422 — THE CLINIC'S SECOND PAGE. Surfaces only once you've woken here BEFORE (a prior death this
        // thread or this session): the fragment's own fiction — a policy number "older than you", carrying
        // entries you never made. Shown on the card the once it is first gathered.
        if ((priorDeaths >= 1 || _rebirthsSeen >= 1) && AssembleNebulaSilently("clinic-ledger"))
        {
            b.ClinicLedger = NebulaLore.ById("clinic-ledger")!.Lore;
        }

        _rebirthsSeen++;

        b.Phase = BustedEncounter.Stage.Resurrected;
        StateHasChanged();
    }

    // #422 arc 2 — the one-flat-second glitch the resurrection card flashes before the welcome loops clean.
    // Seeded off the death so it varies per rebirth yet is deterministic; the "DO NOT REVIVE ORIGINAL" tell
    // is the shard's heart. Presentation only — the canonical shard lore lives in NebulaLore.
    private static readonly string[] NebulaGlitchFlashes =
    [
        "RESTORE FROM PATTERN 40 · SUBSCRIBER LUCID · DO NOT REVIVE ORIGINAL",
        "PATTERN 40 · COPY SPUN · ORIGINAL RETAINED · DO NOT WAKE ORIGINAL",
        "SUBSCRIBER RE-INSTANCED · SEE ARCHIVE · DO NOT REVIVE ORIGINAL",
        "CONTINUITY: BELIEVED · ORIGINAL: FILED · DO NOT DISTURB THE DARK",
    ];

    private static string NebulaGlitchFlash(ulong seed) =>
        NebulaGlitchFlashes[(int)(seed % (ulong)NebulaGlitchFlashes.Length)];

    // Issue the successor captain onto the active universe (Evening wind #20). Reads the retiring identity,
    // rolls + persists the new one through the registry (the #368 fields are editable data), and refreshes
    // the cached roster so the in-play chip and the saved-voyages list show the new face at once. A run with
    // no indexed thread (a legacy save) simply keeps its current identity and the card narrates generically.
    private void IssueSuccessorCaptain(BustedEncounter b)
    {
        if (string.IsNullOrEmpty(_activeThreadId))
        {
            return;
        }

        if (Threads.Get(_activeThreadId) is { } before)
        {
            b.RetiredCaptainName = SpaceSails.Core.Captains.For(before).Name;
        }

        int simDay = (int)(SimTime / 86400);
        if (Threads.IssueSuccessor(_activeThreadId, simDay) is { } after)
        {
            b.NewCaptainName = after.CaptainName;
            b.NewCaptainAvatar = after.AvatarIndex;
            RefreshThreadList(); // the chip (ActiveThreadInfo) + the roster now read the new identity

            // #973 L1 · THE FILING LINE. The successor wakes remembering the ledger only as far as the last
            // premium reached; every page dated after that is one they don't remember writing. Read AFTER the
            // succession is on the thread, because the roll a grey page is later read on is salted with the
            // LIFE, and this is the moment the life number changes. The policy consulted is `_insurance` at
            // `SimTime` — the same two the clinic bill above was computed from, so the receipt and the amnesia
            // can never disagree about whether anybody was covered.
            b.FilingNotice = MarkTheBookAtTheFilingLine();

            // #973 L5a · …and the same moment empties the list of people this captain has explained his face
            // to. Beside the filing line rather than anywhere else because it is the same fact about the same
            // moment: the one who walks out of the clinic is not the one who answered them.
            ANewFaceHasNothingExplained();
        }
    }

    // The tank a rebirth wakes with: a FULL base tank — the same fuel a new voyage starts with
    // (Map.Sim's _reactionMassPulses seed and ReactionMassCapacityFor(0) agree on the number).
    //
    // #477: this used to be a "mercy floor" priced from FuelReachability, falling back to the flat
    // autopilot reserve — AutopilotRehearsal.ReservePulses(500) = 90 p. But the autopilot refuses any
    // journey that would eat into its reserve, so a tank set TO the reserve is a tank with no usable
    // fare: the new captain woke at Uranus with 90 p, 0 cr and an empty hold, and could not fly, buy
    // or sell. Owner's ruling — give the rebirth what a new game gives. Death costs the hull, the
    // purse, the hold and every upgrade; it does not also cost you the ability to leave.
    private int WakeTankPulses() => ReactionMassCapacityFor(0);

    private int ReactionMassCapacityFor(int massLevel) => 500 + 150 * massLevel;

    // Set the ship down at the nearest haven (the clinic), riding its velocity — a starter-grade parked
    // state. Returns the haven's name for the wake card.
    private string WakeAtNearestHaven()
    {
        if (_ephemeris is null)
        {
            return "a frontier clinic";
        }

        CelestialBody? nearest = null;
        double best = double.MaxValue;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (!body.IsHaven)
            {
                continue;
            }

            double d = (_ephemeris.Position(body.Id, SimTime) - _ship.Position).Length;
            if (d < best) { best = d; nearest = body; }
        }

        if (nearest is null)
        {
            return "a frontier clinic";
        }

        Vector2d pos = _ephemeris.Position(nearest.Id, SimTime);

        // #478 · WAKE AT A BERTH, NOT INSIDE THE STATION. This used to be
        //     _ship = new ShipState(pos, vel, SimTime);
        // which parked the new captain at the haven's EXACT CENTRE — distance zero. Every projected sample
        // then sat inside the haven's body radius, ClosestApproach reported a permanent subsurface pass, and
        // the alarm channel shouted "ROCKS AHEAD! — impact with The Tilt" at a ship reading "clamped on" at
        // 0.0 km/s rel. It was never a predictor bug: the ship really was inside the station.
        //
        // A dockable haven now rides the ONE TRUE CLAMP every real arrival uses (co-moving berth offset,
        // welded interior, pinned at dock), so the wake state is byte-for-byte an ordinary docking and the
        // collision projection sees the same honest berth separation it sees after any other arrival.
        if (DockableHavens.IsDockable(nearest))
        {
            ClampOntoHaven(nearest, pos);
        }
        else
        {
            // A haven with no berth to clamp to (a bare waypoint): still never inside it — sit off it at the
            // shared berth offset, co-moving, rather than at its centre.
            _ship = BerthState.CoMoving(_ephemeris, nearest.Id, SimTime, BerthState.BerthOffsetMeters);
        }

        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
        return nearest.Name;
    }
}
