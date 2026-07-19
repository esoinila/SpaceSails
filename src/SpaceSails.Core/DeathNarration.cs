namespace SpaceSails.Core;

/// <summary>
/// #380 item 1 · WHAT KILLED THE CAPTAIN — the place-dependent death classification, so the resurrection
/// card can finally explain its own fiction at the moment it matters (owner cruise ruling, 2026-07-19:
/// "I love the fail forward idea.. we should give the dead captain explanation with gen ai images. Eaten
/// by reevers etc place dependent or joined them"). The read-only audit's item 1 gap is "the resurrection
/// fiction arrives too late"; this names the cause up front.
///
/// <para>A single pure enum carried into the rebirth flow. The client maps it to one of the Grok-generated
/// death images and one seeded house-voice line. Two causes are LIVE today — <see cref="DeathCause.Collector"/>
/// (the BUSTED last stand) and <see cref="DeathCause.Impact"/> (periapsis under the surface). The surface
/// causes — <see cref="DeathCause.Reevers"/>, <see cref="DeathCause.Joined"/> — and the deep-space
/// <see cref="DeathCause.Void"/> are wired READY (art + tested lines + the <see cref="DeathNarration.SurfaceEnd"/>
/// classifier): a surface Reever catch does NOT kill today (it prices heat + a nerve shock, see
/// Map.Surface ReeverCatch) and nerve overdraw does not kill either, so nothing routes to them yet. When the
/// surface-death lane lands it only has to set the cause — this rule already narrates it.</para>
/// </summary>
public enum DeathCause
{
    /// <summary>A heat-hunter's collector ran you down and the last stand went to the volley — the BUSTED
    /// FreezeFrame. The most common death, and the one the game ships live.</summary>
    Collector,

    /// <summary>You put the ship into a body at speed — periapsis went under the surface (TriggerImpact).
    /// Live today.</summary>
    Impact,

    /// <summary>The Old Ones took you on a surface — a Reever laid hands and this time did not let go. The
    /// chest is still out there. (Wired ready; the surface-death lane sets it.)</summary>
    Reevers,

    /// <summary>The eerie variant of a surface death: nerves shot to a sliver, and the last anyone saw, the
    /// captain walked TOWARD the crowd, not away — "joined them" (owner's cruise ruling). Chosen sparingly by
    /// <see cref="DeathNarration.SurfaceEnd"/> so it stays chilling. (Wired ready.)</summary>
    Joined,

    /// <summary>Lost to the void — adrift / EVA / an orbit that slipped, no body to name. (Wired ready for
    /// whatever void death lands; none routes here today.)</summary>
    Void,
}

/// <summary>
/// The pure narration seam for <see cref="DeathCause"/>: the art file each cause shows, the seeded
/// house-voice line pool that explains the death place-dependently, the WHAT-HAPPENED headline, and the
/// "joined them" trigger rule. All deterministic — a test pins an exact line for an exact seed — because
/// determinism is law in Core.
/// </summary>
public static class DeathNarration
{
    // ── The "joined them" trigger (owner cruise ruling, 2026-07-19) ──────────────────────────────────
    //
    // On a surface Reever death, the narration is USUALLY that the Old Ones took you (Reevers). But when the
    // captain's nerve was shot to a sliver at the very end, a seeded MINORITY of those deaths tell the eerie
    // story instead — the captain walked into the crowd, not away from it. Rare-ish and gated on a shattered
    // nerve so it stays chilling, exactly as the ruling asks ("or joined them" — used sparingly).

    /// <summary>Nerve at/under this sliver of the 0..100 gauge makes a death ELIGIBLE for the "joined them"
    /// variant — you have to be shattered to walk the wrong way. FLAGGED for the owner's tuning.</summary>
    public const double JoinedNerveSliver = 8.0;

    /// <summary>Of the sliver-nerve surface deaths, roughly one in this many "joins them" — seeded, so it is
    /// reproducible, and rare-ish so the variant stays rare. FLAGGED for the owner's tuning.</summary>
    public const int JoinedChanceInN = 3;

    /// <summary>Classify a surface Reever death place-dependently: normally the Old Ones TOOK you
    /// (<see cref="DeathCause.Reevers"/>), but with the nerve shot to a sliver a seeded minority JOINED them
    /// (<see cref="DeathCause.Joined"/>). Pure and seeded so a test pins the split. This is the one law the
    /// surface-death lane calls when it lands.</summary>
    public static DeathCause SurfaceEnd(double nerveAtDeath, ulong seed)
    {
        if (nerveAtDeath > JoinedNerveSliver)
        {
            return DeathCause.Reevers; // steady enough hands — you ran the right way
        }

        return seed % (ulong)JoinedChanceInN == 0 ? DeathCause.Joined : DeathCause.Reevers;
    }

