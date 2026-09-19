using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #701 · READING A SHELF IN SOMEBODY'S ROOM — the occupant layer's one verb.
///
/// <para>Owner's morning expansion: <i>"They have their work books and they have their freetime books there…
/// provide soft clues about what kind of people stay in those rooms."</i> Two shelves on the wall of every
/// room somebody was given: what they did, and who they were.</para>
///
/// <para>THE ODD BOOK'S OWN PATH AND NOT A SECOND ONE. It opens the same caption-only look-card, it files to
/// the same casebook, and it writes to the SAME read-list the shelves in empty rooms have ridden the vault
/// on since #701 v1 (<c>Vault.Progress.OddBooksRead</c>) — the ids are namespaced in Core, so one store
/// serves both halves of this issue and no second reader exists to disagree with the first. What this file
/// adds is a press, because a shelf is a FIXTURE and the odd book is the room's own haul table.</para>
///
/// <para>Nothing enters the satchel, no credits change hands, no lead is granted and <b>the room is never
/// struck off</b> — which is what makes re-reading possible at all. Looking is free; the casebook learns the
/// gist once per shelf per game-thread (#603), which Core decides and this method never re-decides.</para>
/// </summary>
public partial class Map
{
    /// <summary>#701 · Stop at a shelf, read what is on it. The card is the shelf line and the card text,
    /// composed the way #736 composes every card that carries its own saying — one object card, one text,
    /// never a pulse playing under the card's own blur.</summary>
    private void HiveShelfInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveShelf } spot)
        {
            return;
        }

        // WHICH SHELF, ASKED OF CORE AND NOT OF THE CONSOLE. The plate on the spot is the shelf line, and a
        // press that read its own card back off the label it was drawn with would be the client keeping a
        // second copy of authored prose — the very drift the #701 v1 guards were written against. The floor
        // is rebuilt (pure and deterministic per body and level, like every other press down here) and the
        // shelf is found where it has always been.
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        Shelves.Shelf? standing = null;
        double bestSq = double.MaxValue;
        foreach (Shelves.Shelf shelf in floor.TheShelves)
        {
            double dx = shelf.X - spot.X, dy = shelf.Y - spot.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < bestSq)
            {
                bestSq = d2;
                standing = shelf;
            }
        }

        // A console spot IS a shelf's own coordinate, so the nearest one is the one being pressed; the
        // tolerance exists only so a future renderer nudging a plate by a hair cannot silently open the
        // other shelf in the room. The same match TheLockComesOff makes, and for the same reason.
        if (standing is not { } read || bestSq > 1.0)
        {
            return;
        }

        Shelves.Reading look = Shelves.Read(read, _oddBooksRead);

        // #528's idiom, caption-only: there is no art file for these and one that is wired but unpainted
        // would render an img the browser hides — the lifeboat-muster precedent, which the odd book already
        // took. The title is the shelf line the room is showing, so the captain reads the same words on the
        // card that the wall just said.
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            look.Title, null, look.Card);

        if (look.Gist is { } gist)
        {
            // #741 · AND THE BOOK IS TOLD WHAT THE ENTRY IS ABOUT, by the author, at writing time. The
            // subject is the PLACE — a shelf names nobody, and minting a person out of one would be the game
            // doing the detecting (§12.4). Core composes it beside the sentence that names it.
            //
            // FileNoteAbout and not ShowAndFile: the saying is on the card above, and a pulse would play
            // under the card's own blur (#686/#736).
            FileNoteAbout(gist, Shelves.Glyph, Shelves.SubjectsFor(TheBooksNameForHere()));
            _oddBooksRead = [.. look.Filed];
        }

        RequestVaultSave();
    }
}
