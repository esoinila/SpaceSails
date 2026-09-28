using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #707 · THE AMENITIES — SOMEBODY WORKED SHIFTS DOWN HERE ──────────────────────────────────────────
    //
    // Owner, the morning after walking a clinic: "all the secret labs dont have any cantina / bar nor any
    // toilets. We should add those like to the most top most pressurized floor. The toilets should have like
    // bathroom level equipments and the high level important rooms would have their built in bathrooms and
    // be pressurized."
    //
    // It is the cheapest storytelling left in this building and the most damning. Everything down here says
    // BUDGET — a lined shaft, poured walls, a lift on somebody's account decades after the last invoice —
    // and none of it says PEOPLE. A canteen and a wall of cubicles say people, in the only register this
    // ground is allowed to use: what somebody was made to pay for.
    //
    // THE TWO TIERS, which is the owner's ruling of 2026-08-05 and is a design and not a decoration:
    //
    //   1. THE UPPER CANTEEN, on the topmost floor that holds pressure. "publicly accessible and just
    //      happens to be in the secret base" — vendors drink here, normal credits work, and security is
    //      loose BY DESIGN. Classy, dangerous, and tight-lipped: there are strangers in the room and
    //      everybody knows it.
    //   2. THE STAFF CANTEEN, on the deepest floor the building ADMITS to that still holds pressure.
    //      Machines, no bottles, and a room where every face is known — so the talk is careless in exactly
    //      the room a stranger cannot stand in.
    //
    // And the upper one is the answer to a question the mechanics have been shipping since #590 without one.
    // Owner, closing the loop: "setting access to off the books secret lab to partners all trying to keep
    // things off records would be bureaucratic nightmare of office interorganization bureaucracy so the
    // underground bar just is there with access from surface. It kind of provides cover-story as well."
    // Band 0 has never wanted a card. Now it has a reason: credentialing every deniable partner across
    // organisations that all deny existing was never going to happen, so the first floor is simply OPEN, and
    // the bar is why anybody believes the shed on the surface is what it pretends to be. Access control
    // starts where the drinks stop. Nothing is built for that here — the plate carries it in four words and
    // the room prose carries the rest, and neither of them ever explains a thing.
    //
    // WHY THE AMENITIES ONLY EXIST ON FLOORS THAT BREATHE, which is the one rule the whole section turns on:
    // a canteen, a cubicle and an en-suite are all PLUMBING, and plumbing is for people out of their suits.
    // The owner already ruled the general form of it — "any room that would house like office work would be
    // pressurized by that constraint ... any kind of fine motor skill stuff" — and eating, washing and
    // signing things are the same constraint. So there is NO SECOND PRESSURE MAP here and there never will
    // be: HoldsPressure is asked, and where it says no, nothing is plumbed. A private washroom breathing on
    // a floor the plate by the lift is calling NO ATMOSPHERE would be two instruments disagreeing about air,
    // which is the one thing §13.13 says is worse than saying nothing.

    /// <summary>#707 · What an amenity room is FOR. Three, and the difference between the first and the
    /// third is the whole of the owner's inverted-economics ruling rather than a change of furniture.</summary>
    public enum Comfort
    {
        /// <summary>The bar on the top floor that holds pressure — the one room in the building outsiders
        /// are in, and the reason nobody at band 0 is ever asked for a card.</summary>
        UpperCanteen,
        /// <summary>Cubicles, a basin run and a mirror. Bathroom-grade, per the owner.</summary>
        Washroom,
        /// <summary>Machines and close tables, on the deepest floor the directory admits to that still
        /// breathes. Staff only, and the paperwork says it is somewhere else entirely.</summary>
        StaffCanteen,
    }

    /// <summary>One amenity room, taken out of the rooms the floor had already built — same discipline as
    /// <see cref="Refuge"/>, and for the same reason: a room is already audited walkable from the lift,
    /// already has a door, and already sits down a rib.</summary>
    /// <param name="Use">Which of the three it is.</param>
    /// <param name="X">Centre, in the surface's own coordinates.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="Plate">What is stencilled beside the door.</param>
    /// <param name="Fixture">What the thing in the middle of the room is called, at console size.</param>
    /// <param name="Tables">Round tops on the floor, drawn in the game's existing table idiom. Empty in a
    /// washroom, which is the one amenity nobody sits down in.</param>
    /// <param name="Hall">#751 · The hall this amenity IS, when it is one — a room that left the standard
    /// grammar. Null for the ordinary three-top canteen and for every washroom.</param>
    public readonly record struct Amenity(
        Comfort Use, double X, double Y, string Plate, string Fixture,
        IReadOnlyList<(double X, double Y)> Tables,
        Hall? Hall = null)
    {
        /// <summary>#725 · Is the captain standing in this room? <see cref="RefugeHolds"/>, because an
        /// amenity is one of the floor's own rooms taken over — the same poured box, with the same square
        /// corners — and a second containment box written here would be a room whose walls the sim and the
        /// picture disagreed about. One law, asked in one place, exactly as the refuge does it.
        ///
        /// <para>#751 · …unless it is a HALL, in which case the box is the hall's own — carved, published,
        /// and the very same rectangle the walls were laid on. A hall is thirty times the floor area of the
        /// module, so a refuge-sized containment box would have said "you are not in the canteen" from
        /// almost everywhere inside the canteen.</para></summary>
        public bool Contains(double x, double y) => Hall is { } hall
            ? hall.Contains(x, y)
            : RefugeHolds(X, Y, x, y);
    }
}
