using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #677 · THE FOUND HALLS — v1, the BAND. Owner ruling 2026-08-04, recorded in
/// <c>worldbuilding-notes.md</c> §10: this is humanity's fourth run, the prior three were ENDED, and every
/// end spared a remnant underground in massive halls. Out on the moons that is a dig which breaks into
/// volume that was <i>already there</i>.
///
/// <para>It is a different CLASS of thing from the band nobody listed (#592, §13.7), and the guards below are
/// mostly about keeping the two apart. The unlisted band is human all the way down — poured, surveyed,
/// invoiced, hidden from the staff who paid for it. This one was never ours, and every single thing that
/// makes a facility legible is therefore ABSENT from it: no plate, no department, no livery, no lock, no
/// stencilled distance, no drain, no shelf, no lamp.</para>
///
/// <para><b>The canon walls, which are the reason this file is long.</b> Nothing down there names a builder,
/// an age or a purpose; the word §8 reserves never appears; and both readings of §10 — the instruments simply
/// got better / this was always here and is being SHOWN to us — survive every line. The prose is authored by
/// the owner and lifted verbatim, and the guards check it character-for-character, because the way this
/// feature dies is one helpful sentence written to fill a gap.</para>
/// </summary>
public sealed class TheFoundBandTests
{
    /// <summary>The scenario's own sites, plus the rock the cheat parks. Without that last entry every sweep
    /// in this file audits a universe where the feature does not exist and passes for the wrong reason —
    /// the fifth named bug class, and the one this project has paid for most recently.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every site in a wide generated sweep that actually has halls. Asserted non-empty rather than
    /// assumed: a one-in-fifty feature is exactly the rate a ten-site sample tells you nothing about.</summary>
    private static List<string> Sites()
    {
        var hiding = new List<string>();
        for (int i = 0; i < 4000; i++)
        {
            string body = $"probe-moon-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                hiding.Add(body);
            }
        }
        hiding.Add(UndergroundComplex.FoundBandCheatSiteId);
        Assert.True(hiding.Count > 40, $"only {hiding.Count} generated site(s) had halls — this proves little.");
        return hiding;
    }

    /// <summary>The first <paramref name="n"/> of them, for the sweeps that build a real floor plan per
    /// gallery. Asserted to be a real slice rather than the whole list: a "sample" that is silently the
    /// population is a test that stopped saying what it costs.</summary>
    private static List<string> Sites(int n)
    {
        List<string> all = Sites();
        Assert.True(all.Count > n, $"the sample of {n} is the whole population of {all.Count}.");
        return [.. all.Take(n)];
    }

    private static IEnumerable<int> GalleriesOf(string body) =>
        UndergroundComplex.FloorsOf(body).Where(l => UndergroundComplex.IsFound(body, l));

    // ── The band ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT IS RARER THAN THE BAND NOBODY LISTED, AND THE RATE IS MEASURED RATHER THAN ASSERTED.
    ///
    /// <para>#592 is one site in four of the ones deep enough to hide something. This is one in five of the
    /// ones that already do — and because only the shallower half of those has room under it for another
    /// shaft inside the performance guard, the measured incidence is lower again. All three numbers are read
    /// off the sweep and printed, because a rate nobody measured is a rate nobody chose (§13.12's lesson: a
    /// threshold that nothing can violate is not a threshold).</para>
    /// </summary>
    [Fact]
    public void ItIsRARERThanTheBandNobodyListedAndTheRateIsMEASURED()
    {
        const int Probes = 4000;
        int unlisted = 0, eligible = 0, halls = 0;
        for (int i = 0; i < Probes; i++)
        {
            string body = $"probe-moon-{i}";
            if (!UndergroundComplex.HasUnlistedBand(body))
            {
                Assert.False(UndergroundComplex.HasFoundBand(body),
                    $"{body}: galleries under a site with nothing to hide — the halls hang off #592's band.");
                continue;
            }
            unlisted++;
            if (UndergroundComplex.BandTop(UndergroundComplex.FoundBandOf(body))
                > UndergroundComplex.DeepestPossibleFloor)
            {
                eligible++;
            }
            if (UndergroundComplex.HasFoundBand(body))
            {
                halls++;
            }
        }

        double ofAll = halls / (double)Probes;
        double ofHiders = halls / (double)unlisted;
        double ofEligible = halls / (double)eligible;
        string measured =
            $"halls on {halls} of {Probes} sites ({ofAll:P2}); "
            + $"{ofHiders:P1} of the {unlisted} that already hide a band; "
            + $"{ofEligible:P1} of the {eligible} with room under them. The die is one in "
            + UndergroundComplex.FoundOneInN.ToString(CultureInfo.InvariantCulture) + ".";

        Assert.True(eligible > 200, $"only {eligible} eligible sites — {measured}");

        // The die itself, recovered from the sweep: one in five of the sites that could have halls.
        Assert.InRange(ofEligible, 0.12, 0.28);

        // And it is strictly rarer than #592, which is the whole shape of the thing: a secret under a secret.
        Assert.True(ofHiders < 1.0 / UndergroundComplex.UnlistedOneInN,
            $"the halls are no rarer than the band nobody listed — {measured}");
        Assert.True(ofAll < 0.05, $"one site in twenty has halls — that is a level, not a find. {measured}");
    }

    /// <summary>
    /// A WHOLE BAND OF NOTHING SITS BETWEEN THE TWO BUILDINGS, and that gap is the feature rather than an
    /// artefact.
    ///
    /// <para>§13.7's gap exists because a listed depth happened to stop mid-band. This one is deliberate: the
    /// band nobody listed FILLS its band, so the next one down would be flush against it — one shaft's floor
    /// and the next shaft's ceiling, which is how a building continues. Four floors of untouched rock is what
    /// says the digging stopped and something else began.</para>
    /// </summary>
    [Fact]
    public void AWholeBandOfNOTHINGSitsBetweenTheDIGAndTheHALLS()
    {
        foreach (string body in Sites())
        {
            int unlistedBand = UndergroundComplex.UnlistedBandOf(body);
            int foundBand = UndergroundComplex.FoundBandOf(body);
            Assert.Equal(unlistedBand + 2, foundBand);

            var floors = UndergroundComplex.FloorsOf(body).ToList();
            Assert.Equal(floors.Count, floors.Distinct().Count());

            // Nothing was dug in the band between them — not one floor of it exists, nothing may authorise
            // it, and nothing may offer a button to it.
            int between = unlistedBand + 1;
            Assert.False(UndergroundComplex.SiteHasBand(body, between));
            for (int level = UndergroundComplex.BandTop(between);
                 level > UndergroundComplex.BandTop(foundBand);
                 level--)
            {
                Assert.DoesNotContain(level, floors);
                Assert.False(UndergroundComplex.IsFound(body, level));
                Assert.False(UndergroundComplex.IsUnlisted(body, level));
            }

            // The halls are their own whole band on their own shaft, and the true bottom is theirs.
            Assert.Contains(UndergroundComplex.BandTop(foundBand), floors);
            Assert.Contains(UndergroundComplex.TrueDepthOf(body), floors);
            Assert.Equal(4, GalleriesOf(body).Count());
            Assert.True(UndergroundComplex.TrueDepthOf(body) < UndergroundComplex.UnlistedBottomOf(body));

            // And the way deeper steps OVER the nothing rather than into it.
            Assert.Equal(foundBand,
                UndergroundComplex.NextShaftBelow(body, UndergroundComplex.UnlistedBottomOf(body)));
            Assert.Null(UndergroundComplex.NextShaftBelow(body, UndergroundComplex.TrueDepthOf(body)));
        }
    }

    /// <summary>
    /// THE BOTTOM OF THE BUILDING IS NO LONGER THE BOTTOM OF THE HOLE, and everything that belongs to the
    /// OPERATION still sits in the operation.
    ///
    /// <para>This is the assertion <see cref="UndergroundComplex.UnlistedBottomOf"/> exists for. The thing on
    /// the pallet (#614) is crated, invoiced and has the lights left on over it, so it lives on the deepest
    /// floor somebody DUG. Written as <c>TrueDepthOf</c> — which it was — it moved two bands down into a
    /// gallery the moment this feature shipped, and every existing test stayed green.</para>
    /// </summary>
    [Fact]
    public void THEPALLETStaysInTheBuildingThatPaidForIt()
    {
        foreach (string body in Sites())
        {
            (int level, int room) = UndergroundComplex.RelicRoomFor(body)!.Value;
            Assert.Equal(UndergroundComplex.UnlistedBottomOf(body), level);
            Assert.True(UndergroundComplex.IsUnlisted(body, level));
            Assert.False(UndergroundComplex.IsFound(body, level));
            Assert.Equal(UndergroundComplex.Haul.Relic, UndergroundComplex.InRoom(body, level, room));

            // …and its own line is the pallet's, not the wall's. One haul, two objects, told apart by the id
            // the find is minted with.
            string palletId = UndergroundComplex.FindId(body, level, room);
            Assert.False(UndergroundComplex.IsHallRecord(palletId));
            Assert.Equal(CarriedObject.CollarArtUrl, CarriedObject.RelicReveal(palletId).ArtUrl);
        }
    }

    /// <summary>
    /// THE WAY IN IS A CARD SOMEBODY LEFT IN A ROOM, one rung further along — and it is DESIGNATED, for the
    /// reason #592's own way down is: a Key is one face in nine, and a band that rolled none would leave a
    /// site's halls unreachable not for that visit but forever, with nothing on screen ever saying so.
    /// </summary>
    [Fact]
    public void TheWayInIsACardFoundInTheBandNOBODYListed()
    {
        foreach (string body in Sites())
        {
            (int level, int room) = UndergroundComplex.FoundKeyRoomFor(body)!.Value;

            // On the unlisted band's own shaft head — the floor where the plate finally names a different
            // building (#694). Not its bottom (the pallet's) and not the listed bottom (#592's card's).
            Assert.Equal(UndergroundComplex.BandTop(UndergroundComplex.UnlistedBandOf(body)), level);
            Assert.True(UndergroundComplex.IsUnlisted(body, level));
            Assert.NotEqual(UndergroundComplex.KeyRoomFor(body), (level, room));
            Assert.NotEqual(UndergroundComplex.RelicRoomFor(body), (level, room));

            // The room exists on the field the rest of the suite uses, and it holds a Key.
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            Assert.InRange(room, 0, floor.RoomCentres.Count - 1);
            Assert.Equal(UndergroundComplex.Haul.Key, UndergroundComplex.InRoom(body, level, room));

            // And the card it mints runs the HALLS' shaft — stepping over the band of nothing.
            UndergroundComplex.AuthorityCard card = UndergroundComplex.CardInRoom(body, level)!.Value;
            Assert.Equal(UndergroundComplex.FoundBandOf(body), card.Band);

            // Nothing past it. The halls are the bottom, and no card is ever issued down there.
            foreach (int gallery in GalleriesOf(body))
            {
                Assert.Null(UndergroundComplex.CardInRoom(body, gallery));
            }
        }
    }

    /// <summary>
    /// THE PANEL NEVER ADMITS THE HALLS EXIST, in exactly the way it never admits the unlisted band does.
    /// A refusal that named the shaft would give the whole thing away in the one sentence it cannot survive,
    /// so the button is simply not there until the paper is in the wallet.
    /// </summary>
    [Fact]
    public void NoLIFTDIRECTORYEverMentionsThem()
    {
        foreach (string body in Sites(20))
        {
            int fromLevel = UndergroundComplex.UnlistedBottomOf(body);
            int head = UndergroundComplex.BandTop(UndergroundComplex.FoundBandOf(body));

            IReadOnlyList<UndergroundComplex.LiftStop> silent =
                UndergroundComplex.LiftPanel(body, fromLevel, []);
            Assert.DoesNotContain(silent, s => s.Level == head);
            Assert.All(silent, s => Assert.True(s.Level >= UndergroundComplex.UnlistedBottomOf(body)));

            // …and with the card, the row is there and it opens.
            string cardId = new UndergroundComplex.AuthorityCard(
                body, UndergroundComplex.FoundBandOf(body)).Id;
            UndergroundComplex.LiftStop opened = Assert.Single(
                UndergroundComplex.LiftPanel(body, fromLevel, [cardId]), s => s.Level == head);
            Assert.Null(opened.Refusal);
            Assert.NotNull(opened.OpenedBy);
            Assert.True(opened.Pressurised);

            // The ride is a gate crossing, so the countersignature beat fires — the same one #689 built.
            Assert.Equal(UndergroundComplex.FoundBandOf(body),
                UndergroundComplex.GateOpenedByRidingTo(body, fromLevel, head, [cardId])!.Value.Band);
        }
    }

    // ── The senses ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// DARK, AND IT IS THE FLOOR'S OWN CLAIM (#708's customer, arrived). Every gallery declares darkness;
    /// nothing the facility built ever does. The cone is the whole of the seeing.
    /// </summary>
    [Fact]
    public void EveryGalleryDeclaresITSELFDarkAndNoFacilityFloorDoes()
    {
        foreach (string body in Sites(20))
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                bool gallery = UndergroundComplex.IsFound(body, level);
                Assert.Equal(gallery, UndergroundComplex.DeclaresDarkness(body, level));
                Assert.Equal(gallery, UndergroundComplex.IsDark(body, level));
            }
        }
    }

    /// <summary>
    /// AIR, AND NOTHING ANYWHERE SHOWS WHAT MAKES IT. Owner's ruling: the gauge reads SEALED — you are
    /// breathing the room — and there is no plant, no duct, no grille, no pump anywhere the cone can find.
    ///
    /// <para>Both halves are checked, because either alone is harmless and together they are the sensation.
    /// The floor holds pressure through the ONE pressure fact (§13.13, so the plate by the lift and the tank
    /// on the captain's back cannot come apart), and <see cref="UndergroundComplex.IsPlumbed"/> says no, so
    /// not one fixture, cell, counter or refuge is ever built down there.</para>
    /// </summary>
    [Fact]
    public void THEAIRHoldsAndNOTHINGIsEverPLUMBEDToExplainIt()
    {
        foreach (string body in Sites(20))
        {
            Assert.NotEqual(UndergroundComplex.TopPressurisedFloor(body), UndergroundComplex.BandTop(
                UndergroundComplex.FoundBandOf(body)));

            foreach (int level in GalleriesOf(body))
            {
                Assert.True(UndergroundComplex.HoldsPressure(body, level),
                    $"{body} B{-level}: a gallery that cannot be breathed.");
                Assert.False(UndergroundComplex.IsPlumbed(body, level),
                    $"{body} B{-level}: a gallery with plumbing in it.");

                // The suit and the plate read the same one predicate, and both say ROOM.
                Assert.Equal(SuitAir.Supply.Room,
                    SuitAir.SourceOf(body, level, insideShelter: false, aboard: false));

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                Assert.True(floor.Pressurised);
                Assert.Empty(floor.Amenities);
                Assert.Empty(floor.EnSuites);
                Assert.Empty(floor.Refuges);
            }

            // …and the two rooms the directory hangs its plumbing off are never down there.
            if (UndergroundComplex.StaffCanteenFloor(body) is { } mess)
            {
                Assert.False(UndergroundComplex.IsFound(body, mess));
            }
        }
    }

    /// <summary>
    /// NOTHING DOWN THERE IS SIGNAGE. No plate by the lift, no department, no livery, no locked door with a
    /// purpose painted on it, no stencilled distance, and no imported door leaf — which is the material
    /// language (#592) saying somebody flew a thing here and fitted it.
    /// </summary>
    [Fact]
    public void NoPLATE_NoLIVERY_NoLOCK_NoSTENCIL_NoIMPORTEDLEAF()
    {
        foreach (string body in Sites(20))
        {
            foreach (int level in GalleriesOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);

                Assert.Empty(floor.Locked);
                Assert.Empty(floor.Doorways);
                Assert.Null(UndergroundComplex.LiveryFor(body, level));
                Assert.False(UndergroundComplex.ShowsFacilityPlate(body, level));
                Assert.Contains("NO PLATE", UndergroundComplex.NameOf(body, level), StringComparison.Ordinal);

                // It is still a place worth stepping out into, which is the other half of every §13 law.
                Assert.True(floor.RoomCentres.Count >= 4,
                    $"{body} B{-level}: {floor.RoomCentres.Count} chamber(s) — that is a flat.");
                Assert.True(floor.Walls.Count > 60, $"{body} B{-level}: that is not a gallery, it is a line.");
                foreach (SurfaceLayout.Wall w in floor.Walls)
                {
                    Assert.InRange(w.X1, Field.LeftX, Field.RightX);
                    Assert.InRange(w.Y1, Field.BottomY, Field.LandingBandY);
                }
            }
        }
    }

    /// <summary>
    /// THE CHAMBERS GET BIGGER GOING DOWN, which inverts everything the game has taught, and the renderer's
    /// room scale is the only sentence said about it.
    ///
    /// <para>Measured off the real floor plans rather than off the constant: the deepest gallery's chambers
    /// have visibly more floor area than the first's, and there are fewer of them. And the scale is DERIVED —
    /// the facility's own room module taken to the power of how far into the band you are — so no dimension
    /// down here is typed anywhere.</para>
    /// </summary>
    [Fact]
    public void TheCHAMBERSGetBIGGERTheDeeperYouGo()
    {
        var sb = new StringBuilder();
        int measured = 0;

        foreach (string body in Sites(20))
        {
            var galleries = GalleriesOf(body).ToList();
            int head = galleries[0];
            int bottom = galleries[^1];

            Assert.Equal(1.0, UndergroundComplex.RoomScaleOn(body, head), 6);
            Assert.Equal(
                Math.Pow(UndergroundComplex.FoundGrowthPerFloor, head - bottom),
                UndergroundComplex.RoomScaleOn(body, bottom),
                6);

            // Every floor above the seam is untouched — the same module the Hive has always used.
            foreach (int level in UndergroundComplex.FloorsOf(body).Where(l => !UndergroundComplex.IsFound(body, l)))
            {
                Assert.Equal(1.0, UndergroundComplex.RoomScaleOn(body, level), 6);
            }

            // …and the drawn floors agree: bigger boxes, fewer of them.
            double Area(int level) =>
                UndergroundComplex.RoomWidthDu * UndergroundComplex.RoomHeightDu
                * Math.Pow(UndergroundComplex.RoomScaleOn(body, level), 2);

            int chambersAtTop = UndergroundComplex.Build(body, head, Field).RoomCentres.Count;
            int chambersAtBottom = UndergroundComplex.Build(body, bottom, Field).RoomCentres.Count;

            if (Area(bottom) <= Area(head) * 1.4)
            {
                sb.AppendLine($"  {body}: the deepest gallery is only {Area(bottom) / Area(head):F2}× the "
                    + "first — not visibly larger.");
            }
            if (chambersAtBottom >= chambersAtTop)
            {
                sb.AppendLine($"  {body}: {chambersAtTop} chambers at the top and {chambersAtBottom} at the "
                    + "bottom — the grammar did not invert.");
            }
            measured++;
        }

        Assert.True(measured >= 20, $"only {measured} site(s) measured.");
        Assert.True(sb.Length == 0, $"the geometry says nothing:{Environment.NewLine}{sb}");
    }

    // ── What it pays ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT PAYS IN A MEASUREMENT AND IN NOTHING ELSE, and almost every room pays nothing at all.
    ///
    /// <para>Measured over enough rooms to be the law and not the dice. What is deliberately absent is each
    /// its own canon rule: no EQUIPMENT (a crate names a supplier), no RECORDS and no DIRT (paperwork is an
    /// institution, and a file on somebody would say who kept it), no KEY (the entry card is the last card
    /// there is, and a second would make the halls a building with a directory).</para>
    /// </summary>
    [Fact]
    public void ItPaysInAMEASUREMENTAndAlmostEveryRoomPaysNOTHING()
    {
        const int Sample = 900;
        int empty = 0, records = 0;

        foreach (string body in Sites(20))
        {
            foreach (int level in GalleriesOf(body))
            {
                for (int room = 0; room < Sample / 4; room++)
                {
                    UndergroundComplex.Haul haul = UndergroundComplex.InRoom(body, level, room);
                    Assert.True(haul is UndergroundComplex.Haul.Nothing or UndergroundComplex.Haul.Relic,
                        $"{body} B{-level} room {room}: a gallery paid out {haul}.");
                    if (haul == UndergroundComplex.Haul.Relic)
                    {
                        records++;
                    }
                    else
                    {
                        empty++;
                    }
                }
            }
        }

        double rate = records / (double)(records + empty);
        string measured = $"a record turns up in {records} of {records + empty} chambers ({rate:P1}) — "
            + $"the law is one in {UndergroundComplex.FoundRecordOneInN}";
        Assert.InRange(rate, 0.06, 0.16);
        Assert.True(empty > records * 4, $"the emptiness has stopped being the common case: {measured}");
    }

    /// <summary>
    /// WHAT GOES IN THE POCKET IS THE RECORD, AND THE WALL KEEPS THE REST (#614's law, one class of object
    /// further along). The thing is not liftable, so a satchel claiming to contain it would be the third
    /// named bug class one size up.
    /// </summary>
    [Fact]
    public void TheRECORDGoesInThePocketAndTheWALLStaysWhereItIs()
    {
        string body = Sites(20)[0];
        int level = GalleriesOf(body).First();
        int room = Enumerable.Range(0, 200)
            .First(r => UndergroundComplex.InRoom(body, level, r) == UndergroundComplex.Haul.Relic);

        string findId = UndergroundComplex.FindId(body, level, room);
        Assert.True(UndergroundComplex.IsHallRecord(findId));

        UndergroundComplex.Pickup pick = UndergroundComplex.WhatGoesInThePocket(
            UndergroundComplex.Haul.Relic, body, minted: null, findId, carried: []);

        Assert.True(pick.RoomEmptied);
        Assert.Equal(Satchel.Kind.Relic, pick.Take!.Value.Kind);
        Assert.Equal(UndergroundComplex.FoundRecordFindLine, pick.Line);

        // The room itself says nothing, because nothing was authored for it to say — the card does the
        // describing, and a sentence invented here is the one thing this feature forbids.
        Assert.Equal("", UndergroundComplex.HaulLine(
            UndergroundComplex.Haul.Relic, body, level, room, minted: null));

        // The card is caption-only in the #528 idiom, and it is NOT the pallet's.
        CarriedObject.Reveal card = CarriedObject.RelicReveal(findId);
        Assert.Equal("", card.ArtUrl);
        Assert.Equal(UndergroundComplex.FoundRecordCard, card.Story);
        Assert.NotEqual(CarriedObject.CollarStory, card.Story);

        // …and the casebook keeps the gist rather than the sentence (#603: looking is free, knowledge is
        // one-shot). Nothing above the seam ever answers this.
        Assert.Equal(UndergroundComplex.FoundRecordGist,
            UndergroundComplex.CasebookGistOf(UndergroundComplex.Haul.Relic, body, level));
        Assert.Null(UndergroundComplex.CasebookGistOf(
            UndergroundComplex.Haul.Relic, body, UndergroundComplex.UnlistedBottomOf(body)));
        Assert.Null(UndergroundComplex.CasebookGistOf(UndergroundComplex.Haul.Nothing, body, level));
    }

    /// <summary>
    /// THE ODD BOOK DOES NOT RUN DOWN HERE, and the cheat cannot put one there either.
    ///
    /// <para>A shelf is a FACILITY object: the engine behind it is a department that reads everything, and a
    /// department is staff, a budget, a requisition and a room somebody was given. A paperback in a gallery
    /// would be the most explaining object the game could put in one — it would say somebody LIVED there,
    /// and which century they came from.</para>
    ///
    /// <para>The cheat half matters as much as the roll: <c>OddBooks.Search</c>'s forced path bypasses
    /// <c>HoldsOne</c> entirely, so two guards would be two answers and the one that got missed would be the
    /// one a tester typing <c>?book=6</c> in a hall walked straight through.</para>
    /// </summary>
    [Fact]
    public void TheODDBOOKDoesNotRunInAGalleryAndNoCHEATPutsOneThere()
    {
        int swept = 0;
        foreach (string body in Sites(20))
        {
            foreach (int level in GalleriesOf(body))
            {
                Assert.False(OddBooks.ShelvesStandHere(body, level));
                for (int room = 0; room < 40; room++)
                {
                    swept++;
                    Assert.False(OddBooks.HoldsOne(body, level, room),
                        $"{body} B{-level} room {room}: a shelf in a gallery.");
                    Assert.Null(OddBooks.Search(body, level, room, filed: []));

                    for (int forced = 0; forced <= OddBooks.Catalog.Count; forced++)
                    {
                        Assert.Null(OddBooks.Search(body, level, room, filed: [], forced));
                    }
                }
            }

            // …and it still runs everywhere it always did, which is the other half of the claim.
            foreach (int level in UndergroundComplex.FloorsOf(body).Where(l => !UndergroundComplex.IsFound(body, l)))
            {
                Assert.True(OddBooks.ShelvesStandHere(body, level));
            }
        }

        Assert.True(swept > 800, $"only {swept} gallery chamber(s) swept — this proved little.");
    }

    // ── The canon walls ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE GREP. Every string the halls can produce, against every word that would end the feature.
    ///
    /// <para><b>monolith</b> is reserved by §8 and is the first entry, because it is the connection the player
    /// is supposed to make alone, in the dark, with a gauge that says the air is fine. After it come the
    /// builder, the age, the purpose and the two inferences that must stay open — the Old Ones and the
    /// Reevers — plus the vocabulary of the amenities and the fixtures (§13.17), which is a vocabulary about
    /// people who were paid, invoiced and given somewhere to wash.</para>
    /// </summary>
    [Fact]
    public void NOTHINGDownHereNamesABUILDERAnAGEOrAPURPOSE()
    {
        string[] forbidden =
        [
            // §8 · the word is reserved. This one first, always.
            "monolith",

            // The inferences that stay open, and the origin that is never confirmed.
            "reever", "old one", "ancient", "caretaker", "watcher", "remnant",
            "restore", "backup", "revive", "resurrect", "clone", "slave",

            // A builder, an age, a purpose.
            "built by", "made by", "who built", "older than", "millennia", "aeon", "eon",
            "civilis", "civiliz", "alien", "species", "they made", "designed to", "meant for",
            "temple", "tomb", "shrine", "ritual",

            // §13.17's vocabulary: a duct is an invoice with a shape.
            "duct", "grille", "vent", "pump", "fan ", "plant", "wiring", "cable", "conduit",
            "fixture", "fitting", "canteen", "washroom", "cubicle", "basin", "drain",

            // NB: the facility's own "stripped to the fittings" line is not grepped here — the authored
            // empty-room line says "Not stripped", on purpose. That law has its own test below, which
            // compares the two STRINGS rather than hunting for a word inside one of them.
        ];

        var text = new List<string>
        {
            UndergroundComplex.SeamLine,
            UndergroundComplex.FoundArrivalLine,
            UndergroundComplex.FoundEmptyRoomLine,
            UndergroundComplex.FoundRecordFindLine,
            UndergroundComplex.FoundRecordGist,
            UndergroundComplex.FoundRecordCard,
            UndergroundComplex.FoundRecordCardLabel,
        };

        foreach (string body in Sites(20).Take(8))
        {
            foreach (int level in GalleriesOf(body))
            {
                text.Add(UndergroundComplex.NameOf(body, level));
                text.Add(UndergroundComplex.DepthPaint(level));
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                foreach (UndergroundComplex.LockedDoor door in floor.Locked)
                {
                    text.Add(door.Sign);
                }
                foreach (SurfaceLayout.Landmark label in floor.Labels)
                {
                    text.Add(label.Label);
                }
                foreach (UndergroundComplex.Refuge refuge in floor.Refuges)
                {
                    text.Add(refuge.Sign);
                }
                for (int room = 0; room < floor.RoomCentres.Count; room++)
                {
                    UndergroundComplex.Haul haul = UndergroundComplex.InRoom(body, level, room);
                    text.Add(UndergroundComplex.HaulLine(haul, body, level, room, minted: null));
                    text.Add(UndergroundComplex.WhatGoesInThePocket(
                        haul, body, minted: null,
                        UndergroundComplex.FindId(body, level, room), carried: []).Line);
                }
            }
        }

        var sb = new StringBuilder();
        foreach (string line in text)
        {
            foreach (string bad in forbidden)
            {
                if (line.Contains(bad, StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"  Found: \"{bad}\" in: {line}");
                }
            }
        }

        Assert.True(sb.Length == 0, $"the halls explained themselves:{Environment.NewLine}{sb}");
        Assert.True(text.Count > 200, $"only {text.Count} string(s) swept — the grep proved little.");
    }

    /// <summary>
    /// THE FACILITY'S OWN EMPTY-ROOM LINE NEVER APPEARS IN A GALLERY, and the authored one always does.
    ///
    /// <para><i>"Stripped to the fittings. Whoever cleared this room did it carefully and did it in a hurry"</i>
    /// is a sentence about STAFF — somebody was there, somebody left in a hurry, and somebody decided what to
    /// take. Down here nobody ever was. Both halves are asserted because the way this dies is a new
    /// <c>Haul</c> value falling through to the facility's default arm.</para>
    /// </summary>
    [Fact]
    public void TheSTRIPPEDLineNeverAppearsPastTheSeamAndTheAuthoredOneAlwaysDoes()
    {
        string facilityEmpty = UndergroundComplex.HaulLine(
            UndergroundComplex.Haul.Nothing, "miranda", -2, 0, minted: null);
        Assert.Contains("Stripped", facilityEmpty, StringComparison.Ordinal);

        int checkedRooms = 0;
        foreach (string body in Sites(20).Take(12))
        {
            foreach (int level in GalleriesOf(body))
            {
                for (int room = 0; room < 30; room++)
                {
                    UndergroundComplex.Haul haul = UndergroundComplex.InRoom(body, level, room);
                    string line = UndergroundComplex.HaulLine(haul, body, level, room, minted: null);
                    checkedRooms++;

                    Assert.NotEqual(facilityEmpty, line);
                    if (haul == UndergroundComplex.Haul.Nothing)
                    {
                        Assert.Equal(UndergroundComplex.FoundEmptyRoomLine, line);
                    }
                }

                // …and EVERY Haul value the enum has, including ones a gallery cannot roll today, answers in
                // the halls' own vocabulary rather than falling through to the facility's default arm.
                foreach (UndergroundComplex.Haul haul in Enum.GetValues<UndergroundComplex.Haul>())
                {
                    string line = UndergroundComplex.HaulLine(haul, body, level, 0, minted: null);
                    Assert.True(
                        line.Length == 0 || string.Equals(
                            line, UndergroundComplex.FoundEmptyRoomLine, StringComparison.Ordinal),
                        $"{body} B{-level}: {haul} said \"{line}\" in a gallery.");
                }
            }
        }

        Assert.True(checkedRooms > 300, $"only {checkedRooms} chamber(s) checked.");
    }

    /// <summary>
    /// THE AUTHORED PROSE IS LIFTED VERBATIM. Six strings, the owner's, character-for-character — because the
    /// way this feature dies is somebody improving one of them.
    /// </summary>
    [Fact]
    public void THEAUTHOREDSTRINGSAreLiftedVERBATIM()
    {
        Assert.Equal(
            "The pour stops. Not at a wall — at a line, clean as a tide mark, and past it the tunnel keeps "
            + "going in a material the light does not grip.",
            UndergroundComplex.SeamLine);

        Assert.Equal(
            "The car has no button for this floor. It stops anyway. The air is good. Nothing here says why.",
            UndergroundComplex.FoundArrivalLine);

        Assert.Equal(
            "🎒 Into your pocket: measurements, a photograph, a rubbing. The wall keeps the rest.",
            UndergroundComplex.FoundRecordFindLine);

        Assert.Equal(
            "A section of wall, recorded because it cannot be brought back: continuous, seamless, faintly "
            + "warm. The tape measure in the photograph is there to give it a scale, and fails.",
            UndergroundComplex.FoundRecordCard);

        Assert.Equal(
            "a wall with no seam, faintly warm — the tape measure fails to give it scale",
            UndergroundComplex.FoundRecordGist);

        Assert.Equal(
            "Nothing. Not stripped — nothing was ever here. The room is clean the way a prepared room is "
            + "clean.",
            UndergroundComplex.FoundEmptyRoomLine);

        // The card's title is the authored gist inside the house frame — the odd book's own idiom, and the
        // reason no caption prose was invented for a hall.
        Assert.EndsWith(UndergroundComplex.FoundRecordGist, UndergroundComplex.FoundRecordCardLabel,
            StringComparison.Ordinal);
    }

    // ── The cheat ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE <c>?found=1</c> ROCK STILL HAS HALLS UNDER IT. A scene nobody can reach on demand ships broken,
    /// and this one is about one site in fifty: the cheat is a body ID and nothing else, which is the right
    /// shape and also the fragile one.
    ///
    /// <para>If this goes red, sweep for another id and change the one const — <see
    /// cref="UndergroundComplex.FoundBandCheatSiteId"/> — which every sweep in this file and in the client's
    /// audits reads.</para>
    /// </summary>
    [Fact]
    public void TheFOUNDCheatRockStillHasGalleriesUnderIt()
    {
        string rock = UndergroundComplex.FoundBandCheatSiteId;

        Assert.True(UndergroundComplex.HasUnlistedBand(rock), $"'{rock}' no longer hides a band.");
        Assert.True(UndergroundComplex.HasFoundBand(rock),
            $"'{rock}' no longer has halls under it — ?found=1 now lands on an ordinary facility.");

        // The full chain, in one rock: a facility, a band nobody listed with a different Kind, a band of
        // nothing, and four galleries.
        Assert.NotEqual(
            UndergroundComplex.KindFor(rock),
            UndergroundComplex.KindOn(rock, UndergroundComplex.UnlistedBottomOf(rock)));
        Assert.Equal(4, GalleriesOf(rock).Count());

        // And the cheat's own promise: every band it mints a card for is a band that exists, and the deepest
        // of them is the halls. The loop in the client walks band indices and skips the absent one — a loop
        // that stopped at the first absence would hand out every card except the only one that matters.
        var minted = new List<int>();
        for (int band = 0; band <= UndergroundComplex.BandOf(UndergroundComplex.DeepestPossibleFloor); band++)
        {
            if (UndergroundComplex.SiteHasBand(rock, band))
            {
                minted.Add(band);
            }
        }
        Assert.Contains(UndergroundComplex.FoundBandOf(rock), minted);
        Assert.DoesNotContain(UndergroundComplex.UnlistedBandOf(rock) + 1, minted);
    }

    /// <summary>
    /// AND THE FLOOR CHEAT NEVER SETS ANYBODY DOWN IN ROCK. <c>?floor=N</c> used to clamp to the true bottom
    /// and then snap into the unlisted band's shaft head — correct arithmetic for a building with ONE gap in
    /// it, and a captain standing in solid rock the day there were two (§13.15's second cause, third
    /// occurrence). The building answers now.
    /// </summary>
    [Fact]
    public void TheFLOORCheatAlwaysLandsOnAFloorThatEXISTS()
    {
        foreach (string body in Bodies)
        {
            var real = UndergroundComplex.FloorsOf(body).ToHashSet();
            for (int asked = -1; asked >= UndergroundComplex.DeepestPossibleFloor - 6; asked--)
            {
                int landed = UndergroundComplex.NearestFloorTo(body, asked);
                Assert.Contains(landed, real);
            }

            // Asking for the bottom gets the bottom, and B1 is always B1.
            Assert.Equal(UndergroundComplex.TrueDepthOf(body),
                UndergroundComplex.NearestFloorTo(body, UndergroundComplex.DeepestPossibleFloor));
            Assert.Equal(-1, UndergroundComplex.NearestFloorTo(body, -1));
        }

        // …and a floor asked for INSIDE the band of nothing lands on the nearest thing anybody dug or found,
        // never in the rock. Asked one floor above the galleries' own shaft head — a level that exists in the
        // arithmetic and in no building.
        string rock = UndergroundComplex.FoundBandCheatSiteId;
        int inTheGap = UndergroundComplex.BandTop(UndergroundComplex.FoundBandOf(rock)) + 1;
        Assert.DoesNotContain(inTheGap, UndergroundComplex.FloorsOf(rock));
        Assert.True(UndergroundComplex.IsFound(rock, UndergroundComplex.NearestFloorTo(rock, inTheGap)),
            "asking for a floor inside the band of nothing did not reach the galleries under it.");
    }
}
