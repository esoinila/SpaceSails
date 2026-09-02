using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1066 · THE PROMISE THE GAME DID NOT MAKE.
///
/// <para><c>StoryBeats.Beat.CrewMeeting</c> shipped with a painted canvas, a caption and a cadence, and
/// #1065 left it deliberately caller-less: its edge is <see cref="CrewTemp.Standing.Ultimatum"/>, and an
/// ultimatum needs <i>a broken promise TO THE CREW or months without shore leave</i> — two numbers nobody
/// kept. This is the arithmetic half of the number that now exists.</para>
///
/// <para><b>The rule, and why it is this one.</b> A clamp is a <b>run ashore</b> only at a great port —
/// <see cref="ArrivalTube.Tier.GreatPort"/>, the tier the arrival tube already derives from the scenario's
/// own traffic model. Every other clamp is a <b>working stop</b>. The captain is never told this in a new
/// sentence, because the game already tells him in the picture it raises at that very clamp: the long glazed
/// gangway with <i>"two streams of people … and nobody who has any idea who you are"</i> is the only berth
/// a crew can disappear into for a night, and <i>"one rigid tube … two dock workers waiting for you to get
/// out of the way"</i> is a shift, not a run ashore. One tier read, two readers — the establishing shot and
/// the ledger line — which is #1065's one-event law.</para>
///
/// <para><b>Why the WORD and not REST.</b> <see cref="CrewTemp.Dimension.TheWord"/>'s own doc-comment lists
/// the promises it is made of and names this one first: <i>"a run ashore, a share, a rescue"</i>. So an
/// overdue run ashore is a broken promise, weighted three times a kept one, which is the dimension that
/// <i>"actually ends captaincies"</i> — exactly the edge the meeting sits on.
/// <see cref="CrewTemp.Voyage.DaysSinceShoreLeave"/> stays dormant on purpose: the game counts STOPS, and
/// converting stops into days to move REST would be a number invented to fill a bar.</para>
/// </summary>
public sealed class ShoreLeaveIsALedgerLineTests
{
    // ===== 1 · The dial, and that it can say both things =====

    /// <summary>
    /// THE LINE IS WHERE THE DIAL SAYS IT IS. Read off the constant rather than typed, so the owner turning
    /// <see cref="CrewTemp.WorkingStopsBetweenRunsAshore"/> moves the test with the game instead of reddening
    /// it — and the pair at the bottom is the vacuity floor: a yardstick that answered "broken" to every
    /// input would pass the top half alone.
    /// </summary>
    [Fact]
    public void TheWordHoldsUntilTheStopsPassTheLine()
    {
        int line = CrewTemp.WorkingStopsBetweenRunsAshore;
        Assert.True(line > 1, "a line at one working stop is not a promise, it is a leash");

        // Every stop up to the line is a ship still keeping its word.
        for (int stops = 0; stops < line; stops++)
        {
            Assert.Equal(0, CrewTemp.ShoreLeavePromisesBroken(stops));
        }

        // The stop that passes it breaks the word once, and every stop kept aboard past it breaks it again —
        // a grievance that compounds while it is not fixed, which is how an overdue thing behaves.
        Assert.Equal(1, CrewTemp.ShoreLeavePromisesBroken(line));
        Assert.Equal(2, CrewTemp.ShoreLeavePromisesBroken(line + 1));
        Assert.Equal(6, CrewTemp.ShoreLeavePromisesBroken(line + 5));

        // …and a counter that has gone backwards (a save from a build with a different dial) is not a debt.
        Assert.Equal(0, CrewTemp.ShoreLeavePromisesBroken(-3));
    }

