namespace SpaceSails.Core;

/// <summary>What kind of work one sensor task asks of the telescope.</summary>
public enum SensorTaskKind
{
    /// <summary>A short directed look at a tracked ship's predicted position (custody pass).</summary>
    TrackUpdate,

    /// <summary>Point the telescope at a patch of sky and resolve what's there.</summary>
    AreaScan,

    /// <summary>Sweep the wedge covering a known trade lane from the ship's vantage.</summary>
    CorridorSweep,

    /// <summary>Search the expanding region where a lost track must still be.</summary>
    LostSearch,
}

/// <summary>
/// One entry in the sensor-tasks queue (SundaySecondPlan F1/F8): a unit of telescope work the
/// crew has asked for. The queue is the scanning desk's whole to-do list — what the single
/// onboard instrument does next is always one of these, in list order. Recurring tasks stay in
/// the carousel after completing (custody passes, standing corridor watches); one-shots leave.
/// </summary>
public sealed record SensorTask(
    string Id,
    SensorTaskKind Kind,
    string Label,
    bool Recurring,
    string? TargetShipId = null,
    Vector2d AreaCenter = default,
    double AreaRadius = 0,
    string? CorridorAId = null,
    string? CorridorBId = null)
{
    public static SensorTask TrackUpdate(string shipId, string label) =>
        new($"track:{shipId}", SensorTaskKind.TrackUpdate, label, Recurring: true, TargetShipId: shipId);

    public static SensorTask AreaScan(Vector2d center, double radiusMeters, string label) =>
        new($"area:{(long)Math.Round(center.X / 1e9)}:{(long)Math.Round(center.Y / 1e9)}",
            SensorTaskKind.AreaScan, label, Recurring: false, AreaCenter: center, AreaRadius: radiusMeters);

    public static SensorTask CorridorSweep(string aId, string bId, string label, bool recurring) =>
        new($"corridor:{aId}:{bId}", SensorTaskKind.CorridorSweep, label, recurring,
            CorridorAId: aId, CorridorBId: bId);

    public static SensorTask LostSearch(string shipId, string label) =>
        new($"search:{shipId}", SensorTaskKind.LostSearch, label, Recurring: true, TargetShipId: shipId);
}

/// <summary>One finished telescope pass: which task ran and over what sim-time span.</summary>
public readonly record struct CompletedPass(SensorTask Task, double StartTime, double CompleteTime);

/// <summary>
/// The single steerable telescope's schedule (vision: "It cannot look more than one way and one
/// focus at a time. We need to priorize in the scanning desk."). A carousel over an ordered task
/// list: the instrument works the queue top to bottom, spends each task's duration, then moves
/// to the next, wrapping around. Reordering the list changes when a task comes around;
/// <see cref="PrioritizeNext"/> jumps a task to the very next slot without reordering.
/// Deterministic: the passes emitted depend only on the operations applied and the durations the
/// caller supplies (durations are sampled once, at each pass's start).
/// </summary>
public sealed class TelescopeSchedule
{
    private readonly List<SensorTask> _queue = [];
    private SensorTask? _active;
    private bool _activeWasForced;
    private double _activeStart;
    private double _activeDuration;
    private int _nextIndex;
    private string? _forcedNextId;
    private double _clock;

    public IReadOnlyList<SensorTask> Queue => _queue;

    public SensorTask? Active => _active;

    public double ActiveStartTime => _activeStart;

    public double ActiveCompleteTime => _activeStart + _activeDuration;

    /// <summary>0..1 fraction of the active pass done at <paramref name="simTime"/>; 0 when idle.</summary>
    public double ActiveProgress(double simTime) =>
        _active is null || _activeDuration <= 0
            ? 0
            : Math.Clamp((simTime - _activeStart) / _activeDuration, 0, 1);

    public bool Contains(string taskId) => IndexOf(taskId) >= 0;

    /// <summary>Append a task to the queue. Refused (false) if the id is already queued.</summary>
    public bool Enqueue(SensorTask task)
    {
        if (Contains(task.Id))
        {
            return false;
        }

        _queue.Add(task);
        return true;
    }

    /// <summary>Remove a task. Removing the active task cancels its pass (nothing is emitted).</summary>
    public bool Remove(string taskId)
    {
        int index = IndexOf(taskId);
        if (index < 0)
        {
            return false;
        }

        _queue.RemoveAt(index);
        if (index < _nextIndex)
        {
            _nextIndex--;
        }

        if (_active?.Id == taskId)
        {
            _active = null;
            _activeWasForced = false;
        }

        if (_forcedNextId == taskId)
        {
            _forcedNextId = null;
        }

        return true;
    }

    public bool MoveUp(string taskId) => Swap(IndexOf(taskId), -1);

    public bool MoveDown(string taskId) => Swap(IndexOf(taskId), +1);

    /// <summary>The PRIORITIZE REDISCOVERY button: this task runs as soon as the current pass
    /// ends (the current look is never yanked mid-exposure), then the carousel resumes.</summary>
    public bool PrioritizeNext(string taskId)
    {
        if (!Contains(taskId))
        {
            return false;
        }

        _forcedNextId = taskId;
        return true;
    }

