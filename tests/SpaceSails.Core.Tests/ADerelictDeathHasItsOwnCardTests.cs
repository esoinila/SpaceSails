using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #621 · THE PICTURE ON A DEATH CARD IS A SENTENCE TOO.
///
/// <para>#574 established the law — <i>"the place decides the picture; the cause decides the words"</i> — and
/// then only ever wrote the words. Walking every cause × place and LOOKING at the file each one resolves to
/// found three faults on one place, <see cref="DeathPlace.Derelict"/>, which had no card of its own:</para>
///
/// <list type="bullet">
/// <item><b>Reevers aboard a hull → <c>death-reevers.jpg</c></b>, which is boot prints in regolith with an
/// open chest and an Earth in the sky — shown directly under the pool's own line, <i>"No dust to leave a
/// mark in — just a corridor, and then not you."</i> The prose denies the dust the picture is made of.</item>
/// <item><b>Joined aboard a hull → <c>death-joined.jpg</c></b>, a crowd of Old Ones standing on a moon under
/// a moonlit sky, shown under <i>"…stopped moving deep inside {body}"</i>.</item>
/// <item><b>Suffocated aboard a hull → <c>death-suffocated.jpg</c>, a file this game has never shipped.</b>
/// A broken image on a death card, and nobody could have found it by playing, because a second bug
/// (<c>AwayTeamSide</c>) meant the tank could not run out aboard a derelict at all.</item>
/// </list>
///
/// <para>So there are two guards here and they are different in kind. One asks whether the words and the
/// picture agree; the other asks whether the picture EXISTS — because a name that resolves to nothing passes
/// every string assertion ever written about it. <c>EveryCause_HasItsOwnArt</c> checked that the suffocation's
/// art was not one of the BUSTED frames and was perfectly happy with a file that was not there. The world
/// handed to that test could not tell pass from fail.</para>
/// </summary>
public class ADerelictDeathHasItsOwnCardTests
{
    private static readonly DeathCause[] AllCauses = Enum.GetValues<DeathCause>();
    private static readonly DeathPlace[] AllPlaces = Enum.GetValues<DeathPlace>();

    /// <summary>The live art directory, found by walking up from the test assembly — the same idiom
    /// <c>PlotPathExplosionTests</c> uses to read a repo file. The pictures are what the player sees; a copy
    /// beside the test binary would be a different world.</summary>
    private static string ArtDir
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                string candidate = Path.Combine(dir, "src", "SpaceSails.Client", "wwwroot", "art");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("src/SpaceSails.Client/wwwroot/art not found above the test assembly");
        }
    }

    [Fact]
    public void EveryDeathTheGameCanStage_ResolvesToAPictureThatEXISTS()
    {
        // The one that would have caught it. Every (cause, place) the law permits, plus the placeless
        // overload for every cause, must name a file that is actually in wwwroot/art — there is no onerror
        // fallback on .bf-art, so a missing file is a broken-image icon in the middle of the death card.
        string art = ArtDir;
        var bad = new List<string>();

        foreach (DeathCause cause in AllCauses)
        {
            string placeless = DeathNarration.ArtFile(cause);
            if (!File.Exists(Path.Combine(art, placeless)))
            {
                bad.Add($"  {cause} (no place): art/{placeless} does not exist.");
            }

            foreach (DeathPlace place in AllPlaces)
            {
                if (!DeathNarration.CanHappen(cause, place))
                {
                    continue; // the law says this death cannot occur here; it needs no picture
                }

                string file = DeathNarration.ArtFile(cause, place);
                if (!File.Exists(Path.Combine(art, file)))
                {
                    bad.Add($"  {cause} on a {place}: art/{file} does not exist.");
                }
            }
        }

        Report(bad, "deaths whose card points at a picture the game does not ship");
    }

    [Fact]
    public void ADeathAboardAHull_IsNeverShownTheGroundsPicture()
    {
        // The #574 law, applied to the pixels. A derelict has no regolith, no sky, no chest sitting in the
        // dust and no crowd walking across it, so it must not be handed a card that is made of those.
        string[] theGrounds =
        [
            DeathNarration.ArtFile(DeathCause.Reevers, DeathPlace.LandingParty),
            DeathNarration.ArtFile(DeathCause.Joined, DeathPlace.LandingParty),
            DeathNarration.ArtFile(DeathCause.Suffocated, DeathPlace.LandingParty),
            "death-reevers.jpg",
            "death-joined.jpg",
            "death-landing-party.jpg",
        ];

        var bad = new List<string>();
        foreach (DeathCause cause in AllCauses.Where(c => DeathNarration.CanHappen(c, DeathPlace.Derelict)))
        {
            string file = DeathNarration.ArtFile(cause, DeathPlace.Derelict);
            if (theGrounds.Contains(file))
            {
                bad.Add($"  {cause} aboard a derelict is shown art/{file}, which is a picture of a moon.");
            }

            // …nor the Hive's. Poured concrete under a moon is not a steel hull in space either — #609's
            // own reasoning, pointed the other way.
            if (file == DeathNarration.ArtFile(cause, DeathPlace.Underground))
            {
                bad.Add($"  {cause} aboard a derelict borrows the Hive's card (art/{file}).");
            }
        }

        Report(bad, "derelict deaths wearing somebody else's picture");
    }

    [Fact]
    public void EveryPlaceThatCanKillYou_HasACardOfItsOwn()
    {
        // A place with no picture of its own does not fail loudly — it silently inherits whichever place is
        // tested first in the chain, which is exactly how the away team's card ended up under a moon (#609)
        // and inside a hull (this one). Two places sharing one file is the tell.
        var byPlace = new Dictionary<DeathPlace, HashSet<string>>();
        foreach (DeathPlace place in AllPlaces)
        {
            byPlace[place] =
            [
                .. AllCauses.Where(c => DeathNarration.CanHappen(c, place))
                    .Select(c => DeathNarration.ArtFile(c, place)),
            ];
        }

        // The ground, the hull and the Hive are three different worlds and must not share a single frame.
        // (OwnShip is exempt: its two causes legitimately reuse the BUSTED frames, which predate the set.)
        DeathPlace[] awayPlaces = [DeathPlace.LandingParty, DeathPlace.Derelict, DeathPlace.Underground];
        var bad = new List<string>();
        foreach (DeathPlace a in awayPlaces)
        {
            foreach (DeathPlace b in awayPlaces.Where(p => p != a))
            {
                foreach (string shared in byPlace[a].Intersect(byPlace[b]))
                {
                    bad.Add($"  {a} and {b} both show art/{shared}.");
                }
            }
        }

        Report(bad, "places that share a death card");
    }

    [Fact]
    public void TheVoidIsNotAPlaceYouCanStandIn()
    {
        // "no beacon, no body, just the long dark"; "There was no ground to hit". Void was falling through
        // CanHappen's `_ => true` default, which made it legal on a moon, inside a wreck, and a hundred and
        // fifty metres under a moon — three worlds its own prose flatly denies. A default that absorbs a gap
        // is the shape #609 named.
        Assert.True(DeathNarration.CanHappen(DeathCause.Void, DeathPlace.OwnShip));
        foreach (DeathPlace standing in new[]
                 { DeathPlace.LandingParty, DeathPlace.Derelict, DeathPlace.Underground })
        {
            Assert.False(DeathNarration.CanHappen(DeathCause.Void, standing),
                $"a captain standing on a {standing} has ground under them; the void has none.");
        }
    }

    private static void Report(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad.Distinct().Take(40))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}
