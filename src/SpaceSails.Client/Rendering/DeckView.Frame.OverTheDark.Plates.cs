using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #1353 · <b>A CONSOLE'S PLATE, AND THE TWO THINGS IT NOW ASKS BEFORE IT IS WRITTEN.</b> Split out of the consoles
/// pass (<c>DeckView.Frame.OverTheDark.Consoles.cs</c>) so the pass keeps reading as the pass; the ink, the lift and
/// the band book are the ones every plate has always used.
///
/// <list type="number">
/// <item><b>A plate in a row of doors keeps to its own door's width.</b> The Preservation office's plate is the one
/// plate in a row of numbers that is longer than its door, and drawn on one line it ran over the frontage of the
/// cabins either side of it (QA 2026-09-30). A plate that would run wider than the row's own pitch is folded at its
/// <c> · </c> into rows, stacked upward from the plate's own row, and set in the stencil size that fits — the words
/// are the plate's own, in order; only the line breaks and the size are the renderer's. A plate that fits, which is
/// every plate but that one, is drawn exactly as it was.</item>
/// <item><b>A paper on a desk is read from its room.</b> A <see cref="DeckPlan.ConsoleKind.ViewObject"/> lying on a
/// piece of furniture is a paper, not a notice on a wall: its title is drawn only while nothing — no wall and no
/// door, open or shut — stands between the captain and it. From the corridor the office's sheet is a dot on a desk
/// through a doorway; step in and it has a name. The console the key would answer is never hidden (#212).</item>
/// </list>
///
/// <para>No static field is declared here (#1163).</para>
/// </summary>
public sealed partial class DeckView
{
    /// <summary>The plate over one console, at its dot — folded to its door's width where it has to be, and left
    /// off where it is a paper out of sight of its room.</summary>
    private void PlateTheConsole(
        DeckPlan plan, in DeckPlan.ConsoleSpot console, in State state, float sx, float sy, float dot, bool near,
        Func<double, double, (float X, float Y)> project)
    {
        if (!near && APaperKeptToItsRoom(plan, in console, state.AvatarX, state.AvatarY))
        {
            return;
        }

        double platePx = near ? 10.0 : 9.0;
        RgbaColor ink = near ? ConsoleNear : TextDim;
        string weight = near ? "bold " : "";
        double room = TheDoorsWidthPx(plan, in console, project);

        if (CommsBand.WidthOf(console.Label, platePx) <= room
            || !console.Label.Contains(PlateFold, StringComparison.Ordinal))
        {
            float seat = SeatAboveAMark(sx, sy, dot, console.Label, platePx, TextAlign.Center);
            BackThePlate(plan, in console, sx, seat, console.Label, platePx);
            _renderer.DrawText(
                sx, seat,
                console.Label, ink, near ? "bold 10px monospace" : "9px monospace", TextAlign.Center);
            return;
        }

        // Folded at its separators, and set in the largest half-pixel stencil in which the widest row fits the door.
        string[] rows = console.Label.Split(PlateFold);
        double px = TheStencilThatFits(rows, platePx, room);

        // #1332 D · …and where that stencil is too small to read (the forwarding desk's COLD-CHAIN FORWARDING ran at 4 px
        // across a 55 px door), the widest row is broken once more at its own space, nearest its middle, until the
        // stencil is legible or no row has a space left. The words are the plate's own, in order, as before.
        while (px < LegibleStencilPx && BreakTheWidestRow(rows) is { } broken)
        {
            rows = broken;
            px = TheStencilThatFits(rows, platePx, room);
        }
        string font = string.Create(
            System.Globalization.CultureInfo.InvariantCulture, $"{weight}{px:0.#}px monospace");

        // The LAST row takes the plate's own row, at the one lift every plate is seated at; each row before it is
        // seated on the row above the one under it, through the band book, so nothing drawn later prints through.
        float baseline = SeatAboveAMark(sx, sy, dot, rows[^1], px, TextAlign.Center);
        BackThePlate(plan, in console, sx, baseline, rows[^1], px);
        _renderer.DrawText(sx, baseline, rows[^1], ink, font, TextAlign.Center);
        for (int i = rows.Length - 2; i >= 0; i--)
        {
            double above = MarkBand.RowAbove(baseline - MarkBand.AscentOf(px)) - MarkBand.DescentOf(px);
            baseline = SeatTheCaption(sx, (float)above, rows[i], px, TextAlign.Center);
            BackThePlate(plan, in console, sx, baseline, rows[i], px);
            _renderer.DrawText(sx, baseline, rows[i], ink, font, TextAlign.Center);
        }
    }

