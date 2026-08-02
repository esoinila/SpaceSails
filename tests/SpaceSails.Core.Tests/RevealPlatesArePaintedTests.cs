using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #528 · THE PLATES ARE REAL — the guard behind the reveal-card lane.
///
/// <para>A reveal plate is a title, a painting and a caption. The house degradation law says the code ships
/// first and the JPG drops in behind it, and the <c>onerror</c>-hide is what makes that safe — which is
/// exactly why a plate pointed at a file nobody ever painted is INVISIBLE in the browser. It does not throw,
/// it does not log, it does not draw a broken frame. It simply shows a card with a hole in it, forever.</para>
///
/// <para>That is not hypothetical. <c>DeathNarration.ArtFile</c> answered <c>death-suffocated.jpg</c> for a
/// year and <b>the game never shipped that file</b> (#621 found it, #636 fixed it). The name was a promise
/// nothing had to keep, and nothing in the build could tell. So the art has to be checked somewhere that can
/// fail — this is that place, for every art constant the story lane owns.</para>
///
/// <para>The csproj copies the story art beside this assembly on every build (the <c>CssZBandSyncTests</c>
/// idiom), so the test reads the LIVE art directory rather than a snapshot.</para>
///
/// <para><b>Proven RED:</b> point <c>KaamosLore.PlateFor("cold-pod")</c> at <c>art/kaamos-cold-pod-nope.jpg</c>
/// and <see cref="EveryPlateNamesAPaintingThatExists"/> fails with the missing basename; rename a plate's key
/// to <c>"cold-pods"</c> and <see cref="EveryPlateIsKeyedToARealFragment"/> fails. Both were watched go red
/// before this shipped, and the death sweep goes red on the pre-#636 mapping.</para>
/// </summary>
public class RevealPlatesArePaintedTests
{
    /// <summary>Where the build drops the story art — see the csproj's artsource ItemGroup.</summary>
    private static string ArtDir => Path.Combine(AppContext.BaseDirectory, "artsource");

    /// <summary>Both arcs' plates in one sweep, tagged with the arc that owns them so a failure names it.</summary>
    private static IEnumerable<(string Arc, string Id, RevealPlate Plate)> AllPlates()
    {
        foreach ((string id, RevealPlate plate) in KaamosLore.AllPlates)
        {
            yield return ("KAAMOS", id, plate);
        }

        foreach ((string id, RevealPlate plate) in NebulaLore.AllPlates)
        {
            yield return ("NEBULA", id, plate);
        }
    }

    /// <summary>Assert one <c>art/…</c> constant names a file that is actually on disk.</summary>
    private static void AssertPainted(string what, string artFile)
    {
        Assert.False(string.IsNullOrWhiteSpace(artFile), $"{what} names no art at all.");
        string file = Path.Combine(ArtDir, Path.GetFileName(artFile));
        Assert.True(
            File.Exists(file),
            $"{what} names {artFile}, which is not in wwwroot/art. The onerror-hide law means a missing " +
            "painting NEVER shows up in the browser — it just leaves a hole in the card.");
    }

    [Fact]
    public void TheArcsDeclarePlatesAtAll()
    {
        // A guard that passes on an empty collection is a green test that asserts nothing (the house's
        // fifth bug class). Pin the counts so deleting the plates cannot quietly satisfy every test below.
        Assert.Equal(3, KaamosLore.AllPlates.Count());
        Assert.Equal(2, NebulaLore.AllPlates.Count());
    }

    [Fact]
    public void EveryPlateIsKeyedToARealFragment()
    {
        foreach ((string arc, string id, RevealPlate plate) in AllPlates())
        {
            bool known = arc == "KAAMOS" ? KaamosLore.ById(id) is not null : NebulaLore.ById(id) is not null;
            Assert.True(
                known,
                $"{arc} plate \"{plate.Title}\" is keyed to \"{id}\", which is not a fragment in that arc's " +
                "pool. A plate nobody can reach is a painting nobody sees.");
        }
    }

