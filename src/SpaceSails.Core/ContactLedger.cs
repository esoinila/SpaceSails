using System.Collections.Immutable;

namespace SpaceSails.Core;

/// <summary>What a single credit movement on the favor bank was (PR-WIRE, FridaySecondPlan §0). The
/// amount is the SIGNED delta it applied to <see cref="ContactHistory.CreditBalance"/> — so the sum
/// of every transaction's amount is exactly the running balance, a book that always foots.</summary>
public enum CreditKind
{
    /// <summary>We parked coin with the contact (balance ↑ — they hold our money).</summary>
    Deposit,

    /// <summary>The distress cut a fence takes on the way in while we're HEATED (balance ↓, but less
    /// than the collector would take — see <see cref="FavorBank"/>).</summary>
    FenceCut,

    /// <summary>Calm-weather interest the contact pays on parked coin (balance ↑).</summary>
    Interest,

    /// <summary>We drew our parked coin back out (balance ↓ toward zero).</summary>
    Withdrawal,

    /// <summary>The contact wired us gas money — we now owe them (balance ↓ below zero).</summary>
    Borrow,

    /// <summary>We paid a debt down, in coin or by working off a favor (balance ↑ toward zero).</summary>
    Repayment,
}

/// <summary>One line in a contact's passbook: what kind of move, the signed credit delta, when, and a
/// human note. A <c>readonly record struct</c> so a future save layer serializes it flat.</summary>
public readonly record struct CreditTransaction(CreditKind Kind, long Amount, double SimTime, string Note);

/// <summary>
/// The relationship seam (#185, owner: "we have a relationship now to the task giver"). A small,
/// SAVED record of the player's history with one named contact — the lady at the Ringside bar, a
/// fixer, a fence. Today it counts the jobs we've finished together, the coin they've paid, and
/// when we last delivered; the future relationship system reads it to know a history exists at
/// all. Deliberately one record type, not a system.
///
/// <para>PR-WIRE (FridaySecondPlan §0, the favor bank) grows two additive fields on the SAME book:
/// a signed <see cref="CreditBalance"/> (+ = they hold OUR coin; − = we owe THEM) and the
/// <see cref="Transactions"/> that produced it. Both default to empty/zero, so every pre-existing
/// construction (and a <c>default</c> struct) still reads as a clean-slate contact with no money in
/// the air — the field exists now, canon, without disturbing the celebration or plunder flows.</para>
/// </summary>
/// <param name="Hostile">#202 — the NEGATIVE seam: we boarded and robbed this contact. One field, not
/// a system — a future reputation layer reads it to know the history is a bad one (a fence won't buy,
/// a fixer won't call), the mirror of the warm welcome an honest job earns.</param>
public readonly record struct ContactHistory(
    string ContactId,
    string DisplayName,
    int MissionsCompleted,
    int TotalPaidCredits,
    double LastCompletedSimTime,
    bool Hostile = false)
{
    /// <summary>The signed running balance with this contact: positive when they hold our banked coin
    /// (a deposit we can draw back), negative when we owe them (gas money they wired). The sum of every
    /// <see cref="Transactions"/> entry's <see cref="CreditTransaction.Amount"/>.</summary>
    public long CreditBalance { get; init; }

    /// <summary>Warmth toward this contact that wasn't bought with a job — the cheap way to thaw a cold
    /// one (#247 kin #224): standing a round at the bar while they're drinking nudges it up. Additive and
    /// SAVED like the rest of the book; a future relationship layer reads it beside
    /// <see cref="MissionsCompleted"/> to know a contact has been made welcome, not just useful.</summary>
    public int Goodwill { get; init; }

    // Stored as ImmutableArray but read through a default-safe accessor: a ContactHistory built the old
    // way (or a `default` struct) never ran this initializer, leaving the field `IsDefault` — so the
    // getter coalesces to Empty rather than throwing. Round-trips through `with { Transactions = ... }`.
    private readonly ImmutableArray<CreditTransaction> _transactions;

    /// <summary>The passbook: every credit movement with this contact, oldest first.</summary>
    public ImmutableArray<CreditTransaction> Transactions
    {
        get => _transactions.IsDefault ? ImmutableArray<CreditTransaction>.Empty : _transactions;
        init => _transactions = value;
    }

    /// <summary>A blank slate for a contact we've not yet done business with.</summary>
    public static ContactHistory New(string contactId, string displayName) =>
        new(contactId, displayName, 0, 0, double.NegativeInfinity);

    /// <summary>Book one more finished job for this contact: a mission, its pay, stamped now.</summary>
    public ContactHistory WithCompletedMission(int paidCredits, double simTime) => this with
    {
        MissionsCompleted = MissionsCompleted + 1,
        TotalPaidCredits = TotalPaidCredits + Math.Max(0, paidCredits),
        LastCompletedSimTime = simTime,
    };

    /// <summary>Mark this contact hostile — we boarded and robbed them (#202) — and stamp when. The
    /// negative twin of <see cref="WithCompletedMission"/>; it books no coin, only the grudge.</summary>
    public ContactHistory WithPlunder(double simTime) => this with
    {
        Hostile = true,
        LastCompletedSimTime = simTime,
    };

    /// <summary>Post one credit movement: append it to the passbook and move the balance by its signed
    /// amount. The invariant <c>CreditBalance == Σ Transactions.Amount</c> holds for any history built
    /// from <see cref="New"/> and only ever mutated through here.</summary>
    public ContactHistory WithCredit(CreditTransaction txn) => this with
    {
        CreditBalance = CreditBalance + txn.Amount,
        Transactions = Transactions.Add(txn),
    };

    /// <summary>Warm this contact by <paramref name="delta"/> goodwill (a round bought at the bar). No
    /// coin moves — this is the non-transactional twin of <see cref="WithCredit"/>.</summary>
    public ContactHistory WithGoodwill(int delta) => this with { Goodwill = Goodwill + delta };

    /// <summary>True once there is any history to read — an honest job done, a hull we robbed, coin
    /// in the air (banked with them or owed to them), or a round stood them (goodwill).</summary>
    public bool HasHistory => MissionsCompleted > 0 || Hostile || CreditBalance != 0 || Transactions.Length > 0 || Goodwill != 0;
}

