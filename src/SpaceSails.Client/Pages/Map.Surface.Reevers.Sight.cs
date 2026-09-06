using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — THE EYE, AND WHAT IT REPORTS.
// #465's sight list (the walls PLUS whatever doors are shut this instant — opacity and solidity are not the
// same property) and #858's kept WallIndex behind it, `IsDoorShut` which decides what a leaf is doing this
// frame, and the three proximity readings the nerve is priced from: how many movers are close enough to
// frighten, how far off the nearest Old One is, and whether the way up is contested. `SightBlockers` is read
// by a dozen files outside this one; the readings are read by Map.Surface.Nerve.cs.
public partial class Map
{
    // #465 · A SHUT DOOR IS OPAQUE. Owner, 2026-07-27: "the gun would be behind one door and not shooting
    // through it." Doors are not collision segments — the passage is always walkable, by law — so they never
    // entered the sight test, and the tube's built-in gun happily shot straight through a closed airlock.
    //
    // Opacity and solidity are NOT the same property (this is exactly the distinction #442 is about): a shut
    // door stops the eye and the round while never stopping the captain's boots. So sight queries get the
    // walls PLUS whatever doors are shut this instant, and collision keeps getting the walls alone.
    private readonly List<SurfaceCollision.Segment> _sightBlockers = [];

    // #858 · AND THE EYE IS HANDED THE INDEX, like everything that walks already is.
    //
    // Lab 45 measured what this list cost: the sightline sweep is strictly O(walls) at ~18–25 ns a segment,
    // it is 63% of a guard's whole per-frame bill on the 465-segment floor, and the SAME query against the
    // SAME walls filed into the SAME grid DeckPlan already carries (_deckPlan.CollisionField, #448) is 29×
    // faster at 436 segments and FLAT — 1.6× for 8× the stone. The legs were handed the index and the eye
    // was handed a plain list, one line apart, and nobody had noticed because the eye is small until it is
    // not. It also refilled that list EVERY frame, at O(walls), whether or not anybody was looking at
    // anything (0.0011 ms on B1) — and some callers ask for it inside a loop, once per candidate pair.
    //
    // So the list is filed into a WallIndex and KEPT. One source of truth: the index is built FROM
    // _sightBlockers, the very list this method used to return, so what the eye sweeps and what a hand-swept
    // list would sweep are the same segments by construction rather than by two authorities agreeing.
    //
    // WHEN IT IS REBUILT is the whole of the caching, and it is derived rather than timed: the stone changes
    // only when the plan does (a fresh deck, or #371's AppendRegion — both of which hand CollisionSegments a
    // NEW array, so reference identity is the honest generation token), and the door set changes only when a
    // door's shut-state actually flips. Those flips are still ASKED every frame — IsDoorShut is a handful of
    // doors and the renderer's own answer must never lag the sim's by a frame — but they are cheap, and a
    // frame that answers "the same doors are shut as last frame" does no work at all.
    private SurfaceCollision.WallIndex? _sightIndex;
    private SurfaceCollision.Segment[]? _sightStone;   // the stone _sightIndex was filed from, by identity
    private bool[] _sightDoorShut = [];                // …and which doors were shut when it was

    private IReadOnlyList<SurfaceCollision.Segment> SightBlockers()
    {
        DeckPlan.Door[] doors = _deckPlan.Doors;
        bool asFiled = _sightIndex is not null && ReferenceEquals(_sightStone, _deckPlan.CollisionSegments);
        if (_sightDoorShut.Length != doors.Length)
        {
            _sightDoorShut = new bool[doors.Length];
            asFiled = false;
        }
        for (int i = 0; i < doors.Length; i++)
        {
            bool shut = IsDoorShut(doors[i], i);
            if (_sightDoorShut[i] != shut)
            {
                _sightDoorShut[i] = shut;
                asFiled = false;
            }
        }
        if (asFiled)
        {
            return _sightIndex!;
        }

        _sightBlockers.Clear();
        foreach (SurfaceCollision.Segment seg in _deckPlan.CollisionSegments)
        {
            _sightBlockers.Add(seg);
        }
        for (int i = 0; i < doors.Length; i++)
        {
            if (!_sightDoorShut[i])
            {
                continue; // standing open — you can see (and shoot) straight down the tube
            }
            DeckPlan.Door d = doors[i];
            _sightBlockers.Add(new SurfaceCollision.Segment(d.X1, d.Y1, d.X2, d.Y2));
        }
        _sightStone = _deckPlan.CollisionSegments;
        _sightIndex = SurfaceCollision.WallIndex.Build(_sightBlockers);
        return _sightIndex;
    }

