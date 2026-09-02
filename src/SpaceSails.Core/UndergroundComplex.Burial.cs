using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #1063 · CLAUSE FOUR: ONE SPECIMEN IS KEPT ────────────────────────────────────────────────────────
    //
    // The erasure procedure the issue states is four clauses long and the first three are one line each in
    // this file's neighbour: remove the element, remove its marks (both are Burial.IsFilled consulted at
    // HasFoundBand), and let the town above keep living (nothing else about the site changes — the listed
    // floors, the band nobody listed, the plates, the people, the prices, all untouched). This file is the
    // fourth, and it is the one the owner's research kept finding:
    //
    //     A SHORT STAIR DOWN TO A SINGLE OLD DOOR, PRESERVED FOR DISPLAY.
    //
    // The world keeps one souvenir of what it will not admit existed, and nobody finds that strange. It is
    // the whole beat in one object: a thing that is obviously older than the building it stands in, kept
    // deliberately, labelled by nobody, opening onto nothing.
    //
    // IT IS DRAWN IN THE FOUND BAND'S OWN IDIOM AND IT IS ON A LISTED FLOOR. That is the entire tell and it
    // is a material rather than a sentence: the leaf takes DeckPlan.Wall.IsSeamless — the third idiom, which
    // belongs to no palette and says who built nothing (§13.20) — set into a recess of ordinary poured hull.
    // The rag's own line is exactly this and does not know it: "the old kerbs make a handsome course of
    // masonry in the new wall." A Scully sees a bricked-up doorway kept as a feature in a refurbished
    // concourse, which is a thing that exists in every old building anybody has ever worked in.
    //
    // NOTHING IS SAID ABOUT IT. No plate, no sign console, no card, no note, no glyph. #1063 authored no line
    // for the specimen and this file invents none — the object is the whole statement, and a caption would be
    // the one helpful sentence that kills the feature (§13.20's own lesson).

    /// <summary>#1063 · What the captain reads in the room the ledger is kept in. <b>The authored line and
    /// nothing else</b>, behind the paper glyph every operational-paper line in this building already wears.
    ///
    /// <para><b>The anomaly is the brevity, and the brevity is the whole evidence.</b> The issue's own
    /// framing is that this ledger is otherwise meticulous — it cites an instruction number for every job it
    /// has ever recorded — and that this job cites none. Those two contrasting cited lines are NOT authored
    /// anywhere, and this file does not write them: a pair of invented ledger entries either side of the
    /// authored one would be two sentences of canon written by an implementer to make a point the authored
    /// sentence already makes on its own. <i>"…per instruction"</i>, with no instruction named, is a whole
    /// piece of evidence by itself.</para></summary>
    // FABLE: line needed — the two CITED maintenance-ledger entries that bracket this one (each with its own
    // instruction number), so the missing number reads as an omission rather than as a house style. #1063
    // authored the anomalous entry only. Shipping it alone reads; the pair would read better.
    public const string MaintenanceLedgerLine = "📋 " + Burial.LedgerLine;

    /// <summary>#1063 · Which floor of this site carries the specimen, or null where there is none.
    ///
    /// <para><b>The listed bottom</b> — the deepest floor the building admits to, which is the deepest floor a
    /// visitor is ever taken to and therefore the only floor a display piece could sensibly be kept on. It is
    /// also, after the fill, the floor the shaft now ends at: the last thing in the building, at the bottom of
    /// the building, and past it there has never been anything.</para>
    ///
    /// <para>The one legal caller of <see cref="FoundBandSeeded"/> outside <see cref="HasFoundBand"/>, and it
    /// has to be: a filled ground answers no to every question about halls, so the only way to ask "was there
    /// something here to keep a souvenir OF" is to ask the seed, before the burial. It publishes a LEVEL and
    /// nothing else — no band, no depth, no way down — so it cannot be turned back into the halls.</para></summary>
    public static int? SpecimenFloorOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return Burial.IsFilled(bodyId) && FoundBandSeeded(bodyId) ? DepthOf(bodyId) : null;
    }

    /// <summary>#1063 · Is this the floor with the display piece on it? False everywhere in a world where
    /// nothing has been filled in, which is almost every world.</summary>
    public static bool HasSpecimenOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return SpecimenFloorOf(bodyId) == level;
    }

    /// <summary>#1063 · How deep the recess runs off the spine. <b>The lift alcove's own five du</b>, and it
    /// is that number rather than a new one because the piece has to read as the same class of pocket the
    /// building already cuts — a step down off the corridor and no further. A recess a captain could stand
    /// three abreast in would be a room, and a room is a thing with a purpose.</summary>
    public const double SpecimenRecessDu = 5.0;

    /// <summary>#1063 · One preserved doorway, as the segment it is drawn across. The client puts it into the
    /// deck twice — as a leaf that is drawn shut and as a wall a body cannot cross — which is exactly what
    /// <see cref="LockedDoor"/> already does, minus the sign console, because there is no sign.</summary>
    public readonly record struct Specimen(double X1, double Y1, double X2, double Y2)
    {
        /// <summary>The middle of the leaf.</summary>
        public double X => (X1 + X2) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y1 + Y2) / 2.0;
    }

    /// <summary>#1063 · WHERE the recess is cut, decided from the field alone so the piece stands in the same
    /// place on every visit and the answer never depends on which floor asked.
    ///
    /// <para>It takes a <b>blind end of the spine</b> — the ground past the last rib's chambers, which is the
    /// same ground <see cref="ServiceShaftAt"/> is written to find and for the same reason: it is the only
    /// stretch of corridor in the building that nothing else is ever laid against. It is cut into the
    /// <b>upper</b> face, so the goods car (always lower) cannot be beside it whichever end it takes, and it
    /// is held a car's own clearance off the cage, which is the one other thing on that face.</para>
    ///
    /// <para>Null where the ribs run all the way to both caps and there is no blind end to have — in which
    /// case the site keeps no specimen, which is the honest answer rather than a piece jammed into somebody
    /// else's room.</para></summary>
    public static (double X, double Y)? SpecimenRecessAt(in SurfaceLayout.Field field)
    {
        IReadOnlyList<(int Ordinal, double X)> ribs = RibColumnsOn(field);
        if (ribs.Count == 0)
        {
            return null;
        }

        (double cageX, double cageY) = ShaftAt(field);
        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;

        // The same two numbers ServiceShaftAt measures a blind end with, asked the same way: how far a rib's
        // chambers reach along the spine at their biggest, and how much air a box in that wall wants.
        double reach = CorridorHalf + (RoomWidthDu * DeepestRoomScale) + 1.5;
        double clear = ShaftHalf + ShaftClearDu;

        (double Lo, double Hi)[] ends =
        [
            (left + clear, ribs[0].X - reach - clear),
            (ribs[^1].X + reach + clear, right - clear),
        ];

        // Widest end wins, and the left one breaks a tie — a rule, so the piece cannot move when somebody
        // re-times a rib. Sorting rather than max() because ties have to break the same way every time.
        double bestX = double.NaN, bestWidth = -1;
        foreach ((double lo, double hi) in ends)
        {
            double width = hi - lo;
            if (width < 2 * ShaftHalf)
            {
                continue;   // not enough wall to cut a mouth in
            }
            double x = (lo + hi) / 2.0;
            if (Math.Abs(x - cageX) < (2 * ShaftHalf) + ShaftClearDu)
            {
                continue;   // never shoulder to shoulder with the car everybody arrives in
            }
            if (width > bestWidth + 0.001)
            {
                (bestX, bestWidth) = (x, width);
            }
        }

        return double.IsNaN(bestX) ? null : (bestX, cageY + CorridorHalf);
    }

    /// <summary>#1063 · The preserved leaf on this floor, or null where this floor keeps none — which is every
    /// floor of every site nobody has filled in.</summary>
    public static Specimen? SpecimenOn(string bodyId, int level, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (!HasSpecimenOn(bodyId, level) || SpecimenRecessAt(field) is not { } at)
        {
            return null;
        }
        double far = at.Y + SpecimenRecessDu;
        return new Specimen(at.X - ShaftHalf, far, at.X + ShaftHalf, far);
    }

    /// <summary>#1063 · Cut the recess into the floor being built: the two sides in ordinary poured hull —
    /// the new work, which is what a preserved thing is set into — the mouth handed to the spine's own sweep
    /// at the pocket's own width (#819's law: an alcove's mouth is cut to the ALCOVE's width and never to a
    /// corridor's), and the ground claimed so no later placer lays a room across it.
    ///
    /// <para>The far end is NOT built here. It is the leaf, and it goes out on the plan as
    /// <see cref="FloorPlan.Specimen"/> so the renderer can draw it in a material this list cannot carry —
    /// the same reason #759's glazing is kept out of <c>Walls</c>: the list a segment arrives in is what
    /// decides its ink.</para></summary>
    private static void CarveSpecimen(
        string bodyId, int level, in SurfaceLayout.Field field,
        List<SurfaceLayout.Wall> walls,
        List<(double Y, double Lo, double Hi)> alcoveMouths,
        List<(double X0, double Y0, double X1, double Y1)> claimed)
    {
        if (!HasSpecimenOn(bodyId, level) || SpecimenRecessAt(field) is not { } at)
        {
            return;
        }

        double far = at.Y + SpecimenRecessDu;
        walls.Add(new(at.X - ShaftHalf, at.Y, at.X - ShaftHalf, far, true));
        walls.Add(new(at.X + ShaftHalf, at.Y, at.X + ShaftHalf, far, true));
        alcoveMouths.Add((at.Y, at.X - ShaftHalf, at.X + ShaftHalf));
        claimed.Add((at.X - ShaftHalf - 1.5, at.Y, at.X + ShaftHalf + 1.5, far + 1.5));
    }
}
