namespace SpaceSails.Core;

/// <summary>
/// #563 · THE GROUND GREW — the card that goes up the first time a captain forces something open and the
/// map itself gets bigger.
///
/// <para>Owner, 2026-07-31: <i>"The expanding site, how we tell that story to user clearly (do we have
/// pop-up with image) is of great interest."</i> The answer, before this, was no. The most distinctive
/// thing this game does — ground appearing under your feet, live, with nobody teleported anywhere — was
/// announced by a one-line toast that faded in a few seconds. Meanwhile a scuttled ship gets a full card
/// with art, the first landing gets a full card, and the arc convergence gets a full card.</para>
///
/// <para>So: once per captain, the first time it ever happens, the world stops and says what just
/// happened. Every time after that the toast is right — you understand the rule now, and a card on every
/// door would be a nag. This is the #292 ruling (a lesson may only greet the truly new) applied to the one
/// mechanic nobody would guess exists.</para>
///
/// <para>The copy's whole job is to make a promise the rest of the game keeps: <b>the edges of this place
/// are not the edges of the map.</b> A player who believes that explores differently — and exploring
/// differently is the entire point of the huts, the caches and the breadcrumbs.</para>
///
/// <para>Pure data so the copy is pinned by tests and the client only renders it — the
/// <see cref="GroundLesson"/> idiom.</para>
/// </summary>
public static class GroundGrows
{
    /// <summary>The stamp across the top of the card.</summary>
    public const string Stamp = "🗺 THE MAP JUST GOT BIGGER";

    /// <summary>The line under the stamp — what just happened, in one breath.</summary>
    public const string Head =
        "You forced it, and the plan drew itself further. That space was always here; nobody had opened it.";

    /// <summary>The art that rides the card. Missing art degrades to no image (the house
    /// <c>onerror</c> rule), never to a broken frame.</summary>
    public const string ArtFile = "art/ground-grows.jpg";

    /// <summary>The three things this teaches, in the order they change how you play. Deliberately short:
    /// this is a card that interrupts a live excursion, possibly with something walking toward you.</summary>
    public static IReadOnlyList<string> Beats { get; } =
    [
        "Nothing was loaded and you were not moved. The ground you were standing on simply continues now — "
        + "walk in, walk out, the way you would through any door.",

        "So a wall is not always the end of the site. Anything that looks sealed, dogged, drifted over or "
        + "welded shut is a question, and the tools you carry are the answer.",

        "What is behind one is worth the time it costs to open — and the time is the cost. The tracker keeps "
        + "sweeping while you work, and whatever is out there does not wait politely.",
    ];

    /// <summary>The button that dismisses it — an acknowledgement, not an "OK".</summary>
    public const string Dismiss = "Then let's see what's in there.";

    /// <summary>The line under the button: where the rule keeps being stated once the card is gone.</summary>
    public const string Foot =
        "Sealed ways read as consoles on the plan. Stand at one and hold E — stepping away lets it close again.";

    // ── #584 · AND WHERE ────────────────────────────────────────────────────────────────────────────────
    //
    // Owner, mid-tour of the rebuilt grounds: "I got like one 'you expanded the map' notification in one map
    // but I was left totally un-aware about what that did and where?"
    //
    // The card above answers WHAT, at length, and has since #563. It has never answered WHERE — and the
    // ground it announces is laid at a seeded spot that is routinely off the current view, so the most
    // distinctive thing this game does was announced by a card telling a captain to go and look at something
    // without saying which way to walk. A notification that cannot be acted on is worse than silence,
    // because the player now knows they have missed something.
    //
    // THE ANSWER IS NOT A NEW SENTENCE. Every word below is already in the game, said by the instrument or
    // the wall that owns it:
    //
    //   * the FLOOR is the building's own plate, read through WalletChoice.FloorTag — "SURFACE" out on the
    //     regolith, "B2" once the ground that grew is underground. Read, never re-derived: a building that
    //     renames its floors renames this too;
    //   * the BEARING is SdrScanner's four-word compass, the one the SDR kit already answers a captain in.
    //     Its two long words are FIELD words — the landing band and the deep field are surface geography the
    //     block downstairs borrowed (SurfaceLayout.Field owns both, above ground and below) — so the compass
    //     is honest on the regolith as well as in a corridor;
    //   * the RANGE is the fan's own "N du", the format MotionTracker.Readout has quoted a captain since
    //     #338. The guard holds it to that BY SUBSTRING against the fan's own line, so a unit invented here
    //     goes red instead of quietly becoming a second way this game says how far.
    //
    // Which leaves this function nothing of its own to say, and that is the whole point: it is punctuation
    // around three tokens somebody else owns. The instrument answers the rest — #584's other half puts a
    // ring on the new ground for the remainder of the excursion, so the card NAMES it and the fan POINTS at
    // it, which is the difference between being told and being able to act.

    /// <summary>#584 · WHERE the ground grew, in the plate idiom the game already uses for a place: the
    /// floor's own plate, the bearing from the captain, and the range the fan would quote — composed, never
    /// typed. <paramref name="dx"/>/<paramref name="dy"/> are the mouth of the new ground MINUS the captain,
    /// in the field's own coordinates.</summary>
    public static string Where(string bodyId, int level, double dx, double dy) =>
        $"{WalletChoice.FloorTag(bodyId, level)} · {SdrScanner.BearingFrom(dx, dy)} — " +
        $"{Math.Sqrt((dx * dx) + (dy * dy)):F0} du";
}
