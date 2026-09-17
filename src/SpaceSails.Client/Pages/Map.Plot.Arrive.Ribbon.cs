using System.Globalization;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Plot.Arrive (#251 split; the header note lives in Map.Plot.Arrive.cs) —
// #952 · HOW LONG THE PLOTTED COURSE ACTUALLY IS, AND WHETHER IT REACHES THE PLAN'S OWN ENDING.
//
// A projection that stops short of its own arrival cannot honestly judge one: the pass at the very
// beginning of the ribbon is the ribbon's beginning and not an arrival, and the row says so instead
// of giving a verdict it cannot give. ReachTheArrivalWithTheRibbon is the fix the captain can press.
//
// #1042's leading approach lives here too, because it is the same question asked at the other end:
// how far, how fast that distance is closing, and how fast we are actually going, AS THE PICTURE
// OPENS.
//
// A pure move out of Map.Plot.Arrive.cs under #251 — no member renamed, re-scoped or re-ordered, and
// not one field moved.
public partial class Map
{
    // ===== #952 — how long the plotted course actually is, and whether it reaches the plan's own ending =====

    /// <summary>
    /// The projection's last sample and its own spacing there. Both facts come off <c>_samples</c> — the very
    /// list <c>ClosestApproach.Passes</c> swept — so "the ribbon ends here" is measured against the same
    /// world the pass was measured in, never against the requested horizon (which the adaptive projector's
    /// sample cap may not have reached). Null until there is a projection to speak of.
    /// </summary>
    private (double EndSimTime, double SampleStepSeconds)? RibbonEnd() =>
        _samples.Count < 2
            ? null
            : (_samples[^1].SimTime, Math.Max(1.0, _samples[^1].SimTime - _samples[^2].SimTime));

    /// <summary>
    /// #1042 — the projection's FIRST sample and its own spacing there: <see cref="RibbonEnd"/>'s sibling at
    /// the other end of the line. Read off <c>_samples</c> for the same reason — the ribbon's beginning has
    /// to be measured in the very world the pass was measured in. Null until there is a projection to speak
    /// of.
    /// </summary>
    private (double StartSimTime, double SampleStepSeconds)? RibbonStart() =>
        _samples.Count < 2
            ? null
            : (_samples[0].SimTime, Math.Max(1.0, _samples[1].SimTime - _samples[0].SimTime));

    /// <summary>#1042 — the approach to a body AS THE PICTURE OPENS: how far, how fast that distance is
    /// growing, and how fast the ship is moving relative to it, all at the ribbon's first sample. The
    /// velocities are read the one way the rest of this file reads them (<c>SampledVelocityAt</c> off the
    /// ribbon, <c>PassBodyVelocity</c> off the ephemeris), so this and the verdict cannot describe two
    /// ships.</summary>
    private (double Range, double RangeRate, double RelSpeed)? LeadingApproach(string bodyId)
    {
        if (_ephemeris is null || _samples.Count < 2)
        {
            return null;
        }

        double startSimTime = _samples[0].SimTime;
        Vector2d offset = _samples[0].Position - _ephemeris.Position(bodyId, startSimTime);
        double range = offset.Length;
        if (range <= 0)
        {
            return null;
        }

        Vector2d relVelocity = SampledVelocityAt(startSimTime) - PassBodyVelocity(bodyId, startSimTime);
        return (range, offset.Dot(relVelocity) / range, relVelocity.Length);
    }

    /// <summary>
    /// #1042 — is this "closest pass" only where the PICTURE begins? A body the ship has been opening from
    /// since before the plan existed has its sweep-reported minimum pinned to the ribbon's first sample, and
    /// with the scrub at zero that artefact beats every real encounter to the compose button.
    ///
    /// <para>Used ONLY to decide what may be OFFERED (<see cref="ArriveCandidate"/>). The arrive row's own
    /// verdict is deliberately left alone — see the note above
    /// <see cref="ArrivalStepRule.PassIsOffTheFrontOfTheRibbon"/> for why the two edges are not one law.</para>
    ///
    /// <para>Asked of every body in the system on every render of the two compose labels, so it takes the
    /// law's own cheap half first (<see cref="ArrivalStepRule.PassSitsAtTheRibbonsStart"/>) and reads the
    /// geometry only for the handful of passes that sit on the ribbon's front at all.</para>
    /// </summary>
    private bool PassIsOnlyTheRibbonsBeginning(ClosestApproach.Pass pass)
    {
        if (RibbonStart() is not { } start
            || !ArrivalStepRule.PassSitsAtTheRibbonsStart(pass.SimTime, start.StartSimTime, start.SampleStepSeconds))
        {
            return false;
        }

        return LeadingApproach(pass.BodyId) is { } approach
            && ArrivalStepRule.PassIsOffTheFrontOfTheRibbon(
                pass.SimTime, start.StartSimTime, start.SampleStepSeconds,
                approach.Range, approach.RangeRate, approach.RelSpeed);
    }

    /// <summary>#952 — is the arrival's pass only the end of the picture? See
    /// <see cref="ArrivalStepRule.PassIsOffTheEndOfTheRibbon"/> for why this is not a verdict.</summary>
    private bool ArriveRibbonIsTooShort() =>
        _arrive is { } step
        && ArrivePassFor(step.BodyId) is { } pass
        && RibbonEnd() is { } end
        && ArrivalStepRule.PassIsOffTheEndOfTheRibbon(pass.SimTime, end.EndSimTime, end.SampleStepSeconds);

    /// <summary>
    /// #952 — <b>PUT THE PLAN'S NEW ENDING ON THE LINE, AND DO IT ON THE PRESS.</b>
    ///
    /// <para>Path length on "auto" means "let the nav line pick its own length", and until the arrival became
    /// a step the furthest thing auto knew about was the last BURN. Plot two burns off Earth, end the plan at
    /// Mars nine months out, and the ribbon stopped at burn + 90 d — two hundred days short of the plan's own
    /// ending — with the row's ✗ computed off a "pass" that was really just where the line ran out.</para>
    ///
    /// <para>The arrival's epoch cannot be known independently of the ribbon; it is READ OFF the ribbon. So
    /// this converges it, bounded, in two turns: project (reaching for the cap while the pass is off the end
    /// — see <c>CurrentPlotHorizonSeconds</c>), sweep to find the real encounter on that longer line, then
    /// project again so the drawn ribbon settles back onto encounter + margin and the Path-length readout and
    /// the picture agree. Two turns is enough by construction — the second projection's horizon is computed
    /// from a pass that is already interior — and there is no third.</para>
    ///
    /// <para>Every step here is work the 300 ms cadence does anyway (<c>ReprojectThePassesOnTheirCadence</c>);
    /// doing it synchronously on a button press is what makes the press an ANSWER rather than a wrong number
    /// that quietly corrects itself a third of a second later. The cadence still runs after us — <c>_passDirty</c>
    /// is left set — and recomputes the same thing, so this is an early evaluation, never a second truth.</para>
    /// </summary>
    private void ReachTheArrivalWithTheRibbon()
    {
        if (_ephemeris is null || _simulator is null)
        {
            return;
        }

        for (int turn = 0; turn < 2; turn++)
        {
            ReprojectTrajectory();
            _passes = ClosestApproach.Passes(_samples, _ephemeris);
        }
    }

    /// <summary>The sentence the row speaks in place of a verdict it cannot honestly give. Null whenever the
    /// arrival IS judgeable (or there is no arrival), so a caller can print it unconditionally.</summary>
    private string? ArriveRibbonTooShortLine() =>
        _arrive is { } step && ArriveRibbonIsTooShort() && RibbonEnd() is { } end
            ? ArrivalStepRule.RibbonTooShort(BodyName(step.BodyId), FormatHorizon(end.EndSimTime - _ship.SimTime))
            : null;

    private Vector2d PassBodyVelocity(string bodyId, double simTime)
    {
        const double h = 1.0;
        return (_ephemeris!.Position(bodyId, simTime + h) - _ephemeris.Position(bodyId, simTime - h)) / (2 * h);
    }
}
