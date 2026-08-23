using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #973 L2 · HARLAN FESS, NEBULA MUTUAL, OUTER REACHES DESK — the recurring salesman, and the cheapest NPC in
/// the game to make MOVE.
///
/// <para>Owner, on the arc-2 write-up: <i>"the salesmen are a low-stakes place to practise NPC interaction
/// (they move about, they try to sell, nothing is at stake)."</i> So this is the practice ground for the four
/// verbs the old crew (#973 L5) will later use with feeling — <b>approach, address, withdraw, and remember you
/// said no</b> — and every one of them is decided here, in Core, as a pure function, so the client only has to
/// walk a body to a table and render a card.</para>
///
/// <h3>The joke, and why it is a rule rather than a script</h3>
///
/// <para>He never recognises the captain's face, because after a rebirth there is a new one
/// (<see cref="CaptainSuccession"/>) and he was never looking at faces anyway. He always has the NAME on file,
/// because the file is the only continuous thing in the whole arrangement — which is
/// <c>docs/features/NebulaArc.md</c> §2 stated as a sales manner instead of as a secret. And the
/// remember-you-said-no is <b>per visit</b>: next time you dock he starts over, and that is the whole gag.</para>
///
/// <h3>What is NOT here</h3>
///
/// <para>The <c>adjuster-tell</c> fragment stays exactly where it is (<see cref="NebulaLore"/>,
/// <see cref="NebulaSource.Adjuster"/>). <b>The adjuster and the rep are two people.</b> Fess sells; the
/// adjuster assesses, and the adjuster is the one who says the quiet thing over a drink. Merging them would
/// cost the arc its only sober witness.</para>
/// </summary>
public static class NebulaRep
{
    /// <summary>His id in the <see cref="ContactLedger"/> — goodwill, transactions and history accrue against
    /// this exactly as they do for a fence or a fixer. He is a relationship, not a vending machine.</summary>
    public const string ContactId = "nebula-rep-fess";

    /// <summary>How the book names him.</summary>
    public const string DisplayName = "Harlan Fess · Nebula Mutual";

    /// <summary>What he calls himself, mid-sentence, every single time, as though you had not met.</summary>
    public const string RepName = "Harlan Fess";

    /// <summary>The desk he works out of, for the contact sheet and the card's second line.</summary>
    public const string Desk = "Nebula Mutual · Outer Reaches desk";

    /// <summary>
    /// #973 L2 · The memory the rep hands back — the subject the signing flashback is raised under.
    ///
    /// <para>L1's flashback subjects are LEDGER ENTRY ids, because in the book a flashback is always about
    /// one page. His is not a page in the book at all: it is the day the policy was signed, which is the one
    /// memory that predates every entry the ledger has. So it gets a name of its own in the same namespace,
    /// and the plate is the same bleached painting every flashback wears.</para>
    /// </summary>
    public const string SigningMemoryId = "signing";

    /// <summary>How many of him are ever on a floor. One, and it is a constant rather than a literal so the
    /// figure buffer that has to hold him and the rule that puts him there cannot disagree.</summary>
    public const int OnTheFloorAtOnce = 1;

    /// <summary>What the deck calls the figure crossing the floor — the house's <c>◈</c> plate idiom, the
    /// same one the haulier who comes to your table wears.</summary>
    public const string Plate = "◈ HARLAN FESS · NEBULA MUTUAL";

    /// <summary>The portrait at the top of his card. The one portrait arc 2 allows, and the arc's own
    /// inversion: the adjuster is framed from across the room and denied portrait dignity precisely so the
    /// SALESMAN can have it.</summary>
    public const string PortraitArt = "art/nebula-rep-fess.jpg";

