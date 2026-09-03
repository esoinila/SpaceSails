namespace SpaceSails.Core;

/// <summary>
/// Builds the co-moving berth state: the ship pinned onto a body's rail — a small radial offset out
/// from the body (in the Sun's frame), riding at the body's own orbital velocity. This is the ONE
/// construction behind every "the ship is at this berth" moment: a docked start, a shuttle arrival, a
/// vault-resume boot, and — since #269 — completing a manual clamp. Centralised so those four paths
/// can never build a berth four subtly different ways.
///
/// #269: before this, the ⚓ Dock button only set the docked flag and froze the arm at wherever the
/// ship happened to float — and the 500,000 km approach envelope meant "float" could be 100,000 km out
/// on a conic diving at the planet (the owner's Tilt arrival: "docked and about to slam into the
/// planet"). Completing a clamp now snaps the ship HERE: one body, one rail.
///
/// #742: there are now TWO constructions here and the split is the whole point. <see cref="CoMoving"/>
/// is the CLAMPED berth — the ship is pinned, the berth owns her position, and which way the standoff
/// points is cosmetic. <see cref="CoOrbital"/> is the FREE park — nobody is holding her, so the standoff
/// direction is not decoration, it is the orbit she will fly for as long as the captain is away. Every
/// call site is one or the other and says which.
/// </summary>
public static class BerthState
{
    /// <summary>The radial gap (metres) the berth sits off the body — just clear of the ~1 km station,
    /// well inside <see cref="DockRule.EnvelopeMeters"/>. Was a bare <c>3_000</c> repeated at every
    /// berth-building call site; named here so they all quote the same reach.</summary>
    public const double BerthOffsetMeters = 3_000;

