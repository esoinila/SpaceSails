using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #325 / #332 · THE HAVEN CHANDLERY — what it sells, what it charges, and the law that makes the tank
/// worth buying: every instrument, radius and sentence about air reads ONE function.
///
/// <para>The dangerous half of this feature is not the shop. It is that an excursion's air budget stopped
/// being a constant, and six places in <c>src/</c> were multiplying or dividing by that constant to answer
/// "how big is this walk". A tank that doubles the budget while any one of them stays on
/// <see cref="SuitAir.TankSeconds"/> is this project's third named bug class — the sim doing one thing
/// while a drawn shape or a sentence reports another — and it would have shipped as a captain with air in
/// the bottle being told by the suit that the tank does not reach the tube.</para>
///
/// <para>So the first guard here is a SOURCE SWEEP rather than a behaviour test: behaviour tests can only
/// catch the readers somebody remembered to write a test for, and the whole failure mode is the reader
/// nobody remembered.</para>
/// </summary>
public sealed class TheChandlerySellsMarginTests
{
    private static readonly Barkeep[] Houses = [.. Barkeeps.AllBarkeeps];

    // ══ LAW 1 · ONE FUNCTION, AND THE SWEEP THAT SAYS SO ════════════════════════════════════════════════

    /// <summary>The one file allowed to read the constant: the one that defines it, and therefore the one
    /// that defines <see cref="SuitAir.PlayBudget"/> off it. Everything else asks the function.</summary>
    private const string TheOnlyAllowedReader = "src/SpaceSails.Core/SuitAir.cs";

