using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #652 · THE HONEST ROAD'S LASTING HALF, WHICH LASTED NOTHING.
///
/// <para>The wreck lane's two roads differ in four spendable ways — credits, heat, hot cargo, the crew's
/// opinion — and in a fifth that was a boolean nobody read. The outcome card said <i>"Somebody now owes you a
/// straight answer"</i> and there was no somebody anywhere in the tree: nothing recorded who, nothing
/// persisted, nothing took your call. Which matters because it is the owner's own framing of why the road
/// exists (2026-07-28): <i>"Honest salvage and contacts that may provide in future, or fast win
/// immediately."</i> Against 10–15% versus 100%, "a nicer sentence" is not a decision.</para>
///
/// <para>This pins #652's option 1 and only that: the contact has a NAME, goes on the book the game already
/// keeps, and the card says whose name is under the finding. No new reputation track and no rebalanced
/// fractions — those are FLAGGED separately and pricing the honest road as though it already had its
/// mechanic is the mistake the issue closes with a warning about.</para>
/// </summary>
public sealed class TheHonestFilingEarnsSomebodyTests
{
    private static readonly Derelict.Wreck Hull =
        new("derelict-maren-vey", "Maren Vey", Derelict.WreckCause.DriveFailure, 40_000, 12.0);

    private static string WreckSource()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client", "Pages");
            if (Directory.Exists(candidate))
            {
                return File.ReadAllText(Path.Combine(candidate, "Map.Wreck.cs"));
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the client above {AppContext.BaseDirectory}");
    }

    private static string OutcomeCardSource()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client", "Pages", "Map.razor");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }
            at = at.Parent;
        }

        throw new FileNotFoundException("could not find Map.razor");
    }

    /// <summary>
    /// THE WORLD REACHES THE STATE, not just the assertion. A real wreck, filed truthfully through the shipped
    /// pricing function, earns the contact — and the two roads that must NOT earn one still do not. Without
    /// this the tests below would be claims about a flag nothing sets.
    /// </summary>
    [Fact]
    public void OnlyAFilingThatReadHerRightEarnsAnybody()
    {
        Derelict.SalvageOutcome filed =
            Derelict.Resolve(Hull, Derelict.SalvageChoice.FileTheReport, Hull.Cause);
        Derelict.SalvageOutcome misfiled =
            Derelict.Resolve(Hull, Derelict.SalvageChoice.FileTheReport, Derelict.WreckCause.Mutiny);
        Derelict.SalvageOutcome stripped =
            Derelict.Resolve(Hull, Derelict.SalvageChoice.StripAndSayNothing, null);

        Assert.True(filed.ContactEarned);
        Assert.False(misfiled.ContactEarned);   // a bad report is worse than no report
        Assert.False(stripped.ContactEarned);   // a ship that was never found makes no friends
    }

    /// <summary>The half that used to evaporate: somebody is now in the book, by name, through the ledger's
    /// own existing seam — so it round-trips through the vault with every other relationship.</summary>
    [Fact]
    public void TheFilingPutsANamedAssessorOnTheContactLedger()
    {
        Derelict.SalvageOutcome filed =
            Derelict.Resolve(Hull, Derelict.SalvageChoice.FileTheReport, Hull.Cause);
        Assert.True(filed.ContactEarned);

        (string id, string name) = Derelict.ContactFor(Hull);
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.False(string.IsNullOrWhiteSpace(name));

        var ledger = new ContactLedger();
        Assert.False(ledger.For(id).HasHistory);   // nobody yet — the state this lane starts from

        ledger.AddGoodwill(id, name, Derelict.ContactGoodwill);

        ContactHistory after = ledger.For(id);
        Assert.True(after.HasHistory);
        Assert.Equal(Derelict.ContactGoodwill, after.Goodwill);
        Assert.Equal(name, after.DisplayName);
    }

    /// <summary>Pure, and seeded from the hull: a captain who files on the same wreck twice does not meet two
    /// different people, and two different hulls are not guaranteed the same one.</summary>
    [Fact]
    public void TheAssessorIsTheSamePersonForTheSameHullEveryTime()
    {
        Assert.Equal(Derelict.ContactFor(Hull), Derelict.ContactFor(Hull));

        var others = new HashSet<string>();
        for (int i = 0; i < 40; i++)
        {
            var w = new Derelict.Wreck($"derelict-{i}", $"Hull {i}", Derelict.WreckCause.HullBreach, 10_000, 4.0);
            others.Add(Derelict.ContactFor(w).Id);
        }

        // Not one name for the whole system, and not a different name every single time either — a small cast
        // that the player can start to recognise is the point of putting them in a ledger at all.
        Assert.True(others.Count > 1, "every wreck in the system is countersigned by the same person");
    }

    /// <summary>The sentence names them and stops. It is the #761 half of this issue: the plot-significant
    /// fact is what the player reads, not a flag a razor branch happens to test.</summary>
    [Fact]
    public void TheLineSaysWhoSignedIt()
    {
        (string _, string name) = Derelict.ContactFor(Hull);
        string line = Derelict.ContactLine(name);

        Assert.Contains(name, line, StringComparison.Ordinal);
        Assert.DoesNotContain("Somebody now owes you", line, StringComparison.Ordinal);
    }

    /// <summary>And it is wired where the decision is spent, and read where the captain is looking. The
    /// outcome card is a pop-up, so the #769 seam applies: the answer lands ON the card that was pressed.</summary>
    [Fact]
    public void TheWreckLaneBooksItAndTheOutcomeCardSaysIt()
    {
        string wreck = WreckSource();
        Assert.Contains("BookTheSalvageContact", wreck, StringComparison.Ordinal);
        Assert.Contains("Derelict.ContactFor(wreck)", wreck, StringComparison.Ordinal);
        Assert.Contains("_contacts.AddGoodwill(", wreck, StringComparison.Ordinal);
        Assert.Contains("Derelict.ContactLine(name)", wreck, StringComparison.Ordinal);

        string card = OutcomeCardSource();
        Assert.Contains("Derelict.ContactLine(signedBy)", card, StringComparison.Ordinal);
        Assert.DoesNotContain("🤝 Somebody now owes you a straight answer.", card, StringComparison.Ordinal);

        // The cue is the shipped idiom and stays the shipped idiom — this lane adds no audio system.
        Assert.Contains("PlayCue(outcome.ContactEarned ? \"reveal\" : \"board\")", wreck, StringComparison.Ordinal);
    }
}
