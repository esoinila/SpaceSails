using System;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #564 · <i>"let's make that story testable then"</i> — the owner, on being told that the surface death path
/// ROLLS its cause and would therefore have narrated a suffocation as an Old One's hand.
///
/// <para>This is the third time this project has been bitten by the same thing: the sim doing one thing
/// while a SENTENCE reports another. #545's death card blamed Reevers for three men with rifles, because
/// <see cref="DeathNarration.SurfaceEnd"/> only knows the ground's two answers and was asked anyway. The fix
/// there and here is identical — the caller passes what it KNOWS instead of rolling — and these hold it.</para>
/// </summary>
public class SuffocationStoryTests
{
    [Fact]
    public void TheRollCanNeverProduceSuffocation_WhichIsWhyItMustBePassed()
    {
        // The heart of it. SurfaceEnd is asked "what killed this captain?" and can only ever answer with the
        // ground's two stories. If a future caller forgets to pass the known cause, it will get a wrong one —
        // confidently. Pinned across a wide seed sweep and the whole nerve range so nobody can mistake this
        // for a gap that might close on its own.
        for (ulong seed = 0; seed < 400; seed++)
        {
            foreach (double nerve in new[] { 0.0, 4.0, 8.0, 25.0, 60.0, 100.0 })
            {
                DeathCause rolled = DeathNarration.SurfaceEnd(nerve, seed);
                Assert.NotEqual(DeathCause.Suffocated, rolled);
                Assert.True(rolled is DeathCause.Reevers or DeathCause.Joined);
            }
        }
    }

    [Fact]
    public void TheSuffocationHeadlineNamesTheBody_AndBlamesNobody()
    {
        // It must not mention creatures, hands, nerve or crowds. A captain who walked past a warning they
        // were given deserves an honest account of it, and the reveal-protecting canon rule (#563) applies
        // here too: nothing about the Old Ones is ever stated in a card.
        string line = DeathNarration.SuffocationHeadline("Miranda");

        Assert.Contains("Miranda", line, StringComparison.Ordinal);
        foreach (string wrong in new[] { "Old One", "Reever", "hand", "nerve", "crowd", "breaks" })
        {
            Assert.DoesNotContain(wrong, line, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheHeadlineSaysTheCaptainWasWarned()
    {
        // The whole design in one sentence: this was not a number that ran out, it was a line that was
        // crossed, and the game said so at the time. If the death card does not carry that, the player is
        // entitled to feel cheated — and would be right.
        Assert.Contains("said so", DeathNarration.SuffocationHeadline("Luna"), StringComparison.Ordinal);
    }

    [Fact]
    public void SuffocationIsItsOwnCause_NotFoldedIntoTheVoid()
    {
        // There is an existing catch-all (Void: "adrift / EVA / an orbit that slipped, no body to name").
        // Reusing it would have been cheaper and would have lost the thing that matters — suffocation
        // happens on a NAMED body, after a warning, and the card should say which moon.
        Assert.NotEqual(DeathCause.Void, DeathCause.Suffocated);
        Assert.Contains("Enceladus", DeathNarration.SuffocationHeadline("Enceladus"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheWarningAndTheDeathAreTheSameStory()
    {
        // The line you are given and the epitaph you get must agree about what happened, or the mechanic
        // reads as two unrelated systems. Both are about a tank, and both about knowing.
        Assert.Contains("tank", SuitAir.CrossingWarning, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tank", SuitAir.SuffocationLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tank", DeathNarration.SuffocationHeadline("Titan"), StringComparison.OrdinalIgnoreCase);
    }
}
