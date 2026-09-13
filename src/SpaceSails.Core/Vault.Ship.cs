namespace SpaceSails.Core;

// ─── VAULT SECTIONS: THE SHIP AND WHAT IT CARRIES (#225) ───
//
// The purse, the fit, the hold, the heat on it, the insurance tier, the upgrades bought and the dice
// items rattling in a pocket. Everything here is a fact about the vessel and the coin, and none of it is
// a fact about a place or a person.
// 
// Split out of Vault.cs under #251. These are TOP-LEVEL RECORD TYPES, not partials, so no declaration
// moved inside a type and no property changed position: VaultSerializer writes each section with
// JsonSerializer.SerializeToNode, whose field order is the order the properties are declared WITHIN the
// record. Moving whole types between files cannot reach that order, and the section order in the
// envelope is spelled out by hand in VaultSerializer.Save. The save fingerprint is untouched.

/// <summary>Carried coin (the purse). Banked coin lives on the contacts ledger, not here.</summary>
public sealed record PurseSection(long Credits);

/// <summary>The ship's durable fit that survives a berth-to-berth save: reaction mass in the tank
/// and the magazines. Position/velocity are NOT here — the resume section docks the ship fresh.</summary>
public sealed record ShipSection
{
    /// <summary>Reaction mass remaining, in pulses (the game's fuel unit). Double so a partial pulse
    /// survives; the mercy law keeps a restart from ever stranding you below a pump's reach.</summary>
    public double ReactionMassPulses { get; init; }
    public int SlugAmmo { get; init; }
    public int MissileAmmo { get; init; }

    /// <summary>#314 — the sentry roster's magazines, one entry per ship bot (K-77, R-3B), each 0..99
    /// rounds. Durable ship state like the other ammo above; a missing section (old file) defaults to a
    /// full-loaded roster on the client. Order matches <see cref="SentryBot.RosterUnits"/>.</summary>
    public IReadOnlyList<int> SentryMagazines { get; init; } = [];

}

/// <summary>The hold. Each line is a cargo class and its unit count; the hot (stolen-while-heated)
/// flags ride separately in <see cref="Vault.Cargo"/>'s hot list so laundering is independent.</summary>
public sealed record CargoSection(IReadOnlyList<CargoLine> Hold, IReadOnlyList<HotCargoLine> Hot)
{
    public CargoSection() : this([], []) { }
}

public sealed record CargoLine(string CargoClass, int Units);

/// <summary>A class of stolen-while-heated cargo (the <see cref="HotCargoLedger"/> stamp). When heat
/// fully cools the class launders; until then it is evidence the collectors can take.</summary>
public sealed record HotCargoLine(string CargoClass, int HotUnits);

/// <summary>The heat (wanted level) — a faithful mirror of <see cref="HeatState"/>. MUST be saved:
/// otherwise a server restart would cleanse heat, turning the vault into an exploit.
/// <see cref="RaisedAtSimTime"/> is the decay checkpoint and may be <c>double.NegativeInfinity</c>
/// (the "None" sentinel), so the serializer enables named floating-point literals.</summary>
public sealed record HeatSection(int Level, double RaisedAtSimTime);

// ── Relationships: the whole social network, mirrored from the ContactLedger. ──

/// <summary>The pirate-insurance policy (mirror of <see cref="PirateInsurance"/>). Tier stored as the
/// int value of <see cref="InsuranceTier"/> for forward-tolerance.</summary>
public sealed record InsuranceSection(int Tier, double PremiumPaidThroughSimTime);

/// <summary>The bought-and-kept ship upgrade levels. Separate from <see cref="ShipSection"/> because
/// they are permanent purchases, not consumables — and the owner's spec lists them as their own
/// section.</summary>
public sealed record UpgradesSection
{
    public int MassLevel { get; init; }
    public int SensorLevel { get; init; }
    public int HoldLevel { get; init; }
    public int TelescopeLevel { get; init; }
}

/// <summary>Persistent TTRPG dice items — the purchasable style modifiers ("boarding-nets jammer",
/// etc.) that tilt a roll. Each is a <see cref="DiceModifier"/> (label + value) plus a stable item
/// id so duplicates and stacking survive.</summary>
public sealed record DiceItemsSection(IReadOnlyList<DiceItemRecord> Items)
{
    public DiceItemsSection() : this([]) { }
}

public sealed record DiceItemRecord(string ItemId, string Label, int Value);

// ── Player progression flags (#292): onboarding state that gates the nav-screen greeting. ──

// ── #325/#332 · THE CHANDLERY'S STORES ────────────────────────────────────────────────────────────────

/// <summary>
/// #325/#332 · What the haven chandlery sold this ship and she has not used yet: spare suit bottles in
/// stores, and the med bay's pill cabinet as it actually stands.
///
/// <para><b>Why a whole SECTION rather than two fields on <see cref="ShipSection"/>.</b> Because the
/// checksum is taken over the payload. Two keys added to a section every save already writes would have
/// changed the digest of every vault ever written, and the #638 note on <see cref="Vault.Void"/> spells out
/// what that costs: the 📛 tampered marker hung on an honest voyage. The save-compat proof is a
/// byte-for-byte one (<c>ALegacyVaultRoundTripsByteForByteAcrossTheVoid</c>), and it caught exactly this —
/// the first cut of this lane DID put the fields on <see cref="ShipSection"/>, and the guard reddened at the
/// character where <c>"extendedTanks"</c> appeared. A section the writer omits when there is nothing to say
/// leaves an old file untouched.</para>
///
/// <para>So it is written only when the client has something to record, and a vault that lacks it loads as
/// the opening stake: no bottles in stores, a full cabinet. Which is what every captain has had until
/// now.</para>
/// </summary>
public sealed record ChandlerySection
{
    /// <summary>#325 — extended suit tanks bought and not yet fitted, as a COUNT. They ride the ship and not
    /// the satchel (owner's placement: a spare bottle is stores, not something you carry out with you), they
    /// stack, and one is consumed at the start of the next excursion.</summary>
    public int ExtendedTanks { get; init; }

    /// <summary>#332 — calming pills in the med bay's cabinet, 0..<see cref="Chandlery.MedKitFullStock"/>.
    /// Durable: a cabinet does not refill itself over a reload, which was the whole reason the restock had
    /// to exist.
    ///
    /// <para>Nullable, and that matters. A file that carries this section but not this key has nothing to
    /// say about pills and loads as a full cabinet; a recorded ZERO is an empty one and stays empty. A plain
    /// int could not tell those apart, and whichever way it guessed would be wrong for somebody — either
    /// every returning captain opens to an empty cabinet, or reloading is a free restock.</para></summary>
    public int? MedKitPills { get; init; }
}