    /// <summary>
    /// #325 · NOTHING IN <c>src/</c> READS THE TANK CONSTANT EXCEPT THE FILE THAT OWNS IT.
    ///
    /// <para>Written as a sweep because the bug is an OMISSION. Six readers had to move for the extended
    /// tank to mean anything (the backstop radius, the tile lattice's extent, the meter's full mark, the
    /// shelter rack's fill cap, its gauge line, the boot cheat's clamp) and a seventh added next month would
    /// reintroduce the fault silently — a captain with a bought bottle and one instrument quietly measuring
    /// a bottle they do not have.</para>
    ///
    /// <para><b>Proven RED</b> by putting one reader back — <c>SurfaceTiles.BackstopRadiusDu</c> returning
    /// <c>SuitAir.TankSeconds * SuitAir.WalkSpeedDu</c> — and watching this name the file and the line, then
    /// restored.</para>
    /// </summary>
    [Fact]
    public void EveryReaderOfTheAirBudget_AsksTheOneFunction_AndNotTheConstant()
    {
        string root = TestTree.RepoRoot();
        List<string> offenders = [];

        foreach (string full in Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(root, full).Replace('\\', '/');
            if (rel.Contains("/obj/", StringComparison.Ordinal) ||
                rel.Contains("/bin/", StringComparison.Ordinal) ||
                !(rel.EndsWith(".cs", StringComparison.Ordinal) ||
                  rel.EndsWith(".razor", StringComparison.Ordinal)) ||
                rel == TheOnlyAllowedReader)
            {
                continue;
            }

            string[] lines = File.ReadAllLines(full);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!line.Contains("SuitAir.TankSeconds", StringComparison.Ordinal))
                {
                    continue;
                }

                // A <see cref="SuitAir.TankSeconds"/> in a comment is a REFERENCE, not a reader — the
                // constant is still a real thing and prose is allowed to name it. Only code counts.
                string code = line.TrimStart();
                if (code.StartsWith("///", StringComparison.Ordinal) ||
                    code.StartsWith("//", StringComparison.Ordinal) ||
                    code.StartsWith("*", StringComparison.Ordinal))
                {
                    continue;
                }

                offenders.Add($"{rel}:{i + 1} — {line.Trim()}");
            }
        }

        Assert.True(offenders.Count == 0,
            "#325 · these read the STANDARD tank where they should be reading this excursion's budget "
            + $"(SuitAir.PlayBudget). A captain who bought a bottle has an instrument measuring one they do "
            + $"not have:{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", offenders));
    }

    // ══ LAW 2 · THE BOTTLE MOVES THE WORLD, AND MOVES ALL OF IT TOGETHER ════════════════════════════════

    /// <summary>The distance from the tube at which a captain who started full has just crossed the point
    /// of no return — asked of <see cref="SuitAir.PastPointOfNoReturn"/> by walking, never by inverting the
    /// formula here. A second arithmetic for one fact is the thing this whole PR is about.</summary>
    private static double PointOfNoReturnDu(double budgetSeconds)
    {
        double air = budgetSeconds;
        double outDu = 0;
        const double stepDu = 0.5;

        while (!SuitAir.PastPointOfNoReturn(air, outDu) && air > 0)
        {
            outDu += stepDu;
            air -= stepDu / SuitAir.WalkSpeedDu;
        }
        return outDu;
    }

    /// <summary>
    /// #325 · A FITTED TANK DOUBLES THE BACKSTOP AND THE TURN-BACK POINT, AND LEAVES THE RATIO BETWEEN THEM
    /// EXACTLY WHERE IT WAS.
    ///
    /// <para>Both halves matter. The doubling is the purchase; the ratio is Fable's second sentence — <i>"the
    /// walk back is still half"</i> — which is the promise that stops a bigger bottle being a licence to go
    /// twice as far and turn round at the same moment. If the backstop moved and the line did not, a captain
    /// would have bought ground they cannot come back from.</para>
    ///
    /// <para><b>Proven RED</b> twice, because there are two claims. Making <see cref="SuitAir.PlayBudget"/>
    /// ignore its argument reddened the doubling assertions. Giving <c>NeededToGetHome</c> a margin of its
    /// own (1.30 in place of <see cref="SuitAir.ReserveFactor"/>) left every doubling green and reddened
    /// the ratio assertions alone — the half a single guard would have missed.</para>
    /// </summary>
    [Fact]
    public void AFittedTank_DoublesTheBackstopAndTheTurnBackPoint_InTheSameRatio()
    {
        double standard = SuitAir.PlayBudget(extendedTank: false);
        double extended = SuitAir.PlayBudget(extendedTank: true);

        Assert.Equal(standard * SuitAir.ExtendedTankFactor, extended, 6);

        double standardBackstop = SurfaceTiles.BackstopRadiusDu(standard);
        double extendedBackstop = SurfaceTiles.BackstopRadiusDu(extended);
        Assert.Equal(standardBackstop * SuitAir.ExtendedTankFactor, extendedBackstop, 6);

        double standardLine = PointOfNoReturnDu(standard);
        double extendedLine = PointOfNoReturnDu(extended);
        Assert.True(standardLine > 0, "the turn-back point is not reachable on a standard tank at all");
        Assert.Equal(standardLine * SuitAir.ExtendedTankFactor, extendedLine, 0);

        // …and the walk back is still half, in the one sense the fiction means it. Stated against
        // ReserveFactor rather than against the other measurement, because "the two ratios agree" is
        // already implied by the two doublings above and would be a green assertion that asserts nothing.
        // This one is an independent claim: every deck unit walked out is paid for once going and once
        // coming back at the reserve rate, so the turn-back point sits at 1/(1+ReserveFactor) of the
        // backstop — and it does so on BOTH bottles.
        Assert.Equal(1.0 / (1.0 + SuitAir.ReserveFactor), standardLine / standardBackstop, 3);
        Assert.Equal(1.0 / (1.0 + SuitAir.ReserveFactor), extendedLine / extendedBackstop, 3);
    }

    /// <summary>
    /// #325 · THE RESERVE IS NOT FOR SALE. A chandlery sells margin, not a longer grace period — the EMU's
    /// half-hour secondary pack is the one honest thing left when the arithmetic has gone wrong, and a
    /// reserve that scaled with the purse would have made the game's last mercy a function of money.
    ///
    /// <para><b>Proven RED</b> by defining <c>ReserveSeconds</c> off the extended budget: the equality
    /// below failed at 75 s vs 150 s.</para>
    /// </summary>
    [Fact]
    public void TheBottleBuysWalking_NeverASecondOfTheReserve()
    {
        double before = SuitAir.ReserveSeconds;

        Assert.Equal(before, SuitAir.FullSecondsWith(extendedTank: false) - SuitAir.PlayBudget(false), 6);
        Assert.Equal(before, SuitAir.FullSecondsWith(extendedTank: true) - SuitAir.PlayBudget(true), 6);
    }

    /// <summary>#325 · The tile lattice reaches as far as the suit is willing to walk. A world that stopped
    /// at the standard radius while the backstop allowed twice it is a captain walking off the ground with
    /// air in the bottle — which is the one failure the backstop exists to prevent.
    ///
    /// <para><b>Proven RED</b> by leaving <c>SurfaceTiles.Chunk</c> reading the standard budget: a tile at
    /// 15,000 du was outside the world and inside the suit's permission at the same time.</para></summary>
    [Fact]
    public void TheGroundReachesAsFarAsTheBottleDoes()
    {
        double standard = SuitAir.PlayBudget(extendedTank: false);
        double extended = SuitAir.PlayBudget(extendedTank: true);

        // A tile beyond the standard backstop but inside the extended one. Addressed off the lattice's own
        // arithmetic rather than a typed coordinate, so a re-tuned tile size cannot make this vacuous.
        double between = (SurfaceTiles.BackstopRadiusDu(standard) + SurfaceTiles.BackstopRadiusDu(extended)) / 2.0;
        (double hx, double hy) = SurfaceTiles.TubeMouth();
        SurfaceTiles.Address far = SurfaceTiles.At(hx, hy - between);

        Assert.False(SurfaceTiles.WithinBackstop(far, standard),
            "the guard is vacuous: that tile is inside the STANDARD backstop too");
        Assert.True(SurfaceTiles.WithinBackstop(far, extended),
            "a captain with an extended tank is allowed to walk to ground the world does not generate");

        // And the suit agrees with the world at the same spot, from every bearing.
        for (int i = 0; i < 12; i++)
        {
            double bearing = i * Math.Tau / 12.0;
            double x = hx + (Math.Cos(bearing) * between);
            double y = hy + (Math.Sin(bearing) * between);
            Assert.True(SurfaceEdge.BeyondBackstop("miranda", "", x, y, standard));
            Assert.False(SurfaceEdge.BeyondBackstop("miranda", "", x, y, extended));
        }
    }

    // ══ LAW 3 · THE PRICE IS THE HAVEN'S OWN ════════════════════════════════════════════════════════════

    /// <summary>
    /// #325/#332 · EVERY PRICE IS READ OFF THIS HAVEN'S BAR CARD. Not "is a plausible number" — is exactly
    /// the number the house already charges, times the factor the bottle already carries.
    ///
    /// <para>Checked at every haven, because a price that happened to agree at one berth and was typed
    /// rather than derived would pass a single-house test. The spread across houses is asserted too: if the
    /// prices were constants, every haven would quote the same figure.</para>
    ///
    /// <para><b>Proven RED</b> by typing a flat price into <c>ExtendedTankPrice</c>: the per-house equality
    /// failed at the Roadstead and the spread assertion failed as well.</para>
    /// </summary>
    [Fact]
    public void ThePriceIsWhatThisHouseAlreadyCharges()
    {
        Assert.True(Houses.Length >= 4, "too few houses for this guard to mean anything");

        var tankPrices = new List<int>();
        foreach (Barkeep house in Houses)
        {
            Assert.Equal(house.RoundPrice * SuitAir.ExtendedTankFactor, Chandlery.ExtendedTankPrice(house));
            tankPrices.Add(Chandlery.ExtendedTankPrice(house));

            for (int pills = 0; pills <= Chandlery.MedKitFullStock; pills++)
            {
                Assert.Equal(pills * house.DrinkPrice, Chandlery.MedKitRefillPrice(house, pills));
            }

            // A full cabinet costs nothing to top up, and an over-full one cannot be charged for.
            Assert.Equal(0, Chandlery.MedKitRefillPrice(house, 0));
            Assert.Equal(
                Chandlery.MedKitRefillPrice(house, Chandlery.MedKitFullStock),
                Chandlery.MedKitRefillPrice(house, Chandlery.MedKitFullStock + 9));
        }

        Assert.True(tankPrices.Distinct().Count() > 1,
            "every haven quotes the same price for a tank — that is a typed figure wearing a function's "
            + "clothes, not a price read off the house card");
    }

    /// <summary>#332 · The shortfall the row quotes is the shortfall the press racks. One number, asked of
    /// one function, at every state a cabinet can be in.</summary>
    [Fact]
    public void TheRefillRacksExactlyWhatItCharged()
    {
        for (int inCabinet = 0; inCabinet <= Chandlery.MedKitFullStock; inCabinet++)
        {
            int missing = Chandlery.MedKitPillsMissing(inCabinet);
            Assert.Equal(Chandlery.MedKitFullStock, inCabinet + missing);

            foreach (Barkeep house in Houses)
            {
                Assert.Equal(missing * house.DrinkPrice, Chandlery.MedKitRefillPrice(house, missing));
            }
        }

        Assert.Equal(0, Chandlery.MedKitPillsMissing(Chandlery.MedKitFullStock + 4));
        Assert.Equal(Chandlery.MedKitFullStock, Chandlery.MedKitPillsMissing(-3));
    }

    // ══ LAW 4 · THE PROSE ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>§8's reserved list, as the burial guard holds it: words that would settle a question the
    /// game exists to leave open. A counter selling bottles has no business near any of them.</summary>
    private static readonly string[] Forbidden =
    [
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
        "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
    ];

    /// <summary>
    /// #325/#332 · THE TWO AUTHORED LINES ARE WHAT FABLE WROTE, ARE ENUMERATED, AND SAY NOTHING RESERVED.
    ///
    /// <para>The cabinet's old line ended <i>"(Restock is a later lane.)"</i> — the game telling a captain
    /// about the backlog rather than about the world, and the single most direct thing #332 asked to be
    /// removed. It is asserted GONE by name, because "the string changed" is not the same claim.</para>
    ///
    /// <para><b>Proven RED</b> by restoring the old parenthetical: the backlog assertion failed. And by
    /// planting a reserved word in the fitted line: the sweep named it.</para>
    /// </summary>
    [Fact]
    public void TheAuthoredLinesAreVerbatim_AndSayNothingTheyMayNot()
    {
        Assert.Equal(
            "Extended tank fitted. The arithmetic is kinder today: twice the walk, and the walk back is "
            + "still half.",
            Chandlery.TankFittedLine);

        Assert.Equal(
            "MED KIT: the pill cabinet is empty — the calming stock is spent. Any haven's chandlery sells "
            + "the refill.",
            Chandlery.CabinetEmptyLine);

        Assert.DoesNotContain("later lane", Chandlery.CabinetEmptyLine, StringComparison.OrdinalIgnoreCase);

        Assert.Equal("EXTENDED TANK", Chandlery.ExtendedTankPlate);
        Assert.Equal("MED-KIT REFILL", Chandlery.MedKitRefillPlate);

        IReadOnlyList<string> prose = Chandlery.AllProse();
        Assert.Contains(Chandlery.TankFittedLine, prose);
        Assert.Contains(Chandlery.CabinetEmptyLine, prose);
        Assert.Contains(Chandlery.ExtendedTankPlate, prose);
        Assert.Contains(Chandlery.MedKitRefillPlate, prose);

        List<string> offenders = [];
        foreach (string line in prose.Concat([Chandlery.TankReceiptLine(80, 2), Chandlery.RefillReceiptLine(3, 21)]))
        {
            foreach (string word in Forbidden)
            {
                if (line.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add($"\"{word}\" in: {line}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "#325/#332 · the chandlery names something §8 reserves:" + Environment.NewLine + "  "
            + string.Join(Environment.NewLine + "  ", offenders));
    }

    /// <summary>#332 · The empty cabinet says where the refill is — which is the whole of what replaced the
    /// backlog note, and the only part of the line that is a fact about the world.</summary>
    [Fact]
    public void TheEmptyCabinetNamesWhereTheRefillIs()
    {
        Assert.Contains("chandlery", Chandlery.CabinetEmptyLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("haven", Chandlery.CabinetEmptyLine, StringComparison.OrdinalIgnoreCase);
    }

    // ══ LAW 5 · THE STOCK COUNTS RIDE THE VAULT ═════════════════════════════════════════════════════════

    /// <summary>
    /// #325/#332 · BOTH COUNTS SURVIVE A SAVE — and an EMPTY cabinet survives as empty.
    ///
    /// <para>That second half is the guard with teeth. <c>MedKitPills</c> is nullable precisely so a file
    /// written before this lane (which genuinely has nothing to say about pills) can be told apart from one
    /// that records a spent cabinet. A plain int would have loaded every old save's zero as "empty" —
    /// punishing returning captains — and any fix that rounded zero up to full would have handed every
    /// captain a free restock on reload, which is the exploit the heat section exists to refuse.</para>
    ///
    /// <para><b>Proven RED</b> by dropping <c>ExtendedTanks</c> from the round-trip and by making the
    /// section's pills non-nullable: the first failed on the tank count, the second could no longer tell an
    /// old file from an empty cabinet.</para>
    /// </summary>
    [Fact]
    public void TheStockCountsRoundTripTheVault()
    {
        var saved = new Vault
        {
            Ship = new ShipSection { ExtendedTanks = 3, MedKitPills = 0 },
        };

        string json = VaultSerializer.Save(saved);
        Vault loaded = VaultSerializer.Load(json);

        Assert.Equal(3, loaded.Ship!.ExtendedTanks);
        Assert.Equal(0, loaded.Ship!.MedKitPills);

        // …and a section that never heard of pills says so, rather than saying "empty".
        var old = new ShipSection { ExtendedTanks = 0 };
        Assert.Null(old.MedKitPills);
    }
}
