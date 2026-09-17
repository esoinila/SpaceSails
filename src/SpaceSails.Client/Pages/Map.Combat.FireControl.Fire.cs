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

// Subject: part of Map.Combat.FireControl (#251 split; the header note lives in
// Map.Combat.FireControl.cs) — THE PRESS, THE LOCK, AND THE SHOT.

/// <summary>
/// #251 · EVERYTHING BETWEEN A SOLUTION AND A ROUND LEAVING THE TUBE: auto-aim, the deliberate FIRE
/// press (which re-solves at the current clock, so a stale lock can never be what fires), the scan
/// that finds the windows, the cancel, the countdown that fires itself when it expires, and F6's
/// instructor line.
///
/// <para>Split out of <c>Map.Combat.FireControl.cs</c> under #251 with no member renamed, re-scoped
/// or re-ordered, and not one field moved.</para>
/// </summary>
public partial class Map
{
    /// <summary>
    /// The one-button gunner (owner: "why does it not just point nose at this and fire with
    /// advance?"). Jump the aim to the shortest flight the round can physically make to the
    /// target's current range — switching to the missile when the slug can't live that long —
    /// solve there, and if the geometry refuses, sweep the windows. Computes only; FIRE stays
    /// a separate deliberate press.
    /// </summary>
    private void AutoAim()
    {
        if (FireLocked || InterestDistanceNow() is not { } distance)
        {
            return;
        }

        // The kinematic window bounds everything: outside it no straight shot exists at ANY
        // aim, and solving there just freezes the deck for nothing (the live test burned two
        // minutes escalating through six hopeless solves).
        if (StraightShotWindow() is not { } window)
        {
            ShowPulseMessage("No straight shot at any flight time — the drift outruns the muzzle; 🔭 sweeping for a gravity window");
            ScanFiringWindows();
            return;
        }

        double floorToF = window.MinToF * 1.03 + FireLockLeadSeconds;
        if (_fireKind == OrdnanceKind.Slug && floorToF > OrdnanceRule.SlugLifetimeSeconds - FireLockLeadSeconds)
        {
            _fireKind = OrdnanceKind.Missile;
            ShowPulseMessage("That window needs the missile — switched");
        }

        // A cross-system window means MINUTES of synchronous WASM solving per attempt (the
        // live test froze the deck ~10 min escalating blind). Auto-aim's job is the SIGNAL:
        // put the aim inside the window instantly and let SOLVE be the deliberate press.
        if (floorToF > 20 * 86400)
        {
            _fireAimOffsetSeconds = Math.Clamp(floorToF, 600, MaxFireAimOffsetSeconds);
            _fireSolution = null;
            ShowPulseMessage($"Aim set inside the window (+{FormatFlightTime(_fireAimOffsetSeconds)}) — press SOLVE (cross-system solves take a while)");
            StateHasChanged();
            return;
        }

        double ceiling = Math.Min(
            double.IsPositiveInfinity(window.MaxToF) ? MaxFireAimOffsetSeconds : window.MaxToF * 0.97,
            MaxFireAimOffsetSeconds);
        foreach (double factor in (double[])[1.0, 1.15, 1.5, 2.4])
        {
            double offset = Math.Clamp(floorToF * factor, 600, Math.Max(ceiling, 600));
            _fireAimOffsetSeconds = offset;
            ComputeFiringSolution();
            if (_fireSolution is { Converged: true } && _fireBlockedBy is null)
            {
                return;
            }

            if (offset >= ceiling)
            {
                break;
            }
        }

        // The straight window refused (gravity bends it away) — sweep as a last resort.
        ScanFiringWindows();
    }

    /// <summary>The deliberate FIRE press: re-solve at the current clock (no stale locks) and
    /// only then arm the T−60 s auto-release. Until this, everything upstream is just aiming.</summary>
    private void ArmFire()
    {
        if (FireLocked)
        {
            return;
        }

        if (!WeaponsAuthorized)
        {
            ShowPulseMessage("HOLD — the captain has not authorized the shot (desk 0)");
            return;
        }

        if (_fireKind == OrdnanceKind.Missile ? _missileAmmo <= 0 : _slugAmmo <= 0)
        {
            ShowPulseMessage($"Magazine empty — buy {(_fireKind == OrdnanceKind.Missile ? "missiles" : "slugs")} dockside");
            return;
        }

        ComputeFiringSolution();
        if (_fireSolution is { Converged: true } && _fireBlockedBy is null)
        {
            _fireAtSimTime = SimTime + FireLockLeadSeconds;
            _shotAuthorized = false; // one shot per captain's word (fire-at-will stands)
            ShowPulseMessage("BARREL LOCKED — round away in 60 s (scrub to abort)");

            // #528 · THE FIRST ROUND THIS CAPTAIN EVER FIRED. A smuggler becomes a pirate exactly once, and it
            // used to be a status line that faded in a second and a half. A PLATE rather than a card, because
            // this happens mid-fight and must not take the keyboard.
            RaiseStoryBeat(StoryBeats.Beat.FirstShotFired);
        }
        else if (_fireBlockedBy is not null)
        {
            ShowPulseMessage($"HOLD — {_fireBlockedBy} blocks the transfer; find another window");
        }
    }

