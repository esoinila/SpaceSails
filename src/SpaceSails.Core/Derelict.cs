namespace SpaceSails.Core;

/// <summary>
/// #488 · THE WRECK — a lost ship, why it was lost, and the choice of what to do about it.
///
/// <para>Owner, 2026-07-28: <i>"let's make the wreck case. A kind of salvage run and exploration. We need
/// some reason why is hard to find … maybe a lost ship due to some malfunction with valuable cargo. I
/// think the accident investigation might be something we could even get paid from … say 10% of the value
/// or we might want to loot it all and never tell anyone. That option might be fun to have. Honest salvage
/// and contacts that may provide in future or fast win immediately. I guess there are many things that
/// could have happened :-D"</i></para>
///
/// <para><b>Why it is hard to find</b> is the physics, not a dice roll. A ship that dies under way does not
/// stop — it keeps the velocity it had when the lights went out, and coasts. Every year since widens the
/// cone of places it could be, because the last position anyone logged has been multiplied by an unknown
/// drift for a very long time. <see cref="SearchConeRadiusMeters"/> prices that honestly: the haystack is
/// the last known vector times the years, and THAT is why nobody has collected the cargo.</para>
///
/// <para><b>The two endings</b> are the fiction the owner asked for. FILE THE REPORT and you are an
/// accident investigator: a finder's fee of <see cref="ReportFeeFraction"/> of the assessed value, a bonus
/// for actually reading the wreck correctly, and — the part that outlives the payout — a CONTACT who
/// remembers. STRIP IT and say nothing and you take everything today, but it is someone's insured cargo,
/// so it is hot, and nobody ever thanks you for a ship that was never found.</para>
///
/// <para>Pure and deterministic — every wreck is a seeded read of its id, so a given wreck is always the
/// same wreck and a test can pin it. Determinism is law in Core.</para>
/// </summary>
public static partial class Derelict
{
    /// <summary>Every wreck body's id starts with this, so the whole client — the boarding board, the deck
    /// builder, the excursion — can route a derelict by id alone, the same trick the expedition sites and
    /// the deflection rock use. A wreck is a boardable SITE, not a world, and this is what says so.</summary>
    public const string BodyIdPrefix = "wreck-";

    /// <summary>The body id a given wreck spawns under.</summary>
    public static string BodyIdFor(string wreckId) => BodyIdPrefix + (wreckId ?? string.Empty);

    /// <summary>Is this body a derelict, and if so which one? The single predicate the client branches on.</summary>
    public static bool TryParseWreckId(string? bodyId, out string wreckId)
    {
        if (bodyId is not null && bodyId.StartsWith(BodyIdPrefix, System.StringComparison.Ordinal)
            && bodyId.Length > BodyIdPrefix.Length)
        {
            wreckId = bodyId[BodyIdPrefix.Length..];
            return true;
        }

        wreckId = string.Empty;
        return false;
    }

    /// <summary>#241 · The scenario's own hand-authored wreck — the lost roadster the fetch job is sent
    /// after. It predates <see cref="BodyIdPrefix"/> and so does not carry it, which is why the client had
    /// the id typed into five files and no predicate could answer "is that body a wreck".</summary>
    public const string RoadsterBodyId = "derelict-roadster";

    /// <summary>#238 · <b>WHERE SHE IS, IN THREE WORDS, ONCE.</b> The roadster's bearing phrase — the
    /// owner's own — and the one the transponder fix, the ledger's job line, the Fixer's offer blurb and the
    /// test start have each been carrying as their own typed literal. #238's reveal headline wanted a fifth
    /// copy and got this instead: where she is is a FACT about the scenario's wreck, and a fact lives in one
    /// place (§5). Nothing computes it — she is hand-placed sunward of Mars — which is exactly why it must
    /// not be re-typed: four literals are four chances for the sentence and the map to part company.</summary>
    public const string RoadsterBearingPhrase = "sunward of Mars";

    /// <summary>#238 · What the found-her beat calls her out loud. The owner's own headline names <i>the
    /// roadster</i> and not the body's charted name ("Derelict Roadster") — the moment is a shout across a
    /// bridge, not a label on a chart — so the two are kept apart deliberately.</summary>
    public const string RoadsterRevealName = "the roadster";

