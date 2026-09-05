// #251 item 1 · THE TRACKING POST'S CODE-BEHIND, ON ITS OWN FILE.
//
// THE SWEEP CONTROLS: the manual aim the two sliders write, starting and stopping a sweep,
// what a completed sweep puts on the ledger, and the two per-contact buttons (Confirm, Drop).
//
// PURE MOTION: the members below are the members that stood in TrackingPost.razor's @code block,
// character for character, at the same indentation and in the same order. Nothing was renamed,
// resignatured or reordered — the whole change is which file the lines sit in. The desk is one
// `partial class TrackingPost`; the razor file keeps the markup and nothing else. (Map.razor's own
// code came out into 155 `Map.*.cs` partials in 2026-07 for exactly this reason; the tracking post
// is the desk that never got the same treatment, which is how it reached 1,473 lines.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

public partial class TrackingPost
{
    // ---- Sweep controls ----

    private double SweepProgressPercent
    {
        get
        {
            if (_activeJob is not { } job || job.DurationSeconds <= 0)
            {
                return 0;
            }

            double elapsed = SimTime - _sweepStartSimTime;
            return Math.Clamp(elapsed / job.DurationSeconds * 100.0, 0, 100);
        }
    }

    private void OnBearingInput(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out double v))
        {
            _centerBearingDeg = v;
        }
    }

    private void OnArcInput(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out double v))
        {
            _arcWidthDeg = v;
        }
    }

    private void StartSweep()
    {
        double bearingRad = _centerBearingDeg * Math.PI / 180.0;
        double arcRad = Math.Clamp(_arcWidthDeg, 1, 360) * Math.PI / 180.0;
        _activeJob = new ScanJob(bearingRad, arcRad);
        _sweepStartSimTime = SimTime;
        _passiveJobRunning = false; // a manual sweep takes the instrument
        _lastSweepMessage = "Sweeping…";
    }

    private void StopSweep()
    {
        if (_passiveJobRunning)
        {
            // Stopping the watch itself — otherwise it would just restart next tick.
            _passiveWatch = false;
            _passiveJobRunning = false;
            _lastSweepMessage = "Passive watch stood down";
        }
        else
        {
            _lastSweepMessage = "Sweep aborted";
        }

        _activeJob = null;
    }

    private void CompleteSweep(ScanJob job)
    {
        bool wasPassive = _passiveJobRunning;
        _activeJob = null;
        _passiveJobRunning = false;
        IEnumerable<(string Id, ShipState State)> candidates = Candidates.Select(c => (c.Id, c.State));
        IReadOnlyList<Observation> found = TrackingStation.Sweep(_telescope, job, ShipPosition, candidates, SimTime);

        int added = 0, refused = 0;
        foreach (Observation obs in found)
        {
            if (_ledger.Add(obs))
            {
                added++;
            }
            else
            {
                refused++;
            }
        }

        // The passive watch stays quiet unless it actually found something new.
        _lastSweepMessage = found.Count == 0
            ? wasPassive ? _lastSweepMessage : "Sweep complete — nothing found"
            : refused > 0
                ? $"{(wasPassive ? "Passive watch" : "Sweep complete")} — {added} tracked, {refused} slipped (telescopes full)"
                : $"{(wasPassive ? "Passive watch" : "Sweep complete")} — {added} contact(s) found";
    }

    private TrackingCandidate? FindCandidate(string id)
    {
        foreach (TrackingCandidate c in Candidates)
        {
            if (c.Id == id)
            {
                return c;
            }
        }

        return null;
    }

    private void ConfirmNow(string shipId)
    {
        if (Ephemeris is null)
        {
            return;
        }

        TrackingCandidate? candidate = FindCandidate(shipId);
        if (candidate is null)
        {
            _lastSweepMessage = "Can't confirm — contact isn't in range of the sim right now";
            return;
        }

        bool ok = _ledger.TryConfirm(shipId, Ephemeris, _telescope, ShipPosition, candidate.Value.State, SimTime);
        _lastSweepMessage = ok
            ? $"Reconfirmed {candidate.Value.Callsign}"
            : $"Lost the fix on {candidate.Value.Callsign} — try a fresh sweep";
    }

    private void Drop(string shipId) => _ledger.Drop(shipId);
}
