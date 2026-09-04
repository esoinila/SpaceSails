using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: how a room is read — what it is dressed as, and its own u/v axes.
public static partial class RingOffice
{
    // ── HOW THE ROOM IS READ ──────────────────────────────────────────────────────────────────────────

    /// <summary>Which dressing a room takes. Off its PLATE where it has one of the block's own
    /// (<see cref="UndergroundComplex.ParkViewPlates"/>), because the plate is the only thing that says what
    /// the room is for — and a room whose plate says NEGOTIATION ROOM and whose floor says twelve
    /// workstations is the sim and the sentence disagreeing, which is a bug class this house has a name
    /// for.</summary>
    public enum Dressing
    {
        /// <summary>Banks of workstations facing the glass. The default, and what most of the register
        /// means.</summary>
        Cubicles,

        /// <summary>One long table with chairs down both sides.</summary>
        LongTable,

        /// <summary>Shelving down both piers and a row of reading desks.</summary>
        ReadingRoom,

        /// <summary>A counter facing the door, with the desks behind it.</summary>
        Reception,

        /// <summary>Shelving and a bench. What a corner office with no view and the back of house get:
        /// cheap is fine, empty is not.</summary>
        Plain,

        /// <summary>#821 · A terrace of cubicles down one pier, a basin run down the other, and a bench to
        /// wait on. The block's own public washroom.</summary>
        Washroom,

        /// <summary>#775 · A run of laboratory bench down one pier with stools at it, and banks of desks
        /// behind. The owner's second beat — <i>"the work done there is lab work and office work related to
        /// that lab work, so the space should look like that"</i> — as one room rather than as two.
        ///
        /// <para>Never returned by <see cref="DressingFor"/>, because nothing on a door says it: it is
        /// handed to <see cref="Fit(in UndergroundComplex.RingRoom, Dressing)"/> by the carve, which is the
        /// only thing that knows it is standing on a LABORATORIES floor.</para></summary>
        LabHall,
    }

    /// <summary>#821 · Is this the block's public washroom? Off the PLATE, which is the only thing that says
    /// what a room is for — the same one question <see cref="DressingFor"/> asks, published so a guard, a
    /// renderer and the size gate cannot each answer it their own way.</summary>
    public static bool IsWashroom(in UndergroundComplex.RingRoom room) =>
        room.Plate.Contains("WASHROOM", StringComparison.Ordinal);

    /// <summary>What this room is dressed as. The back of house keeps #801's plates and its own character —
    /// a potting shed is not an office — and a corner room has no view to sit facing, so both take the plain
    /// version.</summary>
    public static Dressing DressingFor(in UndergroundComplex.RingRoom room)
    {
        // #821 · THE WASHROOM IS ASKED FIRST, and before the view clause, because it is the one dressing
        // that is not an office at all: a room of cubicles with desks in it would be the plate and the floor
        // disagreeing, which is the bug class this switch was written to be incapable of.
        if (IsWashroom(in room))
        {
            return Dressing.Washroom;
        }
        if (!room.HasView || room.Side == UndergroundComplex.RingSide.Far)
        {
            return Dressing.Plain;
        }
        if (room.Plate.Contains("NEGOTIATION", StringComparison.Ordinal))
        {
            return Dressing.LongTable;
        }
        if (room.Plate.Contains("READING ROOM", StringComparison.Ordinal))
        {
            return Dressing.ReadingRoom;
        }
        if (room.Plate.Contains("RECEPTION", StringComparison.Ordinal))
        {
            return Dressing.Reception;
        }
        return Dressing.Cubicles;
    }

    /// <summary>Does this room earn the service strip? Measured on the STREET FRONTAGE, which is the
    /// dimension the owner was looking down when he called the room big — and the same dimension the door
    /// count is scaled on, so "big" means one thing in this file.</summary>
    /// <para>#821 · …and never the block's own washroom, however wide it is. The service strip is the tier a
    /// PREMIUM OFFICE earns — a kitchenette, two staff WCs and the privacy booths — and a public washroom
    /// that grew a kitchenette would be the amenity ladder handing the same room both rungs.</para>
    public static bool IsBigSuite(in UndergroundComplex.RingRoom room) =>
        room.HasView && room.Side != UndergroundComplex.RingSide.Far && !IsWashroom(in room)
        && FrontageOf(room) >= BigSuiteDu;

