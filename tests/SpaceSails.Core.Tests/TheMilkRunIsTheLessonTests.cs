using System;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #160 · THE MILK RUN'S EIGHT LINES, AND NOTHING ELSE.
///
/// <para>Owner, 2026-07-16, mid-playtest: <i>"I think we should have a tutorial for mission also? Some easy
/// milk run with autopilot from moon to moon, maybe?"</i> The canon pass that answered it (Fable, on the
/// issue, 2026-09-05) wrote eight lines — one per step, in the labs' edutainment tone — and closed with a
/// law: <i>implement verbatim; nothing else is authored</i>.</para>
///
/// <para><b>Why a test and not a code review.</b> Prose drifts. Somebody tightening a sentence, or a lane
/// that needs "just one more line" for a picker card or an empty-state, is exactly how a canon pass turns
/// into a draft nobody agreed to. This class pins the eight against a copy written down here, so a change to
/// either goes red with both texts in the message and the change has to be made in two places on purpose.
/// It also pins the two DERIVED strings — the lesson card's name and blurb — as slices of line 1, which is
/// the whole reason they are derived: a card needs a name, the canon pass wrote eight lines, and a ninth
/// string beside them would be a ninth line.</para>
///
/// <para><b>Proven red</b> by changing one character of any line, by adding a ninth, by authoring a Title of
/// its own, and by dropping the <c>JsonIgnore</c> off the vault's new field.</para>
/// </summary>
public sealed class TheMilkRunIsTheLessonTests
{
    /// <summary>The eight, written down a second time — deliberately, and by hand. A guard that reads the
    /// shipped array and compares it with itself is the fifth bug class this repo keeps a note about: green,
    /// and unable to tell pass from fail.</summary>
    private static readonly string[] TheCanonPass =
    [
        "A milk run. Drums from Enceladus to Titan; nobody shoots at drums. Take it from the board.",
        "Plan the whole trip, dock to dock. The plan is a list of steps; the autopilot flies the list.",
        "Top her off before you leave. The autopilot quotes fuel honestly, and it cannot quote what you did not load.",
        "Arm it. The rehearsal flies the plan on paper first and tells you what it will cost. Believe the number.",
        "The departure burn fires itself at its epoch. Watch the banner: NOW is what she is doing, NEXT is what she will.",
        "Warp is your clock, not hers. The plan does not care how fast you watch it.",
        "Arrival is autopilot then, not now — it was armed at plan time. Dock, and the contract pays at the counter.",
        "That is the loop. Everything else in this game is what happens when a milk run goes wrong.",
    ];

    // ── (a) THE WORDS ────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE EIGHT ARE THE EIGHT, IN ORDER, TO THE CHARACTER. Em dash, semicolons and all — the
    /// punctuation is the voice.</summary>
    [Fact]
    public void THE_LINES_AreTheCanonPassVerbatimAndInStepOrder()
    {
        Assert.Equal(TheCanonPass.Length, MilkRunLesson.Lines.Length);
        for (int i = 0; i < TheCanonPass.Length; i++)
        {
            Assert.Equal(TheCanonPass[i], MilkRunLesson.Lines[i]);
        }
    }

    /// <summary>AND THERE ARE EIGHT OF THEM, WHICH IS ALSO HOW MANY STEPS THE LESSON HAS. One line per step
    /// is not a coincidence to be maintained by hand: the count is read off the array, so a ninth line would
    /// be a ninth step with a gate nobody wrote, and this says so out loud.</summary>
    [Fact]
    public void THE_LESSON_HasExactlyOneStepPerLine()
    {
        Assert.Equal(8, MilkRunLesson.Lines.Length);
        Assert.Equal(MilkRunLesson.Lines.Length, MilkRunLesson.StepCount);
    }

