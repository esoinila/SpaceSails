using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1202 slice 3 · <b>THE TAIL AT HER TABLE, ON A LIVE PAGE.</b> The arithmetic is Core's
/// (<c>TheTailAtHerTableTests</c>); what is driven here is the shipping page at the composed dev start
/// <c>/map?dock=selene-gate&amp;ashore=1&amp;spike=1&amp;tailed=1</c>, walked the way a tester walks it: out of the
/// bar with the grey coat behind, into the tube, the coat to its mouth, down to the gallery, and TAKE THE PAGES at
/// her table while she feeds the machine. Nothing is teleported that the coat could see teleport — a captain who
/// blinked across the floor would break the man's line for him, and every guard here would be a guard about the
/// losing rule instead. Every guard was watched go red on the revert its summary names.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[Collection(SpaceSails.Core.Tests.StopRegisterCollection.Name)]
public sealed class TheTakeCanBeSeenTests
{
    private const string Port = "selene-gate";
    private const string Composed = "/map?dock=selene-gate&ashore=1&spike=1&tailed=1";
    private const string Untailed = "/map?dock=selene-gate&ashore=1&spike=1";
    private const double Dt = 0.1;

    private static readonly double AvatarSpeed =
        (double)typeof(Pages.Map).GetField("AvatarSpeed", TestTree.AnythingAtAll)!.GetRawConstantValue()!;

    // ── the bench ───────────────────────────────────────────────────────────────────────────────────────

    private static async Task<DeskBench> Booted(string url)
    {
        DeskBench b = await DeskBench.BootAsync(url);
        await b.RenderAsync();
        return b;
    }

    private static void Frame(DeskBench b, int frames = 1)
    {
        for (int i = 0; i < frames; i++)
        {
            // The pulse slot's own clock, a tenth of a second a frame, so a line said earlier dwells and then
            // expires the way it does on a screen (the berth's sim clock does not run while he walks it).
            b.Poke("_lastTimestampMs", (double?)(((double?)b.Peek("_lastTimestampMs") ?? 0) + (Dt * 1000)));
            b.CallOnTheDispatcher("AdvanceBarWalkers", Dt);
        }
    }

    private static double Ax(DeskBench b) => (double)b.Peek("_avatarX")!;

    private static double Ay(DeskBench b) => (double)b.Peek("_avatarY")!;

    private static void StandAt(DeskBench b, double x, double y)
    {
        b.Poke("_avatarX", x);
        b.Poke("_avatarY", y);
        b.Poke("_lookPrevAvatarX", x);
        b.Poke("_lookPrevAvatarY", y);
    }

    /// <summary>At the captain's own pace, one frame a step.</summary>
    private static void WalkTo(DeskBench b, double x, double y)
    {
        double step = AvatarSpeed * Dt;
        for (int guard = 0; guard < 4000; guard++)
        {
            double dx = x - Ax(b), dy = y - Ay(b), left = Math.Sqrt((dx * dx) + (dy * dy));
            if (left <= step)
            {
                StandAt(b, x, y);
                Frame(b);
                return;
            }

            StandAt(b, Ax(b) + (dx / left * step), Ay(b) + (dy / left * step));
            Frame(b);
        }
    }

    private static List<Pages.Map.Walker> Afoot(DeskBench b) => (List<Pages.Map.Walker>)b.Peek("_barAfoot")!;

    private static Pages.Map.Walker? TheCoat(DeskBench b) => Afoot(b).SingleOrDefault(w => w.For == Pages.Map.Errand.BehindYou);

    private static Pages.Map.Walker Her(DeskBench b) =>
        Afoot(b).SingleOrDefault(w => w.For == Pages.Map.Errand.AtHerPages)
        ?? throw new InvalidOperationException("premise: she is on the gallery floor");

    private static Pages.Map.Quest Hers(DeskBench b) =>
        Assert.Single((List<Pages.Map.Quest>)b.Peek("_quests")!, q => q.Kind == Pages.Map.QuestKind.CarryThePress);

    private static CarryThePress.Passage PassageOf(DeskBench b) => CarryThePress.Passage.Read(Hers(b).Pin);

    private static void Rewrite(DeskBench b, CarryThePress.Passage p) => b.CallOnTheDispatcher("RewritePassage", Hers(b), p);

