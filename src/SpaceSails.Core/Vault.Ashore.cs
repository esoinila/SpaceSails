namespace SpaceSails.Core;

// ─── VAULT SECTIONS: WHAT HAPPENED ASHORE (#225) ───
//
// Nerve, the adrift countdown, lines overheard in a bar, field notes, the case threads they feed, what
// the captain changed on a ground and which papers have been shown to whom. The excursion's own residue.
// 
// Split out of Vault.cs under #251 — whole record types moved, nothing inside one re-ordered.

/// <summary>The captain's nerve (#317 / #226): the sanity gauge that debuts on surface excursions.
/// Persisted so a captain who fled a moon shaking is still shaking after a reload — the ease-off is time
/// spent aboard, never the load itself. Its own independently-optional section: a pre-#317 file simply
/// lacks it and defaults to a full, calm gauge with an unseen monolith.</summary>
public sealed record NerveSection
{
    /// <summary>Current nerve, 0..<see cref="NerveModel.Max"/> (full = steady hands, 0 = nerves shot).
    /// Defaults full so a file that carries the section but not this field — or no section at all — loads
    /// calm rather than at the "nerves shot" floor a bare <c>default(double)</c> would imply.</summary>
    public double Nerve { get; init; } = NerveModel.Max;

    /// <summary>True once the captain has laid eyes on the monolith. Persisted so the big first-sight hit
    /// (<see cref="NerveModel.MonolithSightShock"/>) fires once in a life and never again on a revisit.</summary>
    public bool MonolithSeen { get; init; }
}

// ── #638 · The void's countdown, which is a clock and therefore has to survive a save. ──

/// <summary>
/// #638 · The adrift countdown, as it stands. Present in the file ONLY while a clock is actually running —
/// see <see cref="Vault.Void"/> for why that is the whole save-compat story.
///
/// <para>Two numbers and nothing else, because everything else about the void is derived: the day the run
/// began, and the last day the captain was told anything on. <see cref="VoidRule"/> turns them back into a
/// countdown, and a resumed voyage that slept through a telling gets it on the next tick
/// (<see cref="VoidRule.TellingsBetween"/>), because a beat you can miss by loading is not a beat.</para>
/// </summary>
public sealed record VoidSection
{
    /// <summary>The whole sim-day (<see cref="VoidRule.DayIndex"/>) the ship was declared ADRIFT on.
    /// Defaults to <see cref="VoidRule.ClockNotRunning"/> so a file that carries the section but not this
    /// field reads as "no clock", which is the safe direction: the worst a wrong default can do here is kill
    /// a captain who was fine.</summary>
    public long DeclaredDay { get; init; } = VoidRule.ClockNotRunning;

    /// <summary>The last day-count of THIS run the captain has already been told about. Defaults to
    /// <see cref="VoidRule.NothingToldYet"/> — below day 0, because day 0 is itself a telling.</summary>
    public int LastToldDay { get; init; } = VoidRule.NothingToldYet;
}

// ── Overheard at the bar (#308/#283 → owner 2026-07-18): the words the player paid a round to hear. ──

/// <summary>One durable line of bar intel the captain has been handed (#308 OpensUp intel, a barkeep
/// rumor firmed into a tip, a round's volunteered whisper). Owner's law: "the words the player paid a
/// round to hear may not hide" and "it autodisappears which is not convenient" — so every such line is
/// WRITTEN here, revisitable, rather than lived and lost in a transient toast. A <c>readonly record
/// struct</c> — flat, tolerant, trivially round-tripped.</summary>
public readonly record struct OverheardLine(string Text, double SimTime, string Source, string BarName);

/// <summary>The captain's "overheard at the bar" book (the #119 receipt/ledger idiom, for intel): a
/// capped, time-ordered log of the tips and rumors handed across the counter. Its own
/// independently-optional section — a pre-existing file simply lacks it and defaults to an empty book.</summary>
public sealed record OverheardSection
{
    /// <summary>The overheard lines, oldest first. Capped by the writer (see <c>OverheardLog</c>).</summary>
    public IReadOnlyList<OverheardLine> Lines { get; init; } = [];
}

/// <summary>#587 · The captain's field book: everything found out on a surface, kept so it can be re-read.
/// Owner: <i>"we should maybe collect the tips to ledger if we don't show them again?"</i> — the same ruling
/// he made for bar intel in #347, pointed at the ground. Its own independently-optional section.</summary>
public sealed record FieldNotesSection
{
    /// <summary>The notes, oldest first. Capped by the writer (see <c>FieldNotes</c>).</summary>
    public IReadOnlyList<FieldNote> Notes { get; init; } = [];
}

/// <summary>#741 · THE RED LINES. Owner: <i>"I dream of drawing those conspiracy board connecting red
/// lines… I guess it could be a red pen only used to connect the things."</i>
///
/// <para>Stored as opaque pair strings (<c>CaseThreads.Thread.Stored</c>), which is the same shape
/// <see cref="SatchelSection"/> uses and for the same reason: the file carries the FACT — these two entries
/// were connected by a hand — and never the words. Both ends are
/// <c>CaseThreads.IdentityOf</c> handles derived from the note's own place, moment and text, so a book that
/// round-trips brings its lines back with it. A pair this build cannot parse is dropped on load rather than
/// thrown over.</para></summary>
public sealed record CaseThreadsSection
{
    /// <summary>The threads, in the order they were drawn. Capped by the writer (see <c>CaseThreads</c>).</summary>
    public IReadOnlyList<string> Threads { get; init; } = [];
}

/// <summary>#836 · THE CAPTAIN'S OWN PAPER TRAIL: which identity was shown, where, on what floor, and how it
/// went — one row per guard's read.
///
/// <para>Stored as opaque row strings (<c>WalletChoice.Shown.Stored</c>), the same shape
/// <see cref="SatchelSection"/> and <see cref="CaseThreadsSection"/> use and for the same reason: the file
/// carries the FACT and never the words, because every sentence built out of these rows is a seeded property
/// of the world. A row this build cannot parse is dropped on load rather than thrown over.</para>
///
/// <para>It is durable because the wallet's hint is: <i>worked here, twice</i> is worth nothing if it forgets
/// between excursions, and the paper you handed a man last night is exactly what a gumshoe would remember
/// about him.</para></summary>
/// <summary>#563 slice 2 · The marks a captain has left on a landing site's ground — see
/// <see cref="GroundMemory"/> for the key shape and for why this is a flat list of strings.</summary>
public sealed record GroundSection
{
    /// <summary>The marks, in a stable order (<see cref="GroundMemory.Stored"/>).</summary>
    public IReadOnlyList<string> Changed { get; init; } = [];
}

public sealed record PapersShownSection
{
    /// <summary>The reads, oldest first. Capped by the writer (see <c>WalletChoice.BookKeeps</c>).</summary>
    public IReadOnlyList<string> Shown { get; init; } = [];
}
