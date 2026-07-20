namespace SpaceSails.Core;

/// <summary>
/// STRANGER-BOND · the WARM twin of the ambient-dread system (#429, owner mid-storm 2026-07-20): <b>"This
/// all made unknown people talk to each other and they recommended cognac.. I agreed. So kind of adversity
/// makes the sharers bond. Kind of beautiful how they shared 'what was that bump, buzzer, warning'.. it
/// bonds strangers. Let's use that."</b>
///
/// <para>The very same ambient scares that UNSETTLE you — a hull-shudder, an unexplained buzzer, a caution
/// PA (all <see cref="HullShudder"/>, #430) — can, when a not-yet-known patron is co-present in the bar,
/// OPEN them to you instead of only chilling the room. Dread and grace in the same beat. A shared fright is
/// the icebreaker: sometimes it's just a warm word; sometimes a stranger stands you a cognac by name;
/// sometimes it makes a whole new contact you can find again; and a colder, deep-site scare bonds RARER but
/// DEEPER — the gallows-humour register ("if that's our last buzzer, I'm glad I met you").</para>
///
/// <para>Pure and fully deterministic from a seed and a monotonic bond index — the same seeded idiom as
/// <see cref="HullShudder"/> and <see cref="DiceRule"/> (never <see cref="System.Random"/> or the clock:
/// determinism is law in Core). Given a seed, the next index, which scare fired, whether it was cold, and
/// who is co-present, it answers "does this scare open a bond", "which outcome", and "which house-voice
/// line speaks it". The cooldown, the co-presence scan, and the goodwill/contact effect are the client's
/// thin real-time layer — applied through the EXISTING <see cref="ContactLedger"/> methods, never
/// re-implemented here.</para>
/// </summary>
public static class StrangerBond
{
    /// <summary>Which ambient scare (all <see cref="HullShudder"/>, #430) opened the shared moment — so the
    /// bond line can name the very thing that triggered it ("that buzzer again — drink?").</summary>
    public enum Scare
    {
        /// <summary>A hull-shudder: the deck flexed, every head came up as one.</summary>
        Shudder = 0,
        /// <summary>An unexplained signal: a distant buzzer/tone the staff wouldn't explain.</summary>
        Signal = 1,
        /// <summary>A caution PA: the house voice asking all hands to move deliberately.</summary>
        Caution = 2,
    }

    /// <summary>What the shared scare opened. <see cref="None"/> when it bonded no one (nobody eligible was
    /// near, or the seeded gate held). The other four are the owner's grace notes.</summary>
    public enum Bond
    {
        /// <summary>Nothing opened — the scare only unsettled, as most do.</summary>
        None,
        /// <summary>The stranger just COMMENTS on the scare — a warm word, a shared look. No ledger change:
        /// the warmth is real but nothing is written down (the tiny grace, not a whole friendship).</summary>
        Comment,
        /// <summary>The stranger RECOMMENDS or STANDS you a drink — a cognac by name (<see cref="HeroCognac"/>)
        /// — and goodwill nudges up. The hero beat the owner lived at sea.</summary>
        Drink,
        /// <summary>The shared fright turns a stranger into a KNOWN CONTACT you can find again (the rota's
        /// known-contact path, #414): a real relationship, booked as goodwill.</summary>
        NewContact,
        /// <summary>The scare deepens an EXISTING acquaintance a notch — no stranger was near, but someone you
        /// half-know was, and the shared beat warmed them further (bounded by <see cref="AlreadyCloseGoodwill"/>).</summary>
        Deepen,
    }

    // ── The gate: does a scare bond, and how often. ───────────────────────────────────────────────────

    /// <summary>The chance a WARM scare (a haven/ship shudder, a buzzer, a routine PA) opens a bond when a
    /// stranger is co-present — the common, generous register: a shared fright often gets a word. FLAGGED
    /// for the owner's tuning.</summary>
    public const double WarmBondChance = 0.5;

