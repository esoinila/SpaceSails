using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #1074 · THE STOP ORDER, IN THE BUILDING ──────────────────────────────────────────────────────────
    //
    // What a STOP does to the ground, in one sentence: the shaft below the listed bottom is SEALED. Nothing
    // is filled and nothing is removed — this is not the burial — so IsFound, HasFoundBand, FloorsOf,
    // TrueDepthOf and the disclosure clock all answer for this site exactly what they answered yesterday.
    // The halls are still there. The way down is not.
    //
    // TWO THINGS DO THAT, AND THEY ARE THE SAME FACT SAID IN TWO ROOMS:
    //
    //   * THE GATE. LiftPanel stops offering the ride into the band nobody listed, which is the shaft the
    //     order closes — everything under the listed bottom hangs off it. It ends in SILENCE and not in a
    //     refusing row, and that is #592's rule rather than a new one: the building does not admit that band
    //     exists, so its panel may not name it even to say no. The card in the wallet is not consulted,
    //     because an order is not a clearance question.
    //   * THE SEAL. A short recess off the spine on the listed bottom, with one leaf at the back of it that
    //     does not open, wearing the order's plate. It is a LockedDoor and therefore it is drawn by the code
    //     that has drawn forty of them since #585 — a leaf shut, a real wall behind it, a 🔒 console reading
    //     the plate. NO KEYPAD (#602's in-code ruling): there is no reader on it, nothing to try a card
    //     against and nothing to take a hasp off, and ShootTheLock.Judge and the satchel both say so.
    //
    // THE SEAL STANDS IN THE POCKET #1063's PRESERVED DOORWAY STANDS IN, and it may, because a ground is
    // stopped or buried and never both (StopOrder.TheOfficeGetsThisOne is the whole of that split). It is
    // the one stretch of this building nothing else is ever laid against — the blind end of the spine past
    // the last rib's chambers — and the same argument that put a display piece there puts a sealed shaft
    // head there: it is the only ground in the building free to hold something.
    //
    // NOTHING IS SAID BEYOND THE PLATE. No card, no pulse of its own, no beat, no nerve shock, no marker. A
    // captain rides to the bottom, finds the panel has nothing under it, walks the corridor to its end and
    // reads one sentence stamped by an office. That is the entire staging.

    /// <summary>#1074 · Which floor of this site the order is posted on, or null where there is none.
    ///
    /// <para><b>The listed bottom</b> — the deepest floor the building admits to, which is the floor a
    /// captain is standing on when the panel goes quiet and, in the building's own fiction, the floor the
    /// other shaft is on (<c>"This car does not go lower. The shaft that does is on this floor"</c>). Sealing
    /// it anywhere else would be sealing a shaft the world has never said was there.</para></summary>
    public static int? StopSealFloorOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return StopOrder.On(bodyId) ? DepthOf(bodyId) : null;
    }

    /// <summary>#1074 · Is the order posted on this floor? False everywhere in a world where nobody has been
    /// past a seam long enough ago, which is almost every world.</summary>
    public static bool HasStopSealOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return StopSealFloorOf(bodyId) == level;
    }

    /// <summary>#1074 · <b>DOES THE ORDER CLOSE THIS GATE?</b> Asked by <see cref="LiftPanel(string, int,
    /// ShaftKind, System.Collections.Generic.IReadOnlyCollection{string}, IReadOnlyList{Satchel.Item}, int)"/>
    /// of the band it was about to offer a ride into.
    ///
    /// <para>It is the gate into the band nobody listed and no other gate in the building. The listed bands
    /// above it are the working the site admits to and a stop order about a deep working has no business
    /// closing a lift between two office floors; everything BELOW that band hangs off this one shaft, so
    /// closing it is the whole of the closure and there is nothing further down to close separately.</para>
    ///
    /// <para>The captain's wallet is deliberately not a parameter. A clearance answers the question "does
    /// this outfit know you"; an order answers nothing, because it is not addressed to him.</para></summary>
    public static bool StopSealsTheGateTo(string bodyId, int band)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return StopOrder.On(bodyId) && HasUnlistedBand(bodyId) && band == UnlistedBandOf(bodyId);
    }

    /// <summary>#1074 · WHERE THE SEAL STANDS. <b>The blind end of the spine</b> — the same ground #1063's
    /// preserved doorway takes, asked of the same function, because a site is stopped or buried and never
    /// both. A second copy of that arithmetic would be the mirrored constant this file keeps a table of, and
    /// a second pocket would be a building with two blind ends in it.</summary>
    public static (double X, double Y)? StopSealRecessAt(in SurfaceLayout.Field field) =>
        SpecimenRecessAt(field);

    /// <summary>#1074 · The sealed leaf on this floor, or null where this floor carries none — which is every
    /// floor of every site nobody has stopped. It is a <see cref="LockedDoor"/> and nothing more exotic: the
    /// building already knows how to draw a leaf that will not open, with the plate that is on it.</summary>
    public static LockedDoor? StopSealOn(string bodyId, int level, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (!HasStopSealOn(bodyId, level) || StopSealRecessAt(field) is not { } at)
        {
            return null;
        }
        double far = at.Y + SpecimenRecessDu;
        return new LockedDoor(at.X - ShaftHalf, far, at.X + ShaftHalf, far, StopOrder.Plate);
    }

    /// <summary>#1074 · Cut the recess into the floor being built and hang the sealed leaf at the back of it:
    /// the two sides in ordinary poured hull, the mouth handed to the spine's own sweep at the pocket's own
    /// width (#819's law), the ground claimed so no later placer lays a room across it, and the far end
    /// appended to the floor's locked doors.
    ///
    /// <para>The leaf goes into <c>locked</c> rather than into <c>walls</c> because that list is what carries
    /// a SIGN: a segment in <c>walls</c> is stone and a segment in <c>locked</c> is stone with a plate on it,
    /// and the plate is the entire point of this one. It is APPENDED and never inserted, exactly as #1068's
    /// declined leaf is, because locked doors are addressed by index upstream (a sentry's shot-open lock, a
    /// walker's chosen leaf) and a seal that renumbered them would silently move which door those are
    /// about.</para></summary>
    private static void CarveStopSeal(
        string bodyId, int level, in SurfaceLayout.Field field,
        List<SurfaceLayout.Wall> walls,
        List<(double Y, double Lo, double Hi)> alcoveMouths,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<LockedDoor> locked)
    {
        if (StopSealOn(bodyId, level, field) is not { } seal
            || StopSealRecessAt(field) is not { } at)
        {
            return;
        }

        double far = at.Y + SpecimenRecessDu;
        walls.Add(new(at.X - ShaftHalf, at.Y, at.X - ShaftHalf, far, true));
        walls.Add(new(at.X + ShaftHalf, at.Y, at.X + ShaftHalf, far, true));
        alcoveMouths.Add((at.Y, at.X - ShaftHalf, at.X + ShaftHalf));
        claimed.Add((at.X - ShaftHalf - 1.5, at.Y, at.X + ShaftHalf + 1.5, far + 1.5));
        locked.Add(seal);
    }

    /// <summary>#1074 · <b>THE VALVE-BOOK</b> — the plant's own maintenance book, open at the three entries
    /// that bracket the closure, behind the paper glyph every operational-paper line in this building already
    /// wears.
    ///
    /// <para><b>Composed exactly as #1063's <see cref="MaintenanceLedgerLine"/> is</b> — one pulse string,
    /// three closed sentences in one rigid clerical form, no punctuation invented here — because the haul
    /// line is a single pulse string the client concatenates the pickup sentence straight onto, and two
    /// spellings of "three entries on one paper" would be two answers to one question.</para>
    ///
    /// <para><b>THE CLUE IS THE NUMBERING AND THE PREPOSITION.</b> Read the citations down the page and they
    /// go <b>2231</b>, then <i>nothing</i>, then <b>2233</b>: the book's own arithmetic says an instruction
    /// 2232 was issued and the one line that would cite it is the one line that cites none. And where every
    /// other line in the book says <i>per instruction</i>, that line says <i>per order</i> — a different kind
    /// of paper, from a different kind of office, and nobody writes down that they noticed.</para></summary>
    public const string PlantValveBookLine =
        "📋 " + StopOrder.ValveBookBefore + " " + StopOrder.ValveBookLine + " " + StopOrder.ValveBookAfter;

    /// <summary>#1074 · WHERE THE VALVE-BOOK IS, designated for exactly the reason its four siblings are
    /// (<see cref="KeyRoomFor"/>, <see cref="RelicRoomFor"/>, <see cref="StandingOrderRoomFor"/>,
    /// <see cref="MaintenanceLedgerRoomFor"/>): a seeded one-in-nine paper is a paper that is silently absent
    /// forever on some worlds, with nothing on screen ever saying so.
    ///
    /// <para><b>The listed bottom — the floor the order is posted on</b>, because that is what makes it
    /// evidence rather than a document: the book that goes terse for one line is kept on the floor whose
    /// working was closed, a corridor's length from the seal. Room <b>1</b> and not room 0, which on this
    /// floor is already <see cref="KeyRoomFor"/>'s; the two are the only designations this floor carries and
    /// they cannot collide.</para>
    ///
    /// <para><b>It is a paper on a floor that may not breathe, and that is deliberate</b> — the same
    /// judgement #411's standing order already ships at B12 of a head office where only every fourth floor
    /// holds air. #608's rule is about a ROLL, and these are authored placements that exist precisely because
    /// a roll can be silently absent forever. A plant book is also the least strained possible case for it: a
    /// riser book lives at the risers, in a suit or out of one.</para></summary>
    public static (int Level, int RoomIndex)? ValveBookRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return StopOrder.On(bodyId) ? (DepthOf(bodyId), 1) : null;
    }
}
