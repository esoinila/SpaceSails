namespace SpaceSails.Core;

/// <summary>
/// The relationship seam (#185, owner: "we have a relationship now to the task giver"). A small,
/// SAVED record of the player's history with one named contact — the lady at the Ringside bar, a
/// fixer, a fence. Today it counts the jobs we've finished together, the coin they've paid, and
/// when we last delivered; the future relationship system reads it to know a history exists at
/// all. Deliberately one record type, not a system.
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

    /// <summary>True once there is any history to read — an honest job done, or a hull we robbed.</summary>
    public bool HasHistory => MissionsCompleted > 0 || Hostile;
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
}
