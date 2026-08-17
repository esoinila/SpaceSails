using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #821 · A LOCKED CUBICLE, ON THE DECK THE CAPTAIN'S BOOTS ACTUALLY COLLIDE WITH.
///
/// <para>Owner, standing in the park on the evening of 2026-08-11: <i>"let's add toilets there.. we might
/// want to hide from guards in one toilet cubicle we lock from inside :-D"</i></para>
///
/// <para>Core decides what a shut catch MEANS; this is the other half, and it is the half this ground has
/// historically got wrong. A door that is shut in a sentence and open on the plan is the third named bug
/// class — and the specific shape it would take here is the nastiest available: the floor plan is regenerated
/// from scratch on every rebuild (a room searched, a bin used, a satchel opened), so a catch that is not
/// REPLAYED comes off behind the captain's shoulder while they are sitting still reading about the boots
/// outside.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ALockedCubicleBuysTimeTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(
        string body, int level, IReadOnlyCollection<string>? shut = null) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], 0, null, shut);

    /// <summary>The first park block that carries a public washroom, with one of its cubicles.</summary>
    private static (string Body, int Level, UndergroundComplex.RingRoom Room, RingOffice.Stall Cell)
        FindAPublicCubicle()
    {
        foreach (string body in new[] { "luna", "phobos", "europa", "titan", "triton" })
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || UndergroundComplex.Build(body, level, Field).Park is not { } park)
            {
                continue;
            }

            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (RingOffice.IsWashroom(in room) && room.Cubicles.Count > 0)
                {
                    return (body, level, room, room.Cubicles[0]);
                }
            }
        }

        throw new InvalidOperationException("no public cubicle anywhere in the building.");
    }

    private static bool HasWallOn(DeckPlan deck, in SurfaceLayout.Doorway leaf)
    {
        foreach (DeckPlan.Wall w in deck.Walls)
        {
            if (Math.Abs(w.X1 - (float)leaf.X1) < 0.01f && Math.Abs(w.Y1 - (float)leaf.Y1) < 0.01f
                && Math.Abs(w.X2 - (float)leaf.X2) < 0.01f && Math.Abs(w.Y2 - (float)leaf.Y2) < 0.01f)
            {
                return true;
            }
        }
        return false;
    }

    private static DeckPlan.ConsoleSpot LeafConsole(DeckPlan deck, in RingOffice.Stall cell)
    {
        double x = cell.DoorX, y = cell.DoorY;
        return deck.Consoles.Single(c =>
            c.Kind == DeckPlan.ConsoleKind.HiveCubicle
            && Math.Abs(c.X - (float)x) < 0.01f && Math.Abs(c.Y - (float)y) < 0.01f);
    }

    // ── (a) THE DOOR REFUSES ENTRY FROM THE OUTSIDE ───────────────────────────────────────────────────

    /// <summary>
    /// SHUT: A WALL ACROSS THE OPENING AND OCCUPIED ON THE PLATE. OPEN: NEITHER, AND A WAY THROUGH.
    ///
    /// <para>Both directions off ONE cell in one real washroom, so the assertions are about the same door
    /// rather than about two conveniently different ones. And the refusal is measured where it matters — on
    /// <see cref="DeckPlan.Collides"/>, the very predicate the captain's own legs are stepped through — so
    /// "the door refuses entry from outside" is a fact about the ground rather than a check somebody
    /// remembered to write at a call site.</para>
    ///
    /// <para><b>Proven RED</b> by removing the <c>walls.Add</c> from the shut branch of
    /// <c>HiveInterior.FloorDeck</c>: <i>the catch is over and a body walks straight through the leaf</i> —
    /// the version where the plate says OCCUPIED and the round strolls in anyway.</para>
    /// </summary>
    [Fact]
    public void A_SHUT_CUBICLE_IsAWallAcrossItsOwnOpeningAndAnOpenOneIsNot()
    {
        (string body, int level, _, RingOffice.Stall cell) = FindAPublicCubicle();
        string key = HiveInterior.CubicleKey(level, in cell);

        DeckPlan open = DeckFor(body, level);
        Assert.False(HasWallOn(open, cell.Door), "an open cubicle was walled up.");
        Assert.Contains(open.Doors, d =>
            Math.Abs(d.X1 - (float)cell.Door.X1) < 0.01f && Math.Abs(d.Y1 - (float)cell.Door.Y1) < 0.01f
            && !d.Locked && d.Imported);
        Assert.Equal(CubicleLock.VacantPlate, LeafConsole(open, in cell).Label);

        // …and a body can be in the doorway of it. The step outside is floor, the seat inside is floor, and
        // the leaf between them is a hole.
        Assert.False(open.Collides(cell.StepX, cell.StepY), "there is nowhere to stand outside the door.");
        Assert.False(open.Collides(cell.SeatX, cell.SeatY), "there is nowhere to stand inside the cell.");
        Assert.False(open.Collides(cell.DoorX, cell.DoorY), "an open cubicle's doorway is blocked.");

        DeckPlan shut = DeckFor(body, level, [key]);
        Assert.True(HasWallOn(shut, cell.Door), "the catch is over and there is nothing across the opening.");
        Assert.Contains(shut.Doors, d =>
            Math.Abs(d.X1 - (float)cell.Door.X1) < 0.01f && Math.Abs(d.Y1 - (float)cell.Door.Y1) < 0.01f
            && d.Locked);
        Assert.Equal(CubicleLock.OccupiedPlate, LeafConsole(shut, in cell).Label);

        // THE REFUSAL, ON THE PREDICATE THE CAPTAIN'S OWN LEGS ARE STEPPED THROUGH.
        Assert.True(shut.Collides(cell.DoorX, cell.DoorY),
            "the catch is over and a body walks straight through the leaf.");

        // …and the seat inside is still floor, because the captain is standing on it. A lock that walled up
        // the room it was protecting would trap the dot, which is the oldest bug on this ground.
        Assert.False(shut.Collides(cell.SeatX, cell.SeatY), "the catch trapped the captain in a solid.");
    }

    /// <summary>ONE CELL, NOT THE WHOLE TERRACE. Three cubicles in a row wear the same plate and the same
    /// box shape; keying the shut set on anything but the cell's own geometry would shut all of them at
    /// once, which is a washroom nobody can use and a hide that announces itself.</summary>
    [Fact]
    public void ShuttingOneCubicleDoesNotShutItsNeighbours()
    {
        (string body, int level, UndergroundComplex.RingRoom room, RingOffice.Stall first) =
            FindAPublicCubicle();
        Assert.True(room.Cubicles.Count >= 2, "the washroom is a row and this floor has one cubicle in it.");

        DeckPlan deck = DeckFor(body, level, [HiveInterior.CubicleKey(level, in first)]);

        Assert.True(HasWallOn(deck, first.Door));
        foreach (RingOffice.Stall other in room.Cubicles)
        {
            if (other.Index == first.Index)
            {
                continue;
            }
            Assert.False(HasWallOn(deck, other.Door),
                $"shutting cubicle {first.Index} shut cubicle {other.Index} as well.");
            Assert.Equal(CubicleLock.VacantPlate, LeafConsole(deck, in other).Label);
        }

        // …and the keys really are different, which is the fact the paragraph above rests on.
        Assert.Equal(
            room.Cubicles.Count,
            room.Cubicles.Select(c => HiveInterior.CubicleKey(level, in c)).Distinct(StringComparer.Ordinal)
                .Count());
    }

    // ── (b) THE ROOM IS PRESSABLE ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY CUBICLE HAS A LEAF TO PRESS AND A SEAT TO SIT ON, AND THE BASIN RUN IS ONE FIXTURE THE LENGTH
    /// OF ITSELF.
    ///
    /// <para>All three hung on CORE'S OWN coordinates (§13.15): a renderer that worked out where a cubicle
    /// door was would be doing geometry about a room it did not carve, which is the mistake this project has
    /// twice paid for by putting a captain in a wall.</para>
    ///
    /// <para>And the two presses inside one cell are far enough apart to be told apart — the leaf at the
    /// opening, the seat at the pan — which is the "there two e's are too close to each others" complaint
    /// priced in advance.</para>
    /// </summary>
    [Fact]
    public void THE_WASHROOM_HangsItsPressesOnCoresOwnCoordinates()
    {
        (string body, int level, UndergroundComplex.RingRoom room, _) = FindAPublicCubicle();
        DeckPlan deck = DeckFor(body, level);

        foreach (RingOffice.Stall cell in room.Cubicles)
        {
            DeckPlan.ConsoleSpot leaf = LeafConsole(deck, in cell);
            Assert.Equal(DeckPlan.ConsoleKind.HiveCubicle, leaf.Kind);

            // The SEAT takes the office chair's own verb and the cell's own plate — "AN OFFICE CHAIR" in a
            // WC would be the plate and the room disagreeing.
            DeckPlan.ConsoleSpot seat = deck.Consoles.Single(c =>
                c.Kind == DeckPlan.ConsoleKind.HiveOfficeChair
                && Math.Abs(c.X - (float)cell.SeatX) < 0.01f
                && Math.Abs(c.Y - (float)cell.SeatY) < 0.01f);
            Assert.Equal(cell.Plate, seat.Label);

            // Far enough apart that standing at one picks that one. NearestConsoleSpot takes the nearer, so
            // what is needed is a gap, not a separation of reaches.
            double dx = cell.SeatX - cell.DoorX, dy = cell.SeatY - cell.DoorY;
            Assert.True(Math.Sqrt((dx * dx) + (dy * dy)) > DeckPlan.AvatarRadius * 2,
                "the leaf and the seat are on top of each other — one press, two verbs, no way to choose.");
        }

        // THE BASIN RUN: one console with a LENGTH on it, #791's E-bus, so the whole length of the porcelain
        // answers rather than one tap somebody has to find. (#827 · a run is its two ENDS now, not a
        // half-span about the plate — the counter's plate stopped standing at the middle of its own desk.)
        DeckPlan.ConsoleSpot basins = deck.Consoles.Single(
            c => c.Kind == DeckPlan.ConsoleKind.HiveBasin);
        Assert.Equal(RingOffice.BasinRunPlate, basins.Label);
        Assert.True(basins.IsRun, "the basin run is a dot — a row of four taps with one press in the middle.");
        (float bx0, float by0) = basins.End0;
        (float bx1, float by1) = basins.End1;
        Assert.True(Math.Abs(bx1 - bx0) + Math.Abs(by1 - by0) > 1.0f,
            "the basin run has no length — a row of four taps with one press in the middle of it.");

        // …and it reaches every tap Core published, which is what the length is FOR.
        foreach (RingOffice.Basin tap in room.Basins)
        {
            Assert.True(
                deck.NearestConsoleSpot(tap.X, tap.Y) is { Kind: DeckPlan.ConsoleKind.HiveBasin },
                $"tap {tap.Index} is not on the run's own E-bus.");
        }
    }

    // ── (c) THE PRESS AND THE ROUND, AS WIRED ─────────────────────────────────────────────────────────
    //
    // Source-text guards, the idiom this project already uses for laws that live in an ordering rather than
    // in a value. They are cheap and they are exact, and each one names a bug that is invisible in a diff.

    private static string Source(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    /// <summary>#870 lane 6c · Re-PATHED, never re-asserted. The seat's verbs moved onto
    /// <c>Map.Seating</c> behind <c>ISeatHost</c>, so the chair is two files now — the page's half and the
    /// seat's own — read here concatenated in that order, which is exactly the text this guard read before
    /// the move.</summary>
    private static string Seat(string page, string seat) =>
        Source(page) + Source(Path.Combine("Seating", seat));

    /// <summary>#870 · The round is six partials by subject now, so the page this guard reads is all six —
    /// concatenated in the order the one file laid them out, which is exactly the text it read before the
    /// split. A part not named here is appended rather than dropped, so nothing can go unread.</summary>
    private static string Patrol()
    {
        string dir = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages");
        string[] order =
        [
            // #870 lane 6′c · RE-PATHED. The verbs moved onto Patrol's own partials, so the page's half
            // is four files: Map.Patrol.Round.cs and Map.Patrol.Escort.cs had no caller outside the family
            // to forward to and are gone. The count is still asserted, so a fifth part cannot go unread.
            "Map.Patrol.cs", "Map.Patrol.Hide.cs", "Map.Patrol.Challenge.cs", "Map.Patrol.Run.cs",
            "Map.PatrolHost.cs",
        ];
        // #870 lane 6′b · RE-PATHED, never re-asserted. The round's twenty-two fields and the Guard they
        // are made of moved into Pages/Patrol/, so the page this guard reads is EIGHT files now — the six
        // verbs first, then the state. A part not named here is still appended rather than dropped.
        return string.Concat(
            Directory.EnumerateFiles(dir, "Map.Patrol*.cs")
                .OrderBy(p => Array.IndexOf(order, Path.GetFileName(p)) switch
                {
                    < 0 => int.MaxValue,
                    var i => i,
                })
                .Concat(Directory.EnumerateFiles(Path.Combine(dir, "Patrol"), "*.cs")
                    .OrderBy(p => p, StringComparer.Ordinal))
                .Select(File.ReadAllText));
    }

    /// <summary>
    /// The same file with every comment line taken out.
    ///
    /// <para><b>Learned inside this very issue and worth the paragraph.</b> The ordering guard below was
    /// written against the raw text and it was GREEN on the broken code: the doc-comment above the method
    /// names <c>PatrolBeat.Notices</c>, so the first occurrence in the file was a sentence ABOUT the call
    /// rather than the call, and the sightline could be moved after the rebuild without the guard noticing.
    /// A source guard that reads prose is the house's fifth bug class exactly — the assertion was right and
    /// the world it was handed could not tell pass from fail. Every text guard here reads CODE.</para>
    /// </summary>
    private static string Code(string file) => CodeOf(Source(file));

    /// <summary>The same, for a text that is already in hand rather than a filename.</summary>
    private static string CodeOf(string text)
    {
        var kept = new List<string>();
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.TrimStart();
            if (!trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                kept.Add(line);
            }
        }
        return string.Join("\n", kept);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary.");
    }

    /// <summary>
    /// WHO SAW YOU IS ASKED BEFORE THE PARTITION GOES ACROSS THE OPENING.
    ///
    /// <para>THE bug this feature could have shipped with, and it would have been silent: the shut leaf is a
    /// wall, so a sightline asked AFTER the rebuild is a sightline through a wall, and every guard in the
    /// building would answer <i>no, I did not see you</i> through the door they had just watched you shut.
    /// The hide would then always work, the knock would never happen, and every test about the guard's own
    /// predicate would still be green — the fifth bug class exactly (a guard handed the wrong world).</para>
    ///
    /// <para>It is an ORDERING, so it is checked as one.</para>
    /// </summary>
    [Fact]
    public void WHO_SAW_YOU_IsAskedBeforeTheDoorBecomesAWall()
    {
        string src = Code("Map.Cubicle.cs");

        // #870 lane 6′a · RE-NEEDLED, never re-asserted. The bit that gets written is a guard's, so the
        // sightline moved into the round itself (RememberWhoWatchedTheCatchGoOver, Map.Patrol.Hide.cs). The
        // ORDER is still the press's, and it is still the whole law: the ASK is what must come first.
        int asked = src.IndexOf("RememberWhoWatchedTheCatchGoOver", StringComparison.Ordinal);
        int shutIt = src.IndexOf("ex.CubiclesShut.Add", StringComparison.Ordinal);
        int rebuilt = src.IndexOf("RebuildSurfaceDeck", StringComparison.Ordinal);

        Assert.True(asked > 0, "nothing in the press asks who was looking.");
        Assert.True(shutIt > asked,
            "the catch goes over before anybody is asked whether they saw it — every guard would then be "
            + "asked through the partition, and the answer through a partition is always no.");
        Assert.True(rebuilt > asked, "the deck is rebuilt before the sightline is taken.");

        // …and it is the SAME predicate the challenge is gated on, not a second opinion about who sees what.
        string round = CodeOf(Patrol());
        Assert.Contains("PatrolBeat.Notices", round, StringComparison.Ordinal);
        Assert.Contains("g.SawYouShutIt = true", round, StringComparison.Ordinal);
        Assert.Contains("SightBlockers()", src, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE SENTRY'S OWN LAW IS WIRED THE WAY IT IS WRITTEN: nobody new notices you through a shut door, the
    /// man who saw you waits at it, and OPENING it is what raises the card.
    ///
    /// <para>Owner: <i>"A guard who SAW you duck in knocks, then waits, then the escort line is waiting when
    /// you open the door."</i> The challenge road is #833's own walk-up and there is deliberately no second
    /// one — a card raised from inside the hide would be a man reading a wallet through a partition.</para>
    /// </summary>
    [Fact]
    public void THE_HIDE_StopsTheHailAndTheOpeningRaisesTheCard()
    {
        string patrol = CodeOf(Patrol());
        string press = Code("Map.Cubicle.cs");

        // Nobody NEW notices you while the catch is over.
        Assert.Contains("if (hide is null)", patrol, StringComparison.Ordinal);
        Assert.Contains("StopTheRoundIfAnybodySeesYou(sight);", patrol, StringComparison.Ordinal);

        // The man who saw you goes and stands at the cell's PUBLISHED step square — not a coordinate the
        // client measured for itself.
        // #870 lane 6′d · RE-SPELLED, never re-asserted. The predicate that picks the door arm is on the MAN
        // now (Guard.DoingThisFrame), where the other six are, so it has lost its receiver and nothing else.
        Assert.Contains("CubicleLock.WaitsAtTheDoor(SawYouShutIt)", patrol, StringComparison.Ordinal);
        Assert.Contains("cell.StepX", patrol, StringComparison.Ordinal);
        Assert.Contains("CubicleLock.KnockLine", patrol, StringComparison.Ordinal);

        // …and nothing on the round opens it. There is no CALL of OpensALockedCubicle anywhere, because it
        // is a constant false and there is nothing to call; nothing anywhere in the round takes a single
        // cubicle off the shut set; and the WALKING half of the file — everything from AdvancePatrol on —
        // only READS that set and never writes to it. That last clause is what makes "no guard ever opens a
        // locked cubicle" a fact about the code rather than a branch somebody could add without noticing.
        Assert.DoesNotContain("OpensALockedCubicle(", patrol, StringComparison.Ordinal);
        Assert.DoesNotContain("CubiclesShut.Remove", patrol, StringComparison.Ordinal);

        // #870 lane 6′c · RE-SPELLED, never re-asserted: the per-frame loop is a member of the round now
        // and the page keeps a one-line forwarder of the same name, which appears EARLIER in this
        // concatenation. Cutting at the forwarder would have put every claim below on the wrong side of
        // the split — so the needle names the one that has a body.
        int loop = patrol.IndexOf("public void AdvancePatrol", StringComparison.Ordinal);
        Assert.True(loop > 0, "the per-frame loop is not where this guard thinks it is.");
        foreach (string write in new[] { "CubiclesShut.Add", "CubiclesShut.Remove", "CubiclesShut.Clear" })
        {
            Assert.DoesNotContain(write, patrol[loop..], StringComparison.Ordinal);
        }

        // The one WRITE there is lives in the floor-change spawn, where a catch dies with the floor the
        // hand that turned it has ridden away from.
        Assert.Contains("ex.CubiclesShut.Clear()", patrol[..loop], StringComparison.Ordinal);

        // THE CARD FIRES ON OPENING, through the one approach the floor already has.
        Assert.Contains("ex.CubiclesShut.Remove(key)", press, StringComparison.Ordinal);
        Assert.Contains("TheHail(him)", press, StringComparison.Ordinal);

        // …and EVERY guard forgets on the way out, not just the one who knocked. A second man who also
        // watched the catch go over would otherwise keep the bit and be standing outside the NEXT cubicle
        // the captain shut, having seen nothing at all.
        //
        // #870 lane 6′a · RE-NEEDLED, never re-asserted. The loop moved into the round as one verb
        // (EverybodyForgetsTheCatch — Pages/Patrol/Patrol.cs since 6′b), so the press is read for the ASK
        // the SWEEP — cut to that one method's body, because the same two lines exist elsewhere in the
        // family for another reason and a whole-file search would pass on them.
        Assert.Contains("EverybodyForgetsTheCatch()", press, StringComparison.Ordinal);

        int forgets = patrol.IndexOf(
            "public Guard? EverybodyForgetsTheCatch()", StringComparison.Ordinal);
        Assert.True(forgets > 0, "the forgetting is not where this guard thinks it is.");
        string forgetting = patrol[forgets..patrol.IndexOf("\n        }", forgets, StringComparison.Ordinal)];
        Assert.Contains("g.SawYouShutIt = false;", forgetting, StringComparison.Ordinal);
        Assert.Contains("foreach (Guard g in Guards)", forgetting, StringComparison.Ordinal);

        // #835 · …AND THE DOOR MAY NOT DOWNGRADE A RUN INTO A REQUEST FOR PAPERS. A man who had called it
        // in and was coming at a run is still that man — the door stopped his legs, it did not un-say the
        // radio. Hailing him on the way out would hand a captain a way to turn a chase into a polite
        // inspection by shutting a door on it, which is the door buying SAFETY, and that is the one thing
        // this feature may never do.
        Assert.Contains("if (!him.AfterYou)", press, StringComparison.Ordinal);
        Assert.DoesNotContain("him.AfterYou = false", press, StringComparison.Ordinal);
        Assert.DoesNotContain("g.AfterYou = false", press, StringComparison.Ordinal);

        // …and the round's own chase branch is BELOW the wait, so a man coming at a run cannot run through a
        // partition: he arrives, and then he is a man standing outside a door.
        //
        // #870 · RE-NEEDLED, never re-asserted. The `if / else if` chain is a LIST now
        // (`TheOneThingHeIsDoingThisFrame`), so the two lines this ordering lives on are the two arms' own
        // calls in that list rather than the two clauses they used to be. Same law, same two things, in the
        // same order — and it is pinned a second way now: the fingerprint case "he called it in, and then
        // you shut a door in his face" (`EveryRoundFingerprintsTheSameTests`) exists precisely because it is
        // the one case in the game where both arms want the same man, and it goes RED when these two lines
        // are swapped.
        //
        // #870 lane 6′d · RE-NEEDLED once more. The list is a SWITCH over the seven states, so the two lines
        // the ordering lives on are the two arms' case labels — and the order is pinned a THIRD way now, in
        // Guard.DoingThisFrame itself, where `Doing.AtTheDoor` is tested before `Doing.AfterYou`.
        int picksTheDoor = patrol.IndexOf("? Doing.AtTheDoor", StringComparison.Ordinal);
        int picksTheRun = patrol.IndexOf("? Doing.AfterYou", StringComparison.Ordinal);
        Assert.True(picksTheDoor > 0 && picksTheRun > picksTheDoor,
            "the man reports himself as coming at a run before he reports himself as standing at a door, so "
            + "the switch below could never reach the door arm at all.");

        int waits = patrol.IndexOf(
            "case Guard.Doing.AtTheDoor: HeIsWaitingOutsideTheDoorHeSawYouShut(", StringComparison.Ordinal);
        int runs = patrol.IndexOf("case Guard.Doing.AfterYou: HeIsComingAfterYou(", StringComparison.Ordinal);
        Assert.True(waits > 0 && runs > 0, "the hide and the run are not where this guard thinks they are.");
        Assert.True(waits < runs,
            "the run is taken before the wait, so a guard at a locked door would keep running at it — and "
            + "PatrolBeat.HasYou would land a hand on an arm through a partition.");
        Assert.Contains("CubicleLock.OpenedOnHimLine", press, StringComparison.Ordinal);
    }

    /// <summary>
    /// #821 · THE ROUND DOES NOT CARVE A FLOOR EVERY FRAME.
    ///
    /// <para>The hide asks "is the captain shut in?" once a frame, of a list that comes off
    /// <c>UndergroundComplex.Build</c> — which carves a whole floor, allocating a dozen lists and several
    /// hundred wall segments. This repo runs in WASM, where a Debug build is a hundred times slower than the
    /// machine it is written on, and a per-frame carve is the shape of hitch #832 was paid for. It is also
    /// completely invisible to every other test in the repository: the code compiles, the answer is right,
    /// and the frame simply takes longer.</para>
    ///
    /// <para>So the read is CACHED per (site, level) — the two facts the generator is pure in — and the
    /// cache is checked before the carve, which is an ordering and is checked as one.</para>
    /// </summary>
    [Fact]
    public void THE_HIDE_ReadsTheFloorsCubiclesOnceAndKeepsThem()
    {
        string src = Code("Map.Cubicle.cs");

        int cached = src.IndexOf("_seating.CubicleFloorKey, key", StringComparison.Ordinal);
        int carved = src.IndexOf("UndergroundComplex.Build", StringComparison.Ordinal);

        Assert.True(cached > 0, "nothing caches the floor's cubicles — the round carves a floor every frame.");
        Assert.True(carved > cached,
            "the floor is carved before the cache is consulted, which is a carve per frame in WASM.");

        // …and the key is the two facts the generator is pure in, and nothing about the state of any door —
        // a cache keyed on a door would be rebuilt by the very act it exists to make cheap.
        Assert.Contains("$\"{ex.Stop.Body.Id}|{ex.Floor}\"", src, StringComparison.Ordinal);
    }

    /// <summary>THE LADDER READS THE CATCH, NOT A FLAG CAPTURED WHEN THE CAPTAIN SAT DOWN. A captain can sit
    /// down in an open cubicle, reach back and turn the catch, and the spread has to become allowed on that
    /// very frame — so the seat carries the cell's KEY and the rung is decided by asking the one set of shut
    /// doors the deck itself is rebuilt from.</summary>
    [Fact]
    public void THE_LADDER_AsksTheCatchEveryFrame()
    {
        string seated = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Seating", "Seating.Seated.cs"));

        Assert.Contains("CubicleIsShut(wc) ? SeatedHud.Seat.LockedCubicle", seated, StringComparison.Ordinal);

        // …and an UNLOCKED cubicle is not dealt the top rung. It is not the cabinet's flag either — a
        // cabinet's door was paid for and this one is a catch on a partition.
        string chair = CodeOf(Seat("Map.OfficeChair.cs", "Seating.OfficeChair.cs"));
        Assert.Contains("Quiet = false", chair, StringComparison.Ordinal);
        Assert.Contains("CubicleKey = HiveInterior.CubicleKey", chair, StringComparison.Ordinal);
    }
}
