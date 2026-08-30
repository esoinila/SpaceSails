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
/// #1016 · <b>WORKING THE CASE IS NOT A THING YOU DO ON A MOON.</b>
///
/// <para>Owner, 2026-08-30, sitting at a takeable top in The Stormwatch Bar aboard The Red Eye — the eighth
/// seat #973 L5b had just built — and pressing the strip's own <b>Work the case</b> button: <i>"I do not see
/// the detective book here when I work the case? Some kind of bug?"</i> Then the ruling: <i>"Maybe it might
/// be good idea to refactor the working the case etc table options to not be tied to any location? Kind of
/// clean separation from the arriving random encounters that are more place tied events."</i></para>
///
/// <h3>What was actually wrong, and why it was four bugs and not one</h3>
///
/// <para>Every organ of the dig had quietly been written as a fact about a <c>SurfaceExcursion</c>, back when
/// every seat in the game was on one. A docked berth has none, so at that top:</para>
/// <list type="number">
/// <item><c>OpenTheSpread</c> returned on its first line — the button was live and <b>dead and silent</b>,
/// which is #603's founding sin with the lid screwed down.</item>
/// <item>the write-up register lived on the excursion, so every reader answered <i>no</i> and every writer
/// dropped its write on the floor;</item>
/// <item>the darkroom hold lived there too, and was stepped only out of <c>StepSurface</c>, which returns on
/// its first line off an excursion — so the bar could never fill;</item>
/// <item><c>FileNoteAbout</c> needed a body and a site to name the place, so the entry a dig had just spent
/// twenty seconds on would have been dropped even if the other three had worked.</item>
/// </list>
///
/// <h3>What these guards hold</h3>
///
/// <para>All of it is driven, not read: a real page, clamped at the owner's own berth, standing in the real
/// bar, sat down through the SHIPPING <c>[E]</c> verb, with the shipping clock stepped by the shipping
/// stepper. The one source-shape assertion in the file is the frame wiring, which cannot be driven here for
/// the reason <c>TheDockedBarIsAWalkableRoomTests.TheWalkedFrameStepsTheBar</c> already writes down (the
/// walked frame paints to a renderer this bench has none of) — and it names its own revert.</para>
///
/// <para><b>Proven RED.</b> Each test below names, in its own summary, the line to put back to watch it
/// fail; all four were run against the reverted lines before they were committed.</para>
///
/// <para><b>The other half of the ruling is not tested here because it is not touched:</b> the arrival rolls,
/// the walkers, the approach, the watch and the room's own encounters stay exactly as place-tied as they
/// were. <c>TheDockedBarIsAWalkableRoomTests</c> and <c>TheEighthSeatIsInTheDockedBarTests</c> are the guards
/// on that half and neither of them moved.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCaseIsNotTiedToAPlaceTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The classy great-port tier, and the one the owner filed this from.</summary>
    private const string TheRedEye = "red-eye";

    private const string ThreadId = "b71f4a0c39d24e5ba8027c6f1d3e5490";

    /// <summary>One of the two demo papers the spread cheat seeds — an ordinary sleeve item with an ordinary
    /// gist, so this file invents no content of its own.</summary>
    private static readonly Satchel.Item APaper = new(Satchel.Kind.Paper, "spread-demo-1");

    // ── 0 · THE WORLD THIS FILE HANDS THE GUARDS CAN TELL PASS FROM FAIL ──────────────────────────────

    /// <summary>
    /// THE BENCH IS THE BUG'S OWN WORLD: seated, private, and with <b>no excursion under it</b>.
    ///
    /// <para>The fifth named bug class, aimed straight at this file: every assertion below is about what
    /// happens when <c>_surface</c> is null, and a bench that had quietly acquired an excursion — or had
    /// never actually sat the captain down — would pass all of them while proving nothing about the room the
    /// owner was in. So the three facts the rest of the file rests on are asserted first, off the page's own
    /// members.</para>
    /// </summary>
    [Fact]
    public void THE_BENCH_IsASeatWithNoGroundUnderIt()
    {
        Pages.Map map = SatAtATopInTheBar();

        Assert.Null(Field(map, "_surface"));
        Assert.NotNull(Field(map, "_dockedHavenId"));
        Assert.True((bool)Field(map, "_ashore")!, "the bench never got the captain past the tube.");

        // Seated, and in a seat the spread is allowed at — both asked of the shipping members, because
        // "sat down" and "may lay the papers out" are two different questions and this file needs both.
        Assert.NotNull(Read(map, "SeatedIn"));
        Assert.True((bool)Read(map, "CaptainIsSeatedAnywhere")!);
        Assert.Null(Read(map, "SpreadRefusal"));

        // …and the sleeve has something with a gist in it, or the spread would be an empty page for an
        // honest reason and every row assertion below would be vacuous.
        Assert.Contains(Sleeve(map), i => i.Kind == Satchel.Kind.Paper);
        Assert.True((bool)Invoke(map, "CanWriteUp", APaper)!);
    }

    // ── (a) THE BUTTON THE OWNER PRESSED ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>WORK THE CASE OPENS THE CASE.</b> The press the owner made, at the seat he made it in.
    ///
    /// <para><b>Proven RED</b> by putting <c>if (_surface is null) { return; }</c> back at the top of
    /// <c>OpenTheSpread</c> — the line as it shipped: the satchel stays shut, the page never changes, and
    /// nothing at all is said, which is exactly what the owner saw.</para>
    ///
    /// <para>And the gate that IS there is asked in the other direction too: stand the same captain up and
    /// the page refuses OUT LOUD rather than silently, because the posture law is the one a player learns by
    /// pressing it once (#784/#603). A build that had simply deleted the gate would pass the first half of
    /// this test and fail the second.</para>
    /// </summary>
    [Fact]
    public void THE_SPREAD_OpensAtATopInADockedBar()
    {
        Pages.Map map = SatAtATopInTheBar();

        Invoke(map, "OpenTheSpread");

        Assert.True((bool)Field(map, "_showSatchel")!,
            "\"Work the case\" at a docked bar top opened nothing — the owner's own bug (#1016).");
        Assert.Equal("Spread", Field(map, "_satchelPage")!.ToString());

        // The page it opened onto has rows on it. A spread that opened onto an unexplained empty list is
        // #603's founding sin with a lid on it, and it is the shape a half-fix would leave behind.
        var rows = (List<Satchel.Item>)Invoke(map, "SpreadableFinds")!;
        Assert.NotEmpty(rows);

        // …and nothing was invented: still no excursion under any of it.
        Assert.Null(Field(map, "_surface"));
    }

    /// <summary>The other direction: the gate that remains is POSTURE, and it refuses out loud.</summary>
    [Fact]
    public void THE_SPREAD_StillRefusesOnYourFeetAndSaysSo()
    {
        Pages.Map map = SatAtATopInTheBar();
        Invoke(map, "StandUpFromTable");

        Assert.Null(Read(map, "SeatedIn"));
        Assert.NotNull(Read(map, "SpreadRefusal"));

        Invoke(map, "OpenTheSpread");

        // It still OPENS — the refusal is a sentence on the page, never a dead control (#603/#212) — and the
        // sentence is the one the seat family composes.
        Assert.True((bool)Field(map, "_showSatchel")!);
        Assert.Equal(Read(map, "SpreadRefusal"), Field(map, "_satchelOutcome"));
    }

    // ── (b) THE DIG ITSELF ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>A DIG AT A DOCKED BAR TOP TAKES ITS SECONDS, THEN THE BOOK TAKES THE ENTRY.</b>
    ///
    /// <para>The owner's law about this act, from the issue it was built in (#784 phase two): <i>"The
    /// inventory processing should have like a timer progress bar like the digging has… we are digging for
    /// info and understanding."</i> So the guard is deliberately written to fail on an INSTANT write as well
    /// as on a dead one: half a document's worth of stepping must leave the book empty and the bar half
    /// full. A build that quietly filed on the press would pass every "the entry lands" assertion and be
    /// exactly the shortcut this issue was told not to take.</para>
    ///
    /// <para>What it holds, in order: the hold exists off an excursion; the strip has a fraction to draw;
    /// nothing is filed and nothing is spent until the far end; the entry lands in the ONE book, filed under
    /// the berth's own name; the sheet is still in the sleeve afterwards (which is the whole difference
    /// between a table and a photograph); and the register remembers it, so the row's verb changes.</para>
    ///
    /// <para><b>Proven RED</b> three ways: putting <c>if (_surface is not { } ex) { return; }</c> back at the
    /// top of <c>WriteItUp</c> (no hold at all); reverting <c>StepProcessing</c> to
    /// <c>_surface is not { Processing: { } hold }</c> (the hold starts and the clock never moves); and
    /// putting the <c>_surface is not { } ex</c> clause back in <c>FileNoteAbout</c> (the bar fills, the line
    /// is said, and the book stays empty — which is the sharpest of the three, because it looks like it
    /// worked).</para>
    /// </summary>
    [Fact]
    public void THE_DIG_TakesItsSecondsAndTheBookTakesTheEntryWithNoGroundUnderIt()
    {
        Pages.Map map = SatAtATopInTheBar();

        Invoke(map, "WriteItUp", APaper);

        Assert.NotNull(Field(map, "_processing"));
        Assert.NotNull(Invoke(map, "ProcessingUnderway"));
        Assert.Equal(0.0, (double)Invoke(map, "ProcessingFraction")!, 3);

        // Nothing is spent on the press. An interruption has to have nothing to undo (#696).
        Assert.Empty(Book(map));
        Assert.Empty(Register(map));

        // HALF WAY. The bar has moved and the book has not — this is the assertion that fails on an instant
        // write, which is the failure mode this issue's brief forbids by name.
        Invoke(map, "StepProcessing", Processing.SecondsPerDocument / 2);
        Assert.NotNull(Field(map, "_processing"));
        Assert.Empty(Book(map));
        double half = (double)Invoke(map, "ProcessingFraction")!;
        Assert.True(half is > 0.3 and < 0.7,
            $"half a document in, the strip's bar reads {half:F2} — the clock is not the document's clock.");

        // …and the rest of it.
        Invoke(map, "StepProcessing", Processing.SecondsPerDocument);
        Assert.Null(Field(map, "_processing"));

        // THE ENTRY, in the one book, filed under the room the captain is actually sitting in.
        FieldNote wrote = Assert.Single(Book(map));
        Assert.Equal(SeatedPosture.WriteGlyph, wrote.Glyph);
        Assert.Contains("The Red Eye", wrote.Place, StringComparison.Ordinal);
        Assert.Contains(HavenInterior.BarNameOf(TheRedEye)!, wrote.Place, StringComparison.Ordinal);

        // …and it says where the captain was standing in Fable's own words, rather than announcing a moon
        // to somebody in a station bar (#562's class: the prose reporting a world the sim is not in).
        Assert.Contains("on the haven's deck", wrote.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("regolith", wrote.Text, StringComparison.Ordinal);

        // THE SHEET IS STILL YOURS. What a table buys is not a better fact (#784).
        Assert.Contains(Sleeve(map), i => i.Kind == APaper.Kind && i.Id == APaper.Id);

        // AND THE REGISTER REMEMBERS, which is what makes the row's own verb change under the captain.
        Assert.Contains($"{APaper.Kind}:{APaper.Id}", Register(map));
        Assert.False((bool)Invoke(map, "CanWriteUp", APaper)!);
        Assert.True((bool)Read(map, "CaseHasBegun")!);
        Assert.Equal(SeatedSpread.SpreadAgainLabel, Read(map, "SpreadDoorLabel"));
    }

    /// <summary>
    /// …AND STANDING UP MID-DIG STILL ENDS THE DIG, at a seat with no ground under it.
    ///
    /// <para>#696's promise is that an interruption loses nothing, and #784 wired standing up to it. That
    /// wiring read <c>_host.Surface is { Processing.Work: Write }</c> — a question about a MOON — so at the
    /// eighth seat a captain who stood up mid-sheet walked away from a clock the seat family could not see:
    /// the hold survived the sitting that licensed it, and the next frame in that berth went on filling a bar
    /// for a chair nobody was in.</para>
    ///
    /// <para><b>Proven RED</b> by reverting <c>CloseTable</c>'s gate to the surface read: the hold is still
    /// there after the stand-up, and the book gains the entry twenty seconds later.</para>
    ///
    /// <para>The abandon's own SENTENCE is deliberately not asserted here. <c>StandUpFromTable</c> pulses
    /// <see cref="SeatedPosture.StoodUpToWalkLine"/> immediately after <c>CloseTable</c>, so the abandon line
    /// loses the one slot — which is shipped behaviour at every seat in the game, on a surface exactly as in
    /// a berth, and not something this issue touched. A guard that pinned it here would be pinning a
    /// pre-existing ordering to this lane.</para>
    /// </summary>
    [Fact]
    public void STANDING_UP_MidDigEndsTheHoldInABerthToo()
    {
        Pages.Map map = SatAtATopInTheBar();
        Invoke(map, "WriteItUp", APaper);
        Invoke(map, "StepProcessing", Processing.SecondsPerDocument / 4);
        Assert.NotNull(Field(map, "_processing"));

        Invoke(map, "StandUpFromTable");

        Assert.Null(Field(map, "_processing"));
        Assert.Null(Read(map, "SeatedIn"));
        Assert.Empty(Book(map));
        Assert.Empty(Register(map));

        // …and the sheet is untouched, which is the whole of "an interruption loses nothing".
        Assert.Contains(Sleeve(map), i => i.Kind == APaper.Kind && i.Id == APaper.Id);

        // A frame later there is still nothing to finish — the clock did not survive the chair.
        Invoke(map, "StepProcessing", Processing.SecondsPerDocument);
        Assert.Empty(Book(map));
    }

    // ── (c) THE REGISTER IS THE CASE'S, AND IT RIDES THE VAULT ───────────────────────────────────────

    /// <summary>
    /// <b>A SHEET DUG ONCE IS DUG FOR GOOD.</b> The register round-trips through the real serializer.
    ///
    /// <para>This is the half of the ruling with a semantics change in it, and it is intended: the register
    /// was excursion-scoped, which made "have I dug this sheet" a fact about a WALK — fly home with the paper
    /// still in the sleeve and the pen offered to write a page the book already held. It belongs to the case
    /// now, so it belongs in the file beside the satchel and the book.</para>
    ///
    /// <para>Driven through <see cref="VaultSerializer"/> itself rather than pinned in source, because the
    /// way a section is lost is not a missing field — it is a missing <c>AddSection</c> or a missing
    /// <c>Harvest</c>, and both of those leave a perfectly plausible-looking record type behind.</para>
    ///
    /// <para><b>Proven RED</b> by deleting <c>AddSection(sections, SecWorkedUp, vault.WorkedUp);</c> from
    /// <c>VaultSerializer.Save</c>: the record survives, the file does not, and the captain wakes up being
    /// offered every sheet in the sleeve again.</para>
    /// </summary>
    [Fact]
    public void THE_REGISTER_SurvivesASaveAndALoad()
    {
        string[] worked = ["Paper:spread-demo-1", "Dirt:spread-demo-3"];

        var before = new Vault
        {
            SavedSimTime = 1234.0,
            WorkedUp = new WorkedUpSection { Sheets = worked },
        };

        Vault after = VaultSerializer.Load(VaultSerializer.Save(before));

        Assert.NotNull(after.WorkedUp);
        Assert.Equal(worked.OrderBy(s => s, StringComparer.Ordinal),
                     after.WorkedUp!.Sheets.OrderBy(s => s, StringComparer.Ordinal));

        // …and a file written before this section simply has none, which is the truth about a case nobody
        // had worked — never a crash and never a default that claims something was dug.
        Assert.Null(VaultSerializer.Load(VaultSerializer.Save(new Vault { SavedSimTime = 1.0 })).WorkedUp);
    }

    /// <summary>…and the page writes it and reads it back. The two ends the round-trip above cannot see: the
    /// section is built from the live set on save and poured back into it on load.
    ///
    /// <para>Source-shape for the reason <c>TheEighthSeatIsInTheDockedBarTests</c> already writes down — the
    /// whole of <c>BuildVault</c>/<c>ApplyVault</c> cannot run on a bench — and narrow enough that the only
    /// way to satisfy it is to actually wire the field.</para>
    ///
    /// <para><b>Proven RED</b> by deleting either line.</para></summary>
    [Fact]
    public void THE_REGISTER_IsWrittenAndReadByThePage()
    {
        string vault = Pages("Map.Vault.cs");

        Assert.Contains("WorkedUp = _workedUp.Count > 0", vault, StringComparison.Ordinal);
        Assert.Contains("vault.WorkedUp?.Sheets ?? []", vault, StringComparison.Ordinal);

        // A load with no section in it must EMPTY the set, not leave the last voyage's case in it — the same
        // contract `_satchel = [];` two members down has.
        Assert.Contains("_workedUp.Clear();", vault, StringComparison.Ordinal);
    }

    // ── (d) THE FRAME THE BERTH ACTUALLY RUNS ────────────────────────────────────────────────────────

    /// <summary>
    /// …AND THE WALKED FRAME STEPS THE HOLD when there is no surface clock to step it.
    ///
    /// <para>Everything above drives <c>StepProcessing</c> by hand, which proves the hold works off an
    /// excursion and proves nothing about whether the game ever asks it to. It cannot be driven the other way
    /// here — the walked frame paints to a renderer this bench has none of, which is the horizon
    /// <c>TheDockedBarIsAWalkableRoomTests.TheWalkedFrameStepsTheBar</c> measured one issue ago — so the
    /// wiring is read out of the source, once, beside the sit beat that was split ashore for the identical
    /// reason in #973 L5b.</para>
    ///
    /// <para>ONLY where the surface clock cannot reach, and that half is pinned too: stepped inside the
    /// <c>_surface is null</c> branch, so no tick is ever charged twice.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the call from <c>TheWalkedViewOwnsThisFrame</c> — and again by
    /// hoisting it out of the <c>_surface is null</c> branch, which makes a surface dig run at double
    /// speed.</para>
    /// </summary>
    [Fact]
    public void THE_WALKED_FRAME_StepsTheDigWhereTheSurfaceClockCannotReach()
    {
        string tick = Pages("Map.Sim.Tick.cs");
        int walked = tick.IndexOf("private bool TheWalkedViewOwnsThisFrame", StringComparison.Ordinal);
        Assert.True(walked >= 0, "Map.Sim.Tick.cs no longer has a walked frame — this guard reads a dead name.");

        string body = tick[walked..];
        int branch = body.IndexOf("if (_surface is null)", StringComparison.Ordinal);
        Assert.True(branch > 0, "the walked frame no longer has a no-excursion branch to step the hold in.");
        int closes = body.IndexOf("\n            }", branch, StringComparison.Ordinal);
        Assert.True(closes > branch, "that branch no longer closes where this guard can see it.");

        string offExcursion = body[branch..closes];
        Assert.Contains("StepProcessing(dtRealSeconds);", offExcursion, StringComparison.Ordinal);

        // …and NOWHERE ELSE in the walked frame, so a berth's tick and the surface tick can never both
        // charge one hold on one frame.
        Assert.DoesNotContain("StepProcessing(", body[..branch], StringComparison.Ordinal);
        Assert.DoesNotContain("StepProcessing(", body[closes..], StringComparison.Ordinal);

        // The surface side is untouched and stays after the tank: a hold out on the regolith must never
        // finish a frame the suit was not charged for (#696, guarded next door in
        // ProcessingTheLootTakesTimeTests — asserted here too because THIS change is what could move it).
        string frame = Pages("Map.Surface.Frame.cs");
        int air = frame.IndexOf("StepSuitAir(dtRealSeconds);", StringComparison.Ordinal);
        int hold = frame.IndexOf("StepProcessing(dtRealSeconds);", StringComparison.Ordinal);
        Assert.True(air >= 0 && hold > air);
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A page clamped onto The Red Eye, standing in its bar, <b>sat down at one of its tops</b>, with three
    /// finds in the sleeve — the owner's own seat.
    ///
    /// <para>Built out of the shipping verbs at every step: the deck is <c>SetDeckForDock</c>'s, the walk is
    /// <c>StandAtTheBarThreshold</c>'s (#428's own cheat), and the sitting is <c>TryTakeBarTop</c> — the very
    /// handler [E] reaches (#973 L5b). A bench that assembled its own <c>TableTalk</c> would be testing a
    /// seat that does not ship, which is this repo's first named bug class wearing a test.</para>
    ///
    /// <para>The papers are <c>SeedTheSpreadFinds</c>'s, the same three the <c>?spread=1</c> row puts in the
    /// sleeve, so this file invents no content and no ids of its own.</para></summary>
    private static Pages.Map SatAtATopInTheBar()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        // The real Sol ephemeris, because the BOOK names the place off the berth's own body
        // (`DockedStationName`) — a bench with no bodies in it would file every entry under "ashore" and the
        // place assertion below would be agreeing with a hole rather than with the room.
        Set(map, "_ephemeris", CircularOrbitEphemeris.FromScenario(Sol.Value));
        Set(map, "_dockedHavenId", TheRedEye);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Invoke(map, "SetDeckForDock", TheRedEye);
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!,
            $"{TheRedEye} has no walkable bar to stand in — this bench has no room for its subject.");

        Invoke(map, "SeedTheSpreadFinds");

        // Walk to a top and press [E], through the room's own list and the game's own verb.
        HavenInterior.BarFloor bar = HavenInterior.BarBand(TheRedEye)!.Value;
        foreach (DeckReachability.Point top in bar.Tops)
        {
            Set(map, "_avatarX", top.X);
            Set(map, "_avatarY", top.Y);
            if ((bool)Invoke(map, "TryTakeBarTop")! && Read(map, "SeatedIn") is not null)
            {
                return map;
            }
        }

        throw new InvalidOperationException(
            "no top in The Stormwatch Bar took the press — the eighth seat is gone and this whole file is "
            + "arguing about a chair that does not exist.");
    }

    // ── Reading the page ─────────────────────────────────────────────────────────────────────────────

    private static IReadOnlyList<FieldNote> Book(Pages.Map map) => (List<FieldNote>)Field(map, "_fieldNotes")!;

    private static IReadOnlyList<Satchel.Item> Sleeve(Pages.Map map) =>
        (List<Satchel.Item>)Field(map, "_satchel")!;

    private static IReadOnlyCollection<string> Register(Pages.Map map) =>
        (HashSet<string>)Field(map, "_workedUp")!;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

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
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    // ── Reflection plumbing ──────────────────────────────────────────────────────────────────────────

    private static FieldInfo FieldOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name.");

    private static object? Field(Pages.Map map, string name) => FieldOf(name).GetValue(map);

    private static void Set(Pages.Map map, string name, object? value) => FieldOf(name).SetValue(map, value);

    private static object? Read(Pages.Map map, string property) =>
        (typeof(Pages.Map).GetProperty(property, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{property}` — this guard is reading a dead name."))
            .GetValue(map);

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
