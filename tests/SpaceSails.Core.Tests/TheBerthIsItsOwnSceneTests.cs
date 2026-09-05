using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #525 · <b>BLOWING HER AT A STATION BERTH IS ITS OWN SCENE, AND ITS OWN CRIME</b> — the Core half.
///
/// <para>The rules, and every one of them was watched to go red before it was allowed to go green: the
/// branch that makes a berth a different scene at all, the slots a declared overload clears (and the one it
/// must never clear), the crossing that is the meter's own ceiling rather than a number somebody typed, the
/// two authored sentences and the sweep that fails on a third, and the roster identity the client's PA now
/// leans on.</para>
///
/// <para><b>What is NOT here, deliberately:</b> anything about insurance. The owner's question on #525 —
/// <i>does Nebula Mutual pay when the captain turned the keys?</i> — is open, this lane leaves
/// <see cref="InsuranceRule.ApplyToRebirth"/> exactly as it was, and a guard that pinned today's behaviour
/// would be this lane quietly answering it.</para>
/// </summary>
// #1108 · Writes the process-wide quiet-hands register (and restores it with a read-modify-write), so it
// joins the collection that serialises every guard which does.
[Collection(StopRegisterCollection.Name)]
public sealed class TheBerthIsItsOwnSceneTests
{
    // ══ THE BRANCH ═══════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The one question that makes this a different scene, and the three answers that are NO. A
    /// hull in the dark, a hull with no clamp, and a captain standing on a DERELICT whose own console runs
    /// its own clock — none of those is at a berth, and the whole of this lane is downstream of that.</summary>
    [Fact]
    public void AScuttleInOpenSpaceIsNotThisScene()
    {
        Assert.False(BerthScuttle.AtABerth(null, onWreck: false));
        Assert.False(BerthScuttle.AtABerth("", onWreck: false));
        Assert.False(BerthScuttle.AtABerth("selene-gate", onWreck: true));
        Assert.True(BerthScuttle.AtABerth("selene-gate", onWreck: false));
    }

    /// <summary>A roster counts slots from zero because a bearing round a ring does. No harbour has ever
    /// painted BERTH 0 on a frame, and the PA reads a frame.</summary>
    [Fact]
    public void TheNumberOnTheFrameIsNotTheIndexInTheRoster()
    {
        Assert.Equal(1, BerthScuttle.BerthNumber(0));
        Assert.Equal(12, BerthScuttle.BerthNumber(DockRoster.BerthsAtAGreatPort - 1));
        Assert.Contains(
            $"Berth {DockRoster.BerthsAtAGreatPort}:",
            BerthScuttle.PaCall(BerthScuttle.BerthNumber(DockRoster.BerthsAtAGreatPort - 1)),
            StringComparison.Ordinal);
    }

    // ══ THE COLLAR ═══════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE SLOTS THE ROSTER EMPTIES, at every port kind the game keeps — read off
    /// <see cref="DockRoster.BerthsAt"/> rather than three typed counts, so a re-tuned roster moves this
    /// guard with it instead of leaving it asserting yesterday's harbour.
    /// </summary>
    [Theory]
    [InlineData(ArrivalTube.Tier.GreatPort)]
    [InlineData(ArrivalTube.Tier.WorkingBerth)]
    [InlineData(ArrivalTube.Tier.Outpost)]
    public void TheNeighboursGoAndHisOwnSlotNeverDoes(ArrivalTube.Tier tier)
    {
        int berths = DockRoster.BerthsAt(tier);

        for (int mine = 0; mine < berths; mine++)
        {
            IReadOnlyList<int> cleared = BerthScuttle.CollarCleared(mine, berths);

            // He is still tied up in his. A roster that reassigned the ship declaring the overload away
            // from the overload is the feature silently doing nothing.
            Assert.DoesNotContain(mine, cleared);

            // Every slot is a real slot on this ring, each named once, in order.
            Assert.All(cleared, slot => Assert.InRange(slot, 0, berths - 1));
            Assert.Equal(cleared.Distinct().Count(), cleared.Count);
            Assert.Equal([.. cleared.OrderBy(s => s)], cleared);

            // Either side, and never more: a blast radius is not a harbour-wide evacuation. At a two-berth
            // port both sides are the same neighbour and the answer is ONE slot, not the same slot twice.
            Assert.Equal(Math.Min(2, berths - 1), cleared.Count);
        }
    }

