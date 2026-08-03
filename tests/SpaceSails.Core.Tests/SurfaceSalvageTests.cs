using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>#573 · What is still in the ruins — the "services" half of the owner's report that they "were
/// just missing the services and the doors".</summary>
public class SurfaceSalvageTests
{
    private static IEnumerable<(string Body, string Salt)> Sites() =>
        new[] { "miranda", "luna", "phobos", "europa", "titan", "triton" }
            .SelectMany(b => LandingSites.For(b).Select(s => (b, s.LayoutSalt)));

    [Fact]
    public void MostRuinsAreEmpty_WhichIsWhatMakesTheRestWorthTheWalk()
    {
        // If every building paid out, entering them would stop being a decision and become a chore you
        // perform on all of them. The empty ones are load-bearing.
        var finds = new List<SurfaceSalvage.Find>();
        foreach ((string body, string salt) in Sites())
        {
            for (int i = 0; i < 40; i++)
            {
                finds.Add(SurfaceSalvage.WhatIsInside(body, salt, i));
            }
        }

        double emptyShare = finds.Count(f => f == SurfaceSalvage.Find.Nothing) / (double)finds.Count;
        Assert.InRange(emptyShare, 0.35, 0.70);
    }

    [Fact]
    public void EveryKindOfFindActuallyOccurs()
    {
        // A find that never comes up is dead content that still cost the walk to look for.
        var finds = new HashSet<SurfaceSalvage.Find>();
        foreach ((string body, string salt) in Sites())
        {
            for (int i = 0; i < 40; i++)
            {
                finds.Add(SurfaceSalvage.WhatIsInside(body, salt, i));
            }
        }
        Assert.Equal(Enum.GetValues<SurfaceSalvage.Find>().Length, finds.Count);
    }

    [Fact]
    public void ARuinNeverHoldsAWholeMagazine()
    {
        // #563's partial-cache law, again: found ammunition extends a trip, it does not resupply one. The
        // shelters' lockers are the real thing, and a drawer in a ruin must not compete with them.
        foreach ((string body, string salt) in Sites())
        {
            for (int i = 0; i < 20; i++)
            {
                Assert.True(SurfaceSalvage.RoundsIn(body, salt, i) < SentryBot.MaxMagazine / 2);
            }
        }
    }

    [Fact]
    public void ItIsDeterministic_SoARuinYouLeftIsTheRuinYouComeBackTo()
    {
        foreach ((string body, string salt) in Sites())
        {
            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(SurfaceSalvage.WhatIsInside(body, salt, i), SurfaceSalvage.WhatIsInside(body, salt, i));
                Assert.Equal(SurfaceSalvage.GoodsIn(body, salt, i), SurfaceSalvage.GoodsIn(body, salt, i));
            }
        }
    }

    [Fact]
    public void ThePapersNeverExplainWhatIsOutside()
    {
        // The canon rule, the third time it has had to be held: the Old Ones are never stated in a card. A
        // roster, a docket, a note in a locker door — the player assembles it or they do not.
        string[] forbidden = ["REEVER", "OLD ONE", "RESTORE", "BACKUP", "REVIV", "CLONE", "CREATURE"];
        foreach ((string body, string salt) in Sites())
        {
            for (int i = 0; i < 20; i++)
            {
                string papers = SurfaceSalvage.PapersLine(body, salt, i).ToUpperInvariant();
                foreach (string wrong in forbidden)
                {
                    Assert.DoesNotContain(wrong, papers, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void EveryFindThatExistsHasALabelToPressEOn()
    {
        foreach (SurfaceSalvage.Find find in Enum.GetValues<SurfaceSalvage.Find>())
        {
            if (find == SurfaceSalvage.Find.Nothing)
            {
                continue;
            }
            Assert.False(string.IsNullOrWhiteSpace(SurfaceSalvage.LabelFor(find)));
        }
    }
}
