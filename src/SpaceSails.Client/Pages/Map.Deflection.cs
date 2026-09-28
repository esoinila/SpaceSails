using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Deflection — #394 THE ASTEROID DEFLECTION. Armageddon-style, homage not reproduction (owner: "asteroid
// deflection Armageddon movie style 🫡😎"), and the rock NEVER threatens Earth — the target is the Ringside
// Exchange, the He3 clearing-house (owner ruling 2026-07-20). The pure spine lives in Core (DeflectionGig:
// the colliding Kepler rail, the miss math, the drill/ablation/rotation model, the diced complications, the
// success bands, the heroic pay); this partial is the thin client: the accepted gig, the inbound rock's
// threat-line drawn on the nav map, the on-site drilling + complications, the burn that bends the rail (the
// money shot), the storyboard aftermath, and the payout + the plaque's line of gratitude.
public partial class Map
{
    // The accepted deflection gig, or null when none is running. Held past resolution so the nav map can show
    // the bent/cleared rail (the money shot is seen on the return to the map); retired on the next dock at the
    // saved port. Session state like every mission; the SAVE flag (RingsideSaved) is what persists per-universe.
    private DeflectionPlan? _deflection;

    // The periapsis raise the burn delivered (0 until it fires) — the map bends the drawn rail up by this.
    private double _deflectionRaiseMeters;

    // #394: whether THIS universe's crew has saved Ringside (persisted in the vault's ProgressSection). Gates
    // the plaque's appended gratitude line. Set on a full/grazing deflection; false in a fresh universe.
    private bool _ringsideSaved;

    // Set once the gig resolves (full / grazing / impact) — colours the rail and gates the retire.
    private DeflectionOutcome? _deflectionResolved;

    // True once the crew has left the saved port after resolution — so the bent/cleared rail (the money shot)
    // persists through the immediate post-liftoff dock and only clears on a deliberate later return.
    private bool _deflectionLeftPort;

    // #394: the crew's own hull, named on Ringside's plaque after a save (owner: "raised again by the crew of
    // Hull No. 77"). Matches the builder's-plate canon (Plaques.Ship).
    private const string DeflectionShipName = "Hull No. 77";

    // The default crew the gig risks (narrated, like the expedition team — the pay tracks how many come home).
    private const int DeflectionCrewSize = 5;

    private const int MaxDeflectionBeatsPerFrame = 2;

    // The pending cheat spec, resolved at world-build (the rock body is appended pre-ephemeris) and consumed by
    // InjectDeflectionCheat after the berth clamp, so the accepted gig lands on a live world.
    private (RockType Type, string RockName, DeflectionGig.RockRail Rail,
        string TargetId, string TargetName, double TargetRadius, double TargetPeriod, double TargetPhase,
        string ParentId, double ImpactRailTime, double SpinPeriod, double SpinPhase)? _pendingDeflectionCheat;

    // One accepted deflection gig — the captain's contract, session-held. The rail and target geometry are
    // frozen at accept so the threat line and the miss both read the same numbers all gig long.
    private sealed record DeflectionPlan(
        string TargetBodyId, string TargetName,
        string RockBodyId, string RockName, RockType Type,
        string ParentBodyId,
        double TargetRadius, double TargetPeriod, double TargetPhase,
        double ImpactRailTime,
        DeflectionGig.RockRail BaseRail,
        double SpinPeriod, double SpinPhase,
        int BaseFee, double AcceptedSimTime)
    {
        public string Describe() => $"⚠ DEFLECTION: turn {RockName} off {TargetName}";
    }

