using System;
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
/// #763 · THE KIT IS WORKED FROM THE POCKET, AND EVERY ANSWER LANDS ON ITS OWN SCREEN.
///
/// <para>Two halves, because the wiring is a razor file and a partial class and neither half can be proved
/// by the other. The <b>source-shape</b> facts read <c>Map.razor</c> and <c>Map.Scan.cs</c> in
/// <see cref="TheChitRidesTheCageTests"/>' idiom — that the labels are Core's, that the controls are never
/// disabled, that the press lives inside the kit's own card. The <b>behavioural</b> facts drive a REAL
/// <see cref="Pages.Map"/> on a real hive floor, because "the beat is told once per floor" is a claim about
/// what happens on the second press and a grep cannot see a second press.</para>
///
/// <para>Each test names the exact revert that turns it RED in its own remarks.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheKitIsWorkedFromThePocketTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    // ── THE SOURCE, CUT TO THE PART THAT IS BEING CLAIMED ABOUT ─────────────────────────────────────────

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"no src/SpaceSails.Client above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file) =>
        MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    /// <summary>A subtree of a file, with BOTH markers asserted — a cut that silently missed would make
    /// every claim about it vacuous.</summary>
    private static string Between(string text, string from, string to)
    {
        int a = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(a >= 0, $"the source no longer contains `{from}` — this guard needs re-reading.");
        int b = text.IndexOf(to, a + from.Length, StringComparison.Ordinal);
        Assert.True(b > a, $"the source no longer contains `{to}` after `{from}`.");
        return text[a..b];
    }

    /// <summary>The object card's whole subtree — the one surface the kit answers on.</summary>
    private static string TheObjectCard() =>
        Between(Pages("Map.razor"), "@if (_viewObject is { } vo)", "@* THE CAPTAIN'S SELFIE");

    // ── (a) THE SWITCH ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SCAN SWITCH SAYS CORE'S WORDS, POINTS AT THE VERB, AND IS NEVER DISABLED.
    ///
    /// <para>Never disabled is the load-bearing half (#212/#603): with nothing in reach the kit answers with
    /// the sentence that says so, which is the only way a player can learn that proximity is the skill. A
    /// greyed-out button teaches nothing and is indistinguishable from a bug.</para>
    ///
    /// <para><b>Proven RED</b> by pointing the switch at <c>OpenItemCard</c> — the ordinary look — on
    /// <c>Assert.Contains() Failure: Not found: "() =&gt; ScanWithTheKit(item)"</c>, and RED again by typing
    /// the label into the razor instead of reading <c>SdrScanner.ScanLabel</c>.</para>
    /// </summary>
    [Fact]
    public void TheScanSwitchIsCoresOwnLabelPointedAtTheVerbAndNeverDisabled()
    {
        string row = Between(
            Pages("Map.razor"), "@if (ScanIsOffered(item))", "</div>;");

        Assert.Contains("@SpaceSails.Core.SdrScanner.ScanLabel", row, StringComparison.Ordinal);
        Assert.Contains("() => ScanWithTheKit(item)", row, StringComparison.Ordinal);
        Assert.Contains("@ScanHint(item)", row, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=", row, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE PRESS IS ON THE KIT'S OWN CARD, ONE PER CARRIER, AND NEVER AUTOMATIC.
    ///
    /// <para>The owner's ruling, in markup: <i>"CONNECT is a button the player must press — never automatic,
    /// because crossing from listening to transmitting is exactly the kind of plot-significant choice the
    /// game must put in the player's hand."</i> So it is a control, it is inside the card the sweep is
    /// written on, and there is one of them per thing the kit heard.</para>
    ///
    /// <para><b>Proven RED</b> by moving the block out of the object card into the satchel's own dialog —
    /// the cut asserts the whole thing lives inside <c>@if (_viewObject is { } vo)</c> — and RED again by
    /// dropping the <c>TheKitsCardIsUp</c> gate, which would draw the kit's presses over every other card in
    /// the game.</para>
    /// </summary>
    [Fact]
    public void ThePressIsAControlOnTheKitsOwnCardAndThereIsOnePerCarrier()
    {
        string card = TheObjectCard();

        Assert.Contains("@if (TheKitsCardIsUp)", card, StringComparison.Ordinal);
        Assert.Contains("foreach (SpaceSails.Core.SdrScanner.Hit hit in TheKitSweeps())",
            card, StringComparison.Ordinal);
        Assert.Contains("@SpaceSails.Core.SdrScanner.HitLine(hit)", card, StringComparison.Ordinal);
        Assert.Contains("@SpaceSails.Core.SdrScanner.PressLabel", card, StringComparison.Ordinal);
        Assert.Contains("() => PressTheHit(hit)", card, StringComparison.Ordinal);

        string press = Between(card, "@if (TheKitsCardIsUp)", "@* #774 ·");
        Assert.DoesNotContain("disabled=", press, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE BACK COUNTER IS DRAWN ONLY WHERE A VENUE ADMITS TO HAVING ONE, and it is Core's price and Core's
    /// receipt. Never disabled: an empty purse is answered in the keep's own words.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>TheBackCounterIsOpen</c> gate — every bar in the system
    /// would sell one, and a lead you can buy anywhere is not a lead.</para>
    /// </summary>
    [Fact]
    public void TheBackCounterIsDrawnOnlyWhereThereIsOne()
    {
        string bar = Between(Pages("Map.razor"), "@if (TheBackCounterIsOpen)", "</button>");

        Assert.Contains("@Core.SdrScanner.BuyLabel", bar, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"BuyTheKit\"", bar, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=", bar, StringComparison.Ordinal);
    }

    /// <summary>
    /// #777's LAW, SAID ABOUT A BEAT WITH NO PAINTING: the kit raises NOTHING over its own card.
    ///
    /// <para>The seam's <c>Hosted</c> presentation exists to keep the BOOKS for a beat whose picture is
    /// already on somebody else's card (<see cref="TheHailIsHostedByTheCardItArrivesOnTests"/>). This beat
    /// has no picture — <c>StoryArtPresentTests</c> would demand one — so the honest shape is the per-floor
    /// latch the hall cards already keep, and the discipline that survives from #777 is the one that
    /// matters: <b>never stack a card on a card</b>. Nothing in this feature's own file may reach the story
    /// seam, the reveal card, or the pulse HUD behind the backdrop.</para>
    ///
    /// <para><b>Proven RED</b> by replacing the beat's <c>SayWhereTheyAreLookingAndFile</c> with
    /// <c>RaiseStoryBeat(...)</c> — the normal seam — on
    /// <c>Assert.DoesNotContain() Failure: Sub-string found … "RaiseStoryBeat"</c>.</para>
    /// </summary>
    [Fact]
    public void TheKitNeverRaisesASecondSurfaceOverItsOwnCard()
    {
        string wiring = Pages("Map.Scan.cs");

        foreach (string stacked in new[]
                 {
                     "RaiseStoryBeat", "ShowStoryBeat", "ShowRevealCard", "_storyCard", "_storyPlate",
                     "ShowPulseMessage", "ShowAndFile",
                 })
        {
            Assert.DoesNotContain(stacked, wiring, StringComparison.Ordinal);
        }

        // …and every sentence it does say is Core's, verbatim. A page writing its own prose is the second
        // spelling this repo keeps a table of.
        Assert.Contains("SayWhereTheyAreLookingAndFile(TheBeatHere()", wiring, StringComparison.Ordinal);
        Assert.Contains("SayWhereTheyAreLookingAndFile(pressed.Line", wiring, StringComparison.Ordinal);
        Assert.Contains("SdrScanner.QuietLine", wiring, StringComparison.Ordinal);
        Assert.Contains("SdrScanner.NothingHeardLine", wiring, StringComparison.Ordinal);
        Assert.Contains("SdrScanner.HitLine(heard[i])", wiring, StringComparison.Ordinal);
    }

    // ── THE COMPONENT, DRIVEN ───────────────────────────────────────────────────────────────────────────

    private static int AFloorWithSomethingOnTheAir(int skip = 0) =>
        UndergroundComplex.FloorsOf(Body).Where(l => l < 0).Skip(skip).First();

    private static Pages.Map OnTheFloor(int floor)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved, and the kit's verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, floor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_satchel", new List<Satchel.Item> { SdrScanner.TheKit });

        // Standing a pace out of the cage, which is where a captain steps out of the car.
        UndergroundComplex.Shaft cage = UndergroundComplex.ShaftsOn(MoonSurface.ExpeditionField())
            .First(c => c.Kind == UndergroundComplex.ShaftKind.Cage);
        Set(map, "_avatarX", cage.Landing.X);
        Set(map, "_avatarY", cage.Landing.Y);

        return map;
    }

    private static void MoveTo(Pages.Map map, int floor)
    {
        object ex = Get(map, "_surface")!;
        ex.GetType().GetProperty("Floor")!.SetValue(ex, floor);
    }

    private static object? Get(object o, string member) =>
        o.GetType().GetField(member, Hidden) is { } f
            ? f.GetValue(o)
            : (o.GetType().GetProperty(member, Hidden)
               ?? throw new InvalidOperationException($"the component has no `{member}`.")).GetValue(o);

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"the component has no `{field}` field."))
        .SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"the component has no `{method}` method."))
        .Invoke(o, args);

    /// <summary>The row printer is static — it rebuilds the prose from the world and holds nothing.</summary>
    private static string RowFor(Satchel.Item item) =>
        (string)(typeof(Pages.Map).GetMethod("SatchelLabel", Hidden | BindingFlags.Static)
                 ?? throw new InvalidOperationException("Map has no `SatchelLabel` — this guard needs re-reading."))
            .Invoke(null, [item])!;

    private static void Scan(Pages.Map map) => Invoke(map, "ScanWithTheKit", SdrScanner.TheKit);

    private static DeckPlan.ConsoleSpot? TheCard(Pages.Map map) =>
        (DeckPlan.ConsoleSpot?)Get(map, "_viewObject");

    private static IReadOnlyList<FieldNote> Book(Pages.Map map) =>
        (IReadOnlyList<FieldNote>)Get(map, "_fieldNotes")!;

    // ── (d) THE BEAT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SWEEP LANDS ON THE KIT'S OWN CARD, AND THE BEAT IS TOLD ONCE PER FLOOR.
    ///
    /// <para>Four things at once, and only all four together mean anything: the card that comes up is the
    /// KIT'S (so the sweep has a surface), it carries the hit lines Core composed (so the sweep is what was
    /// heard), the beat is on it the first time (#761 — told where the captain is looking), and it is NOT on
    /// it the second time on the same floor (so it is a beat and not wallpaper).</para>
    ///
    /// <para><b>Proven RED</b> both ways. Deleting the <c>TheBeatIsAlreadyInTheBook()</c> clause reddens the
    /// second half — <c>the beat was told twice on one floor</c>; making <c>TheBeatIsAlreadyInTheBook</c>
    /// return <c>true</c> unconditionally reddens the first — <c>the kit heard something and said nothing
    /// about it</c>.</para>
    /// </summary>
    [Fact]
    public void TheBeatIsToldOnceOnTheKitsOwnCardAndAgainOnTheNextFloor()
    {
        int first = AFloorWithSomethingOnTheAir();
        Pages.Map map = OnTheFloor(first);

        Scan(map);

        DeckPlan.ConsoleSpot card = TheCard(map)
            ?? throw new InvalidOperationException("SCAN raised no card at all.");
        Assert.Equal(SdrScanner.CardLabel, card.Label);

        IReadOnlyList<SdrScanner.Hit> heard = (IReadOnlyList<SdrScanner.Hit>)
            Invoke(map, "TheKitSweeps")!;
        Assert.NotEmpty(heard);
        foreach (SdrScanner.Hit hit in heard)
        {
            Assert.Contains(SdrScanner.HitLine(hit), card.Caption!, StringComparison.Ordinal);
        }

        Assert.Contains(SdrScanner.BeatLine, card.Outcome ?? "", StringComparison.Ordinal);
        Assert.Contains(Book(map), n => n.Text.Contains(SdrScanner.BeatLine, StringComparison.Ordinal));

        // …and again, on the same floor. The kit still answers; the beat does not repeat.
        Scan(map);
        DeckPlan.ConsoleSpot again = TheCard(map)!.Value;
        Assert.Equal(SdrScanner.CardLabel, again.Label);
        Assert.DoesNotContain(SdrScanner.BeatLine, again.Outcome ?? "", StringComparison.Ordinal);

        // …and on a DIFFERENT floor it is news again, because "there is something on the air here" is a fact
        // about a floor.
        MoveTo(map, AFloorWithSomethingOnTheAir(skip: 1));
        Scan(map);
        Assert.Contains(SdrScanner.BeatLine, TheCard(map)!.Value.Outcome ?? "",
            StringComparison.Ordinal);
    }

    /// <summary>
    /// THE PRESS ANSWERS ON THE CARD THE PLAYER IS LOOKING AT, IN CORE'S OWN WORDS, AND THE BOOK KEEPS IT.
    ///
    /// <para>#736's law, at the newest pop-up in the game: a line sent to the HUD from inside a modal is in
    /// the DOM and not on the screen. And #715's: a refusal is a signature in somebody's log, so the sentence
    /// that earned it is written down.</para>
    ///
    /// <para><b>Proven RED</b> by routing <c>PressTheHit</c> through <c>ShowPulseMessage</c> instead — the
    /// answer disappears behind the card's own backdrop and the Outcome comes back empty.</para>
    /// </summary>
    [Fact]
    public void ThePressIsAnsweredOnTheCardAndFiledInTheBook()
    {
        Pages.Map map = OnTheFloor(AFloorWithSomethingOnTheAir());
        Scan(map);

        SdrScanner.Hit hit = ((IReadOnlyList<SdrScanner.Hit>)Invoke(map, "TheKitSweeps")!)[0];
        SdrScanner.Pressed expected = SdrScanner.Press(Body, AFloorWithSomethingOnTheAir(), hit);
        Assert.False(expected.Worked, $"{Body} is not a listed operator's site — pick another rock.");

        Invoke(map, "PressTheHit", hit);

        Assert.Contains(expected.Line, TheCard(map)!.Value.Outcome ?? "", StringComparison.Ordinal);
        Assert.Contains(Book(map), n => string.Equals(n.Text, expected.Line, StringComparison.Ordinal));
    }

    /// <summary>
    /// THE SATCHEL KNOWS WHAT IT IS CARRYING. A new <see cref="Satchel.Kind"/> that nobody taught the row
    /// printer falls through to the default arm and prints a receiver as a file on somebody — this repo's
    /// third named bug class, in a row of a list.
    ///
    /// <para><b>Proven RED</b> by deleting the <c>Kind.Tool</c> arm from <c>SatchelLabel</c>:
    /// <c>Assert.Equal() Failure … Expected: "📻 SDR SCANNER" / Actual: "🗃 a file on somebody"</c>.</para>
    /// </summary>
    [Fact]
    public void EveryKindTheSatchelCanHoldHasARowOfItsOwn()
    {
        Assert.Equal(SdrScanner.ItemName, RowFor(SdrScanner.TheKit));

        // …and the fallback is a TOOL's fallback and not a document's, for a tool this build has never heard
        // of — an edited save, or a later build's kit.
        string strange = RowFor(new Satchel.Item(Satchel.Kind.Tool, "something-a-later-build-carries"));
        Assert.DoesNotContain("file on somebody", strange, StringComparison.Ordinal);
    }
}
