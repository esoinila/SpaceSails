using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #698 · WHAT YOU LEFT, DRAWN ON THE DECK YOU LEFT IT ON.
///
/// <para>Owner, on B12 of the clinic, within the hour of #691 shipping: <i>"I dropped 3 files on somebody
/// here but there was nothing marked onto the map?"</i></para>
///
/// <para>That is the judgement call #691 shipped open, answered. The store knew where everything was lying;
/// the pulsed sentence said so once and then went away; and the deck — the one surface a captain actually
/// navigates by — said nothing at all. #615's law is that leaving never destroys, and a way back that runs
/// on the memory of a line of text is not a way back (#600's lift went <b>down</b> for the same reason: an
/// audit can prove you can REACH a thing without ever proving you can get home to it).</para>
///
/// <h3>Why a region and not a generator argument</h3>
/// <para>Left things exist on Hive floors, on the regolith, and on a derelict's steel — three different
/// builders (<c>HiveInterior</c>, <c>MoonSurface</c>, <c>WreckInterior</c>) with nothing in common but the
/// <see cref="DeckPlan"/> they hand back. Threading a store through all three would put the same four lines
/// in three files and give the next deck a fourth place to forget. So this is an APPENDED REGION, the idiom
/// the hidden door, the outpost hut and the head office's beat consoles already use — one composer, called
/// once per rebuild, on whatever the excursion is standing on.</para>
///
/// <h3>What it is, physically</h3>
/// <para>A console spot and nothing else: no wall, no structure, no collision segment. The owner's brief
/// said scenery that must not get in the way, and a mark you can be blocked by is a mark that can pin a
/// captain against their own paperwork. It carries the <b>same [E] the pickup already answers</b> — the
/// recovery runs ahead of console dispatch (#691), so when this mark is the nearest console the offer
/// drawn over it is the true one, which is exactly the law <c>DeckView</c> learned the hard way.</para>
/// </summary>
public static class LeftMarks
{
    /// <summary>The marks for one floor, ready to append. Empty when nothing is lying about — a floor with
    /// no marks appends no consoles, which is how the mark disappears the moment the spot empties.</summary>
    public static DeckPlan.DeckRegion Region(LeftBehind ground, int floor)
    {
        System.ArgumentNullException.ThrowIfNull(ground);

        var marks = new List<DeckPlan.ConsoleSpot>();
        foreach ((int squareX, int squareY) in ground.SpotsOn(floor))
        {
            // The square's own centre, the same point the swept-grid marks and the probe glyphs use — so a
            // captain who set something down mid-square finds the mark where the SQUARE is, which is the
            // unit the recovery ring is measured in. Anything else would draw one law and obey another.
            (double x, double y) = BeachComber.SquareCenter(squareX, squareY);
            marks.Add(new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.LeftBehind, (float)x, (float)y, LeftBehind.MarkerLabel));
        }

        return new DeckPlan.DeckRegion([], [.. marks], [], []);
    }
}
