namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L4 · THE ADVERTISING, AND WHAT IT SHAKES LOOSE.
//
// The PIRATE INSURANCE poster has hung in every port since #380 and has always been the same two
// reads: the cheerful sell, and — the second time you stop at one — the grey line no advertising
// should keep (`NebulaLore.fine-print`). Neither of those changes here.
//
// What L4 adds is the thing a rebirth does to a wall you have walked past a hundred times. A captain
// who has died once reads the same poster differently, because the afternoon it is selling is an
// afternoon he can no longer be sure he was at; and the three small NEBULA MUTUAL plates hung around
// the concourse do the same job in three instalments — one line each of ONE memory, THE FILING DAY,
// which is only whole when all three have been read.
//
// THE ORDER OF THE LINES IS NOT THE ORDER OF THE ADS. A captain who reads ad 3 first still gets the
// afternoon in the order the afternoon happened: the clerk, then the small print, then the pen. The
// sheet is a MEMORY, not a log of where the captain walked, and a memory assembled in the order the
// captain happened to wander would be the fifth bug class wearing a fiction's clothes.
//
// Nothing in this file names what is in a pod, and the word "copy" does not appear in any of it.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#973 L4 · The three NEBULA MUTUAL wall plates, the memory they assemble between them, and
/// the two sentences a rebirth adds to the old PIRATE INSURANCE poster. Pure authored Core data — the
/// client hangs the fixtures and detects them by their own labels, exactly as the poster is detected.</summary>
public static class StationAds
{
    // ── §1 · THE THREE ADS ───────────────────────────────────────────────────────────────────────────

    /// <summary>The emoji tag every one of these plates wears on the deck — the house's fixture idiom
    /// (📋 for the poster, 👕 for the tee, ⛑ for the muster). It is a TAG and not a word of the ad: the
    /// advertising itself is Fable's and is reproduced with nothing added to it.</summary>
    public const string Tag = "📣";

    /// <summary>
    /// One wall plate: what it says, and the label the deck hangs it under.
    /// </summary>
    /// <param name="Text">Fable's words, verbatim, and the string the client matches a console label on.</param>
    /// <param name="Line">The one line of <see cref="TheFilingDay"/> this plate is worth — its place in
    /// the afternoon, which is not necessarily its place on the captain's walk.</param>
    public readonly record struct Ad(string Text, string Line)
    {
        /// <summary>What the console is labelled on the deck plan, and what a captain reads walking past.</summary>
        public string Label => $"{Tag} {Text}";
    }

    /// <summary>
    /// The three, in the order the AFTERNOON ran — which is the order their lines are laid into the sheet,
    /// whichever plate the captain stopped at first.
    /// </summary>
    public static IReadOnlyList<Ad> Ads { get; } =
    [
        new("NEBULA MUTUAL — Because the void does not take appointments.",
            "The clerk's finger, tapping the counter. You signed faster."),
        new("NEBULA MUTUAL — Premium remembers. Basic returns. Ask at any desk.",
            "The small print, folded, in your other hand. You did not unfold it."),
        new("NEBULA MUTUAL — Your pattern, kept. Terms on file.",
            "The chain on the pen, too short; you leaned in to sign. Somebody behind you sighed."),
    ];

    /// <summary>Which of the three a console label is, or null for any other fixture in the game. Matched on
    /// the ad's own WORDS rather than on an index baked into a label, so a plate cannot be hung under one
    /// number and read as another — the poster's <c>fine-print</c> detection is the same idiom.</summary>
    public static int? IndexOfLabel(string? label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return null;
        }