    private static List<FieldNote> Book(DeskBench b) => [.. (IEnumerable<FieldNote>)b.Peek("_fieldNotes")!];

    private static int Filed(DeskBench b, string line) => Book(b).Count(n => string.Equals(n.Text, line, StringComparison.Ordinal));

    private static bool InTheGallery(DeskBench b) => HavenInterior.InTheGallery(Port, Ax(b), Ay(b));

    /// <summary>Out of the bar and across the concourse, a step into the tube, and a wait while the man comes to
    /// its mouth — the one doorway between the two of them.</summary>
    private static void IntoTheTubeWithHimAtItsMouth(DeskBench b)
    {
        (double tx, double ty, _) = HavenInterior.BarThreshold;
        DeckReachability.Point mouth = HavenInterior.TheWalksMouthAt(Port)!.Value;
        WalkTo(b, tx, ty - 4);
        WalkTo(b, 2.5, 40);
        WalkTo(b, mouth.X - 4, mouth.Y);
        for (int i = 0; i < 600 && TheCoat(b) is { } c && Dist(c.Walk.X - mouth.X, c.Walk.Y - mouth.Y) > 2; i++)
        {
            Frame(b);
        }

        Pages.Map.Walker coat = TheCoat(b) ?? throw new InvalidOperationException("premise: the grey coat came in after him");
        Assert.True(Dist(coat.Walk.X - mouth.X, coat.Walk.Y - mouth.Y) <= 2,
            $"premise: the coat came to the mouth of the tube; he is at ({coat.Walk.X:F1},{coat.Walk.Y:F1}).");
    }

    /// <summary>Down the tube to the throat, where the man at the mouth still has him, and a wait there until she
    /// is at the machine.</summary>
    private static void ToTheThroatUntilSheIsAway(DeskBench b)
    {
        DeckReachability.Point throat = HavenInterior.TheThroatAt(Port)!.Value;
        WalkTo(b, throat.X, throat.Y);
        Her(b).PassHeld = SpikeIt.WritesAtMostSeconds + 1;
        for (int i = 0; i < 400 && !(Her(b).Table == -1 && !Her(b).Walk.Afoot); i++)
        {
            Frame(b);
        }

        Assert.True(Her(b).Table == -1 && !Her(b).Walk.Afoot, "premise: she went to the machine.");
    }

    /// <summary>Her table, [E], and the card carries TAKE THE PAGES.</summary>
    private static void SitAtHerTable(DeskBench b)
    {
        DeckReachability.Point top = HavenInterior.GalleryTops(Port)[SpikeIt.HerTable];
        WalkTo(b, top.X, top.Y);
        Assert.True((bool)b.CallOnTheDispatcher("TryTakeBarTop")!, "her table refused [E].");
        Frame(b);
        Assert.Equal(SpikeIt.TakeThePages, SpikeIt.Offers(Seated(b).Scene));
    }

    private static Pages.Map.TableTalk Seated(DeskBench b) => (Pages.Map.TableTalk)b.Call("get_SeatedTable")!;

    private static Task Press(DeskBench b, string move) => (Task)b.CallOnTheDispatcher("TableMoveClicked", move)!;

    /// <summary>The whole seen take, as a tester plays it at the composed start.</summary>
    private static async Task<DeskBench> ASeenTake()
    {
        DeskBench b = await Booted(Composed);
        IntoTheTubeWithHimAtItsMouth(b);
        ToTheThroatUntilSheIsAway(b);
        SitAtHerTable(b);
        Pages.Map.Walker coat = TheCoat(b)!;
        double range = Dist(coat.Walk.X - Ax(b), coat.Walk.Y - Ay(b));
        Assert.True(TheTailBehindYou.HoldsHisBand(range), $"premise: he holds his band at the take ({range:F1} du).");
        await Press(b, SpikeIt.TakeThePages);
        return b;
    }

    /// <summary>The window comes: her story's due time is put behind the clock, and the wire's pass runs.</summary>
    private static void TheWindowComes(DeskBench b)
    {
        Rewrite(b, PassageOf(b) with { TurnedIn = (double)b.Peek("SimTime")! - CarryThePress.StoryAfterSeconds - 1 });
        b.CallOnTheDispatcher("ThePressRunsHerStory");
    }

