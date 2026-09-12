using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: #243 the conditions strip and #242 the frame-motion tip — the two readouts that answer "how
// close am I to being allowed to do this" and "why does nothing appear to be happening".
public partial class Map
{
    /// <summary>
    /// #243 · <b>WHICH GATE THE NAV SCREEN IS SPEAKING ABOUT, AND WITH WHAT NUMBERS.</b>
    ///
    /// <para>Owner: <i>"trying to be dockable, or capture etc — we always have the duo of relative speed and
    /// distance, and some criteria for those."</i> So this method's whole job is to answer WHICH duo. Every
    /// limit, every comparison and every word comes back out of <see cref="ConditionsGate"/>, which reads
    /// them off the code that enforces each gate; the page contributes only the live geometry, which is the
    /// one thing Core cannot know.</para>
    ///
    /// <para><b>Nothing is remembered.</b> There is no field behind this and none behind the trend either —
    /// the rate is #210's analytic range-rate, not a difference between two frames — which is why an
    /// instrument this size moved no row of <c>EveryFrameLeavesTheSameFingerprintTests</c>' sweep. A strip
    /// that had needed a "previous sample" field would have re-baselined thirty pinned hashes to draw an
    /// arrow, and that trade is not worth an arrow.</para>
    ///
    /// <para><b>The shuttle hop is deliberately not among the candidates.</b> <see cref="ConditionsGate.Hop"/>
    /// exists and is guarded — it is the one-criterion case that proves the instrument is not secretly
    /// hard-wired to two chips — but the Nav screen has no state in which a hop is the ship's OBJECTIVE: the
    /// hop is chosen at the shuttle-bay door on another desk, which already carries its own in-range board
    /// with its own trend (#368). Offering it here would mean inventing an objective the game does not have,
    /// and it would paint a red chip on every interplanetary trip the captain never meant to shuttle.</para>
    /// </summary>
    private ConditionsReading? ActiveConditions()
    {
        var offered = new List<ConditionsReading>(3);

        // The boarding window — the SAME target UpdateCapture runs the window on, so the strip cannot be
        // green about a hull the shuttles are not being launched at.
        if (SelectedCaptureTarget() is { } prey)
        {
            double distance = (prey.State.Position - _ship.Position).Length;
            double relSpeed = (prey.State.Velocity - _ship.Velocity).Length;
            offered.Add(ConditionsGate.Board(
                prey.Ship.Callsign, distance, relSpeed,
                RelativeMotion.ClosingSpeed(_ship.Position, _ship.Velocity, prey.State.Position, prey.State.Velocity)));
        }

        // The clamp — the affordance's own phase decides whether docking is the live intent (#212), exactly
        // as #200's focus panel decides it, and the chips ARE that panel's rows.
        if (DockFocusLive)
        {
            offered.Add(ConditionsGate.Dock(_dockAffordance, EffectiveDockTankPulses, DockAffordanceClosingSpeed()));
        }

        // The capture gate of an armed arrival — live exactly while an insertion is armed, which is the one
        // state in which "am I going to make this orbit" is a question about NOW rather than about a plan.
        if (ArmedOrbitApproach() is { } approach)
        {
            offered.Add(ConditionsGate.Orbit(
                approach.BodyName, approach.Distance, approach.RelSpeed, approach.HillRadius, approach.Closing));
        }

        return ConditionsGate.Active(offered);
    }

    /// <summary>#243/#210 · The signed range-rate to the haven the clamp affordance resolved to — the same
    /// haven every number in <see cref="DockFocus.Rows"/> is about, found by id so the arrow cannot end up
    /// describing a different body from the readings beside it.</summary>
    private double DockAffordanceClosingSpeed()
    {
        if (_ephemeris is null || _dockAffordance.HavenId is not { } havenId)
        {
            return 0;
        }

        const double h = 1.0;
        Vector2d position = _ephemeris.Position(havenId, SimTime);
        Vector2d velocity =
            (_ephemeris.Position(havenId, SimTime + h) - _ephemeris.Position(havenId, SimTime - h)) / (2 * h);
        return RelativeMotion.ClosingSpeed(_ship.Position, _ship.Velocity, position, velocity);
    }

