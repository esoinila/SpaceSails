using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Sim.Tick (the header note lives in Map.Sim.Tick.cs) — HOW FAST THE CLOCK IS ALLOWED TO RUN
// THIS FRAME. `UpdateEffectiveWarp` is the whole of the answer: free in a dock, where the ship is held
// fast and there is nothing to overshoot, and capped everywhere the integrator could step past something
// that matters. `DeepWellInsertionWarpCap` is the tightest of those caps — the room left between here and
// the body being closed on, priced in the closing speed, so a warp can never carry the ship through the
// insertion it was flying to make.
public partial class Map
{
    private void UpdateEffectiveWarp()
    {
        // Clamped in a dock: the ship is held fast (HoldAtDock overrides the integrator), so there's
        // nothing to overshoot or collide with — warp freely. This is what makes lying low to bleed
        // off heat a quick fast-forward (heat cools ~5 sim-days/level at a haven) instead of an
        // hours-long crawl under the near-body warp cap.
        if (_dockedHavenId is not null)
        {
            _effectiveWarp = Warp;
            return;
        }

        // Bound to a planet (M20)? No encounter to overshoot — let the orbit spin at up to
        // 1000x instead of crawling on the near-body tiers.
        if (OrbitInfo() is { } orbitInfo
            && OrbitRule.IsBound(_ship, _nearestBodyPosition, _nearestBodyVelocity, orbitInfo.Body, orbitInfo.Hill))
        {
            _effectiveWarp = Math.Min(Warp, 1000);
            return;
        }

        if (_nearestBody == null)
        {
            _effectiveWarp = Warp;
            return;
        }

        // Absolute tiers with a body-radius floor so the Sun's huge radius still gets a sane
        // (small) zone while planets use encounter-scale distances. Pure BodyRadius multiples
        // don't work: ×5000 on the Sun caps warp across ~23 AU, i.e. the whole inner system.
        double distance = (_ship.Position - _nearestBodyPosition).Length;
        double encounterRadius = Math.Max(1e9, _nearestBody.BodyRadius * 30);   // ~3 lunar distances at Earth
        double closeRadius = Math.Max(1e8, _nearestBody.BodyRadius * 6);
        double grazingRadius = _nearestBody.BodyRadius * 3;

        int cap = int.MaxValue;
        if (distance < grazingRadius)
        {
            cap = 10;
        }
        else if (distance < closeRadius)
        {
            cap = 100;
        }
        else if (distance < encounterRadius)
        {
            cap = 1000;
        }

        _effectiveWarp = Math.Min(Warp, cap);

        // A live capture window is a close encounter by definition: cap warp so the 60 s window
        // is actually holdable. Selection alone doesn't cap — only an engaged window.
        NpcState? captureTarget = SelectedCaptureTarget();
        if (captureTarget is not null && CaptureRule.IsInWindow(_ship, captureTarget.State))
        {
            _effectiveWarp = Math.Min(_effectiveWarp, CaptureWarpCap);
        }

        // #136: a deep-well moon's parking band is only tens of km wide — far thinner than the
        // grazing-tier step at 10×. When armed for such a moon, cap warp so one tick advances only
        // a fraction of the distance still to close, easing to 1× right at the band the way the
        // 60 s unit test threads it. Inert for planets/roomy moons (band far outside the grazing
        // radius) and when not armed. Keyed off the nearest body, which IS the armed one on final.
        _effectiveWarp = Math.Min(_effectiveWarp, DeepWellInsertionWarpCap(distance));
    }

    // The warp ceiling that keeps an armed deep-well insertion holdable (issue #136). Returns
    // int.MaxValue (no cap) unless the ship is armed for the nearest body and that body is a deep
    // well whose whole parking band sits inside its grazing radius.
    private int DeepWellInsertionWarpCap(double distanceToNearest)
    {
        if (_armedOrbitBodyId is null || _ephemeris is null || _nearestBody is null
            || _armedOrbitBodyId != _nearestBody.Id || _nearestBody.ParentId is null)
        {
            return int.MaxValue;
        }

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == _nearestBody.ParentId) { parent = candidate; break; }
        }
        if (parent is null) return int.MaxValue;

        double hill = OrbitRule.HillRadius(_nearestBody, parent.Mu);
        // #1179 · THE BAND SHE IS CLOSING ON, NOT THE ONE THE TIDE ALONE WOULD ALLOW. Both uses of `park`
        // below are about the ship's NEARNESS TO THE PARK SHE WILL FLY: the gate asks whether that band sits
        // inside the grazing radius, and `room` is literally the distance still to close to it. The armed
        // loop flies to #286's CLAMPED park, so the unclamped tide-stable radius was the wrong quantity for
        // both. At an inner moon whose cap cuts the park down inside the grazing radius, the old expression
        // read "roomy moon — the tiers suffice" and left warp at 10× while the ship was in fact closing on a
        // band tens of km wide; and where it did cap, it sized `room` off a band the autopilot never aims at.
        double park = KeptParkRadius(_nearestBody, hill, KeptRadiusCap(_nearestBody, parent));
        if (park >= _nearestBody.BodyRadius * 3 || distanceToNearest > OrbitRule.CaptureRange(hill))
        {
            return int.MaxValue; // roomy moon/planet, or not yet closing — the tiers suffice
        }

        // Advance at most ~⅓ of the room left to the band per 60 s tick; never below 1×. As the
        // ship reaches the band the room shrinks to a body radius and the cap eases to 1×.
        double closing = Math.Max(1.0, Math.Abs(OrbitRule.ClosingSpeed(_ship, _nearestBodyPosition, _nearestBodyVelocity)));
        double room = Math.Max(distanceToNearest - park, _nearestBody.BodyRadius);
        return Math.Max(1, (int)(room / (3 * 60 * closing)));
    }
}