    private static List<NewsWire.NewsEvent> Stories(DeskBench b) =>
        [.. ((List<NewsWire.NewsEvent>)b.Peek("_newsEvents")!).Where(e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled)];

    private static double Dist(double dx, double dy) => Math.Sqrt((dx * dx) + (dy * dy));

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE COMPOSED START: the spike in hand, the client's page in the satchel, Lind at the far table — and the
    /// captain left at the bar's threshold (not stood in the gallery), so the grey coat comes in after him, and the
    /// DEV line says to walk him out along the tube.
    ///
    /// <para><b>RED</b> by dropping <c>!tailed</c> from <c>SpikeItIfAsked</c> (the captain was stood in the gallery,
    /// the coat was never dealt, and the start could not stage the beat it is for).</para>
    /// </summary>
    [Fact]
    public async Task TheComposedStartLeavesHimAtTheBarWithTheCoatComingInAfter()
    {
        DeskBench b = await Booted(Composed);
        Assert.Contains("DEV ?spike=1", b.Pulse, StringComparison.Ordinal);
        Assert.Contains("walk him out along the tube", b.Pulse, StringComparison.Ordinal);
        Assert.True(PassageOf(b).Spike);
        Assert.False(InTheGallery(b));
        Frame(b, 30);
        Assert.NotNull(TheCoat(b));
        Assert.Equal(SpikeIt.HerTable, Her(b).Table);
    }

    // ── THE TAKE, SEEN ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE COAT'S LINE IS FILED ONCE, WITH THE TAKE, AND NOTHING AT THE TABLE SAYS SO. The take line stays; the
    /// coat's line is its neighbour in the book, both under 📰; the card says what an unseen take's card says, and
    /// the pulse carries neither book line. A second press files nothing.
    ///
    /// <para><b>RED</b> by not filing <c>SeenTakeEntry</c> in <c>ThePagesAreTouched</c> (the book had one line), and
    /// by filing it unconditionally (the unseen guard below had two).</para>
    /// </summary>
    [Fact]
    public async Task TheCoatsLineIsFiledOnceWithTheTake()
    {
        DeskBench b = await ASeenTake();
        List<FieldNote> book = Book(b);
        int take = book.FindIndex(n => n.Text == SpikeIt.TookThePages(12));
        int coat = book.FindIndex(n => n.Text == SpikeIt.SeenTakeEntry);
        Assert.True(take >= 0 && coat >= 0, "both book lines are filed.");
        Assert.Equal(1, Math.Abs(coat - take));
        Assert.Equal(CarryThePress.Glyph, book[coat].Glyph);
        Assert.Equal(1, Filed(b, SpikeIt.SeenTakeEntry));
        Assert.True(PassageOf(b).Watched);

        Assert.Equal(SpikeIt.TakeThePagesLine, Seated(b).Outcome);
        Assert.DoesNotContain(SpikeIt.SeenTakeEntry, b.Pulse, StringComparison.Ordinal);

        await Press(b, SpikeIt.TakeThePages);
        Frame(b, 5);
        Assert.Equal(1, Filed(b, SpikeIt.SeenTakeEntry));
        Assert.Equal(1, Filed(b, SpikeIt.TookThePages(12)));
    }