    /// <summary>How much street face a room presents — X on the two bands, Y on the two ends.</summary>
    public static double FrontageOf(in UndergroundComplex.RingRoom room) =>
        room.Side is UndergroundComplex.RingSide.Near or UndergroundComplex.RingSide.Far
            ? room.X1 - room.X0
            : room.Y1 - room.Y0;

    // ── THE FRAME ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A room read in its OWN axes — u along the street face, v inward from the street toward the glass.
    ///
    /// <para>The same trick <c>RingBox</c> plays with its from/to corners, and for the same reason: one
    /// layout sentence furnishes a room on any of the four sides without ever asking which side it is on.
    /// Get this wrong and every desk on the block's two ends is laid at ninety degrees to the room it is
    /// in — which is the shape of bug that is invisible in a diff and obvious on the first boot.</para>
    /// </summary>
    private readonly struct Frame
    {
        private readonly UndergroundComplex.RingSide _side;
        private readonly double _x0, _y0, _x1, _y1;

        internal Frame(in UndergroundComplex.RingRoom room)
        {
            _side = room.Side;
            (_x0, _y0, _x1, _y1) = (room.X0, room.Y0, room.X1, room.Y1);
            Horizontal = room.Side is UndergroundComplex.RingSide.Near or UndergroundComplex.RingSide.Far;
            ULo = Horizontal ? room.X0 : room.Y0;
            UHi = Horizontal ? room.X1 : room.Y1;
            Depth = Horizontal ? room.Y1 - room.Y0 : room.X1 - room.X0;
        }

        /// <summary>Does the street face run along X? True on the two bands, false on the two ends.</summary>
        internal bool Horizontal { get; }

        /// <summary>The frontage, in world coordinates on its own axis.</summary>
        internal double ULo { get; }

        /// <summary>The same, far end.</summary>
        internal double UHi { get; }

        /// <summary>How far it is from the street wall to the park wall.</summary>
        internal double Depth { get; }

        /// <summary>One point of the room's own grid, in the surface's coordinates.</summary>
        internal (double X, double Y) At(double u, double v) => _side switch
        {
            UndergroundComplex.RingSide.Near => (u, _y1 - v),
            UndergroundComplex.RingSide.Far => (u, _y0 + v),
            UndergroundComplex.RingSide.West => (_x0 + v, u),
            _ => (_x1 - v, u),
        };

        /// <summary>One box of the room's own grid, as a world rectangle with its corners in order.</summary>
        internal (double X0, double Y0, double X1, double Y1) Box(
            double uA, double vA, double uB, double vB)
        {
            (double ax, double ay) = At(uA, vA);
            (double bx, double by) = At(uB, vB);
            return (Math.Min(ax, bx), Math.Min(ay, by), Math.Max(ax, bx), Math.Max(ay, by));
        }

        /// <summary>#868 · Which way the room's FRONTAGE runs, as a unit vector — the +u axis of the grid
        /// everything in this file is laid in, in the surface's own coordinates.
        ///
        /// <para>Published for the same reason <see cref="TowardTheGlass"/> is: a layout that wants to face
        /// somebody ACROSS the room rather than at the window would otherwise write its own pair of numbers
        /// per side, and four hand-written pairs is four chances to get a sign wrong on one of the ring's
        /// four bands. The back-of-house set (<see cref="Plain"/>) is laid entirely along this axis, because
        /// that band is one chamber module deep and has no other axis to be laid along.</para></summary>
        internal (double X, double Y) AlongTheFrontage => Horizontal ? (1.0, 0.0) : (0.0, 1.0);

        /// <summary>Which way the glass is, as a unit vector — what a chair at a desk is looking at.</summary>
        internal (double X, double Y) TowardTheGlass => _side switch
        {
            UndergroundComplex.RingSide.Near => (0.0, -1.0),
            UndergroundComplex.RingSide.Far => (0.0, 1.0),
            UndergroundComplex.RingSide.West => (1.0, 0.0),
            _ => (-1.0, 0.0),
        };
    }
}
