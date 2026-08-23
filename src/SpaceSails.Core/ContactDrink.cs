namespace SpaceSails.Core;

/// <summary>Which way a shared drink went (#306). A drink with a contact is a two-edged trust
/// maneuver: it always warms the relationship, but the SAME dice decide whether they open up to
/// YOU (intel, or business once trust is deep) or whether YOU slip a tell to THEM.</summary>
public enum DrinkOutcome
{
    /// <summary>Bad roll — holding two realities steady failed and you leaked a tell. Trust still rose
    /// (you drank together), but the contact now knows something of yours (see <c>KnownTells</c>).</summary>
    Slip,

    /// <summary>The common middle — a warm exchange, goodwill up, but nothing concrete changes hands.</summary>
    Warm,

    /// <summary>Good roll — the contact opens up: a rumor becomes real, actionable intel.</summary>
    OpensUp,

    /// <summary>Good roll AND trust already deep (goodwill ≥ <see cref="ContactDrink.TrustForBusiness"/>):
    /// a business door opens — a contract offered, a price cut (#224 favor-bank direction).</summary>
    BusinessUnlock,
}

/// <summary>
/// PR-306 · A drink shared is a lock opened (owner ruling 2026-07-18): "people who will not have a
/// drink with you are treated as suspicious… having a drink at a bar with somebody is a sign of trust
/// and should open up new business opportunities, or give access to information. Of course we might
/// slip information… Keeping two realities in one's mind at the same time [is] a lot." The dice model
/// exactly that — the same roll that can make a contact open up can make you the one who slips.
///
/// <para>Pure Core resolver. It rolls on the ONE shared engine (<see cref="DiceRule"/>, owner ruling
/// §5.0 "the dice are the engine"), never a private random source — the tabletop <b>2D6</b>: two d6
/// off the shared rule, salted apart, summed to a 2..12 total, exactly the pattern the Reever
/// watchdogs use (#295/#303). Modifiers are NAMED so the UI can show the math: warmth already earned
/// nudges the total UP (an old friend relaxes with you), while carrying something to hide nudges it
/// DOWN (two realities are hard to hold). Fully deterministic — client and any future server agree on
/// every face.</para>
///
/// <para>TODO(#305): surface the roll through the shared dice tray once that lane merges; today the
/// caller shows <see cref="DrinkParley.Describe"/> on the receipt/pulse line.</para>
/// </summary>
public static class ContactDrink
{
    /// <summary>The 2D6 die: a d6, rolled twice.</summary>
    public const int Faces = 6;

    /// <summary>Goodwill a shared drink is worth — deliberately more than the #283 "round for the room"
    /// per-head nudge (+1), because drinking WITH a named contact is the stronger trust maneuver. Booked
    /// on every outcome, even a slip: the drink was shared, so the relationship warms regardless.</summary>
    public const int GoodwillPerDrink = 3;

    /// <summary>What refusing a contact's drink costs in goodwill (#306 item 3: the choice is never
    /// free — a refused glass reads as suspicion). Applied as a debit of this magnitude.</summary>
    public const int RefusalDebit = 2;

    /// <summary>Goodwill at which a contact relaxes enough that good rolls come easier (+1 "old friends").
    /// Below business trust, but past a cold first meeting.</summary>
    public const int WarmThreshold = 3;

    /// <summary>Goodwill at which a good roll unlocks BUSINESS rather than mere intel (#224 favor-bank
    /// direction — trust deep enough to trade on).</summary>
    public const int TrustForBusiness = 5;

    // ── #973 L5a · THE THREE THE OLD CREW ADD ────────────────────────────────────────────────────────
    //
    // Named, like every modifier on this roll, because the whole point of the dice homage is that the
    // player watches the numbers add up (and the UI prints them through `Describe`). One helps and two
    // hurt, which is the shape the owner asked for: an old friend relaxes with you, and the two people
    // you least want in the room while you relax are the one who signed and the one you did not marry.

    /// <summary>What a shared past is worth over a glass: an old shipmate is easier to talk to than a
    /// contact you did a job for, and they were easier to talk to before any of this. FLAGGED.</summary>
    public const int SharedHistoryBonus = +1;

