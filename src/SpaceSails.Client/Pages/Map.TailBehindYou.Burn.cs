using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE BURN (#1062, #1210) — a port the captain did quiet business at while watched is walked through
/// ahead of him: the one burn tag written, read through one predicate, and spent by the telling.
///
/// <para>Split out of <c>Map.TailBehindYou.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field.</para>
/// </summary>
public partial class Map
{
    // ── THE BURN ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1062 · Is there a man on this floor right now with the captain's back in front of him? The
    /// walker list is the truth about who is afoot, so it is counted rather than tracked — and a man who has
    /// been shaken is not on it, which is the whole of what shaking him buys.</summary>
    private bool TheCoatIsAfoot()
    {
        foreach (Walker w in _barAfoot)
        {
            if (w.For == Errand.BehindYou)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// #1062 · <b>THE PLACE IS BURNED, AND NOTHING IS SAID ABOUT IT.</b> Called from the ONE strike-off both
    /// of a berth's quiet verbs already share, so the favour across a table and the code bought at a desk
    /// cannot come to two views of what being watched costs.
    ///
    /// <para><b>It is DETERMINISTIC and it is SILENT.</b> No roll decides it — a man was standing behind the
    /// captain while he did a thing that says where he goes, and that is the whole condition. And not one
    /// word is raised on this frame: the captain gets what he came for, walks out, and finds out later. That
    /// is #761 read exactly as it is written, and it is also the only version of this beat that is any good —
    /// a warning at the moment of the act would turn the tail into a mechanic the player manages, and the
    /// entire feature is that he does not know.</para>
    ///
    /// <para><b>He has to be able to see it.</b> A captain who shook the man first, or who never let him onto
    /// the floor, does his business unwatched — which is the counter-play, and it is made of the same nine
    /// seconds of stone the rest of this file is made of.</para>
    /// </summary>
    private void TheyBurnThisPlaceIfSomebodyIsWatching(string portId)
    {
        if (_coatLost || !TheCoatIsAfoot())
        {
            return;
        }

        _roomsTurnedOver.Add(TheTailBehindYou.BurnTag(portId));
        _coatBurnedThisVisit.Add(portId);
        RequestVaultSave();
    }

    /// <summary>#1062 · <b>HAS SOMEBODY BEEN THROUGH THIS PLACE AHEAD OF THE CAPTAIN?</b> The ONE burn
    /// predicate, and the only reader the tag has.
    ///
    /// <para>Slice 2 shipped it with a single caller — the strike-off the fence's key and the bar favour
    /// already shared — so those two could not come to two views of what being watched costs. Slice 2c adds
    /// the third quiet verb at a port, the unlisted parcel's row (<c>ParcelOnOffer</c>), and adds it by
    /// asking THIS question rather than by minting a second predicate or a second tag: three verbs, one
    /// question, one register. The burn still needs no refusal of its own anywhere in the game, because
    /// every row that answers to it is drawn where it applies and absent where it does not.</para></summary>
    private bool ThisPlaceWasWalkedFirst(string portId) =>
        _roomsTurnedOver.Contains(TheTailBehindYou.BurnTag(portId));

    /// <summary>
    /// #1062 · <b>AND WHEN HE COMES BACK, IT IS TIDY.</b> One pulse and one line in the book, at the burned
    /// place, on the visit AFTER the one that burned it — and then the burn is spent.
    ///
    /// <para>Spending it here rather than counting watches is the smallest honest shape: the cost is one
    /// visit's worth of what that port deals, the telling and the spending are the same event, and there is
    /// no window a captain can miss. A burn that expired on a clock would be a consequence the player could
    /// fail to be told about, which is the one thing #761 forbids.</para>
    ///
    /// <para>The note goes through the one funnel under the PLACE, so it stacks on THREADS with the losing
    /// note from the same evening and the captain reads the two of them in the order they happened — which
    /// is the whole of the inference: <i>he was behind me, and then this place had been gone through</i>. The
    /// book never says that. It says what happened.</para>
    /// </summary>
    private void TheBurnIsToldHere(string portId)
    {
        if (_coatBurnedThisVisit.Contains(portId) || !ThisPlaceWasWalkedFirst(portId) || !_ashore)
        {
            return;
        }

        _roomsTurnedOver.Remove(TheTailBehindYou.BurnTag(portId));
        ShowPulseMessage(TheTailBehindYou.TheBurnLine, PulseRank.Beat);

        string place = DockedStationName();
        FileNoteAbout(
            TheTailBehindYou.BurnNote(place), TheTailBehindYou.Glyph, TheTailBehindYou.BurnSubjects(place));
        RequestVaultSave();
    }
}
