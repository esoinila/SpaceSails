namespace SpaceSails.Core;

/// <summary>
/// #954 — 🎯 NEAREST DOES NOT FLICKER, AND IT SPEAKS THE HIERARCHY.
///
/// <para>The bug, in the owner's words: "Nearest flickers between Mars and The Rusty Roadstead every
/// orbit." Both readouts were true. The Roadstead is a station on a 12,000 km, two-hour rail around Mars;
/// from a quarter of an AU away the two are the same distance to four decimal places, and which of them is
/// the literal closest swaps twice per station orbit — several times a second at warp. A readout that
/// re-decides an unchanged truth is noise, and the same noise drove the scope's AUTO lock, which picks the
/// nearest body: the picture in the video box ping-ponged with it.</para>
///
/// <para><b>The law is two rules, and they are the same rule twice.</b> (1) <see cref="Unseats"/> —
/// a challenger takes the "nearest" slot only when it is closer by a real margin, not by a hair; the
/// incumbent keeps it otherwise. That is plain hysteresis, and it is what stops the swap. (2) Its
/// complement, <see cref="InTheSameBreath"/> — two things neither of which can unseat the other are, for a
/// captain's purposes, in the same place, and the honest readout names them TOGETHER rather than picking a
/// winner: "Mars › The Rusty Roadstead". That is the owner's ask — "present the hierarchy, Mars is closest
/// and it contains (in its Hill sphere) The Rusty Roadstead" — and it is why the flicker was never simply a
/// display bug: the readout had one slot for a fact that needs two.</para>
///
/// <para>Pure and deterministic: distances in, a verdict out. No clock, no state — the caller holds the
/// incumbent, which is the only memory the law needs.</para>
/// </summary>
public static class NearestRule
{
    /// <summary>
    /// How much closer a challenger must be, as a fraction of the incumbent's distance, before it takes the
    /// "nearest" slot. Sized off the case that broke: a station 12,000 km out from Mars, seen from 0.16 AU
    /// (2.4e10 m), swings the two distances apart by 0.05% — three orders of magnitude inside this band, so
    /// the pair never trades places at cruise range. A genuine change of neighbourhood — Mars giving way to
    /// Jupiter as the ship crosses — clears 3% long before anyone would call the readout wrong.
    /// </summary>
    public const double SwapFraction = 0.03;

    /// <summary>
    /// Does <paramref name="challengerDistance"/> unseat the current nearest at
    /// <paramref name="incumbentDistance"/>? Only when it is closer by more than <see cref="SwapFraction"/>
    /// of the incumbent's distance. A challenger that is merely, fractionally closer does NOT — that is the
    /// whole of the anti-flicker law. (With no incumbent, the caller takes the challenger outright; this
    /// answers only the contest between two live candidates.)
    /// </summary>
    public static bool Unseats(double incumbentDistance, double challengerDistance) =>
        challengerDistance < incumbentDistance * (1.0 - SwapFraction);

    /// <summary>Squared-distance form, so the per-frame sweep never takes a square root it doesn't need.
    /// Exactly <see cref="Unseats"/> with both sides squared.</summary>
    public static bool UnseatsSquared(double incumbentDistanceSquared, double challengerDistanceSquared) =>
        challengerDistanceSquared < incumbentDistanceSquared * ((1.0 - SwapFraction) * (1.0 - SwapFraction));

    /// <summary>
    /// Are these two at the same distance <i>as far as the captain is concerned</i> — neither able to unseat
    /// the other? This is the hierarchy trigger: a planet and a station in its Hill sphere that sit inside
    /// each other's band are one place with two names, and the readout says so instead of choosing.
    /// </summary>
    public static bool InTheSameBreath(double aDistance, double bDistance) =>
        !Unseats(aDistance, bDistance) && !Unseats(bDistance, aDistance);

    /// <summary>Squared-distance form of <see cref="InTheSameBreath"/>.</summary>
    public static bool InTheSameBreathSquared(double aDistanceSquared, double bDistanceSquared) =>
        !UnseatsSquared(aDistanceSquared, bDistanceSquared) && !UnseatsSquared(bDistanceSquared, aDistanceSquared);

    /// <summary>
    /// 🛰 THE SECOND HALF OF THE SAME LAW — <b>a satellite does not contest "nearest" from inside its
    /// primary's skirts.</b>
    ///
    /// <para>The band above is measured along the sightline, so it shrinks as the ship closes: at a quarter
    /// of an AU 3% of the range is 700,000 km and Mars's whole family fits inside it, but at 100,000 km it is
    /// 3,000 km and Phobos (9,376 km out) and the Roadstead (12,000 km out) start trading places again —
    /// every orbit, exactly as before, just nearer. It is worse at Earth, where a station in low orbit and
    /// the planet swap several times a minute. The hysteresis was never wrong; it was measured against the
    /// wrong thing.</para>
    ///
    /// <para>So a satellite defers to its primary, and the primary stands for the whole neighbourhood, until
    /// the ship is <b>inside the satellite's Hill sphere</b> — the line at which it is captured, and the
    /// very same line the market and the lying-low rule already use for "you are at this body". Then it
    /// speaks for itself and the band above decides the rest.</para>
    ///
    /// <para><b>Why the Hill sphere and not something roomier.</b> A parked ship sees a satellite's
    /// distance swing between |D−a| and D+a as its rail turns (D = range to the primary, a = the rail). Any
    /// threshold T the satellite can cross is crossed TWICE AN ORBIT for every D in a ± T — a shell of
    /// hover ranges where the flicker comes straight back. The Hill radius is kilometres where the rails
    /// are hundreds of thousands of them, so that shell shrinks to the moon's own capture width and there
    /// is nowhere left to park and watch the readout blink. A roomier threshold — "nearer to it than it is
    /// to its primary", say — sets T = a and reopens the shell over the entire approach.</para>
    ///
    /// <para>A mass-less berth has no Hill sphere, so it never takes the slot by drifting past; it takes it
    /// by being clamped to, which the caller writes in as its own clause. And a real moon still takes the
    /// slot the moment the ship is captured by it — this is a rule about what "nearest" MEANS, not a lock.
    /// </para>
    /// </summary>
    /// <param name="shipToBody">How far the ship is from the satellite.</param>
    /// <param name="hillRadius">The satellite's Hill radius about the body it orbits.</param>
    public static bool StandsForItself(double shipToBody, double hillRadius) =>
        shipToBody < hillRadius;

    /// <summary>Squared-distance form of <see cref="StandsForItself"/>, for the per-frame sweep.</summary>
    public static bool StandsForItselfSquared(double shipToBodySquared, double hillRadiusSquared) =>
        shipToBodySquared < hillRadiusSquared;

    /// <summary>
    /// The one line for a nearest reading that has a parent and a child: "Mars › The Rusty Roadstead". The
    /// chevron is doing real work — it says the second thing is <i>inside</i> the first's Hill sphere, which
    /// is exactly the fact the flickering single-slot readout could not hold.
    /// </summary>
    public static string Hierarchy(string parentName, string childName) => $"{parentName} › {childName}";

    /// <summary>The scope's honest sub-line for a hierarchical lock: the box is pointed at the child, and
    /// this says whose sphere the child is in — without renaming the thing actually drawn.</summary>
    public static string OrbitsNote(string parentName) => $"orbits {parentName}";
}