    /// <summary>The outpost, said on its own because the empty answer is the correct one rather than a gap:
    /// <see cref="DockRoster.BerthsAtAnOutpost"/>'s own words are that a place with one berth cannot reassign
    /// anybody. The PA still goes out; there is simply no next slot to move anyone to.</summary>
    [Fact]
    public void AOneBerthOutpostClearsNobodyBecauseThereIsNobodyToClear()
    {
        Assert.Empty(BerthScuttle.CollarCleared(0, DockRoster.BerthsAtAnOutpost));
        Assert.Empty(BerthScuttle.CollarCleared(0, 0));
    }

    /// <summary>…and the ring WRAPS, which is the whole reason a captain in the last slot still has a
    /// neighbour — the modulo #1092's roster is built on, said here about the other direction.</summary>
    [Fact]
    public void TheRingWrapsSoTheLastSlotHasTwoNeighboursToo()
    {
        int berths = DockRoster.BerthsAtAGreatPort;
        Assert.Equal([0, berths - 2], BerthScuttle.CollarCleared(berths - 1, berths));
        Assert.Equal([1, berths - 1], BerthScuttle.CollarCleared(0, berths));
    }

    // ══ THE METER ════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE CEILING BAND, AND IT IS THE METER'S OWN — never a typed number. The weight IS
    /// <see cref="IllegalHeat.Ceiling"/>, and <see cref="IllegalHeat.Bank"/> clamps to the room left, so the
    /// answer afterwards is exactly the ceiling from ANY starting standing: a cold book, a warm one, and one
    /// already at the top.
    /// </summary>
    [Fact]
    public void ThePortsOperatorGoesToTheCeilingFromWhereverTheyWere()
    {
        Assert.Equal(IllegalHeat.Ceiling, IllegalHeat.WeightOf(IllegalHeat.Crossing.SheWentAtTheirBerth));

        foreach (int already in new[] { 0, 1, IllegalHeat.ABand, IllegalHeat.Ceiling })
        {
            var book = new ContactLedger();
            string op = SiteOperator.Of("luna").Id;
            if (already > 0)
            {
                book.ApplyHeat(IllegalHeat.LedgerId(op), "whoever", already, 0);
            }

            int after = IllegalHeat.Bank(book, BerthScuttle.Charge("luna"), simTime: 10);

            Assert.Equal(IllegalHeat.Ceiling, after);
            Assert.Equal(IllegalHeat.Ceiling, IllegalHeat.HeatAtSite(book, "luna"));

            // …and it is IN the top band, which is the sentence the canon uses: never below it, never past it.
            Assert.InRange(after, IllegalHeat.Ceiling - IllegalHeat.ABand, IllegalHeat.Ceiling);
        }
    }

    /// <summary>
    /// …AND WHAT THAT BUYS IS THE FUGITIVE ON FOOT, expressed in the machinery the building already speaks.
    /// The round keeps ONE ladder of patience and heat starts you further up it; at the ceiling the rung is
    /// the last one, so their watch begins at the end of its patience. One band lower it does not — which is
    /// what makes the ceiling mean something rather than being a big number.
    /// </summary>
    [Fact]
    public void TheCeilingIsTheLastRungOfTheRoundsPatienceAndOneBandLowerIsNot()
    {
        Assert.True(BerthScuttle.AFugitiveOnTheirFloor(IllegalHeat.Ceiling));
        Assert.Equal(PatrolBeat.EscortsAWatchAllows, IllegalHeat.StartingRung(IllegalHeat.Ceiling));

        Assert.False(BerthScuttle.AFugitiveOnTheirFloor(0));
        Assert.False(BerthScuttle.AFugitiveOnTheirFloor(
            PatrolBeat.EscortsAWatchAllows * IllegalHeat.HeatPerRung - 1));
    }

