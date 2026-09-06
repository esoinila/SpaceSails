namespace SpaceSails.Core;

/// <summary>
/// #1151 · <b>THE CLAIM IS THE SCENE.</b> Owner ruling, 2026-09-06, on #525: <i>"insurance does not know
/// automatically unless we die — deaths are registered and auto-processed, everything else is a claim to the
/// rep, gamified: automatic kiosks scattered through the system, contactable from the captain's remote in
/// range; and as the plot clears the less we want the insurance."</i>
///
/// <para><b>What this file is, and what it deliberately is not.</b> It is the RULE of a claim: who may lodge
/// one, what the desk asks for, which of the three things it asks for the captain actually has, when the desk
/// comes back as a memory, and where in the system a kiosk stands at all. It is not the money — the money is
/// <see cref="InsuranceRule.HullClaimPayoutCr"/>, quoted from the death path so a claim can never be worth a
/// number somebody typed here. And it is not the presence law — a writ that cannot proceed without the master
/// is <see cref="PendingWritPlate"/> and the client's own reading of where the captain is standing.</para>
///
/// <para><b>The prose is canon and there is no more of it.</b> Six strings were authored for this beat (the
/// plate, the two things the kiosk says, the rep's line at the payout, and the flashback card's title and
/// caption) and every one of them is below as a <c>const</c>. Nothing in this feature says anything else: a
/// refusal at the desk is told by the desk's own standing line — <i>"Present the policy"</i> — and by the row
/// not going away, because the alternative was writing three sentences nobody authored. Where a beat wants
/// one, the marker is in the code and says so.</para>
///
/// <para><b>Why the desk asks for three things.</b> The owner's note is that the collectors' process is
/// <i>super strict</i> and that they must prove a ship and a captain are one. The kiosk is the same process
/// with the captain on the other side of it: a policy that is in force (the wallet), a hull that is HERS and
/// not a name she used to answer to (the dossier — #397), and an entry on the wire that the loss happened at
/// all (#1141's headline). The third is the one with teeth: no wire entry, no claim, which is exactly why
/// #535's key — whose whole design is that <i>nothing is ever pushed, so there is nothing on the wire to
/// find</i> — also unhappens the claim, with no code here knowing the key exists.</para>
/// </summary>
public static class NebulaClaims
{
    // ══ THE CANON ════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The plate on the kiosk. Also the label of the card it raises, because a fixture and the card
    /// it opens wearing two different names is two fixtures to the player.</summary>
    public const string KioskPlate = "NEBULA MUTUAL · CLAIMS";

    /// <summary>What the kiosk says the moment the captain is standing at it — and goes on saying, through
    /// all three presses, which is why a refusal needs no line of its own: the desk has already told him what
    /// it wants and it has not stopped asking.</summary>
    public const string OnApproach =
        "Loss of hull, non-fatal. Deaths are processed automatically; everything else is a claim. "
        + "Present the policy.";

    /// <summary>What it says once the third press lands.</summary>
    public const string LodgedLine = "Claim lodged. A representative will find you. They always do.";

    /// <summary>What Harlan Fess says when he finds you — the clinic-bill idiom in reverse, and the only
    /// place in the game the third page is mentioned. He never says what is on it.</summary>
    public const string RepAtThePayout =
        "Your hull is a line item now. Sign here, and here, and try not to read the third page.";

    /// <summary>The plate a collector's ledger row wears while the process cannot proceed. The plate idiom
    /// (#1138's <c>SOMETHING · SOMETHING</c>), because what is being shown is a state of paperwork.</summary>
    public const string PendingWritPlate = "WRIT · AWAITING THE MASTER";

    /// <summary>The flashback card's stamp, from the second lodged claim onward.</summary>
    public const string DeskTitle = "THE CLAIM";

    /// <summary>The artist's canvas for it.</summary>
    public const string DeskArt = "art/claim-desk.jpg";

