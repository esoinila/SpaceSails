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
/// <para>Owner, 2026-09-01: <i>"… or trying to lose a tail our selves :-D"</i>. Nine claims, each with its
/// own revert:</para>
///
/// <list type="number">
/// <item>nobody is behind a captain nobody has written anything about — and the folder is what changes it;</item>
/// <item>he comes in after you, keeps his band, and ORDERS NOTHING;</item>
/// <item><b>until he is noticed, nothing in this game says one word</b> — no pulse, no card, no book;</item>
/// <item>the chair that faces the door pays off, and only while you are actually sitting in it;</item>
/// <item>the same coat through two doorways pays off — and a LOCKED leaf is not a doorway;</item>
/// <item>breaking his line for long enough loses him, he leaves, the line plays and the book files it under
/// the PLACE — and a captain who never noticed him is told nothing at all;</item>
/// <item>a quiet verb done with him watching BURNS the place, <b>and nothing is said at the moment</b>;</item>
/// <item>…and when the captain comes back it is tidy, the book says why, and the burn is spent;</item>
/// <item>…and a captain who shook him first pays nothing, which is what the nine seconds of stone buy.</item>
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
    /// #1062 · <b>HE COMES IN AFTER YOU, TAKES A PLACE IN THE ROOM, AND ORDERS NOTHING.</b>
    ///
    /// <para>Three claims about one body. He is not on the floor while the captain is still in the concourse
    /// (the whole shape of the beat is that he follows you in). Once the captain is in the room he is, and he
    /// settles somewhere IN it. And he never goes to the counter — the one fixture in this room where service
    /// happens, and the one spot the canon line says he has not been to.</para>
    ///
    /// <para>#1229 · <b>the middle claim was "inside the band Core publishes", and it was the bug.</b> A
    /// 9–30 du band does not fit in a station bar, so the sounding's first answer was nineteen units down the
    /// concourse — outside the room, through the one doorway — and the assertion passed because it only asked
    /// the RANGE. It asks the room now: he is on the captain's own side of the bar's own south wall, which is
    /// where a man who came in after you is by definition. What he does in here is take a POST rather than
    /// keep a band, and that is <c>TheManTakesAPostTests</c>' subject.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>InTheBar</c> clause deleted from <c>AdvanceTheCoat</c> —
    /// <i>the man is already standing in the bar on the frame the captain clamps on, which is not a tail, it
    /// is a fixture</i>.</para>
    /// </summary>
    [Fact]
    public void HeComesInAfterYouTakesAPlaceInTheRoomAndNeverGoesToTheCounter()
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

        // …and he is dealt OUTSIDE the room, not on the captain's feet: he comes in AFTER you.
        HavenInterior.BarFloor bar = HavenInterior.BarBand(Berth)!.Value;
        Assert.True(CoatY(coat) < bar.FloorY,
            "he was dealt inside the room the captain is already standing in, which is not following him in.");

        // He settles, and where he settles is IN THE ROOM WITH THE CAPTAIN.
        for (int i = 0; i < 600 && Afoot(TheCoat(map)!); i++)
        {
            RunFrames(map, 1);
        }

        coat = TheCoat(map)!;
        Assert.True(CoatY(coat) > bar.FloorY,
            $"he settled at ({CoatX(coat):F2},{CoatY(coat):F2}) — the captain is in the bar and he is not in " +
            "it with him (#1229).");

        // ORDERS NOTHING. The room publishes exactly one place where service happens, and seven tops a patron
        // sits at; he is at none of them. #1229 · the tops are in this sweep now as well as the counter,
        // because a POST is against the room's own stone and so is a bar's furniture.
        foreach (DeckReachability.Point service in bar.Fixtures)
        {
            double dx = service.X - CoatX(coat), dy = service.Y - CoatY(coat);
            Assert.True(Math.Sqrt((dx * dx) + (dy * dy)) > DeckPlan.InteractRadius,
                "he is standing at the counter — the one thing the canon says he has not done.");
        }

        foreach (DeckReachability.Point top in bar.Tops)
        {
            double dx = top.X - CoatX(coat), dy = top.Y - CoatY(coat);
            Assert.True(Math.Sqrt((dx * dx) + (dy * dy)) > DeckPlan.InteractRadius,
                "he is standing at a top — a seat would make him a patron, and he has not ordered.");
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

        // …and the two leaves that never open are not doorways AT ALL, however squarely a body stands in
        // them. The assertion is NULL and not "some other index", which is the whole of what this guard is
        // for — the first draft asked only that the cellar was not the BAR's door, and a cellar counted under
        // its own index satisfies that while being exactly the bug: two leaves nobody can walk through, in
        // one room, adding up to a tell a man earns by standing still. It stayed GREEN on the revert.
        Assert.NotEmpty(bar.Doors);
        foreach (UndergroundComplex.LockedDoor leaf in bar.Doors)
        {
            Assert.Null((int?)Invoke(map, "TheDoorwayHeIsIn", leaf.X1, (leaf.Y1 + leaf.Y2) / 2));
        }

        // …and the locked leaves really are ON the plan at those coordinates, so the claim above is about a
        // door that exists rather than about empty floor.
        DeckPlan plan = (DeckPlan)Field(map, "_deckPlan")!;
        foreach (UndergroundComplex.LockedDoor leaf in bar.Doors)
        {
            Assert.Contains(plan.Doors, d =>
                d.Locked
                && Math.Abs(((d.X1 + d.X2) / 2) - leaf.X1) < DeckPlan.DoorOpenRadius
                && Math.Abs(((d.Y1 + d.Y2) / 2) - ((leaf.Y1 + leaf.Y2) / 2)) < DeckPlan.DoorOpenRadius);
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
        HavenInterior.BarFloor bar = HavenInterior.BarBand(Berth)!.Value;
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 1);
        Assert.NotNull(TheCoat(map));

        // He came in through the bar's own doorway, and the ledger has it.
        RunFrames(map, 10);
        var seen = (ICollection<int>)Field(map, "_coatDoors")!;
        Assert.NotEmpty(seen);
        Assert.False((bool)Field(map, "_coatSeen")!, "one doorway is not a tell.");

        // #1229 · THE HONEST QUESTION, AND THE WHOLE POINT OF RE-GROUNDING THIS GUARD. The captain walks in,
        // the way a player does, and the man settles WHERE HE IS — in the room with him, on the captain's own
        // side of the bar's south wall. Until #1229 he was planted nineteen units down the concourse, on the
        // exit path, before the captain had moved at all: this test was green BECAUSE of that bug, and the
        // sequence it claimed to be watching (posted inside → captain exits → he follows through door one →
        // captain takes a second doorway → he follows through door two) never happened.
        WalkCaptainTo(map, bar.Tops[2].X, bar.Tops[2].Y);
        for (int i = 0; i < 600 && Afoot(TheCoat(map)!); i++)
        {
            RunFrames(map, 1);
        }

        object inTheRoom = TheCoat(map)!;
        Assert.True(CoatY(inTheRoom) > bar.FloorY,
            $"before the captain moves a step, the man is standing at ({CoatX(inTheRoom):F2}," +
            $"{CoatY(inTheRoom):F2}) and the room's south wall is at y={bar.FloorY:F2} — he is NOT in the " +
            "room with the captain, so whatever this guard sees next is not a man following him out (#1229).");

        // …and now the captain WALKS — at a walking pace, out of the bar, across the concourse and down
        // #1199's tube to the far end of it, where the only spot in the room with a line to him is its own
        // mouth. He is never teleported: a captain who blinked across the floor would break the man's line
        // for him, and this guard would be testing the LOSING rule by accident.
        //
        // #1199 (2026-09-18) · THE LANDMARK MOVED WITH THE ROOM, and the claim above did not. The tube's
        // blind end used to BE the rail; the walk is a T now, and the far end of the leg is the THROAT where
        // it opens into the gallery. The rail is out in the crossbar, at the one stretch of glass the mouth
        // cannot see — walk all the way to THERE and the man at the mouth has no line at all, which is the
        // LOSING rule and a different guard's business (the one below). Standing here is still the captain
        // doubling back into a one-way room with one doorway between them, which is what this tell is.
        DeckReachability.Point mouth = HavenInterior.TheWalksMouthAt(Berth)!.Value;
        DeckReachability.Point throat = HavenInterior.TheThroatAt(Berth)!.Value;
        WalkCaptainTo(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y - 4);
        WalkCaptainTo(map, 2.5, 40);
        WalkCaptainTo(map, mouth.X, mouth.Y);
        WalkCaptainTo(map, throat.X, throat.Y);

        for (int i = 0; i < 900 && !(bool)Field(map, "_coatSeen")!; i++)
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
        Settle(map);

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
        Set(map, "_pulse", default(PulseSlot));   // the chair says its own line on the way up; not ours.
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

    // ── 7 · THE BURN ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>A QUIET VERB DONE WITH SOMEBODY WATCHING BURNS THE PLACE — AND NOTHING IS SAID.</b>
    ///
    /// <para>Driven through `ThisPortHasNowDealtAKey`, which is not a shortcut: it is the ONE strike-off both
    /// of a berth's quiet verbs already run through (the favour across a contact's table, the code bought off
    /// a fence at a desk), and the burn hangs on it precisely so the two of them cannot come to two views of
    /// what being watched costs.</para>
    ///
    /// <para>Three claims: the port is struck, **not one word is raised on the frame it happens** (#761 — the
    /// burn is told when the book shows it, and never at the moment), and the port's own question now answers
    /// "nothing left here" so neither row is ever drawn to be refused.</para>
    ///
    /// <para><b>Revert that reddened it:</b> `TheyBurnThisPlaceIfSomebodyIsWatching` raising the line on the
    /// spot — <i>the game warning the captain that the deal he just did was watched, which turns the man on
    /// the floor into a meter the player manages</i>.</para>
    /// </summary>
    [Fact]
    public void AQuietVerbWithSomebodyWatchingBurnsThePlaceAndSaysNothingAtTheMoment()
    {
        Pages.Map map = Tailed(Berth);
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 1);
        Assert.NotNull(TheCoat(map));

        Assert.False((bool)Invoke(map, "ThisPlaceWasWalkedFirst", Berth)!);
        Invoke(map, "ThisPortHasNowDealtAKey", Berth);

        Assert.True((bool)Invoke(map, "ThisPlaceWasWalkedFirst", Berth)!);

        // …and the port's own question answers "nothing left here" for a reason the WATCH cannot explain.
        //
        // Asking it on this watch would be vacuous — the act just struck the port off for this watch anyway,
        // so the answer is true whether or not the burn exists, and the first draft of this line stayed GREEN
        // with the burn clause deleted from `ThisPortHasAlreadyDealtAKey`. The burn's whole claim is that it
        // OUTLIVES the watch that made it, so the clock is wound on to a watch with no strike-off in it and
        // the question is asked there.
        double was = (double)Field(map, "SimTime")!;
        Set(map, "SimTime", was + PatronRota.WatchSeconds);
        Assert.False(
            ((System.Collections.Generic.HashSet<string>)Field(map, "_roomsTurnedOver")!)
                .Contains(BlackOpsKey.ThePortHasDealtOne(Berth, BlackOpsKey.FenceWindow((double)Field(map, "SimTime")!))),
            "the next watch already has a strike-off of its own, so this question could not tell them apart.");
        Assert.True((bool)Invoke(map, "ThisPortHasAlreadyDealtAKey", Berth)!);
        Set(map, "SimTime", was);

        // SILENT. On this frame and on a hundred more.
        RunFrames(map, 100);
        Assert.Null(PulseSaying(map));
        Assert.Null(Field(map, "_storyCard"));
        Assert.Empty((IEnumerable<FieldNote>)Field(map, "_fieldNotes")!);
    }

    /// <summary>
    /// #1062 · <b>AND WHEN HE COMES BACK, IT IS TIDY.</b> The captain casts off and docks again; on the next
    /// visit the place says the one authored sentence, the book files the one authored line under the PLACE,
    /// and the burn is spent — the visit after that says nothing.
    ///
    /// <para>Coming back is done the way the game does it, through the one place that knows the berth has
    /// changed (`ForgetTheBarsFeet`), rather than by reaching in and clearing the visit's own set.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the `_coatBurnedThisVisit` clause dropped from
    /// `TheBurnIsToldHere` — <i>the place told the captain it had been gone through on the same frame he
    /// finished going through it, which is the burn arriving at the moment</i>.</para>
    /// </summary>
    [Fact]
    public void AndWhenYouComeBackItIsTidyAndTheBookSaysWhy()
    {
        Pages.Map map = Tailed(Berth);
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 1);
        Assert.NotNull(TheCoat(map));
        Invoke(map, "ThisPortHasNowDealtAKey", Berth);
        RunFrames(map, 50);
        Assert.Null(PulseSaying(map));

        // Cast off, and come back. The room forgets; the register does not.
        Invoke(map, "ForgetTheBarsFeet", (string?)null);
        Invoke(map, "ForgetTheBarsFeet", Berth);
        RunFrames(map, 1);

        Assert.Equal(TheTailBehindYou.TheBurnLine, PulseSaying(map));

        var book = (IReadOnlyList<FieldNote>)Field(map, "_fieldNotes")!;
        FieldNote note = Assert.Single(book);
        string place = (string)Invoke(map, "DockedStationName")!;
        Assert.Equal(TheTailBehindYou.BurnNote(place), note.Text);
        Assert.Equal(TheTailBehindYou.BurnSubjects(place), note.Subjects);
        CaseSubjects.Subject filed = Assert.Single(CaseSubjects.On(in note));
        Assert.Equal(CaseSubjects.Kind.Place, filed.Of);

        // SPENT. The tag is gone, the port deals again, and the next evening says nothing.
        Assert.False((bool)Invoke(map, "ThisPlaceWasWalkedFirst", Berth)!);
        Set(map, "_pulse", default(PulseSlot));
        Invoke(map, "ForgetTheBarsFeet", (string?)null);
        Invoke(map, "ForgetTheBarsFeet", Berth);
        RunFrames(map, 100);
        Assert.Null(PulseSaying(map));
        Assert.Single((IReadOnlyList<FieldNote>)Field(map, "_fieldNotes")!);
    }

    /// <summary>
    /// #1062 · <b>AND A CAPTAIN WHO SHOOK HIM FIRST PAYS NOTHING.</b> The same verb at the same port with the
    /// man off the floor, and the place is not burned — which is the counter-play, and the reason the nine
    /// seconds of stone are worth spending.
    ///
    /// <para>Anti-vacuity: it is the SAME page, the same berth and the same call as the guard above, so the
    /// only difference between burned and not burned is whether anybody was there to watch.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the `TheCoatIsAfoot` clause dropped from
    /// `TheyBurnThisPlaceIfSomebodyIsWatching` — <i>every quiet verb at a berth the captain has ever been
    /// followed at burns that berth, whether or not anybody is still behind him</i>.</para>
    /// </summary>
    [Fact]
    public void AVerbDoneAfterYouHaveShakenHimCostsNothing()
    {
        Pages.Map map = Tailed(Berth);
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        RunFrames(map, 1);
        Assert.NotNull(TheCoat(map));

        // Shake him honestly — down the gangway, and wait him out.
        StandCaptainAt(map, 2.5, 6);
        for (int i = 0; i < 1200 && TheCoat(map) is not null; i++)
        {
            RunFrames(map, 1);
        }
        Assert.Null(TheCoat(map));
        Assert.True((bool)Field(map, "_coatLost")!);

        Invoke(map, "ThisPortHasNowDealtAKey", Berth);
        Assert.False((bool)Invoke(map, "ThisPlaceWasWalkedFirst", Berth)!);

        Invoke(map, "ForgetTheBarsFeet", (string?)null);
        Invoke(map, "ForgetTheBarsFeet", Berth);
        StandCaptainAt(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y + 6);
        Set(map, "_pulse", default(PulseSlot));
        RunFrames(map, 100);
        Assert.Null(PulseSaying(map));
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

    /// <summary>Let the man reach wherever he set off for, so a guard about what he does when he is STANDING
    /// is not asked of a body that is still walking.</summary>
    private static void Settle(Pages.Map map)
    {
        for (int i = 0; i < 900 && TheCoat(map) is { } coat && Afoot(coat); i++)
        {
            RunFrames(map, 1);
        }
    }

    /// <summary>
    /// WALK the captain there, at the pace a person walks, one frame at a time.
    ///
    /// <para>Every other guard in this file puts him somewhere and gets on with it, which is fine when the
    /// claim is about a spot. It is NOT fine when the claim is about somebody following him: a captain who
    /// blinks eighteen deck units breaks the man's line himself, and the guard would quietly become a test of
    /// the losing rule. The step is <c>NpcWalk.PaceDu</c> per second — the man's own pace, so he can keep
    /// up.</para>
    /// </summary>
    private static void WalkCaptainTo(Pages.Map map, double x, double y, double dt = 0.1)
    {
        double step = NpcWalk.PaceDu * dt;
        for (int guard = 0; guard < 4000; guard++)
        {
            double atX = (double)Field(map, "_avatarX")!, atY = (double)Field(map, "_avatarY")!;
            double dx = x - atX, dy = y - atY;
            double left = Math.Sqrt((dx * dx) + (dy * dy));
            if (left <= step)
            {
                StandCaptainAt(map, x, y);
                RunFrames(map, 1, dt);
                return;
            }

            StandCaptainAt(map, atX + (dx / left * step), atY + (dy / left * step));
            RunFrames(map, 1, dt);
        }
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