    /// <summary>The charge is published in the one shape every crossing in the game is published in, owed to
    /// the outfit that runs THIS port — never re-derived here, because a scene with its own opinion about
    /// who runs a station is a second source for one fact.</summary>
    [Fact]
    public void TheChargeIsOwedToWhoeverRunsThePort()
    {
        UndergroundComplex.HeatCharge charge = BerthScuttle.Charge("selene-gate");
        Assert.Equal(SiteOperator.Of("selene-gate").Id, charge.OperatorId);
        Assert.Equal(IllegalHeat.WeightOf(IllegalHeat.Crossing.SheWentAtTheirBerth), charge.Points);
    }

    // ══ THE TWO SENTENCES, AND THE THIRD THAT MAY NOT EXIST ══════════════════════════════════════════════

    /// <summary>Both lines, verbatim from the canon pass of 2026-09-05. Character for character: this is the
    /// row that goes red if anybody "improves" one of them.</summary>
    [Fact]
    public void TheTwoLinesAreTheOnesThatWereAuthored()
    {
        Assert.Equal(
            "Berth 9: reactor overload declared. Clear the collar. This is not a drill.",
            BerthScuttle.PaCall(9));

        Assert.Equal("The port has your name. It had your berth.", BerthScuttle.ThePortHasYourName);
    }