    /// <summary>#238 item 3 · What the telescope's own complaint calls the aimed job that is waiting on her.
    /// The queue's label for that job is built from the intel card's headline (<i>"intel fix · 🔭 Roadster
    /// orbit fix"</i>) and the owner's sentence is shorter than that on purpose — <i>"sweep holds the scope —
    /// 🔭 Roadster fix waits behind it"</i> — because a line that also has to fit on a desk chip is not a
    /// queue row. Spelled here beside <see cref="RoadsterRevealName"/> for the same reason that one is: a name
    /// the owner gave her is a FACT about the scenario's wreck, and a fact lives in one place (§5).</summary>
    public const string RoadsterScopeJobName = "🔭 Roadster fix";

    /// <summary>#238 · What came out of her, in the owner's own words: <i>"💾 GOT IT — the wallet, from
    /// between the seats"</i>. The same closing words the fetch's own receipt has said since PR-A; spelled
    /// here so the beat and the receipt cannot drift into two accounts of one hand in one gap.</summary>
    public const string WalletBetweenTheSeats = "the wallet, from between the seats";

    /// <summary>#241 · IS THIS BODY A DEAD HULL? Every generated wreck (<see cref="BodyIdPrefix"/>) and the
    /// scenario's own roadster. One question, one answer — so an instrument that must not call a three-metre
    /// wreck a planet, and a code path that must not promise a clamp on one, are asking the same thing.</summary>
    public static bool IsWreckBody(string? bodyId) =>
        TryParseWreckId(bodyId, out _)
        || string.Equals(bodyId, RoadsterBodyId, System.StringComparison.Ordinal);

    /// <summary>The finder's fee for filing an honest accident report, as a fraction of the assessed cargo
    /// value (owner: <i>"say 10% of the value"</i>). FLAGGED for tuning.</summary>
    public const double ReportFeeFraction = 0.10;

    /// <summary>Read the wreck RIGHT — name the cause the evidence actually supports — and the report is
    /// worth more than a finder's fee, because an insurer will pay for a cause they can act on. A fraction
    /// of the assessed value, on top. FLAGGED for tuning.</summary>
    public const double CorrectCauseBonusFraction = 0.05;

    /// <summary>Catching a STAGED loss (<see cref="WreckCause.InsuranceJob"/>) and filing it is the big
    /// one — you have handed an underwriter a fraud they were about to pay out on. FLAGGED for tuning.</summary>
    public const double FraudBountyFraction = 0.35;

    /// <summary>Stripping a wreck and saying nothing yields the cargo's full market value — but it is
    /// someone's INSURED cargo and it is now aboard your ship. This much heat, immediately.</summary>
    public const int QuietSalvageHeat = 2;

    /// <summary>How far the drift cone opens per year adrift, in metres — the ship's last logged velocity
    /// carried by an unknown fraction. This is the whole reason a wreck stays lost: the haystack grows
    /// with the calendar. FLAGGED for tuning.</summary>
    public const double DriftConePerYearMeters = 2.5e9;

    /// <summary>The radius of the volume the wreck could be in, given how long it has been adrift. Grows
    /// linearly with the years — the last fix ages into a guess.</summary>
    public static double SearchConeRadiusMeters(double yearsAdrift) =>
        System.Math.Max(0.0, yearsAdrift) * DriftConePerYearMeters;

    // ── #652 · THE HALF THAT OUTLIVED NOTHING ────────────────────────────────────────────────────────
    //
    // Owner's own framing of why the honest road exists (2026-07-28): "Honest salvage and CONTACTS THAT MAY
    // PROVIDE IN FUTURE, or fast win immediately." Four of the five things a decision spends were real and
    // spendable — credits, heat, hot cargo, the crew's opinion. The fifth was a boolean nobody read: the
    // card said "somebody now owes you a straight answer" and there was no somebody. A promise the game
    // cannot name is a promise it has not made, and a player who works that out has been handed the answer
    // to the one decision in the lane that was supposed to be hard.
    //
    // This is #652's option 1, the cheapest of the three it lays out, and deliberately ONLY that: the
    // contact gets a NAME and goes on the ledger the game already keeps. No new behaviour, no new
    // reputation track, no rebalanced fractions (those are FLAGGED separately and are not this lane's).
    // What it buys is that the sentence stops being a lie and the count becomes a thing the player watches
    // grow — and, being on the ContactLedger, it round-trips through the vault for free.

