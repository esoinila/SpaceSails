using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1151 slice 4 · <b>THE WAITING WRIT IS SERVED, AND THERE IS ONE WRIT</b>.
///
/// <para>What this part owns is what a collector leaves on the file when the master is off her: the writ is
/// served when he comes back aboard her <i>at that berth</i>, priced on the terms it had the day it was
/// filed, and there is exactly one of them under any sequence of catches. Nothing here is about the
/// counter; everything here is about the file the counter's world keeps.</para>
///
/// <para>Its own bench steps — a castaway with a writ waiting, the whole counter pressed — sit at the foot
/// of the file rather than in <c>…World.cs</c>, because they are this slice's recipe and no other part of
/// the class uses them.</para>
/// </summary>
public sealed partial class TheClaimIsWalkedEndToEndTests
{
    // ══ 5 · #1151 SLICE 4 · THE WAITING WRIT IS SERVED, AND THERE IS ONE WRIT ════════════════════════════

    /// <summary>
    /// <b>THE SCENE THE FILE WAS WRITTEN FOR.</b> He scuttles her over a moon and gets clear; the contract
    /// goes onto the file at the harbour that serves that ground; the tug sets him down at that very harbour
    /// — and the man who has been standing on that ramp since the ending walks up it.
    ///
    /// <para>Nothing here is written by hand. The writ is filed by the ending itself, the berth is the one
    /// <c>WakeAtNearestHaven</c> chose, and the service is a frame of the running game. The assertion the
    /// slice is about is the FLIP: the ledger carried <c>WRIT · AWAITING THE MASTER</c> and now carries no
    /// row at all, because the demand card a served writ has always shown is up instead.</para>
    /// </summary>
    [Fact]
    public void THE_WAITING_WritIsServedWhenHeIsBackAboardHerAtThatPort()
    {
        Pages.Map map = ACastawayWithAWritWaitingForHim(out string berth);

        // Still waiting while the ending's own card is up — one card at a time, and the terms are on the
        // file, so a beat's wait is not a discount.
        Assert.NotNull(Read(map, "_shipEpitaph"));
        Assert.NotNull(Read(map, "_writPending"));
        Assert.NotNull(TheWritRow(map));
        Assert.Equal(berth, (string?)Read(map, "_dockedHavenId"));
        Assert.True((bool)Invoke(map, "TheMasterIsAboardHer")!);

        // …and it stays waiting for as long as that card is up. Fifty frames of the running game with every
        // other condition already met, and nothing opens over the top of the ending that filed it.
        RunFrames(map, seconds: 5);
        Assert.Null(Read(map, "_busted"));
        Assert.NotNull(Read(map, "_writPending"));

        Invoke(map, "CloseShipEpitaph");
        RunFrames(map, seconds: 1);

        object demand = Read(map, "_busted") ?? throw new InvalidOperationException(
            "he came back aboard her at their own berth and nobody was standing there.");
        Assert.Equal(WaitingCallsign, (string)Get(demand, "HunterCallsign")!);
        Assert.Null(Read(map, "_writPending"));
        Assert.Null(TheWritRow(map));               // the plate is gone; the card is the row now
    }