    /// <summary>
    /// A SEEN TAKE FORCES LATE EVEN WITH THE CLIENT'S PAGE LEFT: LEAVE YOUR PAGE still plays at the table, but at
    /// the window her own story runs, not the client's sentence; the book says it ran; and the desk's next open pays
    /// nothing and says <i>'Noted that you were seen.'</i> — once.
    ///
    /// <para><b>RED</b> by passing <c>AtTheWindow(next.Pages)</c> again in <c>TheWindowDecides</c> (ALTERED: the
    /// client's sentence ran under her name), and by passing <c>LateLine</c> in <c>TheSpikeIsSettled</c> (the
    /// receipt said "tried").</para>
    /// </summary>
    [Fact]
    public async Task ASeenTakeIsLateEvenWithTheClientsPageLeft()
    {
        DeskBench b = await ASeenTake();
        Frame(b);
        Assert.Equal(SpikeIt.LeaveYourPage, SpikeIt.Offers(Seated(b).Scene));
        await Press(b, SpikeIt.LeaveYourPage);
        Assert.Equal(SpikeIt.Pages.Swapped, PassageOf(b).Pages);

        string body = (string)b.CallOnTheDispatcher("BodyName", Hers(b).DestBodyId!)!;
        TheWindowComes(b);
        Assert.Equal(SpikeIt.Outcome.Late, PassageOf(b).Outcome);
        Assert.Equal(CarryThePress.Story(body, withTheTin: true), Assert.Single(Stories(b)).Subject);
        Assert.Equal(1, Filed(b, CarryThePress.StoryRanLine));
        Assert.Equal(0, Filed(b, SpikeIt.AlteredEntry));

        Frame(b, 200);   // …and some while later, at a dark-web desk
        int before = (int)b.Peek("_credits")!;
        b.CallOnTheDispatcher("TheSpikeIsSettled");
        Assert.Equal(before, (int)b.Peek("_credits")!);
        Assert.Contains("💳 " + SpikeIt.SeenLateLine, b.Pulse, StringComparison.Ordinal);
        Assert.True(PassageOf(b).Paid);
    }

    /// <summary>
    /// HER STACK IS SQUARED — TOLD ONCE, ON ENTERING, AND ONLY AFTER A SEEN TAKE. Sitting on at her table says
    /// nothing; walking out into the tube says nothing; walking back into the gallery says it (where he is looking,
    /// and in the book under 📰); out and in again says nothing more.
    ///
    /// <para><b>RED</b> by dropping the <c>TheStackIsSquaredIfSeen</c> call from <c>AdvanceTheStringerAtHerPages</c>
    /// (never told), and by telling it on the <c>NotYet</c> step (told at the table, the frame after the take).</para>
    /// </summary>
    [Fact]
    public async Task HerSquaredStackIsToldOnceOnEnteringAfterASeenTake()
    {
        DeskBench b = await ASeenTake();
        Frame(b, 20);
        Assert.Equal(0, Filed(b, SpikeIt.SquaredLine));

        b.CallOnTheDispatcher("StandUpFromTable");
        DeckReachability.Point throat = HavenInterior.TheThroatAt(Port)!.Value;
        WalkTo(b, throat.X + 6, throat.Y);
        Assert.False(InTheGallery(b));
        Frame(b, 200);   // out in the tube a while, long enough for anything said at the table to have gone
        Assert.Equal(0, Filed(b, SpikeIt.SquaredLine));

        WalkTo(b, throat.X - 3, throat.Y);
        Assert.True(InTheGallery(b));
        Assert.Equal(1, Filed(b, SpikeIt.SquaredLine));
        Assert.Contains(SpikeIt.SquaredLine, b.Pulse, StringComparison.Ordinal);

        WalkTo(b, throat.X + 6, throat.Y);
        WalkTo(b, throat.X - 3, throat.Y);
        Frame(b, 10);
        Assert.Equal(1, Filed(b, SpikeIt.SquaredLine));
        Assert.Equal(SpikeIt.Stack.Told, PassageOf(b).Stack);
    }