    /// <summary>THE CARD'S NAME AND BLURB ARE SLICES OF LINE 1, NOT PROSE OF THEIR OWN. Put back together
    /// they are the line, which is the only proof that nothing was written between them.</summary>
    [Fact]
    public void THE_PICKER_CARD_IsTheFirstLineCutInTwoAndNotANinthString()
    {
        Assert.Equal("A milk run", MilkRunLesson.Title);
        Assert.Equal("Drums from Enceladus to Titan; nobody shoots at drums. Take it from the board.",
            MilkRunLesson.Blurb);

        // …and the two halves ARE the line: title, the full stop the title trimmed, a space, then the rest.
        Assert.Equal(MilkRunLesson.Lines[0], $"{MilkRunLesson.Title}. {MilkRunLesson.Blurb}");
    }

    /// <summary>THE RESERVED WORD IS ABSENT. docs/worldbuilding-notes.md §8: there is ONE monolith, and the
    /// word is reserved to it. A teaching lesson about hauling drums is the last place it belongs, and this
    /// is the cheapest possible place to keep it out.</summary>
    [Fact]
    public void THE_RESERVED_WORD_IsNowhereInTheLesson()
    {
        foreach (string line in MilkRunLesson.Lines.Append(MilkRunLesson.Title).Append(MilkRunLesson.Blurb))
        {
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>THE BOARD'S GIVER IS NOT A NEW NAME. Line 1 says "take it from the board"; the game already
    /// had a board-posted haul with a giver of exactly that, so the lesson borrows it rather than inventing
    /// a ninth string with a face on it.</summary>
    [Fact]
    public void THE_GIVER_IsTheBoardTheGameAlreadyPostsWorkOn()
    {
        Assert.Equal("THE BOARD", MilkRunLesson.BoardGiver);
    }

    // ── (b) THE PLACE THE LESSON KEEPS ───────────────────────────────────────────────────────────────

    /// <summary>THE STEP SURVIVES A SAVE AND A LOAD. The canon law is that no line is re-fired on a reload,
    /// and this is the field that carries it — <c>_tutorialStep</c> deliberately cannot.</summary>
    [Fact]
    public void THE_LESSONS_PLACE_RoundTripsThroughTheVault()
    {
        var before = new Vault
        {
            Version = 1,
            Progress = new ProgressSection { TutorialPlayed = true, MilkRunLessonStep = 5 },
        };

        Vault after = VaultSerializer.Load(VaultSerializer.Save(before));

        Assert.Equal(5, after.Progress?.MilkRunLessonStep);
    }

    /// <summary>…AND IS NOT WRITTEN AT ALL WHEN NOBODY HAS TAKEN THE LESSON. The checksum is taken over the
    /// payload: a <c>"milkRunLessonStep": 0</c> on every save would move the digest of every vault ever
    /// written and hang the 📛 tampered marker on an honest voyage (#1057/#1066/#1072's law). Null is the
    /// truth about every file written before this lesson existed.</summary>
    [Fact]
    public void A_CAPTAIN_WHO_NEVER_TOOK_IT_LeavesNoTraceInTheFile()
    {
        var untaught = new Vault { Version = 1, Progress = new ProgressSection { TutorialPlayed = true } };

        string json = VaultSerializer.Save(untaught);

        Assert.DoesNotContain("milkRunLessonStep", json, StringComparison.OrdinalIgnoreCase);
        // …and the guard is not passing because the section itself went missing.
        Assert.Contains("tutorialPlayed", json, StringComparison.Ordinal);
    }

    /// <summary>AND A PRE-#160 FILE LOADS AS ONE. A vault with no such key must come back null — "never took
    /// it" — rather than as a zero that some later reader could mistake for a step.</summary>
    [Fact]
    public void A_LEGACY_FILE_ComesBackAsNeverTookItAndNotAsStepZero()
    {
        string legacy = VaultSerializer.Save(
            new Vault { Version = 1, Progress = new ProgressSection { TutorialPlayed = true } });

        Assert.Null(VaultSerializer.Load(legacy).Progress?.MilkRunLessonStep);
    }
}
