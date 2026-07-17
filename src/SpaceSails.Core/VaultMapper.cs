using System.Collections.Immutable;

namespace SpaceSails.Core;

/// <summary>
/// Maps the live Core game records (<see cref="ContactLedger"/>, <see cref="CacheLedger"/>,
/// <see cref="HotCargoLedger"/>, <see cref="PirateInsurance"/>, <see cref="FavorObligation"/>) to and
/// from the flat, self-described <see cref="Vault"/> sections. Kept Core-side so the round-trip is one
/// tested truth rather than hand-rolled in the client — the client only gathers its own primitives
/// (purse, tank, cargo, upgrade levels) into the remaining sections.
///
/// <para>Every "to section" is total; every "apply" is lossless against its "to" counterpart, and
/// tolerant of a null/partial section (it simply applies what it has). Enum values cross as their int
/// so an unknown future <see cref="CreditKind"/> or <see cref="InsuranceTier"/> survives a save/load
/// as a number instead of failing.</para>
/// </summary>
public static class VaultMapper
{
    // ── Contacts ──

    public static ContactsSection ToSection(ContactLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        var contacts = new List<ContactRecord>(ledger.Entries.Count);
        foreach (ContactHistory h in ledger.Entries.Values)
        {
            contacts.Add(new ContactRecord
            {
                ContactId = h.ContactId,
                DisplayName = h.DisplayName,
                MissionsCompleted = h.MissionsCompleted,
                TotalPaidCredits = h.TotalPaidCredits,
                LastCompletedSimTime = h.LastCompletedSimTime,
                Hostile = h.Hostile,
                CreditBalance = h.CreditBalance,
                Goodwill = h.Goodwill,
                Transactions = h.Transactions
                    .Select(t => new CreditTxnRecord((int)t.Kind, t.Amount, t.SimTime, t.Note))
                    .ToList(),
            });
        }

        return new ContactsSection(contacts);
    }

    /// <summary>Rehydrate a ledger from the section (verbatim, via <see cref="ContactLedger.Load"/>).
    /// A null section is a no-op — the ledger simply starts blank.</summary>
    public static void Apply(ContactsSection? section, ContactLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        if (section is null)
        {
            return;
        }

        foreach (ContactRecord r in section.Contacts)
        {
            ImmutableArray<CreditTransaction> txns = r.Transactions
                .Select(t => new CreditTransaction((CreditKind)t.Kind, t.Amount, t.SimTime, t.Note))
                .ToImmutableArray();

            var history = new ContactHistory(
                r.ContactId, r.DisplayName, r.MissionsCompleted, r.TotalPaidCredits,
                r.LastCompletedSimTime, r.Hostile)
            {
                CreditBalance = r.CreditBalance,
                Goodwill = r.Goodwill,
                Transactions = txns,
            };
            ledger.Load(history);
        }
    }

    // ── Caches / hoard ──

    public static CachesSection ToSection(CacheLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        var caches = ledger.Caches.Select(ToRecord).ToList();
        return new CachesSection { NextMintIndex = ledger.NextMintIndex(), Caches = caches };
    }

    public static void Apply(CachesSection? section, CacheLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        if (section is null)
        {
            return;
        }

        foreach (CacheRecord r in section.Caches)
        {
            ledger.Load(ToCache(r));
        }

        ledger.RestoreMintIndex(section.NextMintIndex);
    }

    private static CacheRecord ToRecord(TreasureCache c) => new()
    {
        Id = c.Id,
        BodyId = c.BodyId,
        LandmarkName = c.LandmarkName,
        Bearing = c.Bearing,
        Paces = c.Paces,
        Coin = c.Coin,
        Cargo = (c.Cargo ?? []).Select(g => new CacheCargoRecord(g.CargoClass, g.Units, g.Hot)).ToList(),
        BuriedSimTime = c.BuriedSimTime,
        Owner = c.Owner,
        PlayerOwned = c.PlayerOwned,
    };

