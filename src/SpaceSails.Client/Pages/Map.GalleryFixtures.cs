using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// #1199 (2026-09-18) · THE TWO COIN MACHINES IN THE OBSERVATION WALK'S GALLERY.
//
// Owner, live on the walk: "maybe one of those pay-coin-to-use binoculars common on sightseeing spots with
// gen-AI image(s) — use with E … same for the vending machine", and, earlier the same afternoon, "a small
// vending machine cafeteria with a couple of tables there… the station likes to get the tourist money."
//
// WHAT IS HERE. Two presses. Each one takes the same handful of change out of the purse, raises a card with a
// picture on it, and — for the binoculars, once per captain — files one line in the book.
//
// WHAT IS DELIBERATELY NOT. No new prose (Core's, authored, verbatim). No new refusal (CoinSlot.ShortLine,
// the souvenir kiosk's own words since #379). No new price (GalleryFixtures.CoinCr, off the haven's own
// round). No roll — the binoculars ALTERNATE, because a fixture with two things to show that could hand you
// the same one twice is a slot machine, and the half worth finding is the second one.
//
// AND NOBODY IS THERE. A vending machine is unstaffed by definition, which is exactly why the owner's
// cafeteria can furnish a room whose entire content is that there is no one in it. Not one line below names
// the tracks, the buried rail, the hatch or anybody who might have put them there.
public partial class Map
{
    /// <summary>#1199 · How many times the binoculars have been fed this visit — the alternation's whole
    /// state, and a visit's own fact like the bar's feet: a different berth is a different room, and a count
    /// carried across a casting-off would be a machine remembering a captain it never met.</summary>
    private int _galleryLooksTaken;

    /// <summary>#1199 · Has the book already got the binoculars' one line? Once per captain, like every other
    /// gist in the field book — a second identical entry is the book repeating itself, which is the one thing
    /// a record the player re-reads must not do.</summary>
    private bool _galleryBinocularsFiled;

    /// <summary>#1199 · CASTING OFF IS THE ROOM FORGETTING, here as everywhere else on this deck. Called from
    /// <see cref="ForgetTheWalk"/>, which is the one place that knows a berth has changed — the alternation is
    /// about a machine in a room and the book's line is about a captain, so exactly one of the two is
    /// forgotten.</summary>
    private void ForgetTheGallerysMachines() => _galleryLooksTaken = 0;

    /// <summary>#1199 · What a coin costs HERE — the local keep's own round, a tenth of it, floored at a
    /// credit (<see cref="GalleryFixtures.CoinCr"/>). Asked of the berth the captain is clamped to rather
    /// than of the walk's own haven, so a machine that ever stands in a second station charges that station's
    /// prices; at a berth with no keep at all it falls back on the canteen's round, which is the game's one
    /// other statement of what standing a room costs.</summary>
    private int TheCoinPrice =>
        GalleryFixtures.CoinCr(
            (_dockedHavenId is { } berth ? Barkeeps.For(berth) : null)?.RoundPrice ?? CanteenTable.RoundPrice);

    /// <summary>
    /// #1199 · <b>TAKE THE COINS, OR SAY WHY NOT.</b> The one gate both machines pass through, so the two
    /// cannot come to two views of what a coin costs or of what an empty purse is told.
    ///
    /// <para>The refusal is OFF-PLATE — a pulse and never a card. A full-screen picture for <i>you have no
    /// money</i> is the house making a ceremony out of a refusal, and #774's own lesson is that the surface
    /// has to match the size of what happened. The words are the game's one machine refusal and not a second
    /// sentence written to say the same thing.</para>
    /// </summary>
    /// <returns>Whether the coins went in.</returns>
    private bool TheSlotTakesIt()
    {
        int price = TheCoinPrice;
        if (_credits < price)
        {
            ShowPulseMessage(GalleryFixtures.ShortLine(price));
            return false;
        }

        _credits -= price;
        return true;
    }

    /// <summary>
    /// #1199 · <b>THE COIN BINOCULARS AT THE RAIL.</b> Two coins, and the optics show one of two things.
    ///
    /// <para>Which one is <see cref="GalleryFixtures.LookAt"/>'s and never this file's: the first look is out
    /// over the grey, the second swings down through the glass floor, and after that it goes round again. The
    /// count is incremented AFTER the coins are taken, so a refused press does not quietly advance the
    /// machine and cost the captain the view they were reaching for.</para>
    ///
    /// <para>The book gets one line, once per captain, filed under the PLACE (#741's law, minted in Core
    /// beside the sentence that names it) — the binoculars are bolted to a room, and what they show is about
    /// the moon rather than about anybody in the bar. Filed and NOT pulsed: there is a card standing in front
    /// of the HUD, and a line played behind a backdrop is the bug #774 was opened for.</para>
    /// </summary>
    private void LookThroughTheBinoculars()
    {
        if (!TheSlotTakesIt())
        {
            return;
        }

        GalleryFixtures.Look look = GalleryFixtures.LookAt(_galleryLooksTaken);
        _galleryLooksTaken++;

        if (!_galleryBinocularsFiled)
        {
            _galleryBinocularsFiled = true;
            FileNoteAbout(
                GalleryFixtures.BinocularsNoteLine,
                ObservationWalk.Glyph,
                GalleryFixtures.BinocularsSubjects());
        }

        RaiseStoryBeat(StoryBeats.Beat.TheWalksBinoculars, look.ToString());
    }

    /// <summary>
    /// #1199 · <b>THE VENDING MACHINE.</b> A coin, and something wrapped in a language nobody aboard reads.
    ///
    /// <para><b>The purse and nothing else, and that is an audit rather than a shortcut.</b> The galley's own
    /// consumable seam is a POUR — <c>Interior.Barkeep.BuyDrink</c> and the tot it hands back, which is what
    /// the bond, the round and the gumshoe rung are all written about. There is no snack in this game: no
    /// item, no condition, no line that a wrapped cold thing out of a slot could honestly become. Pouring a
    /// tot for it would be the machine handing the captain a drink he did not buy and the bar's own economy
    /// quietly gaining a second till — a snack is not a drink. So the coin moves, the card says what came
    /// out, and nothing else in the world changes, which is the truthful version of this fixture.</para>
    /// </summary>
    private void UseTheVendingMachine()
    {
        if (!TheSlotTakesIt())
        {
            return;
        }

        RaiseStoryBeat(StoryBeats.Beat.TheGalleryVendor);
    }
}
