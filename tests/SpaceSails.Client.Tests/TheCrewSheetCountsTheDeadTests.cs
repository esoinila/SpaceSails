using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #663 · THE REPORT ON THE DESK SAID NOBODY HAD BEEN LOST, AND PEOPLE HAD BEEN.
///
/// <para>The deputation beat (<c>StoryBeats.Beat.CrewDeputation</c>) shipped with a painted canvas, a
/// cadence and no caller, and the excuse filed against it in <see cref="EveryStoryBeatHasACallerTests"/>
/// was that the world could not cross the <c>CrewTemp.Standing.Petition</c> edge — because <c>CrewLost</c>
/// was an honest dormant constant in <c>Map.CrewTemp</c>, <i>"needs individual crew before anybody can fail
/// to return"</i>.</para>
///
/// <para><b>That excuse had already expired.</b> The asteroid-deflection gig (#394) puts five of the crew on
/// a rock falling at the Ringside Exchange, rolls <c>DeflectionGig</c>'s crew-bolt complication against
/// them, docks <c>PerCrewLostPenalty</c> off the fee per body, and prints <i>"N of the crew did not come
/// home."</i> The crew sheet on the captain's desk went on answering <i>"Nobody has been lost. On a ship
/// like this that is not luck, it is the captain."</i> — the third bug class exactly: the sim doing one
/// thing while a sentence reports another.</para>
///
/// <para><b>Why these guards read the PAGE and not a hand-typed Voyage.</b> The claim is about which inputs
/// the shipped game can move, so it is asked of <c>Map.CrewVoyage()</c> itself, through the same reflection
/// idiom <see cref="TheBootBuildsTheSameWorldTests"/> boots the page with. A sweep of literal
/// <see cref="CrewTemp.Voyage"/> values in the Core suite could only ever mirror this file, and would go on
/// passing the day somebody put the constant back. The rules' half of the claim — what a body is WORTH —
/// stays in Core, in <c>CrewTempTests</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCrewSheetCountsTheDeadTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The shipping page, with the voyage's live inputs set the way the game sets them.</summary>
    private static CrewTemp.Voyage VoyageOf(
        int crewLost = 0, int honestFilings = 0, int profitableLies = 0, int credits = 0,
        int rumTots = 0, int nearMisses = 0, int heat = 0, bool vented = false)
    {
        var map = new Pages.Map();
        Set(map, "_crewLost", crewLost);
        Set(map, "_honestFilings", honestFilings);
        Set(map, "_profitableLies", profitableLies);
        Set(map, "_credits", credits);
        Set(map, "_rumTots", rumTots);
        Set(map, "_nearMisses", nearMisses);
        Set(map, "_ventedOwnCompartment", vented);
        Set(map, "_heat", new HeatState(heat, 0.0));

        MethodInfo voyage = typeof(Pages.Map).GetMethod("CrewVoyage", Hidden)
            ?? throw new InvalidOperationException("Map has no CrewVoyage() — the crew sheet's seam has been renamed.");
        return (CrewTemp.Voyage)voyage.Invoke(map, [])!;
    }

    private static void Set(Pages.Map map, string field, object value)
    {
        FieldInfo f = typeof(Pages.Map).GetField(field, Hidden)
            ?? throw new InvalidOperationException(
                $"Pages.Map has no {field} — the crew sheet is reading something this guard cannot see.");
        f.SetValue(map, value);
    }

    private static string ClientSource(string file)
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return File.ReadAllText(Path.Combine(candidate, "Pages", file));
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the client above {AppContext.BaseDirectory}");
    }

    /// <summary>
    /// THE GUARD THIS LANE EXISTS FOR. The sheet the ship hands the captain counts the people who did not
    /// come home. Asked of the page's own <c>CrewVoyage()</c>, so a constant put back in its place reddens
    /// this immediately.
    /// </summary>
    [Fact]
    public void TheSheetCountsThePeopleWhoDidNotComeHome()
    {
        Assert.Equal(0, VoyageOf().CrewLost);          // a voyage nobody died on says so
        Assert.Equal(3, VoyageOf(crewLost: 3).CrewLost);

        // …and the sentence under GETTING HOME stops being a lie. This is the third bug class stated as an
        // assertion: the line the crew wrote must not claim an intact roster over three empty bunks.
        string comment = Find(VoyageOf(crewLost: 3), CrewTemp.Dimension.Safety).Comment;
        Assert.DoesNotContain("Nobody has been lost", comment, StringComparison.Ordinal);
    }

    /// <summary>
    /// The fifth-bug-class companion: a reflection harness that silently set nothing would pass the test
    /// above on a page that reads a constant. So pin that every live input this guard writes actually
    /// reaches the voyage, and that the two the game still does not keep are still not kept.
    /// </summary>
    [Fact]
    public void TheHarnessIsReallyDrivingTheShippingSheet()
    {
        CrewTemp.Voyage v = VoyageOf(crewLost: 3, honestFilings: 4, profitableLies: 2, credits: 900,
                                     rumTots: 6, nearMisses: 5, heat: 2, vented: true);

        Assert.Equal(3, v.CrewLost);
        Assert.Equal(4, v.HonestFilings);
        Assert.Equal(2, v.ProfitableLies);
        Assert.Equal(900, v.SharePaid);
        Assert.Equal(6, v.TotsPoured);
        Assert.Equal(5, v.NearMisses);
        Assert.Equal(2, v.Heat);
        Assert.True(v.VentedOwnCompartment);

        // The hooks that are STILL honest zeroes. #1066 struck the third name off this list — shore leave IS
        // tracked now, in berths, and it reaches the sheet as PromisesBroken, which reads zero here only
        // because this harness leaves the crew freshly ashore (TheCrewSheetCountsTheStopsAshoreTests is what
        // drives that input). What is left dormant: nothing in the game makes a promise TO THE CREW and then
        // keeps it, and DaysSinceShoreLeave is the DAYS version of a thing the game counts in BERTHS —
        // deriving one from the other would be the invented number this sheet exists to refuse.
        Assert.Equal(0, v.PromisesKept);
        Assert.Equal(0, v.PromisesBroken);
        Assert.Equal(0, v.DaysSinceShoreLeave);
    }

    /// <summary>
    /// AND THE WORLD CAN ACTUALLY GET THERE — the half that makes the deputation's caller a caller rather
    /// than a scanner-satisfying gesture. The sweep is over the page's own voyage, so what it measures is
    /// the reach of the SHIPPED inputs, and it says: a Petition, and no further.
    ///
    /// <para>#1066 · <b>The harness leaves the crew freshly ashore</b> (<c>VoyageOf</c> never writes
    /// <c>_workingStopsSinceShoreLeave</c>), so what this sweep now measures is the reach of a captain WHO
    /// TAKES HIS CREW ASHORE — and the answer is still a deputation and no further. That is the kept half of
    /// #1066's vacuity pair; the other half, the same sweep with nobody ashore, lives next door in
    /// <see cref="TheCrewSheetCountsTheStopsAshoreTests"/> and reaches the ultimatum the meeting sits on.</para>
    /// </summary>
    [Fact]
    public void TheShippedInputsReachADeputationAndNoFurther()
    {
        CrewTemp.Standing worst = CrewTemp.Standing.Solid;
        CrewTemp.Voyage worstVoyage = default;

        foreach (int filings in new[] { 0, 1, 3, 6, 7, 10, 20, 40 })
        {
            foreach (int lies in new[] { 0, 1, 3, 10, 40 })
            {
                foreach (int credits in new[] { 0, 400, 5_000, 50_000 })
                {
                    foreach (int tots in new[] { 0, 10 })
                    {
                        foreach (int nearMisses in new[] { 0, 5 })
                        {
                            // 0…12: more than two full gigs' worth of the five the rock risks, so the claim
                            // is about the arithmetic's shape rather than about one gig's roster.
                            foreach (int lost in new[] { 0, 1, 2, 3, 5, 12 })
                            {
                                for (int heat = 0; heat <= EncounterRule.MaxHeatLevel; heat++)
                                {
                                    foreach (bool vented in new[] { false, true })
                                    {
                                        CrewTemp.Voyage v = VoyageOf(lost, filings, lies, credits, tots,
                                                                     nearMisses, heat, vented);
                                        CrewTemp.Standing s = CrewTemp.StandingOf(v);
                                        if (s > worst)
                                        {
                                            worst = s;
                                            worstVoyage = v;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        Assert.True(worst == CrewTemp.Standing.Petition,
            $"the shipped inputs reach {worst} ({worstVoyage}), not a Petition. BELOW it the deputation's " +
            "caller in Map.CrewTemp can never fire, and StoryBeats.Beat.CrewDeputation must go back on " +
            "EveryStoryBeatHasACallerTests.KnownOrphans; ABOVE it the deputation is no longer the worst " +
            "thing this sweep can build and the beat that IS should be raised here too (#663).");

        // …and the sweep is not a loop over one answer: the same page, kept well, is Solid.
        Assert.Equal(CrewTemp.Standing.Solid, CrewTemp.StandingOf(VoyageOf(credits: 50_000, rumTots: 10)));

        // …and it is the BODIES that bought the deputation. Take them out of the worst voyage the sweep
        // could build and the crew go back to talking in the galley.
        Assert.Equal(CrewTemp.Standing.Grumbling, CrewTemp.StandingOf(worstVoyage with { CrewLost = 0 }));
    }

    /// <summary>
    /// ONE EVENT, TWO REPORTERS — the same law the arc wire lives under (see
    /// <see cref="BothArcsBreakOnTheWireTests"/>). A crewman lost on the rock docks the fee AND is
    /// remembered by the crew, and both must come off the one increment: a second place that decides
    /// somebody died is a second bookkeeping system, and this house has a named bug class for those.
    /// </summary>
    [Fact]
    public void TheGigTellsTheCrewSheetAtTheSameSeamItDocksThePay()
    {
        string src = ClientSource("Map.Deflection.cs");

        Assert.Contains("ex.DeflectionCrewLost++", src, StringComparison.Ordinal);
        Assert.Contains("NoteCrewDidNotComeHome()", src, StringComparison.Ordinal);

        // …and they are the same seam, not two places that happen to agree. The note has to sit inside the
        // `if (c.CrewLost)` arm that does the increment.
        int at = src.IndexOf("ex.DeflectionCrewLost++", StringComparison.Ordinal);
        string arm = src.Substring(at, Math.Min(240, src.Length - at));
        Assert.Contains("NoteCrewDidNotComeHome()", arm, StringComparison.Ordinal);
    }

    /// <summary>
    /// AND YOU CAN REACH IT ON DEMAND. <i>"A scene nobody can reach on demand is a scene that ships
    /// broken"</i> — the boot's own rule, written beside the <c>?query=</c> readers. So the dev door is
    /// asked the only question that matters about it: does <c>/map?crew=petition</c> actually put the crew
    /// past the edge the deputation is raised on? The cheat's numbers are seeded by the SHIPPING method and
    /// read back through the SHIPPING sheet, so a tuned-away constant reddens this instead of quietly
    /// handing a playtester a URL that shows nothing.
    /// </summary>
    [Fact]
    public void TheDevDoorPutsTheCrewAtTheCaptainsDoor()
    {
        var map = new Pages.Map();
        MethodInfo seed = typeof(Pages.Map).GetMethod("SeedCrewCheat", Hidden)
            ?? throw new InvalidOperationException("Map has no SeedCrewCheat — /map?crew=petition has lost its seat.");
        seed.Invoke(map, ["petition"]);

        MethodInfo voyage = typeof(Pages.Map).GetMethod("CrewVoyage", Hidden)!;
        var seeded = (CrewTemp.Voyage)voyage.Invoke(map, [])!;

        Assert.True(seeded.CrewLost > 0, "?crew=petition left nobody on the rock");
        Assert.Equal(CrewTemp.Standing.Petition, CrewTemp.StandingOf(seeded));

        // …and it is not a cheat that would work on any voyage at all: drop the bodies it left behind and
        // the same seeded ship is merely grumbling. The dev door grants the two halves the edge needs.
        Assert.Equal(CrewTemp.Standing.Grumbling, CrewTemp.StandingOf(seeded with { CrewLost = 0 }));

        // The reader half: the URL the testing guide prints is the URL the parse answers to.
        Assert.Equal("petition", CrewCheatFrom("/map?crew=petition"));
        Assert.Equal("petition", CrewCheatFrom("/map?crew=deputation"));
        Assert.Null(CrewCheatFrom("/map?crew=marooning"));   // an edge nothing can reach is not a dev door
        Assert.Null(CrewCheatFrom("/map?oldcrew=1"));        // …and the key next to it is not this key
    }

    /// <summary>
    /// AND THE WHOLE CHAIN, END TO END, THROUGH THE SHIPPING SEAMS. The dev door seeds the counters, the
    /// ship's own clock reads the sheet, the standing crosses, and <c>RaiseStoryBeat</c> — with Core's
    /// cadence and presentation rules applied, and nothing faked in between — puts the deputation on the
    /// screen. Everything above this proves one link; this is the one that proves they are joined.
    ///
    /// <para>It also pins the two halves the beat exists for and a scanner cannot see: the card is CARD (a
    /// picture the captain reads, not a plate that slides past) and it speaks ONCE, because
    /// <c>StoryBeats.CadenceOf</c> says once ever and a crew's standing is a state that stays true — so
    /// without that rule this seam would raise a card on every frame for the rest of the voyage.</para>
    /// </summary>
    [Fact]
    public void TheShipsOwnClockPutsTheDeputationOnTheScreen()
    {
        var map = new Pages.Map();
        TheBootBuildsTheSameWorldTests.NeverRender(map);

        MethodInfo watch = typeof(Pages.Map).GetMethod("WatchWhereTheCrewStand", Hidden)
            ?? throw new InvalidOperationException("Map has no WatchWhereTheCrewStand — the deputation's caller is gone.");
        FieldInfo card = typeof(Pages.Map).GetField("_storyCard", Hidden)!;

        // A ship nobody died on says nothing, however many times she is asked.
        watch.Invoke(map, []);
        watch.Invoke(map, []);
        Assert.Null(card.GetValue(map));

        // …and then the dev door's voyage, read on the ship's own clock.
        typeof(Pages.Map).GetMethod("SeedCrewCheat", Hidden)!.Invoke(map, ["petition"]);
        watch.Invoke(map, []);

        object? raised = card.GetValue(map);
        Assert.NotNull(raised);
        Assert.Equal(StoryBeats.Beat.CrewDeputation,
            raised.GetType().GetField("Item1")!.GetValue(raised));

        // Once ever: dismiss it and keep asking, and the crew do not come back to the door.
        card.SetValue(map, null);
        for (int frame = 0; frame < 5; frame++)
        {
            typeof(Pages.Map).GetField("_nearMisses", Hidden)!.SetValue(map, frame); // the sheet keeps moving
            watch.Invoke(map, []);
        }

        Assert.Null(card.GetValue(map));
    }

    /// <summary>What <c>?crew=</c> answered, read through the shipping parse (the idiom
    /// <see cref="TheBootReadsTheSameQueryTests"/> uses on the whole holder).</summary>
    private static string? CrewCheatFrom(string url)
    {
        var map = new Pages.Map();
        TheBootBuildsTheSameWorldTests.NeverRender(map);
        TheBootBuildsTheSameWorldTests.Hand(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench(url));

        MethodInfo read = typeof(Pages.Map).GetMethod("ReadEveryQueryKey", Hidden)!;
        object q = read.Invoke(map, [new Uri("http://localhost" + url)])!;
        return (string?)q.GetType().GetField("CrewCheat", Hidden)!.GetValue(q);
    }

    /// <summary>The world's own half of that: the gig can actually kill somebody. A guard against a state
    /// nothing can enter is the vacuous world, and it is what the deputation's whole excuse turned on.</summary>
    [Fact]
    public void TheGigCanActuallyLoseACrewman()
    {
        var lost = new List<string>();

        foreach (RockComposition composition in Enum.GetValues<RockComposition>())
        {
            var type = new RockType(composition);
            for (int ordinal = 1; ordinal <= 40; ordinal++)
            {
                ulong seed = DeflectionGig.Seed(1_000.0 + ordinal, $"rock-{composition}", ordinal);
                if (DeflectionGig.Roll(seed, type, ordinal).CrewLost)
                {
                    lost.Add($"{composition}#{ordinal}");
                }
            }
        }

        Assert.True(lost.Count > 0,
            "no rolled complication in the shipped gig loses a crewman — the crew sheet's CrewLost input is " +
            "back to being a number nobody keeps, and the deputation's caller can never fire (#663)");
    }

    private static CrewTemp.Reading Find(in CrewTemp.Voyage v, CrewTemp.Dimension d)
    {
        foreach (CrewTemp.Reading r in CrewTemp.Readings(v))
        {
            if (r.Dimension == d)
            {
                return r;
            }
        }

        throw new InvalidOperationException($"no reading for {d}");
    }
}