    /// <summary>…and they are the ONLY strings this scene publishes. A reflection sweep over every public
    /// static string on the type, checked against the type's own declared prose — the discipline every
    /// prose-bearing type in Core keeps, and the reason a third authored line cannot arrive quietly.</summary>
    [Fact]
    public void NothingElseIsAuthoredHere()
    {
        var declared = BerthScuttle.AllProse().ToList();
        Assert.Equal(2, declared.Count);

        foreach (FieldInfo f in typeof(BerthScuttle)
                     .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (f.FieldType == typeof(string) && f.GetValue(null) is string s)
            {
                Assert.True(declared.Contains(s, StringComparer.Ordinal),
                    $"BerthScuttle publishes prose the canon pass did not author: {f.Name} = \"{s}\"");
            }
        }

        foreach (PropertyInfo p in typeof(BerthScuttle)
                     .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (p.PropertyType == typeof(string) && p.GetValue(null) is string s)
            {
                Assert.True(declared.Contains(s, StringComparer.Ordinal),
                    $"BerthScuttle publishes prose the canon pass did not author: {p.Name} = \"{s}\"");
            }
        }

        // The one method that formats a line is the PA, and it formats the SAME sentence at every slot —
        // a number is not a second authored string.
        for (int slot = 0; slot < DockRoster.BerthsAtAGreatPort; slot++)
        {
            string said = BerthScuttle.PaCall(BerthScuttle.BerthNumber(slot));
            Assert.EndsWith(": reactor overload declared. Clear the collar. This is not a drill.",
                said, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// §8's reserved word and the fifteen beside it — and, on top of them, THE PORT NEVER NAMES WHO CALLED
    /// IT IN. That is the canon law of this scene and the whole of why the announcement is frightening: the
    /// concourse is told there is a reactor running away in slot nine, and working out who is standing in
    /// slot nine is left to the concourse.
    /// </summary>
    [Fact]
    public void ThePortNamesTheBerthAndNobodyAtAll()
    {
        string[] forbidden =
        [
            // §8 and its neighbours
            "monolith", "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect",
            "clone", "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
            // …and anybody at all: no informant, no office, no rank, no hull.
            "captain", "master", "crew", "engineer", "nebula", "informant", "witness", "report",
            "reported", "informed", "dockmaster", "harbourmaster", "harbormaster", "security",
        ];

        foreach (string line in BerthScuttle.AllProse())
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ══ THE ROSTER IDENTITY THE PA LEANS ON ══════════════════════════════════════════════════════════════

    /// <summary>
    /// #525 · <b>THE SPLIT IS AN IDENTITY, NOT A SECOND OPINION.</b> The clamp used to ask
    /// <see cref="DockRoster.BearingAt"/> for a bearing; it now asks for the SLOT and turns that into the
    /// bearing, because the reassignment is marked spent one line later and the number has to survive.
    ///
    /// <para><b>AND IT IS ASKED IN A WORLD THAT CAN TELL THE DIFFERENCE.</b> With no register installed,
    /// <see cref="DockRoster.BerthGiven"/> and <see cref="DockRoster.OrdinaryBerth"/> answer the same slot at
    /// every port in the sky — so a guard run over a clean world would go green whichever of the two the
    /// bearing was derived from, which is this ground's fifth named bug class exactly. A real reassignment is
    /// installed first, the world is asserted to have MOVED somebody, and only then is the identity taken.
    /// The register is ambient and is put back in a <c>finally</c>.</para>
    /// </summary>
    [Fact]
    public void AskingTheRosterForTheSlotAndThenTheBearingIsTheSameBearing()
    {
        var sol = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

        // A ground whose harbour keeps more than one berth — derived, never typed, because a one-collar port
        // cannot reassign anybody and a sweep that found only those would be green and empty.
        var moved = new List<QuietHands.Hand>();
        foreach (CelestialBody body in sol.Bodies)
        {
            if (body.Kind == BodyKind.Station || body.IsHaven || body.ParentId is null)
            {
                continue;
            }
            if (QuietHands.PortFor(sol, body.Id) is { } port && DockRoster.BerthsAt(sol, port.Id) > 1)
            {
                moved.Add(new QuietHands.Hand(body.Id, Window: 7, BerthGiven: false));
            }
        }
        Assert.NotEmpty(moved);

        IReadOnlyList<QuietHands.Hand> had = QuietHands.Handled;
        QuietHands.Install(moved);
        try
        {
            int checkedBerths = 0;
            int reassigned = 0;

            foreach (CelestialBody body in sol.Bodies)
            {
                if (!DockableHavens.IsDockable(body))
                {
                    continue;
                }

                int berths = DockRoster.BerthsAt(sol, body.Id);
                long? owed = QuietHands.BerthOwedAt(sol, body.Id);
                int slot = DockRoster.BerthGiven(body.Id, berths, owed);

                Assert.Equal(DockRoster.BearingAt(sol, body.Id), DockRoster.BearingOf(slot, berths));
                Assert.InRange(slot, 0, berths - 1);

                if (owed is not null && slot != DockRoster.OrdinaryBerth(body.Id, berths))
                {
                    reassigned++;
                }
                checkedBerths++;
            }

            Assert.True(checkedBerths > 0, "the scenario had no dockable berth to check the roster against.");
            Assert.True(reassigned > 0,
                "no berth in this world was actually moved, so the identity proves nothing about WHICH slot.");
        }
        finally
        {
            QuietHands.Install(had);
        }
    }

    // ══ THE VAULT ════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE CLEARED COLLAR RIDES THE FILE — through real JSON, because a section that never reached the
    /// serializer is this repo's own named vault bug (<c>EveryVaultSectionReachesTheFileTests</c>).
    ///
    /// <para>And the null case is the load-bearing half: almost every vault ever written has no collar in it,
    /// and an eager row would change the digest of all of them.</para>
    /// </summary>
    [Fact]
    public void TheClearedCollarRoundTripsAndAnHonestVaultStillCarriesNothing()
    {
        var withOne = new Vault
        {
            Progress = new ProgressSection
            {
                CollarCleared = new ClearedCollarRecord(
                    "selene-gate", 4, [3, 5], BerthScuttle.Why.DeclaredOverload.ToString()),
            },
        };

        Vault back = VaultSerializer.Load(VaultSerializer.Save(withOne));
        ClearedCollarRecord row = back.Progress?.CollarCleared
            ?? throw new InvalidOperationException("the collar never reached the file.");

        Assert.Equal("selene-gate", row.HavenId);
        Assert.Equal(4, row.Berth);
        Assert.Equal([3, 5], row.Neighbours);
        Assert.Equal(BerthScuttle.Why.DeclaredOverload, Enum.Parse<BerthScuttle.Why>(row.Reason));

        // …and a voyage in which nobody declared anything writes no key at all.
        string quiet = VaultSerializer.Save(new Vault { Progress = new ProgressSection() });
        Assert.DoesNotContain("collarCleared", quiet, StringComparison.OrdinalIgnoreCase);
        Assert.Null(VaultSerializer.Load(quiet).Progress?.CollarCleared);
    }
}
