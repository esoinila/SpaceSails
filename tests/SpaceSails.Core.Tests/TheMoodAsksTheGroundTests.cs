using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #867 · THE MOOD ASKS THE GROUND — a canon sweep of every prose line a captain can be told while standing
/// on ground that holds its own air.
///
/// <para>Owner, 2026-08-13, strolling the B1 park: <i>"Our mood texts still assume the vacuum of the surface
/// btw :-D ... talk of nothing carrying the sound here as we are literally taking a walk in a park :-D"</i>
/// His nerve ledger, on a lawn under grow-lights, kept saying <i>"the airlock closes behind you +1"</i> and
/// <i>"a cold breath through the hull −1"</i>.</para>
///
/// <h3>The law, and why it is a sweep and not four assertions</h3>
///
/// <para>The law is #802's row law extended to mood: <b>a mood sentence is a claim about the room</b>, so
/// every ambience / ledger / shudder line that names vacuum furniture must ask the same fact the lift
/// panel's rows ask — <see cref="UndergroundComplex.HoldsPressure"/> — and speak the register the ground
/// earns. Same mechanics, different sentence.</para>
///
/// <para>Four assertions would pin the four sentences the owner happened to read. The thing that actually
/// went wrong is structural and it will go wrong again: a floor of the Hive is laid inside the SURFACE'S OWN
/// coordinate envelope, so every predicate in the mood seam that means "outdoors" is satisfied by a lit
/// gallery two hundred deck units down. Any pool wired to one of those predicates speaks in the park, today
/// or the day after somebody adds one. So this walks the whole reachable set instead, and the failure counts
/// what it found: <c>N vacuum lines reachable on pressurised ground</c>.</para>
///
/// <h3>What "reachable on pressurised ground" means, exactly</h3>
///
/// <list type="bullet">
/// <item><b>The nerve ledger</b> — every <see cref="NervePips.Cause"/>'s words, asked with the ground's own
/// fact. The ease is the one that forked; the rest are swept because the sweep must not know in advance
/// which one is the offender.</item>
/// <item><b>The ambient hull-shudder</b>, ordinary pool, in whichever voice a pressurised floor selects. Not
/// a hand-picked <see cref="HullShudder.Setting"/>: the selector itself is asked, because "which pool does
/// the park get" IS the bug.</item>
/// <item><b>The chill escalation</b> — its pool and its nerve-ledger label. Reachable down here whenever the
/// floor carries a lab or the KAAMOS arc has gone deep.</item>
/// </list>
///
/// <para>Two ambient siblings are deliberately NOT in the set, and the reason is a gate, not an oversight:
/// the unexplained signal and the caution PA both require <c>_surface is null</c> (a populated interior with
/// staff and a public-address system), so neither can fire on any landing at all — <c>"heavy weather on the
/// hull"</c> is unreachable from the park. Nor is <c>HullVenting.NoAirToCarryThemOut</c>: that is a
/// derelict's flooded corridor, a world with no Hive floors in it.</para>
/// </summary>
public sealed class TheMoodAsksTheGroundTests
{
    /// <summary>Standing on a floor that <see cref="UndergroundComplex.HoldsPressure"/> says yes about —
    /// the B1 park under grow-lights, spelled once so every call below reads as the place it is about.</summary>
    private const bool InThePark = true;

    /// <summary>Out on the regolith in a suit — the ground every one of these lines was written for.</summary>
    private const bool OutOnTheSurface = false;

    /// <summary>One line the game can say, and the family it comes from — the family is carried so a red
    /// message names where to go and not merely what was said.</summary>
    private readonly record struct MoodLine(string Family, string Line);

    // ── THE REACHABLE SET ─────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<MoodLine> EveryMoodLineReachableInThePark()
    {
        foreach (NervePips.Cause cause in Enum.GetValues<NervePips.Cause>())
        {
            yield return new($"nerve ledger · {cause}", NervePips.Name(cause, groundHoldsPressure: InThePark));
        }

        // ASK THE SELECTOR, DO NOT NAME THE POOL. The whole bug was that the park selected the surface's
        // voice, so a sweep that reached for Setting.Pressurised by hand would be grading its own homework.
        HullShudder.Setting inThePark = HullShudder.SettingOutside(
            groundHoldsPressure: InThePark, onRegolith: true, deepSite: true, haven: false);
        foreach (string line in HullShudder.LinesFor(inThePark))
        {
            yield return new($"hull-shudder · {inThePark}", line);
        }

        foreach (string line in HullShudder.ChillLinesFor(groundHoldsPressure: InThePark))
        {
            yield return new("hull-shudder · chill", line);
        }

        yield return new("hull-shudder · chill nerve ledger",
            HullShudder.ChillNerveLabel(groundHoldsPressure: InThePark));
    }

