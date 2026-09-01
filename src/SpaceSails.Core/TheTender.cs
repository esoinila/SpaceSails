namespace SpaceSails.Core;

/// <summary>
/// #1022 · <b>B-7V, "THE TENDER"</b> — every word the galley's bartender has, and the laws that choose
/// between them.
///
/// <para>Owner, live (2026-08-30), the founding idea: <i>"The dialog, one imagines, is a set of phrases to
/// keep the customer talking :-D ... but there is something heart warming in those scenes at the same
/// time."</i></para>
///
/// <para><b>That is the whole mechanic, and it is the whole character.</b> He is a set of phrases. Nothing
/// he says is generated, nothing he says is new, and everything he can say he has always said — so this file
/// is not a helper that builds sentences, it is a CATALOGUE with a picker on top. A pool per occasion
/// (opening the card, a pour, a second look), one line for the drink the set is obliged to advise against,
/// and one slot that comes round rarely. The warmth is not in the picker; it is in the lines, and they are
/// authored, verbatim, and never assembled.</para>
///
/// <para><b>Second-hand.</b> The counter, the rack and B-7V bolted to it were sold as one lot, used. Which
/// ship he came off is never stated — not here, not in a card, not by a sensor.</para>
///
/// <para><b>The flashback channel.</b> Rarely — <see cref="FlashbackFaces"/>, and never twice in one
/// <see cref="Sitting"/> — the register changes and he announces the evening of a grander room than this
/// one. It is followed by <see cref="Recovery"/>, always: the two lines are ONE <see cref="Line"/> object,
/// so there is no code path anywhere that can say the first without the second.</para>
///
/// <para><b>Where the picking law lives.</b> #1006's shape, one door along: the index is a salted roll on
/// the shared <see cref="DiceRule"/> (never a private random — determinism is law in Core), and a
/// <see cref="Sitting"/> carries the small memory that walks a collision forward, so consecutive beats do
/// not read the same card while the pool lasts. When the pool runs out the wheel turns over and the next
/// beat draws from all of it again — a repeat is then ALLOWED but never FORCED.</para>
/// </summary>
public static class TheTender
{
    /// <summary>The speaker label the card prints above his line. The crew just says "the tender".</summary>
    public const string Plate = "🤖 B-7V · THE TENDER";

    /// <summary>What he says when the card comes up and he has not greeted you yet this sitting.</summary>
    public static readonly IReadOnlyList<string> Openers =
    [
        "Hell of a day out there, captain?",
        "Welcome back, sir. The stool remembers you.",
        "Quiet shift, captain. I kept your glass where you left it.",
    ];

    /// <summary>The low-frequency member of the opener rotation — <see cref="RareOpenerFaces"/>, so most
    /// sittings never hear it.</summary>
    public const string RareOpener = "Welcome aboard the White Night, sir. — Forgive me. Welcome home.";

    /// <summary>What he says over a pour, while the set still approves of pouring.</summary>
    public static readonly IReadOnlyList<string> Pours =
    [
        "The usual, then. Everything here is the usual.",
        "One tot, the liner flourish. No extra charge for the flourish. There has never been a charge.",
        "The glass holds still even when she doesn't.",
    ];

    /// <summary>The one line the set is obliged to say at the threshold, and the one exception it makes.
    /// It REPLACES the pour line — see <see cref="Sitting.Pour"/> for which pour that is.</summary>
    public const string LastCall =
        "The set advises: last one, captain. The set has always advised that. "
        + "You have always been my favourite exception.";

    /// <summary>A second look in the same sitting. Keeping the customer talking is the entire job.</summary>
    public static readonly IReadOnlyList<string> Idles =
    [
        "I am told I am a good listener. It is the only thing I am told.",
        "Do not mind the polishing. The glass is clean. The polishing is for me.",
        "Take your time, sir. The night is on rails and we are ahead of schedule.",
        "This counter and I were sold as one lot, sir. The invoice says salvage. The invoice is being polite.",
    ];

