using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · THE BAR'S PEOPLE — the Magpie's resolved post, the numbered chairs and the evening's churn, the
/// oracle's watch, and the four seated regulars resolved per watch.
///
/// <para>Split out of <c>HavenInterior.cs</c> under #251 as a pure move: three runs of the base file, no
/// member renamed, re-scoped or re-ordered. Every field these read — <c>MagpieBarPost</c>,
/// <c>MagpieBackPost</c>, <c>MagpieRota</c>, <c>PatronSeats</c>, <c>OracleCorner</c> — is a
/// <c>static readonly</c> measured off <c>HallTopY</c> and stays in the opening file, in its original order
/// (#1163).</para>
/// </summary>
public static partial class HavenInterior
{
    /// <summary>Where the Magpie is at <paramref name="simTime"/>. If the rota would place them in the
    /// back room but it hasn't been cracked open yet, they're simply out of reach (the GONE slot) —
    /// so the deck never draws them standing inside a wall that isn't there.</summary>
    public static NpcPost ResolveMagpie(double simTime, bool backRoomOpen)
    {
        NpcPost p = MagpieRota.Resolve(simTime);
        return p.Location == "BACK ROOM" && !backRoomOpen ? MagpieRota.PostAt(1) : p;
    }

    /// <summary>The bar's seated-regular pool size (issue #410) — the number of chairs the rota seeds
    /// present regulars into. Exposed for tests that assert distinct, in-range seat assignment.</summary>
    public static int PatronSeatCount => PatronSeats.Length;

    /// <summary>#731 · ONE OF THE BAR'S NUMBERED CHAIRS, BY ITS INDEX — the pool above, published rather than
    /// copied.
    ///
    /// <para>Somebody who comes out of the back and takes a seat has to be WALKED to it, and a walk needs a
    /// coordinate. Measured on the page it would be a second opinion about where this bar's chairs are, which
    /// is the one kind of number a client file is never allowed to hold twice (§13.15) — so the room answers
    /// it, off the same array the rota seats people into and the deck draws them at.</para>
    ///
    /// <para>Null for an index outside the pool, which is the honest answer and never a chair invented at the
    /// origin.</para></summary>
    public static DeckReachability.Point? PatronSeatAt(int index) =>
        index < 0 || index >= PatronSeats.Length
            ? null
            : new DeckReachability.Point(PatronSeats[index].X, PatronSeats[index].Y);

    /// <summary>
    /// #731 · <b>WHAT THE WATCH HAS DONE TO THIS ROOM SINCE THE CAPTAIN WALKED INTO IT.</b>
    ///
    /// <para><b>Owner, 2026-09-01:</b> <i>"also just other customers arriving and leaving in the bars already
    /// does a lot… they can go behind doors that are locked to us."</i> The rota (<see cref="PatronRota"/>)
    /// answers who is drinking here on a WATCH, and until this lane that answer was frozen for the whole
    /// visit: a regular seated when you docked was seated when you cast off. The owner's own complaint about
    /// this room was exactly that — <i>"on the bar now they have to wait for us to leave before they can sit
    /// up… or leave the bar."</i></para>
    ///
    /// <para>This is the room's memory of the two things that can happen to it while somebody is standing in
    /// it, and it is applied INSIDE <see cref="ResolveRegulars"/> so that every reader — the consoles the [E]
    /// key finds, the figures the renderer draws, and the barkeep's own line about who is in tonight — reads
    /// one answer. A churn applied in one of those three places and not the others is the drawn room and the
    /// walked room disagreeing, which is this repository's third named bug class.</para>
    ///
    /// <para>It belongs to the PAGE, because it is a fact about one evening rather than about a watch: WHO
    /// churns and WHEN is dealt off the frozen watch and is deterministic (<see cref="Egress"/>), but whether
    /// it has happened yet is how long the captain has been standing there.</para>
    /// </summary>
    /// <param name="Left">Who has stood up and walked out. Their chair is empty and their console is gone —
    /// [E] finds nothing there, exactly as it finds nothing at an away regular's chair.</param>
    /// <param name="CameIn">…and who has come out of the back and sat down, by the chair they took. A chair
    /// nobody else holds, allotted by the caller, because a free chair is a fact about a room.</param>
    public readonly record struct RoomChurn(
        IReadOnlySet<string> Left, IReadOnlyDictionary<string, int> CameIn)
    {
        /// <summary>Has anything happened at all? A room nobody has left and nobody has come into is the room
        /// the rota already describes, so the deck may be shared out of the cache untouched.</summary>
        public bool Anything => Left.Count > 0 || CameIn.Count > 0;

        /// <summary>What tells this churn from another, for a cache key. Ordered, so two rooms with the same
        /// people in them cannot be told apart by the order somebody was added.</summary>
        public string Signature =>
            string.Join(",", Left.OrderBy(s => s, System.StringComparer.Ordinal))
            + "/"
            + string.Join(",", CameIn.OrderBy(p => p.Key, System.StringComparer.Ordinal)
                                     .Select(p => $"{p.Key}@{p.Value}"));
    }

