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

    private void ResolveDeflectionBeat(SurfaceExcursion ex, DeflectionPlan plan, int ordinal)
    {
        ulong seed = DeflectionGig.Seed(plan.AcceptedSimTime, plan.RockBodyId, ordinal);
        DeflectionComplication c = DeflectionGig.Roll(seed, plan.Type, ordinal);
        RaiseDiceEvent(c.Event); // the cast dice, shown (the house homage)

        if (c.NerveHit > 0)
        {
            _nerve = NerveModel.Shock(_nerve, c.NerveHit);
        }
        if (c.DrillProgressDelta != 0.0 && !ex.ChargeArmed)
        {
            // A snap sets the bit back; a good bite gains depth. Once armed, the bore is done — no change.
            ex.DrillProgress = Math.Clamp(ex.DrillProgress + c.DrillProgressDelta, 0.0, 1.0);
        }
        if (c.CrewLost)
        {
            ex.DeflectionCrewLost++;
        }

        RendererInterop.PlayCue(c.Band switch
        {
            DeflectionBand.CrewBolts => "alarm",
            DeflectionBand.GoodBite => "reveal",
            _ => "board",
        });
        ShowPulseMessage($"{c.Event.Headline} {c.Event.Detail}");
    }

    // ── The drill channel: a long bore (per rock type), abortable by stepping away, its depth persisting
    //    across snaps. On completion the charge is set (armed) and auto-fires when the spin aligns. ──
    private void StepDrillChannel(double dtRealSeconds)
    {
        if (_surface is not { DrillChannel: { } ch, Deflection: true } ex || _deflection is not { } plan)
        {
            return;
        }
        double dx = _avatarX - ch.AnchorX, dy = _avatarY - ch.AnchorY;
        if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius)
        {
            ex.DrillChannel = null;
            ShowPulseMessage("You step back — the bore pauses. Depth holds. The clock does not.");
            return;
        }

        double drillSeconds = Math.Max(1.0, DeflectionGig.RockProfile.DrillSeconds(plan.Type));
        ex.DrillProgress = Math.Min(1.0, ex.DrillProgress + dtRealSeconds / drillSeconds);
        if (ex.DrillProgress >= 1.0)
        {
            ex.DrillChannel = null;
            ex.ChargeArmed = true;
            RendererInterop.PlayCue("reveal");
            ShowPulseMessage("🧨 The charge is set to depth — ARMED. The rig backs off. Firing when the spin brings the bore around; or hit the point to fire NOW (risk a bad angle).");
            RebuildSurfaceDeck(); // the DRILL POINT console relabels to FIRE THE CHARGE
        }
    }

    // [E] on the DRILL POINT: start (or resume) the bore, or — once armed — fire the charge NOW (which may
    // catch the rock off-angle and waste part of the shove).
    private void DrillPointInteract()
    {
        if (_surface is not { Deflection: true } ex || _deflection is not { } plan)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.DrillPoint } spot)
        {
            return;
        }
        if (ex.BurnFired)
        {
            return;
        }
        if (ex.ChargeArmed)
        {
            FireCharge(ex, plan, manual: true); // the captain triggers it — the angle is whatever it is
            return;
        }
        if (ex.AnyChannel)
        {
            return;
        }
        ex.DrillChannel = new DrillChannel { AnchorX = spot.X, AnchorY = spot.Y };
        RendererInterop.PlayCue("board");
        ShowPulseMessage($"🛠 Setting the rig — {plan.Type.Label}. {plan.Type.BriefLine} Hold position; step away to pause. The clock does not.");
    }

    // THE BURN — the charge fires, ablation shoves the rock, and its rail lifts off the station's orbit. The
    // periapsis raise (Core: charge × ablation efficiency × rotation alignment) sets the miss and the band.
    private void FireCharge(SurfaceExcursion ex, DeflectionPlan plan, bool manual)
    {
        if (ex.BurnFired || !ex.ChargeArmed)
        {
            return;
        }
        double align = DeflectionGig.RotationAlignment(plan.SpinPeriod, plan.SpinPhase, ex.DeflectionOnSiteSeconds);
        double raise = DeflectionGig.PeriapsisRaiseForBurn(plan.Type, chargeFraction: ex.DrillProgress, rotationAlignment: align);

        ex.BurnFired = true;
        ex.DeflectionResolved = true;
        _deflectionRaiseMeters = raise;

        DeflectionGig.RockRail bent = DeflectionGig.RaisePeriapsis(plan.BaseRail, raise);
        double miss = DeflectionGig.MissDistanceMeters(
            bent, plan.TargetRadius, plan.TargetPeriod, plan.TargetPhase, plan.ImpactRailTime);
        DeflectionOutcome outcome = DeflectionGig.Classify(miss);
        _deflectionResolved = outcome;

        // A heavy shock through the seams either way — you do not stand on a falling mountain and fire a charge
        // and feel calm. The S-curve does the rest.
        _nerve = NerveModel.Shock(_nerve, outcome == DeflectionOutcome.FullDeflection ? 10.0 : 16.0);

        if (outcome != DeflectionOutcome.Impact)
        {
            MarkRingsideSaved();
            PushNewsEvent(NewsWire.NewsEventKind.AsteroidDeflected, plan.TargetName);
            // #400 §3: "me, personally, saving Ringside" — the hero shot. A one-time nudge (guarded per-life
            // by the album); the narration below runs exactly as before.
            OfferSelfie(SelfieBeats.Deflection, "art/ringside-bar.jpg");
        }
        else
        {
            PushNewsEvent(NewsWire.NewsEventKind.AsteroidStruck, plan.TargetName);
        }

        RendererInterop.PlayCue(outcome == DeflectionOutcome.Impact ? "alarm" : "reveal");
        ShowDeflectionStory(outcome);
        RebuildSurfaceDeck(); // the spent bore drops its console
    }

    // The clock ran out with the crew still drilling and no burn away — the rock arrives. Ringside SURVIVES as
    // canon (heavy damage, never destroyed). The crew can still lift off alive (bounded consequences).
    private void ResolveImpactOnSite(SurfaceExcursion ex, DeflectionPlan plan)
    {
        ex.DeflectionResolved = true;
        _deflectionResolved = DeflectionOutcome.Impact;
        _nerve = NerveModel.Shock(_nerve, NerveModel.MonolithSightShock);
        PushNewsEvent(NewsWire.NewsEventKind.AsteroidStruck, plan.TargetName);
        RendererInterop.PlayCue("alarm");
        ShowDeflectionStory(DeflectionOutcome.Impact);
        ShowPulseMessage("⏱ T-0 — no burn in time. The rock keeps its appointment with the Exchange. Get the crew off the rock.");
    }

    // ── The doom clock line (SurfaceOrbitComms routes here on the rock): T-minus to impact, naming the port. ──
    private (string Line, int Severity)? DeflectionComms()
    {
        if (_deflection is not { } plan || _surface is not { Deflection: true } ex)
        {
            return null;
        }
        if (ex.BurnFired || ex.DeflectionResolved)
        {
            DeflectionOutcome o = _deflectionResolved ?? DeflectionOutcome.Impact;
            return o switch
            {
                DeflectionOutcome.FullDeflection => ($"✔ {plan.TargetName} CLEAR — the rock is off the line.", 0),
                DeflectionOutcome.GrazingMiss => ($"➰ Grazing miss — {plan.TargetName} scraped but standing.", 1),
                _ => ($"💥 {plan.TargetName} STRUCK — get the crew off the rock.", 2),
            };
        }

        double left = DeflectionGig.SecondsToImpact(ex.DeflectionOnSiteSeconds);
        ImpactClock clock = DeflectionGig.ClassifyClock(ex.DeflectionOnSiteSeconds);
        int severity = clock switch { ImpactClock.Counting => 1, _ => 2 };
        string word = ex.ChargeArmed ? "CHARGE ARMED — firing solution aligning"
            : ex.DrillProgress > 0 ? $"drilling {(int)(ex.DrillProgress * 100)}%"
            : "land and drill";
        return ($"⏱ IMPACT — {plan.TargetName.ToUpperInvariant()} — T-{FormatClock(left)} · {word}", severity);
    }

    // ── Settle on liftoff: heroic pay if the burn fired (band-scaled, docked per crew lost); floor only on an
    //    honest abort (the rock left on its line). ──
    private bool SettleDeflection(SurfaceExcursion ex)
    {
        if (_deflection is not { } plan)
        {
            return false;
        }
        DeflectionOutcome outcome = ex.BurnFired ? (_deflectionResolved ?? DeflectionOutcome.GrazingMiss)
            : DeflectionOutcome.Impact; // lifted off without firing = aborted, the rock hits

        if (!ex.BurnFired && !ex.DeflectionResolved)
        {
            // An abort BEFORE the clock ran out — the crew is alive, but the port takes it. Narrate + news once.
            ex.DeflectionResolved = true;
            _deflectionResolved = DeflectionOutcome.Impact;
            PushNewsEvent(NewsWire.NewsEventKind.AsteroidStruck, plan.TargetName);
        }

        double fromRadius = HelioRadiusMeters(ex.RestoreHavenId);
        double toRadius = HelioRadiusMeters(plan.RockBodyId);
        int pay = DeflectionGig.Total(plan.BaseFee, fromRadius, toRadius, outcome, ex.DeflectionCrewLost);
        _credits += pay;

        int brought = Math.Max(0, DeflectionCrewSize - ex.DeflectionCrewLost);
        string toll = ex.DeflectionCrewLost > 0
            ? $" {ex.DeflectionCrewLost} of the crew did not come home."
            : " All hands came home.";
        string band = outcome switch
        {
            DeflectionOutcome.FullDeflection => $"🛰 {plan.TargetName} SAVED — {plan.RockName} shoved clean off the line.",
            DeflectionOutcome.GrazingMiss => $"➰ {plan.TargetName} grazed — {plan.RockName} scraped past; heavy damage, but she stands. Half the fee, honestly.",
            _ => $"💥 {plan.TargetName} STRUCK — {plan.RockName} arrived. The Exchange is wreckage but holding. The port pays the floor for the attempt.",
        };

        RendererInterop.PlayCue(outcome == DeflectionOutcome.Impact ? "alarm" : "reveal");
        RequestVaultSave();
        ShowPulseMessage($"{band} Deflection paid {pay:N0} cr ({brought}/{DeflectionCrewSize} back).{toll}");
        // The gig stays on the map (as a cleared/struck rail) until the crew next docks at the port — then retire.
        return true;
    }

    // Retire the resolved gig once the crew LEAVES the saved port and later returns (the alert closes; the map
    // clears). Persisting through the immediate post-liftoff dock keeps the money shot — the bent/cleared rail
    // — on screen for the return to the map, and only a deliberate later re-dock clears it.
    private void RetireDeflectionIfDone()
    {
        if (_deflection is not { } plan || _deflectionResolved is null)
        {
            return;
        }
        bool atPort = _surface is null && _dockedHavenId == plan.TargetBodyId;
        if (!atPort)
        {
            _deflectionLeftPort = true;
        }
        else if (_deflectionLeftPort)
        {
            _deflection = null;
            _deflectionResolved = null;
            _deflectionRaiseMeters = 0;
            _deflectionLeftPort = false;
        }
    }

    // Persist that this universe's crew saved Ringside — the plaque grows a line of gratitude, forever after,
    // on this thread and no other (the vault's ProgressSection).
    private void MarkRingsideSaved()
    {
        if (_ringsideSaved)
        {
            return;
        }
        _ringsideSaved = true;
        RequestVaultSave();
    }

    // #394: when the captain reads Ringside's dedication plaque AND this universe's crew turned the rock, the
    // plate carries the appended gratitude line (Core Plaques.DedicationLore). Untouched otherwise.
    private DeckPlan.ConsoleSpot MaybeAppendPlaqueGratitude(DeckPlan.ConsoleSpot spot)
    {
        if (_ringsideSaved && _dockedHavenId == Plaques.DeflectionGratitudeStationId
            && Plaques.For(Plaques.DeflectionGratitudeStationId) is { } ring
            && spot.Caption == ring.Lore)
        {
            return spot with { Caption = Plaques.DedicationLore(ring, ringsideSaved: true, DeflectionShipName) };
        }
        return spot;
    }

    // ── The rock's ground: one marked DRILL POINT on open regolith (relabels to FIRE THE CHARGE once armed).
    //    Composed onto the freshly-built base like the expedition site (RebuildSurfaceDeck). ──
    private void ComposeDeflectionSite(SurfaceExcursion ex)
    {
        if (ex.BurnFired)
        {
            return; // the bore is spent — no console
        }
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        float x = (float)(field.AnchorX + 12.0);
        float y = (float)((field.LandingBandY + field.BottomY) / 2.0); // open ground below the landing band
        string label = ex.ChargeArmed ? "🧨 FIRE THE CHARGE" : "🛠 DRILL POINT";
        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            [], [new DeckPlan.ConsoleSpot(DeckPlan.ConsoleKind.DrillPoint, x, y, label)], [], []));
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

    private void DrawAsteroidThreat()
    {
        // 🗺 Layers (#405) — SAFETY INVARIANT: the inbound rock / collision warning is PINNED. It
        // deliberately consults NO LayerVisible gate (Threats is a pinned family, and even so the
        // rock's leaf resolves always-visible in Core). A hidden layer must never be able to swallow
        // the one thing that can end the run — do not add a layer check here.
        if (_deflection is not { } plan || _ephemeris is null || _renderer is null)
        {
            return;
        }
        if (_ephemeris.Bodies.All(b => b.Id != plan.ParentBodyId))
        {
            return;
        }

        RgbaColor color = _deflectionResolved switch
        {
            DeflectionOutcome.FullDeflection => ClearGreen,
            DeflectionOutcome.GrazingMiss => GrazeAmber,
            DeflectionOutcome.Impact => ThreatRed,
            _ => ThreatRed, // unresolved — inbound
        };

        Vector2d parent = _ephemeris.Position(plan.ParentBodyId, SimTime);
        DeflectionGig.RockRail rail = DeflectionGig.RaisePeriapsis(plan.BaseRail, _deflectionRaiseMeters);

        // The rail ellipse (perifocal → rotate by ω → parent-relative → world), swept in eccentric anomaly.
        Span<float> ring = stackalloc float[(DeflectionRailSegments + 1) * 2];
        double a = rail.SemiMajorAxis, e = rail.Eccentricity;
        double semiMinor = a * Math.Sqrt(1.0 - e * e);
        double cosW = Math.Cos(rail.ArgPeriapsis), sinW = Math.Sin(rail.ArgPeriapsis);
        for (int i = 0; i <= DeflectionRailSegments; i++)
        {
            double t = Math.Tau * i / DeflectionRailSegments;
            double px = a * (Math.Cos(t) - e);
            double py = semiMinor * Math.Sin(t);
            Vector2d world = parent + new Vector2d(cosW * px - sinW * py, sinW * px + cosW * py);
            (float sx, float sy) = _camera.WorldToScreen(world);
            ring[i * 2] = sx;
            ring[i * 2 + 1] = sy;
        }
        _renderer.DrawPolyline(ring, color, 2f);

        // The intersect ⚠ — the rail's periapsis point (on the station's orbit before deflection; lifted off
        // after). Drawn where the collision WOULD be.
        Vector2d impactWorld = parent + new Vector2d(
            rail.PeriapsisMeters * cosW, rail.PeriapsisMeters * sinW);
        (float ix, float iy) = _camera.WorldToScreen(impactWorld);

        // The threat line: from the rock (its live position on the map) to the intersect point.
        if (!_deflectionResolved.HasValue || _deflectionResolved == DeflectionOutcome.Impact)
        {
            Vector2d rockWorld = _ephemeris.Bodies.Any(b => b.Id == plan.RockBodyId)
                ? _ephemeris.Position(plan.RockBodyId, SimTime)
                : parent + DeflectionGig.RockPosition(rail, SimTime);
            (float rx, float ry) = _camera.WorldToScreen(rockWorld);
            Span<float> line = [rx, ry, ix, iy];
            _renderer.DrawPolyline(line, color, 1.6f);
        }

        // The marker glyph + label.
        (string glyph, string text) = _deflectionResolved switch
        {
            DeflectionOutcome.FullDeflection => ("✔", $"{plan.TargetName} — CLEAR"),
            DeflectionOutcome.GrazingMiss => ("➰", $"{plan.TargetName} — GRAZED"),
            DeflectionOutcome.Impact => ("💥", $"{plan.TargetName} — STRUCK"),
            _ => ("⚠", $"IMPACT — {plan.TargetName}"),
        };
        _renderer.DrawCircle(ix, iy, 5f, color, color);
        // #402: the threat rock's ⚠/name is the deflection money-moment — it sits at the top of the
        // label priority ladder so FlushNavLabels never lets a depot's name smear over it.
        EnqueueNavLabel(ix + 8, iy - 6, $"{glyph} {text}", color, LabelPriorityThreatRock);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE STORYBOARD — the aftermath, told in staged panels with delivered art (the BUSTED staged idiom):
    //  a full deflection is the 4-panel money shot; a graze and an impact each get 2. Per-panel gradient
    //  fallback, house-voice captions.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private readonly record struct DeflectionStoryPanel(string ArtFile, int Hue, string Caption);
    private List<DeflectionStoryPanel>? _deflectionStory;
    private int _deflectionStoryIndex;

    private void ShowDeflectionStory(DeflectionOutcome outcome)
    {
        _deflectionStory = outcome switch
        {
            DeflectionOutcome.FullDeflection =>
            [
                new("deflect-success-0.jpg", 210, "Hold the light, Ringside — this one's ours. And she sets her thumb on the plunger."),
                new("deflect-success-1.jpg", 30, "The charge goes down the bore, all of it. The rock doesn't know yet."),
                new("deflect-success-2.jpg", 45, "Then the mountain comes apart — a white flash and a million glittering stones, and not one of them is aimed at the Exchange anymore."),
                new("deflect-success-3.jpg", 190, $"Back at Ringside they're still trading. The crew of {DeflectionShipName} drinks for free tonight — and every night the rings keep turning."),
            ],
            DeflectionOutcome.GrazingMiss =>
            [
                new("deflect-partial-1.jpg", 35, "The charge bites, but the rock is stubborn — it heels over, groaning, and slides past the Exchange close enough to scratch the paint."),
                new("deflect-partial-2.jpg", 15, "Ringside is bleeding — a dock gone, a deck open to the black — but she is standing. Half the fee, and honest about why."),
            ],
            _ =>
            [
                new("deflect-impact-1.jpg", 8, "No burn in time. The rock keeps its appointment. Ringside fills the sky and there is nothing left to do but hold on."),
                new("deflect-impact-2.jpg", 12, "It hits. The trade decks are wreckage and the berths are a ruin — but the Exchange held, and crews are already clearing the dark. She'll trade again. She always does."),
            ],
        };
        _deflectionStoryIndex = 0;
    }

    private void AdvanceDeflectionStory()
    {
        if (_deflectionStory is null)
        {
            return;
        }
        _deflectionStoryIndex++;
        if (_deflectionStoryIndex >= _deflectionStory.Count)
        {
            _deflectionStory = null;
        }
    }

    // The panel's hero image over a per-panel gradient, so a missing/404 asset still reads as a tinted card
    // (the ExpeditionBriefArtCss / souvenir onerror-hide idiom).
    private static string DeflectionPanelArtCss(DeflectionStoryPanel panel)
    {
        string gradient = $"radial-gradient(circle at 42% 36%, hsl({panel.Hue}, 55%, 30%), hsl({(panel.Hue + 24) % 360}, 60%, 8%) 74%)";
        return $"url('art/{panel.ArtFile}'), {gradient}";
    }
}
