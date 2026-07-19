using System;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Expedition — #370 THE AWAY EXPEDITION. The high-pay, high-risk "take a group X to Y to do Z" gig:
// ferry a science team (or a mining-survey crew — same skeleton) to a mission-spawned passing rock, hold
// the ship in shuttle range, and walk the team through the diced site while the away clock runs. The pure
// spine lives in Core (ExpeditionSite / ExpeditionWindow / AwayExpeditionEvents / ExpeditionReward /
// SurfaceLayout.ForExpedition); this partial is the thin client: the accepted plan, the on-site beats, the
// away clock line, and the payout. Owner spec: issue #370.
public partial class Map
{
    // The accepted away-expedition gig, or null when none is running. Session-only, like every other
    // mission — there is no save system for it yet (a reload drops it, same law as _mission/_quests).
    private ExpeditionPlan? _expedition;

    // The default team the sponsor sends down — narrated deck droids scurrying the consoles, not modelled
    // individually in v1 (the payout tracks how many come home; the ground is authored, not pathfound).
    private const int ExpeditionTeamSize = 4;

    // Cap the LIMITED pack a bad on-site beat can rouse — never the Miranda stream (owner's hard line).
    private const int ExpeditionMaxPack = 5;

    // At most this many beats resolve in one frame, so a background-tab resume that hands in a big time
    // delta can't burst the whole table at once (mirrors the tide's MaxTideSpawnsPerFrame guard).
    private const int MaxExpeditionBeatsPerFrame = 2;

    // #370 dev cheat entry (/map?expedition=1|mining): the site body was appended before the ephemeris was
    // built; here — after the berth clamp — we drop the ACCEPTED gig onto the live world so the loop is:
    // spawn → shuttle door → take the team down → see the away clock → come back. Idempotent; no-ops if the
    // site body somehow isn't on the charts.
    private void InjectExpeditionCheat()
    {
        if (_pendingExpeditionCheat is not { } spec || _ephemeris is null
            || _ephemeris.Bodies.All(b => b.Id != spec.SiteBodyId) || _expedition is not null)
        {
            return;
        }

        _expedition = new ExpeditionPlan(
            spec.Flavor, spec.Kind, spec.SiteBodyId, spec.SiteName,
            TeamSize: ExpeditionTeamSize, BaseFee: ExpeditionReward.BaseFee, AcceptedSimTime: SimTime);
        _pendingExpeditionCheat = null;

        string who = spec.Flavor == ExpeditionFlavor.Science ? "science team" : "survey crew";
        RendererInterop.PlayCue("reveal");
        ShowPulseMessage(
            $"🧪 Test: away-expedition accepted — ferry the {who} to {spec.SiteName}. " +
            $"It's a short hop off the berth (shuttle range). Open the shuttle door and take them down.");
    }

    // ── The on-site beats: while the team is on the gig's ground, roll the diced table on a cadence. ──
    // Called from StepSurface (instead of StepTide) for an expedition excursion. Each due beat rolls a 2D6
    // episode (AwayExpeditionEvents), shows it on the shared dice tray, and applies its consequence: banks a
    // discovery bonus, shocks the nerve through the existing seam, loses a scientist to the dark, or rouses a
    // LIMITED pack. When the away clock finally closes, one stranding toll rolls.
    private void StepExpedition(double dtRealSeconds)
    {
        if (_surface is not { Expedition: true } ex || _expedition is not { } plan)
        {
            return;
        }

        ex.ExpeditionOnSiteSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);

        int due = AwayExpeditionEvents.EpisodesElapsed(ex.ExpeditionOnSiteSeconds);
        int fired = 0;
        while (ex.ExpeditionLastOrdinal + 1 < due && fired < MaxExpeditionBeatsPerFrame)
        {
            int ordinal = ++ex.ExpeditionLastOrdinal;
            fired++;
            ResolveExpeditionBeat(ex, plan, ordinal);
        }