    // ── The cheat inject (/map?deflection=1): drop the ACCEPTED gig onto the live world after the berth clamp,
    //    so the loop is: rock inbound on the map → shuttle to the rock → drill the charge → fire → the rail
    //    bends → home. Idempotent; no-ops if the rock body somehow isn't on the charts. ──
    private void InjectDeflectionCheat()
    {
        if (_pendingDeflectionCheat is not { } spec || _ephemeris is null
            || _ephemeris.Bodies.All(b => b.Id != DeflectionGig.BodyId) || _deflection is not null)
        {
            return;
        }

        _deflection = new DeflectionPlan(
            spec.TargetId, spec.TargetName, DeflectionGig.BodyId, spec.RockName, spec.Type, spec.ParentId,
            spec.TargetRadius, spec.TargetPeriod, spec.TargetPhase, spec.ImpactRailTime, spec.Rail,
            spec.SpinPeriod, spec.SpinPhase, DeflectionGig.BaseFee, SimTime);
        _deflectionRaiseMeters = 0;
        _deflectionResolved = null;
        _pendingDeflectionCheat = null;

        AnnounceInboundRock(_deflection);
        ShowPulseMessage(
            $"🧪 Test: deflection gig accepted — {spec.RockName} ({spec.Type.Label}) is inbound on {spec.TargetName}. " +
            "It's a short shuttle hop off the berth. Fly out, land on the rock, drill the charge, and FIRE before T-0. " +
            "Watch the red threat line on the map bend off the station when the burn takes.");
    }

    // The LOUD emergency (owner: "a rare, LOUD emergency gig"). Fires the alarm cue, a news-wire collision
    // alert, and the pulse — the offer that reads like a klaxon, not the neighborhood mix.
    private void AnnounceInboundRock(DeflectionPlan plan)
    {
        RendererInterop.PlayCue("alarm");
        PushNewsEvent(NewsWire.NewsEventKind.AsteroidInbound, plan.TargetName, plan.Type.Label);
    }

    // ── The on-site loop: while the crew is on the rock, run the doom clock, roll complications on a cadence,
    //    fill the drill, and auto-fire the armed charge at the next rotation-aligned moment. ──
    private void StepDeflection(double dtRealSeconds)
    {
        if (_surface is not { Deflection: true } ex || _deflection is not { } plan)
        {
            return;
        }
        if (ex.DeflectionResolved)
        {
            return; // the gig is settled on-site (fired or struck) — the clock stops, nothing more rolls
        }

        ex.DeflectionOnSiteSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);

        // The diced complications (the #370 cadence): drill snaps, tremors, a crew member bolting.
        int due = DeflectionGig.EpisodesElapsed(ex.DeflectionOnSiteSeconds);
        int fired = 0;
        while (ex.DeflectionLastOrdinal + 1 < due && fired < MaxDeflectionBeatsPerFrame)
        {
            int ordinal = ++ex.DeflectionLastOrdinal;
            fired++;
            ResolveDeflectionBeat(ex, plan, ordinal);
        }

        // Auto-fire: once the charge is drilled to depth, hold it until the spinning rock brings the bore into
        // the firing window, then let it go (a clean run fires near-perfectly aligned).
        if (ex is { ChargeArmed: true, BurnFired: false })
        {
            double align = DeflectionGig.RotationAlignment(plan.SpinPeriod, plan.SpinPhase, ex.DeflectionOnSiteSeconds);
            if (align >= DeflectionGig.FiringWindowAlignment)
            {
                FireCharge(ex, plan, manual: false);
            }
        }

        // The clock ran out with no burn away: the rock keeps its appointment. Impact — resolved once.
        if (!ex.BurnFired && !ex.DeflectionResolved
            && DeflectionGig.SecondsToImpact(ex.DeflectionOnSiteSeconds) <= 0.0)
        {
            ResolveImpactOnSite(ex, plan);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE THREAT LINE — drawn on the nav map (called from OnTick after the bodies). The rock's rail, a red
    //  ⚠ where it kisses the station's orbit, and a threat line from the rock to that point. When the burn
    //  fires, the rail bends up off the station and goes green — the money shot.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private const int DeflectionRailSegments = 96;
    private static readonly RgbaColor ThreatRed = new(255, 74, 74, 235);
    private static readonly RgbaColor ClearGreen = new(90, 230, 140, 235);
    private static readonly RgbaColor GrazeAmber = new(245, 190, 90, 235);
}
