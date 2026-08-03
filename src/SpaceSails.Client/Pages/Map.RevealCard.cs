namespace SpaceSails.Client.Pages;

// Map.RevealCard — #528 · THE REVEAL CARD, GENERALISED.
//
// The owner, from the boat: "Let's add some cool gen-ai to places where we tell the story in the game. I
// think it makes a big difference to have that pop-up style we used in the reever vented room scenario.
// We have a lot of events that don't have that level of service yet."
//
// The vented-room card (Map.Venting.CheckVentPayoffUnderfoot) is the recipe, and it is four things, all
// load-bearing: a TITLE that names the place and the verb; ONE painted image of a consequence rather than
// an action shot; a CAPTION that describes evidence and stops; and it fires at the moment it explains the
// most. What it was NOT, until now, was reusable — it was a `WreckLook`, and only a wreck could raise it.
//
// This partial is the same card with the wreck taken out of it, so any beat in the game can raise one
// without inventing its own modal. It owns nothing but the state: the TEXT of every card lives in Core
// beside the predicate that decides the beat happened (KaamosLore.PlateFor is the first tenant), which is
// #634's law — a sentence built in the client is a sentence that can drift away from the sim.
//
// ── #633 · THIS FILE WAS Map.StoryPlate.cs AND THIS FIELD WAS CALLED `_storyPlate`. ──────────────────────
//
// So was `main`'s. The two branches answered the same owner request (#528, "universally in the game as long
// as it does not block the playing too much") on the same day, from opposite ends of one design: `main`
// built StoryBeats — a Core cadence + presentation law with a CARD form and a PLATE form — and this branch
// built the card form alone, raised by hand with text from Core. Reunited, the two `_storyPlate` fields
// collided on the compiler, which is the only reason it was caught in the first minute rather than in a
// playtest.
//
// The rename says which is which instead of pretending one of them is not there: what lives here is a CARD
// (a full-screen modal that stops you and waits to be dismissed) and `Map.StoryCards._storyPlate` is a
// PLATE (an edge flash that steals nothing and retires itself). They were never the same thing under one
// name; they were two different things under one name, which is worse.
//
// This IS duplication and it is filed as such: the follow-up is to make every ShowRevealCard call site
// raise a StoryBeats.Beat, so the cadence rules ("not too repetitive") and deferral-while-in-danger cover
// these moments too. That is integration work, deliberately NOT done inside the reunification merge.
public partial class Map
{
    /// <summary>What the captain is being shown: a title, a painting, and a caption that stops. The most
    /// modal thing in the game — it opens without being asked for, so Esc takes it before anything else
    /// (TryDismissTopOverlay in Map.Sim).</summary>
    private readonly record struct RevealCard(string Title, string Art, string Caption);

    private RevealCard? _revealCard;

    private void CloseRevealCard() => _revealCard = null;

    /// <summary>Raise the card. Deliberately silent — the caller owns its own audio cue, because the beat
    /// knows whether it is a find ("board"), a reveal ("reveal") or something louder, and a card that
    /// always chirped the same note would flatten three different moments into one.</summary>
    private void ShowRevealCard(string title, string art, string caption) =>
        _revealCard = new RevealCard(title, art, caption);
}
