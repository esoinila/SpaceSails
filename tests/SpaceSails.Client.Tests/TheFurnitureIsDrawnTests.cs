using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #868 · THE CLIENT HALF OF "JUST SAY TABLE" — every piece of furniture Core stood in a ring room reaches
/// the deck as a FILLED RECTANGLE, in the ink of what it is.
///
/// <para>Owner, 2026-08-13, reading <c>❄ COLD ROOM · TO CANTEEN 1</c> off the plan: <i>"The graphics kind of
/// does not show there being a table"</i> · <i>"The bench is a line"</i> · <i>"The Shelving is clear as
/// furniture goes."</i> And then the fix in his own words: <i>"could the table just be a different color
/// rectangle in front of the chair, so arms (and papers) could rest on it?"</i>, sealed with <i>"I think
/// table should be similar just say table."</i></para>
///
/// <para>Core's own guard (<c>TheDeskIsAThingYouCanSeeTests</c>) proves that a plain room now publishes a
/// worktop and that its bench has two dimensions. It cannot prove the half only the client has a pen for,
/// and that half is the whole of the owner's complaint: a rectangle published in Core and drawn as four
/// strokes round a dark gap is a rectangle that reads as SOMEWHERE YOU COULD STAND. What is measured here is
/// the drawn deck — the same <c>HiveInterior.FloorDeck</c> call the game boots with.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFurnitureIsDrawnTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>Every floor of every site in the sweep that has a block on it, with the deck the renderer
    /// actually draws.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park, DeckPlan Deck)>
        EveryBlock()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                if (floor.Park is not { } park)
                {
                    continue;
                }
                yield return (body, level, park,
                    HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], 0));
            }
        }
    }

    /// <summary>
    /// EVERY PUBLISHED FITTING IS A FILLED RECTANGLE ON THE DECK, IN THE INK OF WHAT IT IS.
    ///
    /// <para>Matched on the BOX rather than on a midpoint, because the box is the thing the owner is looking
    /// at: a slab drawn at the right centre and the wrong size is exactly the picture that started this
    /// issue. The tone is asserted too — a bench drawn in the worktop's ink would be one idiom saying two
    /// things, which is the opposite of the ruling.</para>
    ///
    /// <para>A CUBICLE and a BOOTH are exempt and must stay exempt: they are little rooms a captain steps
    /// inside and shuts the door on, and filling one would draw the building's only hiding place as a solid
    /// block. A PARTITION is a screen between two workstations and honestly IS a line.</para>
    ///
    /// <para><b>Proven RED</b> by handing the deck nothing (<c>furniture: []</c> in
    /// <c>HiveInterior.FloorDeck</c>) — which is the state of the world this issue was opened about:</para>
    /// <code>
    /// 359 fitting(s) Core stood in a room are not drawn as furniture:
    ///   luna B1 (🚻 PUBLIC WASHROOMS · NO PASS REQUIRED) — its Counter at (-125.0,-172.4)-(-123.0,-162.0)
    ///   is published in Core and filled nowhere on the deck.
    ///   luna B1 (🚻 PUBLIC WASHROOMS · NO PASS REQUIRED) — its Bench at (-125.0,-183.4)-(-123.0,-177.4) is
    ///   published in Core and filled nowhere on the deck.
    ///   luna B1 (SENIOR ROTA · GREEN SIDE) — its Kitchenette at (-65.2,-168.0)-(-59.2,-162.0) is published
    ///   in Core and filled nowhere on the deck.
    ///   luna B1 (SENIOR ROTA · GREEN SIDE) — its DeskBank at (-105.0,-167.0)-(-76.2,-165.0) is published in
    ///   Core and filled nowhere on the deck.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryPublishedFittingIsDrawnAsAFilledRectangle()
    {
        var wrong = new List<string>();
        int fittings = 0, benches = 0, worktops = 0, cells = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, DeckPlan deck) in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                foreach (RingOffice.Fixture f in room.Furniture)
                {
                    bool box = Math.Abs(f.X1 - f.X0) >= 0.001 && Math.Abs(f.Y1 - f.Y0) >= 0.001;

                    // The little rooms. They must be published by Core and NOT filled by the pen.
                    if (f.Kind is RingOffice.Fitting.Cubicle or RingOffice.Fitting.Booth)
                    {
                        cells++;
                        if (deck.Furniture.Any(s => Same(s, in f)))
                        {
                            wrong.Add($"  {body} B{-level} ({room.Plate}) — its {f.Kind} is FILLED. A cell "
                                + "is a room you step into, and a filled cell is a hiding place drawn as a "
                                + "solid block.");
                        }
                        continue;
                    }

                    if (f.Kind == RingOffice.Fitting.Partition || !box)
                    {
                        continue;   // a screen IS a line
                    }

                    // Counted BEFORE the lookup, so that handing the deck an empty list makes this guard
                    // report the FURNITURE rather than report that it saw none — an anti-vacuity clause that
                    // fires first turns a red into a shrug.
                    fittings++;
                    if (f.Kind == RingOffice.Fitting.Bench)
                    {
                        benches++;
                    }
                    if (RingOffice.IsWorkSurface(f.Kind))
                    {
                        worktops++;
                    }

                    DeckPlan.FurnitureSpot? hit = null;
                    foreach (DeckPlan.FurnitureSpot s in deck.Furniture)
                    {
                        if (Same(s, in f))
                        {
                            hit = s;
                            break;
                        }
                    }

                    if (hit is not { } spot)
                    {
                        wrong.Add($"  {body} B{-level} ({room.Plate}) — its {f.Kind} at "
                            + $"({f.X0:F1},{f.Y0:F1})-({f.X1:F1},{f.Y1:F1}) is published in Core and "
                            + "filled nowhere on the deck.");
                        continue;
                    }

                    int want = DeckPlan.FurnitureSpot.ToneOf(f.Kind);
                    if (spot.Tone != want)
                    {
                        wrong.Add($"  {body} B{-level} ({room.Plate}) — its {f.Kind} draws in tone "
                            + $"{spot.Tone} and it is a {want}.");
                    }
                }
            }
        }

        Assert.True(fittings > 300, $"only {fittings} fitting(s) were measured — this proved little.");
        Assert.True(benches > 40,
            $"only {benches} bench(es) were met — the fixture the owner called a line barely ran.");
        Assert.True(worktops > 150, $"only {worktops} work surface(s) were met — this proved little.");
        Assert.True(cells > 100,
            $"only {cells} cubicle(s)/booth(s) were met — the half of this guard that proves a hiding place "
            + "is NOT filled never ran.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} fitting(s) Core stood in a room are not drawn as furniture:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }

    /// <summary>Is this drawn rectangle the very box Core published? Corner for corner, at a float cast's
    /// tolerance — the deck is handed the box whole and the only thing between them is a cast.</summary>
    private static bool Same(in DeckPlan.FurnitureSpot s, in RingOffice.Fixture f) =>
        Math.Abs(s.X0 - f.X0) < 0.01 && Math.Abs(s.Y0 - f.Y0) < 0.01
        && Math.Abs(s.X1 - f.X1) < 0.01 && Math.Abs(s.Y1 - f.Y1) < 0.01;
}
