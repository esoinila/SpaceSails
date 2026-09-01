namespace SpaceSails.Client.Pages;

// Subject: #949 — the plotting card. One gate, three verbs, and the argument for why a help page that
// already exists needed a second door that does not leave the game.
public partial class Map
{
    /// <summary>
    /// #949 · <b>THE PLOTTING CARD.</b> Owner, 2026-08-18, posting a screenshot of his own multi-step plan:
    /// <i>"We should have a help page where we show multi-step plan and the use of the schrub and burn. New
    /// player seeing this image would understand how to play. I really like the increment - and decrement
    /// options."</i>
    ///
    /// <para><b>The page already existed, and it was in the wrong place at the wrong moment.</b> #972 built
    /// <c>/help/nav</c> — eight steps, a drawn sketch each — and hung it off the Nav toolbar's <c>?</c> as
    /// <c>target="_blank"</c>. That is right for the read you do BEFORE you start and wrong for the only
    /// moment anybody actually presses <c>?</c>: mid-plan, panel open, one row not making sense. A full page
    /// in a second tab answers that question by taking the question off the screen.</para>
    ///
    /// <para><b>So the ? opens a card and the card opens the page.</b> Toolbar <c>?</c> (or the <c>?</c>
    /// key) raises this over the map — the panel still underneath it, the scrub still where he left it —
    /// and its foot hands the reader on to <c>/help/nav</c> for the long version and to the Guide for
    /// everything else. Nothing was taken away; a shorter road was put in front of the long one.
    /// TheNavHelpPageTeachesTheWholeLoopTests walks that whole chain, door to door, so neither hop can
    /// quietly stop resolving.</para>
    ///
    /// <para><b>It is a card in the house grammar</b> — the satchel's and the galley's: a
    /// <c>.view-object</c> over a click-to-close backdrop, a visible way out, and Escape
    /// (<see cref="TryDismissTopOverlay"/>). It answers the owner's pop-up ruling of 2026-08-24 like every
    /// other surface in this client, and <c>EveryPopUpCanBeDismissedTests</c> proves it by pressing.</para>
    /// </summary>
    private bool _navHelpOpen;

    /// <summary>Raise the card. The one verb the toolbar, the key and the law's own driver all go through,
    /// so a door that stopped working fails in a guard rather than nowhere (#1021's finding).</summary>
    private void OpenNavHelp() => _navHelpOpen = true;

    /// <summary>Put it away. Also the Escape chain's rung and the ✕'s verb — one closer, three callers,
    /// which is what stops a card from having two ideas about being shut.</summary>
    private void CloseNavHelp() => _navHelpOpen = false;

    /// <summary>
    /// What the <c>?</c> — the button and the key — actually does.
    ///
    /// <para>A TOGGLE, and the galley's own reasoning (#1021) applies: <c>?</c> is the key you press to ask
    /// a question and the key you press again when you have your answer, so a second press that re-opened
    /// what the first press opened would be a key that only works one way. Escape and the card's own way
    /// out are the OTHER two roads to closed, and neither of them can ever start it — a key that means
    /// "stop" may not start anything (#1038).</para>
    /// </summary>
    private void ToggleNavHelp()
    {
        if (_navHelpOpen)
        {
            CloseNavHelp();
            return;
        }

        OpenNavHelp();
    }
}
