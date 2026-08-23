using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #727 · THE MISSION THAT LEAVES THE SHIP — the client half.
///
/// <para>Owner, 2026-08-06: <i>"Right now the tasks and missions have been ship-UI specific, so a mission that
/// works outside the ship UI is something new … we should filter out / minimize ship-specific stuff to
/// appropriate ('cannot do in this UI level, but high level: go to Moon X') type info in the carried mission
/// UI."</i></para>
///
/// <para><b>THE LAW: one mission model, two projections — never two mission lists.</b> Four guards, each
/// closing one way the second screen could start disagreeing with the first:</para>
/// <list type="number">
///   <item><b>ONE MODEL</b> — swept over every (kind, state) a contract can be in, on a real component over
///   the real Sol ephemeris: every mission the captain's desk draws is in the pane too (collapsed, but
///   present), and every foot-level line the pane shows is the DESK'S OWN STRING, byte for byte.</item>
///   <item><b>NO DEAD BUTTON</b> — the pane's own markup, sliced out of Map.razor, contains no control at
///   all. A dead affordance in a satchel is the lift-that-only-went-down bug wearing a UI.</item>
///   <item><b>THE COLLAPSE</b> — a ship-level step renders exactly <c>⛵ return to the ship — next: X</c>,
///   with the right X.</item>
///   <item><b>ONE WRITER, AND THE BEAT IS ON THE POP-UP</b> — every advance in the client goes through
///   <c>AdvanceMission</c> (proved by sweeping the source for a second one), and a step finished on foot with
///   the satchel open answers INSIDE the satchel rather than on a banner behind its backdrop (#736).</item>
/// </list>
///
/// <para><b>PROVEN ABLE TO FAIL.</b> Each guard's own summary names the edit that reddens it; the verbatim
/// runs are in the pull request.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCarriedMissionsPaneTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    // ── The bench ───────────────────────────────────────────────────────────────────────────────────────

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

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    /// <summary>The shipping scenario, off the canonical copy at the repo root — the same JSON the client
    /// fetches. Cached: the ephemeris is read-only here and every case wants the same sky.</summary>
    private static readonly Lazy<ICelestialEphemeris> Sol = new(() => CircularOrbitEphemeris.FromScenario(
        ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json"))));

    private static Type Nested(string name) =>
        typeof(Pages.Map).GetNestedType(name, Hidden | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Pages.Map has no nested `{name}` — this guard reads a dead name.");

    private static object Kind(string name) => Enum.Parse(Nested("QuestKind"), name);

    private static object State(string name) => Enum.Parse(Nested("QuestState"), name);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Get(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(map, args);
    }

    /// <summary>A live component over the real Sol ephemeris, standing on <paramref name="standingOn"/> (a
    /// station's deck, which is the ground the break-in arc is walked on). The one piece of theatre is the
    /// render handle — the same early-out <c>MustStandUpBeforeWalkingTests</c> rides.</summary>
    private static Pages.Map Ashore(string standingOn = "cinder-roost")
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_scenarioName", "Sol");
        Set(map, "_ephemeris", Sol.Value);
        Set(map, "_ship", Invoke(map, "InitializeShipState")!);
        Set(map, "_dockedHavenId", standingOn);
        return map;
    }

    /// <summary>One contract, minted through the component's own private record and pushed into the one
    /// store both views read.</summary>
    private static object Give(Pages.Map map, string kind, string state,
        string targetShipId = "V-06", string callsign = "The Rusty Roadstead",
        string? destBodyId = null, string? sourceBodyId = null, string? pin = null)
    {
        object quest = Activator.CreateInstance(Nested("Quest"), Hidden, binder: null,
        [
            $"{kind}-1", Kind(kind), "THE FIXER", targetShipId, callsign,
            // #973 L5b · …and the theory tag the row carries since the walk-in's favour became the first
            // LOVE-tagged job in the game. Null here on purpose: every contract this bench mints is one of
            // the money-shaped ones, and a tag invented for them would be this bench answering a question
            // the ledger is supposed to.
            $"A {kind} job", "What the stranger wanted.", 2600, destBodyId, sourceBodyId, pin, null,
        ], culture: null)!;
        Nested("Quest").GetProperty("State", Hidden)!.SetValue(quest, State(state));
        ((IList)Get(map, "_quests")!).Add(quest);
        return quest;
    }

    private static IReadOnlyList<Pages.Stations.Captain.QuestItem> TheChairsView(Pages.Map map) =>
        (Pages.Stations.Captain.QuestItem[])Invoke(map, "QuestCards")!;

    private static IReadOnlyList<CompassLine> ThePane(Pages.Map map) =>
        (IReadOnlyList<CompassLine>)Invoke(map, "CompassOnFoot")!;

    // ── (a) ONE MODEL ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SWEEP. Every kind of contract the game can hand you, in every state it can be in, on a real
    /// component over the real Sol ephemeris — and for each one:
    /// <list type="bullet">
    ///   <item>the captain's desk draws it, and so does the pane (unless it is settled and paid);</item>
    ///   <item>the pane's line for it is EITHER the desk's own status label byte for byte (foot level, this
    ///   ground) OR the one collapsed sentence — never a third wording;</item>
    ///   <item>a collapsed line names a destination and says nothing about burns.</item>
    /// </list>
    /// <para><b>RED by dropping a mission kind from the projection</b> — teach <c>LiveMissions</c> to skip
    /// <c>QuestKind.Crack</c> and the four Crack rows fail on "the desk holds it and the pane does not".
    /// Also RED by rewording a foot step in the pane, which is the two-lists bug in its first minute.</para>
    /// <para>The counters at the end are the anti-vacuous tripwire (guard e): a sweep that saw no foot-level
    /// steps, or no ship-level ones, is a sweep that proved nothing about the fold.</para>
    /// </summary>
    [Fact]
    public void EVERY_MISSION_TheDeskHoldsIsInThePaneAndEveryFootStepIsTheDesksOwnWords()
    {
        (string Kind, string State)[] everyContract =
        [
            ("Hunt", "Active"), ("Hunt", "Complete"), ("Hunt", "TurnedIn"),
            ("CargoRun", "Active"), ("CargoRun", "Complete"),
            ("Intel", "TurnedIn"),
            ("Fetch", "Active"), ("Fetch", "PickedUp"),
            ("Crack", "Active"), ("Crack", "PickedUp"),
            ("Favor", "Active"), ("Favor", "Complete"),
            ("FetchCache", "Active"), ("FetchCache", "PickedUp"),
        ];

        int seen = 0, onFoot = 0, collapsed = 0;

        foreach ((string kind, string state) in everyContract)
        {
            Pages.Map map = Ashore();
            Give(map, kind, state,
                destBodyId: "cinder-roost", sourceBodyId: kind == "FetchCache" ? "cinder-roost" : "luna",
                pin: "4417");

            IReadOnlyList<Pages.Stations.Captain.QuestItem> desk = TheChairsView(map);
            IReadOnlyList<CompassLine> pane = ThePane(map);
            string where = $"{kind}/{state}";

            Assert.Single(desk);
            bool settled = state == "TurnedIn";
            Assert.True(pane.Count == (settled ? 0 : 1),
                $"{where}: the desk holds {desk.Count} and the pane shows {pane.Count} — the two views have " +
                "become two lists (#727).");
            if (settled)
            {
                continue;
            }

            seen++;
            CompassLine line = pane[0];
            if (line.Actionable)
            {
                onFoot++;
                Assert.True(desk[0].StatusLabel == line.Step,
                    $"{where}: the pane says \"{line.Step}\" where the desk says \"{desk[0].StatusLabel}\" — " +
                    "the carried view has started wording the same step its own way (#727).");
            }
            else
            {
                collapsed++;
                Assert.StartsWith(MissionProjection.ReturnToShip, line.Step, StringComparison.Ordinal);
                Assert.True(line.Step.Length > MissionProjection.ReturnToShip.Length,
                    $"{where}: the collapse names nowhere at all.");
                Assert.Null(line.Action);
            }

            Assert.Equal(desk[0].Title, line.Title);
        }

        // Anti-vacuous. All three must be non-zero or the sweep saw one shape of contract and called it a law.
        Assert.True(seen >= 12, $"the sweep only carried {seen} live missions.");
        Assert.True(onFoot >= 3, $"the sweep found {onFoot} foot-level steps — it never exercised the pass-through.");
        Assert.True(collapsed >= 6, $"the sweep found {collapsed} collapsed steps — it never exercised the fold.");
    }

    /// <summary>…and the DEV BOOT that carries a mission is swept too, not just the hand-built matrix: the
    /// shipping fetch cheat, at each of its three stages, seeded by the shipping code. RED the same way.</summary>
    [Theory]
    [InlineData("intel")]
    [InlineData("active")]
    [InlineData("picked")]
    public void TheFetchBootCarriesItsMissionIntoThePaneToo(string stage)
    {
        Pages.Map map = Ashore("cinder-roost");
        Invoke(map, "InjectFetchCheat", stage);

        Assert.Single(TheChairsView(map));
        CompassLine line = Assert.Single(ThePane(map));
        Assert.False(string.IsNullOrWhiteSpace(line.Step));
        Assert.Equal(TheChairsView(map)[0].Title, line.Title);
    }

    // ── (c) THE COLLAPSE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>A run the chair owns renders as DIRECTION and names the world it is bound for — the owner's
    /// own "cannot do in this UI level, but high level: go to Moon X". RED by leaking the burn text: give the
    /// ship-level branch of <c>MissionProjection.OnFoot</c> the step's own <c>Text</c> and this fails on the
    /// missing ⛵ prefix.</summary>
    [Fact]
    public void ASHIP_LEVEL_StepRendersTheReturnToShipFormWithTheRightDestination()
    {
        Pages.Map map = Ashore("cinder-roost");
        Give(map, "CargoRun", "Active", callsign: "Selene Gate", destBodyId: "luna");

        CompassLine line = Assert.Single(ThePane(map));
        Assert.Equal("⛵ return to the ship — next: Luna", line.Step);
        Assert.False(line.Actionable);
    }

    /// <summary>…and the SAME contract, walked to its ground, spells its step out instead. One quest, two
    /// grounds, two answers — which is the whole feature in one assertion.</summary>
    [Fact]
    public void TheSameContractSpellsItselfOutOnItsOwnGround()
    {
        Pages.Map here = Ashore("cinder-roost");
        Give(here, "Crack", "Active", destBodyId: "cinder-roost", sourceBodyId: "cinder-roost", pin: "4417");
        CompassLine standing = Assert.Single(ThePane(here));

        Pages.Map away = Ashore("the-rusty-roadstead");
        Give(away, "Crack", "Active", destBodyId: "cinder-roost", sourceBodyId: "cinder-roost", pin: "4417");
        CompassLine elsewhere = Assert.Single(ThePane(away));

        Assert.True(standing.Actionable, "the break-in is not spelled out on the deck it is on.");
        Assert.Contains("4417", standing.Step, StringComparison.Ordinal);
        Assert.False(elsewhere.Actionable, "the break-in spelled its hatch code out from another station.");
        Assert.StartsWith(MissionProjection.ReturnToShip, elsewhere.Step, StringComparison.Ordinal);
    }

    // ── (b) NO DEAD BUTTON ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The pane's own markup, cut out of Map.razor. It renders no control of any kind — the carried
    /// view is read-only in v1 (the issue's own fence: signing on happens where people are), so there is
    /// nothing here to grey out, hide or refuse. RED by rendering a burn button: put any
    /// <c>&lt;button</c> or <c>@onclick</c> into the MISSIONS page and this fails by name.
    ///
    /// <para>It is a SOURCE-SHAPE guard on purpose. Owner, on #680: <i>"seeing the text in DOM does not mean
    /// user can see it on the screen"</i> — and its converse holds here: a control's deadness is a property
    /// of the markup that draws it, so the markup is what gets read.</para></summary>
    [Fact]
    public void THE_PANE_RendersNoAffordanceItCannotHonour()
    {
        string pane = TheMissionsPage();

        foreach (string dead in new[] { "<button", "@onclick", "disabled=", "EventCallback" })
        {
            Assert.False(pane.Contains(dead, StringComparison.Ordinal),
                $"the MISSIONS pane's markup contains `{dead}` — a control in the satchel that only the " +
                "captain's chair can honour is the lift-that-only-went-down bug wearing a UI (#727).");
        }

        // …and it decides nothing either: the words are Core's and the rows are the projection's.
        foreach (string thinking in new[] { "_quests", "QuestKind", "QuestState", "MissionUiLevel" })
        {
            Assert.False(pane.Contains(thinking, StringComparison.Ordinal),
                $"the MISSIONS pane's markup reaches for `{thinking}` — the page has started classifying " +
                "missions itself, which is the second opinion this lane exists to prevent.");
        }

        Assert.Contains("CompassOnFoot()", pane, StringComparison.Ordinal);
        Assert.Contains("MissionProjection.NothingOwedOnFoot", pane, StringComparison.Ordinal);
        Assert.Contains("MissionProjection.CompassBlurb", pane, StringComparison.Ordinal);
    }

    /// <summary>The tab is beside the field book and it is ALWAYS drawn — unlike SPREAD and THE BIN, whose
    /// tabs follow a posture and a fixture. A compass that disappeared when there was nothing on it would
    /// answer "why am I down here" with silence.</summary>
    [Fact]
    public void TheTabSitsBesideTheFieldBookAndNeverGoesAway()
    {
        string razor = Pages("Map.razor");
        int tabs = razor.IndexOf("<div class=\"satchel-tabs\">", StringComparison.Ordinal);
        Assert.True(tabs > 0, "the satchel has no tab strip.");
        int shut = razor.IndexOf("                </div>", tabs, StringComparison.Ordinal);
        Assert.True(shut > tabs, "the satchel's tab strip no longer closes where this guard expects.");
        string strip = razor[tabs..shut];

        int notes = strip.IndexOf("SatchelPage.Notes", StringComparison.Ordinal);
        int missions = strip.IndexOf("SatchelPage.Missions", StringComparison.Ordinal);
        Assert.True(notes > 0 && missions > notes,
            "the MISSIONS tab is not beside the field book — the owner's default was a satchel tab, not a " +
            "page inside the notebook (#727).");

        // No @if between the book's tab and this one: nothing gates it.
        Assert.DoesNotContain("@if", strip[notes..missions], StringComparison.Ordinal);
        Assert.Contains("🗺 MISSIONS", strip, StringComparison.Ordinal);
    }

    /// <summary>The page's own slice, cut the way this file's siblings cut theirs: from its `else if` to the
    /// next page at the same indent.</summary>
    private static string TheMissionsPage()
    {
        string razor = Pages("Map.razor");
        int at = razor.IndexOf("else if (_satchelPage == SatchelPage.Missions)", StringComparison.Ordinal);
        Assert.True(at > 0, "Map.razor has no MISSIONS page (#727).");
        int end = razor.IndexOf("\n                else", at, StringComparison.Ordinal);
        Assert.True(end > at, "the MISSIONS page no longer ends where this guard expects.");
        return razor[at..end];
    }

    // ── (d) ONE WRITER, AND THE BEAT IS ON THE POP-UP ───────────────────────────────────────────────────

    /// <summary>THE LAW READS ITSELF. There is exactly one place in the client that moves a contract's state,
    /// and it is <c>AdvanceMission</c> — which does not name a <c>QuestState</c> literal at all, so a
    /// <c>.State = QuestState.…</c> anywhere in Pages is by definition a second writer. RED by completing
    /// through a second path: put <c>q.State = QuestState.TurnedIn;</c> back into <c>DeliverFetch</c> and
    /// this names the file and the line.</summary>
    [Fact]
    public void THERE_IS_ONE_WRITER_AndEveryOnFootCompletionGoesThroughIt()
    {
        var second = new List<string>();
        foreach (string file in Directory.EnumerateFiles(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "*.cs", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(".State = QuestState.", StringComparison.Ordinal))
                {
                    second.Add($"{Path.GetFileName(file)}:{i + 1}");
                }
            }
        }

        Assert.True(second.Count == 0,
            "a contract's state is moved somewhere other than AdvanceMission — " + string.Join(", ", second)
            + ". One writer, chair-side and boots-side alike, or the two will drift (#727).");

        // …and the on-foot sites really do call it, rather than having found some third way round — AND the
        // beat each one is finished with rides the WRITER rather than the banner. The index comparison is
        // the same shape TheOutcomeIsOnThePopUpTests uses on the freight agent's receipt: what decides
        // readability is which call the sentence is an argument OF.
        foreach ((string file, string signature, string beat) in new[]
        {
            ("Map.Quests.Contracts.cs", "private void DeliverFetch(", "The wallet changes hands"),
            ("Map.Quests.Contracts.cs", "private void DeliverCrack(", "The package slides across"),
            ("Map.Quests.Contracts.cs", "private void CompleteFetchCacheFor(", "MissionBrief.NextPrefix"),
            ("Map.Quests.Caches.cs", "private void LiftStash(", "You peel the package"),
            ("Map.Deck.Fixtures.cs", "private void SubmitPin(", "You pocket the package"),
        })
        {
            string body = Method(file, signature);
            int said = body.IndexOf(beat, StringComparison.Ordinal);
            Assert.True(said > 0, $"`{signature}` in {file} no longer says \"{beat}\" — this guard is reading a dead line.");

            int writer = body.LastIndexOf("AdvanceMission(", said, StringComparison.Ordinal);
            int banner = body.LastIndexOf("ShowPulseMessage(", said, StringComparison.Ordinal);
            Assert.True(writer >= 0 && writer > banner,
                $"`{signature}` in {file} finishes a step and says so on the HUD banner rather than through " +
                "the one writer — with a card or an open satchel in front of the captain that line plays " +
                "behind the backdrop (#727/#736).");
        }
    }

    /// <summary>One method's body, from its signature to the next member at the same indent — the same cut
    /// <see cref="TheOutcomeIsOnThePopUpTests"/> makes.</summary>
    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        int end = src.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        return src[at..(end > at ? end : src.Length)];
    }

    /// <summary>#736's law, DRIVEN on a real component: with the satchel open on the compass, a step finished
    /// on foot answers INSIDE the satchel — not on the HUD, which is behind the dialog's own backdrop. RED by
    /// bannering: swap <c>AdvanceMission</c>'s <c>SayItWhereTheyAreLooking</c> for
    /// <c>ShowPulseMessage</c> and the receipt lands in the pulse with the satchel outcome empty.</summary>
    [Fact]
    public void FINISHING_ON_FOOT_SaysSoOnThePopUpThatIsUpAndNotOnTheBannerBehindIt()
    {
        Pages.Map map = Ashore("cinder-roost");
        object job = Give(map, "Crack", "PickedUp",
            destBodyId: "cinder-roost", sourceBodyId: "cinder-roost", pin: "4417");

        // The captain is standing there with the compass open — which is exactly when the old banner was
        // invisible, because the satchel draws a backdrop over the HUD.
        Set(map, "_showSatchel", true);
        Set(map, "_satchelPage", Enum.Parse(Nested("SatchelPage"), "Missions"));
        Set(map, "_satchelOutcome", null);

        Invoke(map, "DeliverCrack", job);

        Assert.Equal(State("TurnedIn"), Nested("Quest").GetProperty("State", Hidden)!.GetValue(job));

        string? said = (string?)Get(map, "_satchelOutcome");
        Assert.False(string.IsNullOrWhiteSpace(said),
            "the hand-off said nothing on the pop-up the captain is looking at (#736).");
        Assert.Contains("no receipt", said!, StringComparison.Ordinal);

        // …and nothing was left on the banner underneath.
        Assert.DoesNotContain("no receipt", Get(map, "_pulse")!.ToString() ?? "", StringComparison.Ordinal);

        // The pane agrees with the chair afterwards: a settled job is off both.
        Assert.Empty(ThePane(map));
    }

    /// <summary>…and with NOTHING in front of the captain the same act says it on the world, which is the
    /// seam's own fallback and the reason routing through it is safe everywhere.</summary>
    [Fact]
    public void WithNothingUpTheSameActSaysItOnTheWorld()
    {
        Pages.Map map = Ashore("cinder-roost");
        object job = Give(map, "Fetch", "PickedUp", destBodyId: "cinder-roost", sourceBodyId: "luna");

        Invoke(map, "DeliverFetch", job);

        Assert.Contains("we never met", Get(map, "_pulse")!.ToString() ?? "", StringComparison.Ordinal);
    }
}