    /// <summary>
    /// A ship state co-moving with <paramref name="bodyId"/> at <paramref name="simTime"/>:
    /// <paramref name="offsetMeters"/> radially outward from the body (from the Sun's frame), velocity
    /// equal to the body's orbital velocity by central difference. Bodies are deterministic from time,
    /// so this reconstructs the exact berth at any epoch — no stored orbit need cross a save boundary.
    /// </summary>
    /// <param name="bearingRadians">#1068 · Which way round the body the standoff points, measured from the
    /// Sun-outward direction this construction has always used. <b>Zero is that direction exactly</b>, so
    /// every caller with no roster to consult builds the berth it has always built, to the bit. The docking
    /// clamp passes <see cref="DockRoster.BearingAt"/> — which is how a station comes to have more than one
    /// slot to tie a hull up in, and therefore a slot it can move a hull to.</param>
    public static ShipState CoMoving(
        ICelestialEphemeris ephemeris, string bodyId, double simTime, double offsetMeters, double charge = 0,
        double bearingRadians = 0)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);

        const double h = 1.0;
        Vector2d pos = ephemeris.Position(bodyId, simTime);
        Vector2d vel = (ephemeris.Position(bodyId, simTime + h) - ephemeris.Position(bodyId, simTime - h)) / (2 * h);
        Vector2d outward = pos == Vector2d.Zero ? new Vector2d(1, 0) : pos.Normalized();
        if (bearingRadians != 0)
        {
            double cos = Math.Cos(bearingRadians);
            double sin = Math.Sin(bearingRadians);
            outward = new Vector2d(cos * outward.X - sin * outward.Y, sin * outward.X + cos * outward.Y);
        }
        return new ShipState(pos + outward * offsetMeters, vel, simTime, charge);
    }

    /// <summary>
    /// #742 · THE FREE PARK — the same "alongside that body" idea for a ship that is NOT clamped, laid
    /// along the body's own track instead of out along the Sun's radius.
    ///
    /// <para><b>Why the two are different constructions and not one.</b> Beside a station you clamp on
    /// immediately and the berth owns your position, so which way the 3 km standoff points is cosmetic.
    /// Beside a <i>massive</i> moon, with nothing to clamp to, the standoff direction IS the orbit:
    /// <see cref="CoMoving"/> lays it along the SUN-outward direction and hands over the body's velocity,
    /// and in the parent's frame that is not a park at all — it is a different ellipse. At Enceladus
    /// (rail radius 2.38037e8 m, period 118,387 s) the 1e7 m cycler standoff produced a Saturn-centric
    /// conic whose semi-major axis ranged over 2.193e8 … 2.592e8 m and whose period ranged over
    /// 104,768 … 134,641 s — up to 13.7 % off the moon's own — with e ≈ 0.041 at every phase, so
    /// periapsis and apoapsis <b>bracketed the moon's rail</b> and the mismatch then walked the ship
    /// around that rail until the two crossings coincided. One arrival phase in 24 (phase 2/24, epoch
    /// 9,866 s) closed and struck the ice at +9.54 h; two more (3/24 and 14/24) passed inside 3.2e5 and
    /// 5.8e5 m — one and two surface radii. The Sun-outward direction turns a full circle relative to the
    /// moon's track once per 32.9 h orbit, and the cycler window makes the arrival phase free, so which
    /// one you got was a coin toss the captain never saw.</para>
    ///
    /// <para><b>What this does instead.</b> Takes the body's own state about its parent and ROTATES it —
    /// position and parent-relative velocity together — by the arc that subtends the standoff, so the ship
    /// is on the body's own rail, trailing it. A rotation is an isometry: the radius is the body's radius
    /// to the metre and the speed is the body's speed to the metre per second, so the deposited conic is
    /// the body's own conic with a phase shift and nothing else. It cannot bracket the rail it lies on,
    /// and it is the same orbit at every arrival phase — the phase dependence is gone, not tuned away.
    /// Measured over 40 sim days at all 24 phases: closest approach 9.664e6 m (38.3 surface radii) at the
    /// 1e7 m cycler standoff, phase-to-phase spread under 0.1 %.</para>
    ///
    /// <para><b>Trailing, not leading, and the sign is read off the body.</b> A ship handed a rail state
    /// and then integrated does not fly the rail exactly — the scenario's Enceladus is 0.09 % faster than
    /// Kepler at its radius, so a free hull on that state has a period 377 s longer and falls slowly
    /// behind. Placed <i>behind</i> the moon that lag opens the gap; placed ahead it closes it (measured:
    /// 6.53e6 m at +30 h, versus 9.66e6 m trailing). "Behind" is read from the sign of the body's own
    /// angular momentum about its parent, so a retrograde moon (Triton's period is negative in
    /// <c>sol.json</c>) trails the correct way round.</para>
    ///
    /// <para>Falls back to <see cref="CoMoving"/> — deliberately, not by accident — for a body with no
    /// parent, a body that does not move, or a standoff too wide to be an arc on the rail: there is no
    /// track to lay the offset along, so the old construction is the only meaningful answer.</para>
    /// </summary>
    public static ShipState CoOrbital(
        ICelestialEphemeris ephemeris, string bodyId, double simTime, double offsetMeters, double charge = 0)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);

        CelestialBody? body = null;
        foreach (CelestialBody candidate in ephemeris.Bodies)
        {
            if (candidate.Id == bodyId)
            {
                body = candidate;
                break;
            }
        }

        if (body?.ParentId is null || !(offsetMeters > 0))
        {
            return CoMoving(ephemeris, bodyId, simTime, offsetMeters, charge);
        }

        const double h = 1.0;
        Vector2d pos = ephemeris.Position(bodyId, simTime);
        Vector2d parent = ephemeris.Position(body.ParentId, simTime);
        Vector2d rel = pos - parent;
        double radius = rel.Length;

        // The standoff is a CHORD on the rail, so the arc that subtends it is 2·asin(d / 2r): the gap
        // comes out at exactly the standoff asked for rather than a hair under it, which is what keeps
        // every "the berth sits this far off" assertion honest. A standoff wider than the rail's diameter
        // has no such arc at all.
        if (!(radius > 0) || offsetMeters >= 2.0 * radius)
        {
            return CoMoving(ephemeris, bodyId, simTime, offsetMeters, charge);
        }

        Vector2d vel = (ephemeris.Position(bodyId, simTime + h) - ephemeris.Position(bodyId, simTime - h)) / (2 * h);
        Vector2d parentVel =
            (ephemeris.Position(body.ParentId, simTime + h) - ephemeris.Position(body.ParentId, simTime - h)) / (2 * h);
        Vector2d relVel = vel - parentVel;

        // Which way is BEHIND? The sign of the body's angular momentum about its parent — read off the
        // body itself, never assumed prograde, because sol.json has a retrograde moon in it.
        double angularMomentum = rel.X * relVel.Y - rel.Y * relVel.X;
        if (angularMomentum == 0)
        {
            return CoMoving(ephemeris, bodyId, simTime, offsetMeters, charge);
        }

        double arc = -Math.Sign(angularMomentum) * 2.0 * Math.Asin(offsetMeters / (2.0 * radius));
        double cos = Math.Cos(arc);
        double sin = Math.Sin(arc);

        Vector2d rotatedRel = new(cos * rel.X - sin * rel.Y, sin * rel.X + cos * rel.Y);
        Vector2d rotatedVel = new(cos * relVel.X - sin * relVel.Y, sin * relVel.X + cos * relVel.Y);

        return new ShipState(parent + rotatedRel, parentVel + rotatedVel, simTime, charge);
    }
}
