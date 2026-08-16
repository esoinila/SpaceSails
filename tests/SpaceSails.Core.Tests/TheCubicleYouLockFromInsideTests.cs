using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #821 · THE CUBICLE YOU LOCK FROM INSIDE — the room, the catch, the round that walks past, and the tap.
///
/// <para>Owner, standing in the park on the evening of 2026-08-11: <i>"let's add toilets there.. we might
/// want to hide from guards in one toilet cubicle we lock from inside :-D"</i>, and an hour later: <i>"also a
/// function to wash hands with some film noir comment at the end ... about if we ever feel like our hands are
/// clean :-D"</i></para>
///
/// <para>Every guard below walks the REAL generator over the REAL field and is stated against a PUBLISHED
/// list — the ring, its cubicles, their own leaves, the pool — never against a coordinate typed into the
/// test. A law measured off a number this file believes about the geometry is the house's fifth bug class.</para>
/// </summary>
public sealed class TheCubicleYouLockFromInsideTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every floor that has a block on it, with the park the ring was carved round.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park,
        UndergroundComplex.FloorPlan Floor)> EveryBlock()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || !UndergroundComplex.HasParkBlock(body, level))
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            if (floor.Park is { } park)
            {
                yield return (body, level, park, floor);
            }
        }
    }

    // ── (a) THE ROOM ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY PARK BLOCK HAS EXACTLY ONE PUBLIC WASHROOM, AND IT IS A ROW OF CUBICLES AND A RUN OF BASINS.
    ///
    /// <para>Owner: <i>"The park is the building's one public ground and it has nowhere to wash your
    /// hands."</i> One per block and never two: a captain learns where the washroom on this block is, and a
    /// floor with two of them would be teaching them a room rather than a building.</para>
    ///
    /// <para><b>Proven RED</b> by returning <c>double.NaN</c> from <c>UndergroundComplex.
    /// WashroomFrontageOn</c> — no room is ever re-plated and every block reports
    /// <c>0 washroom(s) — the block's one public ground still has nowhere to wash your hands</c>.</para>
    /// </summary>
    [Fact]
    public void EVERY_BLOCK_HasOnePublicWashroomWithARowOfCubiclesAndARunOfBasins()
    {
        var wrong = new List<string>();
        int blocks = 0, cells = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, _) in EveryBlock())
        {
            if (UndergroundComplex.IsFound(body, level))
            {
                continue;   // a gallery has no plates at all (#677), so it has no plated rooms either
            }

            blocks++;
            var found = new List<UndergroundComplex.RingRoom>();
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (RingOffice.IsWashroom(in room))
                {
                    found.Add(room);
                }
            }

            if (found.Count != 1)
            {
                wrong.Add($"  {body} B{-level}: {found.Count} washroom(s) — the block's one public ground "
                    + "still has nowhere to wash your hands.");
                continue;
            }

            UndergroundComplex.RingRoom wc = found[0];
            Assert.Equal(UndergroundComplex.ParkWashroomPlate, wc.Plate);
            Assert.Equal(RingOffice.Dressing.Washroom, RingOffice.DressingFor(in wc));

            // A ROW, not a cubicle. The issue asks for two or three; the placer lays what the depth holds.
            if (wc.Cubicles.Count < 2 || wc.Cubicles.Count > RingOffice.PublicCubicles)
            {
                wrong.Add($"  {body} B{-level}: the washroom has {wc.Cubicles.Count} cubicle(s) and a row is "
                    + $"between 2 and {RingOffice.PublicCubicles}.");
            }
            cells += wc.Cubicles.Count;

            // …and somewhere to wash. The RUN is a Counter (a basin run is a worktop with holes in it), and
            // the taps along it are what the press is hung on.
            if (!wc.Furniture.Any(f => f.Kind == RingOffice.Fitting.Counter))
            {
                wrong.Add($"  {body} B{-level}: the washroom has no basin run in it.");
            }
            if (wc.Basins.Count == 0)
            {
                wrong.Add($"  {body} B{-level}: the basin run has no taps — nothing to press [E] at.");
            }

            // #818's law, kept: no floor down here is bare, and a washroom you cannot wait in is a corridor
            // with taps in it.
            if (wc.Seats.Count < 2)
            {
                wrong.Add($"  {body} B{-level}: the washroom seats {wc.Seats.Count} — there is nowhere to "
                    + "wait.");
            }

            // …and it is NOT an office. A washroom that grew the premium service tier would be the amenity
            // ladder handing one room both rungs.
            if (RingOffice.IsBigSuite(in wc)
                || wc.Furniture.Any(f => f.Kind == RingOffice.Fitting.Kitchenette
                    || f.Kind == RingOffice.Fitting.DeskBank))
            {
                wrong.Add($"  {body} B{-level}: the washroom is furnished as an office.");
            }
        }

        // The sweep ran, then the FINDING, and only then the anti-vacuity count. In that order deliberately:
        // a broken placer makes both the finding and the count go red, and a guard that reported "I saw no
        // cubicles" first would turn a red that names the fault into a shrug.
        Assert.True(blocks > 40, $"only {blocks} blocks were read — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} block(s) have no usable washroom:\n" + string.Join("\n", wrong.Take(20)));
        Assert.True(cells > 90, $"only {cells} public cubicles were read — this proved little.");
    }

    /// <summary>
    /// EVERY CUBICLE IN THE BUILDING — the public row AND the staff pair in a big suite's service strip —
    /// CARRIES ITS OWN PUBLISHED LEAF, AND ITS SEAT AND ITS STEP ARE WHERE THEY SAY THEY ARE.
    ///
    /// <para>This is the pairing #821 is actually built on. A <c>Cubicle</c> fixture is a box and the floor's
    /// doorway list holds a leaf; until <see cref="RingOffice.Stall"/> existed nothing said which leaf
    /// belonged to which box, and a lock is a fact about ONE door of ONE cell. Measured against
    /// <c>floor.Doorways</c> — the building's own list — because a leaf the floor has never heard of is a
    /// leaf nothing will draw and nothing can shut.</para>
    ///
    /// <para><b>Proven RED</b> by handing <c>lay.Cubicle</c> the cell's own <c>uHi</c> face instead of
    /// <c>uLo - DoorClearDu</c> for the step: <c>the step outside cubicle 0 is INSIDE the cell — a guard
    /// would knock from the pan</c>, on every cubicle in the game.</para>
    /// </summary>
    [Fact]
    public void EVERY_CUBICLE_HasItsOwnPublishedLeafAndASeatInsideAndAStepOutside()
    {
        var wrong = new List<string>();
        int cells = 0, publicCells = 0, staffCells = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.FloorPlan floor)
            in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                // Every Cubicle FIXTURE has a Stall, and there are exactly as many of one as the other. A
                // box without a stall is a cubicle the lock cannot find.
                int boxes = room.Furniture.Count(f => f.Kind == RingOffice.Fitting.Cubicle);
                if (boxes != room.Cubicles.Count)
                {
                    wrong.Add($"  {body} B{-level}: room {room.Number} draws {boxes} cubicle(s) and "
                        + $"publishes {room.Cubicles.Count} — #821's lock has nothing to land on.");
                }

                foreach (RingOffice.Stall cell in room.Cubicles)
                {
                    cells++;
                    if (RingOffice.IsWashroom(in room))
                    {
                        publicCells++;
                    }
                    else
                    {
                        staffCells++;
                    }

                    string what = $"  {body} B{-level}: room {room.Number}'s cubicle {cell.Index}";

                    // THE LEAF IS THE FLOOR'S OWN. Not a copy of one, not a midpoint that happens to line
                    // up — the very record the generator put in floor.Doorways.
                    if (!floor.Doorways.Contains(cell.Door))
                    {
                        wrong.Add($"{what} carries a leaf the floor has never heard of.");
                    }

                    // …and it is in the cell's own face.
                    double dx = cell.DoorX, dy = cell.DoorY;
                    if (dx < cell.X0 - 0.01 || dx > cell.X1 + 0.01
                        || dy < cell.Y0 - 0.01 || dy > cell.Y1 + 0.01)
                    {
                        wrong.Add($"{what} has a door that is not in its own box.");
                    }

                    // THE SEAT IS INSIDE. #820's snap puts the body on it, so a seat outside its own cell
                    // would sit the captain in the room with the door shut behind them.
                    if (!cell.Contains(cell.SeatX, cell.SeatY))
                    {
                        wrong.Add($"{what} has a seat outside the cell.");
                    }
                    Assert.Equal((cell.SeatX, cell.SeatY), cell.StandAt);

                    // THE STEP IS OUTSIDE. It is where a guard knocks from, and a step inside the box would
                    // put him on the pan.
                    if (cell.Contains(cell.StepX, cell.StepY))
                    {
                        wrong.Add($"{what}'s step is INSIDE the cell — a guard would knock from the pan.");
                    }

                    // …and it is a door's clearance out, on the door's own side, so he is standing where the
                    // leaf opens rather than round the back of the partition.
                    double sx = cell.StepX - cell.DoorX, sy = cell.StepY - cell.DoorY;
                    double reach = Math.Sqrt((sx * sx) + (sy * sy));
                    if (Math.Abs(reach - RingOffice.DoorClearDu) > 0.01)
                    {
                        wrong.Add($"{what}'s step is {reach:F1} du off its own leaf and the law wants "
                            + $"{RingOffice.DoorClearDu:F1}.");
                    }
                }
            }
        }

        Assert.True(publicCells > 90,
            $"only {publicCells} public cubicles were read — the row this issue is about barely ran.");
        Assert.True(staffCells > 100,
            $"only {staffCells} staff WCs were read — #817's own pair is not being checked, and they are the "
            + "cells the owner asked to have this lock land on too.");
        Assert.True(cells == publicCells + staffCells);
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} cubicle(s) are not something a lock can land on:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    // ── (b) THE CATCH ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE CATCH IS ON THE INSIDE, WHICH IS WHERE CATCHES ARE — and the plate IS the state. Both
    /// asked of Core, so a dev row, a captain's [E] and the guard that proves the law read one answer.</summary>
    [Fact]
    public void THE_CATCH_TurnsOnlyFromInsideAndThePlateIsTheState()
    {
        Assert.True(CubicleLock.MayTurnTheCatch(inside: true));
        Assert.False(CubicleLock.MayTurnTheCatch(inside: false));

        Assert.Equal(CubicleLock.OccupiedPlate, CubicleLock.PlateFor(locked: true));
        Assert.Equal(CubicleLock.VacantPlate, CubicleLock.PlateFor(locked: false));
        Assert.NotEqual(CubicleLock.VacantPlate, CubicleLock.OccupiedPlate);

        // The refusals are SENTENCES and they name what would fix it (#603) — a wall with a sign on it is
        // not a refusal a player can act on.
        Assert.Contains("inside", CubicleLock.StepInsideFirstLine, StringComparison.OrdinalIgnoreCase);
        Assert.True(CubicleLock.RefusedFromOutsideLine.Length > 30);
    }

    /// <summary>
    /// #821 · THE SENTRY'S OWN LAW: <b>IT BUYS TIME, NOT SAFETY.</b>
    ///
    /// <para>Owner: <i>"A guard who SAW you duck in knocks, then waits, then the escort line is waiting when
    /// you open the door. A guard who didn't see you walks their beat past an OCCUPIED plate without breaking
    /// stride."</i> Three assertions, and the first of them is the one that matters: <b>nothing in this game
    /// opens a locked cubicle</b>, in either state, ever.</para>
    /// </summary>
    [Fact]
    public void A_GUARD_NeverOpensALockedCubicleAndOnlyWaitsIfHeSawTheCatchTurn()
    {
        foreach (bool saw in new[] { true, false })
        {
            // NOBODY OPENS IT. Not the man who watched you, not the man who did not. It is a constant and
            // not a rule precisely so that no later edit can quietly grow a branch here.
            Assert.False(CubicleLock.OpensALockedCubicle(saw));

            // …and exactly one of the two things happens: he waits, or he walks on. Never both, never
            // neither — a door that could leave a round in no state at all would be a man standing in a
            // washroom forever.
            Assert.NotEqual(CubicleLock.WaitsAtTheDoor(saw), CubicleLock.WalksPast(saw));
        }

        Assert.True(CubicleLock.WaitsAtTheDoor(sawYouDuckIn: true));
        Assert.False(CubicleLock.WaitsAtTheDoor(sawYouDuckIn: false));
        Assert.True(CubicleLock.WalksPast(sawYouDuckIn: false));

        // He knocks ONCE and then he waits — the line says so, and it says nothing that threatens or
        // explains (§13.8's register, the one the whole patrol feature is written in).
        Assert.Contains("knuckles", CubicleLock.KnockLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("time", CubicleLock.BoughtTimeLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("safe", CubicleLock.BoughtTimeLine, StringComparison.OrdinalIgnoreCase);
    }

    // ── (c) THE LADDER'S TOP RUNG ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// INSIDE A LOCKED CUBICLE THE SPREAD IS ALWAYS ALLOWED — regardless of anything.
    ///
    /// <para>Owner: <i>"the cubicle is the one place the spread ([I]) is ALWAYS allowed — smaller than a
    /// bench, more private than an office. Half a bench is not a desk; a locked cubicle absolutely is
    /// :-D"</i></para>
    ///
    /// <para>And the rung is at the PRIVATE END of the ladder, which is what the enum says about itself.</para>
    ///
    /// <para><b>Proven RED</b> by changing the arm to <c>SeatedHud.Seat.LockedCubicle =&gt; alone</c>: the
    /// <c>alone: false</c> case refuses, and the refusal handed back is the hall table's <c>Not with
    /// somebody sitting there.</c> — a sentence about a chair opposite, said in a WC.</para>
    /// </summary>
    [Fact]
    public void THE_SPREAD_IsAlwaysAllowedInsideALockedCubicle()
    {
        foreach (bool alone in new[] { true, false })
        {
            Assert.True(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.LockedCubicle, alone));
            Assert.Null(SeatedSpread.RefusalAt(SeatedHud.Seat.LockedCubicle, alone));
        }

        // The private end of the ladder, and the enum's own claim about its ordering.
        Assert.Equal(SeatedHud.Seat.LockedCubicle, SeatedSpread.Ladder[0]);

        // The other rungs are untouched — a new arm that quietly relaxed the ladder would be the thing this
        // whole file exists to price.
        Assert.False(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.BarStool, alone: true));
        Assert.False(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.HallTable, alone: false));
        Assert.False(SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.ParkBench, alone: false));

        // …and the strip says the TRUE thing about the room. Not the cabinet's line — that one says nobody
        // is crossing the room to you, and in here somebody may already be standing at the door.
        string clause = SeatedHud.RoomClause(SeatedHud.Seat.LockedCubicle, 0.9);
        Assert.Equal(SeatedHud.LockedCubicleClause, clause);
        Assert.NotEqual(SeatedHud.RoomClause(SeatedHud.Seat.Cabinet, 0.9), clause);
        Assert.NotEqual(
            SeatedHud.SeatLabel(SeatedHud.Seat.Cabinet), SeatedHud.SeatLabel(SeatedHud.Seat.LockedCubicle));
    }

    // ── (d) THE TAP ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #821 · THE LINE POOL IS THE ISSUE'S POOL, TO THE COMMA.
    ///
    /// <para>The six lines below are transcribed from the owner's own comment on #821, where the lore was
    /// authored. They are compared element by element AND as one hash, because <i>a string drifted in copy is
    /// a joke delivered wrong</i> — and the joke is the whole feature.</para>
    ///
    /// <para>The hash is the belt on top of the braces: an element comparison against a list somebody edited
    /// in both places at once would still pass, and a hash of the whole pool would not.</para>
    /// </summary>
    [Fact]
    public void THE_WASH_POOL_IsTheIssuesPoolVerbatim()
    {
        // Transcribed from #821's comment of 2026-08-11 13:33Z. DO NOT EDIT to make a test pass: if the
        // pool in code has drifted, the pool in code is what is wrong.
        string[] authored =
        [
            "You wash your hands. The water runs grey, then clear. The water is easily satisfied.",
            "The soap does what soap does. It was not the dirt you were thinking about.",
            "You scrub past the knuckles. Whatever it is, it does not come off at a sink.",
            "Clean, says the water. The water has not read your case notes.",
            "You dry your hands on the roller towel, and somewhere a ledger stays exactly as it was.",
            "Good as new. You have met new. You know better.",
        ];

        Assert.Equal(authored.Length, CubicleLock.WashLines.Count);
        for (int i = 0; i < authored.Length; i++)
        {
            Assert.Equal(authored[i], CubicleLock.WashLines[i]);
        }

        Assert.Equal(Fingerprint(authored), Fingerprint(CubicleLock.WashLines));

        // …and the sixth is the RARE one, by the owner's ruling. It is allowed to brush the restore theme
        // and — standing canon law — it does not STATE it: nothing in it names the thing, and a card never
        // confirms what a sensor never saw.
        Assert.Equal(CubicleLock.WashLines.Count, CubicleLock.WashWeights.Count);
        for (int i = 0; i < CubicleLock.WashWeights.Count - 1; i++)
        {
            Assert.True(CubicleLock.WashWeights[i] > CubicleLock.WashWeights[^1],
                $"line {i + 1} is no commoner than the rare one — the punchline is not rare.");
        }
        foreach (string bad in new[] { "restore", "backup", "clone", "revive", "copy of you", "reever" })
        {
            Assert.DoesNotContain(bad, CubicleLock.WashLines[^1], StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>One string for a whole pool, so a comparison cannot be defeated by an edit made in two
    /// places at once. Unit separators, because a line ending in a space and the next one starting with one
    /// would otherwise hash the same as the pair the other way round.</summary>
    private static string Fingerprint(IEnumerable<string> lines) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("", lines))));

    /// <summary>
    /// THE WASH BEAT EMITS EXACTLY ONE LINE FROM THE POOL, every time, at every site, on every watch — and
    /// the sixth comes up about as rarely as the weights say it does.
    ///
    /// <para>Seeded on (site, watch, beat) like the canteen's regulars, so a basin repeats itself only the
    /// way a tired thought does: the same press on the same shift is the same sentence, and the shift turning
    /// over changes what the room has to say.</para>
    ///
    /// <para><b>Proven RED</b> by flattening <c>WashWeights</c> to <c>[1,1,1,1,1,1]</c>: the rare line lands
    /// on 16.7% of presses and the guard reports <c>the rare line came up 1673 times in 10000 — it is not
    /// rare</c>.</para>
    /// </summary>
    [Fact]
    public void THE_WASH_BEAT_EmitsOneLineFromThePoolAndTheSixthIsRare()
    {
        var counts = new int[CubicleLock.WashLines.Count];
        int presses = 0;

        foreach (string body in Sweep())
        {
            for (long watch = 0; watch < 6; watch++)
            {
                for (int beat = 0; beat < 40; beat++)
                {
                    string said = CubicleLock.WashLine(body, watch, beat);
                    presses++;

                    int which = -1;
                    for (int i = 0; i < CubicleLock.WashLines.Count; i++)
                    {
                        if (string.Equals(CubicleLock.WashLines[i], said, StringComparison.Ordinal))
                        {
                            which = i;
                        }
                    }
                    Assert.True(which >= 0, $"{body}/{watch}/{beat} said something not in the pool: {said}");
                    counts[which]++;

                    // DETERMINISTIC. Core's law: the same three facts are the same sentence, whoever asks.
                    Assert.Equal(said, CubicleLock.WashLine(body, watch, beat));
                }
            }
        }

        Assert.True(presses > 10000, $"only {presses} presses were rolled — this proved little.");

        // Every line is reachable. A pool with a line nobody ever hears is five lines and a comment.
        for (int i = 0; i < counts.Length; i++)
        {
            Assert.True(counts[i] > 0, $"line {i + 1} never came up in {presses} presses.");
        }

        // …and the sixth is rare. The bound is stated off the WEIGHTS rather than off a number typed here,
        // so retuning the hat retunes the guard with it — with slack for the die's own scatter.
        double share = (double)counts[^1] / presses;
        double want = (double)CubicleLock.WashWeights[^1] / CubicleLock.WashTickets;
        Assert.True(share < want * 1.5,
            $"the rare line came up {counts[^1]} times in {presses} ({share:P1}) — it is not rare.");
        Assert.True(share > want * 0.5,
            $"the rare line came up {counts[^1]} times in {presses} ({share:P1}) — it is unreachable in "
            + "practice, which is not the same thing as rare.");

        // The five ordinary ones share the rest evenly, which is what makes the sixth land as a punchline
        // rather than as one of six things you have heard.
        for (int i = 0; i < counts.Length - 1; i++)
        {
            Assert.True(counts[i] > counts[^1] * 2,
                $"line {i + 1} came up {counts[i]} times against the rare one's {counts[^1]}.");
        }

        // The pips tick ONCE, and once a watch. A row of four basins is a room and not an income.
        Assert.Equal(1, CubicleLock.WashPips);
        Assert.Equal(1, CubicleLock.WashPipsPerWatch);
    }

    // ── (e) THE CANON SWEEP ───────────────────────────────────────────────────────────────────────────

    /// <summary>Every new sentence is listed for the canon review, and none of them is empty or a
    /// placeholder. The house rule that makes a prose review possible at all.</summary>
    [Fact]
    public void EVERY_NEW_SENTENCE_IsListedForTheCanonSweep()
    {
        List<string> sweep = [.. CubicleLock.AllProse()];

        foreach (string line in sweep)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.DoesNotContain("TODO", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lorem", line, StringComparison.OrdinalIgnoreCase);
        }

        // The pool is swept by walking the POOL, so a seventh line added tomorrow is swept tomorrow.
        foreach (string line in CubicleLock.WashLines)
        {
            Assert.Contains(line, sweep);
        }
        Assert.Contains(CubicleLock.BoughtTimeLine, sweep);
        Assert.Contains(CubicleLock.KnockLine, sweep);
        Assert.Contains(CubicleLock.OccupiedPlate, sweep);

        // …and the room's own stencils are in RingOffice's sweep, where the rest of the ring's signage is.
        List<string> ring = [.. RingOffice.AllProse()];
        Assert.Contains(RingOffice.BasinRunPlate, ring);
        Assert.Contains(RingOffice.PublicWcPlate(1), ring);

        // §13.8: the plate says what the room IS and nothing about what the facility is for.
        foreach (string bad in new[] { "reever", "old one", "restore", "backup", "clone" })
        {
            Assert.DoesNotContain(
                bad, UndergroundComplex.ParkWashroomPlate, StringComparison.OrdinalIgnoreCase);
        }
        Assert.True(
            string.Equals(
                UndergroundComplex.ParkWashroomPlate,
                UndergroundComplex.ParkWashroomPlate.ToUpperInvariant(), StringComparison.Ordinal),
            "a stencil in this building is shouted, and this one is not.");
        Assert.False(UndergroundComplex.ParkWashroomPlate.EndsWith('.'));
    }
}
