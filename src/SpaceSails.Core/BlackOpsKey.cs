using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #535 · <b>THE CODE THAT UNHAPPENS AN ENCOUNTER.</b>
///
/// <para>Owner: <i>"In Expanse they found black ops code keys from the Tacchi that made the Mars military
/// leave them alone and delete all records of ever encountering them. That kind of keys would be very
/// valuable for pirates. 😎"</i> — and, a breath later, the better half of the idea: <i>"We could use these
/// keys to drop heat at a tight spot 😎"</i>.</para>
///
/// <h3>Two halves, and the second one is the treasure</h3>
///
/// <para>Every other way of surviving a catch leaves a mark: heat rises, a collector remembers, the wire
/// files it. A key is the only thing in this game that reaches back and removes the fact. So the object has
/// two spends and BOTH consume it: <b>present it</b> at a catch, and the encounter never happened; <b>burn
/// it cold</b> from the satchel, and a band comes off the meter of whoever is standing over you.</para>
///
/// <h3>What this file is, and what it is not</h3>
///
/// <para>Pure rules and the canon strings, nothing else. It does not spawn anything, it does not touch a
/// ledger and it does not know what a hunter is — the client spends it, exactly the way it spends every
/// other carried thing. <b>And it never says who made it.</b> That is a canon law rather than an omission:
/// a code that named its issuer would answer the one question the whole object is interesting for.</para>
///
/// <para>Canon pass, 2026-09-05 (on the issue): the name, the look-card line, the two verbs, the outcome
/// plate and the burn line are authored there and copied here verbatim. Nothing else in this feature says
/// anything at all.</para>
/// </summary>
public static class BlackOpsKey
{
    // ── THE OBJECT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Canon. The glyph it wears in a pocket, on a plate, and over the place it is lying.</summary>
    public const string Glyph = "🗝";

    /// <summary>Canon. What it is called, and the only name it ever has.</summary>
    public const string Name = "Black-ops key";

    /// <summary>Canon. The whole of the look card (#614's idiom): what the object IS, and not one word about
    /// what to do with it or whose it was.</summary>
    public const string LookCardLine =
        "A code somebody paid a great deal to make sure nobody would ever read. It works once.";

    /// <summary>Canon. The fifth exit on the BUSTED card.</summary>
    public const string PresentVerb = "Present the key";

    /// <summary>Canon. The satchel's verb, available any time the thing is in the pocket.</summary>
    public const string BurnVerb = "Burn the key";

    /// <summary>
    /// Canon. <b>THE WHOLE OF WHAT THE PLAYER IS TOLD when a key is presented</b>, and #761's law is met by
    /// it: a plate, on the card they are already looking at, at the moment it happens.
    ///
    /// <para>No sentence under it. Every other exit from a catch narrates itself — coin changes hands, a hold
    /// is emptied, somebody peels off nursing a dent — and this one is the absence of all of that. The
    /// silence IS the treasure, and a paragraph explaining that nothing was filed would be the game filing
    /// something.</para>
    /// </summary>
    public const string NoContactLoggedPlate = "NO CONTACT LOGGED";

    /// <summary>Canon. The one line a burn says, on the pulse, once.</summary>
    public const string BurnLine = "Somewhere a file closes. The key is ash.";

    /// <summary>The head of the look card: the canon name, in the plate typography every carried object's
    /// card is titled in. Not a second name — the same one, shouted.</summary>
    public static string CardLabel => Glyph + " " + Name.ToUpperInvariant();

    /// <summary>Caption-only, the deliberate no-picture idiom (#528's odd book, #537's cutting rig): a card
    /// that never claims a painting rather than one that wires an unpainted file and hides it on error.</summary>
    public static CarriedObject.Reveal Card => new(string.Empty, CardLabel, LookCardLine);

    // ── IN THE POCKET ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>One key in the satchel. Its id is <b>the hull it came off</b>, for the reason
    /// <see cref="Satchel.Add"/> makes unavoidable: only rounds stack, so two keys sharing one id would be
    /// one key, and the issue's own scarcity rule (<i>"finding two is a run to remember"</i>) would be
    /// arithmetic that could never happen.</summary>
    public static Satchel.Item FoundOn(string wreckId)
    {
        ArgumentNullException.ThrowIfNull(wreckId);
        return new Satchel.Item(Satchel.Kind.BlackOpsKey, wreckId);
    }

    /// <summary>Is this row one of them?</summary>
    public static bool IsTheKey(Satchel.Item item) => item.Kind == Satchel.Kind.BlackOpsKey;

