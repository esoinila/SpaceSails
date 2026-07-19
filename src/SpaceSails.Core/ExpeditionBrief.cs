namespace SpaceSails.Core;

/// <summary>
/// #370 — THE RESEARCH BRIEF and THE REVEAL. The owner's ruling (cruise dessert buffet, 2026-07-19,
/// verbatim): <b>"Maybe they could have some kind of research brief with gen-ai image plus some text.
/// Some usually totally optimistic and sugarcoated version brief that is told to the charter service. 🤭
/// It is a classic in every scifi movie… they expect to find the Engineers in the Prometheus but find a
/// bio-weapons lab planet where things have gone horribly wrong. It is its own kind of horror, a sanity
/// throw moment for players, when some bigger picture is revealed during the expedition."</b>
///
/// <para>THE BRIEF is the lie: a charter-service sales pitch, shamelessly optimistic corporate copy per
/// site kind, where the fine-print hedge words ("(pending)", "allegedly", "no incidents on record*") ARE
/// the joke. THE REVEAL is the horror: once per gig, at a seeded beat mid-excursion, the bigger picture
/// surfaces in the site's OWN voice and contradicts the brief — the henge stones are warnings not
/// monuments, the hull was scuttled from inside not crashed, the tomb was built to keep something IN not
/// robbers out. It is a MAJOR sanity-throw (a bigger <see cref="NerveModel.Shock"/> than the horror band),
/// after which the on-site table DARKENS, and surviving past it earns a "the truth is worth more" bonus.</para>
///
/// <para>Every string, the reveal timing, and the darkening/payout deltas are pure and seeded here (repo
/// law §9 — determinism is law in Core); the client stays a thin card renderer. Pinned by tests so the
/// owner can read the copy off the suite.</para>
/// </summary>
public static class ExpeditionBrief
{
    // ── The art (delivered by Grok into wwwroot/art; the client falls back to a gradient if absent). ──

    /// <summary>The charter-brief hero image filename for a site kind (in <c>wwwroot/art/</c>). The client
    /// layers a deterministic gradient UNDER it, so a missing asset still reads as a tinted card.</summary>
    public static string ArtFile(ExpeditionSiteKind kind) => kind switch
    {
        ExpeditionSiteKind.CrashedHull => "brief-wreck.jpg",
        ExpeditionSiteKind.SealedTunnel => "brief-tunnel.jpg",
        _ => "brief-henge.jpg",
    };

    /// <summary>The charter-service masthead over the brief — the reassuring product name it was sold under.</summary>
    public static string Title(ExpeditionSiteKind kind) => kind switch
    {
        ExpeditionSiteKind.CrashedHull => "CHARTER BRIEF · Vintage Hull Salvage Stroll™",
        ExpeditionSiteKind.SealedTunnel => "CHARTER BRIEF · Heritage Tomb Open-House™",
        _ => "CHARTER BRIEF · Pre-Human Heritage Survey™",
    };

    // ── THE BRIEF pools — the shamelessly optimistic corporate copy (the hedge words are the joke). ──

    private static readonly string[] HengeBriefs =
    [
        "A routine heritage survey of some charming pre-human masonry. Bring a camera — the standing " +
        "stones photograph beautifully at local dawn! The sponsor's actuaries rate this site Fully " +
        "Benign (pending).",

        "Think of it as a weekend at an open-air museum that history forgot to build. The stones are " +
        "almost certainly decorative, and any humming you hear is well within recommended exposure " +
        "limits.* No incidents on record (records begin next quarter).",

        "Our geologists call it 'a quaint circle of rocks'; our marketing team calls it 'the find of the " +
        "century, allegedly.' Either way it's a gentle stroll with excellent acoustics. Hazard rating: adorable.",
    ];

    private static readonly string[] WreckBriefs =
    [
        "A tidy salvage stroll around a vintage hull that simply parked itself — firmly — in the regolith. " +
        "The previous crew disembarked in good order and definitely elsewhere. Structurally sound-ish and " +
        "rated Cozy by our optimists.",

        "One lightly-used starship, one careful owner, sold as-seen. The airlocks were left open for your " +
        "convenience and the interior smells of adventure (and, allegedly, nothing else). Fully Benign " +
        "(pending survey).",

        "A charming crashed-ship experience for the whole away team! The scorch marks are purely aesthetic " +
        "and the distress logs are, our lawyers assure us, unrelated. No incidents on record.*",
    ];

    private static readonly string[] TunnelBriefs =
    [
        "A sealed heritage tunnel, freshly opened just for you — think wine cellar, but ancient and " +
        "morally uncomplicated. The residents are extremely restful and the door was clearly built to " +
        "keep the weather out. Rated Serene.",

        "A once-in-an-epoch chance to tour a tomb that history left tastefully locked. The occupants are " +
        "past caring and the seals are purely traditional, not structural.* Actuarial verdict: Fully " +
        "Benign (pending).",

        "Down you go into a cosy stone corridor lined with the peacefully departed — a spa day for the " +
        "intellectually curious. Any scratching on the inside of the door is, allegedly, decorative. No " +
        "incidents on record (yet!).",
    ];

    /// <summary>The optimistic charter copy for this gig — seeded so the same struck mission always reads
    /// the same sales pitch, drawn from the kind's pool.</summary>
    public static string BriefFor(ExpeditionSiteKind kind, double acceptedSimTime, string siteBodyId)
    {
        string[] pool = BriefPool(kind);
        ulong seed = AwayExpeditionEvents.Seed(acceptedSimTime, siteBodyId, BriefCopySalt);
        return pool[new DeterministicRandom(seed).NextInt(0, pool.Length)];
    }