    /// <summary>The card's one line. It describes what is there and stops — nothing on it says what the third
    /// page says, and nothing ever will.</summary>
    public const string DeskCaption =
        "You remember the desk. You do not remember agreeing to the third page.";

    /// <summary>The three of them as one plate, so <c>StoryBeats</c> adopts them through the door #664 built
    /// rather than retyping the sentence into a second file.</summary>
    public static RevealPlate DeskPlate { get; } = new(DeskTitle, DeskArt, DeskCaption);

    // ══ THE THREE PRESSES ════════════════════════════════════════════════════════════════════════════════

    /// <summary>What the desk asks for, in the order it asks. The order is the owner's <i>super strict</i>: a
    /// policy first (there is no claim without one), then the hull the policy is over, then the proof that
    /// anything happened to it.</summary>
    public enum Press
    {
        /// <summary>The policy — an active <see cref="PirateInsurance"/>, and nothing else counts.</summary>
        Policy,

        /// <summary>The hull's name, off the dossier. A former name is refused.</summary>
        Hull,

        /// <summary>The loss's entry on the wire. No wire entry, no claim.</summary>
        Wire,
    }

    /// <summary>How many presses a lodging is.</summary>
    public const int Presses = 3;

    /// <summary>Which press is next after <paramref name="pressesTaken"/> have landed, or null when the claim
    /// is lodged. One function so the client cannot grow a second opinion about the order.</summary>
    public static Press? NextPress(int pressesTaken) => pressesTaken switch
    {
        0 => Press.Policy,
        1 => Press.Hull,
        2 => Press.Wire,
        _ => null,
    };

    /// <summary>Is the claim lodged — every press taken?</summary>
    public static bool IsLodged(int pressesTaken) => pressesTaken >= Presses;

    /// <summary>What the desk is saying right now: its standing demand until the last press lands, and then
    /// its receipt. Two authored sentences and no third.</summary>
    public static string DeskLine(int pressesTaken) => IsLodged(pressesTaken) ? LodgedLine : OnApproach;

    /// <summary>
    /// One thing the captain can put on the counter, as the desk offers it.
    /// </summary>
    /// <param name="Press">Which of the three the row belongs to.</param>
    /// <param name="Offer">The row's identity — the tier's name, a hull's name, a wire entry's subject. What
    /// the client hands back when the row is pressed.</param>
    /// <param name="Label">The words on the row, and they are never authored here: a policy row wears its
    /// tier's own name, a hull row wears the name off the dossier, a wire row wears
    /// <see cref="NewsWire.Headline"/> — the sentence the wire already wrote.</param>
    public readonly record struct Ask(Press Press, string Offer, string Label);

    /// <summary>
    /// THE POLICY PRESS. A lapsed policy reads as no policy at all, exactly as
    /// <see cref="InsuranceRule.ApplyToRebirth"/> reads it — asked through
    /// <see cref="PirateInsurance.IsActiveAt"/> rather than by looking at the tier, so a captain whose premium
    /// ran out last week cannot claim on the strength of having once bought one.
    /// </summary>
    public static bool ThePolicyIsPresentable(PirateInsurance policy, double simTime) =>
        policy.IsActiveAt(simTime);

    /// <summary>
    /// THE HULL PRESS. <paramref name="offered"/> is accepted only when it is the name she answers to now.
    ///
    /// <para>Ordinal and case-sensitive on purpose: the desk is not being helpful. And there is no separate
    /// "is this a former name" question, because there does not need to be one — a former name is refused for
    /// the same reason a name off a different hull is, which is that it is not hers. The former names are
    /// offered as ROWS (see <see cref="TheNamesOnFile"/>) so the refusal is a thing the captain can actually
    /// press, rather than a rule he can only be told about.</para>
    /// </summary>
    public static bool TheNameIsHers(string? offered, string trueName) =>
        !string.IsNullOrWhiteSpace(offered) && string.Equals(offered, trueName, StringComparison.Ordinal);