    /// <summary>The chance a COLD/deep-site scare (a chill shudder, a lingering cold glance, a deep-site PA)
    /// opens a bond — deliberately RARER than <see cref="WarmBondChance"/> (owner: "the rarer/bigger the
    /// scare, the likelier the bond" — but out here people are wary, so the cold moment lands seldom, and
    /// when it does it's DEEPER). FLAGGED.</summary>
    public const double ColdBondChance = 0.28;

    /// <summary>The shortest quiet between two bonds (real deck-seconds) — the client's cooldown floor, so a
    /// run of scares can't spam the room into a friendship mill. One bond, then the room settles before the
    /// next scare can open another. Exposed so the wiring and a test read one number. FLAGGED.</summary>
    public const double CooldownSeconds = 90.0;

    // ── The already-close cap (bounded warmth). ───────────────────────────────────────────────────────

    /// <summary>Goodwill at or above which a contact is "already close" and a shared scare no longer
    /// DEEPENS them (owner: "never fires for already-close contacts beyond a cap"). A shared fright warms a
    /// cold or middling acquaintance; it has nothing left to add to a true friend. The client filters
    /// present acquaintances by this before offering them as a <see cref="Bond.Deepen"/> target.</summary>
    public const int AlreadyCloseGoodwill = 6;

    // ── The goodwill each outcome books (through ContactLedger.AddGoodwill — the EXISTING method). ─────

    /// <summary>A <see cref="Bond.Comment"/> books NO goodwill — it is warmth without a ledger line, the
    /// smallest grace (a shared word, not a relationship). The client instead steadies the nerve a hair.</summary>
    public const int CommentGoodwill = 0;

    /// <summary>The goodwill a <see cref="Bond.Drink"/> (a stranger standing/recommending a cognac) nudges
    /// up — a real warming, on the order of a bought round (#283's per-head +1), a touch more because a
    /// fright shared over a named glass means something.</summary>
    public const int DrinkGoodwill = 2;

    /// <summary>The goodwill a <see cref="Bond.NewContact"/> books — enough to register a genuine
    /// relationship (a stranger you can now find again), on the order of a shared drink with a contact
    /// (<see cref="ContactDrink.GoodwillPerDrink"/>).</summary>
    public const int NewContactGoodwill = 3;

    /// <summary>The goodwill a warm <see cref="Bond.Deepen"/> adds to an existing acquaintance — a single
    /// notch, the quiet warming of a shared scare.</summary>
    public const int DeepenGoodwill = 1;

    /// <summary>The goodwill a COLD bond books — DEEPER than any warm outcome (the gallows-humour register:
    /// people who share the scary quiet out past the edge bond hard and fast).</summary>
    public const int ColdBondGoodwill = 4;

    /// <summary>The hair of nerve a <see cref="Bond.Comment"/> STEADIES — the warmth of not being alone with
    /// the fright, a small counter to the dread the same scare deals. Smaller than any drink. FLAGGED.</summary>
    public const double CommentNerveSteady = 3.0;

    /// <summary>The in-world cognac a bonding stranger reaches for — the hero of the whole beat (owner: "they
    /// recommended cognac.. I agreed"). An INVENTED label, not a real brand: an amber marc distilled sunward,
    /// the good bottle behind every decent bar out here.</summary>
    public const string HeroCognac = "OLD PERIHELION";

    // ── The line pools (house voice), per scare — the register tied to the SAME thing that triggered it. ─
    //
    // Every pool is indexed by (int)Scare so a comment on a buzzer names the buzzer, a drink after a caution
    // PA names the announcement, etc. The COLD pool (the gallows register) is separate and DISTINCT from the
    // warm pools — a deep-site scare bonds people the way strangers bond in a lifeboat, not over a nice meal.