    // ── THE FORBIDDEN VOCABULARY ──────────────────────────────────────────────────────────────────────

    /// <summary>Vacuum furniture: the things a sentence may only name where there is no air.
    ///
    /// <para>The issue names five (hull, airlock, nothing-to-carry-the-sound, suit, tank). Two more are
    /// here because the line the owner actually walked past uses them and nothing else in the list catches
    /// it — <i>"In vacuum it is entirely silent"</i> and <i>"Dust lifts off the regolith"</i>.</para>
    ///
    /// <para>Every pattern is anchored on WORD boundaries, so a longer word that merely contains one of
    /// these is not a hit: <c>pursuit</c> is not a suit and <c>tankard</c> is not a tank. The phrases are
    /// matched as phrases for the same reason — "carry" alone would redden half the prose in the game.</para>
    /// </summary>
    private static readonly (string Pattern, string Furniture)[] VacuumFurniture =
    [
        (@"\bhulls?\b", "a hull"),
        (@"\bairlocks?\b", "an airlock"),
        (@"\bvacuum\b", "vacuum"),
        (@"\bregolith\b", "the regolith"),
        (@"\bsuits?\b", "a suit"),
        (@"\btanks?\b", "a tank"),
        (@"\b(nothing|not\s+enough)\b[^.!?]{0,60}\bto\s+carry\b", "nothing to carry the sound"),
        (@"\bto\s+carry\s+(a|the)\s+sound\b", "nothing to carry the sound"),
    ];

    /// <summary>THE ALLOW-LIST, AND WHY IT IS EMPTY.
    ///
    /// <para>Not every use of a forbidden word would be a lie down here. <i>"the hull is holding"</i> is
    /// arguably fine in a sealed pressurised site that HAS a hull, and a captain on a Hive floor is still
    /// wearing a suit even where they do not need it. The ruling taken on #867 is that neither earns an
    /// exemption in this build, for one reason: <b>a mood line naming vacuum furniture on ground that
    /// breathes is telling the player something about where they are, and the something is wrong even when
    /// the noun is technically present.</b> The park has no hull. The suit is on your back and is not what
    /// is keeping you alive on this floor.</para>
    ///
    /// <para>So the list is empty on purpose, and it is a METHOD rather than an empty array so that adding
    /// an entry costs a sentence explaining the exemption. If a future line genuinely needs one, it goes
    /// here with its reason — not into the vocabulary above, which would silently unguard every other
    /// family at once.</para></summary>
    private static bool IsAllowedDespiteTheWord(MoodLine _) => false;

    /// <summary>PARK-ONLY FURNITURE — the same bug one scale down, and the one this pass was sent back for.
    ///
    /// <para><see cref="HullShudder.Setting.Pressurised"/> is not the park's voice. It is the voice of EVERY
    /// floor that holds its own air: the lab, the office run, the cold store, the corridor, the canteen. Two
    /// of the lines first written for it rang the water in a planting basin and stirred the leaves on a
    /// growing bed — true on the lawn the owner was walking, and a claim about a room that is a lie in the
    /// machine shop one floor down.</para>
    ///
    /// <para>So the sweep has a second vocabulary. The first list catches a sentence that thinks it is
    /// outdoors; this one catches a sentence that thinks every indoors is a garden. The furniture these
    /// pools may name is the furniture PRESSURE ITSELF implies — air, fans, lights, a door on its stop, dust
    /// — and anything a particular room merely happens to contain belongs to that room's own prose, not to
    /// an ambient pool that plays on all of them.</para></summary>
    private static readonly (string Pattern, string Furniture)[] ParkOnlyFurniture =
    [
        (@"\bgrow[- ]?lights?\b", "grow-lights"),
        (@"\bbasins?\b", "a basin"),
        (@"\bbeds?\b", "a growing bed"),
        (@"\bleaves\b", "leaves"),
        (@"\bgravel\b", "gravel"),
        (@"\blawns?\b", "a lawn"),
    ];

