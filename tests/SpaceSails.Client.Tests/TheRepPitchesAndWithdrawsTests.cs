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
/// <item>"I already have a policy" fires the signing flashback HOSTED on his card — counted, logged, and
/// with no second surface stacked on top of his;</item>
/// <item>it fires once per LIFE, and the next life reads the line the first captain never saw;</item>
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
    /// "I ALREADY HAVE A POLICY" HANDS YOU THE DAY YOU SIGNED — hosted on his card, counted once, and with
    /// NOTHING stacked over it.
    ///
    /// <para>Four things have to be true together and only together do they mean anything: his card is
    /// still up (the canvas exists), the page is set on it (the host is really showing the plate), the beat
    /// is in the seen-set (it was counted), its words are in the ledger (they survive the card closing,
    /// #761) — and <c>_storyCard</c>, <c>_storyPlate</c> and <c>_deferredBeat</c> are all still empty.</para>
    ///
    /// <para><b>Proven RED</b> by putting <c>_storyCard = (beat, subject, outcome);</c> into the seam's
    /// hosted arm, and again by raising the beat before the panel is set.</para>
    /// </summary>
    [Fact]
    public void ThePolicyLineFiresTheSigningFlashbackOnHisOwnCard()
    {
        Pages.Map map = AtATable();
        HeReachesTheTable(map);
        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);

        Assert.NotNull(TheCard(map));
        Assert.Equal(FlashbackMemories.SubjectForLife(FlashbackMemories.Signing, 1),
                     (string?)Field(map, "_repFlashback"));
        Assert.Equal(1, TimesFiled(map, StoryBeats.Beat.Flashback));
        Assert.Contains(Ledger(map), l => l.Contains("chained to it", StringComparison.Ordinal));

        Assert.Null(Field(map, "_storyCard"));
        Assert.Null(Field(map, "_storyPlate"));
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
    /// ONCE PER LIFE, AND NOT ONCE EVER. Saying it twice in one life gets the reply and no second plate;
    /// saying it after a death gets the page back with a line on it the first captain never saw.
    ///
    /// <para><b>Proven RED</b> by dropping the life stamp from the subject
    /// (<c>FlashbackMemories.SubjectForLife</c> → the bare memory id): the reborn captain is then handed
    /// nothing, because the seen-set already has that subject.</para>
    /// </summary>
    [Fact]
    public void TheSigningComesBackOncePerLifeAndDiffersAfterADeath()
    {
        Pages.Map map = AtATable();
        HeReachesTheTable(map);

        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);
        Assert.Equal(1, TimesFiled(map, StoryBeats.Beat.Flashback));

        // Said again in the same life: he answers, and no page comes back.
        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);
        Assert.Equal(1, TimesFiled(map, StoryBeats.Beat.Flashback));
        Assert.Null((string?)Field(map, "_repFlashback"));
        Assert.False(string.IsNullOrWhiteSpace((string?)Field(map, "_repSaid")));

        // …and a death later, the same page, one line longer.
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo
        {
            Id = ThreadId,
            Retired = [new RetiredCaptain("Roake", 12)],
        }]);
        HeReachesTheTable(map);
        Answer(map, NebulaRep.RepMove.AlreadyHaveAPolicy);

        Assert.Equal(2, TimesFiled(map, StoryBeats.Beat.Flashback));
        Assert.Equal(FlashbackMemories.SubjectForLife(FlashbackMemories.Signing, 2),
                     (string?)Field(map, "_repFlashback"));
        Assert.Contains(Ledger(map),
                        l => l.Contains(FlashbackMemories.SigningRebornLine, StringComparison.Ordinal));
    }

    /// <summary>
    /// AND IT IS REFUSED WITH NO CARD UP. His panel is the canvas; a flashback filed as told with nothing on
    /// the screen erases the evidence that #761's law was broken rather than breaking it loudly.
    ///
    /// <para><b>Proven RED</b> by giving <c>TheHostIsUp</c> a <c>_ =&gt; true</c> arm, or by answering
    /// <c>TheRepIsTalkingToYou</c> off "is he on the floor" instead of off the card.</para>
    /// </summary>
    [Fact]
    public void TheFlashbackIsRefusedWhenHisCardIsNotUp()
    {
        Pages.Map map = AtATable();

        Call(map, "RaiseStoryBeat", StoryBeats.Beat.Flashback,
             FlashbackMemories.SubjectForLife(FlashbackMemories.Signing, 1), null);

        Assert.Equal(0, TimesFiled(map, StoryBeats.Beat.Flashback));
        Assert.Contains(Ledger(map), l => l.Contains("⚠ ENGINE", StringComparison.Ordinal));
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
