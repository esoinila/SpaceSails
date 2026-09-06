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
/// #1148 · <b>A BEAT HELD FOR THE GATE IS A BEAT THAT IS STILL OWED.</b>
///
/// <para><b>What went wrong.</b> The UiGate's canaries script <i>open a board, press its way out</i>, and
/// nothing in them quiesces the story seam. In a loaded 13-minute serial run a card's cadence came due while
/// the charge board was open, its <c>.view-object-backdrop</c> went over the board's own <i>Step away</i>, and
/// Playwright waited sixty seconds for a button a modal was standing on. The same class passed alone at the
/// base (35 s) and alone at that head (29 s), and CI's AOT gate passed the whole job — which is the shape of
/// the fault: the gate was measuring the story's timing rather than the board.</para>
///
/// <para><b>The fix, and the trap inside it.</b> A test-only latch in the URL
/// (<see cref="StoryBeats.HoldQueryFlag"/>) holds cards while a canary drives a board. The obvious cheap
/// version of that — <i>while held, don't raise it</i> — is the bug this file exists to make impossible: a
/// beat SWALLOWED is a beat #761's law was broken about, silently, by a flag whose whole purpose was to leave
/// the game alone. So the latch may only DEFER, into the same one-at-a-time queue #865's sit hold and the
/// danger hold already use, and the beat must arrive whole the moment the latch is off, with its cadence
/// spent exactly once.</para>
///
/// <para><b>How it is driven.</b> A bare <see cref="Pages.Map"/> with the framework's own render early-out
/// set (the <c>CoSeatingIsAStripTests</c> bench idiom, so <c>StateHasChanged</c> on an unattached component is
/// a no-op) and the injected <c>NavigationManager</c> handed in by reflection — the latch IS the live address,
/// so handing the page a different address is exactly how the latch is released.</para>
///
/// <para><b>Proven RED</b> three ways, quoted in the PR body: (a) the hold turned into a drop
/// (<c>return;</c> instead of the queue) fails <see cref="AHeldBeatIsDeferredAndNotDropped"/> at the
/// deferred-beat assertion; (b) the queue's own latch check removed from <c>AdvanceStoryCards</c> fails
/// <see cref="AHeldBeatDoesNotSlipOutOfTheQueueWhileTheLatchIsOn"/>; (c) the arm added to
/// <c>RaiseStoryBeat</c> unconditionally (the flag ignored) fails
/// <see cref="WithNoFlagInTheUrlTheSeamIsTheSeamItAlwaysWas"/>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHeldBeatIsStillOwedTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The beat every law here is driven with: a CARD (so the seam's card arms apply at all), and
    /// <see cref="StoryBeats.Cadence.OnceEver"/> (so "spent exactly once" is a question the seen-set can
    /// actually answer — a second raise of an EveryTime beat proves nothing about spending).</summary>
    private const StoryBeats.Beat TheBeat = StoryBeats.Beat.CrewDeputation;

    /// <summary>The premises this file rests on, asked of Core rather than assumed. A day when the deputation
    /// becomes a plate, or stops being once-ever, is a day every law below is measuring something else.</summary>
    [Fact]
    public void TheBenchIsDrivingACardThatSpendsItselfOnce()
    {
        Assert.Equal(StoryBeats.Presentation.Card, StoryBeats.PresentationOf(TheBeat));
        Assert.Equal(StoryBeats.Cadence.OnceEver, StoryBeats.CadenceOf(TheBeat));
    }

    // ── LAW 1 · HELD IS DEFERRED, NEVER DROPPED ───────────────────────────────────────────────────────

    /// <summary>
    /// A card raised while the latch is on takes no screen and spends no cadence — and it is IN THE QUEUE,
    /// which is the whole difference between a hold and a swallow.
    /// </summary>
    [Fact]
    public void AHeldBeatIsDeferredAndNotDropped()
    {
        Pages.Map map = APageAt("/map?scenario=sol&" + StoryBeats.HoldQueryFlag + "=1");

        Raise(map, TheBeat);

        Assert.True(Card(map) is null,
            "a story card took the screen while the gate's latch was on — the board this canary is driving "
            + "is behind it, and the click it is about to make will time out.");
        Assert.True(Deferred(map) is not null,
            "the held beat is not in the queue: it was DROPPED. The latch may only ever defer — a beat the "
            + "flag deletes is #761's law broken by the very thing that was supposed to leave the game alone.");
        Assert.Empty(Spoken(map));
    }

    /// <summary>
    /// …and it stays in the queue frame after frame. <c>AdvanceStoryCards</c> is the seam's own server, called
    /// once a frame from the sim, and a latch the RAISE respects but the QUEUE does not is a latch that holds
    /// a card for exactly one frame.
    /// </summary>
    [Fact]
    public void AHeldBeatDoesNotSlipOutOfTheQueueWhileTheLatchIsOn()
    {
        Pages.Map map = APageAt("/map?" + StoryBeats.HoldQueryFlag + "=1");
        Raise(map, TheBeat);

        for (int frame = 0; frame < 120; frame++)
        {
            Advance(map);
            Assert.True(Card(map) is null,
                $"the held card came up by itself on frame {frame + 1} — the queue is serving beats the "
                + "latch is holding.");
        }

        Assert.True(Deferred(map) is not null, "the beat left the queue without ever being shown.");
        Assert.Empty(Spoken(map));
    }

    // ── LAW 2 · AND IT ARRIVES WHOLE WHEN THE LATCH COMES OFF ─────────────────────────────────────────

    /// <summary>
    /// The latch released, the queue serves the beat it was holding — the SAME beat, with its subject and its
    /// outcome, and its cadence spends exactly once however many frames run afterwards.
    /// </summary>
    [Fact]
    public void AReleasedBeatFiresWholeAndSpendsItsCadenceExactlyOnce()
    {
        Pages.Map map = APageAt("/map?" + StoryBeats.HoldQueryFlag + "=1");
        Raise(map, TheBeat, "the deputation", "-40 cr");
        Assert.True(Card(map) is null, "the latch never held it in the first place.");

        Release(map);
        Advance(map);

        object? card = Card(map);
        Assert.True(card is not null, "the latch came off and the beat the gate was holding never arrived.");
        Assert.Equal(TheBeat, Part(card!, 1));
        Assert.Equal("the deputation", Part(card!, 2));
        Assert.Equal("-40 cr", Part(card!, 3));
        Assert.True(Deferred(map) is null, "the queue still holds the beat it just served.");
        Assert.Single(Spoken(map));

        // …and the books are not written twice. Sixty more frames of the shipping server, and the seen-set
        // is the same one entry it was on the frame the card came up.
        for (int frame = 0; frame < 60; frame++)
        {
            Advance(map);
        }
        Assert.Single(Spoken(map));

        // The card is put away and the same moment asks again: OnceEver means the cadence is gone. A hold
        // that spent the cadence while the beat was still in the queue would have refused it BEFORE it was
        // ever shown, and this is the assertion that tells those two worlds apart.
        Invoke(map, "CloseStoryCard");
        Raise(map, TheBeat, "the deputation");
        Assert.True(Card(map) is null, "a once-ever beat spoke twice.");
        Assert.Single(Spoken(map));
    }

    // ── LAW 3 · WITH NO FLAG, NOTHING MOVED ───────────────────────────────────────────────────────────

    /// <summary>
    /// THE FLAG ABSENT, THE SEAM IS THE SEAM IT ALWAYS WAS. A card raised at an ordinary address takes the
    /// screen on the raise, exactly as it did before this lane — no queue, no frame of delay.
    /// </summary>
    [Fact]
    public void WithNoFlagInTheUrlTheSeamIsTheSeamItAlwaysWas()
    {
        Pages.Map map = APageAt("/map?scenario=sol");

        Raise(map, TheBeat);

        Assert.True(Card(map) is not null,
            "an ordinary boot deferred a story card — the latch is holding beats nobody asked it to hold, "
            + "and every captain's cards are now a frame late.");
        Assert.True(Deferred(map) is null);
        Assert.Single(Spoken(map));
    }

    /// <summary>
    /// …and a page nobody injected a <c>NavigationManager</c> into is not held either. Every bench in this
    /// suite that raises a beat at a bare <c>Map</c> goes through this path, and "I could not read an
    /// address" must read as NOT HELD — the shipping answer — rather than as a throw.
    /// </summary>
    [Fact]
    public void APageWithNoAddressAtAllIsNotHeld()
    {
        var map = new Pages.Map();
        DoNotPaint(map);

        Raise(map, TheBeat);

        Assert.True(Card(map) is not null, "a page with no injected address held its beats.");
    }

    // ── LAW 4 · THE LATCH CAN TELL ITS OWN PASS FROM ITS OWN FAIL ─────────────────────────────────────

    /// <summary>
    /// The key is READ, not merely spotted. A URL that carries the word with a value that is not a yes does
    /// not hold, and neither does one that merely ends in it — a parser that held on the presence of a
    /// substring would hold on <c>?scenario=holdbeats</c> and could never go red for the right reason.
    /// </summary>
    [Theory]
    [InlineData("/map?holdbeats=1", true)]
    [InlineData("/map?holdbeats=true", true)]
    [InlineData("/map?holdbeats=yes", true)]
    [InlineData("/map?scenario=sol&holdbeats=1&dock=red-eye", true)]
    [InlineData("/map?HOLDBEATS=1", true)]
    [InlineData("/map?holdbeats=1#anchor", true)]
    [InlineData("/map?holdbeats=0", false)]
    [InlineData("/map?holdbeats=", false)]
    [InlineData("/map?scenario=holdbeats", false)]
    [InlineData("/map?unholdbeats=1", false)]
    [InlineData("/map?holdbeatsplease=1", false)]
    [InlineData("/map#holdbeats=1", false)]
    [InlineData("/map", false)]
    public void OnlyTheKeyItselfSaidYesHoldsTheBeats(string url, bool held)
    {
        Assert.Equal(held, StoryBeats.HeldIn("http://localhost" + url));

        Pages.Map map = APageAt(url);
        Raise(map, TheBeat);
        Assert.Equal(held, Card(map) is null);
    }

    // ── LAW 5 · AND THE GATE ASKS FOR IT, IN THE WORD THE GAME READS ─────────────────────────────────

    /// <summary>
    /// THE CANARIES THAT DRIVE A BOARD CARRY THE LATCH — and spell it the way Core spells it.
    ///
    /// <para><c>SpaceSails.UiGate</c> deliberately references no project of ours: it drives the PUBLISHED
    /// artifact through a browser, which is the whole of its value, so the key it puts on a URL is a word
    /// TYPED rather than a constant imported. A word typed is a word that can drift, and the drift is
    /// silent in the worst way — the URL still boots, the gate still passes, and the beats are simply not
    /// held any more until the day a card lands on a button again. This is the one place the two spellings
    /// are made to meet.</para>
    ///
    /// <para><b>Proven RED</b> by misspelling the key in one canary (<c>holdbeat=1</c>): the sweep names the
    /// file and the line.</para>
    /// </summary>
    [Fact]
    public void TheBoardDrivingCanariesAskForTheLatchInCoresOwnWord()
    {
        string gate = Path.Combine(RepoRoot(), "tests", "SpaceSails.UiGate");
        string[] canaries = Directory.GetFiles(gate, "*.cs", SearchOption.TopDirectoryOnly);
        Assert.True(canaries.Length >= 10, $"only {canaries.Length} gate file(s) read — the sweep proved nothing.");

        // Every canary that presses something and reads what comes up. Named rather than inferred: a gate
        // that stopped asking for the latch would otherwise leave this bench green and go back to racing a
        // card.
        //
        // The last two joined the roster on EVIDENCE, not on symmetry: this lane's own loaded serial run
        // caught `TheCaptainsIdentRowIsStyledTests` with `<div class="view-object-backdrop"> intercepts
        // pointer events` on the Captain tab, and it passed alone in 27 s — #1148's exact signature, found
        // a second time, in a gate the first pass had not thought to latch. `TheTradeDeskRendersTests` is
        // the same berth and the same press one file over.
        string[] mustHold =
        [
            "BootAndReachabilityTests.cs",          // the three boards, and the one that raced
            "HudCollisionTests.cs",                 // the bar contact's card, driven through its rows
            "PlotPanelFitsTheWindowTests.cs",       // the plan, built through its own buttons
            "TheCaptainsIdentRowIsStyledTests.cs",  // the Captain tab, caught under a backdrop by this lane
            "TheDestinationPanelIsNeverPaintedOverTests.cs",
            "TheTradeDeskRendersTests.cs",          // the same berth, the same press, one file over
            "ThePeekLeavesAWayOutTests.cs",
        ];

        var offences = new List<string>();
        foreach (string path in canaries)
        {
            string src = File.ReadAllText(path);
            string name = Path.GetFileName(path);

            // Anything that looks like the latch has to BE the latch. `holdbeat=1`, `holdbeats=2`,
            // `heldbeats=1` — every near miss reads as an ordinary unknown key and is silently ignored by
            // the boot, which is exactly the failure that cannot be seen from a green gate.
            foreach (string spelling in Spellings(src))
            {
                if (!string.Equals(spelling, StoryBeats.HoldQueryFlag + "=1", StringComparison.Ordinal))
                {
                    offences.Add($"  {name}: asks for `{spelling}`, which the boot reads as nothing. "
                                 + $"Core's own word is `{StoryBeats.HoldQueryFlag}=1`.");
                }
            }

            if (mustHold.Contains(name, StringComparer.Ordinal)
                && !src.Contains(StoryBeats.HoldQueryFlag + "=1", StringComparison.Ordinal))
            {
                offences.Add($"  {name}: opens a panel and presses its way out, and does not hold the story "
                             + "beats — a card whose cadence lands mid-script will stand on the button.");
            }
        }

        Assert.True(offences.Count == 0,
                    "the browser gate and the story seam disagree about the latch:\n" + string.Join("\n", offences));
    }

    /// <summary>Every spelling in a gate file that was MEANT to be the latch — near misses included, which
    /// is the whole point: an exact-match search for the right word can only ever find the files that are
    /// already correct.</summary>
    private static IEnumerable<string> Spellings(string src) =>
        System.Text.RegularExpressions.Regex.Matches(src, @"\b[Hh]old[A-Za-z]*[Bb]eats?[A-Za-z]*=[A-Za-z0-9]*")
            .Select(m => m.Value);

    private static string RepoRoot()
    {
        string? at = AppContext.BaseDirectory;
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at, "tests", "SpaceSails.UiGate")))
            {
                return at;
            }
            at = Path.GetDirectoryName(at);
        }
        throw new DirectoryNotFoundException("Could not find the repository root above the test assembly.");
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A page standing at an address, and nothing else — no world, no renderer. The story seam's
    /// card arms read the queue, the danger roster and the sit beat, and every one of those is empty on a
    /// fresh component, which is exactly the calm scene a canary is driving a board in.</summary>
    private static Pages.Map APageAt(string url)
    {
        var map = new Pages.Map();
        DoNotPaint(map);
        Hand(map, url);
        return map;
    }

    /// <summary>The framework's own render early-out, set so <c>StateHasChanged</c> on a component with no
    /// render handle is a no-op instead of a throw. Named rather than assumed: if the field moves, this
    /// throws here rather than in the middle of a law.</summary>
    private static void DoNotPaint(Pages.Map map)
    {
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has "
                + "moved, and the story seam will throw instead of running.");
        pending.SetValue(map, true);
    }

    /// <summary>Put the page at an address. This is the whole of the latch's state, which is why releasing it
    /// is the same gesture as setting it.</summary>
    private static void Hand(Pages.Map map, string url) =>
        typeof(Pages.Map).GetProperty("Navigation", Hidden)!.SetValue(map, new At(url));

    /// <summary>Take the latch off, the only way there is: the page is somewhere else now.</summary>
    private static void Release(Pages.Map map) => Hand(map, "/map?scenario=sol");

    /// <summary>The seam's one door. Reflection does not fill optional arguments, so all three go in.</summary>
    private static void Raise(Pages.Map map, StoryBeats.Beat beat, string? subject = null, string? outcome = null) =>
        Invoke(map, "RaiseStoryBeat", beat, subject, outcome);

    /// <summary>One frame of the seam's own server — the call the sim makes wherever the ship is.</summary>
    private static void Advance(Pages.Map map) => Invoke(map, "AdvanceStoryCards");

    private static object? Card(Pages.Map map) => Field(map, "_storyCard");

    private static object? Deferred(Pages.Map map) => Field(map, "_deferredBeat");

    /// <summary>The seen-set: what the seam has written down as told. "Spent exactly once" is a count of
    /// this and nothing else.</summary>
    private static System.Collections.ICollection Spoken(Pages.Map map) =>
        (System.Collections.ICollection)Field(map, "_beatsSpoken")!;

    /// <summary>A part of the seam's card, by position. The card is a value tuple and the names it is written
    /// with — <c>(Beat, Subject, Outcome)</c> — are the compiler's, not the runtime's, so the three fields
    /// really are called <c>Item1</c>, <c>Item2</c> and <c>Item3</c> once the box is opened.</summary>
    private static object? Part(object card, int position) =>
        card.GetType().GetField("Item" + position, BindingFlags.Instance | BindingFlags.Public)!.GetValue(card);

    private static object? Field(Pages.Map map, string name) =>
        typeof(Pages.Map).GetField(name, Hidden)!.GetValue(map);

    private static void Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo m = typeof(Pages.Map).GetMethod(method, Hidden)
            ?? throw new InvalidOperationException($"Map has no `{method}` — this bench reads a dead name.");
        try
        {
            m.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>An address, initialised the way the browser initialises the real one.</summary>
    private sealed class At : NavigationManager
    {
        public At(string url) => Initialize("http://localhost/", "http://localhost" + url);
    }
}