    /// <summary>What it costs to drink with anybody while the man who signed the manifest is at the same
    /// venue. He is not listening; he does not have to be. FLAGGED.</summary>
    public const int SignerInTheRoomPenalty = -1;

    /// <summary>…and what it costs when she is. The same roll that opens the friend is the roll where you
    /// slip, and this is the thumb on it. FLAGGED.</summary>
    public const int FlingInTheRoomPenalty = -1;

    /// <summary>The three labels, written once so the roll, the UI math and the guards read one string.</summary>
    public const string SharedHistoryLabel = "shared history";

    /// <summary>The signer's label.</summary>
    public const string SignerInTheRoomLabel = "the signer is in the room";

    /// <summary>The fling's label.</summary>
    public const string FlingInTheRoomLabel = "the fling is in the room";

    /// <summary>#973 L5a · Who else is standing about while the glass is poured, and whether the person
    /// across the table served with the captain. Passed as one value rather than three booleans on every
    /// call so the two rolls (the offer and the drink) can never be handed different rooms.</summary>
    /// <param name="SharedHistory">The contact knew the old face (<c>ContactHistory.KnewTheOldFace</c>).</param>
    /// <param name="SignerPresent">Corwin Sallis is at this venue.</param>
    /// <param name="FlingPresent">Ilse Marrow is at this venue.</param>
    public readonly record struct TheRoom(bool SharedHistory, bool SignerPresent, bool FlingPresent)
    {
        /// <summary>Nobody of consequence, and no history — the room every drink before this lane was
        /// poured in, and the default every existing caller keeps.</summary>
        public static TheRoom Ordinary => default;

        /// <summary>True when the room changes the math at all.</summary>
        public bool Matters => SharedHistory || SignerPresent || FlingPresent;
    }

    /// <summary>Push the room's three named modifiers onto a stack, in the order the UI prints them.</summary>
    private static void AddRoom(List<DiceModifier> modifiers, TheRoom room)
    {
        if (room.SharedHistory)
        {
            modifiers.Add(new DiceModifier(SharedHistoryLabel, SharedHistoryBonus));
        }

        if (room.SignerPresent)
        {
            modifiers.Add(new DiceModifier(SignerInTheRoomLabel, SignerInTheRoomPenalty));
        }

        if (room.FlingPresent)
        {
            modifiers.Add(new DiceModifier(FlingInTheRoomLabel, FlingInTheRoomPenalty));
        }
    }

    /// <summary>Roll the 2D6 for a drink shared with a contact.</summary>
    /// <param name="seed">Folds the contact and the moment (caller passes
    /// <see cref="DiceRule.Seed(string, long[])"/> over contact id + sim-second) so the roll is
    /// deterministic and replayable in a test.</param>
    /// <param name="currentGoodwill">The contact's goodwill BEFORE this drink — sets the "old friends"
    /// bonus and whether a good roll can reach business.</param>
    /// <param name="holdingSecret">True when you are carrying something to hide (heat, hot cargo): the
    /// second reality that makes a slip more likely.</param>
    /// <param name="offeringFavorite">True when you offered the contact the very drink you KNOW is their
    /// favourite (#5 SundayMorningWind). A small honest edge — a warmer glass, +1 "their usual" — so
    /// learning what a contact drinks is progress you can spend.</param>
    /// <param name="room">#973 L5a — the old crew's three named modifiers: a shared past helps, and the
    /// signer or the fling standing about hurts. Defaults to an ordinary room, so every caller written
    /// before this lane rolls exactly the dice it always rolled.</param>
    public static DrinkParley Roll(
        ulong seed, int currentGoodwill, bool holdingSecret, bool offeringFavorite = false,
        TheRoom room = default)
    {
        // Two d6 off the ONE shared rule, salted apart so the pair never correlate on a shared seed
        // (the #295/#303 ReeverRaid 2D6 pattern — mimicked here, not depended on, so this lands
        // whether or not #303 has merged; adopt a shared 2D6 helper if one is later extracted).
        int face1 = DiceRule.Roll(DiceRule.Seed(seed, "drink-die-a"), Faces).Face;
        int face2 = DiceRule.Roll(DiceRule.Seed(seed, "drink-die-b"), Faces).Face;

        var modifiers = new List<DiceModifier>();
        if (currentGoodwill >= WarmThreshold)
        {
            modifiers.Add(new DiceModifier("old friends", +1));
        }

        if (offeringFavorite)
        {
            modifiers.Add(new DiceModifier("their usual", +1));
        }

        if (holdingSecret)
        {
            modifiers.Add(new DiceModifier("two realities to hold", -2));
        }

        AddRoom(modifiers, room);

        int total = face1 + face2;
        foreach (DiceModifier m in modifiers)
        {
            total += m.Value;
        }

        DrinkOutcome outcome = OutcomeFor(total, currentGoodwill);
        return new DrinkParley(face1, face2, modifiers, seed, outcome, GoodwillPerDrink);
    }

