namespace SpaceSails.Core;

/// <summary>
/// #370 — THE AWAY EXPEDITION's diced heart. The owner's spec (issue #370): the scientists "scurry about
/// … they always find something that should not be possible and it tests the group's cohesion and
/// sanity … We can dice throw these outcomes on the gig and narrate them." And the house law (§5.0, "the
/// dice are the engine"): every consequence rolls on the one <see cref="DiceRule"/>, and the cast dice
/// are SHOWN (a <see cref="DiceEvent"/> to the tray). This is that table.
///
/// <para>While the team is on the ground, an episode rolls on a fixed cadence
/// (<see cref="EventCadenceSeconds"/>). Each is a 2D6 (the TTRPG homage's natural 2–12), skewed by the
/// gig's <see cref="ExpeditionFlavor"/> — a mining-survey crew reads the rock more warily (owner: "the
/// tool marks don't look familiar … a nice scare"), so it skews a hair toward the scares. The total picks
/// an <see cref="ExpeditionOutcome"/> band: a discovery pays a bonus, a fray or a bolt or the rare horror
/// costs nerve (through <see cref="NerveModel.Shock"/>) and can lose a scientist to the dark or rouse a
/// LIMITED low-risk pack (never the Miranda stream — the tide is gated off for expedition sites).</para>
///
/// <para>Pure and fully deterministic in the folded seed (never <see cref="System.Random"/> or the
/// clock), so client and any replay agree on every face and every consequence.</para>
/// </summary>
public static class AwayExpeditionEvents
{
    /// <summary>The tray caption every expedition dice event carries.</summary>
    public const string Source = "EXPEDITION";

    /// <summary>Ground-time seconds between rolled episodes — the cadence the client fires the table on
    /// while the team is on-site. Tuned so a normal excursion sees a handful of beats; OWNER-TUNABLE
    /// (issue #370: "this just needs to be playtested").</summary>
    public const double EventCadenceSeconds = 40.0;

    /// <summary>How many episodes have come due by <paramref name="onSiteSeconds"/> of ground time — one
    /// per whole <see cref="EventCadenceSeconds"/>. The client tracks the last ordinal it fired and rolls
    /// each new one; ordinal 0 is the first beat, at the first full cadence.</summary>
    public static int EpisodesElapsed(double onSiteSeconds) =>
        onSiteSeconds <= 0.0 ? 0 : (int)(onSiteSeconds / EventCadenceSeconds);

    /// <summary>Fold one episode's stable seed: the accept moment, the site id and the beat ordinal, so a
    /// gig's Nth beat is reproducible from exactly when and where it happened, and successive beats roll
    /// independent episodes.</summary>
    public static ulong Seed(double acceptedSimTime, string siteBodyId, int ordinal) =>
        DiceRule.Seed("expedition-episode", (long)acceptedSimTime, HashId(siteBodyId), ordinal);

    /// <summary>
    /// Roll one on-site episode for <paramref name="ordinal"/>, seeded by <paramref name="seed"/>, coloured
    /// by <paramref name="flavor"/>. The 2D6 total picks the band; a mining survey carries a small "the
    /// crew reads the rock warily" penalty so it skews toward the scares (never OP — the small-integer
    /// house guardrail). Once the gig's REVEAL has landed (<paramref name="revealed"/>, #370 the bigger
    /// picture), the table DARKENS by a bounded −1 — the ground is worse now that the team knows what it is.
    /// Pure and deterministic.
    /// </summary>
    public static ExpeditionEpisode Roll(ulong seed, ExpeditionFlavor flavor, int ordinal, bool revealed = false)
    {
        List<DiceModifier> mods = [];
        if (flavor == ExpeditionFlavor.MiningSurvey)
        {
            mods.Add(new DiceModifier("the crew reads the rock warily", -1));
        }
        if (revealed)
        {
            mods.Add(new DiceModifier("the bigger picture presses in", -1));
        }

        DicePool pool = DiceRule.RollPool(seed, count: 2, sides: 6, mods);
        int total = pool.Total;

        return total switch
        {
            <= 3 => Horror(seed, pool, flavor),
            <= 5 => Bolt(seed, pool, flavor),
            <= 7 => Fray(pool, flavor),
            <= 9 => Quiet(pool, flavor),
            <= 11 => Discovery(pool, flavor),
            _ => MajorDiscovery(pool, flavor),
        };
    }

