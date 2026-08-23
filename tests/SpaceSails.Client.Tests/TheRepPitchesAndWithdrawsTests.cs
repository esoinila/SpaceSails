using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #973 L2 · HARLAN FESS, DRIVEN. The Core laws are swept in
/// <c>TheRepNeverRemembersYourFaceTests</c>; this file asks the running component the four questions Core
/// cannot answer, because they are about a CARD and a SEEN-SET rather than about arithmetic:
/// <list type="number">
/// <item>the pitch he raises is the one for the policy you actually hold, and a Premium captain is offered
/// nothing to buy;</item>
/// <item>"I already have a policy" raises the signing flashback through the one seam — the bleached plate
/// #973 L1 shipped, counted and logged, with his card still up under it and nothing stacked on it;</item>
/// <item>it comes back ONCE PER LIFE, and the next captain gets it again because it is a different man
/// reaching for it;</item>
/// <item>told no, he takes the card away and remembers it for the rest of the visit — and not one docking
/// longer.</item>
/// </list>
///
/// <para>Nothing here reads source text. The component is real, the seam is the shipped
/// <c>RaiseStoryBeat</c>, the answers go through the shipped <c>AnswerTheRep</c>, and every claim is read
/// off the component's own fields afterwards — a guard that grepped for <c>TheHostIsUp</c> would pass on a
/// build where the call sat below the filing. Each test names the revert that turns it RED.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRepPitchesAndWithdrawsTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string ThreadId = "3b7e51c9d0a44f628e1c7d905af2b613";

    /// <summary>A live component with a thread under it and nothing else running.
    ///
    /// <para>The render handle is the bench <see cref="TheHostedBeatIsCountedOnlyWhenItsHostIsUpTests"/>
    /// uses: a <see cref="ComponentBase"/> that was never attached to a renderer throws out of
    /// <c>StateHasChanged</c>. The thread row is planted rather than minted so the captain has a NAME —
    /// which is the one thing the whole feature is about — and so the retired count can be moved without a
    /// death.</para></summary>
    private static Pages.Map AtATable(InsuranceTier tier = InsuranceTier.None, int retired = 0)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has "
                + "moved, and the seam will throw instead of running.");
        pending.SetValue(map, true);

        List<RetiredCaptain> buried = [];
        for (int i = 0; i < retired; i++)
        {
            buried.Add(new RetiredCaptain(Captains.Name($"{ThreadId}|succ{i}"), 10 * (i + 1)));
        }

        var thread = new GameThreadInfo
        {
            Id = ThreadId,
            Where = "a hive canteen",
            CaptainName = "",
            Retired = buried,
        };

        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[thread]);
        Set(map, "_insurance", new PirateInsurance(tier, tier == InsuranceTier.None ? double.NegativeInfinity : 1e9));
        return map;
    }

    private static FieldInfo FieldOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard needs re-reading.");

    private static void Set(Pages.Map map, string name, object? value) => FieldOf(name).SetValue(map, value);

    private static object? Field(Pages.Map map, string name) => FieldOf(name).GetValue(map);

    private static void Call(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{method}` — this guard needs re-reading."))
        .Invoke(map, args);

    /// <summary>He reaches the table and raises his card — the shipped landing, not a hand-built panel.</summary>
    private static void HeReachesTheTable(Pages.Map map) => Call(map, "HeReachesYourTable");

    private static void Answer(Pages.Map map, NebulaRep.RepMove move) => Call(map, "AnswerTheRep", move);

    private static NebulaRep.RepPitch? TheCard(Pages.Map map) =>
        (NebulaRep.RepPitch?)Field(map, "_repCard");

    private static IReadOnlyDictionary<(StoryBeats.Beat Beat, string? Subject), double> Filed(Pages.Map map) =>
        (IReadOnlyDictionary<(StoryBeats.Beat, string?), double>)Field(map, "_beatsSpoken")!;

    private static IReadOnlyList<string> Ledger(Pages.Map map) =>
        [.. ((IEnumerable<(double SimTime, string Text)>)Field(map, "_autopilotEvents")!).Select(e => e.Text)];

    private static int TimesFiled(Pages.Map map, StoryBeats.Beat beat) =>
        Filed(map).Keys.Count(k => k.Beat == beat);

    // ── The pitch ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CARD HE RAISES IS THE ONE FOR THE POLICY YOU HOLD — read off the live component, so a page that
    /// pitched from a stale tier would be caught here and not by somebody being sold Basic twice.
    /// </summary>
    [Theory]
    [InlineData(InsuranceTier.None)]
    [InlineData(InsuranceTier.Basic)]
    [InlineData(InsuranceTier.Premium)]
    public void HePitchesThePolicyYouActuallyHold(InsuranceTier tier)
    {
        Pages.Map map = AtATable(tier);
        HeReachesTheTable(map);

        NebulaRep.RepPitch card = TheCard(map)!.Value;
        Assert.Equal(NebulaRep.PitchFor(tier, Captains.Name(ThreadId)).Line, card.Line);
    }

    /// <summary>
    /// A PREMIUM CAPTAIN IS OFFERED NOTHING TO BUY. It is the one moment he is likeable and the one a sale
    /// button would ruin.
    ///
    /// <para><b>Proven RED</b> by adding a <c>BuyPremium</c> offer to the Premium arm of
    /// <c>NebulaRep.PitchFor</c>.</para>
    /// </summary>
    [Fact]
    public void ThePremiumCaptainGetsNoSaleButton()
    {
        Pages.Map map = AtATable(InsuranceTier.Premium);
        HeReachesTheTable(map);

        Assert.DoesNotContain(TheCard(map)!.Value.Offers,
                              o => o.Move is NebulaRep.RepMove.BuyBasic or NebulaRep.RepMove.BuyPremium);
        Assert.All(TheCard(map)!.Value.Offers, o => Assert.Equal(0, o.PriceCr));
    }

    // ── The flashback ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// "I ALREADY HAVE A POLICY" HANDS YOU THE DAY YOU SIGNED — through the one seam, as the plate #973 L1
    /// shipped, with his card still up under it.
    ///
    /// <para>Four things have to be true together and only together do they mean anything: the beat went in
    /// under the SIGNING subject (not a ledger page, which is what every other flashback is about), the seam
    /// raised its PLATE rather than a modal (a card here would take the screen off a man who is mid-sentence
    /// and is the presentation a merged guard already pins), its words are in the ledger where they survive
    /// the picture going (#761), and his card is still there to answer.</para>
    ///
    /// <para><b>Proven RED</b> by raising the beat with no subject — the subject assertion names it — and
    /// again by closing his card in <c>TellHimYouAlreadyHaveOne</c>.</para>
    /// </summary>
    [Fact]
    public void ThePolicyLineHandsYouTheDayYouSigned()
    {
        Pages.Map map = AtATable();
        HeReachesTheTable(map);
        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);

        Assert.NotNull(TheCard(map));
        Assert.Equal(1, TimesFiled(map, StoryBeats.Beat.Flashback));

        var plate = (ValueTuple<StoryBeats.Beat, string?, double>?)Field(map, "_storyPlate");
        Assert.NotNull(plate);
        Assert.Equal(StoryBeats.Beat.Flashback, plate!.Value.Item1);
        Assert.Equal(NebulaRep.SigningMemoryId, plate.Value.Item2);

        Assert.Contains(Ledger(map),
                        l => l.Contains(StoryBeats.Caption(StoryBeats.Beat.Flashback), StringComparison.Ordinal));
        Assert.Null(Field(map, "_storyCard"));
        Assert.Null(Field(map, "_deferredBeat"));
    }

    /// <summary>His answer changes once the captain has died — and says nothing whatever about the face he
    /// is looking at, which is the horror.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void HisAnswerToThePolicyLineKnowsHowManyYouHaveBuried(int retired)
    {
        Pages.Map map = AtATable(retired: retired);
        HeReachesTheTable(map);
        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);

        Assert.Equal(NebulaRep.PolicyClaimReply(retired), (string?)Field(map, "_repSaid"));
    }

    /// <summary>
    /// ONCE PER LIFE, AND NOT ONCE EVER. Said twice in one life he answers and no page comes back; said
    /// after a death it comes back, because it is a different man reaching for it.
    ///
    /// <para>The beat's own cadence is <c>EveryTime</c> — L1 chose that for the LEDGER, where the latch is
    /// the page's own <c>Refused</c> state and a rebirth re-greys the book. The rep has no page, so his
    /// latch is <c>_repSigningToldInLife</c>, and this is what says so.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>_repSigningToldInLife</c> check: the second telling in
    /// one life then files a second flashback.</para>
    /// </summary>
    [Fact]
    public void TheSigningComesBackOncePerLife()
    {
        Pages.Map map = AtATable();
        HeReachesTheTable(map);

        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);
        int first = Ledger(map).Count(l => l.Contains("Bleached to the bone", StringComparison.Ordinal));
        Assert.Equal(1, first);

        // Said again in the same life: he answers, and no page comes back.
        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);
        Assert.Equal(1, Ledger(map).Count(l => l.Contains("Bleached to the bone", StringComparison.Ordinal)));
        Assert.False(string.IsNullOrWhiteSpace((string?)Field(map, "_repSaid")));

        // …and a death later, it comes back.
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo
        {
            Id = ThreadId,
            Retired = [new RetiredCaptain("Roake", 12)],
        }]);
        HeReachesTheTable(map);
        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);

        Assert.Equal(2, Ledger(map).Count(l => l.Contains("Bleached to the bone", StringComparison.Ordinal)));
    }

    // ── The withdrawal ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// TOLD NO, HE TAKES THE CARD AWAY AND REMEMBERS IT — for this visit, and the next visit he is
    /// delighted to see you.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>.WithNo()</c> from <c>SendHimToTheBar</c>: he then
    /// approaches again inside the same visit.</para>
    /// </summary>
    [Fact]
    public void ToldNoHeGoesAndDoesNotComeBackThisVisit()
    {
        Pages.Map map = AtATable();
        Set(map, "_repVisitIndex", 4);
        HeReachesTheTable(map);

        Answer(map, NebulaRep.RepMove.NotToday);

        Assert.Null(TheCard(map));
        var memory = (NebulaRepVisit)Field(map, "_repMemory")!;
        Assert.False(memory.MayApproach(4));
        Assert.True(memory.MayApproach(5), "the next docking is a fresh man with your name on the file");
    }

    /// <summary>A Premium captain's way out costs him nothing and remembers nothing: he had nothing to sell,
    /// so there was no refusal to take personally.</summary>
    [Fact]
    public void GoodDayIsNotARefusal()
    {
        Pages.Map map = AtATable(InsuranceTier.Premium);
        Set(map, "_repVisitIndex", 2);
        HeReachesTheTable(map);

        Answer(map, NebulaRep.RepMove.GoodDay);

        Assert.Null(TheCard(map));
        Assert.True(((NebulaRepVisit)Field(map, "_repMemory")!).MayApproach(2));
    }

    // ── The sale ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A SALE SETS THE POLICY THE ONE WAY A POLICY IS SET, books the coin, files the transaction against him
    /// in the contact book, and leaves a receipt in the ledger.
    ///
    /// <para><b>Proven RED</b> by leaving <c>_insurance</c> untouched in <c>BuyFromTheRep</c> — the tier
    /// assertion names it — and again by booking the premium as a <c>CreditKind.Deposit</c>, which the
    /// balance assertion catches (a premium the captain could draw back out of the favor bank).</para>
    /// </summary>
    [Fact]
    public void BuyingLeavesAPolicyAReceiptAndALineInHisBook()
    {
        Pages.Map map = AtATable();
        Set(map, "_credits", 5000);
        HeReachesTheTable(map);

        Answer(map, NebulaRep.RepMove.BuyBasic);

        var policy = (PirateInsurance)Field(map, "_insurance")!;
        Assert.Equal(InsuranceTier.Basic, policy.Tier);
        Assert.Equal(5000 - NebulaRep.BasicPremiumCr, (int)Field(map, "_credits")!);

        var book = (ContactLedger)Field(map, "_contacts")!;
        ContactHistory his = book.For(NebulaRep.ContactId);
        Assert.Equal(NebulaRep.DisplayName, his.DisplayName);
        Assert.Contains(his.Transactions, t => t.Kind == CreditKind.Premium);
        Assert.Equal(0, his.CreditBalance);   // a premium is spent, never parked

        Assert.Contains(Ledger(map), l => l.Contains("NEBULA MUTUAL", StringComparison.Ordinal)
                                          && l.Contains(NebulaRep.RepName, StringComparison.Ordinal));

        // …and he immediately starts on the next tier up, which is the character.
        Assert.Contains(TheCard(map)!.Value.Offers, o => o.Move == NebulaRep.RepMove.BuyPremium);
    }

    /// <summary>A captain who cannot pay buys nothing, and he is not given a new line about it.</summary>
    [Fact]
    public void ACaptainWhoCannotPayBuysNothing()
    {
        Pages.Map map = AtATable();
        Set(map, "_credits", 1);
        HeReachesTheTable(map);

        Answer(map, NebulaRep.RepMove.BuyPremium);

        Assert.Equal(InsuranceTier.None, ((PirateInsurance)Field(map, "_insurance")!).Tier);
        Assert.Equal(1, (int)Field(map, "_credits")!);
        Assert.Null((string?)Field(map, "_repSaid"));
    }

    // ── The bleed ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WRONG NAME, AND THE ONE LINE IT LEAVES. Driven through the shipped path with the bleed forced on,
    /// because the roll is deliberately rare: the card must carry the dead captain's name and the extra
    /// button, and pressing it must put a line in the ship's ledger naming both captains — the note the
    /// black book (#973 L3) will later find.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>LogAutopilotEvent</c> from
    /// <c>TellHimThatIsNotYourName</c>.</para>
    /// </summary>
    [Fact]
    public void TheWrongNameLeavesOneLineInTheShipsLedger()
    {
        Pages.Map map = AtATable(retired: 1);
        HeReachesTheTable(map);

        // Force the rare read of the wrong line: the roll's cadence is Core's business and is swept there.
        string dead = ((GameThreadInfo)((IReadOnlyList<GameThreadInfo>)Field(map, "_threadList")!)[0]).Retired[0].Name;
        Set(map, "_repBleeding", true);
        Set(map, "_repNameOnFile", Captains.CleanName(dead));
        Set(map, "_repCard", NebulaRep.PitchFor(InsuranceTier.None, Captains.CleanName(dead), bleeding: true));

        Assert.Contains(TheCard(map)!.Value.Offers, o => o.Move == NebulaRep.RepMove.ThatsNotMyName);

        Answer(map, NebulaRep.RepMove.ThatsNotMyName);

        Assert.Equal(NebulaRep.BleedApology, (string?)Field(map, "_repSaid"));
        Assert.Contains(Ledger(map), l => l.Contains(Captains.CleanName(dead), StringComparison.Ordinal)
                                          && l.Contains(NebulaRep.BleedApology, StringComparison.Ordinal));

        // …and he does not offer the button twice. There is nothing more to say about it, ever.
        Assert.DoesNotContain(TheCard(map)!.Value.Offers, o => o.Move == NebulaRep.RepMove.ThatsNotMyName);
    }

    // ── He does not sit ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HE STANDS. The whole design turns on it — he was not invited — and the codebase has a hard count of
    /// the ways a sitting can be opened. Asked of the component: after a full pitch, the captain's seat is
    /// still whatever it was and no <c>TableTalk</c> has been handed to anybody.
    ///
    /// <para>The count itself is <c>EverySeatTheCaptainTakesFingerprintsTheSameTests</c>' business and it
    /// still reads seven; this is the behavioural half, because a source count cannot see a page that
    /// mutated a seat it did not construct.</para>
    /// </summary>
    [Fact]
    public void HeNeverTakesAChair()
    {
        Pages.Map map = AtATable();
        HeReachesTheTable(map);
        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);

        Assert.False((bool)typeof(Pages.Map).GetProperty("CaptainIsSeated", Hidden)!.GetValue(map)!);
    }
}
