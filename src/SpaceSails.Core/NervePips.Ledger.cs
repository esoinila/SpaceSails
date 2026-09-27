namespace SpaceSails.Core;

/// <summary>
/// #251 · THE EVENT, THE BEAT CLOCK AND THE LEDGER — what a pip change is, how a sustained cause ticks,
/// and the short ledger it is recorded in.
///
/// <para>Split out of <c>NervePips.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered. Its one field is a <c>const</c>.</para>
/// </summary>
public static partial class NervePips
{
    /// <summary>One pip moving, with the reason attached. <paramref name="Delta"/> is signed: negative
    /// spends pips, positive gives them back. <paramref name="Label"/> is the words the player reads —
    /// normally <see cref="Name"/> of the cause, but a one-off horror (a charge fired on a falling
    /// mountain, what the lab reveals) names itself, so the enum never has to grow a member per event.
    /// This is what the flash speaks and the ledger keeps.</summary>
    public readonly record struct Event(Cause Cause, int Delta, string Label)
    {
        /// <summary>An event that speaks its cause's standard words.</summary>
        public Event(Cause cause, int delta) : this(cause, delta, Name(cause))
        {
        }

        /// <summary>The line the player reads: the cause, and what it cost or gave.</summary>
        public string Line => $"{Label}  {(Delta >= 0 ? "+" : "−")}{System.Math.Abs(Delta)}";
    }

    // ── The beat clock ────────────────────────────────────────────────────────────────────────────────

    /// <summary>How long each sustained pressure has been building toward its next pip, plus the
    /// once-per-encounter touch latch. Carried by the client across frames and advanced by
    /// <see cref="Advance"/>; a cause that stops applying has its clock cleared, so pressure never banks
    /// silently between encounters.</summary>
    /// <param name="TouchSpent">Whether the shock of being caught has already been paid this encounter
    /// (owner: <i>"repeated strikes should not cost more of sanity"</i>). Re-arms the moment the captain
    /// is clear — safe up the tube, or with nothing close enough to frighten them.</param>
    public readonly record struct Beats(
        double Close, double Cornered, double Dig, double Airlock, bool TouchSpent = false,
        double Archive = 0.0)
    {
        /// <summary>Nothing building, and the next hand will be a fresh shock.</summary>
        public static Beats Fresh => new(0, 0, 0, 0, false, 0);

        /// <summary>This cause's accumulated seconds.</summary>
        public double For(Cause c) => c switch
        {
            Cause.Cornered => Cornered,
            Cause.DigUnderThreat => Dig,
            Cause.Airlock => Airlock,
            Cause.Archive => Archive,
            _ => Close,
        };

        /// <summary>This clock with one cause's accumulated seconds replaced.</summary>
        public Beats With(Cause c, double seconds) => c switch
        {
            Cause.Cornered => this with { Cornered = seconds },
            Cause.DigUnderThreat => this with { Dig = seconds },
            Cause.Airlock => this with { Airlock = seconds },
            Cause.Archive => this with { Archive = seconds },
            _ => this with { Close = seconds },
        };
    }

    /// <summary>Run one sustained cause's clock for <paramref name="dtSeconds"/>. Returns the carried-over
    /// seconds and how many WHOLE beats completed (normally 0 or 1; more only if a frame ran long, and
    /// they are all reported so a stutter never silently eats a pip). A cause that is not applying this
    /// frame resets to zero — pressure does not bank between encounters.</summary>
    public static (double Carried, int Beats) Tick(Cause c, double carried, bool applying, double dtSeconds)
    {
        if (!applying)
        {
            return (0.0, 0);
        }

        double period = BeatSeconds(c);
        double t = System.Math.Max(0.0, carried) + System.Math.Max(0.0, dtSeconds);
        if (period <= 0.0)
        {
            return (0.0, 0);
        }

        int beats = (int)System.Math.Floor(t / period);
        return (t - (beats * period), System.Math.Max(0, beats));
    }

    // ── The ledger ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many events the Captain desk keeps. Enough to reconstruct a bad minute, short enough
    /// to read at a glance — and the death card shows exactly this: <i>this is what broke you</i>.</summary>
    public const int LedgerDepth = 8;

    /// <summary>Fold new events onto a ledger, newest FIRST, bounded to <see cref="LedgerDepth"/>. Pure:
    /// returns a new list and never mutates the one handed in.</summary>
    public static IReadOnlyList<Event> Record(IReadOnlyList<Event> ledger, IReadOnlyList<Event> fresh)
    {
        if (fresh.Count == 0)
        {
            return ledger;
        }

        var next = new List<Event>(System.Math.Min(LedgerDepth, ledger.Count + fresh.Count));
        // Newest first: this frame's events (latest last in `fresh`, so walk it backwards), then history.
        for (int i = fresh.Count - 1; i >= 0 && next.Count < LedgerDepth; i--)
        {
            next.Add(fresh[i]);
        }
        for (int i = 0; i < ledger.Count && next.Count < LedgerDepth; i++)
        {
            next.Add(ledger[i]);
        }
        return next;
    }
}
