using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #677 · THE BAND NOBODY DUG — a dig that breaks into volume which was ALREADY THERE.
///
/// <para>Owner ruling 2026-08-04 (worldbuilding-notes.md §10): this is humanity's FOURTH run, the
/// prior three were ENDED, and every end spared a remnant underground in massive halls. It is a
/// different CLASS of thing from the band nobody listed — that one is human all the way down, poured
/// and surveyed and invoiced and merely hidden; this one was never ours — and the whole feature turns
/// on the difference, which is why it sits one band lower with a whole band of undug rock between.</para>
///
/// <para>Split out of <c>UndergroundComplex.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered. Its <c>const</c>s came with it, which is safe for the reason a <c>const</c> always is:
/// it is folded into its uses at compile time and cannot be initialised in the wrong order.</para>
/// </summary>
public static partial class UndergroundComplex
{
    // ── #677 · THE BAND NOBODY DUG ───────────────────────────────────────────────────────────────────────
    //
    // Owner ruling 2026-08-04, recorded in worldbuilding-notes.md §10: this is humanity's FOURTH run, the
    // prior three were ENDED, and every end spared a remnant underground, in massive halls. Out on the moons
    // that is this: a dig that breaks into volume which was ALREADY THERE.
    //
    // It is a different CLASS of thing from the band nobody listed (#592, §13.7), and the whole feature turns
    // on the difference. The unlisted band is human all the way down — poured, surveyed, invoiced, and hidden
    // from the staff who paid for it. This one was never ours. So:
    //
    //   * it is one band BELOW the unlisted band, with a WHOLE BAND of nothing dug between them — §13.7's gap
    //     idiom one rung further along. The unlisted band's gap is the remainder of a band the listed building
    //     stopped inside; this gap is four floors of untouched rock that a shaft was driven straight through;
    //   * the way in is the #590 card idiom again, found in the band nobody listed — the paper telling the
    //     truth about a building that is not;
    //   * nothing down there is a facility. No plate, no department, no livery, no sealed SECTOR doors, no
    //     locked rooms, no plumbing, no fixtures of any kind. The renderer's ink and the room scale do the
    //     storytelling and not one sentence explains anything (§13.8, §13.20).
    //
    // CANON, harder here than anywhere in the game: nothing names a builder, an age or a purpose; the word
    // reserved by §8 never appears; and BOTH readings of §10 — the instruments simply got better / this was
    // always here and is being SHOWN to us — have to survive every line. If any string ever settles which,
    // the horror dies.

    /// <summary>#677 · How many of the sites that already hide a band have something under THAT, and it is
    /// deliberately rarer than <see cref="UnlistedOneInN"/>.
    ///
    /// <para>One in five of the sites that already keep a secret from their own staff. Measured rather than
    /// asserted (<c>TheFoundBandTests</c> sweeps the generator and reads the rate off the sweep), and the
    /// measured incidence is lower still, because only the shallower half of the hiding sites has room under
    /// it for another shaft inside the performance guard — see below.</para></summary>
    public const int FoundOneInN = 5;

    /// <summary>#677 · THE SITE THE <c>?found=1</c> CHEAT PARKS, and the reason a body id lives in Core.
    ///
    /// <para>A site's whole shape — its depth, its kinds, its unlisted band and its halls — is seeded off its
    /// BODY ID, so reaching this feature from a URL is a matter of parking a rock with the right name rather
    /// than of overriding a Core fact from the client. This name is a seven-floor laboratory with an unlisted
    /// clinic under it and galleries under a whole band of nothing: the full chain, in one rock. The suffix is
    /// the search that found it and not decoration — about one id in fifty has halls.</para>
    ///
    /// <para>It sits here rather than beside the cheat because <b>five places have to agree about it</b>: the
    /// cheat itself, and four sweeps that would otherwise be auditing a universe with no galleries in it and
    /// passing vacuously. Five copies of a magic string is this repo's most expensive habit, and the deep rock
    /// already costs two. Pinned by <c>TheFoundBandTests</c>: if the seeding ever stops giving this id halls,
    /// the cheat and every one of those sweeps go red together and say why.</para></summary>
    public const string FoundBandCheatSiteId = "secret-lab-site-halls-116";