        // The window closed: the ship can't hold the course-match any longer. One diced stranding toll.
        if (!ex.ExpeditionStrandingFired && ExpeditionClockSeconds(plan, ex) <= 0.0)
        {
            ex.ExpeditionStrandingFired = true;
            ResolveExpeditionStranding(ex, plan);
        }
    }

    private void ResolveExpeditionBeat(SurfaceExcursion ex, ExpeditionPlan plan, int ordinal)
    {
        ulong seed = AwayExpeditionEvents.Seed(plan.AcceptedSimTime, plan.SiteBodyId, ordinal);
        ExpeditionEpisode ep = AwayExpeditionEvents.Roll(seed, plan.Flavor, ordinal);

        RaiseDiceEvent(ep.Event); // the cast dice, shown (the house homage)

        if (ep.NerveHit > 0)
        {
            _nerve = NerveModel.Shock(_nerve, ep.NerveHit);
        }
        if (ep.BonusCredits > 0)
        {
            ex.ExpeditionBonus += ep.BonusCredits;
        }
        if (ep.ScientistLost)
        {
            ex.ExpeditionScientistsLost++;
        }
        if (ep.HostilePack > 0)
        {
            SpawnReevers(Math.Min(ep.HostilePack, ExpeditionMaxPack));
        }

        RendererInterop.PlayCue(ep.Outcome switch
        {
            ExpeditionOutcome.Horror => "alarm",
            ExpeditionOutcome.Discovery or ExpeditionOutcome.MajorDiscovery => "reveal",
            _ => "board",
        });
        ShowPulseMessage($"{ep.Event.Headline} {ep.Event.Detail}");
    }

    // The stranding toll (owner: "the scientists losing sanity and running off ... we can dice throw these
    // outcomes"): when the hold window shuts, a 2D6 decides how badly the recall goes — clean, a scientist
    // lost in the scramble, or two on a rout. Narrated on the tray. Fires once.
    private void ResolveExpeditionStranding(SurfaceExcursion ex, ExpeditionPlan plan)
    {
        ulong seed = AwayExpeditionEvents.Seed(plan.AcceptedSimTime, plan.SiteBodyId, ordinal: 9001);
        DicePool pool = DiceRule.RollPool(seed, count: 2, sides: 6);
        int lost = pool.FaceTotal <= 4 ? 2 : pool.FaceTotal <= 8 ? 1 : 0;
        ex.ExpeditionScientistsLost += lost;

        string detail = lost switch
        {
            0 => "The last of the team claws back to the tube as the ship's hold gives — everyone makes it, barely.",
            1 => "The ship is slipping out of reach. In the scramble for the tube, one doesn't make it back to the light.",
            _ => "The window slams shut. The recall becomes a rout — two are lost to the dark before the tube seals.",
        };
        RaiseDiceEvent(DiceEvent.FromPool(AwayExpeditionEvents.Source, pool,
            "⏳ The hold window has closed — the ship is breaking station.", detail));
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage("⏳ Out of shuttle range — recall the team NOW. Board the shuttle before it's too late.");
    }

    // ── The away clock: how the HUD's ship-line reads on the gig (SurfaceOrbitComms routes to this). ──
    // The tighter of the sponsor's contracted hold budget and the honest geometry window (ExpeditionWindow):
    // docked at the berth the ship holds perfect range, so the budget is the live countdown; cast off and
    // drift, and the geometry can cut it short.
    private (string Line, int Severity)? ExpeditionComms()
    {
        if (_expedition is not { } plan || _surface is not { Expedition: true } ex)
        {
            return null;
        }

        double clock = ExpeditionClockSeconds(plan, ex);
        WindowStatus status = ExpeditionStatus(plan, ex);
        int severity = status switch
        {
            WindowStatus.Holding => 0,
            WindowStatus.Ticking => 1,
            _ => 2, // Critical or Lost — loud
        };

        string word = status switch
        {
            WindowStatus.Holding => "HOLDING course-match",
            WindowStatus.Ticking => "in shuttle range",
            WindowStatus.Critical => "LAST CALL — recall the team",
            _ => "OUT OF REACH",
        };
        string clockText = status == WindowStatus.Lost ? "0:00" : FormatClock(clock);
        return ($"⏱ Away window {clockText} — {word}", severity);
    }

    // The live away-clock seconds: min(contracted budget remaining, honest geometry window). Docked → the
    // geometry is a held (infinite) window, so the budget rules; adrift → the geometry range-rate can win.
    private double ExpeditionClockSeconds(ExpeditionPlan plan, SurfaceExcursion ex)
    {
        double budget = ExpeditionWindow.OnSiteRemainingSeconds(
            ExpeditionWindow.DefaultHoldWindowSeconds, ex.ExpeditionOnSiteSeconds);
        double geometry = ExpeditionGeometryWindow(plan);
        return ExpeditionWindow.EffectiveClockSeconds(budget, geometry);
    }

    private WindowStatus ExpeditionStatus(ExpeditionPlan plan, SurfaceExcursion ex)
    {
        if (ExpeditionClockSeconds(plan, ex) <= 0.0)
        {
            return WindowStatus.Lost;
        }

        // Docked, the station holds us in the local frame — a held window; the budget simply counts down.
        if (_dockedHavenId is not null)
        {
            return ExpeditionWindow.OnSiteRemainingSeconds(ExpeditionWindow.DefaultHoldWindowSeconds, ex.ExpeditionOnSiteSeconds)
                <= ExpeditionWindow.DefaultCriticalSeconds ? WindowStatus.Critical : WindowStatus.Holding;
        }

        (double distance, double rate) = ExpeditionRangeState(plan);
        WindowStatus geo = ExpeditionWindow.Classify(distance, rate, ExpeditionWindow.DefaultCriticalSeconds);
        // If the budget is the tighter of the two, reflect its urgency too.
        double budget = ExpeditionWindow.OnSiteRemainingSeconds(ExpeditionWindow.DefaultHoldWindowSeconds, ex.ExpeditionOnSiteSeconds);
        if (budget <= ExpeditionWindow.DefaultCriticalSeconds && geo != WindowStatus.Lost)
        {
            return WindowStatus.Critical;
        }
        return geo;
    }

    // The honest geometry window (seconds) from the live ship↔site geometry. Infinite while docked (the
    // station holds the range) or while closing/holding; finite and shrinking only when the gap opens.
    private double ExpeditionGeometryWindow(ExpeditionPlan plan)
    {
        if (_dockedHavenId is not null)
        {
            return double.PositiveInfinity;
        }
        (double distance, double rate) = ExpeditionRangeState(plan);
        return ExpeditionWindow.TimeLeftInRangeSeconds(distance, rate);
    }

    // The site's current distance from the ship and the opening range-rate, read off the ephemeris rail
    // (site velocity by a 1-second finite difference — cheap, deterministic).
    private (double Distance, double Rate) ExpeditionRangeState(ExpeditionPlan plan)
    {
        if (_ephemeris is null)
        {
            return (0.0, 0.0);
        }
        Vector2d sitePos = _ephemeris.Position(plan.SiteBodyId, SimTime);
        Vector2d siteNext = _ephemeris.Position(plan.SiteBodyId, SimTime + 1.0);
        Vector2d siteVel = siteNext - sitePos; // per second
        Vector2d relPos = sitePos - _ship.Position;
        Vector2d relVel = siteVel - _ship.Velocity;
        return (relPos.Length, ExpeditionWindow.RangeRate(relPos, relVel));
    }

    // ── Settle: the payout on the ride home (called from LiftOffFromSurface for an expedition excursion). ──
    private bool SettleExpedition(SurfaceExcursion ex)
    {
        if (_expedition is not { } plan)
        {
            return false;
        }

        double fromRadius = HelioRadiusMeters(ex.RestoreHavenId);
        double toRadius = HelioRadiusMeters(plan.SiteBodyId);
        int pay = ExpeditionReward.Total(
            plan.BaseFee, fromRadius, toRadius, ex.ExpeditionBonus, ex.ExpeditionScientistsLost);
        _credits += pay;

        int brought = Math.Max(0, plan.TeamSize - ex.ExpeditionScientistsLost);
        string who = plan.Flavor == ExpeditionFlavor.Science ? "scientists" : "the survey crew";
        string toll = ex.ExpeditionScientistsLost > 0
            ? $" {ex.ExpeditionScientistsLost} of {who} did not come home."
            : $" All {who} came home.";
        string finds = ex.ExpeditionBonus > 0 ? $" Discoveries banked +{ex.ExpeditionBonus:N0} cr." : "";

        _expedition = null;
        RendererInterop.PlayCue(ex.ExpeditionScientistsLost > 0 ? "alarm" : "reveal");
        RequestVaultSave();
        ShowPulseMessage(
            $"🛸 Away team home from {plan.SiteDisplayName} — expedition paid {pay:N0} cr ({brought}/{plan.TeamSize} back).{finds}{toll}");
        return true;
    }

    // mm:ss for the away clock (capped so a very long held budget still reads sanely).
    private static string FormatClock(double seconds)
    {
        if (double.IsPositiveInfinity(seconds) || seconds > 5999)
        {
            return "99:59";
        }
        int s = (int)Math.Max(0, Math.Round(seconds));
        return string.Create(CultureInfo.InvariantCulture, $"{s / 60}:{s % 60:D2}");
    }
}