    /// <summary>The announcements. A different register — public-address formal — and a different room: a
    /// larger one, elsewhere, some time ago. The card draws these apart from his ordinary line, and every
    /// one of them is followed by <see cref="Recovery"/>.</summary>
    public static readonly IReadOnlyList<string> Announcements =
    [
        "Ladies and gentlemen, the Aurora Deck opens at eight bells. The Minister's table is set.",
        "Tonight's seating honours our Founding Donors. Kindly have your second card ready.",
        "The observation lounge reminds guests: the pods pass at midnight, and they are perfectly safe.",
        "Guests holding premium cover dine with the captain tonight. Premium remembers.",
        "The white night lasts until we say otherwise, ladies and gentlemen. Dance.",
    ];

    /// <summary>What he says after an announcement. Always — there is no announcement without it, which is
    /// why the pair is one <see cref="Line"/> and not two calls a caller could get wrong.</summary>
    public const string Recovery = "…Forgive me, captain. For a moment the room was larger.";

    /// <summary>The die the rare opener is drawn on: face 1 of a d-<see cref="RareOpenerFaces"/> reaches the
    /// slot, so seven greetings in eight are the ordinary three.</summary>
    public const int RareOpenerFaces = 8;

    /// <summary>The die the flashback is drawn on: face 1 of a d-<see cref="FlashbackFaces"/>, and at most
    /// once per <see cref="Sitting"/> however many beats a sitting runs to. Rare on purpose — a channel that
    /// opened every visit would be a feature, and this is a thing that happens to him.</summary>
    public const int FlashbackFaces = 12;

    /// <summary>
    /// One beat of him: what the card shows now.
    ///
    /// <para><see cref="Announcement"/> is null on an ordinary beat and <see cref="Text"/> is simply what he
    /// said. On a flashback the announcement is the line in the other register and <see cref="Text"/> is
    /// <see cref="Recovery"/> — the pairing is structural, so "an announcement with no recovery under it" is
    /// not a state this type can hold.</para>
    /// </summary>
    /// <param name="Announcement">The public-address line, or null on an ordinary beat.</param>
    /// <param name="Text">What he says in his own voice — the recovery, when there is an announcement.</param>
    public readonly record struct Line(string? Announcement, string Text)
    {
        /// <summary>Whether this beat is one of the rare ones. True exactly when there is an announcement to
        /// draw above the line.</summary>
        public bool IsFlashback => Announcement is not null;
    }

    /// <summary>Every authored word, for the canon grep — the plate, all four pools, both singles. The guard
    /// walks THIS, so a line added tomorrow is checked tomorrow.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Plate;
        foreach (string line in Openers)
        {
            yield return line;
        }

        yield return RareOpener;
        foreach (string line in Pours)
        {
            yield return line;
        }

        yield return LastCall;
        foreach (string line in Idles)
        {
            yield return line;
        }

        foreach (string line in Announcements)
        {
            yield return line;
        }