    /// <summary>
    /// The porkchop assist (long-shots PR): a FIXED aim time is only feasible in certain launch
    /// windows — orbital mechanics, not the driver, decides when the geometry aligns. Sweep
    /// candidate aim times with the cheap seed probe, jump the slider to the best window and
    /// immediately run the full solve there.
    /// </summary>
    private void ScanFiringWindows()
    {
        if (_ephemeris is null || _simulator is null || FireLocked || InterestTargetState() is not { } target)
        {
            return;
        }

        PredictedPath predicted = PredictInterestPath(target, MaxFireAimOffsetSeconds);
        ShipState shooterAtFire = _simulator.RunAdaptive(_ship, FireLockLeadSeconds);

        double bestOffset = _fireAimOffsetSeconds;
        double bestMiss = double.MaxValue;
        const int probes = 7;
        // Grid floor = the shortest PHYSICAL flight to the target's current range — probing
        // aim times the round can't reach is wasted flights (and made knife-fight scans blind).
        double minOffset = Math.Clamp(
            (target.Position - _ship.Position).Length / MaxMuzzleSpeed * 0.8, 900, MaxFireAimOffsetSeconds / 4);
        double logSpan = Math.Log(MaxFireAimOffsetSeconds * 0.95 / minOffset);
        for (int i = 0; i < probes; i++)
        {
            double offset = minOffset * Math.Exp(logSpan * i / (probes - 1));
            double timeOfFlight = offset - FireLockLeadSeconds;
            if (timeOfFlight <= 0)
            {
                continue;
            }

            Vector2d aim = SamplePositionAtTime(predicted.Samples, SimTime + offset);
            (_, _, double miss) = FireControl.ProbeSeed(_simulator, shooterAtFire, MaxMuzzleSpeed, aim, timeOfFlight);
            if (miss < bestMiss)
            {
                (bestMiss, bestOffset) = (miss, offset);
            }
        }

        _fireAimOffsetSeconds = Math.Clamp(bestOffset, 600, MaxFireAimOffsetSeconds);
        ShowPulseMessage($"Window scan: best geometry at +{FormatFlightTime(bestOffset)} — solving there");
        ComputeFiringSolution();

        if (_fireSolution is { Converged: false } && _fireKind == OrdnanceKind.Slug)
        {
            ShowPulseMessage("No slug window inside its 2-day legs — switch to the MISSILE and scan again");
        }
    }

    private void CancelFiringSolution()
    {
        _fireAtSimTime = double.NaN;
        _fireSolution = null;
        _fireSolutionPath = [];
        _fireTargetPath = [];
        _fireBlockedBy = null;
        ShowPulseMessage("Firing solution scrubbed");
        StateHasChanged();
    }

    /// <summary>Auto-fires the locked solution the moment the countdown expires.</summary>
    private void CheckLockedFire()
    {
        if (!FireLocked || _fireSolution is not { } solution || SimTime < _fireAtSimTime)
        {
            return;
        }

        int cost = _fireKind == OrdnanceKind.Missile ? MissilePulseCost : SlugPulseCost;
        if (cost > _reactionMassPulses)
        {
            ShowPulseMessage($"No mass for the shot ({cost} pulses) — solution scrubbed");
            CancelFiringSolution();
            return;
        }

        if (_fireKind == OrdnanceKind.Missile ? _missileAmmo <= 0 : _slugAmmo <= 0)
        {
            ShowPulseMessage("Magazine empty — the shot is scrubbed");
            CancelFiringSolution();
            return;
        }

        _reactionMassPulses -= cost;
        if (_fireKind == OrdnanceKind.Missile)
        {
            _missileAmmo--;
        }
        else
        {
            _slugAmmo--;
        }

        FireOrdnance(_fireKind, solution.LaunchDirection, solution.MuzzleSpeed, _fireTargetId);
        _slewBearingRad = solution.BearingRad;
        _slewUntilSimTime = SimTime + 120; // the barrel swings back — control returns
        ShowPulseMessage($"ROUND AWAY — flight time {FormatDuration(solution.TimeOfFlightSeconds)} 🎯");
        _fireAtSimTime = double.NaN;
        _fireSolution = null;
        // Keep the planned transfer + aim point drawn while the round flies it — the whole
        // point of a weeks-long shot is watching the real slug ride the calculated line.
        StateHasChanged();
    }

    /// <summary>F6 — the solver as flight instructor: every locked solution teaches the lead.</summary>
    private string FireTip(FireControl.Solution solution)
    {
        Vector2d toAim = _fireAimPoint - _ship.Position;
        double direct = Math.Atan2(toAim.Y, toAim.X);
        double lead = (solution.BearingRad - direct) * 180.0 / Math.PI;
        while (lead > 180) { lead -= 360; }
        while (lead < -180) { lead += 360; }
        return $"Gunner's lesson: the round leads the mark by {Math.Abs(lead):F1}° and flies " +
            $"{FormatDuration(solution.TimeOfFlightSeconds)} — aim where they WILL be, never where " +
            "they are. Flying your own intercepts works exactly the same way.";
    }
}