    [Fact]
    public void EveryPlateNamesAPaintingThatExists()
    {
        Assert.True(Directory.Exists(ArtDir), $"No copied art beside the test assembly at {ArtDir} — check the csproj artsource ItemGroup.");

        foreach ((string arc, string id, RevealPlate plate) in AllPlates())
        {
            Assert.StartsWith("art/", plate.ArtFile, StringComparison.Ordinal);
            AssertPainted($"{arc} plate \"{id}\"", plate.ArtFile);
        }
    }

    [Fact]
    public void TheConvergenceIsPainted()
    {
        // The biggest reveal in the game. It was a text div while a routine collector shakedown had a
        // painted portrait; if the plate is ever unwired or renamed, this is the thing that notices.
        AssertPainted("The convergence", ArcConvergence.ArtFile);
    }

    [Fact]
    public void EveryBoliviaBeatIsPainted()
    {
        // The stand is keyed by beat ID, not index, so this sweep walks the SCRIPT — the thing the client
        // actually renders — rather than the three ids I happen to remember. Insert a beat and forget its
        // painting and this is what says so.
        Assert.NotEmpty(BoliviaEncounter.Script);

        foreach (EncounterBeat beat in BoliviaEncounter.Script)
        {
            string art = BoliviaEncounter.ArtFile(beat.Id);
            Assert.False(
                string.IsNullOrEmpty(art),
                $"Bolivia beat \"{beat.Id}\" has no plate. The freeze-frame that ENDS this script has been " +
                "painted since PR-BUSTED; a beat that earns it should not be text with buttons under it.");
            AssertPainted($"Bolivia beat \"{beat.Id}\"", art);
        }
    }

    [Fact]
    public void AnUnknownBoliviaBeatAsksForNothing()
    {
        // The client calls ArtFile with whatever id the script hands it and renders nothing on empty. A
        // stray id must be a quiet no-picture, never a broken frame.
        Assert.Equal(string.Empty, BoliviaEncounter.ArtFile("no-such-beat"));
    }

