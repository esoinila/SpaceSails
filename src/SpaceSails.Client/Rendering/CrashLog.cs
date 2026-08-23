using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// THE SHIP'S OWN BLACK BOX (#963 follow-up, owner's playtest 2026-08-22).
///
/// <para>Two of his screenshots end at Blazor's <i>"An unhandled error has occurred. Reload."</i> — a red
/// bar with no text in it. Whatever the console said died with the tab, so the bug arrived as a
/// photograph of a sentence that names nothing. This is the fix, and it is deliberately the smallest
/// thing that works: every place an exception can escape the game hands it here, this turns it into a
/// <see cref="CrashNote"/>, and the note goes two ways — into memory, where the Captain's desk reads it,
/// and out through <see cref="Persist"/> to localStorage, where it OUTLIVES the reload. Come back after
/// the crash and the desk is holding the sentence the console took with it.</para>
///
/// <para>THE REPORTER MAY NOT CRASH. Every path in here swallows its own trouble: a storage that refuses
/// the write, a null exception, a persist hook that throws. The worst case is a note that did not land,
/// never a second crash on top of the first.</para>
///
/// <para>Static because the things that can throw — the rAF frame tick, an unobserved task, a component
/// lifecycle — have no component to hand a service to; they are the paths where the app is already coming
/// apart. <see cref="Reset"/> exists for the tests, which are the only callers that need to forget.</para>
/// </summary>
public static class CrashLog
{
    /// <summary>The localStorage key the note survives the reload in. Shared with the tiny JS bridge in
    /// index.html, which writes the SAME format for the errors that never reach .NET at all.</summary>
    public const string StorageKey = "spacesails.lastVoyageError";

    /// <summary>How many notes are kept in memory. A handler that throws throws EVERY frame; the cap (and
    /// the consecutive-duplicate fold below) is what stops a crash from becoming a memory leak at 60 Hz.</summary>
    private const int MaxKept = 8;

    private static readonly List<CrashNote> Kept = [];

    /// <summary>Where a note goes to outlive the reload. Set by the game once localStorage is reachable;
    /// null in a test or before boot, which simply means the note lives in memory only.</summary>
    public static Action<CrashNote>? Persist { get; set; }

    /// <summary>Raised for each newly recorded note (the folded repeats do not raise). The ledger listens
    /// so the Captain's desk can light up in the same session the crash happened in.</summary>
    public static event Action<CrashNote>? Noted;

    /// <summary>The notes this session recorded, oldest first.</summary>
    public static IReadOnlyList<CrashNote> Entries => Kept;

    /// <summary>The most recent note, or null if this voyage has been clean.</summary>
    public static CrashNote? Latest => Kept.Count > 0 ? Kept[^1] : null;

    /// <summary>The clock the notes are stamped from. A field so a test can pin it.</summary>
    public static Func<long> UtcTicksNow { get; set; } = () => DateTimeOffset.UtcNow.UtcTicks;

    /// <summary>Turn an exception into a note, keep it, and try to make it durable. Safe to call from a
    /// catch block that is already on fire.</summary>
    public static CrashNote Report(string source, Exception? ex)
    {
        CrashNote note;
        try
        {
            note = CrashNote.From(source, ex, UtcTicksNow());
        }
        catch
        {
            // Even reading the exception failed. Something is still better than nothing.
            note = new CrashNote(source, "(unreadable)", "an error that could not be read", [], 0);
        }

        // Fold a handler that throws the same thing every frame into ONE note.
        if (Kept.Count > 0 && Kept[^1] is { } last
            && last.TypeName == note.TypeName && last.Message == note.Message && last.Source == note.Source)
        {
            return last;
        }

        Kept.Add(note);
        if (Kept.Count > MaxKept)
        {
            Kept.RemoveAt(0);
        }

        try
        {
            Persist?.Invoke(note);
        }
        catch
        {
            // A storage that refuses the write is not worth a second crash — the note still lives in memory.
        }

        try
        {
            Noted?.Invoke(note);
        }
        catch
        {
            // A listener that throws must not take the reporter with it.
        }

        return note;
    }

    /// <summary>Run something that must not be allowed to disappear silently. The exception is recorded and
    /// then RETHROWN: this changes what is KNOWN about a crash, never what the crash does.</summary>
    public static void RunGuarded(string source, Action body)
    {
        try
        {
            body();
        }
        catch (Exception ex)
        {
            Report(source, ex);
            throw;
        }
    }

    /// <summary>Hook the two runtime-wide escapes (Program.cs calls this once at boot).</summary>
    public static void Install()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report("unhandled", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Report("task", e.Exception);
            e.SetObserved(); // an unobserved task exception is already lost; observing it keeps it from re-raising
        };
    }

    /// <summary>Forget the notes — the captain has read them and filed the issue. Leaves the hooks in
    /// place: the black box keeps recording.</summary>
    public static void Forget() => Kept.Clear();

    /// <summary>Forget everything, hooks included (tests only — the game never un-crashes).</summary>
    public static void Reset()
    {
        Kept.Clear();
        Persist = null;
        Noted = null;
        UtcTicksNow = () => DateTimeOffset.UtcNow.UtcTicks;
    }
}
