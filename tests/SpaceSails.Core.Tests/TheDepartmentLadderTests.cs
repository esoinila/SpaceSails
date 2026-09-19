using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #605 · <b>THE DEPARTMENT LADDER — one laminate, read at two ranges.</b>
///
/// <para>Owner: <i>"camouflage — a badge means you belong."</i> #804 shipped one tier and wrote the
/// remainder into its own source: <i>"the department ladder is #605's question and not this one's."</i>
/// This is that question answered.</para>
///
/// <para>The guards below are about the LADDER and not about the badge: the object, the fan, the escort and
/// the site code were all written before this lane and none of them changed. What is new is that the same
/// pass answers differently depending on whether it is being read at twenty paces or in somebody's
/// hand.</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public class TheDepartmentLadderTests
{
    private const int Probes = 400;

    // ── THE BOTTOM RUNG: A LAMINATE IS A LAMINATE ─────────────────────────────────────────────────────

    /// <summary>
    /// <b>AT A DISTANCE, ANY TIER OF THIS SITE PASSES.</b> The three things that read a wallet without
    /// holding it — the gate that wants a face (#715), the parcel, and the site that will not put you on its
    /// books twice — all ask <see cref="PatrolBeat.BadgeHeld"/>, and none of them can make out the line
    /// under the site code.
    ///
    /// <para>The other half is the one that keeps it from being a skeleton key: another building's pass is
    /// still nothing at all, at every tier.</para>
    ///
    /// <para><b>PROVEN RED</b> by restoring the shipped body —
    /// <c>Satchel.CountOf(carried, Kind.Badge, BadgeId(bodyId)) &gt; 0</c> — which is an exact id match and
    /// answers NO to every department pass in the game.</para>
    /// </summary>
    [Fact]
    public void TheDistanceReadPassesAnyTierOfThisSite()
    {
        const string here = "ladder-distance-a";
        const string elsewhere = "ladder-distance-b";
        int tiers = 0;

        foreach (string tier in Tiers(here))
        {
            tiers++;
            List<Satchel.Item> wallet = [PatrolBeat.Badge(here, tier)];

            Assert.True(PatrolBeat.BadgeHeld(here, wallet),
                $"a {tier} pass for this site is not read as this site's pass at twenty paces.");
            Assert.False(PatrolBeat.BadgeHeld(elsewhere, wallet),
                $"a {tier} pass for {here} is being read as {elsewhere}'s.");

            // …and the same pass held FOR somewhere else is nothing here, which is #1143's rung untouched.
            Assert.False(PatrolBeat.BadgeHeld(here, [PatrolBeat.Badge(elsewhere, tier)]));
        }

        Assert.True(tiers > 5, $"only {tiers} tiers were tried; this proves little.");
        Assert.False(PatrolBeat.BadgeHeld(here, null));
        Assert.False(PatrolBeat.BadgeHeld(here, []));
    }

    /// <summary>
    /// <b>THE GENERAL PASS KEPT ITS ID, SO EVERY SAVE EVER WRITTEN READS AS IT ALWAYS DID.</b> The tier
    /// rides after the site code and <see cref="PatrolBeat.BadgeTier"/> mints the plain form, so there is
    /// exactly one id for the pass the hiring notice issues and no stored row has to be migrated.
    ///
    /// <para>And the captain's own paper trail survives the second colon: <see cref="WalletChoice.Shown"/>
    /// writes the id LAST behind a bounded split for exactly this class of reason, and this is the first
    /// time an id has actually had two colons in it.</para>
    ///
    /// <para><b>RED</b> by minting <c>badge:luna:GENERAL HANDS</c> for the general pass (the first assert),
    /// or by widening <c>Stored</c>'s split past 4 (the round-trip).</para>
    /// </summary>
    [Fact]
    public void AnOldSaveReadsExactlyAsItAlwaysDid()
    {
        Assert.Equal("badge:luna", PatrolBeat.BadgeId("luna"));
        Assert.Equal("badge:luna", PatrolBeat.BadgeId("luna", PatrolBeat.BadgeTier));
        Assert.Equal("badge:luna:PLANT", PatrolBeat.BadgeId("luna", "PLANT"));

        Assert.Equal("luna", PatrolBeat.SiteOfBadge("badge:luna"));
        Assert.Equal("luna", PatrolBeat.SiteOfBadge("badge:luna:PLANT"));
        Assert.Equal(PatrolBeat.BadgeTier, PatrolBeat.TierOfBadge("badge:luna"));
        Assert.Equal("PLANT", PatrolBeat.TierOfBadge("badge:luna:PLANT"));

        // The inspector's card has a site code that is not a site and no tier at all — untouched.
        Assert.Equal(Inspectorate.IssuerId, PatrolBeat.SiteOfBadge(Inspectorate.Card.Id));
        Assert.True(Inspectorate.IsTheCard(Inspectorate.Card));
        Assert.Equal(Inspectorate.Plate, WalletChoice.Claims(Inspectorate.Card));

        // Nothing readable is still nothing readable.
        Assert.Null(PatrolBeat.SiteOfBadge(null));
        Assert.Null(PatrolBeat.SiteOfBadge("badge:"));
        Assert.Null(PatrolBeat.SiteOfBadge("chit:day"));

        foreach (string id in new[] { "badge:luna", "badge:luna:PLANT", "badge:luna:LONG STORAGE" })
        {
            var row = new WalletChoice.Shown(id, "luna", -4, WalletChoice.Outcome.WrongDepartment);
            Assert.True(WalletChoice.Shown.TryParse(row.Stored, out WalletChoice.Shown back));
            Assert.Equal(row, back);
        }
    }

    // ── THE TOP RUNG: THE PASS HAS TO FIT THE FLOOR ───────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SWEEP — every generated floor of every site, both outcomes seen.</b>
    ///
    /// <para>Four claims at once, and each one is checked on every patrolled floor of four hundred grounds
    /// so that no site's particular shape can make it true by accident:</para>
    ///
    /// <list type="number">
    /// <item>a floor a department owns REFUSES <see cref="PatrolBeat.BadgeTier"/>;</item>
    /// <item>a floor no department owns (and the hall floors) ACCEPT it;</item>
    /// <item>a department floor accepts its OWN department's pass;</item>
    /// <item>and refuses every OTHER department's.</item>
    /// </list>
    ///
    /// <para><b>The counters are the guard against the fifth named bug class.</b> A sweep that met only
    /// department floors, or only general ones, would pass every assertion above while the world could not
    /// tell pass from fail — so both tallies are required to be large, and the wrong-department tally is
    /// required to be many times the accept tally because there are seven other plates for every one.</para>
    ///
    /// <para><b>PROVEN RED</b> by returning <c>true</c> from <see cref="PatrolBeat.ThePassFitsTheFloor"/>
    /// (every refusal assert fires), by returning <c>false</c> (every accept assert fires), and by dropping
    /// the hall clause from <see cref="PatrolBeat.GeneralHandsBelongOn"/> (the canteen floor starts
    /// escorting hands out from under a plate reading NO PASS REQUIRED).</para>
    /// </summary>
    [Fact]
    public void TheConversationAsksWhatIsPaintedOnTheFloor()
    {
        int generalAccepted = 0, generalRefused = 0, ownAccepted = 0, otherRefused = 0, floors = 0;

        foreach (string body in Grounds())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }
                floors++;

                string? plate = ChamberFitting.DepartmentOn(body, level);
                bool owned = PatrolBeat.ADepartmentOwnsTheFloor(plate)
                             && !UndergroundComplex.IsHallFloor(body, level);

                // 1 / 2 · the tier the hiring notice issues.
                bool hands = WalletChoice.WhatHappens(body, level, 0L, PatrolBeat.Badge(body))
                             == WalletChoice.Outcome.Worked;
                if (owned)
                {
                    Assert.False(hands,
                        $"{body} B{-level} is {plate} and GENERAL HANDS walked straight through it.");
                    generalRefused++;
                }
                else
                {
                    Assert.True(hands,
                        $"{body} B{-level} is nobody's department and GENERAL HANDS was refused on it.");
                    generalAccepted++;
                }

                // 3 / 4 · every tier this site's signage prints, on this floor.
                foreach (string tier in Tiers(body))
                {
                    WalletChoice.Outcome how =
                        WalletChoice.WhatHappens(body, level, 0L, PatrolBeat.Badge(body, tier));

                    if (string.Equals(tier, plate, StringComparison.Ordinal))
                    {
                        Assert.Equal(WalletChoice.Outcome.Worked, how);
                        ownAccepted++;
                    }
                    else if (owned)
                    {
                        Assert.Equal(WalletChoice.Outcome.WrongDepartment, how);
                        otherRefused++;
                    }
                }
            }
        }

        Assert.True(floors > 2000, $"only {floors} patrolled floors were swept; this proves little.");
        Assert.True(generalAccepted > 200, $"only {generalAccepted} floors accepted a hand.");
        Assert.True(generalRefused > 200, $"only {generalRefused} floors refused one.");
        Assert.True(ownAccepted > 200, $"only {ownAccepted} floors honoured their own department's pass.");
        Assert.True(otherRefused > ownAccepted * 3,
            $"{otherRefused} refusals against {ownAccepted} acceptances — the sweep is not meeting the "
            + "other departments' passes, so 'a department floor rejects another department's' is untested.");
    }

    /// <summary>
    /// <b>THE RULE IS A FACT ABOUT THE PLATE, NOT ABOUT THE FLOOR NUMBER.</b> The branch stock cycles, so a
    /// deep site has B3 and B11 both reading LONG STORAGE — and the ladder has to answer the same on both,
    /// or it is a list of floors somebody would have to keep in step with a list of buildings.
    ///
    /// <para><b>RED</b> by keying <see cref="PatrolBeat.GeneralHandsBelongOn"/> on the level (a depth band, a
    /// literal) instead of on <see cref="ChamberFitting.DepartmentOn"/>.</para>
    /// </summary>
    [Fact]
    public void TheSamePlateAnswersTheSameNineFloorsDown()
    {
        int pairs = 0;

        foreach (string body in Grounds().Take(120))
        {
            var byPlate = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                // The hall floors are exempt BY THE RULE — their own sign says no pass is required — so they
                // are the one place two floors of one plate may honestly disagree, and they are skipped
                // rather than quietly folded in.
                if (!PatrolBeat.IsPatrolled(body, level) || UndergroundComplex.IsHallFloor(body, level))
                {
                    continue;
                }

                string plate = UndergroundComplex.DepartmentOf(body, level);
                bool general = PatrolBeat.GeneralHandsBelongOn(body, level);
                if (byPlate.TryGetValue(plate, out bool before))
                {
                    Assert.True(before == general,
                        $"{body}: two floors both plated {plate} disagree about whether a hand belongs.");
                    pairs++;
                }
                else
                {
                    byPlate[plate] = general;
                }
            }
        }

        Assert.True(pairs > 100, $"only {pairs} repeated plates were met; this proves little.");
    }

    /// <summary>The one plate in the stock that is not a department is named where the stock is, and it is
    /// really in the stock. A constant that had drifted out of the list would quietly make every UNMARKED
    /// floor a department floor, and nothing on screen would say so.</summary>
    [Fact]
    public void TheUnmarkedPlateIsOneOfTheDepartments()
    {
        Assert.Contains(UndergroundComplex.UnmarkedPlate, UndergroundComplex.Departments);
        Assert.False(PatrolBeat.ADepartmentOwnsTheFloor(UndergroundComplex.UnmarkedPlate));
        Assert.False(PatrolBeat.ADepartmentOwnsTheFloor(null));
        Assert.False(PatrolBeat.ADepartmentOwnsTheFloor(""));
        Assert.False(PatrolBeat.ADepartmentOwnsTheFloor("LONG STORAGE"));
        Assert.False(PatrolBeat.ADepartmentOwnsTheFloor("DEEP STORAGE"));
        Assert.True(PatrolBeat.ADepartmentOwnsTheFloor("LABORATORIES"));
        Assert.True(PatrolBeat.ADepartmentOwnsTheFloor("PLANT"));

        // GENERAL HANDS is not a department of anywhere: it is what the site ISSUES.
        Assert.False(PatrolBeat.IsADepartmentOf("luna", PatrolBeat.BadgeTier));
        Assert.True(PatrolBeat.IsADepartmentOf("luna", "PLANT"));
    }

    /// <summary>
    /// <b>#1143'S FALSE ID STILL FAILS ON THE SITE CODE, AT EVERY TIER.</b> The department ladder is asked
    /// AFTER the site code and only on this building's own paper — a foreign pass that started passing
    /// because it happened to name this floor's department would have turned every found pass into a
    /// skeleton key for one band of the building.
    ///
    /// <para><b>RED</b> by asking <see cref="PatrolBeat.ThePassFitsTheFloor"/> before the site code in
    /// <see cref="WalletChoice.WhatHappens"/>.</para>
    /// </summary>
    [Fact]
    public void AnotherSitesPassStillFailsTheReadAtEveryTier()
    {
        const string here = "ladder-foreign-here";
        const string mintedFor = "ladder-foreign-there";
        int read = 0;

        foreach (int level in UndergroundComplex.FloorsOf(here))
        {
            if (!PatrolBeat.IsPatrolled(here, level))
            {
                continue;
            }

            foreach (string tier in Tiers(here).Append(PatrolBeat.BadgeTier))
            {
                Satchel.Item pass = PatrolBeat.Badge(mintedFor, tier);
                Assert.Equal(
                    WalletChoice.Outcome.WrongSite, WalletChoice.WhatHappens(here, level, 0L, pass));
                Assert.True(FoundPass.IsForElsewhere(pass, here));
                read++;
            }
        }

        Assert.True(read > 20, $"only {read} reads were made; this proves little.");
    }

    // ── WHAT IS SAID, AND WHAT IS FILED ───────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE AUTHORED LINE IS SAID ON THE DEPARTMENT RUNG AND ON NO OTHER, VERBATIM.</b> It is asked of the
    /// SIM — the sentence a captain is actually told (<see cref="PatrolBeat.TheGuardReads"/>) — rather than
    /// of the constant, so a paraphrase cannot ship by being tidier than what was written.
    ///
    /// <para>And nothing is said when cover HOLDS. Cover is a state and silence is the reward: the satisfied
    /// arm is the sentence it has been since #804's canon pass, with no line about the tier added to
    /// it.</para>
    ///
    /// <para><b>RED</b> by composing the line at the call site instead of quoting the constant, or by
    /// putting it on any other arm of the read.</para>
    /// </summary>
    [Fact]
    public void TheSecondReadIsTheOneAuthoredLineAndNothingIsSaidWhenCoverHolds()
    {
        const string verbatim = "He reads the pass twice. The second time he is reading your face.";
        Assert.Equal(verbatim, PatrolBeat.ReadsItTwiceLine);
        Assert.Contains(verbatim, PatrolBeat.AuthoredLines);

        const string body = "ladder-said";
        int said = 0, silent = 0;

        foreach (int level in UndergroundComplex.FloorsOf(body))
        {
            if (!PatrolBeat.IsPatrolled(body, level))
            {
                continue;
            }

            foreach (string tier in Tiers(body).Append(PatrolBeat.BadgeTier))
            {
                Satchel.Item pass = PatrolBeat.Badge(body, tier);
                PatrolBeat.Read read = PatrolBeat.TheGuardReads(
                    body, level, 0L, "◈ A PLATE", pass, inspectionRunning: false);

                if (WalletChoice.WhatHappens(body, level, 0L, pass)
                    == WalletChoice.Outcome.WrongDepartment)
                {
                    Assert.False(read.Satisfied);
                    Assert.Equal(verbatim, read.Line);
                    Assert.Equal(PatrolBeat.EscortLine, read.Consequence);   // what a refusal already cost
                    said++;
                }
                else
                {
                    Assert.DoesNotContain(verbatim, read.Told, StringComparison.Ordinal);
                    if (read.Satisfied)
                    {
                        Assert.Equal(PatrolBeat.SatisfiedLine, read.Line);
                        Assert.Null(read.Consequence);
                        silent++;
                    }
                }
            }
        }

        Assert.True(said > 10, $"the line was said on only {said} reads; this proves little.");
        Assert.True(silent > 5, $"cover held on only {silent} reads; this proves little.");

        // …and the other four authored lines did not move, in their own order.
        Assert.Equal(
            [
                "Boots on shotcrete, out of step with yours.",
                "Hold there. Floor's restricted. Show me something.",
                "Right. Keep to the lit side.",
                "No? Then you walk ahead of me to the lift, and we don't make it a thing.",
                verbatim,
            ],
            PatrolBeat.AuthoredLines);
    }

    /// <summary>
    /// <b>THE BLOW HAS A REASON, AND THE REASON IS ONE ANSWER.</b> <see cref="WalletChoice.CoverBlew"/>
    /// partitions the whole ladder — every rung is a man walking on or a man who is not satisfied, and there
    /// is no third answer — and the card's own <c>Satisfied</c> agrees with it on every rung, which is what
    /// keeps the sentence, the pip, the escort and the filed line from ever disagreeing about the same
    /// thirty seconds.
    ///
    /// <para><b>RED</b> by adding a rung to <see cref="WalletChoice.Outcome"/> without teaching
    /// <see cref="WalletChoice.CoverBlew"/> about it, which is exactly the day this guard is for.</para>
    /// </summary>
    [Fact]
    public void EveryRungOfTheLadderIsEitherAManWalkingOnOrABlow()
    {
        const string body = "ladder-reasons";
        int rungs = 0;

        foreach (WalletChoice.Outcome how in Enum.GetValues<WalletChoice.Outcome>())
        {
            rungs++;

            // The captain's shorthand exists for every rung and is never blank — a blow the book could not
            // name is the arbitrary punishment #605's second property rules out.
            Assert.False(string.IsNullOrWhiteSpace(WalletChoice.ReasonTag(how)));
            Assert.False(string.IsNullOrWhiteSpace(
                WalletChoice.ShownNote(PatrolBeat.Badge(body), body, -2, how, "SOMEBODY")));
        }

        Assert.True(rungs >= 7, $"only {rungs} rungs; a rung has gone missing.");

        Assert.False(WalletChoice.CoverBlew(WalletChoice.Outcome.Worked));
        Assert.False(WalletChoice.CoverBlew(WalletChoice.Outcome.Inspection));
        Assert.True(WalletChoice.CoverBlew(WalletChoice.Outcome.WrongDepartment));
        Assert.True(WalletChoice.CoverBlew(WalletChoice.Outcome.WrongSite));
        Assert.True(WalletChoice.CoverBlew(WalletChoice.Outcome.WrongPaper));
        Assert.True(WalletChoice.CoverBlew(WalletChoice.Outcome.NothingShown));
        Assert.True(WalletChoice.CoverBlew(WalletChoice.Outcome.NoInspectionDue));

        // …and the card's own arm is the same answer as the book's. Swept over the floors, because two of
        // the rungs depend on where the captain is standing.
        int checked_ = 0;
        foreach (int level in UndergroundComplex.FloorsOf(body))
        {
            if (!PatrolBeat.IsPatrolled(body, level))
            {
                continue;
            }

            foreach (Satchel.Item? shown in Wallets(body))
            {
                WalletChoice.Outcome how = WalletChoice.WhatHappens(body, level, 0L, shown);
                PatrolBeat.Read read = PatrolBeat.TheGuardReads(
                    body, level, 0L, "◈ A PLATE", shown, inspectionRunning: false);

                Assert.Equal(WalletChoice.CoverBlew(how), !read.Satisfied);
                Assert.Equal(WalletChoice.CoverBlew(how), read.Consequence is { Length: > 0 });
                checked_++;
            }
        }

        Assert.True(checked_ > 40, $"only {checked_} reads were cross-checked; this proves little.");
    }

    /// <summary>#741's law, on the blow's own entry: ONE subject, and it is the PLACE. A person minted here
    /// would be a heading naming somebody the game has never printed — the one thing #741 exists to refuse —
    /// and the man on the rota is never named.
    ///
    /// <para><b>RED</b> by minting <c>CaseSubjects.Person</c> or <c>CaseSubjects.Office</c> in
    /// <see cref="PatrolBeat.BlowSubjects"/>.</para></summary>
    [Fact]
    public void TheBlowIsFiledAboutThePlaceAndNothingElse()
    {
        var note = new FieldNote(
            "anything", 0.0, "THE SITE", "👮", PatrolBeat.BlowSubjects("LUNA · THE DEPOT"));

        IReadOnlyList<CaseSubjects.Subject> on = CaseSubjects.On(in note);
        CaseSubjects.Subject one = Assert.Single(on);
        Assert.Equal(CaseSubjects.Kind.Place, one.Of);
        Assert.Equal("LUNA · THE DEPOT", one.Name);

        // An author with no place to name files no subject rather than an empty heading.
        var blank = new FieldNote("anything", 0.0, "", "👮", PatrolBeat.BlowSubjects(""));
        Assert.Empty(CaseSubjects.On(in blank));
    }

    // ── WHERE A DEPARTMENT PASS COMES FROM ────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>IT IS LYING IN ITS OWN DEPARTMENT'S ROOMS AND NOWHERE ELSE</b>, and the room it is lying in is one
    /// the haul table had already called empty — which is what keeps the seven designated finds this ground
    /// keeps from being silently replaced by a laminate nothing on screen would ever mention.
    ///
    /// <para>Measured with both registers installed, so the papers that only exist on a stopped or filled
    /// ground really exist: a guard handed a floor with no other finds on it would prove nothing.</para>
    ///
    /// <para><b>RED</b> by dropping the <see cref="PatrolBeat.ADepartmentOwnsTheFloor"/> clause (passes turn
    /// up in the stores, where a hand already belongs, and the find changes nothing), by dropping the
    /// <c>Haul.Nothing</c> clause (it stands on the pallet), or by dropping the
    /// <c>!FoundPass.IsHere</c> clause (it takes the false ID's drawer, and one press answers twice).</para>
    /// </summary>
    [Fact]
    public void ADepartmentPassLiesInItsOwnDepartmentsRoomsOnly()
    {
        string[] grounds = [.. Grounds()];
        StopOrder.Install([.. grounds]);
        PreservationZone.Install([.. grounds]);
        try
        {
            int kept = 0;
            var departments = new HashSet<string>(StringComparer.Ordinal);

            foreach (string body in grounds)
            {
                if (FoundPass.DepartmentRoomFor(body) is not { } at)
                {
                    continue;
                }
                kept++;
                departments.Add(at.Department);

                // It is the floor's OWN plate, and the floor is one a round walks and a department owns.
                Assert.Equal(UndergroundComplex.DepartmentOf(body, at.Level), at.Department);
                Assert.True(PatrolBeat.IsADepartmentOf(body, at.Department));
                Assert.True(PatrolBeat.IsPatrolled(body, at.Level));
                Assert.True(PatrolBeat.ADepartmentOwnsTheFloor(
                    ChamberFitting.DepartmentOn(body, at.Level)));

                // …and a pass found there is worth something: it opens the floor it was found on, which a
                // GENERAL HANDS pass does not.
                Satchel.Item pass = PatrolBeat.Badge(body, at.Department);
                Assert.Equal(
                    WalletChoice.Outcome.Worked,
                    WalletChoice.WhatHappens(body, at.Level, 0L, pass));
                Assert.Equal(
                    WalletChoice.Outcome.WrongDepartment,
                    WalletChoice.WhatHappens(body, at.Level, 0L, PatrolBeat.Badge(body)));

                // It may only ever stand in a room the haul table has already called empty, and never in
                // the other drawer.
                Assert.InRange(at.RoomIndex, FoundPass.Room, FoundPass.Room + FoundPass.RoomsWalked - 1);
                Assert.Equal(
                    UndergroundComplex.Haul.Nothing,
                    UndergroundComplex.InRoom(body, at.Level, at.RoomIndex));
                Assert.False(FoundPass.IsHere(body, at.Level, at.RoomIndex),
                    $"{body}: both drawers are in {at.Level}/{at.RoomIndex}.");
                Assert.Null(UndergroundComplex.MoneyTrailPaperIn(body, at.Level, at.RoomIndex));

                // …and #701's shelf walks past it, through the one predicate that knows about both drawers.
                Assert.True(FoundPass.APassIsLyingAt(body, at.Level, at.RoomIndex));
                Assert.False(OddBooks.HoldsOne(body, at.Level, at.RoomIndex));
                Assert.False(OddBooks.CouldHoldOne(body, at.Level, at.RoomIndex),
                    $"{body}: the shelf roll can still reach the department drawer.");
            }

            // The rate, MEASURED. Both bounds: a pass in every building is a supply rather than a find, and
            // a pass in none is a feature that is silently dead with every other guard in this file green.
            Assert.True(kept > 40, $"only {kept} of {grounds.Length} grounds keep a department pass.");
            Assert.True(kept < grounds.Length * 3 / 4,
                $"{kept} of {grounds.Length} grounds keep one — that is a supply, not a find.");

            // …and the department is not a constant dressed as a roll.
            Assert.True(departments.Count >= 4,
                $"only {departments.Count} distinct departments were ever dealt: "
                + string.Join(", ", departments.OrderBy(d => d, StringComparer.Ordinal)));
        }
        finally
        {
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }
    }

    /// <summary>
    /// <b>A DRAWER YOU LEAVE IS THE DRAWER YOU COME BACK TO</b> — #1143's own guard, for the second drawer,
    /// and for its own reason: the answer has to survive the PROCESS (<c>string.GetHashCode</c> is randomised
    /// per run in .NET) and has to be a fact about the GROUND rather than about the world's state.
    ///
    /// <para><b>RED</b> by hashing the id into the seed, or by folding any world state into
    /// <see cref="FoundPass.ASiteKeepsADepartmentPass"/>.</para>
    /// </summary>
    [Fact]
    public void TheDepartmentDrawerSurvivesTheProcessAndTheRegisters()
    {
        string source = TheCardCarriesItsOwnStoryTests.ReadRepoFile("src/SpaceSails.Core/FoundPass.cs");
        Assert.DoesNotContain("GetHashCode", source, StringComparison.Ordinal);
        Assert.Contains("DiceRule.Seed($\"dept-pass:kept:{bodyId}\")", source, StringComparison.Ordinal);
        Assert.Contains("DiceRule.Seed($\"dept-pass:floor:{bodyId}\")", source, StringComparison.Ordinal);

        string[] grounds = [.. Grounds().Take(150)];
        (int Level, int RoomIndex, string Department)?[] before =
            [.. grounds.Select(FoundPass.DepartmentRoomFor)];

        StopOrder.Install([.. grounds]);
        PreservationZone.Install([.. grounds]);
        try
        {
            for (int i = 0; i < grounds.Length; i++)
            {
                // The ROOM may honestly move when a register fills the one it was in — that is the emptiness
                // clause doing its job — but whether the site keeps one, and whose it is, may not.
                Assert.Equal(
                    FoundPass.ASiteKeepsADepartmentPass(grounds[i]),
                    FoundPass.ASiteKeepsADepartmentPass(grounds[i]));
                if (before[i] is { } was && FoundPass.DepartmentRoomFor(grounds[i]) is { } now)
                {
                    Assert.Equal(was.Level, now.Level);
                    Assert.Equal(was.Department, now.Department);
                }
            }
        }
        finally
        {
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }

        // …and the grounds really disagree with each other, so the roll is a roll.
        Assert.Contains(true, grounds.Select(FoundPass.ASiteKeepsADepartmentPass));
        Assert.Contains(false, grounds.Select(FoundPass.ASiteKeepsADepartmentPass));
    }

    /// <summary>The face on a department pass is composed by the ONE function that says what is printed on a
    /// pass, in the format the wallet already uses — no second description anywhere. And nothing on it
    /// explains what the place is for (§13.8).</summary>
    [Fact]
    public void TheFaceIsTheOneFormatAndItExplainsNothing()
    {
        string[] forbidden =
        [
            "monolith", "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect",
            "clone", "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
        ];

        foreach (string tier in Tiers("luna").Append(PatrolBeat.BadgeTier))
        {
            Satchel.Item pass = PatrolBeat.Badge("luna", tier);
            string face = PatrolBeat.BadgeTitle("luna", tier);

            Assert.Equal($"SITE PASS · {tier} · {BodyNames.Designation("luna")} SITE", face);
            Assert.Equal(face, PatrolBeat.BadgeFaceOf(pass.Id));
            Assert.Equal(face, WalletChoice.Claims(pass));
            Assert.Equal($"{PatrolBeat.BadgeGlyph} {face}", FoundPass.Plate(pass));

            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, face, StringComparison.OrdinalIgnoreCase);
            }
        }

        // A pass this build cannot read is still shown and still honest about itself.
        Assert.Equal(
            WalletChoice.UnreadableFaceLine,
            WalletChoice.Claims(new Satchel.Item(Satchel.Kind.Badge, "badge:")));
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every department this site's own signage prints. GENERAL HANDS is deliberately not among
    /// them: it is what the site issues, not a department.</summary>
    private static IEnumerable<string> Tiers(string bodyId) =>
        UndergroundComplex.DepartmentsFor(bodyId);

    /// <summary>One of every sort of thing a palm can be handed, for the cross-check sweep.</summary>
    private static IEnumerable<Satchel.Item?> Wallets(string bodyId)
    {
        yield return null;
        yield return PatrolBeat.Badge(bodyId);
        yield return PatrolBeat.Badge(bodyId, "PLANT");
        yield return PatrolBeat.Badge(bodyId, "LABORATORIES");
        yield return PatrolBeat.Badge("ladder-somewhere-else");
        yield return CanteenTable.Chit(underAnotherName: false);
        yield return Inspectorate.Card;
    }

    private static IEnumerable<string> Grounds()
    {
        for (int i = 0; i < Probes; i++)
        {
            yield return $"ladder-ground-{i}";
        }
    }
}
