namespace SpaceSails.Core;

/// <summary>
/// #336 · <b>THE LINK IS RANGE AND CLOSING SPEED. IT WAS NEVER DOCKAGE.</b>
///
/// <para>Owner ruling (2026-07-18), verbatim: <i>"the shuttle ship link should not break even if the ship
/// undocks, because the docking is not requisite for the ship to stay in vicinity. As long as the ship is in
/// shuttle range (shown on map) and is not moving too fast away, we should be able to fly back to it from a
/// landing site."</i></para>
///
/// <para><b>What this file is, and what it deliberately is not.</b> It is the READING — which rung of the
/// #331 ladder the captain is on, and what the boat says when she will not go. It is <i>not</i> a second
/// radius: the reach and the catch speed are <see cref="ShuttleRange"/>'s, the classification is
/// <see cref="ExpeditionWindow.ClassifyClock"/>'s (the same law the shuttle board and the away clock have
/// always run, measured by walking the real geometry rather than extrapolating a range-rate — see #955
/// NAV-2 and its 986-day reading). A ladder that asked its own distance question would be this repository's
/// oldest bug class: two sources that agree today.</para>
///
/// <h3>THE THREE RUNGS, AND THE ONE STATUS THAT IS NOT A RUNG</h3>
///
/// <para>#331's ladder used to speak the ORBIT HOLD: steady → slipping → failing → lost, off the tank and
/// the trim bill. That stays exactly where it is and keeps saying what it always said, because it answers a
/// real question — <b>WHY is she drifting</b>. What it cannot answer is the one the captain standing on the
/// regolith actually has, which is <b>WHETHER it matters</b>. A ship whose keeper gave up an hour ago but
/// which is still sitting a tenth of a hop away is a captain with a ride home; a ship holding a perfect
/// orbit on the far side of the primary is a maroon. So the rungs re-anchor to reachability:</para>
/// <list type="bullet">
///   <item><b>Calm</b> — the gap is inside a hop and is not opening. <see cref="Calm"/>.</item>
///   <item><b>Amber</b> — the gap is inside a hop and there is a clock on it. <see cref="Amber"/>.</item>
///   <item><b>Lost</b> — the gap is past the boat's legs and nothing on the charts brings it back.
///   <see cref="Maroon"/>.</item>
/// </list>
///
/// <para><b><see cref="WindowStatus.Closed"/> is not one of the three, and that is the whole care taken
/// here.</b> #955 NAV-2 built it for the owner's own corner case — clamped at a Jupiter or Saturn haven the
/// moon windows are PERIODIC, so the gap opens past a hop and closes again every synodic period with nobody
/// in any danger at all. Calling that a maroon would be the third named bug class wearing the new ladder's
/// clothes: a sentence saying "that is a maroon, captain" over a sim that will hand the boat back in twenty
/// minutes. A closed window answers <see langword="null"/> here and keeps the reading #955 gave it —
/// <i>closed · next window in X</i> — untouched.</para>
/// </summary>
public static class ShuttleLink
{
    // ── The three lines. Authored once, spelled once, never composed with. ───────────────────────────────

    /// <summary>The calm rung: she is adrift, and it does not matter.</summary>
    public const string Calm = "The ship drifts — still in shuttle range.";

    /// <summary>The amber rung: it is starting to matter.</summary>
    public const string Amber = "The ship is nearing the edge of shuttle range.";

    /// <summary>The lost rung — the true maroon, still survivable and still announced (the maroon canon:
    /// the captain is revived, the thread goes on; what is gone is the ride).</summary>
    public const string Maroon = "She is beyond the shuttle's legs. That is a maroon, captain.";

    /// <summary>The rungs of the reachability ladder, in escalation order.</summary>
    public enum Stage
    {
        /// <summary>In range, and the gap is not opening.</summary>
        Calm = 0,

        /// <summary>In range, and there is a clock on it.</summary>
        Amber = 1,

        /// <summary>Past the boat's legs, with nothing bringing it back.</summary>
        Lost = 2,
    }

