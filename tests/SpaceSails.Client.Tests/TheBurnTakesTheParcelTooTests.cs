using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using SpaceSails.Client.Rendering;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1062 slice 2c · <b>THE BURN TAKES THE PARCEL ROW TOO.</b>
///
/// <para>#1210 burned a port's two quiet verbs — the fence's key and the bar favour — for one visit when a
/// grey coat was on the floor behind the captain while he did the deal. #1209 landed a THIRD quiet verb at
/// the same desk two days later (the unlisted parcel's row) and deliberately left it out of the burn,
/// saying so in as many words: <i>"Whether a captain who was followed should also find no parcel is a
/// gameplay-feel call across two lanes that neither crew was briefed on … One clause in
/// <c>ParcelOnOffer</c> closes it the day the owner wants it closed."</i></para>
///
/// <para><b>Ruled, and this is the clause.</b> The burn silences EVERY quiet verb at that port, because the
/// parcel is the quietest deal of the three and a man who watched you at the desk watched the desk. What
/// the captain meets is the row NOT BEING DRAWN — never greyed, never refusing, never explained — for the
/// same one visit the other two are absent, and the burn's telling when he comes back is still exactly the
/// one line #1210 ships.</para>
///
/// <para><b>What is proved here, and what is proved by the file next door.</b> #1210's own suite
/// (<see cref="TheTailBehindYouTests"/>) owns the burn: that it is written, that it is silent at the
/// moment, that it is told and spent on the visit after. This file owns the seam the inspector was left —
/// that the third row answers to that same one predicate, that it comes back, that the anti-vacuity half
/// holds (a captain who shook him first does the same deal and keeps his row), and that the two registers
/// living in <c>_roomsTurnedOver</c> still do not collide on one save.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBurnTakesTheParcelTooTests
{
    private const string Berth = ObservationWalk.HavenId;

    // ── 1 · THE ROW IS ABSENT AT A PLACE SOMEBODY WALKED FIRST ──────────────────────────────────────────

    /// <summary>
    /// <b>ONE QUIET DEAL WITH A MAN ON THE FLOOR BEHIND YOU, AND THE DESK HAS NO PARCEL EITHER.</b>
    ///
    /// <para>Driven through the shipping press — <c>BuyTheKeyFromTheFence</c>, coin out and key in at a
    /// berth the game clamped onto for real — and not through the burn writer, so what is asked is the thing
    /// a player does rather than the thing the code does.</para>
    ///
    /// <para><b>THE WORLD CAN TELL PASS FROM FAIL, and this is the whole design of the guard:</b> the row is
    /// asked BEFORE the deal and it is on offer; the identical page with the identical press at the
    /// identical berth on the identical watch, differing only in whether anybody was behind the captain,
    /// keeps its row. So the absence cannot be the key purchase, the satchel, the watch, the clamp or the
    /// desk — it is the man.</para>
    ///
    /// <para><b>RED</b> by dropping <c>&amp;&amp; TheDesksPort() is { } port &amp;&amp;
    /// !ThisPlaceWasWalkedFirst(port)</c> from <c>ParcelOnOffer</c>: <i>a captain who was followed to the
    /// desk is still handed a box</i>.</para>
    /// </summary>
    [Fact]
    public void AQuietDealDoneWithSomebodyWatchingTakesTheParcelRowWithIt()
    {
        Pages.Map watched = ADeskAshoreAt(Berth, tailed: true);
        Pages.Map alone = ADeskAshoreAt(Berth, tailed: false);

        Assert.NotNull(TheCoat(watched));
        Assert.Null(TheCoat(alone));

        // Before the deal, both desks have the same box on the same row.
        Assert.True((bool)Invoke(watched, "ParcelOnOffer")!);
        Assert.True((bool)Invoke(alone, "ParcelOnOffer")!);

        BuyTheKey(watched);
        BuyTheKey(alone);

        // The man is the only difference, and the man is what costs the row.
        Assert.True((bool)Invoke(watched, "ThisPlaceWasWalkedFirst", Berth)!);
        Assert.False((bool)Invoke(alone, "ThisPlaceWasWalkedFirst", Berth)!);

        Assert.False((bool)Invoke(watched, "ParcelOnOffer")!,
            "a captain who was followed to the desk was still handed a box.");
        Assert.True((bool)Invoke(alone, "ParcelOnOffer")!,
            "the parcel row went away for a captain nobody was watching — the burn is not what took it.");

        // …and the row is the ONLY thing that happened. Not one word, on this frame or a hundred more.
        Tick(watched, 100);
        Assert.Null(PulseSaying(watched));
        Assert.Null(Read(watched, "_storyCard"));
        Assert.Empty((IEnumerable<FieldNote>)Read(watched, "_fieldNotes")!);
    }

    // ── 2 · …AND IT IS BACK THE NEXT VISIT, ON THE LINE #1210 ALREADY SHIPS ─────────────────────────────

    /// <summary>
    /// <b>ONE VISIT, AND NO NEW PROSE.</b> Cast off, come back, and the place says the one thing it has
    /// always said — <i>"Tidy, in the way a place is after somebody has been through it first."</i> — and in
    /// the same breath the parcel row is on the board again.
    ///
    /// <para>The burn is spent by being told, so the row coming back and the sentence being said are one
    /// event. This guard exists to pin that the coupling shipped here did not buy itself a second telling: a
    /// captain hears the ONE line, the field book takes the ONE note, and nothing anywhere says that a
    /// parcel was among the things it cost him.</para>
    ///
    /// <para>Coming back is done the way the game does it, through the one place that knows the berth has
    /// changed (<c>ForgetTheBarsFeet</c>), never by clearing the visit's own set.</para>
    ///
    /// <para><b>RED</b> by spending the tag anywhere but in the telling (removing the
    /// <c>_roomsTurnedOver.Remove</c> from <c>TheBurnIsToldHere</c>): <i>the row never comes back</i>.</para>
    /// </summary>
    [Fact]
    public void AndTheRowIsBackOnTheVisitTheBurnIsToldOn()
    {
        Pages.Map map = ADeskAshoreAt(Berth, tailed: true);
        BuyTheKey(map);
        Assert.False((bool)Invoke(map, "ParcelOnOffer")!);

        // Cast off and come back. The room forgets; the register does not.
        Invoke(map, "ForgetTheBarsFeet", (string?)null);
        Invoke(map, "ForgetTheBarsFeet", Berth);
        Tick(map, 1);

        Assert.Equal(TheTailBehindYou.TheBurnLine, PulseSaying(map));
        FieldNote note = Assert.Single((IReadOnlyList<FieldNote>)Read(map, "_fieldNotes")!);
        Assert.Equal(TheTailBehindYou.BurnNote((string)Invoke(map, "DockedStationName")!), note.Text);

        // Spent, and the desk deals again — the same event, with nothing extra said about the box.
        Assert.False((bool)Invoke(map, "ThisPlaceWasWalkedFirst", Berth)!);
        Assert.True((bool)Invoke(map, "ParcelOnOffer")!,
            "the place told the captain it had been walked first and STILL had nothing for him.");
    }

    // ── 3 · THE TWO REGISTERS STILL DO NOT COLLIDE ──────────────────────────────────────────────────────

    /// <summary>
    /// <b>ONE SAVE, TWO QUIETS, AND EACH IS SPENT BY ITS OWN CLOCK.</b> #1209's headline claim was that the
    /// burn's PORT tag (<c>tail-burn:{portId}</c>) and the confiscation's WATCH tag
    /// (<c>parcel:none@{watch}</c>) are two independent strings in one durable set. Wiring the burn to the
    /// parcel row is exactly the change that could have quietly made that untrue — by folding one register
    /// into the other, or by letting either reader answer for both.
    /// </para>
    ///
    /// <para>So: a captain who was followed AND had a box taken off him carries both tags. Telling the burn
    /// spends the burn and NOT the confiscation — the row stays absent, for the reason it was already
    /// absent — and the row comes back only when the confiscation's own watches have run out.</para>
    ///
    /// <para><b>RED</b> by making <c>TheDeskHasNothingForThisHull</c> read the burn tag as well (one
    /// register answering for both): <i>the telling gives back a row a man with a form took</i>.</para>
    /// </summary>
    [Fact]
    public void BothQuietsRideOneSaveAndNeitherSpendsTheOther()
    {
        Pages.Map map = ADeskAshoreAt(Berth, tailed: true);
        var parcel = (Satchel.Item)Invoke(map, "TheParcelOnThisDesk")!;

        BuyTheKey(map);                                            // …followed to the desk
        Invoke(map, "TheDeskHasNothingForAWhile", parcel.Id);      // …and a man with a form took the box

        var register = (HashSet<string>)Read(map, "_roomsTurnedOver")!;
        double at = (double)Read(map, "SimTime")!;
        Assert.Contains(TheTailBehindYou.BurnTag(Berth), register);
        Assert.Contains(ParcelDrop.NothingForThisHullOn(PatronRota.WatchIndex(at)), register);
        Assert.True((bool)Invoke(map, "ThisPlaceWasWalkedFirst", Berth)!);
        Assert.True((bool)Invoke(map, "TheDeskHasNothingForThisHull")!);
        Assert.False((bool)Invoke(map, "ParcelOnOffer")!);

        // The burn is told and spent. The confiscation is not — it answers to a clock of its own.
        Invoke(map, "ForgetTheBarsFeet", (string?)null);
        Invoke(map, "ForgetTheBarsFeet", Berth);
        Tick(map, 1);
        Assert.Equal(TheTailBehindYou.TheBurnLine, PulseSaying(map));
        Assert.False((bool)Invoke(map, "ThisPlaceWasWalkedFirst", Berth)!);
        Assert.True((bool)Invoke(map, "TheDeskHasNothingForThisHull")!);
        Assert.False((bool)Invoke(map, "ParcelOnOffer")!,
            "the burn's telling handed back a row that a man with a form had taken — one register is "
            + "answering for both.");

        // …and when the confiscation's own watches have run out, the row is there for the first time.
        Set(map, "SimTime", at + ((ParcelDrop.MostQuietWatches + 1) * PatronRota.WatchSeconds));
        Assert.False((bool)Invoke(map, "TheDeskHasNothingForThisHull")!);
        Assert.True((bool)Invoke(map, "ParcelOnOffer")!);
    }

    // ── 4 · ONE BURN PREDICATE, THREE VERBS ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE AUDIT, MADE MECHANICAL.</b> The burn's tag is minted in one place, read through one predicate,
    /// and the three quiet verbs at a port reach it by asking that predicate — never by a copy of the tag, a
    /// second predicate, or a string of their own.
    ///
    /// <para>And one thing the parcel row must NOT do: reach the burn through
    /// <c>ThisPortHasAlreadyDealtAKey</c>. That method is also the KEY's one-per-watch strike-off, so a
    /// parcel row that read it would go away because the captain bought a key — the two-meters-that-must-
    /// agree bug wearing the burn's coat.</para>
    /// </summary>
    [Fact]
    public void ThereIsOneBurnPredicateAndTheParcelRowDoesNotRideTheKeysRegister()
    {
        string tail = Code(Source("src", "SpaceSails.Client", "Pages", "Map.TailBehindYou.cs"));
        string sources = Code(Source("src", "SpaceSails.Client", "Pages", "Map.BlackOpsKey.Sources.cs"));
        string parcel = Code(Source("src", "SpaceSails.Client", "Pages", "Map.UnlistedParcel.cs"));
        string client = string.Join("\n", Directory
            .EnumerateFiles(
                Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client"), "*.cs",
                SearchOption.AllDirectories)
            .Select(f => Code(File.ReadAllText(f))));

        // ONE predicate, and the tag is touched in exactly the three places a tag may be touched: the burn
        // writes it, this predicate reads it, the telling spends it. All three are in #1210's own file.
        Assert.Equal(1, Count(tail, "private bool ThisPlaceWasWalkedFirst("));
        Assert.Equal(3, Count(client, "TheTailBehindYou.BurnTag("));
        Assert.Equal(3, Count(tail, "TheTailBehindYou.BurnTag("));
        Assert.Equal(1, Count(tail, "_roomsTurnedOver.Add(TheTailBehindYou.BurnTag(portId))"));
        Assert.Equal(1, Count(tail, "_roomsTurnedOver.Contains(TheTailBehindYou.BurnTag(portId))"));
        Assert.Equal(1, Count(tail, "_roomsTurnedOver.Remove(TheTailBehindYou.BurnTag(portId))"));

        // …and the three quiet verbs reach it by asking, each exactly once.
        Assert.Equal(1, Count(sources, "|| ThisPlaceWasWalkedFirst(portId)"));
        Assert.Equal(1, Count(parcel, "&& TheDesksPort() is { } port && !ThisPlaceWasWalkedFirst(port)"));
        // …and NOBODY ELSE asks it: one declaration, the telling's own read, and the two quiet rows.
        Assert.Equal(4, Count(client, "ThisPlaceWasWalkedFirst("));
        Assert.Equal(1, Count(tail, "!ThisPlaceWasWalkedFirst(portId)"));

        // The parcel row never touches the KEY's register — not the strike-off, not the tag.
        Assert.Equal(0, Count(parcel, "ThisPortHasAlreadyDealtAKey"));
        Assert.Equal(0, Count(parcel, "BlackOpsKey.ThePortHasDealtOne"));

        // …and all three rows name the desk's port with ONE expression, so they cannot come to three views
        // of WHICH place somebody walked first.
        Assert.Equal(1, Count(sources, "private string? TheDesksPort()"));
        Assert.Equal(0, Count(client, "TheFencesPort"));
    }

    // ── THE WORLD ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A ship tied up at a berth the way the game ties one up (<c>ClampOntoHaven</c> — the one door the ⚓
    /// press, the honest auto-dock and the <c>?dock=</c> boot all go through, #1217's own law), with the
    /// captain then standing ashore in that berth's bar the way <c>?ashore=1</c> stands him there.
    ///
    /// <para>Both halves are needed and neither is faked: the DESK answers to the clamp, and the man behind
    /// the captain answers to the room. Nothing here writes a dock field, a burn tag or a walker.</para>
    /// </summary>
    private static Pages.Map ADeskAshoreAt(string berthId, bool tailed)
    {
        Pages.Map map = Boot("the-burn-and-the-parcel");
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody dock = sky.Bodies.First(b => b.Id == berthId);

        Invoke(map, "ClampOntoHaven", dock, sky.Position(berthId, (double)Read(map, "SimTime")!), null);
        Frame(map);
        Assert.Equal(berthId, (string?)Read(map, "_dockedHavenId"));
        Assert.True((bool)Invoke(map, "DarkWebCanTrade")!,
            $"the desk is shut at {berthId}, so this bench proves nothing about its rows.");

        Set(map, "_tailedCheat", (bool?)tailed);
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!,
            $"{berthId} has no walkable interior — nobody can be standing behind anybody here.");

        // One step in off the threshold, so the room counts him as being IN it, and then the frames the man
        // is dealt on. He is dealt on the concourse side of the doorway and walks in after the captain.
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        Tick(map, 1);

        // The berthing's own ⚓ line is on the glass from the clamp above. Clear it, so a guard that asks
        // whether the BURN said anything is asking about the burn and not about docking.
        Set(map, "_pulse", default(PulseSlot));
        Set(map, "_credits", 1_000_000);
        return map;
    }

    /// <summary>The quiet deal, done the way a player does it: the fence's own row at the desk, coin out and
    /// key in. It runs through <c>ThisPortHasNowDealtAKey</c>, which is where #1210 hung the burn.</summary>
    private static void BuyTheKey(Pages.Map map)
    {
        int before = (int)Read(map, "_credits")!;
        Assert.NotNull(Invoke(map, "TheFencesKeyPrice"));
        Invoke(map, "BuyTheKeyFromTheFence");
        Assert.True((int)Read(map, "_credits")! < before, "no coin moved — the key was never bought.");
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────────

    private static string Source(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), .. parts]));

    /// <summary>The CODE, with the design record taken out of it — these files are half comment by weight
    /// and every name a guard counts is discussed in prose beside the line that uses it.</summary>
    private static string Code(string source) =>
        Regex.Replace(Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline), "//[^\n]*", " ");

    private static int Count(string haystack, string needle)
    {
        int n = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal);
             i >= 0;
             i = haystack.IndexOf(needle, i + 1, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }

    /// <summary>Put the captain somewhere, and tell the motion rule he has been there a while — so a
    /// placement is never read as a sprint (#436's own <c>TeleportSpeedDu</c> clause).</summary>
    private static void StandCaptainAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
        Set(map, "_lookPrevAvatarX", x);
        Set(map, "_lookPrevAvatarY", y);
    }

    /// <summary>The room's own frames — BOTH clocks, because this half is counted in real seconds off the
    /// frame stamp while the room's hours are sim seconds. Deliberately not the whole page frame: a berth
    /// scene re-clamping every tick is a world this guard did not ask for.</summary>
    private static void Tick(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Read(map, "SimTime")! + dt);
            Set(map, "_lastTimestampMs", (double?)(((double?)Read(map, "_lastTimestampMs") ?? 0) + (dt * 1000)));
            Invoke(map, "AdvanceBarWalkers", dt);
        }
    }

    private static object? TheCoat(Pages.Map map)
    {
        foreach (object who in (IList)Read(map, "_barAfoot")!)
        {
            if (Get(who, "For")!.ToString() is "BehindYou" or "AskingTheWrongFloor")
            {
                return who;
            }
        }

        return null;
    }

    private static string? PulseSaying(Pages.Map map) => Get(Read(map, "_pulse")!, "Message") as string;
}
