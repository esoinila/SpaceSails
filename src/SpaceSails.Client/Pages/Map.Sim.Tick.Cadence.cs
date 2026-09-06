using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Sim.Tick (the header note lives in Map.Sim.Tick.cs) — THE WORK THAT DOES NOT RUN EVERY
// FRAME. Three passes, each with its own clock rather than the frame's: the passes re-projected on a
// 300 ms cadence (which body we come nearest, which we could arm, sling off or skim, and what the
// destination's own departure would cost), the trajectory re-solved when it is due, and the arc cue,
// which is not a cadence at all but a RISING EDGE — sounded on the frame the arc starts and never again
// while it lasts.
public partial class Map
{
    /// <summary>The passes, on their own 300 ms cadence: which body we come nearest, which we could arm, which
    /// we could sling off, which we could skim — and what the destination's own departure would cost.</summary>
    private void ReprojectThePassesOnTheirCadence(double highResTimestampMs)
    {
        if (_passDirty && highResTimestampMs - _lastReprojectMs > 300)
        {
            _passDirty = false;
            _closestPass = null;
            _armablePass = null;
            _destinationPass = null;
            _slingablePass = null;
            _skimmablePass = null;
            _passes = [];
            if (_ephemeris is not null)
            {
                double bestArmable = double.MaxValue;
                double bestSling = double.MaxValue;
                double bestSkim = double.MaxValue;
                // #952: the pass list is KEPT this time round, not just folded into four fields. The arrive
                // step re-picks its candidate against the LIVE scrub (Map.Plot.Arrive) and re-judges its
                // valid/invalid bit off the same list, so dragging the scrub costs a lookup over a handful
                // of bodies instead of another 8000-sample scan.
                IReadOnlyList<ClosestApproach.Pass> passes = ClosestApproach.Passes(_samples, _ephemeris);
                _passes = passes;
                foreach (ClosestApproach.Pass pass in passes)
                {
                    if (_closestPass is null || pass.Severity < _closestPass.Value.Severity)
                    {
                        _closestPass = pass;
                    }

                    // Armable = tightest pass by a PLANET, even when the sun ranks more severe.
                    if (PassIsOrbitable(pass) is not null && pass.Severity < bestArmable)
                    {
                        (bestArmable, _armablePass) = (pass.Severity, pass);
                    }

                    // Slingable = tightest planet pass inside the body's Hill sphere (a real flyby the
                    // crank can bend), even when it's too fast/far to orbit. PR-G's panel handle.
                    if (PassIsSlingable(pass) && pass.Severity < bestSling)
                    {
                        (bestSling, _slingablePass) = (pass.Severity, pass);
                    }

                    // Skimmable = tightest pass by an atmosphere-bearing body — PR-I's corridor gauge handle.
                    if (PassIsSkimmable(pass) && pass.Severity < bestSkim)
                    {
                        (bestSkim, _skimmablePass) = (pass.Severity, pass);
                    }

                    if (pass.BodyId == _destinationBodyId)
                    {
                        _destinationPass = pass;
                    }
                }

                // #246: the destination's OWN planet (the void mode stops at its capture range) and the
                // solved cheap DEPARTURE the offer quotes — recomputed on the reprojection cadence. The
                // departure solve (not the current-coast Project) is what the offer keys off, so the button
                // is reachable from a berth or any coast (#249 fix). The current-coast Project stays too, but
                // only for the manual-coast PROMISE verdict line ("does NOT reach — closest pass X AU").
                _longHaulPlanet = LongHaulTargetPlanet(_destinationBodyId); // null unless a real void to cross
                _longHaulReach = _longHaulPlanet is { } lhPlanet ? LongHaul.Project(_ship, _ephemeris, lhPlanet) : null;
                _longHaulDeparture = _longHaulPlanet is { } lhp2 ? LongHaul.SolveDeparture(_ship, _ephemeris, lhp2) : null;
                // #267: price the destination departure's surface-clearance verdict on THIS cadence (once,
                // not per render) so the chip/card offer gate reads it cheaply — the arc-sampling scan is too
                // heavy to run every frame.
                _longHaulClearanceBlock = _longHaulPlanet is { } lhp3 && _longHaulDeparture is { Ok: true } lhDep
                    ? LongHaulClearanceBlock(lhDep, lhp3)
                    : null;
            }

            UpdateInterceptEstimate(); // M27: the war room's clock rides the same recompute
            UpdateCourseOpportunities(); // M29: what does this course conveniently brush by?
            // #952: does the plan still END SAFELY? Judged on the freshly rebuilt passes, so a mid-flight
            // edit or a missed burn flips the arrive row to ✗ and wakes the captain once.
            RefreshArriveValidity();
            // #989: …and can the plan be FLOWN as written at all? A cast off that is no longer the first
            // step, or a second one standing behind it, is a plan the ship cannot obey — judged on the same
            // cadence and woken with the same one-shot alarm, because both are "nobody is flying the ship".
            RefreshPlanShapeValidity();
        }
    }

    /// <summary>The ribbon itself, on two clocks: a 250 ms one for a horizon the captain just changed, and a
    /// sim-time one for a coast that has simply outrun the last projection.</summary>
    private void ReprojectTheTrajectoryWhenItIsDue(double highResTimestampMs)
    {
        if (_horizonDirty && highResTimestampMs - _lastHorizonReprojectMs > 250)
        {
            _horizonDirty = false;
            _lastHorizonReprojectMs = highResTimestampMs;
            ReprojectTrajectory();
        }

        if (_ship.SimTime >= _nextProjectionSimTime)
        {
            ReprojectTrajectory();
        }
    }

    /// <summary>Thunder, once per arcing episode.</summary>
    private void SoundTheArcOnItsRisingEdge()
    {
        // Thunder on the rising edge of an arc (M10 polish) — once per arcing episode.
        bool arcing = _plasma is not null && _ship.Charge >= ArcChargeThreshold;
        if (arcing && !_wasArcing)
        {
            RendererInterop.PlayCue("arc");
        }
        _wasArcing = arcing;
    }
}