    // (a) COMMENT — a warm word, a shared look. No drink, no ledger — the smallest grace.
    private static readonly string[][] CommentLines =
    [
        // Shudder
        [
            "The stranger two stools down catches your eye as the deck settles. “You felt that too? Thought it was just me and one drink too many.” A short laugh, and the room feels a size smaller.",
            "“There she goes again,” the stranger beside you says to nobody and everybody, nodding at the floor. You find you’re both almost smiling about it. Strangers a breath ago.",
            "The one across the bar meets your look while the glasses stop ringing. “Just a wave,” they say, the way you tell it to a kid — and to yourself. It helps to hear someone else say it.",
        ],
        // Signal
        [
            "“That buzzer,” the stranger murmurs, not quite a question, catching your eye. “Never do find out what they are, do you.” A shrug, a small shared grin — and the unease has company now.",
            "The one at the next table tips their head toward the bulkhead the tone came from. “You heard it. Good. Thought I was losing it.” You hadn’t met a minute ago; now you’re in on the same small mystery.",
            "As the far-off tone dies the stranger beside you lets out the breath you were both holding. “Whatever that was,” they say, and leave it there, and somehow that’s exactly right.",
        ],
        // Caution
        [
            "“‘Move deliberately,’” the stranger quotes the PA back, dry as dust, catching your eye. “Like we weren’t already.” You laugh despite yourself — and just like that there’s two of you against the weather.",
            "The one down the bar raises an eyebrow at the ceiling speaker. “One hand for the ship, they say. What’s the other hand for?” They lift their glass. You lift yours. Point made.",
            "“They only make that call when it’s properly rough,” the stranger says, almost to you, almost to themselves. A shared glance, a shared shrug — the announcement bonded you more than it warned you.",
        ],
    ];

    // (b) DRINK — the HERO beat: the stranger recommends or stands you a cognac by name.
    private static readonly string[][] DrinkLines =
    [
        // Shudder
        [
            "The stranger beside you rides out the shudder, then turns. “That calls for something better than what you’re drinking. Barkeep — two of the " + HeroCognac + ", on the fright.” Amber cognac, and a stranger who isn’t one anymore.",
            "“Deck jumps like that, you want a cognac, not that,” the stranger says, eyeing your glass. “The " + HeroCognac + " — trust me. On me.” They wave the barkeep over before you can argue.",
            "As the room decides it was nothing, the stranger next to you decides something else. “Life’s short and the hull’s loud. " + HeroCognac + ", the both of us.” A warm glass slides your way.",
        ],
        // Signal
        [
            "“That buzzer again — drink?” the stranger says, already signalling the bar. “The " + HeroCognac + ". Best thing for a sound nobody’ll explain.” Two amber cognacs, and the unease turned to company.",
            "The stranger tips their head at the fading tone. “Whenever I hear that, I have a good cognac and stop asking questions. " + HeroCognac + " — let me stand you one.” And they do.",
            "“You look like I feel about that noise,” the stranger says, and grins. “The cure’s behind the bar. " + HeroCognac + ", two — my shout.” The glass warms your hand and the buzzer stops mattering.",
        ],
        // Caution
        [
            "“If they’re telling us to hold the rail,” the stranger says over the dying PA, “we’d best have something worth spilling. " + HeroCognac + " — two, barkeep, on me.” Cognac against caution; the good kind of defiance.",
            "The stranger raises their empty glass to the ceiling speaker. “Advisory noted. Countermeasure: cognac.” A grin your way. “The " + HeroCognac + ". You’re not drinking that swill through a rough patch alone.”",
            "“Rough passage calls for a proper glass,” the stranger decides, flagging the bar as the announcement fades. “" + HeroCognac + ", the both of us. Weather’s better shared.” And it is.",
        ],
    ];