    // ── THE TAKE, UNSEEN ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// AN UNSEEN TAKE IS EXACTLY SLICE 2: with no tail at all, and with the tail lost first (the captain walked
    /// straight down the tube and waited at the far table longer than the man at the mouth could keep him), the
    /// take files only its own line, the contract's line carries no seen key, the squared stack is never told, the
    /// window is SPIKED and the desk pays the full purse.
    ///
    /// <para><b>RED</b> by <c>TheCoatSeesTheTake</c> answering true with no man on the floor (the no-tail case filed
    /// the coat's line). The lost clause itself is pinned by <c>AManWhoHasLostYouSeesNothingWhereverHeStands</c>:
    /// counting the wrong-floor man as on him stays green HERE, honestly, because by the time she leaves her table
    /// the man who lost him has walked out of his band.</para>
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnUnseenTakeIsSliceTwos(bool tailedButLostFirst)
    {
        DeskBench b = await Booted(tailedButLostFirst ? Composed : Untailed);
        if (tailedButLostFirst)
        {
            Frame(b, 30);
            Assert.NotNull(TheCoat(b));
            DeckReachability.Point throat = HavenInterior.TheThroatAt(Port)!.Value;
            WalkTo(b, 2.5, 40);
            WalkTo(b, throat.X, throat.Y);
            for (int i = 0; i < 400 && !(bool)b.Peek("_coatLost")!; i++)
            {
                Frame(b);
            }

            Assert.True((bool)b.Peek("_coatLost")!, "premise: the man lost him.");
        }

        ToTheThroatUntilSheIsAway(b);
        SitAtHerTable(b);
        await Press(b, SpikeIt.TakeThePages);
        Assert.Equal(1, Filed(b, SpikeIt.TookThePages(12)));
        Assert.Equal(0, Filed(b, SpikeIt.SeenTakeEntry));
        Assert.False(PassageOf(b).Watched);
        Assert.DoesNotContain("watched", Hers(b).Pin, StringComparison.Ordinal);

        b.CallOnTheDispatcher("StandUpFromTable");
        DeckReachability.Point t = HavenInterior.TheThroatAt(Port)!.Value;
        WalkTo(b, t.X + 6, t.Y);
        WalkTo(b, t.X - 3, t.Y);
        Frame(b, 5);
        Assert.Equal(0, Filed(b, SpikeIt.SquaredLine));

        TheWindowComes(b);
        Assert.Equal(SpikeIt.Outcome.Spiked, PassageOf(b).Outcome);
        Assert.Empty(Stories(b));
        int before = (int)b.Peek("_credits")!;
        b.CallOnTheDispatcher("TheSpikeIsSettled");
        Assert.Equal(SpikeIt.Purse(Hers(b).Reward), (int)b.Peek("_credits")! - before);
        Assert.DoesNotContain(SpikeIt.SeenLateLine, b.Pulse, StringComparison.Ordinal);
    }

    /// <summary>
    /// A MAN WHO HAS LOST YOU SEES NOTHING, WHEREVER HE IS STANDING: the same walk as the seen take, the same man
    /// at the same spot in his band — but re-badged the way the tail re-badges him on the frame his clock runs out
    /// (<c>AskingTheWrongFloor</c>). The take is unseen. (The live lost case above is a man already out of band by
    /// the time she leaves her table; this pins the clause itself.)
    ///
    /// <para><b>RED</b> by counting <c>Errand.AskingTheWrongFloor</c> as on him in <c>TheCoatSeesTheTake</c>.</para>
    /// </summary>
    [Fact]
    public async Task AManWhoHasLostYouSeesNothingWhereverHeStands()
    {
        DeskBench b = await Booted(Composed);
        IntoTheTubeWithHimAtItsMouth(b);
        ToTheThroatUntilSheIsAway(b);
        SitAtHerTable(b);
        Pages.Map.Walker coat = TheCoat(b)!;
        Assert.True(TheTailBehindYou.HoldsHisBand(Dist(coat.Walk.X - Ax(b), coat.Walk.Y - Ay(b))), "premise: in his band.");
        Afoot(b)[Afoot(b).IndexOf(coat)] = new Pages.Map.Walker { Walk = coat.Walk, Table = coat.Table, For = Pages.Map.Errand.AskingTheWrongFloor };

        await Press(b, SpikeIt.TakeThePages);
        Assert.Equal(1, Filed(b, SpikeIt.TookThePages(12)));
        Assert.Equal(0, Filed(b, SpikeIt.SeenTakeEntry));
        Assert.False(PassageOf(b).Watched);
    }

    /// <summary>
    /// THE RECEIPT LINE IS ONLY FOR A SEEN LATE: a story nobody touched runs LATE and the desk says slice 2's
    /// <i>'Noted that you tried.'</i>, never <i>'you were seen'</i>. <b>RED</b> by <c>LateReceipt</c> answering the
    /// seen line for every LATE.
    /// </summary>
    [Fact]
    public async Task AnUntouchedLateSaysTriedNotSeen()
    {
        DeskBench b = await Booted(Untailed);
        TheWindowComes(b);
        Assert.Equal(SpikeIt.Outcome.Late, PassageOf(b).Outcome);
        b.CallOnTheDispatcher("TheSpikeIsSettled");
        Assert.Contains("💳 " + SpikeIt.LateLine, b.Pulse, StringComparison.Ordinal);
        Assert.DoesNotContain(SpikeIt.SeenLateLine, b.Pulse, StringComparison.Ordinal);
    }
}
