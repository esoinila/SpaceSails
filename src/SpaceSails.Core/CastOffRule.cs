namespace SpaceSails.Core;

/// <summary>
/// What kind of step a plotted flight-plan row is. Until #955 NAV-1 every plotted row was a burn and the
/// type said so by saying nothing; the owner's test story for the unified nav list needs two more —
/// <i>"the plan starts with an undock step recorded topmost in the nav-burn list, then safe-harbour
/// out-thrust to clear the vicinity of the station, then the actual burns"</i> — and a kind that no
/// executor can fly is exactly the bug class this repo has named twice (a sentence reporting one thing
/// while the sim does another). So the kinds are an enum, every switch over it is total, and a law test
/// walks <see cref="System.Enum.GetValues{TEnum}()"/> to prove each one has a label AND a branch.
/// </summary>
public enum PlanStepKind
{
    /// <summary>The clamp lets go. Costs nothing, moves nothing but the arm — the berth's own shove
    /// (the client's undock push) is what she leaves with.</summary>
    Undock,

    /// <summary>The safe-harbour out-thrust: a small burn straight away from the haven, sized by
    /// <see cref="CastOffRule"/> off the harbour's own reach, that carries her clear of the berth's
    /// traffic before the trip's real burns begin.</summary>
    ClearHarbour,

    /// <summary>An ordinary plotted burn — the seed of the list, unchanged since M4.</summary>
    Burn,
}

/// <summary>
/// #955 NAV-1 · <b>CASTING OFF IS A STEP, AND THE HARBOUR SIZES IT.</b>
///
/// <para>Owner's test story for the unified nav list (2026-08-23): <i>"plan while docked, then the plan
/// starts with an undock step recorded topmost in the nav-burn list, then safe-harbour out-thrust to clear
/// the vicinity of the station, then the actual burns, then the autopilot approach step, then the dock
/// step."</i> This is the law behind the middle clause — how big the out-thrust is, which way it points,
/// when the harbour is behind you — kept in Core so the row's ≈p, the plotted ribbon, the rehearsal that
/// judges the arrival and the burn the ship actually fires are four readings of ONE rule and never four
/// opinions.</para>
///
/// <h3>Where the numbers come from</h3>
/// <para>Both of them are <see cref="DockRule"/>'s, because the harbour's reach is the harbour's own
/// business and it is already written down once:</para>
/// <list type="bullet">
/// <item><b>How far is clear</b> — <see cref="DockRule.EnvelopeMeters"/>, the radius inside which the
/// clamp can still reach her (and inside which the ⚓ affordance still offers the berth), plus
/// <see cref="MarginFactor"/> so a drifting berth cannot re-acquire her the moment she stops watching.</item>
/// <item><b>How fast she leaves</b> — a share of <see cref="DockRule.MatchSpeed"/>, the fastest relative
/// speed that same law calls <i>matched</i>. A departure is not an arrival: she needs to be gone, not
/// stopped, so <see cref="DepartureShareOfMatchSpeed"/> takes a tenth of the arrival ceiling. At a Sol
/// berth that is a couple of pulses and she is still bound to the world she left — which is the point.
/// Leaving at the full ceiling would fling her clean out of Earth's well and make the cast-off, not the
/// transfer, the dominant burn of the trip.</item>
/// </list>
///
/// <para><b>OWNER KNOB.</b> <see cref="DepartureShareOfMatchSpeed"/> and <see cref="MarginFactor"/> are the
/// two numbers here that are a judgement rather than a consequence. Everything else is derived.</para>
/// </summary>
public static class CastOffRule
{
    /// <summary>How far beyond the harbour's own reach the clearance aims — a tenth again, so a berth that
    /// drifts toward her on its orbit cannot put her back inside the envelope she just left.</summary>
    public const double MarginFactor = 1.1;

    /// <summary>The share of the envelope law's matched speed a DEPARTURE leaves at (owner knob). A tenth:
    /// enough to be gone, small enough that the cast-off never outweighs the trip it begins.</summary>
    public const double DepartureShareOfMatchSpeed = 0.1;