    /// <summary>
    /// #336 · Where the amber rung begins, as a fraction of the ONE reach
    /// (<see cref="ShuttleRange.RangeMeters"/>) and never as a radius of its own. The same idiom
    /// <see cref="OrbitHold.SlippingFraction"/> uses on the hold clock: a warning is a fraction of the real
    /// thing, so there is one number to tune and one number that can be wrong. OWNER-TUNABLE.
    ///
    /// <para><b>Why a band and not a clock.</b> The obvious alternative was to read amber off
    /// <see cref="WindowStatus.Critical"/> — the away window's existing last-call margin — and it was tried
    /// and measured and it is wrong here. That margin is sixty seconds, and at any recession a captain would
    /// notice (a tenth of the boat's cruise, 800 m/s) sixty seconds is 48 km out of a 500,000 km reach: the
    /// warning would arrive one ten-thousandth of the way from "plenty" to "gone" and would be gone again
    /// before it was read. The owner's own words are spatial — "nearing the edge of shuttle range" — and a
    /// band is what makes them true.</para>
    /// </summary>
    public const double AmberFraction = 0.75;

    /// <summary>
    /// Which rung the link reads as, off the honest gap and the window's own verdict — or
    /// <see langword="null"/> for <see cref="WindowStatus.Closed"/>, which is not a rung (see the type's own
    /// note: a periodic window that swings back is a wait, not a maroon).
    ///
    /// <para>The distance decides the three rungs and the status decides only whether this is a maroon or a
    /// wait, which is exactly the division of labour the two laws were built for: <see cref="ShuttleRange"/>
    /// owns the reach, and #955 NAV-2's bounded forward scan owns the question of whether the geometry brings
    /// her back.</para>
    /// </summary>
    public static Stage? StageFor(double distanceMeters, WindowStatus status)
    {
        if (status == WindowStatus.Closed)
        {
            return null; // #955 NAV-2's own reading stands
        }

        if (!ShuttleRange.InRange(distanceMeters))
        {
            return Stage.Lost;
        }

        return distanceMeters >= AmberFraction * ShuttleRange.RangeMeters ? Stage.Amber : Stage.Calm;
    }

    /// <summary>The line for a rung. The three constants and nothing else: no interpolation, no tail, no
    /// number spliced in — a canon line that is composed with is a canon line that has been edited.</summary>
    public static string Line(Stage stage) => stage switch
    {
        Stage.Calm => Calm,
        Stage.Amber => Amber,
        _ => Maroon,
    };

    /// <summary>How loudly the surface HUD paints a rung — the same 0/1/2 scale
    /// <see cref="OrbitHold.Severity"/> uses, so the comms strip needs no second colour table.</summary>
    public static int Severity(Stage stage) => stage switch
    {
        Stage.Calm => 0,
        Stage.Amber => 1,
        _ => 2,
    };

    // ── The boat's own answer when a captain asks to go home ─────────────────────────────────────────────

    /// <summary>Why the boat will not fly the captain back up.</summary>
    public enum Refusal
    {
        /// <summary>She will fly.</summary>
        None,

        /// <summary>The gap is past her legs. Whether anybody is docked has never entered into it.</summary>
        BeyondRange,

        /// <summary>The gap is inside her legs and the hulls are parting faster than she can fly.</summary>
        TooFastToCatch,
    }

    /// <summary>
    /// Ask the boat. <paramref name="distanceMeters"/> is the honest gap between the ground the captain is
    /// standing on and where the mothership actually is; <paramref name="relativeSpeedMps"/> is the speed of
    /// one hull relative to the other.
    ///
    /// <para><b>Dock state is not an argument to this method and never will be.</b> That is the whole of the
    /// ruling: a ship that has slipped her clamp, slipped her orbit or simply been left drifting is a ship
    /// the boat can still reach, and the only question worth asking is whether she can be caught.</para>
    /// </summary>
    public static Refusal AskTheBoat(double distanceMeters, double relativeSpeedMps)
    {
        if (!ShuttleRange.InRange(distanceMeters))
        {
            return Refusal.BeyondRange;
        }

        return ShuttleRange.CanCatch(distanceMeters, relativeSpeedMps)
            ? Refusal.None
            : Refusal.TooFastToCatch;
    }

    /// <summary>What the boat says when she will not go. In her own voice and never one of the three ladder
    /// lines — the ladder is the ship reporting, this is the boat refusing, and a captain who reads the
    /// maroon line as the answer to pressing a button would think the run was over when it is not.</summary>
    public static string RefusalLine(Refusal refusal) => refusal switch
    {
        Refusal.BeyondRange =>
            "🛸 The boat will not fly it. The ship is past her legs — she would burn everything she has and "
            + "come up short in the dark.",
        Refusal.TooFastToCatch =>
            "🛸 The boat will not fly it. The ship is close enough, but she is parting company faster than "
            + "the boat can fly — there would be nothing to come alongside.",
        _ => "",
    };
}