    /// <summary>What one straight filing is worth as goodwill, booked through the ledger's existing
    /// <c>AddGoodwill</c> seam — the same non-transactional warming a round at the bar buys. Small on
    /// purpose: this is somebody remembering you favourably, not a favour already owed.</summary>
    public const int ContactGoodwill = 2;

    /// <summary>The people who countersign findings. Shout-names in the ledger's own idiom ("MADAM COIL",
    /// "THE FIXER"), because that is the key <see cref="ContactLedger"/> and the bar consoles share. They
    /// are assessors and adjusters rather than characters: an honest filing earns you a professional who
    /// remembers, and the game does not yet claim more than that.</summary>
    private static readonly (string Id, string Name)[] Assessors =
    [
        ("ASSESSOR PRYNNE", "Assessor Prynne"),
        ("ASSESSOR VEKKONEN", "Assessor Vekkonen"),
        ("ADJUSTER HALLORAN", "Adjuster Halloran"),
        ("ASSESSOR IGE", "Assessor Ige"),
        ("ADJUSTER SARTO", "Adjuster Sarto"),
        ("ASSESSOR BAKHTIAR", "Assessor Bakhtiar"),
    ];

    // ── The seeded wreck ──────────────────────────────────────────────────────────────────────────────

    private static readonly string[] Names =
    [
        "Kestrel's Promise", "Anna Vale", "Long Shrift", "Ptarmigan", "Second Marriage",
        "Cold Harvest", "Understudy", "Tenth of June", "Marbury", "Quiet Sister",
    ];

    /// <summary>#533 · THE FLOOR OF WHAT A WRECK'S CARGO IS ASSESSED AT — the bottom of the span
    /// <see cref="Seeded"/> deals inside. It was a literal in that signature, which is a fine place for a
    /// default and a poor place for a FACT: anything that wants to say what a RICH hull is has to ask where
    /// the span begins, and a reader carrying its own copy of the number would drift away from the span the
    /// first day either was tuned. FLAGGED for tuning.</summary>
    public const int AssessedFloorCr = 40_000;

    /// <summary>…and the top of it, for <see cref="AssessedFloorCr"/>'s reason. FLAGGED for tuning.</summary>
    public const int AssessedCeilingCr = 320_000;

    /// <summary>Build the wreck a given id names — the same id always yields the same ship, the same cause
    /// and the same cargo, so a rumour that names her can be trusted and a test can pin her.</summary>
    public static Wreck Seeded(string id, int minValueCr = AssessedFloorCr, int maxValueCr = AssessedCeilingCr)
    {
        string key = id ?? string.Empty;
        ulong h = StableHash.Of(key);

        var causes = System.Enum.GetValues<WreckCause>();
        WreckCause cause = causes[(int)(h % (ulong)causes.Length)];

        string name = Names[(int)((h / 7) % (ulong)Names.Length)];

        int span = System.Math.Max(1, maxValueCr - minValueCr);
        int value = minValueCr + (int)((h / 13) % (ulong)span);

        // Between three and forty-odd years out here. Long enough that the search cone is the real
        // obstacle, and long enough that whoever filed the original loss has stopped hoping.
        double years = 3.0 + ((h / 17) % 40);

        return new Wreck(key, name, cause, value, years);
    }

    /// <summary>Find a seeded wreck that died a GIVEN way — the dev hook behind <c>?wreck=&lt;cause&gt;</c>,
    /// so a playtester can board an infested hull (or a staged loss) on purpose instead of re-rolling ids
    /// until one turns up. Walks a deterministic sequence, so the same cause always yields the same ship.
    /// Returns null only if the cause is somehow unreachable from the seeding, which a test pins.</summary>
    public static Wreck? SeededWithCause(WreckCause cause, int searchLimit = 400)
    {
        for (int i = 0; i < searchLimit; i++)
        {
            Wreck w = Seeded($"lost-{i}");
            if (w.Cause == cause)
            {
                return w;
            }
        }
        return null;
    }
}