    /// <summary>The separation at which the harbour is behind you: the clamp's reach, with margin.</summary>
    public static double ClearRangeMeters => DockRule.EnvelopeMeters * MarginFactor;

    /// <summary>The outbound relative speed the clearance thrusts to — a share of the speed the envelope
    /// law itself accepts, so she never leaves faster than the clamp would have taken her coming in.</summary>
    public static double OutboundSpeedMps => DockRule.MatchSpeed * DepartureShareOfMatchSpeed;

    /// <summary>The Δv the clearance still owes, given what the cast-off itself already handed her (the
    /// berth's shove is real speed and is not paid for twice). Never negative: a shove that already exceeds
    /// the target leaves nothing to buy.</summary>
    public static double DeltaVMps(double outboundSpeedAlready) =>
        Math.Max(0, OutboundSpeedMps - Math.Max(0, outboundSpeedAlready));

    /// <summary>What that Δv costs, priced with the one kernel every other burn in the game is priced with
    /// (<see cref="OrbitRule.PulsesFor"/> — a pulse is <see cref="OrbitRule.DeltaVPerPulseFraction"/> of the
    /// speed she carries). Zero only when the shove already did the whole job.</summary>
    public static int Pulses(double shipSpeedMps, double outboundSpeedAlready)
    {
        double deltaV = DeltaVMps(outboundSpeedAlready);
        return deltaV <= 0 ? 0 : OrbitRule.PulsesFor(deltaV, shipSpeedMps);
    }

    /// <summary>The per-pulse strength a clearance node burns at, as a percentage — the same tenth-of-a-
    /// percent-free kernel <see cref="Pulses"/> priced it with, so the node the ship flies delivers the Δv
    /// the row quoted rather than ten times it.</summary>
    public static double PulsePercent => OrbitRule.DeltaVPerPulseFraction * 100.0;

    /// <summary>Is the harbour behind her? The one predicate the step's own "done" reads and the test
    /// asserts, so "cleared" cannot mean two things.</summary>
    public static bool Cleared(double separationMeters) => separationMeters >= ClearRangeMeters;

    /// <summary>A straight-line estimate of how long the clearance takes from this separation — what the
    /// row says under its ≈p. Zero once she is already clear.</summary>
    public static double SecondsToClear(double separationMeters) =>
        Cleared(separationMeters) ? 0 : (ClearRangeMeters - separationMeters) / OutboundSpeedMps;

    // ===== The one voice =====

    /// <summary>The compose button: one press lays BOTH rows, because a cast-off that leaves you drifting
    /// in the traffic is not a cast-off.</summary>
    public const string ComposeButton = "⚓ + Cast off";

    /// <summary>What that button promises, for its tooltip.</summary>
    public const string ComposeHint =
        "Start the plan at the berth: the clamp lets go, then a small burn carries her clear of the harbour.";

    /// <summary>The sentence a clamped arm hears when the plan does not begin at the berth. Said in the
    /// ⚓ register the nav lock already speaks in (the client prefixes its own DockNavLockTip), never in a
    /// new voice.</summary>
    public const string ArmNeedsCastOff =
        "add ⚓ + Cast off to the top of the plan and she casts off by herself when it starts";

    /// <summary>The NOW line while the plan is casting her off — the pilot banner's own words. Said only
    /// when the clamp is letting go THIS INSTANT; a departure still ahead reads
    /// <see cref="WaitingAtTheBerth"/> instead, because a ship still tied up is not casting off.</summary>
    public static string CastingOffNow(string havenName) =>
        $"🛰 AUTOPILOT HAS THE SHIP — NOW: casting off from {havenName}";

