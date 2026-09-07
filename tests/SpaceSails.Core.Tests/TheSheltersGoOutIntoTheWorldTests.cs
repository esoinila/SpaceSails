using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #563 slice 3 · THE SHELTERS COME OFF THE HOME TILE.
///
/// <para>Slice 2 shipped doors, drawers and hut memory out into the lattice and stopped at the shelters,
/// with the reason stated rather than waved at: the rack state was keyed on an <c>int</c> index into ONE
/// site's list, so a list spanning a moving chunk would have silently re-pointed every reservoir on a tile
/// crossing — <i>"in the one system where getting it wrong empties a rack somebody walked to on their last
/// two hundred seconds of air."</i></para>
///
/// <para>These guard the half of the fix that lives in Core: the ground out there really does carry air, it
/// carries it at the density the field under the tube always had, the ground under the tube did not move,
/// nothing is built through a pressure drum, and no drum straddles a boundary (which is the property that
/// lets everything asking "which shelter am I in?" ask ONE tile rather than nine).</para>
///
/// <para>The behavioural half — a rack drawn down on a far tile, a chunk evicted underneath it, the rack
/// found as it was left — is in the client's <c>TheGroundRemembersWhatYouDidToItTests</c>, because that is
/// where the excursion and its dictionaries live.</para>
/// </summary>
public class TheSheltersGoOutIntoTheWorldTests
{
    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "titan", "ganymede", "callisto", "triton"];

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

    /// <summary>The tiles swept — the home tile and a spread of neighbours in every direction the world
    /// actually has. The lattice does not run up through the ship, so nothing above the top row.</summary>
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

    /// <summary>
    /// <b>THE GROUND UNDER THE TUBE DID NOT MOVE.</b> The client's home build used to pass
    /// <c>(bodyId, siteSalt, ExpeditionField())</c> to <see cref="SurfaceShelter.SpecsFor"/> by hand; it asks
    /// <see cref="SurfaceTiles.Shelters"/> now, so that one question serves the tube and the lattice alike.
    /// That is only safe if the answer at the tube is byte for byte the answer that was always there.
    ///
    /// <para>Measured against the raw call, field by field — the centre, the angle and the shell — because a
    /// shelter that moved by a metre is a beacon pointing at regolith, and a shelter whose ANGLE moved is a
    /// door in a different wall. Every landing site of every landable body in the scenario.</para>
    /// </summary>
    [Fact]
    public void TheHomeTilesShelters_AreTheOnesThatWereAlwaysThere()
    {
        int measured = 0;

        foreach ((string body, string salt) in Sites())
        {
            IReadOnlyList<SurfaceStructure.Spec> always =
                SurfaceShelter.SpecsFor(body, salt, SurfaceLayout.DefaultField);
            IReadOnlyList<SurfaceStructure.Spec> now =
                SurfaceTiles.Shelters(body, salt, SurfaceTiles.Home);

            Assert.Equal(always.Count, now.Count);
            for (int i = 0; i < always.Count; i++)
            {
                Assert.Equal(always[i].CentreX, now[i].CentreX, 9);
                Assert.Equal(always[i].CentreY, now[i].CentreY, 9);
                Assert.Equal(always[i].AngleRad, now[i].AngleRad, 9);
                Assert.Equal(always[i].Width, now[i].Width, 9);
                Assert.Equal(always[i].Height, now[i].Height, 9);
                Assert.Equal(always[i].WallThickness, now[i].WallThickness, 9);
                measured++;
            }

            // …and the seeded story a rack wakes up with. ContentSalt hands the home tile the site's own
            // salt, so "somebody was here" rolls what it has always rolled at the tube — the one thing a
            // captain would notice immediately and could never prove.
            for (int i = 0; i < always.Count; i++)
            {
                Assert.Equal(
                    SurfaceShelter.SomebodyWasHere(body, salt, i),
                    SurfaceShelter.SomebodyWasHere(
                        body, SurfaceTiles.ContentSalt(body, salt, SurfaceTiles.Home), i));
            }
        }

        Assert.True(measured > 100, $"only {measured} shelters were compared — that is not a sweep.");
    }

    /// <summary>
    /// <b>EVERY TILE CARRIES AIR, AT THE DENSITY THE TUBE'S OWN GROUND HAS.</b> #563's decision is that the
    /// ground simply carries on; a lattice whose only refuge was beside the ship would be a lattice a captain
    /// may look at and never enter, since #562 prices distance in the walk back and #564 prices it in air.
    ///
    /// <para>The rarity is not converted and never needed converting: <see cref="SurfaceShelter.CountFor"/>
    /// has been per AREA since #585 — roughly one per 9,000 du² — so the number is read off the tile's own
    /// envelope rather than chosen. Asserted against <c>CountFor</c> itself AND against the home tile's
    /// count, which is the pair that can tell pass from fail: a tile that answered with a hard-typed number
    /// would satisfy one of them and not the other.</para>
    /// </summary>
    [Fact]
    public void EveryTileCarriesAir_AtTheRarityTheGroundAtTheTubeHas()
    {
        foreach ((string body, string salt) in Sites())
        {
            int atTheTube = SurfaceTiles.Shelters(body, salt, SurfaceTiles.Home).Count;
            Assert.True(atTheTube > 0, $"{body}/{salt}: no shelter at the tube at all.");

            foreach (SurfaceTiles.Address a in Spread())
            {
                IReadOnlyList<SurfaceStructure.Spec> here = SurfaceTiles.Shelters(body, salt, a);
                Assert.True(here.Count > 0,
                    $"{body}/{salt} tile ({a.X}, {a.Y}): no air out here. A walk that cannot be refilled is a "
                    + "walk nobody takes twice.");
                Assert.Equal(
                    SurfaceShelter.CountFor(SurfaceTiles.GenerationField(body, salt, a)), here.Count);
                Assert.Equal(atTheTube, here.Count);
            }
        }
    }

    /// <summary>
    /// <b>AND THE SHELTERS OUT THERE ARE NOT ONE SHED COPIED OUTWARD.</b> The whole reason the contents are
    /// asked on <see cref="SurfaceTiles.ContentSalt"/> rather than on the site's salt: a lattice where every
    /// tile puts its drums on the same nine spots is wallpaper, for exactly the reason the ground itself
    /// would have been.
    ///
    /// <para>Measured on the SEQUENCE of places, not on a count — a guard that compared counts would pass
    /// happily on a lattice of identical tiles, which is this project's fifth named bug class.</para>
    /// </summary>
    [Fact]
    public void TwoTiles_DoNotStandTheirSheltersOnTheSameGround()
    {
        foreach ((string body, string salt) in Sites())
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int counted = 0;

            foreach (SurfaceTiles.Address a in Spread())
            {
                // Relative to the tile's own corner, so two tiles holding a genuinely identical arrangement
                // read as identical here rather than being told apart by their addresses.
                (double leftX, double _, double bottomY, double _2) = SurfaceTiles.Rect(a);
                string plan = string.Join(",", SurfaceTiles.Shelters(body, salt, a)
                    .Select(s => $"{s.CentreX - leftX:F1}:{s.CentreY - bottomY:F1}"));
                counted++;
                seen.Add(plan);
            }

            Assert.True(seen.Count * 2 > counted,
                $"{body}/{salt}: only {seen.Count} of {counted} tiles arrange their shelters differently.");
        }
    }

    /// <summary>
    /// <b>NO DRUM STRADDLES ITS OWN TILE.</b> This is the property the whole client-side economy rests on:
    /// <c>ShelterUnderfoot</c> and #585's threshold rule ask the tile under the boots and no other, which is
    /// nine <c>Contains()</c> per hunter per frame instead of eighty-one. If a shelter could hang over a
    /// boundary, a captain could stand in one the game says they are not in — and the Old Ones could walk
    /// into it.
    ///
    /// <para>It is true because of two numbers chosen a long way apart — the placer's 24 du margin
    /// (<see cref="SurfaceShelter.PlacesOn"/>) and a drum's own keep-out radius, under 20
    /// (<see cref="SurfaceStructure.KeepOutRadius"/>) — so it is measured rather than assumed, on the
    /// inflated footprint at any angle.</para>
    /// </summary>
    [Fact]
    public void NoShelterHangsOverTheEdgeOfItsOwnTile()
    {
        int measured = 0;

        foreach ((string body, string salt) in Sites())
        {
            foreach (SurfaceTiles.Address a in Spread())
            {
                (double leftX, double rightX, double bottomY, double topY) = SurfaceTiles.Rect(a);
                foreach (SurfaceStructure.Spec spec in SurfaceTiles.Shelters(body, salt, a))
                {
                    double r = SurfaceStructure.KeepOutRadius(spec);
                    Assert.True(
                        spec.CentreX - r > leftX && spec.CentreX + r < rightX
                        && spec.CentreY - r > bottomY && spec.CentreY + r < topY,
                        $"{body}/{salt} tile ({a.X}, {a.Y}): a shelter at ({spec.CentreX:F0}, "
                        + $"{spec.CentreY:F0}) reaches over its tile's edge. Every caller that asks one tile "
                        + "which shelter it is standing in would miss it.");
                    measured++;
                }
            }
        }

        Assert.True(measured > 500, $"only {measured} shelters were measured — that is not a sweep.");
    }

    /// <summary>
    /// <b>NOTHING IS BUILT THROUGH A PRESSURE DRUM.</b> #585's ledger
    /// (<see cref="SurfaceLayout.StandingClaims"/>) claims the shelters FIRST and never yields them, and its
    /// reason is the asymmetry: <i>a hut that has to move is a cosmetic loss, a shelter that has to move is a
    /// captain who dies looking for it.</i> Slice 2 generated every tile out in the world with no ledger at
    /// all, because it had no shelters on it to claim.
    ///
    /// <para>Measured on the ground the lattice actually lays — a generated building's own centre inside a
    /// shelter's keep-out radius — and the home tile is swept alongside so the guard is asking the same
    /// question of both grounds.</para>
    /// </summary>
    [Fact]
    public void TheGeneratorKeepsOffTheShelters_OnEveryTile()
    {
        int checkedTiles = 0;

        foreach ((string body, string salt) in Sites())
        {
            foreach (SurfaceTiles.Address a in Spread())
            {
                IReadOnlyList<SurfaceStructure.Spec> drums = SurfaceTiles.Shelters(body, salt, a);
                IReadOnlyList<(double X, double Y)> centres =
                    SurfaceTiles.Ground(body, salt, a).BuildingCentres ?? [];
                checkedTiles++;

                foreach ((double bx, double by) in centres)
                {
                    foreach (SurfaceStructure.Spec drum in drums)
                    {
                        double dx = bx - drum.CentreX, dy = by - drum.CentreY;
                        Assert.True(
                            Math.Sqrt((dx * dx) + (dy * dy)) >= SurfaceStructure.KeepOutRadius(drum),
                            $"{body}/{salt} tile ({a.X}, {a.Y}): a ruin stands inside the shelter at "
                            + $"({drum.CentreX:F0}, {drum.CentreY:F0}). The one building on this ground that "
                            + "has to be findable is the one with somebody's house through it.");
                    }
                }
            }
        }

        Assert.True(checkedTiles > 100, $"only {checkedTiles} tiles were swept.");
    }

    /// <summary>
    /// <b>#573'S IDIOM SURVIVES THE TRIP OUT, AND SURVIVES BEING FORGOTTEN.</b> <i>"They replenish
    /// themselves automatically"</i> — so a rack that is NOT full means somebody was here, which is a fact
    /// about the world told by state rather than by a card.
    ///
    /// <para>On a far tile that fact is asked on the tile's own contents salt, so it is a property of THAT
    /// GROUND: regenerate the tile from its address after any number of evictions, saves and lift-offs and
    /// the same racks are found part-drawn. This is why none of it rides the vault — the seeded half comes
    /// back by regeneration, and the drawn-down half must not come back at all, since
    /// <see cref="SurfaceShelter.RechargeSeconds"/> is two minutes and any return trip is very much
    /// longer.</para>
    ///
    /// <para>Both halves are asserted, and the second is the one that can tell pass from fail: the story
    /// must be STABLE for one tile and DIFFERENT between tiles. A roll that ignored the salt would satisfy
    /// stability and hand every tile in the lattice the same nine part-empty racks.</para>
    /// </summary>
    [Fact]
    public void APartlyDrawnRackIsAFactAboutTheGround_AndTheGroundIsTheTile()
    {
        foreach ((string body, string salt) in Sites().Take(10))
        {
            var stories = new HashSet<string>(StringComparer.Ordinal);
            int tiles = 0;

            foreach (SurfaceTiles.Address a in Spread())
            {
                string content = SurfaceTiles.ContentSalt(body, salt, a);
                int count = SurfaceTiles.Shelters(body, salt, a).Count;

                string told = string.Concat(Enumerable.Range(0, count)
                    .Select(i => SurfaceShelter.SomebodyWasHere(body, content, i) ? "1" : "0"));

                // Asked twice, from nothing but the address — which is what "it comes back by regeneration"
                // means. A cache would make this trivially true; there is no cache.
                Assert.Equal(told, string.Concat(Enumerable.Range(0, count)
                    .Select(i => SurfaceShelter.SomebodyWasHere(
                        body, SurfaceTiles.ContentSalt(body, salt, a), i) ? "1" : "0")));

                stories.Add(told);
                tiles++;
            }

            Assert.True(stories.Count > 2,
                $"{body}/{salt}: {tiles} tiles tell {stories.Count} different stories about who has been "
                + "through. Every rack in the lattice is reading off one roll.");
        }
    }
}
