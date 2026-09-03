using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1074 beat 4 · IS THE MUG ACTUALLY ON THE SHELF? — the wiring half of the career-cost beat.
///
/// <para>Core decides who is in the room and where the shelf is (<c>TheCareerCostTests</c> pins both laws,
/// each proven red). What is left to get wrong is exactly what this repo has been caught getting wrong
/// before: a renderer that asks the predicate and then draws something else, draws nothing, or draws it on
/// every ground in the game. So these build the deck a captain's boots actually collide with and count the
/// glasses on it.</para>
///
/// <para><i>"The mug is the whole testimony."</i> It is drawn as a LABEL and never a console: there is
/// nothing to work, nothing to press and nothing to take — a captain sees a glass on a shelf behind a woman
/// eating, and the only thing he can do about it is ask her.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheMugOnTheShelfTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto", "titan", "enceladus", "miranda", "triton",
    ];

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, MoonSurface.ExpeditionField(), 0, (_, _) => { }, []);

    private static int MugsOn(DeckPlan deck) =>
        deck.RoomLabels.Count(l => l.Text == CareerCost.MugGlyph);

    /// <summary>
    /// #1074 · ONE MUG, ON THE FLOOR THE PEOPLE ARE ON, ON A STOPPED GROUND AND NOWHERE ELSE.
    ///
    /// <para>Three failures in one guard, and each of them has happened in this building before: a prop that
    /// leaks onto every world (nothing of this beat may exist where no order stands), a prop that leaks down
    /// the descent gradient (B1 is the only floor with people on it), and a prop that is simply not wired up
    /// at all.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the <c>MugPlate</c> arm removed from
    /// <c>HiveInterior.FloorDeck</c> — <i>"9 floor(s) disagree with the mug's own law: luna B1: 0 mug(s) where
    /// one regular keeps one …"</i>; and the plate check widened to every seated regular — <i>"luna B1: 6
    /// mug(s) where one regular keeps one"</i>, a shelf of glasses behind every table in the room.</para>
    /// </summary>
    [Fact]
    public void OneMugIsDrawnOnAStoppedGroundAndNoneAnywhereElse()
    {
        var wrong = new List<string>();
        int drawn = 0;

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } top)
            {
                continue;
            }

            // ── ORDINARY. No order has been posted here and there is no mug in the building.
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (MugsOn(DeckFor(body, level)) > 0)
                {
                    wrong.Add($"  {body} B{-level}: a mug on a ground nobody has stopped");
                }
            }

            // ── CLOSED. Exactly one, on the canteen floor, and on no other floor of the site.
            StopOrder.Install([body]);
            try
            {
                foreach (int level in UndergroundComplex.FloorsOf(body))
                {
                    int mugs = MugsOn(DeckFor(body, level));
                    drawn += mugs;

                    if (level == top && mugs != 1)
                    {
                        wrong.Add($"  {body} B{-level}: {mugs} mug(s) where one regular keeps one");
                    }
                    if (level != top && mugs > 0)
                    {
                        wrong.Add($"  {body} B{-level}: {mugs} mug(s) below the floor the people are on");
                    }
                }
            }
            finally
            {
                StopOrder.Install([]);
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} floor(s) disagree with the mug's own law:\n{string.Join("\n", wrong.Take(20))}");
        Assert.True(drawn >= 5, $"only {drawn} mug(s) were drawn across {Bodies.Length} sites.");
    }

    /// <summary>
    /// #1074 · THE MUG IS EXACTLY WHERE CORE SAID, AND IT IS NOT A CONSOLE.
    ///
    /// <para>The renderer may not round, nudge or re-place it (§13.15): Core clamped that coordinate into the
    /// room off the top's own chair ring, and a shelf offset by a hand-typed du here would be a second author
    /// for one piece of furniture. And nothing may be pressable at it — a mug with a verb on it would turn
    /// the one object in this beat that says nothing into a thing the game hands you.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the label's coordinate written as the table's own rather than
    /// asked of <c>CareerCost.MugAt</c> — <i>"Assert.Contains() Failure: Filter not matched in
    /// collection"</i>, the glass drawn in her dinner.</para>
    /// </summary>
    [Fact]
    public void TheMugIsWhereCoreSAIDAndNothingIsPressableAtIt()
    {
        int checkedMugs = 0;

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } top)
            {
                continue;
            }

            StopOrder.Install([body]);
            try
            {
                DeckPlan deck = DeckFor(body, top);
                UndergroundComplex.FloorPlan floor =
                    UndergroundComplex.Build(body, top, MoonSurface.ExpeditionField());

                foreach (UndergroundComplex.Amenity a in floor.Amenities)
                {
                    foreach (CanteenRegulars.TableSeat seat in CanteenRegulars.Tables(body, top, a))
                    {
                        if (seat.Plate != CareerCost.MugPlate)
                        {
                            continue;
                        }

                        (double mx, double my) = CareerCost.MugAt(seat, a);
                        Assert.Contains(deck.RoomLabels, l =>
                            l.Text == CareerCost.MugGlyph
                            && Math.Abs(l.X - (float)mx) < 0.01f
                            && Math.Abs(l.Y - (float)my) < 0.01f);

                        // …and no console within reach of it. The regular's own is at the TABLE, which is
                        // where a captain stops to talk to her; the shelf is scenery.
                        Assert.DoesNotContain(deck.Consoles, c =>
                            Math.Abs(c.X - (float)mx) < 0.01f && Math.Abs(c.Y - (float)my) < 0.01f);
                        checkedMugs++;
                    }
                }
            }
            finally
            {
                StopOrder.Install([]);
            }
        }

        Assert.True(checkedMugs >= 5, $"only {checkedMugs} mug(s) were checked.");
    }
}
