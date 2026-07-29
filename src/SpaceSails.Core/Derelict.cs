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
public static class Derelict
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

    /// <summary>What happened to her. Owner: <i>"I guess there are many things that could have
    /// happened :-D"</i> — so the taxonomy is the content, and each one leaves a DIFFERENT wreck to walk
    /// through.</summary>
    public enum WreckCause
    {
        /// <summary>The drive quit mid-transfer. Nothing burned, nothing breached — she simply stopped
        /// being able to arrive, and everyone aboard had a long time to think about it.</summary>
        DriveFailure,

        /// <summary>The reactor ran away. The aft third is slag and the radiation is still interesting.</summary>
        ReactorCascade,

        /// <summary>Something small and fast went through her at closing speed. A hole you can see the
        /// stars through, and a decompression that took the crew before the alarm finished.</summary>
        HullBreach,

        /// <summary>The air plant failed and could not be fixed. The ship is INTACT — that is the horror of
        /// it. Everything works except the one thing that mattered.</summary>
        LifeSupportFailure,

        /// <summary>Somebody put a decimal in the wrong place and the arrival burn never came. She is
        /// exactly where the arithmetic said she would be, which is nowhere anyone was looking.</summary>
        NavigationalError,

        /// <summary>The crew fell out. Two factions, two barricades, and a ship that stopped being flown
        /// while they settled it.</summary>
        Mutiny,

        /// <summary>Boarded, stripped of what was easy, and left under way. Whoever did it was in a hurry —
        /// the valuable cargo is still in the deep hold.</summary>
        Piracy,

        /// <summary>SHE IS NOT EMPTY. Owner: <i>"there might be an infested ship where the cannons are
        /// needed also :-D … we should have a cannon in the airlock there to cover the retreat."</i>
        ///
        /// <para>Nothing failed aboard her — everything worked right up until something got in. The crew's
        /// barricades were built from the INSIDE, which is exactly what a mutiny looks like from a distance
        /// (see <see cref="MisreadsAs"/>), and that misreading is the most expensive one in the game: name
        /// it a mutiny and you file a report that sends the next crew in unarmed.</para></summary>
        Infested,

        /// <summary>She was LOST ON PURPOSE. The cargo was over-insured, the distress call was scripted, and
        /// the crew were taken off before she was set adrift. The most valuable thing aboard is the
        /// evidence (see <see cref="FraudBountyFraction"/>).</summary>
        InsuranceJob,

        /// <summary>SHE WAS VENTED FROM THE INSIDE, BY ONE OF HER OWN. Owner: <i>"one ship destiny might
        /// be that somebody went crazy due to an object and vented most of the ship and the survivors also
        /// fell to some other craziness after that."</i>
        ///
        /// <para>Someone aboard stood too long beside the thing in her hold, walked aft to the valve board,
        /// and blew her compartment by compartment with her crew inside. The doors were thrown from the
        /// SPINE side, not barricaded from within — and exactly ONE compartment was left with air in it,
        /// because <see cref="HullVenting.Readiness"/> will not blow the room you are standing in. The
        /// interlock the player knows from their own hands, read years later as a confession.</para>
        ///
        /// <para>Then the second madness: the few who lived through their own ship kept her log for months
        /// afterwards in an immaculate administrative hand — watch rotations, meal counts, forty names
        /// signing on and off a ship with nobody aboard. In the log, nothing ever happened.</para></summary>
        VentedByOneOfTheirOwn,
    }

    /// <summary>The headline the wreck reads as once the cause is known.</summary>
    public static string CauseHeadline(WreckCause cause) => cause switch
    {
        WreckCause.DriveFailure => "the drive quit and she never arrived",
        WreckCause.ReactorCascade => "the reactor ran away with her",
        WreckCause.HullBreach => "something small went through her at speed",
        WreckCause.LifeSupportFailure => "the air plant failed and everything else kept working",
        WreckCause.NavigationalError => "the arrival burn was never coming",
        WreckCause.Mutiny => "the crew stopped flying her to settle something",
        WreckCause.Piracy => "she was boarded, stripped in a hurry, and left under way",
        WreckCause.Infested => "she is not empty",
        WreckCause.InsuranceJob => "she was lost on purpose",
        WreckCause.VentedByOneOfTheirOwn => "one of her own opened her to space, compartment by compartment",
        _ => "",
    };

    /// <summary>The wreck's portrait — a Grok canvas per cause, because eight ships that died eight
    /// different ways should not all look the same. Shown when the away team reads the cause's own station
    /// (you are looking at the thing) and again on the decision card (you are deciding about it).
    ///
    /// <para>The art shows the EVIDENCE, never the conclusion: empty lifeboat cradles, not a caption saying
    /// "fraud". Naming what it means is still the captain's job, and a wreck that lies still lies.</para>
    ///
    /// <para>Every slot degrades cleanly — the client's img onerror-hides — so an unpainted cause reads as
    /// text alone rather than a broken frame.</para></summary>
    public static string ArtFile(WreckCause cause) => cause switch
    {
        WreckCause.DriveFailure => "art/wreck-drive-failure.jpg",
        WreckCause.ReactorCascade => "art/wreck-reactor-cascade.jpg",
        WreckCause.HullBreach => "art/wreck-hull-breach.jpg",
        WreckCause.LifeSupportFailure => "art/wreck-life-support.jpg",
        WreckCause.NavigationalError => "art/wreck-navigational-error.jpg",
        WreckCause.Mutiny => "art/wreck-mutiny.jpg",
        WreckCause.Piracy => "art/wreck-piracy.jpg",
        WreckCause.Infested => "art/wreck-infested.jpg",
        WreckCause.InsuranceJob => "art/wreck-insurance-job.jpg",
        WreckCause.VentedByOneOfTheirOwn => "art/wreck-vented.jpg",
        _ => "",
    };

    /// <summary>What the away team actually FINDS aboard — the investigation's raw material. Deliberately
    /// physical: the report is written from what is bolted to the deck, not from a tooltip.</summary>
    public static string Evidence(WreckCause cause) => cause switch
    {
        WreckCause.Infested =>
            "every barricade aboard was built from the INSIDE, the arms lockers are open and spent, and something has been nesting in the deep hold for a very long time",
        WreckCause.DriveFailure =>
            "the drive bells are cold and clean, the fuel gauges read FULL, and the log's last hundred entries are all the same failed restart",
        WreckCause.ReactorCascade =>
            "the aft third is slag, the shielding is peeled outward, and the dosimeters by the door are still counting",
        WreckCause.HullBreach =>
            "a hole the width of a thumb through four decks in a straight line, and every locker on that line blown open from inside",
        WreckCause.LifeSupportFailure =>
            "the scrubber stacks are choked solid, every other system reads nominal, and the bunks were made up",
        WreckCause.NavigationalError =>
            "a flight plan pinned at the nav post with the arrival burn worked twice, in two different hands, to two different answers",
        WreckCause.Mutiny =>
            "two barricades facing each other down one corridor, and the arms locker opened with a cutting torch",
        WreckCause.Piracy =>
            "the near hold stripped to bare frames, the deep hold untouched, and the airlock cycled from outside",
        WreckCause.InsuranceJob =>
            "the lifeboat cradles are empty and their release logs predate the distress call, the manifest is countersigned twice, and the cargo seals were opened and re-set",
        WreckCause.VentedByOneOfTheirOwn =>
            "every door was thrown from the SPINE side and every compartment but one is frosted hard vacuum — and the log runs on for months after that, in one immaculate hand, signing forty names on and off watch",
        _ => "",
    };

    /// <summary>
    /// HOW MANY BOATS LEFT HER — the count you take off the wall. Gated by what killed her, because whether
    /// anybody got off is one of the loudest facts a wreck has, and the causes already disagree about it:
    ///
    /// <list type="bullet">
    /// <item><b>InsuranceJob</b> — every cradle empty, and the clamps neatly stowed rather than blown.
    /// Nobody left in a hurry. This is already what <see cref="Evidence"/> says about her.</item>
    /// <item><b>Infested / VentedByOneOfTheirOwn / LifeSupportFailure</b> — NONE. Nobody got off, which
    /// means everybody aboard is still aboard, and the away team is walking through the reason.</item>
    /// <item><b>Mutiny</b> — some. One side left; the other did not.</item>
    /// <item><b>Everything else</b> — seeded, because "did anyone make it" should not be predictable from
    /// the cause alone.</item>
    /// </list>
    ///
    /// <para>Deterministic off the wreck id, so the same hull always reads the same from the doorway.</para>
    /// </summary>
    public static int LifeboatsLaunched(string wreckId, WreckCause cause, int cradles)
    {
        switch (cause)
        {
            case WreckCause.InsuranceJob:
                return cradles;                 // all of them, and tidily
            case WreckCause.Infested:
            case WreckCause.VentedByOneOfTheirOwn:
            case WreckCause.LifeSupportFailure:
                return 0;                       // nobody got off
            case WreckCause.Mutiny:
                return System.Math.Max(1, cradles / 3);
            default:
                ulong h = StableHash.Of($"{wreckId}|cradles");
                return (int)(h % (ulong)(cradles + 1));
        }
    }

    /// <summary>What the row of cradles says, in the house voice: the count, never the conclusion. "Every
    /// one of them gone" and "not one of them launched" are the same sentence structure on purpose — the
    /// captain decides which is worse.</summary>
    public static string CradleReading(int launched, int cradles)
    {
        int left = cradles - launched;

        if (launched == 0)
        {
            return $"All {cradles} cradles are still occupied. Not one boat left this ship. Whatever " +
                   "happened aboard her, nobody aboard decided it was worth leaving over — or nobody was " +
                   "left who could.";
        }
        if (left == 0)
        {
            return $"All {cradles} cradles are empty, and the release clamps are stowed rather than blown. " +
                   "Everybody got off, and they did it without hurrying.";
        }
        return $"{launched} of {cradles} cradles are empty; {left} boat{(left == 1 ? " is" : "s are")} still " +
               "clamped in. Somebody left. Somebody did not.";
    }

    /// <summary>Some wrecks lie. A cause the evidence can be honestly MISREAD as — the wrong answer an
    /// investigator would reach in a hurry, which is what makes reading one a skill and not a lookup.
    /// Null when the wreck is unambiguous.</summary>
    public static WreckCause? MisreadsAs(WreckCause cause) => cause switch
    {
        // A staged loss is DRESSED as an ordinary failure — that is the entire point of staging it.
        WreckCause.InsuranceJob => WreckCause.DriveFailure,
        // A breach and a runaway both end in a hull nobody wants to stand in.
        WreckCause.ReactorCascade => WreckCause.HullBreach,
        // An intact ship with a dead crew reads as a mutiny to anyone who wants a story.
        WreckCause.LifeSupportFailure => WreckCause.Mutiny,
        // Barricades built from the INSIDE look exactly like a crew that fell out — and this is the most
        // expensive misreading in the game. File "mutiny" on an infested hull and the next crew goes in
        // unarmed on the strength of your report.
        WreckCause.Infested => WreckCause.Mutiny,
        // A ship that never burned looks like a ship that could not.
        WreckCause.NavigationalError => WreckCause.DriveFailure,
        // Doors shut, air gone: that reads as the air plant failing, and it reads that way easily. Unlike
        // every other entry here, this misreading is not a mistake the WRECK makes — the evidence is plain
        // at the valve board. It is the one the CAPTAIN is invited to make, because naming it truly means
        // naming a dead crewmember on circumstantial evidence, while the comfortable answer pays the same
        // and leaves the thing in her hold out of the record entirely.
        WreckCause.VentedByOneOfTheirOwn => WreckCause.LifeSupportFailure,
        _ => null,
    };

    /// <summary>One found wreck: who she was, what she was carrying, what happened, and how long she has
    /// been out here. <paramref name="AssessedValueCr"/> is the cargo's assessed worth — the number both
    /// endings are priced against.</summary>
    public readonly record struct Wreck(
        string Id,
        string ShipName,
        WreckCause Cause,
        int AssessedValueCr,
        double YearsAdrift)
    {
        /// <summary>The volume she could be hiding in, given the years — see
        /// <see cref="SearchConeRadiusMeters"/>.</summary>
        public double SearchConeMeters => SearchConeRadiusMeters(YearsAdrift);
    }

    /// <summary>What the captain decided to do about her.</summary>
    public enum SalvageChoice
    {
        /// <summary>File the accident report: the finder's fee, a bonus for reading her right, and a contact
        /// who remembers you were straight with them.</summary>
        FileTheReport,

        /// <summary>Strip her and say nothing. Everything, today — and it is all hot.</summary>
        StripAndSayNothing,
    }

    /// <summary>What a decision actually pays. <paramref name="ContactEarned"/> is the part that outlives
    /// the payout: an honest filing makes somebody who will take your call later (owner: <i>"contacts that
    /// may provide in future or fast win immediately"</i>).</summary>
    public readonly record struct SalvageOutcome(
        int CreditsNow,
        int HeatGained,
        bool ContactEarned,
        bool CargoIsHot,
        string Line);

    /// <summary>
    /// Price the captain's decision.
    ///
    /// <para>FILE THE REPORT pays <see cref="ReportFeeFraction"/> of the assessed value, plus
    /// <see cref="CorrectCauseBonusFraction"/> when <paramref name="reportedCause"/> matches what actually
    /// happened, plus <see cref="FraudBountyFraction"/> when the captain correctly names a staged loss —
    /// an underwriter pays real money for a claim they no longer have to honour. It earns a contact and
    /// takes no heat. Naming the WRONG cause still pays the finder's fee (you did find her) but earns no
    /// bonus and no contact: a bad report is worse than no report.</para>
    ///
    /// <para>STRIP AND SAY NOTHING pays the whole assessed value immediately and earns nothing else. The
    /// cargo is insured, so it is hot (<see cref="QuietSalvageHeat"/>), and a wreck that was never found
    /// makes no friends.</para>
    ///
    /// <para>Pure: same wreck + same choice + same reported cause → same outcome, always.</para>
    /// </summary>
    public static SalvageOutcome Resolve(in Wreck wreck, SalvageChoice choice, WreckCause? reportedCause)
    {
        int value = System.Math.Max(0, wreck.AssessedValueCr);

        if (choice == SalvageChoice.StripAndSayNothing)
        {
            return new SalvageOutcome(
                CreditsNow: value,
                HeatGained: QuietSalvageHeat,
                ContactEarned: false,
                CargoIsHot: true,
                Line: $"The {wreck.ShipName} was never found. Nobody thanks you, and nobody comes looking — yet.");
        }

        int fee = (int)System.Math.Round(value * ReportFeeFraction);
        bool readRight = reportedCause == wreck.Cause;

        if (!readRight)
        {
            return new SalvageOutcome(
                CreditsNow: fee,
                HeatGained: 0,
                ContactEarned: false,
                CargoIsHot: false,
                Line: $"You filed on the {wreck.ShipName}, and the fee cleared. The finding did not survive review — " +
                      "a wrong cause helps nobody, and they will remember that too.");
        }

        bool fraud = wreck.Cause == WreckCause.InsuranceJob;
        int bonus = (int)System.Math.Round(value * (fraud ? FraudBountyFraction : CorrectCauseBonusFraction));

        return new SalvageOutcome(
            CreditsNow: fee + bonus,
            HeatGained: 0,
            ContactEarned: true,
            CargoIsHot: false,
            Line: fraud
                ? $"You filed on the {wreck.ShipName} and named it staged. An underwriter who was about to pay out " +
                  "instead pays YOU, and will want to know what else you have seen."
                : $"You filed on the {wreck.ShipName} and read her right. The fee cleared, the finding stood, and " +
                  "somebody now owes you a straight answer.");
    }

    /// <summary>The two roads, stated plainly for the choice card — the honest one and the quiet one.
    /// Quotes the actual numbers so the captain is never guessing what they are choosing between.</summary>
    public static string DescribeChoice(in Wreck wreck, SalvageChoice choice)
    {
        int value = System.Math.Max(0, wreck.AssessedValueCr);
        return choice switch
        {
            SalvageChoice.FileTheReport =>
                $"File it: {(int)System.Math.Round(value * ReportFeeFraction):N0} cr finder's fee, more if you read her " +
                "right, and somebody who takes your call afterwards.",
            SalvageChoice.StripAndSayNothing =>
                $"Strip her: {value:N0} cr today, all of it hot, and the {wreck.ShipName} stays lost.",
            _ => "",
        };
    }

    // ── The seeded wreck ──────────────────────────────────────────────────────────────────────────────

    private static readonly string[] Names =
    [
        "Kestrel's Promise", "Anna Vale", "Long Shrift", "Ptarmigan", "Second Marriage",
        "Cold Harvest", "Understudy", "Tenth of June", "Marbury", "Quiet Sister",
    ];

    /// <summary>Build the wreck a given id names — the same id always yields the same ship, the same cause
    /// and the same cargo, so a rumour that names her can be trusted and a test can pin her.</summary>
    public static Wreck Seeded(string id, int minValueCr = 40_000, int maxValueCr = 320_000)
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
