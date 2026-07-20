namespace SpaceSails.Core.Interior;

/// <summary>Where a bar regular is on a given watch: at a table, stepped out, or gone in the back
/// (issue #410, owner 2026-07-20 "Are the contacts moving and not in same seats in same bars?").
/// <see cref="AtBar"/> is the only state the deck draws a patron for; the two away states differ only in
/// the "drifted off" flavor.</summary>
public enum PatronState
{
    /// <summary>Sitting at a table this watch — found, and drinkable-with.</summary>
    AtBar,

    /// <summary>Stepped out — an empty chair. Opportunity or dread, not a bug.</summary>
    Gone,

    /// <summary>Away in the back / a locked room — same empty chair, a shadier reason.</summary>
    InTheBack,
}

/// <summary>One regular's resolved seating for a watch: which of the bar's numbered seats they took
/// (−1 when away), and why. <see cref="Present"/> is the deck's draw gate.</summary>
public readonly record struct PatronSeating(string Regular, PatronState State, int SeatIndex)
{
    /// <summary>At a table this watch (so the deck seats them and E finds them).</summary>
    public bool Present => State == PatronState.AtBar;
}

/// <summary>
/// The roving-regulars rota (issue #410): the four bar regulars (One-Eye Silas, Madam Coil, Gilt-Eye,
/// The Fixer) are no longer one shared roster nailed to fixed chairs in every bar. Each is present at a
/// given port only <b>sometimes</b>, seeded by (station + sim-time watch), and when they are they take a
/// <b>different</b> seat from the bar's pool — the same "people cannot be static furniture" ruling that
/// already sends the Magpie roaming (<see cref="NpcSchedule"/>), extended to the seated cast.
///
/// <para>Same 4-sim-hour watch beat as the Magpie, so a docked captain who warps the clock sees the room
/// change over. <b>Per-place skew</b>: which regulars haunt which ports is biased, not everyone-everywhere
/// (the Fixer works grey-market Cinder Roost; the straight-laced oldest port sees less of them) — the
/// weights are data in <see cref="Affinities"/> and exposed via <see cref="Affinity"/>.</para>
///
/// <para>Pure and deterministic (repo agreement §9): presence and seat are a function of the station id,
/// the sim-time watch, and the seat count alone — no <see cref="System.Random"/>, no clock read — via a
/// splitmix64 hash (the same finalizer <see cref="ReeverIdle"/> seeds its shuffle with). So the deck
/// build, the droid fill, and the interaction gate all read one answer for one watch, and it is unit-
/// testable without a browser. Contacts stay keyed by their shout-name id, never by seat, so the drink /
/// rumor / pick systems keep working whichever chair a regular drifts to (issue #410 item 5).</para>
/// </summary>
public static class PatronRota
{
    /// <summary>Sim-seconds a regular holds one presence state before the rota re-rolls — the Magpie's
    /// watch length (four sim-hours), so the whole bar cast shuffles on the same beat.</summary>
    public const double WatchSeconds = 4 * 3600;

    /// <summary>The four seated regulars, by the shout-name the bar consoles and the contact systems key
    /// on (<c>ContactSheets.For</c>, the quest givers). Order is stable — it seats present regulars.</summary>
    public static readonly IReadOnlyList<string> Roster =
    [
        "ONE-EYE SILAS", "MADAM COIL", "GILT-EYE", "THE FIXER",
    ];

    /// <summary>The baseline chance a regular is at a bar on any given watch, when no honest bias applies.
    /// With four regulars this seats a little over two of them on average — a lived-in room, the odd empty
    /// chair, and a rare quiet house (dread, not a bug).</summary>
    public const double DefaultAffinity = 0.6;

