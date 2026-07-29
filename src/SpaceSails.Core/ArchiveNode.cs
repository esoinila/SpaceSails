namespace SpaceSails.Core;

/// <summary>
/// THE ARCHIVE NODE — the one warm thing on a dead ship, and the first time the player is in the room
/// with Nebula's cold archive instead of reading about it.
///
/// <para>Owner, 2026-07-29: <i>"some object that going close to it is told to the player with GEN AI art
/// that they see flashbacks of their cloning facility they were not meant to see .. some spider like
/// beings maybe even as workers there, something that forces a sanity roll when confronted … those could
/// contain clues about some big plots we have in the game."</i> Design lineage — the SWITCH is an homage
/// to Ren &amp; Stimpy's "Space Madness" history-eraser button: an honest label, and a captain alone with
/// it too long. Homage, not reproduction: our object, our fiction. See
/// <c>docs/features/the-archive-node.md</c>.</para>
///
/// <para><b>What this file is.</b> Pure rules, no world: where a node may be, what standing near it costs,
/// the throw and its bands, the authored vision pool, and what the handle does. It never spawns anything
/// and never touches <see cref="NervePips"/> state — the client spends the pips, the same way it does for
/// every other pressure.</para>
///
/// <para><b>The shape of the whole thing:</b> arc 2 currently converts curiosity into knowledge. The node
/// converts NERVE into knowledge. Everything here is bought; nothing here is given.</para>
/// </summary>
public static class ArchiveNode
{
    // ── Where one is ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Whether a wreck of this cause may carry a node at all. Rarity is the point, so the answer
    /// is "only where it explains something":
    ///
    /// <list type="bullet">
    /// <item><see cref="Derelict.WreckCause.VentedByOneOfTheirOwn"/> — ALWAYS. The node is why she died.</item>
    /// <item><see cref="Derelict.WreckCause.InsuranceJob"/> — a staged loss is exactly the freight run you
    /// would use to move one somewhere it was never invoiced to.</item>
    /// <item><see cref="Derelict.WreckCause.Mutiny"/> — they were arguing about something.</item>
    /// <item><see cref="Derelict.WreckCause.LifeSupportFailure"/> — nobody noticed the air going because
    /// nobody was entirely present.</item>
    /// </list>
    ///
    /// <para>Never on <see cref="Derelict.WreckCause.Infested"/>: that hull already carries a full deck of
    /// tension and two horrors in one room is one horror less.</para></summary>
    public static bool CauseMayCarryOne(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.VentedByOneOfTheirOwn => true,
        Derelict.WreckCause.InsuranceJob or Derelict.WreckCause.Mutiny
            or Derelict.WreckCause.LifeSupportFailure => true,
        _ => false,
    };

    /// <summary>One wreck in this many of the eligible causes actually has one aboard — the vented hull
    /// excepted, where the node is the cause and is always there. FLAGGED for the owner's tuning.</summary>
    public const int OneInEligibleWrecks = 3;

    /// <summary>Is there a node on THIS wreck? Seeded off her id, so a reload finds the same ship — and so
    /// a captain who has heard a rumour about a hull can go and look.</summary>
    public static bool IsAboard(string wreckId, Derelict.WreckCause cause)
    {
        if (!CauseMayCarryOne(cause))
        {
            return false;
        }
        if (cause == Derelict.WreckCause.VentedByOneOfTheirOwn)
        {
            return true;   // she is a wreck BECAUSE of it
        }

        return DiceRule.Roll(DiceRule.Seed("archive-aboard", (long)wreckId.GetHashCode(System.StringComparison.Ordinal)),
                             OneInEligibleWrecks).Face == 1;
    }

    /// <summary>The compartment a node sits in: the deep hold, because that is where cargo nobody
    /// invoiced ends up, and because it puts the field between the away team and the far end of the
    /// ship.</summary>
    public const string HoldCompartment = "DEEP HOLD";

    // ── Layer 1: the dwell ────────────────────────────────────────────────────────────────────────────

    /// <summary>How near you have to be for it to start counting, in deck units. Wide enough that the
    /// whole compartment is inside it — the decision is "am I finished in this room", not "can I stand on
    /// the correct tile". FLAGGED for tuning.</summary>
    public const double FieldRadius = 9.0;

    /// <summary>Close enough to be CONFRONTING it rather than merely near it. Crossing this is what forces
    /// the throw, so it is deliberately small: you have to have gone to it.</summary>
    public const double ConfrontRadius = 2.5;

