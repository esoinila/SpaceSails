using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1016 · <b>THE CAPTAIN CAN SIT DOWN ON HIS OWN SHIP.</b>
///
/// <para><b>Owner, on 7 Deck, with the room drawn in front of him:</b> <i>"Why no table here to sit at?"</i>,
/// then <i>"Why no table in cabin either?"</i>, and then the ruling that names this lane —
/// <i>"I expect to have a bar table like this in this ships galley also.... feature complete."</i></para>
///
/// <para>He is right, and the gap has exactly the shape #973 L0 found one room over. Her cantina has drawn
/// three tops since the deck plan was written and they were <b>dressing</b>: furniture with no console over
/// it, so [E] at one answered nothing at all. Her cabins had a BUNK and a backdrop. Meanwhile the docked
/// station bar three hundred thousand kilometres away had grown a chair you could pull out — on somebody
/// else's station, in somebody else's room, while the boat the player actually owns had nowhere to sit.</para>
///
/// <h3>What this file is, and what it deliberately is not</h3>
///
/// <para><b>It is two ANSWERS and not a ninth kind of sitting.</b> Every seat in this game is opened through
/// <c>Seating.TakeThisSeat</c>; the eighth site (<c>Seating.BarTop.cs</c>) already knows how to seat a
/// captain off a page's answer rather than off a <c>SurfaceExcursion</c>, which is exactly the shape a ship
/// needs — a boat has no landing either. So this file adds nothing to <c>ISeatHost</c> (the ratchet says the
/// list may only shrink, and this lane had no argument for growing it): it widens the ONE member the eighth
/// seat already asks, so one question answers three rooms.</para>
///
/// <para><b>Not one coordinate is measured here.</b> Which tops she has is <c>DeckPlan.Ship.Tables</c> — the
/// same list the pen draws the room from. Where the desk is bolted is <see cref="ShipLayout.CabinDeskStation"/>,
/// derived from CABIN 1's own bounds in Core. Where a body SITS at either is
/// <see cref="HavenInterior.BesideATop"/> sounded against the deck's own collision field, the same sounding
/// a walker crossing a station bar uses. §13.15: two numbers for one fixture is this ship's named console
/// bug, and she has had four of them.</para>
///
/// <h3>What is different about a boat, and it is exactly two things</h3>
///
/// <para><b>Nobody is ever going to come over.</b> Ashore, sitting down alone is a choice to be FINDABLE
/// (#757) and the room may send somebody across the floor. Aboard, the room is a crew of three droids on a
/// fixed patrol: a stranger crossing the captain's own cantina to offer him work would be the scene and the
/// ship disagreeing, which is the Office rung's own reasoning (#817) with a hull round it. So the wait beat
/// aboard is always the silence, and the silence is the ship's own words
/// (<see cref="SittingAlone.NobodyCameAboard"/>).</para>
///
/// <h3>#1040 · …and then she grew a counter</h3>
///
/// <para><b>Owner, on the same deck:</b> <i>"Our on ship bar can be upgraded to match the other bars... the
/// UI represents code long time ago."</i> The room had three rings, a galley console standing in the middle
/// of them and no counter at all — in a room whose own backdrop is a photograph of a counter with a row of
/// stools down it. #1040 built the counter (<see cref="ShipLayout.CantinaCounter"/>, a real wall you belly
/// up to, the haven bars' own idiom since #247) and bolted a row of stools along it, and it added a desk to
/// CABIN 2 beside CABIN 1's.</para>
///
/// <para><b>A stool changes exactly one thing, and it is the RUNG.</b> The sitting is the same sitting
/// through the same one method; <c>SeatedIn</c> reads the flag and answers <c>BarStool</c>, which is where
/// the gumshoe rule lives — so the case does not come out at the captain's own bar either, and the refusal
/// is the shipped sentence said out loud. That is the intended outcome and not a gap: this lane put a seat
/// on a rung that already knew what to say, and added no predicate of its own.</para>
///
/// <para><b>And there is nobody behind the counter, and no excursion.</b> No pour can be bought aboard — she
/// has a bar and no barkeep, which is what #1022's tender is for and is a different lane — so the register is
/// decided by the watch alone; and the short rest's ledger lives on an excursion this deck does not have, so
/// the wait beat says its line and files no pips. That gap is named where it is taken, in
/// <c>Seating.Table.cs</c>'s wait beat, rather than papered over with a second ledger.</para>
/// </summary>
public partial class Map
{
    /// <summary>
    /// #1016 · WHICH WATCH IT IS ABOARD — the one clock the ship's own seats ask, and they ask it for one
    /// question: whether a sit at this hour reads relaxed (<see cref="SittingAlone.SitReadsAsRelaxed"/>).
    ///
    /// <para>Clamped on, it is the FROZEN DOCKING WATCH — <see cref="BarWatch"/>, the very number the bar
    /// upstairs was drawn and seated on. A captain who walks down the tube from a bar that is on the small
    /// watch into his own cantina must not find a different hour there; a room drawn at one instant and
    /// walked at another is two rooms (#709), and while she is clamped on there is only one room.</para>
    ///
    /// <para>Under way there is nothing to freeze, so it is the sim clock's own shift, taken the way every
    /// other watch in this game is taken (<see cref="PatronRota.WatchIndex"/>). Nothing else about the boat
    /// changes with it — she has no rota and no fill — which is why this is a two-line answer rather than a
    /// visit with a metabolism.</para>
    /// </summary>
    private long ShipWatch => _dockedHavenId is null ? PatronRota.WatchIndex(SimTime) : BarWatch;

