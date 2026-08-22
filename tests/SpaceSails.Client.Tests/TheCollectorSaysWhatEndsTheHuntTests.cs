using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #962/#961 · THE COLLECTOR'S CARD SAYS WHO BOUGHT HER AND WHAT ENDS IT — and quotes the reach the sim
/// actually enforces.
///
/// <para>Owner, docked at a haven with the heat gauge reading zero and a collector still inbound: <i>"So we
/// have zero heat and are docked at haven … why is this still hunting us?"</i> Every rule that answers him
/// was already in <c>EncounterRule</c> — the contract is bought once and does not cool with the heat, two
/// unbroken days at a haven make her lose the scent, warning shots erode her nerve, a holed sail ends it —
/// and not one of them was ever said on a screen he was looking at.</para>
///
/// <para>And the one number the card DID quote was wrong: <i>"driver reach ≈ 691200 km"</i> was muzzle × a
/// day, while <c>InWeaponRange</c> gated every warning shot at 200,000 km. A card and a sim disagreeing by
/// three and a half times, which is this repo's fifth named bug class, live on the screen in his
/// screenshot.</para>
///
/// <para>The sentences themselves are composed in Core beside the rules they describe, and the arithmetic
/// agreement between sentence and rule is swept there (<c>EncounterRuleTests</c>). What Core cannot see is
/// the thing that would actually break here: a PAGE that composes its own copy, or hands Core numbers off
/// the wrong clock, or lets two desks say different things. So this file drives the shipping page and asks
/// the dossier and the war room what they are about to render.</para>
///
/// <para><b>Red proof (run before shipping).</b> Put <c>double reach = MaxMuzzleSpeed * 86400;</c> back in
/// <c>Map.Npc.DossierFor</c> and the reach test goes red at the boundary. Drop the terms out of
/// <c>WarRoomHunters()</c> and the two-desks test goes red. Hand <c>TermsOfTheHunt</c> a hard-coded
/// <c>hiddenNow: true</c> and the not-running test goes red. All three were watched red.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCollectorSaysWhatEndsTheHuntTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private const string HunterId = "hunter-0";
    private const string Callsign = "Debt Collector";
    private const string RobbedHull = "CARDINAL DRIFT";

    // ── The reach the card quotes is the reach the sim enforces ───────────────────────────────────────

    /// <summary>
    /// ONE REACH, AT THE BOUNDARY. The card's "inside the driver's reach" and the sim's
    /// <see cref="EncounterRule.InWeaponRange"/> are asked about the same two positions, a hair either side
    /// of the envelope. They used to disagree over the whole 200,000–691,200 km band — the band the owner
    /// was flying in while the card told him to close 0.18 AU for a firing solution.
    /// </summary>
    [Theory]
    [InlineData(0.999, true)]
    [InlineData(1.001, false)]
    public void THE_CARD_AND_THE_SIM_AgreeAboutTheDriversReachAtItsVeryEdge(double fraction, bool expected)
    {
        double range = EncounterRule.WeaponRangeMeters * fraction;
        Pages.Map map = AChaseInProgress(hunterRange: range);

        object dossier = Dossier(map);
        bool cardSaysInReach = (bool)Field(dossier, "InDriverReach")!;
        bool simSaysInRange = EncounterRule.InWeaponRange(
            new ShipState(Vector2d.Zero, Vector2d.Zero, 0),
            new ShipState(new Vector2d(range, 0), Vector2d.Zero, 0));

        Assert.Equal(expected, simSaysInRange); // the bench really did straddle the envelope
        Assert.Equal(simSaysInRange, cardSaysInReach);
    }

    /// <summary>…AND THE LASSO IS SHORTER THAN THE GUN, where the captain reads it. A collector sitting
    /// exactly on her own catch envelope is inside our driver's reach — which is the whole of the owner's
    /// complaint: <i>"It is like being outside of bullets range but still getting lassoed."</i></summary>
    [Fact]
    public void A_COLLECTOR_OnHerOwnCatchEnvelope_IsInsideOurGuns()
    {
        Pages.Map map = AChaseInProgress(hunterRange: EncounterRule.CatchRadiusMeters);

        Assert.True((bool)Field(Dossier(map), "InDriverReach")!);
    }

    // ── What ends the hunt ────────────────────────────────────────────────────────────────────────────

    /// <summary>THE CARD NAMES THE JOB THAT BOUGHT HER. The robbery that spawns a collector had the robbed
    /// hull in hand and threw it away at the spawn call, so "why is this still hunting us" was unanswerable
    /// from any screen.</summary>
    [Fact]
    public void THE_DOSSIER_NamesTheWarrantAndThatAContractDoesNotCool()
    {
        Pages.Map map = AChaseInProgress(hunterRange: 1e9);

        string warrant = (string)Field(Terms(Dossier(map)), "Warrant")!;

        Assert.Contains(RobbedHull, warrant, StringComparison.Ordinal);
        Assert.Contains("Heat cools; a contract does not", warrant, StringComparison.Ordinal);
    }

    /// <summary>THE HAVEN CLOCK ON THE CARD IS THE PAGE'S OWN CLOCK. The page owns
    /// <c>_hiddenAtHavenSinceSimTime</c> — the exact value it feeds <see cref="EncounterRule.ApplyBreakOff"/>
    /// every tick — and the card has to be quoting THAT, not a second clock of its own. Asserted against
    /// Core recomputed from the same field, so a page that invents its own elapsed time fails here.</summary>
    [Fact]
    public void THE_DOSSIER_CountsDownTheSameHavenClockTheBreakOffRuleReads()
    {
        double hiddenFor = 0.6 * EncounterRule.BreakOffHiddenDays * 86400;
        Pages.Map map = AChaseInProgress(hunterRange: 1e9, hiddenForSeconds: hiddenFor);

        Assert.Equal(
            EncounterRule.HidingTerm(hiddenFor, hiddenNow: true),
            (string)Field(Terms(Dossier(map)), "Hiding")!);
    }

    /// <summary>…AND SAYS SO WHEN NOTHING IS COUNTING. Flying free with a collector inbound, "1.4 d to go"
    /// would be the lie; the honest sentence is that the clock is not running at all. This is the case the
    /// owner was NOT in and would have needed most.</summary>
    [Fact]
    public void THE_DOSSIER_SaysWhenTheHavenClockIsNotRunning()
    {
        Pages.Map map = AChaseInProgress(hunterRange: 1e9, hiddenForSeconds: null);

        Assert.Contains("NOT running", (string)Field(Terms(Dossier(map)), "Hiding")!, StringComparison.Ordinal);
    }

    /// <summary>TWO DESKS, ONE STORY. The war room is the other screen a captain stares at while a collector
    /// closes, and a second copy of these sentences composed over there would be the same bug in a new file.
    /// Both projections are taken off one page state and compared word for word.</summary>
    [Fact]
    public void THE_WAR_ROOM_SaysExactlyWhatTheDossierSays()
    {
        double hiddenFor = 0.3 * EncounterRule.BreakOffHiddenDays * 86400;
        Pages.Map map = AChaseInProgress(hunterRange: 1e9, hiddenForSeconds: hiddenFor);

        object terms = Terms(Dossier(map));
        var atTheWarRoom = (IReadOnlyList<Pages.Stations.WarRoom.HunterContact>)
            typeof(Pages.Map).GetMethod("WarRoomHunters", Hidden)!.Invoke(map, null)!;

        Pages.Stations.WarRoom.HunterContact her = Assert.Single(atTheWarRoom);
        Assert.Equal((string)Field(terms, "Warrant")!, her.Warrant);
        Assert.Equal((string)Field(terms, "Hiding")!, her.HidingTerm);
        Assert.Equal((string)Field(terms, "Nerve")!, her.NerveTerm);

        // …and they are saying something, not agreeing on emptiness — the fifth bug class, in one line.
        Assert.NotEqual("", her.HidingTerm);
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    private static object Dossier(Pages.Map map, string id = HunterId) =>
        typeof(Pages.Map).GetMethod("DossierFor", Hidden)!.Invoke(map, [id])
        ?? throw new InvalidOperationException($"the page built no dossier for {id} at all.");

    private static object Terms(object dossier) =>
        Field(dossier, "Terms") ?? throw new InvalidOperationException("the hunter's card carries no terms.");

    /// <summary>A collector at a chosen range, hired over a named hull, with the haven clock either running
    /// for a chosen span or not running at all.</summary>
    private static Pages.Map AChaseInProgress(double hunterRange, double? hiddenForSeconds = null)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);

        const double now = 1_000_000;
        Set(map, "SimTime", now);
        Set(map, "_ship", new ShipState(Vector2d.Zero, Vector2d.Zero, now));
        Set(map, "_heat", EncounterRule.RaiseHeat(HeatState.None, 1, now));
        Set(map, "_hiddenAtHavenSinceSimTime", hiddenForSeconds is { } span ? now - span : double.NaN);

        var hunters = (List<HunterState>)Get(map, "_hunters")!;
        hunters.Add(EncounterRule.SpawnHunter(
            HunterId, Callsign, "earth", new Vector2d(hunterRange, 0), Vector2d.Zero, now, warrant: RobbedHull));

        return map;
    }

    private static object? Field(object o, string name) =>
        o.GetType().GetProperty(name)!.GetValue(o);

    private static object? Get(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);
}
