namespace SpaceSails.Core.Tests;

/// <summary>
/// #963 follow-up · A CRASH THE PLAYER CAN QUOTE.
///
/// <para>Two of the owner's playtest screenshots end at Blazor's "An unhandled error has occurred.
/// Reload." — a red bar with no text in it, because the console that knew the answer died with the tab.
/// <see cref="CrashNote"/> is the sentence he can paste into an issue instead, and the reason it is a
/// pure Core type rather than a bit of client plumbing is right here: the note has to survive a round
/// trip through localStorage, and a format that loses the message on the way back is a black box that
/// records silence.</para>
/// </summary>
public class CrashNoteTests
{
    private static Exception Thrown(Action body)
    {
        try
        {
            body();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException("this bench needs a real thrown exception, stack and all");
    }

    [Fact]
    public void From_ReadsTheTypeTheMessageAndTheTopFrames()
    {
        Exception ex = Thrown(static () => throw new InvalidOperationException("the lift only went down"));

        CrashNote note = CrashNote.From("frame tick", ex, whenUtcTicks: 42);

        Assert.Equal("frame tick", note.Source);
        Assert.Equal("InvalidOperationException", note.TypeName);
        Assert.Equal("the lift only went down", note.Message);
        Assert.NotEmpty(note.Frames);
        Assert.True(note.Frames.Count <= CrashNote.MaxFrames,
            $"a note nobody can paste is a note nobody files — {note.Frames.Count} frames kept");
        Assert.Equal(42, note.WhenUtcTicks);
    }

    /// <summary>An AggregateException's own message is boilerplate. The note has to name the exception
    /// that actually went off, or every task-side crash reads "One or more errors occurred".</summary>
    [Fact]
    public void From_NamesTheRealExceptionInsideAnAggregate()
    {
        Exception inner = Thrown(static () => throw new FormatException("a heading that is not a number"));

        CrashNote note = CrashNote.From("task", new AggregateException(inner), whenUtcTicks: 1);

        Assert.Equal("FormatException", note.TypeName);
        Assert.Equal("a heading that is not a number", note.Message);
    }

    /// <summary>The one thing the crash reporter may not do is crash.</summary>
    [Fact]
    public void From_SurvivesHavingNoExceptionAtAll()
    {
        CrashNote note = CrashNote.From("unhandled", null, whenUtcTicks: 7);

        Assert.Equal("(none)", note.TypeName);
        Assert.NotEqual("", note.Message);
    }

    /// <summary>THE ROUND TRIP IS THE WHOLE FEATURE: the note is written to localStorage by the voyage
    /// that died and read by the one that comes after it. A format that cannot carry the message home is
    /// a black box recording silence.</summary>
    [Fact]
    public void ToStorage_RoundTripsThroughOneString()
    {
        Exception ex = Thrown(static () => throw new InvalidOperationException("the sail would not furl"));
        CrashNote written = CrashNote.From("frame tick", ex, whenUtcTicks: 638_000_000_000_000_000);

        Assert.True(CrashNote.TryParse(written.ToStorage(), out CrashNote read));

        // Compared part by part, not with Assert.Equal(written, read): a record's generated equality
        // compares the Frames list by REFERENCE, so two notes carrying identical frames are never equal
        // and the assertion would be green on a parser that dropped every frame.
        Assert.Equal(written.Source, read.Source);
        Assert.Equal(written.TypeName, read.TypeName);
        Assert.Equal(written.Message, read.Message);
        Assert.Equal(written.WhenUtcTicks, read.WhenUtcTicks);
        Assert.Equal(written.Frames, read.Frames);              // element-wise, which is the point
        Assert.Equal(written.Describe(), read.Describe());

        // …and writing it again produces the same bytes, so the note survives any number of voyages.
        Assert.Equal(written.ToStorage(), read.ToStorage());
    }

    /// <summary>A message with a newline in it used to be able to cut the note in half on the way back —
    /// the storage format is line-based. It is flattened going in, so the message survives whole.</summary>
    [Fact]
    public void ToStorage_RoundTripsAMessageThatContainsNewlines()
    {
        var note = new CrashNote("frame tick", "InvalidOperationException",
            "line one / line two", ["Map.OnTick(Double)"], 5);

        CrashNote flattened = CrashNote.From("frame tick",
            new InvalidOperationException("line one\nline two"), whenUtcTicks: 5);
        Assert.DoesNotContain("\n", flattened.Message);

        Assert.True(CrashNote.TryParse(note.ToStorage(), out CrashNote read));
        Assert.Equal("line one / line two", read.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a crash note at all")]
    [InlineData("crash-v2\nfrom the future\nX\nY\n1")]
    [InlineData("crash-v1\nsource\nType\nmessage\nnot-a-number")]
    public void TryParse_RefusesAnythingItDidNotWrite(string? stored)
    {
        Assert.False(CrashNote.TryParse(stored, out _));
    }

    /// <summary>The line the Captain's desk shows: enough to identify the bug, short enough to read on
    /// the way past.</summary>
    [Fact]
    public void Describe_NamesTheTypeTheMessageAndWhereItWentOff()
    {
        Exception ex = Thrown(static () => throw new InvalidOperationException("boom"));
        CrashNote note = CrashNote.From("frame tick", ex, whenUtcTicks: 1);

        string line = note.Describe();
        Assert.Contains("InvalidOperationException", line);
        Assert.Contains("boom", line);
        Assert.Contains("frame tick", line);
        Assert.DoesNotContain("\n", line);

        // …and the full form keeps the rest of the frames, one per line, for the paste.
        Assert.StartsWith(line, note.DescribeFully());
    }

    /// <summary>A stack trace is trimmed of its "   at " noise and capped — four frames name the culprit,
    /// and a note the size of a screen is a note nobody copies.</summary>
    [Fact]
    public void TopFrames_TrimsTheNoiseAndStopsAtTheCap()
    {
        string stack = string.Join("\n", Enumerable.Range(0, 12).Select(i => $"   at Some.Frame{i}(Int32 x)"));

        IReadOnlyList<string> frames = CrashNote.TopFrames(stack);

        Assert.Equal(CrashNote.MaxFrames, frames.Count);
        Assert.Equal("Some.Frame0(Int32 x)", frames[0]);
        Assert.All(frames, f => Assert.DoesNotContain("   at ", f));
    }

    [Fact]
    public void TopFrames_IsEmptyForAnExceptionThatWasNeverThrown()
    {
        Assert.Empty(CrashNote.TopFrames(null));
        Assert.Empty(CrashNote.TopFrames("   "));
    }
}
