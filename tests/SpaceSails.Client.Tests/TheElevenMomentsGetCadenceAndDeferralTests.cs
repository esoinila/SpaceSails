using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #664 · ONE REVEAL-CARD SYSTEM, AND THE ELEVEN MOMENTS THAT CAME ACROSS TO IT.
///
/// <para>The fork answered #528 twice on the same day. `main` built <see cref="StoryBeats"/> — a Core cadence
/// law, a card/plate split and a defer-while-in-danger rule — and this branch built the CARD alone, raised by
/// hand out of <c>Map.RevealCard.cs</c> with the text coming from Core and nothing else deciding anything.
/// The reunification merge (#633) kept both on purpose and filed the duplication rather than picking a winner
/// inside a conflict-bearing merge. This is where the winner is picked, and it is the one that can say NO:
/// the owner's sentence was <i>"should happen universally in the game <b>as long as it does not block the
/// playing too much or be too repetitive</b>"</i>, and the second half of it is a cadence and a deferral.</para>
///
/// <para><b>Nothing here reads a hash or an absence.</b> The claims are read off a live
/// <see cref="Pages.Map"/> after the shipping <c>RaiseStoryBeat</c> has run: which card is up, what words are
/// on it, whether the seen-set spent, whether the queue is holding. The source-shape half is small and
/// separate, and says only the things a running component cannot: that the second card's FILE is gone and
/// that each of the eleven beats is raised from the file whose moment it is.</para>
///
/// <para>Each fact names the change that turns it RED in its own remarks.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheElevenMomentsGetCadenceAndDeferralTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    // ── The eleven, and where each one's moment lives ─────────────────────────────────────────────────

    /// <summary>Every moment that came off the deleted reveal card, with the client file that owns the press
    /// or the event it belongs to. The pairing is the point: a beat declared in Core and raised from
    /// <i>somewhere</i> would satisfy #663's scanner and still not be this issue's job done.</summary>
    public static TheoryData<StoryBeats.Beat, string> TheEleven => new()
    {
        { StoryBeats.Beat.ArchivePurged, "Map.Archive.cs" },
        { StoryBeats.Beat.StrangerStandsADrink, "Map.Bond.cs" },
        { StoryBeats.Beat.KaamosShardFound, "Map.Kaamos.cs" },
        { StoryBeats.Beat.KaamosFilingBounced, "Map.Kaamos.cs" },
        { StoryBeats.Beat.NebulaShardFound, "Map.Nebula.cs" },
        { StoryBeats.Beat.OutpostEffectsRead, "Map.Outpost.cs" },
        { StoryBeats.Beat.SecretLabDoorFound, "Map.SecretLab.cs" },
        { StoryBeats.Beat.TheDormantThingWakes, "Map.SecretLab.cs" },
        { StoryBeats.Beat.ShelterIsNotSanctuary, "Map.Surface.RepoBoat.cs" },
        { StoryBeats.Beat.CollectorsSetDown, "Map.Surface.RepoBoat.cs" },
        { StoryBeats.Beat.SealedDoorReleased, "Map.Venting.Doors.cs" },
    };

    /// <summary>A subject that reaches a real plate, for the two beats whose canvas is chosen by one. The ids
    /// come out of the arcs' own pools, never typed in here — a shard id invented for a test is a world that
    /// cannot tell pass from fail.</summary>
    private static string? ASubjectFor(StoryBeats.Beat beat) => beat switch
    {
        StoryBeats.Beat.KaamosShardFound => KaamosLore.AllPlates.First().Key,
        StoryBeats.Beat.NebulaShardFound => NebulaLore.AllPlates.First().Key,
        StoryBeats.Beat.SealedDoorReleased => "DEEP HOLD",
        _ => null,
    };

    private static string? ASecondSubjectFor(StoryBeats.Beat beat) => beat switch
    {
        StoryBeats.Beat.KaamosShardFound => KaamosLore.AllPlates.Skip(1).First().Key,
        StoryBeats.Beat.NebulaShardFound => NebulaLore.AllPlates.Skip(1).First().Key,
        _ => "a second one",
    };

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component with nothing running but the seam — the same bench
    /// <see cref="TheHostedBeatIsCountedOnlyWhenItsHostIsUpTests"/> drives, and the same one piece of
    /// theatre: a <see cref="ComponentBase"/> that was never attached to a renderer throws out of
    /// <c>StateHasChanged</c>, and every raise ends with one.</summary>
    private static Pages.Map ACalmDeck()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has "
                + "moved, and the seam will throw instead of running.");
        pending.SetValue(map, true);

        return map;
    }

    private static object? Call(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{method}` — this guard needs re-reading."))
        .Invoke(map, args);

    private static FieldInfo FieldInfoOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard needs re-reading.");

    private static object? Field(Pages.Map map, string name) => FieldInfoOf(name).GetValue(map);

    /// <summary>The ONE door, called exactly as a feature calls it. Reflection does not fill optional
    /// arguments, so all three are written out.</summary>
    private static void Raise(Pages.Map map, StoryBeats.Beat beat, string? subject, string? outcome = null) =>
        Call(map, "RaiseStoryBeat", beat, subject, outcome);

    /// <summary>What is on the screen, unpacked out of the seam's own tuple.</summary>
    private static (StoryBeats.Beat Beat, string? Subject, string? Outcome)? CardUp(Pages.Map map)
    {
        object? raw = Field(map, "_storyCard");
        if (raw is null)
        {
            return null;
        }

        Type t = raw.GetType();
        return ((StoryBeats.Beat)t.GetField("Item1")!.GetValue(raw)!,
                (string?)t.GetField("Item2")!.GetValue(raw),
                (string?)t.GetField("Item3")!.GetValue(raw));
    }

    /// <summary>Put the Old Ones on the deck — the danger four of the eleven are raised INTO, built out of
    /// the component's own type rather than out of a flag this bench invented.</summary>
    private static void TheOldOnesAreOnHer(Pages.Map map)
    {
        Type reever = typeof(Pages.Map).GetNestedType("Reever", BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException(
                "Map has no nested `Reever` — the danger this bench arranges has moved, and every deferral "
                + "claim below would be made on a calm deck.");

        object list = Field(map, "_reevers")!;
        list.GetType().GetMethod("Add")!.Invoke(list, [Activator.CreateInstance(reever)]);
    }

    private static void TheDeckIsClearAgain(Pages.Map map)
    {
        object list = Field(map, "_reevers")!;
        list.GetType().GetMethod("Clear")!.Invoke(list, null);
    }

    private static bool InDanger(Pages.Map map) => (bool)Call(map, "CaptainIsInDanger")!;

    private static void TheClockRunsOn(Pages.Map map, double seconds) =>
        FieldInfoOf("SimTime").SetValue(map, (double)Field(map, "SimTime")! + seconds);

    // ── The anti-vacuous facts, first ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE BENCH CAN TELL CALM FROM DANGER. Every deferral claim below is a claim about
    /// <c>CaptainIsInDanger()</c>, and a bench where that predicate answered the same thing both ways would
    /// pass all of them on a seam that had stopped deferring at all — this house's fifth named bug class,
    /// exactly.
    ///
    /// <para><b>RED</b> by making <c>TheOldOnesAreOnHer</c> a no-op.</para>
    /// </summary>
    [Fact]
    public void TheBenchCanActuallyTellDangerFromCalm()
    {
        Pages.Map map = ACalmDeck();
        Assert.False(InDanger(map), "the bench's calm deck is not calm — nothing below can prove a deferral.");

        TheOldOnesAreOnHer(map);
        Assert.True(InDanger(map), "the bench cannot arrange danger, so every 'it defers' claim is vacuous.");

        TheDeckIsClearAgain(map);
        Assert.False(InDanger(map), "the danger cannot be cleared again, so 'and then it lands' is vacuous.");
    }

    /// <summary>
    /// AND THE ELEVEN ARE ELEVEN DIFFERENT MOMENTS. A theory that had quietly collapsed onto one beat would
    /// report eleven green rows about one card.
    /// </summary>
    [Fact]
    public void TheElevenAreElevenDistinctBeats()
    {
        List<StoryBeats.Beat> beats = [.. TheEleven.Select(row => (StoryBeats.Beat)row[0]!)];

        Assert.Equal(11, beats.Count);
        Assert.Equal(11, beats.Distinct().Count());
    }

    // ── They arrive, with Core's own words on them ────────────────────────────────────────────────────

    /// <summary>
    /// EVERY ONE OF THE ELEVEN REACHES THE CARD, AND THE WORDS ON IT ARE CORE'S. The old system passed a
    /// title, a picture and a caption in by hand at the call site; the new one passes a beat and a subject,
    /// and the copy is resolved from the same <c>RevealPlate</c> constants that were there before. So this
    /// asserts the resolution, not the presence: an empty title on a card that is up is the honest-degrading
    /// path firing where a real plate was meant to be.
    ///
    /// <para><b>RED</b> by deleting any arm of <c>StoryBeats.PlateOf</c> — the beat still raises, the card is
    /// still up, and its title, art and caption are all the empty string.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheEleven))]
    public void EachMomentLandsOnTheCardWithCoresOwnWords(StoryBeats.Beat beat, string file)
    {
        Assert.False(string.IsNullOrWhiteSpace(file));   // the file is this row's other half; used below

        Pages.Map map = ACalmDeck();
        string? subject = ASubjectFor(beat);
        Raise(map, beat, subject);

        (StoryBeats.Beat Beat, string? Subject, string? Outcome)? card = CardUp(map);
        Assert.True(card is not null, $"{beat} raised nothing at all — the moment is silent.");
        Assert.Equal(beat, card!.Value.Beat);

        Assert.False(string.IsNullOrWhiteSpace(StoryBeats.Title(beat, subject)),
            $"{beat} reaches the card with no title — its plate is not wired into StoryBeats.PlateOf.");
        Assert.False(string.IsNullOrWhiteSpace(StoryBeats.Caption(beat, subject)),
            $"{beat} reaches the card with no caption.");
        Assert.False(string.IsNullOrWhiteSpace(StoryBeats.ArtFile(beat, subject)),
            $"{beat} reaches the card with no painting.");
    }

    // ── …and then the cadence, which is the half the deleted system never had ─────────────────────────

    /// <summary>
    /// A ONCE-EVER MOMENT DOES NOT COME BACK ON A SECOND VISIT. The three that carry this cadence carry it
    /// for one reason each and the same reason in general: the picture, the caption and the arithmetic are
    /// byte-identical every time, so the second showing is a dismissal and nothing else.
    ///
    /// <para><b>RED</b> by moving any of the three to <c>Cadence.EveryTime</c> in
    /// <c>StoryBeats.CadenceOf</c>.</para>
    /// </summary>
    [Theory]
    [InlineData(StoryBeats.Beat.ArchivePurged)]
    [InlineData(StoryBeats.Beat.KaamosFilingBounced)]
    [InlineData(StoryBeats.Beat.ShelterIsNotSanctuary)]
    public void AOnceEverMomentDoesNotComeBackOnASecondVisit(StoryBeats.Beat beat)
    {
        Assert.Equal(StoryBeats.Cadence.OnceEver, StoryBeats.CadenceOf(beat));

        Pages.Map map = ACalmDeck();
        Raise(map, beat, null);
        Assert.NotNull(CardUp(map));

        Call(map, "CloseStoryCard");
        TheClockRunsOn(map, 60 * 60.0);       // an hour later, on a different day's play
        Raise(map, beat, null);

        Assert.True(CardUp(map) is null,
            $"{beat} is once-ever and came back — the owner's 'or be too repetitive' is the half of #528 " +
            "this whole lane exists to keep.");
    }

    /// <summary>
    /// A PER-SUBJECT MOMENT IS SPENT FOR THE THING IT WAS ABOUT AND FRESH FOR THE NEXT ONE. #541 widened the
    /// seen-key for exactly this: a beat about a PLACE or a THING, filed per beat alone, shows one and
    /// silently swallows every other one in the game.
    ///
    /// <para><b>RED</b> by moving any of the five to <c>Cadence.OnceEver</c> — the second subject goes
    /// silent — or to <c>EveryTime</c>, where the first one repeats.</para>
    /// </summary>
    [Theory]
    [InlineData(StoryBeats.Beat.KaamosShardFound)]
    [InlineData(StoryBeats.Beat.NebulaShardFound)]
    [InlineData(StoryBeats.Beat.OutpostEffectsRead)]
    [InlineData(StoryBeats.Beat.SecretLabDoorFound)]
    [InlineData(StoryBeats.Beat.TheDormantThingWakes)]
    public void APerSubjectMomentIsSpentForOneSubjectAndFreshForTheNext(StoryBeats.Beat beat)
    {
        Assert.Equal(StoryBeats.Cadence.OncePerSubject, StoryBeats.CadenceOf(beat));

        string first = ASubjectFor(beat) ?? "the first one";
        string second = ASecondSubjectFor(beat)!;
        Assert.NotEqual(first, second);

        Pages.Map map = ACalmDeck();
        Raise(map, beat, first);
        Assert.NotNull(CardUp(map));

        Call(map, "CloseStoryCard");
        Raise(map, beat, first);
        Assert.True(CardUp(map) is null, $"{beat} told the SAME subject twice.");

        Raise(map, beat, second);
        (StoryBeats.Beat Beat, string? Subject, string? Outcome)? card = CardUp(map);
        Assert.True(card is not null,
            $"{beat} swallowed a fresh subject — one place gets its establishing shot and every other place " +
            "in the game gets nothing, which is the failure #541's widened key was written after.");
        Assert.Equal(second, card!.Value.Subject);
    }

    /// <summary>
    /// A COOLED MOMENT HOLDS ITS TONGUE, AND THEN STOPS HOLDING IT. Both halves in one fact on purpose: a
    /// cooldown that never expires is not a cadence, it is a once-ever with extra words.
    ///
    /// <para><b>RED</b> either way — <c>Cadence.EveryTime</c> makes the first half fail, and a
    /// <c>CooldownSeconds</c> that never runs out makes the second.</para>
    /// </summary>
    [Theory]
    [InlineData(StoryBeats.Beat.StrangerStandsADrink)]
    [InlineData(StoryBeats.Beat.SealedDoorReleased)]
    public void ACooledMomentHoldsItsTongueUntilTheCooldownHasRun(StoryBeats.Beat beat)
    {
        Assert.Equal(StoryBeats.Cadence.Cooled, StoryBeats.CadenceOf(beat));
        double cooldown = StoryBeats.CooldownSeconds(beat);
        Assert.True(cooldown > 0, $"{beat} is Cooled with a zero cooldown, which is EveryTime wearing a hat.");

        Pages.Map map = ACalmDeck();
        Raise(map, beat, ASubjectFor(beat));
        Assert.NotNull(CardUp(map));

        Call(map, "CloseStoryCard");
        TheClockRunsOn(map, cooldown - 1.0);
        Raise(map, beat, ASubjectFor(beat));
        Assert.True(CardUp(map) is null, $"{beat} spoke again one second inside its own cooldown.");

        TheClockRunsOn(map, 2.0);
        Raise(map, beat, ASubjectFor(beat));
        Assert.True(CardUp(map) is not null,
            $"{beat}'s cooldown never expires — a cooled beat that can only ever speak once is a once-ever " +
            "beat that lies about which rule it is keeping.");
    }

    /// <summary>
    /// AND THE ONE THAT KEEPS EVERY TIME, BECAUSE IT IS THE ONLY WARNING. The arrival card's own call site
    /// has said since #528 that after it <i>"the only information in the world is a tracker fan"</i>. A
    /// warning suppressed for being repetitive is not a warning, and this is written down as a guard rather
    /// than left as a comment because it is the one row a later tidying pass would "fix".
    ///
    /// <para><b>RED</b> by giving <c>CollectorsSetDown</c> any other cadence.</para>
    /// </summary>
    [Fact]
    public void TheOnlyWarningThePlayerGetsIsNotRationed()
    {
        Assert.Equal(StoryBeats.Cadence.EveryTime, StoryBeats.CadenceOf(StoryBeats.Beat.CollectorsSetDown));

        Pages.Map map = ACalmDeck();
        Raise(map, StoryBeats.Beat.CollectorsSetDown, "GRUDGE");
        Assert.NotNull(CardUp(map));

        Call(map, "CloseStoryCard");
        Raise(map, StoryBeats.Beat.CollectorsSetDown, "TALLY");
        Assert.True(CardUp(map) is not null,
            "a second boat set down beside the captain and the game said nothing — the excursion after this " +
            "one is played blind on purpose.");
    }

    // ── The deferral, which is the other half the deleted system never had ────────────────────────────

    /// <summary>
    /// A DEFERRABLE MOMENT RAISED MID-FIGHT WAITS — AND LANDS AFTERWARDS WITH ITS ARITHMETIC INTACT. The
    /// wreck lane paid for the first clause once already, when a full-screen tutorial card let a pack kill
    /// the captain behind it. The second clause is #736's, and it is the reason the deferred slot carries an
    /// outcome at all: a card that arrived late without its numbers would be the same bug with a delay on it.
    ///
    /// <para><b>RED</b> three ways — mark <c>ArchivePurged</c> non-deferrable (it lands mid-fight), drop the
    /// deferred queue's serve in <c>AdvanceStoryCards</c> (it never lands), or drop <c>Outcome</c> from
    /// <c>_deferredBeat</c> (it lands with the fiction and none of the facts).</para>
    /// </summary>
    [Fact]
    public void ADeferrableMomentWaitsForTheFightAndBringsItsArithmeticWithIt()
    {
        const string Receipt = "⏻ the column is cold, and the handle is down";

        Pages.Map map = ACalmDeck();
        TheOldOnesAreOnHer(map);

        Assert.True(StoryBeats.DeferrableWhileInDanger(StoryBeats.Beat.ArchivePurged));
        Raise(map, StoryBeats.Beat.ArchivePurged, null, Receipt);

        Assert.True(CardUp(map) is null,
            "a deferrable card took the screen with the Old Ones on the deck — this is the exact shape that " +
            "killed a captain behind a modal once already.");
        Assert.NotNull(Field(map, "_deferredBeat"));

        TheDeckIsClearAgain(map);
        Call(map, "AdvanceStoryCards");

        (StoryBeats.Beat Beat, string? Subject, string? Outcome)? card = CardUp(map);
        Assert.True(card is not null, "the held card never landed — a deferred beat is postponed, not dropped.");
        Assert.Equal(StoryBeats.Beat.ArchivePurged, card!.Value.Beat);
        Assert.Equal(Receipt, card.Value.Outcome);
        Assert.Null(Field(map, "_deferredBeat"));
    }

    /// <summary>
    /// THE FOUR THAT MAY NOT WAIT LAND IN THE MIDDLE OF THE FIGHT. Three of them are raised one statement
    /// after the thing that makes the fight — <c>SpawnReevers</c>, the pack coming out of the hatch, the
    /// collectors already walking — so a deferrable card there does not wait for a calmer moment, it waits
    /// for the fight to be over and then explains a thing that has already finished. Two of them (the shelter
    /// and the arrival) ARE the warning, and a warning read afterwards is a receipt.
    ///
    /// <para><b>RED</b> by taking any of the four out of <c>StoryBeats.DeferrableWhileInDanger</c>'s false
    /// arms — the card goes to the queue and the captain reads "get to the tube — RUN" after the pack is
    /// dead.</para>
    /// </summary>
    [Theory]
    [InlineData(StoryBeats.Beat.TheDormantThingWakes)]
    [InlineData(StoryBeats.Beat.SealedDoorReleased)]
    [InlineData(StoryBeats.Beat.ShelterIsNotSanctuary)]
    [InlineData(StoryBeats.Beat.CollectorsSetDown)]
    public void TheFourThatMayNotWaitLandInTheMiddleOfTheFight(StoryBeats.Beat beat)
    {
        Assert.False(StoryBeats.DeferrableWhileInDanger(beat),
            $"{beat} is marked deferrable, and every one of its callers raises it with the danger already on " +
            "the deck — so it would ALWAYS be held, and always be read too late.");

        Pages.Map map = ACalmDeck();
        TheOldOnesAreOnHer(map);
        Raise(map, beat, ASubjectFor(beat));

        Assert.True(CardUp(map) is not null, $"{beat} was held while the fight it is about was happening.");
        Assert.Null(Field(map, "_deferredBeat"));
    }

    // ── The source shape: the things a running component cannot say ───────────────────────────────────

    private static string ClientRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find src/SpaceSails.Client above {AppContext.BaseDirectory}");
    }

    private static IEnumerable<(string Name, string Text)> ClientSource() =>
        Directory.EnumerateFiles(ClientRoot(), "*.*", SearchOption.AllDirectories)
                 .Where(f => Path.GetExtension(f) is ".cs" or ".razor")
                 .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                          && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                 .Select(f => (Path.GetFileName(f), File.ReadAllText(f)));

    /// <summary>
    /// THE SECOND CARD IS GONE, AND NOT MERELY UNUSED. A dead partial that still compiles is a system
    /// somebody re-adopts in six weeks because it looked like the shorter road — which is precisely how the
    /// fork produced two of these in one day.
    ///
    /// <para><b>RED</b> by leaving one <c>ShowRevealCard(</c> call in <c>src/</c>, or by putting
    /// <c>Map.RevealCard.cs</c> back.</para>
    /// </summary>
    [Fact]
    public void ThereIsOnlyOneRevealCardSystemLeft()
    {
        List<(string Name, string Text)> source = [.. ClientSource()];

        // A sweep that read nothing would pass every claim below.
        Assert.True(source.Count > 100, $"only {source.Count} client source files were read — the sweep is " +
                                        "looking in the wrong place and proves nothing.");
        Assert.Contains(source, f => f.Name == "Map.StoryCards.cs");

        Assert.DoesNotContain(source, f => f.Name == "Map.RevealCard.cs");

        List<string> survivors = [.. source
            .Where(f => f.Text.Contains("ShowRevealCard(", StringComparison.Ordinal)
                     || f.Text.Contains("_revealCard", StringComparison.Ordinal))
            .Select(f => f.Name)];

        Assert.True(survivors.Count == 0,
            "the deleted reveal-card system still has callers, so the game has two answers to #528 again — " +
            $"and only one of them can say no: {string.Join(", ", survivors)}");
    }

    /// <summary>
    /// AND EACH MOMENT IS RAISED WHERE THE MOMENT IS. #663's scanner proves a beat has SOME caller anywhere
    /// in the client, which is the right law for an orphan sweep and one step short of this issue's job: the
    /// eleven were named by the file they lived in, and this is what says they are still there.
    ///
    /// <para><b>RED</b> by moving any one raise to a different partial, or by leaving one behind as a
    /// <c>ShowRevealCard</c> call.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheEleven))]
    public void EachMomentIsRaisedFromTheFileWhoseMomentItIs(StoryBeats.Beat beat, string file)
    {
        (string Name, string Text) owner = ClientSource().FirstOrDefault(f => f.Name == file);
        Assert.True(owner.Text is not null, $"{file} is not in the client any more — this guard needs re-reading.");

        Assert.Contains($"RaiseStoryBeat(StoryBeats.Beat.{beat}", owner.Text, StringComparison.Ordinal);
    }
}