    /// <summary>
    /// <b>AND IT DOES NOT FOLLOW HIM ASHORE, OR ANYWHERE ELSE.</b> The presence law is not suspended by the
    /// captain being in the right postcode: three hundred metres of concourse at that same port is still not
    /// being aboard her, and another port is not that port. In both the writ simply keeps waiting, with the
    /// plate still on the ledger — and then, from the same world, he does the one thing that serves it.
    /// </summary>
    [Theory]
    [InlineData(true)]      // past the tube, on their own concourse, with her clamped to their collar
    [InlineData(false)]     // aboard her, but gone from their berth
    public void THE_WAITING_WritKeepsWaitingUntilHeIsAboardHerAtThatVeryBerth(bool ashore)
    {
        Pages.Map map = ACastawayWithAWritWaitingForHim(out string berth);

        // Moved BEFORE the ending's card comes down, because coming down is the last thing between him and
        // being served: a frame with him on her at their berth is a frame the writ is served on.
        if (ashore)
        {
            Invoke(map, "SetDeckForDock", berth);    // the concourse the wake tied him up to
            WalkHimAshore(map);
        }
        else
        {
            Set(map, "_dockedHavenId", null);        // he let go of their collar and left
        }

        Invoke(map, "CloseShipEpitaph");
        RunFrames(map, seconds: 20);

        Assert.Null(Read(map, "_busted"));
        Assert.NotNull(Read(map, "_writPending"));
        Assert.Equal(NebulaClaims.PendingWritPlate,
            (string)Get(TheWritRow(map) ?? throw new InvalidOperationException(
                "the writ is still waiting and the ledger stopped saying so."), "Title")!);

        // …and the same world, with him back on her at their berth: served.
        if (ashore)
        {
            Set(map, "_avatarY", -40.0);
            Invoke(map, "RefreshAshore");
        }
        else
        {
            Set(map, "_dockedHavenId", berth);
        }

        Assert.True((bool)Invoke(map, "TheMasterIsAboardHer")!);
        RunFrames(map, seconds: 1);
        Assert.NotNull(Read(map, "_busted"));
        Assert.Null(Read(map, "_writPending"));
    }

    /// <summary>
    /// <b>ON THE TERMS IT HAD ON THE DAY.</b> No discount for the wait and no penalty for it: the demand a
    /// served writ opens carries the heat that bought the contract and the seed the moment of filing cut,
    /// not the gauge as it stands when he finally walks back up the ramp.
    ///
    /// <para>Asked in a world that would answer differently — the gauge is buried when the writ is filed and
    /// back at the floor when it is served, so a demand priced today is a demand a captain chose for himself
    /// by loitering. Both numbers are computed from the shipping rule rather than typed, and the guard also
    /// asserts they are NOT the numbers today would have given, because two heats that happened to quote the
    /// same bribe would make this test agree with the bug.</para>
    /// </summary>
    [Fact]
    public void THE_SERVED_WritIsPricedOnTheDayItWasFiledAndNotOnTheDayItIsServed()
    {
        Pages.Map map = ACastawayWithAWritWaitingForHim(out _, heatWhenFiled: 3);

        object writ = Read(map, "_writPending")!;
        Assert.Equal(3, (int)Get(writ, "HeatWhenFiled")!);

        // The heat the contract was worth is long gone by the time he is back on her.
        Set(map, "_heat", new HeatState(0, (double)Read(map, "SimTime")!));
        Invoke(map, "CloseShipEpitaph");
        RunFrames(map, seconds: 1);

        object demand = Read(map, "_busted")!;
        ulong theDay = DiceRule.Seed("busted", 0, (long)(double)Get(writ, "FiledAtSimTime")!);
        Assert.Equal(3, (int)Get(demand, "Heat")!);
        Assert.Equal(theDay, (ulong)Get(demand, "Seed")!);
        Assert.Equal(BustedRule.BribeDemand(3, theDay).Total, ((DiceRoll)Get(demand, "Bribe")!).Total);

        // …and that is not what the gauge in front of him would have bought.
        ulong today = DiceRule.Seed("busted", 0, (long)(double)Read(map, "SimTime")!);
        Assert.NotEqual(BustedRule.BribeDemand(1, today).Total, ((DiceRoll)Get(demand, "Bribe")!).Total);
    }

