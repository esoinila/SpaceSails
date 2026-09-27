namespace SpaceSails.Core;

/// <summary>
/// #251 · §3–§4 · THE POSTINGS AND THE SIGNER — the berths, who is posted where, and the signer who
/// reports you.
///
/// <para>Split out of <c>OldCrew.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered. Its one field is a <c>const</c>.</para>
/// </summary>
public static partial class OldCrew
{
    // ── §3 · THE POSTINGS, ROLLED SECOND ─────────────────────────────────────────────────────────────

    /// <summary>One berth the world offers, as the postings see it: an id and how big the place is. The
    /// client builds this list off the live ephemeris (<see cref="BerthsOf"/>); a test builds it by hand,
    /// which is the whole reason the postings take a list rather than a world.</summary>
    public readonly record struct Berth(string Id, ArrivalTube.Tier Tier);

    /// <summary>Every dockable berth in the scenario with the tier it has earned — the list the postings are
    /// drawn from.</summary>
    public static IReadOnlyList<Berth> BerthsOf(ICelestialEphemeris ephemeris)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        var berths = new List<Berth>();
        foreach (CelestialBody body in DockableHavens.All(ephemeris))
        {
            berths.Add(new Berth(body.Id, ArrivalTube.TierFor(ephemeris, body.Id)));
        }

