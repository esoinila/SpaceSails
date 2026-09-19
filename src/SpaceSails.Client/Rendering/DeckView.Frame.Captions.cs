using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// DeckView.Frame.Captions — THE ONE SEAM EVERY WORD ON THE DECK PLAN GOES THROUGH.
//
// #1218 · The owner filed the same bug from three floors in two days: a forensic line printed through a dig
// plate at his own cache, five captions and a sentry's drum stacked into one unreadable pile in a derelict's
// DEEP HOLD, and a bench's plate over a slop bin's in the park. Three different pairs of drawers, and not one
// of them could have known about the others, because where a caption sat was a pixel literal typed at the
// call site — five of them, in three files.
//
// THE RULING (Fable, 2026-09-18): THE BAND IS THE PLATE. A mark's second line folds into its plate — one
// plate per anchor, the plate grows a second row — and never a second caption at a second hand-typed lift.
// The lift itself is SpaceSails.Core.MarkBand's single constant, spent against each mark's own half-height.
//
// WHAT THIS PAGE ADDS is the book the deck never kept: every band a caption occupies this frame, written
// down as it is laid, so the NEXT caption that wants the same piece of glass takes the row above instead of
// printing through it. Immediate-mode, one pass, no second phase: the frame's pass order is the priority
// order, which is exactly the right one — a room is NAMED before the fittings in it are plated, so a
// console's offer steps over the room's name rather than burying it.

public sealed partial class DeckView
{
    /// <summary>What is already written on the glass this frame, in pixels — left, right, top, bottom of each
    /// band a caption has taken. Cleared at the top of every <see cref="Draw"/>: a band is a fact about ONE
    /// frame, exactly as the lamp is (#708).</summary>
    private readonly List<(double L, double R, double Top, double Bottom)> _bands = [];

    /// <summary>#1218 · The book, opened blank. Called by the conductor before the first pass.</summary>
    private void OpenTheBandBook() => _bands.Clear();

    /// <summary>
    /// THE SEAM. A box that would like to sit at <paramref name="top"/>…<paramref name="bottom"/> between
    /// <paramref name="left"/> and <paramref name="right"/>: what comes back is its TOP once it has been
    /// lifted clear of everything already written there, and the box is written down at that row.
    ///
    /// <para>Lifting to the top of the HIGHEST band it met clears every band it met at once, so a pile five
    /// deep costs five rows and not five passes per row. <see cref="MarkBand.MaxRows"/> bounds it: a caption
    /// that kept climbing would end up naming a mark it is no longer anywhere near, which is the same class
    /// of lie as #1039's magazine count riding the walker.</para>
    ///
    /// <para>The lamp is not consulted, on purpose. On a dark floor some of these bands are drawn to the mask
    /// and thrown away, and they are written down all the same — otherwise where a caption sat would depend
    /// on where the captain happened to be pointing their head, and the words on the deck would crawl as they
    /// turned.</para>
    /// </summary>
    private double SeatTheBand(double left, double right, double top, double bottom)
    {
        double height = bottom - top;
        for (int row = 0; row < MarkBand.MaxRows; row++)
        {
            double highest = double.MaxValue;
            foreach ((double l, double r, double t, double b) in _bands)
            {
                if (left < r && l < right && top < b && t < bottom)
                {
                    highest = Math.Min(highest, t);
                }
            }
            if (double.IsPositiveInfinity(highest) || highest == double.MaxValue)
            {
                break;
            }
            bottom = MarkBand.RowAbove(highest);
            top = bottom - height;
        }

        _bands.Add((left, right, top, bottom));
        return top;
    }

    /// <summary>The ink box of one line of monospace, seated — what comes back is the BASELINE to draw at.
    ///
    /// <para>A MARK is handed straight back where it asked to be: one glyph on the ground is the thing a
    /// caption is drawn over, not a caption (<see cref="MarkBand.IsAMark"/>), and a band rule that shoved the
    /// ✗ around would be moving the ground under the words.</para></summary>
    private float SeatTheCaption(float x, float y, string text, double px, TextAlign align)
    {
        if (MarkBand.IsAMark(text))
        {
            return y;
        }
        double width = CommsBand.WidthOf(text, px);
        double left = LeftEdgeOf(x, width, align);
        double top = SeatTheBand(left, left + width, y - MarkBand.AscentOf(px), y + MarkBand.DescentOf(px));
        return (float)(top + MarkBand.AscentOf(px));
    }

    /// <summary>#1218 · A CAPTION OVER A MARK, at THE lift — the one statement that replaced <c>sy - 12</c>,
    /// <c>sy - 10</c>, <c>dy - 11</c>, <c>dy - 0.9f*scale</c> and <c>ay - 1.6f*scale</c>. What a caller still
    /// brings is its own mark's half-height, because that is the part that genuinely differs between a ✗ at
    /// 16px bold and a console dot of radius three and a half.</summary>
    private float SeatAboveAMark(float sx, float sy, double markHalfPx, string text, double px, TextAlign align) =>
        SeatTheCaption(sx, (float)(sy - MarkBand.LiftAbove(markHalfPx)), text, px, align);

    /// <summary>A row that is SPOKEN FOR and never moves: <c>[E]</c> keeps the row below its console (the
    /// ruling says so in as many words), and an offer that wandered up the screen looking for space would be
    /// an offer pointing at the wrong fitting. Written down so the captions after it step over it.</summary>
    private void ReserveTheRow(float x, float y, string text, double px, TextAlign align)
    {
        double width = CommsBand.WidthOf(text, px);
        double left = LeftEdgeOf(x, width, align);
        _bands.Add((left, left + width, y - MarkBand.AscentOf(px), y + MarkBand.DescentOf(px)));
    }

    /// <summary>Where a line of <paramref name="width"/> starts, given where it was anchored and how it is
    /// hung off that anchor. The band book is in screen rectangles; the pen is in anchors and alignments.
    /// </summary>
    private static double LeftEdgeOf(double x, double width, TextAlign align) => align switch
    {
        TextAlign.Center => x - (width / 2),
        TextAlign.Right => x - width,
        _ => x,
    };
}
