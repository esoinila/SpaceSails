using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1066 · THE MEETING THE BEAT WAS WAITING FOR.
///
/// <para><c>StoryBeats.Beat.CrewMeeting</c> shipped with <c>crew-meeting.jpg</c>, a title, a caption and a
/// cadence, and #1065 left it the last name on <see cref="EveryStoryBeatHasACallerTests"/>'s excuse list for
/// an honest reason: its <see cref="CrewTemp.Standing.Ultimatum"/> edge needed <i>a broken promise TO THE
/// CREW or months without shore leave</i>, and neither was a number anybody kept.</para>
///
/// <para><b>The rule this lane picked, and why it is legible.</b> A clamp is a RUN ASHORE at a great port
/// (<see cref="ArrivalTube.Tier.GreatPort"/>) and a WORKING STOP everywhere else. The player is never told
/// this in a new sentence, because the game already tells him at that very clamp: the arrival tube's
/// establishing shot (#541) is either a glazed gangway with <i>"two streams of people … and nobody who has
/// any idea who you are"</i> or one rigid tube with <i>"two dock workers waiting for you to get out of the
/// way"</i>. And the tier is derived from the scenario's own traffic model rather than authored, so a berth
/// added tomorrow gets the honest answer without anybody tagging it.</para>
///
/// <para><b>Why these guards read the PAGE and not a hand-typed voyage</b>, exactly as
/// <see cref="TheCrewSheetCountsTheDeadTests"/> does: the claim is about what the SHIPPED game can reach.
/// The arithmetic half — what an overdue run ashore is worth — lives in Core, in
/// <c>ShoreLeaveIsALedgerLineTests</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCrewSheetCountsTheStopsAshoreTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static void Set(Pages.Map map, string field, object value)
    {
        FieldInfo f = typeof(Pages.Map).GetField(field, Hidden)
            ?? throw new InvalidOperationException(
                $"Pages.Map has no {field} — the crew sheet is reading something this guard cannot see.");
        f.SetValue(map, value);
    }

    private static T Get<T>(Pages.Map map, string field)
    {
        FieldInfo f = typeof(Pages.Map).GetField(field, Hidden)
            ?? throw new InvalidOperationException($"Pages.Map has no {field}.");
        return (T)f.GetValue(map)!;
    }

    private static CrewTemp.Voyage VoyageOf(Pages.Map map)
    {
        MethodInfo voyage = typeof(Pages.Map).GetMethod("CrewVoyage", Hidden)
            ?? throw new InvalidOperationException("Map has no CrewVoyage() — the crew sheet's seam has been renamed.");
        return (CrewTemp.Voyage)voyage.Invoke(map, [])!;
    }

    private static string ClientSource(params string[] parts)
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return MapMarkup.Read(Path.Combine([candidate, .. parts]));
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the client above {AppContext.BaseDirectory}");
    }

    // ===== 1 · The berth writes the ledger line, and a great port is what clears it =====

    /// <summary>
    /// THE COUNTER IS A COUNTER. A working stop adds one, a great port puts it back to nothing — and both
    /// halves are asserted, because a method that only ever incremented would pass the first alone and a
    /// method that only ever cleared would pass the second.
    /// </summary>
    [Fact]
    public void AGreatPortIsARunAshoreAndEveryOtherBerthIsAWorkingStop()
    {
        var map = new Pages.Map();
        MethodInfo note = typeof(Pages.Map).GetMethod("NoteTheBerthTheCrewGot", Hidden)
            ?? throw new InvalidOperationException(
                "Map has no NoteTheBerthTheCrewGot — the clamp has stopped telling the crew sheet what kind " +
                "of berth this was (#1066).");

        Assert.Equal(0, Get<int>(map, "_workingStopsSinceShoreLeave"));

        note.Invoke(map, [ArrivalTube.Tier.WorkingBerth]);
        note.Invoke(map, [ArrivalTube.Tier.Outpost]);
        note.Invoke(map, [ArrivalTube.Tier.WorkingBerth]);
        Assert.Equal(3, Get<int>(map, "_workingStopsSinceShoreLeave"));

        note.Invoke(map, [ArrivalTube.Tier.GreatPort]);
        Assert.Equal(0, Get<int>(map, "_workingStopsSinceShoreLeave"));

        // …and it starts counting again from the gangway, rather than latching cleared.
        note.Invoke(map, [ArrivalTube.Tier.Outpost]);
        Assert.Equal(1, Get<int>(map, "_workingStopsSinceShoreLeave"));
    }

    /// <summary>
    /// ONE EVENT, TWO REPORTERS — #1065's law, and the reason it matters here is the third bug class. The
    /// clamp raises the arrival tube's establishing shot off a tier, and now writes the crew's ledger line
    /// off a tier. If those were two reads, a later change could leave the picture on the screen saying
    /// "great port" while the sheet counted a working stop, and nothing would be red.
    ///
    /// <para>Measured as a distance the way <c>BothArcsBreakOnTheWireTests</c> measures the arc pair, with a
    /// companion assertion proving the yardstick can say NO.</para>
    /// </summary>
    [Fact]
    public void TheLedgerLineAndTheEstablishingShotComeOffOneTierRead()
    {
        string src = ClientSource("Pages", "Map.Docking.cs");

        Assert.Contains("ArrivalTube.Tier tier = ArrivalTube.TierFor(", src, StringComparison.Ordinal);
        Assert.Contains("NoteTheBerthTheCrewGot(tier)", src, StringComparison.Ordinal);
        Assert.Contains("RaiseStoryBeat(ArrivalTube.BeatFor(tier)", src, StringComparison.Ordinal);

        int note = src.IndexOf("NoteTheBerthTheCrewGot(tier)", StringComparison.Ordinal);
        int shot = src.IndexOf("RaiseStoryBeat(ArrivalTube.BeatFor(tier)", StringComparison.Ordinal);
        Assert.True(Math.Abs(shot - note) < 200,
            $"the crew's ledger line and the berth's establishing shot are {Math.Abs(shot - note)} characters " +
            "apart — they are no longer the one statement about the one berth (#1066)");

        // The yardstick can say NO: two statements that really are far apart in this file measure far apart.
        int clamp = src.IndexOf("private void ClampOntoHaven(", StringComparison.Ordinal);
        int undock = src.IndexOf("private void Undock()", StringComparison.Ordinal);
        Assert.True(clamp > 0 && undock > 0 && Math.Abs(undock - clamp) >= 200,
            "the distance yardstick above cannot tell near from far — it would pass on anything");
    }

    /// <summary>The clamp is the ONLY place in the client that classifies a berth for the crew, because a
    /// second one would be a second set of books.</summary>
    [Fact]
    public void OnlyTheClampDecidesWhatKindOfBerthTheCrewGot()
    {
        int callers = 0;
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        string clientDir = "";
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                clientDir = Path.Combine(at.FullName, "src", "SpaceSails.Client");
                break;
            }
            at = at.Parent;
        }

        Assert.True(clientDir.Length > 0, "could not find the client");

        foreach (string file in Directory.EnumerateFiles(clientDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            int from = 0;
            while ((from = text.IndexOf("NoteTheBerthTheCrewGot(", from, StringComparison.Ordinal)) >= 0)
            {
                // the declaration itself is not a call
                if (!text[..from].EndsWith("private void ", StringComparison.Ordinal))
                {
                    callers++;
                }
                from += 1;
            }
        }

        Assert.Equal(1, callers);
    }

    // ===== 2 · The counter reaches the sheet, and the sheet shows it =====

    /// <summary>
    /// THE HARNESS IS REALLY DRIVING THE SHIPPING SHEET. The tally does not sit in a field admiring itself:
    /// it reaches <c>CrewVoyage()</c> as <see cref="CrewTemp.Voyage.PromisesBroken"/>, which is the hook
    /// THE CAPTAIN'S WORD names first — <i>"a run ashore, a share, a rescue"</i>.
    /// </summary>
    [Fact]
    public void TheOverdueRunAshoreReachesTheSheetAsABrokenPromise()
    {
        var map = new Pages.Map();

        Assert.Equal(0, VoyageOf(map).PromisesBroken);

        Set(map, "_workingStopsSinceShoreLeave", CrewTemp.WorkingStopsBetweenRunsAshore - 1);
        Assert.Equal(0, VoyageOf(map).PromisesBroken);   // inside the line, the word still holds

        Set(map, "_workingStopsSinceShoreLeave", CrewTemp.WorkingStopsBetweenRunsAshore + 2);
        Assert.Equal(3, VoyageOf(map).PromisesBroken);

        // …and it lands on the dimension the promise was made on, not on a new one. REST is untouched,
        // because the game counts BERTHS and this field is DAYS — see Map.CrewTemp's note on why that stays
        // dormant rather than being faked from stops.
        Assert.Equal(0, VoyageOf(map).DaysSinceShoreLeave);
    }

    /// <summary>
    /// #761 · AND THE CAPTAIN IS TOLD BEFORE IT BREAKS. The crew's report is the warning at every step —
    /// <i>"a captain who is surprised was not reading it"</i> — so the counter and the line it is measured
    /// against are both on the sheet, in the cohesion footnote's own idiom and not in a new panel.
    /// </summary>
    [Fact]
    public void TheCrewSheetShowsTheCounterAndWhereTheLineIs()
    {
        string sheet = ClientSource("Pages", "Stations", "Captain.razor");

        Assert.Contains("WorkingStopsSinceShoreLeave", sheet, StringComparison.Ordinal);
        Assert.Contains("CrewTemp.WorkingStopsBetweenRunsAshore", sheet, StringComparison.Ordinal);
        Assert.Contains("CrewTemp.ShoreLeaveLine(", sheet, StringComparison.Ordinal);

        // …and the page really hands it over, rather than the sheet rendering a parameter nobody sets.
        string page = ClientSource("Pages", "Map.razor");
        Assert.Contains("WorkingStopsSinceShoreLeave=\"_workingStopsSinceShoreLeave\"", page,
            StringComparison.Ordinal);

        // It is a footnote beside cohesion, not a panel of its own (the sheet's own idiom, #761).
        Assert.Contains("crew-shore", sheet, StringComparison.Ordinal);
        Assert.Contains("crew-shore", ClientSource("Pages", "Stations", "Captain.razor.css"),
            StringComparison.Ordinal);
    }

    // ===== 3 · The world can get there — and a route with shore leave never does =====

    /// <summary>
    /// <b>THE VACUITY PAIR THIS LANE EXISTS FOR.</b> Two routes through the shipping page's own voyage: one
    /// that takes its shore leave and one that does not. The first must never convene the meeting; the
    /// second must. Either half alone would pass on a game that raised the beat for everybody, or for
    /// nobody.
    ///
    /// <para>The sweep is over <c>Map.CrewVoyage()</c> itself, so what it measures is the reach of the
    /// SHIPPED inputs rather than a hand-typed mirror of them.</para>
    /// </summary>
    [Fact]
    public void ARouteThatTakesItsShoreLeaveNeverConvenesTheMeetingAndOneThatDoesNotDoes()
    {
        // ── the route WITH shore leave: every berth cleared the counter, so the tally never leaves zero.
        CrewTemp.Standing worstAshore = CrewTemp.Standing.Solid;
        CrewTemp.Voyage worstAshoreVoyage = default;

        foreach (CrewTemp.Voyage v in EverySheetTheGameCanBuild(stops: 0))
        {
            CrewTemp.Standing s = CrewTemp.StandingOf(v);
            if (s > worstAshore)
            {
                worstAshore = s;
                worstAshoreVoyage = v;
            }
        }

        Assert.True(worstAshore < CrewTemp.Standing.Ultimatum,
            $"a captain who took his crew ashore at every port still reached {worstAshore} " +
            $"({worstAshoreVoyage}) — shore leave has stopped being what the meeting is about (#1066)");

        // …and the floor is not vacuous either: the route ashore still reaches the DEPUTATION, which is
        // #1065's pin and the reason that beat has a caller at all.
        Assert.Equal(CrewTemp.Standing.Petition, worstAshore);

        // ── the same page, the same sweep, one thing different: nobody went ashore.
        CrewTemp.Standing worstAdrift = CrewTemp.Standing.Solid;
        int stops = CrewTemp.WorkingStopsBetweenRunsAshore + 1;

        foreach (CrewTemp.Voyage v in EverySheetTheGameCanBuild(stops))
        {
            CrewTemp.Standing s = CrewTemp.StandingOf(v);
            if (s > worstAdrift)
            {
                worstAdrift = s;
            }
        }

        Assert.True(worstAdrift >= CrewTemp.Standing.Ultimatum,
            $"{stops} berths with nobody ashore reach no further than {worstAdrift} — " +
            "StoryBeats.Beat.CrewMeeting's caller can never fire and it belongs back on " +
            "EveryStoryBeatHasACallerTests.KnownOrphans (#1066)");
    }

    /// <summary>Every crew sheet the shipped inputs can build at a given shore-leave tally, read off the
    /// page's own <c>CrewVoyage()</c>.</summary>
    private static IEnumerable<CrewTemp.Voyage> EverySheetTheGameCanBuild(int stops)
    {
        foreach (int filings in new[] { 0, 3, 7, 12, 40 })
        {
            foreach (int lies in new[] { 0, 3, 40 })
            {
                foreach (int credits in new[] { 0, 5_000, 50_000 })
                {
                    foreach (int tots in new[] { 0, 10 })
                    {
                        foreach (int nearMisses in new[] { 0, 5 })
                        {
                            foreach (int lost in new[] { 0, 1, 3, 5, 12 })
                            {
                                for (int heat = 0; heat <= EncounterRule.MaxHeatLevel; heat++)
                                {
                                    foreach (bool vented in new[] { false, true })
                                    {
                                        var map = new Pages.Map();
                                        Set(map, "_crewLost", lost);
                                        Set(map, "_honestFilings", filings);
                                        Set(map, "_profitableLies", lies);
                                        Set(map, "_credits", credits);
                                        Set(map, "_rumTots", tots);
                                        Set(map, "_nearMisses", nearMisses);
                                        Set(map, "_ventedOwnCompartment", vented);
                                        Set(map, "_heat", new HeatState(heat, 0.0));
                                        Set(map, "_workingStopsSinceShoreLeave", stops);
                                        yield return VoyageOf(map);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // ===== 4 · The whole chain, through the shipping seams =====

    /// <summary>
    /// AND THE CHAIN, END TO END. The dev door seeds the counters, the ship's own clock reads the sheet, the
    /// standing crosses the Ultimatum edge, and <c>RaiseStoryBeat</c> — with Core's cadence and presentation
    /// rules applied and nothing faked in between — puts the meeting on the screen. This is the assertion
    /// that would go red if any link were only pretending.
    ///
    /// <para>It also pins the half a scanner cannot see and a modal storm would hide: the beat is
    /// <see cref="StoryBeats.Cadence.EveryTime"/>, so raising it unconditionally on a standing that STAYS
    /// true would put a card on the screen every time the captain's purse moved. It is raised on the
    /// CROSSING, and the loop below is what proves it.</para>
    /// </summary>
    [Fact]
    public void TheShipsOwnClockConvenesTheMeetingOnceAndNotOnEveryFrame()
    {
        var map = new Pages.Map();
        TheBootBuildsTheSameWorldTests.NeverRender(map);

        MethodInfo watch = typeof(Pages.Map).GetMethod("WatchWhereTheCrewStand", Hidden)
            ?? throw new InvalidOperationException("Map has no WatchWhereTheCrewStand — the meeting's caller is gone.");
        FieldInfo card = typeof(Pages.Map).GetField("_storyCard", Hidden)!;

        // A ship that has been ashore says nothing, however many times she is asked.
        watch.Invoke(map, []);
        watch.Invoke(map, []);
        Assert.Null(card.GetValue(map));

        // The dev door's voyage, read on the ship's own clock.
        typeof(Pages.Map).GetMethod("SeedCrewCheat", Hidden)!.Invoke(map, ["meeting"]);
        Assert.Equal(CrewTemp.Standing.Ultimatum, CrewTemp.StandingOf(VoyageOf(map)));

        watch.Invoke(map, []);

        object? raised = card.GetValue(map);
        Assert.NotNull(raised);
        Assert.Equal(StoryBeats.Beat.CrewMeeting, raised.GetType().GetField("Item1")!.GetValue(raised));

        // NO MODAL STORM. Dismiss it, keep the crew at an ultimatum, and keep the sheet moving the way a
        // voyage moves it — the purse changes on every sale. The meeting does not come back.
        card.SetValue(map, null);
        for (int frame = 1; frame <= 8; frame++)
        {
            Set(map, "_credits", frame * 25);
            watch.Invoke(map, []);
            Assert.Equal(CrewTemp.Standing.Ultimatum, CrewTemp.StandingOf(VoyageOf(map)));
            Assert.Null(card.GetValue(map));
        }

        // …and the deputation does not arrive AFTER the meeting either: a crew at an ultimatum are past
        // asking, and hats-in-hands would be the escalation running backwards.
        Assert.Null(card.GetValue(map));
    }

    /// <summary>
    /// AND YOU CAN REACH IT ON DEMAND — the boot's own rule beside the query readers: <i>"a scene nobody can
    /// reach on demand is a scene that ships broken"</i>. Reaching this edge honestly is most of a session
    /// (bad dice on the rock, a dozen honest filings, then five berths in a row with no gangway), so it gets
    /// a door — and the door grants COUNTERS ONLY: no standing, no card, no beat.
    /// </summary>
    [Fact]
    public void TheDevDoorConvenesTheMeetingAndOnlyEverGrantsCounters()
    {
        var map = new Pages.Map();
        typeof(Pages.Map).GetMethod("SeedCrewCheat", Hidden)!.Invoke(map, ["meeting"]);

        CrewTemp.Voyage seeded = VoyageOf(map);
        Assert.Equal(CrewTemp.Standing.Ultimatum, CrewTemp.StandingOf(seeded));

        // BOTH halves are needed, which is the design and not a threshold: take the shore leave back and the
        // same seeded ship is only at her crew's door; take the bodies back and she is merely grumbling.
        Assert.Equal(CrewTemp.Standing.Petition, CrewTemp.StandingOf(seeded with { PromisesBroken = 0 }));
        Assert.True(CrewTemp.StandingOf(seeded with { CrewLost = 0, HonestFilings = 0 })
                    < CrewTemp.Standing.Ultimatum);

        // Nothing was pushed on the way in.
        Assert.Null(typeof(Pages.Map).GetField("_storyCard", Hidden)!.GetValue(map));

        // The reader half: the URLs the testing guide prints are the URLs the parse answers to.
        Assert.Equal("meeting", CrewCheatFrom("/map?crew=meeting"));
        Assert.Equal("meeting", CrewCheatFrom("/map?crew=ultimatum"));
        Assert.Equal("petition", CrewCheatFrom("/map?crew=petition"));
        Assert.Null(CrewCheatFrom("/map?crew=marooning"));   // an edge nothing can reach is not a dev door
    }

    /// <summary>What <c>?crew=</c> answered, read through the shipping parse.</summary>
    private static string? CrewCheatFrom(string url)
    {
        var map = new Pages.Map();
        TheBootBuildsTheSameWorldTests.NeverRender(map);
        TheBootBuildsTheSameWorldTests.Hand(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench(url));

        MethodInfo read = typeof(Pages.Map).GetMethod("ReadEveryQueryKey", Hidden)!;
        object q = read.Invoke(map, [new Uri("http://localhost" + url)])!;
        return (string?)q.GetType().GetField("CrewCheat", Hidden)!.GetValue(q);
    }
}