    /// <summary>Is the oracle at this bar on this docking watch? The pure Core rota (OracleRant.PresentAt);
    /// exposed so the interaction gate and the deck build agree on whether her corner holds anyone.
    /// <paramref name="forced"/> is the <c>?oracle=1</c> seat cheat (#428) — passed straight through to Core,
    /// so the console the deck plants and the gate the E-key reads can never disagree about it.</summary>
    public static bool OraclePresent(string bodyId, double simTime, bool forced = false) =>
        SpaceSails.Core.OracleRant.PresentAt(bodyId, simTime, forced);

    /// <summary>One resolved seated regular for a bar watch: the same shout-name id the contact systems
    /// key on, whether they're at a table this watch, and — when present — the deck coords of their seat
    /// (with a per-regular seeded facing so two visits don't line up identically). Away regulars carry
    /// <see cref="Present"/> = false and are parked off-frame by the droid fill.</summary>
    /// <param name="State">WHY they are or aren't here — at a table, stepped out, or away in the back.
    /// The rota has always computed this; until the bar could SAY it, an away regular was an empty chair
    /// with no console and therefore no sentence, and the distinction lived only in Core.</param>
    public readonly record struct SeatedRegular(string Id, string Label, string ShortName, bool Present, double X, double Y, double Facing, ulong Seed, PatronState State);

    // The floating deck-label short-name per regular (the droid tag, kept as it read before #410); the
    // full shout-name id lives on the console. Unknown ids fall back to the id itself.
    private static string ShortNameFor(string id) => id switch
    {
        "ONE-EYE SILAS" => "Silas",
        "MADAM COIL" => "Coil",
        "GILT-EYE" => "Gilt-Eye",
        "THE FIXER" => "The Fixer",
        _ => id,
    };

