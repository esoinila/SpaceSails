using System;
using System.Collections.Generic;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

// Subject: #759 · the underground park's own grow-cycle — the light phase and level at a sim-time, and the
// one beat a captain who lingers is told. Part of the park family (UndergroundComplex.Park.cs carves it,
// ParkBenches.cs seats it, this file gives it a day).

/// <summary>
/// #759 · <b>THE LIGHT KEEPS ITS OWN DAY.</b> The last named remainder of the park behind the bar, and the
/// smallest of them: <i>"a grow-cycle that matches no watch of the building above or below. Anyone who
/// lingers notices the park's morning arriving at the wrong time. Subtly wrong is the register: never
/// broken, never right."</i>
///
/// <para><b>What this is.</b> A pure function of sim-time and a site id to a light phase and a level in
/// 0..1 — nothing else. No clock, no seed handed in, no state. The renderer asks it what the gravel and
/// the floodlight masts look like right now; the page asks it whether a captain standing on that gravel
/// has just watched the morning come up. Those are the only two readers there will ever be, because the
/// third — a label, a plate, a HUD row that SAYS what time it is in here — is the thing the beat is made
/// of not existing.</para>
///
/// <para><b>Why the period is the number it is.</b> Stated once, here, and argued rather than typed.
/// Two constraints, and one of them is arithmetic:</para>
/// <list type="number">
///   <item><b>It has to be a photoperiod a crop house would actually run.</b> Controlled-environment
///   growers are not obliged to keep a planet's day and mostly do not: a leaf crop under lamps is run on
///   whatever cycle buys the most dry weight per kilowatt, and sub-24-hour cycles (18 to 22 hours, light
///   for four fifths of it) are ordinary practice. This one is <b>4.618034 watches — 18 h 28 m of sim
///   time</b>, lit for 14 h 46 m of that. That is a working schedule, not a mood.</item>
///   <item><b>It must never come back onto the building's shift clock.</b> The watch every other clock in
///   this building is kept by is <see cref="PatronRota.WatchSeconds"/> — four sim-hours, the beat the whole
///   bar cast shuffles on. If the grow cycle were a whole number of watches, or a half or a third or a
///   quarter of one, the park's morning would arrive at the same point of somebody's shift every time and
///   the room would simply have a different day — a legible one. So the ratio is <see cref="Watches"/> =
///   four watches and the GOLDEN SECTION of a fifth, which is the worst-approximable number there is: no
///   fraction p/q with a small q comes anywhere near it, and the park's morning therefore walks around the
///   building's clock for ever without ever settling on it. <c>TheParkKeepsItsOwnDayTests</c> states the
///   bound and holds it.</item>
/// </list>
///
/// <para><b>And a per-site offset</b>, seeded off the site's own id, so the three buildings with a park in
/// them are not keeping the same secret day as each other either.</para>
///
/// <para><b>Nothing here says why.</b> The park's second purpose is reserved arc material (#759: <i>"no
/// card, no sensor, no bark may ever confirm whatever it turns out to be"</i>). The two authored lines
/// below report what a captain saw and stop.</para>
/// </summary>
public static class ParkDay
{
    /// <summary>The four parts of a grow cycle, in the order they run. <see cref="Night"/> is lamps off and
    /// the cycle's own beginning, so <see cref="Dawn"/> — the morning the beat is about — is a boundary a
    /// standing captain can be on the wrong side of.</summary>
    public enum Phase
    {
        /// <summary>Lamps off.</summary>
        Night = 0,
        /// <summary>Coming up — the ramp the beat is written on.</summary>
        Dawn = 1,
        /// <summary>Full.</summary>
        Day = 2,
        /// <summary>Going down.</summary>
        Dusk = 3,
    }

    /// <summary>The golden section, φ − 1 = 0.618033988749895 — the number no fraction approximates well.
    /// It is here for one reason and it is stated in the type's own docs: it is what keeps the park's day
    /// off the building's watch for ever rather than for a while.</summary>
    public const double GoldenSection = 0.618_033_988_749_895;

    /// <summary>The grow cycle, in <see cref="PatronRota.WatchSeconds"/> — <b>4.618034 watches</b>, four
    /// watches and the golden section of a fifth. See the type docs: one number, two constraints, and this
    /// is the value that meets both.</summary>
    public const double Watches = 4.0 + GoldenSection;

    /// <summary>The grow cycle in sim-seconds — 66,499.7 s, eighteen hours and twenty-eight minutes.</summary>
    public static double PeriodSeconds => PatronRota.WatchSeconds * Watches;

    /// <summary>Lamps off for a fifth of the cycle: 3 h 41 m of dark.</summary>
    public const double NightFraction = 0.20;

    /// <summary>The ramp up — 55 minutes, which is long enough to walk into the middle of.</summary>
    public const double DawnFraction = 0.05;

