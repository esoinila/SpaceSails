namespace SpaceSails.Core;

/// <summary>
/// #649 · HOW BIG A DECK UNIT IS, stated once.
///
/// <para>The game has always had a scale and never written it down. The deck grid is in <b>deck units</b>;
/// the fiction is in <b>metres</b> — <see cref="Landmarks"/> records the Phobos monolith's height as 85 m,
/// away-team briefs quote distances in metres, the worldbuilding notes talk about an 85 m boulder — and
/// nothing anywhere converted between the two. So every time something had to be drawn at its real size,
/// somebody typed a deck-unit number that felt right, which is bug class 1 (<i>unaudited client geometry
/// literals: found wrong three times out of three</i>) with the wrongness baked in before the literal was
/// even typed.</para>
///
/// <para><b>The anchor is the captain.</b> The avatar is 1.4 du across (<c>DeckPlan.AvatarRadius</c> × 2)
/// and a human in a hard suit is a shade under a metre across the shoulders. That fixes everything else:
/// the walked field is 310 × 260 du, so a landing site is about 220 × 180 m of regolith, which is the right
/// order for somewhere you can cross on foot but not casually. Nothing here is a new decision — it is the
/// scale the game has been drawing at all along, finally written where a test can read it.</para>
///
/// <para>The one thing it is FOR: an object whose real dimensions are canon can now be drawn at them
/// instead of at a guess. <see cref="Monolith"/> is the first, and it is the whole reason this exists.</para>
/// </summary>
public static class SurfaceScale
{
    /// <summary>The captain's width on the grid — the same 1.4 du the collision uses. Mirrored from the
    /// client's <c>DeckPlan.AvatarRadius</c>, which cannot be referenced from Core; a client guard proves
    /// the two agree, because a mirror nobody checks is exactly how the field envelope drifted (#573).</summary>
    public const double CaptainWidthDu = 1.4;

    /// <summary>A suited human across the shoulders. Not a free parameter — a hard suit with a life pack is
    /// a little under a metre wide, and everything else in this file follows from it.</summary>
    public const double CaptainWidthMetres = 0.98;

    /// <summary>The conversion. 0.7 m to the deck unit.</summary>
    public const double MetresPerDeckUnit = CaptainWidthMetres / CaptainWidthDu;

    /// <summary>A real-world size, on the grid.</summary>
    public static double DeckUnits(double metres) => metres / MetresPerDeckUnit;

    /// <summary>A grid size, in the world.</summary>
    public static double Metres(double deckUnits) => deckUnits * MetresPerDeckUnit;
}
