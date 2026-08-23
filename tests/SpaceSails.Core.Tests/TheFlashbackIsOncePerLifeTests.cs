using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #973 · THE PAGE COMES BACK ONCE PER LIFE, AND IT COMES BACK DIFFERENT. Two laws, and both of them are
/// carried by the SUBJECT rather than by a flag anybody has to clear:
/// <list type="number">
/// <item>the seen-set's <see cref="StoryBeats.Cadence.OncePerSubject"/> plus a life-stamped subject gives
/// "once per subject per life" with no rebirth hook at all — and a forgotten <c>.Clear()</c> on the rebirth
/// path is a bug that only surfaces two deaths later, in somebody else's session;</item>
/// <item>the plate a reborn captain reads has one line the first one never saw, and it is the line the whole
/// arc turns on.</item>
/// </list>
/// </summary>
public sealed class TheFlashbackIsOncePerLifeTests
{
    /// <summary>A life is a different subject, which is the whole of how the cadence buys a rebirth.</summary>
    [Fact]
    public void EachLifeIsItsOwnSubject()
    {
        string first = FlashbackMemories.SubjectForLife(FlashbackMemories.Signing, 1);
        string second = FlashbackMemories.SubjectForLife(FlashbackMemories.Signing, 2);

        Assert.NotEqual(first, second);
        Assert.Equal(FlashbackMemories.Signing, FlashbackMemories.MemoryOf(first));
        Assert.Equal(FlashbackMemories.Signing, FlashbackMemories.MemoryOf(second));
        Assert.Equal(1, FlashbackMemories.LifeOf(first));
        Assert.Equal(2, FlashbackMemories.LifeOf(second));
    }

    /// <summary>…and the seen-set really does treat them as two, because the beat is once-PER-SUBJECT. A
    /// once-ever cadence here would have shown the signing to the first captain and silently swallowed it
    /// for every one after — which is #541's exact failure wearing arc 2's clothes.</summary>
    [Fact]
    public void TheCadenceIsTheOneThatRemembersWhatItWasAbout()
    {
        Assert.Equal(StoryBeats.Cadence.OncePerSubject, StoryBeats.CadenceOf(StoryBeats.Beat.Flashback));
    }

    /// <summary>A subject with no life on it is a first life — an unversioned caller means the first
    /// telling, and a total function is what keeps the beat from going captionless.</summary>
    [Fact]
    public void ASubjectWithNoLifeOnItIsAFirstLife()
    {
        Assert.Equal(1, FlashbackMemories.LifeOf(FlashbackMemories.Signing));
        Assert.Equal(1, FlashbackMemories.LifeOf(""));
        Assert.Equal(FlashbackMemories.Signing, FlashbackMemories.MemoryOf(""));
    }

    /// <summary>
    /// THE SECOND LIFE READS ONE LINE MORE. Owner's ruling 3 and the arc's deepest tell in one sentence:
    /// the memory is intact and the hand in it is not yours.
    ///
    /// <para><b>Proven RED</b> by returning <c>SigningCaption</c> unconditionally from
    /// <c>PlateFor</c> — the reborn assertion then finds the first captain's plate.</para>
    /// </summary>
    [Fact]
    public void TheRebornCaptainReadsTheLineTheFirstOneNeverSaw()
    {
        RevealPlate first = FlashbackMemories.PlateFor(
            FlashbackMemories.SubjectForLife(FlashbackMemories.Signing, 1))!;
        RevealPlate reborn = FlashbackMemories.PlateFor(
            FlashbackMemories.SubjectForLife(FlashbackMemories.Signing, 2))!;

        Assert.DoesNotContain(FlashbackMemories.SigningRebornLine, first.Caption, StringComparison.Ordinal);
        Assert.Contains(FlashbackMemories.SigningRebornLine, reborn.Caption, StringComparison.Ordinal);
        Assert.StartsWith(first.Caption, reborn.Caption, StringComparison.Ordinal);
        Assert.Equal(first.Title, reborn.Title);
        Assert.Equal(first.ArtFile, reborn.ArtFile);
    }

    /// <summary>Every later life reads the reborn plate — the line does not arrive once and then leave.</summary>
    [Fact]
    public void AndEveryLifeAfterThatReadsItToo()
    {
        for (int life = 2; life < 8; life++)
        {
            RevealPlate plate = FlashbackMemories.PlateFor(
                FlashbackMemories.SubjectForLife(FlashbackMemories.Signing, life))!;
            Assert.Contains(FlashbackMemories.SigningRebornLine, plate.Caption, StringComparison.Ordinal);
        }
    }

    /// <summary>A memory nobody has authored resolves to nothing at all — the honest degrade the two arcs'
    /// pools already make, so a card can never be raised about a page that does not exist.</summary>
    [Fact]
    public void AMemoryNobodyWroteResolvesToNothing()
    {
        Assert.Null(FlashbackMemories.PlateFor("the-summer-party"));
        Assert.Null(FlashbackMemories.PlateFor("the-summer-party#3"));
    }

    /// <summary>
    /// HOSTED, AND NEVER ANYTHING ELSE. A flashback arrives inside somebody else's scene — the rep's pitch,
    /// a poster, a photograph in a friend's hand — so it never raises a surface of its own and never waits.
    /// </summary>
    [Fact]
    public void TheFlashbackIsAlwaysHostedByWhoeverIsTalkingToYou()
    {
        Assert.Equal(StoryBeats.Presentation.Hosted, StoryBeats.PresentationOf(StoryBeats.Beat.Flashback));
        Assert.False(string.IsNullOrWhiteSpace(StoryBeats.HostCard(StoryBeats.Beat.Flashback)));
        Assert.False(StoryBeats.DeferrableWhileInDanger(StoryBeats.Beat.Flashback));
    }

    /// <summary>The beat's words come from the pool and are never retyped in the seam — the second-source
    /// drift this project keeps paying for.</summary>
    [Fact]
    public void TheSeamAsksThePoolForTheWords()
    {
        string subject = FlashbackMemories.SubjectForLife(FlashbackMemories.Signing, 2);
        RevealPlate plate = FlashbackMemories.PlateFor(subject)!;

        Assert.Equal(plate.Title, StoryBeats.Title(StoryBeats.Beat.Flashback, subject));
        Assert.Equal(plate.Caption, StoryBeats.Caption(StoryBeats.Beat.Flashback, subject));
        Assert.Equal(FlashbackMemories.PlateArt, StoryBeats.ArtFile(StoryBeats.Beat.Flashback, subject));
    }
}
