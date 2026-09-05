// #251 item 1 · THE TRACKING POST'S CODE-BEHIND, ON ITS OWN FILE.
//
// THE SCHEDULED INSTRUMENT (SundaySecondPlan PR-B): one telescope, worked top to bottom.
// Where a task points the scope, what a completed pass does to the ledger, how a lost lock
// becomes a cold case with a growing search area, and the API the map's scan menus enqueue into.
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
    // ---- SundaySecondPlan PR-B: the scheduled instrument ----

    private void RunScheduledInstrument(bool timeAdvanced)
    {
        if (_activeJob is not null && !_passiveJobRunning)
        {
            _schedule.Interrupt(SimTime); // a manual sweep owns the instrument; the queue waits
            return;
        }

        if (_schedule.Queue.Count > 0 && _passiveJobRunning)
        {
            _activeJob = null; // real tasks outrank the idle watch
            _passiveJobRunning = false;
        }

        if (!timeAdvanced)
        {
            return;
        }

        foreach (CompletedPass pass in _schedule.Advance(SimTime, DurationOf))
        {
            // #1068 · CHANNEL TWO — THE PASS THAT DOES NOT LAND. The telescope worked the job for its full
            // duration and the job left the carousel exactly as a finished one-shot does; what does not
            // happen is the RESULT. No fix, no ledger row, no reveal, no flash, and — the whole point —
            // no message: a desk that said "returned no data" would be the instrument reporting on itself,
            // which is the one thing #672's sensor law forbids. The failure IS the manifestation, and it
            // happens on one ground only, on the captain's own one-shot only. See PoliteDecline.
            if (Ephemeris is not null && PoliteDecline.BringsNothingBack(pass.Task, Ephemeris, pass.CompleteTime))
            {
                continue;
            }

            HandlePass(pass);
        }

        // #240 · THE BEAM'S OWN POSITION, reported while it is still moving — it was always 100 % because
        // the reveal hung on the completion instant. The #1068 gate applies here exactly as to a landed pass.
        if (_schedule.Active is { Kind: SensorTaskKind.AreaScan } live
            && !(Ephemeris is not null && PoliteDecline.BringsNothingBack(live, Ephemeris, _schedule.ActiveCompleteTime)))
        {
            ReportCoverage(live, _schedule.ActiveProgress(SimTime), SimTime);
        }
    }

    // #240 · The ONE place a coverage report is built.
    private void ReportCoverage(SensorTask task, double covered, double atTime) =>
        OnAreaScanCoverage.InvokeAsync(new AreaScanCoverage(
            task.AreaCenter, task.AreaRadius,
            SensorTaskGeometry.WedgeToward(ShipPosition, task.AreaCenter, task.AreaRadius),
            ShipPosition, covered, atTime));

    private double DurationOf(SensorTask task) =>
        SensorTaskGeometry.Duration(task, JobFor(task), Math.Max(0.1, TelescopeSpeedFactor));

    /// <summary>Where the telescope points for a task, as a sweep wedge from the ship — also
    /// what the map draws as the live scan area.</summary>
    public ScanJob JobFor(SensorTask task)
    {
        switch (task.Kind)
        {
            case SensorTaskKind.TrackUpdate:
            {
                Vector2d position = FindCandidate(task.TargetShipId!)?.State.Position
                    ?? (_ledger.TryGet(task.TargetShipId!, out TrackedTarget entry)
                        ? entry.LastObservation.Position
                        : ShipPosition);
                return SensorTaskGeometry.WedgeToward(ShipPosition, position, 0);
            }

            case SensorTaskKind.CorridorSweep:
            {
                foreach (CorridorRegion region in _corridorRegions)
                {
                    if (region.AId == task.CorridorAId && region.BId == task.CorridorBId)
                    {
                        return TradeCorridors.SweepJobFor(region, ShipPosition);
                    }
                }

                return new ScanJob(0, Math.Tau); // lane's anchors missing: survey the sky
            }

            case SensorTaskKind.LostSearch:
            {
                if (_lostTracks.TryGet(task.TargetShipId!, out LostTrack lost))
                {
                    return SensorTaskGeometry.WedgeToward(
                        ShipPosition, LostCenter(lost), lost.SearchRadius(SimTime));
                }

                return new ScanJob(0, SensorTaskGeometry.MinArcRad);
            }

            default: // AreaScan
                return SensorTaskGeometry.WedgeToward(ShipPosition, task.AreaCenter, task.AreaRadius);
        }
    }

    private void HandlePass(CompletedPass pass)
    {
        switch (pass.Task.Kind)
        {
            case SensorTaskKind.TrackUpdate:
            {
                string shipId = pass.Task.TargetShipId!;
                TrackingCandidate? candidate = FindCandidate(shipId);
                if (candidate is { } seen)
                {
                    if (_ledger.IsTracked(shipId))
                    {
                        if (Ephemeris is not null)
                        {
                            _ledger.TryConfirm(
                                shipId, Ephemeris, _telescope, ShipPosition, seen.State, pass.CompleteTime);
                        }
                    }
                    else
                    {
                        // #962 · THE ORDERED LOOK LANDED ON A CONTACT WE DO NOT HOLD. TryConfirm above only
                        // ever refreshes an entry that already exists, so this pass used to complete and do
                        // nothing whatsoever — the captain watched the telescope swing onto her and the
                        // ledger stayed empty. The fix a completed pass earns enters the ledger if a
                        // telescope is free; if every one is spoken for the look still HAPPENED, and the
                        // desk says which of the two it was rather than letting the job leave the list in
                        // silence.
                        var fix = new Observation(shipId, pass.CompleteTime, seen.State.Position, seen.State.Velocity);
                        _lastSweepMessage = _ledger.Add(fix)
                            ? $"{seen.Callsign} on the telescope ledger — fix taken"
                            : $"{seen.Callsign}: fix taken, but every telescope is held — drop a track to keep her";
                    }
                }

                _lastPassFlash = (shipId, DateTime.UtcNow);
                break;
            }

            case SensorTaskKind.LostSearch:
            {
                string shipId = pass.Task.TargetShipId!;
                if (!_lostTracks.TryGet(shipId, out LostTrack lost))
                {
                    break;
                }

                TrackingCandidate? candidate = FindCandidate(shipId);
                if (candidate is not null && Ephemeris is not null
                    && LostSearchRule.TryReacquire(Ephemeris, _telescope, lost, ShipPosition,
                        candidate.Value.State, pass.CompleteTime, out Observation found)
                    && _ledger.Add(found))
                {
                    _lostTracks.Drop(shipId);
                    _lostCenters.Remove(shipId);
                    _schedule.Remove(pass.Task.Id);
                    _lastPassFlash = (shipId, DateTime.UtcNow);
                    _lastSweepMessage = $"REACQUIRED {candidate.Value.Callsign} — lock restored";
                }
                else
                {
                    _lostTracks.RecordSearchPass(shipId, pass.CompleteTime); // ruled out sky
                }

                break;
            }

            case SensorTaskKind.CorridorSweep:
                RunDetectionSweep(JobFor(pass.Task), pass.CompleteTime, pass.Task.Label);
                break;

            default: // AreaScan: real ships in the wedge, plus whatever the populated sky holds
                RunDetectionSweep(
                    SensorTaskGeometry.WedgeToward(ShipPosition, pass.Task.AreaCenter, pass.Task.AreaRadius),
                    pass.CompleteTime, pass.Task.Label);
                _recentFinds = ScanDiscoveries.FindAt(
                    SkySeed, pass.Task.AreaCenter, pass.Task.AreaRadius, pass.CompleteTime);
                _lastSweepMessage =
                    $"{pass.Task.Label}: resolved {_recentFinds.Count} object(s) — {_recentFinds[0].Description}";
                // The whole wedge is behind the beam now (intel → scan → reveal, Tuesday plan PR-A): the
                // backstop for anything the live coverage did not catch, such as a contact that drifted
                // into the disc late in the pass.
                ReportCoverage(pass.Task, 1.0, pass.CompleteTime);
                break;
        }
    }

    private void RunDetectionSweep(ScanJob job, double completeTime, string label)
    {
        IReadOnlyList<Observation> found = TrackingStation.Sweep(
            _telescope, job, ShipPosition, Candidates.Select(c => (c.Id, c.State)), completeTime);
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

        if (found.Count > 0)
        {
            _lastSweepMessage = refused > 0
                ? $"{label} — {added} tracked, {refused} slipped (telescopes full)"
                : $"{label} — {added} contact(s) confirmed";
        }
    }

    private void HandleLostAndColdTracks()
    {
        foreach (TrackedTarget droppedTrack in _ledger.AdvanceTime(SimTime))
        {
            _lostTracks.AddFrom(droppedTrack, SimTime);
            _schedule.Remove($"track:{droppedTrack.ShipId}");
            string callsign = FindCandidate(droppedTrack.ShipId)?.Callsign ?? droppedTrack.ShipId;
            _schedule.Enqueue(SensorTask.LostSearch(droppedTrack.ShipId, $"search — {callsign}"));
            _lastSweepMessage = $"LOST LOCK on {callsign} — search area opened on the map";
        }

        foreach (LostTrack cold in _lostTracks.DropColdCases(SimTime))
        {
            _schedule.Remove($"search:{cold.ShipId}");
            _lostCenters.Remove(cold.ShipId);
            _lastSweepMessage =
                $"Trail gone cold on {FindCandidate(cold.ShipId)?.Callsign ?? cold.ShipId} — case closed";
        }

        // Keep the custody carousel in step with the ledger: every held track gets a standing
        // update pass; tasks whose subject left the ledger or case board leave the queue.
        foreach (TrackedTarget entry in _ledger.Entries)
        {
            _schedule.Enqueue(SensorTask.TrackUpdate(
                entry.ShipId, FindCandidate(entry.ShipId)?.Callsign ?? entry.ShipId));
        }

        for (int i = _schedule.Queue.Count - 1; i >= 0; i--)
        {
            SensorTask task = _schedule.Queue[i];
            bool stale = task.Kind switch
            {
                // #962 · A STANDING custody pass belongs to the ledger, and goes when the ledger lets go.
                // A one-shot SharpenFix look belongs to the captain who ordered it — it is on the list
                // PRECISELY BECAUSE she is not held — so it outlives this sweep and leaves on its own once
                // the glass has taken it. Sweeping it out here deleted 📡 sharpen fix's whole order one
                // tick after the press, every time the telescopes were already full.
                SensorTaskKind.TrackUpdate => task.Recurring && !_ledger.IsTracked(task.TargetShipId!),
                SensorTaskKind.LostSearch => !_lostTracks.IsLost(task.TargetShipId!),
                _ => false,
            };
            if (stale)
            {
                _schedule.Remove(task.Id);
            }
        }
    }

    /// <summary>Search-region center for a lost track — a PathPredictor dead-reckon, cached on
    /// a coarse cadence so the map can ask for it every frame.</summary>
    public Vector2d LostCenter(LostTrack lost)
    {
        if (_lostCenters.TryGetValue(lost.ShipId, out (double BuiltAt, Vector2d Center) cached)
            && SimTime - cached.BuiltAt < 1800)
        {
            return cached.Center;
        }

        Vector2d center = Ephemeris is null
            ? lost.LastObservation.Position
            : LostSearchRule.PredictedCenter(Ephemeris, lost, SimTime);
        _lostCenters[lost.ShipId] = (SimTime, center);
        return center;
    }

    /// <summary>What the instrument is doing right now, for the map overlay: the wedge, 0..1
    /// progress, and a label. Manual/passive sweeps and scheduled tasks all report here —
    /// there is only one telescope, so there is only ever one answer.</summary>
    public (ScanJob Job, double Progress, string Label)? CurrentScan
    {
        get
        {
            if (_activeJob is { } job)
            {
                return (job,
                    Math.Clamp((SimTime - _sweepStartSimTime) / Math.Max(1, job.DurationSeconds), 0, 1),
                    _passiveJobRunning ? "passive watch" : "manual sweep");
            }

            if (_schedule.Active is { } task)
            {
                return (JobFor(task), _schedule.ActiveProgress(SimTime), task.Label);
            }

            return null;
        }
    }

    /// <summary>The lost-track case board, for the map's search-region circles.</summary>
    public IReadOnlyCollection<LostTrack> LostTrackEntries => _lostTracks.Entries;

    /// <summary>The telescope's task queue, in carousel order (PR-D renders this as a list).</summary>
    public IReadOnlyList<SensorTask> TaskQueue => _schedule.Queue;

    /// <summary>What an area scan last resolved from the populated sky.</summary>
    public IReadOnlyList<Discovery> RecentFinds => _recentFinds;

    /// <summary>The last completed custody/search pass, for the map's bracket flash.</summary>
    public (string ShipId, DateTime WallTime)? LastPassFlash => _lastPassFlash;

    /// <summary>Telescope reach along a world direction from the ship — the same envelope the
    /// sweep uses, exposed so the map can draw the scan wedge honestly range-limited.</summary>
    public double TelescopeRangeAlong(Vector2d lookDirection) => _telescope.Range(ShipPosition, lookDirection);

    /// <summary>PR-C's entry point: the map's scan menus enqueue telescope work here.</summary>
    public bool EnqueueTask(SensorTask task)
    {
        bool added = _schedule.Enqueue(task);
        _lastSweepMessage = added ? $"Queued: {task.Label}" : $"{task.Label} is already queued";
        return added;
    }

    /// <summary>Tuesday plan PR-A: an intel-fed scan wants the scope pointed HERE, next — enqueue
    /// the area scan (or find it already queued) and jump it to the front of the carousel so the
    /// intel-driven hunt resolves as soon as the current look ends. Returns the task's id.</summary>
    public string EnqueueAndPrioritize(SensorTask task)
    {
        if (!_schedule.Contains(task.Id))
        {
            _schedule.Enqueue(task);
        }

        _schedule.PrioritizeNext(task.Id);
        _lastSweepMessage = $"Prioritized: {task.Label}";
        return task.Id;
    }

    /// <summary>PRIORITIZE REDISCOVERY: the search pass runs as soon as the current look ends.</summary>
    public void PrioritizeSearch(string shipId) => _schedule.PrioritizeNext($"search:{shipId}");

    /// <summary>Owner: "LOST CONTACT is big news — the ship should do all it can to re-acquire."
    /// The moment a shadowed target falls off our live fix, jump any open search for her (or her
    /// standing track pass) to the FRONT of the telescope queue so re-acquisition is the very next
    /// thing the scope does, ahead of routine passive watch. Safe no-op when there's nothing to
    /// jump to (e.g. a beacon-lit contact that was never truly lost).</summary>
    public void ForceReacquire(string shipId)
    {
        if (!_schedule.PrioritizeNext($"search:{shipId}"))
        {
            _schedule.PrioritizeNext($"track:{shipId}");
        }
    }
}
