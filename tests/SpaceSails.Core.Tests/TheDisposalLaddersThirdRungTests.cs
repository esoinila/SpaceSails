using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #828 · THE THIRD RUNG — the office secure disposal, and the only disposal in the game that is not a bet.
///
/// <para>Owner: <i>"One thing the good offices would also have is a safe paper disposal trashes… that
/// visually destroy the notes as we watch… a more secure disposal than restaurant trash."</i></para>
///
/// <para>The ladder was already a ladder and it was priced like every other privacy instrument here: the
/// slop bin is a bet against curious hands, the paper bin is a bet against professionals who read what they
/// empty, the chute is a bet that <i>somewhere</i> is far enough. This adds the rung that ANSWERS instead of
/// wagering — and it is nowhere except behind the rank that pays for a kitchenette and two staff WCs, which
/// is the whole of what the fixture says about who gets one.</para>
///
/// <h3>What is stated here and what is stated elsewhere</h3>
/// <para>The BEAT — feed it, stand there, and only then is the sheet gone — is a running world's fact and is
/// driven in the client's <c>TheDisposalYouWatchTests</c>. What is stated here is everything a floor plan
/// can answer: where the machine stands, that the bin and the machine are ONE rectangle, and that the top
/// rung files the same shape of fact as the three below it while leaving nothing behind that anybody could
/// ever find.</para>
/// </summary>
public sealed class TheDisposalLaddersThirdRungTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The scenario's own moons plus a wide net of generated ids — the same shape of sweep the ring
    /// audit and the bin audit both cast, because a fixture that lands wrong on one generated moon in forty
    /// is a bug nobody will ever reproduce by hand.</summary>
    private static IEnumerable<string> Sweep()
    {
        foreach (string body in new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        })
        {
            yield return body;
        }
        for (int i = 0; i < 40; i++)
        {
            yield return $"probe-moon-{i}";
        }
    }

    /// <summary>Every floor in the sweep that has an office block on it, with the ring it was carved round.
    /// The machine is a ring FITTING, so a floor with no ring has nothing to say about it.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.FloorPlan Floor,
        UndergroundComplex.Park Park)> EveryBlock()
    {
        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                if (floor.Park is { } park)
                {
                    yield return (body, level, floor, park);
                }
            }
        }
    }

    /// <summary>How far a point is from a segment.</summary>
    private static double ToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double len2 = (dx * dx) + (dy * dy);
        double t = len2 < 1e-9 ? 0.0 : Math.Clamp((((px - x1) * dx) + ((py - y1) * dy)) / len2, 0.0, 1.0);
        double cx = x1 + (t * dx), cy = y1 + (t * dy);
        return Math.Sqrt(((px - cx) * (px - cx)) + ((py - cy) * (py - cy)));
    }

    /// <summary>…and how far a BOX is from one, sampled along the segment. Same measurement the ring's own
    /// clearance audit makes, so a doorway is measured the same way whoever is asking.</summary>
    private static double BoxToSegment(
        in RingOffice.Fixture f, double x1, double y1, double x2, double y2)
    {
        double best = double.MaxValue;
        for (int i = 0; i <= 24; i++)
        {
            double t = i / 24.0;
            double px = x1 + ((x2 - x1) * t), py = y1 + ((y2 - y1) * t);
            double ddx = Math.Max(Math.Max(f.X0 - px, px - f.X1), 0.0);
            double ddy = Math.Max(Math.Max(f.Y0 - py, py - f.Y1), 0.0);
            best = Math.Min(best, Math.Sqrt((ddx * ddx) + (ddy * ddy)));
        }
        return Math.Min(best, ToSegment((f.X0 + f.X1) / 2, (f.Y0 + f.Y1) / 2, x1, y1, x2, y2));
    }

    // ── (a) WHERE IT STANDS ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · THE MACHINE IS A PREMIUM FIXTURE AND STANDS NOWHERE ELSE.
    ///
    /// <para>The ladder is priced. A shredder-class machine belongs to the tier that already runs to a
    /// kitchenette, two staff WCs and a pair of privacy booths (#817's service strip) — so it is in every
    /// big suite and in no plain room, no washroom, no store and no corridor. A machine by every lift would
    /// hand the best rung of a graded ladder to everybody for nothing.</para>
    ///
    /// <para><b>Proven RED</b> by script-adding a second <c>lay.SolidBox</c> call to <c>Banks</c>, which
    /// every desk-dressed room on the ring runs through — premium or not:</para>
    /// <code>
    /// 215 secure disposal(s) are standing outside the tier that pays for one:
    ///   luna B1: ring room 2 — SENIOR ROTA · GREEN SIDE is a premium suite with 2 secure disposal(s) and
    ///     the tier promises exactly one.
    ///   luna B1: ring room 11 (WEST) — REGISTERED OFFICE · GARDEN ASPECT is not a premium suite and has a
    ///     secure disposal in it.
    ///   luna B1: ring room 13 (EAST) — SIGNATORY SUITE · TWO KEYS is not a premium suite and has a secure
    ///     disposal in it.
    /// </code>
    ///
    /// <para>…and <see cref="EVERY_MACHINE_IsExactlyOneBinAndItIsTheTopRung"/> goes red in the same run
    /// (<c>luna B1: 7 machine(s) on the plan and 2 secure bin(s) in the verb's own list.</c>), which is the
    /// pair of laws doing what they are for: one says the machine is in the wrong room, the other says the
    /// stencil and the bucket have come apart.</para>
    /// </summary>
    [Fact]
    public void THE_MACHINE_StandsOnlyInThePremiumSuites()
    {
        var wrong = new List<string>();
        int suites = 0, machines = 0, rooms = 0;

        foreach ((string body, int level, _, UndergroundComplex.Park park) in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                rooms++;
                bool premium = RingOffice.IsBigSuite(in room);
                int here = room.Furniture.Count(f => f.Kind == RingOffice.Fitting.SecureDisposal);
                machines += here;

                if (premium)
                {
                    suites++;

                    // …and the tier gets what the tier was promised. A suite that quietly lost its machine
                    // because a strip ran out of depth is the ladder's top rung going missing on some moons
                    // and not others, which is worse than not having one.
                    if (here != 1)
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number} — {room.Plate} is a premium "
                            + $"suite with {here} secure disposal(s) and the tier promises exactly one.");
                    }
                    continue;
                }

                if (here > 0)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} "
                        + $"({room.Side.ToString().ToUpperInvariant()}) — {room.Plate} is not a premium "
                        + "suite and has a secure disposal in it.");
                }
            }
        }

        // The net, then the LAW, then the tally. In that order on purpose: an anti-vacuity clause that
        // fires ahead of the complaint turns a red into a shrug, and the complaint is the list of rooms.
        Assert.True(rooms > 300, $"only {rooms} ring room(s) were walked — this net proves nothing.");
        Assert.True(suites > 50, $"only {suites} premium suite(s) were found — nothing was really checked.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} secure disposal(s) are standing outside the tier that pays for one:\n"
            + string.Join("\n", wrong.Take(15)));
        Assert.True(machines == suites,
            $"{machines} machine(s) for {suites} premium suite(s) — the fixture and the tier have come "
            + "apart.");
    }

    /// <summary>
    /// #828/#818 · AND IT IS NOT STANDING IN A DOORWAY.
    ///
    /// <para>The oldest and most expensive class of bug in this project is a room that is correct on the
    /// plan and wrong to walk. A machine you have to be able to stand AT, laid across the way out of a
    /// cubicle, would be exactly that — and it is a machine, so a body meets it (the box is solid) rather
    /// than walking through the mistake.</para>
    ///
    /// <para><b>Proven RED</b> by script-laying the box at the STREET FACE of the suite (<c>v = 0</c>
    /// instead of the far end of the strip), which is the one line of the room the doors are cut in:</para>
    /// <code>
    /// 98 secure disposal(s) are in somebody's way:
    ///   luna B1: room 2's secure disposal is 0.0 du from a doorway and the law wants 4.0.
    ///   luna B1: room 3's secure disposal is 0.0 du from a doorway and the law wants 4.0.
    ///   phobos B1: room 2's secure disposal is 0.0 du from a doorway and the law wants 4.0.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_MACHINE_IsClearOfEveryDoorwayAndInsideItsOwnRoom()
    {
        var wrong = new List<string>();
        int machines = 0, doors = 0;

        foreach ((string body, int level, _, UndergroundComplex.Park park) in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                var ways = new List<SurfaceLayout.Doorway>(room.Doors);
                if (room.Gate is { } onto)
                {
                    ways.Add(onto);
                }

                foreach (RingOffice.Fixture f in room.Furniture)
                {
                    if (f.Kind != RingOffice.Fitting.SecureDisposal)
                    {
                        continue;
                    }
                    machines++;
                    doors += ways.Count;

                    if (f.X0 < room.X0 - 0.01 || f.X1 > room.X1 + 0.01
                        || f.Y0 < room.Y0 - 0.01 || f.Y1 > room.Y1 + 0.01)
                    {
                        wrong.Add($"  {body} B{-level}: room {room.Number}'s secure disposal is not inside "
                            + "the suite it is fitted to.");
                    }

                    foreach (SurfaceLayout.Doorway d in ways)
                    {
                        double clear = BoxToSegment(in f, d.X1, d.Y1, d.X2, d.Y2);
                        if (clear < RingOffice.DoorClearDu - 0.01)
                        {
                            wrong.Add($"  {body} B{-level}: room {room.Number}'s secure disposal is "
                                + $"{clear:F1} du from a doorway and the law wants "
                                + $"{RingOffice.DoorClearDu:F1}.");
                        }
                    }
                }
            }
        }

        Assert.True(machines > 50, $"only {machines} machine(s) were measured — this proved little.");
        Assert.True(doors > 100, $"only {doors} door(s) were measured against them.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} secure disposal(s) are in somebody's way:\n" + string.Join("\n", wrong.Take(15)));
    }

    // ── (b) ONE RECTANGLE, TWO ENDS OF THE BUILDING ───────────────────────────────────────────────────

    /// <summary>
    /// #828 · THE MACHINE AND THE BIN ARE THE SAME OBJECT.
    ///
    /// <para>This is the fixture idiom stated as a law: the suite stands ONE box and plates it, and the bin
    /// the verb feeds is READ OFF that box rather than placed a second time. If they ever come apart, the
    /// captain reads a stencil on one rectangle and feeds a bucket that is somewhere else — which is this
    /// project's third named bug class with a shredder in it.</para>
    ///
    /// <para>It also states the rung: the bin published here is the SECURE one and nothing else. Making the
    /// carve publish it as a <see cref="RipAndBin.Tier.PaperBin"/> takes this red on the tier line and takes
    /// <see cref="THE_TOP_RUNG_LeavesNothingForAnybodyToFind"/> red on the residue.</para>
    /// </summary>
    [Fact]
    public void EVERY_MACHINE_IsExactlyOneBinAndItIsTheTopRung()
    {
        var wrong = new List<string>();
        int pairs = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor,
            UndergroundComplex.Park park) in EveryBlock())
        {
            var machines = new List<RingOffice.Fixture>();
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                machines.AddRange(room.Furniture.Where(f =>
                    f.Kind == RingOffice.Fitting.SecureDisposal));
            }

            List<RipAndBin.Bin> secure =
                [.. floor.TheBins.Where(b => b.Tier == RipAndBin.Tier.SecureDisposal)];

            if (machines.Count != secure.Count)
            {
                wrong.Add($"  {body} B{-level}: {machines.Count} machine(s) on the plan and "
                    + $"{secure.Count} secure bin(s) in the verb's own list.");
                continue;
            }

            foreach (RingOffice.Fixture f in machines)
            {
                RipAndBin.Bin? found = null;
                foreach (RipAndBin.Bin b in secure)
                {
                    if (Math.Abs(b.X - f.X) < 0.001 && Math.Abs(b.Y - f.Y) < 0.001)
                    {
                        found = b;
                    }
                }
                if (found is not { } bin)
                {
                    wrong.Add($"  {body} B{-level}: the machine at ({f.X:F1}, {f.Y:F1}) has no bin standing "
                        + "on it — the stencil and the bucket are two objects.");
                    continue;
                }
                pairs++;

                // The plate is ONE string: what the plan stencils is what the verb names.
                if (!string.Equals(bin.Plate, f.Plate, StringComparison.Ordinal))
                {
                    wrong.Add($"  {body} B{-level}: the machine says \"{f.Plate}\" and the bin says "
                        + $"\"{bin.Plate}\".");
                }
                if (!string.Equals(bin.Plate, RipAndBin.PlateFor(RipAndBin.Tier.SecureDisposal),
                    StringComparison.Ordinal))
                {
                    wrong.Add($"  {body} B{-level}: the plate is not the rung's own "
                        + $"({RipAndBin.PlateFor(RipAndBin.Tier.SecureDisposal)}).");
                }

                // …and the BOX, so everything that measures this bin measures the machine and not a
                // 1.8 du bucket somebody assumed.
                if (Math.Abs(bin.HalfX - ((f.X1 - f.X0) / 2)) > 0.001
                    || Math.Abs(bin.HalfY - ((f.Y1 - f.Y0) / 2)) > 0.001)
                {
                    wrong.Add($"  {body} B{-level}: the bin publishes a "
                        + $"{2 * bin.HalfX:F1}×{2 * bin.HalfY:F1} box over a "
                        + $"{f.X1 - f.X0:F1}×{f.Y1 - f.Y0:F1} machine.");
                }

                // …and a captain can stand at it. The published spot has to be OUT of the box and inside
                // the reach, and the reach is measured to the box (#828) — which is the whole reason the
                // box is published at all.
                if (!RipAndBin.WithinReach(bin.StandX, bin.StandY, bin))
                {
                    wrong.Add($"  {body} B{-level}: the machine refuses a captain standing exactly where "
                        + "it says to stand.");
                }
                if (Math.Abs(bin.StandX - bin.X) <= bin.HalfX && Math.Abs(bin.StandY - bin.Y) <= bin.HalfY)
                {
                    wrong.Add($"  {body} B{-level}: the standing spot is INSIDE the machine.");
                }
            }
        }

        // The complaint first, the tally after — see the note in THE_MACHINE_StandsOnlyInThePremiumSuites.
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} machine(s) and bin(s) have come apart:\n" + string.Join("\n", wrong.Take(15)));
        Assert.True(pairs > 50, $"only {pairs} machine(s) were paired with a bin — this proved little.");
    }

    // ── (c) WHAT THE RUNG IS WORTH ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · THE TOP RUNG FILES THE SAME SHAPE OF FACT AND LEAVES NOTHING BEHIND.
    ///
    /// <para>Same shape, because a later arc reads these notes back and a line that changed format at the
    /// top of the ladder would be a second thing to parse: bucket first, document after, one flat clause
    /// about what was left. What differs is that clause, and it has to — three of these rungs left a thing
    /// in a room, and the fourth left nothing anywhere.</para>
    ///
    /// <para><b>Proven RED</b> by making it a paper bin: <c>CarveBins</c> script-edited to publish the
    /// machine as <c>Tier.PaperBin</c> and <c>LeavesSomethingToFind</c> to <c>tier => true</c>, which is
    /// exactly the shape of a "third bin kind" that is really the first one wearing a new plate:</para>
    /// <code>
    /// SecureDisposal: the ladder and the residue predicate disagree about what this rung is worth.
    /// </code>
    ///
    /// <para>…and <see cref="EVERY_MACHINE_IsExactlyOneBinAndItIsTheTopRung"/> goes red beside it —
    /// <c>luna B1: 2 machine(s) on the plan and 0 secure bin(s) in the verb's own list.</c></para>
    /// </summary>
    [Fact]
    public void THE_TOP_RUNG_LeavesNothingForAnybodyToFind()
    {
        // Three bets and one answer, and the answer is the rung a captain had to walk to a premium suite for.
        foreach (RipAndBin.Tier tier in RipAndBin.Ladder)
        {
            bool bet = tier != RipAndBin.Tier.SecureDisposal;
            Assert.True(bet == RipAndBin.LeavesSomethingToFind(tier),
                $"{tier}: the ladder and the residue predicate disagree about what this rung is worth.");
        }
        Assert.False(RipAndBin.LeavesSomethingToFind(RipAndBin.Tier.SecureDisposal),
            "the secure disposal leaves something for somebody to find — the one rung that closes the bet "
            + "has gone back to placing one.");
        Assert.Equal(1, RipAndBin.Ladder.Count(t => !RipAndBin.LeavesSomethingToFind(t)));

        // THE SAME SHAPE. Every rung's filed fact names its bucket and names the document, and the document
        // is named at the END of its clause (#798's own reading law, which this rung does not get to break).
        const string label = "📋 a manifest, two hands — a mention";
        foreach (RipAndBin.Tier tier in RipAndBin.Ladder)
        {
            string note = RipAndBin.DisposalNote(label, tier);
            Assert.Contains(RipAndBin.TheBin(tier), note, StringComparison.Ordinal);
            Assert.Contains(label, note, StringComparison.Ordinal);

            int after = note.IndexOf(label, StringComparison.Ordinal) + label.Length;
            Assert.True(after < note.Length && (note[after] == '.' || note[after] == ',' || note[after] == ';'),
                $"{tier}: the note goes on past the document's name without closing its clause — "
                + $"\"{note}\"");
        }

        // …and the top rung's own clause is the only one allowed to say the paper is GONE, because it is the
        // only one the captain watched.
        Assert.Contains("nothing left of it to find",
            RipAndBin.DisposalNote(label, RipAndBin.Tier.SecureDisposal), StringComparison.Ordinal);
        foreach (RipAndBin.Tier bet in RipAndBin.Ladder.Where(RipAndBin.LeavesSomethingToFind))
        {
            Assert.DoesNotContain("nothing left of it to find", RipAndBin.DisposalNote(label, bet),
                StringComparison.Ordinal);
        }
    }

    /// <summary>#828 · The rung has its own words everywhere a rung has words — the plate, the name, the
    /// bet, the hint, the key prompt — and none of them is another rung's. #798's own distinctness law,
    /// which the ladder walks by <see cref="RipAndBin.Ladder"/> and therefore already covers; this states
    /// the half that is specific to a machine you STAND AT rather than a bucket you drop into.</summary>
    [Fact]
    public void THE_TOP_RUNG_TalksLikeAMachineAndNotLikeABucket()
    {
        const RipAndBin.Tier secure = RipAndBin.Tier.SecureDisposal;

        // You do not tear anything up at this one: you feed it, and you stand there.
        Assert.DoesNotContain("Tear it up", RipAndBin.Hint(secure), StringComparison.Ordinal);
        Assert.Contains("stand", RipAndBin.Hint(secure), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("watch", RipAndBin.RippedLine("📋 a pay sheet", secure),
            StringComparison.OrdinalIgnoreCase);

        // …and the promise every rung makes, which this one may not drop: the book keeps what you dug.
        foreach (RipAndBin.Tier tier in RipAndBin.Ladder)
        {
            Assert.Contains("stays in the book", RipAndBin.Hint(tier), StringComparison.Ordinal);
        }

        // The plate is the machine's, and the plan reads the SAME string — one object, two ends of the
        // building.
        Assert.Equal(RipAndBin.PlateFor(secure), RingOffice.SecureDisposalPlate);
        Assert.Contains("SECURE DISPOSAL", RipAndBin.PlateFor(secure), StringComparison.Ordinal);
    }
}
