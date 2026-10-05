namespace SpaceSails.Core;

/// <summary>
/// #620 · THE KEEPSAKE SHELF — "my precious", played straight. Owner, 2026-10-04: <i>"Think of that in
/// reference of the Lord of the Rings, Frodo's Ring. It's precious — something made of gold but far more
/// precious. If we have items of great plot significance, like a pendant with a picture in it … Some item
/// that brings back memories or was crucial earlier, or signals past relationships, maybe love, betrayal
/// etc. … Also such an item could be used to reduce stress. Some kind of special treatment in the
/// inventory."</i>
///
/// <para>A keepsake is an item tier above plot items: a thing whose value is WHO IT POINTS AT, not what it
/// opens. SLICE 1 is the first memento only — THE PENDANT, seeded on the shelf from the first minute of every
/// run (the breadcrumb ruling, 2026-09-13: own lineage first). SLICE 2 puts the #973 photograph and the slips on the
/// shelf beside it (<see cref="FromSheet"/>, <see cref="Shelf(IReadOnlyList{HeldMemory.Sheet})"/>); the
/// annular collar (slice 3) is not here; the model is shaped for them (<see cref="Piece.Theory"/> already
/// speaks Love / Money / Unsettled) but nothing else is built.</para>
///
/// <para><b>THE QUIET MINUTE.</b> Opening a memento in the captain's own cabin, alone, restores nerve through
/// the EXISTING #339 relief seam (<see cref="NerveModel.DrinkRestore"/> with
/// <see cref="NerveModel.DrinkKind.Keepsake"/>), never a parallel one. One satiety window is SHARED across the
/// whole shelf (the sleep idiom — <see cref="CabinComforts.StillRested"/>): look again too soon and the locket
/// does nothing but say why. Elsewhere the shelf shows and the locket does not open (seat-tied and private —
/// the Kosh principle: spent rarely and privately).</para>
///
/// <para><b>THE UNSETTLED BAND.</b> The pendant's theory is neither Love nor Money. It restores clean, but on
/// the SAME seeded roll, one visit in <see cref="StingOneIn"/>, the face reads differently and the minute
/// costs a dab instead — the toilet's 1-in-12 idiom (<see cref="CabinComforts.VisitToilet"/>), the same
/// <see cref="DiceRule.Seed(string, long[])"/> + <see cref="DeterministicRandom"/> discipline. The ambiguity
/// IS the mechanic. No line, card or colleague ever settles it (§13.8).</para>
///
/// <para><b>THE MONEY BAND (slice 2).</b> A held-memory sheet is a memento too. A LOVE-marked sheet restores
/// clean (22) from the sheets' own four clean lines; a MONEY-marked one restores less
/// (<see cref="NerveModel.KeepsakeMoneyRestore"/>) and on the same seeded 1-in-<see cref="StingOneIn"/> roll
/// STINGS instead, from its own three lines. The pendant's Unsettled band belongs to the pendant alone: an
/// Unsettled sheet routes as Love. The shelf holds faces only (<see cref="IsMemento"/>).</para>
///
/// <para>Everything here is PURE and DETERMINISTIC: same nerve + same gap + same sim time + same place →
/// same minute. Every sentence the player reads is Fable's canon from the issue (2026-10-05), verbatim.
/// Magnitudes are FLAGGED for the owner's tuning.</para>
/// </summary>
public static class Keepsake
{
    // ─────────────────────────────── THE MODEL ───────────────────────────────

    /// <summary>One piece on the shelf: its stable id, its closed card, and the theory its face serves.
    /// Pure data — the file carries the FACT and the words live here, never in the page.</summary>
    /// <param name="Id">Stable across a save and a reload.</param>
    /// <param name="Title">The shelf card's title, closed.</param>
    /// <param name="CardLine">The one line under the title.</param>
    /// <param name="Mark">Whose memory it is (<see cref="HeldMemory.Mark"/>) — the pendant is the captain's
    /// own.</param>
    /// <param name="Theory">Which theory the face serves; <see cref="HeldMemory.Theory.Unsettled"/> for the
    /// pendant, which no line ever settles.</param>
    /// <param name="FlashbackSubject">The subject the first opening raises the Flashback beat with.</param>
    public readonly record struct Piece(
        string Id,
        string Title,
        string CardLine,
        HeldMemory.Mark Mark,
        HeldMemory.Theory Theory,
        string FlashbackSubject)
    {
        /// <summary>A held-memory SHEET (slice 2) rather than the pendant: it carries no flashback subject, so
        /// it has no first opening, no unsettled band and no pendant lines.</summary>
        public bool IsSheet => FlashbackSubject.Length == 0;
    }

