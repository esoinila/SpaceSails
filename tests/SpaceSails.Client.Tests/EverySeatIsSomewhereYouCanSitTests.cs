using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #820 · EVERY SEAT IS SOMEWHERE YOU CAN SIT — and every seat is somewhere you can get UP from.
///
/// <para>Owner, evening playtest 2026-08-11, on a park bench: <i>"I would move the avatar on top of the
/// bench when I sit… just snap it into the correct position."</i> The snap itself is Core's coordinate
/// (guarded next door in <c>SittingSnapsYouOntoTheSeatTests</c>); what is guarded HERE is the half only the
/// client can answer, because only the client has the collision field: <b>is that coordinate somewhere a
/// body can be, and can the body get out again?</b></para>
///
/// <h3>Why the two halves are not the same question</h3>
///
/// <para>A park bench is a SOLID SEGMENT — you cannot walk through a bench, and #790 laid it as a wall on
/// purpose. So the sit puts the captain inside collision by design, and the thing that stops that being a
/// trap is the step off, which is why the first guard below asserts both directions at once: the seat is
/// blocked, the step-off is not. A guard that only checked the step-off was clear would pass just as
/// happily on a bench that was never solid in the first place, and would have proved nothing about the law
/// it is named after.</para>
///
/// <para>The other seats are the opposite shape — a stool and a chair round a top stand on floor a captain
/// could have walked to anyway — and the guard for them is that this is TRUE rather than assumed, since a
/// snap onto a chair that had ended up inside a wall would put the dot in the wall.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class EverySeatIsSomewhereYouCanSitTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>Every pressurised floor the sweep reaches, with the deck the renderer actually draws — the
    /// same call the game boots with, so the walls below are the walls a captain walks into.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.FloorPlan Floor, DeckPlan Deck)>
        EveryFloor()
    {
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }
            yield return (body, level, UndergroundComplex.Build(body, level, Field),
                HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], 0));
        }
    }

    /// <summary>Is a body of the captain's size stuck at this spot? The deck's own question, asked the way
    /// the walk asks it.</summary>
    private static bool Stuck(DeckPlan deck, double x, double y) =>
        SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, deck.CollisionField);

    /// <summary>The standoff the bench sitting steps off with — <c>Map.Bench.BenchStandoffDu</c>, which is a
    /// body's radius and a pace. Stated as what it is FOR rather than copied blind: if that constant ever
    /// changes, this guard should be the thing that notices the step-off no longer clears the plank.</summary>
    private const double StandoffDu = DeckPlan.AvatarRadius + 1.0;

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Source(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    // ── (a) THE BENCH · SOLID TO SIT ON, AND A WAY OFF IT ────────────────────────────────────────────

    /// <summary>
    /// #820 · THE PLANK REALLY IS SOLID, AND STANDING UP REALLY DOES GET YOU OFF IT.
    ///
    /// <para>Both ends of every bench in every park, against the real deck: the seat the snap lands on is
    /// inside the bench's own collision segment — which is what makes the step off necessary rather than
    /// decorative — and the square standing up hands to <c>StandCaptainAt</c> is clear ground inside the
    /// park. A captain left where they sat would be standing in the furniture, and this is the pair of
    /// assertions that says so out loud.</para>
    ///
    /// <para><b>Proven RED</b> by standing up where you sat down, which is what every seat verb in this game
    /// did until this issue — <c>StepOff</c> reading the bench end itself:</para>
    /// <code>
    /// 96 bench end(s) cannot be stood up from:
    ///   luna B1 bench 0 end 0: standing up leaves the captain at (-86.3,-248.1), inside something solid.
    ///   luna B1 bench 0 end 1: standing up leaves the captain at (-84.5,-248.1), inside something solid.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_BENCH_IsSolidToSitOnAndClearToStandUpFrom()
    {
        var wrong = new List<string>();
        int ends = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor, DeckPlan deck) in EveryFloor())
        {
            if (floor.Park is not { } park)
            {
                continue;
            }

            foreach (ParkBenches.Bench bench in ParkBenches.On(in park))
            {
                for (int end = 0; end < ParkBenches.Ends; end++)
                {
                    ends++;
                    (double sx, double sy) = bench.End(end);
                    string what = $"  {body} B{-level} bench {bench.Index} end {end}";

                    // The seat is INSIDE the plank. Said first, because everything else in this guard is
                    // only worth asserting about a seat that can trap a body.
                    if (!Stuck(deck, sx, sy))
                    {
                        wrong.Add($"{what}: the seat at ({sx:F1},{sy:F1}) is not inside the bench at all — "
                            + "either the plank stopped being solid or the ends stopped being on it, and "
                            + "the step-off below is guarding nothing.");
                    }

                    (double ox, double oy) = ParkBenches.TowardTheWalk(in park, sx, sy, StandoffDu);
                    if (Stuck(deck, ox, oy))
                    {
                        wrong.Add($"{what}: standing up leaves the captain at ({ox:F1},{oy:F1}), inside "
                            + "something solid.");
                    }
                    if (!park.Contains(ox, oy))
                    {
                        wrong.Add($"{what}: standing up puts the captain at ({ox:F1},{oy:F1}), which is "
                            + "outside the park.");
                    }
                }
            }
        }

        Assert.True(ends > 60, $"only {ends} bench end(s) were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} bench end(s) cannot be stood up from:\n" + string.Join("\n", wrong.Take(20)));
    }

    // ── (b) THE STOOLS AND THE CHAIRS · FLOOR, AND PROVED TO BE ──────────────────────────────────────

    /// <summary>
    /// #820 · A STOOL AND A CHAIR ROUND A TOP ARE GROUND A BODY CAN STAND ON.
    ///
    /// <para>These are the seats the snap treats as ordinary floor: it puts the captain on them and standing
    /// up leaves them there, because a stool stands on the hall side of the desk and a canteen top is drawn
    /// without colliding. That is a claim about the CARVE, and the whole reason the placement is allowed to
    /// skip the spawn nudge — so it is asserted rather than believed, on every counter and every top the
    /// generator lays.</para>
    ///
    /// <para><b>Proven RED</b> once per clause, because they are two different pieces of furniture. The row
    /// laid ON the counter line rather than a standoff off it (<c>stoolV = counterV</c>) —</para>
    /// <code>
    /// 88 seat(s) are somewhere a body cannot stand:
    ///   luna B1 stool 0: the seat at (18.9,-203.5) is inside something solid.
    ///   luna B1 stool 1: the seat at (27.6,-203.5) is inside something solid.
    /// </code>
    ///
    /// <para>…and the chair ring pushed out until it reaches the walls (<c>ChairRingDu = 12.0</c>), which is
    /// the cabinets going first because a cabinet is a small room —</para>
    /// <code>
    /// 124 seat(s) are somewhere a body cannot stand:
    ///   luna B1 top 6 chair 5: the seat at (15.3,-184.6) is inside something solid.
    ///   luna B1 top 11 chair 0: the seat at (89.7,-174.2) is inside something solid.
    /// </code>
    ///
    /// <para><b>And what this guard CANNOT see, stated so nobody trusts it further than it goes.</b> The
    /// first red tried was the stool row laid on the wrong side of the desk (<c>counterV +
    /// HallStoolStandoffDu</c>, the sealed band the keep works behind) and it passed green:
    /// <c>SurfaceCollision.Blocked</c> is a radius test against segments, so a seat sealed INSIDE a room
    /// reads as perfectly clear ground. Being unable to walk to a seat is a reachability question and this
    /// is a collision one. The park's own flood (<c>TheParkIsWalkableTests</c>) is where that half lives.</para>
    /// </summary>
    [Fact]
    public void EVERY_STOOL_AndEveryChairRoundATop_IsFloor()
    {
        var wrong = new List<string>();
        int stools = 0, chairs = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor, DeckPlan deck) in EveryFloor())
        {
            IReadOnlyList<(double X, double Y)> row = floor.TheStoolRow;
            for (int s = 0; s < row.Count; s++)
            {
                stools++;
                if (Stuck(deck, row[s].X, row[s].Y))
                {
                    wrong.Add($"  {body} B{-level} stool {s}: the seat at ({row[s].X:F1},{row[s].Y:F1}) is "
                        + "inside something solid.");
                }
            }

            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                foreach (CanteenRegulars.TableSeat top in CanteenRegulars.Tables(body, level, a, watch: 0))
                {
                    for (int c = 0; c < top.Seats; c++)
                    {
                        chairs++;
                        (double cx, double cy) = top.Chair(c);
                        if (Stuck(deck, cx, cy))
                        {
                            wrong.Add($"  {body} B{-level} top {top.Index} chair {c}: the seat at "
                                + $"({cx:F1},{cy:F1}) is inside something solid.");
                        }
                    }
                }
            }
        }

        Assert.True(stools > 60, $"only {stools} stool(s) were measured — this proved little.");
        Assert.True(chairs > 200, $"only {chairs} chair(s) were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} seat(s) are somewhere a body cannot stand:\n" + string.Join("\n", wrong.Take(20)));
    }

    // ── (c) THE WIRE · EVERY SIT VERB SNAPS, AND ASKS CORE WHERE ─────────────────────────────────────

    /// <summary>
    /// #820 · ALL FOUR SEATS SIT THE CAPTAIN DOWN THROUGH THE ONE PLACEMENT, ON A COORDINATE THEY ASKED CORE
    /// FOR.
    ///
    /// <para>The bug was four verbs that never moved the body at all, so the thing worth pinning is that
    /// each of them now does — through <c>SitCaptainOn</c> and never by assigning the avatar's position
    /// itself, and with an argument that came off the published furniture rather than out of a sum. A seat
    /// verb that measured its own coordinate would be §13.15's second cause, which this project has paid for
    /// twice.</para>
    ///
    /// <para><b>Proven RED</b> by putting the bench back the way the owner found it — the sitting built and
    /// the body left where it stood:</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// String:    "    private void SitOnThisBench(in UndergroundComplex.Park green, ParkBenches.Bench bench)···
    /// Not found: "SitCaptainOn("
    /// </code>
    /// </summary>
    [Fact]
    public void EVERY_SIT_VERB_PutsTheBodyOnTheSeatItAskedCoreFor()
    {
        // THE BENCH — Core picks the end, this file picks nothing.
        string bench = Region(Source("Pages", "Map.Bench.cs"), "private void SitOnThisBench(");
        Assert.Contains("bench.EndYouTake(_avatarX, _avatarY)", bench, StringComparison.Ordinal);
        Assert.Contains("SitCaptainOn(", bench, StringComparison.Ordinal);
        Assert.Contains("TowardTheWalk(in green,", bench, StringComparison.Ordinal);

        // THE STOOL — the counter's own row, by the ordinal the verb was dealt.
        string stool = Region(Source("Pages", "Map.Stool.cs"), "private void TakeAStool()");
        Assert.Contains(".TheStoolRow", stool, StringComparison.Ordinal);
        Assert.Contains("SitCaptainOn(row[seat].X, row[seat].Y)", stool, StringComparison.Ordinal);

        // THE TOPS — both presses, the ask-to-join and the free table.
        string table = Source("Pages", "Map.Table.cs");
        foreach (string verb in new[] { "private bool TryOpenTable()", "private bool TryTakeTable()" })
        {
            string press = Region(table, verb);
            Assert.Contains("top.ChairYouTake(_avatarX, _avatarY)", press, StringComparison.Ordinal);
            Assert.Contains("SitCaptainOn(sit.X, sit.Y)", press, StringComparison.Ordinal);
            Assert.Contains("StepOff = chair,", press, StringComparison.Ordinal);
        }

        // THE OFFICE CHAIR — shipped with the snap (#817) and on the shared placement with the rest.
        string chair = Region(Source("Pages", "Map.OfficeChair.cs"), "private void SitInThisChair(");
        Assert.Contains("SitCaptainOn(chair.X, chair.Y)", chair, StringComparison.Ordinal);
        Assert.Contains("StepOff = chair.StandAt,", chair, StringComparison.Ordinal);

        // …and NONE of them moves the avatar by hand. One placement for sitting down, so a fifth seat
        // cannot arrive with a fifth way of doing it.
        foreach (string file in new[] { "Map.Bench.cs", "Map.Stool.cs", "Map.Table.cs", "Map.OfficeChair.cs" })
        {
            Assert.DoesNotContain("_avatarX =", Source("Pages", file), StringComparison.Ordinal);
            Assert.DoesNotContain("_avatarX,  _avatarY) =", Source("Pages", file), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// #820 · STANDING UP GOES OUT THROUGH THE NUDGE, at the square the sitting carried.
    ///
    /// <para>This is the clause that keeps a solid seat from ever trapping the dot, and it has two halves
    /// that have to stay together: the step-off square is read off the sitting (worked out from published
    /// geometry when the captain sat down) and it is gone to through <c>StandCaptainAt</c>, which is the one
    /// placement that walks a body out of collision. Reading the right square and then assigning it directly
    /// would be a rescue nobody performs.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the step-off and leaving the captain where they sat:</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// String:    "    private void CloseTable()···
    /// Not found: "StandCaptainAt(spot.X, spot.Y"
    /// </code>
    /// </summary>
    [Fact]
    public void STANDING_UP_StepsOffThroughTheNudge()
    {
        string close = Region(Source("Pages", "Map.Table.cs"), "private void CloseTable()");
        Assert.Contains("_table?.StepOff", close, StringComparison.Ordinal);
        Assert.Contains("StandCaptainAt(spot.X, spot.Y", close, StringComparison.Ordinal);

        // …and the placement itself is the one that does NOT nudge, said where somebody changing it will
        // read it: a sit through StandCaptainAt would walk the captain back off the plank.
        string surface = Source("Pages", "Map.Surface.cs");
        string sit = Region(surface, "private void SitCaptainOn(");
        Assert.Contains("(_avatarX, _avatarY) = (x, y);", sit, StringComparison.Ordinal);
        Assert.DoesNotContain("SpawnNudge", sit, StringComparison.Ordinal);
    }

    /// <summary>One method or property, from its signature to the blank line that ends it — enough to state
    /// what a press does without pinning the prose round it.</summary>
    private static string Region(string source, string signature)
    {
        int at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"the client no longer has `{signature}`.");
        int end = source.IndexOf("\n    /// <summary>", at, StringComparison.Ordinal);
        return end > at ? source[at..end] : source[at..];
    }
}