    /// <summary>The receipt a sale leaves in the ship's ledger. Engine voice, not his — a receipt is a
    /// number and a date, and the only sentence he ever gets is the one on his card.</summary>
    public static string SaleLedgerNote(InsuranceTier tier, int priceCr, string captainName) =>
        $"🧾 NEBULA MUTUAL — {tier} policy taken out with {RepName}, {priceCr} cr, "
        + $"on the file under Capt. {captainName}.";

    // ── Presence: at most one station in three, never two in a row ─────────────────────────────────────

    /// <summary>The rota's period. He is on the road for one dock visit in <see cref="RotaPeriod"/> at the
    /// most — which is also, and not by coincidence, what makes two consecutive visits impossible: the
    /// eligible visits are three apart by construction, so "never twice in a row" is arithmetic rather than a
    /// flag somebody has to remember to clear.</summary>
    public const int RotaPeriod = 3;

    /// <summary>
    /// Is Fess working THIS station on THIS visit? Deterministic in the thread, the station and the running
    /// count of dock visits — no clock, no <c>Random</c>, so a reloaded save meets the same man in the same
    /// concourse.
    ///
    /// <para>Two gates, and the order of them is the design. The ROTA gate is a function of the visit index
    /// alone, which is what buys the owner's two cadence rules at once: at most one visit in three, and never
    /// two visits running (a station can only ever REMOVE him from an eligible visit, never add him to an
    /// ineligible one). The DESK gate is where the station id enters — a quarter of his eligible calls are
    /// somewhere else, so a captain who docks the same port every third visit does not get a man who is
    /// always, reliably, there.</para>
    /// </summary>
    /// <param name="threadId">The game thread's id — the per-universe seed (<c>GameThreadInfo.Id</c>).</param>
    /// <param name="stationId">The dockable station being visited.</param>
    /// <param name="visitIndex">How many dock visits this thread has made, counting from zero.</param>
    public static bool IsWorkingThisStation(string threadId, string stationId, int visitIndex)
    {
        if (visitIndex < 0)
        {
            return false;
        }

        return OnTheRota(threadId, visitIndex) && TheDeskSendsHim(threadId, stationId, visitIndex);
    }

    /// <summary>The visit-index half of <see cref="IsWorkingThisStation"/>: every third visit, offset by a
    /// per-thread slot so two universes do not put him on the same watch.</summary>
    private static bool OnTheRota(string threadId, int visitIndex)
    {
        int slot = (int)(DiceRule.Seed(0UL, $"nebula-rep-rota:{threadId}") % (ulong)RotaPeriod);
        return Mod(visitIndex - slot, RotaPeriod) == 0;
    }

    /// <summary>The station half: three eligible calls in four are actually made. Keeps the rota from
    /// becoming a timetable the player can set a watch by.</summary>
    private static bool TheDeskSendsHim(string threadId, string stationId, int visitIndex) =>
        DiceRule.Roll(DiceRule.Seed($"nebula-rep-desk:{threadId}|{stationId}", visitIndex), 4).Face != 1;

    /// <summary>Non-negative remainder — C#'s <c>%</c> keeps the sign of the dividend, and a negative visit
    /// offset would put him on a watch that does not exist.</summary>
    private static int Mod(int value, int modulus)
    {
        int r = value % modulus;
        return r < 0 ? r + modulus : r;
    }

    // ── The prices ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// What Basic costs. <b>Quoted from <see cref="InsuranceRule"/>, not invented</b>: Basic's whole cover is
    /// that it halves the clinic bill (<see cref="InsuranceRule.ApplyToRebirth"/>), so its premium is exactly
    /// the half-bill it saves. #227's vendor lane owns the real pricing when it lands and this is the seam it
    /// re-prices, not a number it has to hunt for.
    /// </summary>
    public const int BasicPremiumCr = InsuranceRule.BaseClinicBillCr / 2;

    /// <summary>What Premium costs — again quoted rather than invented: Premium waives the whole clinic bill
    /// and hands a mid-grade hull, so its premium is the whole bill.</summary>
    public const int PremiumPremiumCr = InsuranceRule.BaseClinicBillCr;

