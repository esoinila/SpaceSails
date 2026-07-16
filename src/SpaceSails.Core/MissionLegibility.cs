namespace SpaceSails.Core;

// #207 / #208 (Friday-night playtest: "I took the parcel but the mission is quite unclear"). The
// game already KNOWS the job — the quest card, the delivery marker — but at the moment of accepting
// a contract nothing was spoken in-face, and the desk chips kept promising "Make for X" after the
// ship had already docked. This is the pure, unit-tested text layer behind two fixes:
//   * MissionBrief — the #119-style receipt AND the immediate next-action line, per contract kind,
//     so accepting ANY job says its name and its next step where the captain is looking.
//   * DeskChipStatus — the dock-wins truth for the Captain/Nav chips, so a completed navigation
//     never leaves a stale "Make for X · ETA" once the ship is berthed.
// No ship, no ephemeris, no UI — just the already-derived facts the caller hands it (repo §9).

/// <summary>The kinds of table contract a stranger offers (mirrors Map.razor's private QuestKind,
/// kept as its own public enum so this pure text layer stays testable without exposing the page's
/// internals). Intel is instant (turned in on the spot), so it carries no next-action line.</summary>
public enum ContractKind
{
    Hunt,
    CargoRun,
    Intel,
    Fetch,
    Crack,
}

/// <summary>Everything the brief text needs, already resolved to display strings by the caller
/// (giver title-cased, destination/parent named off the ephemeris). Only the fields relevant to
/// <see cref="Kind"/> are populated.</summary>
/// <param name="Kind">The contract kind.</param>
/// <param name="Giver">The giver's display name, e.g. "Madam Coil".</param>
/// <param name="DestName">Where the job is delivered/handed off, e.g. "The Rusty Roadstead".</param>
/// <param name="DestParent">The delivery world, appended for place ("…, Mars"); null to omit.</param>
/// <param name="TargetName">The prey callsign (hunt/intel) or the hatch id (crack), e.g. "V-06".</param>
/// <param name="Pin">The crack job's access code.</param>
/// <param name="Charted">A fetch's wreck is already resolved on the chart (skip the scan step).</param>
/// <param name="PickedUp">The goods are in hand (fetch/crack) — the next step is the hand-off.</param>
public readonly record struct ContractFacts(
    ContractKind Kind,
    string Giver,
    string? DestName = null,
    string? DestParent = null,
    string? TargetName = null,
    string? Pin = null,
    bool Charted = false,
    bool PickedUp = false);

/// <summary>The captain-legible brief for a contract: the receipt filed at acceptance and the
/// immediate next physical action, both as one-voice strings (#119 / #207).</summary>
public static class MissionBrief
{
    /// <summary>The cue the banner/chips prepend to a next-action so it reads as "what the ship does
    /// next", mirroring the NOW:/NEXT: banner idiom.</summary>
    public const string NextPrefix = "NEXT: ";

    /// <summary>The #119-style receipt printed the instant a contract is accepted — it names the job
    /// and its giver and says where it now lives: "parcel for Madam Coil — filed in the Captain's
    /// ledger (0)." One voice with the fetch/intel receipts already on that ledger.</summary>
    public static string Receipt(ContractKind kind, string giver) =>
        $"{JobNoun(kind)} for {NameOr(giver, "the stranger")} — filed in the Captain's ledger (0).";

    /// <summary>The immediate next action as a banner-style line: "NEXT: deliver to The Rusty
    /// Roadstead, Mars". Empty for Intel (instant — nothing to fly).</summary>
    public static string NextLine(ContractFacts f)
    {
        string action = Action(f);
        return action.Length == 0 ? "" : NextPrefix + action;
    }

    /// <summary>The bare next physical step for a live contract, kind-aware (no "NEXT:" prefix). The
    /// STATIC first move per kind; a live cargo run's positional detail (too far / in the envelope)
    /// is overlaid by the caller, which knows ship state.</summary>
    public static string Action(ContractFacts f) => f.Kind switch
    {
        ContractKind.CargoRun => $"deliver to {NameOr(f.DestName, "the drop")}{Place(f.DestParent)}",
        ContractKind.Hunt => $"bring down {NameOr(f.TargetName, "the prey")} — hole her sail or board her",
        ContractKind.Fetch when f.PickedUp => $"hand the wallet to {NameOr(f.Giver, "the Fixer")} at {NameOr(f.DestName, "the drop")}",
        ContractKind.Fetch when !f.Charted => "aim the scope from the Fixer's fix (Comms 🔭) to find the roadster",
        ContractKind.Fetch => "fly to the roadster and prise the wallet loose",
        ContractKind.Crack when f.PickedUp => $"hand the package to {NameOr(f.Giver, "the Fixer")} here",
        ContractKind.Crack => $"key {NameOr(f.Pin, "the code")} into hatch {NameOr(f.TargetName, "the lockup")} here, then hand it to {NameOr(f.Giver, "the Fixer")}",
        // Intel is a gift of information, settled on the spot — no task to fly.
        _ => "",
    };

    /// <summary>The short noun the receipt uses for each kind.</summary>
    private static string JobNoun(ContractKind kind) => kind switch
    {
        ContractKind.CargoRun => "parcel",
        ContractKind.Hunt => "bounty",
        ContractKind.Intel => "tip",
        ContractKind.Fetch => "recovery job",
        ContractKind.Crack => "break-in",
        _ => "job",
    };

    private static string Place(string? parent) =>
        string.IsNullOrWhiteSpace(parent) ? "" : $", {parent}";

    private static string NameOr(string? name, string fallback) =>
        string.IsNullOrWhiteSpace(name) ? fallback : name;
}

/// <summary>The dock-wins truth for the Captain/Nav desk chips (#207). A docked ship's chips must
/// read the same thing the pilot banner's FlightPlanStatus does — "Docked at X" — never a stale
/// "Make for X · ETA" left over from a navigation that has already completed. Pure so the chip-truth
/// selection can be unit-tested.</summary>
public static class DeskChipStatus
{
    /// <summary>The chip's headline line: "Docked at X" while berthed, else whatever the ship is
    /// doing underway (the mission summary or the Nav objective).</summary>
    public static string PrimaryLine(bool docked, string? havenName, string underwayLine) =>
        docked ? $"Docked at {NameOr(havenName, "the haven")}" : underwayLine;

    /// <summary>The chip's ETA line, suppressed while docked (a berthed ship has no arrival to
    /// count down — the very stale promise the owner caught).</summary>
    public static string? EtaLine(bool docked, string? underwayEta) =>
        docked ? null : underwayEta;

    private static string NameOr(string? name, string fallback) =>
        string.IsNullOrWhiteSpace(name) ? fallback : name;
}
