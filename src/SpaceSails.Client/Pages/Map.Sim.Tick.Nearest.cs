using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Sim.Tick (the header note lives in Map.Sim.Tick.cs) — #954 · WHAT THE SHIP IS NEAREST, WITH
// THE FLICKER TAKEN OUT OF IT. The reading used to be the literal per-frame minimum, which is why the HUD
// and the scope's AUTO lock alternated between a planet and its station twice per station orbit. Two laws
// hold it still and both live here: the incumbent keeps the slot until a challenger beats it by a margin
// measured along the sightline, and a satellite does not contest the slot at all until the ship is inside
// its Hill sphere — so the neighbourhood, not the body, is the unit, and the family is named together
// ("Mars › The Rusty Roadstead"). The four fields the HUD and the ⚓ hint read off it close the file.
public partial class Map
{
    // #954 — the nearest reading, with the flicker taken out of it. It used to take the literal minimum
    // every frame, which is why the HUD (and the scope's AUTO lock, which reads the same field) alternated
    // between Mars and The Rusty Roadstead twice per two-hour station orbit: from 0.16 AU the two ARE the
    // same distance, and "closest" was re-decided on a hair.
    //
    // TWO LAWS HOLD IT STILL, and the second is why the first was not enough. (1) The incumbent keeps the
    // slot until a challenger beats it by a real margin (NearestRule.Unseats). That band is measured along
    // the sightline, so it SHRINKS as the ship closes — and the same flicker was waiting at every range the
    // ship actually flies: 1,744 changes of mind in five orbits, parked 100,000 km off Earth. So (2) the
    // neighbourhood is the unit: a satellite does not contest the slot at all until the ship is inside its
    // Hill sphere (NearestRule.StandsForItself, and StandsForItself below), and its primary stands for the
    // whole family until then. The family is named together, "Mars › The Rusty Roadstead", by
    // UpdateNearestNeighbourhood.
    private void UpdateNearestBody()
    {
        // The incumbent, re-read from the live ephemeris (it may have been hidden or charted since) — and
        // only while it still stands for itself. A satellite the ship has pulled away from hands the slot
        // back to its primary instead of holding it from outside its own rail.
        CelestialBody? incumbent = _nearestBody is { } held && !IsBodyHidden(held.Id)
            ? _ephemeris!.Bodies.FirstOrDefault(b => b.Id == held.Id)
            : null;
        double incumbentDistSq = double.MaxValue;
        if (incumbent is not null)
        {
            Vector2d incumbentPos = _ephemeris!.Position(incumbent.Id, SimTime);
            if (StandsForItself(incumbent, incumbentPos))
            {
                incumbentDistSq = (_ship.Position - incumbentPos).LengthSquared;
            }
            else
            {
                incumbent = null;
            }
        }

        CelestialBody? challenger = null;
        double minDistanceSq = double.MaxValue;
        foreach (var body in _ephemeris!.Bodies)
        {
            if (IsBodyHidden(body.Id)) continue; // a hidden wreck is never "Nearest" until charted (PR-A)
            var bodyPos = _ephemeris.Position(body.Id, SimTime);
            // #954: out here it defers to what it goes round — the neighbourhood contests, not the family.
            if (!StandsForItself(body, bodyPos)) continue;
            double distSq = (_ship.Position - bodyPos).LengthSquared;
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                challenger = body;
            }
        }

        // With no incumbent the closest takes the slot outright; otherwise it has to earn it.
        _nearestBody = incumbent is null || (challenger is not null
            && NearestRule.UnseatsSquared(incumbentDistSq, minDistanceSq))
            ? challenger ?? incumbent
            : incumbent;

        if (_nearestBody is not null)
        {
            _nearestBodyPosition = _ephemeris.Position(_nearestBody.Id, SimTime);
            // Same numeric derivative as the ship's initial state — can't disagree with the ephemeris.
            const double h = 1.0;
            _nearestBodyVelocity = (_ephemeris.Position(_nearestBody.Id, SimTime + h)
                                  - _ephemeris.Position(_nearestBody.Id, SimTime - h)) / (2 * h);
        }

