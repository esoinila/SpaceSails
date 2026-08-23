using System;

namespace SpaceSails.Core;

/// <summary>
/// #973 · THE FLASHBACK POOL — the fine print made playable, one authored memory at a time.
///
/// <para><c>docs/features/NebulaArc.md</c> §2: a resurrection is a fresh copy off your last-filed pattern, so
/// a captain remembers up to the filing line and nothing after. A FLASHBACK is that machinery seen from the
/// inside — a bleached plate with one object in focus, arriving when something in the world touches a page the
/// captain half has. It reaches the player through the ordinary story-card door
/// (<see cref="StoryBeats.Beat.Flashback"/>), so it obeys #664's cadence and deferral like every other beat
/// and needs no machinery of its own.</para>
///
/// <h3>Why a subject carries a LIFE</h3>
///
/// <para>The cadence the owner asked for is <i>once per subject per LIFE</i> — the same memory may come back
/// after a rebirth, and it must, because coming back differently is the point. #664 has exactly one cadence
/// that remembers what a beat was about (<see cref="StoryBeats.Cadence.OncePerSubject"/>) and no notion of a
/// life at all. So the life is folded into the subject: <c>signing#1</c> and <c>signing#2</c> are two subjects
/// to the seen-set and one memory to the reader. That buys the rule with no new state and, crucially, no
/// clearing step — a forgotten <c>.Clear()</c> on the rebirth path is a bug that only shows up two deaths
/// later, in somebody else's session.</para>
/// </summary>
public static class FlashbackMemories
{
    /// <summary>The day you signed. The first memory in the pool, and the one the rep hands you every time you
    /// tell him you already have a policy.</summary>
    public const string Signing = "signing";

    /// <summary>The bleached plate the whole pool is painted in. One canvas today; when the pool grows past
    /// one memory this becomes a lookup and <see cref="StoryBeats.Canvases"/> gains an arm, exactly as the two
    /// arcs' pools did.</summary>
    public const string PlateArt = "art/flashback-signing.jpg";

    /// <summary>The separator between a memory and the life it is being remembered in. A character that cannot
    /// occur in an authored memory id, so <see cref="MemoryOf"/> can never split one in half.</summary>
    private const char LifeMark = '#';

    /// <summary>The seen-set subject for one memory in one life. <paramref name="life"/> is the captain's
    /// generation, counting from one — <c>GameThreadInfo.Retired.Count + 1</c>.</summary>
    public static string SubjectForLife(string memoryId, int life) =>
        $"{memoryId}{LifeMark}{Math.Max(1, life)}";

    /// <summary>Which memory a subject is about, whether or not it carries a life.</summary>
    public static string MemoryOf(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Signing;   // the pool of one: a caller with nothing to say means the only page there is
        }

        int mark = subject.IndexOf(LifeMark);
        return mark < 0 ? subject : subject[..mark];
    }

    /// <summary>Which life a subject is being remembered in. One when the subject does not say — a memory
    /// with no life on it is a first life, which is what an unversioned caller means.</summary>
    public static int LifeOf(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return 1;
        }

        int mark = subject.IndexOf(LifeMark);
        return mark >= 0 && int.TryParse(subject[(mark + 1)..], out int life) && life >= 1 ? life : 1;
    }

    // FABLE: line needed — the stamp for the signing flashback plate. The house idiom for an arc plate is a
    // ▓ prefix and a short line that names the act and never the outcome (compare "▓ THE ONE WHO FILES YOU").
    // Placeholder below.
    /// <summary>The stamp on the signing plate.</summary>
    public const string SigningTitle = "▓ THE DAY YOU SIGNED";

    /// <summary>The plate itself. Written whole so it reads complete on a first life, with the one line that
    /// only a reborn captain can be told added on the end.</summary>
    private const string SigningCaption =
        "A desk that was not quite level. A pen that had been chained to it. You read the first clause and not "
        + "the second, because the clerk was tapping the counter, and you signed where the ink was already wet "
        + "from the one before you. They gave you the small print folded, like a napkin.";

    /// <summary>The line the second life adds, and never explains.</summary>
    public const string SigningRebornLine = "…and the hand that signed it was not this hand.";

    /// <summary>
    /// The plate for a subject, or <c>null</c> for a memory nobody has authored — the same honest degrade the
    /// two arcs' pools make, so a card can never be raised about a page that does not exist.
    /// </summary>
    public static RevealPlate? PlateFor(string subject)
    {
        string memory = MemoryOf(subject);
        if (!string.Equals(memory, Signing, StringComparison.Ordinal))
        {
            return null;
        }

        string caption = LifeOf(subject) > 1
            ? $"{SigningCaption} {SigningRebornLine}"
            : SigningCaption;

        return new RevealPlate(SigningTitle, PlateArt, caption);
    }
}
