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
