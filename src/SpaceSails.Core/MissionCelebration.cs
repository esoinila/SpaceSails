namespace SpaceSails.Core;

/// <summary>
/// The fanfare a finished contract earns (#185, owner: "It is a CELEBRATION.... POP UP PARROT
/// SINGs maybe even. Drinks free!!!!"). A pure payload — what was delivered, to whom, the pay, the
/// giver's thanks in their own voice, how many jobs we've now done together, and the parrot's song
/// — so the pop-up, the ledger, and the tests all read one celebration. The money is INSIDE this:
/// a completion never just silently books credits again.
/// </summary>
public readonly record struct MissionCelebration(
    string Title,
    string GiverName,
    string GiverThanks,
    int PaidCredits,
    int MissionsWithGiver,
    string ParrotSong);

/// <summary>Builds the <see cref="MissionCelebration"/> at the payment moment, and owns the task
/// givers' grateful voices.</summary>
public static class Celebrations
{
    /// <summary>The task giver's gratitude, in their own voice. The lady at the Ringside bar
    /// (Madam Coil, the cargo-run giver) buys the round — "Drinks free!!" (owner, #185, verbatim).
    /// Others thank in character; a nameless stranger gets a plain, warm nod.</summary>
    public static string GiverThanks(string giverName)
    {
        string g = (giverName ?? string.Empty).ToUpperInvariant();
        if (g.Contains("COIL"))
        {
            return "\"Drinks free!! You got my parcel through, captain — the whole bar's on my tab tonight!\"";
        }

        if (g.Contains("GILT"))
        {
            return "\"Knew you had the eye for it. Pleasure doing business — the good word comes to you first now.\"";
        }

        if (g.Contains("FIXER"))
        {
            return "\"Clean work. We never spoke — but my door's open to you now, quiet-like.\"";
        }

        if (g.Contains("MAGPIE"))
        {
            return "\"Shiny. Very shiny. Bring me another and there's always a perch for you here.\"";
        }

        return "\"Much obliged, captain. Word travels out here — you'll find a warmer welcome next time.\"";
    }

    /// <summary>Build the fanfare for a paid contract, folding in how many jobs we've now done for
    /// this giver (<paramref name="missionsWithGiver"/>, from the <see cref="ContactLedger"/>) and
    /// the bird's celebratory song (the existing <see cref="Parrot"/> channel, so client and tests
    /// share one source of words).</summary>
    public static MissionCelebration ForCompletion(
        string title, string giverName, int paidCredits, int missionsWithGiver, int parrotCounter) =>
        new(
            title,
            giverName,
            GiverThanks(giverName),
            paidCredits,
            missionsWithGiver,
            Parrot.Line(Parrot.Squawk.ContractPaid, parrotCounter));
}