    // (c) NEW CONTACT — the shared fright makes a friend you can find again.
    private static readonly string[][] NewContactLines =
    [
        // Shudder
        [
            "By the time the deck stops flexing you’ve traded names. “I’m in this port most watches,” the stranger says. “Ask for me. A shudder shared’s as good as a handshake out here.” A stranger, filed as a friend.",
            "The shudder does what a week of small talk couldn’t. You end up swapping berths and names, and the stranger claps your shoulder: “Next time you’re through, you drink with me. That’s settled.” It is.",
            "“Funny how a bit of a scare sorts the good ones from the rest,” the stranger says, and means you. Names change hands. You’ll know where to find them now — a contact made over a flex of steel.",
        ],
        // Signal
        [
            "The unexplained buzzer leaves you both grinning at nothing, and somewhere in it you’ve become friends. “Name’s worth knowing,” the stranger says, and gives it. “Find me next time the ceiling makes a noise.”",
            "“Anyone who hears that thing and doesn’t pretend they didn’t — that’s someone I drink with,” the stranger says, and puts a name to it. A buzzer nobody explained just handed you a contact.",
            "You never do learn what the tone was, but you learn the stranger’s name, their usual port, their line of work. “Small system,” they say. “We’ll cross again.” And now you’ll know them when you do.",
        ],
        // Caution
        [
            "The caution PA turns two strangers into two names by the time it fades. “I’m easy to find,” the stranger says. “Anyone weathers a rough patch beside me, I remember them.” They’ll remember you.",
            "“You keep a level head when the deck’s not itself,” the stranger says, approving, and offers a name for yours. A contact made — not over business, but over a bad night taken well together.",
            "By the time the announcement’s done you’ve swapped names and a standing invitation. “Next rough passage,” the stranger says, “you find me. We’ll ride it out properly.” A friend, out of a warning.",
        ],
    ];

    // (d) DEEPEN — no stranger near, but a half-known face was, and the shared beat warmed them a notch.
    private static readonly string[][] DeepenLines =
    [
        // Shudder
        [
            "You catch {0}’s eye as the deck settles, and the old wariness thaws a degree. Nothing said — just the two of you, riding out the same shudder, a little less strangers than you were.",
            "{0} rides out the flex beside you and gives a small nod, the kind you only give someone you’re starting to trust. The shared scare closed a little more of the distance.",
            "The shudder finds you both at the same bar, and {0} lifts their glass an inch your way when it passes. A degree warmer between you now, and neither of you had to say why.",
        ],
        // Signal
        [
            "The buzzer sounds and you and {0} trade the same weary look you’re starting to have a history of. It warms things a notch — shared unease, from a face you half-know.",
            "{0} tips their head at the fading tone, then at you, wry. You’ve heard this one together before. The room’s a little friendlier for it.",
            "Whatever the tone was, {0} clearly heard it too, and clearly filed it with you — the pair of you and the noise nobody explains. A notch closer.",
        ],
        // Caution
        [
            "“Here we go,” {0} mutters as the PA winds up, and the shared eye-roll warms the acquaintance a degree. You’ve weathered one of these beside them now.",
            "The caution call finds you and {0} at the same bar again, and this time there’s an almost-grin in it. Rough passages shared add up; you’re a notch past strangers.",
            "{0} raises an eyebrow at the ceiling speaker, then at you, as if to say ‘you and me again.’ The announcement warmed the acquaintance more than it cautioned anyone.",
        ],
    ];

    // COLD — the gallows-humour register: rarer, deeper. A deep-site scare bonds people like a lifeboat.
    // DISTINCT from every warm pool. Names the cognac too (the hero survives into the dark), but darker.
    private static readonly string[][] GallowsLines =
    [
        // Shudder
        [
            "The ground shudders and doesn’t quite stop when it should, and the stranger beside you doesn’t pretend it did. “If this rock decides to keep us,” they say quietly, “I’d rather not be drinking alone. " + HeroCognac + ", while there’s a barkeep to pour it.” You drink. You mean it.",
            "Nobody in the place lets the breath back out this time. The stranger meets your eye across the too-long quiet. “Well,” they say, “if that was something, it was something with company.” They put out a hand, and a name. You take both.",
            "The flex goes on a beat past nothing, and the stranger next to you says, low, “I don’t know what’s under us, and I’ve decided I don’t want to know it by myself.” A cognac, two, and a friendship forged where the light gets thin.",
        ],
        // Signal
        [
            "The tone climbs past the bulkheads and doesn’t stop when it should. The stranger holds your eye a beat too long, the way the crew do. “If that’s our last buzzer,” they say, “I’m glad I met you.” " + HeroCognac + ", the both of you, against the dark.",
            "The staff trade a look that lingers, and so do you and the stranger beside you. “They’ve heard it before,” they murmur. “They’re not saying. So we’ll sit here and not say it together.” A name changes hands. It feels like more than a name, out here.",
            "Far off, the klaxon sounds a pattern — not random, a pattern — and the stranger’s hand finds their glass, and yours. “Names,” they say. “Quick. In case.” You give yours. They give theirs. Out past the edge, that’s a bond you keep.",
        ],
        // Caution
        [
            "“Stay in pairs,” the cold PA says, “and don’t go looking.” The stranger beside you huffs a laugh with no bottom to it. “Pair, then,” they say, and offer a name, and a cognac. “" + HeroCognac + ". If the dark wants us it can wait till we’ve finished.”",
            "The announcement names the thing it’s pretending isn’t there, and the stranger next to you stops pretending too. “No one out here believes ‘routine,’” they say. “But I’ll believe in a good glass and decent company.” They mean you. It holds.",
            "“The deck is not itself tonight,” the PA admits, and the stranger raises their glass to that awful honesty. “To not being ourselves, together,” they say. A name, a cognac, and a friendship that only a place this cold could weld this fast.",
        ],
    ];