    /// <summary>Which way a settled total goes. Low totals slip; the fat middle is a warm exchange; high
    /// totals open the contact up — to business if trust is already deep, otherwise to intel.</summary>
    public static DrinkOutcome OutcomeFor(int total, int currentGoodwill) => total switch
    {
        <= 5 => DrinkOutcome.Slip,
        <= 9 => DrinkOutcome.Warm,
        _ => currentGoodwill >= TrustForBusiness ? DrinkOutcome.BusinessUnlock : DrinkOutcome.OpensUp,
    };

    // --- The OFFER, resolved BEFORE a credit moves (#347, owner playtest 2026-07-18) ------------------
    // "The person may refuse the drink here. That possibility should be determined first… If we just buy
    // it then we don't know what they would have and if they accept it." So standing a contact a glass is
    // now two beats: the captain OFFERS, and the contact — deterministically from seed — ACCEPTS or waves
    // off. Only an accepted glass is poured (and only then does the shared-drink 2D6 above decide the
    // edge). A warm contact takes it gladly; a wary one (you're running heat or hot cargo) may pass.

    /// <summary>2D6 total (after modifiers) at or above which the contact accepts an offered glass. Set
    /// low — most people take a free drink — so a refusal reads as real suspicion, not bad luck.</summary>
    public const int AcceptThreshold = 6;

    /// <summary>The pure accept/refuse verdict for a settled offer total — the boundary the client and any
    /// future server share. At or above <see cref="AcceptThreshold"/> the glass is taken.</summary>
    public static bool Accepts(int total) => total >= AcceptThreshold;

    /// <summary>Roll whether a contact accepts an offered drink, before anything is bought.</summary>
    /// <param name="seed">Folds the contact and the moment (salted apart from <see cref="Roll"/>, so the
    /// offer and the shared-drink roll are independent) — deterministic and replayable in a test.</param>
    /// <param name="currentGoodwill">Goodwill BEFORE the offer: a warm contact (≥ <see cref="WarmThreshold"/>)
    /// takes the glass readily.</param>
    /// <param name="holdingSecret">True when you're carrying heat or hot cargo — you read as shifty, and a
    /// wary contact is likelier to wave the glass off.</param>
    /// <param name="offeringFavorite">True when you offered the exact drink you KNOW is their favourite
    /// (#5 SundayMorningWind) — a +1 "their usual" nudge to accept, the small honest edge that knowing
    /// what a contact drinks buys you.</param>
    /// <param name="room">#973 L5a — the same three named modifiers the shared-drink roll takes, for the
    /// same reason: whether an old shipmate takes the glass at all is coloured by who else is standing
    /// about. Defaults to an ordinary room.</param>
    public static DrinkOfferResult OfferDrink(
        ulong seed, int currentGoodwill, bool holdingSecret, bool offeringFavorite = false,
        TheRoom room = default)
    {
        int face1 = DiceRule.Roll(DiceRule.Seed(seed, "drink-offer-a"), Faces).Face;
        int face2 = DiceRule.Roll(DiceRule.Seed(seed, "drink-offer-b"), Faces).Face;

        var modifiers = new List<DiceModifier>();
        if (currentGoodwill >= WarmThreshold)
        {
            modifiers.Add(new DiceModifier("they know you", +2));
        }

        if (offeringFavorite)
        {
            modifiers.Add(new DiceModifier("their usual", +1));
        }

        if (holdingSecret)
        {
            modifiers.Add(new DiceModifier("you read as shifty", -2));
        }

        AddRoom(modifiers, room);

        int total = face1 + face2;
        foreach (DiceModifier m in modifiers)
        {
            total += m.Value;
        }

        return new DrinkOfferResult(face1, face2, modifiers, seed, Accepts(total));
    }
}