        UpdateNearestNeighbourhood();
    }

    // #954 · Is this body somewhere IN ITS OWN RIGHT from where the ship sits, or is it just a piece of the
    // thing it goes round? A planet (and the Sun, and a derelict on its own heliocentric rail) always
    // stands for itself. A satellite only does once the ship is INSIDE ITS HILL SPHERE — the same "you are
    // at this body" line the market (LocalMarketBody) and lying-low (IsHiddenAtHaven) already draw, so the
    // nearest slot now agrees with them instead of wandering off on its own.
    //
    // Why that line and not a bigger one: a satellite's distance from a parked ship swings between |D−a|
    // and D+a as its rail turns, so ANY threshold T it can cross produces a shell of hover ranges where it
    // crosses twice an orbit — that shell is D within a ± T. Setting T to the Hill radius (kilometres, for
    // bodies whose rails are hundreds of thousands) shrinks the shell to the moon's own capture width; a
    // roomier threshold widens it back into exactly the flicker the owner reported. A mass-less berth has
    // no Hill sphere at all, so it never takes the slot by drifting near — only by being clamped to, which
    // is why the dock is written in as its own clause.
    // <paramref name="bodyPos"/> is passed in because the caller has already paid for it — this runs once
    // per body per frame, so it takes the cheap outs first and never allocates (an explicit loop for the
    // parent, not a LINQ closure: the same reason the rest of this file looks up bodies the long way).
    private bool StandsForItself(CelestialBody body, Vector2d bodyPos)
    {
        if (_ephemeris is null || body.ParentId is not { } parentId)
        {
            return true; // the root orbits nothing
        }

        if (_dockedHavenId == body.Id)
        {
            return true; // clamped on: we are unarguably here (the berth's Hill sphere is zero)
        }

        CelestialBody? primary = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == parentId) { primary = candidate; break; }
        }

        if (primary is null || primary.ParentId is null)
        {
            return true; // a direct child of the root IS a neighbourhood — Mars never defers to the Sun
        }

        // Instantaneous separation, so an elliptical rail is judged on where it actually is (PR-B).
        double railNow = (bodyPos - _ephemeris.Position(primary.Id, SimTime)).Length;
        double hill = OrbitRule.HillRadius(railNow, body.Mu, primary.Mu);
        return NearestRule.StandsForItselfSquared((_ship.Position - bodyPos).LengthSquared, Squared(hill));
    }

    // #954 · The hierarchy the single "Nearest" slot could not hold. Owner: "present the hierarchy — Mars is
    // closest and it contains (in its Hill sphere) The Rusty Roadstead." Two shapes qualify, and they are
    // deliberately the SAME readout, so whichever of the pair happens to hold the slot the line reads alike:
    //   (a) the nearest thing itself orbits a body that orbits something else — a moon or a station, never
    //       the nonsense "Sun › Mars"; and
    //   (b) the nearest thing IS a planet, and one of its own dockable berths rides with it — the very body
    //       the readout used to flip to every orbit, and (since the neighbourhood law) the ordinary case,
    //       because out here the berth defers to the planet rather than taking the slot from it.
    private void UpdateNearestNeighbourhood()
    {
        _nearestParentName = null;
        _nearestChildName = null;
        _nearestHaven = null;

        if (_ephemeris is null || _nearestBody is not { } near)
        {
            return;
        }

        if (IsDockableHaven(near))
        {
            _nearestHaven = near;
        }

        // (a) A satellite of a satellite-bearing body: Phobos and the Roadstead qualify, Mars does not
        // (its parent is the sun, which orbits nothing — "Sun › Mars" is not a neighbourhood).
        CelestialBody? parent = near.ParentId is { } pid
            ? _ephemeris.Bodies.FirstOrDefault(b => b.Id == pid)
            : null;
        if (parent is { ParentId: not null })
        {
            _nearestParentName = parent.Name;
            _nearestChildName = near.Name;
            return;
        }

        // (b) The planet holds the slot — name the berth riding with it. A planet can hold more than one
        // (Earth has the gate out at Luna and the factory in low orbit), and picking the one nearest the
        // SHIP every frame just moves the flicker into the NAME: from ten million km those two swapped
        // every time the Moon came round. So which berth is named is decided in the frame the question
        // actually belongs to, and neither answer can blink:
        //
        //   · INSIDE the planet's Hill sphere the ship is in among them, and "nearest" means something —
        //     take the nearest to the ship, and hold it while the two are in the same breath (#966's law,
        //     doing here what it does for the slot);
        //   · OUTSIDE it, no berth is meaningfully nearer than another — the whole family is one dot at
        //     this range — so the line names the planet's OWN berth: the innermost, shortest rail. That is
        //     read off the rails, which do not turn, so it is the same answer every frame of every orbit.
        //
        // Either way the neighbourhood keeps naming a berth whenever it has one, which is what holds the ⚓
        // hint steady all the way in instead of dropping it somewhere on the approach.
        double nearDist = (_ship.Position - _nearestBodyPosition).Length;
        bool insideTheWell = parent is not null && nearDist < OrbitRule.HillRadius(near, parent.Mu);

        CelestialBody? haven = null;
        double havenDistSq = double.MaxValue;      // to the ship, inside the well; to the planet, outside it
        CelestialBody? incumbentHaven = null;
        double incumbentHavenDistSq = double.MaxValue;

        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (body.ParentId != near.Id || IsBodyHidden(body.Id) || !IsDockableHaven(body))
            {
                continue;
            }

            Vector2d berth = _ephemeris.Position(body.Id, SimTime);
            double d = insideTheWell
                ? (_ship.Position - berth).LengthSquared
                : (berth - _nearestBodyPosition).LengthSquared;
            if (d < havenDistSq)
            {
                (havenDistSq, haven) = (d, body);
            }

            if (body.Id == _neighbourhoodHavenId)
            {
                (incumbentHavenDistSq, incumbentHaven) = (d, body);
            }
        }

        // The incumbent keeps the line while the contest is too close to call — and outside the well every
        // contest is decided on rails that never move, so this only ever bites down among them.
        if (incumbentHaven is not null
            && NearestRule.InTheSameBreathSquared(incumbentHavenDistSq, havenDistSq))
        {
            haven = incumbentHaven;
        }

        _neighbourhoodHavenId = haven?.Id;

        if (haven is not null)
        {
            _nearestParentName = near.Name;
            _nearestChildName = haven.Name;
            _nearestHaven = haven;
        }
    }

    private static double Squared(double x) => x * x;

    // The neighbourhood the nearest reading belongs to: the containing body's name and the thing inside it,
    // or nulls when the nearest is just itself. Read by the HUD line and by the scope's AUTO sub-line.
    private string? _nearestParentName;
    private string? _nearestChildName;

    // The dockable haven this neighbourhood offers — the nearest body itself when IT is the haven, else the
    // one riding beside it. The ⚓ affordance hint follows this, so it no longer blinks out on the frames
    // the planet held the slot.
    private CelestialBody? _nearestHaven;

    // #954 · Which of the neighbourhood's berths the line is naming — the incumbent the hysteresis above
    // holds onto, so a planet with two of them doesn't trade their names every time a rail comes round.
    private string? _neighbourhoodHavenId;

    // The one line the "Nearest:" readout speaks — "Mars › The Rusty Roadstead" when there is a hierarchy
    // to present, the plain name when there is not.
    private string NearestReadoutName() =>
        _nearestParentName is { } p && _nearestChildName is { } c
            ? NearestRule.Hierarchy(p, c)
            : _nearestBody?.Name ?? "";
}