    /// <summary>Full light for seven tenths of the cycle: 12 h 55 m.</summary>
    public const double DayFraction = 0.70;

    /// <summary>And the ramp down, the same length as the ramp up.</summary>
    public const double DuskFraction = 1.0 - NightFraction - DawnFraction - DayFraction;

    // ── THE CYCLE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where <paramref name="siteId"/>'s park is in its own cycle at <paramref name="simTime"/>,
    /// as a fraction in [0,1) — 0 is the top of the dark. Pure, and the one place the per-site offset is
    /// applied, so no caller can take the offset twice or forget it.</summary>
    public static double CycleFraction(double simTime, string siteId) =>
        Frac((simTime / PeriodSeconds) + OffsetFraction(siteId));

    /// <summary>The park's phase at <paramref name="simTime"/>.</summary>
    public static Phase PhaseAt(double simTime, string siteId) => PhaseOf(CycleFraction(simTime, siteId));

    /// <summary>How lit the park is at <paramref name="simTime"/>, in 0..1 — flat 0 through the dark, a
    /// straight ramp across each shoulder, flat 1 through the day. Continuous, so nothing the renderer
    /// draws off it can snap.</summary>
    public static double LevelAt(double simTime, string siteId) => LevelOf(CycleFraction(simTime, siteId));

    /// <summary>
    /// The same four-part arc read off <b>the building's own shift clock</b> — the yardstick, and not a
    /// thing anybody in the game is ever told. A watch is what the rest of this facility keeps its day by
    /// (<see cref="PatronRota.WatchSeconds"/>): it turns over in the dark, comes up, runs, and goes down to
    /// last call. Measured with the SAME fractions as the park's own cycle on purpose — comparing two
    /// differently-shaped days would make a disagreement mean nothing, and the whole beat is the
    /// disagreement.
    /// </summary>
    public static Phase BuildingPhaseAt(double simTime) =>
        PhaseOf(Frac(simTime / PatronRota.WatchSeconds));

    /// <summary>A site's own place in the cycle, in [0,1) — seeded off its id, so two parks are not keeping
    /// the same day as each other.</summary>
    public static double OffsetFraction(string siteId) => Unit(Fold(siteId ?? string.Empty, 0x759));

    // ── THE LINGERING NOTICE ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #759 · <b>THE WHOLE OF THE BEAT, AS ONE PREDICATE.</b> Whether a captain who is still standing in
    /// the park has just watched its morning come up at a time the building was not having one.
    ///
    /// <para>Read the authored line and the rule is already in it — <i>"It is coming up to morning in here.
    /// It was not morning anywhere else in the building when you came in."</i> Three claims, and each is a
    /// term: it is morning in the park NOW; it was not when you walked in, or you did not see it arrive;
    /// and the building was not having its own morning at that moment either, or there would be nothing
    /// wrong with this one.</para>
    ///
    /// <para>Pure, and exhaustively testable over its 128 inputs, which is why the page's side of this is
    /// three lines that cannot be got wrong.</para>
    /// </summary>
    public static bool WouldNotice(
        Phase cameInOn, Phase theBuildingWhenYouCameIn, Phase inTheParkNow, bool alreadySpent) =>
        !alreadySpent
        && inTheParkNow == Phase.Dawn
        && cameInOn != Phase.Dawn
        && theBuildingWhenYouCameIn != Phase.Dawn;

    /// <summary>One key per captain per site, in the register that rides the vault. Prefixed so it cannot
    /// be mistaken for a room key or any other tag in that set.</summary>
    public static string NoticeTag(string siteId) =>
        FormattableString.Invariant($"park-day:{siteId}");

    /// <summary>The glyph the park files under — its own, the one the attendance note already uses.</summary>
    public const string Glyph = UndergroundComplex.ParkGlyph;

    /// <summary><b>AUTHORED (Fable), VERBATIM.</b> The one pulse, said once per captain per site. It
    /// reports and does not explain: no lamp, no schedule, no company, no reason.</summary>
    public const string LingerLine =
        "It is coming up to morning in here. It was not morning anywhere else in the building when you "
        + "came in.";

    /// <summary><b>AUTHORED (Fable), VERBATIM.</b> The field-book entry — lower case and no full stop,
    /// because it is an entry and not an announcement.</summary>
    public const string NoteLine = "the park keeps a day of its own — set to nobody's watch";

    /// <summary>#741's subject law: the author declares what the note is about, and the note's words are
    /// never read to find out. It is about the PLACE — the building you are standing under, which is the
    /// only thing a captain could file this under without being told something nobody has told them.</summary>
    public static string Subjects(string place) =>
        CaseSubjects.Line(CaseSubjects.Place(place ?? string.Empty));