    /// <summary>
    /// #1016 · <b>WHICH OF THE SHIP'S OWN SEATS THAT [E] LANDED ON, AND WHERE A BODY SITS AT IT</b> — or
    /// null when the press was not one of hers.
    ///
    /// <para>The shape is the docked bar's, one hull in: match the console the press landed on back against
    /// the room's OWN published furniture rather than against anything measured here, sound the chair against
    /// the stone, and hand back the ordinal. What the ship adds is a SECOND room — a berth with a door — and
    /// the two differ in the four facts the answer carries: the plate, the setting, whether it is quiet, and
    /// how many the fixture seats.</para>
    ///
    /// <para><b>Null means this press is not one of hers</b> — not on her deck, not standing at one of her
    /// tops, or the stone allows no body a place at it — and the caller does what every seat verb does with
    /// a press that is not its own, which is nothing at all.</para>
    /// </summary>
    /// <param name="spot">The console the press landed on, already found by
    /// <see cref="TheBarTopUnderfoot"/>. Passed in rather than looked up a second time: two lookups is two
    /// answers to "what am I standing at", and this repository has a named bug class for that.</param>
    private BarTopUnderfoot? TheShipsOwnSeatUnderfoot(DeckPlan.ConsoleSpot spot)
    {
        // HER DECK, AND NOT A LOOK-ALIKE. On a moon the deck under foot is the surface's or the Hive's, and
        // on a boarded hulk it is the wreck's — neither carries her consoles, so the kind check below would
        // already refuse. This says it out loud all the same: "can only" is exactly the reasoning that put a
        // moon constant in charge of a ship four times in one weekend.
        if (!_deckMode || _surface is not null || OnWreck)
        {
            return null;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;

        // ── THE STOOL ROW AT HER COUNTER ─────────────────────────────────────────────────────────────
        //
        // Owner, on 7 Deck: "Our on ship bar can be upgraded to match the other bars... the UI represents
        // code long time ago." The counter it needed is Core's (ShipLayout.CantinaCounter) and so is the row
        // bolted along it; what is decided here is WHICH STOOL, and the answer is the one the captain is
        // standing at. The console is a RUN down the whole counter (#791's E-bus), so [E] anywhere along it
        // answers — and answering with a fixed stool would sit a man at the far end of the bar from where he
        // walked up, which is the drawn room and the walked room disagreeing.
        //
        // A stool is NOT sounded against the stone the way a top's chair is: a chair is a place BESIDE a
        // fixture and has to be found, and a stool IS the fixture. The square is the row's own, and that it
        // is a square a body fits on is a law with a guard on it (TheCounterAboardHasStoolsTests) rather
        // than an arithmetic done twice.
        if (spot.Kind == DeckPlan.ConsoleKind.ShipStool)
        {
            IReadOnlyList<DeckReachability.Point> row = ShipLayout.CantinaStools;
            int nearest = -1;
            double best = double.MaxValue;
            for (int i = 0; i < row.Count; i++)
            {
                double dx = row[i].X - _avatarX, dy = row[i].Y - _avatarY;
                double d2 = (dx * dx) + (dy * dy);
                if (d2 < best)
                {
                    best = d2;
                    nearest = i;
                }
            }

            if (nearest < 0)
            {
                return null;
            }

            return new BarTopUnderfoot(
                nearest, $"ship:cantina:stool:{nearest}", ShipWatch, row[nearest].X, row[nearest].Y,
                ShipLayout.CantinaStoolSeats, SittingAlone.ShipCounterSetting, SittingAlone.OwnStoolPlate,
                Quiet: false, Aboard: true, Stool: true);
        }

        // ── THE DESK IN A BERTH ──────────────────────────────────────────────────────────────────────
        //
        // Owner: "Why no table in cabin either?", and on #1040: "CABIN 2 could take a desk like CABIN 1's."
        // It is the one KIND of seat aboard behind a door, which is what makes it the ship's CABINET rung —
        // SeatedIn reads Quiet, and the case may be spread there unconditionally. Cabinet stays 0: a cabinet
        // NUMBER is a leaf in a hall the counter can see, and a berth aboard your own ship is nobody's
        // business to dog but yours.
        //
        // WHICH BERTH is matched back against Core's own list rather than being assumed, because there are
        // two of them now: a key that named the wrong cabin would file two sittings in one drawer, and a key
        // that named neither would be the sitting having no identity at all.
        if (spot.Kind == DeckPlan.ConsoleKind.ShipDesk)
        {
            for (int i = 0; i < ShipLayout.DeskCabins.Length; i++)
            {
                string cabin = ShipLayout.DeskCabins[i];
                DeckReachability.Point desk = ShipLayout.CabinDeskStationIn(cabin);
                if (Math.Abs(desk.X - spot.X) >= 0.5 || Math.Abs(desk.Y - spot.Y) >= 0.5)
                {
                    continue;
                }

                return BesideThisTop(desk, walls) is not { } deskChair
                    ? null
                    : new BarTopUnderfoot(
                        i, ShipLayout.CabinDeskKey(cabin), ShipWatch, deskChair.X, deskChair.Y,
                        ShipLayout.CabinDeskSeats, SittingAlone.ShipCabinSetting, SittingAlone.OwnDeskPlate,
                        Quiet: true, Aboard: true);
            }

            return null;
        }

        // ── THE TOPS IN HER CANTINA ──────────────────────────────────────────────────────────────────
        //
        // Off DeckPlan.Ship.Tables — the canonical list the pen draws her cantina from, and the same list
        // BuildShip publishes the consoles off. Deliberately NOT `_deckPlan.Tables`: clamped on, that list
        // is the ship's tops AND the station bar's, and an ordinal taken out of it would renumber the boat's
        // own furniture the moment she docked. The key is what every fact about this sitting hangs on, and a
        // key that moved with the berth would file two sittings in one drawer.
        for (int i = 0; i < DeckPlan.Ship.Tables.Length; i++)
        {
            DeckPlan.TableTop top = DeckPlan.Ship.Tables[i];
            if (Math.Abs(top.X - spot.X) >= 0.5 || Math.Abs(top.Y - spot.Y) >= 0.5)
            {
                continue;
            }

            // No place at it, so there is no seat here — an absence, answered as one, rather than the
            // captain being set down inside the window wall.
            if (BesideThisTop(new DeckReachability.Point(top.X, top.Y), walls) is not { } chair)
            {
                return null;
            }

            return new BarTopUnderfoot(
                i, $"ship:cantina:{i}", ShipWatch, chair.X, chair.Y, ShipLayout.CantinaTopSeats,
                SittingAlone.ShipCantinaSetting, SittingAlone.OwnTablePlate, Quiet: false, Aboard: true);
        }

        return null;
    }
}