    /// <summary>Resolve the four regulars for <paramref name="bodyId"/> at <paramref name="simTime"/> —
    /// the pure rota (<see cref="PatronRota"/>) turned into deck seats (<see cref="PatronSeats"/>). Which
    /// regulars are present, and which chair each took, is a deterministic function of the station and the
    /// sim-time watch, so the console placement, the droid fill and any interaction gate all agree.</summary>
    /// <param name="churn">#731 · What has happened to the room since the captain walked in — who has stood
    /// up and gone, and who has come out of the back and sat down. Null for the rota's own untouched answer,
    /// which is what every caller that only wants the geometry asks for.</param>
    public static IReadOnlyList<SeatedRegular> ResolveRegulars(
        string bodyId, double simTime, RoomChurn? churn = null)
    {
        var seated = new List<SeatedRegular>(PatronRota.Roster.Count);
        foreach (PatronSeating s in PatronRota.ResolveSeating(bodyId, simTime, PatronSeats.Length))
        {
            // ── #731 · THE ROOM'S OWN EVENING, over the top of the watch's own answer ──
            //
            // Applied HERE and in exactly one place, because three readers ask this question — the [E]
            // consoles, the drawn figures, and the barkeep's line about who is in tonight — and a room that
            // answered two of them would be a chair with a man drawn in it that the key finds nobody at.
            PatronState state = s.State;
            int seat = s.SeatIndex;
            if (churn is { } room)
            {
                if (room.Left.Contains(s.Regular))
                {
                    // He got up and walked out through a leaf that does not open for you. As far as this room
                    // is now concerned he has stepped out, which is the truest of the three states it has.
                    (state, seat) = (PatronState.Gone, -1);
                }
                else if (room.CameIn.TryGetValue(s.Regular, out int took))
                {
                    (state, seat) = (PatronState.AtBar, took);
                }
            }

            bool present = state == PatronState.AtBar && seat >= 0 && seat < PatronSeats.Length;
            (float sx, float sy) = present ? PatronSeats[seat] : default;
            // A seeded base facing per (regular, watch) so a returning captain doesn't find them frozen at
            // the identical angle each visit — small idle life on top of the per-frame thermal jitter.
            ulong seed = RegularSeed(s.Regular, PatronRota.WatchIndex(simTime));
            double facing = -System.Math.PI / 2 + (SpaceSails.Core.ReeverIdle.FacingTwitchAt(seed, 0) * 1.5);
            seated.Add(new SeatedRegular(s.Regular, $"◈ {s.Regular}", ShortNameFor(s.Regular), present, sx, sy, facing, seed, state));
        }
        return seated;
    }

    /// <summary>#731 · WHICH OF THE BAR'S NUMBERED CHAIRS NOBODY IS IN, on this watch as this evening has left
    /// it — in the pool's own order, so a caller allotting one to somebody walking in gets the same chair on
    /// every machine.
    ///
    /// <para>Read off <see cref="ResolveRegulars"/> rather than off the rota, so a chair whose regular has
    /// stood up and gone is free again and a chair somebody has just taken is not: the room's own answer, and
    /// never a second tally of it.</para></summary>
    public static IReadOnlyList<int> FreePatronSeats(string bodyId, double simTime, RoomChurn? churn = null)
    {
        var taken = new HashSet<int>();
        foreach (SeatedRegular r in ResolveRegulars(bodyId, simTime, churn))
        {
            if (r.Present)
            {
                taken.Add(SeatIndexOf(r));
            }
        }

        // #1202 · …and Rauha Lind's chair on her watch, reserved whether or not she is in it, so nobody coming
        // out of the back is ever allotted the seat that is hers (HavenInterior.Stringer).
        if (TheStringersChair(bodyId, simTime) is { } hers)
        {
            taken.Add(hers);
        }

        var free = new List<int>(PatronSeats.Length);
        for (int i = 0; i < PatronSeats.Length; i++)
        {
            if (!taken.Contains(i))
            {
                free.Add(i);
            }
        }
        return free;
    }

    /// <summary>Which numbered chair a present regular is in, by matching their drawn seat back to the pool —
    /// a lookup and not a second geometry. −1 for anybody the room is not seating.</summary>
    private static int SeatIndexOf(SeatedRegular r)
    {
        for (int i = 0; i < PatronSeats.Length; i++)
        {
            if (System.Math.Abs(PatronSeats[i].X - r.X) < 1e-3
                && System.Math.Abs(PatronSeats[i].Y - r.Y) < 1e-3)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>A stable per-regular jitter seed (issue #410 idle life): folds the regular's name and the
    /// watch so their thermal shuffle differs from their neighbours' and from their own last visit.</summary>
    public static ulong RegularSeed(string regular, long watch)
    {
        ulong h = 0x9E3779B97F4A7C15UL;
        foreach (char c in regular)
        {
            h = (h ^ c) * 0x100000001B3UL;
        }
        return h ^ (ulong)watch;
    }
}