    /// <summary>The pendant's stable id.</summary>
    public const string PendantId = "pendant";

    /// <summary>The subject of the pendant's flashback plate — the caption is chosen by it
    /// (<see cref="FlashbackCaption"/>), the way the photograph's and the walk-in's are.</summary>
    public const string PendantSubject = "the face in the pendant";

    /// <summary>The pendant's shelf card title (canon, verbatim).</summary>
    public const string PendantTitle = "A gold pendant, worn smooth";

    /// <summary>The pendant's shelf card line (canon, verbatim).</summary>
    public const string PendantCardLine = "A hinge, a hairline seam, and more weight than the gold explains.";

    /// <summary>THE PENDANT — gold, worn smooth, a hinge; a picture of a face inside, and the ledger has no
    /// page for it. Marked MINE (it is the captain's own) and UNSETTLED (nobody ever says who it is).</summary>
    public static readonly Piece Pendant = new(
        PendantId, PendantTitle, PendantCardLine,
        HeldMemory.Mark.Mine, HeldMemory.Theory.Unsettled, PendantSubject);

    /// <summary>What is on the shelf. Every run starts with the pendant already there ("seeded in the cabin
    /// locker from day one"); the shelf has no other occupant in slice 1. A function, not a stored list, so
    /// nothing mutable lives on a static.</summary>
    public static IReadOnlyList<Piece> Shelf() => [Pendant];

    /// <summary>#620 slice 2 · A held-memory sheet as a memento on the shelf. Everything on the card is the
    /// sheet's own existing field — its row title (<see cref="HeldMemory.RowTitle"/>) and its book line (whose
    /// it is, which theory, who handed it over, the day) — so nothing is authored here. A sheet carries no
    /// flashback subject: its beat (the photograph's, #973) fired at handover, so opening it in the cabin is
    /// only ever a quiet minute.</summary>
    public static Piece FromSheet(HeldMemory.Sheet sheet) =>
        new(sheet.Id, HeldMemory.RowTitle(sheet), sheet.BookLine, sheet.Mark, sheet.Tag, FlashbackSubject: "");

    /// <summary>#620 slice 2 · Is this sheet a MEMENTO — a held FACE? The shelf is for faces, by an EXPLICIT
    /// include list (coordinator ruling): the #973 photograph, a shipmate's slip (<see cref="HeldMemory.SlipId"/>
    /// ids) and the summer-party picture (<see cref="OldCrewScene.SummerPartyId"/>, which holds faces). The
    /// signing memory, the filing-day sheet, walk-in job slips, weather/old-ship pages and strays are ledger
    /// evidence, not mementos — the canon quiet-minute lines would misfire on them. A future memento opts in
    /// deliberately by joining this list.</summary>
    public static bool IsMemento(HeldMemory.Sheet sheet) =>
        string.Equals(sheet.Id, HeldMemory.PhotographId, StringComparison.Ordinal)
        || string.Equals(sheet.Id, OldCrewScene.SummerPartyId, StringComparison.Ordinal)
        || sheet.Id.StartsWith(HeldMemory.SlipId(string.Empty), StringComparison.Ordinal);

    /// <summary>#620 slice 2 · THE SHELF WITH THE BOOK'S FACES — the pendant first, then every held sheet that
    /// is a memento (<see cref="IsMemento"/>), in the book's own order.</summary>
    public static IReadOnlyList<Piece> Shelf(IReadOnlyList<HeldMemory.Sheet> book)
    {
        ArgumentNullException.ThrowIfNull(book);
        var pieces = new List<Piece>(book.Count + 1) { Pendant };
        foreach (HeldMemory.Sheet sheet in book)
        {
            if (IsMemento(sheet))
            {
                pieces.Add(FromSheet(sheet));
            }
        }

        return pieces;
    }

    // ─────────────────────────────── THE WORDS (canon, verbatim) ───────────────────────────────

    /// <summary>The first opening — the flashback plate's caption (canon, verbatim). Raised ONCE per run.</summary>
    public const string FirstOpeningLine =
        "You know the face the way you know a word in a language you've stopped dreaming in. The name is not where you left it. " +
        "Somewhere a door closes — a real one, down the corridor — and for half a second it is a different door, years ago, " +
        "and you are the one leaving. Or being left. The picture doesn't say. It never has.";

