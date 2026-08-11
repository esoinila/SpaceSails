using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #827 · ONE COUNTER, ONE PLACE — the bar has stopped having three opinions about where it is.
///
/// <para>Owner, evening playtest 2026-08-11, from stool 3: <i>"the counter should be at the counter position
/// in the underlying picture … the yellow box seems like it is the counter … we should have the counter as
/// something that cannot be walked through but can be used as a table."</i> And, from the seats' side:
/// <i>"Now the blue seats are like without the table that the counter always provides."</i></para>
///
/// <h3>What the pixels showed</h3>
///
/// <para>Three representations of one bar at two heights. The [E] service rail labelled THE COUNTER was laid
/// 2.0 du out from the counter's line; the row of stools 1.6 du out from it; the desk's photograph ON it. So
/// the seats bellied up to a cyan rail floating in open floor, a step in front of the yellow box the owner —
/// correctly — read as the counter. Every one of those three numbers was right. There were just three of
/// them, which is this house's third named bug class: <b>one thing, two authors, no shared source.</b></para>
///
/// <h3>What is asserted here</h3>
///
/// <para>(a) the row and the run lie on the DESK'S OWN FACE, (b) the desk cannot be walked through and still
/// serves anywhere along it, (c) the row is one-sided and its gaps are where the published list says, and
/// (d) the photograph is stretched over the desk rect itself. Every one of them is measured against
/// <see cref="UndergroundComplex.CounterDesk"/> — the published rect — and against
/// <see cref="SurfaceScale.CaptainWidthDu"/>, which is the captain's own body and not a number this file
/// chose.</para>
/// </summary>
public sealed class OneCounterAndOnlyOneTests
{
    /// <summary>The same spread <c>TheDeskServesItsWholeLengthTests</c> sweeps.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 20).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The captain's body, as Core knows it. <c>DeckPlan.AvatarRadius</c> lives in the client and
    /// cannot be referenced from here; <see cref="SurfaceScale.CaptainWidthDu"/> is the mirror the client's
    /// own guard holds it to, so this is the same body the collision measures.</summary>
    private const double BodyRadiusDu = SurfaceScale.CaptainWidthDu / 2.0;

    /// <summary>How far an [E] reaches — <c>DeckPlan.InteractRadius</c>, restated because it lives in the
    /// client. Only ever used to assert that the whole face is IN reach, so a mirror that drifted small
    /// would make this guard stricter rather than weaker.</summary>
    private const double InteractRadiusDu = 3.0;

    /// <summary>A hair. The carve's own arithmetic is exact; this is here so a guard cannot be defeated by
    /// the last bit of a double.</summary>
    private const double Eps = 0.001;

