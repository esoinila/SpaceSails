namespace SpaceSails.Core;

/// <summary>
/// #440 · THE FIRST GROUND — the one lesson a captain gets before their boots touch regolith.
///
/// <para>Owner, 2026-07-26: <i>"that feature and button is not clearly told to a new player… Maybe a pop-up
/// reminder on first planet-side visit."</i> Then: <i>"Definitely we need a landing site tutorial also for
/// new captains."</i> The trigger was the shovel — the owner, who specified burying, could not recall which
/// key does it (it is <c>E</c>). If the author forgets, a new captain never knew.</para>
///
/// <para>The surface is the only place in the game that can take everything from you in ninety seconds, and
/// until now it explained itself in 10px of dimmed text in the corner. This is the card that goes up the
/// FIRST time a captain rides the shuttle down, once per captain, never again — per #292's ruling that a
/// lesson may only greet the truly new.</para>
///
/// <para>It teaches three keys and four laws, and nothing else. Not a manual: the shortest thing that stops
/// a first excursion from being a mystery. Pure data so the copy is pinned by tests and the client only
/// renders it.</para>
/// </summary>
public static class GroundLesson
{
    /// <summary>The stamp across the top of the card.</summary>
    public const string Stamp = "⛏ FIRST TIME ON THE GROUND";

    /// <summary>The line under the stamp — what this place is, in one breath.</summary>
    public const string Head = "Down here you are on foot, and the ground keeps what you leave in it.";

    /// <summary>One thing your hands can do, and the key that does it.</summary>
    public readonly record struct Lever(string Key, string Title, string What);

    /// <summary>The three keys. In the order they matter on a first trip: bury what you brought, buy time,
    /// and — when neither worked — run lighter.</summary>
    public static IReadOnlyList<Lever> Levers { get; } =
    [
        new("E", "Dig",
            "Carrying a chest, E puts it in the ground WHERE YOU STAND — anywhere out on the open regolith, "
            + "and an ✗ marks the spot. Empty-handed, E probes the square for what somebody else buried. "
            + "Standing on your own ✗, E lifts your cache back up."),
        new("T", "Set a sentry",
            "T plants one of the bots riding your sling; T again at a planted one takes it back. They hold "
            + "what they can see and grind an Old One down. They buy you time. They never buy you safety."),
        new("G", "Drop the chest and run",
            "G leaves the chest where it falls and lets you sprint. When they are already on you, that is "
            + "the trade — a dropped chest can be dug up later by whoever gets there first."),
    ];

    /// <summary>The laws of the place — the four things that kill a first excursion, said plainly.</summary>
    public static IReadOnlyList<string> Laws { get; } =
    [
        "The walk back is half the tank. Turn around before you think you need to.",
        "The Old Ones cannot follow you up the tube — the shuttle door opens to crew only.",
        "Anything still in your hands when they reach you is theirs.",
        "The landing pad is fused rockcrete. Nothing buries there; carry it out onto the regolith first.",
    ];

    /// <summary>The button that dismisses it — an acknowledgement, not an "OK".</summary>
    public const string Dismiss = "Boots on, then.";

    /// <summary>The line under the button, for the captain who wants it again: the ground keeps saying it,
    /// just quietly, in the instrument column and along the keybar.</summary>
    public const string Foot = "The keys stay spelled out under the motion tracker and along the bottom of the screen.";
}