    /// <summary>The field-book line the first opening writes (📍, once; canon, verbatim).</summary>
    public const string FieldBookLine =
        "A face I carry. No page for it in the ledger. The pendant was never written down — maybe that's why I still have it.";

    /// <summary>The field book's glyph for the line above.</summary>
    public const string FieldBookGlyph = "📍";

    /// <summary>The flashback plate's caption for a keepsake subject, or null for every other subject so the
    /// existing flashbacks keep their own words. Chosen by the subject (the walk-in's pattern).</summary>
    public static string? FlashbackCaption(string? subject) =>
        string.Equals(subject, PendantSubject, StringComparison.Ordinal) ? FirstOpeningLine : null;

    /// <summary>The plate's title stamp for the pendant's flashback (Fable's canon addendum on #620,
    /// 2026-10-05, verbatim): the pendant was never a page, so the generic "A PAGE YOU DON'T REMEMBER
    /// WRITING" would contradict it. The caller keeps the mark prefix and the styling.</summary>
    public const string FlashbackTitleText = "A PAGE THAT WAS NEVER WRITTEN";

    /// <summary>The plate's title for a keepsake subject, or null for every other subject so no other
    /// flashback's stamp changes.</summary>
    public static string? FlashbackTitle(string? subject) =>
        string.Equals(subject, PendantSubject, StringComparison.Ordinal) ? FlashbackTitleText : null;

    // Five clean minutes, rotated on the seeded roll (canon, verbatim).
    private static readonly string[] CleanLines =
    [
        "You look until the lamp's hum comes back. You put it away before you can decide anything. Steadier, though. Always steadier.",
        "The hinge knows your thumb. A minute goes by that belongs to nobody — not the Authority, not the ledger, not the wire. You pocket it warm.",
        "You angle it to the light the way you always do, as if the face might blink. It doesn't. Something in your chest unclenches anyway.",
        "Whoever they were, they are still exactly where you left them: behind glass the size of a thumbnail, keeping your worst hours company.",
        "You don't say anything. Neither do they. It's the best conversation you've had all watch.",
    ];

    // #620 slice 2 · Four clean minutes for a SHEET, on the same seeded roll (Fable's canon addendum 3,
    // verbatim): a photograph has no hinge, so the pendant's five clean lines stay the pendant's.
    private static readonly string[] SheetCleanLines =
    [
        "You hold it by the edges, the way you were never taught and always knew. A minute passes that nobody bills.",
        "The faces have not aged a day since the last time you looked. Somebody in this picture still owes somebody a drink.",
        "Paper remembers differently than you do — flatter, kinder. You let its version stand a while.",
        "You know where you were standing when this was taken. You can almost feel the floor of it under your boots.",
    ];

    /// <summary>The four clean quiet-minute lines for a held-memory sheet (Love or Money alike).</summary>
    public static IReadOnlyList<string> SheetCleanPool { get; } = Array.AsReadOnly(SheetCleanLines);

    // Three unsettled stings, on the same roll (canon, verbatim).
    private static readonly string[] StingLines =
    [
        "Tonight the smile reads differently, and you were there, and you still can't say for what.",
        "For one bad second you remember a dock, and rain that can't have been rain, and which of you walked. Then it's gone, and the lamp is just a lamp.",
        "You catch yourself checking the seam, as if something could have gotten in. Nothing has. Something got out, years ago. You put it away too fast.",
    ];

    /// <summary>The satiety refusal, shared across the whole shelf (canon, verbatim).</summary>
    public const string SatietyLine = "It's the same picture it was an hour ago. That's not what it's for.";

    // Two not-here refusals (canon, verbatim).
    private static readonly string[] NotHereLines =
    [
        "Not here. Not with the concourse watching.",
        "Your thumb finds the hinge and stops. This is a cabin thing. It has always been a cabin thing.",
    ];

    /// <summary>The five clean quiet-minute lines, for the tests and for any reader that needs the pool.</summary>
    public static IReadOnlyList<string> CleanPool { get; } = Array.AsReadOnly(CleanLines);

    /// <summary>The three unsettled sting lines.</summary>
    public static IReadOnlyList<string> StingPool { get; } = Array.AsReadOnly(StingLines);

