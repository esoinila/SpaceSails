namespace SpaceSails.Core;

/// <summary>
/// #1218 · THE ROW ABOVE A MARK — the lift every caption on the deck plan is drawn at, said once.
///
/// <para><b>Why it exists.</b> <see cref="CommsBand"/> reserves the mothership's own top-centre rows and had
/// an opinion about nothing else. Every upward caption in the whole <c>DeckView.Frame*</c> family was a local
/// literal instead — <c>sy - 12</c> over a cache's ✗, <c>sy - 10</c> over a console, <c>dy - 0.9f*scale</c>
/// over a droid, <c>ay - 1.6f*scale</c> over the captain, <c>dy - 11</c> over a dropped chest — five lifts in
/// three files, two of them fixed pixels and three scale-relative, and not one of them aware that any of the
/// others existed. So when two of them landed on the same point nobody had written anything down that could
/// notice: the owner read <i>"yours · something walks near it"</i> printed straight through
/// <i>"🗺 DIG AT THE X"</i> at his own cache, five captions and a sentry's drum stacked into one unreadable
/// pile in a derelict's DEEP HOLD, and <i>A STEEL BENCH — SIT DOWN</i> over
/// <i>SLOP · FOOD WASTE ONLY · NO TRAYS</i> in the park.</para>
///
/// <para><b>The ruling (#1218, 2026-09-18): THE BAND IS THE PLATE.</b> A mark's second line folds into its
/// plate — one plate per anchor, the plate grows a second row — and never a second caption at a second
/// hand-typed lift. The row BELOW a console stays <c>[E]</c>'s.</para>
///
/// <para><b>The lift is one constant and a measurement, not one number.</b> <see cref="ClearPx"/> is the
/// whole of the decision: how much daylight there is between a mark's own ink and the words over it. What a
/// caller adds to it is its MARK'S OWN HALF-HEIGHT, which it already knows and which is the reason the five
/// literals disagreed in the first place — a ✗ drawn at 16px bold and a console dot of radius 3.5 are not the
/// same distance from their anchor, and a single pixel lift for both would have buried one and stranded the
/// other. One decision, spent five times.</para>
///
/// <para><b>And a MARK IS NOT A CAPTION.</b> <see cref="IsAMark"/> is the line between them: a single glyph
/// on the ground — a ✗, a ×, a 🧰, a lifeboat cradle's ▮ — stands FOR something and does not name it. It is
/// the thing a caption is drawn over, so it neither takes a row nor pushes one out of the way. The whole
/// vocabulary of this deck plan is one-glyph marks with words above them, and a band rule that forgot that
/// would start shoving the ground itself around.</para>
/// </summary>
public static class MarkBand
{
    /// <summary>THE ONE CONSTANT — the daylight between a mark's own ink and the words that name it, in
    /// pixels. Everything else on this page is arithmetic.</summary>
    public const double ClearPx = 7.0;

    /// <summary>…and between two ROWS of one plate, which is a smaller gap on purpose: the rows belong
    /// together, and a plate whose rows were as far apart as a plate is from its mark would read as two
    /// captions again — which is the thing this page exists to stop.</summary>
    public const double GutterPx = 2.0;

    /// <summary>How far a line of monospace reaches above its own baseline. About the cap height of the face
    /// rather than a whole em: a band measured to the em would call two rows that read perfectly well an
    /// overlap, and a rule that cries at legible text is a rule somebody turns off.</summary>
    public const double CapHeightRatio = 0.72;

    /// <summary>…and below it, for the descenders in "yours · something walks near it".</summary>
    public const double DescenderRatio = 0.18;

    /// <summary>How many rows one plate may ever grow to. Five was the deepest pile the owner photographed
    /// (a derelict's DEEP HOLD), so a plate that needed a sixth would be a floor nobody has designed — and a
    /// caption that kept climbing would end up naming a mark it was no longer anywhere near, which is this
    /// repository's third named bug class wearing a tidier hat.</summary>
    public const int MaxRows = 6;

    /// <summary>How far a line of <paramref name="px"/> monospace reaches above its baseline.</summary>
    public static double AscentOf(double px) => px * CapHeightRatio;

    /// <summary>…and below it.</summary>
    public static double DescentOf(double px) => px * DescenderRatio;

    /// <summary>THE ROW ABOVE A MARK: how far over its anchor a caption's baseline sits, for a mark whose own
    /// ink reaches <paramref name="markHalfPx"/> either side of that anchor.</summary>
    public static double LiftAbove(double markHalfPx) =>
        (markHalfPx > 0 ? markHalfPx : 0) + ClearPx;

    /// <summary>The bottom edge a box takes when it has to sit clear of something whose top is
    /// <paramref name="occupiedTop"/> — the plate growing its next row up.</summary>
    public static double RowAbove(double occupiedTop) => occupiedTop - GutterPx;

    /// <summary>
    /// A MARK IS NOT A CAPTION — one glyph on the ground stands FOR a thing rather than naming one.
    ///
    /// <para>Counted in GRAPHEMES and not in chars, because the chest is 🧰: a surrogate pair is one mark to
    /// the eye and two to <c>string.Length</c>, and a rule that read the second number would have decided
    /// that half the ground vocabulary was a sentence.</para>
    /// </summary>
    public static bool IsAMark(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;    // nothing said is nothing to make room for
        }
        var walk = System.Globalization.StringInfo.GetTextElementEnumerator(text);
        int glyphs = 0;
        while (walk.MoveNext())
        {
            if (++glyphs > 1)
            {
                return false;
            }
        }
        return true;
    }
}
