namespace SpaceSails.Core;

/// <summary>How good a round-loosened tongue turns out to be (owner 2026-07-18: "ordering a round might
/// drop some tips etc from the people that got it … by their initiative that they might not have offered
/// otherwise"). A strengthening ladder — most regulars stay quiet, a few offer vague color, and a warm
/// known contact may hand you something solid, or the best material of all.</summary>
public enum TipTier
{
    /// <summary>No tip — they raise the glass and say nothing worth keeping. The common case.</summary>
    None,

    /// <summary>Vague color — atmosphere, not intel. All a loosened STRANGER ever offers.</summary>
    Vague,

    /// <summary>A solid, actionable tip — a price whisper, a heat warning, a ship that runs dark.</summary>
    Solid,

    /// <summary>The best material — reserved for a contact who already trusts you deeply.</summary>
    Choice,
}

/// <summary>
/// PR (owner 2026-07-18) · A round for the room loosens tongues. After you stand a round, each regular
/// who actually drank rolls — on THEIR initiative — whether to volunteer something they'd not have
/// offered unasked. Pure, seeded, deterministic: the salted-2D6 idiom the shared drink (#306) and the
/// Reever watchdogs (#295/#303) use, so client and any future server agree on every face.
///
/// <para><b>Their initiative, not a vending machine.</b> This resolver only decides IF and HOW GOOD a
/// volunteered line is; the caller composes the actual words from live world state (a dark-running ship,
/// a soft price, a heat warning) and gates the whole thing to ONCE PER BAR VISIT — a second round the
/// same visit warms goodwill (#283) but the tongues are already loose. Known contacts (a real
/// relationship, goodwill-weighted) volunteer better material; strangers give only vague color.</para>
/// </summary>
public static class RoundTips
{
    /// <summary>The 2D6 die: a d6, rolled twice, salted apart.</summary>
    public const int Faces = 6;

    /// <summary>Roll whether a regular volunteers something after a round, and how good it is.</summary>
    /// <param name="seed">Folds the contact and the moment (caller passes a seed salted per-NPC + the
    /// bar visit) so the roll is deterministic and replayable in a test.</param>
    /// <param name="goodwill">The regular's goodwill BEFORE this round — weights a known contact's
    /// material upward (an old friend leans in with better than a bare acquaintance).</param>
    /// <param name="known">True when the room-mate is a KNOWN contact (ContactLedger history). A stranger
    /// (false) offers only vague color at best, and only sometimes.</param>
    public static TipTier Volunteer(ulong seed, int goodwill, bool known)
    {
        int face1 = DiceRule.Roll(DiceRule.Seed(seed, "round-tip-a"), Faces).Face;
        int face2 = DiceRule.Roll(DiceRule.Seed(seed, "round-tip-b"), Faces).Face;
        int total = face1 + face2;

        if (!known)
        {
            // A stranger, loosened by a free drink, offers only vague color — and only on a warm roll.
            return total >= 9 ? TipTier.Vague : TipTier.None;
        }

        // Goodwill-weighted: a warmer contact leans in easier and with better material (capped so a
        // single deep friend can't turn every round into a firehose).
        total += 1 + System.Math.Clamp(goodwill / 2, 0, 3);
        return total switch
        {
            <= 7 => TipTier.None,
            <= 10 => TipTier.Vague,
            <= 12 => TipTier.Solid,
            _ => TipTier.Choice,
        };
    }
}

/// <summary>
/// The captain's "overheard at the bar" book (#119 receipt/ledger idiom, for intel). Owner 2026-07-18:
/// the words a player paid a round to hear "may not hide" and must not "autodisappear" — so every bar
/// tip/rumor is APPENDED here, durable and revisitable, and round-trips through the Vault
/// (<see cref="OverheardSection"/>). Pure, capped, oldest-trimmed — a log, not a leak.
/// </summary>
public static class OverheardLog
{
    /// <summary>How many lines the book keeps — the most recent, oldest trimmed. Ample for a session's
    /// worth of counter-talk without letting the save grow without bound.</summary>
    public const int Cap = 40;

    /// <summary>Append a line to the book, returning a new capped list (oldest lines trimmed off the
    /// front). Pure — never mutates the input.</summary>
    public static IReadOnlyList<OverheardLine> Append(IReadOnlyList<OverheardLine> log, OverheardLine line)
    {
        var list = new List<OverheardLine>(log ?? []) { line };
        if (list.Count > Cap)
        {
            list.RemoveRange(0, list.Count - Cap);
        }

        return list;
    }

    /// <summary>
    /// Collect the overheard book into the captain's ledger, GROUPED PER CONTACT (#347, owner playtest
    /// 2026-07-18: "we should collect the useful rumors and tips into our ledger per contact… Tips, Intel,
    /// Rumors :-D"). Each source who told you something becomes one <see cref="LedgerRumor"/> carrying
    /// their lines newest-first; the groups themselves come back most-recently-heard first. Pure Core
    /// projection so the client renders the cards rather than doing the grouping — the bug the owner hit
    /// was that this crossing (durable book → ledger) was never wired at all.
    /// </summary>
    public static IReadOnlyList<LedgerRumor> PerContact(IReadOnlyList<OverheardLine>? log)
    {
        if (log is null || log.Count == 0)
        {
            return [];
        }

        // Bucket by source in first-seen order, each bucket newest-first; then order the buckets by their
        // most recent line so the freshest talk sits on top — deterministic, no wall clock.
        var order = new List<string>();
        var buckets = new Dictionary<string, List<OverheardLine>>(System.StringComparer.OrdinalIgnoreCase);
        foreach (OverheardLine line in log)
        {
            string key = string.IsNullOrWhiteSpace(line.Source) ? "The bar" : line.Source;
            if (!buckets.TryGetValue(key, out List<OverheardLine>? lines))
            {
                lines = [];
                buckets[key] = lines;
                order.Add(key);
            }

            lines.Insert(0, line); // newest first within the contact
        }

        return [.. order
            .Select(k => new LedgerRumor(k, buckets[k]))
            .OrderByDescending(r => r.Lines[0].SimTime)];
    }
}

/// <summary>Every tip/rumor one source has handed the captain, collected for the ledger's per-contact
/// "Tips, Intel, Rumors" section (#347). <paramref name="Lines"/> are that source's overheard lines,
/// newest first. A pure projection over the durable <c>OverheardLog</c>.</summary>
public readonly record struct LedgerRumor(string Source, IReadOnlyList<OverheardLine> Lines)
{
    /// <summary>The most recent line's sim-time — when this contact last told you something.</summary>
    public double LatestSimTime => Lines.Count > 0 ? Lines[0].SimTime : double.NegativeInfinity;

    /// <summary>The bar the most recent line was heard in — where you last drank with this source.</summary>
    public string LatestBar => Lines.Count > 0 ? Lines[0].BarName : "";
}
