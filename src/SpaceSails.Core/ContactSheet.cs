namespace SpaceSails.Core;

/// <summary>How a contact will do banking business with us (ruling 6, FridaySecondPlan §5). The
/// asteroid hermit deals in person only — you fly to their rock; a dark-web-native fixer wires coin
/// anywhere. This is the field the owner made canon: it exists now for the whole known cast, and a
/// future hermit archetype simply reads <see cref="InPersonOnly"/>.</summary>
public enum BankingChannel
{
    /// <summary>Face to face, at their table, no electronic trace — the asteroid hermit's way.</summary>
    InPersonOnly,

    /// <summary>Anonymized dark-web wire — deposit, withdraw or borrow from anywhere their desk reaches.</summary>
    DarkWebWire,
}

/// <summary>How far a relationship has come, derived purely from jobs finished together (ruling 6).
/// Trust unlocks the favor bank's riskier moves — only a <see cref="Trusted"/> contact will wire you
/// gas money on a promise.</summary>
public enum TrustTier
{
    /// <summary>No shared history — they'll take your deposit but won't stake you.</summary>
    Stranger,

    /// <summary>A job or two done — a nod, a fair rate.</summary>
    Acquaintance,

    /// <summary>Enough history that they'll wire you money against a favor owed.</summary>
    Trusted,

    /// <summary>Inner circle — the best rates, the standing line of credit.</summary>
    Confidant,
}

/// <summary>
/// A contact's character sheet (ruling 6, "withdrawal is in the contact's character"): the small,
/// per-contact record that decides HOW they bank. Data-driven for the known bar cast in
/// <see cref="ContactSheets"/>; <see cref="TrustTier"/> is not stored here but derived live from the
/// <see cref="ContactHistory.MissionsCompleted"/> on the ledger, so trust always tracks the real
/// relationship. <see cref="VoiceStyle"/> flavors the lines they speak (their bank patter, the favor
/// they call in). Pure data — the razor and the rules read it, nobody mutates it.
/// </summary>
public readonly record struct ContactSheet(
    string ContactId,
    string DisplayName,
    BankingChannel Channel,
    string VoiceStyle)
{
    /// <summary>Can this contact move money for us over the wire (dark-web desk, anywhere), or only
    /// across their own table?</summary>
    public bool CanWire => Channel == BankingChannel.DarkWebWire;
}

/// <summary>The known cast's character sheets, plus the trust-tier derivation — the data-driven core
/// of ruling 6. Keyed by the same shout-name the bar consoles and the <see cref="ContactLedger"/> use
/// ("MADAM COIL", "THE FIXER", …), matched case-insensitively by keyword the way
/// <see cref="Celebrations.GiverThanks"/> already reads givers.</summary>
public static class ContactSheets
{
    // Trust ladder (ruling 6): trust is a number you earn by finishing jobs. The Trusted rung — where
    // a contact will WIRE you gas money on a favor — sits at three shared jobs, the same "3rd job
    // together" the celebration already celebrates.
    public const int AcquaintanceAtMissions = 1;
    public const int TrustedAtMissions = 3;
    public const int ConfidantAtMissions = 6;

    /// <summary>The trust tier a mission count buys — the live derivation (nothing stored).</summary>
    public static TrustTier TrustFor(int missionsCompleted) => missionsCompleted switch
    {
        >= ConfidantAtMissions => TrustTier.Confidant,
        >= TrustedAtMissions => TrustTier.Trusted,
        >= AcquaintanceAtMissions => TrustTier.Acquaintance,
        _ => TrustTier.Stranger,
    };

    /// <summary>True once a contact is Trusted or better — the gate on borrowing (they'll stake you).</summary>
    public static bool WillStake(int missionsCompleted) => TrustFor(missionsCompleted) >= TrustTier.Trusted;

    /// <summary>The character sheet for a contact by their shout-name. The known cast is authored; an
    /// unknown name gets a cautious default — in-person only, a plain voice (the asteroid-hermit
    /// archetype's own settings, standing in until a specific hermit is authored).</summary>
    public static ContactSheet For(string contactId)
    {
        string g = (contactId ?? string.Empty).ToUpperInvariant();

        // Madam Coil — runs quiet parcels through the dark web; wires anywhere.
        if (g.Contains("COIL"))
        {
            return new(contactId!, "Madam Coil", BankingChannel.DarkWebWire, "warm-underworld");
        }

        // The Fixer — off-the-books, confidential; the wire is their native tongue.
        if (g.Contains("FIXER"))
        {
            return new(contactId!, "The Fixer", BankingChannel.DarkWebWire, "clipped-discreet");
        }

        // Gilt-Eye — the intel dealer; deals over the wire.
        if (g.Contains("GILT"))
        {
            return new(contactId!, "Gilt-Eye", BankingChannel.DarkWebWire, "appraising");
        }

        // The Magpie — a fence's runner who never sits still and keeps no desk: in person only, if you
        // can catch them (their rota already makes them hard to find — HavenInterior.MagpieRota).
        if (g.Contains("MAGPIE"))
        {
            return new(contactId!, "The Magpie", BankingChannel.InPersonOnly, "flighty");
        }

        // One-Eye Silas — the bounty fence at the bar; hand to hand, in person.
        if (g.Contains("SILAS"))
        {
            return new(contactId!, "One-Eye Silas", BankingChannel.InPersonOnly, "gruff");
        }

        // The default — and the asteroid hermit's settings until a hermit is authored: in person only.
        return new(contactId ?? string.Empty, contactId ?? string.Empty, BankingChannel.InPersonOnly, "plain");
    }
}