        for (int i = 0; i < Ads.Count; i++)
        {
            if (label.Contains(Ads[i].Text, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return null;
    }

    // ── §2 · THE FILING DAY ──────────────────────────────────────────────────────────────────────────

    /// <summary>The sheet the three plates assemble between them. Marked <i>mine</i> and tagged
    /// <b>money</b>, for the same reason the signing sheet is: it is a memory of a transaction, and the
    /// SPREAD's second question is only worth asking if the tags mean something.</summary>
    public const string TheFilingDay = "the-filing-day";

    /// <summary>How many lines the afternoon has. Stated once so the completion rule and the pool cannot
    /// disagree about when a memory is whole.</summary>
    public static int LinesInTheAfternoon => Ads.Count;

    /// <summary>
    /// The sheet's text for a captain who has read <paramref name="seen"/> of the plates — the lines
    /// present, in the afternoon's order, joined the way the signing sheet joins its reborn line.
    ///
    /// <para>Order is imposed HERE and nowhere else, so no caller has to remember to sort. A caller that
    /// passed the indices in the order the captain walked gets the afternoon back in the order it happened.</para>
    /// </summary>
    public static string TextFor(IEnumerable<int> seen)
    {
        ArgumentNullException.ThrowIfNull(seen);
        var have = new HashSet<int>();
        foreach (int i in seen)
        {
            if (i >= 0 && i < Ads.Count)
            {
                have.Add(i);
            }
        }

        var parts = new List<string>(Ads.Count);
        for (int i = 0; i < Ads.Count; i++)
        {
            if (have.Contains(i))
            {
                parts.Add(Ads[i].Line);
            }
        }

        return string.Join(' ', parts);
    }

    /// <summary>
    /// WHICH LINES A SHEET ALREADY CARRIES, read back off its own text. The sheet is the whole of the
    /// bookkeeping — there is no second store of which plates a captain has stopped at, and there must not
    /// be one: a set kept beside the sheet would have to be persisted, migrated and cleared, and could
    /// disagree with the words actually on the page. This cannot.
    /// </summary>
    public static IReadOnlyList<int> LinesIn(string? text)
    {
        var found = new List<int>();
        if (string.IsNullOrEmpty(text))
        {
            return found;
        }

        for (int i = 0; i < Ads.Count; i++)
        {
            if (text.Contains(Ads[i].Line, StringComparison.Ordinal))
            {
                found.Add(i);
            }
        }

        return found;
    }

    /// <summary>Is the afternoon whole? Asked of a sheet's text, so the rule and the page agree by
    /// construction.</summary>
    public static bool IsWhole(string? text) => LinesIn(text).Count >= LinesInTheAfternoon;

    /// <summary>The line the third plate leaves the captain with — the whole afternoon, measured.</summary>
    public const string WholeToast =
        "That was the whole of the afternoon. You were there for eleven minutes.";

    // ── §3 · THE POSTER, AFTER A REBIRTH ─────────────────────────────────────────────────────────────

    /// <summary>What a reborn captain gets from a poster whose afternoon is already in the book. The
    /// sentence says twice what it is about, which is the joke and is not a typo.</summary>
    public const string PosterAgainToast =
        "You have read this before. You have read this before that, too.";

    // ── §4 · YOU HAVE BEEN HERE ──────────────────────────────────────────────────────────────────────

    /// <summary>Arriving somewhere a page you don't remember writing already names.</summary>
    public const string BeenHereToast = "You have been here. — No. He has.";

    /// <summary>…and what the place does to the page: finishes it, with no dice thrown at all.</summary>
    public const string PlaceFinishesToast = "The place finishes the page.";

    /// <summary>
    /// DOES THIS PAGE NAME THIS PLACE? Asked of the page's own three text fields — the title, its lines,
    /// and the provenance the ledger writes as <c>&lt;who&gt; · &lt;where&gt; · day N</c>. A place is named
    /// by a page when the page says its name; nothing subtler is honest, because the captain reading the
    /// row would be reading exactly the same words.
    ///
    /// <para>A blank name never matches. Every row in the ledger contains the empty string, so a body whose
    /// name failed to resolve would otherwise finish the entire book on one landing.</para>
    /// </summary>
    public static bool NamesThePlace(LedgerPage page, string? placeName)
    {
        if (string.IsNullOrWhiteSpace(placeName))
        {
            return false;
        }

        if (page.Title is { Length: > 0 } title
            && title.Contains(placeName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (page.Provenance is { Length: > 0 } prov
            && prov.Contains(placeName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (string line in page.Lines ?? [])
        {
            if (line is { Length: > 0 } && line.Contains(placeName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