/// <summary>A settled drink-OFFER 2D6 (#347): the two natural faces, the named modifier stack, the seed,
/// and whether the contact <see cref="Accepted"/> the glass. Pure data — the caller pours (and rolls the
/// shared-drink <see cref="DrinkParley"/>) only on an accept; a refusal costs nothing but the ask.</summary>
public readonly record struct DrinkOfferResult(
    int Face1,
    int Face2,
    IReadOnlyList<DiceModifier> Modifiers,
    ulong Seed,
    bool Accepted)
{
    /// <summary>The natural pips: the two d6 faces summed, 2..12 before modifiers.</summary>
    public int Pips => Face1 + Face2;

    /// <summary>The sum of every modifier's value (can be negative).</summary>
    public int ModifierTotal
    {
        get
        {
            int sum = 0;
            foreach (DiceModifier m in Modifiers)
            {
                sum += m.Value;
            }

            return sum;
        }
    }

    /// <summary>The pips plus every modifier — the number the accept/refuse verdict reads.</summary>
    public int Total => Pips + ModifierTotal;

    /// <summary>The math, spelled out for the reveal:
    /// <c>2d6: 3 + 4 = 7  −2 (you read as shifty) = 5 → they wave it off</c>.</summary>
    public string Describe()
    {
        var parts = new System.Text.StringBuilder();
        parts.Append("2d6: ").Append(Face1).Append(" + ").Append(Face2).Append(" = ").Append(Pips);
        foreach (DiceModifier m in Modifiers)
        {
            parts.Append("  ").Append(m.Value >= 0 ? '+' : '−').Append(System.Math.Abs(m.Value))
                .Append(" (").Append(m.Label).Append(')');
        }

        if (Modifiers.Count > 0)
        {
            parts.Append("  = ").Append(Total);
        }

        parts.Append(" → ").Append(Accepted ? "they take the glass" : "they wave it off");
        return parts.ToString();
    }
}

/// <summary>A settled drink-parley 2D6 (#306): the two natural d6 faces, the named modifier stack, the
/// seed it came from, the way it went, and the goodwill it books. Pure data — the reveal lives
/// client-side (receipt/pulse today, the shared dice tray tomorrow, #305).</summary>
public readonly record struct DrinkParley(
    int Face1,
    int Face2,
    IReadOnlyList<DiceModifier> Modifiers,
    ulong Seed,
    DrinkOutcome Outcome,
    int GoodwillDelta)
{
    /// <summary>The natural pips: the two d6 faces summed, 2..12 before modifiers.</summary>
    public int Pips => Face1 + Face2;

    /// <summary>The sum of every modifier's value (can be negative).</summary>
    public int ModifierTotal
    {
        get
        {
            int sum = 0;
            foreach (DiceModifier m in Modifiers)
            {
                sum += m.Value;
            }

            return sum;
        }
    }

    /// <summary>The pips plus every modifier — the number that decides the outcome.</summary>
    public int Total => Pips + ModifierTotal;

    /// <summary>The math, spelled out for the reveal:
    /// <c>2d6: 4 + 5 = 9  +1 (old friends)  = 10 → opens up</c>.</summary>
    public string Describe()
    {
        var parts = new System.Text.StringBuilder();
        parts.Append("2d6: ").Append(Face1).Append(" + ").Append(Face2).Append(" = ").Append(Pips);
        foreach (DiceModifier m in Modifiers)
        {
            parts.Append("  ").Append(m.Value >= 0 ? '+' : '−').Append(System.Math.Abs(m.Value))
                .Append(" (").Append(m.Label).Append(')');
        }

        if (Modifiers.Count > 0)
        {
            parts.Append("  = ").Append(Total);
        }

        parts.Append(" → ").Append(Outcome switch
        {
            DrinkOutcome.Slip => "you slip a tell",
            DrinkOutcome.Warm => "a warm glass",
            DrinkOutcome.OpensUp => "they open up",
            _ => "business opens",
        });
        return parts.ToString();
    }
}
