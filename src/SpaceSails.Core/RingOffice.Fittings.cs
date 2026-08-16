using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: what a piece of furniture IS — the vocabulary the whole file answers in.
public static partial class RingOffice
{
    // ── WHAT A PIECE OF FURNITURE IS ──────────────────────────────────────────────────────────────────

    /// <summary>What a fitting IS — so a renderer can stencil it without measuring it, and a guard can say
    /// "every view suite has a desk" without pattern-matching on a string.</summary>
    public enum Fitting
    {
        /// <summary>A run of worktop several people sit at.</summary>
        DeskBank,

        /// <summary>The screen between two workstations at one bank. The word the owner used was
        /// "cubicles", and this is the du of it that makes one.</summary>
        Partition,

        /// <summary>The negotiation room's one long table.</summary>
        Table,

        /// <summary>A reception counter, or the kitchenette's own worktop.</summary>
        Counter,

        /// <summary>A run of shelving against a pier — the reading room's, and every plain room's.</summary>
        Shelving,

        /// <summary>A bench. What a back-of-house room has instead of a desk.</summary>
        Bench,

        /// <summary>The kitchenette block in a big suite's service strip.</summary>
        Kitchenette,

        /// <summary>A WC cubicle: a walled box with its own published door (#821 will lock it from the
        /// inside).</summary>
        Cubicle,

        /// <summary>A privacy booth — one seat, open-fronted, phone-box shaped.</summary>
        Booth,

        // ── #818 · THE REST OF THE BUILDING'S FITTINGS ────────────────────────────────────────────────
        //
        // Owner, generalising #817 past the ring: "Same for labs etc spaces… they have chairs and desks and
        // equipment … never ever empty floor", and then the kit itself, from somebody who has run these
        // rooms: "chairs / tables / vacuum chambers (cough !!), chemical test ventilation boxes [fume
        // hoods], etc where do I put my test tube? Etc furnaces".
        //
        // They live HERE and not in a second enum beside ChamberFitting for the reason a Fixture is one
        // record: a renderer that draws a piece of furniture, a guard that counts one, and a plate that
        // names one should read ONE vocabulary. This list stopped being the ring's the moment the law went
        // building-wide; the ring is only where it was first written down.

        /// <summary>A laboratory bench, with the glassware racked along it. The answer to <i>where do I put
        /// my test tube?</i></summary>
        LabBench,

        /// <summary>A chemical test ventilation box, against a wall. The owner's own phrase for it.</summary>
        FumeHood,

        /// <summary>A vacuum vessel. Asked for by name, with a cough.</summary>
        VacuumChamber,

        /// <summary>A furnace.</summary>
        Furnace,

        /// <summary>A run of bays in a long store. Shelving's heavy cousin, and its own kind because a store
        /// that reported <see cref="Shelving"/> would be a warehouse pretending to be a reading room.</summary>
        Racking,

        /// <summary>A machine bolted to a plant floor.</summary>
        Machinery,

        /// <summary>A bank of filing in an administration chamber.</summary>
        FilingCabinet,

        /// <summary>#828 · THE SECURE DISPOSAL — the shredder-class machine at the far end of a premium
        /// suite's service strip. Owner: <i>"One thing the good offices would also have is a safe paper
        /// disposal trashes… that visually destroy the notes as we watch… a more secure disposal than
        /// restaurant trash."</i>
        ///
        /// <para>A FITTING here and a <see cref="RipAndBin.Tier.SecureDisposal"/> to the verb: one box on
        /// the plan, plated once, and the third rung of a ladder that already existed. It is the tier the
        /// service strip is — the kitchenette, the staff WCs, the privacy booths — which is why it stands
        /// here and not by every lift.</para></summary>
        SecureDisposal,
    }