        return berths;
    }

    /// <summary>Which berths a place of this kind can exist at. A Nebula claims desk is at a GREAT PORT and
    /// nowhere else (the desks are where the traffic is); a working berth's bar is at a working berth; every
    /// other office will take whatever the system has. A kind whose tier the scenario does not contain falls
    /// back to the whole list rather than leaving a shipmate unposted — a person with no job is not a
    /// contact, and a seeding that silently dropped one would be four people three of the time.</summary>
    public static IReadOnlyList<Berth> BerthsFor(PlaceKind kind, IReadOnlyList<Berth> berths)
    {
        ArgumentNullException.ThrowIfNull(berths);
        ArrivalTube.Tier? want = kind switch
        {
            PlaceKind.NebulaClaimsDesk => ArrivalTube.Tier.GreatPort,
            PlaceKind.WorkingBerthBar => ArrivalTube.Tier.WorkingBerth,
            _ => null,
        };

        if (want is not { } tier)
        {
            return berths;
        }

        var matching = new List<Berth>();
        foreach (Berth b in berths)
        {
            if (b.Tier == tier)
            {
                matching.Add(b);
            }
        }

        return matching.Count > 0 ? matching : berths;
    }

    /// <summary>
    /// POST THEM. Called with the bonds ALREADY ROLLED, and that is the law this lane guards: a berth is
    /// chosen from the shipmate's <see cref="PlaceKind"/>, which comes from their <see cref="Role"/>, which
    /// the table decided one step earlier. Nothing about a berth ever reaches back into who somebody was.
    ///
    /// <para>The one arrangement that is not a roll: when the table produced THE CLASSIC, the fling is
    /// posted where the best friend is. She moved to be with him, which is the fact the whole beat is about
    /// — and it is what makes knocking on his door a thing the captain can actually do.</para>
    /// </summary>
    public static IReadOnlyList<Seeded> Post(
        string threadId, IReadOnlyList<Seeded> seeded, IReadOnlyList<Berth> berths)
    {
        ArgumentNullException.ThrowIfNull(threadId);
        ArgumentNullException.ThrowIfNull(seeded);
        ArgumentNullException.ThrowIfNull(berths);
        if (berths.Count == 0)
        {
            return seeded;
        }

        var posted = new List<Seeded>();
        string herBerth = "";
        foreach (Seeded s in seeded)
        {
            Shipmate who = ById(s.Id)!.Value;
            IReadOnlyList<Berth> choices = BerthsFor(who.Posting, berths);
            string at = choices[
                (int)(DiceRule.Seed($"oldcrew|post|{threadId}|{s.Id}") % (ulong)choices.Count)].Id;
            if (s.Id == FlingId)
            {
                herBerth = at;
            }

            posted.Add(s with { StationId = at });
        }

        // …and HE is the one who moved. A registrar's office exists at a great port as readily as anywhere
        // else, while a Nebula claims desk does not exist at a working berth at all — so moving her to him
        // would have broken the one posting rule the world actually asserts, and moving him to her breaks
        // nothing. The fiction is the better way round too: she took the desk, and he followed.
        if (herBerth.Length > 0)
        {
            for (int i = 0; i < posted.Count; i++)
            {
                if (posted[i].Id == BestFriendId && posted[i].IsTheClassic)
                {
                    posted[i] = posted[i] with { StationId = herBerth };
                }
            }
        }

        return posted;
    }

    /// <summary>The whole seeding in the one order it is allowed to happen in: bonds, then postings.</summary>
    public static IReadOnlyList<Seeded> Seed(string threadId, IReadOnlyList<Berth> berths) =>
        Post(threadId, Bonds(threadId), berths);

    // ── §4 · THE SIGNER REPORTS ──────────────────────────────────────────────────────────────────────

    /// <summary>What one meeting with the man who signed costs, in heat owed to the port authority of the
    /// station he works at. One unit: he does not denounce you, he files. FLAGGED for the owner's tuning.</summary>
    public const int SignerReportUnits = 1;

    /// <summary>
    /// #715's charge for a meeting the signer was in the room for. It is owed to the authority of THAT
    /// station and to nobody else, which is the whole of the #715 law: a customs man who reports you has
    /// told his own port, and a port that told the others would be admitting what it keeps in its basement.
    ///
    /// <para>Built through <see cref="SiteOperator.Of"/> and handed to <see cref="IllegalHeat.Bank"/> like
    /// every other crossing — no second banking path, no second key.</para>
    /// </summary>
    public static UndergroundComplex.HeatCharge SignerReport(string stationId)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        return new UndergroundComplex.HeatCharge(SiteOperator.Of(stationId).Id, SignerReportUnits);
    }

    /// <summary>Is the man who signed at this berth? The question the drink's modifier and the report both
    /// ask, asked once so they cannot disagree about the room they are in.</summary>
    public static bool SignerIsAt(IReadOnlyList<Seeded> seeded, string? stationId) =>
        IsAt(seeded, SignerId, stationId);

    /// <summary>Is she at this berth?</summary>
    public static bool FlingIsAt(IReadOnlyList<Seeded> seeded, string? stationId) =>
        IsAt(seeded, FlingId, stationId);

    /// <summary>Is this shipmate posted at this berth right now?</summary>
    public static bool IsAt(IReadOnlyList<Seeded> seeded, string shipmateId, string? stationId)
    {
        ArgumentNullException.ThrowIfNull(seeded);
        if (string.IsNullOrEmpty(stationId))
        {
            return false;
        }

        return Find(seeded, shipmateId) is { } s && string.Equals(s.StationId, stationId, StringComparison.Ordinal);
    }

    /// <summary>Every shipmate posted at this berth, in pool order — who the captain can walk in on.</summary>
    public static IReadOnlyList<Seeded> At(IReadOnlyList<Seeded> seeded, string? stationId)
    {
        ArgumentNullException.ThrowIfNull(seeded);
        if (string.IsNullOrEmpty(stationId))
        {
            return [];
        }

        var here = new List<Seeded>();
        foreach (Seeded s in seeded)
        {
            if (string.Equals(s.StationId, stationId, StringComparison.Ordinal))
            {
                here.Add(s);
            }
        }

        return here;
    }

    /// <summary>#973 L5a · <b>THE DOOR YOU STAND OUTSIDE OF.</b> True when the black book already says the
    /// best friend is with the fling AND both of them are at this berth — the moment the owner's addendum 2
    /// is about, and the only moment the knock costs a nerve pip.</summary>
    public static bool KnockingCostsNerve(IReadOnlyList<Seeded> seeded, string? stationId) =>
        Find(seeded, BestFriendId) is { IsTheClassic: true }
        && IsAt(seeded, BestFriendId, stationId)
        && IsAt(seeded, FlingId, stationId);

    /// <summary>The seeded row for one shipmate id, or null when this thread did not cast them.</summary>
    public static Seeded? Find(IReadOnlyList<Seeded> seeded, string? id)
    {
        ArgumentNullException.ThrowIfNull(seeded);
        foreach (Seeded s in seeded)
        {
            if (string.Equals(s.Id, id, StringComparison.Ordinal))
            {
                return s;
            }
        }

        return null;
    }
}
