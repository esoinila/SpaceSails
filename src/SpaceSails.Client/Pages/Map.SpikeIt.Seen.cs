using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1202 slice 3 · <b>THE TAIL AT HER TABLE — the page's half.</b> SPIKE IT (<c>Map.SpikeIt.cs</c>) lets the
/// captain take Rauha Lind's pages off the far gallery table while she feeds the machine; the grey coat
/// (<c>Map.TailBehindYou*.cs</c>, #1062) follows the captain through Selene Gate. Here the two cross: if the coat
/// is still on the captain and holds his band at the moment TAKE THE PAGES is pressed, the take was SEEN. A seen take cannot
/// spike — her pages are back on her table by the next watch, the story runs on time, and the client's receipt
/// says why. Losing the tail first is how a captain keeps the take clean.
///
/// <h3>What SEEN does, and nothing else</h3>
///
/// <para>It is a flag on the spike (<see cref="CarryThePress.Passage.Watched"/>, one more key on slice 2's line
/// in the quest's free slot) and never a fourth outcome. Its only effects are the two book lines
/// (<see cref="SpikeIt.SeenTakeEntry"/> filed beside the take line, <see cref="SpikeIt.SquaredLine"/> on entering
/// the gallery afterwards), the forced LATE (<see cref="SpikeIt.TheWindowFor"/>), and the receipt line
/// (<see cref="SpikeIt.LateReceipt"/>). No page field, no <c>TableTalk</c> field.</para>
///
/// <h3>Kosh</h3>
///
/// <para>Nothing at the table says the coat saw: the card reads what an unseen take reads, and no pulse is said.
/// The book is the only tell before the window.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>
    /// #1202 slice 3 · <b>DOES THE COAT HAVE HIM, THIS INSTANT?</b> A man behind the captain on this floor — on
    /// him (<see cref="Errand.BehindYou"/>), not lost (<c>_coatLost</c>, the tail's own latch, written the moment
    /// his blind clock runs out) — holding his band on the captain (<see cref="SpikeIt.TheTakeIsSeen"/>). The
    /// tail's own contact, read and never re-derived: no second line-of-sight model, and nothing here moves him.
    /// With no man on the floor it is false and asks nothing.
    /// </summary>
    private bool TheCoatSeesTheTake()
    {
        if (_coatLost)
        {
            return false;
        }

        foreach (Walker w in _barAfoot)
        {
            if (w.For == Errand.BehindYou)
            {
                double dx = w.Walk.X - _avatarX, dy = w.Walk.Y - _avatarY;
                return SpikeIt.TheTakeIsSeen(aManIsOnYou: true, rangeDu: System.Math.Sqrt((dx * dx) + (dy * dy)));
            }
        }

        return false;
    }

    /// <summary>
    /// #1202 slice 3 · <b>HER STACK IS SQUARED.</b> After a seen take and before the window, the first time the
    /// captain ENTERS the gallery — he took the pages sitting in it, so he must have been out of it since — the
    /// line is said where he is looking and filed in the book under 📰, once. Called from her frame at the gallery
    /// while the spike is in hand; with no seen take it reads the line and writes nothing.
    /// </summary>
    /// <returns>The contract to go on with — the copy, when its line was rewritten.</returns>
    private Quest TheStackIsSquaredIfSeen(Quest q, string berth)
    {
        CarryThePress.Passage p = PassageOf(q);
        if (!p.Watched || p.Stack == SpikeIt.Stack.Told)
        {
            return q;
        }

        (SpikeIt.Stack next, bool tell) =
            SpikeIt.TheStackOnEntering(p.Stack, HavenInterior.InTheGallery(berth, _avatarX, _avatarY, _havenFloor));
        if (next == p.Stack)
        {
            return q;
        }

        if (tell)
        {
            SayItWhereTheyAreLooking(SpikeIt.SquaredLine);
            FileNote(SpikeIt.SquaredLine, CarryThePress.Glyph);
        }

        Quest rewritten = RewritePassage(q, p with { Stack = next });
        StateHasChanged();
        return rewritten;
    }
}