    /// <summary>How long a premium buys. FLAGGED for the owner's tuning: nothing in the tree priced a TERM
    /// before this lane, and a policy with no end date is not a policy you can lapse — and lapsing is the
    /// whole of what the arc's cold archive holds over you.</summary>
    public const double PremiumTermSeconds = 30 * 24 * 3600.0;

    /// <summary>The premium for a tier, or zero for the two moves that cost nothing.</summary>
    public static int PremiumFor(InsuranceTier tier) => tier switch
    {
        InsuranceTier.Basic => BasicPremiumCr,
        InsuranceTier.Premium => PremiumPremiumCr,
        _ => 0,
    };

    /// <summary>The policy a purchase leaves behind: the tier bought, paid through
    /// <see cref="PremiumTermSeconds"/> from now. One function so the client cannot invent a second way of
    /// setting <see cref="PirateInsurance.PremiumPaidThroughSimTime"/>, and so #227's vendor and this rep
    /// always agree about what a sale IS.</summary>
    public static PirateInsurance PolicyAfterBuying(InsuranceTier tier, double simTime) =>
        tier == InsuranceTier.None
            ? PirateInsurance.Uninsured
            : new PirateInsurance(tier, simTime + PremiumTermSeconds);

    // ── The pitch ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What the captain can press on his card. Not strings: the client renders
    /// <see cref="RepOffer.Label"/> and calls back with the move, so a button's words and a button's meaning
    /// cannot drift.</summary>
    public enum RepMove
    {
        /// <summary>Buy Basic at <see cref="BasicPremiumCr"/>.</summary>
        BuyBasic,

        /// <summary>Buy — or upgrade to — Premium at <see cref="PremiumPremiumCr"/>.</summary>
        BuyPremium,

        /// <summary>The captain's line, always available. Sometimes true.</summary>
        AlreadyHaveAPolicy,

        /// <summary>No. He withdraws to the bar and does not come back THIS visit.</summary>
        NotToday,

        /// <summary>Nothing to sell, nothing to refuse — the Premium captain's way out.</summary>
        GoodDay,

        /// <summary>Only ever offered when he has just used the previous captain's name.</summary>
        ThatsNotMyName,
    }

    /// <summary>One button on the rep's card: what it does, what it says, and what it costs (zero when it
    /// costs nothing).</summary>
    public readonly record struct RepOffer(RepMove Move, string Label, int PriceCr);

    /// <summary>His whole turn at your table: the line he says and the ways out of it.</summary>
    public readonly record struct RepPitch(string Line, IReadOnlyList<RepOffer> Offers);

