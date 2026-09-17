using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1062 slice 2 · <b>THE MAN BEHIND THE CAPTAIN</b>, driven at a real berth on a real deck.
///
/// <para>Owner, 2026-09-01: <i>"… or trying to lose a tail our selves :-D"</i>. Six claims, each with its own
/// revert:</para>
///
/// <list type="number">
/// <item>nobody is behind a captain nobody has written anything about — and the folder is what changes it;</item>
/// <item>he comes in after you, keeps his band, and ORDERS NOTHING;</item>
/// <item><b>until he is noticed, nothing in this game says one word</b> — no pulse, no card, no book;</item>
/// <item>the chair that faces the door pays off, and only while you are actually sitting in it;</item>
/// <item>the same coat through two doorways pays off — and a LOCKED leaf is not a doorway;</item>
/// <item>breaking his line for long enough loses him, he leaves, the line plays and the book files it under
/// the PLACE — and a captain who never noticed him is told nothing at all.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheTailBehindYouTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;
    private const string ThreadId = "b47c2f1a08d94e6cb1f37a55d0e29c31";
    private const string Berth = ObservationWalk.HavenId;

    // ── 1 · WHO PUTS HIM THERE ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>NOBODY IS BEHIND A CAPTAIN NOBODY HAS WRITTEN ANYTHING ABOUT</b> — and #715's folder is the
    /// one thing that changes it. The same page, the same berth, the same watch, run twice: once with a cold
    /// book and once with the outfit at the band where it wants a face.
    ///
    /// <para>This is the anti-vacuity guard for the whole suite. Every test below forces him on with the dev
    /// row; if the WORLD could never produce him, all of them would be testing a cheat.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>TheCoatIsBehindYou</c> written to ignore the folder
    /// (<c>_tailedCheat ?? true</c>) — <i>a man behind every captain in the game, at every berth, for
    /// ever</i>.</para>
    /// </summary>
    [Fact]
    public void TheFolderIsWhatPutsHimThereAndAColdBookPutsNobody()
    {
        Pages.Map cold = AshoreAt(Berth);
        StandCaptainAt(cold, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(cold, 40);
        Assert.Null(TheCoat(cold));

        // …and the same evening with the outfit's folder open at the band the game itself calls
        // "the gate wants a face".
        Pages.Map warm = AshoreAt(Berth);
        BankHeatAt(warm, Berth, IllegalHeat.TheGateWantsAFaceAt);
        StandCaptainAt(warm, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(warm, 40);

        Assert.True(
            IllegalHeat.HeatAtSite(Contacts(warm), Berth) >= IllegalHeat.TheGateWantsAFaceAt,
            "the folder did not actually open — this guard would be proving nothing.");
        Assert.NotNull(TheCoat(warm));
    }

    // ── 2 · WHAT HE DOES ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>HE COMES IN AFTER YOU, KEEPS HIS BAND, AND ORDERS NOTHING.</b>
    ///
    /// <para>Three claims about one body. He is not on the floor while the captain is still in the concourse
    /// (the whole shape of the beat is that he follows you in). Once the captain is in the room he is, and he
    /// settles inside the band Core publishes. And he never goes to the counter — the one fixture in this
    /// room where service happens, and the one spot the canon line says he has not been to.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>InTheBar</c> clause deleted from <c>AdvanceTheCoat</c> —
    /// <i>the man is already standing in the bar on the frame the captain clamps on, which is not a tail, it
    /// is a fixture</i>.</para>
    /// </summary>
    [Fact]
    public void HeComesInAfterYouKeepsHisBandAndNeverGoesToTheCounter()
    {
        Pages.Map map = Tailed(Berth);

        // Still aboard, in the airlock corridor: the room has nobody in it on his account.
        StandCaptainAt(map, 2.5, 6);
        RunFrames(map, 40);
        Assert.Null(TheCoat(map));

        // …and then the captain walks in.
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 1);
        object coat = TheCoat(map) ?? throw new InvalidOperationException("nobody followed the captain in.");
        Assert.Equal("BehindYou", Get(coat, "For")!.ToString());

        // He settles, and where he settles is inside the band Core publishes.
        for (int i = 0; i < 600 && Afoot(TheCoat(map)!); i++)
        {
            RunFrames(map, 1);
        }

        coat = TheCoat(map)!;
        Assert.True(TheTailBehindYou.HoldsHisBand(RangeToCoat(map, coat)),
            "he walked to a spot outside the band he is supposed to keep.");

        // ORDERS NOTHING. The room publishes exactly one place where service happens; he is not at it, and
        // the code never so much as asks the room where it is.
        HavenInterior.BarFloor bar = HavenInterior.BarBand(Berth)!.Value;
        foreach (DeckReachability.Point counter in bar.Fixtures)
        {
            double dx = counter.X - CoatX(coat), dy = counter.Y - CoatY(coat);
            Assert.True(Math.Sqrt((dx * dx) + (dy * dy)) > DeckPlan.InteractRadius,
                "he is standing at the counter — the one thing the canon says he has not done.");
        }
    }

    // ── 3 · SILENCE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>UNTIL HE IS NOTICED, NOTHING IN THIS GAME SAYS ONE WORD.</b> The captain stands in the room
    /// with him for a thousand frames, on his feet, and the HUD, the card slot and the book are all exactly
    /// as empty as they were before he walked in.
    ///
    /// <para>This is #1062's inference horror said as a guard. The one thing that gives him away is the
    /// figure on the floor, which is where a gumshoe's evidence is supposed to be.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>YouHaveNoticedHim</c> called unconditionally from
    /// <c>AdvanceTheCoat</c> the moment he is dealt — <i>the game announcing the tail it exists to make the
    /// player find</i>.</para>
    /// </summary>
    [Fact]
    public void UntilYouNoticeHimNothingIsSaidAtAll()
    {
        Pages.Map map = Tailed(Berth);
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);

        RunFrames(map, 1000);

        Assert.NotNull(TheCoat(map));
        Assert.False((bool)Field(map, "_coatSeen")!);
        Assert.Null(PulseSaying(map));
        Assert.Null(Field(map, "_storyCard"));
        Assert.Empty((IEnumerable<FieldNote>)Field(map, "_fieldNotes")!);
    }

    // ── 4 · THE CHAIR THAT FACES THE DOOR ───────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>THE CHAIR PAYS OFF — AND ONLY WHILE YOU ARE IN IT.</b> The captain takes a top the ONE way
    /// this game opens a sitting, waits out the exposure, and the authored line plays once.
    ///
    /// <para>The clause that makes it craft rather than a timer is asserted first: the same man, the same
    /// range, the same clear line, for twice as long, with the captain ON HIS FEET — and nothing happens. The
    /// sit is the whole cost.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>CaptainIsSeated</c> clause dropped from
    /// <c>watchingTheDoor</c> — <i>the line fired at a captain who had simply stood in the room for nine
    /// seconds, which is every captain who has ever docked</i>.</para>
    /// </summary>
    [Fact]
    public void TheChairThatFacesTheDoorPaysOffAndStandingUpBuysNothing()
    {
        Pages.Map map = Tailed(Berth);
        HavenInterior.BarFloor bar = HavenInterior.BarBand(Berth)!.Value;

        // On his feet, in the room, for twice the exposure: nothing.
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 1);
        Assert.NotNull(TheCoat(map));
        RunFrames(map, (int)(2 * TheTailBehindYou.NoticeSeconds / 0.1));
        Assert.False((bool)Field(map, "_coatSeen")!);

        // …and then he sits down, at a top that can see the door.
        Assert.True(SitAtATopThatSeesTheDoor(map, bar), "no top in this bar had a line to its own doorway.");
        Assert.True((bool)Invoke(map, "get_CaptainIsSeated")!);

        for (int i = 0; i < 1200 && !(bool)Field(map, "_coatSeen")!; i++)
        {
            RunFrames(map, 1);
        }

        Assert.True((bool)Field(map, "_coatSeen")!,
            "a hundred and twenty seconds in a chair with the door in front of it and the man never resolved.");
        Assert.Equal(TheTailBehindYou.FromThisChairLine, PulseSaying(map));

        // ONCE. The HUD is wiped and a hundred frames say nothing back into it.
        Set(map, "_pulse", default(PulseSlot));
        RunFrames(map, 100);
        Assert.Null(PulseSaying(map));
    }

    // ── 5 · THE SAME COAT THROUGH TWO DOORS ─────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>A DOORWAY IS A WAY THROUGH A ROOM, AND A LOCKED LEAF IS NOT ONE.</b> The ledger the
    /// two-door tell is counted on, asked of the built deck at the berth that has three walkable doorways
    /// and two leaves that never open.
    ///
    /// <para>Anti-vacuity is the second half: the bar's CELLAR and STOREROOM leaves are real doors on the
    /// plan, at real coordinates, and a body standing in one of them must count for nothing — otherwise a
    /// man leaning on the cellar door is half of a tell he never earned.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>Locked</c> filter deleted from
    /// <c>TheDoorwayHeIsIn</c> — <i>the cellar counted, and a man who never moved was two doors
    /// running</i>.</para>
    /// </summary>
    [Fact]
    public void ALockedLeafIsNotADoorwayAndTheTellCountsDistinctOnes()
    {
        Pages.Map map = Tailed(Berth);
        HavenInterior.BarFloor bar = HavenInterior.BarBand(Berth)!.Value;

        // The bar's own north auto-door — the one a person may walk through — is found.
        int? atTheBarDoor = (int?)Invoke(
            map, "TheDoorwayHeIsIn", HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y);
        Assert.NotNull(atTheBarDoor);

        // …and the two leaves that never open are not doorways, however squarely a body stands in them.
        Assert.NotEmpty(bar.Doors);
        foreach (UndergroundComplex.LockedDoor leaf in bar.Doors)
        {
            int? counted = (int?)Invoke(
                map, "TheDoorwayHeIsIn", leaf.X1, (leaf.Y1 + leaf.Y2) / 2);
            Assert.True(counted != atTheBarDoor,
                "a leaf the captain's own TRY is refused at was counted as the bar's own doorway.");
        }

        // The deck has more than one walkable doorway at all, which is what makes the move possible.
        DeckPlan deck = (DeckPlan)Field(map, "_deckPlan")!;
        Assert.True(deck.Doors.Count(d => !d.Locked) >= TheTailBehindYou.DoorsThatMakeTheTell);
    }

    /// <summary>
    /// #1062 · <b>THE TELL ITSELF.</b> He is seen in the bar's own doorway on the way in; the captain then
    /// walks out to the blind end of the observation walk, where the only place in the room with a line to
    /// him is the walk's own mouth — so the second doorway is earned by geometry rather than arranged, and
    /// the authored line plays.
    ///
    /// <para>#1199's tube is doing real work here: a room with one way in is a room where a man keeping a
    /// band on you has exactly one place to stand, which is the doorway. Doubling back into it is the move.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>TwoDoorsRunning</c> handed <c>_coatDoors.Count</c> replaced
    /// by the constant <c>2</c> — the line fired on the first doorway he was ever seen in, which is the one
    /// he walked in through.</para>
    /// </summary>
    [Fact]
    public void TheSameCoatThroughTwoDoorwaysIsTheTell()
    {
        Pages.Map map = Tailed(Berth);
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 1);
        Assert.NotNull(TheCoat(map));

        // He came in through the bar's own doorway, and the ledger has it.
        RunFrames(map, 10);
        var seen = (ICollection<int>)Field(map, "_coatDoors")!;
        Assert.NotEmpty(seen);
        Assert.False((bool)Field(map, "_coatSeen")!, "one doorway is not a tell.");

        // …and now out to the blind end of the walk, where his band leaves him one place to stand.
        DeckReachability.Point rail = HavenInterior.TheRailAt(Berth)!.Value;
        StandCaptainAt(map, rail.X, rail.Y);

        for (int i = 0; i < 2000 && !(bool)Field(map, "_coatSeen")!; i++)
        {
            RunFrames(map, 1);
        }

        Assert.True((bool)Field(map, "_coatSeen")!,
            "the captain doubled back into a one-way room and the coat never came to the mouth of it.");
        Assert.True(TheTailBehindYou.TwoDoorsRunning(seen.Count));
        Assert.Equal(TheTailBehindYou.TwoDoorsLine, PulseSaying(map));
    }

    // ── 6 · LOSING HIM ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>BREAK HIS LINE FOR LONG ENOUGH AND HE GOES</b> — the authored line plays, the book files it
    /// under the PLACE, and he walks off the floor.
    ///
    /// <para>What breaks the line here is the geography the audit left this feature: the captain goes back
    /// down his own gangway, which is the one part of a berth a man keeping station on a hull does not
    /// follow him into.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>HeIsLost</c> written as
    /// <c>outOfHisSightSeconds &gt; 0</c> — <i>he gave up the first time the captain stepped behind a
    /// pillar, and the whole exchange cost nothing</i>. (Caught by the two-frame assertion below rather than
    /// by the end state, which is identical either way.)</para>
    /// </summary>
    [Fact]
    public void BreakingHisLineLosesHimAndTheBookFilesItUnderThePlace()
    {
        Pages.Map map = Tailed(Berth);
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 1);
        Assert.NotNull(TheCoat(map));

        // He has to have been NOTICED for a word of this to be said, so the chair is taken first.
        Assert.True(SitAtATopThatSeesTheDoor(map, HavenInterior.BarBand(Berth)!.Value));
        for (int i = 0; i < 1200 && !(bool)Field(map, "_coatSeen")!; i++)
        {
            RunFrames(map, 1);
        }
        Assert.True((bool)Field(map, "_coatSeen")!);
        Set(map, "_pulse", default(PulseSlot));

        // Down the gangway — out of the room, out of his line, and he will not follow.
        Invoke(map, "StandUpFromTable");
        StandCaptainAt(map, 2.5, 6);

        // A frame or two is NOT losing him: the exchange costs what the finding cost.
        RunFrames(map, 2);
        Assert.False((bool)Field(map, "_coatLost")!);
        Assert.Null(PulseSaying(map));

        for (int i = 0; i < 1200 && !(bool)Field(map, "_coatLost")!; i++)
        {
            RunFrames(map, 1);
        }

        Assert.True((bool)Field(map, "_coatLost")!, "the corridor behind the captain never became a corridor.");
        Assert.Equal(TheTailBehindYou.LostLine, PulseSaying(map));

        var book = (IReadOnlyList<FieldNote>)Field(map, "_fieldNotes")!;
        FieldNote note = Assert.Single(book);
        string place = (string)Invoke(map, "DockedStationName")!;
        Assert.Equal(TheTailBehindYou.NoteLine(place), note.Text);
        Assert.Equal(TheTailBehindYou.Subjects(place), note.Subjects);
        Assert.Equal(TheTailBehindYou.Glyph, note.Glyph);

        // …and the heading over it is a PLACE and never a face.
        CaseSubjects.Subject filed = Assert.Single(CaseSubjects.On(in note));
        Assert.Equal(CaseSubjects.Kind.Place, filed.Of);

        // He walks off the floor, and once he is gone he does not come back this visit.
        for (int i = 0; i < 2000 && TheCoat(map) is not null; i++)
        {
            RunFrames(map, 1);
        }
        Assert.Null(TheCoat(map));

        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 300);
        Assert.Null(TheCoat(map));
    }

    /// <summary>
    /// #1062 · <b>AND A CAPTAIN WHO NEVER NOTICED HIM IS TOLD NOTHING, EVER.</b> The same losing, from a
    /// captain who never sat down: the man stands about, gives up and leaves, and no pulse, card or book
    /// entry in this game ever mentions that anybody was there.
    ///
    /// <para>This is the guard that stops the feature explaining its own best beat. It is separate from the
    /// silence guard above because it covers the ONE place a line is actually raised.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>_coatSeen</c> clause dropped from
    /// <c>HeGoesAndAsksTheWrongFloor</c> — <i>the game told the captain it had just lost a tail he never knew
    /// he had</i>.</para>
    /// </summary>
    [Fact]
    public void ACaptainWhoNeverNoticedHimIsToldNothing()
    {
        Pages.Map map = Tailed(Berth);
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 1);
        Assert.NotNull(TheCoat(map));

        StandCaptainAt(map, 2.5, 6);
        for (int i = 0; i < 1200 && !(bool)Field(map, "_coatLost")!; i++)
        {
            RunFrames(map, 1);
        }

        Assert.True((bool)Field(map, "_coatLost")!);
        Assert.False((bool)Field(map, "_coatSeen")!);
        Assert.Null(PulseSaying(map));
        Assert.Null(Field(map, "_storyCard"));
        Assert.Empty((IEnumerable<FieldNote>)Field(map, "_fieldNotes")!);
    }

    // ── PLUMBING ─────────────────────────────────────────────────────────────────────────────────────────

    private static Pages.Map AshoreAt(string berth, long watch = 0)
    {
        var map = new Pages.Map();
        Set(map, "SimTime", watch * PatronRota.WatchSeconds);
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_dockedHavenId", berth);
        Set(map, "_deckMode", true);
        Set(map, "_ashore", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", (bool?)false);
        Invoke(map, "SetDeckForDock", berth);
        Invoke(map, "StandAtTheBarThreshold");
        return map;
    }

    /// <summary>A page with the dev row on — <c>?tailed=1</c>, which forces WHETHER and never WHO or WHAT.
    /// The world's own route to the same state is guarded separately, above.</summary>
    private static Pages.Map Tailed(string berth)
    {
        Pages.Map map = AshoreAt(berth);
        Set(map, "_tailedCheat", (bool?)true);
        return map;
    }

    /// <summary>#715 · Open the outfit's folder the way the game opens it — through
    /// <see cref="IllegalHeat.Bank"/>, the one banking call, with a charge owed to whoever runs this
    /// berth.</summary>
    private static void BankHeatAt(Pages.Map map, string berth, int points) =>
        IllegalHeat.Bank(
            Contacts(map), new UndergroundComplex.HeatCharge(SiteOperator.Of(berth).Id, points),
            (double)Field(map, "SimTime")!);

    private static ContactLedger Contacts(Pages.Map map) => (ContactLedger)Field(map, "_contacts")!;

    /// <summary>Take a bar top that has a line to the room's own doorway, through the one [E] a player
    /// presses — <c>Seating.TryTakeBarTop</c>. A test that assembled its own sitting would be demonstrating
    /// a seat that does not ship.</summary>
    private static bool SitAtATopThatSeesTheDoor(Pages.Map map, HavenInterior.BarFloor bar)
    {
        var deck = (DeckPlan)Field(map, "_deckPlan")!;
        (double doorX, double doorY, _) = HavenInterior.BarThreshold;

        foreach (DeckReachability.Point top in bar.Tops)
        {
            if (HavenInterior.BesideATop(top, DeckPlan.AvatarRadius, deck.CollisionField) is not { } chair
                || !TheTailBehindYou.ThisChairSeesTheDoor(chair.X, chair.Y, doorX, doorY, deck.CollisionField))
            {
                continue;
            }

            StandCaptainAt(map, top.X, top.Y);
            if ((bool)Invoke(map, "TryTakeBarTop")!)
            {
                Set(map, "_pulse", default(PulseSlot));
                return true;
            }
        }

        return false;
    }

    private static IList BarAfoot(Pages.Map map) => (IList)Field(map, "_barAfoot")!;

    private static object? TheCoat(Pages.Map map)
    {
        foreach (object who in BarAfoot(map))
        {
            string errand = Get(who, "For")!.ToString()!;
            if (errand is "BehindYou" or "AskingTheWrongFloor")
            {
                return who;
            }
        }

        return null;
    }

    private static double CoatX(object coat) => (double)Get(Get(coat, "Walk")!, "X")!;

    private static double CoatY(object coat) => (double)Get(Get(coat, "Walk")!, "Y")!;

    private static bool Afoot(object coat) => (bool)Get(Get(coat, "Walk")!, "Afoot")!;

    private static double RangeToCoat(Pages.Map map, object coat)
    {
        double dx = CoatX(coat) - (double)Field(map, "_avatarX")!;
        double dy = CoatY(coat) - (double)Field(map, "_avatarY")!;
        return Math.Sqrt((dx * dx) + (dy * dy));
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

    /// <summary>One frame the way the game runs it — and BOTH clocks, because this half is counted in real
    /// seconds off the frame stamp while the room's own hours are sim seconds.</summary>
    private static void RunFrames(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Field(map, "SimTime")! + dt);
            Set(map, "_lastTimestampMs", (double?)(((double?)Field(map, "_lastTimestampMs") ?? 0) + (dt * 1000)));
            Invoke(map, "AdvanceBarWalkers", dt);
        }
    }

    private static string? PulseSaying(Pages.Map map)
    {
        object pulse = Field(map, "_pulse")!;
        object? said = pulse.GetType().GetProperty("Message", Hidden)!.GetValue(pulse);
        return said as string;
    }

    private static FieldInfo FieldOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name.");

    private static object? Field(Pages.Map map, string name) => FieldOf(name).GetValue(map);

    private static void Set(Pages.Map map, string name, object? value) => FieldOf(name).SetValue(map, value);

    private static object? Get(object o, string member) => o.GetType().GetProperty(member, Hidden)!.GetValue(o);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo call = typeof(Pages.Map).GetMethod(method, Hidden)
            ?? throw new InvalidOperationException($"Map has no `{method}` — this guard is reading a dead name.");
        try
        {
            return call.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