    /// <summary>
    /// The instrument was physically taken over (a manual sweep): the active pass is abandoned
    /// (nothing emitted — the exposure never finished) and the queue's clock consumes the time,
    /// so the carousel resumes from "now" when the instrument comes back, never back-filling.
    /// </summary>
    public void Interrupt(double simTime)
    {
        _active = null;
        _activeWasForced = false;
        _clock = Math.Max(_clock, simTime);
    }

    /// <summary>
    /// Run the telescope forward to <paramref name="toSimTime"/>. <paramref name="durationOf"/>
    /// prices a pass in sim seconds and is sampled once at the pass's start (use
    /// <see cref="SensorTaskGeometry"/>). Returns every pass completed in the window, in order.
    /// </summary>
    public IReadOnlyList<CompletedPass> Advance(double toSimTime, Func<SensorTask, double> durationOf)
    {
        List<CompletedPass>? completed = null;
        while (_clock < toSimTime)
        {
            if (_active is null)
            {
                if (_queue.Count == 0)
                {
                    break;
                }

                StartNext(durationOf);
            }

            double end = _activeStart + _activeDuration;
            if (end > toSimTime)
            {
                break;
            }

            _clock = end;
            (completed ??= []).Add(new CompletedPass(_active!, _activeStart, end));
            CompleteActive();
        }

        _clock = Math.Max(_clock, toSimTime);
        return completed ?? (IReadOnlyList<CompletedPass>)[];
    }

    private void StartNext(Func<SensorTask, double> durationOf)
    {
        int index;
        if (_forcedNextId is not null && (index = IndexOf(_forcedNextId)) >= 0)
        {
            _forcedNextId = null;
            _activeWasForced = true;
        }
        else
        {
            index = _queue.Count == 0 ? 0 : _nextIndex % _queue.Count;
            _activeWasForced = false;
        }

        _active = _queue[index];
        _activeStart = _clock;
        _activeDuration = Math.Max(1, durationOf(_active));
    }

    private void CompleteActive()
    {
        // A forced (prioritized) pass is an interjection: it must not advance the carousel
        // pointer, or prioritizing one task would silently skip another's turn.
        int index = IndexOf(_active!.Id);
        if (index >= 0)
        {
            if (_active.Recurring)
            {
                if (!_activeWasForced)
                {
                    _nextIndex = index + 1;
                }
            }
            else
            {
                _queue.RemoveAt(index);
                if (!_activeWasForced)
                {
                    _nextIndex = index;
                }
                else if (index < _nextIndex)
                {
                    _nextIndex--;
                }
            }

            _nextIndex = _queue.Count == 0 ? 0 : _nextIndex % _queue.Count;
        }

        _active = null;
        _activeWasForced = false;
    }

    private int IndexOf(string taskId) => _queue.FindIndex(t => t.Id == taskId);

    private bool Swap(int index, int delta)
    {
        int other = index + delta;
        if (index < 0 || other < 0 || other >= _queue.Count)
        {
            return false;
        }

        (_queue[index], _queue[other]) = (_queue[other], _queue[index]);
        return true;
    }
}

/// <summary>
/// Prices and aims sensor tasks: what wedge the telescope points at for a task, and how long the
/// pass takes. Pure geometry — reuses <see cref="ScanJob"/>'s sweep-rate so a task over a wide
/// patch of sky honestly costs more sim time than a narrow custody pass.
/// </summary>
public static class SensorTaskGeometry
{
    /// <summary>A custody pass on a held track: short, the cone says where to look.</summary>
    public const double TrackPassSeconds = 900;

    /// <summary>No pass is cheaper than this — slewing and settling are real.</summary>
    public const double MinPassSeconds = 600;

    /// <summary>Narrowest useful wedge (a directed look, not a survey).</summary>
    public const double MinArcRad = 2.0 * Math.PI / 180.0;

    /// <summary>The wedge that covers a disc of <paramref name="radiusMeters"/> around
    /// <paramref name="targetCenter"/> as seen from <paramref name="shipPosition"/>. Standing
    /// inside the disc means looking everywhere: a full-circle job.</summary>
    public static ScanJob WedgeToward(Vector2d shipPosition, Vector2d targetCenter, double radiusMeters)
    {
        Vector2d toTarget = targetCenter - shipPosition;
        double distance = toTarget.Length;
        if (distance <= radiusMeters || distance <= 0)
        {
            return new ScanJob(0, Math.Tau);
        }

        double halfArc = Math.Asin(Math.Clamp(radiusMeters / distance, 0, 1));
        return new ScanJob(TrackingStation.Bearing(toTarget), Math.Clamp(2 * halfArc, MinArcRad, Math.Tau));
    }

    /// <summary>
    /// Sim seconds one pass of <paramref name="task"/> takes when aimed as <paramref name="job"/>.
    /// <paramref name="speedFactor"/> is the telescope-upgrade axis: a better instrument slews and
    /// settles faster, so every pass shortens and the whole carousel revisits sooner.
    /// </summary>
    public static double Duration(SensorTask task, ScanJob job, double speedFactor = 1)
    {
        double baseSeconds = task.Kind == SensorTaskKind.TrackUpdate
            ? TrackPassSeconds
            : Math.Max(MinPassSeconds, job.DurationSeconds);
        return baseSeconds / Math.Max(0.1, speedFactor);
    }
}
