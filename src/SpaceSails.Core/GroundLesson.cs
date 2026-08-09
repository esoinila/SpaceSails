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

    /// <summary>The line under the stamp — what this place is, in one breath.
    ///
    /// <para>#440 · REWRITTEN. Owner, after a day of building out the ground: <i>"also going to ground is
    /// more than burying chest now"</i>. He is right, and the old line — <i>"the ground keeps what you leave
    /// in it"</i> — was written when a surface was somewhere to bury a chest and walk back. It now has
    /// buildings with doors, shelters that hold air, ruins somebody stripped, and on a rare moon a lid over
    /// a facility. A first-trip card that only teaches caching is teaching a game we stopped shipping.</para>
    ///
    /// <para>What is still true of every square metre of it, and is the one thing worth leading with, is the
    /// AIR. Everything else out here is a choice; the tank is the clock all of them run against.</para></summary>
    public const string Head =
        "Down here you are on foot and on your own air. The ground keeps what you leave in it, "
        + "and it is holding a great deal that somebody else left.";

    /// <summary>One thing your hands can do, and the key that does it.</summary>
    public readonly record struct Lever(string Key, string Title, string What);

    /// <summary>The keys. In the order they matter on a first trip: bury what you brought, buy time, run
    /// lighter when neither worked — and, once there is anything out here worth taking, the pocket you take
    /// it home in.</summary>
    public static IReadOnlyList<Lever> Levers { get; } =
    [
        // #440 · Owner: "the E key does a lot more now." It did mean DIG, back when the ground was regolith
        // and a place to put a chest. It is now the one key that touches anything at all — a door, a
        // console, a shelter's pump, a lift panel, a room worth searching — and a card that still called it
        // "Dig" was teaching a captain to walk past every building on the moon.
        new("E", "Use whatever is in front of you",
            "E is the hands. A door, a console, a shelter's pump, a lift panel, a room worth turning over — "
            + "if it can be worked, E works it. Out on the open regolith it is a shovel instead: carrying a "
            + "chest it buries it WHERE YOU STAND and an ✗ marks the spot, empty-handed it probes the square "
            + "for what somebody else buried, and on your own ✗ it lifts your cache back up."),
        new("T", "Set a sentry",
            "T plants one of the bots riding your sling; T again at a planted one takes it back. They hold "
            + "what they can see and grind an Old One down. They buy you time. They never buy you safety."),
        new("G", "Drop the chest and run",
            "G leaves the chest where it falls and lets you sprint. When they are already on you, that is "
            + "the trade — a dropped chest can be dug up later by whoever gets there first."),

        // #603 · Owner: "should we mention the inventory here also?" Yes — this card is where keys are
        // introduced, and I now does something. Worded for a captain whose pockets are still EMPTY, because
        // on a first landing they are: what matters is knowing there IS somewhere for the first thing you
        // find to go, and that it will still be there next time.
        // #728 · AND IT NO LONGER PROMISES ROUNDS. Owner, in the smoke run, opening the satchel on the spot
        // where a shelter had just told him "70 rounds into your magazines" and reading "Empty".
        //
        // The card said "loose rounds" go in your pockets. Nothing in this game has ever put a round in a
        // pocket: every round a captain finds — the shelter press, the outpost locker, the half-shut drawer
        // in a ruin — goes STRAIGHT into the sentries' magazines, by #580's ruling that ammunition is not
        // inventory and scarcity lives in the walk. So the one word was doing two jobs and telling the truth
        // in neither: it promised a pocketful that never arrives, and it made the abstraction look like a
        // bug at the first locker. The fix is the SENTENCE (#740) — it now says where rounds actually go,
        // which is also the only place the card ever needed to point.
        new("I", "What you are carrying",
            "I opens your pockets. Papers, keys, files on people, a card that opens a door — everything you "
            + "pick up out here goes in, and stays yours between visits. Rounds are the exception: what you "
            + "find loose goes straight into your sentries' magazines, never into the bag. At a door that "
            + "will not open the pocket is also the list of things to TRY on it."),
    ];

    /// <summary>The laws of the place — the four things that kill a first excursion, said plainly.</summary>
    public static IReadOnlyList<string> Laws { get; } =
    [
        "The walk back is half the tank. Turn around before you think you need to.",
        "The Old Ones cannot follow you up the tube — the shuttle door opens to crew only.",
        "Anything still in your hands when they reach you is theirs.",

        // #573 · The shelters have been out there since the air system landed, and this card never mentioned
        // them — which made the one building on the moon that can save your life a thing you had to find out
        // about by nearly dying.
        "There is a shelter on this ground. It holds pressure, it has a door nothing outside can work, and "
        + "it will fill your tank. Find it before you need it.",

        "The landing pad is fused rockcrete. Nothing buries there; carry it out onto the regolith first.",
    ];

    /// <summary>The button that dismisses it — an acknowledgement, not an "OK".</summary>
    public const string Dismiss = "Boots on, then.";

    /// <summary>The line under the button, for the captain who wants it again: the ground keeps saying it,
    /// just quietly, in the instrument column and along the keybar.</summary>
    public const string Foot = "The keys stay spelled out under the motion tracker and along the bottom of the screen.";
}