    private static string[] BriefPool(ExpeditionSiteKind kind) => kind switch
    {
        ExpeditionSiteKind.CrashedHull => WreckBriefs,
        ExpeditionSiteKind.SealedTunnel => TunnelBriefs,
        _ => HengeBriefs,
    };

    // ── THE REVEAL — the bigger picture, in the site's own voice, that contradicts the brief. ──

    /// <summary>The nerve lump the reveal drives through <see cref="NerveModel.Shock"/> — deliberately
    /// BIGGER than the horror band's <see cref="NerveModel.MonolithSightShock"/> (24): this is the major
    /// sanity-throw the owner asked for, "a bigger picture revealed during the expedition."</summary>
    public const double RevealShock = 36.0;

    /// <summary>The earliest / latest on-site beat ordinal the reveal can land on — mid-gig, never the
    /// first beat (let the brief's lie breathe) and never so late the window strands you first. OWNER-TUNABLE.</summary>
    public const int RevealMinOrdinal = 2;
    public const int RevealMaxOrdinal = 4;

    // Salts that fold accept-moment + site into stable, collision-free seeds via AwayExpeditionEvents.Seed
    // (real beat ordinals are 0..~7 and the stranding toll uses 9001 — these negatives never collide).
    private const int BriefCopySalt = -8801;
    private const int RevealTimingSalt = -8701;
    private const int RevealCopySalt = -8901;

    /// <summary>The seeded beat ordinal at which the reveal fires for this gig — a pure function of when and
    /// where the mission was struck, so client and any replay agree on the moment the horror lands.</summary>
    public static int RevealOrdinal(double acceptedSimTime, string siteBodyId)
    {
        ulong seed = AwayExpeditionEvents.Seed(acceptedSimTime, siteBodyId, RevealTimingSalt);
        return new DeterministicRandom(seed).NextInt(RevealMinOrdinal, RevealMaxOrdinal + 1);
    }

    /// <summary>True if <paramref name="ordinal"/> is the gig's seeded reveal beat.</summary>
    public static bool IsRevealBeat(double acceptedSimTime, string siteBodyId, int ordinal) =>
        ordinal == RevealOrdinal(acceptedSimTime, siteBodyId);

    private static readonly RevealCopy[] HengeReveals =
    [
        new("👁 THE STONES ARE NOT MONUMENTS.",
            "The alignment only reads from the inside — and it is not a calendar. Every stone is a warning " +
            "post, and they all point inward at the thing they were raised to keep watched. The survey is " +
            "standing in the middle of the fence."),

        new("👁 THE CIRCLE IS A CAGE, SEEN FROM WITHIN.",
            "These were never raised to be admired. The masons carved the same word on every face, over and " +
            "over, and the closest the team can render it is DO NOT WAKE IT. The humming has changed pitch " +
            "since you arrived."),
    ];

    private static readonly RevealCopy[] WreckReveals =
    [
        new("🩸 THE HULL DID NOT CRASH.",
            "The breaches all bloom outward. The airlocks were blown from the inside, and the last logs are " +
            "not a mayday — they are a quarantine order the crew wrote against themselves. Whatever they were " +
            "running from, they made sure it went down with them. Here."),

        new("🩸 THEY SCUTTLED IT ON PURPOSE.",
            "No impact ever bent metal this way. Someone flooded the reactor and welded the doors from within, " +
            "and the scorch on the walls is a message, not damage. The away team is walking through a grave " +
            "that was dug to stay shut."),
    ];

    private static readonly RevealCopy[] TunnelReveals =
    [
        new("⛓ THE DOOR LOCKS FROM THE OUTSIDE.",
            "The seals, the wards, the weight of the stone — none of it was built to keep robbers out. It was " +
            "built to keep the occupants IN. And the team has just opened it from the wrong side. The tomb " +
            "was a lid."),

        new("⛓ THIS WAS NEVER A TOMB.",
            "The 'peacefully departed' were the jailers, and they died at their posts holding the door. Every " +
            "scratch on the inner face was made by something trying to get out — recently. The survey didn't " +
            "break in. It let something out."),
    ];

    /// <summary>The reveal copy for this gig — seeded, drawn from the kind's pool, contradicting the brief in
    /// the site's own voice.</summary>
    public static RevealCopy RevealFor(ExpeditionSiteKind kind, double acceptedSimTime, string siteBodyId)
    {
        RevealCopy[] pool = RevealPool(kind);
        ulong seed = AwayExpeditionEvents.Seed(acceptedSimTime, siteBodyId, RevealCopySalt);
        return pool[new DeterministicRandom(seed).NextInt(0, pool.Length)];
    }

    private static RevealCopy[] RevealPool(ExpeditionSiteKind kind) => kind switch
    {
        ExpeditionSiteKind.CrashedHull => WreckReveals,
        ExpeditionSiteKind.SealedTunnel => TunnelReveals,
        _ => HengeReveals,
    };
}

/// <summary>One reveal beat's copy — the horror headline and the bigger-picture body, in the site's own
/// voice (see <see cref="ExpeditionBrief.RevealFor"/>).</summary>
public readonly record struct RevealCopy(string Headline, string Body);
