using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// WHAT KILLED THE LAST VOYAGE, IN WORDS THE CAPTAIN CAN HAND OVER.
///
/// <para>Two of the owner's playtest screenshots end at Blazor's <i>"An unhandled error has occurred.
/// Reload."</i> — a red bar with nothing in it. The console had the real text, and the console died with
/// his tab: by the time he could file the issue the only evidence left was a photograph of a sentence
/// that says nothing. A crash the player cannot quote is a crash nobody can fix.</para>
///
/// <para>So an exception is turned into a NOTE here, in Core, where it can be tested: the type, the
/// message, and the top few stack frames, flattened to one line each and capped, plus where in the ship
/// it was thrown (<see cref="Source"/> — "frame tick", "task"…). The note round-trips through a single
/// string so it can ride localStorage and outlive the reload that erased the console, and it prints one
/// short line for the Captain's desk to show and the owner to paste into an issue.</para>
///
/// <para>Deliberately NOT a stack dump: four frames is what names the culprit, and a note the size of a
/// screen is a note nobody copies.</para>
/// </summary>
public sealed record CrashNote(
    string Source,
    string TypeName,
    string Message,
    IReadOnlyList<string> Frames,
    long WhenUtcTicks)
{
    /// <summary>How many stack frames are kept. Enough to name the culprit, short enough to paste.</summary>
    public const int MaxFrames = 4;

    /// <summary>Storage format marker — a future format can be recognised and ignored rather than
    /// mis-parsed into a note that lies about what happened.</summary>
    private const string Version = "crash-v1";

    private const int MaxMessageChars = 400;

    /// <summary>Read an exception into a note. Never throws: a null/odd exception still yields a note
    /// saying so, because the one thing the crash reporter may not do is crash.</summary>
    public static CrashNote From(string source, Exception? ex, long whenUtcTicks)
    {
        if (ex is null)
        {
            return new CrashNote(Flatten(source), "(none)", "an error with no exception attached", [], whenUtcTicks);
        }

        // An AggregateException's own message is boilerplate ("One or more errors occurred") — the note
        // should name the exception that actually went off.
        Exception real = ex is AggregateException agg && agg.InnerExceptions.Count > 0
            ? agg.InnerExceptions[0]
            : ex;

        return new CrashNote(
            Flatten(source),
            real.GetType().Name,
            Clip(Flatten(real.Message)),
            TopFrames(real.StackTrace),
            whenUtcTicks);
    }

    /// <summary>The top frames of a stack trace string, trimmed of the "   at " noise and capped.</summary>
    public static IReadOnlyList<string> TopFrames(string? stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
        {
            return [];
        }

        var frames = new List<string>();
        foreach (string raw in stackTrace.Split('\n'))
        {
            string line = raw.Trim().TrimStart();
            if (line.StartsWith("at ", StringComparison.Ordinal))
            {
                line = line[3..];
            }

            if (line.Length == 0)
            {
                continue;
            }

            frames.Add(Clip(Flatten(line)));
            if (frames.Count == MaxFrames)
            {
                break;
            }
        }

        return frames;
    }

    /// <summary>The one line the Captain's desk shows and the owner pastes into an issue.</summary>
    public string Describe()
    {
        string where = Frames.Count > 0 ? $" — at {Frames[0]}" : "";
        return $"{TypeName}: {Message}{where} [{Source}]";
    }

    /// <summary>The whole note, frames and all, as the multi-line text behind the [copy] button.</summary>
    public string DescribeFully()
    {
        var lines = new List<string> { Describe() };
        for (int i = 1; i < Frames.Count; i++)
        {
            lines.Add($"    at {Frames[i]}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>One string, safe for localStorage. Line-based rather than JSON so the parser cannot be
    /// surprised by a message that happens to contain a brace or a quote.</summary>
    public string ToStorage()
    {
        var lines = new List<string>
        {
            Version,
            Source,
            TypeName,
            Message,
            WhenUtcTicks.ToString(CultureInfo.InvariantCulture),
        };
        lines.AddRange(Frames);
        return string.Join("\n", lines);
    }

    /// <summary>Read a stored note back. False for anything that is not a note this version wrote —
    /// a stale key, a truncated write, another game's leftovers.</summary>
    public static bool TryParse(string? stored, out CrashNote note)
    {
        note = null!;
        if (string.IsNullOrWhiteSpace(stored))
        {
            return false;
        }

        string[] lines = stored.Replace("\r\n", "\n").Split('\n');
        if (lines.Length < 5 || lines[0] != Version)
        {
            return false;
        }

        if (!long.TryParse(lines[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks))
        {
            return false;
        }

        string[] frames = lines.Length > 5 ? lines[5..] : [];
        note = new CrashNote(lines[1], lines[2], lines[3], frames, ticks);
        return true;
    }

    /// <summary>Newlines and tabs out: the note's own format is line-based, and a message that carries a
    /// newline would otherwise cut the note in half on the way back in.</summary>
    private static string Flatten(string? text) =>
        (text ?? "").Replace("\r\n", " / ").Replace("\n", " / ").Replace("\r", " / ").Replace('\t', ' ').Trim();

    private static string Clip(string text) =>
        text.Length <= MaxMessageChars ? text : text[..MaxMessageChars] + "…";
}