    /// <summary>
    /// The names the desk puts on the counter for the hull press: the one she answers to, then every name she
    /// has left behind, in the order the dossier keeps them.
    ///
    /// <para>Her true name first is not a convenience — a desk that buried the right answer among the wrong
    /// ones would be a puzzle, and this is not a puzzle. It is a thing you can get wrong in a hurry.</para>
    /// </summary>
    public static IReadOnlyList<Ask> TheNamesOnFile(string trueName, IReadOnlyList<string>? formerNames)
    {
        List<Ask> rows = [new Ask(Press.Hull, trueName, trueName)];
        if (formerNames is not null)
        {
            foreach (string was in formerNames)
            {
                if (!string.Equals(was, trueName, StringComparison.Ordinal))
                {
                    rows.Add(new Ask(Press.Hull, was, was));
                }
            }
        }

        return rows;
    }

    /// <summary>
    /// THE WIRE PRESS. Which headlines are a receipt for a loss of hull.
    ///
    /// <para>Two kinds today, and they are the two ways a hull leaves a living captain: she was blown at
    /// somebody's berth (#1141's <see cref="NewsWire.NewsEventKind.HullLostAtABerth"/>), or the contract that
    /// was flying at her gave up on a hull that had stopped existing
    /// (<see cref="NewsWire.NewsEventKind.HunterBrokeOff"/>). A death is on neither list and never will be:
    /// a death does not want a receipt, it is processed.</para>
    /// </summary>
    public static bool IsAReceipt(NewsWire.NewsEventKind kind) =>
        kind is NewsWire.NewsEventKind.HullLostAtABerth or NewsWire.NewsEventKind.HunterBrokeOff;

    // ══ THE UNEASE ═══════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// From which lodged claim the desk comes back. <b>Two</b> — the owner's <i>"a flashback image or two
    /// from the process"</i>, and the second claim is the first one that is a PATTERN. A card on the first
    /// would be the game telling the captain what to feel about a thing he has done once.
    /// </summary>
    public const int FlashbackFromClaim = 2;

    /// <summary>Does this lodging carry the flashback? <paramref name="claimsLodged"/> counts THIS one.</summary>
    public static bool TheDeskComesBack(int claimsLodged) => claimsLodged >= FlashbackFromClaim;

    // ══ WHERE A KIOSK STANDS ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Is there a claims kiosk at this berth?
    ///
    /// <para><b>Every great port, a dealt share of the working berths, and never an outpost</b> — the tier
    /// rule the arrival tube already draws the system with (<see cref="ArrivalTube.TierFor"/>), asked once
    /// here so the concourse builder and every guard read one answer. A great port processes people for a
    /// living and an insurer puts a machine where the traffic is; a working berth gets one if the company
    /// thought it was worth the wall, which is a thing the company decided once and not a thing that changes
    /// while you watch; and an outpost has no concourse to stand it in — the same reason
    /// <see cref="ArrivalTube.CustomsLine"/> answers null there.</para>
    ///
    /// <para>The deal is off the berth's own id and nothing else, so it is the same berth every session and
    /// on every machine, and it is <see cref="DiceRule.Seed(string, long[])"/> rather than a hash somebody
    /// wrote here — the repo has one dealer.</para>
    /// </summary>
    public static bool AKioskStands(ArrivalTube.Tier tier, string havenId)
    {
        ArgumentNullException.ThrowIfNull(havenId);

        return tier switch
        {
            ArrivalTube.Tier.GreatPort => true,
            ArrivalTube.Tier.WorkingBerth => DiceRule.Seed(havenId, DealSalt) % WorkingBerthsInN == 0,
            _ => false,
        };
    }

    /// <summary>One working berth in this many carries a machine. Two: enough that a captain learns a berth
    /// either has one or does not, few enough that it is worth remembering which.</summary>
    public const ulong WorkingBerthsInN = 2;

    /// <summary>The salt the deal is taken with. It is a number the deal needs and not a fact about the
    /// world, which is why it is here and not in a scenario.</summary>
    private const long DealSalt = 1151;
}