    [Fact]
    public void EveryDeathCardIsPainted()
    {
        // The sweep that #621's bug would have failed: every cause × every place, because the PLACE decides
        // the picture and the pre-#636 mapping only broke for one pairing nobody could reach until they
        // could. Enumerating both enums means a new cause or a new place cannot ship art-less.
        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            AssertPainted($"Death card {cause} (no place)", "art/" + DeathNarration.ArtFile(cause));

            foreach (DeathPlace place in Enum.GetValues<DeathPlace>())
            {
                AssertPainted($"Death card {cause} on {place}", "art/" + DeathNarration.ArtFile(cause, place));
            }
        }
    }

    /// <summary>
    /// #654 · A WRECK HAS THREE EVIDENCE STATIONS AND ALL THREE SHOW A PICTURE — on all ten hulls.
    ///
    /// <para>The bridge log and the cargo manifest are the corroboration <c>MisreadsAs</c> is built on:
    /// reading both is the single thing that takes the decoy off the choice card. They were a pulse line
    /// that faded in a second and a half while the damage two rooms away got a full plate.</para>
    ///
    /// <para><b>Proven RED:</b> point <see cref="Derelict.LogArtFile"/> at <c>art/wreck-log-nope.jpg</c> and
    /// this fails naming the basename; the whole point of the sweep is that the browser would not have.</para>
    /// </summary>
    [Fact]
    public void EveryWreckEvidenceStationIsPainted()
    {
        AssertPainted("The bridge log station", Derelict.LogArtFile);
        AssertPainted("The cargo manifest station", Derelict.ManifestArtFile);
        Assert.NotEqual(Derelict.LogArtFile, Derelict.ManifestArtFile);

        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            AssertPainted($"Wreck {cause}'s own cause station", Derelict.ArtFile(cause));
        }
    }

    /// <summary>
    /// AND THE PICTURE IS THE SAME ONE EVERY TIME, WHILE THE WORDS ARE NEVER THE SAME TWICE.
    ///
    /// <para>That trade is the whole design of the two new slots, and it is the owner's anti-tell law
    /// applied to art: a plate that showed up on the insurance job's manifest and nowhere else would tell
    /// the captain the ship was a staged loss before they had read a word of it, and undo the misdirection
    /// the cause taxonomy exists for. So the variance is not allowed to live in the pixels — it has to live
    /// in the caption, and this asserts BOTH halves at once so neither can drift alone.</para>
    ///
    /// <para><b>Proven RED:</b> delete <see cref="Derelict.WreckCause.HullBreach"/>'s arm from
    /// <c>LogCaption</c> — she falls through to <i>"ordinary ship's business"</i>, the distinct count drops
    /// to nine, and this names her. (That is exactly how the vented hull's log broke in #533, one switch
    /// over.)</para>
    /// </summary>
    [Fact]
    public void TheHullsShareOnePaintingAndShareNoSentence()
    {
        var logs = new HashSet<string>(StringComparer.Ordinal);
        var pictures = new HashSet<string>(StringComparer.Ordinal);
        int hulls = 0;

        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            Derelict.Wreck w = Derelict.SeededWithCause(cause)
                ?? throw new InvalidOperationException($"no seeded hull for {cause}");
            hulls++;

            // The station's picture, chosen the way Map.Wreck.ExamineWreckEvidence chooses it.
            pictures.Add(Derelict.LogArtFile);
            pictures.Add(Derelict.ManifestArtFile);

            string log = Derelict.LogCaption(w);
            Assert.True(log.Length > 60, $"{cause}'s log caption is too short to be evidence: \"{log}\"");
            Assert.DoesNotContain("ordinary ship's business", log, StringComparison.Ordinal);
            Assert.True(
                logs.Add(log),
                $"{cause}'s bridge log reads word for word like another hull's. The painting is deliberately "
                + "shared; the SENTENCE is the only thing telling these ten ships apart.");

            string manifest = Derelict.ManifestCaption(w);
            Assert.True(manifest.Length > 40, $"{cause}'s manifest caption is too short to be evidence.");

            // The glyph belongs to the station label, not to the words — the card prints the title above the
            // caption, and the pulse line prints the glyph in front of it. One text, two readers, one icon.
            Assert.DoesNotContain("🖥", log, StringComparison.Ordinal);
            Assert.DoesNotContain("📦", manifest, StringComparison.Ordinal);
            Assert.StartsWith("🖥 " + log, Derelict.LogFinding(w), StringComparison.Ordinal);
            Assert.StartsWith("📦 " + manifest, Derelict.ManifestFinding(w), StringComparison.Ordinal);
        }

        Assert.Equal(10, hulls);
        Assert.Equal(10, logs.Count);
        Assert.Equal(2, pictures.Count);   // one log plate and one manifest plate, across all ten hulls
    }

    /// <summary>
    /// #528 ROUND TWO — the plates that belong to a BEAT rather than to an arc's fragment pool.
    ///
    /// <para>KAAMOS and NEBULA hand their plates out of a keyed pool, so <see cref="AllPlates"/> can walk
    /// them. These ones hang off the predicate that fires them (a d20 branch, a detector square, a console
    /// press), which means nothing enumerates them and nothing would ever notice one going dark. This list
    /// IS the enumeration, and the count below is what stops deleting an entry from quietly satisfying it.
    /// </para>
    ///
    /// <para><b>Proven RED:</b> point <c>SecretLab.TheyStandPlate</c> at <c>art/lab-they-stand-nope.jpg</c>
    /// and this fails naming the basename; drop an entry from the list and the count fails.</para>
    /// </summary>
    private static IEnumerable<(string Where, RevealPlate Plate)> BeatPlates() =>
    [
        ("SecretLab.DoorPlate", SecretLab.DoorPlate),
        ("SecretLab.TheyStandPlate", SecretLab.TheyStandPlate),
        ("SurfaceOutpost.EffectsPlate", SurfaceOutpost.EffectsPlate),
    ];

    [Fact]
    public void EveryBeatPlateIsPaintedAndSaysItOnce()
    {
        var seenArt = new HashSet<string>(StringComparer.Ordinal);
        var seenTitles = new HashSet<string>(StringComparer.Ordinal);
        int n = 0;

        foreach ((string where, RevealPlate plate) in BeatPlates())
        {
            n++;
            Assert.StartsWith("art/", plate.ArtFile, StringComparison.Ordinal);
            AssertPainted($"Beat plate {where}", plate.ArtFile);
            Assert.False(string.IsNullOrWhiteSpace(plate.Title), $"{where} has no title.");
            Assert.True(plate.Caption.Length > 80, $"{where}'s caption is too short to be evidence.");
            Assert.True(seenArt.Add(plate.ArtFile), $"{where} shares a painting with another plate.");
            Assert.True(seenTitles.Add(plate.Title), $"{where} shares a title with another plate.");
        }

        Assert.Equal(3, n);
    }

    [Fact]
    public void EveryPlateSaysSomethingAndSaysItOnce()
    {
        var seenArt = new HashSet<string>(StringComparer.Ordinal);
        var seenTitles = new HashSet<string>(StringComparer.Ordinal);

        foreach ((string arc, string id, RevealPlate plate) in AllPlates())
        {
            Assert.False(string.IsNullOrWhiteSpace(plate.Title), $"{arc} plate \"{id}\" has no title.");
            Assert.True(plate.Caption.Length > 80, $"{arc} plate \"{id}\" has a caption too short to be evidence.");
            Assert.True(seenArt.Add(plate.ArtFile), $"Two plates share the painting {plate.ArtFile}.");
            Assert.True(seenTitles.Add(plate.Title), $"Two plates share the title \"{plate.Title}\".");
        }
    }

    [Fact]
    public void TheBeatsThatAreTheRightSizeAsProseGetNoPlate()
    {
        // Over-carding cheapens the big ones (#528's own discipline). The KAAMOS three are a line on a
        // plaque, a log found in a drawer, and a coordinate bought over a counter — each already arrives
        // with its own scene around it, and none of them is a turning.
        Assert.Null(KaamosLore.PlateFor("listed-berth"));
        Assert.Null(KaamosLore.PlateFor("vantar-log"));
        Assert.Null(KaamosLore.PlateFor("bought-coordinate"));

        // The NEBULA four arrive INSIDE a host card that already carries a picture — the BUSTED modal and
        // the poster. A second frame there would stack a card on a card.
        Assert.Null(NebulaLore.PlateFor("rebirth-glitch"));
        Assert.Null(NebulaLore.PlateFor("fine-print"));
        Assert.Null(NebulaLore.PlateFor("collector-writ"));
        Assert.Null(NebulaLore.PlateFor("clinic-ledger"));
    }

    [Fact]
    public void AnUnknownIdAsksForNothing()
    {
        // The client asks PlateFor at the single assemble seam, with whatever id the caller passed. A typo
        // must be a quiet no-card, never a throw in the middle of a find.
        Assert.Null(KaamosLore.PlateFor("no-such-shard"));
        Assert.Null(NebulaLore.PlateFor("no-such-shard"));
    }

    [Fact]
    public void TheCapstonePlatesAreTheCapstones()
    {
        // The loudest plate in each arc belongs to the beat the whole thread is for — the berth answering,
        // and the policy's true terms resolving. If either key fragment is renamed, this catches the plate
        // left behind on the old id.
        Assert.NotNull(KaamosLore.PlateFor(KaamosLore.KeyFragment.Id));
        Assert.NotNull(NebulaLore.PlateFor(NebulaLore.KeyFragment.Id));
    }
}
