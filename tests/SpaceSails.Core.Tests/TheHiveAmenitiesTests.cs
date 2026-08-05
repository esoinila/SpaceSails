using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #707 · THE HIVE HAS NO TOILETS.
///
/// <para>Owner, the morning after walking a clinic: <i>"all the secret labs dont have any cantina / bar nor
/// any toilets. We should add those like to the most top most pressurized floor. The toilets should have
/// like bathroom level equipments and the high level important rooms would have their built in bathrooms
/// and be pressurized."</i></para>
///
/// <para>And then, ruling on what the canteen IS (2026-08-05): two of them, with inverted economics. The
/// upper bar is <i>"publicly accessible and just happens to be in the secret base"</i> — loose security by
/// design, because it is the compromise that keeps deliveries coming to a need-to-know site. The deep staff
/// cantina is machines and no bottles and a room where every face is known. This file builds the ROOMS;
/// the social layer is its own issue.</para>
///
/// <para>Everything here is a law about EVERY SITE, so it is walked over a hundred of them rather than
/// checked on one and believed. The rules, in the order they matter:</para>
/// <list type="number">
/// <item>every clandestine site has exactly one upper canteen and at least one washroom, on the topmost
/// floor that holds pressure, and nowhere else;</item>
/// <item>every site deep enough to have a second pressurised floor the directory admits to has exactly one
/// staff canteen, on the deepest one;</item>
/// <item><b>nothing is ever plumbed on a floor that cannot breathe</b> — the pressure fact comes from
/// <see cref="UndergroundComplex.HoldsPressure"/> and from nowhere else, because a second pressure map is
/// how two instruments come to disagree about air;</item>
/// <item>an en-suite hangs off a principal room and nothing else, and rank is therefore readable in
/// plumbing without one word of narration.</item>
/// </list>
/// </summary>
public sealed class TheHiveAmenitiesTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The scenario's own moons plus a wide net of generated ids — the ten prove the game people
    /// play, the ninety prove the GENERATOR, which is what a law about "every clandestine site" is about.
    /// The head office is in the list on purpose: it takes no exception to any of these rules and answers
    /// every one of them in its own vocabulary (#411).</summary>
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

    private static int Count(UndergroundComplex.FloorPlan floor, UndergroundComplex.Comfort use) =>
        floor.Amenities.Count(a => a.Use == use);

    [Fact]
    public void EverySiteHasABarAndAWashroomOnTheTopFloorThatHoldsPressure()
    {
        // The owner's own sentence, as an absolute: "We should add those like to the most top most
        // pressurized floor". One canteen — two would be a chain — and at least one washroom, on every
        // clandestine site in the system including the one under the ice.
        var bad = new List<string>();
        int sites = 0;
        foreach (string body in ManySites())
        {
            sites++;
            int? top = UndergroundComplex.TopPressurisedFloor(body);
            if (top is not { } level)
            {
                bad.Add($"  {body}: no floor of this building holds pressure at all.");
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            int bars = Count(floor, UndergroundComplex.Comfort.UpperCanteen);
            int heads = Count(floor, UndergroundComplex.Comfort.Washroom);
            if (bars != 1 || heads < 1)
            {
                bad.Add($"  {body} B{-level}: {bars} canteen(s) and {heads} washroom(s) — wanted 1 and 1+.");
            }
        }

        Assert.True(sites > 50, $"only {sites} sites audited.");
        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} of {sites} site(s) have nowhere to eat and nowhere to wash:");
            foreach (string line in bad.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    [Fact]
    public void TheBarIsOnTheTOPFloorAndTheMessIsOnTheDEEPESTOneTheDirectoryAdmitsTo()
    {
        // The inversion is the design (owner, 2026-08-05): the bar is where strangers come off the surface,
        // and the mess has to be as far from it as the building goes, or "a face that does not belong in a
        // room where every face is known" is a matter of taste rather than a fact about the room.
        //
        // And the mess is on a LISTED floor. The band nobody listed has no department, no livery and no
        // plate — the absence is #592's whole tell — and a canteen sign down there would be the building
        // admitting to a floor in the one place it must not.
        AuditEveryFloor((body, level, floor) =>
        {
            bool top = UndergroundComplex.TopPressurisedFloor(body) == level;
            bool mess = UndergroundComplex.StaffCanteenFloor(body) == level;

            int bars = Count(floor, UndergroundComplex.Comfort.UpperCanteen);
            int messes = Count(floor, UndergroundComplex.Comfort.StaffCanteen);
            int heads = Count(floor, UndergroundComplex.Comfort.Washroom);

            if (bars != (top ? 1 : 0))
            {
                return $"{bars} upper canteen(s) on a floor that is {(top ? "" : "not ")}the top one.";
            }
            if (messes != (mess ? 1 : 0))
            {
                return $"{messes} staff canteen(s) on a floor that is {(mess ? "" : "not ")}the deep one.";
            }
            if (!top && heads > 0)
            {
                return $"{heads} washroom(s) on a floor that is not the top one.";
            }
            if (UndergroundComplex.IsUnlisted(body, level) && floor.Amenities.Count > 0)
            {
                return "an amenity on a floor the building does not admit to having.";
            }
            return null;
        }, "spec — one bar at the top, one mess at the listed bottom, and nothing anywhere else");
    }

    [Fact]
    public void AShallowSiteHasNoStaffCanteenAndThatIsTheHonestAnswer()
    {
        // A three-floor annex has one canteen, because one canteen is the entire catering budget of a
        // three-floor annex. The law has to be able to say "none here" out loud, or it is a law that
        // invents a room to satisfy itself.
        int shallow = 0, deep = 0;
        foreach (string body in ManySites())
        {
            if (UndergroundComplex.StaffCanteenFloor(body) is { } mess)
            {
                deep++;
                Assert.True(UndergroundComplex.HoldsPressure(mess), $"{body}: the mess cannot breathe.");
                Assert.True(mess < UndergroundComplex.TopPressurisedFloor(body),
                    $"{body}: the mess is not below the bar.");
                Assert.False(UndergroundComplex.IsUnlisted(body, mess),
                    $"{body}: the mess is on a floor nobody listed.");
            }
            else
            {
                shallow++;
                // Only ever because the building has no second pressurised listed floor to put one on.
                Assert.True(UndergroundComplex.DepthOf(body) > -UndergroundComplex.FloorsPerShaft - 1,
                    $"{body}: deep enough for a mess ({UndergroundComplex.DepthOf(body)}) and has none.");
            }
        }

        Assert.True(shallow > 0, "no shallow site in the net — the null case is never exercised.");
        Assert.True(deep > 0, "no deep site in the net — the mess is never exercised.");
    }

    [Fact]
    public void NOTHINGIsEverPlumbedOnAFloorThatCannotBreathe()
    {
        // ── THE ONE PRESSURE SOURCE ──
        //
        // A canteen, a cubicle and an en-suite are all plumbing, and plumbing is for people out of their
        // suits — the owner's own general rule ("any room that would house like office work would be
        // pressurized by that constraint ... any kind of fine motor skill stuff"), and eating and washing
        // are the same constraint one notch further along.
        //
        // The rule matters far more than the fiction does: the moment ONE room down here breathes for a
        // reason that is not HoldsPressure, the plate by the lift and the suit gauge are reading two
        // different maps, and §13.13 exists because that is worse than saying nothing at all. There is one
        // pressure fact in this building and every amenity is asked to justify itself against it.
        AuditEveryFloor((_, level, floor) =>
        {
            if (UndergroundComplex.HoldsPressure(level))
            {
                return null;
            }
            if (floor.Amenities.Count > 0)
            {
                return $"{floor.Amenities.Count} amenity room(s) on a vacuum floor.";
            }
            return floor.EnSuites.Count > 0
                ? $"{floor.EnSuites.Count} en-suite(s) plumbed into vacuum."
                : null;
        }, "spec — the amenities breathe because HoldsPressure says the floor does, and for no other reason");
    }

    [Fact]
    public void RankIsReadableInPlumbingAndNowhereElseIsACellHung()
    {
        // Owner: "the high level important rooms would have their built in bathrooms". The tell only works
        // if it is EXACT — a cell on a store room says nothing, and a principal room without one says the
        // opposite of the thing it is there to say. Both directions, on every floor.
        AuditEveryFloor((body, level, floor) =>
        {
            foreach (UndergroundComplex.EnSuite cell in floor.EnSuites)
            {
                if (!UndergroundComplex.IsPrincipalRoom(cell.Of))
                {
                    return $"a private washroom off '{cell.Of}', which is not a room anybody sat in.";
                }
            }

            // …and the other direction: every principal room on a floor that breathes HAS one. The cell is
            // skipped when the ground behind the room is already claimed (the ledger is law, #585), so this
            // is also the guard that says that fallback has never once fired on the shipped field.
            if (!UndergroundComplex.HoldsPressure(level))
            {
                return null;
            }
            int principal = 0;
            foreach (string plate in PlatesOn(body, level, floor))
            {
                if (UndergroundComplex.IsPrincipalRoom(plate))
                {
                    principal++;
                }
            }
            return principal == floor.EnSuites.Count ? null
                : $"{principal} principal room(s) and {floor.EnSuites.Count} en-suite(s).";
        }, "spec — an en-suite hangs off a room somebody sat in, and off every one of them");
    }

    /// <summary>Every plate this floor hangs, on a shut door or an open one. The open rooms have no sign on
    /// screen and never will — a room you can walk into does not need one — but the generator knows what
    /// each of them was, and that is what the en-suite reads.</summary>
    private static IEnumerable<string> PlatesOn(
        string body, int level, UndergroundComplex.FloorPlan floor)
    {
        foreach (UndergroundComplex.LockedDoor door in floor.Locked)
        {
            if (!UndergroundComplex.IsSealedWay(door.Sign))
            {
                yield return door.Sign;
            }
        }
        foreach (UndergroundComplex.EnSuite cell in floor.EnSuites)
        {
            // An OPEN principal room hangs its plate nowhere the plan can see, so the cell is the only
            // record of it. Counted from here rather than re-rolled, which would be a second source.
            if (cell.Open)
            {
                yield return cell.Of;
            }
        }
        _ = body;
        _ = level;
    }

    [Fact]
    public void EveryPrincipalPlateIsAPlateThisBuildingActuallyHangs()
    {
        // The list is written out verbatim rather than matched by keyword, which is the right call (a match
        // on "OFFICE" would silently collect the next plate anybody writes containing the word). The cost of
        // verbatim is a typo that selects NOTHING and passes every other test in this file — the fifth bug
        // class exactly. So every entry is proved to exist in some kind's vocabulary.
        var vocabulary = new HashSet<string>(StringComparer.Ordinal);
        foreach (UndergroundComplex.Kind kind in Enum.GetValues<UndergroundComplex.Kind>())
        {
            foreach (string sign in UndergroundComplex.SignsFor(kind))
            {
                vocabulary.Add(sign);
            }
        }

        foreach (string plate in UndergroundComplex.PrincipalPlates)
        {
            Assert.True(vocabulary.Contains(plate),
                $"'{plate}' is on nobody's door — a principal plate no building hangs selects nothing.");
        }

        // And it selects a MINORITY. A rank tell that fires on most doors is wallpaper (#694's lesson).
        Assert.True(UndergroundComplex.PrincipalPlates.Length < vocabulary.Count / 2,
            "more than half the plates in the game are principal — that is not a rank, that is a corridor.");
    }

    [Fact]
    public void TheHeadOfficeAnswersTheSameLawInItsOwnVocabulary()
    {
        // #411 · "outclass them IN THE SAME VOCABULARY". Not a new grammar and not an exception: the same
        // one rule, and the rank falls out of the word lists and the plate stock.
        string hq = KaamosLore.IceMoonBodyId;
        const string branch = "miranda";

        foreach (UndergroundComplex.Comfort use in Enum.GetValues<UndergroundComplex.Comfort>())
        {
            (string hqPlate, string hqFixture) = UndergroundComplex.AmenitySigns(hq, use);
            (string branchPlate, string branchFixture) = UndergroundComplex.AmenitySigns(branch, use);
            Assert.NotEqual(branchPlate, hqPlate);
            Assert.False(string.IsNullOrWhiteSpace(hqFixture));
            Assert.False(string.IsNullOrWhiteSpace(branchFixture));
            Assert.NotEqual(UndergroundComplex.AmenityLine(branch, use), UndergroundComplex.AmenityLine(hq, use));
        }

        // The dining room and the staff hall are HQ's own words, and the staff hall is for the
        // ESTABLISHMENT — which is the plate on its own B2. The grammar is the building's, not a new one.
        Assert.Contains("DINING ROOM", UndergroundComplex.AmenitySigns(hq, UndergroundComplex.Comfort.UpperCanteen).Plate,
            StringComparison.Ordinal);
        Assert.Contains("ESTABLISHMENT",
            UndergroundComplex.AmenitySigns(hq, UndergroundComplex.Comfort.StaffCanteen).Plate,
            StringComparison.Ordinal);
        Assert.Contains("ESTABLISHMENT", UndergroundComplex.HeadOfficeDepartments);

        // …and the rank is in the plumbing too, emergent from the plate stock rather than typed anywhere:
        // most of a head office is people who sign things, so most of a head-office corridor has a private
        // washroom on it and a branch-office corridor has almost none.
        double hqShare = Share(UndergroundComplex.Kind.HeadOffice);
        double branchShare = Share(UndergroundComplex.Kind.RecordsAnnex);
        Assert.True(hqShare > branchShare * 2,
            $"HQ {hqShare:P0} principal vs a branch's {branchShare:P0} — the rank does not read.");

        static double Share(UndergroundComplex.Kind kind)
        {
            string[] signs = UndergroundComplex.SignsFor(kind);
            return signs.Count(UndergroundComplex.IsPrincipalRoom) / (double)signs.Length;
        }
    }

    [Fact]
    public void TheBarPlateSaysNoPassIsNeededAndNeverSaysWHY()
    {
        // ── Owner, closing the loop 2026-08-05 ──
        //
        // "The bar would explain why no card is needed to go down to the first floor… setting access to off
        // the books secret lab to partners all trying to keep things off records would be bureaucratic
        // nightmare of office interorganization bureaucracy so the underground bar just is there with access
        // from surface. It kind of provides cover-story as well."
        //
        // Nothing to build — band 0 has never wanted a card (LiftPanel, #590) — but the plate is now allowed
        // to say the FACT out loud, and forbidden to say the reason. That is the whole house style: evidence,
        // and then stop.
        (string plate, _) = UndergroundComplex.AmenitySigns("miranda", UndergroundComplex.Comfort.UpperCanteen);
        Assert.Contains("NO PASS REQUIRED", plate, StringComparison.Ordinal);

        // The mechanical fact the plate is describing: standing on B1 with an empty wallet, the panel has
        // never asked for anything. If that ever changes, the sign on the wall becomes a lie.
        foreach (string body in new[] { "miranda", "luna", "the-clinker" })
        {
            int level = UndergroundComplex.TopPressurisedFloor(body)!.Value;
            IReadOnlyList<UndergroundComplex.LiftStop> panel =
                UndergroundComplex.LiftPanel(body, level, []);
            Assert.Contains(panel, s => s.Level == level && s.Refusal is null);
            Assert.Contains(panel, s => s.Level == 0 && s.Refusal is null);
        }

        string[] why = ["because", "cover", "deniab", "partner", "bureaucra", "off the books"];
        foreach (string bad in why)
        {
            Assert.DoesNotContain(bad, plate, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(bad,
                UndergroundComplex.AmenityLine("miranda", UndergroundComplex.Comfort.UpperCanteen),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheMESSPaperworkIsDeniableAndNamesNOBODY()
    {
        // Owner: "paperwork that says it is officially delivered to and operating at a school far away 🤭 —
        // the procurement comedy in one manifest line (homage unnamed; no film titles)."
        //
        // So the joke ships as ONE manifest, and it is a catering manifest: costed per head, quarterly, for
        // a school roll. It has to be readable as a fiddle and never as a confession, because the thing this
        // organisation actually procures is the one thing nothing in this building is allowed to say.
        string mess = UndergroundComplex.AmenityLine("miranda", UndergroundComplex.Comfort.StaffCanteen);
        Assert.Contains("SCHOOL", mess, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("manifest", mess, StringComparison.OrdinalIgnoreCase);

        // No film, no homage, nobody's name, and no wink at the reader.
        string[] forbidden = ["fletch", "utopia", "harris", "cocaine", "as seen in", ":-D", "🤭"];
        foreach (string bad in forbidden)
        {
            Assert.DoesNotContain(bad, mess, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void NoAmenityEXPLAINSWhatThisPlaceWasFor()
    {
        // Canon, owner ruling 2026-07-30, and a canteen is a tempting place to break it — a room about
        // people is one sentence from a room about what was done to them. A washroom explains nothing.
        string[] forbidden =
        [
            "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect", "clone",
            "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
        ];

        var prose = new List<string>();
        foreach (string body in new[] { "miranda", KaamosLore.IceMoonBodyId })
        {
            foreach (UndergroundComplex.Comfort use in Enum.GetValues<UndergroundComplex.Comfort>())
            {
                (string plate, string fixture) = UndergroundComplex.AmenitySigns(body, use);
                prose.Add(plate);
                prose.Add(fixture);
                prose.Add(UndergroundComplex.AmenityLine(body, use));
            }
        }

        foreach (string line in prose)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void AnAmenityIsNotAlsoARoomToTurnOver()
    {
        // Same law the refuge lives under: the room stops being a haul room when it becomes one. A canteen
        // that also pays out bench hardware would be two rooms drawn on one another, and the search console
        // would sit on top of the counter.
        AuditEveryFloor((_, _, floor) =>
        {
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                foreach ((double rx, double ry) in floor.RoomCentres)
                {
                    if (Math.Abs(a.X - rx) < 0.5 && Math.Abs(a.Y - ry) < 0.5)
                    {
                        return $"the {a.Use} at ({a.X:F0}, {a.Y:F0}) is still listed as a room to search.";
                    }
                }
                foreach (UndergroundComplex.Refuge r in floor.Refuges)
                {
                    if (Math.Abs(a.X - r.X) < 0.5 && Math.Abs(a.Y - r.Y) < 0.5)
                    {
                        return "an amenity and a pressure refuge are the same room.";
                    }
                }
            }
            return null;
        }, "spec — an amenity is carved out of the rooms, not laid on top of one");
    }

    [Fact]
    public void CarvingTheAmenitiesNeverEMPTIESAFloor()
    {
        // #585's founding complaint, still the law: not a two-door apartment. Three rooms come out of the
        // top floor's pool, and if that ever cost a captain somewhere to go, the feature has taken more than
        // it paid.
        //
        // Stated as CONSERVATION rather than as a floor of four, and that is deliberate. "At least four
        // places" is already the client's law over the ten sites the game actually ships
        // (YouCanWalkTheHiveTests.AFloorIsWorthTheLiftRide), and asserting it here over ninety GENERATED
        // moons fails on ten floors that were tight long before this issue — a guard that ships red teaches
        // everybody to scroll past it. What is this feature's business is that carving takes nothing away:
        // every doorway the generator cut is still a door into somewhere, as a room to search, a refuge, or
        // a canteen. Doorways.Count is the count before any carving, so the sum is exact.
        AuditEveryFloor((_, _, floor) =>
        {
            int places = floor.RoomCentres.Count + floor.Refuges.Count + floor.Amenities.Count;
            if (places != floor.Doorways.Count)
            {
                return $"{floor.Doorways.Count} doors were cut and only {places} of them lead anywhere.";
            }
            return floor.Amenities.Count > 0 && floor.RoomCentres.Count == 0
                ? "the carving took the last room on the floor."
                : null;
        }, "spec — an amenity is a room relabelled, never a room removed");
    }

    [Fact]
    public void TheWayDownIsStillInARoomNobodyTurnedIntoACanteen()
    {
        // #592 · The designated Key room is the whole way into the band nobody listed, and the carving takes
        // rooms out of the same list its index is read from. The refuge already reserves it; a canteen
        // sitting on the deepest listed floor is a second placer able to make the same mistake, and the
        // mistake is a feature silently dead on some worlds forever with every test still green.
        foreach (string body in ManySites())
        {
            if (UndergroundComplex.KeyRoomFor(body) is not { } key)
            {
                continue;
            }
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, key.Level, Field);
            Assert.True(floor.RoomCentres.Count > key.RoomIndex,
                $"{body} B{-key.Level}: the way down is room {key.RoomIndex} of {floor.RoomCentres.Count}.");
            Assert.Equal(UndergroundComplex.Haul.Key,
                UndergroundComplex.InRoom(body, key.Level, key.RoomIndex));
        }
    }

    [Fact]
    public void TheSameFloorHasTheSameCanteenEveryVisit()
    {
        // Determinism is law down here and here it is also the design: a captain learns where the bar is.
        foreach (string body in new[] { "miranda", "luna", KaamosLore.IceMoonBodyId })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan a = UndergroundComplex.Build(body, level, Field);
                UndergroundComplex.FloorPlan b = UndergroundComplex.Build(body, level, Field);
                Assert.Equal(a.Amenities.Count, b.Amenities.Count);
                for (int i = 0; i < a.Amenities.Count; i++)
                {
                    // Field by field rather than record-equal: Amenity carries a LIST of table tops, and a
                    // record's generated Equals compares that by reference, so two identical floors would
                    // never be equal and the guard would be red for a reason that has nothing to do with
                    // the world. (Which is the same trap in reverse as a guard that passes on anything.)
                    Assert.Equal(a.Amenities[i].Use, b.Amenities[i].Use);
                    Assert.Equal(a.Amenities[i].X, b.Amenities[i].X);
                    Assert.Equal(a.Amenities[i].Y, b.Amenities[i].Y);
                    Assert.Equal(a.Amenities[i].Plate, b.Amenities[i].Plate);
                    Assert.Equal(a.Amenities[i].Tables, b.Amenities[i].Tables);
                }
                Assert.Equal(a.EnSuites.Count, b.EnSuites.Count);
                for (int i = 0; i < a.EnSuites.Count; i++)
                {
                    Assert.Equal(a.EnSuites[i], b.EnSuites[i]);
                }
            }
        }
    }

    [Fact]
    public void TheCellFitsInsideTheGroundItClaims()
    {
        // The en-suite hangs off the back of a room, so its box has to be a box: tall enough that the
        // doorway cut in the parent's back wall lands INSIDE it rather than beside it, and deep enough to
        // stand in. Cheap arithmetic, and it is the arithmetic that decides whether the audit's flood can
        // ever get in there.
        Assert.True(UndergroundComplex.EnSuiteHalfHeight > UndergroundComplex.DoorHalf,
            "the cell is shorter than its own doorway — the door opens partly into wall.");
        Assert.True(UndergroundComplex.EnSuiteDepth > 4 * DeckReachability.DefaultStep,
            "the cell is too shallow for the flood to sample.");
    }
}