    /// <summary>The warm line pool for an <paramref name="outcome"/> and <paramref name="scare"/> — exposed so
    /// a test can pin every line non-blank, the pool free of duplicates, and the scare register present.
    /// <see cref="Bond.None"/> has no pool (empty). The COLD/gallows pool is <see cref="GallowsLinesFor"/>.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> LinesFor(Bond outcome, Scare scare) => outcome switch
    {
        Bond.Comment => CommentLines[(int)scare],
        Bond.Drink => DrinkLines[(int)scare],
        Bond.NewContact => NewContactLines[(int)scare],
        Bond.Deepen => DeepenLines[(int)scare],
        _ => System.Array.Empty<string>(),
    };

    /// <summary>The COLD/gallows line pool for a <paramref name="scare"/> — the deep-site register (rarer,
    /// deeper). Distinct from every warm pool. Exposed for the same non-blank / unique / register pinning.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> GallowsLinesFor(Scare scare) => GallowsLines[(int)scare];

    // ── The gate. ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Does the <paramref name="bondIndex"/>-th scare OPEN a bond? Deterministic per (seed, index),
    /// salted apart from the line stream. A <paramref name="cold"/> scare clears the rarer
    /// <see cref="ColdBondChance"/>; a warm one the generous <see cref="WarmBondChance"/>. The caller only
    /// reaches here when someone eligible is co-present; the cooldown is the client's own floor.</summary>
    public static bool Opens(ulong seed, int bondIndex, bool cold) =>
        Fraction(seed, $"bond-open:{bondIndex}") < (cold ? ColdBondChance : WarmBondChance);

    // ── The outcome. ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Which grace note a bonding scare plays, given who is co-present. Deterministic per (seed, index),
    /// salted apart from the gate and line streams.
    ///
    /// <para>A <paramref name="cold"/> scare is the gallows register and always bonds DEEP: with a stranger
    /// near it makes a <see cref="Bond.NewContact"/> outright (no mere word or single glass out here); with
    /// only an eligible acquaintance near it <see cref="Bond.Deepen"/>s them. A WARM scare with a stranger
    /// near rolls the three warm notes — <see cref="Bond.Comment"/>, <see cref="Bond.Drink"/> (the
    /// cognac hero), or <see cref="Bond.NewContact"/>; with only an acquaintance near it
    /// <see cref="Bond.Deepen"/>s. With no one eligible, <see cref="Bond.None"/>.</para>
    /// </summary>
    /// <param name="strangerPresent">A not-yet-known patron is co-present (a candidate for a/b/c).</param>
    /// <param name="acquaintanceEligible">A KNOWN contact below <see cref="AlreadyCloseGoodwill"/> is
    /// co-present (a candidate for <see cref="Bond.Deepen"/> — not already close).</param>
    public static Bond Outcome(ulong seed, int bondIndex, bool cold, bool strangerPresent, bool acquaintanceEligible)
    {
        if (cold)
        {
            // The gallows register bonds deep or not at all: a stranger becomes a contact; failing that a
            // half-known face is drawn a notch closer.
            return strangerPresent ? Bond.NewContact : acquaintanceEligible ? Bond.Deepen : Bond.None;
        }

        if (strangerPresent)
        {
            // The three warm notes, weighted: a word is most common, a stood cognac the warm middle, a whole
            // new contact the rarer gift. Weights are the seeded [0,1) split below.
            double u = Fraction(seed, $"bond-outcome:{bondIndex}");
            return u < CommentWeight ? Bond.Comment
                 : u < CommentWeight + DrinkWeight ? Bond.Drink
                 : Bond.NewContact;
        }

        return acquaintanceEligible ? Bond.Deepen : Bond.None;
    }

