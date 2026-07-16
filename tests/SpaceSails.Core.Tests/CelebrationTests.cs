namespace SpaceSails.Core.Tests;

/// <summary>
/// #185 — a completion is a CELEBRATION, not a silent credit. The fanfare carries the payment, the
/// giver's grateful voice, and the parrot's song; and the moment seeds a real, saved relationship
/// history that increments per contact.
/// </summary>
public class CelebrationTests
{
    [Fact]
    public void Completion_EmitsCelebration_WithPaymentDetails()
    {
        MissionCelebration party = Celebrations.ForCompletion(
            title: "Run a parcel to Enceladus",
            giverName: "MADAM COIL",
            paidCredits: 340,
            missionsWithGiver: 1,
            parrotCounter: 0);

        Assert.Equal("Run a parcel to Enceladus", party.Title);
        Assert.Equal("MADAM COIL", party.GiverName);
        Assert.Equal(340, party.PaidCredits);          // the money is INSIDE the celebration
        Assert.Equal(1, party.MissionsWithGiver);
        Assert.False(string.IsNullOrWhiteSpace(party.ParrotSong)); // the parrot SINGS
    }

    [Fact]
    public void RingsideBarLady_BuysTheRound()
    {
        // Owner, #185 verbatim: the lady at the Ringside bar (Madam Coil) says "Drinks free!!"
        MissionCelebration party = Celebrations.ForCompletion(
            "Run a parcel to Titan", "MADAM COIL", 300, 1, 0);

        Assert.Contains("Drinks free", party.GiverThanks, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EachGiver_ThanksInTheirOwnVoice()
    {
        Assert.NotEqual(Celebrations.GiverThanks("MADAM COIL"), Celebrations.GiverThanks("THE FIXER"));
        Assert.NotEqual(Celebrations.GiverThanks("GILT-EYE"), Celebrations.GiverThanks("THE MAGPIE"));
        // A nameless stranger still gets a warm, non-empty nod.
        Assert.False(string.IsNullOrWhiteSpace(Celebrations.GiverThanks("A STRANGER AT THE BAR")));
    }

    [Fact]
    public void ParrotSong_IsCelebratory_AndDeterministic()
    {
        // Shares the Parrot channel, so client bubble and pop-up read one source of words.
        Assert.Equal(
            Parrot.Line(Parrot.Squawk.ContractPaid, 0),
            Celebrations.ForCompletion("t", "g", 1, 1, 0).ParrotSong);
        Assert.Equal(
            Celebrations.ForCompletion("t", "g", 1, 1, 2).ParrotSong,
            Celebrations.ForCompletion("t", "g", 1, 1, 2).ParrotSong);
    }
}

/// <summary>The relationship seam (#185): a saved per-contact history the future relationship
/// system reads — "we now have a history with the lady at the Ringside bar."</summary>
public class ContactLedgerTests
{
    [Fact]
    public void FirstCompletion_CreatesHistory_ThenIncrements()
    {
        var ledger = new ContactLedger();
        Assert.False(ledger.For("MADAM COIL").HasHistory); // no dealings yet

        ContactHistory first = ledger.RecordCompletion("MADAM COIL", "MADAM COIL", 300, simTime: 100);
        Assert.True(first.HasHistory);
        Assert.Equal(1, first.MissionsCompleted);
        Assert.Equal(300, first.TotalPaidCredits);
        Assert.Equal(100, first.LastCompletedSimTime);

        ContactHistory second = ledger.RecordCompletion("MADAM COIL", "MADAM COIL", 340, simTime: 500);
        Assert.Equal(2, second.MissionsCompleted);      // the count increments
        Assert.Equal(640, second.TotalPaidCredits);     // pay accumulates
        Assert.Equal(500, second.LastCompletedSimTime);
    }

    [Fact]
    public void DifferentContacts_KeepSeparateHistories()
    {
        var ledger = new ContactLedger();
        ledger.RecordCompletion("MADAM COIL", "MADAM COIL", 300, 10);
        ledger.RecordCompletion("THE FIXER", "THE FIXER", 4200, 20);

        Assert.Equal(1, ledger.For("MADAM COIL").MissionsCompleted);
        Assert.Equal(1, ledger.For("THE FIXER").MissionsCompleted);
        Assert.Equal(2, ledger.Entries.Count);          // both are saved facts
    }

    [Fact]
    public void UnknownContact_ReadsAsBlankSlate_NotAnError()
    {
        var ledger = new ContactLedger();
        ContactHistory unknown = ledger.For("NOBODY");
        Assert.Equal(0, unknown.MissionsCompleted);
        Assert.False(unknown.HasHistory);
    }
}
