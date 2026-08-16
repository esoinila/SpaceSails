using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — the PAGE's half of the challenge: the name on your own papers, which is the captain thread's business and not the round's, and the four things the markup and the cancel chain still ask the round for by name.
public sealed partial class Map
{
    /// <summary>#836 · The name on your own papers. Everything the site issued you carries it, spelled the
    /// way the rota spells it — read off the thread the captain is being played on, so a renamed captain's
    /// wallet renames itself with them.</summary>
    private string NameOnYourOwnPapers =>
        ActiveThreadInfo is { } row ? Captains.For(row).Name : Captains.Name(_activeThreadId);

    // #870 lane 6′c · IT STAYED HERE, and it is on IPatrolHost. It is an IDENTITY, read off the captain
    // THREAD this game is being played on — two page members deep, and nothing whatever to do with a round.
    // The wallet asks for the ANSWER, and Map.razor still asks for it by this name to letter a fan row.

    /// <inheritdoc cref="Patrol.TheHail"/>
    private void TheHail(Guard g) => _patrol.TheHail(g);

    /// <inheritdoc cref="Patrol.TheWalletFan"/>
    private IReadOnlyList<Satchel.Item> TheWalletFan => _patrol.TheWalletFan;

    /// <inheritdoc cref="Patrol.WalletFanIsUp"/>
    private bool WalletFanIsUp => _patrol.WalletFanIsUp;

    /// <inheritdoc cref="Patrol.ChooseThePaper"/>
    private void ChooseThePaper(Satchel.Item paper) => _patrol.ChooseThePaper(paper);
}
