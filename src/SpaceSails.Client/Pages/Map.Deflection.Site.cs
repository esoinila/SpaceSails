using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · ON SITE (#394) — each beat resolved, the drill channel and the drill point, firing the charge, the
/// impact, the comms line, settling the gig, retiring it, the plaque's gratitude, and composing the site.
///
/// <para>Split out of <c>Map.Deflection.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public partial class Map
{
    private void ResolveDeflectionBeat(SurfaceExcursion ex, DeflectionPlan plan, int ordinal)
    {
        ulong seed = DeflectionGig.Seed(plan.AcceptedSimTime, plan.RockBodyId, ordinal);
        DeflectionComplication c = DeflectionGig.Roll(seed, plan.Type, ordinal);
        RaiseDiceEvent(c.Event); // the cast dice, shown (the house homage)

        if (c.NerveHit > 0)
        {
            ApplyNerveShock(c.NerveHit, "the charge work goes wrong under your hands");
        }
        if (c.DrillProgressDelta != 0.0 && !ex.ChargeArmed)
        {
            // A snap sets the bit back; a good bite gains depth. Once armed, the bore is done — no change.
            ex.DrillProgress = Math.Clamp(ex.DrillProgress + c.DrillProgressDelta, 0.0, 1.0);
        }
        if (c.CrewLost)
        {
            // #663 · ONE DEATH, TWO REPORTERS. The excursion's count docks the fee and prints "N of the crew
            // did not come home" on liftoff; the crew's own report keeps the other half, because the sheet
            // on the captain's desk used to answer "Nobody has been lost. On a ship like this that is not
            // luck, it is the captain." over the empty bunks. Both come off this ONE increment — a second
            // place that decided somebody died would be a second set of books, and this house has a bug
            // class named for those.
            ex.DeflectionCrewLost++;
            NoteCrewDidNotComeHome();
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
        if (AnySlowThingUnderYourHands)
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

        // A heavy shock through the seams either way — you do not stand on a falling mountain and fire a
        // charge and feel calm. #480: named, and spent in whole pips.
        ApplyNerveShock(
            outcome == DeflectionOutcome.FullDeflection ? 10.0 : 16.0,
            "you fired a charge standing on a falling mountain");

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
        ApplyNerveShock(NerveModel.MonolithSightShock, "you watched it strike, and could not stop it");
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
}