    /// <summary>
    /// The pitch for the policy you hold, addressed to the name he has on file.
    ///
    /// <para>Total over <see cref="InsuranceTier"/> by construction — every tier has a line, because the one
    /// thing a salesman is never allowed to be is speechless. And the Premium captain is offered no sale at
    /// all: he has nothing left to sell and says so, which is the only moment he is likeable.</para>
    /// </summary>
    /// <param name="tier">The policy the captain actually holds.</param>
    /// <param name="nameOnFile">The name he reads off the file. Normally the captain's; on
    /// <see cref="BleedsThePreviousName">the bleed</see>, a dead one's.</param>
    /// <param name="bleeding">Whether this is a bleed — adds the one button that only exists then.</param>
    public static RepPitch PitchFor(InsuranceTier tier, string nameOnFile, bool bleeding = false)
    {
        string name = string.IsNullOrWhiteSpace(nameOnFile) ? "" : nameOnFile.Trim();
        List<RepOffer> offers = [];
        string line;

        switch (tier)
        {
            case InsuranceTier.Premium:
                line = $"Captain {name} — Premium. Nothing to sell you, then. I only stopped to say the file "
                     + "is in order. It is a comfort, isn't it, a file in order.";
                offers.Add(new RepOffer(RepMove.AlreadyHaveAPolicy, AlreadyHaveAPolicyLabel, 0));
                offers.Add(new RepOffer(RepMove.GoodDay, "Good day", 0));
                break;

            case InsuranceTier.Basic:
                line = $"Captain {name}. Basic, still? Basic brings you back; Premium brings you back "
                     + "remembering your mother's face. I'll say no more. Upgrade?";
                offers.Add(new RepOffer(RepMove.BuyPremium, $"Premium · {PremiumPremiumCr} cr", PremiumPremiumCr));
                offers.Add(new RepOffer(RepMove.AlreadyHaveAPolicy, AlreadyHaveAPolicyLabel, 0));
                offers.Add(new RepOffer(RepMove.NotToday, NotTodayLabel, 0));
                break;

            default:
                line = $"Captain {name}! Harlan Fess, Nebula Mutual. Don't tell me — uninsured. I can see it "
                     + "in the walk. One premium and the void stops being the end of the conversation. We "
                     + "Bring You Back Meaner — and, between us, with your own face. Shall I put you on the file?";
                offers.Add(new RepOffer(RepMove.BuyBasic, $"Put me on the file — Basic · {BasicPremiumCr} cr", BasicPremiumCr));
                offers.Add(new RepOffer(RepMove.BuyPremium, $"Premium · {PremiumPremiumCr} cr", PremiumPremiumCr));
                offers.Add(new RepOffer(RepMove.AlreadyHaveAPolicy, AlreadyHaveAPolicyLabel, 0));
                offers.Add(new RepOffer(RepMove.NotToday, NotTodayLabel, 0));
                break;
        }

        if (bleeding)
        {
            offers.Add(new RepOffer(RepMove.ThatsNotMyName, "That's not my name", 0));
        }

        return new RepPitch(line, offers);
    }

    /// <summary>The captain's own line, on every card he ever raises — because it is available whether or not
    /// it is true. An uninsured captain saying it is lying to a man holding the file, which is a crossing the
    /// later lanes get to pick up; nothing here books it.</summary>
    public const string AlreadyHaveAPolicyLabel = "I already have a policy";

    /// <summary>The refusal.</summary>
    public const string NotTodayLabel = "Not today";

    /// <summary>
    /// His answer to the captain's line. The first life he is reassuring about a file; after the captain has
    /// been reborn he says a warmer version of the same thing to a face he has never seen, and does not
    /// remark on the face, because he is not looking at it.
    /// </summary>
    /// <param name="retiredCaptains">How many captains this thread has buried (<c>GameThreadInfo.Retired</c>).</param>
    public static string PolicyClaimReply(int retiredCaptains) =>
        retiredCaptains > 0
            ? "Of course you do. It's on the file. Same name — I never forget a file."
            : "Of course you do, captain — it's on the file. I only ask because the file is such a small thing to lose.";

    /// <summary>What he says as he goes, once told no. He means it about the bar and he means it about the void.</summary>
    public const string WithdrawLine = "I'll be at the bar if the void changes your mind. It usually does.";

    /// <summary>What he says if the captain walks past him again in the same visit — the remembering that
    /// only lasts as long as the concourse does.</summary>
    public const string PassingLine = "No, captain — you said. I'm only passing.";

    // ── The bleed ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Meetings that are safe from the bleed. Owner's cadence: not in the first three — the joke
    /// only lands on a man you have already learned to expect.</summary>
    public const int MeetingsBeforeTheBleed = 3;

