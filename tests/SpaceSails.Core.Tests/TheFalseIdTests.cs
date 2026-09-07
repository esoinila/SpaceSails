using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #804 · <b>THE FALSE ID — A PASS SOMEBODY ELSE WAS ISSUED, AND WHAT A MAN ON A ROTA MAKES OF IT.</b>
///
/// <para>The reconciliation audit of 2026-09-06 filed the gap in one sentence: <i>"WalletChoice.Outcome
/// .WrongSite exists and is judged properly, but nothing authored ever puts a foreign site's pass in the
/// wallet — only a dev cheat."</i> So the judgement below is NOT new and these guards do not re-prove it;
/// what they prove is that a world now deals the paper the judgement was written for, that the paper is
/// always somebody else's, and that the two roads to it (the drawer and the cheat) are one road.</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public class TheFalseIdTests
{
    // ── The rarity, measured rather than asserted ─────────────────────────────────────────────────────

    private const int Probes = 2000;

    /// <summary>
    /// <b>THE RATE IS THE ONE THAT WAS MEASURED, AND THE WORLD CAN TELL PASS FROM FAIL.</b>
    ///
    /// <para>Both halves matter. A sweep that found a pass on every ground would prove nothing about a
    /// rarity, and a sweep that found none would pass every other guard in this file while the feature was
    /// silently dead — this repo's fifth named bug class, a world that cannot tell pass from fail.</para>
    ///
    /// <para>The band is wide because the number is the owner's to tune; what is pinned is that the
    /// COMPOSITION still bites — <see cref="FoundPass.OneInSites"/> through whether the building has a mess
    /// floor at all and through that floor's own haul roll, which is why the measured rate (~19%, about one
    /// ground in five) is nothing like one in <see cref="FoundPass.OneInSites"/>.</para>
    ///
    /// <para><b>RED</b> by making <see cref="FoundPass.RoomFor"/> return the room unconditionally (every
    /// ground carries one) or by returning null (none does).</para>
    /// </summary>
    [Fact]
    public void SomeGroundsKeepOneAndMostDoNot()
    {
        int with = 0;
        foreach (string body in Grounds())
        {
            if (FoundPass.RoomFor(body) is not null)
            {
                with++;
            }
        }

        Assert.True(with > 100, $"only {with} of {Probes} grounds keep a foreign pass — the feature is dead.");
        Assert.True(with < Probes / 2,
            $"{with} of {Probes} grounds keep one — a pass in every other building is not a find, it is a "
            + "supply.");
    }

    /// <summary>
    /// <b>A DRAWER YOU LEAVE IS THE DRAWER YOU COME BACK TO</b> — and, the half that is worth a test, the
    /// answer survives the PROCESS. Asserting <c>RoomFor(b) == RoomFor(b)</c> inside one run is a green test
    /// that asserts nothing: any deterministic function passes it, including one seeded off
    /// <c>string.GetHashCode</c>, which is randomised per process in .NET and is exactly the one line
    /// <see cref="BlackOpsKey.IsAboard"/>'s docblock records paying for. So the guard reads the SEED: the
    /// body id goes into it whole, and nothing else does.
    ///
    /// <para>The other half is that the answer is a fact about the GROUND rather than about the world's
    /// state — installing the two registers that decide what else is on that floor must not move it.</para>
    ///
    /// <para><b>RED</b> by hashing the id into the seed, or by folding any world state into
    /// <c>ASiteKeepsOne</c>.</para>
    /// </summary>
    [Fact]
    public void ADrawerYouLeaveIsTheDrawerYouComeBackTo()
    {
        string source = TheCardCarriesItsOwnStoryTests.ReadRepoFile("src/SpaceSails.Core/FoundPass.cs");
        Assert.DoesNotContain("GetHashCode", source, StringComparison.Ordinal);
        Assert.Contains("DiceRule.Seed($\"false-id:kept:{bodyId}\")", source, StringComparison.Ordinal);

        string[] grounds = [.. Grounds().Take(200)];
        bool[] before = [.. grounds.Select(FoundPass.ASiteKeepsOne)];

        StopOrder.Install([.. grounds]);
        PreservationZone.Install([.. grounds]);
        try
        {
            for (int i = 0; i < grounds.Length; i++)
            {
                Assert.Equal(before[i], FoundPass.ASiteKeepsOne(grounds[i]));
            }
        }
        finally
        {
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }

        // …and it is not a constant dressed as a roll: the grounds really disagree with each other.
        Assert.Contains(true, before);
        Assert.Contains(false, before);
    }

    // ── Where it lies ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE DRAWER IS ON THE MESS FLOOR, IT IS AN INDEX EVERY FLOOR HAS, AND IT TAKES NOTHING ELSE'S
    /// ROOM.</b>
    ///
    /// <para>A collision would silently replace one of the seven designated finds this ground already keeps
    /// and nothing on screen would ever say which one the captain did not get. Asked with both registers
    /// installed so the papers that only exist on a stopped or filled ground really exist — a guard handed a
    /// floor with no other finds on it would prove nothing at all.</para>
    ///
    /// <para>The index bound is the other half: <see cref="FoundPass.Room"/> plus
    /// <see cref="FoundPass.RoomsWalked"/> must stay inside the three rooms a floor is guaranteed to have,
    /// because this file has no field to count with. Room 5 passed every other guard in this file and does
    /// not exist on Enceladus.</para>
    ///
    /// <para><b>RED</b> by pointing <see cref="FoundPass.RoomFor"/> at
    /// <c>UndergroundComplex.TopPressurisedFloor</c> (the ledger's and the cost-centre papers' own floor),
    /// or by widening <see cref="FoundPass.RoomsWalked"/> past the guaranteed three.</para>
    /// </summary>
    [Fact]
    public void TheDrawerCollidesWithNothingElseTheFloorKeeps()
    {
        Assert.True(FoundPass.Room + FoundPass.RoomsWalked <= 3,
            "the drawer may be walked to a room index no floor is guaranteed to have.");

        string[] grounds = [.. Grounds().Take(600)];
        StopOrder.Install([.. grounds]);
        PreservationZone.Install([.. grounds]);
        try
        {
            int seen = 0;

            foreach (string body in grounds)
            {
                if (FoundPass.RoomFor(body) is not { } at)
                {
                    continue;
                }
                seen++;

                Assert.Equal(UndergroundComplex.StaffCanteenFloor(body), at.Level);
                Assert.InRange(at.RoomIndex, FoundPass.Room, FoundPass.Room + FoundPass.RoomsWalked - 1);

                // It may only ever stand in a room the haul table has already called empty — which is what
                // makes every clause below true by construction rather than by seven conditions somebody has
                // to keep agreeing.
                Assert.Equal(
                    UndergroundComplex.Haul.Nothing,
                    UndergroundComplex.InRoom(body, at.Level, at.RoomIndex));

                Assert.False(
                    UndergroundComplex.LiftCode.PaperRoomFor(body) is { } code
                    && code.Level == at.Level && code.RoomIndex == at.RoomIndex,
                    $"{body}: the pass took the code paper's room.");
                Assert.False(
                    UndergroundComplex.MaintenanceLedgerRoomFor(body) is { } ledger
                    && ledger.Level == at.Level && ledger.RoomIndex == at.RoomIndex,
                    $"{body}: the pass took the maintenance ledger's room.");
                Assert.False(
                    UndergroundComplex.KeyRoomFor(body) is { } key
                    && key.Level == at.Level && key.RoomIndex == at.RoomIndex,
                    $"{body}: the pass took the Key room — the way down went with it.");
                Assert.False(
                    UndergroundComplex.RelicRoomFor(body) is { } relic
                    && relic.Level == at.Level && relic.RoomIndex == at.RoomIndex,
                    $"{body}: the pass is standing on the pallet.");
                Assert.False(
                    UndergroundComplex.FoundKeyRoomFor(body) is { } halls
                    && halls.Level == at.Level && halls.RoomIndex == at.RoomIndex,
                    $"{body}: the pass took the way down to the halls.");
                Assert.False(
                    UndergroundComplex.ValveBookRoomFor(body) is { } valves
                    && valves.Level == at.Level && valves.RoomIndex == at.RoomIndex,
                    $"{body}: the pass took the valve-book's room.");
                Assert.Null(UndergroundComplex.MoneyTrailPaperIn(body, at.Level, at.RoomIndex));

                // …and #701's shelf, which is the one thing that shares this drawer's own "the room is
                // empty" test and would otherwise answer the same press twice.
                Assert.False(OddBooks.HoldsOne(body, at.Level, at.RoomIndex),
                    $"{body}: a book and a pass are both lying in {at.Level}/{at.RoomIndex}.");
                Assert.False(OddBooks.CouldHoldOne(body, at.Level, at.RoomIndex),
                    $"{body}: the shelf roll can still reach the drawer at {at.Level}/{at.RoomIndex}, so "
                    + "which of the two the captain gets depends on the order two files are asked in.");
            }

            Assert.True(seen > 40, $"only {seen} grounds carried a pass at all; this proves little.");
        }
        finally
        {
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }
    }

    // ── Whose it is ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>IT IS NEVER THIS BUILDING'S OWN PASS.</b> The whole object is a pass that belongs somewhere else;
    /// a producer that could hand back the local one would silently give a captain the thing the gig is for.
    ///
    /// <para><b>RED</b> by dropping the <c>string.Equals(body, hereBodyId)</c> skip in
    /// <see cref="FoundPass.MintedElsewhere"/>.</para>
    /// </summary>
    [Fact]
    public void ItIsAlwaysSomebodyElsesSite()
    {
        string[] world = [.. Enumerable.Range(0, 9).Select(i => $"false-id-world-{i}")];
        int minted = 0;

        foreach (string here in world)
        {
            for (ulong seed = 0; seed < 64; seed++)
            {
                Satchel.Item pass = Assert.IsType<Satchel.Item>(
                    FoundPass.MintedElsewhere(here, world, seed), exactMatch: false);
                minted++;

                Assert.Equal(Satchel.Kind.Badge, pass.Kind);
                string site = Assert.IsType<string>(PatrolBeat.SiteOfBadge(pass.Id), exactMatch: false);
                Assert.NotEqual(here, site);
                Assert.Contains(site, world);
                Assert.True(FoundPass.IsForElsewhere(pass, here));
            }
        }

        Assert.True(minted > 500, $"only {minted} passes were minted; this proves little.");
    }

    /// <summary>A world with nowhere else to have issued one deals nothing, rather than dealing this
    /// building's own pass. Every caller has to be able to find nothing.
    ///
    /// <para><b>RED</b> by returning <c>PatrolBeat.Badge(hereBodyId)</c> instead of null on an empty
    /// roster.</para></summary>
    [Fact]
    public void AWorldWithNowhereElseDealsNothing()
    {
        Assert.Null(FoundPass.MintedElsewhere("luna", null, 7));
        Assert.Null(FoundPass.MintedElsewhere("luna", [], 7));
        Assert.Null(FoundPass.MintedElsewhere("luna", ["luna", "luna"], 7));
    }

    /// <summary>
    /// <b>THE ROSTER'S ORDER DOES NOT DECIDE WHOSE PASS IT IS.</b> The caller builds its list by appending —
    /// shuttle stops, in whatever order the sky is in this afternoon — and a list built by appending is not
    /// a list in order (this repo's fourth named bug class). Unsorted, the same drawer on the same moon would
    /// hold a different site's pass depending on where the ship was parked.
    ///
    /// <para><b>RED</b> by deleting the <c>elsewhere.Sort(StringComparer.Ordinal)</c> line.</para>
    /// </summary>
    [Fact]
    public void WhoseItIsDoesNotDependOnHowTheSkyWasSorted()
    {
        string[] world = [.. Enumerable.Range(0, 7).Select(i => $"false-id-order-{i}")];
        string[] backwards = [.. world.Reverse()];
        string[] rotated = [.. world.Skip(3), .. world.Take(3)];

        for (ulong seed = 0; seed < 40; seed++)
        {
            Satchel.Item? one = FoundPass.MintedElsewhere(world[0], world, seed);
            Assert.Equal(one, FoundPass.MintedElsewhere(world[0], backwards, seed));
            Assert.Equal(one, FoundPass.MintedElsewhere(world[0], rotated, seed));
        }
    }

    // ── The judgement, which was already written ──────────────────────────────────────────────────────

    /// <summary>
    /// <b>ON THIS FLOOR IT IS REFUSED; ON ITS OWN IT IS A PASS.</b> Both arms off the ladder that was already
    /// there (<see cref="WalletChoice.WhatHappens"/>), and both off the SAME object — the point of the
    /// feature is that a false ID is a real pass in the wrong building, so the same row has to read two ways
    /// depending only on where the captain is standing.
    ///
    /// <para><b>RED</b> by having the producer mint a badge for the site it is found on: the first assert
    /// goes green-for-the-wrong-reason and the second stops being about a foreign pass at all — which is why
    /// <see cref="ItIsAlwaysSomebodyElsesSite"/> is a separate guard and this one asserts the site
    /// too.</para>
    /// </summary>
    [Fact]
    public void ItIsRefusedWhereItIsFoundAndWorksWhereItWasIssued()
    {
        string[] world = [.. Enumerable.Range(0, 6).Select(i => $"false-id-judged-{i}")];
        int judged = 0;

        foreach (string here in world)
        {
            for (ulong seed = 0; seed < 20; seed++)
            {
                Satchel.Item pass = FoundPass.MintedElsewhere(here, world, seed)!.Value;
                string mintedFor = PatrolBeat.SiteOfBadge(pass.Id)!;
                Assert.NotEqual(here, mintedFor);
                judged++;

                Assert.Equal(WalletChoice.Outcome.WrongSite, WalletChoice.WhatHappens(here, -2, 0L, pass));
                Assert.Equal(WalletChoice.Outcome.Worked, WalletChoice.WhatHappens(mintedFor, -2, 0L, pass));

                // …and the card the man's read is told on says the same thing, because it is composed off
                // that one ladder and nothing else.
                Assert.False(PatrolBeat.TheGuardReads(here, -2, 0L, "A ROUND", pass, false).Satisfied);
                Assert.True(PatrolBeat.TheGuardReads(mintedFor, -2, 0L, "A ROUND", pass, false).Satisfied);
            }
        }

        Assert.True(judged > 100, $"only {judged} reads were asked; this proves little.");
    }

    /// <summary>
    /// <b>A PASS IS A PASS AND NOT A DOOR.</b> #590's gate reads authority cards and this is not one —
    /// GENERAL HANDS, one tier, no band. A found pass that also ran a shaft would be the skeleton key
    /// #679 ruled out, arriving through the wallet instead of through the card.
    ///
    /// <para><b>RED</b> by minting <c>Satchel.Kind.Authority</c> instead of a badge.</para>
    /// </summary>
    [Fact]
    public void ItOpensNoShaftAndCarriesNoBand()
    {
        string[] world = ["false-id-band-a", "false-id-band-b", "false-id-band-c"];
        Satchel.Item pass = FoundPass.MintedElsewhere(world[0], world, 3)!.Value;

        Assert.False(UndergroundComplex.AuthorityCard.TryParse(pass.Id, out _));
        Assert.Empty(SiteOperator.Accesses([pass]));
        Assert.Equal(PatrolBeat.BadgeTier, PatrolBeat.BadgeTier);
        Assert.Contains(PatrolBeat.BadgeTier, WalletChoice.Claims(pass), StringComparison.Ordinal);
    }

    // ── It survives the file ──────────────────────────────────────────────────────────────────────────

    /// <summary>A pass found eleven floors under a moon has to still be in the wallet a month and a world
    /// later, or it is not a possession — it is a mood. Round-tripped through the row's own storage AND
    /// through the whole vault, because those are two different files' worth of promises.
    ///
    /// <para><b>RED</b> by storing the badge under an id containing the section separator without the
    /// bounded split <c>Satchel.Item.Stored</c> already pays for.</para></summary>
    [Fact]
    public void ItSurvivesTheVault()
    {
        string[] world = ["false-id-vault-a", "false-id-vault-b"];
        Satchel.Item pass = FoundPass.MintedElsewhere(world[0], world, 1)!.Value;

        Assert.True(Satchel.Item.TryParse(pass.Stored, out Satchel.Item read));
        Assert.Equal(pass, read);

        var saved = new Vault
        {
            Version = Vault.CurrentVersion,
            Satchel = new SatchelSection { Items = [pass.Stored] },
        };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(saved));

        string stored = Assert.Single(loaded.Satchel!.Items);
        Assert.True(Satchel.Item.TryParse(stored, out Satchel.Item back));
        Assert.Equal(pass, back);
        Assert.Equal(
            WalletChoice.Outcome.WrongSite, WalletChoice.WhatHappens(world[0], -2, 0L, back));
    }

    // ── The prose, and what there is none of ──────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THIS FEATURE AUTHORS EXACTLY ONE SENTENCE.</b> #1143 shipped with none and a
    /// <c>// FABLE: line needed</c> over the plate; the canon pass of 2026-09-06 wrote the one line the
    /// marker asked for — the moment the paper goes into the wallet — and the count is held at ONE for the
    /// reason that was a canon rule before there were any: the plate a found pass wears is the MINTING
    /// SITE'S OWN (<see cref="PatrolBeat.BadgeTitle"/>, #590's grammar), and a second authored sentence
    /// would be a second voice describing a piece of paper that already says what it is.
    ///
    /// <para>Asked by REFLECTION rather than by reading the file, so the number cannot drift behind a
    /// comment: every static string this class declares is enumerated, there is one, it is
    /// <see cref="FoundPass.TakenLine"/>, and it is the owner-facing text VERBATIM.</para>
    ///
    /// <para><b>RED</b> three ways, all watched: add a second <c>const string</c> to
    /// <see cref="FoundPass"/>; change one character of the line; or compose the plate out of anything but
    /// the pass's own face.</para>
    /// </summary>
    [Fact]
    public void ItAuthorsExactlyOneSentenceOfItsOwn()
    {
        FieldInfo[] strings = [.. typeof(FoundPass)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))];

        FieldInfo only = Assert.Single(strings);
        Assert.Equal(nameof(FoundPass.TakenLine), only.Name);
        Assert.Equal(
            "Somebody's old site pass, still warm from a locker. The photograph is not you. Nobody has ever checked.",
            (string?)only.GetValue(null));

        // …and the plate is still the site's own face and nothing composed beside it: the sentence is said
        // at the find, never wrapped around the row the wallet keeps.
        string[] world = ["false-id-prose-a", "false-id-prose-b"];
        Satchel.Item pass = FoundPass.MintedElsewhere(world[0], world, 2)!.Value;
        string site = PatrolBeat.SiteOfBadge(pass.Id)!;

        Assert.Equal($"{PatrolBeat.BadgeGlyph} {PatrolBeat.BadgeTitle(site)}", FoundPass.Plate(pass));

        // …and a row this build cannot read is honest about itself rather than invented over.
        Assert.Equal(
            $"{PatrolBeat.BadgeGlyph} {WalletChoice.UnreadableFaceLine}",
            FoundPass.Plate(new Satchel.Item(Satchel.Kind.Badge, "badge:")));
    }

    /// <summary>§8's reserved word and the fifteen beside it: nothing this feature can put on a screen says
    /// what any of this place was FOR. It used to be nearly free, because the only thing this feature could
    /// put on a screen was a site code and a tier; it costs something now that there is an authored
    /// sentence, and the sentence is exactly where a line about a stolen pass would reach for an
    /// explanation. It reaches for none — the reason the card works is <i>nobody has ever checked</i>, which
    /// names no system and no one upstairs (#649's comprehension without acceptance).</summary>
    [Fact]
    public void NothingItSaysExplainsWhatThePlaceWasFor()
    {
        string[] forbidden =
        [
            "monolith", "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect",
            "clone", "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
        ];

        string[] world = ["false-id-word-a", "false-id-word-b"];
        string plate = FoundPass.Plate(FoundPass.MintedElsewhere(world[0], world, 5)!.Value);

        foreach (string bad in forbidden)
        {
            Assert.DoesNotContain(bad, plate, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(bad, FoundPass.TakenLine, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>THE MARKER IS GONE. <c>FoundPass</c> shipped in #1143 with one
    /// <c>// FABLE: line needed</c> standing in for the sentence this file now has. A marker left behind a
    /// written line is a request nobody will read twice, and the file is read here rather than reasoned
    /// about.</summary>
    [Fact]
    public void TheLineNeededMarkerIsGone()
    {
        string source = TheCardCarriesItsOwnStoryTests.ReadRepoFile("src/SpaceSails.Core/FoundPass.cs");

        Assert.DoesNotContain("FABLE: line needed", source, StringComparison.Ordinal);
        Assert.Contains(FoundPass.TakenLine, source, StringComparison.Ordinal);
    }

    private static IEnumerable<string> Grounds()
    {
        for (int i = 0; i < Probes; i++)
        {
            yield return $"false-id-ground-{i}";
        }
    }
}