    /// <summary>#677/#1063 · Does this site have a band nobody dug? Seeded off its own id, like everything
    /// else about a site's shape — <b>and no longer, once the neighbours have filled it in</b>.
    ///
    /// <para><b>#1063 · THE ONE GATE.</b> This is the single seam every question about the halls already goes
    /// through — <see cref="IsFound"/>, <see cref="FoundBandOf"/>'s customers, <see cref="TrueDepthOf"/>,
    /// <see cref="FloorsOf"/>, <see cref="NextShaftBelow"/> (through <c>SiteHasBand</c>),
    /// <see cref="FoundKeyRoomFor"/>, <see cref="DeclaresDarkness"/>, and
    /// <see cref="DisclosureClock.OpensOn"/>, which delegates to <see cref="IsFound"/> by design. So the
    /// burial is asked HERE and nowhere else: after a fill, the shaft ends at the listed bottom and every one
    /// of those predicates answers exactly as it would for a site that never had halls at all. The erasure
    /// procedure's clauses (1) <i>remove the element</i> and (2) <i>remove its marks</i> are both this one
    /// line, because the marks — the found-key card room, the hall record's find id, the darkness, the room
    /// scale, the plateless name — are every one of them downstream of it.</para>
    ///
    /// <para>The alternative was thirty callers each taught what a burial is, which is §13.15's second cause
    /// (a caller reasoning about the shape of a building it does not own) said thirty times.</para></summary>
    public static bool HasFoundBand(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #1063 · Filled in, floored over, resurfaced. Asked FIRST, and asked cheaply: on every world where
        // nobody has been past a seam long enough ago — which is almost every world — this is one length
        // check on an empty list and the site's shape is exactly what it always was.
        if (Burial.IsFilled(bodyId))
        {
            return false;
        }

        return FoundBandSeeded(bodyId);
    }

    /// <summary>#677/#1063 · THE SEEDED FACT, asked BEFORE any burial: does this site's own id deal it a band
    /// nobody dug?
    ///
    /// <para>Private on purpose and it must stay private. Exactly two things may ask it: the public predicate
    /// above, which is the whole game's answer, and the specimen (<see cref="SpecimenFloorOf"/>), which is the
    /// one souvenir the erasure keeps and therefore the one caller that has to know what was filled in. A
    /// third caller would be a way to see the halls past the burial, which is the feature undone.</para></summary>
    private static bool FoundBandSeeded(string bodyId)
    {
        // It hangs off the band nobody listed, so a site with nothing to hide has nothing under that either.
        // (The head office is already excluded by HasUnlistedBand, and for the reason recorded there: the
        // directory is complete and the rank difference IS the absence.)
        if (!HasUnlistedBand(bodyId))
        {
            return false;
        }

        // And only where the whole arrangement — a band of nothing, then a band of halls — still fits above
        // the generator's own floor. This is HasUnlistedBand's second guard one rung further along, and it is
        // a PERFORMANCE bound rather than a design one (#585): a band clamped to nothing is not a band.
        if (BandTop(FoundBandOf(bodyId)) <= DeepestPossibleFloor)
        {
            return false;
        }

        return DiceRule.Roll(DiceRule.Seed($"hive:found:{bodyId}"), FoundOneInN).Face == 1;
    }

    /// <summary>#677 · WHICH band the halls are, and why it is <b>two</b> bands under the listed bottom's own.
    ///
    /// <para>The band nobody listed fills its band, so the next one down would be flush against it — one
    /// shaft's floor and the next shaft's ceiling, which is how a BUILDING continues. What has to read here
    /// is that the digging stopped and something else began, so there is a whole band between them with
    /// nothing in it: the shaft was driven through four floors' worth of rock and broke into what was
    /// waiting. §13.7's gap, one rung further, and the only rung where the gap is the point rather than an
    /// artefact of where a depth happened to stop.</para></summary>
    public static int FoundBandOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return UnlistedBandOf(bodyId) + 2;
    }

    /// <summary>#677 · Is this floor one of the ones nobody built?</summary>
    public static bool IsFound(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasFoundBand(bodyId) && level < 0 && BandOf(level) == FoundBandOf(bodyId);
    }
}
