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
/// <para><b>This build does not react.</b> Deliberately. The pack's own ear is rung by the existing call
/// and nothing else in the game reads this ledger yet — it is a seam, filed from the first shot so that
/// whoever prices it is reading a record rather than re-deriving one from a HUD line that has scrolled
/// away. A seam that is written only when somebody needs it is a seam that is written wrong.</para>
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
