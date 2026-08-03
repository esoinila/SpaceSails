namespace SpaceSails.Core;

/// <summary>
/// #649 · THE FALSE SLAB — what stands at the heart of Miranda's canon maze, now that the word
/// <i>monolith</i> is reserved for the one object on Phobos.
///
/// <para>Owner's ruling (2026-08-03, <c>docs/worldbuilding-notes.md</c> §8): <i>"There is ONE monolith. Not
/// a class of object, not a kind of landmark a generator can roll twice. If two grounds both call something
/// 'the monolith', one of them is wrong and has to be renamed — the word is reserved."</i> Two grounds did:
/// the map cards paced off <c>the monolith</c> on Phobos (<see cref="Landmarks.PhobosMonolith"/>, since
/// #164) while the drawn slab stood on Miranda. The card was never the half that was wrong.</para>
///
/// <para><b>Why a rename was not enough on its own.</b> The same ruling says the monolith is not <i>a kind
/// of landmark</i>. Leaving a second nameless ancient slab at the middle of a second maze would have obeyed
/// the letter and lost the point — you would still have met two of the thing that there is one of. So
/// Miranda's centre is a DIFFERENT CLASS of object: <b>quarried, mortared, tool-marked and weathering</b>.
/// Everything the monolith's card says it is not (<see cref="Monolith.Lore"/>: no seam, no weathering, no
/// dust on the face), this one plainly is.</para>
///
/// <para><b>And it explains nothing</b>, which is the standing canon law. The card does not say who built
/// it, when, or why, and never uses the word for the thing on Phobos. It reports a made object on a moon and
/// leaves the captain to do the arithmetic — the same law as the Reever origin: the inference is the horror,
/// and confirming it kills it. What the geometry says out loud is enough: this one is in a boxed backyard,
/// walled in on three sides by somebody's corridor rows, and the real one is not in anything at all.</para>
///
/// <para><b>The maze itself is untouched.</b> Every constant here is byte-for-byte the number the slab used
/// to carry, so Miranda's ground — canon since #313, the owner's own hand-built geometry — generates exactly
/// the walls it generated yesterday. Only the NAME, the CARD and which moon the ceremony belongs to have
/// moved.</para>
/// </summary>
public static class FalseSlab
{
    /// <summary>The body it stands on.</summary>
    public const string BodyId = "miranda";

    /// <summary>Does the false slab stand on THIS ground? Miranda's canon site only — site 0, the Wild
    /// Plain, the empty-salt ground <c>SurfaceLayout.For</c> routes to the authored maze for. Miranda's
    /// seeded sites (the Shadowed Rille, the Ridge Camp) build no maze and so have no slab in them.
    ///
    /// <para>One predicate, exactly like <see cref="Monolith.StandsOn"/>, and for exactly the same reason:
    /// the geometry, the label, the swept apron and the card must not each decide this for themselves. That
    /// is how the monolith came to be on two moons.</para></summary>
    public static bool StandsOn(string? bodyId, string? siteSalt) =>
        string.Equals(bodyId, BodyId, System.StringComparison.Ordinal) && string.IsNullOrEmpty(siteSalt);

    /// <summary>Half-width of the slab. Six deck units across, sized to the cell the canon maze rows leave
    /// for it (the rows at anchor +12, +6 and −4 are canon and stay exactly as authored).</summary>
    public const double HalfWidth = 3.0;

    /// <summary>Half-height of the slab. Seven deep, filling its cell without crossing the maze rows.</summary>
    public const double HalfHeight = 3.5;

    /// <summary>The swept apron it stands on — drawn, never collided, so it can never become a fence.</summary>
    public const double ApronRadius = 15.0;

    /// <summary>How many segments the apron ring is drawn with.</summary>
    public const int ApronSegments = 28;

    /// <summary>Four stubs at the compass points just inside the apron: the remains of an approach.</summary>
    public const double MarkerRing = 11.0;
    public const double MarkerHalf = 0.9;

    /// <summary>The label on the ground. It does not contain the reserved word, and a guard proves it.</summary>
    public const string ConsoleLabel = "◫ THE FALSE SLAB";

    /// <summary>The scheme name the location line reads on this ground.</summary>
    public const string Scheme = "THE FALSE-SLAB MAZE";

    /// <summary>The picture, on [E].</summary>
    public const string ArtUrl = "art/false-slab.jpg";

    /// <summary>What the card says beside the picture. It describes; it does not account for. No builder is
    /// named, no purpose is given, and the other object is never mentioned — a card that said <i>"a copy
    /// of"</i> would have explained the thing it was copying, which is the one move this world does not
    /// make.</summary>
    public const string Lore =
        "You put a glove on it because everyone does, and this one comes away grey.\n\n" +
        "Nine courses of quarried block, mortared. The joints are wide at the top and tight at the bottom, " +
        "which is what happens when the last three courses go up in a hurry. There is a drill scar down the " +
        "third course where somebody's rig walked, and they carried on anyway.\n\n" +
        "The dust holds to the face. Four billion years of nothing much has taken the north edge off it and " +
        "left a rounded shoulder that no mason cut. It is weathering the way a made thing weathers, on a " +
        "timescale that has an answer.\n\n" +
        "The corridors around it were laid to bring somebody past it in a particular order. Whoever went to " +
        "that much trouble had seen something worth the trouble, and it was not this.";
}