    // The warm-note weights (they + the NewContact remainder sum to 1). A shared word is the common grace;
    // the cognac hero is the warm middle; a new contact is the rarer, bigger gift. FLAGGED for tuning.
    private const double CommentWeight = 0.42;
    private const double DrinkWeight = 0.34;   // ⇒ NewContact ~0.24

    /// <summary>The goodwill an <paramref name="outcome"/> books through <see cref="ContactLedger.AddGoodwill"/>
    /// — the EXISTING method the client calls (never a new ledger path). A <paramref name="cold"/> bond
    /// runs DEEPER. <see cref="Bond.Comment"/> books none (warmth without a ledger line); <see cref="Bond.None"/>
    /// books none.</summary>
    public static int GoodwillFor(Bond outcome, bool cold) => outcome switch
    {
        Bond.Comment => CommentGoodwill,
        Bond.Drink => DrinkGoodwill,
        Bond.NewContact => cold ? ColdBondGoodwill : NewContactGoodwill,
        Bond.Deepen => cold ? ColdBondGoodwill - 2 : DeepenGoodwill, // a cold deepen still runs deeper than warm
        _ => 0,
    };

    // ── Line selection. ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The house-voice line for a bonding scare — drawn deterministically from the
    /// <paramref name="outcome"/> + <paramref name="scare"/> pool (or the gallows pool when
    /// <paramref name="cold"/>) per (seed, index), so the same bond always speaks the same words and a run
    /// rotates the pool. A <see cref="Bond.Deepen"/> line carries a <c>{0}</c> the caller fills with the
    /// acquaintance's name.</summary>
    public static string Line(Bond outcome, Scare scare, bool cold, ulong seed, int bondIndex)
    {
        // The cold/gallows register replaces the warm comment/drink/new-contact voice; a Deepen keeps its
        // own pool (there is no gallows "deepen" — a cold deepen is still the half-known face, warmed hard).
        System.Collections.Generic.IReadOnlyList<string> pool =
            cold && outcome != Bond.Deepen ? GallowsLinesFor(scare) : LinesFor(outcome, scare);
        if (pool.Count == 0)
        {
            return string.Empty;
        }
        return pool[Index(seed, $"bond-line:{(int)outcome}:{(int)scare}:{(cold ? "cold" : "warm")}:{bondIndex}", pool.Count)];
    }

    /// <summary>A deterministic pick of one of <paramref name="count"/> co-present candidates (which stranger
    /// or acquaintance the scare bonds), salted apart from the outcome/line streams. Returns 0 when there is
    /// one or none, so the caller can index safely.</summary>
    public static int Pick(ulong seed, int bondIndex, int count) =>
        count <= 1 ? 0 : Index(seed, $"bond-pick:{bondIndex}", count);

    // ── Seeded primitives (the HullShudder idiom: one large-faced die off the shared rule). ────────────

    private const int Resolution = 4096;

    // A uniform [0,1) sample off the ONE shared DiceRule, salted by the purpose tag so the gate, outcome,
    // line and pick streams are independent — platform-stable and replayable (no System.Random, no clock).
    private static double Fraction(ulong seed, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed(seed, tag), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }

    // A deterministic index into a pool of the given size, salted by the purpose tag off the shared rule.
    private static int Index(ulong seed, string tag, int count) =>
        count <= 1 ? 0 : (int)(Fraction(seed, tag) * count) % count;
}