/// <summary>
/// Every contact the player has dealt with, keyed by a stable contact id (the giver's name today).
/// A plain mutable holder so it drops straight into the game state and a future save layer just
/// serializes <see cref="Entries"/>. <see cref="RecordCompletion"/> is the single mutation the
/// celebration flow performs.
/// </summary>
public sealed class ContactLedger
{
    private readonly Dictionary<string, ContactHistory> _byId = [];

    /// <summary>The saved cast of contacts — what a future persistence layer serializes.</summary>
    public IReadOnlyDictionary<string, ContactHistory> Entries => _byId;

    /// <summary>This contact's history, or a blank slate if we've never dealt with them.</summary>
    public ContactHistory For(string contactId) =>
        _byId.TryGetValue(contactId, out ContactHistory h) ? h : ContactHistory.New(contactId, contactId);

    /// <summary>Book a finished job against a contact, creating their record on first dealing.
    /// Returns the updated history so the caller can narrate "your 3rd job together."</summary>
    public ContactHistory RecordCompletion(string contactId, string displayName, int paidCredits, double simTime)
    {
        ContactHistory current = _byId.TryGetValue(contactId, out ContactHistory existing)
            ? existing with { DisplayName = displayName }
            : ContactHistory.New(contactId, displayName);
        ContactHistory updated = current.WithCompletedMission(paidCredits, simTime);
        _byId[contactId] = updated;
        return updated;
    }

    /// <summary>#202 — book a boarding against a victim: create their record on first crossing and mark
    /// it hostile. The negative twin of <see cref="RecordCompletion"/>; a future reputation system reads
    /// <see cref="ContactHistory.Hostile"/> to know we wronged them. Returns the marked history.</summary>
    public ContactHistory RecordPlunder(string contactId, string displayName, double simTime)
    {
        ContactHistory current = _byId.TryGetValue(contactId, out ContactHistory existing)
            ? existing with { DisplayName = displayName }
            : ContactHistory.New(contactId, displayName);
        ContactHistory updated = current.WithPlunder(simTime);
        _byId[contactId] = updated;
        return updated;
    }

    /// <summary>PR-WIRE — the ONE mutation the favor bank performs: post a signed credit movement
    /// (deposit, fence cut, interest, withdrawal, borrow, repayment) against a contact, creating their
    /// record on first dealing. Additive by construction — the celebration and plunder flows are
    /// untouched, and both sibling lanes (BUSTED confiscation, HOARD caches) can read
    /// <see cref="ContactHistory.CreditBalance"/> without a new API. Returns the updated history.</summary>
    public ContactHistory ApplyCredit(string contactId, string displayName, CreditTransaction txn)
    {
        ContactHistory current = _byId.TryGetValue(contactId, out ContactHistory existing)
            ? existing with { DisplayName = displayName }
            : ContactHistory.New(contactId, displayName);
        ContactHistory updated = current.WithCredit(txn);
        _byId[contactId] = updated;
        return updated;
    }

    /// <summary>#247 — warm a contact by standing them a round at the bar (kin #224): add goodwill,
    /// creating their record on first dealing. Books no coin — the non-transactional twin of
    /// <see cref="ApplyCredit"/>. Returns the updated history so the caller can narrate the warming.</summary>
    public ContactHistory AddGoodwill(string contactId, string displayName, int delta)
    {
        ContactHistory current = _byId.TryGetValue(contactId, out ContactHistory existing)
            ? existing with { DisplayName = displayName }
            : ContactHistory.New(contactId, displayName);
        ContactHistory updated = current.WithGoodwill(delta);
        _byId[contactId] = updated;
        return updated;
    }

    /// <summary>Rehydrate one contact's whole history verbatim (the personal-vault load path, #225).
    /// Unlike the recording mutators this does not derive anything — it stores exactly what was saved,
    /// keyed by <see cref="ContactHistory.ContactId"/>, so a round-trip through the vault is lossless.</summary>
    public void Load(ContactHistory history) => _byId[history.ContactId] = history;
}
