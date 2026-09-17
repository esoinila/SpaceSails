using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #708 · WHAT A FLOOR STATES ABOUT ITSELF — whether it holds pressure, whether it is plumbed, and
/// whether it is dark.
///
/// <para>Owner's ruling 2026-08-05, filed with the headlights: darkness is NOT a filter somebody
/// switches on over the top of the game. It is a fact a floor states about itself, in exactly the way
/// <see cref="HoldsPressure"/> states whether the same floor can be breathed — and for exactly the
/// same reason. The moment two things in this building can each hold an opinion about whether the
/// lights are on, the plate by the lift and the picture on the screen are reading two different maps,
/// and this ground has already paid for that mistake once (§13.13, the pressure fact).</para>
///
/// <para>Split out of <c>UndergroundComplex.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>Which floors still hold atmosphere. THE one pressure fact in this building (§13.13), and
    /// everything that shows it — the drain, the gauge, the plate by the car — asks this and nothing else.
    ///
    /// <para>Owner's biggest open question, answered with a beat in it: a floor with power lulls you and the
    /// rest costs you. Extended for unbounded depth by making it the TOP OF EVERY SHAFT BAND — that is where
    /// a facility puts its lobbies — so a captain who finds the next shaft gets one floor of relief before the
    /// dark again. It keeps a very deep site playable without ever making it safe.</para>
    ///
    /// <para><b>#677 · And it takes the BODY now, which is the whole cost of the halls.</b> Every floor of
    /// the band nobody dug holds pressure — all of it, all the way down, and nothing anywhere shows the plant
    /// that does it. Whether a floor breathes therefore stopped being arithmetic on a level and became a fact
    /// about the site, and there is deliberately NO level-only overload left: one would be a second answer to
    /// the one question §13.13 exists to keep single, silently right on every floor of every building except
    /// the four this feature is about. The compiler made every caller say which moon it is standing under,
    /// which is the strongest guard available.</para>
    ///
    /// <para><b>#802 · AND THE SURFACE IS A FLOOR IT ANSWERS FOR.</b> Owner: <i>"the surface should be
    /// vacuum / unbreathable ... being unbreathable makes the breathing so much more scary."</i> Level 0 and
    /// above is the regolith, and on every body this game lands on the regolith is airless — so the answer
    /// here is <c>false</c>, deliberately and not as a side effect of the <c>level &lt; 0</c> clause. It is
    /// stated because a caller had to ask: the lift panel typed <c>Pressurised: true</c> into its SURFACE row
    /// rather than asking, and for as long as it did, the one button every captain presses on the way out
    /// promised air on the one ground the whole tank mechanic is built on.</para></summary>
    public static bool HoldsPressure(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (level >= 0)
        {
            return false;   // #802 · the regolith. Vacuum on every body, with no exception to write down.
        }
        return (-level - 1) % FloorsPerShaft == 0 || IsFound(bodyId, level);
    }

    /// <summary>#677 · IS ANYTHING PLUMBED ON THIS FLOOR — the question every amenity, cubicle and en-suite
    /// asks, and the one place the answer differs from <see cref="HoldsPressure"/>.
    ///
    /// <para>§13.17's law is that plumbing is for people out of their suits, and it is asked against the one
    /// pressure fact so that no room down here can ever breathe for a reason the plate by the lift does not
    /// know about. The halls breathe and are not plumbed, and those are not in tension: a canteen, a cubicle
    /// and a duct are all things somebody was made to PAY for, and there is no invoice down there. The air in
    /// a found gallery is not provided by anything the cone can find — that is the entire sensation (§13.20)
    /// — so a grille in one would be the building explaining the one thing it must never explain.</para></summary>
    public static bool IsPlumbed(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HoldsPressure(bodyId, level) && !IsFound(bodyId, level);
    }

    // ── #708 · DARKNESS IS A PROPERTY OF A FLOOR ─────────────────────────────────────────────────────────
    //
    // Owner's ruling 2026-08-05, filed with the headlights: darkness is NOT a filter somebody switches on
    // over the top of the game. It is a fact a floor states about itself, in exactly the way HoldsPressure
    // states whether the same floor can be breathed — and for exactly the same reason. The moment two things
    // in this building can each hold an opinion about whether the lights are on, the plate by the lift and
    // the picture on the screen are reading two different maps, and this ground has already paid for that
    // mistake once (§13.13, the pressure fact).
    //
    // So there is ONE ask, and everything that cares calls it: the renderer, the boot cheat, and whatever sim
    // eventually wants to know (nothing does today — a sentry's rules are its own, §13.18). The cheat is an
    // ARGUMENT to the ask, never a second answer OR-ed in beside it at a call site, because an `||` at a call
    // site is precisely how a second source of truth gets built one honest line at a time.

    /// <summary>
    /// #708 · Whether this floor is DARK: no fixtures, no failing facility light, nothing at all — the suit's
    /// headlights (<see cref="SuitLamp"/>) are the whole of the seeing there is.
    ///
    /// <para>The one ask. Nowhere else in this game gets to decide this.</para>
    ///
    /// <para><b>Above ground is never dark.</b> A surface has a sun, a sky and the #563 falloff into the
    /// unseen bound; darkness is a property of somewhere with a roof on it, and a cheat that blacked out the
    /// regolith would be testing a different feature.</para>
    /// </summary>
    /// <param name="lampsOut">The <c>?dark=1</c> boot cheat: kill the fixtures on every floor of this
    /// excursion. No shipped floor declares darkness yet (see <see cref="DeclaresDarkness"/>), so this is the
    /// only way to reach the feature today — and a scene nobody can reach on demand is a scene that ships
    /// broken.</param>
    public static bool IsDark(string bodyId, int level, bool lampsOut = false) =>
        level < 0 && (lampsOut || DeclaresDarkness(bodyId, level));

    /// <summary>
    /// #708/#677 · Whether a floor declares itself dark of its own accord.
    ///
    /// <para><b>No shipped floor does, and that is deliberate.</b> Every listed floor down here has failing
    /// facility light and the instrument-lit look it has always had; changing that would change every Hive
    /// anybody has ever played, to solve a problem those floors do not have. The customer is the FOUND BAND
    /// (#677) — galleries that pre-exist the shaft, with no fixtures, no wiring and no ventilation anybody
    /// can find — and it will answer here, in one line, when it is built.</para>
    ///
    /// <para>Dead-air floors are NOT dark and do not flicker. A flicker is a fixture reporting that it is
    /// dying; a floor that cannot be breathed is not a floor whose lamps have failed, and wiring the two
    /// together would have made the suit gauge and the ceiling say the same thing twice.</para>
    ///
    /// <para><b>#677 · The customer arrived.</b> The band nobody dug is the one thing in the game that
    /// declares itself dark, and it is one line, exactly as #708 promised. Owner's ruling: <i>"the
    /// pre-existing tunnels would be scary as dark ones and totally different style"</i> — no fixtures, no
    /// wiring, nothing that ever held a lamp. The facility's failing light stops at the poured concrete;
    /// past the seam the dark is ORIGINAL, and the cone is the whole of the seeing.</para>
    /// </summary>
    public static bool DeclaresDarkness(string bodyId, int level) => IsFound(bodyId, level);
}