    /// <summary>The first one in the pocket, or null. The one a catch spends, and the one the client asks
    /// about to decide whether the fifth exit is drawn at all.</summary>
    public static Satchel.Item? InThePocket(IReadOnlyList<Satchel.Item>? carried)
    {
        foreach (Satchel.Item item in carried ?? [])
        {
            if (IsTheKey(item))
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>How many are carried. Both spends take exactly one.</summary>
    public static int CountIn(IReadOnlyList<Satchel.Item>? carried) =>
        Satchel.CountOf(carried, Satchel.Kind.BlackOpsKey);

    /// <summary>Spend one — the same call for both spends, so a presentation and a burn can never come to
    /// disagree about what "consumed" means.</summary>
    public static IReadOnlyList<Satchel.Item> Spend(IReadOnlyList<Satchel.Item>? carried, Satchel.Item key) =>
        Satchel.Remove(carried, Satchel.Kind.BlackOpsKey, key.Id);

    // ── WHERE THEY LIE ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #535 · <b>ONLY ON A HULL THAT FOUGHT</b>, which in this build means exactly one thing: the hull
    /// #538's black-ops sweep team comes aboard to make stop existing as evidence
    /// (<see cref="Derelict.WreckCause.InsuranceJob"/> — the one cause <c>Map.Surface</c> spawns them on).
    ///
    /// <para>Canon, 2026-09-05: <i>"dealt by the wreck salvage roll on hulls that fought (the black-ops
    /// sweep's own kind, #538's hulls and the Q-ship class when #534 builds) — never on a merchant, never
    /// bought."</i> The Q-ship class is not a cause yet, so this switch has one arm and will grow a second
    /// the day #534 lands; it is written as a switch rather than an equality for that reason.</para>
    ///
    /// <para><b>And never <see cref="Derelict.WreckCause.Piracy"/>, which is the arm somebody will reach
    /// for.</b> That hull was boarded and stripped in a hurry with her deep hold untouched — she is the
    /// merchant the canon rules out, read from the other end.</para>
    /// </summary>
    public static bool CauseMayCarryOne(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.InsuranceJob => true,
        _ => false,
    };

    /// <summary>One eligible hull in this many is actually carrying one. FLAGGED for the owner's tuning, and
    /// the only number in this file — the scarcity the whole design rests on lives here and nowhere else.
    ///
    /// <para>It sits on top of the cause gate, not beside it, so the rate a captain actually meets is this
    /// one THROUGH the frequency of the sweep's own hull class. <c>TheBlackOpsKeyTests</c> measures both.</para></summary>
    public const int OneInEligibleHulls = 5;

    /// <summary>
    /// <b>THE WRECK SALVAGE ROLL.</b> Is there a key on THIS hull? Seeded off her id, so a reload finds the
    /// same ship, a captain who has heard a rumour about a hull can go and look, and leaving and coming back
    /// is not a re-roll.
    ///
    /// <para><b>The id goes into the seed WHOLE</b> — <see cref="DiceRule.Seed(string, long[])"/> folds the
    /// characters itself. It is not hashed with <c>string.GetHashCode</c> first, which is the tempting one
    /// line and is randomised per process in .NET: a roll seeded that way is a different roll every time the
    /// tab is reloaded, so the ship a rumour named would be carrying a key this afternoon and not tomorrow.
    /// Measured, not assumed — the first cut of this method did it that way, and
    /// <c>TheRarityIsTheOneThatWasMeasured</c> reported two different rates on two consecutive runs.</para>
    /// </summary>
    public static bool IsAboard(string wreckId, Derelict.WreckCause cause)
    {
        ArgumentNullException.ThrowIfNull(wreckId);
        if (!CauseMayCarryOne(cause))
        {
            return false;
        }

        return DiceRule.Roll(DiceRule.Seed($"black-ops-key-aboard:{wreckId}"), OneInEligibleHulls).Face == 1;
    }

    // ── WHAT A BURN COSTS THE FILE ──────────────────────────────────────────────────────────────────────

    /// <summary>#535/#938 · The reason written into an outfit's book when a key is burned against it. Not
    /// prose and never shown: <see cref="IllegalHeat.Scrub"/> wants a caller who says WHY, so an edit in that
    /// ledger can be told apart from an hour of absence.</summary>
    public const string ScrubReason = "a key burned cold";

    // ── THE OTHER TWO SOURCES · A FAVOUR AND A FENCE ────────────────────────────────────────────────────
    //
    // #535 · SLICE 2. Slice 1 dealt the key ONE way: lying in the crew spaces of a hull that fought. That
    // made it a thing that happens TO a captain and never a thing a captain can go and get, and the issue
    // names the two roads still open — a fence, a favour. Both are here, and neither of them is a second
    // answer to what a key IS: they mint the same `Satchel.Kind.BlackOpsKey` row through this same file, so
    // there is exactly one object in this game with exactly two spends, however it got into the pocket.
    //
    // Canon pass (Fable, on the issue) authors the four strings below and nothing else. The favour's line is
    // the contact SPEAKING, on the card the verb sits on; the fence's line is a ROW on a desk. Neither
    // announces a source, because a source that announced itself would be a quest marker — and this object's
    // whole register is that nobody says anything (see the plate, above).

    /// <summary>Canon. What the contact says as they hand it over — the line on the verb's own card, and the
    /// whole of what is ever said about the favour. It names the reason (<i>you never lied to me</i>) and
    /// refuses the gratitude, which is why <see cref="ContactHistory.WasLiedTo"/> is the gate and not a flag
    /// invented for this beat: the sentence and the predicate are the same fact.</summary>
    public const string FavourLine =
        "You never lied to me. That is rarer than what I'm about to hand you. Don't bring it back.";

    /// <summary>Canon. The favour's verb, on the seat's existing card beside the round.</summary>
    public const string FavourVerb = "Take the favour";

    /// <summary>Canon. The fence's row on the dark-web desk. He prices a clean record and never says what
    /// the thing is — the row he sits in already does, in the object's own words.</summary>
    public const string FenceRowLine =
        "Fresh this cycle. Ask me what it costs and I'll ask you what a clean record costs.";

    /// <summary>Canon. The fence's verb.</summary>
    public const string FenceVerb = "Buy the key";

    /// <summary>
    /// #535 slice 2 · <b>THE TOP GOODWILL BAND, AND IT IS NOT A NUMBER THIS FILE INVENTED.</b>
    ///
    /// <para>The ledger has no bands of its own — <see cref="ContactHistory.Goodwill"/> is a running int and
    /// nothing in it says where "close" begins. So this reads the HIGHEST threshold anything already reads
    /// that number against: <see cref="StrangerBond.AlreadyCloseGoodwill"/>, <i>"goodwill at or above which a
    /// contact is already close"</i> — the one the bond lane filters acquaintances by before it offers to
    /// deepen them, because a true friend has nothing left to add. Above it are people who would hand you
    /// something; below it are people you have been buying drinks for.</para>
    ///
    /// <para>The two lower thresholds in the same family are deliberately NOT used.
    /// <see cref="ContactDrink.WarmThreshold"/> (3) is a contact who relaxes;
    /// <see cref="ContactDrink.TrustForBusiness"/> (5) is a contact who will trade with you. Trading with you
    /// is not the same as spending their own standing on you, which is what this is.</para>
    /// </summary>
    public static int TopGoodwillBand => StrangerBond.AlreadyCloseGoodwill;

    /// <summary>
    /// #535 slice 2 · <b>WHAT THE FAVOUR COSTS THEM, AND IT IS EXACTLY THE BAND THAT QUALIFIED IT.</b>
    ///
    /// <para>A favour is spent, not banked: the standing that made the offer possible is the standing that
    /// pays for it. So the debit IS <see cref="TopGoodwillBand"/> — the whole band — which means a contact
    /// who was just close enough is back at the bottom afterwards, and one who was far past it is merely
    /// close. One number, derived, read in both directions; retune the band and the price follows it instead
    /// of drifting away from it.</para>
    /// </summary>
    public static int FavourCostsGoodwill => TopGoodwillBand;

    /// <summary>
    /// #535 slice 2 · <b>IS THE FAVOUR ON THE TABLE?</b> Three questions, and all three are the ledger's
    /// own: they are in the top band, the book marks no lie, and they have not already done this.
    ///
    /// <para><b>Why the lie and not, say, missions completed.</b> The canon line is the gate stated out
    /// loud — <i>"You never lied to me"</i> — and <see cref="ContactHistory.WasLiedTo"/> is the one place
    /// this game records having lied to somebody. A second predicate meaning "trustworthy" would be a number
    /// that stops agreeing with the sentence the first afternoon either is touched.</para>
    /// </summary>
    public static bool FavourIsOnTheTable(ContactHistory history) =>
        !history.WasLiedTo
        && !history.FavourSpent
        && history.Goodwill >= TopGoodwillBand;

    /// <summary>The key a contact hands over. Its id is the CONTACT, for the reason <see cref="FoundOn"/>'s
    /// is the hull: only rounds stack, so a shared id would silently make two people's favours one key.</summary>
    public static Satchel.Item FavourFrom(string contactId)
    {
        ArgumentNullException.ThrowIfNull(contactId);
        return new Satchel.Item(Satchel.Kind.BlackOpsKey, "favour:" + contactId);
    }

    // ── THE FENCE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#535 slice 2 · The purpose tag the fence's stream is salted with, so his price can never move
    /// with anything else rolled off the same port on the same watch.</summary>
    public const string FenceSeedTag = "black-ops-key-fence";

    /// <summary>
    /// #535 slice 2 · <b>THE ROTATION WINDOW IS A WATCH.</b> Slice 1 built no rotation of any kind — the
    /// salvage roll is seeded off a hull id and has no clock in it at all — so the fence needs one, and it is
    /// the world's own shift rather than a number typed here: <see cref="Interior.PatronRota.WatchIndex"/>,
    /// the four-sim-hour beat the seated regulars, the escort's patience and the oracle's corner all turn on.
    /// <i>"Fresh this cycle"</i> is that cycle.
    /// </summary>
    public static long FenceWindow(double simTime) => Interior.PatronRota.WatchIndex(simTime);

    /// <summary>The fence's seed at one port on one watch. A stable char-sum of the port id, the idiom the
    /// booth's own deals are salted with (<see cref="CompromisingChip.Seed"/>) — so two ports quote two
    /// prices, and one port quotes ONE price for the whole watch however many times the desk is opened.</summary>
    public static ulong FenceSeed(string portId, long window)
    {
        ArgumentNullException.ThrowIfNull(portId);
        long salt = 0;
        foreach (char c in portId)
        {
            salt += c;
        }

        return DiceRule.Seed(FenceSeedTag, window, salt);
    }

    /// <summary>#535 slice 2 · How many bribes the fence wants. THREE — the design's own number, and the only
    /// constant in this half of the file.</summary>
    public const int FenceAsksThisManyBribes = 3;

    /// <summary>
    /// #535 slice 2 · <b>WHAT THE FENCE WANTS, AND IT IS THE BUSTED CARD'S OWN ARITHMETIC.</b>
    ///
    /// <para>Three times the bribe — <see cref="BustedRule.BribeDemand"/>, <b>called</b>, never restated. That
    /// is the joke the fence's own line makes out loud (<i>"I'll ask you what a clean record costs"</i>): the
    /// price of the thing that makes a catch unhappen is three times the price of buying your way out of one
    /// catch, so it is only worth the coin to a captain who expects three.</para>
    ///
    /// <para>It moves with the captain's heat because the bribe does, and that is the fiction rather than a
    /// quirk: the fence is pricing what a clean record is worth TO YOU, and he can read the same meter the
    /// collector reads.</para>
    ///
    /// <para><b>There is deliberately no heat floor here, and the red-proof is why.</b> The first cut wrote
    /// <c>Math.Max(1, heat)</c> — the repo boat's own line, copied across. Reverting it did not turn a single
    /// guard red, because <see cref="BustedRule.BribeDemand"/>'s own band table already answers
    /// <c>&lt;= 1</c> with one arm: the floor was a SECOND opinion about a question the bribe had already
    /// settled, and the only thing it could ever do is disagree with it — silently, on the afternoon somebody
    /// gives that table a heat-zero arm. So the heat goes through whole and the bribe decides what a cold
    /// captain is quoted, the way it decides what a hot one is.</para>
    /// </summary>
    public static int FencePrice(int heat, ulong seed) =>
        FenceAsksThisManyBribes * BustedRule.BribeDemand(heat, seed).Total;

    /// <summary>The key the fence sells. Its id is the port and the watch, so two windows are two keys and
    /// one window is one key however many times the row is looked at.</summary>
    public static Satchel.Item FromTheFence(string portId, long window)
    {
        ArgumentNullException.ThrowIfNull(portId);
        return new Satchel.Item(Satchel.Kind.BlackOpsKey, $"fence:{portId}@{window}");
    }

    /// <summary>
    /// #535 slice 2 · <b>ONE KEY PER PORT PER WINDOW, COUNTING BOTH NEW SOURCES TOGETHER.</b>
    ///
    /// <para>This is the strike-off the favour and the fence BOTH write and BOTH read, kept in the captain's
    /// register of ground already gone through (#615's <c>_roomsTurnedOver</c> — the same durable set slice 1
    /// strikes a spent hull off in). One tag, two writers, which is the only shape under which the law holds:
    /// a captain cannot take the favour at the bar and then walk to the desk and buy the second one on the
    /// same watch, and the two sources cannot come to two opinions about whether this port has dealt today.</para>
    ///
    /// <para>A prefix of its own, and nothing <c>KeepOrLeave.TryReadKey</c> can parse as a Hive room — it
    /// walks straight past this the way it walks past slice 1's <c>wreck-key:</c>.</para>
    /// </summary>
    public static string ThePortHasDealtOne(string portId, long window)
    {
        ArgumentNullException.ThrowIfNull(portId);
        return $"port-key:{portId}@{window}";
    }

    /// <summary>Every sentence this feature can put on a screen, for the audit that reads them all. Slice 1
    /// published three; slice 2 adds the favour's line, the fence's row, and the two verbs they sit on.</summary>
    public static IEnumerable<string> EveryLine()
    {
        yield return LookCardLine;
        yield return BurnLine;
        yield return NoContactLoggedPlate;
        yield return FavourLine;
        yield return FavourVerb;
        yield return FenceRowLine;
        yield return FenceVerb;
    }
}
