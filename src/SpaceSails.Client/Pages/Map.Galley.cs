using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: #1021/#1022 — the galley, after the desk. The rum locker and the news wire are a POP-UP CARD now,
// and this file is the whole of its state: one gate, three verbs, the guard that says the desk band may
// never draw it again — and the man behind the counter, who answers both of the card's doors.
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
    /// captain is standing in the cantina and the card is what the cantina hands them.
    ///
    /// <para>#1022 · The card opening is a beat: B-7V looks up. Every door onto the card funnels through
    /// here (the ✕'s twin <c>ToggleGalleyCard</c>, the 6 key, the chips, the cantina console), so there is
    /// one place the greeting can happen and none of them has to know about him.</para></summary>
    private void OpenGalleyCard()
    {
        _galleyCardOpen = true;
        TheTenderLooksUp();
    }

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

    // ── #1022 · B-7V, "THE TENDER" ────────────────────────────────────────────────────────────────────
    //
    // Owner, live (2026-08-30): "The dialog, one imagines, is a set of phrases to keep the customer talking
    // :-D ... but there is something heart warming in those scenes at the same time."
    //
    // Every word he has is in Core.TheTender, which is also where the picking law lives — this half is only
    // the state a page has to hold for him: whose sitting this is, which beat we are on, and what he last
    // said. The two doors are OpenGalleyCard (the card came up) and PourRumFromGalley (a tot went in the
    // glass); both of them come here, and neither of them decides anything.

    /// <summary>The sitting at the counter, or null before the first one. It survives the card being shut
    /// and reopened — that is the whole point of the idle pool, which is what a captain who comes back gets
    /// instead of a second greeting.</summary>
    private TheTender.Sitting? _tenderSitting;

    /// <summary>When he was last spoken to, on the page's own frame clock. A gap of
    /// <see cref="NerveModel.SpreeGapMs"/> — the same window the rum ledger starts a fresh tot count on —
    /// ends the sitting, so one visit to the counter is one spree and one sitting.</summary>
    private double _tenderTouchedMs = double.MinValue;

    /// <summary>Which beat of the game this is, counted across sittings. It is the salt that moves his pick
    /// on: two beats at the same sim-second are still independent draws, which matters because a captain can
    /// press the button twice inside one sim-minute.</summary>
    private int _tenderBeat;

    /// <summary>What he is saying right now, or null before he has said anything.</summary>
    private TheTender.Line? _tenderLine;

    /// <summary>#1022 QA · <c>?tender=flash</c> — force the rare roll for a tester.
    ///
    /// <para>It forces the ROLL and never the content, which is <c>?roll=</c>'s own philosophy (#746): which
    /// announcement he reaches for is still his own salted pick, and the once-a-sitting law still holds — so
    /// what a tester watches is the beat a captain would get, never a rigged one. Without it the channel is
    /// a 1-in-12 on a card most sessions open twice.</para></summary>
    private bool _tenderFlashCheat;

    /// <summary>The card came up — he looks up and says something.</summary>
    private void TheTenderLooksUp() =>
        _tenderLine = TheSittingNow().Open((long)SimTime, ++_tenderBeat, _tenderFlashCheat);

    /// <summary>A tot went in the glass. Called AFTER the pour, so the tot count it reads is the one the
    /// drink law just counted — the threshold is the ledger's, never a second copy of it.</summary>
    private void TheTenderPours() =>
        _tenderLine = TheSittingNow().Pour((long)SimTime, ++_tenderBeat, _rumTots, _tenderFlashCheat);

    /// <summary>The sitting this beat belongs to: the one in progress, or a fresh one if the captain has
    /// been away longer than a spree lasts.</summary>
    private TheTender.Sitting TheSittingNow()
    {
        double now = _lastTimestampMs ?? 0;
        if (_tenderSitting is null || now - _tenderTouchedMs >= NerveModel.SpreeGapMs)
        {
            _tenderSitting = new TheTender.Sitting();
        }

        _tenderTouchedMs = now;
        return _tenderSitting;
    }
}