    /// <summary>
    /// #989 · <b>THE NOW LINE WHILE THE DEPARTURE IS STILL AHEAD.</b> Owner, docked at The Red Eye with the
    /// scrub 33 h out (2026-08-22): <i>"Cast off time says in zero hours here even though the scrub is in 33
    /// hours?"</i> A SCHEDULED departure is a wait, and the banner says the wait: she is tied up, the captain
    /// has the ship, and the plan lets go at its own hour. Calling that "casting off now" was the sim doing
    /// one thing while a sentence reported another — this repo's third named bug class.
    /// </summary>
    public static string WaitingAtTheBerth(string havenName, string inWhen) =>
        $"🧭 YOU HAVE THE SHIP — docked at {havenName} · ⚓ the plan casts off {inWhen}";

    /// <summary>The banner / desk label for a step of this kind. TOTAL over
    /// <see cref="PlanStepKind"/> on purpose: a kind with no words is a kind the captain cannot read, and
    /// the law test walks the enum to prove none is missing.</summary>
    public static string StepLabel(PlanStepKind kind, string havenName, int pulses) => kind switch
    {
        PlanStepKind.Undock => $"⚓ cast off from {havenName}",
        PlanStepKind.ClearHarbour => $"🚀 clear the harbour at {havenName} · ≈{pulses} p",
        PlanStepKind.Burn => $"burn {pulses} p",
        // No silent default: the named kinds are all above, and a value that is not one of them is a
        // cast integer, not a step. It must not be given a plausible label.
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "no label for this step kind"),
    };

    /// <summary>What the row says when it is standing in the list — the glance line's own sentence for the
    /// two departure kinds. (A plain burn's glance line is the burn list's, unchanged; it is named here so
    /// the switch stays total and the law test can see it.)</summary>
    public static string GlanceLine(PlanStepKind kind, string havenName, int pulses, string when) => kind switch
    {
        PlanStepKind.Undock => $"⚓ cast off from {havenName} · the clamp lets go · {when}",
        PlanStepKind.ClearHarbour => $"🚀 clear the harbour · {FormatSpeed(OutboundSpeedMps)} out · ≈{pulses} p · {when}",
        PlanStepKind.Burn => $"burn {pulses} p · {when}",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "no glance line for this step kind"),
    };

    /// <summary>The clearance row's second line: how far is clear, and roughly how long from here.</summary>
    public static string ClearanceWhy(double separationMeters) =>
        Cleared(separationMeters)
            ? $"already clear — the harbour's reach is {FormatDistance(ClearRangeMeters)} and she is {FormatDistance(separationMeters)} out"
            : $"clear of the harbour at {FormatDistance(ClearRangeMeters)} — the clamp's reach ({FormatDistance(DockRule.EnvelopeMeters)}) and a margin";

    // The two formatters are ArrivalStepRule's, reused rather than re-typed so a distance reads the same
    // on a departure row as it does on the arrival row three rows below it.
    private static string FormatDistance(double metres) => ArrivalStepRule.FormatDistance(metres);

    private static string FormatSpeed(double mps) => ArrivalStepRule.FormatSpeed(mps);

    // ===== #989 — THE PLAN GRAMMAR: A SHIP LEAVES A BERTH ONCE =====
    //
    // Owner, off the second screenshot (2026-08-22): "there is something wonky with deletion of cast off
    // events also… there are two in a row now", and then the ruling: "2 cast-off in sequence sounds kind of
    // silly — we should have some logic check."
    //
    // A button guard would have been the cheap answer and the wrong one: the shape a plan is ALLOWED to have
    // is a law about the plan, not about the press that made it, and a plan can reach a bad shape without any
    // press at all (an old vault, a re-timed row that overtook the cast off, a burn dropped in front of the
    // clamp). So the law is HERE, pure, over the list of live step kinds — and the client reads the same
    // function twice: once to refuse an add, once every cadence to judge what is on the board.

    /// <summary>What is wrong with a plan's shape, or <see cref="None"/> when nothing is.</summary>
    public enum PlanShapeFault
    {
        /// <summary>Well-formed: at most one departure pair, first, and only while she is clamped.</summary>
        None,

        /// <summary>Two ⚓ cast-off rows. She can only leave a berth once.</summary>
        SecondCastOff,

        /// <summary>Two 🚀 clearance rows for one departure.</summary>
        SecondClearance,

        /// <summary>Something is plotted AHEAD of the clamp letting go — a burn the clamp would eat.</summary>
        CastOffNotFirst,

        /// <summary>The clearance is not the step straight after the cast off it belongs to.</summary>
        ClearanceOutOfPlace,

        /// <summary>A cast-off row in the plan of a ship that is already under way.</summary>
        CastOffWhileFree,
    }

    /// <summary>
    /// <b>Is this plan's shape legal?</b> Judged over the LIVE steps in plan order — what is still standing
    /// and still ahead, struck and flown rows excluded, because a departure that has already happened is not
    /// a second departure.
    ///
    /// <para>The three clauses, in the owner's words and in this order: <i>at most one cast-off pair</i>;
    /// <i>the pair is the FIRST steps when present</i>; <i>a cast off is present only while she is clamped</i>.
    /// The clearance is deliberately NOT bound by that last one — between the clamp letting go and the
    /// out-thrust firing she is free with a live 🚀 row, and that minute is the feature working, not a
    /// malformed plan.</para>
    /// </summary>
    /// <param name="liveSteps">The still-live step kinds, in the order they will be flown.</param>
    /// <param name="clamped">Is the ship on the clamp right now?</param>
    public static PlanShapeFault CheckShape(IReadOnlyList<PlanStepKind> liveSteps, bool clamped)
    {
        int castOffs = 0, clearances = 0, castOffAt = -1, clearanceAt = -1;
        for (int i = 0; i < liveSteps.Count; i++)
        {
            if (liveSteps[i] == PlanStepKind.Undock)
            {
                castOffs++;
                if (castOffAt < 0) castOffAt = i;
            }
            else if (liveSteps[i] == PlanStepKind.ClearHarbour)
            {
                clearances++;
                if (clearanceAt < 0) clearanceAt = i;
            }
        }

        if (castOffs > 1) return PlanShapeFault.SecondCastOff;
        if (clearances > 1) return PlanShapeFault.SecondClearance;
        if (castOffAt < 0) return PlanShapeFault.None;              // no departure: nothing here to judge
        if (!clamped) return PlanShapeFault.CastOffWhileFree;
        if (castOffAt != 0) return PlanShapeFault.CastOffNotFirst;
        if (clearanceAt >= 0 && clearanceAt != castOffAt + 1) return PlanShapeFault.ClearanceOutOfPlace;
        return PlanShapeFault.None;
    }

    /// <summary>Convenience over <see cref="CheckShape"/> for the places that only want the bit.</summary>
    public static bool ShapeIsWellFormed(IReadOnlyList<PlanStepKind> liveSteps, bool clamped) =>
        CheckShape(liveSteps, clamped) == PlanShapeFault.None;

    /// <summary>
    /// The one sentence the captain hears for a fault — the refusal when he presses, and the alarm when a
    /// plan on the board goes bad. TOTAL over <see cref="PlanShapeFault"/>: a fault with no words would be a
    /// plan flipped ✗ for a reason nobody can read.
    /// </summary>
    public static string ShapeComplaint(PlanShapeFault fault) => fault switch
    {
        PlanShapeFault.None => "the plan's shape is sound",
        PlanShapeFault.SecondCastOff =>
            "⚓ She can only leave this berth once — the plan already casts her off.",
        PlanShapeFault.SecondClearance =>
            "🚀 One departure, one clearance — the plan already carries the out-thrust that clears the harbour.",
        PlanShapeFault.CastOffNotFirst =>
            "⚓ Nothing in this plan can happen before the clamp lets go — the cast off has to be its first step.",
        PlanShapeFault.ClearanceOutOfPlace =>
            "🚀 The clearance belongs to the cast off — it has to be the step straight after it.",
        PlanShapeFault.CastOffWhileFree =>
            "⚓ She is already under way — a cast off left standing in the plan is a clamp that is not there.",
        _ => throw new ArgumentOutOfRangeException(nameof(fault), fault, "no words for this plan-shape fault"),
    };
}