    // ===== The episode table — the house voice (crude TTRPG entries; homage, never reproduction) ======

    // 2–3 · THE RARE HORROR. A nerve lump through the existing seam, and on the worst of it a LIMITED pack
    // rouses (a few motion-tracked movers — never the endless tide). A second salted roll decides whether
    // it also costs a scientist to the dark.
    private static ExpeditionEpisode Horror(ulong seed, DicePool pool, ExpeditionFlavor flavor)
    {
        bool lost = DiceRule.RollPool(DiceRule.Seed(seed, "horror-toll"), 1, 6).FaceTotal <= 2; // ~1 in 3
        int pack = 2 + (int)(DiceRule.RollPool(DiceRule.Seed(seed, "horror-pack"), 1, 3).FaceTotal); // 3..5, LIMITED
        (string head, string detail) = flavor == ExpeditionFlavor.Science
            ? ("👁 Something that should not be possible.",
               "The chamber was not empty. It is looking back. The team's nerve buckles" + (lost ? " — one does not make it back to the light." : "."))
            : ("🩸 The old cuts are not human.",
               "This rock was mined before — and the tool marks belong to no maker the crew can name. Something is still down here" + (lost ? "; one of the survey never answers the call again." : "."));
        return new ExpeditionEpisode(
            DiceEvent.FromPool(Source, pool, head, detail),
            ExpeditionOutcome.Horror, BonusCredits: 0, NerveHit: NerveModel.MonolithSightShock,
            ScientistLost: lost, HostilePack: pack);
    }

    // 4–5 · A SCIENTIST'S NERVE BREAKS and they bolt into the dark. A salted recovery roll decides retrieve
    // vs lost (owner: "retrieve or lose them — narrated, affects payout"). Either way a nerve hit.
    private static ExpeditionEpisode Bolt(ulong seed, DicePool pool, ExpeditionFlavor flavor)
    {
        bool recovered = DiceRule.RollPool(DiceRule.Seed(seed, "bolt-recover"), 2, 6).FaceTotal >= 7; // ~58% back
        string who = flavor == ExpeditionFlavor.Science ? "A scientist" : "A surveyor";
        (string head, string detail) = recovered
            ? ($"🏃 {who} breaks and runs.",
               "They scramble off into the shafts, keening — the team corners them and drags them back, shaking but whole.")
            : ($"🏃 {who} breaks and runs — into the dark.",
               "They are gone before anyone can grab a sleeve. The tracker holds one fading blip, then nothing. The team comes back one short.");
        return new ExpeditionEpisode(
            DiceEvent.FromPool(Source, pool, head, detail),
            ExpeditionOutcome.ScientistBolts, BonusCredits: 0, NerveHit: 12,
            ScientistLost: !recovered, HostilePack: 0);
    }

    // 6–7 · AN UNSETTLING FIND. A small nerve fray, no lasting harm — the dread tax.
    private static ExpeditionEpisode Fray(DicePool pool, ExpeditionFlavor flavor)
    {
        (string head, string detail) = flavor == ExpeditionFlavor.Science
            ? ("🕯 The walls are carved in a hand no one knows.",
               "Hieroglyphs that predate every catalogue. The team photographs them and tries not to look too long.")
            : ("🔩 The seams read wrong.",
               "The ore was cut by tools the survey can't place. Nobody says it, but everyone's watching the exits now.");
        return new ExpeditionEpisode(
            DiceEvent.FromPool(Source, pool, head, detail),
            ExpeditionOutcome.NerveFray, BonusCredits: 0, NerveHit: 6, ScientistLost: false, HostilePack: 0);
    }

