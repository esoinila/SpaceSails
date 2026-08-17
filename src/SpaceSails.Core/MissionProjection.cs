namespace SpaceSails.Core;

// #727 · THE MISSION THAT LEAVES THE SHIP. Owner, 2026-08-06: "Right now the tasks and missions have been
// ship-UI specific, so a mission that works outside the ship UI is something new… we should filter out /
// minimize ship-specific stuff to appropriate ('cannot do in this UI level, but high level: go to Moon X')
// type info in the carried mission UI."
//
// THE LAW, WRITTEN FIRST: **one mission model, two projections — never two mission lists.** The captain's
// chair keeps rendering everything it renders today; the satchel's MISSIONS pane renders the SAME missions
// through this filter. If the two views ever read from different stores they will disagree about the same
// journey, which is this repo's one-source-of-truth disease in its most player-visible organ.
//
// Pure Core (repo §9): no ship, no ephemeris, no UI. The caller (Map.Quests.Compass.cs) resolves ids to
// names and hands the already-derived facts over, exactly as ContractFacts/MissionBrief does next door.

/// <summary>The UI LEVEL a mission step is actionable at — the whole of the filter, and it is read off the
/// step's own kind, never off its prose.</summary>
public enum MissionUiLevel
{
    /// <summary>Burn, coast, dock, undock, clamp, aim the scope. The captain's chair's own verbs. Nothing
    /// at this level can be honoured by somebody standing in a basement twenty floors down.</summary>
    Ship,

    /// <summary>Buy, sign, sell, collect — a counter you reach by sailing to it. Not a chair verb, but not
    /// something you can do where you stand either, so the carried view collapses it the same way.</summary>
    Berth,

    /// <summary>Find, press E, key a code, dig, carry, hand over. The steps you can act on where you are
    /// standing — the only ones the carried view spells out.</summary>
    Foot,
}

/// <summary>One live mission's CURRENT step, resolved to display strings by the caller.</summary>
/// <param name="Text">The chair's own words for this step — passed through VERBATIM. The carried view is a
/// projection, not a rewrite: a second wording is a second mission list wearing a disguise.</param>
/// <param name="Level">Which UI can honour it.</param>
/// <param name="Destination">Where the ship must go for this step, as a display name. Used by the collapse
/// and by nothing else.</param>
/// <param name="PlaceId">The body id this step is actionable at, or null for "wherever you are". A foot-level
/// step whose ground you are not standing on is still a journey, so it collapses like any other.</param>
/// <param name="Action">At most one foot-level action id the pane may offer. Null everywhere else — the pane
/// must never render a control it cannot honour.</param>
public readonly record struct MissionStep(
    string Text,
    MissionUiLevel Level,
    string Destination,
    string? PlaceId = null,
    string? Action = null);

/// <summary>One live mission as the captain's chair holds it: its own name, its own why, and the step it is
/// currently on.</summary>
public readonly record struct CarriedMission(string Title, string Why, MissionStep Step);

/// <summary>One line of the carried compass. <paramref name="Step"/> is either the chair's own foot-level
/// step text, byte for byte, or the collapsed <see cref="MissionProjection.ReturnToShip"/> form — never a
/// third thing. <paramref name="Action"/> is non-null only when <paramref name="Actionable"/> is true.</summary>
public readonly record struct CompassLine(
    string Title,
    string Why,
    string Step,
    bool Actionable,
    string? Action);

/// <summary>#727 · The second projection of the one mission model — the compass a captain carries off the
/// ship. One line per live mission ("why am I here"), the current foot-level step spelled out where you
/// stand, every other run collapsed to its destination.</summary>
public static class MissionProjection
{
    /// <summary>The collapse. Owner's own example: "cannot do in this UI level, but high level: go to Moon
    /// X" — not "prograde 12 m/s, coast 40 h, clamp at Selene Gate" but the one sentence that is true from a
    /// corridor. The captain's chair is where it expands back into burns.</summary>
    public const string ReturnToShip = "⛵ return to the ship — next: ";

    /// <summary>What the collapse names when the chair gave the step no destination of its own. Rare — every
    /// authored contract names somewhere — but a blank after "next:" would be the pane lying by omission.</summary>
    public const string NoDestinationNamed = "orders from the chair";

    /// <summary>The pane's empty state. It says the honest thing rather than "no missions": work you have
    /// taken on and cannot act on down here has not stopped existing.</summary>
    public const string NothingOwedOnFoot =
        "Nothing owed on foot. What you took on is the ship's business, and it waits at the chair.";

    /// <summary>The blurb under the tab — what this page is, and what it deliberately is not.</summary>
    public const string CompassBlurb =
        "Why you are down here. The full ladder — every burn, every clamp — stays at the captain's chair.";

    /// <summary>Whether a step can be honoured by somebody standing on <paramref name="standingOn"/>. Foot
    /// level AND the right ground: a dig on Phobos is a foot step, and from a Luna basement it is a voyage.</summary>
    public static bool ActionableWhereYouStand(MissionStep step, string? standingOn) =>
        step.Level == MissionUiLevel.Foot
        && (step.PlaceId is null
            || (standingOn is not null && string.Equals(step.PlaceId, standingOn, StringComparison.Ordinal)));

    /// <summary>The collapsed form for a step this UI level cannot honour.</summary>
    public static string Collapse(MissionStep step) =>
        ReturnToShip + (string.IsNullOrWhiteSpace(step.Destination) ? NoDestinationNamed : step.Destination);

    /// <summary>The carried view. ONE line per mission handed in — nothing is filtered out, because a mission
    /// the pane silently dropped is the two-lists bug with better manners. What varies is only whether the
    /// step is spelled out or collapsed, and whether an action rides with it.</summary>
    public static IReadOnlyList<CompassLine> OnFoot(IEnumerable<CarriedMission> missions, string? standingOn)
    {
        ArgumentNullException.ThrowIfNull(missions);
        var lines = new List<CompassLine>();
        foreach (CarriedMission m in missions)
        {
            bool here = ActionableWhereYouStand(m.Step, standingOn);
            lines.Add(new CompassLine(
                m.Title,
                m.Why,
                here ? m.Step.Text : Collapse(m.Step),
                here,
                here ? m.Step.Action : null));
        }

        return lines;
    }
}