    /// <summary>U10-F05 · A DOOR'S STENCIL OVER PAINTED ART WEARS THE DARK BACKING THE ROOM LABELS WEAR. The hotel lobby's
    /// CABIN n and office plates are a hatch's plate drawn straight onto the lobby's photograph, whose ceiling lamps are
    /// the brightest thing in the frame, so a 9 px dim stencil washed out on them (QA 2026-10-29). Room labels have
    /// worn <see cref="RoomLabelPlate"/> since #348 for exactly this and the floor and lift plates wear their own
    /// stencil plate, so a hatch's plate takes the room label's — and only where there IS a photograph under it: a
    /// plate on the bare plan, which reads fine, is drawn exactly as it was. The plate is the float buffer's (flushed
    /// under all text), sized to the run the way <see cref="DrawRoomLabel"/> sizes it.</summary>
    private void BackThePlate(DeckPlan plan, in DeckPlan.ConsoleSpot console, float sx, float baseline, string text, double px)
    {
        if (console.Kind != DeckPlan.ConsoleKind.Hatch || !StandsOnPaintedArt(plan, console.X, console.Y))
        {
            return;
        }

        float w = (float)CommsBand.WidthOf(text, px) + 6f;
        float h = (float)px + 4f;
        FillRect(sx - (w / 2f), baseline - (float)px - 1f, w, h, RoomLabelPlate);
    }