    /// <summary>The sheet's own line moves with the counter, and says the two different things it has to
    /// say: how many stops are left, and that the word is broken. A line that read the same at both ends
    /// would be a bar that does not move.</summary>
    [Fact]
    public void TheSheetSaysWhereTheLineIsAndWhetherItHasBeenCrossed()
    {
        int line = CrewTemp.WorkingStopsBetweenRunsAshore;

        string kept = CrewTemp.ShoreLeaveLine(0);
        string due = CrewTemp.ShoreLeaveLine(line - 1);
        string broken = CrewTemp.ShoreLeaveLine(line + 1);

        Assert.NotEqual(kept, due);
        Assert.NotEqual(due, broken);
        Assert.All([kept, due, broken], s => Assert.False(string.IsNullOrWhiteSpace(s)));

        // The captain is told where the line is before he is told he has crossed it (#761).
        Assert.Contains(line.ToString(System.Globalization.CultureInfo.InvariantCulture), due,
            System.StringComparison.Ordinal);
        Assert.Contains("overdue", broken, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("overdue", kept, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// THE RULE IS NOT VACUOUS IN THE SHIPPED SYSTEM, which is the world's half of the dial and the fifth
    /// bug class's favourite hiding place. A rule that answered the same thing about every berth would pass
    /// every assertion in this file and mean nothing: with no great port in the game the counter could never
    /// be cleared and the crew would be marooned aboard by arithmetic; with nothing but great ports it could
    /// never rise and <c>StoryBeats.Beat.CrewMeeting</c>'s caller could never fire.
    ///
    /// <para>Asked of the REAL <c>sol.json</c> through the shipping tier function, the way
    /// <see cref="ArrivalTubeTests"/> asks it — a fixture would let the rule and the scenario drift apart,
    /// which is the whole reason the tier is derived rather than authored.</para>
    /// </summary>
    [Fact]
    public void TheShippedSystemHasBothARunAshoreAndAWorkingStop()
    {
        ICelestialEphemeris sol = CircularOrbitEphemeris.FromScenario(ScenarioLoader.LoadFile(
            System.IO.Path.Combine(System.AppContext.BaseDirectory, "scenarios", "sol.json")));

        var ashore = new System.Collections.Generic.List<string>();
        var working = new System.Collections.Generic.List<string>();

        foreach (CelestialBody body in sol.Bodies)
        {
            if (!DockableHavens.IsDockable(body))
            {
                continue;   // a moon with a haven on it is not a berth you throw a clamp onto
            }

            (ArrivalTube.TierFor(sol, body.Id) == ArrivalTube.Tier.GreatPort ? ashore : working).Add(body.Name);
        }

        Assert.True(ashore.Count > 0,
            "no berth a captain can clamp onto in sol.json is a great port, so the shore-leave counter can " +
            "never be cleared — the crew are marooned aboard by arithmetic (#1066)");
        Assert.True(working.Count > 0,
            "every berth a captain can clamp onto in sol.json is a great port, so the counter can never " +
            "rise and StoryBeats.Beat.CrewMeeting's caller can never fire (#1066)");
    }

    // ===== 2 · The edge, with its vacuity pair =====

    /// <summary>
    /// THE PAIR THIS LANE EXISTS FOR. The same voyage twice: one that has been ashore, one that has not.
    /// The first must never convene the meeting; the second must. Without both halves the guard would pass
    /// on a game that raised the beat for everybody, and on a game that raised it for nobody.
    /// </summary>
    [Fact]
    public void AnOverdueRunAshoreIsWhatCrossesTheUltimatumAndAnHonestOneNeverDoes()
    {
        // A poor, honest, bereaved ship — the voyage #1065 pinned as reaching a Petition and no further.
        var voyage = new CrewTemp.Voyage(HonestFilings: 12, CrewLost: 5);
        Assert.Equal(CrewTemp.Standing.Petition, CrewTemp.StandingOf(voyage));

        // WITH shore leave kept, no number of asks changes that. The counter is at zero because the last
        // berth was a great port, and the crew have nothing new to say.
        for (int stops = 0; stops < CrewTemp.WorkingStopsBetweenRunsAshore; stops++)
        {
            CrewTemp.Voyage ashore = voyage with
            {
                PromisesBroken = CrewTemp.ShoreLeavePromisesBroken(stops),
            };
            Assert.True(CrewTemp.StandingOf(ashore) < CrewTemp.Standing.Ultimatum,
                $"{stops} working stops is inside the line and must not convene the meeting");
        }

        // WITHOUT it, the word breaks and keeps breaking until the standing crosses. The count is measured
        // rather than typed, so it is the arithmetic that names the price and not this test.
        int crossedAt = -1;
        for (int stops = 0; stops <= 40; stops++)
        {
            CrewTemp.Voyage adrift = voyage with
            {
                PromisesBroken = CrewTemp.ShoreLeavePromisesBroken(stops),
            };
            if (CrewTemp.StandingOf(adrift) >= CrewTemp.Standing.Ultimatum)
            {
                crossedAt = stops;
                break;
            }
        }

        Assert.True(crossedAt > 0,
            "no number of working stops crosses the Ultimatum edge on a ship the shipped game can build — " +
            "StoryBeats.Beat.CrewMeeting is back to being a caller that can never fire (#1066)");
        Assert.True(crossedAt > CrewTemp.WorkingStopsBetweenRunsAshore,
            $"the meeting convenes at {crossedAt} stops, on the first broken promise — an ultimatum should " +
            "cost more than one missed run ashore");
    }

    /// <summary>
    /// AND IT IS NOT A SECOND DIAL THAT OVERRIDES THE SHEET. A well-fed, safe, well-paid ship that has not
    /// been ashore in a hundred stops still never reaches an ultimatum: THE CAPTAIN'S WORD floors at zero
    /// and the other four dimensions carry it. That is the design — the meeting is convened by a captain who
    /// is failing his crew in several ways at once, and shore leave alone is a grumble.
    /// </summary>
    [Fact]
    public void NoRunAshoreAloneNeverMaroonsAGoodCaptain()
    {
        for (int stops = 0; stops <= 100; stops++)
        {
            var kept = new CrewTemp.Voyage(
                SharePaid: 20_000, TotsPoured: 10, NearMisses: 1,
                PromisesBroken: CrewTemp.ShoreLeavePromisesBroken(stops));

            Assert.True(CrewTemp.StandingOf(kept) < CrewTemp.Standing.Ultimatum,
                $"a ship that pays, feeds and brings everybody home reached {CrewTemp.StandingOf(kept)} on " +
                $"{stops} working stops — shore leave has become a dial that overrides the whole sheet");
        }

        // …and the pair, so the sweep above is not a loop over one answer: the SAME counter on the poor,
        // bereaved ship does convene it.
        Assert.Equal(CrewTemp.Standing.Ultimatum, CrewTemp.StandingOf(
            new CrewTemp.Voyage(HonestFilings: 12, CrewLost: 5,
                                PromisesBroken: CrewTemp.ShoreLeavePromisesBroken(100))));
    }

    /// <summary>A run ashore is forgiveness, not amnesia's opposite: the counter resets at a great port, so
    /// the standing recovers. The temperature is what the crew are saying NOW — the sheet's own words, <i>"it
    /// is what the crew said when he was not in the room"</i> — and a grievance that is being fixed stops
    /// being a live grievance.</summary>
    [Fact]
    public void AGreatPortPutsTheWordBackTogether()
    {
        var adrift = new CrewTemp.Voyage(HonestFilings: 12, CrewLost: 5,
                                         PromisesBroken: CrewTemp.ShoreLeavePromisesBroken(100));
        Assert.Equal(CrewTemp.Standing.Ultimatum, CrewTemp.StandingOf(adrift));

        CrewTemp.Voyage ashore = adrift with { PromisesBroken = CrewTemp.ShoreLeavePromisesBroken(0) };
        Assert.True(CrewTemp.StandingOf(ashore) < CrewTemp.Standing.Ultimatum);
    }

    // ===== 3 · The counter rides the vault, and the bytes prove it =====

    /// <summary>
    /// <b>THE SAVE-COMPAT PROOF, IN BYTES</b> — #1057/#1072's recipe. <see cref="LegacyProgressVault"/> is
    /// not a hand-written fixture: it is the exact output of <see cref="VaultSerializer.Save"/> on the build
    /// immediately before #1066 (base 9d146f7), checksum and all, and it carries a <c>progress</c> section
    /// so the claim is about the section this lane actually touched. This build must load it and write it
    /// back CHARACTER FOR CHARACTER.
    ///
    /// <para>That is why the counter is a <c>int?</c> under
    /// <c>JsonIgnore(WhenWritingNull)</c> and the client hands over <c>null</c> whenever nobody is counting:
    /// the checksum is taken over the payload, so one extra
    /// <c>"workingStopsSinceShoreLeave": 0</c> would change the digest of every save ever made and hang the
    /// 📛 tampered marker on an honest voyage.</para>
    /// </summary>
    [Fact]
    public void ALegacyVaultRoundTripsByteForByteAcrossTheShoreLeaveLine()
    {
        // The writer indents with the platform's newline, so the fixture is read in the platform's newline
        // too — the only normalization, and it is the checked-out file's line endings, not anything of ours.
        string legacy = LegacyProgressVault.ReplaceLineEndings();

        Vault loaded = VaultSerializer.Load(legacy);

        Assert.False(loaded.Tampered);                                  // the digest still validates as written
        Assert.NotNull(loaded.Progress);                                // and the section this lane touched is really in it
        Assert.Null(loaded.Progress!.WorkingStopsSinceShoreLeave);      // an old voyage counted nothing
        Assert.True(loaded.Progress.TutorialPlayed);                    // …and the rest of it survived

        string rewritten = VaultSerializer.Save(loaded);
        Assert.Equal(legacy, rewritten);
        Assert.DoesNotContain("workingStopsSinceShoreLeave", rewritten, System.StringComparison.Ordinal);
    }

    /// <summary>…and the other half, so the field is not merely unwritten but unwritten FOR A REASON: a
    /// voyage that IS counting carries it, and it comes back the same.</summary>
    [Fact]
    public void ACounterThatISRunningRidesTheVault()
    {
        Vault legacy = VaultSerializer.Load(LegacyProgressVault.ReplaceLineEndings());
        var before = new Vault
        {
            Version = legacy.Version,
            SavedSimTime = legacy.SavedSimTime,
            Purse = legacy.Purse,
            Progress = legacy.Progress! with { WorkingStopsSinceShoreLeave = 7 },
        };

        string written = VaultSerializer.Save(before);
        Assert.Contains("\"workingStopsSinceShoreLeave\": 7", written, System.StringComparison.Ordinal);

        Vault after = VaultSerializer.Load(written);
        Assert.False(after.Tampered);
        Assert.Equal(7, after.Progress!.WorkingStopsSinceShoreLeave);
    }

    /// <summary>A progress section carrying no count at all — the shape a forward-compatible writer could
    /// hand back — reads as "nobody is counting", never as "ashore last berth" dressed up as a fact.</summary>
    [Fact]
    public void AProgressSectionWithNoCountIsNotAShipThatWasAshore()
    {
        var bare = new ProgressSection();
        Assert.Null(bare.WorkingStopsSinceShoreLeave);
        Assert.Equal(0, CrewTemp.ShoreLeavePromisesBroken(bare.WorkingStopsSinceShoreLeave ?? 0));
    }

    // Captured from the build at 9d146f7 (pre-#1066). Verbatim — do not reformat, the whole point is the bytes.
    private const string LegacyProgressVault = """
        {
          "version": 1,
          "savedSimTime": 1728000,
          "sections": {
            "purse": {
              "credits": 4250
            },
            "ship": {
              "reactionMassPulses": 0,
              "slugAmmo": 4,
              "missileAmmo": 1,
              "sentryMagazines": [
                99,
                99
              ]
            },
            "heat": {
              "level": 1,
              "raisedAtSimTime": 900000
            },
            "progress": {
              "tutorialPlayed": true,
              "ringsideSaved": false,
              "secretLabsFound": [],
              "groundLessonSeen": true,
              "groundGrewSeen": false,
              "tubeRearmSeen": false,
              "airCardSeen": true,
              "oddBooksRead": []
            },
            "nerve": {
              "nerve": 61.5,
              "monolithSeen": true
            },
            "logbook": {
              "captainName": "Ines Varga",
              "title": "the long way round",
              "note": "tank dry off Ceres"
            }
          },
          "checksum": "82953eb6c2032ebd191666eed8b52ccdec98999022fcce8c8aa839d347053ca1"
        }
        """;
}
