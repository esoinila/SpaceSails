namespace SpaceSails.Core;

/// <summary>
/// #561 · THE LEFT-EDGE INSTRUMENT COLUMN, MEASURED ONCE.
///
/// <para>Owner, playtesting Miranda on the regolith: <i>"the UI could use a little more space between the
/// health and the motion tracker."</i> He was reading two defects stacked, and they had one cause.</para>
///
/// <para><b>The cause.</b> <c>DeckView</c> carried a typed <c>SanityColumnTop = 82.0</c> whose comment said
/// it was "the SANITY plate's base bottom ≈ 70px + a small consistent gap" — true the day #330 wrote it.
/// #453 then hung the five condition pips under the nerve bar, at <c>y0 + h + 22</c>, and the gauge grew to
/// y=80. Nobody moved the 82. That is this repo's named class — <b>a constant governing one thing while
/// another thing grew under it</b> — and it broke the column in two places at once:</para>
/// <list type="number">
///   <item>the backing plate still ended at y=70, so the pips it was drawn to back hung off it onto bare
///   regolith;</item>
///   <item>the tracker's caption started at cap-top ≈ 81, one pixel under a pip bottom of 80, so the nerve
///   block and the fan read as ONE instrument instead of two.</item>
/// </list>
///
/// <para><b>The fix is that nothing here is typed twice.</b> The block knows where its own pips end; the
/// plate is drawn to <see cref="NerveBlock.PlateHeight"/>, which is that bottom plus the plate's own inset;
/// and the tracker's column top is <see cref="NerveBlock.ColumnTop"/> — the plate's published bottom plus
/// <see cref="GapPx"/>. Hang something else under the pips tomorrow and both the plate and the fan move
/// with it, because there is no second number to forget.</para>
/// </summary>
public static class HudColumn
{
    /// <summary>#561 · THE AIR BETWEEN TWO INSTRUMENTS. Below the nerve block's plate, before the motion
    /// tracker's caption may begin. This is the owner's "a little more space", written down as the one
    /// number that means it — not a slack allowance inside somebody's else's constant.</summary>
    public const double GapPx = 12.0;

    /// <summary>The margin the nerve plate keeps around its own contents. It is what the plate already
    /// holds at its left and right edges (<c>x0 − 8</c>, <c>w + 16</c>); the bottom now keeps the same,
    /// which is the whole of defect (2) — a plate that backs everything it was drawn to back.</summary>
    public const double PlateInsetPx = 8.0;

    /// <summary>The plate's head above the bar, which holds the "NERVE" name (#380 item 2).</summary>
    public const double PlateHeadPx = 20.0;

    /// <summary>#453 · How far under the bar's BOTTOM the condition pips are hung.</summary>
    public const double PipDropPx = 22.0;

    /// <summary>One nerve gauge's vertical geometry, in screen pixels. The horizontal half (the bar's width,
    /// the label size) is the renderer's business; only what stacks DOWN the column belongs here, because
    /// only that decides where the next instrument may start.</summary>
    /// <param name="BaseY">The bar's top — the head of the block.</param>
    /// <param name="BarHeight">The nerve bar itself.</param>
    /// <param name="PipSize">One condition pip, square (#453).</param>
    public readonly record struct NerveBlock(double BaseY, double BarHeight, double PipSize)
    {
        /// <summary>The backing plate's top edge.</summary>
        public double PlateTop => BaseY - PlateHeadPx;

        /// <summary>The condition pips' top edge (#453).</summary>
        public double PipsTop => BaseY + BarHeight + PipDropPx;

        /// <summary>The lowest ink the block puts on the screen.</summary>
        public double PipsBottom => PipsTop + PipSize;

        /// <summary>The plate's bottom edge — its lowest content plus its own inset. Defect (2): this used
        /// to be a typed <c>h + 42</c> that predated the pips.</summary>
        public double PlateBottom => PipsBottom + PlateInsetPx;

        /// <summary>What the plate is drawn with. Derived, so it can never again end above its contents.</summary>
        public double PlateHeight => PlateBottom - PlateTop;

        /// <summary>#561 · Where the NEXT instrument in the column may begin: this block's published bottom
        /// plus <see cref="GapPx"/>. Defect (1): this used to be the typed 82.0.</summary>
        public double ColumnTop => PlateBottom + GapPx;
    }

    /// <summary>On the regolith the gauge is the full-size head of the column and the motion tracker seats
    /// beneath it (#324/#330). Plate 10 → 88, pips 70 → 80, column top 100.</summary>
    public static NerveBlock Surface => new(BaseY: 30.0, BarHeight: 18.0, PipSize: 10.0);

    /// <summary>Aboard and ashore the gauge whispers, tucked below the top-left deck chrome. Plate 92 → 162,
    /// pips 147 → 154, column top 174 (nothing seats under it today; the law is the same either way).</summary>
    public static NerveBlock Aboard => new(BaseY: 112.0, BarHeight: 13.0, PipSize: 7.0);

    /// <summary>The layout in force for a given gauge, so the drawing and the tracker cannot disagree about
    /// which block they are measuring from.</summary>
    public static NerveBlock For(bool compact) => compact ? Aboard : Surface;
}
