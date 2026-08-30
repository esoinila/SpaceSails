namespace SpaceSails.Client.Pages;

// Subject: #1021 — the galley, after the desk. The rum locker and the news wire are a POP-UP CARD now, and
// this file is the whole of its state: one gate, three verbs, and the guard that says the desk band may
// never draw it again.
public partial class Map
{
    /// <summary>
    /// #1021 · <b>THE GALLEY IS A CARD.</b> Owner, on the full-screen Galley desk, verbatim:
    /// <i>"We want to keep the news feed... refactor it so it can be elsewhere also, but this UI MUST GO!...
    /// This was our first version, it has no gen AI or visibility to the bar surroundings. So keep the
    /// features but I want it done in pop-up style like the work the case is."</i>
    ///
    /// <para><b>What was wrong with it.</b> The Galley desk was the desk band's seventh full-screen screen:
    /// a news column and a rum card over a bar photograph darkened to 0.82/0.88 — which is to say, over
    /// black. Sitting down at it took the whole glass and put the captain in a room with no room in it. Its
    /// one real door was the deck's CANTINA console, and pressing [E] there — standing IN the cantina, with
    /// the cantina drawn under your feet — switched the screen away from the cantina to a picture of one.
    /// That is the owner's "no visibility to the bar surroundings" exactly: the bar was behind you and the
    /// desk was in the way.</para>
    ///
    /// <para><b>What it is now.</b> The satchel's grammar, which is what "like the work the case is" names:
    /// a card over a click-to-close backdrop, the room still live behind it, one ✕, and Esc. Opened at the
    /// cantina it hangs over the cantina's own art — the surroundings the owner asked for, with no new
    /// picture drawn for it. Opened with <kbd>6</kbd> from any desk it hangs over that desk.</para>
    ///
    /// <para><b>THE ENUM DOES NOT MOVE.</b> <see cref="ShipDesk.Galley"/> is still 6 and still on the tab
    /// bar; what has gone is its ability to become <c>_activeDesk</c>. Removing the member would have
    /// renumbered <see cref="ShipDesk.Deck"/> and <see cref="ShipDesk.Captain"/> under every keyed and
    /// persisted thing that reads them — the per-desk layer sets, the tab order, the digit keys — which is a
    /// far larger change than the one the owner asked for. So the desk stays declared and unreachable AS A
    /// DESK: <c>SwitchDesk</c> forks here before it touches <c>_activeDesk</c>, and
    /// <c>TheGalleyIsACardNotADeskTests</c> holds both halves of that.</para>
    ///
    /// <para><b>The toggle is the satchel's law (#688), not a new one.</b> Owner, of the I key: <i>"If I
    /// press I when inventory is open, let's close it then."</i> A card you raise by reflex has to fall by
    /// the same reflex, so 6 — and the "6 Galley" chip, and the right-rail Galley chip, all of which funnel
    /// through <c>SwitchDesk</c> — is both doors.</para>
    /// </summary>
    private bool _galleyCardOpen;

    /// <summary>Raise it. Used by the deck's CANTINA console, which does NOT switch desks any more: the
    /// captain is standing in the cantina and the card is what the cantina hands them.</summary>
    private void OpenGalleyCard() => _galleyCardOpen = true;

    /// <summary>Put it down. The one house closer — the ✕, the backdrop, Esc and the second press of 6 all
    /// end here, so there is exactly one thing "the galley card is shut" can mean.</summary>
    private void CloseGalleyCard() => _galleyCardOpen = false;

    /// <summary>#688's law, applied: the key that opens it closes it.</summary>
    private void ToggleGalleyCard()
    {
        if (_galleyCardOpen)
        {
            CloseGalleyCard();
            return;
        }

        OpenGalleyCard();
    }
}
