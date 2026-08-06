using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #724 · A DOORWAY WITH NO FUNNEL. Owner, playing <c>/map?secretlab=deep&amp;land=1&amp;floor=1</c>: <i>"walk
/// up the first rib to CANTEEN 1's west face, then walk straight at the doorway… I pressed D thirty-five
/// times against the wall with zero movement, and the screen gives no hint the door is half a step north."</i>
///
/// <para>The door was never sealed and the cut was never narrow: a Hive doorway is
/// <see cref="UndergroundComplex.DoorHalf"/> × 2 = 6.4 du of clear opening and a body is
/// <see cref="DeckPlan.AvatarRadius"/> × 2 = 1.4 du across. The captain was simply a body-width off the
/// mouth, and axis-separated collision converted the entire press into nothing. On the deck-plan zoom a jamb
/// and a doorway are a few pixels apart, so that reads as <b>the door is sealed</b> — a false locked-door
/// tell in the one game whose locked doors are load-bearing storytelling.</para>
///
/// <para>THE LAW, in the issue's own words: <i>"a captain one body-width off a doorway cut, pressing only the
/// axis through it, must pass within N steps."</i> Stated here on REAL generated geometry — the same
/// <see cref="UndergroundComplex.Build"/> floors the client walks, through the same
/// <see cref="DeckPlan.Move"/> the arrow keys dispatch — because a hand-typed pair of walls would only ever
/// prove that the arithmetic does what the arithmetic does. The synthetic companion cases (a wall list you
/// can read in one glance, including the ones where the funnel must NOT fire) live beside the primitive, in
/// <c>SurfaceCollisionTests</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AJambIsNotASealedDoorTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The floor the owner was standing on when they filed it.</summary>
    private const string ReproBody = "secret-lab-site-unlisted";
    private const int ReproLevel = -1;

    /// <summary>Where the walk starts: far enough back that the captain is genuinely walking down a corridor
    /// at the door rather than being placed in its mouth, close enough to stay inside the rib.</summary>
    private const double RunUp = 4.0;

    /// <summary>One press, at the speed the deck actually walks: 9 du/s at a 60 Hz frame. The law must not
    /// depend on it, so every case below is run at this AND at a third of it.</summary>
    private const double PressStep = 0.15;

    /// <summary>How many presses count as "within N steps". 400 × 0.15 du is sixty deck units of walking to
    /// cross four — absurdly generous on purpose, because the failure being guarded is not slowness, it is a
    /// captain who never moves again. The owner gave it thirty-five.</summary>
    private const int Presses = 400;

    /// <summary>How far off the cut's centreline a captain must still be taken through, and it is a
    /// geometric statement rather than a tuned number: <b>if any part of the body is over the opening, the
    /// opening takes it.</b> The body's near edge is still on the cut right up to one radius past the jamb,
    /// so the band is the cut's own half-width plus a radius. Past it the captain is squarely in front of
    /// poured stone, and stone is allowed to stop them — that case is asserted too, below.</summary>
    private static double BandHalf => UndergroundComplex.DoorHalf + DeckPlan.AvatarRadius;

    /// <summary>How many offsets the band is sounded at. Twenty-one samples over 3.9 du, and — because it
    /// divides exactly — the last of them sits ON the edge of the law rather than safely inside it.</summary>
    private const int BandSamples = 20;

    /// <summary>Walks that actually happened. A guard whose every case was skipped is the fifth named bug
    /// class wearing a green tick, and every case here begins with a standability check that COULD skip it.
    /// So the sweeps count, and the count is asserted.</summary>
    private int _walked;

    [Fact]
    public void AtTheCanteenDoor_ThirtyFivePressesOfTheThroughAxis_ActuallyGoSomewhere()
    {
        // The repro floor, doorway by doorway. Every offset across the band, both hands off the cut's
        // centreline, both approach sides, both press sizes.
        var bad = new List<string>();
        Sweep(ReproBody, ReproLevel, bad);
        Report(bad, $"the doorways of {ReproBody} B{-ReproLevel} — the floor #724 was filed from", 300);
    }

    [Fact]
    public void AndEveryOtherDoorwayInEveryOtherHiveToo()
    {
        // A look tells you about the door you happened to walk at; a flood tells you about every doorway of
        // every clandestine site in the system. The bug was never specific to the canteen — it was the
        // collision primitive — so the law is stated where it lives.
        var bad = new List<string>();
        foreach (string body in new[] { "luna", "phobos", "europa", "titan", "miranda", "the-clinker" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                Sweep(body, level, bad);
            }
        }
        Report(bad, "every doorway of every clandestine site", 5000);
    }

    [Fact]
    public void ButAWallIsStillAWall_AndPressingIntoOneGetsYouNothing()
    {
        // THE OTHER HALF OF THE LAW, and the one that keeps the fix from being "walk through walls". Beyond
        // the band no part of the captain is over the opening any more — they are square in front of poured
        // stone, and a facility whose walls funnel you sideways from three metres away would be a facility
        // with no walls. This is also the case that would go red first if the reach were ever quietly widened
        // to make some other floor pass.
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(ReproBody, ReproLevel, Field);
        DeckPlan deck = HiveInterior.FloorDeck(ReproBody, ReproLevel, Field, 0, (_, _) => { }, []);
        var through = new List<string>();
        int tried = 0;

        foreach (SurfaceLayout.Doorway cut in floor.Doorways)
        {
            bool cutRunsAlongX = Math.Abs(cut.X1 - cut.X2) > Math.Abs(cut.Y1 - cut.Y2);
            double alongCentre = cutRunsAlongX ? (cut.X1 + cut.X2) / 2 : (cut.Y1 + cut.Y2) / 2;
            double wallAt = cutRunsAlongX ? cut.Y1 : cut.X1;

            // A full body-width clear of the band: the near edge of the body is a whole body off the cut.
            foreach (double off in new[] { BandHalf + (2 * DeckPlan.AvatarRadius), BandHalf + (4 * DeckPlan.AvatarRadius) })
            {
                foreach (int hand in new[] { -1, 1 })
                {
                    double along = alongCentre + (off * hand);
                    double x = cutRunsAlongX ? along : wallAt - RunUp;
                    double y = cutRunsAlongX ? wallAt - RunUp : along;
                    if (deck.Collides(x, y))
                    {
                        continue;
                    }
                    tried++;
                    double dx = cutRunsAlongX ? 0 : PressStep, dy = cutRunsAlongX ? PressStep : 0;
                    for (int i = 0; i < Presses; i++)
                    {
                        (x, y) = deck.Move(x, y, dx, dy);
                    }
                    if ((cutRunsAlongX ? y : x) > wallAt)
                    {
                        through.Add($"  {off * hand:+0.00;-0.00} du off the cut at ({cut.X1:F1},{cut.Y1:F1}) — "
                            + "the captain walked out the far side of solid wall.");
                    }
                }
            }
        }

        Assert.True(tried >= 12, $"only {tried} approach(es) started — this proved nothing about walls.");
        Assert.True(through.Count == 0, string.Join(Environment.NewLine, through));
    }

    private void Sweep(string body, int level, List<string> bad)
    {
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
        DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

        foreach (SurfaceLayout.Doorway cut in floor.Doorways)
        {
            // A cut is a segment in the wall it was taken out of, so the axis it is DRAWN along is the axis
            // you stand off, and the other one is the axis you walk through it on.
            bool cutRunsAlongX = Math.Abs(cut.X1 - cut.X2) > Math.Abs(cut.Y1 - cut.Y2);
            double alongCentre = cutRunsAlongX ? (cut.X1 + cut.X2) / 2 : (cut.Y1 + cut.Y2) / 2;
            double wallAt = cutRunsAlongX ? cut.Y1 : cut.X1;

            for (int i = 0; i <= BandSamples; i++)
            {
                double off = BandHalf * i / BandSamples;
                foreach (int hand in new[] { -1, 1 })
                {
                    foreach (int side in new[] { -1, 1 })
                    {
                        foreach (double press in new[] { PressStep, PressStep / 3 })
                        {
                            Walk(deck, cutRunsAlongX, alongCentre + (off * hand), wallAt, side, press,
                                $"{body} B{-level} cut at ({cut.X1:F1},{cut.Y1:F1})-({cut.X2:F1},{cut.Y2:F1})"
                                    + $" · {off * hand:+0.00;-0.00;0.00} du off the centreline"
                                    + $" · approached from {(side < 0 ? "below" : "above")} at {press:F2} du/press",
                                bad);
                        }
                    }
                }
            }
        }
    }

    private void Walk(
        DeckPlan deck, bool cutRunsAlongX, double along, double wallAt, int side, double press,
        string what, List<string> bad)
    {
        // Stand back from the wall on the chosen side, level with the offset being tested.
        double x = cutRunsAlongX ? along : wallAt - (RunUp * side);
        double y = cutRunsAlongX ? wallAt - (RunUp * side) : along;

        // #600's lesson, both ends of it: a walk that starts inside stone proves nothing either way, and a
        // corridor that has no room to stand at this offset is not this test's business.
        if (deck.Collides(x, y))
        {
            return;
        }
        _walked++;

        // Pure through-axis, exactly as the owner pressed it: no second key, ever.
        double dx = cutRunsAlongX ? 0 : press * side, dy = cutRunsAlongX ? press * side : 0;
        for (int i = 0; i < Presses; i++)
        {
            (x, y) = deck.Move(x, y, dx, dy);
            double across = cutRunsAlongX ? y : x;
            if ((across - wallAt) * side > 0)
            {
                return;   // through the door
            }
        }

        double ended = cutRunsAlongX ? y : x;
        bad.Add($"  {what}: {Presses} presses left the captain at {ended.ToString("F2", CultureInfo.InvariantCulture)}, "
            + $"still on the near side of the wall at {wallAt.ToString("F2", CultureInfo.InvariantCulture)}.");
    }

    private void Report(List<string> bad, string what, int atLeast)
    {
        Assert.True(_walked >= atLeast,
            $"only {_walked} walk(s) ever started — this sweep proved nothing about {what}.");
        if (bad.Count == 0)
        {
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"{bad.Count} approach(es) pinned on the jamb — {what}:");
        foreach (string line in bad.Take(12))
        {
            sb.AppendLine(line);
        }
        if (bad.Count > 12)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  …and {bad.Count - 12} more.");
        }
        Assert.Fail(sb.ToString());
    }
}