    /// <summary>
    /// <b>AND A WRIT FILED BEFORE THE TERMS EXISTED IS SERVED AT THE DEMAND'S OWN FLOOR.</b> Slice 1 shipped
    /// this record with three fields and no terms on it, and a voyage saved between then and now can have one
    /// on the file right this minute. It is served — a file that could not be served would be a captain stuck
    /// owing a process nobody can close — at heat 1, the floor every demand in the game already has, and
    /// <b>not</b> off the gauge in front of him, which is set to 3 here precisely so the two answers differ.
    /// </summary>
    [Fact]
    public void A_WRIT_FiledBeforeTheTermsExistedIsServedAtTheDemandsOwnFloor()
    {
        Pages.Map map = ACastawayWithAWritWaitingForHim(out string berth);
        object filed = Read(map, "_writPending")!;

        // The file as slice 1 wrote it: whose contract, which berth, when — and nothing else.
        Set(map, "_writPending", new PendingWritRecord(
            (string)Get(filed, "Callsign")!, berth, (double)Get(filed, "FiledAtSimTime")!));
        Set(map, "_heat", new HeatState(3, (double)Read(map, "SimTime")!));

        Invoke(map, "CloseShipEpitaph");
        RunFrames(map, seconds: 1);

        object demand = Read(map, "_busted") ?? throw new InvalidOperationException(
            "a writ from before the terms existed can never be served, and the captain owes it forever.");
        Assert.Equal(1, (int)Get(demand, "Heat")!);
    }

    /// <summary>
    /// <b>ONE WRIT, NOT A QUEUE — UNDER ANY SEQUENCE OF CATCHES.</b> A writ is on the file and the captain is
    /// aboard her in the dark, which is every condition a collector needs except the one this slice adds. Two
    /// more collectors are put on top of her, one after another, each of them parked exactly where she is so
    /// that on any frame she is allowed to close she HAS closed; then he scuttles a second hull over a second
    /// ground with a third collector on him. Through all of it the file holds exactly one writ, and it is the
    /// first one.
    ///
    /// <para>The deferral has the shape the sim already gives a pursuer who cannot proceed: they hold station
    /// — not caught, not broken off, still out there — because the second man on the ramp has no line and
    /// never had one.</para>
    ///
    /// <para><b>And the world can answer the other way.</b> The last act clears the file and does nothing
    /// else, in the same world with the same collector, and she closes on the first frame she is allowed to.
    /// Without that arm this guard would pass on a game where collectors had simply stopped working.</para>
    /// </summary>
    [Fact]
    public void THERE_IsOneWritOnTheFileUnderAnySequenceOfCatches()
    {
        Pages.Map map = ACastawayWithAWritWaitingForHim(out _);
        Set(map, "_dockedHavenId", null);          // away from their berth: nothing here can be SERVED
        Invoke(map, "CloseShipEpitaph");
        Assert.True((bool)Invoke(map, "TheMasterIsAboardHer")!);

        double filedAt = (double)Get(Read(map, "_writPending")!, "FiledAtSimTime")!;

        // The ending's own flags, off the roster: a pursuer the ending broke off is not somebody deferring,
        // and a hold that never retires him is slice 1's behaviour, not this guard's subject.
        ((IList)Read(map, "_hunters")!).Clear();

        foreach (string second in new[] { "SALT WIDOW", "BAILIFF" })
        {
            PutACollectorOnTopOfHer(map, second);
            RunFrames(map, seconds: 10);

            Assert.Null(Read(map, "_busted"));
            foreach (object deferring in (IList)Read(map, "_hunters")!)
            {
                Assert.False((bool)Get(deferring, "CaughtPlayer")!, "a second writ was served over the first.");
                Assert.False((bool)Get(deferring, "BrokenOff")!, "the second man gave up instead of deferring.");
            }

            TheFileHoldsExactlyTheFirstWrit(map, filedAt);
        }

        // …and a second ending, over a second ground, with a third pursuer on him: still one writ.
        ((IList)Read(map, "_hunters")!).Clear();
        PutHimOnAGround(map);
        PutACollectorOnTopOfHer(map, "THIRD MAN");
        ArmHerCharges(map);
        RunUntilSheGoes(map);
        TheFileHoldsExactlyTheFirstWrit(map, filedAt);

        // ── THE OTHER WAY. Clear the file and change nothing else: the sky works again at once.
        Invoke(map, "CloseShipEpitaph");
        Set(map, "_dockedHavenId", null);
        Set(map, "_writPending", null);
        ((IList)Read(map, "_hunters")!).Clear();
        PutACollectorOnTopOfHer(map, "SALT WIDOW");
        RunFrames(map, seconds: 10);
        Assert.NotNull(Read(map, "_busted"));
    }

