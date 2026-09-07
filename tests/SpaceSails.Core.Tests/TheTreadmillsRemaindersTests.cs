using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #563 slice 2 · THE TREADMILL'S REMAINDERS, each one measured.
///
/// <para>Slice 1 (#1089) made the ground an unbounded lattice of addressed tiles and left three things
/// behind: a bound that stopped a captain in silence, hut state that died with the shuttle, and tiles
/// carrying walls and nothing else. Every guard below defends one of those, and every one of them was
/// watched go RED with its fix reverted before it was allowed to stay.</para>
/// </summary>
public class TheTreadmillsRemaindersTests
{
    private static readonly string[] Bodies = ["miranda", "luna", "phobos", "europa", "titan", "ganymede"];

    private static IEnumerable<(string Body, string Salt)> Sites()
    {
        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                yield return (body, site.LayoutSalt);
            }
        }
    }

    /// <summary>The tiles the content guards sweep — the home tile and a spread of neighbours in every
    /// direction the world actually has (the lattice does not run up through the ship).</summary>
    private static IEnumerable<SurfaceTiles.Address> Spread()
    {
        for (int dx = -3; dx <= 3; dx++)
        {
            for (int dy = -3; dy <= 0; dy++)
            {
                yield return new SurfaceTiles.Address(dx, dy);
            }
        }
    }

    // ── REMAINDER 1 · THE BACKSTOP SPEAKS, ONCE ─────────────────────────────────────────────────────────

    /// <summary>THE LINE IS SAID EXACTLY ONCE, AND ONLY OUT THERE. A captain walked dead away from the tube
    /// past the bound and back again hears the suit refuse the step once — not once per frame it is leaned
    /// on, and not at all while there is still ground in front of them.
    ///
    /// <para>Walked in one-du steps rather than tested at two points, because <b>both</b> halves of this
    /// have a failure that only a walk can catch: a latch that is never set fires on every step of a
    /// boundary a captain can stand against (sixty sentences a second), and a gate read off the wrong
    /// distance fires in the middle of an ordinary excursion. The count is taken over the whole walk and
    /// asserted as a number.</para></summary>
    [Fact]
    public void TheBackstop_SaysItsLineOnceAndOnlyPastTheRadius()
    {
        double r = SurfaceTiles.BackstopRadiusDu;
        (double cx, double cy) = SurfaceTiles.TubeMouth();

        foreach ((string body, string salt) in Sites().Take(8))
        {
            for (int i = 0; i < 8; i++)
            {
                double bearing = i * Math.Tau / 8.0;
                double ux = Math.Cos(bearing), uy = Math.Sin(bearing);
                var voice = new SurfaceEdge.BackstopVoice();

                int said = 0;
                double firstAt = -1.0;
                // Out past the bound and all the way home again: the return leg is what catches a latch
                // that re-arms itself on the way back in.
                for (double d = 0.0; d <= r * 1.2; d += 1.0)
                {
                    if (voice.Step(body, salt, cx + (ux * d), cy + (uy * d)).Line is not null)
                    {
                        said++;
                        firstAt = firstAt < 0 ? d : firstAt;
                    }
                }
                for (double d = r * 1.2; d >= 0.0; d -= 1.0)
                {
                    if (voice.Step(body, salt, cx + (ux * d), cy + (uy * d)).Line is not null)
                    {
                        said++;
                    }
                }

                Assert.True(said == 1,
                    $"{body}/{salt} bearing {bearing:F2}: the suit refused a step {said} times on one " +
                    "excursion — once is a fact, twice is a nag, none is the invisible wall #563 opened with.");
                Assert.True(firstAt > r * (1.0 - SurfaceEdge.BackstopWanderFraction),
                    $"{body}/{salt} bearing {bearing:F2}: the refusal landed at {firstAt:F0} du, inside the " +
                    $"innermost the backstop can ever come ({r * (1.0 - SurfaceEdge.BackstopWanderFraction):F0} du).");
            }
        }
    }

    /// <summary>AND IT NEVER SPEAKS ON AN EXCURSION A CAPTAIN COULD SURVIVE. The point of no return bites
    /// around five thousand du; a whole walk that turns round short of the bound must be silent, or the
    /// backstop is a line the game nags about rather than one nobody meets.</summary>
    [Fact]
    public void TheBackstop_IsSilentOnEveryWalkAnybodyActuallyTakes()
    {
        double reach = SurfaceTiles.BackstopRadiusDu * (1.0 - SurfaceEdge.BackstopWanderFraction) - 1.0;
        (double cx, double cy) = SurfaceTiles.TubeMouth();

        foreach ((string body, string salt) in Sites())
        {
            var voice = new SurfaceEdge.BackstopVoice();
            for (int i = 0; i < 24; i++)
            {
                double bearing = i * Math.Tau / 24.0;
                for (double d = 0.0; d <= reach; d += 25.0)
                {
                    SurfaceEdge.BackstopVoice.Refusal step =
                        voice.Step(body, salt, cx + (Math.Cos(bearing) * d), cy + (Math.Sin(bearing) * d));
                    Assert.True(step.Line is null && !step.Beyond,
                        $"{body}/{salt}: the world declined a step {d:F0} du out, which is ground a captain " +
                        "walks. The backstop is meant to be a limit nobody meets.");
                }
            }
            Assert.False(voice.HasSpoken);
        }
    }

    /// <summary>THE LINE IS THE SUIT'S, AND IT IS FABLE'S, VERBATIM. Authored on #563 (canon pass
    /// 2026-09-03) and shipped as written — so the guard is the sentence, character for character. It also
    /// checks what the line must NOT be: it may not name a wall, an edge, a boundary or a limit, because
    /// what actually stops a captain out there is arithmetic about a tank and saying otherwise would be the
    /// game explaining its own implementation.</summary>
    [Fact]
    public void TheBackstopLine_IsTheAuthoredSentenceAndTalksOnlyAboutTheTank()
    {
        Assert.Equal(
            "The suit refuses the step. Its arithmetic is simple: from here, the tank does not reach the tube.",
            SuitAir.BackstopRefusal);

        foreach (string forbidden in new[] { "wall", "edge", "boundary", "limit", "further", "world" })
        {
            Assert.DoesNotContain(forbidden, SuitAir.BackstopRefusal, StringComparison.OrdinalIgnoreCase);
        }

        // And it is what the boundary actually hands back, rather than a constant nobody says.
        (double cx, double cy) = SurfaceTiles.TubeMouth();
        double far = SurfaceTiles.BackstopRadiusDu * 1.3;
        Assert.Equal(
            SuitAir.BackstopRefusal,
            new SurfaceEdge.BackstopVoice().Step("phobos", "", cx + far, cy).Line);
    }

    // ── REMAINDER 2 · THE HUTS SURVIVE THE SHUTTLE ──────────────────────────────────────────────────────

    /// <summary>A HUT FORCED ON ONE VISIT IS STILL FORCED ON THE NEXT — through a real save and load, which
    /// is the only version of "the next visit" that matters. The excursion is thrown away between the two
    /// halves, exactly as the shuttle throws it away.</summary>
    [Fact]
    public void AHutForcedOnOneVisit_IsStillForcedOnTheNextAndAcrossTheFile()
    {
        var tile = new SurfaceTiles.Address(-2, -3);
        string forced = GroundMemory.HutKey("phobos", "ridge-camp", tile, GroundMemory.HutChange.Forced);
        string emptied = GroundMemory.HutKey("phobos", "ridge-camp", tile, GroundMemory.HutChange.Emptied);

        // The first excursion: a hatch comes off its dogs and a locker is lifted.
        var ship = new GroundMemory();
        Assert.True(ship.Remember(forced));
        Assert.True(ship.Remember(emptied));
        Assert.False(ship.Remember(forced));   // the same hatch twice is not two hatches

        // Lift-off. Everything the visit was holding is gone; the ledger is not the visit.
        Assert.True(ship.Knows(forced));

        // …and the save. A captain who saves on the ground and loads a week later finds the same hut open.
        var saved = new Vault { Ground = new GroundSection { Changed = ship.Stored } };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(saved));
        GroundMemory back = GroundMemory.Restore(loaded.Ground?.Changed);

        Assert.True(back.Knows(forced), "the hatch was dogged again by a save/load round trip.");
        Assert.True(back.Knows(emptied), "the locker refilled itself in the file.");
        Assert.Equal(ship.Stored, back.Stored);
    }

    /// <summary>…AND FORCING ONE HUT DOES NOT OPEN EVERY HUT. The key carries the body, the site salt, the
    /// tile AND what was done, so nothing else in the world is touched by one shoulder against one hatch.
    /// This is slice 1's law re-measured on the durable ledger — the exact place it would be quietly lost,
    /// since a persisted key that collides is a bug that only shows up a week later.</summary>
    [Fact]
    public void OneHutsState_IsOnlyThatHutsState()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        int made = 0;

        foreach ((string body, string salt) in Sites())
        {
            foreach (SurfaceTiles.Address a in Spread())
            {
                foreach (GroundMemory.HutChange what in Enum.GetValues<GroundMemory.HutChange>())
                {
                    made++;
                    Assert.True(keys.Add(GroundMemory.HutKey(body, salt, a, what)),
                        $"{body}/{salt} tile ({a.X}, {a.Y}) {what}: this key is already somebody else's — " +
                        "forcing one hatch would open another moon's.");
                }
            }
        }
        Assert.Equal(made, keys.Count);

        // And the forcing of one is genuinely invisible to its neighbour.
        var ship = new GroundMemory();
        ship.Remember(GroundMemory.HutKey("phobos", "", new(0, 0), GroundMemory.HutChange.Forced));
        Assert.False(ship.Knows(GroundMemory.HutKey("phobos", "", new(0, -1), GroundMemory.HutChange.Forced)));
        Assert.False(ship.Knows(GroundMemory.HutKey("phobos", "", new(0, 0), GroundMemory.HutChange.Emptied)));
        Assert.False(ship.Knows(GroundMemory.HutKey("luna", "", new(0, 0), GroundMemory.HutChange.Forced)));
    }

    /// <summary>A FILE WRITTEN BEFORE THE LEDGER LOADS WITH EVERY HATCH DOGGED — which is the honest truth
    /// about a save that never recorded one — and a voyage that has touched nothing writes NO section at
    /// all, so the payload of an untouched vault is byte for byte what it was before this shipped.</summary>
    [Fact]
    public void AVaultFromBeforeTheLedger_LoadsWithNothingMarkedAndAnUntouchedOneWritesNothing()
    {
        Assert.Equal(0, GroundMemory.Restore(null).Count);
        Assert.Equal(0, GroundMemory.Restore([]).Count);
        Assert.False(GroundMemory.Restore(null)
            .Knows(GroundMemory.HutKey("phobos", "", new(0, 0), GroundMemory.HutChange.Forced)));

        // A vault with no ground section survives the round trip without growing one.
        Vault empty = VaultSerializer.Load(VaultSerializer.Save(new Vault()));
        Assert.Null(empty.Ground);
        Assert.DoesNotContain("\"ground\"", VaultSerializer.Save(new Vault()), StringComparison.Ordinal);

        // Rubbish rows are dropped rather than trusted: a tampered file loads as a captain who did less.
        GroundMemory tolerant = GroundMemory.Restore(["", "   ", "hut:phobos::0_0:forced"]);
        Assert.Equal(1, tolerant.Count);
    }

    // ── #316 law 1 · THE HUSKS TELL THE TALE ────────────────────────────────────────────────────────────
    //
    //  Owner, live: "If we find already shot Reevers at a site then we know that somebody else has been there
    //  to hide, pick-up, search etc :-D It serves as a clue." A clue is a thing you come BACK to, and the
    //  husks were the one mark in this game that could not be: a list on the excursion, thrown away with the
    //  visit, so the field where four Old Ones went down was clean regolith on the next landing. They are on
    //  the same ledger as everything else the captain changed now, and they carry WHEN, because a husk's
    //  whole value as evidence is its age.

    /// <summary>
    /// A HUSK LEFT ON ONE VISIT IS STILL LYING THERE ON THE NEXT — through a real save and load, which is
    /// the only version of "the next visit" that matters. The excursion is thrown away between the halves,
    /// exactly as the shuttle throws it away, and what comes back is a position and a MOMENT: the sim time it
    /// fell, so the age is knowable a month later.
    ///
    /// <para>Positions round to a hundredth of a deck unit and the moment to the second on the way in, which
    /// is deliberate — a key built out of raw doubles would come back a different husk every time the game
    /// was saved. Two Old Ones cannot stand a centimetre apart, so nothing real collides.</para>
    ///
    /// <para><b>Proven RED</b> by keeping the husks on the excursion (not writing the ledger row): the
    /// reload finds an empty field.</para>
    /// </summary>
    [Fact]
    public void AHuskLeftOnOneVisit_IsStillLyingThereOnTheNextAndAcrossTheFile()
    {
        // Two went down in a stand at the tube mouth, one out on a tile of its own, at two different hours.
        GroundMemory.Husk[] fell =
        [
            new(4.25, -8.5, 120_000.0),
            new(-2.0, -11.25, 120_000.0),
            new(SurfaceTiles.TileWidthDu * 2 + 3.5, -SurfaceTiles.TileHeightDu - 6.0, 300_000.0),
        ];

        var ship = new GroundMemory();
        foreach (GroundMemory.Husk husk in fell)
        {
            Assert.True(ship.Remember(GroundMemory.HuskKey("phobos", "RidgeCamp", husk)));
        }
        Assert.False(ship.Remember(GroundMemory.HuskKey("phobos", "RidgeCamp", fell[0])),
            "the same corpse twice is not two corpses.");

        // Lift-off, then the file. A captain who saves on the ground and loads a week later walks back into
        // the same field.
        var saved = new Vault { Ground = new GroundSection { Changed = ship.Stored } };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(saved));
        GroundMemory back = GroundMemory.Restore(loaded.Ground?.Changed);

        IReadOnlyList<GroundMemory.Husk> found = back.HusksAt("phobos", "RidgeCamp");
        Assert.Equal(fell.Length, found.Count);

        foreach (GroundMemory.Husk want in fell)
        {
            GroundMemory.Husk got = Assert.Single(found,
                h => Math.Abs(h.X - want.X) < 0.005 && Math.Abs(h.Y - want.Y) < 0.005);
            Assert.Equal(want.FellAtSimTime, got.FellAtSimTime, 0);

            // …and it is lying on the tile it fell on, which is DERIVED from the position and never a second
            // field that could disagree with it.
            Assert.Equal(SurfaceTiles.At(want.X, want.Y), got.Tile);
        }

        // NOTHING APPEARS WHERE NOTHING FELL. No seeding, no roll: another site of the same moon, another
        // moon, and a moon nobody has walked on all hold an empty field.
        Assert.Empty(back.HusksAt("phobos", "WildPlain"));
        Assert.Empty(back.HusksAt("miranda", "RidgeCamp"));
        Assert.Empty(new GroundMemory().HusksAt("phobos", "RidgeCamp"));

        // A row this build cannot parse is refused rather than guessed at — the file's own standing law.
        Assert.False(GroundMemory.TryReadHuskKey("husk:phobos:RidgeCamp:0_0:nonsense", "phobos", "RidgeCamp", out _));
        Assert.False(GroundMemory.TryReadHuskKey("hut:phobos:RidgeCamp:0_0:forced", "phobos", "RidgeCamp", out _));
        Assert.Empty(GroundMemory.Restore(["husk:phobos:RidgeCamp:0_0:4.25_junk@1"]).HusksAt("phobos", "RidgeCamp"));
    }

    /// <summary>
    /// #316 law 2 · WHAT A CAPTAIN WHO LOOKS CAN TELL. The two ends are the owner's own words — <i>"'still
    /// smoking' vs 'regolith-dusted, weeks old'"</i> — and the band is read off the SIM CLOCK against the
    /// moment in the ledger, so the sentence is a fact about the world rather than about the session.
    ///
    /// <para>THE MIDDLE AGE now has its own sentence (#316, 2026-09-03), so there is no silent band left and
    /// every husk in reach answers. Asserted verbatim at every boundary, because a threshold that selects
    /// EVERYTHING is a known bug class here — a middle line that also came back at nought seconds, or at
    /// forty days, would pass a test that only asked "is it non-null".</para>
    ///
    /// <para><b>Proven RED</b> by widening the fresh band to a week: a six-day-old husk claims to be
    /// smoking, and the middle band is never reached.</para>
    /// </summary>
    [Fact]
    public void AHusksAgeReadsOffTheSimClockAndPicksTheRightBand()
    {
        const string smoking = "Still smoking.";
        const string middling = "Dusted over. Days old.";
        const string dusted = "Regolith-dusted. Weeks old.";
        const double now = 1_000_000.0;

        GroundMemory.Husk At(double secondsAgo) => new(3.0, -5.0, now - secondsAgo);

        // FRESH — under one sim day, and right up to the boundary.
        Assert.Equal(smoking, GroundMemory.AgeLine(At(0), now));
        Assert.Equal(smoking, GroundMemory.AgeLine(At(GroundMemory.FreshWithinSeconds - 1), now));

        // THE MIDDLE — a day old to a week old, at both boundaries and in between.
        Assert.Equal(middling, GroundMemory.AgeLine(At(GroundMemory.FreshWithinSeconds), now));
        Assert.Equal(middling, GroundMemory.AgeLine(At(3 * GroundMemory.DaySeconds), now));
        Assert.Equal(middling, GroundMemory.AgeLine(At(GroundMemory.OldAfterSeconds - 1), now));

        // OLD — a week or more.
        Assert.Equal(dusted, GroundMemory.AgeLine(At(GroundMemory.OldAfterSeconds), now));
        Assert.Equal(dusted, GroundMemory.AgeLine(At(40 * GroundMemory.DaySeconds), now));

        // The bands are the sim clock's, not the wall clock's: the same husk read at three moments reads
        // three different ways, which is the whole of "recency is legible".
        GroundMemory.Husk shot = new(3.0, -5.0, now);
        Assert.Equal(smoking, GroundMemory.AgeLine(shot, now + 3_600));
        Assert.Equal(middling, GroundMemory.AgeLine(shot, now + (2 * GroundMemory.DaySeconds)));
        Assert.Equal(dusted, GroundMemory.AgeLine(shot, now + (30 * GroundMemory.DaySeconds)));

        // Three strings, all distinct, and none borrows the reserved word (worldbuilding-notes §8).
        string[] bands = [smoking, middling, dusted];
        Assert.Equal(3, bands.Distinct(StringComparer.Ordinal).Count());
        foreach (string line in bands)
        {
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase);
        }

        // And the marker is gone from the band it stood at: the line is authored, not deferred.
        Assert.DoesNotContain(
            "FABLE: line needed",
            TheCardCarriesItsOwnStoryTests.ReadRepoFile("src/SpaceSails.Core/GroundMemory.cs"),
            StringComparison.Ordinal);
    }

    /// <summary>ONE HUSK'S ROW IS ONLY THAT HUSK'S ROW. The key carries the body, the site salt, the tile,
    /// the position and the moment — so a stand at one landing site does not litter another site of the same
    /// moon, and a husk that fell on Tuesday is not the one that fell on Friday two metres away. A persisted
    /// key that collides is a bug that only shows up on the visit after next.</summary>
    [Fact]
    public void OneHusksRow_IsOnlyThatHusksRow()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        int made = 0;

        foreach ((string body, string salt) in Sites())
        {
            foreach (SurfaceTiles.Address a in Spread())
            {
                (double leftX, _, double bottomY, _) = SurfaceTiles.Rect(a);
                foreach (double when in new[] { 0.0, 86_400.0, 900_000.5 })
                {
                    foreach (double offset in new[] { 1.5, 4.25 })
                    {
                        made++;
                        Assert.True(
                            keys.Add(GroundMemory.HuskKey(body, salt,
                                new GroundMemory.Husk(leftX + offset, bottomY + offset, when))),
                            $"{body}/{salt} tile ({a.X}, {a.Y}) +{offset} at {when}: this row is already "
                            + "somebody else's — one stand would litter another moon.");
                    }
                }
            }
        }
        Assert.Equal(made, keys.Count);

        // …and the reader only ever hands back this ground's dead.
        var ship = new GroundMemory();
        ship.Remember(GroundMemory.HuskKey("phobos", "RidgeCamp", new(4.0, -8.0, 500.0)));
        ship.Remember(GroundMemory.HuskKey("phobos", "", new(4.0, -8.0, 500.0)));
        ship.Remember(GroundMemory.HuskKey("miranda", "RidgeCamp", new(4.0, -8.0, 500.0)));
        ship.Remember(GroundMemory.HutKey("phobos", "RidgeCamp", new(0, -1), GroundMemory.HutChange.Forced));

        Assert.Single(ship.HusksAt("phobos", "RidgeCamp"));
        Assert.Single(ship.HusksAt("phobos", ""));
        Assert.Single(ship.HusksAt("miranda", "RidgeCamp"));
        Assert.Equal(4, ship.Count);   // the hatch is still a hatch and never read back as a corpse
    }

    // ── REMAINDER 3 · THE TILES OUT THERE HAVE DOORS AND DRAWERS ────────────────────────────────────────

    /// <summary>ONE SALT PER TILE'S CONTENTS, AND THE HOME TILE'S IS THE SITE'S OWN. The first clause is
    /// what stops every ruin on the moon holding the same wallet; the second is what keeps the ground under
    /// the tube byte for byte the ground the game has always laid.</summary>
    [Fact]
    public void EachTilesContents_AreSeededFromThatTileAlone()
    {
        foreach ((string body, string salt) in Sites())
        {
            Assert.Equal(salt, SurfaceTiles.ContentSalt(body, salt, SurfaceTiles.Home));

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (SurfaceTiles.Address a in Spread())
            {
                Assert.True(seen.Add(SurfaceTiles.ContentSalt(body, salt, a)),
                    $"{body}/{salt} tile ({a.X}, {a.Y}) shares its contents salt with another tile — every " +
                    "ruin on the moon holds the same drawer.");
            }
        }
    }

    /// <summary>THERE IS SOMETHING OUT THERE. Owner, on the whole point of the structure generator:
    /// <i>"the idea is that we can then use those places to have supplies and clues we can find on the way
    /// to somewhere."</i> A tile a long walk from the tube has to carry buildings with doorways in them and
    /// drawers with things in them, or the lattice is a treadmill of empty rooms.
    ///
    /// <para><b>Measured across every site rather than asserted from the rarity constant</b>, because what
    /// a captain actually meets is the generator's output and not <see cref="SurfaceSalvage"/>'s nominal
    /// weighting — the placement can refuse a tile, and that refusal is part of the real rate. The bar is
    /// deliberately low (a third of far tiles), because the guard is "the world out there is furnished",
    /// not "it is furnished to four decimal places".</para></summary>
    [Fact]
    public void TilesFarFromHome_CarryDoorwaysAndThingsInDrawers()
    {
        int tiles = 0, withDoors = 0, withFinds = 0;

        foreach ((string body, string salt) in Sites())
        {
            foreach (SurfaceTiles.Address a in Spread())
            {
                if (a == SurfaceTiles.Home)
                {
                    continue;
                }
                tiles++;

                // Asked through the two functions the GROUND is actually furnished by — the same pair the
                // home tile's build calls — rather than through the raw plan. A guard that re-derives the
                // answer beside the code is a guard that can stay green while the game lays nothing.
                if (SurfaceTiles.Doors(body, salt, a).Count > 0)
                {
                    withDoors++;
                }
                if (SurfaceTiles.Drawers(body, salt, a).Count > 0)
                {
                    withFinds++;
                }
            }
        }

        Assert.True(tiles > 100, $"only {tiles} far tiles were measured — that is not a sweep.");
        Assert.True(withDoors > tiles / 3,
            $"{withDoors} of {tiles} tiles away from the tube carry a doorway. The ground out there is " +
            "thick-walled rooms with no way in.");
        Assert.True(withFinds > tiles / 3,
            $"{withFinds} of {tiles} tiles away from the tube hold anything at all. A walk that costs air " +
            "and finds nothing teaches a captain not to walk.");
    }

    /// <summary>AND WHAT IS IN THEM IS NOT ONE DRAWER COPIED OUTWARD. Two tiles must not hold the same
    /// finds in the same order, or the contents are wallpaper for exactly the reason the ground would have
    /// been (#563's whole decision) — and this is measured on the sequence, because a guard that compares
    /// COUNTS passes happily on a lattice where every ruin holds the same wallet.</summary>
    [Fact]
    public void TwoTiles_DoNotHoldTheSameDrawers()
    {
        foreach ((string body, string salt) in Sites())
        {
            var seen = new Dictionary<string, SurfaceTiles.Address>(StringComparer.Ordinal);
            int distinct = 0, counted = 0;

            foreach (SurfaceTiles.Address a in Spread())
            {
                IReadOnlyList<SurfaceTiles.Drawer> drawers = SurfaceTiles.Drawers(body, salt, a);
                if (drawers.Count == 0)
                {
                    continue;
                }

                string finds = string.Join(",", drawers.Select(d => $"{d.Index}:{(int)d.Find}"));
                counted++;
                if (seen.TryAdd(finds, a))
                {
                    distinct++;
                }
            }

            Assert.True(distinct * 2 > counted,
                $"{body}/{salt}: only {distinct} of {counted} furnished tiles hold a distinct set of finds.");
        }
    }

    /// <summary>AND THE HOME TILE IS FURNISHED EXACTLY AS IT ALWAYS WAS. The two furnishing questions moved
    /// into Core so the lattice and the tube's own ground ask one question rather than two — which is only
    /// safe if the answer at the tube is byte for byte the answer that was there before. Measured against
    /// the raw arithmetic the client used to run inline.</summary>
    [Fact]
    public void TheHomeTile_IsFurnishedTheWayItAlwaysWas()
    {
        foreach ((string body, string salt) in Sites())
        {
            SurfaceLayout.Plan plan = SurfaceLayout.For(body, SurfaceLayout.DefaultField, salt);

            IReadOnlyList<SurfaceLayout.Doorway> ways = plan.Doorways ?? [];
            IReadOnlyList<SurfaceTiles.HungDoor> hung = SurfaceTiles.Doors(body, salt, SurfaceTiles.Home);
            Assert.Equal(ways.Count, hung.Count);
            for (int i = 0; i < ways.Count; i++)
            {
                Assert.Equal(ways[i].X1, hung[i].X1, 9);
                Assert.Equal(ways[i].Y1, hung[i].Y1, 9);
                Assert.Equal(ways[i].X2, hung[i].X2, 9);
                Assert.Equal(ways[i].Y2, hung[i].Y2, 9);
                Assert.Equal(
                    DiceRule.Roll(DiceRule.Seed($"imported-door:{body}:{salt}:{i}"), 7).Face == 1,
                    hung[i].Imported);
            }

            IReadOnlyList<(double X, double Y)> centres = plan.BuildingCentres ?? [];
            var expected = new List<(int, double, double, SurfaceSalvage.Find)>();
            for (int i = 0; i < centres.Count; i++)
            {
                SurfaceSalvage.Find find = SurfaceSalvage.WhatIsInside(body, salt, i);
                if (find != SurfaceSalvage.Find.Nothing)
                {
                    expected.Add((i, centres[i].X, centres[i].Y, find));
                }
            }
            Assert.Equal(
                expected,
                SurfaceTiles.Drawers(body, salt, SurfaceTiles.Home)
                    .Select(d => (d.Index, d.X, d.Y, d.Find)).ToList());
        }
    }

    /// <summary>THE SINGLETONS STAY WHERE THEY BELONG. #1058 made the landmarks a fact about a BODY — one
    /// monolith, standing on one ground — and an unbounded lattice is precisely the machine that would
    /// quietly turn that into a wallpaper of monoliths, one per tile, for ever.
    ///
    /// <para>Measured on the tile's own ground: the authored signature (the maze, the marker stones, the
    /// ▮ THE MONOLITH plate, the scheme name) belongs to the home tile and appears on no other. This
    /// reddens the moment a tile out in the world is routed through <see cref="SurfaceLayout.For"/> with the
    /// site's salt instead of the seeded generator with its own.</para></summary>
    [Fact]
    public void TheMonolith_StandsOnTheHomeTileAndNowhereElse()
    {
        bool anywhere = false;

        foreach ((string body, string salt) in Sites())
        {
            SurfaceLayout.Plan home = SurfaceTiles.Ground(body, salt, SurfaceTiles.Home);
            bool onThisGround = home.Landmarks.Any(m => m.Label == Monolith.ConsoleLabel);
            Assert.Equal(Monolith.StandsOn(body, salt), onThisGround);
            anywhere |= onThisGround;

            foreach (SurfaceTiles.Address a in Spread())
            {
                if (a == SurfaceTiles.Home)
                {
                    continue;
                }
                SurfaceLayout.Plan plan = SurfaceTiles.Ground(body, salt, a);
                Assert.DoesNotContain(plan.Landmarks, m => m.Label == Monolith.ConsoleLabel);
                Assert.NotEqual(SurfaceLayout.MonolithScheme, plan.Scheme);
            }
        }

        Assert.True(anywhere,
            "no site in the sweep has a monolith on it at all — this guard is measuring nothing.");
    }
}