    /// <summary>Is the captain inside the field, and therefore paying <see cref="NervePips.Cause.Archive"/>
    /// beats? Pure geometry so the audit and the client agree.</summary>
    public static bool InField(double dx, double dy) =>
        (dx * dx) + (dy * dy) <= FieldRadius * FieldRadius;

    /// <summary>Close enough that looking at it is unavoidable.</summary>
    public static bool AtArmsLength(double dx, double dy) =>
        (dx * dx) + (dy * dy) <= ConfrontRadius * ConfrontRadius;

    // ── Layer 2: the confrontation ────────────────────────────────────────────────────────────────────

    /// <summary>Everything the throw is allowed to know. World-blind on purpose: the client hands over
    /// facts, this file decides nothing about where any of them came from.</summary>
    /// <param name="PipsNow">Nerve remaining, 0–10. Steady hands look; a frayed captain is looked at.</param>
    /// <param name="Suited">The helmet is between you and it.</param>
    /// <param name="PriorConfrontations">How many times this captain has already faced THIS node.</param>
    /// <param name="NebulaShardsHeld">Arc-2 fragments already assembled. The more of the shape you hold,
    /// the more of the vision you are equipped to understand — and understanding is the injury.</param>
    /// <param name="HasEverDied">Whether the insurance has ever paid out this thread. If it has, the
    /// archive has something of yours on file, and it knows.</param>
    public readonly record struct Confrontation(
        int PipsNow,
        bool Suited,
        int PriorConfrontations,
        int NebulaShardsHeld,
        bool HasEverDied);

    /// <summary>The named modifiers, in the house idiom: visible arithmetic, no hidden fudging. The player
    /// can read exactly why the throw went the way it did, which is the only reason a bad result is
    /// bearable.</summary>
    public static IReadOnlyList<DiceModifier> Modifiers(in Confrontation c)
    {
        var mods = new List<DiceModifier>();

        int steadiness = c.PipsNow - 5;
        if (steadiness != 0)
        {
            mods.Add(new(steadiness > 0 ? "steady hands" : "frayed", steadiness));
        }
        if (c.Suited)
        {
            mods.Add(new("suited", SuitedBonus));
        }
        if (c.PriorConfrontations > 0)
        {
            mods.Add(new("it knows the way in now", -RepeatPenalty * c.PriorConfrontations));
        }
        if (c.NebulaShardsHeld > 0)
        {
            mods.Add(new("you know what you are looking at", -c.NebulaShardsHeld));
        }
        if (!c.HasEverDied)
        {
            mods.Add(new("nothing of yours on file", NeverDiedBonus));
        }

        return mods;
    }

    /// <summary>The helmet between you and it.</summary>
    public const int SuitedBonus = 2;

    /// <summary>Every previous look makes the next one worse. Something in there turned its head.</summary>
    public const int RepeatPenalty = 2;

    /// <summary>A captain the archive has never had a withdrawal from.</summary>
    public const int NeverDiedBonus = 3;

    /// <summary>How a confrontation ended. A table, not a coin — the <see cref="Derelict.Resolve"/>
    /// idiom.</summary>
    public enum Band
    {
        /// <summary>You looked away in time. A half-image, and it is still there.</summary>
        LookedAway,

        /// <summary>You saw it. One vision, one shard — the trade this whole feature exists for.</summary>
        Saw,

        /// <summary>It saw you looking. The vision, and the next look starts worse.</summary>
        Noticed,

        /// <summary>You are in it. The rack, the number, and it is not a stranger on the slab.</summary>
        Inside,
    }

    /// <summary>Band thresholds on the total. Wide middle on purpose: the common case should be the
    /// bargain (a vision for pips), not the catastrophe.</summary>
    public const int LookedAwayAt = 15;
    public const int SawAt = 9;
    public const int NoticedAt = 4;

    /// <summary>What each band spends, in whole pips.</summary>
    public static int PipCost(Band band) => band switch
    {
        Band.LookedAway => 1,
        Band.Saw => VisionPips,
        Band.Noticed => VisionPips + 1,
        Band.Inside => InsidePips,
        _ => 0,
    };

    /// <summary>Seeing one costs what the monolith costs. It is the same class of thing: a fact about the
    /// universe you cannot give back. FLAGGED for tuning.</summary>
    public const int VisionPips = NervePips.MonolithPips;

    /// <summary>Being inside one. The largest single lump the wreck lane can spend, and it needs the
    /// captain to have walked to it twice over — through the field, past the label, and into arm's
    /// reach.</summary>
    public const int InsidePips = 5;

