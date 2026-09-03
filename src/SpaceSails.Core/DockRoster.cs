using System;

namespace SpaceSails.Core;

/// <summary>
/// #1068 · <b>THE DOCKMASTER'S ROSTER</b> — which slot a port ties a visiting ship up in, and where round
/// the station that slot sits.
///
/// <para>Every berth in this game was the same berth until now: <see cref="BerthState.CoMoving"/> pinned
/// the hull three kilometres out along the Sun's radius, at every station, on every arrival, for ever. That
/// was fine while a berth was a state change, and it stopped being fine the moment #1068's third channel
/// needed <b>a berth reassigned</b> to be a thing a captain could meet — because a harbour that has one
/// slot cannot reassign anybody to anything.</para>
///
/// <para><b>THE ROSTER IS DERIVED, NEVER AUTHORED PER PORT</b> — <see cref="ArrivalTube"/>'s own discipline,
/// for its own reason. How many berths a place keeps is read off the tube it has earned, which is read off
/// the scenario's traffic model; so the busy ports have a ring of slots and the outpost has the one collar,
/// and a scenario that adds a station gets a roster for free with no table to keep. The three counts below
/// are the only authored numbers in this file and each says what it is standing on.</para>
///
/// <para><b>AND THE ORDINARY BERTH IS STABLE.</b> A captain ties up in the same slot at the same port every
/// time he comes — that is what makes a different one legible at all. If the roster rolled a slot per
/// arrival, "they put me somewhere else today" would not be an observation, it would be the weather, and
/// the watchers' one quiet act would be invisible inside the noise it was supposed to hide in.</para>
///
/// <para>Nothing here reads <see cref="ArrivalTube.Tier"/> as anything but a count. The berth KIND is
/// untouched by every function in this file: a great port is still a run ashore and a working stop is still
/// a working stop (#1066/#1078), and the establishing shot is still the establishing shot, because a slot
/// number is not a tier and this file never returns one.</para>
/// </summary>
public static class DockRoster
{
    // ── HOW MANY BERTHS A PORT KEEPS ─────────────────────────────────────────────────────────────────────

    /// <summary>Berths at a great port. <b>Twelve, because the concourse ring is a twelve-sided hall</b> —
    /// the walkable interior every station shares has been telling the player a dozen ships are docked here
    /// since the first one was built (the hall's own remark: <i>"a 12-sided ring — 10 other berths' hatches
    /// sealed, so it reads like a dozen ships are docked"</i>). The roster agreeing with the architecture is
    /// worth more than a rounder number.</summary>
    public const int BerthsAtAGreatPort = 12;

    /// <summary>Berths at a working berth: <b>four</b> — a handful, which is what the tube's own caption
    /// describes (<i>"the berth number brushed on the frame by hand and painted over the last one"</i>: a
    /// place that repaints one number, not a place with a departures board). FLAGGED for owner tuning.</summary>
    public const int BerthsAtAWorkingBerth = 4;

    /// <summary>Berths at an outpost: <b>one</b>, and it is not a tuning knob. The outpost's whole
    /// description is that their collar comes across and mates to your lock — <i>"they were not expecting a
    /// ship, and there is nobody else here to expect one either"</i>. A place with one berth cannot reassign
    /// anybody, and that is the correct behaviour rather than a gap: the quiet hand has nowhere to move you
    /// to, so it does not move you.</summary>
    public const int BerthsAtAnOutpost = 1;

    /// <summary>How many berths this kind of port keeps.</summary>
    public static int BerthsAt(ArrivalTube.Tier tier) => tier switch
    {
        ArrivalTube.Tier.GreatPort => BerthsAtAGreatPort,
        ArrivalTube.Tier.WorkingBerth => BerthsAtAWorkingBerth,
        _ => BerthsAtAnOutpost,
    };

    /// <summary>…and how many this port keeps, asked of the scenario. One read of
    /// <see cref="ArrivalTube.TierFor"/>, which is the tier the arrival plate and the crew's ledger line
    /// already agree on.</summary>
    public static int BerthsAt(ICelestialEphemeris ephemeris, string havenId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        ArgumentNullException.ThrowIfNull(havenId);
        return BerthsAt(ArrivalTube.TierFor(ephemeris, havenId));
    }

    // ── WHICH BERTH ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1068 · <b>The berth this port has always given him</b> — seeded on the port and nothing
    /// else, so it never moves. Zero-based, always inside <c>[0, berths)</c>, and always zero at a port with
    /// one berth.</summary>
    public static int OrdinaryBerth(string havenId, int berths)
    {
        ArgumentNullException.ThrowIfNull(havenId);
        return berths <= 1 ? 0 : DiceRule.Roll(DiceRule.Seed($"berth:{havenId}"), berths).Face - 1;
    }

    /// <summary>#1068 · <b>The berth he is actually given</b> — the ordinary one, unless the harbour has
    /// retyped its roster overnight (<see cref="QuietHands.BerthOwedAt"/> hands over the window), in which
    /// case it is a <b>different</b> slot at the same port.
    ///
    /// <para><b>Different is guaranteed by construction, not by a retry.</b> The roll picks a STEP in
    /// <c>[1, berths − 1]</c> and walks round the ring from the ordinary slot, so the answer can never come
    /// back as the slot he already had — a reassignment that reassigned him to where he was standing is the
    /// feature silently doing nothing, and it is exactly what a re-roll-until-different loop would produce
    /// once in every <c>berths</c> arrivals with nobody able to tell.</para>
    ///
    /// <para>Seeded on <b>(port, the window the roster moved in)</b>, so it is the same slot for as long as
    /// that reassignment stands and it survives a reload.</para></summary>
    public static int BerthGiven(string havenId, int berths, long? reassignedInWindow)
    {
        ArgumentNullException.ThrowIfNull(havenId);

        int ordinary = OrdinaryBerth(havenId, berths);
        if (reassignedInWindow is not { } window || berths <= 1)
        {
            return ordinary;
        }

        int step = DiceRule.Roll(DiceRule.Seed($"berth:moved:{havenId}", window), berths - 1).Face;
        return (ordinary + step) % berths;
    }

    // ── AND WHERE THAT BERTH SITS ────────────────────────────────────────────────────────────────────────

    /// <summary>#1068 · Which way the ship's three-kilometre standoff points for this slot: the berths are
    /// spaced evenly round the station, and slot zero is the Sun-outward direction every berth in the game
    /// used before there were slots — so a one-berth port is bit-for-bit the berth it always was.</summary>
    public static double BearingOf(int berth, int berths) =>
        berths <= 1 ? 0 : 2 * Math.PI * (((berth % berths) + berths) % berths) / berths;

    /// <summary>#1068 · The whole question in one call, for the one caller that asks it: which way to pin a
    /// hull clamping on at this port right now. Reads the installed register through
    /// <see cref="QuietHands.BerthOwedAt"/>; the caller marks the reassignment spent.</summary>
    public static double BearingAt(ICelestialEphemeris ephemeris, string havenId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        ArgumentNullException.ThrowIfNull(havenId);

        int berths = BerthsAt(ephemeris, havenId);
        return BearingOf(BerthGiven(havenId, berths, QuietHands.BerthOwedAt(ephemeris, havenId)), berths);
    }
}