    /// <summary>The armed insertion's live geometry, or null when nothing is armed. The Hill radius is the
    /// body's own against its parent — capture range is a function of the well, not a constant — read the
    /// same way <c>CheckArmedInsertion</c> reads it, so the strip and the autopilot judge one approach.</summary>
    private (string BodyName, double Distance, double RelSpeed, double HillRadius, double Closing)? ArmedOrbitApproach()
    {
        if (_armedOrbitBodyId is null || _ephemeris is null)
        {
            return null;
        }

        CelestialBody? body = null, parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == _armedOrbitBodyId) { body = candidate; }
        }
        if (body?.ParentId is null)
        {
            return null;
        }
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; }
        }
        if (parent is null)
        {
            return null;
        }

        const double h = 1.0;
        Vector2d position = _ephemeris.Position(body.Id, SimTime);
        Vector2d velocity =
            (_ephemeris.Position(body.Id, SimTime + h) - _ephemeris.Position(body.Id, SimTime - h)) / (2 * h);
        return (body.Name,
                (position - _ship.Position).Length,
                (velocity - _ship.Velocity).Length,
                OrbitRule.HillRadius(body, parent.Mu),
                RelativeMotion.ClosingSpeed(_ship.Position, _ship.Velocity, position, velocity));
    }

    /// <summary>
    /// #243 · <b>THE ONE LINE THE SCOPE CORNER MIRRORS.</b> Owner: <i>"Mirror the strip's summary in the
    /// Scope corner (where the eyes are during an approach)."</i> During an approach the captain is watching
    /// the glass, and the glass is where the answer has to be. Derived from the very reading the strip draws
    /// — one question asked once — so the corner and the column cannot disagree, and empty when no gate is
    /// live, which leaves the eyepiece exactly as it was.
    /// </summary>
    private string ConditionsScopeSummary() =>
        ActiveConditions() is { } conditions ? ConditionsGate.ScopeSummary(conditions) : string.Empty;

    /// <summary>True when every criterion of the live gate is met — the Scope's colour cue, and nothing
    /// more: the same dim green/amber pair #210 already uses down there, never a new alarm colour.</summary>
    private bool ConditionsScopeMet() => ActiveConditions() is { } conditions && conditions.AllInside;

    /// <summary>
    /// #242 · <b>"WHY AREN'T WE MOVING?"</b> Owner, post-undock: <i>"I was wondering why the ship don't
    /// move... the origin was set to Mars... after setting to Sun we got going. We should have some tip about
    /// that in burn planning."</i>
    ///
    /// <para>The judgement — when it speaks, and the sentence it speaks — is <see cref="FrameMotionTip"/>'s,
    /// in Core, so the razor has no threshold of its own and the guard cannot be reading a second copy of the
    /// owner's 15 %. All the page supplies is the pair of speeds, and it supplies them <b>through the
    /// functions that already print them</b>: <c>FrameRelativeVelocity</c> is the one arithmetic behind the
    /// frame row's "v rel Mars" AND the velocity arrowhead on the map (#135/#933), and the heliocentric
    /// speed is the ship's own. Two numbers already on the glass, connected — which is the whole issue.</para>
    ///
    /// <para><b>"Under thrust" is the plume, not a flag of its own.</b> <c>ThePlumeRightNow</c> is the drive
    /// flame the map is drawing this frame; asking it is asking the picture, so the line cannot appear over a
    /// ship that is visibly coasting.</para>
    /// </summary>
    private (string Line, string Readout)? FrameMotionTipLine()
    {
        if (_ephemeris is null || _plotFrameBodyId is null)
        {
            return null;
        }

        double frameSpeed = FrameRelativeVelocity(SimTime).Length;
        double helioSpeed = _ship.Velocity.Length;
        bool underThrust = ThePlumeRightNow.Intensity > 0;

        return FrameMotionTip.ShouldShow(PlotMode, underThrust, frameSpeed, helioSpeed, _plotFrameBodyId)
            ? (FrameMotionTip.Line(BodyName(_plotFrameBodyId)),
               FrameMotionTip.SharedMotionReadout(frameSpeed, helioSpeed))
            : null;
    }
}