    /// <summary>The throw. Deterministic in the wreck's own seed and the number of looks already taken, so
    /// the same captain on the same hull gets the same answer — reload-scumming a vision out of it is not
    /// a strategy.</summary>
    public static (DiceRoll Roll, Band Band, Vision Vision) Confront(string wreckId, in Confrontation c)
    {
        DiceRoll roll = DiceRule.Roll(
            DiceRule.Seed("archive-confront",
                          (long)wreckId.GetHashCode(System.StringComparison.Ordinal),
                          c.PriorConfrontations),
            DiceRule.D20,
            Modifiers(c));

        Band band =
            roll.Total >= LookedAwayAt ? Band.LookedAway :
            roll.Total >= SawAt ? Band.Saw :
            roll.Total >= NoticedAt ? Band.Noticed :
            Band.Inside;

        return (roll, band, VisionFor(band, c));
    }

    /// <summary>The line the captain reads for the band — what happened, never what it means.</summary>
    public static string BandLine(Band band) => band switch
    {
        Band.LookedAway =>
            "You get your eyes off it. Something was starting and you did not let it finish. The node hums on, patient, still the only warm thing aboard.",
        Band.Saw =>
            "It is not a screen and there is nothing to look at, and you see it anyway.",
        Band.Noticed =>
            "You see it — and the seeing goes both ways. Whatever is resident in there stops what it was doing and attends to you. You will feel this the next time.",
        Band.Inside =>
            "You do not see it. You are in it, and you have been for longer than the moment took.",
        _ => "",
    };

    // ── The visions ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>One thing the archive gives back. Authored, never generated: the pool is the content.</summary>
    /// <param name="Id">Stable id — the client binds art and arc-2 fragments to this.</param>
    /// <param name="Title">The two or three words the captain's ledger files it under.</param>
    /// <param name="ArtFile">The canvas. Degrades cleanly when unpainted, like every wreck slot.</param>
    /// <param name="Lore">What the captain reads. Evidence, never conclusion.</param>
    public readonly record struct Vision(string Id, string Title, string ArtFile, string Lore);

    /// <summary>Why the handlers are spiders, and why that is not an answer.
    ///
    /// <para>A stored pattern has no eyes. What comes back is not footage — it is what a mind with no body
    /// made of the things that handled it: too many joints, too patient, always working in threes, always
    /// gentle. Whether they are people in rigs, waldos run from somewhere else, or something that was
    /// never on the payroll, THE ARCHIVE DOES NOT KNOW EITHER. It only knows how careful they were.</para>
    ///
    /// <para>That is a new question planted in a mechanic, not a reveal — the seed for a third arc, the way
    /// the Ringside plaque was the seed for KAAMOS.</para></summary>
    public const string HandlerNote =
        "A stored pattern has no eyes. What it gives back is not a recording — it is the shape a mind with " +
        "no body chose to keep the memory in. It remembers how they moved: too many joints, too patient, " +
        "always in threes, always gentle. It does not know what they were. It only knows how careful.";

    /// <summary>The pool, in the order a captain works through it. Five, authored, deterministic — the dice
    /// choose the BAND, never the content, so the story a hull tells is the same story every universe.</summary>
    public static readonly Vision[] Visions =
    [
        new("archive-rows", "The rows", "art/vision-rows.jpg",
            "A hall of jars racked to a ceiling that does not arrive, going away from you in both directions " +
            "further than the light does. Every one of them is labelled. None of them is named. The air has " +
            "given up and gone to frost on the rails, and the whole enormous room is as quiet as arithmetic. " +
            "You are not being shown this. You are remembering it."),

        new("archive-handlers", "The handlers", "art/vision-handlers.jpg",
            "Three of them working a rack together, unhurried, in a light that was not installed for eyes. " +
            "Too many joints. They do not look at the jar they are handling — they lean in and ATTEND to it, " +
            "the way you would to something small that was still talking. Whatever else is true, they were " +
            "gentle with it. " + HandlerNote),

        new("archive-intake", "The intake", "art/vision-intake.jpg",
            "A clinic you half-recognise: gurney, rig, the smell of a cheap policy. The rig comes down, and " +
            "the wrong detail is the count — TWO patterns come off a subscriber who lay down once. One is " +
            "filed. One is billed. You have signed for this. Everyone has."),

        new("archive-wintering", "The wintering", "art/vision-wintering.jpg",
            "Water with no surface anywhere, and cold past the point where cold is a temperature. Something " +
            "the size of a district is keeping still down there, and it is not the archive — it is the thing " +
            "the archive was copied from, badly, and it is very much bigger. Forty is not a number here. It " +
            "is a name."),

        new("archive-your-rack", "The rack with your number", "art/vision-your-rack.jpg",
            "One jar, close enough to touch, and stencilled on the collar is your own policy number — the one " +
            "off the card they hand you at every clinic. It is occupied. It is lucid. It has been in the dark " +
            "since your first premium, and it has been awake for all of it, and it turns toward you the way " +
            "you would turn toward a door. It is not surprised. It has been expecting you the whole time you " +
            "have been alive."),
    ];

