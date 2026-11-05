using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1394 · <b>THE HALLS NOBODY DUG AND THE LOBBY NOBODY LISTED CARRY NO DEPARTMENT PAPER BIN.</b> They breathe, so
/// the pressure rule let <c>CarveBins</c> fit a <c>PAPER · FLATTEN AND STACK · EMPTIED DAILY</c> bin beside the
/// car: a janitorial schedule in a place with no janitor. The guard walks every found floor and every unlisted
/// lobby on a sweep of generated grounds (a found band is about one site in fifty, so a small sample proves
/// nothing) and demands zero bins; the anti-vacuous half demands that the listed floors on the same grounds
/// still carry bins, so "no bins anywhere" cannot pass it.
///
/// <para><b>Proven RED</b> by removing the found/unlisted-lobby line from <c>CarveBins</c>: bins on found
/// floors and unlisted lobbies.</para>
/// </summary>
public sealed class TheHallsNobodyDugKeepNoPaperBinTests
{
    private const int Probes = 2500;

    [Fact]
    public void NoFoundFloorAndNoUnlistedLobbyCarriesABin()
    {
        var wrong = new List<string>();
        int found = 0, lobbies = 0, listedWithBins = 0;

        for (int i = 0; i < Probes; i++)
        {
            string body = $"paper-bin-ground-{i}";
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                bool isFound = UndergroundComplex.IsFound(body, level);
                bool isLobby = UndergroundComplex.IsUnlistedLobby(body, level);
                int bins = UndergroundComplex.Build(body, level, SurfaceLayout.DefaultField).TheBins.Count;

                if (isFound || isLobby)
                {
                    found += isFound ? 1 : 0;
                    lobbies += isLobby ? 1 : 0;
                    if (bins > 0)
                    {
                        wrong.Add($"{body} B{-level}: {bins} bin(s) on a {(isFound ? "found hall" : "unlisted lobby")}");
                    }
                }
                else if (bins > 0)
                {
                    listedWithBins++;
                }
            }
        }

        Assert.True(found > 20 && lobbies > 20, $"only {found} found floors and {lobbies} lobbies swept — proves little.");
        Assert.True(listedWithBins > 0, "no listed floor carried a bin either - the sweep reads nothing.");
        Assert.True(wrong.Count == 0, string.Join("\n", wrong.Take(10)));
    }
}