    /// <summary>Nothing in the pools that play on EVERY breathing floor may name a fixture only one kind of
    /// floor has. Swept over the ambient pools rather than the whole reachable set, because a nerve-ledger
    /// line fired BY a park bench is entitled to mention one — the law is about ambience that plays
    /// everywhere, not about prose that belongs to a place.</summary>
    [Fact]
    public void NoAmbientLineOnBreathingGroundNamesFurnitureOnlyOneKindOfFloorHas()
    {
        var offenders = new List<string>();
        IEnumerable<MoodLine> ambient =
            HullShudder.LinesFor(HullShudder.Setting.Pressurised)
                .Select(l => new MoodLine("hull-shudder · Pressurised", l))
                .Concat(HullShudder.ChillLinesFor(groundHoldsPressure: InThePark)
                    .Select(l => new MoodLine("hull-shudder · chill", l)));

        foreach (MoodLine mood in ambient)
        {
            IReadOnlyList<string> found = ParkOnlyFurniture
                .Where(f => Regex.IsMatch(mood.Line, f.Pattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                .Select(f => f.Furniture)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (found.Count > 0)
            {
                offenders.Add($"  {mood.Family} names {string.Join(", ", found)} — which the lab, the cold "
                    + $"store and the corridor do not have: \"{mood.Line}\"");
            }
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} ambient lines on pressurised ground name park-only furniture"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static IReadOnlyList<string> FurnitureIn(string line) =>
        VacuumFurniture
            .Where(f => Regex.IsMatch(line, f.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .Select(f => f.Furniture)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    // ── THE SWEEP ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE FINDING THE OWNER ASKED FOR. Walk everything the park can say and count what still
    /// speaks of vacuum. Zero is the only passing number; the message says how far from it the build is and
    /// prints the offenders with their families, so a red run is a work list rather than a riddle.</summary>
    [Fact]
    public void NotOneMoodLineReachableInTheParkNamesVacuumFurniture()
    {
        var offenders = new List<string>();
        foreach (MoodLine mood in EveryMoodLineReachableInThePark())
        {
            if (IsAllowedDespiteTheWord(mood))
            {
                continue;
            }

            IReadOnlyList<string> furniture = FurnitureIn(mood.Line);
            if (furniture.Count > 0)
            {
                offenders.Add($"  {mood.Family} names {string.Join(", ", furniture)}: \"{mood.Line}\"");
            }
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} vacuum lines reachable on pressurised ground"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>AND THE PARK IS NOT MERELY SILENT. A pool emptied of vacuum words would pass the sweep above
    /// and say nothing; the register the ground earns is the actual deliverable. Every line the park can
    /// speak is a line, and the ambient pool hears the sound ARRIVE rather than explaining why it cannot.</summary>
    [Fact]
    public void TheParkSpeaksItsOwnRegisterAndNotAnEmptyOne()
    {
        var park = EveryMoodLineReachableInThePark().ToList();
        Assert.NotEmpty(park);
        Assert.All(park, m => Assert.False(string.IsNullOrWhiteSpace(m.Line), $"{m.Family} says nothing."));

        HullShudder.Setting setting = HullShudder.SettingOutside(
            groundHoldsPressure: InThePark, onRegolith: true, deepSite: true, haven: false);
        IReadOnlyList<string> pool = HullShudder.LinesFor(setting);
        Assert.True(pool.Count >= 3, "one ambient line repeated is not a mood, it is a notification.");
        Assert.Equal(pool.Count, pool.Distinct(StringComparer.Ordinal).Count());

        // The owner's own sentence for this family, verbatim, because he authored it on the issue and lore
        // is his. A pool that quietly paraphrased it would still pass every other assertion in this file.
        Assert.Contains("The shudder arrives through the floor, and this time the room hears it with you.",
            pool);

        // The two forked ledger lines, verbatim, for the same reason.
        Assert.Equal("warm air and standing lights - the floor is holding",
            NervePips.Name(NervePips.Cause.Airlock, groundHoldsPressure: InThePark));
        Assert.Equal("a cold draught from somewhere the fans do not reach",
            HullShudder.ChillNerveLabel(groundHoldsPressure: InThePark));
    }

    /// <summary>THE GROUND ANSWERS FIRST. Whatever else is true of a landing — regolith, a deep site, a
    /// docked haven — a floor that holds its own air is not a place where nothing carries the sound, so the
    /// pressure fact wins the toss outright. This is the half that makes the fix reachable at all: the park
    /// satisfies <c>onRegolith</c> and <c>deepSite</c> simultaneously.</summary>
    [Fact]
    public void PressureWinsTheTossOverEveryOtherFlag()
    {
        foreach (bool regolith in new[] { false, true })
        {
            foreach (bool deep in new[] { false, true })
            {
                foreach (bool haven in new[] { false, true })
                {
                    Assert.Equal(HullShudder.Setting.Pressurised,
                        HullShudder.SettingOutside(InThePark, regolith, deep, haven));
                }
            }
        }
    }

    /// <summary>AND VACUUM GROUND KEEPS EVERY LINE IT HAD, BYTE FOR BYTE. #867 forked the sentences; it did
    /// not edit them. The surface's own voice is the thing #590 fought for and the thing a captain in a suit
    /// is owed, and the cheapest way to break it is to "improve" it while forking it.</summary>
    [Fact]
    public void TheSurfaceKeepsItsOwnWordsUntouched()
    {
        Assert.Equal("the airlock closes behind you",
            NervePips.Name(NervePips.Cause.Airlock, groundHoldsPressure: OutOnTheSurface));
        Assert.Equal("the airlock closes behind you", NervePips.Name(NervePips.Cause.Airlock));
        Assert.Equal("a cold breath through the hull",
            HullShudder.ChillNerveLabel(groundHoldsPressure: OutOnTheSurface));

        // The regolith pool is still allowed — required, in fact — to say the thing the park may not.
        IReadOnlyList<string> surface = HullShudder.LinesFor(HullShudder.Setting.Regolith);
        Assert.Contains(surface, l => FurnitureIn(l).Contains("nothing to carry the sound"));
        Assert.Contains(surface, l => FurnitureIn(l).Contains("vacuum"));

        // And the two grounds share no words at all — a shared line is a line that cannot be about a room.
        IReadOnlyList<string> park = HullShudder.LinesFor(HullShudder.Setting.Pressurised);
        Assert.Empty(park.Intersect(surface, StringComparer.Ordinal));
        Assert.Empty(HullShudder.ChillLinesFor(InThePark)
            .Intersect(HullShudder.ChillLinesFor(OutOnTheSurface), StringComparer.Ordinal));
    }

    /// <summary>THE MECHANICS DID NOT MOVE. The whole ruling is "same mechanics, different sentence", so the
    /// ease still costs what it cost, beats when it beat, and is still the same named cause — a fork that
    /// quietly repriced the recovery would be a balance change wearing a prose change's clothes.</summary>
    [Fact]
    public void OnlyTheSentenceForked_NotThePip()
    {
        Assert.Equal(-NervePips.BeatPips, NervePips.Cost(NervePips.Cause.Airlock));
        Assert.Equal(NervePips.AirlockBeatSeconds, NervePips.BeatSeconds(NervePips.Cause.Airlock));
        Assert.True(NervePips.IsSustained(NervePips.Cause.Airlock));

        // Driven, not read: the same safe frame on both grounds gives back the same pip and differs in one
        // thing only — what the ledger says about it.
        static NervePips.Event EaseOn(bool pressurised)
        {
            var frame = new NervePips.Frame(
                OnExcursion: true, OnRegolith: false, SeesMonolith: false,
                Stressors: default, FreshSightings: 0, Touched: false,
                DtSeconds: NervePips.AirlockBeatSeconds, SafeGroundHoldsPressure: pressurised);
            NervePips.Step step = NervePips.Advance(
                NervePips.FromPips(NervePips.MaxPips - 2), monolithSeen: false, NervePips.Beats.Fresh, in frame);
            return Assert.Single(step.Events);
        }

        NervePips.Event park = EaseOn(InThePark);
        NervePips.Event surface = EaseOn(OutOnTheSurface);

        Assert.Equal(NervePips.Cause.Airlock, park.Cause);
        Assert.Equal(surface.Cause, park.Cause);
        Assert.Equal(surface.Delta, park.Delta);
        Assert.Equal("warm air and standing lights - the floor is holding", park.Label);
        Assert.Equal("the airlock closes behind you", surface.Label);
        Assert.Empty(FurnitureIn(park.Line));
    }
}