    /// <summary>Which vision this look delivers. Order is fixed — a captain works down the pool — except
    /// that the last one is GATED: the rack with your number only comes up for a captain the insurance has
    /// already paid out on. It is not lore about a stranger, it is the player's own save looking back, and
    /// it means nothing to someone who has never died.</summary>
    public static Vision VisionFor(Band band, in Confrontation c)
    {
        if (band == Band.LookedAway)
        {
            return Visions[0] with
            {
                Lore = "Half of something, and then your own hand on the bulkhead and no memory of putting it there.",
            };
        }

        // The Inside band always resolves to the rack — that IS what being inside one means — but only for
        // a captain who has something on file. Anyone else gets the wintering instead: just as large, and
        // not about them.
        if (band == Band.Inside)
        {
            return c.HasEverDied ? Visions[4] : Visions[3];
        }

        int reach = System.Math.Clamp(c.PriorConfrontations, 0, Visions.Length - 1);
        if (reach == Visions.Length - 1 && !c.HasEverDied)
        {
            reach--;   // never show the rack to a captain it cannot mean anything to
        }
        return Visions[reach];
    }

    // ── Layer 3: the switch ───────────────────────────────────────────────────────────────────────────

    /// <summary>The legend stencilled on the housing by an engineer who had no reason to be coy. The game
    /// NEVER restates it, hedges it, softens it, or puts a confirmation dialog in front of it: the label is
    /// the confirmation dialog. That restraint is the entire joke and the entire mechanic.</summary>
    public const string SwitchLegend = "PURGE NODE — RESIDENT PATTERN NOT RECOVERABLE";

    /// <summary>Who was in the spar.</summary>
    public enum Resident
    {
        /// <summary>Somebody nobody is looking for. Nothing follows, and nothing ever tells you so.</summary>
        Stranger,

        /// <summary>A lapsed subscriber a collector is still chasing. Purge it and the writ has nothing left
        /// to recover — a real payment for the reckless road, so the handle is not merely a punishment.</summary>
        DelinquentSubscriber,

        /// <summary>YOURS. The rebirth is over for this thread and nothing announces it. You find out the
        /// way the man in the cartoon did: afterwards.</summary>
        YourOwn,
    }

    /// <summary>What is actually in this node — seeded off the wreck, fixed before the captain ever comes
    /// aboard, and READABLE IN ADVANCE at the price of a bad throw (the <see cref="Band.Noticed"/> band or
    /// worse reads the collar). The house shape: the instrument cannot tell you, the dice can, the dice
    /// cost nerve — and you may always pull the handle without paying, and never learn what you did.</summary>
    public static Resident ResidentOf(string wreckId)
    {
        int face = DiceRule.Roll(
            DiceRule.Seed("archive-resident", (long)wreckId.GetHashCode(System.StringComparison.Ordinal)),
            DiceRule.D20).Face;

        // 1 in 20 is yours. Rare enough to be a story rather than a tax, common enough that an unread
        // handle is a genuine gamble across a career.
        return face == 1 ? Resident.YourOwn
             : face <= 6 ? Resident.DelinquentSubscriber
             : Resident.Stranger;
    }

    /// <summary>Pulling it. The noise stops, the visions stop, the hull goes as cold and quiet as every
    /// other wreck in the game — and pips come back, because relief is real and that is the trap.</summary>
    public const int PurgeRelief = 2;

    /// <summary>What the captain reads at the handle. Note what is NOT here: any statement about whose
    /// pattern it was. A purge the captain did not pay to read stays unknown forever — the record of it is
    /// the silence afterwards.</summary>
    public const string PurgeLine =
        "The handle is stiff and then it is not. Somewhere inside the column a circulation you never " +
        "consciously heard stops, and the compartment gets colder in the time it takes to notice. The hull " +
        "is now exactly as quiet as every other dead ship you have stood in. Your hands are steadier. That " +
        "is the part to be careful about.";

    /// <summary>The line the resurrection card reads, ONCE, on the death after a captain purged their own
    /// pattern — the smallest change in the whole feature and the thing everything else is for. The card
    /// has never read this before and will never read it again.</summary>
    public const string NoRestoreLine =
        "NO PATTERN ON FILE — POLICY CLOSED AT SUBSCRIBER REQUEST. The clinic's welcome loop does not play. " +
        "Nobody comes. You did read the label.";
}