    /// <summary>The two not-here refusals.</summary>
    public static IReadOnlyList<string> NotHerePool { get; } = Array.AsReadOnly(NotHereLines);

    // #620 slice 2 · Three stings for a MONEY-marked memento (a #973 slip or sheet), on the same seeded roll
    // (canon, verbatim — the issue's "Money-marked mementos" pool).
    private static readonly string[] MoneyStingLines =
    [
        "A good evening, a steady hand on your shoulder, and you know exactly what it cost you, to the decimal. You look anyway.",
        "They're smiling because the deal hadn't landed yet. You keep it because somebody has to remember the before.",
        "The picture hasn't changed. Your reading of it does, some nights, and tonight is one of them.",
    ];

    /// <summary>The three Money-marked sting lines.</summary>
    public static IReadOnlyList<string> MoneyStingPool { get; } = Array.AsReadOnly(MoneyStingLines);

    /// <summary>Every string this type publishes — the sweep's one source (a reflection test fails on a
    /// public string constant that is not declared here).</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return PendantTitle;
        yield return PendantCardLine;
        yield return FirstOpeningLine;
        yield return FieldBookLine;
        yield return FlashbackTitleText;
        yield return SatietyLine;
        foreach (string line in CleanLines) { yield return line; }
        foreach (string line in SheetCleanLines) { yield return line; }
        foreach (string line in StingLines) { yield return line; }
        foreach (string line in MoneyStingLines) { yield return line; }
        foreach (string line in NotHereLines) { yield return line; }
    }

    // ─────────────────────────────── THE LAW ───────────────────────────────

    /// <summary>How long a quiet minute leaves the whole shelf SATED (sim-seconds). Open anything inside the
    /// window and the locket does nothing but say <see cref="SatietyLine"/>. One window for the shelf, never
    /// one per piece — no grinding, no per-item rotation tricks. Two sim-hours: longer than a night's bunk
    /// advances the clock (so sleeping then looking is honest) and far shorter than the bunk's own six-hour
    /// window. FLAGGED for the owner's tuning.</summary>
    public const double QuietWindowSeconds = 2.0 * 3600.0;

    /// <summary>Whether the shelf is still sated from a quiet minute <paramref name="secondsSinceMinute"/>
    /// ago — the sleep idiom to the letter: true inside the window, and a negative gap (no minute yet, or a
    /// clock that jumped backwards on a load) reads as NOT sated, so the first minute always lands.</summary>
    public static bool StillSated(double secondsSinceMinute) =>
        secondsSinceMinute >= 0.0 && secondsSinceMinute < QuietWindowSeconds;

    /// <summary>The unsettled band on the seeded roll: 1-in-<see cref="StingOneIn"/> minutes the face reads
    /// differently. The toilet's house idiom (<see cref="CabinComforts.ScareOneIn"/>), kept as its own
    /// constant so one can be tuned without the other. FLAGGED.</summary>
    public const int StingOneIn = 12;

    /// <summary>The dab a stung minute COSTS instead of restoring — small, on a par with the toilet's scare
    /// so a bad minute undoes a good one and never wrecks a captain. FLAGGED.</summary>
    public const double StingNerve = 4.0;

    /// <summary>#620 slice 2 · The dab a stung Money-marked minute costs — its own constant so the sheets'
    /// betrayal can be tuned apart from the pendant's unsettled band; the same small number to start.
    /// FLAGGED.</summary>
    public const double MoneyStingNerve = 4.0;

    /// <summary>What happened when the locket was pressed.</summary>
    public enum Outcome
    {
        /// <summary>A clean quiet minute: nerve restored, one of the pendant's five lines or a sheet's four.</summary>
        Restored,

        /// <summary>The unsettled band: the face read differently, a dab was spent instead.</summary>
        Stung,

        /// <summary>The first opening in the cabin: restored (always clean) and the flashback is raised.</summary>
        FirstOpening,

        /// <summary>Inside the shared satiety window — nothing happens but the line.</summary>
        Sated,

        /// <summary>Not in the captain's own cabin, alone — the locket does not open.</summary>
        NotHere,
    }

    /// <summary>The outcome of pressing a keepsake: what happened, the nerve after, the SIGNED nerve change,
    /// and the line to show. <see cref="Opened"/> is true for the three outcomes that spent a minute (and so
    /// stamp the shared window); a refusal changes nothing and stamps nothing.</summary>
    public readonly record struct QuietMinute(Outcome Outcome, double Nerve, double Delta, string Line)
    {
        /// <summary>Whether a minute was actually spent (restored, stung or the first opening).</summary>
        public bool Opened => Outcome is Outcome.Restored or Outcome.Stung or Outcome.FirstOpening;

        /// <summary>Whether the first-opening flashback + field-book line should be raised.</summary>
        public bool RaisesFlashback => Outcome == Outcome.FirstOpening;
    }

    /// <summary>
    /// Open a piece. Order of refusals is the order of honesty: the locket does not open outside the
    /// captain's own cabin AT ALL (so a refusal there never spends or reads the window), then the shared
    /// satiety window, then the minute itself.
    ///
    /// <para>The FIRST opening in the cabin (once per run, <paramref name="firstOpening"/>) always lands
    /// CLEAN — a first look at an unplaceable face is not the night to be stung — and its line is the flashback
    /// plate's, raised by the client. Every later minute rolls ONE seeded roll, sim-time salted so each
    /// differs yet replays exactly, that decides BOTH the sting band and which line you get. Pure.</para>
    /// </summary>
    /// <param name="piece">What was pressed.</param>
    /// <param name="inOwnCabin">The captain is in his own cabin, alone — the only place it opens.</param>
    /// <param name="firstOpening">This run has not yet opened a keepsake in the cabin.</param>
    /// <param name="nerve">The current nerve.</param>
    /// <param name="secondsSinceMinute">Sim-seconds since the last quiet minute; negative = never.</param>
    /// <param name="simTime">The sim clock — the roll's salt, never a wall clock.</param>
    public static QuietMinute Open(
        Piece piece, bool inOwnCabin, bool firstOpening, double nerve, double secondsSinceMinute, double simTime)
    {
        ulong seed = DiceRule.Seed("keepsake-quiet-minute", (long)simTime);
        var rng = new DeterministicRandom(seed);

        if (!inOwnCabin)
        {
            // The refusal line comes off its own roll, so the one that decides the sting is never perturbed.
            var refusal = new DeterministicRandom(DiceRule.Seed("keepsake-not-here", (long)simTime));
            return new QuietMinute(Outcome.NotHere, NerveModel.Clamp(nerve), 0.0,
                NotHereLines[refusal.NextInt(0, NotHereLines.Length)]);
        }

        if (StillSated(secondsSinceMinute))
        {
            return new QuietMinute(Outcome.Sated, NerveModel.Clamp(nerve), 0.0, SatietyLine);
        }

        // The band and the line share ONE roll — the toilet's idiom: the band is drawn first, then the line.
        // The band is drawn for EVERY piece (a Love sheet's clean line comes off the same stream the pendant's
        // does), but only the pendant's UNSETTLED face and a MONEY-marked sheet can be stung by it.
        bool band = rng.NextInt(0, StingOneIn) == 0;
        bool money = piece.Theory == HeldMemory.Theory.Money;

        // Only the pendant has a first opening (a sheet's flashback fired at handover): no subject, no first.
        // The UNSETTLED band is the PENDANT's alone: an Unsettled SHEET (not made today, but defined) routes as
        // LOVE — 22 clean from the sheet pool, never stung, never borrowing the pendant's seam lines.
        bool first = firstOpening && !piece.IsSheet;
        bool unsettledBand = !piece.IsSheet && piece.Theory == HeldMemory.Theory.Unsettled;
        if (band && !first && (money || unsettledBand))
        {
            double dab = money ? MoneyStingNerve : StingNerve;
            string[] pool = money ? MoneyStingLines : StingLines;
            double after = NerveModel.Clamp(nerve - dab);
            return new QuietMinute(Outcome.Stung, after, after - NerveModel.Clamp(nerve),
                pool[rng.NextInt(0, pool.Length)]);
        }

        double restored = NerveModel.DrinkRestore(
            nerve, money ? NerveModel.DrinkKind.KeepsakeMoney : NerveModel.DrinkKind.Keepsake, totNumber: 1);
        double delta = restored - NerveModel.Clamp(nerve);
        if (first)
        {
            return new QuietMinute(Outcome.FirstOpening, restored, delta, FirstOpeningLine);
        }

        string[] clean = piece.IsSheet ? SheetCleanLines : CleanLines; // a sheet has no hinge
        return new QuietMinute(Outcome.Restored, restored, delta, clean[rng.NextInt(0, clean.Length)]);
    }
}