    /// <summary>Is the point inside any of the plan's backdrop photographs (deck units)? A backdrop is anchored at its
    /// top-left and the glass's y runs up, so its box is [X, X + W] by [Y - H, Y] (the draw pass projects (X, Y) and
    /// paints H deck units DOWN the screen).</summary>
    private static bool StandsOnPaintedArt(DeckPlan plan, double x, double y)
    {
        foreach (DeckPlan.Backdrop b in plan.Backdrops)
        {
            if (x >= b.X && x <= b.X + b.W && y <= b.Y && y >= b.Y - b.H)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Where a plate may be folded: the house separator between the halves of a plate.</summary>
    private const string PlateFold = " · ";

    /// <summary>#1332 D · The smallest stencil a folded plate is left at before its widest row is broken at a space.
    /// Slice C's office plate is set at 6 px and slice E's at 6.5, both at or above it, so neither moves (U07-F03: it was 5.5, which left COLD-CHAIN FORWARDING on two rows at 1600x900).</summary>
    private const double LegibleStencilPx = 6.0;

    /// <summary>The largest half-pixel stencil, no larger than the plate's own, in which the widest of
    /// <paramref name="rows"/> fits <paramref name="room"/>.</summary>
    private static double TheStencilThatFits(string[] rows, double platePx, double room)
    {
        double widestPerPx = 0.0;
        foreach (string row in rows)
        {
            widestPerPx = Math.Max(widestPerPx, CommsBand.WidthOf(row, 1.0));
        }

        return Math.Min(platePx, Math.Floor(room / widestPerPx * 2.0) / 2.0);
    }

    /// <summary>The same rows with the widest one that has a space in it broken in two at the space nearest its
    /// middle — or null when no row has a space left.</summary>
    private static string[]? BreakTheWidestRow(string[] rows)
    {
        int widest = -1;
        double widestWidth = -1.0;
        for (int i = 0; i < rows.Length; i++)
        {
            double width = CommsBand.WidthOf(rows[i], 1.0);
            if (rows[i].Contains(' ', StringComparison.Ordinal) && width > widestWidth)
            {
                (widest, widestWidth) = (i, width);
            }
        }

        if (widest < 0)
        {
            return null;
        }

        string row = rows[widest];
        int at = -1;
        for (int i = 0; i < row.Length; i++)
        {
            if (row[i] == ' ' && (at < 0 || Math.Abs(i - (row.Length / 2.0)) < Math.Abs(at - (row.Length / 2.0))))
            {
                at = i;
            }
        }

        var broken = new string[rows.Length + 1];
        Array.Copy(rows, broken, widest);
        broken[widest] = row[..at];
        broken[widest + 1] = row[(at + 1)..];
        Array.Copy(rows, widest + 1, broken, widest + 2, rows.Length - widest - 1);
        return broken;
    }

    /// <summary>
    /// #1353 · <b>HOW WIDE THIS PLATE'S DOOR IS, ON THE GLASS</b> — the pitch of the row of hatches it stands in
    /// (the nearest other <see cref="DeckPlan.ConsoleKind.Hatch"/> plated on the same line), less a gutter of air.
    /// Unbounded for anything that is not a hatch in a row: a plate with no neighbours has nobody to run over.
    /// </summary>
    private static double TheDoorsWidthPx(
        DeckPlan plan, in DeckPlan.ConsoleSpot console, Func<double, double, (float X, float Y)> project)
    {
        if (console.Kind != DeckPlan.ConsoleKind.Hatch)
        {
            return double.PositiveInfinity;
        }

        double pitch = double.PositiveInfinity;
        foreach (DeckPlan.ConsoleSpot other in plan.Consoles)
        {
            if (other.Kind != DeckPlan.ConsoleKind.Hatch || other == console
                || Math.Abs(other.Y - console.Y) > 1e-3f)
            {
                continue;
            }
            pitch = Math.Min(pitch, Math.Abs(other.X - console.X));
        }

        if (double.IsPositiveInfinity(pitch))
        {
            return pitch;
        }

        (float x0, _) = project(console.X, console.Y);
        (float x1, _) = project(console.X + pitch, console.Y);
        return Math.Abs(x1 - x0) - MarkBand.GutterPx;
    }

    /// <summary>
    /// #1353 · <b>IS THIS A PAPER THE CAPTAIN CANNOT SEE FROM WHERE HE STANDS?</b> A viewable thing lying on a piece
    /// of furniture, with a wall or a door — open or shut — on the line between the captain and it.
    /// </summary>
    internal static bool APaperKeptToItsRoom(DeckPlan plan, in DeckPlan.ConsoleSpot console, double ax, double ay)
    {
        if (console.Kind != DeckPlan.ConsoleKind.ViewObject || !LiesOnFurniture(plan, console.X, console.Y))
        {
            return false;
        }

        if (!SurfaceCollision.HasLineOfSight(ax, ay, console.X, console.Y, plan.CollisionField))
        {
            return true;
        }

        var doors = new SurfaceCollision.Segment[plan.Doors.Length];
        for (int i = 0; i < doors.Length; i++)
        {
            DeckPlan.Door d = plan.Doors[i];
            doors[i] = new SurfaceCollision.Segment(d.X1, d.Y1, d.X2, d.Y2);
        }
        return !SurfaceCollision.HasLineOfSight(ax, ay, console.X, console.Y, doors);
    }

    private static bool LiesOnFurniture(DeckPlan plan, double x, double y)
    {
        foreach (DeckPlan.FurnitureSpot f in plan.Furniture)
        {
            if (x >= Math.Min(f.X0, f.X1) && x <= Math.Max(f.X0, f.X1)
                && y >= Math.Min(f.Y0, f.Y1) && y <= Math.Max(f.Y0, f.Y1))
            {
                return true;
            }
        }
        return false;
    }
}
