namespace SpaceSails.Core.Tests;

/// <summary>
/// #440 · The first-ground lesson. Owner, 2026-07-26: <i>"Definitely we need a landing site tutorial also
/// for new captains."</i> The card exists because the surface taught itself only in dimmed corner text — and
/// the proof was the owner reaching for the wrong key for the feature he had specified. These pin the thing
/// that must never rot: that the card actually NAMES the keys, in the plainest terms, and that the copy
/// cannot quietly drift back into vagueness.
/// </summary>
public class GroundLessonTests
{
    [Fact]
    public void EverySurfaceKey_IsNamed()
    {
        // E, T and G are the whole reason the card exists. If a lever is ever dropped from this list, the
        // key it teaches goes back to being a secret.
        string[] keys = [.. GroundLesson.Levers.Select(l => l.Key)];
        Assert.Contains("E", keys);
        Assert.Contains("T", keys);
        Assert.Contains("G", keys);
    }

    [Fact]
    public void EveryLever_IsFullyDressed_AndSaysWhatTheKeyDoes()
    {
        foreach (GroundLesson.Lever lever in GroundLesson.Levers)
        {
            Assert.False(string.IsNullOrWhiteSpace(lever.Key));
            Assert.False(string.IsNullOrWhiteSpace(lever.Title));
            // A key with a title but no explanation is exactly the dimmed-corner-text failure this card was
            // built to end — so the prose has to be real prose, not a stub.
            Assert.True(lever.What.Length > 40, $"the '{lever.Key}' lever barely explains itself: \"{lever.What}\"");
        }
    }

    [Fact]
    public void TheDigLever_TellsThemTheChestGoesWhereTheyStand()
    {
        // The single most-missed rule on the surface: there is no designated dig site. Anywhere on the open
        // regolith is the spot. If this sentence goes, new captains go back to hunting for a magic square.
        GroundLesson.Lever dig = GroundLesson.Levers.Single(l => l.Key == "E");
        Assert.Contains("WHERE YOU STAND", dig.What, System.StringComparison.Ordinal);
        Assert.Contains("regolith", dig.What, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheLaws_CoverWhatActuallyKillsAFirstExcursion()
    {
        string all = string.Join(" ", GroundLesson.Laws);
        Assert.Contains("half the tank", all, System.StringComparison.OrdinalIgnoreCase);   // the walk back
        Assert.Contains("crew only", all, System.StringComparison.OrdinalIgnoreCase);       // the shuttle door (#295)
        Assert.Contains("theirs", all, System.StringComparison.OrdinalIgnoreCase);          // what you lose if caught
        Assert.Contains("rockcrete", all, System.StringComparison.OrdinalIgnoreCase);       // no burying on the pad
    }

    [Fact]
    public void TheCardIsShort_ItIsALessonNotAManual()
    {
        // The card goes up between a captain and the ground they are standing on. Anything longer than a
        // glance will simply be dismissed unread, which is the same as not existing.
        Assert.InRange(GroundLesson.Levers.Count, 3, 4);
        Assert.InRange(GroundLesson.Laws.Count, 3, 5);
    }

    [Fact]
    public void EveryPieceOfChrome_IsWritten()
    {
        Assert.False(string.IsNullOrWhiteSpace(GroundLesson.Stamp));
        Assert.False(string.IsNullOrWhiteSpace(GroundLesson.Head));
        Assert.False(string.IsNullOrWhiteSpace(GroundLesson.Dismiss));
        // The foot points at where the keys keep living once the card is gone — the affordances-never-hide
        // law (#212): dismissing the lesson must not be the last time the player is told. It names the two
        // places in PLAYER terms (under the tracker, along the bottom), never our word "keybar".
        Assert.Contains("tracker", GroundLesson.Foot, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bottom", GroundLesson.Foot, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("keybar", GroundLesson.Foot, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheSeenBit_RoundTripsInTheVault_SoACaptainIsGreetedOnce()
    {
        // #292's ruling: only the truly new. The bit rides in ProgressSection beside TutorialPlayed, so a
        // reload cannot re-teach a captain who has already walked a moon.
        var progress = new ProgressSection { GroundLessonSeen = true };
        Assert.True(progress.GroundLessonSeen);

        // …and a save written before #440 simply lacks the field, which must read as "not yet taught"
        // (they get it once on their next trip down) rather than throwing or defaulting to already-seen.
        Assert.False(new ProgressSection().GroundLessonSeen);
    }

    [Fact]
    public void TheEKeyTeachesEVERYTHINGItDoesNow_AndNotJustDigging()
    {
        // #440 REOPENED. Owner, after a day of building the ground out: "the E key does a lot more now."
        //
        // It did mean DIG, once, when a surface was regolith and somewhere to leave a chest. E is now the
        // ONE key that touches anything at all, and a card that still titled it "Dig" was teaching a new
        // captain to walk past every door, console, pump, lift and searchable room on the moon.
        //
        // This guard is deliberately about the TITLE as well as the body, because the title is the only part
        // a captain reads at a glance, and the title was the part that was lying.
        GroundLesson.Lever e = Assert.Single(GroundLesson.Levers, l => l.Key == "E");

        Assert.DoesNotContain("dig", e.Title, System.StringComparison.OrdinalIgnoreCase);
        foreach (string thing in new[] { "door", "console", "lift", "room" })
        {
            Assert.Contains(thing, e.What, System.StringComparison.OrdinalIgnoreCase);
        }

        // The shovel is not GONE — it is one of the things E does, and burying is still the reason most
        // captains first press it. It just no longer gets to be the whole lesson.
        Assert.Contains("bur", e.What, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheCardKnowsTheGroundIsMoreThanAPlaceToBuryThings()
    {
        // Owner: "also going to ground is more than burying chest now." The head line led with caching,
        // which was written when caching was all there was. The one thing true of every square metre of a
        // surface — and the clock every other choice out here runs against — is the AIR.
        Assert.Contains("air", GroundLesson.Head, System.StringComparison.OrdinalIgnoreCase);

        // And the shelter is on the card at all, which it was not until #573: the single building that can
        // save a captain's life must not be something they discover by nearly dying without it.
        Assert.Contains(GroundLesson.Laws, l => l.Contains("shelter", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheSatchelLeverIsWrittenForEmptyPockets()
    {
        // #603 · Owner: "should we mention the inventory here also?" This card is where keys are introduced
        // and I now does something, so yes.
        //
        // The wording constraint is the interesting half. On a FIRST landing the pockets are empty, so a
        // lever that describes what is in them describes nothing. It has to promise (a) there is somewhere
        // for the first thing you find to go, (b) it survives the trip home, and (c) it is what you reach
        // for at a door that will not open — which is the only moment a captain goes looking for it.
        GroundLesson.Lever i = Assert.Single(GroundLesson.Levers, l => l.Key == "I");

        Assert.Contains("stays yours", i.What, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("try", i.What, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("door", i.What, System.StringComparison.OrdinalIgnoreCase);
    }
}
