namespace SpaceSails.Core;

// ─── VAULT SECTIONS: THE PAPERWORK THAT FOLLOWS A CAPTAIN (#225) ───
//
// A claim lodged on a lost hull, a writ waiting to be served, a collar already cleared, the harbours
// that declined and the ones that quietly found a berth anyway, and the weather the underwriters are
// reading. What somebody else has written down about this ship.
// 
// Split out of Vault.cs under #251 — whole record types moved, nothing inside one re-ordered.

/// <summary>#1151 — <see cref="ProgressSection.ClaimOwed"/>'s row: the hull that was named at the counter and
/// what the claim is worth. The payout is STORED rather than recomputed when the rep arrives, because it was
/// quoted off the policy the captain held on the day: a premium that lapses between the kiosk and the
/// salesman does not un-lodge a claim, and a claim re-priced at payout time would be a second opinion about
/// <see cref="InsuranceRule.HullClaimPayoutCr"/>.</summary>
public sealed record LodgedClaimRecord(string HullName, int PayoutCr, double LodgedAtSimTime);

/// <summary>#1151 — <see cref="ProgressSection.WritPending"/>'s row: whose contract it is, and the berth they
/// are waiting at (<see cref="QuietHands.PortFor"/>'s harbour, or the port she was clamped to). A BERTH
/// rather than a ground, because a ground has no berths to let and a pursuer waits where a captain has to
/// come back to.
///
/// <para><b>#1151 slice 4 · AND THE TERMS THE WRIT WAS WRITTEN ON.</b> A writ that waited is served on the
/// terms it had at the moment of filing — no discount for the wait, and no penalty for it — so the demand it
/// opens cannot be re-priced off a heat gauge that has decayed since, or been raised again by something the
/// contract was never about. The two things a boarding demand is cut from are STORED rather than recomputed:
/// the heat the contract was worth (<paramref name="HeatWhenFiled"/>) and the pursuer's own id, which
/// together with <paramref name="FiledAtSimTime"/> is the whole of that demand's seed. Asking the world
/// again at service time would be a second opinion about a bill that was already written, which is
/// <see cref="LodgedClaimRecord"/>'s own reason one docblock up.</para>
///
/// <para>Both are OPTIONAL, so a vault written before slice 4 — a writ filed and not yet served — loads as
/// what it is rather than as a broken file. Such a writ is served at heat 1, the floor every demand in the
/// game already has.</para></summary>
public sealed record PendingWritRecord(
    string Callsign, string HavenId, double FiledAtSimTime, int HeatWhenFiled = 0, string? HunterId = null);

/// <summary>#525 — <see cref="ProgressSection.CollarCleared"/>'s row: the port, the slot the ship that
/// declared the overload was tied up in, the neighbouring slots the roster emptied, and <b>why</b>.
///
/// <para>The reason rides as a STRING and is parsed back, rather than as the enum serialized directly, for
/// <see cref="HallDeclineRecord"/>'s own reason and one of its own: a file written by a later build with a
/// second reason in it has to load on this one as a reassignment this build cannot account for — which is
/// #1092's kind and not this one's — rather than as a silently wrong cause.</para></summary>
public sealed record ClearedCollarRecord(
    string HavenId, int Berth, IReadOnlyList<int> Neighbours, string Reason);

/// <summary>#1068 — one row of <see cref="ProgressSection.HallsDeclined"/>: a ground, and the world-side
/// window the world declined on it in. A wire record of its own rather than
/// <see cref="PoliteDecline.Decline"/> serialized directly, for <see cref="HallOpeningRecord"/>'s own
/// reason — this file is a FORMAT, and a format spelled as a domain type moves the day the domain type is
/// renamed, with old saves silently losing the field.</summary>
public sealed record HallDeclineRecord(string BodyId, long Window);

/// <summary>#1068 — one row of <see cref="ProgressSection.HallsHandled"/>: a ground, the world-side window
/// the harbour filed it in, and whether the reassigned berth has already been given. A wire record of its
/// own rather than <see cref="QuietHands.Hand"/> serialized directly, for
/// <see cref="HallDeclineRecord"/>'s own reason.</summary>
public sealed record QuietHandRecord(string BodyId, long Window, bool BerthGiven);

/// <summary>#677 — one row of <see cref="ProgressSection.HallsOpened"/>: a ground, and the world-side window
/// its halls were first entered in.
///
/// <para>A wire record of its own rather than <see cref="DisclosureClock.Opening"/> serialized directly, for
/// the reason <see cref="DiceItemRecord"/> is one: this file is a FORMAT, and a format that is spelled as a
/// domain type moves every time the domain type is renamed — with old saves silently losing the field, which
/// is the one failure mode this serializer's two promises exist to prevent.</para></summary>
public sealed record HallOpeningRecord(string BodyId, long Window);

// ── The captain's nerve (#317, first slice of #226): the sanity gauge that debuts on the regolith. ──

/// <summary>#973 · The void's weather (<see cref="InsuranceWeather"/>). Opaque pipe-separated rows, the house
/// idiom: the file carries how OFTEN a line was heard and never the sentence, so an edited save can never make
/// the room say something nobody wrote. A row this build cannot parse is dropped rather than thrown over.</summary>
public sealed record InsuranceWeatherSection
{
    /// <summary>One row per line the captain has heard at all: <c>lineId|times</c>.</summary>
    public IReadOnlyList<string> Heard { get; init; } = [];

    /// <summary>One row per station the captain has drunk at: <c>stationId|visits|lastSaidAtVisit</c>, where
    /// the last field is −1 for a station the weather has never blown through.</summary>
    public IReadOnlyList<string> Stations { get; init; } = [];

    /// <summary>
    /// #1061 beat 2 · <b>THE GROUNDS BREM KOLT HAS BEEN FOUND ON</b> — one row per ground
    /// (<see cref="HardcaseRep.GroundKey"/>), at most <see cref="HardcaseRep.GroundsAtMost"/> of them.
    ///
    /// <para>It rides the vault because the cap is a fact about a UNIVERSE and not about a walk: <i>"They all
    /// decline on the first moon. The book says you'll sign on the second"</i> is a promise the game keeps
    /// across a save, and a captain who quit between moons and came back to a third Kolt would have caught
    /// the game forgetting its own line. It is kept beside the insurance weather because it is the same
    /// arc's bookkeeping and this section is already the file's answer to <i>what has Nebula Mutual done to
    /// this captain</i>.</para>
    ///
    /// <para><b>Written only when he has actually been found somewhere</b> — the #1057/#1066 pattern, and
    /// not a stylistic choice: the checksum is taken over the payload, so an extra <c>"hardcase": []</c> on
    /// every save would change the digest of every vault ever written and hang the 📛 tampered marker on an
    /// honest voyage. Null means "this universe has never met him", which is the truth about every save
    /// written before this lane.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Hardcase { get; init; }
}