    /// <summary>Every player-facing string this type publishes, so the canon sweeps walk them all and a
    /// sentence added next month cannot slip past by not being enumerated.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return LingerLine;
        yield return NoteLine;
    }

    // ── THE DEV DOOR ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>How long before the park's morning <c>?parkphase=morning</c> sets a tester down — five
    /// sim-minutes, which is a wait you can sit through at warp 1 and still short enough that nobody has to
    /// find the slider.</summary>
    public const double MorningLeadSeconds = 300.0;

    /// <summary>
    /// The earliest sim-time at which <paramref name="siteId"/>'s park is in the MIDDLE of
    /// <paramref name="phase"/> — the whole arithmetic behind <c>?parkphase=</c>, stated here rather than in
    /// the cheat, so a dev door cannot become a second answer to "when is it morning in here".
    /// </summary>
    public static double FirstMiddleOf(Phase phase, string siteId)
    {
        double mid = phase switch
        {
            Phase.Night => NightFraction / 2.0,
            Phase.Dawn => NightFraction + (DawnFraction / 2.0),
            Phase.Day => NightFraction + DawnFraction + (DayFraction / 2.0),
            _ => NightFraction + DawnFraction + DayFraction + (DuskFraction / 2.0),
        };

        double at = PeriodSeconds * Frac(mid - OffsetFraction(siteId));
        return at > 0.0 ? at : at + PeriodSeconds;
    }

    /// <summary>
    /// The earliest sim-time at which a captain standing in <paramref name="siteId"/>'s park is
    /// <see cref="MorningLeadSeconds"/> away from watching its morning come up AND can be told about it —
    /// the one door a tester actually wants, because the beat is the CHANGE and you cannot walk in on a
    /// change. Walks forward a cycle at a time until it finds a morning the building is not also having,
    /// which is the second half of <see cref="WouldNotice"/> and therefore not something the cheat is
    /// allowed to assume.
    /// </summary>
    public static double FirstJustBeforeMorning(string siteId)
    {
        // A second PAST the boundary, not on it. A phase edge is where a double is least trustworthy — the
        // arithmetic that lands exactly on 0.20 of a cycle lands on either side of it depending on the last
        // bit — and a frame arrives where a frame arrives anyway. One second of sim time costs the tester
        // nothing and makes the door's promise a thing that can be asserted.
        double dawnAt = (PeriodSeconds * Frac(NightFraction - OffsetFraction(siteId))) + 1.0;
        for (int cycle = 0; cycle < 64; cycle++)
        {
            double at = dawnAt + (cycle * PeriodSeconds) - MorningLeadSeconds;
            if (at > 0.0
                && PhaseAt(at, siteId) != Phase.Dawn
                && BuildingPhaseAt(at) != Phase.Dawn)
            {
                return at;
            }
        }

        return dawnAt + PeriodSeconds;
    }

    // ── THE ARITHMETIC ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Which part of a cycle a fraction in [0,1) falls in. One table, read by the park and by the
    /// building alike.</summary>
    public static Phase PhaseOf(double fraction)
    {
        double f = Frac(fraction);
        return f < NightFraction ? Phase.Night
            : f < NightFraction + DawnFraction ? Phase.Dawn
            : f < NightFraction + DawnFraction + DayFraction ? Phase.Day
            : Phase.Dusk;
    }

    /// <summary>How lit a cycle is at a fraction in [0,1): 0 through the dark, a straight ramp up across
    /// the dawn, 1 through the day, a straight ramp down across the dusk.</summary>
    public static double LevelOf(double fraction)
    {
        double f = Frac(fraction);
        if (f < NightFraction)
        {
            return 0.0;
        }
        if (f < NightFraction + DawnFraction)
        {
            return (f - NightFraction) / DawnFraction;
        }
        if (f < NightFraction + DawnFraction + DayFraction)
        {
            return 1.0;
        }
        return 1.0 - ((f - (NightFraction + DawnFraction + DayFraction)) / DuskFraction);
    }

    /// <summary>The fractional part, correct for negative inputs too (defensive; the clock starts at 0).</summary>
    private static double Frac(double v)
    {
        double f = v - Math.Floor(v);
        return f is >= 0.0 and < 1.0 ? f : 0.0;
    }

    // splitmix64 over a salted fold of the id — PatronRota's own idiom, and for its reason: determinism is
    // law in Core, so no System.Random and no string hash the runtime is allowed to change between versions.
    private static ulong Fold(string s, ulong salt)
    {
        ulong h = salt;
        foreach (char c in s)
        {
            h = SplitMix64(h + c);
        }
        return h;
    }

    private static ulong SplitMix64(ulong z)
    {
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private static double Unit(ulong h) => (h >> 11) * (1.0 / (1UL << 53));
}
