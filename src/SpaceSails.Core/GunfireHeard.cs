using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #803 · A SHOT IS A FACT THE WORLD KEEPS. The captain's remote can now fire a gun deliberately, indoors,
/// at a door — and the interesting half of that is not the door.
///
/// <para><b>Why a record and not just a noise.</b> The ground has had an ear since #456: a Reever hears
/// gunfire at <c>ReeverHearing.RangeOf(Noise.Gunfire)</c> and walks to the PLACE it came from. That call is
/// fire-and-forget — it moves some goals and returns, and a second later nothing anywhere can say a shot
/// was ever fired. Everything that will want to price this (the guards' lane, #804: patrols that came to
/// look, a sweep team that heard the third one, a facility that is now expecting somebody) needs the fact
/// itself: <b>who fired, at what, where, when, and how many</b>.</para>
///
/// <para><b>#618 · AND NOW SOMETHING READS IT.</b> This class shipped saying "this build does not react",
/// filed from the first shot so that whoever priced it would be reading a record rather than re-deriving
/// one from a HUD line that had scrolled away. The round is what prices it, and it prices it out of exactly
/// the two questions that were left here for it: <see cref="WithinEarshot"/>, whose doc comment has said
/// since the day it was written that it is <i>the question #804 will ask of every patrol on the floor</i>,
/// and <see cref="NearestEar"/> beside it. A seam that is written only when somebody needs it is a seam
/// that is written wrong; this one was written first and has not had to move.</para>
/// </summary>
public static class GunfireHeard
{
    /// <summary>One shot, as the world will remember it.</summary>
    /// <param name="Unit">The gun that fired. A captain's own machine, named, because whose gun it was is
    /// the first thing anybody asking about it will want.</param>
    /// <param name="At">What it was fired at, in the world's own words — the sign on the door.</param>
    /// <param name="SimTime">When, on the ship's clock, so a later reader can say how long ago.</param>
    /// <param name="Rounds">How many went down range.</param>
    public readonly record struct Shot(
        string Unit, string At, double X, double Y, double SimTime, int Rounds);

    /// <summary>How far a shot carries, in deck units. NOT a second number: it is the ear the ground has
    /// used since #456, asked of the one place that owns it.</summary>
    public static double EarshotDu => ReeverHearing.RangeOf(ReeverHearing.Noise.Gunfire);

    /// <summary>Was somebody standing here close enough to have heard it? The question #804 will ask of
    /// every patrol on the floor, written down here so it is asked once and answered the same way.</summary>
    public static bool WithinEarshot(Shot shot, double x, double y)
    {
        double dx = x - shot.X, dy = y - shot.Y;
        return (dx * dx) + (dy * dy) <= EarshotDu * EarshotDu;
    }

    /// <summary>
    /// #618 · <b>WHO HEARS IT — the nearest pair of ears inside earshot, or −1 when nobody is close enough.</b>
    ///
    /// <para>Range and nothing else, which is not a shortcut and is not a second model: it is
    /// <see cref="WithinEarshot"/> asked of a list, and <see cref="WithinEarshot"/> is the ear this ground
    /// has had since #456. There is deliberately no wall term. <c>PatrolBeat.Heard</c> — the boots a captain
    /// hears through the rock — already states the reason in one line (<i>sound goes round corners</i>), and a
    /// gun is not quieter than a man walking. A second acoustics with an attenuation count in it would be two
    /// answers to <i>how far does a noise carry on this floor</i>, which on this repo is the oldest bug class
    /// there is.</para>
    ///
    /// <para><b>The NEAREST, and only one.</b> A bang is one thing to walk toward, not a thing every man on
    /// the rota converges on: the whole feature is one man leaving his round to go and look, and a floor-wide
    /// response would be the hunt #835 deliberately did not build.</para>
    /// </summary>
    /// <param name="shot">The shot, as it was filed.</param>
    /// <param name="ears">Where everybody who could hear it is standing, in the caller's own order — which
    /// fixes the answer when two of them are exactly equidistant, so the same floor always hands the same
    /// man the errand.</param>
    /// <returns>The index into <paramref name="ears"/>, or −1.</returns>
    public static int NearestEar(Shot shot, IReadOnlyList<(double X, double Y)>? ears)
    {
        int nearest = -1;
        double best = double.PositiveInfinity;
        for (int i = 0; i < (ears?.Count ?? 0); i++)
        {
            (double x, double y) = ears![i];
            if (!WithinEarshot(shot, x, y))
            {
                continue;
            }

            double dx = x - shot.X, dy = y - shot.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < best)
            {
                best = d2;
                nearest = i;
            }
        }
        return nearest;
    }

    /// <summary>
    /// #618 · <b>HAS ANYTHING BEEN FIRED SINCE SOMEBODY LAST LOOKED?</b> The newest shot on this ground when
    /// the log has grown past <paramref name="alreadyHeard"/>, and null when it has not.
    ///
    /// <para><b>The NEWEST and not the next one along</b>, and that is the ruling rather than a saving. Three
    /// rounds into one hasp inside a second are one noise from where a man on a rota is standing, and a
    /// reader that walked the backlog would send him to the first door, then the second, then the third — a
    /// queue of errands nobody fired. He goes to the last place it came from, which is where it is still
    /// coming from.</para>
    ///
    /// <para>The caller keeps the count it has answered and moves it to <see cref="Count"/> — the log is
    /// append-only (that is <see cref="File"/>'s own law), so a count is a cursor and cannot go stale in the
    /// direction that would answer one bang twice.</para>
    /// </summary>
    public static Shot? SinceLastHeard(IReadOnlyList<Shot>? log, int alreadyHeard) =>
        Count(log) > alreadyHeard && log is { Count: > 0 } ? log[^1] : null;

    /// <summary>File one. Pure — the caller owns the list, the same way the field notes and the hot-cargo
    /// ledger are owned — and the newest shot goes on the END, so the log reads in the order it happened.
    ///
    /// <para>A list built by appending is a list in order, and it is read in that order everywhere. (The
    /// fourth named bug class on this repo is a source consumed in the wrong one.)</para></summary>
    public static IReadOnlyList<Shot> File(IReadOnlyList<Shot>? log, Shot shot)
    {
        ArgumentNullException.ThrowIfNull(shot.Unit);
        var next = new List<Shot>(log ?? []) { shot };
        return next;
    }

    /// <summary>How many shots this ground has heard. The cheapest thing a later rule can ask.</summary>
    public static int Count(IReadOnlyList<Shot>? log) => log?.Count ?? 0;

    /// <summary>Rounds fired on this ground, all told.</summary>
    public static int RoundsFired(IReadOnlyList<Shot>? log)
    {
        int total = 0;
        foreach (Shot s in log ?? [])
        {
            total += s.Rounds;
        }
        return total;
    }

    /// <summary>The line the field book keeps. Not the event's own telling — that is the shot's line, said
    /// where the captain is looking — this is the RECORD, in the register the book is written in: flat, and
    /// about a place.</summary>
    public static string BookLine(Shot shot)
    {
        ArgumentNullException.ThrowIfNull(shot.Unit);
        return
            $"🔫 {shot.Unit} fired {shot.Rounds} round{(shot.Rounds == 1 ? "" : "s")} at {shot.At}. " +
            $"Anything with ears inside {EarshotDu:F0} du of that spot has a place to walk to now.";
    }

    /// <summary>The warning worth having, said once per ground the first time a captain does this: what they
    /// have actually spent is not the rounds.</summary>
    public const string WhatItCostLine =
        "You have just told the whole floor where you are. Nothing has come yet. That is not the same as " +
        "nothing having heard it.";
}