    /// <summary>The Grok-generated death image (under <c>art/</c>) a cause shows on the resurrection card.
    /// The two live causes reuse the existing BUSTED frames; the surface + void causes use the death-* set.</summary>
    public static string ArtFile(DeathCause cause) => cause switch
    {
        DeathCause.Collector => "busted-freeze-frame.jpg",
        DeathCause.Impact => "busted-ship-explosion.jpg",
        DeathCause.Reevers => "death-reevers.jpg",
        DeathCause.Joined => "death-joined.jpg",
        DeathCause.Void => "death-void.jpg",
        _ => "busted-ship-explosion.jpg",
    };

    /// <summary>The short WHAT-HAPPENED headline for the block above the brain-backup copy.</summary>
    public static string Headline(DeathCause cause) => cause switch
    {
        DeathCause.Collector => "WHAT HAPPENED — the collectors got you",
        DeathCause.Impact => "WHAT HAPPENED — you hit the ground",
        DeathCause.Reevers => "WHAT HAPPENED — the Old Ones took you",
        DeathCause.Joined => "WHAT HAPPENED — you walked into the crowd",
        DeathCause.Void => "WHAT HAPPENED — lost to the void",
        _ => "WHAT HAPPENED",
    };

    // ── The house-voice line pools (place-dependent) ─────────────────────────────────────────────────
    //
    // One to two sentences, in the house voice, that narrate the death WHERE it happened. {body} is filled by
    // the caller — the moon the Old Ones took you on, the body you flew into. A null/empty body reads with a
    // generic place ("out there" / "open space") so the line is always whole. Seeded so the variant is
    // reproducible; small pools kept deliberately tight so every line stays quotable.

    private static readonly string[] CollectorLines =
    [
        "The collectors' boarding volley caught you {where} — they don't come to collect twice.",
        "One massive volley {where}, and the last stand was over before the echo. The debt collected itself.",
        "They ran you down {where} and settled the account in lead. The purse was never the point.",
    ];

    private static readonly string[] ImpactLines =
    [
        "You put the ship into {body} at speed — the periapsis said 'under the surface', and the surface won.",
        "The hull met {body} at speed. No corridor, no atmosphere to catch you — just rock, and then nothing.",
        "You flew {body}'s periapsis under its own surface. The ground was where the orbit said it would be.",
    ];

    private static readonly string[] ReeverLines =
    [
        "The Old Ones took you on {body} — the chest is still out there, for anyone fool enough to go back for it.",
        "A Reever laid hands on you on {body} and this time did not let go. They wanted no loot — only you.",
        "They ran you down on {body}'s regolith short of the tube. The helmet's out there yet; you are not.",
    ];

    private static readonly string[] JoinedLines =
    [
        "They found no body on {body}. The last anyone saw, you walked TOWARD the crowd, not away from it.",
        "On {body} your nerve went to nothing, and then so did you. No struggle in the regolith — just footprints, leading in.",
        "The tracker on {body} still shows you moving, some nights. You didn't run from the Old Ones at the end. You joined them.",
    ];

    private static readonly string[] VoidLines =
    [
        "Lost to the void — no beacon, no body, just the long dark and a brain-backup that remembers the cold.",
        "The orbit slipped and the void kept you. There was no ground to hit and no one to hear the carrier fade.",
        "You went adrift past every well and the dark closed over the transponder. The backup is all that came home.",
    ];

    private static string[] PoolFor(DeathCause cause) => cause switch
    {
        DeathCause.Collector => CollectorLines,
        DeathCause.Impact => ImpactLines,
        DeathCause.Reevers => ReeverLines,
        DeathCause.Joined => JoinedLines,
        DeathCause.Void => VoidLines,
        _ => CollectorLines,
    };

    /// <summary>The seeded, place-dependent house-voice line for a death — the WHAT-HAPPENED sentence(s) the
    /// resurrection card reads before the existing brain-backup copy. <paramref name="bodyName"/> names the
    /// place (the moon, the body flown into); null/blank reads with a generic place so the line is whole.
    /// Pure: same cause + same seed + same body → same line.</summary>
    public static string Line(DeathCause cause, ulong seed, string? bodyName)
    {
        string[] pool = PoolFor(cause);
        string template = pool[(int)(seed % (ulong)pool.Length)];
        string body = string.IsNullOrWhiteSpace(bodyName) ? "that world" : bodyName!;
        string where = string.IsNullOrWhiteSpace(bodyName) ? "in open space" : $"off {bodyName}";
        return template.Replace("{body}", body).Replace("{where}", where);
    }
}