    private static TreasureCache ToCache(CacheRecord r) => new(
        r.Id, r.BodyId, r.LandmarkName, r.Bearing, r.Paces, r.Coin,
        (r.Cargo ?? []).Select(g => new CacheCargo(g.CargoClass, g.Units, g.Hot)).ToList(),
        r.BuriedSimTime, r.Owner, r.PlayerOwned);

    // ── Hot cargo (the stolen-while-heated flags). These ride in CargoSection.Hot. ──

    public static IReadOnlyList<HotCargoLine> ToHotLines(HotCargoLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        return ledger.Entries.Select(kv => new HotCargoLine(kv.Key, kv.Value)).ToList();
    }

    public static void ApplyHot(IReadOnlyList<HotCargoLine>? hot, HotCargoLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        if (hot is null)
        {
            return;
        }

        foreach (HotCargoLine line in hot)
        {
            ledger.Load(line.CargoClass, line.HotUnits);
        }
    }

    // ── Insurance ──

    public static InsuranceSection ToSection(PirateInsurance policy) =>
        new((int)policy.Tier, policy.PremiumPaidThroughSimTime);

    public static PirateInsurance ToInsurance(InsuranceSection? section) =>
        section is null
            ? PirateInsurance.Uninsured
            : new PirateInsurance((InsuranceTier)section.Tier, section.PremiumPaidThroughSimTime);

    // ── Favor obligations (the favor-debt queue) ──

    public static IReadOnlyList<ObligationRecord> ToRecords(IEnumerable<FavorObligation> obligations)
    {
        ArgumentNullException.ThrowIfNull(obligations);
        return obligations
            .Select(o => new ObligationRecord(o.ContactId, o.DisplayName, o.PrincipalCredits, o.IncurredSimTime, o.VoiceLine))
            .ToList();
    }

    public static IReadOnlyList<FavorObligation> ToObligations(IEnumerable<ObligationRecord>? records)
    {
        if (records is null)
        {
            return [];
        }

        return records
            .Select(r => new FavorObligation(r.ContactId, r.DisplayName, r.PrincipalCredits, r.IncurredSimTime, r.VoiceLine))
            .ToList();
    }
}

/// <summary>
/// The resume-berth selector (owner's law, #225): the vault always resumes the pirate DOCKED. If the
/// save happened at a berth, that berth IS the resume. If it happened in flight, the NEAREST dockable
/// haven is chosen and stored THEN (never a trajectory) — so a load rebuilds the ship clamped there at
/// load-time ephemeris. Pure and deterministic, so the docked-vs-nearest choice is a tested truth.
/// </summary>
public static class VaultResume
{
    /// <summary>A dockable haven's identity and where it was at save time.</summary>
    public readonly record struct HavenLocus(string Id, string Name, Vector2d Position);

    /// <summary>Choose the resume berth. When <paramref name="dockedHavenId"/> names a haven in
    /// <paramref name="havens"/>, that berth resumes (WasDocked = true). Otherwise the nearest haven to
    /// <paramref name="shipPosition"/> is chosen (WasDocked = false). Null when there are no havens to
    /// resume at (the caller then omits the resume section and falls back to a fresh start).</summary>
    public static ResumeSection? Select(
        string? dockedHavenId, Vector2d shipPosition, IReadOnlyList<HavenLocus> havens)
    {
        ArgumentNullException.ThrowIfNull(havens);
        if (havens.Count == 0)
        {
            return null;
        }

        if (dockedHavenId is not null)
        {
            foreach (HavenLocus h in havens)
            {
                if (h.Id == dockedHavenId)
                {
                    return new ResumeSection { HavenId = h.Id, HavenName = h.Name, WasDocked = true };
                }
            }
            // Docked id no longer among the havens (scenario changed) — fall through to nearest.
        }

        HavenLocus nearest = havens[0];
        double best = (havens[0].Position - shipPosition).LengthSquared;
        for (int i = 1; i < havens.Count; i++)
        {
            double d = (havens[i].Position - shipPosition).LengthSquared;
            if (d < best)
            {
                best = d;
                nearest = havens[i];
            }
        }

        return new ResumeSection { HavenId = nearest.Id, HavenName = nearest.Name, WasDocked = false };
    }
}
