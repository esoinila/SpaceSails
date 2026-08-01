using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #592 · A SECRET LAB'S OWN SECRET LAB. Owner: <i>"we could even have a secret lab lab :-D"</i>.
///
/// <para>A facility whose bottom band is not on its own plan. Everything above it is a real, expensive,
/// thoroughly documented clandestine operation — and underneath THAT is the thing the clandestine operation
/// was hiding from its own staff.</para>
///
/// <para>The whole feature lives in the gap between two numbers: what the building says about itself
/// (<see cref="UndergroundComplex.DepthOf"/>) and how far a captain can actually walk
/// (<see cref="UndergroundComplex.TrueDepthOf"/>). These pin the gap.</para>
/// </summary>
public sealed class TheUnlistedBandTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    [Fact]
    public void ItIsRAREAndItIsAFactAboutTheWORLD()
    {
        // The moment it is common it stops being a secret and becomes a level. And it is seeded off the
        // site's own id, so a moon has what it has — the same building on every visit and in every session.
        foreach (string body in Bodies)
        {
            Assert.Equal(UndergroundComplex.HasUnlistedBand(body), UndergroundComplex.HasUnlistedBand(body));
            Assert.Equal(UndergroundComplex.TrueDepthOf(body), UndergroundComplex.TrueDepthOf(body));
        }

        // Across a wide sample of ids the rate should sit near the declared rarity among ELIGIBLE sites
        // (the shallow ones are excluded — see below). Bounds are loose on purpose: this pins "rare and not
        // never", not a distribution.
        int eligible = 0, hidden = 0;
        for (int i = 0; i < 400; i++)
        {
            string body = $"probe-moon-{i}";
            if (UndergroundComplex.DepthOf(body) <= -UndergroundComplex.FloorsPerShaft)
            {
                eligible++;
                if (UndergroundComplex.HasUnlistedBand(body))
                {
                    hidden++;
                }
            }
        }
        Assert.True(eligible > 200, $"only {eligible} of 400 sites were deep enough to hide anything.");
        double rate = hidden / (double)eligible;
        Assert.InRange(rate, 0.12, 0.45);
    }

    [Fact]
    public void ABungalowDoesNotGetADungeon()
    {
        // A three-floor annex with a secret basement is a joke rather than a secret: the lie needs a
        // building big enough to keep something from its own staff.
        for (int i = 0; i < 400; i++)
        {
            string body = $"probe-moon-{i}";
            if (UndergroundComplex.DepthOf(body) > -UndergroundComplex.FloorsPerShaft)
            {
                Assert.False(UndergroundComplex.HasUnlistedBand(body),
                    $"{body}: a {-UndergroundComplex.DepthOf(body)}-floor site is hiding a whole band.");
            }
        }
    }

    [Fact]
    public void AnOrdinarySiteIsExactlyWhatItAlwaysWas()
    {
        // The feature has to be invisible where it is not present, or it is not a rare secret — it is a
        // change to every facility in the solar system.
        foreach (string body in Bodies.Where(b => !UndergroundComplex.HasUnlistedBand(b)))
        {
            Assert.Equal(UndergroundComplex.DepthOf(body), UndergroundComplex.TrueDepthOf(body));
            Assert.Equal(
                Enumerable.Range(1, -UndergroundComplex.DepthOf(body)).Select(n => -n),
                UndergroundComplex.FloorsOf(body));

            for (int level = -1; level >= UndergroundComplex.DepthOf(body); level--)
            {
                Assert.False(UndergroundComplex.IsUnlisted(body, level));
                Assert.Equal(UndergroundComplex.KindFor(body), UndergroundComplex.KindOn(body, level));
                Assert.Equal(UndergroundComplex.NameOf(level), UndergroundComplex.NameOf(body, level));
            }
        }
    }

    [Fact]
    public void TheHiddenBandIsAWHOLEBandOnItsOwnShaft()
    {
        // The trap this feature has, and the reason it is a BAND and not "four floors below the listed
        // bottom". Bands are fixed slices counted from the surface, because that is what a shaft IS. A
        // hidden band that began at an arbitrary depth would share a car with the floors above it, and the
        // secret would be reachable by pressing DOWN.
        foreach (string body in Sites())
        {
            int listedBottom = UndergroundComplex.DepthOf(body);
            int band = UndergroundComplex.UnlistedBandOf(body);

            Assert.True(band > UndergroundComplex.BandOf(listedBottom),
                $"{body}: the hidden band shares a shaft with the floors above it.");

            // Its top floor is its own shaft head, and every floor of it is unlisted.
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                Assert.Equal(UndergroundComplex.BandOf(level) == band, UndergroundComplex.IsUnlisted(body, level));
            }

            // The car in the LISTED building stops at the listed bottom and cannot be pressed past it.
            Assert.Equal(listedBottom, UndergroundComplex.BandFloor(body, UndergroundComplex.BandOf(listedBottom)));
        }
    }

    [Fact]
    public void NothingIsDugInTheGAP()
    {
        // Where a site's listed depth stops mid-band there is a gap under it before the unlisted shaft head.
        // Those floors must not be in the floor list: a phantom floor is a topology nobody ships being
        // audited instead of the one they do (and, in the client, a lift that steps into rock).
        foreach (string body in Sites())
        {
            var floors = UndergroundComplex.FloorsOf(body).ToList();
            int listed = UndergroundComplex.DepthOf(body);
            int head = UndergroundComplex.BandTop(UndergroundComplex.UnlistedBandOf(body));

            for (int level = listed - 1; level > head; level--)
            {
                Assert.DoesNotContain(level, floors);
            }
            Assert.Contains(head, floors);
            Assert.Contains(UndergroundComplex.TrueDepthOf(body), floors);
            Assert.Equal(floors.Count, floors.Distinct().Count());
        }
    }

    [Fact]
    public void ItsKindIsALWAYSDifferentFromTheFloorsAboveIt()
    {
        // Where the storytelling is. A records annex whose bottom is a clinic tells you what the records
        // were OF, without one line of narration — and a hidden clinic under a clinic is just a bigger
        // clinic, which is the one outcome that makes the whole thing pointless.
        foreach (string body in Sites())
        {
            UndergroundComplex.Kind above = UndergroundComplex.KindFor(body);
            foreach (int level in UndergroundComplex.FloorsOf(body).Where(l => UndergroundComplex.IsUnlisted(body, l)))
            {
                Assert.NotEqual(above, UndergroundComplex.KindOn(body, level));
            }
        }

        // And it holds for every site the generator could ever produce, not just the ten in the scenario.
        for (int i = 0; i < 400; i++)
        {
            string body = $"probe-moon-{i}";
            if (UndergroundComplex.HasUnlistedBand(body))
            {
                Assert.NotEqual(
                    UndergroundComplex.KindFor(body),
                    UndergroundComplex.KindOn(body, UndergroundComplex.TrueDepthOf(body)));
            }
        }
    }

    [Fact]
    public void ItsDoorsSpeakItsOwnVocabularyAndNotTheBuildingsAboveIt()
    {
        // The kind is only worth having if it reaches the SIGNS, because the signs are the only thing the
        // captain actually reads down there.
        string body = Sites().First();
        int deep = UndergroundComplex.TrueDepthOf(body);

        var itsOwn = new HashSet<string>(UndergroundComplex.SignsFor(UndergroundComplex.KindOn(body, deep)));
        var upstairs = new HashSet<string>(UndergroundComplex.SignsFor(UndergroundComplex.KindFor(body)));

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(body, deep, SurfaceLayout.DefaultField);
        Assert.NotEmpty(floor.Locked);
        foreach (UndergroundComplex.LockedDoor door in floor.Locked)
        {
            if (door.Sign.StartsWith('⟶'))
            {
                continue;   // a SECTOR corridor, not a room door
            }
            Assert.Contains(door.Sign, itsOwn);
            Assert.DoesNotContain(door.Sign, upstairs);
        }
    }

    [Fact]
    public void AFloorNobodyListedHasNoDepartment()
    {
        // A department is a thing you write on a plan, and the whole point of these floors is that nobody
        // wrote them anywhere. It is not a hint: you can only read it standing on the floor, by which time
        // the building has stopped keeping the secret from YOU.
        foreach (string body in Sites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                string name = UndergroundComplex.NameOf(body, level);
                Assert.StartsWith($"B{-level} ", name, StringComparison.Ordinal);
                Assert.Equal(UndergroundComplex.IsUnlisted(body, level), name.Contains("NO PLATE", StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void TheWayDownIsACardAndTheCARDExists()
    {
        // #590 composed with #592, which is the whole way in. A Key found on the last floor the building
        // admits to issues the authority for the band it does not — the panel never mentions the shaft, and
        // a piece of paper somebody left in a room does.
        foreach (string body in Sites())
        {
            int listedBottom = UndergroundComplex.DepthOf(body);
            int hidden = UndergroundComplex.UnlistedBandOf(body);

            Assert.True(UndergroundComplex.SiteHasBand(body, hidden),
                $"{body}: no card can ever be issued for the band that exists.");

            UndergroundComplex.AuthorityCard? card = UndergroundComplex.CardInRoom(body, listedBottom);
            Assert.NotNull(card);
            Assert.Equal(hidden, card!.Value.Band);

            // And there is no card for anything past it — the hidden band really is the bottom.
            Assert.False(UndergroundComplex.SiteHasBand(body, hidden + 1));
            Assert.Null(UndergroundComplex.CardInRoom(body, UndergroundComplex.TrueDepthOf(body)));
        }
    }

    [Fact]
    public void ReachingItPaysInINFORMATIONAndNotInABiggerNumber()
    {
        // The issue is explicit and it is the right call: a crate of credits is a number going up, and this
        // game already has the better currency. If the hidden floor paid in hardware it would be a loot room
        // with a story painted on it, and every captain would call it "the good level".
        // Measured over enough rooms to be the WEIGHTING and not the draw. A floor holds a dozen rooms, and
        // a dozen seeded rolls can invert any of these by luck — a guard that reads one floor's dice would
        // be pinning the seed, not the design, and would go red the first time anything upstream of the
        // roll changed.
        const int Sample = 600;
        foreach (string body in Sites())
        {
            int listedLevel = UndergroundComplex.DepthOf(body);
            int deepLevel = UndergroundComplex.TrueDepthOf(body);
            Assert.True(UndergroundComplex.IsUnlisted(body, deepLevel));
            Assert.False(UndergroundComplex.IsUnlisted(body, listedLevel));

            var deepHauls = new List<UndergroundComplex.Haul>(Sample);
            var shallowHauls = new List<UndergroundComplex.Haul>(Sample);
            for (int room = 0; room < Sample; room++)
            {
                deepHauls.Add(UndergroundComplex.InRoom(body, deepLevel, room));
                shallowHauls.Add(UndergroundComplex.InRoom(body, listedLevel, room));
            }

            double deepDirt = deepHauls.Count(h => h == UndergroundComplex.Haul.Dirt) / (double)deepHauls.Count;
            double upDirt = shallowHauls.Count(h => h == UndergroundComplex.Haul.Dirt) / (double)shallowHauls.Count;
            Assert.True(deepDirt > upDirt,
                $"{body}: the floor nobody listed is no heavier with files than the ones that are.");

            double deepKit = deepHauls.Count(h => h == UndergroundComplex.Haul.Equipment) / (double)deepHauls.Count;
            double upKit = shallowHauls.Count(h => h == UndergroundComplex.Haul.Equipment) / (double)shallowHauls.Count;
            Assert.True(deepKit <= upKit,
                $"{body}: the hidden floor pays better in HARDWARE, which makes it a loot room.");
        }
    }

    [Fact]
    public void ItStillEXPLAINSNothing()
    {
        // Canon holds hardest here — this is the single most tempting place in the entire game to say what
        // the Old Ones are. The deepest floor of the deepest facility may be full of evidence of an
        // enormous, well-funded, decades-long operation, and may never once name what it produced.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        var text = new List<string> { UndergroundComplex.UnlistedFloorLine };
        foreach (UndergroundComplex.Kind above in Enum.GetValues<UndergroundComplex.Kind>())
        {
            foreach (UndergroundComplex.Kind here in Enum.GetValues<UndergroundComplex.Kind>())
            {
                text.Add(UndergroundComplex.UnlistedArrivalLine(12, above, here));
            }
        }
        foreach (string body in Sites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body).Where(l => UndergroundComplex.IsUnlisted(body, l)))
            {
                text.Add(UndergroundComplex.NameOf(body, level));
                for (int room = 0; room < 12; room++)
                {
                    text.Add(UndergroundComplex.HaulLine(
                        UndergroundComplex.InRoom(body, level, room), body, level, room));
                }
            }
        }

        foreach (string line in text)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void AFloorNobodyListedIsStillAPROPERFloor()
    {
        // It is a new topology, and #587 is what a new topology costs when nobody walks it. Everything the
        // spec demands of a floor is demanded of these too — the client audit floods them for real
        // (YouCanWalkTheHiveTests reads FloorsOf now); this is the Core half.
        foreach (string body in Sites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body).Where(l => UndergroundComplex.IsUnlisted(body, l)))
            {
                UndergroundComplex.FloorPlan floor =
                    UndergroundComplex.Build(body, level, SurfaceLayout.DefaultField);

                Assert.True(floor.Walls.Count > 60, $"{body} {floor.Name}: that is a flat, not a facility.");
                Assert.True(floor.RoomCentres.Count >= 4, $"{body} {floor.Name}: too few rooms to search.");
                Assert.True(floor.Locked.Count >= 3, $"{body} {floor.Name}: nothing implies the rest of it.");

                foreach (SurfaceLayout.Wall w in floor.Walls)
                {
                    Assert.InRange(w.X1, SurfaceLayout.DefaultField.LeftX, SurfaceLayout.DefaultField.RightX);
                    Assert.InRange(w.Y1, SurfaceLayout.DefaultField.BottomY, SurfaceLayout.DefaultField.LandingBandY);
                }
            }
        }
    }

    [Fact]
    public void TheDeepCHEATRockStillHasSomethingUnderIt()
    {
        // #592 · `/map?secretlab=deep` parks a rock whose site hides a band, because the ORDINARY cheat
        // rock's site is seeded like any other and happens to be four floors of records annex with nothing
        // under it — so the feature could not be reached from a URL at all, which is the exact tax the dev
        // cheats exist to remove.
        //
        // The cheat is a BODY ID and nothing else: no Core fact is overridden from the client, the site is
        // seeded from its name like every other site. That is the right shape, and it is also the fragile
        // one — a change to the seeding could quietly leave the cheat pointing at an ordinary building and
        // nobody would notice until they went looking for a floor that was not there. So it is pinned.
        //
        // If this ever goes red: run the lab, which prints "+ an UNLISTED band to Bn" per site, pick another
        // id, and change both this test and Map.Sim's SecretLabDeepCheatBodyId.
        //
        //   dotnet run --project labs/44-a-lab-about-the-lab/Lab44.csproj -c Release
        const string CheatRock = "secret-lab-site-unlisted";

        Assert.True(UndergroundComplex.HasUnlistedBand(CheatRock),
            $"'{CheatRock}' no longer hides anything — ?secretlab=deep now lands on an ordinary facility.");

        // And it is the awkward case on purpose: as deep as the generator goes, so the cheat exercises the
        // performance guard and the band arithmetic at the same time.
        Assert.Equal(UndergroundComplex.DeepestPossibleFloor, UndergroundComplex.TrueDepthOf(CheatRock));
        Assert.NotEqual(
            UndergroundComplex.KindFor(CheatRock),
            UndergroundComplex.KindOn(CheatRock, UndergroundComplex.TrueDepthOf(CheatRock)));
    }

    /// <summary>The scenario's sites that actually have something under them. If this is ever empty the
    /// tests above pass vacuously, so it is asserted rather than assumed.</summary>
    private static IEnumerable<string> Sites()
    {
        var hiding = Bodies.Where(UndergroundComplex.HasUnlistedBand).ToList();
        Assert.NotEmpty(hiding);
        return hiding;
    }
}
