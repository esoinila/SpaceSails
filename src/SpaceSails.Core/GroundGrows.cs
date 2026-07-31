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
}
