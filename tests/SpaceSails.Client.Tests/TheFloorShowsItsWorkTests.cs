using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #818/#853 · THE CLIENT HALF OF "NEVER AN EMPTY FLOOR" — the furniture Core stood in every room is on the
/// deck a captain actually walks, and the posters can be read.
///
/// <para>Core's own guard (<c>NeverAnEmptyFloorTests</c>) proves the LAW: every carved room publishes
/// something. It cannot prove the half only the client has a collision field for, and that half is where
/// this project's most expensive bugs have always lived: <b>a room that is correct on the plan and wrong to
/// walk.</b> A bench laid across the square the A* audit stands on does not report as furniture — it reports
/// as a room nobody can be in, on every floor in the game.</para>
///
/// <para>So what is measured here is the drawn, collided deck: the room's own centre is still floor, every
/// published seat is somewhere a body can be, every stencilled fixture has a stencil on the plan, and every
/// poster is a press with a card behind it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFloorShowsItsWorkTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>Every floor of every site in the sweep, with the deck the renderer actually draws — the same
    /// call the game boots with, so the walls below are the walls a captain walks into.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.FloorPlan Floor, DeckPlan Deck)>
        EveryFloor()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                yield return (body, level, UndergroundComplex.Build(body, level, Field),
                    HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], 0));
            }
        }
    }

    private static bool Stuck(DeckPlan deck, double x, double y) =>
        SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, deck.CollisionField);

    /// <summary>
    /// #818 · THE FURNITURE IS SOLID, AND THE ROOM IS STILL A ROOM.
    ///
    /// <para>Every furnished chamber's own centre is floor a body can stand on, and every seat in it is a
    /// square a body can be put down on — which is what #820's snap does the moment [E] is pressed there.</para>
    /// </summary>
    [Fact]
    public void EveryFurnishedRoomIsStillSomewhereABodyCanStand()
    {
        var wrong = new List<string>();
        int rooms = 0, seats = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor, DeckPlan deck) in EveryFloor())
        {
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (room.Kind != UndergroundComplex.RoomKind.Chamber || !room.IsFurnished)
                {
                    continue;
                }
                rooms++;

                if (Stuck(deck, room.X, room.Y))
                {
                    wrong.Add($"  {body} B{-level} ({room.Plate}) at ({room.X:F1},{room.Y:F1}) — the room's "
                        + "own centre is blocked; something was built through it.");
                }

                foreach (RingOffice.Chair seat in room.Seats)
                {
                    seats++;
                    if (Stuck(deck, seat.X, seat.Y))
                    {
                        wrong.Add($"  {body} B{-level} ({room.Plate}) — the seat at "
                            + $"({seat.X:F1},{seat.Y:F1}) is inside something.");
                    }
                }
            }
        }

        Assert.True(rooms > 500, $"only {rooms} furnished room(s) were walked — this proved little.");
        Assert.True(seats > 100, $"only {seats} seat(s) were walked — this proved little.");
        Assert.True(
            wrong.Count == 0,
            $"{wrong.Count} furnished room(s) are wrong to walk:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }

    /// <summary>
    /// #818 · WHAT CORE STENCILLED, THE DECK DRAWS — and every seat Core published is a press.
    ///
    /// <para>The house's own bug class said about a renderer: the sim doing one thing while the drawn world
    /// reports another. A fixture Core plated and the deck did not draw is a room the captain is told
    /// nothing about; a seat Core published and the deck hung no console on is #757's complaint restated
    /// (an absence, not a refusal anybody can read).</para>
    /// </summary>
    [Fact]
    public void EveryStencilAndEverySeatReachesTheDeck()
    {
        var wrong = new List<string>();
        int plated = 0, seats = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor, DeckPlan deck) in EveryFloor())
        {
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (room.Kind != UndergroundComplex.RoomKind.Chamber)
                {
                    continue;
                }

                foreach (RingOffice.Fixture fit in room.Furniture)
                {
                    if (fit.Plate.Length == 0)
                    {
                        continue;
                    }
                    plated++;
                    if (!deck.RoomLabels.Any(l =>
                            string.Equals(l.Text, fit.Plate, StringComparison.Ordinal)
                            && Math.Abs(l.X - fit.X) < 0.5 && Math.Abs(l.Y - fit.Y) < 0.5))
                    {
                        wrong.Add($"  {body} B{-level} — \"{fit.Plate}\" is stencilled in Core and drawn "
                            + "nowhere on the deck.");
                    }
                }

                foreach (RingOffice.Chair seat in room.Seats)
                {
                    seats++;
                    if (!deck.Consoles.Any(c =>
                            c.Kind == DeckPlan.ConsoleKind.HiveOfficeChair
                            && Math.Abs(c.X - seat.X) < 0.5 && Math.Abs(c.Y - seat.Y) < 0.5))
                    {
                        wrong.Add($"  {body} B{-level} ({room.Plate}) — a seat at "
                            + $"({seat.X:F1},{seat.Y:F1}) with nothing to press.");
                    }
                }
            }
        }

        Assert.True(plated > 500, $"only {plated} stencil(s) were checked — this proved little.");
        Assert.True(seats > 100, $"only {seats} seat(s) were checked — this proved little.");
        Assert.True(
            wrong.Count == 0,
            $"{wrong.Count} thing(s) Core published never reach the deck:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }

    /// <summary>
    /// #853 · EVERY POSTER IS A PRESS WITH A CARD BEHIND IT.
    ///
    /// <para>Owner: <i>"a poster is a Landmark with an ArtUrl, readable on E for the fine print."</i> The
    /// press is the ViewObject the monolith and the ports' own PIRATE INSURANCE poster already take, and the
    /// art slot degrades exactly as theirs do — so the card carries the copy TODAY, before a picture has
    /// been shot, and this guard asserts the caption rather than the file.</para>
    /// </summary>
    [Fact]
    public void EveryPosterIsAPressWithTheFinePrintOnIt()
    {
        var wrong = new List<string>();
        int hung = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor, DeckPlan deck) in EveryFloor())
        {
            foreach (LabPosters.Poster poster in floor.TheWalls)
            {
                hung++;
                DeckPlan.ConsoleSpot? found = null;
                foreach (DeckPlan.ConsoleSpot spot in deck.Consoles)
                {
                    if (spot.Kind == DeckPlan.ConsoleKind.ViewObject
                        && Math.Abs(spot.X - poster.X) < 0.5 && Math.Abs(spot.Y - poster.Y) < 0.5)
                    {
                        found = spot;
                        break;
                    }
                }

                if (found is not { } press)
                {
                    wrong.Add($"  {body} B{-level} — poster {poster.Number} hangs on the plan with nothing "
                        + "to press.");
                    continue;
                }

                if (!string.Equals(press.Caption, poster.FinePrint, StringComparison.Ordinal))
                {
                    wrong.Add($"  {body} B{-level} — poster {poster.Number}'s card does not carry its own "
                        + "fine print.");
                }
                if (!string.Equals(press.ImageUrl, poster.ArtUrl, StringComparison.Ordinal))
                {
                    wrong.Add($"  {body} B{-level} — poster {poster.Number} wears the wrong art slot.");
                }
                if (!press.Label.Contains(poster.Title, StringComparison.Ordinal))
                {
                    wrong.Add($"  {body} B{-level} — poster {poster.Number}'s plate does not say what it is.");
                }

                // …and the captain can get to it. A picture you can see and cannot reach is worse than none
                // (#212), and a poster is hung a hand's breadth off a corridor wall for exactly that reason.
                if (Stuck(deck, poster.X, poster.Y))
                {
                    wrong.Add($"  {body} B{-level} — poster {poster.Number} hangs inside a wall.");
                }
            }
        }

        Assert.True(hung > 50, $"only {hung} poster(s) were checked — this proved little.");
        Assert.True(
            wrong.Count == 0,
            $"{wrong.Count} poster(s) are not readable:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }
}
