using System;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #417 slice 1 · <b>THE CASE, AND HOW FAR DOWN IT THE CAPTAIN HAS GOT — as two opaque rows.</b>
///
/// <para>The house idiom for a book that rides the vault (<c>FilingLine.Page.Stored</c>,
/// <c>CaptainCrossings.Crossing.Stored</c>, <c>OldCrewSection.Shipmates</c>): the file carries the FACTS and
/// never the sentences, which are rebuilt from this file's own constants on every render. A row this build
/// cannot parse is dropped rather than thrown over, exactly as the filing line's and the satchel's are — a
/// case the captain cannot see is not worth a lost game.</para>
///
/// <h3>Why the CASE is stored at all, when it is a pure function of the thread</h3>
///
/// <para>Because it is a pure function of the thread <b>and of the traffic</b>, and the traffic is a wave the
/// scenario deals afresh. A case rebuilt from a different wave would move the hull, the herring and the berth
/// under a captain who is halfway down the trail — the game quietly changing its own story between two
/// sessions, which is the worst kind of save bug because nothing about it looks wrong. So the graph is
/// written down the moment it is taken, and the reload reads it back rather than re-deriving it.</para>
/// </summary>
public static partial class FinderCase
{
    /// <summary>What the stored fields are joined with: a control character the house never writes, so no
    /// port name, hull callsign or former name can spell a separator. The same character
    /// <see cref="CaseSubjects"/> joins its ids with, and for the same reason.</summary>
    private const char Separator = (char)0x1F;

    /// <summary>How many fields a stored case has. Named so the reader can refuse a row from a build that
    /// wrote a different shape rather than half-reading it.</summary>
    private const int StoredFields = 16;

    /// <summary>How many a stored progress has.</summary>
    private const int StoredProgressFields = 8;

    // ── HOW FAR DOWN THE TRAIL ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// What the captain has actually done about this case. Every field is a fact he could point at: a person
    /// he talked to, a paper he clipped, a record he read.
    /// </summary>
    /// <param name="Taken">He sat down and took it.</param>
    /// <param name="WitnessHeard">The roving regular has been found at their own port and has spoken.</param>
    /// <param name="PaperFound">A paper off the case's ground is in the field book under its subjects.</param>
    /// <param name="HullRead">The hull's dossier has been read, and the name she used to carry with it.</param>
    /// <param name="HerringCleared">The second hull's chain of custody has been read, and it cleared her.</param>
    /// <param name="Revealed">The reveal card has stood at the confrontation berth.</param>
    /// <param name="Settled">Which way the captain settled it, or <see cref="Outcome.Open"/>.</param>
    /// <param name="PaidOff">Varga has said her last line about it.</param>
    public readonly record struct Progress(
        bool Taken,
        bool WitnessHeard,
        bool PaperFound,
        bool HullRead,
        bool HerringCleared,
        bool Revealed,
        Outcome Settled,
        bool PaidOff)
    {
        /// <summary>Nothing done. What a captain who has never met her has.</summary>
        public static Progress Fresh => default;

        /// <summary>All three leads answered. The red herring is deliberately NOT one of them: clearing her
        /// is a door closing, and a captain who never looked at the second hull has still followed the
        /// trail — he simply never found out he could have gone wrong.</summary>
        public bool TrailWalked => WitnessHeard && PaperFound && HullRead;

        /// <summary>Is there anything here to write down at all?</summary>
        public bool HasHistory => Taken || WitnessHeard || PaperFound || HullRead
                                  || HerringCleared || Revealed || Settled != Outcome.Open || PaidOff;
    }

    // ── WRITING ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The case as one opaque row.</summary>
    public static string Stored(in Case c) => string.Join(Separator,
        c.ClientPortId, c.ClientPortName, c.WitnessId, c.WitnessPortId,
        c.PaperSiteBodyId, c.PaperSiteName, c.HullId, c.HullCallsign, c.FormerName,
        c.HerringHullId, c.HerringCallsign, c.BerthPortId,
        Figure(c.BerthSlot), Figure(c.PayCredits), Figure(c.PayReputation), Figure(c.BribeCredits));

    /// <summary>…and the progress as another. Flags as 0/1 and the outcome as its own figure, so a build that
    /// grows a third outcome reads an old row without inventing one.</summary>
    public static string Stored(in Progress p) => string.Join(Separator,
        Flag(p.Taken), Flag(p.WitnessHeard), Flag(p.PaperFound), Flag(p.HullRead),
        Flag(p.HerringCleared), Flag(p.Revealed), Figure((int)p.Settled), Flag(p.PaidOff));

    private static string Flag(bool set) => set ? "1" : "0";

    private static string Figure(int value) => value.ToString(CultureInfo.InvariantCulture);

    // ── READING ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Read a stored case back. False for anything this build cannot parse — a row of the wrong
    /// width, a blank id where a place has to be, a figure that is not one.</summary>
    public static bool TryRead(string? row, out Case c)
    {
        c = default;
        if (row is null)
        {
            return false;
        }

        string[] f = row.Split(Separator);
        if (f.Length != StoredFields)
        {
            return false;
        }

        if (!Figure(f[12], out int slot) || !Figure(f[13], out int pay)
            || !Figure(f[14], out int rep) || !Figure(f[15], out int bribe))
        {
            return false;
        }

        // The ids and the names a sentence is built out of. A case missing one of them would print an empty
        // brace at the player, which is worse than a case that never loaded.
        foreach (int at in (ReadOnlySpan<int>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11])
        {
            if (f[at].Length == 0)
            {
                return false;
            }
        }

        c = new Case(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11],
                     slot, pay, rep, bribe);
        return true;
    }

    /// <summary>…and a stored progress. Same tolerance; an outcome figure this build does not know falls to
    /// <see cref="Outcome.Open"/> rather than being thrown over, because a captain whose save says he
    /// settled it some way this build cannot name has still settled it and should not lose the case.</summary>
    public static bool TryRead(string? row, out Progress p)
    {
        p = Progress.Fresh;
        if (row is null)
        {
            return false;
        }

        string[] f = row.Split(Separator);
        if (f.Length != StoredProgressFields || !Figure(f[6], out int settled))
        {
            return false;
        }

        p = new Progress(
            Set(f[0]), Set(f[1]), Set(f[2]), Set(f[3]), Set(f[4]), Set(f[5]),
            Enum.IsDefined((Outcome)settled) ? (Outcome)settled : Outcome.Open,
            Set(f[7]));
        return true;
    }

    private static bool Set(string field) => field == "1";

    private static bool Figure(string field, out int value) =>
        int.TryParse(field, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