    // Per-place skews (issue #410 item 3): honest biases on who frequents which port, keyed
    // "<REGULAR>|<stationId>". Only the biased pairs are listed; everything else is DefaultAffinity.
    // These are exposed (Affinity / Affinities) so the flavor is data, not a magic number in a hash.
    private static readonly IReadOnlyDictionary<string, double> AffinityTable =
        new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase)
        {
            // The Fixer haunts grey-market Cinder Roost (Venus clouds, the fence's back room is here) —
            // and shows their face far less at Selene Gate, the oldest, most-stamped port in the system.
            ["THE FIXER|cinder-roost"] = 0.88,
            ["THE FIXER|selene-gate"] = 0.32,

            // One-Eye Silas, the bounty fence, works the trade hubs: the Ringside exchange and his home
            // berth at the Rusty Roadstead. He's a rarer sight out at the cold, half-empty Deep.
            ["ONE-EYE SILAS|ringside-exchange"] = 0.82,
            ["ONE-EYE SILAS|the-space-bar"] = 0.80,
            ["ONE-EYE SILAS|the-deep"] = 0.38,

            // Madam Coil runs quiet parcels through the dark web; she lingers where lockups get cracked
            // (the sideways Tilt, codes forever going around) and passes through the busy Roadstead less.
            ["MADAM COIL|the-tilt"] = 0.78,
            ["MADAM COIL|the-space-bar"] = 0.48,

            // Gilt-Eye, the intel dealer, works the old crossroads where everyone's traffic pauses
            // (Selene Gate) and is scarce out at the quiet storm-pilgrim Red Eye.
            ["GILT-EYE|selene-gate"] = 0.80,
            ["GILT-EYE|red-eye"] = 0.36,
        };

    /// <summary>The per-place skews as a read-only map (issue #410 item 3, "expose the weights"): every
    /// biased <c>"&lt;REGULAR&gt;|&lt;stationId&gt;"</c> pair and its presence weight. Unlisted pairs use
    /// <see cref="DefaultAffinity"/>.</summary>
    public static IReadOnlyDictionary<string, double> Affinities => AffinityTable;

    /// <summary>How likely <paramref name="regular"/> is to be at <paramref name="stationId"/> on any
    /// watch, in (0,1] — the honest bias if one is authored, else <see cref="DefaultAffinity"/>.</summary>
    public static double Affinity(string regular, string stationId) =>
        AffinityTable.TryGetValue($"{regular}|{stationId}", out double w) ? w : DefaultAffinity;

    /// <summary>The watch index at <paramref name="simTime"/> — floor-divide by the watch length, correct
    /// for negative sim times too (defensive; the clock starts at 0), so it is a stable per-watch seed.</summary>
    public static long WatchIndex(double simTime) => (long)System.Math.Floor(simTime / WatchSeconds);

    /// <summary>Where <paramref name="regular"/> is at <paramref name="stationId"/> on the watch containing
    /// <paramref name="simTime"/>. Present when the watch's seeded roll clears their <see cref="Affinity"/>;
    /// otherwise <see cref="PatronState.Gone"/> or <see cref="PatronState.InTheBack"/> (a second seeded bit,
    /// pure flavor for the empty-chair line).</summary>
    public static PatronState Resolve(string regular, string stationId, double simTime)
    {
        long watch = WatchIndex(simTime);
        double roll = Unit(Hash(Key(regular, stationId), (ulong)watch, 0xA1));
        if (roll < Affinity(regular, stationId))
        {
            return PatronState.AtBar;
        }
        // Away: split the two flavors evenly — a coin on an independent salt.
        return Unit(Hash(Key(regular, stationId), (ulong)watch, 0xB2)) < 0.5
            ? PatronState.Gone
            : PatronState.InTheBack;
    }

    /// <summary>
    /// The whole cast's seating for a bar on the watch containing <paramref name="simTime"/>: one entry
    /// per regular (in <see cref="Roster"/> order), each with their state and — for the present ones — a
    /// <b>distinct</b> seat index in <c>[0, seatCount)</c>, drawn from a seeded permutation of the seats so
    /// the same regular takes a different chair on a different watch and two regulars never share one.
    /// Away regulars carry <c>SeatIndex = -1</c>. Deterministic in (station, watch, seatCount).
    /// </summary>
    public static IReadOnlyList<PatronSeating> ResolveSeating(string stationId, double simTime, int seatCount)
    {
        if (seatCount < 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(seatCount), seatCount, "Seat count cannot be negative.");
        }

        long watch = WatchIndex(simTime);
        int[] seats = SeatPermutation(stationId, watch, seatCount);

        var result = new List<PatronSeating>(Roster.Count);
        int taken = 0;
        foreach (string regular in Roster)
        {
            PatronState state = Resolve(regular, stationId, simTime);
            if (state == PatronState.AtBar && taken < seatCount)
            {
                result.Add(new PatronSeating(regular, state, seats[taken]));
                taken++;
            }
            else
            {
                // Away, or present but the room ran out of seats (more regulars than chairs) — treat the
                // overflow as stepped out this watch rather than double-booking a table.
                PatronState shown = state == PatronState.AtBar ? PatronState.Gone : state;
                result.Add(new PatronSeating(regular, shown, -1));
            }
        }
        return result;
    }

    // A seeded Fisher–Yates permutation of [0, n) — the seat order for this station/watch. Pure: the RNG
    // is a splitmix64 stream advanced by the loop, seeded from the station and watch, so the shuffle is
    // stable for a watch and re-rolls the next one (a regular drifts to a new chair between visits).
    private static int[] SeatPermutation(string stationId, long watch, int n)
    {
        var order = new int[n];
        for (int i = 0; i < n; i++)
        {
            order[i] = i;
        }
        ulong state = Hash(Key("SEATS", stationId), (ulong)watch, 0xC3);
        for (int i = n - 1; i > 0; i--)
        {
            state = SplitMix64(state);
            int j = (int)(state % (ulong)(i + 1));
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

    // A stable 64-bit key for a (name, station) pair — folds the two strings into a seed with distinct
    // salts so "SILAS|ringside" and "SEATS|ringside" never collide.
    private static ulong Key(string a, string b) => Fold(a, 0x100) ^ Fold(b, 0x200);

    private static ulong Fold(string s, ulong salt)
    {
        ulong h = salt;
        foreach (char c in s ?? string.Empty)
        {
            h = SplitMix64(h + c);
        }
        return h;
    }

    // splitmix64 finalizer over (seed, stream, salt) → a well-mixed 64-bit value, the same platform-stable
    // idiom ReeverIdle.Phase uses (no System.Random, no clock — determinism is law in Core).
    private static ulong Hash(ulong seed, ulong stream, ulong salt) =>
        SplitMix64(SplitMix64(seed ^ (salt * 0x9E3779B97F4A7C15UL)) + stream);

    private static ulong SplitMix64(ulong z)
    {
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    // A 64-bit hash to a double in [0,1).
    private static double Unit(ulong h) => (h >> 11) * (1.0 / (1UL << 53));
}