    /// <summary>
    /// Once in a long while, and only after somebody has died, he reads the wrong line off the file and
    /// greets the captain by the PREVIOUS captain's name.
    ///
    /// <para>Never in the first three meetings, and never twice running. The second of those is enforced by
    /// asking the SAME roll about the previous meeting rather than by keeping a flag: a flag is a thing a
    /// reload can lose, and this must survive one. It is deliberately conservative — a meeting whose
    /// predecessor rolled a bleed is suppressed even if that predecessor was itself blocked by the
    /// retired-captain gate — because "never twice in a row" is a promise and a suppressed rarity costs
    /// nothing.</para>
    /// </summary>
    /// <param name="threadId">The game thread's id.</param>
    /// <param name="meetingIndex">Which meeting with Fess this is, counting from one.</param>
    /// <param name="retiredCaptains">How many captains this thread has buried. Zero means there is no
    /// previous name to read, so there is no bleed.</param>
    public static bool BleedsThePreviousName(string threadId, int meetingIndex, int retiredCaptains) =>
        retiredCaptains >= 1
        && TheRollBleeds(threadId, meetingIndex)
        && !TheRollBleeds(threadId, meetingIndex - 1);

    /// <summary>The raw rarity roll on the shared dice, before the never-twice-running rule. One in twelve,
    /// and never inside <see cref="MeetingsBeforeTheBleed"/>.</summary>
    private static bool TheRollBleeds(string threadId, int meetingIndex) =>
        meetingIndex > MeetingsBeforeTheBleed
        && DiceRule.Roll(DiceRule.Seed($"nebula-rep-bleed:{threadId}", meetingIndex), 12).Face == 1;

    /// <summary>What he says when the captain tells him it is not their name. There is never any more than
    /// this — no explanation, no correction to the file, and no second thought.</summary>
    public const string BleedApology = "Did I? Force of habit. The file's a long one.";

    // FABLE: line needed — the ship's-ledger note for the bleed. One line, engine voice, filed so the black
    // book (#973 L3) can find it later; it must record WHICH dead captain's name was used and that Fess said
    // it, and it must not explain anything. Placeholder below.
    /// <summary>The one line the bleed leaves in the ship's ledger, so the black book can find it later.</summary>
    public static string BleedLedgerNote(string wrongName, string rightName) =>
        $"▓ NEBULA MUTUAL — Harlan Fess read the file and called you Captain {wrongName}. You are Captain "
        + $"{rightName}. \"{BleedApology}\"";
}

/// <summary>
/// #973 L2 · REMEMBER-YOU-SAID-NO, AND ONLY UNTIL THE DOORS SHUT. The whole of the rep's memory, as a value:
/// which dock visit he is remembering, and whether he was told no during it.
///
/// <para>Per VISIT and not per captain, because that is the joke — the next time you dock he starts over,
/// delighted, with your name on the file and no idea he has ever met you. Modelled as a
/// <c>readonly record struct</c> rather than two fields on the page so the law is testable without a
/// browser, and so the old crew (#973 L5) inherit an approach memory that already knows when to forget.</para>
/// </summary>
/// <param name="VisitIndex">The dock visit this memory is about; <c>-1</c> before the first one.</param>
/// <param name="ToldNo">Whether the captain has already refused him during that visit.</param>
public readonly record struct NebulaRepVisit(int VisitIndex, bool ToldNo)
{
    /// <summary>Before any concourse: he remembers nothing, because there is nothing yet.</summary>
    public static readonly NebulaRepVisit Fresh = new(-1, false);

    /// <summary>Roll the memory onto <paramref name="visitIndex"/>. A NEW visit wipes it; the same visit
    /// keeps it. This is the one place forgetting happens, so it cannot happen by accident anywhere else.</summary>
    public NebulaRepVisit AtVisit(int visitIndex) =>
        VisitIndex == visitIndex ? this : new NebulaRepVisit(visitIndex, false);

    /// <summary>The captain said no. He withdraws to the bar.</summary>
    public NebulaRepVisit WithNo() => this with { ToldNo = true };

    /// <summary>May he cross the floor and stand at the table? Only if he has not already been told no
    /// during THIS visit.</summary>
    public bool MayApproach(int visitIndex) => !(VisitIndex == visitIndex && ToldNo);
}
