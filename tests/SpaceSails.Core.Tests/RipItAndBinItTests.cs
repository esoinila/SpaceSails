using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #798 · RIP IT AND BIN IT — the verb, the fixtures, and the one law the whole thing rests on.
///
/// <para>Owner, live in play (2026-08-09): <i>"Now the option is to photograph the papers and leave them
/// there… I would not like to leave them at table in canteen… we need option to destroy them by ripping and
/// binning etc?"</i></para>
///
/// <para>Everything here is a law about EVERY SITE, so it is walked over a hundred of them rather than
/// checked on one and believed — the same net <see cref="TheHiveAmenitiesTests"/> casts, for the same
/// reason: a bin that lands inside a table on one generated moon in forty is a bug nobody will ever
/// reproduce by hand.</para>
/// </summary>
public sealed class RipItAndBinItTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The scenario's own moons plus a wide net of generated ids, exactly as the amenities audit
    /// casts it. The head office is in the list on purpose: it takes no exception to any of these rules.</summary>
    private static IEnumerable<string> ManySites()
    {
        foreach (string body in new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        })
        {
            yield return body;
        }
        for (int i = 0; i < 90; i++)
        {
            yield return $"generated-moon-{i}";
        }
        yield return KaamosLore.IceMoonBodyId;
    }

    private static void AuditEveryFloor(
        Func<string, int, UndergroundComplex.FloorPlan, string?> check, string law)
    {
        var bad = new List<string>();
        int floors = 0;
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                if (check(body, level, UndergroundComplex.Build(body, level, Field)) is { } complaint)
                {
                    bad.Add($"  {body} B{-level}: {complaint}");
                }
            }
        }

        Assert.True(floors > 500, $"only {floors} floors audited — this net is not wide enough to mean much.");
        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} of {floors} floor(s) break the law: {law}");
            foreach (string line in bad.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    private static IReadOnlyList<SurfaceCollision.Segment> Segments(
        in UndergroundComplex.FloorPlan floor, in RipAndBin.Bin skip)
    {
        var segs = new List<SurfaceCollision.Segment>();
        double h = RipAndBin.HalfDu;
        foreach (SurfaceLayout.Wall w in floor.Walls)
        {
            // Everything except the bin's OWN four sides — a box is allowed to touch itself.
            bool mine = Math.Abs(w.X1 - skip.X) <= h + 0.001 && Math.Abs(w.X2 - skip.X) <= h + 0.001
                && Math.Abs(w.Y1 - skip.Y) <= h + 0.001 && Math.Abs(w.Y2 - skip.Y) <= h + 0.001;
            if (!mine)
            {
                segs.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));
            }
        }
        return segs;
    }

    // ── (a) THE FIXTURES ARE THERE, AND THEY ARE THERE FOR A STATED REASON ────────────────────────────

    /// <summary>
    /// #798 · EVERY FLOOR THAT BREATHES HAS SOMEWHERE TO PUT A PAPER — AND NO FLOOR THAT DOES NOT.
    ///
    /// <para>#775's tidy-spaces rule is what makes a bin worth having and what makes binning a BET: this
    /// building is kept by professionals, and professionals empty bins in rooms with air in them. So the
    /// pressure fact governs the fixture, and it is read from
    /// <see cref="UndergroundComplex.HoldsPressure"/> and from nowhere else — a second pressure map is how
    /// two instruments come to disagree about air.</para>
    /// </summary>
    [Fact]
    public void EVERY_FLOOR_THAT_BREATHES_HasSomewhereToPutAPaper()
    {
        AuditEveryFloor((body, level, floor) =>
        {
            bool air = UndergroundComplex.HoldsPressure(body, level);
            if (!air)
            {
                return floor.TheBins.Count == 0 ? null
                    : $"{floor.TheBins.Count} bin(s) on a floor with no air in it — nobody empties those.";
            }
            return floor.TheBins.Count > 0 ? null
                : "a floor that holds pressure and nowhere at all to put a document.";
        }, "spec — a bin is an amenity of a floor people work on (#775), and every such floor has one");
    }

    /// <summary>
    /// #798 · THE CANTEEN OFFERS BOTH RUNGS, IN THE ROOM WHERE THE CHOICE MEANS SOMETHING.
    ///
    /// <para>Owner: the clean paper bin is <i>"the worst choice wearing the safest face"</i>; the slop bin is
    /// the competent one; the chute is the better bet again. That ladder is only a CHOICE if more than one
    /// rung is inside one room — a floor with a paper bin by the lift and nothing else is a ladder with one
    /// step on it.</para>
    /// </summary>
    [Fact]
    public void THE_HALL_OffersTheSlopBinAndTheChuteInsideItsOwnRoom()
    {
        AuditEveryFloor((body, level, floor) =>
        {
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (a.Hall is not { } hall)
                {
                    continue;
                }

                var inside = new List<RipAndBin.Tier>();
                foreach (RipAndBin.Bin bin in floor.TheBins)
                {
                    if (hall.Contains(bin.X, bin.Y))
                    {
                        inside.Add(bin.Tier);
                    }
                }

                if (!inside.Contains(RipAndBin.Tier.SlopBin) || !inside.Contains(RipAndBin.Tier.Chute))
                {
                    return $"the hall ({a.Use}) holds [{string.Join(", ", inside)}] — the disposal choice "
                        + "the whole feature turns on is not in the room the captain sits in.";
                }
            }
            return null;
        }, "spec — bin CHOICE is a choice, so the canteen hall carries both the slop bin and the chute");
    }

    /// <summary>#798 · …and the park's walk has one too, because "rip and bin it in ANOTHER building" is the
    /// next rung of the tradecraft ladder and a park with no bin is a walk you cannot use for anything.</summary>
    [Fact]
    public void THE_PARK_HasABinOffTheWalk()
    {
        int parks = 0, binned = 0;
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                if (floor.Park is not { } park)
                {
                    continue;
                }
                parks++;
                foreach (RipAndBin.Bin bin in floor.TheBins)
                {
                    if (park.Contains(bin.X, bin.Y))
                    {
                        binned++;
                        break;
                    }
                }
            }
        }

        Assert.True(parks > 50, $"only {parks} park(s) walked — this net proves nothing.");
        Assert.True(binned == parks, $"{parks - binned} of {parks} park(s) have nowhere to put anything.");
    }

    // ── (b) THE DRAWN BIN AND THE COLLIDED BIN ARE ONE OBJECT ─────────────────────────────────────────

    /// <summary>
    /// #798 · WHAT IS DRAWN IS WHAT YOU WALK INTO.
    ///
    /// <para>This project's most expensive habit is the sim doing one thing while a picture reports another,
    /// and a fixture is where it happens: a renderer that laid out its own box would be one more caller doing
    /// geometry about a room it does not own. A bin's box goes into <c>FloorPlan.Walls</c>, which is the ONE
    /// list that is both drawn and collided with, so this guard asserts the four sides are actually there —
    /// if they ever stop being, the bin becomes a plate the captain walks straight through.</para>
    ///
    /// <para>#874 · <b>AND THE INSIDE OF IT IS NOT A PLACE.</b> Four sides was the whole of the law here, and
    /// four sides round 1.8 du leave exactly one lattice square in the middle that a 1.4 du captain fits on
    /// and nothing on the floor can walk to — a sealed island, in every bin in the building, invisible to
    /// every picture the game draws. So the count is now of the RAILS specifically (a side is a segment
    /// lying along an edge of the box, which is what "drawn as a thing" always meant), and the second half
    /// asks what counting sides could not: standing in the middle of a waste bin must be blocked BY THE
    /// BIN.</para>
    ///
    /// <para><b>The new half proven RED</b> by script-reverting <c>UndergroundComplex.cs</c> to 11356fe —
    /// <c>CarveBins.AddBox</c> back to its four <c>walls.Add</c> calls:</para>
    /// <code>
    /// 321 of 1154 floor(s) break the law: spec — the box the eye sees is the box the boots meet, off one
    /// list, and it is filled in
    ///   luna B1: the Chute at (1.9, -200.5) is a box the captain can STAND IN — four rails round a sealed
    ///     square, drawn solid and simulated hollow (#874).
    ///   luna B5: the PaperBin at (40.5, -155.0) is a box the captain can STAND IN — …
    ///   luna B13: the Chute at (-11.9, -93.5) is a box the captain can STAND IN — …
    /// </code>
    /// </summary>
    [Fact]
    public void THE_DRAWN_BOX_IsTheCollidedBox()
    {
        // The captain's own half-width. Core carries no client constant, so it is named here and it is what
        // DeckPlan.AvatarRadius holds — a body this wide is what makes a 1.8 du box a place to stand.
        const double captain = 0.7;

        AuditEveryFloor((_, _, floor) =>
        {
            double h = RipAndBin.HalfDu;
            foreach (RipAndBin.Bin bin in floor.TheBins)
            {
                int rails = 0;
                var own = new List<SurfaceCollision.Segment>();
                foreach (SurfaceLayout.Wall w in floor.Walls)
                {
                    bool mine = Math.Abs(w.X1 - bin.X) <= h + 0.001 && Math.Abs(w.X2 - bin.X) <= h + 0.001
                        && Math.Abs(w.Y1 - bin.Y) <= h + 0.001 && Math.Abs(w.Y2 - bin.Y) <= h + 0.001;
                    if (!mine)
                    {
                        continue;
                    }
                    own.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));

                    // A SIDE: a segment that RUNS ALONG one of the box's four edges — flat on one axis, and
                    // that axis pinned at the edge. A stroke through the middle of the mass is not a side,
                    // which is the distinction the old count could not make.
                    bool onEdge =
                        (Math.Abs(w.Y1 - w.Y2) < 0.001 && Math.Abs(Math.Abs(w.Y1 - bin.Y) - h) < 0.001)
                        || (Math.Abs(w.X1 - w.X2) < 0.001 && Math.Abs(Math.Abs(w.X1 - bin.X) - h) < 0.001);
                    if (onEdge)
                    {
                        rails++;
                    }
                }
                if (rails != 4)
                {
                    return $"the {bin.Tier} at ({bin.X:F1}, {bin.Y:F1}) has {rails} collidable side(s) — "
                        + "it is drawn as a thing and is not one.";
                }
                if (!SurfaceCollision.Blocked(bin.X, bin.Y, captain, own))
                {
                    return $"the {bin.Tier} at ({bin.X:F1}, {bin.Y:F1}) is a box the captain can STAND IN — "
                        + "four rails round a sealed square, drawn solid and simulated hollow (#874).";
                }
            }
            return null;
        }, "spec — the box the eye sees is the box the boots meet, off one list, and it is filled in");
    }

    /// <summary>
    /// #798 · A BIN IS SOMETHING YOU CAN GET TO, AND IT IS NOT STANDING IN THE ROOM'S WAY.
    ///
    /// <para>Two halves of one law, and both directions of it. The <b>standing spot</b> the carve publishes
    /// must be clear floor and inside <see cref="RipAndBin.ReachDu"/> — a bin nobody can stand at is a
    /// refusal the player cannot act on. And the bin's own box must be clear of every other wall on the
    /// floor: this project has set its captain down inside a wall twice, and both times it was a caller
    /// typing geometry about a room it did not own.</para>
    /// </summary>
    [Fact]
    public void EVERY_BIN_HasClearFloorRoundItAndSomewhereToStandAtIt()
    {
        AuditEveryFloor((_, _, floor) =>
        {
            foreach (RipAndBin.Bin bin in floor.TheBins)
            {
                IReadOnlyList<SurfaceCollision.Segment> others = Segments(floor, bin);

                if (SurfaceCollision.Blocked(bin.X, bin.Y, RipAndBin.ClearDu, others))
                {
                    return $"the {bin.Tier} at ({bin.X:F1}, {bin.Y:F1}) is inside something else.";
                }
                if (SurfaceCollision.Blocked(bin.StandX, bin.StandY, RipAndBin.StandClearDu, others))
                {
                    return $"the {bin.Tier} at ({bin.X:F1}, {bin.Y:F1}) has its standing spot in a wall.";
                }

                double reach = bin.DistanceTo(bin.StandX, bin.StandY);
                if (reach > RipAndBin.ReachDu)
                {
                    return $"the {bin.Tier}'s own standing spot is {reach:F1} du away — further than the "
                        + $"{RipAndBin.ReachDu:F1} du the verb reaches, so the game would refuse itself.";
                }
                if (!RipAndBin.WithinReach(bin.StandX, bin.StandY, bin))
                {
                    return $"the {bin.Tier} refuses a captain standing exactly where it says to stand.";
                }
            }
            return null;
        }, "spec — a bin stands clear of the room and publishes a spot a captain can actually reach it from");
    }

    /// <summary>#798 · …and no two bins on a floor are stood on top of one another, which is the same law one
    /// size up: a placer that could not see what it had already placed would fit the chute inside the slop
    /// bin on the floors where the two anchors happened to agree.</summary>
    [Fact]
    public void NO_TWO_BINS_StandInTheSamePlace()
    {
        AuditEveryFloor((_, _, floor) =>
        {
            IReadOnlyList<RipAndBin.Bin> bins = floor.TheBins;
            for (int i = 0; i < bins.Count; i++)
            {
                for (int j = i + 1; j < bins.Count; j++)
                {
                    double at = bins[i].DistanceTo(bins[j].X, bins[j].Y);
                    if (at < 2 * RipAndBin.ClearDu)
                    {
                        return $"the {bins[i].Tier} and the {bins[j].Tier} are {at:F1} du apart.";
                    }
                }
            }
            return null;
        }, "spec — two bins in one place is one bin drawn twice");
    }

    // ── (c) THE VERB ITSELF ───────────────────────────────────────────────────────────────────────────

    /// <summary>#798 · Only EVIDENCE is torn up. The pair is the same pair the field book has a gist for —
    /// paper, and a file on somebody — because "what can be read over your shoulder" and "what the book can
    /// keep" are the same question, and two answers to it is how they come to disagree.</summary>
    [Fact]
    public void THE_VERB_TakesEvidenceAndNothingElse()
    {
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            bool evidence = kind is Satchel.Kind.Paper or Satchel.Kind.Dirt;
            Assert.Equal(evidence, RipAndBin.IsEvidence(kind));
        }

        // The pair matches the book's own idea of a document, asked of the book rather than restated.
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            bool hasGist = LeftBehind.GistOf(new Satchel.Item(kind, "rip-guard-1"), "B1") is { Length: > 0 };
            Assert.True(hasGist == RipAndBin.IsEvidence(kind),
                $"{kind}: the book says gist={hasGist} and the shredder says evidence="
                + $"{RipAndBin.IsEvidence(kind)} — two answers to one question.");
        }
    }

    /// <summary>#798 · The reach is a REACH: outside it there is no bin, and inside it the better bet wins a
    /// tie. Both directions, because a predicate that only ever answers yes has not been tested.</summary>
    [Fact]
    public void THE_REACH_RefusesFromAcrossTheRoomAndPrefersTheBetterBet()
    {
        var paper = new RipAndBin.Bin(RipAndBin.Tier.PaperBin, 0, 0, 0, 2, "p");
        var chute = new RipAndBin.Bin(RipAndBin.Tier.Chute, 0, 0, 0, 2, "c");
        var far = new RipAndBin.Bin(RipAndBin.Tier.Chute, 100, 100, 100, 98, "f");

        Assert.Null(RipAndBin.NearestWithinReach(0, 0, null));
        Assert.Null(RipAndBin.NearestWithinReach(0, 0, []));
        Assert.Null(RipAndBin.NearestWithinReach(0, 0, [far]));
        Assert.Null(RipAndBin.NearestWithinReach(RipAndBin.ReachDu + 0.5, 0, [paper]));

        Assert.Equal(RipAndBin.Tier.PaperBin,
            RipAndBin.NearestWithinReach(RipAndBin.ReachDu - 0.1, 0, [paper])!.Value.Tier);

        // Dead level, two buckets at the same spot: the one a captain would actually use.
        Assert.Equal(RipAndBin.Tier.Chute,
            RipAndBin.NearestWithinReach(1, 0, [paper, chute])!.Value.Tier);
        Assert.Equal(RipAndBin.Tier.Chute,
            RipAndBin.NearestWithinReach(1, 0, [chute, paper])!.Value.Tier);

        // …and NEARER still beats better, because reaching past the bin in front of you is not a thing.
        var nearPaper = new RipAndBin.Bin(RipAndBin.Tier.PaperBin, 1, 0, 1, 2, "p");
        var farChute = new RipAndBin.Bin(RipAndBin.Tier.Chute, 3.5, 0, 3.5, 2, "c");
        Assert.Equal(RipAndBin.Tier.PaperBin,
            RipAndBin.NearestWithinReach(0, 0, [nearPaper, farChute])!.Value.Tier);
    }

    /// <summary>#798 · The ladder is ordered worst first, and the order IS the design: a caller asking "is
    /// this a better bet" compares two enum values rather than consulting a table somebody has to
    /// remember.</summary>
    [Fact]
    public void THE_LADDER_IsOrderedWorstBetFirst()
    {
        Assert.Equal(
            new[] { RipAndBin.Tier.PaperBin, RipAndBin.Tier.SlopBin, RipAndBin.Tier.Chute },
            RipAndBin.Ladder.ToArray());
        Assert.True(RipAndBin.Tier.PaperBin < RipAndBin.Tier.SlopBin);
        Assert.True(RipAndBin.Tier.SlopBin < RipAndBin.Tier.Chute);
    }

    // ── (d) THE WORDS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>#798 · Every rung says something DIFFERENT, on the plate, in the sentence and in the line the
    /// book keeps — because the tiers do exactly the same thing in this cut and the WORD on the filed fact is
    /// the entire difference a later arc has to read back.</summary>
    [Fact]
    public void EVERY_RUNG_IsToldApartInWords()
    {
        var plates = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (RipAndBin.Tier tier in RipAndBin.Ladder)
        {
            Assert.True(plates.Add(RipAndBin.PlateFor(tier)), $"{tier} shares a plate with another rung.");
            Assert.True(names.Add(RipAndBin.TheBin(tier)), $"{tier} shares its NAME with another rung.");
            Assert.NotEqual("", RipAndBin.TierBet(tier));

            // The filed fact names the bucket. This is the whole of the tier's mechanical existence today.
            Assert.Contains(RipAndBin.TheBin(tier), RipAndBin.DisposalNote("a manifest", tier),
                StringComparison.Ordinal);
            Assert.Contains(RipAndBin.TheBin(tier), RipAndBin.Hint(tier), StringComparison.Ordinal);
        }

        // The wet-bin joke lives at exactly ONE rung. Owner: "some bin that has wet stuff :-D" — a joke told
        // at three bins is not a joke, it is a tone.
        int soup = RipAndBin.Ladder.Count(t =>
            RipAndBin.TierBet(t).Contains("soup", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, soup);
        Assert.Contains("Soup", RipAndBin.TierBet(RipAndBin.Tier.SlopBin), StringComparison.Ordinal);
    }

    /// <summary>
    /// #798 · THE DOCUMENT IS NAMED AT THE END OF ITS CLAUSE, NEVER INSIDE ONE.
    ///
    /// <para><b>Found by reading it on the screen</b>, in the browser, on this build.
    /// <c>Map.SatchelLabel</c> hands these two sentences a phrase that carries its own em-dash clause —
    /// <i>"maintenance log, two hands — a mention"</i> — and the first cut wrote <i>"You tear {what} up"</i>,
    /// which rendered as <i>"You tear maintenance log, two hands — a mention up and push the pieces down"</i>.
    /// The particle was five words from its verb and the line was gibberish, in the pulse AND in the book,
    /// on the very first press of the very first demo row.</para>
    ///
    /// <para>The law that fixes it is checkable: whatever follows the label has to be punctuation that CLOSES
    /// its clause. A guard about "does it read well" cannot be written; this one can, and it is the same
    /// fault.</para>
    /// </summary>
    [Fact]
    public void THE_DOCUMENT_IsNamedAtTheEndOfItsClause()
    {
        const string label = "a manifest, two hands — a mention";

        foreach (RipAndBin.Tier tier in RipAndBin.Ladder)
        {
            foreach (string line in new[]
            {
                RipAndBin.RippedLine(label, tier), RipAndBin.DisposalNote(label, tier),
            })
            {
                int at = line.IndexOf(label, StringComparison.Ordinal);
                Assert.True(at >= 0, $"{tier}: the sentence does not name the document at all.");

                int after = at + label.Length;
                Assert.True(after < line.Length, $"{tier}: the sentence ends on the label with no stop.");

                char next = line[after];
                Assert.True(next is ',' or '.' or ':' or ';' or '—',
                    $"{tier}: the document is named mid-clause — the sentence goes on with \""
                    + line[after..Math.Min(line.Length, after + 24)]
                    + "\", which is a word the label's own clause has already run away with.");
            }
        }
    }

    /// <summary>#798 · NOTHING IN THIS FEATURE PROMISES THE PAPER IS GONE. A bin in this building is a
    /// HANDOVER (#775: professionals empty every one of them on schedule), and the discipline #649 wrote is
    /// that you find out only if it comes back. A sentence saying "destroyed", "safe" or "nobody will ever"
    /// would be the game answering a question it has not earned.</summary>
    [Fact]
    public void NOTHING_EverTellsTheCaptainTheyGotAwayWithIt()
    {
        string[] forbidden =
        [
            "destroyed", "for good", "for ever", "forever", "gone for", "nobody will ever",
            "safely", "safe now", "untraceable", "no trace", "cannot be read", "can never",
        ];

        foreach (string line in RipAndBin.AllProse())
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>#798/§13.8 · …and no plate, bin name or sentence in it explains what this place was for. The
    /// same sixteen words <c>TheHiveAmenitiesTests.NoAmenityEXPLAINSWhatThisPlaceWasFor</c> runs over the
    /// amenities, run over the fixtures this issue adds — a bin is a tempting place to break canon, because a
    /// bin is where a building throws away what it is.</summary>
    [Fact]
    public void NO_BIN_EXPLAINSWhatThisPlaceWasFor()
    {
        string[] forbidden =
        [
            "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect", "clone",
            "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
        ];

        foreach (string line in RipAndBin.AllProse())
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>#798 · Every refusal names WHAT WOULD FIX IT (#603) — a refusal a player cannot act on is a
    /// wall with a sign on it — and every witness gets their own clause, so "somebody saw you" is never the
    /// same sentence twice.</summary>
    [Fact]
    public void EVERY_REFUSAL_SaysWhatWouldFixIt()
    {
        Assert.Contains("find a bin", RipAndBin.NoBinLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stand at it", RipAndBin.NoBinLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Paper is", RipAndBin.NotEvidenceLine, StringComparison.Ordinal);

        var clauses = new HashSet<string>(StringComparer.Ordinal);
        foreach (RipAndBin.Watcher who in Enum.GetValues<RipAndBin.Watcher>())
        {
            Assert.True(clauses.Add(RipAndBin.WatcherClause(who)), $"{who} shares its clause.");
            Assert.Contains(RipAndBin.WatcherClause(who), RipAndBin.SeenLine(who), StringComparison.Ordinal);
            Assert.Contains(RipAndBin.WatcherClause(who), RipAndBin.SeenNote(who), StringComparison.Ordinal);
        }

        // …and the filed line is a FACT about what was done, never a verdict about what it bought.
        foreach (RipAndBin.Watcher who in Enum.GetValues<RipAndBin.Watcher>())
        {
            Assert.DoesNotContain("consequence", RipAndBin.SeenNote(who), StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>#798 · THE BOOK NEVER UNLEARNS, stated where it can be enforced: nothing in
    /// <see cref="RipAndBin"/> can reach a book, a note or a thread. That is not a promise about behaviour,
    /// it is a fact about the file — it has no way to take an entry away, so no future edit can accidentally
    /// give it one without this guard going red.</summary>
    [Fact]
    public void THE_SHREDDER_CannotReachTheBook()
    {
        // Read off the CODE, with the commentary stripped — the file's own docs name the notebook in the
        // sentence that promises never to touch it, and a guard that could not tell those apart would be
        // unwritable. (It shipped red on its own documentation the first time it ran.)
        string source = CodeOnly(CoreSource("RipAndBin.cs"));

        foreach (string reach in new[] { "FieldNote", "FieldNotes", "CaseThreads", "Thread(", "Erase(" })
        {
            Assert.False(source.Contains(reach, StringComparison.Ordinal),
                $"RipAndBin.cs mentions {reach} — the file that destroys evidence has grown a hand that can "
                + "reach the captain's own notes.");
        }
    }

    /// <summary>The same source with every comment taken out. The law is about what the code DOES.</summary>
    private static string CodeOnly(string source)
    {
        var kept = new StringBuilder();
        foreach (string line in source.Split('\n'))
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }
            int slashes = line.IndexOf("//", StringComparison.Ordinal);
            kept.AppendLine(slashes >= 0 ? line[..slashes] : line);
        }
        return kept.ToString();
    }

    // ── (f) #828 · THE BIN'S OWN DOOR — THE WORDS AND THE ORDER ───────────────────────────────────────

    /// <summary>
    /// #828 · WORKED SHEETS LEAD, and nothing is hidden.
    ///
    /// <para>Owner's two-tier reading law: only the dig guarantees the book kept what the sheet had, so the
    /// finished papers are the natural feed and go under the captain's thumb. The rest still come — a picker
    /// that dropped them would be the game deciding what a captain may do with their own paper.</para>
    ///
    /// <para>Swept in BOTH directions and for STABILITY, because a partition is exactly the kind of law that
    /// passes when handed a list that was already in the right order.</para>
    /// </summary>
    [Fact]
    public void THE_PICKER_LeadsWithWhatIsInTheBookAndDropsNothing()
    {
        string[] sleeve = ["glanced-a", "dug-a", "glanced-b", "dug-b", "glanced-c"];
        bool Dug(string id) => id.StartsWith("dug", StringComparison.Ordinal);

        List<string> order = RipAndBin.WorkedLeading(sleeve, Dug);

        Assert.Equal(["dug-a", "dug-b", "glanced-a", "glanced-b", "glanced-c"], order);
        Assert.Equal(sleeve.Length, order.Count);      // nothing is dropped…
        Assert.Equal([.. sleeve.OrderBy(s => s)], [.. order.OrderBy(s => s)]);   // …and nothing is invented.

        // Both ends of the ladder: all worked and none worked are the ordinary cases, and both must come
        // back in the sleeve's own order rather than reversed by a partition that only got tested mixed.
        Assert.Equal(sleeve, RipAndBin.WorkedLeading(sleeve, _ => true));
        Assert.Equal(sleeve, RipAndBin.WorkedLeading(sleeve, _ => false));
        Assert.Empty(RipAndBin.WorkedLeading<string>([], _ => true));
    }

    /// <summary>#828 · THE PICKER NAMES THE BUCKET IT IS OVER. A title reading "the bin" would be the one
    /// thing this whole ladder exists to refuse: which rung you fed is the entire mechanical difference
    /// between the three, and it is the word a later arc reads back.</summary>
    [Fact]
    public void THE_PICKER_TitleNamesTheRungTheCaptainIsStandingAt()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (RipAndBin.Tier tier in RipAndBin.Ladder)
        {
            string title = RipAndBin.PickerTitle(tier);
            Assert.True(seen.Add(title), $"{tier} shares its picker title with another rung.");
            Assert.Contains(RipAndBin.TheBin(tier).ToUpperInvariant(), title, StringComparison.Ordinal);
            Assert.Contains(RipAndBin.Glyph, title, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// #828 · THE POLITE REFUSAL, and the quiet flag that is not one.
    ///
    /// <para>The empty-sleeve line is <c>SeatedSpread.NothingToWorkLine</c>'s kin, bin-flavoured and kept to
    /// ONE sentence at the owner's word — and like every refusal in this feature it names what would fix it
    /// (#603).</para>
    ///
    /// <para>The unworked flag is the opposite thing and must stay the opposite thing: it is a WORD ON A ROW.
    /// Nothing in this file may turn it into a refusal, so the warning behind it says what it costs and never
    /// that it is not allowed.</para>
    /// </summary>
    [Fact]
    public void AN_EMPTY_SLEEVE_IsRefusedPolitelyAndAGLANCEDSheetIsNotRefusedAtAll()
    {
        Assert.Equal(1, RipAndBin.NothingToBinLine.Count(c => c == '.'));
        Assert.Contains("paper", RipAndBin.NothingToBinLine, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(SeatedSpread.NothingToWorkLine, RipAndBin.NothingToBinLine);

        // The flag is a word, not a verdict — and the warning behind it is about the BOOK, which is the only
        // thing a dig ever bought.
        Assert.DoesNotContain("cannot", RipAndBin.NotYetWorkedFlag, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cannot", RipAndBin.NotYetWorkedWarning, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("must", RipAndBin.NotYetWorkedWarning, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("book", RipAndBin.NotYetWorkedWarning, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(RipAndBin.NotYetWorkedFlag, RipAndBin.AlreadyInTheBookFlag);
    }

    /// <summary>Read one of Core's own source files, for the guards that are about SHAPE rather than
    /// behaviour. Walks up from the test binary the way the client's source guards do.</summary>
    private static string CoreSource(string file)
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null
            && !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "src", "SpaceSails.Core")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        return System.IO.File.ReadAllText(
            System.IO.Path.Combine(dir!.FullName, "src", "SpaceSails.Core", file));
    }
}