    /// <summary>
    /// #821 · ONE CUBICLE, PUBLISHED AS THE THING THE LOCK LANDS ON.
    ///
    /// <para>A <see cref="Fitting.Cubicle"/> fixture is the BOX and the floor's doorway list holds the LEAF,
    /// and until this record existed nothing said which leaf belonged to which box. That pairing is the
    /// whole of #821 — a lock is a fact about one door of one cell — so it is published by the placer that
    /// laid both rather than recovered afterwards by a client measuring midpoints, which is §13.15 and the
    /// shape of bug this ground has twice paid for.</para>
    /// </summary>
    /// <param name="Index">Its ordinal in the room, so a watch-scoped key can name it.</param>
    /// <param name="X0">Left edge of the box, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Door">Its own leaf — the very doorway the floor published, not a copy of one.</param>
    /// <param name="SeatX">Where a body goes when it sits down in here. The middle of the box: clear of the
    /// pan against the back wall and clear of the leaf, so the snap (#820's law, kept) can put the captain
    /// ON this coordinate and standing up leaves them exactly there.</param>
    /// <param name="SeatY">The same.</param>
    /// <param name="StepX">Where somebody stands OUTSIDE it — a door's clearance off the leaf, on the room's
    /// side. Where a guard knocks from, and published rather than derived for that reason: a man who walked
    /// to a coordinate this file did not choose would be standing somewhere nobody laid out.</param>
    /// <param name="StepY">The same.</param>
    /// <param name="Plate">What is stencilled on it while it is free.</param>
    public readonly record struct Stall(
        int Index, double X0, double Y0, double X1, double Y1,
        SurfaceLayout.Doorway Door, double SeatX, double SeatY, double StepX, double StepY, string Plate)
    {
        /// <summary>The middle of it.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>Where the leaf's own console hangs — the middle of the doorway, which is the square a
        /// captain is standing on when they reach behind them and turn the catch.</summary>
        public double DoorX => (Door.X1 + Door.X2) / 2.0;

        /// <summary>The same.</summary>
        public double DoorY => (Door.Y1 + Door.Y2) / 2.0;

        /// <summary>Is the captain in it? The box the three walls were laid on, and nothing else — the same
        /// arithmetic <see cref="UndergroundComplex.RingRoom.Contains"/> uses one scale up.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>
        /// #821 · Is a body of <paramref name="bodyRadius"/> standing in here AND CLEAR OF THE LEAF — which
        /// is what "inside" has to mean for the catch, as opposed to for the hide.
        ///
        /// <para>A shut cubicle is a WALL SEGMENT laid on the leaf, so a captain who turned the catch while
        /// standing in the opening would be standing inside the thing they just made. Nothing traps the dot
        /// on this ground (the ring chair's own law, and the oldest bug this project has), so the catch asks
        /// for a body's clearance and the refusal that follows is the ordinary one: step in first.</para>
        /// </summary>
        public bool ClearOfTheLeaf(double x, double y, double bodyRadius)
        {
            if (!Contains(x, y))
            {
                return false;
            }
            double dx = x - DoorX, dy = y - DoorY;
            return (dx * dx) + (dy * dy) >= bodyRadius * bodyRadius;
        }

        /// <summary>#821 · Where the captain is put down when they stand up again — the seat, because a
        /// cubicle's pan is not a solid on the plan and the seat is the square you were already standing on
        /// to press [E]. The ring chair's own law (<see cref="Chair.StandAt"/>), said about a smaller
        /// room.</summary>
        public (double X, double Y) StandAt => (SeatX, SeatY);
    }

    /// <summary>
    /// #821 · ONE WASHBASIN. Owner: <i>"also a function to wash hands with some film noir comment at the
    /// end."</i>
    ///
    /// <para>The RUN is one <see cref="Fitting.Counter"/> — a basin run is a worktop with holes in it, and
    /// calling it what it is keeps every guard about worktops true about this room without a second kind to
    /// teach them. These are the taps along it, and they exist so the press has somewhere to be that is not
    /// the middle of a six-du counter.</para>
    /// </summary>
    /// <param name="Index">Its ordinal along the run.</param>
    /// <param name="X">Where a body stands to use it.</param>
    /// <param name="Y">The same.</param>
    public readonly record struct Basin(int Index, double X, double Y);

    /// <summary>
    /// #827 · WHICH SIDES OF A PIECE OF FURNITURE CARRY SEATS.
    ///
    /// <para>Owner's reading of the whole family, arriving while this issue was in flight: a canteen counter
    /// is <i>the biggest table, with seats on ONE side only</i> and the round tops are the same object with
    /// seats all the way round. So "how many sides" is a fact a table HAS rather than something each
    /// renderer works out from where the chairs happened to land — and an office desk is one-sided in
    /// exactly the sense a bar is, which is why the two can eventually be one record.</para>
    ///
    /// <para>The continuity of the row (a bar's seats have gaps in them where customers walk up to be
    /// served) is #827's own sweep and is deliberately not modelled here: nothing in a ring office has a
    /// broken seat row today, and inventing the field before the case exists would be a second opinion
    /// waiting to disagree with the first.</para>
    /// </summary>
    public enum Seating
    {
        /// <summary>Nobody sits at it. Shelving, a kitchenette, a screen.</summary>
        None,

        /// <summary>One side. A desk bank (the chairs are on the street side, looking at the glass over the
        /// worktop) and a reception counter (the staff side, looking at the door) — the bar's own shape.</summary>
        OneSide,

