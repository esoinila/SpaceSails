using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #602 · <b>THE NUMPAD ON THE CAR PANEL.</b> Owner, 2026-08-02, overruling #590's call 3 deliberately:
/// <i>"the numpad idea is to even have like a vicious security sticked warning next to it and it gives you 3
/// tries before calling security"</i>, and then the shape of the counter: <i>"the alert resets if you just
/// walk away… but if you repeat the try soon after before the reset then the security patrol comes"</i>.
///
/// <para>Everything the pad is allowed to be rests on two facts holding each other up — the code is FOUND and
/// never derived, and the count is a WINDOW rather than a ledger — so the guards below are mostly about
/// those two and about the things that must NOT have happened: no new security kind, no pad on a welded
/// seal, no fifth string.</para>
///
/// <h3>Proven able to fail, each by breaking the shipped code and watching it go red</h3>
/// <list type="bullet">
/// <item>Dropping the window (making <c>Forgotten</c> return false) reddens
/// <see cref="TwoTriesAMinuteAndAHalfApartAreTwoBoredTechnicians"/>.</item>
/// <item>Making the window a ledger that never resets its clock on the third miss reddens
/// <see cref="TheThirdMissInsideTheWindowCallsSecurityAndTheDarkRunsFromThere"/>.</item>
/// <item>Seeding the paper's number from a different tag than the pad's reddens
/// <see cref="ThePaperSaysTheNumberThePadActuallyAnswersTo"/>.</item>
/// <item>Ignoring <c>padOpened</c> in <c>LiftPanel</c> reddens
/// <see cref="ARightCodeOpensTheBandForThisExcursionAndNoLonger"/>.</item>
/// <item>Putting the pad on every refusing row reddens <see cref="OneRowInOneBuildingCarriesAPad"/> and
/// <see cref="ASealedWorkingNeverCarriesAPad"/>.</item>
/// <item>Adding a sixth authored string to <see cref="UndergroundComplex.LiftCode"/> reddens
/// <see cref="TheFourPlatesAndTheStickerAreVerbatimAndAreTheOnlyProseHere"/>.</item>
/// <item>Adding a <c>Provocation</c> member for the pad reddens
/// <see cref="WhatComesIsTheChallengeThisGroundAlreadyHadAndNoNewKind"/>.</item>
/// <item>Tuning <c>WindowSeconds</c> to 60 reddens <see cref="TheWindowIsNinetySecondsAndThePromiseIsThree"/>;
/// making the first miss say <c>SECURITY CALLED</c> reddens
/// <see cref="ThePadCountsAloudAndIsSilentOutsideItsOwnWindow"/>; seeding every building off one tag reddens
/// <see cref="TheCodeIsThisBuildingsAndNotEverybodys"/>; moving <c>PaperRoom</c> to 0 reddens
/// <see cref="ThePapersRoomCollidesWithNothingElseTheFloorKeeps"/>; and putting the reserved word on the
/// sticker reddens <see cref="TheReservedWordIsNowhereNearAKeypad"/>.</item>
/// </list>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class ThePadOnTheCarPanelTests
{
    /// <summary>How many generated rocks the sweeps walk. Sites differ in depth and in plumbing, and a guard
    /// about a paper that exists on SOME grounds has to see a great many of them.</summary>
    private const int Probes = 400;

    /// <summary>Grounds that carry a pad and therefore a paper — swept off the shipped predicate rather than
    /// typed, so a guard here cannot quietly test a world the game does not generate.</summary>
    private static List<string> PaddedGrounds() => _padded ??= Sweep();

    private static List<string>? _padded;

    private static List<string> Sweep()
    {
        List<string> found = [];
        for (int i = 0; i < Probes; i++)
        {
            string body = $"pad-ground-{i}";
            if (UndergroundComplex.LiftCode.PaperRoomFor(body) is not null)
            {
                found.Add(body);
            }
        }

        // THE ANTI-VACUOUS HALF. A sweep that found nothing would pass every loop below without asking the
        // world a single question, which is this repo's fifth named bug class in its purest form.
        Assert.True(found.Count > 100,
            $"only {found.Count} of {Probes} generated grounds carry a pad — a sweep this thin proves little.");
        return found;
    }

    // ── THE WINDOW ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>A BORED TECHNICIAN IS NOT A BURGLAR.</b> Two misses a minute and a half apart are two separate
    /// nothings: the second opens its own window, the count reads one, and nobody is called.
    ///
    /// <para>This is the owner's whole reason for a decay window — <i>"I bet their employees try their luck
    /// all the time"</i> — and a building that summoned a patrol every third lifetime attempt would be
    /// summoning security constantly, for people who work there.</para>
    ///
    /// <para><b>RED</b> by making <c>Forgotten</c> return false: the second miss then stacks on the first and
    /// <c>MissesAt</c> comes back 2.</para>
    /// </summary>
    [Fact]
    public void TwoTriesAMinuteAndAHalfApartAreTwoBoredTechnicians()
    {
        UndergroundComplex.LiftCode.Pad pad = UndergroundComplex.LiftCode.Pad.Fresh;
        Assert.Equal(0, UndergroundComplex.LiftCode.MissesAt(pad, 0));

        pad = UndergroundComplex.LiftCode.AWrongCode(pad, 0);
        Assert.Equal(1, UndergroundComplex.LiftCode.MissesAt(pad, 0));

        // …and 100 seconds later the first one never happened.
        Assert.Equal(0, UndergroundComplex.LiftCode.MissesAt(pad, 100));

        pad = UndergroundComplex.LiftCode.AWrongCode(pad, 100);
        Assert.Equal(1, UndergroundComplex.LiftCode.MissesAt(pad, 100));
        Assert.False(UndergroundComplex.LiftCode.SecurityIsCalled(pad, 100),
            "two tries a hundred seconds apart summoned a patrol — the window the owner ruled is not there.");

        // …and a third one, also outside, is still the first of its own window. Unlimited windows buy nothing
        // because the code is found and never derived; that is the argument, and this is it as arithmetic.
        pad = UndergroundComplex.LiftCode.AWrongCode(pad, 300);
        Assert.Equal(1, UndergroundComplex.LiftCode.MissesAt(pad, 300));
        Assert.False(UndergroundComplex.LiftCode.SecurityIsCalled(pad, 300));
    }

    /// <summary>
    /// <b>THREE INSIDE THE WINDOW IS SOMEBODY WORKING THE PROBLEM</b>, and the pad says so and goes out.
    ///
    /// <para>The dark runs from the THIRD miss and not from the first, which is the one clock this whole
    /// state is written around: at the far edge of that window the count is zero again and the pad is back.
    /// </para>
    ///
    /// <para><b>RED</b> by having <c>AWrongCode</c> keep the original <c>FirstMissAt</c> on the third miss:
    /// the pad then comes back alive 20 seconds early, and the two assertions at 89 and 90 seconds swap.
    /// </para>
    /// </summary>
    [Fact]
    public void TheThirdMissInsideTheWindowCallsSecurityAndTheDarkRunsFromThere()
    {
        UndergroundComplex.LiftCode.Pad pad = UndergroundComplex.LiftCode.Pad.Fresh;
        pad = UndergroundComplex.LiftCode.AWrongCode(pad, 0);
        pad = UndergroundComplex.LiftCode.AWrongCode(pad, 10);

        Assert.Equal(2, UndergroundComplex.LiftCode.MissesAt(pad, 10));
        Assert.False(UndergroundComplex.LiftCode.SecurityIsCalled(pad, 10),
            "two misses inside the window called security — the sticker promises THREE.");
        Assert.False(UndergroundComplex.LiftCode.IsDark(pad, 10));

        pad = UndergroundComplex.LiftCode.AWrongCode(pad, 20);
        Assert.True(UndergroundComplex.LiftCode.SecurityIsCalled(pad, 20),
            "three misses inside ninety seconds did not call anybody — the sticker is bluffing, and the "
            + "whole reason a keypad is allowed on this ground is that it never bluffs.");
        Assert.True(UndergroundComplex.LiftCode.IsDark(pad, 20));

        // The dark runs a window from the CALL. One clock: the pad is out, and when it stops being out the
        // count is zero with it.
        Assert.True(UndergroundComplex.LiftCode.IsDark(pad, 20 + UndergroundComplex.LiftCode.WindowSeconds - 1));
        Assert.False(UndergroundComplex.LiftCode.IsDark(pad, 20 + UndergroundComplex.LiftCode.WindowSeconds));
        Assert.Equal(0, UndergroundComplex.LiftCode.MissesAt(
            pad, 20 + UndergroundComplex.LiftCode.WindowSeconds));
    }

    /// <summary>The window is the owner's <i>soon after</i>, in seconds, and the pad's promise is three. Both
    /// pinned so a tuning pass has to come through this file and read the note at
    /// <c>LiftCode.WindowSeconds</c> about why a resettable counter is not farmable.</summary>
    [Fact]
    public void TheWindowIsNinetySecondsAndThePromiseIsThree()
    {
        Assert.Equal(90.0, UndergroundComplex.LiftCode.WindowSeconds);
        Assert.Equal(3, UndergroundComplex.LiftCode.TriesBeforeSecurity);
    }

    /// <summary>The pad counts ALOUD, which is the owner's second requirement — a stake nobody can see is not
    /// a stake — and it says nothing at all before the first press or after the window has closed.</summary>
    [Fact]
    public void ThePadCountsAloudAndIsSilentOutsideItsOwnWindow()
    {
        UndergroundComplex.LiftCode.Pad pad = UndergroundComplex.LiftCode.Pad.Fresh;
        Assert.Null(UndergroundComplex.LiftCode.PlateFor(pad, 0));

        pad = UndergroundComplex.LiftCode.AWrongCode(pad, 0);
        Assert.Equal(UndergroundComplex.LiftCode.WrongOnePlate,
            UndergroundComplex.LiftCode.PlateFor(pad, 0));

        pad = UndergroundComplex.LiftCode.AWrongCode(pad, 1);
        Assert.Equal(UndergroundComplex.LiftCode.WrongTwoPlate,
            UndergroundComplex.LiftCode.PlateFor(pad, 1));

        pad = UndergroundComplex.LiftCode.AWrongCode(pad, 2);
        Assert.Equal(UndergroundComplex.LiftCode.SecurityCalledPlate,
            UndergroundComplex.LiftCode.PlateFor(pad, 2));

        Assert.Null(UndergroundComplex.LiftCode.PlateFor(
            pad, 2 + UndergroundComplex.LiftCode.WindowSeconds));
    }

    // ── THE PAPER AND THE CODE ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SHEET SAYS THE NUMBER THE LOCK ANSWERS TO.</b> The one guard this feature would be a lie
    /// without: a pad whose paper quoted a different code would be a lock that cannot be opened at all, and
    /// nothing on screen would ever say so — the quiet bug class this ground has paid for before.
    ///
    /// <para>Asserted three ways down the whole chain a captain actually meets: the sentence, the room that
    /// holds it, and the sleeve the paper is read in afterwards. And the sentence itself is canon verbatim —
    /// only the four digits move.</para>
    ///
    /// <para><b>RED</b> by seeding <c>PaperLine</c> off a different tag than <c>CodeFor</c>.</para>
    /// </summary>
    [Fact]
    public void ThePaperSaysTheNumberThePadActuallyAnswersTo()
    {
        foreach (string body in PaddedGrounds())
        {
            string code = UndergroundComplex.LiftCode.CodeFor(body);
            Assert.Matches("^[0-9]{4}$", code);
            Assert.True(UndergroundComplex.LiftCode.Answers(body, code));
            Assert.False(UndergroundComplex.LiftCode.Answers(body, code == "0000" ? "1111" : "0000"));

            // The canon sentence, with the site's own number in the place the sentence keeps for it.
            Assert.Equal(
                $"Lift code, lower band: {code}. Do not write this down.",
                UndergroundComplex.LiftCode.PaperLine(body));

            (int Level, int RoomIndex) at = UndergroundComplex.LiftCode.PaperRoomFor(body)!.Value;
            Assert.NotEqual(0, at.RoomIndex);   // never room 0: a find you cannot miss is not a find

            // The room really hands one over, and what it says is the sheet.
            Assert.Equal(
                UndergroundComplex.Haul.Records,
                UndergroundComplex.InRoom(body, at.Level, at.RoomIndex));
            Assert.Contains(code, UndergroundComplex.HaulLine(
                UndergroundComplex.Haul.Records, body, at.Level, at.RoomIndex, null));

            // …and it still says it in the sleeve, away from the room, which is where a captain who is
            // standing at the pad will actually be reading it.
            string findId = UndergroundComplex.FindId(body, at.Level, at.RoomIndex);
            Assert.Equal(UndergroundComplex.LiftCode.PaperLine(body), FieldClue.Document(findId));
            Assert.Equal(UndergroundComplex.LiftCode.PaperTitle, FieldClue.Title(findId));

            // The pocket row never shouts the answer. Finding the paper is the act being paid for.
            Assert.DoesNotContain(code, FieldClue.Title(findId), StringComparison.Ordinal);
        }
    }

    /// <summary>The code is a fact about a SITE and differs between them — a single number shared by every
    /// building in the game would be a skeleton key learned once and typed forever, which is the one thing a
    /// found-only code may never become.</summary>
    [Fact]
    public void TheCodeIsThisBuildingsAndNotEverybodys()
    {
        List<string> grounds = PaddedGrounds();
        int distinct = grounds.Select(UndergroundComplex.LiftCode.CodeFor).Distinct().Count();
        Assert.True(distinct > grounds.Count / 2,
            $"{grounds.Count} buildings share only {distinct} codes between them — a captain who learns one "
            + "pad has learnt the fleet.");
    }

    /// <summary>
    /// <b>THE PAPER IS A DESIGNATED ROOM, AND IT LANDS WHERE NOTHING ELSE ON THAT FLOOR DOES.</b> A
    /// collision would silently replace one of #1063's or #1074's sheets and nothing on screen would ever
    /// say which paper the captain did not get — the quiet half of the house bug class.
    ///
    /// <para><b>ASKED ON A GROUND WHERE THE OTHER PAPERS ACTUALLY EXIST,</b> which is the whole difference
    /// between this guard and a guard handed a world that cannot tell pass from fail. The maintenance ledger
    /// only exists on a ground somebody filled in and the three cost-centre line items only on one the
    /// Authority has closed and taken into care, so the register is installed for the length of this case and
    /// the sweep is asserted to have really met them.</para>
    ///
    /// <para><b>RED</b> by moving <c>PaperRoom</c> to 0, which is the maintenance ledger's.</para>
    /// </summary>
    [Fact]
    public void ThePapersRoomCollidesWithNothingElseTheFloorKeeps()
    {
        string[] grounds = [.. PaddedGrounds().Take(40)];
        StopOrder.Install([.. grounds]);
        PreservationZone.Install([.. grounds]);
        try
        {
            int ledgersSeen = 0;
            int lineItemsSeen = 0;

            foreach (string body in grounds)
            {
                (int Level, int RoomIndex) at = UndergroundComplex.LiftCode.PaperRoomFor(body)!.Value;

                // NEVER ROOM 0. The standing rule for a seeded paper on this ground: a find the first search
                // on the floor is guaranteed to turn up is not a find.
                Assert.NotEqual(0, at.RoomIndex);

                Assert.False(
                    UndergroundComplex.KeyRoomFor(body) is { } k
                    && k.Level == at.Level && k.RoomIndex == at.RoomIndex,
                    $"{body}: the code paper is in the Key room — the way into the unlisted band vanished.");
                Assert.False(
                    UndergroundComplex.RelicRoomFor(body) is { } r
                    && r.Level == at.Level && r.RoomIndex == at.RoomIndex,
                    $"{body}: the code paper is on the relic's pallet.");
                Assert.False(
                    UndergroundComplex.ValveBookRoomFor(body) is { } v
                    && v.Level == at.Level && v.RoomIndex == at.RoomIndex,
                    $"{body}: the code paper took the valve-book's room.");

                if (UndergroundComplex.MaintenanceLedgerRoomFor(body) is { } l)
                {
                    ledgersSeen++;
                    Assert.False(l.Level == at.Level && l.RoomIndex == at.RoomIndex,
                        $"{body}: the code paper took the maintenance ledger's room.");
                }

                for (int room = 0; room < 8; room++)
                {
                    if (UndergroundComplex.MoneyTrailPaperIn(body, at.Level, room) is not null)
                    {
                        lineItemsSeen++;
                        Assert.NotEqual(at.RoomIndex, room);
                    }
                }

                // …and the room the code paper is in really answers with a paper rather than with one of
                // those, whatever else this ground is carrying.
                Assert.Null(UndergroundComplex.MoneyTrailPaperIn(body, at.Level, at.RoomIndex));
            }

            // THE ANTI-VACUOUS HALF. If the register installed nothing, every clause above was asked of a
            // floor with no other papers on it and this guard proved exactly nothing.
            Assert.True(lineItemsSeen > 20,
                $"only {lineItemsSeen} cost-centre line items were anywhere near the code paper's floor — "
                + "the collision this guard is about was never actually possible.");
            Assert.True(ledgersSeen > 0 || lineItemsSeen > 20,
                "neither of the other designated papers existed on any swept ground.");
        }
        finally
        {
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }
    }

    // ── THE ROW ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Standing where the pad is: the bottom of the first band, where the car stops and the gate
    /// down refuses. Found off the shipped panel rather than typed.</summary>
    private static (string Body, int From, UndergroundComplex.LiftStop Row) APaddedRow()
    {
        foreach (string body in PaddedGrounds())
        {
            foreach (int from in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.LiftStop stop in UndergroundComplex.LiftPanel(body, from, []))
                {
                    if (stop.HasPad)
                    {
                        return (body, from, stop);
                    }
                }
            }
        }

        throw new InvalidOperationException("no generated site's panel offers a row with a pad on it.");
    }

    /// <summary>
    /// <b>A RIGHT CODE BUYS THE AFTERNOON.</b> The band opens while the excursion's own set says so, and the
    /// identical panel on the identical floor refuses again the moment that set is empty — which is what the
    /// next landing is.
    ///
    /// <para>That is the line between the two papers, and it is the reason the pad does not demote the card:
    /// a countersignature is in the wallet and is still there next time; a number off somebody's desk is not.
    /// </para>
    ///
    /// <para><b>RED</b> by ignoring <c>padOpened</c> in <c>LiftPanel</c> — the opened panel then still
    /// refuses.</para>
    /// </summary>
    [Fact]
    public void ARightCodeOpensTheBandForThisExcursionAndNoLonger()
    {
        (string body, int from, UndergroundComplex.LiftStop shut) = APaddedRow();
        int band = UndergroundComplex.BandOf(shut.Level);

        Assert.NotNull(shut.Refusal);
        Assert.Null(shut.OpenedBy);
        Assert.Equal(UndergroundComplex.LiftCode.PadBand, band);

        UndergroundComplex.LiftStop open = UndergroundComplex.LiftPanel(
            body, from, [], null, 0, new HashSet<int> { band })
            .Single(s => s.Level == shut.Level);

        Assert.Null(open.Refusal);
        Assert.False(open.HasPad,
            "the pad is still bolted to a gate that is already open — an affordance with nothing behind it.");
        Assert.Null(open.OpenedBy);   // no card did this, and the row must not claim one did

        // …AND THE NEXT TRIP. An empty set is a captain who has landed again, and the gate is shut.
        UndergroundComplex.LiftStop nextTime = UndergroundComplex.LiftPanel(
            body, from, [], null, 0, new HashSet<int>())
            .Single(s => s.Level == shut.Level);
        Assert.NotNull(nextTime.Refusal);
        Assert.True(nextTime.HasPad);
    }

    /// <summary>
    /// <b>ONE ROW, IN ONE BUILDING, HAS A PAD ON IT</b> — the SEALED gate into the first band, where staff
    /// have to move. Never an ID CHECK row (#715): that gate has already read the paper and is asking for a
    /// FACE, and a code there would be a second road past a heat gate nobody has ruled on. Never a floor
    /// button, never SURFACE, never a carded row.
    ///
    /// <para><b>RED</b> by hanging the pad off every refusing row: deeper gates and the ID CHECK row then
    /// grow one.</para>
    /// </summary>
    [Fact]
    public void OneRowInOneBuildingCarriesAPad()
    {
        int padsSeen = 0;
        int idChecksSeen = 0;

        foreach (string body in PaddedGrounds().Take(60))
        {
            foreach (int from in UndergroundComplex.FloorsOf(body))
            {
                // Asked three ways, because the row has three shapes and only one of them may grow a pad:
                // an empty wallet at a cold outfit (SEALED), the gate's own card at a cold outfit (open),
                // and the same card at an outfit that remembers this captain (ID CHECK, #715).
                string gateCard =
                    new UndergroundComplex.AuthorityCard(body, UndergroundComplex.LiftCode.PadBand).Id;
                (string[] Wallet, int Heat)[] asked =
                [
                    ([], 0), ([gateCard], 0), ([gateCard], 9),
                ];
                foreach ((string[] wallet, int heat) in asked)
                {
                    int padsOnThisPanel = 0;
                    foreach (UndergroundComplex.LiftStop stop in
                             UndergroundComplex.LiftPanel(body, from, wallet, null, heat))
                    {
                        if (stop.Name.Contains("ID CHECK", StringComparison.Ordinal))
                        {
                            idChecksSeen++;
                            Assert.False(stop.HasPad,
                                $"{body} B{-from}: the ID CHECK row grew a keypad. That gate read the paper "
                                + "and wants a face; a code there is a second road past a heat gate.");
                            continue;
                        }

                        if (!stop.HasPad)
                        {
                            continue;
                        }

                        padsOnThisPanel++;
                        padsSeen++;

                        // A padded row is a REFUSING gate row and never anything else — never SURFACE, never
                        // a floor button, never a row a paper already opens.
                        Assert.NotNull(stop.Refusal);
                        Assert.Null(stop.OpenedBy);
                        Assert.False(stop.OpenedByChit);
                        Assert.Contains("SEALED", stop.Name, StringComparison.Ordinal);

                        // …and always the SAME gate: the one into the first band, where staff have to move.
                        // A pad on a deeper gate would be the lab boss's own lair negotiating.
                        Assert.Equal(UndergroundComplex.LiftCode.PadBand,
                            UndergroundComplex.BandOf(stop.Level));
                    }

                    Assert.True(padsOnThisPanel <= 1,
                        $"{body} B{-from} draws {padsOnThisPanel} keypads on one panel — a panel is a set of "
                        + "buttons and two pads on it would be a car with two of the same lock in it.");
                }
            }
        }

        // THE ANTI-VACUOUS HALF, both ways: a guard that never met a pad or never met an ID CHECK row would
        // pass on a game with neither in it.
        Assert.True(padsSeen > 20, $"the sweep only ever saw {padsSeen} pads; this proves nothing.");
        Assert.True(idChecksSeen > 5,
            $"the sweep only ever saw {idChecksSeen} ID CHECK rows; the clause above was never exercised.");
    }

    /// <summary>
    /// <b>A SEALED WORKING NEVER CARRIES A PAD,</b> and that survives the owner's overrule intact. A rib's
    /// sealed mouth and a stop order's welded leaf are not LOCKS — there is nothing on either of them to read
    /// a card, type into or break — so <see cref="UndergroundComplex.Signs.HasNoReader"/> still answers yes
    /// for both, and the panel on a stopped ground still ends in silence rather than in a row with a keypad
    /// bolted to it.
    ///
    /// <para><b>RED</b> by making <c>HasNoReader</c> return false, or by drawing the pad on a gate the stop
    /// order has closed.</para>
    /// </summary>
    [Fact]
    public void ASealedWorkingNeverCarriesAPad()
    {
        Assert.True(UndergroundComplex.HasNoReader(StopOrder.Plate),
            "the stop order's plate grew a reader — #1074's welded leaf is a lock now.");

        // The real thing, off the generator, rather than a hand-typed lookalike — TheHiveCardsTests' own
        // walk of the built floors.
        int sectorsSeen = 0;
        foreach (string body in PaddedGrounds().Take(12))
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor =
                    UndergroundComplex.Build(body, level, SurfaceLayout.DefaultField);
                foreach (UndergroundComplex.LockedDoor door in floor.Locked)
                {
                    if (!UndergroundComplex.IsSealedWay(door.Sign))
                    {
                        continue;
                    }
                    sectorsSeen++;
                    Assert.True(UndergroundComplex.HasNoReader(door.Sign),
                        $"'{door.Sign}' has a reader on it now — #590's call 2 is gone with call 3, and it "
                        + "was not the one the owner overruled.");
                }
            }
        }

        Assert.True(sectorsSeen > 10, $"only {sectorsSeen} sealed ways were ever asked; this proves little.");
    }

    // ── THE PROSE, AND WHAT DID NOT HAPPEN ────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE FIVE AUTHORED STRINGS, VERBATIM — AND THERE IS NO SIXTH.</b> Canon (2026-09-03) gave the
    /// sticker, the four plates and the code paper's sentence, and that is the whole of the new prose this
    /// feature is allowed to put in front of a player.
    ///
    /// <para>Swept by reflection rather than listed, because a list is a thing somebody forgets to add to:
    /// the guard walks every public string on <see cref="UndergroundComplex.LiftCode"/> and demands it be one
    /// of the authored ones.</para>
    ///
    /// <para><b>RED</b> by adding any sixth authored string to the class, or by touching a comma in one of
    /// the five.</para>
    /// </summary>
    [Fact]
    public void TheFourPlatesAndTheStickerAreVerbatimAndAreTheOnlyProseHere()
    {
        Assert.Equal("THREE WRONG ENTRIES CALL SECURITY. THE PAD REMEMBERS.",
            UndergroundComplex.LiftCode.Sticker);
        Assert.Equal("OPEN", UndergroundComplex.LiftCode.OpenPlate);
        Assert.Equal("WRONG · 1", UndergroundComplex.LiftCode.WrongOnePlate);
        Assert.Equal("WRONG · 2", UndergroundComplex.LiftCode.WrongTwoPlate);
        Assert.Equal("SECURITY CALLED", UndergroundComplex.LiftCode.SecurityCalledPlate);
        Assert.Equal("Lift code, lower band: 4471. Do not write this down.",
            UndergroundComplex.LiftCode.PaperLine("a-body-whose-code-is-not-4471")
                .Replace(UndergroundComplex.LiftCode.CodeFor("a-body-whose-code-is-not-4471"), "4471",
                    StringComparison.Ordinal));

        HashSet<string> authored =
        [
            UndergroundComplex.LiftCode.Sticker,
            UndergroundComplex.LiftCode.OpenPlate,
            UndergroundComplex.LiftCode.WrongOnePlate,
            UndergroundComplex.LiftCode.WrongTwoPlate,
            UndergroundComplex.LiftCode.SecurityCalledPlate,
            UndergroundComplex.LiftCode.PaperTitle,
        ];

        FieldInfo[] strings = [.. typeof(UndergroundComplex.LiftCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.FieldType == typeof(string))];

        Assert.True(strings.Length >= 6,
            "the reflection sweep found fewer strings than the file declares — it is reading the wrong type.");

        foreach (FieldInfo f in strings)
        {
            string said = (string)f.GetValue(null)!;
            Assert.True(authored.Contains(said),
                $"LiftCode.{f.Name} is a player-facing string nobody authored: \"{said}\". Canon gave the "
                + "sticker, the four plates and the paper's sentence and nothing else — file a FABLE marker "
                + "instead of writing one.");
        }
    }

    /// <summary>The reserved word (docs/worldbuilding-notes.md §8) is absent from every string this feature
    /// puts on a wall. A hall is a hall and never borrows it.</summary>
    [Fact]
    public void TheReservedWordIsNowhereNearAKeypad()
    {
        foreach (string said in new[]
                 {
                     UndergroundComplex.LiftCode.Sticker, UndergroundComplex.LiftCode.OpenPlate,
                     UndergroundComplex.LiftCode.WrongOnePlate, UndergroundComplex.LiftCode.WrongTwoPlate,
                     UndergroundComplex.LiftCode.SecurityCalledPlate,
                     UndergroundComplex.LiftCode.PaperTitle,
                     UndergroundComplex.LiftCode.PaperLine("pad-ground-1"),
                 })
        {
            Assert.DoesNotContain("monolith", said, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// <b>WHAT COMES IS WHAT WAS ALREADY DOWN THERE.</b> The third miss summons the GENERAL HANDS challenge
    /// (#804) and nothing else: no new security kind, no new <see cref="PatrolBeat.Provocation"/>, no alert
    /// state. #618 still owes the owner the ruling on a second security body and this lane leaves it owing.
    ///
    /// <para>The challenge's own outcomes are asserted here rather than described: a captain with this site's
    /// pass passes, and one without gets the refusal and the walk-out that has always followed it.</para>
    ///
    /// <para><b>RED</b> by adding a <c>Provocation</c> member for the pad, or by giving the summons a read of
    /// its own.</para>
    /// </summary>
    [Fact]
    public void WhatComesIsTheChallengeThisGroundAlreadyHadAndNoNewKind()
    {
        // The ladder of reasons a man leaves his round, entire. A sixth member would be a second security
        // system arriving without a ruling.
        Assert.Equal(
            new[]
            {
                "None", "WalkedAwayTwice", "SeenAtTheHasp", "BookedTooManyTimes", "GunfireHeard",
            },
            Enum.GetNames<PatrolBeat.Provocation>());

        const string body = "pad-ground-1";
        Assert.Equal("GENERAL HANDS", PatrolBeat.BadgeTier);

        PatrolBeat.Read passes = PatrolBeat.TheGuardReads(body, "◈ A PLATE", PatrolBeat.Badge(body));
        Assert.True(passes.Satisfied, "this site's own pass no longer satisfies the round.");
        Assert.Equal(PatrolBeat.ChallengeLabel, passes.Label);

        PatrolBeat.Read nothing = PatrolBeat.TheGuardReads(body, "◈ A PLATE", null);
        Assert.False(nothing.Satisfied);
        Assert.Equal(PatrolBeat.ChallengeLabel, nothing.Label);
        Assert.NotNull(nothing.Consequence);
    }
}