    private static IEnumerable<(string Body, int Level, UndergroundComplex.Comfort Use,
        UndergroundComplex.Amenity Amenity, UndergroundComplex.Hall Hall,
        UndergroundComplex.FloorPlan Floor)> Halls()
    {
        foreach (string body in Sweep())
        {
            for (int level = -1; level >= -8; level--)
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                foreach (UndergroundComplex.Amenity a in floor.Amenities)
                {
                    if (a.Hall is { } hall)
                    {
                        yield return (body, level, a.Use, a, hall, floor);
                    }
                }
            }
        }
    }

    /// <summary>Every serving counter in the game, with the desk it is.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Amenity Amenity,
        UndergroundComplex.Hall Hall, UndergroundComplex.CounterDesk Desk,
        UndergroundComplex.FloorPlan Floor)> Counters()
    {
        foreach ((string body, int level, _, UndergroundComplex.Amenity a,
            UndergroundComplex.Hall hall, UndergroundComplex.FloorPlan floor) in Halls())
        {
            if (hall.Desk is { } desk && desk.Row.Count > 0)
            {
                yield return (body, level, a, hall, desk, floor);
            }
        }
    }

    /// <summary>The walls a body is actually stopped by, as the collision's own segments — the same three
    /// lists <c>HiveInterior</c> hands the deck the captain's boots collide with.
    ///
    /// <para>The GLASS goes in because an eye crosses it and a body does not, and a walk that ignored it
    /// would find a way out through the park. The LOCKED DOORS go in because that is what a locked door is
    /// down here: a leaf, a plate, and a wall behind it. The goods hoist's shutter is one of them (#775),
    /// and leaving it out would have this guard walk through a door the game does not open — which is the
    /// same mistake in reverse.</para></summary>
    private static SurfaceCollision.Segment[] Solid(in UndergroundComplex.FloorPlan floor) =>
    [
        .. floor.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)),
        .. (floor.Windows ?? []).Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)),
        .. floor.Locked.Select(l => new SurfaceCollision.Segment(l.X1, l.Y1, l.X2, l.Y2)),
    ];

    /// <summary>Which way is OUT of the desk and into the hall, as a unit vector — read off a published
    /// place rather than worked out from the room's axes, because the places are the only thing in the
    /// record that knows which side the customers are on.</summary>
    private static (double X, double Y) IntoTheHall(in UndergroundComplex.CounterDesk desk)
    {
        UndergroundComplex.CounterPlace p = desk.Row[0];
        double d = p.StandoffDu;
        return d > 0 ? ((p.X - p.FaceX) / d, (p.Y - p.FaceY) / d) : (0, 0);
    }

    // ── (a) THE DRIFT, KILLED ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #827 · THE [E] RUN IS THE DESK'S FRONT FACE, AND EVERY STOOL TOUCHES IT.
    ///
    /// <para>Two clauses, one law. The service run's own two ends lie ON the rect's customer face — not a
    /// standoff out from it, not a standoff in — and every seat's centre is between a body's RADIUS and a
    /// body's WIDTH off that face, which is what "the person on the stool has their elbows on the counter"
    /// is in deck units. The bound is <see cref="SurfaceScale.CaptainWidthDu"/>'s and not this file's: a row
    /// laid at any offset somebody liked the look of fails it.</para>
    ///
    /// <para><b>Proven RED against the bar as it shipped</b> — the run at <c>HallServiceStandoffDu</c> and
    /// the row at the old <c>HallStoolStandoffDu = 1.6</c>:</para>
    /// <code>
    /// 41 counter(s) are in more than one place:
    ///   europa B1: the [E] run is 2.00 du off the desk's own face — the rail floats in open floor.
    ///   europa B1 stool 0: sits 1.60 du off the desk's face, further than a body is wide (1.40) —
    ///       a seat at a counter that is not there.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void THE_RUN_IsTheDesksFaceAndTheStoolsBellyUpToIt()
    {
        var wrong = new List<string>();
        int counters = 0, seats = 0;

        foreach ((string body, int level, _, UndergroundComplex.Hall hall,
            UndergroundComplex.CounterDesk desk, _) in Counters())
        {
            counters++;

            if (hall.Service is not { } run)
            {
                wrong.Add($"  {body} B{-level}: a counter with a row and no run at all.");
                continue;
            }

            foreach ((double x, double y, string which) in new[]
            {
                (run.X0, run.Y0, "one end"), (run.X1, run.Y1, "the other end"),
                (run.MidX, run.MidY, "the middle"),
            })
            {
                double off = desk.DistanceToFace(x, y);
                if (off > Eps)
                {
                    wrong.Add($"  {body} B{-level}: the [E] run is {off:F2} du off the desk's own face at "
                        + $"{which} — the rail floats in open floor.");
                }
            }

            foreach (UndergroundComplex.CounterPlace place in desk.Row)
            {
                if (!place.Seated)
                {
                    continue;
                }

                seats++;
                double off = desk.DistanceToFace(place.X, place.Y);
                if (off < BodyRadiusDu - Eps)
                {
                    wrong.Add($"  {body} B{-level} stool {place.Stool}: sits {off:F2} du off the desk's "
                        + $"face — inside a counter a body is {BodyRadiusDu:F2} du thick cannot be.");
                }
                else if (off > SurfaceScale.CaptainWidthDu + Eps)
                {
                    wrong.Add($"  {body} B{-level} stool {place.Stool}: sits {off:F2} du off the desk's "
                        + $"face, further than a body is wide ({SurfaceScale.CaptainWidthDu:F2}) — a seat "
                        + "at a counter that is not there.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} counter(s) are in more than one place:\n" + string.Join("\n", wrong.Take(12)));

        // ANTI-VACUITY. A sweep that met no serving counter would pass having measured nothing, and one that
        // met a counter with no seats would prove nothing about a ROW.
        Assert.True(counters >= 20, $"only {counters} serving counter(s) in the whole sweep.");
        Assert.True(seats >= 20 * TheStools.Count, $"only {seats} stool(s) were measured.");
    }

    /// <summary>
    /// #827 · AND THE PHOTOGRAPH IS THE DESK. The bar-desk picture is stretched over the rect itself — same
    /// four numbers, not four numbers that agree.
    ///
    /// <para>The picture was the one thing on this bar that was already in the right place, and it was in
    /// the right place because two pieces of arithmetic happened to match. That is a coincidence with a
    /// maintenance schedule. It reads the rect now.</para>
    /// </summary>
    [Fact]
    public void THE_PICTURE_IsStretchedOverTheDeskRectItself()
    {
        var wrong = new List<string>();
        int painted = 0;

        foreach ((string body, int level, _, UndergroundComplex.Hall hall,
            UndergroundComplex.CounterDesk desk, _) in Counters())
        {
            foreach (UndergroundComplex.SpotArt art in hall.Painted)
            {
                if (art.Url != UndergroundComplex.CounterArtUrl)
                {
                    continue;
                }

                painted++;
                if (Math.Abs(art.X0 - desk.X0) > Eps || Math.Abs(art.Y0 - desk.Y0) > Eps
                    || Math.Abs(art.X1 - desk.X1) > Eps || Math.Abs(art.Y1 - desk.Y1) > Eps)
                {
                    wrong.Add($"  {body} B{-level}: the bar's picture is ({art.X0:F2},{art.Y0:F2})-"
                        + $"({art.X1:F2},{art.Y1:F2}) and the desk is ({desk.X0:F2},{desk.Y0:F2})-"
                        + $"({desk.X1:F2},{desk.Y1:F2}).");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} bar picture(s) are not the desk:\n" + string.Join("\n", wrong.Take(12)));
        Assert.True(painted >= 20, $"only {painted} bar picture(s) were measured.");
    }

    // ── (b) SOLID, BUT SERVABLE ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #827 · YOU CANNOT WALK THROUGH THE COUNTER — and you can order anywhere along it.
    ///
    /// <para>Owner: <i>"we should have the counter as something that cannot be walked through but can be
    /// used as a table."</i> Both halves, on the same room, in the same guard, because either one alone is
    /// satisfiable by something nobody wants: a wall you cannot press, or a press you can walk through.</para>
    ///
    /// <para><b>The walk is a real A* over the real collision geometry</b> — <see cref="DeckReachability"/>,
    /// the same walker that found #587 and #600 — from the customer's own standing square to the keep's spot
    /// behind the desk. It must fail. Both endpoints are asserted STANDABLE first, because a route that
    /// fails because its destination is inside a wall is a different and much weaker statement (#600's
    /// lesson), and a control walk to the other end of the same counter must SUCCEED, so a bounds slip
    /// cannot quietly make every walk fail.</para>
    ///
    /// <para><b>Proven RED</b> by taking the desk's own front face out of the wall list — the one line the
    /// whole counter rests on:</para>
    /// <code>
    /// 41 counter(s) can be walked through, or cannot be ordered at:
    ///   luna B1: the keep's own square is reachable from the customer's side in 6 steps.
    ///   luna B1: a captain can walk off the hall floor into the goods hoist's car — the crew's own side of
    ///       the counter is open floor.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void THE_COUNTER_CannotBeWalkedThroughAndSERVESAnywhereAlongIt()
    {
        var wrong = new List<string>();
        int counters = 0, served = 0;

        foreach ((string body, int level, _, UndergroundComplex.Hall hall,
            UndergroundComplex.CounterDesk desk, UndergroundComplex.FloorPlan floor) in Counters())
        {
            if (hall.Service is not { } run)
            {
                continue;
            }

            counters++;
            SurfaceCollision.Segment[] solid = Solid(in floor);
            (double MinX, double MinY, double MaxX, double MaxY) bounds =
                (hall.X0 - 1, hall.Y0 - 1, hall.X1 + 1, hall.Y1 + 1);

            var from = new DeckReachability.Point(run.StandX, run.StandY);
            var keep = new DeckReachability.Point(run.KeepX, run.KeepY);

            // Both ends first. A refusal that came from standing in a wall would prove nothing at all.
            if (!DeckReachability.Standable(from.X, from.Y, BodyRadiusDu, solid))
            {
                wrong.Add($"  {body} B{-level}: the customer's own standing square is solid wall — the "
                    + "walk below would refuse for the wrong reason.");
                continue;
            }
            if (!DeckReachability.Standable(keep.X, keep.Y, BodyRadiusDu, solid))
            {
                wrong.Add($"  {body} B{-level}: the keep's own square is solid wall — the walk below would "
                    + "refuse for the wrong reason.");
                continue;
            }

            DeckReachability.Walk across = DeckReachability.Path(from, keep, solid, BodyRadiusDu, bounds);
            if (across.Reached)
            {
                wrong.Add($"  {body} B{-level}: the keep's own square is reachable from the customer's "
                    + $"side in {across.Steps} steps.");
            }

            // …AND THE GOODS HOIST'S OWN CAR, which is the same band and the same law. It is behind the same
            // line — its front is the shutter, which is a locked door and therefore a wall until somebody
            // takes the hasp off it with a sentry (#803). The flood the client already runs cannot see this
            // one: it watches the desk's own photograph, and the car is the twelve du before the picture
            // starts. So the counter is asked about its WHOLE length here, hoist and all.
            if (hall.Freight is { } hoist)
            {
                double carX = (hoist.X0 + hoist.X1) / 2.0, carY = (hoist.Y0 + hoist.Y1) / 2.0;
                if (!DeckReachability.Standable(carX, carY, BodyRadiusDu, solid))
                {
                    wrong.Add($"  {body} B{-level}: the goods car has no floor in it — the walk below "
                        + "would refuse for the wrong reason.");
                }
                else if (DeckReachability.Path(
                    from, new DeckReachability.Point(carX, carY), solid, BodyRadiusDu, bounds).Reached)
                {
                    wrong.Add($"  {body} B{-level}: a captain can walk off the hall floor into the goods "
                        + "hoist's car — the crew's own side of the counter is open floor.");
                }
            }

            // THE CONTROL. The far end of the same desk, on the customer's side — a walk that must succeed,
            // so the refusal above cannot be a bounds slip or a walker that never reaches anything.
            (double ox, double oy) = IntoTheHall(in desk);
            var along = new DeckReachability.Point(
                run.X1 + (ox * SurfaceScale.CaptainWidthDu * 2),
                run.Y1 + (oy * SurfaceScale.CaptainWidthDu * 2));
            if (DeckReachability.Standable(along.X, along.Y, BodyRadiusDu, solid)
                && !DeckReachability.Path(from, along, solid, BodyRadiusDu, bounds).Reached)
            {
                wrong.Add($"  {body} B{-level}: the far end of the captain's own side of the bar cannot be "
                    + "walked to either — this room is broken in some other way and the refusal above "
                    + "means nothing.");
            }

            // …AND IT STILL SERVES. Every step of the way along the front, at the distance a customer
            // stands, [E] reaches the run. A counter you cannot press is a wall.
            int steps = Math.Max(8, (int)(run.LengthDu / 1.5));
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double sx = run.X0 + ((run.X1 - run.X0) * t) + (ox * UndergroundComplex.HallServiceStandoffDu);
                double sy = run.Y0 + ((run.Y1 - run.Y0) * t) + (oy * UndergroundComplex.HallServiceStandoffDu);
                served++;
                if (!run.Serves(sx, sy, InteractRadiusDu))
                {
                    wrong.Add($"  {body} B{-level}: standing at ({sx:F1},{sy:F1}) — on the desk's own front "
                        + "— [E] does not reach the counter.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} counter(s) can be walked through, or cannot be ordered at:\n"
            + string.Join("\n", wrong.Take(12)));
        Assert.True(counters >= 20, $"only {counters} counter(s) were walked.");
        Assert.True(served >= 400, $"only {served} press point(s) — the desk was barely walked.");
    }

    // ── (c) THE GAPPED, ONE-SIDED ROW ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// #827 · THE ROW IS ONE-SIDED, AND ITS GAPS ARE WHERE THE LIST SAYS.
    ///
    /// <para>Owner: <i>"the counter is the biggest table with customer seats only on one side … but not
    /// continuously … there are gaps for people to walk to the cashier etc."</i> Four clauses:</para>
    ///
    /// <para><b>One side, and the keep's side publishes nothing.</b> Every place in the row — seat or gap —
    /// is on the hall side of the face and outside the desk's own box, and the desk says so in the record
    /// (<see cref="RingOffice.Seating.OneSide"/>, the field #834 gave the office fixtures).</para>
    ///
    /// <para><b>The gaps are real gaps.</b> Exactly <see cref="UndergroundComplex.HallCounterGaps"/> of
    /// them, one till and one collection point, and NO stool is laid at either — the seats step over the
    /// holes rather than being drawn under them.</para>
    ///
    /// <para><b>The seats keep their ordinals through the gaps.</b> Entry <c>s</c> of the row's seats is
    /// stool <c>s</c>, which is what #820's snap and #792's occupancy both read the row by. A gap that
    /// renumbered the seats would sit a captain in somebody's lap.</para>
    ///
    /// <para><b>Proven RED</b> by laying the row the way #792 did — eight seats, evenly, no holes:</para>
    /// <code>
    /// 41 counter row(s) are wrong:
    ///   europa B1: 8 place(s) in the row and no TILL in it at all.
    ///   europa B1: 8 place(s) in the row and no COLLECTION in it at all.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void THE_ROW_SeatsOneSideOnlyAndLeavesItsGapsWhereItSaysItDoes()
    {
        var wrong = new List<string>();
        int counters = 0;

        foreach ((string body, int level, _, UndergroundComplex.Hall hall,
            UndergroundComplex.CounterDesk desk, _) in Counters())
        {
            counters++;
            string where = $"  {body} B{-level}";

            if (desk.Sides != RingOffice.Seating.OneSide)
            {
                wrong.Add($"{where}: the desk says it seats {desk.Sides} — a counter seats ONE side.");
            }

            // THE GAPS. Counted off the published row, and each one asserted to be a hole the seats stepped
            // over rather than a label on a seat.
            int gaps = 0;
            foreach (UndergroundComplex.CounterPost post in
                new[] { UndergroundComplex.CounterPost.Till, UndergroundComplex.CounterPost.Collection })
            {
                if (desk.Gap(post) is not { } gap)
                {
                    wrong.Add($"{where}: {desk.Row.Count} place(s) in the row and no {post} in it at all.");
                    continue;
                }

                gaps++;
                if (gap.Seated || gap.Stool >= 0)
                {
                    wrong.Add($"{where}: the {post} is a stool — the gap is not a gap.");
                }
                if (gap.Plate.Length == 0)
                {
                    wrong.Add($"{where}: the {post} has nothing painted on it, so it reads as a seat "
                        + "somebody unbolted.");
                }
                if (desk.Stools.Any(s => Math.Abs(s.X - gap.X) < Eps && Math.Abs(s.Y - gap.Y) < Eps))
                {
                    wrong.Add($"{where}: a stool is bolted down in the {post}'s own gap.");
                }
            }

            if (gaps != UndergroundComplex.HallCounterGaps)
            {
                wrong.Add($"{where}: {gaps} gap(s) in the row, and the counter says there are "
                    + $"{UndergroundComplex.HallCounterGaps}.");
            }

            // THE SEATS, IN THE COUNTER'S OWN ORDER, still one per stool the sim keeps.
            var ordinals = desk.Row.Where(p => p.Seated).Select(p => p.Stool).ToList();
            if (!ordinals.SequenceEqual(Enumerable.Range(0, TheStools.Count)))
            {
                wrong.Add($"{where}: the seats are numbered [{string.Join(",", ordinals)}] and the sim "
                    + $"keeps {TheStools.Count} stools in order.");
            }
            if (hall.StoolRow.Count != TheStools.Count)
            {
                wrong.Add($"{where}: the published row hands out {hall.StoolRow.Count} seats.");
            }

            // ONE SIDE. Every place — seat and gap alike — is out in the hall, and the keep's side of the
            // face has nothing on it.
            (double ox, double oy) = IntoTheHall(in desk);
            foreach (UndergroundComplex.CounterPlace place in desk.Row)
            {
                double side = ((place.X - place.FaceX) * ox) + ((place.Y - place.FaceY) * oy);
                if (side <= 0)
                {
                    wrong.Add($"{where}: place {place.Index} ({place.Post}) is {-side:F2} du on the KEEP's "
                        + "side of the desk.");
                }
                if (desk.Contains(place.X, place.Y))
                {
                    wrong.Add($"{where}: place {place.Index} ({place.Post}) is inside the desk itself.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} counter row(s) are wrong:\n" + string.Join("\n", wrong.Take(16)));
        Assert.True(counters >= 20, $"only {counters} counter row(s) were read.");
    }

    /// <summary>
    /// #827 · AND A COUNTER NOBODY IS SERVED AT PUBLISHES NO SEATS. The staff mess and the head office's
    /// dining room have a counter in them and have never poured anything over it; a row of stools there
    /// would be eight seats nothing will ever sit on, drawn on the deck, offered to the eye.
    ///
    /// <para>The box is still published — the wall is real, and the collision above is about the wall — so
    /// this asserts the pair: a desk, and an empty row.</para>
    /// </summary>
    [Fact]
    public void A_COUNTER_ThatServesNobodyPublishesABoxAndAnEmptyRow()
    {
        var wrong = new List<string>();
        int quiet = 0;

        foreach ((string body, int level, UndergroundComplex.Comfort use, _,
            UndergroundComplex.Hall hall, _) in Halls())
        {
            if (CounterService.For(body, use) is not null)
            {
                continue;
            }

            quiet++;
            if (hall.Desk is not { } desk)
            {
                wrong.Add($"  {body} B{-level}: no counter box at all — the wall is there and nothing "
                    + "published it.");
                continue;
            }
            if (desk.Row.Count > 0 || hall.StoolRow.Count > 0)
            {
                wrong.Add($"  {body} B{-level}: {desk.Row.Count} place(s) at a counter nobody is served "
                    + "at.");
            }
            if (desk.FaceLengthDu <= 0)
            {
                wrong.Add($"  {body} B{-level}: the counter has no face.");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} unserved counter(s) are wrong:\n" + string.Join("\n", wrong.Take(12)));
        Assert.True(quiet >= 5, $"only {quiet} hall(s) whose counter takes no orders were measured.");
    }
}
