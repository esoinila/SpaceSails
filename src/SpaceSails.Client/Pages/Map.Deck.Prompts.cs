using SpaceSails.Client.Rendering;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #440 · WHAT THE DECK'S KEYBAR SAYS — the contextual strip along the bottom of the ship's own deck, of a
/// haven's floor, of any walked view that is not an excursion.
///
/// <para>The regolith has had a bar that NAMES THE VERB AT THE MOMENT IT APPLIES since #324 — a bot in the
/// sling spells out <c>T</c>, a chest in hand spells out <c>G</c>, a lock under the hand spells out
/// <c>F</c>. Off the regolith the same strip was one fixed sentence naming three keys, and it said those
/// three whatever the captain was standing at.</para>
///
/// <para><b>Which is how <c>B</c> came to be bound on every deck in the game and named on none of them.</b>
/// The favour bank (<c>Map.Quests.Bank</c>) opens at a contact's table and nowhere else, it is the only way
/// to put coin somewhere a collector cannot take it, and the only place in the shipped game that its key
/// was written down is the Captain's Guide — a manual, read before you play, which is not a telling at the
/// moment it matters. An affordance a player can only find by accident is a bug, not a secret (#212).</para>
///
/// <para>Composed HERE and handed down whole, for the reason <c>SurfaceHud.KeyHints</c> is: the bar is
/// written where the facts are, never worked out again inside the renderer (#591's one-reach lesson). The
/// two fixed sentences it builds on are <see cref="DeckView.DockedKeyHints"/> and
/// <see cref="DeckView.DeckKeyHints"/> — quoted, not re-typed, so the fallback the renderer draws for a
/// caller that hands nothing down and the line the page composes cannot come to disagree.</para>
/// </summary>
public partial class Map
{
    /// <summary>#440 · Is the ship clamped somewhere that has a floor to walk up into? Asked by the keybar
    /// below and by the <c>Docked:</c> the draw hands the renderer (<c>Map.Sim.Tick.Views</c>), from one
    /// place: a bar that said "walk up through the airlock" over a berth with nothing above it would be the
    /// screen describing a door the ship does not have.</summary>
    private bool DockedSomewhereWithAFloorAboveIt =>
        _dockedHavenId is not null && HavenInterior.HasInterior(_dockedHavenId);

    /// <summary>#440 · The deck's keybar, or <c>null</c> when there is nothing to add to the fixed sentence
    /// and the renderer may go on drawing it.
    ///
    /// <para>Null rather than the bare fallback on purpose: every caller that predates this — the warm
    /// first surface frame in <c>Map.Sim.Boot</c>, the fingerprint benches — draws the exact bar it drew
    /// before, byte for byte, because nothing new is composed for it.</para></summary>
    private string? BuildDeckKeyHints()
    {
        // The excursion owns the bar while there is one (BuildSurfaceKeyHints, and the renderer's ladder
        // puts it first anyway). Nothing here has anything to add to the regolith.
        if (_surface is not null || !_deckMode)
        {
            return null;
        }

        var parts = new List<string> { DockedSomewhereWithAFloorAboveIt ? DeckView.DockedKeyHints : DeckView.DeckKeyHints };

        // ── #440 · THE BANK, AT THE ONE FIXTURE THAT HAS ONE ──
        //
        // The same question the key itself asks (OpenBankAtBar → NearestConsoleSpot, Kind: BarPatron), so
        // the bar can never offer the press a step further away than the press will answer — the lesson
        // #723 paid for with "E — dig" on poured rockcrete, and the one the [E] plate learned when it lit
        // over every console in reach instead of the one that answers.
        //
        // It says the VERB and not the contact's name: a name here would be a second spelling of the one
        // OpenBankAtBar strips off the plate, and the keybar's register is three words, never a sentence.
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is { Kind: DeckPlan.ConsoleKind.BarPatron })
        {
            parts.Add("💰 B — open an account at this table");
        }

        // #338 · The sound switch, spelled out the way the regolith's bar has always spelled it
        // (BuildSurfaceKeyHints' last entry). M is bound everywhere in the game and, until this line, was
        // written down in exactly one place a captain who never lands would never see.
        parts.Add(_audioEnabled ? "🔊 M — mute" : "🔇 M — unmute");
        return string.Join(" ∙ ", parts);
    }
}