    // The same rule DeckView draws with (Core Airlock), so what blocks a shot is exactly what the player
    // sees closed — one door open at a time, the far end of an interlocked tube always shut.
    private bool IsDoorShut(DeckPlan.Door d, int index)
    {
        if (d.Locked)
        {
            return true;
        }

        // #563 · …AND A LEAF SOMETHING HAULED OVER STAYS OVER. Owner ruling, 2026-09-06: the Old Ones use
        // doors — an unlocked leaf opens for them over a beat, and they do not close it behind them.
        //
        // This is the second opener the paragraph below warns about, and it is legal for the one reason that
        // paragraph gives: the RENDERER learns it in the same instant. The state is not held here; it is on
        // the plan (DeckPlan.Leafs), and DrawTheDoors reads that same field on the same frame. One source of
        // truth, so a leaf can never be shut to the pen and open to a round, or the other way about.
        if (_deckPlan.LeafHeldOpen(index))
        {
            return false;
        }
        double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
        double toDoor = Math.Sqrt(((_avatarX - mx) * (_avatarX - mx)) + ((_avatarY - my) * (_avatarY - my)));

        // ONE RULE, AND IT IS THE ONE THE PLAYER CAN SEE. I briefly opened doors here for Reevers too, on
        // the owner's "unlocked doors should open for reevers" — and it broke the invariant this method
        // exists to hold, stated in the comment above it: the RENDERER decides a door is open from the
        // CAPTAIN's distance and nothing else. Adding a second opener here made the sim treat a door as
        // open while the deck drew it shut, so a gun fired through a door the player could see was closed
        // (owner, twice: "a reever was shot through a closed door").
        //
        // What blocks a shot must be exactly what the player sees closed. If Reevers are ever to work
        // doors, the RENDERER has to learn it at the same moment — one source of truth or none.
        //
        // #563 · THEY DO NOW, AND THAT IS THE SHAPE THIS PARAGRAPH ASKED FOR. The captain's distance is
        // still the only thing decided HERE; the leaf they hauled is decided ONCE, on the plan, and read
        // from there by this method and by the pen alike (see the clause above). What was rejected was a
        // second opener with its own private answer — not the Old Ones having a door.
        double nearestPartner = double.PositiveInfinity;
        if (d.Interlock != 0)
        {
            foreach (DeckPlan.Door other in _deckPlan.Doors)
            {
                if (other.Interlock != d.Interlock || other.Locked || other.Equals(d))
                {
                    continue;
                }
                double ox = (other.X1 + other.X2) / 2.0, oy = (other.Y1 + other.Y2) / 2.0;
                nearestPartner = Math.Min(nearestPartner,
                    Math.Sqrt(((_avatarX - ox) * (_avatarX - ox)) + ((_avatarY - oy) * (_avatarY - oy))));
            }
        }
        return !Airlock.MayOpen(toDoor, nearestPartner, DeckPlan.DoorOpenRadius);
    }

    // #446: the movers CLOSE ENOUGH TO FRIGHTEN — the same count, fenced to the dread range. The tracker
    // still hears every mover on the field (its fan is untouched, and a far blip is exactly the dread the
    // fan is for); this is only what the nerve is priced from, so a hunter you have time to walk away from
    // costs nothing. It also feeds the sighting spell, so a dot on the far rim no longer lands a jolt.
    private int CountMovingReeversWithin(double range)
    {
        double r2 = range * range;
        int n = 0;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - _avatarX, dy = r.Y - _avatarY;
            if (MotionTracker.IsMoving(r.Vx, r.Vy) && (dx * dx) + (dy * dy) <= r2)
            {
                n++;
            }
        }
        return n;
    }

    // #446: how far off the nearest Old One is, in deck units — infinity on an empty ground. Core prices the
    // whole sustained dread through this one number (NerveModel.Dread).
    private double NearestReeverRange()
    {
        double best = double.PositiveInfinity;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - _avatarX, dy = r.Y - _avatarY;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < best)
            {
                best = d2;
            }
        }
        return double.IsPositiveInfinity(best) ? best : Math.Sqrt(best);
    }

    // A net between the captain and the tube: an Old One wedged up-field (nearer the tube mouth than the
    // captain) and laterally close enough to block the sprint. Cheap geometry, matching the encirclement
    // the pack already leans into — the "cornered" the owner named, priced as a stressor.
    // #475 · CORNERED HAS TO MEAN CORNERED. Core prices this as "a net wedged between the captain and the
    // tube mouth" and charges the sharpest routine drain in the game for it — 5.0/s, more than a full-contact
    // chase — deliberately NOT discounted by range, because being cut off is not a distance term
    // (NerveModelTests.BeingCornered_IsCloseByDefinition_AndIsNeverDiscountedByRange pins that on purpose).
    //
    // The law was right; this predicate was not keeping its side of the bargain. It asked only for a contact
    // somewhere ABOVE the captain in a lateral lane, with no bound on how far above — so a single Old One
    // drifting forty deck units up, nowhere near anything, read as a net and billed the full 5.0/s. Three
    // captains in a row died on that: full gauge, never touched, killed by a dot on the far rim.
    //
    // A hunter you can comfortably walk around is not wedged between you and anywhere. So it only counts once
    // it is near enough to contest the escape — the same range at which Core says an Old One stops being
    // scenery, which keeps the two halves of the owner's ruling ("not unless they get REALLY close") agreeing.
    private bool IsCornered()
    {
        foreach (Reever r in _reevers)
        {
            if (r.Y > _avatarY + 1.0 && r.Y <= MoonSurface.SurfaceTopY + 0.5 &&
                Math.Abs(r.X - _avatarX) < CornerLateralRange)
            {
                double dx = r.X - _avatarX, dy = r.Y - _avatarY;
                if ((dx * dx) + (dy * dy) <= NerveModel.DreadRangeDeckUnits * NerveModel.DreadRangeDeckUnits)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
