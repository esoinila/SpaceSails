namespace SpaceSails.Core;

/// <summary>
/// #160 · THE MILK RUN — the tutorial that flies the whole loop, and the eight lines it says.
///
/// <para>Owner, 2026-07-16, during the moon-run playtest: <i>"I think we should have a tutorial for mission
/// also? Some easy milk run with autopilot from moon to moon, maybe?"</i> The three lessons that existed
/// before this one teach a hunt, a gun and a haven — every one of them a thing that happens when the work
/// goes wrong. Nothing taught the work.</para>
///
/// <para><b>Why the words live in Core.</b> They are canon, authored once on the issue (Fable, 2026-09-05)
/// and implemented verbatim; nothing else about this lesson is authored. Holding them here rather than in
/// the page means the guard that pins them can read the same array the checklist renders and the pulse
/// speaks, so "the eight are the only new strings" is a sentence a test can actually check, and a ninth
/// line cannot be slipped in beside them without that test seeing it.</para>
///
/// <para><b>One line per step, said as that step becomes the one to do.</b> <see cref="Lines"/>[k-1] is
/// step k's line: it is both the checklist row (the page splices this array onto the end of its
/// <c>TutorialSteps</c>) and the thing the game says on the pulse the moment step k becomes current. Step
/// k's own gate — the real state that finishes it — is on the page, in <c>Map.Quests.MilkRun.cs</c>, one
/// row per line.</para>
///
/// <para><b>The order is load-bearing and is not a schedule.</b> #1091's ruling ("the tutorial selection
/// should trigger the launch of the target vehicles") applies here as it does to the hunts: choosing this
/// lesson is what puts the contract on the board, priced from the berth the captain is actually standing
/// in, so there is no window to miss and no job that was posted at a body half an AU away.</para>
/// </summary>
public static class MilkRunLesson
{
    /// <summary>The eight lines, in step order — canon, verbatim, and the whole of what this lesson says.
    /// Index k-1 is step k. Nothing else in the lane is authored prose.</summary>
    public static readonly string[] Lines =
    [
        // 1 · Take the contract.
        "A milk run. Drums from Enceladus to Titan; nobody shoots at drums. Take it from the board.",
        // 2 · Plan dock to dock.
        "Plan the whole trip, dock to dock. The plan is a list of steps; the autopilot flies the list.",
        // 3 · Top off (#157).
        "Top her off before you leave. The autopilot quotes fuel honestly, and it cannot quote what you did not load.",
        // 4 · Arm, and read the rehearsal.
        "Arm it. The rehearsal flies the plan on paper first and tells you what it will cost. Believe the number.",
        // 5 · The departure burn fires itself (#159).
        "The departure burn fires itself at its epoch. Watch the banner: NOW is what she is doing, NEXT is what she will.",
        // 6 · Warp.
        "Warp is your clock, not hers. The plan does not care how fast you watch it.",
        // 7 · Arrive and dock (#955 — the arrival is an autopilot THEN).
        "Arrival is autopilot then, not now — it was armed at plan time. Dock, and the contract pays at the counter.",
        // 8 · Paid.
        "That is the loop. Everything else in this game is what happens when a milk run goes wrong.",
    ];

    /// <summary>How many steps the lesson has. One per line, by construction.</summary>
    public static int StepCount => Lines.Length;

    /// <summary>The id the lesson's own contract carries, so the watcher can find it among the captain's
    /// other work — and, because quests are vaulted by id, still find it after a reload.</summary>
    public const string QuestId = "milk-run";

    /// <summary>Who posts it. Not a face: the same house giver a board-posted haul already carries
    /// (<c>Map.Cycler</c>'s standing consignment), which is the giver line 1's "take it from the board"
    /// is naming. No new name is invented for a notice on a wall.</summary>
    public const string BoardGiver = "THE BOARD";

    /// <summary>The lesson card's title on the Captain's Tutorials tab — the first sentence of line 1, with
    /// its full stop trimmed. DERIVED rather than authored: the picker needs a name and the canon pass
    /// wrote eight lines, so the name is taken out of the line the player will hear anyway rather than
    /// written fresh beside it.</summary>
    public static string Title => FirstSentenceOf(Lines[0]).TrimEnd('.');

    /// <summary>The card's blurb — the REST of line 1, for the same reason and out of the same string.</summary>
    public static string Blurb => Lines[0][FirstSentenceOf(Lines[0]).Length..].TrimStart();

    /// <summary>Everything up to and including the first full stop, or the whole line if it has none.</summary>
    private static string FirstSentenceOf(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        int stop = line.IndexOf('.', StringComparison.Ordinal);
        return stop < 0 ? line : line[..(stop + 1)];
    }
}