    // 8–9 · A QUIET BEAT. The dig continues; nothing the crew will retell. No effect.
    private static ExpeditionEpisode Quiet(DicePool pool, ExpeditionFlavor flavor)
    {
        (string head, string detail) = flavor == ExpeditionFlavor.Science
            ? ("📋 The team works the grid.",
               "Careful, methodical, nothing stirring. The tracker stays quiet — for now.")
            : ("⛏ The survey cores another face.",
               "Clean readings, a good seam. The crew almost relaxes.");
        return new ExpeditionEpisode(
            DiceEvent.FromPool(Source, pool, head, detail),
            ExpeditionOutcome.Nothing, BonusCredits: 0, NerveHit: 0, ScientistLost: false, HostilePack: 0);
    }

    // 10–11 · A REAL FIND. A pay bonus; mining's platinum reads richer than a lab's relic.
    private static ExpeditionEpisode Discovery(DicePool pool, ExpeditionFlavor flavor)
    {
        int bonus = flavor == ExpeditionFlavor.Science ? 800 : 1200;
        (string head, string detail) = flavor == ExpeditionFlavor.Science
            ? ("🏺 A find — intact, cataloguable, saleable.",
               $"An artefact worth the trip. The sponsor will pay for this. +{bonus:N0} cr banked to the gig.")
            : ("💎 A rich seam — platinum-group, high grade.",
               $"The assay comes back fat. The survey earns its keep. +{bonus:N0} cr banked to the gig.");
        return new ExpeditionEpisode(
            DiceEvent.FromPool(Source, pool, head, detail),
            ExpeditionOutcome.Discovery, BonusCredits: bonus, NerveHit: 0, ScientistLost: false, HostilePack: 0);
    }

    // 12 · THE BIG ONE — "something that should not be possible", the good side of it. A fat bonus, and a
    // small awe/dread fray (you do not find this and sleep easy).
    private static ExpeditionEpisode MajorDiscovery(DicePool pool, ExpeditionFlavor flavor)
    {
        int bonus = flavor == ExpeditionFlavor.Science ? 2500 : 3000;
        (string head, string detail) = flavor == ExpeditionFlavor.Science
            ? ("✨ A discovery that rewrites the catalogue.",
               $"Something that should not exist — and it is real. Priceless, and it will keep the team up at night. +{bonus:N0} cr.")
            : ("✨ A mother lode — and a machine that should not be here.",
               $"The richest strike the crew has seen, guarded by workings no one built in this age. +{bonus:N0} cr.");
        return new ExpeditionEpisode(
            DiceEvent.FromPool(Source, pool, head, detail),
            ExpeditionOutcome.MajorDiscovery, BonusCredits: bonus, NerveHit: 4, ScientistLost: false, HostilePack: 0);
    }

    // FNV-1a of the site id → a stable long for the seed fold (matches DiceRule's own tag hashing style).
    private static long HashId(string id)
    {
        unchecked
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offsetBasis;
            foreach (char c in id ?? "")
            {
                hash ^= c;
                hash *= prime;
            }

            return (long)hash;
        }
    }
}

/// <summary>The band one on-site episode landed in — the coarse bucket the log and tests read.</summary>
public enum ExpeditionOutcome
{
    /// <summary>A quiet beat — the dig continues, nothing stirs.</summary>
    Nothing,

    /// <summary>A real find — a pay bonus banked to the gig.</summary>
    Discovery,

    /// <summary>The big one — "something that should not be possible", a fat bonus (and a little dread).</summary>
    MajorDiscovery,

    /// <summary>An unsettling find — a small nerve fray, no lasting harm.</summary>
    NerveFray,

    /// <summary>A scientist's nerve breaks and they bolt — retrieved, or lost to the dark (diced).</summary>
    ScientistBolts,

    /// <summary>The rare horror — a nerve lump, a limited pack roused, maybe a life owed to the dark.</summary>
    Horror,
}

/// <summary>One rolled on-site episode: the dice event to SHOW, the band, and its consequences — a pay
/// bonus banked to the gig, a nerve lump to <see cref="NerveModel.Shock"/> through, whether a scientist
/// was lost (docks the payout), and how many of a LIMITED hostile pack to rouse (0 = none; never the
/// endless tide).</summary>
public readonly record struct ExpeditionEpisode(
    DiceEvent Event,
    ExpeditionOutcome Outcome,
    int BonusCredits,
    double NerveHit,
    bool ScientistLost,
    int HostilePack);
