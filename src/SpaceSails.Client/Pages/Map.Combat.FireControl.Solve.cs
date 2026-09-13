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
// Map.Combat.FireControl.cs) — THE NORDEN SOLUTION.

/// <summary>
/// #251 · WHERE ON THE PREY'S TRACK WE AIM, WHAT WE FIRE, AND THE SOLVE ITSELF — the aim offset and
/// the round that bounds it, the two predicted paths (the mark's, and our own plotted course a
/// pursuer would chase), and <c>ComputeFiringSolution</c>, which coasts the shooter to T+60 s and
/// runs the intercept from there.
///
/// <para>Split out of <c>Map.Combat.FireControl.cs</c> under #251 with no member renamed, re-scoped
/// or re-ordered, and not one field or <c>const</c> moved.</para>
/// </summary>
public partial class Map
{
    /// <summary>The longest shot the selected round can FLY: its lifetime, minus the lock
    /// lead. Beyond this the ordnance evaporates mid-flight — the silent guaranteed miss the
    /// old fixed 48 h slider quietly allowed (slug lived 6 h!).</summary>
    private double MaxFireAimOffsetSeconds => OrdnanceRule.LifetimeSeconds(_fireKind) - FireLockLeadSeconds;

    private void SetFireAimOffset(double seconds)
    {
        _fireAimOffsetSeconds = Math.Clamp(seconds, 600, MaxFireAimOffsetSeconds);
        if (!FireLocked)
        {
            _fireSolution = null; // a moved aim point voids an unlocked solution
        }
    }

    private void SetFireKind(OrdnanceKind kind)
    {
        _fireKind = kind;
        // Switching missile → slug with a weeks-long aim must pull the aim back inside what
        // the slug can actually fly.
        _fireAimOffsetSeconds = Math.Clamp(_fireAimOffsetSeconds, 600, MaxFireAimOffsetSeconds);
        if (!FireLocked)
        {
            _fireSolution = null;
        }
    }

    /// <summary>
    /// The aim-solution fork (core-gravity review, 2026-07-06): a freighter's future is gravity
    /// (PathPredictor dead-reckons it through the Simulator — gravity is public knowledge), but a
    /// hunter's future is the PURSUIT LAW: no gravity, +0.5 m/s² toward us every quantum. The old
    /// gravity dead-reckon put a hunter aim point ~½·a·τ² off — 13,000 km on a 2 h flight against
    /// the 5e5 m hit radius, a structural miss beyond point-blank. The fork replays the pursuit law
    /// itself (EncounterRule.PredictHunterPath) against our own plotted course; both arms honor the
    /// same PredictedPath contract, so SOLVE, the window scan, the orrery backdrop and the
    /// dispersion cone all stay target-agnostic. The hunter cone keeps budgets 0 — the pursuit law
    /// is known exactly, pod-thin; what really bends a hunter shot is leaving your own plot
    /// mid-flight (the collector chases the real you, not the plan).
    /// </summary>
    private PredictedPath PredictInterestPath((Vector2d Position, Vector2d Velocity) target, double horizonSeconds)
    {
        var observation = new Observation(_interestTargetId!, SimTime, target.Position, target.Velocity);
        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == _interestTargetId)
            {
                return new PredictedPath(observation,
                    EncounterRule.PredictHunterPath(hunter, PlayerPathForPrediction(), horizonSeconds),
                    ManeuverBudget: 0, ImpulseBudget: 0);
            }
        }

        return PathPredictor.Predict(_ephemeris!, observation, null, horizonSeconds,
            maneuverBudget: InterestManeuverBudget());
    }

    /// <summary>What a pursuer will actually chase: the plotted course — except BERTHED, where the
    /// plot shows a gravity coast the clamps will never allow; ride the dock's rails instead. The
    /// no-plot fallback (a beat before the first projection lands) is a straight coast.</summary>
    private IReadOnlyList<TrajectorySample> PlayerPathForPrediction()
    {
        if (_dockedHavenId is { } dockId && _ephemeris is not null)
        {
            double horizon = Math.Max(CurrentPlotHorizonSeconds, 2 * 86400);
            const int knots = 128;
            var docked = new List<TrajectorySample>(knots + 1);
            for (int k = 0; k <= knots; k++)
            {
                double t = SimTime + horizon * k / knots;
                docked.Add(new TrajectorySample(t, _ephemeris.Position(dockId, t) + _dockOffset));
            }

            return docked;
        }

        return _samples.Count >= 2
            ? _samples
            : [new TrajectorySample(SimTime, _ship.Position), new TrajectorySample(SimTime + 3600, _ship.Position + _ship.Velocity * 3600)];
    }

    /// <summary>The Norden beat: predict the mark, coast the shooter to T+60 s, run the
    /// shooting method, and — if it converges — lock, count down, auto-fire.</summary>
    private void ComputeFiringSolution()
    {
        if (_ephemeris is null || _simulator is null || FireLocked || InterestTargetState() is not { } target)
        {
            return;
        }

        _fireAimOffsetSeconds = Math.Clamp(_fireAimOffsetSeconds, 600, MaxFireAimOffsetSeconds);

        PredictedPath predicted = PredictInterestPath(target, _fireAimOffsetSeconds);
        _fireAimPoint = predicted.Samples[^1].Position;
        double tHit = SimTime + _fireAimOffsetSeconds;
        _fireDispersionMeters = predicted.HalfWidthAt(tHit);

        ShipState shooterAtFire = _simulator.RunAdaptive(_ship, FireLockLeadSeconds);
        FireControl.Solution solution = FireControl.Solve(_simulator, shooterAtFire, MaxMuzzleSpeed, _fireAimPoint, tHit);
        _fireSolution = solution;
        _fireTargetId = _interestTargetId;
        _revealedIterations = 0;
        _lastRevealMs = 0;

        if (solution.Converged)
        {
            // Computing is SAFE — nothing flies until the gunner presses FIRE (owner: locking
            // a solution is often the THREAT in a piracy stop; auto-firing it is the "oops").
            _fireAtSimTime = double.NaN;
            _fireTip = FireTip(solution);
            // The orrery view (owner + Gemini consult): fly the solved round once more and keep
            // the samples — the war-room backdrop draws the whole transfer, aim point and
            // dispersion on the live map, because a 100 M km shot cannot live inside a 5 M km
            // tactical circle.
            var round = new ShipState(
                shooterAtFire.Position,
                shooterAtFire.Velocity + solution.LaunchDirection * solution.MuzzleSpeed,
                shooterAtFire.SimTime);
            _fireSolutionPath = _simulator.ProjectAdaptive(round, null, solution.TimeOfFlightSeconds,
                maxSamples: Math.Max(64, (int)(solution.TimeOfFlightSeconds / 3600) + 16));
            _fireTargetPath = predicted.Samples;
            _fireBlockedBy = FirePlanBlockedBy();
            ShowPulseMessage("CALCULATING FIRING SOLUTION…");
            SquawkNow(Parrot.Squawk.FiringSolution, _lastTimestampMs ?? 0, force: true);
        }
        else
        {
            _fireAtSimTime = double.NaN;
            _fireTip = null;
            _fireSolutionPath = [];
            _fireTargetPath = [];
            _fireBlockedBy = null;
            ShowPulseMessage("No firing solution — beyond the driver's reach at that moment");
        }

        StateHasChanged();
    }
}