        /// <summary>Both long sides, facing each other across it. The negotiation table.</summary>
        BothSides,

        /// <summary>All the way round. What a canteen's round top is, and what nothing in an office
        /// is.</summary>
        AllRound,
    }

    /// <summary>One piece of furniture, as the box it occupies on the plan.</summary>
    /// <param name="Kind">What it is.</param>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Plate">What is stencilled on it, where it is the kind of thing that gets a stencil.
    /// Empty on a desk bank: a plate over every worktop in a landscape office is a room nobody can read.</param>
    /// <param name="Sides">#827 · Which sides of it carry chairs. See <see cref="Seating"/>.</param>
    public readonly record struct Fixture(
        Fitting Kind, double X0, double Y0, double X1, double Y1, string Plate,
        Seating Sides = Seating.None)
    {
        /// <summary>The middle of it — where its stencil is read from.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;
    }

    /// <summary>
    /// One chair, as a seat with a facing.
    /// </summary>
    /// <param name="Index">Its ordinal in the room, so a watch-scoped state key can name it.</param>
    /// <param name="X">Where a body goes when it sits down. #820 · sitting SNAPS the captain onto this
    /// coordinate, so it is the seat and not a hint at one — it is laid clear of the worktop it belongs to by
    /// more than a body's radius for exactly that reason.</param>
    /// <param name="Y">The same.</param>
    /// <param name="FaceX">Which way the person in it is looking, as a unit vector. Desk chairs face the
    /// GLASS: the view is what the room rents for and the furniture should agree.</param>
    /// <param name="FaceY">The same.</param>
    /// <param name="Room">The plate of the room it stands in, so sitting down can name where you are without
    /// the seat having to be looked up in anything.</param>
    public readonly record struct Chair(
        int Index, double X, double Y, double FaceX, double FaceY, string Room)
    {
        /// <summary>What is drawn over it, with the verb on it — #783's ruling, unchanged: <i>"why not use
        /// words like SIT DOWN here if it means sitting down?"</i></summary>
        public string DeckPlate => FreeChairPlate;

        /// <summary>#820 · Where the captain is put down when they stand up again. A ring chair is not a
        /// solid — the seat is a square you were already standing on to press [E] — so standing up leaves you
        /// exactly where sitting down put you, and nothing can trap the dot. Published rather than assumed,
        /// so the sweep that gives the park bench the same law has one seam to change.</summary>
        public (double X, double Y) StandAt => (X, Y);
    }

    /// <summary>Everything one room is furnished with, in one answer: what the deck draws and collides with,
    /// what a captain can sit on, and the doors the cubicles brought with them.</summary>
    /// <param name="Fixtures">The furniture, as boxes, for the stencils and for the guards.</param>
    /// <param name="Chairs">Every seat, in the room's own order.</param>
    /// <param name="Solids">The same furniture as WALL SEGMENTS — what actually goes into the floor's wall
    /// list, so it is drawn and walked into exactly the way the en-suite's pan and the park's raised beds
    /// are. One list, both jobs, and no second opinion about where a desk is.</param>
    /// <param name="Doors">The cubicles' own leaves, as published doorways. #821 · A real door and never a
    /// decorative gap: the lock-from-inside is coming and it must land on a door the building already
    /// knows about.</param>
    /// <param name="Stalls">#821 · The cubicles, each paired with its OWN leaf. See <see cref="Stall"/>: the
    /// lock is a fact about one door of one cell, and the placer that laid both is the only thing that can
    /// say which is which.</param>
    /// <param name="Basins">#821 · The taps along a public washroom's basin run.</param>
    public readonly record struct Furnishing(
        IReadOnlyList<Fixture> Fixtures,
        IReadOnlyList<Chair> Chairs,
        IReadOnlyList<SurfaceLayout.Wall> Solids,
        IReadOnlyList<SurfaceLayout.Doorway> Doors,
        IReadOnlyList<Stall>? Stalls = null,
        IReadOnlyList<Basin>? Basins = null)
    {
        /// <summary>Nothing at all — what a room too small to stand a desk in gets, and what nothing in the
        /// ring actually gets today. Kept so a degenerate box returns an empty answer rather than a
        /// half-built one.</summary>
        public static Furnishing Empty { get; } = new([], [], [], []);

        /// <summary>The cubicles, never null.</summary>
        public IReadOnlyList<Stall> Cells => Stalls ?? [];

        /// <summary>The taps, never null.</summary>
        public IReadOnlyList<Basin> Taps => Basins ?? [];
    }
}
