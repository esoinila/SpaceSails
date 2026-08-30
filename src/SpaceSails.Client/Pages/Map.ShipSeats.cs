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
/// <para><b>And there is no counter and no excursion.</b> No pour can be bought aboard, so the register is
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

        // ── THE DESK IN CABIN 1 ──────────────────────────────────────────────────────────────────────
        //
        // Owner: "Why no table in cabin either?" It is the one seat aboard behind a door, which is what
        // makes it the ship's CABINET rung — SeatedIn reads Quiet, and the case may be spread there
        // unconditionally. Cabinet stays 0: a cabinet NUMBER is a leaf in a hall the counter can see, and a
        // berth aboard your own ship is nobody's business to dog but yours.
        if (spot.Kind == DeckPlan.ConsoleKind.ShipDesk)
        {
            var desk = new DeckReachability.Point(spot.X, spot.Y);
            return BesideThisTop(desk, walls) is not { } deskChair
                ? null
                : new BarTopUnderfoot(
                    0, "ship:cabin:desk", ShipWatch, deskChair.X, deskChair.Y, ShipLayout.CabinDeskSeats,
                    SittingAlone.ShipCabinSetting, SittingAlone.OwnDeskPlate, Quiet: true, Aboard: true);
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