    /// <summary>The one writ the file is allowed to hold, identified by the moment it was filed — a second
    /// writ that replaced the first would carry a later one.</summary>
    private static void TheFileHoldsExactlyTheFirstWrit(Pages.Map map, double filedAt)
    {
        object writ = Read(map, "_writPending") ?? throw new InvalidOperationException(
            "the writ came off the file without anybody serving it.");
        Assert.Equal(WaitingCallsign, (string)Get(writ, "Callsign")!);
        Assert.Equal(filedAt, (double)Get(writ, "FiledAtSimTime")!);
    }

    /// <summary>The callsign of the collector who files the writ in every slice-4 guard.</summary>
    private const string WaitingCallsign = "GRIMHOLD";

    /// <summary>
    /// A captain who scuttled her over a moon and got clear, with that pursuer's contract on the file — built
    /// by the game: a real excursion, a real collector, the panel's own three verbs, and the ending's own
    /// filing. <paramref name="berth"/> comes back as the harbour the writ is waiting at, asserted to be the
    /// one the wake actually set him down at, because a guard about coming back to their berth is worth
    /// nothing if he was never taken to it.
    /// </summary>
    private static Pages.Map ACastawayWithAWritWaitingForHim(out string berth, int heatWhenFiled = 0)
    {
        Pages.Map map = Boot();
        PutHimOnAGround(map);
        PutACollectorOnTopOfHer(map, WaitingCallsign);
        if (heatWhenFiled > 0)
        {
            Set(map, "_heat", new HeatState(heatWhenFiled, (double)Read(map, "SimTime")!));
        }

        ArmHerCharges(map);
        RunUntilSheGoes(map);

        object writ = Read(map, "_writPending") ?? throw new InvalidOperationException(
            "the ending filed no writ, so there is nothing for this file's guards to serve.");
        berth = (string)Get(writ, "HavenId")!;
        Assert.Equal(berth, (string?)Read(map, "_dockedHavenId"));
        return map;
    }

    /// <summary>A captain who has lost her at a berth, is insured, and has not been near a machine: the
    /// world every slice-2 guard above starts in.</summary>
    private static Pages.Map TheCastawayWithALossOnTheWire()
    {
        Pages.Map map = Boot();
        ClampAtThePort(map);
        Set(map, "_insurance",
            NebulaRep.PolicyAfterBuying(InsuranceTier.Premium, (double)Read(map, "SimTime")!));

        ArmHerCharges(map);
        WalkHimAshore(map);
        RunUntilSheGoes(map);
        Assert.Contains(
            (IReadOnlyList<NewsWire.NewsEvent>)Read(map, "_newsEvents")!,
            e => e.Kind == NewsWire.NewsEventKind.HullLostAtABerth);
        return map;
    }

    /// <summary>Every press the counter still wants, correct and in order, off its own rows — wherever it is
    /// standing. One helper for both hosts, because there is one counter.</summary>
    private static void PressTheWholeCounter(Pages.Map map)
    {
        while (NebulaClaims.NextPress(PressesTaken(map)) is { } press)
        {
            IReadOnlyList<NebulaClaims.Ask> rows = TheRows(map);
            Assert.NotEmpty(rows);
            NebulaClaims.Ask take = press == NebulaClaims.Press.Hull
                ? rows.Single(r => r.Offer == (string)Invoke(map, "ShipNameNow")!)
                : rows[0];
            Press(map, take);
        }
    }
}