        yield return Recovery;
    }

    /// <summary>
    /// ONE SITTING at the counter — the small memory that keeps him from saying the same thing twice in a
    /// row, and the flag that keeps the rare thing rare.
    ///
    /// <para>A sitting is not one card-open: the captain shuts the card and comes back, and B-7V does not
    /// greet him again for it — he says something else, which is the idle pool and the whole point of a set
    /// of phrases whose job is to keep the customer talking. The client starts a fresh sitting when the gap
    /// since the last beat is long enough that the rum ledger would also have started a fresh spree
    /// (<see cref="NerveModel.SpreeGapMs"/>): one visit to the counter, one sitting, one tot count.</para>
    /// </summary>
    public sealed class Sitting
    {
        private readonly HashSet<int> _openersSaid = [];
        private readonly HashSet<int> _idlesSaid = [];
        private readonly HashSet<int> _poursSaid = [];
        private bool _greeted;

        /// <summary>Whether he has greeted the captain yet this sitting. False until an
        /// <see cref="Open"/> actually returns a greeting — a first open that came up a flashback has not
        /// greeted anybody, so the next one still does.</summary>
        public bool Greeted => _greeted;

        /// <summary>Whether the rare beat has already happened this sitting. Once true it stays true: the
        /// room is larger once, and then it is this room again for the rest of the visit.</summary>
        public bool FlashbackSpent { get; private set; }

        /// <summary>
        /// The card came up. A greeting the first time he actually gets to give one, an idle line every time
        /// after that — and, rarely, neither.
        /// </summary>
        /// <param name="simSeconds">The moment, for the seed. Sim state only; never a wall clock.</param>
        /// <param name="beat">Which beat of this sitting this is — the salt that moves the pick on. Two
        /// beats at the same sim-second are still independent draws.</param>
        /// <param name="forceFlashback">The dev cheat, and it forces the ROLL and never the content: which
        /// announcement he reaches for is still his own salted pick, and the once-a-sitting law still
        /// holds.</param>
        public Line Open(long simSeconds, int beat, bool forceFlashback = false)
        {
            if (TryFlashback(simSeconds, beat, forceFlashback, out Line flashback))
            {
                return flashback;
            }

            if (!_greeted)
            {
                _greeted = true;
                return new Line(null, Greeting(simSeconds, beat));
            }

            return new Line(null, Pick(Idles, _idlesSaid, DiceRule.Seed("tender:idle", simSeconds, beat)));
        }

        /// <summary>
        /// A tot went in the glass.
        ///
        /// <para><b>Which pour is the threshold pour.</b> The shipped drink law counts the tot as it is
        /// poured and asks <see cref="NerveModel.DrunkAt"/> of the NEW count — the third tot is the one that
        /// makes the deck tilty and the one whose restore is already zero. So <paramref name="totNumber"/>
        /// here is the tot just poured, and the pour that CROSSES the threshold is the pour the set advises
        /// against: that is the reading the shipped law supports, and taking the other one would have B-7V
        /// warning about a drink the game had already stopped counting as help.</para>
        ///
        /// <para>The threshold wins over the rare roll. At the threshold the set has one thing to say and
        /// says it, so no flashback is rolled on that pour and none is spent — the room can still get larger
        /// later in the sitting.</para>
        /// </summary>
        /// <param name="simSeconds">The moment, for the seed.</param>
        /// <param name="beat">Which beat of this sitting this is.</param>
        /// <param name="totNumber">The tot count AFTER this pour was counted.</param>
        /// <param name="forceFlashback">The dev cheat — see <see cref="Open"/>.</param>
        public Line Pour(long simSeconds, int beat, int totNumber, bool forceFlashback = false)
        {
            if (NerveModel.DrunkAt(totNumber))
            {
                return new Line(null, LastCall);
            }

            if (TryFlashback(simSeconds, beat, forceFlashback, out Line flashback))
            {
                return flashback;
            }

            return new Line(null, Pick(Pours, _poursSaid, DiceRule.Seed("tender:pour", simSeconds, beat)));
        }

        /// <summary>The greeting: the rare slot on its own low die, the ordinary three on the rotation.</summary>
        private string Greeting(long simSeconds, int beat)
        {
            ulong slot = DiceRule.Seed("tender:rare-slot", simSeconds, beat);
            if (DiceRule.Roll(slot, RareOpenerFaces).Face == 1)
            {
                return RareOpener;
            }

            return Pick(Openers, _openersSaid, DiceRule.Seed("tender:opener", simSeconds, beat));
        }

        /// <summary>Roll for the other room, and pair the announcement with the recovery on the way out.</summary>
        private bool TryFlashback(long simSeconds, int beat, bool force, out Line line)
        {
            line = default;
            if (FlashbackSpent)
            {
                return false;
            }

            ulong seed = DiceRule.Seed("tender:flashback", simSeconds, beat);
            if (!force && DiceRule.Roll(seed, FlashbackFaces).Face != 1)
            {
                return false;
            }

            FlashbackSpent = true;
            int i = (int)(DiceRule.Seed("tender:announcement", simSeconds, beat) % (ulong)Announcements.Count);
            line = new Line(Announcements[i], Recovery);
            return true;
        }

        /// <summary>#1006's pick, one door along: a salted index, walked forward past anything already said
        /// this sitting while unspoken lines remain. When the sitting outruns the pool the wheel turns over
        /// and this beat's own salted pick decides — so a repeat becomes possible, never forced.</summary>
        private static string Pick(IReadOnlyList<string> pool, HashSet<int> said, ulong seed)
        {
            int i = (int)(seed % (ulong)pool.Count);

            if (said.Count >= pool.Count)
            {
                said.Clear();
            }

            for (int step = 0; step < pool.Count && said.Contains(i); step++)
            {
                i = (i + 1) % pool.Count;
            }

            said.Add(i);
            return pool[i];
        }
    }
}
